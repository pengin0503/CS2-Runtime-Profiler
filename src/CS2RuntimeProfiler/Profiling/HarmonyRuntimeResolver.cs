using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CS2RuntimeProfiler.Profiling
{
    public static class HarmonyRuntimeResolver
    {
        private const string HarmonyTypeName = "HarmonyLib.Harmony";
        private const string HarmonyAssemblyName = "0Harmony";

        public static Assembly Resolve(
            IEnumerable<Assembly> loadedAssemblies,
            Func<AssemblyName, Assembly> loader)
        {
            try
            {
                var loaded = (loadedAssemblies ?? Array.Empty<Assembly>())
                    .FirstOrDefault(assembly => HasHarmonyType(assembly));
                if (loaded != null)
                    return loaded;

                if (loader == null)
                    return null;

                Assembly bundled;
                try
                {
                    bundled = loader(new AssemblyName(HarmonyAssemblyName));
                }
                catch
                {
                    return null;
                }

                return HasHarmonyType(bundled) ? bundled : null;
            }
            catch
            {
                return null;
            }
        }

        private static bool HasHarmonyType(Assembly assembly)
        {
            if (assembly == null)
                return false;

            try
            {
                return assembly.GetType(HarmonyTypeName, throwOnError: false) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
