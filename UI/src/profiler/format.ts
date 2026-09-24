import type { UiMetricRow } from "./bindings";

export function formatNumber(value: number | null | undefined, digits = 2): string {
  if (value == null || !Number.isFinite(value)) return "—";
  return value.toFixed(digits);
}

export function formatMilliseconds(value: number | null | undefined): string {
  const formatted = formatNumber(value, 2);
  return formatted === "—" ? formatted : `${formatted} ms`;
}

export function formatSpeed(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) return "—";
  const digits = Number.isInteger(value) ? 0 : Math.abs(value) >= 10 ? 1 : 2;
  return `${Number(value.toFixed(digits))}×`;
}

export function formatPercent(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) return "—";
  return `${(value * 100).toFixed(1)}%`;
}

export function formatInteger(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) return "—";
  return Math.round(value).toLocaleString();
}

export function formatMetricValue(metric: Pick<UiMetricRow, "value" | "availability">): string {
  if (metric.availability !== "Available" || metric.value == null || !Number.isFinite(metric.value)) return "—";
  return formatNumber(metric.value, 2);
}

export function shortMetricName(id: string): string {
  const parts = id.split("\u001f");
  return parts[parts.length - 1] || id;
}
