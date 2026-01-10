using System;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Serializable data class for a single dialogue line in JSON.
    /// Used by JsonUtility to deserialize dialogue templates.
    /// </summary>
    [Serializable]
    public class DialogueLineData
    {
        public string id;
        public string text;
        public string tone;
        public float weight = 1f;
    }

    /// <summary>
    /// Serializable data class for Vern's dialogue template in JSON.
    /// Maps directly to VernDialogue.json structure.
    /// </summary>
    [Serializable]
    public class VernDialogueData
    {
        public DialogueLineData[] introductionLines;
        public DialogueLineData[] probingLines;
        public DialogueLineData[] extraProbingLines;
        public DialogueLineData[] skepticalLines;
        public DialogueLineData[] dismissiveLines;
        public DialogueLineData[] believingLines;
        public DialogueLineData[] tiredLines;
        public DialogueLineData[] annoyedLines;
        public DialogueLineData[] engagingLines;
        public DialogueLineData[] cutOffLines;
        public DialogueLineData[] signOffLines;

        // Filler dialogue for broadcast flow
        public DialogueLineData[] showOpeningLines;
        public DialogueLineData[] showClosingLines;
        public DialogueLineData[] betweenCallersLines;
        public DialogueLineData[] deadAirFillerLines;

        // Error handling dialogue
        public DialogueLineData[] droppedCallerLines;
    }
}
