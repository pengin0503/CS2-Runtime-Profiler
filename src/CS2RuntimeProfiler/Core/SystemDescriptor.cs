using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2RuntimeProfiler.Core
{
    public enum SystemSourceKind
    {
        Vanilla,
        Runtime,
        Mod,
        Profiler,
        SharedLibrary,
        Unknown
    }

    public sealed class SystemDescriptor
    {
        public SystemDescriptor(
            string fullTypeName,
            string assemblyName,
            SystemSourceKind sourceKind,
            string modName,
            MetricConfidence confidence,
            IEnumerable<PatchOwnerInfo> patchOwners = null)
        {
            FullTypeName = fullTypeName ?? string.Empty;
            AssemblyName = assemblyName ?? string.Empty;
            SourceKind = sourceKind;
            ModName = modName;
            Confidence = confidence;
            PatchOwners = (patchOwners ?? Array.Empty<PatchOwnerInfo>()).ToArray();
        }

        public string FullTypeName { get; }
        public string AssemblyName { get; }
        public SystemSourceKind SourceKind { get; }
        public string ModName { get; }
        public MetricConfidence Confidence { get; }
        public IReadOnlyList<PatchOwnerInfo> PatchOwners { get; }
    }
}
