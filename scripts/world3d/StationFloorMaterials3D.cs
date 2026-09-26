using Godot;

namespace KBTV.World3D;

public static class StationFloorMaterials3D
{
	private const string TextureRoot = "res://assets/textures/world3d/";
	private const float DefaultDetailCenter = 0.5f;

	public static StandardMaterial3D MakeControlCarpet()
	{
		return MakeComicMaskedMaterial("control_carpet.png", new Color(0.18f, 0.12f, 0.075f), 0.08f, 0.015f, 0.045f, 0.18f, 0.96f);
	}

	public static StandardMaterial3D MakeStudioCarpet()
	{
		return MakeComicMaskedMaterial("studio_carpet.png", new Color(0.17f, 0.065f, 0.05f), 0.08f, 0.015f, 0.04f, 0.16f, 0.97f);
	}

	public static StandardMaterial3D MakeHallLinoleum()
	{
		return MakeComicMaskedMaterial("hall_linoleum.png", new Color(0.105f, 0.125f, 0.12f), 0.28f, 0.015f, 0.04f, 0.15f, 0.92f, borderDarken: 0.34f, borderWidthFraction: 0.085f);
	}

	public static StandardMaterial3D MakeWallpaper()
	{
		return MakeComicMaskedMaterial("wallpaper_subtle.png", new Color(0.18f, 0.145f, 0.105f), 0.06f, 0.01f, 0.045f, 0.16f, 0.9f);
	}

	public static StandardMaterial3D MakeRedBrick(string textureName = "wallpaper_subtle.png")
	{
		return MakeComicMaskedMaterial(textureName, new Color(0.18f, 0.055f, 0.035f), 0.16f, 0.025f, 0.035f, 0.18f, 0.95f);
	}

	public static ArrayMesh MakeTiledFloorMesh(float width, float depth, float tileWorldSize)
	{
		var columns = Mathf.Max(1, Mathf.CeilToInt(width / tileWorldSize));
		var rows = Mathf.Max(1, Mathf.CeilToInt(depth / tileWorldSize));
		var vertexCount = columns * rows * 4;
		var indexCount = columns * rows * 6;
		var vertices = new Vector3[vertexCount];
		var normals = new Vector3[vertexCount];
		var uvs = new Vector2[vertexCount];
		var indices = new int[indexCount];

		var v = 0;
		var i = 0;
		var left = -width * 0.5f;
		var near = -depth * 0.5f;
		for (var row = 0; row < rows; row++)
		{
			var z0 = near + row * tileWorldSize;
			var z1 = Mathf.Min(z0 + tileWorldSize, near + depth);
			var vScale = (z1 - z0) / tileWorldSize;
			for (var column = 0; column < columns; column++)
			{
				var x0 = left + column * tileWorldSize;
				var x1 = Mathf.Min(x0 + tileWorldSize, left + width);
				var uScale = (x1 - x0) / tileWorldSize;
				vertices[v + 0] = new Vector3(x0, 0f, z0);
				vertices[v + 1] = new Vector3(x1, 0f, z0);
				vertices[v + 2] = new Vector3(x1, 0f, z1);
				vertices[v + 3] = new Vector3(x0, 0f, z1);
				normals[v + 0] = Vector3.Up;
				normals[v + 1] = Vector3.Up;
				normals[v + 2] = Vector3.Up;
				normals[v + 3] = Vector3.Up;
				uvs[v + 0] = Vector2.Zero;
				uvs[v + 1] = new Vector2(uScale, 0f);
				uvs[v + 2] = new Vector2(uScale, vScale);
				uvs[v + 3] = new Vector2(0f, vScale);
				indices[i + 0] = v + 0;
				indices[i + 1] = v + 1;
				indices[i + 2] = v + 2;
				indices[i + 3] = v + 0;
				indices[i + 4] = v + 2;
				indices[i + 5] = v + 3;
				v += 4;
				i += 6;
			}
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices;
		arrays[(int)Mesh.ArrayType.Normal] = normals;
		arrays[(int)Mesh.ArrayType.TexUV] = uvs;
		arrays[(int)Mesh.ArrayType.Index] = indices;

		var mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
		return mesh;
	}

	private static StandardMaterial3D MakeComicMaskedMaterial(
		string textureName,
		Color baseColor,
		float darkenStrength,
		float brightenStrength,
		float minLuma,
		float maxLuma,
		float roughness,
		float detailCenter = DefaultDetailCenter,
		float borderDarken = 0f,
		float borderWidthFraction = 0.035f)
	{
		var image = LoadPng(TextureRoot + textureName);
		if (image.IsEmpty())
		{
			return new StandardMaterial3D { AlbedoColor = ClampLuma(baseColor, minLuma, maxLuma), Roughness = roughness };
		}

		image.Convert(Image.Format.Rgba8);
		var width = image.GetWidth();
		var height = image.GetHeight();
		for (var y = 0; y < image.GetHeight(); y++)
		{
			for (var x = 0; x < image.GetWidth(); x++)
			{
				var source = image.GetPixel(x, y);
				var detail = Luma(source) - detailCenter;
				var strength = detail < 0f ? darkenStrength : brightenStrength;
				var color = baseColor * (1f + detail * 2f * strength);

				var borderMask = BorderMask(x, y, width, height, borderWidthFraction);
				if (borderMask > 0f && borderDarken > 0f)
				{
					color *= 1f - borderMask * borderDarken;
				}

				color = ClampLuma(color, minLuma, maxLuma);
				color.A = 1f;
				image.SetPixel(x, y, color);
			}
		}

		return new StandardMaterial3D
		{
			AlbedoColor = Colors.White,
			AlbedoTexture = ImageTexture.CreateFromImage(image),
			TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			Roughness = roughness
		};
	}

	private static float Luma(Color color)
	{
		return color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f;
	}

	private static Color ClampLuma(Color color, float minLuma, float maxLuma)
	{
		var luma = Luma(color);
		if (luma <= 0.0001f)
		{
			return new Color(minLuma, minLuma, minLuma, color.A);
		}

		var targetLuma = Mathf.Clamp(luma, minLuma, maxLuma);
		var scaled = color * (targetLuma / luma);
		scaled.A = color.A;
		return scaled;
	}

	private static float BorderMask(int x, int y, int width, int height, float borderWidthFraction)
	{
		var borderWidth = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(width, height) * borderWidthFraction));
		var distanceToEdge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
		return 1f - Mathf.Clamp(distanceToEdge / (float)borderWidth, 0f, 1f);
	}

	private static Image LoadPng(string path)
	{
		var image = new Image();
		var bytes = FileAccess.GetFileAsBytes(path);
		if (bytes.Length == 0 || image.LoadPngFromBuffer(bytes) != Error.Ok)
		{
			return Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
		}

		return image;
	}
}
