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
