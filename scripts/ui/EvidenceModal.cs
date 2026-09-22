using Godot;
using System;
using System.Linq;
using Chickensoft.GoDotTest;
using System.Collections.Generic;
using KBTV.Callers;
using KBTV.UI;
using KBTV.UI.Themes;
using KBTV.Managers;
using KBTV.Screening;
using KBTV.Core;
using KBTV.Items;
using KBTV.Persistence;

namespace KBTV.UI;

/// <summary>
/// Evidence decryption dialog. Hosted on the CRT terminal above the screening
/// UI (see ModalManager.SetModalHost) with a fullscreen overlay fallback.
/// The player cracks a 5-character password to unlock the caller's evidence.
/// </summary>
public partial class EvidenceModal : Control
{
    // Event for when modal is closed
    public event Action? ModalClosed;

    [Export]
    private Label _titleLabel = null!;

    [Export]
    private Button _collectButton = null!;

    [Export]
    private Control _guessHistory = null!;

    [Export]
    private Button _closeButton = null!;

    private Label? _attemptsLabel;
    private Control? _alphabetDisplay;
    private HBoxContainer _inputRow = null!;
    private Label _descriptionLabel = null!;
    private ProgressBar? _patienceProgressBar;
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
    private bool _crtHosted;

    // Cached caller reference and patience tracking
    private Caller? _caller;

    // Decryption theme colors
    private static readonly Color CorrectColor = new(0.2f, 0.9f, 0.2f);
    private static readonly Color WrongPosColor = new(0.9f, 0.9f, 0.2f);
    private static readonly Color RuledOutColor = new(0.85f, 0.3f, 0.25f);
    private static readonly Color UnusedColor = new(0.75f, 0.85f, 0.78f);
    private static readonly Color BlockedColor = new(0.35f, 0.4f, 0.37f);

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
        if (caller != null)
        {
            _caller.OnDisconnected += OnCallerDisconnected;
        }

        // Load word list if not already loaded
        LoadWordList();
    }

    /// <summary>
    /// True when the dialog lives inside the CRT's SubViewport. In that mode the
    /// main viewport's ModalManager forwards key events via HandleKey.
    /// </summary>
    public bool IsCrtHosted() => _crtHosted && IsInsideTree();

    /// <summary>
    /// Force-close as an aborted decryption: forfeits the evidence opportunity.
    /// Called when the player leaves the terminal mid-game.
    /// </summary>
    public void Abort()
    {
        _screeningController?.LoseEvidenceOpportunity();
        ModalClosed?.Invoke();
    }

    /// <summary>
    /// Handle caller disconnection event.
    /// </summary>
    private void OnCallerDisconnected()
    {
        ModalClosed?.Invoke();
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
        }
        catch (InvalidOperationException ex)
        {
            GD.PrintErr($"EvidenceModal: Failed to resolve IScreeningController - {ex.Message}");
            _screeningController = null;
        }
    }

    /// <summary>
    /// Applies monospace font to a control for terminal-like display.
    /// </summary>
    private static void ApplyMonospaceFont(Control control)
    {
        control.AddThemeFontOverride("font", UITheme.MonoFont);
    }

    public override void _Ready()
    {
        // Keyboard comes via ModalManager._Input when hosted in the CRT SubViewport;
        // only handle it locally when parented to the root window's CanvasLayer.
        _crtHosted = GetViewport() != GetTree().Root;

        FocusMode = FocusModeEnum.All;
        MouseFilter = MouseFilterEnum.Stop;
        // No ZIndex: when hosted in the CRT viewport it would lift the dialog
        // above the scanline/tint/vignette layers instead of behind them.

        EnsureNodesInitialized();
        SetupModal();
        StartNewGame();

        ResolveDependencies();
    }

    private void SetupModal()
    {
        ApplyThemeToLabels();
        InitializeLetterStates();

        _collectButton.Pressed += OnCollectPressed;
        _collectButton.Disabled = true;
        _collectButton.Text = "Extract Evidence";

        SetCallerHeader();

        BuildInputRow();
        UpdateAlphabetDisplay();

        if (_closeButton == null)
        {
            SetupCloseButton();
        }
        else
        {
            _closeButton.Pressed += OnClosePressed;
        }
    }

    /// <summary>
    /// Apply DOS-terminal styling consistent with the screening UI.
    /// </summary>
    private void ApplyThemeToLabels()
    {
        if (_titleLabel != null)
        {
            _titleLabel.AddThemeFontOverride("font", UITheme.MonoFont);
            _titleLabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_MEDIUM);
            _titleLabel.AddThemeColorOverride("font_color", UIColors.Screening.HeaderText);
        }

        if (_attemptsLabel != null)
        {
            _attemptsLabel.AddThemeFontOverride("font", UITheme.MonoFont);
            _attemptsLabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_SMALL);
            _attemptsLabel.AddThemeColorOverride("font_color", UIColors.Screening.DimText);
        }

        if (_descriptionLabel != null)
        {
            _descriptionLabel.AddThemeFontOverride("font", UITheme.MonoFont);
            _descriptionLabel.AddThemeFontSizeOverride("font_size", UITheme.FONT_SMALL);
            _descriptionLabel.AddThemeColorOverride("font_color", UIColors.Screening.DefaultText);
        }

        if (_collectButton != null)
        {
            UITheme.ApplyButtonStyle(_collectButton);
        }
    }

    /// <summary>
    /// Wire the patience header (caller name + bar) when present in the scene.
    /// </summary>
    private void SetCallerHeader()
    {
        if (_patienceProgressBar != null && _caller != null)
        {
            _patienceProgressBar.MaxValue = _caller.ScreeningPatience;
            _patienceProgressBar.Value = _caller.ScreeningPatience;
        }
    }

    /// <summary>
    /// Sets up the close button in the top-right corner of the dialog.
    /// </summary>
    private void SetupCloseButton()
    {
        var headerContainer = GetNodeOrNull<HBoxContainer>("ModalPanel/ContentContainer/HeaderContainer");
        if (headerContainer == null)
        {
            GD.PrintErr("EvidenceModal: Cannot find HeaderContainer for close button");
            return;
        }

        _closeButton = new Button
        {
            Text = "X",
            CustomMinimumSize = new Vector2(24, 22),
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
        };
        ApplyDosButtonStyle(_closeButton, UIColors.Screening.DefaultText);
        _closeButton.AddThemeColorOverride("font_hover_color", RuledOutColor);
        _closeButton.Pressed += OnClosePressed;
        headerContainer.AddChild(_closeButton);
    }

    /// <summary>
    /// Flat black + 1px border style matching the screening terminal panels.
    /// </summary>
    private static void ApplyDosButtonStyle(Button button, Color accent)
    {
        button.Flat = true;
        button.AddThemeFontOverride("font", UITheme.MonoFont);
        button.AddThemeFontSizeOverride("font_size", UITheme.FONT_SMALL);
        button.AddThemeColorOverride("font_color", accent);

        var bg = UIColors.Screening.Background;
        ApplyDosButtonState(button, "normal", bg, new Color(accent.R, accent.G, accent.B, 0.45f));
        ApplyDosButtonState(button, "hover", new Color(0.08f, 0.12f, 0.1f, 1f), accent);
        ApplyDosButtonState(button, "pressed", new Color(accent.R * 0.25f, accent.G * 0.25f, accent.B * 0.25f, 1f), accent);
        ApplyDosButtonState(button, "disabled", bg, new Color(0.25f, 0.28f, 0.26f, 0.6f));

        button.AddThemeColorOverride("font_hover_color", new Color(
            Mathf.Min(1f, accent.R + 0.1f), Mathf.Min(1f, accent.G + 0.1f), Mathf.Min(1f, accent.B + 0.1f)));
        button.AddThemeColorOverride("font_pressed_color", UIColors.Screening.HeaderText);
        button.AddThemeColorOverride("font_disabled_color", BlockedColor);
    }

    private static void ApplyDosButtonState(Button button, string state, Color bg, Color border)
    {
        var style = new StyleBoxFlat
        {
            BgColor = bg,
            BorderColor = border,
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            ContentMarginLeft = 4,
            ContentMarginRight = 4,
            ContentMarginTop = 2,
            ContentMarginBottom = 2
        };
        button.AddThemeStyleboxOverride(state, style);
    }

    private void OnClosePressed()
    {
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

    /// <summary>
    /// Build the clickable alphabet board. A letter stays enabled while it could
    /// still be in the password; only ruled-out (red) letters are disabled.
    /// Clicking an enabled letter types it.
    /// </summary>
    private void UpdateAlphabetDisplay()
    {
        if (_alphabetDisplay == null)
        {
            return;
        }

        // Detach + free so stale buttons never receive input this frame.
        foreach (var child in _alphabetDisplay.GetChildren().ToList())
        {
            _alphabetDisplay.RemoveChild(child);
            child.QueueFree();
        }

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
            rowContainer.AddThemeConstantOverride("separation", 4);

            foreach (var letter in row)
            {
                var state = _letterStates[letter];
                var enabled = state != LetterState.RuledOut && !_gameCompleted;
                var color = state switch
                {
                    LetterState.CorrectPosition => CorrectColor,
                    LetterState.WrongPosition => WrongPosColor,
                    LetterState.RuledOut => RuledOutColor,
                    _ => UnusedColor
                };

                var letterButton = new Button
                {
                    Text = letter.ToString(),
                    CustomMinimumSize = new Vector2(28, 22),
                    Disabled = !enabled,
                    SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
                };
                ApplyDosButtonStyle(letterButton, color);

                var captured = letter;
                letterButton.Pressed += () => TryTypeLetter(captured);
                rowContainer.AddChild(letterButton);
            }

            _alphabetDisplay.AddChild(rowContainer);
        }
    }

    private void StartNewGame()
    {
        LoadWordList();

        if (_wordList.Count > 0)
        {
            _targetWord = _wordList[(int)(GD.Randi() % (uint)_wordList.Count)];
        }
        else
        {
            _targetWord = "HOUSE";
            GD.PrintErr("EvidenceModal: Word list empty, using hardcoded fallback");
        }

        _currentAttempt = 0;
        _previousGuesses.Clear();
        _gameCompleted = false;
        _currentInputChars = new char[] { '_', '_', '_', '_', '_' };
        _positionsFilled = new bool[5];

        InitializeLetterStates();

        UpdateUI();

        _collectButton.Disabled = true;
        _collectButton.Text = "Extract Evidence";
    }

    public override void _Process(double delta)
    {
        if (_caller != null && !_gameCompleted)
        {
            if (_patienceProgressBar != null && IsInstanceValid(_patienceProgressBar))
            {
                var progress = _screeningController?.Progress;
                if (progress != null)
                {
                    _patienceProgressBar.Value = _caller.ScreeningPatience - progress.ElapsedTime;
                }
                else
                {
                    _patienceProgressBar.Value = _caller.ScreeningPatience;
                }
            }

            if (_caller.ScreeningPatience <= 0f)
            {
                ModalClosed?.Invoke();
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (_crtHosted)
        {
            // Forwarded through ModalManager instead; avoid double handling.
            return;
        }

        if (@event is InputEventKey key && HandleKey(key))
        {
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>
    /// Handle a key event. Returns true when consumed. Used both by local _Input
    /// (fullscreen fallback) and ModalManager forwarding (CRT hosted mode).
    /// </summary>
    public bool HandleKey(InputEventKey key)
    {
        if (_gameCompleted || !key.Pressed)
        {
            return false;
        }

        if (key.Keycode == Key.Enter)
        {
            if (IsCompleteGuess())
            {
                MakeGuess(GetCurrentGuess());
            }
            return true;
        }

        if (key.Keycode == Key.Backspace)
        {
            TryClearLastTyped();
            return true;
        }

        if (key.Unicode != 0 && char.IsLetter((char)key.Unicode))
        {
            TryTypeLetter(char.ToUpper((char)key.Unicode));
            return true;
        }

        return false;
    }

    /// <summary>
    /// Type a letter into the first open (unlocked, empty) slot. Only letters
    /// ruled out by a guess (red) are locked out, matching the alphabet board;
    /// green/yellow letters can be reused (needed for repeated letters).
    /// </summary>
    private void TryTypeLetter(char letter)
    {
        if (_gameCompleted ||
            !_letterStates.TryGetValue(letter, out var state) ||
            state == LetterState.RuledOut)
        {
            return;
        }

        for (int i = 0; i < 5; i++)
        {
            if (!_positionsFilled[i] && _currentInputChars[i] == '_')
            {
                _currentInputChars[i] = letter;
                RebuildInputRow();
                return;
            }
        }
    }

    /// <summary>
    /// Backspace: clear the last typed (not yet locked) letter.
    /// </summary>
    private void TryClearLastTyped()
    {
        for (int i = 4; i >= 0; i--)
        {
            if (!_positionsFilled[i] && _currentInputChars[i] != '_')
            {
                _currentInputChars[i] = '_';
                RebuildInputRow();
                return;
            }
        }
    }

    /// <summary>
    /// Click a typed, unlocked slot to clear it.
    /// </summary>
    private void TryClearSlot(int index)
    {
        if (index < 0 || index >= 5 || _positionsFilled[index])
        {
            return;
        }

        _currentInputChars[index] = '_';
        RebuildInputRow();
    }

    private void MakeGuess(string guess)
    {
        if (_gameCompleted || !IsCompleteGuess())
        {
            return;
        }

        if (!IsValidGuess(guess))
        {
            return;
        }

        _currentAttempt++;
        _previousGuesses.Add(guess);

        UpdateLetterStates(guess);

        if (guess == _targetWord)
        {
            OnGameWon();
        }
        else if (_currentAttempt >= _maxAttempts)
        {
            OnGameLost();
        }
        else
        {
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
                    if (_letterStates.ContainsKey(guessChars[i]) && _letterStates[guessChars[i]] == LetterState.Unused)
                    {
                        _letterStates[guessChars[i]] = LetterState.RuledOut;
                    }
                }
            }
        }
    }

    private bool IsValidGuess(string guess)
    {
        if (guess.Length != 5)
            return false;

        foreach (char c in guess)
        {
            if (!char.IsLetter(c))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Build the live password row: PWD> [slot][slot][slot][slot][slot] [ENTER].
    /// Slots are clickable: a typed, unlocked slot clears on click; locked
    /// (correct) slots and empty slots are not interactive.
    /// </summary>
    private void BuildInputRow()
    {
        var guessHistoryContent = _guessHistory.GetChild(0) as VBoxContainer;
        if (guessHistoryContent == null)
        {
            GD.PrintErr("EvidenceModal: Could not find GuessHistory content");
            return;
        }

        _inputRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        _inputRow.AddThemeConstantOverride("separation", 3);
        guessHistoryContent.AddChild(_inputRow);
        RebuildInputRow();
    }

    private void RebuildInputRow()
    {
        if (_inputRow == null || !IsInstanceValid(_inputRow))
        {
            return;
        }

        foreach (var child in _inputRow.GetChildren())
        {
            child.QueueFree();
        }

        var prefix = new Label { Text = "PWD>" };
        prefix.AddThemeFontOverride("font", UITheme.MonoFont);
        prefix.AddThemeFontSizeOverride("font_size", UITheme.FONT_BASE);
        prefix.AddThemeColorOverride("font_color", UIColors.Screening.DimText);
        _inputRow.AddChild(prefix);

        for (int i = 0; i < 5; i++)
        {
            bool locked = _positionsFilled[i] && _currentInputChars[i] != '_';
            bool filled = _currentInputChars[i] != '_';
            var slot = new Button
            {
                Text = filled ? _currentInputChars[i].ToString() : "_",
                CustomMinimumSize = new Vector2(26, 22),
                Disabled = locked || !filled,
                SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
            };
            ApplyDosButtonStyle(slot, locked ? CorrectColor : UIColors.Screening.DefaultText);
            if (!locked && filled)
            {
                var captured = i;
                slot.Pressed += () => TryClearSlot(captured);
            }
            _inputRow.AddChild(slot);
        }

        var enterButton = new Button
        {
            Text = "ENTER",
            CustomMinimumSize = new Vector2(44, 22),
            Disabled = _gameCompleted || !IsCompleteGuess(),
            SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
        };
        ApplyDosButtonStyle(enterButton, WrongPosColor);
        enterButton.Pressed += () =>
        {
            if (IsCompleteGuess())
            {
                MakeGuess(GetCurrentGuess());
            }
        };
        _inputRow.AddChild(enterButton);
    }

    private string EvaluateGuess(string guess)
    {
        var result = new string[5];
        var targetChars = _targetWord.ToCharArray();
        var guessChars = guess.ToCharArray();

        for (int i = 0; i < 5; i++)
        {
            if (guessChars[i] == targetChars[i])
            {
                result[i] = $"[color={ColorHtml(CorrectColor)}]{guessChars[i]}[/color]";
                targetChars[i] = '\0';
                guessChars[i] = '\0';
            }
        }

        for (int i = 0; i < 5; i++)
        {
            if (guessChars[i] != '\0')
            {
                bool found = false;
                for (int j = 0; j < 5; j++)
                {
                    if (guessChars[i] == targetChars[j])
                    {
                        result[i] = $"[color={ColorHtml(WrongPosColor)}]{guessChars[i]}[/color]";
                        targetChars[j] = '\0';
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    result[i] = $"[color={ColorHtml(RuledOutColor)}]{guessChars[i]}[/color]";
                }
            }
        }

        return $"> {string.Join(" ", result)}";
    }

    private static string ColorHtml(Color color) => "#" + color.ToHtml(false);

    private void OnGameWon()
    {
        _gameCompleted = true;
        _evidenceCollected = false;

        _screeningController?.ResetPatienceAndTime();

        if (_patienceProgressBar != null)
        {
            _patienceProgressBar.Value = _patienceProgressBar.MaxValue;
        }

        _discoveredTier = RollEvidenceTier();

        UpdateUI();
        AddRichMessage("[color=" + ColorHtml(CorrectColor) + "]PASSWORD ACCEPTED - FILE UNLOCKED[/color]");
        ShowDiscoveryMessage(_discoveredTier);

        _collectButton.Disabled = false;
        _collectButton.Text = "Extract Evidence";
    }

    /// <summary>
    /// Attempt to collect evidence. Tries via screening controller first,
    /// falls back to direct creation if caller is no longer current.
    /// </summary>
    private bool TryCollectEvidence(string? callerName, string? evidenceLevel, string? callerId)
    {
        if (string.IsNullOrEmpty(callerName) || string.IsNullOrEmpty(callerId))
        {
            GD.PrintErr("EvidenceModal: Cannot collect evidence - no cached caller data");
            return false;
        }

        if (_screeningController != null)
        {
            var currentCaller = _screeningController.CurrentCaller;

            if (currentCaller != null && currentCaller.Id == callerId)
            {
                bool success = _screeningController.CollectEvidence(_targetWord, _discoveredTier);
                if (success)
                {
                    return true;
                }
                GD.Print("EvidenceModal: Normal collection failed, trying direct creation");
            }
        }

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
            if (string.IsNullOrEmpty(callerName) || string.IsNullOrEmpty(_targetWord))
            {
                GD.PrintErr("EvidenceModal: Cannot create evidence - missing caller or word");
                return false;
            }

            evidenceLevel ??= "None";

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

            if (saveManager?.CurrentSave == null)
            {
                GD.PrintErr("EvidenceModal: SaveManager/CurrentSave unavailable");
                return false;
            }

            saveManager.CurrentSave.CollectedEvidence ??= new System.Collections.Generic.List<Items.EvidenceItem>();
            saveManager.CurrentSave.CollectedEvidence.Add(evidence);

            saveManager.CurrentSave.EvidenceSystem ??= new Persistence.EvidenceSystemData();
            saveManager.CurrentSave.EvidenceSystem.RawEvidence ??= new System.Collections.Generic.List<Persistence.IdentifiedEvidenceData>();

            saveManager.CurrentSave.EvidenceSystem.RawEvidence.Add(new Persistence.IdentifiedEvidenceData
            {
                Word = evidence.Word,
                SourceCallerName = evidence.SourceCallerName,
                EvidenceLevel = evidence.EvidenceLevel,
                Tier = (int)evidence.Tier,
                BonusType = 0,
                BonusAmount = 0f,
                Status = 0 // EvidenceStatus.Raw
            });

            saveManager.Save();

            _evidenceCollected = true;
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"EvidenceModal: Failed to create evidence directly - {ex.Message}");
            return false;
        }
    }

    private void OnGameLost()
    {
        _gameCompleted = true;
        UpdateUI();
        AddRichMessage("[color=" + ColorHtml(RuledOutColor) + "]DECRYPTION FAILED - FILE SEALED. EVIDENCE LOST.[/color]");

        _collectButton.Disabled = false;
        _collectButton.Text = "Dismiss";

        _screeningController?.LoseEvidenceOpportunity();
    }

    /// <summary>
    /// Append a colored status line to the history, above the bottom edge.
    /// </summary>
    private void AddRichMessage(string bbcode)
    {
        var label = new RichTextLabel
        {
            BbcodeEnabled = true,
            Text = bbcode,
            FitContent = true,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeFontSizeOverride("normal_font_size", UITheme.FONT_SMALL);
        ApplyMonospaceFont(label);

        var guessHistoryContent = _guessHistory.GetChild(0) as VBoxContainer;
        guessHistoryContent?.AddChild(label);
    }

    private void ShowErrorMessage(string message)
    {
        AddRichMessage($"[color={ColorHtml(RuledOutColor)}]WARNING: {message}[/color]");
    }

    private void UpdateUI()
    {
        if (_attemptsLabel != null)
        {
            _attemptsLabel.Text = $"DECRYPTION ATTEMPTS REMAINING: {_maxAttempts - _currentAttempt}/{_maxAttempts}";
        }

        if (_descriptionLabel != null)
        {
            _descriptionLabel.Text = "Crack the 5-character password to unlock this caller's evidence file.";
        }

        UpdateAlphabetDisplay();

        // Rebuild history above the live input row
        var guessHistoryContent = _guessHistory.GetChild(0) as VBoxContainer;
        if (guessHistoryContent == null)
        {
            GD.PrintErr("EvidenceModal: Could not find GuessHistory content in UpdateUI");
            return;
        }

        foreach (var child in guessHistoryContent.GetChildren().ToList())
        {
            if (child != _inputRow)
            {
                guessHistoryContent.RemoveChild(child);
                child.QueueFree();
            }
        }

        foreach (var guess in _previousGuesses)
        {
            var guessResult = EvaluateGuess(guess);
            var guessLabel = new RichTextLabel
            {
                BbcodeEnabled = true,
                Text = guessResult,
                FitContent = true,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            guessLabel.AddThemeFontSizeOverride("normal_font_size", UITheme.FONT_SMALL);
            ApplyMonospaceFont(guessLabel);
            guessHistoryContent.AddChild(guessLabel);
        }

        guessHistoryContent.MoveChild(_inputRow, guessHistoryContent.GetChildCount() - 1);
        RebuildInputRow();
    }

    private void OnCollectPressed()
    {
        if (_gameCompleted && !_evidenceCollected)
        {
            string? callerName = _caller?.Name;
            string? evidenceLevel = _caller?.EvidenceLevel.ToString();
            string? callerId = _caller?.Id;

            bool success = TryCollectEvidence(callerName, evidenceLevel, callerId);

            if (success)
            {
                ModalClosed?.Invoke();
            }
            else
            {
                ShowErrorMessage("Extraction failed - try again");
            }
        }
        else if (_gameCompleted && _evidenceCollected)
        {
            ModalClosed?.Invoke();
        }
    }

    private void EnsureNodesInitialized()
    {
        _titleLabel ??= GetNodeOrNull<Label>("ModalPanel/ContentContainer/HeaderContainer/TitleLabel");
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
        return _discoveredTier;
    }

    /// <summary>
    /// Get display color for evidence tier.
    /// </summary>
    private string GetTierColor(EvidenceTier tier)
    {
        return tier switch
        {
            EvidenceTier.Common => "9a9a9a",
            EvidenceTier.Uncommon => "38e838",
            EvidenceTier.Rare => "3878e8",
            EvidenceTier.VeryRare => "a040e0",
            EvidenceTier.OneOfAKind => "ffd700",
            _ => "9a9a9a"
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
        AddRichMessage($"[color=#{GetTierColor(tier)}]EVIDENCE DECRYPTED: {GetTierDisplayName(tier)}[/color]");
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
