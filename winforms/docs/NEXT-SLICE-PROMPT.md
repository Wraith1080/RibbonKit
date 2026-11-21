# Continue native Office 2007 after G6 completion

Workspace:
`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Updated **2026-11-21**. Last verified checkout: `codex/Ribbonkit-Winform` at
`04a757e` (`G5 2007 Theme`). G6 implementation is complete and uncommitted. Start with
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
with package `0.1.0-designer.local20261121T044540709Z`; authored Forms/resources
were preserved. Select **View > Themes > Office 2007 Blue**, then the existing
Options/customization commands. ScreenTips retain their near-white role and native
text editor wells stay white; list/tree/icon wells use the reference gradient.

G6 evidence under `artifacts/office2007-g6/` records 275/275 affected xUnit cases,
40 byte-exact old-family package image pairs, an independent public-only consumer
and both actual samples. Use the G6 record for final log paths, commands, native
background checks and limits; keep earlier attempts. This is focused evidence,
not a full-suite or physical-input result.

**G6 is wrapped up; G7 has not started.** After continuation is authorized,
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
do not claim full-suite success or final live caption acceptance. G7 remains the
next composed review slice, and G8 Aero remains separate.

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
