namespace KBTV.Upgrades
{
    /// <summary>
    /// Categories of upgradeable equipment.
    /// Each type has its own upgrade track with levels 1-4.
    /// </summary>
    public enum EquipmentType
    {
        /// <summary>
        /// Phone line equipment - affects caller audio quality.
        /// Controls filters, distortion, and static on caller voices.
        /// </summary>
        PhoneLine,

        /// <summary>
        /// Broadcast equipment - affects Vern's audio quality.
        /// Controls EQ, compression, and clarity of the host voice.
        /// </summary>
        Broadcast

        // Future equipment types:
        // Transmitter,      // Listener range, signal strength
        // ScreeningTools,   // Caller info visibility
        // StudioAcoustics,  // Background noise reduction
        // RecordingGear     // Call recording capabilities
    }
}