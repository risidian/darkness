using Darkness.Core.Interfaces;
using System.Threading.Tasks;

namespace Darkness.Core.Services
{
    public class SettingsService : ISettingsService
    {
        public double MasterVolume { get; set; } = 1.0;
        public double MusicVolume { get; set; } = 0.8;
        public double SfxVolume { get; set; } = 0.8;

        public Task LoadSettingsAsync()
        {
            // Mock implementation
            return Task.CompletedTask;
        }

        public Task SaveSettingsAsync()
        {
            // Mock implementation
            return Task.CompletedTask;
        }
    }
}
