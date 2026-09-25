using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2RuntimeProfiler.Core
{
    /// <summary>
    /// Projects captured Unity Entities profiler markers into per-system timing.
    /// Only uniquely matched full system type names with TimeNanoseconds units are accepted.
    /// Short-name guessing and unknown units are intentionally rejected.
    /// </summary>
    public static class SystemMarkerTimingProjector
    {
        private const double NanosecondsPerMillisecond = 1_000_000d;

        public static SystemTimingSnapshot Project(
            IEnumerable<SystemDescriptor> systems,
            IEnumerable<RecorderDescriptor> recorders,
            CaptureSession capture)
        {
            var result = new SystemTimingSnapshot();
            if (capture == null)
                return result;

            var systemList = (systems ?? Array.Empty<SystemDescriptor>())
                .Where(system => system != null && !string.IsNullOrWhiteSpace(system.FullTypeName))
                .ToArray();
            var recorderList = (recorders ?? Array.Empty<RecorderDescriptor>())
                .Where(recorder => recorder != null
                    && string.Equals(recorder.UnitType, "TimeNanoseconds", StringComparison.Ordinal))
                .ToArray();

            foreach (var system in systemList)
            {
                var candidates = recorderList
                    .Where(recorder => MatchesFullSystemName(recorder.Name, system.FullTypeName))
                    .ToArray();

                // Ambiguous matches are left unattributed rather than guessed.
                if (candidates.Length != 1)
                    continue;

                var recorder = candidates[0];
                if (!capture.TryGetMarkerSamples(recorder.Id, out var samples) || samples.Count == 0)
                    continue;

                var validSamples = samples
                    .Where(sample => sample.Value >= 0d && !double.IsNaN(sample.Value) && !double.IsInfinity(sample.Value))
                    .ToArray();
                if (validSamples.Length == 0)
                    continue;

                var milliseconds = validSamples
                    .Select(sample => sample.Value / NanosecondsPerMillisecond)
                    .ToArray();
                var calls = SumCallCounts(validSamples);

                var aggregate = SystemMetricAggregate.FromSamples(
                    system.FullTypeName,
                    milliseconds,
                    MetricConfidence.Full,
                    calls);
                result.AddSystemAggregate(
                    aggregate,
                    system.AssemblyName,
                    system.PatchOwners.Select(owner => owner.OwnerId));
            }

            return result;
        }

        private static int? SumCallCounts(IReadOnlyList<MetricSample> samples)
        {
            if (samples.Count == 0 || samples.Any(sample => !sample.CallCount.HasValue))
                return null;

            long total = 0;
            foreach (var sample in samples)
            {
                var count = Math.Max(0L, sample.CallCount.Value);
                if (count >= int.MaxValue - total)
                    return int.MaxValue;
                total += count;
            }

            return (int)total;
        }

        private static bool MatchesFullSystemName(string markerName, string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(markerName) || string.IsNullOrWhiteSpace(fullTypeName))
                return false;

            if (string.Equals(markerName, fullTypeName, StringComparison.Ordinal))
                return true;

            // Unity Entities creates the system marker as "<World name> <full system name>".
            return markerName.EndsWith(" " + fullTypeName, StringComparison.Ordinal);
        }
    }
}
