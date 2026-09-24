namespace CS2RuntimeProfiler.Core
{
    public sealed class NamedMetricValue
    {
        private NamedMetricValue(string id, double? value, MetricAvailability availability, MetricConfidence confidence, string reason)
        {
            Id = id ?? string.Empty;
            Value = value;
            Availability = availability;
            Confidence = confidence;
            Reason = reason;
        }

        public string Id { get; }
        public double? Value { get; }
        public MetricAvailability Availability { get; }
        public MetricConfidence Confidence { get; }
        public string Reason { get; }

        public static NamedMetricValue Available(string id, double value, MetricConfidence confidence)
            => new NamedMetricValue(id, value, MetricAvailability.Available, confidence, null);

        public static NamedMetricValue Unavailable(string id, string reason)
            => new NamedMetricValue(id, null, MetricAvailability.Unavailable, MetricConfidence.Unavailable, reason);
    }
}
