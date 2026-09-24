import React, { useMemo, useState } from "react";
import type { TimelinePoint } from "../bindings";
import { formatNumber, shortMetricName } from "../format";
import styles from "../profiler.module.scss";

function displayMetric(metric: string): string {
  const withoutPrefix = metric.replace(/^(recorder|marker|system):/, "");
  return shortMetricName(withoutPrefix);
}

function polyline(points: TimelinePoint[], minTime: number, maxTime: number): string {
  if (!points.length) return "";
  const values = points.map(point => point.value);
  const min = Math.min(...values);
  const max = Math.max(...values);
  const timeSpan = Math.max(0.0001, maxTime - minTime);
  const valueSpan = Math.max(0.0001, max - min);
  return points.map(point => {
    const x = 28 + ((point.timestampSeconds - minTime) / timeSpan) * 944;
    const y = max === min ? 130 : 232 - ((point.value - min) / valueSpan) * 204;
    return `${x.toFixed(1)},${y.toFixed(1)}`;
  }).join(" ");
}

export function TimelineTab({ points }: { points: TimelinePoint[] }) {
  const series = useMemo(() => Array.from(new Set((points ?? []).map(point => point.metric))).sort(), [points]);
  const [hidden, setHidden] = useState<Set<string>>(() => new Set());
  const [selectedTime, setSelectedTime] = useState<number | null>(null);

  const visible = series.filter(metric => !hidden.has(metric));
  const minTime = points?.length ? Math.min(...points.map(point => point.timestampSeconds)) : 0;
  const maxTime = points?.length ? Math.max(...points.map(point => point.timestampSeconds)) : 1;

  const toggle = (metric: string) => setHidden(previous => {
    const next = new Set(previous);
    if (next.has(metric)) next.delete(metric); else next.add(metric);
    return next;
  });

  if (!points?.length) {
    return <p className={styles.empty}>No retained timeline history is available yet. Series are never synthesized from point-in-time counters.</p>;
  }

  const selected = selectedTime == null
    ? []
    : points.filter(point => Math.abs(point.timestampSeconds - selectedTime) < 0.0001);

  return (
    <div className={styles.tabBody}>
      <div className={styles.seriesControls}>
        {series.map(metric => (
          <label key={metric} className={styles.seriesToggle}>
            <input type="checkbox" checked={!hidden.has(metric)} onChange={() => toggle(metric)} />
            <span>{displayMetric(metric)}</span>
          </label>
        ))}
      </div>

      <div className={styles.chartWrap}>
        <svg className={styles.timelineChart} viewBox="0 0 1000 260" role="img" aria-label="Profiler timeline">
          <line x1="28" y1="232" x2="972" y2="232" className={styles.chartAxis} />
          <line x1="28" y1="28" x2="28" y2="232" className={styles.chartAxis} />
          {visible.map(metric => {
            const metricPoints = points.filter(point => point.metric === metric).sort((a, b) => a.timestampSeconds - b.timestampSeconds);
            return (
              <g key={metric} data-series={metric}>
                <polyline points={polyline(metricPoints, minTime, maxTime)} fill="none" className={styles.chartLine} />
                {metricPoints.map((point, index) => {
                  const coords = polyline([point], point.timestampSeconds, point.timestampSeconds).split(",");
                  const x = 28 + ((point.timestampSeconds - minTime) / Math.max(0.0001, maxTime - minTime)) * 944;
                  return (
                    <circle key={`${point.timestampSeconds}-${index}`} cx={x} cy={coords[1] ?? 130} r="5" className={styles.chartPoint} onClick={() => setSelectedTime(point.timestampSeconds)}>
                      <title>{`${displayMetric(metric)} @ ${point.timestampSeconds.toFixed(2)}s = ${formatNumber(point.value, 2)} (${point.confidence})`}</title>
                    </circle>
                  );
                })}
              </g>
            );
          })}
        </svg>
      </div>
      <p className={styles.chartNote}>Each series is normalized to its own observed range for shape comparison; raw values remain available on point hover/click.</p>
      {selectedTime != null && (
        <div className={styles.selectedPoint}>
          <strong>t = {selectedTime.toFixed(2)} s</strong>
          {selected.map(point => <span key={`${point.metric}-${point.value}`}>{displayMetric(point.metric)}: {formatNumber(point.value, 2)} ({point.confidence})</span>)}
        </div>
      )}
    </div>
  );
}
