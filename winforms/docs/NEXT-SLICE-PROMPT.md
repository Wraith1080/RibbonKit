# Continue native Office 2007 after the framed Backstage addition

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-21**. Last verified checkout: `codex/Ribbonkit-Winform` at
`8663a3f` (`2007 Backstage Implementation`). G6 and the subsequent framed
Backstage addition are committed. The stationary-Orb/message-panel follow-up is
implemented and uncommitted. Start with
`git status --short`, verify the live branch/HEAD, and preserve user edits and
commits. Do not stage, commit, push, publish or change branches unless requested.

## Current state and next boundary

Read the [current themes review ledger](THEMES-PLAN.md#current-status-and-review-ledger)
and [Office 2007 contract/slice order](OFFICE2007-BLUE-PLAN.md). The ledger owns
user acceptance separately from automated, native-print and sampled evidence.

- G2 exact hard transitions and measured group panels/label bands are shared
  library foundations. Preserve immutable copied stop data, the unchanged strict
  constructor, exact duplicate-offset transitions, all-stop mapping and full-surface
  sampling/clipping.
- G3 Orb/menu composition and G4 normal opaque Blue preset are committed. Preserve
  append-only identity 4, designer mapping, both permanent View gallery tiles,
  explicit rectangular File overrides, native hit testing and host-owned routing.
- G5 Application Menu palette and the
  [caption-clearance correction](OFFICE2007-BLUE-PLAN.md#g5-caption-clearance-correction--2026-11-21)
  are committed. Floor below-opening frame origin at the caption bottom before
  measuring Orb/content clearance. Retain the Orb cutout, stable command positions
  and working-area/above-caption fallback.
- [G6 Options and companion surfaces](OFFICE2007-BLUE-PLAN.md#g6-options-and-companion-surfaces--2026-11-21)
  is complete with the corrections below. Preserve independent ordinary/primary action rims,
  exact blue primary creases and washes, gold selections, white native text wells,
  scoped ScreenTip fill/rim and additive public API entries. The subsequent
  [gradient/thin-outline correction](OFFICE2007-BLUE-PLAN.md#g6-gradient-and-button-outline-correction--2026-11-21)
  replaces the flat backing with the full-height reference valley in ordinary
  popups, the Options body and native list/tree/icon wells. BackgroundFill and
  ListBackgroundFill retain Color-setter authority and null resets. Default
  action outlines are one device pixel; explicit logical widths and focus cues win.

The Designer Sample was refreshed **in place**, normal Debug/default output,
with package `0.1.0-designer.local20261121T155405129Z`; authored Forms/resources
were preserved. Select **View > Themes > Office 2007 Blue**, then the existing
Options/customization commands. ScreenTips retain their near-white role and native
text editor wells stay white; list/tree/icon wells use the reference gradient.

G6 evidence under `artifacts/office2007-g6/` records 275/275 affected xUnit cases,
40 byte-exact old-family package image pairs, an independent public-only consumer
and both actual samples. Use the G6 record for final log paths, commands, native
background checks and limits; keep earlier attempts. This is focused evidence,
not a full-suite or physical-input result.

**G6 is wrapped up; the user paused G7 for a separate Backstage addition.** Read
the [Backstage record](OFFICE2007-BLUE-PLAN.md#framed-backstage-workspace-before-g7--2026-11-21)
before continuing. The accepted proposal uses one frame around both panes, a
single divider, blue return header and equal row/separator padding. The requested
side clearance matches the top: **4 logical pixels**, with an 8-pixel frame gutter.
G7 has not started. After continuation is authorized,
G7 is the composed normal-theme check across both existing samples and an
independent consumer: Classic/Simplified, Compact/Touch, states, native editors,
File routes and resize/maximize/restore. Reuse passed scopes; add checks only for
new dependencies or concrete gaps. G8 is a separate palette on the existing
Aero frame, after normal-theme acceptance; theme selection must not enable Aero
or overwrite a host's File route/material preference.

## Latest G6 command and Simplified correction

Read the [split/QAT/group/overflow record](OFFICE2007-BLUE-PLAN.md#g6-split-qat-simplified-groups-and-overflow-correction--2026-11-21).
The user added vertical split-arrow gel, gold QAT hover, separator-only Simplified
groups and gold/orange overflow feedback to G6. These are delivered through six
additive appearance members, with existing layout, callbacks and input retained.
Current review belongs in the ledger. That correction's selected run passes 117/117,
including 19 new cases; two initial legacy checked-hover assertions are recorded
separately and must not be represented as passing. Read the record for final
package/sample comparisons and fixture limits; preserve earlier evidence.

## Final G6 caption correction

Read the [caption hover-outline completion](OFFICE2007-BLUE-PLAN.md#g6-caption-hover-outlines-and-slice-completion--2026-11-21).
The clarified request is hover-only borders: blue for minimize/maximize/restore,
red for Close, with corresponding pressed rims; idle stays borderless. This uses
existing StateChrome/CloseStateChrome roles without renderer/input/API changes.
The latest selected run passes **55/55**, including nine new cases at four DPIs.
Independent package and both actual samples pass native caption and prior G6
checks; 40 existing-family image pairs remain exact. Three old caption-backdrop
assertions fail identically with the retained G5 runtime, separate from the earlier
two checked-hover split limits. Preserve their logs and the clean selected runs;
do not claim full-suite success. Current user acceptance belongs in the ledger. G7 remains the
next composed review slice, and G8 Aero remains separate.

## Framed Backstage addition

The [new Backstage record](OFFICE2007-BLUE-PLAN.md#framed-backstage-workspace-before-g7--2026-11-21)
owns this separate pre-G7 implementation. Appended BackstageStyle.Office2007=3 and
BackstageAppearance.Workspace provide the blue header, one shared frame/divider,
gold navigation and equal four-pixel row/separator top/side spacing. The virtual
header hook preserves existing Back/File/glyph/text hooks. Native Info/Print
controls and application content remain; the mockup's illustrative document
operations are not injected. Default frame strokes are one device pixel; explicit
logical strokes and independent null/Empty/Transparent/zero resets remain.

Initial addition evidence under `artifacts/office2007-backstage/`: **133/133** selected xUnit
cases, 17 new; independent package and both actual samples pass four Backstage
scopes each at 120 DPI, and **32 byte-exact** G6/current old-family image pairs match.
Use `*-delivery` logs and corrected visible-HWND native prints. `public-final/`
whole-Form DrawToBitmap images are explicitly rejected. No full-suite, actual VS,
physical-input/IME, mixed-monitor or new motion/material acceptance is claimed.
Current design approval and pending runtime review belong in the ledger. G7
remains paused; no Aero palette or accepted Backstage investigation was reopened.

## Latest Backstage paint correction

Read the [Orb-only animated-paint correction](OFFICE2007-BLUE-PLAN.md#orb-only-return-and-animated-paint-correction--2026-11-21).
Office2007 now uses the Orb as return without a separate Back button by default.
Explicit BackButtonVisible=true remains an opt-in. Header Orb paint stays in
settled pane coordinates while the retained native drawing moves. One effective
paint-mode decision makes the opaque workspace use Solid preparation before
movement, including on Mica hosts, without changing their material preference.
The five reproduced failures and earlier attempts are retained. Final focused
evidence is **152/152**, including four new None/Mica x Subtle/Expressive animation
cases; independent package and both samples pass Orb return and prior contracts,
with 32 byte-exact old-family image pairs. Use `artifacts/office2007-backstage-paint/`
for that correction's evidence. Human follow-up animation review remains pending in the
ledger, and G7 stays paused. No new Zoom/desktop-recording investigation was run.

## Latest Backstage accent, Orb KeyTips and caption join

Read the [accent/KeyTip/caption-join record](OFFICE2007-BLUE-PLAN.md#backstage-accent-orb-keytips-and-a-joined-standard-caption--2026-11-21).
The header now follows Accent through the existing File recipe, retaining exact
default blue. Explicit header/File fills, null resets and zero strength keep their
authority. The active Orb shares the navigation/footer KeyTip resolver; its File
KeyTip closes through native page validation. A matching colored standard caption
and header share one full gradient canvas, retaining the caption crease once.
Neutral captions, actual Aero and explicit Form/header overrides stay independent.
JoinColoredCaption=false opts out; BackstageHeaderBackgroundBounds exposes the
shared paint canvas without changing content geometry. Four additive getter/init
entries are recorded in Unshipped; the shipped baseline is unchanged.

Latest evidence under `artifacts/office2007-backstage-accent-keytips/`:
**184/184** focused cases pass, including 15 new; independent package and both
actual sample paths pass four Compact/Touch x Classic/Simplified scopes each at
120 DPI. All **32** retained-G6/current old-family paint pairs remain byte-exact.
Use `*-delivery` logs/directories and the final in-place package identity above.
The background native verifier uses the public control's PreProcessMessage route;
the global shortcut filter correctly rejects an unfocused host. Physical Alt/F10,
actual VS, mixed-monitor and live motion/appearance review remain separate.
Current user review belongs in the ledger. **G7 remains paused.**

## Stationary Orb and message panel foundation

Read the [stationary-Orb/message-panel follow-up](OFFICE2007-BLUE-PLAN.md#stationary-orb-and-office-2007-message-panel--2026-11-21)
before further implementation. The supplied clips showed a moving lower Orb
fragment and overlapping glyphs. A small passive plane now owns the whole Orb
through opening/closing, at fixed native File bounds/full opacity; original
File/Back input, measured header hooks, live pages, fade and Solid preparation
remain. Native paint is ready before handback; cancellation/cleanup releases it.
Explicit rectangular File and other Backstage styles retain existing behavior.

Office 2007 messages now restart the WPF pale-gold ramp per measured row, with
amber frame/separators, blue shield/exclamation, white action buttons and themed
active rims. Additive MessageAppearance adjusts this independently; null retains
legacy paint, and existing MessageBackground/MessageBorder/padding/height roles
keep authority. Alpha is composited once; frame clipping, regular title
measurement/paint, actions and dismissal use shared library paths. There are
39 additive Unshipped entries, including generated record members; shipped stays
unchanged. No Showcase resources, new diagnostic tabs or fabricated app content.

That follow-up's focused evidence is **156/156**, including 16 new cases; this is a new
selection for motion/message dependencies, not a cumulative count with the prior
184. Use `artifacts/office2007-orb-message/*-delivery` logs/directories.
Independent package and both actual sample paths pass four native scopes each at
120 DPI, including message ramps/action/dismissal, and 32 old-family combined
Backstage/message paint sheets remain byte-exact against the pre-follow-up package.
Supplied video frame sheets and native message prints are inspected evidence, not a new
desktop recording or live acceptance. G7 remains paused pending continuation.

## Latest Orb flicker correction

Read the [Orb flicker correction](OFFICE2007-BLUE-PLAN.md#orb-layer-handoff-and-flicker-correction--2026-11-21)
before continuing. The user subsequently reported flicker during both transitions.
Native paint was suppressed before the replacement plane had been shown; the
retained repro fails on None/Mica opening. The plane is now fully published before
ownership changes, and identical stationary pixels skip repeated native uploads
and positioning. Renderer/icon callbacks still run, changed pixels update the
same HWND, and reentrant cancellation cannot leave an orphan plane. No new public
API or message-panel change. Latest **94/94** focused cases pass, including six
new handoff/publication/cancellation cases. Use `artifacts/office2007-orb-flicker/`
for current package/sample evidence and fixture limits. Live flicker acceptance
remains pending; G7 stays paused.
Independent package and both actual sample paths pass their four retained native
scopes plus opening/closing Orb timer checks under hidden owners; all 32 old-family
Backstage/message paint pairs remain byte-exact against the pre-flicker package.

## Preserve accepted work and verification boundaries

- Keep the accepted Backstage black-bar fix under Zoom, ribbon repaint performance,
  backdrop group-title alignment and Showcase frame/File-route persistence. Read
  [the motion/preparation record](BACKSTAGE-PAGE-MOTION.md#full-solid-pane-preparation-under-zoom)
  only for a concrete regression. Do not repeat desktop capture or reopen those
  investigations speculatively.
- Keep all edits under `winforms/`. Use **one agent**, normal Debug/Release
  configurations and default output folders. No computer-use tools or WPF builds.
  WPF tokens/shared templates are read-only design references.
- Follow repository instructions, the relevant design-index link and bounded
  subsystem history. Reuse the [architecture map](THEME-ARCHITECTURE-PLAN.md) and
  [adjustability audit](THEME-EFFECT-ADJUSTABILITY-AUDIT.md). Do not replace layout,
  input, rendering or native editor ownership to implement a palette.
- New public effects must be additive and independently adjustable, with XML docs,
  Unshipped baseline entries, null/Empty/alpha/zero/reset and atomic validation.
  Preserve the shipped baseline and existing renderer hooks.
- Native HWNDs, parents, values, selection, focus, scrollbars, validation,
  source/draft identity and popup ownership retain their existing owners.
  App-authored pages, content, icons, persistence and platform participation remain
  application-owned. Portable effects belong in the library, without Showcase
  resources or helpers.
- Keep diagnostic tabs retired. The ordinary Designer Sample has authored
  Home/Edit/Gallery/View only and no test code. Use
  `eng/Test-DesignerSample.ps1 -List` and its dedicated package-only test host;
  do not move checks back into the sample. Refresh with
  `eng/Update-DesignerSample.ps1` only when required by changed runtime/package code.
- Record focused xUnit, package/native print, actual VS, physical input/IME,
  mixed-monitor DPI, desktop material/continuous motion and live user review
  separately. Do not rerun unrelated matrices or call sampled evidence acceptance.

Previous family context remains in the themes ledger and bounded
[Office 2010 records](OFFICE2010-BLUE-PLAN.md) /
[Office 2013 records](OFFICE2013-LIGHT-PLAN.md). Preserve their accepted contracts;
these are not instructions to repeat their passed matrices for G6/G7.
