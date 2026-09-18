## Current Session

**Branch**: develop
**Task**: Top status overlay polish — halve bar height, merge BREAK next to ON-AIR timer, label prefixes, feed cycles any last-minute event; round 2: font 14 (match transcript), bar 27px, move ScreenNav back/close + soundboard drain panel below the HUD bar. **Status: Completed (round 2) — build green, full suite 582/13 (same pre-existing DI baseline); in-editor visual verification + commit pending**

### Round 2 (completed)
- `TopStateOverlay.cs`: `BarHeight` 34→27 + now `public const` (with doc); `StatFontSize`/`FeedFontSize` 16→14 (transcript uses 14px, `LiveShowPanel.tscn:136`).
- `ScreenNavOverlay.cs`: new `NavTopY => TopStateOverlay.BarHeight + UITheme.MARGIN_SMALL` used for back-button `Position` + close-button y in `_Process` (HUD CanvasLayer 140 drew over these 121/122 buttons at y=6).
- `SoundboardOverlay.cs`: drain panel y 14 → same expression (~33) so it clears the bar.
- Verify: `dotnet build` 0 errors. Full `run-tests.ps1`: **582/13** (same pre-existing DI-harness baseline; no UI-position tests exist).
- Remaining: in-editor check — 27px bar readable at 14px font, `<-`/`X` buttons clear of the HUD, drain readout visible under the bar.

### Round 1 (completed)
- Build green, full suite 582/13 (same pre-existing DI baseline, +4 new tests).

- `TopStateOverlay.cs`: `BarHeight` 68→34 + stylebox v-margins 6→2; time + break merged into one `ClockPair` pod (bar = [ON-AIR+BREAK] [feed] [Listeners+trend] [Bank]); label text now `ON-AIR: mm:ss`, `Listeners: …`, `Bank: $…`. Feed rebuilt as a single centered `_feedLabel` (was 3-line VBox) cycling every 5s (`UpdateFeedCycle`/`UpdateFeedDisplay`, `FeedWindowSeconds=60`); a new event (top-signature change) resets rotation to newest; per-label fade-in tween kept.
- `StatusFeedModel.cs`: `StatusFeedEntry` gained `ElapsedSeconds` (raw, alongside `FormattedTime`); new `EntriesWithinWindow(now, windowSeconds)` (newest-first, break on first stale); `MaxEntries` 3→12 as pure memory cap — display is now window-based so ANY event in the last minute cycles (user-confirmed).
- `TopStateOverlayModelsTests.cs`: ElapsedSeconds assert extended; +4 tests (filter, boundary inclusive, all-expired, empty).
- Verification: `dotnet build` 0 errors (6 pre-existing warnings). `-Filter TopStateOverlayModelsTests` 16/0. Full: **582/13** — same 6 pre-existing DI-harness suites.
- Files Modified: `scripts/ui/TopStateOverlay.cs`, `scripts/ui/StatusFeedModel.cs`, `tests/unit/ui/TopStateOverlayModelsTests.cs`, `SESSION_LOG.md`.
- Blockers: none.
- Remaining: in-editor check — 34px bar readable, ON-AIR+BREAK fit at min window width, feed single line cycles ~5s; commit when asked.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Fix top overlay misalignment — letterbox removal + fullscreen re-sync layout. **Status: Completed — implementation done, build green, full suite 578/13 (same pre-existing DI baseline); in-editor visual verification + commit pending**

- Root cause 1: `project.godot` had `window/stretch/scale_mode="integer"` and `Main` forces a borderless window at the display's native resolution. On displays that are not an integer multiple of 1280x720, integer scale floors down and the whole game renders centered with pillarbox bars — user confirmed black bars left/right. Fix: removed `scale_mode="integer"`, added `window/stretch/aspect="expand"`; `WindowScaleManager.cs` dropped the snap logic, keeps `SetBorderlessFullscreen()`.
- Root cause 2: the "KBTV 3D BLOCKOUT | HALLWAY | connected floorplan" rounded panel piling on the HUD is World3D's blockout-era debug `StatusPanel` (`World3D.tscn` StatusLayer/CanvasLayer 20; long single-line label grows the PanelContainer across the top). Fix: `World3D._Ready()` now hides `StatusLayer/StatusPanel` (label updates continue harmlessly; the terminal screen-debug preview parents into StatusLayer, unaffected).
- Root cause 3 (main, per user screenshots): the HUD is built at boot against the 1280x720 viewport; Main then resizes the window borderless-fullscreen and the logical viewport resizes — the `Control` under the `CanvasLayer` keeps the stale anchor rect (bar not full width, pods overflow so AIR/BREAK clip off-screen). TranscriptOverlay/ScreenNavOverlay already re-sync from `GetViewport().GetVisibleRect()` every frame for the same reason. Fix in `TopStateOverlay`: new `SyncToViewport()` called every visible frame — explicitly sets root `Size`, bar `Position/Size` (0,0 → vp.X × BarHeight), and content `Position/Size` (12px margins). Kept anchors as fallback + `SetAnchorsAndOffsetsPreset(FullRect)` in `_Ready`. `FeedMinWidth` 300→220 so pods can't overflow at odd aspects. Diagnostic prints (added mid-session to chase the silent-log question) removed.
- Verification: `dotnet build` 0 errors (6 pre-existing warnings). `run-tests.ps1`: **578 passed / 13 failed** — same 6 pre-existing DI-harness suites (AdManager, AudioDialoguePlayer, BroadcastStateManager, GameStateManager, LoadingScreen, TranscriptManager). `TopStateOverlayModelsTests` 12/0.
- Files Modified: `project.godot`, `scripts/core/WindowScaleManager.cs`, `scripts/world3d/World3D.cs`, `scripts/ui/TopStateOverlay.cs`, `SESSION_LOG.md`.
- Related Docs: `docs/ui/UI_IMPLEMENTATION.md` (panel/pattern refs), `docs/technical/MONITOR_PATTERN.md` (not touched).
- Blockers: none.
- Remaining: in-editor run — HUD should hug the true top edge at full window width with all items (AIR / BREAK / feed / listeners / money); confirm no leftover black side borders. Commit when asked.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Soundboard Round 13 — Vern clarity (fixed presence EQ + louder compressor), slightly wider effect-knob ranges, hover no longer scales/brightens the glow. **Status: Completed — implementation done, build green, full suite 562/13 (same pre-existing DI baseline), soundboard suites green, docs synced; in-editor audio pass + commit pending**

- User-confirmed: (1) **Vern clarity = fixed EQ** at every broadcast level (not level-scaled) — one consistent clean-studio voice; (2) range bumps "about right" as proposed.
- **R13 implemented + verified**:
  - **Vern fixed-clean** (`scripts/audio/AudioMixerManager.cs`): `ConfigureVernBus()` adds a fixed 5-band presence EQ after the 80 Hz high-pass (bands 2/3/4 ≈ +0.5/+1.0/+1.5 dB at ~320 Hz/1 kHz/3.2 kHz), stored in `_vernEqIndex`; removed the dead `VernPresets` array + the `_vernEqIndex = -1` stub; `ApplyVernEffects` no longer touches EQ. Compressor `VERN_COMPRESSOR_THRESHOLD -20→-18`, `RATIO 3→3.5`, `GAIN 2→4` dB.
  - **Wider ranges** (`scripts/audio/SoundboardMixerDriver.cs`; `AudioMixerManager.CallerAmplifySpanDb` mirrored 8→10): LP span 1000→1400, HP span 400→600, `CallerDriveSpan 0.35→0.45`, caller amplify ±8→±10, `MuffleMuffledHz 1200→1000`, `VernDriveSpan`/`AdsDriveSpan 0.55→0.65`, `CallerLevelSpanDb`/`CallerLevelMinDb 15`/`-15 → 18`/`-18`.
  - **Hover/glow decoupled** (`scripts/world3d/Soundboard3D.cs`): removed `HoverScale (1.35)`/`HoverAlpha (0.85)`; halos are now constant `HaloAlpha 0.55` with `MinRingFraction` 0.75 (in `SoundboardGlow`), no hover size/alpha change; hover feedback = part highlight + `IsLampHighlighted` lamp only. Corrected stale doc `HaloSize 0.06`→`0.07`.
- Verification: `dotnet build` 0 errors. `run-tests.ps1 -Filter SoundboardMixerDriverTests` → **16/0**. Full `run-tests.ps1`: **562 passed / 13 failed** — same 6 pre-existing DI-harness suites; none touch audio/soundboard.
- Files Modified: `scripts/audio/{AudioMixerManager.cs,SoundboardMixerDriver.cs}`, `scripts/world3d/Soundboard3D.cs`, `tests/unit/audio/SoundboardMixerDriverTests.cs`, `docs/systems/SOUNDBOARD_DESIGN.md` (§5 spans, §7 hover, §9 tuning + halo, R13 changelog), `docs/audio/AUDIO_DESIGN.md` (Vern fixed-clean note + caller floors), `SESSION_LOG.md`.
- Related Docs: `docs/systems/SOUNDBOARD_DESIGN.md`, `docs/audio/AUDIO_DESIGN.md`.
- Blockers: none.
- Remaining: in-editor audio pass — confirm Vern is clearly cleaner than the caller at every broadcast level; confirm min→max knob travel is now more pronounced; confirm hovering a knob/fader no longer scales or brightens its ring. Commit when asked.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Caller sound design rework — light telephone phone-preset (always intelligible), gain knob = real trim (louder + rough above target), caller never inaudible, compression becomes fixed normalization. **Status: Completed — implementation done, build green, soundboard suites green, docs synced; in-editor audio pass + commit pending**

- User design decisions locked: (1) **Subtle phone tone kept** — fixed light telephone EQ (bandpass + presence) per equipment level, always intelligible; upgrades audibly widen clarity. (2) **Gain = real trim** — above target = actually louder + mild drive/roughness; below = quieter/soft. Replaces the old "never louder" law.
- Root causes: phone preset too suffocating (L1 low-pass 600 Hz escaped by a ±3000 Hz LP knob span → wrong knob made caller *clearer*); above-target gain penalty (compressor ratio 12, never louder) inaudible; below-target muffle swept to 220 Hz + fader floor −30 dB → caller inaudible; `PerKnobJitterRange 0.5` spread targets across full 0..1 so even correct mixes sounded inconsistent.
- **R12 implemented + verified**: new `CallerPresets` (LP 3500/4800/6000/8500, HP 250/220/190/150, distortion ~0.02, resonance 3.0→1.2); symmetric trim ±8 dB (`CallerAmplifySpanDb`) replacing `CallerBaseAmplifyDb`; above-target excess → drive (rough), caller compressor now **fixed glue** (threshold −18 / ratio 4 / makeup `CALLER_COMPRESSOR_GAIN = 5`; removed `SetCallerCompression` + `CallerCompress*Max` + `_callerCompressorIndex`); `AudioEffectLimiter` (threshold −1 dB, no `SoftClip` — not in this Godot binding) added last on caller bus as `_callerLimiterIndex`; deleted dead `_callerChorusIndex`; spans shrunk (LP 3000→1000, HP 1200→400, `MuffleMuffledHz` 220→1200, `CallerLevelSpanDb 30→15` / `CallerLevelMinDb −30→−15`); `PerKnobJitterRange 0.5→0.2`; **deleted `AudioEffectsProcessor.cs`** (dead `EffectPresets` duplication; `AudioMixerManager.CallerPresets` is sole source of truth) + fixed its comment ref in `SoundboardKnobState.cs`.
- `SoundboardMixerDriver` keep: `CallerCompression` field still computed (= callerTotalOver) for the score/UI; Vern/Ads gain still "never louder" (drive + compression only).
- Verification: `dotnet build` 0 errors (6 pre-existing warnings). `run-tests.ps1`: **562 passed / 13 failed** — same 6 pre-existing DI-harness suites (AdManager, AudioDialoguePlayer, BroadcastStateManager, GameStateManager, LoadingScreen, TranscriptManager: "No provider found for service…"); none touch audio/soundboard. All soundboard suites green: SoundboardMixerDriverTests (rewritten gain tests `RealTrimLouderAndRougher`, `GetsLouderAndRougher`, `MufflesAndAttenuatesInsteadOfCutting` −8 dB, fader floor −15), SoundboardTargetGeneratorTests (still green at jitter 0.2), SoundboardControlApplierTests, SoundboardGlowTests.
- Files Modified: `scripts/audio/{AudioMixerManager.cs,SoundboardMixerDriver.cs,SoundboardTargetGenerator.cs,SoundboardKnobState.cs}`, `scripts/audio/AudioEffectsProcessor.cs` (+`.uid` deleted), `tests/unit/audio/SoundboardMixerDriverTests.cs`, `docs/systems/SOUNDBOARD_DESIGN.md` (§5 table + chain incl. limiter, R12 changelog, §9 tuning, jitter 0.2; AudioEffectsProcessor refs removed; R7/R10 marked superseded), `docs/audio/AUDIO_DESIGN.md` (caller "never inaudible" note), `SESSION_LOG.md`.
- Related Docs: `docs/systems/SOUNDBOARD_DESIGN.md`, `docs/audio/AUDIO_DESIGN.md`.
- Blockers: none.

---

## Previous Session (carried over — in progress)

**Branch**: develop
**Task**: Soundboard glow follow-up — current-channel glow with brief pause hold/subtle dimming, replace rectangular fader halo with circular glow at existing CallerLevel `Lamp_6`. **Status: In Progress — partial implementation present; compile/runtime verification pending**

- **R10-1 (done)**: `CallerLevel` fader was graded vs neutral and showed GREEN at rest 0.5. Added a 4th per-caller perfect-mix target (Volume) so it behaves like the other caller knobs. `SoundboardCallerTargets`/`SoundboardCallerBands` gained `Volume` + 4-arg ctors; constants retuned — `PerKnobJitterRange 0.11 → 0.5`, `MinTargetKnob/MaxTargetKnob 0.25/0.75 → 0/1`, new `VolumeSalt = 0x564F4C01u`; `center = 0.5 + (clamp(speakingVolume,0,1) - 0.5) * 2 * JitterRange (0.08)`, target = `Clamp(center + perKnobJitter, 0, 1)`, per-knob jitter = `(HashUnit(seed, salt) - 0.5) * 2 * PerKnobJitterRange` (HashUnit = MurmurHash3 finalizer, verified). Volume wired through `GetCallerTargets/GetCallerBands/GetControlBand/GetControlError/GetWorstBand`. Fixed positions (volume .5, seed 0): Gain .8408, LowPass .8364, HighPass .4489, Volume .7708 — all distinct; seeds 7 & 42 also fully distinct. `NeutralCallerTargets()` now Volume .5. `SoundboardMixerDriver` `callerLevelDelta = NormalizedDeltaFrom(State.CallerLevel, t.Volume)`; `ComputeEffectSettings(state, preset, targets = null)` keeps the null→neutral fallback so fader tests unchanged.
- **R10-2 (done)**: new `scripts/audio/SoundboardGlow.cs` — `SpeakingChannel { None, Caller, Vern }` + static `ChooseSpeakingChannel(vernDb, callerDb)` (both below `SilenceFloorDb -50`: None; diff > `HysteresisDb 3`: louder wins; within hysteresis both audible → louder wins; exact tie → None) + `GlowFromPeakDb` (clamp((peak − −50)/(−8 − −50), 0, 1)) + `ChannelOf(SoundboardControl)` (Caller* → Caller, Vern* → Vern, else None).
- **R10-3/4a (done)**: `AudioMixerManager` — added `GetVernBusPeakDb()`/`GetCallerBusPeakDb()`/`GetBusPeakDb(idx)` using the correct Godot 4.6.3 API `AudioServer.GetBusPeakVolumeLeftDb/RightDb` + `GetBusChannels` (no generic `GetBusPeakVolumeDb`, no `Mathf.NEG_INF` — sentinel is `float.NegativeInfinity`, invalid bus guard −80 dB) + `GetAdsBusPeakDb()` (`_sfxBusIndex`, ads lives on the SFX bus).
- **R10-4b (done, user-confirmed design)**: **always-on per-ring glow** in `Soundboard3D`. Replaced the single hover `_halo`/`_haloMaterial`/`_glowIntensity` with per-control dictionaries `_halos`/`_haloMaterials` (one ring per `SoundboardPhysicalLayout.Slots` entry, 9 total, built by `BuildHalos()` at `_Ready` — same unshaded radial-gradient depth-tested recipe, initial size `HaloSize 0.06`) and per-channel eased values `_callerGlow`/`_vernGlow`/`_adsGlow`. `UpdateSpeakingState(delta)` still sets `_speakingChannel` via `ChooseSpeakingChannel` and eases each channel's glow off its own live bus peak (`GlowResponsePerSecond 8`; `_mixer` resolved in `_Ready` via `GetNodeOrNull<AudioMixerManager>("/root/AudioMixerManager")`, null-safe headless −80 dB). `UpdateHalos()` (was `UpdateHoverVisual()`) runs every frame while handles are visible: every ring repositions onto its part (`_parts[control].Position + HaloLift 0.008`, fader rings follow the moving caps), color per the channel gate via `SoundboardGlow.ChannelOf` — `ChannelOf(control) == _speakingChannel` → `ColorForError(ControlError(control))` (caller uses live monitor `CallerSpeakingVolume`/`CallerSoundboardSeed`; others neutral/0), else constant white `LedSelected` (silent-channel/Ads/Master rings stay small idle white, no clashing bright white); size = `HaloSize * SoundboardGlow.SizeScaleFromGlow(ControlGlow(control))` — `SizeScaleFromGlow = Lerp(MinRingFraction 0.4, 1.0, glow)` → silent ≈ 0.024 / loud 0.06, hovered × `HoverScale 1.35` and brightened `HaloAlpha 0.55 → HoverAlpha 0.85`; all rings hidden when `!_handlesVisible`. `SoundboardGlow` gained `MinRingFraction 0.4f` + `SizeScaleFromGlow(float)` (pure/static). Call sites updated: `_Process`, `ShowHandles`, `HideHandles`, `SetHover`. Existing channel-lamp brighten (`IsLampHighlighted`) unchanged.
- **R10 verification**: `dotnet build` 0 errors (6 pre-existing warnings). Tests — `SoundboardTargetGeneratorTests` (Volume asserts + 2 new regression tests: `GetCallerBands_NeutralFader_OffWhenVolumeTargetNotCenter`, `GetControlBand_CallerLevel_AtTargetVolume_Green`), `SoundboardMixerDriverTests` (4-arg ctor swap + `ComputeEffectSettings_CallerLevel_GradesAgainstCallerTargetVolume`), `SoundboardMonitorTests` (`BringMixToTarget` now sets `State.CallerLevel = targets.Volume`), `SoundboardGlowTests` (15: ChooseSpeakingChannel cases, GlowFromPeakDb −8→1/−80→0/−29→0.5, ChannelOf map incl. Ads/Master/None, + new `SizeScaleFromGlow` — 0→MinRingFraction, 1→1, midway/clamp — and `MinRingFraction`). Full `run-tests.ps1`: **562 passed / 13 failed** — same 6 pre-existing DI-harness suites (AdManager, AudioDialoguePlayer, BroadcastStateManager, GameStateManager, LoadingScreen, TranscriptManager: "No provider found for service TimeManager/EventBus/GameStateManager" etc.); none reference changed symbols; the +3 from 559 are the new ring-size glow tests. `-Filter SoundboardGlowTests` → 15/0.
- Docs: `SOUNDBOARD_DESIGN.md` §4 formula + table row for CallerLevel + summary targets + **§7 hover affordance rewritten for the R10 always-on per-ring glow** (ring/positioning/color gate/size=loudness/hover) + **Round 10 (modified)** changelog entry (incl. per-ring `Soundboard3D` redesign + `GetAdsBusPeakDb`) + §9 halo tuning + tests list incl. `SoundboardGlowTests.cs`.
- Remaining: in-editor audio/visual pass — knob & fader at per-caller target = no coloration; past target = grit/compression never louder; confirm every ring follows its own channel's loudness (caller/Vern/Ads sized by their bus peak), that the error-ramp color only shows on the currently-speaking channel while silent/Ads/Master stay small white idle rings, and that hovering scales ×1.35 + brightens while the channel lamp still lights.
- Files Modified: `scripts/audio/{SoundboardTargetGenerator.cs,SoundboardMixerDriver.cs,AudioMixerManager.cs,SoundboardGlow.cs(new)}`, `scripts/world3d/Soundboard3D.cs`, `scripts/monitors/SoundboardMonitor.cs`, `tests/unit/audio/{SoundboardTargetGeneratorTests.cs,SoundboardMixerDriverTests.cs,SoundboardGlowTests.cs(new)}`, `tests/unit/monitors/SoundboardMonitorTests.cs`, `docs/systems/SOUNDBOARD_DESIGN.md`, `SESSION_LOG.md`.
- Related Docs: `docs/systems/SOUNDBOARD_DESIGN.md`.
- Blockers: none.

- **Glow follow-up (in progress)**: `SoundboardGlow.MinRingFraction` raised from `0.4f` to `0.75f`; `Soundboard3D` now uses `_speakingChannel` plus `_speakingChannelHold`, removes the old `_faderTrackerLights`/rectangular fader halo path, and creates radial `FaderGlow_*` meshes at fader lamps. Remaining work: finish the 0.35s pause hold/dim state, remove stale `_lastSpeakingChannel` references, fix the static/instance color helper, position the glow with `FaderGlowLift`, and verify whether the glow should cover all faders or only `CallerLevel`/`Lamp_6`.
- **Glow follow-up verification**: `dotnet build` and relevant soundboard tests have not yet run after these partial edits; in-editor visual pass remains.
- Files Modified: `scripts/audio/SoundboardGlow.cs`, `scripts/world3d/Soundboard3D.cs`; pending documentation and test updates.
- Related Docs: `docs/systems/SOUNDBOARD_DESIGN.md`.
- Blockers: current `Soundboard3D.cs` has stale `_lastSpeakingChannel` references and a static method calling instance `ControlError`; rebuild is required after repair.

---

## Earlier Session

**Branch**: develop
**Task**: Soundboard DSP rounds R1–R4 (knob rotation endpoints, per-caller target seeding, base-preserving DSP) + R8 defaults & persistence + R9 full-face default parking. **Status: Completed — build green, all soundboard suites pass, full suite 544/13 (same pre-existing DI baseline), docs + session log synced**

- **R8 (done, user-confirmed)**: (1) knob default = 12 o'clock (rest 0.5 → 180°, already true after R1); (2) fader defaults — Caller/Vern level faders at 50% (0.5), Ads level fader at bottom **0% (0.0)** — user confirmed literal 0% = SFX/ads/UI muted at −30 dB until the fader is raised; (3) **persistence** — state was wiped on every zoom-in because both `SoundboardOverlay.ShowSoundboard()` and `Soundboard3D.ShowHandles()` called `Driver.ResetToNeutral()`; removed those resets → shared driver (session-long World3D child) + `AudioMixerManager._soundboardState` + applied DSP survive move-away-and-return. New `SoundboardKnobState.Default()` (knobs 0.5, Caller/Vern faders 0.5, Ads fader 0) distinct from `Neutral()` (all 0.5, DSP-neutral test invariant); `SoundboardMixerDriver.State` starts at Default; `ResetToNeutral()` renamed `ResetToDefault()`.
- **R8 verification**: build 0 errors (6 pre-existing warnings). `SoundboardMixerDriverTests` now 15/0 (added `ComputeEffectSettings_DefaultAdsFader_CutsSfxBus`, `DefaultState_AdsFaderAtBottom`, `NeutralState_AllControlsCenter`, `ResetToDefault_RestoresBoardDefaults`), `SoundboardPhysicalLayoutTests` 11/0, `SoundboardControlApplierTests` 8/0, `SoundboardTargetGeneratorTests` 32/0, `SoundboardMonitorTests` 6/0 (pre-existing soft assertions). Full `run-tests.ps1`: **544 passed / 13 failed** — same pre-existing DI-harness suites (AdManager, AudioDialoguePlayer, BroadcastStateManager, GameStateManager, LoadingScreen, TranscriptManager). Docs updated: `SOUNDBOARD_DESIGN.md` §5 default/persistence bullet, §7 Show/hide (no-reset + persistence), §8 Round 8 changelog.
- **R9 (done, user-corrected)**: full-face defaults — the GLB authors every knob (`soundboard.py` `Knob_{ch}_{side}`) at identity rotation and the unused fader caps at mid-track, so only the 9 driven controls rendered from state and the cosmetic knobs (columns 0-4 entirely, plus the non-gain `Knob_{5|7}_{0,1}`) sat wherever authored while unused `FaderCap_0..4` sat mid-track. User direction: **ALL knobs on the board at 12 o'clock** (incl. Vern's, Ads', and unused) and **faders at 0% except Vern + Caller at 50%**. Fix is purely visual/structural, in `Soundboard3D`: new `NormalizeUnusedParts()` runs once in `AttachBoard` — every `Knob_*` not in `Slots` gets `RotationDegrees (0, KnobRestOffsetDeg=180, 0)` (12 o'clock), and every `FaderCap_*` not in `Slots` is pinned to `FaderLocalZ(0f)` (bottom, −0.19). Driven slots (Caller/Vern faders 50%, Ads fader 0%, all knobs 0.5) unchanged and still come from driver state; cosmetic parts have no slots so they are parked once and never move again.
- **R9 verification**: build + full suite pending re-run (no pure-logic changes — the parking is Godot scene-node work; `SoundboardPhysicalLayoutTests` 11/0 unchanged).
- **R1 (done, user-confirmed)**: knob rotation now maps value 0 → 315° (down-right), value 1 → 45° (up-right), with rest (0.5) at **180° = 12 o'clock**, keeping value-up clockwise. `SoundboardPhysicalLayout.KnobTurnDeg 90 → 270`, `KnobRestOffsetDeg 90 → 180`; `KnobRotationDeg(value) = rest - (value - 0.5) * turn`. Doc comment updated. Tests added: `KnobRotationDeg_ValueZero_315Degrees`, `KnobRotationDeg_ValueOne_45Degrees`, `KnobRotationDeg_Rest_12OClock` (+ formula-based `_SwingsAroundRest` unchanged).
- **R2 (done)**: per-caller target knob values. `Caller.SoundboardSeed` (int, default 0, NOT persisted) seeded with `(int)GD.Randi()` in `CallerGenerator` right after `SpeakingVolume`. `SoundboardTargetGenerator` — `PerKnobJitterRange 0.11`, `MinTargetKnob/MaxTargetKnob 0.25/0.75`, `GainSalt/LowPassSalt/HighPassSalt`, deterministic `HashUnit(seed, salt)` (MurmurHash3 finalizer → [0,1]); target = `Clamp(0.5 + volumeJitter + perKnobJitter, 0.25, 0.75)`; `NeutralCallerTargets()` = all 0.5 (no-caller case, distinct from `GetCallerTargets(0.5f, 0)`); `GetCallerBands/GetControlBand/GetControlError` gained `seed = 0` params. `SoundboardMonitor` exposes `CallerSoundboardSeed`; OnCallerOnAir pushes `GetCallerTargets(SpeakingVolume, seed)`, OnCallerOnAirEnded pushes neutral; `Soundboard3D.HoveredControlError` passes the live seed.
- **R3/R4 (done)**: base-preserving DSP. `SoundboardEffectSettings` = 17 fields (CallerLowPassHz, CallerHighPassHz, CallerDrive, CallerAmplifyDb, CallerMuffleHz, CallerCompression, VernDrive, VernCompression, VernMuffleHz, AdsDrive, AdsCompression, AdsMuffleHz, CallerLevelDb, VernLevelDb, AdsLevelDb, MusicFaderDb, MasterFaderDb; Neutral all 0 — old VernGainDb/AdsGainDb removed). `NormalizedDeltaFrom(value, target) = Clamp((value-target)*2, -1, 1)`; OverDrive/BelowDrive split. Caller gain/LP/HP grade vs per-caller targets; Vern/Ads gain + level faders grade vs neutral. Caller: drive `Clamp(preset.Distortion + callerTotalOver*0.35, 0.05, 0.95)`, amplify offset `-gainBelow*6f` (0 at/above target), compression = callerTotalOver. Vern/Ads: drive `Clamp(totalOver*0.55, 0, 1)`, compression = totalOver, muffle from below-depth (`MuffleTransparentHz 20000`, `MuffleMuffledHz 220`). Level faders `Clamp(delta*30, -30, 0)` — capped 0 dB above neutral, excess → drive/compression. Master/Music faders unchanged.
- **AudioMixerManager (R3/R4, done)**: `CallerBaseAmplifyDb = 8f`; `SetCallerAmplify(offsetDb)` → `amplify.VolumeDb = 8 + offsetDb` (phone-preset baseline preserved, never boosted above it). `ApplySoundboard(state)` computes everything from stored `_callerTargets` (Set via `SetCallerTargets`, re-applied in `UpdateAudioQuality`); added Vern bus distortion (`_vernDistortionIndex`) + SFX distortion/compressor (`_sfxDistortionIndex`/`_sfxCompressorIndex`); `TuneCompressor` (caller Lerp(-18→-28, 4→12), vern Lerp(-20→-30, 3→10), ads/SFX Lerp(-12→-28, 2→10)); new setters `SetCallerCompression/SetVernDrive/SetVernCompression/SetAdsDrive/SetAdsCompression`. Ads still routes to SFX bus (pre-existing convention).
- Tests updated (all green): `SoundboardMixerDriverTests` — neutral keeps preset, full/above-target CallerGain → drive 0.75 + amplify 0 + compression 1, low gain → amplify -6, Vern/Ads high → drive 0.55 + compression, low Vern → VernDrive 0, level faders below/above neutral (capped 0 dB + compression), plus `_WithCallerTargets_GradesAgainstTarget` and `_AboveCallerTarget_DoesNotGetLouder`. `SoundboardTargetGeneratorTests` — neutral, per-knob target spread (seeds 0..19 stay in reachable range), determinism, loud-volume center shift, HashUnit bounds/seed sensitivity, bands-at-target green (seed 7), error-zero-at-target (seed 3). `SoundboardMonitorTests` — the two perfect-mix tests now set knobs via new `BringMixToTarget` helper using `GetCallerTargets(SpeakingVolume, SoundboardSeed)`.
- Verification: `dotnet build` 0 errors (6 pre-existing warnings). Grep confirms no stale `VernGainDb/AdsGainDb/…` references anywhere. Full `run-tests.ps1`: **541 passed / 13 failed** — same 6 pre-existing DI-harness suites (AdManager, AudioDialoguePlayer, BroadcastStateManager, GameStateManager, LoadingScreen, TranscriptManager); none reference any changed symbol. All soundboard/caller/mixer suites green.
- Remaining: refresh `SOUNDBOARD_DESIGN.md` (§4 rotation endpoints, §7 change, §9 tuning, changelog) then in-editor audio pass (knob at target = no coloration; pushed past target = grit/compression, never louder). Consider documenting the Godot `AudioEffectDistortion` Drive=0 transparency assumption.
- Files Modified: `scripts/world3d/{SoundboardPhysicalLayout.cs,Soundboard3D.cs}`, `scripts/callers/{Caller.cs,CallerGenerator.cs}`, `scripts/audio/{SoundboardTargetGenerator.cs,SoundboardMixerDriver.cs,AudioMixerManager.cs}`, `scripts/monitors/SoundboardMonitor.cs`, `tests/unit/{world3d/SoundboardPhysicalLayoutTests.cs,audio/SoundboardMixerDriverTests.cs,audio/SoundboardTargetGeneratorTests.cs,monitors/SoundboardMonitorTests.cs}`, `SESSION_LOG.md` (docs refresh still owed).
- Related Docs: `docs/systems/SOUNDBOARD_DESIGN.md`.
- Blockers: none.

---

## Previous Session

**Branch**: develop
**Task**: Soundboard interaction polish R5 — reduce R4 spacing extremes and shrink glow another 25%. **Status: Completed — build green, all soundboard suites pass, full-suite baseline unchanged**

- Goal: knobs were too high and faders too low after R4; move knob rows back down, faders back up, and shrink the already-tightened halo by 25% (`0.08 → 0.06`).
- Model: `Tools/modelgen/soundboard.py` knob rows relaxed from `(-0.155, -0.105, -0.055)` to `(-0.12, -0.07, -0.02)` and fader track/caps moved from authoring `y = 0.17` to `y = 0.14` (track stays `0.13` long). Regenerated `assets/models3d/props/soundboard.glb`, `Tools/modelgen/source/soundboard.blend`, `docs/art/model_previews/soundboard.{json,png}`. Validation passed: 66 meshes, 7312 triangles, 7 materials, dimensions `[1.12, 0.5025, 0.156]`, file bytes 506924.
- R6 feedback pass: knob rows spread wider (`0.05 → 0.065` spacing, rows `(-0.10, -0.035, 0.02)`) and per-channel lamp moved `y 0.011 → 0.055` to sit between knob stack and fader track. Regenerated (validated, 66/7312/7, dims unchanged, 506940 bytes).
- R6 follow-up fix (user feedback: spacing uneven + hover misalignment): first pass left rows `(-0.10, -0.035, 0.02)` — geometrically uneven (gaps `0.065`/`0.055`). Rebalanced to truly even rows `(-0.11, -0.045, 0.02)` (`0.065` apart). Hover root-cause: knob tap-collider depth was `0.11` (GLB Z = row direction), larger than the `0.065` row pitch, so adjacent hitboxes overlapped and the oblique raycast grabbed the wrong knob; reduced knob collider depth `0.11 → 0.05` in `Soundboard3D.BuildCollider` so hitboxes fit within the pitch. Regenerated GLB (validated, 66/7312/7, dims unchanged, 506932 bytes).
- Runtime: `SoundboardPhysicalLayout.FaderRestLocalZ = -0.14` (matches new authoring fader Y); fader travel/rotation logic unchanged. `Soundboard3D.HaloSize = 0.08 → 0.06` (25% smaller, still larger than the knob caps).
- Docs: `SOUNDBOARD_DESIGN.md` §7 fader rest/track values, halo sizing, and a new Round 5 modified-file list; §9 halo tuning ref updated to `0.06`.
- Verification: `dotnet build KBTV.csproj` 0 errors (6 existing warnings). Focused suites: `SoundboardPhysicalLayoutTests` 8/0, `SoundboardMixerDriverTests` 9/0, `SoundboardControlApplierTests` 8/0, `SoundboardTargetGeneratorTests` 28/0, `SoundboardMonitorTests` 6/0. Full `run-tests.ps1`: **531 passed / 13 failed**, same pre-existing failing suites (`AdManagerTests`, `AudioDialoguePlayerTests`, `BroadcastStateManagerTests`, `GameStateManagerTests`, `LoadingScreenTests`, `TranscriptManagerTests`).
- Remaining: in-editor visual pass — confirm the rebalanced knob rows read as evenly spaced, hover line-up is fixed (hitbox now fits in the row pitch), the tighter knob/fader gap reads well and the 0.06 halo still appears as a visible under-handle ring.
- Files Modified: `Tools/modelgen/soundboard.py`, `Tools/modelgen/source/soundboard.blend`, `assets/models3d/props/soundboard.glb`, `docs/art/model_previews/soundboard.{json,png}`, `scripts/world3d/{SoundboardPhysicalLayout.cs,Soundboard3D.cs}`, `docs/systems/SOUNDBOARD_DESIGN.md`, `SESSION_LOG.md`.
- Related Docs: `docs/systems/SOUNDBOARD_DESIGN.md`, `docs/art/3D_ASSET_WORKFLOW.md`.
- Blockers: none.

---

## Previous Session

**Branch**: develop
**Task**: Soundboard interaction polish R4 — model spacing, fader cap hover, clockwise knob rotation, tighter glow, exaggerated DSP. **Status: Completed — build green, all soundboard suites pass, full-suite baseline unchanged**

- Goal: move the bottom knob row away from the fader track, make fader hover/click register on the cap (not the track), invert knob visual rotation so mouse-up/value-up reads clockwise, shrink the halo to just over knob size, and make soundboard audio changes more exaggerated/fun while preserving clamps.
- Model regenerated: `Tools/modelgen/soundboard.py` now uses knob rows `(-0.155, -0.105, -0.055)` and fader track/caps at `y=0.17` (track length `0.13`). Regenerated `assets/models3d/props/soundboard.glb`, `Tools/modelgen/source/soundboard.blend`, `docs/art/model_previews/soundboard.{json,png}`. Validation passed: 66 meshes, 7312 triangles, 7 materials, dimensions `[1.12, 0.5025, 0.156]`, file bytes 506916.
- Runtime: `SoundboardPhysicalLayout` now uses `FaderTravel=0.05`, `FaderRestLocalZ=-0.17`, and value-up/drag-up clockwise knob rotation (`KnobRotationDeg = rest - (value - 0.5) * KnobTurnDeg`, total 90° swing). `Soundboard3D` tracks control→body hitboxes, fader hitboxes are cap-sized (`0.055×0.05×0.04`) and move with the fader cap, and hover halo is tighter (`HaloSize=0.08`).
- Exaggerated DSP: `CallerLowPassSpanHz=3000`, `CallerHighPassSpanHz=1200`, `CallerDriveSpan=0.35`, `CallerDriveMax=0.95`, `CallerAmplifySpanDb=18`, `MuffleMuffledHz=220`, `VernGainSpanDb=AdsGainSpanDb=16`, channel level spans `14` (`-30..+14`), `MusicFaderSpanDb=14`, `MasterFaderSpanDb=8` (`-12..+8`).
- Tests/docs: updated `SoundboardPhysicalLayoutTests` for clockwise rotation, `SoundboardMixerDriverTests` for new DSP values, and `SOUNDBOARD_DESIGN.md` §7/§8/§9 for R4 model/runtime/DSP tuning.
- Verification: `dotnet build KBTV.csproj` 0 errors (6 existing warnings). Focused suites: `SoundboardPhysicalLayoutTests` 8/0 (clean), `SoundboardMixerDriverTests` 9/0, `SoundboardControlApplierTests` 8/0, `SoundboardTargetGeneratorTests` 28/0, `SoundboardMonitorTests` 6/0 (same pre-existing soft assertions). Full `run-tests.ps1`: **531 passed / 13 failed**, same pre-existing failing suites (`AdManagerTests`, `AudioDialoguePlayerTests`, `BroadcastStateManagerTests`, `GameStateManagerTests`, `LoadingScreenTests`, `TranscriptManagerTests`).
- Remaining: in-editor visual/audio pass — verify the regenerated fader/knob spacing reads correctly, fader hover only hits the cap, drag-up makes knobs rotate clockwise, tighter halo remains visible, and exaggerated DSP feels fun rather than too harsh.
- Files Modified: `Tools/modelgen/soundboard.py`, `Tools/modelgen/source/soundboard.blend`, `assets/models3d/props/soundboard.glb`, `docs/art/model_previews/soundboard.{json,png}`, `scripts/world3d/{SoundboardPhysicalLayout.cs,Soundboard3D.cs}`, `scripts/audio/SoundboardMixerDriver.cs`, `tests/unit/{world3d/SoundboardPhysicalLayoutTests.cs,audio/SoundboardMixerDriverTests.cs}`, `docs/systems/SOUNDBOARD_DESIGN.md`, `SESSION_LOG.md`.
- Related Docs: `docs/systems/SOUNDBOARD_DESIGN.md`, `docs/art/3D_ASSET_WORKFLOW.md`.
- Blockers: none.

---

## Previous Session

**Branch**: develop
**Task**: Soundboard hover R3 — halo alignment + scale fix, glow under knob, directional 5-color ramp. **Status: Completed — build green, all 5 soundboard suites pass (0 hard failures), full suite 531/13 (same pre-existing baseline)**

- Round 3 (user-approved): (1) hover glow must line up with the cursor — root cause was the constant board-local `HaloTowardCameraZ=0.045` + `HaloLift=0.02` offsets under the 75° camera (raycast itself was accurate; `Soundboard3D` is parented at identity so `part.Position` is the right center); glow also too big/intense (`HaloSize 0.24`, opaque). (2) glow should sit **under** the knobs/faders — remove `NoDepthTest` + the bright-blue whole-mesh emissive tint (halo-only hover). (3) color codes become a **directional 5-color ramp**: below target = cyan (close) → blue (far), above = yellow (close) → red (far), green = perfect.
- **Target generator (`SoundboardTargetGenerator.cs`, done)**: `SoundboardBand` enum → `None/Green/Cyan/Yellow/Blue/Red`; `PerfectTolerance = 0.09` (kept), `CyanTolerance = YellowTolerance = 0.12` (replaces `AcceptableTolerance`); severity-based `GetWorstBand` (Green 0 / Cyan,Yellow 1 / Blue,Red 2; tie → higher enum so `Green,Blue,Red` still returns Red); `GetControlError(state, control, speakingVolume)` signed (current − target); `ColorForError(error)` continuous blue→cyan→green→yellow→red ramp via `ColorRampHalfSpan = 0.30f` (red at ±0.30, exact cyan/yellow at ±0.15, green at 0); `RampBlue/Cyan/Green/Yellow/Red` const colors; `GetControlBand(None)` → `None` band (fixes the pre-existing soft-failing test).
- **Soundboard3D (`Soundboard3D.cs`, done)**: halo anchored exactly at `part.Position` + tiny `HaloLift = 0.008`; `HaloSize 0.24 → 0.14`; `HaloAlpha = 0.55` center (gradient fades edges); removed `NoDepthTest` → knob/fader bodies occlude the disc center (soft ring under the handle); removed `_hoverMaterial`/`_hoverPart` emissive mesh tint + dead `HoveredControlBand()`; `UpdateHoverVisual` uses `ColorForError(HoveredControlError())` with the live caller `SpeakingVolume`; slim colliders (knob `0.075×0.05×0.11`, fader `0.055×0.045×0.18`, master `0.17×0.07×0.15`) so hover only activates over the handle; `BandColor`/lamps → `Ramp*` colors (Cyan added).
- **Monitor (`SoundboardMonitor.cs`, done)**: added `CallerSpeakingVolume` (`_repository?.OnAirCaller?.SpeakingVolume`) for accurate halo blending; caller-knob halo falls back to 0.5 when no monitor.
- **Tests (updated, all green)**: `SoundboardTargetGeneratorTests` 28/0 — directional bands (far below → Blue, just below → Cyan, just above → Yellow, far above → Red), within-tolerance both sides green (floats: used `PerfectTolerance * 0.5`), `ColorForError` (0/±half-span/±1 → Green/Cyan,Yellow/Blue,Red), `GetControlError` signed + zero-at-neutral, worst-band severity + Cyan-vs-Yellow tie → Yellow, `GetControlBand(None)` → None. `SoundboardMixerDriverTests` 9/0, `SoundboardControlApplierTests` 8/0, `SoundboardPhysicalLayoutTests` 8/0, `SoundboardMonitorTests` 6/0 (unchanged `IsOffPerfect`). `dotnet build`: 0 errors (6 pre-existing warnings).
- **Full suite**: `run-tests.ps1` → **531 passed / 13 failed** — same 6 pre-existing failing suites (AdManager, AudioDialoguePlayer, BroadcastStateManager, GameStateManager, LoadingScreen, TranscriptManager); none soundboard.
- Docs refreshed: `SOUNDBOARD_DESIGN.md` §4 (directional band semantics + continuous hover ramp), §7 hover-affordance (recentering, depth test, no tint, HaloSize/Alpha/Lift, collider sizes), §8 Round 3 file list, §9 tuning (Cyan/YellowTolerance 0.12, `ColorRampHalfSpan` 0.30, ramp colors, halo 0.14/0.55/0.008).
- Remaining: in-editor fit pass — verify halo ring now hugs the knob/fader, sits under the cap, reads as a color-coded ring at the cursor, and caller LED arc still reads. Then commit wave.
- Files Modified: `scripts/audio/SoundboardTargetGenerator.cs`, `scripts/world3d/Soundboard3D.cs`, `scripts/monitors/SoundboardMonitor.cs`, `tests/unit/audio/SoundboardTargetGeneratorTests.cs`, `docs/systems/SOUNDBOARD_DESIGN.md`, `SESSION_LOG.md`.
- Related Docs: `docs/systems/SOUNDBOARD_DESIGN.md`.
- Blockers: none.

- Design (user-confirmed): keep the 6-control mixer model (CallerGain, CallerLowPass, CallerHighPass, VernGain, AdsGain, Master, normalized 0..1); columns = channels (Caller = Gain fader + bottom LowPass knob + top HighPass knob; Vern = Gain fader; Ads = Gain fader; Master = big round knob; cols 4-8 cosmetic); LEDs = GLB per-channel lamps only (no floating dots). Camera zooms to ~75° down-angle framing the board letterboxed in the top ~73% of the viewport, leaving the bottom ~27% clear for the transcript overlay.
- **ROUND 2 (user-approved, in progress)**: 9 controls — added per-channel output level faders (CallerLevel/VernLevel/AdsLevel) + Vern/Ads gain knobs alongside the existing CallerGain/3 knobs + Master. Fader drag flip (drag-only, `World3D.cs:1094` sign swap). Knob notches rotated 180° (`KnobRestOffsetDeg=180`, rest up, swing 135°–225°). Hover: lamp-brighten + color-coded hover-only quad halo (Green/Blue/Yellow/Red). PerfectTolerance 0.05→0.09. Board FX fully additive (neutral = equipment preset unchanged).
- **GLB regenerated (round 1, done)**: `docs/art/model_previews/soundboard.json` — `validation: passed, meshes: 66, triangles: 7312, materials: 7, dimensions [1.12, 0.5025, 0.156]`. Deterministic part names `FaderCap_{ch}` / `Knob_{ch}_{side}` / `Index_{ch}_{side}` / `Lamp_{ch}` / `MasterKnob`; `index.parent = knob; index.matrix_parent_inverse = knob.matrix_world.inverted()`. Caps slide glTF-local Z (rest -0.12, 0 → -0.18 front / 1 → -0.06 back), knobs + master spin local Y (±45°, +180° notch offset).
- **C# layout (done, build green)**: `SoundboardPhysicalLayout.cs` — `Slots` now 9 controls: CallerGain→`Knob_6_2`, CallerLowPass→`Knob_6_0`, CallerHighPass→`Knob_6_1`, CallerLevel→`FaderCap_6` (Lamp_6); VernGain→`Knob_7_2`, VernLevel→`FaderCap_7` (Lamp_7); AdsGain→`Knob_5_2`, AdsLevel→`FaderCap_5` (Lamp_5); Master→`MasterKnob` (Lamp_3). `KnobRestOffsetDeg=180f`; `KnobRotationDeg(0.5)=180`, (0)=135, (1)=225. `KnobTurnDeg` stays 90. `IdleLamps = {Lamp_0,Lamp_1,Lamp_2,Lamp_4}`.
- **Knob state + applier (done)**: `SoundboardKnobState` +3 fields (`CallerLevel/VernLevel/AdsLevel`), ResetToNeutral/CopyFrom updated; `SoundboardControlApplier` enum now 9 values + Apply/CurrentValue cases.
- **Mixer driver + AudioMixerManager (done, 14-field DSP, fully additive)**: `SoundboardEffectSettings` = CallerLowPassHz, CallerHighPassHz, CallerDrive, CallerAmplifyDb, CallerMuffleHz, VernGainDb, VernMuffleHz, AdsGainDb, AdsMuffleHz, CallerLevelDb, VernLevelDb, AdsLevelDb, MusicFaderDb, MasterFaderDb. Neutral = all zeros. Constants: `CallerAmplifySpanDb=10` (0..10, dropped `CallerAmplifyBaseDb`), `MuffleTransparentHz=20000`, `MuffleMuffledHz=500`, `VernGainSpanDb=8` (0..8), `AdsGainSpanDb=8` (0..8), level faders `SpanDb=8, Min=-24, Max=8`. CallerGain: drive = clamp(preset.Distortion + gainDelta*0.15, 0.05, 0.8); amplify = max(gainDelta,0)*10; muffle = lerp(20000→500, max(-gainDelta,0)). Vern/Ads gain: +0..8 boost (high) / muffle sweep (low). Master → MusicFaderDb+MasterFaderDb. `ApplySoundboard` now also Sets muffle busses (caller/vern/sfx) + per-bus volumes (caller=CallerLevelDb, vern=VernLevelDb+VernGainDb, sfx=AdsLevelDb+AdsGainDb); new `SetCallerMuffle/SetVernMuffle/SetAdsMuffle` setters added.
- **Target generator (done)**: `PerfectTolerance = 0.09f`; new `GetControlBand(state, control, speakingVolume)` — caller controls → `GetCallerBands` fields, all others → `GetBand(CurrentValue, NeutralValue)`.
- **Tests (updated, all green)**: `SoundboardPhysicalLayoutTests` 8/0 (9 controls, lamps Lamp_6/7/5/3, rest 180°), `SoundboardMixerDriverTests` 9/0 (neutral amp 0, full gain amp 10, muffle at low gain, Vern/Ads boost+level spans), `SoundboardControlApplierTests` 8/0 (+3 level controls read/write/clamp), `SoundboardTargetGeneratorTests` 12/0 (+tolerance + GetControlBand). `dotnet build`: 0 errors.
- **Soundboard3D hover/halo (done, build green)**: `SetHover(SoundboardControl)` public API; shared hover quad (one `MeshInstance3D` + `QuadMesh` 0.24 × 0.24, `StandardMaterial3D` Unshaded/Alpha/`NoDepthTest`/`CullMode Disabled` + radial `GradientTexture2D` white→transparent, `RotationDegrees (-90,0,0)`), hidden when hover None; positioned `HaloLift 0.02` above the part + `HaloTowardCameraZ 0.045` toward operator (`_halo.Position = part.Position + offset`); albedo color per frame = `BandColor(HoveredControlBand())` — caller knobs use live `CurrentCallerBands()` (monitor when attached), others `GetBand(CurrentValue(state, control), NeutralValue)`; hovered part mesh gets shared blue emissive `MaterialOverride` (`_hoverPart`, cleared on leave). `UpdateHoverVisual()` called from `_Process`; `ShowHandles/HideHandles` clear hover. `UpdateLeds` remap: Lamp_6 = caller worst band, Lamp_7 = LedGreen, Lamp_5 = LedGreen if `AdManager.IsAdBreakActive` else LedDim, Lamp_3 = LedGreen, IdleLamps (Lamp_0/1/2/4) = LedDim; `IsLampHighlighted` brightens slot lamp to LedSelected when control Selected OR Hovered.
- **World3D wiring (done, build green)**: fader drag sign flip at ~1100 (`deltaY = mousePosition.Y - _boardLastDragScreenY`, drag **up = increase**); per-frame hover raycast in `PollSoundboardMouse` (non-drag) + `SetHover(_boardSelected)` while dragging; `SetHover(None)` when `GuiGetHoveredControl() != null` and on click miss.
- **Tests (updated, all green)**: `SoundboardPhysicalLayoutTests` 8/0, `SoundboardMixerDriverTests` 9/0, `SoundboardControlApplierTests` 8/0, `SoundboardTargetGeneratorTests` 12/0, `SoundboardMonitorTests` 6/0 (still on `IsOffPerfect`). `dotnet build`: 0 errors.
- Docs refreshed: `SOUNDBOARD_DESIGN.md` §7 control-slot table (9 controls), hover-affordance section (halo + drag flip), §8 round-2 file list, §9 DSP/halo/tolerance tuning references; `soundboard.json` already current (66 meshes/7312 tris).
- Remaining: in-editor fit pass — game-screen control order (caller col 6, vern col 7, ads col 5, master centre), notch up at rest (135–225°), fader drag direction (if inverted use documented one-liner `deltaY = _boardLastDragScreenY - mousePosition.Y` fallback), halo hover-only (no halo while idle), Fader/Knob handles sit flush on GLB face. Then commit wave.
- Files Modified: `Tools/modelgen/soundboard.py`, `assets/models3d/props/soundboard.glb` (+.import), `docs/art/model_previews/soundboard.json`, `scripts/world3d/{SoundboardPhysicalLayout.cs,Soundboard3D.cs,ControlRoom3D.cs,World3D.cs}`, `scripts/audio/{SoundboardKnobState.cs,SoundboardControlApplier.cs,SoundboardMixerDriver.cs,SoundboardTargetGenerator.cs,AudioMixerManager.cs}`, 4 test suites, `docs/systems/SOUNDBOARD_DESIGN.md` (pending §7–§9 refresh), `SESSION_LOG.md`.
- Related Docs: `docs/systems/SOUNDBOARD_DESIGN.md`, `docs/art/3D_ASSET_WORKFLOW.md`, `docs/art/PIXELLAB_PROMPT_RULES.md`.
- Blockers: none.

---

## Previous Session

- Baseline: build succeeded; full tests 494 passed / 14 failed, including obsolete frozen-pose Vern expectation; existing soft assertions and shutdown leaks also present.
- Implemented eight-second asymmetric whole-arm talking with wrist turns, torso/head accents, continuous Hermite positional tangents and contact holds. Drink/smoke recline around fixed seated spine pivot; shoulder IK and mouth contact follow torso/head. Neutral bind, bone names and separate chair retained.
- Regenerated `vern.blend`, `vern.glb`, contact JSON, prop outputs and Blender moving/contact previews. 21 bones, 15,168 triangles, 10 materials. Godot per-frame validation passes: talking wrist excursions 0.287/0.269m, grip transform error <0.000001, mouth position error <0.0000002m, fixed pelvis/feet error <0.0000004.
- Runtime: imported-duration one-shot admission, actual completion, two-second pre-speak buffer, safe return on unexpected speech/interruption, phase-preserving consecutive talking and stale item filtering. Added `VernPerformanceProps`: single permanent props on the animation clock; smaller exhale puffs from animated head marker replace periodic mouth puffs.
- Files Modified: `Tools/modelgen/{vern_animation,vern_review,vern_pack_review,vern_godot_validate}.py`, `preview_vern.gd`, generated sources/assets/previews; `scripts/world3d/props/{VernAnimationController,VernCharacter3D,VernPerformanceProps}.cs`, `StudioSmoke3D.cs`; both Vern integration suites; art workflow/brief; this log.
- Verification: Blender export/clean round-trip, Godot 4.6.3 editor import and independent all-frame validation passed. Actual studio + 320x180 feed sequences captured/packed for all four performances; front/side contacts and Godot sequence sheets inspected. Build 0 errors / 6 existing warnings. Focused suites 8/0 + 1/0. Full suite 498 passed / 13 failed (same unrelated baseline failures; obsolete Vern failure fixed). `-Filter Vern` matches no tests; use exact suite names.
- Remaining: generic jaw motion and simplified existing grip rig, no phoneme sync; the close feed crops low/resting hands. Item-use notification/queue from the older full pass-1 plan remains separate from this caller-time animation-quality baseline. No commits made.
- Next Steps: user aesthetic review of `docs/art/model_previews/vern_godot_*_{feed,wide}.gif` and contact sheets.
- Related Docs: `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`, `docs/testing/TESTING.md`.
- Blockers: none.

---

## Previous Session

**Branch**: develop
**Task**: Wire Vern's 3D animations to the broadcast speaker state — `VernAnimationController` + integration tests. **Status: Completed — build green, suites pass, full-suite failures are pre-existing**

- Implemented `scripts/world3d/props/VernAnimationController.cs` (ns `KBTV.World3D`), added as child node of `Vern.tscn`; `VernCharacter3D.AnimPlayer`/`FindAnimPlayer` added.
- Controller subscribes to `BroadcastItemStartedEvent` (VernLine/DeadAir → talking; CallerLine → random idle behavior + 2s pre-speak idle; else idle_breathing) and `BroadcastEvent` (`Interrupted` only → reset timers, idle_breathing). Bounded, `_Process`-driven EventBus retry (max 10 frames) so the pre-existing `VernCharacterIntegrationTests` no longer crashes (`seated_rest` stays paused when no EventBus). Diagnostics seam: `DiagnosticAnimation`/`DiagnosticPreSpeakIdle`.
- Added `tests/integration/VernAnimationControllerTests.cs` (5 tests, poll-based to survive EventBus deferred delivery — tree-less EventBus `_mainThreadId==0` → defer via message queue): `PublishVernLine_SwitchesToTalkingAnimation`, `PublishCallerLine_LeavesTalkingForAnIdleBehavior`, `PublishMusic_ReturnsToIdleBreathing`, `CallerLine_ReturnsToIdleBeforeLineEnd`, `InterruptedLine_ReturnsToIdle`.
- Verification: `dotnet build` 0 errors. `run-tests.ps1 -Filter VernAnimationControllerTests` → 5/0; `-Filter VernCharacterIntegrationTests` → 1/0 (was fatal 0xC0000005 before bounded retry). Full suite → 494 passed / 14 failed — all 14 confirmed pre-existing by re-running each failing suite in isolation (LoadingScreen 2, GameStateManager 1, AudioDialoguePlayer 3, BroadcastStateManager 1, TranscriptManager 2, + remainder; none touch Vern/DI EventBus).
- Next Steps: in-editor visual pass (talking/idle/smoking/drink anims against real broadcast); import real animation clips per `docs/art/VERN_3D_MODEL_BRIEF.md`.
- Related Docs: `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`, `docs/testing/TESTING.md`.
- Blockers: none.

---

## Previous Session

**Branch**: develop
**Task**: Plan Vern animation pass 1 for GPT-6 Astra: idle breathing, generic talking, smoking, drinking coffee, plus permanent mug/ashtray/cigarette props. **Status: Completed — production handoff documented**

- User direction: start with one generic default talking animation; future mood variants can come later. Coffee mug should be permanent. Cigarette and ashtray props may also be needed.
- Plan: update Vern's 3D art brief with named action specs, permanent prop requirements, runtime follow-up notes, validation checks, and a copyable Astra handoff prompt.
- Files Modified: `SESSION_LOG.md`, `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`.
- Work Done: documented four clip contracts, permanent props with seamless pickup/return, runtime scheduling and event integration, export/test updates, moving-preview acceptance checks, and copyable Astra prompt. Original model-production prompt retained as completed history.
- Verification: reviewed documentation diff; `git diff --check` passed. Documentation only; no build/tests run.
- Next Steps: implement the animation handoff, starting with Blender motion/contact blocking and permanent prop placement; no animation assets or runtime behavior changed in this planning session.
- Related Docs: `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`.
- Blockers: none.

---

## Previous Session

**Branch**: develop
**Task**: Build, rig, export and integrate seated Vern in the 3D studio. **Status: Completed — model reviewed in Blender/Godot; build passes, baseline failures unchanged**

- User approved model production from the saved brief and supplied Art Bell reference.
- Plan: procedural Blender character with neutral rig and held seated action; separate existing chair; preview/re-import validation, studio integration and build/tests.
- Implemented: stylized dark sweater/trousers, mustache, glasses, swept hair and vintage headphones; 18-bone rig, neutral A-pose and held `seated_rest` action. Mesh is inverse-skinned from authored seating into editable neutral geometry. Corrected bone roll and tube frame twists after inspecting initial renders.
- Asset: `assets/models3d/characters/vern/vern.glb`, 14,728 triangles, 10 materials, 445,548 bytes. Editable `Tools/modelgen/source/vern.blend`; chair remains a separate existing `office_chair.glb`.
- Integration: `scenes/world3d/Vern.tscn` + `scripts/world3d/props/VernCharacter3D.cs` apply/pause the pose before visibility. `VernStation` at studio-local (-0.85, 0.1, -0.05), yaw 180, leaves table clearance. Chair collider and smoke origin follow placement. Broadcast camera now frames actual face/chest via marker at Y=1.26.
- Files Modified: `Tools/modelgen/vern.py`, `vern_mesh.py`, `vern_rig.py`, `vern_export.py`, `preview_vern.gd` (+ generated UID), source/GLB and `docs/art/model_previews/vern*`; `scenes/world3d/Vern.tscn`, `World3D.tscn`; `scripts/world3d/props/VernCharacter3D.cs`, `scripts/world3d/World3D.cs`, `StudioRoom3D.cs`; `tests/integration/VernCharacterIntegrationTests.cs`; art workflow/brief and this log.
- Verification: Blender round-trip preserves skin, action and posed bounds; neutral/seated/front/side/portrait renders reviewed. Actual Godot studio and 320x180 feed captured with `Tools/modelgen/preview_vern.gd`. `dotnet build`: 0 errors, 6 existing warnings. Focused Vern test: 1 passed. Full suite: 490 passed, 13 failed (baseline 489/13; same existing hard failures and existing soft assertion logs).
- Environment: temp Godot 4.6 runtime lacks GodotSharpEditor and crashes in editor mode; successful asset import/captures used `C:/Software/Godot/Godot_v4.6.3-stable_mono_win64/Godot_v4.6.3-stable_mono_win64_console.exe`. Standard test wrapper still works with the temp runtime.
- Next Steps: user visual review; future breathing/head/arm animation and facial expressions. Fingers currently rigid to hand bones. Model generator/review commands documented in the 3D asset workflow.
- Related Docs: `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`, `docs/testing/TESTING.md`.
- Blockers: none.

---

## Previous Session

**Branch**: develop
**Task**: Prepare GPT-6 Astra production brief for Art Bell-inspired seated Vern, matching existing 3D props. **Status: Completed**

- User direction: Vern and chair separate; static seated presentation initially, future animation; slightly cartoony is welcome but match current props.
- Reviewed studio placement/camera, office-chair generator, shared materials, and chair/audio-cabinet previews. Existing prop exporter joins static meshes and rejects armatures; character needs a separate export path.
- Files Modified: `SESSION_LOG.md`, `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`, `AGENTS.md`.
- Work Done: saved style references/palette, separate chair assembly, neutral rig with held seated clip, measured seat height, floor/camera corrections, output paths, acceptance checks and copyable Astra prompt.
- Verification: reviewed technical values against scene/generator sources and documentation diff; `git diff --check` passed. Documentation-only change; no build/tests required.
- Next Steps: use brief and reattach reference photograph for Astra's model production pass; review silhouette and chair fit first.
- Related Docs: `docs/art/3D_ASSET_WORKFLOW.md`, `docs/art/ART_STYLE.md`, `docs/technical/THREED_MIGRATION_PLAN.md`.
- Blockers: supplied photograph is available in conversation; no repository image path has been established for a future session.

---

## Previous Session

**Branch**: 3d-migration
**Task**: Transcript overlay — bottom-screen live show overlay with left picture section, right typewriter transcript, and a real 3D Vern studio camera feed. **Status: Completed — build green, full suite still 13 known failures**

- Plan: keep the existing event-driven `LiveShowPanel`/typewriter path, rework its layout into a bottom overlay, add a `World3D` SubViewport camera aimed at `StudioRoom3D/VernStandIn`, and show the transcript layer only during `LiveShow` without reopening the full caller screener.
- Implemented: `World3D` now adds itself to group `world3d`, creates an always-updating `VernCameraViewport` + `VernStudioCamera`, and exposes `GetVernCameraTexture()` for UI. `CallerScreenerManager` now owns a separate `TranscriptCanvasLayer` (layer 101) and shows it only during `GamePhase.LiveShow`, avoiding the full caller screener/background. `TranscriptOverlay` is tuned to a bottom-center strip. `LiveShowPanel.tscn` is now a two-column overlay: left picture frame, right transcript. `LiveShowPanel.cs` keeps existing `BroadcastItemStartedEvent` + typewriter behavior and switches the left frame between live Vern feed, caller placeholder, and ad/bumper/system cards.
- Verification: `dotnet build` 0 errors (6 existing warnings). Full `pwsh -NoProfile -File run-tests.ps1`: Passed 489 | Failed 13 — same known baseline after 2D cleanup.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/World3D.cs`
- `scripts/ui/LiveShowPanel.cs`
- `scenes/ui/LiveShowPanel.tscn`
- `scripts/ui/components/TranscriptOverlay.cs`
- `scripts/ui/CallerScreenerManager.cs`

### Next Steps
- [ ] In-editor visual pass: start a show, verify the overlay appears at the bottom, typewriter timing still feels right, caller/ad/bumper states switch, and Vern's 3D camera framing catches `VernStandIn` cleanly.

---

## Previous Session

**Branch**: 3d-migration
**Task**: 2D cleanup — delete all 2D world/player code, scenes, tests, shaders, and `assets/tiles` + `assets/sprites/characters/player` from the 3D migration. **Status: Completed — build green, full suite still 13 known failures**

- Plan approved (9 steps). Tag `pre-2d-cleanup` set as recovery point.
- Deletion: `scripts/world/`, `scripts/player/Player.cs`, `scripts/components/Occluder.cs`, `scenes/world/` (World.tscn, Player.tscn), `scenes/Game.tscn`, `scenes/NoirPost.tscn`, `scenes/Main.tscn` + `.backup`, `tests/unit/world/`, 5 2D shaders (keep `crt_output_feather`), `assets/tiles/`, `assets/sprites/characters/player/`.
- Kept (unimplemented-feature/reference value per user): `assets/sprites/characters/vern/` + `callers/` (Vern portrait + caller art, ROADMAP Art pass / mood portraits TODO), `assets/props_samples/` (2D→3D prop reference), all other asset dirs.
- Edits: `RoomStateManager.cs` (drop 2D bounds API, keep manual `SetPlayerLocation`), `CallerScreenerManager.cs` (drop dead Vern sub-viewport block w/ `WorldRoom` refs), `Main.cs:24` fallback → `Game3D.tscn`, `RoomStateManagerTests.cs`, `LoadingScreenTests.cs`.
- Runtime reference sweep: no deleted 2D classes/assets/scenes remain in `scripts/`, `scenes/`, or `project.godot` (the only `scenes/world` match is `scenes/world3d`).
- **`dotnet build`: 0 errors** (6 pre-existing warnings). **Full `run-tests.ps1`: Passed 489 | Failed 13** — same known failure count; pass count dropped because 2D world tests were removed.

---

## Previous Session

**Branch**: 3d-migration
**Task**: Screening UI polish follow-up #2: widen the stat +/- symbol scale, make approve/reject span the middle column, bottom-align the stat panel + buttons, show the evidence button above the stat panel, and stop the stat panel from shifting 1px when evidence appears. **Status: Completed — build green, full suite 514/13 baseline unchanged**

- **Fixed stat-panel shift on evidence popup** (`StatSummaryPanel.cs`): the evidence row used to collapse its slot when hidden (invisible children are skipped by containers + the 4px VBox separation), shifting the stats box down ~a pixel when evidence appeared. The `_evidenceContainer` is now always visible with a fixed `CustomMinimumSize (0, 24)` + `MouseFilter.Ignore`, and `UpdateEvidenceButton` toggles only `_evidenceLabel.Visible` / `_evidenceFoundButton.Visible` (new `_evidenceLabel` field). Extra stats-height safety margin (24 > ~22px button) guarantees no growth when shown.

- **Wider symbol bins** (`StatSummaryPanel.cs`): `BuildMagnitudeSymbols` is now threshold-parameterized — stats `|1-6|→1, |7-12|→2, |13+|→3`; XP uses 3x `|1-18|→1, |19-36|→2, |37+|→3` (user choice). Callers: `CreateStatLabel(amount, 6, 12)`, `CreateXPLabel(xpImpact, 18, 36)`. Tooltips still show exact amounts.
- **Evidence above the stat panel, centered** (`StatSummaryPanel.cs` `_Ready`): the gray border `StyleBoxFlat` moved from the outer `PanelContainer` onto a new inner `statsBox` (`PanelContainer`) that wraps only the stats row; outer control gets `StyleBoxEmpty`. `_evidenceContainer` is now the first row of the outer VBox → renders above the box, centered (`Alignment = Center`). Evidence wiring untouched.
- **Buttons span the middle column** (`ScreeningPanel.tscn`): deleted the `ButtonCenter` `CenterContainer`; `ButtonRow` (HBox) is now a direct child heading the root VBox; `RejectButton`/`ApproveButton` → `size_flags_horizontal = 3` (Fill|Expand) splitting the full width; height `custom_minimum_size (0, 26)`.
- **Bottom alignment** (`ScreeningPanel.tscn`): removed the unused `NotificationContainer` (40px empty Panel, zero code refs); VBox is now TopRow / CallerInfoScroll(expand) / ImpactRow / ButtonRow → stat panel + buttons sit flush at the bottom.
- **FIXED node paths** (`ScreeningPanel.cs` `EnsureNodesInitialized`): buttons moved from `ButtonCenter/HBoxContainer/...` to `ButtonRow/...` (this path change would otherwise cause the same NRE seen earlier).
- Files: `scripts/ui/components/StatSummaryPanel.cs`, `scenes/ui/ScreeningPanel.tscn`, `scripts/ui/ScreeningPanel.cs`, `SESSION_LOG.md`.
- **`dotnet build`: 0 errors** (10 pre-existing warnings). **Full `run-tests.ps1`: Passed 514 | Failed 13 — baseline unchanged; no tests reference the changed symbols.**

### Todo / Next Steps
- [x] Parameterize `BuildMagnitudeSymbols` (stats 6/12, XP 18/36) per user's 1-6/7-12/13+ and 3x XP scale.
- [x] Evidence row above the stat box (inner PanelContainer for the border), centered.
- [x] REJECT/APPROVE full-width of the middle column (`size_flags_horizontal = 3`), taller (26px).
- [x] Remove unused `NotificationContainer`; stat panel + buttons bottom-aligned.
- [x] Update `ScreeningPanel.cs` button node paths; `dotnet build` 0 errors; full suite 514/13.
- [x] Reserve evidence row slot (fixed 24px, always-visible) so the stat panel never shifts.
- [ ] Verify in-game: symbol distribution, button sizing/positioning, evidence button above the box, no 1px shift on evidence popup.

### Files Modified
- `scripts/ui/components/StatSummaryPanel.cs`
- `scenes/ui/ScreeningPanel.tscn`
- `scripts/ui/ScreeningPanel.cs`
- `SESSION_LOG.md`

---

## Previous Session

**Branch**: 3d-migration
**Task**: Follow-up UI refinements on the DOS screening UI: remove duplicate name header, center bottom approve/reject, abbreviate stat changes (P/E/M/C/N/XP with ± count, 3 bins), evidence button above stat change. **Status: Completed — build green, full suite 514/13 baseline unchanged**

- **Removed the name header**: `ScreeningPanel.cs` — deleted `_headerRow` field/`[Export]`, its node lookup, `UpdateForNoCaller`/`UpdateForCaller` writes, and the now-dead `GetCallerDisplayName` method (Name now shown only via its screenable property row). Removed `HeaderRow` node from `ScreeningPanel.tscn` and the unused `using System.Linq;`.
- **Removed CURRENT CALLER name line**: `CallerTab.cs` — deleted `_currentCallerNameLabel` field, creation block, and its Update logic; `UpdateCurrentCallerDisplay` now shows only `Phone: …`.
- **Bottom-middle buttons**: `ScreeningPanel.tscn` — wrapped the approve/reject `HBoxContainer` in a `CenterContainer` (`ButtonCenter`); buttons now `size_flags_horizontal = 0` (shrink to content, centered) directly under the stat/evidence block.
- **Stat abbreviation** (`StatSummaryPanel.cs`): `CreateStatLabel` → `P++/E---/M+` via `GetStatAbbreviation` (P/E/M/C/N) + new `BuildMagnitudeSymbols` (|1-3|→1, |4-6|→2, |7+|→3, `+`/`-`); `CreateXPLabel` → `XP++` style. Tooltips keep full name + exact amount. Green/red colors unchanged.
- **Evidence button above stat change**: restructured `StatSummaryPanel` layout from one `HBoxContainer` (stats | evidence) to a `VBoxContainer` with the evidence row (centered) on top and the stats row below; all evidence visibility/flash/UX logic unchanged.
- Out of scope per user: `LiveShowFooter` "NONE" on-air display (footer not currently shown).
- Files: `scripts/ui/ScreeningPanel.cs`, `scenes/ui/ScreeningPanel.tscn`, `scripts/ui/CallerTab.cs`, `scripts/ui/components/StatSummaryPanel.cs`, `SESSION_LOG.md`.
- **`dotnet build`: 0 errors** (only the 10 pre-existing warnings). **Full `run-tests.ps1`: Passed 514 | Failed 13 — identical to baseline; no UI symbols referenced by tests (grep).**

### Todo / Next Steps
- [x] Remove `HeaderRow` (ScreeningPanel.cs/.tscn) — name shows only as property row.
- [x] Remove CURRENT CALLER `Name:` line (CallerTab.cs).
- [x] Center approve/reject at bottom (CenterContainer in ScreeningPanel.tscn).
- [x] Abbreviate stats (`P++/E---/M+`, `XP++`, 3 bins) in StatSummaryPanel.
- [x] Evidence button row above the stat change (VBox layout).
- [x] `dotnet build` 0 errors; full suite 514/13 (baseline unchanged).
- [ ] Optional follow-ups (carried): in-game visual pass; document 13 pre-existing hard failures + Result `Fail` arg-order bug; commit wave once visually verified.

### Files Modified
- `scripts/ui/ScreeningPanel.cs`
- `scenes/ui/ScreeningPanel.tscn`
- `scripts/ui/CallerTab.cs`
- `scripts/ui/components/StatSummaryPanel.cs`
- `SESSION_LOG.md`

---

## Previous Session

**Branch**: 3d-migration
**Task**: Retro DOS/BIOS terminal restyle of the caller screening UI, make caller name a hidden screenable property, and switch queue phone numbers to `+1XXXXXXXXXX`. **Status: Completed — code done, build green, tests verified**

- Converted the screening/caller UI to a DOS terminal look: pure black backgrounds, gray borders, corner radius 0, monospace (`AcPlus_IBM_VGA_8x16.ttf`) text at 12–18px, character-based headers/dividers (`=`, `-`, `|`), hidden scrollbars (`vertical_scroll_mode = 3`), and a CURRENT CALLER box. Green `+`/red `-` stat accents preserved in `StatSummaryPanel`.
- Made caller `Name` a screenable property (`Caller.cs` `InitializeScreenableProperties`, first property; `ScreeningConfig.BaseDurations.Name = 4f`, Tier 1 → 11 properties / 64s baseline). Name masked in queue/ScreeningPanel (`???` until revealed); `ScreeningPanel.GetCallerDisplayName` shows phone when name hidden.
- Phone format: `CallerGenerator.GenerateRandomCaller()` now emits `$"+1{3-digit}{7-digit pad}"` (e.g. `+17421234565`); incoming/on-hold queues show phone only.
- Files: `Caller.cs`, `CallerGenerator.cs`, `ScreeningConfig.cs`, `UIColors.cs`, `UITheme.cs`, `CallerTab.cs/.tscn`, `CallerQueueItem.cs/.tscn`, `CallerListAdapter.cs`, `ScreeningPanel.cs/.tscn`, `ScreenablePropertyRow.cs`, `StatSummaryPanel.cs`.
- **`dotnet build`: 0 errors** (only 10 pre-existing warnings). **Full `run-tests.ps1`: Passed 514 | Failed 13 — identical to the pre-change baseline; the 13 are pre-existing hard failures (AdManager x4, AudioDialoguePlayer x3, BroadcastStateManager x1, GameStateManager x1, LoadingScreen x2, TranscriptManager x2), none touch modified symbols.**
- Test harness nuance (critical for interpreting results): `tests/KBTVTestClass.cs` `AssertThat`/`AssertAreEqual` are SOFT assertions — they `RecordFailure` without throwing, so GoDotTest counts the test as "passed" while printing `Test assertion failed` + a suite-level `Test suite had N failure(s)`. The `Passed/Failed` summary counts only hard (exception) failures. Verified per-suite in isolation:
  - `CallerTests` 43: 0 soft / 0 hard ✅ (do NOT edit — `Length == 11` assertions now satisfied).
  - `CallerGeneratorTests` 11: had 1 soft failure (`Contains("-")` against new hyphen-less phone) — **fixed test** to assert `+1` prefix, length 12, no hyphen → now 0 soft / 0 hard ✅.
  - `ScreenablePropertyTests` 12: 0 soft / 0 hard ✅.
  - `ScreeningControllerTests` 17 (`ErrorCode == "NO_SESSION"` x2 soft) and `ScreeningControllerEventsTests` 10 (`ErrorCode == "NO_REPOSITORY"/"NO_SCREENING"` x2 + `PatienceExpired` x1 soft) — all **pre-existing**: `Result<T>.Fail("CODE","msg")` passes args (errorMessage, errorCode) so the code token lands in `ErrorMessage`; and `Caller.State` defaults to `Incoming` (never set to `Screening` in the test) so patience never decays. Untouched by this work.
- `CallerStatEffectsTests` (66 soft) + `PersonalityStatEffectsTests` (53 soft) call pure static `GetStatEffects(key, enum)` with no `Caller` instance — pre-existing, unaffected by the added Name property.

### Todo / Next Steps
- [x] DOS restyle of CallerTab, CallerQueueItem, ScreeningPanel, ScreenablePropertyRow, StatSummaryPanel (+ theme/colors).
- [x] Name as hidden screenable property + ScreeningConfig duration.
- [x] Phone format `+1XXXXXXXXXX` + hide name in queues/header/CURRENT CALLER.
- [x] `dotnet build` 0 errors; full suite 514/13 (pre-existing baseline unchanged).
- [x] Fixed `CallerGeneratorTests` phone assertion (new format) → suite clean.
- [ ] Optional follow-ups: in-game visual pass of the DOS styling; document the 13 pre-existing hard failures + Result `Fail` arg-order bug; commit wave once visually verified.

### Files Modified
- `SESSION_LOG.md`
- `scripts/callers/Caller.cs`
- `scripts/callers/CallerGenerator.cs`
- `scripts/screening/ScreeningConfig.cs`
- `scripts/ui/themes/UIColors.cs`
- `scripts/ui/UITheme.cs`
- `scripts/ui/CallerTab.cs`
- `scenes/ui/CallerTab.tscn`
- `scripts/ui/CallerQueueItem.cs`
- `scenes/ui/CallerQueueItem.tscn`
- `scripts/ui/components/CallerListAdapter.cs`
- `scripts/ui/ScreeningPanel.cs`
- `scenes/ui/ScreeningPanel.tscn`
- `scripts/ui/components/ScreenablePropertyRow.cs`
- `scripts/ui/components/StatSummaryPanel.cs`
- `tests/unit/callers/CallerGeneratorTests.cs`

---

## Previous Session

**Branch**: 3d-migration
**Task**: Make the soundboard a 3D model with animated, interactable faders/knobs (mirror the computer-terminal pattern), reachable via the diegetic zoom. **Status: Code complete + tested; in-editor fit pass pending**

- Direction (user-confirmed): reuse `soundboard.glb` body + simple box/cylinder handle models; status LEDs as 3D emissive dots (only drain text stays 2D); click-to-select a channel then drag to adjust. Camera zooms to ~35°-down framing so the board + desk/room around it are visible.
- THIS SESSION (completed): wrote `scripts/audio/SoundboardControlApplier.cs` (pure `enum SoundboardControl { None, CallerGain, CallerLowPass, CallerHighPass, VernGain, AdsGain, Master }` + `ValueFromDrag`/`ClampValue`/`Apply`/`CurrentValue`); wrote `scripts/world3d/Soundboard3D.cs` (child of `ControlRoom3D` at `(0.25, 0.9, -3.55)` rot Y 180, name `Soundboard3D`; code-built fader BoxMesh + knob CylinderMesh handles on `CoverZ = -0.09`, oversized tap colliders on `HitLayer = 1u<<20` at `ColliderZ = -0.16`, 3D emissive LEDs from `SoundboardMonitor.CallerBands` / `AdManager.IsAdBreakActive`; `ShowHandles`/`HideHandles` toggle root `Visible`, `ShowHandles` resets driver to neutral + applies; removed `SetVisualsEnabled`; simplified `UpdateLeds` dropping a buggy cached `_callerBands` field); wired `ControlRoom3D.cs` (new `SoundBoard3D` property + `AddChild` in `_Ready`); wired `World3D.cs` (constants `SoundboardFramingWidth = 3.2`, `SoundboardCameraDistance = 2.0`, `SoundboardElevationDeg = 35`, `SoundboardLookPivotHeight = 0.06`, `SoundboardDragPixelsPerUnit = 220`; fields `_soundboard3D`, `_boardLeftWasPressed`, `_boardDragging`, `_boardSelected`, `_boardLastDragScreenY`; `_Ready` wires driver + monitor + `HideHandles`; `_Process` calls `PollSoundboardMouse()` when view open; 35°-elevation framing; `ShowHandles`/`HideHandles` in zoom-in/out completions and `OnNavBackRequested` soundboard→terminal branch; click-to-select-then-drag with per-frame incremental rebase via `SoundboardControlApplier.ValueFromDrag`); rewrote `scripts/ui/SoundboardOverlay.cs` to a mouse-transparent (`MouseFilterEnum.Ignore`), top-center, drain-status-only label (OFF AIR / GRACE n s / DRAIN -x/s / MIXED PERFECT) keeping `Driver`/`SetMonitor`/`ShowSoundboard`(ResetToNeutral+Apply)/`HideSoundboard`; wrote `tests/unit/audio/SoundboardControlApplierTests.cs` (8 tests).
- **`dotnet build KBTV.csproj`: 0 errors** (fixed two compile errors: 3-tuple deconstruct in `UpdateControls` -> `(control, _, _)`; `LayoutPreset.TopCenter` -> `CenterTop`). **Full `run-tests.ps1`: 513 passed / 13 failed — the 13 are the same pre-existing baseline; all 8 new soundboard-applier tests pass.**
- NOTE: still uncommitted along with the prior passes listed in the Previous Session — build on them, do not lose.

### Todo / Next Steps
- [x] `SoundboardControlApplier` (pure drag->state mapper) + tests.
- [x] `Soundboard3D` (handles, LEDs, tap colliders, show/hide, reset-to-neutral on open).
- [x] `ControlRoom3D.SoundBoard3D` wiring.
- [x] World3D: `PollSoundboardMouse`/`RaycastBoardControl`/`ResetSoundboardInput`, 35° framing, show/hide in zoom transitions + nav-back.
- [x] Overlay strip to drain-status-only + mouse-transparent.
- [x] `dotnet build` 0 errors; `run-tests.ps1` 513/13 (8 new tests green; 13 baseline failures untouched).
- [x] Docs: `docs/systems/SOUNDBOARD_DESIGN.md` section 7 "3D Soundboard Presentation (Shipped)" + renumbered Files/Tuning References.
- [ ] In-editor fit pass: verify 6 handles sit on the GLB face (tune slot X / `SlotCenterY` / rotation), knob/fader travel, `SoundboardDragPixelsPerUnit` 220, LED positions, 35° pitch + framing width 3.2; Esc closes; drain label transitions GRACE→DRAIN.
- [ ] Optional: dedupe 13 pre-existing test failures (missing AutoInject providers); commit wave once visually verified.

### Files Modified
- `SESSION_LOG.md`
- `docs/systems/SOUNDBOARD_DESIGN.md`
- `scripts/audio/SoundboardControlApplier.cs` (new)
- `scripts/world3d/Soundboard3D.cs` (new)
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/World3D.cs`
- `scripts/ui/SoundboardOverlay.cs`
- `tests/unit/audio/SoundboardControlApplierTests.cs` (new)

---

## Previous Session

**Branch**: 3d-migration
**Task**: Implement the soundboard supporting classes + wiring so `World3D` compiles and runs. **Status: Completed — code complete + tested; needs in-editor verification**

- Completed this pass (uncommitted): `docs/systems/SOUNDBOARD_DESIGN.md` written; `CallerTab` nav surgery + `CallerScreenerManager`/`TerminalOverlay` de-wiring; `ScreenNavOverlay` (CanvasLayer 121); `World3D.cs` view-state machine + soundboard plumbing.
- THIS SESSION (completed): created `SoundboardKnobState`, `SoundboardTargetGenerator`, `SoundboardMixerDriver`, `SoundboardOverlay`, `SoundboardMonitor`; added `AudioMixerManager.ApplySoundboard` + re-apply in `UpdateAudioQuality`; added `Caller.SpeakingVolume` + `CallerGenerator` seeding; wired driver/monitor into `World3D._Ready` (L155-161: `_soundboardMonitor` field, `SetDriver(_soundboardOverlay.Driver)`, `_soundboardOverlay.SetMonitor(...)`, `AddChild`); fixed broadcast of build errors; added 18 unit tests. **`dotnet build`: 0 errors. Full `run-tests.ps1`: 505 passed / 13 failed — the 13 failures are the same pre-existing baseline (AdManager/LoadingScreen/GameStateManager/AudioDialoguePlayer/BroadcastStateManager/TranscriptManager DI-provider + NRE issues), none touching soundboard code; 18 new soundboard tests all pass.**
- NOTE: working tree carries uncommitted prior-session changes (`TerminalOverlay.cs`, `ComputerTerminal3D.cs`, `shaders/crt_output_feather.gdshader*`, `CallerTab.cs`, `CallerScreenerManager.cs`, `ScreenNavOverlay.cs`, `World3D.cs`, `SOUNDBOARD_DESIGN.md`) — build on them, do not lose.
- Key facts re-confirmed: `AudioMixerManager` is autoload at `/root/AudioMixerManager` (project.godot L24); buses = Master/Vern/Caller/Static/Music/SFX (no Ads bus); `DomainMonitor` resolves `_repository = CallerRepository` in `OnResolved` via `_Notification` (NOT triggered by manual `_Ready()` in tests → also add test hooks `BindRepository`/`BindVernStats`/`SetDriver`); `DependencyInjection.Get<T>` throws `InvalidOperationException` with no provider → overlay resolves `AdManager` in try/catch; `CallerRepository.NotifyObservers` fires `OnCallerOnAir`/`OnCallerOnAirEnded`; `Caller` gains `SpeakingVolume` as property (seeded 0.2-0.8 in generator); `tests/unit/audio/` created.

### Work Done
- CRT seamlessness pass (completed): confirmed via headless inspect that `crt_computer.glb` is ONE merged mesh (5 surfaces; "Cool phosphor.005" is a bright-green emissive StandardMaterial3D, albedo 0.23/0.54/0.47, on surface 3) — that glow behind the feathered/semi-transparent UI is what read as a pale white edge. Added `ComputerTerminal3D.ConfigureGlassMaterial(Node3D)` which overrides ONLY the "Cool phosphor" surface per-instance via `MeshInstance3D.SetSurfaceOverrideMaterial` (duplicate, emission off, near-black green albedo 0.012/0.020/0.018, roughness 0.8), leaving housing surfaces + the shared GLB untouched; wired in `ControlRoom3D._Ready` after `GetNode("Computer")`. Added `TerminalOverlay.ContentMargin = 24` + `_contentHost` MarginContainer (FullRect) so interactive CallerTab content keeps safe margins while the phosphor background + CRT effects still reach the perimeter; CallerTab now ExpandFill inside the host. `crt_output_feather.gdshader` `edge_feather` 16 → 10. DrawGlass glare halved (softGlare 0.055→0.025, hardGlare 0.085→0.04); the single overlapping diagonal band replaced with 32 non-overlapping strips (sin²-weighted, peak alpha 0.014); scratch/smudge unchanged. DrawVignette corner-cutout polygons removed and replaced with a shallow 24px inner-bezel shadow band (top row strongest at alpha*0.28, others *0.12); deleted the now-unused `DrawCornerCutout` helper. New `tests/integration/CrtGlassIntegrationTests.cs` verifies override hits exactly the phosphor surface, darkens + un-emits it, and leaves a second untouched instance's material unchanged — `pwsh -NoProfile -File run-tests.ps1 -Filter CrtGlassIntegrationTests` → 1/0; full suite **514 passed / 13 failed** (13 = same pre-existing baseline; no regressions).
- Baseline `run-tests.ps1` (before this pass): 513 passed / 13 failed (existing dependency/null-reference failures and additional assertion warnings).
- Read: `SOUNDBOARD_DESIGN.md`, `DomainMonitor.cs`, `CallerGenerator.cs`, `ICallerRepository.cs`, `VernStats.cs`, `Stat.cs`, `AudioMixerManager.cs`, `SoundboardMixerDriver.cs`, `SoundboardTargetGenerator.cs`, `SoundboardKnobState.cs`, `World3D.cs` (soundboard regions), `CallerRepository.cs` (PutOnAir/EndOnAir), `CallerMonitorTests.cs` (test pattern).
- Full code (all named above) + World3D wiring + 18 tests written. Build clean; tests green for the new feature; pre-existing 13-failure baseline unchanged.

### Todo / Next Steps
- [x] Write `docs/systems/SOUNDBOARD_DESIGN.md`.
- [x] `ScreenNavOverlay` (CanvasLayer 121).
- [x] CallerTab surgery + de-wiring (`CallerScreenerManager`, `TerminalOverlay`).
- [x] `SoundboardViewState` in World3D + AABB framing + proximity + overlay show/hide.
- [x] `Caller.SpeakingVolume` + seed in `CallerGenerator`.
- [x] `SoundboardKnobState` (knob fields + `Neutral()` + `NormalizedDelta`).
- [x] `SoundboardTargetGenerator` (pure band computation; `SoundboardBand` + `GetCallerBands` + `IsOffPerfect` + `GetWorstBand`).
- [x] `SoundboardMixerDriver` (holds knob state; `Apply()`/`ResetToNeutral` → `AudioMixerManager.ApplySoundboard`; `ComputeEffectSettings` pure fn).
- [x] `AudioMixerManager.ApplySoundboard` + re-apply in `UpdateAudioQuality` (guarded on -1 indices).
- [x] `SoundboardOverlay` (CanvasLayer 122; VERN locked-green, CALLER live, ADS/BUMPER, master fader + worst-of LED, drain label GRACE/DRAIN/MIXED PERFECT/OFF AIR; resolves `/root/AudioMixerManager` + DI `AdManager` in try/catch).
- [x] `SoundboardMonitor` (10s grace + stepped Emotional/Mental drain capped 3/s, reset on OnCallerOnAirEnded; exposes `IsCallerOnAir`/`GraceRemaining`/`IsDraining`/`CurrentDrainRate`/`CallerBands`/`OverallBand`; test hooks `SetDriver`/`BindRepository`/`BindVernStats`).
- [x] Wire in World3D: monitor created, `SetDriver(_soundboardOverlay.Driver)`, `_soundboardOverlay.SetMonitor(...)`, `AddChild`.
- [x] `dotnet build KBTV.csproj` 0 errors (fixed Aabb.Transform/Transformed API mismatch via manual basis transform; `VisualInstance3D.GetAabb`; BuildChannelRow out-param order; `KnobKind.None` for missing knobs; PanelContainer→custom StyleBox).
- [x] Tests added: `tests/unit/audio/SoundboardTargetGeneratorTests.cs` (7), `SoundboardMixerDriverTests.cs` (5), `tests/unit/monitors/SoundboardMonitorTests.cs` (6); `pwsh -NoProfile -File run-tests.ps1` → 505 pass / 13 pre-existing fail.
- [ ] In-editor verification: stand near control-room soundboard prop → E opens panel; VERN row locked green; CALLER LEDs live; ADS dims unless ad break; MASTER fader + worst LED; drain label transitions GRACE→DRAIN when knobs off-perfect with a caller on air; Esc closes.
- [ ] Optional follow-ups: dedupe 13 pre-existing test failures (missing AutoInject providers); commit wave once visually verified.

### Files Modified
- `SESSION_LOG.md`
- `docs/systems/SOUNDBOARD_DESIGN.md` (new)
- `scripts/audio/SoundboardKnobState.cs` (new)
- `scripts/audio/SoundboardTargetGenerator.cs` (new)
- `scripts/audio/SoundboardMixerDriver.cs` (new)
- `scripts/audio/AudioMixerManager.cs`
- `scripts/monitors/SoundboardMonitor.cs` (new)
- `scripts/ui/SoundboardOverlay.cs` (new)
- `scripts/ui/ScreenNavOverlay.cs` (new)
- `scripts/ui/CallerTab.cs`
- `scripts/ui/CallerScreenerManager.cs`
- `scripts/world3d/TerminalOverlay.cs`
- `scripts/world3d/World3D.cs`
- `scripts/world3d/props/ComputerTerminal3D.cs`
- `scripts/callers/Caller.cs`
- `scripts/callers/CallerGenerator.cs`
- `tests/unit/audio/` (new: SoundboardTargetGeneratorTests.cs, SoundboardMixerDriverTests.cs)
- `tests/unit/monitors/SoundboardMonitorTests.cs` (new)

---

## Previous Session

**Branch**: 3d-migration
**Task**: Fix `affine_invert` (Transform2D det==0) error flood at boot. **Status: Completed**
- **Root cause found**: `TerminalOverlay._screenFrame` (SubViewportContainer) was created with `Stretch = true` but no explicit size, so at boot it force-resized child `ProjectedCrtViewport` (Shows 1152x640 Size2DOverride) to raw (0,0) → SubViewport `_set_size` clamp reports `size=(2,2)` but the override-stretch `stretch_transform` is computed from the raw (0,0) size → `final det = 0.000000` (singular). Engine inverts that transform every frame (passive-hover/picking path) → ~66-465 `affine_invert` errors over a 7s run, starting ~0:00:01.475.
- Confirmed via disposable probe (`ZeroScaleProbe.cs`, now removed): only `ProjectedCrtViewport` had `final det=0.000000`; root and `VernSubViewport` were fine. Minimal empty project reproduced 0 errors → kbtv content was the trigger, not engine/display settings. `get_mouse_position()` in 4.6 has its own det-guard (not the source); `_make_input_local()` (viewport.cpp:1436) and `_process_picking` (line 890) are unguarded inverts.
- **Fix**: removed `Stretch = true` from `_screenFrame` boot config in `BuildUi()`; it is now enabled only in `SetScreenBounds()` (after Position/Size/Scale/Rotation are applied), so the container only stretches once the terminal opens with real bounds. At boot the SubViewport keeps its explicit 1152x640 size → `final det = 1.0`.
- Verified on clean tree (probe + `Main.cs` AddChild removed, `dotnet build` 0 errors/0 warnings): non-minimized no-mouse run → **0 affine_invert, 0 ERROR lines** (was 465/7s); mouse-over-window run (original repro) → **0 affine_invert**.
- Note: `rg` is not on PATH in the shell; use `Select-String` or the grep tool for engine-source greps.

### Work Done
- Current follow-up implemented: final-output shader uses premultiplied blending and fades RGB/alpha together; feather narrowed to 16 logical pixels with full center opacity. Removed obsolete 2D glow. Output render size now accounts for viewport stretch density, with inverse container scaling and Size2DOverride preserving logical UI layout; final texture uses linear filtering. Approved 3D lighting unchanged. `dotnet build`: 0 errors, 10 existing warnings. Runtime visual/input verification remains pending; build does not validate shader rendering. If pale edges remain, inspect the lit glass underneath rather than expanding the fade again.
- Started edge integration polish: soften the sharp rectangular CallerTab boundary with top-layer CRT-colored feathering and rounded glass corner masks.
- Reworked `TerminalOverlay.DrawVignette()` from a few hard edge bands into a denser soft phosphor-colored edge feather, then added rounded corner cutout polygons with a light feather so the rectangular UI blends into the CRT glass.
- `dotnet build`: 0 errors, 10 pre-existing warnings.
- Started CRT effect layering polish. Root cause: `CallerTab` is lazily added after the CRT effect nodes, so default Godot Control sibling draw order places the UI over the effects.
- Implemented minimal fix in `TerminalOverlay.EnsureCallerTab()`: after adding `ProjectedCallerTab`, move it to child index 1 so it draws above only `ScreenPhosphorBackground`; CRT tint, scanlines, dust, glass, and vignette remain above the UI.
- `dotnet build`: 0 errors, 10 pre-existing warnings.
- **Edge feather shader (THIS SESSION):** objective is edges of the projected screenshot UI blending to transparent (with rounded feel) so the 3D prop shows through instead of a hard rectangle. Ruled out `CanvasGroup`+Mask (docs warn children with `clip_children` set "will not function correctly"; `ScrollContainer` sets `clip_contents=true`, which CallerTab uses). Implemented **screen-space feathered alpha mask applied to `_screenFrame`** as an inherited material so phosphor bg + CallerTab + all CRT effect layers share one mask.
- New `shaders/crt_screen_feather.gdshader`: `shader_type canvas_item`, uniforms `screen_center`, `screen_size`, `viewport_size`, `rot_cos`, `rot_sin`, `corner_radius` (34), `edge_feather` (16). Rounded-box SDF in local px space derived from `(SCREEN_UV - center) * viewport_size` then un-rotated via rot_cos/rot_sin; `alpha = 1 - smoothstep(-edge_feather, 0, d)`; `COLOR.a *= alpha`. Note: uses `SCREEN_UV` (global) not `UV` because every child evaluates with its own local UV, so screen-space avoids inconsistent masking across the subtree.
- `TerminalOverlay.cs`: added `_frameMaterial` (ShaderMaterial), applied to `_screenFrame` in `BuildUi()`; `SetScreenBounds()` now feeds `screen_center`, `screen_size`, `viewport_size`, `rot_cos`, `rot_sin` each call. `_screenFrame` material inherits to descendants (no child overrides material). `screen_center` uses the frame's true visual center (`topLeft + rotated(size/2)`), not the projected-corner center, so the box is exactly aligned to the rotated frame.
- `dotnet build`: 0 errors, 10 pre-existing warnings. **Needs in-editor visual verification.**
- User verified the first shader pass still reads too sharp at the perimeter. Next tuning pass: widen shader edge feather, increase corner radius, add a subtle global projection alpha, reduce phosphor background opacity, and dim the old dark corner cutout polygons so the transparent shader controls the edge blend.
- Edge softness tuning applied: `crt_screen_feather.gdshader` now uses `corner_radius=52`, `edge_feather=56`, and `overall_alpha=0.88`; `ScreenPhosphorBackground` alpha reduced `0.96 -> 0.84`; vignette edge/corner cutout alphas lowered so old black corner chunks do not dominate the transparent shader mask; `screen_center` uniform fixed to the rotated frame center (`topLeft + rotated(size/2)`). `dotnet build`: 0 errors, 10 pre-existing warnings. Needs in-editor visual verification.
- User reported the UI itself looked smoother but the overlaid CRT/effect box still looked sharp. Root cause likely Godot `CanvasItem.UseParentMaterial` defaulting false, so `_screenFrame.Material` did not automatically shade descendant CanvasItems. Applied `_frameMaterial` directly to `_glow`, added `UseFrameMaterialForDescendants(...)` helper to set `UseParentMaterial=true` recursively under `_screenFrame`, and explicitly set `ProjectedCallerTab.UseParentMaterial=true` after lazy instantiation. `dotnet build`: 0 errors, 10 pre-existing warnings. Needs in-editor verification that bg/UI/effects all share the feather mask.
- User reported that pass was wrong: glow was constrained to the UI mask instead of scaling up, and feathering looked broken. Correction applied: `_glow` now uses its own `_glowMaterial` with larger bounds (`GlowPadding=72`, `corner_radius=86`, `edge_feather=96`) and no longer shares the exact UI mask; `_frameMaterial` remains for the UI/effects subtree. `crt_screen_feather.gdshader` now uses fragment `VERTEX` screen coordinates instead of `SCREEN_UV * viewport_size`, so nested UI controls and top CRT effect CanvasItems evaluate the same mask in actual screen space. `dotnet build`: 0 errors, 10 pre-existing warnings. Needs in-editor verification.
- User confirmed the shader/material-inheritance direction was worse and asked for better organization plus physical monitor glow. Rolled `TerminalOverlay` back to the stable layered overlay: removed `_frameMaterial`, `_glowMaterial`, `GlowPadding`, recursive `UseParentMaterial`, shader uniform updates, and deleted the unused `crt_screen_feather.gdshader` resources. Kept the earlier draw-order fix (`ProjectedCallerTab` moved to index 1 above the phosphor background and below CRT effects). Added real 3D CRT illumination to `ComputerTerminal3D`: new hidden `OmniLight3D ScreenLight` near the screen surface (`LightColor 0.10,0.85,0.62`, `Energy 0.75`, `Range 1.9`, attenuation 2.4, no shadows) and `SetScreenLightEnabled(bool)`. `World3D` enables it when terminal view opens / texture attaches and disables it on close / detach. `dotnet build`: 0 errors, 10 pre-existing warnings. Next visual pass should use a composed-screen boundary if we revisit feathering, not descendant material inheritance.
- User approved composed-screen approach and asked to cool/dim the monitor light. Refactored `TerminalOverlay` so the projected UI/effects stack renders inside a single `SubViewport` (`ProjectedCrtViewport`, 1152x640) hosted by `SubViewportContainer ProjectedCrtScreen`; phosphor background, lazy `CallerTab`, and CRT tint/scanlines/dust/glass/vignette now live under `ProjectedCrtRoot` inside that viewport. Added new final-output shader `shaders/crt_output_feather.gdshader` only on the `SubViewportContainer`, so feathering applies once to the composed image instead of recursively to nested controls. Shader defaults: `corner_radius=26`, `edge_feather=42`, `overall_alpha=0.96`; `screen_size` uniform updated in `SetScreenBounds()`. Tuned `ComputerTerminal3D.ScreenLight` to cool CRT white-blue (`0.72,0.86,1.0`), `Energy 0.38`, `Range 1.5`, attenuation `2.8`. `dotnet build`: 0 errors, 10 pre-existing warnings. Needs in-editor verification, especially mouse input through the `SubViewportContainer` and edge softness.
- Round 1 (completed, in-editor verified): occlusion + viewport fixes landed. UI was visible on the monitor but user reported the text was unreadable/tiny and clicks didn't register.
- Diagnosed root cause: the CallerTab UI was authored for a 1280x720 window but rendered into a 960x640 (1.5:1) SubViewport drawn on a 0.9 x 0.5 (1.8:1) screen plane. At `TerminalFramingWidth=1.6` the monitor footprint is only ~720x400 screen px → text ~6px on screen, 1.2x horizontal stretch (aspect mismatch), buttons sub-5px (un-clickable in practice).
- Plan approved: keep monitor texture (Option A). Fix = zoom so footprint is ~1152x640 (1:1 with viewport → fonts at native design size), make viewport 1152x640 (1.8:1 = plane aspect, kills stretch), harden `ForwardTerminalMouse` (ButtonMask on hover motion; ensure wheel/scroll passthrough), `TerminalFramingWidth 1.6 → 1.0`.
- Implemented all three World3D.cs changes: `TerminalFramingWidth 1.6 → 1.0` (line 21); SubViewport size (960,640) → (1152,640) — now 1.8:1 = plane aspect (kills the 1.2x stretch); `ForwardTerminalMouse` now forwards `ButtonMask` from motion events (via `(MouseButtonMask)0` default since `MouseButtonMask.None` doesn't exist in Godot) and keeps `InputEventMouseButton` passthrough for wheel/scroll.
- User reported (post round-2) "no hover, clicks do nothing" on the monitor UI. Investigated the full input chain: OS mouse → `_UnhandledInput` → raycast to `ScreenBody` → `PushInput` → CallerTab GUI in the SubViewport. Strongest static suspect: `EnsureTerminalViewport()` set `MouseFilter.Ignore` on both `screenRoot` AND `_terminalTab` (Godot 4 `MOUSE_FILTER_IGNORE` can make the root + subtree transparent to mouse → exactly the symptom). The working 2D path (`CallerScreenerManager`) leaves the same CallerTab at default Stop.
- Step 1 applied: removed the `MouseFilter.Ignore` overrides on `screenRoot` and `_terminalTab` in `EnsureTerminalViewport()` (backdrop keeps Ignore; it's a pass-through leaf under the tab).
- `dotnet clean && dotnet build`: 0 errors, 10 pre-existing warnings. `run-tests.ps1`: **487 passed / 13 failed = unchanged pre-existing baseline**.
- **User verification after Step 1: still can't click on the screen.** MouseFilter was NOT the (only) cause.
- **User diagnostic run #1 hit an NRE** at `UpdateTerminalCamera` (World3D.cs:404): my OPEN print referenced `_terminalViewport.Size`/`GuiDisableInput`, but the viewport is lazily created by `EnsureTerminalViewport()` — which only runs later from `AttachScreenTexture()` (line ~578). On first open the viewport is still null. **Fixed**: OPEN print now builds a null-safe `vpDesc` ("null" or size/guiDisable/kids); `EnsureTerminalViewport` already logs its own viewport-size line afterward.
- User's diagnostic run reported "no logs at all" (not just no mouse logs). Since telemetry was build-green after the NRE fix, no logs means `ForwardTerminalMouse` never runs → events never reach `_UnhandledInput`.
- **Root cause FOUND (static):** `GameStateManager.FinishLoading()` lands in `GamePhase.PreShow` (GameStateManager.cs:127), and UIManager shows the PreShow layer in that phase. `PreShowUIManager.CreateTabSystem()` (PreShowUIManager.cs:83-92) builds a **full-rect anchored `MarginContainer` with default `MouseFilter.Stop`** covering the whole window. Godot 4 input order is `_input` (all nodes) → GUI `_gui_input` (or `_shortcut_input`) → `_unhandled_input`, so that STOP container consumes every mouse event before `_UnhandledInput` while the 3D game is in PreShow. Key events still pass through (no GUI focus) — exactly why `E` opens the terminal / Esc closes it but mouse never logs and hover/click never works. `StatusPanel` (top-left 420x56) and CallerScreener canvas (hidden) are not the blocker.
- **Fix applied (Step 3):** moved terminal mouse handling from `_UnhandledInput` to `World3D._Input()` — `_input` is dispatched to all nodes *before* GUI processing, so it bypasses the full-screen STOP container. While `_terminalViewState == Open`, mouse events now go straight to `ForwardTerminalMouse` + `GetViewport().SetInputAsHandled()` (also blocks the background pre-show buttons from receiving phantom clicks). Esc-to-close also moved to `_Input`; `E`-to-close while open and `E`-to-open (when in range + closed) kept in `_UnhandledInput`. Keys deliberately left out of `_Input` so the pre-show UI behaves normally when the terminal is closed.
- `dotnet build` after Step 3: **0 errors, 10 pre-existing warnings** (unchanged baseline). Tests not re-run (input routing change, no test coverage for World3D input; baseline 487 pass / 13 fail).
- **Pending user verification:** in-editor run → hover (buttons highlight), left-click (Approve/Reject/X/caller rows), scroll, and Esc. Telemetry (`[TerminalMouse]`) should now show `PUSH #N` lines.
- User reported after rebuild: still no logs at all. Treating event callback logging as unreliable/blocked in-editor. Next fix: add `_Process()` polling fallback that forwards hover and button transitions directly from `GetViewport().GetMousePosition()` / `Input.IsMouseButtonPressed()` while terminal is open, plus status-label diagnostics so feedback is visible even if console output is absent.
- Step 4 implemented in `World3D.cs`: terminal mouse hover/left/right/middle click now forward from `_Process()` polling while terminal is open; `_Input()` now keeps only wheel scroll forwarding to avoid duplicate click events; shared raycast-to-viewport mapping extracted; status label shows `TERMINAL | mouse x,y mask=...` or `mouse off screen` while open.
- `dotnet build`: 0 errors, 10 pre-existing warnings.
- User confirmed mouse inputs still do not work correctly. New approach approved: stop using interactive 3D SubViewport/raycast forwarding and create a normal `CanvasLayer` UI overlay projected onto the CRT screen bounds, preserving diegetic monitor look while using native Godot mouse input.
- Added `scripts/world3d/TerminalOverlay.cs`: high-layer `CanvasLayer` with clipped projected screen frame, native `CallerTab` instance, phosphor tint, glow, scanlines, and vignette. `CloseRequested`/`BackRequested` route back to terminal close.
- Wired `World3D.cs` to create the overlay, show it when terminal zoom reaches `Open`, hide it on close, and update its bounds by unprojecting the procedural CRT screen's four 3D corners each frame. Removed the mouse-swallowing `_Input()` branch so native overlay controls receive mouse events directly.
- Terminal screen mesh now gets a simple dark green emissive glow while the overlay is open instead of relying on an interactive viewport texture.
- `dotnet build`: 0 errors, 10 pre-existing warnings.
- User hit two first-run overlay errors: `CallerTab` initialized before global DI resolvers were registered, and projected screen bounds could exceed the viewport causing an invalid `Mathf.Clamp` range. Fixed by lazily instantiating `CallerTab` in `TerminalOverlay.ShowTerminal()` and capping overlay size to the available viewport before clamping. `dotnet build`: 0 errors, 10 pre-existing warnings.
- User confirmed projected overlay fixed input. Starting polish pass: zoom out slightly to show more screen/monitor, center/inset the UI within projected CRT bounds, and add stronger monitor treatment (glow, scanlines, vignette, edge dust) while preserving native UI input.
- Polish pass implemented: `TerminalFramingWidth` 1.0 → 1.25; `TerminalOverlay` now centers the UI on projected screen center, preserves 1.8 screen aspect, insets to 92%, expands glow, and adds enhanced scanlines, tint, custom vignette, and deterministic edge dust/smudges. `dotnet build`: 0 errors, 10 pre-existing warnings.
- User wants overlay fit judged against the actual `crt_computer.glb`, not the procedural placeholder. Starting fit pass: keep the GLB visible during zoom, use `ComputerTerminal3D` only as invisible projection/interact anchor, scale overlay down, and zoom out more.
- Fit pass implemented: `World3D` now keeps `ComputerGlb.Visible = true` during terminal open, `ComputerTerminal3D` no longer builds visible greybox monitor/keyboard/mouse geometry (screen plane is invisible anchor only), `TerminalFramingWidth` 1.25 → 1.5, and `ScreenInsetScale` 0.92 → 0.76. `dotnet build`: 0 errors, 10 pre-existing warnings.
- Screenshot review showed the overlay still felt detached: it was too large, status HUD showed through, and the glow rectangle spilled visibly around the screen. Cleanup pass: `TerminalFramingWidth` 1.5 → 2.1, `ScreenInsetScale` 0.76 → 0.58, overlay min size reduced, glow opacity/expansion reduced, and `StatusLayer` is hidden while terminal overlay is open then restored on close. `dotnet build`: 0 errors, 10 pre-existing warnings.
- Follow-up fit/parallax pass: user wanted the UI a little larger, the CRT view slightly angled/down/side, and the overlay fitted more tightly to the real glass. Tuned `TerminalFramingWidth` 2.1 → 1.85, terminal camera offset to `(-0.16, 0.12, 1.42)` looking at `(0.04, -0.08, 0)`, `ComputerTerminal3D.ScreenWidth` 0.9 → 0.74, `ScreenHeight` 0.5 → 0.42, `ScreenCenterY` 0.35 → 0.335, `ScreenZOffset` 0.10 → 0.105, and `ScreenInsetScale` 0.58 → 0.66. `dotnet build`: 0 errors, 10 pre-existing warnings.
- Second parallax/fit pass from screenshot: pitch was good but yaw needed reversing/reducing; screen anchor needed moving up; UI needed screen-plane transform. Tuned camera offset to `(0.09, 0.12, 1.42)` and look target to `(-0.02, -0.08, 0)`, moved screen anchor up (`ScreenCenterY` 0.335 → 0.385), adjusted `ScreenInsetScale` 0.66 → 0.70, and changed `TerminalOverlay.SetScreenBounds()` to rotate the overlay/glow to match the projected top screen edge. Tried full affine transform first, but Godot C# `Control` does not expose arbitrary `Transform2D`; final pass uses supported `Position`/`Size`/`Rotation`. `dotnet build`: 0 errors, 10 pre-existing warnings.
- Added procedural glass overlay on top of the CRT UI (`CrtGlassOverlay`) with `MouseFilter.Ignore`: faint top/diagonal reflection bands, edge highlights, small scratches, and subtle smudges so the UI reads as behind glass. `dotnet build`: 0 errors, 10 pre-existing warnings.
- Screenshot review showed quarter-circle corner artifacts and a slight screen aspect/fit mismatch. Removed the large circular vignette corner draws, reduced glass smudge circle sizes/opacity, changed `ScreenAspect` 1.8 → 16:9, nudged the overlay center up by 6px via `ScreenFitOffset`, and increased `ScreenInsetScale` 0.70 → 0.72. `dotnet build`: 0 errors, 10 pre-existing warnings.
- User asked to shrink the UI to fit within the cyan/glass part of the monitor screen. Tuned `TerminalOverlay.ScreenInsetScale` 0.72 → 0.58. `dotnet build`: 0 errors, 10 pre-existing warnings.
- Follow-up: user wanted UI slightly bigger, moved down, with rounded-corner/transparent border-gradient integration. Tuned `ScreenInsetScale` 0.58 → 0.62 and `ScreenFitOffset` `(0, -6)` → `(0, 4)`. Reworked `DrawVignette()` into stepped edge-gradient bands and small dark corner masks to imply rounded glass corners without reintroducing large quarter-circle artifacts. `dotnet build`: 0 errors, 10 pre-existing warnings.
- Edge still reads as a hard straight cut even after the vignette feather; user wants the UI edge to blend to transparent so the monitor prop shows through. Evaluated `CanvasGroup` approach — ScrollContainer (`CallerTab` incoming/on-hold lists) sets `clip_contents=true`, and the CanvasGroup docs explicitly say children with clip_children set don't work correctly (both use the backbuffer). Rejected CanvasGroup.
- Implemented shader-based edge masking instead: new `shaders/crt_screen_feather.gdshader` (rounded-corner SDF box + edge feather), applied via `ShaderMaterial` to the `ProjectedCrtScreen` frame so the material propagates to the phosphor background AND all CRT effect layers, fading all of them to alpha 0 at the screen perimeter. `screen_size` uniform updated each `SetScreenBounds()`.
- `dotnet build`: 0 errors, 10 pre-existing warnings.

### Todo / Next Steps
- [x] `TerminalFramingWidth` 1.6 → 1.0.
- [x] SubViewport size (960,640) → (1152,640).
- [x] `ForwardTerminalMouse`: ButtonMask forwarded on motion; InputEventMouseButton passthrough kept (covers wheel).
- [x] Build + tests (baseline 487 pass / 13 fail).
- [x] Step 1: remove `MouseFilter.Ignore` on `screenRoot` + `_terminalTab` (build/tests green). User verified: still broken.
- [x] Step 2: add bounded telemetry (build green). User diagnostic: no logs at all.
- [x] Step 3: root-caused the PreShow full-rect `MouseFilter.Stop` GUI swallowing mouse; moved terminal mouse + Esc to `World3D._Input()` (pre-GUI). Build green.
- [ ] User verification of Step 3 (hover/click/scroll/Esc works).
- [x] Step 4: process-based mouse hover/click fallback + on-screen debug status (build green).
- [x] Step 5: projected CRT overlay with native UI input (build green).
- [ ] In-editor verify: overlay appears aligned to CRT, CallerTab hover/click/scroll works, Esc/X/<- close returns to zoomed-out game.
- [x] Polish pass: wider zoom, centered/inset UI, stronger CRT effects (build green).
- [ ] In-editor tune values if needed: `TerminalFramingWidth`, `ScreenInsetScale`, glow opacity, scanline opacity, dust opacity.
- [x] Fit pass: real CRT GLB remains visible during zoom; procedural terminal becomes invisible anchor; overlay scale/zoom tuned down (build green).
- [ ] In-editor verify/tune: anchor lines up with real GLB glass; if offset, tune `ComputerTerminal3D.ScreenWidth`, `ScreenHeight`, `ScreenCenterY`, `ScreenZOffset`, or node position.
- [ ] Re-check screenshot/in-editor fit: overlay should be smaller, HUD hidden, and no large green glow rectangle around the monitor.
- [ ] Re-check screenshot/in-editor fit after parallax pass: screen should feel closer/larger, with subtle side/top angle; if perspective mismatch is visible, tune camera offset smaller or revert toward straight-on.
- [ ] Re-check screenshot/in-editor fit after rotated overlay pass: yaw should be opposite/reduced, overlay should sit higher and rotate with the monitor edge; if not enough, next step is shader/texture-based UI for true perspective warp (more complex input handling).
- [ ] Evaluate glass overlay in-editor: if it hurts readability, reduce `DrawGlass` alpha values; if too subtle, slightly raise top band/scratch alpha.
- [ ] Re-check edge artifacts and glass fit after removing corner circles; if still off, tune `ScreenFitOffset`, `ScreenAspect`, or `ComputerTerminal3D` anchor dimensions.
- [ ] If hover still dead after the event reaches the SubViewport: structural fix (drop inner CanvasLayer, controls directly under SubViewport).
- [ ] Remove telemetry + debug preview + `ScreenDebug` sampling once confirmed.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/props/ComputerTerminal3D.cs`
- `scripts/world3d/World3D.cs`

---

## Previous Session

**Branch**: 3d-migration
**Task**: Document the GPT-assisted Blender 3D prop workflow for future development.
**Status**: Completed

### Work Done
- Reworked `docs/art/3D_ASSET_WORKFLOW.md` from a trial-result note into a reusable developer workflow.
- Documented toolchain requirements, folder conventions, GPT prompt template, generator pattern, validation checklist, Godot placement checklist, accepted trial assets, and troubleshooting.
- Preserved the audio cabinet and microphone stand dimensions/placement as concrete examples for future props.

### Files Modified
- `SESSION_LOG.md`
- `docs/art/3D_ASSET_WORKFLOW.md`

### Next Steps
1. Use this workflow for the next one or two simple props before scaling production.
2. If the workflow continues to hold up, add a small Godot review scene or automated prop placement helper.

---

## Previous Session

**Branch**: 3d-migration
**Task**: Import generated 3D prop GLBs into the world scene.
**Status**: Completed

### Work Done
- Started placement pass for `audio_cabinet.glb` and `microphone_stand.glb` in `World3D.tscn`.
- Preserving existing generated collision boxes while replacing only placeholder visuals.
- Added both GLBs as external packed-scene resources in `World3D.tscn`.
- Replaced the control-room audio cabinet placeholder visual with `audio_cabinet.glb` at bottom-origin room-local position `(4.1, 0, -2.3)`.
- Replaced the studio mic cylinder visual with `microphone_stand.glb` at room-local position `(0.95, 0.2, 0.65)`.
- Rotated both models 180 degrees around Y so their generated fronts face back toward the room/gameplay camera.
- Verified with `dotnet build`; build passes.
- Ran Godot 4.6.3 headless project check; GLBs imported without new model-reference errors. Existing unrelated invalid UID warning remains in `scenes/world/World.tscn`.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`

### Next Steps
1. Playtest `Game3D.tscn` and check scale/readability under actual station lighting.
2. Tune positions/rotations if the cabinet or mic face the wrong direction in the gameplay camera.

---

## Previous Session

**Branch**: 3d-migration
**Task**: Restore 3D room-based audio (control = full audio, studio = Vern only, else muffled) and fix the broken GoDotTest test toolchain.
**Status**: In Progress

### Work Done
- Diagnosed why all audio was muffled after the 3D migration: `RoomStateManager._Process()` looks up the player via `GetTree().GetFirstNodeInGroup("player") as Player` (2D `Player` type); `Player3D : CharacterBody3D` is never added to the `"player"` group, so the lookup always fails and `CurrentLocation` stays `Outside` forever; `BroadcastAudioService` then initializes everything muffled and `PlayerLocationChanged` never fires. The 3D world also never registered room bounds (`SetControlRoomBounds`/`SetStudioBounds`) — only the 2D rooms did.
- Added `RoomStateManager.SetPlayerLocation(PlayerLocation)` (-manual reporting API with a `_manualLocation` guard so 2D bounds detection cannot fight it); emits `PlayerLocationChanged` only on change.
- Wired `World3D._UpdatePlayerRoomState()` to report `InControlRoom` (room or control-room doorway), `InStudio` (studio), else `Outside` to `RoomStateManager` every frame via the cached `/root/RoomStateManager` autoload.
- Added `using KBTV.Core;` to `World3D.cs` to fix `CS0246: RoomStateManager could not be found`.
- Added `tests/unit/core/RoomStateManagerTests.cs` (3 tests: updates location, emits only on change, disables bounds detection in `_Process`).
- Root-caused the "tests hang" issue: `--run-tests` was never honored because the main scene is the game (`Game3D.tscn`); no harness checks the flag. GoDotTest must be launched with the test scene explicitly (`res://test/Tests.tscn`). The `godot` CLI binary was also not on PATH and the documented engine was the wrong version (4.5.1) — running the 4.6 project with 4.5.1 throws `FileAccess.GetAsText()` `MissingMethodException`.
- Added `run-tests.ps1`: auto-detects a Godot 4.6 mono console build (ignores a wrong-version `GODOT` env var with a warning), launches `test/Tests.tscn` via `--main-scene`/scene arg, parses the GoDotTest summary, and exits non-zero when any test fails.
- Copied the Godot 4.6.3 mono install from `opencode\godot463` (temp) into `D:\Software\Godot\Godot_v4.6.3-stable_mono_win64` so the toolchain is stable (Temp gets cleaned).
- Updated `report-tests.bat`, `run_tests_quick.bat`, `run_tests_capture.bat` (fixed `TestRunner.tscn` → `Tests.tscn`) and report-tests.sh to the 4.6 engine / correct args.
- Updated `AGENTS.md` and `docs/testing/TESTING.md`: replace `godot --run-tests` with `pwsh -NoProfile -File run-tests.ps1`, correct engine version notes, fix the CI example.
- Verified: `dotnet build` passes; full suite runs via `run-tests.ps1` → **Passed: 487 | Failed: 13 | Skipped: 0**. All 13 failures are pre-existing (AutoInject providers missing in tests: `No provider found for service GameStateManager/EventBus/TimeManager`, etc.) and unrelated to this work. The 3 new `RoomStateManagerTests` pass.

### Files Modified
- `SESSION_LOG.md`
- `scripts/core/RoomStateManager.cs`
- `scripts/world3d/World3D.cs`
- `tests/unit/core/RoomStateManagerTests.cs` (new)
- `run-tests.ps1` (new)
- `report-tests.bat`
- `report-tests.sh`
- `run_tests_quick.bat`
- `run_tests_capture.bat`
- `AGENTS.md`
- `docs/testing/TESTING.md`

### Next Steps
1. Playtest audio in 3D: control room = full audio, studio = Vern only, corridors/equipment = muffled.
2. Optionally fix the 13 pre-existing test failures (missing AutoInject providers) in a follow-up pass.
3. Consider making the temp/`GODOT` env var point at the new stable 4.6.3 engine path (currently auto-detected).

---

## Previous Session

**Branch**: 3d-migration
**Task**: Generate first Blender-authored 3D props: audio cabinet and microphone stand.
**Status**: Completed

### Work Done
- Confirmed clean working tree and selected reproducible Blender Python to GLB workflow.
- Targeting meter-scale, bottom-center origins, and Godot-facing negative Z.
- Generated both GLBs, editable Blender sources, neutral-lit previews, and validation reports.
- Fixed degenerate bevel geometry before export and verified both GLBs by clean re-import.
- Inspected both preview images; documented generation and Godot placement workflow.

### Files Modified
- `SESSION_LOG.md`
- `AGENTS.md`
- `Tools/modelgen/` (Python generators and editable `.blend` sources)
- `assets/models3d/props/audio_cabinet.glb`
- `assets/models3d/props/microphone_stand.glb`
- `docs/art/3D_ASSET_WORKFLOW.md`
- `docs/art/model_previews/` (PNG previews and JSON validation reports)

### Next Steps
1. Import/place trial props in Godot and review under station lighting.
2. Tune silhouette and small details based on gameplay-camera feedback.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Remove rectangular haze veil artifacts and rely on true room-filling studio fog.

**Status**: Completed

### Work Done
- Started implementation pass for a persistent smoky 3D studio.
- Confirmed active main scene is `Game3D.tscn`, so smoke belongs in `StudioRoom3D` rather than the older 2D `StudioSmoke` path.
- Added `StudioSmoke3D`, a procedural 3D billboard-smoke node with persistent ambient haze and periodic cigarette puff bursts.
- Wired `StudioRoom3D` to create the smoke node with exported tuning values for density, opacity, puff timing, drift, origin, and room extents.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Removed the three `StudioFogVeil` billboard fallback planes after playtest showed them as distinct fog rectangles.
- Removed obsolete haze texture/shader generation and `HazeSwirlSpeed` / `HazeSwirlStrength` exports.
- Raised the actual studio-local `FogVolume` density default via `AmbientSmokeOpacity` from `0.24` to `0.45` so the fog fills the room without visible cards.
- Kept Vern puffs and continuous door-leak smoke behavior intact.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Lowered true studio `FogVolume` density default from `0.45` to `0.38` after playtest showed the no-rectangle fog looked good but too dense.
- Added `FogMotionSpeed` and `FogMotionStrength` exports that subtly animate true fog density, position, and X/Z size so the room haze breathes without reintroducing haze-card rectangles.
- Set default fog motion to `FogMotionSpeed = 0.24` and `FogMotionStrength = 0.08`.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Increased true fog motion visibility after playtest showed the movement was still hard to notice.
- Raised `FogMotionSpeed` from `0.24` to `0.55` and `FogMotionStrength` from `0.08` to `0.22`.
- Widened fog density pulsing and increased fog volume position/size modulation while keeping the effect on the real `FogVolume` only.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started follow-up after playtest showed haze swirl was not noticeable and door smoke should continuously leak while doors are open.
- Increased haze animation defaults: `HazeSwirlSpeed` to `0.11`, `HazeSwirlStrength` to `0.46`, and added more visible slow veil position/scale movement.
- Lowered door leak opacity to `0.045` and added `DoorLeakInterval` so door wisps emit sparsely and continuously while a studio door is open.
- Replaced the one-shot player-transition leak trigger with door-state-driven leak activation from `DoorLightLinkChanged` for the control/studio and studio/hall doors.
- Added separate state/timers for each studio door leak so either studio exit can leak independently while open.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Raised default studio haze opacity from `0.16` to `0.24` in `StudioRoom3D` and `StudioSmoke3D` after playtest feedback that the fog was not visible enough.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started animation pass after the broad haze read better but still felt too flat/static.
- Replaced the haze veil standard material with a small shader that slowly drifts and blends two haze texture samples for subtle swirl/movement.
- Added `HazeSwirlSpeed` and `HazeSwirlStrength` exports on `StudioRoom3D` and `StudioSmoke3D`.
- Added a separate door-leak smoke pool and `EmitDoorLeak` method for subtle outward wisps at studio exits.
- Added `StudioRoom3D.EmitDoorSmokeLeak(...)` and wired `World3D` to trigger it once when the player leaves `STUDIO` for any other resolved room/doorway.
- Kept Vern's existing cigarette puff behavior unchanged.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started visibility pass after `AmbientSmokeOpacity` changes were not visibly affecting the studio haze.
- Added broad non-puffy studio fog veils as a visible orthographic fallback, driven by the same `AmbientSmokeOpacity` knob as the local `FogVolume`.
- Raised `AmbientSmokeOpacity` default to `0.16` in both `StudioRoom3D` and `StudioSmoke3D` so the active room passes a visible value at runtime.
- Lightened the fog color and flattened the `FogVolume` falloff so the haze reads more like cigarette smoke in the whole room.
- Kept Vern's local cigarette puff behavior unchanged.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started fog-only revision after playtest showed the room smoke cloud billboards still read as unnatural isolated puffs.
- Removed the whole-room billboard cloud layer and its exports (`RoomCloudCount`, `RoomCloudOpacity`).
- Tuned the local studio `FogVolume` to carry the room smoke: `AmbientSmokeOpacity` now defaults to `0.085`, with flatter `HeightFalloff` and softer `EdgeFade`.
- Kept cigarette puff behavior and opacity unchanged.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started room-fog visibility pass after playtest confirmed puffs look good but the overall studio smoke is not noticeable enough.
- Raised studio-only `FogVolume` density via `AmbientSmokeOpacity` from `0.022` to `0.048`.
- Raised full-room smoke cloud layer from `7` to `9` clouds and `RoomCloudOpacity` from `0.045` to `0.07`.
- Kept cigarette puff opacity/timing unchanged because the puff effect is already reading well.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.
- Started iteration after playtest showed the first-pass billboard quads render as visible blocky rectangles.
- Replaced the ambient billboard field with a studio-local `FogVolume` using a low-density cool grey `FogMaterial`.
- Reworked cigarette puffs to use procedural soft/noisy alpha textures on billboard quads, avoiding visible rectangular cards.
- Fixed the Godot C# fog shape enum to `RenderingServer.FogVolumeShape.Box`.
- Enabled volumetric fog globally at zero density in `StationLighting3D` so local `FogVolume` nodes can render without adding world-wide haze.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Started tuning pass after playtest confirmed the improved smoke shape works but should be subtler, with a separate whole-room smoky cloud.
- Lowered default studio smoke intensity: `AmbientSmokeOpacity` from `0.105` to `0.022`, puff opacity from `0.24` to `0.16`.
- Added `RoomCloudCount` and `RoomCloudOpacity` exports to drive a separate subtle whole-room smoke cloud layer.
- Added slow oversized procedural smoke cloud billboards across the studio volume, separate from the local fog volume and cigarette puffs.
- Removed the unused ambient smoke count export from the 3D smoke implementation.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StudioSmoke3D.cs`
- `scripts/world3d/StudioSmoke3D.cs.uid`
- `scripts/world3d/StudioRoom3D.cs`
- `scripts/world3d/StationLighting3D.cs`

### Next Steps
1. Playtest true `FogVolume` visibility at `AmbientSmokeOpacity = 0.38`.
2. Tune `FogMotionSpeed` / `FogMotionStrength` if the haze motion is still too subtle or becomes distracting.
3. Playtest both studio exits and tune `DoorLeakSmokeOpacity` / `DoorLeakInterval` if the continuous leak is too visible or too sparse.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Fix fluorescent shadow halting in the rest of the world.

**Status**: Completed

### Work Done
- Started shadow-system pass for fluorescent lights and stuck-looking shadows.
- Found fluorescent wash/fill lights had `ShadowEnabled = false`.
- Found flat generated 3D floor/route-marker meshes use default mesh shadow casting, which can create fixed ground shadows.
- Found 2D `PropBuilder` accepts `createCastShadow` but never creates the shadow.
- Enabled shadows on fluorescent wash spotlights while leaving broad fill lights non-shadowed.
- Disabled mesh shadow casting on generated floors, walkway, route markers, and scene-authored 3D floor/threshold meshes.
- Wired 2D prop cast-shadow creation in `PropBuilder` for both auto-collider and explicit-collider paths.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.
- User confirmed the halting/stuck-looking shadow issue still appears under the rest-of-world fluorescent lights.
- Changed station fluorescents so all wash/fill lights still illuminate, but only the nearest fluorescent wash spotlight casts shadows each frame.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`
- `scripts/world3d/StationGreybox3D.cs`
- `scripts/world/common/PropBuilder.cs`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/World3D.cs`

### Next Steps
1. Playtest the hallway/rest-of-world fluorescents and confirm the player shadow no longer appears to halt or leave fixed duplicates behind.
2. If the nearest-light handoff is too abrupt, add a small distance hysteresis before switching fluorescent shadow casters.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Fix 3D control room window frame aliasing and stray green object.

**Status**: Completed

### Work Done
- Started a focused pass on the 2D control room north wall/window rendering.
- Identified that `WallSystem` hardcodes the generic `wall_window_atlas.png`, which contains the visible green object.
- Identified unused control-room-specific assets: `control_room_north_atlas.png` and `control_room_north_window_atlas.png`.
- Added configurable window texture, frame count, and offset settings to `WallSystem` while preserving the old generic defaults.
- Configured `ControlRoom` to use `control_room_north_atlas.png` and `control_room_north_window_atlas.png`.
- Reduced the control room visual window span from columns `3..9` to `6..7` to match the 2-frame control room window asset.
- Verified with `dotnet build`; build passes with existing warnings.
- User provided a screenshot showing the artifact is in the 3D control room, not the 2D wall system.
- Identified the scene-authored `OnAirSign` mesh at world `z ~= -0.05`, directly behind/on the generated control/studio window plane.
- Identified the generated 3D window half-wall and frame pieces share the same `z = 0` plane/depth as adjacent wall geometry, making z-fighting likely.
- Restored the generated 3D control/studio window half-wall and frame pieces to the wall centerline/full wall depth so they remain part of the wall.
- Hid the scene-authored green `OnAirSign` placeholder in `World3D.tscn` so it no longer renders inside the window.
- Corrected the initial 3D offset approach after playtest showed the trim no longer lined up with the wall.
- Removed duplicate generated corner posts at the control/studio window jambs so the window frames themselves fill those wall endpoints without overlapping extra wall blocks.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world/common/WallSystem.cs`
- `scripts/world/control_room/ControlRoom.cs`
- `scripts/world3d/StationGreybox3D.cs`
- `scenes/world3d/World3D.tscn`

### Next Steps
1. Playtest the 3D control room and confirm the window frame no longer flickers/aliases.
2. Replace the hidden placeholder `OnAirSign` with a real red sign on a non-window wall when signage art/layout is ready.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Change door-open lighting to localized doorway spill.

**Status**: Completed

### Work Done
- Started a refinement pass so open doors create localized doorway light spill instead of lighting the whole adjacent room.
- Removed open-door whole-room light mask widening from `StationLighting3D`.
- Kept primary control, studio, equipment, and station lights permanently isolated to their own visual layers.
- Added disabled-by-default doorway spill lights for Control/Studio, Control/Station, Studio/Station, and Equipment/Station links.
- Door-open events now toggle only the localized short-range spill lights for the matching doorway.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`

### Next Steps
1. Playtest open doors and tune doorway spill `LightEnergy`, `OmniRange`, and `OmniAttenuation` in `StationLighting3D` if the glow is too wide or too subtle.
2. If omnidirectional spill still feels too round, replace specific doorway spills with directional spot spill lights aimed through each opening.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Add room-isolated 3D lighting with door-open light spill.

**Status**: Completed

### Work Done
- Started a lighting isolation pass to prevent light bleed through walls while letting open doors link light between adjacent rooms.
- Converted `StationLighting3D` into a stateful node that tracks control, studio, equipment, and station light groups.
- Added visual/light layer constants and cull-mask refresh logic so each room's lights affect only that room by default.
- Added door-open light links for Control/Studio, Control/Station, Studio/Station, and Equipment/Station doors.
- Assigned generated station floors/props to room-specific visual layers, with shared walls/doors on interior layers.
- Assigned scene-authored control/studio meshes and the player visual to the correct lighting layers, including multi-room player lighting at thresholds.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`
- `scripts/world3d/StationGreybox3D.cs`
- `scripts/world3d/World3D.cs`

### Next Steps
1. Playtest closed doors between control/studio/hall/equipment to confirm light no longer bleeds across floors/props.
2. Playtest opening those doors to confirm adjacent-room light spill appears only while the door trigger is active.
3. If walls still look too globally lit, split shared wall meshes into per-room visual layers in a follow-up pass.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Tune 3D lighting with centered room lamps and hidden brighter fluorescents.

**Status**: Completed

### Work Done
- Started a lighting tune to simplify control/studio/equipment lighting to one centered overhead lamp per room and make fluorescents brighter, whiter, and hidden.
- Replaced the two-light control room setup with one centered overhead pendant/spot.
- Replaced the two-light studio setup with one centered overhead pendant/spot.
- Added one centered overhead pendant/spot for the equipment room.
- Changed fluorescent hue closer to white, increased fill/wash strength, and stopped rendering visible fluorescent fixture bars.
- Raised ambient energy slightly to keep the noir mood readable.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`

### Next Steps
1. Playtest the latest lighting pass in Godot and tune brightness/range if needed.
2. Do the separate 3D player/prop shadow policy pass after the lighting baseline is approved.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Correct first-pass 3D lighting readability and fluorescent behavior.

**Status**: Completed

### Work Done
- Started a correction pass after playtest showed the noir rooms were too dark and fluorescent fixtures were visible without useful light.
- Raised the dark ambient environment and exposure enough to keep unlit geometry readable.
- Broadened and brightened the control/studio overhead spots while keeping shadows only on the main noir practicals.
- Changed fluorescent fixtures from shadowed spotlights into broad no-shadow omni fill plus a soft downward wash.
- Disabled shadow casting on decorative light fixtures so light bars, pendant shades, cords, and bulbs do not block their own lights.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`
- `scripts/world3d/StationLighting3D.cs.uid`
- `scripts/world3d/World3D.cs`
- `scenes/world3d/World3D.tscn`

### Next Steps
1. Playtest readability in Godot and tune `StationLighting3D` light energy/range if any room is still too dark or too flat.
2. Do the separate 3D player/prop shadow policy pass after the lighting baseline feels right.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Add first-pass 3D noir and fluorescent lighting.

**Status**: Completed

### Work Done
- Started the first 3D lighting pass for dark noir control/studio rooms and cooler fluorescent station spaces.
- Added `StationLighting3D`, which builds a dark `WorldEnvironment`, warm overhead spotlights with visible pendant fixtures for the control room and studio, accent glows, and cooler fluorescent station lighting.
- Wired `StationLighting3D` into `World3D._Ready()`.
- Removed the old broad `DirectionalLight3D` from `World3D.tscn` so practical lights define the mood.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationLighting3D.cs`
- `scripts/world3d/World3D.cs`
- `scenes/world3d/World3D.tscn`

### Next Steps
1. Playtest the room readability and tune light energy/range/positions in `StationLighting3D`.
2. Do a separate 3D shadow pass for explicit player/prop shadow policy and bias/contact-shadow tuning.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Tune double doors and move the control/studio doorway left to avoid speaker overlap.

**Status**: Completed

### Work Done
- Started a follow-up pass to make double doors twice the regular door size, open one/both leaves based on player position, and shift the control/studio doorway left.
- Changed double doors to use `SingleDoorWidth * 2f` while keeping regular doors narrow.
- Expanded exterior double-door wall gaps back out to fit the larger two-leaf doors.
- Added per-door center/orientation and per-leaf side signs so double doors open one side when the player is off-center and both sides when the player is near the split.
- Kept double-door leaves swinging outward by retaining explicit per-leaf open rotations.
- Moved the control/studio doorway left in generated greybox markers, generated door placement, room doorway checks, and scene trigger/threshold nodes.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationGreybox3D.cs`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StudioRoom3D.cs`
- `scenes/world3d/World3D.tscn`

### Next Steps
1. Playtest each double door by entering on left/right/center to confirm one-leaf vs two-leaf behavior feels correct.
2. Check the moved control/studio doorway against the left speaker and wall jambs in-editor.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Tune 3D greybox doors after playtest: narrower doors, no player collision, and outward double-door swing.

**Status**: Completed

### Work Done
- Started a door tuning pass from playtest feedback.
- Reduced generated door panel widths to about half the previous size.
- Tightened generated wall gaps around door openings so the narrower greybox doors fit better visually.
- Removed temporary `StaticBody3D` collision from generated door panels so they no longer block or snag the player.
- Changed double exterior doors to store explicit per-leaf open rotations so both leaves swing toward the outside.
- Verified with `dotnet build`; build passes with existing warnings.
- Ran `git diff --check`; no whitespace errors reported, only existing line-ending warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationGreybox3D.cs`

### Next Steps
1. Playtest in Godot to confirm the tightened wall gaps still leave comfortable player clearance.
2. Check each exterior double door from camera view to verify its outward swing reads correctly.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Add 3D greybox doors at marked level openings with fixed-direction hinge animation and auto-close triggers.

**Status**: Completed

### Work Done
- Started a focused pass on 3D greybox doors using the existing marked threshold locations in `StationGreybox3D`.
- Added generated greybox door leaves for every marked threshold in `BuildRouteMarkers()`.
- Added single-door generation for interior openings and double-door generation for exterior openings.
- Added `Area3D` trigger volumes per doorway; doors open while the player overlaps the trigger and close after the player exits.
- Kept hinge rotation fixed per doorway so doors do not flip direction based on approach side.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scripts/world3d/StationGreybox3D.cs`

### Next Steps
1. Playtest the 3D scene in Godot to confirm each door swings to the expected side and does not snag the player capsule.
2. If any door feels too tight, widen that doorway trigger or disable temporary panel collision for greybox traversal.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Add automatic 3D greybox wall corner caps across the full layout and document the pattern.

**Status**: Completed

### Work Done
- Started a focused pass to make corner caps a generated wall-layout rule instead of a manually patched exception.
- Replaced the partial hardcoded cap list in `StationGreybox3D` with endpoint-driven cap generation for all horizontal/vertical generated wall segments.
- Added a shared wall-corner-post position set so each wall endpoint receives one deduplicated wall-height cap/post.
- Documented the 3D greybox wall construction pattern in `docs/technical/THREED_MIGRATION_PLAN.md`: trim wall segments, fill L/T/cross seams with explicit posts, and avoid arbitrary wall extension.
- Added the 3D migration plan to `AGENTS.md` referenced docs so future agents check the greybox wall rules.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `AGENTS.md`
- `docs/technical/THREED_MIGRATION_PLAN.md`
- `scripts/world3d/StationGreybox3D.cs`

### Next Steps
1. Playtest the full station layout to confirm the generated endpoint caps fill every visible wall seam without over-framing door openings.
2. If any doorway cap reads too chunky, add a small rule to skip caps for selected door threshold endpoints.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Fix the 3D greybox control-room audio cabinet clipping through the east wall.

**Status**: Completed

### Work Done
- Started a focused audio cabinet fit pass after playtest showed it clipping through the right wall.
- Moved `AudioCabinet` inward from the east wall and reduced its width/depth scale so it fits within the control room.
- Updated `AudioCabinetCollider` to match the smaller cabinet.
- Verified with `dotnet build`; build passes with existing warnings.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/ControlRoom3D.cs`

### Next Steps
1. Playtest the cabinet against the east wall and tune another small nudge if it still visually touches the wall from the camera angle.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Polish the 3D greybox control-room artifact, cabinet placement, wall corners, and wall fading.

**Status**: Completed

### Work Done
- Started greybox polish pass from latest playtest feedback.
- Removed the green board behind/next to the control/studio window; it was the `BoardWall` mesh in `World3D.tscn`.
- Moved the audio cabinet farther right against the control room east wall and updated its collider.
- Added explicit wall corner posts to fill the square gaps created by trimmed wall segments at L/T junctions.
- Added per-wall material instances and player-proximity wall fading for generated greybox walls.
- Wired `World3D` to pass the player to `StationGreybox3D` for wall fading, with a fallback lookup in the greybox.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StationGreybox3D.cs`
- `scripts/world3d/World3D.cs`

### Next Steps
1. Playtest wall fading in-editor and tune `WallFadeAlpha`, fade distance, or whether vertical side walls should fade more/less aggressively.
2. Add more corner posts if playtest reveals gaps in support-room wall junctions outside the current visible route.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Correct the 3D greybox control room desk, chair, wall, doorway, window, and camera framing.

**Status**: Completed

### Work Done
- Started control/studio correction pass from latest playtest feedback.
- Increased generated greybox wall height and changed wall helper placement so corners meet flush instead of visually doubling up.
- Closed the control/studio west wall connection and moved the control-to-studio doorway to the upper-left side of the shared wall.
- Added a larger control/studio half-wall window frame aligned with the control desk.
- Pushed the control desk against the north wall and moved the phoneboard, soundboard, computer, wall board, and speakers onto/against the desk area.
- Removed the control chair collider and added gentle proximity-based chair movement so it moves out of the player's way without blocking navigation.
- Zoomed the 3D camera in by reducing orthographic size from 12.5 to 10.5.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StudioRoom3D.cs`
- `scripts/world3d/StationGreybox3D.cs`

### Next Steps
1. Playtest the control room to confirm the upper-left studio doorway and window frame read correctly from the new camera framing.
2. Tune prop spacing if the desk-top items overlap visually in the editor.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Rework the 3D greybox control room and studio layout from the supplied sketch.

**Status**: Completed

### Work Done
- Started a 3D-only control room / studio greybox layout pass.
- Confirmed active work is in `scenes/world3d/World3D.tscn` plus `scripts/world3d/ControlRoom3D.cs`, `scripts/world3d/StudioRoom3D.cs`, and `scripts/world3d/StationGreybox3D.cs`.
- Removed duplicate room-local visible wall meshes from `ControlRoom3D` and `StudioRoom3D` in the 3D scene so the generated station greybox owns the visible wall grid.
- Removed old room-local wall colliders from `ControlRoom3D.cs` and `StudioRoom3D.cs`; kept floor and prop colliders aligned to the new layout.
- Repositioned the control room greybox: desk on the north side, boards/computer on the desk, chair below it, speakers flanking it, audio cabinet at the upper right, shelves along the lower wall.
- Repositioned the studio greybox: bookcases on the north wall, table and Vern group in the lower-middle, mic stand near the table, and a control-window marker on the studio/control boundary.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StudioRoom3D.cs`

### Next Steps
1. Playtest the 3D greybox to confirm the room-local wall duplicates are gone and movement has no invisible blockers.
2. Tune individual prop positions after reviewing the camera framing in-editor.

---

## Previous Session

**Branch**: 3d-migration

**Task**: Clean up the 3D station greybox wall grid, corners, and door openings.

**Status**: Completed

### Work Done
- Started first expanded greybox modeling pass based on `docs/design/station-layout-notes.md`.
- Added generated support-room greybox geometry for hallway, equipment room, kitchen / break room, and bathroom.
- Opened the control-room east wall so the player can leave the broadcast core into the new hallway.
- Added support-room labels, simple placeholder props, colliders, and floor threshold markers.
- Expanded camera X/Z bounds and status-label room detection to cover the new spaces.
- Verified with `dotnet build`; build passes with existing warnings.
- Attempted `godot --check-only project.godot`, but the `godot` executable is not on PATH in this shell.
- Playtest feedback: collisions work, camera is too high, control room and studio are too large, support rooms are too small, and several rough-draft rooms are missing.
- Lowered the camera pitch from steep top-down toward a more perspective-heavy angle and added subtle yaw for an isometric-like view.
- Shrank the control room and studio footprint from the initial oversized blockout.
- Enlarged support rooms and rebuilt the generated station greybox around the rough Excalidraw room list.
- Added greybox spaces for document/archive, office, lobby/front desk, parking lot, backyard, toolshed, walkway, and ladder to roof.
- Re-ran `dotnet build`; build passes with existing warnings.
- New playtest feedback: camera yaw is too strong, room sizes are improved, but the station layout must be rearranged to match the supplied rough plan.
- Reduced camera yaw from 12 degrees to 6 degrees while keeping the lower perspective angle.
- Rebuilt the greybox layout to follow the supplied sketch: backyard/toolshed west, equipment/studio/control stack left, hallway spine center, document/archive, office, kitchen, bathroom and lobby east, parking lot farther east, walkway and roof ladder south.
- Split right-side hallway walls and studio/control divider walls around intended door openings.
- Opened the studio east wall toward the hallway.
- Re-ran `dotnet build`; build passes with existing warnings.
- New feedback: corrected topology is closer, but some walls/doors are missing, lobby/front desk placement needs adjustment, and the camera should move lower.
- Lowered camera height and reduced pitch again for a more grounded view while preserving the subtle 6-degree yaw.
- Split front desk and lobby into separate top-right zones matching the sketch: front desk above, lobby below.
- Added missing wall segments around archive, office, kitchen, bathroom, front desk, lobby, and parking-lot boundary.
- Added door markers for archive/front-desk and kitchen/bathroom connections.
- Added a hallway-to-lobby connector floor so the central open passage matches the sketch better.
- Re-ran `dotnet build`; build passes with existing warnings.
- New feedback: remove yaw, reduce camera pitch, add missing office wall, keep lobby empty, and replace the lobby/front-desk divider with one long counter.
- Set camera yaw to 0 degrees and centered the camera X offset.
- Reduced camera pitch from -48 degrees to -42 degrees for a lower, more straight-on view.
- Added the missing office north wall.
- Removed the wall-like lobby/front-desk divider and replaced it with one long counter.
- Removed the extra front-desk block so the lobby reads as empty except for the counter relationship.
- Re-ran `dotnet build`; build passes with existing warnings.
- New feedback: reduce pitch to -35, fix the bathroom south wall protrusion, and add north/south outside doors from the middle hallway.
- Set camera pitch to -35 degrees in both `World3D.cs` and `World3D.tscn`.
- Split the main building north and south walls around the central hallway to create exterior door gaps.
- Added north and south exterior door floor markers.
- Shortened/nudged the bathroom south wall so it no longer reads as protruding beyond the building.
- Re-ran `dotnet build`; build passes with existing warnings.
- New feedback: the bathroom south wall still extends too far to the right in runtime.
- Trimmed `MainBuildingSouthEast` from an 18m-wide wall segment down to the 8m bathroom/right-support-room edge.
- Re-ran `dotnet build`; build passes with existing warnings.
- New feedback: general layout is roughly correct; clean walls so they are straight, connected at 90-degree corners, and use consistent door openings.
- Replaced the hand-tuned wall list with helper-driven horizontal and vertical wall segments so corners and openings snap to straight 90-degree geometry.
- Added consistent `DoorGap` spacing and wall helper methods for rectilinear segments.
- Re-ran `dotnet build`; build passes with existing warnings.

### Files Modified
- `SESSION_LOG.md`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/World3D.cs`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StationGreybox3D.cs`
- `scripts/world3d/StationGreybox3D.cs.uid`
- `scripts/world3d/StudioRoom3D.cs`

### Next Steps
1. Playtest the rectilinear wall cleanup for any remaining misplaced openings.
2. Tune specific room proportions only after the wall grid reads cleanly.

---

## Previous Session

**Branch**: feature working tree (3D migration planning)

**Task**: Fix the first 3D migration slice by making the control room and studio one connected floorplan with camera follow and working collisions.

**Status**: Completed - corrected the 3D connected floorplan orientation to match the original 2D layout: control room south, studio north, camera following player movement through room depth. `dotnet build` passes.

### Work Done
- Added `docs/technical/THREED_MIGRATION_PLAN.md` covering scope, what stays, phased rollout, suggested scene structure, room requirements, UI strategy, asset strategy, risks, and first-definition-of-done.
- Updated `docs/technical/TECHNICAL_SPEC.md` to point at the 3D migration plan and note the planned `Game3D.tscn` path.
- Updated `docs/design/ROADMAP.md` with a new `World Presentation Migration` section.
- Updated `docs/design/GAME_DESIGN.md` to reflect the planned 3D world presentation layer.
- Updated `docs/technical/TOPDOWN_BUILDING_PATTERN.md` to direct readers to the 3D migration plan.
- Added `scenes/Game3D.tscn`, `scenes/world3d/World3D.tscn`, and `scripts/world3d/*` for the first 3D scaffold.
- Switched `project.godot` to launch `res://scenes/Game3D.tscn` by default.
- Added a basic 3D room switch loop with `interact` doorway checks and a visible player blockout.
- Tightened the 3D camera toward a more angled top-down framing and nudged room spacing/proportions closer to a flatter layout.
- Started connected-floorplan correction pass after playtest feedback: camera should follow the player, rooms should read clearly, and collisions should work.
- Refactored `World3D` so the control room and studio stay visible together instead of switching/hiding and teleporting the player.
- Added a smoothed orthographic follow camera with exported offset/speed/bounds.
- Split the shared room wall into doorway segments and added floor threshold markers.
- Removed incorrect 90-degree rotations from side-wall meshes so they align with their intended dimensions.
- Added generated `StaticBody3D` wall and major-prop colliders in `ControlRoom3D` and `StudioRoom3D`.
- Verified C# compilation with `dotnet build`.
- Started movement/collision correction after playtest showed the player spawning near wall geometry and unable to move freely around the floor.
- Moved the control room player start to the center of the open floor instead of the lower half near wall/prop paths.
- Added generated floor `StaticBody3D` colliders for both rooms.
- Widened the control-room/studio doorway gap in both visual meshes and collision segments.
- Relaxed camera Z follow bounds so the camera can track room-depth movement.
- Re-ran `dotnet build`; build passes with existing warnings.
- Started north/south orientation correction after playtest feedback: the 3D layout was incorrectly east/west and did not match the original control-room-south/studio-north arrangement.
- Repositioned `ControlRoom3D` south on positive Z and `StudioRoom3D` north on negative Z.
- Moved the shared doorway from east/west walls to the control north / studio south boundary.
- Split the control room north wall around the doorway and removed duplicate studio south wall geometry/collision to avoid overlap.
- Updated doorway trigger positions, doorway detection, and collision segments to use Z-axis movement.
- Adjusted camera bounds so the camera position can actually follow player Z movement after applying its offset.
- Moved the player start farther south of the control room desk to avoid spawning next to the desk collider.
- Re-ran `dotnet build`; build passes with existing warnings.

### Files Modified
- `docs/technical/THREED_MIGRATION_PLAN.md`
- `docs/technical/TECHNICAL_SPEC.md`
- `docs/design/ROADMAP.md`
- `docs/design/GAME_DESIGN.md`
- `docs/technical/TOPDOWN_BUILDING_PATTERN.md`
- `project.godot`
- `scenes/Game3D.tscn`
- `scenes/world3d/World3D.tscn`
- `scripts/world3d/World3D.cs`
- `scripts/world3d/ControlRoom3D.cs`
- `scripts/world3d/StudioRoom3D.cs`
- `scripts/world3d/Player3D.cs`
- `SESSION_LOG.md`

### Next Steps
1. Re-test movement in Godot editor/runtime and confirm up/north reaches the studio.
2. Tune visual prop placement against the old 2D room composition.
3. Run `godot --check-only project.godot` from an environment where the Godot executable is on PATH.
