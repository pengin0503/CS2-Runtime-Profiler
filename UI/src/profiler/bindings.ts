import { bindValue, trigger, useValue } from "cs2/api";

export interface UiMetricRow {
  id: string;
  value: number | null;
  confidence: string;
  availability: string;
  reason: string | null;
  unitType?: string;
}

export interface GlobalUiMetrics {
  available: boolean;
  timestampSeconds: number;
  selectedSpeed: number | null;
  actualSpeed: number | null;
  efficiency: number | null;
  recorderMetrics: UiMetricRow[];
}

export interface CaptureUiState {
  state: string;
  isDeepCapture: boolean;
  completedCount: number;
}

export interface UiHudSnapshot {
  selectedSpeed: number | null;
  actualSpeed: number | null;
  state: string;
  isDeepCapture: boolean;
}

export interface SystemUiRow {
  id: string;
  ownerAssembly: string;
  currentMilliseconds: number;
  meanMilliseconds: number | null;
  p95Milliseconds: number | null;
  p99Milliseconds: number | null;
  maxMilliseconds: number | null;
  calls: number | null;
  confidence: string;
  patchOwners: string[];
}

export interface ModUiRow {
  assemblyName: string;
  directSystemMilliseconds: number;
  directSystemCount: number;
  patchedVanillaSystemCount: number;
}

export interface TimelinePoint {
  timestampSeconds: number;
  metric: string;
  value: number;
  confidence: string;
}

export interface CorrelatedChangeUi {
  metric: string;
  before: number;
  after: number;
  delta: number;
  relativeDelta: number | null;
  confidence: string;
}

export interface CaptureSummaryUi {
  id: string;
  triggerKind: string;
  triggeredAtSeconds: number;
  durationSeconds: number;
  discoveredMarkers: number;
  capturedMarkers: number;
  batched: boolean;
  coverageRatio: number;
  warningCount: number;
  profilerOverheadShare: number;
  warnings: string[];
  correlatedChanges: CorrelatedChangeUi[];
}

export interface DiagnosticsUi {
  profilerOverheadShare: number;
  unattributedJobsMilliseconds: number;
  messages: string[];
  gameVersion: string;
  profilerVersion: string;
  discoveredMarkerCount: number;
  capturedMarkerCount: number;
  systemCount: number;
  markerBatchSize: number;
  samplingStride: number;
  patchMapState: string;
}

export interface UiSnapshot {
  global: GlobalUiMetrics;
  capture: CaptureUiState;
  systems: SystemUiRow[];
  mods: ModUiRow[];
  pathfinding: { metrics: UiMetricRow[] };
  domainMetrics: UiMetricRow[];
  timeline: TimelinePoint[];
  captures: CaptureSummaryUi[];
  diagnostics: DiagnosticsUi;
}

export const EMPTY_HUD_SNAPSHOT: UiHudSnapshot = {
  selectedSpeed: null,
  actualSpeed: null,
  state: "Monitoring",
  isDeepCapture: false
};

export const EMPTY_SNAPSHOT: UiSnapshot = {
  global: {
    available: false,
    timestampSeconds: 0,
    selectedSpeed: null,
    actualSpeed: null,
    efficiency: null,
    recorderMetrics: []
  },
  capture: { state: "Monitoring", isDeepCapture: false, completedCount: 0 },
  systems: [],
  mods: [],
  pathfinding: { metrics: [] },
  domainMetrics: [],
  timeline: [],
  captures: [],
  diagnostics: {
    profilerOverheadShare: 0,
    unattributedJobsMilliseconds: 0,
    messages: [],
    gameVersion: "",
    profilerVersion: "",
    discoveredMarkerCount: 0,
    capturedMarkerCount: 0,
    systemCount: 0,
    markerBatchSize: 0,
    samplingStride: 1,
    patchMapState: ""
  }
};

const GROUP = "CS2RuntimeProfiler";

const snapshotBinding = bindValue<UiSnapshot>(GROUP, "snapshot", EMPTY_SNAPSHOT);
const hudSnapshotBinding = bindValue<UiHudSnapshot>(GROUP, "hudSnapshot", EMPTY_HUD_SNAPSHOT);
const panelVisibleBinding = bindValue<boolean>(GROUP, "panelVisible", false);
const selectedCaptureBinding = bindValue<string>(GROUP, "selectedCaptureId", "");
const selectedSystemBinding = bindValue<string>(GROUP, "selectedSystemId", "");
const selectedModBinding = bindValue<string>(GROUP, "selectedModId", "");
const exportResultBinding = bindValue<string>(GROUP, "exportResult", "");

export const useProfilerSnapshot = () => useValue(snapshotBinding);
export const useProfilerHudSnapshot = () => useValue(hudSnapshotBinding);
export const usePanelVisible = () => useValue(panelVisibleBinding);
export const useSelectedCaptureId = () => useValue(selectedCaptureBinding);
export const useSelectedSystemId = () => useValue(selectedSystemBinding);
export const useSelectedModId = () => useValue(selectedModBinding);
export const useExportResult = () => useValue(exportResultBinding);

export const togglePanel = () => trigger(GROUP, "togglePanel");
export const requestManualCapture = () => trigger(GROUP, "manualCapture");
export const selectCapture = (id: string) => trigger(GROUP, "selectCapture", id);
export const selectSystem = (id: string) => trigger(GROUP, "selectSystem", id);
export const selectMod = (id: string) => trigger(GROUP, "selectMod", id);
export const exportReport = () => trigger(GROUP, "exportReport");
