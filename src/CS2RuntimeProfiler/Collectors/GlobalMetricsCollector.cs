using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CS2RuntimeProfiler.Core;
using CS2RuntimeProfiler.Profiling;
using Game;
using Game.Simulation;

namespace CS2RuntimeProfiler.Collectors
{
    public partial class GlobalMetricsCollector : GameSystemBase, IMetricCollector
    {
        public const double SamplingPeriodSeconds = 0.5;
        private const int HistoryCapacity = 60;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private SimulationSystem _simulationSystem;
        private RecorderManager _recorderManager;
        private ProfilerOverheadTracker _overhead;
        private GlobalSnapshotHistory _history;
        private IReadOnlyDictionary<string, string> _recorderUnits = new Dictionary<string, string>();
        private double _nextSampleAt;

        public string Name => "Global";
        public GlobalMetricsSnapshot Latest { get; private set; }
        public ProfilerOverheadTracker Overhead => _overhead;
        public RecorderManager Recorders => _recorderManager;

        protected override void OnCreate()
        {
            base.OnCreate();
            _simulationSystem = World.GetOrCreateSystemManaged<SimulationSystem>();
            _recorderManager = new RecorderManager(new UnityRecorderBackend());
            _overhead = new ProfilerOverheadTracker();
            _history = new GlobalSnapshotHistory(HistoryCapacity);
            _recorderManager.DiscoverAvailableMarkers();
            _recorderUnits = _recorderManager.Descriptors.ToDictionary(descriptor => descriptor.Id, descriptor => descriptor.UnitType);
            RestoreNormalRecorders();
        }

        protected override void OnUpdate()
        {
            if (Mod.Settings != null && !Mod.Settings.EnableMonitoring)
                return;

            var now = _clock.Elapsed.TotalSeconds;
            if (now < _nextSampleAt)
                return;

            _nextSampleAt = now + SamplingPeriodSeconds;
            _overhead.Measure(now, () => Sample(now));
        }

        public void Sample(double timestampSeconds)
        {
            Latest = new GlobalMetricsSnapshot(
                timestampSeconds,
                _simulationSystem.selectedSpeed,
                _simulationSystem.smoothSpeed,
                _recorderManager.SampleActive(),
                _recorderUnits);
            _history?.Add(Latest);
        }

        public IReadOnlyList<GlobalMetricsSnapshot> GetRecentHistory(double windowSeconds = 5d)
        {
            if (_history == null || Latest == null)
                return System.Array.Empty<GlobalMetricsSnapshot>();
            return _history.Recent(Latest.TimestampSeconds, windowSeconds);
        }

        public void RestoreNormalRecorders()
        {
            if (_recorderManager == null)
                return;

            _recorderManager.DeactivateAll();
            ActivatePreferred(8, "Main Thread");
            ActivatePreferred(8, "Render Thread");
            ActivatePreferred(8, "GPU Frame Time", "GPU Time");
            ActivatePreferred(8, "Total Used Memory", "System Used Memory");
        }

        protected override void OnDestroy()
        {
            _recorderManager?.Dispose();
            base.OnDestroy();
        }

        private void ActivatePreferred(int capacity, params string[] names)
        {
            var descriptor = _recorderManager.FindFirstByName(names);
            if (descriptor != null)
                _recorderManager.TryActivate(descriptor.Id, capacity, out _);
        }
    }
}
