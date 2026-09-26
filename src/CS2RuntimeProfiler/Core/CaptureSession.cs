using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2RuntimeProfiler.Core
{
    public readonly struct MarkerCoverageInfo
    {
        public MarkerCoverageInfo(int discovered, int captured, bool isBatched)
        {
            Discovered = Math.Max(0, discovered);
            Captured = Math.Max(0, Math.Min(captured, Discovered));
            IsBatched = isBatched;
        }

        public int Discovered { get; }
        public int Captured { get; }
        public bool IsBatched { get; }
        public double? Ratio => Discovered == 0 ? (double?)null : (double)Captured / Discovered;
    }

    public sealed class CaptureSession
    {
        private readonly int _maxSamplesPerSeries;
        private readonly Dictionary<string, RollingMetricSeries> _markerSamples = new Dictionary<string, RollingMetricSeries>(StringComparer.Ordinal);
        private readonly List<string> _warnings = new List<string>();
        private readonly List<GlobalMetricsSnapshot> _globalSamples = new List<GlobalMetricsSnapshot>();

        public CaptureSession(string id, CaptureTrigger trigger, int maxSamplesPerSeries)
        {
            if (maxSamplesPerSeries < 1)
                throw new ArgumentOutOfRangeException(nameof(maxSamplesPerSeries));

            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id;
            Trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
            _maxSamplesPerSeries = maxSamplesPerSeries;
            MarkerCoverage = new MarkerCoverageInfo(0, 0, false);
        }

        public string Id { get; }
        public CaptureTrigger Trigger { get; }
        public MarkerCoverageInfo MarkerCoverage { get; private set; }
        public SystemTimingSnapshot SystemTiming { get; private set; }
        public double MaxProfilerOverheadShare { get; private set; }
        public IReadOnlyList<string> Warnings => _warnings;
        public IReadOnlyList<GlobalMetricsSnapshot> GlobalSamples => _globalSamples;
        public IReadOnlyDictionary<string, IReadOnlyList<MetricSample>> MarkerSamples =>
            _markerSamples.ToDictionary(pair => pair.Key, pair => pair.Value.Snapshot(), StringComparer.Ordinal);

        public bool TryGetMarkerSamples(string markerId, out IReadOnlyList<MetricSample> samples)
        {
            if (!string.IsNullOrWhiteSpace(markerId) && _markerSamples.TryGetValue(markerId, out var series))
            {
                samples = series.Snapshot();
                return true;
            }

            samples = Array.Empty<MetricSample>();
            return false;
        }

        public void SetMarkerCoverage(int discovered, int captured, bool isBatched)
        {
            MarkerCoverage = new MarkerCoverageInfo(discovered, captured, isBatched);
        }

        public void SetSystemTiming(SystemTimingSnapshot snapshot)
        {
            SystemTiming = snapshot;
        }

        public void ObserveProfilerOverheadShare(double share)
        {
            if (double.IsNaN(share) || double.IsInfinity(share) || share < 0d)
                return;
            MaxProfilerOverheadShare = Math.Max(MaxProfilerOverheadShare, share);
        }

        public void AddWarning(string warning)
        {
            if (!string.IsNullOrWhiteSpace(warning))
                _warnings.Add(warning.Trim());
        }

        public void AddGlobalSample(GlobalMetricsSnapshot sample)
        {
            if (sample == null)
                return;

            if (_globalSamples.Count > 0
                && _globalSamples[_globalSamples.Count - 1].TimestampSeconds.Equals(sample.TimestampSeconds))
                return;

            _globalSamples.Add(sample);
            var maxGlobalSamples = Math.Max(32, _maxSamplesPerSeries * 2);
            if (_globalSamples.Count > maxGlobalSamples)
                _globalSamples.RemoveRange(0, _globalSamples.Count - maxGlobalSamples);
        }

        public void AddMarkerSample(string markerId, MetricSample sample)
        {
            if (string.IsNullOrWhiteSpace(markerId))
                return;
            if (!_markerSamples.TryGetValue(markerId, out var series))
            {
                series = new RollingMetricSeries(_maxSamplesPerSeries);
                _markerSamples[markerId] = series;
            }
            series.Add(sample);
        }
    }
}
