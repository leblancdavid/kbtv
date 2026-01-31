using System;
using Godot;
using KBTV.Core;
using KBTV.Persistence;

namespace KBTV.Managers;

/// <summary>
/// Manages the in-game clock for radio show.
/// Handles time progression during live broadcasts.
/// Converted to AutoInject Provider pattern.
/// </summary>
public partial class TimeManager : Node, ISaveable, ITimeManager,
    IProvide<TimeManager>,
    IDependent
{
    public override void _Notification(int what) => this.Notify(what);

    [Signal] public delegate void TickEventHandler(float delta);
    [Signal] public delegate void ShowEndedEventHandler();
    [Signal] public delegate void ShowEndingWarningEventHandler(float secondsRemaining);
    [Signal] public delegate void RunningChangedEventHandler(bool isRunning);

    public event Action<float> OnTick;
    public event Action OnShowEnded;
    public event Action<float> OnShowEndingWarning;
    public event Action<bool> OnRunningChanged;

    private SaveManager SaveManager => DependencyInjection.Get<SaveManager>(this);

    [Export] private float _showDurationSeconds = 600f; // 10 minutes real-time
    [Export] private float _showDurationHours = 4f;
    [Export] private int _showStartHour = 22;

    // Runtime state (not serialized - always starts fresh)
    private float _elapsedTime = 0f;
    private bool _isRunning = false;

    public float ElapsedTime => _elapsedTime;
    public float ShowDuration => _showDurationSeconds;
    public float Progress => Mathf.Clamp(_elapsedTime / _showDurationSeconds, 0f, 1f);
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Current in-game time as a formatted string (e.g., "10:45 PM").
    /// </summary>
    public string CurrentTimeFormatted => GetFormattedTime();

    /// <summary>
    /// Current in-game hour (0-23).
    /// </summary>
    public float CurrentHour => _showStartHour + (_showDurationHours * Progress);

    TimeManager IProvide<TimeManager>.Value() => this;

    /// <summary>
    /// Set show duration in seconds.
    /// </summary>
    public void SetShowDuration(float seconds)
    {
        _showDurationSeconds = seconds;
        _showDurationHours = seconds / 3600f; // Update hours for consistency
    }

    /// <summary>
    /// Initialize the TimeManager provider.
    /// Called after dependencies are resolved.
    /// </summary>
    public void Initialize()
    {
        _elapsedTime = 0f;
        _isRunning = false;
        
        // Register with SaveManager now that it's available
        SaveManager?.RegisterSaveable(this);
    }

    /// <summary>
    /// Called when all dependencies are resolved.
    /// </summary>
    public void OnResolved()
    {
    }

    /// <summary>
    /// Called when node enters the scene tree and is ready.
    /// </summary>
    public void OnReady()
    {
        // Provide this service to descendants
        this.Provide();
    }

    /// <summary>
    /// Save current show duration.
    /// </summary>
    public void OnBeforeSave(SaveData data)
    {
        data.ShowDurationMinutes = (int)(_showDurationSeconds / 60f);
    }

    /// <summary>
    /// Load show duration from save.
    /// </summary>
    public void OnAfterLoad(SaveData data)
    {
        if (data.ShowDurationMinutes > 0 && data.ShowDurationMinutes <= 20)
        {
            SetShowDuration(data.ShowDurationMinutes * 60f);
        }
    }

    public override void _Process(double delta)
    {
        if (!_isRunning) return;

        float deltaTime = (float)delta;
        _elapsedTime += deltaTime;

        EmitSignal("Tick", deltaTime);
        OnTick?.Invoke(deltaTime);

        // Check for show ending warning (10 seconds remaining)
        if (_elapsedTime >= _showDurationSeconds - 10f && _elapsedTime < _showDurationSeconds - 10f + deltaTime)
        {
            EmitSignal("ShowEndingWarning", 10f);
            OnShowEndingWarning?.Invoke(10f);
        }

        // Check for show end
        if (_elapsedTime >= _showDurationSeconds)
        {
            _elapsedTime = _showDurationSeconds; // Prevent overflow
            _isRunning = false;
            EmitSignal("ShowEnded");
            OnShowEnded?.Invoke();
        }
    }

    /// <summary>
    /// Start the in-game clock.
    /// </summary>
    public void StartClock()
    {
        if (_isRunning) return;

        _elapsedTime = 0f;
        _isRunning = true;
        EmitSignal("RunningChanged", true);
        OnRunningChanged?.Invoke(true);
    }

    /// <summary>
    /// Stop the in-game clock.
    /// </summary>
    public void StopClock()
    {
        if (!_isRunning) return;

        _isRunning = false;
        EmitSignal("RunningChanged", false);
        OnRunningChanged?.Invoke(false);
    }

    /// <summary>
    /// Pause the in-game clock without resetting.
    /// </summary>
    public void PauseClock()
    {
        if (!_isRunning) return;
        _isRunning = false;
        EmitSignal("RunningChanged", false);
        OnRunningChanged?.Invoke(false);
    }

    /// <summary>
    /// End the show immediately.
    /// </summary>
    public void EndShow()
    {
        _elapsedTime = _showDurationSeconds;
        _isRunning = false;
        EmitSignal("ShowEnded");
        OnShowEnded?.Invoke();
        EmitSignal("RunningChanged", false);
        OnRunningChanged?.Invoke(false);
    }

    /// <summary>
    /// Reset the clock to initial state.
    /// </summary>
    public void ResetClock()
    {
        _elapsedTime = 0f;
        _isRunning = false;
        EmitSignal("RunningChanged", false);
        OnRunningChanged?.Invoke(false);
    }

    /// <summary>
    /// Remaining time in seconds until show ends.
    /// </summary>
    public float RemainingTime => Mathf.Max(0f, _showDurationSeconds - _elapsedTime);

    /// <summary>
    /// Remaining time formatted as MM:SS.
    /// </summary>
    public string RemainingTimeFormatted
    {
        get
        {
            int minutes = (int)(RemainingTime / 60f);
            int seconds = (int)(RemainingTime % 60f);
            return $"{minutes:D2}:{seconds:D2}";
        }
    }

    /// <summary>
    /// Get formatted time string from elapsed hours.
    /// </summary>
    private string GetFormattedTime()
    {
        float currentHour = CurrentHour;
        int displayHour = (int)currentHour % 24;
        int displayMinutes = (int)((currentHour % 1) * 60);

        if (displayHour >= 12)
        {
            return $"{displayHour - 12:D2}:{displayMinutes:D2} PM";
        }
        else
        {
            return $"{displayHour:D2}:{displayMinutes:D2} AM";
        }
    }
}