using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CS2RuntimeProfiler.Core;

namespace CS2RuntimeProfiler.Attribution
{
    public sealed class HarmonyPatchInspector
    {
        private readonly MethodInfo _getPatchInfo;

        private HarmonyPatchInspector(MethodInfo getPatchInfo)
        {
            _getPatchInfo = getPatchInfo;
        }

        public static HarmonyPatchInspector TryCreate()
        {
            try
            {
                var harmonyAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "0Harmony", StringComparison.OrdinalIgnoreCase));
                var harmonyType = harmonyAssembly?.GetType("HarmonyLib.Harmony", throwOnError: false);
                var getPatchInfo = harmonyType?.GetMethod(
                    "GetPatchInfo",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof(MethodBase) },
                    modifiers: null);
                return new HarmonyPatchInspector(getPatchInfo);
            }
            catch
            {
                return new HarmonyPatchInspector(null);
            }
        }

        public IReadOnlyList<PatchOwnerInfo> GetPatchOwners(MethodBase method)
        {
            if (_getPatchInfo == null || method == null)
                return Array.Empty<PatchOwnerInfo>();

            try
            {
                var patchInfo = _getPatchInfo.Invoke(null, new object[] { method });
                if (patchInfo == null)
                    return Array.Empty<PatchOwnerInfo>();

                var ownersProperty = patchInfo.GetType().GetProperty("Owners", BindingFlags.Public | BindingFlags.Instance);
                if (!(ownersProperty?.GetValue(patchInfo) is IEnumerable<string> owners))
                    return Array.Empty<PatchOwnerInfo>();

                return owners
                    .Where(owner => !string.IsNullOrWhiteSpace(owner))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(owner => new PatchOwnerInfo(owner, owner))
                    .ToArray();
            }
            catch
            {
                return Array.Empty<PatchOwnerInfo>();
            }
        }
    }
}
