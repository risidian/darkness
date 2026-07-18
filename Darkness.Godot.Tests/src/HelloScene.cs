using Godot;

namespace Darkness.Godot.Tests;

/// <summary>
/// A tiny self-contained scene used to prove the gdUnit4 scene-testing loop
/// (ISceneRunner.Load + node assertions) without depending on the heavy
/// Darkness.Godot game autoloads/services.
/// </summary>
public partial class HelloScene : Control
{
    public string State { get; private set; } = "created";

    public override void _Ready() => State = "_Ready";
}
