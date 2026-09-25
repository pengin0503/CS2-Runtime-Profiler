using CS2RuntimeProfiler.Export;
using CS2RuntimeProfiler.UI;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class RuntimeReportMetadataTests
{
    [Test]
    public void Builder_includes_runtime_hardware_and_enabled_mods()
    {
        var metadata = new RuntimeReportMetadata
        {
            HardwareSummary = "CPU=Example CPU; RAM=32768 MB; GPU=Example GPU",
            EnabledMods = new[] { "Traffic.Mod", "Asset.Mod", "Traffic.Mod", "" }
        };

        var report = ProfilerReportBuilder.Build(
            new UiSnapshot(),
            gameVersion: "1.6.2f1",
            profilerVersion: "1.0.0",
            metadata: metadata);

        Assert.Multiple(() =>
        {
            Assert.That(report.HardwareSummary, Is.EqualTo(metadata.HardwareSummary));
            Assert.That(report.EnabledMods, Is.EqualTo(new[] { "Asset.Mod", "Traffic.Mod" }));
        });
    }
}
