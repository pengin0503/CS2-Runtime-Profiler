import React from "react";
import type { CaptureSummaryUi, DiagnosticsUi } from "../bindings";
import { formatMilliseconds, formatPercent } from "../format";
import styles from "../profiler.module.scss";

export function DiagnosticsTab({ diagnostics, captures }: { diagnostics: DiagnosticsUi; captures: CaptureSummaryUi[] }) {
  const latest = captures?.length ? captures[captures.length - 1] : null;
  const values: Array<[string, string | number]> = [
    ["Game version", diagnostics.gameVersion || "Unavailable"],
    ["Profiler version", diagnostics.profilerVersion || "Unavailable"],
    ["Discovered markers", diagnostics.discoveredMarkerCount],
    ["Captured markers", diagnostics.capturedMarkerCount],
    ["System rows", diagnostics.systemCount],
    ["Marker batch size", diagnostics.markerBatchSize],
    ["Sampling stride", diagnostics.samplingStride],
    ["Current profiler overhead", formatPercent(diagnostics.profilerOverheadShare)],
    ["Unattributed job time", formatMilliseconds(diagnostics.unattributedJobsMilliseconds)],
    ["Patch map", diagnostics.patchMapState || "Unavailable"]
  ];

  return (
    <div className={styles.tabBody}>
      <div className={styles.diagnosticsGrid}>
        {values.map(([name, value]) => <div className={styles.diagnosticItem} key={name}><span>{name}</span><strong>{value}</strong></div>)}
      </div>
      {latest && (
        <section>
          <h3>Latest capture coverage</h3>
          <div className={styles.captureFacts}>
            <span>Coverage <b>{formatPercent(latest.coverageRatio)}</b></span>
            <span>Mode <b>{latest.batched ? "Batched" : "Simultaneous"}</b></span>
            <span>Peak self-overhead <b>{formatPercent(latest.profilerOverheadShare)}</b></span>
          </div>
        </section>
      )}
      <section>
        <h3>Collector / compatibility notes</h3>
        {diagnostics.messages?.length
          ? diagnostics.messages.map(message => <p className={styles.diagnosticMessage} key={message}>{message}</p>)
          : <p className={styles.empty}>No diagnostic messages.</p>}
      </section>
    </div>
  );
}
