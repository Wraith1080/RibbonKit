# RibbonKit — Design Notes & Session Context

> Living document capturing architecture decisions, implemented features, and the
> hard-won pitfalls of this project. Written so that any future session (human or AI)
> can pick up exactly where we left off without re-discovering these lessons.
>
> Last updated: July 2026.

## 1. Project Overview

**RibbonKit** is an open-source (MIT) WPF custom control library recreating the Office
Fluent UI Ribbon on modern .NET.

Locked decisions:

- **Targets `net8.0-windows` and `net9.0-windows` only.** No .NET Framework support.
- Open source, packaged as a downloadable GitHub Release asset (`RibbonKit`, local `v1.0.0`
  candidate); NuGet.org publication is not planned.
- Planning docs live in `docs/` inside the repo.
- Sample app: `samples/RibbonKit.Showcase` (a Word-like demo window).

Repo layout:

```
src/RibbonKit/
  Animation/       RibbonAnimation.cs (config), RibbonMotion.cs (transitions)
  Automation/      UIA peers (aliased: WPF's legacy ribbon has same-named peers)
  Controls/        All lookless controls (Ribbon, tabs, buttons, galleries, backstage, KeyTips, QAT)
  Interop/         MicaHelper.cs (DWM system backdrop)
  Layout/          Adaptive sizing engine (RibbonGroupsPanel, ReductionAlgorithm, RibbonSizeDefinition)
  Themes/          Generic.xaml, Office2024.xaml (ALL templates), Tokens.Office{2024,2019,2013,2010}.xaml
  Theming/         ThemeManager.cs
samples/RibbonKit.Showcase/
```

## 2. Core Architecture

### 2.1 One template set, token-driven themes

- **All control templates live in `Themes/Office2024.xaml`**, shared by every theme.
  Templates never hardcode colors/metrics — they reference tokens via `DynamicResource`
  (`RibbonKit.Brushes.*`, `RibbonKit.Metrics.*`, `RibbonKit.Effects.*`).
  Since 2026-07-27 that file is a 35-line aggregator merging ten `Controls.*.xaml`
  parts — see §3.37, which is EXPERIMENTAL and may be reverted.
- **Per-theme values** live in `Tokens.Office2024.xaml`, `Tokens.Office2019.xaml`,
  `Tokens.Office2013.xaml`, `Tokens.Office2010.xaml`. Same keys, different values. A theme
  "chooses" a visual style by zeroing what it doesn't use (e.g. flat themes set underline
  brushes to `Transparent`, corner radii to `0`, `ContextualUnderlineHeight` to `0`).
- **A token value need not be a `SolidColorBrush`** — any `Brush` works, since templates bind
  each brush key via `DynamicResource`. Office 2010 exploits this: its background/hover/File-button
  tokens are `LinearGradientBrush`es (see §3.27). This is safe because nothing animates a token
  brush's `Color` — every RibbonKit transition targets `UIElement.Opacity` (the hover/press/check
  washes fade a layer's opacity; the theme-switch cross-fade dips the tab control's opacity), so a
  gradient brush drops in wherever a solid one did.
- The app merges one token dictionary into `Application.Resources` (App.xaml), and
  `ThemeManager.Apply` swaps it at runtime.

Theme identities:

- **Office2024** (default): light, rounded "floating card" ribbon body with shadow,
  accent underline tab selection.
- **Office2019** ("modern grey"): flat, grey band (`#E6E6E6`) behind tabs, white body,
  fill/pill tab selection. When the colored-title-bar toggle is ON the band + title bar
  turn accent-colored with white text.
- **Office2013** ("White"): fully flat/square, white strip, outlined active tab that
  cuts into the body, SOLID accent File button, tabs flush to the title bar.
- **Office2010** ("Blue"): the first NON-flat theme — gradient silver-blue window/ribbon
  chrome, dark-blue (`#15428B`) tab labels, the iconic amber/gold glossy highlight on
  hovered/pressed/toggled controls, a connected (outlined) light active tab, a solid blue
  gradient File button, and gently rounded (2-3px) corners. See §3.27.

### 2.2 ThemeManager (`Theming/ThemeManager.cs`)

- `Apply(app, theme)` swaps the merged token dictionary.
- **Runtime overrides layer on top by setting keys directly on `Application.Resources`**
  — own-dictionary entries beat merged dictionaries, and `DynamicResource` picks up the
  change live. Always *clear all override keys first*, then re-derive (prevents leaking
  a 2024 underline onto flat 2019, etc.).
- `SetAccent(app, color)`: derives a full accent family via `Mix()` (blend toward
  white/black). **Theme-aware**: only overrides tokens that theme actually uses.
- `SetAccentedTitleBar(app, bool)`: colors the title bar; in 2019 also the tab-strip
  band, hovers, and foregrounds. **Order matters**: it re-runs `ApplyAccentOverrides`
  first because the accent system owns `ApplicationButton.HoverBackground` for 2013.
- `Changed` static event fires after every Apply/SetAccent/ClearAccent/SetAccentedTitleBar
  — the Ribbon listens to re-evaluate QAT icon tinting.
- `IsAccentedTitleBar`, `CurrentTheme` are queryable statics.

### 2.3 Window chrome (`Controls/RibbonWindow.cs`)

- Custom chrome via `WindowChrome` (`CaptionHeight=34`, `UseAeroCaptionButtons=False`),
  themed caption buttons, a `TitleBarContent` slot (used by the title-bar QAT).
- Windows 11 Snap Layouts: the HWND hook returns `HTMAXBUTTON` from `WM_NCHITTEST`
  when the pointer is within the visible themed maximize/restore template part. Snap is
  a native non-client hit-test contract; a WPF hover or `SystemCommands.MaximizeWindow`
  invocation alone does not expose the flyout. Once advertised that way, Windows sends
  non-client mouse messages instead of WPF mouse events, so the hook also tracks leave,
  drives the existing caption hover/pressed brush tokens through `Button.Background`,
  and owns maximize/restore button-up. Do not forward the button down/up to the native
  caption handler: on a transparent Mica title bar it can paint over the themed control.
  The resource references deliberately share `ThemeManager`'s keys, so colored-title-bar
  accent/glass overrides and live theme/accent switches continue to apply while hovered.
  **User-verified on Windows 11 (2026-08-11):** the Snap Layout flyout, themed hover/press,
  maximize/restore clicks, Mica on/off, and colored-title-bar combinations all behave correctly.
- **Maximize overhang fix (important)**: a maximized WindowChrome window hangs past the
  monitor work area. `WM_GETMINMAXINFO` hooks did NOT fix it reliably. The working fix
  is *measured margin compensation*: on state/DPI changes, measure `GetWindowRect` vs
  `MonitorFromWindow`/`GetMonitorInfo.rcWork`, convert device px → DIP via
  `VisualTreeHelper.GetDpi`, and inset `PART_WindowRoot` by exactly that margin.
- The template intentionally had **no `GlassFrameThickness="-1"`** (it broke maximize
  before the measured fix); Mica later required extending glass — see §3.10.

## 3. Implemented Features (chronological, with pitfalls)

### 3.1 Core ribbon skeleton
Tabs/groups/buttons (`RibbonButton`, `RibbonToggleButton`, `RibbonSplitButton`,
`RibbonDropDownButton`, `RibbonComboBox`), adaptive sizing engine
(Large→Medium→Small→Collapsed by `ReductionPriority`, `ReductionMode`, `CanResize`;
`SizeDefinition="Large, Medium, Small"` strings), collapsed-group flyouts that re-home
the group's content grid into a popup, galleries with live preview
(`RibbonGallery`/`InRibbonGallery` share ONE items presenter re-homed between strip and
popup — re-homing is driven by the *property* change, never Popup.Closed, which is
unreliable for nested popups), backstage overlay, ScreenTips, QAT, minimize, UIA peers.

**Popup pattern used everywhere**: `StaysOpen=True` + custom `PopupDismissHelper` for
light-dismiss, so WPF popup mouse-capture never steals the opener toggle's clicks.

### 3.2 Minimized QAT card (2024)
When minimized with QAT below, the extender becomes a floating card: token trio
`QatExtenderMargin/BorderThickness/CornerRadius` + `*Minimized` variants; flat themes
point both at the same values so nothing changes shape.

### 3.3 KeyTips (Alt / F10)
`KeyTip.cs` (attached props: `KeyTip.Keys`, auto-derivation from headers),
`KeyTipAdorner.cs` (badge visuals via theme tokens), `KeyTipService.cs` (state machine).

- Badges render in **per-target `AdornerLayer`s** — popups need their own
  `AdornerDecorator` inside the popup template (dropdown/split menu hosts + group
  flyout popup all have one).
- Levels: root (tabs + QAT + File) → tab level (groups' controls) → menu levels.
  Split buttons badge primary and chevron separately; collapsed groups badge the
  collapsed button then descend into the opened flyout; dialog launchers, gallery
  expanders, and backstage items are all badged.
- **Bug fixed**: pressing Alt while the backstage was open showed ribbon KeyTips —
  `Enter()` now builds a backstage-level when `_ribbon.IsBackstageOpen` (and doesn't
  close a mouse-opened backstage on exit).
- **Application-button lookup invariant**: the File button lives in the nested tab-control
  template and is found by `Ribbon.ApplicationButtonPartName` (`PART_ApplicationButton`). The
  application-menu addition renamed that part for light-dismiss but left KeyTips searching for
  `ApplicationButton`, which removed the badge from both the rectangular button and the orb. Both
  consumers now use the shared constant. When both application surfaces are assigned, the menu
  also wins in KeyTip activation just as it does in `Ribbon` (so File opens the menu rather than
  descending into the covered backstage).
- **The application menu is now a real KeyTip level.** Pressing Alt while it is mouse-open badges
  its nav commands, whichever pane is currently visible (including Recent Documents), and its
  footer buttons. Activating File/Orb by KeyTip opens the same terminal level. Plain nav commands
  anchor one badge to `PART_Primary`; true split rows add an auto-derived second badge on
  `PART_Arrow`, while merged drop-down rows keep one opener badge. Activating either kind of opener
  refreshes the same terminal level after the pane changes, exposing the newly visible pane items
  without executing the split command. Pane and footer commands are collected from the realized
  visual tree because both are arbitrary content properties.
- **QAT overflow is a KeyTip level, not a pile of hidden root items.** Overflowed QAT elements keep
  `Visibility=Visible` and are arranged into a zero-sized slot, so filtering root badges by
  `IsVisible` stacked their number badges at the strip origin. The active
  `RibbonQuickAccessToolBar` now exposes the panel's overflow membership: root badges include only
  strip items, then the `»` button takes the next number and opens a level built from the flyout's
  command proxies. The overflow popup carries its own `AdornerDecorator`, as every popup KeyTip
  scope must. The window also has an outer, window-wide decorator because the title-bar QAT sits
  outside the content-row adorner layer. A title-row-only layer did show its badges, but its adorner
  still painted below the later content row, so legacy themes' opaque tab strips covered the badge
  bottoms. The outer layer paints QAT KeyTips above both rows without lifting the title background
  over the Office 2007 orb; the content row retains its inner decorator for backstage overlays.
- Invocation goes through UIA patterns (Invoke/Toggle) so it works for every control.

### 3.4 Contextual tabs = custom coloring (no marker line)
`RibbonTab.ContextualColor` (Brush) + read-only `ContextualBrush` (falls back to theme
accent). The old *upper marker line was removed*. Template has TWO header presenters
(`HeaderText` / tinted `ContextualHeaderText`) and a tinted
`ContextualSelectionIndicator` underline.

- 2024: tinted text dimmed via `ContextualUnselectedOpacity=0.6` until selected;
  selected = full tint + tinted underline (normal accent underline hidden).
- 2019/2013: tinted text only (`ContextualUnderlineHeight=0`, opacity 1).
- Showcase: PictureFormatTab uses `ContextualColor="#C43E96"`, shown by the Insert
  Picture toggle.

### 3.5 Colored title bar + accent customization
Showcase View tab has a "Colored Title Bar" toggle and an accent gallery (swatches with
hex `Tag`, "Auto" resets). All accents derive from ONE color via `SetAccent`.

### 3.6 2019 modernization & hover consistency
2019 recolored grey/white (`Ribbon.Background=#E6E6E6`, white selected tab/body). The
band **tracks the colored-title-bar toggle** (accent when on, grey when off). File
button + minimize toggle hover on the colored strip use the
`TabStrip.ControlHoverBackground` token synced to `Tab.HoverBackground` — but do NOT
unconditionally clear `ApplicationButton.HoverBackground`; the 2013 accent system owns
it (only set in the 2019 branch, after re-running accent overrides).

### 3.7 Large-button label alignment
Multi-line labels made buttons uneven. Fix: all four large layouts
(button/toggle/dropdown/split) use `Margin="6,8,6,0"` (icons top-anchored ~10px down)
+ label `MinHeight="32"` (reserves 2 lines) + `GroupsRowHeight` 96→104. Do NOT
vertically center large content — icons must line up across the row.

### 3.8 QAT placement (TitleBar / TabRow / BelowRibbon) + context menu
`RibbonQuickAccessPosition.TitleBar` added. Right-click context menu (3 placements,
check on current) attached to all three hosts.

- **`QatTabRowHost` lives in the nested RibbonTabControl template** — NOT reachable via
  the Ribbon's `GetTemplateChild`. Find it with a visual-tree search in `OnLoaded`.
- **Single-parent reparenting rule**: exactly ONE host binds `ItemsSource` at a time.
  Leaving the title bar releases synchronously; the new claim is deferred via
  `Dispatcher.BeginInvoke(Background)` so the old host frees the items past a layout
  pass first (avoids transient double-parent exceptions).
- Title-bar host is code-created (`ItemsControl` + horizontal StackPanel), projected
  into `RibbonWindow.TitleBarContent`; previous content is saved/restored.

### 3.9 QAT white icons on colored surfaces
When the QAT sits on an accent surface (title bar with colored-title-bar ON, or 2019
tab row with colored band), icons turn white silhouettes and hover matches the band.

- **Inherited attached properties DID NOT propagate** to the QAT buttons across the
  ItemsControl/reparenting boundary — that approach failed. The working model is
  **direct-set**: `Ribbon.QatOnColoredSurface` (bool, direct-set + `Inherits` so nested
  template parts see it) plus **per-item resource overrides** for the brushes —
  `UpdateQatButtonContext()` writes the resolved band brushes into each item's
  `Resources` under `RibbonKit.Brushes.Qat.ColoredHoverBackground` /
  `...ColoredPressedBackground`, and templates consume them with `{DynamicResource}`.
  Re-run on `ThemeManager.Changed`, collection changes, and placement changes.
- White icon = `Rectangle Fill=#FFFFFF` with `OpacityMask=ImageBrush(Icon)`; the small
  layout has `SmallImage` + hidden `SmallImageTint`, swapped by template trigger.
- **Possible later API, not committed:** an optional per-command `MonochromeIcon` could provide a
  purpose-authored alpha mask for colored QAT surfaces. That would preserve holes and internal
  strokes that the automatic mask loses when a full-color icon contains several opaque regions.
  Keep the existing automatic mask as the compatibility fallback; do not add a separate dark-mode
  icon matrix unless real application icons demonstrate that theme-aware resources are insufficient.

### 3.10 Animation system (global + per-action)
**Configuration model chosen by user: global level + per-action overrides, default
Subtle.**

- `Animation/RibbonAnimation.cs`: `GlobalLevel` (`None/Subtle/Expressive`),
  `SetActionLevel/ClearActionLevel` per `RibbonAnimationAction` (12 actions:
  RibbonMinimize, Backstage, TabMarker, TabSwitch, Gallery, DropdownMenu, Hover,
  QuickAccessMove, ContextualTab, KeyTip, ToggleState, ThemeSwitch).
  `RespectSystemReduceMotion` (default true) → effective level None when
  `SystemParameters.ClientAreaAnimation` is off. Per-action durations (Subtle ~90–220ms;
  Expressive ×1.4) and slide offsets (Expressive ×1.8); easing CubicOut, Expressive gets
  BackEase on marker/QAT/KeyTip/Toggle. `Initialize(app)` publishes
  `RibbonKit.Animation.Duration.*` Duration tokens for template storyboards.
- `Animation/RibbonMotion.cs`: `PlayOpen` (fade+slide from an edge), `PlayClose`
  (fade+slide out, with completion callback), `PlaySlideIn` (slide WITHOUT opacity),
  `PlayFadeIn`, `AnimateTranslateY` (translate-only glide), `FadeWash` (cross-fades a
  hover/press/checked highlight layer's opacity — used by buttons/toggles since a
  templated storyboard can't animate a `DynamicResource` duration), `PlayThemeCrossfade`
  (85%→100% opacity dip on theme/accent change — not a full fade, which would flash an
  already-opaque element to transparent), `PlayKeyTipPop` (KeyTip badge fade+short
  downward settle; releases its own opacity animation on completion — see hard rule 8),
  `Rest`.

**Hard rules learned:**

1. **Never animate layout properties** (Width/Height/Margin) — transforms + opacity only.
2. **Never fade an element that's already rendered opaque** — resetting it to 0 first
   reads as a flicker. This killed the QAT-move cross-fade (removed) and changed tab
   switch to slide-only (`PlaySlideIn`).
3. **Never transform a Popup's direct child** — the transparent popup positions itself
   from that child's bounds, so a start offset bakes into the popup's resting position
   (the gallery "dropped a few pixels"). Animate the child's *inner content*.
4. Minimize/restore: the body's Visibility is **code-managed** (template trigger
   removed) — slide up + fade out, collapse row in the Completed callback; restore
   shows the row then slides down. Row height itself is never animated.
5. The below-ribbon QAT bar **glides with the body** on minimize/restore via
   `AnimateTranslateY(±bodyHeight)` (body height captured while visible), staying
   visible; transform resets in the same step as the collapse so it looks stationary.
6. Backstage: slide-in from the logical leading edge (LEFT in LTR, RIGHT in RTL) on open;
   slide-out through the same edge on close with the adorner
   removed in the Completed callback (`_backstageClosing` guard; re-open mid-close
   reuses the existing adorner — a UIElement can't have two).
7. Tab switch: slide from **Top** (content drops down away from the tab strip — user
   preference).
8. **An opacity animation's default `FillBehavior.HoldEnd` swallows later plain
   property sets.** `KeyTipAdorner.Dimmed` sets `Opacity` directly to dim/undim a badge
   as the user types; if the pop-in animation were left holding the property, those
   sets would silently do nothing. `PlayKeyTipPop` clears its own animation
   (`BeginAnimation(OpacityProperty, null)`) and sets a plain `Opacity = 1d` in its
   `Completed` handler so the property is a normal local value again afterward.

**All planned transitions are now wired:** dropdown/split/flyout menus, gallery expand,
backstage open/close, ribbon minimize/restore (+QAT glide), tab-switch slide, hover/press
cross-fade (`RibbonButton`/`RibbonToggleButton` via `FadeWash`), the sliding tab marker
(`RibbonTabControl` — a real underline glide between tabs, not just content slide),
contextual-tab appear (`RibbonTab.cs`, `PlayOpen` with `ContextualTab`), toggle-state
cross-fade (`RibbonToggleButton`'s check wash), theme-switch cross-fade (`Ribbon.cs` calls
`PlayThemeCrossfade` on the tab control), and KeyTip badge pop-in (`KeyTipService.AddAdorners`
calls `PlayKeyTipPop` once per badge, the same run it first shows it — see hard rule 8).
Showcase: View → Motion group (None/Subtle/Expressive + Respect System toggle);
`App.xaml.cs` calls `RibbonAnimation.Initialize(this)`.

### 3.11 Backstage redesign (Modern 2024) + icons
`RibbonBackstageDesign` enum (`Classic`/`Modern`); `Backstage.Design` is an **inherited
attached property** so nav items restyle from one setting (this inheritance works
because backstage items are direct logical children — unlike the QAT case in §3.9).

- Modern: light rail `#F5F4F3` (width 200 vs classic 220), dark text, rounded inset
  item highlights, selected = light fill + 3px accent left bar + accent text.
- Classic (default): the original accent column, untouched — backward compatible.
- The back button tints via `TemplateBinding Foreground` with a foreground-tinted
  hover disc (works on both designs).
- `BackstageTabItem.Icon` (ImageSource): rendered as a **foreground-tinted silhouette**
  (Rectangle + OpacityMask) in an always-reserved 16px column → icon-less items stay
  aligned. Selected item's icon goes accent automatically.
- **Modern.* brushes are app-scope `DynamicResource` tokens** (promoted from template-local
  statics in §3.49), so the rail follows the active light/dark palette. Do not move them back
  into the shared template dictionary or a live variant switch cannot replace them.
- Trigger order matters: Modern trigger first, then Translucent triggers (later wins).
- Showcase: `Design="Modern"` default, Home/Info/New/Open items (Info deliberately has
  no icon to demo alignment), View → Backstage group toggle.

### 3.12 Mica (Windows 11 system backdrop) — EXPERIMENTAL
`Interop/MicaHelper.cs`: `TrySetBackdrop(window, RibbonBackdrop)` sets
`DWMWA_SYSTEMBACKDROP_TYPE` (38); `None/Mica/Acrylic/Tabbed`; requires build ≥22621
(`IsSupported`), returns false otherwise (toggle self-reverts).

- **Black-background pitfall (real bug we hit)**: the backdrop only composites where
  the DWM glass frame reaches. Our chrome has no glass → transparent window rendered
  BLACK. Fix: `ExtendGlassFrame(window, full)` swaps in a WindowChrome clone with
  `GlassFrameThickness = -1` (or 0 to restore), preserving caption/resize settings.
  Uses WindowChrome (not raw `DwmExtendFrameIntoClientArea`) so it survives WPF's
  chrome re-application.
- `Backstage.Translucent` (bool): transparent root + semi-transparent content
  (`#E6FFFFFF`) and modern nav (`#CCF5F4F3`) so Mica shows through the backstage.
  **No longer used by the showcase** — see the backstage-opaque decision below.
- Showcase Mica toggle: backdrop + glass + transparent Window/MainContentArea
  backgrounds; all restored on un-toggle.

**Glass-frame-on-un-toggle pitfall (real bug we hit):** turning Mica OFF used to call
`ExtendGlassFrame(false)`, collapsing `GlassFrameThickness` to `0` — which **destroyed the
window border and Windows 11 rounded corners**. Cause: the RibbonWindow template keeps
`GlassFrameThickness="-1"` as its resting state, and on a WindowChrome window (native NC
frame stripped) that extended glass is the *only* thing the DWM has to draw the border and
rounded corners. Collapsing to `0` removes them. (It only became visible after we added the
`WS_SYSMENU` toggle's `SWP_FRAMECHANGED`, which forces the frame to recompute.) Fix: on
Mica-off, **leave the glass extended** (don't call `ExtendGlassFrame(false)`) — the opaque
window background is enough to avoid the black-background problem. `ExtendGlassFrame`'s
remarks now warn about the `false`/`0` case.

**Title-bar-through-Mica (added this session):** the title bar can go transparent so
Mica composites through it, but *only* in the case where a solid bar isn't wanted:
Office 2024 **and** the title bar is not colored. Rules: 2024 + non-colored →
transparent (Mica); any other theme + non-colored → keep the theme's light grey/white
band; colored title bar (any theme) → keep the accent. This lives in
`ThemeManager.SetTitleBarBackdrop(app, bool)` / `IsTitleBarBackdrop`, which sets/clears a
transparent `TitleBar.Background` override inside `ApplyTitleBarOverride`. Because that
method runs on every `Apply`/accent/accent-title-bar change, the transparency is
**re-derived on theme switch** — fixing the earlier bug where changing theme reverted the
title bar to a solid color instead of staying transparent. Caption foreground/hover are
left at their theme defaults (dark text + light hover), which read fine over the material.

**Native caption buttons pitfall (real bug we hit):** with the glass frame extended
(`-1`), a *transparent* title bar let the DWM's own min/max/close buttons show through and
overlap our custom caption buttons (they were previously just covered by the opaque bar).
Fix: `MicaHelper.ShowNativeCaptionButtons(window, bool)` strips/restores `WS_SYSMENU` via
`SetWindowLong(GWL_STYLE)` + `SetWindowPos(SWP_FRAMECHANGED)`. Chosen over
`WindowStyle="None"` deliberately: it's surgical (leaves `WindowStyle` =
`SingleBorderWindow`, so all the tuned maximize/snap/work-area handling is unchanged) and
it toggles **live** with no HWND recreation — which `WindowStyle` can't do. Trade-off:
Alt+Space system menu + window-icon menu are gone while it's off (fine for a fully
custom-chrome window). Toggled in sync with the Mica on/off state.

**Backstage stays opaque under Mica (decision this session):** the modern
`Translucent` effect didn't read well over Mica, so the showcase no longer enables it.
An opaque backstage fully covers the content behind it, so Mica shows only in the title
bar / ribbon chrome, never bleeding through the backstage page. (`Backstage.Translucent`
is kept as a library API, just unused by the sample.)

- **VERIFIED on real hardware (user-confirmed):** maximize with Mica ON stays inside the
  work area (the measured compensation absorbs the glass overhang); native caption buttons
  are gone with a transparent bar; a theme switch keeps the bar transparent; and the
  colored-title-bar toggle flips 2024 back to an opaque accent bar. Only remaining Mica idea
  is a future one: dark-mode-aware translucency.

### 3.13 UI polish fixes

- **Gallery scroll-to-chosen-item: ATTEMPTED, then REVERTED (known-good restored).** The
  idea: committing a pick in an `InRibbonGallery`'s expanded popup would close the popup
  (Office-style) and scroll the single-row strip to the selected tile so you'd see the pick
  once the popup was gone. It was implemented via `OnSelectionChanged` (deferred close) +
  a `ScrollSelectedItemIntoView` on close. **It repeatedly broke popup hit-testing** and was
  backed out — `InRibbonGallery.cs` is now the original pre-feature version.
  - **Why it's fragile (for whoever retries this):** the strip and the popup share ONE
    `ScrollViewer` that re-homes between them. Scrolling that scroller for the strip (whose
    viewport is a single ~54px row) leaves it in a state that corrupts the popup's
    hit-testing when the same scroller is re-homed there. Symptoms walked through three
    forms: (1) selecting the tile *below* the clicked one (leftover vertical offset carried
    into the popup — clicks off by exactly the offset, clamped at the ends); (2) after adding
    `ScrollToVerticalOffset(0)` on open, a *scale-like* miss where only the top row selected
    correctly and everything lower clamped to the last item (the scroller's **viewport was
    stale** right after the re-home — top row hit-tests, everything past the stale viewport
    clamps to the bottom). Rendering stayed correct throughout, so it *looked* like a DPI
    scale bug but wasn't (it worked at the same DPI before the feature).
  - **If retried:** don't scroll the shared re-homed scroller. Give the popup its **own**
    items presenter (don't re-home), or reset/relayout on the popup's `Opened` event once the
    content is actually laid out — not synchronously right after the re-home.
- **Tab underline hover flicker (2024) fixed.** The hover trigger is scoped to
  `SourceName="HeaderChrome"`, but the three indicator rectangles (`HoverIndicator`,
  `SelectionIndicator`, `ContextualSelectionIndicator`) are *siblings* overlaying it. A
  hit-testable underline stole the mouse from HeaderChrome → `IsMouseOver` dropped → the
  underline hid → the hit fell back to HeaderChrome → repeat = flicker (only on the
  hover underline; the active tab has no hover state so it never flickered). Fix:
  `IsHitTestVisible="False"` on all three indicators. The File button was immune because
  its trigger uses the button's own `IsMouseOver` and its underline is a descendant.
- **ComboBox height.** The input box had no min height, collapsing to the text height.
  Added `MinHeight="24"` to the input `Grid` in the `RibbonComboBox` template.
- **Showcase content area → document editor.** The centered instruction StackPanel was
  replaced with a Word-like layout: a `Border` "panel" with rounded TOP corners
  (`CornerRadius="8,8,0,0"`, square bottom meeting the status bar) hosting an editable
  `RichTextBox` (`DocumentEditor`, borderless/transparent so the panel supplies the card
  look), plus a `StatusBar` docked at the bottom (DockPanel items panel so left cluster and
  right zoom split to the edges). The same instruction text now lives in the RichTextBox's
  `FlowDocument`. **Live-preview wiring note:** the preview sentence is a named `Run`
  (`x:Name="StylePreviewText"`) inside the document — `Run` exposes the same
  `FontSize/FontWeight/FontStyle/Foreground` the old `TextBlock` did, so `ApplyStyleToSample`
  in the code-behind kept working unchanged (just retargeted from a TextBlock to a Run).

### 3.14 XAML design-time preview (active tab + backstage)

Goal: see a specific tab's content — and the backstage — on the XAML designer surface
instead of guessing. Two mechanisms, both driven by the developer's design-time `d:` attrs.

- **Active tab.** Added `Ribbon.SelectedIndex` (int, two-way, mirrored with `SelectedTab`
  via `OnSelectedTabChanged`/`OnSelectedIndexChanged` behind a `_syncingSelection` guard).
  It's a real runtime API too, but its point here is design-time: `d:SelectedIndex="2"`
  previews the third tab's groups on the surface without touching runtime selection.
  Timing: a `SelectedIndex` set before the child tabs are parsed (or a `d:` value applied
  during tree construction) is re-applied by `OnTabsCollectionChanged` and honored by
  `EnsureSelection`, so it lands once the tabs exist.
- **Backstage.** The runtime overlay is a `BackstageAdorner` added to **the window's**
  adorner layer via `Window.GetWindow(this)` — which is null in the designer, so the
  overlay silently no-ops and nothing shows. Fix: a design-mode-only path. The Ribbon
  template carries a normally-Collapsed `PART_DesignBackstageHost` (`Border`,
  `Grid.RowSpan="2"`, `MinHeight="440"`); `UpdateBackstageOverlay` checks
  `DesignerProperties.GetIsInDesignMode` and, when true, hosts the `Backstage` element in
  that border (no window, no adorner, no animation) instead of the adorner. `OnApplyTemplate`
  reflects `IsBackstageOpen` into it once the host exists. Runtime is untouched (the design
  host stays Collapsed/empty). Preview it with `d:IsBackstageOpen="True"`. Note: the design
  host lives inside the ribbon, so the preview covers the ribbon's area (not the whole
  window) — enough to see/edit backstage content. **Needs verification in VS/Blend** (can't
  drive the designer from the build box); `d:` honoring and design-surface rendering are the
  two things to confirm there.

### 3.15 QAT customization + extensible options dialog (Word-Options style)

Goal: Office-style customization — right-click "Add to Quick Access Toolbar", a QAT
customize page, and ONE extensible options dialog the app can merge its own pages into
(so RibbonKit's customization pages and the app's options live together, like Word).

- **`RibbonOptionsDialog`** (`Controls/RibbonOptionsDialog.cs`): a lookless `Window` —
  custom white title bar (see below) + left nav rail of `Pages` + selected page content +
  OK/Cancel. `RibbonOptionsPage : HeaderedContentControl` is one page; its `Content` can be
  ANY element, including app user controls — that's the extensibility. **Key template
  trick:** the page control's own template renders ONLY its Header (it *is* the nav entry,
  hosted in `PART_PageList`), while the dialog presents `SelectedPage.Content` separately —
  this avoids the element ever having two visual parents. Result flow: OK raises
  **`Applied`** (the app's persist cue, per user's "dialog result event" requirement) then
  sets `DialogResult=true`; Cancel → `false`. Styles ride theme tokens; rail brush is a
  local static (Modern-backstage precedent).
  - **Chrome + layout (user-refined):** `WindowStyle=None` + `WindowChrome`
    (`CaptionHeight=34`, `ResizeBorderThickness=SystemParameters.WindowResizeBorderThickness`)
    + `ResizeMode=CanResize` → the dialog draws its OWN white title bar: `Title` text left
    (no icon), a single Close button right (`PART_CloseButton`, reuses the RibbonWindow
    close-glyph/red-hover; no min/max — a modal needs none). Close = Cancel (no `Applied`).
    **Rounded corners:** a `WindowStyle=None` window doesn't get Win11 rounding for free, so
    `MicaHelper.SetRoundedCorners` (new; `DWMWA_WINDOW_CORNER_PREFERENCE=ROUND` +
    `DWMWA_BORDER_COLOR`) is called from `OnSourceInitialized`; the template therefore keeps
    `WindowChrome CornerRadius=0` and NO root border (the DWM draws the rounded border — a
    square one would fight it). Win10 (< build 22000) is a no-op (square, as it would be
    anyway). Layout: outer 2-row grid (title bar | body); body is 2 rows (rail+page | button
    bar), so the nav **rail spans only the rail+page row** and ends where the content does —
    the button bar is full width beneath both.
  - **Scroll policy (user-refined) — per-page via `IRibbonFillPage`:** the page content is
    hosted in a ScrollViewer (`PART_ContentScroll`) whose `VerticalScrollBarVisibility` the
    dialog sets in code (`UpdateContentScrollMode`, on `SelectedPage` change / `OnApplyTemplate`):
    `Disabled` when `SelectedPage.Content` is an **`IRibbonFillPage`**, else `Auto`. A ScrollViewer
    with vertical scroll *Disabled* measures its content with the finite viewport height (not
    infinity), so a Stretch control FILLS it — that's how `RibbonQuickAccessPage` (which
    implements `IRibbonFillPage`) fills the content area while its own two ListBoxes scroll
    internally; the dialog scrollbar never appears for it. Any other page keeps `Auto`, so tall
    app content scrolls in the dialog (convenient default). Extensible: a user page can implement
    `IRibbonFillPage` to fill too.
    **Dead ends we tried first:** no-scroll + a fixed `MinHeight` (magic number); then a
    ScrollViewer + `MaxHeight`=viewport (only *caps*, so inside the infinite-height ScrollViewer
    the page shrank to content and floated short/centered); then `Height`=`ViewportHeight` binding
    (fragile). The Disabled-scroll approach needs no page-height binding at all.
- **QAT proxies (`Ribbon.AddToQuickAccess`)**: a WPF element has ONE visual parent, so
  adding a ribbon control to the QAT creates a small PROXY button mirroring its 16px
  icon/ScreenTip. Invocation reuses `KeyTipService.InvokeControl` (now `internal` static) —
  the UIA Invoke/Toggle path KeyTips already use, so split buttons invoke their PRIMARY via
  their automation peer. Toggles instead get a **two-way `IsChecked` binding** to the source
  (state lives on the source; both stay in sync; source Checked/Unchecked handlers run).
  Proxies carry the readonly attached `Ribbon.QuickAccessSource` so Remove/duplicate-check/
  dialog can map proxy → source (`IsInQuickAccess` checks both identity and source).
  **v1 limitation:** a dropdown proxy opens the source's popup at the *ribbon* location, not
  at the QAT. Combos/galleries aren't offered as candidates.
- **`RibbonQuickAccessPage`** (`Controls/RibbonQuickAccessPage.cs`): the built-in customize
  page — available commands (left; flattened from `Tabs→Groups→logical descendants`, since
  groups host arbitrary panels; depth-capped, popup content never reached because those
  types aren't descended into) | Add/Remove/Up/Down | current QAT (right). Display via
  `RibbonCommandEntry` wrappers ("Home › Font › Bold" + icon). Edits are LIVE on
  `QuickAccessItems` (Office batches until OK; simpler v1 — `Applied` still signals when to
  persist). Subscribes `QuickAccessItems.CollectionChanged` while loaded so a right-click
  add elsewhere refreshes the open dialog.
- **Right-click menus**: `Ribbon.OnContextMenuOpening` override — if the (visual-then-
  logical; `VisualTreeHelper.GetParent` throws on non-visuals like `Run`s, hence the guard)
  ancestor walk from the click finds a `RibbonButton`/`RibbonToggleButton`/
  `RibbonDropDownButton`, it opens: Add to QAT (disabled if already there) / Customize
  Quick Access Toolbar… / Collapse the Ribbon. QAT items are untouched by this path —
  their hosts carry the shared placement menu, which opens (and sets Handled) before the
  event bubbles to the ribbon. That shared menu gained "Remove from Quick Access Toolbar"
  (+ separator, both hidden when the click wasn't on an item) and "Customize…": the hosts'
  `ContextMenuOpening` records which item was clicked into `_qatMenuTarget`
  (`AttachQatContextMenu` wires menu + hook at all three host sites), because the SHARED
  menu's Opened event alone can't tell.
- **`Ribbon.QuickAccessCustomizeRequested`** event: raised by both "Customize…" items; the
  app opens its merged dialog (RibbonKit doesn't open a dialog itself — the app owns it, so
  IT decides which pages exist).
- Showcase: View → Application → **Options** button; both entry points open the same dialog
  (an app "Editor" demo page + the QAT page; the right-click path pre-selects the QAT page);
  `Applied` sets the status bar to "Options applied".
- **Deferred (design sketched, not built):** the "Customize the Ribbon" structure page —
  tab show/hide checkboxes (→ `tab.Visibility`), tab/group reordering (the `Tabs` /
  `tab.Groups` observable collections already support `Move`; group moves across tabs =
  remove+add, mind single-parent timing à la §3.8), custom tabs/groups, and customization
  persistence (serialize QAT sources + ribbon layout). Slots into the dialog as just
  another `RibbonOptionsPage`.

### 3.16 "Customize the Ribbon" structure page

`Controls/RibbonCustomizePage.cs` — the second built-in dialog page (the §3.15 sketch,
built). Layout mirrors Word: available commands (left) | Add/Remove | structure TreeView
(right: tabs → groups → commands, checkbox = tab visibility) | Up/Down, with New Tab /
New Group / Rename under the tree. Implements `IRibbonFillPage`.

- **Office-consistent rules** (they keep customization reversible): reorder anything
  in-parent; hide/show any non-contextual tab EXCEPT the last visible one (the checkbox
  snaps back — note: the refusal notification must be **dispatched**, a synchronous
  `PropertyChanged` inside the setter is swallowed by the binding's reentrancy guard);
  ADD commands only into CUSTOM groups; REMOVE only custom tabs/groups/commands; RENAME
  tabs, groups, and custom (proxy) commands. **Contextual tabs are excluded** from the
  tree — the app drives their visibility (a manual checkbox would fight it).
- **`Ribbon.IsCustom` attached property** marks user-created tabs/groups (the page sets it
  on New Tab/New Group; apps may pre-mark XAML-declared ones to make them user-editable).
  Custom entries display an "(Custom)" suffix like Office. New custom groups get a
  vertical-`WrapPanel` items panel so Medium proxies wrap into 3-row columns instead of
  the default StackPanel overflowing the groups row.
- **Command proxies reused from §3.15**: `CreateQuickAccessProxy` generalized to
  `Ribbon.CreateCommandProxy(source, size)` — Small for the QAT, Medium (icon + label) for
  custom groups. Same invoke/toggle-sync semantics.
  - **Toggle proxy also raises the source's `Click`** (later fix): the toggle proxy's `IsChecked`
    is two-way bound to the source, which fires the source's `Checked`/`Unchecked` — but a toggle
    whose action is wired via `Click` (a valid pattern, e.g. the showcase's disable-samples toggle)
    never ran when proxied, so the copy only mirrored the checked state. The proxy now also raises
    `ButtonBase.ClickEvent` on the source, making a proxy click equivalent to a direct click.
    `RaiseEvent` doesn't re-toggle `IsChecked` (the binding already did), so there's no double-toggle,
    and it runs after the state has updated so the handler reads the new value.
- **`RibbonCommandCatalog`** (new, internal): the command discovery/description helpers
  extracted from the QAT page so both pages agree — `CollectControls` (logical-tree walk,
  depth-capped, skips proxies to prevent proxy-of-proxy chains), `CollectAvailable`
  (path-prefixed entries), `Describe` (caption+icon; a renamed proxy shows its own header).
- **Tree mechanics**: `RibbonCustomizeNode` (public, INPC) exposes `IsSelected`/`IsExpanded`
  two-way bound via `TreeViewItem` `ItemContainerStyle` — that's what lets the page
  re-select the moved/renamed/added item after each full tree rebuild (rebuild-per-edit is
  deliberate: trees are small, incremental sync isn't worth it). Custom groups list their
  `Items` directly (mutable: add/remove/reorder = `Items` ops); built-in groups show their
  commands via the catalog walk **read-only** (they live inside arbitrary panels, so
  `Items`-level ops are impossible — also why command reorder is custom-groups-only).
- **`Ribbon.RibbonCustomizeRequested`** event + "Customize the Ribbon…" in the ribbon
  right-click menu (next to the QAT one). Showcase: third dialog page "Customize Ribbon";
  both right-click entries open the dialog pre-selected on the matching page.
- **Still deferred:** persistence (serialize layout + QAT; enables Reset/Import/Export),
  drag-drop in the tree, moving groups across tabs.

**Round 2 (user-verified round 1, then requested):**

- **Proxy label fix:** small-sized sources (B/I/U) have no `Header`, so Medium/Large proxies
  were label-less. Proxies now derive a label from the ScreenTip title with the trailing
  "(Ctrl+B)"-style shortcut stripped (`StripShortcutSuffix`) — label "Bold", tooltip keeps
  the full title. Proxies also copy `LargeIcon` now (needed for the Large layout).
- **`RibbonGroup.Layout`** (DP + `RibbonGroupLayout` enum): `Default` (content-driven, never
  forces anything — built-ins safe), `Stacked` (vertical-wrap panel → 3-row columns;
  items Medium/Small), `Large` (horizontal row; items forced Large). Setting it swaps the
  ItemsPanel and normalizes direct items' sizes (`NormalizeItemSize`: Large layout → Large;
  Stacked demotes Large→Medium, preserves Medium/Small). New custom groups set `Stacked`
  explicitly (the enum's `Default` default means the change callback fires). Add-command
  proxies size to the target group's layout.
- **Edit dialog** (`RibbonCustomizeEditDialog`, replaces the cramped inline rename row —
  the "Edit…" button under the tree): a small `SizeToContent=Height` modal with the same
  chrome recipe as the options dialog (white close-only title bar, DWM rounded corners).
  Per-target sections: name (always; built-in tabs/groups = name-only, like Office);
  custom groups add an **icon picker harvested from the ribbon's own icons**
  (`RibbonCommandCatalog.CollectIcons`, + "no icon"; user-chosen: self-contained over
  app-supplied) and the **layout choice** (Stacked/Large — user chose the two-layout set);
  custom-group commands add **button size** (Medium/Small), shown locked to "Large" when
  the group's layout is Large. Office fun fact honored: Office's own "Rename" dialog for
  custom groups is secretly this (it has a symbol picker).
  - **Sizing fix (user-hit):** `SizeToContent=Height` + `WindowStyle=None` + WindowChrome
    **collapsed the dialog width** to ~nothing (WPF mis-measures a custom-chrome window's
    size). Replaced with a fixed `Width=460` and a `Height` derived in `OnSourceInitialized`
    from which sections are enabled (base + icon/layout/size adders) — deterministic, no
    SizeToContent.

### 3.17 Customization persistence (serialize / restore / Reset)

`Controls/RibbonCustomizationSerializer.cs` (new, public static) — saves and restores the
user's ribbon customizations as JSON so they survive restarts, and so **Reset / Import /
Export are all just `Apply` of a different string**. Two entry points: `Serialize(ribbon)
→ string` and `Apply(ribbon, json)`.

- **Stable identity via `Ribbon.CommandId`** (new attached string property): the whole
  scheme keys off a stable id. Proxies don't survive a restart, so a saved "custom group
  contains Bold" persists Bold's **source id** (`cmd.bold`), and `Apply` recreates the proxy
  via `CreateCommandProxy(source, size)`. Custom tabs/groups created in the page auto-get a
  generated id (`"custom:" + Guid.N`). Built-in **tabs/groups** without an id are left alone
  (not serialized, never touched by `Apply`) so an app opts into tab/group persistence
  incrementally.
- **Command tagging is OPTIONAL (`BuildIdentity`)**: a command need NOT carry an explicit
  `CommandId` to be addable to a custom group and survive restart. `BuildIdentity` walks the
  ribbon once and keys every command under BOTH its explicit id (when set) AND an auto-derived
  path id (`auto:tabKey/groupKey/caption#index`, where tab/group keys prefer their CommandId
  then fall back to header). Serialize writes the preferred id (explicit else auto); `Apply`'s
  `sources` map registers both forms, so either resolves. Explicit ids are still better (stable
  across built-in renames/reorders); the auto id is the fallback. **This was the fix for
  "custom-group items don't persist"** — the first cut silently dropped any proxy whose source
  wasn't hand-tagged, and most showcase commands weren't. The per-group index stays stable
  because custom groups are all-proxy and the catalog skips proxies, so they contribute no
  controls to the walk.
- **What's captured** (`RibbonLayoutDto`): per non-contextual tab — id, IsCustom, header,
  visibility, and its groups; per group — id, IsCustom, header, `Layout`, and (custom only)
  an `IconCommandId` + the proxy commands (each = source id + header + size); plus the QAT
  as an ordered list of `Ref` ids (a proxy persists its source's id; a hand-declared QAT
  item persists its own id). Contextual tabs and id-less built-ins are skipped.
- **Icon persistence without serializing pixels**: a custom group's `Icon` is matched
  (`ReferenceEquals`) against a `CommandId → ImageSource` lookup harvested from the ribbon,
  stored as that command's id, and re-resolved on load. Icons never leave the app; the JSON
  carries only ids.
- **Full-reconcile `Apply`** (robust from ANY starting state — that's what makes Reset
  trivial): (1) catalog current identity → live elements from the CURRENT ribbon (the
  catalog **excludes proxies**, so `sources` holds only real commands + declared QAT items);
  (2) strip every custom tab/group back to the built-in skeleton; (3) rebuild the desired
  tab list — create custom tabs, re-find built-in ones by id, set visibility/header, and
  reconcile each tab's groups (create custom groups + their proxies, rename built-ins);
  (4) reorder tabs to match, appending any current tab the layout didn't mention (a
  newly-shipped built-in, a contextual tab) at the end — so unknown/contextual content is
  preserved, not destroyed; (5) rebuild the QAT in saved order. **Missing ids are skipped;
  a corrupt/foreign string is caught (`JsonException`) and leaves the ribbon as-is.**
- **Reset wired into the page**: `RibbonCustomizePage` gains a `ResetLayout` string DP + a
  `PART_ResetButton` (bottom-left of the template, like Office). The host passes the
  **baseline** it captured at startup; clicking Reset does `Apply(ribbon, ResetLayout)` then
  rebuilds the tree. The button disables when no baseline is supplied.
- **Showcase round-trip** (`MainWindow`): on `Loaded`, **capture the baseline first**
  (`Serialize` the factory ribbon — this is the Reset target, so it must precede any restore),
  then `Apply` the saved JSON from
  `%LocalAppData%\RibbonKitShowcase\ribbon-customization.json` if present. The options
  dialog's `Applied` event (raised on OK) writes the current `Serialize` back to that file.
  All the showcase's tabs/groups + the key commands (Paste/Cut/Copy/Format Painter, B/I/U,
  Find/Replace/Select, Table/Pictures/Link, Zoom/Options) and the three QAT buttons now
  carry `rk:Ribbon.CommandId`s so they're addable and round-trip.
- **Ordering invariant**: baseline capture MUST run before restore, and `Apply`'s
  proxy-excluding `sources` walk is what lets Reset work *after* customization (the custom
  proxies are ignored; Bold is re-found in its built-in Font group). Import/Export are not
  yet surfaced in the UI but are one `Apply`/`Serialize` call away.

### 3.18 QAT/dialog polish batch (context menus, persistence, maximize, layout, hover)

Six nagging issues after persistence landed:

- **QAT item right-click menu now works in ALL placements.** Only the title-bar QAT showed the
  Remove/placement menu; the tab-row and below-ribbon hosts fell through to the ribbon's
  "Add to QAT" command menu. Cause: those hosts live INSIDE the ribbon, so a right-click on a
  QAT proxy (itself a `RibbonButton`) bubbled to `Ribbon.OnContextMenuOpening`, where
  `ResolveCommandControl` matched the proxy and hijacked it (marking the event handled, which
  suppressed the host's own menu). Fix: `OnContextMenuOpening` now bails early when the click
  resolves to a `QuickAccessItems` member (`ResolveQuickAccessItem`), letting the host's shared
  placement/Remove menu open. The title-bar host was unaffected because it's projected into the
  window, outside the ribbon's tree.
- **QAT placement now persists.** Added `QuickAccessPosition` to `RibbonLayoutDto`
  (serialize/apply). Because placement and item add/remove happen via the RIGHT-CLICK menus (not
  the options dialog), the showcase now also saves eagerly: it subscribes to
  `QuickAccessItems.CollectionChanged` and to `QuickAccessPosition` changes (via
  `DependencyPropertyDescriptor`) AFTER the initial restore, so those out-of-dialog edits persist
  too. (Kept in the same JSON file — no need for a separate one; the position is one nullable
  enum field, null in older files → left as-is.)
- **Options dialog maximize fixed.** `RibbonOptionsDialog` is resizable, so it can be maximized
  (double-click title bar / Win+Up) and overhung the work area like any WindowChrome window. New
  `Interop/MaximizeGuard.cs` encapsulates the exact mechanism `RibbonWindow` uses (WM_GETMINMAXINFO
  clamp + measured work-area inset applied to a `PART_WindowRoot`); the dialog attaches it in its
  ctor and names its template root. Deliberately duplicates RibbonWindow's logic rather than
  refactoring that verified type — consolidation is a future cleanup.
- **Customize QAT page now matches the Customize Ribbon page.** Up/Down moved out of the middle
  button stack into a fourth column to the RIGHT of the current-QAT list (mirroring the ribbon
  page's tree+Up/Down layout). Up/Down are icon-only (▲/▼) via a new compact
  `OptionsDialogReorderButtonStyle` (BasedOn the action style, MinWidth 40) applied on BOTH pages.
  Add/Remove now read `Add »` / `« Remove` (the tiny ▸/◂ glyphs were near-invisible).
- **Dialog action buttons show real, accent-following hover/press now.** `OptionsDialogActionButtonStyle`
  faded the chrome via `Opacity` (0.88/0.75) — imperceptible on a white button over a white dialog.
  Now the shared template overlays a translucent wash of the CURRENT accent (`Wash` border, accent
  `Background`, `Opacity` 0→0.16 hover→0.30 press) plus an accent border, so hover is a light tint
  of whatever the accent is (light green for a green accent, not a fixed blue). Every button on the
  customize pages already shares this one style (Add/Remove/Up/Down/New Tab/New Group/Edit/Reset,
  and Cancel), so they all follow suit — no per-button setup. **Two gotchas hit and fixed:**
  (1) the accent (OK) button uses `OptionsDialogPrimaryButtonStyle`, which needs its OWN template —
  an accent wash on an already-accent fill is invisible — so it washes translucent WHITE (lighten)
  on hover and BLACK (darken) on press, staying in the accent family for any accent colour;
  (2) the wash must fill the whole button, so `Padding` is applied to the CONTENT (`ContentPresenter.Margin`),
  NOT the Chrome border — a Chrome padding leaves an un-washed rim ("only the centre lights up").
  (Ribbon command buttons were already fine: `#E6E6E6` hover on the `#FFFFFF` ContentBackground.)

### 3.19 Dropdown proxies (real dropdown that borrows the source's menu)

Adding a `RibbonDropDownButton` (e.g. "Select") to the QAT or a custom group needs a proxy whose
flyout drops under the PROXY, toggles/dismisses correctly, and works regardless of the source's
tab. A first cut made the proxy a plain button that re-opened the SOURCE dropdown (optionally
retargeting the source popup's `PlacementTarget`) — but that had two flaws: clicking the proxy
re-fired "open" so it never toggled closed and the source's dismiss helper didn't recognise the
proxy as its opener; and it depended on the source being realized, so a proxy in a custom group on
another tab did nothing (the source's popup didn't exist).

Fixed by making the proxy a **real `RibbonDropDownButton` with its own popup** that BORROWS the
source's menu items while open:

- `CreateCommandProxy` builds a `RibbonDropDownButton` (mirroring the source's icon/header/size/
  ScreenTip) and calls `proxyDrop.BorrowMenuFrom(source)`. Being a real dropdown, it gets the
  correct toggle (its `PART_Toggle` two-way-binds `IsDropDownOpen`), its own `PopupDismissHelper`,
  and a popup placed under ITSELF — so placement, toggle-close, and light-dismiss all just work.
- **Borrow, don't share.** A `RibbonMenuItem` is a single-parent UIElement, so it can't live in
  two dropdowns. `OnIsDropDownOpenChanged` moves the source's items INTO the proxy as it opens
  (before the popup lays out, so it sizes to the real menu); `OnPopupClosed` moves them back.
  `Items` is a logical collection that exists whether or not the source's tab is realized, so this
  works cross-tab — the fix for the custom-group case.
- **Return is deferred + guarded.** Items are returned on a `DispatcherPriority.Background` post so
  we never reparent a menu item mid-click-dispatch, and a `_borrowed` flag + an `IsDropDownOpen`
  re-check make a fast close→reopen a no-op (items stay in the proxy) — no loss, no double-move.
- Only one of {source, any proxy} shows the menu at a time (only one popup open at once), so the
  source is whole whenever its proxy is closed.
- **Split proxies now include the dropdown too.** A split-button QAT proxy is a real
  `RibbonSplitButton`: its primary part invokes the source's primary action, and its chevron opens
  the source's menu under the proxy (borrowed, same as the dropdown proxy). So the split's dropdown
  IS in the QAT, matching Office.
- **Colored-surface QAT tinting is unified across ALL button types.** `Ribbon.QatOnColoredSurface`
  (bool, `Inherits`) gates the triggers; the brushes are **per-item resource overrides** consumed
  via `{DynamicResource RibbonKit.Brushes.Qat.ColoredHoverBackground}` / `...ColoredPressedBackground`
  (see gotcha below). Every QAT button — RibbonButton, RibbonToggleButton, dropdown opener, split
  primary + toggle — white-outs its Small icon (and chevron) on a colored surface AND carries
  a consistent set of state triggers: `IsMouseOver+colored → ColoredHoverBackground`, then
  `IsPressed/IsChecked+colored → ColoredPressedBackground` LAST so the pressed/open/checked state wins
  and holds ONE stable background. That last part fixed three bugs: (1) the **flicker** — an open
  dropdown/split had no colored "open" state, so it flipped between the band hover and the neutral
  gray checked box as `IsMouseOver` changed during the click; (2) **no pressed effect** on a colored
  bar — the colored hover trigger used to win over the neutral pressed one, so nothing changed on
  press; (3) the **toggle** (e.g. Bold) had NO colored treatment at all — dark icon on an opaque
  light box. The pressed key resolves to `CaptionButton.PressedBackground` (title bar) so the
  pressed/checked look matches the window's caption buttons.
  - **Gotcha (brushes for nested template parts: publish as RESOURCES, not bindable properties).**
    Three binding-based attempts to hand the band brushes to the dropdown/split proxies' nested
    parts (opener toggle, split primary + chevron) all failed the same way: the trigger `Setter`
    binding produced `null`, and a `Border` whose trigger sets `Background=null` is NOT
    hit-testable — so on a WindowChrome title bar hovering dropped the button out of hit-testing
    and the click fell THROUGH to the caption (drag/maximize), with no hover/press visuals.
    The attempts: (a) `Inherits` attached brush set via `SetResourceReference` + `RelativeSource
    Self` read — a resource-reference value does not propagate its resolved brush to inheriting
    template children (the plain bool `QatOnColoredSurface` does, which is why the triggers still
    *fired*); (b) `RelativeSource AncestorType` read — `FindAncestor` in a template-trigger
    `Setter.Value` never delivered the value; (c) plain `SetValue` of resolved brushes + `Self`
    read — still null at the nested `Chrome` (user-verified). What DOES work — user-verified —
    is a plain `{DynamicResource}` in the trigger setter. So `UpdateQatButtonContext()` resolves
    the band brushes (via `TryFindResource` on the Ribbon, never-null with a `Transparent`
    fallback) and writes them into each item's `Resources` under the two `Qat.Colored*` keys;
    resource lookup walks from the nested `Chrome` up to the proxy, finds the override, and
    re-resolves when the entries are rewritten on theme/accent changes. Token dictionaries carry
    safety-net defaults for both keys so an unresolved lookup can never reintroduce the null.
    ALL colored hover/pressed setters (plain + toggle buttons too, previously `TemplatedParent`
    bindings) now use the same two keys — one mechanism everywhere.

### 3.20 Large-button label: inline dropdown chevron + multi-line ellipsis

- **Inline chevron (dropdown Large layout).** The Large `RibbonDropDownButton` drew its ▾ as a
  separate `<Path>` ROW under the label, making the button taller than a plain large button (a
  visible vertical offset in a mixed group). Now the chevron is part of the LABEL text — a Segoe
  MDL2 `ChevronDown` (`&#xE70D;`) in a small trailing `<Run>` after two spaces — so it flows and
  wraps with the last word (like Word) and adds no extra row. The button then matches a plain
  large button's height. Medium/Small keep their inline Path (they're already horizontal, no
  offset). Split-button's arrow is a separate side column, so it was never affected.
- **Multi-line ellipsis (all large layouts).** Long labels wrapped past two lines and grew the
  button unbounded. WPF `TextBlock` has no `MaxLines` (that's UWP), but `TextWrapping="Wrap"` +
  `TextTrimming="CharacterEllipsis"` + a height cap gives multi-line ellipsis: it wraps up to the
  cap, then ellipsizes the last visible line. Applied to RibbonButton / RibbonToggleButton /
  RibbonSplitButton / RibbonDropDownButton large labels with `MaxHeight="48"` (≈3 lines at the
  default ~12px font; `MinHeight="32"` still reserves 2). **Tradeoff:** the cap is a pixel height,
  not an exact line count (fine at the current font; would need `MaxHeight` ∝ FontSize to be
  font-independent), and allowing 3 lines lets a long-labelled button be taller than its 2-line
  neighbours (Office usually caps at 2 for uniformity) — change the one `MaxHeight` to `32` for a
  strict 2-line cap.

### 3.21 Backstage: footer items, button items, design-time page preview

- **Design-time page preview (#1).** `Backstage` is a `TabControl`, so `SelectedIndex` already
  selects the previewed page — no new plumbing. Recipe: `d:IsBackstageOpen="True"` on the ribbon
  (its design-time host renders the backstage on the surface) + `d:SelectedIndex="N"` on the
  backstage. Documented on the `Backstage` summary; demoed in the showcase.
- **Footer section (#2).** New `BackstageItemPlacement { Top, Bottom }` + a `BackstageTabItem.Placement`
  property, and a custom `BackstageNavPanel : Panel` used as the `IsItemsHost` (replacing the plain
  `TabPanel`). It packs Top items from the top and Bottom items from the bottom (Word's Account /
  Options footer), drawing a subtle divider above the footer block (a `SeparatorBrush` bound to the
  backstage Foreground, rendered at 0.25 opacity). All items stay in the one `TabControl`, so
  selection is unchanged — only vertical arrangement differs. The nav `DockPanel` is now
  `LastChildFill="True"` so the panel fills the column (letting bottom items reach the bottom).
  Works for both designs (shared template; divider follows the design's foreground).
- **Button items (#3).** `BackstageTabItem.IsButton` makes an item an ACTION, not a page: it gains
  `Command`/`CommandParameter` and a `Click` routed event. `OnPreviewMouseLeftButtonDown` marks the
  input handled (suppressing TabItem's bubbling selection) and calls `Activate()` (raise Click, run
  Command); `OnKeyDown` does the same for Enter/Space. A safety net in `Backstage.OnSelectionChanged`
  reverts selection off any button item (guarded against re-entrancy) so it can never become the
  active page even via keyboard — and arrowing PAST one does nothing (invocation is click/Enter only).
  Showcase: an "Account" footer page plus "Options" and "Exit" footer buttons (new Account/Exit icons).
- **Tab-focus leak (#4) — FIXED.** With the backstage open, Tab used to reach the ribbon/document
  controls behind the adorner overlay. Root cause: the backstage lives in the WINDOW'S ADORNER LAYER
  (a separate visual branch that paints on top of the content but isn't between it and the focus tree),
  so those covered-but-still-tabbable controls stayed in the tab order. Fix: a **focus trap** on the
  `Backstage` element — `KeyboardNavigation.SetTabNavigation(this, Cycle)` in the (new) instance
  constructor. Cycle contains Tab/Shift+Tab within the backstage subtree and wraps at the ends, so once
  the host `Focus()`es the backstage on open (existing behavior, both the fresh-open and reopen-during-
  close paths) focus can never escape while it's up — matching Office. Applied unconditionally: a
  `Backstage` is only ever this overlay, and when closed the element leaves the tree so the setting is
  inert. Chose the focus trap over disabling the background content (the note's other option) because
  it's self-contained on the control and needs no open/close state on the ribbon. Only plain Tab was
  trapped (`ControlTabNavigation` left alone so the TabControl's Ctrl+Tab page switching is unchanged).

### 3.22 Design-time smart tags / quick actions (XAML designer) — VERIFIED IN VS

New `src/RibbonKit.Design/` project: **design-time only** tooling for the VS/Blend XAML
designer (toolbox defaults + right-click verbs for building a ribbon on the surface). Runtime is
untouched — the demo app owns any runtime contextual UI. **All of the below is user-confirmed
working in VS.**

**The architecture (the part that dictated everything):**

- **Targets the NEW (surface-isolation) WPF designer**, not the legacy one. So the design assembly
  targets **net472** (VS runs on .NET Framework), outputs **`RibbonKit.DesignTools.dll`** (the new
  `*.designtools.dll` discovery convention — the old one was `*.design.dll`), and is discovered from
  a **`Design` subfolder next to `RibbonKit.dll`** (csproj `DeployToDesignFolder` target copies into
  both TFM output folders; NuGet path is `lib/<tfm>/Design/`). **`RibbonKit.Design` is NOT added to
  the .sln** by default — build it once, then **close/reopen the designer** (it caches design assemblies).
- **Process-isolated from the runtime controls**: the extension can't reference RibbonKit or use
  `typeof` on control types. Everything is by **string type name** and edits go through the **Model API**.
- SDK: `Microsoft.VisualStudio.DesignTools.Extensibility` (namespaces
  `...Extensibility.{Metadata,Features,Model,Interaction}`). Registration = `[assembly: ProvideMetadata]`
  + `IProvideAttributeTable` / `AttributeTableBuilder.AddCustomAttributes(typeName, new FeatureAttribute(...))`.

**Hard-won specifics (all verified — don't re-derive):**

- **`TypeIdentifier` is in `...Extensibility.Metadata`** (not `.Model`), and its 2-arg ctor takes the
  **XAML namespace**, NOT the CLR namespace. RibbonKit declares `[assembly: XmlnsDefinition("urn:ribbonkit",
  "RibbonKit.Controls")]`, so `new TypeIdentifier("urn:ribbonkit", "RibbonTab")` + `ModelFactory.CreateItem(
  item.Context, id)`. Passing the CLR namespace made `CreateItem` silently fail (see next point) — this
  was THE bug that made "menus show but nothing happens".
- **The designer swallows exceptions thrown inside providers** — a failed edit just looks like nothing
  happened. `Diagnostics.cs` (`DesignLog`) wraps every action and logs start/ok/FAILED+exception to
  `%TEMP%\RibbonKit.DesignTools.log`. Keep it while iterating; strip before shipping.
- Adds use explicit collection property names, not `.Content`: `DesignModel.Add(parent, "Tabs"/"Groups"/"Items", child)`
  (avoids `.Content` ambiguity for the group's `HeaderedItemsControl.Items`). Button/nav caption = `Header`.
- **Enums are set by NAME STRING** — `props["QuickAccessPosition"].SetValue("BelowRibbon")`,
  `props["Placement"].SetValue("Bottom")` — because the design assembly can't reference the enum type;
  the property's type converter resolves it. Verified working.
- Singleton / checked state via `ContextMenuProvider.UpdateItemStatus`: `MenuAction.Enabled` /
  `.Checkable` / `.Checked`; read current values with `ModelProperty.Value` (is it set? → null when not)
  and `ModelProperty.ComputedValue` (effective value incl. defaults).

**Verbs shipped (all working):**

- Toolbox: `RibbonDefaultInitializer` seeds a dropped Ribbon with a "Home" tab + "Group".
- Ribbon: Add Tab; **Add Backstage** (once — disabled after one exists via `UpdateItemStatus`; also
  surfaces the File button, which is hidden while `Backstage` is null; seeds one "Info" nav item);
  **Quick Access Toolbar** submenu (`MenuGroup`, HasDropDown) — Title Bar / Tab Row / Below Ribbon,
  radio-checked on the current `QuickAccessPosition`.
- RibbonTab: Add Group + Move Tab Left/Right + Delete Tab.
- RibbonGroup: Add Button/Toggle/Split/Drop-Down + Move Group Left/Right + Delete Group.
- Leaf controls (button/toggle/split/drop-down): one provider on all four types — Move Control Left/Right + Delete Control.
- Backstage: **Add Nav Item** (a page) + **Add Nav Button** (a footer action: `IsButton=true`, `Placement="Bottom"`).
- Reorder = `ModelItemCollection` IndexOf/Remove/Insert via `item.Parent`; Delete = `.Remove`. All single-undo.

**Toolbox + Properties-window polish — DONE (Properties verified; toolbox is package-only):**

- Properties window: `PropertyMetadata.cs` puts the main controls' key properties under a "RibbonKit"
  category with descriptions, via the design attribute table (`AddCustomAttributes(type, prop,
  new CategoryAttribute(...), new DescriptionAttribute(...))`). **Verified showing in VS.** `IsBackstageOpen`
  is `[Browsable(false)]` (grid footgun — it'd persist to runtime; preview via `d:` instead); `SelectedIndex`
  kept visible with a runtime-vs-preview warning.
- Toolbox: the NEW designer does **NOT** use `ToolboxBrowsableAttribute`. Toolbox is populated from a
  NuGet-package **`tools\VisualStudioToolsManifest.xml`** allowlist (`<FileList><File Reference="RibbonKit.dll">
  <ToolboxItems VSCategory="RibbonKit" UIFramework="WPF"><Item Type="..."/>`). Created + wired into the
  package (`None Include ... Pack`). **Only takes effect when RibbonKit is consumed as a NuGet package** —
  a project-reference setup still reflects all public controls, so it does nothing in the current showcase.

**Smart-tag adorner panel — ATTEMPTED, DOESN'T RENDER in the new designer (don't retry blindly):**

- Empirically tested (`PrimarySelectionAdornerProvider`): the types all **exist and compile**
  (`PrimarySelectionAdornerProvider`, `AdornerPanel`, `AdornerPlacementCollection` in
  `...Extensibility.Interaction`), the provider **activates** on ribbon selection (logged
  `Activate`/`Deactivate`), and `Adorners.Add` succeeds (count = 1) — **but the custom WPF adorner UI
  never paints on the surface.** Explicit-size + on-surface placement didn't help. Conclusion: the new
  **surface-isolation** designer renders the surface in a separate process from where the extension runs,
  so custom adorner *visuals* aren't hosted (matches Microsoft's unresolved 2025 Q&A). Adorner *activation
  + model editing* work; adorner *rendering* does not. **The glyph/flyout smart tag is not achievable in
  the new designer with this API.** Spike file kept out of the committed project.
- Consequence: the **context-menu verbs are the delivery surface** for quick actions (they already cover
  every action the flyout would have). **Design-only preview** of a tab / backstage uses the idiomatic
  `d:SelectedIndex` / `d:IsBackstageOpen` in XAML (works today). A `DesignModeValueProvider` (design-time
  value that renders but isn't serialized) is the only remaining avenue to a togglable preview and is
  unexplored — note it changes *values*, which DO render, unlike adorner overlays.

**Still deferred:**

- **Design-time "Add to QAT"**: held — QAT items are runtime-generated proxies of a source command, not
  plain XAML, so there's nothing clean to write into markup. Needs a dedicated approach.
- `ParentAdapter` parenting rules; NuGet `Design/` packaging target (also carries the toolbox manifest).
  "Add Application Button" was **dropped** — no such element; the File button is intrinsic and appears with
  `Backstage` (only its text, `ApplicationButtonHeader`, is settable).
- `Diagnostics.cs` (`DesignLog`) is still wired into every verb — strip before shipping.

### 3.23 Ribbon Editor dialog (design-time) + tab-preview feasibility

Loop back to design-time tooling: a launchable **structure editor** dialog, plus settling
whether a `d:`-driven tab-preview toggle is achievable.

**Feasibility findings (verified against the current designer API, July 2026):**

- **Dialogs from verbs — YES.** The design assembly loads INSIDE the VS process (net472);
  only the *surface* is process-isolated. So a `MenuAction.Execute` handler can `new
  Window(...).ShowDialog()` — a plain code-built WPF window works (the runtime dialogs' themed
  templates aren't available here since the design assembly can't reference RibbonKit). This is
  unlike the adorner wall (§3.22): adorner *visuals* need the surface's process; a dialog does not.
- **Writing a literal `d:SelectedIndex` from an extension — NO.** The new ModelItem API has no
  design-namespace write path; `Properties["SelectedIndex"].SetValue(n)` writes the REAL attribute
  (persists to runtime). Confirmed against the ModelItem members list and the MS migration doc.
  Hand-authored `d:SelectedIndex` still works (the XAML *parser* honors it — §3.14) but can't be
  emitted programmatically.
- **Design-only preview toggle — YES, via `DesignModeValueProvider`** (supported in the new
  designer; the avenue §3.22 flagged as the one left). It returns a design-time render value for a
  property without serializing it, and re-runs on `InvalidateProperty`. Registration pattern
  (from Microsoft's own sample): `Properties.Add(new TypeIdentifier("RibbonKit.Controls.Ribbon"),
  "SelectedIndex")` + override `TranslatePropertyValue(ModelItem, PropertyIdentifier, object)`.
  The shipping toggle stores a chosen preview index (design-only backing the dialog writes) and
  returns it here; a literal `d:` write is neither needed nor possible.
- Aside: `SuggestedActionProvider` (the selected-element quick-actions flyout) is a newer
  extensibility point that renders in a POPUP (not on the surface) — a possible nicer launcher than
  the context menu later. Noted, not built.

**Built this session:**

- `RibbonEditorWindow.cs` — code-only WPF modal: a Tabs → Groups → Controls tree, a toolbar
  (Add Tab / Add Group / Add Control ▾ / Move Up / Move Down / Delete) and a Header rename box.
  Owned to the VS main window via `WindowInteropHelper` + `Process…MainWindowHandle` (best-effort).
  Edits go straight to the `ModelItem` tree through `DesignModel`; each op is its own undo (no
  OK/Cancel transaction — surface updates live, matching the verb model). Chose per-op scopes over
  a session-long `ModelEditingScope`/reconcile for lowest risk and to preserve unmodeled props.
- `DesignModel.cs` — added read helpers (`Children`, `Header`, `TypeName`, `IndexInParent`,
  `SiblingCount`) and scoped create/rename helpers (`AddTab`, `AddGroup`, `AddControl`, `Rename`).
- `ContextMenuProviders.cs` — "Edit Ribbon…" launcher verb on `RibbonContextMenuProvider`.
**Spike result (confirmed on Windows):** the dialog shows cleanly and modally from the verb —
so dialogs-from-verbs is proven. But a load-time-only `DesignModeValueProvider` did **nothing**:
the new designer calls `TranslatePropertyValue` **lazily** — only on
`ValueTranslationService.InvalidateProperty` or when the property is edited in the designer, never
on initial parse (the migration doc's fine print, now verified). The load-time spike never called
`InvalidateProperty`, so it never fired.

**Real preview toggle — built on the correct trigger (`TabPreview.cs`):**

- `SelectedTabPreviewProvider : DesignModeValueProvider` on `Ribbon.SelectedIndex` returns the
  editor's chosen preview index from `TranslatePropertyValue`; nothing is serialized and the
  running app is untouched (provider isn't invoked for run-time code).
- The trigger is the piece the spike missed: any feature holding the ModelItem can force
  re-evaluation via
  `ribbon.Context.Services.GetRequiredService<ValueTranslationService>().InvalidateProperty(ribbon, selectedIndexId)`
  (pattern lifted from Microsoft's CustomComboBox sample, where an AdornerProvider does it — we
  don't need the adorner, just the service call). `TabPreviewCoordinator.Set(ribbon, index)` stores
  the index in design-session state and fires that invalidation; the editor's **Preview tab** combo
  ("(no preview)" + one entry per tab) calls it. This is the supported equivalent of hand-authored
  `d:SelectedIndex` — which can't be written programmatically (no design-namespace write path in the
  model API). Preview is session state, so it resets on designer reload; that's fine for a live toggle.
- Replaced the throwaway `SelectedTabPreviewProviderSpike.cs`; `Metadata.cs` now registers the real
  `SelectedTabPreviewProvider`.

**Confirmed working on Windows (user-verified):** changing the editor's **Preview tab** combo
repaints the design surface to the chosen tab, with nothing written to the XAML and no runtime
effect. So the full chain — `TabPreviewCoordinator.Set` → `ValueTranslationService.InvalidateProperty`
→ `SelectedTabPreviewProvider.TranslatePropertyValue` → the ribbon's selection visual — works end
to end; the §3.14 `EnsureSelection` fallback was not needed. The dialog-from-verb path is likewise
confirmed. Design-time component work (editor + design-only preview) is done.

Cleanup: delete the retired `SelectedTabPreviewProviderSpike.cs` (unregistered/inert, replaced by
`TabPreview.cs`).

**Property editors added (per-item panel in the dialog):** the editor now shows a property panel
for the selected node, driven by a small spec table + `DesignModel.HasProperty` so only properties
the type actually has are shown (leans on the `FindProperty` lesson). Covered this pass: controls —
`Size` (enum), `SizeDefinition`, `ScreenTipTitle`, `ScreenTipText`; tab — `IsContextual` (bool),
`ContextualColor` (string via brush converter); group — `ShowDialogLauncher` (bool), `ReductionMode`
(enum), `CanResize` (bool). Editors: text (commit on Enter/lost-focus), checkbox (Click, so a
programmatic initial set doesn't write), enum combo (values set as strings → type converter, the QAT
trick). `DesignModel.SetProperty` wraps each in a scope and swallows/logs converter failures (e.g. a
bad colour) so a typo never crashes the dialog. Each edit = one undo. KeyTip access keys were
**deferred** here (attached property — different model access, unproven at the time) and later
implemented once attached-property access was proven — see the CommandId / KeyTip notes below.

**Split / drop-down button menu items (later pass):** `RibbonSplitButton` derives from
`RibbonDropDownButton`; both are `ItemsControl`s holding their flyout entries as `RibbonMenuItem`s in
`Items` — structurally identical to the combo/gallery item path. So the editor needed only an
`ItemRule` entry (`RibbonMenuItem`, caption = `Header`) plus a friendly type name: the existing tree
recursion into `Items`, the "Add Item" sibling-insert, and the caption/icon editors all flowed from
that. Menu-item text edits via the Caption box (Header); Icon via the icon picker.

**`Ribbon.CommandId` attached-property editing (unblocks the deferred KeyTip path):** the "different
model access" the KeyTip note flagged is now solved. Attached members don't surface through
`Properties[name]` (it only sees an element's own members and throws for an attached one), so
`DesignModel.FindAttached` resolves `Ribbon.CommandId` by a type-qualified `PropertyIdentifier`
(`new TypeIdentifier("RibbonKit.Controls.Ribbon")`, the same identifier form `TabPreview` uses). Two
paths: a fast string-indexer lookup for already-set values (the showcase controls carry
`rk:Ribbon.CommandId`), and a slow path that binds the collection's `Find(PropertyIdentifier)` /
`this[PropertyIdentifier]` accessor **by reflection** and logs which shape worked — the accessor's
exact signature in the shipped SDK couldn't be verified from the Linux sandbox, so reflection keeps a
wrong guess from breaking the build (same defensive style as the StaticResource icon spike). Exposed as
a "Command Id (persistence)" `AttachedText` row on tabs, groups, and command controls (hidden on
combo/gallery/menu/backstage entries, which aren't persistable commands); blank clears the attribute.
The same `FindAttached`/`SetAttached` helpers are what a future KeyTip-access-key editor would reuse.

**KeyTip access-key editing (the deferral, now DONE).** With attached-property access proven, the
parked KeyTip editor was straightforward: `KeyTip.Keys` is another attached string, just declared on
`RibbonKit.Controls.KeyTip` instead of `Ribbon`. `FindAttached`/`GetAttachedString`/`SetAttached` gained
an `ownerTypeName` parameter (deriving the short key form from its last segment), so the same reflection
resolver serves both owners. `PropSpec` gained an `AttachedOwner` field; a **KeyTip (Alt access key)**
`AttachedText` row now sits beside **Command Id** on the same node set — tabs, groups, and leaf command
controls (`ShowsCommandId` → `ShowsIdentityProps`). That set is exactly where the KeyTip service reads
`KeyTip.GetKeys` (tab / collapsed-group flyout / group launcher / leaf command), so the editor never
offers a KeyTip where the runtime ignores it. Blank clears the attribute, letting the ribbon auto-derive
a key from the label (Office behaviour); a pinned value overrides it.

**Icon picker (`Icon`/`LargeIcon`) — user wants the full Icons.xaml picker; treated as a spike.**
Icons are `DrawingImage` resources keyed `Icon.*` in the showcase's `Icons.xaml`, referenced as
`Icon="{StaticResource Icon.Paste}"`. So the picker needs to (1) enumerate those keys and (2) write
a **StaticResource reference** to the property — NOT a plain value or a URI (the icons are inline
vector resources, no file/URI form exists). Both halves use under-documented APIs (`ModelResource`
in `…Extensibility.Services`; no clear StaticResource-write on `ModelProperty`/`ModelFactory`) and
can't be tested from the Linux box, so — consistent with how the `d:` preview and the smart-tag
adorner were handled — it gets a probe before a full build.

**Write-spike round 1 (raw extension) — FAILED, informatively (user-confirmed):**
`property.SetValue(new StaticResourceExtension(key))` wrote `Icon="{StaticResource}"` with the **key
dropped**. Lesson: the model serializes the model TREE, not a raw CLR object's internals — a live
markup-extension object's `ResourceKey` is invisible to it. (Also confirmed `ModelFactory.CreateItem`
in the new API has NO `params object[] arguments` overload, so the key can't be passed as a ctor arg.)

**Round 2 (shipped): build the extension as a ModelItem + set `ResourceKey` in the model.**
`CreateStaticResourceItem` does `ModelFactory.CreateItem(ctx, <StaticResource TypeIdentifier>)` then
`ext.Properties["ResourceKey"].SetValue(key)`, and `SetStaticResource` assigns that ModelItem to the
target property. The exact `TypeIdentifier` form is unverified, so it tries three in order —
`(presentationNs,"StaticResourceExtension")`, `(presentationNs,"StaticResource")`, and CLR
`"System.Windows.StaticResourceExtension"` — logging which one creates successfully.
`SetStaticResource` then reads the key back and logs `read-back key = '…' (expected '…')`.

**Read-back added (`GetStaticResourceKey`).** The icon fields show the current key for buttons that
already have an icon, and read-back is what let round 2's write be verified.

**CONFIRMED WORKING on Windows (user):** setting an icon on a blank button, reading an existing
button's icon key, and copying it to another all work; the log is clean with correct read-back and no
errors. Icon read+write via a StaticResource model item is fully proven.

**Visual picker shipped (`IconPickerDialog` + `IconCatalog`).** Enumeration was the last constraint:
no reliable resource-enumeration API, `ModelItem.Source` is not a file path, and resources live in the
isolated surface process the extension can't read — so the extension can't discover Icons.xaml through
the design model itself.
Design that needs zero uncertain APIs: a "…" button on each icon row opens a picker that (1) always
lists the icon keys **already used elsewhere in this ribbon** (a pure model walk, `CollectUsedIconKeys`),
and (2) has **"Load Icons.xaml…"** — an `OpenFileDialog` that parses the file with `XamlReader.Load`
in the extension's own WPF context, so the `DrawingImage` values render as real **thumbnails**; the
loaded dictionary is cached for the session (`IconCatalog`). A filter box narrows the grid, the current
key is highlighted, and clicking a tile writes via the proven `SetStaticResource`. Graceful: useful
with no file loaded (used-keys), and it can't hit an undocumented API. Trimmed the now-proven spike
logging (read-back / create-attempt / model-type lines). Later polish: remember the Icons.xaml path
across sessions; a "(none)" tile to clear an icon (needs a verified `ClearValue`).

**Automatic Icons.xaml check (2026-08-11 follow-up).** The picker now performs a conservative
filesystem discovery before its first render while retaining **Load Icons.xaml…** unchanged. Because
the design extension runs in the Visual Studio process, it locates the DTE automation object registered
for that exact process in the Running Object Table, reads `ActiveDocument.FullName` and
`Solution.FullName` by reflection (no EnvDTE package/deployment dependency), and searches in priority
order: beside the active document, within its nearest `.csproj` directory, then the solution directory.
Build/output/cache folders and reparse points are skipped. It auto-loads only one unambiguous match;
none, multiple matches, an unavailable IDE context, or a parse failure leaves the browse workflow and
an explanatory picker status. The selected dictionary remains cached for the design-tools session.

**Nested containers (StackPanels) in the editor — DONE.** Real ribbons put a `StackPanel` (often a
vertical column of horizontal icon rows) inside a group's `Items`, not just leaf controls. The editor
now models that: `NodeKind` gained `Container`, `NodeInfo` stores its parent collection explicitly
(the same kind can live in a group's `Items` or a container's `Children`), and `AddItemNodes` recurses
into any node that has a `Children` collection (`HasProperty(child,"Children")` — Panels have it,
ribbon controls don't). New verbs: **Add Stack** (`DesignModel.AddStackPanel` via `CreateFramework`,
which creates the WPF `StackPanel` through the presentation xmlns / CLR-name fallback) — vertical in a
group, horizontal inside another stack; **Add Control** now targets the selection's child collection
(`ResolveChildTarget`: group→`Items`, container→`Children`, control→sibling) and defaults stacked
buttons to `Size="Small"`. Container nodes get an `Orientation` editor; `ResolveTab` now walks
ancestors by type so Add-Group works from any depth; `CollectUsedIconKeys` recurses into containers.
`DesignModel.AddControl` generalized to `(parent, collection, type, label, size)`.

**More control types in Add Control (DONE).** The menu now also offers Combo Box (`RibbonComboBox`),
Gallery in-ribbon (`InRibbonGallery`), Gallery drop-down (`RibbonGallery`), and `Separator`. `AddControl`
made `header` optional (only buttons get a caption + the Small-in-stack default; combos/galleries/
separators get neither) and now creates via `CreateAny` — tries the RibbonKit xmlns, then the framework
namespaces — so `Separator` (a `System.Windows.Controls` type) works alongside the RibbonKit controls.
Galleries/combos are leaf nodes (no `Children`), so the tree shows them without descending into their
items; editing gallery/combo items is a possible later step.

**Item editing (combo / gallery / backstage) + backstage toggle — DONE.** The tree now descends into
item containers too: a combo/gallery (`ItemRule` matches `RibbonComboBox` / `RibbonGallery` /
`InRibbonGallery`) expands via its `Items`, and the **Backstage** — a scalar `ribbon.Backstage`
property, not part of `Tabs` — is surfaced as its own root node whose nav items (`BackstageTabItem`)
are editable. New **Add Item** verb creates the right child per container (`ComboBoxItem` /
`RibbonGalleryItem` / `BackstageTabItem`) via `DesignModel.AddItem`, resolved by `ResolveItemTarget`
(the container itself, or the container of a selected item → sibling). Caption editing generalized:
the box is now **Caption** and edits `Header` OR `Content` (`DesignModel.CaptionProperty`/`GetCaption`/
`SetCaption`) — combo/gallery items caption via `Content`, everything else via `Header` — so the same
box renames buttons, tabs, backstage pages, and combo/gallery items. Item creation reuses `CreateAny`
(so framework `ComboBoxItem` and RibbonKit `RibbonGalleryItem`/`BackstageTabItem` both work).

**Gallery-item caption fix + type-specific props (DONE).** A `RibbonGalleryItem`'s `Content` is a
`StackPanel` (a visual), so stringifying it showed garbage like `Handle=103 … (StackPanel)`.
`CaptionProperty` now skips **complex** values (`ModelProperty.Value != null` / non-primitive
`ComputedValue`) and falls back to `Tag` for gallery items (their idiomatic identity — "Normal",
"Heading 1", …). So the tree shows gallery items by Tag, combo items by their string Content, and
buttons/tabs/backstage pages by Header — and the Caption box edits whichever applies. Added
type-specific property editors (shown ahead of the kind-based ones, deduped by name): `BackstageTabItem`
→ `IsButton`, `Placement` (Top/Bottom) [+ its `Icon` via the control specs]; `RibbonComboBox` →
`InputWidth`, `IsEditable` [+ ScreenTip]. Wired via `TypeSpecs(typeName)` + `SpecsForNode`.

**Show-backstage toggle:** a "Show backstage" checkbox next to the preview-tab combo, driven by the
same `DesignModeValueProvider` mechanism as the tab preview — `SelectedTabPreviewProvider` now also
translates `Ribbon.IsBackstageOpen`, and `TabPreviewCoordinator` gained `SetBackstage`/`TryGetBackstage`
(+ the invalidation targets `IsBackstageOpen`). Design-only, no XAML/runtime effect; the design-mode
backstage host from §3.14 renders it. The checkbox enables only when the ribbon has a `Backstage`.

**Backstage page switcher (later pass):** a **Page** combo beside the "Show backstage" checkbox
previews a specific backstage page on the surface. A second provider, `BackstagePagePreviewProvider`
(attached to `Backstage` in `Metadata`), translates the backstage's `SelectedIndex` the same design-only
way; `TabPreviewCoordinator` gained `SetBackstagePage`/`TryGetBackstagePage`. The combo lists nav pages
only (footer `IsButton` action items excluded, since they don't switch to a page) and maps each entry to
its true `Items` index; "(default)" clears the override; it's enabled only while the backstage is shown.
Wrinkle vs the ribbon's own `SelectedIndex`: the backstage's `SelectedIndex` is **inherited from
`Selector`**, so the property identifier's declaring type could be reported as either `Backstage` or
`Selector`. Which one the designer uses for an inherited DP is unverified from the sandbox, so the
provider registers **both** `(Backstage, SelectedIndex)` and `(Selector, SelectedIndex)` and the
coordinator invalidates under both — whichever the designer actually keys, one matches (a Windows build
confirms via the `[RibbonKit] Preview Backstage SelectedIndex -> N` debug line).

**Gallery-item content editing — TRIED, then ROLLED BACK (too noisy).** `AddNode` briefly descended
into a control's rich `Content`, but expanding every backstage page and gallery item into its full
visual tree (Borders, page bodies, etc.) drowned the structure. Reverted: `AddNode` now recurses only
into Panels (`Children`) and item containers (`Items`), never a control's Content. (`TextBlock` editors
+ "Add Text Block" + `ContentElement` are kept — inert unless a TextBlock is added to a group panel.)

**Color swatch picker (DONE).** `ContextualColor` and `TextBlock.Foreground` are now a `Color`
editor kind (`BuildColorEditor`): a live swatch + hex/name box + a "…" button that opens a
self-contained WPF `ColorPickerDialog` (a palette of standard/Office swatches plus a hex box with
preview — no WinForms dependency). Picking or typing still writes the value as a string through the
type converter (so it round-trips as a brush); `ColorPickerDialog.ParseBrush` renders the swatch and
is tolerant of invalid input (falls back to transparent).

**Scalar-value fix (the real bug behind the noise).** This designer wraps even a plain **string** value
in a child `ModelItem`, so `ModelProperty.Value != null` is NOT a reliable "is complex?" test — it
wrongly flagged string Header/Content as complex. Symptoms: items showed only their type (empty caption,
couldn't edit the header), and a combo item's string Content expanded into a bogus "String" child.
`IsScalarValue` now keys off `ComputedValue`'s TYPE (string / primitive / decimal → scalar) instead of
`Value`. Result: items display "caption [type]" again and the Caption box edits Header; a combo item's
**Content** is a scalar string, so it's shown/edited via the Caption box (no "String" child) — which is
how combo-item content editing is now done; and a gallery item's complex Content is correctly skipped,
so its caption falls back to `Tag`.

**Diagnostics added (`DesignLog.cs`):** the editor opened fine on a barebones ribbon but failed to
open on the full MainWindow.xaml ribbon — a hard throw during construction, which the designer
swallows so the dialog just never appears. Added a file-based log
(`%LOCALAPPDATA%\RibbonKit\DesignTools.log`), wrapped the "Edit Ribbon…" verb in try/catch (logs +
MessageBox with the log path), and made the dialog's tree reads defensive via
`SafeChildren`/`SafeHeader`/`SafeType`.

**Root cause found + fixed (user log, confirmed):** `ModelItem.Properties["Header"]` **throws
`ArgumentException` when the type has no such property — it does NOT return null** (my original
assumption). The full ribbon has controls in groups without a `Header` (combo boxes, galleries, …),
so reading them threw and aborted construction; the barebones ribbon had only headered buttons, so
it never hit it. Fix: `DesignModel.FindProperty(item, name)` wraps the throwing indexer and returns
null for an absent property; `Children`/`Header`/`HasHeader`/`IndexInParent`/`SiblingCount`/`Rename`
all route through it. The editor now walks mixed control types cleanly (no logged errors), labels a
header-less control by its type, and **disables Rename/​the header box for header-less items while
keeping Move/Delete** (those are structural, not header-dependent). This is a general lesson for all
future design-model access: never assume `Properties[name]` returns null for a missing property —
go through `FindProperty`.

### 3.24 Animation polish batch — all six remaining transitions wired

- **Hover/press cross-fade**: `RibbonButton`/`RibbonToggleButton` call `RibbonMotion.FadeWash`
  on their `_hoverWash`/`_pressWash` (and, for the toggle, `_checkWash`) layers instead of
  an instant visibility flip.
- **True sliding tab marker**: `RibbonTabControl` now owns `PART_TabMarker` +
  `PART_TabMarkerTranslate` and glides the underline between tabs (`UpdateMarker`,
  `RibbonAnimationAction.TabMarker`) instead of only sliding the tab content. The whole marker is
  **gated on `RibbonKit.Brushes.Tab.SelectedUnderline` being a visible (non-transparent) colour**, so
  it's effectively Office-2024-only — flat themes (2019/2013) set that token `Transparent`. This gate
  also covers contextual tabs: `UpdateMarker` tints the marker with the tab's own `ContextualBrush`, but
  bails before that when the theme's underline token is transparent, so a selected contextual tab no
  longer leaks an underline into flat themes (`IsVisibleBrush` helper).
- **Contextual-tab appear**: `RibbonTab.cs` plays `RibbonMotion.PlayOpen(this,
  RibbonAnimationAction.ContextualTab, RibbonSlideFrom.Top)` when a tab's contextual
  coloring turns on.
- **Toggle-state cross-fade**: covered by the hover/press item above — same `FadeWash`
  call, `ToggleState` action, `_checkWash` layer.
- **Theme-switch cross-fade**: `Ribbon.cs` calls `RibbonMotion.PlayThemeCrossfade` on the
  tab control when the active theme/accent changes (85%→100% opacity dip, not a full
  fade — a full fade would flash the already-opaque ribbon to transparent first).
- **KeyTip badge pop** (the last of the six, added this session): `RibbonMotion` gained a
  new `PlayKeyTipPop` method (fade + short downward settle, `RibbonAnimationAction.KeyTip`
  timing), called once from `KeyTipService.AddAdorners` right where a badge is first shown
  (that call site already guards on `item.Shown`, so it fires once per badge, not on every
  keystroke while typing a KeyTip). **Gotcha discovered and fixed**: a `DoubleAnimation`'s
  default `FillBehavior.HoldEnd` keeps holding the `Opacity` property after it finishes,
  which would have silently broken `KeyTipAdorner.Dimmed` (a plain property setter used to
  dim/undim a badge as the user types a multi-character KeyTip). `PlayKeyTipPop` clears its
  own animation and sets a plain `Opacity = 1d` in the fade's `Completed` handler so the
  property is back to a normal local value by the time `Dimmed` needs to touch it. See
  hard rule 8 in §3.10.

With this batch, animation polish (backlog item 2 as of the prior session) is complete —
no unwired transitions remain.

### 3.25 Ribbon horizontal scroll (tab strip + groups row)

When the window is too narrow for even the fully-collapsed groups — or for all the tabs — Office shows
left/right chevron buttons to scroll the overflow into view. Added the same to RibbonKit.

- **`Layout/RibbonScrollContentHost.cs`** — a `Decorator` that shows its single child clipped to the
  viewport and offset by a `TranslateTransform`, exposing `ExtentWidth`/`ViewportWidth`/`CanScrollLeft`/
  `CanScrollRight` (readonly DPs) and `ScrollLeft`/`ScrollRightCommand`. Key trick vs a stock
  `ScrollViewer`: `ConstrainChildWidth=true` measures the child at the **viewport** width (not
  infinity), so the adaptive `RibbonGroupsPanel` still reduces groups to fit FIRST; scrolling engages
  only when the fully-reduced row is *still* wider than the viewport. The tab strip leaves it off
  (`false`) so tabs keep natural size and scroll when too many. Mouse-wheel scrolls horizontally.
- **`Office2024.xaml`** — the `RibbonTabControl` template now wraps both the `TabPanel` (with the
  gliding `PART_TabMarker` INSIDE the scroller, so the marker scrolls in lockstep with its tab) and the
  `SelectedContent` groups presenter in a `RibbonScrollContentHost`, each with two rounded chevron
  `RepeatButton`s (`RibbonKit.ScrollLeftButton`/`RightButton`, `ControlCornerRadius` token). The buttons
  **overlay** the content edges (no layout space) so showing/hiding them can't reflow-oscillate; they're
  bound to the host's commands + `CanScroll*` via `BooleanToVisibilityConverter`. `PART_TabScroll` /
  `PART_ContentScroll`. Runtime feature — needs a Windows build to confirm layout + the marker-under-scroll.
- **Clamp fix — two iterations.** WPF's `Measure` clamps an element's reported `DesiredSize` to the
  width you pass it, which fights the "measure at viewport to force reduction, but still detect overflow"
  requirement.
  - *First attempt (groups clipped but no chevron → then chevron but groups stopped collapsing).*
    Measuring the groups row at the viewport clamped its reported width to the viewport, so the scroller
    never saw overflow. Switching to measure the child **unconstrained** made the chevron appear but the
    groups no longer reduced — reduction then read a viewport width off the ancestor scroller via
    `FindScrollHost`, and that walk returns **null during an items-host panel's `MeasureOverride`** (the
    visual parent chain isn't reliably connected mid-measure), so reduction fell back to infinite width
    and never fired.
  - *Robust fix (current).* Decouple the two concerns instead of doing both at measure time:
    `RibbonScrollContentHost` measures the constrained child **at the viewport width** again, so
    `RibbonGroupsPanel` reduces reliably against its own `availableSize.Width` (no ancestor walk needed).
    To recover the true width the clamp hides, the panel **pushes** its real (unclamped) total to the
    scroller via `RibbonScrollContentHost.ReportContentWidth(totalWidth)` at the end of its measure; the
    scroller uses that reported width as `ExtentWidth` instead of the clamped `child.DesiredSize.Width`.
    The panel resolves + caches the scroller at `Loaded` (tree fully connected → `FindScrollHost` works),
    falling back to a lazy resolve. The tab strip stays unconstrained (measured at infinity), so its
    overflow is visible directly and it needs no reporting. Net: reduce-then-scroll works — groups
    collapse to the viewport first, and only the leftover overflow scrolls.
- **Chevron button chrome.** The `RibbonKit.ScrollLeftButton`/`RightButton` styles give each button a
  1px `Ribbon.Border` outline so the overlaid buttons stand out against the ribbon/tab-strip background
  instead of blending in. (A `DropShadowEffect` was tried first but read as a heavy dark box against the
  light content area — dropped in favour of the clean border, which also gives flat themes below 2024 a
  themed outline with no shadow, for free, since `Ribbon.Border` is a per-theme token.)
- **Chevrons must return after visiting a non-overflowing tab.** The groups scroller's `ExtentWidth` is
  driven by `ReportContentWidth`, which `RibbonGroupsPanel` only calls from its own `MeasureOverride`.
  Switching tabs disconnects the old groups row from the single shared content scroller and reconnects
  the new one, but WPF reuses the reconnected panel's cached measure — so `MeasureOverride` (and the
  report) never runs, and the scroller keeps the previously shown tab's extent. Result: after visiting a
  tab that fits (chevrons hide), returning to an overflowing tab left the chevrons hidden because the
  scroller still saw the fitted extent (the fallback `child.DesiredSize.Width` is clamped to the
  viewport). Two fixes were tried before the one that stuck; the failures pin down the mechanism:
  - *Panel `InvalidateMeasure()` on `IsVisibleChanged`* — no effect. Re-runs the panel (which re-reports)
    but leaves every ancestor measure-valid at the same size, so the scroller never re-measures to READ
    the report or re-arranges to update the chevrons.
  - *Panel invalidates the whole chain up to the scroller on `IsVisibleChanged`* — also no effect,
    because `IsVisibleChanged` fires **too early**: at that instant the newly connected panel's parent
    chain hasn't reached the scroller yet, so the upward `FindScrollHost` walk returns null and only the
    panel gets invalidated. (Tell: the chevrons returned only after a 1px window nudge — a real size
    change is what forced the cached chain to re-descend — and minimize→expand always worked, because
    that toggles the whole content Border's visibility so the entire subtree re-measures.)
  - *Working fix:* drive it from **`RibbonTabControl.OnSelectionChanged`** (always fires on switch),
    `Dispatcher.BeginInvoke` at `Loaded` priority so the new groups row is realized under the scroller,
    then call `RibbonScrollContentHost.Refresh()` on the captured `PART_ContentScroll`. `Refresh()`
    invalidates measure across its **entire visual subtree** (walking down from the known scroller, so no
    fragile upward lookup and no timing race), which dirties every level and forces the one top-down
    re-measure the resize used to: the panel re-reports and the scroller reads it and recomputes
    `CanScrollLeft/Right`.

### 3.26 Modern context menus (ribbon-item + QAT right-click)

The right-click menus were stock WPF `ContextMenu`/`MenuItem` (created in code in `Ribbon.cs`), so they
rendered with the dated native menu chrome while the `RibbonMenuItem` dropdowns looked modern. Fixed with
styles that match the dropdown, in a **dedicated `Themes/Menus.xaml`** dictionary:

- `RibbonKit.ContextMenu` — rounded flyout `Border` (`PopupCornerRadius`, `ScreenTip.Border`,
  `ContentBackground`) with the same soft `DropShadowEffect` the dropdown popup uses. `HasDropShadow` is
  left **True** on purpose: that's what keeps the hosting popup's `AllowsTransparency` on (so the rounded
  corners + soft shadow render); the system shadow itself isn't drawn because the custom template omits
  `SystemDropShadowChrome`.
- `RibbonKit.MenuItem` — a `RibbonMenuItem`-style row: 24px icon/check gutter, header, submenu arrow +
  flyout, `Control.HoverBackground` on `IsHighlighted`, 0.4 opacity when disabled. A **check glyph** shows
  on `IsChecked` (the QAT placement items use it), sharing the gutter with the optional `Icon`.
- `RibbonKit.MenuSeparator` — a themed 1px line via `Group.Separator`.
- Wiring / **why a separate dictionary** (first attempt failed): the styles first lived in
  `Office2024.xaml` and were applied with `SetResourceReference(StyleProperty, "RibbonKit.ContextMenu")`
  — and the menu stayed native. `Office2024.xaml` is merged only into `Generic.xaml` (the assembly THEME
  dictionary); implicit RibbonKit control styles resolve from there via `DefaultStyleKey`, but a **keyed**
  resource in a theme dictionary is NOT reachable by a normal runtime lookup, and a `ContextMenu` (a
  PresentationFramework type) resolves its theme resources against PresentationFramework's theme, never
  RibbonKit's `Generic.xaml`. Fix: the styles live in their own `Themes/Menus.xaml`, which `Ribbon.cs`
  loads once by pack URI (`pack://application:,,,/RibbonKit;component/Themes/Menus.xaml`, cached static)
  and assigns the `Style` object directly to each menu (`ApplyModernMenuStyle`). The style's brushes are
  `DynamicResource`, so they still resolve — and re-theme — from the app-merged token set. Applied only to
  the ribbon's OWN two menus, not a host app's.
- Getting the per-item look onto the rows took a second correction. `ItemContainerStyle` throws at
  runtime — WPF applies it to the `Separator` items too (*"a style intended for MenuItem cannot be
  applied to Separator"*), so the earlier assumption that separators are skipped was wrong. A keyed
  `Style.Resources` also isn't a reliable way to reach the items. What works: `ApplyModernMenuStyle`
  injects the two item styles as IMPLICIT entries straight into the menu's own `Resources` —
  `menu.Resources[typeof(MenuItem)] = RibbonKit.MenuItem` and
  `menu.Resources[MenuItem.SeparatorStyleKey] = RibbonKit.MenuSeparator` — which every `MenuItem`
  (including submenu items) and `Separator` in the menu subtree resolves.

### 3.27 Office 2010 ("Blue") theme — the first gradient theme

A fourth token set, `Themes/Tokens.Office2010.xaml`, added as a pure token dictionary (no new
templates — same 65 keys as the other themes, verified identical). Wired end-to-end: `RibbonTheme.Office2010`
enum member, an `Office2010` case in `ThemeManager.ApplyAccentOverrides`, and an "Office 2010" button
(+`OnApplyOffice2010`) in the showcase Theme group.

**Why it's different from every prior theme:** 2010 is the first NON-flat look, and its identity is
**gradients**. The three earlier themes use `SolidColorBrush` for every surface; 2010's chrome tokens
are `LinearGradientBrush`es (vertical, `StartPoint="0,0" EndPoint="0,1"`):

- **Silver-blue window/ribbon chrome** — `TitleBar.Background`, `Ribbon.Background` (tab strip band),
  and `Ribbon.ContentBackground` (the groups area) are light blue-grey vertical gradients (lighter top,
  darker bottom — the classic 2010 ribbon shading).
- **Amber/gold glossy highlights** — the iconic 2007/2010 "hot" states: `Control.HoverBackground` is a
  warm gold gradient, `PressedBackground` a deeper gold, `Checked*` a gold toggled fill. These read as
  glossy warm accents against the cool blue chrome. Unselected **tab** hover gets a lighter amber glow.
- **Dark-blue tab labels** (`TabStrip.Foreground`/`Tab.SelectedForeground` = `#15428B`).
- **Connected (outlined) active tab** — reuses the 2013 mechanism: `Tab.SelectedBorderBrush` +
  `TabSelectedBorderThickness=1,1,1,0`, with a light gradient fill that merges into the ribbon body top.
  Underline tokens are `Transparent` (fills, not underlines).
- **Solid blue gradient File button** — `ApplicationButton.Background` is a blue gradient with white text
  (`Foreground=#FFFFFF`), a brighter blue gradient on hover. A tab-row button (small `ApplicationButtonMargin`),
  not the full-height flush block of 2013.
- **Gently rounded corners** (2-3px) — softer than the flat themes (0), subtler than 2024 (4-8px). A faint
  ribbon-body shadow (`Opacity=0.12`) separates it from the document — not the floating card of 2024.

**Key safety property (why gradients "just work"):** no code animates a token brush's `Color`. Every
transition targets `UIElement.Opacity` — `RibbonMotion.FadeWash` fades a wash *layer*'s opacity (the wash
layer's `Background` is the token brush, untouched), and `PlayThemeCrossfade` dips the tab control's
opacity. So a `LinearGradientBrush` behind a wash/at a key is never cast to `SolidColorBrush` or fed to a
`ColorAnimation`. (Confirmed by grep: `RibbonMotion.cs` only ever calls `BeginAnimation(UIElement.OpacityProperty, …)`.)

**Accent handling:** `ApplyAccentOverrides`' `Office2010` case maps a custom accent onto the File
button (`ApplicationButton.Background` + hover), like 2013 — a custom accent replaces the blue gradient
with a solid accent block. `SelectedForeground` is intentionally *left* at the theme's dark blue (a
custom accent doesn't tint the connected selected-tab label, which reads better on a light tab). When
no custom accent is set (the default), the theme's own blue gradient File button and amber toggled
fills show. The Colored-Title-Bar toggle uses the generic (non-2019) branch: an accent title bar with
white caption text; the gradient strip below stays (2019's strip-coloring special-case doesn't apply).

**Post-feedback refinements (first visual pass on Windows):**

- **Glass "gel" gradients.** The first gradients read flat (2-stop, low contrast). The button-state
  tokens (`Control.HoverBackground`/`PressedBackground`/`CheckedBackground`/`CheckedHoverBackground`) and
  the File-button tokens (`ApplicationButton.Background`/`HoverBackground`) are now 4-stop Aero gels: a
  bright top highlight, a **hard crease at the midpoint** (two `GradientStop`s at the same `Offset="0.5"`,
  giving an instant color step — the glossy split), then a richer lower half. Pressed inverts (darker at
  top = recessed). The washes already bound these keys (`HoverWash`/`PressWash`/`CheckWash` Backgrounds),
  so this was a pure token change.
- **Connected active tab.** The tab strip (`Grid.Row=0`) and body (`Grid.Row=1`) are stacked with no
  overlap, so the body's 1px top border drew an unbroken line under the selected tab. Fixed token-only:
  2010's `TabStripMargin` bottom is `-1`, dropping the strip 1px so the selected tab overlaps the body's
  top border; the selected fill (`Tab.SelectedBackground`, bottom stop = the body's top color `#F6F9FC`)
  covers that 1px line seamlessly, while unselected (transparent) tabs leave it showing. The tab's
  top+side border (`SelectedBorderBrush`, `TabSelectedBorderThickness=1,1,1,0`) meets the body border at
  the corners — the "cut into the body" outline.
- **File-button width is now a token.** The width was hardcoded `Padding="14,7,14,9"` on the File button's
  `Chrome` in the shared template. Tokenized as `RibbonKit.Metrics.ApplicationButtonPadding` (one template
  edit) and added to ALL four theme files (66 keys each now): 2024 keeps `14,7,14,9`; 2019 `20,7,20,9`,
  2010 `22,7,22,9`, 2013 `24,7,24,9` (the pre-2024 File tabs read as broader blocks).

**Second feedback pass (reference images provided):**

- **Glass was the 2007 look, not 2010.** The 4-stop hard-crease "gel" was Office 2007's aggressive
  gloss. Real 2010 is a SMOOTH subtle gradient + a thin **border**. Reworked: 2010's
  hover/press/checked and File-button gradients are now smooth (no duplicate-offset crease), and a
  new set of border tokens draws the defining edge — `Control.{Hover,Pressed,Checked}Border` +
  `ControlHighlightBorderThickness` (gold, 1px in 2010; Transparent/0 elsewhere) on the wash layers,
  and `ApplicationButton.Border` + `ApplicationButtonBorderThickness` (blue, 1px in 2010) on the File
  `Chrome`. All four theme files carry the 6 new keys (72 keys each now); the template wires
  `BorderBrush`/`BorderThickness` onto `HoverWash`/`PressWash`/`CheckWash` and the File Chrome.
- **Accent no longer flattens 2010.** `ApplyAccentOverrides` used flat `Frozen(Mix(...))` solids,
  which replaced 2010's gradients when a custom accent was set. Fixes: (1) the toggle/checked
  highlight is now SKIPPED for 2010 (authentic — 2010's highlight is always amber regardless of the
  color scheme; the accent recolors chrome, not the hot state), so it keeps its amber gradient; (2)
  the File button is re-derived as a **gradient** via a new `Gel(Color)` helper (a 3-stop vertical
  gel: lighter top, base middle, darker bottom) plus a matching border, instead of a flat solid.
  `ApplicationButton.Border` was added to `AccentOverrideKeys` so it clears on theme switch.
- **Backstage translucency + blur (new).** `Backstage.Translucent` already existed (Mica reveal);
  extended it into a frosted-acrylic effect: when a translucent backstage opens, `Ribbon` applies a
  strong Gaussian `BlurEffect` (radius 34) to the adorned root (the content behind) and restores the
  prior effect on close (`ApplyBackstageBlur`/`ClearBackstageBlur`). The backstage stays sharp
  because it lives in the adorner layer (a sibling visual), not under the blurred root. The
  translucent brushes were made genuinely see-through (`ContentTranslucent` 90%→70%) so the blur
  reads. Showcase gained a **Translucent Backstage** toggle (`OnToggleBackstageTranslucent` →
  `ShowcaseBackstage.Translucent`).

**Below-tabs backstage — DROPPED for a third backstage DESIGN instead.** The below-tabs *layout*
(repositioning the overlay under the tab strip) was judged not worth the structural cost. Instead a
third `RibbonBackstageDesign` value, **`Classic2010`**, was added: same solid accent nav column as
`Classic` (white text), but the SELECTED item is a glossy blue "glass" marker
(`Backstage.ItemSelectedGlass`, a gradient) rather than the flat Classic fill. So there are now three
backstage looks — Classic (2013 flat accent), Modern (2024 light rail), Classic2010 (blue glass). One
`MultiTrigger` in the `BackstageTabItem` template (Design=Classic2010 + IsSelected) does it; the
Backstage template itself is unchanged (Classic2010 inherits Classic's accent column). The glass
marker tracks a custom accent via `ThemeManager` (`Gel(accent)`). Showcase: the single Modern/Classic
toggle was replaced with three explicit design buttons (2013 Rail / 2024 Rail / 2010 Glass) →
`OnSelectBackstageDesign` (reads the button `Tag`).

**Glassy OK button.** The options-dialog primary (OK) button now borrows the File-button glass via new
theme-aware tokens `Dialog.PrimaryBackground`/`Dialog.PrimaryBorder` (`OptionsDialogPrimaryButtonStyle`
binds them): a glossy blue gel in Office 2010, a flat accent elsewhere — both tracking a custom accent
through `ThemeManager` (the shared branch sets flat accent; the Office2010 case swaps in `Gel(accent)`).
All four theme files carry the 3 new keys (75 keys each).

**Third feedback pass (reference: a real 2010 glass button):**

- **Tab connect (the real fix).** The round-2 `-1` tab overlap never showed because the ribbon body
  (`ContentHost`, declared after the tab strip) painted OVER the tabs. Fixed two ways: the tab-strip
  row grid gets `Panel.ZIndex="1"` (paints above the body), and 2010's overlap moved from the tab
  strip to the body — `ContentMargin`/`ContentMarginQatBelow` top is now `-1`, so the body slides up
  1px UNDER the tabs (moving the body, not the tabs, avoids the tab scroll-host clipping the overlap).
  The higher-z selected tab's fill then covers the body's top border → connected.
- **Tab hover border.** Tabs now get the gold hover border like buttons (hover trigger sets
  `HeaderChrome` `BorderBrush`=`Control.HoverBorder` + `BorderThickness`=`ControlHighlightBorderThickness`;
  Transparent/0 in other themes). A selected+hovered tab keeps its connected border (selected trigger
  wins, later in the template).
- **Glass rebuilt to match the reference (inner rim + specular).** The reference 2010 button has an
  inner light rim and a small specular reflection low on the button. Two changes: (1) every 2010 glass
  gradient is now a smooth "valley" — light top (top inner glow), matte darker middle, then a LIGHTER
  bottom (the specular) — no hard crease; (2) a new `Control.InnerGlow` token (semi-white `#88FFFFFF`
  in 2010, Transparent elsewhere) draws a nested inner border, inset by the outer border, on the
  button/toggle washes and the File/OK chrome. `ThemeManager.Gel()` was updated to the same profile so
  accent-derived gels match. (76 keys/theme file now.)
- **2010 backstage nav reworked (Classic2010).** Per feedback: the nav column is the ribbon's blue
  GRADIENT (`Ribbon.Background`, not a solid accent); item text is DARK, turning white only when
  selected; the selected item is a glossy accent glass box (`ItemSelectedGlass`) WITH a border
  (`ApplicationButton.Border`). Hover is a subtle light wash so the dark text stays legible.

**STILL PENDING (fast follow-up) — propagate the glass to dropdown / split / combo / menu items.**
Those controls (`RibbonDropDownButton`, `RibbonSplitButton`, `RibbonComboBox` + `ComboBoxItem`,
`RibbonMenuItem`) render hover/press by swapping a `Chrome` border's `Background` only — no border. The
fix is mechanical (add `BorderBrush`=`Control.{Hover,Pressed}Border` + `BorderThickness`=
`ControlHighlightBorderThickness` at each hover/press trigger, ~12 sites, all token-based so they inherit
the confirmed recipe). Deliberately deferred so the glass recipe is confirmed on the prominent buttons
first rather than stamped across all sites blind.

**Fourth feedback pass ("almost there"):**

- **Tab connect now applies on theme switch (root cause found).** The connect (themed negative body
  margin + tab-strip `ZIndex`) is correct, but it only re-PAINTED after a layout pass — and the user's
  clue ("it merges when I hover another tab") pinpointed why: the round-5 tab hover border changes a
  tab's size, forcing the re-arrange that reveals the overlap. On a theme switch nothing did. Fix:
  `Ribbon.OnThemeConfigurationChanged` now calls `InvalidateArrange()` + `UpdateLayout()` on the tab
  control after the swap, so the active tab merges immediately.
- **Glass propagated to the held-back controls.** The gold glass border (`Control.{Hover,Pressed,
  Checked}Border` + `ControlHighlightBorderThickness`) is now applied at the hover/press/checked/selected
  triggers of `RibbonDropDownButton`, `RibbonSplitButton` (primary + chevron), the collapsed-group
  button, the gallery scroll buttons, `RibbonMenuItem`, `ComboBoxItem`, and the options-dialog nav
  (`RibbonOptionsPage`) + its reorder buttons. All reuse the same tokens, so they inherit the confirmed
  recipe (the specular fill was already there via `Control.*Background`). NOTE: the inner-glow *rim* is
  still only on the wash-based main buttons + File/OK; these Chrome-based controls get the gradient +
  outer gold border (the border shows on hover via a trigger — a possible 1px content nudge on
  left-aligned items like menu rows; reserve static border space if it reads as jitter).
- **Application button mouse-down state.** Added an `IsPressed` trigger to the File button showing a
  new `ApplicationButton.PressedBackground` (a deeper recessed blue gel in 2010, flat elsewhere,
  accent-tracked for 2010/2013), so the click registers on press, not only when the backstage opens.
- **Classic2010 nav hover border.** The 2010 backstage nav hover gained a hairline `#66FFFFFF` border
  in addition to its light wash.

**Fifth feedback pass — jitter + the REAL connect mechanism (user diagnosed it):**

- **Root cause of both bugs was the trigger-based border.** Setting `BorderThickness` 0→1 on hover
  changes content layout → the 1px "jitter" the user saw on every hover. AND that jitter is what was
  accidentally connecting the active tab: hovering any tab re-arranged the strip, which dropped the
  active tab 1px into the body. So the connect was never my body-overlap — it was the jitter.
- **Jitter fix — reserve the border space.** Every glass hover `Chrome` now carries a STATIC
  `BorderThickness="{DynamicResource ControlHighlightBorderThickness}"` (1 in 2010, 0 elsewhere) with
  no brush at rest; the triggers only swap `BorderBrush`, so hovering never changes size. Applied
  programmatically to exactly the 9 glass controls (dropdown, split ×2, collapsed button, gallery
  scroll button, gallery expander, menu item, combo item, options-nav) — identified by their use of
  `Control.HoverBackground` and the ABSENCE of a wash (wash-based buttons, caption buttons, and the
  backstage nav item were correctly skipped). The tab's `HeaderChrome` got the same static thickness.
- **Connect — do what actually works: drop the ACTIVE TAB, not the body.** Reverted the body-up
  `ContentMargin -1`. New `TabSelectedMargin` token (`0,0,0,-1` in 2010, `0` elsewhere) is applied to
  the selected `HeaderChrome`, so the active tab permanently extends 1px into the body; the tab-strip
  `Panel.ZIndex="1"` paints it over the body's top border, and the selected fill (bottom stop = body
  top color) hides the seam. This is the mechanism the user observed working via the jitter, now made
  permanent and jitter-free. (78 keys/theme file.)

Note: selecting a tab still changes its border from uniform to `1,1,1,0` (+ the -1 drop) — a 1px
settle that rides the existing tab-switch slide animation, so it shouldn't read as jitter.

**Sixth feedback pass:**

- **Active tab extended to -2.** `TabSelectedMargin` (2010) is now `0,0,0,-2` so the tab fully overlaps
  and hides the body's top border (the -1 still left a hairline).
- **Backstage nav hover jitter fixed.** The `BackstageTabItem` `Chrome` was skipped by the earlier
  reserve pass (it uses `Backstage.*` brushes, not `Control.HoverBackground`). Gave it a static
  `BorderThickness="1"` so the Classic2010 hover/selected border triggers only swap the brush — no
  jitter. (Invisible 1px inset in Classic/Modern, which have no nav border.)
- **Glass back button (Classic2010).** The backstage back button becomes a filled blue "glass" disc
  (`Dialog.PrimaryBackground` + `Dialog.PrimaryBorder`, same as the OK/File button, accent-tracked) with
  a WHITE arrow, via a `controls:Backstage.Design == Classic2010` trigger (the outline-circle look stays
  for Classic/Modern). Hover lightens the disc (white hover wash).

**Seventh pass — back-button click, NuGet packaging, and a DEFERRED tab-connect bug:**

- **Tab connect DEFERRED — `TabSelectedMargin` has NO effect at all.** The user reports setting it to
  -1, -2, even -5 changes nothing; they reverted it to 0. So the selected `HeaderChrome` `Margin` setter
  is not being applied (or is overridden / the tab is size-constrained so the margin doesn't move it).
  Next session: investigate WHY the IsSelected trigger's `Margin` doesn't move the tab — candidates: the
  `HeaderChrome` is stretched by its parent Grid so a bottom margin can't extend it; the tab is clipped
  by `PART_TabScroll`; or the trigger is losing to another setter. A different connect approach may be
  needed (e.g. a dedicated connector rectangle drawn over the seam, or restructuring the tab/body layout).
  Bundle this with the Office 2007 theme and dark mode (all three are next-session work).
- **Back button click.** The backstage back button gained an `IsPressed` trigger (a recessed dark wash,
  last in the trigger list so it wins over hover and the Classic2010 white wash while held).
- **NuGet packaging wired (library + designer tools).** `RibbonKit.csproj` now bundles the design
  assembly: a build-only `ProjectReference` to `RibbonKit.Design` (`ReferenceOutputAssembly=false`,
  `SkipGetTargetFrameworkProperties=true` — builds the net472 `RibbonKit.DesignTools.dll` without a
  runtime reference) plus a `TargetsForTfmSpecificContentInPackage` target that packs it into
  `lib/<tfm>/Design/` for each TFM. With the already-packed `tools/VisualStudioToolsManifest.xml`,
  `dotnet pack src/RibbonKit/RibbonKit.csproj -c Release` yields a package that gives consumers the
  toolbox items + right-click design-time editor. The package repository metadata points to the
  public RibbonKit GitHub repository. See `RibbonKit.Design/SETUP-DESIGNTOOLS.md` → "NuGet packaging".

**Eighth feedback pass — continuous top chrome + complete button-state glass (2026-08-02):**

- **The title/tab gradient seam was real.** `RibbonWindow` paints `TitleBar.Background` while the
  nested `Ribbon` separately paints `Ribbon.Background`; WPF normalizes each gradient inside its own
  element, so 2010's old title-bottom `#CFDEEE` abruptly reset to strip-top `#E4EDF7`. The light and
  Black palettes now make the title's final stop exactly equal the strip's first stop. The two bands
  remain independently tokenized (colored-title-bar behavior is unchanged) but read as one continuous
  ramp at their physical seam. A contract test locks the matching endpoints in both variants.
- **Every released 2010 command highlight now has the strong lower inner glow from the reference.**
  The common hover/checked gradients keep their smooth matte middle, move the former light foot to
  offset `0.9`, and finish at `1.0` with a narrow near-white specular stop. This is token-only, so it
  reaches normal/toggle buttons at every size, QAT proxies, dropdown/split halves, collapsed-group and
  gallery buttons, plus the existing menu/combo consumers without duplicating templates or changing
  `BorderThickness` (therefore no return of the 1px hover jitter).
- **Pressed is recessed, not glowing.** The live reference correction showed that the near-white foot
  belongs to hover and checked only. `Control.PressedBackground` and
  `ApplicationButton.PressedBackground` are three-stop ramps again, the normal/toggle `PressWash`
  layers no longer draw `Control.InnerGlow`, and custom-accent 2010 pressed states use `PressedGel(...)`.
- **The blue File application button uses a faint radial inner rim, not a white band.** Rest, hover,
  pressed, and open Backstage all remain smooth three-stop gradients without a uniform bright foot.
  A File-specific `ApplicationButton.InnerGlow` radial brush supplies the low-alpha rim and localized
  lower-center bloom seen in the reference; the pressed trigger hides it. Custom accents use
  `ApplicationButtonGel(...)` for rest/hover/open and `PressedGel(...)` for pressed, so neither path
  recreates the fluorescent lower stripe. The other generations define the same rim token as transparent.
- **Deterministic coverage.** `Office2010ThemeContractTests` checks the light/Black seam, the bright
  released-state feet, their deliberate absence from pressed and File-body ramps, the scoped radial
  File rim, and every shared ribbon-button template family. A focused
  `office2010-button-states-100` approved PNG renders the open File button, a checked toggle, an open
  dropdown, and both halves of an open split button. It complements the 40-image matrix and RTL smoke,
  bringing the approved total to **42 images**; live `IsMouseOver` still requires the Windows pass.

**Ninth feedback pass — Classic2010 Backstage shell depth (2026-08-02):**

- **The selected page stays square and uses the reference's concentrated blue glow.** The oversized
  triangular experiment was removed. `Backstage.ItemSelectedGlass` is now a four-stop radial gradient
  centered slightly left and below the row's midpoint, producing the bright core and darker side edges
  visible in the Word crop. Custom Office 2010 accents derive the same shape through
  `BackstageSelectionGlow(...)` instead of falling back to the generic vertical `Gel(...)`.
- **The white content sheet now casts a conventional shadow.** `ContentArea` receives a tokenized
  left-casting `DropShadowEffect` in `Classic2010`; the painted edge-gradient strip was removed. The
  effect is active in the light and Black 2010 palettes and zeroed in every other palette. The existing
  full-window overlay, back button, page layout, selection behavior, and animation remain unchanged.
- **Deterministic coverage.** Two template/token contract tests lock the square full-width selection,
  radial glow, drop-shadow trigger, and ten-palette effect parity. A new
  `office2010-backstage-shell-100` approval renders
  the real `Backstage` and selected page end-to-end, bringing the visual total to **43 images**.

**Tenth feedback pass — complete 2007/2010 Black application menus (2026-08-02):**

- **The blue bars and light footer buttons were missing dark overrides.** The 2007/2010 dark overlay
  dictionaries previously replaced only five application-menu resources; `FrameRim`, top/footer bands,
  nav/pane surfaces, separators, and footer-button fills still fell through to the light blue base
  palette. Both Black variants now own the complete 14-surface menu palette. Office 2007 retains its
  hard-crease gradients while 2010 uses smoother two-stop ramps.
- **Menu text is now scoped independently from the hybrid ribbon.** Both historical Black variants
  intentionally keep their silver command surface and dark `Text.Primary`, so darkening the menu while
  continuing to consume global text tokens would make its labels unreadable. New application-menu
  foreground, secondary-foreground, and heading-foreground tokens are defined in all ten palettes and
  consumed throughout the menu templates. The Showcase Recent Documents heading and rules now use the
  same tokens instead of fixed light-theme colors.
- **Deterministic coverage.** Three logic contracts require complete dark surface ownership, ten-palette
  foreground parity, and template isolation from global text tokens. Focused 100%-scale snapshots cover
  the real 2007 Black and 2010 Black menu shells, bringing the approved total to **45 images**.

At that point this batch had not yet been built or visually checked on Windows; the later verification
record in §5 supersedes that historical status.

### 3.28 Backstage page-text colour + ribbon focus (RichTextBox) — 2026-07-21

Two fixes in `Themes/Office2024.xaml` (the shared template dict for all themes).

- **Backstage page content was white/invisible on Office 2010 & 2013 (now working).** The page
  content shown for the selected tab inherits the SELECTED `BackstageTabItem`'s `Foreground`, which
  was `#FFFFFF` for the Classic (2013) and Classic2010 (2010) selected states → white text on the
  light content area. A `TextElement.Foreground` pin on the content area did NOT win (content follows
  the item, not the content area). Modern (2024) hid the bug because its selected-item foreground is
  the accent, not white. **Fix — decoupling:** the nav item's text + icon now come from named
  template elements — `NavText` (a StackPanel carrying `TextElement.Foreground`) and `NavIcon`
  (Rectangle `Fill`) — driven per design/selection by the triggers; the container `Foreground` no
  longer colours the nav, so it's free to be the content colour the page inherits. Template triggers
  still override the `#FFFFFF` defaults on NavText/NavIcon (template triggers outrank template
  attribute values). **The container `Foreground` is set to `RibbonKit.Brushes.Accent`** (user's
  choice — page text reads in the theme accent, matching how 2024 looked originally); swap to
  `Text.Primary` for plain dark page text.
- **Ribbon was stealing keyboard focus from the document (e.g. a RichTextBox).** Office keeps focus
  in the document when a ribbon command is clicked, so the command applies to the live selection.
  `RibbonDropDownButton`/`RibbonSplitButton` were already `Focusable=False`; these were `True` and
  stole focus — now `False`: **`RibbonButton`, `RibbonToggleButton`, the collapsed-group
  `PART_CollapsedButton` toggle, `RibbonTab`, and the `ApplicationButton` (File)**. Invocation is by
  click / KeyTip (automation Invoke) — neither needs focus; keyboard ribbon access is Alt/KeyTips,
  not the focus tab-order, so nothing is lost. **Left focusable by design** (they legitimately need
  focus): `RibbonComboBox`'s editable box, the galleries, and `RibbonMenuItem` popups. For those the
  app should set `RichTextBox.IsInactiveSelectionHighlightEnabled="True"` (keeps the selection visible
  if focus does move) and/or restore focus to the document after the action; galleries can be made
  non-focusable too but that risks the popup/hit-testing work — do it as a separate tested change.

Both unbuilt in the sandbox — pending the user's visual check on Windows.

### 3.29 Connected-tab body-border cut (2010/2013) — the "notch" — 2026-07-23

Completes the tab-connect work (§3.27 deferred item): the selected tab now cuts a tab-wide gap
into the ribbon BODY's top border, so it opens seamlessly into the body like real Office 2010/2013.
The ConnectFoot alone couldn't do it, and neither could any ZIndex — two hard WPF constraints:

- **`PART_TabScroll` clips its subtree** (`RibbonScrollContentHost` sets `ClipToBounds=true` — it
  must, it's a scroller). Anything inside the tab strip (ConnectFoot included) is cut off at the
  strip's bottom edge and can NEVER paint onto the body's border below it.
- **`Panel.ZIndex` only orders siblings of the same panel.** The strip (row 0 of the
  RibbonTabControl template) and the body (`ContentHost`, row 1) are separate branches, so no
  ZIndex placed anywhere inside either subtree can reorder one against the other. (Setting ZIndex
  on the body "did nothing" for exactly this reason — the strip already painted above via row 0's
  `Panel.ZIndex=1`; there was no sibling relationship left to reorder.)

**Mechanism — draw the cut body-side instead** (`PART_ConnectNotch`, in `Themes/Office2024.xaml`):
a 1px-tall Border in ROW 1 of the RibbonTabControl template, declared AFTER `ContentHost` so plain
declaration order paints it over the body's top border. Top-aligned (ContentMargin is 0 in both
connecting themes, incl. QAT-below variants), `Width=0` at rest, `IsHitTestVisible=False` — pure
render, never affects layout. Its Background is the new token `RibbonKit.Brushes.Tab.ConnectNotch`
(2010 `#F6F9FC` = body gradient's top stop; 2013 `#FFFFFF`; Transparent in 2019/2024 — the code
gates on the brush being a visible colour, same pattern as the marker's underline gate). Its
`BorderThickness=1,0,1,0` with `Tab.SelectedBorderBrush` continues the active tab's outline down
through the cut, closing the corners.

**Positioning** (`RibbonTabControl.UpdateConnectNotch`): `TransformToVisual` (NOT
TransformToAncestor — the notch's parent is a sibling branch of the tab, not an ancestor) maps the
selected tab into the notch parent's space, scroll transform included; sets `Width` +
`PART_ConnectNotchTranslate.X`. Clamped to `PART_TabScroll`'s viewport so a selected tab scrolled
out of view doesn't cut an orphan gap (hidden under ~2px). Deliberately NOT animated — the
connecting themes' tab chrome snaps on selection, so a gliding gap would read as a detached slit.
Update sites: ctor SizeChanged/Loaded, OnApplyTemplate (dispatcher, Loaded priority),
OnSelectionChanged, and a new `RibbonScrollContentHost.OffsetChanged` event (raised per frame of a
scroll glide; handler updates immediately — 1-frame lag, transform applies next arrange — plus a
Loaded-priority re-run to correct the final resting position). Theme swaps fire no
selection/size event, so `Ribbon.OnThemeConfigurationChanged` now calls
`tabControl.RefreshSelectionVisuals()` (internal: re-places marker + notch, no animation).
Minimize: notch `Visibility` is BOUND to `ContentHost.Visibility`, so it collapses with the body
(during the ~150ms minimize slide the 1px sliver lingers until the collapse callback — accepted).

Files: `Themes/Office2024.xaml` (notch element + corrected ConnectFoot comments), all four
`Tokens.*.xaml` (ConnectNotch brush), `Controls/RibbonTabControl.cs` (UpdateConnectNotch +
RefreshSelectionVisuals + wiring), `Controls/Ribbon.cs` (theme-swap refresh),
`Layout/RibbonScrollContentHost.cs` (OffsetChanged event).

Unbuilt in the sandbox — pending the user's visual check on Windows (watch: 1px alignment at
125%/150% DPI, tab-strip scroll with a selected tab at the viewport edge, theme switching
2024↔2010↔2013, minimize/restore).

### 3.30 Backstage Mica pass-through (Modern/2024) — hide, don't blur — 2026-07-23

The translucent Modern backstage never showed Mica — only a blur of the app content behind it.
Root cause (fundamental, not tunable): **the DWM composites Mica BENEATH the window and only
through pixels the window never painted.** The old approach kept the ribbon/document rendering
under a BlurEffect; a blurred pixel is still a painted pixel, so it occluded the material no
matter the backstage's alpha. User-proposed fix (correct): stop rendering the content behind the
backstage entirely, let the nav rail be a plain alpha wash over raw Mica, keep the page content
solid.

Mechanism:

- `Ribbon.HideContentBehindBackstage` (replaces `ApplyBackstageBlur`): when a `Translucent`
  backstage opens, the adorned root fades to **Opacity 0** (Backstage animation timing; snap when
  animations off) and gets `IsHitTestVisible=false` (zero-opacity elements still receive input —
  WPF hit-testing ignores opacity; the old blur never disabled it). Opacity, not Visibility: it's
  animatable and cannot disturb the adorner layer — the backstage lives in the AdornerDecorator's
  adorner layer, a SIBLING branch of the root, so the root's opacity doesn't touch it.
- `RestoreContentBehindBackstage` runs at the START of the exit slide (old blur cleared on
  completion): the backstage slides out through its logical leading edge and must reveal live
  content, not bare
  backdrop. A reopen-while-closing simply re-hides; saved state (opacity/hit-test) is captured
  only on the first hide so a mid-fade reopen can't corrupt it.
- Template: the `Translucent` trigger now only clears `RootGrid`'s fill; the
  **`ContentTranslucent` brush + its ContentArea setter are REMOVED** — the page area stays
  solid (crisp text), only the nav rail (`NavBackgroundTranslucent` #B8F5F4F3, unchanged)
  reveals the material. No blur anywhere: Mica itself provides the softness.
- Behind the overlay everything is already transparent in backdrop mode (showcase Mica toggle:
  `Window.Background`/`MainContentArea` transparent + `SetTitleBarBackdrop`), so hiding the root
  is sufficient — no window-template changes.
- No-backdrop case (Win10 / Mica off) intentionally simple (user's call): Translucent still
  hides the content; the rail sits on plain window white. The frosted-blur fallback was
  deliberately dropped.
- App side: turn on BOTH the window backdrop (MicaHelper) and `Backstage.Translucent` — the
  showcase currently treats them as independent toggles (its Mica handler comment still says
  "Backstage stays opaque"); flip both to see the effect. Classic/Classic2010 designs are
  unaffected in practice (their nav is an opaque accent/gradient and the content is solid, so
  nothing reveals the material — by design, those generations predate Mica).

Unbuilt in the sandbox — pending the user's visual check on Windows (watch: open/close animation
— content fade-out on open, instant reveal on close; reopen mid-close; Esc; clicking during the
exit slide; Translucent+Mica vs Translucent-only).

**Addendum (same day, after user verification — Mica pass-through WORKS):** the user zeroed the
rail's alpha (`NavBackgroundTranslucent` → `#00F5F4F3`, pure Mica rail, Office look) and two
follow-ups landed: (1) **Translucent nav item states** — the opaque grey `Modern.ItemHover`/
`ItemSelected` fills read as solid cards over raw Mica, so the translucent Modern rail now uses
black ALPHA washes instead: new brushes `Modern.ItemHoverTranslucent` (#12000000, ~7%) and
`Modern.ItemSelectedTranslucent` (#1F000000, ~12%), applied by two MultiDataTriggers in the
BackstageTabItem template (conditions: attached `Backstage.Design`=Modern via RelativeSource Self
— in template triggers Self is the templated control — + ancestor `Backstage.Translucent` +
Chrome IsMouseOver / Self IsSelected). They override ONLY Chrome.Background; the opaque triggers'
SelBar/accent-text setters still apply. Placed after the opaque Modern triggers so they win.
(2) **2024 default accent aligned with the older themes** — see §3.31.

### 3.31 Office 2024 default (Auto) accent aligned to #2B579A — 2026-07-23

2024's token palette defaulted to Fluent blue #0F6CBD while 2019/2013 default to Office blue
#2B579A (2010 uses #1E5A9C), so switching generations jumped the accent. Per user request 2024
now defaults to **#2B579A** everywhere the old value appeared in `Tokens.Office2024.xaml`:
`Accent`, `Tab.SelectedUnderline`, `Tab.SelectedForeground`, `Dialog.PrimaryBackground/Border`,
`MdiChild.ActiveCaptionBackground/ActiveBorder` — plus the toggled washes recomputed with the
SAME formula ThemeManager uses for custom accents (Mix(accent, White, 0.82/0.72)):
`Control.CheckedBackground` #CDE0F3→#D9E1ED, `Control.CheckedHoverBackground` #BDD5EC→#C4D0E3,
so the default and an explicitly-set #2B579A accent render identically.
`ThemeManager.DefaultAccent` (last-resort fallback in `EffectiveAccent`) matched to #2B579A too.
NOT changed: the contextual-tab tints (`Tab.Contextual*`, decorative), the showcase's accent
gallery "Blue" swatch (#0F6CBD — still a valid pick, just no longer the default), and other
themes' tokens. Unbuilt in the sandbox — pending the user's Windows check (watch: 2024 tab
underline/File-button/OK-button hue, toggled button washes, MDI active caption).

**Session gotcha (tooling):** re-staging an already-staged device file can silently serve the
STALE cached copy at `/mnt/user-data/uploads/...` (old mtime/content) while the tool response
reports the CURRENT device size/mtime. If a freshly re-staged file looks reverted, compare the
response's byte size against the expected committed size before concluding anything — this
session nearly misdiagnosed a full working-tree revert that way.

### 3.32 Modal tabs (Print-Preview mode) — Phase 7 P7.1 — 2026-07-27

`RibbonTab.IsModal` marks a tab as *eligible*; the app enters and leaves the mode with
`Ribbon.EnterModal(tab)` / `ExitModal()`. Entering hides every other tab and the application (File)
button, blocks minimize and the backstage, and leaves the QAT alone — Word's Print Preview
behaviour. `CanClose` (default true) puts a close affordance at the end of the tab strip, labelled
with `CloseButtonText` when set, bound to `Ribbon.ExitModalCommand`. Enter and exit each raise a
cancellable `-ing` event plus an `-ed` event, carrying a `RibbonModalReason`
(Application / CloseButton / TabRemoved).

State lives in `Controls/RibbonModalScope.cs`. It hides the other tabs with plain `Visibility`,
which is what makes the feature cheap: the ribbon's existing selection guard
(`OnTabIsVisibleChanged` → `FindFirstVisibleTab`) and `KeyTipService`'s visible-tab filter then do
the right thing with no special-casing, honouring architecture §8's rule that core layout must never
know about modal tabs. Order matters on entry — select the modal tab BEFORE collapsing the others,
or the selection guard briefly promotes the wrong one.

**Pitfall 1 — visibility is also PERSISTED.** `RibbonCustomizationSerializer` captured
`tab.Visibility` per tab, so saving ribbon state while modal wrote every other tab as hidden and
restored a one-tab ribbon on the next run. The scope records each tab's pre-modal value and
publishes it through `Ribbon.GetAuthoredVisibility`, which the serializer now reads instead of the
live property. `SetAuthoredVisibility` is the matching setter for app state (a contextual tab's
context) changing mid-mode.

**Pitfall 2 — block by REVERTING, not by coercing.** Minimize and the backstage are blocked inside
their property-changed callbacks with `SetCurrentValue(..., false)` behind `_suppressMinimizeChange`
/ `_suppressBackstageChange` guards. A `CoerceValueCallback` looks tidier but leaves a stale `true`
*base* value that springs back the moment modal mode ends.

**Pitfall 3 — `Tabs.Clear()` reports a Reset with no `OldItems`**, which is exactly how the
serializer rebuilds the collection; `OnCollectionReset` reconciles modal state against the new
contents.

Template: the right end of the tab-strip row is now a horizontal `StackPanel` holding
`PART_ModalClose` and the existing MinimizeToggle (no new Grid column). **No new tokens** — the
close button reuses `TabStrip.Foreground` / `TabStrip.ControlHoverBackground` /
`ControlCornerRadius`, so it themes across all four generations for free. DataTriggers on
`Ribbon.IsModal` collapse the ApplicationButton and the MinimizeToggle; `Style.Triggers` sits after
every setter (MC3088).

**Build gotcha (hit twice this arc):** a private nested `ICommand` class must not share its name
with the property that exposes it — `public ICommand FooCommand => _foo;` beside
`private sealed class FooCommand` is **CS0102**, since a nested type and a member share one
declaration space. Named `ModalCloseCommand` and `CaptionActionCommand` instead.

### 3.33 Tab merging + group contributions — Phase 7 P7.2/P7.3 — 2026-07-27

A `RibbonMergeSource` (public `FrameworkElement`, `Tabs` as content property) is a declarative bag
of ribbon content belonging to a child context — an embedded editor, an MDI document, a plug-in.
`Ribbon.Merge(source)` / `Unmerge(source)` insert and remove it; `RibbonGroupContribution` injects a
single group into an existing HOST tab addressed by that tab's `Ribbon.CommandId` (unmatched target
= silently skipped, so a source can't break a host that lacks the tab it hoped for).

**Activation is declarative and is NOT WPF focus.** `Target` + `IsActive` on the source drive
merge/unmerge, so the showcase merges with zero code-behind
(`IsActive="{Binding IsChecked, ElementName=MergeToggle}"`). Focus lands on ribbon buttons
constantly and would thrash the merge — the same lesson as §3.28. An attached
`RibbonMergeSource.Source` lets a child element *carry* its source, which is the hook `MdiContainer`
uses (§3.34).

**Ordering — a sort key, not index arithmetic.** Index maths at merge time isn't stable across
repeated cycles, so every tab in the strip (and every group in a target tab) gets a key and inserts
land at the first position whose key is greater. Host-declared content is `(0, -1)`; merged content
is `(order, firstMergeSequence)`, where the sequence is assigned the FIRST time a source merges and
reused forever. That buys three properties at once: same-`Order` sources keep their first-merge
relative order, a source that unmerges and re-merges returns to the same slot, and a **negative**
`Order` sorts before the host's own content — which is why the host sequence is `-1` rather than `0`.

**Unmerge is removal by reference**, never by remembered index, so two sources contributing into one
host tab can unmerge in any order and the host's own groups close back up correctly. The plan had
flagged index restoration as a risk; it dissolved on contact.

**DataContext, but not visual inheritance.** A merged tab's `DataContext` is *bound* to the source's
(only when the app hasn't pinned one on the tab), so an MVVM child's bindings resolve against the
CHILD's view model. Inherited *visual* properties still come from the host ribbon, so a merged tab
looks native. One mechanism can't give both; this splits them the useful way.

**Merged content is invisible to customization.** Merged tabs, contributed groups and their command
controls all carry the read-only attached `Ribbon.IsMerged` (set via
`RibbonCommandCatalog.CollectControls`, so "what counts as a command" stays defined in one place).
`RibbonCustomizationSerializer` and `RibbonCustomizePage` skip them — otherwise a group contributed
into a host tab would be captured as part of that tab's layout and re-created as a *user
customization* belonging to a child that may not even be loaded. Merged tabs also don't count
towards the customize page's "at least one tab must stay visible" rule.

`Apply` does **unmerge-all → ApplyLayout → re-merge** in a `try/finally`, because `ApplyLayout`
clears and rebuilds `Ribbon.Tabs` wholesale and would otherwise strand merged tabs at the end with
stale records.

**QAT proxies are PARKED, not orphaned.** `AddToQuickAccess` copies the command's `MergeSource` onto
the new proxy. Unmerging disables those proxies (greyed, like Office treats an unavailable command)
and keeps the marker; merging re-enables them; the serializer skips any QAT entry carrying one, so a
proxy of a transient child's command never restores pointing at nothing. It's a flag sweep rather
than a tree walk — an unmerged tab is in no tree to walk.

### 3.34 MDI ⇄ ribbon integration: tab merge + caption merge — MDI M4 — 2026-07-27

`docs/05-MDI-EMULATION-PLAN.md` §4 insists these are two features and must stay apart, and they do:

- **Tab merge** is not MDI-specific. `MdiContainer` sets `Ribbon` and the active document's
  `MdiDocument.MergeSource` is merged in, swapped as documents activate, removed when the last one
  closes. Wiring only — the mechanism is §3.33's.
- **Caption merge** is MDI-specific and goes through a deliberately thin contract on the ribbon:
  `ShowMergedCaption(icon, title, canClose)` / `ClearMergedCaption()`, four read-only properties,
  ONE `MergedCaptionCommand` taking a `RibbonMergedCaptionAction` parameter, and one
  `MergedCaptionActionRequested` event. **The ribbon knows nothing about MDI** — it offers placement
  and reports presses; the container decides what minimize/restore/close mean.

`MdiChild.IsCaptionMerged` collapses the child's own caption (trigger declared AFTER the
WindowState triggers so it outranks them) and a new bubbling `WindowStateChangedEvent` — raised
after `ApplyWindowState`, so the container sees the settled state — tells the container when to
merge. `MdiContainer.UpdateRibbonIntegration()` is written as **"make reality match state"** rather
than as transitions, so activation, close, maximize, restore, retarget and unload all converge
through one path.

**The icon is an `ImageSource`, not an element.** The child's caption and the ribbon would both want
to display it and a `UIElement` has one visual parent; `IconSourceOf` unwraps an `Image` to its
`Source`. **`MergedCaptionTitle` is deliberately not drawn in the strip** — classic MDI puts it in
the host window's title bar, and a second title would eat tab space; it's exposed so a host can bind
its window title to it.

Template: `PART_MergedCaptionIcon` (an `Image`) joins the ApplicationButton in a left-hand
`StackPanel`, and `PART_MergedCaptionMinimize/Restore/Close` join the right-hand one after the
minimize chevron, styled by `RibbonKit.MergedCaptionButton` — again reusing `TabStrip.*` tokens, so
still no new tokens.

**Regression this caused:** wrapping the ApplicationButton in that StackPanel with
`VerticalAlignment="Center"` stole the Stretch it had been inheriting from its Grid cell, and Office
2013 — whose File button is a solid accent block that must reach the row's bottom edge — grew a thin
gap. Wrapper is `Stretch`; the icon centres itself. General rule: when wrapping an existing template
element, give the wrapper the alignment the old parent provided and set explicit alignment on the new
siblings. Connected-edge themes (2013's block, 2010's tab) expose seams that flat 2024 hides.

**MDI child captions now track the accent.** `MdiChild.ActiveCaptionBackground` / `ActiveBorder`
were baked per theme and absent from `ThemeManager.AccentOverrideKeys`, so a custom accent left child
windows blue inside an otherwise recoloured ribbon. Both are now derived in `ApplyAccentOverrides`:
flat accent for 2024/2019/2013, and for Office 2010 a new **`CaptionRamp(accent)`** — light → base →
darker. Deliberately not `Gel()`: a gel ends *lighter* at the bottom (a glossy button's specular
highlight), while a title bar is a lit surface receding downwards, and reusing it made child windows
look like giant buttons. **Rule of thumb: any token whose default is the theme's accent must be in
`AccentOverrideKeys` AND derived in `ApplyAccentOverrides`, or it silently stops tracking.**

### 3.35 Quick access toolbar overflow — 2026-07-27

The QAT needed a length limit and an overflow flyout in the two placements that SHARE a row —
TabRow (competing with the tabs) and TitleBar (competing with the window title). BelowRibbon owns a
full-width row, so it stretches and never overflows and was left untouched.

New `Controls/RibbonQuickAccessToolBar.cs` (lookless `ItemsControl`) and
`Layout/RibbonQuickAccessPanel.cs`. `Ribbon.QuickAccessMaxWidth` (default 240 DIPs, ~8 small
buttons) caps the two shared placements.

- **A horizontal `StackPanel` can never detect overflow** — it measures children with INFINITE width
  in the stacking direction. The new panel honours the finite width it's given; children past that
  point are arranged to a zero rect (NOT collapsed — `Visibility` belongs to the app).
- **The template is a `DockPanel`, not a StackPanel.** The overflow button docks Right so DockPanel
  measures it first and hands the `ItemsPresenter` the remaining width — that IS the overflow
  signal. The panel must therefore not also reserve button width. Overflow only triggers when
  something CONSTRAINS the toolbar (`MaxWidth`); an Auto grid column measures with infinity.
- **`StaysOpen=True` + `PopupDismissHelper`**, the house rule for every RibbonKit flyout (§3.19).
  With WPF's own light-dismiss, a second click on » closes the popup on mouse-DOWN and the button's
  click reopens it. Always close through the BUTTON's `IsChecked`, never the popup's `IsOpen` — they
  are bound two-way and driving the popup leaves the button stuck looking pressed.

**⚠ The flyout must REUSE its proxies, never rebuild them.** A drop-down or split proxy *borrows*
its source's menu items while open and returns them when its own flyout closes (§3.19). Rebuilding
the entries per open discarded a proxy that still held borrowed items: they were never returned, the
SOURCE menu stayed permanently empty, and later opens showed a bare rounded panel. The symptom is
distinctive — it only hits borrowers (split/drop-down, never plain buttons) and only after a few
opens, because only the FIRST borrow strands the items. `_entries` now caches one proxy per QAT item
for the toolbar's lifetime; closing the flyout force-closes any entry whose menu is still open so
the borrow goes home. **Any change that recreates those proxies reintroduces this bug.**

Two more flyout rules: entries carry `Ribbon.QuickAccessOverflowItem` pointing at the real QAT item,
so a right-click inside the flyout can offer "Remove from Quick Access Toolbar" for the *item*
rather than the proxy; and the flyout is a SNAPSHOT taken at open, so `OnItemsChanged` closes it on
any toolbar change (deferred to Background priority, so the collection change finishes dispatching
before a borrow is returned).

Alignment: the drop-down and split templates hardcoded `HorizontalAlignment="Center"` on their inner
`ContentPresenter`, so a stretched entry centred its icon+label while plain buttons left-aligned.
`PART_Toggle` and `PART_Primary` now forward `HorizontalContentAlignment` from the templated parent
and the presenter binds to it; the default stays Control's own `Center`, so nothing in the ribbon
changed and the flyout opts in with `Left`.

### 3.36 Selection visuals: the tab-strip-row reflow rule

The sliding underline (`PART_TabMarker`) and the 2010/2013 body-border notch (`PART_ConnectNotch`)
are positioned from the **selected tab's transform**, computed on demand. Nothing recomputes them
automatically, and there are two independent reasons a size event won't tell you:

1. **`SizeChanged` on the tab control doesn't fire.** The strip lives in the star-width column of the
   row, so a SIBLING growing or shrinking re-lays-out the strip while the `RibbonTabControl` keeps
   its own size.
2. **`SizeChanged` on the sibling doesn't fire either, when its `Visibility` toggles.** A `Collapsed`
   element is skipped by layout entirely — never measured, keeps its stale `RenderSize`, raises
   nothing going Collapsed or coming back.

Both shipped broken during this arc (the merged-caption icon for #1; the QAT moving in and out of
the tab row for #2 — where adding and removing *items* updated the notch, because that is a real
size change, while moving the whole toolbar did not).

**The rule:** anything that changes what is laid out in the tab-strip row must reach
`Ribbon.RequestSelectionVisualsRefresh()`, and a Visibility toggle or an async re-parent needs an
EXPLICIT call. Hazardous siblings: the merged-caption icon and window buttons, the quick access strip
(placement, item count, overflow button), the File button hiding for a modal tab, the modal close
button, tabs merging in or out. Current callers: `OnMergeChanged`, `OnModalStateChanged`,
`ShowMergedCaption`, `ClearMergedCaption`, `OnTabRowQatSizeChanged`, `ApplyQuickAccessPlacement`,
and `OnThemeConfigurationChanged`.

`RefreshSelectionVisuals()` is InvalidateArrange → UpdateLayout → `RibbonTabControl.RefreshSelectionVisuals()`
— layout must be forced FIRST or the transform read is stale.
`RequestSelectionVisualsRefresh()` defers it to `DispatcherPriority.Loaded` behind a coalescing flag,
because `SizeChanged` fires *during* layout and quick-access placement re-parents asynchronously.
`Loaded` sits below `Render`, which is what makes it "after layout".

### 3.37 Splitting Office2024.xaml into Controls.*.xaml parts — 2026-07-27 — ⚗ EXPERIMENTAL

> **Status: on trial.** Adopted to cut assistant token cost per edit. Keep for now, but
> re-evaluate against the exit criteria at the end of this section. Reverting is cheap:
> `git revert` the split commit, or concatenate the ten parts back in aggregator order.

**Problem.** `Themes/Office2024.xaml` had grown to 3,814 lines / 281 KB. Reading it once
costs roughly 85k tokens, so any assistant session that touched a template burned a large
share of the usage budget before making a single edit.

**Change.** The file is now a 35-line aggregator whose entire body is
`ResourceDictionary.MergedDictionaries`, listing ten parts split on control-family lines:

| Part (`Themes/Controls.*.xaml`) | ~Lines | Contains |
|---|---|---|
| `Controls.Shared.xaml`         | 240 | `RibbonKit.BoolToVis`, `MergedCaptionButton`, `RibbonQuickAccessToolBar`, scroll buttons |
| `Controls.RibbonChrome.xaml`   | 578 | `Ribbon`, `RibbonTabControl` |
| `Controls.Groups.xaml`         | 451 | `RibbonTab`, `RibbonGroupsHost`, `RibbonGroup` |
| `Controls.Buttons.xaml`        | 362 | `RibbonButton`, `RibbonScreenTip`, `RibbonToggleButton` |
| `Controls.DropDowns.xaml`      | 673 | `RibbonDropDownButton`, `RibbonSplitButton`, `RibbonMenuItem`, `RibbonComboBox` |
| `Controls.Backstage.xaml`      | 367 | Modern backstage brushes, `Backstage`, `BackstageTabItem` |
| `Controls.Galleries.xaml`      | 231 | `RibbonGalleryItem`, `RibbonGallery`, `InRibbonGallery` |
| `Controls.Window.xaml`         | 189 | `RibbonWindow` |
| `Controls.OptionsDialog.xaml`  | 382 | Rail brush, Options button styles, `RibbonOptionsPage`, `RibbonOptionsDialog` |
| `Controls.Customize.xaml`      | 463 | `RibbonQuickAccessPage`, `RibbonCustomizePage`, `RibbonCustomizeEditDialog` |

Nothing was retyped: the split was done by slicing byte ranges in binary mode, so CRLF
survived and the `x:Key` / `TargetType` / `DynamicResource` / `StaticResource` inventories are
identical before and after. `Generic.xaml` is unchanged — it still merges `Office2024.xaml`.
The csproj is SDK-style with implicit globbing, so new parts need no csproj entry.

#### The pitfall this cost us: StaticResource does not cross siblings

The first attempt threw at runtime, with no useful location:

```
Cannot find resource named 'RibbonKit.BoolToVis'. Resource names are case sensitive.
```

**A `StaticResource` inside a merged dictionary resolves only against that dictionary and the
dictionaries IT merges — never against sibling dictionaries merged by the same parent.**
Listing `Controls.Shared.xaml` first in the aggregator does nothing; merge order is irrelevant
to this lookup. The exception surfaces when the *template* is realized, far from the `x:Key`,
which is why it is so hard to locate.

Fix: the depending part merges its dependency locally, at the top of its own file.

```xml
<ResourceDictionary.MergedDictionaries>
    <ResourceDictionary Source="/RibbonKit;component/Themes/Controls.Shared.xaml" />
</ResourceDictionary.MergedDictionaries>
```

Exactly two parts need this — `RibbonChrome`→`Shared` and `Customize`→`OptionsDialog`. Both
dependencies stay listed in the aggregator too; WPF caches dictionaries by `Source` URI, and the
duplicated implicit styles are identical, so last-merged-wins is a no-op.

`DynamicResource` is **not** an escape hatch: ten of the sixteen cross-boundary references are
`Converter={StaticResource RibbonKit.BoolToVis}` inside a `Binding`, and `Binding.Converter` is a
plain CLR property, not a dependency property, so it cannot take a dynamic reference.

#### Rules for living with the split

1. Before moving a resource between parts, check every `{StaticResource K}` still resolves:
   K must be defined in the same file **above** its use, or in a file that part merges.
   Forward references inside one file fail too. This is a runtime failure, not a build error.
2. Keep `BasedOn` chains inside a single part. All three are: ScrollRight→ScrollLeft (Shared),
   Primary→Action and Reorder→Action (OptionsDialog).
3. A new part must be added to `Office2024.xaml`'s merge list. Forgetting is silent — the
   control simply renders untemplated, with no error.
4. Never define the same implicit `TargetType` style in two parts. Last-merged silently wins,
   and the conflict is invisible because the two definitions are in different files.

**All four rules are enforced at build time** by `tests/RibbonKit.Tests/ThemeDictionaryScopeTests.cs`
(four xunit facts, pure XML analysis — no WPF types, so it runs headless in CI via the existing
`dotnet test` step). It reports the offending file and line, which WPF itself does not. Verified
by mutation: removing RibbonChrome's local merge of Shared, introducing a forward reference,
adding an unregistered part, duplicating an implicit style, and pointing a `BasedOn` across files
each fail exactly one fact; a commented-out `{StaticResource}` correctly does not.

#### Known downsides (the reason this is experimental)

- ~~**Runtime-only failure modes.**~~ Rules 1, 3 and 4 would fail at runtime or not at all;
  the monolith made these mistakes impossible or obvious. **Mitigated 2026-07-27** by the
  guard test above, which turns all four into build failures. This was the main argument
  against the split; treat the guard as part of the split, not an optional extra.
- **No help for cross-cutting edits.** Renaming a token or auditing every template still
  touches ten files instead of one, and costs slightly more than the monolith did.
- ~~**Design-time load.**~~ The XAML designer now walks an eleven-dictionary graph, so designer
  preview (§3.22, §3.23) was the remaining worry. **Checked 2026-07-27 on a laptop — no
  measurable slowdown or flakiness in either the XAML editor or the Ribbon Editor.** A laptop
  is the weaker case, so this is a reasonably strong negative result.
- **Discoverability.** "Where is X?" is one grep away, but it *is* an extra step, and the
  aggregator's merge order now carries meaning that a casual reader will not guess.

#### Exit criteria — revisit when any of these is true

- Designer preview or the Ribbon Editor becomes measurably slower or less reliable (checked
  once on 2026-07-27 and clean; re-check on a large multi-tab document).
- A cross-part resource bug reaches runtime *despite* the guard test (i.e. the guard has a
  hole — note it only tracks keys per file, not per nested resource scope).
- Sessions are observed reading three or more parts for a typical single-control change,
  which would mean the split boundaries are wrong (fix the boundaries, not the idea).

If it survives a few sessions, promote it out of EXPERIMENTAL and drop the caveat in §2.1.

### 3.38 Office 2007 theme — the last generation, and the one that changed the templates — 2026-07-27

The fifth and oldest theme. Planned in `docs/07-OFFICE-2007-THEME-PLAN.md`, which stays the
reference for the measured palette; this section records what was actually built and what it cost.

**Why it was scheduled before Phase 8:** every other remaining item is additive, but 2007 was the
only generation left that could still force a change to the token layer or the shared templates. It
did — twice (the group box and the orb). Doing it after an API freeze would have been painful.

#### Method: measured, not guessed

The user supplied 13 real Office 2007 screenshots and every colour in `Tokens.Office2007.xaml` was
sampled from them pixel by pixel rather than eyeballed. That single change of method is why 2007
took roughly four visual passes against 2010's seven (§3.27). It also **corrected two things this
document previously stated wrongly**:

- §3.27 recorded that 2007's pressed state "inverts (darker at top = recessed)". It does not.
  Pressed keeps the same light-top / dark-waist / light-foot valley as hover and simply shifts from
  gold to saturated orange.
- An early pass here recorded an "etched group separator, `#9EBED9` with a `#ECF4FA` companion".
  **Office 2007 has no group separator at all.** Sampling across a group boundary gives
  `body / border / inner-highlight / gap / border / inner-highlight / body` — that is two adjacent
  group BOXES meeting, not one divider.

#### The two signatures

- **The valley.** Both the title bar and the ribbon body run light at the top, dip to a darker waist
  high up (17–28% down), then lighten to the foot. No other RibbonKit theme does this, and it is the
  strongest 2007 cue — the theme reads as 2007 before any glass is applied.
- **The crease.** Every hot state is a 4–5 stop gel with **two `GradientStop`s at the same offset**,
  painting an instant colour step (hover crease at 0.38, pressed at 0.40). Hover is gold, pressed is
  saturated orange. 2010 explicitly softened this into a smooth ramp, which is why the derived-accent
  helpers must stay separate (below).

The tab strip is a **flat `SolidColorBrush`** (`#BFDBFF`), identical under Aero and non-Aero — the
one place 2007 is flatter than 2010.

#### Token growth: 95 → 118 keys per theme file

| Batch | Keys | What forced it |
|---|---|---|
| Group box | 7 | `Group.Background/Border/InnerHighlight/LabelBackground`, `GroupBorderThickness`, `GroupCornerRadius`, `GroupLabelCornerRadius` |
| Group spacing | 4 | `GroupMargin`, `GroupPadding`, `GroupsRowMargin`, `GroupCollapsedMargin` — see below |
| Separator suppression | 1 | `GroupSeparatorOpacity` |
| Orb | 6 | `ApplicationOrb.Background/HoverBackground/PressedBackground/Ring/HoverRing`, `ApplicationOrbSize`, `ApplicationOrbMargin`, `Effects.ApplicationOrbShadow` |
| Title-bar QAT + backstage | 3 | `TitleBarQatMargin`, `Backstage.NavBackground`, `BackstageBackButtonSize` |

Every non-2007 value reproduces a previously hard-coded literal exactly, so the other four themes are
unchanged. **Five hard-coded literals became tokens in the process** — that is the real story of this
theme: 2007 is different enough that anything the templates had baked in had to become themable.

#### Group boxes (`Themes/Controls.Groups.xaml`)

Each 2007 group is a bordered rounded box with a light inner rim and a shaded label band at its foot
(`#A8BFD4` border, `#E4ECF5` rim, `#C1D9F1` band, ~3px radius). The flat themes zero the new keys, so
one visual tree serves both looks. The collapsed-group button gets the same box so a mixed row reads
uniformly — but its `Chrome` keeps its own `ControlHighlightBorderThickness`, because that thickness
is statically reserved (§3.27 fifth pass) and swapping it would bring 2010's 1px hover jitter back.

⚠ **Two placement rules were learned the hard way:**

1. **The box goes OUTSIDE `PART_NormalHost`.** `RibbonGroup` re-homes `_normalHost.Child` into the
   flyout when a collapsed group opens, so a box inside the Decorator travelled into the popup and
   drew a box inside a popup that already had chrome. Correct order is
   `Border GroupBox → Border rim → Decorator PART_NormalHost → content`; only bare content moves.
   The `SizeState=Collapsed` trigger therefore hides `GroupBox`, not the host.
2. **`GroupPadding` belongs on the `ItemsPresenter`, not the rim Border.** On the rim it insets the
   label band too, and the band floats free of the border instead of spanning it.

Splitting the old hard-coded `Margin="4,2,4,0"` into an outer `GroupMargin` and an inner
`GroupPadding` fixed two bugs at once: content had lost its inset (combo boxes and galleries hugged
the border) and box-to-box gaps had grown to ~11px.

#### The orb (`RibbonApplicationButtonShape`)

The only new public API: an enum (`Tab` | `Orb`) in `RibbonControlSize.cs` plus
`Ribbon.ApplicationButtonShape`, defaulted to `Tab` and registered for the designer. **Shape is an
application choice, not a theme one** — RibbonKit themes recolour through tokens and never reshape
controls, so the showcase opts in explicitly when it switches to 2007.

The glyph is built into the template because `ApplicationButtonHeader` is typed `string` — a consumer
literally cannot put an image in it. The header became the orb's `AutomationProperties.Name` and
`ToolTip` instead.

⚠ **Three findings worth keeping:**

1. **`WindowChrome.IsHitTestVisibleInChrome="True"` is mandatory.** The orb overhangs upward into the
   caption region via a negative top margin, and the caption swallows input as a window drag — only
   the bottom half was clickable without it. Any element overhanging into the caption needs this.
2. **The orb must hide when the backstage opens.** The same negative margin puts it above the
   backstage adorner.
3. **The dark edge is a SHADOW, not a border.** A 2px dark stroke read as a hard ring; a real orb is
   a soft drop shadow plus a near-white rim over a spherical gradient. And **hover recolours the
   whole sphere amber**, not just the rim — the same class of mistake as 2010's first gradient pass.

An inset white highlight ring was tried and removed: real glass highlights the top arc, so a full
circle reads as a hard band at any opacity.

The overhang is `ApplicationOrbMargin`'s negative top. **If it is ever clipped, set that top to 0** —
the orb then sits wholly inside the tab strip and everything else still works.

#### Accent: `Glass()` beside `Gel()`

`ThemeManager` now derives two gradient profiles. `Gel` is 2010's smooth 3-stop ramp; `Glass` is
2007's 4 stops with two sharing offset 0.45. **They must not be merged** — that crease is the entire
difference between the generations. The `Office2007` case mirrors the 2010 case with `Glass()`
throughout, and the toggled-highlight skip was widened to cover both themes (both keep a gold hot
state regardless of accent). The orb is deliberately excluded from accent derivation: it carries a
logo, not a themeable surface.

**`Classic2007` was NOT added to `RibbonBackstageDesign`.** `Classic2010` already is the 2007 look,
and its glass marker binds `Dialog.PrimaryBackground`, so it picks up whichever profile the active
theme derives. A near-duplicate enum member right before the API freeze was not worth it; the
existing value's doc comment was widened instead. The name reads narrow now — worth one thought at
the Phase 8 review, but renaming is breaking and it has shipped.

#### Two bugs found on the way

- **`Group.Separator` was doing triple duty.** Setting it `Transparent` to suppress 2007's group
  separator also deleted the **menu separator** (`Menus.xaml`) and the **gallery item hover border**
  (`Controls.Galleries.xaml`). Resolution: a `GroupSeparatorOpacity` metric hides the ribbon
  separator only, and the brush keeps a real colour. Opacity rather than width, so group spacing is
  unchanged. **Grep `Themes/` for a key before neutralising it — names lie about scope.**
- **The body-border notch was left stranded by the backstage.** Repro: unmaximized → open backstage
  → maximize → close. While the overlay is up the ribbon is hidden, so the strip widening never
  reached the notch. `OnIsBackstageOpenChanged` now calls `RefreshSelectionVisuals()` on close, at
  `DispatcherPriority.Loaded`. Same family as the theme-switch case in §3.27's fourth pass; §5's
  refresh rule now lists overlay-close as a caller.

#### Deferred, deliberately

- **The 2007 window frame** (`#3B5A82` over a 5–7px `#9BBBE3` band). Windows 11 has no Aero and draws
  its own border; the frame is also the change most likely to perturb the measured-margin maximize
  fix (§2.3). Left until it can be done alone and tested properly. The later post-v1 decision in
  §3.88 keeps this opaque frame as the guaranteed baseline and adds an optional Aero-inspired mode.
- **The real two-pane application menu** — command column, Recent Documents pane, Options/Exit bar.
  That is a NEW CONTROL, not a theme, and it is a genuine feature gap: `README.md` claimed an
  application menu existed when it only had the application button plus the backstage. The README row
  is now corrected.
- **Silver and Black schemes.** Pure token clones now that the geometry is proven.

### 3.39 Stranded menus, take two: the borrow must not hang off `Popup.Closed` — 2026-07-27

§3.35 fixed proxy REBUILDING as the cause of empty source menus. Caching the entries was necessary
but not sufficient — a second, independent path stranded the same items, with the same
who-would-guess symptom: open the QAT overflow flyout, open a drop-down or split entry's menu inside
it, then dismiss everything with one click somewhere else in the window. From then on the ORIGINAL
button back in the ribbon opens onto an empty menu, which reads to the user as "the popup won't show
up at all". Nothing is wrong at the place they clicked.

**The mechanism.** WPF coerces `Popup.IsOpen` to false while a popup is not loaded. Closing the
overflow flyout unloads everything inside it, so the entry's own popup is coerced shut — and a
coerced value never travels back through the template's `IsOpen` binding. The entry is left with
`IsDropDownOpen == true` describing a popup that is already gone. Every consequence follows from
that one desync:

- The old return path was `Popup.Closed` → `BeginInvoke(Background)` → `if (!IsDropDownOpen) return
  the items`. The guard exists for a fast close→reopen, but here it read the STALE true and skipped.
- `OnOverflowClosed`'s `IsDropDownOpen: true` → `SetCurrentValue(false)` rescue was a no-op that the
  popup could never see (its effective value was already false), so no second `Closed` was raised and
  there was no second chance to return.
- The items stayed in a proxy that is only reachable through a flyout that rebuilds its list on every
  open. Nothing ever asked that proxy to close again, so the source menu stayed empty for the rest of
  the session.

**The fix is to make the PROPERTY the contract and the popup an implementation detail.**
`OnIsDropDownOpenChanged` now schedules the return on the false transition (it already borrowed on
the true one), so the round trip is symmetric and does not care whether a popup exists at all.
`OnPopupClosed` first SYNCS `IsDropDownOpen` when it finds it still true — that is the moment we
learn the popup was shut behind our back, and correcting it there both frees the return and stops
the entry from looking pressed (and from auto-popping its menu the next time the flyout opens, since
the stale local `IsOpen=true` would otherwise re-assert on load). `EnsureBorrowedItemsReturned()` is
the explicit hook for hosts: it closes the dropdown if needed and drives the return itself.
`RibbonQuickAccessToolBar` calls it for EVERY cached entry on close and before pruning one, instead
of testing `IsDropDownOpen` — the property it cannot trust. A final `Unloaded` net covers a host that
disappears without the popup raising `Closed` at all; it only fires when the popup is genuinely not
open, so a theme swap re-templating under an open menu is left alone. All four paths are idempotent
and re-guarded at Background priority, so overlapping requests move the menu exactly once.

**Rule of thumb:** `Popup.Closed` tells you the popup closed, not that the control agreed to it. Any
state a RibbonKit flyout owns must be reconciled on the CONTROL's property, and any popup that can
be nested inside another popup will one day be closed by its host rather than by itself.

**First unit tests.** This is also where the test project stopped being a smoke test.
`tests/RibbonKit.Tests/Sta.cs` runs a body on an STA thread with a live dispatcher plus a `Drain()`
that pumps queued Background work — no `Application`, no window, so it runs on a CI agent.
`DropDownBorrowTests` pins the whole borrow protocol (including this regression, which fails against
the old code) and `QuickAccessOverflowTests` pins the panel's measure/arrange rules and the command
proxy factory. `[assembly: InternalsVisibleTo("RibbonKit.Tests")]` was added for this: the contracts
worth testing here — `BorrowMenuFrom`, `EnsureBorrowedItemsReturned`, `OverflowedChildren`,
`CreateCommandProxy` — are deliberately not public API, and widening the surface to test them would
be the wrong trade. Testing without popups is not a compromise either; it is the point, since the
bug was caused by trusting the popup in the first place.

### 3.40 Chrome polish batch — pressed states, caption glass, and four hijacked tokens — 2026-07-28

Nine user-reported defects in one arc. They look unrelated in a screenshot and mostly are not: four
of them are the same mistake, which is worth naming before the list.

**The pattern: chrome that borrows a token another system owns.** A brush like
`Ribbon.Background` reads like "the strip's colour", so small chrome bound to it directly. But two
subsystems rewrite that key out from under a consumer — `ThemeManager` repaints it with the accent
on a coloured 2019 strip, and 2024 sets it `Transparent` so a Mica/Acrylic backdrop can show through
the band. Anything that borrowed it inherited both. **Chrome gets its own token whenever its intent
differs from the surface it sits on**, even when the two happen to share a value today.

**1 — No pressed state anywhere on the tab-strip chrome.** Six buttons (modal-tab close, minimize
chevron, the three merged-caption window buttons, the QAT overflow ») had hover and nothing else, so
a click never registered. New `RibbonKit.Brushes.TabStrip.ControlPressedBackground` in all five
generations: a step darker for the flat themes, the hover glass INVERTED (dark top face, specular
foot) for 2010, and the same inverted with 2007's hard crease. ⚠ **The `IsPressed` trigger must be
LAST in the block** — the pointer is over the button for the whole time it is held, so a trigger
placed before `IsMouseOver` (or before `IsChecked` on the overflow toggle) is immediately overwritten
and the press never shows.

**2 — Solid hover/press chips floating on Mica.** DWM composites a backdrop *beneath* the window, so
a solid `#E6E6E6` wash reads as an opaque sticker on the material. `ApplyTitleBarOverride`'s backdrop
branch now also swaps the caption buttons, the tab-strip chrome and the File button's pressed fill
for low-alpha black (`#1F000000` / `#33000000`), which tints the backdrop instead of covering it.
⚠ `ApplicationButton.PressedBackground` is **owned by the accent system** (2013 flat mixes, 2010 a
gel, 2007 glass), which runs first — clearing it unconditionally deletes the value that pass just
derived. The clear is guarded by `ReferenceEquals(resources[key], BackdropControlPressed)` so only
our own override is removed.

**3 — The 2024 File button's pressed fill had square bottom corners.** It was reusing
`TabCornerRadius`, right for 2010/2007/2013 whose File button is physically connected to the ribbon,
wrong for 2024 where it floats above the card. Split out as
`RibbonKit.Metrics.ApplicationButtonCornerRadius`; only 2024's value changes.

**4 — "Close Print Preview" sat flush against the window edge.** `PART_ModalClose` margin
`0,6,2,0` → `0,6,8,0`, matching the minimize chevron. Safe because the two are mutually exclusive —
the chevron collapses via the `IsModal` DataTrigger.

**5 — 2007/2010 caption buttons were flat chips on a glass caption.** All four
`CaptionButton.*Background` keys became gradients: hover lit from above, pressed INVERTED, close the
same recipe in red, with 2007 carrying the hard crease and 2010 a smooth ramp — the same
`Glass()`/`Gel()` split the code already makes. Close pressed stays LIGHTER than close hover, the
convention the other generations follow. Shared with `RibbonKit.MdiCloseButton`, which wants the
identical look.

**6 — A colored title bar flattened 2007 and 2010.** `SetAccentedTitleBar` wrote `Frozen(accent)`
for every theme; right for the flat generations, wrong for the two whose uncolored caption is glass —
it turned the top 34px into 2013. Each now keeps its own gradient SHAPE re-hued to the accent: 2010
reuses `CaptionRamp`, 2007 gets a new `CaptionValley` helper matching its own token (light lip,
deeper band at 0.28, bright specular foot). Not `CaptionRamp` (ends dark, loses 2007's bright bottom
edge) and not `Glass` (the hard crease belongs on a button, not a window-wide caption). Caption
buttons follow with `Gel`/`Glass` in the accent hue. ⚠ Every white mix here stays ≤ 0.30: the accented
caption draws white text and glyphs over it.

**7 — The QAT overflow chevron stayed dark on an accent title bar, in every theme but 2019.**
`UpdateQatButtonContext` only walked `QuickAccessItems`; the » lives in
`RibbonQuickAccessToolBar`'s own TEMPLATE and is not an item, so it never received
`Ribbon.QatOnColoredSurface` or the band brushes. 2019 hid the bug — its colored strip repaints
`TabStrip.Foreground` white app-wide, which the chevron's stroke happens to use. New
`ApplyQatSurfaceContext(host, colored, hoverKey, pressedKey)` does for a HOST what the loop does for
an item; because the attached property **inherits**, setting it on the host carries it into the
template. Called per host with its own flag (`_titleBarQatHost` → `titleBarColored`, the new cached
`_qatTabRowHost` → `tabRowColored`, `_qatBelowHost` → always false). ⚠ Resolve the brushes via
`TryFindResource` on the RIBBON, not the host — a title-bar host lives outside the ribbon's visual
tree — and never store null: a `Border` whose `Background` trigger sets null drops out of hit-testing
and the click falls through to the WindowChrome caption.

**8 — A minimized ribbon had no bottom edge in 2007/2010/2013.** Collapsing the body takes its
outline with it, and in the bordered generations the tab strip then butts straight into the app's
content. New `MinimizedDivider` Border in the tab control's **body row** (so the hairline lands
exactly where the body's top border was), painted in `Ribbon.Border`. Per-theme opt-out is a height
token — `MinimizedDividerHeight` is 1 for the three, **0** for 2019 (tinted band) and 2024 (floating
card), and zero costs them no layout height either, the same idiom as a zeroed
`ContextualUnderlineHeight`. ⚠ It triggers on `Ribbon.IsMinimized`, **not** on
`ContentHost.Visibility`: the collapse is animated in code (slide, fade, *then* Collapsed), so keying
off Visibility would pop the line several frames late.

**9 — The tab-strip scroll chevrons: wrong fill, and floating above the tabs.** The fill was
`Ribbon.Background`, so 2024 rendered a bare outline with tabs sliding under it and an accented 2019
strip produced an accent block still carrying a dark glyph. New
`RibbonKit.Brushes.TabStrip.ScrollButtonBackground`: 2007/2010/2013 restate what they already
rendered, 2019 and 2024 become white so the chip reads as the selected tab. The vertical offset had a
real cause rather than needing a nudge — the tabs live inside `PART_TabScroll` and are inset by
`TabStripMargin`, while the chevrons are its SIBLINGS and stretch to the full row, so they floated
above the tabs by exactly that top inset (2007/2010: 2, 2019/2024: 4, **2013: 0**). The user
independently reported "too high in every theme except 2013" — the one theme with a top of 0, which
is what confirmed the diagnosis. Binding their `Margin` to the same `TabStripMargin` token makes them
track the strip everywhere, and keeps them correct if a margin is ever retuned.

**Also: modal tabs no longer appear in Customize Ribbon.** `RebuildTree` excluded contextual and
merged tabs and had no modal case, so Print Preview sat in the list offering a visibility checkbox
for a tab the ribbon re-hides. `RibbonTab.IsModal` already existed, so the filter needed no new API —
the three inline copies of the predicate collapsed into `IsCustomizableTab`. ⚠ **The one-line filter
alone would have created a worse bug.** `CanMove`/`MoveSelected` counted positions in the RAW
`ribbon.Tabs`; hiding a tab from the tree while still counting it for reordering means Up/Down can
swap the selection with an entry the user cannot see. That was live in the showcase (Print Preview is
declared last, so "move Favorite down" traded places with it). Both now work in the filtered list and
the move targets the NEIGHBOUR'S raw index instead of ±1, so `ObservableCollection.Move` lands the
tab where the neighbour was and anything excluded in between shuffles along. The flaw already existed
for contextual and merged tabs; the modal filter merely made it reachable by default.

**Rule of thumb from this batch:** when a fix hides something from a list, check every OTHER
operation that indexes the same collection. Filtering a view is half a change.

### 3.41 Two ordering bugs: an Effect painted over, and a FLIP that flashed — 2026-07-28

Both are about *when* WPF draws, not what. They were reported as unrelated cosmetic glitches and
share no code, but the debugging lesson is the same, so they are recorded together.

**The ribbon's drop shadow only appeared under Mica.** The 2007/2010/2024 bodies carry a real
`DropShadowEffect` (`Effects.ContentShadow`), and an **Effect renders outside the element's layout
bounds** — straight into whatever sits below. Panel siblings paint in declaration order, and a host's
content area is declared after the ribbon, so its opaque background covered the shadow. Mica hid the
bug rather than enabling the feature: the showcase sets `MainContentArea.Background = Transparent`
while a backdrop is on and back to `White` when off, so the shadow only ever survived in backdrop
mode — which made a paint-order bug look like a backdrop feature. Confirmed from the screenshot
pixels: directly under the body border, Mica ON gave `dbe3e6 → e0e8eb → e5ecf0 → e8f0f4 → eaf2f6`
(a shadow fading downward) and Mica OFF gave five rows of flat `ffffff`.

Fixed with `<Setter Property="Panel.ZIndex" Value="1" />` in the `Ribbon` implicit style — the ribbon
paints last among its siblings. Nothing was clipping the shadow; it only needed to be drawn later.
Library-side rather than showcase-side on purpose: every consumer that puts content below a ribbon
hits this, and an app author would sooner conclude the theme has no shadow. It sits beside the
existing `VerticalAlignment="Top"` setter — the same kind of defensive layout default, and a local
value still beats it.

**The window title now glides when the backstage hides the QAT.** `Ribbon` sets
`IsTitleBarContentVisible = false` while the backstage is open, the template collapses the
quick-access slot, and the title — which lives in the `*` column between that slot and the caption
buttons — teleported sideways by half the slot's width. `PART_Title` is now a declared template part
and `RibbonWindow` animates the difference via the new `RibbonMotion.AnimateTranslateX` (the
horizontal twin of `AnimateTranslateY`), on `RibbonAnimationAction.Backstage` timing so it moves with
the backstage rather than on its own clock.

It measures rather than computes: the shift involves an Auto column, a themed margin (2007 insets the
slot to clear the overhanging orb) and a trimmed `TextBlock`, so hand-computed geometry would drift
from what renders. ⚠ **The two measurements are deliberately asymmetric** — BEFORE *includes* any
transform still running from a previous toggle (that is where the title visually is), AFTER
*subtracts* it (that transform is about to be replaced; we want the resting position). Reading both
the same way makes a fast open-close-open sequence jump.

**And then it flickered, intermittently — two independent one-frame bugs, both needed fixing.**
The symptom was the title snapping to its destination for a frame before animating properly.

1. **`DispatcherPriority.Loaded` runs AFTER `Render`.** The first version took its second measurement
   on a dispatcher hop, so layout and rendering both completed before any offset was set and the
   composition thread could present a frame with the title already home. Replaced with a one-shot
   `LayoutUpdated` handler, which fires at the end of the arrange pass, inside the frame that is
   about to be presented. **Never use a dispatcher hop for a FLIP in WPF.**
2. **The animation clock had not ticked.** WPF ticks the timing manager at the START of a render
   frame, before layout — so an animation begun during that frame's layout is first ticked on the
   NEXT frame, and until then the property falls back to its BASE value, which was 0: the
   destination. `AnimateTranslateX` now seeds `translate.X = fromX` immediately before
   `BeginAnimation`; the animation outranks the base value as soon as the clock catches up.

Intermittency was the tell — whether a frame is presented in either gap depends on render-thread
timing. Fixing only one would have left a shorter flash.

Bookkeeping worth keeping: `_titleShiftPending` stops a double subscription when a toggle arrives
before the layout pass (the newest BEFORE reading wins, since nothing has moved yet), and
`OnApplyTemplate` unsubscribes while the OLD part is still in hand or the handler pins a discarded
element.

**Also shipped here: the combo box drop-down fades and slides down.**
`RibbonAnimationAction.DropdownMenu` (130ms, 8px) had been declared with timings and **zero
consumers** — every flyout opened instantly. `RibbonComboBox.OnDropDownOpened` now calls
`RibbonMotion.PlayOpen(_popupRoot, DropdownMenu, RibbonSlideFrom.Top)`;
`RibbonDropDownButton`, `RibbonSplitButton` and `RibbonMenuItem` are the same three lines each if
they should follow. ⚠ **A `Popup` clips its child's `RenderTransform`**: the popup's window is sized
to the child's LAYOUT size and a transform does not grow it, so sliding the border up from -8 sliced
its top 8px against the window edge. Fixed with a matched pair — `Popup.VerticalOffset="-10"` plus a
10px larger top margin on the child — which leaves the resting position pixel-identical while giving
the slide room. Keep 10 > the slide offset if that is ever raised, and expect to repeat the trick for
any other flyout given a slide. Open only: a close animation would mean holding the popup alive past
the close, and `ComboBox`'s built-in mouse-capture management assumes the popup closes when it says
so.

### 3.42 Every flyout now opens as a whole surface — and the DPI manifest — 2026-07-28

**The complaint was that only the *contents* moved.** §3.41 gave `RibbonComboBox` a proper
fade-and-slide, but the drop-down button, the split button and the in-ribbon gallery animated
`_menuHost.Child` / `_popupHost.Child` instead — so the bordered card and its shadow snapped into
existence around a set of items that then slid down inside it. Read as a glitch rather than as
motion. The context menu had no entrance at all.

That inner-content choice was made deliberately, on a diagnosis that turns out to be wrong. Both
call sites carried a comment saying a transform on the popup's own child border would "shift the
transparent popup's resting position". It does not: a `Popup` positions its window from its child's
LAYOUT size, and a `RenderTransform` is not layout — the window never moves. What actually happens
is that the transformed border is **CLIPPED** against the window's edge, which looks like the
surface being cut off, and is easy to misread as displacement. §3.41 had already found the real
mechanism and the fix while doing the combo box; the two older call sites simply predated it.

**Every flyout now animates the surface itself**, through one method —
`RibbonMotion.PlayFlyoutOpen` — on `RibbonAnimationAction.DropdownMenu` (gallery: `Gallery`):
drop-down button, split button, combo box, context menu, menu-item submenu, in-ribbon gallery, QAT
overflow » flyout, collapsed-group flyout. **No template geometry changed.** That sentence is the
entire point of the section below.

The rule for **where** the call goes: a control that already owns its popup's `Opened` handler plays
the transition there (`RibbonDropDownButton`, `InRibbonGallery`, `RibbonQuickAccessToolBar`,
`RibbonComboBox` via `OnDropDownOpened`); `RibbonPopupMotion` exists only for the flyouts with no
such hook. Do not do both on one popup.

**⚠⚠ The SURFACE only fades. The CONTENT slides. That split is a correctness decision, not a taste
one, and it took three attempts to land on it.**

A moving surface has to start OUTSIDE its resting bounds, and a popup's window is sized to its
child's **layout** size, which a transform does not grow — so it is sliced against the window's top
edge. The obvious remedy is extra top margin on the child. The trap is what that margin does to
POSITION, and the answer is **not the same for every popup**:

- a plain `Popup` (drop-down button, split button, QAT overflow, collapsed group) **compensates** for
  the child's margin — the surface stays on its anchor, so no offset is wanted;
- a `ComboBox`'s managed `PART_Popup`, a `ContextMenu`'s internally-built popup, and the gallery's
  overlay popup **do not** — the margin displaces them, so an offset IS wanted.

Attempt one assumed the second rule for all seven: a negative `VerticalOffset` everywhere, which
lifted the four compensating popups off their anchors (menus 16px, gallery 20px). Attempt two
assumed the first rule for all seven: no offsets, which dropped the other three by the same amounts.
The symptom flipped; the model was wrong either way. Attempt three scaled the surface from 0.92
instead — geometrically safe, since a scale under 1 never leaves its bounds, but it distorts text
for the length of the transition and simply looked cheap.

**Moving the content is what the ORIGINAL code did**, before any of this — and it was right, for a
reason it had not written down. The content lives inside the surface's padding, so its travel never
approaches the popup window's edge, and the surface keeps whatever geometry the template always had.
No headroom margin, no placement offset, nothing that depends on which kind of popup this is. The
only thing genuinely wrong with the original was the bordered card snapping into existence around
moving items, and a fade on the surface fixes exactly that and nothing else.

Every template is back to the exact geometry it shipped with before §3.41 — including the combo box,
which had been sitting 10px high since §3.41 paired a `-10` offset with a 10px margin and passed a
verification round anyway, because an error that small just reads as design.

**And the fade flickered until it was seeded — the §3.41 rule again, this time on opacity.** WPF
ticks the timing manager at the START of a render frame, so an animation begun during that frame
first ticks on the NEXT one, and until then the property falls back to its BASE value. With the base
left at 1 the surface was presented fully opaque for one frame and only then faded in from 0: a
flicker, not a fade. `PlayFlyoutOpen` now seeds `Opacity = 0` before `BeginAnimation` — the same
thing the slide already did for its transform, missed on the fade because opacity does not *feel*
like a FLIP.

Seeding alone would have been a worse bug, though. A base of 0 means anything that drops the
animation without calling `Rest` leaves an **invisible flyout**, and that was the stated reason
§3.41 deliberately did not seed opacity in shared code. So the fade **releases itself**: on
`Completed` the base goes back to 1 and the animation is cleared, leaving the surface with no
animation and a resting value of 1. Order matters inside that handler — set the base first, then
clear, or the property momentarily falls back to 0. Same shape as `PlayKeyTipPop`.

`PopupMotionTests.The_surface_is_never_transformed` sweeps every animation level and fails if a
transform ever lands on a flyout surface — the one edit that would reintroduce the displacement on
all seven at once. `The_surface_starts_transparent` guards the seeding. `PlayFlyoutOpen` also clears
any transform it finds on the surface, so a stale one from a previous revision cannot survive.

**The process lesson, since it repeated three times:** the first fix reasoned from a layout model,
the second from a corrected model, the third from avoiding the model — and only the fourth asked
what the code already did and why. When a previous implementation looks naive, the cheapest move is
to work out what it was defending against before replacing it. Also: measuring the screenshots (~16px
and ~20px, matching the two margin values exactly) settled in one pass what two rounds of reasoning
could not.

**Two flyouts had no control of their own to hook**, so they get an attached behaviour —
`Animation/RibbonPopupMotion.cs`, `AnimateOpen` + `OpenAction`. On a `Popup` it animates
`Popup.Child`; on a `ContextMenu` it animates **the menu itself**, because WPF builds a
`ContextMenu`'s hosting popup internally and never exposes it — the menu *is* that popup's child.
The property-changed callback always unsubscribes before subscribing: a template can be applied
more than once (theme switch), and a duplicated handler would start the transition twice. The
submenu's `PopupAnimation="Fade"` is now `None`, so the entrance comes from themed timing rather
than from WPF's fixed one.

The screen-edge flip worry from the first version of this section is also gone: a surface that never
moves is unaffected by which edge WPF anchors the popup to.

**The collapsed-group flyout also needed menu semantics.** Clicking a command inside it left it
open — you had to click away. It now closes on `ButtonBase.Click`, deferred to
`DispatcherPriority.Background` because closing re-homes the entire content grid (including the
element whose click is still being dispatched) back into the ribbon; reparenting mid-dispatch is the
shape of the bug §3.19 and §3.39 each spent a round unpicking.

Openers are exempt, and the exemption is decided by **`TemplatedParent`, not by walking the tree**.
Openers are template parts — `PART_Toggle`, the gallery's expand/scroll buttons, the combo's chevron
— so they always carry the owning control as their templated parent, while the things a user
actually invokes (a `RibbonButton`, a `RibbonMenuItem`, a split button's `PART_Primary`) either live
in application markup with no templated parent, or are the primary part itself. A tree walk would
have had to hop the popup boundary between a menu item and its drop-down button, and that is exactly
the case it would get backwards — the menu item's nearest interesting ancestor IS a
`RibbonDropDownButton`, which is the one thing that must not close the flyout.

Right-click needs no special case: a context menu raises no `ButtonBase.Click`, and its rows are
`MenuItem`s, which raise `MenuItem.Click` — a different routed event. **Galleries and combo boxes
deliberately do NOT close it**: they commit through selection, and selection also changes when the
user merely arrows through the list, so closing there would be worse than not closing. Revisit only
with a real "committed" signal to hang it on. `CloseNestedGalleryPopups` also became
`CloseNestedFlyouts` and now shuts nested drop-downs as well as galleries, so dismissing the group
cannot leave a menu floating over a button that has moved back into the ribbon.

**⚠ And the overflow flyout thought it was sitting on the accent band.** Reported alongside the
animation gap: with the QAT in the tab-strip row or the title bar, entries inside the » flyout drew
accent-derived hover and pressed washes, and a split button's chevron came out white — invisible
against the white popup. `Ribbon.QatOnColoredSurface` is declared `Inherits` (§3.21 / the chevron
fix), the ribbon sets it on the toolbar HOST so the template's own chrome can read it, and
**property inheritance crosses into a `Popup`'s child** — so the whole flyout inherited it. The
flyout is an ordinary popup surface and never part of the band. Fixed by resetting the flag to
`False` on the popup's content border in `Controls.Shared.xaml`: a local value beats the inherited
one and re-propagates `False` down the subtree, which covers the entries, their nested
primary/chevron parts, and anything added there later.

The general shape is worth remembering: **an inheriting attached flag set on a host reaches every
popup that host's template opens.** Any flyout whose surface is NOT the thing the flag describes has
to opt out explicitly, and nothing warns you — the leak only shows up in whichever theme makes the
band's brushes visibly wrong.

**No Motion also has to suppress WPF's private context-menu popup.** The manual Windows pass found
that command and QAT context menus still faded after `DropdownMenu` correctly rested at `None`.
`ContextMenu` creates a private parent `Popup` and binds that host directly to
`SystemParameters.MenuPopupAnimationKey`; a resource placed on the child menu cannot reliably reach
its parent. RibbonKit now leases an application-level `PopupAnimation.None` override immediately
before either RibbonKit menu opens, keeps it only for that menu's lifetime, and restores the host
application's previous value on close. RibbonKit's own `RibbonPopupMotion` is therefore the sole
entrance at Subtle/Expressive, while No Motion is genuinely instant. The scope is reference-counted
so overlapping RibbonKit menus cannot restore the resource out of order. Covered by
`PopupMotionTests` and user-verified on Windows 2026-08-01.

**Separately: the showcase now declares PerMonitorV2 DPI awareness.** Changing the Windows display
scale with the app running left it the same size and blurry until a restart — the signature of
bitmap stretching, i.e. of a process that is only System-DPI aware. WPF on .NET does **not** opt in
by default. `samples/RibbonKit.Showcase/app.manifest` (+ `<ApplicationManifest>` in the csproj) now
declares `dpiAware=true/pm` *and* `dpiAwareness=PerMonitorV2` — both, since down-level Windows reads
the 2005 element and Windows 10 1703+ reads the 2016 one — plus the `supportedOS` block, without
which Windows will not honour PerMonitorV2 at all. `RibbonWindow.OnDpiChanged` already re-measured
the maximize inset; it had simply never been reached. **A library cannot set process DPI awareness
for its host, so consumers need the same manifest** — `README.md` now says so.

### 3.43 Split button: a vertical arrangement, and halves that acknowledge each other — 2026-07-28

Two changes to `RibbonSplitButton`, both about it reading as ONE control rather than two buttons
that happen to touch.

**Vertical arrangement (Large only).** Icon on top — the command half — with the caption and chevron
stacked beneath it on the drop-down half, which is Office's large Paste button. New public API:
`RibbonSplitButtonLayout` (`Horizontal` default / `Vertical`) and `RibbonSplitButton.Layout`, plus a
read-only `IsVerticalLayout`.

`IsVerticalLayout` is the piece worth keeping. `Layout` alone is not enough, because vertical is only
honoured at `Large` and the sizing engine steps a button down to `Medium` as its group narrows — so
the real condition is `Layout == Vertical && Size == Large`, and it has to be re-evaluated whenever
EITHER changes. `Size` is declared by `RibbonDropDownButton`, so the derived class re-registers it:

```csharp
SizeProperty.OverrideMetadata(typeof(RibbonSplitButton),
    new FrameworkPropertyMetadata(RibbonControlSize.Large, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutInputChanged));
```

⚠ `OverrideMetadata` **replaces** the base metadata rather than merging it, so the default and
`AffectsMeasure` are re-stated there deliberately — dropping either would break the sizing engine in
a way that only shows up on resize.

Publishing one flag instead of testing the pair in the template is not a convenience. The
vertical-only differences live in **three separate namescopes** — the outer template and the nested
template of each half — so the alternative was the same two-condition `MultiDataTrigger` written six
times, with six chances for the halves to disagree about which way round the button is.

**One grid, re-spanned; not two panels.** The halves stay put and the vertical trigger moves them
from `Column 0 | Column 1` to `Row 0 / Row 1`, with `ChevronColumn` pinned to 0 and the chevron
half's `Width` set to `Auto` (= `NaN`) so it stretches instead of staying a 15px sliver. A second
panel would have needed a second `PART_Primary` / `PART_Toggle`, and a template part may only be
named once per namescope.

The nested half templates read `IsVerticalLayout` through an **AncestorType** binding, not a
`TemplateBinding` — inside the `Button`'s own `ControlTemplate` the templated parent is the *button*,
not the split button. Same reason the vertical caption binds `Header` that way.

The caption is `TextWrapping="NoWrap"` + `CharacterEllipsis` at `MaxWidth="76"`. One line is the
contract, not a limitation: a second line here would make the two halves different heights whenever
the text was long, and 76 is `RibbonButton`'s Large width, so a vertical split button lines up with
the plain large buttons beside it.

**Companion highlight.** Hovering or pressing either half now marks the other one. Three tokens per
theme carry the whole difference:

| generation | `CompanionBackground` | `CompanionBorder` | `CompanionGlow` |
| --- | --- | --- | --- |
| 2007 | `Transparent` | `#C1A877` | `#CCFFEDC2` |
| 2010 | `Transparent` | `#E9C25E` | `#CCFFF0CB` |
| 2013 | `#F4F7FC` | `Transparent` | `Transparent` |
| 2019 | `#EFEEED` | `Transparent` | `Transparent` |
| 2024 | `#F2F2F2` | `Transparent` | `Transparent` |

The two treatments are genuinely different, not scaled versions of each other. The gradient
generations draw the amber outline plus a **1px glow rim just inside it, and no fill at all** — the
first cut washed the whole half in pale amber and the result was that a merely-adjacent button read
as almost as hot as the hovered one, which defeats the point of distinguishing them. The flat
generations have no highlight border to glow inside, so they take a lighter version of their own
hover fill and leave both border and glow `Transparent`.

Because the *theme* decides what companion means, the template needs three setters and four
triggers — identical across all five generations — instead of per-theme branching.

⚠ The glow is an **overlay in the same grid cell, not a nested container**. A real inner border would
inset the content by 1px in every state, so the icon would visibly shift the moment the other half
was hovered. `CompanionRim` draws only its 1px edge, is `IsHitTestVisible="False"`, and binds
`CornerRadius` to `Chrome` by `ElementName` so it tracks the horizontal/vertical switch for free.

⚠ The mark is carried on `Tag`. An outer `TargetName` cannot reach a half's `Chrome`, which lives in
that half's own namescope, so the outer triggers set `Tag="Companion"` on the sibling and each half's
template reacts. `Tag` is free on these two template-private buttons and costs no public API before
the Phase 8 freeze. The companion trigger is declared FIRST in each half so the half's own
hover/press always wins when both apply — press the command half, then drag onto the chevron.

New tokens: `Control.CompanionBackground`, `Control.CompanionBorder`,
`Metrics.SplitTopCornerRadius`, `Metrics.SplitBottomCornerRadius` — four per file, all five files.
The showcase's Paste button now ships `Layout="Vertical"`, which also demonstrates the fallback:
narrow the window and it returns to horizontal as it reduces to Medium.

**Design-time.** The Ribbon Editor's property panel gained a **gated** row: `PropSpec` now takes an
optional `AppliesTo` predicate and `BuildProps` skips rows that fail it, so "Split layout" appears
only when the selected button could actually render Large. Offering `Vertical` on a button that
can never be Large would write a property with no visible effect, and the author would have no way
to tell that apart from a bug in the control.

⚠ The gate is `CanRenderLarge`, which tests `Size == Large` **OR** `SizeDefinition` mentioning
Large — not `Size` alone. The sizing engine owns `Size` whenever a definition is present, so a
`Size`-only test would hide the row on exactly the buttons most likely to want it.

`AfterPropertyCommitted` runs after a `Size` / `SizeDefinition` edit: if the button can no longer be
Large it resets a `Vertical` layout to `Horizontal` and rebuilds the panel, so the row appears and
disappears as you edit rather than only on reselect. Two deliberate limits on that: it is driven by
an explicit EDIT, never by selection (silently rewriting a property because someone clicked a node
would put a surprise entry on the undo stack), and **the runtime control never coerces `Layout` at
all** — it falls back to horizontal while remembering the author's choice, so a button that reduces
to Medium and back is unchanged.

VS Properties window: `Layout` is described under the RibbonKit category with the Large-only rule
spelled out, and the computed `IsVerticalLayout` is `Browsable(false)` — it exists for templates and
triggers, not for authoring.

### 3.44 The shared easing function was never frozen — 2026-07-28

`dotnet test` failed 9 of 59, every one of them in `PopupMotionTests`, with

> System.InvalidOperationException : The calling thread cannot access this object because a different
> thread owns it

thrown from `Clock.AllocateClock` → `Timeline.GetCurrentValueAsFrozenCore` → `Freezable.CloneCoreCommon`.
Nothing in that stack names the culprit, which is what made it worth writing down.

**Cause.** `RibbonAnimation.SharedCubicOut` — the single `CubicEase` every transition reuses — was
constructed but never frozen. An `IEasingFunction` is a `Freezable`, so an unfrozen one takes thread
affinity from whichever thread first touched the class. Starting an animation **clones the timeline
and everything hanging off it**, so any LATER thread that builds a clock trips `VerifyAccess` on the
easing function. `Sta.Run` gives each test its own STA thread, so test #1 claimed the ease and every
test after it failed.

**This was not a test-only bug.** WPF supports a second window on its own dispatcher thread, and
every RibbonKit control in such a window would have thrown on its first hover. The suite found a real
defect in code that had shipped since the animation system was built.

**Fix:** freeze it (`Frozen(new CubicEase { ... })`, with a tiny generic helper — the same idiom
`RibbonEditorWindow.DropAdorner` already uses for its pens and brushes). A frozen `Freezable` has no
dispatcher affinity at all. ⚠ **Any shared `Freezable` added to the animation layer needs the same
treatment**, and the failure will point somewhere else when it doesn't.

`PopupMotionTests.A_transition_starts_on_any_thread` runs a transition on two successive STA threads.
Every other test in the file already crossed threads — they all failed together — so that one exists
purely so the NAME says what broke.

**Separately, two of the nine were the test's own fault.** `The_surface_is_never_transformed` reported
"carries a MatrixTransform" for a Border nobody had touched, because
⚠ **`UIElement.RenderTransform` defaults to `Transform.Identity`, which IS a `MatrixTransform`** — it
is not null. The helper was type-switching (`null` / `TranslateTransform` at 0,0 / `ScaleTransform` at
1,1) and treating everything else as transformed. Now it tests the MATRIX —
`transform is null || transform.Value.IsIdentity` — which is both shorter and correct for every
transform type, including the identity one WPF hands out by default.

### 3.45 Proxies never mirrored their source's enabled state — 2026-07-28

Disabling a ribbon command in code left every COPY of it live: the quick-access proxy, its overflow
entry, and any custom-group proxy stayed enabled and still invoked the command the app had just
switched off. The ribbon button greyed exactly as intended, which is what made it easy to miss —
nothing looks broken until someone uses the copy.

`Ribbon.CreateCommandProxy` copies icon/header/ScreenTip values and wires behaviour, but nothing ever
linked `IsEnabled`. One fix there covers all three surfaces: the QAT (`AddToQuickAccess`), the
overflow flyout (`RibbonQuickAccessToolBar.GetOrCreateEntry`) and custom groups
(`RibbonCustomizePage`) all build their copies through that one factory.

**Binding to the source's `IsEnabled` picks up the COERCED value, which is what we want.** A button
inside a group the app disabled reports `false` even though its own property was never touched, so
its proxies grey with it — the case an `IsEnabledCore`-style check on the source's local value would
have missed.

**⚠ It is a `MultiBinding`, and that is the whole design.** Two independent things disable a proxy:
its source, and `IsCommandParkedProperty` — the flag for a merged source that has stepped out and
should grey like Office rather than vanish (§3.33). They cannot be separate writes to the same
property, because **assigning a value to a property that carries a one-way binding CLEARS that
binding**. The merge service's old `proxy.IsEnabled = false` would have severed the source mirror on
the first park and never restored it — a bug that only appears after an unrelated feature is used
once. Combining both inputs into a single expression removes the ordering question entirely;
`SetProxiesEnabled` now sets the park flag and lets the binding recompute.

**⚠ And parking still missed the overflow flyout, one hop further out.** An overflow entry is a proxy
of the ORIGINAL command (a proxy of a QAT proxy would mirror the mirror), so the fix above gave it
the source's enabled state correctly — but the merge service only ever sets the park flag on
elements in `QuickAccessItems`, and an overflow entry is not one. Its twin in the strip greyed while
it stayed live.

Fixed by delegating one level: `RibbonQuickAccessToolBar.GetOrCreateEntry` re-binds the entry's
`IsEnabled` to the **strip item it stands for**, replacing the source binding
`CreateCommandProxy` installed. The strip item already combines both reasons, so the entry inherits
source-disable AND parking together — and any future third reason for free. Content and behaviour
still come from the source; only the enabled state is delegated. The general shape worth keeping:
**when B is a stand-in for A, derive B's state from A, not from what A derived its own state from.**

New public API: read-only attached `Ribbon.IsCommandParked` (+ `GetIsCommandParked`), with an
internal setter. `ProxyMirrorTests` covers source-disable, restore, park/revive, the
already-disabled-at-creation case, and — the one that guards the design —
`Parking_does_not_sever_the_source_mirror`, which fails if anyone splits the two inputs back into
two writes while the other four still pass. The overflow hop is NOT unit-tested: entries are only
built inside `OnOverflowOpened`, which needs the popup and panel template parts, and the harness
deliberately never opens a real popup. It is on the manual checklist instead.

### 3.46 The real Office 2007 application menu — a control that had to sit BEHIND the orb — 2026-07-28

The second of the two things §3.38 deferred. `README.md` claimed for a long time that RibbonKit had
an "application menu"; it had the application *button* and the backstage, and the two-pane drop-down
Word 2007 actually opens did not exist. It does now: `RibbonApplicationMenu` plus three companions,
`Themes/Controls.ApplicationMenu.xaml`, 24 new tokens per theme, and one new `Ribbon` property.

#### The z-order requirement drove the whole architecture

In Office 2007 the orb sits **on top of** the menu it opened — the menu's rounded top-left corner
disappears under it. That one sentence rules out both of the obvious hosts:

| Host | Why not |
|---|---|
| `Popup` | Its own top-level HWND. It is above *everything* in the owner window by construction, and nothing in that window can ever paint over it. |
| Adorner layer (what `Backstage` uses) | A sibling visual branch inside the `AdornerDecorator` that paints above all window content — including the ribbon, including the orb. |

The first implementation hosted the orb menu inside the tab-strip row so the real button could
paint above it. Message-bar integration exposed the cost of that arrangement: promoting the nested
tab-control branch also promoted the ribbon body's shadow over later QAT/message siblings. The
shipping template now hosts **every** application menu in the Ribbon template's outer, zero-sized
`Canvas`, above those siblings but independently of the body:

```xml
<Canvas Grid.Row="0" Grid.RowSpan="3" Panel.ZIndex="2"
        Width="0" Height="0" HorizontalAlignment="Left" VerticalAlignment="Top">
    <ContentPresenter x:Name="PART_ApplicationMenuOverlayPresenter"
                      Margin="{DynamicResource RibbonKit.Metrics.ApplicationMenuMargin}" />
    <Border x:Name="PART_ApplicationButtonOverlay" Panel.ZIndex="1" />
</Canvas>
```

For a rectangular File button, the second element stays collapsed. For the Office 2007 orb, Ribbon
replaces `PART_ApplicationButton` in its stack with an inert same-size placeholder, then temporarily
moves that **real button** into the small outer host at the placeholder's measured coordinates. The
placeholder preserves tab/QAT layout; moving the original preserves its exact pixels, bindings,
focus/automation identity, hover state, and second-click toggle behavior. Closing the menu or
changing shape restores it to its original panel/index. **Do not clear or assign the button's
`Margin` while moving it.** The first reparenting pass restored the resolved Office 2007 `2,2,2,0`
value as a literal, severing the template's `DynamicResource`; switching that same Ribbon to Office
2019/2024 then left File offset instead of adopting `8,4,2,0`. The overlay host and placeholder now
include the margin in their slot geometry while the real button keeps its resource reference alive.
A focused realized-template regression changes that resource after an orb open/close and requires
the same button to adopt the new value. A nested presenter remains only as a compatibility fallback
for custom templates that omit the outer parts. The shipping template no longer promotes
`TabControlHost` for either button shape.

Two properties of `Canvas` are doing real work here, and both are the reason it is a `Canvas` and
not a `Grid` cell:

1. **A `Canvas` child contributes nothing to its parent's measure**, so a 500px-tall menu cannot
   inflate the tab strip. The `Canvas` itself is pinned to `0×0`.
2. **`Canvas` does not clip.** WPF hit-tests and renders children outside a parent's bounds as long
   as no ancestor sets `ClipToBounds`, so the menu is free to hang down over the ribbon body and the
   document. §3.41 already proved this path works — that is the same reason the ribbon's drop shadow
   needed `Panel.ZIndex="1"` on the `Ribbon` style rather than more room.

The original 2007 placement was one `Thickness` token, `ApplicationMenuMargin`, measured against
the top-left of the tab-strip row. The orb overhangs *upward* out of that row (its own margin has a
negative top), which is why the old `2,10,0,0` position put the menu's top edge below the row origin,
tucked under the orb's lower half. The cross-generation pass below keeps the same visual result but
anchors both the menu and temporarily reparented orb to the button's preserved layout slot.

#### Cross-generation placement — 2026-08-01

The first implementation merely gave the non-2007 themes flatter colours; it still placed the menu
from the tab-row origin, so their rectangular File buttons sat **under** the surface. Placement is
now anchored to the measured `PART_ApplicationButton` bounds by `Ribbon`, which avoids
duplicating the button's theme-specific height in another token and remains correct when font,
padding, DPI, or a merged-caption icon changes.

- 2007 sets `ApplicationMenuAnchorBelowButton=False` and keeps an `ApplicationMenuMargin` overlay
  offset, preserving the orb-over-frame composition.
- 2010/2013/2019 anchor directly below the button with zero margin: the button's bottom edge and
  menu's top edge meet, and an open-only button shadow makes the ownership clear.
- 2024 uses the same measured anchor plus a 6 DIP gap. Its 8 DIP outer corner is carried through the
  inner frame, top band, and footer tokens so square child fills cannot visually erase the rounding.
  Both button and menu cast restrained, independent shadows.

The menu remains in the original z=1 canvas and the button in z=2; only its coordinates changed.
Moving the presenter into the button branch would align it conveniently but would also promote the
whole menu above tab labels and reintroduce the layering bug this section originally solved.

#### One open flag, two surfaces

`Ribbon.ApplicationMenu` is new; `Ribbon.IsBackstageOpen` was NOT split in two. It now means "the
File button's surface is open", and `UpdateBackstageOverlay` returns immediately when an application
menu is assigned — no adorner, nothing hidden behind, and critically **no hiding of the title-bar
quick access strip**, which Office 2007 keeps visible under the menu. Everything the flag already
carried comes along free: the modal-tab block, the design-time route, the notch/marker re-placement
on close.

The discriminator templates need is the new read-only `Ribbon.IsApplicationMenuOpen`
(`IsBackstageOpen && ApplicationMenu is not null`). It buys exactly two trigger changes, and the
first is the one that matters:

- The orb's "hide me, the backstage is covering me" `MultiDataTrigger` gained a third condition,
  `IsApplicationMenuOpen = False`. Without it the orb would delete itself over the menu — over the
  one surface the whole layering exists to sit beneath.
- A new trigger holds the orb's pressed gel while the menu is up, so it reads as the thing the menu
  belongs to.

The File button's "hidden when there is nothing to open" `DataTrigger` became a `MultiDataTrigger`
over *both* `Backstage` and `ApplicationMenu` being null. A ribbon may legitimately carry either
one alone.

**When both are set, the menu wins.** That is deliberate: an app can keep both assigned and switch
generations at runtime by nulling one out, which is exactly what the showcase's "2007 Menu" toggle
and its `ApplyTheme` do.

#### The hover model: nav-row entry claims, empty space is neutral

This is the part with real behaviour in it. Word 2007's rules, as specified by the user and
confirmed against the captures:

- The pane shows the default page (Recent Documents) until the pointer enters a nav row that has a
  pane of its own.
- Entering a pane-bearing row claims that pane immediately.
- Leaving the row does **nothing**. The pane remains active across the separator chrome, the tiny
  nav-to-pane gap, the pane itself, and empty menu space.
- Entering another pane-bearing main row replaces it; entering a row with **no** pane (New, Save,
  Close) restores the default page.
- Closing and reopening the application menu always starts on the default page.

The original implementation deferred row-leave to `DispatcherPriority.Background`, then restored
the default unless either another row or `PART_Pane.IsMouseOver` had become true. It solved a fast
leave/enter ordering race, but made correctness depend on pointer speed: crossing the one- or
two-DIP separator gap slowly enough let the deferred evaluation run while neither surface reported
hover, collapsing the pane immediately before the pointer reached it.

There is no useful user intent in hovering separator chrome. The state machine is now smaller and
deterministic: **only main-nav `MouseEnter` mutates pane ownership**. Hover logic no longer depends
on `PART_Pane` or a dispatcher timer; the named part remains in the template contract for backward
compatibility. `ApplicationMenuHoverTests` pumps the dispatcher after row-leave to preserve the
slow-crossing reproduction and verifies the pane remains active.

**The general rule worth carrying forward: absence of hover is not an action. Treat transit space
as neutral and change selection only when the pointer enters another actionable target.**

#### Split and dropdown rows: physical hover, not logical hover

The verified visual contract is:

- A true **split** row is neutral at rest. While the pointer is physically over the row, both halves
  light and the divider appears. While its pane is being used, the arrow stays fully active and the
  separate command half stays at its theme's subdued, half-lit level; the full active outline
  remains and the divider defines the boundary between those two intensities.
- A **non-split dropdown** has two template buttons only for hit testing. Visually they are one
  opener: its active state spans the whole row, and pressing either half paints the same full-row
  pressed surface.

The original template used a control-level `IsMouseOver` trigger. That property is reverse-inherited,
and a nav item's pane content remains its **logical child** even while a separate presenter displays
it in the right column. Hovering a submenu item therefore made the left nav row report
`IsMouseOver=True`, lighting the split command half as if the pointer had returned to it.

The template now keys hover from `PART_Primary.IsMouseOver` and `PART_Arrow.IsMouseOver` — the two
actual visual hit areas. This preserves the original active split rendering
(`ApplicationMenuDimOpacity`: 0.32 in 2007, 0.35 in 2010, 0.4 in the flat themes) while preventing
submenu hover from escalating it to full intensity. Two non-split pressed `MultiTrigger`s
deliberately set **both** fills, regardless of which implementation button received the press. The
divider is shown for a physically hovered split row or its active half-lit state, never at neutral
rest.

`IsSplitPresentation` is a read-only DP = `HasPane && IsSplit`, exactly so the two halves cannot
disagree about which shape the row is — the same one-flag trick §3.43 used for the vertical split
button.

#### Three findings that cost a rewrite each

**1. `FrameworkElement.Resources` is not a `DependencyProperty`.** The group dividers were going to
be stock `Separator`s styled from a `<Setter Property="Resources">` on the menu's Style — scoped to
the menu so a theme dictionary would not restyle every separator in the consuming app. That Setter
cannot exist: `Resources` is a plain CLR property. There is no scope that works (an implicit
file-level style leaks; `ControlTemplate.Resources` is not on an inline item's lookup path; the
theme dictionary is not in the app's lookup chain at all), so the divider became its own control,
`RibbonApplicationMenuSeparator`, which gets its style from `Generic.xaml` with no lookup involved.

**2. The pane must never fall back to `DefaultContent`.** `ActivePaneContent` returns `null` when no
row is active, and the default page has its own presenter in the template. Routing the same object
through both presenters throws the moment the pane switches back — a `UIElement` has exactly one
visual parent. (The active row's `Content` is fine to present: the row is its *logical* parent and
never presents it itself, which is precisely the split `TabControl` uses for a `TabItem`'s content.)

**3. Click-outside dismissal has to know the application button.** The menu closes on
`PreviewMouseDown` anywhere outside itself. Clicking the orb is outside itself — so the menu would
close on mouse-DOWN, leaving the two-way-bound `ToggleButton` unchecked, and the toggle's own click
on mouse-UP would re-open the menu it had just closed. The orb would look dead. The button is
therefore exempt, matched by name (`Ribbon.ApplicationButtonPartName` = `PART_ApplicationButton`,
renamed from `ApplicationButton` for this) because it lives in the NESTED `RibbonTabControl`
template, which `Ribbon.GetTemplateChild` cannot reach. Same exemption `PopupDismissHelper` gives a
flyout's opener, for the same reason.

Dismissal is otherwise the house pattern: Esc (at the window as well as the menu, since focus may
still be on the button), window deactivate/move/resize, and any `ButtonBase.Click` that bubbles to
the menu unhandled. **The two cases that must not dismiss mark the click handled themselves** — a
row's arrow half, and a pane-less drop-down row — so the menu never has to reason about where a
click came from. §3.40's collapsed-group flyout learned that lesson the expensive way: a visual-tree
walk gets menu items backwards.

#### The frame

Sampled across a scan line, the border is a five-band sandwich — `#9BAFCA` / `#FFFFFF` / band /
`#FFFFFF` / `#9BAFCA` — where the band is flat 3px down the sides, an **18px gradient across the
top** (the strip the orb sits half inside, which is why the top is thicker than the sides), and the
**footer bar itself** along the bottom. Hence three nested `Border`s around a three-row `Grid`
rather than one `Border` with a fat `BorderThickness`. Both band gradients carry the 2007 valley
with its hard crease, same as the ribbon body.

Two more measured details worth keeping: the default page renders **bare** — no frame, no header
band, its heading is part of the content — while a nav row's page is **framed and gets the shaded
`#DDE7EE` band**. And `PART_Frame` deliberately carries no chrome: `RibbonMotion.PlayFlyoutOpen`
fades the surface it is handed and slides that surface's *child*, so making the invisible wrapper
the surface lets the whole visible menu glide in. Painting the border on `PART_Frame` would have
slid the menu's insides against their own frame — the third time §3.42's margin/offset headroom rule
has bitten.

#### What shipped

New public types: `RibbonApplicationMenu`, `RibbonApplicationMenuItem`,
`RibbonApplicationMenuPaneItem`, `RibbonApplicationMenuButton`, `RibbonApplicationMenuSeparator`.
New `Ribbon` members: `ApplicationMenu`, read-only `IsApplicationMenuOpen`. Tokens went **127 → 158
keys** per theme (the original 24, the later cross-generation placement/corner/shadow profile, and
the separate application-menu-open File-button surface pair).
2007's values are measured; 2010 gets a
soft-glass translation and 2013/2019/2024 flat on-palette equivalents, so assigning an application
menu under any generation is legal and looks deliberate.

**Originally deferred:** KeyTips for the menu, arrow-key navigation down the column, and a
scrolling pane. KeyTips shipped with the later QAT/application-menu keyboard-access fix; arrow-key
navigation and a scrolling pane remain additive work and neither changes the geometry.

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

### 3.62 Responsive Ribbon Editor + application-menu authoring — VERIFIED IN VS, 2026-08-08

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

### 3.65 Repeatable Ribbon message bar — 2026-08-08

`RibbonMessageBar` is a lookless `ItemsControl`; ordinary `Items`/`ItemsSource` collection semantics
determine how many rows are present. For the connected Office presentation it is assigned through
`Ribbon.MessageBar`, which hosts it as the ribbon template's third row. A separate top-docked sibling
cannot alter the inner 2007/2024 card or below-ribbon QAT geometry and therefore leaves rounded feet,
shadows, and bottom gaps visible. The integrated path consumes `HasOpenMessages` to square the
preceding surface and remove only its lower gap: the ribbon/QAT keeps its depth shadow at the join,
while the complete message stack casts a second shadow from its final edge. When the last row closes,
the normal ribbon/QAT foot geometry returns.
The vertical items panel stacks every open `RibbonMessage`; one message closing does not disturb the
state of its siblings.

Each `RibbonMessage` exposes `Title`, `Message`, optional `Icon`, action content/command/parameter,
two-way-by-default `IsOpen`, and `IsDismissible`. The action and dismissal paths publish bubbling
`ActionClick` and `Dismissed` routed events, while `Dismiss()` is idempotent for code-driven closure.
The shared template provides the shield, wrapped text, action button, localized Close tooltip, and
per-item visibility triggers. Dedicated UI Automation peers expose the bar as a pane and each
polite live-region message as text named from its title/body. Four matching brush tokens exist in every light/dark generation;
historical dark themes retain the readable light warning surface while modern dark themes use a
muted dark ochre surface. Office 2007 uses a period-correct hard glass crease and Office 2010 a
smoother four-stop glass face; the flat generations retain restrained solid fills. The connected
bar mirrors the generation's horizontal card inset (7px in 2024, 2px in 2007), and the final open
row receives the theme's lower corner token (8px in 2024, 3px in 2007). Rows use a compact 34px
minimum with smaller action/close chrome, while wrapped text can still grow naturally. Inset
generations also carry explicit side outlines on every row, closing the combined card rather than
leaving yellow fill against an unbordered edge. The Close target reserves a transparent rounded
1px outline at rest and resolves a dedicated theme border on hover/press; this contains the heavy
2007/2010 state wash instead of rendering the large borderless rectangle seen in the first pass.
The target is 22×22 with an 8px trailing inset, matching the restrained Office 2024 close affordance
without letting its hover outline crowd the message card edge. Its diagonal glyph receives a
half-DIP optical offset down and right so anti-aliasing does not make the mathematically centered
path appear high-left beside the action button.

Message appearance/dismissal uses the dedicated `RibbonAnimationAction.MessageBar`: a restrained
160ms opacity/6-DIP vertical transform at the subtle level, with the normal expressive multiplier
and instant reduced-motion path inherited from `RibbonAnimation`. `IsOpen` remains the immediate
logical/bindable state, while read-only `IsPresented` keeps an exiting row, the stack shadow, and
the ribbon connection alive until `RibbonMotion.PlayClose` completes. No height, margin, or other
layout property is animated, and the translated template root is clipped to its row so stacked
messages never paint across each other. Rapid close/reopen changes invalidate the older completion callback,
so a newly reopened row cannot be collapsed by a stale exit. The Showcase and RTL lab now begin
with both sample rows closed; their ribbon-level Add Message commands reveal or reopen the next row
one at a time, making repeatability and both transition directions directly testable.

The first live entrance exposed the same intermittent destination-frame flash documented in
§3.41. The row became presentable and then queued `PlayOpen` at `DispatcherPriority.Loaded`, which
runs after Render; depending on frame timing, the fully opaque/resting row could be presented before
the queued animation rewound it. `BeginShow` now starts motion synchronously in the same dispatcher
turn as `IsPresented`, and shared `RibbonMotion.PlayOpen` seeds both base opacity and translation
before installing their clocks. Both animations self-release to ordinary resting values on
completion, so seeding cannot leave a row invisible or displaced if a later caller clears a clock.
The regression contracts pin the seeded start and prohibit another dispatcher hop in the message
entrance path.

A second first-use-only gap remained: rows authored closed can stay `Collapsed` without ever
instantiating their control template, so the initial `BeginShow` had no `PART_Root` for `PlayOpen`
even though later reopens did. The row now records a pending entrance, requests `ApplyTemplate`, and
lets `OnApplyTemplate` consume that request synchronously once the root exists. If realization is
deferred until layout, the row still has no visual to flash; the pending animation is installed
inside `OnApplyTemplate` before the new root can render. Close/unload clears the request, and the
already-realized reopen path remains immediate.

Template realization also raises `Loaded` for that first reveal. Without coordination,
`OnApplyTemplate` consumed the pending entrance and the following `OnLoaded` called `BeginShow`
again, producing a short first pass followed by a full restart. A per-open handled flag now makes
pending-template consumption and `Loaded` mutually exclusive animation owners. A logical close
resets the flag for the next open; No Motion marks the entrance handled while snapping to rest.

The first template attachment can itself produce an `Unloaded`/`Loaded` pair while `IsOpen` remains
true. Resetting the handled flag from `OnUnloaded` therefore still authorized a second entrance:
the first animation was cancelled quickly by `Rest`, then `Loaded` replayed it at full duration.
`BeginHide` is now the sole handled-flag reset point. `OnUnloaded` clears only an unrealized pending
request and rests the departing visual, preserving the one-animation-per-logical-open invariant
across template churn.

The persistent first-show restart ultimately exposed a separate control-flow error in
`OnApplyTemplate`, not another lifecycle race. Its original compound condition was
`if (IsOpen && !TryPlayPendingEntrance()) ... else if (IsPresented) BeginHide()`. When a pending
entrance was successfully consumed, the negated call made the whole condition false, so the attached
`else if` immediately entered `BeginHide` even though `IsOpen` was still true. That cancelled the
first clock, reset the per-open guard, and let `Loaded` start the normal entrance again. The open and
closed cases are now separate outer branches; successful pending consumption can no longer fall
through to the close path. A focused STA regression realizes a pending-open template and verifies
that it remains presented, reproducing the old failure without relying on frame timing.

Application-menu integration exposed one cross-branch depth leak. The original menu-open trigger
promoted the complete `TabControlHost` above the later QAT/message rows because every menu lived
inside the nested tab-strip template. That promotion also raised `ContentHost` and its ribbon-card
shadow; Office 2024's 12px shadow consequently painted a dark band over the QAT and open message.
Suppressing `ContentHost.Effect` removed the leak but also removed wanted depth, so the final fix
moves every shipping application menu into a direct Ribbon-template overlay above the QAT/message
siblings; the `TabControlHost` and its unchanged shadow stay at their normal depth. For the
historical orb, a measured placeholder preserves its nested layout slot while the real button is
temporarily reparented above the outer menu. Ribbon assigns the single menu object to exactly one
presenter, with the nested presenter retained only as a custom-template fallback. The combined 2024
rectangular and 2007 orb menu/message visuals are focused approvals rather than relying on isolated
scenes to imply correct
cross-branch ordering.

The Showcase and Localization/RTL lab each place two independently actionable rows through the
integrated property. The RTL lab uses mixed Arabic/English title, body, and action text and verifies
that logical start content mirrors right while action/Close move left. Seven focused logic contracts cover
the public control shape/defaults, idempotent aggregate/last-open state, connected template geometry,
pending-template entrance ownership, application-menu shadow isolation, vertical item parts/states,
ten-palette token parity, and initially empty/add-from-ribbon Showcase paths. Focused approvals cover
connected 2007/2010/2024 stacks, the mirrored 2024 stack, and the 2024 rectangular plus 2007 orb
open-menu/message compositions. Current automated baseline: 182 logic tests and 56 approved images.

### 3.66 Dark/Black Backstage rails use generation-matched neutrals â€” 2026-08-08

The Classic (2013-style) Backstage rail previously bound its entire navigation column directly to
`Accent`, so every dark generation retained a saturated blue slab even when the surrounding palette
was neutral. The shared template now consumes `Backstage.Classic.NavBackground`. All light themes
publish their established accent color and `ThemeManager.SetAccent` continues to derive it there;
all dark/Black overlays publish a matching neutral surface instead. The dark Classic hover and
selected-row brushes are neutral as well. Custom accents deliberately stop at the rail boundary in
dark mode, while page headings, the File button, and other intended accent consumers remain colored.

Office 2010/2007 Black had a separate gap: their overlays inherited the base generation's pale-blue
`Backstage.NavBackground` and blue selected glass for the Classic2010 design. Both overlays now
replace the rail, selected glass, and selected border with grayscale recipes. Office 2010 preserves
its smooth two-stop rail and radial selection glow; Office 2007 preserves its four-stop hard crease.
The blue glass back button remains the intentional accent affordance rather than part of the neutral
navigation sheet.

Nine focused logic cases pin shared-template routing, ten-palette token parity, strict grayscale
colors, and the distinct 2010/2007 gradient profiles. Three reviewed approvals cover Office 2013
Dark Gray Classic plus Office 2010/2007 Black Classic2010. Current automated baseline: 194 logic
tests and 59 approved images; the full Showcase-height live check remains useful for final tuning.

### 3.67 Compact RibbonCheckBox and RibbonRadioButton — 2026-08-08

The first pre-freeze input-control slice adds `RibbonCheckBox` and `RibbonRadioButton` as lookless
subclasses of WPF's real `CheckBox` and `RadioButton`. They deliberately have one compact form, like
`RibbonComboBox`, rather than artificial Large/Medium/Small layouts. Both expose RibbonKit's familiar
string `Header` plus rich ScreenTip properties; when Header is absent, the shared template falls back
to ordinary WPF `Content`, preserving standard content syntax. Native command, routed-event,
keyboard, three-state check-box, `GroupName`, and radio exclusivity behavior remain inherited.

The shared button dictionary supplies theme-aware square/check and circular/radio indicators,
keyboard-focus outlines, disabled state, and reduced-motion-aware hover/press washes. Existing accent,
text, surface, and interaction tokens carry the structure; the only new token is
`RibbonKit.Brushes.Input.Glyph`, defined by all ten light/dark generation dictionaries so checked
marks remain legible on the accent fill. No layout property is animated. The check template also
covers the indeterminate state.

Dedicated UI Automation peers retain the native CheckBox Toggle and RadioButton SelectionItem
patterns while naming controls from Header. KeyTip invocation now consumes SelectionItem after
Invoke/Toggle, so a radio KeyTip selects the target and applies normal `GroupName` exclusivity rather
than merely raising a routed click. The Visual Studio toolbox, surface context verbs, responsive
Ribbon Editor Add menu, friendly tree names, reorder/delete metadata, and option-specific property
rows all include both controls. The Showcase View tab has a compact Inputs group for direct theme,
hover, keyboard, ScreenTip, KeyTip, and mutual-exclusion checks. They are intentionally not promoted
to QAT/customization icon-command candidates in this slice.

Six focused logic tests cover lookless inheritance/style keys, ScreenTips, UIA patterns/names,
KeyTip behavior, ten-palette token parity, templates, and designer/toolbox wiring. Two deterministic
Office 2024 light/dark approvals cover checked, unchecked, indeterminate, selected, unselected, and
disabled states. Current automated baseline at this point was 200 logic tests and 61 approved images.

The first live hover/focus check exposed one template-layering mistake: `Chrome` owned the content
padding, so WPF inset every child—including `HoverWash` and `PressWash`—while the focus visual still
used the complete control bounds. Classic themes made the smaller amber box especially conspicuous.
`Chrome` is now unpadded and the mathematically equivalent `5,1,5,1` inset lives only on the named
content grid. Resting indicator/text geometry and desired size are unchanged; interaction washes now
fill the complete focus rectangle. The template contract pins that separation for both controls.

### 3.68 Compact RibbonTextBox — 2026-08-08

The next pre-freeze input slice adds `RibbonTextBox` as a lookless subclass of WPF's real `TextBox`,
again with one compact form rather than artificial ribbon sizes. `Header` supplies an optional label,
`InputWidth` sizes only the editor chrome, and the standard `Text`, selection, caret, validation,
binding, command, keyboard, scrolling, and IME contracts remain native. `IsReadOnly` therefore still
allows selection and copying. Rich ScreenTips and KeyTips follow the same surface conventions as the
other compact inputs; a text-box KeyTip transfers focus without changing its text or selection.

The shared dropdown/input dictionary reuses the existing control-surface, border, primary-text,
secondary-text, accent, and corner-radius resources, so no new palette token was required. Its
24-pixel input chrome matches `RibbonComboBox`; hover changes only the border and keyboard focus uses
the accent border, with no animation or layout-property transition. The required `PART_ContentHost`
remains the native `ScrollViewer`, preserving WPF editing behavior across all ten themes.

A dedicated automation peer retains Edit identity and the Value pattern while deriving its accessible
name from `Header`. The Visual Studio toolbox, group context verb, responsive Ribbon Editor Add menu,
friendly tree name, reorder/delete metadata, and property rows cover `InputWidth`, `Text`, `IsReadOnly`,
and `MaxLength`. The Showcase adds editable and read-only fields beside the option controls.

Five focused logic tests pin lookless inheritance/style key, native state and ScreenTips, UIA name and
Value pattern, KeyTip focus, required template part/resources, and designer/toolbox wiring. The two
existing Office 2024 light/dark input approvals now also cover editable, read-only, and disabled text
fields, keeping the visual corpus at 61 images. The initial automated baseline was 205 logic tests and
61 approved images. A ribbon slider was considered after this slice and is intentionally not planned.

The live follow-up passed at 100/125/150/175/200% DPI in both the Office 2024 and classic visual
profiles. RTL follow-up adds an `RTL Inputs` group to the Localization/RTL lab with inherited mixed
Arabic/Latin content plus a left-aligned Latin document identifier inside a still-mirrored control.
The checklist explicitly distinguishes component mirroring from inner text direction. A source
contract prevents either field from forcing the entire control LTR, and a focused Office 2024 RTL
approval pins group order, leading option indicators, and label/field reversal. The interactive RTL
caret, selection, mixed-direction text, explicitly LTR document identifier, and KeyTip focus checks
then passed in the live lab. Current automated baseline: 206 logic tests and 62 approved images.

### 3.69 KeyTip resolution is typeable and prefix-free — 2026-08-08

The per-level resolver now guarantees that every non-empty badge is independently typeable. The old
`AutoAssign` reserved only exact strings, so duplicate explicit keys never activated and an explicit
pair such as `F` / `FN` left the shorter key permanently waiting behind the longer prefix. Explicit
assignments are now reserved before automatic derivation; the first conflicting explicit assignment
in visual order wins, while later exact or prefix collisions fall back to their label and then the
standard fallback alphabet. Automatic keys avoid every reserved prefix as well as exact duplicates.

Resolution also now matches the actual input state machine. `KeyToChar` accepts A–Z and 0–9, but the
old derivation accepted any Unicode letter or digit, producing unreachable badges for fully Arabic
and other non-Latin labels. Authored keys are trimmed, normalized, and accepted only when every
character is typeable; label derivation skips non-typeable characters and uses the ASCII fallback
when necessary. The public `KeyTip.Keys` documentation records the typeable character contract and
the deterministic first-explicit-wins collision policy.

Ten focused cases cover explicit reservation/case normalization, same-initial label derivation,
exact and both directions of prefix collision, prefix-free automatic assignment, unlabeled fallback,
non-Latin labels, and untypeable explicit values. Full validation is green at 216 logic tests plus
the one visual test covering 62 approved images.

### 3.70 Explicit KeyTips inside arbitrary File-surface content — 2026-08-08

Backstage pages and application-menu panes are intentionally arbitrary content, but their KeyTip
levels previously recognized only surface navigation plus RibbonKit's two built-in application-menu
content controls. Setting `rk:KeyTip.Keys` on an ordinary WPF `Button`, toggle, text input, or custom
automation-aware `UIElement` inside a user-authored page therefore serialized correctly but produced
no badge.

Both File surfaces now share an explicit-content discovery rule. The walker follows only realized,
visible visual-tree branches, so a selected Backstage page and the active/default application-menu
pane contribute targets while hidden pages do not. Application-menu pane/footer items retain their
existing automatic indexing. Any other `UIElement` opts in only through a non-blank `KeyTip.Keys`,
which prevents arbitrary page layouts from being flooded with derived badges. Navigation rows are
excluded from the content walk because their own primary/arrow targets are registered separately;
the combined level then passes through the same prefix-free resolver from §3.69.

Selecting a Backstage page by KeyTip now keeps the terminal level active and rebuilds it at
`DispatcherPriority.Loaded`, after the selected-content presenter realizes the new page. This mirrors
the application menu's existing pane-refresh rule. The work also fixes Backstage action items:
`IsButton=True` now invokes the item's Click/Command path under KeyTips instead of trying to select
the action as a page.

Disabled targets remain visible in a stable KeyTip level but are never invoked. The activation state
machine checks effective `UIElement.IsEnabled` before changing levels or dismissing KeyTip mode, then
clears the typed prefix so another badge can be chosen. The shared `InvokeControl` helper repeats the
guard because QAT/customization proxies also call it directly; effective WPF enablement covers both a
locally disabled control and disablement inherited/coerced from a parent or command source.

The live **Disable Samples** follow-up exposed a separate invocation mismatch: UI Automation's
`Toggle`/`SelectionItem` patterns update checked/selected state but do not run
`ButtonBase.OnClick`. The toggle therefore looked checked under its KeyTip while its XAML `Click`
handler never disabled the sample controls. `RibbonToggleButton`, `RibbonCheckBox`, and
`RibbonRadioButton` now expose an internal KeyTip activation hook that calls their real `OnClick`
path. KeyTips therefore update state, raise routed `Click`, and execute `Command` in the same order as
mouse or Space activation; UIA peers remain unchanged for external automation clients.

The compact-input live pass then exposed a discovery mismatch: `RibbonCheckBox`,
`RibbonRadioButton`, and `RibbonTextBox` already had invocation support and authored Showcase keys,
but the root-level collector's ribbon-control type list predated those controls. They now participate
in normal ribbon KeyTip discovery, with auto-derived labels taken from `Header`; text-box activation
continues to transfer focus without changing the value.

A collapsed-group follow-up exposed one more lifecycle distinction: invoking any leaf previously
tore down every non-persistent KeyTip level, whose `OnExit` closes the collapsed-group flyout. That is
correct after commands but made a text-box KeyTip focus an editor that immediately disappeared. Leaf
teardown now preserves only the activated level for editors and nested-picker openers (`TextBox`,
`ComboBox`, and `InRibbonGallery`) while still removing its badges and ending KeyTip mode. Buttons,
checkboxes, and radio buttons retain normal command dismissal; Escape or light-dismiss still closes
the preserved flyout afterward.

The Showcase demonstrates both extension points with ordinary WPF buttons: **Create custom draft**
(`CD`) in the Backstage Home page and **Manage recent locations** (`MR`) in the application menu's
default pane. Six focused cases cover visible explicit Backstage targets, built-in plus explicit
application-menu targets, ordinary-button invocation, Backstage action invocation, and inherited
disabled-state blocking, plus native toggle Click/state behavior. Automated baseline: 222 logic tests
plus the one visual test covering 62 approved images; the two live examples remain the focused
interactive verification surface.

Two additional compact-input cases pin checkbox, radio-button, and text-box participation,
Header-based label derivation, and the containing-flyout preservation boundary. Current automated
baseline: 224 logic tests plus the one visual test covering 62 approved images.

### 3.71 Showcase appearance preferences stay separate from ribbon customization — 2026-08-10

The Showcase now persists its user-selected appearance in
`%LocalAppData%\RibbonKitShowcase\appearance-preferences.json`. This is deliberately a second file,
not an expansion of `RibbonCustomizationSerializer`: importing or resetting tab/group/QAT structure
must not unexpectedly change the application palette, swap its File surface, or activate an
operating-system window material.

`ShowcaseAppearancePreferences` is an app-owned, schema-versioned record covering the Office
generation, nullable custom accent (`null` means the theme default), dark variant, colored title bar,
Backstage design/translucency, Backstage versus application-menu surface, and one mutually-exclusive
None/Mica/Acrylic backdrop preference. Enums serialize as readable strings and accents normalize to
`#AARRGGBB`; corrupt JSON, unknown enum values, invalid colors, and future schema versions fall back
to the factory appearance without partially applying state.

Restore order is part of the contract: apply the theme first, then dark/accent/title-bar state, then
the Backstage design and explicit File-surface choice, and finally request the DWM backdrop. Theme
selection still chooses its conventional live surface (2007 menu for Office 2007, Backstage for the
other generations), but the saved explicit surface wins during startup. The stored backdrop is the
user's requested preference rather than only the last successful DWM state: on an unsupported
Windows build the toggle truthfully reverts off while the preference remains available for a later
supported launch. `ThemeManager.IsTitleBarBackdrop` is derived runtime state and is not serialized.

The runtime library gains no public API from this sample feature. Thirteen focused cases cover
complete JSON round-trip, factory defaults, schema/corruption rejection, enum rejection, and stable
accent normalization. Current automated baseline: 237 logic tests plus the one visual test covering
62 approved images.

### 3.72 Customization serializer round-trip and foreign-JSON hardening — 2026-08-10

A dedicated `RibbonCustomizationSerializerTests` suite now exercises the real public
`Serialize`/`Apply` boundary against separately constructed ribbons, rather than only checking the
merge/modal exclusions around it. The complete round trip covers built-in tab/group reorder and
renaming, authored visibility, custom tabs/groups, custom layout, renamed and resized button/toggle/
drop-down proxies, explicit and auto-derived command identities, borrowed icons, declared plus proxy
QAT ordering, QAT placement, newly shipped/contextual/id-less content preservation, missing-command
skips, baseline Reset, repeated Apply idempotence, and older JSON without `QuickAccessPosition`.

Two real defects fell out of that coverage:

- A syntactically valid but unrelated JSON object deserialized into an empty DTO because
  `System.Text.Json` ignores unknown properties. Applying `{}` or `{"theme":"dark"}` therefore
  stripped custom tabs/groups and cleared the QAT. `Apply` now verifies the root signature contains
  array-valued `Tabs` and `QuickAccess`, validates nested collections and enum values, and returns
  before unmerge/reconciliation for foreign, null-shaped, or otherwise invalid documents.
- `BuildIdentity` registered a custom group's borrowed icon under the custom group's own id before
  `FindIconId` ran. Depending on traversal order, serialization stored that self-reference; a fresh
  ribbon could not resolve it because the custom group did not exist until reconstruction. Custom
  groups no longer seed the icon identity map, so the stable built-in command/group id wins.

Twenty focused cases cover these contracts. Current automated baseline: 257 logic tests plus the
one visual test covering 62 approved images.

### 3.73 Reduction thresholds, priority order, and malformed-width coverage — 2026-08-10

The remaining adaptive-layout test gap is closed with 37 focused cases: 29 against the pure
`ReductionAlgorithm` and eight STA measurements through the real `RibbonGroupsPanel`. The panel
cases pin explicit priority (highest first), rightmost tie-breaking, unprioritized largest-first
ordering, fixed-group exclusion, the complete ResizeThenCollapse state map, and cache invalidation/
reprobe after a runtime width change.

The pure cases add exact-fit, fractional-DIP, equal/nearly-equal/non-monotonic state, empty/duplicate
custom order, null/empty table, invalid index, and invalid measurement coverage. Two hardening changes
were warranted:

- Layout comparisons now use a scale-aware epsilon equivalent to WPF's internal double-comparison
  pattern. A conceptual exact fit such as `0.1 + 0.2 == 0.3` no longer crosses a reduction threshold
  because of binary floating-point noise, and a near-identical probed state is skipped until a
  genuinely narrower state is found. Meaningful DIP differences and deliberately non-monotonic
  sequences retain the existing behavior.
- Public inputs are validated before the positive-infinity/empty fast paths: available width must be
  non-negative (positive infinity remains supported), every state width must be finite/non-negative,
  each group must expose a state, the combined large width must remain finite, and every reduction
  index must exist. The public XML documentation now records these exceptions.

No `RibbonGroupsPanel` ordering or cache behavior needed correction. Twenty-five new cases raise the
current automated baseline to 282 logic tests plus the one visual test covering 62 approved images.

### 3.74 Visual Studio debugger distorted live-resize performance — 2026-08-10

The initial screen-recording comparison appeared to show that RibbonKit resized materially more
slowly than Word. A resize-only shadow-suppression path and, later, lightweight body/QAT/message proxy
shadows were explored. Follow-up testing isolated the decisive variable: the slowdown occurred only
while the Showcase was attached to Visual Studio's debugger. Running with Ctrl+F5 or launching the
built executable removed the slowdown with opaque, Mica, and Acrylic backgrounds.

The workaround was therefore removed completely. RibbonKit has no `IsLiveResizing` public state or
native sizing hook, no resize-specific shadow switching, proxy geometry, or theme tokens, and no
resize-only marker/notch coalescing or application-menu placement guard. The normal themed
`DropShadowEffect` remains present throughout interactive resizing, preserving the intended visual
language without adding debugger-driven API and template complexity before the API freeze.

**Verification rule:** assess perceived WPF resize performance outside the debugger. Visual Studio's
managed debugger, XAML Hot Reload, and diagnostic tooling can materially alter UI-thread, layout, and
render cadence. Debug runs remain useful for correctness; Ctrl+F5 or the built executable is the
appropriate baseline for interaction performance. The current automated baseline is 283 logic tests
plus the one visual test covering 62 approved images.

### 3.75 Phase 8 API review and freeze — 2026-08-10

The v1 runtime surface is now frozen. `RibbonKit.csproj` references
`Microsoft.CodeAnalysis.PublicApiAnalyzers` 5.6.0 as a private analyzer and marks its compatibility,
nullability and baseline-integrity diagnostics as errors for both `net8.0-windows` and
`net9.0-windows`. `PublicAPI.Shipped.txt` captures the reviewed 1,094-line nullability-aware surface;
`PublicAPI.Unshipped.txt` starts empty except for `#nullable enable`. Compiler errors also enforce
missing public XML docs and broken `cref` references. Future compatible additions belong in the
unshipped file and must be reviewed deliberately; the shipped file is not edited to disguise a
breaking change.

The review retained the intentional WPF extension surfaces: lookless controls and their automation
peers, dependency/routed-event identifiers, layout panels used by templates, `IRibbonSizeAware`,
theme/localization/backdrop APIs, and the public animation primitives that let application-authored
controls honor RibbonKit's motion policy. The analyzer confirms that the surface has no oblivious
reference types and is identical across the two runtime TFMs.

One behavior needed correction before freezing. `Ribbon.AddToQuickAccess(FrameworkElement)` formerly
accepted an unknown control and fell into `CreateCommandProxy`'s generic UIA button, producing a
blank or misleading QAT entry even though the catalog never offered that type. It now returns
`false` unless the source is a `RibbonButton`, `RibbonToggleButton`, `RibbonSplitButton`, or
`RibbonDropDownButton` (and still returns false for a duplicate). The generic internal fallback
remains available to overflow a hand-declared QAT element without widening the supported automatic
projection claim. A test pins rejection of groups, combo boxes, galleries and arbitrary WPF buttons;
richer group/gallery/combo representations remain the post-v1 work recorded below.

The same zero-warning pass fixed two broken runtime XML-doc references and four real nullable-flow
warnings in the net472 design tools without changing their behavior. Verification: Release solution
build zero warnings/errors; 283 logic tests green; the visual test green across all 62 approvals.

### 3.76 Repository-native v1 documentation gate — 2026-08-10

The v1 documentation deliberately stays in the GitHub repository rather than introducing a separate
site and deployment stack for a single WPF control library. `README.md` is the maintained public
entry point: it now links directly to the getting-started, feature, theming, design-tool,
documentation and roadmap sections; records the current source-reference path before the v1 NuGet
package is published; and makes the first XAML sample pasteable by omitting application-specific icon
resources. It also explains that Office 2024 is the default theme and routes readers by task into the
Showcase and focused Markdown references.

The existing four screenshots already cover the useful product story—current ribbon, Backstage,
historical theming and the Visual Studio Ribbon Editor—so no decorative or redundant captures were
added. Screenshot generation remains available when a future feature needs visual explanation. The
obsolete documentation-site feature/roadmap entries were replaced with the completed repository-docs
gate. Link, image and sample validation are part of this gate; API-reference generation remains a
possible post-v1 addition rather than a release dependency.

### 3.77 NuGet and Showcase identity icon — 2026-08-11

The release package and executable now share a purpose-built RibbonKit identity. The deterministic
`assets/RibbonKit.svg` master uses a compact folded-ribbon `R` that remains recognizable at Windows
taskbar sizes; `RibbonKit.png` is the NuGet package icon and the multi-resolution `RibbonKit.ico`
contains 16 through 256 px frames. The Showcase embeds the ICO as its executable icon and assigns it
directly to `MainWindow`. Because `RibbonWindow` replaces the native caption, its shared template now
also binds `Window.Icon` into a 16-DIP leading image; setting the property alone populated the taskbar
but left the custom title bar empty. The icon remains visible when Backstage hides title-bar QAT
content, while a null icon collapses without leaving a new gap. When a hosted ribbon explicitly uses
the Office 2007 `Orb` application-button shape, it internally registers that state with its owning
`RibbonWindow`; the caption icon then collapses and returns its width to the QAT because the orb
already owns application identity. This is keyed to the actual shape rather than the theme, keeps
`Window.Icon` intact for the taskbar/executable, handles shape changes and unload/reparent cleanup,
and adds no post-freeze public API. NuGet's packaged README was already wired; SourceLink and final
package-content validation remain next.

### 3.78 Office 2007 application-menu open layout stability — 2026-08-11

Opening the two-pane menu could nudge the complete ribbon downward by a few DIPs. This was specific
to the orb path, not the new caption icon or Windows 11 maximize hook: the menu moves the real orb
above its outer overlay and leaves a placeholder in `ApplicationButtonLayer`, while rectangular File
tabs never take that path. The placeholder was initially sized from the button's `ActualHeight` plus
margin and then repeatedly recalculated after the button had moved into a differently constrained
overlay. That created a layout feedback loop; Office 2007's negative orb overhang made the changed
tab-row contribution visible.

The placeholder now reserves the button's pre-reparent `DesiredSize`, which is exactly the size
reported to its original panel and already includes margin, and keeps that reservation stable until
the button returns. Menu/overlay placement continues to follow the arranged placeholder, but no
longer writes post-reparent measurements back into the ribbon's layout. A focused STA case pins the
difference between desired layout contribution and a stretched post-arrange `ActualSize`.

The first correction stabilized the ribbon but exposed the other half of the distinction: giving the
placeholder an explicit desired height centered its origin inside the taller tab row, so the overlay
followed that origin and the orb itself moved down on open. The placeholder now uses its desired
height as a minimum while retaining stretch alignment, preserving the original slot origin. The
overlay separately keeps the pre-reparent arranged size (`ActualSize` plus margin), so the real orb
neither shrinks nor moves. The deterministic open/closed scene now pins the application button's X/Y
origin in addition to the ribbon, body, QAT and message-bar geometry.

### 3.79 Source Link and symbol package verification — 2026-08-11

Source Link is complete without an explicit `Microsoft.SourceLink.GitHub` dependency. RibbonKit is
built with the .NET 8-or-newer SDK, which supplies GitHub Source Link automatically; adding the
provider package would only override the SDK's bundled implementation. The existing
`PublishRepositoryUrl`, `IncludeSymbols`, and `SymbolPackageFormat=snupkg` settings are therefore the
intentional release configuration.

A clean Release pack was inspected rather than inferred from a successful build. Its NuSpec carries
the canonical repository URL, `git` type, branch, and exact commit. The `.snupkg` contains one
portable `RibbonKit.pdb` for each of `net8.0-windows` and `net9.0-windows`; both PDBs contain exactly
one Source Link record mapping all repository documents to the same commit-pinned
`raw.githubusercontent.com/Wraith1080/RibbonKit` URL. The generated assembly informational version
also includes that commit. Final package-content/consumer validation remains the next release item.

### 3.80 Portable local package output — 2026-08-11

The runtime project no longer sends every local pack to the original developer's machine-specific
`E:\NuGet` directory. Its default `PackageOutputPath` is now the repository-relative `artifacts/`
directory, matching the location already used explicitly by GitHub Actions. A plain
`dotnet pack src/RibbonKit/RibbonKit.csproj -c Release` therefore has the same discoverable output
location on any checkout while callers can still override it with `--output` when needed. Release
versioning and final package-content/consumer validation remain next.

### 3.81 Deterministic package versioning — 2026-08-11

Package versions no longer depend on the wall clock. The former
`1.0.0-dev-<yyyyMMddHHmm>` expression could give separate build and `--no-build` pack evaluations
different identities, made identical source commits produce differently named packages, and
prematurely presented routine local output as the v1 line. The project now composes its documented
current version from `VersionPrefix=0.1.0` and `VersionSuffix=alpha.1`, so normal builds and packs
consistently produce `0.1.0-alpha.1` and the assembly informational version adds only the Source Link
commit metadata.

Release automation may still set the standard `Version` MSBuild property explicitly (for example,
`-p:Version=1.0.0`) when a validated release is intentionally prepared. No tag-derived or automatic
publishing behavior was added. Final package-content and clean-consumer validation remain next.

### 3.82 NuGet package and clean-consumer release gate — 2026-08-11

`eng/Validate-Package.ps1` is now the repeatable post-pack gate. It requires exactly one matching
`.nupkg`/`.snupkg` pair, allowlists the package layout, rejects duplicate entries, source leakage,
PDB duplication and unexpected consumer dependencies, and verifies the license, readme, icon,
repository/commit, target-framework groups, toolbox manifest and matching symbol-package version.
Both runtime assemblies, XML documentation files, design-tools copies and portable PDBs must be in
their exact framework-specific locations.

The same gate creates a temporary package-reference-only WPF consumer with `nuget.org` and every
other feed cleared, restores RibbonKit solely from the local `artifacts/` directory into an isolated
package cache, and compiles real `urn:ribbonkit` XAML against both `net8.0-windows` and
`net9.0-windows`. The temporary project is removed after success and preserved on failure for
diagnosis. The local `0.1.0-alpha.1` validation completed with zero warnings or errors.

CI runs this gate after packing and retains both the main and symbol packages in its existing build
artifact; this does not publish either package to NuGet. Final live runtime/performance and Visual
Studio installed-package designer validation remain next.

### 3.83 Final live performance and installed-package pass — 2026-08-11

`eng/Measure-ShowcasePerformance.ps1` now captures the Release Showcase baseline by launching the
built executable directly, never through the Visual Studio debugger. Five process-start-to-input-idle
runs measured 711.88–747.12 ms (727.66 ms median). After one 160-resize cache-warmup sweep, three
identical 160-resize passes averaged 16.081 ms of process CPU per resize and 63.61% of one core. The
Showcase's large all-features visual tree allocated its expected WPF/render caches during warmup;
across the subsequent 480 resizes, working set and private memory changed by 10.12 MiB and 10.41 MiB
respectively rather than repeating the initial allocation on every pass. Results are written only to
ignored `TestResults/performance/showcase-release.json`; the live-GUI baseline is intentionally not a
machine-independent CI threshold.

The package validator's generated consumer is now a real executable as well as a compile probe. Its
`App.xaml` explicitly merges `Tokens.Office2024.xaml`, the complete `Office2024.xaml` shared-control
aggregator, and `Mdi.xaml`, so package URI and visible styling failures cannot hide behind the
assembly's implicit default theme. With `-RunConsumer`, it opens the package-installed RibbonWindow,
renders Backstage, opens and closes that surface, performs 120 width/layout changes, forces managed
collections, records a local JSON result, and exits. The final styled run reached `ContentRendered`
in 996.05 ms and retained 71,152 bytes (about 69 KiB) of managed memory across the exercise; both
target frameworks still compiled with zero warnings and errors from the isolated local package feed.

Visual Studio Community 2026 18.7.1 then opened that same package-only project from a clean IDE
process. Its isolated `WpfSurface` loaded `RibbonKit.dll` 0.1.0-alpha.1 from the designer cache, and
the live automation tree exposed the RibbonWindow, ribbon, tab, group, and buttons. Selecting the
ribbon produced the packaged design-tools commands (`Edit Ribbon…`, `Add Tab`, application-menu and
QAT actions); invoking `Edit Ribbon…` opened the net472 Ribbon Editor, rebuilt the one-tab model and
logged `RibbonEditorWindow: ready`. The user confirmed both the styled surface and editor visually.
At that point the final performance/install gate was complete and community launch was the remaining
Phase 8 item; §3.84 records the subsequent distribution decision.

### 3.84 Local v1.0.0 GitHub-release candidate — 2026-08-11

The public-launch policy changed at the user's direction: RibbonKit will not be submitted to
NuGet.org. If public distribution is requested later, the validated `.nupkg` and `.snupkg` will be
attached directly to a GitHub Release. Until then, no release, tag, push, or upload is performed.

Release metadata now defaults to `1.0.0`, attributes the package and assemblies neutrally to
`RibbonKit contributors`, and documents that development was performed primarily with AI coding
assistants under human direction, visual review, and automated verification. The same neutral
copyright notice is used by the MIT license; no individual is presented as the package author.

`eng/Prepare-GitHubRelease.ps1` is the repeatable, local-only release gate. It restores, builds,
tests, packs, invokes the isolated package validator, copies `RELEASE_NOTES.md`, and emits SHA-256
sums under ignored `artifacts/github-release-v1.0.0/`; it contains no publishing command. The first
candidate passed with zero build warnings or errors, 298/298 logic tests, the single visual test and
all 62 approvals, and isolated package consumption on both runtime TFMs. Its inspected NuSpec reports
version `1.0.0`, author `RibbonKit contributors`, MIT, the expected repository and release notes.

The prepared files remain local: `RibbonKit.1.0.0.nupkg` (567,192 bytes),
`RibbonKit.1.0.0.snupkg` (109,774 bytes), `RELEASE_NOTES.md`, and `SHA256SUMS.txt`. Because the release
changes are not yet committed, this candidate's Source Link repository commit still identifies the
current pre-release HEAD. If publication is ever authorized, commit first and rerun the preparation
script so the final assets point to the exact release commit. Local release preparation is complete;
public community launch is intentionally deferred.

### 3.85 GitHub v1.0.0 community release — 2026-08-11

The user subsequently authorized and published the GitHub-only community release. Tag `v1.0.0`
points at merge commit `6d6dd32`; the GitHub Release carries the main package, symbol package,
release notes/checksums, and GitHub's generated source archives. RibbonKit remains intentionally
absent from NuGet.org. The release-facing repository documentation now describes v1.0.0 as published
rather than as a local candidate. Deleting unrelated leftover patch/bug-note files after the tag is
ordinary repository cleanup and does not require a new binary release.

### 3.86 Post-v1 custom-control projection and future-theme plans — 2026-08-12

`docs/08-CUSTOM-CONTROL-INTEGRATION-PLAN.md` records an opt-in integration contract for arbitrary
controls hosted in `RibbonGroup`. Ordinary hosting remains requirement-free and `IRibbonSizeAware`
remains the optional adaptive-layout hook. A control that wants customization/QAT participation must
instead provide stable identity, display metadata, a small icon and a context-aware factory that
creates fresh source-bound projections for QAT strip, overflow and custom-group use. The provider
owns command/state binding and custom event cleanup; RibbonKit owns identity, persistence,
enabled/merge state, placement, KeyTip/automation context and deterministic disposal. The proposed
names are deliberately provisional until two distinct control shapes prove the lifecycle.

The same plan makes a small consumer-facing `DynamicResource` token subset the theme contract for
custom content; it rejects a copied palette snapshot that would go stale on a live theme switch.
`docs/09-FUTURE-THEMES-PLAN.md` records the complementary theme-intake and whole-surface verification
gate. Office 2021 is the leading candidate: the deliberately skipped sharp-edged midpoint between
RibbonKit's Office 2019 and Office 2024 themes. The user-approved reference is the pre-rounded UI with
Office 2019's compact square group/control geometry, saturated blue integrated title bar, white
ribbon, centered title-bar search and rectangular search flyout. It is explicitly not the later
Windows 11-style rounded visual refresh that already resembles Office 2024. The theme must remain
visibly distinct from both neighboring generations in a side-by-side matrix.

The ordered theme shortlist continues with **RibbonKit Aurora**, a signature dark-indigo theme using
matte surfaces, translucent state washes, a restrained blue/violet title band and middle-weight
rounding; **Warm Sand**, a non-white parchment/teal light theme; and **Graphite Copper**, a compact
charcoal/copper professional dark theme. Evergreen, Aubergine and Polar Slate remain exploratory.
High contrast stays a system-accessibility mode rather than an aesthetic theme. Exact palette anchors
and acceptance boundaries live in `docs/09-FUTURE-THEMES-PLAN.md` and remain provisional until
Showcase comparison.

### 3.87 RibbonKit Writer functional reference-app plan — 2026-08-12

`docs/10-RIBBONKIT-WRITER-PLAN.md` records a separate post-v1 `RibbonKit.Writer` application rather
than adding more product behaviour to the Showcase. The target is a genuinely usable lightweight
rich-text editor that exercises document lifetime, selection-sensitive command state, Backstage,
QAT, contextual tabs, customization, appearance persistence, accessibility and DPI in one coherent
consumer. The initial format boundary is `.txt`, `.rtf` and a versioned native `.rkw` package; the
native format owns complete page-settings, image and structured-content fidelity.

Paper size, orientation and margins belong to a document-owned `DocumentPageSettings` model applied
to `FlowDocument.PageWidth`, `PageHeight` and `PagePadding`. Editing remains a centred continuous
`RichTextBox` paper surface; a cloned document renders through the paginator for true page preview and
printing. The plan deliberately rejects fake editable page breaks and does not promise Word's fixed
layout engine.

Tables are in scope as native FlowDocument structure. Insert uses a small grid picker; a contextual
Table Tools tab owns row/column insertion and deletion, merge/split, cell sizing, alignment, padding,
borders and background. Table mutation helpers must preserve a valid document tree and predictable
caret position, with `.rkw` as the fidelity format and RTF interoperability treated as best effort.

OLE/COM compound objects are explicitly out of scope. Although FlowDocument can host WPF UI elements,
in-place OLE would add executable-content, COM activation, focus, storage, bitness, print and security
contracts unrelated to proving RibbonKit. Images and hyperlinks ship first; any later attachment is
an inert Writer-owned file card, not an activated embedded application.

### 3.88 Office 2007 opaque and Aero-inspired window-frame plan — 2026-08-12

The deferred Office 2007 window frame now has a two-tier contract in
`docs/07-OFFICE-2007-THEME-PLAN.md` §6. The measured non-Aero treatment remains the guaranteed
baseline: a `#3B5A82` 1px outline, a 5–7 DIP `#9BBBE3` active frame, the existing opaque title
gradient and a quieter inactive state. It must work without transparency or a system material.

An optional Aero-inspired mode may layer translucent tint, grain, reflection and active/inactive
glass treatment over that geometry. Where supported, the existing app-controlled Acrylic backdrop
may show through only the transparent frame/title regions; body and ribbon surfaces remain opaque,
including the measured `#BFDBFF` tab strip. Theme selection does not activate the material, and the
host's frame/backdrop choice remains separate from structural ribbon customization persistence.

The implementation stays inside the application. It must not use `AllowsTransparency`, desktop
capture/CPU blur, DWM injection, private composition hooks or downloaded symbols. DWMBlurGlass is a
visual reference only. The user plans to provide injected Task Manager and Paint captures because
their native DWM-painted frames expose useful tint, reflection, caption and inactive-state evidence;
the plan requests active/inactive, normal/maximized, detail crops and the injection settings.

Implement and approve the opaque frame first. The Aero enhancement follows as a separate prototype
and must fall back to the opaque result. Verification covers 100/125/150/175/200% DPI,
normal/maximized/restore, active/inactive, all resize edges, mixed-DPI monitor movement, caption
commands and the existing Windows 11 `HTMAXBUTTON` Snap Layout/non-client hover bridge. Actual
Acrylic composition is a live check rather than a deterministic snapshot baseline.

## 4. Workflow / Session Conventions

- Cloud workspace: `/home/user/ribbonkit/`. The user's machine:
  `C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF\`
  (device `brin-mm-2026-0004`).
- This Windows workspace builds and runs the WPF solution locally. Use `dotnet build RibbonKit.sln`
  and `dotnet test RibbonKit.sln`; do not carry forward the older Linux-sandbox limitation.
- The user prefers: concise explanations, minimal-formatting replies, files delivered
  immediately, and "just update your side + reply 'Got it'" for their own edits.
- User's own edits so far: `UseLayoutRounding="True"` on the showcase RibbonWindow
  (fixes blurriness); tab-switch slide direction Bottom→Top; backstage open changed
  fade→slide-from-left; document panel margin 14→`8,8,8,0` (aligns the panel edges with
  the ribbon card's inner edge: 7px card margin + 1px border).

## 5. Current State & Next Steps

> **Status as of 2026-08-08: everything through §3.61 and the responsive Ribbon Editor/application-
> menu authoring work in the later §3.62 entry are implemented AND user-verified on Windows;
> §§3.64–3.66 are automated and await focused live visual rechecks; §§3.67–3.68 are user-verified,
> including §3.68 at 100/125/150/175/200% DPI and in the focused RTL input lab; §§3.69–3.70's
> KeyTip resolution and explicit File-surface content contracts are automated.**
> The ten-point §3.40/§3.41 checklist that stood here has been walked and passed in full, and the
> **2007 DPI matrix is clean at 100/125/150/175/200%** — which closes the last S6 exit criterion the
> 2007 arc left open. §3.42's whole-surface flyout animation, reduced-motion behavior, and the
> DPI-awareness manifest have now also passed their Windows verification.
>
> Roadmap Phases 0–7 are complete. **Phase 6 closed in §3.61**: all five generations ship with
> dark/black variants in §3.49, and the complete 40-image
> theme/variant/DPI matrix plus twenty-two focused scenes are covered by 62 approvals; localization,
> representative bidirectional content, and the live RTL popup/window pass are complete.
> Phase 8 release engineering and the GitHub v1.0.0 community launch are complete through §3.85.
> Sections §3.86–§3.88 record the provisional custom-control projection and future-theme tracks,
> the RibbonKit Writer reference-app plan, and the opaque/Aero-inspired Office 2007 frame decision.
> Of the
> two items
> deferred out of §3.38, the two-pane 2007 application menu shipped in §3.46; only the 2007 window
> frame is still owed.

**Manual verification complete through §3.61 (Windows 2026-08-08).** The earlier whole-surface
flyout and No Motion pass (§3.42), complete split-button matrix including the Visual Studio Ribbon
Editor gate/reset/single-Undo behavior (§3.43), proxy enabled-state propagation (§3.45), and complete
application-menu theme/DPI matrix (§3.46) all pass; §§3.51–3.61 add the completed localization/RTL,
bidirectional-content, Backstage/title, popup/window-edge, and application-menu/orb pass. §3.44 is
covered by automated tests.

A. **A vertical split button** (the showcase's Paste): icon on top, ONE line of caption with an
   ellipsis if it is long, chevron beneath it. Narrow the window until the group reduces — it must
   fall back to the horizontal arrangement at Medium and Small, and come back on widening.
B. **Companion highlight, all five themes**: hover the icon half and the chevron half should also
   light; hover the chevron half and the icon half should. 2007/2010 draw an amber border with a
   thin glow rim just inside it and NO fill — the companion must stay clearly cooler than the half
   under the pointer; 2013/2019/2024 draw a lighter version of their hover fill and no border.
   Check the open (checked) chevron state too, and both arrangements. The icon must not shift by a
   pixel when the rim appears.
C. **The corners still meet** — top/bottom rounding in vertical, left/right in horizontal, with no
   gap or double border down the seam.
D. **Ribbon Editor**: select a split button — "Split layout" shows for a Large one (or one whose
   SizeDefinition names Large) and is absent otherwise. Set Size to Medium on a Large+Vertical
   button: the row should vanish and the XAML drop back to Horizontal in one undo step.
   **Verified in Visual Studio on Windows 2026-08-01**, including the SizeDefinition gate and one
   Undo restoring both the Large-capable definition and Vertical layout.
E. **§3.45 proxies (manual — not unit-testable):** disable a command from code and confirm its QAT
   proxy, its entry in the » overflow flyout, and any custom-group copy all grey together. Repeat
   with a whole GROUP disabled (that path goes through coercion, not a property set). Then unmerge a
   merged source and confirm its parked proxy greys in BOTH the strip and the overflow flyout —
   the flyout was the half that stayed live. **Verified 2026-08-01.** To trigger overflow, add enough
   items to cross `QuickAccessMaxWidth`; narrowing the window alone is not expected to do it because
   the tab header scrolls and the title text ellipsizes first. That is intentional QAT behavior.

F. **§3.46 application menu.** Switch to the Office 2007 theme (which turns the "2007 Menu" toggle
   on for you) and click the orb.
   1. **The orb is ON TOP of the menu** and stays lit while it is open. The menu is in the Ribbon's
      outer overlay and the real orb is temporarily hosted above it; there must not be a second or
      offset rendering. If the ribbon body or its shadow rises over the QAT/message rows, the
      outer-host separation is wrong. Check the title-bar QAT is still visible too — only the
      backstage hides it.
   2. **Position.** The menu is anchored to the measured application-button bounds. The 2007
      `Metrics.ApplicationMenuMargin` (`0,8,0,0`) then tucks its top-left under the orb's lower half.
   3. **Hover ownership:** slide down New → Open → Save → Save As. Pane-less rows show Recent
      Documents; Save As claims "Save a copy of the document" immediately, with no flicker.
   4. **Slow gap crossing:** hover Save As, then move right INTO the pane as slowly as possible over
      the narrow separator gap. The pane must not reset. Once the pointer leaves the nav row, its
      command half drops to the theme's subdued level while **the arrow half stays fully lit** and
      the full outline and divider remain. Hover individual items inside the pane:
      the command half must stay subdued, never jump back to full intensity. Drag off any empty edge
      of the menu — the pane must still stay put until another nav row is entered or the menu closes.
      Return to the row: both halves light fully while the divider remains visible.
   5. **Publish is `IsSplit="False"`** — its active state spans the whole row. Press the body and
      then the arrow: both must show the same merged full-row pressed visual, never two independent
      halves. That is the contrast the two shapes exist to show.
   6. **Clicks:** a pane row or a plain nav row closes the menu (status bar shows what was picked);
      the arrow half of a split row does NOT; clicking Publish anywhere does NOT. Esc closes;
      clicking the document closes; **clicking the orb again closes and does not immediately
      re-open** (that is the dismissal exemption). **Steps 1–6 verified on Windows 2026-08-01.**
   7. **Every other generation:** flip the toggle on under 2010/2013/2019/2024. In 2010/2013/2019,
      the menu's top border must touch the File button's bottom edge and their left edges must align;
      the open button gets a compact shadow. In 2024 there is a 6 DIP gap, both surfaces have a soft
      shadow, and every menu corner is visibly rounded. No theme may cover its application button.
   8. **DPI 125/150/200%** on the frame bands and the 52px rows. **Steps 7–8 verified on Windows
      2026-08-01 across all five themes at 100/125/150/175/200/225%.** The pass exposed one Office
      2019 colored-title-bar issue: the File button disappeared into the accent band while open and
      flashed a neutral grey on mouse-down. The fix gives application-menu-open its own background /
      foreground tokens (separate from Backstage), connects the open File tab to the neutral menu
      frame, and derives mouse-down from the accent band. The corrected states were user-verified.



1. **Every flyout opens as ONE surface** — border, shadow and contents together, no card snapping
   in around sliding items: drop-down button, split button, combo box, right-click menu, a
   right-click **submenu**, the in-ribbon gallery, and the QAT » overflow flyout.
1a. **POSITION, on every one of them** — this is the item that failed twice. Every template is back
   to its pre-§3.41 geometry, so each flyout should sit exactly where it did before any of this
   work: menus flush under their openers, the context menu on the cursor, the gallery flyout over
   the strip it replaces. The combo box is the reference case: it had been 10px high since §3.41
   and should now be flush for the first time.
1c. **A collapsed group's flyout** (narrow the window until a group collapses): clicking a plain
   button or a menu row inside it CLOSES it; clicking a drop-down/split chevron, a gallery's expand
   or scroll buttons, or a combo's chevron does NOT; right-clicking does not; and a split button's
   left half both runs the command and closes.
1b. **The » flyout with the QAT in the tab row AND in an accent title bar** — entries must use the
   NORMAL hover/pressed colours, and a split entry's chevron must be dark and visible. Check the
   chevron on the strip itself is still white-on-band; that is the behaviour the reset must not undo.
2. **No sliced top edge, and no flicker** — the card should fade up cleanly rather than blinking in
   at full strength first, and the contents should settle without any edge being cut.
3. **Nothing moved at rest** — see 1a; this is the item that failed the first time.
4. **`RibbonAnimation.GlobalLevel = Expressive`** — the level that used to clip. The content should
   simply travel further, with no edge sliced and the card itself still not moving.
5. **Change the Windows display scale with the app running.** It should relayout live and stay
   crisp; no restart, and the maximized window must still sit flush against the work area
   afterwards. (Try it maximized, and on a second monitor at a different scale if there is one.)

**Working and confirmed by user: everything through §3.21** — including the QAT
customization + merged options dialog with all its refinements (custom close-only title
bar, DWM rounded corners, resizable, per-page scroll policy via `IRibbonFillPage`), the
Customize-the-Ribbon page + Edit… dialog, customization **persistence** (round-trip / Reset /
corrupt-JSON-starts-clean), the §3.18 QAT/dialog polish batch, the §3.19 dropdown/split QAT
proxies, the §3.20 large-label chevron/ellipsis work, and the §3.21 backstage footer/button
items. The §3.14 XAML **design-time** preview (active tab + backstage on the VS/Blend surface)
is also user-confirmed. The §3.21 #4 **backstage Tab-focus leak is now fixed** (focus trap;
see §3.21). Nothing through §3.21 remains in the "needs verification" state; the later manual checks
listed above are still outstanding.

**Animation polish is now complete.** All six items formerly tracked here — hover
cross-fade, the true sliding tab marker (shared animated underline), contextual-tab
appear, toggle-state cross-fade, theme-switch cross-fade, and KeyTip badge pop — are
wired and confirmed; see §3/"Wired so far" list above for the code sites. KeyTip badge
pop was the last of the six: `RibbonMotion.PlayKeyTipPop` plays a fade + short downward
settle from `KeyTipService.AddAdorners`, self-releasing its opacity animation on
completion so the existing dim/undim-while-typing logic (`KeyTipAdorner.Dimmed`) keeps
working afterward (hard rule 8).

**Import / Export (customize page) — DONE.** `RibbonCustomizePage` now has **Import…** / **Export…**
buttons beside **Reset** (bottom-left, `PART_ImportButton` / `PART_ExportButton` in the Office2024
template). Export writes `RibbonCustomizationSerializer.Serialize(ribbon)` to a `.json` the user picks
(`Microsoft.Win32.SaveFileDialog`, no WinForms); Import reads one back (`OpenFileDialog`) and
`Apply`s it, then `RebuildAll`. File IO is guarded (IO/access/security exceptions → a `MessageBox`),
and `Apply` already tolerates a foreign/corrupt string, so a bad file can't corrupt the ribbon. Import
mutates the live ribbon immediately (same as Reset); the host persists it when the options dialog's
Apply fires. The serializer already round-tripped (§3.17) — this was just the missing UI.

**Design-time editor — done this arc:** the runtime ribbon horizontal scroll (§3.25, incl. the
reduce-then-scroll clamp fix and the chevrons-return-on-tab-switch fix), split/drop-down button menu-item
editing, `Ribbon.CommandId` + `KeyTip.Keys` attached-property editors (attached-property model access
proven — `Find(PropertyIdentifier)`), the backstage page switcher, the modern context menus (§3.26), and
**drag-drop tree reordering** (see below). Showcase gained a Disable-Samples demo (button/split/group
disabled states).

**Drag-drop reordering (RibbonEditorWindow):** drag a tree node onto another to reorder or reparent.
`PreviewMouseLeftButtonDown` records the candidate + start point; `PreviewMouseMove` starts
`DragDrop.DoDragDrop` once past the system drag threshold (so a plain click still selects). `DragOver`
builds a `DropPlan` and shows a `DropAdorner` (a blue insertion line above/below the target row, or a
rounded box for an "into"/append drop, chosen by where the pointer sits in the row's header height);
`Drop` applies it via `DesignModel.MoveInto` (remove-from + insert-into as one undo, with the insert
index adjusted for the removal shift when it's a same-collection reorder). Compatibility (`Accepts`)
mirrors the verbs: tabs↔Tabs, groups↔Groups (any tab), real controls/panels↔a group's `Items` or a
panel's `Children` (any group/panel), and item entries↔`Items` of a container of the SAME item type
(so a `RibbonMenuItem` can move between dropdowns/splits, a `ComboBoxItem` between combos, etc.).
`IsAncestorOrSelf` blocks dropping a node into itself or its own subtree. The drag payload is the
`NodeInfo` itself (in-process WPF drag-drop keeps the managed reference, so an internal type is fine).

**Phase 7 is COMPLETE and user-verified on Windows (2026-07-27).** All four steps landed in one
arc: modal tabs (§3.32), tab merging (§3.33), group contributions + the declarative activation path
+ QAT proxy parking (§3.33), and the MDI tab/caption merge that also closes **MDI milestone M4**
(§3.34). Design doc: `docs/06-MERGE-AND-MODAL-PLAN.md`. Every remaining v1.0 item is now a repeat of
a pattern already solved here.

**Quick access toolbar overflow — DONE and verified (§3.35).** The tab-row and title-bar placements
cap at `Ribbon.QuickAccessMaxWidth` and move the rest into a » flyout; below-ribbon is unconstrained
and unchanged.

**The overflow flyout's second stranding bug is fixed (§3.39)** — dismissing the flyout while one of
its drop-down/split entries had its menu open used to leave the ORIGINAL ribbon button opening onto
an empty menu for the rest of the session. Covered by tests rather than by clicking, so the manual
repro in §3.39 is still worth one pass.

**Cross-cutting rule worth reading before touching the tab strip: §3.36.** The sliding underline and
the 2010/2013 connect notch do not update themselves, and neither `SizeChanged` on the tab control
nor `SizeChanged` on a sibling whose `Visibility` toggles will tell you. Both variants shipped broken
during this arc.

Backlog (rough priority):

1. **XAML Designer Ribbon Editor application-menu parity — DONE AND USER-VERIFIED (§3.62).** The
   responsive editor, singleton/menu structure authoring, property reuse, atomic File-surface selector,
   and design-only active-pane preview are complete. Keep default/minimum width, live DPI moves, and
   representative add/delete/reorder/undo operations in future designer regression passes.
1a. Design editor: optional clear-to-default buttons for scalar properties. (Drag-drop tree
   reordering + cross-tab/group moves are now DONE — see §5 "Drag-drop reordering".)
1b. ~~Finish the `DropdownMenu` animation.~~ **DONE (§3.42)** — all five flyouts plus the context
   menu and its submenus now animate the whole surface.
2. **Office 2007 leftover** — the one piece §3.38 deferred that is still open: the 2007 WINDOW
   FRAME (glass caption + orb overhang). The other deferral, the real two-pane APPLICATION MENU,
   **shipped 2026-07-28 (§3.46)**. (The 2007 DPI matrix pass is DONE — clean at
   100/125/150/175/200%.)
3. **Full RTL verification + localization resources — DONE (§§3.51–3.61).** The
   §§3.48–3.49 visual-regression matrix is complete, §3.51 adds the first RTL smoke snapshot, and
   §§3.52–3.55 localize/mirror ribbon-owned context menus, Customize/Options, chrome tooltips and the
   default File label. §3.58 completes the representative bidirectional-text Backstage pass and
   §3.59 fixes its live adorner/title transition; §3.61 records the green live popup/window,
   screen-edge, and 2024/2007 application-menu/orb verification.
4. **The remaining unit tests — DONE.** Customization serializer round-trips are covered in §3.72
   and the reduction algorithm/panel-order gaps in §3.73. KeyTip resolution is DONE in §3.69. The
   Phase 7 merge/modal invariants
   are now DONE: 13 tests cover
   stable ordering, repeated merge/unmerge, two-source group restoration, modal transitions and
   cancellation, serialization exclusion, rebuild/remerge and declarative activation. The harness
   and house style for headless WPF tests are in place (§3.39).
5. **MDI M1–M3**: cascade/tile/arrange commands + Ctrl+Tab (M1), the MVVM `ItemsSource` demo and a
   per-theme pass (M2), tabbed-documents mode + `RibbonState` layout persistence (M3). M0 and M4 are
   done, so the feature currently has a hole in its middle.
6. Roadmap Phase 8 release engineering: **API review/freeze DONE (§3.75); repository documentation
   DONE (§3.76); repository URL and package/Showcase icon DONE (§3.77); Source Link and symbol
   generation DONE (§3.79); portable package output DONE (§3.80); deterministic versioning DONE
   (§3.81); package/clean-consumer gate DONE (§3.82); live performance/install pass DONE (§3.83);
   local v1.0.0 GitHub-release candidate DONE (§3.84); GitHub community launch DONE (§3.85).**
   NuGet.org publication is not planned.
7. GitHub repository metadata: **DONE** — the package points to `Wraith1080/RibbonKit`.

Resolved at the API freeze (not v1 scope):

- **Touch mode toggle.** The Phase 8 API review decided not to expose an explicit v1 density API.
  A future implementation would need an opt-in density mode that enlarges hit targets and spacing
  across the ribbon, QAT, menus, and
  customization surfaces. Keep it token/metric-driven and compatible with reduction, DPI, RTL, and
  mouse/keyboard use; do not infer it from a single touch event.
  **Feasibility check (2026-08-08):** this is not currently a one-property template switch. Theme
  chrome metrics are centralized, but hit-target geometry still spans hundreds of literal sizes,
  margins, and paddings across the shared button, dropdown, input, Backstage, application-menu, and
  customization templates. A complete implementation would first need a semantic density-metric
  layer, an inheritable enum-shaped setting (not a dead-end boolean), explicit propagation into
  popup and dialog roots, reduction/layout invalidation, and focused mouse/touch/DPI/RTL approvals.
  Do not freeze a placeholder API; keep this deferred unless a dedicated cross-surface arc is approved.

Possible post-v1 polish:

- **Custom-control integration/projection contract — planned in
  `docs/08-CUSTOM-CONTROL-INTEGRATION-PLAN.md` (§3.86).** Keep arbitrary group hosting unrestricted;
  require identity, display/icon metadata and a fresh-view factory only for controls opting into
  customization/QAT. The provider owns behavioral bindings and event cleanup while RibbonKit owns
  placement, persistence, enabled/merge state and lifecycle calls. Prototype strip and overflow
  contexts before freezing the additive public API.
- **Office 2021 bridge theme — leading candidate in `docs/09-FUTURE-THEMES-PLAN.md` (§3.86).** Model
  the sharp-edged pre-rounded UI between Office 2019 and 2024 through the shared token/template
  system; do not substitute the later rounded visual refresh, and do not ship it unless the midpoint
  is visibly distinct.
- **Original theme sequence — planned in `docs/09-FUTURE-THEMES-PLAN.md`.** After Office 2021,
  prototype RibbonKit Aurora, then Warm Sand and Graphite Copper. Keep Evergreen, Aubergine and Polar
  Slate exploratory until the snapshot/maintenance cost of the earlier additions is understood.
- **RibbonKit Writer reference application — planned in
  `docs/10-RIBBONKIT-WRITER-PLAN.md` (§3.87).** Build it as a separate functional consumer with a
  paper-aware native format, paginated preview/printing and contextual table editing. Keep OLE, DOCX
  and true editable page layout outside the boundary.

- **Richer QAT projections — decision recorded 2026-08-10, not Phase 8 scope.** The current supported
  discovery/proxy set is `RibbonButton`, `RibbonToggleButton`, `RibbonSplitButton`, and
  `RibbonDropDownButton`. `QuickAccessItems` can still contain hand-declared elements, but the
  customization catalog, overflow projection, persistence identity, popup lifetime and KeyTips do
  not make an arbitrary element a supported source. `AddToQuickAccess(FrameworkElement)` now returns
  `false` for those unsupported types. Keep that boundary explicit rather than reopening the v1 freeze.
  - **Group first.** Represent a `RibbonGroup` as a small dropdown using its existing `Header` and
    `Icon`; require a usable group icon for QAT eligibility. Its popup should be generated from
    source-linked command proxies (the same proven machinery used by custom groups), never by moving
    the original group body — reparenting would visibly empty a source group whose tab is open.
    Candidate discovery and persistence must register the group itself, and the projection must cover
    dialog launchers, nested dropdowns, overflow, KeyTips, merge parking and removal while open.
    Until richer child projections exist, do not silently omit a gallery/combo/custom child from an
    otherwise addable group; either declare that group ineligible or present a clear limitation.
  - **Gallery second; combo separately.** A gallery should become an icon/label dropdown rather than
    an inline QAT strip and will need explicit quick-access metadata plus synchronized selection and
    preview/commit forwarding. A combo box is useful only as a compact inline editor and needs fresh
    strip/overflow views synchronized for `ItemsSource`, selection, editable `Text`, validation and
    width. Direct `RibbonGalleryItem`/`ComboBoxItem` children remain single-parent WPF elements and
    cannot simply be shared between projections.
  - Do not freeze the architecture document's old, unimplemented one-method
    `IQuickAccessItemProvider` sketch. A proven future contract needs presentation context (strip vs
    overflow), fresh-view creation, stable source identity/persistence, enabled/merge propagation and
    deterministic popup cleanup. Adding such an extension point after v1 is additive; a placeholder
    contract now would be harder to repair.
- **Custom group content needs no new host control.** `RibbonGroup` is already a
  `HeaderedItemsControl`, so an application may place a custom control or panel directly in it; the
  collapsed group re-homes the existing body intact. The child needs no icon — `RibbonGroup.Icon`
  represents the whole group — and may implement the existing public `IRibbonSizeAware` hook when it
  must react to reduction states. Prefer a `RibbonDropDownButton` for large, secondary, or
  popup-owning content. A future integration pass may broaden explicit-KeyTip discovery and provide a
  keep-open/lifecycle hook for arbitrary popup-owning controls, but those needs do not justify a
  wrapper whose only job is to host WPF content.
- An optional `MonochromeIcon` on QAT-capable command buttons, used as a purpose-authored alpha mask
  on accent-colored title/tab surfaces. This is an API idea, not scheduled work; a separate
  `DarkIcon` property remains intentionally unplanned.
- A non-Mica light/dark transition that captures the old opaque window chrome, applies the new
  palette underneath, and fades the capture away. It should use `RibbonAnimationAction.ThemeSwitch`,
  honor reduced motion, handle rapid reversals, and stay disabled while a DWM backdrop is active so
  Mica/Acrylic retain their native material retint without a second cross-fade on top.

**Unit tests: 298 green (verified 2026-08-11).** Coverage now includes the STA harness, the borrow
protocol, overflow strip measure/arrange rules, popup motion and dismissal, proxy mirroring,
application-menu layering/hover/footer-outline/KeyTips, repeatable message-bar API/template/theme
contracts, Office 2010 seam/state/consumer contracts, localization/RTL
context-menu, customization-template, chrome-tooltip, default-File, representative bidi-lab and
live Backstage/title-transition contracts, design-preview runtime isolation, scoped theme-token
replacement, dark/Black neutral Backstage rail contracts, and the existing
reduction/size-definition/theme-scope tests, plus rejection of unsupported automatic QAT projections.
The KeyTip resolver cases add explicit-key precedence,
exact/prefix collision recovery, prefix-free derivation, typeable non-Latin fallback, and explicit
custom-content discovery/invocation across Backstage and application-menu panes, including disabled
target blocking, native toggle Click/Command semantics, compact-input discovery with Header-based
label derivation, and collapsed-group preservation for editor/picker activation.
`RibbonMergeModalTests` adds the Phase 7 automated invariants: merge ordering across later
permutations, merge/unmerge round-trips, group restore with two sources in one tab, capture while
modal, modal enter/exit selection and cancellation, forced exit when a merged modal tab leaves,
customization rebuild/remerge, and declarative activation. The broader coverage gaps listed above remain.

**Visual tests: 1 green locally (2026-08-08), covering all 62 approved images.** The §§3.48–3.49 matrix
spans five light themes plus five dark/black variants × 100/125/150/200%, followed by the §3.51 ribbon
RTL smoke, §3.53 QAT-customization RTL scene, §3.58 representative bidirectional Backstage scene,
and the focused §3.27 Office 2010 button-state/Backstage-shell, §3.65 connected/RTL message-bar
stacks and 2024 rectangular/2007 orb open-menu/message compositions, plus
2007/2010 Black application-menu scenes; its separate project keeps rendering policy out of the
headless logic-test harness. It passed three successive fresh-process stability runs in addition to
the normal project and solution runs.
