import React, { useState } from "react";
import {
  exportReport,
  requestManualCapture,
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
import styles from "./profiler.module.scss";

type CoreTab = "overview" | "systems" | "mods";

export function ProfilerRoot() {
  const visible = usePanelVisible();
  const snapshot = useProfilerSnapshot();
  const exportResult = useExportResult();
  const [tab, setTab] = useState<CoreTab>("overview");

  if (!visible) return null;

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
        <button className={tab === "overview" ? styles.activeTab : ""} onClick={() => setTab("overview")}>Overview</button>
        <button className={tab === "systems" ? styles.activeTab : ""} onClick={() => setTab("systems")}>Systems</button>
        <button className={tab === "mods" ? styles.activeTab : ""} onClick={() => setTab("mods")}>Mods</button>
      </nav>

      <main className={styles.panelBody}>
        {tab === "overview" && <OverviewTab snapshot={snapshot} onManualCapture={requestManualCapture} onExport={exportReport} exportResult={exportResult} />}
        {tab === "systems" && <SystemsTab systems={snapshot.systems} onSelect={selectSystem} />}
        {tab === "mods" && <ModsTab mods={snapshot.mods} onSelect={selectMod} />}
      </main>
    </div>
  );
}
