using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Darkness.Tests.Services
{
    public class SessionPersistenceTests : IDisposable
    {
        private readonly Mock<IFileSystemService> _fileSystemMock;
        private readonly string _testAppDataDir;
        private readonly LocalDatabaseService _dbService;
        private readonly UserService _userService;
        private readonly SettingsService _settingsService;

        public SessionPersistenceTests()
        {
            _fileSystemMock = new Mock<IFileSystemService>();
            _testAppDataDir = Path.Combine(Path.GetTempPath(), "SessionPersistenceTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testAppDataDir);
            _fileSystemMock.Setup(f => f.AppDataDirectory).Returns(_testAppDataDir);
            _fileSystemMock.Setup(f => f.OpenAppPackageFileAsync(It.IsAny<string>()))
                           .ThrowsAsync(new FileNotFoundException());

            _dbService = new LocalDatabaseService(_fileSystemMock.Object);
            _userService = new UserService(_dbService);
            _settingsService = new SettingsService(_fileSystemMock.Object);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testAppDataDir))
                {
                    Directory.Delete(_testAppDataDir, true);
                }
            }
            catch (IOException) { }
        }

        private SessionService CreateSessionService() 
            => new SessionService(_settingsService, _userService);

        [Fact]
        public async Task Regression_SessionPersistsAfterMultipleRestarts()
        {
            // 1. Setup: Create user and "log in" (save ID)
            await _userService.InitializeAsync();
            var user = new User { Username = "persistent_user", Password = "pw" };
            await _userService.CreateUserAsync(user);
            int userId = user.Id;

            var session1 = CreateSessionService();
            session1.CurrentUser = user;
            _settingsService.LastUserId = userId;
            await _settingsService.SaveSettingsAsync();

            // 2. First Restart
            var session2 = CreateSessionService();
            await session2.InitializeAsync();
            Assert.NotNull(session2.CurrentUser);
            Assert.Equal(userId, session2.CurrentUser.Id);

            // 3. Second Restart (ensure settings file is stable)
            var session3 = CreateSessionService();
            await session3.InitializeAsync();
            Assert.NotNull(session3.CurrentUser);
            Assert.Equal(userId, session3.CurrentUser.Id);
        }

        [Fact]
        public async Task Regression_LogoutClearsPersistence()
        {
            // 1. Setup: Logged in user
            await _userService.InitializeAsync();
            var user = new User { Username = "logout_user", Password = "pw" };
            await _userService.CreateUserAsync(user);
            _settingsService.LastUserId = user.Id;
            await _settingsService.SaveSettingsAsync();

            // 2. Action: Logout (simulated via clearing setting)
            _settingsService.LastUserId = 0;
            await _settingsService.SaveSettingsAsync();

            // 3. Restart: Session should be empty
            var session = CreateSessionService();
            await session.InitializeAsync();
            Assert.Null(session.CurrentUser);
        }

        [Fact]
        public async Task Unit_InitializeAsync_IsThreadSafe()
        {
            // Arrange
            await _userService.InitializeAsync();
            var user = new User { Username = "thread_user", Password = "pw" };
            await _userService.CreateUserAsync(user);
            _settingsService.LastUserId = user.Id;
            await _settingsService.SaveSettingsAsync();

            var session = CreateSessionService();

            // Act: Fire multiple initializations simultaneously
            var tasks = new Task[10];
            for (int i = 0; i < 10; i++)
            {
                tasks[i] = session.InitializeAsync();
            }
            await Task.WhenAll(tasks);

            // Assert: Should be logged in and stable
            Assert.NotNull(session.CurrentUser);
            Assert.Equal(user.Id, session.CurrentUser.Id);
        }

        [Fact]
        public async Task Unit_InitializeAsync_HandlesMissingUserGracefully()
        {
            // Arrange: ID exists in settings but user deleted from DB
            _settingsService.LastUserId = 999;
            await _settingsService.SaveSettingsAsync();

            var session = CreateSessionService();

            // Act
            await session.InitializeAsync();

            // Assert
            Assert.Null(session.CurrentUser);
        }
    }
}
