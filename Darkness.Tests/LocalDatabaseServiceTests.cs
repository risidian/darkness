using Darkness.Core.Data;
using Darkness.Core.Interfaces;
using Moq;
using System;
using System.IO;
using Xunit;

namespace Darkness.Tests
{
    public class LocalDatabaseServiceTests : IDisposable
    {
        private readonly Mock<IFileSystemService> _fileSystemMock;
        private readonly LocalDatabaseService _service;
        private readonly string _testAppDataDir;

        public LocalDatabaseServiceTests()
        {
            _fileSystemMock = new Mock<IFileSystemService>();
            _testAppDataDir = Path.Combine(Path.GetTempPath(), "DarknessTests_" + Guid.NewGuid().ToString());
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
        public void GetLocalFilePath_ReturnsCorrectPath()
        {
            string result = _service.GetLocalFilePath("test.db");
            Assert.Equal(Path.Combine(_testAppDataDir, "test.db"), result);
        }

        [Fact]
        public void GetLocalFilePath_CreatesDirectoryIfMissing()
        {
            // The constructor now creates the directory by default to ensure path is valid
            _service.GetLocalFilePath("test.db");

            Assert.True(Directory.Exists(_testAppDataDir));
        }
    }
}
