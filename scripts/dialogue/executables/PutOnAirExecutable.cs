#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Audio;
using KBTV.Core;
using KBTV.Callers;
using KBTV.Dialogue;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Executable that attempts to put an OnHold caller on air.
    /// If successful, the broadcast continues with conversation.
    /// If no suitable callers available, transitions to DroppedCaller state.
    /// </summary>
    public partial class PutOnAirExecutable : BroadcastExecutable
    {
        private readonly ICallerRepository _callerRepository;
        private readonly BroadcastStateManager _stateManager;

        public PutOnAirExecutable(EventBus eventBus, ICallerRepository callerRepository, BroadcastStateManager stateManager)
            : base("put_on_air", BroadcastItemType.PutOnAir, false, 0f, eventBus, null, stateManager.SceneTree)
        {
            _callerRepository = callerRepository;
            _stateManager = stateManager;
        }

        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            Log.Debug($"PutOnAirExecutable: Starting - OnHoldCallers: {_callerRepository.OnHoldCallers.Count}, IsOnAir: {_callerRepository.IsOnAir}");
            
            var result = _callerRepository.PutOnAir();

            if (result.IsSuccess)
            {
                // Caller successfully put on air - broadcast continues normally
                Log.Debug($"PutOnAirExecutable: Successfully put caller {result.Value.Name} on air");
            }
            else
            {
                // No suitable callers - transition to dropped caller state
                Log.Debug($"PutOnAirExecutable: Failed to put caller on air - {result.ErrorMessage}. Transitioning to DroppedCaller state");
                _stateManager.SetState(AsyncBroadcastState.DroppedCaller);
            }
            
            Log.Debug($"PutOnAirExecutable: Completed - OnAirCaller: {_callerRepository.OnAirCaller?.Name ?? "null"}");
        }

        protected override BroadcastItem CreateBroadcastItem()
        {
            return new BroadcastItem(_id, _type, "Putting caller on air...", null, _duration, new { Action = "PutOnAir" });
        }
    }
}