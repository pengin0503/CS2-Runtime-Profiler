export function captureStateLabel(state: string, isDeepCapture = false): string {
  if (isDeepCapture || state === "DeepCapture") return "詳細キャプチャ中";
  switch (state) {
    case "Monitoring": return "監視中";
    case "PostBuffer": return "後処理中";
    case "Cooldown": return "クールダウン";
    default: return state || "不明";
  }
}

export function confidenceLabel(value: string): string {
  switch (value) {
    case "Full": return "完全";
    case "Managed": return "管理コード";
    case "Indirect": return "間接";
    case "Unavailable": return "利用不可";
    default: return value || "利用不可";
  }
}

export function triggerKindLabel(value: string): string {
  switch (value) {
    case "Manual": return "手動";
    case "Automatic":
    case "AutomaticLowEfficiency": return "自動（低効率）";
    default: return value || "不明";
  }
}

export function exportResultLabel(value: string): string {
  if (!value) return "";
  if (value.startsWith("ok:")) return `エクスポート完了: ${value.slice(3)}`;
  if (value.startsWith("error:")) return `エクスポート失敗: ${value.slice(6)}`;
  return value;
}

export function metricReasonLabel(reason?: string | null): string {
  if (!reason) return "";

  switch (reason) {
    case "A prior verified pending sample is required.":
      return "比較には直前の検証済み待機サンプルが必要です。";
    case "No verified runtime request counter is available for this game build.":
      return "このゲーム環境では検証済みの要求カウンターを取得できません。";
    case "No verified runtime result counter is available for this game build.":
      return "このゲーム環境では検証済みの結果カウンターを取得できません。";
    case "m_PathfindActions is not available in this runtime build.":
      return "このゲーム環境では m_PathfindActions を取得できません。";
    case "m_PathfindActions returned null.":
      return "m_PathfindActions が null を返しました。";
    case "ActionList layout is not verified in this runtime build.":
      return "このゲーム環境では ActionList の構造を検証できていません。";
    case "ActionList m_Items is not a countable collection.":
      return "ActionList の m_Items は件数を取得できるコレクションではありません。";
    case "unsupported: no verified generic service-vehicle component is available for this game build.":
      return "未対応: このゲーム環境では検証済みの汎用サービス車両コンポーネントを取得できません。";
  }

  let match = reason.match(/^Runtime field for '(.+)' is not available\.$/);
  if (match) return `ランタイムフィールド「${match[1]}」を取得できません。`;

  match = reason.match(/^Runtime field for '(.+)' is not a countable collection\.$/);
  if (match) return `ランタイムフィールド「${match[1]}」は件数を取得できるコレクションではありません。`;

  match = reason.match(/^Runtime method for '(.+)' is not available\.$/);
  if (match) return `ランタイムメソッド「${match[1]}」を取得できません。`;

  match = reason.match(/^Reading pathfind action queue failed: (.+)$/);
  if (match) return `経路探索アクションキューの読み取りに失敗しました: ${match[1]}`;

  match = reason.match(/^Reading '(.+)' failed: (.+)$/);
  if (match) return `「${match[1]}」の読み取りに失敗しました: ${match[2]}`;

  match = reason.match(/^Counting (.+) failed: (.+)$/);
  if (match) return `「${match[1]}」の件数取得に失敗しました: ${match[2]}`;

  match = reason.match(/^unsupported: no verified EntityQuery is registered for (.+)\.$/);
  if (match) return `未対応: 「${match[1]}」用の検証済み EntityQuery が登録されていません。`;

  match = reason.match(/^unsupported: (.+)$/);
  if (match) return `未対応: ${match[1]}`;

  match = reason.match(/^Metric '(.+)' is unavailable\.$/);
  if (match) return `メトリクス「${match[1]}」は利用できません。`;

  return reason;
}

export function captureWarningLabel(warning: string): string {
  if (!warning) return "";

  let match = warning.match(/^Profiler overhead remains high; sampling stride increased to (\d+)\.$/);
  if (match) return `プロファイラー負荷が高い状態が続いているため、サンプリング間引きを ${match[1]} に増やしました。`;

  match = warning.match(/^Profiler overhead exceeded ([^;]+); marker batching reduced to (\d+) concurrent recorders\.$/);
  if (match) return `プロファイラー負荷が ${match[1]} を超えたため、同時記録数を ${match[2]} に抑えた分割計測へ切り替えました。`;

  match = warning.match(/^Profiler memory grew by ([^ ]+) MiB during this capture; marker batching reduced to (\d+) concurrent recorders\.$/);
  if (match) return `詳細キャプチャ中のプロファイラーメモリが ${match[1]} MiB 増加したため、同時記録数を ${match[2]} に抑えました。`;

  match = warning.match(/^Profiler memory grew by ([^ ]+) MiB during this capture; sampling stride increased to (\d+)\.$/);
  if (match) return `詳細キャプチャ中のプロファイラーメモリが ${match[1]} MiB 増加したため、サンプリング間引きを ${match[2]} に増やしました。`;

  if (warning === "System timing projection failed for this capture; per-system timing is unavailable.") {
    return "このキャプチャのシステム時間集計に失敗したため、システム別時間は利用できません。";
  }

  return warning;
}
