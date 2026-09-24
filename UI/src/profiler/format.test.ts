import { describe, expect, it } from "vitest";
import { formatMetricValue, formatPercent, shortMetricName } from "./format";

describe("metric formatting", () => {
  it("never renders unavailable metrics as zero", () => {
    expect(formatMetricValue({ value: 0, availability: "Unavailable" })).toBe("—");
    expect(formatMetricValue({ value: null, availability: "Available" })).toBe("—");
  });

  it("formats ratios as percentages", () => {
    expect(formatPercent(0.875)).toBe("87.5%");
    expect(formatPercent(null)).toBe("—");
  });

  it("removes profiler category separators from marker names", () => {
    expect(shortMetricName("CPU\u001fMain Thread")).toBe("Main Thread");
  });
});
