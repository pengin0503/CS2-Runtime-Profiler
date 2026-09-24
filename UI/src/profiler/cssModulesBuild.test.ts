import os from "node:os";
import path from "node:path";
import { createRequire } from "node:module";
import webpack, { type Configuration, type Stats } from "webpack";
import { describe, expect, it } from "vitest";

const require = createRequire(import.meta.url);

function compile(config: Configuration): Promise<Stats> {
  return new Promise((resolve, reject) => {
    webpack(config, (error, stats) => {
      if (error) {
        reject(error);
        return;
      }

      if (!stats) {
        reject(new Error("Webpack completed without returning compilation stats."));
        return;
      }

      resolve(stats);
    });
  });
}

describe("CSS Modules build", () => {
  it("does not emit missing default-export warnings for profiler.module.scss", async () => {
    const previousUserDataPath = process.env.CSII_USERDATAPATH;
    process.env.CSII_USERDATAPATH = path.join(os.tmpdir(), "cs2-runtime-profiler-ui-vitest");

    try {
      const configPath = require.resolve("../../webpack.config.js");
      delete require.cache[configPath];
      const config = require(configPath) as Configuration;
      const stats = await compile(config);
      const result = stats.toJson({ all: false, errors: true, warnings: true });

      expect(result.errors ?? []).toEqual([]);

      const cssModuleWarnings = (result.warnings ?? [])
        .map((warning) => (typeof warning === "string" ? warning : warning.message ?? String(warning)))
        .filter((message) => message.includes("export 'default'") && message.includes("profiler.module.scss"));

      expect(cssModuleWarnings).toEqual([]);
    } finally {
      if (previousUserDataPath === undefined) {
        delete process.env.CSII_USERDATAPATH;
      } else {
        process.env.CSII_USERDATAPATH = previousUserDataPath;
      }
    }
  }, 20_000);

  it("emits the profiler stylesheet and exports hasCSS for the CS2 UI loader", async () => {
    const previousUserDataPath = process.env.CSII_USERDATAPATH;
    process.env.CSII_USERDATAPATH = path.join(os.tmpdir(), "cs2-runtime-profiler-ui-vitest");

    try {
      const configPath = require.resolve("../../webpack.config.js");
      delete require.cache[configPath];
      const config = require(configPath) as Configuration;
      const stats = await compile(config);
      const result = stats.toJson({ all: false, errors: true, assets: true });

      expect(result.errors ?? []).toEqual([]);
      expect((result.assets ?? []).map((asset) => asset.name)).toContain("CS2RuntimeProfiler.css");

      const moduleAsset = stats.compilation.getAsset("CS2RuntimeProfiler.mjs");
      expect(moduleAsset, "CS2RuntimeProfiler.mjs should be emitted").toBeDefined();

      const moduleSource = moduleAsset!.source.source().toString();
      expect(moduleSource).toMatch(/hasCSS/);
    } finally {
      if (previousUserDataPath === undefined) {
        delete process.env.CSII_USERDATAPATH;
      } else {
        process.env.CSII_USERDATAPATH = previousUserDataPath;
      }
    }
  }, 20_000);
});
