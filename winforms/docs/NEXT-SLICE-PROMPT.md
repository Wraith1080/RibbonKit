# Continue native WinForms themes after E2 window corners

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-13**. E2 entry baseline was user commit `fd356b1` on
`codex/Ribbonkit-Winform`, clean at entry; E2 changes are uncommitted. Recheck
`git status --short`, live branch and HEAD; preserve user commits and edits. Do not
commit, stage, push, publish or change branches unless requested.

## Current state and scope

B1–D8b, all identified A00–A17 corrections and E2 window corners are implemented.
Current automated evidence, exact user review and remaining gates live only in
the [themes ledger](THEMES-PLAN.md#current-status-and-review-ledger).

Interactive diagnostic/theme-effect/geometry tabs have been retired. The
permanent Designer Sample now starts with authored **Home/Edit/Gallery/View**.
The user subsequently requested View's permanent theme gallery; it offers the
two delivered light themes plus Application theme Reset through public APIs.
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
Future Office 2013 must use Square. EffectiveWindowCornerPreference reports the
resolved request, not native success or physical appearance. See the bounded
[E2 implementation/evidence](OFFICE2013-LIGHT-PLAN.md#e2-implementation-and-verification--2026-11-13).
E2 is complete as an implementation slice; visible native corner review is still
pending. Stop after E2 in this task. On an authorized continuation, start E3;
do not reopen the rewrite or implement E4/E5 together with E3.

Native `winforms/` projects only. Keep parent WPF runtime, builds, tests and CI
read-only. Use one agent and no computer-use tools; actual VS review remains
with the user. Read matching repository instructions, source map and bounded
records before edits. Reuse the
[architecture map](THEME-ARCHITECTURE-PLAN.md) and
[adjustability audit](THEME-EFFECT-ADJUSTABILITY-AUDIT.md).

## Next bounded implementation: E3 Office 2013 accent palette and File states

Resolve accent-derived defaults versus explicit authored fill overrides first,
then append the stable Office2013Light built-in identity. Commands use adjustable
accent washes; File stays accent-filled at rest with white ink and darkens on
hover/press/open. Cover CommandMenu and Backstage, disabled/focus states and
close/reopen. Preserve application-theme Reset, existing identity values,
persistence, designer/generated-code contracts and independent setting Reset.

Use shared recipes and public settings; no Showcase-only recoloring callback or
family selector merely for a palette. Accent changes must update default state
colors without losing explicit alpha/gradient/Transparent fills. Keep native
EDIT, disabled/inactive colors and backdrop contrast with their existing owners.
Set the 2013 theme's WindowCornerPreference to Square through the delivered E2 API.

Keep the connected outline/white tab-strip structural work in E4. Validate the
affected E3 states and accent/override/reset paths with focused checks and an
independent public consumer. No diagnostic tabs or full unrelated matrices.

## Later bounded work

- **E4:** Connected gray selected-tab/ribbon outline and white tab strip beneath
  an optional colored caption. Measure existing Filled/header/body paint before
  adding a seam policy or structural sibling. Preserve targets, clipping,
  QAT placement, scrolling/contextual tabs and Classic/Simplified geometry.
- **E5:** Composed light-family/native-editor and independent-consumer review.
  Review this family before selecting another light family or dark variant.

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
