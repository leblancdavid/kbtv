#nullable enable

using Godot;
using KBTV.Audio;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Monitors
{
    /// <summary>
    /// Drains Vern's Emotional/Mental stats while a caller is on air and the
    /// soundboard CALLER channel is off-perfect (worse than GREEN). After a
    /// short grace period the drain runs each frame and accelerates every few
    /// seconds up to a cap, resetting when the caller hangs up or the mix is
    /// brought back to perfect.
    ///
    /// Grading reuses <see cref="SoundboardTargetGenerator"/> so the monitor and
    /// the <see cref="UI.SoundboardOverlay"/> can never disagree.
    /// </summary>
    public partial class SoundboardMonitor : DomainMonitor, ICallerRepositoryObserver
    {
        /// <summary>Seconds of free time after a caller goes on air before drain starts.</summary>
        public const float GracePeriod = 10f;

        /// <summary>Stat points drained per second while the mix is off-perfect (base).</summary>
        public const float GraceDrainRate = 0.5f;

        /// <summary>Seconds of continuous off-perfect mixing before the drain accelerates.</summary>
        public const float DrainAccelerationInterval = 5f;

        /// <summary>Stat points/second added to the drain on each acceleration step.</summary>
        public const float DrainRateStep = 0.5f;

        /// <summary>Drain never exceeds this many stat points per second.</summary>
        public const float MaxDrainRate = 3f;

        private GameStateManager? _gameState;
        private VernStats? _vernStats;
        private SoundboardMixerDriver? _driver;
        private bool _subscribed;

        private bool _callerOnAir;
        private float _graceRemaining;
        private float _accelerationTimer;
        private float _drainRate;
        private SoundboardCallerBands _callerBands;

        /// <summary>Whether a caller is currently on air.</summary>
        public bool IsCallerOnAir => _callerOnAir;

        /// <summary>Seconds remaining in the grace period.</summary>
        public float GraceRemaining => _graceRemaining;

        /// <summary>True while Vern is being drained this frame.</summary>
        public bool IsDraining => _callerOnAir
            && _graceRemaining <= 0f
            && _drainRate > 0f
            && SoundboardTargetGenerator.IsOffPerfect(SoundboardTargetGenerator.GetWorstBand(_callerBands));

        /// <summary>Active drain rate (0 when not draining).</summary>
        public float CurrentDrainRate => IsDraining ? _drainRate : 0f;

        /// <summary>Per-knob CALLER bands for the current on-air caller.</summary>
        public SoundboardCallerBands CallerBands => _callerBands;

        /// <summary>Worst CALLER band for the current on-air caller.</summary>
        public SoundboardBand OverallBand => SoundboardTargetGenerator.GetWorstBand(_callerBands);

        /// <summary>The mixer driver the monitor grades against.</summary>
        public SoundboardMixerDriver? Driver => _driver;

        private GameStateManager GameStateManager => DependencyInjection.Get<GameStateManager>(this);

        /// <summary>
        /// Supplies the knob state the monitor grades. Usually set by World3D after
        /// creating the overlay (the overlay owns the driver).
        /// </summary>
        public void SetDriver(SoundboardMixerDriver driver) => _driver = driver;

        /// <summary>
        /// Binds a repository directly (used by tests; game path resolves via DI in
        /// <see cref="OnResolved"/>). Subscribes if not already subscribed.
        /// </summary>
        public void BindRepository(ICallerRepository repository)
        {
            if (repository != null && repository != _repository)
            {
                _repository?.Unsubscribe(this);
                _repository = repository;
                _subscribed = false;
            }

            if (_repository != null && !_subscribed)
            {
                _repository.Subscribe(this);
                _subscribed = true;
            }
        }

        /// <summary>Binds Vern's stats directly (used by tests; game path resolves via DI).</summary>
        public void BindVernStats(VernStats vernStats) => _vernStats = vernStats;

        public override void OnResolved()
        {
            base.OnResolved();
            BindRepository(_repository);
            _gameState = GameStateManager;
            _vernStats = _gameState?.VernStats;
        }

        public override void _ExitTree()
        {
            if (_subscribed && _repository != null)
            {
                _repository.Unsubscribe(this);
                _subscribed = false;
            }
        }

        public void OnCallerAdded(Caller caller) { }

        public void OnCallerRemoved(Caller caller) { }

        public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState) { }

        public void OnScreeningStarted(Caller caller) { }

        public void OnScreeningEnded(Caller caller, bool approved) { }

        public void OnCallerOnAir(Caller caller)
        {
            _callerOnAir = true;
            _graceRemaining = GracePeriod;
            _accelerationTimer = 0f;
            _drainRate = GraceDrainRate;
        }

        public void OnCallerOnAirEnded(Caller caller)
        {
            _callerOnAir = false;
            _graceRemaining = 0f;
            _accelerationTimer = 0f;
            _drainRate = GraceDrainRate;
            _callerBands = new SoundboardCallerBands(SoundboardBand.None, SoundboardBand.None, SoundboardBand.None);
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (!_callerOnAir)
            {
                return;
            }

            RefreshCallerBands();

            if (_graceRemaining > 0f)
            {
                _graceRemaining -= deltaTime;
                return;
            }

            if (!SoundboardTargetGenerator.IsOffPerfect(SoundboardTargetGenerator.GetWorstBand(_callerBands)))
            {
                _accelerationTimer = 0f;
                _drainRate = GraceDrainRate;
                return;
            }

            if (_repository?.OnAirCaller == null || _vernStats == null)
            {
                return;
            }

            _vernStats.Emotional.Modify(-_drainRate * deltaTime);
            _vernStats.Mental.Modify(-_drainRate * deltaTime);

            _accelerationTimer += deltaTime;
            if (_accelerationTimer >= DrainAccelerationInterval)
            {
                _accelerationTimer = 0f;
                _drainRate = Mathf.Min(_drainRate + DrainRateStep, MaxDrainRate);
            }
        }

        private void RefreshCallerBands()
        {
            var caller = _repository?.OnAirCaller;
            if (caller == null || _driver == null)
            {
                _callerBands = new SoundboardCallerBands(SoundboardBand.None, SoundboardBand.None, SoundboardBand.None);
                return;
            }

            _callerBands = SoundboardTargetGenerator.GetCallerBands(_driver.State, caller.SpeakingVolume);
        }
    }
}