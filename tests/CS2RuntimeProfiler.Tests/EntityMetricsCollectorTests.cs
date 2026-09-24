using System.Collections.Generic;
using CS2RuntimeProfiler.Collectors;
using CS2RuntimeProfiler.Core;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class EntityMetricsCollectorTests
{
    private sealed class FakeSource : IEntityDomainCountSource
    {
        public readonly Dictionary<string, int> Counts = new();
        public int Calls { get; private set; }

        public bool TryGetCount(string componentTypeName, out int count, out string reason)
        {
            Calls++;
            if (Counts.TryGetValue(componentTypeName, out count))
            {
                reason = null;
                return true;
            }

            count = 0;
            reason = $"unsupported: {componentTypeName}";
            return false;
        }
    }

    [Test]
    public void Known_domains_are_indirect_counts_and_sampling_is_throttled()
    {
        var source = new FakeSource();
        source.Counts["Game.Citizens.Citizen"] = 1234;
        source.Counts["Game.Citizens.Household"] = 456;
        var collector = new EntityMetricsCollector(source, samplePeriodSeconds: 2);

        collector.Sample(0);
        var callsAfterFirstSample = source.Calls;
        collector.Sample(1);

        Assert.That(collector.Latest.Get("citizens").Value, Is.EqualTo(1234));
        Assert.That(collector.Latest.Get("citizens").Confidence, Is.EqualTo(MetricConfidence.Indirect));
        Assert.That(collector.Latest.Get("households").Value, Is.EqualTo(456));
        Assert.That(source.Calls, Is.EqualTo(callsAfterFirstSample));

        collector.Sample(2);
        Assert.That(source.Calls, Is.GreaterThan(callsAfterFirstSample));
    }

    [Test]
    public void Unsupported_domain_remains_unavailable_not_zero()
    {
        var collector = new EntityMetricsCollector(new FakeSource(), samplePeriodSeconds: 2);
        collector.Sample(0);

        var service = collector.Latest.Get("serviceVehicles");
        Assert.That(service.Availability, Is.EqualTo(MetricAvailability.Unavailable));
        Assert.That(service.Value, Is.Null);
        Assert.That(service.Reason, Does.Contain("unsupported"));
    }
}
