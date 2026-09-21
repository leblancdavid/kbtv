#!/usr/bin/env python3
"""
KBTV Arc Factory - builds conversation-arc JSON files from compact content data.

Content modules live in arcs_data/*.py, each defining ARCS: a list of arc dicts:
  {
    "arcId": str, "legitimacy": "Fake|Questionable|Credible|Compelling",
    "gender": "male|female", "personality": str,
    "summary": str, "notes": str,
    "turns": [
      ("v", {"neutral": str, "tired": str, "irritated": str,
             "gruff": str, "amused": str, "focused": str}),
      ("c", text, voiceText),
      ...  # strictly alternating, starts+ends with ("v", ...)
    ],
  }

Hand-authored moods: neutral, tired, irritated, gruff, amused, focused.
Derived mechanically (mirroring the pilot-arc conventions):
  energized  = CAPS
  angry      = CAPS, . -> !
  manic      = CAPS + !!!
  exhausted  = "... " + lowercase
  depressed  = trailing punctuation -> "..."
  frustrated = "Ugh - " + sentence
  obsessive  = neutral + rotating detail-demand suffix
Output: assets/dialogue/arcs/UFOs/{arcId}.json (compact one-line-per-line format).
"""
import json
import os
import sys
import glob
import hashlib
import importlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
ARCS_DIR = os.path.join(ROOT, "assets", "dialogue", "arcs")
OUT_DIR = os.path.join(ARCS_DIR, "UFOs")

MOOD_ORDER = ["neutral", "tired", "energized", "irritated", "gruff", "amused",
              "focused", "exhausted", "depressed", "angry", "frustrated",
              "obsessive", "manic"]

AUTHORED = {"neutral", "tired", "irritated", "gruff", "amused", "focused"}

OBS_SUFFIXES = ["Tell me exactly.", "Leave nothing out.", "Don't skip a single detail.",
                "Every detail. Every one.", "I want the exact words."]


def caps(text):
    return text.upper()


def derive(mood, neutral, seed):
    t = neutral.rstrip()
    if mood == "energized":
        return caps(t).rstrip(".!") + "!"
    if mood == "angry":
        return caps(t).replace(".", "!")
    if mood == "manic":
        return caps(t).rstrip(".!") + "!!!"
    if mood == "exhausted":
        low = t[0].lower() + t[1:] if t else t
        return "... " + low
    if mood == "depressed":
        out = t.rstrip(".!?")
        return out + "..."
    if mood == "frustrated":
        low = t[0].lower() + t[1:] if t else t
        return "Ugh - " + low
    if mood == "obsessive":
        return t.rstrip(".!?") + ". " + OBS_SUFFIXES[seed % len(OBS_SUFFIXES)]
    raise ValueError(f"cannot derive mood {mood}")


def build_arc(spec, existing_ids):
    arc_id = spec["arcId"]
    legit = spec["legitimacy"].lower()
    turns = spec["turns"]
    assert turns and turns[0][0] == "v" and turns[-1][0] == "v", f"{arc_id}: must start+end with vern"
    for i, t in enumerate(turns):
        expect = "v" if i % 2 == 0 else "c"
        assert t[0] == expect, f"{arc_id}: turn {i} must be {expect}"

    groups = []
    vn = cn = 0
    for kind, *payload in turns:
        if kind == "v":
            vn += 1
            authored = payload[0]
            missing = AUTHORED - set(authored)
            assert not missing, f"{arc_id}: vern turn {vn} missing authored moods {missing}"
            lines = []
            arc_seed = int(hashlib.md5(arc_id.encode()).hexdigest(), 16)
            for j, mood in enumerate(MOOD_ORDER):
                text = authored[mood] if mood in authored else derive(mood, authored["neutral"], vn * 13 + j + arc_seed)
                lid = f"ufos_{legit}_{arc_id}_vern_{mood}_{vn}"
                assert lid not in existing_ids, f"duplicate id {lid}"
                existing_ids.add(lid)
                lines.append({"id": lid, "mood": mood, "text": text, "voiceText": text})
            groups.append(("vern", lines))
        else:
            cn += 1
            text, voice = payload[0], payload[1]
            lid = f"ufos_{legit}_{arc_id}_caller_{cn}"
            assert lid not in existing_ids, f"duplicate id {lid}"
            existing_ids.add(lid)
            groups.append(("caller", [{"id": lid, "mood": "neutral", "text": text, "voiceText": voice}]))

    arc = {
        "arcId": arc_id,
        "topic": "UFOs",
        "legitimacy": spec["legitimacy"],
        "callerGender": spec["gender"],
        "callerPersonality": spec["personality"],
        "screeningSummary": spec["summary"],
        "arcNotes": spec["notes"],
        "arcLines": [{"speaker": s, "lines": ls} for s, ls in groups],
    }
    return arc


def dump_compact(arc):
    """JSON with each line object on a single line (pilot format)."""
    out = ["{"]
    for key in ("arcId", "topic", "legitimacy", "callerGender", "callerPersonality",
                "screeningSummary", "arcNotes"):
        out.append(f"  {json.dumps(key)}: {json.dumps(arc[key], ensure_ascii=False)},")
    out.append('  "arcLines": [')
    groups = arc["arcLines"]
    for gi, g in enumerate(groups):
        out.append(f'    {{ "speaker": {json.dumps(g["speaker"])}, "lines": [')
        for li, ln in enumerate(g["lines"]):
            comma = "," if li < len(g["lines"]) - 1 else ""
            out.append("      " + json.dumps(ln, ensure_ascii=False) + comma)
        tail = "]}," if gi < len(groups) - 1 else "]}"
        out.append("    " + tail)
    out.append("  ]")
    out.append("}")
    return "\n".join(out) + "\n"


def load_existing_ids():
    ids = set()
    for path in glob.glob(os.path.join(ARCS_DIR, "**", "*.json"), recursive=True):
        with open(path, "r", encoding="utf-8") as f:
            data = json.load(f)
        for g in data.get("arcLines", []):
            for ln in g.get("lines", []):
                ids.add(ln.get("id", ""))
    return ids


def main():
    sys.path.insert(0, HERE)
    modules = sys.argv[1:] or sorted(
        os.path.splitext(os.path.basename(p))[0]
        for p in glob.glob(os.path.join(HERE, "arcs_data", "*.py"))
        if not os.path.basename(p).startswith("_"))
    existing = load_existing_ids()
    written = 0
    for mod_name in modules:
        mod = importlib.import_module(f"arcs_data.{mod_name}")
        for spec in mod.ARCS:
            arc = build_arc(spec, existing)
            path = os.path.join(OUT_DIR, arc["arcId"] + ".json")
            with open(path, "w", encoding="utf-8", newline="\n") as f:
                f.write(dump_compact(arc))
            print(f"WROTE {path}")
            written += 1
    print(f"{written} arc(s) written")


if __name__ == "__main__":
    main()
