using Godot;

namespace KBTV.Callers
{
    /// <summary>
    /// Enumeration definitions for caller properties and behaviors.
    /// Separated from Caller.cs for better organization and readability.
    /// </summary>

    /// <summary>
    /// How legitimate/credible the caller appears to be.
    /// </summary>
    public enum CallerLegitimacy
    {
        /// <summary>Clearly fake, prank caller, or troll</summary>
        Fake,
        /// <summary>Suspicious, hard to verify</summary>
        Questionable,
        /// <summary>Seems genuine, average caller</summary>
        Credible,
        /// <summary>Very convincing, has details/evidence</summary>
        Compelling
    }

    /// <summary>
    /// The audio quality of the caller's phone connection.
    /// Affects how they sound on air, independent of player's equipment.
    /// </summary>
    public enum CallerPhoneQuality
    {
        /// <summary>Rotary phone, bad cell signal, payphone (-2 levels)</summary>
        Terrible,
        /// <summary>Old cordless, cheap prepaid phone (-1 level)</summary>
        Poor,
        /// <summary>Standard landline or decent cell (baseline)</summary>
        Average,
        /// <summary>Modern smartphone, clear VOIP (+1 level)</summary>
        Good
    }

    /// <summary>
    /// The caller's emotional state during the call.
    /// Affects engagement level and conversation dynamics.
    /// </summary>
    public enum CallerEmotionalState
    {
        Calm,
        Anxious,
        Excited,
        Scared,
        Angry
    }

    /// <summary>
    /// Likelihood of the caller using profanity.
    /// </summary>
    public enum CallerCurseRisk
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// How convinced the caller is about their paranormal experience.
    /// </summary>
    public enum CallerBeliefLevel
    {
        Curious,
        Partial,
        Committed,
        Certain,
        Zealot
    }

    /// <summary>
    /// Quality of evidence the caller claims to have.
    /// </summary>
    public enum CallerEvidenceLevel
    {
        None,
        Low,
        Medium,
        High,
        Irrefutable
    }

    /// <summary>
    /// How well the caller can communicate their story.
    /// </summary>
    public enum CallerCoherence
    {
        Coherent,
        Questionable,
        Incoherent
    }

    /// <summary>
    /// How time-sensitive the caller's situation is.
    /// </summary>
    public enum CallerUrgency
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Weight tiers for property revelation order.
    /// Lower tiers reveal earlier, higher tiers reveal later.
    /// </summary>
    public enum RevelationTier
    {
        Tier1_Early,
        Tier2_Mid,
        Tier3_Late
    }

    /// <summary>
    /// The current state of a caller in the system.
    /// </summary>
    public enum CallerState
    {
        /// <summary>Waiting to be screened</summary>
        Incoming,
        /// <summary>Currently being screened by player</summary>
        Screening,
        /// <summary>Approved and waiting to go on air</summary>
        OnHold,
        /// <summary>Currently on air with Vern</summary>
        OnAir,
        /// <summary>Call completed</summary>
        Completed,
        /// <summary>Rejected by screener</summary>
        Rejected,
        /// <summary>Hung up or disconnected</summary>
        Disconnected
    }

    /// <summary>
    /// State of a property during screening revelation.
    /// </summary>
    public enum RevelationState
    {
        Hidden,
        Revealing,
        Revealed
    }
}