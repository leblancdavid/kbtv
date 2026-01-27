namespace KBTV.Data
{
    /// <summary>
    /// Types of Vern dialogue lines by purpose/context.
    /// Used to classify Vern's speech without relying on string IDs.
    /// </summary>
    public enum VernLineType
    {
        /// <summary>First Vern line at show start</summary>
        ShowOpening,

        /// <summary>Vern line at show end</summary>
        ShowClosing,

        /// <summary>Transition between callers</summary>
        BetweenCallers,

        /// <summary>Filler when no callers available</summary>
        DeadAirFiller,

        /// <summary>When caller disconnects unexpectedly</summary>
        DroppedCaller,

        /// <summary>Going to commercial break</summary>
        BreakTransition,

        /// <summary>Returning from commercial break</summary>
        ReturnFromBreak,

        /// <summary>Random remarks during conversations</summary>
        OffTopicRemark,

        /// <summary>Introducing a caller</summary>
        Introduction,

        /// <summary>Generic fallback line</summary>
        Fallback
    }
}