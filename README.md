# CS2 Runtime Profiler

CS2 Runtime Profiler is a read-mostly diagnostic mod for Cities: Skylines II. It is designed to make runtime performance evidence visible without automatically changing gameplay systems, disabling mods, or claiming unsupported causal relationships.

## What it measures

The profiler combines low-overhead global monitoring with bounded Deep Capture sessions:

- **Normal Monitoring** keeps global and domain-level metrics available continuously while monitoring is enabled.
- **Deep Capture** temporarily enables a broader set of safe profiler markers, records marker coverage, and preserves pre/deep/post capture context.
- **Systems** shows per-system timing when a captured ECS profiler marker can be matched uniquely to a full system type name.
- **Mods** groups only directly owned systems by assembly/mod metadata. A patched vanilla system remains attributed to its vanilla system; patch owners are shown as metadata rather than inheriting the vanilla system's cost.
- **Timeline / Captures / Diagnostics** expose the capture window, selected metrics, coverage, warnings, batching and profiler self-overhead information.

Completed captures project captured `TimeNanoseconds` ECS markers into milliseconds and retain current, mean, median, P95, P99, max, total and call-count statistics where data is available.

## Confidence and missing data

Timing rows carry a confidence label. The important rule is that **Unavailable does not mean zero**.

- **Full**: directly measured from a matching profiler marker.
- **Managed**: measured through a safe managed-only fallback where implemented; it may exclude Burst/Job work.
- **Indirect**: inferred from a less direct observable and should be interpreted accordingly.
- **Unavailable**: the profiler cannot support that measurement from the available evidence.

The profiler does not force Job completion to manufacture timing data. Burst/Job work that cannot be safely and directly attributed remains unattributed or unavailable.

## Capture trigger defaults

The current default automatic trigger uses simulation efficiency (`actual speed / selected speed`):

- efficiency threshold: **0.80**
- sustained below-threshold duration: **2 seconds**
- Deep Capture duration: **10 seconds**
- post-buffer duration: **5 seconds**
- cooldown: **30 seconds**

Manual capture can be requested while Monitoring or Cooldown is active. These are implementation defaults, not performance guarantees.

## Export and privacy

JSON reports are written under the Cities: Skylines II user-data directory at:

`ModsData/CS2RuntimeProfiler/CS2Profiler-report-YYYY-MM-DD_HHmmss.json`

The exporter applies a privacy sanitizer intended to replace Windows/macOS/Linux user-home paths and the current account name when detected. Treat this as defense in depth: inspect an exported report before sharing it, and complete the export/privacy scenario in the runtime validation matrix for each release candidate.

## Non-goals and limitations

- This project is a diagnostic profiler, not an automatic optimizer or mod disabler.
- It does not rank mods as "good" or "bad" and does not turn correlation into a causal claim.
- Per-system timing is emitted only when the captured marker evidence can be matched conservatively. Ambiguous matches are not guessed.
- Managed system timing does not imply complete Burst/Job attribution.
- Some runtime discovery depends on the current game/runtime version and may become unavailable after upstream changes.
- Profiler overhead limits in the design are validation targets, not claims until measured on a real game session.

## Build and test

Game/Unity/Colossal DLLs are not redistributed in this repository. A full mod build requires the user's local Cities: Skylines II managed assemblies and official modding toolchain/environment.

Pure core tests:

```powershell
dotnet test .\tests\CS2RuntimeProfiler.Tests\CS2RuntimeProfiler.Tests.csproj -v minimal
```

UI tests/build:

```powershell
cd UI
npm ci
npm test -- --run
npm run build
```

Full mod build on a correctly configured Windows CS2 modding environment:

```powershell
dotnet build .\src\CS2RuntimeProfiler\CS2RuntimeProfiler.csproj -c Release
```

## Validation status

Automated tests and static/runtime-independent checks are not substitutes for in-game validation. The release validation matrix and its current evidence are maintained in [docs/validation/runtime-validation.md](docs/validation/runtime-validation.md). Any scenario not actually run must remain marked **NOT RUN / UNVERIFIED**.
