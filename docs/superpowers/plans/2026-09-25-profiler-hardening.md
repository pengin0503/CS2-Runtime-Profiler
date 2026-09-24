# CS2 Runtime Profiler Hardening Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce profiler self-overhead and long-session memory growth while fixing capture lifecycle edge cases without changing the user-facing profiling model.

**Architecture:** Keep the existing collector/controller/UI architecture. Fix lifecycle boundaries in `DeepCaptureController` and `GlobalMetricsCollector`, avoid repeated marker snapshot materialization, make adaptive overhead degradation hysteretic and per-capture, bound completed capture retention, and split lightweight HUD snapshot delivery from full panel snapshot delivery.

**Tech Stack:** C#/.NET, Unity Entities/ProfilerRecorder, Cities: Skylines II GameSystemBase, React/TypeScript UI, NUnit, Vitest, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-09-24-cs2-runtime-profiler-design.md`

## Global Constraints

- Work only on `main`; do not create another branch.
- Use the supplied CS2 managed reference files as the authoritative API reference for runtime-facing changes.
- Preserve Japanese-first UI behavior.
- Preserve existing public UI binding names unless a task explicitly adds a new lightweight binding.
- Every behavior change follows RED → GREEN TDD and is committed in small recoverable steps.
- Do not make captured data less accurate unless the existing profiler-overhead ceiling requires adaptive degradation.

## Review Focus

1. A large time jump that crosses `DeepCapture -> PostBuffer -> Cooldown` in one observation must still finalize exactly once.
2. Disabling monitoring during either normal sampling or a deep capture must deactivate profiler recorders and re-enable only the normal set after monitoring resumes.
3. Marker sample projection must not repeatedly clone the complete capture dictionary during one projection/build operation.
4. A transient overhead spike must not permanently collapse future captures to tiny recorder batches or high sample stride.
5. Long sessions and a closed profiler panel must have bounded capture memory and avoid rebuilding full-detail UI snapshots unnecessarily.

---

### Task 1: Make capture finalization transition-safe

**Files:**
- Modify: `src/CS2RuntimeProfiler/Profiling/DeepCaptureController.cs`
- Modify: `tests/CS2RuntimeProfiler.Tests/CS2RuntimeProfiler.Tests.csproj`
- Create: `tests/CS2RuntimeProfiler.Tests/DeepCaptureControllerTests.cs`

**Interfaces:**
- Consumes: `DeepCaptureStateMachine`, `RecorderManager`, `CaptureSession`.
- Produces: transition-safe `Observe()` behavior; `CurrentSession` is null after any transition reaching Cooldown from an active capture.

- [ ] Add the controller source to the pure test project and add a fake recorder backend test fixture.
- [ ] Add a failing regression test where one `Observe()` jumps from DeepCapture through PostBuffer into Cooldown and assert the session is finalized exactly once.
- [ ] Confirm the new test fails on current main.
- [ ] Change controller lifecycle handling to finalize based on resulting state/lifecycle ownership rather than requiring the exact `PostBuffer -> Cooldown` adjacent transition.
- [ ] Run the focused test and full pure-core suite to green.
- [ ] Commit the fix.

### Task 2: Stop recorder work while monitoring is disabled

**Files:**
- Modify: `src/CS2RuntimeProfiler/Collectors/GlobalMetricsCollector.cs`
- Modify: `src/CS2RuntimeProfiler/Profiling/CaptureRuntimeSystem.cs`
- Add/modify pure tests around extracted lifecycle logic if runtime system classes cannot be compiled in the pure test project.

**Interfaces:**
- Produces: explicit monitoring enabled/disabled transition behavior; disabled means no active recorder sampling; re-enabled restores the normal recorder set.

- [ ] Add a failing test for the extracted monitoring transition state helper.
- [ ] Confirm RED.
- [ ] Implement one-shot disable/enable transition handling and wire it into runtime systems so deep-capture recorders are also deactivated.
- [ ] Confirm GREEN and run the full pure suite.
- [ ] Commit.

### Task 3: Eliminate repeated marker snapshot materialization

**Files:**
- Modify: `src/CS2RuntimeProfiler/Core/CaptureSession.cs`
- Modify: `src/CS2RuntimeProfiler/Core/SystemMarkerTimingProjector.cs`
- Modify: `src/CS2RuntimeProfiler/UI/UiSnapshotBuilder.cs` if needed.
- Modify: relevant NUnit tests.

**Interfaces:**
- Produces: one explicit marker snapshot per projection/build operation or direct read-only per-series access without cloning unrelated series.

- [ ] Add a failing behavior/API test proving repeated per-marker lookup can occur without requesting the entire dictionary snapshot.
- [ ] Confirm RED.
- [ ] Add narrow read APIs (`TryGetMarkerSamples` / marker id enumeration or equivalent) and update projection/build consumers.
- [ ] Confirm GREEN and full suite.
- [ ] Commit.

### Task 4: Add hysteresis and per-capture reset to adaptive overhead control

**Files:**
- Modify: `src/CS2RuntimeProfiler/Profiling/DeepCaptureController.cs`
- Modify: `tests/CS2RuntimeProfiler.Tests/DeepCaptureControllerTests.cs`

**Interfaces:**
- Produces: degradation only after consecutive over-ceiling observations; new capture starts from configured batch size and stride 1.

- [ ] Add failing tests for a single transient spike and for a new capture after a degraded capture.
- [ ] Confirm RED.
- [ ] Implement consecutive-spike threshold and reset adaptive fields in `BeginCapture()`.
- [ ] Confirm GREEN and full suite.
- [ ] Commit.

### Task 5: Bound completed capture retention

**Files:**
- Modify: `src/CS2RuntimeProfiler/Profiling/DeepCaptureController.cs`
- Modify: `src/CS2RuntimeProfiler/Core/CaptureCompletionTimingProcessor.cs`
- Modify: relevant tests.

**Interfaces:**
- Produces: fixed maximum completed capture history with timing finalization remaining correct when old sessions are evicted.

- [ ] Add failing tests that complete more than the retention cap and still process timing for each newly completed retained session exactly once.
- [ ] Confirm RED.
- [ ] Implement bounded completed history and replace index-only timing processing with capture-identity-aware processing compatible with eviction.
- [ ] Confirm GREEN and full suite.
- [ ] Commit.

### Task 6: Avoid full-detail UI rebuilds while panel is closed

**Files:**
- Modify: `src/CS2RuntimeProfiler/UI/ProfilerUISystem.cs`
- Modify: `src/CS2RuntimeProfiler/UI/UiSnapshotBuilder.cs` and/or `UiSnapshot.cs` as needed.
- Modify: `UI/src/profiler/bindings.ts` only if a lightweight binding is required.
- Modify: UI/pure tests.

**Interfaces:**
- Produces: HUD-visible data continues updating with the panel closed; expensive capture/system/mod/timeline projections refresh only when the panel is visible or visibility changes to open.

- [ ] Add regression tests for lightweight-vs-full snapshot selection and panel-open refresh semantics.
- [ ] Confirm RED.
- [ ] Implement the smallest split that preserves existing UI contracts.
- [ ] Run pure tests, UI tests, and UI build.
- [ ] Commit.

### Task 7: Final verification and documentation

**Files:**
- Modify: `docs/validation/runtime-validation.md` if validation steps change.
- Update this plan's execution notes/checkmarks.

- [ ] Run all pure core tests.
- [ ] Run all UI Vitest tests and production build.
- [ ] Inspect GitHub Actions for the final main commit.
- [ ] Re-check runtime-facing member/type assumptions against supplied CS2 managed references.
- [ ] Review the complete diff for regressions, profiler overhead, exception safety, and UI↔C# binding consistency.
- [ ] Record final verification evidence.

## Execution notes

- Ruling: no new worktree/branch is created because the project instruction explicitly requires all changes to be saved on `main`, and the user explicitly approved direct implementation on `main`. Recovery is provided by small main-branch commits and this plan document.
