import React, { useState } from "react";
import { Button } from "cs2/ui";
import { AdvisorChange, AdvisorRecommendation, AdvisorUiState, CaptureSummaryUi, EMPTY_ADVISOR } from "../bindings";
import styles from "../profiler.module.scss";

const GROUPS = [
  { id: "high", label: "高優先", include: (r: AdvisorRecommendation) => r.direction === "LowerRecommended" && r.priority === "High" },
  { id: "medium", label: "中優先", include: (r: AdvisorRecommendation) => r.direction === "LowerRecommended" && r.priority === "Medium" },
  { id: "low", label: "低優先", include: (r: AdvisorRecommendation) => r.direction === "LowerRecommended" && r.priority === "Low" },
  { id: "headroom", label: "上げる余地あり", include: (r: AdvisorRecommendation) => r.direction === "HeadroomAvailable" },
  { id: "none", label: "推奨なし", include: (r: AdvisorRecommendation) => r.direction === "NoRecommendation" || r.direction === "KeepCurrent" }
];

function RecommendationCard({ recommendation, onApply }: {
  recommendation: AdvisorRecommendation;
  onApply?: (id: string, value: string, confirmed: boolean) => void;
}) {
  const [details, setDetails] = useState(false);
  const [acknowledge, setAcknowledge] = useState(false);
  const needsConfirmation = recommendation.applyBehavior === "ConfirmationRequired";
  return (
    <article className={styles.advisorCard}>
      <strong>{recommendation.displayName}</strong>
      <span>現在値: {recommendation.currentValue} → 提案値: {recommendation.recommendedValue}</span>
      <span>優先度: {recommendation.priority}・確信度: {recommendation.confidence}</span>
      <span>{recommendation.applyCapability === "ReadOnlyForAdvisor"
        ? "この項目はゲームの標準設定画面で変更してください。" : "変更する前に根拠を確認してください。"}</span>
      {onApply && recommendation.applyCapability === "Available" &&
        (needsConfirmation && !acknowledge
          ? <Button as="button" variant="flat" onSelect={() => setAcknowledge(true)}>確認が必要: 変更内容を確認</Button>
          : <Button as="button" variant="flat" onSelect={() => onApply(recommendation.settingId,
              recommendation.recommendedValue, needsConfirmation && acknowledge)}>Apply</Button>)}
      <Button as="button" variant="flat" onSelect={() => setDetails(!details)} aria-expanded={details}>
        {details ? "詳細を閉じる" : "詳細を見る"}
      </Button>
      {details && <div className={styles.advisorDetails}>
        <span>{recommendation.rationale}</span>
        <span>根拠: {recommendation.evidenceIds?.join("、") || "十分な根拠なし"}</span>
        <span>反映方法: {recommendation.applyBehavior}</span>
      </div>}
    </article>
  );
}

function ChangeCard({ change, onUndo, onResolveConflict }: {
  change: AdvisorChange;
  onUndo?: (id: string, confirmed: boolean) => void;
  onResolveConflict?: (id: string, restoreOriginal: boolean) => void;
}) {
  const [acknowledgeUndo, setAcknowledgeUndo] = useState(false);
  return <article className={styles.advisorCard}>
    <strong>{change.settingId}</strong>
    <span>{change.originalValue} → {change.appliedValue}・現在値: {change.currentObservedValue}</span>
    <span>状態: {change.status}</span>
    {change.status === "Applied" && onUndo && (
      acknowledgeUndo
        ? <Button as="button" variant="flat" onSelect={() => onUndo(change.settingId, true)}>Undo を確定</Button>
        : <Button as="button" variant="flat" onSelect={() => setAcknowledgeUndo(true)}>Undo</Button>
    )}
    {change.status === "ExternallyModified" && onResolveConflict && <div className={styles.advisorActions}>
      <span>外部変更を検出しました。元の値へ自動的には戻しません。</span>
      <Button as="button" variant="flat" onSelect={() => onResolveConflict(change.settingId, false)}>現在値を維持</Button>
      <Button as="button" variant="flat" onSelect={() => onResolveConflict(change.settingId, true)}>変更前の値へ戻す</Button>
    </div>}
  </article>;
}

export function PerformanceAdvisorTab({ advisor = EMPTY_ADVISOR, captures = [], onDiagnose, onBaseline, onManualCapture,
  onApply, onUndo, onUndoSession, onResolveConflict, onRediagnose }: {
  advisor?: AdvisorUiState;
  captures?: CaptureSummaryUi[];
  onDiagnose?: (id: string) => void;
  onBaseline?: (id: string) => void;
  onManualCapture?: () => void;
  onApply?: (id: string, value: string, confirmed: boolean) => void;
  onUndo?: (id: string, confirmed: boolean) => void;
  onUndoSession?: () => void;
  onResolveConflict?: (id: string, restoreOriginal: boolean) => void;
  onRediagnose?: (id: string) => void;
}) {
  const [showNoRecommendation, setShowNoRecommendation] = useState(false);
  return (
    <section className={styles.advisorTab}>
      <h2>Performance Advisor・改善提案</h2>
      <p>診断 → 提案 → ユーザーによる変更 → 再診断。変更後の結果をもう一度計測してください。</p>
      <div className={styles.advisorActions}>
        {onManualCapture && <Button as="button" variant="flat" onSelect={onManualCapture}>手動詳細キャプチャを開始</Button>}
        {captures.map(capture => (
          <div className={styles.advisorCapture} key={capture.id}>
            <span>{capture.id}</span>
            {onDiagnose && <Button as="button" variant="flat" onSelect={() => onDiagnose(capture.id)}>このキャプチャを診断</Button>}
            {onRediagnose && advisor.selectedCaptureId && <Button as="button" variant="flat"
              onSelect={() => onRediagnose(capture.id)}>このキャプチャで再診断</Button>}
            {onBaseline && <Button as="button" variant="flat" onSelect={() => onBaseline(capture.id)}>比較の基準に選択</Button>}
          </div>
        ))}
      </div>
      {advisor.unavailableReason && <p className={styles.empty}>Advisor: {advisor.unavailableReason}</p>}
      {advisor.selectedCaptureId
        ? <p>診断対象: {advisor.selectedCaptureId}{advisor.baselineCaptureId ? `・基準: ${advisor.baselineCaptureId}` : ""}</p>
        : <p>完了した詳細キャプチャを選んで診断してください。</p>}
      {advisor.observations.map((observation, index) => (
        <div className={styles.advisorObservation} key={`${observation.category}-${index}`}>
          <strong>{observation.category}・{observation.severity}</strong>
          <span>確信度: {observation.confidence}・根拠: {observation.evidenceIds?.join("、")}</span>
          <span>{observation.rationale}</span>
        </div>
      ))}
      {GROUPS.map(group => {
        const entries = advisor.recommendations.filter(group.include);
        const open = group.id !== "none" || showNoRecommendation;
        return (
          <section className={styles.advisorGroup} key={group.id}>
            {group.id === "none"
              ? <Button as="button" variant="flat" aria-label="推奨なしを表示" aria-expanded={open}
                  onSelect={() => setShowNoRecommendation(!showNoRecommendation)}>{group.label} ({entries.length})</Button>
              : <h3>{group.label} ({entries.length})</h3>}
            {open && entries.map(entry => <RecommendationCard key={entry.settingId} recommendation={entry} onApply={onApply} />)}
          </section>
        );
      })}
      {!!advisor.changes?.length && <section className={styles.advisorGroup}>
        <h3>このセッションの変更 ({advisor.changes.length})</h3>
        {advisor.changes.map((change, index) => <ChangeCard key={`${change.settingId}-${index}`} change={change}
          onUndo={onUndo} onResolveConflict={onResolveConflict} />)}
        {onUndoSession && advisor.changes.some(change => change.status === "Applied") &&
          <Button as="button" variant="flat" onSelect={onUndoSession}>セッションの変更を元に戻す</Button>}
      </section>}
      {advisor.comparison && <section className={styles.advisorGroup}>
        <h3>診断の前後比較</h3>
        {advisor.comparison.multipleChanges &&
          <p>複数の設定を変更しています。以下は測定差分であり、個別設定の効果を断定しません。</p>}
        {advisor.comparison.metrics.map(metric => (
          <div className={styles.advisorObservation} key={metric.id}>
            <strong>{metric.id}: {{ Improved: "改善", Regressed: "悪化", NoMaterialChange: "大きな変化なし",
              NotComparable: "比較不可" }[metric.state]}</strong>
            <span>{metric.baselineValue ?? "取得不可"} → {metric.followUpValue ?? "取得不可"}</span>
            {metric.reason && <span>{metric.reason}</span>}
          </div>
        ))}
      </section>}
    </section>
  );
}
