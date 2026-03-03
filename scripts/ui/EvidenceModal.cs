using Godot;
using System;
using System.Linq;
using Chickensoft.GoDotTest;
using System.Collections.Generic;
using KBTV.Callers;
using KBTV.UI;
using KBTV.Managers;
using KBTV.Screening;
using KBTV.Core;
using KBTV.Items;
using KBTV.Persistence;

namespace KBTV.UI;

public partial class EvidenceModal : Control
{
    // Event for when modal is closed
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
    private Button _collectButton = null!;

    [Export]
    private Control _guessHistory = null!;

    [Export]
    private Button _closeButton = null!;

    private LineEdit _hiddenInput;
    private RichTextLabel _currentInputDisplay;
    private Label _descriptionLabel = null!;
    private Dictionary<char, LetterState> _letterStates = new();
    private char[] _currentInputChars = new char[5];
    private bool[] _positionsFilled = new bool[5];

    private string _targetWord;
    private int _maxAttempts = 6;
    private int _currentAttempt = 0;
    private List<string> _previousGuesses = new();
    private bool _gameCompleted;

    private EvidenceTier _discoveredTier;
    private bool _evidenceCollected = false;

    private IScreeningController? _screeningController;
    private bool _dependencyResolutionAttempted = false;

    // Cached caller reference and patience tracking
    private Caller? _caller;
    private ProgressBar? _patienceProgressBar;
    private Label? _callerNameLabel;
    private float _initialPatience;

    // Word list configuration
    private static List<string> _wordList = new();
    private static bool _wordListLoaded = false;
    private const string WORD_LIST_PATH = "res://assets/config/evidence_words.json";

    // Fallback word list in case JSON loading fails
    private static readonly string[] FallbackWords = {
        "HOUSE", "PHONE", "TRUCK", "LIGHT", "PAPER", "TABLE",
        "GHOST", "ALIEN", "PROOF", "TRACE", "SIGHT", "AUDIO",
        "VIDEO", "PHOTO", "SPELL", "CURSE", "DEMON", "ANGEL",
        "SPIRIT", "NIGHT", "DARK", "MOON", "CLOUD", "MISTY",
        "BEAST", "STORY", "TALES", "TRUTH", "GUESS", "CLUES", "SIGNS"
    };

    /// <summary>
    /// Initialize the modal with the caller being screened.
    /// Must be called immediately after instantiation.
    /// </summary>
    public void Initialize(Caller? caller)
    {
        _caller = caller;
        _initialPatience = caller?.ScreeningPatience ?? 0f;
        GD.Print($"EvidenceModal: Initialized for caller '{caller?.Name ?? "null"}' with patience {_initialPatience}");
        
        // Subscribe to caller disconnection event
        if (_caller != null)
        {
            _caller.OnDisconnected += OnCallerDisconnected;
            GD.Print($"EvidenceModal: Subscribed to OnDisconnected event for {_caller.Name}");
        }
        
        // Load word list if not already loaded
        LoadWordList();
    }

    /// <summary>
    /// Handle caller disconnection event.
    /// </summary>
    private void OnCallerDisconnected()
    {
        try
        {
            GD.Print("EvidenceModal: Caller disconnected, invoking ModalClosed");
            ModalClosed?.Invoke();
            GD.Print("EvidenceModal: ModalClosed invoked successfully");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"EvidenceModal: Error in OnCallerDisconnected - {ex.Message}");
        }
    }

    /// <summary>
    /// Load the word list from the JSON configuration file.
    /// Falls back to embedded defaults if loading fails.
    /// </summary>
    private static void LoadWordList()
    {
        if (_wordListLoaded)
        {
            return;
        }

        _wordListLoaded = true;

        try
        {
            if (!Godot.FileAccess.FileExists(WORD_LIST_PATH))
            {
                GD.PrintErr($"EvidenceModal: Word list file not found at {WORD_LIST_PATH}, using fallback");
                UseFallbackWords();
                return;
            }

            var file = Godot.FileAccess.Open(WORD_LIST_PATH, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"EvidenceModal: Failed to open word list file: {Godot.FileAccess.GetOpenError()}, using fallback");
                UseFallbackWords();
                return;
            }

            string json = file.GetAsText();
            file.Close();

            var jsonParse = Json.ParseString(json);
            if (jsonParse.VariantType == Variant.Type.Nil)
            {
                GD.PrintErr("EvidenceModal: Failed to parse word list JSON, using fallback");
                UseFallbackWords();
                return;
            }

            var dict = (Godot.Collections.Dictionary)jsonParse;
            if (!dict.ContainsKey("words"))
            {
                GD.PrintErr("EvidenceModal: Word list JSON missing 'words' key, using fallback");
                UseFallbackWords();
                return;
            }

            var wordsArray = (Godot.Collections.Array)dict["words"];
            _wordList.Clear();

            foreach (var wordVariant in wordsArray)
            {
                string word = wordVariant.ToString().ToUpper();
                
                // Validate: must be exactly 5 letters
                if (word.Length == 5)
                {
                    _wordList.Add(word);
                }
                else
                {
                    GD.PrintErr($"EvidenceModal: Skipping invalid word '{word}' (length {word.Length}, expected 5)");
                }
            }

            if (_wordList.Count < 50)
            {
                GD.PrintErr($"EvidenceModal: Only {_wordList.Count} valid words loaded, using fallback");
                UseFallbackWords();
                return;
            }

            GD.Print($"EvidenceModal: Successfully loaded {_wordList.Count} words from {WORD_LIST_PATH}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"EvidenceModal: Exception loading word list: {ex.Message}, using fallback");
            UseFallbackWords();
        }
    }

    /// <summary>
    /// Use the fallback embedded word list.
    /// </summary>
    private static void UseFallbackWords()
    {
        _wordList.Clear();
        _wordList.AddRange(FallbackWords);
        GD.Print($"EvidenceModal: Using fallback list with {_wordList.Count} words");
    }

    /// <summary>
    /// Resolve dependencies when modal is properly in scene tree.
    /// </summary>
    private void ResolveDependencies()
    {
        if (_dependencyResolutionAttempted)
            return;
            
        _dependencyResolutionAttempted = true;
        
        try
        {
            _screeningController = DependencyInjection.Get<IScreeningController>(this);
            GD.Print("EvidenceModal: Successfully resolved IScreeningController");
        }
        catch (InvalidOperationException ex)
        {
            GD.PrintErr($"EvidenceModal: Failed to resolve IScreeningController - {ex.Message}");
            _screeningController = null;
        }
    }

    /// <summary>
    /// Applies monospace font to a control for terminal-like display.
    /// Uses the same font pattern as other UI components in the project.
    /// </summary>
    private static void ApplyMonospaceFont(Control control)
    {
        control.AddThemeFontOverride("font", UITheme.MonoFont);
    }

    public override void _Ready()
    {
        GD.Print("EvidenceModal _Ready called");

        // Ensure modal can receive input and appear on top
        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 100;

        EnsureNodesInitialized();
        
        // Verify node paths are working
        GD.Print($"Node paths - GuessHistory: {_guessHistory?.Name}, Alphabet: {_alphabetDisplay?.Name}, Attempts: {_attemptsLabel?.Name}");
        
        SetupModal();
        StartNewGame();

        // Verify critical components
        GD.Print($"Components ready - CollectButton: {_collectButton != null}");
        
        // Verify terminal display is ready
        GD.Print($"Terminal display: {(_currentInputDisplay != null ? "Created" : "NOT CREATED")}");
        if (_currentInputDisplay != null)
        {
            GD.Print($"Terminal text: '{_currentInputDisplay.Text}'");
        }
        
        // Try to resolve dependencies now that we're in the scene tree
        ResolveDependencies();
    }

    private void SetupModal()
    {
        // Initialize letter states
        InitializeLetterStates();

        // Connect signals with debugging
        _collectButton.Pressed += OnCollectPressed;

        // Initially disable collect button, it will be enabled when evidence is collected
        _collectButton.Disabled = true;
        _collectButton.Text = "Dismiss";

        // Set patience progress bar initial value and max
        if (_patienceProgressBar != null)
        {
            _patienceProgressBar.MaxValue = _initialPatience;
            _patienceProgressBar.Value = _initialPatience;
        }

        // Create alphabet display
        UpdateAlphabetDisplay();

        // Setup close button (if not already set via Export)
        if (_closeButton == null)
        {
            SetupCloseButton();
        }
        else
        {
            _closeButton.Pressed += OnClosePressed;
        }

        // Ensure terminal display is visible immediately
        GD.Print("Setting up modal - creating terminal display");
        
        // Create current input display
        _currentInputDisplay = new RichTextLabel
        {
            BbcodeEnabled = true,
            Text = GetCurrentInputDisplay(),
            FitContent = true,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _currentInputDisplay.AddThemeFontSizeOverride("normal_font_size", 24);
        ApplyMonospaceFont(_currentInputDisplay);
        
        // Add to ScrollContainer content - FIXED: _guessHistory IS the ScrollContainer
        var guessHistoryContent = _guessHistory.GetChild(0) as VBoxContainer;
        if (guessHistoryContent != null)
        {
            guessHistoryContent.AddChild(_currentInputDisplay);
            GD.Print($"Terminal display added to content. Total children: {guessHistoryContent.GetChildCount()}");
        }
        else
        {
            GD.Print("ERROR: Could not find ScrollContainer content for terminal display!");
            GD.Print($"_guessHistory type: {_guessHistory?.GetType().Name}");
            GD.Print($"_guessHistory child count: {_guessHistory?.GetChildCount()}");
        }
    }

    /// <summary>
    /// Sets up the close button in the top-right corner of the modal.
    /// Creates an X button with styling if one doesn't exist via Export.
    /// </summary>
    private void SetupCloseButton()
    {
        var headerContainer = GetNodeOrNull<HBoxContainer>("ModalPanel/ContentContainer/HeaderContainer");
        if (headerContainer == null)
        {
            GD.PrintErr("EvidenceModal: Cannot find HeaderContainer for close button");
            return;
        }

        // Create close button
        _closeButton = new Button
        {
            Text = "X",
            CustomMinimumSize = new Vector2(30, 30),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            SizeFlagsVertical = Control.SizeFlags.Expand
        };

        // Style the close button
        var closeStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.2f, 0.2f, 0.2f, 1.0f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _closeButton.AddThemeStyleboxOverride("normal", closeStyle);

        var closeHoverStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.8f, 0.2f, 0.2f, 1.0f),
            CornerRadiusTopLeft = 4,
            CornerRadiusTopRight = 4,
            CornerRadiusBottomLeft = 4,
            CornerRadiusBottomRight = 4
        };
        _closeButton.AddThemeStyleboxOverride("hover", closeHoverStyle);

        _closeButton.AddThemeFontSizeOverride("font_size", 18);
        _closeButton.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f, 1.0f));

        // Connect close button signal
        _closeButton.Pressed += OnClosePressed;

        // Add to header container (after patience bar)
        headerContainer.AddChild(_closeButton);

        GD.Print("EvidenceModal: Close button created and added to header container");
    }

    /// <summary>
    /// Handles close button press - closes the modal.
    /// </summary>
    private void OnClosePressed()
    {
        GD.Print("EvidenceModal: Close button clicked");
        ModalClosed?.Invoke();
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

        // Create 4 rows of letters with enhanced spacing
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
            rowContainer.AddThemeConstantOverride("separation", 8);
            rowContainer.SizeFlagsVertical = Control.SizeFlags.Expand;

                foreach (var letter in row)
                {
                    var letterLabel = new RichTextLabel
                    {
                        BbcodeEnabled = true,
                        Text = GetLetterDisplayText(letter),
                        FitContent = true,
                        CustomMinimumSize = new Vector2(25, 25)
                    };
                    letterLabel.SizeFlagsVertical = Control.SizeFlags.Expand;
                    letterLabel.AddThemeFontSizeOverride("normal_font_size", 20);
                    ApplyMonospaceFont(letterLabel);
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
        // Ensure word list is loaded
        LoadWordList();
        
        // Select a random word from the loaded list
        if (_wordList.Count > 0)
        {
            _targetWord = _wordList[(int)(GD.Randi() % (uint)_wordList.Count)];
            GD.Print("");
            GD.Print("========================================");
            GD.Print($"TARGET WORD: {_targetWord}");
            GD.Print("========================================");
            GD.Print($"(from {_wordList.Count} words)");
            GD.Print("");
        }
        else
        {
            // Ultimate fallback - should never happen
            _targetWord = "HOUSE";
            GD.PrintErr("EvidenceModal: Word list empty, using hardcoded fallback");
        }

        // Reset game state
        _currentAttempt = 0;
        _previousGuesses.Clear();
        _gameCompleted = false;
        _currentInputChars = new char[] { '_', '_', '_', '_', '_' };
        _positionsFilled = new bool[5];

        // Reset letter states
        InitializeLetterStates();

        // Update UI
        UpdateUI();

        // Reset button state - disabled until evidence is collected
        _collectButton.Disabled = true;
        _collectButton.Text = "Collect Evidence";
    }

    private void GrabModalFocus()
    {
        GrabFocus();
    }

    public override void _Process(double delta)
    {
        // Check if caller is still valid and update patience bar
        if (_caller != null && !_gameCompleted)
        {
            // Update patience progress bar - use same calculation as ScreeningPanel
            if (_patienceProgressBar != null && IsInstanceValid(_patienceProgressBar))
            {
                var screeningController = DependencyInjection.Get<IScreeningController>(this);
                if (screeningController != null)
                {
                    var progress = screeningController.Progress;
                    _patienceProgressBar.Value = _caller.ScreeningPatience - progress.ElapsedTime;
                }
                else
                {
                    _patienceProgressBar.Value = _caller.ScreeningPatience;
                }
            }
            
            // Check if patience expired (fallback if event doesn't fire)
            if (_caller.ScreeningPatience <= 0f)
            {
                GD.Print($"EvidenceModal: Patience expired for {_caller.Name}, closing modal");
                ModalClosed?.Invoke();
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_gameCompleted) return;

        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Enter)
            {
                if (IsCompleteGuess())
                {
                    MakeGuess(GetCurrentGuess());
                }
                GetViewport().SetInputAsHandled();
            }
            else if (keyEvent.Keycode == Key.Backspace)
            {
                // Find the last unfilled, unlocked position and clear it
                for (int i = 4; i >= 0; i--)
                {
                    if (!_positionsFilled[i] && _currentInputChars[i] != '_')
                    {
                        _currentInputChars[i] = '_';
                        UpdateCurrentInputDisplay();
                        break;
                    }
                }
                GetViewport().SetInputAsHandled();
            }
            else if (keyEvent.Unicode != 0 && char.IsLetter((char)keyEvent.Unicode))
            {
                char letter = char.ToUpper((char)keyEvent.Unicode);
                // Find the first unfilled, unlocked position and fill it
                for (int i = 0; i < 5; i++)
                {
                    if (!_positionsFilled[i] && _currentInputChars[i] == '_')
                    {
                        _currentInputChars[i] = letter;
                        UpdateCurrentInputDisplay();
                        break;
                    }
                }
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void MakeGuess(string guess)
    {
        if (_gameCompleted || !IsCompleteGuess())
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
            // Prepare next input: keep correct letters, clear wrong ones
            PrepareNextInput(guess);
            UpdateUI();
        }
    }

    private bool IsCompleteGuess()
    {
        for (int i = 0; i < 5; i++)
        {
            if (_currentInputChars[i] == '_')
                return false;
        }
        return true;
    }

    private string GetCurrentGuess()
    {
        return new string(_currentInputChars);
    }

    private void PrepareNextInput(string guess)
    {
        // Defensive: validate inputs
        if (string.IsNullOrEmpty(_targetWord) || string.IsNullOrEmpty(guess) || guess.Length != 5 || _targetWord.Length != 5)
        {
            GD.PrintErr($"EvidenceModal.PrepareNextInput: Invalid input - target: '{_targetWord}', guess: '{guess}'");
            return;
        }
        
        var targetChars = _targetWord.ToCharArray();
        var guessChars = guess.ToCharArray();

        for (int i = 0; i < 5; i++)
        {
            if (guessChars[i] == targetChars[i])
            {
                // Correct position - keep the letter filled
                _currentInputChars[i] = guessChars[i];
                _positionsFilled[i] = true;
            }
            else
            {
                // Wrong position - clear for next attempt
                _currentInputChars[i] = '_';
                _positionsFilled[i] = false;
            }
        }
    }

    private void UpdateLetterStates(string guess)
    {
        // Defensive: validate inputs
        if (string.IsNullOrEmpty(_targetWord) || string.IsNullOrEmpty(guess) || guess.Length != 5 || _targetWord.Length != 5)
        {
            GD.PrintErr($"EvidenceModal.UpdateLetterStates: Invalid input - target: '{_targetWord}', guess: '{guess}'");
            return;
        }
        
        var targetChars = _targetWord.ToCharArray();
        var guessChars = guess.ToCharArray();
        var usedPositions = new bool[5];

        // First pass: mark correct positions
        for (int i = 0; i < 5; i++)
        {
            if (guessChars[i] == targetChars[i])
            {
                // Ensure the character exists in dictionary before accessing
                if (_letterStates.ContainsKey(guessChars[i]))
                {
                    _letterStates[guessChars[i]] = LetterState.CorrectPosition;
                }
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
                        // Ensure the character exists in dictionary before accessing
                        if (_letterStates.ContainsKey(guessChars[i]) && _letterStates[guessChars[i]] != LetterState.CorrectPosition)
                        {
                            _letterStates[guessChars[i]] = LetterState.WrongPosition;
                        }
                        found = true;
                        usedPositions[j] = true;
                        break;
                    }
                }
                if (!found)
                {
                    // Ensure the character exists in dictionary before accessing
                    if (_letterStates.ContainsKey(guessChars[i]) && _letterStates[guessChars[i]] == LetterState.Unused)
                    {
                        _letterStates[guessChars[i]] = LetterState.RuledOut;
                    }
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
            if (_positionsFilled[i] && _currentInputChars[i] != '_')
            {
                // Locked correct letter - show in green
                display[i] = $"[color=green]{_currentInputChars[i]}[/color]";
            }
            else if (_currentInputChars[i] == '_')
            {
                // Empty position - show as gray underscore
                display[i] = $"[color=gray]_[/color]";
            }
            else
            {
                // Unfilled position - show as white letter
                display[i] = _currentInputChars[i].ToString();
            }
        }
        return $"> {string.Join(" ", display)}";
    }

    private void UpdateCurrentInputDisplay()
    {
        if (_currentInputDisplay != null)
        {
            _currentInputDisplay.Text = GetCurrentInputDisplay();
            GD.Print($"Terminal updated: '{_currentInputDisplay.Text}'");
        }
        else
        {
            GD.Print("ERROR: _currentInputDisplay is null in UpdateCurrentInputDisplay!");
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
        // For now, just keep the current input
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
        guessLabel.AddThemeFontSizeOverride("normal_font_size", 24);
        ApplyMonospaceFont(guessLabel);
        
        // Find the ScrollContainer and its content - FIXED node path
        var guessHistoryContent = _guessHistory.GetChild(0) as VBoxContainer;
        if (guessHistoryContent != null)
        {
            guessHistoryContent.AddChild(guessLabel);
        }
        else
        {
            GD.Print("ERROR: Could not find ScrollContainer content in AddGuessToHistory!");
        }
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
                    result[i] = $"[color=red]{guessChars[i]}[/color]";
                }
            }
        }

        return $"> {string.Join(" ", result)}";
    }

    private void OnGameWon(string winningGuess)
    {
        _gameCompleted = true;
        _evidenceCollected = false; // Reset collection flag
        AddGuessToHistory(winningGuess);

        // Reset patience when evidence is revealed (both in minigame and screening panel)
        var screeningController = DependencyInjection.Get<IScreeningController>(this);
        screeningController?.ResetPatienceAndTime();

        // Reset progress bar to full immediately
        if (_patienceProgressBar != null)
        {
            _patienceProgressBar.Value = _patienceProgressBar.MaxValue;
        }

        // Roll loot table to determine evidence tier
        _discoveredTier = RollEvidenceTier();
        
        // Show discovery message instead of collecting immediately
        ShowDiscoveryMessage(_discoveredTier);
        
        // Enable collect button for user to collect evidence
        _collectButton.Disabled = false;
        _collectButton.Text = "Collect Evidence";
        GD.Print($"Collect button enabled for evidence discovery (tier: {_discoveredTier})");
    }

    /// <summary>
    /// Attempt to collect evidence. Tries via screening controller first,
    /// falls back to direct creation if caller is no longer current.
    /// </summary>
    private bool TryCollectEvidence(string? callerName, string? evidenceLevel, string? callerId)
    {
        // Check if we have cached caller data
        if (string.IsNullOrEmpty(callerName) || string.IsNullOrEmpty(callerId))
        {
            GD.PrintErr("EvidenceModal: Cannot collect evidence - no cached caller data");
            return false;
        }

        // First, try using the screening controller if the cached caller is still current
        if (_screeningController != null)
        {
            var currentCaller = _screeningController.CurrentCaller;
            
            // If the cached caller is the same as the current caller, use normal flow
            if (currentCaller != null && currentCaller.Id == callerId)
            {
                GD.Print("EvidenceModal: Cached caller matches current caller, using normal collection flow");
                bool success = _screeningController.CollectEvidence(_targetWord, _discoveredTier);
                if (success)
                {
                    return true;
                }
                GD.Print("EvidenceModal: Normal collection failed, trying direct creation");
            }
        }

        // Fallback: Create evidence directly using cached data
        GD.Print("EvidenceModal: Using direct evidence creation for cached caller");
        return CreateEvidenceDirectly(callerName, evidenceLevel);
    }

    /// <summary>
    /// Create evidence item directly without going through screening controller.
    /// Used when the caller is no longer the current screening session.
    /// </summary>
    private bool CreateEvidenceDirectly(string callerName, string? evidenceLevel)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrEmpty(callerName))
            {
                GD.PrintErr("EvidenceModal: Cannot create evidence - caller name is null or empty");
                return false;
            }

            // Check target word
            if (string.IsNullOrEmpty(_targetWord))
            {
                GD.PrintErr("EvidenceModal: Cannot create evidence - _targetWord is null or empty");
                return false;
            }

            // Use default evidence level if not provided
            evidenceLevel ??= "None";

            GD.Print($"EvidenceModal: Creating evidence item - Word: {_targetWord}, Caller: {callerName}, Level: {evidenceLevel}, Tier: {_discoveredTier}");

            // Create evidence item using cached data and pre-rolled tier
            var evidence = EvidenceItem.Create(
                _targetWord,
                callerName,
                evidenceLevel,
                _discoveredTier
            );

            if (evidence == null)
            {
                GD.PrintErr("EvidenceModal: EvidenceItem.Create returned null");
                return false;
            }

            GD.Print("EvidenceModal: Evidence item created successfully");

            // Get save manager and save evidence
            SaveManager? saveManager = null;
            try
            {
                saveManager = DependencyInjection.Get<SaveManager>(this);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"EvidenceModal: Failed to get SaveManager - {ex.Message}");
                return false;
            }

            if (saveManager == null)
            {
                GD.PrintErr("EvidenceModal: SaveManager is null");
                return false;
            }

            GD.Print("EvidenceModal: Got SaveManager successfully");

            if (saveManager.CurrentSave == null)
            {
                GD.PrintErr("EvidenceModal: CurrentSave is null");
                return false;
            }

            GD.Print("EvidenceModal: CurrentSave is valid");

            if (saveManager.CurrentSave.CollectedEvidence == null)
            {
                GD.Print("EvidenceModal: CollectedEvidence list is null, initializing it");
                saveManager.CurrentSave.CollectedEvidence = new System.Collections.Generic.List<Items.EvidenceItem>();
            }

            GD.Print($"EvidenceModal: CollectedEvidence list has {saveManager.CurrentSave.CollectedEvidence.Count} items");

            saveManager.CurrentSave.CollectedEvidence.Add(evidence);

            // Also store in new EvidenceSystem for immediate availability
            if (saveManager.CurrentSave.EvidenceSystem == null)
            {
                GD.Print("EvidenceModal: EvidenceSystem is null, initializing it");
                saveManager.CurrentSave.EvidenceSystem = new Persistence.EvidenceSystemData();
            }

            if (saveManager.CurrentSave.EvidenceSystem.RawEvidence == null)
            {
                saveManager.CurrentSave.EvidenceSystem.RawEvidence = new System.Collections.Generic.List<Persistence.IdentifiedEvidenceData>();
            }

            var rawEvidenceData = new Persistence.IdentifiedEvidenceData
            {
                Word = evidence.Word,
                SourceCallerName = evidence.SourceCallerName,
                EvidenceLevel = evidence.EvidenceLevel,
                Tier = (int)evidence.Tier,
                BonusType = 0, // Will be determined during analysis
                BonusAmount = 0f,
                Status = 0 // EvidenceStatus.Raw
            };

            saveManager.CurrentSave.EvidenceSystem.RawEvidence.Add(rawEvidenceData);

            saveManager.Save();

            _evidenceCollected = true;
            GD.Print($"EvidenceModal: Direct evidence creation successful for {callerName} with tier {_discoveredTier}");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"EvidenceModal: Failed to create evidence directly - {ex.Message}");
            GD.PrintErr($"EvidenceModal: Stack trace - {ex.StackTrace}");
            return false;
        }
    }

    private void OnGameLost()
    {
        _gameCompleted = true;
        ShowFailureMessage();
        _collectButton.Disabled = false; // Enable button for dismissal
        _collectButton.Text = "Dismiss";

        // Mark evidence opportunity as lost to hide the examine button
        _screeningController?.LoseEvidenceOpportunity();
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
        successLabel.AddThemeFontSizeOverride("normal_font_size", 20);
        ApplyMonospaceFont(successLabel);
        
        // Find the ScrollContainer content - FIXED node path
        var guessHistoryContent = _guessHistory.GetChild(0) as VBoxContainer;
        if (guessHistoryContent != null)
        {
            guessHistoryContent.AddChild(successLabel);
        }
        else
        {
            GD.Print("ERROR: Could not find ScrollContainer content in ShowSuccessMessage!");
        }
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
        failureLabel.AddThemeFontSizeOverride("normal_font_size", 20);
        ApplyMonospaceFont(failureLabel);
        
        // Find the ScrollContainer content - FIXED node path
        var guessHistoryContent = _guessHistory.GetChild(0) as VBoxContainer;
        if (guessHistoryContent != null)
        {
            guessHistoryContent.AddChild(failureLabel);
        }
        else
        {
            GD.Print("ERROR: Could not find ScrollContainer content in ShowFailureMessage!");
        }
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
        messageLabel.AddThemeFontSizeOverride("normal_font_size", 20);
        ApplyMonospaceFont(messageLabel);
        
        // Find the ScrollContainer content - FIXED node path
        var guessHistoryContent = _guessHistory.GetChild(0) as VBoxContainer;
        if (guessHistoryContent != null)
        {
            guessHistoryContent.AddChild(messageLabel);
        }
        else
        {
            GD.Print("ERROR: Could not find ScrollContainer content in ShowPrematureExitMessage!");
        }
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
        errorLabel.AddThemeFontSizeOverride("normal_font_size", 20);
        ApplyMonospaceFont(errorLabel);
        
        // Find the ScrollContainer content - FIXED node path
        var guessHistoryContent = _guessHistory.GetChild(0) as VBoxContainer;
        if (guessHistoryContent != null)
        {
            guessHistoryContent.AddChild(errorLabel);
        }
        else
        {
            GD.Print("ERROR: Could not find ScrollContainer content in ShowErrorMessage!");
        }
    }

    private void UpdateUI()
    {
        _attemptsLabel.Text = $"Attempts remaining: {_maxAttempts - _currentAttempt}/{_maxAttempts}";

        // Update description label with current attempts
        if (_descriptionLabel != null)
        {
            _descriptionLabel.Text = "Guess the 5-letter code";
        }

        // Update alphabet display
        UpdateAlphabetDisplay();

        // Clear previous history from ScrollContainer content - FIXED node path
        var guessHistoryContent = _guessHistory.GetChild(0) as VBoxContainer;
        if (guessHistoryContent != null)
        {
            foreach (var child in guessHistoryContent.GetChildren())
            {
                child.QueueFree();
            }

            // Rebuild history
            foreach (var guess in _previousGuesses)
            {
                AddGuessToHistory(guess);
            }

            // Add current input display (recreate to maintain reference)
            _currentInputDisplay = new RichTextLabel
            {
                BbcodeEnabled = true,
                Text = GetCurrentInputDisplay(),
                FitContent = true,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            _currentInputDisplay.AddThemeFontSizeOverride("normal_font_size", 24);
            ApplyMonospaceFont(_currentInputDisplay);
            guessHistoryContent.AddChild(_currentInputDisplay);
        }
        else
        {
            GD.Print("ERROR: Could not find ScrollContainer content in UpdateUI!");
        }
    }

    private void OnCollectPressed()
    {
        if (_gameCompleted && !_evidenceCollected)
        {
            // Collect evidence with pre-rolled tier
            string? callerName = _caller?.Name;
            string? evidenceLevel = _caller?.EvidenceLevel.ToString();
            string? callerId = _caller?.Id;

            bool success = TryCollectEvidence(callerName, evidenceLevel, callerId);

            if (success)
            {
                // Evidence collected successfully - close modal
                ModalClosed?.Invoke();
            }
            else
            {
                // Collection failed - show error but keep modal open
                ShowErrorMessage("Failed to collect evidence - try again");
                GD.PrintErr("EvidenceModal: Evidence collection failed in OnCollectPressed");
            }
        }
        else if (_gameCompleted && _evidenceCollected)
        {
            // Evidence already collected - just close modal
            ModalClosed?.Invoke();
        }
    }



    private void EnsureNodesInitialized()
    {
        _titleLabel ??= GetNodeOrNull<Label>("ModalPanel/ContentContainer/HeaderContainer/TitleLabel");
        _wordDisplay ??= GetNodeOrNull<Control>("ModalPanel/ContentContainer/ContentVBox/MainHBoxContainer/WordDisplay");
        _alphabetDisplay ??= GetNodeOrNull<Control>("ModalPanel/ContentContainer/ContentVBox/MainHBoxContainer/RightPanel/AlphabetDisplay");
        _attemptsLabel ??= GetNodeOrNull<Label>("ModalPanel/ContentContainer/ContentVBox/MainHBoxContainer/LeftPanel/AttemptsLabel");
        _collectButton ??= GetNodeOrNull<Button>("ModalPanel/ContentContainer/ContentVBox/FooterHBox/CollectButton");
        _guessHistory ??= GetNodeOrNull<Control>("ModalPanel/ContentContainer/ContentVBox/MainHBoxContainer/LeftPanel/GuessHistory");
        _patienceProgressBar ??= GetNodeOrNull<ProgressBar>("ModalPanel/ContentContainer/HeaderContainer/PatienceHBox/PatienceProgressBar");
        _descriptionLabel ??= GetNodeOrNull<Label>("ModalPanel/ContentContainer/ContentVBox/DescriptionLabel");
    }

    /// <summary>
    /// Roll loot table to determine evidence tier when puzzle is solved.
    /// </summary>
    private EvidenceTier RollEvidenceTier()
    {
        // Get total belief level from topic mastery
        int totalBeliefLevel;
        try
        {
            var topicManager = DependencyInjection.Get<TopicManager>(this);
            totalBeliefLevel = topicManager.GetTotalBeliefLevel();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"EvidenceModal: Failed to get TopicManager - {ex.Message}, using belief level 0");
            totalBeliefLevel = 0;
        }

        _discoveredTier = EvidenceLootTable.RollQuality(totalBeliefLevel);
        GD.Print($"EvidenceModal: Rolled evidence tier {_discoveredTier} for belief level {totalBeliefLevel}");
        return _discoveredTier;
    }

    /// <summary>
    /// Get display color for evidence tier.
    /// </summary>
    private string GetTierColor(EvidenceTier tier)
    {
        return tier switch
        {
            EvidenceTier.Common => "gray",
            EvidenceTier.Uncommon => "green",
            EvidenceTier.Rare => "blue",
            EvidenceTier.VeryRare => "purple",
            EvidenceTier.OneOfAKind => "gold",
            _ => "gray"
        };
    }

    /// <summary>
    /// Get display name for evidence tier.
    /// </summary>
    private string GetTierDisplayName(EvidenceTier tier)
    {
        return tier switch
        {
            EvidenceTier.Common => "Common",
            EvidenceTier.Uncommon => "Uncommon",
            EvidenceTier.Rare => "Rare",
            EvidenceTier.VeryRare => "Very Rare",
            EvidenceTier.OneOfAKind => "One of a Kind",
            _ => "Common"
        };
    }

    /// <summary>
    /// Show discovery message when evidence tier is determined.
    /// </summary>
    private void ShowDiscoveryMessage(EvidenceTier tier)
    {
        string color = GetTierColor(tier);
        string displayName = GetTierDisplayName(tier);
        
        var discoveryLabel = new RichTextLabel
        {
            BbcodeEnabled = true,
            Text = $"[color={color}]Evidence Discovered: {displayName}[/color]",
            FitContent = true,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        discoveryLabel.AddThemeFontSizeOverride("normal_font_size", 20);
        ApplyMonospaceFont(discoveryLabel);
        
        // Find the ScrollContainer content
        var guessHistoryContent = _guessHistory.GetChild(0) as VBoxContainer;
        if (guessHistoryContent != null)
        {
            guessHistoryContent.AddChild(discoveryLabel);
        }
        else
        {
            GD.Print("ERROR: Could not find ScrollContainer content in ShowDiscoveryMessage!");
        }
    }

    public override void _ExitTree()
    {
        if (_caller != null)
        {
            _caller.OnDisconnected -= OnCallerDisconnected;
        }

        if (_collectButton != null)
        {
            _collectButton.Pressed -= OnCollectPressed;
        }

        if (_closeButton != null)
        {
            _closeButton.Pressed -= OnClosePressed;
        }
    }
}

public enum LetterState
{
    Unused,
    CorrectPosition,
    WrongPosition,
    RuledOut
}
