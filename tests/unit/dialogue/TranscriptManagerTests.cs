using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Dialogue;
using KBTV.Data;

namespace KBTV.Tests.Unit.Dialogue;

public class TranscriptManagerTests : KBTVTestClass
{
    public TranscriptManagerTests(Node testScene) : base(testScene) { }

    [Test]
    public void AddEntry_MusicLine_AddsToRepository()
    {
        var repo = new TranscriptRepository();
        var manager = new TranscriptManager(repo);

        var line = BroadcastLine.Music();
        manager.AddEntry(line);

        var entries = repo.GetCurrentShowTranscript();
        AssertAreEqual(1, entries.Count);
        AssertAreEqual(Speaker.Music, entries[0].Speaker);
    }

    [Test]
    public void AddEntry_CallerLine_IncludesCallerName()
    {
        var repo = new TranscriptRepository();
        var manager = new TranscriptManager(repo);
        var caller = new Caller(
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
            CallerBeliefLevel.Curious,
            CallerEvidenceLevel.None,
            CallerCoherence.Coherent,
            CallerUrgency.Low,
            "nervous_hiker",
            null,
            null,
            null,
            "Test summary",
            30f,
            0.8f
        );

        var line = BroadcastLine.CallerDialogue("Test text", caller.Name, caller.Id, ConversationPhase.Intro);
        manager.AddEntry(line, caller);

        var entries = repo.GetCurrentShowTranscript();
        AssertAreEqual(1, entries.Count);
        AssertAreEqual("Test Caller", entries[0].SpeakerName);
    }
}