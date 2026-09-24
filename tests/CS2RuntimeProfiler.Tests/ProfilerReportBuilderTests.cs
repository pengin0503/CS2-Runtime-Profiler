using CS2RuntimeProfiler.Export;
using CS2RuntimeProfiler.UI;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class ProfilerReportBuilderTests
{
    [Test]
    public void Builder_maps_visible_snapshot_sections_without_city_name()
    {
        var snapshot = new UiSnapshot
        {
            Global = new GlobalUiMetrics
            {
                Available = true,
                SelectedSpeed = 4,
                ActualSpeed = 2.8,
                Efficiency = 0.7,
                RecorderMetrics = new[]
                {
                    new UiMetricRow { Id = "main", Value = 9.2, Confidence = "Full", Availability = "Available" }
                }
            },
            Systems = new[]
            {
                new SystemUiRow { Id = "Example.System", OwnerAssembly = "Example.Mod", CurrentMilliseconds = 1.4, Confidence = "Managed" }
            },
            Mods = new[]
            {
                new ModUiRow { AssemblyName = "Example.Mod", DirectSystemMilliseconds = 1.4, DirectSystemCount = 1 }
            },
            Pathfinding = new PathfindingUiMetrics
            {
                Metrics = new[] { new UiMetricRow { Id = "pendingPathfindActions", Value = 42, Confidence = "Indirect", Availability = "Available" } }
            },
            DomainMetrics = new[]
            {
                new UiMetricRow { Id = "citizens", Value = 40000, Confidence = "Indirect", Availability = "Available" }
            },
            Diagnostics = new DiagnosticsUi { ProfilerOverheadShare = 0.025, UnattributedJobsMilliseconds = 3.1 }
        };

        var report = ProfilerReportBuilder.Build(snapshot, gameVersion: "1.6.0f1", profilerVersion: "0.1.0");

        Assert.That(report.GameVersion, Is.EqualTo("1.6.0f1"));
        Assert.That(report.ProfilerVersion, Is.EqualTo("0.1.0"));
        Assert.That(report.CityName, Is.Null);
        Assert.That(report.GlobalMetrics.Any(x => x.Name == "efficiency" && x.Value == 0.7), Is.True);
        Assert.That(report.Systems.Single().OwnerAssembly, Is.EqualTo("Example.Mod"));
        Assert.That(report.ModAttribution.Any(x => x.Name == "Example.Mod"), Is.True);
        Assert.That(report.Pathfinding.Single().Name, Is.EqualTo("pendingPathfindActions"));
        Assert.That(report.DomainMetrics.Single().Name, Is.EqualTo("citizens"));
        Assert.That(report.ProfilerOverhead.Any(x => x.Name == "captureOverheadShare" && x.Value == 0.025), Is.True);
    }
}
