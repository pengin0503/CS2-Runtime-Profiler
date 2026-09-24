using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2RuntimeProfiler.Core
{
    public sealed class SystemTimingEntry
    {
        public SystemTimingEntry(string systemId, double milliseconds, MetricConfidence confidence, string ownerAssembly, IEnumerable<string> patchOwners)
        {
            SystemId = systemId ?? string.Empty;
            Milliseconds = Math.Max(0d, milliseconds);
            Confidence = confidence;
            OwnerAssembly = ownerAssembly ?? string.Empty;
            PatchOwners = (patchOwners ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
        }

        public string SystemId { get; }
        public double Milliseconds { get; }
        public MetricConfidence Confidence { get; }
        public string OwnerAssembly { get; }
        public IReadOnlyList<string> PatchOwners { get; }
    }

    public sealed class SystemTimingSnapshot
    {
        private readonly List<SystemTimingEntry> _systems = new List<SystemTimingEntry>();

        public IReadOnlyList<SystemTimingEntry> Systems => _systems;
        public double UnattributedJobsMilliseconds { get; private set; }

        public void AddSystem(string systemId, double milliseconds, MetricConfidence confidence, string ownerAssembly = "", IEnumerable<string> patchOwners = null)
        {
            _systems.Add(new SystemTimingEntry(systemId, milliseconds, confidence, ownerAssembly, patchOwners));
        }

        public void SetUnattributedJobsMilliseconds(double milliseconds)
        {
            UnattributedJobsMilliseconds = Math.Max(0d, milliseconds);
        }

        public double GetDirectAssemblyTotal(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
                return 0d;
            return _systems
                .Where(system => string.Equals(system.OwnerAssembly, assemblyName, StringComparison.Ordinal))
                .Sum(system => system.Milliseconds);
        }
    }
}
