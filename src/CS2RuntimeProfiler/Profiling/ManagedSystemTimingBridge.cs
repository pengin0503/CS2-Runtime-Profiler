using System;
using System.Collections.Generic;
using CS2RuntimeProfiler.Core;

namespace CS2RuntimeProfiler.Profiling
{
    internal static class ManagedSystemTimingBridge
    {
        private static readonly object Gate = new object();
        private static ManagedSystemTimingAccumulator _active;

        public static bool IsActive
        {
            get
            {
                lock (Gate)
                    return _active != null;
            }
        }

        public static void BeginCapture()
        {
            lock (Gate)
                _active = new ManagedSystemTimingAccumulator();
        }

        public static void Record(string systemId, double milliseconds)
        {
            lock (Gate)
                _active?.Record(systemId, milliseconds);
        }

        public static SystemTimingSnapshot EndCapture(IEnumerable<SystemDescriptor> systems)
        {
            ManagedSystemTimingAccumulator completed;
            lock (Gate)
            {
                completed = _active;
                _active = null;
            }

            return completed?.BuildSnapshot(systems) ?? new SystemTimingSnapshot();
        }

        public static void AbortCapture()
        {
            lock (Gate)
                _active = null;
        }
    }
}
