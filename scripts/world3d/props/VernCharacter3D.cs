using System;
using Godot;

namespace KBTV.World3D;

public partial class VernCharacter3D : Node3D
{
	private static readonly (string Name, string Path)[] InjectedClips =
	{
		("talk_calm", "res://assets/models3d/characters/vern/animations/talk_calm_mpfb.tres"),
		("idle_breathing", "res://assets/models3d/characters/vern/animations/idle_breathing_mpfb.tres"),
		("talking_default", "res://assets/models3d/characters/vern/animations/talking_default_mpfb.tres"),
		("smoking", "res://assets/models3d/characters/vern/animations/smoking_mpfb.tres"),
		("drink_coffee", "res://assets/models3d/characters/vern/animations/drink_coffee_mpfb.tres"),
	};

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
		// The baked MPFB clips ship as .tres files alongside the model. They are not
		// part of vern_mpfb_fitted.glb (which only carries seated_rest), so inject
		// each into the player's root (empty-name) library with the exact names the
		// controller suffix-matches on.
		InjectClips(AnimPlayer);
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

	private static void InjectClips(AnimationPlayer player)
	{
		if (!player.HasAnimationLibrary(""))
		{
			player.AddAnimationLibrary("", new AnimationLibrary());
		}
		var library = player.GetAnimationLibrary("");
		if (library == null)
		{
			GD.PushWarning("Vern: no animation library to host the MPFB clips.");
			return;
		}
		foreach (var (name, path) in InjectedClips)
		{
			if (player.HasAnimation(name))
			{
				continue;
			}
			var animation = GD.Load<Animation>(path);
			if (animation == null)
			{
				GD.PushWarning($"Vern: could not load {path}.");
				continue;
			}
			// Injected paths target the MPFB skeleton the GLB clips use, so each
			// clip evaluates against the same skeleton as the imported rest pose.
			library.AddAnimation(name, animation);
		}
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
