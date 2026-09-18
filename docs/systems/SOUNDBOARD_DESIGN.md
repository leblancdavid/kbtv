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
   presets in `AudioMixerManager`. Getting the levels wrong drains Vern's
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
  per-caller `SpeakingVolume` variance, and the `AudioMixerManager` preset values.
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
  MinTargetKnob (0), MaxTargetKnob (1))`, where `perKnobJitter_k =
  (HashUnit(seed, salt_k) - 0.5f) * 2f * PerKnobJitterRange (0.2)` via a
  deterministic MurmurHash3-finalizer hash. There are **four** caller targets —
  Gain, LowPass, HighPass and the **CallerLevel fader** (`VolumeSalt`) — each
  independently jittered so no two callers share an ideal position; the modest
  ±0.2 spread (R12) keeps every target near the phone-ideal, so a correct mix
  always sounds like the same clean, intelligible caller.
  `NeutralCallerTargets()` = all 0.5 (used when no caller is on air).
- Vern / Ads targets = neutral 0.5 (the "clean" position) — their gain knobs and
  level faders grade against neutral, not a per-caller target.
- Hover color uses the **continuous** blue→cyan→green→yellow→red ramp over the
  signed knob error (`ColorForError`, ramp half-span `ColorRampHalfSpan = 0.30`);
  the discrete LEDs stay 5-band.

## 5. Audio Application (AudioMixerManager)

Knob deltas are stacked **on top of** the equipment phone presets in
`AudioMixerManager.CallerPresets` (light telephone EQ, always intelligible):

| Knob | Effect target | Note |
|------|---------------|------|
| CallerGain | `_callerDistortionIndex.Drive`, `_callerAmplifyIndex.VolumeDb` | real trim: above target is audibly louder + rougher (drive); below is softer |
| CallerLowPass | `_callerLowPassIndex.CutoffHz` | grades against its per-caller target (±1400 Hz span) |
| CallerHighPass | `_callerHighPassIndex.CutoffHz` | grades against its per-caller target (±600 Hz span) |
| VernGain/AdsGain | Vern/SFX bus distortion + compression | neutral target; above → drive/compress, never louder |
| CallerLevel | caller bus strip volume | its own per-caller fader target; above → capped 0 dB, excess → compress |
| VernLevel/AdsLevel | Vern/SFX bus strip volume + compression | neutral; above → capped 0 dB, excess → compress |
| MasterFader | Music bus `VolumeDb` + program bus | unchanged |

- **Caller bus chain order**: board adjustments are applied to the **clean**
  caller voice first, then the quality-based phone-line effects color the
  result. Chain = Amplify (real trim) → Compressor (fixed normalize) → LowPass
  → HighPass → Distortion → EQ → Muffle → **Limiter (last, fixed output
  protection)**. `AudioMixerManager.ConfigureCallerBus()` builds this order;
  effect indices are derived from `GetBusEffectCount()`. The CallerLevel fader
  is a bus volume, applied at the bus output regardless of effect order.

- New method `AudioMixerManager.ApplySoundboard(SoundboardKnobState)` computes
  the `SoundboardEffectSettings` (17 fields) and applies via the existing bus
  indices; `SoundboardMixerDriver.ComputeEffectSettings(state, preset, targets)`
  is the pure calculation. Caller knobs grade against the per-caller target
  knob values, Vern/Ads against neutral.
- **Real trim (R12)**: the Caller gain knob is genuine loudness control, not a
  penalty. It clamps to `±CallerAmplifySpanDb` (±10 dB) around 0 dB; pushing above
  target makes the caller audibly louder **and** rougher (drive — `CallerDrive`
  feeds the distortion `Drive`), pulling below makes them softer. The old
  "above-target never louder" rule is gone for Caller (Vern/Ads still score
  excess as drive + compression only). The compressor is **fixed** glue
  (threshold −18 dB, ratio 4, makeup `CALLER_COMPRESSOR_GAIN = 5 dB`) that
  normalizes every caller to the same intelligible level — it no longer tightens
  when a knob sits above target. The `AudioEffectLimiter` (threshold −1 dB) added
  last on the caller bus keeps the hot end (full trim + full drive) from hard
  clipping, so abuse sounds "rough, not painful". `CallerCompression` remains
  computed for the score/UI.
- **Always audible**: filter spans are deliberately modest — LowPass ±1400 Hz,
  HighPass ±600 Hz swing a caller duller/thinner but can never undo the phone
  band and make them clearer, and never silence them. Pulling the gain/level
  below target softens + muffles (`MuffleMuffledHz = 1000`, a dull but
  intelligible floor) instead of cutting; the bus limiter holds the floor.
- Level faders: Caller is `Clamp(delta·18, -18, 0)` (bottoming muffles but never
  cuts); Vern/Ads stay `Clamp(delta·30, -30, 0)`. Master/Music faders are
  unchanged.
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

### Hover Affordance (Round 2 → R10: always-on per-ring glow)

- `Soundboard3D` keeps **one persistent halo ring per driven control**, visible
  whenever the handles are shown — no hovering required, so every knob/fader
  reads its live status at a glance. Built once at `_Ready` by `BuildHalos()`
  into `_halos`/`_haloMaterials`, one entry per `SoundboardPhysicalLayout.Slots`:
  - **Ring** (R3 recipe, extended to all slots): a `MeshInstance3D` with a
    `QuadMesh` rotated `-90°` about X (facing up), unshaded alpha
    `StandardMaterial3D` with **depth test enabled** (the knob/fader body
    occludes the disc centre, leaving a soft ring of light pooling under the
    handle — no whole-mesh emissive tint on the part) and a radial
    `GradientTexture2D` (white centre → transparent edge, `Fill = Radial`,
    `FillFrom (0.5,0.5)`, `FillTo (1,0.5)`). Max ring size `HaloSize = 0.07`
    (`MasterHaloSize = 0.10`).
  - **Positioning**: each ring is anchored at exactly its part's position + a
    tiny `HaloLift (0.008)` in `+Y` (board-local). `Soundboard3D` is parented at
    identity under the board GLB, so `part.Position` is the correct centre from
    any camera angle. `UpdateHalos()` repositions every ring each frame, so the
    fader rings follow their moving caps.
  - **Color gate (R10)**: a ring shows the continuous `ColorForError` ramp of
    its signed control error (`SoundboardTargetGenerator.GetControlError`) only
    while **its own channel is the talking one** (`SoundboardGlow.ChannelOf`
    equals `_speakingChannel` from `ChooseSpeakingChannel`). Caller knobs blend
    against their per-caller target (live `CallerSpeakingVolume` /
    `CallerSoundboardSeed` via the monitor, falling back to 0.5/0), everything
    else against neutral. Silent-channel controls, **Ads, and Master stay
    constant white** (`LedSelected`) — no clashing bright white. Centre alpha is
    constant `HaloAlpha = 0.55` regardless of hover (the old hover brighten to
    `HoverAlpha = 0.85` was removed in R13 — hover no longer touches the ring).
  - **Size = loudness (R10)**: each ring scales off its own channel's live bus
    peak — `HaloSize * SizeScaleFromGlow(ControlGlow(control))`, where
    `SizeScaleFromGlow = Lerp(MinRingFraction 0.75, 1.0, glow)` (silent = 75% ≈
    0.053, loud = 0.07). Per-channel glows `_callerGlow`/`_vernGlow`/`_adsGlow`
    are eased toward `GlowFromPeakDb` of their bus (via `_mixer`
    `GetCallerBusPeakDb`/`GetVernBusPeakDb`/`GetAdsBusPeakDb`, null-safe −80 dB)
    at `GlowResponsePerSecond 8`, so the rings breathe with the actual audio.
    Master has no channel of its own → constant quiet baseline size. Hover does
    **not** scale the ring (the old `× HoverScale 1.35` was removed in R13).
  - All rings are hidden while the handles are (`!_handlesVisible`).
- `World3D.PollSoundboardMouse()` raycasts each frame for hover: no control
  under the cursor → `SetHover(None)` (also forced when the GUI is hovered);
  while dragging, hover follows `_boardSelected` so the halo tracks the active
  control. Hover feedback is now only the part highlight + the existing
  channel-lamp brighten (`IsLampHighlighted`) — it no longer scales or brightens
  the halo ring (R13).

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
- `scripts/audio/AudioMixerManager.cs` - `CallerBaseAmplifyDb = 8`, Vern/SFX drive + compression, `TuneCompressor`, above-target → drive/compress (never louder). *(Superseded for Caller by R12 — see changelog.)*
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

**Round 10 (modified) — fourth caller target + speaking-channel glow**
- `scripts/audio/SoundboardCallerTargets.cs` -> `SoundboardTargetGenerator.cs`:
  `SoundboardCallerTargets`/`SoundboardCallerBands` gain a `Volume` field (4-arg
  ctors), wired through `GetCallerTargets`/`GetCallerBands`/`GetControlBand`/
  `GetControlError`/`GetWorstBand`; `PerKnobJitterRange 0.11 → 0.5`,
  `Min/MaxTargetKnob 0.25/0.75 → 0/1`, new `VolumeSalt 0x564F4C01`. The
  CallerLevel fader is now a real per-caller target spread across the full track
  (root cause: it graded against neutral 0.5 and always read GREEN at rest). *(Spread
  narrowed to 0.2 in R12 — see changelog.)*
- `scripts/audio/SoundboardMixerDriver.cs` - `callerLevelDelta` grades against
  `t.Volume` (per-caller) instead of neutral `center`.
- `scripts/audio/SoundboardGlow.cs` (new) - `SpeakingChannel` enum,
  `ChooseSpeakingChannel(callerDb, vernDb)`, `GlowFromPeakDb(db)`,
  `ChannelOf(control)`, `MinRingFraction (0.4)`, `SizeScaleFromGlow(glow)`;
  pure static, unit-tested.
- `scripts/audio/AudioMixerManager.cs` - `GetVernBusPeakDb()`/`GetCallerBusPeakDb()`
  from the Godot 4.6.3 `AudioServer.GetBusPeakVolumeLeftDb/RightDb` +
  `GetBusChannels` (no generic `GetBusPeakVolumeDb`; invalid bus → −80 dB),
  plus `GetAdsBusPeakDb()` (`_sfxBusIndex` — ads lives on the SFX bus).
- `scripts/world3d/Soundboard3D.cs` - resolves the mixer in `_Ready`
  (`GetNodeOrNull("/root/AudioMixerManager")`, null-safe headless −80 dB).
  **R10 always-on per-ring redesign**: `_halos`/`_haloMaterials` (one ring per
  `Slots` entry, built by `BuildHalos()`), per-channel eased glows
  `_callerGlow`/`_vernGlow`/`_adsGlow` sampled from each bus peak in
  `UpdateSpeakingState(delta)`; `UpdateHalos()` each frame repositions every
  ring onto its part, gates colour by channel (`ChannelOf(control) ==
  _speakingChannel` → `ColorForError(ControlError(...))`, else constant white
  `LedSelected`), sizes by `HaloSize * SizeScaleFromGlow(glow)` (hovered ×
  `HoverScale 1.35`, brightened to `HoverAlpha 0.85`); hidden when
  `!_handlesVisible`. `HoveredControlError()` → per-control `ControlError(control)`
  (caller uses live `CallerSpeakingVolume`/`CallerSoundboardSeed`).

**Round 12 — caller sound design rework (real trim, never-inaudible)**
- **Root causes fixed**: (1) the phone preset was a suffocating tin can — new
  `AudioMixerManager.CallerPresets` L1..L4 are a light telephone EQ
  (low-pass 3500/4800/6000/8500 Hz, high-pass 250/220/190/150 Hz, resonance
  3.0→1.2, distortion ~0.02) that always sounds like a clear phone call;
  (2) the ±3000/±1200 Hz LP/HP knob spans let wrong knobs make the caller
  *clearer* and the deep-muffle sweep (220 Hz) + −30 dB fader floor made them
  *inaudible* — spans now ±1000/±400 Hz, `MuffleMuffledHz = 1200`,
  `CallerLevelMinDb = -15` (floor muffles, never cuts); (3) the gain-above
  penalty was inaudible — the Caller gain knob is now a **real trim**.
- `scripts/audio/AudioMixerManager.cs` - `CallerBaseAmplifyDb` → symmetric
  `CallerAmplifySpanDb = 8` (amplify starts at 0 dB real trim; `SetCallerAmplify`
  clamps `±8`); removed caller deep-tighten (`SetCallerCompression` +
  `_callerCompressorIndex` + `CallerCompressThresholdMaxDb`/`CallerCompressRatioMax`)
  — the caller compressor is fixed glue (`CALLER_COMPRESSOR_GAIN 3→5`);
  removed dead `_callerChorusIndex`; added `AudioEffectLimiter` (threshold −1 dB)
  last on the caller bus as `_callerLimiterIndex`; above-target excess feeds the
  drive (rough) instead of compression.
- `scripts/audio/SoundboardMixerDriver.cs` - `CallerAmplify` law now
  `clamp(gainDelta·CallerAmplifySpanDb, -8..8)` (real trim; killed
  `CallerAttenuateSpanDb`/`gainBelow`); `CallerLowPassSpanHz 3000→1000`,
  `CallerHighPassSpanHz 1200→400`, `MuffleMuffledHz 220→1200`,
  `CallerLevel` `Clamp(delta·15, -15, 0)` (30→15 span). `CallerCompression`
  stays computed (= callerTotalOver) so the UI/score keeps grading the overshoot.
- `scripts/audio/SoundboardTargetGenerator.cs` - `PerKnobJitterRange 0.5 → 0.2`
  (targets stay near the phone-ideal — a correct mix always sounds consistent).
- `scripts/audio/AudioEffectsProcessor.cs` (deleted) - the duplicated
  `EffectPresets` table and its stub node are gone; `AudioMixerManager
  CallerPresets` is the single source of truth.
- `tests/unit/audio/SoundboardMixerDriverTests.cs` - rewritten gain tests to the
  real-trim law (`RealTrimLouderAndRougher`, `GetsLouderAndRougher`,
  `MufflesAndAttenuatesInsteadOfCutting` −8 dB, fader floor −15).

**Round 13 — Vern clarity, wider ranges, hover/glow decoupling**
- **Vern is fixed-clean**: `AudioMixerManager.ConfigureVernBus()` builds a fixed
  presence EQ after the 80 Hz high-pass (bands 2/3/4 ≈ +0.5/+1.0/+1.5 dB at
  ~320 Hz/1 kHz/3.2 kHz), configured once and never changed by broadcast level —
  removed the dead `VernPresets` array and the `_vernEqIndex = -1` stub;
  `ApplyVernEffects` no longer touches EQ. Compressor bumped to
  `VERN_COMPRESSOR_THRESHOLD -20→-18`, `RATIO 3→3.5`, `GAIN 2→4` dB, so Vern reads
  as the crisp studio voice against any caller. Broadcast upgrades change the
  caller's phone line, not Vern.
- **Wider effect ranges** (`SoundboardMixerDriver.cs`, mirrored in
  `AudioMixerManager`): `CallerLowPassSpanHz 1000→1400`, `CallerHighPassSpanHz
  400→600`, `CallerDriveSpan 0.35→0.45`, `CallerAmplifySpanDb 8→10`,
  `MuffleMuffledHz 1200→1000`, `VernDriveSpan`/`AdsDriveSpan 0.55→0.65`,
  `CallerLevelSpanDb`/`CallerLevelMinDb 15`/`-15 → 18`/`-18` — min→max knob travel
  is more audible while the never-inaudible floors still hold.
- **Hover no longer touches the halo** (`Soundboard3D.cs`): removed `HoverScale`
  (1.35) and `HoverAlpha` (0.85); centre alpha is now constant `HaloAlpha 0.55`
  and `MinRingFraction 0.4→0.75` (rings no longer shrink or brighten on hover, and
  are less tiny when silent). Hover feedback is only the part highlight +
  `IsLampHighlighted` channel lamp. Also corrected stale doc `HaloSize 0.06`→`0.07`.
- `tests/unit/audio/SoundboardMixerDriverTests.cs` - expectations updated to the
  new spans (drive 0.85, amplify ±10, Vern/Ads drive 0.65, caller fader floor −18).

**Tests**
- `tests/unit/audio/SoundboardControlApplierTests.cs`
- `tests/unit/audio/SoundboardTargetGeneratorTests.cs`
- `tests/unit/audio/SoundboardMixerDriverTests.cs`
- `tests/unit/audio/SoundboardGlowTests.cs`
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
- Soundboard DSP (Round 12 real-trim tuning, `SoundboardMixerDriver` —
  `SoundboardEffectSettings` has 17 fields; neutral = all zeros = equipment
  preset unchanged; caller knobs grade vs per-caller targets, Vern/Ads vs
  neutral). Caveat: `AudioEffectDistortion` with `Drive = 0` (the neutral audio
  state) is assumed transparent in Godot — confirm no audible coloration while
  at rest in-engine.
  - CallerGain → drive `clamp(preset.Distortion + callerTotalOver·0.45, 0.05,
    0.95)`; amplify offset `clamp(gainDelta·CallerAmplifySpanDb (10), -10..10)` dB
    around 0 — a **real trim**, so at/above target the caller is audibly louder
    (and rougher via drive), below target softer; compression = totalOver
    (informational for the score/UI). Filter spans are modest:
    `CallerLowPassSpanHz = 1400`, `CallerHighPassSpanHz = 600` — a wrong knob
    dulls/thins but never undoes the phone band and never silences the caller.
  - Vern/Ads → drive `clamp(totalOver·VernDriveSpan/AdsDriveSpan (0.65), 0, 1)`,
    compression = totalOver, muffle `lerp(MuffleTransparentHz → MuffleMuffledHz,
    belowDepth)` with `MuffleTransparentHz = 20000`, `MuffleMuffledHz = 1000`
    (dull-but-intelligible floor, low end of the caller gain knob too).
  - CallerLevel → `Clamp(delta·18, -18, 0)` dB strip volume (below target lowers
    the bus to a −18 dB floor — muffled, never cut; above target caps at 0 dB and
    the excess feeds drive/compression). VernLevel/AdsLevel stay
    `Clamp(delta·30, -30, 0)`.
  - Master (`Fader`) → `MusicFaderSpanDb = 14` and `MasterFaderSpanDb = 8`, with
    music clamped `-30..+14` dB and master clamped `-12..+8` dB (unchanged).
  - Compression: caller compressor is **fixed glue** — threshold −18 dB, ratio 4,
    makeup `CALLER_COMPRESSOR_GAIN = 5` dB (normalizes every caller; no longer
    tightened by knob position). Vern is **fixed clean (R13)**: a presence EQ
    (bands 2/3/4 ≈ +0.5/+1.0/+1.5 dB at ~320 Hz/1 kHz/3.2 kHz) is configured
    once on the Vern bus (after the 80 Hz high-pass), and the compressor settings
    are `VERN_COMPRESSOR_THRESHOLD = -18`, `RATIO = 3.5`, `GAIN = 4` dB — Vern
    stays crisp and consistent at every broadcast level. Ads/SFX use
    `Lerp(-12→-28, 2→10)` via `TuneCompressor`. A limiter (threshold −1 dB) sits
    last on the caller bus so full trim + full drive never hard-clips.
- Targets (`SoundboardTargetGenerator`): `PerfectTolerance = 0.09f`,
  `YellowTolerance = CyanTolerance = 0.12f` (directional bands, Round 3);
  per-caller targets = `Clamp(0.5 + volumeJitter + perKnobJitter_k,
  0, 1)` with `PerKnobJitterRange = 0.2` (R12) and `HashUnit(seed, salt_k)`
  (salts `GainSalt 0x475F6911`, `LowPassSalt 0x6F502F01`, `HighPassSalt
  0x48503401`, `VolumeSalt 0x564F4C01` — the four caller targets include the
  CallerLevel fader, each independently jittered so no two callers share an
  ideal spot but all stay near the phone-ideal); `GetControlBand/
  GetControlError` take the caller seed (0 = no caller → neutral targets) and
  return neutral-based grades for Vern/Ads/Master controls; `None` control →
  `None`.
  `ColorRampHalfSpan = 0.30f` (full ramp half-travel: red at ±0.30, exact
  cyan/yellow at ±0.15, green at 0) with `RampBlue (0.3,0.6,1)`,
  `RampCyan (0.25,0.95,1)`, `RampGreen (0.2,0.9,0.3)`,
  `RampYellow (1,0.8,0.2)`, `RampRed (0.9,0.25,0.2)`.
- Halo rings (`Soundboard3D`, Round 5 + R10): one persistent ring per driven
  control, always visible while the handles are shown; squarish
  `HaloSize = 0.07` (`MasterHaloSize 0.10`) quad, `HaloLift = 0.008`, centre `HaloAlpha = 0.55`
  (constant — hover no longer changes ring alpha or size, R13), depth-tested (no
  `NoDepthTest`), no per-part emissive override, centered exactly on
  `part.Position` each frame (fader rings follow the cap). Size =
  `HaloSize * SizeScaleFromGlow(glow)` with
  `SizeScaleFromGlow = Lerp(MinRingFraction 0.75, 1.0, glow)` (silent idle
  ~0.053 → loud 0.07). Color =
  `ColorForError(GetControlError(...))` with the live caller
  `SpeakingVolume`/`SoundboardSeed` for caller knobs, shown **only while that
  control's channel is speaking** (`SoundboardGlow.ChannelOf` ==
  `ChooseSpeakingChannel` from the Vern/Caller bus peaks, floored at
  `SilenceFloorDb -50`, `HysteresisDb 3`) — silent-channel/Ads/Master rings stay
  constant white `LedSelected`. Per-channel glows `_callerGlow`/`_vernGlow`/
  `_adsGlow` ease toward `GlowFromPeakDb` (`LoudPeakDb -8`, bus peaks via
  `GetCallerBusPeakDb`/`GetVernBusPeakDb`/`GetAdsBusPeakDb`) at
  `GlowResponsePerSecond 8`, so each ring breathes with the voice/audio on its
  own channel.
- Full DSP when the engine's ipso bus values are in the air, or the in-editor
  fit pass shows the notch/fader moving the wrong way: flip only
  `World3D.cs` line 1094 (`deltaY = _boardLastDragScreenY - mousePosition.Y`
  → `mousePosition.Y - _boardLastDragScreenY`), never the GLB geometry.
- Audio indices/presets: `AudioMixerManager` `CallerPresets` L1..L4 (light
  telephone EQ — low-pass 3500/4800/6000/8500 Hz, high-pass 250/220/190/150 Hz).
  A single source of truth (was duplicated in `AudioEffectsProcessor`, now
  removed). Knob deltas should keep L4 the floor/best.
- CRIT calibration: `TerminalOverlay` layer `120`; use `121`/`122` for nav + soundboard overlays.
