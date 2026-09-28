import React, { useMemo, useState } from "react";
import { Button } from "cs2/ui";
import type { SystemUiRow } from "../bindings";
import { formatInteger, formatMilliseconds } from "../format";
import { MetricBadge } from "../components/MetricBadge";
import styles from "../profiler.module.scss";

type SortKey = "current" | "mean" | "p95" | "p99" | "max" | "calls" | "name";

interface SystemsTabProps {
  systems: SystemUiRow[];
  onSelect?: (id: string) => void;
}

function sortValue(row: SystemUiRow, key: SortKey): number | string {
  switch (key) {
    case "current": return row.currentMilliseconds;
    case "mean": return row.meanMilliseconds ?? -1;
    case "p95": return row.p95Milliseconds ?? -1;
    case "p99": return row.p99Milliseconds ?? -1;
    case "max": return row.maxMilliseconds ?? -1;
    case "calls": return row.calls ?? -1;
    case "name": return row.id;
  }
}

function measurementSource(confidence: string): string {
  switch (confidence) {
    case "Full": return "Unity ECS プロファイラーマーカー";
    case "Managed": return "管理システムの実行境界";
    default: return "不明";
  }
}

export function SystemsTab({ systems, onSelect }: SystemsTabProps) {
  const [sortKey, setSortKey] = useState<SortKey>("current");
  const rows = useMemo(() => [...systems].sort((a, b) => {
    const av = sortValue(a, sortKey);
    const bv = sortValue(b, sortKey);
    if (typeof av === "string" && typeof bv === "string") return av.localeCompare(bv);
    return Number(bv) - Number(av);
  }), [systems, sortKey]);

  if (!rows.length) return <p className={styles.empty}>システム別の実行時間は、対応する詳細キャプチャが作成されるまで利用できません。</p>;

  return (
    <div className={styles.tableWrap}>
      <table className={styles.table}>
        <thead><tr>
          <th><Button as="button" variant="flat" onSelect={() => setSortKey("name")}>システム</Button></th>
          <th>所有元</th>
          <th><Button as="button" variant="flat" onSelect={() => setSortKey("current")}>現在</Button></th>
          <th><Button as="button" variant="flat" onSelect={() => setSortKey("mean")}>平均</Button></th>
          <th><Button as="button" variant="flat" onSelect={() => setSortKey("p95")}>P95</Button></th>
          <th><Button as="button" variant="flat" onSelect={() => setSortKey("p99")}>P99</Button></th>
          <th><Button as="button" variant="flat" onSelect={() => setSortKey("max")}>最大</Button></th>
          <th><Button as="button" variant="flat" onSelect={() => setSortKey("calls")}>呼出回数</Button></th>
          <th>根拠</th>
        </tr></thead>
        <tbody>{rows.map(row => (
          <tr key={row.id} onClick={() => onSelect?.(row.id)}>
            <td>
              <details>
                <summary>{row.id}</summary>
                <div className={styles.details}>
                  <div>測定元: {measurementSource(row.confidence)}</div>
                  <div>パッチ所有者: {row.patchOwners.length ? row.patchOwners.join(", ") : "検出なし"}</div>
                  <div>マーカーや更新間隔の詳細は現在のスナップショットでは公開されていません。</div>
                </div>
              </details>
            </td>
            <td>{row.ownerAssembly || "—"}</td>
            <td>{formatMilliseconds(row.currentMilliseconds)}</td>
            <td>{formatMilliseconds(row.meanMilliseconds)}</td>
            <td>{formatMilliseconds(row.p95Milliseconds)}</td>
            <td>{formatMilliseconds(row.p99Milliseconds)}</td>
            <td>{formatMilliseconds(row.maxMilliseconds)}</td>
            <td>{formatInteger(row.calls)}</td>
            <td><MetricBadge confidence={row.confidence} availability={row.confidence === "Unavailable" ? "Unavailable" : "Available"} /></td>
          </tr>
        ))}</tbody>
      </table>
    </div>
  );
}
