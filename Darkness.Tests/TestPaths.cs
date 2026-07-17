using System.IO;
using System.Runtime.CompilerServices;

namespace Darkness.Tests;

/// <summary>
/// Resolves the repository root independently of the process working directory
/// or build-output location. Uses <see cref="CallerFilePathAttribute"/>, which
/// binds to the compile-time source path (the checkout dir), so it is correct
/// on CI runners and locally even when build output is redirected elsewhere.
/// </summary>
public static class TestPaths
{
    /// <summary>
    /// Walks up from the calling test's source file until a directory containing
    /// <c>Darkness.sln</c> is found, and returns that directory.
    /// </summary>
    public static string RepoRoot([CallerFilePath] string callerFilePath = "")
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(callerFilePath)!);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Darkness.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate Darkness.sln walking up from '{callerFilePath}'.");
    }
}
