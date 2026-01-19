namespace KBTV.Callers
{
    public enum ShowTopic
    {
        Ghosts,
        UFOs,
        Cryptids,
        Conspiracies
    }

    public static class ShowTopicExtensions
    {
        public static string ToTopicName(this ShowTopic topic)
        {
            return topic switch
            {
                ShowTopic.Ghosts => "Ghosts",
                ShowTopic.UFOs => "UFOs",
                ShowTopic.Cryptids => "Cryptids",
                ShowTopic.Conspiracies => "Conspiracies",
                _ => "Unknown"
            };
        }

        public static ShowTopic? ParseTopic(string topicString)
        {
            if (string.IsNullOrEmpty(topicString))
            {
                return null;
            }

            return topicString.ToLowerInvariant() switch
            {
                "ghosts" => ShowTopic.Ghosts,
                "ufos" => ShowTopic.UFOs,
                "cryptids" => ShowTopic.Cryptids,
                "conspiracies" or "government" => ShowTopic.Conspiracies,
                _ => null
            };
        }
    }
}
