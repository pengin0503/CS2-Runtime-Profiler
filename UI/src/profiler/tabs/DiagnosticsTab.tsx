import React from "react";
import type { CaptureSummaryUi, DiagnosticsUi } from "../bindings";
import { formatMilliseconds, formatPercent } from "../format";
import styles from "../profiler.module.scss";

export function DiagnosticsTab({ diagnostics, captures }: { diagnostics: DiagnosticsUi; captures: CaptureSummaryUi[] }) {
  const latest = captures?.length ? captures[captures.length - 1] : null;
  const values: Array<[string, string | number]> = [
    ["ゲームバージョン", diagnostics.gameVersion || "利用不可"],
    ["プロファイラーバージョン", diagnostics.profilerVersion || "利用不可"],
    ["検出マーカー数", diagnostics.discoveredMarkerCount],
    ["取得マーカー数", diagnostics.capturedMarkerCount],
    ["システム行数", diagnostics.systemCount],
    ["マーカーバッチサイズ", diagnostics.markerBatchSize],
    ["サンプリング間引き", diagnostics.samplingStride],
    ["現在のプロファイラー負荷", formatPercent(diagnostics.profilerOverheadShare)],
    ["未帰属ジョブ時間", formatMilliseconds(diagnostics.unattributedJobsMilliseconds)],
    ["パッチ情報", diagnostics.patchMapState || "利用不可"]
  ];

  return (
    <div className={styles.tabBody}>
      <div className={styles.diagnosticsGrid}>
        {values.map(([name, value]) => <div className={styles.diagnosticItem} key={name}><span>{name}</span><strong>{value}</strong></div>)}
      </div>
      {latest && (
        <section>
          <h3>最新キャプチャのカバレッジ</h3>
          <div className={styles.captureFacts}>
            <span>カバレッジ <b>{formatPercent(latest.coverageRatio)}</b></span>
            <span>方式 <b>{latest.batched ? "分割計測" : "同時計測"}</b></span>
            <span>最大自己負荷 <b>{formatPercent(latest.profilerOverheadShare)}</b></span>
          </div>
        </section>
      )}
      <section>
        <h3>コレクター／互換性メモ</h3>
        {diagnostics.messages?.length
          ? diagnostics.messages.map(message => <p className={styles.diagnosticMessage} key={message}>{message}</p>)
          : <p className={styles.empty}>診断メッセージはありません。</p>}
      </section>
    </div>
  );
}
