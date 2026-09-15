using Chickensoft.GoDotTest;
using Godot;
using KBTV.World3D;

namespace KBTV.Tests.Integration;

public class CrtGlassIntegrationTests : KBTVTestClass
{
	public CrtGlassIntegrationTests(Node testScene) : base(testScene) { }

	[Test]
	public void ImportedGlassOverride_PreservesHousingAndSharedAsset()
	{
		var scene = GD.Load<PackedScene>("res://assets/models3d/props/crt_computer.glb");
		var model = scene.Instantiate<Node3D>();
		var untouched = scene.Instantiate<Node3D>();
		try
		{
			ComputerTerminal3D.ConfigureGlassMaterial(model);
			var matched = 0;
			foreach (var node in model.FindChildren("*", "MeshInstance3D", true, false))
			{
				var mesh = (MeshInstance3D)node;
				var other = untouched.GetNode<MeshInstance3D>(model.GetPathTo(mesh));
				for (var i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
				{
					var source = (StandardMaterial3D)mesh.Mesh.SurfaceGetMaterial(i);
					if (!source.ResourceName.StartsWith("Cool phosphor"))
					{
						AssertThat(mesh.GetSurfaceOverrideMaterial(i) == null);
						continue;
					}
					matched++;
					var glass = (StandardMaterial3D)mesh.GetActiveMaterial(i);
					AssertThat(glass != source && !glass.EmissionEnabled);
					AssertThat(glass.AlbedoColor.V < 0.03f);
					AssertThat(other.GetActiveMaterial(i) == source);
					AssertThat(source.EmissionEnabled);
				}
			}
			AssertThat(matched == 1);
		}
		finally
		{
			model.Free();
			untouched.Free();
		}
	}
}
