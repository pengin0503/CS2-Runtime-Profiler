using System;

namespace CS2RuntimeProfiler.Core
{
    public sealed class DeepCaptureStateMachine
    {
        private readonly double _efficiencyThreshold;
        private readonly double _sustainSeconds;
        private readonly double _deepSeconds;
        private readonly double _postSeconds;
        private readonly double _cooldownSeconds;

        private double? _lowEfficiencySince;
        private double _stateEnteredAt;

        public DeepCaptureStateMachine(
            double efficiencyThreshold,
            double sustainSeconds,
            double deepSeconds,
            double postSeconds,
            double cooldownSeconds)
        {
            _efficiencyThreshold = efficiencyThreshold;
            _sustainSeconds = sustainSeconds;
            _deepSeconds = deepSeconds;
            _postSeconds = postSeconds;
            _cooldownSeconds = cooldownSeconds;
            State = CaptureState.Monitoring;
        }

        public CaptureState State { get; private set; }
        public CaptureTrigger LastTrigger { get; private set; }

        public static DeepCaptureStateMachine CreateDefault()
        {
            return new DeepCaptureStateMachine(
                efficiencyThreshold: 0.80,
                sustainSeconds: 2,
                deepSeconds: 10,
                postSeconds: 5,
                cooldownSeconds: 30);
        }

        public void Observe(double nowSeconds, double selectedSpeed, double actualSpeed)
        {
            AdvanceTimedStates(nowSeconds);

            if (State != CaptureState.Monitoring)
                return;

            if (selectedSpeed <= 0)
            {
                _lowEfficiencySince = null;
                return;
            }

            var efficiency = SimulationEfficiency.Calculate(selectedSpeed, actualSpeed);
            if (efficiency >= _efficiencyThreshold)
            {
                _lowEfficiencySince = null;
                return;
            }

            if (!_lowEfficiencySince.HasValue)
            {
                _lowEfficiencySince = nowSeconds;
                return;
            }

            if (nowSeconds - _lowEfficiencySince.Value >= _sustainSeconds)
                StartCapture(nowSeconds, new CaptureTrigger(CaptureTriggerKind.AutomaticLowEfficiency, nowSeconds, efficiency));
        }

        public void RequestManualCapture(double nowSeconds)
        {
            if (State != CaptureState.Monitoring && State != CaptureState.Cooldown)
                return;

            StartCapture(nowSeconds, new CaptureTrigger(CaptureTriggerKind.Manual, nowSeconds, null));
        }

        private void StartCapture(double nowSeconds, CaptureTrigger trigger)
        {
            State = CaptureState.DeepCapture;
            _stateEnteredAt = nowSeconds;
            _lowEfficiencySince = null;
            LastTrigger = trigger;
        }

        private void AdvanceTimedStates(double nowSeconds)
        {
            var advanced = true;
            while (advanced)
            {
                advanced = false;

                if (State == CaptureState.DeepCapture && nowSeconds - _stateEnteredAt >= _deepSeconds)
                {
                    _stateEnteredAt += _deepSeconds;
                    State = CaptureState.PostBuffer;
                    advanced = true;
                }
                else if (State == CaptureState.PostBuffer && nowSeconds - _stateEnteredAt >= _postSeconds)
                {
                    _stateEnteredAt += _postSeconds;
                    State = CaptureState.Cooldown;
                    advanced = true;
                }
                else if (State == CaptureState.Cooldown && nowSeconds - _stateEnteredAt >= _cooldownSeconds)
                {
                    _stateEnteredAt += _cooldownSeconds;
                    State = CaptureState.Monitoring;
                    _lowEfficiencySince = null;
                    advanced = true;
                }
            }
        }
    }
}
