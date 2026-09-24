using System.Collections.Generic;
using CS2RuntimeProfiler.Collectors;
using CS2RuntimeProfiler.Core;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class PathfindingCollectorTests
{
    private sealed class FakeActionList
    {
        public readonly List<int> m_Items = new();
        public int m_NextIndex;
    }

    private sealed class FakePathfindQueueSystem
    {
        private readonly FakeActionList m_PathfindActions = new();
        private readonly Queue<int> m_ActionTypes = new();
        private readonly Queue<int> m_WorkerActions = new();

        public FakePathfindQueueSystem(int itemCount, int nextIndex, int actionTypes, int workers)
        {
            for (var i = 0; i < itemCount; i++) m_PathfindActions.m_Items.Add(i);
            m_PathfindActions.m_NextIndex = nextIndex;
            for (var i = 0; i < actionTypes; i++) m_ActionTypes.Enqueue(i);
            for (var i = 0; i < workers; i++) m_WorkerActions.Enqueue(i);
        }

        public int GetGraphSize() => 321;
        public void GetGraphMemory(out uint usedMemory, out uint allocatedMemory)
        {
            usedMemory = 100;
            allocatedMemory = 200;
        }

        public void GetQueryMemory(out uint usedMemory, out uint allocatedMemory)
        {
            usedMemory = 50;
            allocatedMemory = 80;
        }
    }

    [Test]
    public void Verified_queue_structure_exposes_pending_and_memory_metrics()
    {
        var collector = new PathfindingCollector(new FakePathfindQueueSystem(10, 4, 3, 2));
        collector.Sample(1);

        Assert.That(collector.Latest.Get("pendingPathfindActions").Value, Is.EqualTo(6));
        Assert.That(collector.Latest.Get("actionTypeQueue").Value, Is.EqualTo(3));
        Assert.That(collector.Latest.Get("workerActionQueue").Value, Is.EqualTo(2));
        Assert.That(collector.Latest.Get("graphSize").Value, Is.EqualTo(321));
        Assert.That(collector.Latest.Get("graphMemoryUsed").Value, Is.EqualTo(100));
        Assert.That(collector.Latest.Get("queryMemoryAllocated").Value, Is.EqualTo(80));
    }

    [Test]
    public void Unverified_rate_counters_are_explicitly_unavailable()
    {
        var collector = new PathfindingCollector(new FakePathfindQueueSystem(1, 0, 0, 0));
        collector.Sample(1);

        var requests = collector.Latest.Get("requestsPerSecond");
        var results = collector.Latest.Get("resultsPerSecond");
        Assert.That(requests.Availability, Is.EqualTo(MetricAvailability.Unavailable));
        Assert.That(results.Availability, Is.EqualTo(MetricAvailability.Unavailable));
        Assert.That(requests.Value, Is.Null);
        Assert.That(requests.Reason, Does.Contain("verified"));
    }

    [Test]
    public void Missing_runtime_members_fail_open_instead_of_throwing()
    {
        var collector = new PathfindingCollector(new object());
        Assert.DoesNotThrow(() => collector.Sample(1));
        Assert.That(collector.Latest.Get("pendingPathfindActions").Availability, Is.EqualTo(MetricAvailability.Unavailable));
        Assert.That(collector.Latest.Get("graphSize").Availability, Is.EqualTo(MetricAvailability.Unavailable));
    }
}
