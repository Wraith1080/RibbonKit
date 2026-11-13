# Native Office 2013 Light and square-window plan

**2026-11-13 — E1 approved; E2 window-corner preference implemented.** Diagnostic-tab
retirement remains in place. Office 2013 runtime work starts with E3; no new
built-in identity or palette is added by E2. E2 entry was clean at user commit
`fd356b1` on `codex/Ribbonkit-Winform`.
Current acceptance remains in [THEMES-PLAN](THEMES-PLAN.md#current-status-and-review-ledger).

## User reference and intended appearance

These are user-provided WPF screenshots used as visual references, not native
WinForms acceptance captures:

- [Neutral caption and ribbon outline](reference/themes/office2013-user-neutral-overview.png).
- [Command hover, selected gallery and tab outline](reference/themes/office2013-user-command-hover.png).
- [Colored caption with a white tab strip](reference/themes/office2013-user-colored-caption.png).

The existing [WPF 2013 tokens](../../src/RibbonKit/Themes/Tokens.Office2013.xaml)
are a read-only color/behavior reference. They identify white surfaces, gray
outline `#919191`, accent `#2B579A`, light accent washes for hover/press/checked,
white File ink and darker File hover/pressed fills. Native implementation must
use its own shared controls/recipes and retain adjustable settings.

| Area | Intended native Office 2013 behavior |
| --- | --- |
| Base surfaces | Flat white caption/tab band/body, square theme-owned chrome, readable dark text. Keep the selected tab white and its text accent-colored. |
| Commands | Hover, pressed and checked/toggled treatments derive from the current accent, replacing 2019's neutral-gray state palette. Use light accent washes for readability, with a stronger checked/pressed treatment; checked-hover remains distinguishable. Cover buttons, toggles, split halves and their QAT/menu projections through shared recipes. |
| File | A full rectangular accent block at rest, white text, darker accent on hover and a deeper pressed/open state. Cover both CommandMenu and Backstage routes, disabled/focus states, closing and reopening. It must not revert to a neutral File button when the menu closes. |
| Ribbon/tab frame | Thin gray ribbon outline and selected-tab top/left/right outline connected to the body. The selected tab's bottom is open into the body, without a double seam or accidental underline. Borders stay visible around the top/header/body connection as in the references. |
| Colored title bar | Optional accent-colored caption with contrasting ink; the tab strip remains white as in the third reference. Do not inherit 2019's default caption-color extension into the tab band. |
| Window silhouette | Square native top-level form corners for both Office 2019 and Office 2013. Office 2024 retains system-default window corners. This is independent of control, popup and Backstage-page radii. |
| Backstage and native pages | Start by reusing the accepted flat Classic Backstage owner/layout and title handoff. Keep A10, native page handles/values/validation and host-owned page-control appearance. Do not add legacy 2010 effects or new motion without a concrete additional requirement. |

## Reuse and bounded extensions

Reuse the neutral command, menu, popup, gallery, dialog and Backstage helpers from
the completed rewrite. Do not clone the 2019 renderer or introduce a family flag
solely to select blue rather than gray.

Three actual gaps need bounded contracts before their implementation:

1. **Accent-derived state defaults.** Changing Accent must update default 2013
   state colors consistently, including derived projections. Keep explicit
   authored state fills authoritative and retain alpha/gradient/Transparent and
   independent Reset. Reuse Hover/Pressed/Checked roles where possible. Resolve
   the live-derivation/explicit-override boundary in the first palette slice;
   do not silently change existing nonnullable public property types or depend
   on a Showcase-only recoloring callback. Any added wash/darkening strengths
   must be adjustable rather than new internal fixed effects.
2. **File state coverage.** Existing HeaderHover/HeaderPressed and the
   HeaderAppearance FileHighlight members do not fully express this contract:
   the current highlight helper covers only an already-open CommandMenu. Add
   the smallest immutable, independently resettable state overrides needed for
   idle/hover/pressed/open fills and ink, with accent-derived 2013 defaults and
   unchanged 2019/2024 fallbacks. Preserve actual text/focus hooks, click targets,
   menu routing and captured closing state.
3. **Connected outline.** Reuse BodyBorder/BodyAppearance.BorderWidth,
   HeaderAppearance.FilledBorderColor/FilledBorderWidth and shared Filled
   primitives first. A selected-tab opening into the body may need a small
   explicit seam policy or a genuine connected-header sibling. Measure current
   paint order and bounds before choosing; do not paint a full rectangle over
   the active-tab opening. Include tab scrolling/collection/visibility changes,
   contextual tabs, minimization, QAT positions, Classic/Simplified and both
   densities. Add no host-owned layout coordinator.

Initial colors should follow the references, but the palette, border width/ink
and state strengths remain changeable public theme settings. User accent changes
must propagate without losing explicit overrides. Inactive/disabled/native EDIT
colors and material contrast remain separate from ordinary enabled button washes.

## Native square-window contract

Yes: Windows 11 exposes `DWMWA_WINDOW_CORNER_PREFERENCE` (attribute 33), including
`DWMWCP_DONOTROUND`. Microsoft's enum documentation lists support from Windows
build 22000. Use the existing RibbonForm native integration boundary rather than
removing the resizable frame or assigning Form.Region:

- [DWM corner preference](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwm_window_corner_preference).
- [DWM attribute and application guidance](https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/ui/apply-rounded-corners).

Additive E2 API:

- `RibbonWindowCornerPreference` with SystemDefault=0, Square=1, Rounded=2 and
  SmallRounded=3, matching the supported DWM values.
- `RibbonTheme.WindowCornerPreference`: SystemDefault for existing 2024, Square
  for 2019. The future 2013 definition must explicitly use Square.
- Nullable `RibbonForm.WindowCornerPreference`; null follows the associated theme,
  explicit values win, and Reset restores theme ownership. Validate enum values
  before mutation. This remains an adjustable setting, not a theme-name check
  buried in native paint code.
- `RibbonForm.EffectiveWindowCornerPreference` reports the resolved request,
  including before handle creation and on unsupported systems. It does not
  report native API success or physical appearance. Without an associated
  ribbon, null resolves to SystemDefault. `WindowCornerPreferenceChanged` reports
  override changes; theme changes retain the override. Property-grid Reset uses
  the nullable default, and the effective property is hidden from serialization.

Apply through DwmSetWindowAttribute on eligible top-level runtime handles and
reapply after handle recreation or an effective theme/override change. Skip
designer/child handles, preserve requested values when unsupported and keep a
safe OS fallback. Restoring SystemDefault when switching back to 2024 matters.
Do not change native caption buttons, resize hit zones, maximize/restore/snap,
Mica/Acrylic eligibility, document controls or caption clocks. Do not reset DWM
appearance while Windows is taking the final closing frame; handle destruction
owns native cleanup. Physical corner captures remain a separate acceptance gate
from a successful API call/readback.

## Implementation order and review boundaries

1. **E2 — window-corner preference, implemented.** Native API/lifecycle evidence
   is recorded below; actual visible corner acceptance remains separate.
2. **E3 — 2013 accent palette and File states, next slice.** Finalize accent/default/override rules,
   append a stable Office2013Light built-in identity, and implement accent-driven
   button states plus the always-colored File block. Keep existing identities,
   application-theme Reset, persistence and designer/generated code compatible.
3. **E4 — connected ribbon/tab outline.** Implement only the measured structural
   difference required for the reference outline and white tab strip, with
   configurable borders and no geometry regressions.
4. **E5 — composed light-family review.** Check remaining shared surfaces/native
   editors and a public-package consumer independent of Showcase helpers, then
   review the whole family before another theme or dark variant.

Use relevant focused checks; reuse passed rewrite/geometry evidence unless
affected. Existing 2019/2024 defaults must remain unchanged except the explicitly
requested 2019 window silhouette. Include alternate accent colors, all button/
File states, alpha/zero/Reset, narrow/scrolling headers, both layout modes and
densities, representative DPI and native page retention. Keep automated paint,
native DWM observation, continuous motion, actual VS and physical/mixed-monitor
acceptance distinct. No new diagnostic tabs are authorized by this plan; use
focused fixtures and ordinary sample/Showcase theme selection when implemented.

## E2 implementation and verification — 2026-11-13

`RibbonForm.Corners.cs` owns the policy-to-DWM adapter, using attribute 33 only
on supported top-level runtime handles. Form association/release, effective
Theme/BuiltInTheme changes, explicit override/Reset, handle creation/recreation
and native settings/theme/composition/style notifications reapply the resolved
request. Enum validation happens before state mutation. No form handle is
created just to store a preference. Unsupported calls leave requested settings
intact; designer and child windows skip application. No theme-name branch,
Region, frame removal, backdrop reset, title/pane clock or paint change is added.
Native handle destruction owns attribute cleanup, including final close frames.

Evidence is under `artifacts/window-corners/`:

- **25/25 focused Release cases pass**, zero skips, in `e2-focused-final.trx`:
  15 new corner cases plus 10 existing caption-handoff cases. Covers defaults,
  all choices, explicit override/Reset, application-theme restoration, invalid
  atomic assignments, metadata, inert designer/child HWNDs, supported DWM
  readback, association/release, resize/caption hit targets, maximize/restore,
  recreation, material coexistence, canceled close and WM_DESTROY readback.
  A dedicated change-during-close case retains both title/pane clocks and the
  native page handle/parent/value/selection. The initial run's new motion fixture
  omitted File visibility; enabling its intended Backstage route and deterministic
  test motion policy corrected the setup. Initial `e2-focused.trx` is retained.
- Normal **Debug runtime, designer server/client and persistent Designer Sample
  builds pass with zero warnings/errors**. The sample now uses local package
  `0.1.0-designer.local20261113T091119035Z`; authored source/resources/project files
  and Home/Edit/Gallery startup remain untouched. No diagnostic tabs were added.
- **6/6 independent public-package scopes pass** in
  `public-final/native/result.log`, Windows `10.0.26300.0`, actual **120 DPI**:
  Office 2019/2024 x None/Mica/Acrylic, Classic/Compact only. Each covers all
  four requests, theme changes and Reset, atomic validation, reassociation,
  recreation, retained live native page handles/parent/text/focus/selection with
  no spurious validation on corner changes, native resizing, HTMAXBUTTON,
  standard system maximize/restore/minimize/close, canceled close and final
  WM_DESTROY. Both requested materials applied on this machine.
  `eng/Test-WindowCorners.ps1` creates a fresh public-only package consumer/cache
  with local build-import barriers and normal bin/Debug output; no project
  reference, Showcase resource/helper or friend access is used. The first broad
  consumer compile encountered pre-existing internal Parent/IsDisposed references
  in unrelated fixtures; the scoped runner compiles only the E2 fixture and its
  entry point, leaving those unrelated fixtures unchanged.

Reproduce from the repository root after `eng/Update-DesignerSample.ps1` refreshes
the normal Debug package:

```powershell
dotnet test winforms/tests/RibbonKit.WinForms.Tests -c Release --filter "FullyQualifiedName~WindowCornerPreferenceTests|FullyQualifiedName~BackstageCaptionHandoffTests"
& winforms/eng/Test-WindowCorners.ps1
```

These are native API/offscreen lifecycle and package checks, **not visual corner
acceptance**. No physical silhouette capture, all-four-corner/DPI symmetry review,
desktop Snap gesture, actual VS session, physical input/IME or mixed-monitor
acceptance ran. Unsupported-OS policy is deterministically exercised, but an
actual pre-build-22000 machine was not available. The accepted title handoff is
protected by focused regression checks; no new visible-motion acceptance is
claimed. No broad geometry/appearance/native matrix, full suite or WPF check ran.
Stop E2 here. Next implementation: **E3 Office 2013 accent palette and File states**.

## Diagnostic-tab retirement evidence

The authored Designer Sample has only Home/Edit/Gallery. Review tabs were added
by two interactive startup modes. Program now leaves the authored collection
intact, retires geometry-tab creation, and makes `--office2019-light` (plus the
legacy `--office2019-theme-demo` alias) a tab-free 2019 selection. Existing
explicit `--verify-*` fixtures and their review-model builders remain available;
no library features, regression coverage, authored Forms/UserControls/resources
or user settings were deleted.

Normal Debug sample build passes with zero warnings/errors. Three scoped launch
checks pass under `artifacts/review-tab-removal/final/`: normal startup, combined
legacy switches and the new 2019 switch. Each retains the current tab/group/
command/QAT/page/control references/order, File mode, layout and density and
contains exactly Home/Edit/Gallery. The first attempt called an older broad
round-trip check whose Command identity expectation no longer matches the
current authored sample; its failure is retained under the initial default
artifact folder. The scoped check compares the actual current graph and does
not change authored commands to satisfy that older fixture. No full library,
geometry or caption matrix was rerun for this consumer-only launch change.
