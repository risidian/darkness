using System.IO;
using Xunit;

namespace Darkness.Tests;

public class ProjectConfigTests
{
    [Fact]
    public void ProjectGodot_ShouldNotUseWASAPIGlobally()
    {
        // Resolve the repo root via the test's compile-time source path so this
        // works regardless of the process cwd or redirected build output.
        string root = TestPaths.RepoRoot();
        string projectGodotPath = Path.Combine(root, "Darkness.Godot", "project.godot");

        Assert.True(File.Exists(projectGodotPath), $"project.godot not found: {projectGodotPath}");
        string content = File.ReadAllText(projectGodotPath);

        // Check if WASAPI is set globally (without platform tag)
        // Correct way is driver/driver.windows="WASAPI"
        Assert.DoesNotContain("driver/driver=\"WASAPI\"", content);
    }

    [Fact]
    public void CSharpProjects_ShouldTreatWarningsAsErrors()
    {
        string root = TestPaths.RepoRoot();

        string[] projects = {
            Path.Combine(root, "Darkness.Core", "Darkness.Core.csproj"),
            Path.Combine(root, "Darkness.Godot", "Darkness.Godot.csproj")
        };

        foreach (var project in projects)
        {
            Assert.True(File.Exists(project), $"Project file not found: {project}");
            string content = File.ReadAllText(project);
            Assert.Contains("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>", content, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
