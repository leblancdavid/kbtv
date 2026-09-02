# KBTV PixelLab Prompt Rules (Authoritative)

This is the **only source of truth** for generating pixel art with PixelLab MCP in KBTV. If a rule here conflicts with any other doc, this doc wins.

- **Style spec**: [`docs/art/ART_STYLE.md`](ART_STYLE.md) — what the art must look like (palette, proportions, prop categories).
- **Tool mechanics**: [`docs/art/PIXELLAB_MCP_GUIDE.md`](PIXELLAB_MCP_GUIDE.md) — which tools exist, costs, known MCP restrictions (base64 truncation, aspect limits). No prompt guidance there anymore.
- **This doc**: which tool to use, the exact prompt + parameters, and how many generations an asset is allowed to burn.

If a fresh discovery invalidates a rule here, update THIS file (and SESSION_LOG), not ART_STYLE.

---

## 1. Tool Decision Matrix

| I need this asset | Use this tool | Cost | Notes |
|---|---|---|---|
| Single prop, any aspect | `pixellab_create_image_pro` | 20-40 gen | **DEFAULT for every prop (user directive 2026-08-28 — pro render quality).** Palette-locked via style swatch (§6). Use `pixellab_create_image_pixflux` (1 gen) only for cheap quick-proxies or tiny sprites when budget is tight. |
| Hero prop / wide flat prop | `pixellab_create_image_pro` | 20-40 gen | Full-width clause mandatory for wide flats (see §7). Returns 64/16/4/**1** candidate(s) by size — wide flats (>170px long side) return a single image. |
| Small sprite (≤32px final) | `pixellab_create_image_pixen` | 1 gen | Cleaner on tiny sprites. |
| Character (directional / animated) | `pixellab_create_character` (+ `animate_character`) | varies | Only for in-world NPCs, not props. |
| Caller silhouette (single pose) | `pixellab_create_image_pixflux` | 1 gen | Transparent, one shadow figure. |
| Edit an existing png | `pixellab_edit_image_pixen` (1 gen) or `pixellab_edit_image` (20-40 gen) | 1 / 20-40 | Pixen for single small frame; pro only for batch/consistent multi-frame edits. |
| Lock/enforce palette | `pixellab_reduce_colors` (w/ KBTV palette swatch) | 0.1 gen | **Run on every accepted sprite.** |
| Clean up pixel grid / edges | `pixellab_correct_pixelart` | 0.1 gen | Fix stray pixels, AA, jaggies. |
| Upscaled art → native grid | `pixellab_unzoom_image` | 0.1 gen | Run before passing foreign art to any style/ref input. |
| Wall/floor structural atlases | `pixellab_create_topdown_tileset` / `building_kit` | varies | **Structural atlases are locked** (deliberately upscaled, tiling-safe). Do NOT regenerate without user approval. |
| Check budget | `pixellab_get_balance` | free | Run before a generation batch. |

### Cost floor protocol
- 0.1-gen utilities (`reduce_colors`, `correct_pixelart`) are essentially free — **always** run them on accept/resize.
- 1-gen tools are the default. 20-40 gen tools are reserved for candidate pick sets and are **one call per asset**.
- Never chain pro tools on the same asset (max **one** pro call per asset per sitting).

## 2. Canonical Parameter Block

Every `create_image_pixflux` / `create_image_pixen` / `create_image_pro` call uses exactly:

```
view: "high top-down"
width / height: 2x the final target dims (see dims table)
no_background: true
outline: "single color outline"
shading: "basic shading"
detail: "medium detail"
text_guidance_scale: 10-12
color_image_base64: <KBTV palette swatch, 264 chars — below>
seed: <any integer — RECORD IT>
```

**Palette swatch (264-char base64 — the only reliable palette lock).** Generate the 16×16 swatch with the script in `PIXELLAB_MCP_GUIDE.md` §1, or reuse this decoded string exactly:

```
iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAABZSURBVDhPY+DlFfovL6/2X0vX7L+Vrcd/LSXp/1HBDv+r8kP/W3VV/F9kZfX/xKyo/7jUMRCrEJc6BmIV4lLHQKxCXOoYiFWISx0DsQpxqRsNxNFAHByBCABw+NZc4D+vIwAAAABJRU5ErkJggg==
```

### Generation dimension rules
- **Generate at 2× the final target, then nearest-neighbor downsample.** Never upscale AI output.
- Final targets match the post-migration wired dims in §7 / the dims table.
- Keep generation aspect between 1:1 and 3:2. 3:1+ is unreliable — split the asset.
- Square-only tools (`create_1_direction_object`, `create_8_direction_object`) are **NOT** for KBTV props — non-square content gets rotated. Use pixflux.

**Rule:** `width`/`height` in the tool call is the GENERATION size (2× target), not the final size.

### Pro method (the default — §3 templates + style swatch)

`create_image_pro` has **no** `view/outline/shading/detail` params — style is carried entirely by the description (template §3 verbatim) + the style swatch. Every pro call uses:

```
description:   <template §3 verbatim, {subject} swapped>
width / height: final target for wide flats; 2× target otherwise
no_background: true                      (pro default, keep it)
style_image_base64: <KBTV palette swatch, 264 chars>
style_copy:    ["color_palette"]         (palette lock — do NOT copy outline/shading)
seed:          <any integer — RECORD IT>
```

- Wide flat assets (desk, window atlas): generate at **final** dims with the edge-to-edge clause in the description (see §7).
- Pro accepts one image input; use the palette swatch, never a content reference, for KBTV props.

---

## 3. Prompt Templates

Use the correct template **verbatim**, only swapping the `{subject}` slot. The trailing suffix carries all style enforcement — do not trim it.

### 3a. Universal front-facing prop (ALL standing/vertical furniture)

Default for cabinets, bookcases, audio racks, monitors, boards, consoles, small props.

```
{subject}, front-facing view from slightly above with vertical top-down perspective,
the FRONT FACE is dominant showing the main details with only horizontal and vertical edges no diagonals,
a thin TOP SLIVER is visible at the top showing the top surface,
subject fills entire canvas with minimal transparent padding,
dark noir palette, charcoal black outlines, transparent background,
NO white outline, NO white border, NO white rim, NO light-colored border around the subject,
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath,
NO ambient occlusion, NO brown shadow gradient beneath,
single color black outline, crisp pixels, no anti-aliasing, pixel art,
16-bit retro game asset
```

### 3b. Table / horizontal surface (top dominant + stubby legs)

For desks, tables, consoles with items on top.

```
{subject}, viewed from slightly above showing both TOP SURFACE and FRONT FACE clearly,
the wide flat top takes up the upper portion as a flat horizontal plane items sit on,
narrow front face visible below with only horizontal and vertical edges,
the legs are VERY SHORT STUBBY legs about half the size of a person,
the overall is LOW and SHORT,
subject fills entire canvas with minimal transparent padding,
dark noir palette, charcoal black outlines, transparent background,
NO white outline, NO white border, NO white rim, NO light-colored border around the subject,
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath,
NO ambient occlusion, NO brown shadow gradient beneath,
NO oval blob, NO ground beneath, NO floor visible,
the bottom of the legs is the actual bottom edge, not a shadow fade,
single color black outline, crisp pixels, no anti-aliasing, pixel art,
16-bit retro game asset
```

> **Bare tabletop**: the template assumes items sit on top. For an empty surface, put it in `{subject}` explicitly — "completely EMPTY tabletop, NOTHING on top, no mug, no papers, no phone, no computer, no objects of any kind, perfectly bare". Omitting it lets the model decorate the top.

### 3c. Wall-mounted item (flat face-on)

Posters, wall clocks, signs.

```
{subject}, flat face-on view, dark noir palette, charcoal black outlines,
subject fills entire canvas with minimal transparent padding,
transparent background, NO shadows, NO ground shadow, NO drop shadow,
NO contact shadow underneath, single color black outline,
crisp pixels, no anti-aliasing, pixel art, 16-bit retro game asset
```

### 3d. Chair — north-facing back view

Only for the computer chair against the north wall.

```
back view of an office chair, viewed from BEHIND showing the BACK of the chair,
tall charcoal mesh backrest with lumbar support curve,
the BACKREST is the dominant feature of the sprite,
five-star wheeled base with small caster wheels visible at the bottom,
charcoal metal post connecting backrest to base,
a thin TOP SLIVER visible at the top showing the top edge of the backrest,
NO armrests, just the backrest and wheeled base,
subject fills entire canvas with minimal transparent padding,
dark noir palette, charcoal black outlines, transparent background,
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath,
single color black outline, crisp pixels, no anti-aliasing, pixel art,
16-bit retro game asset
```

### 3e. Caller silhouette (front, single figure)

For callers / placeholder silhouette props.

```
{subject}, single dark silhouette figure, front-facing, pixel art,
silhouette reads as one solid dark shape with a few interior detail pixels max,
dark charcoal (#1f1f26) fill, transparent background,
clean crisp edges, no anti-aliasing, no shadows, no floor, no ground,
subject fills entire canvas with minimal transparent padding,
16-bit retro game asset
```

### 3f. Subject-swap rules (consistency)

- **One template per asset category; only `{subject}` changes.** Never free-write a prompt from scratch.
- Parallel structure: same material words, same accent-color words, same scale hints across a set.
- Describe features by **count** ("two columns of audio gear", "4 drawers", "3 screens"), not adjectives.
- Dark charcoal monochrome for studio tech (`NO wood texture, NO warm brown colors`).
- Wood warm tones only for homey props (bookcase, home desk).
- Phosphor green accents = `#3a8a78` tone described as "phosphor green LED" (`NO saturated #00ff44`).

## 4. Dimension Table (final target → generation size)

| Final | Generate at | Note |
|-------|-------------|------|
| 16×16 | 32×32 | mug, ashtray, papers |
| 24×24 | 48×48 | small props |
| 32×32 | 64×64 | monitor, station, small cabinets |
| 32×48 | 64×96 | silhouette props |
| 32×56 | 64×112 | tall cabinets (pre-2× refs) |
| 48×32 | 96×64 | tables, wall boards |
| 64×64 | 128×128 | shelf, board, chair |
| 64×80 | 128×160 | chair (north) |
| 80×48 | 160×96 | round table |
| 96×48 | 192×96 | phone board |
| 96×128 | 192×256 | bookcase |
| 128×48 | 256×96 | on-air sign |
| 256×96 | 256×96 | studio table (wide desk — generated at final, see §4 note) |
| 128×128 | 256×256 | audio cabinet |

**Post-migration wired dims (current build)** — file a prop is actually used at:
`audio_cabinet` 128×128 · `storage_shelf` 128×128 · `phone_board` 96×48 · `sound_board` 64×64 · `computer_station` 64×64 · `computer_chair` 64×80 · `speaker_stand` 45×132 (cropped to content 2026-08-28, was 96×192) · `bookcase` 96×128 · `round_table` 160×96 · `studio_table` 256×96 (pro bare/transparent rework 2026-08-28; desktop spans full width) · `on_air_sign` 128×48 · wall window atlas 224×128 (7×32px `Hframes=7`) · small props (mug/ashtray/papers/phone/monitor/clock/poster/filing_cabinet/cabinet_tall) still 16–32px.

Never 2× upscale a texture that is already 2× (4× total) — items taller than the room wall (~128px) fly up behind the north wall and vanish.

**Wide flat assets (desk, window atlas) are generated at final dims, not 2×.** The 2× dims (512×192, 448×256) exceed the 16:9 pro-canvas cap and pixflux's 400 px/side cap. Ask the model to make the surface span edge-to-edge, then verify with the alpha-bbox probe (content width must ≈ canvas width).

**NEVER locally widen/extend generated art** — mirror-tiling a flat prop's edge band to fake extra width was built once (2026-08-28) and rejected in QA (visible banding). If a wide asset comes back narrow, regenerate it; do not stretch, tile, or pad it locally.

---

## 5. Keyword Rules

### Always present (in every prompt)
`crisp pixels`, `no anti-aliasing`, `transparent background`, `single color black outline`, `dark noir palette`, `subject fills entire canvas with minimal transparent padding`, `NO shadows` + the full no-shadow block from the template.

### Never use (structural failure triggers)
- ❌ `isometric` — pulls the model to 30° depth
- ❌ `axonometric`, `rotated cube`, `3D`, `pseudo-3D`
- ❌ Saturated hex anchors in the description: `#00ff44`, `#ff4444`, `#00ffff`, `#ffaa00` — the palette swatch is the color authority; hexes in text get treated as anchors
- ❌ `bright`, `vibrant`, `saturated`, `high contrast` color phrases
- ❌ `top-down`/`low top-down` words in the DESCRIPTION text (the `view` parameter carries this; the words mislead the text model)

### View parameter
ALL KBTV props use `view: "high top-down"`. `"side"` and `"low top-down"` are forbidden for props — they produce isometric/oblique or face-on-with-no-depth. The only exception is a deliberate flat wall-sign/portrait, which still uses `high top-down` + the flat face-on template.

---

## 6. Iteration Budget Protocol (anti-credit-burn)

Hard caps per asset. This is the rule that stops re-roll spirals.

| Step | Action | Spend |
|------|--------|-------|
| 1 | ONE `create_image_pro` batch (palette swatch, template §3) → user picks a winner | 20-40 gen |
| 2 | User visual check. Pass → STOP. Fail → step 3. | 0 |
| 3 | Diagnose via §7, edit the `{subject}` / add the clause, then ONE more pro batch | 20-40 gen |
| 4 | Two failed pro batches in a sitting → **STOP SPENDING.** Defer the asset (placeholder) and move on. | 0 |

The 1-gen tools are now the **quick-proxy fallback**, not step 1: use `pixellab_create_image_pixflux` to sanity-check a subject/wording before committing a 20-40 gen pro batch on it. Pixen stays for ≤32px sprites.

### Hard rules
1. **No 3× consecutive 1-gen re-rolls with the same prompt.** A new seed fixes minor glitches only; it never fixes a structural failure (wrong view, baked shadows, saturated palette).
2. **Never two pro calls on the same asset spec in a sitting.** Pro is winner-pick, not a re-roll wheel.
3. **A user-requested pro re-roll that changes the spec** (e.g., "same look but bare + transparent") is a NEW asset task, not a blind re-roll — one pro call per changed spec is allowed. Log it as its own row in §8.
3. If the asset is a **hero prop** (large, in the player's face): skip step 1, go straight to a pro batch (step 3) so the user reviews candidates instead of one image at a time.
4. Every accepted sprite: `reduce_colors` with the KBTV palette swatch (0.1 gen), then `correct_pixelart` if edges are soft (0.1 gen), then nearest-neighbor resize.
5. Run `pixellab_get_balance` before any batch. If under ~120 generations, stop pro batches until replenished.

---

## 7. Failure-Mode → Fix Table

When a generation is rejected, look up the symptom and change ONLY the listed thing. Do not re-roll blindly.

| Symptom | Root cause → Fix |
|---------|------------------|
| Front face has diagonals / looks isometric | `view` not `high top-down`, or "isometric" snuck into description → restore view + template §3a |
| Oblique/cabinet side depth (3/4 rotated) | `view:"side"` or `"low top-down"` → switch to `"high top-down"` |
| Flat top-down plan view (no front face) | Template missing "FRONT FACE is dominant" → use §3a verbatim |
| Head-on portrait (no top sliver) | Template missing "thin TOP SLIVER" → use §3a verbatim, don't remove the depth sentence |
| Baked ground shadow / oval blob under prop | Template lost the NO-shadow block → restore template; NO-shadow block must be literal |
| Saturated colors (hot green/red) | Palette swatch missing or truncated → re-pass swatch; remove saturated hex words from description |
| Subject tiny / floating in canvas | Missing "fills entire canvas" → add; or aspect too extreme → move toward 1:1-3:2 |
| Table legs way too long | Used §3a instead of §3b → swap to the stubby-legs template |
| Subject rotated 90° to fit | Aspect too extreme → square / nearer dims, or split asset |
| Blurry or doubled outlines | `outline` param alone (soft guidance) → also put "single color black outline" in the description text |
| White box / non-transparent bg | `no_background` not true → set `no_background: true` |
| White outline / light rim around an otherwise good sprite | The pro painter adds a white pixel-art rim by default even with `no_background: true` → forbid it in the description ("NO white outline, NO white border, NO white rim, NO light-colored border around the subject"); verify near-white pixels ≈ 0 with the probe |
| Pro comes back narrow/centered on a wide flat asset | "bare"/"empty" phrasing alone shrinks the subject (seed 4759 → 176px) → add the full-width clause: keep an accent (e.g., LED strip) + "the desk surface EXTENDS ACROSS THE ENTIRE WIDTH of the canvas from left edge to right edge with NO empty transparent gaps on either side" (seed 4760 → 256px) |
| Unwanted objects/items baked onto a tabletop | Template 3b implies items; for a bare surface, say so in `{subject}` ("completely EMPTY tabletop, NOTHING on top, perfectly bare") |
| Item ascends above north wall when placed | Texture taller than wall (~128px) after upscale → never 2×-upscale a 2× texture; regenerate at wall-safe height |

---

## 8. Per-Asset Repro Log

Every generated asset gets a row so any asset can be reproduced or varied consistently. Keep it in SESSION_LOG.md "Work Done".

```
asset:            audio_cabinet
tool:             create_image_pro
view / dims:      high top-down / 256×256
text_guidance:    10
seed:             12345
prompt:           3a (universal) + subject "tall audio equipment rack, two columns..."
job_id:           0423c67f
candidate wired:  cand_0
status:           accepted
```

Rules: same template + same seed = deterministic style base. Vary `{subject}` (not the template) and the seed to explore a set. Record the job id so a failed job can be traced before re-spend.

---

## 9. Prioritized Generation Plan + Budget

Order matters: finish what exists before minting new art.

### Tier 0 — Wire existing candidates (0 generations)

Contact sheets already in `assets/props_samples/`. User picks a winner from the contact sheet; wire the file into `ControlRoom.cs` / `StudioRoom.cs`.

| Asset | Candidates | Wired to | Template |
|-------|-----------|----------|----------|
| `audio_cabinet` | 4 (v1) + 4 (v2) | control room north wall | §3a |
| `computer_chair` (north) | 16 | control room | §3d |
| `computer_station` | 16 | studio table | §3a |
| `sound_board` | 16 | studio table | §3a |
| `phone_board` | 4 | control room table | §3a |

Acceptance: front face flat, fills canvas, no baked shadow, noir palette, reads at game size. After accept: `reduce_colors` + resize per §4.

### Tier 1 — Small tabletop props at full detail (~6 gens)

Current versions are 16-32px stubs. Regenerate at 2× gen size for the 2× world. 1-gen pixen/pixflux each (small sprites don't need pro; use §6 quick-proxy), pro batch only if rejected.

| Asset | Final | Gen at | Template |
|-------|-------|--------|----------|
| `coffee_mug` | 16×16 | 32×32 | §3a (small) |
| `ashtray` | 16×16 | 32×32 | §3a (small) |
| `papers` | 16×16 | 32×32 | §3a (small) |
| `phone` (rotary) | 32×32 | 64×64 | §3a |
| `monitor` (CRT) | 32×32 | 64×64 | §3a |
| `coffee_station` | 32×32 | 64×64 | §3a |

### Tier 2 — Missing functional props (~5 gens)

| Asset | Final | Gen at | Template |
|-------|-------|--------|----------|
| `dead_air` sign (blinking) | 32×32 | 64×64 | §3c (flat sign) |
| `evidence_cabinet` (locked) | 32×48 | 64×96 | §3a |
| `phone_bank` refresh | 48×32 | 96×64 | §3a |
| `boom_mic` refresh | 24×24 | 48×48 | §3a |
| `poster` + `wall_clock` refresh | 48×32 / 32×32 | 96×64 / 64×64 | §3c |

### Tier 3 — Callers + Vern moods (~9 gens)

- **Callers** (1-gen silhouette each, §3e): conspiracy_theorist, nervous, confident_whistleblower, aggressive, shy, excited, panicked, mysterious_female — 8 new; authority/elderly/mysterious/sketchy exist.
- **Vern moods**: use `pixellab_edit_image_pixen` (1 gen each) on `vern_portrait.png` — motivated, tired, suspicious, pleased. Do NOT use `create_character_state` (20-40 gen/state) for portraits.

### Budget summary

| Tier | Spend | Purpose |
|------|-------|---------|
| 0 | 0 gens | Wire existing winners |
| 1 | ≤6 gens (+0.6 reduce_colors) | Small tabletop detail |
| 2 | ≤5 gens (+0.5) | Missing functional props |
| 3 | ~11 gens (+1.1) | Caller silhouettes + Vern moods |
| **Total** | **~22 gens** | Full sweep, one batch each |

Props above the small tier use the **pro method by default** (§6) — budget the pro cost (~20-40 gen per asset) accordingly; the summary above tracks the small-sprites fallback path only.

*Last updated: 2026-08-28 — pro method made the default for prop generation (user directive); added §2 pro-method block, §7 no-white/edge-to-edge failure rows, and the bare-tabletop clause to 3b. Previous update 2026-08-27 established this doc as the single authoritative prompt-rules source.*