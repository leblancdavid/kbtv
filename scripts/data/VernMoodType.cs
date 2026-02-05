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
        /// <summary>Physical < -66 AND Emotional < -33 - Completely drained, barely coherent</summary>
        Exhausted,

        /// <summary>Emotional < -66 AND Mental < -33 - Deep sadness, hopeless, withdrawn</summary>
        Depressed,

        /// <summary>Emotional < -66 AND Physical > -33 - Hostile, confrontational, high energy rage</summary>
        Angry,

        /// <summary>Emotional -66 to -33 AND Mental -33 to +33 - Annoyed, short-tempered, distracted</summary>
        Frustrated,

        /// <summary>Physical -66 to -33 AND Emotional -33 to +33 - Low energy, dismissive, needs rest</summary>
        Tired,

        /// <summary>Mental < -33 AND Emotional -33 to +33 AND Physical -33 to +33 - Annoyed by details, mentally fatigued</summary>
        Irritated,

        /// <summary>Mental > +66 AND Emotional -33 to +66 - Hyper-focused, conspiracy-prone, intense</summary>
        Obsessive,

        /// <summary>Physical > +66 AND Emotional > +33 - Over-excited, erratic, high-energy enthusiasm</summary>
        Manic,

        /// <summary>Physical +33 to +66 AND Emotional +33 to +66 - Enthusiastic, quick-witted, engaging</summary>
        Energized,

        /// <summary>Emotional +33 to +66 AND Mental -33 to +66 - Playful, entertained, light-hearted</summary>
        Amused,

        /// <summary>Mental +33 to +66 AND Physical -33 to +66 - Analytical, detail-oriented, methodical</summary>
        Focused,

        /// <summary>Emotional -33 to 0 AND Mental +33 to +66 AND Physical -33 to +66 - Grumpy but competent, professional cynicism</summary>
        Gruff,

        /// <summary>All stats -33 to +33 - Professional, balanced, standard hosting mode</summary>
        Neutral
    }
}