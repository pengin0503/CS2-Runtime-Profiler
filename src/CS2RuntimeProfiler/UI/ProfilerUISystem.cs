using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Colossal.UI.Binding;
using CS2RuntimeProfiler.Collectors;
using CS2RuntimeProfiler.Core;
using CS2RuntimeProfiler.Export;
using CS2RuntimeProfiler.Profiling;
using Game;
using Game.UI;

namespace CS2RuntimeProfiler.UI
{
    public partial class ProfilerUISystem : UISystemBase
    {
        private const string Group = Mod.Id;
        private const double UiRefreshPeriodSeconds = 0.5d;

        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private GlobalMetricsCollector _global;
        private DomainMetricsSystem _domains;
        private CaptureRuntimeSystem _capture;
        private ReportExporter _exporter;
        private UiSnapshot _snapshot = new UiSnapshot();
        private RawValueBinding _snapshotBinding;
        private RawValueBinding _hudSnapshotBinding;
        private ValueBinding<bool> _panelVisibleBinding;
        private ValueBinding<string> _selectedCaptureBinding;
        private ValueBinding<string> _selectedSystemBinding;
        private ValueBinding<string> _selectedModBinding;
        private ValueBinding<string> _exportResultBinding;
        private double _nextRefreshAt;
        private bool _panelVisible;
        private string _selectedCaptureId = string.Empty;

        public override GameMode gameMode => GameMode.Game;

        protected override void OnCreate()
        {
            base.OnCreate();

            _global = World.GetOrCreateSystemManaged<GlobalMetricsCollector>();
            _domains = World.GetOrCreateSystemManaged<DomainMetricsSystem>();
            _capture = World.GetOrCreateSystemManaged<CaptureRuntimeSystem>();
            _exporter = new ReportExporter();

            AddBinding(_snapshotBinding = new RawValueBinding(Group, "snapshot", WriteSnapshot));
            AddBinding(_hudSnapshotBinding = new RawValueBinding(Group, "hudSnapshot", WriteHudSnapshot));
            AddBinding(_panelVisibleBinding = new ValueBinding<bool>(Group, "panelVisible", false));
            AddBinding(_selectedCaptureBinding = new ValueBinding<string>(Group, "selectedCaptureId", string.Empty));
            AddBinding(_selectedSystemBinding = new ValueBinding<string>(Group, "selectedSystemId", string.Empty));
            AddBinding(_selectedModBinding = new ValueBinding<string>(Group, "selectedModId", string.Empty));
            AddBinding(_exportResultBinding = new ValueBinding<string>(Group, "exportResult", string.Empty));

            AddBinding(new TriggerBinding(Group, "togglePanel", TogglePanel));
            AddBinding(new TriggerBinding(Group, "manualCapture", ManualCapture));
            AddBinding(new TriggerBinding<string>(Group, "selectCapture", SelectCapture));
            AddBinding(new TriggerBinding<string>(Group, "selectSystem", id => _selectedSystemBinding.Update(id ?? string.Empty)));
            AddBinding(new TriggerBinding<string>(Group, "selectMod", id => _selectedModBinding.Update(id ?? string.Empty)));
            AddBinding(new TriggerBinding(Group, "exportReport", ExportReport));

            RefreshSnapshot();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            var now = _clock.Elapsed.TotalSeconds;
            if (now < _nextRefreshAt)
                return;

            _nextRefreshAt = now + UiRefreshPeriodSeconds;
            _hudSnapshotBinding.Update();

            if (!_panelVisible)
                return;

            RefreshSnapshot();
            _snapshotBinding.Update();
        }

        private void TogglePanel()
        {
            _panelVisible = !_panelVisible;
            _panelVisibleBinding.Update(_panelVisible);
            _hudSnapshotBinding.Update();

            if (_panelVisible)
            {
                RefreshSnapshot();
                _snapshotBinding.Update();
            }
        }

        private void ManualCapture()
        {
            _capture?.RequestManualCapture();
            _hudSnapshotBinding.Update();

            if (_panelVisible)
            {
                RefreshSnapshot();
                _snapshotBinding.Update();
            }
        }

        private void SelectCapture(string id)
        {
            _selectedCaptureId = id ?? string.Empty;
            _selectedCaptureBinding.Update(_selectedCaptureId);

            if (_panelVisible)
            {
                RefreshSnapshot();
                _snapshotBinding.Update();
            }
        }

        private void ExportReport()
        {
            try
            {
                var exportSnapshot = UiSnapshotBuilder.BuildForExport(CreateSnapshotInput());
                var report = ProfilerReportBuilder.Build(
                    exportSnapshot,
                    gameVersion: exportSnapshot.Diagnostics?.GameVersion,
                    profilerVersion: exportSnapshot.Diagnostics?.ProfilerVersion);
                var result = _exporter.Export(report);
                var message = result.Success
                    ? $"ok:{Path.GetFileName(result.Path)}"
                    : $"error:{result.Error}";
                _exportResultBinding.Update(message);
            }
            catch (Exception ex)
            {
                Mod.Log.Error(ex, "UI-triggered profiler report export failed");
                _exportResultBinding.Update($"error:{PrivacySanitizer.Sanitize(ex.Message)}");
            }
        }

        private void RefreshSnapshot()
        {
            _snapshot = UiSnapshotBuilder.Build(CreateSnapshotInput());
        }

        private UiSnapshotInput CreateSnapshotInput()
        {
            var captures = new List<CaptureSession>();
            if (_capture?.CompletedSessions != null)
                captures.AddRange(_capture.CompletedSessions.Where(capture => capture != null));

            if (!string.IsNullOrWhiteSpace(_selectedCaptureId)
                && !captures.Any(capture => string.Equals(capture.Id, _selectedCaptureId, StringComparison.Ordinal)))
            {
                _selectedCaptureId = string.Empty;
                _selectedCaptureBinding.Update(string.Empty);
            }

            var currentCapture = _capture?.CurrentSession;
            CaptureSession detailCapture = null;
            if (!string.IsNullOrWhiteSpace(_selectedCaptureId))
            {
                detailCapture = captures.FirstOrDefault(capture =>
                    string.Equals(capture.Id, _selectedCaptureId, StringComparison.Ordinal));
            }
            detailCapture = detailCapture ?? currentCapture ?? captures.LastOrDefault();

            var timing = detailCapture?.SystemTiming;
            var diagnostics = new List<string>();
            if (timing == null)
                diagnostics.Add("システム別の実行時間は、対応する詳細キャプチャが作成されるまで利用できません。");
            diagnostics.Add("タイムラインは現在選択しているキャプチャが実際に保持した履歴だけを表示します。利用できない系列を推測で生成することはありません。");

            var markerCapture = detailCapture ?? currentCapture ?? captures.LastOrDefault();
            var patchMapState = timing == null
                ? "利用不可: システム時間スナップショットがありません。"
                : timing.Systems.Any(system => system.PatchOwners != null && system.PatchOwners.Count > 0)
                    ? "現在のシステム時間スナップショットでパッチ情報を検出しました。"
                    : "現在のシステム時間スナップショットではパッチ所有者を検出していません。";

            return new UiSnapshotInput(
                _global?.Latest,
                _capture?.State ?? CaptureState.Monitoring,
                _domains?.Pathfinding?.Latest,
                _domains?.Entities?.Latest,
                timing,
                captures,
                _capture?.LastOverheadShare ?? 0d,
                diagnostics)
            {
                CurrentCapture = currentCapture,
                SelectedCaptureId = _selectedCaptureId,
                GameVersion = GetGameVersion(),
                ProfilerVersion = typeof(Mod).Assembly.GetName().Version?.ToString() ?? string.Empty,
                DiscoveredMarkerCount = _global?.Recorders?.Descriptors?.Count ?? 0,
                CapturedMarkerCount = markerCapture?.MarkerCoverage.Captured ?? 0,
                MarkerBatchSize = _capture?.CurrentBatchSize ?? 0,
                SamplingStride = _capture?.SamplingStride ?? 1,
                PatchMapState = patchMapState
            };
        }

        private void WriteHudSnapshot(IJsonWriter writer)
        {
            var latest = _global?.Latest;
            var state = _capture?.State ?? CaptureState.Monitoring;

            writer.TypeBegin("CS2RuntimeProfiler.UiHudSnapshot");
            writer.PropertyName("selectedSpeed");
            if (latest == null) writer.WriteNull(); else writer.Write(latest.SelectedSpeed);
            writer.PropertyName("actualSpeed");
            if (latest == null) writer.WriteNull(); else writer.Write(latest.ActualSpeed);
            writer.PropertyName("state"); writer.Write(state.ToString());
            writer.PropertyName("isDeepCapture"); writer.Write(state == CaptureState.DeepCapture);
            writer.TypeEnd();
        }

        private static string GetGameVersion()
        {
            try
            {
                return Game.Version.current.fullVersion ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private void WriteSnapshot(IJsonWriter writer)
        {
            var snapshot = _snapshot ?? new UiSnapshot();
            writer.TypeBegin("CS2RuntimeProfiler.UiSnapshot");

            writer.PropertyName("global");
            WriteGlobal(writer, snapshot.Global);
            writer.PropertyName("capture");
            WriteCaptureState(writer, snapshot.Capture);
            writer.PropertyName("systems");
            WriteSystems(writer, snapshot.Systems);
            writer.PropertyName("mods");
            WriteMods(writer, snapshot.Mods);
            writer.PropertyName("pathfinding");
            WriteMetricsObject(writer, snapshot.Pathfinding?.Metrics);
            writer.PropertyName("domainMetrics");
            WriteMetricsArray(writer, snapshot.DomainMetrics);
            writer.PropertyName("timeline");
            WriteTimeline(writer, snapshot.Timeline);
            writer.PropertyName("captures");
            WriteCaptures(writer, snapshot.Captures);
            writer.PropertyName("diagnostics");
            WriteDiagnostics(writer, snapshot.Diagnostics);

            writer.TypeEnd();
        }

        private static void WriteGlobal(IJsonWriter writer, GlobalUiMetrics global)
        {
            global = global ?? new GlobalUiMetrics();
            writer.TypeBegin("CS2RuntimeProfiler.GlobalUiMetrics");
            writer.PropertyName("available"); writer.Write(global.Available);
            writer.PropertyName("timestampSeconds"); writer.Write(global.TimestampSeconds);
            writer.PropertyName("selectedSpeed"); WriteNullable(writer, global.SelectedSpeed);
            writer.PropertyName("actualSpeed"); WriteNullable(writer, global.ActualSpeed);
            writer.PropertyName("efficiency"); WriteNullable(writer, global.Efficiency);
            writer.PropertyName("recorderMetrics"); WriteMetricsArray(writer, global.RecorderMetrics);
            writer.TypeEnd();
        }

        private static void WriteCaptureState(IJsonWriter writer, CaptureUiState capture)
        {
            capture = capture ?? new CaptureUiState();
            writer.TypeBegin("CS2RuntimeProfiler.CaptureUiState");
            writer.PropertyName("state"); writer.Write(capture.State ?? string.Empty);
            writer.PropertyName("isDeepCapture"); writer.Write(capture.IsDeepCapture);
            writer.PropertyName("completedCount"); writer.Write(capture.CompletedCount);
            writer.PropertyName("detailCaptureId"); writer.Write(capture.DetailCaptureId ?? string.Empty);
            writer.PropertyName("detailScope"); writer.Write(capture.DetailScope ?? string.Empty);
            writer.TypeEnd();
        }

        private static void WriteSystems(IJsonWriter writer, IReadOnlyList<SystemUiRow> systems)
        {
            systems = systems ?? Array.Empty<SystemUiRow>();
            writer.ArrayBegin((uint)systems.Count);
            foreach (var row in systems)
            {
                var item = row ?? new SystemUiRow();
                writer.TypeBegin("CS2RuntimeProfiler.SystemUiRow");
                writer.PropertyName("id"); writer.Write(item.Id ?? string.Empty);
                writer.PropertyName("ownerAssembly"); writer.Write(item.OwnerAssembly ?? string.Empty);
                writer.PropertyName("sourceKind"); writer.Write(item.SourceKind ?? string.Empty);
                writer.PropertyName("currentMilliseconds"); writer.Write(item.CurrentMilliseconds);
                writer.PropertyName("meanMilliseconds"); WriteNullable(writer, item.MeanMilliseconds);
                writer.PropertyName("medianMilliseconds"); WriteNullable(writer, item.MedianMilliseconds);
                writer.PropertyName("p95Milliseconds"); WriteNullable(writer, item.P95Milliseconds);
                writer.PropertyName("p99Milliseconds"); WriteNullable(writer, item.P99Milliseconds);
                writer.PropertyName("maxMilliseconds"); WriteNullable(writer, item.MaxMilliseconds);
                writer.PropertyName("totalMilliseconds"); WriteNullable(writer, item.TotalMilliseconds);
                writer.PropertyName("calls"); if (item.Calls.HasValue) writer.Write(item.Calls.Value); else writer.WriteNull();
                writer.PropertyName("confidence"); writer.Write(item.Confidence ?? string.Empty);
                writer.PropertyName("patchOwners"); WriteStrings(writer, item.PatchOwners);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private static void WriteMods(IJsonWriter writer, IReadOnlyList<ModUiRow> mods)
        {
            mods = mods ?? Array.Empty<ModUiRow>();
            writer.ArrayBegin((uint)mods.Count);
            foreach (var row in mods)
            {
                var item = row ?? new ModUiRow();
                writer.TypeBegin("CS2RuntimeProfiler.ModUiRow");
                writer.PropertyName("assemblyName"); writer.Write(item.AssemblyName ?? string.Empty);
                writer.PropertyName("directSystemMilliseconds"); writer.Write(item.DirectSystemMilliseconds);
                writer.PropertyName("directSystemCount"); writer.Write(item.DirectSystemCount);
                writer.PropertyName("patchedVanillaSystemCount"); writer.Write(item.PatchedVanillaSystemCount);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private static void WriteMetricsObject(IJsonWriter writer, IReadOnlyList<UiMetricRow> metrics)
        {
            writer.TypeBegin("CS2RuntimeProfiler.PathfindingUiMetrics");
            writer.PropertyName("metrics"); WriteMetricsArray(writer, metrics);
            writer.TypeEnd();
        }

        private static void WriteMetricsArray(IJsonWriter writer, IReadOnlyList<UiMetricRow> metrics)
        {
            metrics = metrics ?? Array.Empty<UiMetricRow>();
            writer.ArrayBegin((uint)metrics.Count);
            foreach (var row in metrics)
            {
                var item = row ?? new UiMetricRow();
                writer.TypeBegin("CS2RuntimeProfiler.UiMetricRow");
                writer.PropertyName("id"); writer.Write(item.Id ?? string.Empty);
                writer.PropertyName("value"); WriteNullable(writer, item.Value);
                writer.PropertyName("unitType"); writer.Write(item.UnitType ?? string.Empty);
                writer.PropertyName("confidence"); writer.Write(item.Confidence ?? string.Empty);
                writer.PropertyName("availability"); writer.Write(item.Availability ?? string.Empty);
                writer.PropertyName("reason"); if (item.Reason == null) writer.WriteNull(); else writer.Write(item.Reason);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private static void WriteTimeline(IJsonWriter writer, IReadOnlyList<TimelinePoint> timeline)
        {
            timeline = timeline ?? Array.Empty<TimelinePoint>();
            writer.ArrayBegin((uint)timeline.Count);
            foreach (var point in timeline)
            {
                var item = point ?? new TimelinePoint();
                writer.TypeBegin("CS2RuntimeProfiler.TimelinePoint");
                writer.PropertyName("timestampSeconds"); writer.Write(item.TimestampSeconds);
                writer.PropertyName("metric"); writer.Write(item.Metric ?? string.Empty);
                writer.PropertyName("value"); writer.Write(item.Value);
                writer.PropertyName("confidence"); writer.Write(item.Confidence ?? string.Empty);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private static void WriteCaptures(IJsonWriter writer, IReadOnlyList<CaptureSummaryUi> captures)
        {
            captures = captures ?? Array.Empty<CaptureSummaryUi>();
            writer.ArrayBegin((uint)captures.Count);
            foreach (var capture in captures)
            {
                var item = capture ?? new CaptureSummaryUi();
                writer.TypeBegin("CS2RuntimeProfiler.CaptureSummaryUi");
                writer.PropertyName("id"); writer.Write(item.Id ?? string.Empty);
                writer.PropertyName("triggerKind"); writer.Write(item.TriggerKind ?? string.Empty);
                writer.PropertyName("triggeredAtSeconds"); writer.Write(item.TriggeredAtSeconds);
                writer.PropertyName("durationSeconds"); writer.Write(item.DurationSeconds);
                writer.PropertyName("discoveredMarkers"); writer.Write(item.DiscoveredMarkers);
                writer.PropertyName("capturedMarkers"); writer.Write(item.CapturedMarkers);
                writer.PropertyName("batched"); writer.Write(item.Batched);
                writer.PropertyName("coverageRatio"); WriteNullable(writer, item.CoverageRatio);
                writer.PropertyName("warningCount"); writer.Write(item.WarningCount);
                writer.PropertyName("profilerOverheadShare"); writer.Write(item.ProfilerOverheadShare);
                writer.PropertyName("warnings"); WriteStrings(writer, item.Warnings);
                writer.PropertyName("correlatedChanges"); WriteCorrelatedChanges(writer, item.CorrelatedChanges);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private static void WriteCorrelatedChanges(IJsonWriter writer, IReadOnlyList<CorrelatedChangeUi> changes)
        {
            changes = changes ?? Array.Empty<CorrelatedChangeUi>();
            writer.ArrayBegin((uint)changes.Count);
            foreach (var change in changes)
            {
                var item = change ?? new CorrelatedChangeUi();
                writer.TypeBegin("CS2RuntimeProfiler.CorrelatedChangeUi");
                writer.PropertyName("metric"); writer.Write(item.Metric ?? string.Empty);
                writer.PropertyName("before"); writer.Write(item.Before);
                writer.PropertyName("after"); writer.Write(item.After);
                writer.PropertyName("delta"); writer.Write(item.Delta);
                writer.PropertyName("relativeDelta"); WriteNullable(writer, item.RelativeDelta);
                writer.PropertyName("confidence"); writer.Write(item.Confidence ?? string.Empty);
                writer.TypeEnd();
            }
            writer.ArrayEnd();
        }

        private static void WriteDiagnostics(IJsonWriter writer, DiagnosticsUi diagnostics)
        {
            diagnostics = diagnostics ?? new DiagnosticsUi();
            writer.TypeBegin("CS2RuntimeProfiler.DiagnosticsUi");
            writer.PropertyName("profilerOverheadShare"); writer.Write(diagnostics.ProfilerOverheadShare);
            writer.PropertyName("unattributedJobsMilliseconds"); writer.Write(diagnostics.UnattributedJobsMilliseconds);
            writer.PropertyName("messages"); WriteStrings(writer, diagnostics.Messages);
            writer.PropertyName("gameVersion"); writer.Write(diagnostics.GameVersion ?? string.Empty);
            writer.PropertyName("profilerVersion"); writer.Write(diagnostics.ProfilerVersion ?? string.Empty);
            writer.PropertyName("discoveredMarkerCount"); writer.Write(diagnostics.DiscoveredMarkerCount);
            writer.PropertyName("capturedMarkerCount"); writer.Write(diagnostics.CapturedMarkerCount);
            writer.PropertyName("systemCount"); writer.Write(diagnostics.SystemCount);
            writer.PropertyName("markerBatchSize"); writer.Write(diagnostics.MarkerBatchSize);
            writer.PropertyName("samplingStride"); writer.Write(diagnostics.SamplingStride);
            writer.PropertyName("patchMapState"); writer.Write(diagnostics.PatchMapState ?? string.Empty);
            writer.TypeEnd();
        }

        private static void WriteStrings(IJsonWriter writer, IReadOnlyList<string> values)
        {
            values = values ?? Array.Empty<string>();
            writer.ArrayBegin((uint)values.Count);
            foreach (var value in values)
                writer.Write(value ?? string.Empty);
            writer.ArrayEnd();
        }

        private static void WriteNullable(IJsonWriter writer, double? value)
        {
            if (value.HasValue)
                writer.Write(value.Value);
            else
                writer.WriteNull();
        }
    }
}
