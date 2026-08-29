using Godot;

/// <summary>
/// Room-level layout facts for the control room: ceiling light geometry. Per-prop placement
/// (desk group, speaker stands, cabinet, shelves, chair) and the on-air sign's position/light
/// tuning live in a dedicated file under <c>control_room/props/</c> and <c>common/props/</c>.
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
	/// ignored and reach = texture_size x texture_scale. Keep at 1.0 for the intended brightness;
	/// coverage of the full room + player area is handled by the light's z-range (see
	/// LIGHTING_SETUP.md — "Light2D z-range vs the manual y-sort"), NOT by inflating this value.
	/// </summary>
	public float CeilingLightTextureScale { get; } = 1.0f;

	/// <summary>Ceiling light texture size (square), before <see cref="CeilingLightTextureScale"/> applies.</summary>
	public int CeilingLightTextureSize { get; } = 512;
}
