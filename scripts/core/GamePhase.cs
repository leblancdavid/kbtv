namespace KBTV.Core
{
    /// <summary>
    /// The phases of a nightly broadcast.
    /// </summary>
    public enum GamePhase
    {
        /// <summary>
        /// Loading/initialization phase: services and UI setup.
        /// </summary>
        Loading,

        /// <summary>
        /// Pre-show preparation: choose topics, set caller rules, purchase supplies.
        /// </summary>
        PreShow,

        /// <summary>
        /// Live broadcast: screen callers, manage ads, fulfill Vern's needs.
        /// </summary>
        LiveShow,

        /// <summary>
        /// Post-show wrap-up: calculate income, purchase upgrades, hire staff.
        /// </summary>
        PostShow
    }
}