namespace Darkness.Godot.Tests;

using System.Threading.Tasks;

using GdUnit4;

using static GdUnit4.Assertions;

/// <summary>
/// Minimal proof that the gdUnit4 C# headless test loop works end-to-end:
/// 1. A trivial assertion (proves discovery + execution of C# gdUnit4 tests).
/// 2. A scene-loading test (proves ISceneRunner + the Godot runtime).
/// </summary>
[TestSuite]
public class PipelineTest
{
    [TestCase]
    public void TrivialMath()
        => AssertThat(2 + 2).IsEqual(4);

    [TestCase]
    [RequireGodotRuntime]
    public async Task SceneLoads()
    {
        var runner = ISceneRunner.Load("res://scenes/HelloScene.tscn", true);
        await runner.SimulateFrames(10);

        var scene = runner.Scene();
        AssertThat(scene)
            .IsNotNull()
            .IsInstanceOf<HelloScene>();

        var hello = (HelloScene)scene!;
        AssertThat(hello.GetNodeOrNull("Label")).IsNotNull();
        AssertThat(hello.State).IsEqual("_Ready");
    }
}
