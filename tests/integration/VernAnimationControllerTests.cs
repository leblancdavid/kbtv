#nullable enable

using System.Linq;
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
		_eventBus = new EventBus();
		DependencyInjection.Register<EventBus>(_ => _eventBus);
		_vern = null;
		_player = null;
	}

	[Test]
	public async Task PublishVernLine_SwitchesToTalkingAnimation()
	{
		await SetupVernAsync();
		PublishItem(BroadcastItemType.VernLine, audioLength: 3.0f);

		AssertThat(Unqualified(_player!.AssignedAnimation.ToString()) == TalkingAnimation,
			$"Expected {TalkingAnimation}, got {_player.AssignedAnimation}.");
	}

	[Test]
	public async Task PublishCallerLine_PlaysAnIdleBehavior()
	{
		await SetupVernAsync();
		PublishItem(BroadcastItemType.CallerLine, audioLength: 30.0f);

		var animation = Unqualified(_player!.AssignedAnimation.ToString());
		AssertThat(_idleBehaviors.Contains(animation),
			$"Expected one of [{string.Join(", ", _idleBehaviors)}], got {animation}.");
		AssertThat(animation != TalkingAnimation, "Vern must not talk while the caller speaks.");
	}

	[Test]
	public async Task PublishMusic_PlaysIdleBreathing()
	{
		await SetupVernAsync();
		PublishItem(BroadcastItemType.Music, audioLength: 5.0f);

		AssertThat(Unqualified(_player!.AssignedAnimation.ToString()) == IdleBreathingAnimation,
			$"Expected {IdleBreathingAnimation}, got {_player.AssignedAnimation}.");
	}

	[Test]
	public async Task CallerLine_ReturnsToIdleBeforeLineEnd()
	{
		await SetupVernAsync();

		// 3s line -> pre-speak idle scheduled 2s before the end (at 1s).
		PublishItem(BroadcastItemType.CallerLine, audioLength: 3.0f);

		AssertThat(await WaitForAnimationAsync(IdleBreathingAnimation),
			$"Vern should be idle by the pre-speak moment.");

		// Even if the first random pick was smoking/drinking, the behavior timer
		// could have switched for up to 6s; the pre-speak idle lock prevents that,
		// so Vern must stay breathing through the line end.
		await _testScene.ToSignal(_testScene.GetTree(), SceneTree.SignalName.ProcessFrame);
		await _testScene.ToSignal(_testScene.GetTree(), SceneTree.SignalName.ProcessFrame);
		AssertThat(Unqualified(_player!.AssignedAnimation.ToString()) == IdleBreathingAnimation,
			"Vern must remain idle breathing while the caller finishes.");
	}

	[Test]
	public async Task InterruptedLine_ReturnsToIdle()
	{
		await SetupVernAsync();
		PublishItem(BroadcastItemType.CallerLine, audioLength: 30.0f);
		await _testScene.ToSignal(_testScene.GetTree(), SceneTree.SignalName.ProcessFrame);

		_eventBus!.Publish(new BroadcastEvent(BroadcastEventType.Interrupted, "int-1"));

		AssertThat(Unqualified(_player!.AssignedAnimation.ToString()) == IdleBreathingAnimation,
			$"Expected {IdleBreathingAnimation} after interruption, got {_player.AssignedAnimation}.");
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
			await _testScene.ToSignal(_testScene.GetTree(), SceneTree.SignalName.ProcessFrame);
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

	private async Task<bool> WaitForAnimationAsync(string unqualifiedName, int maxFrames = 300)
	{
		for (var i = 0; i < maxFrames; i++)
		{
			if (Unqualified(_player!.AssignedAnimation.ToString()) == unqualifiedName)
			{
				return true;
			}
			await _testScene.ToSignal(_testScene.GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private static string Unqualified(string? animationName)
	{
		if (string.IsNullOrEmpty(animationName))
		{
			return "";
		}
		var lastSlash = animationName.LastIndexOf('/');
		return lastSlash >= 0 ? animationName.Substring(lastSlash + 1) : animationName;
	}
}