import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { MetricBadge } from "./MetricBadge";

describe("MetricBadge", () => {
  it("shows confidence and preserves unavailable reason", () => {
    const html = renderToStaticMarkup(<MetricBadge confidence="Unavailable" availability="Unavailable" reason="member missing" />);
    expect(html).toContain("Unavailable");
    expect(html).toContain("member missing");
  });

  it("shows managed and indirect evidence labels verbatim", () => {
    expect(renderToStaticMarkup(<MetricBadge confidence="Managed" availability="Available" />)).toContain("Managed");
    expect(renderToStaticMarkup(<MetricBadge confidence="Indirect" availability="Available" />)).toContain("Indirect");
  });
});
