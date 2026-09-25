import fs from "node:fs";
import path from "node:path";
import { describe, expect, it } from "vitest";

const stylesheet = fs.readFileSync(
  path.resolve(process.cwd(), "src/profiler/profiler.module.scss"),
  "utf8"
);

describe("Gameface runtime compatibility", () => {
  it("keeps the Game-mounted profiler panel pointer-interactive", () => {
    const panelRule = stylesheet.match(/\.panel\s*\{([\s\S]*?)\}/)?.[1] ?? "";
    expect(panelRule).toMatch(/pointer-events\s*:\s*auto\s*;/);
  });

  it("avoids CSS syntax rejected by the CS2 Gameface runtime", () => {
    const rejectedPatterns: Array<[string, RegExp]> = [
      ["CSS Grid", /display\s*:\s*grid\b/i],
      ["inline-flex", /display\s*:\s*inline-flex\b/i],
      ["fractional grid units", /\b\d+(?:\.\d+)?fr\b/i],
      ["CSS min()", /\bmin\s*\(/i],
      [":disabled pseudo-class", /:disabled\b/i],
      ["inherit color value", /color\s*:\s*inherit\b/i],
      ["inherit font-size value", /font-size\s*:\s*inherit\b/i],
      ["background shorthand", /(^|[;{]\s*)background\s*:/im],
      ["border shorthand", /(^|[;{]\s*)border(?:-(?:top|right|bottom|left))?\s*:/im]
    ];

    for (const [name, pattern] of rejectedPatterns) {
      expect(stylesheet, `${name} should not be present`).not.toMatch(pattern);
    }
  });
});
