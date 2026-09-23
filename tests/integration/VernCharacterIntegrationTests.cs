using System;
using System.Linq;
using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Godot;
using KBTV.World3D;

namespace KBTV.Tests.Integration;

public class VernCharacterIntegrationTests : TestClass
{
	private readonly Node _testScene;

	public VernCharacterIntegrationTests(Node testScene) : base(testScene)
	{
		_testScene = testScene;
	}

	[Test]
	public async Task ImportedVern_IsSkinnedSeatedThenBreathesWithFixedFeet()
	{
		var scene = GD.Load<PackedScene>("res://scenes/world3d/Vern.tscn");
		var vern = scene.Instantiate<VernCharacter3D>();
		try
		{
			_testScene.AddChild(vern);
			// No frame wait here: _Ready must already have evaluated the clip.
			Require(vern.Visible && vern.Scale.IsEqualApprox(Vector3.One), "Vern must initialize at unit scale.");
			var skeletons = vern.FindChildren("*", "Skeleton3D", true, false).Cast<Skeleton3D>().ToArray();
			Require(skeletons.Length > 0, "Imported GLB must retain its skeleton.");
			var meshes = vern.FindChildren("*", "MeshInstance3D", true, false).Cast<MeshInstance3D>().ToArray();
			Require(meshes.Any(mesh => mesh.Skin != null && mesh.Skin.GetBindCount() > 0
				&& mesh.GetNodeOrNull<Skeleton3D>(mesh.Skeleton) != null), "Body must have a bound skin.");
			Require(meshes.All(mesh => mesh.Layers == StationLighting3D.StudioLayer), "Meshes must receive studio lighting.");
			var players = vern.FindChildren("*", "AnimationPlayer", true, false).Cast<AnimationPlayer>().ToArray();
			Require(players.Any(player => !player.IsPlaying()
				&& (player.AssignedAnimation.ToString() == "seated_rest"
					|| player.AssignedAnimation.ToString().EndsWith("/seated_rest", StringComparison.Ordinal))),
				"The seated clip must be assigned and paused immediately.");
			Require(players.Any(player => player.HasAnimation("talk_calm")),
				"The baked talk_calm clip must be injected into the imported model.");

			var bones = skeletons.SelectMany(skeleton => Enumerable.Range(0, skeleton.GetBoneCount())
				.Select(index => (Skeleton: skeleton, Index: index, Pose: skeleton.GetBonePose(index)))).ToArray();
			Require(bones.Count(bone => !bone.Pose.IsEqualApprox(bone.Skeleton.GetBoneRest(bone.Index))) >= 4,
				"Seated animation must move multiple bones away from the neutral bind pose at startup.");
			await _testScene.ToSignal(_testScene.GetTree(), SceneTree.SignalName.ProcessFrame);
			await _testScene.ToSignal(_testScene.GetTree(), SceneTree.SignalName.ProcessFrame);
			players[0].Advance(1);
			Require(bones.Any(bone => !bone.Pose.IsEqualApprox(bone.Skeleton.GetBonePose(bone.Index))),
				"Idle must move the upper body.");
			Require(bones.Where(b => new[] { "root", "pelvis", "foot.L", "foot.R" }.Contains(b.Skeleton.GetBoneName(b.Index)))
				.All(b => b.Pose.IsEqualApprox(b.Skeleton.GetBonePose(b.Index))), "Seat and feet must stay fixed.");
		}
		finally
		{
			vern.Free();
		}
	}

	private static void Require(bool condition, string message)
	{
		// Throw so failures count in the runner, rather than only logging soft assertions.
		if (!condition) throw new InvalidOperationException(message);
	}
}
