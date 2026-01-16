using System;

namespace KBTV.Screening
{
    /// <summary>
    /// Phase of the screening process.
    /// </summary>
    public enum ScreeningPhase
    {
        Idle,
        Gathering,
        Deciding,
        Completed
    }

    /// <summary>
    /// Progress information for the screening process.
    /// </summary>
    public record ScreeningProgress(
        int PropertiesRevealed,
        int TotalProperties,
        float PatienceRemaining,
        float MaxPatience,
        float ElapsedTime
    )
    {
        public ScreeningProgress() : this(0, 0, 0f, 0f, 0f) { }

        public float ProgressPercent => MaxPatience > 0 ? PatienceRemaining / MaxPatience : 0f;
        public float RevelationPercent => TotalProperties > 0 ? (float)PropertiesRevealed / TotalProperties : 0f;
    }

    /// <summary>
    /// Result of a screening approval action.
    /// </summary>
    public record ScreeningApprovalResult(
        bool Success,
        string? ErrorCode,
        string? ErrorMessage,
        Callers.Caller? Caller
    )
    {
        public static ScreeningApprovalResult Ok(Callers.Caller caller) =>
            new(true, null, null, caller);

        public static ScreeningApprovalResult Fail(string errorCode, string errorMessage) =>
            new(false, errorCode, errorMessage, null);
    }

    /// <summary>
    /// Controller interface for the screening workflow.
    /// </summary>
    public interface IScreeningController
    {
        Callers.Caller? CurrentCaller { get; }
        bool IsActive { get; }
        ScreeningPhase Phase { get; }
        ScreeningProgress Progress { get; }

        void Start(Callers.Caller caller);
        ScreeningApprovalResult Approve();
        ScreeningApprovalResult Reject();
        void Update(float deltaTime);

        event Action<ScreeningPhase> PhaseChanged;
        event Action<ScreeningProgress> ProgressUpdated;
    }
}
