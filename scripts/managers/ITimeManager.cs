using System;
using Godot;

namespace KBTV.Managers
{
    public interface ITimeManager
    {
        float ElapsedTime { get; }
        float ShowDuration { get; }
        float Progress { get; }
        bool IsRunning { get; }
        string CurrentTimeFormatted { get; }
        float CurrentHour { get; }
        float RemainingTime { get; }
        string RemainingTimeFormatted { get; }

        event Action<float> Tick;
        event Action ShowEnded;
        event Action<bool> RunningChanged;

        void StartClock();
        void PauseClock();
        void ResetClock();
        void EndShow();
    }
}
