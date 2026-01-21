using System;
using Godot;
using KBTV.Callers;
using KBTV.Core;

namespace KBTV.Dialogue
{
    public partial class BroadcastStateManager : Node
    {
        private ICallerRepository _repository;
        private BroadcastCoordinator _coordinator;

        public BroadcastCoordinator.BroadcastState CurrentState { get; private set; } = BroadcastCoordinator.BroadcastState.Idle;

        public int FillerCycleCount { get; private set; } = 0;

        public BroadcastStateManager(ICallerRepository repository, BroadcastCoordinator coordinator)
        {
            _repository = repository;
            _coordinator = coordinator;
        }

        public void SetState(BroadcastCoordinator.BroadcastState newState)
        {
            CurrentState = newState;
        }

        public void AdvanceState()
        {
            switch (CurrentState)
            {
                case BroadcastCoordinator.BroadcastState.IntroMusic:
                    AdvanceFromIntroMusic();
                    break;
                case BroadcastCoordinator.BroadcastState.ShowOpening:
                    AdvanceFromShowOpening();
                    break;
                case BroadcastCoordinator.BroadcastState.Conversation:
                    AdvanceFromConversation();
                    break;
                case BroadcastCoordinator.BroadcastState.BetweenCallers:
                    AdvanceFromBetweenCallers();
                    break;
                case BroadcastCoordinator.BroadcastState.DeadAirFiller:
                    AdvanceFromFiller();
                    break;
                case BroadcastCoordinator.BroadcastState.OffTopicRemark:
                    AdvanceFromOffTopicRemark();
                    break;
                case BroadcastCoordinator.BroadcastState.ShowClosing:
                    CurrentState = BroadcastCoordinator.BroadcastState.Idle;
                    break;
            }
        }

        public void ResetFillerCycleCount()
        {
            FillerCycleCount = 0;
        }

        public void IncrementFillerCycle()
        {
            FillerCycleCount++;
        }

        private void AdvanceFromIntroMusic()
        {
            CurrentState = BroadcastCoordinator.BroadcastState.ShowOpening;
        }

        private void AdvanceFromShowOpening()
        {
            if (_repository.HasOnHoldCallers)
            {
                CurrentState = BroadcastCoordinator.BroadcastState.Conversation;
            }
            else
            {
                CurrentState = BroadcastCoordinator.BroadcastState.DeadAirFiller;
                FillerCycleCount = 0;
            }
        }

        private void AdvanceFromConversation()
        {
            if (_repository.OnAirCaller == null)
            {
                if (_repository.HasOnHoldCallers)
                {
                    CurrentState = BroadcastCoordinator.BroadcastState.BetweenCallers;
                }
                else
                {
                    CurrentState = BroadcastCoordinator.BroadcastState.DeadAirFiller;
                    FillerCycleCount = 0;
                }
            }
        }

        private void AdvanceFromBetweenCallers()
        {
            if (_repository.HasOnHoldCallers)
            {
                CurrentState = BroadcastCoordinator.BroadcastState.Conversation;
            }
            else
            {
                CurrentState = BroadcastCoordinator.BroadcastState.DeadAirFiller;
                FillerCycleCount = 0;
            }
        }

        private void AdvanceFromOffTopicRemark()
        {
            CurrentState = _coordinator.GetNextStateAfterOffTopic();
        }

        private void AdvanceFromFiller()
        {
            if (_repository.HasOnHoldCallers)
            {
                CurrentState = BroadcastCoordinator.BroadcastState.Conversation;
            }
            else
            {
                FillerCycleCount = 0;
            }
        }
    }
}