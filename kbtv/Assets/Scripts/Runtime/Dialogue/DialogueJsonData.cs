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
    /// Serializable data class for Vern's broadcast dialogue template in JSON.
    /// Maps directly to VernDialogue.json structure (broadcast flow lines only).
    /// Note: Caller conversation lines are now handled by the arc-based system.
    /// </summary>
    [Serializable]
    public class VernDialogueData
    {
        // Broadcast flow lines
        public DialogueLineData[] showOpeningLines;
        public DialogueLineData[] showClosingLines;
        public DialogueLineData[] betweenCallersLines;
        public DialogueLineData[] deadAirFillerLines;

        // Error handling dialogue
        public DialogueLineData[] droppedCallerLines;
    }
}
