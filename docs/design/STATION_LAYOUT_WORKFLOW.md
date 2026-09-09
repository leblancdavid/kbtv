# KBTV Station Layout Workflow

## Goal

Design the KBTV station layout as an editable 2D floorplan before committing to 3D level production.

The floorplan is the source of truth for room relationships, player routes, visibility, risk, and paranormal-event staging. Godot greyboxing should start only after the floorplan passes review.

## Source Files

Keep station layout artifacts in `docs/design`:

```text
docs/design/
+-- station-layout.excalidraw
+-- station-layout-notes.md
+-- reference/
```

Use `station-layout.excalidraw` for the editable diagram and `station-layout-notes.md` for decisions, rejected alternatives, measurements, and unresolved questions.

## Recommended Toolchain

| Stage | Tool | Purpose |
|-------|------|---------|
| Floorplan | Excalidraw | Fast room, route, door, sightline, and prop-zone iteration |
| Review | OpenCode | Inspect the structured canvas, identify layout problems, propose changes |
| Greybox | Godot / HammerForge | Turn the approved plan into a playable blockout |
| Final Art | Blender / Godot assets | Replace validated blockout pieces with production assets |

Do not use Blender as the first layout tool. Blender is useful once room sizes and relationships are already approved.

## OpenCode MCP Setup

The Excalidraw MCP server can be configured for OpenCode with:

```json
{
  "$schema": "https://opencode.ai/config.json",
  "mcp": {
    "excalidraw": {
      "type": "local",
      "command": ["npx", "-y", "mcp-excalidraw-server"],
      "enabled": true
    }
  }
}
```

Requirements:

- Node 20+
- No API keys
- `.excalidraw` files saved in the repo
- Screenshots or canvas inspection available for visual self-checks

## Workflow

1. Sketch the first station plan in Excalidraw.
2. Label every room, doorway, window, and high-value interaction zone.
3. Add player-route arrows for normal live-show tasks.
4. Mark risk zones where leaving the control console costs time or information.
5. Ask OpenCode to review the diagram before making changes.
6. Iterate until circulation, visibility, and event staging work on paper.
7. Record approved measurements in `station-layout-notes.md`.
8. Build the greybox from the approved notes.

## Review Prompts

Use prompts like:

```text
Review docs/design/station-layout.excalidraw as a first-person paranormal radio station. The control room is the player's primary work area. Check walking distances, visibility into the host studio, opportunities for paranormal events, bottlenecks, and whether leaving the console creates meaningful risk. Suggest changes before modifying the diagram.
```

```text
Move the equipment room so the player has to pass the studio window to reach it, but keep the total round trip under 25 seconds. Preserve the control room as the primary workspace.
```

```text
Identify three places where a paranormal event could happen while the player is distracted by caller screening. Do not modify the diagram yet; list the gameplay purpose of each location.
```

## Diagram Standards

Every floorplan revision should include:

- Room names
- Door swing or opening direction where relevant
- Studio window or sightline markers
- Console, screening desk, and phone-board zones
- Vern/studio performance zone
- Equipment maintenance zone
- Kitchen/bathroom supply zone
- Paranormal-event candidate zones
- Player route arrows for common tasks
- Approximate dimensions or grid scale

## Review Criteria

A floorplan is ready for greyboxing when:

- The control room remains the player's clear home base.
- The studio is visible enough to create tension but not directly accessible during broadcasts.
- Important errands require leaving the console for meaningful but fair amounts of time.
- Player routes avoid accidental dead ends and frustrating narrow bottlenecks.
- At least three paranormal-event spaces are visible or reachable during live-show pressure.
- The equipment room and supply area create different route decisions.
- The plan can be translated into simple rectangular room dimensions.

## Change Control

Do not rewrite the 3D scene directly when a problem is fundamentally spatial. Update the Excalidraw plan first, then adjust the greybox.

Use this order:

1. Diagram problem.
2. Review the floorplan.
3. Approve measurements.
4. Change the greybox.
5. Playtest timing and visibility.
