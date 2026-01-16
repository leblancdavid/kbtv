#nullable enable

using System;
using Godot;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.Screening
{
    /// <summary>
    /// Manages the screening workflow for callers.
    /// Encapsulates revelation timing, patience tracking, and state machine.
    /// </summary>
    public partial class ScreeningController : IScreeningController
    {
        private ScreeningSession? _session;
        private ScreeningPhase _phase = ScreeningPhase.Idle;

        public Caller? CurrentCaller => _session?.Caller;
        public bool IsActive => _session != null && _phase != ScreeningPhase.Completed;
        public ScreeningPhase Phase => _phase;
        public ScreeningProgress Progress => CreateProgress();

        public event Action<ScreeningPhase>? PhaseChanged;
        public event Action<ScreeningProgress>? ProgressUpdated;

        public void Start(Caller caller)
        {
            if (caller == null)
            {
                GD.PrintErr("ScreeningController: Cannot start with null caller");
                return;
            }

            _session = new ScreeningSession(caller);
            SetPhase(ScreeningPhase.Gathering);

            var events = ServiceRegistry.Instance?.EventAggregator;
            events?.Publish(new Core.Events.Screening.ScreeningStarted { Caller = caller });

            GD.Print($"ScreeningController: Started for {caller.Name}");
        }

        public Result<Caller> Approve()
        {
            if (_session == null)
            {
                return Result<Caller>.Fail("NO_SESSION", "No active screening session");
            }

            var repository = ServiceRegistry.Instance?.CallerRepository;
            if (repository == null)
            {
                return Result<Caller>.Fail("NO_REPOSITORY", "Repository not available");
            }

            var result = repository.ApproveScreening();
            if (result.IsSuccess)
            {
                SetPhase(ScreeningPhase.Completed);
            var events = ServiceRegistry.Instance?.EventAggregator;
            events?.Publish(new Core.Events.Screening.ScreeningApproved { Caller = _session.Caller });

                GD.Print($"ScreeningController: Approved {_session.Caller.Name}");
                return Result<Caller>.Ok(_session.Caller);
            }

            GD.PrintErr($"ScreeningController: Failed to approve - {result.ErrorMessage}");
            return Result<Caller>.Fail(result.ErrorCode ?? "UNKNOWN", result.ErrorMessage);
        }

        public Result<Caller> Reject()
        {
            if (_session == null)
            {
                return Result<Caller>.Fail("NO_SESSION", "No active screening session");
            }

            var repository = ServiceRegistry.Instance?.CallerRepository;
            if (repository == null)
            {
                return Result<Caller>.Fail("NO_REPOSITORY", "Repository not available");
            }

            var result = repository.RejectScreening();
            if (result.IsSuccess)
            {
                SetPhase(ScreeningPhase.Completed);
            var events = ServiceRegistry.Instance?.EventAggregator;
            events?.Publish(new Core.Events.Screening.ScreeningRejected { Caller = _session.Caller });

                GD.Print($"ScreeningController: Rejected {_session.Caller.Name}");
                return Result<Caller>.Ok(_session.Caller);
            }

            GD.PrintErr($"ScreeningController: Failed to reject - {result.ErrorMessage}");
            return Result<Caller>.Fail(result.ErrorCode ?? "UNKNOWN", result.ErrorMessage);
        }

        public void Update(float deltaTime)
        {
            if (_session == null)
            {
                return;
            }

            _session.Update(deltaTime);

            if (!_session.HasPatience)
            {
                HandlePatienceExpired();
            }

            var progress = CreateProgress();
            ProgressUpdated?.Invoke(progress);

            var events = ServiceRegistry.Instance?.EventAggregator;
            events?.Publish(new Core.Events.Screening.ScreeningProgressUpdated
            {
                PropertiesRevealed = _session.PropertiesRevealed,
                TotalProperties = _session.TotalProperties,
                PatienceRemaining = _session.PatienceRemaining,
                ElapsedTime = _session.ElapsedTime
            });
        }

        private void HandlePatienceExpired()
        {
            if (_session == null)
            {
                return;
            }

            GD.Print($"ScreeningController: Patience expired for {_session.Caller.Name}");

            var repository = ServiceRegistry.Instance?.CallerRepository;
            repository?.RemoveCaller(_session.Caller);

            var events = ServiceRegistry.Instance?.EventAggregator;
            events?.Publish(new Core.Events.Screening.ScreeningEnded { Caller = _session.Caller });

            _session = null;
            SetPhase(ScreeningPhase.Completed);
        }

        private void SetPhase(ScreeningPhase newPhase)
        {
            if (_phase == newPhase)
            {
                return;
            }

            var oldPhase = _phase;
            _phase = newPhase;
            PhaseChanged?.Invoke(_phase);

            var events = ServiceRegistry.Instance?.EventAggregator;
            switch (newPhase)
            {
                case ScreeningPhase.Gathering:
                    break;
                case ScreeningPhase.Deciding:
                    events?.Publish(new Core.Events.Screening.ScreeningEnded { Caller = null });
                    break;
            }

            GD.Print($"ScreeningController: Phase {oldPhase} -> {newPhase}");
        }

        private ScreeningProgress CreateProgress()
        {
            if (_session == null)
            {
                return new ScreeningProgress(0, 0, 0f, 0f, 0f);
            }

            return new ScreeningProgress(
                _session.PropertiesRevealed,
                _session.TotalProperties,
                _session.PatienceRemaining,
                _session.MaxPatience,
                _session.ElapsedTime
            );
        }
    }
}
