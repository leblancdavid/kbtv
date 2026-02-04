#nullable enable

using System;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;

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
        private readonly ICallerRepository _callerRepository;

        public Caller? CurrentCaller => _session?.Caller;
        public bool IsActive => _session != null && _phase != ScreeningPhase.Completed;
        public ScreeningPhase Phase => _phase;
        public ScreeningProgress Progress => CreateProgress();

        public event Action<ScreeningPhase>? PhaseChanged;
        public event Action<ScreeningProgress>? ProgressUpdated;

        public ScreeningController(ICallerRepository callerRepository)
        {
            _callerRepository = callerRepository;
        }

        public void Start(Caller caller)
        {
            if (caller == null)
            {
                GD.PrintErr("ScreeningController: Cannot start with null caller");
                return;
            }

            _session = new ScreeningSession(caller);
            SetPhase(ScreeningPhase.Gathering);

            GD.Print($"ScreeningController: Started for {caller.Name}");
        }

        public Result<Caller> Approve()
        {
            if (_session == null)
            {
                return Result<Caller>.Fail("NO_SESSION", "No active screening session");
            }

            var repository = _callerRepository;
            if (repository == null)
            {
                return Result<Caller>.Fail("NO_REPOSITORY", "Repository not available");
            }

            if (!repository.IsScreening)
            {
                GD.PrintErr($"ScreeningController: State mismatch - controller has session but repository is not screening. Caller: {_session.Caller.Name}");
                _session = null;
                return Result<Caller>.Fail("NO_SCREENING", "No caller being screened (state mismatch detected)");
            }

            // Save caller reference and clear session BEFORE calling repository
            // This allows AutoStartNextScreening() to create a new session without it being overwritten
            var caller = _session.Caller;
            _session = null;
            SetPhase(ScreeningPhase.Completed);
            GD.Print($"ScreeningController: Approved {caller.Name}");

            var result = repository.ApproveScreening();
            if (result.IsSuccess)
            {
                // Calculate and award belief points
                var beliefReward = CalculateBeliefReward(caller, true);
                AwardBeliefPoints(caller.ClaimedTopic, beliefReward);
                ShowBeliefRewardNotification(caller, beliefReward, true);
                return Result<Caller>.Ok(caller);
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

            var repository = _callerRepository;
            if (repository == null)
            {
                return Result<Caller>.Fail("NO_REPOSITORY", "Repository not available");
            }

            if (!repository.IsScreening)
            {
                GD.PrintErr($"ScreeningController: State mismatch - controller has session but repository is not screening. Caller: {_session.Caller.Name}");
                _session = null;
                return Result<Caller>.Fail("NO_SCREENING", "No caller being screened (state mismatch detected)");
            }

            // Save caller reference and clear session BEFORE calling repository
            // This allows AutoStartNextScreening() to create a new session without it being overwritten
            var caller = _session.Caller;
            _session = null;
            SetPhase(ScreeningPhase.Completed);
            GD.Print($"ScreeningController: Rejected {caller.Name}");

            var result = repository.RejectScreening();
            if (result.IsSuccess)
            {
                // Calculate and award belief points (negative for rejection)
                var beliefReward = CalculateBeliefReward(caller, false);
                AwardBeliefPoints(caller.ClaimedTopic, beliefReward);
                ShowBeliefRewardNotification(caller, beliefReward, false);
                return Result<Caller>.Ok(caller);
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
        }

        private void HandlePatienceExpired()
        {
            if (_session == null)
            {
                return;
            }

            GD.Print($"ScreeningController: Patience expired for {_session.Caller.Name}");

            var repository = _callerRepository;
            repository?.RemoveCaller(_session.Caller);

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

            switch (newPhase)
            {
                case ScreeningPhase.Gathering:
                    break;
                case ScreeningPhase.Deciding:
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

        private int CalculateBeliefReward(Caller caller, bool approved)
        {
            // Base belief rewards based on caller quality and approval
            if (approved)
            {
                // Approved callers: +10 to +20 belief points based on quality
                return caller.Quality > 0.5f ? 20 : 10;
            }
            else
            {
                // Rejected callers: -5 to -15 belief points based on quality
                return caller.Quality > 0.5f ? -5 : -15;
            }
        }

        private void AwardBeliefPoints(string topic, int points)
        {
            // TODO: Connect to actual TopicBelief system
            GD.Print($"ScreeningController: Awarded {points} belief points for topic '{topic}'");

            // Placeholder: In a real implementation, this would update TopicBelief.ApplyGoodCaller/ApplyBadCaller
            // based on whether points are positive or negative
        }

        private void ShowBeliefRewardNotification(Caller caller, int beliefPoints, bool approved)
        {
            var action = approved ? "APPROVED" : "REJECTED";
            var topic = caller.ClaimedTopic;
            var sign = beliefPoints >= 0 ? "+" : "";
            var message = $"{action}: {sign}{beliefPoints} Belief Points\nTopic: {topic}";

            GD.Print($"ScreeningController: {message}");

            // TODO: Display notification in UI
            // For now, just log. In real implementation, show popup in ScreeningPanel
        }
    }
}
