# M6B native window integration and TitleBar QAT

Status, **2026-10-29: bounded implementation complete for review**. Human visual
acceptance, physical monitor/DPI and broader OS/accessibility gates remain open.
M6C backdrop effects and the remaining roadmap are not part of this slice.

## Public contract and ownership

- `RibbonForm : Form` supplies optional reusable caption/window behavior.
  `RibbonForm.Ribbon` borrows an already-added direct `DockStyle.Top` ribbon.
  Caption integration requires empty Form padding, no other visible top-docked
  sibling, and Sizable/FixedSingle/Fixed3D borders. Other arrangements retain an
  ordinary ribbon beneath the separately reserved caption. None/tool-window
  border styles retain their native Form behavior and TabRow QAT fallback.
- The association does not reparent controls, duplicate command collections or
  dispose its borrowed ribbon. Normal Form Controls disposal still applies.
  Null, reparenting, Dock changes or ribbon disposal release subscriptions and
  the association. Another host can associate the same surviving ribbon.
  Source commands, values, bindings, order and native editor handles remain owned
  by the original ribbon/model. Documents, icons, commands, profile storage,
  process DPI declaration and application lifetime remain consumer-owned.
- `RibbonQuickAccessPosition.TitleBar = 2` appends to the existing enum without
  changing TabRow=0/BelowRibbon=1. `QuickAccessPosition` stores the requested
  preference; `EffectiveQuickAccessPosition` exposes the presentation. Unsupported
  hosts or zero available caption width fall back to TabRow and recover when
  support/width returns. Hidden QAT retains its preference and declarations.
  Versioned JSON accepts the new string while retaining older profiles/schema.
- The existing QAT layout, targets, source projections, KeyTips/UIA bridge,
  ScreenTips, overflow and menu/gallery/group/ComboBox popup/editor services are
  reused. The ribbon owns its measured caption band, so targets and screen anchors
  share the same coordinate system. No second command tree or presenter owner
  is created. Groups remain available only in Customize QAT's catalog.
- Supported-host context menus offer TitleBar and explicit TabRow commands.
  The existing above/below and visibility commands remain. The customization
  editor continues editing command membership; placement/visibility stay in its
  existing menu/profile transaction. Apply/Cancel and profile storage ownership
  are unchanged. XML comments, property metadata and Unshipped API entries cover
  the additions; actual VS authoring remains deferred.

## Window and presentation behavior

The frame retains native Form styles and default side/bottom sizing. Public
Win32 nonclient calculation reclaims the caption; one DPI-scaled measurement
supplies icon/title/button painting and caption/system-button hit zones. Native
HTCAPTION supports dragging and double-click maximize; HTMAXBUTTON exposes the
Windows Snap Layout hover. Nonclient button press/release posts standard system
commands. Native system-menu and window lifetime semantics remain in Form/Windows.
The implementation follows the [Windows custom-frame hit-testing contract](https://learn.microsoft.com/en-us/windows/win32/dwm/customframe).

Maximized caption calculation uses `IsZoomed` and the actual default resize inset;
the managed WindowState can still describe the old state during NCCALCSIZE.
Interactive and disabled QAT bounds return client hit tests, excluding them from
caption dragging. Geometry and projections support Compact/Touch, Classic/
Simplified, minimized bodies and messages. File retains its measured left inset.
Backstage reserves the caption; temporary minimized bodies open below the tabs
and retain the existing side margins/overlay rules. Minimized TitleBar messages
keep the standalone not-BelowRibbon frame.

Moves involving TitleBar are **immediate in this first pass**, including menu/
profile routes. TabRow/BelowRibbon movement retains its existing opt-in motion.
Body/header motion keeps a steady caption drawing at its committed coordinates.
No application handler completes motion or dismisses library popups.

Showcase now inherits RibbonForm and associates its ribbon through the public
property. Fresh defaults use TitleBar; saved application customization still wins.
The move command cycles TitleBar → TabRow → BelowRibbon. The independent consumer
creates its own RibbonForm and ordinary Form without Showcase resources/helpers.

## Verification recorded 2026-10-29

- Normal native Debug solution build: **zero warnings/errors**, including runtime
  XML/API and separate design assembly compilation. No WPF build/runtime edits.
- `m6b-host-final.trx`: **10/10** host/placement/ownership tests. Includes synthetic
  caption metrics at 96/120/144/192 DPI and odd/even bounds; fallback/recovery,
  hidden preference, host switching/disposal, File inset, minimized/message
  separation, validation, KeyTips/overflow, profile/menu behavior, Backstage page
  handles/bounds and caption pixels during geometry motion.
- `m6b-motion-backstage.trx`: **5/5**, adding immediate TitleBar menu movement and
  retained TabRow/BelowRibbon animation, plus affected ordinary context motion,
  header drawings and Backstage routing. `m6b-backstage-host-final.trx` adds
  **2/2** for density/association changes while Backstage is open, retained page
  handles, caption drawing and ordinary Backstage routing. Selections overlap
  other scopes.
- `m6b-affected.trx`: **43/44** related QAT/frame/group/combo/menu/minimize/
  customization cases passed. The remaining test expected a source group press
  to reopen an already-borrowed group. Current pre-M6B behavior closes that shared
  presentation; its repaired assertion checks close, next-press reopen and the
  same editor handle. That repair plus the earlier host scope passed **8/8** in
  `m6b-host-repair.trx`. These results are not one clean aggregate or additive count.
- Public-only native projection checks passed at actual **120 DPI**, both
  densities/layouts and Office/neutral renderers:
  `artifacts/m6b-projections-office-final/` and
  `artifacts/m6b-projections-neutral-final/`. Actual pointer actions execute source
  commands once; menu/gallery/chooser/group openings anchor in the caption, group
  HWNDs survive, and minimized bodies anchor below the tab row. Actual popup and
  minimized captures were inspected.
- `artifacts/m6b-frame-office-final/`: completed public-only native frame check at
  actual 120 DPI. Real caption drag, edge resize, maximize/restore/minimize/close,
  Alt+Space system menu, placement/profile changes, hidden/detached fallback,
  ordinary Form rehosting and reattachment/source state passed. Maximized client
  exactly matched the current monitor work area: `(0,0,1920,1140)`.
  `artifacts/m6b-tail-neutral-final/` completes neutral system-menu, minimize,
  close and rehosting checks; the passed frame/projection prefix is retained in
  `artifacts/m6b-host-neutral-final/`. Actual captions, maximized edges, popup
  anchors, system menu and native Snap Layout hover captures were inspected.
- `artifacts/m6b-snap-final/`: completed real Windows keyboard Snap check and
  inspected snapped-window capture. Snap Assist can own foreground afterward;
  that scope performs only read-only capture/cleanup and sends no more input.

Early native failures exposed button-release/default-caption mismatch and the
managed-state maximize inset; both were repaired and their final checks passed.
Earlier hidden-start/capture-name and SendKeys/system-menu probes were automation
diagnostics. Native scenarios now require verified process foreground, spaced
press/release, and a completion marker so closing cannot produce a premature pass.
Failed/interrupted artifacts remain diagnostic; no production focus-stealing
workaround was added. Foreground runs were serialized and processes awaited.

## Try and reproduce

Use the normal Debug Showcase executable, then right-click QAT to select TitleBar,
TabRow or BelowRibbon. Existing saved preferences may choose another initial position.

```powershell
dotnet build winforms/RibbonKit.WinForms.sln -c Debug
dotnet test winforms/tests/RibbonKit.WinForms.Tests --filter FullyQualifiedName~RibbonFormTests
winforms/tests/RibbonKit.WinForms.NativeTests/bin/Debug/net10.0-windows/RibbonKit.WinForms.NativeTests.exe --window-checks winforms/artifacts/m6b-review
```

`--window-checks-flat` selects neutral. `--window-projection-checks`,
`--window-frame-checks`, `--window-tail-checks` and `--window-snap-checks` (and their
`-flat` variants) retain separate scopes without repeating completed work.
The standalone consumer has `--validate-window`/`--validate-window-flat` plus the
same focused switches. This session linked its public-only source into the native
runner; **no fresh-package consumer build** was performed.

## Motion review corrections - 2026-10-29

All three reported issues have bounded repairs for review; the fade style is retained.

| Requested correction | Implementation |
| --- | --- |
| Frame becomes square / line after caption when motion starts | In-window geometry, group and navigation drawings use a layered child of RibbonForm, clipped by its actual native frame. Popup/dismissal windows retain their external presentation. Child position converts screen coordinates to the parent client coordinates. The tab-header crop also includes its caption offset, preventing disappearing/misplaced header ink. |
| Group motion stops on host width changes | RibbonForm no longer calls ModelChanged on every Resize. Native docking commits width and owns the existing group-motion handoff. Caption/DPI/association changes retain their required refreshes. |
| Backstage page flickers after fade | The library keeps its existing live Backstage opacity surface at alpha 255 while visible instead of removing WS_EX_LAYERED and forcing a native page repaint at every fade endpoint. Hide, handle destruction and disposal release it. Existing application-owned opacity remains untouched. Native page controls are neither captured nor moved for animation. |

The frame issue and lost group motion were reproduced before repair. The simple
Backstage page probe did not reproduce the intermittent blink; the final review
uses **Showcase's actual Info and Print pages**, including its bound document-name
TextBox, multiline Notes TextBox, heading labels, NumericUpDown and paper ComboBox.
Both TextBoxes remain live. Handle/value/selection baselines are taken after the
real page first inherits its binding context and DPI, without prematurely creating
unparented handles. No public API or application completion handler was added.

Focused evidence (separate, overlapping scopes; not a clean aggregate):

- Native Debug solution build passed with zero warnings/errors, including API/XML
  and design compilation. No WPF, Release, package or full suite was run.
- `m6b-motion-regression-first.trx`: **7/7** new resize/child-window-coordinate/
  TextBox-opacity lifecycle and external-opacity ownership checks.
  `m6b-fade-stable-layer-first.trx`: **11/11** affected Backstage motion/policy/
  input/close/reopen/callback cases. `m6b-motion-host-related.trx`: **11/11** host
  association/fallback/geometry cases. The two new caption-offset drawing cases
  failed before correction; they and affected ordinary header drawings passed
  **4/4** in `m6b-header-offset-fixed.trx`.
- At actual **120 DPI**, public-only frame captures changed **1,228** sampled
  caption/frame pixels before repair. Final Office and neutral tab-transition
  checks in `artifacts/m6b-frame-office-final-fix/` and
  `artifacts/m6b-frame-neutral-final-fix/`, plus Office minimize checks in
  `artifacts/m6b-minimize-final-fix/`, retain **zero** changed frame/caption edge
  pixels in Compact/Touch. Full-window captures include all four native corners;
  header ink remains in its measured band. Actual intermediate captures were inspected.
- Public-only width checks passed for both renderers, Classic/Simplified,
  Compact/Touch and Fade/SlideDown in `artifacts/m6b-resize-first/` and
  `artifacts/m6b-resize-neutral-final/`. Collapse, continued resize and restoration
  retain playback, native editor handles/values and customization JSON. The native
  pointer check in `artifacts/m6b-motion-input-final/` confirms that the child
  drawing passes a real caption-QAT click to its original command exactly once.
- `artifacts/m6b-showcase-backstage-final/` records actual Showcase opening and
  four Info/Print switches per density at 120 DPI. Sampled settled page pixels
  have **zero** changes after every fade; both TextBoxes retain handles, text and
  selections. Opening, intermediate text and settled captures were inspected.
  `artifacts/m6b-backstage-neutral-final/` adds independent neutral page-switch
  capture/state checks. These sampled runs do not prove absence of every
  intermittent blink on other machines; human review remains open.

Run the native public-only `--window-motion-regression` (or `-flat` variant) with
`frame`, `minimize`, `groups`, `backstage` or `input` and an evidence directory.
Showcase `--capture-backstage-motion <directory>` exercises its existing page tree.
Earlier failing/intermediate captures remain diagnostic, including the initial
caption-only probe and the capture review that exposed the header-offset error.

### Compositor follow-up - 2026-10-29

The user's later recording and displaced navigation screenshot exposed a regression
in the child-overlay repair above. Hover was not required: actual F10/File KeyTips
reproduced it. Correct HWND rectangles and settled/page-only captures missed the
transient misplaced bitmap, so those earlier positive captures do not establish
acceptance of this regression.

`RibbonMotionWindow.Present` now supplies the same converted parent-client position
to both native presentation calls for clipped children. Previously the bitmap
presentation used the screen position before the child HWND was placed in client
coordinates. The original top-level popup path keeps its screen position. The
Backstage fade remains; no page-tree replacement or public API was added.

Follow-up evidence at actual 120 DPI:

- Actual Showcase whole-window capture reproduced **63 displaced navigation
  columns** before the position fix; afterward three real KeyTip open/close cycles
  had **zero** in `artifacts/m6b-flash-parent-position-retry/`. Intermediate frames
  were inspected, including opening and ribbon restoration.
- `artifacts/m6b-unattended-interactions/` passed real pointer hover during opening,
  pointer Back, native TextBox commit, native ComboBox choice and physical width
  drags in **Compact and Touch**. Both resize directions sampled active group
  animation; editor handles and edited values survived. Hover captures had zero
  displaced columns. Typing/drag logs establish input and identity, not pixel
  stability; their saved frames were also inspected.
- `artifacts/m6b-full-editor-document-final/` passed tab switches and ribbon
  minimize/restore with zero sampled visible document or settled-editor changes.
  The comparator ignores RGB differences of at most 12 levels; inspected earlier
  exact-pixel differences were only a one-level tint from the alpha-1 input hold.
- Independent public-only group checks passed all **eight** density/layout/style
  combinations in `artifacts/m6b-compositor-groups-final/`. Frame captures in
  `artifacts/m6b-compositor-static-frame/` retained all four corners, caption edges
  and tab labels with zero sampled frame changes in both densities. A static
  test-owned backdrop excludes moving desktop content under invisible resize edges.
- Native Debug solution build passed with zero warnings/errors. The three focused
  position cases passed in `m6b-child-composition-position.trx`; they validate HWND
  geometry, while the actual full-window capture supplies compositor evidence.

Showcase switches `--capture-motion-input`, `--capture-motion-input-tail` and
`--capture-motion-interactions` take an evidence directory and close their own
process after the bounded check. Foreground checks stop synthetic input if focus
leaves that process. The review replay `artifacts/m6b-compositor-review.mp4` is
assembled from sampled frames; it is not frame-pacing/performance acceptance.
Human visual acceptance and the remaining gates below are still open.

## Remaining gates

Human visual review is pending. Synthetic DPI geometry and native 120-DPI results
do not establish physical mixed-monitor/per-monitor migration, other OS versions,
taskbar auto-hide/multi-monitor work areas, touch/IME hardware, accessibility
compatibility, actual VS designer save/reopen or performance/resource budgets.
No full native/WPF suite, Release, package or broad previous-motion campaign ran.
M6C Mica/Acrylic/glass and comprehensive theme/RTL/localization remain separate.
