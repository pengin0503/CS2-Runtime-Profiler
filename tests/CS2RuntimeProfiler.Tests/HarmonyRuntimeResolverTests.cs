using System;
using System.Reflection;
using CS2RuntimeProfiler.Profiling;
using NUnit.Framework;

namespace CS2RuntimeProfiler.Tests
{
    public class HarmonyRuntimeResolverTests
    {
        [Test]
        public void Resolve_prefers_an_already_loaded_Harmony_assembly()
        {
            var expected = typeof(HarmonyLib.Harmony).Assembly;
            var loaderCalled = false;

            var resolved = HarmonyRuntimeResolver.Resolve(
                new[] { typeof(string).Assembly, expected },
                _ =>
                {
                    loaderCalled = true;
                    return null;
                });

            Assert.Multiple(() =>
            {
                Assert.That(resolved, Is.SameAs(expected));
                Assert.That(loaderCalled, Is.False);
            });
        }

        [Test]
        public void Resolve_loads_bundled_0Harmony_when_not_already_loaded()
        {
            var expected = typeof(HarmonyLib.Harmony).Assembly;
            AssemblyName requested = null;

            var resolved = HarmonyRuntimeResolver.Resolve(
                Array.Empty<Assembly>(),
                name =>
                {
                    requested = name;
                    return expected;
                });

            Assert.Multiple(() =>
            {
                Assert.That(resolved, Is.SameAs(expected));
                Assert.That(requested?.Name, Is.EqualTo("0Harmony"));
            });
        }

        [Test]
        public void Resolve_fails_open_when_bundled_Harmony_cannot_be_loaded()
        {
            var resolved = HarmonyRuntimeResolver.Resolve(
                Array.Empty<Assembly>(),
                _ => throw new InvalidOperationException("missing"));

            Assert.That(resolved, Is.Null);
        }
    }
}

namespace HarmonyLib
{
    public sealed class Harmony { }
}
