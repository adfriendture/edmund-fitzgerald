#!/usr/bin/env python3
"""Writes VOICE_SCRIPT.md from Assets/StreamingAssets/Dialogue/lines.tsv.

The TSV is the single source of truth: edit lines there, then run
    python3 tools/export_script.py
"""
import os
from collections import OrderedDict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TSV = os.path.join(ROOT, "Assets", "StreamingAssets", "Dialogue", "lines.tsv")
OUT = os.path.join(ROOT, "VOICE_SCRIPT.md")

ROLES = OrderedDict([
    ("CAPTAIN", ("You, the captain", "Fifties, forty years on the Lakes. Calm, dry, never raises his voice. The player hears these as their own lines.")),
    ("MATE", ("First Mate", "In the pilothouse or the chart room just behind you. Steady, a bit younger than the captain.")),
    ("WHEELSMAN", ("Wheelsman", "Standing beside you. Few words.")),
    ("CHIEF", ("Chief Engineer", "On the sound-powered phone from the engine room. Gruff, practical, engine noise behind him.")),
    ("WATCHMAN", ("Deck Watch", "On the phone from aft, shouting over the wind. Youngest voice aboard.")),
    ("ANDERSON", ("Captain of the Arthur M. Anderson", "On VHF from ten miles astern. An old friend; easy radio manner.")),
    ("COASTGUARD", ("Coast Guard Group Sault Ste. Marie", "Young radioman, by the book.")),
    ("AVAFORS", ("Pilot aboard the Avafors", "A Great Lakes pilot on a foreign ship near Whitefish. Plain-spoken Midwesterner.")),
    ("NEWS", ("WGLR 1230 newsreader", "1970s small-market AM news voice.")),
    ("DJ", ("CKSM 850 disc jockey", "Cheerful Canadian AM DJ.")),
    ("NIGHTDJ", ("1360 late-show host", "Soft, slow, a little uncanny. The easter-egg station.")),
])

CHANNEL = {"Room": "in the room", "Phone": "phone", "Radio": "VHF radio", "Broadcast": "AM broadcast"}


def main():
    lines = []
    with open(TSV, encoding="utf-8") as f:
        for raw in f:
            raw = raw.rstrip("\n")
            if not raw or raw.startswith("#") or raw.startswith("id\t"):
                continue
            c = raw.split("\t")
            if len(c) < 4:
                continue
            lines.append({"id": c[0], "speaker": c[1], "channel": c[2], "text": c[3], "dir": c[4] if len(c) > 4 else ""})

    out = []
    out.append("# Edmund Fitzgerald: voice script\n")
    out.append(f"{len(lines)} lines, grouped by character in story order, so each voice can be recorded in one sitting.\n")
    out.append("## How to record\n")
    out.append("- **Format:** WAV, mono, 44.1 or 48 kHz, 16 or 24 bit. OGG and MP3 also work.")
    out.append("- **File name:** exactly the line id, e.g. `and_final_01.wav`. Put the files in `Assets/StreamingAssets/Voice/`.")
    out.append("- **Keep it dry.** Close mic, no reverb, no EQ. The game adds the VHF crackle, the phone band-pass and the AM radio colour itself.")
    out.append("- Leave about a quarter second of silence before and after each take. Trim longer gaps; the game waits for the clip to finish.")
    out.append("- Any line without a recording still shows as a subtitle, so you can record in any order and test as you go.")
    out.append("- After adding files, rebuild (`tools/build_mac.sh`) or copy them into the built app's `StreamingAssets/Voice` folder.\n")
    out.append("A note on tone: these were real men. Play them as professionals doing their jobs on a bad afternoon, not as doomed characters. "
               "Nobody aboard knew how it would end, and that is what makes the last exchange land.\n")

    for key, (name, desc) in ROLES.items():
        mine = [l for l in lines if l["speaker"] == key]
        if not mine:
            continue
        out.append(f"## {name}  ({len(mine)} lines)\n")
        out.append(f"*{desc}*\n")
        out.append("| File | Line | Direction |")
        out.append("|---|---|---|")
        for l in mine:
            text = l["text"].replace("|", "\\|")
            d = l["dir"].replace("|", "\\|")
            ch = CHANNEL.get(l["channel"], l["channel"])
            out.append(f"| `{l['id']}` | {text} | {d} *({ch})* |")
        out.append("")

    unknown = sorted({l["speaker"] for l in lines} - set(ROLES))
    if unknown:
        out.append("## Other\n")
        for l in lines:
            if l["speaker"] in unknown:
                out.append(f"- `{l['id']}` ({l['speaker']}): {l['text']}")

    with open(OUT, "w", encoding="utf-8") as f:
        f.write("\n".join(out) + "\n")
    print(f"wrote {OUT} ({len(lines)} lines)")


if __name__ == "__main__":
    main()
