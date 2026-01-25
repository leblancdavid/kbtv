namespace KBTV.Dialogue
{
    /// <summary>
    /// Interface for broadcast coordination, used by AdManager to trigger break states.
    /// </summary>
    public interface IBroadcastCoordinator
    {
        void OnAdBreakStarted();
        void OnAdBreakEnded();
        BroadcastState CurrentState { get; }
        event System.Action? OnBreakTransitionCompleted;
    }
}
