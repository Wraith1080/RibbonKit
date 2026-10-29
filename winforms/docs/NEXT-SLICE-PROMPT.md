# Continuation prompt: native RibbonKit.WinForms M6B window integration and title-bar QAT

Continue the native RibbonKit.WinForms implementation in:

`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Prepared **2026-10-29**. The user responded positively to the latest group motion
and shared Fade/Slide selector and requested continuation to the next slice.
The next bounded slice is **M6B: optional RibbonForm/window integration and
TitleBar Quick Access Toolbar hosting**. M6B has not been implemented.

Inspect the current implementation and relevant WPF behavior, state a small
native API/ownership/interaction proposal, then implement the authorized slice
and run relevant verification. Resolve routine choices without asking again;
ask only when missing information materially changes scope or correctness.
Do not implement the remaining roadmap in this session.

## Starting state and working agreement

- Start with `git status --short`. At handoff the branch is
  `codex/Ribbonkit-Winform`, HEAD **07c864c — Ribbonkit Winform - Animation WIP 2**.
  The checkout was clean before this handoff/acceptance documentation edit.
  The user committed the preceding implementation. Recheck live state; preserve
  all tracked/untracked work and do not assume older dirty-checkout notes apply.
- Read root `AGENTS.md` and `winforms/AGENTS.md`. Use one agent. No commit, push,
  publication, branch switch, stash/reset/clean or new worktree unless requested.
- Work in `winforms/`. Keep WPF runtime/build/CI unchanged. WPF provides behavior
  and visual references, not controls/templates to host or runtime code to share.
- Use normal Debug configuration and default outputs. The user explicitly wants
  **relevant tests only**. Retain passed evidence; do not rerun unrelated motion,
  customization or component campaigns. No automatic full suite, Release or package
  campaign. A new failure or affected contract can justify a targeted rerun.
- Reusable ribbon/window behavior belongs in the library and must work for another
  application through public APIs. Commands, document Controls/data, icons,
  persistence storage, startup DPI declaration and application lifetime stay with
  the consumer. Do not make Showcase helpers responsible for shared behavior.
- Comprehensive appearance/theme work, M6C backdrop effects, localization/RTL,
  optional ICommand, accessibility compatibility and actual VS designer work remain
  separate slices. Existing bridges/behavior must continue working. Physical
  mixed-monitor, hardware touch/IME and performance acceptance are separate gates.

## Read bounded current sources

- `docs/21-NATIVE-WINFORMS-PLAN.md`: current banner, optional native-window proposal
  near "Optional native window, proposed 2026-10-26", M6 row and M3A hosting boundary.
- `winforms/docs/M4-STATUS.md`: pre-M6B review fixes, additional corrections and
  Remaining slices. `winforms/docs/M6A-MOTION-PLAN.md`: current contract and final
  Group collapse and restore section. Earlier "next" narration is historical.
- `winforms/docs/M5D-SIMPLIFIED-RIBBON.md`, relevant QAT/customization contracts in
  `winforms/README.md`, and the affected part of `winforms/docs/M5-STATUS.md` only
  if placement/profile/context-menu behavior changes.
- Before code changes, follow root AGENTS' relevant design-index/history pointers.
  WPF `src/RibbonKit/Controls/RibbonWindow.cs` and
  `src/RibbonKit/Controls/RibbonQuickAccessToolBar.cs` are read-only references.
  Do not start WPF implementation or WPF tests.
- Native starting points: `RibbonControl.QuickAccess.cs`, `Model/RibbonQuickAccess.cs`,
  `Layout/RibbonQuickAccessLayout.cs`, `Layout/RibbonLayout.cs`, popup/editor managers,
  customization placement commands/state, renderer/theme contracts, motion helpers
  and Showcase MainForm/Program/manifest. Inspect only affected code.

## Current behavior to preserve

- Native tabs/groups/buttons/splits/options/editors, rich ScreenTips/KeyTips,
  Backstage/application pages, galleries, contextual tabs, merge/modal services,
  Compact/Touch, message panels, QAT/Ribbon customization and Simplified mode exist.
- QAT currently supports **TabRow and BelowRibbon in ordinary Forms**. TitleBar is
  not yet exposed by the placement enum. Reuse the same declarations, source state,
  order, bindings and lifetime; never create another command owner or model tree.
  Native editor Controls retain their handles when borrowed into full-group popups.
- Groups are available as whole entries in Customize QAT, not in Customize Ribbon's
  available-command list. Classic/Simplified share structural customization;
  inline selections and QAT preferences remain independent. Apply/Cancel and
  existing versioned JSON behavior must remain intact.
- Preserve File's measured invariant: `File.Left == scaled BodyMargin.Left`.
  Paint, hit testing, tabs/QAT placement and popup anchors share measured geometry.
- Office 2024 joins a BelowRibbon QAT to the body; ordinary QAT targets are square.
  Attached messages omit the duplicate first top border and use a continuous
  combined shadow. When the body is minimized/hidden and QAT is **not BelowRibbon**,
  messages keep their standalone frame. This condition must cover TitleBar too.
- A minimized tab's temporary body keeps side margins and overlays QAT/messages;
  outside-click dismissal animates without delaying real input or editor return.
- Customization pages share column/search/button geometry. Apply/OK holds native
  tree/list redraw and preserves scroll/handles. Office rounded controls, themed
  ribbon action menus, plain checkmarks, swapped transfer icons and deferred VS
  testing must be retained.
- Simplified More Commands aligns with the body's right inset. Inline ComboBoxes
  have slightly taller fields. While a full-group popup borrows controls, inline
  commands become one medium group button and restore their current values/checked
  states on close. This temporary presentation never changes profiles.
- Showcase Chart/Print Preview have Simplified defaults; Picture Format follows
  normal authored tabs and precedes merged contributions. Tab-header scrollers
  use body-scroller styling/vector chevrons with their own dimensions retained.

## Motion checkpoint

- Existing `AnimationLevel`: None (library default), Subtle, Expressive. Showcase
  opts into Subtle. System-disabled animation and unsafe contexts stay immediate.
- Geometry/layout/density/QAT placement, tab body/header arrival and indicator,
  minimize/restore, popup opening, Backstage/pages/open/close and message motion
  exist. Library coordination owns completion; hosts must not complete transitions
  or dismiss library popups for it. Application content currently reflows immediately.
  Optional host synchronization remains separate unless needed and proposed.
- Latest group motion covers Classic adaptive collapse/restore, Simplified width
  overflow/return and temporary borrowed-group buttons, including minimized bodies.
  It follows the **existing TabContentAnimation Fade/SlideDown selector**:
  Fade crossfades; SlideDown uses leftward group collapse and rightward restoration
  while tab/header travel stays vertical. No separate group selector is wanted.
- Group motion uses 150 ms / 12 logical pixels (Subtle), 210 ms / 20 (Expressive).
  Text/icons retain natural size. Real geometry commits once; inert drawings retain
  no input or model ownership. Continued width changes update arrival without
  restarting the clock. Input, policy/style changes, movement/hiding/disposal settle
  drawings; source values and HWNDs survive. Do not regress old ComboBox native-arrow
  clipping, bold/flickering captured text, stationary tab markers or popup z-order.

## M6B: bounded first implementation

1. Propose the minimal optional `RibbonForm : Form` and ribbon association/hosting
   contract before runtime API edits. Define responsibilities, ownership, attach/
   detach/disposal and ordinary-Form fallback. Keep document hosting application-owned.
2. Add a functional DPI-aware caption/window foundation using public WinForms/Win32
   APIs. Preserve caption dragging, resizing, system menu, minimize/maximize/restore,
   maximized work-area edges and native Snap behavior. Reserve caption/button areas
   consistently; interactive QAT targets must not behave as draggable caption.
3. Add real TitleBar QAT hosting only with a supporting host. Reuse existing QAT
   source declarations and projection/popup/editor infrastructure; no duplicate
   collections, source reparenting or second model owner. Define behavior if a
   TitleBar placement is requested in an ordinary/unsupported Form, or the host
   detaches. No silent invisible/orphaned toolbar.
4. Extend placement/context/customization/profile behavior only as required for
   this host. Preserve existing TabRow/BelowRibbon meanings and older profiles.
   Define TitleBar availability, hidden-QAT retained preferences and host switching.
   Include dropdown/group/gallery/ComboBox chooser anchoring and validation/focus.
5. Use consistent DPI-scaled measured rectangles for painting, hit testing, drag
   exclusion, keyboard targets and popup anchors. Include Compact/Touch, Classic/
   Simplified, minimized bodies and messages. Review BelowRibbon <-> TitleBar
   movement against existing motion; any immediate first-pass fallback must be
   explicit, bounded and documented rather than claimed as animated parity.
6. Demonstrate the optional host through public APIs in Showcase and an independent
   consumer; retain ordinary Form support. Document public additions with XML,
   appropriate metadata and the unshipped API baseline. Actual VS testing remains
   deferred to the designer slice; do not expand into a designer redesign.
7. Start with the existing Office 2024/neutral presentation and relevant checks.
   Mica/Acrylic/glass, broad theme consolidation, native MDI/document management,
   broad accessibility expansion and M6C effects are not prerequisites or part of
   this bounded implementation.

## Evidence to retain and verification for M6B

These are recorded previous results, not a new full-suite pass:

- Normal Debug native solution build: zero warnings/errors, including XML/API and
  design compilation. Focused results/evidence details are in the active plans.
- Latest group checks: 26/26 selected render/source/popup/Simplified regressions;
  6/6 final Fade/Slide drawing/policy checks; lifecycle/fallback/width scopes have
  separately recorded passing prefixes/repairs. Counts overlap; do not add them or
  describe them as one clean aggregate. Earlier failed probes remain diagnostic.
- Final public-only group checks passed at actual **120 DPI**, both densities,
  both renderers and both styles, with real resizing, full-group/temporary-button
  actions, minimized rows, autonomous completion and value/HWND/profile retention:
  `artifacts/m6a-groups-slide-office-final/`,
  `artifacts/m6a-groups-slide-neutral-final/`,
  `artifacts/m6a-groups-fade-office-click/`,
  `artifacts/m6a-groups-fade-neutral-final/`.
- The user's latest positive review authorizes moving to M6B. It does not establish
  performance budgets, compositor/high-refresh tracing, physical mixed-monitor
  DPI, hardware touch/IME, fresh-package or actual VS acceptance.

For the new slice, build only the native solution in normal Debug, select meaningful
new and affected regression checks, and verify the host/title-bar QAT in a public-only
consumer plus ordinary Form fallback. Cover real caption/resize/maximize/system-menu
interaction as feasible and inspect actual captures. Explicitly separate synthetic
DPI geometry, actual native 120-DPI results and unrun physical-monitor/OS gates.
Do not run a full WPF/native/Release/package campaign by default.

Native automation needs verified foreground ownership. Windows can deny a background
SetForegroundWindow request. Stop input when another application owns focus; do not
add production focus-stealing workarounds for test failures. Allow a real press/release
interval so MouseUp reaches its target after focus/capture changes. Do continued-motion
assertions before potentially slow PNG encoding. Serialize foreground runs and await
GUI process exit before the next one; direct PowerShell invocation of a GUI exe may
return before that application closes. Do not close user-owned apps to unlock builds.

Update the native README and the active M6B status/plan with concise implementation,
actual evidence and remaining limits. Keep acceptance in the active plan, not mirrored
through README/history/design indexes. Finish with what changed, where to try it and
what actually ran. No commit/push. No runtime work is needed merely to read this handoff.
