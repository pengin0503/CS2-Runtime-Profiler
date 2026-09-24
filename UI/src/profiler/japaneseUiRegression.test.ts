import { existsSync, readFileSync } from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";
import { formatMetricValue } from "./format";

describe("Japanese profiler UI regression coverage", () => {
  it("formats profiler time and memory readings using their Unity recorder units", () => {
    expect(formatMetricValue({ value: 16_521_800, availability: "Available", unitType: "TimeNanoseconds" } as any)).toBe("16.52 ms");
    expect(formatMetricValue({ value: 4_559_608_748, availability: "Available", unitType: "Bytes" } as any)).toBe("4.25 GiB");
  });

  it("uses Japanese labels and CS2-native select events for the main tabs", () => {
    const source = readFileSync(new URL("./ProfilerRoot.tsx", import.meta.url), "utf8");
    for (const label of ["概要", "システム", "MOD", "経路探索", "タイムライン", "キャプチャ", "診断"]) {
      expect(source).toContain(label);
    }
    expect(source).toContain('from "cs2/ui"');
    expect(source).toContain("onSelect={() => setTab(id)}");
  });

  it("renders the top-left launcher as a floating CS2 icon button", () => {
    const source = readFileSync(new URL("./components/ProfilerHud.tsx", import.meta.url), "utf8");
    expect(source).toContain('variant="floating"');
    expect(source).toContain("onSelect={onToggle}");
    expect(source).not.toContain(">Profiler<");
  });

  it("registers Japanese option localization through the game localization manager", () => {
    const localePath = path.resolve(process.cwd(), "../src/CS2RuntimeProfiler/Localization/LocaleJA.cs");
    expect(existsSync(localePath)).toBe(true);
    if (!existsSync(localePath)) return;

    const locale = readFileSync(localePath, "utf8");
    expect(locale).toContain("GetSettingsLocaleID");
    expect(locale).toContain("GetOptionLabelLocaleID");
    expect(locale).toContain("GetOptionDescLocaleID");
    expect(locale).toContain("ランタイムプロファイラー");
    expect(locale).toContain("監視を有効化");

    const mod = readFileSync(path.resolve(process.cwd(), "../src/CS2RuntimeProfiler/Mod.cs"), "utf8");
    expect(mod).toContain('localizationManager.AddSource("ja-JP"');
  });
});
