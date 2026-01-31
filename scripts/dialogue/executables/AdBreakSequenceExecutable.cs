#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using KBTV.Audio;
using KBTV.Core;
using KBTV.Managers;
using KBTV.Dialogue;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Executable for coordinating a complete ad break sequence (all 6 ads).
    /// Handles ad progression and auto-advancement through the sequence.
    /// </summary>
    public partial class AdBreakSequenceExecutable : BroadcastExecutable
    {
        private readonly EventBus _eventBus;
        private readonly ListenerManager _listenerManager;
        private readonly IBroadcastAudioService _audioService;
        private readonly SceneTree _sceneTree;
        
        private int _currentAdIndex = 0;
        private int _totalAds;
        
        public AdBreakSequenceExecutable(string id, EventBus eventBus, ListenerManager listenerManager, IBroadcastAudioService audioService, SceneTree sceneTree, int adCount)
            : base(id, BroadcastItemType.Ad, true, 0f, eventBus, audioService, sceneTree)
        {
            _eventBus = eventBus;
            _listenerManager = listenerManager;
            _audioService = audioService;
            _sceneTree = sceneTree;
            
            // Get ad count from parameter instead of hardcoding
            _totalAds = adCount;
            
            GD.Print($"AdBreakSequenceExecutable: Initialized to play {_totalAds} ads");
        }
        
        protected override async Task ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            GD.Print($"AdBreakSequenceExecutable: Starting ad break sequence - will play {_totalAds} ads");
            
            // Play each ad sequentially
            for (int i = 0; i < _totalAds; i++)
            {
                _currentAdIndex = i + 1;
                var adExecutable = AdExecutable.CreateForListenerCount($"ad_{_currentAdIndex}", _listenerManager?.CurrentListeners ?? 100, _currentAdIndex, _eventBus, _listenerManager, _audioService, _sceneTree);
                
                // Create broadcast item for this individual ad
                var adBroadcastItem = adExecutable.CreateBroadcastItemPublic();
                
                // Get audio duration for the started event
                float audioDuration = _audioService.IsAudioDisabled ? 0f : await adExecutable.GetAudioDurationAsyncPublic();
                
                // Publish individual ad started event for UI
                var startedEvent = new BroadcastItemStartedEvent(adBroadcastItem, adExecutable.Duration, audioDuration);
                _eventBus.Publish(startedEvent);
                
                GD.Print($"AdBreakSequenceExecutable: Published started event for ad {_currentAdIndex}/{_totalAds} - '{adBroadcastItem.Text}'");
                
                // Execute the ad and wait for it to complete
                await adExecutable.ExecuteAsync(cancellationToken);
                
                GD.Print($"AdBreakSequenceExecutable: Completed ad {_currentAdIndex}/{_totalAds}");
            }
        
            GD.Print($"AdBreakSequenceExecutable: All {_totalAds} ads completed, moving to next state");
            
            // Auto-advance after all ads complete
            // The BroadcastStateManager will handle state transition in UpdateStateAfterExecution
        }

        protected override BroadcastItem CreateBroadcastItem()
        {
            return new BroadcastItem(_id, _type, GetDisplayText(), null, _duration, new { Speaker = "AD" });
        }

        private string GetDisplayText()
        {
            return $"Playing {_totalAds} ads";
        }
    }
}