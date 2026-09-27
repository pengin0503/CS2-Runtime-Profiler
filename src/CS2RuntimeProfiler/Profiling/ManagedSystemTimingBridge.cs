using System;
using System.Collections.Generic;
using CS2RuntimeProfiler.Core;

namespace CS2RuntimeProfiler.Profiling
{
    internal static class ManagedSystemTimingBridge
    {
        private static readonly object Gate = new object();
        private static volatile ManagedSystemTimingAccumulator _active;

        // This check runs from the Harmony prefix of every managed SystemBase.Update call.
        // Keep the normal-monitoring path lock-free; locking is limited to active Deep Capture.
        public static bool IsActive => _active != null;

        public static void BeginCapture()
        {
            lock (Gate)
                _active = new ManagedSystemTimingAccumulator();
        }

        public static void Record(string systemId, double milliseconds)
        {
            var active = _active;
            if (active == null)
                return;

            lock (Gate)
            {
                if (ReferenceEquals(_active, active))
                    active.Record(systemId, milliseconds);
            }
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
