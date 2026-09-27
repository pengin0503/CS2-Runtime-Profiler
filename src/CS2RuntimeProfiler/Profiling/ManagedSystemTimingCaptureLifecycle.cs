using System;
using System.Collections.Generic;
using CS2RuntimeProfiler.Core;

namespace CS2RuntimeProfiler.Profiling
{
    public interface IManagedSystemTimingInstrumentation : IDisposable
    {
        bool TryInstall(out string reason);
    }

    public interface IManagedSystemTimingBridge
    {
        bool IsActive { get; }
        void BeginCapture();
        SystemTimingSnapshot EndCapture(IEnumerable<SystemDescriptor> systems);
        void AbortCapture();
    }

    public sealed class ManagedSystemTimingCaptureLifecycle : IDisposable
    {
        private readonly IManagedSystemTimingInstrumentation _instrumentation;
        private readonly IManagedSystemTimingBridge _bridge;
        private bool _active;

        public ManagedSystemTimingCaptureLifecycle(
            IManagedSystemTimingInstrumentation instrumentation,
            IManagedSystemTimingBridge bridge)
        {
            _instrumentation = instrumentation ?? throw new ArgumentNullException(nameof(instrumentation));
            _bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        }

        public bool IsActive => _active;

        public bool TryStart(out string reason)
        {
            if (_active)
            {
                reason = null;
                return true;
            }

            if (!_instrumentation.TryInstall(out reason))
                return false;

            try
            {
                _bridge.BeginCapture();
                if (!_bridge.IsActive)
                    throw new InvalidOperationException("Managed timing bridge did not enter the active state.");

                _active = true;
                reason = null;
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    _bridge.AbortCapture();
                }
                finally
                {
                    _instrumentation.Dispose();
                    _active = false;
                }

                reason = ex.GetBaseException().Message;
                return false;
            }
        }

        public SystemTimingSnapshot Finish(IEnumerable<SystemDescriptor> systems)
        {
            if (!_active)
                return new SystemTimingSnapshot();

            try
            {
                return _bridge.EndCapture(systems) ?? new SystemTimingSnapshot();
            }
            finally
            {
                _active = false;
                _instrumentation.Dispose();
            }
        }

        public void Abort()
        {
            if (!_active && !_bridge.IsActive)
                return;

            try
            {
                _bridge.AbortCapture();
            }
            finally
            {
                _active = false;
                _instrumentation.Dispose();
            }
        }

        public void Dispose() => Abort();
    }
}
