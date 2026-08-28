#!/usr/bin/env python3
"""
DEPRECATED — do not use for new art.

This batch generator targets Leonardo.ai / DALL-E and hard-codes prompt
styles that violate the current rules (saturated hexes, "top-down isometric",
"side" views). Art generation now goes through the PixelLab MCP workflow;
see `docs/art/PIXELLAB_PROMPT_RULES.md` (authoritative) and
`docs/art/PIXELLAB_MCP_GUIDE.md` (tool mechanics). Retained for reference only.

KBTV Pixel Art Batch Generator
Generates pixel art assets using AI image generation APIs.

Supports:
- Leonardo.ai (recommended, best pixel art quality)
- DALL-E 3 (fallback, decent results)
- Local Stable Diffusion (optional)

Usage:
    python generate_pixel_art.py --category tiles --asset floor
    python generate_pixel_art.py --all
    python generate_pixel_art.py --list-categories
"""

import json
import os
import sys
import argparse
import time
from pathlib import Path
from typing import Dict, List, Optional
from dataclasses import dataclass, asdict
from abc import ABC, abstractmethod

# ============================================================================
# Configuration
# ============================================================================

PROJECT_ROOT = Path(__file__).parent.parent.parent
ASSETS_DIR = PROJECT_ROOT / "assets"
PROMPTS_FILE = PROJECT_ROOT / "docs" / "art" / "PIXELLAB_PROMPT_RULES.md"

# AI Service API Keys (set via environment variables)
LEONARDO_API_KEY = os.environ.get("LEONARDO_API_KEY", "")
OPENAI_API_KEY = os.environ.get("OPENAI_API_KEY", "")

# ============================================================================
# Data Models
# ============================================================================

@dataclass
class AssetSpec:
    """Specification for a single asset generation task."""
    name: str
    prompt: str
    width: int
    height: int
    category: str
    subcategory: str
    variations: int = 1
    negative_prompt: str = ""
    seed: Optional[int] = None
    style_preset: str = "pixel-art"
    guidance_scale: float = 7.0

@dataclass
class GenerationResult:
    """Result of a single generation."""
    asset_name: str
    filepath: Path
    success: bool
    error: Optional[str] = None
    seed: Optional[int] = None
    generation_time: float = 0.0

# ============================================================================
# Prompt Parser (Extracts from Markdown)
# ============================================================================

class PromptParser:
    """Parses prompt specs (legacy: PIXELLAB_PROMPT_RULES.md no longer carries asset specs)."""

    def __init__(self, prompts_file: Path):
        self.prompts_file = prompts_file
        self.categories = self._load_categories()

    def _load_categories(self) -> Dict[str, Dict]:
        """Load category definitions from prompts file."""
        # Define categories based on document structure
        return {
            "tiles": {
                "floor": {"size": (16, 16), "keywords": ["floor", "tile", "carpet"]},
                "walls": {"size": (16, 64), "keywords": ["wall", "texture"]},
                "corners": {"size": (16, 64), "keywords": ["corner", "bracket"]},
            },
            "furniture": {
                "desk": {"size": (64, 64), "keywords": ["desk", "console"]},
                "chair": {"size": (32, 32), "keywords": ["chair"]},
                "cabinet": {"size": (32, 48), "keywords": ["cabinet", "filing"]},
                "bookcase": {"size": (48, 64), "keywords": ["bookcase", "shelf"]},
                "table": {"size": (48, 48), "keywords": ["table", "round"]},
                "coffee_station": {"size": (32, 32), "keywords": ["coffee"]},
                "clock": {"size": (24, 24), "keywords": ["clock"]},
            },
            "characters": {
                "vern_portrait": {"size": (64, 64), "keywords": ["portrait", "Vern"]},
                "vern_sprite": {"size": (32, 48), "keywords": ["sprite", "Vern standing"]},
                "caller_silhouette": {"size": (32, 48), "keywords": ["silhouette", "caller"]},
            },
            "ui": {
                "panel": {"size": (200, 28), "keywords": ["panel", "background"]},
                "button": {"size": (80, 28), "keywords": ["button"]},
                "indicator": {"size": (16, 16), "keywords": ["indicator", "ON AIR", "LED"]},
                "progress_bar": {"size": (200, 16), "keywords": ["progress", "bar"]},
                "tab": {"size": (80, 24), "keywords": ["tab"]},
            },
            "equipment": {
                "microphone": {"size": (24, 24), "keywords": ["microphone", "mic"]},
                "sound_board": {"size": (64, 32), "keywords": ["sound board", "mixing"]},
                "phone": {"size": (24, 32), "keywords": ["phone", "telephone"]},
                "monitor": {"size": (32, 32), "keywords": ["monitor", "CRT"]},
            },
            "effects": {
                "glow": {"size": (32, 32), "keywords": ["glow", "light"]},
                "scanlines": {"size": (64, 64), "keywords": ["scanlines", "CRT"]},
                "static": {"size": (32, 32), "keywords": ["static", "noise"]},
            },
        }

    def parse_asset_prompts(self, category: str, subcategory: Optional[str] = None) -> List[AssetSpec]:
        """Parse prompts for a category/subcategory."""
        specs = []

        # For now, generate from predefined templates
        # In production, would parse the actual markdown file
        if category == "tiles":
            if subcategory == "floor":
                specs.extend(self._generate_floor_tiles())
            elif subcategory == "walls":
                specs.extend(self._generate_wall_tiles())
        elif category == "furniture":
            specs.extend(self._generate_furniture(subcategory))
        elif category == "characters":
            specs.extend(self._generate_characters(subcategory))
        elif category == "ui":
            specs.extend(self._generate_ui_elements(subcategory))
        elif category == "equipment":
            specs.extend(self._generate_equipment(subcategory))
        elif category == "effects":
            specs.extend(self._generate_effects(subcategory))

        return specs

    def _generate_floor_tiles(self) -> List[AssetSpec]:
        """Generate floor tile specifications."""
        base_prompt = "dark carpet tile pattern, pixel art, 16x16, seamless tiling, texture with subtle variation, muted green #2d4a2d, top-down view, office flooring, clean edges, no anti-aliasing, crisp pixels"
        return [
            AssetSpec("floor_tile_clean", base_prompt + " uniform texture", 16, 16, "tiles", "floor"),
            AssetSpec("floor_tile_wear", base_prompt + " small scuff marks, realistic detail", 16, 16, "tiles", "floor"),
            AssetSpec("floor_tile_stain", base_prompt + " coffee drip mark, darker patch", 16, 16, "tiles", "floor"),
        ]

    def _generate_wall_tiles(self) -> List[AssetSpec]:
        """Generate wall tile specifications."""
        base_prompt = "wall texture, retro pixel art, 16x64, concrete/plaster surface, brown tones #5a4a3a, isometric side view, minimal detail, vertical strip, crisp pixels"
        return [
            AssetSpec("wall_north", base_prompt, 16, 64, "tiles", "walls"),
            AssetSpec("wall_south", base_prompt, 16, 64, "tiles", "walls"),
            AssetSpec("wall_corner", "wall corner bracket, pixel art, 16x64, joining two walls, isometric angle, dark wood texture, clean edges", 16, 64, "tiles", "corners"),
        ]

    def _generate_furniture(self, subcategory: Optional[str]) -> List[AssetSpec]:
        """Generate furniture specifications."""
        all_specs = []

        if subcategory in (None, "desk"):
            all_specs.append(AssetSpec(
                "monitor_console",
                "retro computer console, monitor + keyboard + phone, pixel art, 64x64, dark wood/metal, top-down isometric, glowing CRT screen #00ff44, office equipment, 80s aesthetic",
                64, 64, "furniture", "desk"
            ))
            all_specs.append(AssetSpec(
                "monitor_console_depth",
                "desk depth sprite, pixel art, 48x64, front face darker #3a3a3a, top face lighter #4a4a4a, isometric occlusion, vertical shadow",
                48, 64, "furniture", "desk"
            ))

        if subcategory in (None, "cabinet"):
            all_specs.append(AssetSpec(
                "filing_cabinet",
                "vertical filing cabinet, 2-3 drawers, pixel art, 32x48, steel grey #6a6a6a, office storage, labeled drawers",
                32, 48, "furniture", "cabinet"
            ))
            all_specs.append(AssetSpec(
                "filing_cabinet_depth",
                "filing cabinet depth, pixel art, 48x64, tall rectangular, front face dark, top face lit, industrial design",
                48, 64, "furniture", "cabinet"
            ))

        if subcategory in (None, "bookcase"):
            all_specs.append(AssetSpec(
                "bookcase",
                "wooden bookcase, filled with books, pixel art, 48x64, dark stain #5a4a3a, organized clutter, isometric angle",
                48, 64, "furniture", "bookcase"
            ))

        if subcategory in (None, "table"):
            all_specs.append(AssetSpec(
                "round_table",
                "round conference table, pixel art, 48x48, dark wood, oval shape, top-down isometric",
                48, 48, "furniture", "table"
            ))

        if subcategory in (None, "coffee_station"):
            all_specs.append(AssetSpec(
                "coffee_station",
                "coffee maker with mug, office break area, pixel art, warm light, top-down, simple details, 32x32",
                32, 32, "furniture", "coffee_station"
            ))

        if subcategory in (None, "clock"):
            all_specs.append(AssetSpec(
                "wall_clock",
                "wall clock, analog, retro design, pixel art, 24x24, ticking hands, office wall mount, dark face, white numbers",
                24, 24, "furniture", "clock"
            ))

        return all_specs

    def _generate_characters(self, subcategory: Optional[str]) -> List[AssetSpec]:
        """Generate character specifications."""
        all_specs = []

        if subcategory in (None, "vern_portrait"):
            moods = ["neutral", "tired", "focused", "confused", "stressed", "suspicious",
                    "happy", "angry", "scared", "eureka"]
            for mood in moods:
                all_specs.append(AssetSpec(
                    f"vern_portrait_{mood}",
                    f"radio host portrait, pixel art, 64x64, mid-30s male, dark beard, warm skin tones, DSLR close-up, noir lighting, expressive eyes, {mood} expression, charismatic",
                    64, 64, "characters", "vern_portrait",
                    variations=3
                ))

        if subcategory in (None, "vern_sprite"):
            all_specs.append(AssetSpec(
                "vern_standing",
                "isometric character, radio host Vern, standing pose, pixel art, 32x48, dark jacket, office clothes, top-down view, ready to walk, noir atmosphere",
                32, 48, "characters", "vern_sprite"
            ))

        if subcategory in (None, "caller_silhouette"):
            archetypes = ["generic_male", "generic_female", "conspiracy", "nervous", "confident",
                         "elderly", "aggressive", "shy", "excited", "panicked", "mysterious"]
            for archetype in archetypes:
                all_specs.append(AssetSpec(
                    f"caller_silhouette_{archetype}",
                    f"{archetype} caller silhouette, pixel art, 32x48, dark shadow #1a1a2a, front-facing, {archetype} body language",
                    32, 48, "characters", "caller_silhouette"
                ))

        return all_specs

    def _generate_ui_elements(self, subcategory: Optional[str]) -> List[AssetSpec]:
        """Generate UI element specifications."""
        all_specs = []

        if subcategory in (None, "panel"):
            all_specs.append(AssetSpec(
                "panel_background",
                "dark UI panel background, pixel-perfect borders, retro interface, #2a2a3a fill, subtle gradient, 1px stroke #3a3a4a, tech-panel design",
                200, 28, "ui", "panel"
            ))

        if subcategory in (None, "button"):
            states = ["default", "hover", "pressed"]
            colors = {
                "default": "dark grey #4a4a5a",
                "hover": "brighter #5a5a6a, cyan glow #00ffff edge",
                "pressed": "darker #3a3a4a, inset shadow"
            }
            for state in states:
                all_specs.append(AssetSpec(
                    f"button_{state}",
                    f"round button, pixel art, 80x28, {state} state, {colors[state]}, retro UI design",
                    80, 28, "ui", "button"
                ))

        if subcategory in (None, "indicator"):
            all_specs.append(AssetSpec(
                "indicator_onair",
                "ON AIR sign, glowing red LED, pixel art, 32x16, illuminated text, retro electronics, #ff4444 bright",
                32, 16, "ui", "indicator"
            ))
            all_specs.append(AssetSpec(
                "indicator_waiting",
                "caller waiting icon, silhouette, pixel art, 16x16, amber color #ffaa00, antenna waves",
                16, 16, "ui", "indicator"
            ))

        if subcategory in (None, "progress_bar"):
            all_specs.append(AssetSpec(
                "progress_bar",
                "horizontal progress bar, retro style, pixel art, 200x16, empty state, cyan fill indicator #00aaaa, 1px segment divisions",
                200, 16, "ui", "progress_bar"
            ))

        return all_specs

    def _generate_equipment(self, subcategory: Optional[str]) -> List[AssetSpec]:
        """Generate equipment specifications."""
        all_specs = []

        if subcategory in (None, "microphone"):
            all_specs.append(AssetSpec(
                "boom_mic",
                "vintage boom microphone, large capsule, pixel art, 24x24, retro radio studio, dark metal",
                24, 24, "equipment", "microphone"
            ))

        if subcategory in (None, "sound_board"):
            all_specs.append(AssetSpec(
                "sound_board",
                "mixing console, retro broadcast gear, pixel art, 64x32, top-down view, faders and knobs, detailed, dark surface",
                64, 32, "equipment", "sound_board"
            ))

        if subcategory in (None, "phone"):
            all_specs.append(AssetSpec(
                "phone_rotary",
                "vintage rotary telephone, dark color, pixel art, 24x32, office phone, dial mechanism visible",
                24, 32, "equipment", "phone"
            ))

        if subcategory in (None, "monitor"):
            all_specs.append(AssetSpec(
                "crt_monitor",
                "CRT monitor, pixel art, 32x32, glowing screen, beige/black bezel, retro computer, scanlines",
                32, 32, "equipment", "monitor"
            ))

        return all_specs

    def _generate_effects(self, subcategory: Optional[str]) -> List[AssetSpec]:
        """Generate effect specifications."""
        all_specs = []

        if subcategory in (None, "glow"):
            colors = ["green", "red", "cyan", "amber"]
            hex_colors = {
                "green": "#00ff44",
                "red": "#ff4444",
                "cyan": "#00ffff",
                "amber": "#ffaa00"
            }
            for color in colors:
                all_specs.append(AssetSpec(
                    f"glow_{color}",
                    f"{color} monitor glow, pixel art, 32x32, radial gradient, alpha transparency, CRT screen effect, {hex_colors[color]} at center fading to transparent",
                    32, 32, "effects", "glow"
                ))

        if subcategory in (None, "scanlines"):
            all_specs.append(AssetSpec(
                "scanlines_overlay",
                "scanlines overlay, pixel art, 64x64, horizontal lines, 2px spacing, 50% opacity, retro CRT monitor effect",
                64, 64, "effects", "scanlines"
            ))

        if subcategory in (None, "static"):
            for i in range(1, 4):
                all_specs.append(AssetSpec(
                    f"static_noise_frame_{i}",
                    f"TV static noise frame {i}, pixel art, 32x32, random white/black specks, grain texture, transparent background",
                    32, 32, "effects", "static"
                ))

        return all_specs

# ============================================================================
# AI Service Integrations
# ============================================================================

class AIService(ABC):
    """Abstract base class for AI image generation services."""

    @abstractmethod
    def generate(self, spec: AssetSpec) -> GenerationResult:
        """Generate a single asset."""
        pass

    @abstractmethod
    def validate_config(self) -> bool:
        """Check if service is properly configured."""
        pass

class LeonardoService(AIService):
    """Leonardo.ai API integration."""

    BASE_URL = "https://api.leonardo.ai/v1/"

    def __init__(self, api_key: str):
        self.api_key = api_key
        self.session = None

    def validate_config(self) -> bool:
        return bool(self.api_key)

    def generate(self, spec: AssetSpec) -> GenerationResult:
        """Generate image using Leonardo.ai."""
        start_time = time.time()

        try:
            import requests

            headers = {
                "Authorization": f"Bearer {self.api_key}",
                "Content-Type": "application/json"
            }

            # Build prompt with pixel art style
            full_prompt = f"pixel art, {spec.prompt}, 8-bit retro, crisp pixels, no anti-aliasing"

            payload = {
                "prompt": full_prompt,
                "negative_prompt": spec.negative_prompt or "blurry, anti-aliased, smooth, photo-realistic, 3D, gradient",
                "width": spec.width * 4,  # Generate at 4x for quality
                "height": spec.height * 4,
                "num_inference_steps": 30,
                "guidance_scale": spec.guidance_scale,
                "model_id": "b24e16ff-0646-49db-9f8c-79ca244166f4",  # Leonardo Pixel Art model
                "preset_style": spec.style_preset,
            }

            if spec.seed:
                payload["seed"] = spec.seed

            # Submit generation
            response = requests.post(
                self.BASE_URL + "generations",
                headers=headers,
                json=payload,
                timeout=300
            )

            if response.status_code != 200:
                error_msg = f"Leonardo API error: {response.status_code} - {response.text}"
                return GenerationResult(spec.name, Path(""), False, error_msg)

            generation_id = response.json().get("generation_id")

            # Poll for completion
            max_polls = 120  # 2 minutes max
            for _ in range(max_polls):
                status_response = requests.get(
                    self.BASE_URL + f"generations/{generation_id}",
                    headers=headers,
                    timeout=30
                )

                if status_response.status_code == 200:
                    status_data = status_response.json()
                    if status_data.get("status") == "COMPLETE":
                        image_url = status_data["generated_images"][0]["url"]
                        break
                    elif status_data.get("status") == "FAILED":
                        return GenerationResult(spec.name, Path(""), False, "Generation failed")
                time.sleep(2)
            else:
                return GenerationResult(spec.name, Path(""), False, "Generation timeout")

            # Download image
            img_response = requests.get(image_url, timeout=60)
            if img_response.status_code != 200:
                return GenerationResult(spec.name, Path(""), False, "Failed to download image")

            # Save to file
            output_dir = ASSETS_DIR / spec.category
            if spec.subcategory:
                output_dir = output_dir / spec.subcategory
            output_dir.mkdir(parents=True, exist_ok=True)

            output_path = output_dir / f"{spec.name}.png"
            with open(output_path, "wb") as f:
                f.write(img_response.content)

            generation_time = time.time() - start_time
            return GenerationResult(spec.name, output_path, True, None, spec.seed, generation_time)

        except ImportError:
            error_msg = "Requests library not installed. Run: pip install requests"
            return GenerationResult(spec.name, Path(""), False, error_msg)
        except Exception as e:
            error_msg = f"Leonardo generation error: {str(e)}"
            return GenerationResult(spec.name, Path(""), False, error_msg)

class DalleService(AIService):
    """DALL-E 3 API integration (fallback)."""

    def __init__(self, api_key: str):
        self.api_key = api_key

    def validate_config(self) -> bool:
        return bool(self.api_key)

    def generate(self, spec: AssetSpec) -> GenerationResult:
        """Generate image using DALL-E 3."""
        start_time = time.time()

        try:
            import openai

            client = openai.OpenAI(api_key=self.api_key)

            full_prompt = f"Pixel art, {spec.prompt}. 8-bit retro style, crisp pixels, no anti-aliasing, high contrast, game asset with transparent background."

            response = client.images.generate(
                model="dall-e-3",
                prompt=full_prompt,
                size="1024x1024",  # DALL-E 3 standard
                quality="standard",
                n=1,
                response_format="url"
            )

            image_url = response.data[0].url

            # Download
            import requests
            img_response = requests.get(image_url, timeout=60)
            if img_response.status_code != 200:
                return GenerationResult(spec.name, Path(""), False, "Failed to download DALL-E image")

            # Save
            output_dir = ASSETS_DIR / spec.category
            if spec.subcategory:
                output_dir = output_dir / spec.subcategory
            output_dir.mkdir(parents=True, exist_ok=True)

            output_path = output_dir / f"{spec.name}.png"
            with open(output_path, "wb") as f:
                f.write(img_response.content)

            generation_time = time.time() - start_time
            return GenerationResult(spec.name, output_path, True, None, spec.seed, generation_time)

        except ImportError:
            error_msg = "OpenAI library not installed. Run: pip install openai"
            return GenerationResult(spec.name, Path(""), False, error_msg)
        except Exception as e:
            error_msg = f"DALL-E generation error: {str(e)}"
            return GenerationResult(spec.name, Path(""), False, error_msg)

# ============================================================================
# Generator Orchestrator
# ============================================================================

class PixelArtGenerator:
    """Main orchestrator for batch pixel art generation."""

    def __init__(self, service: AIService, dry_run: bool = False):
        self.service = service
        self.dry_run = dry_run
        self.parser = PromptParser(PROMPTS_FILE)
        self.results: List[GenerationResult] = []

    def generate_category(self, category: str, subcategory: Optional[str] = None,
                         variations: int = 1, specific_assets: Optional[List[str]] = None) -> List[GenerationResult]:
        """Generate all assets in a category."""

        specs = self.parser.parse_asset_prompts(category, subcategory)

        if specific_assets:
            specs = [s for s in specs if s.name in specific_assets]

        results = []
        for spec in specs:
            if self.dry_run:
                print(f"[DRY RUN] Would generate: {spec.name} ({spec.width}x{spec.height})")
                results.append(GenerationResult(spec.name, Path(""), True))
            else:
                print(f"Generating: {spec.name}...")
                result = self.service.generate(spec)
                results.append(result)
                status = "✓" if result.success else "✗"
                print(f"  {status} {result.filepath.name if result.success else result.error}")
                time.sleep(1)  # Rate limiting

        self.results.extend(results)
        return results

    def generate_all(self) -> List[GenerationResult]:
        """Generate all defined assets."""
        all_results = []
        categories = self.parser.categories.keys()

        for category in categories:
            print(f"\n=== Category: {category} ===")
            results = self.generate_category(category)
            all_results.extend(results)

        return all_results

    def print_summary(self):
        """Print generation summary."""
        print("\n" + "=" * 60)
        print("GENERATION SUMMARY")
        print("=" * 60)

        success_count = sum(1 for r in self.results if r.success)
        fail_count = len(self.results) - success_count
        total_time = sum(r.generation_time for r in self.results if r.success)

        print(f"Total:  {len(self.results)}")
        print(f"Success: {success_count}")
        print(f"Failed:  {fail_count}")
        print(f"Time:    {total_time:.1f}s")

        if fail_count > 0:
            print("\nFailed generations:")
            for r in self.results:
                if not r.success:
                    print(f"  - {r.asset_name}: {r.error}")

# ============================================================================
# CLI Interface
# ============================================================================

def main():
    parser = argparse.ArgumentParser(
        description="KBTV Pixel Art Batch Generator",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  %(prog)s --category tiles --asset floor
  %(prog)s --category furniture --subcategory desk
  %(prog)s --all
  %(prog)s --category characters --subcategory vern_portrait
        """
    )

    parser.add_argument("--category", help="Asset category (tiles, furniture, characters, ui, equipment, effects)")
    parser.add_argument("--subcategory", help="Subcategory within the category")
    parser.add_argument("--asset", help="Specific asset name (optional)")
    parser.add_argument("--all", action="store_true", help="Generate all assets")
    parser.add_argument("--service", choices=["leonardo", "dalle"], default="leonardo",
                       help="AI service to use (default: leonardo)")
    parser.add_argument("--dry-run", action="store_true",
                       help="Show what would be generated without actually calling API")
    parser.add_argument("--list-categories", action="store_true", help="List all available categories and subcategories")
    parser.add_argument("--list-assets", action="store_true", help="List assets that would be generated for given category")

    args = parser.parse_args()

    if args.list_categories:
        parser_obj = PromptParser(PROMPTS_FILE)
        print("Available categories and subcategories:\n")
        for cat, subcats in parser_obj.categories.items():
            print(f"  {cat}:")
            for subcat in subcats.keys():
                print(f"    - {subcat}")
        return

    if args.list_assets:
        if not args.category:
            print("Error: --category required for --list-assets")
            sys.exit(1)
        parser_obj = PromptParser(PROMPTS_FILE)
        specs = parser_obj.parse_asset_prompts(args.category, args.subcategory)
        print(f"Assets in category '{args.category}'" + (f" / '{args.subcategory}'" if args.subcategory else ""))
        for spec in specs:
            print(f"  - {spec.name} ({spec.width}x{spec.height})")
        return

    if not args.all and not args.category:
        parser.print_help()
        print("\nError: Must specify --category or --all")
        sys.exit(1)

    # Select service
    if args.service == "leonardo":
        if not LEONARDO_API_KEY and not args.dry_run:
            print("Error: LEONARDO_API_KEY environment variable not set")
            print("Set it: export LEONARDO_API_KEY='your_key_here'")
            sys.exit(1)
        service = LeonardoService(LEONARDO_API_KEY)
    else:  # dalle
        if not OPENAI_API_KEY and not args.dry_run:
            print("Error: OPENAI_API_KEY environment variable not set")
            print("Set it: export OPENAI_API_KEY='your_key_here'")
            sys.exit(1)
        service = DalleService(OPENAI_API_KEY)

    if not service.validate_config() and not args.dry_run:
        print(f"Error: {args.service} service not properly configured")
        sys.exit(1)

    # Create generator
    generator = PixelArtGenerator(service, dry_run=args.dry_run)

    # Run generation
    if args.all:
        print("Generating ALL assets...")
        generator.generate_all()
    else:
        specific_assets = [args.asset] if args.asset else None
        generator.generate_category(args.category, args.subcategory, specific_assets=specific_assets)

    generator.print_summary()

# ============================================================================

if __name__ == "__main__":
    main()