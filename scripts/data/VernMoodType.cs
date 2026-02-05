 namespace KBTV.Data
{
    /// <summary>
    /// Vern's current mood type, used to select dialog variants.
    /// Determined by VernStats.CalculateMoodType() based on multi-stat combinations.
    /// Priority order: Exhausted → Depressed → Angry → Frustrated → Tired → Irritated → Obsessive → Manic → Energized → Amused → Focused → Gruff → Neutral
    /// See docs/VERN_STATS.md for full documentation.
    /// </summary>
    public enum VernMoodType
    {
        /// <summary>Physical < -50 AND Emotional < -20 - Completely drained, barely coherent</summary>
        Exhausted,

        /// <summary>Emotional < -50 AND Mental < -20 - Deep sadness, hopeless, withdrawn</summary>
        Depressed,

        /// <summary>Emotional < -50 AND Physical > -20 - Hostile, confrontational, high energy rage</summary>
        Angry,

        /// <summary>Emotional -50 to -20 AND Mental -20 to +20 - Annoyed, short-tempered, distracted</summary>
        Frustrated,

        /// <summary>Physical -50 to -20 AND Emotional -20 to +20 - Low energy, dismissive, needs rest</summary>
        Tired,

        /// <summary>Mental < -20 AND Emotional -20 to +20 AND Physical -20 to +20 - Annoyed by details, mentally fatigued</summary>
        Irritated,

        /// <summary>Mental > +50 AND Emotional -20 to +50 - Hyper-focused, conspiracy-prone, intense</summary>
        Obsessive,

        /// <summary>Physical > +50 AND Emotional > +20 - Over-excited, erratic, high-energy enthusiasm</summary>
        Manic,

        /// <summary>Physical +20 to +50 AND Emotional +20 to +50 - Enthusiastic, quick-witted, engaging</summary>
        Energized,

        /// <summary>Emotional +20 to +50 AND Mental -20 to +50 - Playful, entertained, light-hearted</summary>
        Amused,

        /// <summary>Mental +20 to +50 AND Physical -20 to +50 - Analytical, detail-oriented, methodical</summary>
        Focused,

        /// <summary>Emotional -20 to 0 AND Mental +20 to +50 AND Physical -20 to +50 - Grumpy but competent, professional cynicism</summary>
        Gruff,

        /// <summary>All stats -20 to +20 - Professional, balanced, standard hosting mode</summary>
        Neutral
    }
}