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

export function formatBytes(value: number | null | undefined): string {
  if (value == null || !Number.isFinite(value)) return "—";
  const absolute = Math.abs(value);
  if (absolute >= 1024 ** 3) return `${(value / (1024 ** 3)).toFixed(2)} GiB`;
  if (absolute >= 1024 ** 2) return `${(value / (1024 ** 2)).toFixed(2)} MiB`;
  if (absolute >= 1024) return `${(value / 1024).toFixed(2)} KiB`;
  return `${Math.round(value)} B`;
}

export function formatMetricValue(metric: Pick<UiMetricRow, "value" | "availability"> & { unitType?: string }): string {
  if (metric.availability !== "Available" || metric.value == null || !Number.isFinite(metric.value)) return "—";
  if (metric.unitType === "TimeNanoseconds") return formatMilliseconds(metric.value / 1_000_000);
  if (metric.unitType === "Bytes") return formatBytes(metric.value);
  return formatNumber(metric.value, 2);
}

export function shortMetricName(id: string): string {
  const parts = id.split("\u001f");
  return parts[parts.length - 1] || id;
}
