import React from "react";
import type { UiSnapshot } from "../bindings";
import { formatSpeed } from "../format";
import styles from "../profiler.module.scss";

interface ProfilerHudProps {
  snapshot: UiSnapshot;
  panelVisible: boolean;
  onToggle: () => void;
}

export function ProfilerHud({ snapshot, panelVisible, onToggle }: ProfilerHudProps) {
  const captureLabel = snapshot.capture.isDeepCapture ? "Deep Capture" : snapshot.capture.state;
  return (
    <button
      type="button"
      className={`${styles.hud} ${snapshot.capture.isDeepCapture ? styles.hudCapture : ""}`}
      onClick={onToggle}
      aria-pressed={panelVisible}
      title="Open CS2 Runtime Profiler"
    >
      <span className={styles.hudTitle}>Profiler</span>
      <span>Sel {formatSpeed(snapshot.global.selectedSpeed)}</span>
      <span>Act {formatSpeed(snapshot.global.actualSpeed)}</span>
      <span className={styles.hudState}>{captureLabel}</span>
    </button>
  );
}
