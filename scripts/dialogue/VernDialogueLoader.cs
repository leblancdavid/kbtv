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
            var jsonPath = "res://assets/dialogue/vern/VernDialogue.json";
            var file = Godot.FileAccess.FileExists(jsonPath) ? Godot.FileAccess.Open(jsonPath, Godot.FileAccess.ModeFlags.Read) : null;
            if (file == null)
            {
                return new VernDialogueTemplate();
            }

            try
            {
                var jsonText = file.GetAsText();
                file.Close();

                var json = new Json();
                var error = json.Parse(jsonText);
                if (error != Error.Ok)
                {
                    return new VernDialogueTemplate();
                }

                var result = new VernDialogueTemplate();
                var data = json.Data.As<Godot.Collections.Dictionary>();
                if (data == null)
                {
                    return result;
                }

                result.SetShowOpeningLines(ParseDialogueArray(data, "openings"));
                result.SetIntroductionLines(ParseDialogueArray(data, "introductionLines"));
                result.SetShowClosingLines(ParseDialogueArray(data, "closings"));
                result.SetBetweenCallersLines(ParseDialogueArray(data, "betweenCallers"));
                result.SetDeadAirFillerLines(ParseDialogueArray(data, "deadAirFillers"));
                result.SetDroppedCallerLines(ParseDialogueArray(data, "droppedCallers"));
                result.SetBreakTransitionLines(ParseDialogueArray(data, "breakTransitions"));
                result.SetReturnFromBreakLines(ParseDialogueArray(data, "returnFromBreaks"));
                result.SetOffTopicRemarkLines(ParseDialogueArray(data, "offTopicRemarks"));

                return result;
            }
            catch
            {
                return new VernDialogueTemplate();
            }
        }

        private static DialogueTemplate[] ParseDialogueArray(Godot.Collections.Dictionary data, string key)
        {
            if (!data.ContainsKey(key))
            {
                return Array.Empty<DialogueTemplate>();
            }

            var array = data[key].As<Godot.Collections.Array>();
            if (array == null || array.Count == 0)
            {
                return Array.Empty<DialogueTemplate>();
            }

            return array.Select(item => {
                var itemDict = item.As<Godot.Collections.Dictionary>();
                if (itemDict == null) return null;

                var id = itemDict.ContainsKey("id") ? itemDict["id"].AsString() : "";
                var text = itemDict.ContainsKey("text") ? itemDict["text"].AsString() : "";
                var weight = itemDict.ContainsKey("weight") ? itemDict["weight"].AsSingle() : 1f;
                var mood = itemDict.ContainsKey("mood") ? itemDict["mood"].AsString() : "";

                return new DialogueTemplate(id, text, weight, mood);
            }).Where(x => x != null).ToArray()!;
        }
    }
}