using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2RuntimeProfiler.Core
{
    public sealed class SystemCatalogCache
    {
        private readonly Func<IEnumerable<SystemDescriptor>> _provider;
        private IReadOnlyList<SystemDescriptor> _snapshot = Array.Empty<SystemDescriptor>();

        public SystemCatalogCache(Func<IEnumerable<SystemDescriptor>> provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public IReadOnlyList<SystemDescriptor> Snapshot => _snapshot;

        public bool TryRefresh(out string error)
        {
            try
            {
                var discovered = (_provider() ?? Array.Empty<SystemDescriptor>())
                    .Where(system => system != null && !string.IsNullOrWhiteSpace(system.FullTypeName))
                    .ToArray();
                _snapshot = discovered;
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetBaseException().Message;
                return false;
            }
        }
    }
}
