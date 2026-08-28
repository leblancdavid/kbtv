using Godot;

/// <summary>
/// Room-level layout facts for the control room: ceiling light geometry and the on-air sign's
/// position/light tuning. Per-prop placement (desk group, speaker stands, cabinet, shelves,
/// chair) now lives in a dedicated file under <c>control_room/props/</c>.
/// </summary>
public sealed class ControlRoomLayout
{
	// ── Grid ────────────────────────────────────────────────────────────────────────────
	// Control room is 14 wide x 10 tall, anchored at (0, 1000) in world space.

	// ── Ceiling light ───────────────────────────────────────────────────────────────────
	/// <summary>Ceiling light's downward offset from the room center (light hovers above the room).</summary>
	public int CeilingLightOffsetY { get; } = 64;

	/// <summary>
	/// Multiplier on the ceiling light's 512px texture. With a texture set, <c>range</c> is
	/// ignored and reach = texture_size x texture_scale — at scale 1.0 the tall control room's
	/// south half (player Y ~1100-1240) falls in the texture fade-out. This value keeps the
	/// whole room + player area inside the fully-lit region.
	/// </summary>
	public float CeilingLightTextureScale { get; } = 2.4f;

	/// <summary>Ceiling light texture size (square), before <see cref="CeilingLightTextureScale"/> applies.</summary>
	public int CeilingLightTextureSize { get; } = 512;

	// ── On-air sign ─────────────────────────────────────────────────────────────────────
	/// <summary>Offset from the grid anchor to the on-air sign (above the door).</summary>
	public Vector2 OnAirSignFromAnchor { get; } = new(32, -112);
	public Vector2 OnAirSignScale { get; } = new(0.75f, 1.0f);

	/// <summary>Low-level key light for the on-air sign, placed at the sign's position.</summary>
	public Color OnAirSignLightColor { get; } = new(1f, 0.1f, 0.1f);
	public float OnAirSignLightEnergy { get; } = 0.5f;
	public float OnAirSignLightRadius { get; } = 120f;

	public int LightMask { get; } = 1;
}