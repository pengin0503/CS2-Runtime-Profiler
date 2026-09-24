import React from "react";
import type { UiMetricRow, UiSnapshot } from "../bindings";
import { formatMetricValue, formatPercent, formatSpeed, shortMetricName } from "../format";
import { MetricBadge } from "../components/MetricBadge";
import styles from "../profiler.module.scss";

interface OverviewTabProps {
  snapshot: UiSnapshot;
  onManualCapture: () => void;
  onExport: () => void;
  exportResult: string;
}

function MetricRow({ metric }: { metric: UiMetricRow }) {
  return (
    <div className={styles.metricRow}>
      <div className={styles.metricIdentity}>
        <strong>{shortMetricName(metric.id)}</strong>
        <small>{metric.id.includes("\u001f") ? metric.id.split("\u001f")[0] : "Runtime"}</small>
      </div>
      <span className={styles.metricValue}>{formatMetricValue(metric)}</span>
      <MetricBadge confidence={metric.confidence} availability={metric.availability} reason={metric.reason} />
    </div>
  );
}

export function OverviewTab({ snapshot, onManualCapture, onExport, exportResult }: OverviewTabProps) {
  const pathfinding = snapshot.pathfinding?.metrics ?? [];
  const recorders = snapshot.global?.recorderMetrics ?? [];

  return (
    <div className={styles.tabBody}>
      <section className={styles.summaryGrid}>
        <div className={styles.summaryCard}><span>Selected speed</span><strong>{formatSpeed(snapshot.global.selectedSpeed)}</strong></div>
        <div className={styles.summaryCard}><span>Actual speed</span><strong>{formatSpeed(snapshot.global.actualSpeed)}</strong></div>
        <div className={styles.summaryCard}><span>Simulation efficiency</span><strong>{formatPercent(snapshot.global.efficiency)}</strong></div>
        <div className={styles.summaryCard}><span>Capture state</span><strong>{snapshot.capture.isDeepCapture ? "Deep Capture" : snapshot.capture.state}</strong></div>
        <div className={styles.summaryCard}><span>Profiler overhead</span><strong>{formatPercent(snapshot.diagnostics.profilerOverheadShare)}</strong></div>
        <div className={styles.summaryCard} title="No explicit FPS field is currently emitted by the C# snapshot."><span>FPS</span><strong>—</strong><small>Unavailable</small></div>
      </section>

      <div className={styles.actionRow}>
        <button type="button" onClick={onManualCapture} disabled={snapshot.capture.isDeepCapture}>Manual Deep Capture</button>
        <button type="button" onClick={onExport}>Export JSON</button>
        {exportResult && <span className={styles.exportResult}>{exportResult}</span>}
      </div>

      <section>
        <h3>Global recorder metrics</h3>
        {recorders.length ? recorders.map(metric => <MetricRow key={metric.id} metric={metric} />) : <p className={styles.empty}>No global recorder metrics are currently exposed.</p>}
      </section>

      <section>
        <h3>Pathfinding summary</h3>
        {pathfinding.length ? pathfinding.slice(0, 6).map(metric => <MetricRow key={metric.id} metric={metric} />) : <p className={styles.empty}>Pathfinding counters are unavailable until a compatible runtime member is discovered.</p>}
      </section>
    </div>
  );
}
