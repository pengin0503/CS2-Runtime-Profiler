using CS2RuntimeProfiler.Core;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests;

public class SystemCatalogCacheTests
{
    [Test]
    public void Successful_refresh_replaces_the_published_catalog_snapshot()
    {
        IEnumerable<SystemDescriptor> current = new[] { Descriptor("System.A") };
        var cache = new SystemCatalogCache(() => current);

        Assert.That(cache.TryRefresh(out var firstError), Is.True);
        Assert.That(firstError, Is.Null);
        Assert.That(cache.Snapshot.Select(system => system.FullTypeName), Is.EqualTo(new[] { "System.A" }));

        current = new[] { Descriptor("System.B"), Descriptor("System.C") };

        Assert.That(cache.TryRefresh(out var secondError), Is.True);
        Assert.That(secondError, Is.Null);
        Assert.That(cache.Snapshot.Select(system => system.FullTypeName), Is.EqualTo(new[] { "System.B", "System.C" }));
    }

    [Test]
    public void Failed_refresh_preserves_the_last_known_good_catalog_snapshot()
    {
        var fail = false;
        var cache = new SystemCatalogCache(() =>
        {
            if (fail)
                throw new InvalidOperationException("catalog refresh failed");
            return new[] { Descriptor("System.A") };
        });

        Assert.That(cache.TryRefresh(out _), Is.True);
        fail = true;

        Assert.Multiple(() =>
        {
            Assert.That(cache.TryRefresh(out var error), Is.False);
            Assert.That(error, Does.Contain("catalog refresh failed"));
            Assert.That(cache.Snapshot.Select(system => system.FullTypeName), Is.EqualTo(new[] { "System.A" }));
        });
    }

    private static SystemDescriptor Descriptor(string name) => new(
        name,
        "Game",
        SystemSourceKind.Vanilla,
        null,
        MetricConfidence.Unavailable,
        Array.Empty<PatchOwnerInfo>());
}
