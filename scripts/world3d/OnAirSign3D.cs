using Godot;
using System.Collections.Generic;

namespace KBTV.World3D;

public partial class OnAirSign3D : Node
{
	private static readonly Color ActiveRed = new(1.0f, 0.08f, 0.04f);
	private static readonly Color OffGlow = new(0.12f, 0.015f, 0.01f);
	private readonly List<MaterialState> _materials = new();
	private OmniLight3D? _glow;

	private sealed class MaterialState
	{
		public required StandardMaterial3D Material { get; init; }
		public required bool OriginalEmissionEnabled { get; init; }
		public required Color OriginalEmission { get; init; }
		public required float OriginalEmissionEnergy { get; init; }
		public required bool IsTextMaterial { get; init; }
	}

	public void Attach(Node3D signRoot, uint lightMask)
	{
		signRoot.Visible = true;
		_materials.Clear();
		CollectEditableMaterials(signRoot);

		_glow = new OmniLight3D
		{
			Name = "OnAirRoomGlow",
			Position = new Vector3(0f, -0.08f, -0.72f),
			LightColor = ActiveRed,
			LightEnergy = 0f,
			LightIndirectEnergy = 0f,
			OmniRange = 1.05f,
			OmniAttenuation = 5.2f,
			LightCullMask = lightMask,
			Visible = false
		};
		signRoot.AddChild(_glow);

		SetActive(false);
	}

	public void SetActive(bool active)
	{
		foreach (var state in _materials)
		{
			var material = state.Material;
			if (active)
			{
				material.EmissionEnabled = true;
				material.Emission = ActiveRed;
				material.EmissionEnergyMultiplier = state.IsTextMaterial ? 1.2f : 0.12f;
			}
			else
			{
				material.EmissionEnabled = true;
				material.Emission = state.OriginalEmissionEnabled ? state.OriginalEmission : OffGlow;
				material.EmissionEnergyMultiplier = state.OriginalEmissionEnabled
					? Mathf.Max(state.OriginalEmissionEnergy, 0.035f)
					: 0.035f;
			}
		}

		if (_glow != null)
		{
			_glow.Visible = active;
			_glow.LightEnergy = active ? 0.32f : 0f;
		}
	}

	private void CollectEditableMaterials(Node node)
	{
		if (node is MeshInstance3D meshInstance)
		{
			meshInstance.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			CollectMeshMaterials(meshInstance);
		}

		foreach (var child in node.GetChildren())
		{
			CollectEditableMaterials(child);
		}
	}

	private void CollectMeshMaterials(MeshInstance3D meshInstance)
	{
		if (meshInstance.MaterialOverride is StandardMaterial3D overrideMaterial)
		{
			var copy = DuplicateMaterial(overrideMaterial);
			meshInstance.MaterialOverride = copy;
			_materials.Add(CaptureState(copy));
			return;
		}

		var surfaceCount = meshInstance.Mesh?.GetSurfaceCount() ?? 0;
		for (var i = 0; i < surfaceCount; i++)
		{
			var material = meshInstance.GetActiveMaterial(i) as StandardMaterial3D;
			if (material == null)
			{
				continue;
			}

			var copy = DuplicateMaterial(material);
			meshInstance.SetSurfaceOverrideMaterial(i, copy);
			_materials.Add(CaptureState(copy));
		}
	}

	private static StandardMaterial3D DuplicateMaterial(StandardMaterial3D material)
	{
		var copy = (StandardMaterial3D)material.Duplicate();
		copy.ResourceLocalToScene = true;
		return copy;
	}

	private static MaterialState CaptureState(StandardMaterial3D material) => new()
	{
		Material = material,
		OriginalEmissionEnabled = material.EmissionEnabled,
		OriginalEmission = material.Emission,
		OriginalEmissionEnergy = material.EmissionEnergyMultiplier,
		IsTextMaterial = IsTextMaterial(material)
	};

	private static bool IsTextMaterial(StandardMaterial3D material)
	{
		var name = material.ResourceName.ToLowerInvariant();
		return name.Contains("text") || name.Contains("letter") || name.Contains("on_air") ||
			name.Contains("onair") || name.Contains("neon");
	}
}
