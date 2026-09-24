using System;
using System.Collections.Generic;

namespace CS2RuntimeProfiler.Core
{
    /// <summary>
    /// Finalizes per-system timing for a completed capture from already captured profiler markers.
    /// Keeps the projection pure so the runtime system only coordinates lifecycle boundaries.
    /// </summary>
    public static class CaptureSystemTimingFinalizer
    {
        public static SystemTimingSnapshot Apply(
            CaptureSession capture,
            IEnumerable<SystemDescriptor> systems,
            IEnumerable<RecorderDescriptor> recorders)
        {
            if (capture == null)
                throw new ArgumentNullException(nameof(capture));

            var timing = SystemMarkerTimingProjector.Project(systems, recorders, capture);
            capture.SetSystemTiming(timing);
            return timing;
        }
    }
}
