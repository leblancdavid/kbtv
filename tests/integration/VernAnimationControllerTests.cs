#nullable enable

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.Core;
using KBTV.Dialogue;
using KBTV.World3D;

namespace KBTV.Tests.Integration;

public class VernAnimationControllerTests : KBTVTestClass
{
	private const string TalkingAnimation = "talking_default";
	private const string IdleBreathingAnimation = "idle_breathing";
	private readonly string[] _idleBehaviors = { "smoking", IdleBreathingAnimation, "drink_coffee" };

	private readonly Node _testScene;
	private EventBus? _eventBus;
	private VernCharacter3D? _vern;
	private AnimationPlayer? _player;

	public VernAnimationControllerTests(Node testScene) : base(testScene)
	{
		_testScene = testScene;
	}

	[Setup]
	public void BeforeEach()
	{
		// ServiceProviderRoot registers this in the running game; tests provide their own.
		// Note: it is not added to the tree, so Publish defers delivery to the message queue.
		_eventBus = new EventBus();
		DependencyInjection.Register<EventBus>(_ => _eventBus);
		_vern = null;
		_player = null;
	}

	[Cleanup]
	public void AfterEach()
	{
		_vern?.Free();
		_eventBus?.Dispose();
		_eventBus = null;
	}

	[Test]
	public async Task PublishVernLine_SwitchesToTalkingAnimation()
	{
		await SetupVernAsync();
		PublishItem(BroadcastItemType.VernLine, audioLength: 3.0f);

		AssertThat(await WaitForAnimationAsync(TalkingAnimation),
			$"VernLine should play {TalkingAnimation}, got {CurrentAnimation()}.");
	}

	[Test]
	public async Task PublishCallerLine_LeavesTalkingForAnIdleBehavior()
	{
		await SetupVernAsync();
		PublishItem(BroadcastItemType.VernLine, audioLength: 3.0f);
		AssertThat(await WaitForAnimationAsync(TalkingAnimation),
			$"VernLine should play {TalkingAnimation} first.");

		// Long line so the 2s pre-speak idle does not interfere.
		PublishItem(BroadcastItemType.CallerLine, audioLength: 30.0f);

		AssertThat(await WaitUntilAsync(() => _idleBehaviors.Contains(CurrentAnimation())),
			$"CallerLine should play an idle behavior, got {CurrentAnimation()}.");
		AssertThat(CurrentAnimation() != TalkingAnimation,
			$"Vern must not talk while the caller speaks, got {CurrentAnimation()}.");
	}

	[Test]
	public async Task PublishMusic_ReturnsToIdleBreathing()
	{
		await SetupVernAsync();
		PublishItem(BroadcastItemType.VernLine, audioLength: 3.0f);
		AssertThat(await WaitForAnimationAsync(TalkingAnimation),
			$"VernLine should play {TalkingAnimation} first.");

		PublishItem(BroadcastItemType.Music, audioLength: 5.0f);

		AssertThat(await WaitForAnimationAsync(IdleBreathingAnimation),
			$"Music should return Vern to {IdleBreathingAnimation}, got {CurrentAnimation()}.");
	}

	[Test]
	public async Task CallerLine_ReturnsToIdleBeforeLineEnd()
	{
		await SetupVernAsync();

		// 2.5s line -> pre-speak idle 2s before the end (at 0.5s), well before any
		// behavior switch timer (>= 2s) could fire. Waiting on the lock itself makes
		// the test deterministic regardless of the random first behavior.
		PublishItem(BroadcastItemType.CallerLine, audioLength: 2.5f);

		AssertThat(await WaitUntilAsync(() => Controller().DiagnosticPreSpeakIdle),
			"The pre-speak idle lock should engage while the caller speaks.");
		AssertThat(CurrentAnimation() == IdleBreathingAnimation,
			$"Vern should be idle the moment the lock engages, got {CurrentAnimation()}.");

		// Let the line elapse. The pre-speak lock must keep Vern idle past the end.
		await WaitSecondsAsync(2.6f);
		AssertThat(CurrentAnimation() == IdleBreathingAnimation,
			$"Vern must stay idle breathing through the line end, got {CurrentAnimation()}.");
		AssertThat(Controller().DiagnosticPreSpeakIdle,
			"The pre-speak idle lock should stay engaged until a new broadcast item.");
	}

	[Test]
	public async Task InterruptedLine_ReturnsToIdle()
	{
		await SetupVernAsync();

		// Get a CallerLine running and its pre-speak lock engaged.
		PublishItem(BroadcastItemType.CallerLine, audioLength: 2.5f);
		AssertThat(await WaitUntilAsync(() => Controller().DiagnosticPreSpeakIdle),
			"The pre-speak idle lock should engage while the caller speaks.");

		_eventBus!.Publish(new BroadcastEvent(BroadcastEventType.Interrupted, "test-CallerLine"));

		AssertThat(await WaitUntilAsync(() => !Controller().DiagnosticPreSpeakIdle),
			"Interruption should release the pre-speak idle lock.");
		AssertThat(await WaitForAnimationAsync(IdleBreathingAnimation),
			$"Interruption should return Vern to {IdleBreathingAnimation}, got {CurrentAnimation()}.");
	}

	private string CurrentAnimation()

		=> Unqualified(_player?.AssignedAnimation.ToString());

	[Test]
	public async Task ConsecutiveSpeech_PreservesPhaseAndIgnoresStaleInterruption()
	{
		await SetupVernAsync();
		PublishItem(BroadcastItemType.VernLine, 10);
		await WaitForAnimationAsync(TalkingAnimation);
		_player!.Advance(2);
		var before = _player.CurrentAnimationPosition;
		_eventBus!.Publish(new BroadcastEvent(BroadcastEventType.Completed, "test-VernLine"));
		PublishItem(BroadcastItemType.VernLine, 10);
		_eventBus.Publish(new BroadcastEvent(BroadcastEventType.Interrupted, "stale-line"));
		await WaitSecondsAsync(.2f);
		Require(CurrentAnimation() == TalkingAnimation && _player.CurrentAnimationPosition >= before,
			"Consecutive speech must preserve gesture phase; stale events must be ignored.");
	}

	[Test]
	public async Task ShortCaller_NeverStartsPropAction()
	{
		await SetupVernAsync();
		for (var i = 0; i < 12; i++)
		{
			PublishItem(BroadcastItemType.CallerLine, 6);
			await FrameAsync();
			await FrameAsync();
			Require(CurrentAnimation() == IdleBreathingAnimation, "Action cannot fit before pre-speak idle.");
		}
	}

	[Test]
	public async Task PropAction_FinishesBeforeSpeechAndRestoresSingleProp()
	{
		await SetupVernAsync();
		for (var attempt = 0; attempt < 30 && CurrentAnimation() == IdleBreathingAnimation; attempt++)
		{
			PublishItem(BroadcastItemType.CallerLine, 30);
			await FrameAsync();
			await FrameAsync();
		}
		var clip = CurrentAnimation();
		Require(clip is "smoking" or "drink_coffee", "Expected a caller prop action.");
		Require(_player!.GetAnimation(_player.AssignedAnimation).LoopMode == Animation.LoopModeEnum.None,
			"Prop actions must be one-shots.");
		var props = _vern!.GetNode<VernPerformanceProps>("PerformanceProps");
		var prop = props.GetNode<Node3D>(clip == "smoking" ? "cigarette" : "coffee_mug");
		var rest = prop.Transform;
		_player.Advance(2.5);
		props._Process(0);
		Require(prop.Position.DistanceTo(rest.Origin) > .2f, "Held prop must follow the hand toward the mouth.");
		PublishItem(BroadcastItemType.VernLine, 10);
		await FrameAsync();
		await FrameAsync();
		Require(CurrentAnimation() == clip, "New speech must not abandon a held prop.");
		_player.Advance(6);
		props._Process(0);
		Require(CurrentAnimation() == TalkingAnimation, "Completion must resume the latest speech state.");
		Require(prop.Transform.IsEqualApprox(rest) && props.GetChildCount() == 4,
			"Completion must return the single visible prop to its exact anchor.");
	}

	[Test]
	public async Task HeldProps_FollowEvaluatedHandsUnderTransformedRoot()
	{
		await SetupVernAsync();
		using var contract = JsonDocument.Parse(FileAccess.GetFileAsString(
			"res://assets/models3d/characters/vern/animation_contacts.json"));
		var props = _vern!.GetNode<VernPerformanceProps>("PerformanceProps");
		var skeleton = _vern.FindChildren("*", "Skeleton3D", true, false).Cast<Skeleton3D>().First();
		var instances = props.GetChildren().Cast<Node3D>().ToArray();
		var rootTransform = _vern.Transform;
		var propsTransform = props.Transform;
		var speed = _player!.SpeedScale;
		_player.SpeedScale = 0;
		try
		{
			foreach (var transformed in new[] { false, true })
			{
				_vern.Transform = transformed
					? new Transform3D(new Basis(Vector3.Up, .7f).Scaled(new Vector3(1.3f, .8f, 1.1f)),
						new Vector3(4, 2, -3)) : rootTransform;
				// Also exercise the conversion into the props root's own local space.
				props.Transform = transformed
					? new Transform3D(new Basis(Vector3.Right, .2f), new Vector3(.2f, .1f, -.3f))
					: propsTransform;
				foreach (var clip in new[] { "smoking", "drink_coffee" })
				{
					var action = contract.RootElement.GetProperty("actions").GetProperty(clip);
					var name = action.GetProperty("prop").GetString()!;
					var prop = props.GetNode<Node3D>(name);
					var rest = DecodeContact(contract.RootElement.GetProperty("props").GetProperty(name).GetProperty("rest"));
					var grip = DecodeContact(action.GetProperty("hand_local_grip"));
					var hand = skeleton.FindBone(action.GetProperty("hand_bone").GetString()!);
					Require(hand >= 0, $"{clip} must reference an imported hand bone.");
					_player.Play(_player.GetAnimationList().First(a => Unqualified(a) == clip), 0);
					_player.Seek(2.5, update: true);
					await FrameAsync();
					await FrameAsync();
					Require(prop.GlobalTransform.IsEqualApprox(skeleton.GlobalTransform * skeleton.GetBoneGlobalPose(hand) * grip),
						$"{clip}: held prop must match the evaluated hand after frame processing (transformed={transformed}).");
					var held = prop.GlobalTransform;
					var position = skeleton.GetBonePosePosition(hand);
					var rotation = skeleton.GetBonePoseRotation(hand);
					try
					{
						skeleton.SetBonePosePosition(hand, position + new Vector3(.17f, .09f, -.12f));
						skeleton.SetBonePoseRotation(hand, rotation * new Quaternion(Vector3.Up, .4f));
						props._Process(0);
						Require(!prop.GlobalTransform.IsEqualApprox(held), $"{clip}: changing the hand at fixed clip time must move the prop.");
						Require(prop.GlobalTransform.IsEqualApprox(skeleton.GlobalTransform * skeleton.GetBoneGlobalPose(hand) * grip),
							$"{clip}: hand translation and rotation must drive the prop, not sampled trajectory JSON.");
					}
					finally
					{
						skeleton.SetBonePosePosition(hand, position);
						skeleton.SetBonePoseRotation(hand, rotation);
					}
					foreach (var time in new[] { action.GetProperty("pickup_seconds").GetDouble() - .01,
						action.GetProperty("release_seconds").GetDouble(), 5.0 })
					{
						_player.Seek(time, update: true);
						props._Process(0);
						Require(prop.Transform.IsEqualApprox(rest), $"{clip}: prop must stay at its tray anchor outside contact ({time}s).");
					}
					Require(instances.SequenceEqual(props.GetChildren().Cast<Node3D>()),
						"Attachment and release must preserve every original prop instance.");
				}
			}
		}
		finally
		{
			_vern.Transform = rootTransform;
			props.Transform = propsTransform;
			_player.SpeedScale = speed;
		}
	}

	private static Transform3D DecodeContact(JsonElement value)
	{
		var p = value.GetProperty("position");
		var q = value.GetProperty("rotation_quaternion_xyzw");
		return new Transform3D(new Basis(new Quaternion(q[0].GetSingle(), q[1].GetSingle(),
			q[2].GetSingle(), q[3].GetSingle())), new Vector3(p[0].GetSingle(), p[1].GetSingle(), p[2].GetSingle()));
	}

	private static void Require(bool condition, string message)
	{
		if (!condition) throw new InvalidOperationException(message);
	}

	private VernAnimationController Controller()
		=> _vern!.GetNode<VernAnimationController>("VernAnimationController");

	private string Unqualified(string? animationName)
	{
		if (string.IsNullOrEmpty(animationName))
		{
			return "";
		}
		var lastSlash = animationName.LastIndexOf('/');
		return lastSlash >= 0 ? animationName.Substring(lastSlash + 1) : animationName;
	}

	private async Task SetupVernAsync()
	{
		if (_player != null)
		{
			return;
		}
		var scene = GD.Load<PackedScene>("res://scenes/world3d/Vern.tscn");
		_vern = scene.Instantiate<VernCharacter3D>();
		_testScene.AddChild(_vern);
		// Let the controller's deferred Initialize resolve and subscribe.
		for (var i = 0; i < 3; i++)
		{
			await FrameAsync();
		}
		_player = _vern.FindChildren("*", "AnimationPlayer", true, false)
			.Cast<AnimationPlayer>()
			.First();
	}

	private void PublishItem(BroadcastItemType type, float audioLength)
	{
		var item = new BroadcastItem("test-" + type, type, "Test line", null, 4.0f);
		_eventBus!.Publish(new BroadcastItemStartedEvent(item, 4.0f, audioLength));
	}

	private async Task WaitSecondsAsync(float seconds)
		=> await _testScene.ToSignal(_testScene.GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

	private async Task FrameAsync()
		=> await _testScene.ToSignal(_testScene.GetTree(), SceneTree.SignalName.ProcessFrame);

	private async Task<bool> WaitForAnimationAsync(string unqualifiedName, int maxFrames = 480)
		=> await WaitUntilAsync(() => CurrentAnimation() == unqualifiedName, maxFrames);

	private async Task<bool> WaitUntilAsync(Func<bool> predicate, int maxFrames = 480)
	{
		for (var i = 0; i < maxFrames; i++)
		{
			if (predicate())
			{
				return true;
			}
			await FrameAsync();
		}
		return false;
	}
}
