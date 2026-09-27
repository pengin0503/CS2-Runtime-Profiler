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
        private readonly World _world;

        public ProfilerCatalog(
            ModAttributor modAttributor = null,
            HarmonyPatchInspector patchInspector = null,
            World world = null)
        {
            _modAttributor = modAttributor ?? new ModAttributor(null);
            _patchInspector = patchInspector ?? HarmonyPatchInspector.TryCreate();
            _world = world;
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
                patchOwners,
                profilerMarkerName: TryGetProfilerMarkerName(type),
                // A catalog bound to a World is runtime evidence. If that type is not live,
                // keep it for ownership metadata but never infer timing from its CLR name.
                allowLegacyProfilerMarkerMatching: _world == null,
                // ComponentSystemGroup.Update is an inclusive container around child system updates.
                // Keep its timing visible, but downstream additive totals must not count it again.
                isAggregateContainer: typeof(ComponentSystemGroup).IsAssignableFrom(type));
        }

        private string TryGetProfilerMarkerName(Type type)
        {
            if (_world == null || !_world.IsCreated || type == null)
                return null;

            try
            {
                var system = _world.GetExistingSystemManaged(type);
                if (system == null)
                    return null;

                return EntityManager.EntityManagerDebug.GetSystemProfilerMarkerName(
                    _world,
                    system.SystemHandle);
            }
            catch
            {
                // A catalogued type may not be instantiated in this World. Do not guess its marker name.
                return null;
            }
        }
    }
}
