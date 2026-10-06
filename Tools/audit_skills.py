#!/usr/bin/env python3
"""Validate project SKILL entry points without creating persistent audit artifacts."""

from __future__ import annotations

import argparse
import json
import statistics
import sys
from pathlib import Path

try:
    import yaml
except ImportError as exc:
    raise SystemExit("PyYAML is required: python -m pip install pyyaml") from exc


ROOT = Path(__file__).resolve().parent.parent
SKILLS_DIR = ROOT / ".claude" / "skills"
TRIGGER_MARKERS = ("触发", "适用", "use when", "when to use", "trigger")


def scan(skills_dir: Path) -> list[dict]:
    results: list[dict] = []
    for skill_dir in sorted(path for path in skills_dir.iterdir() if path.is_dir()):
        skill_file = skill_dir / "SKILL.md"
        item = {"name": skill_dir.name, "path": skill_file.as_posix()}
        if not skill_file.is_file():
            item["error"] = "missing SKILL.md"
            results.append(item)
            continue
        try:
            text = skill_file.read_text(encoding="utf-8")
            if not text.startswith("---"):
                raise ValueError("missing YAML frontmatter")
            parts = text.split("---", 2)
            if len(parts) < 3:
                raise ValueError("unterminated YAML frontmatter")
            metadata = yaml.safe_load(parts[1]) or {}
            if not isinstance(metadata, dict):
                raise ValueError("frontmatter must be a mapping")
        except (OSError, UnicodeError, ValueError, yaml.YAMLError) as exc:
            item["error"] = str(exc)
            results.append(item)
            continue

        declared_name = metadata.get("name")
        description = str(metadata.get("description") or "").strip()
        item.update(
            {
                "declared_name": declared_name,
                "description_length": len(description),
                "has_trigger": any(
                    marker in description.lower() for marker in TRIGGER_MARKERS
                ),
            }
        )
        if declared_name != skill_dir.name:
            item["error"] = f"name mismatch: {declared_name!r}"
        elif not description:
            item["error"] = "missing description"
        results.append(item)
    return results


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--json", action="store_true", help="write the report to stdout as JSON")
    args = parser.parse_args(argv)
    skills_dir = args.root.resolve() / ".claude" / "skills"
    if not skills_dir.is_dir():
        print(f"[FAIL] missing skills directory: {skills_dir}")
        return 1

    results = scan(skills_dir)
    errors = [item for item in results if "error" in item]
    lengths = [
        item["description_length"]
        for item in results
        if "description_length" in item
    ]
    missing_triggers = [
        item["name"] for item in results if item.get("has_trigger") is False
    ]

    if args.json:
        print(
            json.dumps(
                {
                    "skills": results,
                    "errors": len(errors),
                    "missing_trigger_markers": missing_triggers,
                },
                ensure_ascii=False,
                indent=2,
            )
        )
    else:
        average = statistics.mean(lengths) if lengths else 0
        print(
            f"SKILL count={len(results)} "
            f"description min={min(lengths, default=0)} "
            f"max={max(lengths, default=0)} avg={average:.1f}"
        )
        for item in errors:
            print(f"[FAIL] {item['name']}: {item['error']}")
        if missing_triggers:
            print(
                "[WARN] description lacks an explicit trigger marker: "
                + ", ".join(missing_triggers)
            )

    if errors:
        return 1
    print("[OK] SKILL entry audit passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
