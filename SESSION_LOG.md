## Current Session

**Branch**: comic-styling
**Task**: Make light posterization affect all room light pools, not only bright hallway pixels.
**Status**: Completed (build + Godot check green; visual review pending)
- Files Modified: `SESSION_LOG.md`, `scripts/world3d/StationFloorMaterials3D.cs`, `scripts/world3d/ControlRoom3D.cs`, `scripts/world3d/StudioRoom3D.cs`, `scripts/world3d/StationGreybox3D.cs`, `scripts/world3d/StationLighting3D.cs`, `scripts/world3d/ComicPostLayer.cs`, `shaders/comic_post.gdshader`, `assets/textures/world3d/{control_carpet,studio_carpet,hall_linoleum,wallpaper_subtle}.png`
- Work Done: User screenshot showed the first procedural runtime texture pass was unacceptable: default box UVs and high-contrast procedural patterns produced huge blotchy artifacts under the comic pass. Reverted that attempt, then generated actual PixelLab texture swatches (seeds 41011/41023/41037/41051) and saved them under `assets/textures/world3d/`. Added `StationFloorMaterials3D`, which loads PNG bytes with `FileAccess.GetFileAsBytes()` + `Image.LoadPngFromBuffer()` (export-safe, no Godot `.import` dependency), builds explicit tiled floor `ArrayMesh` quads, and applies nearest-filtered floor materials. Control/studio floor texturing and global wallpaper were rolled back after they flattened the noir lighting. Replaced the old texture-brightness lifting path with `MakeComicMaskedMaterial(...)`: source PNGs are converted to luma/detail masks around a controlled dark base color, with separate darken/brighten strengths, luma clamps, and optional dark border/grout. Initial hallway luma-mask passes were either blown out or collapsed by comic posterization into mostly flat color with only vague texture outlines. Tried explicit hallway grout geometry, but user screenshot showed it read as oversized black bands, not tile texture, so it was removed. Added post-posterize detail reinjection to `comic_post.gdshader` and `ComicPostLayer`: after lighting is posterized, high-frequency source detail (`original - smooth_source`) is blended back in with configurable strength/threshold/max-luma so texture can survive without driving lighting bands. Changed hallway fluorescents from 3 hot pools to 5 lower-energy overlapping fixtures with wider effective range. Added `SurfacePosterizeStrength` to keep global surface luma posterization off by default. Updated light posterization to use smoothed scene luma plus a local-relative light mask instead of raw absolute pixel luma, with new `LightPosterizeRelativeThreshold`, `LightPosterizeRelativeSoftness`, and `LightPosterizeLocalDarken` controls. This should let warm/darker control and studio light pools qualify for banding, not just bright hallway fluorescents.
- Verification: `dotnet build KBTV.csproj` succeeded with 0 errors and the existing 7 warnings. Godot 4.6.3 mono `--headless --path . --check-only --quit` loaded with no new script/resource errors; it still prints the known pre-existing shutdown disconnect errors from `LiveShowPanel`/`BroadcastAudioService`.
- Next Steps: User visual check. If room lights still do not band enough, lower `LightPosterizeRelativeThreshold` or `LightPosterizeLocalDarken`; if texture starts banding/noising, raise the relative threshold or lower `LightPosterizeStrength`.
- Blockers: none.

---

## Previous Session (comic light posterization)

**Branch**: comic-styling
**Task**: Apply comic posterization to light falloff and transparent glow effects.
**Status**: Completed (build + Godot check green; visual review pending)
- Files Modified: `SESSION_LOG.md`, `shaders/comic_post.gdshader`, `scripts/world3d/ComicPostLayer.cs`, `scripts/world3d/Soundboard3D.cs`
- Work Done: Added a separate light-posterization path to `comic_post.gdshader` so bright light falloff uses coarser bands independent of normal surface posterization. New shader uniforms: `light_posterize_enabled`, `light_posterize_steps`, `light_posterize_strength`, `light_posterize_threshold`, and `light_posterize_softness`; `ComicPostLayer.cs` exposes and pushes matching runtime defaults (`true`, `4`, `0.65`, `0.34`, `0.18`). Kept the previous soundboard render-order fix intact, then changed `Soundboard3D.MakeHaloGradient()` to generate a 4-step radial alpha gradient so knob halos, caller fader glow, and flashing button glow read as posterized even though they render after the comic post pass.
- Verification: `dotnet build KBTV.csproj` succeeded with 0 errors and the existing 7 warnings. Godot 4.6.3 mono `--headless --check-only --quit` loaded the project with no new shader/script-load errors; it still prints the known pre-existing shutdown disconnect errors from `LiveShowPanel`/`BroadcastAudioService`.
- Next Steps: User visual check: confirm studio/control room light falloff has pleasing comic bands, and soundboard halo/button glow bands are visible but not too chunky. Tune `LightPosterizeStrength` first if the room lights are too smooth or too harsh.
- Blockers: none.

---

## Previous Session (soundboard glow render order)

**Branch**: comic-styling
**Task**: Restore the 3D soundboard knob/fader/button glow after the comic post-effect migration.
**Status**: Completed (build + Godot check green; visual review pending)
- Files Modified: `SESSION_LOG.md`, `scripts/world3d/ComicPostLayer.cs`, `scripts/world3d/Soundboard3D.cs`
- Work Done: Read-only trace found the likely regression path: the soundboard halos/fader/button glow pools are alpha-transparent unshaded 3D quads in `Soundboard3D.cs`, while the comic effect was migrated from a CanvasLayer to a spatial full-screen post quad sampling `screen_tex`. Transparent glow quads can be missing from the sampled back-buffer and/or be covered by the post quad depending on render order. Kept glow logic/placement unchanged and only adjusted render ordering: `ComicPostLayer` now assigns the comic post material Godot's lowest render priority (`-128`), while all soundboard halo/fader/button glow materials are configured with the highest render priority (`127`). This lets the post pass establish the comic-treated base image while soundboard transparent glow quads render after it.
- Verification: `dotnet build KBTV.csproj` succeeded with 0 errors and the existing 7 warnings; a final incremental rerun after diff review succeeded with 0 warnings/0 errors. Godot 4.6.3 mono `--headless --check-only --quit` loaded the project with no new shader/script-load errors; it still prints the known pre-existing shutdown disconnect errors from `LiveShowPanel`/`BroadcastAudioService`.
- Note: Final diff review showed `ComicPostLayer.Saturation = 1.9f` versus git's `0.9f`. This was not part of the render-order patch and appears to be concurrent/unrelated; left intact.
- Next Steps: User visual check in soundboard view with comic enabled: confirm knob/fader idle halos, caller fader glow, and flashing button glow appear over the comic-treated board.
- Blockers: none.

---

## Previous Session (comic post darkening/depth robustness)

**Branch**: comic-styling
**Task**: Fix the comic post effect darkening the whole screen, and make depth silhouettes distance-robust without changing the approved look from commit `ad5abda9`.
**Status**: Completed (build + shader compile green; user visual review pending)
- Files Modified: `SESSION_LOG.md`, `shaders/comic_post.gdshader`, `scripts/world3d/ComicPostLayer.cs`
- Work Done: **Root cause:** the depth edge pass compared RAW non-linear depth samples. `depth_edge_sensitivity = 140.0` turned a depth step into `abs_d * 140.0`, so the 0.0-ish threshold needed to trigger an edge was ~`0.00013`. On any surface tilted away from the camera the raw depth gradient across two pixels far exceeds that, so nearly every pixel got inked and the frame went dark. **Fix:** replaced the raw-delta test with a RELATIVE distance test. Depth is linearized, then the jump is measured as a fraction of the farther of the two samples, so a slanted floor stays clean at any camera distance. Switched from an 8-neighbour depth compare to a 2-tap diagonal Roberts cross (half the depth reads, no gradient on flat surfaces), added `depth_edge_width_px` for tap spacing, and made a sample sitting on the far plane always count as a silhouette (background against geometry). Removed `depth_edge_sensitivity`/`DepthEdgeSensitivity` entirely. Replaced normal-vector LENGTH deltas with diagonal dot-product crease severity plus a zero-length normal guard (an unwritten normal buffer samples as zero and `normalize()` would yield NaN). **Depth linearization:** verified the closed form against Godot's own `Projection::set_perspective`, which stores `m22 = -(f+n)/(f-n)` and `m32 = -2fn/(f-n)`; inverting that collapses to `2nf / (f + n - z_ndc*(f - n))`. Numerically confirmed it recovers near and far exactly (max error ~8e-12) across four near/far configs and round-trips distance over the full range. **Godot constraint:** `INV_PROJECTION_MATRIX` AND `PROJECTION_MATRIX` are both rejected in the spatial FRAGMENT stage, so matrix reconstruction is impossible here; near/far are pushed from C# instead. Added `SyncCameraNearFar()` in `_Process`, caching the tracked `Camera3D` so the steady-state cost is two float compares, and holding the last known values if a frame has no active camera. Restored all user-tuned non-depth defaults from `ad5abda9` (`effect_strength 0.78`, `posterize 5`, `saturation 1.22`, `outline_threshold 0.28`, `outline_bias 0.07`, `luma_edge_mix 0.35`, `sobel 0.65`, `normal_edge_mix 0.25`).
- Verification: `dotnet build KBTV.csproj` succeeded with 0 errors (7 pre-existing warnings, unrelated). Godot 4.6.3 mono `--headless --check-only --quit` now reports NO shader errors for `comic_post.gdshader` (it failed on `INV_PROJECTION_MATRIX` and then `PROJECTION_MATRIX` before the uniform switch); the only remaining `ERROR:` lines are the known pre-existing live-show/audio shutdown disconnects. Numeric linearization harness confirmed the relative threshold separates sub-0.5% steps (clean) from 1.6%+ steps (edge) and forces far-plane samples to 1.0. Identifier sweep confirms no stale `depth_edge_sensitivity`, `DepthEdgeSensitivity`, `max_depth_delta`, `max_normal_delta`, `camera_far_plane`, `view_distance`, or matrix-builtin references in code.
- Correction: User reported the scene still rendered black. The standard-Z formula was wrong for Godot 4.6 because Godot 4.3+ uses reverse-Z depth (`1.0` near, `0.0` far). Replaced the near/far closed-form linearization with Godot's documented inverse-projection reconstruction. `INV_PROJECTION_MATRIX` is now captured inside `fragment()` and passed into helper functions, which compiles cleanly. Re-verified `dotnet build KBTV.csproj` with 0 warnings/0 errors and Godot 4.6.3 `--headless --check-only --quit` with no shader errors; only the known audio shutdown disconnect errors remain.
- Correction 2: User still saw only a dark world with the post-comic fog overlay visible. The spatial fullscreen pass was outputting `ALBEDO = vec3(0.0)` and relying on `EMISSION = color`; Godot's documented post-process path writes the sampled/composited color to `ALBEDO`. Switched final output to `ALBEDO = color; EMISSION = vec3(0.0); ALPHA = 1.0;`. Re-verified `dotnet build KBTV.csproj` with 0 warnings/0 errors and Godot 4.6.3 `--headless --check-only --quit` with no shader errors.
- Correction 3: User could see edges again, but most textured surfaces remained black except bright/emissive elements. Cause: noir-lit scene luminance often sits below `0.1`, and `posterize_steps=5` rounded those pixels to the zero band. Added a protected dark-band floor (`0.065`) gated by original luminance (`smoothstep(0.018, 0.055, base_luma)`) so dim textures stay shadowed instead of erased. Also stopped `shadow_smooth_mask` from forcing the final blend to 100%; final mix now respects `effect_strength`. Re-verified `dotnet build KBTV.csproj` with 0 warnings/0 errors and Godot 4.6.3 `--headless --check-only --quit` with no shader errors.
- Correction 4: User reported colors returned but some props/walls read as transparent and outlines needed to be blacker/more opaque. Added a depth-gated surface mask so the dark-band floor applies only to real geometry, not far-plane background, lowered its activation range (`smoothstep(0.004, 0.040, base_luma)`), and raised the floor to `0.095` for dark geometry. Strengthened outline defaults: `OutlineMix=1.15`, `DepthEdgeMix=1.0`, `NormalEdgeMix=0.45`, `LumaEdgeMix=0.45`. Re-verified `dotnet build KBTV.csproj` with 0 errors (existing 7 warnings) and Godot 4.6.3 `--headless --check-only --quit` with no shader errors.
- Correction 5: User reported good edges but some props/walls still looked transparent and posterization looked weak. Found `StationGreybox3D.MakeWallMaterial()` forced all fadeable walls into `TransparencyEnum.Alpha` even at alpha 1.0, which can put opaque walls in Godot's transparent pass after the screen texture is captured; the opaque post quad then covers them. Changed walls to start with `Transparency.Disabled` and only switch to alpha while actual wall fade alpha is below 0.99, then back to disabled when opaque. Changed the dark fill lift to a hard geometry-only band (`0.095 * step(0.004, base_luma)`) instead of a smooth ramp so it stays posterized, and reduced `ShadowSmoothStrength` to 0.25. Re-verified `dotnet build KBTV.csproj` with 0 errors (existing 7 warnings) and Godot 4.6.3 `--headless --check-only --quit` with no shader errors.
- Correction 6: User liked the recovered edges but asked for a darker, more dramatic posterized look and 100% black/opaque ink. Tuned the comic pass darker without touching room lighting: `EffectStrength=0.90`, `Saturation=1.10`, true black `OutlineColor`, `OutlineMix=1.25`, `ShadowSmoothStrength=0.0`, dark geometry band floor lowered to `0.075`, added an ink opacity curve (`smoothstep(0.08, 0.55, ink)`), and applied a subtle noir shadow/midtone grade (`color *= mix(1.0, 0.78, noir_shadow)`) before final blending. Re-verified `dotnet build KBTV.csproj` with 0 errors (existing 7 warnings) and Godot 4.6.3 `--headless --check-only --quit` with no shader errors.
- Correction 7: User still saw brown/tan bleed in edges and weak drama. Root cause: ink was applied before the final original-image blend, so even full ink was diluted by the remaining original color; exported C# defaults also still had `EffectStrength=0.50`, `PosterizeSteps=6`, `Saturation=0.50`, `OutlineMix=1.0`, overriding shader defaults and weakening the pass. Moved ink to the very end after the original/comic blend, steepened the ink curve to `smoothstep(0.03, 0.35, ink)`, added a cooler noir tint (`vec3(0.78,0.82,0.92) * 0.68`) to shadows/midtones, lowered the geometry dark band to `0.060`, and aligned runtime defaults to `EffectStrength=0.90`, `PosterizeSteps=5`, `Saturation=0.75`, `OutlineMix=1.25`. Re-verified `dotnet build KBTV.csproj` with 0 errors (existing 7 warnings) and Godot 4.6.3 `--headless --check-only --quit` with no shader errors.
- Correction 8: User reported inner/depth edges still weak, Vern's face over-inked, and soundboard tracks washed into the board color. Lowered runtime depth edge threshold/bias to `0.045/0.018` so internal geometry depth breaks can ink again, set `OutlineThreshold=0.14`, `OutlineBias=0.045`, `OutlineMix=1.2`, `NormalEdgeMix=0.35`, `LumaEdgeMix=0.55`, and kept `Saturation=0.75`. Added warm/red face-like suppression that reduces only luma/normal ink (depth silhouettes unaffected) to keep Vern's face from becoming hatch noise. Added a small high-frequency detail restore after posterization (`fine_detail` using local Sobel edge but rejecting macro edges) so soundboard tracks/faders retain some chroma contrast without undoing broad comic bands. Re-verified `dotnet build KBTV.csproj` with 0 errors (existing 7 warnings) and Godot 4.6.3 `--headless --check-only --quit` with no shader errors.
- Correction 9: User reported good edges but suspected color/shadow overflow on the player and loss of details. Hardened `comic_post.gdshader` against RGB overflow by adding `clamp_rgb()` and `capped_luma_scale()`, clamping posterize dark-band boosts, shadow-smooth boosts, noir tint output, detail restore, and final ink output. Adjusted fine-detail restore to bring back a small amount of original luma plus chroma instead of chroma-only. Raised runtime `PosterizeSteps` from 5 to 6 and reduced `MedianFilterStrength`/`SobelPrefilterStrength` from `0.25/0.65` to `0.18/0.45` so small player/model details are less likely to be averaged away. Re-verified `dotnet build KBTV.csproj` with 0 errors (existing 7 warnings) and Godot 4.6.3 `--headless --check-only --quit` with no shader errors; only the known shutdown disconnect errors remain.
- Correction 10: User confirmed the black door/player problem persists and clarified the door should be bright from studio/hall lights. Root cause is now treated as pre-post lighting/material clipping, not shader overflow: F8 conflicts with Godot's stop-project shortcut, vertical door faces receive little from down-facing lights, linked door panels are on `AllInteriorLayers`, and door/player materials have no minimum fill. Moved the comic toggle from `F8` to `F10`. Added a door material with a small emission floor, assigned linked doors to only their adjacent light layers (`Control`/`Studio`/`Station`/`Equipment`/`Exterior`) instead of `AllInteriorLayers`, and disabled door-panel shadow casting so thin moving panels do not become black shadow slabs. Added a simple player material with low emission fill so the capsule does not collapse to pure black under overhead shadows. Re-verified `dotnet build KBTV.csproj` with 0 errors (existing 7 warnings) and Godot 4.6.3 `--headless --check-only --quit` with no shader/script load errors; only the known shutdown disconnect errors remain.
- Correction 11: User provided paired screenshots with and without the comic effect. The no-effect frame shows the door/player are lit correctly; the comic frame turns the smooth door light blob into black ink, proving the remaining issue is luma ink misclassifying broad lighting gradients. Changed the shader so macro/broad luma gradients suppress luma ink instead of enabling it (`detail_gate` now trends toward `1.0 - macro_gate`), added a bright low-chroma light-gradient rejection mask, and lowered `LumaEdgeMix` defaults from `0.55` to `0.18` so depth/normal outlines carry the comic silhouette while luma only contributes detail. Re-verified `dotnet build KBTV.csproj` with 0 errors (existing 7 warnings) and Godot 4.6.3 `--headless --check-only --quit` with no shader/script-load errors; only the known shutdown disconnect errors remain.
- Next Steps: User visual check — confirm the screen is no longer globally darkened, silhouettes ink only at real object boundaries, and the look still matches the approved `ad5abda9` style. F9 toggles outlines and `DepthEdgesEnabled=false` isolates the depth pass if outlines still read too heavy.
- Blockers: none (agent cannot view rendered output — visual gate is user-owned).

---

## Previous Session (comic median filter + depth edge scaffolding)

**Branch**: comic-styling
**Task**: Add optional median filtering to the comic post effect to reduce salt-and-pepper shadow speckles.
**Status**: Completed (build green; visual review pending)
- Files Modified: `SESSION_LOG.md`, `shaders/comic_post.gdshader`, `scripts/world3d/ComicPostLayer.cs`, `scripts/world3d/StationLighting3D.cs`, `scripts/world3d/World3D.cs`, `scripts/world3d/ControlRoom3D.cs`, `scripts/world3d/props/ComputerTerminal3D.cs`
- Work Done: Started from user request to prototype median filtering in the comic post effect after shadow speckles/noise became visible under the stronger comic style. Added optional median-luminance outlier clamping that reuses the existing 3x3 Sobel samples, reducing isolated luminance spikes before edge detection and posterization without adding more texture reads. Exposed `MedianFilterEnabled`, `MedianFilterStrength`, and `MedianFilterThreshold` from `ComicPostLayer.cs`. User screenshot showed no obvious improvement; likely cause is that the first pass filtered only luminance used for edge/posterize math while the visible center RGB and later halftone dots still survived. Strengthened the pass so outlier center pixels use the neighbor color nearest local median luminance, lowered the median threshold to `0.035`, raised strength to `1.0`, raised the halftone lower band out of deep shadows (`0.24`), and halved halftone strength (`0.006`) so black shadow regions stay flatter. User reported that this was definitely better but asked for more, possibly a larger kernel; added `MedianFilterRadiusPx` and set it to `3.0` so the 3x3 median samples a wider footprint without the cost of a true 5x5 median, lowered threshold to `0.025`, raised halftone lower band to `0.30`, and lowered halftone strength to `0.003`. User then clarified they want sharp shadow edges without smeared-looking cleanup; retuned away from broad visible median filtering. Median is now subtle again (`strength=0.25`, `threshold=0.08`, `radius=1.5`) and mainly stabilizes Sobel/posterize input, while a new `shadow_flatten_*` pass flattens dark posterized shadow interiors into ink shapes and suppresses halftone there. User reported the flatten pass made the style too dark and speckles remained; current conclusion is the visible speckles are the procedural halftone itself, not median-filterable source noise. Disabled shadow flatten by default (`enabled=false`, `strength=0`) to restore the prior colors, and set `HalftoneStrength=0` by default while leaving controls available. User still saw grainy shadows with colors now acceptable; added a new color-preserving `shadow_smooth_*` pass that averages nearby source color only in dark, low-edge regions, re-applies the current posterized luminance band and saturation, and leaves high-edge silhouettes/object boundaries untouched. User did not see much difference; likely causes were edge rejection treating the grain itself as edges and final `effect_strength` blending 22% raw noisy pixels back over smoothed shadows. Strengthened the pass: larger 9-tap weighted blur, `ShadowSmoothRadiusPx=4.0`, `ShadowSmoothStrength=1.0`, `ShadowSmoothEdgeReject=0.34`, and final blend now uses full comic/smoothed color wherever `shadow_smooth_mask` is stronger than `effect_strength`. User then asked whether the shadow shader/source should be examined; there is no custom shadow shader, but `StationLighting3D` was using huge `SpotRange` values (`100`) and default shadow settings. Reduced room shadow spot range to `13`, capped fluorescent shadow wash range at `12`, set explicit shadow bias/normal-bias/blur (`0.035`/`1.2`/`2.0`), and increased the main viewport positional shadow atlas to `4096`. User asked to try removing shadow blur altogether; set `ShadowBlur=0.0` for an easy visual A/B while keeping the tighter ranges and atlas. User liked the sharp shadows but reported aliasing lines shifting while moving; changed the comic screen sampler from `filter_nearest` to `filter_linear` and added `SobelPrefilterStrength=0.65`, a luma-only cross prefilter used by the Sobel edge detector so final color remains posterized while micro-edge shimmer is reduced. User reported shifting aliasing still happening; added exported `PixelSnapCamera=true` and snap the orthographic camera position to screen-pixel increments after smooth follow interpolation, using camera screen-space right/up axes so it works with the tilted camera. User reported it still happens; screenshot indicates the moving lines are likely luma-Sobel inking fine model/material detail, not temporal camera jitter. Added a macro-edge gate to the comic shader: a wider Sobel keeps large silhouettes inked while detail suppression attenuates fine one-pixel internal hatching that lacks a broader shape edge. User confirmed this improved things but showed remaining hatching on a bright cylinder; tuned stronger by raising `DetailInkSuppression` from `0.80` to `0.92`, `MacroEdgeWidthPx` from `5` to `7`, and `OutlineThreshold` from `0.22` to `0.28`. User still saw some lines and jagged shadows; added brightness-weighted detail suppression (`BrightDetailInkSuppression=0.75`, threshold `0.34`) to reduce hatching on bright/midtone interiors, and set `ShadowBlur=0.5` as a compromise to reduce shadow stair-stepping while keeping the sharper style. Added runtime A/B controls: `F8` still toggles the whole comic post layer, `F9` now toggles outline ink only, `F6` toggles camera pixel snapping, and `F7` cycles shadow blur presets `0.0`, `0.5`, and `1.0` by updating all configured station shadow lights. Existing unrelated Vern animation changes were present and left untouched.
- Verification: Median sort logic was sanity-checked with randomized PowerShell input. `dotnet build KBTV.csproj` passed with existing warnings only. Godot 4.6.3 `--check-only --quit` started and exited without shader compile errors; it printed pre-existing shutdown disconnect errors from live-show/audio cleanup paths. The attempted quadrant subdivision atlas setting did not match the Godot C# API and was removed; atlas size remains set to `4096`.
- Follow-up: User remembered a possible noir/grit pass and reported the current look is much better, with remaining artifacts near the right speaker plus stuttery computer-light flicker. Read-only inspection found no active full-screen noise/grain shader and `halftone_strength=0`; CRT grit exists only inside `TerminalOverlay` scanlines/dust/glass/vignette. Targeted source artifacts instead: smoothed CRT light flicker in `ComputerTerminal3D` by slowing target changes, reducing variation, using exponential easing, raising dip level, and fading dip intensity; disabled shadow casting only on `SpeakerRight` meshes recursively in `ControlRoom3D` to remove the localized corner stipple without changing global lighting.
- Follow-up Verification: `dotnet build KBTV.csproj` passed with existing warnings only. Godot 4.6.3 `--check-only --quit` passed without new errors; it still prints the known shutdown disconnect errors from live-show/audio cleanup paths.
- Depth-Aware Edge Pass: User asked to try a depth-aware edge detector instead of relying only on Sobel/luminance. Preserved the user's tuned `ComicPostLayer` defaults and added depth/normal geometry edge controls so real silhouettes and creases drive the main ink, with luminance Sobel retained as a reduced detail layer. Godot does not support `hint_depth_texture` in `canvas_item` shaders, so the comic pass was converted from a `CanvasLayer`/`ColorRect` to a spatial full-screen `QuadMesh` post pass using the same shader path. The shader now samples `screen_tex`, `depth_tex`, and `normal_roughness_tex`, combines depth/normal ink with reduced luma ink, and outputs through a spatial unshaded fullscreen vertex pass.
- Depth-Aware Verification: First Godot check correctly failed on `hint_depth_texture` in `canvas_item`; after converting to spatial fullscreen quad, `dotnet build KBTV.csproj` passed and Godot 4.6.3 `--check-only --quit` passed without shader errors. It still prints the known shutdown disconnect errors from live-show/audio cleanup paths.
- Next Steps: Visually tune depth/normal edge controls. If outlines become too heavy, lower `DepthEdgeMix` or `NormalEdgeMix`; if interior material/shadow lines remain too busy, lower `LumaEdgeMix` further.
- Blockers: none.

---

## Previous Session (Vern MPFB basic talking loop)

**Branch**: comic-styling
**Task**: Rebuild Vern's `talk_calm_mpfb` as a conservative basic talking loop after proving runtime front/left/right semantics.
**Status**: Completed (basic talking clip generated; runtime side diagnostic + build/tests green; visual review pending)
- Files Modified: `SESSION_LOG.md`, `Tools/modelgen/bake_basic_talk_mpfb.gd` (+`.uid`), `Tools/modelgen/diagnose_vern_runtime_sides.gd` (+`.uid` from Godot import), `Tools/modelgen/preview_mpfb_calm.gd`, `Tools/modelgen/pack_mpfb_calm.py`, `assets/models3d/characters/vern/animations/{talk_calm,idle_breathing,talking_default,smoking,drink_coffee}_mpfb.tres`, `docs/art/VERN_CHARACTER_GUIDELINES.md`, `docs/art/VERN_MPFB_MIGRATION.md`.
- Work Done: User approved moving from orientation/side validation into the basic talking animation pass. Added `bake_basic_talk_mpfb.gd`, a conservative 3s loop that holds the full MPFB seated skeleton, keeps legs/pelvis planted, and limits deliberate motion to spine/neck/head/jaw plus small arm/wrist life. Generated `talk_calm_mpfb.tres` (3.0s, 138 tracks), then ran the existing `fix_vern_mpfb_arm_front.gd` post-pass so the new talking wrists move from raw seated-rest back-side placement to Vern-local front/table side. The post-pass re-saved all MPFB performance clips; its output showed only `talk_calm` had wrists behind before correction (`worst_back_before=0.3173`), while idle/default/smoke/drink were already front-corrected (`0.0000`). Regenerated runtime-chain preview frames and ignored review artifacts `docs/art/model_previews/vern_talk_calm_mpfb_{feed.gif,sheet.png}`. The sheet now shows Vern forward-facing at the table with restrained hands in front rather than broad switched-looking gestures.
- Verification: `diagnose_vern_runtime_sides.gd` passed after the new bake: `talk_calm` mouth/front remained negative Vern-local Z and wrists stayed L/R ordered with front-side Z (`wrist_z L/R` around `-0.31`). `dotnet build KBTV.csproj` passed. `run-tests.ps1 -Godot D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe -Filter VernCharacterIntegrationTests` passed 1/0. `run-tests.ps1 ... -Filter VernAnimationControllerTests` passed 9/0. `preview_mpfb_calm.gd` produced 37 feed + 37 front frames and `pack_mpfb_calm.py` regenerated the review GIF/sheet.
- Next Steps: User visual review of the regenerated `vern_talk_calm_mpfb_sheet.png` / GIF in `docs/art/model_previews/`. If approved, use this as the baseline for adding carefully bounded talking hand variants; if too stiff, add one small single-hand gesture while keeping the side diagnostic as the gate.
- Blockers: none.

---

## Previous Session (Vern MPFB runtime orientation/side contract)

**Branch**: comic-styling
**Task**: Define and validate Vern MPFB runtime body orientation, left/right semantics, and begin a safer basic talking-animation workflow.
**Status**: Completed (orientation/side contract + runtime diagnostic green; basic talking preview regenerated)
- Files Modified: `SESSION_LOG.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`, `docs/art/VERN_MPFB_MIGRATION.md`, `Tools/modelgen/diagnose_vern_runtime_sides.gd`, `Tools/modelgen/preview_mpfb_calm.gd`, `Tools/modelgen/pack_mpfb_calm.py`.
- Work Done: Started from the user's report that Vern's animations still look wrong and left/right arm motion may be switched. Initial read found the scene currently uses `VernStation` yaw-180 plus `Vern.tscn/Model` yaw-180 (net identity), while `VERN_MPFB_MIGRATION.md` still contained stale text saying `Model` must stay unrotated. Added the canonical Vern-local contract to `VERN_CHARACTER_GUIDELINES.md`: `+Y` up, `-Z` front/table/camera, `+X` Vern's right, `-X` Vern's left, `*.L`/`*.R` are semantic body sides. Corrected the migration doc to preserve the current yaw-180 + yaw-180 chain and require contact/preview/diagnostic sync if it changes. Added `diagnose_vern_runtime_sides.gd`, which instantiates the real `Vern.tscn` under the real station yaw and samples shoulders, wrists, head, and mouth marker in Vern-local space across seated/idle/talk/smoke/drink. Updated `preview_mpfb_calm.gd` to render the runtime chain instead of raw GLB space. Fixed `pack_mpfb_calm.py` so contact sheets resize 640x360 frames into 320x180 cells instead of cropping them. Regenerated ignored review artifacts `docs/art/model_previews/vern_talk_calm_mpfb_{feed.gif,sheet.png}` from runtime-chain frames; sheet now shows Vern facing forward at the table.
- Verification: `dotnet build KBTV.csproj` passed. `diagnose_vern_runtime_sides.gd` passed all sampled poses: mouth/front stayed negative Vern-local Z and `wrist.L/upperarm01.L` stayed left of `wrist.R/upperarm01.R` across `seated_rest`, `idle_breathing`, `talk_calm`, `talking_default`, `smoking`, and `drink_coffee`. `run-tests.ps1 -Godot D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe -Filter VernCharacterIntegrationTests` passed 1/0. `run-tests.ps1 ... -Filter VernAnimationControllerTests` passed 9/0. `preview_mpfb_calm.gd` ran and produced 36 feed + 36 front frames.
- Next Steps: For the actual animation quality pass, reduce `talk_calm` to a safer basic talking clip first: keep the proven seated base and side mapping, minimize whole-arm gesture until visually approved, and use jaw/head/shoulder/subtle wrist motion as the baseline.
- Blockers: none.

---

## Previous Session (Vern MPFB arm/front fix)

**Branch**: comic-styling
**Task**: Fix Vern's MPFB talking/smoking/drinking animations after the runtime facing correction. User confirmed the pose now faces the right way, but the authored performance motions appear backwards.
**Status**: Completed (build + Vern tests green; visual review pending)
- Files Modified: `SESSION_LOG.md`, `Tools/modelgen/vern_mpfb_seat.py`, `Tools/modelgen/remap_contacts.gd`, `Tools/modelgen/fix_vern_mpfb_arm_front.gd` (+`.uid`), `Tools/modelgen/source/vern_mpfb_fitted.blend` (+`.blend1`), `assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb`, `assets/models3d/characters/vern/animations/{talk_calm,idle_breathing,talking_default,smoking,drink_coffee}_mpfb.tres`, `assets/models3d/characters/vern/animation_contacts.json`.
- Work Done: Probed current runtime transforms and confirmed the active wrists were behind Vern's mouth/front axis after the face-yaw fix. Made `vern_mpfb_seat.py` idempotent (clears old actions/pose) and corrected the seated limb target side for the final runtime yaw. Re-exported the fitted GLB and forced a Godot import using the full `D:\Software\Godot` 4.6.3 mono install (the temp opencode install lacks `GodotSharpEditor.dll`). Re-baked all MPFB clips, then added `fix_vern_mpfb_arm_front.gd` as a reproducible post-pass that mirrors evaluated MPFB wrist targets to Vern-local front and re-solves the arm chain. Recomputed final smoking/drink grips and updated `animation_contacts.json`; `remap_contacts.gd` now skips the legacy old-rig continuity check when the contract already references MPFB `wrist.*` bones.
- Verification: Numeric probe after the post-pass showed talk/smoke/drink wrists on Vern-local front (`z` negative, with smoke/drink reaching near/in front of the mouth plane). `dotnet build KBTV.csproj` passed. `run-tests.ps1 -Godot D:\Software\Godot\Godot_v4.6.3-stable_mono_win64\Godot_v4.6.3-stable_mono_win64_console.exe -Filter VernAnimationControllerTests` passed 9/0. `run-tests.ps1 -Godot ... -Filter VernCharacterIntegrationTests` passed 1/0.
- Next Steps: User visual check in-editor: confirm Vern still faces correctly and talking/smoking/drinking now move toward the table/camera side instead of behind him.
- Blockers: none.

## Concurrent Session (hallway shadows)

**Branch**: comic-styling
**Task**: Restore readable hallway fluorescent shadows under the stronger comic style.
**Status**: Completed (build green; visual review pending)
- Files Modified: `SESSION_LOG.md`, `scripts/world3d/StationLighting3D.cs`
- Work Done: Read-only trace found the likely issue in `StationLighting3D`: strong non-shadow fluorescent fill lights overlapped weaker shadow-casting wash lights, while the comic post pass posterizes the remaining subtle contrast. Rebalanced fluorescent lighting so fill lights are dimmer (`0.95x`, down from `1.85x`) and shadow wash lights are stronger (`1.35x`, up from `0.8x`). Replaced the previous all-shadow-wash behavior with a stable nearby radius: all fluorescent wash lights within 9.5m of the player cast shadows, with a closest-light fallback outside that radius. This keeps overlap near transitions without returning to the old single-source jump/morph behavior.
- Verification: `dotnet build KBTV.csproj` passed with existing warnings only. Visual hallway check still required because shadow readability is art-directed.
- Next Steps: Run the scene visually and compare hallway shadows with comic enabled/disabled via F8; if shadows are still too flat, add a small shadow-preservation control in `comic_post.gdshader` rather than weakening the whole comic pass.
- Blockers: none.

---

## Previous Session (comic outlines/fog)

**Branch**: comic-styling
**Task**: Strengthen comic outlines substantially and stabilize hallway shadows that changed shape while moving between lights.
**Status**: Completed (build green; visual review pending)
- Files Modified: `SESSION_LOG.md`, `shaders/comic_post.gdshader`, `scripts/world3d/ComicPostLayer.cs`, `scripts/world3d/StationLighting3D.cs`, `scripts/world3d/StudioSmoke3D.cs`, `scripts/world3d/StudioRoom3D.cs`
- Work Done: User reports outlines are still barely visible and hallway shadows morph/move between lights as perspective changes. Root cause found in `StationLighting3D.UpdateFluorescentShadowCaster`: it enables shadows on the fluorescent spot closest to the player, causing the active shadow source to jump between hallway fixtures. Strengthened the comic ink pass substantially: `effect_strength=0.78`, `outline_threshold=0.22`, `outline_bias=0.07`, `outline_width_px=2.0`, `outline_mix=1.0`. Added matching `OutlineWidthPx` export in `ComicPostLayer`. Disabled dynamic fluorescent shadow caster switching so hallway fluorescent lights stay stable instead of swapping the active shadow source. User then reported visible light dots/aliasing; reduced the halftone contribution without changing outlines: `halftone_strength=0.012`, `halftone_size_px=14.0`. User then reported hallway shadows disappeared; fixed by enabling all fluorescent shadow wash lights together when dynamic switching is disabled, restoring shadows without source-jump morphing. User then reported a noticeable "box" around Vern's studio (fog area) that expanded beyond the studio. Fixed in `StudioSmoke3D`: (1) raised the ambient fog volume's base Y from `RoomHalfExtents.Y * 0.58` to `RoomHalfExtents.Y` so the fog box no longer hangs below the floor, and (2) replaced the screen-aligned AABB `DrawRect` veil (grown 12%) with a perspective-accurate convex-hull polygon (`DrawColoredPolygon`) of the projected fog box, with soft smoke blobs clipped to the hull bounds. Removed the old `ProjectFogBounds` AABB helper in favor of `ProjectFogHull`/`ConvexHull`/`ComputeHullBounds` using `Vector2[]`+`List<Vector2>` (added `using System.Collections.Generic`). User then reported the fog shell still trailed short of the room walls; root cause: fog volume used `SmokeRoomHalfExtents=(4.4,1.7,3.4)`, smaller than the actual studio (walls at X±5, Z±4, height 2.3). Matched the fog volume to the real room: `SmokeRoomHalfExtents`/`RoomHalfExtents` → `(5f, 1.15f, 4f)` so the fog box spans floor→wall-top and the projected hull reaches the walls. Build green.
- Verification: `dotnet build KBTV.csproj` passed (existing warnings only). Graphical GoDotTest path was not rerun because the previous session recorded it exiting before a parseable summary even for unrelated suites; visual validation is needed in-editor.
- Next Steps: Run the scene visually and confirm (1) outlines are finally bold enough, (2) hallway lighting no longer has shadow shapes that morph/jump between fluorescents, and (3) the studio fog now fills the room exactly up to the walls with no visible shell. If outlines are too broad/noisy, reduce `OutlineWidthPx` before raising `OutlineThreshold`.
- Blockers: none.

---

## Previous Session (Vern facing fix)

**Branch**: comic-styling
**Task**: Fix Vern's still-backwards runtime facing. User reported the model still facing away AFTER last session's "fix" (which removed the Model yaw and re-solved grips to `MODEL_YAW_DEG=0`). This session proves the facing is a whole-body yaw issue and corrects it WITHOUT disturbing the station/seat chain or prop anchoring.
**Status**: Implemented + verified numerically end-to-end; final user in-engine screenshot gate pending.
- Files Modified: `SESSION_LOG.md`, `Tools/modelgen/remap_contacts.gd` (`MODEL_YAW_DEG := 180.0` + header note that it must match the scene `Model`), `assets/models3d/characters/vern/animation_contacts.json` (new grips for smoking + drink_coffee solved at yaw-180), `scenes/world3d/Vern.tscn` (`Model` gets `rotation = Vector3(0, 3.1415927, 0)` so net yaw = identity). Throwaway numeric probes `Tools/modelgen/probe_facing.gd` + `verify_facing_scene.gd` were written, used, then deleted. Unrelated uncommitted `scripts/world3d/World3D.cs` refactor left untouched.
- Work Done: **Root cause (numeric proof, not pixels):** `probe_facing.gd` on the unrotated `vern_mpfb_fitted.glb` shows facial bones relative to `head`: `eye.L`/`eye.R` z = **+0.0852**, `jaw` z = **+0.0170** → the MPFB rig faces **+Z local**. Under `VernStation` yaw-180 that nets to world **-Z** (back toward mic/world camera/feed camera). Since last session removed the Model yaw AND re-solved grips at yaw-0, the "fix" was the bug. **Fix:** yaw-180 on the `Model` node cancels station yaw → **net identity** → face +Z world; `VernStation`'s transform and SeatAnchor/chair stay untouched. **Grips:** re-ran `remap_contacts.gd` with `MODEL_YAW_DEG=180` — new grips anchor to rest at `d=0.00000`: smoking/wrist.R/cigarette pos `[-0.7809711, -0.4039859, 0.3279209]` quat `[0.98103565, 0.1156334, 0.08778275, -0.12841931]`; drink_coffee/wrist.L/mug pos `[0.2511362, 0.3867952, 0.824887]` quat `[0.77804381, 0.57833713, 0.16645461, -0.18018256]`; both grip blocks updated in the JSON. `mouth_marker.head_local` is yaw-invariant (unchanged); prop rest positions unchanged. Old yaw-0 grips correctly FAIL continuity (factor |d|=0.8861/0.5866) — that signal says the JSON needed updating, and now it is.
- Verification: **End-to-end numeric chain probe** (station yaw-180 + Model yaw-180 + seated_rest, headless): `eye.L/R world z − head world z = +0.0852` → `FACING=FACE ON` (camera at +Z). Net transform is identity as designed. `dotnet build KBTV.csproj` green. Headless GoDotTest: `VernCharacterIntegrationTests` 1/1, `VernAnimationControllerTests` 9/9, full suite **Passed 635 | Failed 10** — the SAME 10 tests (AdManager 4, KBTVTestClass, TranscriptManager, GameStateManager, LoadingScreen) fail on the CLEAN committed tree too (verified by `git stash` + identical headless run) → pre-existing, unrelated to this change (no C# or domain logic touched).
- Blockers: graphical Godot CLI still exits with native code `-1073741571` at display init this session (headless works; suspected stale editor PID 54832 / project contention). Agent cannot view images — visual gate stays user-owned.
- Next Steps: User opens the game in-editor / runs it and confirms Vern now faces the camera (**face, not back**) with cigarette/mug grips intact and seat/feet anchored. If residual head-only weirdness appears, investigate head/neck tracks next. Optional follow-up: promote a permanent numeric facing gate (`verify_facing_scene.gd` style) so future agents never re-flip this.

---

## Previous Session (comic post filter)

**Branch**: develop
**Task**: Add a Borderlands-style full-gameplay comic post filter over the 3D world: screen-space ink outlines, posterized cel bands, halftone dots, and runtime toggle.
**Status**: Completed (stronger comic tuning/build green; visual re-review pending)
- Files Modified: `SESSION_LOG.md`, `shaders/comic_post.gdshader` (+`.uid`), `scripts/world3d/ComicPostLayer.cs` (+`.uid`), `scripts/world3d/World3D.cs`, `scripts/world3d/StudioSmoke3D.cs`
- Work Done: Chosen scope = full gameplay visual filter. Grounded implementation against current runtime: `Game3D.tscn` renders `World3D` with `WorldCamera`; `World3D.tscn` has `StatusLayer` at CanvasLayer layer 20; `TerminalOverlay.cs` provides the existing ShaderMaterial/ColorRect pattern. Added a full-screen `hint_screen_texture` comic shader with Sobel ink outlines, posterized luminance bands, saturation boost, and mid-tone halftone dots. Added `ComicPostLayer` at CanvasLayer layer `-10` so it filters the 3D world before UI/HUD layers, with exported tuning knobs and F8 runtime toggle. Wired it into `World3D._Ready()`. User liked direction but reported the look was too extreme and the top had a strange circular/oval pattern; root cause identified as strong UV-space halftone stretching across the 16:9 viewport. Toned defaults down: added blend strength, posterize steps 6, saturation 1.12, softer outlines, halftone strength 0.04, and pixel-space `halftone_size_px=8`. Added post-comic smoke/fog rendering: `StudioSmoke3D` now creates `StudioSmokePostLayer` at CanvasLayer `-5`; hides the original 3D `FogVolume` when `RenderAmbientFogAfterComicPost=true`; draws a projected ambient fog veil/blob layer from the studio fog volume bounds; hides the 3D wisp meshes when `RenderPuffsAfterComicPost=true`; projects active wisp world positions with the current `Camera3D`; and draws the smoke texture in screen space after the comic pass. With fog/smoke isolated after the effect, raised comic drama defaults: `effect_strength=0.68`, `posterize_steps=5`, `saturation=1.22`, `outline_threshold=0.45`, `outline_bias=0.08`, `outline_mix=0.95`; halftone remains subtle at `0.04`.
- Verification: `dotnet build KBTV.csproj` passed (existing warnings only). `run-tests.ps1 -Filter SoundboardPhysicalLayoutTests` had passed 11/0 before the smoke/fog pass, but after this pass the Godot test command exits before GoDotTest prints a parseable summary (`Could not parse test summary from engine output`) even for unrelated filtered suites. Minimal `Godot --path ... --quit --verbose` starts the project, so this is recorded as a test harness/startup issue requiring follow-up; no C# compile errors.
- Next Steps: Run the scene visually in Godot and check the stronger edges/effect against the now-isolated fog/smoke overlay. If too strong, reduce `EffectStrength` first; if edges are too noisy, raise `OutlineThreshold` before reducing `OutlineMix`.
- Blockers: none.

---

## Previous Session (completed - MPFB Vern migration/runtime swap)

**Branch**: develop
**Task**: Migration Phases 3-4 — remap MPFB contact anchors (`animation_contacts.json` hand→wrist) and swap `Vern.tscn` to `vern_mpfb_fitted.glb` (+yaw/facing), per `docs/art/VERN_MPFB_MIGRATION.md`. Probe sunk: fitted GLB ships **only `seated_rest`**; the 4 other controller clips (`idle_breathing`, `talking_default`, `smoking`, `drink_coffee`) exist only on the old `VernRig` (53 bones, `VernRig/Skeleton3D` paths) → **must be rebased onto MPFB before the swap**. Baked `talk_calm_mpfb.tres` track prefix `Vern_MPFB_StandardRig/Skeleton3D:<bone>` matches the fitted GLB skeleton path exactly.
**Status**: Completed (**Phase 3-4 implementation/test green; final user visual review pending on regenerated previews**)
- Seat-fix diagnostics this session: `VernSeatDiagTests.cs` (temp integration diagnostic, now deleted) mirrored the real test. Baked clips PIN pelvis.L/R via rebake (added to the seat pin branch, dropped `pelvis` from AUTH_VERN_TO_MB map → maps 43/47; header comment updated). **Root cause found + fixed**: root's `seat_local` fell back to `Quaternion(get_bone_rest(root).basis)` because `seated_rest` has NO root ROT track (only POS). But the fitted root rest basis has tiny scale/shear (`get_scale()=(1,1,1.000004)`; quat→basis reconstruction differs from raw rest rows by ~1.7e-6) which Godot's `Transform3D.IsEqualApprox` rejects. **Fix: emit NO root ROT track at all** — source Vern clips and seated_rest both lack root ROT, so root always holds its exact rest basis on both sides (diag: root fixed-eq=True, maxBasisDelta=0.000E+000; pelvis.L/R + foot.L/R fixed-eq=True with deltas ≤1.2e-7). Re-baked all 5 MPFB clips (`talk_calm`, `idle_breathing`, `talking_default`, `smoking`, `drink_coffee`) to 138 tracks each: 136 bone ROT tracks + root POS + jaw SCALE, MISSING_COVERAGE=0. **VernCharacterIntegrationTests PASSES (1/1)** and VernAnimationControllerTests passes (9/9). Full suite 636/10 — the 10 failures are pre-existing and unrelated (LoadingScreen Setup, GameStateManager, TranscriptManager, AdManager, Arc audio integrity); none touch the Vern clips.
- Phase 3 contacts remap (done this session): probe `Tools/modelgen/remap_contacts.gd` (new) samples the OLD rig (authoring continuity: props sat at rest at pickup — smoking |d|=0.0018, drink_coffee |d|=0.0095 OK) and the FITTED rig under a 180°-yaw Model node playing the `*_mpfb.tres` clips at t=pickup(1.1s), computing `grip = (K * bonePose(wrist)).affine_inverse() * rest` (K = GlobalInverse*skeletonGlobal = skeleton→Vern-local incl. yaw) and verifying `K*bonePose*grip == rest` to <1e-4. Results written to `animation_contacts.json`: smoking hand_bone→`wrist.R` grip pos(-0.8782124,-0.2781071,0.3739358) quat(0.95700324,0.16118811,0.11212799,-0.21351779); drink_coffee hand_bone→`wrist.L` grip pos(0.1228644,0.4799280,0.8895093) quat(0.79166245,0.56074047,0.08029366,-0.22889383); `mouth_marker.head_local` position →(0,0.0310001,0.127) (MPFB head frame). `samples` arrays left as legacy authoring-reference (runtime reads none). JSON stays valid JSON.
- Rebase (`Tools/modelgen/rebake_vern_clips.gd`, new, generalized from `bake_talk_calm_mpfb.gd`): probed old-clip inventory first (`Tools/modelgen/probe_old_clips.gd`, new, temp) — all 5 old clips animate the SAME 47 ROT bones (pelvis/spine/chest/neck/head; upper_arm/forearm/hand.L/R; fingers {thumb,index,middle,ring,little}_{1..3}.L/R; thigh/shin/foot.L/R), NO root track; SCALE on chest/eyelid.L/R/jaw/upper_arm/forearm/hand.L/R; POS tracks encode seated placement (old rest is standing → dropped in rebake; MPFB rest already seated). Map: pelvis→pelvis.L/R (split), spine→spine01, neck→neck01, head→head, upper_arm→upperarm01, forearm→lowerarm01, hand→wrist, thigh→upperleg01+02, shin→lowerleg01+02, foot→foot, fingers thumb→finger1…little→finger5 (segments 1-3) via Overwrite-Axis rest-fixer rewrite; root constant POS = fitted rest root origin; jaw ROT holds seat + jaw SCALE carries the speech pulse (type-3 track); everything unmapped HOLD seat rest; gain 1.0; final-key loop clamp.
- Baked + verified this session (`MISSING_COVERAGE=0`, 139 tracks = 137 bones + root POS + jaw SCALE): `idle_breathing_mpfb.tres` (len 4.0, 96f), `talking_default_mpfb.tres` (8.0, 192f), `smoking_mpfb.tres` (5.5, 132f), `drink_coffee_mpfb.tres` (5.5, 132f). Map vern→mb=46, rw=51. FITS the runtime way the clips will be referenced: fitted glb ships ONLY `seated_rest`; controller uses `idle_breathing`/`talking_default`(fallback)/`smoking`/`drink_coffee`, and VernCharacter3D will inject all of them.
- Validation (`Tools/modelgen/validate_rebake.gd`, new): fixed `add_animation` lives on AnimationLibrary not AnimationPlayer; `!is_inside_tree()` → add children to `root` + `await process_frame` after play/seek. Result: poses animate correctly; new-vs-old body offsets are the expected height deltas (head +0.31, pelvis +0.22, wrist +0.19..0.23) — body is taller, motion carries; relative posture matches (hand-to-pelvis 0.078 new vs 0.107 old). MOUTH_NEW candidate `(0, 0.031, +0.127)` reproduces old mouth world placement (newCand y=1.556 == old 1.246 + 0.31).
- Phase 3 corrected grip semantics (supersedes the OLD formula `wrist_local_grip = G_newWrist^-1*G_oldHand*old_grip`): runtime does `propLocal = GlobalTransform.AffineInverse() * skeleton.GlobalTransform * bonePose * hand_local_grip` and drives prop to contract `rest` OUTSIDE the held window (VernPerformanceProps.cs:55-69) — so the grip must anchor the prop to its contract rest AT pickup on the new rig, not preserve the old hand's world pos (that would float the cup ~0.2 m below the new palm). New recipe: with `K = skeleglobal→vern-local` (incl. Phase 4 yaw-180 at Model), `grip = (K * bonePose(wrist @ pickup_mpfb))^-1 * rest`. Old-rig authoring continuity (grip built as `pickup_hand^-1 * REST` in vern_animation.py:141-142) makes the OLD anchor exact by construction — sanity-check both.
- Contacts source read in full (runtime-consumed only): `props{coffee_mug rest(0.34,0.73,-0.43)|ashtray(-0.36,0.73,-0.4475)|cigarette(-0.36,0.755,-0.405)|vern_tray_table}`, `actions{seated_rest|idle_breathing|talking_default: no props; smoking: prop=cigarette, hand_bone=hand.R, pickup 1.1, release 4.6, exhale 3.5, grip pos(0.0131967,0.075618,-0.0166061) quat(0.76974881,0.08244407,-0.03308192,0.63213557); drink_coffee: prop=coffee_mug, hand_bone=hand.L, pickup 1.1, release 4.6, grip pos(0.0772248,0.0990965,-0.0810322) quat(0.48599792,-0.60259128,0.47037989,0.42359495)}`, `mouth_marker` head_local pos(0,0.0310001,-0.127) quat≈identity, seated_vern_local (0,1.246,-0.107). The `samples` arrays (~3900 lines) are authoring reference only — runtime reads none of them.
- MPFB wrist bone names confirmed: `wrist.L` / `wrist.R` (rebake map `"hand."+side → "wrist."+side`).
- Next Steps: (4) PHASE 4 (working now): swap `Vern.tscn` Model → `vern_mpfb_fitted.glb` (path change) + **Model node yaw-180** (`rotation = Vector3(0, PI, 0)` — MPFB faces +Z, old faces -Z; fitted grips were solved under a yaw-180 Model node in the phase-3 probe, so the scene must replicate that exact chain or props will misplace); `LookTarget` y 1.26 → **1.57** (new head ~+0.31 higher); `VernCharacter3D` must inject **all 5 clips** into the empty-name library (`talk_calm`, `idle_breathing`, `talking_default`, `smoking`, `drink_coffee` — fitted glb ships ONLY `seated_rest`), updating `TalkCalmPath`→`talk_calm_mpfb.tres`; controller/`TalkCalmPath` stays; fix `VernCharacterIntegrationTests` if `talk_calm`-inject asserts or bone names break (MPFB has no pelvis single bone — test checks `root/pelvis/foot.L/foot.R`; `pelvis.L/R` are split; update test if `pelvis` name lookup breaks), dotnet build + run-tests.ps1; then preview via `preview_mpfb_calm.gd`/`pack_mpfb_calm.py` on swapped rig + user visual gate.
- Files Modified (this session): `SESSION_LOG.md`, `docs/art/VERN_MPFB_MIGRATION.md`, `Tools/modelgen/{bake_talk_calm_mpfb.gd,rebake_vern_clips.gd}` (root ROT dropped; root POS only; +seat pin), `assets/models3d/characters/vern/animations/{talk_calm,idle_breathing,talking_default,smoking,drink_coffee}_mpfb.tres` (re-baked 138 tracks, no root ROT, MISSING_COVERAGE=0), regenerated `docs/art/model_previews/vern_talk_calm_mpfb_{feed.gif,sheet.png}`. Deleted temp `tests/integration/VernSeatDiagTests.cs` and external temp `probe_seatroot.gd`.
- Final verification: `dotnet build KBTV.csproj` green (warnings only); `run-tests.ps1 -Filter VernCharacterIntegrationTests` Passed 1/0; `run-tests.ps1 -Filter VernAnimationControllerTests` Passed 9/0. Preview capture with Godot 4.6.3 regenerated 36 feed + 36 front frames and packed the GIF/sheet; note the sheet camera is a tight torso/arm crop for motion/hand review, not a full seated-chair framing.
- Blockers: none (agent cannot view images — visual gates stay user-owned). Note: JSON remap + model swap must land together (tests `Require(hand=wrist>=0)`).
- Preview rendered + artifacts committed-logged: `vern_talk_calm_mpfb_feed.gif` + `vern_talk_calm_mpfb_sheet.png` in `docs/art/model_previews/`. User approved. New tools: `Tools/modelgen/preview_mpfb_calm.gd` (offscreen SubViewport renderer, graphical run) + `Tools/modelgen/pack_mpfb_calm.py` (PIL GIF/sheet packer). Fixed missing-await bug (quit() was killing the capture loop after frame 0).
- Bake executed + validated this session: `talk_calm_mpfb.tres` created — LENGTH=2.933 FRAMES=71 FPS=24 TRACKS=139 BONES=137 **MISSING_COVERAGE=0**; pinned=15 delta=4 auth=34 wrist=2 held=81; def_mpfb joined=50, rewrites def=50 auth=34, body samplers=48, vern authored samplers=34, **jaw scale sampler=true** (jaw speech is a SCALE pulse in talking_default — POS+SCALE tracks, no ROT; added as a type-3 track, POS skipped since Vern's jaw POS is absolute rest-origin and would misplace MPFB's jaw joint). Migration-doc finger table fixed (thumb→finger1-* … little→finger5-* per side). `_seat_cross_check` fixed to shortest-arc math — the earlier "360°" rows were the q/-q double cover (same rotation); real worst dev is on limbs because `seated_rest` t=0 records non-seated limb quats, but **world-geometry probe proves skeleton rest IS seated** (knee y=0.761 vs hip 0.823 = thigh horizontal, shin to ankle 0.066, toe 0.007 on floor) → pin-to-rest is correct. Output track spot-check: wrist.L/R, all 30 finger tracks, jaw ROT (hold) + jaw SCALE present. Temp probe scripts (`probe_mpfb_tracks/jaw/legs.gd`) deleted.
- User decisions (question tool): (1) scope = **talk_calm only**; (2) finger mapping = **finger1=thumb … finger5=little** (fix migration doc table); (3) deliverable = **new script + new output** (`bake_talk_calm_mpfb.gd` → `assets/models3d/characters/vern/animations/talk_calm_mpfb.tres`, original bake + `talk_calm.tres` kept intact); (4) bake-approach question left **unanswered** → proceed with the grounded default (S = seated_rest action locals, root LOCATION track at drop −0.2927, PINNED root/pelvis.L/R/full legs, DEF→MPFB body via profile join, authored-arms/fingers/jaw cross-skeleton rewrite VernRig→MPFB, drop eyelid/grip, verify wrist-flip/finger-curl on renders).
- Grounding confirmed this session: `bake_talk_calm.gd` header (REF_GLB `Animation Library[Standard]/Godot/AnimationLibrary_Godot_Standard.glb`, REF_CLIP `Sitting_Talking`, VERN_GLB `vern.glb`, VERN_CLIP `talking_default`, BM_REF `bone_map_ref.tres` / BM_VERN `bone_map_vern.tres`, FPS 24, SKEL_PATH `VernRig/Skeleton3D`, OUT `animations/talk_calm.tres`). MPFB rig facts from `mpfb_rig_hierarchy.json` + migration doc: split `pelvis.L/R`, legs `upperleg01/02+lowerleg01/02+foot+toe1-1.L/R`, spine `spine05→spine04→spine03→spine02→spine01` (+breast.L/R), arms `clavicle→shoulder01→upperarm01/02→lowerarm01/02→wrist.L/R`, fingers `finger1-1…finger5-3` + `metacarpal1-5.L/R`, `neck01/02/03`, `head`, `jaw`; **no grip/eyelid/hand bone**. Runtime: `VernCharacter3D.InjectTalkCalm` adds the .tres to the player's root (empty-name) library; `VernAnimationController` suffix-matches clip names; `Play` replaces the current clip (no blending) → the MPFB clip must embed the full seated body. Finger table in `VERN_MPFB_MIGRATION.md` is wrong (order) → fix to finger1=thumb…finger5=little.
- Work Done: grounding reads (bake_talk_calm.gd header, migration doc, mpfb_rig_hierarchy.json, both bone maps, VernCharacter3D/Controller, animation_contacts.json, Tools listing); finger-numbering analysis (finger1-1 parents wrist.L, finger2-5 sit on metacarpal1-4, thumb angled medially → finger1=thumb; migration doc table order wrong).
- Next Steps: **Phase 3 (MPFB contact anchors)** — anchor locations/detach per `docs/art/VERN_CHARACTER_GUIDELINES.md` (LineOfSightAnchor, WorldAimAnchor, eye_target, SpeaksAnchor, drink/food/coffee anchors, smoking) applied to MPFB bone-dict via `mpfb_rig_hierarchy.json`; then **Phase 4** (`Vern.tscn` swap to `vern_mpfb_fitted.glb`, yaw/facing −Z→+Z fix, clip re-point to talk_calm_mpfb, gates from VERN_3D_MODEL_BRIEF).
- Files Modified: `SESSION_LOG.md`, `bake_talk_calm_mpfb.gd`, `talk_calm_mpfb.tres`, `VERN_MPFB_MIGRATION.md` (finger table), `docs/art/model_previews/vern_talk_calm_mpfb_*.{gif,png}` + `preview_mpfb_calm.gd` + `pack_mpfb_calm.py` (new).
- Blockers: none.
- Grounding resolved this session: (a) Leg direction — production Vern sits toward Blender **+Y** (knees/feet +Y, from `vern_rig.py` seated bone spans), MPFB face is Blender **-Y** (report) → MPFB knees must go **-Y**, which Phase 4's yaw/facing swap already plans for. (b) Hip height 0.53 confirmed from `scenes/world3d/World3D.tscn` `VernStation/SeatAnchor` (0, 0.53, -0.005). (c) Wrist height — the studio has NO armrest chair; Vern's hands rest on the runtime **tray table** (surface_y 0.73, contract supports; production seated wrist z 0.742) → wrists target z ~0.73, NOT the office-chair armrest 0.68 from the earlier plan. (d) FK technique — production (`vern_rig.py` `bind_and_animate`, `vern_animation.py` `solve_arm`) assigns `pose_bone.matrix` directly + `view_layer.update()`, then keyframes basis channels at frames 1/25 @24fps; proven in this Blender 5.2, so `vern_mpfb_seat.py` mirrors it exactly. (e) Real rig numbers (probing source blend, NOT the Plan assumptions): `root` edit head (0, 0.0628, 0.8432) → set pose via `pose_bone.matrix.translation.z += drop` (bone-local `.location` applies along the bone's oriented local axis and skewed the whole chain — first failure); hip joint = `upperleg01.L` head 0.8227 → drop 0.2927; shoulder = `upperarm01.L` head after drop ≈ (0.1788, -0.0079, 1.0365), a=0.2459, b=0.2296, d≈0.442 → reachable; elbow resolves ≈ (0.293, -0.112, 0.845). (f) Blender bones point along local **+Y**, so `aim()` must swing `pb.matrix.to_3x3() @ Vector((0,1,0))` — using (0,0,1) swung the roll axis and flung the knees skyward (second failure).
- Work Done: Wrote/ran `Tools/modelgen/vern_mpfb_seat.py` and fixed 3 bugs en route (PoseBone-as-key dict comp; root move via world matrix; +Y aim axis) plus the wrist-target sign (`FRONT*WRIST_FORWARD`, third failure). Final run: **VERN_MPFB_SEAT** summary hip_z 0.53, knee_z 0.46715, ankle_z 0.05, toe_tip_z 0.01243, wrists (±0.28, -0.31, 0.73), `animations_in_glb ["seated_rest"]` — all asserts passed; GLB re-exported with `export_animations=True` + `export_rest_position_armature=True`; 3 seated renders + `seated` dict appended to the report. **Validator adaptation**: re-export broke `validate_mpfb_fitted.py` "neutral bind match" — the glTF importer bakes the animated ROOT node's first-frame translation into its base TRS (imported rig's default pose is seated, root basis location (0,-0.0557,0.2873) even after `animation_data_clear()`); fixed by toggling `rig.data.pose_position = 'REST'` for the evaluated-vs-bind loop then back to `'POSE'`. Validator now **passes**: 51 meshes, 137 bones, height 1.70701, standing bind intact.
- Next Steps: **USER reviews `docs/art/model_previews/vern_mpfb_seated_{front,side,threequarter}.png`** (seat fit / hand tray placement / foot floor contact). On approve → Phase 2 (rebake `talk_calm` via `bone_map_mpfb.tres` profile join). On tweak → adjust WRIST_Z/X/forward, hip, foot flatness in `vern_mpfb_seat.py` and re-run.
- Files Modified: `SESSION_LOG.md`, `Tools/modelgen/vern_mpfb_seat.py` (new), `Tools/modelgen/validate_mpfb_fitted.py` (pose_position REST toggle for bind check), `assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb` (re-exported, +seated_rest), `Tools/modelgen/source/vern_mpfb_fitted.blend` (re-saved), `docs/art/model_previews/vern_mpfb_fitted.json` (+seated dict), `docs/art/model_previews/vern_mpfb_seated_{front,side,threequarter}.png` (new), `docs/art/model_previews/vern_mpfb_fitted_validation.json` (regenerated).
- Blockers: none (seated render review by user is the gate before Phase 2).

---

## Previous Session (MPFB hair validation + migration plan written)

**Branch**: develop
**Task**: Finish the MPFB hair validation and write the MPFB→production migration plan (seated + animation). Grounding research for the migration is complete.
**Status**: Completed (hair validated; plan written; migration started this session)
- Work Done: Hair rebuild already exported (`vern_mpfb_fitted.glb` now contains `Swept hair clump 00–09`, `Hair edge card 00–09`, `Fitted swept scalp`; report: clumps 10, cards 10, card material "Vern masked hair tips", alpha_mode MASK, 8670 hair tris). Grounded migration unknowns: read both BoneMaps (`bone_map_ref.tres` profile→DEF-*, `bone_map_vern.tres` profile→VernRig names), the proven profile-join bake (`Tools/modelgen/bake_talk_calm.gd` → tracks `VernRig/Skeleton3D:<bone>`), `vern_rig.py` (VernRig hierarchy: root>pelvis>spine>chest>neck>head{eyelid.L/R,jaw}, upper_arm>forearm>hand{grip,fingers}.L/R, thigh>shin>foot), `vern_animation.py` (`build_actions`, `solve_arm`, `prop_keys`; 244 lines), and `vern_mpfb_fitted.py` header (MPFB standard rig, ~137-face-bone etc.). Probed both GLBs directly: MPFB = `Vern_MPFB_StandardRig` armature, 137 joints (split `pelvis.L/R`, `upperleg01/02.L/R`, `spine05..spine01`, `upperarm01/02`, `lowerarm01/02`, `wrist`, finger `1-1..5-3` + metacarpals, `neck01/02/03`, jaw, facial bones orbicularis/oculi/temporalis/oris/levator/tongue; NO grip/eyelid/hand bone); production `vern.glb` = 53-joint `VernRig` hierarchy confirmed. MPFB GLB faces Godot **+Z** (report `front_axis`), production faces Godot **-Z** → placement yaw/facing rework needed. MPFB height 1.70701m, bound max Y 1.713 (standing).
- Files Modified: `SESSION_LOG.md`, `docs/art/VERN_MPFB_MIGRATION.md` (new), `docs/art/3D_ASSET_WORKFLOW.md` (hair paragraph), `docs/art/VERN_3D_MODEL_BRIEF.md` (cross-link), `AGENTS.md` (docs table row).
- Blockers: None.

## Previous Session (silhouette, shoes and flattened hair locks)

**Branch**: develop
**Task**: Correct Vern's long-legged silhouette, flat fabrics, oversized loafers and helmet-like hair following approved appearance direction.
**Status**: Completed (revision generated; user appearance review pending)
- Work Done: Shortened leg region 10% with a matched mesh/edit-bone rest-space remap and modest torso extension, instead of relying on unverified MPFB target weights. Height is 1.70701m; thigh 0.34466m, shin 0.41698m (~44.6% of total height combined). Replaced egg-shaped shoes with 26cm shaped low-profile loafers, narrow heels and matching thin soles/welts. Added subtle trouser breaks and levelled hems. Changed fabrics from repeat 25 to 16/m, increased color contrast and reduced overly strong close-up normals. Added 22 flattened swept locks sharing the cap's texture projection; iterated on scalp penetration and floating ribbon edges through actual image inspection.
- Verification: Existing Blender round-trip baseline passed before edits. Final generator and extended validator passed: normalized weights, facing, body cleanup, height and leg measurements, loafer size, rigid head motion, neutral evaluated/bind geometry agreement, embedded PBR maps. Inspected generated front/side/back, shoes, swatches and export portrait/front. `git diff --check` passed (line-ending warnings only). No C# runtime changes; Godot runtime/seated deformation remain unverified.
- Next Steps: User reviews new front, portrait and shoes renders; hair is still stylized sculpted locks and needs an appearance verdict before seated/chair/animation work.
- Files Modified: `SESSION_LOG.md`, `docs/art/3D_ASSET_WORKFLOW.md`, `Tools/modelgen/{vern_mpfb_fitted.py,vern_mpfb_wardrobe.py,vern_mpfb_surfaces.py,validate_mpfb_fitted.py}`, new `vern_mpfb_proportions.py`, `vern_mpfb_shoes.py`, `vern_mpfb_hair.py`, fitted blend/backup/GLB, ignored previews/maps.
- Notes: Image reads succeed; earlier claims of image-input failure were incorrect. Pre-existing untracked texture copies in `Tools/modelgen/source/textures/` and beside the GLB were present at session start and were retained.
- Blockers: None.

## Previous Session (collar and fabric refinement)

**Branch**: develop
**Task**: Refine the fitted Vern's neck/collar and strange hair surface; add restrained fabric texture to clothing following user feedback.
**Status**: Completed (refined asset and review renders generated)
- Work Done: Replaced the straight oversized collar with a closed rounded band sampled against actual neck geometry, dipped below the chin and weighted from body vertices. Smoothed/subdivided the lumpy scalp, compensated hairline shrinkage, and replaced faceted temple-color patches with a soft gradient and restrained swept-strand map. Added 512px color/normal/roughness-metallic maps for wool knit, ribbed collar, and twill trousers. Physical-area UV normalization keeps yarn scale consistent; headphone plastic uses its own plain material. Expanded the lower sweater slightly to eliminate trouser-waistband overlap exposed by the new textures.
- Verification: Rebuilt the blend/GLB and inspected front, side, back, portrait, and new fabric close-up. `validate_mpfb_fitted.py` passed normalized-weight, facing, covered-skin, bounds, head-motion and embedded-texture checks (four textured PBR materials). Rendered and inspected the re-imported GLB portrait; material appearance matches the source. `git diff --check` passed with line-ending warnings only. No C# runtime changes or Godot runtime verification in this pass.
- Next Steps: Review `docs/art/model_previews/vern_mpfb_fitted_portrait.png`, `_fabric.png`, and `_export_portrait.png`; seated/animation work remains a later phase.
- Files Modified: `SESSION_LOG.md`, `Tools/modelgen/{vern_mpfb_fitted.py,vern_mpfb_wardrobe.py,validate_mpfb_fitted.py}`, new `vern_mpfb_collar.py` and `vern_mpfb_surfaces.py`, fitted blend/backup/GLB, 12 texture PNGs under `assets/models3d/characters/vern_mpfb/textures/`, fitted reports and review PNGs, `docs/art/3D_ASSET_WORKFLOW.md`.
- Related Docs: `docs/art/3D_ASSET_WORKFLOW.md`, `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`.
- Blockers: None.

---

## Previous Session (static MPFB rebuild; user confirms improvement)

**Branch**: develop
**Task**: Rebuild the rejected fitted MPFB Vern using direct rendered inspection: correct anatomy targets, orientation, garment coverage, and face accessories.
**Status**: Completed (static revision generated and visually inspected; user art approval pending)
- Files Modified: `Tools/modelgen/vern_mpfb_fitted.py`, `vern_mpfb_wardrobe.py`, `vern_mpfb_body_diagnostic.py`, `validate_mpfb_fitted.py`; fitted GLB, diagnostic/fitted source blends and Blender backups, diagnostic/fitted PNGs and reports under `docs/art/model_previews/`; `docs/art/3D_ASSET_WORKFLOW.md`, `VERN_3D_MODEL_BRIEF.md`, `VERN_CHARACTER_GUIDELINES.md`, `SESSION_LOG.md`.
- Work Done: Replaced disconnected primitives with continuous surface-derived pullover/trousers retaining MPFB weights. Fitted sleeve ends using forearm planes, gave trousers leg clearance and shoes flat soles, removed hidden skin (head/hands retained). Rebuilt scalp/hairline, gray temples, eyes, eyebrows, closed aviator frames, tapered mustache and arched headphones against head/eye landmarks. Corrected camera directions and full-body framing. Iterated by opening the generated images with the Read tool; the previous claim that the agent cannot inspect images was incorrect.
- Root Causes Corrected: Face is **Blender -Y / Godot +Z**, not +Y/-Z; centroid-extrema direction inference was wrong. MPFB default mixed male/female targets, including breast targets, were being added underneath the selected male targets; both generators now disable defaults before using the two explicit male macros. Diagnostic verification checks exact active target names rather than substring `male` (which also matches `female`). Old `triangles` metric was polygon count; new report counts triangles.
- Verification: Diagnostic generation, fitted generation, and `validate_mpfb_fitted.py` all passed under Blender 5.2.1. Final asset: 29 skinned meshes, 137 bones, 43,148 triangles, ~1.753m with headband. Round-trip checks cover normalized weights, correct facing via the actual diagnostic function, dimensions, covered-skin removal, and rigid head-accessory motion. Final front/side/back/portrait renders reviewed. `git diff --check` passed (line-ending warnings only). Runtime/C# tests not run for this asset/tooling-only revision; Godot runtime and seated deformation are not yet verified.
- Next Steps: User reviews `docs/art/model_previews/vern_mpfb_fitted_{portrait,front,side,back}.png`. After appearance approval, proceed to seated/chair fit, contacts, and animation migration.
- Related Docs: `docs/art/3D_ASSET_WORKFLOW.md`, `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/CHARACTER_GUIDELINES.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`.
- Blockers: None for static revision. Prior Godot import failed loading `GodotSharpEditor`; a missing project build was not established as its cause. Animation rebake reference availability still needs checking before the later migration.

---

## Previous Session (fitted prototype rejected; claims below superseded by visual rebuild)

**Branch**: develop
**Task**: Replace the rejected MPFB clothed prototype with properly fitted, skinned clothing and Vern identity accessories on the male MPFB base, export a cleaned rigged GLB, and hand to the user for visual review. This is Phase 2 of the MPFB Vern migration (diagnostic base was approved).
**Status**: Completed (script + export + validation; **blocked on user visual review of renders**)
- Files Modified: `SESSION_LOG.md`, `Tools/modelgen/vern_mpfb_fitted.py` (new), `Tools/modelgen/source/vern_mpfb_fitted.blend` (new), `assets/models3d/characters/vern_mpfb/vern_mpfb_fitted.glb` (new), `docs/art/model_previews/vern_mpfb_fitted.json` (new), `docs/art/model_previews/vern_mpfb_fitted_{front,side,back,portrait}.png` (new, regenerated), `Tools/modelgen/source/vern_mpfb_body_diagnostic.blend`.
- Work Done: `vern_mpfb_fitted.py` measures the evaluated male body (with the `body`-group MASK giving the 13,380-vert naked surface) cross-sectionally via `ring()`/`limb_radius()` around bone polylines, builds skinned garments (charcoal turtleneck sweater with ribbed hem/cuffs, dark trousers, leather shoes) that inherit each vertex's weights from its nearest body vertex via a kdtree, and adds Vern identity accessories (swept dark hair with gray temple streaks, mustache, aviator glasses, over-ear headphones). Male shape keys are baked into the basis. Fixed several generator bugs (ring→5-tuple for loft, radius-list length for tube, `segments=` param for ellipsoid, `group_weight()` runtime-error guard, active-object guard in `clean_body`, removed a stale shoulder-cap block). **The current session's fix**: `clean_body()` didn't actually run — `MeshVertex.select` doesn't sync to the edit bmesh, so 5,778 helper verts still shipped; now selects via `bmesh.from_edit_mesh` + `bmesh.update_edit_mesh` and re-exported. Report now: `body_verts` 13380 (was 19158), height 1.71506 m (was 1.76094 incl. helpers), triangles 15524 (was 20632), bounds min `[-0.55252, -0.34407, -0.00697]` max `[0.55252, 0.21502, 1.70809]`, meshes 30, armatures 1, skinned_garments 29, shoulder 0.36226, hip 0.21917. GLB re-validated via Blender re-import: 31 meshes (30 skinned + 1 Blender-import Icosphere widget, expected per `vern_export.py:109`), 1 armature / 137 bones, `head` bone present. Godot `--import` smoke test cannot run headless (crashes: .NET plugin unbuilt in 4.6.3 console mode — expected, needs a `dotnet build` first).
- Next Steps: **USER must eyeball `docs/art/model_previews/vern_mpfb_fitted_{front,side,back,portrait}.png`** (the model cannot read images) and pass/fail fit + identity. On approval: later phases — seated pose/chair, contact anchors, `talk_calm` retarget/migration into production. On fail: iterate garment ease/z-caps in `vern_mpfb_fitted.py`.
- Related Docs: `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`, `docs/art/CHARACTER_REFERENCE_LIBRARY.md`.
- Verification: `& "C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --python-exit-code 1 --python "D:\Dev\Games\kbtv\Tools\modelgen\vern_mpfb_fitted.py"` passed, exported GLB + 4 previews + report; Blender GLB re-import validation passed; `dotnet build` / `run-tests.ps1` not yet run (no repo code changed this session).
- Blockers: user visual review (model cannot view images). Unchanged: no local `docs/references/` bundle, so any future rebake requiring `talk_calm` stays deferred.

---

## Previous Session (completed - male MPFB body/orientation diagnostic approved)

**Branch**: develop
**Task**: Replace the rejected MPFB clothed prototype with a male MPFB body/orientation diagnostic. Confirm the generated base is actually male, establish front/back/left/right axes and body landmarks, and defer clothing/accessory fitting until the base orientation is proven.
**Status**: Completed (male body diagnostic generated; clothing still deferred)
- Files Modified: `SESSION_LOG.md`, `Tools/modelgen/vern_mpfb_body_diagnostic.py` (new), `Tools/modelgen/source/vern_mpfb_body_diagnostic.blend` (new), `docs/art/model_previews/vern_mpfb_body_diagnostic.json` (new), `docs/art/model_previews/vern_mpfb_body_axis_{plus_y,minus_y,plus_x,minus_x}.png` (new), `docs/art/3D_ASSET_WORKFLOW.md`, `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`.
- Work Done: User reviewed `vern_mpfb_prototype_side.png` and rejected it: model appears backward/sideways and primitive clothing/accessories do not fit. Added a clean diagnostic path with no clothes/accessories. `vern_mpfb_body_diagnostic.py` creates an MPFB body, applies built-in male target shape keys (`caucasian-male-old.target.gz`, `universal-male-old-averagemuscle-averageweight.target.gz`), adds MPFB standard rig, saves source blend, renders four axis-labelled views, and writes a report. Report confirms `male_assertion.verified=true`, height 1.715m, shoulder width 0.362m, hip width 0.219m, shoulder/hip ratio 1.653. A geometry probe confirms the face points toward **Blender +Y** (exports to glTF/Godot -Z); the MPFB standard-rig/body exposes landed landmarks as vertex groups (`joint-*`, `helper-*`, `body`) for fitted-clothing work.
- Next Steps: User visually verifies the four axis renders (face in `plus_y`, back in `minus_y`). After approval, implement fitted/skinned clothing against evaluated body/rig landmarks; do not reuse primitive overlay clothing.
- Related Docs: `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`.
- Verification: `blender --background --python-exit-code 1 --python Tools/modelgen/vern_mpfb_body_diagnostic.py` passed and produced male-verified diagnostic report/previews.
- Blockers: none for body orientation; clothing/accessory fitting intentionally deferred until front axis is confirmed.

---

## Previous Session (completed - MPFB replacement prototype rejected visually)

**Branch**: develop
**Task**: Build an MPFB-derived Vern replacement prototype instead of continuing to tune the hand-authored procedural body. Keep production `vern.glb` safe while generating a separate measured prototype asset, previews, and migration notes.
**Status**: Completed (prototype generated; not runtime-wired)
- Files Modified: `SESSION_LOG.md`, `Tools/modelgen/vern_mpfb_prototype.py` (new), `Tools/modelgen/source/vern_mpfb_prototype.blend` (new), `assets/models3d/characters/vern_mpfb/vern_mpfb_prototype.glb` (new), `docs/art/model_previews/vern_mpfb_prototype.json` (new), `docs/art/model_previews/vern_mpfb_prototype_{front,side,portrait}.png` (new), `docs/art/3D_ASSET_WORKFLOW.md`, `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`.
- Work Done: User confirmed current procedural Vern still looks wrong/out of proportion and approved using MPFB as the actual base. Added `vern_mpfb_prototype.py`, which uses MPFB's generated body + standard rig as the anatomical base and overlays rough Vern identity geometry (dark sweater/trousers, hair, gray temples, aviator frames, mustache, headphones). Generated separate prototype source/GLB/report/previews. Prototype report: height 1.67m, 1 armature, 34 meshes, ~21.6k polygons; status `prototype_not_runtime_wired`.
- Next Steps: visual review the prototype images; if approved, migrate it toward production: final skinned clothing, seated pose, contact anchors, animation clips, and `talk_calm` retarget/rebake. Do not replace runtime `vern.glb` directly yet.
- Related Docs: `docs/art/CHARACTER_GUIDELINES.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`, `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`.
- Verification: `blender --background --python-exit-code 1 --python Tools/modelgen/vern_mpfb_prototype.py` passed and exported prototype GLB/previews; `dotnet build` passed; `git diff --check` clean except CRLF normalization warnings; `run-tests.ps1 -Filter VernAnimationControllerTests` passed 9/9; `run-tests.ps1 -Filter VernCharacterIntegrationTests` passed 1/1. Test runner still warns that `GODOT` points at 4.5.1 and auto-selects Godot 4.6.3, plus a pre-existing invalid UID warning in `test/Tests.tscn`.
- Blockers: production `talk_calm` and prop contacts are tied to current Vern skeleton; full runtime replacement will need a retarget/contact migration after visual approval.

---

## Previous Session (completed - MPFB audit + procedural lower-body pass)

**Branch**: develop
**Task**: MPFB-informed Vern proportion audit and lower-body refinement: add reproducible current-model and MPFB comparison measurements, document what the latest retarget/runtime integration changed, clear stale known-limitations notes, and make a conservative mesh-only lower-body proportion adjustment without changing Vern's skeleton contract.
**Status**: Completed
- Files Modified: `SESSION_LOG.md`, `Tools/modelgen/vern_proportion_audit.py` (new), `Tools/modelgen/mpfb_vern_proportion_compare.py` (new), `Tools/modelgen/vern.py` (mesh-only lower-body refinement), `Tools/modelgen/source/vern.blend`, `assets/models3d/characters/vern/vern.glb`, `docs/art/model_previews/vern.json`, refreshed static previews (`vern_bind_pose.png`, `vern_feed_preview.png`, `vern_front.png`, `vern_portrait.png`, `vern_seated.png`, `vern_side.png`), `docs/art/model_previews/vern_godot_animation_validation.json`, `docs/art/model_previews/vern_proportion_audit.json`, `docs/art/model_previews/mpfb_vern_proportion_comparison.json`, `docs/art/VERN_CHARACTER_GUIDELINES.md`, `docs/art/3D_ASSET_WORKFLOW.md`, `docs/art/VERN_3D_MODEL_BRIEF.md`.
- Work Done: Added a read-only Blender audit tool that builds current procedural Vern in memory and writes mesh bounds, visual slice widths, skeleton landmarks, limb lengths and ratios. Added an MPFB comparison tool that creates a default MPFB human, adds the MPFB standard rig, measures matching landmarks, and compares ratios. MPFB default baseline: 1.659m height, skeletal shoulder width 0.340m, hip width 0.203m. Current Vern remains 1.877m neutral / 1.542m seated; shoulder/height is close to MPFB, but Vern's skeleton ratios still reflect stylized head/limb choices. First mesh-only edit was too subtle, so it was strengthened: lower torso/ribbing is visibly broader, trouser thighs are thicker, and leg/foot mesh centers moved outward by ~2.8cm per side from the original while preserving all bone names, bone heads, clip names, contact JSON, and `talk_calm` compatibility. Static Blender previews were regenerated so the difference is visible in review images.
- Next Steps: visually review `docs/art/model_previews/vern_seated.png` / Godot feed in-editor or with preview capture; if the lower-body change looks good, commit. Further anatomical changes that move bones should wait until `talk_calm` can be rebaked from local references.
- Related Docs: `docs/art/CHARACTER_GUIDELINES.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`, `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`.
- Verification: `blender --background --python-exit-code 1 --python Tools/modelgen/mpfb_vern_proportion_compare.py` passed; `blender --background --factory-startup --python-exit-code 1 --python Tools/modelgen/vern.py -- --skip-previews` passed and validated exported GLB; `blender --background --factory-startup --python-exit-code 1 --python Tools/modelgen/vern_proportion_audit.py` passed; `python Tools/modelgen/vern_godot_validate.py --godot <4.6.3 console>` passed; `dotnet build` passed; `run-tests.ps1 -Filter VernAnimationControllerTests` passed 9/9; `run-tests.ps1 -Filter VernCharacterIntegrationTests` passed 1/1. Test runner still warns that `GODOT` points at 4.5.1 and auto-selects Godot 4.6.3, plus a pre-existing invalid UID warning in `test/Tests.tscn`.
- Blockers: no local `docs/references/` bundle in this checkout, so any future bone/rest changes requiring `talk_calm` rebake must restore the reference GLB first.

---

## Previous Session (completed - talk_calm production integration)

**Branch**: develop
**Task**: Migrate the approved V5 `talk_calm` retarget bake into the production repo: commit a self-contained Godot bake tool + retarget BoneMaps under `Tools/modelgen/`, bake the clip to `assets/models3d/characters/vern/`, wire it into runtime (VernCharacter3D inject + controller), port validators, and update docs (SESSION_LOG, SPIKE_REPORT, CHARACTER_REFERENCE_LIBRARY clip-name fix, bake-tool usage) so the retarget path is reproducible from committed files only.
**Status**: Completed - baked clip wired into runtime (inject + controller select), all Vern runtime tests green, docs synced, cleanup done. Pre-existing 10 test failures verified unrelated (DI-harness, outside World3D). No commit (not asked).
- Files Modified: `scripts/world3d/props/VernCharacter3D.cs` (inject `talk_calm.tres` into the player's root animation library in `_Ready`; `HasAnimation` guard + load-fail warning), `scripts/world3d/props/VernAnimationController.cs` (`AnimTalking = "talk_calm"` + `AnimTalkingFallback = "talking_default"` via new `ResolveTalkingAnimation`, doc comment updated), `tests/integration/VernAnimationControllerTests.cs` (`TalkingAnimation` const `"talking_default"` → `"talk_calm"`), `tests/integration/VernCharacterIntegrationTests.cs` (+ assert `talk_calm` is injected into the imported model), `docs/art/CHARACTER_REFERENCE_LIBRARY.md` (clip names de-`_Loop`-ified against verified real names; adds asset-order note), `docs/art/VERN_CHARACTER_GUIDELINES.md` (line 14 `Sitting_Idle_Loop`/`Sitting_Talking_Loop` → real names), `docs/art/3D_ASSET_WORKFLOW.md` (+ "Baked talk clip (talk_calm)" section: bake/validate commands, runtime notes), `.gitignore` (+`Tools/modelgen/tmp/`), `SESSION_LOG.md`; deleted `Tools/modelgen/probe_retarget_vs_raw.gd`(+uid, temp absolute path); reverted whitespace-only `project.godot` diff. Temp: finalized `C:\Users\lblan\AppData\Local\Temp\opencode\kbtv_retarget\SPIKE_REPORT.md` (status → COMPLETE, production port + closure notes).
- Work Done: wired `talk_calm.tres` into runtime. Injection verified root-library semantics from the probe (vern.glb clips live in the empty-name library, hence unqualified names; `talk_calm` added the same way → `HasAnimation("talk_calm")` true). Controller now selects `talk_calm` for Vern lines with automatic `talking_default` fallback if injection failed. Build green; `VernAnimationControllerTests` 9/9 and `VernCharacterIntegrationTests` 1/1 (incl. new injection assert) pass; full suite 635/10 with the same 10 pre-existing DI-harness failures (re-verified AdManager via stash: 4/4 fail on clean tree too; the rest - LoadingScreen, TranscriptManager, GameStateManager, ConversationArc - touch no World3D code). Confirmed real reference-GLB clip names headlessly (probe_sources.gd): NO `_Loop` suffixes anywhere on the 46 clips (e.g. `Sitting_Idle`/`Sitting_Talking`, `Idle`, `Idle_Talking`); docs corrected to match.
- Next Steps: none (task complete). Open Godot editor to trigger `.uid`/import of new tool scripts if needed; commit when asked (include `Tools/modelgen/{bake_talk_calm.gd,probe_sources.gd,reimpl_validate.gd,val_fidelity_vs_v5.gd}(+.uid)`, `Tools/modelgen/retarget/`, `assets/models3d/characters/vern/animations/talk_calm.tres`, the 4 code/test files, and the 4 doc/.gitignore changes). `Tools/modelgen/tmp/` and `docs/references/` stay gitignored.
- Related Docs: `docs/art/3D_ASSET_WORKFLOW.md` (Baked talk clip section), `docs/art/CHARACTER_REFERENCE_LIBRARY.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`, `C:\Users\lblan\AppData\Local\Temp\opencode\kbtv_retarget\SPIKE_REPORT.md`.
- Blockers: none.

---

## Previous Session (completed - retarget spike)

**Branch**: develop
**Task**: Retarget spike (user-chosen): use Godot 4.6's built-in importer retarget (`retarget/bone_map` + SkeletonProfile) to retarget the CC0 reference library's `Sitting_Talking` onto Vern's rig, bake candidate `talk_calm` (body from reference rebased onto Vern rest, arms/fingers from Vern's authored talking_default, jaw/eyelid kept from Vern), validate head-to-head vs procedural `talking_default`, then go/no-go. Approach approved via question tool (scope: "Retarget spike first"; path: "Godot importer retarget"), execution approved ("ok try it").
**Status**: Completed (GO) - V5 `talk_calm` approved by visual review. Spike harness lives OUTSIDE the repo at `C:\Users\lblan\AppData\Local\Temp\opencode\kbtv_retarget\` (copies of reference.glb + vern.glb, retarget/ bone maps, `.godot/imported/*.scn`, `bake_talk_calm.gd`, `val_contract.gd`, `probe_baked_hand.gd`, `baked/talk_calm.tres` V5 output, `SPIKE_REPORT.md`).
- Work Done: Resolved all headless-authoring unknowns from Godot 4.6 sources (downloaded to `C:\Users\lblan\AppData\Local\Temp\opencode\godot463\src\`). **Serialization of per-node retarget settings fully understood**: ONE global `_subresources` (Dict) param; per-node settings at `_subresources["nodes"]["<import_id>"]` = `PATH:<scene-root>/<node-path>`; Skeleton3D opts `retarget/bone_map` (BoneMap) + post-import plugins (`retarget/bone_renamer/*`, `retarget/rest_fixer/*` retarget_method 0/1/2 None/Overwrite Axis/"Use Retarget Modifier" default 1, etc.), `retarget/remove_tracks/*`. Renamer MUST be on. Godot rewrites `.import` canonically after every import. Built spike harness, applied retarget to the reference copy (profile-named skeleton), baked V5 talk_calm (52 tracks = 48 body + 4 vern-face; 2.933s @24fps linear loop): PINNED legs/root/pelvis to vern rest, arms/fingers from vern talking_default AUTHORED_WINDOW + FINGER_GAIN 1.8 + SEAM_BLEND 0.4 ease-back, FINGER_CURL_DEG 5/8/10 curl, HAND_ROLL 180 thumb-up, WRIST_GAIN 1.2 mannequin wrist gesture, last==first loop clamp. Validators: plant=0, seam=0, motion=0.4130, head=0.0019, jaw=0.0119, handL=0.2958, handR=0.2403; hand probe roll 179.9/180.0deg, gesture swing 19.29/6.16deg, thumb-world-Y +0.708/+0.671. **Design decision for migration**: reference can be retargeted via `.import` OR the bake can read the RAW reference directly and map DEF-* → vern through a profile join of both BoneMaps (chosen - fully committed, reproducible, no gitignored `.import` dependency).
- Related Docs: `docs/art/CHARACTER_REFERENCE_LIBRARY.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`, `docs/art/CHARACTER_GUIDELINES.md`.
- Blockers: none.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Add local reference-asset catalog for character/animation work: gitignore `docs/references/` (2 large bundles), create `docs/art/CHARACTER_REFERENCE_LIBRARY.md` with extracted inventory + links (Quaternius 46-clip animation library incl. Godot GLB; human-base-meshes v1.4.1 blend + catalog), cross-link from the character guides, register in AGENTS.md.
**Status**: Completed
- Files Modified: `docs/art/CHARACTER_REFERENCE_LIBRARY.md` (new), `AGENTS.md` (docs table row), `SESSION_LOG.md`, `.gitignore` (docs/references/).
- Work Done: scanned folders (~101 MB total). Animation Library[Standard] = Quaternius CC0; Godot GLB has 46 named clips incl. `Sitting_Enter/Exit/Idle_Loop/Talking_Loop`, `Idle_Talking_Loop`, `A_TPose` (Vern-relevant); Unity/Unreal FBX exports + previews. human-base-meshes-bundle-v1.4.1 = `human_base_meshes_bundle.blend` + `blender_assets.cats.txt` + 24 thumbnails across Planar/Primitives/Realistic/Stylized; NO license file bundled (flagged verify-before-shipping). Catalog retarget guidance written (§5 real-motion, §9 Godot humanoid retargeting). Fed the retarget spike below.
- Next Steps: subsequent sessions use the library as inspiration per character guidelines.
- Related Docs: `docs/art/CHARACTER_REFERENCE_LIBRARY.md`, `docs/art/CHARACTER_GUIDELINES.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`.
- Blockers: none.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Add persistent character agent context - generic `docs/art/CHARACTER_GUIDELINES.md` (character-agnostic model/animation principles for future character work) + Vern-specific `docs/art/VERN_CHARACTER_GUIDELINES.md` (IK anchors, animation vocabulary, validation sequence, known limitations); register both in `AGENTS.md` docs table and point `VERN_3D_MODEL_BRIEF.md` (+ Astra handoff prompt) at them.
**Status**: Completed
- Files Modified: `docs/art/CHARACTER_GUIDELINES.md` (new), `docs/art/VERN_CHARACTER_GUIDELINES.md` (new), `AGENTS.md`, `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`, `SESSION_LOG.md`.
- Work Done: split authored character guidelines into generic + Vern-specific docs. Generic keeps pipeline/anatomy checks, rest-pose, IK anchor pattern, reusable animation layers, mocap-first, anti-robotic timing, secondary motion, stateful interactions, Godot humanoid system, fix-priority order, validation sequence, diagnosis-driven AI behavior. Vern doc holds concrete interaction anchors (coffee/cigarette/ashtray/mic/chair/desk), named clip vocabulary, seated pose, coffee validation sequence, smoking state machine and known rig limitations (four fingers share one grip bone, rigid thumb, fixed talking loop, sampled prop trajectories).
- Next Steps: confirm both docs read clean, markdown links resolve, and future Astra sessions reference the generic doc then the Vern doc before touching the character.
- Related Docs: `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`, `docs/art/CHARACTER_GUIDELINES.md`, `docs/art/VERN_CHARACTER_GUIDELINES.md`.
- Blockers: none.

---

## Previous Session (in progress - Vern model/animation enrichment)

**Branch**: develop
**Task**: Enrich Vern's existing stylized model/materials and improve breathing, talking, smoking, and drinking, especially articulated hands and believable contacts.
**Status**: In Progress
- Approved direction: preserve the current stylization with richer detail. User supplied Art Bell seated radio-studio photo: swept dark hair, restrained mustache, aviator glasses, black high-neck sweater, relaxed supported posture.
- Files Modified: `SESSION_LOG.md`.
- Work Done: inspected generator, rig, animation/runtime and existing previews. Flat-color materials; four fingers share one grip bone, thumb rigid; fixed talking loop and separate sampled prop trajectories identified as limitations.
- Next Steps: establish baseline; upgrade model/materials and hand articulation; refine performances; regenerate/review and validate in Blender/Godot.
- Related Docs: `docs/art/VERN_3D_MODEL_BRIEF.md`, `docs/art/3D_ASSET_WORKFLOW.md`.
- Blockers: none.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Evidence modal usability round 2: (1) keyboard/input tiles hide letter state - ruled-out (disabled) letters rendered with the generic gray disabled style, so red status never showed; locked green slots in `PWD>` also gray. Fix `ApplyDosButtonStyle` usage with a tile style that keeps the status color in the disabled state. (2) Curate `assets/config/evidence_words.json` - replace the 3185-entry dictionary dump (ARCUS/TULLE/PILAF tier + unwinnable `SO-SO`/`X-RAY` entries) with ~600 common, theme-weighted 5-letter words; validate `^[A-Z]{5}$` + dedupe on load; fix stale metadata. (3) Interaction: red blink when typing a ruled-out letter, "PASSWORD INCOMPLETE" warning when Enter is pressed with empty slots, description text says "5-letter password (an English word)". (4) Tests: word-file schema, win-sweep over every shipped word, tile style color assertions, incomplete-enter.
**Status**: Completed - build green; full suite 634/10 (baseline 630/10 + 4 new EvidenceModal tests; same 10 pre-existing failures). Live-playtest confirmation: user won a real game with "TARDY" from the curated list.
- Files Modified: `assets/config/evidence_words.json` (replaced 3185-entry dictionary dump with 1315 curated common words - all corpus-validated real words, `^[A-Z]{5}$`, unique, thematic picks like GHOST/PROOF/RADIO/SIREN/HEXES/OUIJA; metadata corrected; hyphenated/unwinnable entries gone), `scripts/ui/EvidenceModal.cs` (loader validates ^[A-Z]{5}$ + dedupes; `ApplyDosButtonStyle` gained `statusColor` param so DISABLED tiles keep their status color - ruled-out letters now render red font/border/dark-red bg instead of generic gray, locked green slots stay green; `_letterButtons` registry + red blink tween when a ruled-out letter is typed; Enter with empty slots shows "PASSWORD INCOMPLETE - N SLOT(S) EMPTY" without spending an attempt; description text now says "5-letter password (an English word)"), `scenes/ui/EvidenceModal.tscn` (description text), `tests/unit/ui/EvidenceModalTests.cs` (+4 tests: word-file schema, sampled win-sweep over the loaded pool incl. double-letter words, ruled-out red disabled state, incomplete-Enter warning), `docs/systems/EVIDENCE_SYSTEM.md`. Color pass: `scripts/world3d/TerminalOverlay.cs` (`PhosphorTint` 0.74/1.0/0.9 -> 0.85/1.0/0.82 so yellow keeps its red channel; `CrtTint` overlay 0.10 alpha teal -> 0.06 lighter teal), `scripts/ui/EvidenceModal.cs` (`WrongPosColor` pure yellow -> amber 0.95/0.72/0.10 - hue-safe against green under any phosphor tint), `docs/systems/EVIDENCE_SYSTEM.md`.
- Work Done: Root-caused the "random letters + stuck logic" report: (1) `OSPTO` was never a valid target (not in any word file) - the game had never accepted the guess; (2) ruled-out keyboard tiles rendered with the generic gray disabled style so eliminations were invisible, making the puzzle feel like the logic was wrong; (3) the pool was a raw dictionary dump including unwinnable `X-RAY`/`SO-SO`. Fixed presentation + curation + guards as above.
- Next Steps: in-editor verify: ruled-out letters now visibly red on the KEYBOARD grid; solved-letter slots green in PWD> row; typing a red letter flashes; incomplete Enter warns; wrong-spot amber vs. correct-spot green clearly distinct on the CRT (softer tint + amber). Commit when asked (save.json is user gameplay data - do not commit unintentionally).
- Related Docs: `docs/systems/EVIDENCE_SYSTEM.md`

---

## Previous Session (completed)

**Branch**: develop
**Task**: Fix evidence Wordle minigame reliability + patience timing: correct `PatienceDisplay.Remaining` double-count (drained patience minus session ElapsedTime expired the dialog at ~1/3 of the real window), raise caller base patience 60->90 (~180s to solve), remove duplicate `UpdateScreenableProperties` call (reveals ran at 2x speed), validate fallback word list (4-letter DARK/MOON made the game unwinnable/no-auto-fill), accept numpad Enter, and add Wordle-invariant regression tests (green auto-fill across guesses, double-letter letters stay enabled until fully ruled out).
**Status**: Completed - build green; full suite 630/10 (baseline 624/10 + 6 net new passing tests; same 10 pre-existing failures: AdManager x4, GameStateManager, LoadingScreen x2, TranscriptManager x2, ConversationArcTests UFO-audio gap)
- Files Modified: `scripts/ui/PatienceDisplay.cs` (Remaining/Ratio now mirror the drained `Caller.ScreeningPatience` - dropped the erroneous `- ElapsedTime` term; signature simplified), `scripts/ui/ScreeningPanel.cs` + `scripts/ui/EvidenceModal.cs` (call-site updates; modal expiry fallback now fires exactly at hangup), `scripts/callers/CallerGenerator.cs` (`_basePatience` 60->90), `scripts/screening/ScreeningSession.cs` (removed duplicate `Caller.UpdateScreenableProperties` - reveals were advancing 2x), `scripts/ui/EvidenceModal.cs` (KpEnter submits; fallback word list fixed `DARK`/`MOON` -> `DUSKY`/`LUNAR` + validated in `UseFallbackWords`), `tests/unit/ui/PatienceDisplayTests.cs` (rewritten for corrected semantics + regression test), `tests/unit/ui/EvidenceModalTests.cs` (+5 Wordle-invariant tests), `docs/systems/EVIDENCE_SYSTEM.md`, `docs/ui/SCREENING_DESIGN.md` (stale 20-40s patience note corrected).
- Work Done: Audited Wordle logic - the two-pass per-guess evaluation, green auto-fill/lock (`PrepareNextInput`), and the `!= RuledOut` keyboard gate are correct: double-letter occurrences stay typeable until fully ruled out (locked in by new tests against deterministic targets PROOF/PAPER/SPELL). Root cause of the perceived modal bug was the patience math: the dialog auto-closed at ~40s (drained patience minus ElapsedTime double-counted the drain) instead of ~120s; now ~180s with the base-patience raise.
- Next Steps: in-editor sanity: screen a caller to Evidence -> confirm PATIENCE bar drains to match the caller actually hanging up; solve a double-letter password and watch greens carry across lines; numpad Enter submits. Commit when asked.
- Related Docs: `docs/systems/EVIDENCE_SYSTEM.md`, `docs/ui/SCREENING_DESIGN.md`

---

## Previous Session (completed)

**Branch**: develop
**Task**: Evidence minigame presentation rework: render `EvidenceModal` on the CRT terminal (inside `TerminalOverlay`'s SubViewport, above the CallerTab screening UI) instead of the fullscreen `ModalManager` CanvasLayer; DOS-restyle to match screening UI fonts; reword as decrypting the evidence password; clickable letter tiles (unused letters only) with keyboard parity (guessed letters blocked); auto-close + lose evidence when the terminal is hidden mid-game.
**Status**: Completed (code) - build green; full suite 617/10 (baseline 610/9 + 7 new `EvidenceModalTests`; 10th failure = pre-existing `ConversationArcTests` UFO-audio coverage gap from blocked session)
- Files Modified: `scripts/ui/ModalManager.cs` (CRT host routing + key forwarding + `AbortEvidenceModal` + phosphor tint param), `scripts/ui/EvidenceModal.cs` (rewrite: decrypt wording, DOS styling, clickable letter/slot buttons, `HandleKey`/`Abort`/`IsCrtHosted`, cached-controller lookups; fix pass: removed `ZIndex=100` so CRT effects render over the dialog; letter gate now `!= RuledOut` so green/yellow stay re-typeable), `scenes/ui/EvidenceModal.tscn` (DOS restyle + compact pass: 16-84% anchors, divider removed, 4-6px separations, 28×22 tiles), `scripts/world3d/TerminalOverlay.cs` (`ContentHost`, `PhosphorTint` constant), `scripts/world3d/World3D.cs` (host sync on open w/ tint, abort on close/nav-back), `tests/unit/ui/EvidenceModalTests.cs` (new, 7 tests incl. ruled-out lockout + wrong-position reuse), `docs/systems/EVIDENCE_SYSTEM.md` (decryption dialog section), `SESSION_LOG.md`.
- Work Done: modal hosted in CRT content host when terminal visible (fullscreen fallback otherwise); keyboard routed via `ModalManager._Input` -> `modal.HandleKey` when hosted (subviewport nodes don't get main-viewport `_input`); only RED (ruled-out) letters lock out - green/yellow re-typeable (Wordle-standard, fixes unsolvable double-letter cases); slot click clears unlocked slot; ENTER button + key submit; win/lose wording per approved set; terminal close/nav-back = `Abort()` (LoseEvidenceOpportunity); hosted modal gets CallerTab's phosphor `Modulate` and no ZIndex so scanlines/vignette/glass draw over it. Follow-up pass: `ContentContainer` insets 16/12 restored (Panel stylebox margins don't inset anchor children), solved-state button label "Download" everywhere (was "Extract Evidence"), error line "Download failed". Patience pass: new shared `scripts/ui/PatienceDisplay.cs` helper (remaining = ScreeningPatience - ElapsedTime, `[|||||.....] NN%` bar, `GetPatienceColor`); ScreeningPanel now delegates to it; EvidenceModal replaced its `ProgressBar` with the same `PATIENCE` caption + text label (12px mono, per-frame update, expiry fallback at remaining<=0). +7 `PatienceDisplayTests`; full suite 624/10.
- Next Steps: in-editor verify - screen a caller to Evidence reveal -> dialog renders on the monitor over screening UI with CRT scanline/tint over it; click letters, wrong guess colors letters red/green/yellow on board; ESC away mid-game seals file; extract stores evidence.
- Related Docs: `docs/systems/EVIDENCE_SYSTEM.md`, `docs/ui/SCREENING_DESIGN.md`

---

## Previous Session (blocked - UFO arc audio)

**Branch**: develop
**Task**: UFO conversation-arc expansion: 50 new arcs (5 crazy-sincere stories, 23 opinion-based callers, 22 question-askers; Vern shares info in question/opinion arcs). Catalog + progress checklist: `docs/design/UFO_ARC_EXPANSION.md`.
**Status**: Blocked - JSON authoring complete for all 50 arcs; audio generation partially complete, blocked by ElevenLabs quota
- Files Modified: `SESSION_LOG.md`, `docs/design/UFO_ARC_EXPANSION.md`, `AGENTS.md` (docs table row), `Tools/ArcFactory/build_arcs.py`, `Tools/ArcFactory/arcs_data/{__init__,batch1,batch2,batch3,batch4,batch5,batch6,batch7,batch8a,batch8b,batch9}.py`, 50 new/updated arcs in `assets/dialogue/arcs/UFOs/`.
- Work Done: pilot 5 arcs hand-authored and audio generated earlier; factory authored/generated batches 1-9, including final arcs `ufo_what_should_i_write`, `ufo_roadside_or_microsleep`, `ufo_still_believe_eyes`, `ufo_have_you_seen_one`, and `ufo_alien_ballot`.
- Verified: `pwsh -NoProfile -File run-tests.ps1 -Filter ArcSchemaValidationTests` passed; `ArcSchemaValidationTests: validated 123 arcs`.
- Audio State: user approved full generation. First pass timed out after 1 hour; second pass continued until ElevenLabs returned `quota_exceeded` / `0 credits remaining`. Missing audio reduced from `3021` to `1575` files. Remaining incomplete arcs: `ufo_metal_shard_safety` (31), `ufo_netflix_or_radio` (68), and 22 additional arcs with full/partial gaps (`ufo_press_coverup_local`, `ufo_producers_compromised`, `ufo_radar_vs_drone`, `ufo_rated_pilot_policy`, `ufo_ready_for_contact`, `ufo_returned_ring`, `ufo_roadside_or_microsleep`, `ufo_rooftop_observation_post`, `ufo_shoot_it_down`, `ufo_sleep_paralysis_purist`, `ufo_starling_man`, `ufo_still_believe_eyes`, `ufo_sue_power_company`, `ufo_support_group`, `ufo_they_run_dmv`, `ufo_time_travelers`, `ufo_tin_hat_neighbor`, `ufo_town_coverup`, `ufo_wedding_lights`, `ufo_what_should_i_write`, `ufo_where_do_i_report`, `ufo_why_only_america`). `ConversationArcTests` will fail audio coverage until complete.
- Next Steps: after ElevenLabs credits reset/top-up, run `python Tools/AudioGeneration/generate_arc_audio.py --all` again; the generator skips existing files. Then run `python Tools/AudioGeneration/generate_arc_audio.py --all --check`, open Godot to import new mp3s, and run relevant conversation tests.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Vern dialogue variety expansion (all types, not just dead air): mood-gap fixes (between-callers +12, off-topic-remarks +15, caller-cursed +16), topic parity + personal flavor (openings/closings/return-from-breaks +40 each & +3/3/4 personal), blend helper applied to GetShowOpening/GetShowClosing/GetReturnFromBreak (topic/open/personal shares), and mood wiring for GetCallerCursed in BroadcastStateMachine. Audio generated for new lines only (~169).
**Done**: (1) +173 lines across 6 JSON files: between-callers now 46 (every 13 moods >=3), off-topic-remarks 44 (5 zero-moods filled), caller-cursed 35 (every mood >=2), openings 93 / closings 93 / returns 94 (all 4 content topics at 20 + `personal` pool 3/3/4). All ids unique cross-file, no asterisks, text==voiceText (except pre-existing `deadair_conspiracies_10`). (2) `VernDialogueTemplate`: new static `GetBlendedTopicLine` helper (topic/generic-open/personal pools, share-normalized, template Weight still applies, null only when all pools empty); applied to `GetShowOpening` (80/15/5; Open shows 90/10), `GetShowClosing` (same), `GetReturnFromBreak` (75/15/10; Open 85/15), and refactored `GetDeadAirFiller` onto it (60/25/15; Open 60/40 - behavior preserved). Legacy mood/random fallbacks preserved. (3) `BroadcastStateMachine` CallerCursed case now resolves `VernStats.CurrentMoodType` and calls `GetCallerCursed(mood)` (was neutral-only). (4) +6 new tests in `ConversationManagerTests` (blend reachability/ordering, foreign-topic exclusion x3, Open-show pool, empty-template null fallback, cursed mood+fallback). (5) Audio: 173 new mp3s generated (378 skipped), full inventory check 551/551 lines have audio. (6) Docs: `AUDIO_GENERATION.md` de-staled counts, `VOICE_AUDIO.md` broadcast file count ~350->~550, `DEAD_AIR_FILLER.md` new "Pool Blending" section. **Status: Completed - build green; full suite 610/9 (baseline 604/9 + 6 new passing tests; same 9 pre-existing DI-harness failures)**
- Files Modified: `assets/dialogue/vern/{between-callers,off-topic-remarks,caller-cursed,openings,closings,return-from-breaks}.json`, `scripts/dialogue/Templates/VernDialogueTemplate.cs`, `scripts/dialogue/BroadcastStateMachine.cs`, `tests/unit/dialogue/ConversationManagerTests.cs`, docs `{tools/AUDIO_GENERATION.md,audio/VOICE_AUDIO.md,ui/DEAD_AIR_FILLER.md}`, `SESSION_LOG.md`, 173 new gitignored mp3s in `assets/audio/voice/Vern/Broadcast/`.
- Known gaps (deferred): break-transitions/dropped-callers still random-select (mood tags affect audio tone only); other topics' dead-air fillers still at 10 (ufos has 20); ~26 legacy recorded-assertion failures (debt pass owed); pre-existing `deadair_conspiracies_10` text/voiceText divergence.
- Next Steps: open Godot once so new mp3s `.import`; in-editor sanity - low-VIBE show should hear tired/depressed between-callers, UFO show should surface `personal` anecdotes in openers/filler/returns; cursed reaction should track Vern's mood; commit when asked.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Expand Vern dead-air filler library (UFO focus): +10 UFO lines, +8 generic (`open`), +8 personal-life anecdotes (`dead-air-fillers.json`), plus small `GetDeadAirFiller(topic)` change so generic/personal mix into topic shows (60/25/15; Open shows 60/40). Audio generated via `generate_vern_audio.py` (existing files skipped).
**Done**: (1) 26 new lines appended (ufos 11-20: Belgium wave, Tic Tac, Navy videos, Whitsunday, AATIP money, Ontario pilot chase, autopsy-hoax angle, Betty & Barney, Colares, hearing no-comment; open 11-18: phone-board/being-believed/mailbag washer/weather alibi/pattern machine/late-night tradition/secret-vs-lie/static; personal 1-8: Venus childhood sighting, night-shift divorce, CB buddy "Ghost I-40", raccoon-in-the-transmitter, 3 a.m. diner, dead truck at the reservoir, radar-operator father's silence, three-page notebook). JSON validated: 76 lines, ids unique file+cross-file, text==voiceText (except pre-existing `deadair_conspiracies_10`), no asterisks. (2) `VernDialogueTemplate.GetDeadAirFiller(ShowTopic)` now blends topic 60% / open 25% / personal 15% via weighted selector over the combined pool (no template mutation); Open shows draw open 60 / personal 40; empty-pool fallback preserved; share constants documented. (3) 4 new tests in `ConversationManagerTests` (foreign-topic exclusion, blend reachability + ordering, Open-show pool, topic-only-pool regression). (4) Audio: 26 new mp3s generated into `Vern/Broadcast/` (352 existing skipped), all present, sane sizes. **Status: Completed — build green; full suite 604/9 (baseline 600/9 + 4 new passing tests; same 9 pre-existing DI-harness failures)**
- Files Modified: `assets/dialogue/vern/dead-air-fillers.json`, `scripts/dialogue/Templates/VernDialogueTemplate.cs`, `tests/unit/dialogue/ConversationManagerTests.cs`, 26 new gitignored mp3s under `assets/audio/voice/Vern/Broadcast/`.
- Next Steps: open Godot once so the new mp3s get `.import`ed; in-editor listen pass on a UFO-topic show dead air (expect variety incl. anecdotes); commit when asked.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Make the dialog/audio generation process seamless ahead of content expansion. Audit found 16 missing mp3s (fixed via ElevenLabs earlier in this work) + 4 process seams: (1) generator's hardcoded `folder_name_map`/arc-id topic heuristics diverged from the runtime line-id-prefix rule and could not handle new non-UFO arcs; (2) 13 mood variants per vern line were authored+generated but `DialogueExecutable` only ever played the neutral default; (3) `AudioDialoguePlayer` + `BroadcastAudioService.GetTopicFromArcId` carried a dead, WRONG arcId-substring path rule (breaks topic-switchers if reused); (4) `KBTVTestClass` recorded assertion failures without ever failing the suite (hidden-debt), and docs (SCHEMA/ARCS/VOICE/AUDIO_GENERATION/TOOLS) still described 7-mood/belief-branch/Unity-Addressables era rules.
**Done**: (1) `generate_arc_audio.py` rewritten JSON-driven: locates arcs by filename under `arcs/`, routes per-line by line-id prefix (mirrors `ArcAudioTopics`), folder = JSON arcId; new `--check`/`--all` (no API, exit 1 on gaps); verified 73/73 arcs, 0 missing, new-arc + topic-switch routing proven. (2) Mood wired: `ArcDialogueLine.GetAudioIdForMood/GetTextForMood` (neutral fallback) + `DialogueExecutable` resolves per line from `VernStats.CurrentMoodType`; 4 new unit tests. (3) `AudioDialoguePlayer.cs`+`IDialoguePlayer.cs`(+tests) deleted (dead); `BroadcastAudioService.LoadVoiceAudioForItem` now uses `ArcAudioTopics`; removed obsolete `GetDialogueForMood`. (4) Integrity test upgraded to EVERY line incl. mood variants; new `ArcSchemaValidationTests` enforces arcId==filename==folder, global uniqueness, alternation, 13-variant/1-variant turn shapes, id pattern + legitimacy/mood tokens, turn-ordinal sequences - all 73 arcs pass strict rules. (5) `KBTVTestClass.FailOnRecordedFailures` opt-in (dialogue suites enable it; surfacing all legacy recorded-failures would add ~26 pre-existing failures - flagged as debt, not fixed this session). (6) Docs: SCHEMA rewritten as authoritative rulebook w/ checklist; ARCS, VOICE, AUDIO_GENERATION, TOOLS de-staled (Piper/belief/Addressables/7-mood removed); `extract_arc_ids.py` + `missing_audio.txt` deleted; ~101 legacy-named Broadcast mp3s noted as orphaned (left on disk; user deferred cleanup). **Status: Completed (code+docs) - build green; full suite 600/9 (baseline 598/12 minus 3 deleted AudioDialoguePlayerTests; remaining 9 all pre-existing DI-harness failures: AdManagerx4, GameStateManager, LoadingScreen, TranscriptManagerx2)**
- Key invariant (now enforced 3 ways - generator `--check`, schema test, audio test): audio topic folder = first `_` token of the line id; arcId == JSON filename == audio folder name; descriptor in line ids should equal arcId (legacy `dashcam`/`dashcam_trucker` deviation tolerated by tests).
- Files Modified: `Tools/AudioGeneration/generate_arc_audio.py`(rewritten), `scripts/dialogue/{ConversationArc.cs,executables/DialogueExecutable.cs}`, `scripts/audio/BroadcastAudioService.cs`, deleted `scripts/dialogue/{AudioDialoguePlayer,IDialoguePlayer}.cs`(+uids, +test), `tests/{KBTVTestClass.cs,unit/dialogue/{ConversationArcTests.cs,ArcSchemaValidationTests.cs(new),ArcDialogueLineMoodTests.cs(new)}}`, docs `{ui/CONVERSATION_ARC_SCHEMA.md,ui/CONVERSATION_ARCS.md,audio/VOICE_AUDIO.md,tools/AUDIO_GENERATION.md,tools/TOOLS.md}`, deleted `Tools/AudioGeneration/extract_arc_ids.py`, `missing_audio.txt`.
- Known issue (not fixed this session): ~26 legacy assertions record failures without failing (`FailOnRecordedFailures` defaults false for old suites) - triage as a dedicated debt pass, then flip the default to true.
- Next Steps: (1) in-editor sanity: play a show, confirm Vern's lines vary in tone with his VIBE mood (the new selection); (2) open Godot once so the 16 mp3s added earlier import; (3) commit when asked; (4) optional: delete the ~101 legacy broadcast mp3s; (5) add new arcs by following docs/ui/CONVERSATION_ARC_SCHEMA.md checklist - everything downstream is automatic.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Fix broadcast bug: missing topic-switch arc audio (`GD.Load` error spam for `Ghosts/topic_switch_ghost/ufos_fake_*.mp3`) + Vern stuck in infinite BetweenCallers↔Conversation loop (trigger suspected: caller dropped during ad break). **Root causes**: (1) `DialogueExecutable` built audio paths from the arc's JSON `topic` folder, but audio (generator convention) lives under the **line-id prefix** topic folder (`ufos_...` → `UFOs/...`) — and audio for `topic_switch_ghost`, `ufo_cryptid_switch`, `business_traveler`, `truck_driver`, `ufo_ferry_captain` was missing entirely; (2) an on-air caller with no matching arc was never released (`EndOnAir` only ran on Conversation-type completion) so `CreateConversationExecutable` fell to the Fallback line → BetweenCallers → same stuck caller forever. **Fixes**: new `ArcAudioTopics.GetTopicFolder` (line-id-prefix rule, mirrors `generate_arc_audio.py`); per-line path derivation + `FileAccess.FileExists` guards (silent `DEFAULT_LINE_DURATION` hold instead of `GD.Load` spam) in `DialogueExecutable`/`BroadcastExecutable`; stuck-caller release; 3-consecutive-fallback-cycle recovery guard (EndOnAir + clear pending flags + DeadAir); `BroadcastStateManager` `_ExitTree`/`PublishStateChangedEvent` guarded when DI never provided EventBus (fixed pre-existing `SetState_AdBreak` test failure); generated 166 missing mp3s via ElevenLabs tool (topic_switch_ghost 41, ufo_cryptid_switch 41, business_traveler 41, truck_driver 41, ufo_ferry_captain 84) — all arcs' audio now present (full inventory scan green). **Status: Completed (code) — build green; full suite 598/12 (baseline 596/13; +6 new tests pass, 1 pre-existing failure fixed, remaining 12 are pre-existing DI-harness failures: AdManager×4, AudioDialoguePlayer×3, GameStateManager×1, LoadingScreen×2, TranscriptManager×2); in-editor verification + commit pending**
- Key invariant (documented in ArcAudioTopics + tests): audio topic folder = first token of the line id (`ufo|ufos→UFOs, ghosts→Ghosts, cryptid(s)→Cryptids, conspiracies→Conspiracies`). JSON `topic`/`claimedTopic` do NOT predict it (claims_ufos: topic Cryptids, claimed UFOs, audio under Cryptids).
- Files Modified: `scripts/dialogue/{ConversationArc.cs,ArcAudioTopics.cs(new),BroadcastStateMachine.cs,BroadcastStateManager.cs,executables/DialogueExecutable.cs,executables/BroadcastExecutable.cs}`, `Tools/AudioGeneration/generate_arc_audio.py`, `tests/unit/dialogue/{ConversationArcTests.cs,BroadcastStateMachineTests.cs}(new)`, ~166 new untracked `.mp3` under `assets/audio/voice/.../UFOs/{topic_switch_ghost,ufo_cryptid_switch,business_traveler,truck_driver,ufo_ferry_captain}/` (need Godot import + `git add`).
- Known issue (not fixed this session): `AsyncBroadcastLoop._lastInterruptionReason` is stale when `BroadcastStateManager` publishes `BroadcastInterruptionEvent` directly on the bus (T5 BreakImminent / ShowEnding) instead of via `InterruptBroadcast()` — the loop's OCE catch misclassifies those; the fallback guard now contains the damage.
- Next Steps (in-editor): run a show with a topic-switch caller — console should be error-free and audio audible (open Godot first so new mp3s get `.import`ed); verify no between-callers loop after dropping a caller mid-break; commit when asked (include the new audio).

---

## Previous Session (completed)

**Branch**: develop
**Task**: Soundboard Round 16 — make the four broadcast buttons actually work. **Root cause** of "nothing happens": `Soundboard3D` resolved all services once in `_Ready()`, which runs BEFORE `Main._Ready → ServiceProviderRoot.Initialize()` registers the DI resolvers → every service was null forever. Now: lazy re-resolve (`ResolveServices()` from `_Process`/`TapButton`, one-time event subscribe). Feature pass per user spec: **Music** flashes when the break window opens and starts the LOOPING `break_transition_*` bed on the board's channel-3 strip (SFX bus — silent until the player fades it up); **Ads** flashes at T-0 and stays pressable until the break rolls (queue decoupled from the bed); **Delay** white-yellow flashes while a caller curses → press hangs the caller up (repository-level) and releases the bleep delay so Vern's cursed line plays; **Drop** drops the on-air caller AND (new) clears the curse QTE (every press publishes `SoundboardButtonPressedEvent`). Curse-window expiry now always drops the caller (the old `_callerDroppedDueToCursing` flag made that code dead). Button hover uses the knob/fader-style subtle cap brighten (was harsh lamp-face `LedSelected @ 2.5×`); `Flashing` = breathing white-yellow lamp pulse + growing/shrinking `BtnGlow_*` halo quad. Bleep + UI sounds moved to Master (off the board-gated SFX bus). **Status: Completed (code) — build green (0 err/6 warn), full suite 591/13 (baseline 590/13, +1 net new test; same 13 pre-existing DI-harness failures), `SoundboardButtonStateTests` 8/0; in-editor verification + commit pending**
- User-confirmed decisions: no 7th requirement (message truncated); Ads = flash at T-0, press within grace (no flow change — window stays open until the break actually rolls); bed loops until StartBreak stops it; bleep + UI → Master bus.
- Files Modified: `scripts/world3d/{Soundboard3D.cs,SoundboardButtonState.cs,SoundboardButtonPressedEvent.cs}`, `scripts/audio/{BroadcastAudioService.cs,UIAudioService.cs}`, `scripts/ads/AdManager.cs`, `scripts/ui/LiveShowFooter.cs`, `scripts/dialogue/executables/CursingDelayExecutable.cs`, `tests/unit/world3d/SoundboardButtonStateTests.cs`, `docs/systems/SOUNDBOARD_DESIGN.md`, `SESSION_LOG.md`.
- Blockers: none.
- Next Steps (in-editor): open board during a show — (1) Music button flashes at break-window open; press starts the (silent) bed, fade channel-3 up to hear it looping; (2) Ads flashes at T-0, press queues; ads audio comes through the same strip; (3) curse a caller (low odds — spam it or raise CurseRisk) → Delay flashes white-yellow, press drops caller + Vern's scold plays, no $100 fine; let it expire → fine + auto-drop; (4) Drop hangs up + Vern's dropped line; (5) hover on caps reads subtle (same lift as knobs); (6) `BtnGlow_*` quads sit flush over the caps under the oblique camera (tune `ButtonGlowLift` if floating/clipping); intro/return bumpers now fade with the channel-3 strip too (verified intended). Commit when asked.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Soundboard Round 15 — wire the four broadcast buttons end-to-end. Music plays a random intro bumper on the Music bus; Delay is a QTE that clears a live-curse penalty (no caller drop) as an alternative to Drop; Ads/Drop/Music/Delay get press + queue/active/curse flash-and-latch feedback on their `BtnLamp_*` faces; a live info screen is rendered onto `ScreenFace` (SubViewport→texture), replacing the 2D `SoundboardOverlay` (deleted; mixer driver relocated to `World3D`). **Status: Completed — build green (0 err/6 warn), full suite 590/13 (prior 583 baseline +7 new `SoundboardButtonStateTests`, same 13 pre-existing DI-harness failures); superseded by Round 16 behavior pass (null-DI fix, channel-3 bed/ads routing, Delay drops caller)**
- User-confirmed decisions: Music = random **intro** bumper (Music bus, dedicated non-looping player); Delay = **QTE-clear only** (clears the FCC fine alongside Drop; does NOT keep the caller — a curse already ends the caller's segment pre-censored, so a true "keep caller" would need a DialogueExecutable redesign — deferred); info moves to the **3D screen**, **drop** the 2D overlay; **everything** incl. screen content is in scope this pass.
- Wiring: `Soundboard3D.InvokeButtonAction` split by button (Music→bumper, Delay publishes `SoundboardButtonPressedEvent`); `_curseActive` mirror via `BroadcastInterruptionEvent`/`CursingTimerCompletedEvent` subs (+ `_ExitTree`); `UpdateButtons`/`ApplyLampLook` drive lamps via new pure `SoundboardButtonState` resolver (gated/queued/active/ready/urgent/pending + press-flash + hover); `BuildScreen`/`UpdateScreen`/`ComposeScreenText` render the info screen on `ScreenFace`. `LiveShowFooter` subscribes the button event → `StopCursingTimer()` on Delay/Drop during a curse. `World3D._soundboardDriver` now owns the driver (attaches the mixer + hands to monitor/board); `SoundboardOverlay.cs`(+uid) deleted; stale crefs repointed.
- Files Modified: `scripts/world3d/{Soundboard3D.cs,SoundboardButtonState.cs(new),World3D.cs}`, `scripts/ui/{LiveShowFooter.cs,SoundboardOverlay.cs(deleted)}`, `scripts/audio/SoundboardTargetGenerator.cs` + `scripts/monitors/SoundboardMonitor.cs` (cref), `tests/unit/world3d/SoundboardButtonStateTests.cs(new)`, `docs/systems/SOUNDBOARD_DESIGN.md`, `SESSION_LOG.md`.
- Blockers: none.
- Next Steps (in-editor): confirm intro bumper is audible on the Music bus; press Delay during a caller curse clears the FCC-fine countdown (bleep keeps running its 20s as today); Drop still hangs up; Ads lamp latches queued/active; `ScreenFace` text legible and oriented correctly under the yaw-180 board (flip the viewport label if inverted — comment marked in `BuildScreen`). Commit when asked.

---

## Previous Session (completed)

**Branch**: develop
**Task**: Soundboard Round 14 — board redesign: channels 1-3 stay (Vern/Caller/Ads-Music), channel 4 becomes a linked stereo master pair, VU meter above master, channels 5-8 replaced by a 2x2 broadcast button grid (Music/Delay/Ads/Drop) with an info screen above. **Status: Completed (incl. 14b orientation fix) — build green, full suite 583/13 (same pre-existing DI baseline, +1 new test); in-editor visual/interaction verification + commit pending**

- **Round 14b (after first in-editor look):** the yaw-180 `SoundBoard` instance made the authored face read mirrored (strips right, buttons left) with the flat labels upside down. Fixed model-only: strips authored right-to-left (`STRIP_X0=+0.45`, `STRIP_PITCH=-0.105`), button grid/screen moved to authored −X, `BtnLabel_*` spun 180°, VU meter moved above the master strip, screen halved in height (bezel 0.115→0.060, face 0.046). Part names/axes untouched → **zero C# changes**. Verified with an in-game-angle Blender render (strips Vern→Master left-to-right, readable buttons, matches the user mock). Docs updated (§7 authoring-orientation note, R14 changelog).

- User-confirmed decisions: strip order **1=Vern, 2=Caller, 3=Ads/Music**; master pair **linked** (one value, `state.Fader`, drives both caps — no DSP change); **Ads → `AdManager.QueueBreak()`, Drop → `InterruptBroadcast(CallerDropped)`** wired now, **Music/Delay publish `SoundboardButtonPressedEvent`** for follow-up wiring; button flash effects + screen look = separate work.
- `Tools/modelgen/soundboard.py` rewritten: 4 strips + single VU meter + `Screen bezel`/`ScreenFace` + 4 `Button_*` caps (parented `BtnLamp_*` face + `BtnLabel_*` text); `MasterKnob`/columns 4-7 deleted. Regenerated GLB (blender 5.2, validated; preview `docs/art/model_previews/soundboard.png` matches the user mock; GLB node names + parenting verified via re-import).
- `SoundboardPhysicalLayout.cs`: new slots (strips 0-2 = Vern/Caller/Ads, `FaderCap_3L/3R` master pair), `IdleLamps` removed, new `SoundboardButton` enum + `ButtonSlots`/`ButtonSlotFor` + `ButtonPressDepth`.
- `SoundboardControlApplier.cs`: `Master` → `MasterLeft`/`MasterRight` (both map to `state.Fader`); `SoundboardGlow` unaffected (defaults to None channel).
- `Soundboard3D.cs`: button subsystem (`BuildButtons`, `TapButton` press animation, `SetButtonHover`, `ButtonFromBody`, per-button lamp materials, `SetButtonLight`/`ClearButtonLight` flash-effect hook), Ads/Drop actions + event publish for Music/Delay, `UpdateLeds`/colliders/hover remapped to new lamp names.
- `World3D.cs`: `RaycastBoardControl` → `RaycastBoardBody`; taps resolve control **or** button; button hover cleared over GUI.
- New `scripts/world3d/SoundboardButtonPressedEvent.cs`.
- Tests: `SoundboardPhysicalLayoutTests` remapped (+ `ButtonSlots_AreComplete`), `SoundboardControlApplierTests` + `MasterPair_IsLinkedThroughOneFaderValue`, Master→MasterLeft in applier/glow/target-generator suites. All soundboard suites green; full run 583/13 == baseline (verified via stash: clean tree fails the same 13 DI-harness tests).
- Docs: `docs/systems/SOUNDBOARD_DESIGN.md` §7 (parts, axis map, control-slot table, button grid table, LEDs, interaction) + Round 14 changelog + §9 halo tuning.
- Blockers: none.
- Remaining: in-editor run — open the soundboard, confirm drag on all 10 controls + lamp colors, button press/hover glow, Ads queues a break, Drop hangs the caller, Music/Delay log events; then define button light effects + screen content (separate work). Commit when asked.

---

## Previous Session (completed)

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
