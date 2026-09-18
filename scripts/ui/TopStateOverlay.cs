#nullable enable

using System;
using Godot;
using KBTV.Ads;
using KBTV.Callers;
using KBTV.Core;
using KBTV.Dialogue;
using KBTV.Economy;
using KBTV.Managers;
using KBTV.Screening;
using KBTV.UI.Themes;

namespace KBTV.UI
{
    /// <summary>
    /// Thin top-of-screen HUD strip shown during the live show. Displays:
    /// show time remaining with next ad-break countdown, a single-line status feed
    /// cycling through events from the last minute, listener count with trend arrows,
    /// and money.
    /// </summary>
    public partial class TopStateOverlay : Control, IDependent
    {
        /// <summary>HUD bar height; nav/soundboard overlays offset their top controls below this.</summary>
        public const float BarHeight = 27f;
        private const float TrendSampleInterval = 0.5f;
        private const float BreakCueWindow = 20f;
        private const float LowTimeThreshold = 20f;
        private const float MoneyFlashDuration = 1.25f;
        private const float PodMinWidth = 90f;
        private const float FeedMinWidth = 220f;
        private const float TrendArrowMinSize = 18f;
        private const float ContentMargin = 12f;
        private const float FeedWindowSeconds = 60f;
        private const float FeedCycleInterval = 5f;

        private const int StatFontSize = 14;
        private const int FeedFontSize = 14;

        // Services (resolved in OnResolved)
        private TimeManager? _timeManager;
        private AdManager? _adManager;
        private ListenerManager? _listenerManager;
        private EconomyManager? _economyManager;
        private IScreeningController? _screeningController;
        private ICallerRepository? _repository;
        private EventBus? _eventBus;

        // UI
        private Label? _timeValue;
        private Label? _breakValue;
        private Label? _listenersValue;
        private Label? _moneyValue;
        private TrendArrow? _trendArrow;
        private Control? _feedHost;
        private Label? _feedLabel;
        private Panel? _bar;
        private HBoxContainer? _content;

        // State
        private readonly StatusFeedModel _feed = new();
        private readonly ListenerTrendTracker _trend = new();
        private string _lastFeedSignature = "";
        private string _lastFeedTopSignature = "";
        private int _feedCycleIndex;
        private float _feedCycleAccum;
        private bool _lastEvidenceAvailable;
        private bool _moneyInitialized;
        private int _lastMoney;
        private bool _moneyFlashPositive;
        private float _moneyFlashTime;
        private float _pulseAccum;
        private float _sampleAccum;
        private float _cuePulseTime;
        private int _lastTrendDirection = ListenerTrendTracker.Flat;

        public override void _Notification(int what) => this.Notify(what);

        public override void _Ready()
        {
            Name = "TopStateOverlay";
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            MouseFilter = MouseFilterEnum.Ignore;
            BuildUi();
        }

        public void OnResolved()
        {
            _timeManager = DependencyInjection.Get<TimeManager>(this);
            _adManager = DependencyInjection.Get<AdManager>(this);
            _listenerManager = DependencyInjection.Get<ListenerManager>(this);
            _economyManager = DependencyInjection.Get<EconomyManager>(this);
            _screeningController = DependencyInjection.Get<IScreeningController>(this);
            _repository = DependencyInjection.Get<ICallerRepository>(this);
            _eventBus = DependencyInjection.Get<EventBus>(this);

            if (_eventBus != null)
            {
                _eventBus.Subscribe<BroadcastInterruptionEvent>(OnBroadcastInterruption);
                _eventBus.Subscribe<CursingTimerCompletedEvent>(OnCursingTimer);
                _eventBus.Subscribe<BroadcastTimingEvent>(OnBroadcastTiming);
            }

            if (_screeningController != null)
            {
                _screeningController.EvidenceStored += OnEvidenceStored;
            }
        }

        public override void _ExitTree()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<BroadcastInterruptionEvent>(OnBroadcastInterruption);
                _eventBus.Unsubscribe<CursingTimerCompletedEvent>(OnCursingTimer);
                _eventBus.Unsubscribe<BroadcastTimingEvent>(OnBroadcastTiming);
            }

            if (_screeningController != null)
            {
                _screeningController.EvidenceStored -= OnEvidenceStored;
            }
        }

        public override void _Process(double delta)
        {
            float dt = (float)delta;
            _pulseAccum += dt;
            _sampleAccum += dt;
            _moneyFlashTime = Mathf.Max(0f, _moneyFlashTime - dt);
            _cuePulseTime = Mathf.Max(0f, _cuePulseTime - dt);

            if (!IsVisibleInTree())
            {
                return;
            }

            SyncToViewport();
            UpdateTimeAndBreak();
            UpdateListeners();
            UpdateMoney();
            UpdateEvidence();
            UpdateFeedCycle(dt);
            UpdateFeedDisplay();
        }

        /// <summary>
        /// Re-sync the bar geometry to the live viewport every visible frame (same
        /// pattern as TranscriptOverlay/ScreenNavOverlay). The window resizes to
        /// borderless fullscreen after boot, and Controls under a CanvasLayer may
        /// keep the stale anchor rect from the pre-resize viewport; explicit sizing
        /// guarantees the bar hugs the true top edge at full window width.
        /// </summary>
        private void SyncToViewport()
        {
            var viewport = GetViewport();
            if (viewport == null || _bar == null || _content == null)
            {
                return;
            }

            var vp = viewport.GetVisibleRect().Size;
            if (vp.X <= 0f || vp.Y <= 0f)
            {
                return;
            }

            Size = vp;
            _bar.Position = Vector2.Zero;
            _bar.Size = new Vector2(vp.X, BarHeight);
            _content.Position = new Vector2(ContentMargin, 2f);
            _content.Size = new Vector2(Mathf.Max(0f, vp.X - ContentMargin * 2f), BarHeight - 4f);
        }

        // ───────────── UI construction ─────────────

        private void BuildUi()
        {
            var bar = new Panel { Name = "Bar" };
            bar.SetAnchorsPreset(LayoutPreset.TopWide);
            bar.OffsetTop = 0;
            bar.OffsetBottom = BarHeight;
            bar.MouseFilter = MouseFilterEnum.Ignore;
            _bar = bar;

            var style = new StyleBoxFlat
            {
                BgColor = new Color(0.03f, 0.03f, 0.03f, 0.88f)
            };
            style.SetBorderWidthAll(0);
            style.BorderWidthBottom = 2;
            style.BorderColor = UIColors.BG_BORDER;
            style.ContentMarginLeft = 12;
            style.ContentMarginRight = 12;
            style.ContentMarginTop = 2;
            style.ContentMarginBottom = 2;
            bar.AddThemeStyleboxOverride("panel", style);
            AddChild(bar);

            var content = new HBoxContainer { Name = "BarContent" };
            content.MouseFilter = MouseFilterEnum.Ignore;
            // Fill the full bar width so the pods spread evenly across the screen.
            // SyncToViewport() re-applies this explicitly each visible frame.
            content.SetAnchorsPreset(LayoutPreset.FullRect);
            content.OffsetLeft = ContentMargin;
            content.OffsetRight = -ContentMargin;
            content.OffsetTop = 2;
            content.OffsetBottom = -2;
            content.GrowHorizontal = GrowDirection.Both;
            content.GrowVertical = GrowDirection.Both;
            bar.AddChild(content);
            _content = content;

            // Each pod shares the width evenly and centers its content.
            // The on-air clock and break countdown travel together in one pod.
            var clockPair = new HBoxContainer
            {
                Name = "ClockPair",
                MouseFilter = MouseFilterEnum.Ignore,
            };
            clockPair.AddThemeConstantOverride("separation", 16);

            _timeValue = MakeLabel("ON-AIR: --:--", StatFontSize, UIColors.TEXT_PRIMARY);
            clockPair.AddChild(_timeValue);

            _breakValue = MakeLabel("BREAK --:--", StatFontSize, UIColors.TEXT_SECONDARY);
            clockPair.AddChild(_breakValue);

            content.AddChild(MakePod(clockPair));

            _feedHost = new Control
            {
                Name = "FeedHost",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.Fill,
                ClipContents = true,
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(FeedMinWidth, 0)
            };
            content.AddChild(_feedHost);

            _feedLabel = MakeLabel("", FeedFontSize, UIColors.TEXT_SECONDARY);
            _feedLabel.Name = "FeedLabel";
            _feedLabel.SetAnchorsPreset(LayoutPreset.FullRect);
            _feedHost.AddChild(_feedLabel);

            var listenerPair = new HBoxContainer
            {
                Name = "ListenerPair",
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(0, TrendArrowMinSize)
            };
            listenerPair.AddThemeConstantOverride("separation", 4);

            _listenersValue = MakeLabel("Listeners: 0", StatFontSize, UIColors.TEXT_PRIMARY);
            listenerPair.AddChild(_listenersValue);

            _trendArrow = new TrendArrow
            {
                Name = "TrendArrow",
                CustomMinimumSize = new Vector2(TrendArrowMinSize, TrendArrowMinSize),
                MouseFilter = MouseFilterEnum.Ignore
            };
            listenerPair.AddChild(_trendArrow);

            content.AddChild(MakePod(listenerPair));

            _moneyValue = MakeLabel("Bank: $0", StatFontSize, UIColors.TEXT_PRIMARY);
            content.AddChild(MakePod(_moneyValue));
        }

        /// <summary>
        /// Wrap a widget in a centered pod that expands to fill an equal share of the bar.
        /// </summary>
        private static CenterContainer MakePod(Control child)
        {
            var pod = new CenterContainer
            {
                Name = $"Pod_{child.Name}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Ignore,
                CustomMinimumSize = new Vector2(PodMinWidth, 0)
            };
            pod.AddChild(child);
            return pod;
        }

        private static Label MakeLabel(string text, int fontSize, Color color)
        {
            var label = new Label
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SizeFlagsVertical = Control.SizeFlags.Fill,
                MouseFilter = MouseFilterEnum.Ignore
            };
            label.AddThemeFontSizeOverride("font_size", fontSize);
            label.AddThemeColorOverride("font_color", color);
            return label;
        }

        private static void SetLabel(Label label, string text, Color color)
        {
            label.Text = text;
            label.AddThemeColorOverride("font_color", color);
        }

        // ───────────── Per-frame updates ─────────────

        private void UpdateTimeAndBreak()
        {
            if (_timeManager == null || _adManager == null || _timeValue == null || _breakValue == null)
            {
                return;
            }

            _timeValue.Text = $"ON-AIR: {_timeManager.RemainingTimeFormatted}";
            bool lowTime = _timeManager.RemainingTime <= LowTimeThreshold;
            _timeValue.AddThemeColorOverride("font_color",
                lowTime && BlinkOn() ? UIColors.Warning.Critical : UIColors.TEXT_PRIMARY);

            if (!_adManager.IsInitialized || _adManager.BreaksRemaining <= 0)
            {
                SetLabel(_breakValue, "BREAK --:--", UIColors.TEXT_DISABLED);
                return;
            }

            if (_adManager.IsAdBreakActive)
            {
                SetLabel(_breakValue, "AD BREAK", BlinkOn() ? UIColors.Warning.Caution : UIColors.TEXT_SECONDARY);
                return;
            }

            float timeUntilBreak = _adManager.TimeUntilNextBreak;
            string fmt = FormatClock(timeUntilBreak);

            if (_adManager.IsQueued)
            {
                SetLabel(_breakValue, $"BREAK {fmt} Q", UIColors.Accent.Green);
            }
            else if (timeUntilBreak <= BreakCueWindow)
            {
                bool pulseFast = _cuePulseTime > 0f && ((int)(_pulseAccum * 6f)) % 2 == 0;
                bool on = pulseFast || BlinkOn();
                SetLabel(_breakValue, $"QUEUE MUSIC {fmt}", on ? UIColors.Warning.Caution : UIColors.TEXT_SECONDARY);
            }
            else
            {
                SetLabel(_breakValue, $"BREAK {fmt}", UIColors.TEXT_SECONDARY);
            }
        }

        private void UpdateListeners()
        {
            if (_listenerManager == null || _listenersValue == null || _trendArrow == null)
            {
                return;
            }

            _listenersValue.Text = $"Listeners: {_listenerManager.GetFormattedListeners()}";

            if (_sampleAccum < TrendSampleInterval || _timeManager == null)
            {
                return;
            }

            _sampleAccum = 0f;
            int direction = _trend.Update(_timeManager.ElapsedTime, _listenerManager.CurrentListeners);
            if (direction == _lastTrendDirection)
            {
                return;
            }

            _lastTrendDirection = direction;
            _trendArrow.SetDirection(direction);
            Color color = direction switch
            {
                ListenerTrendTracker.Up => UIColors.Accent.Green,
                ListenerTrendTracker.Down => UIColors.Warning.Critical,
                _ => UIColors.TEXT_PRIMARY
            };
            _listenersValue.AddThemeColorOverride("font_color", color);
        }

        private void UpdateMoney()
        {
            if (_economyManager == null || _moneyValue == null)
            {
                return;
            }

            int money = _economyManager.CurrentMoney;
            if (_moneyInitialized && money != _lastMoney)
            {
                _moneyFlashTime = MoneyFlashDuration;
                _moneyFlashPositive = money > _lastMoney;
            }

            _lastMoney = money;
            _moneyInitialized = true;

            _moneyValue.Text = $"Bank: ${money:N0}";
            if (_moneyFlashTime > 0f)
            {
                _moneyValue.AddThemeColorOverride("font_color",
                    _moneyFlashPositive ? UIColors.Accent.Green : UIColors.Warning.Critical);
            }
            else
            {
                _moneyValue.AddThemeColorOverride("font_color", UIColors.TEXT_PRIMARY);
            }
        }

        private void UpdateEvidence()
        {
            if (_screeningController == null)
            {
                return;
            }

            bool available = _screeningController.IsEvidenceAvailable;
            if (available && !_lastEvidenceAvailable)
            {
                var callerName = _screeningController.CurrentCaller?.Name ?? "Caller";
                _feed.Add($"EVIDENCE: {callerName} has evidence",
                    StatusFeedKind.EvidenceAvailable, _timeManager?.ElapsedTime ?? 0f);
            }

            _lastEvidenceAvailable = available;
        }

        // ───────────── Status feed ─────────────

        /// <summary>
        /// Advances the feed rotation every <see cref="FeedCycleInterval"/> seconds so the
        /// single feed line cycles through events still inside the display window.
        /// </summary>
        private void UpdateFeedCycle(float delta)
        {
            _feedCycleAccum += delta;
            if (_feedCycleAccum < FeedCycleInterval)
            {
                return;
            }

            _feedCycleAccum = 0f;
            _feedCycleIndex++;
        }

        /// <summary>
        /// Shows one feed line at a time, cycling through all events from the last
        /// <see cref="FeedWindowSeconds"/>; a new event resets the rotation to the newest.
        /// </summary>
        private void UpdateFeedDisplay()
        {
            if (_feedLabel == null)
            {
                return;
            }

            float now = _timeManager?.ElapsedTime ?? 0f;
            var entries = _feed.EntriesWithinWindow(now, FeedWindowSeconds);

            if (entries.Count == 0)
            {
                _feedCycleIndex = 0;
                _feedCycleAccum = 0f;
                _lastFeedTopSignature = "";
                if (_lastFeedSignature != "")
                {
                    _lastFeedSignature = "";
                    _feedLabel.Text = "";
                }

                return;
            }

            string topSignature = EntrySignature(entries[0]);
            if (topSignature != _lastFeedTopSignature)
            {
                _lastFeedTopSignature = topSignature;
                _feedCycleIndex = 0;
                _feedCycleAccum = 0f;
            }

            var entry = entries[_feedCycleIndex % entries.Count];
            string signature = EntrySignature(entry);
            if (signature == _lastFeedSignature)
            {
                return;
            }

            _lastFeedSignature = signature;
            _feedLabel.Text = $"[{entry.FormattedTime}] {entry.Message}";
            _feedLabel.AddThemeColorOverride("font_color", KindColor(entry.Kind));
            _feedLabel.Modulate = new Color(1f, 1f, 1f, 0f);
            var tween = CreateTween();
            tween.TweenProperty(_feedLabel, "modulate:a", 1f, 0.25f);
        }

        private static string EntrySignature(StatusFeedEntry entry) =>
            $"{entry.FormattedTime}|{(int)entry.Kind}|{entry.Message}";

        private void AddFeed(string message, StatusFeedKind kind)
        {
            _feed.Add(message, kind, _timeManager?.ElapsedTime ?? 0f);
        }

        // ───────────── Event handlers ─────────────

        private void OnEvidenceStored(Caller caller)
        {
            AddFeed($"EVIDENCE: collected {caller.Name}", StatusFeedKind.EvidenceCollected);
        }

        private void OnBroadcastInterruption(BroadcastInterruptionEvent ev)
        {
            if (ev.Reason != BroadcastInterruptionReason.CallerCursed)
            {
                return;
            }

            var name = _repository?.OnAirCaller?.Name ?? "Caller";
            AddFeed($"CURSED ON AIR: {name}", StatusFeedKind.Curse);
        }

        private void OnCursingTimer(CursingTimerCompletedEvent ev)
        {
            AddFeed(ev.WasSuccessful ? "CURSING: delayed in time" : "FCC FINE: $100 penalty",
                ev.WasSuccessful ? StatusFeedKind.Info : StatusFeedKind.Warning);
        }

        private void OnBroadcastTiming(BroadcastTimingEvent ev)
        {
            switch (ev.Type)
            {
                case BroadcastTimingEventType.Break20Seconds:
                case BroadcastTimingEventType.Break10Seconds:
                case BroadcastTimingEventType.Break5Seconds:
                case BroadcastTimingEventType.Break0Seconds:
                    _cuePulseTime = 1.5f;
                    break;
            }
        }

        // ───────────── Helpers ─────────────

        private bool BlinkOn() => ((int)(_pulseAccum * 2f)) % 2 == 0;

        private static string FormatClock(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{total / 60:D2}:{total % 60:D2}";
        }

        private static Color KindColor(StatusFeedKind kind) => kind switch
        {
            StatusFeedKind.EvidenceAvailable => UIColors.Warning.Info,
            StatusFeedKind.EvidenceCollected => UIColors.Accent.Green,
            StatusFeedKind.Curse => UIColors.Warning.Critical,
            StatusFeedKind.Warning => UIColors.Warning.Caution,
            _ => UIColors.TEXT_SECONDARY
        };

        /// <summary>
        /// Small drawn arrow/triangle for listener trend. Uses drawn polygons so it never
        /// depends on the bitmap font having glyphs for the unicode arrows.
        /// </summary>
        internal sealed partial class TrendArrow : Control
        {
            public int Direction { get; private set; } = ListenerTrendTracker.Flat;

            public void SetDirection(int direction)
            {
                if (Direction == direction)
                {
                    return;
                }

                Direction = direction;
                QueueRedraw();
            }

            public override void _Draw()
            {
                float w = Size.X;
                float h = Size.Y;
                if (w <= 0f || h <= 0f)
                {
                    return;
                }

                switch (Direction)
                {
                    case ListenerTrendTracker.Up:
                        DrawColoredPolygon(
                            new[] { new Vector2(w * 0.5f, 0f), new Vector2(0f, h), new Vector2(w, h) },
                            UIColors.Accent.Green);
                        break;
                    case ListenerTrendTracker.Down:
                        DrawColoredPolygon(
                            new[] { new Vector2(0f, 0f), new Vector2(w, 0f), new Vector2(w * 0.5f, h) },
                            UIColors.Warning.Critical);
                        break;
                    default:
                        float dashW = 6f;
                        float dashH = 1.5f;
                        DrawRect(new Rect2((w - dashW) * 0.5f, (h - dashH) * 0.5f, dashW, dashH),
                            UIColors.TEXT_DISABLED);
                        break;
                }
            }
        }
    }
}