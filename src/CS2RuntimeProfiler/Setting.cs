using Colossal.IO.AssetDatabase;
using Game.Modding;
using Game.Settings;

namespace CS2RuntimeProfiler
{
    [FileLocation(Mod.Id)]
    public sealed class Setting : ModSetting
    {
        private const string MonitoringSection = "Monitoring";

        public Setting(IMod mod) : base(mod)
        {
            SetDefaults();
        }

        [SettingsUISection(MonitoringSection)]
        public bool EnableMonitoring { get; set; }

        public override void SetDefaults()
        {
            EnableMonitoring = true;
        }
    }
}
