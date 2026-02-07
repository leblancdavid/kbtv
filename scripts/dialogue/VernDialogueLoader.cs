using System;
using System.Linq;
using Godot;

namespace KBTV.Dialogue
{
    public partial class VernDialogueLoader : Node
    {
        private VernDialogueTemplate _vernDialogue;

        public VernDialogueTemplate VernDialogue => _vernDialogue;

        public void LoadDialogue()
        {
            _vernDialogue = LoadVernDialogue();
        }

        private static VernDialogueTemplate LoadVernDialogue()
        {
            var result = new VernDialogueTemplate();

            // Load each line type from separate JSON files
            result.SetShowOpeningLines(LoadDialogueFile("res://assets/dialogue/vern/openings.json"));
            result.SetIntroductionLines(LoadDialogueFile("res://assets/dialogue/vern/introduction-lines.json")); // Note: This file doesn't exist yet, will return empty
            result.SetShowClosingLines(LoadDialogueFile("res://assets/dialogue/vern/closings.json"));
            result.SetBetweenCallersLines(LoadDialogueFile("res://assets/dialogue/vern/between-callers.json"));
            result.SetDeadAirFillerLines(LoadDialogueFile("res://assets/dialogue/vern/dead-air-fillers.json"));
            result.SetDroppedCallerLines(LoadDialogueFile("res://assets/dialogue/vern/dropped-callers.json"));
            result.SetBreakTransitionLines(LoadDialogueFile("res://assets/dialogue/vern/break-transitions.json"));
            result.SetReturnFromBreakLines(LoadDialogueFile("res://assets/dialogue/vern/return-from-breaks.json"));
            result.SetOffTopicRemarkLines(LoadDialogueFile("res://assets/dialogue/vern/off-topic-remarks.json"));
            result.SetCallerCursedLines(LoadDialogueFile("res://assets/dialogue/vern/caller-cursed.json"));

            return result;
        }

        private static DialogueTemplate[] LoadDialogueFile(string jsonPath)
        {
            var file = Godot.FileAccess.FileExists(jsonPath) ? Godot.FileAccess.Open(jsonPath, Godot.FileAccess.ModeFlags.Read) : null;
            if (file == null)
            {
                return Array.Empty<DialogueTemplate>();
            }

            try
            {
                var jsonText = file.GetAsText();
                file.Close();

                var json = new Json();
                var error = json.Parse(jsonText);
                if (error != Error.Ok)
                {
                    return Array.Empty<DialogueTemplate>();
                }

                var data = json.Data.As<Godot.Collections.Dictionary>();
                if (data == null || !data.ContainsKey("lines"))
                {
                    return Array.Empty<DialogueTemplate>();
                }

                var linesArray = data["lines"].As<Godot.Collections.Array>();
                if (linesArray == null || linesArray.Count == 0)
                {
                    return Array.Empty<DialogueTemplate>();
                }

                return linesArray.Select(item => {
                    var itemDict = item.As<Godot.Collections.Dictionary>();
                    if (itemDict == null) return null;

                    var id = itemDict.ContainsKey("id") ? itemDict["id"].AsString() : "";
                    var text = itemDict.ContainsKey("text") ? itemDict["text"].AsString() : "";
                    var weight = itemDict.ContainsKey("weight") ? itemDict["weight"].AsSingle() : 1f;
                    var mood = itemDict.ContainsKey("mood") ? itemDict["mood"].AsString() : "";
                    var topic = itemDict.ContainsKey("topic") ? itemDict["topic"].AsString() : "";

                    return new DialogueTemplate(id, text, weight, mood, topic);
                }).Where(x => x != null).ToArray()!;
            }
            catch
            {
                return Array.Empty<DialogueTemplate>();
            }
        }
    }
}