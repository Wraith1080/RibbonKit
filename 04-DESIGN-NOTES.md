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
- Open source, packaged for NuGet (`RibbonKit`, currently `0.1.0-alpha.1`).
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

- **All control templates live in `Themes/Office2024.xaml`** (~1900 lines), shared by
  every theme. Templates never hardcode colors/metrics — they reference tokens via
  `DynamicResource` (`RibbonKit.Brushes.*`, `RibbonKit.Metrics.*`, `RibbonKit.Effects.*`).
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
6. Backstage: slide-in from LEFT on open; slide-out LEFT on close with the adorner
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
- **Modern.* brushes use `StaticResource`** — they're defined inside Office2024.xaml
  itself; DynamicResource lookup from a template can't reliably find theme-dictionary
  locals (only app-scope tokens). Accent-driven parts stay DynamicResource.
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
isolated surface process the extension can't read — so the extension can't auto-discover Icons.xaml.
Design that needs zero uncertain APIs: a "…" button on each icon row opens a picker that (1) always
lists the icon keys **already used elsewhere in this ribbon** (a pure model walk, `CollectUsedIconKeys`),
and (2) has **"Load Icons.xaml…"** — an `OpenFileDialog` that parses the file with `XamlReader.Load`
in the extension's own WPF context, so the `DrawingImage` values render as real **thumbnails**; the
loaded dictionary is cached for the session (`IconCatalog`). A filter box narrows the grid, the current
key is highlighted, and clicking a tile writes via the proven `SetStaticResource`. Graceful: useful
with no file loaded (used-keys), and it can't hit an undocumented API. Trimmed the now-proven spike
logging (read-back / create-attempt / model-type lines). Later polish: remember the Icons.xaml path
across sessions; a "(none)" tile to clear an icon (needs a verified `ClearValue`).

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

Still unbuilt in the sandbox (WPF needs Windows) — pending the user's visual check on Windows.

## 4. Workflow / Session Conventions

- Cloud workspace: `/home/user/ribbonkit/`. The user's machine:
  `C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF\`
  (device `brin-mm-2026-0004`).
- **No WPF build available in the Linux sandbox** — every change ships unbuilt; the
  user builds/tests on Windows and reports back. Deliver files via SendUserFile when
  the device bridge is offline (it has been, lately); push directly when connected.
- The user prefers: concise explanations, minimal-formatting replies, files delivered
  immediately, and "just update your side + reply 'Got it'" for their own edits.
- User's own edits so far: `UseLayoutRounding="True"` on the showcase RibbonWindow
  (fixes blurriness); tab-switch slide direction Bottom→Top; backstage open changed
  fade→slide-from-left; document panel margin 14→`8,8,8,0` (aligns the panel edges with
  the ribbon card's inner edge: 7px card margin + 1px border).

## 5. Current State & Next Steps

**Working and confirmed by user: everything through §3.21** — including the QAT
customization + merged options dialog with all its refinements (custom close-only title
bar, DWM rounded corners, resizable, per-page scroll policy via `IRibbonFillPage`), the
Customize-the-Ribbon page + Edit… dialog, customization **persistence** (round-trip / Reset /
corrupt-JSON-starts-clean), the §3.18 QAT/dialog polish batch, the §3.19 dropdown/split QAT
proxies, the §3.20 large-label chevron/ellipsis work, and the §3.21 backstage footer/button
items. The §3.14 XAML **design-time** preview (active tab + backstage on the VS/Blend surface)
is also user-confirmed. The §3.21 #4 **backstage Tab-focus leak is now fixed** (focus trap;
see §3.21). Nothing in §3 remains in the "needs verification" state.

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

Backlog (rough priority):

1. Design editor: optional clear-to-default buttons for scalar properties. (Drag-drop tree
   reordering + cross-tab/group moves are now DONE — see §5 "Drag-drop reordering".)
3. Mica hardening (future): dark-mode-aware translucency. (Maximize-with-glass and the
   glass-frame border fix are verified — see §3.12.)
4. Office2007 theme (roadmap Phase 6). **Office2010 is DONE — see §3.27.** 2007 is the last
   remaining classic theme (round Office orb button + heavier glass gradients).
5. Dark mode (2019 white-tab note in §3.6 anticipates it). **Still outstanding** — the last
   item from this theming arc after 2010 and 2007.
6. GitHub publish: repo URL placeholder in csproj (`YOUR-GITHUB-USERNAME`).
