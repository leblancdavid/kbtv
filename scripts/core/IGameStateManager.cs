using System;
using Godot;
using KBTV.Data;
using KBTV.Callers;

namespace KBTV.Core
{
    public interface IGameStateManager
    {
        GamePhase CurrentPhase { get; }
        int CurrentNight { get; }
        Topic SelectedTopic { get; }
        VernStats VernStats { get; }
        bool IsLive { get; }

        event Action<GamePhase, GamePhase> OnPhaseChanged;
        event Action<int> OnNightStarted;

        void InitializeGame();
        void AdvancePhase();
        void StartLiveShow();
        void SetPhase(GamePhase phase);
        void SetSelectedTopic(Topic topic);
        bool CanStartLiveShow();
        void StartNewNight();
    }
}
