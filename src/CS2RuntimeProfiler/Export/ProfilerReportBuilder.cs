using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CS2RuntimeProfiler.UI;

namespace CS2RuntimeProfiler.Export
{
    public static class ProfilerReportBuilder
    {
        public static Func<RuntimeReportMetadata> RuntimeMetadataProvider { get; set; }

        public static PerformanceReport Build(
            UiSnapshot snapshot,
            string gameVersion = null,
            string profilerVersion = null,
            RuntimeReportMetadata metadata = null)
        {
            snapshot = snapshot ?? new UiSnapshot();
            metadata = metadata ?? ResolveRuntimeMetadata();

            var report = new PerformanceReport
            {
                GameVersion = gameVersion,
                ProfilerVersion = profilerVersion,
                HardwareSummary = string.IsNullOrWhiteSpace(metadata?.HardwareSummary) ? null : metadata.HardwareSummary,
                CityName = null
            };

            foreach (var mod in metadata?.EnabledMods ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(mod))
                    report.EnabledMods.Add(mod.Trim());
            }
            report.EnabledMods = report.EnabledMods
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            AddCaptureScope(report, snapshot);
            AddGlobal(report, snapshot.Global);

            foreach (var capture in snapshot.Captures ?? Array.Empty<CaptureSummaryUi>())
            {
                if (capture == null)
                    continue;
                report.Captures.Add(new ReportCapture
                {
                    Id = capture.Id,
                    TriggerKind = capture.TriggerKind,
                    TriggeredAtSeconds = capture.TriggeredAtSeconds,
                    DurationSeconds = capture.DurationSeconds,
                    DiscoveredMarkers = capture.DiscoveredMarkers,
                    CapturedMarkers = capture.CapturedMarkers,
                    CoverageRatio = capture.CoverageRatio,
                    Batched = capture.Batched,
                    ProfilerOverheadShare = capture.ProfilerOverheadShare,
                    Warnings = (capture.Warnings ?? Array.Empty<string>()).ToList()
                });
            }

            foreach (var system in snapshot.Systems ?? Array.Empty<SystemUiRow>())
            {
                if (system == null)
                    continue;
                report.Systems.Add(new ReportSystem
                {
                    SystemId = system.Id,
                    OwnerAssembly = system.OwnerAssembly,
                    Confidence = system.Confidence,
                    CurrentMilliseconds = system.CurrentMilliseconds,
                    MeanMilliseconds = system.MeanMilliseconds,
                    MedianMilliseconds = system.MedianMilliseconds,
                    P95Milliseconds = system.P95Milliseconds,
                    P99Milliseconds = system.P99Milliseconds,
                    MaxMilliseconds = system.MaxMilliseconds,
                    TotalMilliseconds = system.TotalMilliseconds,
                    Calls = system.Calls,
                    PatchOwners = (system.PatchOwners ?? Array.Empty<string>()).ToList()
                });
            }

            foreach (var mod in snapshot.Mods ?? Array.Empty<ModUiRow>())
            {
                if (mod == null)
                    continue;
                var detail = string.Format(
                    CultureInfo.InvariantCulture,
                    "directMs={0:0.###}; directSystems={1}; patchedVanillaSystems={2}",
                    mod.DirectSystemMilliseconds,
                    mod.DirectSystemCount,
                    mod.PatchedVanillaSystemCount);
                report.ModAttribution.Add(new ReportNamedValue(mod.AssemblyName, detail));
            }

            foreach (var metric in snapshot.Pathfinding?.Metrics ?? Array.Empty<UiMetricRow>())
                report.Pathfinding.Add(ToReportMetric(metric));

            foreach (var metric in snapshot.DomainMetrics ?? Array.Empty<UiMetricRow>())
                report.DomainMetrics.Add(ToReportMetric(metric));

            foreach (var point in snapshot.Timeline ?? Array.Empty<TimelinePoint>())
            {
                if (point == null)
                    continue;
                report.Timeline.Add(new ReportTimelinePoint
                {
                    TimestampSeconds = point.TimestampSeconds,
                    Metric = point.Metric,
                    Value = point.Value,
                    Confidence = point.Confidence
                });
            }

            report.ProfilerOverhead.Add(new ReportMetric
            {
                Name = "captureOverheadShare",
                Value = snapshot.Diagnostics?.ProfilerOverheadShare ?? 0d,
                Unit = "ratio",
                Confidence = "Full",
                Availability = "Available"
            });
            report.ProfilerOverhead.Add(new ReportMetric
            {
                Name = "unattributedJobsMilliseconds",
                Value = snapshot.Diagnostics?.UnattributedJobsMilliseconds ?? 0d,
                Unit = "ms",
                Confidence = "Indirect",
                Availability = "Available",
                Note = "Worker/job time is kept separate rather than assigned to a system without evidence."
            });

            foreach (var message in snapshot.Diagnostics?.Messages ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(message))
                    report.Warnings.Add(message);
            }

            report.Capabilities.Add(new ReportNamedValue(
                "systemTiming",
                report.Systems.Count > 0 ? "available" : "unavailable"));
            report.Capabilities.Add(new ReportNamedValue(
                "pathfinding",
                HasAvailableMetric(report.Pathfinding) ? "available" : "unavailable"));
            report.Capabilities.Add(new ReportNamedValue(
                "domainMetrics",
                HasAvailableMetric(report.DomainMetrics) ? "available" : "unavailable"));

            return report;
        }

        private static bool HasAvailableMetric(IEnumerable<ReportMetric> metrics)
        {
            return (metrics ?? Array.Empty<ReportMetric>()).Any(metric =>
                metric != null && string.Equals(metric.Availability, "Available", StringComparison.Ordinal));
        }

        private static RuntimeReportMetadata ResolveRuntimeMetadata()
        {
            var provider = RuntimeMetadataProvider;
            if (provider == null)
                return null;

            try
            {
                return provider();
            }
            catch
            {
                return null;
            }
        }

        private static void AddCaptureScope(PerformanceReport report, UiSnapshot snapshot)
        {
            var capture = snapshot.Capture ?? new CaptureUiState();
            if (!string.IsNullOrWhiteSpace(capture.DetailCaptureId))
                report.CaptureConfig.Add(new ReportNamedValue("detailCaptureId", capture.DetailCaptureId));

            report.CaptureConfig.Add(new ReportNamedValue(
                "detailScope",
                string.IsNullOrWhiteSpace(capture.DetailScope) ? "live" : capture.DetailScope));

            if (snapshot.Global?.Available == true)
            {
                report.CaptureConfig.Add(new ReportNamedValue(
                    "globalTimestampSeconds",
                    snapshot.Global.TimestampSeconds.ToString("R", CultureInfo.InvariantCulture)));
            }
        }

        private static void AddGlobal(PerformanceReport report, GlobalUiMetrics global)
        {
            if (global == null || !global.Available)
                return;

            report.GlobalMetrics.Add(Scalar("selectedSpeed", global.SelectedSpeed, "x"));
            report.GlobalMetrics.Add(Scalar("actualSpeed", global.ActualSpeed, "x"));
            report.GlobalMetrics.Add(Scalar("efficiency", global.Efficiency, "ratio"));

            foreach (var metric in global.RecorderMetrics ?? Array.Empty<UiMetricRow>())
                report.GlobalMetrics.Add(ToReportMetric(metric));
        }

        private static ReportMetric Scalar(string name, double? value, string unit)
        {
            return new ReportMetric
            {
                Name = name,
                Value = value,
                Unit = unit,
                Confidence = value.HasValue ? "Full" : "Unavailable",
                Availability = value.HasValue ? "Available" : "Unavailable"
            };
        }

        private static ReportMetric ToReportMetric(UiMetricRow metric)
        {
            if (metric == null)
                return new ReportMetric { Name = "unknown", Availability = "Unavailable", Confidence = "Unavailable" };

            return new ReportMetric
            {
                Name = metric.Id,
                Value = metric.Value,
                Unit = metric.UnitType,
                Confidence = metric.Confidence,
                Availability = metric.Availability,
                Note = metric.Reason
            };
        }
    }
}
