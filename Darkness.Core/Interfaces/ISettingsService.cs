using System.Threading.Tasks;

namespace Darkness.Core.Interfaces
{
    public interface ISettingsService
    {
        double MasterVolume { get; set; }
        double MusicVolume { get; set; }
        double SfxVolume { get; set; }
        int LastUserId { get; set; }

        Task LoadSettingsAsync();
        Task SaveSettingsAsync();
    }
}