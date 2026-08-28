# PixelLab MCP Integration Guide

## Overview

This guide documents the **PixelLab MCP tool surface** as we actually use it in the KBTV project: which tools exist, what they cost, and the MCP/API restrictions that bite. It is **tool mechanics only**.

- For **what the art must look like** (palette, proportions, prop categories): [`docs/art/ART_STYLE.md`](ART_STYLE.md)
- For **prompt rules, templates, and the generation budget protocol**: [`docs/art/PIXELLAB_PROMPT_RULES.md`](PIXELLAB_PROMPT_RULES.md) — **this is the authoritative operational doc. Any prompt-related guidance lives there, not here.**

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
| `pixellab_create_image_pro` | 20-40 gen | Multiple candidates (4-64). Use only when you need a pick set to review. |
| `pixellab_edit_image` | 20-40 gen | Edit an existing PNG with text instruction (multi-frame batch). |
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
| `pixellab_reduce_colors` | 0.1 gen | Quantize to a target palette. **Run on every accepted sprite.** |
| `pixellab_image_to_pixelart` | 1 gen | Convert non-pixel source art to pixel art. |
| `pixellab_get_balance` | free | Check generation credits remaining. |
| `pixellab_get_image` / `get_character` / `get_object` | free | Poll queued jobs / inspect results. |

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
3. **Do NOT rely on `style_image_base64` with realistic-sized PNGs.** KBTV prop PNGs exceed the truncation limit as base64.

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

The 264-char base64 string of this swatch (stable across regenerations — also embedded in `PIXELLAB_PROMPT_RULES.md` §2):
```
iVBORw0KGgoAAAANSUhEUgAAABAAAAAQCAYAAAAf8/9hAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAABZSURBVDhPY+DlFfovL6/2X0vX7L+Vrcd/LSXp/1HBDv+r8kP/W3VV/F9kZfX/xKyo/7jUMRCrEJc6BmIV4lLHQKxCXOoYiFWISx0DsQpxqRsNxNFAHByBCABw+NZc4D+vIwAAAABJRU5ErkJggg==
```

### 2. View Parameter: Pick the Right One

`pixellab_create_image_pixflux` exposes a `view` parameter. For KBTV props there is exactly ONE correct value:

| `view` value | What you get | KBTV use? |
|--------------|--------------|------------|
| `"side"` | Eye-level flat-front elevation. | ❌ Forbidden for props |
| `"high top-down"` | Steeper top-down (~35°). Front face dominant, top sliver visible. | ✅ **The ONLY value used.** |
| `"low top-down"` | Gentler top-down (~20°). 3/4 view. | ❌ Forbidden — produces isometric |

**Bottom line (unambiguous):** `view: "high top-down"` on every prop call. `"side"` and `"low top-down"` only produce wrong geometry and wasted credits. Do not use the words "top-down" in the description text (see `PIXELLAB_PROMPT_RULES.md` §5).

### 3. Aspect Ratio & Canvas Size

| Tool | Min | Max | Aspect |
|------|-----|-----|--------|
| `create_image_pixflux` | 16px | 400×400 or 16:9 (688×384) | Square or 16:9 OK |
| `create_image_pixen` | 16px | 256×256 (area ≤65,536px) | Strict, square preferred |
| `create_image_pro` | 16px | 512×512 / 688×384 | Aspect-gated |
| `create_1_direction_object` | 16px | 256×256 | **Square only** |
| `create_8_direction_object` | 24px | 168×168 | **Square only** |

**Pitfalls:**
- **Square tools rotate/stretch non-square content.** `create_1_direction_object` with a "32×56 cabinet" prompt → square sprite, the AI rotates/curves the cabinet to fit. Use `create_image_pixflux` for non-square targets instead. KBTV props NEVER use the square-only object tools.
- **Generate OVERSIZE then crop/resize.** Target 32×56 → generate at 64×112 → resize DOWN with nearest-neighbor. Never upscale AI output — pixel art loses its grid.
- **3:1+ aspect ratios are unreliable.** Stick to roughly 1:1, 2:1, or 3:2.

### 4. Generation Cost Reality Check

| Tool | Cost | Notes |
|------|------|-------|
| `create_image_pixflux` | **1 generation** | Fast (~10-40s). Default choice. |
| `create_image_pixen` | **1 generation** | Slightly better quality on small sprites. |
| `create_image_pro` | **20-40 generations** | Returns 4-64 candidates. Expensive — one call per asset max. |
| `create_1_direction_object` ≤42px | **64 candidates** | Review mode — pick frames. ≤85px: 16. Not used for KBTV props. |
| `create_8_direction_object` | **20-40 generations** | Always 8 directions. |
| `correct_pixelart` / `unzoom_image` / `reduce_colors` | **0.1 generation** | Essentially free. Always use on accept. |

**Default strategy:** `create_image_pixflux` (1 gen) + the canonical template + `color_image_base64` palette swatch (§1). Escalate to `create_image_pro` only for hero props or after a failed 1-gen shot (see the budget protocol in `PIXELLAB_PROMPT_RULES.md` §6).

---

## Prompt Templates & Generation Workflow

**Moved.** All prompt templates (universal prop / table / wall-mounted / chair / caller silhouette), the canonical parameter block, keyword rules, the iteration budget protocol, the failure-mode → fix table, and the per-asset repro log format live in:

➡️ **`docs/art/PIXELLAB_PROMPT_RULES.md`**

If you are about to write a pixel-art prompt, read that file first. The budget protocol there is mandatory — it exists because re-roll spirals were burning credits.

---

## Security Best Practices

1. **Never commit API keys** — use environment variables or `.env` file (gitignored)
2. **Rotate keys periodically** — regenerate every few months
3. **Monitor usage** — check PixelLab dashboard for unexpected consumption
4. **Use minimal scopes** — limit API key to image generation if possible
5. **Project-specific keys** — don't reuse keys across projects

---

*Last updated: 2026-08-27 — Reduced to tool-mechanics reference; prompt guidance moved to `PIXELLAB_PROMPT_RULES.md`.*
*KBTV Project - AI-Assisted Pixel Art Pipeline*