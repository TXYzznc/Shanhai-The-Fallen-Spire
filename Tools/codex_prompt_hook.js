#!/usr/bin/env node

const fs = require("fs");

function readStdin() {
  return new Promise((resolve) => {
    let data = "";
    process.stdin.setEncoding("utf8");
    process.stdin.on("data", (chunk) => {
      data += chunk;
    });
    process.stdin.on("end", () => resolve(data));
    process.stdin.on("error", () => resolve(""));
  });
}

function emit(eventName, additionalContext) {
  if (!additionalContext) {
    return;
  }

  console.log(JSON.stringify({
    hookSpecificOutput: {
      hookEventName: eventName,
      additionalContext,
    },
  }));
}

function sessionContext() {
  try {
    const agents = fs.readFileSync("AGENTS.md", "utf8");
    return `【自动注入 AGENTS.md】\n\n${agents}`;
  } catch (_error) {
    return "";
  }
}

function parsePrompt(raw) {
  try {
    return JSON.parse(raw || "{}").prompt || "";
  } catch (_error) {
    return "";
  }
}

function decisionGateContext(prompt) {
  const gate = /设计|架构|重构|大改|重写|PRD|系统|范式|方案|思路/;
  if (!gate.test(prompt)) {
    return "";
  }

  return [
    "检测到大型决策关键词。",
    "阶段 A：先用 grill-me 或 grill-with-docs 至少三轮澄清目标、关键决策、边界、验收和约束。",
    "阶段 B：评估规模；中大型变更建立 OpenSpec change，轻量任务直接执行。",
    "出现共识冲突、不可逆变更或触及 Assets/Game/ScriptsBuiltin 框架核心时暂停并交回主对话。",
  ].join("\n");
}

function graphifyContext(prompt) {
  if (!prompt.trim().startsWith("/graphify")) {
    return "";
  }

  return "检测到 /graphify：先读取并执行 graphify-windows SKILL。";
}

async function main() {
  const mode = process.argv[2] || "prompt";
  if (mode === "session") {
    emit("SessionStart", sessionContext());
    return;
  }

  const prompt = parsePrompt(await readStdin());
  const context = [
    graphifyContext(prompt),
    decisionGateContext(prompt),
  ].filter(Boolean).join("\n\n");

  emit("UserPromptSubmit", context);
}

main().catch(() => {
  // Hook failures must not block the task.
});

