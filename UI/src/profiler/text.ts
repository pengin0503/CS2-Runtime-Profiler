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
