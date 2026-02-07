namespace KBTV.Data
{
    /// <summary>
    /// Types of lines Vern can say during broadcast.
    /// </summary>
    public enum VernLineType
    {
        /// <summary>
        /// When caller curses on air (requires immediate drop)
        /// </summary>
        CallerCursed,

        /// <summary>
        /// Show opening introduction
        /// </summary>
        ShowOpening,

        /// <summary>
        /// Show closing signoff
        /// </summary>
        ShowClosing,

        /// <summary>
        /// Transition between callers
        /// </summary>
        BetweenCallers,

        /// <summary>
        /// Filler when no callers are available
        /// </summary>
        DeadAirFiller,

        /// <summary>
        /// When caller disconnects unexpectedly
        /// </summary>
        DroppedCaller,

        /// <summary>
        /// Going to commercial break
        /// </summary>
        BreakTransition,

        /// <summary>
        /// Returning from commercial break
        /// </summary>
        ReturnFromBreak,

        /// <summary>
        /// Off-topic remark that costs money
        /// </summary>
        OffTopicRemark,

        /// <summary>
        /// Generic fallback line
        /// </summary>
        Fallback
    }
}