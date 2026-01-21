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
        AssertThat(entries.Count).IsEqualTo(1);
        AssertThat(entries[0].Speaker).IsEqualTo(Speaker.Music);
    }

    [Test]
    public void AddEntry_CallerLine_IncludesCallerName()
    {
        var repo = new TranscriptRepository();
        var manager = new TranscriptManager(repo);
        var caller = new Caller("Test Caller");

        var line = BroadcastLine.CallerDialogue("Test text");
        manager.AddEntry(line, caller);

        var entries = repo.GetCurrentShowTranscript();
        AssertThat(entries.Count).IsEqualTo(1);
        AssertThat(entries[0].SpeakerName).IsEqualTo("Test Caller");
    }
}