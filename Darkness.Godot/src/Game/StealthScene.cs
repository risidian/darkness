using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace Darkness.Godot.Game;

public partial class StealthScene : Control, IInitializable
{
    private INavigationService _navigation = null!;
    private ISessionService _session = null!;

    private ProgressBar _stealthProgress = null!;
    private ProgressBar _detectionLevel = null!;
    private Label _statusLabel = null!;
    private Button _sneakButton = null!;

    // Timing Bar Components
    private Control _timingBar = null!;
    private ColorRect _targetZone = null!;
    private ColorRect _slider = null!;

    private int _successes = 0;
    private int _failures = 0;
    private const int MaxSuccesses = 5;
    private const int MaxFailures = 3;

    private Tween? _sliderTween;
    private bool _isMovingRight = true;

    [Export] public float SliderPosition { get; set; } = 0f; // 0 to 1

    private string _successScene = "WorldScene";
    private string _failureScene = "BattleScene";

    public void Initialize(IDictionary<string, object> parameters)
    {
        if (parameters.ContainsKey("SuccessScene"))
            _successScene = parameters["SuccessScene"].ToString() ?? "WorldScene";

        if (parameters.ContainsKey("FailureScene"))
            _failureScene = parameters["FailureScene"].ToString() ?? "BattleScene";
    }

    public override void _Ready()
    {
        var global = GetNode<Global>("/root/Global");
        var sp = global.Services!;
        _navigation = sp.GetRequiredService<INavigationService>();
        _session = sp.GetRequiredService<ISessionService>();

        // UI Bindings
        _stealthProgress = GetNode<ProgressBar>("%StealthProgress");
        _detectionLevel = GetNode<ProgressBar>("%DetectionLevel");
        _statusLabel = GetNode<Label>("%StatusLabel");
        _sneakButton = GetNode<Button>("%SneakButton");

        _timingBar = GetNode<Control>("%TimingBar");
        _targetZone = GetNode<ColorRect>("%TargetZone");
        _slider = GetNode<ColorRect>("%Slider");

        _stealthProgress.MaxValue = MaxSuccesses;
        _stealthProgress.Value = 0;

        _detectionLevel.MaxValue = MaxFailures;
        _detectionLevel.Value = 0;

        _sneakButton.Pressed += OnSneakPressed;

        StartSliderAnimation();
    }

    private void StartSliderAnimation()
    {
        if (_sliderTween != null && _sliderTween.IsValid())
            _sliderTween.Kill();

        _sliderTween = CreateTween();
        _sliderTween.SetLoops(); // Loop forever

        // Move from 0 to 1 and back
        _sliderTween.TweenProperty(this, "SliderPosition", 1.0f, 1.5f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
        _sliderTween.TweenProperty(this, "SliderPosition", 0.0f, 1.5f)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.InOut);
    }

    public override void _Process(double delta)
    {
        // Update visual slider position based on SliderPosition (0 to 1)
        float barWidth = _timingBar.CustomMinimumSize.X;
        if (barWidth <= 0) barWidth = _timingBar.Size.X;

        float sliderWidth = _slider.Size.X;
        float availableWidth = barWidth - sliderWidth;

        _slider.Position = new Vector2(SliderPosition * availableWidth, _slider.Position.Y);
    }

    private void OnSneakPressed()
    {
        if (_successes >= MaxSuccesses || _failures >= MaxFailures) return;

        // Check if slider is within target zone
        float barWidth = _timingBar.Size.X;
        float sliderCenter = _slider.Position.X + (_slider.Size.X / 2f);

        float targetStart = _targetZone.Position.X;
        float targetEnd = targetStart + _targetZone.Size.X;

        if (sliderCenter >= targetStart && sliderCenter <= targetEnd)
        {
            HandleSuccess();
        }
        else
        {
            HandleFailure();
        }
    }

    private void HandleSuccess()
    {
        _successes++;
        _stealthProgress.Value = _successes;
        _statusLabel.Text = "Successful sneak!";
        _statusLabel.Set("theme_override_colors/font_color", Colors.Green);

        if (_successes >= MaxSuccesses)
        {
            EndGame(true);
        }
    }

    private void HandleFailure()
    {
        _failures++;
        _detectionLevel.Value = _failures;
        _statusLabel.Text = "You were spotted!";
        _statusLabel.Set("theme_override_colors/font_color", Colors.Red);

        if (_failures >= MaxFailures)
        {
            EndGame(false);
        }
    }

    private async void EndGame(bool success)
    {
        _sneakButton.Disabled = true;
        if (_sliderTween != null && _sliderTween.IsValid())
            _sliderTween.Kill();

        var parameters = new Dictionary<string, object> { { "StealthOutcome", success ? "Success" : "Failure" } };

        if (success)
        {
            _statusLabel.Text = "Area cleared! Moving forward...";
            await ToSignal(GetTree().CreateTimer(1.5), "timeout");
            await _navigation.NavigateToAsync(Routes.World, parameters);
        }
        else
        {
            _statusLabel.Text = "Enemies alerted! Prepare for battle!";
            await ToSignal(GetTree().CreateTimer(1.5), "timeout");
            await _navigation.NavigateToAsync(Routes.World, parameters);
        }
    }
}