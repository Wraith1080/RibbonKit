# Shared colors and title-bar tint

Status, **2026-11-03: implemented and packaged; color/tint and native consumer
checks passed; the broader Release run retains failures listed below. User
review pending**.
Baseline: clean `codex/Ribbonkit-Winform` at
`1893f0f3a5b7b82466980560e3bd4c9d18c9018b`.
The user selected colors/fills and title-bar tint before themes. Configurable
radii, shadows, spacing and typography remain later work. This slice adds no
built-in palette and stops for review before starting themes.

## Contract

`RibbonTheme` stays immutable and per ribbon. All existing members/defaults and
the shipped API baseline remain. Additions are documented in XML and Unshipped.

| Optional theme role | Unset behavior | Scope |
| --- | --- | --- |
| `RibbonFill? BodyBackground` | `Surface` | Body and joined/standalone below-ribbon QAT frame; opaque solid or vertical two-stop gradient. Editor-label backgrounds and group-motion gaps use the same body origin. |
| `Color BodyBorder` | `Border` | Office body frame/joined separator; an explicit value also supplies the neutral body outline. |
| `Color PopupBackground` | `Surface` | Floating command/group/gallery/choice frames, surface, native choice list, gallery presenter viewport/footer and popup-motion base. |
| `Color EditorBackground` | `Surface` | Native TextBox/ComboBox value field and its library-painted frame/arrow background. |
| `Color EditorForeground` | `Foreground` | Enabled native field text, label, ComboBox selection/choice text and arrow. Disabled native EDIT text remains Windows-owned. |
| `Color EditorBorder` | `Border` | Idle field border. Active/focus/open fields retain `Accent`. |
| `RibbonFill? MenuHeadingBackground` | Existing renderer gallery-heading treatment | Command menu headings only; gallery section headings retain their own roles. |
| `Color MenuHeadingForeground` | Existing renderer gallery-heading treatment | Command menu heading text. |
| `Color TitleBarBackground` | Solid: `Background`; material: clear | Optional default for a RibbonForm caption. |
| `Color TitleBarForeground` | Existing text colors for an unset caption; automatic black/white contrast for a colored caption | Caption text/buttons and TitleBar QAT. |

For colors, unset is `Color.Empty`. Explicit body fill stops and popup/editor/
title-bar backgrounds must be opaque. Existing `Surface` values keep their old
compatibility rules. Application-owned controls/cards/pages are not recursively
recolored. Gallery strips and Backstage page surfaces retain `Surface`; menu,
body and editor state fills retain existing Hover/Pressed/Checked/Accent roles.

`RibbonForm.TitleBarColor` is an ordinary designer Color picker, default/reset
`Color.Empty`, overriding the theme caption color. `TitleBarTintOpacity` defaults
to `.6` and accepts finite values from zero to one. Both have Changed events and
normal designer serialization/reset behavior. Tint covers the existing material
without changing the requested/effective backdrop, HWNDs or native opening
animation. Solid windows use the selected RGB without material opacity.

Contrast uses the chosen color composed over the theme's background for material
mode; it does not continuously sample wallpaper/application content. An explicit
`TitleBarForeground` wins. Caption-specific state colors and foreground are
cached separately, leaving the application theme/body/tab-row QAT untouched.
The close-danger state retains red/white. Backstage caption coverage continues
to use its moving edge and opacity; its covered region retains the Backstage
palette. Restore-button cutouts recompose the same base and overlay once.

## Review

1. Reopen the refreshed permanent Designer Sample solution in Visual Studio.
2. Select the Form, choose **TitleBarColor**, then try reset. Adjust
   **TitleBarTintOpacity** with Mica and Acrylic enabled.
3. Check active/inactive caption text, TitleBar QAT, hover/open/checked states,
   minimize/maximize/restore, and Backstage opening/closing. Check Solid too.
4. Save/reopen and Undo/Redo the two Form properties. Application controls retain
   their own colors. Custom theme roles are application-authored through `Theme`;
   this slice does not add an arbitrary palette editor to Edit Ribbon.

## Evidence and remaining gates

- Normal Debug runtime/designer server/client and permanent package consumer
  builds passed with zero warnings/errors. The normal native Release solution
  build and Release package also passed. Public API analysis passed; there are
  26 additive Unshipped accessors/events, with no shipped-baseline changes.
- All **19 new color/tint tests passed** in Release, covering renderer roles and
  fallback raster equality, gallery/menu independence, retained native editor
  handles/selection, validation, designer Color conversion/serialization/reset/
  events, solid/material coverage, caption/QAT contrast and Backstage composition.
  Evidence: `tests/RibbonKit.WinForms.Tests/TestResults/appearance-release.trx`.
- The Release aggregate completed **1,087 passed / 5 failed / 1,092 total**.
  Failures were the native tree double-buffer check, enabling material on an open
  Backstage, Simplified group borrowing/restoring (non-minimized variant),
  Backstage immediate-page/opaque-layer lifecycle, and customization context
  flyout ownership. The same method families occur in older
  `appearance-20261030/appearance-full-release.trx` and
  `dropdown-content-full-release.trx` records. A fresh process ran only those
  affected methods: **4 passed / 2 failed / 6 total**, retaining the material-on-
  Backstage and opaque-layer lifecycle failures. Evidence:
  `tests/RibbonKit.WinForms.Tests/TestResults/appearance-failures-fresh.trx`.
  This does not establish a clean full-suite pass or fix those existing gates.
- The independent public-API/package consumer passed **six scopes**: Office and
  neutral renderers × Solid/Mica/Acrylic at actual **120 DPI**. Requested Mica and
  Acrylic were confirmed effective. Composed captures verify caption color/reset,
  TitleBar QAT contrast, body gradient, native field roles and command/choice
  popup colors in both densities, with retained HWNDs/selection. Captures under
  `TestResults/appearance-colors/` were inspected. This used local package
  `0.1.0-designer.local20261103T043719003Z`, before the final covered-region
  caption-paint refinement verified by the Release tests.
- The permanent Designer Sample was refreshed to final Debug package
  **`0.1.0-designer.local20261103T044456180Z`**. Authored Forms/UserControls/resources
  were preserved. Its existing ComboBox composed-screen check passed in actual
  Solid/Mica/Acrylic at 120 DPI: **670 field/arrow/list samples**, zero detected
  blanks or black frames through repeated opening/hover/press/closing. Evidence:
  `TestResults/appearance-combo-stability/result.log`.
- The Release package is in
  `artifacts/appearance-colors/release-package/RibbonKit.WinForms.0.1.0.nupkg`.

Physical mixed-monitor DPI changes, high contrast, IME/RTL, exhaustive actual VS
interactions and user acceptance remain separate from automated/native captures.
Fixture-owned input is not physical-input acceptance. No WPF, commit, push or
publish work ran.
