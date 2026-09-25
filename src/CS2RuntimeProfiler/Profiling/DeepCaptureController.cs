using System;
using System.Collections.Generic;
using System.Linq;
using CS2RuntimeProfiler.Core;

namespace CS2RuntimeProfiler.Profiling
{
    public sealed class DeepCaptureController : IDisposable
    {
        private const int ConsecutiveOverheadBreachesBeforeDegrade = 3;

        private readonly RecorderManager _recorders;
        private readonly DeepCaptureStateMachine _stateMachine;
        private readonly List<CaptureSession> _completed = new List<CaptureSession>();
        private readonly HashSet<string> _capturedMarkerIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly double _overheadCeiling;
        private readonly int _configuredMaxConcurrent;
        private readonly int _maxCompletedSessions;
        private int _maxConcurrent;
        private int _currentBatchIndex = -1;
        private int _sampleStride = 1;
        private int _sampleCounter;
        private int _consecutiveOverheadBreaches;
        private double _batchStartedAt;
        private CaptureState _lastState;
        private MarkerBatchPlan _plan;

        public DeepCaptureController(
            RecorderManager recorders,
            DeepCaptureStateMachine stateMachine,
            int maxConcurrent = 150,
            double overheadCeiling = 0.08,
            int maxCompletedSessions = 20)
        {
            _recorders = recorders ?? throw new ArgumentNullException(nameof(recorders));
            _stateMachine = stateMachine ?? throw new ArgumentNullException(nameof(stateMachine));
            _configuredMaxConcurrent = Math.Max(1, maxConcurrent);
            _maxConcurrent = _configuredMaxConcurrent;
            _overheadCeiling = Math.Max(0.001, overheadCeiling);
            _maxCompletedSessions = Math.Max(1, maxCompletedSessions);
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

            if (after == CaptureState.PostBuffer && CurrentSession != null && global != null)
                CurrentSession.AddGlobalSample(global);

            if (CurrentSession != null && after != CaptureState.DeepCapture && after != CaptureState.PostBuffer)
                FinalizeCapture();

            _lastState = after;
        }

        public void InterruptActiveCapture(string warning)
        {
            _recorders.DeactivateAll();

            if (CurrentSession != null)
            {
                CurrentSession.AddWarning(warning);
                FinalizeCapture();
            }

            _stateMachine.ResetToMonitoring();
            _capturedMarkerIds.Clear();
            _currentBatchIndex = -1;
            _sampleCounter = 0;
            _consecutiveOverheadBreaches = 0;
            _lastState = _stateMachine.State;
        }

        public void ReportProfilerOverheadShare(double share)
        {
            if (CurrentSession == null)
            {
                _consecutiveOverheadBreaches = 0;
                return;
            }

            if (double.IsNaN(share) || share < 0d || share <= _overheadCeiling)
            {
                _consecutiveOverheadBreaches = 0;
                return;
            }

            _consecutiveOverheadBreaches++;
            if (_consecutiveOverheadBreaches < ConsecutiveOverheadBreachesBeforeDegrade)
                return;

            _consecutiveOverheadBreaches = 0;
            if (_maxConcurrent > 1)
            {
                _maxConcurrent = Math.Max(1, _maxConcurrent / 2);
                _plan = MarkerBatchPlanner.Create(_recorders.Descriptors.Select(d => d.Id), _maxConcurrent);
                CurrentSession.AddWarning($"Profiler overhead exceeded {_overheadCeiling:P0} repeatedly; marker batching reduced to {_maxConcurrent} concurrent recorders.");
            }
            else
            {
                _sampleStride = Math.Min(16, _sampleStride * 2);
                CurrentSession.AddWarning($"Profiler overhead remains high; sampling stride increased to {_sampleStride}.");
            }
        }

        public void Dispose() => _recorders.Dispose();

        private void BeginCapture(double nowSeconds, IEnumerable<GlobalMetricsSnapshot> prebuffer)
        {
            if (_plan == null)
                Initialize();

            _maxConcurrent = _configuredMaxConcurrent;
            _sampleStride = 1;
            _consecutiveOverheadBreaches = 0;
            _plan = MarkerBatchPlanner.Create(_recorders.Descriptors.Select(d => d.Id), _maxConcurrent);

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
                if (pair.Value.Count <= 0)
                    continue;

                _capturedMarkerIds.Add(pair.Key);
                CurrentSession.AddMarkerSample(
                    pair.Key,
                    new MetricSample(nowSeconds, pair.Value.Value, MetricConfidence.Full, pair.Value.Count));
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
            if (_completed.Count > _maxCompletedSessions)
                _completed.RemoveAt(0);
            CurrentSession = null;
            _consecutiveOverheadBreaches = 0;
        }
    }
}
