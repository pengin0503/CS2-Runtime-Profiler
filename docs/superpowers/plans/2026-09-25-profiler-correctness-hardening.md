# Profiler Correctness Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Correct remaining measurement semantics, monitoring lifecycle, mod attribution, export fidelity, capture-selection/UI consistency and toolchain documentation issues.

**Architecture:** Keep the existing collector/controller/UI pipeline, but make evidence validity explicit at the `RecorderReading -> CaptureSession -> SystemTiming` boundary, carry source metadata through timing projection, and distinguish completed/current/selected captures in the UI projection. Avoid new background scanners or forced job completion.

**Tech Stack:** C# / .NET, Unity ProfilerRecorder, NUnit, React/TypeScript, Vitest, webpack, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-09-25-profiler-correctness-hardening-design.md`

## Global Constraints

- Save all changes directly to `main`; create no other branch.
- Use supplied Cities: Skylines II files for game/toolchain verification.
- Missing measurements stay unavailable; never invent zero.
- Patch ownership never transfers vanilla runtime cost to a mod.
- Keep hot-path changes small and bounded.

## Review Focus

- Recorder `Count == 0`: no marker sample, no coverage increment, no zero system timing.
- Monitoring disabled during active capture: recorder shutdown plus coherent state reset/finalization.
- Vanilla source kind survives timing/UI projection and is excluded from direct MOD totals.
- Selected-capture export/timeline never silently mixes unrelated retained captures.
- Added export fields remain privacy-sanitized and optional/backward-tolerant.

---

### Task 1: Preserve recorder validity and call counts

**Files:**
- Modify: `src/CS2RuntimeProfiler/Core/MetricSample.cs`
- Modify: `src/CS2RuntimeProfiler/Profiling/DeepCaptureController.cs`
- Modify: `src/CS2RuntimeProfiler/Core/SystemMetricAggregate.cs`
- Modify: `src/CS2RuntimeProfiler/Core/SystemMarkerTimingProjector.cs`
- Test: `tests/CS2RuntimeProfiler.Tests/DeepCaptureControllerTests.cs`
- Test: `tests/CS2RuntimeProfiler.Tests/SystemMarkerTimingProjectorTests.cs`

**Interfaces:**
- Produces: `MetricSample.CallCount` (`long?`) and system `Calls` based on recorder counts.

- [ ] Add failing tests proving zero-count recorder reads do not create captured samples/coverage and counts 2+3 aggregate to 5 calls.
- [ ] Run Pure Core CI and verify RED for the intended missing semantics.
- [ ] Add optional call-count to `MetricSample`; skip `RecorderReading.Count <= 0` in Deep Capture; aggregate actual call counts.
- [ ] Run Pure Core CI and verify GREEN.
- [ ] Commit each RED/GREEN checkpoint to `main`.

### Task 2: Make monitoring OFF interrupt active capture coherently

**Files:**
- Modify: `src/CS2RuntimeProfiler/Core/DeepCaptureStateMachine.cs`
- Modify: `src/CS2RuntimeProfiler/Profiling/DeepCaptureController.cs`
- Modify: `src/CS2RuntimeProfiler/Profiling/CaptureRuntimeSystem.cs`
- Test: `tests/CS2RuntimeProfiler.Tests/DeepCaptureControllerTests.cs`

**Interfaces:**
- Produces: controller interruption/reset operation used by runtime monitoring transition.

- [ ] Add failing test: interrupt during Deep Capture finalizes partial session with warning, deactivates recorders, returns Monitoring.
- [ ] Verify RED.
- [ ] Implement state reset + controller interruption; wire one-shot monitoring transition in runtime system.
- [ ] Verify GREEN and no lifecycle regression.

### Task 3: Preserve source kind and exclude vanilla from MOD direct cost

**Files:**
- Modify: `src/CS2RuntimeProfiler/Core/SystemTimingSnapshot.cs`
- Modify: `src/CS2RuntimeProfiler/Core/SystemMarkerTimingProjector.cs`
- Modify: `src/CS2RuntimeProfiler/UI/UiSnapshot.cs`
- Modify: `src/CS2RuntimeProfiler/UI/UiSnapshotBuilder.cs`
- Modify: `src/CS2RuntimeProfiler/UI/ProfilerUISystem.cs`
- Modify: `UI/src/profiler/bindings.ts`
- Test: `tests/CS2RuntimeProfiler.Tests/SystemMarkerTimingProjectorTests.cs`
- Test: `tests/CS2RuntimeProfiler.Tests/UiSnapshotBuilderTests.cs`

**Interfaces:**
- Produces: `SystemTimingEntry.SourceKind` and `SystemUiRow.SourceKind`.

- [ ] Add failing test with one vanilla and one mod system; only mod direct cost may appear in `snapshot.Mods`.
- [ ] Verify RED.
- [ ] Carry source kind through projection/serialization and filter direct MOD aggregation.
- [ ] Verify Pure Core + UI GREEN.

### Task 4: Preserve export units and full system statistics

**Files:**
- Modify: `src/CS2RuntimeProfiler/Export/PerformanceReport.cs`
- Modify: `src/CS2RuntimeProfiler/Export/ProfilerReportBuilder.cs`
- Test: `tests/CS2RuntimeProfiler.Tests/ProfilerReportBuilderTests.cs`

**Interfaces:**
- Produces: report fields for median, P99, max, total, calls and metric unit.

- [ ] Add failing export test for `UnitType=TimeNanoseconds`, P99/max/calls and sanitizer-safe serialization.
- [ ] Verify RED.
- [ ] Map units and all supported statistics into report DTOs and sanitized copies.
- [ ] Verify GREEN.

### Task 5: Separate completed/current captures and wire selection

**Files:**
- Modify: `src/CS2RuntimeProfiler/UI/UiSnapshot.cs`
- Modify: `src/CS2RuntimeProfiler/UI/UiSnapshotBuilder.cs`
- Modify: `src/CS2RuntimeProfiler/UI/ProfilerUISystem.cs`
- Modify: `UI/src/profiler/ProfilerRoot.tsx`
- Modify: `UI/src/profiler/tabs/OverviewTab.tsx`
- Test: `tests/CS2RuntimeProfiler.Tests/CaptureUiProjectionTests.cs`
- Test: `UI/src/profiler/tabs/CoreTabs.test.tsx` or focused new test.

**Interfaces:**
- Produces: completed-only summaries, selected capture-driven timeline/system timing, manual capture state gating.

- [ ] Add failing tests: active capture does not increase completed count/list; selecting older capture uses its timeline/system timing; PostBuffer manual button is disabled.
- [ ] Verify RED.
- [ ] Store selected capture ID in C# system, rebuild snapshot on selection, project only relevant capture timeline, keep active capture separate.
- [ ] Verify Pure Core + UI GREEN.

### Task 6: Make overhead diagnostics semantically truthful

**Files:**
- Modify: `src/CS2RuntimeProfiler/UI/UiSnapshot.cs`
- Modify: `src/CS2RuntimeProfiler/UI/ProfilerUISystem.cs`
- Modify: `UI/src/profiler/tabs/OverviewTab.tsx`
- Modify: `UI/src/profiler/tabs/DiagnosticsTab.tsx`
- Modify: `UI/src/profiler/bindings.ts`
- Test: relevant UI tests.

**Interfaces:**
- Renames UI meaning from total profiler overhead to capture/runtime measured overhead unless a true total is available.

- [ ] Add failing UI/source regression test that rejects ambiguous total-overhead wording.
- [ ] Verify RED.
- [ ] Rename labels/diagnostics to describe measured capture-processing share; keep validation targets separate.
- [ ] Verify UI GREEN.

### Task 7: Correct README toolchain PowerShell instructions

**Files:**
- Modify: `README.md`

**Interfaces:** none.

- [ ] Compare supplied `Mod.props` / `Mod.targets` variable names against README.
- [ ] Replace `CSII_MANAGED_PATH` with `CSII_MANAGEDPATH` and explain official toolchain prerequisites instead of implying three variables fully configure it.
- [ ] Re-read rendered Markdown/source for command consistency.

### Task 8: Final verification and validation record

**Files:**
- Modify: `docs/validation/runtime-validation.md`
- Modify: this plan (mark completed tasks/evidence).

- [ ] Run/inspect latest Pure Core Tests on final HEAD.
- [ ] Run/inspect latest UI Tests and webpack production build on final HEAD.
- [ ] Review diff from `a756b1fb4435d04af2ec19de4460f29c20983e04` to final HEAD for accidental scope expansion.
- [ ] Confirm no branch other than `main` was created by this work.
- [ ] Keep actual in-game scenarios explicitly UNVERIFIED until executed in Cities: Skylines II.