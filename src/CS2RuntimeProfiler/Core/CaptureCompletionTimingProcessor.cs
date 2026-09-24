#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2RuntimeProfiler.Core
{
    /// <summary>
    /// Applies system-timing projection to newly completed captures exactly once.
    /// Runtime callers can invoke this after each capture-controller update without reprocessing retained history.
    /// </summary>
    public sealed class CaptureCompletionTimingProcessor
    {
        private readonly IReadOnlyList<SystemDescriptor> _systems;
        private readonly IReadOnlyList<RecorderDescriptor> _recorders;
        private readonly HashSet<CaptureSession> _processed = new HashSet<CaptureSession>();

        public CaptureCompletionTimingProcessor(
            IEnumerable<SystemDescriptor> systems,
            IEnumerable<RecorderDescriptor> recorders)
        {
            _systems = (systems ?? Array.Empty<SystemDescriptor>()).Where(x => x != null).ToArray();
            _recorders = (recorders ?? Array.Empty<RecorderDescriptor>()).Where(x => x != null).ToArray();
        }

        public int ProcessedCount { get; private set; }
        public CaptureSession? LastProcessedCapture { get; private set; }

        public void ProcessNew(IReadOnlyList<CaptureSession> completedCaptures)
        {
            if (completedCaptures == null)
                return;

            var retained = new HashSet<CaptureSession>(completedCaptures.Where(capture => capture != null));
            _processed.RemoveWhere(capture => !retained.Contains(capture));

            foreach (var capture in completedCaptures)
            {
                if (capture == null || !_processed.Add(capture))
                    continue;

                ProcessedCount++;
                LastProcessedCapture = capture;
                CaptureSystemTimingFinalizer.Apply(capture, _systems, _recorders);
            }
        }
    }
}
