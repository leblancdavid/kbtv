using System;
using Godot;
using KBTV.Data;

/// <summary>
/// The studio's ambient smoke effect, layered over the stage and keyed to how recently Vern
/// smoked: intensity decays from 1 to 0 over the configured decay time after a cigarette.
/// Owns the particle sprites, their per-particle drift/cycle arrays and per-frame updates so
/// the builder stays focused on room orchestration.
/// </summary>
public sealed class StudioSmoke
{
	private const string TexturePath = "res://assets/tiles/smoke_sheet.png";
	private const int LayerCount = 3;
	private const int RootZIndex = 480;
	private const int FrameSize = 256;
	private const int FrameGrid = 5;
	private const int TotalFrames = FrameGrid * FrameGrid;

	private Node2D _smokeRoot = null!;
	private Node2D[] _smokeLayers = Array.Empty<Node2D>();
	private AnimatedSprite2D[] _smokeSprites = Array.Empty<AnimatedSprite2D>();
	private float[] _smokeInitialX = Array.Empty<float>();
	private float[] _smokeTimeOffsets = Array.Empty<float>();
	private float[] _smokeLayerOffsets = Array.Empty<float>();
	private float[] _smokeCycleLengths = Array.Empty<float>();
	private float[] _smokePhaseOffsets = Array.Empty<float>();
	private float _decayTime = 60f;

	public void Initialize(Node2D propSort, Vector2 smokePosition, int maxParticles, float decayTime, int lightMask)
	{
		_decayTime = decayTime;

		var smokeTexture = GD.Load<Texture2D>(TexturePath);
		if (smokeTexture == null)
		{
			GD.PrintErr("StudioSmoke: Failed to load smoke_sheet.png");
			return;
		}

		_smokeRoot = new Node2D
		{
			Name = "SmokeRoot",
			Position = smokePosition,
			YSortEnabled = true,
			ZIndex = RootZIndex
		};

		var frames = new SpriteFrames();
		var frameSize = new Vector2I(FrameSize, FrameSize);
		for (int y = 0; y < FrameGrid; y++)
		{
			for (int x = 0; x < FrameGrid; x++)
			{
				var region = new Rect2I(new Vector2I(x * frameSize.X, y * frameSize.Y), frameSize);
				var frame = new AtlasTexture
				{
					Atlas = smokeTexture,
					Region = region
				};
				frames.AddFrame("default", frame);
			}
		}
		frames.SetAnimationSpeed("default", 0.5f);
		frames.SetAnimationLoop("default", true);

		_smokeLayers = new Node2D[LayerCount];
		_smokeLayerOffsets = new float[LayerCount];
		for (int i = 0; i < LayerCount; i++)
		{
			var layer = new Node2D { Name = $"SmokeLayer_{i}", YSortEnabled = true };
			_smokeLayers[i] = layer;
			_smokeLayerOffsets[i] = i * 7.5f;
			_smokeRoot.AddChild(layer);
		}

		_smokeSprites = new AnimatedSprite2D[maxParticles];
		_smokeInitialX = new float[maxParticles];
		_smokeTimeOffsets = new float[maxParticles];
		_smokeCycleLengths = new float[maxParticles];
		_smokePhaseOffsets = new float[maxParticles];

		for (int i = 0; i < maxParticles; i++)
		{
			var initialX = GD.Randf() * 240 - 120;
			_smokeInitialX[i] = initialX;
			_smokeTimeOffsets[i] = GD.Randf() * 30f;
			_smokeCycleLengths[i] = 50f + GD.Randf() * 20f;
			_smokePhaseOffsets[i] = GD.Randf() * _smokeCycleLengths[i];

			var smokeSprite = new AnimatedSprite2D
			{
				Name = $"SmokePuff_{i}",
				SpriteFrames = frames,
				Position = new Vector2(initialX, -GD.Randf() * 180 + 160),
				Scale = new Vector2(1.8f + GD.Randf() * 0.6f, 1.8f + GD.Randf() * 0.6f),
				Modulate = new Color(1f, 1f, 1f, 0.02f)
			};
			smokeSprite.Set("light_mask", lightMask);
			smokeSprite.Frame = (int)(GD.Randf() * TotalFrames);

			var layerIndex = i % LayerCount;
			_smokeLayers[layerIndex].AddChild(smokeSprite);
			_smokeSprites[i] = smokeSprite;
		}

		propSort.AddChild(_smokeRoot);
	}

	public void Update(VernStats? vernStats)
	{
		if (_smokeRoot == null)
		{
			return;
		}

		float intensity;
		if (vernStats == null)
		{
			intensity = 1f;
		}
		else
		{
			var timeSinceLastCigarette = vernStats.TimeSinceLastCigarette;
			if (timeSinceLastCigarette < 5f)
			{
				intensity = 1f;
			}
			else if (timeSinceLastCigarette < _decayTime)
			{
				float t = (timeSinceLastCigarette - 5f) / (_decayTime - 5f);
				intensity = 1f - t;
			}
			else
			{
				intensity = 0f;
			}
		}

		var baseAlpha = Mathf.Clamp(intensity, 0f, 1f) * 0.03f;
		var smokeTime = Time.GetTicksMsec() / 1000f;

		for (int i = 0; i < _smokeSprites.Length; i++)
		{
			var sprite = _smokeSprites[i];
			if (sprite == null)
			{
				continue;
			}

			var layerIndex = i % _smokeLayerOffsets.Length;
			var adjustedTime = smokeTime + _smokeTimeOffsets[i] + _smokeLayerOffsets[layerIndex];
			var cycleLength = _smokeCycleLengths[i];
			var cyclePos = ((adjustedTime + _smokePhaseOffsets[i]) % cycleLength) / cycleLength;
			var eased = Mathf.SmoothStep(0f, 1f, cyclePos);

			var yOffset = eased * 180f;
			var xWobble = Mathf.Sin(smokeTime * 0.12f + i) * 5f;
			var yBias = i * 0.01f;

			sprite.Position = new Vector2(
				_smokeInitialX[i] + xWobble,
				-yOffset + 32 + yBias
			);

			float fadeIn = cyclePos < 0.3f ? cyclePos / 0.3f : 1f;
			float fadeOut = cyclePos > 0.6f ? (1f - cyclePos) / 0.4f : 1f;
			var alpha = baseAlpha * fadeIn * fadeOut;

			sprite.Modulate = new Color(1f, 1f, 1f, alpha);

			var scale = 1.0f + eased * 0.5f;
			sprite.Scale = new Vector2(scale, scale);

			var frameIndex = Mathf.Clamp((int)(cyclePos * TotalFrames), 0, TotalFrames - 1);
			sprite.Frame = frameIndex;
		}
	}
}