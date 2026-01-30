using System;
using Godot;
using KBTV.Ads;
using KBTV.Managers;

namespace KBTV.Ads
{
    /// <summary>
    /// Handles scheduling and timing of advertisement breaks.
    /// Extracted from AdManager to improve modularity.
    /// </summary>
    public class BreakScheduler
    {
        private AdSchedule _schedule;
        private TimeManager _timeManager;
        private int _currentBreakIndex;

        // Timer callbacks
        private Action _onWindowTimerFired;
        private Action _onGraceTimerFired;
        private Action _onImminentTimerFired;
        private Action _onBreakTimerFired;

        public BreakScheduler(AdSchedule schedule, TimeManager timeManager, int currentBreakIndex)
        {
            _schedule = schedule;
            _timeManager = timeManager;
            _currentBreakIndex = currentBreakIndex;
        }

        public void SetCallbacks(Action onWindow, Action onGrace, Action onImminent, Action onBreak)
        {
            _onWindowTimerFired = onWindow;
            _onGraceTimerFired = onGrace;
            _onImminentTimerFired = onImminent;
            _onBreakTimerFired = onBreak;
        }

        public void ScheduleBreakTimers(Node node)
        {
            float currentTime = _timeManager?.ElapsedTime ?? 0f;
            int nextBreakIndex = _currentBreakIndex + 1;

            if (nextBreakIndex >= _schedule.Breaks.Count)
            {
                GD.Print($"BreakScheduler: All breaks completed - current: {_currentBreakIndex}, total: {_schedule.Breaks.Count}");
                return;
            }

            float breakTime = _schedule.Breaks[nextBreakIndex].ScheduledTime;

            // Schedule window opening
            float windowDelay = breakTime - AdConstants.BREAK_WINDOW_DURATION - currentTime;
            if (windowDelay > 0)
            {
                CreateBreakTimer(node, windowDelay, _onWindowTimerFired);
            }

            // Schedule grace period start
            float graceDelay = breakTime - AdConstants.BREAK_GRACE_TIME - currentTime;
            if (graceDelay > 0)
            {
                CreateBreakTimer(node, graceDelay, _onGraceTimerFired);
            }

            // Schedule imminent warning
            float imminentDelay = breakTime - AdConstants.BREAK_IMMINENT_TIME - currentTime;
            if (imminentDelay > 0)
            {
                CreateBreakTimer(node, imminentDelay, _onImminentTimerFired);
            }

            // Schedule break start
            float breakDelay = breakTime - currentTime;
            if (breakDelay > 0)
            {
                CreateBreakTimer(node, breakDelay, _onBreakTimerFired);
            }
        }

        private SceneTreeTimer CreateBreakTimer(Node node, float delay, Action callback)
        {
            var tree = node.GetTree();
            if (tree == null)
            {
                GD.PrintErr($"BreakScheduler: Cannot create timer - node not in scene tree (delay: {delay})");
                return null;
            }

            try
            {
                var timer = tree.CreateTimer(delay, false);
                timer.Timeout += () =>
                {
                    try
                    {
                        callback?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        GD.PrintErr($"BreakScheduler: Timer callback failed: {ex.Message}");
                    }
                };
                GD.Print($"BreakScheduler: Created timer with {delay:F1}s delay");
                return timer;
            }
            catch (Exception ex)
            {
                GD.PrintErr($"BreakScheduler: Failed to create timer (delay: {delay}): {ex.Message}");
                return null;
            }
        }

        public float GetNextBreakTime()
        {
            if (_schedule == null) return -1f;
            int nextBreakIndex = _currentBreakIndex + 1;
            if (nextBreakIndex >= _schedule.Breaks.Count) return -1f;
            return _schedule.Breaks[nextBreakIndex].ScheduledTime;
        }

        public void UpdateCurrentBreakIndex(int currentBreakIndex)
        {
            _currentBreakIndex = currentBreakIndex;
        }

        public string GetQueueButtonText(float timeUntilBreakWindow, float timeUntilNextBreak, bool isQueued)
        {
            if (isQueued)
            {
                int seconds = (int)timeUntilNextBreak;
                return $"QUEUED {seconds / 60}:{seconds % 60:D2}";
            }
            else
            {
                int seconds = (int)timeUntilBreakWindow;
                if (seconds <= 0)
                {
                    return "QUEUE AD-BREAK";
                }
                else
                {
                    return $"BREAK IN {seconds / 60}:{seconds % 60:D2}";
                }
            }
        }
    }
}