using System;
using System.Collections.Generic;
using System.Linq;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.Tests.Unit.Callers
{
    public class TestCallerRepositoryObserver : ICallerRepositoryObserver
    {
        public List<Caller> AddedCallers = new();
        public List<Caller> RemovedCallers = new();
        public List<(Caller caller, CallerState oldState, CallerState newState)> StateChanges = new();
        public List<Caller> ScreeningStartedCallers = new();
        public List<(Caller caller, bool approved)> ScreeningEndedCallers = new();
        public List<Caller> OnAirCallers = new();
        public List<Caller> OnAirEndedCallers = new();

        public void OnCallerAdded(Caller caller) => AddedCallers.Add(caller);
        public void OnCallerRemoved(Caller caller) => RemovedCallers.Add(caller);
        public void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState)
            => StateChanges.Add((caller, oldState, newState));
        public void OnScreeningStarted(Caller caller) => ScreeningStartedCallers.Add(caller);
        public void OnScreeningEnded(Caller caller, bool approved) => ScreeningEndedCallers.Add((caller, approved));
        public void OnCallerOnAir(Caller caller) => OnAirCallers.Add(caller);
        public void OnCallerOnAirEnded(Caller caller) => OnAirEndedCallers.Add(caller);
    }

    public class CallerRepositoryTests : KBTVTestClass
    {
        public CallerRepositoryTests(Node testScene) : base(testScene) { }

        private CallerRepository _repository = null!;
        private TestCallerRepositoryObserver _observer = null!;

        [Setup]
        public void Setup()
        {
            _repository = new CallerRepository();
            _observer = new TestCallerRepositoryObserver();
            _repository.Subscribe(_observer);
        }

        private Caller CreateTestCaller(string name = "Test Caller")
        {
            return new Caller(
                name,
                "555-0123",
                "Test Location",
                "Ghosts",
                "Ghosts",
                "Test Reason",
                CallerLegitimacy.Credible,
                CallerPhoneQuality.Good,
                CallerEmotionalState.Calm,
                CallerCurseRisk.Low,
                CallerBeliefLevel.Curious,
                CallerEvidenceLevel.None,
                CallerCoherence.Coherent,
                CallerUrgency.Low,
                "personality",
                "arc",
                "summary",
                30f,
                0.8f
            );
        }

        [Test]
        public void Constructor_InitializesEmptyRepository()
        {
            AssertThat(_repository.IncomingCallers.Count == 0);
            AssertThat(_repository.OnHoldCallers.Count == 0);
            AssertThat(_repository.CurrentScreening == null);
            AssertThat(_repository.OnAirCaller == null);
        }

        [Test]
        public void Constructor_InitializesStateQueries()
        {
            AssertThat(!_repository.HasIncomingCallers);
            AssertThat(!_repository.HasOnHoldCallers);
            AssertThat(!_repository.IsScreening);
            AssertThat(!_repository.IsOnAir);
            AssertThat(_repository.CanAcceptMoreCallers);
            AssertThat(_repository.CanPutOnHold);
        }

        [Test]
        public void AddCaller_NullCaller_ReturnsFailure()
        {
            var result = _repository.AddCaller(null!);

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "NULL_CALLER");
        }

        [Test]
        public void AddCaller_ValidCaller_AddsToRepository()
        {
            var caller = CreateTestCaller();
            var result = _repository.AddCaller(caller);

            AssertThat(result.IsSuccess);
            AssertThat(result.Value == caller);
            AssertThat(_repository.IncomingCallers.Contains(caller));
            AssertThat(caller.State == CallerState.Incoming);
        }

        [Test]
        public void AddCaller_NotifiesObserver()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);

            AssertThat(_observer.AddedCallers.Contains(caller));
        }

        [Test]
        public void AddCaller_DuplicateCaller_ReturnsFailure()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);

            var result = _repository.AddCaller(caller);

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "DUPLICATE_CALLER");
        }

        [Test]
        public void AddCaller_MaxIncoming_DoesNotAcceptMore()
        {
            for (int i = 0; i < 10; i++)
            {
                _repository.AddCaller(CreateTestCaller($"Caller {i}"));
            }

            var result = _repository.AddCaller(CreateTestCaller("Overflow"));

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "QUEUE_FULL");
            AssertThat(!_repository.CanAcceptMoreCallers);
        }

        [Test]
        public void StartScreening_NullCaller_ReturnsFailure()
        {
            var result = _repository.StartScreening(null!);

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "NULL_CALLER");
        }

        [Test]
        public void StartScreening_AlreadyScreening_ReturnsFailure()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            var result = _repository.StartScreening(caller);

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "SCREENING_BUSY");
        }

        [Test]
        public void StartScreening_CallerNotFound_ReturnsFailure()
        {
            var caller = CreateTestCaller();

            var result = _repository.StartScreening(caller);

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "CALLER_NOT_FOUND");
        }

        [Test]
        public void StartScreening_ValidCaller_SetsCurrentScreening()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            var result = _repository.StartScreening(caller);

            AssertThat(result.IsSuccess);
            AssertThat(_repository.CurrentScreening == caller);
            AssertThat(_repository.IsScreening);
            AssertThat(caller.State == CallerState.Screening);
        }

        [Test]
        public void StartScreeningNext_AlreadyScreening_ReturnsFailure()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            var result = _repository.StartScreeningNext();

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "SCREENING_BUSY");
        }

        [Test]
        public void StartScreeningNext_NoIncoming_ReturnsFailure()
        {
            var result = _repository.StartScreeningNext();

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "NO_INCOMING");
        }

        [Test]
        public void StartScreeningNext_WithIncoming_StartsScreening()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);

            var result = _repository.StartScreeningNext();

            AssertThat(result.IsSuccess);
            AssertThat(_repository.CurrentScreening == caller);
        }

        [Test]
        public void ApproveScreening_NoScreening_ReturnsFailure()
        {
            var result = _repository.ApproveScreening();

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "NO_SCREENING");
        }

        [Test]
        public void ApproveScreening_FullHoldQueue_ReturnsFailure()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            for (int i = 0; i < 9; i++)
            {
                var holdCaller = CreateTestCaller($"Hold {i}");
                _repository.AddCaller(holdCaller);
                _repository.StartScreening(holdCaller);
                _repository.ApproveScreening();
            }

            AssertThat(_repository.CanPutOnHold);

            var result = _repository.ApproveScreening();

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "NO_SCREENING");
        }

        [Test]
        public void ApproveScreening_ValidScreening_MovesToOnHold()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            var result = _repository.ApproveScreening();

            AssertThat(result.IsSuccess);
            AssertThat(_repository.OnHoldCallers.Contains(caller));
            AssertThat(caller.State == CallerState.OnHold);
            AssertThat(_repository.CurrentScreening == null);
            AssertThat(!_repository.IsScreening);
        }

        [Test]
        public void RejectScreening_NoScreening_ReturnsFailure()
        {
            var result = _repository.RejectScreening();

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "NO_SCREENING");
        }

        [Test]
        public void RejectScreening_ValidScreening_RemovesCaller()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);

            var result = _repository.RejectScreening();

            AssertThat(result.IsSuccess);
            AssertThat(_repository.CurrentScreening == null);
            AssertThat(_observer.RemovedCallers.Contains(caller));
        }

        [Test]
        public void RejectScreening_RemovedFromIncomingCallers()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            AssertThat(_repository.IncomingCallers.Contains(caller));

            _repository.StartScreening(caller);
            AssertThat(!_repository.IncomingCallers.Contains(caller));

            _repository.RejectScreening();

            AssertThat(_repository.CurrentScreening == null);
            AssertThat(_repository.IncomingCallers.Count == 0);
            AssertThat(!_repository.IncomingCallers.Contains(caller));
        }

        [Test]
        public void PutOnAir_AlreadyOnAir_ReturnsFailure()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);
            _repository.ApproveScreening();
            _repository.PutOnAir();

            var result = _repository.PutOnAir();

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "ON_AIR_BUSY");
        }

        [Test]
        public void PutOnAir_NoOnHold_ReturnsFailure()
        {
            var result = _repository.PutOnAir();

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "NO_ON_HOLD");
        }

        [Test]
        public void PutOnAir_WithOnHold_PutsCallerOnAir()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);
            _repository.ApproveScreening();

            var result = _repository.PutOnAir();

            AssertThat(result.IsSuccess);
            AssertThat(_repository.OnAirCaller == caller);
            AssertThat(caller.State == CallerState.OnAir);
            AssertThat(_repository.IsOnAir);
        }

        [Test]
        public void EndOnAir_NoOnAir_ReturnsFailure()
        {
            var result = _repository.EndOnAir();

            AssertThat(result.IsFailure);
            AssertThat(result.ErrorCode == "NO_ON_AIR");
        }

        [Test]
        public void EndOnAir_WithOnAir_CompletesCaller()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);
            _repository.ApproveScreening();
            _repository.PutOnAir();

            var result = _repository.EndOnAir();

            AssertThat(result.IsSuccess);
            AssertThat(_repository.OnAirCaller == null);
            AssertThat(!_repository.IsOnAir);
        }

        [Test]
        public void SetCallerState_NullCaller_ReturnsFalse()
        {
            var result = _repository.SetCallerState(null!, CallerState.Screening);
            AssertThat(!result);
        }

        [Test]
        public void SetCallerState_NotInRepository_ReturnsFalse()
        {
            var caller = CreateTestCaller();
            var result = _repository.SetCallerState(caller, CallerState.Screening);
            AssertThat(!result);
        }

        [Test]
        public void SetCallerState_ValidCaller_UpdatesState()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);

            var result = _repository.SetCallerState(caller, CallerState.Screening);

            AssertThat(result);
            AssertThat(caller.State == CallerState.Screening);
            AssertThat(_observer.StateChanges.Count == 1);
        }

        [Test]
        public void RemoveCaller_NullCaller_ReturnsFalse()
        {
            var result = _repository.RemoveCaller(null!);
            AssertThat(!result);
        }

        [Test]
        public void RemoveCaller_NotInRepository_ReturnsFalse()
        {
            var caller = CreateTestCaller();
            var result = _repository.RemoveCaller(caller);
            AssertThat(!result);
        }

        [Test]
        public void RemoveCaller_ValidCaller_RemovesFromRepository()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);

            var result = _repository.RemoveCaller(caller);

            AssertThat(result);
            AssertThat(!_repository.IncomingCallers.Contains(caller));
            AssertThat(_observer.RemovedCallers.Contains(caller));
        }

        [Test]
        public void ClearAll_RemovesAllCallers()
        {
            for (int i = 0; i < 5; i++)
            {
                _repository.AddCaller(CreateTestCaller($"Caller {i}"));
            }

            _repository.ClearAll();

            AssertThat(_repository.IncomingCallers.Count == 0);
            AssertThat(_repository.OnHoldCallers.Count == 0);
            AssertThat(_repository.CurrentScreening == null);
            AssertThat(_repository.OnAirCaller == null);
        }

        [Test]
        public void Subscribe_Observer_ReceivesEvents()
        {
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);

            AssertThat(_observer.AddedCallers.Contains(caller));
        }

        [Test]
        public void Unsubscribe_Observer_StopsReceivingEvents()
        {
            _repository.Unsubscribe(_observer);
            var caller = CreateTestCaller();
            _repository.AddCaller(caller);

            AssertThat(_observer.AddedCallers.Count == 0);
        }

        [Test]
        public void HasIncomingCallers_ReturnsCorrectState()
        {
            AssertThat(!_repository.HasIncomingCallers);

            var caller = CreateTestCaller();
            _repository.AddCaller(caller);

            AssertThat(_repository.HasIncomingCallers);
        }

        [Test]
        public void HasOnHoldCallers_ReturnsCorrectState()
        {
            AssertThat(!_repository.HasOnHoldCallers);

            var caller = CreateTestCaller();
            _repository.AddCaller(caller);
            _repository.StartScreening(caller);
            _repository.ApproveScreening();

            AssertThat(_repository.HasOnHoldCallers);
        }
    }
}
