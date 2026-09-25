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
        private const double DefaultPrebufferSeconds = 5d;
        private readonly MonitoringLifecycleGate _monitoringGate = new MonitoringLifecycleGate(initiallyEnabled: true);
        private GlobalMetricsCollector _global;
        private DeepCaptureStateMachine _stateMachine;
        private DeepCaptureController _controller;
        private ProfilerOverheadTracker _overhead;
        private CaptureCompletionTimingProcessor _completionTiming;
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
            _stateMachine = DeepCaptureStateMachine.CreateDefault();
            _controller = new DeepCaptureController(
                _global.Recorders,
                _stateMachine,
                maxConcurrent: 150,
                overheadCeiling: 0.08);
            _controller.Initialize();
            ApplyRuntimeSettings();

            try
            {
                var systems = new ProfilerCatalog().Discover();
                _completionTiming = new CaptureCompletionTimingProcessor(
                    systems,
                    () => _global?.Recorders?.Descriptors ?? Array.Empty<RecorderDescriptor>());
            }
            catch (Exception ex)
            {
                // Discovery is lifecycle-gated and fail-open. An unavailable catalog must not prevent
                // global monitoring/capture from running; per-system timing simply remains unavailable.
                Mod.Log.Error(ex, "System catalog discovery failed; per-system timing will be unavailable");
                _completionTiming = null;
            }
        }

        protected override void OnUpdate()
        {
            ApplyRuntimeSettings();

            var monitoringEnabled = Mod.Settings == null || Mod.Settings.EnableMonitoring;
            var transition = _monitoringGate.Observe(monitoringEnabled);

            if (transition == MonitoringTransition.Disabled)
            {
                _controller?.InterruptActiveCapture(
                    "Monitoring was disabled; the active capture was finalized early and recorder activity was stopped.");
                ProjectCompletedCaptureTiming();
            }

            if (!monitoringEnabled)
                return;

            var latest = _global?.Latest;
            if (latest == null || latest.TimestampSeconds <= _lastObservedTimestamp)
                return;

            var before = _controller.State;
            var prebuffer = _global.GetRecentHistory(GetPrebufferSeconds());
            _overhead.Measure(latest.TimestampSeconds, () =>
            {
                _controller.Observe(latest.TimestampSeconds, latest, prebuffer);
                ProjectCompletedCaptureTiming();
            });

            var samplingPeriod = Math.Max(0.001d, _global?.SamplingPeriodSeconds ?? GlobalMetricsCollector.DefaultSamplingPeriodSeconds);
            _lastOverheadShare = Math.Max(
                0d,
                _overhead.LastMilliseconds / (samplingPeriod * 1000d));
            _controller.CurrentSession?.ObserveProfilerOverheadShare(_lastOverheadShare);
            _controller.ReportProfilerOverheadShare(_lastOverheadShare);

            if (before == CaptureState.DeepCapture && _controller.State != CaptureState.DeepCapture)
                _global.RestoreNormalRecorders();

            _lastObservedTimestamp = latest.TimestampSeconds;
        }

        public void RequestManualCapture()
        {
            if (Mod.Settings != null && !Mod.Settings.EnableMonitoring)
                return;

            ApplyRuntimeSettings();
            var latest = _global?.Latest;
            var now = latest?.TimestampSeconds ?? Math.Max(0d, _lastObservedTimestamp);
            _controller?.RequestManualCapture(now, _global?.GetRecentHistory(GetPrebufferSeconds()));
        }

        private double GetPrebufferSeconds()
        {
            return Mod.Settings?.ResolvedPrebufferSeconds ?? DefaultPrebufferSeconds;
        }

        private void ApplyRuntimeSettings()
        {
            var settings = Mod.Settings;
            if (settings == null || _stateMachine == null || _controller == null)
                return;

            _stateMachine.Configure(
                settings.ResolvedEfficiencyThreshold,
                settings.ResolvedLowEfficiencySustainSeconds,
                settings.ResolvedDeepCaptureSeconds,
                settings.ResolvedPostbufferSeconds,
                settings.ResolvedCooldownSeconds,
                settings.EnableAutomaticCapture);

            _controller.UpdateConfiguration(
                settings.ResolvedMaxConcurrentMarkers,
                settings.ResolvedProfilerOverheadLimit,
                settings.ResolvedMaxCompletedCaptures);
        }

        private void ProjectCompletedCaptureTiming()
        {
            if (_completionTiming == null || _controller == null)
                return;

            try
            {
                _completionTiming.ProcessNew(_controller.CompletedSessions);
            }
            catch (Exception ex)
            {
                _completionTiming.LastProcessedCapture?.AddWarning(
                    "System timing projection failed for this capture; per-system timing is unavailable.");

                // Keep capture/global monitoring alive even if one timing projection fails.
                Mod.Log.Error(ex, "System timing projection failed for a completed capture");
            }
        }
    }
}
