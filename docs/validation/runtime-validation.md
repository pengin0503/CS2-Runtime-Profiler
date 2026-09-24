# Runtime validation

Date: 2026-09-24  
Branch: `feature/runtime-profiler-v1`

This document separates automated evidence from checks that require a real Cities: Skylines II session. Unit tests, CI and static inspection do **not** convert an in-game scenario into a pass. If a scenario was not actually executed, its status remains **NOT RUN / UNVERIFIED**.

## Automated evidence recorded in this implementation session

| Check | Evidence | Result | Scope / limitation |
| --- | --- | --- | --- |
| Capture timing finalizer TDD RED | commit `50cee256924376934f976093c7a7da6b814d6246`, Actions run `35971528377` | Expected failure | Failed on missing `CaptureSystemTimingFinalizer` (`CS0103`), establishing the missing behavior before implementation. |
| Capture timing finalizer GREEN | commit `578104eb84789bf22ed6da0a46754c36c2d0c85a`, Actions run `35971630905` | PASS | Pure-layer projection/finalization test only. |
| Completed-capture one-shot processor TDD RED | commit `247043b0518daf7f011169ac83f7876ef92c9dfe`, Actions run `35971836820` | Expected failure | Failed on missing `CaptureCompletionTimingProcessor` (`CS0246`). |
| Completed-capture one-shot processor GREEN | commit `52352d03236a478d281b188767e2a7d0a87dfd78`, Actions run `35971922138` | PASS | Pure-layer lifecycle test only. |
| Runtime wiring commit Pure Core Tests | commit `5441378444f32bb25faf6971b40a6b77b9ea4ea6`, Actions run `35972029165` | PASS | Confirms the pure test suite remained green. The pure test project does not compile the Game-dependent runtime system, so this is not a full CS2 build result. |
| Latest UI-specific CI after Timeline/Captures/Diagnostics work | commit `0d933d55648c306cebe94f0aeb3d12a80347c010`, UI Tests run `35968390716` | PASS | Later Task 8 changes did not modify `UI/**`; the UI workflow is path-filtered, so it was not rerun by the core/runtime changes. |
| Full official Windows CS2 mod build | — | NOT RUN / UNVERIFIED | Not executed in this validation environment. A complete official Windows CS2 modding environment is required. |
| In-game runtime validation | — | NOT RUN / UNVERIFIED | No Cities: Skylines II process was available in this validation environment. |

## In-game validation matrix

Record concrete measurements and evidence in this table when the scenarios are executed. Do not replace missing measurements with inferred values.

| # | Scenario | Status | Selected speed | Actual speed | FPS | Profiler overhead | Capture coverage | Errors / warnings | Pass criteria / notes |
| ---: | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | Profiler disabled baseline | **NOT RUN / UNVERIFIED** | — | — | — | — | — | — | Run the same save/camera position for at least 60 s with monitoring disabled. Establish the comparison baseline; verify the profiler does not perform active sampling/capture work. |
| 2 | Stable Normal Monitoring on a new/vanilla city | **NOT RUN / UNVERIFIED** | — | — | — | — | — | — | Global/domain metrics remain stable and truthful; no unexplained errors; measure profiler overhead rather than assuming it. |
| 3 | Normal Monitoring on the representative ~40k modded city | **NOT RUN / UNVERIFIED** | — | — | — | — | — | — | Same-save comparison against disabled baseline; observe stability and overhead under the intended real workload. |
| 4 | Manual Deep Capture | **NOT RUN / UNVERIFIED** | — | — | — | — | — | — | Manual request starts a bounded capture, records the intended Deep window and post-buffer, attempts safe marker categories, reports coverage/batching, and does not force Job completion or mutate gameplay state. |
| 5 | Automatic Deep Capture during selected 4x / reduced actual speed | **NOT RUN / UNVERIFIED** | — | — | — | — | — | — | Verify the trigger from observed data: efficiency below 0.80 continuously for at least 2 s. Confirm pre/deep/post context without treating correlation as a proven cause. |
| 6 | Large marker set / batched coverage | **NOT RUN / UNVERIFIED** | — | — | — | — | — | — | Confirm batching remains bounded (current concurrent recorder ceiling: 150), every safe category is attempted across batches as designed, and Diagnostics coverage matches what was actually sampled. |
| 7 | Collector failure / deliberately unavailable member | **NOT RUN / UNVERIFIED** | — | — | — | — | — | — | Failure remains isolated; monitoring continues; warning/diagnostic is visible; unavailable data is not rendered as zero or fabricated. |
| 8 | Mod-owned ECS system classification | **NOT RUN / UNVERIFIED** | — | — | — | — | — | — | A uniquely matched direct system marker produces timing for the correct full type; direct owner assembly/mod metadata is retained and Mods totals use direct ownership only. |
| 9 | Patched vanilla system metadata | **NOT RUN / UNVERIFIED** | — | — | — | — | — | — | Vanilla system remains the timing owner; patch owner(s) appear as metadata and do not inherit the vanilla timing as direct mod cost. |
| 10 | JSON export and privacy inspection | **NOT RUN / UNVERIFIED** | — | — | — | — | — | — | Selected-capture values in JSON match the UI/source capture. Inspect for `C:\Users\`, account name, absolute `ModsData`/home paths and other identifying path leakage before sharing. |

## Measurement protocol

1. Use the same save, camera position and simulation state where practical.
2. Run a disabled-profiler baseline for at least 60 seconds, then repeat with Normal Monitoring.
3. Record selected speed, actual speed, FPS, profiler self-overhead, capture coverage, errors and warnings.
4. Exercise both manual and automatic capture paths.
5. For the representative slowdown case, preserve the pre/deep/post timeline and describe only observed correlations unless an independent causal test supports a stronger statement.
6. Inspect the exported JSON itself; UI appearance alone is not sufficient for privacy/export validation.

The design targets for Normal Monitoring are **<1% CPU overhead** and **<2% simulation-impact overhead**. These are acceptance targets only. They must not be reported as achieved until measured in-game with an appropriate baseline.

## System timing acceptance checks

For a completed capture that contains a uniquely matching ECS profiler marker:

- `TimeNanoseconds` is converted to milliseconds.
- Current, mean, median, P95, P99, max, total and calls are preserved where samples exist.
- Full type name matching is conservative; ambiguous matches are skipped rather than guessed.
- The completed `CaptureSession.SystemTiming` is populated exactly once and is then available to Systems/Mods UI projection.
- Direct mod ownership and patch-owner metadata remain separate concepts.
- Missing or unsafe-to-measure work remains unavailable/unattributed; no forced Job completion is introduced.

The pure-layer tests cover the projection/finalization and one-shot lifecycle behavior. The actual Unity/CS2 profiler marker names, game lifecycle behavior and UI presentation with real captures still require the in-game matrix above.

## Release-candidate evidence policy

For each runtime row, attach or reference enough evidence to reproduce the result: save/scenario description, game/mod version, logs, screenshots or capture/export artifacts, and the measured values. If evidence is incomplete, leave the row **UNVERIFIED** rather than inferring a pass.
