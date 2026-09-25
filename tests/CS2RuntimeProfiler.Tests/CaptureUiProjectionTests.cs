using System.Collections.Generic;
using System.Linq;
using CS2RuntimeProfiler.Core;
using CS2RuntimeProfiler.UI;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class CaptureUiProjectionTests
{
    [Test]
    public void Capture_summary_exposes_duration_max_overhead_and_correlated_changes()
    {
        var capture = new CaptureSession(
            "capture-1",
            new CaptureTrigger(CaptureTriggerKind.Manual, 10d, null),
            maxSamplesPerSeries: 64);

        capture.SetMarkerCoverage(100, 80, true);
        capture.AddGlobalSample(Global(6d, 4d, 4d, 10d));
        capture.AddGlobalSample(Global(9d, 4d, 4d, 10d));
        capture.AddGlobalSample(Global(10.5d, 4d, 2d, 20d));
        capture.AddGlobalSample(Global(12d, 4d, 2d, 20d));
        capture.ObserveProfilerOverheadShare(0.03d);
        capture.ObserveProfilerOverheadShare(0.05d);

        var timing = new SystemTimingSnapshot();
        timing.AddSystem("Example.System", 3.25d, MetricConfidence.Managed, "Example.Mod");
        capture.SetSystemTiming(timing);

        var snapshot = UiSnapshotBuilder.Build(new UiSnapshotInput
        {
            Captures = new[] { capture }
        });

        var summary = snapshot.Captures.Single();
        Assert.That(summary.DurationSeconds, Is.EqualTo(6d).Within(0.0001));
        Assert.That(summary.ProfilerOverheadShare, Is.EqualTo(0.05d).Within(0.0001));
        Assert.That(summary.CorrelatedChanges.Any(x => x.Metric == "actualSpeed" && x.Delta == -2d), Is.True);
        Assert.That(summary.CorrelatedChanges.Any(x => x.Metric == "CPU\u001fMain Thread" && x.Delta == 10d), Is.True);
        Assert.That(snapshot.Timeline.Any(x => x.Metric == "recorder:CPU\u001fMain Thread"), Is.True);
        Assert.That(snapshot.Timeline.Any(x => x.Metric == "system:Example.System" && x.Value == 3.25d), Is.True);
    }

    [Test]
    public void Current_capture_is_not_counted_or_listed_as_completed()
    {
        var completed = new CaptureSession("completed", new CaptureTrigger(CaptureTriggerKind.Manual, 1d, null), 16);
        completed.AddGlobalSample(Global(1d, 1d, 1d, 1d));
        var current = new CaptureSession("current", new CaptureTrigger(CaptureTriggerKind.Manual, 2d, null), 16);
        current.AddGlobalSample(Global(2d, 1d, 1d, 2d));

        var snapshot = UiSnapshotBuilder.Build(new UiSnapshotInput
        {
            Captures = new[] { completed },
            CurrentCapture = current,
            CaptureState = CaptureState.DeepCapture
        });

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Capture.CompletedCount, Is.EqualTo(1));
            Assert.That(snapshot.Captures.Select(capture => capture.Id), Is.EqualTo(new[] { "completed" }));
            Assert.That(snapshot.Captures.Any(capture => capture.Id == "current"), Is.False);
        });
    }

    [Test]
    public void Selected_completed_capture_drives_systems_and_timeline_without_mixing_other_captures()
    {
        var first = new CaptureSession("first", new CaptureTrigger(CaptureTriggerKind.Manual, 10d, null), 16);
        first.AddGlobalSample(Global(10d, 4d, 3d, 10d));
        var firstTiming = new SystemTimingSnapshot();
        firstTiming.AddSystem("First.System", 1.5d, MetricConfidence.Full, "First.Mod", sourceKind: SystemSourceKind.Mod);
        first.SetSystemTiming(firstTiming);

        var second = new CaptureSession("second", new CaptureTrigger(CaptureTriggerKind.Manual, 20d, null), 16);
        second.AddGlobalSample(Global(20d, 4d, 2d, 20d));
        var secondTiming = new SystemTimingSnapshot();
        secondTiming.AddSystem("Second.System", 9d, MetricConfidence.Full, "Second.Mod", sourceKind: SystemSourceKind.Mod);
        second.SetSystemTiming(secondTiming);

        var snapshot = UiSnapshotBuilder.Build(new UiSnapshotInput
        {
            Captures = new[] { first, second },
            SelectedCaptureId = "first"
        });

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Systems.Select(row => row.Id), Is.EqualTo(new[] { "First.System" }));
            Assert.That(snapshot.Timeline.Any(point => point.Metric == "system:First.System"), Is.True);
            Assert.That(snapshot.Timeline.Any(point => point.Metric == "system:Second.System"), Is.False);
            Assert.That(snapshot.Timeline.Any(point => point.TimestampSeconds == 20d), Is.False);
        });
    }

    [Test]
    public void Capture_overhead_keeps_the_maximum_observed_share()
    {
        var capture = new CaptureSession("capture-2", new CaptureTrigger(CaptureTriggerKind.Manual, 1d, null), 8);
        capture.ObserveProfilerOverheadShare(0.07d);
        capture.ObserveProfilerOverheadShare(0.02d);

        Assert.That(capture.MaxProfilerOverheadShare, Is.EqualTo(0.07d).Within(0.0001));
    }

    private static GlobalMetricsSnapshot Global(double timestamp, double selected, double actual, double mainThread)
    {
        return new GlobalMetricsSnapshot(timestamp, selected, actual, new Dictionary<string, RecorderReading>
        {
            ["CPU\u001fMain Thread"] = new RecorderReading(mainThread, 1)
        });
    }
}
