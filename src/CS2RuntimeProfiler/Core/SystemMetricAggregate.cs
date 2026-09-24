using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2RuntimeProfiler.Core
{
    public sealed class SystemMetricAggregate
    {
        private SystemMetricAggregate(string systemId, MetricStatistics statistics, MetricConfidence confidence)
        {
            SystemId = systemId ?? string.Empty;
            Statistics = statistics;
            Confidence = confidence;
        }

        public string SystemId { get; }
        public MetricStatistics Statistics { get; }
        public MetricConfidence Confidence { get; }
        public double CurrentMilliseconds => Statistics.Current;
        public double MeanMilliseconds => Statistics.Mean;
        public double MedianMilliseconds => Statistics.Median;
        public double P95Milliseconds => Statistics.P95;
        public double P99Milliseconds => Statistics.P99;
        public double MaxMilliseconds => Statistics.Max;
        public double TotalMilliseconds => Statistics.Total;
        public int Calls => Statistics.Count;

        public static SystemMetricAggregate FromManagedSamples(string systemId, IEnumerable<double> milliseconds)
        {
            if (milliseconds == null)
                throw new ArgumentNullException(nameof(milliseconds));
            var values = milliseconds.Where(value => value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value)).ToArray();
            return new SystemMetricAggregate(systemId, MetricStatistics.From(values), MetricConfidence.Managed);
        }

        public static SystemMetricAggregate FromSamples(string systemId, IEnumerable<double> milliseconds, MetricConfidence confidence)
        {
            if (milliseconds == null)
                throw new ArgumentNullException(nameof(milliseconds));
            var values = milliseconds.Where(value => value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value)).ToArray();
            return new SystemMetricAggregate(systemId, MetricStatistics.From(values), confidence);
        }
    }
}
