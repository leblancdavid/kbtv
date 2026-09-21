using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Audio;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue
{
    public class BroadcastStateMachineTests : KBTVTestClass
    {
        private readonly Node _testScene;

        public BroadcastStateMachineTests(Node testScene) : base(testScene)
        {
            _testScene = testScene;
        }

        private sealed class Harness
        {
            public BroadcastStateMachine Machine { get; }
            public StateMachineCallerRepository Callers { get; } = new();
            public NullArcRepository Arcs { get; } = new();
            public EventBus Bus { get; } = new();
            public BroadcastStateManager StateManager { get; }

            public Harness(Node testScene, StateMachineCallerRepository callers, NullArcRepository arcs)
            {
                StateManager = new BroadcastStateManager();
                testScene.AddChild(StateManager);
                var audio = new SilentAudioService();
                Machine = new BroadcastStateMachine(
                    callers,
                    arcs,
                    new VernDialogueTemplate(),
                    Bus,
                    null!,            // ListenerManager - unused by these paths
                    audio,
                    testScene.GetTree(),
                    null!,            // AdManager - unused by these paths
                    StateManager,
                    null!,            // TimeManager - unused by these paths
                    null!,            // IGameStateManager - null-guarded
                    null!,            // DeadAirManager - null-guarded
                    null!);           // ConversationStatTracker - unused here
            }
        }

        [Test]
        public void CreateConversation_NoArcForOnAirCaller_ReleasesCaller()
        {
            var callers = new StateMachineCallerRepository();
            var harness = new Harness(_testScene, callers, new NullArcRepository());
            callers.SetupOnAirCaller(CreateTestCaller());
            harness.StateManager._hasPlayedVernOpening = true;

            var executable = harness.Machine.GetNextExecutable(AsyncBroadcastState.Conversation);

            AssertThat(callers.EndOnAirCalled, "stuck caller with no arc should be released");
            AssertNotNull(executable);
            AssertAreEqual("dead_air", executable!.Id);
        }

        [Test]
        public void RepeatedFallbackCycles_GuardForcesRecovery()
        {
            var callers = new StateMachineCallerRepository();
            var harness = new Harness(_testScene, callers, new NullArcRepository());
            var sm = harness.StateManager;

            Caller StuckCaller()
            {
                var caller = CreateTestCaller();
                callers.SetupOnAirCaller(caller);
                return caller;
            }

            DialogueExecutable Fallback() => new(
                "vern_fallback", "Welcome to the show.", "Vern",
                harness.Bus, new SilentAudioService(),
                lineType: VernLineType.Fallback, stateManager: sm);

            // Cycles 1 and 2: stuck caller + no on-hold callers pings between
            // Conversation states... the guard must NOT fire yet.
            var state1 = harness.Machine.UpdateStateAfterExecution(AsyncBroadcastState.Conversation, Fallback());
            AssertAreEqual(AsyncBroadcastState.Conversation, state1);
            AssertFalse(callers.EndOnAirCalled);

            // Cycle 3: guard fires, releases the stuck caller, forces DeadAir.
            var state3 = harness.Machine.UpdateStateAfterExecution(AsyncBroadcastState.Conversation, Fallback());
            AssertAreEqual(AsyncBroadcastState.DeadAir, state3);
            AssertThat(callers.EndOnAirCalled, "guard should release the stuck on-air caller");

            // Counter was reset: a single new fallback cycle must not re-fire the guard.
            callers.EndOnAirCalled = false;
            StuckCaller();
            var next = harness.Machine.UpdateStateAfterExecution(AsyncBroadcastState.Conversation, Fallback());
            AssertAreEqual(AsyncBroadcastState.Conversation, next);
            AssertFalse(callers.EndOnAirCalled);
        }

        private static Caller CreateTestCaller()
        {
            return new Caller(
                "Test Caller",
                "555-0123",
                "Test Location",
                "Ghosts",
                "Ghosts",
                "Test Reason",
                CallerLegitimacy.Credible,
                CallerPhoneQuality.Good,
                CallerEmotionalState.Calm,
                CallerCurseRisk.Low,
                CallerEvidenceLevel.None,
                CallerCoherence.Coherent,
                CallerUrgency.Low,
                "personality",
                null,
                null,
                null,
                "summary",
                30f,
                0.8f
            );
        }

        private class StateMachineCallerRepository : ICallerRepository
        {
            private Caller? _onAirCaller;

            public bool EndOnAirCalled { get; set; }

            public void SetupOnAirCaller(Caller? caller) => _onAirCaller = caller;

            public IReadOnlyList<Caller> IncomingCallers => new List<Caller>();
            public IReadOnlyList<Caller> OnHoldCallers => new List<Caller>();
            public Caller? CurrentScreening => null;
            public Caller? OnAirCaller => _onAirCaller;
            public bool HasIncomingCallers => false;
            public bool HasOnHoldCallers => false;
            public bool IsScreening => false;
            public bool IsOnAir => _onAirCaller != null;
            public bool CanAcceptMoreCallers => true;
            public bool CanPutOnHold => true;

            public Result<Caller> AddCaller(Caller caller) => Result<Caller>.Ok(caller);
            public Result<Caller> StartScreening(Caller caller) => Result<Caller>.Ok(caller);
            public Result<Caller> StartScreeningNext() => Result<Caller>.Fail("No callers");
            public Result<Caller> ApproveScreening() => Result<Caller>.Fail("No screening");
            public Result<Caller> RejectScreening() => Result<Caller>.Fail("No screening");
            public Result<Caller> PutOnAir() => Result<Caller>.Fail("No caller");

            public Result<Caller> EndOnAir()
            {
                EndOnAirCalled = true;
                var caller = _onAirCaller;
                _onAirCaller = null;
                return caller != null
                    ? Result<Caller>.Ok(caller)
                    : Result<Caller>.Fail("No caller on air");
            }

            public bool SetCallerState(Caller caller, CallerState newState) => true;
            public bool RemoveCaller(Caller caller) => true;
            public void ClearAll() { }
            public Caller? GetCaller(string callerId) => null;
            public void Subscribe(ICallerRepositoryObserver observer) { }
            public void Unsubscribe(ICallerRepositoryObserver observer) { }
        }

        private class NullArcRepository : IArcRepository
        {
            public Godot.Collections.Array<ConversationArc> Arcs { get; } = new();
            public void Initialize() { }
            public List<ConversationArc> FindMatchingArcs(ShowTopic topic, CallerLegitimacy legitimacy) => new();
            public ConversationArc? GetRandomArc(CallerLegitimacy legitimacy) => null;
            public ConversationArc? GetRandomArcForTopic(ShowTopic topic, CallerLegitimacy legitimacy) => null;
            public ConversationArc? GetRandomArcForDifferentTopic(ShowTopic excludeTopic, CallerLegitimacy legitimacy) => null;
            public List<ConversationArc> FindTopicSwitcherArcs(ShowTopic claimedTopic, ShowTopic actualTopic, CallerLegitimacy legitimacy) => new();
            public ConversationArc? GetRandomTopicSwitcherArc(ShowTopic claimedTopic, ShowTopic actualTopic, CallerLegitimacy legitimacy) => null;
            public void AddArc(ConversationArc arc) { }
            public void Clear() { }
        }

        private class SilentAudioService : IBroadcastAudioService
        {
            public bool IsPlaying => false;
            public bool IsAudioDisabled => true;

            public Task PlayAudioAsync(string audioPath, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task PlayAudioForDurationAsync(string audioPath, float maxDuration, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task PlayAudioForDurationAsync(string audioPath, float maxDuration, bool immediateStop, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task PlayAudioStreamAsync(AudioStream audioStream, CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task PlaySilentAudioAsync(float duration = 4.0f) => Task.CompletedTask;
            public Task PlayAudioForBroadcastItemAsync(BroadcastItem item) => Task.CompletedTask;
            public void Stop() { }
            public float GetAudioDuration(AudioStream audioStream) => 0f;
            public void SetCallerPhoneQuality(CallerPhoneQuality quality) { }
            public event System.Action<AudioCompletedEvent>? LineCompleted;
        }
    }
}
