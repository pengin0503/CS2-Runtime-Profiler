using System;
using System.Collections.Generic;

namespace CS2RuntimeProfiler.Export
{
    public sealed class RuntimeReportMetadata
    {
        public string HardwareSummary { get; set; }
        public IReadOnlyList<string> EnabledMods { get; set; } = Array.Empty<string>();
    }
}
