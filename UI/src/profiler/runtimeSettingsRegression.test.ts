import { readFileSync } from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

function repoFile(relativePath: string): string {
  return readFileSync(path.resolve(process.cwd(), "..", relativePath), "utf8");
}

describe("runtime settings and panel ergonomics", () => {
  it("exposes display, capture, sampling and advanced settings through the native options UI", () => {
    const source = repoFile("src/CS2RuntimeProfiler/Setting.cs");

    for (const property of [
      "EnableMonitoring",
      "EnableAutomaticCapture",
      "UiScalePercent",
      "UiRefreshMilliseconds",
      "SamplingIntervalMilliseconds",
      "EfficiencyThresholdPercent",
      "LowEfficiencySustainSeconds",
      "PrebufferSeconds",
      "DeepCaptureSeconds",
      "PostbufferSeconds",
      "CooldownSeconds",
      "MaxConcurrentMarkers",
      "ProfilerOverheadLimitPercent",
      "MaxCompletedCaptures"
    ]) {
      expect(source).toContain(property);
    }

    expect(source).toContain("SettingsUISlider");
    expect(source).toContain("SettingsUIAdvanced");
  });

  it("keeps the panel body inside a definite flex scroll viewport", () => {
    const stylesheet = readFileSync(new URL("./profiler.module.scss", import.meta.url), "utf8");
    const panelBody = stylesheet.match(/\.panelBody\s*\{([\s\S]*?)\}/)?.[1] ?? "";

    expect(panelBody).toMatch(/flex\s*:\s*1\s+1\s+0\s*;/);
    expect(panelBody).toMatch(/min-height\s*:\s*0\s*;/);
    expect(panelBody).toMatch(/overflow-y\s*:\s*auto\s*;/);
  });

  it("binds the configured UI scale into the profiler panel", () => {
    const bindings = readFileSync(new URL("./bindings.ts", import.meta.url), "utf8");
    const root = readFileSync(new URL("./ProfilerRoot.tsx", import.meta.url), "utf8");

    expect(bindings).toContain('bindValue<number>(GROUP, "uiScalePercent"');
    expect(bindings).toContain("useUiScalePercent");
    expect(root).toContain("useUiScalePercent");
    expect(root).toContain("transform: `scale(${scale})`");
  });

  it("localizes the new option groups and user-facing controls in Japanese", () => {
    const locale = repoFile("src/CS2RuntimeProfiler/Localization/LocaleJA.cs");

    for (const text of [
      "表示",
      "キャプチャ",
      "高度な設定",
      "UI倍率",
      "詳細キャプチャ時間",
      "通常監視のサンプリング間隔",
      "同時プロファイラーマーカー数"
    ]) {
      expect(locale).toContain(text);
    }
  });
});
