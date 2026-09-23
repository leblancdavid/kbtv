using System;
using Godot;

namespace KBTV.World3D;

public partial class VernCharacter3D : Node3D
{
	private const string TalkCalmPath = "res://assets/models3d/characters/vern/animations/talk_calm.tres";
	private const string TalkCalmName = "talk_calm";

	public AnimationPlayer? AnimPlayer { get; private set; }

	public override void _Ready()
	{
		// Apply before the first rendered frame and before World3D's layer traversal.
		Visible = false;
		AnimPlayer = FindAnimPlayer(this);
		if (AnimPlayer == null)
		{
			GD.PushError("Vern: imported model has no AnimationPlayer.");
			return;
		}
		// The baked talk_calm clip ships as a .tres alongside the model. It is not
		// part of vern.glb, so add it to the player's root (empty-name) library,
		// which is where the imported clips live (hence unqualified names).
		InjectTalkCalm(AnimPlayer);
		if (!ApplySeatedPose(AnimPlayer))
		{
			GD.PushError("Vern: imported model has no AnimationPlayer with seated_rest.");
			return;
		}
		StationLighting3D.ApplyLayerToTree(this, StationLighting3D.StudioLayer);
		var props = new VernPerformanceProps { Name = "PerformanceProps" };
		AddChild(props);
		props.Initialize(AnimPlayer);
		Visible = true;
	}

	private static AnimationPlayer? FindAnimPlayer(Node node)
	{
		if (node is AnimationPlayer player)
			return player;
		foreach (var child in node.GetChildren())
		{
			var found = FindAnimPlayer(child);
			if (found != null) return found;
		}
		return null;
	}

	private static void InjectTalkCalm(AnimationPlayer player)
	{
		if (player.HasAnimation(TalkCalmName))
		{
			return;
		}
		var animation = GD.Load<Animation>(TalkCalmPath);
		if (animation == null)
		{
			GD.PushWarning($"Vern: could not load {TalkCalmPath}.");
			return;
		}
		// Injected paths are identical to the imported VernRig tracks, so the
		// clip evaluates against the same skeleton the GLB clips use.
		if (!player.HasAnimationLibrary(""))
		{
			player.AddAnimationLibrary("", new AnimationLibrary());
		}
		var library = player.GetAnimationLibrary("");
		if (library == null)
		{
			GD.PushWarning("Vern: no animation library to host talk_calm.");
			return;
		}
		library.AddAnimation(TalkCalmName, animation);
	}

	private static bool ApplySeatedPose(AnimationPlayer player)
	{
		foreach (var animation in player.GetAnimationList())
		{
			// Imported libraries may qualify the clip as "library/seated_rest".
			if (animation != "seated_rest"
				&& !animation.EndsWith("/seated_rest", StringComparison.Ordinal))
			{
				continue;
			}
			player.Play(animation, customBlend: 0);
			player.Advance(0);
			player.Pause(); // Stop would reset playback; Pause holds the evaluated pose.
			return true;
		}
		return false;
	}
}
