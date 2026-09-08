---
name: ribbonkit-wpf-workflow
description: Implement, debug, and verify RibbonKit WPF controls, Showcase, Writer, or designer behavior in this repository. Use for code changes and WPF regression investigations, not documentation-only edits or general model advice.
---

# RibbonKit WPF workflow

Resolve paths below from the repository root. Follow `AGENTS.md` for scope and
architecture and `CONTRIBUTING.md` for validation commands; do not reload either if
already available in the current context.

## Establish the smallest useful reproduction

Identify the affected control or consumer, observable failure, expected behavior,
and relevant §3 history/§5 status in `04-DESIGN-NOTES.md`. Inspect its implementation
and nearby tests before selecting a fix. Check project files for actual frameworks.
Use a short plan for changes spanning several components; implement a small fix directly.

For Writer, reproduce in the consumer or its tests within the runtime boundary in
`AGENTS.md`. A friction-log entry alone does not authorize a library edit.

## Implement and verify the affected behavior

Choose the narrowest shared location within the authorized scope. Update a test,
Showcase scenario, and public documentation when they provide relevant coverage.
Select the validation tier in `CONTRIBUTING.md` before running checks.

WPF evidence needs the right surface:

- Use the existing STA/Dispatcher test patterns for controls, popups, and deferred templates. Inspect realized template parts, resource lookup, and native scrolling when those cause the failure.
- Use the Showcase Localization/RTL lab for provider refresh, mirroring, popup direction, and customization dialogs. Check affected themes/DPI scales when rendering or geometry changes.
- Test actual input, OS IME, designer interactions, and mixed-monitor transitions on the relevant live surface. Build/startup and synthetic input alone do not prove these behaviors.
- Measure resize performance with the built Release executable outside the debugger. Preserve the task's workload, cache, and memory conditions when comparing runs.
- For CI visual mismatches, retrieve and inspect failure PNGs before adjusting snapshots; retain the baseline until the intended appearance is established.

If a required environment or live gate is unavailable, complete independent checks
and report the specific remaining gate. Do not claim acceptance or expand into an
unrelated workaround. Stop expanding verification once the selected checks pass
and the reported failure is addressed, unless evidence reveals another relevant risk.

## Close or hand off

Inspect the final diff for scope and unintended behavior changes. Record new
subsystem pitfalls in the matching `docs/history/` entry/index and Writer friction log when
applicable; avoid duplicating status into agent instructions.

Report the changed behavior and actual validation results, distinguishing automated
proof from live acceptance. For a continuation, include branch/tip, accepted state,
next bounded task, constraints, and remaining gates. A side question or correction
updates the active task without discarding its completed work.
