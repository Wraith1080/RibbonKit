# Continue normal native Office 2010 Blue after F1

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-14**. Last verified branch is `codex/Ribbonkit-Winform`, HEAD
`1802dad` (`office 2010 planning`), after E5 checkpoint `14aba66`. The requested
Designer Sample test/demo cleanup entered with a clean worktree and remains
uncommitted. Preserve authored Backstage settings and other Form/resource edits. Recheck
`git status --short`, live branch and HEAD; preserve user commits and edits. Do not
commit, stage, push, publish or change branches unless requested.

## Current state and scope

The user selected **normal Office 2010 Blue** and supplied eight WPF references,
including Add Message hover, then explicitly confirmed rounded corners for the
non-flat theme. Read the [F1 appearance/API contract](OFFICE2010-BLUE-PLAN.md).
F1 changed only documentation and unchanged reference copies; no 2010 runtime
API/preset exists yet. Aero-inspired frame work is a separate deferred experiment
after normal-theme implementation and review. WPF remains read-only reference.

The user then requested moving all Designer Sample checks into tests and deleting
retired rewrite/consolidation demo files. That cleanup is implemented: normal
startup contains no verification dispatch or check types; use
`eng/Test-DesignerSample.ps1 -List` and its `-Check`/`-Output` options. The dedicated
package-only host links the actual authored sources/resources; no diagnostic tabs
were restored. Thirty-one retired builders and their UI-only smoke assertions are
removed, with control regressions retained. Debug/Release and focused relocated
checks pass. See [scope/evidence/limits](../tests/RibbonKit.WinForms.DesignerSample.Tests/README.md).
Do not move checks or test-only partial members back into the sample. Existing
foreground/VS/physical gates and historical fixture limitations remain separate;
no startup latency gain is claimed. **F2 has not started.**

B1–D8b, all identified A00–A17 corrections and E2–E4 are implemented. E5 automated
composed checks are delivered. Current review, evidence pointers and remaining gates live in
the [themes ledger](THEMES-PLAN.md#current-status-and-review-ledger).

Interactive diagnostic/theme-effect/geometry tabs have been retired. The
permanent Designer Sample now starts with authored **Home/Edit/Gallery/View**.
Both apps now offer View's theme/accent galleries and Color title bar toggle.
The three delivered light themes plus Application Reset use public APIs. Six
accents plus Default update derived fills; colored captions use white title ink.
The Designer Sample controls are authored fields/collections, not runtime-only
review tabs. Its SVG constructors now explicitly supply both arguments; actual
source load/save/resource reload checks protect the reported designer artwork loss.
`--office2019-light` selects 2019 without review tabs;
`--office2019-theme-demo` is a selection-only compatibility alias and
`--menu-geometry-demo` no longer creates tabs. Explicit `--verify-*` fixtures
and their builders remain available. Do not automatically resurrect diagnostic
tabs or remove regression fixtures. Preserve authored Forms/UserControls,
resources, project files and normal Debug/Release output locations.

The user approved **Office 2013 Light** and square native form corners for
2019/2013. Read the bounded
[E1 contract and E2–E5 sequence](OFFICE2013-LIGHT-PLAN.md). Its screenshots are
WPF visual references, not native acceptance evidence. E2 adds the shared
SystemDefault/Square/Rounded/SmallRounded policy, theme default and nullable
RibbonForm override/Reset. Office 2019 uses Square; Office 2024 keeps SystemDefault.
Office 2013 also uses Square. EffectiveWindowCornerPreference reports the
resolved request, not native success or physical appearance. See the bounded
[E2 implementation/evidence](OFFICE2013-LIGHT-PLAN.md#e2-implementation-and-verification--2026-11-13).
E3 includes live AccentStates, explicit state override/reset aliases and a
shared FileButtonAppearance helper. The user's accent follow-up keeps 2019/2024
ordinary hover/press grey while deriving checked toggles, selected galleries and
checked-hover from Accent. Command text/icons keep their ordinary foreground;
accent text belongs to active sibling navigation, not ribbon buttons.
OptionsButtonAccentStates keeps primary OK filled, darkening by 0.10 on hover
and 0.20 on press with white text. OptionsAppearance exposes independent primary
darkening, state-fill and foreground overrides. Other buttons stay white with
light hover/darker pressed washes; pressed borders retain Accent.
Existing explicit button/state/border overrides win. All three families' sample actions
and persistence are checked; the persistent Designer Sample now uses package
`0.1.0-designer.local20261114T044002490Z`.
See [E3 evidence and scope](OFFICE2013-LIGHT-PLAN.md#e3-accent-palette-file-states-and-sample-appearance-controls--2026-11-13).
E4 adds the #919191 body perimeter, selected-tab top/side outline and visible-tab
opening through HeaderAppearance.ConnectSelectedTabToBody. Existing body/header
border settings remain independently adjustable. HeaderBodyConnectionBounds
carries passive geometry to actual renderer hooks, including transient frames.
Ordinary connected-header selection now commits on frame zero while content
continues Fade/SlideDown; this fixes the user's Designer Sample late-border report.
2024 Underline/pill motion stays on its original path; six existing pill cases pass.
The intermittent Office 2013 Designer Sample tab-switch flash has a native
lifecycle correction: stable opaque tab changes retain the live ribbon HWND's
style and finish native child repaints under the motion cover. Before/after
traces and focused/package checks pass. Current user review is in the ledger.
See [E4 evidence and limits](OFFICE2013-LIGHT-PLAN.md#e4-connected-selected-tabbody-outline--2026-11-14).
E5 adds only public-API verification: both ordinary samples and an independent
consumer pass native editor/popup/gallery, live-accent Backstage/page and Options
draft/validation/Apply/Cancel handoffs. Four package layout/density scopes pass;
each sample separately covers its authored Classic/Compact model at actual 120 DPI.
No runtime/API change or new package was needed. See
[E5 evidence and limits](OFFICE2013-LIGHT-PLAN.md#e5-composed-light-familynative-editor-review--2026-11-14).
The current ledger owns completed user review and the separate native corner,
physical input, actual VS, desktop material and mixed-monitor limits.

Native `winforms/` projects only. Keep parent WPF runtime, builds, tests and CI
read-only. Use one agent and no computer-use tools; actual VS review remains
with the user. Read matching repository instructions, source map and bounded
records before edits. Reuse the
[architecture map](THEME-ARCHITECTURE-PLAN.md) and
[adjustability audit](THEME-EFFECT-ADJUSTABILITY-AUDIT.md).

## Next bounded implementation: F2 shared gradient/state-chrome foundations only

Follow F2 in the [2010 plan](OFFICE2010-BLUE-PLAN.md#f2-fill-and-state-chrome-foundations).
Keep existing RibbonFill constructors, Start/End, deconstruction, endpoint `with`
edits and default two-stop behavior. New stop data is immutable, copied defensively,
validated atomically and equal by value. Preserve intermediate stops through
opacity, clipping, motion and actual renderer hooks. Equal endpoints with different
intermediate colors must not take the old solid fast path.

Use nullable fill companions only where Color-only roles block the reference.
Preserve old Color setter authority, Form.TitleBarColor precedence, opacity
validation across every stop, and null/Empty/alpha/Transparent/zero/Reset. Native
editor wells stay solid; host application controls retain their own appearance.
State outlines/inner rims are independently adjustable, opt-in and target-neutral;
retain existing explicit border overrides and 2013/2019/2024 defaults.

Run focused public API/value/paint checks and an independent public-package
consumer with normal outputs. Include stop retention/equality, alpha, equal
endpoints, short bounds and atomic failure. Build affected native runtime/designer
paths and refresh the persistent Designer Sample only when its package needs
updating. Retain evidence and normal Debug/Release output folders. Reuse passed
E2–E5 evidence instead of unrelated palette/border/corner/material matrix reruns.

Stop after F2 with outcome, verification limits and next slice **F3 Office 2010
ribbon/File shell and both sample galleries**. Do not add a partial 2010 identity,
gallery tile, below-tabs Backstage or Aero implementation during F2. F4 is the
below-tabs Backstage sibling; F5 is composed normal-theme review. Add no diagnostic
tabs and do not start another family or dark variant implicitly.

## Preserve delivered contracts

- Keep E2's eligible DWM attribute 33 application, override precedence and Reset,
  recreation/theme updates, unsupported retention and inert designer/child HWNDs.
  Never assign Form.Region, remove the native resizable frame or reset DWM
  attributes during native destruction. API readback is not visual acceptance.
- Geometry consolidation remains the shared foundation: metrics, snapshots,
  DPI/rounding, clips, hit targets, content bounds and popup anchors. This work
  reuses it; do not replace layout engines or reopen the geometry campaign.
- Keep accepted Backstage registration, whole-pane motion, native control
  handles/parents/values/selection/focus/validation and popup ownership. Preserve
  2019's full-height panes, 2024's separate caption, opaque title contrast,
  temporary/settled glyph parity and normal caption paint before layer retirement.
  Preserve D8b caption-button contracts and A10 page preference/width negotiation.
- Retain existing command/split, editor, menu, gallery, tip and dialog drawing
  boundaries and actual application renderer hooks. Keep gallery source identity,
  preview/cancel/commit, borrowed artwork, draft Apply/Cancel and inert designer
  candidate previews. Native text/input remains with its existing owner.
- Keep the corrected shared square icon-picker cell: artwork size and inset
  adapt None/artwork together, with native item/selection/hit/scroll ownership.
- Every newly exposed visual effect must be independently adjustable. Reuse
  existing colors/fills/radii/spacing first; preserve null/Empty/alpha/zero/hide,
  independent/group Reset and validation before Theme/BuiltInTheme mutation.
  No new family selector for a palette name alone, Showcase-only recoloring,
  broad authoring coordinator or application-content restyling.

Run only checks required by changed dependencies and explicit task gates, using
normal native outputs. Reuse passed evidence; broaden only for new failures or
unresolved risk. Keep focused tests, package consumers, direct-print/native API
checks, continuous visible motion, physical input/IME, actual VS and mixed-DPI
acceptance distinct. Inspect actual/diff output before changing approvals.
