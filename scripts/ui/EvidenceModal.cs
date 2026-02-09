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
        private enum LetterState { Unused, CorrectPosition, WrongPosition, RuledOut }
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
        private Control _alphabetDisplay = null!;

        [Export]
        private Button _xButton = null!;

        [Export]
        private Button _collectButton = null!;

        [Export]
        private Control _guessHistory = null!;

        private LineEdit _hiddenInput;
        private RichTextLabel _currentInputDisplay;
        private Dictionary<char, LetterState> _letterStates = new();
        private string _currentInput = "";

        private string _targetWord;
        private int _maxAttempts = 6;
        private int _currentAttempt = 0;
        private List<string> _previousGuesses = new();
        private bool _gameCompleted;

        private IScreeningController? _screeningController;

        public override void _Ready()
        {
            GD.Print("EvidenceModal _Ready called");

            // Ensure modal can receive input and appear on top
            FocusMode = FocusModeEnum.All;
            MouseFilter = MouseFilterEnum.Stop;
            ZIndex = 100;

            EnsureNodesInitialized();
            SetupModal();
            StartNewGame();

            // Verify critical components
            GD.Print($"Components ready - XButton: {_xButton != null}, CollectButton: {_collectButton != null}");
        }

        private void SetupModal()
        {
            // Initialize letter states
            InitializeLetterStates();

            // Connect signals with debugging
            _xButton.Pressed += OnClosePressed;
            _collectButton.Pressed += OnCollectPressed;

            // Initially disable collect button
            _collectButton.Disabled = true;

            // Create alphabet display
            UpdateAlphabetDisplay();

            // Create current input display
            _currentInputDisplay = new RichTextLabel
            {
                BbcodeEnabled = true,
                Text = GetCurrentInputDisplay(),
                FitContent = true,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _guessHistory.AddChild(_currentInputDisplay);
        }

        private void InitializeLetterStates()
        {
            _letterStates.Clear();
            for (char c = 'A'; c <= 'Z'; c++)
            {
                _letterStates[c] = LetterState.Unused;
            }
        }

        private void UpdateAlphabetDisplay()
        {
            // Clear existing alphabet display
            foreach (var child in _alphabetDisplay.GetChildren())
            {
                child.QueueFree();
            }

            // Create 3 rows of letters
            var rows = new[] {
                "ABCDEFG",
                "HIJKLMN",
                "OPQRSTU",
                "VWXYZ"
            };

            foreach (var row in rows)
            {
                var rowContainer = new HBoxContainer
                {
                    Alignment = BoxContainer.AlignmentMode.Center
                };

                foreach (var letter in row)
                {
                    var letterLabel = new RichTextLabel
                    {
                        BbcodeEnabled = true,
                        Text = GetLetterDisplayText(letter),
                        FitContent = true,
                        CustomMinimumSize = new Vector2(20, 20)
                    };
                    rowContainer.AddChild(letterLabel);
                }

                _alphabetDisplay.AddChild(rowContainer);
            }
        }

        private string GetLetterDisplayText(char letter)
        {
            var color = _letterStates[letter] switch
            {
                LetterState.CorrectPosition => "green",
                LetterState.WrongPosition => "yellow",
                LetterState.RuledOut => "red",
                _ => "gray"
            };
            return $"[color={color}]{letter}[/color]";
        }

        private void StartNewGame()
        {
            // Select random evidence-related word
            _targetWord = SelectRandomWord();
            _currentAttempt = 0;
            _previousGuesses.Clear();
            _gameCompleted = false;
            _currentInput = "";

            UpdateUI();
            // Grab focus on the modal control itself for direct input handling
            CallDeferred(nameof(GrabModalFocus));
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

        private void GrabModalFocus()
        {
            GrabFocus();
        }

        public override void _Input(InputEvent @event)
        {
            if (_gameCompleted) return;

            if (@event is InputEventKey keyEvent && keyEvent.Pressed)
            {
                if (keyEvent.Keycode == Key.Enter)
                {
                    MakeGuess(_currentInput);
                    GetViewport().SetInputAsHandled();
                }
                else if (keyEvent.Keycode == Key.Backspace)
                {
                    if (_currentInput.Length > 0)
                    {
                        _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
                        UpdateCurrentInputDisplay();
                    }
                    GetViewport().SetInputAsHandled();
                }
                else if (keyEvent.Unicode != 0 && char.IsLetter((char)keyEvent.Unicode))
                {
                    char letter = char.ToUpper((char)keyEvent.Unicode);
                    if (_currentInput.Length < 5)
                    {
                        _currentInput += letter;
                        UpdateCurrentInputDisplay();
                    }
                    GetViewport().SetInputAsHandled();
                }
            }
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

            // Update letter states based on this guess
            UpdateLetterStates(guess);

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
                _currentInput = "";
                // No need to grab focus again - modal stays focused
            }
        }

        private void UpdateLetterStates(string guess)
        {
            var targetChars = _targetWord.ToCharArray();
            var guessChars = guess.ToCharArray();
            var usedPositions = new bool[5];

            // First pass: mark correct positions
            for (int i = 0; i < 5; i++)
            {
                if (guessChars[i] == targetChars[i])
                {
                    _letterStates[guessChars[i]] = LetterState.CorrectPosition;
                    usedPositions[i] = true;
                }
            }

            // Second pass: mark wrong positions
            for (int i = 0; i < 5; i++)
            {
                if (!usedPositions[i])
                {
                    bool found = false;
                    for (int j = 0; j < 5; j++)
                    {
                        if (!usedPositions[j] && guessChars[i] == targetChars[j])
                        {
                            if (_letterStates[guessChars[i]] != LetterState.CorrectPosition)
                            {
                                _letterStates[guessChars[i]] = LetterState.WrongPosition;
                            }
                            found = true;
                            usedPositions[j] = true;
                            break;
                        }
                    }
                    if (!found && _letterStates[guessChars[i]] == LetterState.Unused)
                    {
                        _letterStates[guessChars[i]] = LetterState.RuledOut;
                    }
                }
            }
        }

        private bool IsValidPartialInput(string input)
        {
            if (input.Length > 5) return false;
            foreach (char c in input)
            {
                if (!char.IsLetter(c)) return false;
            }
            return true;
        }

        private string GetCurrentInputDisplay()
        {
            var display = new string[5];
            for (int i = 0; i < 5; i++)
            {
                if (i < _currentInput.Length)
                {
                    display[i] = _currentInput[i].ToString();
                }
                else
                {
                    display[i] = "_";
                }
            }
            return $"> {string.Join(" ", display)}";
        }

        private void UpdateCurrentInputDisplay()
        {
            if (_currentInputDisplay != null)
            {
                _currentInputDisplay.Text = GetCurrentInputDisplay();
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
            _currentInput = "";
            UpdateCurrentInputDisplay();
        }

        private void AddGuessToHistory(string guess)
        {
            var guessResult = EvaluateGuess(guess);
            var guessLabel = new RichTextLabel
            {
                BbcodeEnabled = true,
                Text = guessResult,
                FitContent = true,
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
                    result[i] = $"[color=green]{guessChars[i]}[/color]";
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
                            result[i] = $"[color=yellow]{guessChars[i]}[/color]";
                            targetChars[j] = '\0'; // Mark as used
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        result[i] = "_";
                    }
                }
            }

            return $"> {string.Join(" ", result)}";
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
                _collectButton.Disabled = false; // Enable collect button
            }
            else
            {
                ShowErrorMessage("Failed to collect evidence");
            }
        }

        private void OnGameLost()
        {
            _gameCompleted = true;
            ShowFailureMessage();
        }

        private void ShowSuccessMessage()
        {
            var successLabel = new RichTextLabel
            {
                BbcodeEnabled = true,
                Text = "[color=green]🎉 EVIDENCE COLLECTED! 🎉[/color]",
                FitContent = true,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _guessHistory.AddChild(successLabel);
        }

        private void ShowFailureMessage()
        {
            var failureLabel = new RichTextLabel
            {
                BbcodeEnabled = true,
                Text = "[color=red]❌ Evidence lost - better luck next time![/color]",
                FitContent = true,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _guessHistory.AddChild(failureLabel);
        }

        private void ShowPrematureExitMessage()
        {
            var messageLabel = new RichTextLabel
            {
                BbcodeEnabled = true,
                Text = "[color=red]❌ Evidence opportunity lost - closed early[/color]",
                FitContent = true,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _guessHistory.AddChild(messageLabel);
        }

        private void ShowErrorMessage(string message)
        {
            var errorLabel = new RichTextLabel
            {
                BbcodeEnabled = true,
                Text = $"[color=red]⚠️ {message}[/color]",
                FitContent = true,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _guessHistory.AddChild(errorLabel);
        }

        private void UpdateUI()
        {
            _attemptsLabel.Text = $"Attempts remaining: {_maxAttempts - _currentAttempt}/{_maxAttempts}";

            // Update alphabet display
            UpdateAlphabetDisplay();

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

            // Add current input display
            _currentInputDisplay = new RichTextLabel
            {
                BbcodeEnabled = true,
                Text = GetCurrentInputDisplay(),
                FitContent = true,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _guessHistory.AddChild(_currentInputDisplay);
        }

        private void OnCollectPressed()
        {
            if (_gameCompleted)
            {
                ModalClosed?.Invoke();
            }
        }

        private void OnClosePressed()
        {
            if (!_gameCompleted)
            {
                ShowPrematureExitMessage();
                // Don't collect evidence - opportunity lost
            }
            ModalClosed?.Invoke();
        }

        private void EnsureNodesInitialized()
        {
            _titleLabel ??= GetNodeOrNull<Label>("ModalPanel/VBoxContainer/TitleLabel");
            _wordDisplay ??= GetNodeOrNull<Control>("ModalPanel/VBoxContainer/WordDisplay");
            _alphabetDisplay ??= GetNodeOrNull<Control>("ModalPanel/VBoxContainer/AlphabetDisplay");
            _attemptsLabel ??= GetNodeOrNull<Label>("ModalPanel/VBoxContainer/AttemptsLabel");
            _xButton ??= GetNodeOrNull<Button>("ModalPanel/XButton");
            _collectButton ??= GetNodeOrNull<Button>("ModalPanel/VBoxContainer/CollectButton");
            _guessHistory ??= GetNodeOrNull<Control>("ModalPanel/VBoxContainer/GuessHistory");
        }

        public override void _ExitTree()
        {
            if (_xButton != null)
                _xButton.Pressed -= OnClosePressed;
            if (_collectButton != null)
                _collectButton.Pressed -= OnCollectPressed;
        }
    }
}