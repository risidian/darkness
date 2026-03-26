using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Moq;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Darkness.Tests
{
    public class LocalDatabaseServiceTests : IDisposable
    {
        private readonly Mock<IFileSystemService> _fileSystemMock;
        private readonly LocalDatabaseService _service;
        private readonly string _testAppDataDir;
        private readonly string _testFilename = "test.db3";

        public LocalDatabaseServiceTests()
        {
            _fileSystemMock = new Mock<IFileSystemService>();
            _testAppDataDir = Path.Combine(Path.GetTempPath(), "DarknessTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testAppDataDir);
            _fileSystemMock.Setup(f => f.AppDataDirectory).Returns(_testAppDataDir);
            _service = new LocalDatabaseService(_fileSystemMock.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testAppDataDir))
            {
                Directory.Delete(_testAppDataDir, true);
            }
        }

        [Fact]
        public async Task CopyDatabaseIfNotExistsAsync_WhenFileExists_DoesNotCopy()
        {
            // Arrange
            string targetPath = Path.Combine(_testAppDataDir, _testFilename);
            await File.WriteAllTextAsync(targetPath, "existing content");

            // Act
            await _service.CopyDatabaseIfNotExistsAsync(_testFilename);

            // Assert
            _fileSystemMock.Verify(f => f.OpenAppPackageFileAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CopyDatabaseIfNotExistsAsync_WhenFileDoesNotExist_CopiesFromPackage()
        {
            // Arrange
            string content = "seed database content";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            _fileSystemMock.Setup(f => f.OpenAppPackageFileAsync(_testFilename))
                           .ReturnsAsync(stream);

            // Act
            await _service.CopyDatabaseIfNotExistsAsync(_testFilename);

            // Assert
            string targetPath = Path.Combine(_testAppDataDir, _testFilename);
            Assert.True(File.Exists(targetPath));
            string actualContent = await File.ReadAllTextAsync(targetPath);
            Assert.Equal(content, actualContent);
            _fileSystemMock.Verify(f => f.OpenAppPackageFileAsync(_testFilename), Times.Once);
        }

        [Fact]
        public async Task CopyDatabaseIfNotExistsAsync_WhenPackageFileNotFound_DoesNotThrow()
        {
            // Arrange
            _fileSystemMock.Setup(f => f.OpenAppPackageFileAsync(_testFilename))
                           .ThrowsAsync(new FileNotFoundException("Not found in package"));

            // Act
            var exception = await Record.ExceptionAsync(() => _service.CopyDatabaseIfNotExistsAsync(_testFilename));

            // Assert
            Assert.Null(exception);
            string targetPath = Path.Combine(_testAppDataDir, _testFilename);
            Assert.False(File.Exists(targetPath));
        }

        [Fact]
        public async Task CopyDatabaseIfNotExistsAsync_WhenOtherExceptionOccurs_DoesNotThrow()
        {
            // Arrange
            _fileSystemMock.Setup(f => f.OpenAppPackageFileAsync(_testFilename))
                           .ThrowsAsync(new Exception("Some other error"));

            // Act
            var exception = await Record.ExceptionAsync(() => _service.CopyDatabaseIfNotExistsAsync(_testFilename));

            // Assert
            Assert.Null(exception);
        }
    }
}
