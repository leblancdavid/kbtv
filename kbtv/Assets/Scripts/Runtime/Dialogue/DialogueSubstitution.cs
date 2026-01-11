using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Utility class for applying placeholder substitutions to dialogue text.
    /// Supports {callerName}, {callerLocation}, {topic} placeholders.
    /// </summary>
    public static class DialogueSubstitution
    {
        /// <summary>
        /// Apply all placeholder substitutions to the given text.
        /// </summary>
        /// <param name="text">The text with placeholders</param>
        /// <param name="caller">The caller to get name/location from</param>
        /// <param name="topicName">The current topic display name</param>
        /// <returns>Text with all placeholders replaced</returns>
        public static string Apply(string text, Caller caller, string topicName)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string result = text;

            // Caller placeholders
            if (caller != null)
            {
                result = result.Replace("{callerName}", caller.Name ?? "caller");
                result = result.Replace("{callerLocation}", caller.Location ?? "out there");
                
                // Legacy placeholder support
                result = result.Replace("{location}", caller.Location ?? "out there");
            }
            else
            {
                result = result.Replace("{callerName}", "caller");
                result = result.Replace("{callerLocation}", "out there");
                result = result.Replace("{location}", "out there");
            }

            // Topic placeholder
            result = result.Replace("{topic}", topicName ?? "the paranormal");

            return result;
        }

        /// <summary>
        /// Apply substitutions to an ArcDialogueLine and convert to DialogueLine.
        /// </summary>
        public static DialogueLine ApplyToArcLine(
            ArcDialogueLine arcLine, 
            Caller caller, 
            string topicName,
            ConversationPhase phase,
            DialogueTone tone = DialogueTone.Neutral)
        {
            string substitutedText = Apply(arcLine.Text, caller, topicName);
            return new DialogueLine(arcLine.Speaker, substitutedText, tone, phase, 0f, arcLine.ArcLineIndex);
        }
    }
}
