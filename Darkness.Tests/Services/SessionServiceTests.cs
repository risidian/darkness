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
    public class SessionServiceTests : IDisposable
    {
        private readonly Mock<IFileSystemService> _fileSystemMock;
        private readonly string _testAppDataDir;
        private readonly LocalDatabaseService _dbService;
        private readonly UserService _userService;
        private readonly SettingsService _settingsService;
        private readonly SessionService _sessionService;

        public SessionServiceTests()
        {
            _fileSystemMock = new Mock<IFileSystemService>();
            _testAppDataDir = Path.Combine(Path.GetTempPath(), "SessionServiceTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testAppDataDir);
            _fileSystemMock.Setup(f => f.AppDataDirectory).Returns(_testAppDataDir);
            _fileSystemMock.Setup(f => f.OpenAppPackageFileAsync(It.IsAny<string>()))
                           .ThrowsAsync(new FileNotFoundException()); // Don't copy seed db

            _dbService = new LocalDatabaseService(_fileSystemMock.Object);
            _userService = new UserService(_dbService);
            _settingsService = new SettingsService(_fileSystemMock.Object);
            _sessionService = new SessionService(_settingsService, _userService);
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
            catch (IOException)
            {
                // SQLite sometimes keeps a lock on the file for a bit
            }
        }

        [Fact]
        public async Task InitializeAsync_WhenLastUserIdSet_LoadsUser()
        {
            // Arrange
            await _userService.InitializeAsync();
            var user = new User { Username = "testuser", Password = "password" };
            await _userService.CreateUserAsync(user);
            int userId = user.Id;
            Assert.True(userId > 0);

            // Simulate saving last user ID
            _settingsService.LastUserId = userId;
            await _settingsService.SaveSettingsAsync();

            // Create a fresh session service instance to simulate app restart
            var freshSessionService = new SessionService(_settingsService, _userService);

            // Act
            await freshSessionService.InitializeAsync();

            // Assert
            Assert.NotNull(freshSessionService.CurrentUser);
            Assert.Equal(userId, freshSessionService.CurrentUser.Id);
            Assert.Equal("testuser", freshSessionService.CurrentUser.Username);
        }

        [Fact]
        public async Task InitializeAsync_WhenNoLastUserId_DoesNotLoadUser()
        {
            // Act
            await _sessionService.InitializeAsync();

            // Assert
            Assert.Null(_sessionService.CurrentUser);
        }

        [Fact]
        public async Task InitializeAsync_ConcurrentCalls_InitializesOnce()
        {
            // Arrange
            await _userService.InitializeAsync();
            var user = new User { Username = "concurrent", Password = "password" };
            await _userService.CreateUserAsync(user);
            _settingsService.LastUserId = user.Id;
            await _settingsService.SaveSettingsAsync();

            var service = new SessionService(_settingsService, _userService);

            // Act
            var task1 = service.InitializeAsync();
            var task2 = service.InitializeAsync();
            var task3 = service.InitializeAsync();

            await Task.WhenAll(task1, task2, task3);

            // Assert
            Assert.NotNull(service.CurrentUser);
            Assert.Equal(user.Id, service.CurrentUser.Id);
        }
    }
}
