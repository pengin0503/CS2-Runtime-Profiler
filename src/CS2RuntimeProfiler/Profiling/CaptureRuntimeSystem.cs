using System;
using System.Collections.Generic;
using CS2RuntimeProfiler.Collectors;
using CS2RuntimeProfiler.Core;
using Game;

namespace CS2RuntimeProfiler.Profiling
{
    /// <summary>
    /// Drives DeepCaptureController only when a new global sample is available.
    /// Keeps capture orchestration out of the UI system and restores Normal recorders after Deep Capture.
    /// </summary>
    public partial class CaptureRuntimeSystem : GameSystemBase
    {
        private const double PrebufferSeconds = 5d;
        private GlobalMetricsCollector _global;
        private DeepCaptureController _controller;
        private ProfilerOverheadTracker _overhead;
        private double _lastObservedTimestamp = double.NegativeInfinity;
        private double _lastOverheadShare;

        public CaptureState State => _controller?.State ?? CaptureState.Monitoring;
        public CaptureSession CurrentSession => _controller?.CurrentSession;
        public IReadOnlyList<CaptureSession> CompletedSessions => _controller?.CompletedSessions ?? Array.Empty<CaptureSession>();
        public double LastOverheadShare => _lastOverheadShare;
        public int CurrentBatchSize => _controller?.CurrentBatchSize ?? 0;
        public int SamplingStride => _controller?.SamplingStride ?? 1;

        protected override void OnCreate()
        {
            base.OnCreate();
            _global = World.GetOrCreateSystemManaged<GlobalMetricsCollector>();
            _overhead = new ProfilerOverheadTracker();
            _controller = new DeepCaptureController(
                _global.Recorders,
                DeepCaptureStateMachine.CreateDefault(),
                maxConcurrent: 150,
                overheadCeiling: 0.08);
            _controller.Initialize();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings != null && !Mod.Settings.EnableMonitoring)
                return;

            var latest = _global?.Latest;
            if (latest == null || latest.TimestampSeconds <= _lastObservedTimestamp)
                return;

            var before = _controller.State;
            var prebuffer = _global.GetRecentHistory(PrebufferSeconds);
            _overhead.Measure(latest.TimestampSeconds, () =>
                _controller.Observe(latest.TimestampSeconds, latest, prebuffer));

            _lastOverheadShare = Math.Max(
                0d,
                _overhead.LastMilliseconds / (GlobalMetricsCollector.SamplingPeriodSeconds * 1000d));
            _controller.CurrentSession?.ObserveProfilerOverheadShare(_lastOverheadShare);
            _controller.ReportProfilerOverheadShare(_lastOverheadShare);

            if (before == CaptureState.DeepCapture && _controller.State != CaptureState.DeepCapture)
                _global.RestoreNormalRecorders();

            _lastObservedTimestamp = latest.TimestampSeconds;
        }

        public void RequestManualCapture()
        {
            var latest = _global?.Latest;
            var now = latest?.TimestampSeconds ?? Math.Max(0d, _lastObservedTimestamp);
            _controller?.RequestManualCapture(now, _global?.GetRecentHistory(PrebufferSeconds));
        }
    }
}
