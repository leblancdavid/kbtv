# KBTV Pixel Art Generator

Python tool for batch generating pixel art assets for the KBTV radio station game using AI image generation services.

## Quick Start

### 1. Install Dependencies

```bash
cd Tools/ArtGeneration
pip install -r requirements.txt
```

### 2. Get an API Key

**Recommended: Leonardo.ai**
- Sign up at [leonardo.ai](https://leonardo.ai/)
- Get API key from your account settings
- Free tier: 150 tokens/day

**Alternative: OpenAI DALL-E 3**
- Requires OpenAI API key with credits

### 3. Set Environment Variable

```bash
# Windows PowerShell
$env:LEONARDO_API_KEY="your_key_here"

# Or create .env file in Tools/ArtGeneration/
# LEONARDO_API_KEY=your_key_here
```

### 4. Test with Dry Run

```bash
python generate_pixel_art.py --category tiles --dry-run
```

This shows what would be generated without calling the API.

### 5. Generate Assets

```bash
# Generate all floor tiles
python generate_pixel_art.py --category tiles --subcategory floor

# Generate all furniture for the desk
python generate_pixel_art.py --category furniture --subcategory desk

# Generate Vern's portrait variants
python generate_pixel_art.py --category characters --subcategory vern_portrait

# Generate EVERYTHING (takes hours and costs credits!)
python generate_pixel_art.py --all
```

### 6. See What's Available

```bash
# List all categories and subcategories
python generate_pixel_art.py --list-categories

# List all assets that would be generated for a category
python generate_pixel_art.py --category furniture --list-assets
```

## Generated File Structure

Assets are saved to `assets/` following KBTV conventions:

```
assets/
├── tiles/
│   ├── floor/
│   │   ├── floor_tile_clean.png
│   │   ├── floor_tile_wear.png
│   │   └── floor_tile_stain.png
│   └── walls/
│       ├── wall_north.png
│       ├── wall_south.png
│       └── wall_corner.png
├── furniture/
│   ├── desk/
│   │   ├── monitor_console.png
│   │   └── monitor_console_depth.png
│   └── cabinet/
│       ├── filing_cabinet.png
│       └── filing_cabinet_depth.png
├── characters/
│   ├── vern_portrait/
│   │   ├── vern_portrait_neutral.png
│   │   ├── vern_portrait_tired.png
│   │   └── ...
│   └── caller_silhouette/
│       ├── caller_silhouette_generic_male.png
│       └── ...
├── ui/
│   ├── panel/
│   │   └── panel_background.png
│   ├── button/
│   │   ├── button_default.png
│   │   ├── button_hover.png
│   │   └── button_pressed.png
│   └── indicator/
│       ├── indicator_onair.png
│       └── indicator_waiting.png
├── equipment/
│   ├── microphone/
│   │   └── boom_mic.png
│   └── ...
└── effects/
    ├── glow/
    │   ├── glow_green.png
    │   ├── glow_red.png
    │   └── ...
    └── scanlines/
        └── scanlines_overlay.png
```

## Post-Processing Workflow

AI-generated pixel art needs cleanup. After generation:

1. **Open in Aseprite / Pyxel Edit**
2. **Remove anti-aliasing** - Ensure all edges are crisp pixels
3. **Adjust to KBTV palette** - Replace colors with project palette
4. **Fix transparency** - Alpha should be 0 or 255 only
5. **Test in Godot** - Import and verify sharp rendering
6. **Add to TileSet** - For tiles, add to `topdown_tileset.tres`

> **DEPRECATED**: This generator targets Leonardo.ai / DALL-E with out-of-date prompt rules. New art is generated through the PixelLab MCP workflow — see `docs/art/PIXELLAB_PROMPT_RULES.md` (authoritative prompt rules) and `docs/art/PIXELLAB_MCP_GUIDE.md` (tool mechanics).

See `docs/art/PIXELLAB_PROMPT_RULES.md` for complete style guidelines and post-processing steps.

## Command Reference

### Basic Commands

```bash
# Dry run (no API calls)
python generate_pixel_art.py --category tiles --subcategory floor --dry-run

# Generate single asset
python generate_pixel_art.py --category ui --subcategory button --asset button_default

# Generate entire subcategory
python generate_pixel_art.py --category characters --subcategory vern_portrait

# Generate everything
python generate_pixel_art.py --all
```

### Service Selection

```bash
# Use Leonardo.ai (default)
python generate_pixel_art.py --category tiles --service leonardo

# Use DALL-E 3 (fallback)
python generate_pixel_art.py --category tiles --service dalle
```

## Leonardo.ai Specific Notes

The tool uses Leonardo's Pixel Art model (`b24e16ff-0646-49db-9f8c-79ca244166f4`).

**Prompt Processing:**
- Automatically adds "pixel art, 8-bit retro, crisp pixels, no anti-aliasing"
- Generates at 4x target size for quality, then crops to exact dimensions
- Negative prompt: "blurry, anti-aliased, smooth, photo-realistic, 3D, gradient"
- Default guidance scale: 7.0
- Default inference steps: 30

**Tips:**
- Free tier gives 150 tokens/day
- Each generation costs ~4-8 tokens depending on size
- Generation takes 30-90 seconds typically
- Use `--dry-run` to plan without spending tokens

## Troubleshooting

### "Requests library not installed"
```bash
pip install requests
```

### "OpenAI library not installed"
```bash
pip install openai
```

### "LEONARDO_API_KEY environment variable not set"
Set it before running:
```bash
export LEONARDO_API_KEY="your_key"
# Or on Windows:
set LEONARDO_API_KEY=your_key
```

### Generation fails with timeout
- Leonardo can be slow during peak times
- Increase max_polls in LeonardoService (line ~445)
- Check your API quota hasn't been exceeded

### Images not pixel-perfect
- AI is imperfect; expect to manually clean up in Aseprite
- Use variations: generate 3-5 of each asset, pick best
- Refine with img2img using best as seed

### Wrong dimensions in output
The tool generates at 4x size to improve quality, then uses exact dimensions for final output. Your AI service may return slightly different dimensions; they're cropped to match spec.

## Asset Priority

To get a playable demo fastest, generate in this order:

1. **Critical** (playable):
   - floor_tile_clean
   - wall_north, wall_south
   - monitor_console
   - filing_cabinet
   - Vern portraits: neutral, tired, focused
   - caller_silhouette_generic_male
   - button_default, button_hover, button_pressed
   - panel_background
   - indicator_onair
   - progress_bar

2. **Important** (enhances experience):
   - Additional floor variants
   - All furniture (desk_depth, bookcase, coffee_station)
   - All Vern mood portraits (10 total)
   - All caller archetypes (11 total)
   - All UI states and icons

3. **Polish**:
   - Effects (glows, scanlines, static)
   - Equipment (mic, sound_board, phones)
   - Animation frames

## Development

### Extending the Prompt Library

The `PromptParser` class in `generate_pixel_art.py` defines asset specifications programmatically. To add new assets:

1. Edit the `_generate_*` methods in `PromptParser`
2. Add `AssetSpec` entries with name, prompt, dimensions
3. Run `--list-assets` to verify

Alternatively, modify the parser to read from `PIXELLAB_PROMPT_RULES.md` directly for a data-driven approach.

### Adding New AI Services

Create a new class inheriting from `AIService`:

```python
class MyService(AIService):
    def validate_config(self) -> bool:
        return bool(self.api_key)

    def generate(self, spec: AssetSpec) -> GenerationResult:
        # Implement API call
        pass
```

Add to CLI `--service` choices and instantiate in `main()`.

## Files

- `generate_pixel_art.py` - Main CLI tool
- `requirements.txt` - Python dependencies
- `docs/art/PIXELLAB_PROMPT_RULES.md` - Authoritative prompt rules (replaces the deleted PIXEL_ART_PROMPTS.md)
- `docs/art/ART_STYLE.md` - Art style guidelines

## Credits

- **AI Models**: Leonardo.ai Pixel Art model
- **Game**: KBTV - Paranormal Talk Radio Simulator
- **Engine**: Godot 4.x

## License

KBTV project license applies. This tool is part of the KBTV codebase.
