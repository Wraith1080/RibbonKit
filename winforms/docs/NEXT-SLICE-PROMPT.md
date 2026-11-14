# Continue native WinForms themes after E4 connected outline

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-14**. Branch is `codex/Ribbonkit-Winform`. Accepted E3/sample/button
work was committed locally at the user's request as `5a85343`, on top of user
commit `e6ced78`; nothing was pushed. E4 changes remain uncommitted. Preserve the
user's separate authored Backstage setting and other Form/resource edits. Recheck
`git status --short`, live branch and HEAD; preserve user commits and edits. Do not
commit, stage, push, publish or change branches unless requested.

## Current state and scope

B1–D8b, all identified A00–A17 corrections, E2 corners, E3 palette/File states and E4 outline
are implemented. Current review, evidence pointers and remaining gates live in
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
traces and focused/package checks pass; the random flash itself was not captured,
so user visual confirmation remains open.
See [E4 evidence and limits](OFFICE2013-LIGHT-PLAN.md#e4-connected-selected-tabbody-outline--2026-11-14).
Stop after E4 in this task. On an authorized continuation, start E5; native corner,
physical input, actual VS and composed visual acceptance remain separate.

Native `winforms/` projects only. Keep parent WPF runtime, builds, tests and CI
read-only. Use one agent and no computer-use tools; actual VS review remains
with the user. Read matching repository instructions, source map and bounded
records before edits. Reuse the
[architecture map](THEME-ARCHITECTURE-PLAN.md) and
[adjustability audit](THEME-EFFECT-ADJUSTABILITY-AUDIT.md).

## Next bounded slice: E5 composed light-family review

Review Office 2013 as a composed ribbon in ordinary Showcase/Designer Sample and
an independent public-package consumer. Target unresolved shared-surface/native-
editor combinations and any new review findings. Reuse E3/E4 evidence instead of
repeating passed palette, geometry, corner or material matrices. Preserve selected
tab/body opening, active navigation ink, neutral command text/icons, grey 2019/2024
hover, accent checked/gallery fills, primary OK darkening and secondary washes.
Keep native handles/values/selection/validation, QAT placements, clipping, title
handoff and designer serialization. Separate physical input/IME, actual VS,
desktop material pixels, corner silhouette and mixed-monitor acceptance from
offscreen/programmatic evidence. Review this family before another light family
or dark variant. Do not add diagnostic tabs or begin another family implicitly.

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
