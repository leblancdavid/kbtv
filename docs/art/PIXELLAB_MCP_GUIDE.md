# PixelLab MCP Integration Guide

## Overview

This guide covers how to use the PixelLab MCP server with opencode for AI-powered pixel art generation in the KBTV project. It is the **practical companion** to [`docs/art/ART_STYLE.md`](ART_STYLE.md) — read that first for the visual rules, then come here for the workflow.

---

## What is PixelLab MCP?

PixelLab MCP is a Model Context Protocol server providing AI image generation. opencode connects to it via MCP and exposes its tools as native tool calls.

---

## Available Tools (Actual MCP Surface)

These are the tools actually available in our MCP connection. Use these — older docs may reference `pixellab_generate` / `pixellab_refine` which **do not exist** in this MCP version.

### Image Generation

| Tool | Cost | Use for |
|------|------|---------|
| `pixellab_create_image_pixflux` | 1 gen | **Default for KBTV props.** Takes explicit `width` and `height` for non-square targets. ~10-40s. |
| `pixellab_create_image_pixen` | 1 gen | Cleaner small sprites (≤32px). Strict 256×256 area cap. |
| `pixellab_create_image_pro` | 20-40 gen | Multiple candidates (4-64). Use only when you need variations. |
| `pixellab_edit_image` | 20-40 gen | Edit an existing PNG with text instruction. |
| `pixellab_edit_image_pixen` | 1 gen | Cheaper single-frame edit, 256×256 max. |
| `pixellab_inpaint_image` | 20-40 gen | Repair a specific region (white mask). |

### Object / Character / Tileset Generation

| Tool | Cost | Use for |
|------|------|---------|
| `pixellab_create_1_direction_object` | 20-40 gen | Single-view props with style chaining. **Square only.** |
| `pixellab_create_8_direction_object` | 20-40 gen | Rotatable characters/objects. **Square only.** |
| `pixellab_create_map_object` | varies | Props matching a map's art style. Requires existing map. |
| `pixellab_create_character` | 1-40 gen | Characters with multiple directions. v3 mode rotates a reference sprite. |
| `pixellab_create_tiles_pro` | varies | Hex/isometric/oblique tiles. |
| `pixellab_create_topdown_tileset` | varies | Wang tileset for top-down maps. |
| `pixellab_create_building_kit` | varies | Wall/floor/doorway vocabulary for buildings. |

### Animation

| Tool | Cost | Use for |
|------|------|---------|
| `pixellab_animate_character` | scales | Add walk/idle/etc to a character. |
| `pixellab_animate_object` | scales | Add animation to an object. |
| `pixellab_animate_image` | scales | Animate any loose PNG sprite. |

### Utility / Quality

| Tool | Cost | Use for |
|------|------|---------|
| `pixellab_correct_pixelart` | 0.1 gen | Sharpen pixel grid, fix edges. |
| `pixellab_unzoom_image` | 0.1 gen | Downscale upscaled pixel art back to native size. |
| `pixellab_reduce_colors` | 0.1 gen | Quantize to a target palette. Use to enforce palette across a set. |
| `pixellab_image_to_pixelart` | 1 gen | Convert non-pixel source art to pixel art. |
| `pixellab_get_balance` | free | Check generation credits remaining. |
| `pixellab_get_image` | free | Poll a queued job for results. |
| `pixellab_get_character` / `get_object` | free | Inspect generated assets. |

---

## PixelLab Restrictions & Quirks (CRITICAL — read before generating)

These are real restrictions we hit. **Skip at your own risk** — credits will burn.

### 1. Base64 Truncation (the #1 silent failure)

**The MCP client silently truncates inline base64 strings at ~600-800 chars.** Any `*_base64` parameter over that length gets cut off mid-stream, returning:
```
Image data looks incomplete — N base64 chars did not decode, but they START 
with a valid image header. This almost always means the value was TRUNCATED 
in transit
```

**Measured limits:**
- 6008-char base64 (`storage_shelf.png`, 4.5KB PNG) → **truncated, corrupted**
- 1084-char base64 (`cabinet_tall.png`, 1.6KB PNG) → **truncated to ~812 bytes, corrupted**
- 264-char base64 (16×16 palette swatch, 196 bytes PNG) → **survives intact**

**Workarounds (in priority order):**
1. **Use a tiny palette swatch (≤300 chars base64).** Generate a 16×16 PNG with just the KBTV noir palette colors, base64-encode it (~264 chars), pass as `color_image_base64` to `create_image_pixflux`. The model samples colors from this swatch instead of inventing saturated primaries. **This is the only reliable way to lock the palette.**
2. **Prefer URL parameters** when available (`color_image_url`, `style_image_url`). But you need a publicly accessible URL — there is no built-in upload tool in this MCP.
3. **Do NOT rely on `style_image_base64` with realistic-sized PNGs.** The KBTV anchor props (cabinet_tall 1.6KB, storage_shelf 4.5KB) all exceed the truncation limit as base64.

**Generation script for the palette swatch** (run once, save the file, reuse the base64 string forever):

```powershell
# PowerShell — KBTV noir palette swatch (16x16, ~264 chars base64)
Add-Type -AssemblyName System.Drawing
$bmp = New-Object System.Drawing.Bitmap 16, 16
$colors = @(
    [System.Drawing.Color]::FromArgb(255, 0x0d, 0x0d, 0x12),  # void
    [System.Drawing.Color]::FromArgb(255, 0x1f, 0x1f, 0x26),  # charcoal
    [System.Drawing.Color]::FromArgb(255, 0x2a, 0x2d, 0x36),  # cool shadow
    [System.Drawing.Color]::FromArgb(255, 0x3a, 0x3d, 0x48),  # slate
    [System.Drawing.Color]::FromArgb(255, 0x2a, 0x22, 0x1b),  # warm shadow
    [System.Drawing.Color]::FromArgb(255, 0x5a, 0x53, 0x40),  # wood mid
    [System.Drawing.Color]::FromArgb(255, 0x7a, 0x6f, 0x55),  # wood highlight
    [System.Drawing.Color]::FromArgb(255, 0x3a, 0x8a, 0x78),  # phosphor green
    [System.Drawing.Color]::FromArgb(255, 0xa2, 0x3a, 0x3a),  # noir red
    [System.Drawing.Color]::FromArgb(255, 0xc8, 0x9a, 0x5a)   # warm rim
)
$idx = 0
for ($y = 0; $y -lt 16; $y++) {
    for ($x = 0; $x -lt 16; $x++) {
        $bmp.SetPixel($x, $y, $colors[$idx % $colors.Length])
        $idx++
    }
}
$bmp.Save("kbtv_palette_swatch.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
$bytes = [System.IO.File]::ReadAllBytes("kbtv_palette_swatch.png")
$b64 = [Convert]::ToBase64String($bytes)
# Use $b64 as color_image_base64 parameter value
```

The 264-char base64 string of this swatch (stable across regenerations):
```
iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAABZSURBVDhPY+DlFfovL6/2X0vX7L+Vrcd/LSXp/1HBDv+r8kP/W3VV/F9kZfX/xKyo/7jUMRCrEJc6BmIV4lLHQKxCXOoYiFWISx0DsQpxqRsNxNFAHByBCABw+NZc4D+vIwAAAABJRU5ErkJggg==
```

### 2. View Parameter: Pick the Right One

`pixellab_create_image_pixflux` exposes three `view` enum values:

| `view` value | What you get | KBTV use? |
|--------------|--------------|------------|
| `"side"` | Eye-level flat-front elevation. Good base for **oblique/cabinet** when combined with oblique keywords. | ❌ Avoid — produces isometric/oblique, NOT what we want |
| `"high top-down"` | Steeper top-down (~35° above horizon). Shows TOP SURFACE prominently. | ✅ **Use for ALL KBTV props** — front face dominant with top sliver visible |
| `"low top-down"` | Gentler top-down (~20°). 3/4 view, all faces angled. | ❌ Avoid — produces isometric, not oblique |

**Bottom line:**
- **ALL KBTV props use `view: "high top-down"`** — this is the universal standard
- The result is a **front-facing** view (we see the front of the object) with **vertical top-down perspective** (we also see a thin sliver of the top)
- The FRONT FACE is the dominant feature (where the readable detail lives)
- A thin TOP SLIVER shows depth/bulk without taking up much of the sprite
- For tables, the top is more prominent (because items sit on tables)
- For other furniture (cabinets, bookcases, racks), the front face dominates and the top is a thin sliver

### 3. Aspect Ratio & Canvas Size

| Tool | Min | Max | Aspect |
|------|-----|-----|--------|
| `create_image_pixflux` | 16px | 400×400 or 16:9 (688×384) | Square or 16:9 OK |
| `create_image_pixen` | 16px | 256×256 (area ≤65,536px) | Strict, square preferred |
| `create_image_pro` | 16px | 512×512 / 688×384 | Aspect-gated |
| `create_1_direction_object` | 16px | 256×256 | **Square only** |
| `create_8_direction_object` | 24px | 168×168 | **Square only** |

**Pitfalls:**
- **Square tools rotate/stretch non-square content.** `create_1_direction_object` with a "32×56 cabinet" prompt → square sprite, AI rotates/curves the cabinet to fit. Use `create_image_pixflux` for non-square targets instead.
- **Generate OVERSIZE then crop/resize.** Target 32×56 → generate at 64×112 → resize DOWN with nearest-neighbor. Never upscale AI output — pixel art loses its grid.
- **3:1+ aspect ratios are unreliable.** Stick to roughly 1:1, 2:1, or 3:2.

### 4. Generation Cost Reality Check

| Tool | Cost | Notes |
|------|------|-------|
| `create_image_pixflux` | **1 generation** | Fast (~10-40s). Default choice. |
| `create_image_pixen` | **1 generation** | Slightly better quality on small sprites. |
| `create_image_pro` | **20-40 generations** | Returns 4-64 candidates. Expensive. |
| `create_1_direction_object` ≤42px | **64 candidates** | Review mode — pick frames. ≤85px: 16. |
| `create_8_direction_object` | **20-40 generations** | Always 8 directions. |
| `correct_pixelart` / `unzoom_image` / `reduce_colors` | **0.1 generation** | Essentially free. |

**Default strategy:** Use `create_image_pixflux` (1 gen) + the KBTV prompt template + `color_image_base64` palette swatch. Re-roll until acceptable. Don't jump to `create_image_pro` unless you need multiple candidates to pick from.

### 5. Common Failure Modes & Fixes

| Symptom | Cause | Fix |
|---------|-------|-----|
| Result is **isometric** (all 3 axes angled) when you wanted front-facing with top-down | `view: "low top-down"` | Use `view: "high top-down"` |
| Result is **oblique/cabinet** with side depth (like the old style) when you wanted front-facing | `view: "side"` | Switch to `view: "high top-down"` with the universal standard prompt |
| Result is a **side view** (eye-level) | `view: "side"` | Use `view: "high top-down"` |
| **Front face has diagonals** (the face that points at the camera is rotated/slanted) | AI drew an isometric or 3/4 view | Add explicit "FLAT front face, NO diagonals on the front face, only horizontal and vertical edges on the face that points at the camera" |
| **Bookcase looks like a cabinet** (depth on the right side) | Used cabinet template instead of universal front-facing template | Use the Cabinet / Shelf / Audio Rack / Bookcase template with `view: "high top-down"` and emphasize "FRONT FACE is dominant, a thin TOP SLIVER is visible at the top" |
| **Table has long legs** (extends way below) | AI rendered full-height furniture legs | Add "very short stubby legs about half the size of a person, NOT long, NOT tall, NOT skinny" |
| Result has **baked-in shadow** underneath | AI default behavior | Add explicit `NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath, the area beneath is pure transparent nothing` to prompt |
| Result has **saturated colors** (bright green/red) | Palette not enforced | Pass the 16×16 palette swatch as `color_image_base64` |
| **Subject rotated 90°** to fit canvas | Aspect ratio too extreme | Use square, or use `create_image_pro` with explicit `width`/`height` |
| **Outlines missing or doubled** | `outline` param is soft guidance | Specify "single color black outline" in description text too, not just the parameter |
| **Truncated base64 corruption** | MCP client cut off your base64 | Switch to `color_image_base64` (≤300 chars), or upload to public URL |
| **"No candidates match my style"** | Style reference too different | Loosen `style_copy` array, use a closer anchor, or drop the style reference |
| **Background not transparent** | Default behavior | Pass `no_background=true` |
| **Subject too small in canvas** | Prompt didn't emphasize scale | Add "subject fills entire canvas with minimal transparent padding, edges touch edges of canvas" |
| **Top surface not visible** | Used `view: "side"` instead of `view: "high top-down"` | Switch to `view: "high top-down"` and emphasize "thin TOP SLIVER visible at the top" |

---

## KBTV Prompt Templates by Prop Type

**Use these verbatim.** Swap the `{subject}` portion for your specific prop. The trailing suffix does all the style enforcement.

### Cabinet / Shelf / Audio Rack / Bookcase (vertical furniture)

**`view: "high top-down"` + this prompt** (universal standard for vertical furniture):

```
{subject}, front-facing view from slightly above with vertical top-down perspective, 
the FRONT FACE is dominant showing the main details with only horizontal and vertical edges no diagonals, 
a thin TOP SLIVER is visible at the top showing the top surface with 45 degree depth lines, 
subject fills entire canvas with minimal transparent padding, 
dark noir palette, charcoal black outlines, transparent background, 
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath, 
NO ambient occlusion, NO brown shadow gradient beneath, 
single color black outline, crisp pixels, no anti-aliasing, pixel art, 
16-bit retro game asset
```

**Example — tall audio cabinet:**
```
tall audio equipment rack with two columns of audio gear side by side on the front face, 
audio mixer on top, equalizer with sliders, power amplifier with VU meters, 
phosphor green LED indicators, dark charcoal metal rack chassis with vent slots at top, 
front-facing view from slightly above with vertical top-down perspective, 
the FRONT FACE is dominant showing the equipment with only horizontal and vertical edges, 
a thin TOP SLIVER is visible at the top showing the top of the rack, 
subject fills entire canvas with minimal transparent padding, 
dark noir palette, charcoal black outlines, transparent background, 
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath, 
NO ambient occlusion, NO brown shadow gradient beneath, 
single color black outline, crisp pixels, no anti-aliasing, pixel art, 
16-bit retro game asset
```

**Example — bookcase:**
```
tall wooden bookcase with 4 horizontal shelves each holding rows of small books 
in dark noir wood tones and muted accent colors, 
front-facing view from slightly above with vertical top-down perspective, 
the FRONT FACE is dominant showing all the shelves and books clearly, 
a thin TOP SLIVER is visible at the top showing the top of the bookcase, 
no diagonal lines on the front face, only horizontal and vertical edges, 
subject fills entire canvas with minimal transparent padding, 
dark noir palette, charcoal black outlines, transparent background, 
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath, 
NO ambient occlusion, NO brown shadow gradient beneath, 
single color black outline, crisp pixels, no anti-aliasing, pixel art, 
16-bit retro game asset
```

### Table (horizontal furniture — different!)

**`view: "high top-down"` + this prompt:**

```
{subject}, viewed from slightly above showing both TOP SURFACE and FRONT FACE clearly, 
the wide flat top takes up the upper portion as a flat horizontal plane items sit on, 
narrow front face visible below with only horizontal and vertical edges, 
four charcoal metal legs at corners, 
the legs are VERY SHORT STUBBY legs about half the size of a person, 
NOT long, NOT tall, NOT skinny legs, the table overall is LOW and SHORT, 
dark charcoal monochrome metal, 
cabinet projection, oblique projection, 2.5D pixel art, Stardew Valley style, 
subject fills entire canvas with minimal transparent padding, 
dark noir palette, charcoal black outlines, transparent background, 
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath, 
NO ambient occlusion, NO brown shadow gradient beneath, 
NO oval blob, NO ground beneath, NO floor visible, 
the bottom of the legs is the actual bottom edge, not a shadow fade, 
single color black outline, crisp pixels, no anti-aliasing, pixel art, 
16-bit retro game asset
```

**Example — studio broadcast table:**
```
long wide broadcast control table, viewed from slightly above showing both 
TOP SURFACE and FRONT FACE clearly, the wide flat dark charcoal metal top 
takes up the upper portion as a flat horizontal plane where items sit, 
narrow front face visible below, four short stubby charcoal legs at corners, 
legs only about 20 pixels tall, the table overall is LOW and SHORT not tall, 
dark charcoal monochrome metal, NO wood texture, NO warm brown colors, 
cabinet projection, oblique projection, 2.5D pixel art, Stardew Valley style, 
subject fills entire canvas with minimal transparent padding, 
dark noir palette, charcoal black outlines, transparent background, 
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath, 
NO ambient occlusion, NO brown shadow gradient beneath, 
NO oval blob, NO ground beneath, NO floor visible, 
single color black outline, crisp pixels, no anti-aliasing, pixel art, 
16-bit retro game asset
```

### Computer Station (CRT + peripherals)

**`view: "high top-down"` + this prompt:**

```
{subject} with phosphor green CRT monitor screen, mechanical keyboard with 
visible keys, computer mouse, and charcoal metal desktop tower, 
all visible from slightly above, viewed from above showing the CRT top edge, 
subject fills entire canvas with minimal transparent padding, 
dark charcoal monochrome (NO wood texture, NO warm brown colors), 
dark noir palette, charcoal black outlines, transparent background, 
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath, 
NO ambient occlusion, single color black outline, crisp pixels, no anti-aliasing, 
pixel art, 16-bit retro game asset
```

### Wall-Mounted Items (poster, clock, sign)

**`view: "high top-down"` + this prompt** (face-on is fine for these):

```
{subject}, flat face-on view, dark noir palette, charcoal black outlines, 
subject fills entire canvas with minimal transparent padding, 
transparent background, NO shadows, NO ground shadow, NO drop shadow, 
NO contact shadow underneath, single color black outline, 
crisp pixels, no anti-aliasing, pixel art, 16-bit retro game asset
```

### Speaker on Stand (bookshelf studio monitor)

**`view: "high top-down"` + this prompt:**

```
bookshelf studio monitor speaker on a vertical stand, 
dark charcoal rectangular speaker box with a circular speaker cone visible on the FRONT FACE, 
the FRONT FACE is dominant showing the speaker cone grille clearly, 
a thin TOP SLIVER is visible at the top of the speaker box showing its top, 
vertical charcoal metal stand pole below the speaker with a small weighted base, 
front-facing view from slightly above with vertical top-down perspective, 
no diagonal lines on the front face of the speaker, only horizontal and vertical edges, 
subject fills entire canvas with minimal transparent padding, 
dark noir palette, charcoal black outlines, transparent background, 
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath, 
NO ambient occlusion, NO brown shadow gradient beneath, 
single color black outline, crisp pixels, no anti-aliasing, pixel art, 
16-bit retro game asset
```

### Office Chair (back view, with wheels)

**`view: "high top-down"` + this prompt:**

```
back view of an office chair, viewed from BEHIND showing the BACK of the chair, 
tall charcoal mesh backrest with lumbar support curve, 
the BACKREST is the dominant feature of the sprite, 
five-star wheeled base with small caster wheels visible at the bottom, 
charcoal metal post connecting backrest to base, 
front-facing view from slightly above with vertical top-down perspective, 
the FRONT FACE (which here means the BACK of the chair as the viewer sees it) is dominant, 
a thin TOP SLIVER visible at the top showing the top edge of the backrest, 
NO armrests, just the backrest and wheeled base, 
subject fills entire canvas with minimal transparent padding, 
dark noir palette, charcoal black outlines, transparent background, 
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath, 
NO ambient occlusion, NO brown shadow gradient beneath, 
single color black outline, crisp pixels, no anti-aliasing, pixel art, 
16-bit retro game asset
```

### Sound Board (broadcast mixing console with knobs)

**`view: "high top-down"` + this prompt:**

```
large broadcast studio sound mixing console, charcoal metal console chassis, 
the FRONT FACE is dominant showing rows of rotary knobs and VU meters, 
multiple rows of dark circular knobs arranged in a grid, 
small phosphor green LED indicator lights on the meters, 
faders at the bottom of the console front, 
a thin TOP SLIVER visible at the top showing the top of the console chassis, 
front-facing view from slightly above with vertical top-down perspective, 
no diagonal lines on the front face, only horizontal and vertical edges, 
subject fills entire canvas with minimal transparent padding, 
dark noir palette, charcoal black outlines, transparent background, 
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath, 
NO ambient occlusion, NO brown shadow gradient beneath, 
single color black outline, crisp pixels, no anti-aliasing, pixel art, 
16-bit retro game asset
```

### Phone Board (multi-line switchboard)

**`view: "high top-down"` + this prompt:**

```
large multi-line telephone switchboard panel, charcoal metal panel chassis, 
the FRONT FACE is dominant showing multiple rows of line buttons and indicator lights, 
many small square buttons arranged in a grid pattern for multiple phone lines, 
small phosphor green LED lights indicating active lines, 
a thin TOP SLIVER visible at the top showing the top of the switchboard, 
front-facing view from slightly above with vertical top-down perspective, 
no diagonal lines on the front face, only horizontal and vertical edges, 
subject fills entire canvas with minimal transparent padding, 
dark noir palette, charcoal black outlines, transparent background, 
NO shadows, NO ground shadow, NO drop shadow, NO contact shadow underneath, 
NO ambient occlusion, NO brown shadow gradient beneath, 
single color black outline, crisp pixels, no anti-aliasing, pixel art, 
16-bit retro game asset
```

---

## KBTV Generation Workflow (Step by Step)

Use this exact workflow for any prop regeneration. Skipping steps wastes credits.

1. **Pick the right template** from §"KBTV Prompt Templates by Prop Type" based on prop category (cabinet / table / computer / wall item).

2. **Customize the `{subject}` portion** with the specific prop description (1-2 sentences). Include key visual features:
   - Wood grain texture for wood props (or explicit "NO wood texture" for tables/computer)
   - Number of features (handles, drawers, screens, columns of gear)
   - Accent colors (phosphor green LEDs, noir red eye)

3. **Match view parameter to prop type:**
   - Cabinet/shelf/rack: `view: "side"`
   - Table: `view: "high top-down"`
   - Computer station: `view: "high top-down"`

4. **Always pass these parameters:**
   - `width`, `height` — explicit dimensions, 2× the target size for better quality
   - `no_background: true`
   - `view` (per step 3)
   - `outline: "single color outline"`
   - `shading: "basic shading"`
   - `detail: "medium detail"`
   - `text_guidance_scale: 10-12` (higher = more literal prompt following)
   - `color_image_base64`: the 264-char KBTV palette swatch
   - `seed`: any integer (different seeds give different results)

5. **Submit and wait** with `pixellab_get_image(job_id=...)`. Typically 10-40 seconds.

6. **Download the result** via the returned URL and inspect visually:
   ```
   Invoke-WebRequest -Uri "<url>" -OutFile "<path>" -UseBasicParsing
   ```

7. **Check against acceptance criteria** (visual review):
   - Front face is flat? Depth at 45°? Fill canvas? No shadows? Noir palette?
   - If NO → adjust prompt (add more emphasis on missing criteria) and re-roll
   - If YES → resize to target dimensions and save

8. **Resize to target** with nearest-neighbor (2x downsample gives best detail retention):
   ```powershell
   & resize.ps1 -SourcePath "tmp/source.png" -TargetWidth 32 -TargetHeight 56 -DestPath "assets/tiles/props/prop.png"
   ```

9. **Wire into code** (PropBuilder.CreateProp or table group).

10. **Verify build** with `dotnet build` and visually check in Godot.

---

## Style Consistency Across a Set

To make multiple props look like the same artist drew them all:

1. **Use the SAME prompt template** for every prop — only swap the `{subject}` portion.
2. **Lock palette with `color_image_base64`** — the 264-char KBTV swatch (see §1).
3. **Generate at 2× target size and step-downsample** — better detail than generating at target.
4. **Match lighting direction** — every prompt specifies "above-front-left" light source.
5. **Keep descriptions parallel** — same style suffix, only swap subject.
6. **Don't mix views** within one furniture category — all cabinets use `view: "side"`, all tables use `view: "high top-down"`.

---

## Prompt Keywords Reference

### Required Keywords (always include)

**For oblique/cabinet projection:**
- `cabinet projection` OR `oblique projection`
- `flat front face`
- `45 degree depth lines going back`
- `2.5D pixel art` OR `Stardew Valley style`

**For no shadows (always include):**
- `NO shadows`
- `NO ground shadow`
- `NO drop shadow`
- `NO contact shadow underneath`

**For canvas fill:**
- `subject fills entire canvas with minimal transparent padding`
- For tables specifically: `edges touch edges of canvas`, `the area beneath is pure transparent nothing`

**For pixel art quality:**
- `crisp pixels`, `no anti-aliasing`
- `single color black outline`
- `transparent background`

### Forbidden Keywords (will produce wrong style)

- ❌ `isometric` — triggers isometric/30° depth
- � `top-down` / `low top-down` / `high top-down` — but the `view` parameter can still be one of these!
- ❌ `3D` — too generic
- ❌ `axonometric` / `rotated cube` — triggers iso
- ❌ `bright` / `vibrant` / `saturated` — palette enforcement breaks
- ❌ Saturated color hex codes (`#00ff44`, `#ff4444`) — model may use them as anchors

---

## Common Workflows

### Regenerate one prop with a known issue

1. Look up the prop in §"Common Prop Targets" in `ART_STYLE.md`
2. Pick the template for its category (cabinet / table / etc.)
3. Fill in `{subject}` describing what's wrong with the current version + what's wanted
4. Run with the right `view` and dimensions
5. Resize to target

### Bulk-regenerate a whole set (e.g., "redo all the wall items")

1. Pick ONE anchor prop to validate the template (e.g., the wall clock)
2. Generate it, iterate until perfect
3. Run the same template with only `{subject}` swapped for each remaining prop
4. Use different seeds to get natural variation while keeping style consistent
5. Resize each to its target dimensions

### Style-match a new prop to existing KBTV style

1. Identify the prop category (cabinet / table / wall / etc.)
2. Look at the closest existing anchor asset:
   - Cabinets/shelves: `cabinet_tall.png`, `storage_shelf.png`, `filing_cabinet.png`
   - Tables: `studio_table.png`, `round_table.png`
   - Audio gear: `audio_cabinet.png`
3. Describe the new prop in terms that match (same materials, same accent colors, same scale)

---

## Security Best Practices

1. **Never commit API keys** — use environment variables or `.env` file (gitignored)
2. **Rotate keys periodically** — regenerate every few months
3. **Monitor usage** — check PixelLab dashboard for unexpected consumption
4. **Use minimal scopes** — limit API key to image generation if possible
5. **Project-specific keys** — don't reuse keys across projects

---

## Quick Reference Card

```
# Default KBTV prop generation
tool: pixellab_create_image_pixflux
view: "side" (cabinets) OR "high top-down" (tables)
width/height: 2x target size
no_background: true
text_guidance_scale: 10-12
color_image_base64: <KBTV swatch, 264 chars>
outline: "single color outline"
shading: "basic shading"
detail: "medium detail"
seed: <any integer>
prompt: <template + {subject}>

# Required keywords in prompt:
- cabinet projection OR oblique projection
- flat front face
- 45 degree depth lines
- NO shadows, NO ground shadow, NO drop shadow, NO contact shadow
- subject fills entire canvas with minimal transparent padding
- transparent background
- crisp pixels, no anti-aliasing

# Forbidden keywords:
- isometric, top-down (in prompt text), 3D, axonometric

# After generation:
1. Download via the returned URL
2. Inspect for: flat front, 45° depth, full canvas, no shadows, noir palette
3. Resize to target with nearest-neighbor (2x downsample)
4. Save to assets/tiles/props/
5. Wire into code via PropBuilder.CreateProp
6. Build + visually verify in Godot
```

---

*Last updated: 2026-08-26 — Consolidated KBTV PixelLab workflow after iterative prop art sessions.*
*KBTV Project - AI-Assisted Pixel Art Pipeline*
