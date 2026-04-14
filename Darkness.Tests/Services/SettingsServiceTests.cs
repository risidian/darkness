using Darkness.Core.Interfaces;
using Darkness.Core.Services;
using Moq;
using Xunit;

namespace Darkness.Tests.Services;

public class SettingsServiceTests
{
    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var fsMock = new Mock<IFileSystemService>();
        fsMock.Setup(f => f.AppDataDirectory).Returns(Path.GetTempPath());
        var service = new SettingsService(fsMock.Object);

        Assert.Equal(1.0, service.MasterVolume);
        Assert.Equal(0.8, service.MusicVolume);
        Assert.Equal(0.8, service.SfxVolume);
        Assert.Equal(0, service.LastUserId);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tmpDir);
        try
        {
            var fsMock = new Mock<IFileSystemService>();
            fsMock.Setup(f => f.AppDataDirectory).Returns(tmpDir);

            var service = new SettingsService(fsMock.Object);
            service.MasterVolume = 0.5;
            service.MusicVolume = 0.3;
            service.SfxVolume = 0.7;
            service.LastUserId = 42;
            await service.SaveSettingsAsync();

            var service2 = new SettingsService(fsMock.Object);
            await service2.LoadSettingsAsync();

            Assert.Equal(0.5, service2.MasterVolume);
            Assert.Equal(0.3, service2.MusicVolume);
            Assert.Equal(0.7, service2.SfxVolume);
            Assert.Equal(42, service2.LastUserId);
        }
        finally
        {
            Directory.Delete(tmpDir, true);
        }
    }

    [Fact]
    public async Task LoadSettings_MissingFile_KeepsDefaults()
    {
        var fsMock = new Mock<IFileSystemService>();
        fsMock.Setup(f => f.AppDataDirectory).Returns("/nonexistent/path");
        var service = new SettingsService(fsMock.Object);

        await service.LoadSettingsAsync();

        Assert.Equal(1.0, service.MasterVolume);
    }
}
