using System.Linq;
using CS2RuntimeProfiler.Core;
using CS2RuntimeProfiler.UI;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class SystemMarkerTimingProjectorTests
{
    [Test]
    public void Full_name_entities_marker_projects_nanoseconds_to_milliseconds_with_statistics_and_patch_metadata()
    {
        var descriptor = new SystemDescriptor(
            "Game.Simulation.TrafficSystem",
            "Game",
            SystemSourceKind.Vanilla,
            null,
            MetricConfidence.Unavailable,
            new[] { new PatchOwnerInfo("TrafficTweaks", "Traffic Tweaks") });
        var recorder = new RecorderDescriptor(
            "CPU\u001fSimulation Game.Simulation.TrafficSystem",
            "CPU",
            "Simulation Game.Simulation.TrafficSystem",
            "TimeNanoseconds",
            "Int64");
        var capture = new CaptureSession("capture", new CaptureTrigger(CaptureTriggerKind.Manual, 10d, null), 32);
        capture.AddMarkerSample(recorder.Id, new MetricSample(10d, 2_000_000d, MetricConfidence.Full));
        capture.AddMarkerSample(recorder.Id, new MetricSample(10.5d, 4_000_000d, MetricConfidence.Full));
        capture.AddMarkerSample(recorder.Id, new MetricSample(11d, 6_000_000d, MetricConfidence.Full));

        var timing = SystemMarkerTimingProjector.Project(new[] { descriptor }, new[] { recorder }, capture);

        var system = timing.Systems.Single();
        Assert.That(system.SystemId, Is.EqualTo("Game.Simulation.TrafficSystem"));
        Assert.That(system.Milliseconds, Is.EqualTo(6d).Within(0.0001));
        Assert.That(system.MeanMilliseconds, Is.EqualTo(4d).Within(0.0001));
        Assert.That(system.MaxMilliseconds, Is.EqualTo(6d).Within(0.0001));
        Assert.That(system.Calls, Is.EqualTo(3));
        Assert.That(system.Confidence, Is.EqualTo(MetricConfidence.Full));
        Assert.That(system.OwnerAssembly, Is.EqualTo("Game"));
        Assert.That(system.PatchOwners, Is.EquivalentTo(new[] { "TrafficTweaks" }));
        Assert.That(timing.GetDirectAssemblyTotal("Game"), Is.EqualTo(6d).Within(0.0001));
        Assert.That(timing.GetDirectAssemblyTotal("TrafficTweaks"), Is.Zero, "patch ownership must not transfer vanilla runtime cost");

        var ui = UiSnapshotBuilder.Build(new UiSnapshotInput { Systems = timing });
        var row = ui.Systems.Single();
        Assert.That(row.MeanMilliseconds, Is.EqualTo(4d).Within(0.0001));
        Assert.That(row.P95Milliseconds, Is.Not.Null);
        Assert.That(row.P99Milliseconds, Is.Not.Null);
        Assert.That(row.MaxMilliseconds, Is.EqualTo(6d).Within(0.0001));
        Assert.That(row.Calls, Is.EqualTo(3));
    }

    [Test]
    public void Short_or_wrong_unit_marker_is_not_guessed_as_system_timing()
    {
        var descriptors = new[]
        {
            new SystemDescriptor("A.SharedSystem", "A", SystemSourceKind.Mod, "A", MetricConfidence.Unavailable),
            new SystemDescriptor("B.SharedSystem", "B", SystemSourceKind.Mod, "B", MetricConfidence.Unavailable)
        };
        var shortName = new RecorderDescriptor("CPU\u001fSimulation SharedSystem", "CPU", "Simulation SharedSystem", "TimeNanoseconds", "Int64");
        var wrongUnit = new RecorderDescriptor("CPU\u001fSimulation A.SharedSystem", "CPU", "Simulation A.SharedSystem", "Count", "Int64");
        var capture = new CaptureSession("capture", new CaptureTrigger(CaptureTriggerKind.Manual, 1d, null), 16);
        capture.AddMarkerSample(shortName.Id, new MetricSample(1d, 1_000_000d, MetricConfidence.Full));
        capture.AddMarkerSample(wrongUnit.Id, new MetricSample(1d, 1d, MetricConfidence.Full));

        var timing = SystemMarkerTimingProjector.Project(descriptors, new[] { shortName, wrongUnit }, capture);

        Assert.That(timing.Systems, Is.Empty);
    }
}
