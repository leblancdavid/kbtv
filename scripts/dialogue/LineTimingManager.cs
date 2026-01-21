using System;
using Godot;
using KBTV.Core;

namespace KBTV.Dialogue
{
    public partial class LineTimingManager : Node
    {
        private BroadcastLine _currentLine;
        private bool _lineInProgress = false;
        private float _lineStartTime = 0f;
        private float _lineDuration = 0f;

        public BroadcastLine? GetCurrentLine() => _lineInProgress ? _currentLine : null;

        public void StartLine(BroadcastLine line)
        {
            _currentLine = line;
            _lineInProgress = true;
            _lineStartTime = ServiceRegistry.Instance.TimeManager?.ElapsedTime ?? 0f;
            _lineDuration = GetLineDuration(line);
        }

        public bool UpdateProgress(float deltaTime)
        {
            if (!_lineInProgress)
            {
                return false;
            }

            var timeManager = ServiceRegistry.Instance.TimeManager;
            if (timeManager == null)
            {
                return false;
            }

            float elapsed = timeManager.ElapsedTime - _lineStartTime;
            if (elapsed >= _lineDuration)
            {
                _lineInProgress = false;
                return true; // Line completed
            }
            return false;
        }

        public void StopLine()
        {
            _lineInProgress = false;
            _lineStartTime = 0f;
            _lineDuration = 0f;
        }

        private static float GetLineDuration(BroadcastLine line)
        {
            return line.Type switch
            {
                BroadcastLineType.Music => 4f,
                BroadcastLineType.ShowOpening => 5f,
                BroadcastLineType.VernDialogue => 4f,
                BroadcastLineType.CallerDialogue => 4f,
                BroadcastLineType.BetweenCallers => 4f,
                BroadcastLineType.DeadAirFiller => 8f,
                BroadcastLineType.ShowClosing => 5f,
                BroadcastLineType.AdBreak => 2f, // Brief display for "AD BREAK" header
                BroadcastLineType.Ad => 4f,      // 4-second placeholder ads
                _ => 4f
            };
        }
    }
}