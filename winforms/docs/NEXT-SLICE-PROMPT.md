# Continuation: native RibbonKit.WinForms tips/caption combined review

Updated **2026-11-05**. The grouped ScreenTip/KeyTip and caption-spacing session
(15–16) is implemented. **The current acceptance result is recorded in the active
geometry plan; await instruction before frames/shadows or additional themes.** Work in:

`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

## Verified checkpoint

- Branch: `codex/Ribbonkit-Winform`.
- Committed HEAD at session start: `0ac1351bff0f11fcea27831b75d6f71564989919`,
  `Geometry Consolidation 7 WIP`. Recheck branch/HEAD/worktree and preserve later edits.
- Gallery/Backstage implementation and follow-up edits were already uncommitted;
  they were preserved. Their user acceptance is recorded once in the existing
  [geometry plan](GEOMETRY-CONSOLIDATION-PLAN.md) review row. Preserve that behavior.
- The same plan's final 15–16 section owns current contracts, evidence, capture
  limitations and current acceptance row. Do not repeat completed
  checks or refresh packages for a routine acceptance confirmation.
- Current permanent Designer Sample package:
  **`0.1.0-designer.local20261104T142333305Z`**. It was refreshed once for this grouped
  session; runtime/server/client hashes match Debug outputs. Later sample-only
  builds changed capture helpers without changing the library package.

## Current scope and evidence

Both native API/layout/designer contracts were presented before implementation.
Optional code-only RibbonTheme roles cover ScreenTip width/padding/title gap/
placement, KeyTip horizontal/vertical padding, and caption leading/icon/QAT/title
spacing. Each null resets independently. Native caption height/buttons/glyphs,
frame/resize/drag behavior, passive tip timing/focus/cancellation, keyboard targets,
Backstage motion/opaque preview and existing gallery/command geometry remain.

- 32 saved default PNG/measurement pairs match exactly. Synthetic ScreenTip and
  caption-geometry matrices cover 96/120/144/192 DPI; native caption paint in that
  fixture is 96 DPI. The initial composites have a TextRenderer transform overlap,
  so use actual native package PNGs for visual review, not those composite layouts.
- 18 distinct new focused unit/STA cases passed across runs. Related scope passed
  106/106. Normal Release native solution/API/XML build and pack and normal Debug
  runtime/design-server/client/sample builds passed with zero warnings/errors.
- Eight fresh public-package cases passed at native 120 DPI: real passive hover,
  editor focus/selection, caption-derived QAT/client/Close targets, native HWND/value/
  reset and F10/TG badges. The fixture positions its owned window under the
  stationary pointer and stops if the pointer moves or foreground is lost.
- Actual visual evidence: `artifacts/geometry/tips-caption/package-positioned/`
  (the eight completed checks), `package-caption/` (eight caption-only captures),
  `package-hashes.json` and `focused-evidence.json`. Initial fixture placement and
  a later pointer-movement stop are retained with their limits; no clean aggregate
  or completed rerun is inferred from those attempts.
- Actual VS, external UIA/screen readers, physical keyboard/touch/IME/RTL,
  mixed-monitor and new material/motion hardware acceptance remain separate.
  Earlier inherited broader aggregate failures remain unresolved. No WPF work,
  computer-use tools, commit, push or publication.

## Review delivery

Use the existing permanent `winforms/samples/RibbonKit.WinForms.DesignerSample`
with `--menu-geometry-demo`. **Tips geometry** and **Caption geometry** are new;
Tips geometry is initially selected. Authored Forms/UserControls/resources and
normal startup remain intact.

1. Hover the example and menu rows; compare Roomier/Reset ScreenTips and screen-edge
   placement. Check native editor selection and nonactivation.
2. Compare Roomier/Reset KeyTips with F10 then TG and F10 in existing Backstage pages.
3. On Caption geometry, enable Use caption QAT, compare Roomier/Reset, resize for
   title/QAT allocation/overflow and check native buttons/dragging/Backstage caption.
4. Combine prior gallery/Backstage/command/chrome settings and reset each new group
   independently. Preserve values, native handles/owners and accepted Large bands.

Use the active plan for the review result. Remaining estimate: **one grouped frames/radii/shadows/
connected-joins batch**, subject to later authorization. Do not begin it or add
another theme merely because it is on the roadmap.

## Preserved working boundaries

Use one agent and no computer-use tools; leave VS review to the user. Work only
under `winforms/`, with normal Debug/Release outputs. Preserve authored Forms/
resources and later edits. No commit, push, publish, branch switch, stash/reset/
clean, WPF edit/build/test, WPF hosting or dependencies.

Preserve Roomier Large commands' common icon, two-caption-row and arrow bands,
including unused bands, and matching vertical split action regions. Preserve
Application Menu joins/hover/default-pane/validation, state-before-Click/single
invocation, state-command popup retention, callback-created popup retention,
QAT source identity/placements, native fields and gallery presenter ownership.
Host page/card content remains application-owned. Milestone order stays M6A
motion -> M6B window integration/TitleBar QAT -> M6C backdrops -> deferred M4I2
appearance consolidation.
