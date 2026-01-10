#!/usr/bin/env python3
"""
KBTV Voice Audio Generation Script

Generates voice audio files from dialogue JSON using Piper TTS.
See docs/VOICE_AUDIO.md for full documentation.

Usage:
    python generate_audio.py                    # Generate all audio
    python generate_audio.py --arcs-only        # Only conversation arcs
    python generate_audio.py --vern-only        # Only Vern broadcasts
    python generate_audio.py --dry-run          # List lines without generating
    python generate_audio.py --arc ufo_credible_dashcam  # Single arc
"""

import argparse
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional


# =============================================================================
# CONFIGURATION
# =============================================================================

@dataclass
class VoiceSettings:
    """Settings for a voice model."""
    model: str
    speed: float = 1.0
    pitch: float = 1.0
    description: str = ""


@dataclass
class Config:
    """Script configuration loaded from config.json."""
    # Paths
    dialogue_arcs: Path = field(default_factory=Path)
    vern_dialogue: Path = field(default_factory=Path)
    output_dir: Path = field(default_factory=Path)
    temp_dir: Path = field(default_factory=Path)
    manifest_path: Path = field(default_factory=Path)
    
    # Audio settings
    sample_rate: int = 22050
    output_format: str = "ogg"
    ogg_quality: int = 6
    normalize_target_dbfs: float = -20.0
    
    # Voice settings
    vern_voice: VoiceSettings = field(default_factory=lambda: VoiceSettings("en_US-ryan-medium"))
    vern_mood_adjustments: dict = field(default_factory=dict)
    caller_voices: dict = field(default_factory=dict)
    caller_archetype_mapping: dict = field(default_factory=dict)
    
    # Text processing
    placeholder_replacements: dict = field(default_factory=dict)
    stage_direction_pattern: str = r"\*[^*]+\*"


def load_config(config_path: Path) -> Config:
    """Load configuration from JSON file."""
    with open(config_path, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    base_path = config_path.parent
    paths = data.get('paths', {})
    audio = data.get('audio', {})
    voices = data.get('voices', {})
    text_proc = data.get('text_processing', {})
    
    config = Config()
    
    # Resolve paths relative to config file location
    config.dialogue_arcs = (base_path / paths.get('dialogue_arcs', '')).resolve()
    config.vern_dialogue = (base_path / paths.get('vern_dialogue', '')).resolve()
    config.output_dir = (base_path / paths.get('output_dir', '')).resolve()
    config.temp_dir = (base_path / paths.get('temp_dir', './temp')).resolve()
    config.manifest_path = (base_path / paths.get('manifest', '')).resolve()
    
    # Audio settings
    config.sample_rate = audio.get('sample_rate', 22050)
    config.output_format = audio.get('output_format', 'ogg')
    config.ogg_quality = audio.get('ogg_quality', 6)
    config.normalize_target_dbfs = audio.get('normalize_target_dbfs', -20.0)
    
    # Vern voice
    vern = voices.get('vern', {})
    config.vern_voice = VoiceSettings(
        model=vern.get('model', 'en_US-ryan-medium'),
        description=vern.get('description', '')
    )
    config.vern_mood_adjustments = vern.get('mood_adjustments', {})
    
    # Caller voices
    callers = voices.get('callers', {})
    for name, settings in callers.items():
        config.caller_voices[name] = VoiceSettings(
            model=settings.get('model', 'en_US-lessac-medium'),
            speed=settings.get('speed', 1.0),
            pitch=settings.get('pitch', 1.0),
            description=settings.get('description', '')
        )
    
    config.caller_archetype_mapping = voices.get('caller_archetype_mapping', {})
    
    # Text processing
    config.placeholder_replacements = text_proc.get('placeholder_replacements', {})
    config.stage_direction_pattern = text_proc.get('stage_direction_pattern', r"\*[^*]+\*")
    
    return config


# =============================================================================
# TEXT PROCESSING
# =============================================================================

def process_text_for_tts(text: str, config: Config) -> str:
    """
    Process dialogue text for TTS generation.
    
    - Replaces placeholders like {callerName} with generic text
    - Removes stage directions like *yawns*
    - Cleans up punctuation for better TTS
    """
    result = text
    
    # Replace placeholders
    for placeholder, replacement in config.placeholder_replacements.items():
        result = result.replace(placeholder, replacement)
    
    # Remove stage directions (e.g., *yawns*, *sighs*)
    result = re.sub(config.stage_direction_pattern, '', result)
    
    # Clean up extra whitespace
    result = re.sub(r'\s+', ' ', result).strip()
    
    # Clean up punctuation artifacts from removals
    result = re.sub(r'\s+([.,!?])', r'\1', result)
    result = re.sub(r'^[.,]\s*', '', result)
    
    return result


def sanitize_filename(text: str, max_length: int = 50) -> str:
    """Create a safe filename from text."""
    # Remove special characters
    safe = re.sub(r'[^\w\s-]', '', text.lower())
    # Replace spaces with underscores
    safe = re.sub(r'\s+', '_', safe)
    # Truncate
    return safe[:max_length]


def select_caller_voice(arc_id: str, voice_options: list[str]) -> str:
    """
    Select a caller voice deterministically based on arc_id.
    
    Uses hash of arc_id to pick from available voices, ensuring:
    - Same arc always gets the same voice (reproducible)
    - Different arcs get variety across available voices
    """
    import hashlib
    hash_int = int(hashlib.md5(arc_id.encode()).hexdigest(), 16)
    return voice_options[hash_int % len(voice_options)]


# =============================================================================
# DATA STRUCTURES
# =============================================================================

@dataclass
class DialogueLine:
    """A single dialogue line to generate audio for."""
    line_id: str           # Unique ID for filename
    text: str              # Raw dialogue text
    processed_text: str    # Text after processing for TTS
    speaker: str           # "Vern" or "Caller"
    mood: Optional[str]    # Mood variant (Tired, Grumpy, etc.)
    arc_id: Optional[str]  # Which arc this belongs to
    category: str          # "arc", "broadcast", etc.
    output_path: Path      # Where to save the audio
    voice: VoiceSettings   # Which voice to use
    

@dataclass
class GenerationManifest:
    """Tracks what's been generated for incremental updates."""
    generated: dict = field(default_factory=dict)  # line_id -> hash of text
    
    def save(self, path: Path):
        with open(path, 'w', encoding='utf-8') as f:
            json.dump({'generated': self.generated}, f, indent=2)
    
    @classmethod
    def load(cls, path: Path) -> 'GenerationManifest':
        if path.exists():
            with open(path, 'r', encoding='utf-8') as f:
                data = json.load(f)
                return cls(generated=data.get('generated', {}))
        return cls()


# =============================================================================
# DIALOGUE EXTRACTION
# =============================================================================

def extract_lines_from_arc(
    arc_path: Path, 
    config: Config,
    topic: str
) -> tuple[list[DialogueLine], str]:
    """
    Extract all dialogue lines from a conversation arc file.
    
    Returns (lines, caller_voice_name) tuple.
    """
    with open(arc_path, 'r', encoding='utf-8') as f:
        arc = json.load(f)
    
    lines = []
    arc_id = arc.get('arcId', arc_path.stem)
    mood_variants = arc.get('moodVariants', {})
    
    # Pick a caller voice based on topic (deterministic random from arc_id hash)
    caller_archetypes = config.caller_archetype_mapping.get(topic, ['default_male'])
    if caller_archetypes:
        caller_voice_name = select_caller_voice(arc_id, caller_archetypes)
    else:
        caller_voice_name = 'default_male'
    caller_voice = config.caller_voices.get(
        caller_voice_name, 
        VoiceSettings('en_US-lessac-medium')
    )
    
    for mood, variant in mood_variants.items():
        # Get Vern's mood adjustments
        mood_adj = config.vern_mood_adjustments.get(mood, {})
        vern_voice = VoiceSettings(
            model=config.vern_voice.model,
            speed=mood_adj.get('speed', 1.0),
            pitch=mood_adj.get('pitch', 1.0)
        )
        
        line_index = 0
        
        # Process each phase
        for phase in ['intro', 'development', 'conclusion']:
            phase_lines = variant.get(phase, [])
            for entry in phase_lines:
                line_index += 1
                speaker = entry.get('speaker', 'Unknown')
                text = entry.get('text', '')
                processed = process_text_for_tts(text, config)
                
                if not processed:
                    continue
                
                voice = vern_voice if speaker == 'Vern' else caller_voice
                speaker_tag = 'vern' if speaker == 'Vern' else 'caller'
                
                line_id = f"{arc_id}_{mood.lower()}_{line_index:03d}_{speaker_tag}"
                
                # Output path: Voice/{Topic}/{arc_id}/{mood}/{filename}.ogg
                output_path = (
                    config.output_dir / 
                    'Callers' / topic / arc_id / mood /
                    f"{line_id}.{config.output_format}"
                )
                
                lines.append(DialogueLine(
                    line_id=line_id,
                    text=text,
                    processed_text=processed,
                    speaker=speaker,
                    mood=mood,
                    arc_id=arc_id,
                    category='arc',
                    output_path=output_path,
                    voice=voice
                ))
        
        # Process belief branches
        belief_branch = variant.get('beliefBranch', {})
        for belief, belief_lines in belief_branch.items():
            for entry in belief_lines:
                line_index += 1
                speaker = entry.get('speaker', 'Unknown')
                text = entry.get('text', '')
                processed = process_text_for_tts(text, config)
                
                if not processed:
                    continue
                
                voice = vern_voice if speaker == 'Vern' else caller_voice
                speaker_tag = 'vern' if speaker == 'Vern' else 'caller'
                belief_tag = belief.lower()[:4]  # 'skep' or 'beli'
                
                line_id = f"{arc_id}_{mood.lower()}_{belief_tag}_{line_index:03d}_{speaker_tag}"
                
                output_path = (
                    config.output_dir / 
                    'Callers' / topic / arc_id / mood /
                    f"{line_id}.{config.output_format}"
                )
                
                lines.append(DialogueLine(
                    line_id=line_id,
                    text=text,
                    processed_text=processed,
                    speaker=speaker,
                    mood=mood,
                    arc_id=arc_id,
                    category='arc',
                    output_path=output_path,
                    voice=voice
                ))
    
    return lines, caller_voice_name


def extract_vern_broadcast_lines(config: Config) -> list[DialogueLine]:
    """Extract Vern's broadcast lines (openings, closings, fillers, etc.)."""
    if not config.vern_dialogue.exists():
        print(f"Warning: Vern dialogue file not found: {config.vern_dialogue}")
        return []
    
    with open(config.vern_dialogue, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    lines = []
    vern_voice = config.vern_voice
    
    # Map JSON keys to output subfolders
    category_mapping = {
        'showOpeningLines': 'Opening',
        'showClosingLines': 'Closing',
        'betweenCallersLines': 'BetweenCallers',
        'deadAirFillerLines': 'DeadAirFiller',
        'droppedCallerLines': 'DroppedCaller',
        'introductionLines': 'Introduction',
        'probingLines': 'Probing',
        'extraProbingLines': 'ExtraProbing',
        'skepticalLines': 'Skeptical',
        'dismissiveLines': 'Dismissive',
        'believingLines': 'Believing',
        'tiredLines': 'Tired',
        'annoyedLines': 'Annoyed',
        'engagingLines': 'Engaging',
        'cutOffLines': 'CutOff',
        'signOffLines': 'SignOff'
    }
    
    for json_key, folder_name in category_mapping.items():
        entries = data.get(json_key, [])
        for i, entry in enumerate(entries):
            text = entry.get('text', '')
            processed = process_text_for_tts(text, config)
            
            if not processed:
                continue
            
            line_id = f"vern_{folder_name.lower()}_{i+1:03d}"
            
            output_path = (
                config.output_dir /
                'Vern' / 'Broadcast' / folder_name /
                f"{line_id}.{config.output_format}"
            )
            
            lines.append(DialogueLine(
                line_id=line_id,
                text=text,
                processed_text=processed,
                speaker='Vern',
                mood=None,
                arc_id=None,
                category='broadcast',
                output_path=output_path,
                voice=vern_voice
            ))
    
    return lines


def collect_all_arc_files(config: Config) -> list[tuple[Path, str]]:
    """Find all arc JSON files and their topics."""
    arc_files = []
    
    if not config.dialogue_arcs.exists():
        print(f"Warning: Dialogue arcs directory not found: {config.dialogue_arcs}")
        return []
    
    # Walk through topic folders
    for topic_dir in config.dialogue_arcs.iterdir():
        if not topic_dir.is_dir():
            continue
        topic = topic_dir.name
        
        # Walk through legitimacy folders
        for legitimacy_dir in topic_dir.iterdir():
            if not legitimacy_dir.is_dir():
                continue
            
            # Find all JSON files
            for arc_file in legitimacy_dir.glob('*.json'):
                arc_files.append((arc_file, topic))
    
    return arc_files


# =============================================================================
# AUDIO GENERATION (PIPER TTS)
# =============================================================================

def check_piper_installed() -> bool:
    """Check if Piper TTS is available."""
    try:
        # Try as module first (more reliable on Windows)
        result = subprocess.run(
            [sys.executable, '-m', 'piper', '--help'],
            capture_output=True,
            text=True
        )
        if result.returncode == 0:
            return True
        # Fall back to direct command
        result = subprocess.run(
            ['piper', '--version'],
            capture_output=True,
            text=True
        )
        return result.returncode == 0
    except FileNotFoundError:
        return False


def resolve_model_path(model_name: str) -> str:
    """
    Resolve a model name to a full path.
    
    If the model_name is already a path (contains / or \\), return as-is.
    Otherwise, look for the model in the local voices/ directory.
    """
    # If already a path, use as-is
    if '/' in model_name or '\\' in model_name or model_name.endswith('.onnx'):
        return model_name
    
    # Look in local voices directory
    voices_dir = Path(__file__).parent / "voices"
    local_model = voices_dir / f"{model_name}.onnx"
    
    if local_model.exists():
        return str(local_model)
    
    # Fall back to model name (piper might find it in its data dir)
    return model_name


def generate_audio_piper(
    text: str,
    output_path: Path,
    voice: VoiceSettings,
    config: Config
) -> bool:
    """
    Generate audio using Piper TTS.
    
    Returns True on success, False on failure.
    """
    # Ensure output directory exists
    output_path.parent.mkdir(parents=True, exist_ok=True)
    
    # Generate to WAV first (Piper outputs WAV)
    wav_path = output_path.with_suffix('.wav')
    
    try:
        # Resolve model name to path (check local voices/ directory)
        model_path = resolve_model_path(voice.model)
        
        # Build Piper command (use python -m piper for Windows compatibility)
        cmd = [
            sys.executable, '-m', 'piper',
            '--model', model_path,
            '--output_file', str(wav_path)
        ]
        
        # Add length scale (inverse of speed) if not 1.0
        if voice.speed != 1.0:
            length_scale = 1.0 / voice.speed
            cmd.extend(['--length_scale', str(length_scale)])
        
        # Run Piper with text as stdin
        result = subprocess.run(
            cmd,
            input=text,
            capture_output=True,
            text=True,
            timeout=60
        )
        
        if result.returncode != 0:
            print(f"  Piper error: {result.stderr}")
            return False
        
        if not wav_path.exists():
            print(f"  Error: WAV file not created")
            return False
        
        return True
        
    except subprocess.TimeoutExpired:
        print(f"  Error: Piper timed out")
        return False
    except Exception as e:
        print(f"  Error generating audio: {e}")
        return False


# =============================================================================
# AUDIO POST-PROCESSING
# =============================================================================

def normalize_audio(wav_path: Path, target_dbfs: float) -> bool:
    """
    Normalize audio volume using pydub.
    
    Returns True on success, False on failure.
    """
    try:
        from pydub import AudioSegment
        
        audio = AudioSegment.from_wav(str(wav_path))
        
        # Calculate current dBFS and adjustment needed
        change_in_dbfs = target_dbfs - audio.dBFS
        normalized = audio.apply_gain(change_in_dbfs)
        
        # Export back to WAV
        normalized.export(str(wav_path), format='wav')
        return True
        
    except ImportError:
        print("  Warning: pydub not installed, skipping normalization")
        return True  # Continue without normalization
    except Exception as e:
        print(f"  Error normalizing audio: {e}")
        return False


def shift_pitch(wav_path: Path, pitch_factor: float) -> bool:
    """
    Shift pitch of audio using pydub.
    
    pitch_factor < 1.0 = lower pitch
    pitch_factor > 1.0 = higher pitch
    
    Returns True on success, False on failure.
    """
    if pitch_factor == 1.0:
        return True  # No change needed
    
    try:
        from pydub import AudioSegment
        
        audio = AudioSegment.from_wav(str(wav_path))
        
        # Shift pitch by changing frame rate, then resampling back
        # Lower pitch = lower frame rate during processing
        original_rate = audio.frame_rate
        shifted = audio._spawn(audio.raw_data, overrides={
            'frame_rate': int(audio.frame_rate * pitch_factor)
        })
        # Resample back to original rate
        shifted = shifted.set_frame_rate(original_rate)
        
        shifted.export(str(wav_path), format='wav')
        return True
        
    except ImportError:
        print("  Warning: pydub not installed, skipping pitch shift")
        return True
    except Exception as e:
        print(f"  Error shifting pitch: {e}")
        return False


def convert_to_ogg(wav_path: Path, ogg_path: Path, quality: int) -> bool:
    """
    Convert WAV to OGG using ffmpeg.
    
    Returns True on success, False on failure.
    """
    try:
        cmd = [
            'ffmpeg',
            '-y',  # Overwrite output
            '-i', str(wav_path),
            '-c:a', 'libvorbis',
            '-q:a', str(quality),
            str(ogg_path)
        ]
        
        result = subprocess.run(
            cmd,
            capture_output=True,
            text=True,
            timeout=30
        )
        
        if result.returncode != 0:
            print(f"  ffmpeg error: {result.stderr}")
            return False
        
        # Clean up WAV file
        wav_path.unlink()
        
        return ogg_path.exists()
        
    except FileNotFoundError:
        print("  Error: ffmpeg not found in PATH")
        return False
    except Exception as e:
        print(f"  Error converting to OGG: {e}")
        return False


def process_generated_audio(
    output_path: Path,
    config: Config,
    voice: VoiceSettings = None
) -> bool:
    """
    Post-process generated audio: pitch shift, normalize, and convert.
    
    Returns True on success.
    """
    wav_path = output_path.with_suffix('.wav')
    
    if not wav_path.exists():
        return False
    
    # Apply pitch shift if specified
    if voice and voice.pitch != 1.0:
        if not shift_pitch(wav_path, voice.pitch):
            return False
    
    # Normalize volume
    if not normalize_audio(wav_path, config.normalize_target_dbfs):
        return False
    
    # Convert to output format
    if config.output_format == 'ogg':
        return convert_to_ogg(wav_path, output_path, config.ogg_quality)
    elif config.output_format == 'wav':
        # Just rename if keeping as WAV
        if wav_path != output_path:
            wav_path.rename(output_path)
        return True
    else:
        print(f"  Unsupported output format: {config.output_format}")
        return False


# =============================================================================
# MAIN GENERATION ORCHESTRATION
# =============================================================================

def generate_line(
    line: DialogueLine,
    config: Config,
    manifest: GenerationManifest,
    force: bool = False
) -> bool:
    """
    Generate audio for a single dialogue line.
    
    Skips if already generated (unless force=True).
    Returns True on success.
    """
    import hashlib
    
    # Check if already generated and unchanged
    text_hash = hashlib.md5(line.processed_text.encode()).hexdigest()
    if not force and line.line_id in manifest.generated:
        if manifest.generated[line.line_id] == text_hash:
            if line.output_path.exists():
                return True  # Already up to date
    
    # Generate audio
    if not generate_audio_piper(line.processed_text, line.output_path, line.voice, config):
        return False
    
    # Post-process (with pitch from voice settings)
    if not process_generated_audio(line.output_path, config, line.voice):
        return False
    
    # Update manifest
    manifest.generated[line.line_id] = text_hash
    return True


def run_generation(
    config: Config,
    arcs_only: bool = False,
    vern_only: bool = False,
    arc_filter: Optional[str] = None,
    dry_run: bool = False,
    force: bool = False,
    verbose: bool = False,
    limit: Optional[int] = None
) -> tuple[int, int]:
    """
    Run the full audio generation pipeline.
    
    Returns (success_count, failure_count).
    """
    all_lines: list[DialogueLine] = []
    
    # Collect arc lines
    if not vern_only:
        print("Collecting conversation arc lines...")
        arc_files = collect_all_arc_files(config)
        for arc_path, topic in arc_files:
            if arc_filter and arc_filter not in str(arc_path):
                continue
            try:
                lines, voice_name = extract_lines_from_arc(arc_path, config, topic)
                all_lines.extend(lines)
                if verbose:
                    print(f"  {arc_path.stem}: {len(lines)} lines (voice: {voice_name})")
            except Exception as e:
                print(f"  Error reading {arc_path}: {e}")
    
    # Collect Vern broadcast lines
    if not arcs_only:
        print("Collecting Vern broadcast lines...")
        try:
            vern_lines = extract_vern_broadcast_lines(config)
            all_lines.extend(vern_lines)
            if verbose:
                print(f"  VernDialogue: {len(vern_lines)} lines")
        except Exception as e:
            print(f"  Error reading Vern dialogue: {e}")
    
    print(f"\nTotal lines to process: {len(all_lines)}")
    
    if dry_run:
        print("\n--- DRY RUN - No audio will be generated ---\n")
        for line in all_lines[:20]:  # Show first 20
            print(f"  [{line.line_id}] {line.processed_text[:60]}...")
        if len(all_lines) > 20:
            print(f"  ... and {len(all_lines) - 20} more lines")
        return (0, 0)
    
    # Apply limit if specified
    if limit is not None and limit < len(all_lines):
        print(f"Limiting to first {limit} lines (of {len(all_lines)})")
        all_lines = all_lines[:limit]
    
    # Load manifest for incremental generation
    manifest = GenerationManifest.load(config.manifest_path)
    
    success_count = 0
    failure_count = 0
    
    print("\nGenerating audio...")
    for i, line in enumerate(all_lines):
        progress = f"[{i+1}/{len(all_lines)}]"
        
        if verbose:
            print(f"{progress} {line.line_id}")
        else:
            # Print progress every 10 lines
            if (i + 1) % 10 == 0 or i == 0:
                print(f"{progress} Processing...")
        
        if generate_line(line, config, manifest, force):
            success_count += 1
        else:
            failure_count += 1
            print(f"  FAILED: {line.line_id}")
    
    # Save manifest
    config.manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest.save(config.manifest_path)
    
    return (success_count, failure_count)


# =============================================================================
# CLI ENTRY POINT
# =============================================================================

def main():
    parser = argparse.ArgumentParser(
        description='Generate voice audio from KBTV dialogue using Piper TTS'
    )
    
    parser.add_argument(
        '--config', '-c',
        type=Path,
        default=Path(__file__).parent / 'config.json',
        help='Path to config.json (default: ./config.json)'
    )
    
    parser.add_argument(
        '--arcs-only',
        action='store_true',
        help='Only generate conversation arc audio'
    )
    
    parser.add_argument(
        '--vern-only',
        action='store_true',
        help='Only generate Vern broadcast audio'
    )
    
    parser.add_argument(
        '--arc',
        type=str,
        help='Only generate audio for a specific arc (by name or partial match)'
    )
    
    parser.add_argument(
        '--dry-run',
        action='store_true',
        help='List lines without generating audio'
    )
    
    parser.add_argument(
        '--force', '-f',
        action='store_true',
        help='Regenerate all audio (ignore manifest)'
    )
    
    parser.add_argument(
        '--verbose', '-v',
        action='store_true',
        help='Print detailed progress'
    )
    
    parser.add_argument(
        '--check-deps',
        action='store_true',
        help='Check if dependencies are installed'
    )
    
    parser.add_argument(
        '--limit', '-n',
        type=int,
        default=None,
        help='Limit generation to first N lines (for testing)'
    )
    
    args = parser.parse_args()
    
    # Check dependencies
    if args.check_deps:
        print("Checking dependencies...")
        
        piper_ok = check_piper_installed()
        print(f"  Piper TTS: {'OK' if piper_ok else 'NOT FOUND'}")
        
        try:
            import pydub
            print(f"  pydub: OK")
        except ImportError:
            print(f"  pydub: NOT FOUND (pip install pydub)")
        
        try:
            result = subprocess.run(['ffmpeg', '-version'], capture_output=True)
            print(f"  ffmpeg: {'OK' if result.returncode == 0 else 'NOT FOUND'}")
        except FileNotFoundError:
            print(f"  ffmpeg: NOT FOUND")
        
        return 0
    
    # Load config
    if not args.config.exists():
        print(f"Error: Config file not found: {args.config}")
        return 1
    
    try:
        config = load_config(args.config)
    except Exception as e:
        print(f"Error loading config: {e}")
        return 1
    
    # Check Piper before starting
    if not args.dry_run and not check_piper_installed():
        print("Error: Piper TTS not found. Install with: pip install piper-tts")
        print("Or run with --dry-run to see what would be generated.")
        return 1
    
    # Run generation
    success, failures = run_generation(
        config=config,
        arcs_only=args.arcs_only,
        vern_only=args.vern_only,
        arc_filter=args.arc,
        dry_run=args.dry_run,
        force=args.force,
        verbose=args.verbose,
        limit=args.limit
    )
    
    if not args.dry_run:
        print(f"\nGeneration complete!")
        print(f"  Success: {success}")
        print(f"  Failed:  {failures}")
    
    return 0 if failures == 0 else 1


if __name__ == '__main__':
    sys.exit(main())
