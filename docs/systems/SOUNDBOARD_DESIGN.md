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
- Produces per-knob target bands, directional around the target amount (Round 3):
  - **Green** = knob within `PerfectTolerance` of the target (the only "perfect"
    band; nothing below it is usable)
  - **Above** the target: **Yellow** (≤ `YellowTolerance` beyond perfect) / **Red**
    (farther above)
  - **Below** the target: **Cyan** (≤ `CyanTolerance` beyond perfect) / **Blue**
    (farther below)
- **Per-caller targets** (Round 7): each caller carries a `SoundboardSeed`
  (assigned in `CallerGenerator` via `(int)GD.Randi()`, not persisted). The
  Caller target for knob *k* = `Clamp(0.5 + volumeJitter + perKnobJitter_k,
  MinTargetKnob, MaxTargetKnob)` (`0.25..0.75`), where `perKnobJitter_k =
  (HashUnit(seed, salt_k) - 0.5f) * 2f * PerKnobJitterRange (0.11)` via a
  deterministic MurmurHash3-finalizer hash. So every knob of every caller is a
  different but always reachable target; `NeutralCallerTargets()` = all 0.5
  (used when no caller is on air).
- Vern / Ads targets = neutral 0.5 (the "clean" position) — their gain knobs and
  level faders grade against neutral, not a per-caller target.
- Hover color uses the **continuous** blue→cyan→green→yellow→red ramp over the
  signed knob error (`ColorForError`, ramp half-span `ColorRampHalfSpan = 0.30`);
  the discrete LEDs stay 5-band.

## 5. Audio Application (AudioMixerManager)

Knob deltas are stacked **on top of** `AudioEffectsProcessor` presets:

| Knob | Effect target | Note |
|------|---------------|------|
| CallerGain | `_callerDistortionIndex.Drive`, `_callerAmplifyIndex.VolumeDb`, compression | above target: drive + compress, NEVER louder |
| CallerLowPass | `_callerLowPassIndex.CutoffHz` | grades against its per-caller target |
| CallerHighPass | `_callerHighPassIndex.CutoffHz` | grades against its per-caller target |
| VernGain/AdsGain | Vern/SFX bus distortion + compression | neutral target; above → drive/compress, never louder |
| CallerLevel/VernLevel/AdsLevel | per-bus strip volume + compression | neutral; above → capped 0 dB, excess → compress |
| MasterFader | Music bus `VolumeDb` + program bus | unchanged |

- New method `AudioMixerManager.ApplySoundboard(SoundboardKnobState)` computes
  the `SoundboardEffectSettings` (17 fields) and applies via the existing bus
  indices; `SoundboardMixerDriver.ComputeEffectSettings(state, preset, targets)`
  is the pure calculation. Caller knobs grade against the per-caller target
  knob values, Vern/Ads against neutral.
- **Base-preserving** (R3/R4): the caller amplify effect keeps the phone-preset
  loudness as a fixed baseline (`CallerBaseAmplifyDb = 8`); gaining a knob above
  target NEVER raises volume — the excess becomes distortion drive +
  compression. Below target, the caller just gets softer/clearer (amplify offset
  `-below·6 dB`, never a cut of the preset) and the filter knobs sweep the
  low/high-pass cutoffs. Vern/Ads behave the same way above their neutral target.
- Level faders are `Clamp(delta·30, -30, 0)` — pulling a strip below neutral
  lowers that bus; pushing above neutral caps at 0 dB and feeds the excess into
  drive/compression. Master/Music faders are unchanged.
- `UpdateAudioQuality()` re-applies the stored knob state + stored caller targets
  after equip-level changes so equipment upgrades don't wipe knob positions.
- **Board defaults & persistence (Round 8)**: `SoundboardKnobState.Default()`
  rests every knob at 0.5 (= 12 o'clock), the Caller/Vern level faders at 50%,
  and the Ads fader at the very bottom (`0`). Literal 0% is applied
  (user-confirmed): the rested Ads fader cuts the SFX bus (ads/bumpers + UI
  sounds) to −30 dB until the player raises it. `SoundboardKnobState.Neutral()`
  (all controls 0.5) stays the pure DSP baseline for the effect stack, and
  `SoundboardMixerDriver.ResetToDefault()` restores the resting values. The mix
  persists across board visits — see §7 Show/hide.
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
5 (gain + level), **Master** = big centre knob. Columns 0–4 stay cosmetic;
`IdleLamps = { Lamp_0, Lamp_1, Lamp_2, Lamp_4 }`. Round 9 parks the cosmetic
control surface at board defaults so the full face reads uniformly: every
unused `Knob_*` (columns 0–4 plus the non-gain knobs of the Vern/Ads columns)
is pinned to 180° = **12 o'clock** and every unused `FaderCap_*` to the bottom
(`FaderLocalZ(0)` = 0%) at attach time; only the 9 driven slots move (from
driver state).

- Fader cap local Z: `FaderLocalZ(value) = (value - 0.5) * 2 * FaderTravel +
  FaderRestLocalZ` (`FaderTravel = 0.05`, `FaderRestLocalZ = -0.14`); value 0 is
  the front/bottom (`z = -0.19`), value 1 the back/top (`z = -0.09`). Round 4
  moved the physical fader track/caps down in the source model (authoring
  `y = 0.17`) to clear the tightened knob stack; Round 5 relaxes the gap
  (`y = 0.14`, knob rows `(-0.12, -0.07, -0.02)`) so knobs sit lower and faders
  higher. Round 6 spreads the knob stack further apart (spacing `0.05 → 0.065`,
  rows rebalanced to `(-0.11, -0.045, 0.02)`, `0.065` apart) and moves the
  per-channel lamp
  (`y 0.011 → 0.055`) to sit between the lowest knob row and the fader track.
- Knob rotation: `KnobRotationDeg(value) = KnobRestOffsetDeg - (value - 0.5) *
  KnobTurnDeg` (`KnobTurnDeg = 270`, `KnobRestOffsetDeg = 180`). Rest sits at
  180° = **12 o'clock**; value 0 = 315° (down-right) and value 1 = 45° (up-right),
  so value 0→1 swings counterclockwise in degrees = **clockwise on screen**, with
  the 12 o'clock notch exactly centred in the travel; the knob's child index
  follows. Round 7 widened the swing from 90° to 270° so one turn reads as a full
  three-quarter sweep between the notch stops.
- Lamps are driven via per-lamp `MaterialOverride` emission (only while handles
  are visible, once per frame): `Lamp_6` = caller worst band
  (`SoundboardTargetGenerator.GetWorstBand(SoundboardMonitor.CallerBands)`),
  `Lamp_7` = Vern steady green, `Lamp_5` = Ads green while
  `AdManager.IsAdBreakActive` else dim, `Lamp_3` = Master steady green,
  `Lamp_0/1/2/4` dim. The selected **and** hovered control brightens its
  channel lamp.

### Hover Affordance (Round 2 → R3: alignment + glow)

- `Soundboard3D.SetHover(SoundboardControl)` drives a single shared hover
  quad, recomputed every frame while handles are visible:
  - **Halo** (Round 3): one `MeshInstance3D` with a `QuadMesh` rotated `-90°`
    about X (facing up), unshaded alpha `StandardMaterial3D` with **depth test
    enabled** (the knob/fader body occludes the disc centre, leaving a soft ring
    of light pooling under the handle — no whole-mesh emissive tint on the part)
    and a radial `GradientTexture2D` (white centre → transparent edge,
    `Fill = Radial`, `FillFrom (0.5,0.5)`, `FillTo (1,0.5)`).
  - **Positioning** (fixes the old misalignment): the halo is anchored at
    exactly the part's own position + a tiny `HaloLift (0.008)` in `+Y`
    (board-local). `Soundboard3D` is parented at identity under the board GLB,
    so `part.Position` is the correct centre under the cursor from any camera
angle — the old board-local `HaloTowardCameraZ` offset is gone. Round 4
    shrinks this to `HaloSize = 0.08`, just larger than the knob caps; Round 5
    tightens it another 25% to `HaloSize = 0.06` (still small enough to sit
    under the handle and read as a ring). `HaloAlpha
    = 0.55` (centre alpha; gradient fades the edges).
  - **Color** (Round 3) = the continuous `ColorForError` ramp from the signed
    control error (`SoundboardTargetGenerator.GetControlError`): caller knobs
    blend against their per-caller target (using the **live caller
    `SpeakingVolume`** via `SoundboardMonitor.CallerSpeakingVolume`, falling back
    to 0.5), everything else against neutral. So even a *nearly-right* knob shows
    cyan (slightly low) / yellow (slightly high) rather than jumping to a band.
    Exact band colors match the ramp: `RampBlue/Cyan/Green/Yellow/Red`.
- `World3D.PollSoundboardMouse()` raycasts each frame for hover: no control
  under the cursor → `SetHover(None)` (also forced when the GUI is hovered);
  while dragging, hover follows `_boardSelected` so the halo tracks the active
  control.

### Interaction (World3D)

- Each part gets an invisible tap collider on `Soundboard3D.HitLayer`
  (`1u << 20`, `CollisionMask = 0`) anchored to its control handle (fader cap
  `0.055 × 0.05 × 0.04`, knob `0.075 × 0.05 × 0.05`, master `0.17 × 0.07 × 0.15`)
  so only the board raycast hits. Knob depth stays under the `0.065` row pitch so
  adjacent hitboxes never overlap under the oblique camera. Round 4 makes fader
  colliders follow the moving cap, so hover/click registers on the fader control
  itself, not the whole track.
- `World3D.PollSoundboardMouse()` runs while `SoundboardViewState.Open`:
  click raycasts (`PhysicsRayQueryParameters3D`, mask = `HitLayer`), selects
  the control, then incremental vertical drag rebases each frame from
  `CurrentValue`.
- `SoundboardControlApplier.ValueFromDrag(start, dragDeltaScreenY,
  SoundboardDragPixelsPerUnit)` — drag **up** increases; result clamped to
  0..1; zero-ppu falls back to a unit step. Pure + unit-tested
  (`tests/unit/audio/SoundboardControlApplierTests.cs`).
- Show/hide: `ShowHandles()` / `HideHandles()` enable/disable the tap bodies and
  re-push the mix via `Driver.Apply()`; driven by the zoom transitions and
  `OnNavBackRequested`. `UpdateControls`/`UpdateLeds` run only while handles are
  visible; `SoundboardOverlay.ShowSoundboard()` also re-applies. **Round 8 —
  persistence**: the shared `SoundboardMixerDriver` (with its `State`) and
  `AudioMixerManager._soundboardState` are session-long nodes owned by
  `World3D`, and the old `Driver.ResetToNeutral()` calls inside
  `ShowHandles()`/`ShowSoundboard()` (which wiped every control on zoom-in) are
  removed — knob/fader positions and the applied DSP survive walking away from
  and returning to the board.

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

**Round 3 (modified)**
- `scripts/audio/SoundboardTargetGenerator.cs` - directional `SoundboardBand`
  enum (`None/Green/Cyan/Yellow/Blue/Red`), `CyanTolerance = YellowTolerance =
  0.12f`, severity-based `GetWorstBand`, `GetControlError` (signed),
  `ColorForError` (continuous ramp), `Ramp*` colors, `ColorRampHalfSpan`,
  `None` control → `None` band.
- `scripts/world3d/Soundboard3D.cs` - hover halo tuned + recentered on
  `part.Position`, depth-tested, removed `_hoverMaterial`/`_hoverPart` emissive
  tint + `HoveredControlBand`, `UpdateHoverVisual` uses
  `ColorForError(GetControlError(...))`, slim tap colliders, `BandColor`/lamps
  use `Ramp*` colors.
- `scripts/monitors/SoundboardMonitor.cs` - `CallerSpeakingVolume` exposure for
  halo tint accuracy.

**Round 4 (modified)**
- `Tools/modelgen/soundboard.py`, `assets/models3d/props/soundboard.glb`,
  `Tools/modelgen/source/soundboard.blend`, `docs/art/model_previews/soundboard.{json,png}` - tightened/lifted knob rows, lowered fader track/caps, regenerated model.
- `scripts/world3d/SoundboardPhysicalLayout.cs` - `FaderRestLocalZ = -0.17`,
  `FaderTravel = 0.05`, value-up/drag-up knob rotation is clockwise with a total
  90° swing.
- `scripts/world3d/Soundboard3D.cs` - fader hitboxes are cap-sized and follow the
  moving cap; halo shrunk to `HaloSize = 0.08`.
- `scripts/audio/SoundboardMixerDriver.cs` - exaggerated DSP spans for more
  obvious/fun knob response while retaining clamps.
- `scripts/audio/AudioMixerManager.cs` - muffle low-pass indices, per-channel level buses.

**Round 5 (modified)**
- `Tools/modelgen/soundboard.py`, `assets/models3d/props/soundboard.glb`,
  `Tools/modelgen/source/soundboard.blend`, `docs/art/model_previews/soundboard.{json,png}` - relaxed the R4 over-correction: knob rows now `(-0.12, -0.07, -0.02)` and fader track/caps at authoring `y = 0.14` (track `0.13` long), so knobs sit lower and faders higher with a smaller visual gap. Regenerated model.
- `scripts/world3d/SoundboardPhysicalLayout.cs` - `FaderRestLocalZ = -0.14`
  (matches the new authoring fader Y).
- `scripts/world3d/Soundboard3D.cs` - halo tightened another 25% to
  `HaloSize = 0.06`.

**Round 6 (modified)**
- `Tools/modelgen/soundboard.py`, `assets/models3d/props/soundboard.glb`,
  `Tools/modelgen/source/soundboard.blend`, `docs/art/model_previews/soundboard.{json,png}` - feedback pass: knob rows spread wider (spacing `0.05 → 0.065`, rows `(-0.10, -0.035, 0.02)`) and per-channel lamp moved forward to `y = 0.055` so it stays visible below the knob stack and clear of the fader track. Regenerated model.
- Follow-up fix: rows rebalanced to truly even spacing (rows `(-0.11, -0.045, 0.02)`, `0.065` apart) and knob tap-collider depth reduced `0.11 → 0.05` so hover no longer grabs a neighbour row under the oblique camera. Regenerated model (dims unchanged).

**Round 7 (modified) — interaction + DSP rounds**
- `scripts/world3d/SoundboardPhysicalLayout.cs` - knob rotation endpoints: `KnobTurnDeg = 270`, `KnobRestOffsetDeg = 180` (value 0 → 315°, rest 0.5 → 180° = 12 o'clock, value 1 → 45°).
- `scripts/callers/Caller.cs`, `scripts/callers/CallerGenerator.cs` - per-caller `SoundboardSeed`.
- `scripts/audio/SoundboardTargetGenerator.cs` - `HashUnit`, per-knob salts, `PerKnobJitterRange`, `Min/MaxTargetKnob`, `NeutralCallerTargets`, seed-aware targets/bands/error.
- `scripts/audio/SoundboardMixerDriver.cs` - 17-field `SoundboardEffectSettings`, `NormalizedDeltaFrom`, target-aware `ComputeEffectSettings`, `SetCallerTargets`.
- `scripts/audio/AudioMixerManager.cs` - `CallerBaseAmplifyDb = 8`, Vern/SFX drive + compression, `TuneCompressor`, above-target → drive/compress (never louder).
- `scripts/monitors/SoundboardMonitor.cs`, `scripts/world3d/Soundboard3D.cs` - seed plumbing for bands + hover error.

**Round 8 (modified) — board defaults & persistence**
- `scripts/audio/SoundboardKnobState.cs` - `AdsLevel` default `0`; split
  `Neutral()` (all controls 0.5, pure DSP baseline) from `Default()` (knobs 0.5,
  Caller/Vern faders 0.5, Ads fader 0); added `ResetToDefault()`.
- `scripts/audio/SoundboardMixerDriver.cs` - `State` starts at `Default()`;
  `ResetToNeutral()` → `ResetToDefault()`.
- `scripts/ui/SoundboardOverlay.cs`, `scripts/world3d/Soundboard3D.cs` -
  removed `Driver.ResetToNeutral()` from `ShowSoundboard()`/`ShowHandles()`
  (kept `Apply()`) so the knob/fader mix and its DSP persist across board visits.

**Round 9 (modified) — full-face default parking**
- `scripts/world3d/Soundboard3D.cs` - `NormalizeUnusedParts()` runs once at
  `AttachBoard`: every cosmetic `Knob_*`/`FaderCap_*` part not in `Slots` is
  parked at the resting defaults — knobs at 180° = 12 o'clock
  (`KnobRestOffsetDeg`), unused fader caps at the bottom (`FaderLocalZ(0)` = 0%).
  Driven slots are skipped (they come from driver state); cosmetic parts have no
  slots so they never move again. Fixes the board face showing the GLB-authored
  knobs/fader caps drifting off default (e.g. the non-gain knobs of the
  Vern/Ads columns and every knob/fader of columns 0–4).
- User-confirmed rule this implements: all knobs on the board sit at 12 o'clock
  (including Vern's, Ads', and unused ones); faders default to 0% except Vern
  and Caller, which stay at 50%.

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
- Soundboard DSP (Round 7 base-preserving tuning, `SoundboardMixerDriver` —
  `SoundboardEffectSettings` has 17 fields; neutral = all zeros = equipment
  preset unchanged; caller knobs grade vs per-caller targets, Vern/Ads vs
  neutral). Caveat: `AudioEffectDistortion` with `Drive = 0` (the neutral audio
  state) is assumed transparent in Godot — confirm no audible coloration while
  at rest in-engine.
  - CallerGain → drive `clamp(preset.Distortion + callerTotalOver·0.35, 0.05,
    0.95)`; amplify offset `-gainBelow·CallerAttenuateSpanDb (6)` dB (`Min/Max`
    -6/0) over `AudioMixerManager.CallerBaseAmplifyDb (8)` — at/above target the
    caller stays at the preset baseline, NEVER louder; compression = totalOver.
    Below target: knobs go softer/clearer; low LP/HP sweep `CutoffHz` spans
    `CallerLowPassSpanHz = 3000`, `CallerHighPassSpanHz = 1200`.
  - Vern/Ads → drive `clamp(totalOver·VernDriveSpan/AdsDriveSpan (0.55), 0, 1)`,
    compression = totalOver, muffle `lerp(MuffleTransparentHz → MuffleMuffledHz,
    belowDepth)` with `MuffleTransparentHz = 20000`, `MuffleMuffledHz = 220`.
  - CallerLevel/VernLevel/AdsLevel → `Clamp(delta·30, -30, 0)` dB strip volume
    (below neutral lowers the bus; above neutral caps at 0 dB and the excess
    feeds drive/compression).
  - Master (`Fader`) → `MusicFaderSpanDb = 14` and `MasterFaderSpanDb = 8`, with
    music clamped `-30..+14` dB and master clamped `-12..+8` dB (unchanged).
  - Compression (`AudioMixerManager.TuneCompressor`): caller
    `Lerp(-18→-28 dB, ratio 4→12)`, vern `Lerp(-20→-30, 3→10)`, ads/SFX
    `Lerp(-12→-28, 2→10)`.
- Targets (`SoundboardTargetGenerator`): `PerfectTolerance = 0.09f`,
  `YellowTolerance = CyanTolerance = 0.12f` (directional bands, Round 3);
  per-caller targets (Round 7) = `Clamp(0.5 + volumeJitter + perKnobJitter_k,
  0.25, 0.75)` with `PerKnobJitterRange = 0.11` and `HashUnit(seed, salt_k)`
  (salts `GainSalt 0x475F6911`, `LowPassSalt 0x6F502F01`, `HighPassSalt
  0x48503401`); `GetControlBand/GetControlError` take the caller seed (0 = no
  caller → neutral targets) and return neutral-based grades for
  Vern/Ads/Master controls; `None` control → `None`.
  `ColorRampHalfSpan = 0.30f` (full ramp half-travel: red at ±0.30, exact
  cyan/yellow at ±0.15, green at 0) with `RampBlue (0.3,0.6,1)`,
  `RampCyan (0.25,0.95,1)`, `RampGreen (0.2,0.9,0.3)`,
  `RampYellow (1,0.8,0.2)`, `RampRed (0.9,0.25,0.2)`.
- Hover halo (`Soundboard3D`, Round 5): squarish `HaloSize = 0.06` quad,
  `HaloLift = 0.008`, centre `HaloAlpha = 0.55`, depth-tested (no
  `NoDepthTest`), no per-part emissive override, centered exactly on
  `part.Position`; color = `ColorForError(GetControlError(...))` with the live
  caller `SpeakingVolume` for caller knobs.
- Full DSP when the engine's ipso bus values are in the air, or the in-editor
  fit pass shows the notch/fader moving the wrong way: flip only
  `World3D.cs` line 1094 (`deltaY = _boardLastDragScreenY - mousePosition.Y`
  → `mousePosition.Y - _boardLastDragScreenY`), never the GLB geometry.
- Audio indices/presets: `AudioMixerManager` `CallerPresets` L1..L4
  (low-pass 600/800/1200/2500 Hz), `AudioEffectsProcessor` `EffectPresets`
  (2000/3500/6000/10000). Knob deltas should keep L4 the floor/best.
- CRIT calibration: `TerminalOverlay` layer `120`; use `121`/`122` for nav + soundboard overlays.
