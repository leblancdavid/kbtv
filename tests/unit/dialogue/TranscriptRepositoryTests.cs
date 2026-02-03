using System;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue
{
    public class TranscriptRepositoryTests : KBTVTestClass
    {
        public TranscriptRepositoryTests(Node testScene) : base(testScene) { }

        [Test]
        public void AddEntry_IncreasesEntryCount()
        {
            var repo = new TranscriptRepository();

            var entry = new TranscriptEntry(
                Speaker.Vern,
                "Test line",
                ConversationPhase.Intro,
                "test_arc",
                "Vern"
            );

            repo.AddEntry(entry);

            AssertThat(repo.EntryCount == 1);
        }

        [Test]
        public void GetCurrentShowTranscript_ReturnsAllEntries()
        {
            var repo = new TranscriptRepository();

            var entry1 = TranscriptEntry.CreateVernLine("Line 1", ConversationPhase.Intro);
            var entry2 = TranscriptEntry.CreateVernLine("Line 2", ConversationPhase.Probe);

            repo.AddEntry(entry1);
            repo.AddEntry(entry2);

            var transcript = repo.GetCurrentShowTranscript();
            AssertThat(transcript.Count == 2);
        }

        [Test]
        public void GetLatestEntry_ReturnsLastAdded()
        {
            var repo = new TranscriptRepository();

            var entry1 = new TranscriptEntry(Speaker.Vern, "First", ConversationPhase.Intro, null, "Vern");
            var entry2 = new TranscriptEntry(Speaker.Vern, "Second", ConversationPhase.Probe, null, "Vern");

            repo.AddEntry(entry1);
            repo.AddEntry(entry2);

            var latest = repo.GetLatestEntry();
            AssertThat(latest != null);
            if (latest != null)
            {
                AssertThat(latest.Text == "Second");
            }
        }

        [Test]
        public void GetEntriesForArc_ReturnsMatchingEntries()
        {
            var repo = new TranscriptRepository();

            var entry1 = TranscriptEntry.CreateVernLine("Arc1 line", ConversationPhase.Intro, "arc_001");
            var entry2 = TranscriptEntry.CreateVernLine("Arc2 line", ConversationPhase.Probe, "arc_002");
            var entry3 = TranscriptEntry.CreateVernLine("Arc1 again", ConversationPhase.Challenge, "arc_001");

            repo.AddEntry(entry1);
            repo.AddEntry(entry2);
            repo.AddEntry(entry3);

            var arc1Entries = repo.GetEntriesForArc("arc_001");
            AssertThat(arc1Entries.Count == 2);

            var arc2Entries = repo.GetEntriesForArc("arc_002");
            AssertThat(arc2Entries.Count == 1);
        }

        [Test]
        public void GetEntriesForArc_WithNoMatches_ReturnsEmpty()
        {
            var repo = new TranscriptRepository();

            var entry = TranscriptEntry.CreateVernLine("Test", ConversationPhase.Intro, "arc_001");
            repo.AddEntry(entry);

            var entries = repo.GetEntriesForArc("nonexistent");
            AssertThat(entries.Count == 0);
        }

        [Test]
        public void GetEntriesForArc_WithEmptyArcId_ReturnsEmpty()
        {
            var repo = new TranscriptRepository();

            var entry = TranscriptEntry.CreateVernLine("Test", ConversationPhase.Intro, "arc_001");
            repo.AddEntry(entry);

            var entries = repo.GetEntriesForArc("");
            AssertThat(entries.Count == 0);
        }

        [Test]
        public void ClearCurrentShow_ResetsEntries()
        {
            var repo = new TranscriptRepository();

            var entry = TranscriptEntry.CreateVernLine("Test", ConversationPhase.Intro);
            repo.AddEntry(entry);

            repo.ClearCurrentShow();

            AssertThat(repo.EntryCount == 0);
            AssertThat(repo.GetLatestEntry() == null);
        }

        [Test]
        public void AddNullEntry_DoesNotIncreaseCount()
        {
            var repo = new TranscriptRepository();

            repo.AddEntry(null!);

            AssertThat(repo.EntryCount == 0);
        }

        [Test]
        public void TranscriptEntry_GetDisplayText_FormatsCorrectly()
        {
            var entry = new TranscriptEntry(
                Speaker.Vern,
                "Hello world",
                ConversationPhase.Intro,
                "test_arc",
                "Vern"
            );

            var display = entry.GetDisplayText();
            AssertThat(display.Contains("Vern:"));
            AssertThat(display.Contains("Hello world"));
        }

        [Test]
        public void TranscriptEntry_CreateCallerLine_SetsCorrectSpeaker()
        {
            var caller = CreateTestCaller("TestCaller", "caller_001");
            var entry = TranscriptEntry.CreateCallerLine(
                caller,
                "Test message",
                ConversationPhase.Probe,
                "arc_001"
            );

            AssertThat(entry.Speaker == Speaker.Caller);
            AssertThat(entry.SpeakerName == "TestCaller");
            AssertThat(entry.Text == "Test message");
        }

        private static Caller CreateTestCaller(string name, string id)
        {
            return new Caller(
                name,
                "555-0000",
                "Test Location",
                "UFOs",
                "UFOs",
                "Test call reason",
                CallerLegitimacy.Questionable,
                CallerPhoneQuality.Average,
                CallerEmotionalState.Calm,
                CallerCurseRisk.Low,
                CallerBeliefLevel.Curious,
                CallerEvidenceLevel.None,
                CallerCoherence.Coherent,
                CallerUrgency.Low,
                "nervous_hesitant",
                null,
                null,
                null,
                "",
                60f,
                0.5f
            );
        }
    }
}
