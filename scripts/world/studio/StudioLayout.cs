using Godot;

/// <summary>
/// Room-level layout facts for the studio: ceiling light geometry and the smoke emitter's anchor.
/// Per-prop placement (round table, Vern's chair, bookcases) and the on-air sign's position/light
/// tuning live in a dedicated file under <c>studio/props/</c> and <c>common/props/</c>.
/// </summary>
public sealed class StudioLayout
{
	// ── Grid ────────────────────────────────────────────────────────────────────────────
	// Studio is 14 wide x 6 tall, anchored at (0, 776) in world space.

	// ── Ceiling light ───────────────────────────────────────────────────────────────────
	/// <summary>Ceiling light's downward offset from the room center.</summary>
	public int CeilingLightOffsetY { get; } = 32;

	/// <summary>Ceiling light texture size (square). Studio is short, so scale 1.0 covers it.</summary>
	public int CeilingLightTextureSize { get; } = 512;
	public float CeilingLightTextureScale { get; } = 1.0f;

	// ── Smoke ───────────────────────────────────────────────────────────────────────────
	/// <summary>Relative row from the bottom where the smoke emitter anchors (column 7).</summary>
	public int SmokeRowsFromBottom { get; } = 3;
	public int SmokeColumn { get; } = 7;
}