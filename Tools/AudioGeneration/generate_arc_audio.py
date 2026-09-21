#!/usr/bin/env python3
"""
Generate conversation-arc voice audio directly from the arc JSON file.

Audio path rules - these MUST match the runtime lookup in
scripts/dialogue/executables/DialogueExecutable.cs (and ArcAudioTopics.cs):

- The arc JSON lives at  assets/dialogue/arcs/<anything>/{arcId}.json
  (arcId field must equal the filename without extension).
- Per-line topic folder comes from the FIRST '_' token of the line id:
    ufo|ufos -> UFOs, ghosts -> Ghosts, cryptid|cryptids -> Cryptids,
    conspiracies -> Conspiracies
  Unknown tokens fall back to the JSON's "topic" field.
- Output paths:
    vern   -> assets/audio/voice/Vern/ConversationArcs/{topic}/{arcId}/{id}.mp3
    caller -> assets/audio/voice/Callers/{topic}/{arcId}/{id}.mp3

Usage:
    python generate_arc_audio.py <arcId|json-path> [options]
    python generate_arc_audio.py --all [options]
    python generate_arc_audio.py haunted_motel --check        # report only
    python generate_arc_audio.py --all --check                # audit every arc
    python generate_arc_audio.py haunted_motel --speaker vern
    python generate_arc_audio.py haunted_motel --force

Options:
    --force    regenerate files that already exist
    --check    report missing audio without calling the API (exit 1 if any)
    --all      process every arc JSON under assets/dialogue/arcs/
    --speaker  vern | caller | both (default: both)
    --verbose  list skipped/existing files too
"""

import os
import sys
import json
import glob
import time
import hashlib
import argparse

from elevenlabs_setup import ElevenLabsVoiceCloner
from generate_vern_audio import MOOD_SETTINGS

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ARCS_DIR = os.path.join(ROOT, "assets", "dialogue", "arcs")
VOICE_DIR = os.path.join(ROOT, "assets", "audio", "voice")

# Mirror of ArcAudioTopics.GetTopicFolder (scripts/dialogue/ArcAudioTopics.cs)
TOPIC_TOKENS = {
    "ufo": "UFOs",
    "ufos": "UFOs",
    "ghosts": "Ghosts",
    "cryptid": "Cryptids",
    "cryptids": "Cryptids",
    "conspiracies": "Conspiracies",
}


def topic_folder(line_id, fallback):
    token = line_id.split("_", 1)[0].lower() if line_id else ""
    return TOPIC_TOKENS.get(token, fallback)


def find_arc_json(name_or_path):
    if name_or_path.lower().endswith(".json") and os.path.isfile(name_or_path):
        return os.path.abspath(name_or_path)
    matches = glob.glob(os.path.join(ARCS_DIR, "**", f"{name_or_path}.json"), recursive=True)
    if len(matches) == 1:
        return os.path.abspath(matches[0])
    if len(matches) > 1:
        raise FileNotFoundError(f"Arc '{name_or_path}' is ambiguous: {matches}")
    raise FileNotFoundError(f"No arc JSON named '{name_or_path}.json' under {ARCS_DIR}")


def all_arc_jsons():
    return sorted(glob.glob(os.path.join(ARCS_DIR, "**", "*.json"), recursive=True))


def iter_flat_lines(arc_data):
    """Yield (speaker, line_dict) for every line entry in the arc."""
    for group in arc_data.get("arcLines", []):
        speaker = group.get("speaker", "").lower()
        for line in group.get("lines", []):
            yield speaker, line


def audio_path_for(speaker, arc_topic, arc_id, line_id):
    base = "Vern/ConversationArcs" if speaker == "vern" else "Callers"
    topic = topic_folder(line_id, arc_topic)
    return os.path.join(VOICE_DIR, *base.split("/"), topic, arc_id, f"{line_id}.mp3")


def check_arc(json_path, speaker_filter="both", verbose=False):
    """Report missing audio for one arc. Returns (total, missing_paths)."""
    with open(json_path, "r", encoding="utf-8") as f:
        arc_data = json.load(f)
    arc_id = arc_data.get("arcId", os.path.splitext(os.path.basename(json_path))[0])
    arc_topic = arc_data.get("topic", "UFOs")

    total = 0
    missing = []
    for speaker, line in iter_flat_lines(arc_data):
        line_id = line.get("id", "")
        if not line_id:
            print(f"WARNING: {arc_id}: line without id: {line}")
            continue
        if speaker not in ("vern", "caller"):
            print(f"WARNING: {arc_id}: line {line_id} unknown speaker '{speaker}'")
            continue
        if speaker_filter != "both" and speaker != speaker_filter:
            continue
        total += 1
        path = audio_path_for(speaker, arc_topic, arc_id, line_id)
        if os.path.exists(path):
            if verbose:
                print(f"OK: {path}")
        else:
            missing.append(path)
            print(f"MISSING: {path}")
    return total, missing


def generate_arc(json_path, force=False, verbose=False, speaker_filter="both", cloner=None):
    """Generate missing (or all with force) audio for one arc."""
    with open(json_path, "r", encoding="utf-8") as f:
        arc_data = json.load(f)
    arc_id = arc_data.get("arcId", os.path.splitext(os.path.basename(json_path))[0])
    arc_topic = arc_data.get("topic", "UFOs")
    caller_gender = arc_data.get("callerGender", "male").lower()
    if caller_gender not in ("male", "female"):
        caller_gender = "male"

    # Deterministic per-arc caller voice (resolved inside ElevenLabsVoiceCloner.generate_audio)
    arc_hash = int(hashlib.md5(arc_id.encode()).hexdigest(), 16) % 1000
    caller_voice = f"caller_pool_{caller_gender}_{arc_hash}"

    generated = 0
    skipped = 0
    errors = 0
    for speaker, line in iter_flat_lines(arc_data):
        line_id = line.get("id", "")
        text = line.get("voiceText") or line.get("text", "")
        mood = line.get("mood", "")
        if not line_id or not text:
            print(f"SKIPPING invalid line in {arc_id}: {line}")
            continue
        if speaker_filter != "both" and speaker != speaker_filter:
            continue

        path = audio_path_for(speaker, arc_topic, arc_id, line_id)
        if os.path.exists(path) and not force:
            if verbose:
                print(f"SKIPPING: {line_id} (already exists)")
            skipped += 1
            continue

        os.makedirs(os.path.dirname(path), exist_ok=True)
        if speaker == "vern":
            settings = MOOD_SETTINGS.get(mood, MOOD_SETTINGS["neutral"])
            voice = cloner.voice_id
        else:
            settings = {"stability": 0.5, "similarity_boost": 0.8, "style": 0.5}
            voice = caller_voice

        try:
            result = cloner.generate_audio(
                text=text,
                output_path=path,
                voice_id=voice,
                stability=settings["stability"],
                similarity_boost=settings["similarity_boost"],
                style=settings["style"],
            )
            if result:
                print(f"GENERATED: {line_id}")
                generated += 1
                time.sleep(2)  # API rate limiting
            else:
                errors += 1
        except Exception as e:
            print(f"ERROR generating {line_id}: {e}")
            errors += 1

    print(f"  {arc_id}: {generated} generated, {skipped} skipped, {errors} errors")
    return generated, skipped, errors


def main():
    parser = argparse.ArgumentParser(description="Generate conversation-arc audio from arc JSON files")
    parser.add_argument("arc", nargs="?", help="arcId (e.g. haunted_motel) or path to arc JSON")
    parser.add_argument("--all", action="store_true", help="process every arc under assets/dialogue/arcs/")
    parser.add_argument("--force", action="store_true", help="regenerate existing files")
    parser.add_argument("--check", action="store_true", help="report missing audio without API calls")
    parser.add_argument("--speaker", choices=["vern", "caller", "both"], default="both")
    parser.add_argument("--verbose", action="store_true")
    args = parser.parse_args()

    if not args.arc and not args.all:
        parser.error("provide an arcId (or JSON path), or use --all")

    jsons = all_arc_jsons() if args.all else [find_arc_json(args.arc)]

    if args.check:
        total = 0
        missing_count = 0
        for jf in jsons:
            _, missing = check_arc(jf, args.speaker, args.verbose)
            total += 1
            missing_count += len(missing)
            name = os.path.splitext(os.path.basename(jf))[0]
            print(f"{name}: {'INCOMPLETE - ' + str(len(missing)) + ' missing' if missing else 'complete'}")
        print(f"\nChecked {total} arc(s), {missing_count} missing audio file(s)")
        sys.exit(1 if missing_count else 0)

    cloner = ElevenLabsVoiceCloner()
    if not cloner.voice_id:
        print("ERROR: No Vern voice ID available. Run elevenlabs_setup.py first.")
        sys.exit(1)

    for jf in jsons:
        generate_arc(jf, force=args.force, verbose=args.verbose, speaker_filter=args.speaker, cloner=cloner)


if __name__ == "__main__":
    main()
