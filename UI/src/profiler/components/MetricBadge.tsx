import React from "react";
import styles from "../profiler.module.scss";

interface MetricBadgeProps {
  confidence: string;
  availability: string;
  reason?: string | null;
}

export function MetricBadge({ confidence, availability, reason }: MetricBadgeProps) {
  const unavailable = availability !== "Available" || confidence === "Unavailable";
  const label = unavailable ? "Unavailable" : confidence || "Unavailable";
  const className = unavailable
    ? styles.badgeUnavailable
    : label === "Full"
      ? styles.badgeFull
      : label === "Managed"
        ? styles.badgeManaged
        : styles.badgeIndirect;

  const tooltip = unavailable
    ? reason || "This metric is not available on the current runtime."
    : label === "Full"
      ? "Direct profiler evidence from the exposed runtime metric."
      : label === "Managed"
        ? "Measured at a managed-code boundary; worker/job cost may remain unattributed."
        : "Indirect evidence derived from a supported runtime counter.";

  return <span className={`${styles.badge} ${className}`} title={tooltip}>{label}</span>;
}
