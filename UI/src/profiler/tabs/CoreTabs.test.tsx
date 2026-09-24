import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { OverviewTab } from "./OverviewTab";
import { SystemsTab } from "./SystemsTab";
import { ModsTab } from "./ModsTab";

const snapshot: any = {
  global: {
    available: true,
    timestampSeconds: 10,
    selectedSpeed: 4,
    actualSpeed: 2,
    efficiency: 0.5,
    recorderMetrics: [
      { id: "CPU\u001fMain Thread", value: 12_500_000, unitType: "TimeNanoseconds", confidence: "Full", availability: "Available", reason: null },
      { id: "GPU\u001fGPU Time", value: null, confidence: "Unavailable", availability: "Unavailable", reason: "not exposed" }
    ]
  },
  capture: { state: "Monitoring", isDeepCapture: false, completedCount: 1 },
  systems: [
    { id: "TrafficSystem", ownerAssembly: "Game", currentMilliseconds: 3.4, meanMilliseconds: null, p95Milliseconds: null, p99Milliseconds: null, maxMilliseconds: null, calls: null, confidence: "Managed", patchOwners: ["TrafficTweaks"] }
  ],
  mods: [
    { assemblyName: "TrafficTweaks", directSystemMilliseconds: 1.2, directSystemCount: 2, patchedVanillaSystemCount: 1 }
  ],
  pathfinding: { metrics: [] },
  domainMetrics: [], timeline: [], captures: [],
  diagnostics: { profilerOverheadShare: 0.02, unattributedJobsMilliseconds: 4.1, messages: [] }
};

describe("core profiler tabs", () => {
  it("overview shows Japanese status text and formats profiler units truthfully", () => {
    const html = renderToStaticMarkup(<OverviewTab snapshot={snapshot} onManualCapture={() => {}} onExport={() => {}} exportResult="" />);
    expect(html).toContain("4×");
    expect(html).toContain("2×");
    expect(html).toContain("50.0%");
    expect(html).toContain("監視中");
    expect(html).toContain("12.50 ms");
    expect(html).toContain("not exposed");
  });

  it("systems view keeps owner and patch metadata separate", () => {
    const html = renderToStaticMarkup(<SystemsTab systems={snapshot.systems} />);
    expect(html).toContain("TrafficSystem");
    expect(html).toContain("Game");
    expect(html).toContain("TrafficTweaks");
    expect(html).toContain("管理コード");
  });

  it("mods view reports direct ownership and patched-system metadata without blame wording", () => {
    const html = renderToStaticMarkup(<ModsTab mods={snapshot.mods} />);
    expect(html).toContain("TrafficTweaks");
    expect(html).toContain("1.20 ms");
    expect(html).toContain("パッチ対象システム");
    expect(html.toLowerCase()).not.toContain("worst");
  });
});
