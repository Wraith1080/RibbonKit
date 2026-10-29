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

## Remaining gates

Human visual review is pending. Synthetic DPI geometry and native 120-DPI results
do not establish physical mixed-monitor/per-monitor migration, other OS versions,
taskbar auto-hide/multi-monitor work areas, touch/IME hardware, accessibility
compatibility, actual VS designer save/reopen or performance/resource budgets.
No full native/WPF suite, Release, package or broad previous-motion campaign ran.
M6C Mica/Acrylic/glass and comprehensive theme/RTL/localization remain separate.
