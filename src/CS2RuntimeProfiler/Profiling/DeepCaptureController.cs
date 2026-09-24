using System;
using System.Collections.Generic;
using System.Linq;
using CS2RuntimeProfiler.Core;

namespace CS2RuntimeProfiler.Profiling
{
    public sealed class DeepCaptureController : IDisposable
    {
        private readonly RecorderManager _recorders;
        private readonly DeepCaptureStateMachine _stateMachine;
        private readonly List<CaptureSession> _completed = new List<CaptureSession>();
        private readonly HashSet<string> _capturedMarkerIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly double _overheadCeiling;
        private int _maxConcurrent;
        private int _currentBatchIndex = -1;
        private int _sampleStride = 1;
        private int _sampleCounter;
        private double _batchStartedAt;
        private CaptureState _lastState;
        private MarkerBatchPlan _plan;

        public DeepCaptureController(
            RecorderManager recorders,
            DeepCaptureStateMachine stateMachine,
            int maxConcurrent = 150,
            double overheadCeiling = 0.08)
        {
            _recorders = recorders ?? throw new ArgumentNullException(nameof(recorders));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _maxConcurrent = Math.Max(1, maxConcurrent);
            _overheadCeiling = Math.Max(0.001, overheadCeiling);
            _lastState = _stateMachine.State;
        }

        public CaptureState State => _stateMachine.State;
        public CaptureSession CurrentSession { get; private set; }
        public IReadOnlyList<CaptureSession> CompletedSessions => _completed;
        public int CurrentBatchSize => _maxConcurrent;
        public int SamplingStride => _sampleStride;

        public void Initialize()
        {
            _recorders.DiscoverAvailableMarkers();
            _plan = MarkerBatchPlanner.Create(_recorders.Descriptors.Select(d => d.Id), _maxConcurrent);
        }

        public void RequestManualCapture(double nowSeconds, IEnumerable<GlobalMetricsSnapshot> prebuffer = null)
        {
            var before = _stateMachine.State;
            _stateMachine.RequestManualCapture(nowSeconds);
            if (before != CaptureState.DeepCapture && _stateMachine.State == CaptureState.DeepCapture)
                BeginCapture(nowSeconds, prebuffer);
            _lastState = _stateMachine.State;
        }

        public void Observe(double nowSeconds, GlobalMetricsSnapshot global, IEnumerable<GlobalMetricsSnapshot> prebuffer = null)
        {
            var before = _stateMachine.State;
            _stateMachine.Observe(nowSeconds, global?.SelectedSpeed ?? 0, global?.ActualSpeed ?? 0);
            var after = _stateMachine.State;

            if (before != CaptureState.DeepCapture && after == CaptureState.DeepCapture)
                BeginCapture(nowSeconds, prebuffer);

            if (after == CaptureState.DeepCapture && CurrentSession != null)
            {
                if (global != null)
                    CurrentSession.AddGlobalSample(global);
                CaptureDeepSample(nowSeconds);
            }

            if (before == CaptureState.DeepCapture && after != CaptureState.DeepCapture)
                _recorders.DeactivateAll();

            if (before == CaptureState.PostBuffer && after == CaptureState.Cooldown && CurrentSession != null)
                FinalizeCapture();

            if (after == CaptureState.PostBuffer && CurrentSession != null && global != null)
                CurrentSession.AddGlobalSample(global);

            _lastState = after;
        }

        public void ReportProfilerOverheadShare(double share)
        {
            if (share <= _overheadCeiling)
                return;

            if (_maxConcurrent > 1)
            {
                _maxConcurrent = Math.Max(1, _maxConcurrent / 2);
                _plan = MarkerBatchPlanner.Create(_recorders.Descriptors.Select(d => d.Id), _maxConcurrent);
                CurrentSession?.AddWarning($"Profiler overhead exceeded {_overheadCeiling:P0}; marker batching reduced to {_maxConcurrent} concurrent recorders.");
            }
            else
            {
                _sampleStride = Math.Min(16, _sampleStride * 2);
                CurrentSession?.AddWarning($"Profiler overhead remains high; sampling stride increased to {_sampleStride}.");
            }
        }

        public void Dispose() => _recorders.Dispose();

        private void BeginCapture(double nowSeconds, IEnumerable<GlobalMetricsSnapshot> prebuffer)
        {
            if (_plan == null)
                Initialize();

            CurrentSession = new CaptureSession(
                $"capture-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}",
                _stateMachine.LastTrigger ?? new CaptureTrigger(CaptureTriggerKind.Manual, nowSeconds, null),
                maxSamplesPerSeries: 4096);

            if (prebuffer != null)
            {
                foreach (var sample in prebuffer)
                    CurrentSession.AddGlobalSample(sample);
            }

            _capturedMarkerIds.Clear();
            _currentBatchIndex = -1;
            _sampleCounter = 0;
            ActivateNextBatch(nowSeconds);
        }

        private void CaptureDeepSample(double nowSeconds)
        {
            if (_plan == null || _plan.Batches.Count == 0)
                return;

            if (nowSeconds - _batchStartedAt >= 1d)
                ActivateNextBatch(nowSeconds);

            _sampleCounter++;
            if (_sampleCounter % _sampleStride != 0)
                return;

            foreach (var pair in _recorders.SampleActive())
            {
                _capturedMarkerIds.Add(pair.Key);
                CurrentSession.AddMarkerSample(pair.Key, new MetricSample(nowSeconds, pair.Value.Value, MetricConfidence.Full));
            }

            CurrentSession.SetMarkerCoverage(_plan.DiscoveredCount, _capturedMarkerIds.Count, _plan.IsBatched);
        }

        private void ActivateNextBatch(double nowSeconds)
        {
            _recorders.DeactivateAll();
            if (_plan == null || _plan.Batches.Count == 0)
                return;

            _currentBatchIndex = (_currentBatchIndex + 1) % _plan.Batches.Count;
            foreach (var id in _plan.Batches[_currentBatchIndex])
            {
                if (_recorders.TryActivate(id, 8, out var error))
                    continue;
                CurrentSession?.AddWarning($"Recorder activation failed for '{id}': {error}");
            }
            _batchStartedAt = nowSeconds;
        }

        private void FinalizeCapture()
        {
            CurrentSession.SetMarkerCoverage(_plan?.DiscoveredCount ?? 0, _capturedMarkerIds.Count, _plan?.IsBatched ?? false);
            _completed.Add(CurrentSession);
            CurrentSession = null;
        }
    }
}
