using System.Collections.Generic;
using CS2RuntimeProfiler.Core;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class CaptureCompletionTimingProcessorTests
{
    [Test]
    public void ProcessNew_finalizes_each_completed_capture_once()
    {
        var systems = new[]
        {
            new SystemDescriptor(
                "Game.Simulation.ExampleSystem",
                "Game",
                SystemSourceKind.Vanilla,
                null,
                MetricConfidence.Unavailable)
        };
        var recorders = new[]
        {
            new RecorderDescriptor(
                "ecs-system",
                "Scripts",
                "Main World Game.Simulation.ExampleSystem",
                "TimeNanoseconds",
                "Int64")
        };
        var processor = new CaptureCompletionTimingProcessor(systems, recorders);
        var completed = new List<CaptureSession>();

        var first = Capture("first", 2_000_000d);
        completed.Add(first);
        processor.ProcessNew(completed);

        Assert.That(first.SystemTiming, Is.Not.Null);
        Assert.That(first.SystemTiming.Systems[0].Milliseconds, Is.EqualTo(2d).Within(0.0001d));
        var firstTiming = first.SystemTiming;

        processor.ProcessNew(completed);
        Assert.That(first.SystemTiming, Is.SameAs(firstTiming));

        var second = Capture("second", 5_000_000d);
        completed.Add(second);
        processor.ProcessNew(completed);

        Assert.That(first.SystemTiming, Is.SameAs(firstTiming));
        Assert.That(second.SystemTiming, Is.Not.Null);
        Assert.That(second.SystemTiming.Systems[0].Milliseconds, Is.EqualTo(5d).Within(0.0001d));
        Assert.That(processor.ProcessedCount, Is.EqualTo(2));
    }

    private static CaptureSession Capture(string id, double nanoseconds)
    {
        var capture = new CaptureSession(
            id,
            new CaptureTrigger(CaptureTriggerKind.Manual, 0d, null),
            maxSamplesPerSeries: 8);
        capture.AddMarkerSample("ecs-system", new MetricSample(1d, nanoseconds, MetricConfidence.Full));
        return capture;
    }
}
