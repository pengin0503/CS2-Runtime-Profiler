using System;
using System.Collections.Generic;
using System.Linq;
using CS2RuntimeProfiler.Core;

namespace CS2RuntimeProfiler.UI
{
    public static class UiSnapshotBuilder
    {
        private const double CorrelationWindowSeconds = 5d;
        private const int MaxCorrelatedChanges = 8;

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
                Timeline = BuildTimeline(input.Captures ?? Array.Empty<CaptureSession>()),
                Captures = captures,
                Diagnostics = new DiagnosticsUi
                {
                    ProfilerOverheadShare = Math.Max(0d, input.ProfilerOverheadShare),
                    UnattributedJobsMilliseconds = input.Systems?.UnattributedJobsMilliseconds ?? 0d,
                    Messages = (input.Diagnostics ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray(),
                    GameVersion = input.GameVersion ?? string.Empty,
                    ProfilerVersion = input.ProfilerVersion ?? string.Empty,
                    DiscoveredMarkerCount = Math.Max(0, input.DiscoveredMarkerCount),
                    CapturedMarkerCount = Math.Max(0, input.CapturedMarkerCount),
                    SystemCount = systems.Count,
                    MarkerBatchSize = Math.Max(0, input.MarkerBatchSize),
                    SamplingStride = Math.Max(1, input.SamplingStride),
                    PatchMapState = input.PatchMapState ?? string.Empty
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
            var samples = capture.GlobalSamples ?? Array.Empty<GlobalMetricsSnapshot>();
            var duration = samples.Count < 2
                ? 0d
                : Math.Max(0d, samples.Max(sample => sample.TimestampSeconds) - samples.Min(sample => sample.TimestampSeconds));

            return new CaptureSummaryUi
            {
                Id = capture.Id,
                TriggerKind = capture.Trigger.Kind.ToString(),
                TriggeredAtSeconds = capture.Trigger.TimestampSeconds,
                DurationSeconds = duration,
                DiscoveredMarkers = capture.MarkerCoverage.Discovered,
                CapturedMarkers = capture.MarkerCoverage.Captured,
                Batched = capture.MarkerCoverage.IsBatched,
                CoverageRatio = capture.MarkerCoverage.Ratio,
                WarningCount = capture.Warnings.Count,
                ProfilerOverheadShare = capture.MaxProfilerOverheadShare,
                Warnings = capture.Warnings.ToArray(),
                CorrelatedChanges = BuildCorrelatedChanges(capture)
            };
        }

        private static IReadOnlyList<CorrelatedChangeUi> BuildCorrelatedChanges(CaptureSession capture)
        {
            var trigger = capture.Trigger.TimestampSeconds;
            var pre = capture.GlobalSamples
                .Where(sample => sample.TimestampSeconds >= trigger - CorrelationWindowSeconds && sample.TimestampSeconds < trigger)
                .ToArray();
            var post = capture.GlobalSamples
                .Where(sample => sample.TimestampSeconds >= trigger && sample.TimestampSeconds <= trigger + CorrelationWindowSeconds)
                .ToArray();

            if (pre.Length == 0 || post.Length == 0)
                return Array.Empty<CorrelatedChangeUi>();

            var changes = new List<CorrelatedChangeUi>();
            AddChange(changes, "actualSpeed", pre.Average(sample => sample.ActualSpeed), post.Average(sample => sample.ActualSpeed), MetricConfidence.Full);
            AddChange(changes, "efficiency", pre.Average(sample => sample.Efficiency), post.Average(sample => sample.Efficiency), MetricConfidence.Full);

            var sharedRecorderIds = pre
                .SelectMany(sample => sample.RecorderReadings.Keys)
                .Intersect(post.SelectMany(sample => sample.RecorderReadings.Keys), StringComparer.Ordinal)
                .Distinct(StringComparer.Ordinal);

            foreach (var id in sharedRecorderIds)
            {
                var beforeValues = pre.Where(sample => sample.RecorderReadings.ContainsKey(id)).Select(sample => sample.RecorderReadings[id].Value).ToArray();
                var afterValues = post.Where(sample => sample.RecorderReadings.ContainsKey(id)).Select(sample => sample.RecorderReadings[id].Value).ToArray();
                if (beforeValues.Length == 0 || afterValues.Length == 0)
                    continue;
                AddChange(changes, id, beforeValues.Average(), afterValues.Average(), MetricConfidence.Full);
            }

            return changes
                .Where(change => Math.Abs(change.Delta) > double.Epsilon)
                .OrderByDescending(change => Math.Abs(change.RelativeDelta ?? 0d))
                .ThenByDescending(change => Math.Abs(change.Delta))
                .Take(MaxCorrelatedChanges)
                .ToArray();
        }

        private static void AddChange(List<CorrelatedChangeUi> changes, string metric, double before, double after, MetricConfidence confidence)
        {
            if (double.IsNaN(before) || double.IsInfinity(before) || double.IsNaN(after) || double.IsInfinity(after))
                return;
            var delta = after - before;
            changes.Add(new CorrelatedChangeUi
            {
                Metric = metric,
                Before = before,
                After = after,
                Delta = delta,
                RelativeDelta = Math.Abs(before) <= double.Epsilon ? (double?)null : delta / Math.Abs(before),
                Confidence = confidence.ToString()
            });
        }

        private static IReadOnlyList<TimelinePoint> BuildTimeline(IReadOnlyList<CaptureSession> captures)
        {
            if (captures == null || captures.Count == 0)
                return Array.Empty<TimelinePoint>();

            var points = new List<TimelinePoint>();
            foreach (var capture in captures.Where(capture => capture != null))
            {
                foreach (var sample in capture.GlobalSamples)
                {
                    points.Add(new TimelinePoint
                    {
                        TimestampSeconds = sample.TimestampSeconds,
                        Metric = "actualSpeed",
                        Value = sample.ActualSpeed,
                        Confidence = MetricConfidence.Full.ToString()
                    });
                    points.Add(new TimelinePoint
                    {
                        TimestampSeconds = sample.TimestampSeconds,
                        Metric = "efficiency",
                        Value = sample.Efficiency,
                        Confidence = MetricConfidence.Full.ToString()
                    });
                    foreach (var recorder in sample.RecorderReadings)
                    {
                        points.Add(new TimelinePoint
                        {
                            TimestampSeconds = sample.TimestampSeconds,
                            Metric = "recorder:" + recorder.Key,
                            Value = recorder.Value.Value,
                            Confidence = MetricConfidence.Full.ToString()
                        });
                    }
                }

                foreach (var marker in capture.MarkerSamples)
                {
                    foreach (var sample in marker.Value)
                    {
                        points.Add(new TimelinePoint
                        {
                            TimestampSeconds = sample.TimestampSeconds,
                            Metric = "marker:" + marker.Key,
                            Value = sample.Value,
                            Confidence = sample.Confidence.ToString()
                        });
                    }
                }

                if (capture.SystemTiming != null)
                {
                    foreach (var system in capture.SystemTiming.Systems)
                    {
                        points.Add(new TimelinePoint
                        {
                            TimestampSeconds = capture.Trigger.TimestampSeconds,
                            Metric = "system:" + system.SystemId,
                            Value = system.Milliseconds,
                            Confidence = system.Confidence.ToString()
                        });
                    }
                }
            }

            return points
                .OrderBy(point => point.TimestampSeconds)
                .ThenBy(point => point.Metric, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
