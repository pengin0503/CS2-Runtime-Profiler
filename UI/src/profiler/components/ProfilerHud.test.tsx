import React from "react";
import { renderToStaticMarkup } from "react-dom/server";
import { expect, it } from "vitest";
import { ProfilerHud } from "./ProfilerHud";

it("renders a native floating launcher with a Japanese status tooltip", () => {
  const html = renderToStaticMarkup(
    <ProfilerHud
      snapshot={{ global: { available: true, selectedSpeed: 4, actualSpeed: 2.5, efficiency: 0.625 }, capture: { state: "DeepCapture", isDeepCapture: true, completedCount: 0 } } as any}
      panelVisible={false}
      onToggle={() => {}}
    />
  );
  expect(html).toContain('data-variant="floating"');
  expect(html).toContain("CS2 ランタイムプロファイラー");
  expect(html).toContain("指定速度 4×");
  expect(html).toContain("詳細キャプチャ中");
});
