using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class ShadowSystem : Node
{
	[Export] public float HeightScale = 0.02f;
	[Export] public float DistanceScale = 400f;
	[Export] public float LerpFactor = 0.12f;
	[Export] public float Opacity = 0.8f;
	[Export] public int BaseZIndex = 10;

	[Export] public bool DebugEnabled = true;
	[Export] public bool PrintPositions = false;

	private Node2D _shadowParent;
	private ShaderMaterial _shadowMaterial;

	private readonly List<Sprite2D> _shadows = new();
	private readonly Dictionary<Sprite2D, Sprite2D> _shadowToMainSprite = new();

	public int ShadowCount => _shadows.Count;

	public override void _Ready()
	{
		GD.Print($"[ShadowSystem] Initialized with debug={DebugEnabled}");
	}

	public void Initialize(Node2D shadowParent, ShaderMaterial shadowMaterial)
	{
		_shadowParent = shadowParent;
		_shadowMaterial = shadowMaterial;

		if (_shadowMaterial != null)
		{
			_shadowMaterial.SetShaderParameter("shadow_color", new Color(0, 0, 0, Opacity));
		}

		GD.Print($"[ShadowSystem] Initialized: parent={shadowParent?.Name}, material={shadowMaterial != null}");
	}

	public void AddShadow(Sprite2D mainSprite, string name)
	{
		if (_shadowMaterial == null || _shadowParent == null || mainSprite == null)
		{
			GD.PrintErr($"[ShadowSystem] Cannot add shadow: material={_shadowMaterial != null}, parent={_shadowParent != null}, sprite={mainSprite != null}");
			return;
		}

		var shadowSprite = new Sprite2D
		{
			Name = "Shadow_" + name,
			Texture = mainSprite.Texture,
			Material = _shadowMaterial,
			Modulate = new Color(0, 0, 0, Opacity),
			FlipV = true,
			ZAsRelative = false,
			ZIndex = BaseZIndex
		};

		_shadowParent.AddChild(shadowSprite);
		_shadows.Add(shadowSprite);
		_shadowToMainSprite[shadowSprite] = mainSprite;

		var initialPos = GetTargetPosition(mainSprite);
		shadowSprite.GlobalPosition = mainSprite.GlobalPosition;

		if (DebugEnabled)
		{
			GD.Print($"[ShadowSystem] Created shadow '{shadowSprite.Name}' for '{name}'");
			GD.Print($"[ShadowSystem]   Sprite global: {mainSprite.GlobalPosition}");
			GD.Print($"[ShadowSystem]   Shadow global: {shadowSprite.GlobalPosition}");
			GD.Print($"[ShadowSystem]   Parent global: {_shadowParent.GlobalPosition}");
			GD.Print($"[ShadowSystem]   Target local: {initialPos}");
		}
	}

	public void Update(Node2D lightSource, double delta)
	{
		if (_shadows.Count == 0 || lightSource == null || _shadowParent == null)
			return;

		if (DebugEnabled && PrintPositions)
		{
			GD.Print($"[ShadowSystem] Updating {_shadows.Count} shadows");
		}

		var lightPos = lightSource.GlobalPosition;
		var lerpFactor = (float)delta / 0.016f * LerpFactor;
		lerpFactor = Mathf.Clamp(lerpFactor, 0.0f, 1.0f);

		for (int i = _shadows.Count - 1; i >= 0; i--)
		{
			var shadow = _shadows[i];
			if (!IsInstanceValid(shadow))
			{
				_shadows.RemoveAt(i);
				continue;
			}

			if (!_shadowToMainSprite.TryGetValue(shadow, out var mainSprite) || !IsInstanceValid(mainSprite))
				continue;

			var targetGlobalPos = mainSprite.GlobalPosition;

			var currentGlobalPos = shadow.GlobalPosition;
			var newGlobalPos = currentGlobalPos.Lerp(targetGlobalPos, lerpFactor);
			shadow.GlobalPosition = newGlobalPos;

			shadow.ZIndex = BaseZIndex;

			var spriteWorldPos = mainSprite.GlobalPosition;
			var lightToObject = spriteWorldPos - lightPos;
			var distance = lightToObject.Length();
			var direction = lightToObject.Normalized();

			var shearX = direction.X * distance * HeightScale;
			var shearY = direction.Y * distance * HeightScale;
			shearX = Mathf.Clamp(shearX, -2f, 2f);
			shearY = Mathf.Clamp(shearY, -2f, 2f);

			var targetScaleY = Mathf.Clamp(1.5f - (distance / DistanceScale) * 1.4f, 0.1f, 1.5f);

			var transform = Transform2D.Identity;
			transform.X = new Vector2(1f, 0f);
			transform.Y = new Vector2(shearX, targetScaleY);
			shadow.Transform = transform;

			if (DebugEnabled && PrintPositions && i == 0)
			{
				GD.Print($"[ShadowSystem] Shadow '{shadow.Name}':");
				GD.Print($"[ShadowSystem]   Sprite world: {spriteWorldPos}");
				GD.Print($"[ShadowSystem]   Target global: {targetGlobalPos}");
				GD.Print($"[ShadowSystem]   Light to object: {lightToObject}, dist={distance}");
			}
		}
	}

	private Vector2 GetTargetPosition(Sprite2D mainSprite)
	{
		if (_shadowParent == null || mainSprite == null)
			return Vector2.Zero;

		var worldPos = mainSprite.GlobalPosition;
		var localPos = _shadowParent.ToLocal(worldPos);

		return localPos;
	}

	public void SetDebugEnabled(bool enabled)
	{
		DebugEnabled = enabled;
	}

	public void SetPrintPositions(bool print)
	{
		PrintPositions = print;
	}

	public void Clear()
	{
		foreach (var shadow in _shadows)
		{
			if (IsInstanceValid(shadow))
				shadow.QueueFree();
		}
		_shadows.Clear();
		_shadowToMainSprite.Clear();
		GD.Print("[ShadowSystem] Cleared all shadows");
	}
}
