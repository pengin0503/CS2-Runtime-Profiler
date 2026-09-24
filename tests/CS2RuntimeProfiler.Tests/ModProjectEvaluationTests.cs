using System.Xml.Linq;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class ModProjectEvaluationTests
{
    [Test]
    public void Mod_project_delegates_runtime_paths_to_the_CS2_toolchain()
    {
        var projectPath = FindModProjectPath();
        var projectText = File.ReadAllText(projectPath);

        Assert.Multiple(() =>
        {
            Assert.That(projectText, Does.Contain("<Import Project=\"$(CSII_TOOLPATH)\\Mod.props\" />"));
            Assert.That(projectText, Does.Contain("<Import Project=\"$(CSII_TOOLPATH)\\Mod.targets\" />"));
            Assert.That(projectText, Does.Not.Contain("CSII_MANAGED_PATH"));
            Assert.That(projectText, Does.Not.Contain("<CustomManagedPath"));
            Assert.That(projectText, Does.Not.Contain("<MSCORLIBPath"));
        });
    }

    [Test]
    public void Mod_project_updates_framework_serialization_reference_instead_of_adding_one()
    {
        var document = XDocument.Load(FindModProjectPath());
        var references = document
            .Descendants("Reference")
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(
                references.Any(reference => (string?)reference.Attribute("Include") == "System.Runtime.Serialization"),
                Is.False,
                "Adding a new System.Runtime.Serialization reference can resolve a second framework profile through the CS2 Managed search path.");
            Assert.That(
                references.Any(reference => (string?)reference.Attribute("Update") == "System.Runtime.Serialization"),
                Is.True,
                "The existing framework reference should only have Private metadata updated.");
        });
    }

    private static string FindModProjectPath()
    {
        return Path.Combine(
            FindRepositoryRoot(),
            "src",
            "CS2RuntimeProfiler",
            "CS2RuntimeProfiler.csproj");
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
