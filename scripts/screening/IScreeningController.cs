#nullable enable

using System;
using KBTV.Callers;
using KBTV.Core;

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
    /// Controller interface for the screening workflow.
    /// </summary>
    public interface IScreeningController
    {
        Caller? CurrentCaller { get; }
        bool IsActive { get; }
        ScreeningPhase Phase { get; }
        ScreeningProgress Progress { get; }

        void Start(Caller caller);
        Result<Caller> Approve();
        Result<Caller> Reject();
        void Update(float deltaTime);

        event Action<ScreeningPhase> PhaseChanged;
        event Action<ScreeningProgress> ProgressUpdated;
    }
}
