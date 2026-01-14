#!/usr/bin/env python3
"""
KBTV Voice Audio Generation Script

Generates voice audio files from dialogue JSON using Piper TTS.
Supports batch processing for large arc sets and Opus compression for small files.

Usage:
    python generate_audio.py --arcs-only --full-rebuild    # Process 1 arc, stop
    python generate_audio.py --arcs-only --full-rebuild --batch-count 3  # Process 3 arcs
    python generate_audio.py --arcs-only                    # Resume from last position
    python generate_audio.py --vern-only --full-rebuild    # Process Vern broadcasts
    python generate_audio.py --arcs-only --full-rebuild --reset-batch  # Reset and start over
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
from datetime import datetime


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
    opus_bitrate: int = 24000
    ogg_quality: int = 6
    normalize_target_dbfs: float = -20.0
    
    # Silence trimming
    silence_trim_enabled: bool = True
    silence_threshold_db: float = -40.0
    silence_padding_ms: int = 100
    
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
    silence = audio.get('silence_trimming', {})
    
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
    config.opus_bitrate = audio.get('opus_bitrate', 24000)
    config.ogg_quality = audio.get('ogg_quality', 6)
    config.normalize_target_dbfs = audio.get('normalize_target_dbfs', -20.0)
    
    # Silence trimming settings
    config.silence_trim_enabled = silence.get('enabled', True)
    config.silence_threshold_db = silence.get('threshold_db', -40.0)
    config.silence_padding_ms = silence.get('padding_ms', 100)
    
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
    mood: Optional[str]    # Mood variant or None for broadcast
    arc_id: Optional[str]  # Which arc this belongs to
    category: str          # "arc", "broadcast", etc.
    output_path: Path      # Where to save the audio
    voice: VoiceSettings   # Which voice to use
    tone: str              # The tone for this line (affects voice settings)


@dataclass
class GenerationManifest:
    """Tracks what's been generated for incremental updates."""
    generated: dict = field(default_factory=dict)
    processed_arcs: list = field(default_factory=list)
    last_batch_end: Optional[str] = None
    
    def save(self, path: Path):
        manifest_data = {
            'generated': self.generated,
            'processed_arcs': self.processed_arcs,
            'last_batch_end': self.last_batch_end
        }
        with open(path, 'w', encoding='utf-8') as f:
            json.dump(manifest_data, f, indent=2)
    
    @classmethod
    def load(cls, path: Path) -> 'GenerationManifest':
        if path.exists():
            with open(path, 'r', encoding='utf-8') as f:
                data = json.load(f)
                return cls(
                    generated=data.get('generated', {}),
                    processed_arcs=data.get('processed_arcs', []),
                    last_batch_end=data.get('last_batch_end')
                )
        return cls()


# =============================================================================
# DIALOGUE EXTRACTION
# =============================================================================

# Vern mood types to generate audio for
VERN_MOOD_TYPES = ['tired', 'energized', 'irritated', 'amused', 'gruff', 'focused', 'neutral']

# Vern mood type to voice adjustments (speed/pitch)
VERN_MOOD_SETTINGS = {
    'tired': {'speed': 0.85, 'pitch': 0.95},
    'energized': {'speed': 1.15, 'pitch': 1.05},
    'irritated': {'speed': 0.95, 'pitch': 1.0},
    'amused': {'speed': 1.05, 'pitch': 1.03},
    'gruff': {'speed': 0.90, 'pitch': 0.97},
    'focused': {'speed': 1.0, 'pitch': 1.0},
    'neutral': {'speed': 1.0, 'pitch': 1.0},
}


def extract_lines_from_arc(
    arc_path: Path,
    config: Config,
    topic: str,
    mood_filter: Optional[str] = None
) -> tuple[list[DialogueLine], str, str]:
    """
    Extract all dialogue lines from a conversation arc file.

    Returns (lines, caller_voice_name, arc_id) tuple.
    """
    with open(arc_path, 'r', encoding='utf-8') as f:
        arc = json.load(f)

    lines = []
    arc_id = arc.get('arcId', arc_path.stem)
    dialogue = arc.get('dialogue', [])

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

    # Extract all lines from the flat dialogue array
    line_index = 0
    for entry in dialogue:
        line_index += 1
        speaker = entry.get('speaker', '')
        speaker_is_vern = speaker == 'Vern'

        if speaker_is_vern:
            text_variants = entry.get('textVariants', {})
            if not text_variants:
                continue

            if mood_filter:
                moods_to_generate = [mood_filter] if mood_filter in text_variants else []
            else:
                moods_to_generate = list(VERN_MOOD_TYPES)

            for mood_type in moods_to_generate:
                text = text_variants.get(mood_type, '')
                if not text:
                    continue

                processed = process_text_for_tts(text, config)
                if not processed:
                    continue

                mood_adj = VERN_MOOD_SETTINGS.get(mood_type, {})
                vern_voice = VoiceSettings(
                    model=config.vern_voice.model,
                    speed=mood_adj.get('speed', 1.0),
                    pitch=mood_adj.get('pitch', 1.0)
                )

                line_id = f"{arc_id}_{mood_type}_{line_index:03d}_vern"

                output_path = (
                    config.output_dir /
                    'Callers' / topic / arc_id / mood_type /
                    f"{line_id}.{config.output_format}"
                )

                lines.append(DialogueLine(
                    line_id=line_id,
                    text=text,
                    processed_text=processed,
                    speaker=speaker,
                    mood=mood_type,
                    arc_id=arc_id,
                    category='arc',
                    output_path=output_path,
                    voice=vern_voice,
                    tone=mood_type
                ))

        else:
            text = entry.get('text', '')
            if not text:
                continue

            processed = process_text_for_tts(text, config)
            if not processed:
                continue

            line_id = f"{arc_id}_caller_{line_index:03d}_caller"

            output_path = (
                config.output_dir /
                'Callers' / topic / arc_id / 'Caller' /
                f"{line_id}.{config.output_format}"
            )

            lines.append(DialogueLine(
                line_id=line_id,
                text=text,
                processed_text=processed,
                speaker=speaker,
                mood='neutral',
                arc_id=arc_id,
                category='arc',
                output_path=output_path,
                voice=caller_voice,
                tone='neutral'
            ))

    return lines, caller_voice_name, arc_id


def extract_vern_broadcast_lines(config: Config) -> list[DialogueLine]:
    """Extract Vern's broadcast lines (openings, closings, fillers, etc.)."""
    if not config.vern_dialogue.exists():
        print(f"Warning: Vern dialogue file not found: {config.vern_dialogue}")
        return []
    
    with open(config.vern_dialogue, 'r', encoding='utf-8') as f:
        data = json.load(f)
    
    lines = []
    vern_voice = config.vern_voice
    
    category_mapping = {
        'showOpeningLines': 'Opening',
        'showClosingLines': 'Closing',
        'betweenCallersLines': 'BetweenCallers',
        'deadAirFillerLines': 'DeadAirFiller',
        'droppedCallerLines': 'DroppedCaller',
        'breakTransitionLines': 'Break',
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
                voice=vern_voice,
                tone='broadcast'
            ))
    
    return lines


def collect_all_arc_files(config: Config) -> list[tuple[Path, str, str]]:
    """Find all arc JSON files with their topics and arc IDs."""
    arc_files = []
    
    if not config.dialogue_arcs.exists():
        print(f"Warning: Dialogue arcs directory not found: {config.dialogue_arcs}")
        return []
    
    for topic_dir in config.dialogue_arcs.iterdir():
        if not topic_dir.is_dir():
            continue
        topic = topic_dir.name
        
        for legitimacy_dir in topic_dir.iterdir():
            if not legitimacy_dir.is_dir():
                continue
            
            for arc_file in legitimacy_dir.glob('*.json'):
                try:
                    with open(arc_file, 'r', encoding='utf-8') as f:
                        arc_data = json.load(f)
                    arc_id = arc_data.get('arcId', arc_file.stem)
                    arc_files.append((arc_file, topic, arc_id))
                except Exception as e:
                    print(f"  Warning: Could not read {arc_file.name}: {e}")
    
    return arc_files


def delete_existing_audio(
    config: Config,
    vern_only: bool = False
) -> int:
    """Delete existing audio files for the specified scope."""
    deleted_count = 0
    
    if vern_only:
        vern_broadcast_dir = config.output_dir / 'Vern' / 'Broadcast'
        if vern_broadcast_dir.exists():
            for folder in vern_broadcast_dir.iterdir():
                if folder.is_dir():
                    for file in folder.glob('*'):
                        if file.is_file():
                            file.unlink()
                            deleted_count += 1
            print(f"Deleted {deleted_count} Vern broadcast audio files")
    else:
        callers_dir = config.output_dir / 'Callers'
        if callers_dir.exists():
            for topic_dir in callers_dir.iterdir():
                if topic_dir.is_dir():
                    for arc_dir in topic_dir.iterdir():
                        if arc_dir.is_dir():
                            for mood_folder in arc_dir.iterdir():
                                if mood_folder.is_dir():
                                    for file in mood_folder.glob('*'):
                                        if file.is_file():
                                            file.unlink()
                                            deleted_count += 1
            if deleted_count > 0:
                print(f"Deleted {deleted_count} conversation audio files")
    
    return deleted_count


# =============================================================================
# AUDIO GENERATION (PIPER TTS)
# =============================================================================

def check_piper_installed() -> bool:
    """Check if Piper TTS is available."""
    try:
        result = subprocess.run(
            [sys.executable, '-m', 'piper', '--help'],
            capture_output=True,
            text=True
        )
        return result.returncode == 0
    except FileNotFoundError:
        return False


def resolve_model_path(model_name: str) -> str:
    """Resolve a model name to a full path."""
    if '/' in model_name or '\\' in model_name or model_name.endswith('.onnx'):
        return model_name
    
    voices_dir = Path(__file__).parent / "voices"
    local_model = voices_dir / f"{model_name}.onnx"
    
    if local_model.exists():
        return str(local_model)
    
    return model_name


def generate_audio_piper(
    text: str,
    output_path: Path,
    voice: VoiceSettings,
    config: Config
) -> bool:
    """Generate audio using Piper TTS."""
    output_path.parent.mkdir(parents=True, exist_ok=True)
    wav_path = output_path.with_suffix('.wav')
    
    try:
        model_path = resolve_model_path(voice.model)
        
        cmd = [
            sys.executable, '-m', 'piper',
            '--model', model_path,
            '--output_file', str(wav_path)
        ]
        
        if voice.speed != 1.0:
            length_scale = 1.0 / voice.speed
            cmd.extend(['--length_scale', str(length_scale)])
        
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
    """Normalize audio volume using pydub."""
    try:
        from pydub import AudioSegment
        
        audio = AudioSegment.from_wav(str(wav_path))
        change_in_dbfs = target_dbfs - audio.dBFS
        normalized = audio.apply_gain(change_in_dbfs)
        normalized.export(str(wav_path), format='wav')
        return True
        
    except ImportError:
        print("  Warning: pydub not installed, skipping normalization")
        return True
    except Exception as e:
        print(f"  Error normalizing audio: {e}")
        return False


def shift_pitch(wav_path: Path, pitch_factor: float) -> bool:
    """Shift pitch of audio using pydub."""
    if pitch_factor == 1.0:
        return True
    
    try:
        from pydub import AudioSegment
        
        audio = AudioSegment.from_wav(str(wav_path))
        original_rate = audio.frame_rate
        shifted = audio._spawn(audio.raw_data, overrides={
            'frame_rate': int(audio.frame_rate * pitch_factor)
        })
        shifted = shifted.set_frame_rate(original_rate)
        shifted.export(str(wav_path), format='wav')
        return True
        
    except ImportError:
        print("  Warning: pydub not installed, skipping pitch shift")
        return True
    except Exception as e:
        print(f"  Error shifting pitch: {e}")
        return False


def trim_silence(wav_path: Path, threshold_db: float, padding_ms: int) -> float:
    """
    Trim silence from beginning and end of audio file.
    Returns the amount of silence trimmed in seconds.
    """
    try:
        from pydub import AudioSegment
        
        audio = AudioSegment.from_wav(str(wav_path))
        
        # Calculate silence threshold as AudioSegment
        threshold = AudioSegment.silent(duration=1, frame_rate=22050).apply_gain(threshold_db)
        threshold_val = threshold.dBFS
        
        # Find start (skip leading silence)
        start_trim = 0
        for i, sample in enumerate(audio):
            if sample.dBFS > threshold_val:
                start_trim = i
                break
        
        # Find end (skip trailing silence)
        end_trim = len(audio)
        for i in range(len(audio) - 1, -1, -1):
            if audio[i].dBFS > threshold_val:
                end_trim = i + 1
                break
        
        # Apply padding
        start_trim = max(0, start_trim - padding_ms)
        end_trim = min(len(audio), end_trim + padding_ms)
        
        # Trim
        trimmed = audio[start_trim:end_trim]
        trimmed.export(str(wav_path), format='wav')
        
        # Return seconds trimmed
        original_duration = len(audio) / 1000.0
        trimmed_duration = len(trimmed) / 1000.0
        return max(0, original_duration - trimmed_duration)
        
    except ImportError:
        return 0.0
    except Exception as e:
        print(f"  Warning: Error trimming silence: {e}")
        return 0.0


def convert_to_opus(wav_path: Path, opus_path: Path, bitrate: int) -> bool:
    """Convert WAV to OGG Opus using ffmpeg."""
    try:
        cmd = [
            'ffmpeg',
            '-y',
            '-i', str(wav_path),
            '-c:a', 'libopus',
            '-b:a', str(bitrate),
            '-vbr', 'on',
            str(opus_path)
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
        
        wav_path.unlink()
        return opus_path.exists()
        
    except FileNotFoundError:
        print("  Error: ffmpeg not found in PATH")
        return False
    except Exception as e:
        print(f"  Error converting to Opus: {e}")
        return False


def convert_to_ogg(wav_path: Path, ogg_path: Path, quality: int) -> bool:
    """Convert WAV to OGG Vorbis using ffmpeg."""
    try:
        cmd = [
            'ffmpeg',
            '-y',
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
    voice: Optional[VoiceSettings] = None
) -> tuple[bool, float]:
    """
    Post-process generated audio: pitch shift, normalize, trim silence, convert.
    
    Returns (success, silence_trimmed_seconds).
    """
    wav_path = output_path.with_suffix('.wav')
    
    if not wav_path.exists():
        return False, 0.0
    
    silence_trimmed = 0.0
    
    if voice and voice.pitch != 1.0:
        if not shift_pitch(wav_path, voice.pitch):
            return False, 0.0
    
    if not normalize_audio(wav_path, config.normalize_target_dbfs):
        return False, 0.0
    
    if config.silence_trim_enabled:
        silence_trimmed = trim_silence(
            wav_path,
            config.silence_threshold_db,
            config.silence_padding_ms
        )
    
    if config.output_format == 'opus':
        return convert_to_opus(wav_path, output_path, config.opus_bitrate), silence_trimmed
    elif config.output_format == 'ogg':
        return convert_to_ogg(wav_path, output_path, config.ogg_quality), silence_trimmed
    elif config.output_format == 'wav':
        if wav_path != output_path:
            wav_path.rename(output_path)
        return True, silence_trimmed
    else:
        print(f"  Unsupported output format: {config.output_format}")
        return False, 0.0


# =============================================================================
# MAIN GENERATION ORCHESTRATION
# =============================================================================

def generate_line(
    line: DialogueLine,
    config: Config,
    manifest: GenerationManifest,
    force: bool = False
) -> tuple[bool, float]:
    """
    Generate audio for a single dialogue line.
    
    Returns (success, silence_trimmed_seconds).
    """
    import hashlib
    
    text_hash = hashlib.md5(line.processed_text.encode()).hexdigest()
    if not force and line.line_id in manifest.generated:
        if manifest.generated[line.line_id] == text_hash:
            if line.output_path.exists():
                return True, 0.0
    
    if not generate_audio_piper(line.processed_text, line.output_path, line.voice, config):
        return False, 0.0
    
    success, silence_trimmed = process_generated_audio(line.output_path, config, line.voice)
    
    if success:
        manifest.generated[line.line_id] = text_hash
    
    return success, silence_trimmed


def run_generation(
    config: Config,
    arcs_only: bool = False,
    vern_only: bool = False,
    dry_run: bool = False,
    force: bool = False,
    full_rebuild: bool = False,
    reset_batch: bool = False,
    batch_count: int = 1,
    verbose: bool = False
) -> tuple[int, int, int]:
    """
    Run the audio generation pipeline in batch mode.
    
    Returns (success_count, failure_count, arcs_processed).
    """
    # Load manifest
    manifest = GenerationManifest.load(config.manifest_path)
    
    # Handle full rebuild
    if full_rebuild and not dry_run:
        print("Full rebuild: deleting existing audio files...")
        deleted = delete_existing_audio(config, vern_only=vern_only)
        if deleted > 0:
            print(f"  Deleted {deleted} files")
        
        if reset_batch:
            manifest.processed_arcs = []
            print("Cleared arc tracking (ready to start fresh)")
    
    # Collect arc files
    arc_files = []
    all_arcs = []
    
    if not vern_only:
        print("Collecting conversation arc lines...")
        all_arcs = collect_all_arc_files(config)
        
        # Filter out already-processed arcs
        for arc_path, topic, arc_id in all_arcs:
            if arc_id not in manifest.processed_arcs:
                arc_files.append((arc_path, topic, arc_id))
        
        total_arcs = len(all_arcs)
        remaining = len(arc_files)
        print(f"  Found {total_arcs} arcs, {remaining} remaining")
        
        if not arc_files:
            print("  All arcs already processed! Use --full-rebuild --reset-batch to start over.")
            return 0, 0, 0
    
    # Collect Vern broadcast lines
    all_lines = []
    if not arcs_only:
        print("Collecting Vern broadcast lines...")
        try:
            vern_lines = extract_vern_broadcast_lines(config)
            all_lines.extend(vern_lines)
            if verbose:
                print(f"  VernDialogue: {len(vern_lines)} lines")
        except Exception as e:
            print(f"  Error reading Vern dialogue: {e}")
    
    # If we have arc files and batch mode
    if arc_files and batch_count > 0:
        # Take only batch_count arcs
        batch_arcs = arc_files[:batch_count]
        
        print(f"\nProcessing batch of {len(batch_arcs)} arc(s)...")
        
        for arc_path, topic, arc_id in batch_arcs:
            if verbose:
                print(f"\n  [{len(manifest.processed_arcs) + 1}/{len(all_arcs)}] {arc_id}")
            
            try:
                lines, voice_name, _ = extract_lines_from_arc(arc_path, config, topic)
                all_lines.extend(lines)
            except Exception as e:
                print(f"  Error reading {arc_path}: {e}")
        
        if verbose and batch_arcs:
            print(f"  Batch arcs: {[a[2] for a in batch_arcs]}")
    
    if not all_lines:
        print("No lines to process.")
        return 0, 0, 0
    
    print(f"\nTotal lines to process: {len(all_lines)}")
    
    if dry_run:
        print("\n--- DRY RUN - No audio will be generated ---\n")
        vern_count = sum(1 for l in all_lines if l.speaker == 'Vern')
        caller_count = sum(1 for l in all_lines if l.speaker != 'Vern')
        print(f"  Would process: {len(all_lines)} lines (Vern: {vern_count}, Caller: {caller_count})")
        
        if arc_files and batch_count > 0:
            batch_arcs = arc_files[:batch_count]
            print(f"  Batch arcs ({len(batch_arcs)}): {[a[2] for a in batch_arcs]}")
        
        for line in all_lines[:10]:
            print(f"  [{line.line_id}] {line.processed_text[:50]}...")
        if len(all_lines) > 10:
            print(f"  ... and {len(all_lines) - 10} more lines")
        return (0, 0, 0)
    
    success_count = 0
    failure_count = 0
    total_silence_trimmed = 0.0
    
    print("\nGenerating audio...")
    for i, line in enumerate(all_lines):
        progress = f"[{i+1}/{len(all_lines)}]"
        
        if verbose:
            print(f"{progress} {line.line_id}")
        else:
            if (i + 1) % 10 == 0 or i == 0:
                print(f"{progress} Processing...")
        
        success, silence_trimmed = generate_line(line, config, manifest, force)
        total_silence_trimmed += silence_trimmed
        
        if success:
            success_count += 1
        else:
            failure_count += 1
            print(f"  FAILED: {line.line_id}")
    
    # Save manifest
    config.manifest_path.parent.mkdir(parents=True, exist_ok=True)
    manifest.last_batch_end = datetime.now().isoformat()
    
    # Mark processed arcs
    batch_arcs = []
    if arc_files and batch_count > 0:
        batch_arcs = arc_files[:batch_count]
        for _, _, arc_id in batch_arcs:
            if arc_id not in manifest.processed_arcs:
                manifest.processed_arcs.append(arc_id)
    
    manifest.save(config.manifest_path)
    
    # Calculate stats
    arcs_processed = len(batch_arcs)
    total_arcs = len(all_arcs)
    completed_arcs = len(manifest.processed_arcs)
    
    if total_arcs > 0:
        progress_pct = (completed_arcs / total_arcs) * 100
        print(f"\nGeneration complete!")
        print(f"  Success: {success_count}")
        print(f"  Failed:  {failure_count}")
        if total_silence_trimmed > 0:
            print(f"  Silence trimmed: {total_silence_trimmed:.1f}s total")
        if total_arcs > 0:
            print(f"  Progress: {completed_arcs}/{total_arcs} arcs ({progress_pct:.1f}%)")
            if completed_arcs < total_arcs:
                remaining = total_arcs - completed_arcs
                print(f"  Remaining: {remaining} arcs")
                print(f"  Next run: python generate_audio.py --arcs-only")
    else:
        print(f"\nGeneration complete!")
        print(f"  Success: {success_count}")
        print(f"  Failed:  {failure_count}")
        if total_silence_trimmed > 0:
            print(f"  Silence trimmed: {total_silence_trimmed:.1f}s total")
    
    return success_count, failure_count, arcs_processed


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
        '--dry-run',
        action='store_true',
        help='List lines without generating audio'
    )
    
    parser.add_argument(
        '--full-rebuild', '-F',
        action='store_true',
        help='Delete all existing audio files before generating'
    )
    
    parser.add_argument(
        '--reset-batch', '-R',
        action='store_true',
        help='Clear processed arc tracking (use with --full-rebuild)'
    )
    
    parser.add_argument(
        '--batch-count', '-b',
        type=int,
        default=1,
        help='Number of arcs to process per batch (default: 1)'
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
    
    args = parser.parse_args()
    
    # Validate arguments
    if args.arcs_only and args.vern_only:
        print("Error: Cannot use both --arcs-only and --vern-only")
        return 1
    
    if args.batch_count < 1:
        print("Error: --batch-count must be >= 1")
        return 1
    
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
    success, failures, arcs = run_generation(
        config=config,
        arcs_only=args.arcs_only,
        vern_only=args.vern_only,
        dry_run=args.dry_run,
        force=args.force,
        full_rebuild=args.full_rebuild,
        reset_batch=args.reset_batch,
        batch_count=args.batch_count,
        verbose=args.verbose
    )
    
    return 0 if failures == 0 else 1


if __name__ == '__main__':
    sys.exit(main())
