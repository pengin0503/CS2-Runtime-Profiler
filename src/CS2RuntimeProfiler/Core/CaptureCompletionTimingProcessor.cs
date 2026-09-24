using System;
using System.Collections.Generic;
using System.Linq;

namespace CS2RuntimeProfiler.Core
{
    /// <summary>
    /// Applies system-timing projection to newly completed captures exactly once.
    /// Runtime callers can invoke this after each capture-controller update without reprocessing history.
    /// </summary>
    public sealed class CaptureCompletionTimingProcessor
    {
        private readonly IReadOnlyList<SystemDescriptor> _systems;
        private readonly IReadOnlyList<RecorderDescriptor> _recorders;

        public CaptureCompletionTimingProcessor(
            IEnumerable<SystemDescriptor> systems,
            IEnumerable<RecorderDescriptor> recorders)
        {
            _systems = (systems ?? Array.Empty<SystemDescriptor>()).Where(x => x != null).ToArray();
            _recorders = (recorders ?? Array.Empty<RecorderDescriptor>()).Where(x => x != null).ToArray();
        }

        public int ProcessedCount { get; private set; }

        public void ProcessNew(IReadOnlyList<CaptureSession> completedCaptures)
        {
            if (completedCaptures == null)
                return;

            while (ProcessedCount < completedCaptures.Count)
            {
                var capture = completedCaptures[ProcessedCount];
                ProcessedCount++;
                if (capture == null)
                    continue;

                CaptureSystemTimingFinalizer.Apply(capture, _systems, _recorders);
            }
        }
    }
}
