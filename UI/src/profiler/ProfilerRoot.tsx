import React, { useState } from "react";
import { Button } from "cs2/ui";
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
import { captureStateLabel } from "./text";
import styles from "./profiler.module.scss";

type ProfilerTab = "overview" | "systems" | "mods" | "pathfinding" | "timeline" | "captures" | "diagnostics";

export function ProfilerRoot() {
  const visible = usePanelVisible();
  const snapshot = useProfilerSnapshot();
  const exportResult = useExportResult();
  const [tab, setTab] = useState<ProfilerTab>("overview");

  if (!visible) return null;

  const tabs: Array<[ProfilerTab, string]> = [
    ["overview", "概要"],
    ["systems", "システム"],
    ["mods", "MOD"],
    ["pathfinding", "経路探索"],
    ["timeline", "タイムライン"],
    ["captures", "キャプチャ"],
    ["diagnostics", "診断"]
  ];

  return (
    <div className={styles.panel} role="dialog" aria-label="CS2 ランタイムプロファイラー">
      <header className={styles.panelHeader}>
        <div>
          <strong>CS2 ランタイムプロファイラー</strong>
          <small>{snapshot.capture.isDeepCapture ? "詳細キャプチャ実行中" : `低負荷監視・${captureStateLabel(snapshot.capture.state)}`}</small>
        </div>
        <Button as="button" variant="flat" className={styles.closeButton} onSelect={togglePanel} aria-label="プロファイラーを閉じる">×</Button>
      </header>

      <nav className={styles.tabs} aria-label="プロファイラー表示切替">
        {tabs.map(([id, label]) => (
          <Button
            as="button"
            variant="flat"
            key={id}
            selected={tab === id}
            className={`${styles.tabButton} ${tab === id ? styles.activeTab : ""}`}
            onSelect={() => setTab(id)}
          >
            {label}
          </Button>
        ))}
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
