# Native WinForms: focused tab-header correction

Updated **2026-11-10**. The selected geometry sequence is closed. A subsequent
user-requested tab-header overlap correction is implemented; its focused evidence
and review result are in the [final geometry-plan section](GEOMETRY-CONSOLIDATION-PLAN.md#tab-header-overlap-correction-2026-11-05).
**No further batch or theme is queued.** Workspace:

`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

## Verified checkpoint

- Branch: `codex/Ribbonkit-Winform`.
- Current correction baseline: later user commit
  **`7b1d73368cd251597998a57b054cd63a120e1ea7`**, with a clean worktree at entry.
  Preserve it and later edits; recheck branch/HEAD/worktree before resuming.
- The active [geometry plan](GEOMETRY-CONSOLIDATION-PLAN.md#seventeenth-session-frames-and-shadows-2026-11-05)
  owns the current contract, evidence, limits and review result. Earlier
  gallery/Backstage and tips/caption acceptance stays recorded once in its existing
  rows. Preserve accepted behavior and Roomier Large bands.
- Current permanent Designer Sample package:
  **`0.1.0-designer.local20261105T023755822Z`**. Refreshed once for the focused
  tab-header correction. Runtime/server/client package hashes match normal Debug
  outputs. Do not refresh again for a routine review or acceptance.

## Preserved geometry scope

Six optional code-only RibbonTheme roles cover body/page corner radii and body/
popup shadow blur/vertical offset. Null resets independently and preserves exact
legacy dimensions/pixels. Reuse existing popup radius, shadow colors/enable and
renderer ownership. Three additive device-pixel renderer measurements report body
radius, body-shadow clearance and popup-shadow clearance. Explicit body-shadow
settings reserve at least the renderer footprint and authored margins; authored
Theme.BodyMargin is unchanged. Null retains legacy margins exactly. Existing
command/editor sizes and native one-device-pixel frame insets remain.

Connected body/QAT/messages keep square adjoining corners and one outer shadow;
standalone minimized cards share the same roles. Popup chrome and gallery motion
share clearance with temporary-ribbon dismissal and passive ScreenTip edge inset.
Both null popup-shadow roles retain exact legacy scaled-eight DPI rounding.
Ordinary popup placement clamps the opaque frame; external shadows may be trimmed
at a display edge. Native popup/host regions and Backstage page clipping retain
shared radius hooks. Backstage keeps only its top-left page corner, zero page
shadow inset, native page owners, accepted motion/caption synchronization and
opaque retained designer preview. Theme remains hidden/nonserialized/code-only.
No theme editor, coordinator, native-frame change or additional theme.

## Evidence and limits

- 32 saved default PNG/measurement pairs match exactly across both renderers/
  densities, expanded/minimized and synthetic 96/120/144/192 DPI.
- 19 distinct new unit/STA cases passed across focused runs: validation/reset/
  isolation, four-corner odd/even and signed shadow/DPI matrices, effective margins/
  joins, native HWND/value retention, popup chrome/cache/gallery motion footprint,
  custom renderer clamps, joined QAT separator, opaque designer preview, passive
  tip edge/focus/selection and legacy popup clearance at integer DPI 96..240.
  Final consumer scopes passed 3/3 and 4/4 after the last source corrections.
- Related Release scope: **99 passed / 1 failed**. The native nonclient top-edge
  screen-capture test failed (800/2000 changed pixels; focused retry 603). A public
  probe against the previous accepted package also failed (2000/2000 at 120 DPI).
  It remains an existing capture/frame limit; no repair or clean aggregate is
  claimed. Earlier material/page-switch aggregate failures remain separate.
- Normal Release native solution/API/XML build and pack and normal Debug runtime/
  designer/client/sample builds passed with zero warnings/errors. No full-suite
  run, WPF work, computer-use tools, commit, push or publication.
- Eight corrected public-package renderer/density/layout cases passed at native 120 DPI,
  plus the permanent demo independent-reset/owner contract. Checks retained
  native inline/parked field, popup editor and page HWNDs, exact reset pixels,
  QAT sources, gallery selection, chrome bounds/styles and page clip.
- Four settled composed native renderer/density popup/Backstage capture pairs
  were reviewed at 120 DPI. Initial immediate popup screen captures preceded OS
  presentation; `composed-settled/` corrects that timing. DrawToBitmap captures
  alone do not establish native child-region/layered-shadow appearance.
- Evidence: `artifacts/geometry/frames/`, especially `focused-evidence.json`,
  TRX files, `package-final/`, `composed-settled/`, `package-hashes.json`,
  `native-edge-baseline/` and `release-package/`.
- Actual VS, external UIA/screen readers, physical keyboard/touch/IME/RTL,
  mixed-monitor DPI, material/motion hardware and human review remain separate.

## Combined review delivery

Use the existing permanent `winforms/samples/RibbonKit.WinForms.DesignerSample`
with **`--menu-geometry-demo`**. **Frame geometry** is initially selected;
**Frame joins** contains the connected-panel controls. Normal startup and authored
Forms/UserControls/resources remain intact.

1. Compare Rounder corners, Wider panel shadow and Wider popup shadow, resetting
   each independently. Open the menu/nested menu/gallery in Mouse/Touch and
   Classic/Simplified; check all corners and shadow clearance.
2. On Frame joins, move QAT below the ribbon, show the review message and compare
   expanded/minimized joins: square internal corners and one outer shadow. Check
   TabRow/TitleBar QAT placements too.
3. Open existing Backstage pages, check the top-left corner and retained native
   content/motion. In VS, review the retained opaque preview.
4. Combine previous gallery/Backstage/tip/caption/command settings and reset new
   groups independently, preserving values, native owners and accepted Large bands.

The final user confirmation is recorded once in the active plan's existing review item.
For a routine acceptance, read only that section, batch the text edit and run
`git diff --check`; no rebuild, repeated tests, launch or package refresh.
The selected geometry sequence is closed; this checklist remains a review record.
Additional themes, platform-frame work and unselected intrinsic metrics need a
later bounded instruction; do not begin them automatically.

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
