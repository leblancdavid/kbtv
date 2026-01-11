using System;
using System.Collections.Generic;
using KBTV.Callers;

namespace KBTV.Dialogue
{
    /// <summary>
    /// Represents a complete conversation between Vern and a caller.
    /// Contains all dialogue lines and tracks playback state.
    /// </summary>
    [Serializable]
    public class Conversation
    {
        private readonly List<DialogueLine> _lines;
        private int _currentIndex;
        private ConversationState _state;

        /// <summary>
        /// The caller participating in this conversation.
        /// </summary>
        public Caller Caller { get; private set; }

        /// <summary>
        /// All dialogue lines in this conversation.
        /// </summary>
        public IReadOnlyList<DialogueLine> Lines => _lines;

        /// <summary>
        /// Current line index being displayed.
        /// </summary>
        public int CurrentIndex => _currentIndex;

        /// <summary>
        /// Current playback state.
        /// </summary>
        public ConversationState State => _state;

        /// <summary>
        /// The currently active dialogue line (or null if not playing).
        /// </summary>
        public DialogueLine CurrentLine =>
            _lines != null && _currentIndex >= 0 && _currentIndex < _lines.Count
                ? _lines[_currentIndex]
                : null;

        /// <summary>
        /// Whether there are more lines to display.
        /// </summary>
        public bool HasMoreLines => _lines != null && _currentIndex < _lines.Count - 1;

        /// <summary>
        /// Current phase of the conversation based on current line.
        /// </summary>
        public ConversationPhase CurrentPhase =>
            CurrentLine?.Phase ?? ConversationPhase.Intro;

        /// <summary>
        /// Progress through the conversation (0.0 to 1.0).
        /// </summary>
        public float Progress =>
            _lines != null && _lines.Count > 0 ? (float)(_currentIndex + 1) / _lines.Count : 0f;

        // Events
        public event Action<DialogueLine> OnLineStarted;
        public event Action<DialogueLine> OnLineEnded;
        public event Action<ConversationPhase> OnPhaseChanged;
        public event Action OnConversationStarted;
        public event Action OnConversationCompleted;

        public Conversation(Caller caller)
        {
            Caller = caller;
            _lines = new List<DialogueLine>();
            _currentIndex = -1;
            _state = ConversationState.NotStarted;
        }

        public Conversation(Caller caller, IEnumerable<DialogueLine> lines) : this(caller)
        {
            _lines.AddRange(lines);
        }

        /// <summary>
        /// Add a dialogue line to the conversation.
        /// </summary>
        public void AddLine(DialogueLine line)
        {
            _lines.Add(line);
        }

        /// <summary>
        /// Add a dialogue line with parameters.
        /// </summary>
        public void AddLine(Speaker speaker, string text, DialogueTone tone, ConversationPhase phase, float duration = 0f)
        {
            _lines.Add(new DialogueLine(speaker, text, tone, phase, duration));
        }

        /// <summary>
        /// Start the conversation from the beginning.
        /// </summary>
        public void Start()
        {
            if (_lines.Count == 0)
            {
                _state = ConversationState.Completed;
                OnConversationCompleted?.Invoke();
                return;
            }

            _state = ConversationState.Playing;
            _currentIndex = 0;
            OnConversationStarted?.Invoke();
            OnLineStarted?.Invoke(CurrentLine);
        }

        /// <summary>
        /// Advance to the next line. Returns true if advanced, false if conversation ended.
        /// </summary>
        public bool AdvanceToNextLine()
        {
            if (_state != ConversationState.Playing)
                return false;

            DialogueLine previousLine = CurrentLine;
            ConversationPhase previousPhase = CurrentPhase;

            OnLineEnded?.Invoke(previousLine);

            if (!HasMoreLines)
            {
                _state = ConversationState.Completed;
                OnConversationCompleted?.Invoke();
                return false;
            }

            _currentIndex++;

            // Check if phase changed
            if (CurrentLine.Phase != previousPhase)
            {
                OnPhaseChanged?.Invoke(CurrentLine.Phase);
            }

            OnLineStarted?.Invoke(CurrentLine);
            return true;
        }

        /// <summary>
        /// Pause the conversation playback.
        /// </summary>
        public void Pause()
        {
            if (_state == ConversationState.Playing)
            {
                _state = ConversationState.Paused;
            }
        }

        /// <summary>
        /// Resume a paused conversation.
        /// </summary>
        public void Resume()
        {
            if (_state == ConversationState.Paused)
            {
                _state = ConversationState.Playing;
            }
        }

        /// <summary>
        /// Force end the conversation early.
        /// </summary>
        public void End()
        {
            if (_state == ConversationState.Completed)
                return;

            _state = ConversationState.Completed;
            OnConversationCompleted?.Invoke();
        }

        /// <summary>
        /// Reset the conversation to the beginning.
        /// </summary>
        public void Reset()
        {
            _currentIndex = -1;
            _state = ConversationState.NotStarted;
        }

        /// <summary>
        /// Get all lines for a specific phase.
        /// </summary>
        public IEnumerable<DialogueLine> GetLinesForPhase(ConversationPhase phase)
        {
            foreach (var line in _lines)
            {
                if (line.Phase == phase)
                    yield return line;
            }
        }

        /// <summary>
        /// Get all lines spoken by a specific speaker.
        /// </summary>
        public IEnumerable<DialogueLine> GetLinesBySpeaker(Speaker speaker)
        {
            foreach (var line in _lines)
            {
                if (line.Speaker == speaker)
                    yield return line;
            }
        }

        public override string ToString()
        {
            return $"Conversation with {Caller?.Name ?? "Unknown"}: {_lines.Count} lines, {_state}";
        }
    }
}
