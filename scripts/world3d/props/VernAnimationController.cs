#nullable enable

using System;
using Godot;
using KBTV.Core;
using KBTV.Dialogue;

namespace KBTV.World3D;

/// <summary>
/// Drives Vern's 3D animations based on who is speaking in the broadcast.
/// Vern talks while his own lines play; while a caller talks Vern randomly
/// cycles through smoking, idle breathing and drinking coffee, with a
/// mandatory idle pause between the one-shot actions. A couple of seconds
/// before the caller's line ends Vern returns to idle breathing so he is
/// ready to speak. Unexpected speech/interruption waits visually for a safe
/// prop return without delaying audio; consecutive speech keeps its gesture phase.
/// </summary>
public partial class VernAnimationController : Node
{
	// Animation clip names for vern.glb. Imported libraries may qualify them (e.g.
	// "Library/talking_default"), and the production talk clip is injected by
	// VernCharacter3D as "talk_calm", so lookups use suffix matching throughout.
	private const string AnimSeatedRest = "seated_rest";
	private const string AnimIdleBreathing = "idle_breathing";
	private const string AnimTalking = "talk_calm";
	private const string AnimTalkingFallback = "talking_default";
	private const string AnimSmoking = "smoking";
	private const string AnimDrinkCoffee = "drink_coffee";

	// Switch Vern back to idle breathing this many seconds before the caller's line ends.
	private const float PreSpeakIdleSeconds = 2.0f;
	// Mandatory breathing pause between one-shot actions (random between these bounds).
	private const float MinBreathingSeconds = 2.0f;
	private const float MaxBreathingSeconds = 4.0f;
	// One-shots finish on AnimationFinished, using imported duration for admission.
	private double _callerDeadline;
	private bool _oneShot;
	private string? _deferredAnimation;
	private string? _activeItemId;
	// Fallback line length when the broadcast provides no audio timing.
	private const float FallbackLineSeconds = 4.0f;

	private enum AnimState
	{
		Idle,
		Talking,
		CallerIdle
	}

	private enum IdleBehavior
	{
		Smoking,
		Breathing,
		Drinking
	}

	private AnimationPlayer? _animPlayer;
	private EventBus? _eventBus;
	private AnimState _state = AnimState.Idle;
	private bool _preSpeakIdle;
	private bool _subscribed;

	// Bounded EventBus resolution: the registry is populated by ServiceProviderRoot in
	// the game scene, but tests instantiate Vern.tscn without it. Retry a few frames
	// via _Process (never a re-entrant deferred loop), then give up gracefully.
	private int _resolveAttempts;
	private const int MaxResolveAttempts = 10;

	// SceneTreeTimers cannot be cancelled, so a monotonically increasing
	// generation invalidates callbacks scheduled before a state change.
	private ulong _timerGeneration;

	/// <summary>Diagnostics seam: the (unqualified) animation currently assigned to the player.</summary>
	public string DiagnosticAnimation => _animPlayer?.AssignedAnimation.ToString() ?? "";

	/// <summary>Diagnostics seam: true while Vern is locked idle for the end of the caller's line.</summary>
	public bool DiagnosticPreSpeakIdle => _preSpeakIdle;

	public override void _Ready()
	{
		// Parent's _Ready (which caches AnimPlayer) runs after children, so defer.
		// Processing stays off unless EventBus resolution needs retrying.
		SetProcess(false);
		CallDeferred(nameof(Initialize));
	}

	public override void _Process(double delta)
	{
		RetryResolveServices();
	}

	public override void _ExitTree()
	{
		Unsubscribe();
		if (_animPlayer != null) _animPlayer.AnimationFinished -= OnAnimationFinished;
		_timerGeneration++;
	}

	private void Initialize()
	{
		if (GetParent() is VernCharacter3D vern)
		{
			_animPlayer = vern.AnimPlayer;
		}
		_animPlayer ??= FindAnimPlayer(this);
		if (_animPlayer != null) _animPlayer.AnimationFinished += OnAnimationFinished;
		PlayLooping(AnimIdleBreathing);
		RetryResolveServices();
	}

	private void RetryResolveServices()
	{
		if (_eventBus != null)
		{
			SetProcess(false);
			return;
		}
		if (_resolveAttempts >= MaxResolveAttempts)
		{
			SetProcess(false);
			return;
		}
		_resolveAttempts++;
		if (DependencyInjection.TryGet(this, out EventBus eventBus) && eventBus != null)
		{
			_eventBus = eventBus;
			Subscribe();
			PlayLooping(AnimIdleBreathing);
			SetProcess(false);
			return;
		}
		// Try again next frame; if the registry is empty the cap stops it quickly.
		SetProcess(true);
	}

	private void Subscribe()
	{
		if (_subscribed || _eventBus == null)
		{
			return;
		}
		_eventBus.Subscribe<BroadcastItemStartedEvent>(OnBroadcastItemStarted);
		_eventBus.Subscribe<BroadcastEvent>(OnBroadcastEvent);
		_subscribed = true;
	}

	private void Unsubscribe()
	{
		if (!_subscribed || _eventBus == null)
		{
			return;
		}
		_eventBus.Unsubscribe<BroadcastItemStartedEvent>(OnBroadcastItemStarted);
		_eventBus.Unsubscribe<BroadcastEvent>(OnBroadcastEvent);
		_subscribed = false;
	}

	private void OnBroadcastEvent(BroadcastEvent @event)
	{
		// If the current line is interrupted (cursing, caller drop, break) before a
		// replacement item starts, return Vern to idle breathing instead of freezing.
		if (@event.ItemId != _activeItemId || @event.Type == BroadcastEventType.Started)
		{
			return;
		}
		if (@event.Type == BroadcastEventType.Completed)
		{
			// A short grace bridges consecutive lines without restarting the gesture loop.
			var generation = _timerGeneration;
			GetTree().CreateTimer(0.12).Timeout += () =>
			{
				if (generation != _timerGeneration || !IsInsideTree()) return;
				_timerGeneration++;
				_state = AnimState.Idle;
				PlayLooping(AnimIdleBreathing);
			};
			return;
		}
		_timerGeneration++;
		_state = AnimState.Idle;
		_preSpeakIdle = false;
		PlayLooping(AnimIdleBreathing);
	}

	private void OnBroadcastItemStarted(BroadcastItemStartedEvent @event)
	{
		if (_animPlayer == null || @event.Item == null)
		{
			return;
		}

		// Replace intent/timers, retaining an in-progress prop-safe return.
		_timerGeneration++;
		_activeItemId = @event.Item.Id;

		switch (@event.Item.Type)
		{
			case BroadcastItemType.VernLine:
			case BroadcastItemType.DeadAir:
				_state = AnimState.Talking;
				_preSpeakIdle = false;
				PlayLooping(AnimTalking);
				break;

			case BroadcastItemType.CallerLine:
				_state = AnimState.CallerIdle;
				_preSpeakIdle = false;
				var lineEndSeconds = @event.AudioLength > 0f ? @event.AudioLength : FallbackLineSeconds;
				SchedulePreSpeakIdle(lineEndSeconds);
				if (_oneShot) PlayLooping(AnimIdleBreathing);
				PlayRandomIdleBehavior(null);
				break;

			default:
				// Music, ads, transitions and anything else — Vern just sits there.
				_state = AnimState.Idle;
				_preSpeakIdle = false;
				PlayLooping(AnimIdleBreathing);
				break;
		}
	}

	private void SchedulePreSpeakIdle(float lineLengthSeconds)
	{
		_callerDeadline = Time.GetTicksMsec()/1000.0 + Math.Max(0, lineLengthSeconds-PreSpeakIdleSeconds);
		// Make sure Vern is idle (not smoking/drinking) when he is about to talk.
		var delay = Mathf.Max(lineLengthSeconds - PreSpeakIdleSeconds, 0.1f);
		var generation = _timerGeneration;
		GetTree().CreateTimer(delay).Timeout += () =>
		{
			if (generation != _timerGeneration || _state != AnimState.CallerIdle)
			{
				return;
			}
			_preSpeakIdle = true;
			PlayLooping(AnimIdleBreathing);
		};
	}

	private void PlayRandomIdleBehavior(IdleBehavior? previous)
	{
		if (_state != AnimState.CallerIdle || _oneShot || _preSpeakIdle)
		{
			return;
		}

		IdleBehavior next;
		if (previous == null)
		{
			// First behavior of a caller line: any of the three.
			next = (IdleBehavior)(int)(GD.Randi() % 3);
		}
		else if (previous == IdleBehavior.Breathing)
		{
			// After a breathing pause pick a one-shot action (never two pauses in a row).
			next = (int)(GD.Randi() % 2) == 0 ? IdleBehavior.Smoking : IdleBehavior.Drinking;
		}
		else
		{
			// After smoking/drinking there is always a breathing pause.
			next = IdleBehavior.Breathing;
		}

		var animation = next switch
		{
			IdleBehavior.Smoking => AnimSmoking,
			IdleBehavior.Drinking => AnimDrinkCoffee,
			_ => AnimIdleBreathing
		};

		var resolved = ResolveAnimationName(animation);
		var duration = resolved == null ? 0 : _animPlayer!.GetAnimation(resolved).Length;
		if (next != IdleBehavior.Breathing && duration > 0
			&& Time.GetTicksMsec()/1000.0 + duration + 0.25 < _callerDeadline)
		{
			_oneShot = true;
			_animPlayer!.GetAnimation(resolved!).LoopMode = Animation.LoopModeEnum.None;
			_animPlayer.Play(resolved!, customBlend: 0.2);
		}
		else
		{
			PlayLooping(AnimIdleBreathing);
			ScheduleIdleBehaviorAfter(IdleBehavior.Breathing, MinBreathingSeconds, MaxBreathingSeconds);
		}
	}

	private void OnAnimationFinished(StringName animation)
	{
		if (!_oneShot) return;
		_oneShot = false;
		var next = _deferredAnimation;
		_deferredAnimation = null;
		PlayLooping(next ?? (_state == AnimState.Talking ? AnimTalking : AnimIdleBreathing));
		if (_state == AnimState.CallerIdle && !_preSpeakIdle)
			ScheduleIdleBehaviorAfter(IdleBehavior.Breathing, MinBreathingSeconds, MaxBreathingSeconds);
	}

	private void ScheduleIdleBehaviorAfter(IdleBehavior completed, float minSeconds, float maxSeconds)
	{
		var delay = (float)GD.RandRange(minSeconds, maxSeconds);
		var generation = _timerGeneration;
		GetTree().CreateTimer(delay).Timeout += () =>
		{
			if (generation != _timerGeneration || _state != AnimState.CallerIdle || _preSpeakIdle)
			{
				return;
			}
			PlayRandomIdleBehavior(completed);
		};
	}

	private void PlayLooping(string animationName)
	{
		// Let the authored return finish; audio never waits and props never teleport.
		if (_oneShot)
		{
			_deferredAnimation = animationName;
			return;
		}
		if (_animPlayer == null)
		{
			return;
		}

		var resolved = animationName == AnimTalking
			? ResolveTalkingAnimation()
			: ResolveAnimationName(animationName);
		if (resolved == null)
		{
			GD.PushWarning($"VernAnimationController: no animation '{animationName}' on the model.");
			return;
		}

		var animation = _animPlayer.GetAnimation(resolved);
		if (animation != null && animation.LoopMode != Animation.LoopModeEnum.Linear)
		{
			animation.LoopMode = Animation.LoopModeEnum.Linear;
		}

		// Play always resumes even if VernCharacter3D paused the player after seated_rest.
		if (!string.Equals(_animPlayer.CurrentAnimation, resolved, StringComparison.Ordinal))
		{
			_animPlayer.Play(resolved, customBlend: 0.25f);
		}
	}

	// The production talk clip is injected by VernCharacter3D; if it failed to
	// load, fall back to the imported GLB clip so Vern still talks.
	private string? ResolveTalkingAnimation()
		=> ResolveAnimationName(AnimTalking) ?? ResolveAnimationName(AnimTalkingFallback);

	private string? ResolveAnimationName(string animationName)
	{
		if (_animPlayer == null)
		{
			return null;
		}
		foreach (var candidate in _animPlayer.GetAnimationList())
		{
			if (candidate == animationName
				|| candidate.EndsWith("/" + animationName, StringComparison.Ordinal))
			{
				return candidate;
			}
		}
		return null;
	}

	private static AnimationPlayer? FindAnimPlayer(Node node)
	{
		if (node is AnimationPlayer player)
		{
			return player;
		}
		foreach (var child in node.GetChildren())
		{
			var found = FindAnimPlayer(child);
			if (found != null)
			{
				return found;
			}
		}
		return null;
	}
}
