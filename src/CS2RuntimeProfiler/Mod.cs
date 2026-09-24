using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using CS2RuntimeProfiler.Collectors;
using CS2RuntimeProfiler.Localization;
using CS2RuntimeProfiler.Profiling;
using CS2RuntimeProfiler.UI;
using Game;
using Game.Modding;
using Game.SceneFlow;

namespace CS2RuntimeProfiler
{
    public sealed class Mod : IMod
    {
        public const string Id = "CS2RuntimeProfiler";
        public static readonly ILog Log = LogManager.GetLogger($"{nameof(CS2RuntimeProfiler)}.{nameof(Mod)}").SetShowsErrorsInUI(false);
        public static Setting Settings { get; private set; }

        public void OnLoad(UpdateSystem updateSystem)
        {
            Log.Info(nameof(OnLoad));

            Settings = new Setting(this);

            var localizationManager = GameManager.instance?.localizationManager;
            if (localizationManager != null)
            {
                localizationManager.AddSource("ja-JP", new LocaleJA(Settings));
                // Keep the options readable even when the game falls back to its base locale.
                // This mod intentionally presents its user-facing interface in Japanese.
                localizationManager.AddSource("en-US", new LocaleJA(Settings));
            }

            AssetDatabase.global.LoadSettings(Id, Settings, new Setting(this));
            Settings.RegisterInOptionsUI();

            updateSystem.UpdateAt<GlobalMetricsCollector>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<DomainMetricsSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<CaptureRuntimeSystem>(SystemUpdatePhase.UIUpdate);
            updateSystem.UpdateAt<ProfilerUISystem>(SystemUpdatePhase.UIUpdate);
        }

        public void OnDispose()
        {
            Log.Info(nameof(OnDispose));
            Settings?.UnregisterInOptionsUI();
            Settings = null;
        }
    }
}
