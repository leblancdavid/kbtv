using System;

namespace KBTV.Dialogue
{
    /// <summary>
    /// JSON-serializable data for a conversation arc file.
    /// Used by JsonUtility to deserialize arc JSON files.
    /// </summary>
    [Serializable]
    public class ArcJsonData
    {
        public string arcId;
        public string topic;
        public string claimedTopic;  // Optional: for topic-switcher arcs (what the caller claimed)
        public string legitimacy;
        public ArcMoodVariantsData moodVariants;
    }

    /// <summary>
    /// Container for all mood variants in an arc.
    /// Unity's JsonUtility doesn't support Dictionary, so we use explicit fields.
    /// </summary>
    [Serializable]
    public class ArcMoodVariantsData
    {
        public ArcMoodVariantData Tired;
        public ArcMoodVariantData Grumpy;
        public ArcMoodVariantData Neutral;
        public ArcMoodVariantData Engaged;
        public ArcMoodVariantData Excited;
    }

    /// <summary>
    /// JSON data for a single mood variant.
    /// </summary>
    [Serializable]
    public class ArcMoodVariantData
    {
        public ArcLineData[] intro;
        public ArcLineData[] development;
        public ArcBeliefBranchData beliefBranch;
        public ArcLineData[] conclusion;
    }

    /// <summary>
    /// JSON data for belief branch paths.
    /// </summary>
    [Serializable]
    public class ArcBeliefBranchData
    {
        public ArcLineData[] Skeptical;
        public ArcLineData[] Believing;
    }

    /// <summary>
    /// JSON data for a single dialogue line.
    /// </summary>
    [Serializable]
    public class ArcLineData
    {
        public string speaker;
        public string text;
    }
}
