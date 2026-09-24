using System;
using System.Collections.Generic;
using System.Linq;
using CS2RuntimeProfiler.Attribution;
using CS2RuntimeProfiler.Core;
using Unity.Entities;

namespace CS2RuntimeProfiler.Profiling
{
    public sealed class ProfilerCatalog
    {
        private readonly HarmonyPatchInspector _patchInspector;
        private readonly ModAttributor _modAttributor;

        public ProfilerCatalog(ModAttributor modAttributor = null, HarmonyPatchInspector patchInspector = null)
        {
            _modAttributor = modAttributor ?? new ModAttributor(null);
            _patchInspector = patchInspector ?? HarmonyPatchInspector.TryCreate();
        }

        public IReadOnlyList<SystemDescriptor> Discover()
        {
            var types = RuntimeTypeDiscovery.Enumerate(AppDomain.CurrentDomain.GetAssemblies());
            var systemBase = typeof(ComponentSystemBase);

            return types
                .Where(type => type != null && !type.IsAbstract && systemBase.IsAssignableFrom(type))
                .Select(CreateDescriptor)
                .OrderBy(descriptor => descriptor.AssemblyName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(descriptor => descriptor.FullTypeName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private SystemDescriptor CreateDescriptor(Type type)
        {
            var assemblyName = type.Assembly.GetName().Name ?? string.Empty;
            var sourceKind = AssemblyAttributor.ClassifyName(assemblyName);
            var modName = sourceKind == SystemSourceKind.Mod ? _modAttributor.Resolve(assemblyName) : null;

            // Patches are method-specific; their discovery is populated when the parameterless ECS update method is inspectable.
            var onUpdate = UpdateMethodResolver.Resolve(type);
            var patchOwners = onUpdate != null
                ? _patchInspector.GetPatchOwners(onUpdate)
                : Array.Empty<PatchOwnerInfo>();

            return new SystemDescriptor(
                type.FullName ?? type.Name,
                assemblyName,
                sourceKind,
                modName,
                MetricConfidence.Unavailable,
                patchOwners);
        }
    }
}
