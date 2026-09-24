namespace CS2RuntimeProfiler.Core
{
    public readonly struct MetricSample
    {
        public MetricSample(double timestampSeconds, double value, MetricConfidence confidence)
        {
            TimestampSeconds = timestampSeconds;
            Value = value;
            Confidence = confidence;
        }

        public double TimestampSeconds { get; }
        public double Value { get; }
        public MetricConfidence Confidence { get; }
    }
}
