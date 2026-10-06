from __future__ import annotations

import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "audit_framework_purity.py"
SPEC = importlib.util.spec_from_file_location("audit_framework_purity", MODULE_PATH)
audit_module = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
sys.modules[SPEC.name] = audit_module
SPEC.loader.exec_module(audit_module)


class FrameworkPurityAuditTests(unittest.TestCase):
    def setUp(self):
        original_skills = audit_module.ALLOWED_SKILLS
        original_agents = audit_module.ALLOWED_AGENTS
        audit_module.ALLOWED_SKILLS = {"core-skill"}
        audit_module.ALLOWED_AGENTS = set()
        self.addCleanup(setattr, audit_module, "ALLOWED_SKILLS", original_skills)
        self.addCleanup(setattr, audit_module, "ALLOWED_AGENTS", original_agents)

    def make_root(self) -> Path:
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        root = Path(temp.name)
        (root / ".claude/skills/core-skill").mkdir(parents=True)
        (root / ".claude/skills/core-skill/SKILL.md").write_text(
            "---\nname: core-skill\ndescription: Generic workflow.\n---\n\n# Core\n",
            encoding="utf-8",
        )
        (root / ".claude/skills/SKILLS_INDEX.md").write_text(
            "# Index\n\n## 当前 SKILL\n\n`core-skill`\n",
            encoding="utf-8",
        )
        (root / ".claude/agents").mkdir(parents=True)
        (root / ".codex/agents").mkdir(parents=True)
        (root / "ProjectSettings").mkdir(parents=True)
        (root / "ProjectSettings/EditorBuildSettings.asset").write_text(
            "EditorBuildSettings:\n  m_Scenes: []\n",
            encoding="utf-8",
        )
        (root / ".gitignore").write_text(".ai/dispatch/\n", encoding="utf-8")
        collaboration_files = audit_module.COLLABORATION_REQUIRED_FILES
        for relative_name in collaboration_files:
            path = root / relative_name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("# Generic collaboration template\n", encoding="utf-8")
        for relative_name, expected_link in audit_module.COLLABORATION_ENTRY_LINKS.items():
            path = root / relative_name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(expected_link + "\n", encoding="utf-8")
        return root

    def test_clean_minimal_framework_passes(self):
        self.assertEqual([], audit_module.audit(self.make_root()))

    def test_ignored_ai_usage_events_are_allowed(self):
        root = self.make_root()
        events = root / ".ai/usage/events.jsonl"
        events.parent.mkdir(parents=True)
        events.write_text('{"schema_version": 1}\n', encoding="utf-8")

        self.assertEqual([], audit_module.audit(root))

    def test_openspec_archives_are_allowed(self):
        root = self.make_root()
        archive = root / "openspec/changes/archive/2026-08-13-completed-change"
        archive.mkdir(parents=True)
        (archive / "proposal.md").write_text("# Completed change\n", encoding="utf-8")

        self.assertEqual([], audit_module.audit(root))

    def test_unapproved_skill_is_rejected(self):
        root = self.make_root()
        path = root / ".claude/skills/domain-specific-skill"
        path.mkdir()
        (path / "SKILL.md").write_text(
            "---\nname: domain-specific-skill\ndescription: Not approved.\n---\n",
            encoding="utf-8",
        )
        self.assertTrue(any(item.rule == "skill-allowlist" for item in audit_module.audit(root)))

    def test_agent_unknown_skill_is_rejected(self):
        root = self.make_root()
        audit_module.ALLOWED_AGENTS = {"tool"}
        (root / ".claude/agents/tool.md").write_text(
            "---\nname: tool\nskills:\n  - missing-skill\n---\n",
            encoding="utf-8",
        )
        (root / ".codex/agents/tool.toml").write_text('name = "tool"\n', encoding="utf-8")
        self.assertTrue(any(item.rule == "agent-skill" for item in audit_module.audit(root)))

    def test_sample_directory_is_rejected(self):
        root = self.make_root()
        (root / "Assets/Game/Samples").mkdir(parents=True)
        self.assertTrue(any(item.rule == "sample-directory" for item in audit_module.audit(root)))

    def test_legacy_singular_material_directory_is_rejected(self):
        root = self.make_root()
        (root / "Assets/Game/Material").mkdir(parents=True)
        findings = audit_module.audit(root)
        self.assertTrue(
            any(
                item.rule == "forbidden-path" and item.path == "Assets/Game/Material"
                for item in findings
            )
        )

    def test_legacy_singular_material_path_reference_is_rejected(self):
        root = self.make_root()
        path = root / "Assets/Game/Scripts/LegacyPath.cs"
        path.parent.mkdir(parents=True)
        path.write_text(
            'const string Path = "Assets/Game/Material/Test.mat";\n',
            encoding="utf-8",
        )
        self.assertTrue(
            any(
                item.rule == "legacy-content"
                and item.detail == "legacy singular material path"
                for item in audit_module.audit(root)
            )
        )

    def test_business_identifier_is_rejected(self):
        root = self.make_root()
        (root / ".claude/CLAUDE.md").write_text(
            "Business runtime: BusinessEntity\n",
            encoding="utf-8",
        )
        self.assertTrue(any(item.rule == "legacy-content" for item in audit_module.audit(root)))

    def test_missing_collaboration_template_is_rejected(self):
        root = self.make_root()
        (root / audit_module.COLLABORATION_REQUIRED_FILES[0]).unlink()

        self.assertTrue(any(item.rule == "collaboration-entry" for item in audit_module.audit(root)))

    def test_missing_local_registry_ignore_rule_is_rejected(self):
        root = self.make_root()
        (root / ".gitignore").write_text("", encoding="utf-8")

        self.assertTrue(any(item.rule == "collaboration-registry" for item in audit_module.audit(root)))

    def test_product_content_in_collaboration_template_is_rejected(self):
        root = self.make_root()
        path = root / audit_module.COLLABORATION_REQUIRED_FILES[0]
        path.write_text("AutoEra\n", encoding="utf-8")

        self.assertTrue(
            any(item.rule == "collaboration-product-content" for item in audit_module.audit(root))
        )

    def test_utf8_bom_skill_frontmatter_is_accepted(self):
        root = self.make_root()
        path = root / ".claude/skills/core-skill/SKILL.md"
        path.write_text(
            "\ufeff---\nname: core-skill\ndescription: Generic workflow.\n---\n\n# Core\n",
            encoding="utf-8",
        )

        self.assertEqual([], audit_module.audit(root))


if __name__ == "__main__":
    unittest.main()
