import { readFileSync } from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

describe("repository issue regressions", () => {
  it("preserves Unity recorder handles until rediscovery succeeds", () => {
    const source = readFileSync(path.resolve(process.cwd(), "../src/CS2RuntimeProfiler/Profiling/UnityRecorderBackend.cs"), "utf8");
    const discoveryIndex = source.indexOf("ProfilerRecorderHandle.GetAvailable");
    const clearIndex = source.indexOf("_handles.Clear()");

    expect(discoveryIndex).toBeGreaterThanOrEqual(0);
    expect(clearIndex).toBeGreaterThan(discoveryIndex);
  });

  it("uses the current runtime clock for manual capture requests instead of the latest sampled timestamp", () => {
    const globalSource = readFileSync(path.resolve(process.cwd(), "../src/CS2RuntimeProfiler/Collectors/GlobalMetricsCollector.cs"), "utf8");
    const captureSource = readFileSync(path.resolve(process.cwd(), "../src/CS2RuntimeProfiler/Profiling/CaptureRuntimeSystem.cs"), "utf8");

    expect(globalSource).toContain("CurrentTimestampSeconds => _clock.Elapsed.TotalSeconds");
    expect(captureSource).toContain("_global?.CurrentTimestampSeconds");
    expect(captureSource).not.toContain("var now = latest?.TimestampSeconds");
  });

  it("closes the profiler panel when Escape is pressed and unregisters the listener", () => {
    const source = readFileSync(new URL("./ProfilerRoot.tsx", import.meta.url), "utf8");

    expect(source).toContain("useEffect");
    expect(source).toContain('event.key === "Escape"');
    expect(source).toContain('addEventListener("keydown"');
    expect(source).toContain('removeEventListener("keydown"');
    expect(source).toContain("togglePanel();");
  });

  it("keeps review hardening wiring and export documentation aligned", () => {
    const reportBuilder = readFileSync(path.resolve(process.cwd(), "../src/CS2RuntimeProfiler/Export/ProfilerReportBuilder.cs"), "utf8");
    const domains = readFileSync(path.resolve(process.cwd(), "../src/CS2RuntimeProfiler/Collectors/DomainMetricsSystem.cs"), "utf8");
    const timing = readFileSync(path.resolve(process.cwd(), "../src/CS2RuntimeProfiler/Core/CaptureSystemTimingFinalizer.cs"), "utf8");
    const readme = readFileSync(path.resolve(process.cwd(), "../README.md"), "utf8");

    expect(reportBuilder).toContain("HasAvailableMetric(report.Pathfinding)");
    expect(reportBuilder).toContain("HasAvailableMetric(report.DomainMetrics)");
    expect(domains).toContain('"Game.Vehicles.ServiceVehicleUnion"');
    expect(domains).toContain("ComponentType.ReadOnly<Ambulance>()");
    expect(domains).toContain("ComponentType.ReadOnly<PrisonerTransport>()");
    expect(timing).toContain("timeMarkers={diagnostics.TimeMarkerCount}");
    expect(timing).toContain("ambiguousMatches={diagnostics.AmbiguousMatchCount}");
    expect(readme).toContain("CS2Profiler-report-YYYY-MM-DD_HHmmss_fff.json");
    expect(readme).toContain("`-1`、`-2`");
  });

});
