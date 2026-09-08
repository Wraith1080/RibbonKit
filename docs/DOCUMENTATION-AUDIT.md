# Documentation audit — 2026-09-08

Reviewed all 24 authored Markdown files present at the start, including hidden
repository instructions/skill and issue templates. Revised 22; retained the two
small issue templates. Generated `artifacts/github-release-v1.0.0/RELEASE_NOTES.md`
was inspected as an old build/release copy and left unchanged; source release notes
are authoritative. Git internals, IDE caches and bin/obj output are not source docs.
Existing uncommitted instruction-audit changes were preserved and refined.

## Result

| Measure (normalized-LF characters) | Before | After |
| --- | ---: | ---: |
| All authored Markdown, including new history and this report | 947,930 | 807,171 |
| Working/reference docs outside `docs/history` | 947,930 | 155,360 |
| Main design notes | 578,835 | 40,542 |
| README | 21,218 | 10,421 |
| Writer packet plan | 52,282 | 8,309 |
| Writer friction index | 90,313 | 17,273 |

Overall reduction: 14.8%. Working-document reduction: 83.6%.
Moving evidence out of entry-point files is counted separately from actual total
reduction. These are character counts, not tokenizer output or a model-speed benchmark.

## Coverage and decisions

| Documents | Action |
| --- | --- |
| AGENTS, CONTRIBUTING and WPF skill | Preserve prior safeguards/validation; route history reads explicitly and remove stale setup-version wording. |
| README | Shorten features/setup, repair roadmap routing, distinguish current checkout from release assets, correct Writer progress and designer smart-tag claim. |
| Design notes | Keep current state and stable numbered index; move full technical entries into three dated history references and retain the verification ledger. Preserve incomplete gates. |
| Planning overview, architecture, features, roadmap | Retire duplicate inventories and pre-v1 work orders; document actual paths/services instead of nonexistent base APIs, per-theme template forks or the obsolete no-design-tools proposal. |
| MDI and merge/modal | Retain implemented boundaries and remaining work. Preserve source-comment section numbers for merge lifetimes, authored visibility, rebuilds, QAT parking and MDI caption separation. |
| Office 2007 | Preserve all seven original measured tables and frame/File invariants; remove obsolete estimates, completed-stage instructions and experimental-split status. Correct real-orb reparenting to the implemented proxy. |
| Custom projections and future themes | Keep candidate lifecycle, identity, palettes and acceptance constraints; remove verbose speculative API boilerplate and repeated architecture rules. |
| Writer product and Luna packet map | Separate scope from progress; keep packet IDs/dependencies and unfinished gates. Scope historical Luna/max dispatch to explicit requests; remove instructions to recreate completed packets. |
| Writer friction | Index open, corrected and bounded-unsupported outcomes. Retain full reproductions/results in linked evidence; remove the false “None yet” closed summary. |
| Designer setup | Remove redundant solution-add command, stale counts and blanket verification claims; retain package isolation, icon browser, Model API pitfalls and troubleshooting. |
| Icon catalog and visual-test README | Correct source inventory (113 icons, five Large variants; 63 approved PNGs) without claiming tests ran. Remove obsolete reserve labels and duplicated scene inventory. |
| Release notes | Mark release-specific evidence; do not rewrite it as current progress. |
| Earlier agent audit | Keep a short dated record and its official-source links rather than duplicate active instructions. |
| Issue templates | Reviewed; already compact and consistent with current source targets/themes. |

## Verification and limits

- Compared preserved history with the pre-edit snapshot: all 160 numbered entries,
  38 friction entries and the full checkpoint ledger retain their technical text;
  only relative Markdown destinations were rebased. Current index links retain IDs.
- Checked repository-relative Markdown destinations/heading anchors, preserved
  code-referenced section numbers, and parsed README/designer XML examples.
- Checked framework declarations, template aggregation, merge/modal/MDI and orb-proxy
  implementation, icon resources, package setup and snapshot inventory against source.
- Skill metadata validation and `git diff --check` pass. Only Markdown files changed.
- No WPF build, application tests, live UI/IME/DPI/printer acceptance or external-link
  availability check was run for this documentation edit. Recorded prior gates remain
  dated. W1-E live reacceptance, W2-G performance/IME/RTL/default-Paper decisions, W4-C
  hardware checks and narrower library follow-ups remain open where unrecorded.

Maintain current progress in [design notes §5](../04-DESIGN-NOTES.md#5-current-state--next-steps),
public features in [README](../README.md), scope in product/candidate plans, and dated
technical evidence under `docs/history`. Do not copy full status paragraphs back into
multiple plans. History headings may retain superseded terms; their banners identify
them as records, not current execution instructions.
