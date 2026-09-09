# KBTV Greybox Level Workflow

## Goal

Translate the approved Excalidraw station floorplan into a playable 3D greybox without committing to final art, lighting, or Blender production too early.

Greybox work validates scale, navigation, sightlines, collision, and task timing.

## Inputs

Greyboxing should start from:

- `docs/design/station-layout.excalidraw`
- `docs/design/station-layout-notes.md`
- [Station Floorplan Brief](STATION_FLOORPLAN_BRIEF.md)
- [3D Migration Plan](../technical/THREED_MIGRATION_PLAN.md)

Do not treat the current Godot blockout as the source of truth if it conflicts with the approved floorplan. The floorplan wins until the greybox has been playtested and accepted.

## Scale Assumptions

Use simple metric assumptions for the first pass:

- 1 Godot unit = 1 meter
- Minimum comfortable hallway width: 1.5m
- Preferred hallway width: 2m
- Minimum door opening: 1m
- Preferred door opening: 1.2m-1.5m
- Control room minimum: 4m x 5m
- Studio minimum: 4m x 5m
- Equipment room minimum: 3m x 3m
- Kitchen/supply minimum: 2.5m x 3m
- Bathroom minimum: 2m x 2m

These are starting values, not final production dimensions.

## HammerForge Option

HammerForge is a promising Godot brush-editing add-on for building rooms, walls, doors, and collision directly in Godot. It may be useful for fast iteration because it supports Hammer/TrenchBroom-style editing inside the engine.

Use HammerForge experimentally, not as a hard dependency. It is early alpha, so the project should remain able to fall back to native Godot blockout techniques.

## Fallback Options

If HammerForge blocks progress, use one of these:

- Native Godot `CSGBox3D` blockout for walls, floors, and placeholder furniture
- Simple `MeshInstance3D` boxes with generated collision
- External TrenchBroom-style workflow if a stable importer is chosen later
- Minimal scripted geometry for rooms that are still changing frequently

Do not introduce Blender until room relationships and route timings are approved.

## Handoff Steps

1. Convert approved Excalidraw room boxes into meter dimensions.
2. Create simple floors for each room.
3. Add walls and door openings.
4. Add player collision and major room colliders.
5. Add placeholder console, studio table, equipment racks, supplies, and bathroom fixtures.
6. Add temporary labels or color coding for rooms and interaction zones.
7. Add a first-person or constrained camera controller suitable for route testing.
8. Playtest the required route timings.
9. Adjust the floorplan if the problem is spatial, or adjust greybox scale if the plan still works.

## Greybox Review Checklist

The greybox passes the first review when:

- Player can move from console to every required first-pass space.
- Doorways feel readable and do not snag collision.
- Control room remains the obvious primary workspace.
- Studio visibility works from the intended control-room position.
- Equipment and supply errands meet target route timings.
- No required path feels like a maze.
- Player can understand where Vern is, where the console is, and where danger is likely to occur.
- There are at least three playable paranormal-event staging zones.

## Measurement Notes

Record these in `station-layout-notes.md` after each accepted pass:

- Room dimensions
- Door widths and positions
- Console position
- Studio window position
- Player start position
- Important route timings
- Any deviations from the Excalidraw plan
- Known collision or visibility problems

## Definition Of Done

A greybox slice is done when the player can run a representative live-show loop:

- Start at the control console.
- Check the studio through the intended sightline.
- Leave for supplies.
- Return to the console.
- Leave for equipment maintenance.
- Return to the console.
- Deliver something to Vern without entering the studio.

The slice does not need final meshes, textures, polished lighting, or animation.
