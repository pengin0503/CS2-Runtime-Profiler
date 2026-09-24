using CS2RuntimeProfiler.Core;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class SystemAggregationTests
{
    [Test]
    public void Managed_only_system_does_not_claim_full_job_cost()
    {
        var result = SystemMetricAggregate.FromManagedSamples("ExampleSystem", new[] { 0.3, 0.5, 0.4 });

        Assert.That(result.Confidence, Is.EqualTo(MetricConfidence.Managed));
        Assert.That(result.MeanMilliseconds, Is.EqualTo(0.4).Within(0.001));
        Assert.That(result.TotalMilliseconds, Is.EqualTo(1.2).Within(0.001));
        Assert.That(result.Calls, Is.EqualTo(3));
    }

    [Test]
    public void Unattributed_worker_time_remains_separate()
    {
        var snapshot = new SystemTimingSnapshot();
        snapshot.AddSystem("ExampleSystem", 1.0, MetricConfidence.Managed);
        snapshot.SetUnattributedJobsMilliseconds(7.8);

        Assert.That(snapshot.UnattributedJobsMilliseconds, Is.EqualTo(7.8));
        Assert.That(snapshot.Systems.Single().Milliseconds, Is.EqualTo(1.0));
    }

    [Test]
    public void Direct_mod_total_only_counts_systems_owned_by_that_assembly()
    {
        var snapshot = new SystemTimingSnapshot();
        snapshot.AddSystem("ModSystem", 2.0, MetricConfidence.Full, ownerAssembly: "Example.Mod");
        snapshot.AddSystem("VanillaPatched", 5.0, MetricConfidence.Full, ownerAssembly: "Game", patchOwners: new[] { "Example.Mod" });

        Assert.That(snapshot.GetDirectAssemblyTotal("Example.Mod"), Is.EqualTo(2.0));
        Assert.That(snapshot.GetDirectAssemblyTotal("Game"), Is.EqualTo(5.0));
    }
}
