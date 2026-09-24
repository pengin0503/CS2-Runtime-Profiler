using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2RuntimeProfiler.Core
{
    public sealed class GlobalSnapshotHistory
    {
        private readonly int _capacity;
        private readonly Queue<GlobalMetricsSnapshot> _samples;

        public GlobalSnapshotHistory(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            _capacity = capacity;
            _samples = new Queue<GlobalMetricsSnapshot>(capacity);
        }

        public int Count => _samples.Count;

        public void Add(GlobalMetricsSnapshot sample)
        {
            if (sample == null)
                return;

            while (_samples.Count >= _capacity)
                _samples.Dequeue();

            _samples.Enqueue(sample);
        }

        public IReadOnlyList<GlobalMetricsSnapshot> Snapshot()
            => _samples.ToArray();

        public IReadOnlyList<GlobalMetricsSnapshot> Recent(double nowSeconds, double windowSeconds)
        {
            if (windowSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(windowSeconds));

            var lowerBound = nowSeconds - windowSeconds;
            return _samples
                .Where(sample => sample.TimestampSeconds >= lowerBound && sample.TimestampSeconds <= nowSeconds)
                .ToArray();
        }
    }
}
