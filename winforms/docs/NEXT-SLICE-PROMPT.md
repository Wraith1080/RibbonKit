# Continue native WinForms themes after diagnostic-tab retirement

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-13**. Entry baseline was `94c4788` on
`codex/Ribbonkit-Winform`, clean at entry. Recheck `git status --short`, live
branch and HEAD; preserve subsequent user commits and unrelated edits. Do not
commit, stage, push, publish or change branches unless requested.

## Current state and scope

B1–D8b and all identified A00–A17 adjustability corrections are implemented.
Current automated evidence, exact user review and remaining gates live only in
the [themes ledger](THEMES-PLAN.md#current-status-and-review-ledger).

Interactive diagnostic/theme-effect/geometry tabs have been retired. The
permanent Designer Sample now starts with authored **Home/Edit/Gallery** only.
`--office2019-light` selects 2019 without review tabs;
`--office2019-theme-demo` is a selection-only compatibility alias and
`--menu-geometry-demo` no longer creates tabs. Explicit `--verify-*` fixtures
and their builders remain available. Do not automatically resurrect diagnostic
tabs or remove regression fixtures. Preserve authored Forms/UserControls,
resources, project files and normal Debug/Release output locations.

The user selected **Office 2013 Light** and square native form corners for
2019/2013. Read the bounded
[E1 contract and E2–E5 sequence](OFFICE2013-LIGHT-PLAN.md). Its screenshots are
WPF visual references, not native acceptance evidence. This completed slice
removed tabs and planned the work; it did not implement 2013 or window corners.
For the current planning request, finish with a summary. On a later authorized
continuation, start E2 below rather than reopening the completed rewrite.

Native `winforms/` projects only. Keep parent WPF runtime, builds, tests and CI
read-only. Use one agent and no computer-use tools; actual VS review remains
with the user. Read matching repository instructions, source map and bounded
records before edits. Reuse the
[architecture map](THEME-ARCHITECTURE-PLAN.md) and
[adjustability audit](THEME-EFFECT-ADJUSTABILITY-AUDIT.md).

## Next bounded implementation: E2 window-corner preference

Finalize the additive public names and implement the shared policy from the E1
plan: SystemDefault/Square/Rounded/SmallRounded, theme default plus nullable
RibbonForm override and independent Reset. Office 2019 defaults to Square;
Office 2024 retains SystemDefault. Future Office 2013 will use Square. Validate
before mutation, preserve explicit overrides and retain requested values on an
unsupported OS. Do not embed a theme-name condition in native painting.

Use DWM attribute 33 on supported, eligible top-level runtime handles. Cover
initial creation, recreation, effective theme/override changes and restoration
of SystemDefault. Keep designer/child handles inert. Preserve native resizing,
caption actions, maximize/restore/snap, backdrop eligibility and title/pane
clocks. Do not assign Form.Region, remove the native resizable frame, or reset
DWM appearance during the last closing frame. Successful API calls/readback
do not establish physical corner appearance.

Use relevant focused validation for default/override/reset/invalid/unsupported,
theme switching, lifecycle and native coexistence, plus an independent public
consumer. Check the actual supported Windows silhouette separately and report
what was observed. Do not rerun the full geometry, caption, appearance or native
matrix for an isolated setting. No new diagnostic tabs are part of E2.

## Later bounded work

- **E3:** Office 2013 palette and File states. Resolve accent-derived defaults
  versus explicit fill overrides first, then append a stable built-in identity.
  Commands use accent washes; File stays accent-filled at rest and darkens on
  hover/press/open. Cover both CommandMenu and Backstage. Preserve application
  theme Reset, persistence and designer/generated-code contracts.
- **E4:** Connected gray selected-tab/ribbon outline and white tab strip beneath
  an optional colored caption. Measure existing Filled/header/body paint before
  adding a seam policy or structural sibling. Preserve targets, clipping,
  QAT placement, scrolling/contextual tabs and Classic/Simplified geometry.
- **E5:** Composed light-family/native-editor and independent-consumer review.
  Review this family before selecting another light family or dark variant.

## Preserve delivered contracts

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
