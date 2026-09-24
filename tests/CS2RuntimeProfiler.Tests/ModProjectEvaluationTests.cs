using System.Diagnostics;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class ModProjectEvaluationTests
{
    [Test]
    public void Mod_project_reads_CSII_paths_from_process_environment()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(repositoryRoot, "src", "CS2RuntimeProfiler", "CS2RuntimeProfiler.csproj");
        var temporaryDirectory = Directory.CreateTempSubdirectory("cs2-runtime-profiler-msbuild-");

        try
        {
            File.WriteAllText(Path.Combine(temporaryDirectory.FullName, "Mod.props"), "<Project />");
            File.WriteAllText(Path.Combine(temporaryDirectory.FullName, "Mod.targets"), "<Project />");

            var managedPath = Path.Combine(temporaryDirectory.FullName, "Managed");
            Directory.CreateDirectory(managedPath);

            var startInfo = CreateMsBuildStartInfo(projectPath, "CustomManagedPath", temporaryDirectory.FullName, managedPath);

            using var process = Process.Start(startInfo)!;
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Assert.That(
                process.ExitCode,
                Is.EqualTo(0),
                $"MSBuild project evaluation failed.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
            Assert.That(standardOutput, Does.Contain(managedPath));
        }
        finally
        {
            temporaryDirectory.Delete(recursive: true);
        }
    }

    [Test]
    public void Mod_project_uses_game_mscorlib_from_managed_path()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(repositoryRoot, "src", "CS2RuntimeProfiler", "CS2RuntimeProfiler.csproj");
        var temporaryDirectory = Directory.CreateTempSubdirectory("cs2-runtime-profiler-mscorlib-");

        try
        {
            File.WriteAllText(Path.Combine(temporaryDirectory.FullName, "Mod.props"), "<Project />");
            File.WriteAllText(Path.Combine(temporaryDirectory.FullName, "Mod.targets"), "<Project />");

            var managedPath = Path.Combine(temporaryDirectory.FullName, "Managed");
            Directory.CreateDirectory(managedPath);
            var mscorlibPath = Path.Combine(managedPath, "mscorlib.dll");
            File.WriteAllBytes(mscorlibPath, Array.Empty<byte>());

            var startInfo = CreateMsBuildStartInfo(projectPath, "MSCORLIBPath", temporaryDirectory.FullName, managedPath);

            using var process = Process.Start(startInfo)!;
            var standardOutput = process.StandardOutput.ReadToEnd();
            var standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();

            Assert.That(
                process.ExitCode,
                Is.EqualTo(0),
                $"MSBuild project evaluation failed.{Environment.NewLine}{standardOutput}{Environment.NewLine}{standardError}");
            Assert.That(standardOutput, Does.Contain(mscorlibPath));
        }
        finally
        {
            temporaryDirectory.Delete(recursive: true);
        }
    }

    private static ProcessStartInfo CreateMsBuildStartInfo(
        string projectPath,
        string propertyName,
        string toolPath,
        string managedPath)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("-nologo");
        startInfo.ArgumentList.Add($"-getProperty:{propertyName}");
        startInfo.Environment["CSII_MANAGED_PATH"] = managedPath;
        startInfo.Environment["CSII_TOOLPATH"] = toolPath;
        return startInfo;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "CS2RuntimeProfiler.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root from the test directory.");
    }
}
