using System;
using System.Collections.Generic;
using System.Linq;

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

            var systemArray = (systems ?? Array.Empty<SystemDescriptor>()).Where(x => x != null).ToArray();
            var recorderArray = (recorders ?? Array.Empty<RecorderDescriptor>()).Where(x => x != null).ToArray();
            var timing = SystemMarkerTimingProjector.Project(systemArray, recorderArray, capture);
            capture.SetSystemTiming(timing);

            if (timing.Systems.Count == 0)
            {
                var diagnostics = SystemMarkerTimingProjector.Diagnose(systemArray, recorderArray, capture);
                var sampledMarkers = capture.MarkerSamples.Count;
                capture.AddWarning(
                    $"System timing unavailable: catalogSystems={diagnostics.CatalogSystemCount}, profilerMarkers={diagnostics.ProfilerMarkerCount}, timeMarkers={diagnostics.TimeMarkerCount}, uniqueMatches={diagnostics.UniqueMatchCount}, ambiguousMatches={diagnostics.AmbiguousMatchCount}, uniqueMatchesWithoutSamples={diagnostics.UniqueMatchesWithoutSamples}, sampledMarkers={sampledMarkers}, capturedMarkers={capture.MarkerCoverage.Captured}/{capture.MarkerCoverage.Discovered}. No uniquely matching TimeNanoseconds ECS system marker produced a usable sample.");
            }

            return timing;
        }
    }
}
