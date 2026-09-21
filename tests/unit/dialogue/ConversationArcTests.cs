using Chickensoft.GoDotTest;
using Godot;
using KBTV.Callers;
using KBTV.Dialogue;

namespace KBTV.Tests.Unit.Dialogue
{
    public class ConversationArcTests : KBTVTestClass
    {
        public ConversationArcTests(Node testScene) : base(testScene) { }

        [Test]
        public void GetTopicFolder_MapsLineIdPrefixToGeneratorFolder()
        {
            AssertAreEqual("UFOs", ArcAudioTopics.GetTopicFolder("ufos_fake_topic_switch_ghost_vern_neutral_1", "Ghosts"));
            AssertAreEqual("Cryptids", ArcAudioTopics.GetTopicFolder("cryptid_credible_claims_ufos_vern_neutral_1", "UFOs"));
            AssertAreEqual("Ghosts", ArcAudioTopics.GetTopicFolder("ghosts_credible_old_house_vern_neutral_1", "UFOs"));
            AssertAreEqual("Conspiracies", ArcAudioTopics.GetTopicFolder("conspiracies_fake_tinfoil_vern_neutral_1", "UFOs"));
            AssertAreEqual("Ghosts", ArcAudioTopics.GetTopicFolder("", "Ghosts"));
            AssertAreEqual("Ghosts", ArcAudioTopics.GetTopicFolder("mystery_line_1", "Ghosts"));
        }

        [Test]
        public void AudioTopicName_NonTopicSwitcher_MatchesTopic()
        {
            var arc = new ConversationArc("ghost_arc", ShowTopic.Ghosts, CallerLegitimacy.Credible);
            AssertAreEqual("Ghosts", arc.AudioTopicName);
        }

        [Test]
        public void ParsedArcs_AudioFolderMatchesGeneratedFileLayout()
        {
            // Every shipped arc's first Vern line must have audio at
            // ConversationArcs/{line-id-prefix}/{arcId}/{lineId}.mp3 - the exact path
            // DialogueExecutable builds. Guards against silent 4-second-gap playback.
            int checkedArcs = 0;
            var report = new System.Text.StringBuilder();
            CheckArcDir("res://assets/dialogue/arcs", report, ref checkedArcs);
            GD.Print($"ConversationArcTests: verified {checkedArcs} arcs have audio");
            if (report.Length > 0)
            {
                RecordMissing(report.ToString());
            }
        }

        private void CheckArcDir(string dir, System.Text.StringBuilder report, ref int checkedArcs)
        {
            using var access = DirAccess.Open(dir);
            if (access == null) return;

            foreach (var entry in access.GetFiles())
            {
                if (!entry.EndsWith(".json")) continue;
                CheckArcFile(dir + "/" + entry, report, ref checkedArcs);
            }

            foreach (var sub in access.GetDirectories())
            {
                CheckArcDir(dir + "/" + sub, report, ref checkedArcs);
            }
        }

        private void CheckArcFile(string path, System.Text.StringBuilder report, ref int checkedArcs)
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null) return;
            var arc = ArcJsonParser.Parse(file.GetAsText());
            if (arc == null || !arc.HasDialogue()) return;

            string? firstVern = null;
            string? firstCaller = null;
            foreach (var line in arc.Dialogue)
            {
                if (string.IsNullOrEmpty(line.AudioId)) continue;
                if (line.Speaker == Speaker.Vern && firstVern == null) firstVern = line.AudioId;
                if (line.Speaker == Speaker.Caller && firstCaller == null) firstCaller = line.AudioId;
            }

            checkedArcs++;
            CheckFirstLine(report, arc, "Vern/ConversationArcs", firstVern);
            CheckFirstLine(report, arc, "Callers", firstCaller);
        }

        private void CheckFirstLine(System.Text.StringBuilder report, ConversationArc arc, string baseDir, string? lineId)
        {
            if (lineId == null) return;
            var topic = ArcAudioTopics.GetTopicFolder(lineId, arc.TopicName);
            var path = $"res://assets/audio/voice/{baseDir}/{topic}/{arc.ArcId}/{lineId}.mp3";
            if (!FileAccess.FileExists(path))
            {
                report.AppendLine($"Missing audio for {arc.ArcId}: {path}");
            }
        }

        [Test]
        public void TopicSwitcherArcs_ResolveToTheirActualAudioFolder()
        {
            AssertAreEqual("UFOs", LoadArc("res://assets/dialogue/arcs/UFOs/topic_switch_ghost.json").AudioTopicName);
            AssertAreEqual("UFOs", LoadArc("res://assets/dialogue/arcs/UFOs/topic_switch_cryptid.json").AudioTopicName);
            // claims_ufos audio was generated under the line-id prefix folder (Cryptids),
            // even though its JSON topic claims differ - the line ids are the source of truth.
            AssertAreEqual("Cryptids", LoadArc("res://assets/dialogue/arcs/Cryptids/claims_ufos.json").AudioTopicName);
        }

        private static ConversationArc LoadArc(string path)
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            return ArcJsonParser.Parse(file!.GetAsText())!;
        }

        private void RecordMissing(string message) =>
            AssertThat(false, $"Arc audio integrity check failed:\n{message}");
    }
}
