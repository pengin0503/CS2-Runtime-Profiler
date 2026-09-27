using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2RuntimeProfiler.Core
{
    public sealed class ManagedSystemTimingAccumulator
    {
        private readonly int _maxSamplesPerSystem;
        private readonly Dictionary<string, Queue<double>> _samples =
            new Dictionary<string, Queue<double>>(StringComparer.Ordinal);

        public ManagedSystemTimingAccumulator(int maxSamplesPerSystem = 512)
        {
            if (maxSamplesPerSystem < 1)
                throw new ArgumentOutOfRangeException(nameof(maxSamplesPerSystem));
            _maxSamplesPerSystem = maxSamplesPerSystem;
        }

        public int SystemCount => _samples.Count;

        public void Record(string systemId, double milliseconds)
        {
            if (string.IsNullOrWhiteSpace(systemId)
                || milliseconds < 0d
                || double.IsNaN(milliseconds)
                || double.IsInfinity(milliseconds))
            {
                return;
            }

            if (!_samples.TryGetValue(systemId, out var series))
            {
                series = new Queue<double>(_maxSamplesPerSystem);
                _samples[systemId] = series;
            }

            while (series.Count >= _maxSamplesPerSystem)
                series.Dequeue();
            series.Enqueue(milliseconds);
        }

        public void Clear() => _samples.Clear();

        public SystemTimingSnapshot BuildSnapshot(IEnumerable<SystemDescriptor> systems)
        {
            var descriptors = (systems ?? Array.Empty<SystemDescriptor>())
                .Where(system => system != null && !string.IsNullOrWhiteSpace(system.FullTypeName))
                .GroupBy(system => system.FullTypeName, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var snapshot = new SystemTimingSnapshot();

            foreach (var pair in _samples.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                var values = pair.Value.ToArray();
                if (values.Length == 0)
                    continue;

                descriptors.TryGetValue(pair.Key, out var descriptor);
                var aggregate = SystemMetricAggregate.FromSamples(
                    pair.Key,
                    values,
                    MetricConfidence.Managed,
                    calls: values.Length);
                snapshot.AddSystemAggregate(
                    aggregate,
                    descriptor?.AssemblyName ?? string.Empty,
                    descriptor?.PatchOwners?.Select(owner => owner.OwnerId) ?? Array.Empty<string>(),
                    descriptor?.SourceKind ?? SystemSourceKind.Unknown);
            }

            return snapshot;
        }
    }
}
