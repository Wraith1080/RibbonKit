# Agent workflow audit — 2026-09-05

Historical audit of repository instructions against official GPT-6 Astra guidance.
The initial checkout was clean. It contained one AGENTS.md, no repository skill and
no repository Codex model configuration. Later documentation maintenance is recorded
in [DOCUMENTATION-AUDIT.md](DOCUMENTATION-AUDIT.md).

## Applied changes at that checkpoint

Removed stale status/wish lists from root instructions; centralized validation in
CONTRIBUTING; replaced design-notes §4 duplication with links; created the discoverable
WPF workflow skill. Clarified local-task authorization, Writer runtime boundaries,
single-agent default, evidence reporting and focused versus release checks. CI's
full Windows build/test/package gate was retained.

At that point AGENTS.md fell from 7,313 to 4,411 characters (39.7%, normalized LF).
These are historical file sizes, not current token counts or a measured speedup.
The skill validator, paths/links and whitespace checks passed. WPF builds/tests were
not run for instruction-only changes. No model, permission, global skill/plugin or
memory settings changed.

## Sources and evaluation boundary

Official pages consulted on 2026-09-05:

- [GPT-6 Astra guidance](https://developers.openai.com/api/docs/guides/latest-model)
- [Codex best practices](https://learn.chatgpt.com/guides/best-practices)
- [AGENTS.md discovery](https://learn.chatgpt.com/docs/agent-configuration/agents-md)
- [Skill authoring/discovery](https://learn.chatgpt.com/docs/build-skills)

The changes address contradictory instructions, avoidable pauses and excessive
verification. Markdown cannot select the host model or enable API capabilities.
No comparative Astra runs were performed. To measure performance, hold model/effort,
starting code, task, tools, permissions and cache conditions constant across isolated
runs. Compare correctness, boundary violations, necessary verification, questions,
wall time and available token usage over repeated representative tasks. A smaller
instruction file alone does not establish a faster or better result.
