#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Data;

namespace KBTV.Dialogue
{
    /// <summary>
    /// State machine for managing broadcast flow and determining next broadcast items.
    /// Replaces the complex BroadcastCoordinator + BroadcastStateManager logic.
    /// </summary>
    public class BroadcastStateMachine
    {
        private readonly ICallerRepository _repository;
        private readonly BroadcastItemRegistry _itemRegistry;
        private BroadcastState _currentState = BroadcastState.Idle;

        // Conversation state tracking
        private int _currentConversationLineIndex = -1; // -1 = no active conversation
        private string? _currentConversationArcId = null;

        // Ad break interruption state
        private bool _isInBreakGracePeriod = false;

        public BroadcastState CurrentState => _currentState;

        public BroadcastStateMachine(ICallerRepository repository, BroadcastItemRegistry itemRegistry)
        {
            _repository = repository;
            _itemRegistry = itemRegistry;
        }

        /// <summary>
        /// Start the broadcast show.
        /// </summary>
        public BroadcastItem? StartShow()
        {
            _currentState = BroadcastState.IntroMusic;
            return GetNextItem();
        }

        public BroadcastItem? HandleEvent(BroadcastEvent @event)
        {
            return HandleItemCompleted(@event.ItemId);
        }

        public BroadcastItem? HandleInterruption(BroadcastInterruptionEvent @event)
        {
            GD.Print($"BroadcastStateManager: Handling interruption - {@event.Reason}");

            switch (@event.Reason)
            {
                case BroadcastInterruptionReason.BreakStarting:
                    // Enter break grace period - next conversation completion will trigger transition
                    _isInBreakGracePeriod = true;
                    GD.Print("BroadcastStateManager: Entered break grace period");
                    return null; // Don't interrupt current item, wait for natural completion

                case BroadcastInterruptionReason.BreakImminent:
                    // Immediate hard interrupt - return transition item now
                    _isInBreakGracePeriod = false; // Reset for next break
                    GD.Print("BroadcastStateManager: Break imminent - immediate transition");
                    return _itemRegistry.GetVernItem("break_transition", GetVernCurrentMood());

                case BroadcastInterruptionReason.BreakEnding:
                    // Break is ending, return to normal conversation flow
                    _isInBreakGracePeriod = false;
                    GD.Print("BroadcastStateManager: Exited break grace period");
                    return null;

                default:
                    GD.Print($"BroadcastStateManager: Unhandled interruption reason {@event.Reason}");
                    return null;
            }
        }

        private BroadcastItem? GetNextItem()
        {
            var currentMood = GetVernCurrentMood();

            switch (_currentState)
            {
                case BroadcastState.Idle:
                    return null;
                case BroadcastState.IntroMusic:
                    return _itemRegistry.GetItem("music_intro");
                case BroadcastState.ShowOpening:
                    return _itemRegistry.GetVernItem("opening", currentMood);
                case BroadcastState.Conversation:
                    return GetNextConversationItem();
                case BroadcastState.BetweenCallers:
                    return _itemRegistry.GetVernItem("between_callers", currentMood);
                case BroadcastState.DeadAirFiller:
                    return _itemRegistry.GetVernItem("dead_air_filler", currentMood);
                case BroadcastState.Break:
                    return _itemRegistry.GetItem("ad_break");
                case BroadcastState.ReturnFromBreak:
                    return _itemRegistry.GetVernItem("return_from_break", currentMood);
                case BroadcastState.ShowEnding:
                    return _itemRegistry.GetItem("music_outro");
                default:
                    return null;
            }
        }

        private VernMoodType GetVernCurrentMood()
        {
            // Get Vern's current mood from game state
            var vernStats = ServiceRegistry.Instance?.GameStateManager?.VernStats;
            if (vernStats != null)
            {
                return vernStats.CurrentMoodType;
            }
            return VernMoodType.Neutral; // Default fallback
        }

        private string GetVernCurrentMoodString()
        {
            return GetVernCurrentMood().ToString().ToLowerInvariant();
        }

        private string? ResolveAudioPath(string audioId, ConversationArc arc)
        {
            if (string.IsNullOrEmpty(audioId))
            {
                return null;
            }

            // Vern audio: res://assets/audio/voice/Vern/ConversationArcs/{topic}/{arcId}/{audioId}.mp3
            string arcTopic = arc.Topic.ToString();
            string audioPath = $"res://assets/audio/voice/Vern/ConversationArcs/{arcTopic}/{arc.ArcId}/{audioId}.mp3";

            if (GD.Load<AudioStream>(audioPath) != null)
            {
                GD.Print($"BroadcastStateManager: Found Vern audio for {audioId} at {audioPath}");
                return audioPath;
            }

            // No audio found - will use timer fallback
            GD.PushWarning($"BroadcastStateManager: No Vern audio found for '{audioId}' at {audioPath} - using timer fallback");
            return null;
        }

        private bool CanPutCallerOnAir()
        {
            // Check if we're in break window - don't put new callers on air during break window
            var adManager = ServiceRegistry.Instance?.AdManager;
            if (adManager != null && adManager.IsInBreakWindow)
            {
                GD.Print("BroadcastStateManager: Cannot put caller on air - in break window");
                return false;
            }
            return true;
        }



        private BroadcastItem? GetNextConversationItem()
        {
            var caller = _repository.OnAirCaller;
            if (caller == null || caller.ActualArc == null)
            {
                GD.PrintErr("BroadcastStateMachine.GetNextConversationItem: No on-air caller or arc");
                return null;
            }

            var arc = caller.ActualArc;

            // Check if we've reached the end of the conversation
            if (_currentConversationLineIndex >= arc.Dialogue.Count)
            {
                GD.Print($"BroadcastStateMachine: Conversation ended for arc {arc.ArcId}");
                _currentConversationLineIndex = -1;
                _currentConversationArcId = null;
                return null; // Conversation ended, let state machine handle next transition
            }

            // For the new schema, we need to get the line data from the arc's lines array
            // The arc.Dialogue contains ArcDialogueLine objects created from the schema
            var arcLine = arc.Dialogue[_currentConversationLineIndex];

            // Get display text and audio id, selecting mood variant for Vern lines
            string displayText = arcLine.Text;
            string? audioId = arcLine.AudioId;

            // For Vern lines with mood variants, select based on current mood
            if (arcLine.Speaker == Speaker.Vern && arcLine.AudioIds.Count > 0)
            {
                var currentMood = GetVernCurrentMoodString();

                // Try to get mood-specific text and audio
                if (arcLine.AudioIds.TryGetValue(currentMood, out var moodAudioId))
                {
                    audioId = moodAudioId;
                    if (arcLine.TextVariants.TryGetValue(currentMood, out var moodText))
                    {
                        displayText = moodText;
                    }
                }
                else
                {
                    // Fallback to neutral or first available
                    var fallbackMoods = new[] { "neutral", "tired", "energized", "irritated", "gruff", "amused", "focused" };
                    foreach (var mood in fallbackMoods)
                    {
                        if (arcLine.AudioIds.TryGetValue(mood, out var fallbackAudioId))
                        {
                            audioId = fallbackAudioId;
                            if (arcLine.TextVariants.TryGetValue(mood, out var fallbackText))
                            {
                                displayText = fallbackText;
                            }
                            break;
                        }
                    }
                }
            }

            // Create BroadcastLine from arc data
            BroadcastLine broadcastLine;
            if (arcLine.Speaker == Speaker.Vern)
            {
                // Vern line with arc information for audio lookup
                broadcastLine = BroadcastLine.VernDialogue(displayText, ConversationPhase.Probe,
                    arc.ArcId, arcLine.ArcLineIndex, audioId ?? $"vern_fallback_{_currentConversationLineIndex}");
            }
            else
            {
                // Caller line
                broadcastLine = BroadcastLine.CallerDialogue(displayText, caller.Name, caller.Id.ToString(),
                    ConversationPhase.Probe, arc.ArcId, arc.CallerGender.ToLower(), arcLine.ArcLineIndex);
            }

            // Create BroadcastItem
            var itemType = arcLine.Speaker == Speaker.Vern ? BroadcastItemType.VernLine : BroadcastItemType.CallerLine;

            // Resolve audio path using the audioId
            string? audioPath = null;
            if (!string.IsNullOrEmpty(audioId))
            {
                if (arcLine.Speaker == Speaker.Vern)
                {
                    // Vern audio path
                    audioPath = ResolveAudioPath(audioId, arc);
                }
                else
                {
                    // Caller audio path: res://assets/audio/voice/Callers/{topic}/{arcId}/{line.Id}.mp3
                    string callerTopic = arc.Topic.ToString();
                    string callerPath = $"res://assets/audio/voice/Callers/{callerTopic}/{arc.ArcId}/{audioId}.mp3";

                    if (GD.Load<AudioStream>(callerPath) != null)
                    {
                        GD.Print($"BroadcastStateManager: Found Caller audio for {audioId} at {callerPath}");
                        audioPath = callerPath;
                    }
                    else
                    {
                        GD.PushWarning($"BroadcastStateManager: No Caller audio found for '{audioId}' at {callerPath} - using timer fallback");
                    }
                }
            }

            var item = new BroadcastItem(
                id: $"conversation_{arc.ArcId}_{_currentConversationLineIndex}",
                type: itemType,
                text: displayText,
                audioPath: audioPath, // May be null - will use timer fallback
                duration: 4.0f, // Default duration
                metadata: broadcastLine // Store the BroadcastLine for AudioDialoguePlayer
            );

            // Advance to next line
            _currentConversationLineIndex++;

            GD.Print($"BroadcastStateMachine: Returning conversation item - {item.Id} ({item.Type}) - \"{displayText}\"");
            return item;
        }

        private BroadcastItem? HandleItemCompleted(string itemId)
        {
            // Update state based on completed item
            switch (_currentState)
            {
                case BroadcastState.IntroMusic:
                    if (itemId == "music_intro")
                    {
                        _currentState = BroadcastState.ShowOpening;
                        return GetNextItem();
                    }
                    break;

                case BroadcastState.ShowOpening:
                    // After show opening completes, check for callers
                    if (_repository.HasOnHoldCallers)
                    {
                        // Check if we can put caller on air (not in break window)
                        if (CanPutCallerOnAir())
                        {
                            // Put next caller on air automatically
                            var putOnAirResult = _repository.PutOnAir();
                            if (putOnAirResult.IsSuccess)
                            {
                                // Initialize conversation state
                                _currentConversationLineIndex = 0;
                                _currentConversationArcId = putOnAirResult.Value.ActualArc?.ArcId;

                                _currentState = BroadcastState.Conversation;
                                return GetNextConversationItem();
                            }
                            else
                            {
                                GD.PrintErr($"Failed to put caller on air: {putOnAirResult.ErrorMessage}");
                                // Fallback to dead air filler
                                _currentState = BroadcastState.DeadAirFiller;
                                return _itemRegistry.GetNextDeadAirFiller();
                            }
                        }
                        else
                        {
                            // In break window, go to dead air filler instead
                            _currentState = BroadcastState.DeadAirFiller;
                            return _itemRegistry.GetNextDeadAirFiller();
                        }
                    }
                    else
                    {
                        // No callers waiting, start dead air filler
                        _currentState = BroadcastState.DeadAirFiller;
                        return _itemRegistry.GetNextDeadAirFiller();
                    }

                case BroadcastState.Conversation:
                    // Check whether the current arc finished after the last line.
                    var currentCaller = _repository.OnAirCaller;
                    bool endOfArc = false;
                    if (currentCaller != null && currentCaller.ActualArc != null)
                    {
                        var arc = currentCaller.ActualArc;
                        endOfArc = _currentConversationLineIndex >= arc.Dialogue.Count;
                    }

                    // If we're in break grace period and this is the end of current line, trigger transition
                    if (_isInBreakGracePeriod && !endOfArc)
                    {
                        GD.Print("BroadcastStateManager: Grace period active, triggering break transition");
                        _isInBreakGracePeriod = false; // Reset for next break
                        return _itemRegistry.GetVernItem("break_transition", GetVernCurrentMood());
                    }

                    if (endOfArc)
                    {
                        // End the previous caller's on-air status BEFORE transitioning
                        _repository.EndOnAir();
                        
                        // End of this caller's conversation: move to BetweenCallers if possible
                        if (_repository.HasOnHoldCallers)
                        {
                            _currentState = BroadcastState.BetweenCallers;
                            var mood = GetVernCurrentMood();
                            return _itemRegistry.GetVernItem("between_callers", mood);
                        }
                        else
                        {
                            // No callers waiting, go to dead air filler
                            _currentState = BroadcastState.DeadAirFiller;
                            return _itemRegistry.GetNextDeadAirFiller();
                        }
                    }
                    else
                    {
                        // Conversation continues with current caller
                        if (currentCaller != null)
                        {
                            return GetNextConversationItem();
                        }
                        else
                        {
                            // No active caller, fall back to BetweenCallers or DeadAir
                            if (_repository.HasOnHoldCallers)
                            {
                                _currentState = BroadcastState.BetweenCallers;
                                var mood = GetVernCurrentMood();
                                return _itemRegistry.GetVernItem("between_callers", mood);
                            }
                            else
                            {
                                _currentState = BroadcastState.DeadAirFiller;
                                return _itemRegistry.GetNextDeadAirFiller();
                            }
                        }
                    }

                case BroadcastState.BetweenCallers:
                    // Between-callers transition complete, put next caller on air
                    if (_repository.HasOnHoldCallers)
                    {
                        // Check if we can put caller on air (not in break window)
                        if (CanPutCallerOnAir())
                        {
                            var putOnAirResult = _repository.PutOnAir();
                            if (putOnAirResult.IsSuccess)
                            {
                                // Initialize conversation state for new caller
                                _currentConversationLineIndex = 0;
                                _currentConversationArcId = putOnAirResult.Value.ActualArc?.ArcId;

                                _currentState = BroadcastState.Conversation;
                                return GetNextConversationItem();
                            }
                            else
                            {
                                GD.PrintErr($"Failed to put caller on air after between-callers: {putOnAirResult.ErrorMessage}");
                                // Fallback to dead air filler
                                _currentState = BroadcastState.DeadAirFiller;
                                return _itemRegistry.GetNextDeadAirFiller();
                            }
                        }
                        else
                        {
                            // In break window, continue with dead air filler
                            _currentState = BroadcastState.DeadAirFiller;
                            return _itemRegistry.GetNextDeadAirFiller();
                        }
                    }
                    else
                    {
                        // No callers were available when between-callers started
                        _currentState = BroadcastState.DeadAirFiller;
                        return _itemRegistry.GetNextDeadAirFiller();
                    }

                case BroadcastState.DeadAirFiller:
                    // Dead air filler cycle complete, check if callers arrived
                    if (_repository.HasOnHoldCallers)
                    {
                        // Check if we can put caller on air (not in break window)
                        if (CanPutCallerOnAir())
                        {
                            var putOnAirResult = _repository.PutOnAir();
                            if (putOnAirResult.IsSuccess)
                            {
                                // Initialize conversation state for new caller
                                _currentConversationLineIndex = 0;
                                _currentConversationArcId = putOnAirResult.Value.ActualArc?.ArcId;

                                _currentState = BroadcastState.Conversation;
                                return GetNextConversationItem();
                            }
                            else
                            {
                                GD.PrintErr($"Failed to put caller on air from dead air filler: {putOnAirResult.ErrorMessage}");
                                // Continue with filler if put-on-air failed
                                return _itemRegistry.GetNextDeadAirFiller();
                            }
                        }
                        else
                        {
                            // In break window, continue with dead air filler
                            return _itemRegistry.GetNextDeadAirFiller();
                        }
                    }
                    else
                    {
                        // No callers, continue dead air filler cycle
                        return _itemRegistry.GetNextDeadAirFiller();
                    }

                case BroadcastState.ReturnFromBreak:
                    _currentState = BroadcastState.Conversation;
                    return GetNextConversationItem();
            }

            return GetNextItem();
        }

        private BroadcastItem? HandleItemInterrupted(string itemId)
        {
            // Handle interruptions (could transition to break, show ending, etc.)
            return null;
        }
    }
}
