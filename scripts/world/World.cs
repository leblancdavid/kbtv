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

		Player.SetWorld(this);
		WorldRoom.SetPlayer(Player);

		var playerSprite = Player.GetNode<Sprite2D>("Sprite2D");
		if (playerSprite != null)
		{
			WorldRoom.ControlShadows.CreateShadowForObject(Player, playerSprite.Texture);
			WorldRoom.StudioShadows.CreateShadowForObject(Player, playerSprite.Texture);
		}
	}
}
