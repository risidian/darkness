using Darkness.Core.Interfaces;
using System.Text.Json;
using System.IO;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IFileSystemService _fileSystem;
        private readonly string _settingsPath;

        public double MasterVolume { get; set; } = 1.0;
        public double MusicVolume { get; set; } = 0.8;
        public double SfxVolume { get; set; } = 0.8;
        public int LastUserId { get; set; } = 0;

        public SettingsService(IFileSystemService fileSystem)
        {
            _fileSystem = fileSystem;
            _settingsPath = Path.Combine(_fileSystem.AppDataDirectory, "settings.json");
        }

        public async Task LoadSettingsAsync()
        {
            if (!File.Exists(_settingsPath))
                return;

            try
            {
                string json = await File.ReadAllTextAsync(_settingsPath);
                var settings = JsonSerializer.Deserialize<SettingsData>(json);       
                if (settings != null)
                {
                    MasterVolume = settings.MasterVolume;
                    MusicVolume = settings.MusicVolume;
                    SfxVolume = settings.SfxVolume;
                    LastUserId = settings.LastUserId;
                }
            }
            catch (System.Exception)
            {
                // Silently fail if settings can't be loaded
            }
        }

        public async Task SaveSettingsAsync()
        {
            try
            {
                string? directory = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))   
                {
                    Directory.CreateDirectory(directory);
                }

                var settings = new SettingsData
                {
                    MasterVolume = MasterVolume,
                    MusicVolume = MusicVolume,
                    SfxVolume = SfxVolume,
                    LastUserId = LastUserId
                };

                string json = JsonSerializer.Serialize(settings);
                await File.WriteAllTextAsync(_settingsPath, json);
            }
            catch (System.Exception)
            {
                // Silently fail if settings can't be saved
            }
        }

        internal class SettingsData
        {
            public double MasterVolume { get; set; }
            public double MusicVolume { get; set; }
            public double SfxVolume { get; set; }  
            public int LastUserId { get; set; }    
        }
    }
}