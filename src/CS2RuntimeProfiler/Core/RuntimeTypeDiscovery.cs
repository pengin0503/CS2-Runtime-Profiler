using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CS2RuntimeProfiler.Core
{
    public static class RuntimeTypeDiscovery
    {
        public static IReadOnlyList<Type> Enumerate(
            IEnumerable<Assembly> assemblies,
            Func<Assembly, Type[]> typeProvider = null)
        {
            if (assemblies == null)
                throw new ArgumentNullException(nameof(assemblies));

            typeProvider ??= assembly => assembly.GetTypes();
            var result = new List<Type>();

            foreach (var assembly in assemblies)
            {
                if (assembly == null)
                    continue;

                try
                {
                    var types = typeProvider(assembly);
                    if (types != null)
                        result.AddRange(types.Where(type => type != null));
                }
                catch (ReflectionTypeLoadException ex)
                {
                    result.AddRange(ex.Types.Where(type => type != null));
                }
                catch
                {
                    // A single inaccessible/broken assembly must not abort discovery.
                }
            }

            return result;
        }
    }
}
