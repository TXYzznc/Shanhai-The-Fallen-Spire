#!/usr/bin/env python3
"""Audit that the repository is a domain-neutral, sample-free framework."""

from __future__ import annotations

import argparse
import re
import sys
from dataclasses import dataclass
from pathlib import Path

try:
    import yaml
except ImportError as exc:  # pragma: no cover - project tooling dependency
    raise SystemExit("PyYAML is required: python -m pip install pyyaml") from exc


ALLOWED_SKILLS = {
    "3d-modeling",
    "ab-testing",
    "addressables-hotfix",
    "agency-technical-artist",
    "agency-unity-shader-graph-artist",
    "agent-browser",
    "ai-art",
    "animation-systems",
    "arch-api",
    "art-direction",
    "asc-submission-health",
    "backend-testing",
    "blender-mcp",
    "cdn-setup",
    "codex-image-gen",
    "competitive-analysis",
    "crash-analytics",
    "database-schema-design",
    "deep-research",
    "deploy-checklist",
    "dev-tools",
    "devops-deployment",
    "document-tools",
    "feature-flags",
    "find-skills",
    "font-pairing-suggester",
    "font-selection-cjk",
    "font-subsetting",
    "game-art",
    "game-networking",
    "game-ui-design",
    "github-actions-docs",
    "gpt-image-2-style-library",
    "grill-me",
    "grill-with-docs",
    "image-compression",
    "jwt-auth",
    "k6",
    "kafka-development",
    "localization-i18n",
    "milestone-tracker",
    "moai-docs-generation",
    "mobile-cicd",
    "mobile-device-testing",
    "oauth-implementation",
    "openspec",
    "opentelemetry",
    "physics-collision",
    "pixel-font-rendering",
    "project-management",
    "prometheus",
    "redis-best-practices",
    "redis-specialist",
    "rigging",
    "risk-assessment",
    "save-serialization",
    "secrets-management",
    "semver",
    "setup-fastlane",
    "shader-effects",
    "skill-creator",
    "sprint-retrospective",
    "state-machine",
    "steam-deploy",
    "task-estimation",
    "testing-strategies",
    "texture-art",
    "typeset",
    "ui-asset-splitting",
    "uloop-execute-dynamic-code",
    "uloop-run-tests",
    "unity-animation",
    "unity-architecture-di",
    "unity-async-patterns",
    "unity-build-pipeline",
    "unity-dev",
    "unity-ecs-patterns",
    "unity-editor-scripting",
    "unity-foundations",
    "unity-input-correctness",
    "unity-lighting-vfx",
    "unity-networking",
    "unity-rect-transform",
    "unity-shaders-rendering",
    "unity-skills",
    "unity-ui",
    "vfx-realtime",
    "xlsx",
}

ALLOWED_AGENTS = {
    "art-2d",
    "art-3d",
    "art-anim",
    "art-director",
    "art-font",
    "art-ui",
    "art-vfx",
    "client-lead",
    "client-ta",
    "client-unity",
    "devops-engineer",
    "net-backend",
    "net-db",
    "net-lead",
    "producer",
    "qa-engineer",
    "tools-engineer",
}

FORBIDDEN_PATHS = {
    ".agents",
    "Assets/Game/HotfixDlls",
    "Assets/Game/Material",
    "Assets/HybridCLRData",
    "CompressImageTool",
    "OutPackages",
    "artifacts",
    "tools/ai_index",
    "tools/playtest",
}

SAMPLE_DIRECTORY_NAMES = {"demo", "demos", "example", "examples", "sample", "samples"}

PROHIBITED_PATTERNS = {
    "legacy singular material path": re.compile(
        r"Assets[/\\]Game[/\\]Material(?:[/\\]|$)",
        re.IGNORECASE,
    ),
    "business data path": re.compile(r"(GameplayCatalogs|DataTables[/\\]Business)", re.IGNORECASE),
    "business runtime type": re.compile(
        r"\b(BusinessEntity|ProductHud|ProductManager)\b",
        re.IGNORECASE,
    ),
    "sample launch identifier": re.compile(r"(MainMenu|SampleScene)", re.IGNORECASE),
    "removed convenience API": re.compile(
        r"(ShowRewardEffect|ShowPopText|ShowPopEmoji)", re.IGNORECASE
    ),
}

COLLABORATION_REQUIRED_FILES = (
    "Documentation/Development/AI-Team-Collaboration-Initialization.md",
    "Documentation/Development/Dispatch/README.md",
    "Documentation/Development/Dispatch/RoleRouting.md",
    "Documentation/Development/Dispatch/DispatchTemplate.md",
    "Documentation/Development/Dispatch/ActiveAssignments.template.md",
    "Documentation/Development/Dispatch/PauseAndRecovery.md",
    "Documentation/Development/Dispatch/WindowAutomation.md",
    "Documentation/Development/Dispatch/GitIntegration.md",
    "Documentation/Development/Dispatch/WindowRegistry.example.json",
    "Documentation/Development/Dispatch/DecisionArchiveTemplate.md",
    "Documentation/Development/Dispatch/OpenSpecBatching.md",
    "Documentation/Development/Dispatch/Active/README.md",
)

COLLABORATION_ENTRY_LINKS = {
    "AGENTS.md": "Documentation/Development/Dispatch/README.md",
    ".claude/AGENTS.md": "Documentation/Development/Dispatch/README.md",
    ".claude/CLAUDE.md": "Documentation/Development/Dispatch/README.md",
}

COLLABORATION_PROHIBITED_PATTERNS = {
    "historical product name": re.compile(r"AutoEra|stellar-frontier-auto-era", re.IGNORECASE),
    "historical task identifier": re.compile(r"\b(?:P0|ART)-\d+\b", re.IGNORECASE),
    "historical tool instance": re.compile(r"\b809[01]\b"),
    "historical art project": re.compile(r"\bArtResource\b", re.IGNORECASE),
    "fixed Windows product path": re.compile(r"[A-Za-z]:\\\\"),
}

TEXT_SUFFIXES = {
    ".asmdef",
    ".asset",
    ".cs",
    ".json",
    ".md",
    ".ps1",
    ".py",
    ".toml",
    ".txt",
    ".xml",
    ".yaml",
    ".yml",
}

CONTENT_TARGETS = (
    "AGENTS.md",
    ".claude/AGENTS.md",
    ".claude/CLAUDE.md",
    ".claude/SKILL_MATRIX.md",
    ".claude/agents",
    ".claude/skills",
    ".codex/agents",
    "Assets/Game/Scripts",
    "Assets/Game/ScriptsBuiltin",
    "openspec/specs",
    "Documentation/Development/Dispatch",
    "Documentation/Development/AI-Team-Collaboration-Initialization.md",
    "ProjectSettings/EditorBuildSettings.asset",
)


@dataclass(frozen=True)
class Finding:
    rule: str
    path: str
    detail: str


def relative(path: Path, root: Path) -> str:
    return path.relative_to(root).as_posix()


def frontmatter(path: Path) -> dict:
    text = path.read_text(encoding="utf-8-sig")
    match = re.match(r"^---\s*\n(.*?)\n---", text, re.DOTALL)
    if not match:
        raise ValueError("missing YAML frontmatter")
    data = yaml.safe_load(match.group(1)) or {}
    if not isinstance(data, dict):
        raise ValueError("frontmatter must be a mapping")
    return data


def iter_text_files(root: Path):
    for target_name in CONTENT_TARGETS:
        target = root / target_name
        if target.is_file():
            yield target
            continue
        if not target.is_dir():
            continue
        for path in target.rglob("*"):
            if path.is_file() and path.suffix.lower() in TEXT_SUFFIXES:
                yield path


def audit_collaboration(root: Path) -> list[Finding]:
    findings: list[Finding] = []
    for relative_name in COLLABORATION_REQUIRED_FILES:
        if not (root / relative_name).is_file():
            findings.append(Finding("collaboration-entry", relative_name, "missing"))

    gitignore = root / ".gitignore"
    if not gitignore.is_file() or ".ai/dispatch/" not in gitignore.read_text(
        encoding="utf-8-sig", errors="replace"
    ):
        findings.append(
            Finding("collaboration-registry", ".gitignore", "missing .ai/dispatch/ ignore rule")
        )

    for relative_name, expected_link in COLLABORATION_ENTRY_LINKS.items():
        path = root / relative_name
        if not path.is_file():
            continue
        text = path.read_text(encoding="utf-8-sig", errors="replace")
        if expected_link not in text:
            findings.append(
                Finding("collaboration-entry", relative_name, f"missing link: {expected_link}")
            )

    for relative_name in COLLABORATION_REQUIRED_FILES:
        path = root / relative_name
        if not path.is_file():
            continue
        text = path.read_text(encoding="utf-8", errors="replace")
        for label, pattern in COLLABORATION_PROHIBITED_PATTERNS.items():
            if pattern.search(text):
                findings.append(Finding("collaboration-product-content", relative_name, label))
    return findings


def audit(root: Path) -> list[Finding]:
    findings: list[Finding] = []
    skills_root = root / ".claude" / "skills"
    agents_root = root / ".claude" / "agents"
    codex_agents_root = root / ".codex" / "agents"

    findings.extend(audit_collaboration(root))

    for forbidden in sorted(FORBIDDEN_PATHS):
        path = root / forbidden
        if path.exists():
            findings.append(Finding("forbidden-path", forbidden, "path must not exist"))

    for base_name in (".claude/skills", "Assets/Game", "openspec/specs"):
        base = root / base_name
        if not base.exists():
            continue
        for path in base.rglob("*"):
            if path.is_dir() and path.name.lower() in SAMPLE_DIRECTORY_NAMES:
                findings.append(
                    Finding("sample-directory", relative(path, root), "sample artifacts are forbidden")
                )

    actual_skills: set[str] = set()
    if skills_root.is_dir():
        for path in sorted(skills_root.iterdir()):
            if not path.is_dir():
                continue
            skill_file = path / "SKILL.md"
            if not skill_file.is_file():
                findings.append(
                    Finding("skill-entry", relative(path, root), "missing SKILL.md")
                )
                continue
            actual_skills.add(path.name)
            try:
                metadata = frontmatter(skill_file)
            except (OSError, UnicodeError, ValueError, yaml.YAMLError) as exc:
                findings.append(
                    Finding("skill-frontmatter", relative(skill_file, root), str(exc))
                )
                continue
            if metadata.get("name") != path.name:
                findings.append(
                    Finding(
                        "skill-name",
                        relative(skill_file, root),
                        f"frontmatter name is {metadata.get('name')!r}",
                    )
                )

    unexpected_skills = sorted(actual_skills - ALLOWED_SKILLS)
    missing_allowed_skills = sorted(ALLOWED_SKILLS - actual_skills)
    if unexpected_skills:
        findings.append(
            Finding(
                "skill-allowlist",
                ".claude/skills",
                f"unexpected: {', '.join(unexpected_skills)}",
            )
        )
    if missing_allowed_skills:
        findings.append(
            Finding(
                "skill-allowlist",
                ".claude/skills",
                f"missing: {', '.join(missing_allowed_skills)}",
            )
        )

    index_path = skills_root / "SKILLS_INDEX.md"
    if index_path.is_file():
        index_text = index_path.read_text(encoding="utf-8-sig")
        current_section = index_text.split("## 当前 SKILL", 1)
        indexed = set(re.findall(r"`([a-z0-9][a-z0-9-]*)`", current_section[-1]))
        missing = sorted(actual_skills - indexed)
        extra = sorted(indexed - actual_skills)
        if missing:
            findings.append(
                Finding("skill-index", relative(index_path, root), f"missing: {', '.join(missing)}")
            )
        if extra:
            findings.append(
                Finding("skill-index", relative(index_path, root), f"unknown: {', '.join(extra)}")
            )
    else:
        findings.append(Finding("skill-index", ".claude/skills/SKILLS_INDEX.md", "missing"))

    source_agents: set[str] = set()
    if agents_root.is_dir():
        for path in sorted(agents_root.glob("*.md")):
            source_agents.add(path.stem)
            try:
                metadata = frontmatter(path)
            except (OSError, UnicodeError, ValueError, yaml.YAMLError) as exc:
                findings.append(Finding("agent-frontmatter", relative(path, root), str(exc)))
                continue
            for skill_name in metadata.get("skills", []) or []:
                if skill_name not in actual_skills:
                    findings.append(
                        Finding(
                            "agent-skill",
                            relative(path, root),
                            f"unknown skill: {skill_name}",
                        )
                    )

    codex_agents = (
        {path.stem for path in codex_agents_root.glob("*.toml")}
        if codex_agents_root.is_dir()
        else set()
    )
    if source_agents != codex_agents:
        findings.append(
            Finding(
                "agent-mirror",
                ".codex/agents",
                "run python tools/sync-agents.py",
            )
        )
    if source_agents != ALLOWED_AGENTS:
        findings.append(
            Finding(
                "agent-allowlist",
                ".claude/agents",
                "agent set differs from the framework allowlist",
            )
        )

    for path in iter_text_files(root):
        try:
            text = path.read_text(encoding="utf-8-sig")
        except (OSError, UnicodeError):
            continue
        for label, pattern in PROHIBITED_PATTERNS.items():
            match = pattern.search(text)
            if match:
                findings.append(Finding("legacy-content", relative(path, root), label))

    build_settings = root / "ProjectSettings" / "EditorBuildSettings.asset"
    if build_settings.is_file():
        text = build_settings.read_text(encoding="utf-8-sig", errors="replace")
        enabled_scenes = re.findall(
            r"enabled:\s*1\s*\n\s*path:\s*([^\r\n]+)", text
        )
        unsupported_scenes = [
            path.strip()
            for path in enabled_scenes
            if path.strip() != "Assets/Game/Scene/Launch.unity"
        ]
        if unsupported_scenes:
            findings.append(
                Finding(
                    "build-settings",
                    relative(build_settings, root),
                    "only the framework Launch scene may be enabled: "
                    + ", ".join(unsupported_scenes),
                )
            )

    return sorted(findings, key=lambda item: (item.rule, item.path, item.detail))


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parent.parent)
    args = parser.parse_args(argv)
    root = args.root.resolve()

    findings = audit(root)
    if findings:
        print(f"[FAIL] framework purity audit found {len(findings)} issue(s)")
        for item in findings:
            print(f"- {item.rule}: {item.path}: {item.detail}")
        return 1

    print("[OK] framework purity audit passed")
    return 0


if __name__ == "__main__":
    sys.exit(main())
