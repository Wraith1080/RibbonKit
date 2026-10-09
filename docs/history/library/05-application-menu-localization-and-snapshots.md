# Library history: Application menu, localization and snapshots

> Archived implementation evidence. Dates, test counts and pending work describe
> their original checkpoints; later records may supersede them.

[History directory](../01-library.md) · [Current status](../../../04-DESIGN-NOTES.md#5-current-state--next-steps) · [Working agreement](../../../AGENTS.md).

### 3.46 The real Office 2007 application menu — a control that had to sit BEHIND the orb — 2026-07-28

RibbonApplicationMenu and its item/pane/button/separator controls implement the
two-pane File surface. When ApplicationMenu and Backstage are both assigned, the
menu wins; IsBackstageOpen remains the common open state and IsApplicationMenuOpen
distinguishes the surface. The application menu keeps title-bar QAT visible.

The orb-over-menu composition cannot use a Popup HWND or a Backstage-style adorner
above all window content. The shared window uses an unclipped, zero-size Canvas
outside the ribbon rows. Its menu presenter sits below a separate host for the
real orb. An inert placeholder preserves the orb's original layout slot while
its actual button temporarily moves, retaining bindings, automation and focus.
Closing/changing shape restores the original panel/index. Do not replace the
button's DynamicResource Margin with a resolved literal during that move.
Custom templates without the outer hosts retain the nested compatibility fallback.

Placement follows measured application-button bounds: the orb overlaps its menu,
2010/2013/2019 meet the rectangular button edge, and 2024 has a themed gap.
Promoting the entire tab-control branch would wrongly paint labels over the menu.
Frame paint belongs inside the motion wrapper so visible chrome travels with
its content while the handed-in surface fades.

Pane and row behavior:

- Only entry into an actionable main-nav row changes pane ownership. Transit
  across separators/gaps is neutral; row leave must not restore the default pane.
  Reopen starts on the default page. ApplicationMenuHoverTests reproduces slow
  gap crossing by draining deferred work after row leave.
- Split rows use the physical PART_Primary/PART_Arrow hover areas. Control-level
  IsMouseOver includes logical pane children and falsely lights the command half.
  Non-split dropdown rows are one visual opener even with two hit-test parts.
- Default content and active-row content use separate presenters. Presenting one
  UIElement through both gives it two visual parents.
- Resources is a CLR property, not a DP suitable for a Style Setter. Dedicated
  menu separator types avoid leaking an implicit Separator style into the host.
- Outside-click dismissal exempts the application button, or mouse-down closes
  and its mouse-up toggle immediately reopens. Pane-opening rows handle their
  click; command clicks close through the normal routed-event path.

The token profiles allow every theme to render the same menu geometry deliberately.
Later KeyTip and keyboard traversal work is recorded in the keyboard-access entries;
Crystal menu material and responsive pane sizing are covered by [§3.200](09-crystal-menus-scrollbars-and-dpi.md#3200-shared-crystal-application-menu--2026-09-30).

### 3.47 Nested popup Escape is a stack, not five independent window handlers — 2026-08-01

Repro: open the QAT overflow, open a drop-down entry inside it, then press Esc. The overflow closed
instead of the nested menu; the nested `StaysOpen=True` popup could remain visible, and from then on
Esc stopped dismissing every RibbonKit popup until application restart. A split entry happened to
tear down cleanly after the overflow closed, which made the two controls appear to have different
keyboard logic even though they inherit the same dropdown implementation.

The actual difference was timing. Every `PopupDismissHelper` subscribed independently to the same
owner window's `PreviewKeyDown`. The overflow opened first, so its older handler ran first, closed
the host and marked Esc handled before the newer nested-menu handler could see it. Unloading the
proxy could then strand the nested helper's window subscription if WPF coerced the child popup shut
without raising its normal `Closed` path. That stale handler swallowed every later Esc.

`PopupDismissHelper` now keeps a weak per-window stack in open order. An older helper leaves Esc
unhandled unless it is the stack top, allowing the newest visible flyout to close first. The result
is ordinary nested-menu semantics: first Esc closes the entry menu, second Esc closes overflow.
`OnClosed` removes the helper from the stack, and owner `Unloaded` performs a close plus unconditional
unregistration in `finally`, sealing the coerced-close path even when `Popup.Closed` never arrives.
Mouse light-dismiss and window deactivate/move/resize retain their close-all behaviour.

`PopupDismissHelperTests` covers inside-out ordering and the unloaded-owner/no-Closed cleanup path.

### 3.48 Visual-regression snapshots: the complete theme × DPI matrix — 2026-08-01

Phase 6's snapshot matrix is implemented in `tests/RibbonKit.VisualTests`: one fixed 760×170 DIP
ribbon scene rendered off-screen under all five themes at 100/125/150/200% — 20 committed lossless
PNGs. It exercises the real application-scoped token dictionary, the shared control templates,
selected-tab/group layout, three button sizes and the tab-row QAT.

Determinism is part of the test rather than an assumption. The harness fixes invariant culture,
English language metadata, display-mode/grayscale/fixed-hint text, layout rounding, software
rendering and `RibbonAnimationLevel.None`. It renders two fresh copies inside the same STA process
and requires those raw pixels to be identical before consulting the approved image. The approved
comparison ignores only tiny antialiasing noise: at most 0.1% of pixels may differ by more than
eight channel levels, and the mean channel difference must remain at or below 0.05.

⚠ **`RenderTargetBitmap` DPI metadata does not set a disconnected WPF visual's layout DPI.**
The first Office 2024 baseline changed after the host monitor moved from a higher scale to 100%,
despite the bitmap constructor still saying 96. WPF had already assigned system DPI to layout/text
before the final render target sampled it. The working solution is public WPF API, with no hidden
window: call `VisualTreeHelper.SetRootDpi(scene, new DpiScale(scale, scale))` **before measure/layout**,
verify it with `GetDpi`, and scale both the target's pixel dimensions and its DPI metadata by the
same factor. This makes all four rows independent of the physical monitor. Explicitly assigning
root DPI changed only text-antialiasing pixels in the old 100% approvals; those five were reviewed
and reapproved under the deterministic pipeline.

Approved PNGs live under `Snapshots/approved`. Updating them is an explicit opt-in via
`RIBBONKIT_UPDATE_SNAPSHOTS=1`; a normal mismatch writes the actual and a magnified diff beneath
the already-ignored `TestResults/visual` directory. The project is in `RibbonKit.sln`, so the
existing Windows `dotnet test` CI step runs it without a separate workflow or runner policy.

All 20 images were inspected at native resolution and the complete matrix passed in three successive
fresh test processes plus the normal project run. The existing Windows CI step will provide the
remaining cross-machine portability check; no workflow change was needed.

### 3.49 Dark/Black variants for every generation — 2026-08-01/02

Dark mode is a **palette variant**, not a sixth Office generation. `ThemeManager.SetDarkMode(app,
bool)` merges `Tokens.Office20XX.Dark.xaml` after the active generation's base dictionary, so
geometry and the single shared template set stay untouched. The preference survives theme switches,
and `SupportsDarkMode` is true for all five generations.

The palettes remain generation-specific rather than converging on one modern near-black look.
Office 2007/2010 reproduce their historical **Black** schemes: dark title/tab chrome surrounding a
silver-gray ribbon with dark command text, while the base dictionaries retain their hard-crease
2007 and smooth 2010 amber interaction gels. Office 2013 uses a flat **Dark Gray** palette derived
from the 2019 dark family but keeps 2013's gray outlines, connected selected tab and square geometry.
Office 2019/2024 retain their fully dark palettes. The visual harness therefore cannot equate
`IsDarkMode` with light command glyphs: 2007/2010 Black deliberately use dark glyphs on light wells.

The few remaining light-only local resources were promoted into the token contract: window
background, Modern-backstage rail/interaction brushes, and the options-dialog rail. Every light
theme supplies the same new keys, preserving its pixels, while the dark overlays replace them.
This was necessary for a live switch: `StaticResource` values captured inside the shared template
dictionary cannot be recolored by a later application-scope overlay; the promoted brushes now use
`DynamicResource` like the rest of the control surface.

Custom accents still layer after the dark overlay. Checked-state derivation mixes a custom accent
toward black in dark mode rather than toward white, avoiding pale toggled cards. Office 2024's
transparent Mica title bar uses low-alpha white hover/press washes in dark mode (the light variant
keeps its black washes). `MicaHelper.TrySetDarkMode` also sets
`DWMWA_USE_IMMERSIVE_DARK_MODE`, so the DWM backdrop and native frame choose the matching material.
The showcase exposes the variant under View → Accent and keeps the document page deliberately
white while switching the surrounding window, status bar, ribbon, backstage, menus, MDI chrome,
ScreenTips, KeyTips and options dialog.

The existing PNG format, checked-in `Snapshots/approved` storage policy, opt-in update variable,
and Windows CI strategy remain unchanged. The original matrix added 2019-dark and 2024-dark at all
four synthetic DPIs, growing from 20 to 28 approvals. The 2026-08-02 extension adds 2007-dark,
2010-dark and 2013-dark at the same four DPIs, producing a **40-image** ten-palette matrix. Existing
approvals stayed byte-identical; all five dark/black 100% images were inspected at native resolution,
and the full matrix is deterministic in-process.

### 3.50 Dark live-switch corrections from the 100% showcase pass — 2026-08-01

The first real-window pass found five surfaces the minimal ribbon snapshot did not exercise.
Office 2024 dark now gives `Ribbon.Background` an opaque `#181818` resting value; the Mica branch
temporarily overrides that token to Transparent and removes the override on teardown. This keeps
the non-Mica tab band dark without sacrificing the backdrop material.

The showcase must restore its window and content backgrounds with `SetResourceReference`, not by
assigning a previously resolved Brush. A direct Brush assignment replaces the original dynamic
resource expression; the sequence Mica on → dark on → Mica off → dark off therefore left a stale
dark local value until the operations were reversed. Reattaching the `Window.Background` token on
teardown makes every operation order converge on the current palette.

Nested standard WPF controls can also interrupt foreground inheritance because Button, ToggleButton,
TextBox, ListBox and TreeView bring their own default Foreground. The drop-down/split hit controls now
bind their Foreground to the outer Ribbon control; the editable combo TextBox uses TemplateBinding;
customization lists/trees forward their page foreground; and application-menu hit buttons bind to
their owning nav item. The popup surfaces were already correct. Three XML contract tests guard these
otherwise easy-to-miss nested paths, and the visual test now explicitly exercises the dark 2024
Ribbon-background → Mica-transparent → dark-background round trip.

Follow-up from the same pass: setting Foreground on the TreeView itself is insufficient because
generated TreeViewItem containers carry the platform's black default. The item-container style now
binds back to its ancestor TreeView. Galleries deliberately do **not** force-recolor arbitrary item
content: `RibbonGalleryItem.Foreground` already supplies the primary theme token for normal WPF
inheritance, while a local Foreground remains the natural opt-out for semantic colors. The showcase's
neutral style labels now use `Text.Secondary`; its intentional blue heading previews stay blue.

**2007/2010 control-surface regression corrected (2026-08-02).** Dark-mode preparation had replaced
the combo input and in-ribbon gallery's hardcoded white fill with `Ribbon.ContentBackground`. That
token is intentionally the large ribbon-body gradient in Office 2007/2010, so both small wells
acquired an inappropriate button-like gradient. They now consume the dedicated solid
`RibbonKit.Brushes.Control.SurfaceBackground` token. Every light theme supplies white; 2007/2010
Black supply light solid wells, while the 2013/2019/2024 dark overlays supply dark wells. Popup surfaces continue using
`Ribbon.ContentBackground` as before. Contract tests require both template bindings and require the
new resource to remain a `SolidColorBrush` in every theme variant.

### 3.51 First deterministic RTL snapshot slice — 2026-08-01

The visual suite now renders one additional Office 2024 light scene at 100% with
`FlowDirection.RightToLeft`, bringing the approved total to **41 images** after the later all-theme
dark/black expansion. It deliberately keeps
invariant culture and `en-US` language metadata: this isolates WPF mirroring from translation, font
fallback and shaping. The approved result is a clean geometric mirror of the LTR scene—QAT, tabs,
groups, separators and directional glyphs move together while the English labels remain readable.

This is an RTL smoke slice, not completion of the roadmap item. Full RTL verification still needs
the showcase's popup/window surfaces and representative bidirectional text. The first popup and
localization-resource slice has since landed in §3.52.

### 3.52 Localization foundation + RTL ribbon context menus — 2026-08-02

The first localization slice is deliberately library-owned text only. Application-authored tab,
group, command and document strings remain the host application's responsibility. Eight strings
created internally by `Ribbon` for its command and Quick Access Toolbar context menus now live in
`Resources/Strings.resx`, keyed by the public `RibbonString` enum. `RibbonLocalization.GetString`
resolves the embedded resource for `CurrentUICulture`; an optional `IRibbonLocalizationProvider`
can override any subset and return `null` for resource fallback. The cached QAT menu refreshes its
headers every time it opens, so a provider change does not require recreating the ribbon.

The same surface exposed a real RTL boundary: WPF hosts `ContextMenu` in a separate popup visual
tree, so it cannot inherit `FlowDirection` from the ribbon or QAT host. RibbonKit now copies the
placement target's flow direction before opening both menu kinds. The custom submenu template also
switches its physical `Popup.Placement` from Right to Left under RTL; relying on visual mirroring
alone would flip the arrow but still open the child flyout on the wrong screen edge.

Six logic tests pin embedded-resource parity, partial provider fallback, live cached-menu refresh,
the disconnected-popup flow copy, left-opening RTL submenus, and removal of the original hardcoded
English headers. This is an end-to-end foundation, not localization completion: the larger
Customize/Options template strings landed in §3.53, chrome tooltips in §3.54, and the default File
label in §3.55; representative bidirectional content and the remaining popup/window pass are next.

### 3.53 Customize/Options localization + RTL action snapshot — 2026-08-02

The second localization slice moves the complete RibbonKit-owned Customize/Options surface onto the
same resource/provider model: QAT and ribbon page labels, Add/Remove/Move actions, Reset,
Import/Export tooltips and automation names, New Tab/New Group/Edit, edit-dialog labels and choices,
OK/Cancel/Close, custom-node suffixes, file-dialog titles/filter, and import/export error text. The
`RibbonString` resource set now contains 47 keys, including the conventional page titles hosts use
when wrapping `RibbonCustomizePage` and `RibbonQuickAccessPage` in `RibbonOptionsPage`. Persisted
application-authored headers remain untouched; only RibbonKit's fallback names for newly created
custom tabs/groups are localized.

`RibbonStringExtension` supplies live XAML bindings rather than freezing a resource value when a
theme dictionary loads. Replacing `RibbonLocalization.Provider` refreshes open templates, and
`RibbonLocalization.Refresh()` lets a host update them after changing `CurrentUICulture`. Enum
choices in the short-lived edit dialog are rebuilt from the current provider whenever its template
is applied.

RTL exposed a bidi-specific trap in the customization action buttons: a translated word and `«/»`
inside one inherited RTL text run can be reordered back into the wrong visual direction. The words
are now localized separately from fixed directional glyphs; template triggers physically swap the
glyph columns. The approved `office2024-rtl-qat-customize-100.png` scene proves the real page mirrors
available/current lists and points Add toward the current list and Remove back toward available.
Eight new logic tests cover live XAML refresh, localized custom-node formatting, removal of embedded
dialog strings, RTL glyph contracts, live LTR/RTL template realization, and the lab's localized page
titles/small direct-QAT contract, plus the main/dialog physical-frame isolation contracts.
The current baseline is 158 logic tests and 46 approved images.
Remaining Phase 6 localization/RTL work is the broader popup/window/backstage pass and representative
bidirectional content.

The Showcase now exposes **View → Application → Localization / RTL**, a dedicated lab window rather
than temporary edits to `MainWindow`. Its local RTL toggle mirrors only the lab and the dialogs it
opens; its pseudo-localization toggle installs a key-revealing application-wide provider and restores
the previous provider when disabled or closed. The lab includes QAT items, split/dropdown popups,
right-click menus, both built-in customization pages, custom-tab/group editing, a live status card,
and English/Arabic/mixed-direction samples. The launcher is single-instance so two provider scopes
cannot be stacked accidentally. Its directly declared QAT buttons explicitly use `Size="Small"`;
unlike ribbon-command proxies, direct QAT items retain their own size and otherwise default Large.

The lab also exposed an RTL maximize defect at the boundary between custom chrome and logical
content. A real maximized-window measurement showed the Win32 overhang was already zero; the bug
was not a left/right conversion. With `FlowDirection=RightToLeft` on the top-level `Window`, WPF
arranged a margin on its direct template root against the bottom/left edges: top and right
compensation disappeared, while those values accumulated on the opposite sides. This is fatal for
the measured maximize inset and for the glass/WindowChrome edge even though ordinary child layout
mirrors correctly. Both window templates now keep a margin-free outer physical frame and
`PART_WindowRoot` explicitly LTR, then reapply the templated parent's `FlowDirection` on a nested
logical host. Caption buttons, title content, ribbon/dialog UI and popups still mirror, while Mica,
DWM borders and maximize compensation retain physical top/right/left/bottom geometry. Two template
contract tests pin the boundary for `RibbonWindow` and the maximizable Options dialog.

### 3.54 Chrome tooltip localization — 2026-08-02

The third localization slice moves the remaining simple RibbonKit-owned chrome tooltips into the
same live provider: RibbonWindow minimize/maximize/restore/close, Backstage Back, QAT overflow,
ribbon minimize, modal close, merged-window minimize/restore/close, and group launcher More Options.
The QAT overflow KeyTip description now resolves the same `MoreQuickAccessCommands` key instead of
duplicating English in C#. This grows `RibbonString` from 47 to 57 keys. Application-authored command
ScreenTips remain outside the library localization boundary.

The dedicated lab checklist now calls out hovering these chrome surfaces while pseudo-localization
is active. One deterministic source/template contract test proves every tooltip in the five affected
shared dictionaries is markup-backed, pins all expected keys, and rejects the old C# QAT-overflow
literal. Together with the two RTL physical-frame contracts above, the current baseline is 159 logic
tests and 46 approved images. The default application-button `File` label was deliberately separate
because it is dependency-property metadata rather than simple template text; §3.55 supplies the live
fallback without overwriting an application header.

### 3.55 Live localized default File label — 2026-08-02

`RibbonString.File` grows the resource set to 58 keys and now supplies the application button's
default display text, tooltip and automation name. The public `ApplicationButtonHeader` dependency
property retains its original `"File"` metadata for compatibility, but a new read-only
`EffectiveApplicationButtonHeader` separates the template-facing value from the app-owned value
source. When the property is still at metadata default (or resolves `null`), the effective value is
localized; a local value, style setter or binding wins unchanged. Clearing that override immediately
restores the current localized default.

The ribbon listens weakly to the existing localization binding source, so replacing the provider or
calling `RibbonLocalization.Refresh()` updates even an already-created ribbon without keeping it
alive. Delivery is marshalled to each ribbon's own Dispatcher and skipped once that Dispatcher is
shutting down; otherwise a provider change on one UI thread can touch a still-collectable ribbon
from another thread. It never writes into `ApplicationButtonHeader`, which is the crucial
binding-preservation invariant. The lab's pseudo-localization checklist now calls out File. Two
logic tests cover provider changes, explicit values, a live binding, clearing back to fallback, and
the template's content/tooltip/automation binding contract. Current baseline: 161 logic tests and 46
approved images.

### 3.56 Localized conventional application-menu footer actions — 2026-08-02

`RibbonApplicationMenu.FooterContent` intentionally remains arbitrary application-authored content,
so RibbonKit does not translate everything placed there. The Showcase's two conventional shell
actions are the useful exception: its branded literals are now generic `RibbonString.Options` and
`RibbonString.Exit` bindings. They therefore refresh live with the same provider as the default File
button and appear bracketed in the main window while the Localization/RTL lab's provider is active,
while host-specific footer labels remain the host application's responsibility. The resource set is
now 60 keys. One source contract pins the
two bindings and removal of the old embedded labels. Current baseline: 162 logic tests and 46
approved images.

### 3.57 Application-button width now reflows the selection visuals — 2026-08-02

Pseudo localization exposed one more §3.36 tab-row sibling: changing the default File label to
`[File]` widened `PART_ApplicationButton` and moved every tab, but the `RibbonTabControl` itself kept
the same size, so neither its sliding marker nor the 2010/2013 body-border notch recomputed their
selected-tab transform. `RibbonTabControl` now detaches/re-attaches a `SizeChanged` handler whenever
its template is applied and coalesces a `Loaded`-priority `RefreshSelectionVisuals()` after the
button's layout pass. Tracking measured size rather than the localization callback also covers
application-owned headers, fonts, templates and any other real geometry change. One source contract
pins the lifecycle-safe subscription and deferred refresh. Current baseline: 163 logic tests and 46
approved images.

### 3.58 Representative bidirectional content in RTL Backstage — 2026-08-03

The smallest deterministic remaining localization/RTL slice uses the already-settled visual-test
format and storage policy: one focused Office 2024 snapshot renders the real Modern `Backstage`
under inherited RTL with `ar-SA` language metadata, Arabic and Latin navigation headers, mixed
Arabic/Latin content, Arabic-Indic digits, and an explicitly LTR document identifier. The harness
still renders the scene twice before comparison, so the approved pixels are accepted only after the
same in-process determinism gate as the existing matrix.

The Localization/RTL lab now exposes the matching Backstage through its File button and calls out
navigation mirroring, Arabic shaping, mixed-run ordering, and the LTR document name in its manual
checklist. A logic contract pins the three representative nav headers, keeps the mixed content
inheriting the lab's direction, and preserves the document identifier's explicit physical LTR
alignment. This completes the representative bidirectional-content pass without pretending that
`RenderTargetBitmap` covers separate-HWND `Popup` placement; the broader live popup/window pass is
still owed. Current baseline: 164 logic tests and 47 approved images.

### 3.59 Live RTL Backstage rail, slide and title transition — 2026-08-08

A real Localization/RTL lab recording exposed two live-window defects that the disconnected §3.58
snapshot could not see. First, `Ribbon.Backstage` is reparented into a `BackstageAdorner`; that
adorner branch crossed the deliberately physical-LTR window frame without carrying the ribbon's
logical flow, so the otherwise-correct Backstage template rendered its rail on the left. The
adorner now binds its own flow to the owning ribbon and, only when the Backstage has no application-
owned local/style value, binds the child to that live flow as well. Explicit host direction still
wins. Backstage open/reopen/close motion now uses the logical leading edge: left in LTR, right in
RTL.

Second, hiding/showing title-bar QAT content shifts the title's resting center. The existing FLIP
transition measured that shift in physical window coordinates but applied the same number as the
title's local `TranslateX`. Across the physical-LTR/logical-RTL boundary those axes have opposite
signs, so the title overshot beyond its destination before settling. `RibbonWindow` now derives the
realized local-X-to-window-X scale from `TransformToAncestor`, uses it both when removing a current
transform from a measurement and when converting the new FLIP delta, and therefore also remains
correct for a custom template that introduces scaling.

Three realized-layout contracts cover the inverted title axis, a live attached Backstage adorner
with application-owned-flow preservation, and logical-leading-edge selection. The lab checklist
pins the visible rail/slide/title behavior. Current baseline: 167 logic tests and 47 approved images;
the broader live RTL popup/window pass remains.

### 3.60 Localization/RTL lab follows the Showcase File-surface policy — 2026-08-08

The live lab originally hardcoded a Modern Backstage and had no `RibbonApplicationMenu`. Theme
resources still flowed application-wide, but the Showcase's application-owned choices did not:
Office 2007 did not turn the lab's File tab into an orb, the `2007 Menu` toggle could not replace its
Backstage, and the three Backstage design choices affected only the main ribbon. That made the lab
incapable of verifying the surfaces it was meant to exercise.

`MainWindow` now publishes one small Showcase-only application-surface snapshot and change event:
application-button shape, Backstage versus application menu, Backstage design, and translucency.
The modeless lab subscribes while open, detaches on close, and applies every change live. It owns a
separate two-pane application-menu instance because WPF elements cannot have two visual parents;
the menu carries Arabic/Latin navigation, recent-document and pane content plus an explicitly LTR
filename. Switching File-surface kind closes the old surface before reparenting, while design and
translucency can update an already-open Backstage without forcing it closed.

One deterministic source/XAML contract pins the live subscription, all four synchronized choices,
cleanup, representative bidirectional menu content, and the LTR filename boundary. Current baseline:
168 logic tests and 47 approved images. No snapshot format, storage, or CI-policy change was needed;
the broader live RTL popup/window pass remains.

### 3.61 Live RTL popup/window verification closes Phase 6 — 2026-08-08

The user completed the remaining separate-HWND/manual pass in the Localization/RTL lab. Under RTL,
the primary split-button menu opens and aligns correctly; a nested submenu inside the QAT overflow
opens toward the available logical side; the QAT/context menu mirrors its item layout and directional
chevron; and both normal and maximized windows fit the left and right screen edges without losing the
physical frame or caption controls. The edge result reconfirms the physical-frame/logical-content
isolation introduced in §3.53 rather than introducing a second window fix.

The same pass verified the two-pane application menu and application button under Office 2024 and
Office 2007: the modern File button/menu pair mirrors correctly, while the 2007 orb remains above its
menu frame and both surfaces preserve the representative Arabic/Latin and explicitly LTR filename
content from §3.60. The §3.59 Backstage rail, slide, and title transition also remain clean.

These were live visual checks because WPF `Popup` content owns a separate HWND and is outside the
settled `RenderTargetBitmap` approval harness. The supplied captures are verification evidence, not
a new snapshot format or storage policy. With the deterministic 40-image theme/DPI matrix, seven
focused approvals, 168 logic tests, and this live pass all green, **Roadmap Phase 6 is complete**.

### 3.62 Flat-theme application-menu footer buttons retain their outline — 2026-08-08

A live hover recording exposed a post-close visual regression in `RibbonApplicationMenuButton`.
The footer buttons have a deliberate one-DIP application-menu outline at rest, but their hover and
pressed triggers replaced that brush with the generic control-state border. Office 2013, 2019, and
2024 intentionally make the generic border transparent, so the outline disappeared on hover and the
visible face looked two DIPs smaller even though WPF layout never changed.

The shared template now keeps a non-interactive `PersistentOutline` beneath the transient `Chrome`.
Flat themes therefore retain the same measured footprint while their existing fill changes; the
opaque 2007/2010 hover and pressed borders still paint over that base outline, preserving their
classic highlighted states. No new tokens or theme-specific template branches were needed. One
template contract pins the permanent application-menu brush/thickness/corner geometry, prevents
state triggers from mutating it, and retains the existing theme-specific Chrome state brushes.
Current baseline: 171 logic tests and 47 approved images; manual hover recheck is pending.

### 3.63 Failure-only CI artifacts for cross-machine snapshot diagnosis — 2026-08-08

The first GitHub-hosted cross-machine run stopped on `office2007-default-100`, the first scene in the
matrix, with 1,510 significant pixels and a 0.4030 mean channel difference. The triggering commit was
documentation-only, so this is not evidence for reapproving that scene; because the test stops at the
first mismatch, it also does not establish that Office 2007 alone differs. The harness wrote useful
actual/diff PNGs under `TestResults/visual`, but the workflow uploaded only successful NuGet output,
so both diagnostics disappeared with the runner.

With explicit user approval, CI now uploads `TestResults/visual/*.png` as the failure-only
`visual-snapshot-diagnostics` artifact. Missing files are ignored, successful runs upload nothing,
and repository-default artifact retention applies. The committed PNG format/location, explicit
regeneration opt-in, comparison thresholds, and `windows-latest` runner remain unchanged. The next
run's actual/diff pair must be reviewed before choosing any portability correction.

### 3.63a Responsive Ribbon Editor + application-menu authoring — VERIFIED IN VS, 2026-08-08

The application-menu backlog is implemented in the existing net472/code-built designer architecture.
The editor shell now uses a compact contextual **Add** menu, a resizable star-sized tree/inspector
split, a stretching Caption row, and separate **Properties / Design Preview** tabs. Preview controls
are vertical rather than one long row. The window uses device-independent layout, scroll viewers,
layout rounding and a deferred remeasure on `DpiChanged`, so a live move between differently-scaled
monitors does not retain stale measurements.

Preview now models the File button as one mutually-exclusive choice: Closed, Backstage, or
Application menu. `SelectedTabPreviewProvider` still owns `Ribbon.SelectedIndex` and
now translates one integer `DesignPreviewFileSurface` value for the whole File-surface transition. An
earlier implementation tried to clear `Ribbon.ApplicationMenu` on the design surface. Literal null
crashed the VS 2022 isolated designer's
`DesignerModelProperty.Value` getter immediately; `DependencyProperty.UnsetValue` still poisoned the
model so a later read of the unrelated `Backstage` property crashed. Translating two Booleans separately
still exposed a runtime-precedence intermediate frame and invalidating the application menu's
object-valued pane state corrupted its child `ModelItem`s. The editor therefore never translates or
invalidates any object-valued preview property. `Ribbon` consumes the one primitive surface value and
updates its normal open/discriminator/design-host state synchronously. Application-menu pane preview
likewise translates only `DesignPreviewActiveIndex`; `RibbonApplicationMenu` derives its ordinary
`ActiveItem`, pane content/header, and item state inside the runtime control. Backstage pages keep their
existing integer `SelectedIndex` provider. The final primitive-only paths are verified in the VS 2022
isolated designer: Application menu, Closed, and Backstage can be cycled repeatedly without a transient
wrong surface, a crash, model corruption, or serialized preview state, and application-menu pane
selection remains usable afterward.

`Ribbon.ApplicationMenu` is now a deletable singleton editor root and **Add Application Menu** is also
a Ribbon surface verb. The contextual Add menu creates/reorders/deletes command items, separators,
default/command pane items, and footer buttons, reusing the existing caption, property-spec, icon,
KeyTip, drag/drop and one-editing-scope-per-undo paths. Standard panes are managed StackPanels.
Arbitrary existing `DefaultContent`, command `Content`, or `FooterContent` is surfaced as custom
content and left untouched rather than recursively exposing or rewriting its visual tree.

Build verification for this implementation is recorded below. The redesigned editor and its complete
File-surface/pane preview cycle are user-verified in Visual Studio; the live DPI-monitor move and the
full add/delete/reorder/undo matrix remain useful focused regression checks rather than blockers for
the application-menu parity item.

The **Design Preview** tab now also begins with a session-only **Theme** selector: project default,
Office 2024, Office 2019, Office 2013, Office 2010, or Office 2007. The net472 design assembly keeps a
small `ThemePreview` enum whose non-negative values deliberately mirror the runtime `RibbonTheme`
ordering without adding a runtime-project reference. `SelectedTabPreviewProvider` translates only the
primitive hidden `DesignPreviewTheme` integer. In design mode, `Ribbon` responds by replacing one
Ribbon-local token dictionary; project default removes it. This leaves `Application.Resources`, the
serialized XAML, and runtime instances untouched while allowing DynamicResources and theme metrics to
refresh on the designer surface. A deferred selection-visual refresh covers the geometry-driven tab
underline and classic connected-tab notch. The selector does not rewrite authored structural choices
such as `ApplicationButtonShape`, so choosing Office 2007 previews its palette and metrics without
silently converting a rectangular File button into an orb. Three focused tests pin runtime
inertness, local dictionary replacement/removal, representative generation metrics, and the editor /
provider wiring. Current automated baseline: 185 logic tests and 56 approved images; live Visual
Studio designer confirmation of the new selector remains the focused manual check.

### 3.64 Backstage sheet depth and the 2010 colored caption — 2026-08-08

Three reference-driven depth corrections stay inside the shared-template/token architecture. The
Backstage content sheet now consumes `Backstage.ContentShadow` for both `Modern` and `Classic2010`.
Office 2024 publishes a restrained 6px/1px left-cast shadow (0.12 light, 0.14 dark), while Office
2007 publishes the established stronger 2010 sheet recipe (9px/3px, 0.24). Other generations remain
zero, and the 2010 values are unchanged.

An attempted Office 2007 idle outline for Medium/Small commands was rejected in the live Showcase:
the persistent boxes made the ribbon visually busy and unlike the desired large-command treatment.
The `IdleOutline` template overlays and `Control.IdleBorder` palette token were removed completely
from normal, toggle, dropdown, and split families. Compact commands remain borderless at rest and
continue to use the existing hover/pressed glass borders only.

The Office 2010 accent caption now uses a generation-specific smooth glass rather than a plain ramp.
`CaptionGlass` moves through a broad upper reflection, the exact accent-coloured face, and a restrained
lower sheen. Its five offsets are strictly increasing—Office 2007 alone owns the hard crease—and no
stop is darker than the chosen accent, so the earlier heavy lower band cannot return. Three focused
contracts cover the Modern shadow hook, the deliberate absence of compact idle outlines, and the
2010 glass shape/luminance; the existing ten-palette shadow contract covers the new effect values.
Focused approvals cover the 2007 Classic2010 sheet, 2024 Modern sheet, and 2010 colored caption, with
the RTL Modern Backstage approval refreshed. Current automated baseline: 174 logic tests and 50
approved images.
