using Godot;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class TransitionLayer : CanvasLayer
{
    private ColorRect _overlay = null!;
    private Tween? _currentTween;

    public override void _Ready()
    {
        Layer = 128; // Ensure it's on top of everything
        _overlay = new ColorRect
        {
            Color = Colors.Black,
            AnchorsPreset = (int)Control.LayoutPreset.FullRect,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _overlay.Modulate = new Color(1, 1, 1, 0); // Start transparent
        AddChild(_overlay);
    }

    public async Task FadeOut(float duration = 0.5f)
    {
        _currentTween?.Kill();
        _overlay.MouseFilter = Control.MouseFilterEnum.Stop; // Block input during fade
        _currentTween = CreateTween();
        _currentTween.TweenProperty(_overlay, "modulate:a", 1.0f, duration)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);
        
        await ToSignal(_currentTween, "finished");
    }

    public async Task FadeIn(float duration = 0.5f)
    {
        _currentTween?.Kill();
        _currentTween = CreateTween();
        _currentTween.TweenProperty(_overlay, "modulate:a", 0.0f, duration)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.In);
        
        await ToSignal(_currentTween, "finished");
        _overlay.MouseFilter = Control.MouseFilterEnum.Ignore; // Allow input again
    }
}
