#nullable enable

using System.Collections.Generic;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.Callers
{
    /// <summary>
    /// Observer interface for caller repository changes.
    /// </summary>
    public interface ICallerRepositoryObserver
    {
        void OnCallerAdded(Caller caller);
        void OnCallerRemoved(Caller caller);
        void OnCallerStateChanged(Caller caller, CallerState oldState, CallerState newState);
        void OnScreeningStarted(Caller caller);
        void OnScreeningEnded(Caller caller, bool approved);
        void OnCallerOnAir(Caller caller);
        void OnCallerOnAirEnded(Caller caller);
    }

    /// <summary>
    /// Repository interface for managing caller data.
    /// Provides state-based access and observation capabilities.
    /// Replaces direct list manipulation with encapsulated operations.
    /// </summary>
    public interface ICallerRepository
    {
        // Read access by state
        IReadOnlyList<Caller> IncomingCallers { get; }
        IReadOnlyList<Caller> OnHoldCallers { get; }
        Caller? CurrentScreening { get; }
        Caller? OnAirCaller { get; }

        // State queries
        bool HasIncomingCallers { get; }
        bool HasOnHoldCallers { get; }
        bool IsScreening { get; }
        bool IsOnAir { get; }
        bool CanAcceptMoreCallers { get; }
        bool CanPutOnHold { get; }

        // Write operations with Result
        Result<Caller> AddCaller(Caller caller);
        Result<Caller> StartScreening(Caller caller);
        Result<Caller> StartScreeningNext();
        Result<Caller> ApproveScreening();
        Result<Caller> RejectScreening();
        Result<Caller> PutOnAir();
        Result<Caller> EndOnAir();

        // Direct state manipulation
        bool SetCallerState(Caller caller, CallerState newState);
        bool RemoveCaller(Caller caller);
        void ClearAll();
        Caller? GetCaller(string callerId);

        // Observation
        void Subscribe(ICallerRepositoryObserver observer);
        void Unsubscribe(ICallerRepositoryObserver observer);
    }
}
