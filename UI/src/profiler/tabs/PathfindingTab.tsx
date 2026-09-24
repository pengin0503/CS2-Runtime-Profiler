import React from "react";
import type { UiMetricRow } from "../bindings";
import { formatMetricValue, shortMetricName } from "../format";
import { MetricBadge } from "../components/MetricBadge";
import styles from "../profiler.module.scss";

export function PathfindingTab({ metrics }: { metrics: UiMetricRow[] }) {
  if (!metrics?.length) {
    return <p className={styles.empty}>No verified pathfinding counters are exposed by the current runtime.</p>;
  }

  return (
    <div className={styles.tabBody}>
      <p className={styles.explainer}>These counters are supporting evidence. Queue growth or throughput changes are not automatically classified as a cause.</p>
      <div className={styles.metricList}>
        {metrics.map(metric => (
          <div className={styles.metricRow} key={metric.id}>
            <div className={styles.metricIdentity}>
              <strong>{shortMetricName(metric.id)}</strong>
              {metric.reason && <small title={metric.reason}>{metric.reason}</small>}
            </div>
            <span className={styles.metricValue}>{formatMetricValue(metric)}</span>
            <MetricBadge confidence={metric.confidence} availability={metric.availability} reason={metric.reason} />
          </div>
        ))}
      </div>
    </div>
  );
}
