import React from "react";
import type { UiMetricRow } from "../bindings";
import { formatMetricValue, shortMetricName } from "../format";
import { MetricBadge } from "../components/MetricBadge";
import styles from "../profiler.module.scss";

export function PathfindingTab({ metrics }: { metrics: UiMetricRow[] }) {
  if (!metrics?.length) {
    return <p className={styles.empty}>現在のランタイムから検証済みの経路探索カウンターを取得できません。</p>;
  }

  return (
    <div className={styles.tabBody}>
      <p className={styles.explainer}>これらは補助的な指標です。キューの増加や処理量の変化だけで原因と断定しません。</p>
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
