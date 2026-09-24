import React from "react";
import type { ModUiRow } from "../bindings";
import { formatMilliseconds } from "../format";
import styles from "../profiler.module.scss";

interface ModsTabProps {
  mods: ModUiRow[];
  onSelect?: (id: string) => void;
}

export function ModsTab({ mods, onSelect }: ModsTabProps) {
  const rows = [...mods].sort((a, b) => b.directSystemMilliseconds - a.directSystemMilliseconds || a.assemblyName.localeCompare(b.assemblyName));
  if (!rows.length) return <p className={styles.empty}>No directly-owned mod/system timing is available in the current snapshot.</p>;

  return (
    <div className={styles.tabBody}>
      <p className={styles.explainer}>Direct totals include only systems owned by that assembly. Patched vanilla systems are metadata only; their runtime cost is not reassigned to the patch owner.</p>
      <div className={styles.modGrid}>
        {rows.map(row => (
          <button key={row.assemblyName} type="button" className={styles.modCard} onClick={() => onSelect?.(row.assemblyName)}>
            <strong>{row.assemblyName}</strong>
            <span>Direct system time <b>{formatMilliseconds(row.directSystemMilliseconds)}</b></span>
            <span>Direct systems <b>{row.directSystemCount}</b></span>
            <span>Patched systems <b>{row.patchedVanillaSystemCount}</b></span>
          </button>
        ))}
      </div>
    </div>
  );
}
