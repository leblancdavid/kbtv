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
    }

    /// <summary>
    /// Serializable data class for caller dialogue template in JSON.
    /// Maps directly to caller template JSON files.
    /// </summary>
    [Serializable]
    public class CallerDialogueData
    {
        /// <summary>Topic ID this template is for (e.g., "UFOs", "Cryptids"). Null/empty = generic.</summary>
        public string topicId;

        /// <summary>Legitimacy level: "Fake", "Questionable", "Credible", "Compelling"</summary>
        public string legitimacy;

        /// <summary>Conversation length: "Short", "Standard", "Extended", "Long"</summary>
        public string length;

        /// <summary>Priority when multiple templates match (higher = preferred)</summary>
        public int priority;

        // Dialogue phases
        public DialogueLineData[] introLines;
        public DialogueLineData[] detailLines;
        public DialogueLineData[] defenseLines;
        public DialogueLineData[] acceptanceLines;
        public DialogueLineData[] extraDetailLines;
        public DialogueLineData[] extraDefenseLines;
        public DialogueLineData[] conclusionLines;
    }
}
