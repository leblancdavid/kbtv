using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Chickensoft.GoDotTest;
using Godot;

namespace KBTV.Tests.Unit.Dialogue
{
    /// <summary>
    /// Validates every arc JSON against the schema rules in
    /// docs/ui/CONVERSATION_ARC_SCHEMA.md so authored content stays
    /// compatible with the runtime audio lookup and
    /// Tools/AudioGeneration/generate_arc_audio.py.
    /// </summary>
    public class ArcSchemaValidationTests : KBTVTestClass
    {
        public ArcSchemaValidationTests(Node testScene) : base(testScene) { }

        protected override bool FailOnRecordedFailures => true;

        private static readonly string[] ValidMoods =
        {
            "neutral", "tired", "energized", "irritated", "gruff", "amused", "focused",
            "exhausted", "depressed", "angry", "frustrated", "obsessive", "manic"
        };

        private static readonly string[] TopicTokens =
        {
            "ufo", "ufos", "ghosts", "cryptid", "cryptids", "conspiracies"
        };

        private static readonly string[] LegitimacyTokens =
        {
            "fake", "questionable", "credible", "compelling"
        };

        [Test]
        public void AllArcs_ConformToSchema()
        {
            var report = new StringBuilder();
            var arcIds = new HashSet<string>();
            var lineIds = new HashSet<string>();
            int arcCount = 0;

            WalkDir("res://assets/dialogue/arcs", report, arcIds, lineIds, ref arcCount);

            GD.Print($"ArcSchemaValidationTests: validated {arcCount} arcs");
            AssertThat(report.Length == 0, $"Arc schema violations:\n{report}");
        }

        private void WalkDir(string dir, StringBuilder report, HashSet<string> arcIds, HashSet<string> lineIds, ref int arcCount)
        {
            using var access = DirAccess.Open(dir);
            if (access == null) return;

            foreach (var entry in access.GetFiles())
            {
                if (!entry.EndsWith(".json")) continue;
                ValidateArc(dir + "/" + entry, report, arcIds, lineIds);
                arcCount++;
            }

            foreach (var sub in access.GetDirectories())
            {
                WalkDir(dir + "/" + sub, report, arcIds, lineIds, ref arcCount);
            }
        }

        private void ValidateArc(string path, StringBuilder report, HashSet<string> arcIds, HashSet<string> lineIds)
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                report.AppendLine($"{path}: could not open");
                return;
            }

            var json = new Json();
            if (json.Parse(file.GetAsText()) != Error.Ok)
            {
                report.AppendLine($"{path}: invalid JSON");
                return;
            }

            var data = json.Data.As<Godot.Collections.Dictionary>();
            if (data == null)
            {
                report.AppendLine($"{path}: root is not an object");
                return;
            }

            var arcId = data.GetValueOrDefault("arcId", "").AsString();
            var fileName = path.GetFile().GetBaseName();
            if (string.IsNullOrEmpty(arcId))
            {
                report.AppendLine($"{path}: missing arcId");
                return;
            }
            if (arcId != fileName)
            {
                report.AppendLine($"{path}: arcId '{arcId}' must equal filename '{fileName}'");
            }
            if (!arcIds.Add(arcId))
            {
                report.AppendLine($"{path}: duplicate arcId '{arcId}'");
            }

            var legitimacy = data.GetValueOrDefault("legitimacy", "").AsString().ToLowerInvariant();
            if (!System.Array.Exists(LegitimacyTokens, t => t == legitimacy))
            {
                report.AppendLine($"{path}: legitimacy '{legitimacy}' not in fake/questionable/credible/compelling");
            }

            if (!data.TryGetValue("arcLines", out var linesVariant)
                || linesVariant.VariantType != Variant.Type.Array)
            {
                report.AppendLine($"{path}: missing arcLines array");
                return;
            }

            var groups = linesVariant.As<Godot.Collections.Array>();
            if (groups.Count < 2)
            {
                report.AppendLine($"{path}: arcLines must contain at least 2 groups, got {groups.Count}");
                return;
            }

            string? idTopicToken = null;
            int vernTurn = 0;
            int callerTurn = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i].As<Godot.Collections.Dictionary>();
                if (group == null)
                {
                    report.AppendLine($"{path}: group {i} is not an object");
                    continue;
                }

                var speaker = group.GetValueOrDefault("speaker", "").AsString().ToLowerInvariant();
                bool expectVern = i % 2 == 0;
                bool isVern = speaker == "vern";
                if (speaker != "vern" && speaker != "caller")
                {
                    report.AppendLine($"{path}: group {i} speaker '{speaker}' invalid");
                    continue;
                }
                if (isVern != expectVern)
                {
                    report.AppendLine($"{path}: group {i} speaker must be '{(expectVern ? "vern" : "caller")}' (must alternate vern/caller/.../vern)");
                }

                if (!group.TryGetValue("lines", out var groupLinesVariant)
                    || groupLinesVariant.VariantType != Variant.Type.Array)
                {
                    report.AppendLine($"{path}: group {i} missing lines array");
                    continue;
                }

                var lines = groupLinesVariant.As<Godot.Collections.Array>();

                if (isVern)
                {
                    vernTurn++;
                    if (lines.Count != ValidMoods.Length)
                    {
                        report.AppendLine($"{path}: group {i} (vern turn {vernTurn}) must have exactly {ValidMoods.Length} mood variants, got {lines.Count}");
                    }
                }
                else
                {
                    callerTurn++;
                    if (lines.Count != 1)
                    {
                        report.AppendLine($"{path}: group {i} (caller turn {callerTurn}) must have exactly 1 line, got {lines.Count}");
                    }
                }

                var moodsSeen = new HashSet<string>();
                foreach (var lineItem in lines)
                {
                    var line = lineItem.As<Godot.Collections.Dictionary>();
                    if (line == null) continue;

                    var id = line.GetValueOrDefault("id", "").AsString();
                    var text = line.GetValueOrDefault("text", "").AsString();
                    var voiceText = line.GetValueOrDefault("voiceText", "").AsString();
                    var mood = line.GetValueOrDefault("mood", "").AsString().ToLowerInvariant();

                    if (string.IsNullOrEmpty(id))
                    {
                        report.AppendLine($"{path}: group {i} line missing id");
                        continue;
                    }
                    if (!lineIds.Add(id))
                    {
                        report.AppendLine($"{path}: duplicate line id '{id}'");
                    }
                    if (string.IsNullOrEmpty(text))
                    {
                        report.AppendLine($"{path}: {id}: missing text");
                    }
                    if (string.IsNullOrEmpty(voiceText))
                    {
                        report.AppendLine($"{path}: {id}: missing voiceText");
                    }

                    var topicToken = id.Split('_')[0].ToLowerInvariant();
                    if (!System.Array.Exists(TopicTokens, t => t == topicToken))
                    {
                        report.AppendLine($"{path}: {id}: first token '{topicToken}' must be a topic token ({string.Join("/", TopicTokens)}) - it selects the audio folder");
                        continue;
                    }
                    if (idTopicToken == null)
                    {
                        idTopicToken = topicToken;
                    }
                    else if (idTopicToken != topicToken)
                    {
                        report.AppendLine($"{path}: {id}: topic token '{topicToken}' differs from arc-wide '{idTopicToken}' - all ids must share one prefix so audio lands in one folder");
                    }

                    if (isVern)
                    {
                        if (!System.Array.Exists(ValidMoods, m => m == mood))
                        {
                            report.AppendLine($"{path}: {id}: mood '{mood}' not one of the {ValidMoods.Length} VernMoodType names");
                        }
                        else if (!moodsSeen.Add(mood))
                        {
                            report.AppendLine($"{path}: duplicate mood '{mood}' in group {i}");
                        }

                        var match = Regex.Match(id, @"^(?<topic>[a-z]+)_(?<legit>[a-z]+)_.+?_vern_(?<mood>[a-z]+)_(?<seq>\d+)$");
                        if (!match.Success)
                        {
                            report.AppendLine($"{path}: {id}: must match {{topic}}_{{legitimacy}}_{{descriptor}}_vern_{{mood}}_{{sequence}}");
                            continue;
                        }
                        if (match.Groups["legit"].Value != legitimacy)
                        {
                            report.AppendLine($"{path}: {id}: legitimacy token '{match.Groups["legit"].Value}' != arc legitimacy '{legitimacy}'");
                        }
                        if (match.Groups["mood"].Value != mood)
                        {
                            report.AppendLine($"{path}: {id}: mood token in id != mood field '{mood}'");
                        }
                        if (int.Parse(match.Groups["seq"].Value) != vernTurn)
                        {
                            report.AppendLine($"{path}: {id}: sequence must be {vernTurn} (vern turn ordinal)");
                        }
                    }
                    else
                    {
                        var match = Regex.Match(id, @"^(?<topic>[a-z]+)_(?<legit>[a-z]+)_.+?_caller_(?<seq>\d+)$");
                        if (!match.Success)
                        {
                            report.AppendLine($"{path}: {id}: must match {{topic}}_{{legitimacy}}_{{descriptor}}_caller_{{sequence}}");
                            continue;
                        }
                        if (match.Groups["legit"].Value != legitimacy)
                        {
                            report.AppendLine($"{path}: {id}: legitimacy token '{match.Groups["legit"].Value}' != arc legitimacy '{legitimacy}'");
                        }
                        if (int.Parse(match.Groups["seq"].Value) != callerTurn)
                        {
                            report.AppendLine($"{path}: {id}: sequence must be {callerTurn} (caller turn ordinal)");
                        }
                    }
                }
            }
        }
    }
}
