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
/// never mid-action when he is about to speak himself.
/// </summary>
public partial class VernAnimationController : Node
{
	// Animation clip names baked into vern.glb. Imported libraries may qualify
	// them (e.g. "Library/talking_default") so lookups use suffix matching.
	private const string AnimSeatedRest = "seated_rest";
	private const string AnimIdleBreathing = "idle_breathing";
	private const string AnimTalking = "talking_default";
	private const string AnimSmoking = "smoking";
	private const string AnimDrinkCoffee = "drink_coffee";

	// Switch Vern back to idle breathing this many seconds before the caller's line ends.
	private const float PreSpeakIdleSeconds = 2.0f;
	// Mandatory breathing pause between one-shot actions (random between these bounds).
	private const float MinBreathingSeconds = 2.0f;
	private const float MaxBreathingSeconds = 4.0f;
	// How long a smoking/drinking action is held before the next cycle step.
	private const float MinActionSeconds = 3.0f;
	private const float MaxActionSeconds = 6.0f;
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

	// SceneTreeTimers cannot be cancelled, so a monotonically increasing
	// generation invalidates callbacks scheduled before a state change.
	private ulong _timerGeneration;

	public override void _Ready()
	{
		// Parent's _Ready (which caches AnimPlayer) runs after children, so defer.
		CallDeferred(nameof(Initialize));
	}

	public override void _ExitTree()
	{
		Unsubscribe();
		_timerGeneration++;
	}

	private void Initialize()
	{
		if (GetParent() is VernCharacter3D vern)
		{
			_animPlayer = vern.AnimPlayer;
		}
		_animPlayer ??= FindAnimPlayer(this);

		if (DependencyInjection.TryGet(this, out EventBus eventBus))
		{
			_eventBus = eventBus;
			Subscribe();
			PlayLooping(AnimIdleBreathing);
		}
		else
		{
			CallDeferred(nameof(RetryResolveServices));
		}
	}

	private void RetryResolveServices()
	{
		if (_eventBus == null && DependencyInjection.TryGet(this, out EventBus eventBus))
		{
			_eventBus = eventBus;
			Subscribe();
			PlayLooping(AnimIdleBreathing);
			return;
		}
		if (_eventBus == null)
		{
			CallDeferred(nameof(RetryResolveServices));
		}
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
		if (@event.Type != BroadcastEventType.Interrupted)
		{
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

		// Every new broadcast item overrides whatever Vern was doing.
		_timerGeneration++;

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
		if (_state != AnimState.CallerIdle)
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

		var minSeconds = next == IdleBehavior.Breathing ? MinBreathingSeconds : MinActionSeconds;
		var maxSeconds = next == IdleBehavior.Breathing ? MaxBreathingSeconds : MaxActionSeconds;
		var animation = next switch
		{
			IdleBehavior.Smoking => AnimSmoking,
			IdleBehavior.Drinking => AnimDrinkCoffee,
			_ => AnimIdleBreathing
		};

		PlayLooping(animation);
		ScheduleIdleBehaviorAfter(next, minSeconds, maxSeconds);
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
		if (_animPlayer == null)
		{
			return;
		}

		var resolved = ResolveAnimationName(animationName);
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