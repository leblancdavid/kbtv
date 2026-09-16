# Soundboard Minigame Design

## Overview

Two changes in one feature:

1. **Screening navigation overlay** — the `<--`/`X` buttons are removed from the
   projector UI (`CallerTab`) and replaced by a global `ScreenNavOverlay`:
   - Top-left `<-` opens the diegetic **soundboard** view (camera zooms to the
     control-room soundboard prop). While the soundboard is framed the `<-`
     switches back to the terminal; the top-right `X` closes the current view.
   - Top-right `X` closes whatever view is open (matches today's close behavior).
   - The nav overlay is **visible only while a view is open**.

2. **Soundboard minigame** — a 3-channel mixer (Vern / Caller / Ads-Bumper)
   with gain, low-pass, high-pass knobs, a master fader, and status LEDs. Knobs
   drive existing audio-bus effects as **offsets on top of** the equipment
   presets (`AudioEffectsProcessor`). Getting the levels wrong drains Vern's
   Emotional/Mental stats.

## Design Decisions (confirmed)

- **Diegetic zoom, not a panel** — the soundboard is reached by zooming the
  game camera onto the `ControlRoom3D/SoundBoard` prop, mirroring the existing
  terminal camera-view pattern in `World3D`.
- **Overlay knob UI** — while framed, a native `CanvasLayer` overlay panel
  renders the knobs/faders/LEDs (same approach as `TerminalOverlay`, which
  fixed terminal interactivity).
- **Player must be at the desk** — the soundboard view is only openable when
  the player is near the desk (soundboard proximity area).
- **Offsets, not replacement** — knob positions add deltas over the equipment
  presets, so upgrades still matter and the minigame is "set to perfect", not
  "arbitrary knob dance".
- **Vern mood drain** — consequence for leaving the mix off-perfect while a
  caller is on air.

## Architecture

```
ScreenNavOverlay (CanvasLayer 121, <- / X)        -- owns nav buttons + close
World3D.SoundboardViewState (None/ZoomingIn/Open/ZoomingOut) -- camera zoom/pan
SoundboardOverlay (CanvasLayer 122)               -- knob UI while framed
SoundboardTargetGenerator                         -- computes target bands
SoundboardMixerDriver                             -- knob state -> AudioMixerManager
AudioMixerManager.ApplySoundboard(state)          -- applies bus effect deltas
SoundboardMonitor (DomainMonitor + observer)      -- Vern mood drain
Caller.SpeakingVolume                             -- per-caller variance input
```

## 1. Screen Navigation Overlay

- New `scripts/ui/ScreenNavOverlay.cs` (autoload-style service? No — plain node),
  a `CanvasLayer` at layer `121` (above terminal overlay `120`, below CRT HUD).
- Styled with `UITheme` constants (BG_BORDER, ACCENT_GOLD, MonoFont).
- Buttons:
  - `<-  Back` top-left → opens soundboard (terminal world) or returns from
    soundboard to terminal (soundboard world). Disabled/hidden when no view open.
  - `X` top-right → closes current view (terminal or soundboard).
- Visibility: shown while `World3D` has any view open; hidden otherwise (avoids
  clutter between views and matches the "only while a view is open" rule).
- Emits `BackRequested` / `CloseRequested`; `World3D` decides what each does
  based on current `ViewState`.

## 2. Diegetic Soundboard Zoom (World3D)

- Add `enum SoundboardViewState { None, ZoomingIn, Open, ZoomingOut }` mirroring
  `TerminalViewState`, plus `SoundboardZoomSpeed`, `SoundboardFramingWidth`,
  and camera offset/look constants tuned over the same parallax workflow used
  for the CRT (fit pass last).
- Target prop: `ControlRoom3D/SoundBoard` — instance `ExtResource("10")` at
  `(0.25, 0.9, -3.55)` rot `(0,180,0)`, **no attached script**. Compute the
  3D frame by reading the node's AABB (`GetAabb()`) at runtime each frame while
  `Open`/`ZoomingIn`.
- Openable only when player is near the desk: add a small proximity `Area3D`
  (mirror `ComputerTerminal3D.InteractionArea`) on a `SoundboardTrigger3D`
  helper node under the soundboard (or reuse room bounds + distance check).
- `<-` from terminal → `SoundboardViewState.ZoomingIn`; `X` or `E` from
  soundboard → `ZoomingOut` then back to the game; soundboard `<-` →
  `TerminalViewState.ZoomingIn` (switch view).
- While either view is open: hide `StatusLayer` (same as terminal), enable the
  relevant overlay, keep `InteractPrompt` hidden.

## 3. Mixer Model & Overlay

`SoundboardMixerDriver` holds a `SoundboardKnobState`:

| Field | Range | Meaning |
|-------|-------|---------|
| `CallerGain` | 0..1 | caller channel gain |
| `CallerLowPass` | 0..1 | caller low-pass cutoff offset |
| `CallerHighPass` | 0..1 | caller high-pass cutoff offset |
| `VernGain` | 0..1 | Vern channel gain |
| `AdsGain` | 0..1 | ads/bumper channel gain |
| `Fader` | 0..1 | master (music/program) fader |

`SoundboardOverlay` (CanvasLayer `122`):
- Channel rows with background strip + knob (vertical slider) + LED per knob:
  - **VERN** — always locked **GREEN** (fixed broadcast chain; Vern should not
    be fiddled).
  - **CALLER** — live; LEDs from `SoundboardTargetGenerator`.
  - **ADS/BUMPER** — lights up during ad breaks with a nominal target (green);
    dim otherwise.
- Master fader + overall LED (worst-of band).
- Shows the mood-drain status from `SoundboardMonitor` (small text + color).

## 4. Targets (SoundboardTargetGenerator)

Pure static/logic class, unit-testable.

- Inputs: `Caller.PhoneQuality`-derived equipment level (the "ideal" preset),
  per-caller `SpeakingVolume` variance, and the current `AudioEffectsProcessor`
  preset values.
- Produces per-knob target bands (Blue nominal / Green perfect / Yellow /
  Red bad), where **Green** = knob within `PerfectTolerance` of the target,
  **Blue** = within `AcceptableTolerance`, **Yellow** = requiring attention,
  **Red** = badly off.
- The Caller target center = equipment preset value ± small stable jitter seeded
  from `SpeakingVolume`, so callers are never exactly the same but always
  reachable near the preset.
- Vern / Ads targets = fixed nominal values (knob at the "clean" position).

## 5. Audio Application (AudioMixerManager)

Knob deltas are stacked **on top of** `AudioEffectsProcessor` presets:

| Knob | Effect target | Note |
|------|---------------|------|
| CallerGain | `_callerDistortionIndex.Drive`, `_callerAmplifyIndex.VolumeDb` | gain + a touch of drive |
| CallerLowPass | `_callerLowPassIndex.CutoffHz` | |
| CallerHighPass | `_callerHighPassIndex.CutoffHz` | |
| VernGain | Vern bus `VolumeDb` | |
| AdsGain | Ads/Bumper bus `VolumeDb` | |
| MasterFader | Music bus `VolumeDb` + program bus | |

- New method `AudioMixerManager.ApplySoundboard(SoundboardKnobState)` that
  computes each `CutoffHz`/`VolumeDb`/`Drive` = preset value + knob delta and
  applies via the existing effect indices/bus API.
- `UpdateAudioQuality()` re-applies the stored knob state after equip-level
  changes so equipment upgrades don't wipe knob positions.
- All knob deltas are **clamped** so effects never go fully silent/DC.

## 6. Vern Mood Drain (SoundboardMonitor)

`scripts/monitors/SoundboardMonitor.cs`:

- `DomainMonitor` subclass + `ICallerRepositoryObserver` (via
  `CallerRepository.Subscribe`).
- On `OnCallerOnAir`: start a **10s grace** timer.
- After grace, each frame while `OnAir` and any knob is off **Green**:
  drain `VernStats.Emotional` and `.Mental` at `GraceDrainRate`; every **5s**
  the drain accelerates (`DrainRateStep`), up to `MaxDrainRate`.
- On `OnCallerOnAirEnded`: reset timers + drain rate to base.
- Constants exposed for tuning; no UI logic (overlay polls state).
- Verifies target band via the same `SoundboardTargetGenerator` so monitor and
  UI cannot disagree.

## 7. 3D Soundboard Presentation (Shipped)

The interactive surface is a **diegetic 3D board** (branch `3d-migration`),
replacing the 2D `VSlider` overlay. Interaction mirrors the computer-terminal
pattern: fixed ortho camera → click-to-select → drag to adjust.

### Placement & Visuals

- `soundboard.glb` is regenerated so the **chassis is static and every movable
  part is a separately named sibling node**: `FaderCap_0..7`, `Knob_{ch}_{side}`
  (with a child `Index_{ch}_{side}` pointer), `Lamp_0..7`, `MasterKnob`, plus
  the `soundboard` chassis. The generator joins only static objects into the
  chassis and exports the movables alongside (`Tools/modelgen/generate.py`
  `MOVABLE` + selective `select_all(SELECT)` export).
- `scripts/world3d/SoundboardPhysicalLayout.cs` (KBTV.World3D) is the **pure,
  unit-testable** mapping of mixer controls → GLB part, drive kind, and channel
  lamp. No Godot types, so it runs under GoDotTest without a scene.
- `scripts/world3d/Soundboard3D.cs` is added by `ControlRoom3D._Ready` as a
  child of the `"SoundBoard"` GLB node **at identity** (`Position = Zero`, no
  rotation) and `AttachBoard(boardNode)` locates the named parts via
  `FindChild(name, recursive, owned: false)`, captures each part's rest
  transform, and builds the tap colliders. Parenting at identity makes part
  transforms share the board's local frame (the previous absolute placement at
  `(0.25, 0.9, -3.55)` yaw 180 did not match the instance's `x = -0.354`).
- **Axis mapping** (verified from the exported glTF translations; authoring
  `(x, y, z)` → glTF `(x, z, -y)`): fader caps slide along **glTF-local Z**
  (authoring vertical), knobs and `MasterKnob` spin around **glTF-local Y**.

### Control Slots (`SoundboardPhysicalLayout.Slots`)

Round 2 adds three per-channel output level faders and moves the caller gain up
the column, giving **9 controls** driven by the GLB's real movables:

| Control | GLB part | Drive | Channel lamp |
|---------|----------|-------|--------------|
| CallerGain | `Knob_6_2` | Knob | `Lamp_6` |
| CallerLowPass | `Knob_6_0` | Knob | `Lamp_6` |
| CallerHighPass | `Knob_6_1` | Knob | `Lamp_6` |
| CallerLevel | `FaderCap_6` | Fader | `Lamp_6` |
| VernGain | `Knob_7_2` | Knob | `Lamp_7` |
| VernLevel | `FaderCap_7` | Fader | `Lamp_7` |
| AdsGain | `Knob_5_2` | Knob | `Lamp_5` |
| AdsLevel | `FaderCap_5` | Fader | `Lamp_5` |
| Master | `MasterKnob` | Knob | `Lamp_3` |

Screen-bounded columns: **Caller** = column 6 (top gain knob + level fader +
two filter knobs), **Vern** = column 7 (gain + level), **Ads/Bumper** = column
5 (gain + level), **Master** = big centre knob. Columns 0–4 and 8 stay
cosmetic; `IdleLamps = { Lamp_0, Lamp_1, Lamp_2, Lamp_4 }`.

- Fader cap local Z: `FaderLocalZ(value) = (value - 0.5) * 2 * FaderTravel +
  FaderRestLocalZ` (`FaderTravel = 0.06`, `FaderRestLocalZ = -0.12`); value 0 is
  the front/bottom (`z = -0.18`), value 1 the back/top (`z = -0.06`).
- Knob rotation: `KnobRotationDeg(value) = (value - 0.5) * 2 * KnobTurnDeg +
  KnobRestOffsetDeg` (`KnobTurnDeg = 90`, `KnobRestOffsetDeg = 180`). The notch
  points **up at rest** (180°), min = 135°, max = 225°; the knob's child index
  follows.
- Lamps are driven via per-lamp `MaterialOverride` emission (only while handles
  are visible, once per frame): `Lamp_6` = caller worst band
  (`SoundboardTargetGenerator.GetWorstBand(SoundboardMonitor.CallerBands)`),
  `Lamp_7` = Vern steady green, `Lamp_5` = Ads green while
  `AdManager.IsAdBreakActive` else dim, `Lamp_3` = Master steady green,
  `Lamp_0/1/2/4` dim. The selected **and** hovered control brightens its
  channel lamp.

### Hover Affordance (Round 2)

- `Soundboard3D.SetHover(SoundboardControl)` drives a single shared hover
  quad + per-band color, recomputed every frame while handles are visible:
  - **Halo**: one `MeshInstance3D` with a `QuadMesh` rotated `-90°` about X
    (facing up), unshaded alpha `StandardMaterial3D` with
    `NoDepthTest = true` and a radial `GradientTexture2D` (white centre →
    transparent edge, `Fill = Radial`, `FillFrom (0.5,0.5)`, `FillTo (1,0.5)`).
    Positioned `HaloLift (0.02)` above the part (`+Y`, board-local) and
    `HaloTowardCameraZ (0.045)` toward the operator (`-Z`); hidden when hover =
    `None`.
  - **Color** = the control's live band: caller knobs use the caller bands,
    everything else compares its current value to neutral
    (`SoundboardTargetGenerator.GetControlBand`). Green `(0.3,1,0.55)`,
    blue/cyan `(0.35,0.9,1.0)`, yellow `(1.0,0.85,0.2)`, red `(1.0,0.25,0.2)`.
  - The hovered part's mesh gets a shared blue emissive `MaterialOverride`
    (cleared on leave).
- `World3D.PollSoundboardMouse()` now also **flips the fader drag sign** (drag
  **up** increases, `deltaY = mouse Y − last Y`) and raycasts each frame for
  hover: no control under the cursor → `SetHover(None)` (also forced when the
  GUI is hovered, e.g. the drain-status label); while dragging, hover follows
  `_boardSelected` so the halo tracks the active control.

### Interaction (World3D)

- Each part gets an invisible tap collider on `Soundboard3D.HitLayer`
  (`1u << 20`, `CollisionMask = 0`) anchored to its rest position (fader
  `0.08 × 0.06 × 0.22`, knob `0.11 × 0.06 × 0.14`, master `0.2 × 0.08 × 0.18`)
  so only the board raycast hits.
- `World3D.PollSoundboardMouse()` runs while `SoundboardViewState.Open`:
  click raycasts (`PhysicsRayQueryParameters3D`, mask = `HitLayer`), selects
  the control, then incremental vertical drag rebases each frame from
  `CurrentValue`.
- `SoundboardControlApplier.ValueFromDrag(start, dragDeltaScreenY,
  SoundboardDragPixelsPerUnit)` — drag **up** increases; result clamped to
  0..1; zero-ppu falls back to a unit step. Pure + unit-tested
  (`tests/unit/audio/SoundboardControlApplierTests.cs`).
- Show/hide: `ShowHandles()` (reset driver to neutral + apply, enable bodies) /
  `HideHandles()` (disable bodies); driven by the zoom transitions and
  `OnNavBackRequested`. `UpdateControls`/`UpdateLeds` run only while handles are
  visible; overlay show/hide is unchanged.

### Camera

- `SoundboardFramingWidth = 3.2f`, `SoundboardCameraDistance = 2.0f`,
  `SoundboardElevationDeg = 75f` (steep look-down), `SoundboardBottomBand =
  0.27f`; camera pos = board center + `(0, tan(75°)·dist, dist)`, look target
  = center + `(0, SoundboardLookPivotHeight - SoundboardBottomBand·2·size, 0)`
  so the board sits in the upper ~73% and the bottom ~27% stays clear for the
  transcript overlay. `ComputeSoundboardFrame` measures the **combined AABB of
  every mesh under the board** (`CombinedMeshAabb`) because the regenerated
  board root is a plain `Node3D` (`VisualInstance3D.GetAabb` is empty).

### Overlay

- `SoundboardOverlay` now renders only the drain-status label (OFF AIR /
  `GRACE n s` / `DRAIN -x/s` / `MIXED PERFECT`), `MouseFilterEnum.Ignore`,
  top-center, so it never blocks the 3D board raycast. Keeps `Driver`,
  `SetMonitor`, `ShowSoundboard`, `HideSoundboard`.

## 8. Files

**New**
- `scripts/audio/SoundboardControlApplier.cs`
- `scripts/world3d/Soundboard3D.cs`
- `scripts/world3d/SoundboardPhysicalLayout.cs` (pure layout, no Godot types)
- `scripts/ui/ScreenNavOverlay.cs`
- `scripts/audio/SoundboardKnobState.cs`
- `scripts/audio/SoundboardTargetGenerator.cs`
- `scripts/audio/SoundboardMixerDriver.cs`
- `scripts/ui/SoundboardOverlay.cs`
- `scripts/monitors/SoundboardMonitor.cs`
- `docs/systems/SOUNDBOARD_DESIGN.md`

**Modified**
- `scripts/world3d/ControlRoom3D.cs` - parent `Soundboard3D` under the `SoundBoard` GLB node at identity + `AttachBoard`.
- `scripts/world3d/World3D.cs` - `SoundboardElevationDeg 75°`, `SoundboardBottomBand`, `CombinedMeshAabb`.
- `scripts/audio/AudioMixerManager.cs` - `ApplySoundboard`, re-apply in `UpdateAudioQuality`.
- `scripts/callers/Caller.cs`, `scripts/callers/CallerGenerator.cs` - `SpeakingVolume`.

**Round 2 (modified)**
- `scripts/world3d/SoundboardPhysicalLayout.cs` - 9 controls (added gain knobs + level faders), `KnobRestOffsetDeg = 180f`, `IdleLamps`.
- `scripts/world3d/Soundboard3D.cs` - `SetHover`, hover halo + per-band color, hovered-part emissive, `UpdateLeds` lamp remap, `IsLampHighlighted`.
- `scripts/world3d/World3D.cs` - fader drag-sign flip (up = increase), per-frame hover raycast, `SetHover(None)` over GUI.
- `scripts/audio/SoundboardKnobState.cs` - `CallerLevel`/`VernLevel`/`AdsLevel`, `NormalizedDelta`, 14-field `SoundboardEffectSettings`.
- `scripts/audio/SoundboardControlApplier.cs` - `CallerLevel`/`VernLevel`/`AdsLevel`.
- `scripts/audio/SoundboardMixerDriver.cs` - fully additive DSP (level faders, per-channel gain/muffle), `LevelMinDb/MaxDb`.
- `scripts/audio/SoundboardTargetGenerator.cs` - `PerfectTolerance = 0.09f`, `GetControlBand`.
- `scripts/audio/AudioMixerManager.cs` - muffle low-pass indices, per-channel level buses.

**Tests**
- `tests/unit/audio/SoundboardControlApplierTests.cs`
- `tests/unit/audio/SoundboardTargetGeneratorTests.cs`
- `tests/unit/audio/SoundboardMixerDriverTests.cs`
- `tests/unit/monitors/SoundboardMonitorTests.cs`
- `tests/unit/world3d/SoundboardPhysicalLayoutTests.cs`

## 9. Tuning References

- Terminal pattern: `TerminalViewState` enum, `TerminalZoomSpeed = 3.2f`,
  `TerminalFramingWidth = 1.85f`, camera offset `(0.09, 0.12, 1.42)`,
  look `(-0.02, -0.08, 0)`.
- Soundboard: `SoundboardFramingWidth = 3.2f`, `SoundboardCameraDistance = 2.0f`,
  `SoundboardElevationDeg = 75f`, `SoundboardBottomBand = 0.27f` (tune
  0.20–0.27 to shift the look-target up/down for the transcript overlay band);
  `SoundboardLookPivotHeight = 0.06f`, `SoundboardDragPixelsPerUnit = 220f`.
  Bottom-band framing shifts the look target down by `BottomBand × 2 × size`.
- Soundboard DSP (round 2, `SoundboardMixerDriver` — fully additive, neutral =
  equipment preset unchanged):
  - CallerGain → `CallerAmplifySpanDb = 10` (amplify `max(gainΔ,0)·10` dB),
    drive `clamp(preset.Distortion + gainΔ·0.15, 0.05, 0.8)`, muffle
    `lerp(MuffleTransparentHz → MuffleMuffledHz, max(-gainΔ,0))` with
    `MuffleTransparentHz = 20000`, `MuffleMuffledHz = 500`.
  - CallerLowPass/CallerHighPass → additive `CutoffHz` on the caller bus.
  - VernGain/AdsGain → `VernGainSpanDb = AdsGainSpanDb = 8` dB added on the
    Vern/Sfx buses; each muffles below neutral.
  - CallerLevel/VernLevel/AdsLevel → `{Channel}LevelSpanDb = 8`, min `-24`,
    max `+8` dB, applied as `CallerLevelDb` (caller bus),
    `VernLevelDb + VernGainDb` (Vern bus), `AdsLevelDb + AdsGainDb` (Sfx bus).
  - Master (`Fader`) → `MasterFaderSpanDb = 4` over `MasterFaderMinDb = -12` /
    `MasterFaderMaxDb = 4`, added into music + master buses.
- Targets (`SoundboardTargetGenerator`): `PerfectTolerance = 0.09f`,
  `AcceptableTolerance = 0.12f`; caller targets center on
  `NeutralValue + SpeakingVolume jitter`; `GetControlBand(state, control,
  speakingVolume)` returns the caller bands for caller knobs, otherwise the
  value-vs-neutral band.
- Hover halo (`Soundboard3D`): `HaloSize = 0.24` quad, `HaloLift = 0.02`,
  `HaloTowardCameraZ = 0.045`; band colors Green `(0.3,1,0.55)` /
  Blue `(0.35,0.9,1.0)` / Yellow `(1.0,0.85,0.2)` / Red `(1.0,0.25,0.2)`.
- Full DSP when the engine's ipso bus values are in the air, or the in-editor
  fit pass shows the notch/fader moving the wrong way: flip only
  `World3D.cs` line 1094 (`deltaY = _boardLastDragScreenY - mousePosition.Y`
  → `mousePosition.Y - _boardLastDragScreenY`), never the GLB geometry.
- Audio indices/presets: `AudioMixerManager` `CallerPresets` L1..L4
  (low-pass 600/800/1200/2500 Hz), `AudioEffectsProcessor` `EffectPresets`
  (2000/3500/6000/10000). Knob deltas should keep L4 the floor/best.
- CRIT calibration: `TerminalOverlay` layer `120`; use `121`/`122` for nav + soundboard overlays.