import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it } from "vitest";
import { ProfilerHud } from "./ProfilerHud";

it("renders compact simulation and capture state", () => {
  const html = renderToStaticMarkup(
    <ProfilerHud
      snapshot={{ global: { available: true, selectedSpeed: 4, actualSpeed: 2.5, efficiency: 0.625 }, capture: { state: "DeepCapture", isDeepCapture: true, completedCount: 0 } } as any}
      panelVisible={false}
      onToggle={() => {}}
    />
  );
  expect(html).toContain("4×");
  expect(html).toContain("2.5×");
  expect(html).toContain("Deep Capture");
});
