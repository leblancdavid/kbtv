# PixelLab MCP Integration Guide

## Overview

This guide covers how to connect and use the PixelLab MCP server with opencode for AI-powered pixel art generation in the KBTV project.

## What is PixelLab MCP?

PixelLab MCP is a Model Context Protocol server that provides AI image generation capabilities. It connects opencode to PixelLab's AI services, allowing you to generate pixel art assets directly from your chat interface.

---

## Prerequisites

1. **PixelLab Account**: Sign up at [pixellab.app](https://pixellab.app)
2. **API Key**: Obtain from your PixelLab account settings
3. **MCP Server**: Installed and running (see installation section)
4. **opencode**: Latest version with MCP support

---

## Installation & Configuration

### Step 1: Install PixelLab MCP Server

Depending on your setup:

**Option A: Using npm (Node.js)**
```bash
npm install -g @pixellab/mcp-server
```

**Option B: Python (if using our custom server)**
```bash
cd Tools/ArtGeneration
pip install -r requirements.txt
```

**Option C: Standalone executable**
Download from the PixelLab MCP releases page and place in your PATH.

### Step 2: Configure opencode.jsonc

Open your opencode configuration file:

**Location:**
- Linux/macOS: `~/.config/opencode/opencode.jsonc`
- Windows: `%APPDATA%\opencode\opencode.jsonc` or `C:\Users\<User>\.config\opencode\opencode.jsonc`
- Project-local: `.opencode.jsonc` in project root

**Add the MCP server configuration:**

```jsonc
{
  "mcpServers": {
    "pixellab": {
      "command": "npx",  // or "python", or direct path
      "args": ["@pixellab/mcp-server"],
      "env": {
        "PIXELLAB_API_KEY": "your_api_key_here"
      }
    }
  }
}
```

**Alternative configurations:**

*If using Python:*
```jsonc
{
  "mcpServers": {
    "pixellab": {
      "command": "python",
      "args": ["Tools/ArtGeneration/pixel_art_mcp_server.py"],
      "env": {
        "LEONARDO_API_KEY": "your_leonardo_key",  // if using Leonardo
        "PIXELLAB_API_KEY": "your_pixellab_key"
      }
    }
  }
}
```

*If using direct executable:*
```jsonc
{
  "mcpServers": {
    "pixellab": {
      "command": "pixel-lab-mcp",
      "args": ["--port", "8080"],
      "env": {
        "PIXELLAB_BEARER_TOKEN": "your_token"
      }
    }
  }
}
```

**Important Notes:**
- `command` must be in your system PATH, or use absolute path
- The `env` block passes environment variables to the MCP server process
- Never commit your API keys to version control! Use environment variables or a `.env` file

### Step 3: Restart opencode

After saving `opencode.jsonc`, restart opencode to load the new MCP server. You should see a notification that the `pixellab` server connected.

---

## Using PixelLab MCP

Once connected, you can use the PixelLab tools directly in your chat with opencode.

### Tool: `pixellab_generate`

Generate pixel art assets using AI.

**Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `prompt` | string | Yes | Text description of the image to generate |
| `width` | number | No | Image width in pixels (default: 32) |
| `height` | number | No | Image height in pixels (default: 32) |
| `style` | string | No | Art style preset (default: "pixel-art") |
| `palette` | string | No | Color palette reference (e.g., "kbtv") |
| `output` | string | No | Output file path relative to `assets/` |
| `variations` | number | No | Number of variants to generate (1-5) |

**Example usage:**

```bash
Generate a floor tile for KBTV:
prompt: "dark carpet tile, 16x16, seamless, muted green #2d4a2d, top-down, pixel art"
width: 16
height: 16
output: "tiles/floor/floor_tile_01.png"
```

```bash
Generate Vern's portrait:
prompt: "radio host portrait, mid-30s male, dark beard, noir lighting, 64x64, pixel art"
width: 64
height: 64
output: "sprites/characters/vern_portrait_neutral.png"
style: "portrait"
```

### Tool: `pixellab_list_styles`

List available art style presets.

**Example:**
```
What pixel art styles does PixelLab support?
> Use MCP tool: pixellab_list_styles
```

Returns:
```json
{
  "styles": [
    {"id": "pixel-art", "name": "Pixel Art", "description": "Classic 8-bit style"},
    {"id": "8bit", "name": "8-bit", "description": "NES-era retro"},
    {"id": "16bit", "name": "16-bit", "description": "SNES-era detailed"},
    {"id": "isometric", "name": "Isometric", "description": "Top-down depth view"}
  ]
}
```

### Tool: `pixellab_check_status`

Check the PixelLab MCP server status and your API quota.

**Example:**
```
Check my PixelLab quota
> Use MCP tool: pixellab_check_status
```

Returns:
```json
{
  "status": "connected",
  "api_quota_remaining": 145,
  "generated_today": 5,
  "server_version": "1.2.0"
}
```

---

## Common Workflows

### Generate a Complete Asset Set

```bash
# 1. Floor tiles (3 variants)
Generate 3 floor tile variations:
prompt: "dark carpet tile pattern, 16x16, subtle variation, seamless, office flooring"
width: 16
height: 16
output: "tiles/floor/floor_tile_{n}.png"
variations: 3

# 2. Wall textures
Generate wall tile:
prompt: "wall texture, isometric side view, 16x64, brown tones #5a4a3a, concrete surface"
width: 16
height: 64
output: "tiles/wall_north.png"

# 3. Furniture
Generate monitor console:
prompt: "retro computer console, monitor+keyboard+phone, 64x64, glowing CRT #00ff44"
width: 64
height: 64
output: "furniture/desk/monitor_console.png"
```

### Batch Generation from Prompt Library

Refer to `docs/art/PIXEL_ART_PROMPTS.md` for 100+ pre-written prompts.

**Workflow:**
1. Open `PIXEL_ART_PROMPTS.md`
2. Copy a prompt for your needed asset
3. Use `pixellab_generate` with that prompt
4. Adjust dimensions if needed
5.Repeat for variants

### Iterative Refinement

```bash
# First generation
Generate asset:
prompt: "monitor console, dark wood, retro, 64x64, pixel art"
output: "furniture/desk/monitor_console_v1.png"

# Review result, then refine
Generate asset (img2img if supported):
prompt: "same as before but add glowing green screen #00ff44, more detail on keyboard"
base_image: "assets/furniture/desk/monitor_console_v1.png"
output: "furniture/desk/monitor_console_v2.png"
```

---

## KBTV-Specific Settings

### Color Palette

Reference the KBTV palette in your prompts:

```json
{
  "palette": {
    "bg_dark": "#1a1a2a",
    "floor_carpet": "#2d4a2d",
    "walls": "#5a4a3a",
    "accent_green": "#00ff44",
    "accent_red": "#ff4444",
    "accent_cyan": "#00ffff",
    "accent_amber": "#ffaa00"
  }
}
```

Use in prompts:
```
prompt: "dark atmosphere, accent glow #00ff44, noir lighting, palette #1a1a2a, #2d4a2d, #5a4a3a"
```

### Recommended Dimensions

From `docs/art/ART_STYLE.md`:

| Asset Type | Size | Use |
|------------|------|-----|
| Floor tiles | 16x16 | TileMap floor layer |
| Wall strips | 16x64 | Vertical walls |
| Furniture | 32-64px | Props and decor |
| Portraits | 64x64 | UI conversation display |
| Icons | 16-24px | UI indicators |
| Character sprites | 32x48 | In-game player/caller |

---

## Troubleshooting

### "MCP server not responding"
- Check that the command in `opencode.jsonc` is correct and in PATH
- Ensure API key is set in `env` block
- Test the server manually: `pixel-lab-mcp --test` or `python pixel_art_mcp_server.py --health`

### "Invalid API key" or "401 Unauthorized"
- Verify your PixelLab API key is correct
- Check if the key has expired or quota exceeded
- Ensure the `env` variable name matches what the server expects (often `PIXELLAB_API_KEY` or `BEARER_TOKEN`)

### "Generation failed" with no output
- The MCP server may have crashed; check its logs
- API quota may be exceeded
- Prompt may be rejected by content filter; simplify or rephrase

### Generated images are not pixel-perfect
- Add "pixel art", "crisp pixels", "no anti-aliasing" to prompt
- Specify exact dimensions (width/height parameters)
- Post-process in Aseprite/Pyxel Edit to clean up edges
- Generate multiple variations and select best

### "Command not found" on startup
- Use absolute path in `command` instead of just executable name
- Example: `"command": "C:\\path\\to\\pixel-lab-mcp.exe"`
- Or add the directory to your system PATH

---

## Advanced Usage

### Using Custom Style Presets

If your PixelLab MCP supports custom styles:

```jsonc
{
  "mcpServers": {
    "pixellab": {
      "command": "npx",
      "args": ["@pixellab/mcp-server", "--styles", "~/pixellab-styles.json"]
    }
  }
}
```

Create `~/pixellab-styles.json`:
```json
{
  "kbtv-pixel": {
    "prompt_prefix": "pixel art, isometric top-down, limited palette",
    "negative_prompt": "blurry, anti-aliased, smooth, photo-realistic",
    "cfg_scale": 7.0,
    "steps": 30
  }
}
```

Then use: `style: "kbtv-pixel"` in your tool calls.

### Image-to-Image (Refinement)

If supported, you can refine existing assets:

```bash
Refine floor tile:
prompt: "same style but add coffee stain, more worn"
base_image: "assets/tiles/floor/floor_tile_clean.png"
strength: 0.6
output: "assets/tiles/floor/floor_tile_stain.png"
```

---

## Integration with KBTV Workflow

### Phase 1: Critical Assets

Generate in this order:

1. **Environment**
   - `floor_tile_clean` (16x16)
   - `wall_north`, `wall_south` (16x64)
   - `corner_piece` (16x64)

2. **Key Furniture**
   - `monitor_console` (64x64)
   - `monitor_screen` (32x32, with glow variants)
   - `filing_cabinet` (32x48)

3. **Characters**
   - `vern_portrait_neutral` (64x64)
   - `caller_silhouette_male` (32x48)

4. **UI**
   - `panel_background` (200x28)
   - `button_default`, `button_hover`, `button_pressed` (80x28)
   - `indicator_onair` (32x16)

See `docs/art/PIXEL_ART_PROMPTS.md` for exact prompts for each asset.

### Phase 2: Post-Processing

After generation:

1. Open in Aseprite or Pyxel Edit
2. Remove anti-aliasing (should be crisp pixels only)
3. Adjust colors to match KBTV palette exactly
4. Check transparency (alpha 0 or 255 only)
5. Test import in Godot (Filter: Nearest)
6. Add to TileSet or Sprite2D

---

## MCP Tool Reference

All available tools from the PixelLab MCP server:

| Tool | Description | Parameters |
|------|-------------|------------|
| `pixellab_generate` | Generate an image from text prompt | `prompt`, `width`, `height`, `style`, `output`, `variations` |
| `pixellab_refine` | Refine existing image (img2img) | `prompt`, `base_image`, `strength`, `output` |
| `pixellab_list_styles` | List available style presets | None |
| `pixellab_check_status` | Check server and quota status | None |
| `pixellab_upload` | Upload a reference image for style matching | `image_path`, `name` |
| `pixellab_create_tileset` | Generate seamless tileset | `base_prompt`, `count`, `size` |

---

## Environment Variables Reference

| Variable | Purpose | Example |
|----------|---------|---------|
| `PIXELLAB_API_KEY` | Main API authentication | `"sk-123456..."` |
| `PIXELLAB_BEARER_TOKEN` | Alternative auth format | `"Bearer sk-..."` |
| `PIXELLAB_MODEL` | Override default model | `"pixel-art-v3"` |
| `PIXELLAB_STYLES_PATH` | Path to custom styles file | `"./styles.json"` |
| `LEONARDO_API_KEY` | If using Leonardo.ai backend | `"your-key"` |
| `OPENAI_API_KEY` | If using DALL-E 3 fallback | `"sk-..."` |

---

## Security Best Practices

1. **Never commit API keys** - Use environment variables or `.env` files (gitignored)
2. **Rotate keys periodically** - Regenerate API keys every few months
3. **Monitor usage** - Check PixelLab dashboard for unexpected usage
4. **Use minimal scopes** - If possible, limit API key to image generation only
5. **Project-specific keys** - Don't reuse keys across projects

---

## Next Steps

1. **Test the connection**: Use `pixellab_check_status` to verify MCP is working
2. **Generate critical assets**: Start with the priority list in `PIXEL_ART_PROMPTS.md`
3. **Iterate on prompts**: Refine based on output quality
4. **Build asset library**: Generate all needed assets by category
5. **Integrate into Godot**: Import with Nearest filter, create TileSets

---

## Support & Resources

- **PixelLab MCP Docs**: Check the MCP server's `--help` or README
- **KBTV Art Style**: See `docs/art/ART_STYLE.md`
- **Prompt Library**: See `docs/art/PIXEL_ART_PROMPTS.md`
- **MCP Specification**: https://modelcontextprotocol.io/
- **opencode MCP Support**: `/help` in opencode

---

## Quick Reference Card

```
# Test MCP connection
> Use MCP tool: pixellab_check_status

# Generate floor tile
> Use MCP tool: pixellab_generate
{
  "prompt": "dark carpet tile, 16x16, seamless, #2d4a2d, pixel art",
  "width": 16,
  "height": 16,
  "output": "tiles/floor/floor_tile_01.png"
}

# Generate with style preset
> Use MCP tool: pixellab_generate
{
  "prompt": "Vern portrait, noir lighting",
  "width": 64,
  "height": 64,
  "style": "portrait",
  "output": "sprites/characters/vern_portrait.png"
}
```

---

*Last updated: 2026-03-08*
*KBTV Project - AI-Assisted Pixel Art Pipeline*