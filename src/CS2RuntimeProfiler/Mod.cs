using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using CS2RuntimeProfiler.Collectors;
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
            Settings.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(Id, Settings, new Setting(this));

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
