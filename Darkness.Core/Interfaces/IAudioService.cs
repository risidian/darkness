using System;

namespace Darkness.Core.Interfaces;

public interface IAudioService
{
    void PlayMusic(string trackName, bool loop = true);
    void StopMusic(float fadeOutSeconds = 1.0f);
    void PlaySFX(string sfxName);
    void ApplyVolumeSettings();
    bool IsMusicPlaying { get; }
    string? CurrentTrack { get; }
}
