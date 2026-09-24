import React, { useState } from "react";
import {
  exportReport,
  requestManualCapture,
  selectCapture,
  selectMod,
  selectSystem,
  togglePanel,
  useExportResult,
  usePanelVisible,
  useProfilerSnapshot
} from "./bindings";
import { OverviewTab } from "./tabs/OverviewTab";
import { SystemsTab } from "./tabs/SystemsTab";
import { ModsTab } from "./tabs/ModsTab";
import { PathfindingTab } from "./tabs/PathfindingTab";
import { TimelineTab } from "./tabs/TimelineTab";
import { CapturesTab } from "./tabs/CapturesTab";
import { DiagnosticsTab } from "./tabs/DiagnosticsTab";
import styles from "./profiler.module.scss";

type ProfilerTab = "overview" | "systems" | "mods" | "pathfinding" | "timeline" | "captures" | "diagnostics";

export function ProfilerRoot() {
  const visible = usePanelVisible();
  const snapshot = useProfilerSnapshot();
  const exportResult = useExportResult();
  const [tab, setTab] = useState<ProfilerTab>("overview");

  if (!visible) return null;

  const tabs: Array<[ProfilerTab, string]> = [
    ["overview", "Overview"],
    ["systems", "Systems"],
    ["mods", "Mods"],
    ["pathfinding", "Pathfinding"],
    ["timeline", "Timeline"],
    ["captures", "Captures"],
    ["diagnostics", "Diagnostics"]
  ];

  return (
    <div className={styles.panel} role="dialog" aria-label="CS2 Runtime Profiler">
      <header className={styles.panelHeader}>
        <div>
          <strong>CS2 Runtime Profiler</strong>
          <small>{snapshot.capture.isDeepCapture ? "Deep Capture active" : "Low-overhead monitoring"}</small>
        </div>
        <button type="button" className={styles.closeButton} onClick={togglePanel} aria-label="Close profiler">×</button>
      </header>

      <nav className={styles.tabs} aria-label="Profiler views">
        {tabs.map(([id, label]) => <button key={id} className={tab === id ? styles.activeTab : ""} onClick={() => setTab(id)}>{label}</button>)}
      </nav>

      <main className={styles.panelBody}>
        {tab === "overview" && <OverviewTab snapshot={snapshot} onManualCapture={requestManualCapture} onExport={exportReport} exportResult={exportResult} />}
        {tab === "systems" && <SystemsTab systems={snapshot.systems} onSelect={selectSystem} />}
        {tab === "mods" && <ModsTab mods={snapshot.mods} onSelect={selectMod} />}
        {tab === "pathfinding" && <PathfindingTab metrics={snapshot.pathfinding.metrics} />}
        {tab === "timeline" && <TimelineTab points={snapshot.timeline} />}
        {tab === "captures" && <CapturesTab captures={snapshot.captures} onSelect={selectCapture} />}
        {tab === "diagnostics" && <DiagnosticsTab diagnostics={snapshot.diagnostics} captures={snapshot.captures} />}
      </main>
    </div>
  );
}
