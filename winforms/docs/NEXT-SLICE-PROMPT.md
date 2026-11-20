# Continue normal native Office 2010 Blue after F6 review

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-15**. Last verified branch is `codex/Ribbonkit-Winform`, HEAD
`9a70d65` (`2010 F5 Application Button`). The user committed F5; F6 entered
with a clean worktree and its changes remain uncommitted. Preserve authored Backstage
settings and other Form/resource edits. Recheck
`git status --short`, live branch and HEAD; preserve user commits and edits. Do not
commit, stage, push, publish or change branches unless requested.

## Current state and scope

The user selected **normal Office 2010 Blue** and supplied eight WPF references,
including Add Message hover, then explicitly confirmed rounded corners for the
non-flat theme. Read the [F1 appearance/API contract](OFFICE2010-BLUE-PLAN.md).
F1 changed only documentation and unchanged reference copies; F3 now exposes the
2010 ribbon/File preset. On 2026-11-15 the user added dedicated Application Menu
and Options theming slices and made the Aero-inspired frame a required later
stage, after normal-theme review. WPF remains read-only reference.

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
no startup latency gain is claimed. This cleanup was committed before F2.

**F2 shared gradient/state-chrome foundations are implemented.** Read the
[delivered contract and evidence](OFFICE2010-BLUE-PLAN.md#f2-implementation-and-verification--2026-11-14).
Final focused Release checks pass 85/85, including 22 F2 cases. Independent
public-package feature/native lifecycle checks pass and all 24 retained
default/two-stop paint image pairs match byte-for-byte. Normal Debug runtime,
designer server/net472 client and both samples build with zero warnings/errors.
F2 was committed by the user at `ec78e11`. Direct paint/offscreen native evidence
does not establish actual 2010 visual acceptance or actual VS review.

**F3 ribbon/File shell and both View galleries are implemented and committed.**
Current user review is recorded once in the themes ledger. Read the
[contract and exact evidence](OFFICE2010-BLUE-PLAN.md#f3-implementation-and-verification--2026-11-14).
Office2010Blue = 3, immutable IdentityAccent reference ramps and existing
Filled/connected-header helpers provide blue surfaces/File, gold command states,
rounded controls and the existing Rounded native request. Both actual sample
gallery/accent/title/Reset paths, four layout/density scopes, three QAT placements,
both File routes and native content retention pass offscreen checks. Focused
Release designer serialization/Undo/Redo/SVG/paint checks and the independent
public-package consumer pass. Showcase startup persistence and sample isolation
pass too. The normal Designer Sample still compiles zero test methods; all new
checks stay under tests. This F3 record predates the F4 Backstage below; no Aero was implemented. File
uses linear ramps/rims; a separate radial reflection is not claimed.

F3 review follow-up corrected hover-tab bottom outlines and a solid patch around
minimize. The reviewed result and authorization to continue are in the ledger.
Office 2010 sets FilledTabBottomBorderVisible=false for hover/pressed states;
selected connection/File remain independent. Minimize and TabRow QAT keep the
full header fill coordinates, with alpha-safe restoration. The user also requested
general FileButtonMinimumWidth: 1..1024 logical pixels, null=legacy 50 fallback,
Touch>=44, existing text/narrow clamps. Presets 2010/2013/2019 use 64; 2024 stays
unchanged. Focused Release scopes pass 28/28 and 65/65; independent package
`-HeaderOnly` checks pass. See the [bounded correction record](OFFICE2010-BLUE-PLAN.md#f3-header-review-corrections-and-shared-file-minimum--2026-11-14).
F4 was subsequently authorized and is delivered below.

**F4 below-tabs Backstage is implemented; user visual review is pending.** Read
the [delivery, evidence and review guide](OFFICE2010-BLUE-PLAN.md#f4-implementation-and-verification--2026-11-14).
Office2010 = 2 has sibling layout/drawing plus measured placement/caption/motion
registration. The real caption/tab row/QAT stays exposed, File stays open, visual
tab selection is suspended, and validated tab/File/QAT actions retain native
ownership. Pale rail, square blue selected row, open-File seam, left divider
shadow and independent Back/seam/shadow/identity roles are shared public behavior.
Designer preview is inert and reflects open File/tab paint without runtime state.
Host Layout ignores the pane's own motion Location updates; otherwise it cancels
travel. The final correction passes 24/24 affected Release cases.
Latest selected focused results pass 210 distinct cases (not a full-suite run).
Independent package/native checks, 12 byte-exact old-theme Backstage paint pairs,
and both actual sample checks pass. Normal Debug runtime/server/net472 client,
samples and dedicated host build cleanly. Evidence is under
`winforms/artifacts/office2010-f4/`; retain earlier failed/repair records too.
Actual VS/physical/continuous visible motion/corner/material/mixed-monitor gates
are separate; no Aero or final normal-theme acceptance is claimed.

**F4 visual review follow-up is delivered; current review is in the ledger.** The user supplied
eight concrete corrections plus clarification that both the whole card viewport
border and dimensional custom-card states are wanted. Read the
[requested-fix checklist and exact limits](OFFICE2010-BLUE-PLAN.md#f4-visual-review-corrections--2026-11-15)
before proceeding. Caption-button glass is scoped to those buttons; the required
full frame follows in F8 after F7. Shared role additions retain null/Empty/zero/reset
and existing public renderer hooks. Sample cards use DrawGalleryTileState while
their editable artwork remains host-owned. Shared text paint honors the existing
body viewport clip. Focused Release 155/155 plus two real narrow-body cases pass;
independent old/current-package checks reproduce 2,526 -> 0 leaked text pixels
and 0 -> 286 gallery state-border pixels. Both sample review checks pass. Extra
foreground inline-focus verification fails identically on old package/card sources;
the foreground popup check loses activation. Preserve those limits and their logs.

The subsequent selected-navigation correction is delivered; current user review
is recorded in the themes ledger's selected-navigation row.
Read the [hue/lower-band record](OFFICE2010-BLUE-PLAN.md#selected-navigation-hue-and-lower-band-correction--2026-11-15).
The old RGB channel transfer visibly shifted Red toward purple; the reference
ramp also peaked near its bottom. Navigation now preserves accent hue while
transferring lightness/saturation, and its default ramp lightens smoothly.
Selected/pressed rows and outlines share that mapping. Public general AdaptFill,
File/caption recipes and all other reviewed surfaces stay unchanged. Explicit
roles, strength/zero/Reset, alpha and direction retain precedence. Focused Release
24/24, old/new public-package measurements and both samples' six Accent choices
pass; red hue error improves 64.14 -> 0.53 degrees with no lower lightness reversal.
Native editor handles/parents/text/RTF/selection remain. Do not repeat unrelated
matrices or infer selected-row visual acceptance from these measurements.

B1–D8b, all identified A00–A17 corrections and E2–E4 are implemented. E5 automated
composed checks are delivered. Current review, evidence pointers and remaining gates live in
the [themes ledger](THEMES-PLAN.md#current-status-and-review-ledger).

Interactive diagnostic/theme-effect/geometry tabs have been retired. The
permanent Designer Sample now starts with authored **Home/Edit/Gallery/View**.
Both apps now offer View's theme/accent galleries and Color title bar toggle.
The three accepted light themes, new Office 2010 shell and Application Reset use public APIs. Six
accents plus Default update derived fills; colored captions use white title ink.
The Designer Sample controls are authored fields/collections, not runtime-only
review tabs. Its SVG constructors now explicitly supply both arguments; actual
source load/save/resource reload checks protect the reported designer artwork loss.
`--office2019-light` selects 2019 without review tabs;
`--office2019-theme-demo` is a selection-only compatibility alias and
`--menu-geometry-demo` no longer creates tabs. Explicit `--verify-*` fixtures
and their retained builders are available through the dedicated test host.
Do not automatically resurrect diagnostic
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
`0.1.0-designer.local20261120T014843042Z` after the F6 listbox repaint fix.
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

## Current review boundary: F6 Options; next F7 composed review

F6 is delivered, pending user visual review. Current F5 acceptance is in the
[themes ledger](THEMES-PLAN.md#current-status-and-review-ledger). Read the
[F6 implementation/evidence and review steps](OFFICE2010-BLUE-PLAN.md#f6-implementation-and-verification--2026-11-15).
Preserve the user's final clarification: selected rows keep their accent outline,
but all default Options outlines are thin one-device-pixel lines. Explicit wider
logical widths still win. Native focus cues and the separate navigation marker
retain their owners. Ordinary buttons share the Application Menu footer palette;
primary OK has an accent gradient with darker hover/press and white text.
Native scrollbar skin remains Windows-owned; wells, frames and rows are themed.

The [listbox repaint follow-up](OFFICE2010-BLUE-PLAN.md#f6-listbox-repaint-follow-up--2026-11-20)
is in the refreshed package. Hover repaints just the changed rows without erasing.
Each themed row draws into a buffer before display. Focused native checks and both
samples pass. See the themes ledger for the live review result.

Latest focused evidence records 161 passes and one unrelated popup-context
failure reproduced on the retained pre-F6 runtime. Independent public-package
and both actual sample checks pass at 120 DPI, including Blue/Red, three editors,
native hover/scroll/selection/ownership, drafts, validation/Apply/OK/Cancel/Reset
and real secondary group/rename dialogs. Current package is above; evidence is
under `artifacts/office2010-f6/`. Keep intermediate attempts. No clean full suite,
actual VS, physical-input/IME, mixed-monitor or final visual acceptance is claimed.

Stop for F6 visual review. Once the user authorizes continuation, do only
[F7 composed normal-theme review](OFFICE2010-BLUE-PLAN.md#f7-composed-normal-theme-review).
Reuse passed focused evidence and preserve explicit overrides/reset, native
controls/input/layout, source/draft identity and the accepted Backstage handoff.
F8 remains a required separate frame contract/experiment after F7.

The user revised the remaining sequence on **2026-11-15**:

1. [F5 Application Menu theming](OFFICE2010-BLUE-PLAN.md#f5-application-menu-theming).
2. [F6 Options dialog theming](OFFICE2010-BLUE-PLAN.md#f6-options-dialog-theming).
3. [F7 composed normal-theme review](OFFICE2010-BLUE-PLAN.md#f7-composed-normal-theme-review), moved from the former F5.
4. [F8 required Aero-inspired frame experiment](OFFICE2010-BLUE-PLAN.md#f8-required-aero-inspired-frame-experiment).

Reuse passed F2–F6 and E2–E5 evidence; preserve the recorded Backstage approval
unless a concrete regression appears. Do not repeat unrelated passed matrices.

F6 owns the library Options/customization dialogs and draft/button contracts.
F7 closes normal-theme review before F8. The Aero-inspired frame is required
planned work, with its own host/theme/material/fallback contract and review;
caption-button glass alone does not complete it. Do not start F8 during F7.
No diagnostic tabs, temporary review solution, new family or dark
variant. Keep native API/print evidence separate from actual VS, physical input,
continuous motion, desktop material and mixed-monitor acceptance.

## Preserve delivered contracts

- Keep F3's public preset/enum identity and sample SVG constructors. File's four
  explicit fills precede IdentityAccent ramps, which precede solid/darkening
  fallback. FileStrength/CaptionStrength and per-role reference ramps stay
  independently adjustable. Gold states and ordinary command ink do not follow
  identity accent. Existing explicit header border ink wins; selected outlines
  stay independent from FilledHoverBorderColor/FilledPressedBorderColor. Preserve
  File BorderColor and SquareBottomCorners, normal targets, caption/color Reset
  and startup preferences. Actual VS and user appearance acceptance are pending.
- Keep F2's immutable copied/structurally equal stop data, existing RibbonFill
  constructors/endpoints/deconstruction, nullable direction/reset and all-stop
  alpha/darkening. Five fill companions keep old Color setter authority and
  Form.TitleBarColor precedence. Validate every stop in opaque roles atomically.
  BackgroundBounds carries full-surface coordinates for clipping/restoration;
  native editor wells stay solid. Opt-in StateChrome retains existing explicit
  borders, inert empty groups, joined corners and unchanged targets. Public APIs
  are in Unshipped; preserve the shipped baseline.
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
