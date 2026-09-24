using System.Collections.Generic;
using Colossal;

namespace CS2RuntimeProfiler.Localization
{
    public sealed class LocaleJA : IDictionarySource
    {
        private readonly Setting _setting;

        public LocaleJA(Setting setting)
        {
            _setting = setting;
        }

        public IEnumerable<KeyValuePair<string, string>> ReadEntries(
            IList<IDictionaryEntryError> errors,
            Dictionary<string, int> indexCounts)
        {
            return new Dictionary<string, string>
            {
                { _setting.GetSettingsLocaleID(), "CS2 ランタイムプロファイラー" },
                { _setting.GetOptionTabLocaleID(Setting.MainTab), "全般" },
                { _setting.GetOptionGroupLocaleID(Setting.MonitoringGroup), "監視" },
                { _setting.GetOptionLabelLocaleID(nameof(Setting.EnableMonitoring)), "監視を有効化" },
                { _setting.GetOptionDescLocaleID(nameof(Setting.EnableMonitoring)), "低負荷の常時監視と詳細キャプチャを有効にします。無効にするとプロファイラーの収集処理を停止します。" }
            };
        }

        public void Unload()
        {
        }
    }
}
