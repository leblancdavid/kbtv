# KBTV 3D Migration Plan

## Goal

Move KBTV from a 2D top-down presentation to a 3D presentation while keeping the current game loop, UI menus, broadcast systems, and room scope focused on:

- Current UI menus
- Control room
- Studio

This is a presentation migration, not a gameplay rewrite.

## What Stays

The following systems should remain intact during the migration:

- Main menu, loading screen, pre-show UI, live-show UI, post-show UI
- Service bootstrap and game-state systems
- Caller generation, screening, dialogue, ads, economy, save/load
- Existing broadcast flow and monitors

Only the world presentation changes.

## Recommended 3D Approach

Use a **stylized 2.5D 3D setup**:

- 3D room geometry
- Fixed or lightly constrained camera
- UI stays in 2D overlays
- Props can be true 3D meshes or temporary billboard/flat replacements during the transition

This gives the team a controlled migration path without forcing a simultaneous redesign of the UI or game systems.

Before expanding the 3D layout, use the Excalidraw-first process in [Station Layout Workflow](../design/STATION_LAYOUT_WORKFLOW.md). The editable floorplan and notes live under `docs/design/` and should be approved before greybox production continues.

## Migration Phases

### Phase 1: 3D Foundation

Create a new 3D world root and prove the scene bootstrap works.

Deliverables:

- New 3D world scene and root script
- 3D camera setup
- Basic lighting setup
- UI overlay compatibility check
- Scene switch path between current 2D world and new 3D world

### Phase 2: Control Room Blockout

Rebuild the control room in 3D with simple geometry.

Deliverables:

- Floor, walls, ceiling, and doorway layout
- Desk and monitor placeholders
- Screening interaction point
- Basic room lighting
- Player placement and navigation inside the room

Prerequisite: confirm the control-room relationship to the rest of the station in [Station Floorplan Brief](../design/STATION_FLOORPLAN_BRIEF.md).

### Phase 3: Studio Blockout

Rebuild the studio in 3D with matching scale and layout.

Deliverables:

- Floor, walls, ceiling, and doorway layout
- Vern stage area / chair area
- Round table placeholder
- Smoke or atmosphere placeholder
- Basic lighting and transitions

Prerequisite: confirm studio visibility, locked-door behavior, and Vern drop-off path in the approved floorplan notes.

### Phase 3.5: Expanded Station Greybox

Add the first non-studio station spaces after the control-room/studio relationship is approved.

Deliverables:

- Hallway circulation spine
- Equipment room route
- Kitchen / supply route
- Bathroom or compact scare-space route
- Route timing pass against the targets in [Greybox Level Workflow](../design/GREYBOX_LEVEL_WORKFLOW.md)

### Phase 4: Interaction Parity

Restore the core room behaviors.

Deliverables:

- Door transitions between control room and studio
- Screening trigger in the control room
- Room-specific lighting states
- Any room-state gating needed by gameplay

### Phase 5: Visual Pass

Replace blockouts with final presentation assets.

Deliverables:

- Final room materials
- Final props
- Lighting polish
- Camera framing polish
- Performance check

## Suggested Scene Structure

Proposed new 3D structure:

```text
scenes/
├── Main.tscn
├── Game3D.tscn
├── world3d/
│   ├── World3D.tscn
│   ├── ControlRoom3D.tscn
│   ├── StudioRoom3D.tscn
│   └── Player3D.tscn
└── ui/
    └── ... existing UI scenes ...
```

The existing UI scenes should remain reusable. They should be attached to the 3D game scene as overlays, not rebuilt as 3D interfaces.

## Control Room 3D Requirements

The 3D control room should preserve the current gameplay feel:

- Primary player workspace
- Desk / monitoring area
- Screening interaction point
- Props arranged to match the existing room composition
- Lighting that keeps the room readable and moody

Priority is layout fidelity, not immediate graphical fidelity.

## Studio 3D Requirements

The studio should preserve the current gameplay role:

- Vern performance area
- Round table / conversation zone
- Door connection back to the control room
- Atmospheric lighting and smoke equivalent

Again, layout fidelity comes first.

## Player / Camera Strategy

Recommended approach for the first pass:

- Keep movement simple and close to the current player feel
- Use a fixed or constrained camera
- Avoid free camera controls unless a design need emerges
- Preserve current interaction distances and room boundaries as closely as possible

This minimizes the number of game systems that need to change at once.

## UI Strategy

The current UI should remain the default UI layer.

Keep:

- Pre-show menus
- Caller screening UI
- Live show UI
- Post-show UI

Only change UI code if it assumes the world is a `Node2D` or depends on 2D world coordinates.

## Asset Strategy

During migration, prefer staged replacement:

1. Use placeholder 3D meshes for layout validation
2. Reuse 2D artwork as temporary billboards if needed
3. Replace major props first
4. Polish detail props last

This avoids blocking progress on content production before the scene architecture is proven.

## Risks

- Recreating the current room feel in 3D may take several iterations
- UI assumptions may still leak 2D world dependencies
- Lighting and occlusion behavior will need new validation
- Player interaction distances may feel different in 3D and require tuning

## Definition of Done For The First Migration Slice

The first successful 3D slice should include:

- A working 3D world scene
- Control room and studio blockouts
- Existing UI menus intact
- Door transition between rooms
- Screening trigger preserved
- No changes required to the broadcast/economy/save systems

## Recommendation

Build the 3D migration as a parallel path, not an in-place conversion. That keeps the current game playable while the new world is being built and makes it easier to compare room feel, camera framing, and interaction timing against the current implementation.
