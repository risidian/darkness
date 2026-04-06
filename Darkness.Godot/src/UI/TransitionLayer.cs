using Godot;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class TransitionLayer : CanvasLayer
{
    private ColorRect _overlay = null!;

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
        _overlay.MouseFilter = Control.MouseFilterEnum.Stop; // Block input during fade
        var tween = CreateTween();
        tween.TweenProperty(_overlay, "modulate:a", 1.0f, duration)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.Out);
        
        await ToSignal(tween, "finished");
    }

    public async Task FadeIn(float duration = 0.5f)
    {
        var tween = CreateTween();
        tween.TweenProperty(_overlay, "modulate:a", 0.0f, duration)
             .SetTrans(Tween.TransitionType.Quad)
             .SetEase(Tween.EaseType.In);
        
        await ToSignal(tween, "finished");
        _overlay.MouseFilter = Control.MouseFilterEnum.Ignore; // Allow input again
    }
}
