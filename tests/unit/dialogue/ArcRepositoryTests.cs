using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue
{
    public class ArcRepositoryTests : KBTVTestClass
    {
        public ArcRepositoryTests(Node testScene) : base(testScene) { }

        private ArcRepository _repository = null!;

        [Setup]
        public void Setup()
        {
            _repository = new ArcRepository();
        }

        [Test]
        public void Constructor_CreatesEmptyRepository()
        {
            var repo = new ArcRepository();

            AssertThat(repo.Arcs.Count == 0);
        }

        [Test]
        public void AddArc_IncreasesCount()
        {
            var arc = new ConversationArc("test_arc", "Ghosts", CallerLegitimacy.Credible);
            arc.SetScreeningSummary("Test summary");
            arc.SetCallerPersonality("Average");

            _repository.AddArc(arc);

            AssertThat(_repository.Arcs.Count == 1);
        }

        [Test]
        public void AddArc_StoresCorrectArc()
        {
            var arc = new ConversationArc("test_arc", "Ghosts", CallerLegitimacy.Credible);
            arc.SetScreeningSummary("Test summary");

            _repository.AddArc(arc);

            var retrieved = _repository.Arcs[0];
            AssertThat(retrieved.ArcId == "test_arc");
            AssertThat(retrieved.Topic == "Ghosts");
        }

        [Test]
        public void Clear_RemovesAllArcs()
        {
            var arc = new ConversationArc("test_arc", "Ghosts", CallerLegitimacy.Credible);
            _repository.AddArc(arc);

            _repository.Clear();

            AssertThat(_repository.Arcs.Count == 0);
        }

        [Test]
        public void FindMatchingArcs_WithMatchingTopic_ReturnsArc()
        {
            var arc = new ConversationArc("ghost_arc", "Ghosts", CallerLegitimacy.Credible);
            _repository.AddArc(arc);

            var matches = _repository.FindMatchingArcs("Ghosts", CallerLegitimacy.Credible);

            AssertThat(matches.Count == 1);
            AssertThat(matches[0].ArcId == "ghost_arc");
        }

        [Test]
        public void FindMatchingArcs_WithNonMatchingTopic_ReturnsEmpty()
        {
            var arc = new ConversationArc("ghost_arc", "Ghosts", CallerLegitimacy.Credible);
            _repository.AddArc(arc);

            var matches = _repository.FindMatchingArcs("UFOs", CallerLegitimacy.Credible);

            AssertThat(matches.Count == 0);
        }

        [Test]
        public void FindMatchingArcs_WithMatchingLegitimacy_ReturnsArc()
        {
            var arc = new ConversationArc("fake_arc", "Ghosts", CallerLegitimacy.Fake);
            _repository.AddArc(arc);

            var matches = _repository.FindMatchingArcs("Ghosts", CallerLegitimacy.Fake);

            AssertThat(matches.Count == 1);
        }

        [Test]
        public void FindMatchingArcs_WithNonMatchingLegitimacy_ReturnsEmpty()
        {
            var arc = new ConversationArc("fake_arc", "Ghosts", CallerLegitimacy.Fake);
            _repository.AddArc(arc);

            var matches = _repository.FindMatchingArcs("Ghosts", CallerLegitimacy.Credible);

            AssertThat(matches.Count == 0);
        }

        [Test]
        public void GetRandomArc_WithMatchingArc_ReturnsArc()
        {
            var arc = new ConversationArc("ghost_arc", "Ghosts", CallerLegitimacy.Credible);
            _repository.AddArc(arc);

            var result = _repository.GetRandomArc(CallerLegitimacy.Credible);

            AssertThat(result != null);
            AssertThat(result.ArcId == "ghost_arc");
        }

        [Test]
        public void GetRandomArc_WithMatchingLegitimacy_ReturnsArc()
        {
            var arc = new ConversationArc("ghost_arc", "Ghosts", CallerLegitimacy.Credible);
            _repository.AddArc(arc);

            var result = _repository.GetRandomArc(CallerLegitimacy.Credible);

            AssertThat(result != null);
            AssertThat(result.ArcId == "ghost_arc");
        }

        [Test]
        public void GetRandomArc_WithNoMatchingLegitimacy_ReturnsNull()
        {
            var arc = new ConversationArc("ghost_arc", "Ghosts", CallerLegitimacy.Credible);
            _repository.AddArc(arc);

            var result = _repository.GetRandomArc(CallerLegitimacy.Fake);

            AssertThat(result == null);
        }
    }
}
