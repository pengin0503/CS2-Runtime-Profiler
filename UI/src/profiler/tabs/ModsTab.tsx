import React from "react";
import { Button } from "cs2/ui";
import type { ModUiRow } from "../bindings";
import { formatMilliseconds } from "../format";
import styles from "../profiler.module.scss";

interface ModsTabProps {
  mods: ModUiRow[];
  onSelect?: (id: string) => void;
}

export function ModsTab({ mods, onSelect }: ModsTabProps) {
  const rows = [...mods].sort((a, b) => b.directSystemMilliseconds - a.directSystemMilliseconds || a.assemblyName.localeCompare(b.assemblyName));
  if (!rows.length) return <p className={styles.empty}>現在のスナップショットには、MODが直接所有するシステムの計測値がありません。</p>;

  return (
    <div className={styles.tabBody}>
      <p className={styles.explainer}>直接時間には、そのアセンブリが所有するシステムだけを含めます。バニラシステムへのパッチはメタデータとして表示し、その実行時間をパッチ所有者へ付け替えません。</p>
      <div className={styles.modGrid}>
        {rows.map(row => (
          <Button as="button" variant="flat" key={row.assemblyName} className={styles.modCard} onSelect={() => onSelect?.(row.assemblyName)}>
            <strong>{row.assemblyName}</strong>
            <span>直接システム時間 <b>{formatMilliseconds(row.directSystemMilliseconds)}</b></span>
            <span>直接所有システム <b>{row.directSystemCount}</b></span>
            <span>パッチ対象システム <b>{row.patchedVanillaSystemCount}</b></span>
          </Button>
        ))}
      </div>
    </div>
  );
}
