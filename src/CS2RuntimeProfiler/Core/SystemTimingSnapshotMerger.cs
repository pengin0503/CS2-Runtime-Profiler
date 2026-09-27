using System;
using System.Collections.Generic;

namespace CS2RuntimeProfiler.Core
{
    public static class SystemTimingSnapshotMerger
    {
        public static SystemTimingSnapshot Merge(SystemTimingSnapshot primary, SystemTimingSnapshot fallback)
        {
            var result = new SystemTimingSnapshot();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            if (primary != null)
            {
                foreach (var entry in primary.Systems)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.SystemId) || !seen.Add(entry.SystemId))
                        continue;
                    result.AddEntry(entry);
                }
                result.SetUnattributedJobsMilliseconds(primary.UnattributedJobsMilliseconds);
            }

            if (fallback != null)
            {
                foreach (var entry in fallback.Systems)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.SystemId) || !seen.Add(entry.SystemId))
                        continue;
                    result.AddEntry(entry);
                }

                if (primary == null)
                    result.SetUnattributedJobsMilliseconds(fallback.UnattributedJobsMilliseconds);
            }

            return result;
        }
    }
}
