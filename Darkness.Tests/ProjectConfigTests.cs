using System.IO;
using Xunit;

namespace Darkness.Tests;

public class ProjectConfigTests
{
    [Fact]
    public void ProjectGodot_ShouldNotUseWASAPIGlobally()
    {
        // Path to project.godot relative to test execution directory
        // Usually, in dotnet test, the working directory is the project folder or bin
        // We'll try to find it by going up from bin/Debug/net10.0
        string rootDir = Directory.GetCurrentDirectory();
        
        // Navigate up to find the project root (Darkness.Godot is at root/Darkness.Godot)
        string? projectGodotPath = null;
        var dir = new DirectoryInfo(rootDir);
        while (dir != null)
        {
            var potentialPath = Path.Combine(dir.FullName, "Darkness.Godot", "project.godot");
            if (File.Exists(potentialPath))
            {
                projectGodotPath = potentialPath;
                break;
            }
            dir = dir.Parent;
        }

        Assert.NotNull(projectGodotPath);
        string content = File.ReadAllText(projectGodotPath);

        // Check if WASAPI is set globally (without platform tag)
        // Correct way is driver/driver.windows="WASAPI"
        Assert.DoesNotContain("driver/driver=\"WASAPI\"", content);
    }

    [Fact]
    public void CSharpProjects_ShouldTreatWarningsAsErrors()
    {
        string rootDir = Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(rootDir);
        string? root = null;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Darkness.sln")))
            {
                root = dir.FullName;
                break;
            }
            dir = dir.Parent;
        }

        Assert.NotNull(root);

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
