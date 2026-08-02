using System;
using Godot;
using Darkness.Core.Interfaces;

namespace Darkness.Godot.Services;

public partial class GodotAudioService : Node, IAudioService
{
    private readonly ISettingsService _settings;
    
    private AudioStreamPlayer _musicPlayer = null!;
    private AudioStreamPlayer _musicPlayer2 = null!; // For crossfading
    private bool _usePlayer1 = true;

    public bool IsMusicPlaying => (_musicPlayer != null && _musicPlayer.Playing) || (_musicPlayer2 != null && _musicPlayer2.Playing);
    public string? CurrentTrack { get; private set; }

    public GodotAudioService(ISettingsService settings)
    {
        _settings = settings;
    }

    public override void _Ready()
    {
        _musicPlayer = new AudioStreamPlayer { Bus = "Music" };
        _musicPlayer2 = new AudioStreamPlayer { Bus = "Music" };

        AddChild(_musicPlayer);
        AddChild(_musicPlayer2);

        ApplyVolumeSettings();
    }

    public void PlayMusic(string trackName, bool loop = true)
    {
        if (CurrentTrack == trackName && IsMusicPlaying)
            return;

        CurrentTrack = trackName;
        
        string path = $"res://assets/audio/music/{trackName}.ogg";
        if (!ResourceLoader.Exists(path))
        {
            GD.PrintErr($"[AudioService] Music file not found: {path}");
            return;
        }

        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream == null) return;
        
        if (stream is AudioStreamOggVorbis oggStream)
        {
            oggStream.Loop = loop;
        }

        var currentPlayer = _usePlayer1 ? _musicPlayer : _musicPlayer2;
        var nextPlayer = _usePlayer1 ? _musicPlayer2 : _musicPlayer;
        
        _usePlayer1 = !_usePlayer1;

        nextPlayer.Stream = stream;
        nextPlayer.VolumeDb = -80.0f;
        nextPlayer.Play();

        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(nextPlayer, "volume_db", 0.0f, 1.0f);
        
        if (currentPlayer.Playing)
        {
            tween.TweenProperty(currentPlayer, "volume_db", -80.0f, 1.0f);
            tween.Chain().TweenCallback(Callable.From(currentPlayer.Stop));
        }
    }

    public void StopMusic(float fadeOutSeconds = 1.0f)
    {
        CurrentTrack = null;
        var currentPlayer = _usePlayer1 ? _musicPlayer2 : _musicPlayer;
        
        if (currentPlayer.Playing)
        {
            var tween = CreateTween();
            tween.TweenProperty(currentPlayer, "volume_db", -80.0f, fadeOutSeconds);
            tween.TweenCallback(Callable.From(currentPlayer.Stop));
        }
    }

    public void PlaySFX(string sfxName)
    {
        string path = $"res://assets/audio/sfx/{sfxName}.ogg";
        if (!ResourceLoader.Exists(path))
        {
            GD.PrintErr($"[AudioService] SFX file not found: {path}");
            return;
        }

        var stream = ResourceLoader.Load<AudioStream>(path);
        if (stream == null) return;

        var sfx = new AudioStreamPlayer { Stream = stream, Bus = "SFX" };
        AddChild(sfx);
        sfx.Finished += sfx.QueueFree;
        sfx.Play();
    }

    public void ApplyVolumeSettings()
    {
        SetBusVolume("Master", _settings.MasterVolume);
        SetBusVolume("Music", _settings.MusicVolume);
        SetBusVolume("SFX", _settings.SfxVolume);
    }

    private void SetBusVolume(string busName, double linearVolume)
    {
        int busIdx = AudioServer.GetBusIndex(busName);
        if (busIdx < 0) return;

        float clampedVolume = Mathf.Max((float)linearVolume, 0.0001f);
        float db = Mathf.LinearToDb(clampedVolume);
        AudioServer.SetBusVolumeDb(busIdx, db);
        AudioServer.SetBusMute(busIdx, linearVolume <= 0.001);
    }
}
