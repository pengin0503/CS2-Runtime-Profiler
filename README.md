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

## Windows / PowerShell でゲームへ導入する

以下は、リポジトリの `main` ブランチを取得し、UI と C# をビルドして Cities: Skylines II のローカル Mod フォルダへ導入するまでの手順です。

前提:

- Windows 11
- Cities: Skylines II がインストール済み
- Git が使用可能
- Node.js 18 以上 / npm が使用可能
- .NET SDK が使用可能
- Cities: Skylines II の公式 Modding Toolchain がゲームファイル内に存在する

### 1. PowerShell を開き、ゲーム関連パスを設定する

Steam を既定の場所へインストールしている場合は、そのまま以下を実行できます。
Steam ライブラリを別ドライブへ置いている場合は、`$gameDir` だけ実際のゲームフォルダへ変更してください。

```powershell
$gameDir = "${env:ProgramFiles(x86)}\Steam\steamapps\common\Cities Skylines II"

$env:CSII_USERDATAPATH = "$env:USERPROFILE\AppData\LocalLow\Colossal Order\Cities Skylines II"
$env:CSII_MANAGED_PATH = Join-Path $gameDir "Cities2_Data\Managed"
$env:CSII_TOOLPATH = Join-Path $gameDir "Cities2_Data\Content\Game\.ModdingToolchain"

Test-Path $gameDir
Test-Path $env:CSII_USERDATAPATH
Test-Path $env:CSII_MANAGED_PATH
Test-Path $env:CSII_TOOLPATH
```

最後の4行がすべて `True` になることを確認してください。

### 2. リポジトリを取得する

初回のみ:

```powershell
cd "$HOME\Downloads"
git clone https://github.com/pengin0503/CS2-Runtime-Profiler.git
cd .\CS2-Runtime-Profiler
git switch main
```

すでに取得済みの場合:

```powershell
cd "$HOME\Downloads\CS2-Runtime-Profiler"
git switch main
git pull --ff-only origin main
```

現在のブランチ確認:

```powershell
git branch --show-current
```

`main` と表示されれば正しい状態です。

### 3. UI の依存関係を導入してビルドする

```powershell
cd .\UI
npm.cmd install
npm.cmd test
npm.cmd run build
cd ..
```

### 4. Pure Core Tests を実行する

```powershell
dotnet test .\tests\CS2RuntimeProfiler.Tests\CS2RuntimeProfiler.Tests.csproj -v minimal
```

テスト失敗がある場合は、ゲームへ導入する前にログを確認してください。

### 5. Release ビルドする

```powershell
dotnet build .\CS2RuntimeProfiler.sln -c Release
```

公式 Modding Toolchain の `Mod.props` / `Mod.targets` と `CSII_USERDATAPATH` が正しく設定されていれば、ビルド成果物は次のローカル Mod フォルダへ配置されます。

```text
%USERPROFILE%\AppData\LocalLow\Colossal Order\Cities Skylines II\Mods\CS2RuntimeProfiler
```

PowerShell では次の変数で確認できます。

```powershell
$modDir = Join-Path $env:CSII_USERDATAPATH "Mods\CS2RuntimeProfiler"
Get-ChildItem $modDir -Recurse
```

最低限、`CS2RuntimeProfiler.dll` と UI の `.mjs` / `.css` 成果物が存在することを確認してください。

```powershell
Get-ChildItem $modDir -Recurse -File |
    Where-Object { $_.Extension -in ".dll", ".mjs", ".css" } |
    Select-Object FullName, Length, LastWriteTime
```

フォルダをエクスプローラーで開く場合:

```powershell
explorer $modDir
```

### 6. Cities: Skylines II 側で有効化する

1. Cities: Skylines II を起動します。
2. Paradox Mods / Playsets でローカル Mod の **CS2 Runtime Profiler** を有効にします。
3. 対象の Playset を選択します。
4. ゲームを再起動して Mod を読み込みます。
5. ゲーム左上に Runtime Profiler のアイコンが表示されることを確認します。

### 更新後に再導入する場合

2回目以降は、PowerShell で次の流れだけ実行すれば更新できます。

```powershell
cd "$HOME\Downloads\CS2-Runtime-Profiler"

git switch main
git pull --ff-only origin main

$gameDir = "${env:ProgramFiles(x86)}\Steam\steamapps\common\Cities Skylines II"
$env:CSII_USERDATAPATH = "$env:USERPROFILE\AppData\LocalLow\Colossal Order\Cities Skylines II"
$env:CSII_MANAGED_PATH = Join-Path $gameDir "Cities2_Data\Managed"
$env:CSII_TOOLPATH = Join-Path $gameDir "Cities2_Data\Content\Game\.ModdingToolchain"

cd .\UI
npm.cmd install
npm.cmd test
npm.cmd run build
cd ..

dotnet test .\tests\CS2RuntimeProfiler.Tests\CS2RuntimeProfiler.Tests.csproj -v minimal
dotnet build .\CS2RuntimeProfiler.sln -c Release

$modDir = Join-Path $env:CSII_USERDATAPATH "Mods\CS2RuntimeProfiler"
Get-ChildItem $modDir -Recurse -File |
    Where-Object { $_.Extension -in ".dll", ".mjs", ".css" } |
    Select-Object FullName, Length, LastWriteTime
```

ビルド後にゲームが起動中だった場合は、一度 Cities: Skylines II を終了してから再起動してください。

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
npm test
npm run build
```

Full mod build on a correctly configured Windows CS2 modding environment:

```powershell
dotnet build .\src\CS2RuntimeProfiler\CS2RuntimeProfiler.csproj -c Release
```

## Validation status

Automated tests and static/runtime-independent checks are not substitutes for in-game validation. The release validation matrix and its current evidence are maintained in [docs/validation/runtime-validation.md](docs/validation/runtime-validation.md). Any scenario not actually run must remain marked **NOT RUN / UNVERIFIED**.
