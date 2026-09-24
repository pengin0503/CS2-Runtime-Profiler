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
        var stateMachine = new DeepCaptureStateMachine(
            efficiencyThreshold: 0.8,
            sustainSeconds: 2,
            deepSeconds: 1,
            postSeconds: 1,
            cooldownSeconds: 100);
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
