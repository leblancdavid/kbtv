using Chickensoft.GoDotTest;
using Godot;
using KBTV.Audio;
using KBTV.Callers;
using KBTV.Data;
using KBTV.Monitors;

namespace KBTV.Tests.Unit.Monitors
{
    public class SoundboardMonitorTests : KBTVTestClass
    {
        public SoundboardMonitorTests(Node testScene) : base(testScene) { }

        [Test]
        public void Monitor_NoDrain_WhenNoCallerOnAir()
        {
            var (monitor, vernStats, _) = CreateRig();

            monitor._Process(5f);

            AssertThat(vernStats.Emotional.Value == 0f);
            AssertThat(vernStats.Mental.Value == 0f);
        }

        [Test]
        public void Monitor_NoDrain_WhenMixPerfect()
        {
            var (monitor, vernStats, repository) = CreateRig();
            PutCallerOnAir(repository);

            monitor._Process(11f);

            AssertThat(monitor.IsCallerOnAir);
            AssertThat(vernStats.Emotional.Value == 0f);
        }

        [Test]
        public void Monitor_DrainsAfterGrace_WhenOffPerfect()
        {
            var (monitor, vernStats, repository) = CreateRig();
            PutCallerOnAir(repository);
            monitor.Driver!.State.CallerGain = 0.95f;

            monitor._Process(11f);

            AssertThat(monitor.GraceRemaining <= 0f);
            AssertThat(monitor.IsDraining);
            AssertThat(vernStats.Emotional.Value < -5f);
            AssertThat(vernStats.Mental.Value < 0f);
        }

        [Test]
        public void Monitor_Accelerates_WhileContinuouslyOffPerfect()
        {
            var (monitor, _, repository) = CreateRig();
            PutCallerOnAir(repository);
            monitor.Driver!.State.CallerGain = 0.95f;

            monitor._Process(10.5f);

            for (int i = 0; i < 5; i++)
            {
                monitor._Process(1f);
            }

            AssertThat(monitor.CurrentDrainRate >= 1f);
        }

        [Test]
        public void Monitor_Resets_OnCallerOnAirEnded()
        {
            var (monitor, _, repository) = CreateRig();
            PutCallerOnAir(repository);
            monitor.Driver!.State.CallerGain = 0.95f;

            monitor._Process(11f);

            repository.EndOnAir();

            AssertThat(!monitor.IsCallerOnAir);
            AssertThat(monitor.GraceRemaining == 0f);
            AssertThat(!monitor.IsDraining);
        }

        [Test]
        public void Monitor_StopsDraining_WhenMixBroughtToPerfect()
        {
            var (monitor, vernStats, repository) = CreateRig();
            PutCallerOnAir(repository);
            monitor.Driver!.State.CallerGain = 0.95f;

            monitor._Process(11f);
            float drained = vernStats.Emotional.Value;
            AssertThat(drained < 0f);

            monitor.Driver!.ResetToNeutral();

            monitor._Process(1f);

            AssertThat(vernStats.Emotional.Value == drained);
        }

        private (SoundboardMonitor, VernStats, CallerRepository) CreateRig()
        {
            var monitor = new SoundboardMonitor();
            var repository = new CallerRepository(new MockArcRepository());
            var vernStats = new VernStats();
            vernStats.Initialize();
            monitor.SetDriver(new SoundboardMixerDriver());
            monitor.BindRepository(repository);
            monitor.BindVernStats(vernStats);
            return (monitor, vernStats, repository);
        }

        private void PutCallerOnAir(CallerRepository repository)
        {
            var caller = CreateTestCaller();
            repository.AddCaller(caller);
            repository.StartScreening(caller);
            repository.ApproveScreening();
            repository.PutOnAir();
        }

        private Caller CreateTestCaller(string name = "Test Caller", float patience = 30f)
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
                CallerEvidenceLevel.None,
                CallerCoherence.Coherent,
                CallerUrgency.Low,
                "personality",
                null,
                null,
                null,
                "summary",
                patience,
                0.8f
            );
        }
    }
}