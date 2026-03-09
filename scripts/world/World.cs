using Godot;
using KBTV.Core;

public partial class World : Node2D
{
	[Export] public float TileSize = 16f;

	public WorldRoom WorldRoom { get; private set; } = null!;
	public Player Player { get; private set; } = null!;

	public override void _Ready()
	{
		Player = GetNode<Player>("Player");
		WorldRoom = GetNode<WorldRoom>("WorldRoom");

		GD.Print($"World: Player found: {Player != null}, WorldRoom found: {WorldRoom != null}");

		Player.SetWorld(this);
		WorldRoom.SetPlayer(Player);

		// Load a texture for shadow creation using south-facing sprite
		var playerTexture = GD.Load<Texture2D>("res://assets/sprites/characters/player/south.png");
		GD.Print($"World: Loaded south.png texture: {playerTexture != null}");
		if (playerTexture != null)
		{
			// Use offset to shift shadow anchor up 8px to compensate for texture padding
			var shadowOffset = new Vector2(0, -6);
			WorldRoom.ControlShadows.CreateShadowForObject(Player, playerTexture, offset: shadowOffset, createOvalBase: true);
			WorldRoom.StudioShadows.CreateShadowForObject(Player, playerTexture, offset: shadowOffset, createOvalBase: true);
		}
	}
}
