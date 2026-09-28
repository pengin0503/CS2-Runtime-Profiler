# Performance Advisor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a manual, evidence-driven Performance Advisor to CS2 Runtime Profiler that diagnoses current bottlenecks, recommends reversible changes to standard in-game settings, lets the user explicitly apply/undo them, and compares before/after captures without automatic tuning.

**Architecture:** Keep all diagnosis, recommendation, comparison, and undo policy in pure-core `Core/Advisor` types. Put CS2-specific standard-setting discovery and write behavior behind an `Advisor/Settings` gateway that enumerates only built-in `SharedSettings` instances and mirrors the normal Options UI setter/apply semantics. Add a small `AdvisorSystem` orchestration layer and project its state through the existing snapshot/binding/UI/export pipelines so failures remain isolated from normal profiling.

**Tech Stack:** C# 9 / .NET Framework 4.8 for the mod, NUnit pure-core tests on .NET 8, metadata contract tests for current CS2 assemblies, React 18 + TypeScript + GameFace/`cs2` UI, Vitest, existing JSON export stack.

**Spec:** `docs/superpowers/specs/2026-09-28-performance-advisor-design.md`

## Global Constraints

- Performance Advisor is a feature of `pengin0503/CS2-Runtime-Profiler`, not a separate mod.
- Target baseline is Cities: Skylines II 1.6.2-era runtime; later versions degrade by capability rather than guessing.
- No background auto-tuning, automatic bulk apply, or automatic rollback.
- Every setting change requires an explicit user action.
- Direct writes are limited to settings exposed by the standard Cities: Skylines II in-game Options UI.
- Never write hidden/developer-only values, private ECS internals, undocumented simulation constants, internal update intervals, or third-party mod settings.
- Recommendation scope is narrower than setting-access scope: settings may be cataloged without receiving a performance recommendation.
- Every Advisor-applied change stores the immediately preceding value and supports conflict-aware undo when safe.
- If the current value differs from the Advisor-applied value, undo must not silently overwrite it.
- A broken setting adapter/rule/capability must not break existing monitoring, capture, export, or profiler UI.
- Asset Performance Auditor is not a required dependency.
- Existing `Setting.cs` remains the configuration for Runtime Profiler itself; it is not the game-settings gateway.
- No new runtime dependency is added unless a later task proves the existing Game/Colossal APIs cannot implement the approved design.

## Review Focus

- **External modification after Apply:** if the player changes a value in the normal Options UI after Advisor Apply, individual/session undo must detect the mismatch and refuse silent restoration. Task 7 pins this with conflict tests.
- **Game update changes setting metadata/API:** missing `SharedSettings`, setter attributes, or apply members must make only the affected capability read-only/unavailable. Tasks 4–5 pin this with metadata contract and fail-closed tests.
- **Dynamic visibility/disable/confirmation:** a currently hidden/disabled or confirmation-sensitive option must never be written through a generic unsafe path. Task 5 pins visibility/enablement/apply-behavior tests.
- **Degraded profiler evidence:** unavailable/indirect/low-confidence evidence must suppress strong recommendations rather than become zero or a guessed bottleneck. Tasks 2–3 pin this.
- **Several settings changed before follow-up:** before/after comparison may describe metric movement but must not assign causality to one setting. Task 8 pins this wording/model behavior.

## File Structure

### Pure core

- `src/CS2RuntimeProfiler/Core/Advisor/AdvisorEvidenceSnapshot.cs` — normalized capture evidence consumed by Advisor.
- `src/CS2RuntimeProfiler/Core/Advisor/BottleneckObservation.cs` — bottleneck category/severity/confidence/evidence records.
- `src/CS2RuntimeProfiler/Core/Advisor/GameSettingDescriptor.cs` — stable setting identity/capability/value metadata independent of CS2 objects.
- `src/CS2RuntimeProfiler/Core/Advisor/SettingRecommendation.cs` — recommendation direction/priority/confidence and evidence.
- `src/CS2RuntimeProfiler/Core/Advisor/BottleneckClassifier.cs` — deterministic evidence-to-bottleneck logic.
- `src/CS2RuntimeProfiler/Core/Advisor/RecommendationEngine.cs` — deterministic bottleneck + descriptor to recommendation logic.
- `src/CS2RuntimeProfiler/Core/Advisor/SettingChangeSession.cs` — Apply/Undo bookkeeping and conflict policy.
- `src/CS2RuntimeProfiler/Core/Advisor/AdvisorComparison.cs` — baseline/follow-up metric comparison.
- `src/CS2RuntimeProfiler/Core/Advisor/AdvisorState.cs` — immutable state projected to UI/export.

### CS2 setting integration

- `src/CS2RuntimeProfiler/Advisor/Settings/IGameSettingGateway.cs` — read/catalog/apply interface used by `AdvisorSystem`.
- `src/CS2RuntimeProfiler/Advisor/Settings/GameSettingCatalogBuilder.cs` — enumerates built-in `SharedSettings` and creates descriptors.
- `src/CS2RuntimeProfiler/Advisor/Settings/AutomaticSettingAdapter.cs` — safe reflection adapter for reversible standard UI value controls.
- `src/CS2RuntimeProfiler/Advisor/Settings/SettingUiMetadataReader.cs` — interprets Options UI attributes/conditions/apply behavior.
- `src/CS2RuntimeProfiler/Advisor/Settings/GameSettingGateway.cs` — adapter registry, current reads, verified writes, fail-closed results.
- `src/CS2RuntimeProfiler/Advisor/AdvisorSystem.cs` — manual diagnosis, recommendation session, apply/undo/re-diagnose orchestration.

### Existing integration points

- `src/CS2RuntimeProfiler/Mod.cs` — register `AdvisorSystem` at `UIUpdate`.
- `src/CS2RuntimeProfiler/UI/UiSnapshot.cs` — add Advisor DTOs/state.
- `src/CS2RuntimeProfiler/UI/UiSnapshotBuilder.cs` — project Advisor state.
- `src/CS2RuntimeProfiler/UI/ProfilerUISystem.cs` — bindings/triggers for diagnose/apply/undo/conflict resolution.
- `src/CS2RuntimeProfiler/Export/PerformanceReport.cs` — Advisor report DTOs.
- `src/CS2RuntimeProfiler/Export/ProfilerReportBuilder.cs` — include Advisor state in reports.
- `UI/src/profiler/bindings.ts` — Advisor TypeScript contracts/triggers.
- `UI/src/profiler/ProfilerRoot.tsx` — add `advisor` tab.
- `UI/src/profiler/tabs/PerformanceAdvisorTab.tsx` — Advisor UI.
- `UI/src/profiler/profiler.module.scss` — recommendation/session/conflict layout.

### Tests

- `tests/CS2RuntimeProfiler.Tests/AdvisorDomainTests.cs`
- `tests/CS2RuntimeProfiler.Tests/BottleneckClassifierTests.cs`
- `tests/CS2RuntimeProfiler.Tests/RecommendationEngineTests.cs`
- `tests/CS2RuntimeProfiler.Tests/SettingChangeSessionTests.cs`
- `tests/CS2RuntimeProfiler.Tests/AdvisorComparisonTests.cs`
- `tests/CS2RuntimeProfiler.Tests/AdvisorUiProjectionTests.cs`
- `tests/CS2RuntimeProfiler.Tests/AdvisorExportTests.cs`
- `tests/CS2RuntimeProfiler.AdapterTests/CS2RuntimeProfiler.AdapterTests.csproj`
- `tests/CS2RuntimeProfiler.AdapterTests/SettingsApiContractTests.cs`
- `UI/src/profiler/performanceAdvisorRegression.test.ts`

---

### Task 1: Pure Advisor domain contracts

**Files:**
- Create: `src/CS2RuntimeProfiler/Core/Advisor/AdvisorEvidenceSnapshot.cs`
- Create: `src/CS2RuntimeProfiler/Core/Advisor/BottleneckObservation.cs`
- Create: `src/CS2RuntimeProfiler/Core/Advisor/GameSettingDescriptor.cs`
- Create: `src/CS2RuntimeProfiler/Core/Advisor/SettingRecommendation.cs`
- Create: `src/CS2RuntimeProfiler/Core/Advisor/AdvisorState.cs`
- Create: `tests/CS2RuntimeProfiler.Tests/AdvisorDomainTests.cs`

**Interfaces:**
- Produces `AdvisorEvidenceSnapshot`, `BottleneckObservation`, `GameSettingDescriptor`, `SettingRecommendation`, and `AdvisorState` for all later tasks.
- `GameSettingDescriptor.SettingId` is a stable string built as `<settings-type-full-name>::<member-name>`; no runtime object reference is stored in core.
- Value transport uses strings plus `SettingValueKind` (`Boolean`, `Integer`, `Float`, `Enum`, `String`, `KeyBinding`, `Other`) so core remains game-assembly independent.

- [ ] **Step 1: Write failing domain tests**

Test that descriptors preserve user-facing/read/write/apply capability separately; recommendations distinguish `LowerRecommended`, `KeepCurrent`, `HeadroomAvailable`, `NoRecommendation`; unavailable evidence remains unavailable rather than becoming zero.

- [ ] **Step 2: Run the focused tests and verify RED**

Run: `dotnet test tests/CS2RuntimeProfiler.Tests/CS2RuntimeProfiler.Tests.csproj --filter AdvisorDomainTests -v minimal`

Expected: FAIL because `Core.Advisor` types do not exist.

- [ ] **Step 3: Implement the minimal domain records/enums**

Required public/core signatures:

```csharp
public sealed class AdvisorEvidenceSnapshot { ... }
public sealed class BottleneckObservation { ... }
public sealed class GameSettingDescriptor { ... }
public sealed class SettingRecommendation { ... }
public sealed class AdvisorState { ... }
```

Use existing profiler confidence/availability terminology where compatible; do not duplicate runtime measurements in mutable game objects.

- [ ] **Step 4: Run focused and full pure-core tests**

Run:
`dotnet test tests/CS2RuntimeProfiler.Tests/CS2RuntimeProfiler.Tests.csproj --filter AdvisorDomainTests -v minimal`
then
`dotnet test tests/CS2RuntimeProfiler.Tests/CS2RuntimeProfiler.Tests.csproj -v minimal`

Expected: PASS, zero failures.

- [ ] **Step 5: Commit**

```bash
git add src/CS2RuntimeProfiler/Core/Advisor tests/CS2RuntimeProfiler.Tests/AdvisorDomainTests.cs
git commit -m "feat: add Performance Advisor domain model"
```

### Task 2: Capture evidence projection and bottleneck classification

**Files:**
- Create: `src/CS2RuntimeProfiler/Core/Advisor/CaptureAdvisorEvidenceProjector.cs`
- Create: `src/CS2RuntimeProfiler/Core/Advisor/BottleneckClassifier.cs`
- Create: `tests/CS2RuntimeProfiler.Tests/BottleneckClassifierTests.cs`

**Interfaces:**
- Consumes existing `CaptureSession`, global metrics, recorder metrics, system timing, pathfinding/domain counters, confidence/availability, and profiler overhead.
- Produces `AdvisorEvidenceSnapshot CaptureAdvisorEvidenceProjector.Project(CaptureSession capture)`.
- Produces `IReadOnlyList<BottleneckObservation> BottleneckClassifier.Classify(AdvisorEvidenceSnapshot evidence)`.

- [ ] **Step 1: Write failing classifier tests**

Cover at minimum:
- high frame/GPU pressure with healthy simulation => `RenderingGpu`;
- low simulation efficiency with no GPU-pressure evidence => simulation/main-thread observation rather than GPU;
- simultaneous GPU and simulation pressure => multiple observations, not one forced label;
- missing/degraded GPU metric => no `High` confidence GPU conclusion;
- profiler self-overhead above the configured/recorded acceptable state => downgrade Advisor confidence.

- [ ] **Step 2: Run tests and verify RED**

Run: `dotnet test tests/CS2RuntimeProfiler.Tests/CS2RuntimeProfiler.Tests.csproj --filter BottleneckClassifierTests -v minimal`

Expected: FAIL because projector/classifier do not exist.

- [ ] **Step 3: Implement projector and deterministic classifier**

Classifier must only use evidence represented by `AdvisorEvidenceSnapshot`. Keep thresholds as named constants in `BottleneckClassifier`; do not infer unavailable metrics as zero.

- [ ] **Step 4: Verify focused + full tests**

Run the focused filter, then the complete pure-core project. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CS2RuntimeProfiler/Core/Advisor tests/CS2RuntimeProfiler.Tests/BottleneckClassifierTests.cs
git commit -m "feat: classify profiler bottlenecks for Advisor"
```

### Task 3: Evidence-backed recommendation engine

**Files:**
- Create: `src/CS2RuntimeProfiler/Core/Advisor/RecommendationEngine.cs`
- Create: `src/CS2RuntimeProfiler/Core/Advisor/PerformanceSettingRule.cs`
- Create: `tests/CS2RuntimeProfiler.Tests/RecommendationEngineTests.cs`

**Interfaces:**
- Consumes `IReadOnlyList<BottleneckObservation>` and `IReadOnlyList<GameSettingDescriptor>`.
- Produces `IReadOnlyList<SettingRecommendation> RecommendationEngine.Build(...)`.
- `PerformanceSettingRule` contains setting matching metadata, relevant bottleneck category, lowering/headroom eligibility, and evidence/confidence floor; it never writes a setting.

- [ ] **Step 1: Write failing recommendation tests**

Pin these behaviors:
- rendering/GPU bottleneck + performance-relevant graphics descriptor => one-step lower recommendation;
- CPU/simulation-only bottleneck => no claim that lowering a purely GPU setting will fix it;
- large verified VRAM/headroom evidence may produce `HeadroomAvailable`, never an unconditional guarantee;
- low/insufficient evidence => `NoRecommendation` or low-confidence informational result;
- unrelated standard settings such as volume/keybind descriptors remain cataloged but receive no performance recommendation;
- rule output preserves evidence identifiers and confidence.

- [ ] **Step 2: Run and verify RED**

Run: `dotnet test tests/CS2RuntimeProfiler.Tests/CS2RuntimeProfiler.Tests.csproj --filter RecommendationEngineTests -v minimal`

- [ ] **Step 3: Implement rules and engine**

Use conservative single-step changes for ordered quality settings. Rules match semantic descriptor tags supplied by the settings integration layer; do not hard-code private game fields.

- [ ] **Step 4: Verify focused + full pure-core tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CS2RuntimeProfiler/Core/Advisor tests/CS2RuntimeProfiler.Tests/RecommendationEngineTests.cs
git commit -m "feat: generate evidence-backed setting recommendations"
```

### Task 4: Pin the CS2 standard-settings API contract

**Files:**
- Create: `tests/CS2RuntimeProfiler.AdapterTests/CS2RuntimeProfiler.AdapterTests.csproj`
- Create: `tests/CS2RuntimeProfiler.AdapterTests/SettingsApiContractTests.cs`
- Modify: `CS2RuntimeProfiler.sln`

**Interfaces:**
- This task produces no runtime code. It defines the external API assumptions Tasks 5–6 may rely on.
- Contract baseline: game 1.6.2f1 managed assemblies through `CSII_MANAGEDPATH`.

- [ ] **Step 1: Create failing metadata contract tests**

Using `MetadataLoadContext`, assert the baseline provides:
- `Game.Settings.SharedSettings` built-in settings roots including general/audio/gameplay/radio/graphics/editor/userInterface/input/userState/keybinding/benchmark/modding;
- `Game.Settings.Setting.Apply()` and `ApplyAndSave()`;
- UI metadata types used by the normal Options path, including hidden/developer/platform/hide-by-condition/disable-by-condition/advanced/slider/dropdown/confirmation/setter attributes;
- readable public setting properties/method metadata needed to distinguish reversible value controls from action buttons.

Fail with a clear message when `CSII_MANAGEDPATH` is missing rather than silently passing.

- [ ] **Step 2: Run contract tests against current local game references and verify RED for any incorrect assumptions**

Run: `dotnet test tests/CS2RuntimeProfiler.AdapterTests/CS2RuntimeProfiler.AdapterTests.csproj -v minimal`

Expected initial state: either compile/test failures revealing mismatched names/signatures, or RED due the project/tests not yet being wired. Correct only the test assumptions from the actual 1.6.2f1 API; do not weaken the standard-settings boundary.

- [ ] **Step 3: Finish the contract project and add it to the solution**

Mirror the Asset Performance Auditor metadata-test pattern: `net48`, C# 9, NUnit, `System.Reflection.MetadataLoadContext`, no game DLLs committed to the repository.

- [ ] **Step 4: Run adapter contract tests**

Expected: PASS against the local 1.6.2f1 managed directory.

- [ ] **Step 5: Commit**

```bash
git add tests/CS2RuntimeProfiler.AdapterTests CS2RuntimeProfiler.sln
git commit -m "test: pin standard game settings API contract"
```

### Task 5: Build the standard game setting catalog and fail-closed metadata layer

**Files:**
- Create: `src/CS2RuntimeProfiler/Advisor/Settings/SettingUiMetadataReader.cs`
- Create: `src/CS2RuntimeProfiler/Advisor/Settings/GameSettingCatalogBuilder.cs`
- Create: `src/CS2RuntimeProfiler/Advisor/Settings/IGameSettingGateway.cs`
- Create: `tests/CS2RuntimeProfiler.Tests/GameSettingCatalogPolicyTests.cs`
- Modify: `tests/CS2RuntimeProfiler.Tests/CS2RuntimeProfiler.Tests.csproj` only if small game-independent policy helpers must be linked explicitly.

**Interfaces:**
- `IReadOnlyList<GameSettingDescriptor> GameSettingCatalogBuilder.Build()` enumerates only built-in instances reachable from `SharedSettings`.
- `SettingUiMetadataReader` reports `IsUserFacing`, `IsCurrentlyVisible`, `IsCurrentlyEnabled`, `ApplyBehavior`, allowed values/range, and whether a reversible write path exists.
- `IGameSettingGateway` exposes `GetCatalog()`, `Read(settingId)`, `Apply(settingId, value)`, and `Restore(settingId, expectedCurrentValue, originalValue)` result contracts.

- [ ] **Step 1: Write failing policy tests**

Use game-independent fake metadata to prove:
- hidden/developer-only/action-button members are not writable settings;
- normal reversible bool/enum/slider/dropdown/text/keybinding values are catalogable;
- a setting with no provably safe write path becomes `ReadOnlyForAdvisor` rather than writable;
- dynamic hidden/disabled conditions block Apply while active;
- confirmation/restart semantics are preserved in the descriptor.

- [ ] **Step 2: Verify RED**

Run the focused pure-core tests.

- [ ] **Step 3: Implement catalog + metadata reader against SharedSettings/UI attributes**

The catalog source is `SharedSettings`, not the global `ModSetting` registry. Reflection may inspect public built-in setting members and their official UI attributes, but must not use arbitrary private fields as alternate writable settings.

- [ ] **Step 4: Run pure policy tests and adapter contract tests**

Expected: PASS for both suites.

- [ ] **Step 5: Commit**

```bash
git add src/CS2RuntimeProfiler/Advisor/Settings tests/CS2RuntimeProfiler.Tests tests/CS2RuntimeProfiler.AdapterTests
git commit -m "feat: catalog standard CS2 settings for Advisor"
```

### Task 6: Implement verified setting read/apply adapters

**Files:**
- Create: `src/CS2RuntimeProfiler/Advisor/Settings/AutomaticSettingAdapter.cs`
- Create: `src/CS2RuntimeProfiler/Advisor/Settings/GameSettingGateway.cs`
- Create: `tests/CS2RuntimeProfiler.Tests/GameSettingGatewayPolicyTests.cs`

**Interfaces:**
- `AutomaticSettingAdapter` reads/writes one descriptor through the official property/custom-setter path represented by its UI metadata.
- `GameSettingGateway.Apply` returns a structured result containing `Succeeded`, `ObservedBefore`, `Requested`, `ObservedAfter`, `ApplyBehavior`, and failure reason.
- Writes call the same supported setter behavior represented by the Options UI metadata and then the owning `Setting.ApplyAndSave()` when appropriate.

- [ ] **Step 1: Write failing gateway policy tests with fake adapters**

Pin:
- validate requested value before writing;
- invoke custom setter semantics when metadata requires it;
- verify post-apply value and report failure if it did not change as requested;
- refuse currently hidden/disabled/read-only entries;
- confirmation-sensitive settings return `ConfirmationRequired` instead of silently applying without the required acknowledgement;
- one broken adapter does not invalidate unrelated catalog entries.

- [ ] **Step 2: Verify RED**

Run: `dotnet test tests/CS2RuntimeProfiler.Tests/CS2RuntimeProfiler.Tests.csproj --filter GameSettingGatewayPolicyTests -v minimal`

- [ ] **Step 3: Implement gateway and automatic adapters**

Do not add an `Apply all` method. Keep action/button-only controls outside the reversible value adapter set. Any special-case standard setting that cannot safely use the generic path remains `ReadOnlyForAdvisor` until an explicit adapter is added.

- [ ] **Step 4: Run pure tests, adapter contracts, and Release build**

Run:
- pure-core project;
- adapter contract project with local managed path;
- `dotnet build CS2RuntimeProfiler.sln -c Release`.

Expected: zero test/build failures.

- [ ] **Step 5: Commit**

```bash
git add src/CS2RuntimeProfiler/Advisor/Settings tests/CS2RuntimeProfiler.Tests
git commit -m "feat: apply standard settings through verified gateway"
```

### Task 7: Add change sessions, individual Undo, and external-change conflict handling

**Files:**
- Create: `src/CS2RuntimeProfiler/Core/Advisor/SettingChangeSession.cs`
- Create: `tests/CS2RuntimeProfiler.Tests/SettingChangeSessionTests.cs`

**Interfaces:**
- `SettingChangeSession.RecordApplied(settingId, originalValue, appliedValue, appliedAt)` records every successful Advisor change.
- `UndoDecision EvaluateUndo(settingId, currentValue)` returns `SafeRestore`, `AlreadyRestored`, or `Conflict`.
- Session undo enumerates eligible changes in reverse application order and never claims transaction atomicity.

- [ ] **Step 1: Write failing session tests**

Cover:
- individual safe undo when current == applied;
- conflict when player changed `Medium` to `Low` after Advisor changed `High` to `Medium`;
- session undo order is reverse apply order;
- conflicted entries are skipped from silent session rollback;
- failed Apply never becomes an undoable change;
- restart-pending change retains original/applied values.

- [ ] **Step 2: Verify RED**

Run the focused test filter.

- [ ] **Step 3: Implement session state machine**

Keep it pure core: it decides policy but performs no game writes.

- [ ] **Step 4: Verify focused + full pure-core tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CS2RuntimeProfiler/Core/Advisor/SettingChangeSession.cs tests/CS2RuntimeProfiler.Tests/SettingChangeSessionTests.cs
git commit -m "feat: add conflict-aware Advisor undo sessions"
```

### Task 8: Add baseline/follow-up comparison without causal overclaiming

**Files:**
- Create: `src/CS2RuntimeProfiler/Core/Advisor/AdvisorComparison.cs`
- Create: `tests/CS2RuntimeProfiler.Tests/AdvisorComparisonTests.cs`

**Interfaces:**
- `AdvisorComparison Compare(AdvisorEvidenceSnapshot baseline, AdvisorEvidenceSnapshot followUp, IReadOnlyList<SettingChange> changes)`.
- Per-metric result is `Improved`, `Regressed`, `NoMaterialChange`, or `NotComparable`.
- Comparison records which settings changed but never attributes the metric delta to one setting when multiple changes occurred.

- [ ] **Step 1: Write failing comparison tests**

Pin:
- meaningful frame-time reduction => `Improved`;
- small numeric noise => `NoMaterialChange` using named deterministic tolerance constants;
- unavailable in either capture => `NotComparable`;
- opposite-direction regression => `Regressed`;
- two or more changed settings => summary wording/model carries `MultipleChanges` and no single-setting causal field.

- [ ] **Step 2: Verify RED**

Run focused tests.

- [ ] **Step 3: Implement comparison model/calculator**

Use metric-specific directionality: lower frame/CPU/GC time is better, higher simulation efficiency is better. Do not compare metrics whose definitions/capability changed between captures.

- [ ] **Step 4: Verify focused + full tests**

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CS2RuntimeProfiler/Core/Advisor/AdvisorComparison.cs tests/CS2RuntimeProfiler.Tests/AdvisorComparisonTests.cs
git commit -m "feat: compare Advisor baseline and follow-up captures"
```

### Task 9: Orchestrate manual diagnosis and setting actions in `AdvisorSystem`

**Files:**
- Create: `src/CS2RuntimeProfiler/Advisor/AdvisorSystem.cs`
- Modify: `src/CS2RuntimeProfiler/Mod.cs`
- Create: `tests/CS2RuntimeProfiler.Tests/AdvisorCoordinatorPolicyTests.cs`

**Interfaces:**
- `AdvisorSystem` is registered at `SystemUpdatePhase.UIUpdate` after capture systems are available.
- Commands: start/select diagnosis baseline, apply recommendation, undo one, undo session, resolve undo conflict, select follow-up/re-diagnose.
- State: `AdvisorState CurrentState` supplied to UI/export.

- [ ] **Step 1: Write failing coordinator-policy tests against a game-independent coordinator helper**

Pin:
- Advisor only diagnoses an explicitly selected/completed bounded capture;
- Apply is allowed only for a current recommendation and current descriptor value;
- Apply result is recorded only after gateway verification succeeds;
- safe undo calls gateway restore with expected Advisor-applied value;
- conflict pauses restore and exposes conflict state;
- re-diagnose stores comparison against the chosen baseline.

- [ ] **Step 2: Verify RED**

Run focused tests.

- [ ] **Step 3: Implement coordinator helper and thin CS2 `AdvisorSystem`**

Keep the system thin: capture lookup, gateway calls, and publication only. Diagnosis/recommendation/session/comparison decisions stay in `Core/Advisor`.

- [ ] **Step 4: Run pure tests and Release build**

Expected: PASS/build success.

- [ ] **Step 5: Commit**

```bash
git add src/CS2RuntimeProfiler/Advisor src/CS2RuntimeProfiler/Mod.cs tests/CS2RuntimeProfiler.Tests
git commit -m "feat: orchestrate manual Performance Advisor sessions"
```

### Task 10: Project Advisor state through C# UI bindings

**Files:**
- Modify: `src/CS2RuntimeProfiler/UI/UiSnapshot.cs`
- Modify: `src/CS2RuntimeProfiler/UI/UiSnapshotBuilder.cs`
- Modify: `src/CS2RuntimeProfiler/UI/ProfilerUISystem.cs`
- Create: `tests/CS2RuntimeProfiler.Tests/AdvisorUiProjectionTests.cs`

**Interfaces:**
- Add `AdvisorUiState` to `UiSnapshot` containing diagnosis summary, grouped recommendations, session changes, conflict state, comparison summary, and setting capability counts.
- Add triggers under existing `CS2RuntimeProfiler` binding group: `advisorDiagnose`, `advisorApply`, `advisorUndo`, `advisorUndoSession`, `advisorResolveConflict`, `advisorRediagnose`.

- [ ] **Step 1: Write failing projection tests**

Assert unavailable/conflicted/read-only/restart-required states survive projection; no null numeric value is rendered as zero; recommendations retain evidence/confidence/current/proposed values.

- [ ] **Step 2: Verify RED**

Run focused test filter.

- [ ] **Step 3: Implement snapshot DTOs, builder projection, and trigger routing**

Do not overload the existing diagnostics tab with Advisor state; expose a dedicated snapshot section.

- [ ] **Step 4: Run full pure tests + Release build**

Expected: PASS/build success.

- [ ] **Step 5: Commit**

```bash
git add src/CS2RuntimeProfiler/UI tests/CS2RuntimeProfiler.Tests/AdvisorUiProjectionTests.cs
git commit -m "feat: expose Performance Advisor UI bindings"
```

### Task 11: Build the Performance Advisor tab and scalable manual controls

**Files:**
- Create: `UI/src/profiler/tabs/PerformanceAdvisorTab.tsx`
- Create: `UI/src/profiler/performanceAdvisorRegression.test.ts`
- Modify: `UI/src/profiler/bindings.ts`
- Modify: `UI/src/profiler/ProfilerRoot.tsx`
- Modify: `UI/src/profiler/profiler.module.scss`

**Interfaces:**
- New root tab id: `advisor`, Japanese label `パフォーマンス改善` or `改善提案` consistently across tests/UI.
- Recommendation rows expose only explicit `Apply`; no `Apply all` control.
- Applied session rows expose individual `Undo`; footer exposes `Undo session changes`.
- Conflict row exposes `Keep current` and `Restore pre-Advisor value` actions.

- [ ] **Step 1: Write failing UI regression tests**

Assert:
- `ProfilerRoot` contains the Advisor tab;
- bindings expose all Advisor triggers;
- high/medium/headroom/no-recommendation groups are distinct;
- no `Apply all` control/string exists;
- `No recommendation` is collapsed by default;
- read-only settings render a settings-navigation/instruction fallback rather than Apply;
- conflicted changes render explicit choices;
- panel body remains scrollable with many recommendations.

- [ ] **Step 2: Run UI tests and verify RED**

Run: `cd UI && npm test -- performanceAdvisorRegression.test.ts`

- [ ] **Step 3: Implement bindings/tab/styles/root integration**

Reuse existing `cs2/ui` controls and GameFace-safe patterns; avoid browser-only APIs not already covered by the project's compatibility tests.

- [ ] **Step 4: Run all UI tests and production build**

Run:
`cd UI && npm test`
then
`npm run build`

Expected: zero failures; webpack build succeeds.

- [ ] **Step 5: Commit**

```bash
git add UI/src/profiler
git commit -m "feat: add Performance Advisor in-game UI"
```

### Task 12: Export, diagnostics, docs, and end-to-end verification

**Files:**
- Modify: `src/CS2RuntimeProfiler/Export/PerformanceReport.cs`
- Modify: `src/CS2RuntimeProfiler/Export/ProfilerReportBuilder.cs`
- Create: `tests/CS2RuntimeProfiler.Tests/AdvisorExportTests.cs`
- Modify: `README.md`
- Modify: `docs/superpowers/specs/2026-09-28-performance-advisor-design.md` — mark implementation status only; do not rewrite approved requirements.

**Interfaces:**
- Export records diagnosis, recommendations, confidence/evidence IDs, setting capabilities, Advisor changes, conflict/undo outcomes, baseline/follow-up capture IDs, and comparison summary.
- Export does not dump unrelated private configuration or hidden settings.

- [ ] **Step 1: Write failing export tests**

Assert Advisor report serialization contains approved fields, preserves unavailable values, excludes hidden/unrelated setting internals, and round-trips through `PerformanceReportSerializer`.

- [ ] **Step 2: Verify RED**

Run focused tests.

- [ ] **Step 3: Extend report builder and README**

Document manual workflow explicitly: diagnose → inspect evidence → Apply individual setting → re-diagnose → compare/Undo. State that standard game Options remains authoritative and automatic tuning is not performed.

- [ ] **Step 4: Run the complete verification matrix**

Run, in order:

```bash
dotnet test tests/CS2RuntimeProfiler.Tests/CS2RuntimeProfiler.Tests.csproj -v minimal
dotnet test tests/CS2RuntimeProfiler.AdapterTests/CS2RuntimeProfiler.AdapterTests.csproj -v minimal
cd UI && npm test && npm run build && cd ..
dotnet build CS2RuntimeProfiler.sln -c Release
```

Expected: every command exits 0 with zero test failures/build errors.

Then perform in-game validation on current CS2 baseline:
- open Advisor and run manual diagnosis;
- verify catalog matches standard Options values for representative General/Graphics/Gameplay/UI/Input categories;
- apply one immediate graphics setting and verify normal Options reflects the same value;
- undo and verify exact original value returns;
- apply, then change the same setting manually in Options, and verify Advisor reports a conflict rather than overwriting;
- verify confirmation-sensitive/display settings do not bypass their safety behavior;
- run follow-up diagnosis and verify before/after comparison;
- export JSON and inspect Advisor fields/privacy.

- [ ] **Step 5: Commit final integration/docs**

```bash
git add src/CS2RuntimeProfiler/Export tests/CS2RuntimeProfiler.Tests/AdvisorExportTests.cs README.md docs/superpowers/specs/2026-09-28-performance-advisor-design.md
git commit -m "docs: finalize Performance Advisor integration"
```

## Execution Notes

- Tasks 1–3 and 7–8 are pure-core and should stay free of Game/Unity references.
- Tasks 4–6 are the highest-risk area because they define the exact standard-settings boundary and must be verified against the installed 1.6.2f1 managed assemblies before UI work depends on them.
- Do not broaden scope into Asset Performance Auditor integration, third-party mod settings, hidden configuration, automatic tuning, or simulation-internal optimization during this plan.
- If the game exposes a standard setting that cannot be generically and safely written, catalog it as `ReadOnlyForAdvisor`; adding a special adapter is in scope only when it can preserve normal Options behavior and reversible value semantics.
- If repeated adapter exceptions appear for the same Options-widget category, revise the adapter abstraction for that category instead of adding one-off per-setting patches.
