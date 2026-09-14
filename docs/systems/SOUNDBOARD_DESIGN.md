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

## 7. Files

**New**
- `scripts/ui/ScreenNavOverlay.cs`
- `scripts/world3d/SoundboardTrigger3D.cs`
- `scripts/audio/SoundboardKnobState.cs`
- `scripts/audio/SoundboardTargetGenerator.cs`
- `scripts/audio/SoundboardMixerDriver.cs`
- `scripts/ui/SoundboardOverlay.cs`
- `scripts/monitors/SoundboardMonitor.cs`
- `docs/systems/SOUNDBOARD_DESIGN.md`

**Modified**
- `scripts/ui/CallerTab.cs` — remove `BackRequested`/`CloseRequested`, `_backButton`, `_closeButton` (keep show-timer row + ON HOLD header).
- `scripts/ui/CallerScreenerManager.cs` — remove `CallerTab` nav subscriptions; owns close via other paths.
- `scripts/world3d/TerminalOverlay.cs` — remove `CallerTab` nav wiring; nav now global.
- `scripts/world3d/World3D.cs` — `SoundboardViewState`, view switching, AABB framing, nav overlay ownership.
- `scripts/world3d/ControlRoom3D.cs` — expose `SoundBoard` reference (name match).
- `scripts/audio/AudioMixerManager.cs` — `ApplySoundboard`, re-apply in `UpdateAudioQuality`.
- `scripts/callers/Caller.cs`, `scripts/callers/CallerGenerator.cs` — `SpeakingVolume`.

**Tests** (`tests/unit/audio/`, `tests/unit/monitors/`)
- `SoundboardTargetGeneratorTests.cs`
- `SoundboardMixerDriverTests.cs`
- `SoundboardMonitorTests.cs`

## 8. Tuning References

- Terminal pattern: `TerminalViewState` enum, `TerminalZoomSpeed = 3.2f`,
  `TerminalFramingWidth = 1.85f`, camera offset `(0.09, 0.12, 1.42)`,
  look `(-0.02, -0.08, 0)`.
- Audio indices/presets: `AudioMixerManager` `CallerPresets` L1..L4
  (low-pass 600/800/1200/2500 Hz), `AudioEffectsProcessor` `EffectPresets`
  (2000/3500/6000/10000). Knob deltas should keep L4 the floor/best.
- CRIT calibration: `TerminalOverlay` layer `120`; use `121`/`122` for nav + soundboard overlays.