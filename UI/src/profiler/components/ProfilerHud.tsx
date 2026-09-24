import React from "react";
import { Button, Tooltip } from "cs2/ui";
import type { UiSnapshot } from "../bindings";
import { formatSpeed } from "../format";
import { captureStateLabel } from "../text";
import styles from "../profiler.module.scss";

interface ProfilerHudProps {
  snapshot: UiSnapshot;
  panelVisible: boolean;
  onToggle: () => void;
}

const PROFILER_ICON = "data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 32 32'%3E%3Cpath fill='white' d='M5 25h4V14H5v11Zm9 0h4V7h-4v18Zm9 0h4V11h-4v14Z'/%3E%3Cpath fill='none' stroke='white' stroke-width='2' d='M4 27h24'/%3E%3C/svg%3E";

export function ProfilerHud({ snapshot, panelVisible, onToggle }: ProfilerHudProps) {
  const tooltip = `CS2 ランタイムプロファイラーを開く\n指定速度 ${formatSpeed(snapshot.global.selectedSpeed)} / 実効速度 ${formatSpeed(snapshot.global.actualSpeed)} / ${captureStateLabel(snapshot.capture.state, snapshot.capture.isDeepCapture)}`;
  return (
    <div className={styles.hudAnchor}>
      <Tooltip tooltip={tooltip}>
        <Button
          as="button"
          variant="floating"
          src={PROFILER_ICON}
          onSelect={onToggle}
          aria-pressed={panelVisible}
          aria-label="CS2 ランタイムプロファイラー"
        />
      </Tooltip>
    </div>
  );
}
