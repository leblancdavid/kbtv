using System;
using System.Collections.Generic;
using Godot;
using KBTV.UI.Themes;
using KBTV.Screening;
using KBTV.Core;

namespace KBTV.UI
{
    /// <summary>
    /// Modal dialog for evidence collection minigame.
    /// Implements a Wordle-style 5-letter word guessing game.
    /// </summary>
    public partial class EvidenceModal : Control
    {
        public event Action? ModalClosed;

        [Export]
        private Label _titleLabel = null!;

        [Export]
        private Control _wordDisplay = null!;

        [Export]
        private LineEdit _inputField = null!;

        [Export]
        private Button _guessButton = null!;

        [Export]
        private Label _attemptsLabel = null!;

        [Export]
        private Button _closeButton = null!;

        [Export]
        private Control _guessHistory = null!;

        private string _targetWord;
        private int _maxAttempts = 6;
        private int _currentAttempt = 0;
        private List<string> _previousGuesses = new();
        private bool _gameCompleted;

        private IScreeningController? _screeningController;

        public override void _Ready()
        {
            EnsureNodesInitialized();
            SetupModal();
            StartNewGame();
        }

        private void SetupModal()
        {
            // Set up input field restrictions
            _inputField.MaxLength = 5;
            _inputField.PlaceholderText = "Enter 5-letter word";

            // Connect signals
            _guessButton.Pressed += OnGuessPressed;
            _closeButton.Pressed += OnClosePressed;
            _inputField.TextSubmitted += OnTextSubmitted;

            // Make modal uninterruptible during gameplay
            _closeButton.Disabled = true;
        }

        private void StartNewGame()
        {
            // Select random evidence-related word
            _targetWord = SelectRandomWord();
            _currentAttempt = 0;
            _previousGuesses.Clear();
            _gameCompleted = false;

            UpdateUI();
            _inputField.GrabFocus();
        }

        private string SelectRandomWord()
        {
            // Evidence-related 5-letter words
            string[] evidenceWords = {
                "CLUES", "PROOF", "TRACE", "GHOST", "ALIEN", "WITCH", "CURSE",
                "SHRINE", "GRAVE", "ORB", "ECTO", "POLTERGEIST", "VOICES",
                "SHADOW", "HAUNT", "SPELL", "SIGHT", "AUDIO", "VIDEO", "PHOTO"
            };

            var random = new Random();
            return evidenceWords[random.Next(evidenceWords.Length)].ToUpper();
        }

        private void OnGuessPressed()
        {
            MakeGuess(_inputField.Text.Trim().ToUpper());
        }

        private void OnTextSubmitted(string text)
        {
            MakeGuess(text.Trim().ToUpper());
        }

        private void MakeGuess(string guess)
        {
            if (_gameCompleted || guess.Length != 5)
            {
                return;
            }

            // Validate input (must be 5 letters)
            if (!IsValidGuess(guess))
            {
                ShowInvalidGuessMessage();
                return;
            }

            _currentAttempt++;
            _previousGuesses.Add(guess);

            // Check if guess is correct
            if (guess == _targetWord)
            {
                OnGameWon(guess);
            }
            else if (_currentAttempt >= _maxAttempts)
            {
                OnGameLost();
            }
            else
            {
                AddGuessToHistory(guess);
                UpdateUI();
                _inputField.Clear();
                _inputField.GrabFocus();
            }
        }

        private bool IsValidGuess(string guess)
        {
            if (guess.Length != 5)
                return false;

            // Check if all characters are letters
            foreach (char c in guess)
            {
                if (!char.IsLetter(c))
                    return false;
            }

            return true;
        }

        private void ShowInvalidGuessMessage()
        {
            // Could add a temporary error message here
            _inputField.Clear();
            _inputField.GrabFocus();
        }

        private void AddGuessToHistory(string guess)
        {
            var guessResult = EvaluateGuess(guess);
            var guessLabel = new Label
            {
                Text = $"{guess} - {guessResult}",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _guessHistory.AddChild(guessLabel);
        }

        private string EvaluateGuess(string guess)
        {
            var result = new string[5];
            var targetChars = _targetWord.ToCharArray();
            var guessChars = guess.ToCharArray();

            // First pass: mark correct positions (green)
            for (int i = 0; i < 5; i++)
            {
                if (guessChars[i] == targetChars[i])
                {
                    result[i] = "🟩"; // Green square
                    targetChars[i] = '\0'; // Mark as used
                    guessChars[i] = '\0';
                }
            }

            // Second pass: mark wrong positions (yellow)
            for (int i = 0; i < 5; i++)
            {
                if (guessChars[i] != '\0')
                {
                    bool found = false;
                    for (int j = 0; j < 5; j++)
                    {
                        if (guessChars[i] == targetChars[j])
                        {
                            result[i] = "🟨"; // Yellow square
                            targetChars[j] = '\0'; // Mark as used
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        result[i] = "⬜"; // White square
                    }
                }
            }

            return string.Join("", result);
        }

        private void OnGameWon(string winningGuess)
        {
            _gameCompleted = true;
            AddGuessToHistory(winningGuess);

            // Collect evidence
            _screeningController = DependencyInjection.Get<IScreeningController>(this);
            bool success = _screeningController?.CollectEvidence(_targetWord) ?? false;

            if (success)
            {
                ShowSuccessMessage();
            }
            else
            {
                ShowErrorMessage("Failed to collect evidence");
            }

            _closeButton.Disabled = false;
        }

        private void OnGameLost()
        {
            _gameCompleted = true;
            ShowFailureMessage();
            _closeButton.Disabled = false;
        }

        private void ShowSuccessMessage()
        {
            var successLabel = new Label
            {
                Text = "🎉 EVIDENCE COLLECTED! 🎉",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            successLabel.AddThemeColorOverride("font_color", UIColors.Accent.Green);
            _guessHistory.AddChild(successLabel);
        }

        private void ShowFailureMessage()
        {
            var failureLabel = new Label
            {
                Text = "❌ Evidence lost - better luck next time!",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            failureLabel.AddThemeColorOverride("font_color", UIColors.Accent.Red);
            _guessHistory.AddChild(failureLabel);
        }

        private void ShowErrorMessage(string message)
        {
            var errorLabel = new Label
            {
                Text = $"⚠️ {message}",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            errorLabel.AddThemeColorOverride("font_color", UIColors.Warning.Critical);
            _guessHistory.AddChild(errorLabel);
        }

        private void UpdateUI()
        {
            _attemptsLabel.Text = $"Attempts remaining: {_maxAttempts - _currentAttempt}/{_maxAttempts}";

            // Clear previous history
            foreach (var child in _guessHistory.GetChildren())
            {
                child.QueueFree();
            }

            // Rebuild history
            foreach (var guess in _previousGuesses)
            {
                AddGuessToHistory(guess);
            }
        }

        private void OnClosePressed()
        {
            ModalClosed?.Invoke();
        }

        private void EnsureNodesInitialized()
        {
            _titleLabel ??= GetNodeOrNull<Label>("ModalPanel/VBoxContainer/TitleLabel");
            _wordDisplay ??= GetNodeOrNull<Control>("ModalPanel/VBoxContainer/WordDisplay");
            _inputField ??= GetNodeOrNull<LineEdit>("ModalPanel/VBoxContainer/InputContainer/InputField");
            _guessButton ??= GetNodeOrNull<Button>("ModalPanel/VBoxContainer/InputContainer/GuessButton");
            _attemptsLabel ??= GetNodeOrNull<Label>("ModalPanel/VBoxContainer/AttemptsLabel");
            _closeButton ??= GetNodeOrNull<Button>("ModalPanel/VBoxContainer/CloseButton");
            _guessHistory ??= GetNodeOrNull<Control>("ModalPanel/VBoxContainer/GuessHistory");
        }

        public override void _ExitTree()
        {
            if (_guessButton != null)
                _guessButton.Pressed -= OnGuessPressed;
            if (_closeButton != null)
                _closeButton.Pressed -= OnClosePressed;
            if (_inputField != null)
                _inputField.TextSubmitted -= OnTextSubmitted;
        }
    }
}