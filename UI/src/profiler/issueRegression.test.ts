import { readFileSync } from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

describe("repository issue regressions", () => {
  it("prevents report filename collisions without overwriting an existing export", () => {
    const source = readFileSync(path.resolve(process.cwd(), "../src/CS2RuntimeProfiler/Export/ReportExporter.cs"), "utf8");
    expect(source).toContain("yyyy-MM-dd_HHmmss_fff");
    expect(source).toContain("FileMode.CreateNew");
    expect(source).toContain("-{attempt}");
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
});
