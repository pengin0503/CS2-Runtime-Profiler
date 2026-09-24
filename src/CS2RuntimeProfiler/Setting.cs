using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace CS2RuntimeProfiler
{
    [FileLocation(Mod.Id)]
    public sealed class Setting : ModSetting
    {
        internal const string MainTab = "Main";
        internal const string MonitoringGroup = "Monitoring";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(MainTab, MonitoringGroup)]
        public bool EnableMonitoring { get; set; }

        public override void SetDefaults()
        {
            EnableMonitoring = true;
        }
    }
}
