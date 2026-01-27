#nullable enable

using System.Linq;
using Godot;
using KBTV.Core;
using KBTV.Callers;

namespace KBTV.Monitors
{
    /// <summary>
    /// Monitors caller state and updates time-based properties.
    /// Handles wait time accumulation, screening patience drain, and
    /// disconnection when patience runs out.
    ///
    /// State Updates:
    /// - Incoming callers: Accumulate wait time
    /// - Screening caller: Drain screening patience at 50% rate
    /// - OnHold callers: Accumulate wait time at 50% rate
    /// - OnAir callers: No wait time accumulation
    ///
    /// Side Effects:
    /// - Triggers OnDisconnected event when patience runs out
    /// </summary>
    public partial class CallerMonitor : DomainMonitor
    {
    protected override void OnUpdate(float deltaTime)
    {
        var incoming = _repository!.IncomingCallers;
        var onHold = _repository.OnHoldCallers;
        var screening = _repository.CurrentScreening;

        if (incoming.Count > 0)
        {
            foreach (var caller in incoming.ToList())
            {
                caller.UpdateWaitTime(deltaTime);
            }
        }

        if (onHold.Count > 0)
        {
            foreach (var caller in onHold.ToList())
            {
                caller.UpdateWaitTime(deltaTime);
            }
        }

        screening?.UpdateWaitTime(deltaTime);
    }
    }
}
