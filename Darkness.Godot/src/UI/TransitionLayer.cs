using Godot;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class TransitionLayer : CanvasLayer
{
    private ColorRect _overlay = null!;
    private Label _loadingLabel = null!;
    private Tween? _currentTween;

    public override void _Ready()
    {
        Layer = 128; // Ensure it's on top of everything
        _overlay = new ColorRect
        {
            Color = Colors.Black,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _overlay.Modulate = new Color(1, 1, 1, 0); // Start transparent

        _loadingLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _loadingLabel.AddThemeFontSizeOverride("font_size", 32);
        _loadingLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _loadingLabel.Modulate = new Color(1, 1, 1, 0); // Start transparent

        _overlay.AddChild(_loadingLabel);
        AddChild(_overlay);
    }

    public async Task FadeOut(string loadingText = "", float duration = 0.5f)
    {
        _currentTween?.Kill();
        _overlay.MouseFilter = Control.MouseFilterEnum.Stop; // Block input during fade
        
        if (!string.IsNullOrEmpty(loadingText))
        {
            _loadingLabel.Text = loadingText.StartsWith("Loading") ? loadingText : $"Loading: {loadingText}";
        }
        else
        {
            _loadingLabel.Text = "";
        }

        _currentTween = CreateTween();
        _currentTween.SetParallel(true);
        _currentTween.TweenProperty(_overlay, "modulate:a", 1.0f, duration)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);
             
        if (!string.IsNullOrEmpty(loadingText))
        {
            _currentTween.TweenProperty(_loadingLabel, "modulate:a", 1.0f, duration)
                 .SetTrans(Tween.TransitionType.Quad)
                 .SetEase(Tween.EaseType.Out);
        }
        _currentTween.SetParallel(false);
        
        await ToSignal(_currentTween, "finished");
    }

    public async Task FadeIn(float duration = 0.5f)
    {
        _currentTween?.Kill();
        _currentTween = CreateTween();
        
        _currentTween.SetParallel(true);
        _currentTween.TweenProperty(_overlay, "modulate:a", 0.0f, duration)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.In);
        _currentTween.TweenProperty(_loadingLabel, "modulate:a", 0.0f, duration)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.In);
        _currentTween.SetParallel(false);
        
        await ToSignal(_currentTween, "finished");
        _overlay.MouseFilter = Control.MouseFilterEnum.Ignore; // Allow input again
    }
}
