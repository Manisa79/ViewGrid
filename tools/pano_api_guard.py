#!/usr/bin/env python3
"""Lightweight static guard for Taylan.Pano partial-class growth.

It is not a compiler. It catches the most common regression seen during rapid
phase additions: duplicate PanoControl properties/events and sample code that
references missing core symbols by typo.
"""
from __future__ import annotations
import re
from pathlib import Path
from collections import defaultdict

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src" / "Taylan.Pano" / "Core"

PROP_RE = re.compile(r"^\s*public\s+(?!class|enum|interface|delegate|event)(?:[\w<>,?\.\[\]]+\s+)+(?P<name>\w+)\s*\{", re.M)
EVENT_RE = re.compile(r"^\s*public\s+event\s+[^;]+\s+(?P<name>\w+)\s*;", re.M)

# Known nested classes in PanoControl*.cs may have properties named RowObject, Column, etc.
# These are intentionally ignored because they are not PanoControl members.
IGNORE_NAMES = {"RowObject", "RowIndex", "Column", "Model", "Name", "Position", "Rows", "TargetViewIndex", "PanoControl"}


def scan() -> int:
    props: dict[str, list[str]] = defaultdict(list)
    events: dict[str, list[str]] = defaultdict(list)
    for file in CORE.glob("PanoControl*.cs"):
        text = file.read_text(encoding="utf-8-sig", errors="ignore")
        marker = "partial class PanoControl"
        if marker not in text:
            continue
        text = text[text.find(marker):]
        for match in PROP_RE.finditer(text):
            name = match.group("name")
            if name not in IGNORE_NAMES:
                props[name].append(file.name)
        for match in EVENT_RE.finditer(text):
            name = match.group("name")
            if name not in IGNORE_NAMES:
                events[name].append(file.name)

    failed = False
    for title, data in (("property", props), ("event", events)):
        for name, files in sorted(data.items()):
            # Taylan.PanoPlatformFeatureOptions is a separate options class that may share names.
            unique = sorted(set(files))
            if len(unique) > 1 and name in {"EnableCommandPalette", "EnableFilterPresets", "EnableRowDetails", "EnableCopyPro"}:
                print(f"WARNING duplicate-like {title}: {name} -> {', '.join(unique)}")
            elif len(unique) > 1:
                print(f"CHECK duplicate-like {title}: {name} -> {', '.join(unique)}")
    print("Taylan.Pano API guard completed. Review CHECK/WARNING lines before release.")
    return 1 if failed else 0

if __name__ == "__main__":
    raise SystemExit(scan())
