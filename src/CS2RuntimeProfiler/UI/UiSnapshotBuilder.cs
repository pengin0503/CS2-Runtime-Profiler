using System;
using System.Collections.Generic;
using System.Linq;
using CS2RuntimeProfiler.Core;

namespace CS2RuntimeProfiler.UI
{
    public static class UiSnapshotBuilder
    {
        public static UiSnapshot Build(UiSnapshotInput input)
        {
            input = input ?? new UiSnapshotInput();
            var systems = BuildSystems(input.Systems);
            var captures = (input.Captures ?? Array.Empty<CaptureSession>())
                .Where(capture => capture != null)
                .Select(BuildCapture)
                .ToArray();

            return new UiSnapshot
            {
                Global = BuildGlobal(input.Global),
                Capture = new CaptureUiState
                {
                    State = input.CaptureState.ToString(),
                    IsDeepCapture = input.CaptureState == CaptureState.DeepCapture,
                    CompletedCount = captures.Length
                },
                Systems = systems,
                Mods = BuildMods(systems),
                Pathfinding = new PathfindingUiMetrics { Metrics = BuildMetrics(input.Pathfinding) },
                DomainMetrics = BuildMetrics(input.Domains),
                Timeline = BuildTimeline(input.Captures),
                Captures = captures,
                Diagnostics = new DiagnosticsUi
                {
                    ProfilerOverheadShare = Math.Max(0d, input.ProfilerOverheadShare),
                    UnattributedJobsMilliseconds = input.Systems?.UnattributedJobsMilliseconds ?? 0d,
                    Messages = (input.Diagnostics ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
                }
            };
        }

        private static GlobalUiMetrics BuildGlobal(GlobalMetricsSnapshot global)
        {
            if (global == null)
                return new GlobalUiMetrics { Available = false };

            return new GlobalUiMetrics
            {
                Available = true,
                TimestampSeconds = global.TimestampSeconds,
                SelectedSpeed = global.SelectedSpeed,
                ActualSpeed = global.ActualSpeed,
                Efficiency = global.Efficiency,
                RecorderMetrics = global.RecorderReadings
                    .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new UiMetricRow
                    {
                        Id = pair.Key,
                        Value = pair.Value.Value,
                        Availability = MetricAvailability.Available.ToString(),
                        Confidence = MetricConfidence.Full.ToString()
                    })
                    .ToArray()
            };
        }

        private static IReadOnlyList<UiMetricRow> BuildMetrics(NamedMetricSnapshot snapshot)
        {
            if (snapshot == null)
                return Array.Empty<UiMetricRow>();

            return snapshot.Metrics.Values
                .OrderBy(metric => metric.Id, StringComparer.Ordinal)
                .Select(metric => new UiMetricRow
                {
                    Id = metric.Id,
                    Value = metric.Value,
                    Confidence = metric.Confidence.ToString(),
                    Availability = metric.Availability.ToString(),
                    Reason = metric.Reason
                })
                .ToArray();
        }

        private static IReadOnlyList<SystemUiRow> BuildSystems(SystemTimingSnapshot snapshot)
        {
            if (snapshot == null)
                return Array.Empty<SystemUiRow>();

            return snapshot.Systems
                .Select(system => new SystemUiRow
                {
                    Id = system.SystemId,
                    OwnerAssembly = system.OwnerAssembly,
                    CurrentMilliseconds = system.Milliseconds,
                    Confidence = system.Confidence.ToString(),
                    PatchOwners = system.PatchOwners.ToArray()
                })
                .OrderByDescending(system => system.CurrentMilliseconds)
                .ThenBy(system => system.Id, StringComparer.Ordinal)
                .ToArray();
        }

        private static IReadOnlyList<ModUiRow> BuildMods(IReadOnlyList<SystemUiRow> systems)
        {
            if (systems == null || systems.Count == 0)
                return Array.Empty<ModUiRow>();

            var direct = systems
                .Where(system => !string.IsNullOrWhiteSpace(system.OwnerAssembly))
                .GroupBy(system => system.OwnerAssembly, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => new ModUiRow
                    {
                        AssemblyName = group.Key,
                        DirectSystemMilliseconds = group.Sum(system => system.CurrentMilliseconds),
                        DirectSystemCount = group.Count()
                    },
                    StringComparer.Ordinal);

            foreach (var system in systems)
            {
                foreach (var patchOwner in system.PatchOwners ?? Array.Empty<string>())
                {
                    if (string.IsNullOrWhiteSpace(patchOwner))
                        continue;
                    if (!direct.TryGetValue(patchOwner, out var row))
                    {
                        row = new ModUiRow { AssemblyName = patchOwner };
                        direct[patchOwner] = row;
                    }
                    if (!string.Equals(system.OwnerAssembly, patchOwner, StringComparison.Ordinal))
                        row.PatchedVanillaSystemCount++;
                }
            }

            return direct.Values
                .OrderByDescending(row => row.DirectSystemMilliseconds)
                .ThenBy(row => row.AssemblyName, StringComparer.Ordinal)
                .ToArray();
        }

        private static CaptureSummaryUi BuildCapture(CaptureSession capture)
        {
            return new CaptureSummaryUi
            {
                Id = capture.Id,
                TriggerKind = capture.Trigger.Kind.ToString(),
                TriggeredAtSeconds = capture.Trigger.TriggeredAtSeconds,
                DiscoveredMarkers = capture.MarkerCoverage.Discovered,
                CapturedMarkers = capture.MarkerCoverage.Captured,
                Batched = capture.MarkerCoverage.IsBatched,
                CoverageRatio = capture.MarkerCoverage.Ratio,
                WarningCount = capture.Warnings.Count
            };
        }

        private static IReadOnlyList<TimelinePoint> BuildTimeline(IReadOnlyList<CaptureSession> captures)
        {
            if (captures == null || captures.Count == 0)
                return Array.Empty<TimelinePoint>();

            return captures
                .Where(capture => capture != null)
                .SelectMany(capture => capture.GlobalSamples)
                .Select(sample => new TimelinePoint
                {
                    TimestampSeconds = sample.TimestampSeconds,
                    Metric = "actualSpeed",
                    Value = sample.ActualSpeed,
                    Confidence = MetricConfidence.Full.ToString()
                })
                .OrderBy(point => point.TimestampSeconds)
                .ToArray();
        }
    }
}
