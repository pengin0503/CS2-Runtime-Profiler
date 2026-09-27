using CS2RuntimeProfiler.Core;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class ManagedSystemTimingFallbackTests
{
    [Test]
    public void Managed_accumulator_projects_bounded_system_statistics_with_managed_confidence()
    {
        var descriptor = new SystemDescriptor(
            "Game.Simulation.ExampleSystem",
            "Game",
            SystemSourceKind.Vanilla,
            null,
            MetricConfidence.Unavailable,
            new[] { new PatchOwnerInfo("patch.owner", "Patch Owner") });
        var accumulator = new ManagedSystemTimingAccumulator(maxSamplesPerSystem: 8);

        accumulator.Record(descriptor.FullTypeName, 0.25);
        accumulator.Record(descriptor.FullTypeName, 0.75);

        var snapshot = accumulator.BuildSnapshot(new[] { descriptor });
        var system = snapshot.Systems.Single();

        Assert.Multiple(() =>
        {
            Assert.That(system.SystemId, Is.EqualTo(descriptor.FullTypeName));
            Assert.That(system.Confidence, Is.EqualTo(MetricConfidence.Managed));
            Assert.That(system.OwnerAssembly, Is.EqualTo("Game"));
            Assert.That(system.SourceKind, Is.EqualTo(SystemSourceKind.Vanilla));
            Assert.That(system.MeanMilliseconds, Is.EqualTo(0.5).Within(0.0001));
            Assert.That(system.TotalMilliseconds, Is.EqualTo(1.0).Within(0.0001));
            Assert.That(system.Calls, Is.EqualTo(2));
            Assert.That(system.PatchOwners, Does.Contain("patch.owner"));
        });
    }

    [Test]
    public void Full_marker_timing_wins_and_managed_fallback_only_fills_missing_systems()
    {
        var full = new SystemTimingSnapshot();
        full.AddSystem("System.A", 1.5, MetricConfidence.Full, "Game");

        var managed = new SystemTimingSnapshot();
        managed.AddSystem("System.A", 9.0, MetricConfidence.Managed, "Game");
        managed.AddSystem("System.B", 2.0, MetricConfidence.Managed, "Mod.B", sourceKind: SystemSourceKind.Mod);

        var merged = SystemTimingSnapshotMerger.Merge(full, managed);

        Assert.Multiple(() =>
        {
            Assert.That(merged.Systems.Count, Is.EqualTo(2));
            Assert.That(merged.Systems.Single(x => x.SystemId == "System.A").Milliseconds, Is.EqualTo(1.5));
            Assert.That(merged.Systems.Single(x => x.SystemId == "System.A").Confidence, Is.EqualTo(MetricConfidence.Full));
            Assert.That(merged.Systems.Single(x => x.SystemId == "System.B").Confidence, Is.EqualTo(MetricConfidence.Managed));
        });
    }
}
