import React, { useMemo, useState } from "react";
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

export function SystemsTab({ systems, onSelect }: SystemsTabProps) {
  const [sortKey, setSortKey] = useState<SortKey>("current");
  const rows = useMemo(() => [...systems].sort((a, b) => {
    const av = sortValue(a, sortKey);
    const bv = sortValue(b, sortKey);
    if (typeof av === "string" && typeof bv === "string") return av.localeCompare(bv);
    return Number(bv) - Number(av);
  }), [systems, sortKey]);

  if (!rows.length) return <p className={styles.empty}>Per-system timing is unavailable until a timing-capable capture is produced.</p>;

  return (
    <div className={styles.tableWrap}>
      <table className={styles.table}>
        <thead><tr>
          <th><button onClick={() => setSortKey("name")}>System</button></th>
          <th>Owner</th>
          <th><button onClick={() => setSortKey("current")}>Current</button></th>
          <th><button onClick={() => setSortKey("mean")}>Mean</button></th>
          <th><button onClick={() => setSortKey("p95")}>P95</button></th>
          <th><button onClick={() => setSortKey("p99")}>P99</button></th>
          <th><button onClick={() => setSortKey("max")}>Max</button></th>
          <th><button onClick={() => setSortKey("calls")}>Calls</button></th>
          <th>Evidence</th>
        </tr></thead>
        <tbody>{rows.map(row => (
          <tr key={row.id} onClick={() => onSelect?.(row.id)}>
            <td>
              <details>
                <summary>{row.id}</summary>
                <div className={styles.details}>
                  <div>Source: managed system timing boundary</div>
                  <div>Patch owners: {row.patchOwners.length ? row.patchOwners.join(", ") : "none discovered"}</div>
                  <div>Marker/cadence detail: not exposed by the current snapshot.</div>
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
