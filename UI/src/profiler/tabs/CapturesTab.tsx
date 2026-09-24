import React, { useState } from "react";
import type { CaptureSummaryUi, CorrelatedChangeUi } from "../bindings";
import { formatNumber, formatPercent, shortMetricName } from "../format";
import { MetricBadge } from "../components/MetricBadge";
import styles from "../profiler.module.scss";

function metricLabel(metric: string) {
  return shortMetricName(metric.replace(/^(recorder|marker|system):/, ""));
}

function ChangeRow({ change }: { change: CorrelatedChangeUi }) {
  const delta = `${change.delta >= 0 ? "+" : ""}${formatNumber(change.delta, 2)}`;
  return (
    <div className={styles.changeRow}>
      <span>{metricLabel(change.metric)}</span>
      <span>{formatNumber(change.before, 2)} → {formatNumber(change.after, 2)}</span>
      <b>{delta}</b>
      <MetricBadge confidence={change.confidence} availability="Available" />
    </div>
  );
}

export function CapturesTab({ captures, onSelect }: { captures: CaptureSummaryUi[]; onSelect?: (id: string) => void }) {
  const [expanded, setExpanded] = useState<string | null>(captures?.[0]?.id ?? null);
  if (!captures?.length) return <p className={styles.empty}>No Deep Capture session has completed yet.</p>;

  return (
    <div className={styles.captureList}>
      {[...captures].reverse().map(capture => {
        const open = expanded === capture.id;
        return (
          <article className={styles.captureCard} key={capture.id}>
            <button type="button" className={styles.captureHeader} onClick={() => { setExpanded(open ? null : capture.id); onSelect?.(capture.id); }}>
              <span><strong>{capture.triggerKind}</strong><small>{capture.id}</small></span>
              <span>{capture.durationSeconds.toFixed(1)} s</span>
              <span>{formatPercent(capture.coverageRatio)} coverage</span>
              <span>{capture.batched ? "Batched" : "Simultaneous"}</span>
              <span>{formatPercent(capture.profilerOverheadShare)} overhead</span>
            </button>
            {open && (
              <div className={styles.captureBody}>
                <div className={styles.captureFacts}>
                  <span>Markers <b>{capture.capturedMarkers}/{capture.discoveredMarkers}</b></span>
                  <span>Warnings <b>{capture.warningCount}</b></span>
                  <span>Trigger t <b>{capture.triggeredAtSeconds.toFixed(2)} s</b></span>
                </div>
                <h3>Strongest correlated changes</h3>
                <p className={styles.explainer}>Largest observed pre-vs-event changes. Correlation does not establish causation.</p>
                {capture.correlatedChanges?.length
                  ? capture.correlatedChanges.map(change => <ChangeRow key={change.metric} change={change} />)
                  : <p className={styles.empty}>No comparable pre/post samples were retained for this capture.</p>}
                {!!capture.warnings?.length && (
                  <div className={styles.warningBox}>
                    <strong>Capture warnings</strong>
                    {capture.warnings.map(warning => <span key={warning}>{warning}</span>)}
                  </div>
                )}
              </div>
            )}
          </article>
        );
      })}
    </div>
  );
}
