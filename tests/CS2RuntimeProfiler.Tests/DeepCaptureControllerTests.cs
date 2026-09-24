using CS2RuntimeProfiler.Core;
using CS2RuntimeProfiler.Profiling;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class DeepCaptureControllerTests
{
    [Test]
    public void Observe_finalizes_when_time_jump_skips_over_postbuffer()
    {
        var backend = new FakeBackend(new RecorderDescriptor("cpu", "CPU", "Main Thread", "TimeNanoseconds", "Int64"));
        using var manager = new RecorderManager(backend);
        var stateMachine = CreateStateMachine();
        using var controller = new DeepCaptureController(manager, stateMachine, maxConcurrent: 8);
        controller.Initialize();

        controller.RequestManualCapture(0);
        Assert.That(controller.CurrentSession, Is.Not.Null);

        controller.Observe(3, global: null);

        Assert.Multiple(() =>
        {
            Assert.That(controller.State, Is.EqualTo(CaptureState.Cooldown));
            Assert.That(controller.CurrentSession, Is.Null);
            Assert.That(controller.CompletedSessions.Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void Overhead_outside_a_capture_does_not_degrade_batching()
    {
        using var controller = CreateController(maxConcurrent: 8);

        controller.ReportProfilerOverheadShare(0.50);
        controller.ReportProfilerOverheadShare(0.50);
        controller.ReportProfilerOverheadShare(0.50);

        Assert.Multiple(() =>
        {
            Assert.That(controller.CurrentBatchSize, Is.EqualTo(8));
            Assert.That(controller.SamplingStride, Is.EqualTo(1));
        });
    }

    [Test]
    public void Single_overhead_spike_does_not_degrade_active_capture()
    {
        using var controller = CreateController(maxConcurrent: 8);
        controller.RequestManualCapture(0);

        controller.ReportProfilerOverheadShare(0.50);

        Assert.That(controller.CurrentBatchSize, Is.EqualTo(8));
    }

    [Test]
    public void Consecutive_overhead_spikes_degrade_active_capture_once()
    {
        using var controller = CreateController(maxConcurrent: 8);
        controller.RequestManualCapture(0);

        controller.ReportProfilerOverheadShare(0.50);
        controller.ReportProfilerOverheadShare(0.50);
        controller.ReportProfilerOverheadShare(0.50);

        Assert.That(controller.CurrentBatchSize, Is.EqualTo(4));
    }

    [Test]
    public void New_capture_restores_configured_batch_size_and_sampling_stride()
    {
        using var controller = CreateController(maxConcurrent: 8);
        controller.RequestManualCapture(0);

        for (var i = 0; i < 12; i++)
            controller.ReportProfilerOverheadShare(0.50);

        Assert.That(controller.CurrentBatchSize, Is.LessThan(8));
        controller.Observe(3, global: null);
        controller.RequestManualCapture(4);

        Assert.Multiple(() =>
        {
            Assert.That(controller.CurrentSession, Is.Not.Null);
            Assert.That(controller.CurrentBatchSize, Is.EqualTo(8));
            Assert.That(controller.SamplingStride, Is.EqualTo(1));
        });
    }

    private static DeepCaptureController CreateController(int maxConcurrent)
    {
        var descriptors = Enumerable.Range(0, maxConcurrent)
            .Select(index => new RecorderDescriptor($"marker-{index}", "CPU", $"Marker {index}", "TimeNanoseconds", "Int64"))
            .ToArray();
        var manager = new RecorderManager(new FakeBackend(descriptors));
        var controller = new DeepCaptureController(manager, CreateStateMachine(), maxConcurrent: maxConcurrent, overheadCeiling: 0.08);
        controller.Initialize();
        return controller;
    }

    private static DeepCaptureStateMachine CreateStateMachine() => new(
        efficiencyThreshold: 0.8,
        sustainSeconds: 2,
        deepSeconds: 1,
        postSeconds: 1,
        cooldownSeconds: 100);

    private sealed class FakeBackend : IRecorderBackend
    {
        private readonly IReadOnlyList<RecorderDescriptor> _descriptors;

        public FakeBackend(params RecorderDescriptor[] descriptors) => _descriptors = descriptors;

        public IReadOnlyList<RecorderDescriptor> Discover() => _descriptors;

        public IActiveRecorder Start(RecorderDescriptor descriptor, int capacity) => new FakeRecorder(descriptor.Id);
    }

    private sealed class FakeRecorder : IActiveRecorder
    {
        public FakeRecorder(string id) => Id = id;
        public string Id { get; }
        public RecorderReading Read() => new(1.0, 1);
        public void Dispose() { }
    }
}
