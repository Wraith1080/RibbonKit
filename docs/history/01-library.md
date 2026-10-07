# Library implementation history

> Historical evidence, moved without changing its technical content on 2026-09-08.
> Dates, test counts, pending work, and instructions describe their original checkpoint;
> later entries may supersede them. Use [current status](../../04-DESIGN-NOTES.md#5-current-state--next-steps)
> and [AGENTS.md](../../AGENTS.md) for present work. Read only the relevant numbered entry.

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
- **Animated target placement (2026-09-16):** Backstage KeyTips can be realized at
  `DispatcherPriority.Loaded` while the surface's render-transform slide is still running.
  WPF's `AdornerLayer` caches that intermediate target transform during layout; completing
  the animation alone did not refresh it, leaving badges off-center until a hover/layout
  update. Loaded `KeyTipAdorner`s now compare the target's coordinate basis and render size
  on rendering ticks and update their target's layer placement only when those values change.
  Unloading removes the rendering subscription, and reuse starts fresh. Do not replace this
  with a fixed animation delay: duration, disabled motion, and entry into an already-open
  surface all vary. Two realized-window regressions cover LTR/RTL intermediate and final
  slide positions plus badge removal/reuse; both fail against the original implementation.
  Verification: net8/net9 Release runtime build, zero warnings/errors; 36 focused KeyTip/RTL
  tests passed. Live Showcase reproduced the original offset with F10 → F and verified
  centered navigation/footer/custom-content badges with Alt → F after rebuilding, without
  moving the mouse. Full-suite and theme/mixed-DPI matrix checks were not run for this fix.

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
  backed out — `InRibbonGallery.cs` was restored to the original pre-feature version at that point.
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
  - **2026-08-30 follow-up (RKWF-018):** the durable variant keeps separate permanent strip
    and popup scrollers and re-homes only the one items presenter. Thus no viewport/clip object
    crosses the Popup HWND/DPI boundary; focused post-DPI open/close cases pass, with live
    mixed-DPI acceptance still pending.
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
  2010 now uses `24,7,22,9`, and 2013 `24,7,24,9` (the pre-2024 File tabs read as broader blocks).

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

### 3.87a RibbonKit Writer Luna execution decomposition — 2026-08-20

`docs/11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md` decomposes the approved W0-W5 product milestones
into dependency-gated packets for later `gpt-5.6-luna` subagents. It deliberately creates no Writer
project or branch and schedules no agent. The functional scope remains owned by
`docs/10-RIBBONKIT-WRITER-PLAN.md`; §5 remains the live implementation-status authority.

The execution contract keeps solution/project files and primary application/ribbon XAML under
exclusive ownership, treats `src/RibbonKit/**` as read-only unless a separately approved runtime-gap
packet proves a genuine library need, and makes the lead agent responsible for diff review, full
build/tests and verification on the real Writer surface. Agent reports and plan checklists are not
implementation evidence. Native `.rkw` loading, FlowDocument table mutation and final Windows
acceptance have explicit higher-risk gates; W5 distribution remains a user/lead decision rather than
a delegatable coding task.

### 3.88 Office 2007 opaque and Aero-inspired window-frame plan — 2026-08-12

The deferred Office 2007 window frame now has a two-tier contract in
`docs/07-OFFICE-2007-THEME-PLAN.md` §6. The original reading here treated the blue beside the
document as a full-window band; §3.89 supersedes that geometry after row-by-row review of the
restored non-Aero capture. The guaranteed baseline keeps the window root flush, preserves the
existing opaque title gradient and adds only a quieter inactive title state. It must work without
transparency or a system material.

An optional Aero-inspired mode may layer a distinct restored-frame geometry plus translucent tint,
grain, reflection and active/inactive glass treatment. Where supported, the existing app-controlled Acrylic backdrop
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

### 3.89 Office 2007 opaque window baseline — reference correction — 2026-08-12

The first implementation pass on `codex/office-2007-window-frame` misread the light-blue strip beside
the non-Aero Word document as a 5–7 DIP frame around the complete window. The supplied restored
reference (the Maximize button is visible) disproves that interpretation. Pixel rows through the
title, ribbon, document and status areas show wallpaper at x=0 and a consistent one-pixel dark edge at
x=1, but the title gradient and status background both begin at x=2. The wider `#B1C6E1` /
`#C2D9F7` / `#B0CBEF` / `#9BBBE3` stack appears only beside the ribbon/document workspace. It is
Office client-area bevel/padding, not global window chrome.

The uncommitted prototype was corrected immediately: `PART_WindowRoot` is again flush with the
physical-LTR `PhysicalWindowFrameHost`; the custom outline, full-window band and maximized top-band
overlay were removed. RibbonKit relies on the supported native `WindowChrome`/DWM edge rather than
painting a duplicate border. The opaque Office 2007 fallback remains the existing historical title
gradient plus opaque ribbon/document surfaces. Its 34-DIP caption metric is tokenized for the later
Aero experiment, and inactive Office 2007 windows provisionally lower only `TitleBarBand` opacity to
`0.78`. Other generations publish the same caption/state keys with neutral values, preserving the
shared template and token parity.

This correction also preserves the existing maximize implementation: no decorative inset or overlay
is added around `PART_WindowRoot`, so `RibbonWindow.UpdateMaximizeInset`, the `WM_GETMINMAXINFO`
fallback and the previously measured work-area compensation remain the sole maximize geometry.
Resize hit testing and the `HTMAXBUTTON`/non-client caption-state bridge are unchanged.

The Aero Word reference establishes a different composition: its material runs continuously from the
outer restored edge across the title region down to the opaque tab strip. The Task Manager/Paint
DWMBlurGlass captures are appearance references only; their supplied setup is Aero, radius 20,
ColorBalance 8%, BlurBalance 49%, AfterglowBalance 43% and reflection opacity 40%, with accent
override and custom reflection texture disabled. Section §3.90 records the separately implemented
Aero-inspired prototype.

After the correction, `dotnet build RibbonKit.sln --no-restore` is clean and `dotnet test
RibbonKit.sln --no-build --no-restore` passes **306 logic tests plus one visual test covering 62
approved images**. All five base token dictionaries have **188 identical keys**. A per-monitor-aware
real-HWND capture at the available 125% setting confirms that normal title/status surfaces now span
the complete client width and maximize adds no painted frame band. The remaining cross-DPI and
hands-on interaction gates are tracked with the Aero prototype in §3.90.

### 3.90 Office 2007 Aero-inspired window-frame prototype — 2026-08-12

The optional stage is now implemented as a separate, host-controlled appearance. The new public
`RibbonWindow.FrameAppearance` dependency property accepts `Default` or `Office2007Aero`; changing
it never requests a DWM material. `MicaHelper.TrySetBackdrop` independently reports its last accepted
material through the read-only `RibbonWindow.ActiveBackdrop` property so the shared template can
distinguish a real Acrylic composition from its deterministic fallback. No `RibbonBackdrop.Aero`
value was added.

`Controls.Window.xaml` still keeps `PART_WindowRoot` flush in the default/opaque mode. The explicit
Aero appearance alone creates a physical-LTR 6-DIP frame on the restored left, right and bottom
edges, with a blue tint, bright inner edge, restrained diagonal reflection and tiled grain. The same
overlays continue through the title band. Accepted Acrylic makes only that frame and title fill
transparent; the tab strip, ribbon, document surface and status area remain opaque. Without Acrylic,
the title now uses the same `#9BBBE3` Aero fallback brush as the restored frame rather than exposing
the ordinary Office 2007 title gradient; the inactive title/frame pair similarly shares the quieter
`#BCC8D6` fallback. Maximizing collapses the side/bottom frame and leaves the material title
treatment, matching the supplied Word, Task Manager and Paint evidence.

The five base dictionaries now carry **209 identical token keys**. Non-2007 themes publish neutral
transparent/zero frame values, so the project retains one lookless template. Office 2007 Black
overrides only its required frame/tint/foreground colours. Caption hover and pressed brushes also
flow through the existing non-client bridge, so `WM_NCHITTEST` can still return `HTMAXBUTTON` while
the custom button renders the Aero state.

The Showcase exposes `2007 Aero` separately from `Acrylic`. Its versioned appearance settings moved
to schema 3 and migrate schemas 1–2 with the reference tint defaults; this file remains independent
of ribbon customization Import/Export/Reset. Selecting Office 2007 enables the frame control but
does not check it and does not enable Acrylic. A saved Aero preference is dormant under other themes
and returns when Office 2007 is selected.

At the available real 125% per-monitor-v2 setting, live normal active, normal inactive and maximized
Acrylic captures pass: material is continuous only through the title/restored frame, the inactive
state is quieter, and the maximized side/bottom band disappears. The opaque Aero fallback also
passes at 125%. Direct `WM_NCHITTEST` probes return the expected eight resize codes (`HTLEFT` through
`HTBOTTOMRIGHT`) and `HTMAXBUTTON` over the maximize button. The full automated gate passes **313
logic tests plus one visual test covering 62 approved images**. Actual Acrylic remains deliberately
outside deterministic snapshots. The final multi-DPI, mixed-monitor and hands-on approval is recorded
in §3.93.

The first user review found the frame faithful but the composition too dense and identified bright
vertical seams beside the orb and Close button. The supported DWM Acrylic backdrop has no public
blur-density control, but RibbonKit's authored tint does. `RibbonWindow.AeroFrameTint` is a
host-overridable brush and `AeroFrameTintIntensity` is a validated 0–1 dependency property; the
Office 2007 token default is 0.16. The Showcase hosts an ordinary WPF Slider presenting that value as
0–100% and an explicit `Use accent color` checkbox. The latter resolves the current ribbon accent
into a local frame brush without making theme/accent selection automatically enable or recolor Aero.
No public `RibbonSlider` control was introduced.

The seams were not DPI rounding; they were the deliberate inner-bevel stroke incorrectly continuing
through the title. Its left/right strokes now begin below the 34-DIP caption so the title and frame
remain one uninterrupted material surface. Tint opacity is applied only to the tint layers, so the
slider does not weaken reflection, grain or the corrected inner highlight.

### 3.91 RibbonKit-original Glass2007 Backstage prototype — 2026-08-12

The translucent Modern Backstage over the Office 2007 Aero frame suggested a useful post-v1 design
that is intentionally not presented as historical Office behavior. Real Office 2007 still defaults
to RibbonKit's two-pane orb application menu. The additive public
`RibbonBackstageDesign.Glass2007` value is an explicitly selected full-window alternative; neither
the Office 2007 theme nor this design enables Acrylic automatically.

The first visual stage stays in the one shared `Controls.Backstage.xaml` template. Its opaque mode
uses the generation-aware `Backstage.NavBackground`; translucent mode clears that rail and applies
the same low-strength theme/accent tint as the title and outer frame over the app-controlled Acrylic
path. The content sheet remains opaque and keeps only the
established historical shadow. Live review removed the authored rail perimeter, content-edge stroke
and residual top specular line. A follow-up screenshot clarified that the remaining large
frame-sized outline was the window frame's `AeroFrameInnerHighlight` plus the title surface's bottom
highlight, not a navigation-tile border; both now collapse through the existing
`IsTitleBarContentVisible=False` Backstage state.
Hover and selected tile outlines remain crisp, while their fills plus the orb-derived back-button
disc use a shared `0.88` opacity token, allowing a restrained amount of rail color through without
fading text, icons or the back arrow. All five base themes define the new metric, retaining a single
shared template and an identical token contract.

The next live pass exposed three composition details. The translucent rail had used
`Backstage.Classic.NavBackground` while the title used the host-overridable `AeroFrameTint`, and the
rail overlays stayed at active strength after the title became inactive. The rail now binds the same
window tint/intensity and keeps the title's established inactive opacity. While inactive Backstage
owns the window, `AeroInactiveBackstageFrameOpacity=1` compensates for DWM using a different inactive
fallback in the outermost frame pixels than in the transparent client rail. At 125%, the resulting
left frame, bottom frame and rail boundary all sample `#B6BFCA`; using the normal frame attenuation
left a lighter strip. The already-approved top join keeps its existing title/rail composition. The
relative diagonal reflection and tiled grain are
material-only decoration: they appear only for an active, normal title/frame with accepted Acrylic
and are suppressed while Backstage owns the surface, while the window is inactive, or while the
opaque fallback is in use. Mapping those brushes independently into a 34-DIP fill and a full-height
border had visibly restarted them at shared edges. Outside Backstage, inactive Aero instead keeps its
shared fallback base fully opaque and quiets title content separately; title and restored frame then
use the normal `AeroInactiveOverlayOpacity` over that common base. Consequently the inactive Backstage
material stays continuous when Windows suspends Acrylic, while the normal no-material title/frame pair
shares the same inactive fallback composition. Explicitly
opaque `Glass2007` under `Office2007Aero` retains its historical rail and local `1,1,0,1` white bevel.
The title-bottom highlight is now a final,
non-hit-test overlay above the caption buttons; hover/pressed caption fills cannot interrupt or
overlap that one-pixel seam, while Backstage still collapses it with the larger frame bevel.
Dark Office 2007 review also found that the Glass2007 back disc and unselected navigation labels
retained their light-palette colors against the neutral translucent rail. The disc now consumes the
same `ItemSelectedGlass`/`ItemSelectedBorder` pair as a selected tile, its arrow remains explicitly
white, and unselected labels/icons consume the Aero title foreground. That foreground is dark in the
light palette and white in the dark overlay, preserving contrast without forking the template or
changing the page-content foreground.

The Showcase adds a `2007 Glass` selector and persists it through the app-owned appearance document,
separate from ribbon customization. The frame tint slider now has a compact Reset action that assigns
the shared `DefaultAeroFrameTintIntensity` constant (`0.16`), so UI reset, schema migration and the
factory value cannot drift. Logic contracts cover the enum, opaque/translucent template triggers,
generation-aware navigation resources, Showcase wiring and appearance round-trip. Acrylic remains a
live visual check; the next stage is user review of this shell before changing the Backstage page
content layout. The full automated gate passes **318 logic tests plus one visual test covering 62
approved images**.

### 3.92 Separate Classic2007 Backstage concept — 2026-08-13

A new user-supplied concept image explored what a full-window Backstage might have looked like if
Office 2007 had shipped one. It is not historical evidence and does not replace either the real
two-pane Office 2007 application menu or RibbonKit's existing modern `Glass2007` interpretation.
The additive `RibbonBackstageDesign.Classic2007` value is appended as enum value 4, leaving the
already-persisted `Glass2007=3` contract stable. The Showcase labels those two independent choices
`2007 Classic` and `2007 Modern` respectively.

`Classic2007` stays in the single shared Backstage template. Its final shell is an opaque
Office-blue field shared by the 220-DIP navigation rail and the content pane; it deliberately ignores
`Backstage.Translucent`, while retaining that preference for the next switch back to `Glass2007` or
Modern. A single continuous dark-blue perimeter and a matching one-pixel pane divider replace the
earlier segmented and etched borders. The divider terminates inside the continuous white inner
highlight, so the horizontal and vertical strokes never compound at the bottom junction. A second
full-perimeter border carrying `Backstage.ContentShadow` casts depth into the eight-DIP shell gutter.

Classic originally reparented the real ribbon application button into `BackstageAdorner`. That
implementation passed the initial gate, but animated Classic-to-Modern switching later exposed its
dependence on title-row clipping and template ancestry. Section §3.94 supersedes the hosting detail
with a pixel-identical private Backstage proxy while retaining this section's visual design.

When the containing window uses `Office2007Aero`, the Backstage draws a local four-sided white join
bevel between the Aero frame and the opaque shell. The normal window-wide Aero inner highlight stays
suppressed while Backstage is open, preserving the measured maximize/work-area compensation and the
previously approved modern-glass seams.

The Showcase Home page preserves its original `Good morning` content for every established design
and conditionally reveals a Classic2007-only document dashboard: Recent Documents, Preview,
Properties, Quick Actions and a three-part document-management footer. This demonstrates that the
library still accepts arbitrary application content rather than introducing a special-purpose page
control. Live review tightened the content padding, reduced the sample data enough to remove the
scrollbar and added an eight-DIP render gutter around the dashboard. That gutter is balanced by an
equal shell-padding reduction, so the approved layout does not move while every card shadow receives
enough room to render without being clipped by the `ScrollViewer`. A direct same-process and restart
mode-switch check confirmed that `Glass2007` returns to its previous Acrylic rail and original page
unchanged, while Classic restores the same real orb. Final automated gate: **325 logic tests plus one
visual test covering 62 approved images**.

### 3.93 S7 Office 2007 window-frame final verification — 2026-08-13

S7 is complete. Proportional geometry checks cover 100/125/150/175/200% for the frame, maximized
overhang compensation and native maximize hit bounds. Live testing used a real per-monitor-v2
125% 1920×1200 primary display and 150% 2560×1440 secondary display. Repeated restored moves
125%→150%→125% updated the HWND DPI and caption metrics from 58×42 to 69×51 physical pixels and
back without restarting. All eight resize edges/corners retained their native hit codes and direct
drag-resize behavior. Real caption clicks passed minimize, maximize, restore and close; maximize
produced the expected WindowChrome overhang while the measured root inset kept the title, ribbon,
document and status surfaces flush with the monitor work area.

The mixed-monitor pass caught one issue that single-monitor and synthetic `WM_DPICHANGED` checks
could not: WPF `PointToScreen` retained the previous monitor's screen transform for the realized
maximize template part, moving `HTMAXBUTTON` away from the rendered button after a DPI transition.
`RibbonWindow` now transforms the part into window-client DIPs, combines it with the native
`ClientToScreen` origin and applies the window's current `DpiScale`. Focused contracts cover that
conversion at all five required scales, negative monitor coordinates and mirrored corners. On the
real 150% monitor the corrected region measured 69×51 pixels exactly over the visible button; the
Windows 11 Snap Layout flyout appeared, the custom non-client hover/pressed bridge remained visible,
and maximize/restore completed from that same region. Moving back to 125% restored the 58×42 region.

Active/inactive Acrylic, maximized Acrylic and the deterministic opaque fallback were inspected
live. Acrylic remained confined to the transparent title/restored frame, inactive treatment stayed
quieter, and the opaque fallback preserved the same frame geometry. The first post-gate mitigation
for persisted `Classic2007` startup used temporary title-row clipping; §3.94 replaces that workaround
because its animated transition could still expose a partial orb. Actual Acrylic composition remains
a live-only approval rather than a snapshot baseline.

### 3.94 Classic2007 Backstage orb proxy and seamless design switching — 2026-08-13

Classic2007 now owns a private Back button above `BackstageAdorner`; it does not reparent or duplicate
the real application button's command/state ownership. Both presentations render the same
`RibbonKit.Templates.ApplicationOrbChrome` data template, so the sphere, glyph and dynamic theme
resources remain pixel-identical. The real button stays measured and arranged in its original ribbon
slot, but is opacity- and hit-test-suppressed for the entire Backstage lifetime. Ordinary Backstage
designs omit the proxy, while Classic attaches it at the real button's transformed bounds. The old
`RibbonWindow` title-row clipping workaround and `Tag.Backstage.Design` template state are removed;
the historical two-pane application-menu overlay continues to move the real button as before.

The proxy carries the localized Back tooltip and automation name, participates in window-chrome hit
testing, closes through `IsBackstageOpen`, and rotates only the shared `OrbGlyph`. Its content is an
inert string: an early implementation used the owning `Ribbon` as data, which tried to give that
already-parented control a second logical parent and crashed on open. The private proxy template also
pins its `ContentPresenter` to `VerticalAlignment=Top`. Without that explicit alignment, the orb's
negative top margin was stretched inside the proxy and the rendered sphere moved down on open.

Focused lifecycle contracts cover persisted Classic→Glass→Classic switching, stable real-button
parentage/suppression, shared chrome identity, inert proxy content, top alignment, accessibility and
unchanged application-menu layering. A live 60-fps named-window capture verified clean Classic and
ordinary open/close animations. Frame analysis held the orb's yellow-glyph bounds at y=22…69 before
and after proxy takeover, confirming no vertical movement; ordinary Backstage showed no partial
Classic orb. Final gate: **339 logic tests plus one visual test covering 62 approved images**, with
zero build warnings or errors.

A follow-up first-use capture exposed two timing edges. On the first Classic open, the proxy's
`ContentPresenter` had not inflated the shared data template when rotation was requested, so
`OrbGlyph` was absent and only later opens animated. The request is now retained until the proxy's
first Loaded/layout pass, before first rendering, and cancelled on design/template teardown. Ordinary
Backstage also used to restore the real ribbon orb only in the close-completion callback. It now
restores that orb at close start beneath the departing Backstage surface, letting the slide reveal it
naturally; Classic alone keeps the real orb suppressed until its proxy completes the exit rotation.
Fresh-process 60-fps captures verified first-open rotation and both close timings.

### 3.95 Office 2010 Backstage begins below the tab headers — 2026-08-13

`Classic2010` now uses the Word 2010 File-surface placement that was originally deferred in §3.27.
The title bar and complete tab-header row remain visible; the Backstage begins at the live bottom of
that row and covers the ribbon body, below-ribbon QAT/message rows, and document. This is still the
window-level `BackstageAdorner`, not an in-ribbon host: the shared `RibbonTabControl` template exposes
`PART_TabHeaderHost`, and the adorner transforms that part's bottom edge into its adorned-root
coordinates on every relevant layout pass. Resizing, theme/DPI reflow, and tab-row QAT changes can
therefore move the boundary without a fixed theme-specific height. A custom template that omits the
part retains the established full-content overlay rather than failing open or clipping content.

The exposed chrome is functional rather than decorative. The real File button stays visible and
checked, title-bar QAT content remains available, and the Backstage's redundant round Back button is
collapsed only while the inset placement is active. Clicking File again, the selected ribbon tab, or
a different tab closes the surface; a different tab continues through normal selection. Hit testing
above the inset passes through the adorner to those controls. Other Backstage designs retain their
full-content placement, including both Office 2007 paths and the Classic2007 orb proxy. A remembered
`Translucent` preference does not hide the adorned root in this mode because doing so would also erase
the deliberately exposed title/tab band.

Three focused contracts pin the named template anchor, live inset/reflow and hit routing, plus the
placement-owned Back-button state. Live testing on the 125% per-monitor-v2 display confirmed the
Word-style boundary, visible File/tab strip, selected-tab and different-tab close paths, and the
unchanged Office 2010 page shell. The future Office 2010 Aero/frame treatment remains separate work;
this placement deliberately leaves the complete caption/tab band available for that material pass.
Current gate: **342 logic tests plus one visual test covering 62 approved images**, with zero build
warnings or errors.

### 3.96 Office 2010 Backstage tab-strip polish — 2026-08-13

The below-tabs `Classic2010` surface now completes the visual handoff at the exposed tab row. A
device-snapped three-DIP `Classic2010TopSeam` spans the Backstage top edge using the dedicated
`ApplicationButton.MenuOpenBottom` token. Office 2010 sets that token to the checked File gradient's
bottom stop, so the rule reads as a direct continuation of the button while still separating Home
(the first navigation item) from it. Every palette defines the same key, and custom accents derive it
beside `MenuOpenBackground`, preserving the shared-template/theme-token boundary.

The checked File trigger had still been painting the raw flat `Accent` brush even though Office 2010
already supplied a dimensional `ApplicationButton.MenuOpenBackground` gradient. It now consumes that
dedicated open-state token, retaining the established white Backstage foreground. This fixes the flat
open state without altering the short mouse-down `PressedBackground` state or the separate
application-menu-open shadow path.

While a real Backstage (not `RibbonApplicationMenu`) is open, the ribbon-tab template now suppresses
selected fill/border/connect-foot/indicator chrome and restores the normal tab-strip foreground. The
logical `RibbonTabControl.SelectedItem` is deliberately untouched, so the same selection returns on
close. Exposed tabs remain hoverable with the ordinary unselected hover treatment and retain the §3.95
close/select behavior. Both the tab-control host and every `RibbonTab` receive a read-only
`IsBackstageActive` state. Publishing it directly on each tab matters for joined ribbons: a merged
contextual tab did not reliably resolve the earlier ancestor binding in the live Showcase even though
the isolated snapshot did. Collection and merge changes now synchronize newly arriving tabs too.

The historical `Backstage.ContentShadow` remains available to older/full-overlay generations, but
`Classic2010` no longer applies it to the complete `ContentArea`. Instead a clipped 14-DIP host at
the pane divider casts only to the left through the dedicated `Backstage.ContentLeftShadow` token.
Office 2007/2010 palettes use a nine-DIP blur with the three-DIP projection and opacity raised to
0.48. The clipped host keeps that darker, softer edge off the top connector. The
navigation host also gains three DIPs of top padding, and content top padding grows by the same amount,
so neither the first selected row nor page content sits beneath the seam.

Focused contracts pin the seam geometry/color, checked File token, clipped left-only 2010 shadow,
padding, token parity, and host/per-tab Backstage ownership. Intentional shell updates and the new
`office2010-merged-backstage-tabs-100` scene were reviewed. Live testing on the 125% per-monitor-v2
display confirmed the thicker connected edge, clean top boundary, darker left divider depth, padded
first navigation row/content, open File depth, ordinary close/select paths, and a selected merged
`Chart Design` header becoming inactive while Backstage owns the row. Current gate: **347 logic tests
plus one visual test covering 63 approved images**, with zero build warnings or errors.

### 3.97 Office 2010 Aero-inspired frame prototype — 2026-08-20

The optional `RibbonWindowFrameAppearance` contract now includes `Office2010Aero` beside the
established 2007 value. It remains an authored presentation choice rather than a backdrop request:
the host still selects Acrylic independently, and an unavailable or disabled material uses the
tokenized opaque fallback. The restored left/right/bottom frame, title tint, reflection, caption
foreground/state and inactive fallback reuse the proven `RibbonWindow` frame composition with
Office-2010-specific palette values.

Unlike the 2007 treatment, the 2010 material continues through the complete live tab-header row.
The ordinary ribbon root becomes transparent only for this appearance; the groups body, document
surface and status area remain opaque. The tab row owns a deterministic Aero fallback layer which
fades to zero only after Acrylic is accepted. It and the title use the same fallback brush plus the
same live tint brush/intensity, so the caption and tab row form one surface in both opaque fallback
and Acrylic modes. Acrylic itself supplies the textured blur; no authored white readability box or
QAT-specific veil remains above it. File, ordinary/contextual tabs, both QAT placements and caption
controls therefore sit directly on the shared material.

The Showcase's Frame group now exposes mutually exclusive, generation-gated `2007 Aero` and
`2010 Aero` selectors. The existing tint/accent controls apply to either appearance, persistence
round-trips the new enum value, and changing themes leaves a mismatched preference dormant rather
than applying 2010 chrome to another generation. When 2010 Aero is active over Acrylic, the Showcase
keeps the document/status surface opaque so material remains confined to the authored frame,
caption and tab row. Focused template, token, persistence and Showcase contracts accompany the
change. Current automated gate: **350 logic tests plus one visual test covering 63 approved images**,
with zero build warnings or errors. Live Acrylic/fallback comparison and final visual tuning remain
the user-review gate for this prototype.

Early live passes tested a separate feathered white readability pane, first at full height and then
as a shorter blurred layer. That layer was removed after Acrylic proved to already provide the
required textured blur; retaining it only introduced an extra band and obscured material continuity.
The pale Office 2010 outer client edge was instead darkened directly.

Office 2010 light replaces the white outer client edge with a translucent blue-gray rule, but its
upper glass is now structurally continuous. The title-bottom rule is suppressed for `Office2010Aero`,
the frame's side bevel begins at 69 DIPs (the 34-DIP caption plus the live 35-DIP tab row) rather than below the 34-DIP
caption), and the tab row owns a tint layer bound to the same host `AeroFrameTint` and
`AeroFrameTintIntensity` as the title. Accepted Acrylic therefore removes both opaque fallback
layers while preserving one tint percentage across caption and tabs. The title-only reflection and
grain overlays are also suppressed for this appearance because their relative brushes restart at
the row boundary; Acrylic already supplies the texture continuously. Office 2007 and both
dark-generation edge treatments remain unchanged. Office 2010 tab and tab-strip-control hover fills
now carry alpha instead of replacing that shared material with opaque amber/blue rectangles. The
File tab's left margin is zero only while `Office2010Aero` is active and those two DIPs move into its
left padding, so the blue chrome reaches the ribbon body's left rule without shifting either the
label or right edge. The ordinary 2010 frame retains its original two-DIP left margin and narrower
padding, preventing the File tab from touching the non-Aero frame.
The Acrylic tab hover keeps its alpha-bearing amber fill and connector, but its one-DIP outline now
uses a dedicated fully opaque, stronger amber token instead of the paler shared control border.
Office 2010 Aero caption buttons follow the same separation: minimize/maximize/restore use neutral
translucent hover/press washes with opaque gray outlines, while Close uses translucent red with an
opaque red outline. Dedicated sibling state-chrome layers handle ordinary WPF mouse input. The
maximize/restore surface is different: advertising `HTMAXBUTTON` for Windows 11 Snap Layouts moves
its input into non-client messages, so `IsMouseOver` does not activate. The existing native bridge
now resolves the 2010 Aero fill and border resources explicitly and drives all three template-bound
properties; previously it recognized only 2007 Aero and therefore kept applying the ordinary 2010
blue gradient despite correct-looking XAML triggers.

Pixel inspection of the reported capture identified the protruding stroke as the frame inner-right
highlight, not the ribbon body's border: it began at the earlier assumed 68-DIP boundary, one row
above the actual 69-DIP body top. The temporary split/inset ribbon-right workaround was removed and
the original four-sided `ContentHost` border restored. The complete frame inner-right rule remains;
its top margin now matches the real caption-plus-tab-row boundary, so it joins the body at the top
and continues to the bottom frame without the extra row. Final live confirmation remains part of
the prototype's user-review gate.

### 3.98 Collapsed-group and Classic2007 follow-up fixes — 2026-08-20

The collapsed-group flyout now applies the same negative-four-DIP horizontal compensation already
used by the QAT overflow popup. Its visible border therefore begins at the collapsed button's left
edge instead of beginning four DIPs to the right because of the popup host's shadow margin. The
collapsed toggle's private template also gains the standard command disabled opacity (`0.4`). A
disabled group remains non-interactive rather than opening a flyout of disabled commands, but its
collapsed representation now communicates that state as clearly as the full group contents.

The Classic2007 orb proxy no longer snapshots `RibbonString.Back` when it is first created. Both its
tooltip and automation name bind to the shared live localization source, so replacing the provider
or calling `RibbonLocalization.Refresh()` updates an already-realized proxy. The Classic2007 frame
join is now visible only when the containing `RibbonWindow` actually uses `Office2007Aero`; ordinary
frames no longer receive the stray top rule. The Aero join also consumes the generation's existing
semi-transparent `WindowFrame.AeroInnerHighlight` brush instead of adding an opaque white edge, so
it reads as one continuation of the frame rule rather than a heavier second stroke.

The first implementation expressed that condition as an `AncestorType=RibbonWindow` binding in the
Backstage template. Live follow-up showed why that was incorrect: the Backstage lives in the window
adorner's separate branch, so the lookup returned no ancestor and left the rule collapsed even in
Aero mode. `Ribbon` now gives `Backstage` an explicit one-way binding to the real host window's
`FrameAppearance`; the private named frame part follows that state and the live Backstage design.
This also keeps programmatic frame changes current while the surface is already open.

Focused contracts cover popup compensation, disabled collapsed chrome, live proxy localization and
the explicit live Aero-frame binding. A fresh-process Showcase pass inspected the
collapsed/disabled group and both Classic2007 frame paths; the pre-test appearance preferences were
restored afterward.
Current gate: **355 logic tests plus one visual test covering 63 approved images**, with zero build
warnings or errors.

### 3.164 Crystal contextual tab study and compact button refinement — 2026-09-21

Crystal remains a Showcase-only Office 2024 overlay. Teal Picture Format and purple
Table Design tabs now derive glass surfaces, rims, readable darkened text and bubble
markers from their effective solid contextual color. Change tint exercises live updates;
Compare 2024 removes the adapter overrides. Gradient/custom context brushes fall back
to standard rendering. The adapter depends on the shared ContextualHeaderText part;
there is no copied tab template. Compact Clipboard/Text stacks scope a 3-DIP radius,
while large controls keep 8 DIP. Pressed buttons gain a deeper fill and visible lower rim.

API justification: the optional, null-default RibbonTab.ContextualSelectionBrush lets
consumers supply a material marker independently of contextual text color. Existing
callers retain contextual tint as the fallback. The marker now binds to both brushes,
so replacement and clearing update the selected tab immediately. Shipped API is intact;
the addition is recorded in PublicAPI.Unshipped.txt and XML documentation.

Release solution build: zero warnings/errors. Library: 400/400; existing visual test:
1/1. Focused Crystal tests additionally confirm mutable-brush updates and comparison
restoration (2/2). Writer full run: 465/472, seven failures including the recorded
WindowChrome cross-thread issue. Fresh-process follow-ups also failed on the expected
menu lacking Settings and a table-selection count (expected 2, actual 1). These remain
unresolved outside this change; no Writer code was edited. The user owns Crystal visual
checking, so no new live visual/DPI/RTL acceptance is claimed.

2026-09-22 File-panel follow-up: sample-only Modern Backstage palette and preview pages
reuse the accepted Crystal lighting and local tint variants. A top-level keyed
StaticResource alias resolved to the baseline fill in the realized-control test;
explicit DrawingBrush resources with nested geometry/brush references resolved the
intended Crystal fill. Keep the realized selection-fill assertion when changing these
resources. Showcase Release build and twelve focused Crystal/input tests passed;
Backstage live visual acceptance remains with the user.

The subsequent user-requested structural redesign uses a sample-only Backstage
template with a floating horizontal navigation island and separate content cards.
Compare 2024 clears this style to recover the shared layout. The overlay lives outside
the ordinary window namescope/resource route: an explicit scoped palette and binding
source are required for preview colors and document text to survive reparenting.
CrystalBackstagePresentation now owns that bridge. Thirteen focused tests passed,
including detached binding changes, minimum-width navigation, back requests and style
restoration. Live visual review remains with the user.

The floating layout is now optional. The default Glass sidebar preserves traditional
vertical navigation, bottom About placement and a large right-hand content area, with
Crystal surfaces and selection rims. The Appearance page switches layouts in place;
CrystalBackstagePresentation retains the chosen layout during palette replacement and
baseline comparison. Release Showcase build and eight focused Crystal tests passed,
including both navigation arrangements at the preview minimum width. Visual acceptance
remains with the user.

Crystal screen-tip scope note (2026-09-22): detached tooltips require their own palette
bridge. Once the palette is merged into ToolTip.Resources, its general ScreenTip.Border
outranks an implicit style's resource with that key. The preview's bridge therefore
publishes the dedicated glass rim directly in its own scope, removing it on baseline
comparison. A realized open-tooltip test covers tint replacement and restoration;
the focused Crystal/input filter passed sixteen tests. Visual acceptance remains with
the user.

Crystal option preview follow-up (2026-09-22): ElementName bindings from borrowed
group content to EnableOptionsToggle did not resolve after layout. Explicit binding
Source references fix both Options and Spacing panels. A realized CrystalPreviewWindow
test failed before the fix and passes for disable/re-enable afterwards; isolated
control tests alone had missed this preview wiring. The window now merges its icon
resources locally for standalone construction. Release Showcase build and twenty
focused Crystal/option-control tests passed; the user owns visual acceptance.

Crystal runtime token promotion (2026-09-24): the preview's 90 top-level brush,
drawing, effect and metric resources moved unchanged into the packaged
`Tokens.Crystal.Light.xaml` dictionary. It merges Office 2024 tokens for keys Crystal
does not override, while the Showcase dictionary retains its eight local control
styles. A tint variant must recolor the merged Crystal dictionary itself; recoloring
only the sample overlay silently leaves shared templates at the blue palette. The
sample still removes its entire overlay for Office 2024 comparison. This first slice
does not add a public theme enum or move host-owned blur and document underlay into
the control. Release Showcase and both runtime target builds passed without warnings;
the new standalone token test, focused tint and customization checks, and all four
fresh-process follow-ups passed. A grouped Crystal run passed 17/21 and hit the known
WindowChrome cross-thread cache failure in the other four. No new live visual or DPI
acceptance is claimed.
The user then inspected the preview and reported no perceptible difference on the
current display; this accepts the resource move, not the later main-window theme.

### 3.165 Crystal Light theme selection — 2026-09-24

`RibbonTheme.CrystalLight` appends public enum value 5, preserving the existing Office
values. `ThemeManager.Apply` and the design-only `Ribbon.DesignPreviewTheme` share one
mapping to `Tokens.Crystal.Light.xaml`; the net472 editor mirrors value 5 without a
runtime reference. Showcase offers Crystal Light in its main Theme group and saves
the choice separately from the Crystal preview window. It has no dark palette, so
Showcase hides the dark toggle while it is selected; a stored dark preference still
applies when returning to an Office theme.

When a custom accent is set, the shared semantic accent and selected-tab text follow
it. The Crystal reflective selected marker, checked wash and open File surface retain
their drawing brushes rather than flattening into solid colors. The preview's Glass
tint still rotates the full material separately. A focused runtime switch test covers
Crystal → Office 2024 → Crystal with accent and dark preference; a design preview
test covers the token URI and local scope, and Showcase preference serialization
round-trips the new value. Release solution build passed without warnings. Live
main-window appearance, designer interaction, DPI/RTL and Crystal snapshots remain
unverified.

### 3.166 Crystal main Showcase presentation — 2026-09-24

The user's side-by-side screenshot showed the normal Showcase retained a flat QAT
strip and square message bars after selecting Crystal Light. Shared token brushes
alone did not install the preview's host-owned presentation. The preview's implicit
control styles were split into a reusable Showcase dictionary; the main window merges
that dictionary and applies the existing QAT, message, utility, menu and popup
adapters only for Crystal Light. The preview still merges the same styles with its
window-scoped palette. Crystal selects the application menu as the default File
surface, while a separately saved explicit File choice is restored afterward.
Switching back removes the styles and adapters without changing Office resources.
Focused host switch and preview style checks passed; main-window visual acceptance
remains with the user.

### 3.167 Crystal Light first-phase gaps — 2026-09-24

The user's next screenshots exposed the remaining first-phase differences: a plain
contextual tab, merged chart tabs using their ordinary color treatment, Office 2024
scrollbars, an unstyled main options dialog, a split button without the Crystal
paired-hover treatment, ordinary Backstage, and an accent picker that changed only
semantic color. The main Showcase now uses the preview's contextual tab adapter for
its own and merged tabs, scopes the shared scrollbar style and Crystal Backstage
style to the Crystal selection's Modern Backstage, and applies the existing customization adapter to
the detached options dialog. Its accent picker now builds a fresh hue-rotated local
palette via `CrystalPalette.Create`; Office themes remove that window overlay.
The split button's idle companion takes the ordinary hover wash and its active half
receives a slightly stronger wash. A split example was added to the preview's Design
tab without changing its accepted Home layout. Focused main presentation, merge,
tint, Office restoration, and preview customization checks passed. Live visual
acceptance is still open. MDI, localization/RTL, modal tabs, and dark mode are phase
two by user direction.

Theme-contract follow-up: the stronger active split wash uses the shared
`RibbonKit.Brushes.Control.SplitActiveHover` token. Each Office base palette defines
it as Transparent (also inherited by its dark variant); only the Crystal host adapter
uses it, and that adapter removes its local override when Crystal is deselected.
The audit found that Crystal's other `RibbonKit.*` overrides already exist in every
Office base palette. `Crystal.*` resources remain scoped to the opt-in Showcase
adapters rather than becoming shared template tokens.

### 3.168 Crystal Backstage designs and shared split hover — 2026-09-27

The two accepted Crystal Backstage layouts are now distinct `Backstage.Design`
values, `CrystalSidebar` (5) and `CrystalFloating` (6). Their existing templates
move into a shared RibbonKit resource dictionary. The common Backstage style
selects either template and its matching navigation panel only for those explicit
values; the Office designs keep their existing templates. The main Showcase adds
both choices beside its Office layout choices and persists the selected value.
The preview uses the same design values and restores Modern during comparison.
The detached Backstage still receives its own palette scope.

The split-button helper is removed. Both halves' hover triggers now read
`RibbonKit.Brushes.Control.SplitActiveHover`; Crystal defines the stronger wash,
and every Office base palette defines the same brush as its ordinary hover.
The 2013/2019/2024 dark variants override it to match their dark hover brush;
the 2007/2010 dark variants inherit the unchanged base hover.
The new shared template behavior therefore preserves the Office appearance.
An attempted `StaticResource` alias resolved to a neighboring brush during WPF
resource loading, so the Office tokens duplicate their hover brush definitions.
Focused resource and layout checks cover switching between the two Crystal
designs, returning to Modern, preview comparison, and Office split parity.
Live appearance and mixed-monitor DPI remain for user review.

Screenshot follow-up: the first layout check supplied `Office2024.xaml` as a host
resource, which hid a detached Backstage lookup failure. In a real open adorner,
the Crystal template keys were unavailable through `DynamicResource`, so the
control kept its Classic template. `Controls.Backstage.xaml` now merges the
Crystal templates directly and its design triggers resolve them with
`StaticResource`. A focused detached-window test opens the actual Backstage,
checks both Crystal layouts, and checks the Modern return under Office 2024.
The Crystal split companion outline also uses a stronger border brush; its
color remains scoped to Crystal.

QAT height follow-up: a below-ribbon Crystal drawer measured 28.8 DIPs with a
plain small button and 30.4 DIPs after a dropdown joined it. The Crystal
palette now reserves 32 DIPs on the shared drawer template; Office palettes
retain a zero floor. A focused layout check adds both a dropdown
and split button without moving the drawer's lower edge. Office palettes keep
their existing row sizing.

Floating Backstage width follow-up: its inner grid had a 1040-DIP cap, leaving
large side gutters in a maximized window. The Crystal floating template now
uses the available width inside its 28-DIP side margins. A focused layout check
covers ordinary and 1600-DIP widths; the sidebar template is unchanged.

Shared-template audit follow-up: message-row margin and corner radius, and the
below-ribbon QAT minimum height, now come from shared template tokens instead
of Crystal code setting realized parts. Crystal retains its 2-DIP row spacing,
10-DIP corners, and 32-DIP QAT floor. Every Office base palette supplies zero
for these new metrics, including the effective dark palettes that inherit the
base values. The existing split-active hover brush matches ordinary hover in
all Office light/dark palettes; Crystal keeps its stronger active wash.

### 3.169 Crystal File hover rim in the shared template — 2026-09-27

The File button's existing `InnerRim` now uses separate thickness and idle-opacity
tokens, with a shared hover trigger that requires mouse-over without press or open
state. Crystal supplies the accepted 1-DIP glass rim and hides it at rest; its
`ApplicationButton.InnerGlow` drawing matches the tab hover rim. Office palettes
retain their previous inner-rim thickness and full idle opacity, including the
Office 2010 radial glow and all dark variants. The Showcase-only
`CrystalFileHover` part adapter and its call sites are removed. No public API was
added. A Release Showcase build and focused Crystal rim, preview, and main-window
switch checks passed. The Office 2010 File-rim contract checks passed; a separate
unchanged dropdown hover-consumer count check in that class still fails. Live
hover appearance was accepted by the user on 2026-09-27. The separate
`Office2010ThemeContractTests.Every_ribbon_button_family_consumes_the_shared_hover_glass`
case for `Controls.DropDowns.xaml` reports 3 consumers against its threshold of
5. The user deferred that investigation.

### 3.170 Minimized tab shape in the shared template — 2026-09-27

The Showcase-only `CrystalTabShape` helper previously bound
`Ribbon.IsMinimized` and overrode two tab metrics in the ribbon's resource
dictionary. The shared `RibbonTab` template now applies minimized corner
radius and border thickness from dedicated dynamic tokens to every header.
Crystal keeps its accepted top-only 8-DIP corners while expanded and rounds
all four corners at 8 DIPs when minimized; its reserved border changes from
`1,1,1,0` to `1`. The helper and its main-window and preview call sites are
removed. All five Office base palettes set their minimized metrics to their
existing tab metrics, and the dark palettes inherit the same geometry. Focused
checks cover Crystal state and comparison switches and Office light/dark token
parity. Live minimized-tab appearance remains for user review.

### 3.171 Crystal body-scroll geometry in the shared template — 2026-09-27

Body scroll arrows and tab scroll arrows share the left/right button templates.
The body instances now carry an internal `BodyScroll` marker, letting those
shared templates select `RibbonKit.Metrics.BodyScrollButtonCornerRadius` without
changing the tab arrows. Their width reads
`RibbonKit.Metrics.BodyScrollButtonWidth` directly from the ribbon template.
Crystal keeps the accepted 32-DIP width and 10-DIP corners. Every Office base
palette supplies the previous 22-DIP width and its existing control corner
radius; dark variants inherit those values. `CrystalUtilityChrome` no longer
sets or clears these geometric properties. It still owns the utility rim and
scroll-arrow surface switching; moving those across the several shared utility
templates is the next bounded pass. The focused preview interaction and Office
light/dark token checks passed. Live appearance after this migration remains
for user review.

### 3.172 Compact Showcase theme and Backstage choices — 2026-09-27

The main Showcase View tab now puts its six theme choices in a compact
`InRibbonGallery`; the separate Crystal preview action still opens its own
window. The seven Backstage designs are menu rows in one
`RibbonDropDownButton`, whose label shows the current design. Both selectors
are synchronized when appearance preferences are restored. Theme selection
continues through the existing `ApplyTheme` path, and Backstage selection
continues to persist the design and notify the File surface.

An initial `SelectedIndex` on the gallery fired `SelectionChanged` while
`MainWindow.InitializeComponent` was still constructing the window, before
the File-surface controls and application-menu field were ready. Initial
selection now happens after initialization under the synchronization guard.
A focused headless window test covers construction, theme selection, and
Backstage selection without touching saved preferences. The Release Showcase
build passed. Popup layout and live visual acceptance remain with the user;
no Showcase process was launched during this pass.

### 3.173 Crystal Acrylic surface transparency — 2026-09-27

The shared `RibbonTabControl` body now uses `RibbonKit.Brushes.Ribbon.BodyBackground`.
Every Office light and dark palette defines it with the former body paint;
Crystal also retains its original paint when Acrylic is inactive. Dropdowns and
gallery popups continue to use the opaque `ContentBackground` token, so the
new body translucency cannot wash out detached surfaces.

When the Showcase actually activates Acrylic under Crystal Light, its scoped
palette softens the ribbon body, selected tab fill and marker, title bar, and
window/status background. Turning Acrylic off, changing the tint, or switching
themes rebuilds or removes that palette; no public API or Office paint changes.
The focused Crystal presentation test covers token parity across all Office
light and dark palettes, the realized body brush, translucent overrides, and
restoration. Live Acrylic appearance remains a visual review gate.

Acrylic hover follow-up: the accepted blue command hover wash blends into the
more saturated material in the user's screenshot. The Showcase's Acrylic-only
Crystal palette now replaces that shared hover token with a brighter, neutral
glass wash; the existing rim and non-Acrylic Crystal brush remain unchanged.
The focused presentation test confirms the Acrylic value and restoration after
turning Acrylic off. The new hover contrast still needs live visual acceptance.

### 3.174 Optional Acrylic glass treatment across Showcase themes — 2026-09-27

The later screenshot clarified that the faint hover was the tab header marker;
the command-button hover from §3.173 was already accepted. Its exact Acrylic
wash is retained. A brighter tab hover fill and rim, plus matching companion
and active-half split-button washes, now share the same window-scoped
`AcrylicGlassPresentation` as the ribbon-body, selected-tab, title-bar, and
status-bar opacity changes. The shared control templates still resolve theme
tokens; no RibbonKit public API or theme baseline changed.

The Showcase View tab has a separate Glass look switch. Existing preferences
default to the current behavior: Crystal applies it with Acrylic, while Office
themes retain their prior Acrylic appearance. The switch may override that
default for any theme and persists separately from the DWM backdrop choice.
The overlay is rebuilt after theme, tint, dark-mode, and backdrop changes, and
is removed whenever Acrylic is inactive. Focused presentation, selector, and
preference checks cover palette restoration, Office opt-in, and persistence;
live contrast and width/overflow remain visual review gates.

Tab-hover contrast follow-up: the first glass hover wash was too close to the
selected tab in the user's screenshot. Its light and dark alpha and hover-rim
opacity were reduced; the selected marker, command-button hover, and split
hover values remain as accepted in the preceding pass. The focused presentation
check covers both light and dark hover tokens. Live appearance still needs a
new screenshot.

Split-button and switch follow-up: vertically stacked split halves used to
paint both edges at their join. The shared template now reads a border-thickness
token for the primary half. Crystal omits its bottom edge, so the lower half
draws the divider once; all Office light and dark palettes retain their existing
one-pixel border. The Showcase Glass look switch now uses a large icon and
displays its label. Focused checks and WPF renders in both glass states covered
the controls; native Acrylic compositing still needs live visual review.

### 3.175 Crystal translucent Backstage option — 2026-09-27

The Showcase's Transparent Backstage switch already sets `Backstage.Translucent`,
and the Ribbon hides content behind the open surface so DWM Acrylic can show.
Neither Crystal layout template responded to that property: each kept its
window-colored root and opaque navigation surface. Both shared Crystal templates
now clear their root fill when translucent and use the existing translucent
navigation brush token. Crystal supplies a tinted, alpha-bearing value for that
token; Office palettes keep their previous values. The content card stays opaque
for text readability. Turning the option off restores the original Crystal
brushes without changing the chosen layout or adding a public API. A focused
realized-template test checks both Crystal layouts and the on/off transition;
native Acrylic appearance remains for live screenshot review.

### 3.176 Acrylic hover accent and File wash — 2026-09-27

The Glass look overlay's bright command hover had a fixed near-white color, so
it stayed neutral when the user selected another accent. The overlay now blends
the active accent into that wash while retaining its previous light/dark alpha.
The split-button companion and stronger active-half washes use the same tint.
The File button's hover fill now resolves to the same brighter glass brush as a
tab header; its selected and pressed surfaces are unchanged. These remain
Showcase-scoped overrides, removed when Glass look is off. A focused Crystal
presentation test checks accent changes, File/tab resource parity, Office opt-in,
dark alpha, and restoration. Live Acrylic color balance awaits screenshot review.

### 3.177 Crystal selected-tab bridge — 2026-09-27

The user's Acrylic screenshot showed the selected Crystal tab ending above the
tab strip's last pixel and the ribbon body's white top border. The shared tab
foot and body-side `PART_ConnectNotch` already bridge these two pixels for
connected Office themes, but Crystal inherited Office 2024's disabled tokens.
Crystal now supplies the one-pixel foot transform and visible selected-foot and
notch brushes. A follow-up screenshot showed a white strip when Glass look was
off: the opaque notch token used a different color from the foot, while the
Glass look overlay matched them. Both Crystal tokens now use the same
accent-tinted color, so the bridge stays consistent as Glass look changes
without a Showcase-only notch override or layout adjustment. Office 2024 still
has no notch. A focused realized-control test checks Crystal geometry, foot/notch
color in both glass states, tint changes, and Office 2024 comparison; live
ordinary and Acrylic appearance, including minimized tabs, still need review.

### 3.178 Crystal hover tab closes below the header — 2026-09-27

The unselected Crystal tab inherited Office 2024's top-only hover geometry. A
separate, hit-transparent `HoverChrome` now draws the hover outline in the shared
tab template. Crystal supplies full 8-DIP corners and a complete one-pixel border;
its unselected hover hides the connecting foot. The selected tab continues to use
the existing foot and body notch. The normal and Backstage-active hover triggers
use the same overlay, and Office light/dark palettes retain their original hover
corner, border and foot values.

Changing the measured `HeaderChrome` border on hover shifted the ribbon body by
one pixel, so the outline was moved to a visual overlay. A focused WPF check
confirms the overlay's geometry, unchanged tab/ribbon desired sizes when it is
shown, and Office token parity across light and dark variants. Live hover shape
and body position still need screenshot review. The reported 200% icon-bearing
gallery selection clipping is recorded separately in the future themes plan.

### 3.179 Crystal hover and selected tabs share an open lower edge — 2026-09-27

The closed hover pill in §3.178 looked inconsistent when a tab became selected:
Office 2007/2010 avoid that shape change by using top-rounded, open-bottom chrome
and a strip-side hover foot. Crystal now uses the same geometry with its existing
8-DIP upper corners. Its hover foot has the same translucent paint as the hover
fill, including the Showcase's optional Glass look overlay and tint changes.
The selected body-side notch remains reserved for the selected tab. The
separate `HoverChrome` layer and its three hover metrics from §3.178 were
removed: the original measured `HeaderChrome` now changes only brushes on
hover. Its existing minimized trigger still supplies the accepted full-corner
shape. A focused geometry check confirms the brush swap leaves tab and ribbon
measurements unchanged, and presentation checks cover the Glass look foot tint.
Live pointer appearance remains for user review.

### 3.180 Crystal hover foot clipping — 2026-09-27

The follow-up screenshot showed the open-bottom hover tab's side outline ending
before the ribbon body. A focused realized-control check placed `ConnectFoot` at
37 DIP while `PART_TabScroll` clipped at 36.8 DIP: its one-pixel border was fully
outside the strip. The shared template now reads a separate render-only hover
transform. Crystal keeps its hover foot at Y=0, inside the strip, while its
selected foot remains at Y=1 for the existing body notch. Every Office light
and dark palette keeps the same hover displacement it had before. The focused
geometry check verifies the hover trigger, foot/strip intersection, token
parity, and unchanged tab/ribbon measure. Live pointer and DPI appearance
remain for user review.

### 3.181 Crystal hover uses one translucent surface — 2026-09-27

The visible hover foot from §3.180 still crossed a fractional strip edge at some
scales and painted the translucent hover wash twice, making a darker band. A
realized-control check shows the ordinary `HeaderChrome` already extends to the
strip's clip edge. Crystal now suppresses the hover foot's fill and side borders
with a shared opacity token; Office light and dark themes retain opacity 1.
The hover stays top-rounded and open-bottom, while the selected tab keeps its
existing opaque foot and body notch. The separate hover transform and its
Glass look foot override were removed. A focused check covers the header/strip
geometry, token parity, selected foot restoration, and unchanged tab/ribbon
measure; live pointer appearance remains for user review.

### 3.182 Crystal hover rounding and accent-tinted QAT glass — 2026-09-27

The follow-up screenshot still showed an awkward lower edge on the open-bottom
hover tab. The shared `RibbonTab` template now gives its existing measured
`HeaderChrome` a hover corner-radius token. Crystal uses an 8-DIP radius on all
four corners; selected tabs restore their connected upper-only shape. Office
light and dark palettes keep their previous hover radius, and minimized tabs
retain their existing radius trigger. This does not add a second visual layer
or change border thickness. The translucent hover foot remains suppressed for
Crystal, avoiding overlapping wash at fractional DPI; the live lower-edge
appearance still needs screenshot review.

The below-ribbon QAT had reused `Tab.HoverBackground`, which Glass look changes
to a neutral quiet hover wash. Its shared template now reads
`QatExtender.Background` instead. Crystal's normal token matches its former
hover brush and rotates with the accent; optional Glass look gives the QAT an
accent-tinted translucent brush independently of tab hover. Every Office light
and dark palette supplies the exact previous QAT paint, including the 2007 and
2010 gradients. Focused tests verified tab measure, Crystal tint and Glass
switching, and Office token parity; live visual acceptance remains open.

### 3.183 Glass tab hover follows the accent — 2026-09-27

The accepted rounded hover still looked blue against a warm accent because
optional Glass look replaced `Tab.HoverBackground` with a fixed blue-white
wash. It now uses the same accent-aware color recipe as the QAT while retaining
the quieter tab-hover alpha (0x50 light, 0x30 dark). The File button shares the
tab hover token, so it tracks the accent as well. The independent QAT token
remains available for later tuning. Focused presentation checks cover purple,
green, red, and dark glass colors; live color acceptance remains user-owned.

### 3.184 Crystal Light MDI Demo integration — 2026-09-27

The detached MDI Demo had only the app-wide Crystal base palette. Its child
captions, borders and client area inherited Office 2024 MDI values, while its
white host background, text editors and document tabs missed the main Showcase's
local Crystal tint. Existing `Mdi.xaml` templates already consume MDI tokens, so
Crystal Light now overrides those keys without a new template or public API.
Showcase scopes the tint and editor paint to the demo window, enables its contextual
document tabs, and updates open demos when the main theme or tint changes. Leaving
Crystal removes that window scope and restores the Office presentation.

Two focused checks passed: one realizes a child and verifies its token-backed
caption, editor and merged tab through tint and theme changes; the other verifies
all reused MDI keys resolve in every Office base and dark variant. The Showcase
Release build passed. Live MDI appearance and DPI/RTL remain for user screenshots.

### 3.185 Connected first tab with no File button — 2026-09-27

The MDI screenshot showed the first selected tab's left outline ending above the
rounded top-left ribbon body corner. The same layout occurs when modal Print
Preview hides File. The shared `RibbonTabControl` template now insets the first
visible header-row item when both the application button and merged caption icon
are collapsed. A tab-row QAT takes the inset, keeping the normal gap from QAT
to tab; with the QAT in the title bar or below the ribbon, the tab panel takes
the inset. The selected-tab marker and connected body notch follow the measured
tab. RTL uses an inset on the opposite side. The body retains its rounded
corners, including with a below-ribbon QAT or message row. A visible merged
caption icon already reserves the leading space, so both use normal margins.

Every Office base palette defines the tab and QAT LTR/RTL margins, inherited by
its dark variant. Office 2007 adds a small leading inset; the flat
2010/2013/2019 and pill-tab 2024 palettes retain their normal spacing. Crystal
Light uses a larger inset to clear its 14-DIP body radius. Focused
realized-control checks cover File visibility, merged caption icon, tab-row QAT
spacing, RTL, below-ribbon QAT, Office 2007, Office 2024, and modal Print
Preview. Live screenshot acceptance remains with the user.

### 3.186 Crystal Light Localization/RTL lab and Backstage templates — 2026-09-28

The detached Localization/RTL lab inherited Crystal's shared control tokens, but
its host-owned menu, Backstage, message, popup and QAT presentation did not use
the main Showcase's Crystal adapters. Its built-in Options dialog also missed
the Crystal customization styling. The lab now reuses
`CrystalMainWindowPresentation` and `CrystalCustomization` instead of duplicating
their brushes or templates. `MainWindow` forwards theme and tint changes to the
open lab, as it does for MDI demos. The lab retains its independent RTL direction,
pseudo-localization provider and File-surface subscription.

One focused offscreen check covers a realized RTL lab, tinted Crystal resources,
the application menu, styled Quick Access Options, tint replacement and Office
restoration. The lab integration is Showcase-only. Live popup, menu and dialog
appearance remains for user screenshot review.

The Crystal Backstage templates had fixed English Back/File labels. Both designs
now bind control-owned labels, tooltips and automation names to live
`RibbonString` values, and the Back arrow follows the inherited direction.
WPF already mirrors the Backstage layouts, check box/radio indicators and
customization tree through `FlowDirection`; no column or dock swaps are needed.
An initial check measured positions relative to an RTL element and led to
unnecessary swaps. A corrected check uses physical screen coordinates and
verifies the original layouts, both Backstage designs, live provider changes,
the option indicators, customization tree and return to LTR. The shared template
change adds no public API or theme tokens. Application-authored demo labels
remain the host's responsibility; live visual acceptance is still pending.

### 3.187 Crystal Light Print Preview host paint — 2026-09-28

Print Preview already uses RibbonKit's shared modal tab, so its ribbon inherits
Crystal styling without a new template or adapter. Its Showcase-owned preview
canvas, page border and page text had fixed grayscale brushes. The Showcase app
now defines their Office baseline as host resources, and its window-scoped
Crystal palette overrides those resources. The paper remains white. Crystal's
existing tint rotation updates the canvas and border while preserving readable
page text; leaving Crystal restores the original Office paint.

One focused noninteractive check covers modal entry/exit, a tint change and
baseline paint under every Office theme and supported dark variant. There is no
RibbonKit runtime or public API change, and no new shared theme token. Live
Print Preview appearance remains for user screenshot review.

### 3.188 Crystal dark palette — 2026-09-28

`ThemeManager.SetDarkMode` now loads `Tokens.Crystal.Dark.xaml` for the existing
`RibbonTheme.CrystalLight` value. The overlay reuses the Office 2024 dark baseline
for unchanged controls and replaces Crystal's shared glass, MDI, Backstage, KeyTip,
message and connected-tab brushes. It preserves the light palette's geometry and
drawing structure, which the Showcase contextual adapter uses for its marker.
There is no new public theme value, shared template or token key. Every shared key
in the overlay exists in all five Office palettes and their dark variants.

Showcase adds a dark window palette and dark resource scopes for its existing
input and option styles. The theme toggle rebuilds the active tint and refreshes
open MDI and Localization/RTL demos. Print Preview's canvas darkens while its
white paper and ink remain readable. Contextual tabs derive their dark treatment
from the actual palette in their resource scope, so the separate light-only
Crystal preview stays light even when the application has a dark preference.

Focused offscreen checks cover runtime light/dark/Office switching, dark host
styles, contextual markers, MDI, Print Preview and the RTL lab. The Showcase
was not launched; live material, contrast and DPI acceptance remain for user
screenshots. Chart Tools and the Options Editor need no further phase-two edit by
user direction; the deferred Office Glass and View overflow reviews remain open.

### 3.189 Crystal duplicate audit — 2026-09-28

The user accepted Crystal dark mode in the main Showcase, MDI Demo,
Localization/RTL lab, and Print Preview screenshots. This records those reviewed
surfaces only; popup states and DPI scales remain unreviewed. Chart Tools and the
Options Editor need no further phase-two work.

`Crystal.OptionTemplates.xaml` still copies much of the shared check/radio
structure, including native content, mark, hover and disabled behavior. Its
separate focus ring and selected glass lens are distinct from the shared
accent-filled indicators, including in the dark palette. Replacing these
templates with the shared ones would change the accepted appearance, so they
remain Showcase-scoped. They inherit `FlowDirection` and display the caller's
header; they add no localization branch. The shared Crystal Backstage dictionary
already owns both layouts, localized labels, direction-aware Back glyph and selection
behavior. The Showcase `Crystal.Backstage.xaml` dictionary only merged it.
Customization and message actions now merge the shared dictionary directly;
the wrapper was removed. No RibbonKit runtime template, token or public API
changed.

The wider pass found shared tokens and templates already driving ordinary
ribbon controls, split hover, MDI chrome, Backstage and scrollbars. Showcase's
`Crystal.ControlStyles.xaml` scopes glass input, gallery and ScreenTip paint;
`Crystal.Customize.xaml` styles Showcase's dialog presentation. The backdrop
capture, QAT/document underlay, menu shadow, utility rim and detached preview
bindings depend on host visuals or content, so they remain Showcase helpers.
These are distinct presentation choices rather than further safe wrappers to
remove in this slice.

Presentation wiring remains spread across these Showcase owners:

- `MainWindow.ApplyTheme` and `RefreshCrystalTint` apply the standard shell
  adapter; `UpdateDetachedDemoThemes` forwards changes to open demos.
- `LocalizationRtlDemo.ApplyCrystal` separately creates the same shell adapter;
  `MdiDemo.ApplyCrystal` scopes its own palette, editors and contextual tabs.
- `CrystalPreviewWindow.UpdateCrystalDetails` applies each preview adapter for
  comparison; its Backstage helper retains the document-title binding.
- `MainWindow.OpenOptionsDialog` and the RTL lab's Options path opt into
  `CrystalCustomization` for their own dialogs. Print Preview uses the shared
  modal-tab template and Showcase page-paint resources.

The next bounded slice is an optional Showcase-only registration for the
standard shell: add a small `CrystalShellRegistration` helper that constructs
`CrystalMainWindowPresentation` from the explicit window, ribbon, message bar,
menu and Backstage references supplied after `InitializeComponent`. MainWindow
and LocalizationRtlDemo each keep their own registration and call its `Apply`
on theme/tint changes; closing a detached window releases its registration.
Keep MDI's editor/contextual-tab treatment, preview comparison, and dialog
styling in their owning hosts. There is no need for global discovery, a broad
coordinator or a RibbonKit API. The deferred Office Glass, View overflow, 200%
gallery clipping and Office 2010 hover checks remain separate.

### 3.190 Crystal customization list frames — 2026-09-28

The user's main Showcase screenshot confirmed the message panel, and the Options
dialog screenshot showed square outer frames on the Customize Ribbon command
list and tree. The requested round treatment also applies to both Quick Access
Toolbar lists. All four controls are template parts of RibbonKit's shared
customization pages; their data, navigation and scrolling remain owned there.

Showcase's `CrystalCustomization` adapter now applies one local rounded frame
template to those parts. It preserves each control's background, border,
padding, item presenter and scroll viewer, while the Crystal row styles remain
unchanged. No runtime template, theme token or public API changed. The Showcase
Release build and three focused checks passed in separate processes: frame and
scrolling on both pages, existing tree behavior, and the detached RTL Options
path. The dialog was not visually reaccepted at the new corners; live review
and DPI scales remain with the user.

### 3.191 Showcase presentation portability audit — 2026-09-28

A Showcase presentation inventory exposed a gap after Crystal became a public
`RibbonTheme` choice: `ThemeManager.Apply` installs shared palettes, while the
sample still supplies several generic control appearances through local styles,
template-part changes and a `CrystalContextualTab` subclass. Another consumer
can use RibbonKit and its shared Crystal palette and Backstage designs, but
theme selection alone will not reproduce all of Showcase's Crystal visuals.
The proposed Showcase-only adapter registration would organize sample wiring
without closing that gap and is superseded in the future-themes plan.

The reusable candidates are input, option, ScreenTip and customization styles;
QAT, message, application-menu, utility and scrollbar chrome; contextual-tab
material; and full-material tint policy. They require bounded promotion to
RibbonKit with Office parity, RTL/localization and another-consumer checks.
Menu/popup snapshot blur depends on host visuals and should remain explicitly
optional pending a reusable integration contract. `AcrylicGlassPresentation`
is the only other named Showcase presentation adapter; it provides an optional
Glass look rather than the baseline Office themes. Preview comparison,
document-edge fade, MDI editor content, Print Preview paint, icons and
preferences remain application-owned. This audit changed documentation only;
no runtime/public API behavior or visual acceptance is claimed.

The same inventory also checked non-Crystal theme wiring. Showcase explicitly
selects the built-in `RibbonApplicationButtonShape.Orb` for Office 2007, picks
the application menu as its conventional File surface, and exposes optional
2007/2010 Aero frames, Backstage designs and DWM backdrops. The orb template,
menu, frame and Backstage behavior live in RibbonKit; Showcase chooses among
their public options and mirrors the File-surface state to its RTL lab. An app
using `ThemeManager.Apply(Office2007)` must opt into the orb and choose its
File surface separately. Those host choices were omitted from the initial
portability summary and are distinct from the Showcase-only Crystal styling
listed above.

The orb's built-in four-square glyph is fixed in the shared template. Writer's
`RKWF-026` records a separate missing host-level glyph override and its
app-owned workaround; Showcase does not have an orb-glyph adapter. No new
runtime API is approved by this inventory.

### 3.192 Theme-native Office 2007 orb default — 2026-09-28

The shared Ribbon style now reads a typed application-button-shape token. Office
2007 supplies `Orb`; the other base Office palettes supply `Tab`. Dark variants
inherit that value, and Crystal inherits Office 2024's `Tab`. WPF invalidates the
style's `DynamicResource` when `ThemeManager.Apply` or a consumer's merged token
dictionary changes, so an existing ribbon follows the theme without a new
effective-shape property. A local `ApplicationButtonShape` remains authoritative
through switches, and `ClearValue` resumes the token default. No public symbol
was added or changed. Showcase no longer assigns the shape during theme changes;
its application-menu preference and RTL lab forwarding remain separate. Writer
still restores its saved local shape using its existing normalization policy.

A new test project referencing RibbonKit alone proves the loaded control's shape
and realized orb across every theme, Office 2007 black, manual token changes,
explicit `Tab`/`Orb`, `ClearValue`, and Classic2007 proxy attachment. The designer
preview's local token lookup and existing Classic orb lifecycle checks passed in
six focused tests; six focused Writer appearance-preference tests passed. The
Release solution build passed with zero warnings and errors. The full solution
test run did not pass: the portability test passed, while the runtime and Writer
suites reported contract, source-inspection and WPF cross-thread failures outside
the new consumer test. The Office 2007 snapshot differed at 929 pixels in tab
and QAT detail; the same mismatch remained with the new style setter temporarily
removed. Its approved image was not changed. No Showcase live window, DPI,
popup or target-machine visual acceptance was run.

### 3.193 Theme-owned no-application header inset and snapshot renewal — 2026-09-28

The shared ribbon template already selected separate LTR/RTL no-application
tab and tab-row QAT margins from each theme. Comparing each value to its normal
margin showed zero additional inset in Office 2010, 2013, 2019 and 2024; Office
2007 adds four DIPs and Crystal adds 22 to clear their rounded body corners.
Their dark variants inherit the base values. No new token, public API or runtime
geometry change was needed. The template comment now states that a theme can
keep its normal margin when it needs no extra inset.

A consumer test referencing RibbonKit without Showcase verifies the realized
tab/QAT margins and corner clearance where needed, with and without the File
surface, tab-row QAT or no row QAT, LTR/RTL, all light/dark themes, live
theme switches and a manually merged token dictionary. It shares one STA WPF
`Application` with the orb-default test; WPF rejects a second `Application` in
the same test process.

The visual harness can now capture every scene and its mismatch diff without
changing approved images. All 63 current scenes were compared against their
approvals. Only nine Office 2007 images differed, at the header QAT and tabs;
the ribbon body and message surfaces matched. Those nine approvals were renewed
after reviewing their actual/diff PNGs. The Release solution build and focused
visual and portability tests passed. The full solution test run remained red:
one deferred Office 2010 hover-glass contract and seven Writer failures outside
this slice. No Showcase live window, real DPI, popup or user screenshot
acceptance was run; Writer integration remains deferred.

### 3.194 Portable application-orb glyph template — 2026-09-28

`Ribbon.ApplicationOrbGlyphTemplate` is an additive nullable `DataTemplate`
dependency property in the unshipped API list. `null` keeps the existing
four-square vector. The shared `ApplicationOrbChrome` still owns the sphere,
theme state brushes and named `OrbGlyph` rotation target; only the glyph child
changes. The real application button and the private Classic2007 Back proxy
each bind to the Ribbon's property through their own internal button state, so
changing or clearing it updates both without sharing a visual or depending on
a Ribbon ancestor after the real button moves into the application-menu overlay.
The proxy's content remains an inert string, with localized Back tooltip and
automation name. Writer's saved appearance and W-glyph workaround are unchanged;
RKWF-026 remains open for its separate migration.

A RibbonKit-only consumer test realized custom vector templates in both buttons,
verified separate glyph instances and unchanged sphere visuals, changed the
template while Classic2007 Backstage was open, restored the default with `null`,
checked the glyph-only rotation target and accessible Back name, and exercised
the application-menu and ordinary Backstage paths. Ten focused Classic/language
tests and the visual test covering 63 approved scenes passed. The Release
solution build passed with zero warnings or errors. The runtime suite passed
434 tests with the deferred Office 2010 hover-contract method excluded; no
Writer tests were run by user direction. The 1.0.0 package packed and validated
for clean net8/net9 WPF consumption after the validator allowed NuGet's current
`nuget.psmdcp` metadata filename. No Showcase window, live screenshot, real DPI
or popup acceptance was run.

### 3.195 Shared Crystal control resources — 2026-09-28

Crystal menu rows, combo/text inputs, galleries, check/radio controls and
ScreenTips now use the shared `Controls.*.xaml` templates with dedicated
`Tokens.Office*.xaml` and `Tokens.Crystal.*.xaml` keys. Office tokens preserve
their prior light/dark paint and accent behavior. The option templates retain
separate selected lenses and focus rings; Crystal's detached ScreenTip surface,
reflection and shadow resolve through theme resources. Showcase no longer merges
its copied control styles or input/option templates. Its tint palette now sets
the shared material keys, while its preview-specific ScreenTip tint scope remains
host-owned. This slice adds no public API.

The gallery popup needs a concrete brush because its content leaves the ribbon's
resource tree. Its resolver now uses the dedicated popup token and still honors
a nearer host-scoped ribbon-content background, including a local override from
an existing consumer. A WPF `StaticResource` element used as a dictionary alias
did not register a usable brush key; the final tokens use actual brush entries.
The next slice is customization pages, including their list/tree frames; shell
chrome, contextual material/tint and optional captured backdrops remain later.

The Release solution build passed with zero warnings/errors. The non-Writer
runtime suite passed 417 tests with the separately deferred Office 2010
hover-glass contract excluded. The RibbonKit-only consumer test passed with
Crystal light/dark, Office restoration, manual token merges, RTL labels,
detached ScreenTip, gallery popup theme switching and a local popup override.
The visual snapshot test passed its approved scene matrix. An earlier broad
runtime run unintentionally included Writer-named consumer-friction tests and
found the local gallery-popup regression; that test was not rerun after the
fix. The final runtime run excluded Writer-named tests, and the Writer test
project was not run by user direction. No Showcase live window, user screenshot,
DPI or target-machine popup acceptance was run. The accepted earlier Crystal
dark screenshots do not close those remaining gates.

### 3.196 Shared Crystal customization pages — 2026-09-30

Both built-in customization pages now obtain Crystal list/tree frames, rows,
navigation and action materials from shared Options/customization templates and
theme tokens. The tree retains its view-model expansion/selection bindings and
native scroll viewers; selection and keyboard focus use distinct border states.
Crystal primary actions retain the semibold label and 12-percent accent wash.
Separate primary/secondary corner tokens preserve the existing Office button
sizes. No public C# API was added or changed.

Showcase's `Crystal.Customize.xaml` was removed, along with its realized
template-part edits. `CrystalCustomization` now only scopes the preview's
scrollbar tint. That remaining visual difference belongs to slice 7; the
RibbonKit-only consumer does not depend on the helper. Application-provided
Options content and dialog-opening policy remain in the host. The plan also
records the user's final consolidation requirement: after the promotion slices,
move important Crystal preview-only demonstrations to the main Showcase before
removing the separate preview window and launch path.

The first snapshot run stopped at the Office 2024 RTL QAT customization scene.
Its approved, actual and diff PNGs showed fewer visible commands because Crystal
row margins had been applied to Office. Inspecting the native WPF template
established the original compact padding, margins and frame inset. Those values
now have Office tokens, while Crystal keeps its prior spacing. The final visual
test passed all 63 approved scenes without changing approvals or tolerances.

The Release solution build passed with zero warnings/errors. The final runtime
run passed 417 tests, excluding Writer-named tests and the separately deferred
Office 2010 hover-glass contract. The earlier focused Crystal/localization/dark
template run passed 34 tests. The RibbonKit-only consumer test passed with both
customization pages, light/dark switches, every Office baseline, manually merged
light/dark dictionaries, native scrolling, custom accent, localized RTL headers,
and navigation/list/tree/action focus separate from selection. Theme XML parsed
without duplicate direct resource keys. Diff review and `git diff --check`
passed. Writer tests, live Showcase, new screenshots, real DPI transitions and
target-machine popup acceptance were not run; those gates remain unclaimed.

### 3.197 Shared Crystal QAT drawer — 2026-09-30

Slice 6's first bounded pass moves the reusable QAT behavior from Showcase into
the shared ribbon template and Crystal light/dark tokens. The drawer keeps its
16-DIP inset from the default body, open upper rim, 10-DIP lower corners, compact
padding, 32-DIP height reserve, flipped glass border and zero-depth shadow.
Minimization uses a complete rim and 3-DIP gap; message-state trigger priority
keeps the existing drawer spacing and upper-corner geometry. Dedicated
`ContentCornerRadiusQatBelow` and `ContentZIndexQatBelow` resources coordinate
the body rounding and seam shadow while keeping the application menu above both.
`QatExtender.Border` and `QatExtenderShadow` separate drawer paint from body paint.
Office base/dark tokens retain their previous realized values. New metrics have
matching defaults in every base palette; dark palettes inherit unchanged metrics.
No public C# API or shipped API baseline changed.

A RibbonKit-only consumer first reproduced the missing lower body rounding,
then verified the shared replacement on its existing single STA/Application.
It exercises all placements, physical RTL ordering, mixed source-linked button,
dropdown and split proxies, stable drawer height, real minimized body collapse,
message combinations, overflow menu borrowing/return, light/dark and Office
switches, manual dictionaries and resource/local-value precedence. The dark
message case exposed the Office dark dictionary overriding Crystal's drawer
continuation margin; Crystal dark now explicitly reasserts those two metrics.
The layering regression now verifies the QAT seam token is conditional on
BelowRibbon and remains below the application-menu overlay for every theme.

After consumer verification, `CrystalQuickAccess` and its main/preview callers
were removed. The document-under-QAT and fade effects, comparison controls and
preview content remain in Showcase. Its message-only body rounding is retained
as a scoped `ContentCornerRadiusTop` override until the next message-bar pass.
Application-menu promotion also remains open within slice 6. Slice 10 still
requires moving important preview-only demonstrations into the main Showcase
before removing the separate preview window and launch path; neither was removed.

The Release solution build passed with zero warnings/errors. The focused run
passed 100 tests; the final runtime run passed 417 tests. Both exclude
Writer-named runtime tests and the separately deferred Office 2010 hover-contract
method. The RibbonKit-only consumer test passed. The final visual comparison
passed all 77 scenes. Its original 63 approvals and tolerances are unchanged;
14 new light/dark QAT baselines cover expanded/minimized, message combinations,
RTL, tab-row overflow and synthetic 100/200% rendering. Actual-image inspection
found that disconnected minimized fixtures still painted the body. The fixtures
now realize Ribbon's loaded lifecycle in an offscreen test window, reparent the
ribbon into a fresh capture root to avoid native-window offsets, and assert body
collapse before rendering. The corrected actual images were reviewed before
adding the new baselines. No existing scene differed in the full capture pass.
Theme XML parsed without duplicate direct resource keys. Final diff review and
`git diff --check` passed.

Writer tests and manual Showcase launches were not run. User-supplied QAT visual
acceptance, real DPI transitions and target-machine popup acceptance remain
pending; earlier Crystal dark screenshots and slice 5's automated checkpoint
do not establish these gates. Office Glass, View-tab overflow, 200% gallery
clipping, the Office 2010 hover investigation and Writer RKWF-026 stay deferred.

### 3.198 Minimized Crystal QAT corners above messages — 2026-09-30

Live main Showcase review supplied light/dark expanded and minimized QAT
screenshots without messages, followed by expanded light/dark screenshots with
Protected View. The minimized dark message screenshot exposed square upper
drawer corners. The user rejected that geometry: a minimized Crystal QAT must
retain all four rounded corners even when a message is present. This corrects
the message-state priority preserved in §3.197. The correction alone does not
establish live acceptance or complete the remaining slice 6 work.

The shared ribbon template now selects
`RibbonKit.Metrics.QatExtenderCornerRadiusMinimizedMessageBar` for the combined
BelowRibbon/minimized/message state. Crystal light/dark use 10-DIP corners;
every Office base palette retains its previous zero-radius connected message
geometry, inherited by its dark variant. Margins, border thickness, paint,
shadow and host choices are unchanged. The token uses `DynamicResource`; no
public C# API or shipped API baseline changed.

The RibbonKit-only consumer first failed with expected `10,10,10,10` and actual
`0,0,10,10`, then passed with the correction. Its single STA/Application matrix
covers placements, RTL, mixed proxies, minimized/message combinations, live
light/dark and Office switches, overflow, manual light/dark dictionaries,
combined-state resource replacement and explicit local corner precedence.

The Release solution build passed with zero warnings/errors. The focused run
passed 100 tests and the eligible runtime run passed 417, excluding Writer-named
tests and `Every_ribbon_button_family_consumes_the_shared_hover_glass`.
The RibbonKit-only consumer test passed, including the added dynamic-resource
check after rebuilding that project. The initial snapshot comparison passed all
77 scenes because the small corner changes fit its existing tolerance. A full
capture identified exactly two changed PNGs. Actual and amplified diff images
for both minimized-message palettes showed only the upper corners and their
immediate shadow: 317 changed pixels in light and 315 in dark. Only those two
approvals were renewed after inspection; the other 75 PNGs were identical and
no tolerances changed. The final comparison passed all 77 scenes after rebuilding
the visual project. Theme XML parsed without duplicate direct resource keys.
Final diff review and `git diff --check` passed.

Follow-up user screenshots of the corrected minimized/message state in both
Crystal dark and light show all four rounded QAT corners, with the message row
separate below it and the ribbon body hidden. This closes the visual corner
check at the supplied 1350×850 window capture size. The evidence files are
`codex-clipboard-d5160476-f97d-420f-b4e2-67128d52155e.png` (dark) and
`codex-clipboard-4be6f351-cb8f-45db-a678-3348695c7735.png` (light), supplied from
the user's temporary clipboard folder. The status bar's document zoom of 100%
does not establish Windows display scaling.

The next user screenshots show the QAT Paste split-button popup open directly
below its source-linked proxy while the ribbon is minimized above Protected
View. Both palettes have readable rows, rounded popup corners and no visible
clipping. Evidence is `codex-clipboard-f8abaea5-35fb-4459-be9c-6f3f339094d4.png`
(light) and `codex-clipboard-4c88adba-29a5-4676-8557-7b28a668cefd.png` (dark),
again 1350×850 captures from the temporary clipboard folder. The user explicitly
confirmed Esc closes the menu cleanly in both themes. This establishes the
bounded Paste popup appearance/closing check, not every popup state.

Further user screenshots show the QAT Select dropdown aligned beneath its
proxy in the same minimized/message state. All three rows are readable, and
the popup has rounded corners with no visible clipping in both palettes.
Evidence is `codex-clipboard-d2737cdd-ee6c-4838-b22f-d78717a0ebe4.png` (dark)
and `codex-clipboard-3f0c27b8-cb8b-4d56-8562-508e501024cc.png` (light), again
1350×850 captures. The user explicitly confirmed Esc closes Select cleanly in
both themes and reported the current Windows display scale as 125%. These
Select captures establish the live 125% baseline.

The next 200% Windows-scale check supplies maximized 1920×1104 captures,
`codex-clipboard-05a412ea-4970-43c5-9721-d372aa78f1a2.png` (light) and
`codex-clipboard-ed41bcf0-5858-46db-aed9-182dcd1d57ba.png` (dark). The minimized
QAT and Select popup remain sharply rendered, correctly anchored, rounded and
visibly unclipped above Protected View in both themes. The screenshot check
passes for this state. The user then confirmed Showcase stayed open throughout
125% → 200% → 125%. Select was closed when Alt-Tab switched to Display settings
to restore the scale. After the return, reopening Select produced normal
alignment and Esc closed it cleanly in both themes. This completes the bounded
live Windows scale-change/return check for the minimized/message QAT and Select
popup. Keeping a popup open during a scale change and moving between monitors
remain unverified.

The next 125% placement check moves QAT to TabRow using the above-ribbon menu
choice. The light capture `codex-clipboard-a8997597-90d0-4f94-bdfb-4489c8a65e6f.png`
and dark capture `codex-clipboard-044e3ce8-d801-4d72-8a0b-c79a61be0a07.png`
show all five commands aligned between File and Home, with no below-ribbon
drawer and Protected View directly beneath the minimized tab row. This
placement passes visual review in both themes at the supplied window size.

Title-bar QAT placement then passes the same 125% minimized/message check in
both themes. The dark capture `codex-clipboard-4d0df15b-12d3-4ddc-a704-c70a8c36c8e6.png`
and light capture `codex-clipboard-2be6824b-7832-4e7d-8022-b8aadf970250.png`
show all five commands fitting beside the window icon without clipping, while
the tab-row and below-ribbon QAT hosts are cleared.

The title-bar overflow check adds eight temporary Home commands and moves
Paste/Select after them. The light capture
`codex-clipboard-b563c866-93da-4b18-8bb3-c4b0d940ca9c.png` and dark capture
`codex-clipboard-e6718258-ddce-43a0-b6a3-0657ec55d7b2.png` show the overflow
popup anchored beneath its chevron at 125% with readable, unclipped Underline,
Find, Replace, Paste and Select rows. This parent-popup appearance check passes
in both themes.

Nested Paste then passes visual review with both menus open at 125%. The dark
capture `codex-clipboard-bc5307cb-b02b-41ba-98f9-f2f3b87b9865.png` and light
capture `codex-clipboard-5e09ffce-ef64-4a3c-a98a-03189fb65daa.png` show all
three Paste rows readable in a rounded, unclipped child popup below the split
row. The user confirmed the requested two-step Esc sequence works in both
themes: the first closes Paste while retaining overflow, and the second closes
overflow. Reopening overflow and Paste still displays all three commands,
verifying that menu borrowing did not strand them.

The user then confirmed the requested Select closing/reopening sequence works.
The dark capture `codex-clipboard-a561d05e-7867-4280-a8c7-c54c1df24b73.png`
shows Select All, Select Objects and Selection Pane in a readable, rounded,
unclipped child popup beneath the overflow Select row. Dark Select appearance
passes. The light attachment
`codex-clipboard-1d946669-ef56-4539-b27f-af2a5ce5ff5d.png` shows Paste again,
so it does not establish light nested Select appearance. The corrected light
capture `codex-clipboard-57597350-6ff6-479d-a539-5d3a9886dd04.png` then shows
all three Select rows aligned, readable, rounded and unclipped beneath the
overflow Select row. Protected View is closed in this corrected light capture;
the dark Select capture above retains it. Light Select appearance passes,
completing the bounded nested-menu review alongside the user-confirmed closing
and reopening sequence. The user then confirmed cleanup after instructions to
remove the eight temporary commands, retain Save/Undo/Redo/Paste/Select, return
QAT below the ribbon, expand Home and keep Windows scaling at 125%. Restoration
is user-confirmed; no additional screenshot was requested for cleanup.

This completes the bounded QAT live review at the recorded states and scales.
The subsequent screenshot and cleanup record updates changed documentation
only; no builds or tests were rerun for those updates. Final documentation diff
review and `git diff --check` passed.

Broader DPI coverage, remaining target-machine popup acceptance and slice 5
customization review remain unverified. The agent did not launch Showcase or
run Writer tests, and made no commit. Message-bar/application-menu promotion, later
portability slices and slice 10's main Showcase consolidation remain open;
the separate Crystal preview window and its launch path remain.

### 3.199 Shared Crystal message bars — 2026-09-30

Slice 6's second bounded pass closes the reusable message-bar gaps: the
sample's `CrystalMessagePresentation` substituted `Crystal.Backstage.Action`
for each realized action button, and the preview overrode
`ContentCornerRadiusTop` to keep all four body corners above messages.
The rows' semantic amber paint, 10-DIP rounding, spacing and shadow were already
shared. A RibbonKit-only consumer reproduced the remaining action mismatch:
the default Crystal action had 5-DIP corners instead of the accepted 12 DIPs.

`Controls.MessageBar.xaml` now supplies one action template in the message's
template resource scope. A dynamic `RibbonKit.Styles.MessageBar.ActionButton`
theme style selects the existing brushes and action states without duplicating
templates. Matching `MessageBar.ActionCornerRadius` and
`MessageBar.ActionRecognizesAccessKey` metrics preserve Crystal's 12-DIP action,
hand cursor, access keys, glass hover/pressed/focus states and 0.4 disabled
opacity. Office base tokens retain their 0/2-DIP action corners, existing brushes
and 0.45 disabled opacity; dark palettes inherit those styles and resolve their
own live brushes. Crystal dark explicitly supplies the Crystal style rather
than falling back to its merged Office dark palette. Shared
`ContentCornerRadiusTop` now keeps all four Crystal body corners at 14 DIPs.
The separate QAT seam metrics and the previously reviewed minimized/message
corner correction are preserved. No public C# API or shipped baseline changed.

After the replacement passed the existing single STA/Application consumer,
`CrystalMessagePresentation`, its main/preview wiring and the scoped preview
body-corner override were removed. Message content, commands, action-driven
closure and comparison policy stay with the host. Action styles/properties and
message background local values retain WPF precedence; clearing them restores
the theme. The style resolves its template in the message scope, so it is a
message-part extension point rather than a general-purpose application button style.

The consumer exercises ItemsSource, every QAT placement, minimized states,
two independent notices, empty-bar margin/effect cleanup, reopen and wrapping,
physical RTL positions, UI Automation action invocation and command parameter,
dismissal events, command-disabled opacity and synthetic hover/pressed/focus
states. It checks light/dark and every Office palette on realized parts,
manual light/dark dictionary merges, scoped metric replacement and explicit
action style/background and message background choices. Semantic notice paint
does not rotate with a custom accent. Style changes can recreate action chrome;
the regression reacquires the live template part for Office/manual switches
rather than asserting on a detached border.

Validation on this pass: Release solution build and affected visual-test rebuild,
both zero warnings/errors; 100 focused runtime tests; 417 eligible runtime tests;
the single RibbonKit-only consumer test; full capture and final comparison of
85 snapshot scenes. Both runtime filters exclude Writer names and the deferred
`Every_ribbon_button_family_consumes_the_shared_hover_glass` method. The existing
77 captures were byte-identical to their approvals. Eight new light/dark message
images were inspected before adding approvals: stacked/paragraph-wrapped notices
and rounded actions at synthetic 100/200%, RTL stacks and minimized stacks.
The minimized fixture shares the QAT fixture's loaded lifecycle/body-collapse
assertion. No existing approval or tolerance changed in this pass.
All 12 palette dictionaries and the shared message template parsed successfully;
there were no duplicate direct token keys. Final diff review and
`git diff --check` passed. The subsequent documentation edits required no
additional build/test run.

Fresh user message screenshots, live animation, target-machine keyboard/pointer
and DPI acceptance remain pending. The earlier QAT review is preserved and does
not establish those gates. No manual Showcase launch, Writer tests, commit, push
or publication ran. Application-menu promotion still remains within slice 6;
utility chrome, contextual tint, optional effects and slice 10's final Showcase
consolidation remain later work. The separate preview window and launch path
were retained. Office Glass review, View-tab overflow, 200% gallery clipping,
the Office 2010 hover investigation and Writer RKWF-026 remain deferred.

First live message checkpoint: the user supplied light
`codex-clipboard-cd14ae68-3b49-43ac-add3-1ccc637e79f6.png` and dark
`codex-clipboard-2607cb43-182b-45df-8af4-521bb4f63551.png` after the requested
125% setup. Both 1350×850 captures show expanded Home, TabRow QAT and the
Protected View/Security Notice rows. Inspection confirms the body retains its
lower rounded corners, each notice has its own rounded frame and gap, and action
text/close targets are readable without visible clipping. Live action/dismissal,
reopen and animation checks remain pending; these static images do not establish
new DPI or keyboard/pointer acceptance. The worktree was clean at `492fa38`
when this review began. This checkpoint changed records only; diff review and
`git diff --check` passed, with no build/test rerun or manual Showcase launch.

The user then confirmed the expanded interaction sequence in both themes:
Enable Editing closes only Protected View, Add Message restores it, Review
Settings closes only Security Notice, the remaining close target dismisses the
last row, and two Add Message clicks restore both notices. The confirmed sequence
includes independent row dismissal and reopening. Minimized live review and
broader motion/keyboard/DPI checks remain pending. This confirmation updates
documentation only; no build/test rerun or manual Showcase launch was needed.

Minimized live appearance checkpoint: the user supplied dark
`codex-clipboard-d1087026-3b7d-4ec3-b1b9-7dfd3f4cff4f.png` and light
`codex-clipboard-808c81f2-e5ac-4676-8744-2f9e2dbe9393.png` for the requested
125% setup with below-ribbon QAT and both notices visible. Both 1350×850 images
show the collapsed ribbon body, all four rounded QAT corners, separate notice
frames with clear gaps, and readable action/close targets without visible
clipping. These images establish this static minimized layout; minimized
dismiss/reopen/restore interactions and broader motion/keyboard/DPI checks remain
pending. This checkpoint changes documentation only; no build/test rerun or
manual Showcase launch was needed. Slice 6's application-menu pass remains open.

The user then confirmed the minimized interaction sequence in both light and
dark themes: Enable Editing and Review Settings each dismiss only their own
notice while minimized; expanding the ribbon, adding both messages and minimizing
again restores both notices without leftover shadows. Expanded Home restoration
was also included in the requested sequence. This completes the bounded
message-bar live review at the requested 125% setup. Broader motion/keyboard/DPI
acceptance remains pending; this confirmation does not establish new 200% or
mixed-monitor message-bar coverage. Documentation diff review and
`git diff --check` passed; no builds/tests were rerun and Showcase was not launched
by the agent. Application-menu promotion remains open within slice 6, and the
separate Crystal window remains until slice 10's verified consolidation.

### 3.200 Shared Crystal application menu — 2026-09-30

Slice 6's third bounded pass moves the reusable application-menu appearance out
of Showcase. Work resumed from the clean `codex/crystal-theme` checkout at
`e3b7835` after the user's conflict-resolution commit. The earlier bounded QAT
and message-bar live evidence remains valid for those surfaces; it does not
establish acceptance of this new menu implementation.

The RibbonKit-only consumer first reproduced the old 8-DIP frame instead of
Crystal's accepted 14-DIP geometry. Shared `Controls.ApplicationMenu.xaml` and
Crystal light/dark tokens now supply the translucent frame, pane, headings and
footer paint; outer/inner corners; inset rows/separators; and the combined split
row silhouette. A binding-driven internal converter clips the inner content to
the outline's inset corners and keeps split hover/selection surfaces inside the
row's rounded boundary. Layout and token changes invalidate those bindings;
no Showcase part-paint or geometry patches are needed.

Crystal navigation remains 160 DIPs wide. The preferred pane width is 300 DIPs,
with a 180-DIP minimum and a 32-DIP viewport allowance. The converter subtracts
the navigation width and allowance from the owning Ribbon's actual width, then
clamps to the token bounds. A menu created in `Window.Resources` and templated
before assignment exposed an ancestor-binding gap: its inheritance context
could bypass Ribbon during lookup even after visual attachment. Ribbon now
binds its width to an internal attached value on the menu, clearing the old
binding when ownership changes. The consumer verifies this preparation order,
narrow-window resizing, scoped inset replacement and reassignment to another
Ribbon. The bounds are private template values rather than `MinWidth`/`MaxWidth`,
so explicit pane widths below or above the theme range retain normal local-value
precedence.

The shared `MenuSurface` contains a separate noninteractive shadow caster.
Its clip excludes the frame silhouette, leaving the translucent interior
unshaded. The halo follows shadow blur/depth and corner changes, including
nonuniform corners. Crystal uses its accepted 16-DIP, zero-depth light/dark
shadow; Office collapses this layer and retains its existing whole-frame effect.
All new keys have matching Office defaults. Existing Office/menu snapshots are
unchanged. Dynamic resources support scoped paint, corner, spacing, outline,
pane and shadow replacements; explicit frame paint/corners/effect, pane width
and row margins survive theme switches, with `ClearValue` resuming the theme.
The shadow test replaces a resource because WPF can freeze effects after use;
local width rendering assertions account for physical-pixel layout rounding.

No public C# API or shipped API baseline changed. `CrystalApplicationMenuPresentation`
and `CrystalMenuShadow` were removed after the shared replacement passed the
consumer. Main Showcase and Crystal preview callers now use
`CrystalApplicationMenuBackdrop`, which only attaches the existing host capture
layer to the shared surface. The accepted 6-DIP blur, 24-DIP capture padding and
opacity-based capture remain unchanged. Captured blur and its optional reusable
integration contract remain slice 9 work. Menu content/commands, File-surface
choice, preview tint/comparison controls and platform integration remain with
the host. Preview tint continues to replace the shared menu heading token for
readability; full tint promotion remains slice 8. The separate preview window
and launch path are retained for slice 10's later verified consolidation.

Validation: the Release solution build and affected VisualTests rebuild passed
with zero warnings/errors. The focused runtime run passed 116 tests and the
eligible runtime run passed 417, excluding Writer-named tests and
`Every_ribbon_button_family_consumes_the_shared_hover_glass`. The single
STA/WPF Application consumer passed without Showcase resources/helpers. Its
menu matrix covers all QAT placements, minimized states, message presence,
LTR/RTL physical order, repeated light/dark and every Office palette switch,
manual Crystal token merges, resource/local precedence, pre-templated menu
ownership and command routing. Template button handlers select split/dropdown
panes; automation invocation verifies pane/footer commands, closing and default
pane restoration on reopening. A rendered pixel check verifies shadows at all
four outside edges and a transparent center. These synthetic invocations do
not establish native keyboard/pointer acceptance.

The full capture and comparison each passed all 99 snapshot scenes. The old
85 actual images were byte-identical to their approvals. All 14 new light/dark
menu actual PNGs were inspected before adding approvals: default, split,
dropdown, narrow, minimized, RTL and synthetic 100/200% scenes. Existing
approvals and tolerances were unchanged. Shared menu XML and all 12 palette
dictionaries parsed without duplicate direct keys. Final diff review and
`git diff --check` passed; later prose updates require no WPF test rerun.

Fresh live menu screenshots, native interaction, animation and real menu DPI
acceptance remain pending. Slice 6 is not marked complete. Slice 5's fresh
customization review, broader popup/DPI checks, utility chrome, contextual tint,
optional effects and slice 10 remain open. Office Glass review, View-tab
overflow, 200% gallery clipping, the Office 2010 hover-contract investigation
and Writer RKWF-026 remain deferred. No Writer tests, manual Showcase launch,
commit, push or publication ran.

First live menu appearance checkpoint: the user supplied dark
`codex-clipboard-72958fe5-c3ca-4cad-aebf-f4e3b68122ee.png` and light
`codex-clipboard-25fdb015-40e9-4616-8310-7143366d3a23.png` for the requested
125% setup. Both 1350×850 captures show expanded Home with title-bar QAT,
no message rows and File open on Recent Documents. Inspection confirms the
rounded outer frame, inner outline and footer, readable commands/headings,
unclipped footer actions and an outside shadow. The host's captured backdrop
is visible behind the command column while menu text stays sharp. This is
static default-page evidence; active split/dropdown panes, closing/reopening,
narrow-window resizing, live RTL, animation and real menu DPI checks remain
pending. The generic Manage recent locations button remains host content.
Slice 6 is not marked complete. This update changes documentation only;
content/diff review and `git diff --check` passed. No builds/tests were rerun,
no manual Showcase launch occurred, and no commit, push or publication ran.

Save As live checkpoint: the user supplied light
`codex-clipboard-b3df1b4e-2abd-4312-9f34-e854b1071da9.png` and dark
`codex-clipboard-da279045-ef9d-45cc-aa70-789f780f700b.png`. Both 1350×850
captures show the requested Save As split pane with expanded Home, title-bar
QAT and no messages. The rounded split-row silhouette and internal divider are
intact; the command half and active arrow remain distinct. The pane header and
all four commands (Word Document, Word Template, Word 97-2003 Document and Other
Formats) are readable, with descriptions wrapping inside the rounded outline
without visible clipping. The user also confirmed that Esc closes File cleanly
and reopening restores Recent Documents in both light and dark themes. This
establishes the split-pane appearance and bounded close/reopen sequence;
dropdown presentation, native command invocation, narrow resizing, live RTL,
animation and real menu DPI checks remain pending. Slice 6 remains open.
Documentation content/diff review and `git diff --check` passed; no build/test
rerun, manual Showcase launch, commit, push or publication occurred.

Publish dropdown appearance checkpoint: the user supplied dark
`codex-clipboard-e0fd4852-6ee4-425a-b8b4-c0fb5196fb0f.png` and light
`codex-clipboard-c3745689-ae40-4ca3-b6cc-481e6f8a7a40.png` for the requested
125% setup. Both 1350×850 captures show the complete Publish row highlighted
while the right pane is active. The row retains its rounded silhouette without
a split divider or separately dimmed command half. The heading, Blog and
Document Management Server entries are readable; their descriptions wrap
inside the rounded pane without visible clipping. The frame, footer and host
backdrop remain consistent with the default and Save As captures. This records
dropdown appearance; native command invocation, narrow-window resizing,
live RTL, animation and real menu DPI checks remain pending. Slice 6 remains
open. Documentation content/diff review and `git diff --check` passed; no
builds/tests were rerun, Showcase was not launched by the agent, and no commit,
push or publication occurred.

The user then confirmed the native command sequence in both themes: clicking
the Save As command half, reopening and choosing Save As arrow → Word Document,
and reopening and choosing Publish → Blog each closes File and displays the
selected command in Showcase's status line. Reopening after every command
returns to Recent Documents. These Showcase commands use the existing status
handler; no runtime behavior was changed for this check. This establishes
bounded split-primary and pane command invocation and default-page restoration.
Narrow-window resizing, live RTL, animation and real menu DPI checks remain
pending; slice 6 remains open. Documentation content/diff review and
`git diff --check` passed; no builds/tests were rerun, Showcase was not launched
by the agent, and no commit, push or publication occurred.

Narrow default-page checkpoint: the user replaced an accidentally submitted
wide pair with light `codex-clipboard-a7b5b5df-d38e-4798-a716-1a6343aa8993.png`
and dark `codex-clipboard-eaaefaf8-c314-4a8e-9663-8164eb40c6d2.png`. Both PNGs
measure 518×850 pixels, suitable for the requested narrow-window check at the
125% setup. They show File on Recent Documents with title-bar QAT and no
messages. The pane is narrower than in the previous wide captures; the rounded
frame stays within the window, the default entries fit and both footer actions
remain visible without clipping. The user reported that the menu widened to
normal size after enlarging the window again. The earlier wide pair was
explicitly retracted and is not narrow-layout evidence. This records narrow
default-page appearance and reported width restoration; the screenshots do not
show Save As description wrapping. Narrow active-pane review, live RTL,
animation and real menu DPI acceptance remain pending, and slice 6 stays open.
Documentation content/diff review and `git diff --check` passed. No builds/tests
were rerun, Showcase was not launched by the agent, and no commit, push or
publication occurred.

Narrow Save As checkpoint: the user supplied dark
`codex-clipboard-89ef3dac-eb99-4bd6-a677-572cbafe6495.png` and light
`codex-clipboard-bb4f54ad-1f82-480f-ad24-c629026db711.png`. Both PNGs measure
500×850 pixels and show the Save As pane with title-bar QAT and no messages.
All four descriptions wrap inside the reduced pane without visible clipping,
including the longer Word Template and Word 97-2003 Document text. The heading,
rounded split row and divider, outer frame, inner outline and both footer
actions remain intact. Together with the previous default-page pair and
reported width restoration, this completes the bounded narrow default/Save As
appearance check. Live minimized/message menu review, RTL, animation and real
menu DPI acceptance remain pending; slice 6 stays open. Documentation
content/diff review and `git diff --check` passed. No builds/tests were rerun,
Showcase was not launched by the agent, and no commit, push or publication
occurred.

Minimized-ribbon/stacked-notice menu checkpoint: the user supplied light
`codex-clipboard-1cc565b6-e04d-4124-8aa1-79c7bf580b0a.png` and dark
`codex-clipboard-0aeb1139-b55c-4f30-975f-a764e94030aa.png`. Both PNGs measure
1350×850 pixels and show the Save As pane with the ribbon minimized, title-bar
QAT and both Protected View and Security Notice visible. The menu overlays
the notice rows and document; its rounded frame, inner outline, active split
row and divider, all four command descriptions and both footer actions remain
intact without visible clipping. The user confirmed that Esc closes File cleanly
and reopening restores Recent Documents in both themes in this combined state.
This completes its bounded appearance/close/reopen check. Live RTL, animation,
broader keyboard and real menu DPI acceptance remain pending; slice 6 stays
open. Documentation content/diff review and `git diff --check` passed. No
builds/tests were rerun, Showcase was not launched by the agent, and no commit,
push or publication occurred.

### 3.201 RTL lab bilingual application-menu header — 2026-09-30

The user supplied dark `codex-clipboard-4ccad49a-481d-4f3b-99dc-32c6f52f863a.png`
and light `codex-clipboard-9fb37f6e-a148-4e63-8f64-f74b1903d577.png`. Both PNGs
measure 1300×875 pixels and show the Localization/RTL lab's default menu with
Right-to-Left enabled and the embedded/application string provider. The menu
anchors at File on the right; navigation/icons remain on the right, recent
content is on the left and footer actions mirror leftward. Arabic/English
recent content is readable, and the explicitly LTR `Report-2026-Q3.docx` row
keeps its direction. The rounded frame, inner outline and footer remain intact.

The Save As row reveals a real label gap: `حفظ باسم — Save As` exceeds the
text space alongside the icon and split arrow. A focused realized-control
reproduction measured characters outside the primary hit area. This is long
host-owned bilingual content, so the lab now supplies a wrapping
`RibbonApplicationMenuItem.HeaderTemplate` for that one row. Shared navigation
width, menu geometry, colors, behavior and public APIs are unchanged by this
correction; another consumer can use the same existing extension point.

The regression measures every insertion position's character rectangle inside
the primary hit area in Crystal light/dark and LTR/RTL, including vertical fit.
It failed before wrapping and passes afterward. Release compilation of Showcase,
its runtime dependencies and the affected test project passed; both focused
`CrystalLocalizationIntegrationTests` passed. Separate `bin/RtlLabHeaderCheck/`
output preserves the user's running build. No Writer tests, full-suite rerun,
snapshot approval changes or manual Showcase launch occurred. Fresh screenshots
from the updated build are required for RTL label/pane acceptance; closing,
reopening, motion, broader keyboard and real menu DPI checks remain pending.
Slice 6 stays open. Final content/diff review and `git diff --check` passed;
no commit, push or publication occurred.

Fresh RTL Save As appearance checkpoint: the user supplied light
`codex-clipboard-fc5469ce-01ac-4d4b-80a5-e14a085c97d8.png` and dark
`codex-clipboard-0c465c35-51d2-4f44-84ae-7316f1d210d1.png` from the updated
build. Both PNGs measure 1300×875 pixels and show the Save As pane in the RTL
lab with the embedded/application provider. The full bilingual navigation
label is visible on two lines without clipping. Its left-pointing split arrow
and internal divider stay within the rounded active row. Both mixed-language
pane entries are readable on the left, and the frame, inner outline and
mirrored Exit/Options footer remain intact. This verifies the wrapping correction
and bounded RTL active-pane appearance alongside the previous default-page
review. RTL closing/reopening, return to LTR and broader motion/keyboard/real
menu DPI checks remain pending; slice 6 stays open. Documentation content/diff
review and `git diff --check` passed. No builds/tests were rerun, Showcase was
not launched by the agent, and no commit, push or publication occurred.

The user then confirmed that Esc closes File cleanly and reopening restores
Recent Documents in both themes in RTL. After closing File and turning
Right-to-Left off, reopening also returns the menu to normal LTR layout in
both themes. Together with the default/Save As screenshots, this completes
the bounded RTL appearance/close/reset review. Broader motion/keyboard and real
menu DPI acceptance remain pending; slice 6 stays open. This documentation-only
update passed content/diff review and `git diff --check`; no builds/tests were
rerun, Showcase was not launched by the agent, and no commit, push or publication
occurred.

### 3.202 Application-menu footer at limited viewport height — 2026-09-30

The user confirmed alignment and Esc closing in both themes after returning to
125%. The accompanying light `codex-clipboard-98fef5b3-93b6-42c9-bc83-47f05334b2ae.png`
and dark `codex-clipboard-7b26680c-b1f8-43e6-9cb4-08cd73232f35.png` measure
1920×1104 pixels and show Save As at 200%. Both Options/Exit footer buttons are
cut off at the window bottom. The return confirmation remains valid; these
captures do not establish a passing 200% footer or full menu DPI gate.

The ribbon's Canvas measures its overlay with infinite height. Ribbon now
publishes an internal available-height value from the menu anchor to the bottom
of the window's client-content presenter, accounting for menu/presenter margins.
The presenter matters when the content itself aligns to the top, including a
window containing only Ribbon. `PART_Frame.MaxHeight` follows that value;
navigation, default content and active-pane content use native ScrollViewers,
while the pane header and footer stay outside their scrolling areas. Layout
reflow refreshes the bound, and detaching/unloading the ribbon or replacing its
menu releases the previous window's constraint. Explicit local part limits keep
WPF precedence. There is no public API or new palette key; Office menus retain
their normal geometry when enough space is available.

A RibbonKit-only reproduction failed before the fix: in a 320-DIP-tall window,
the footer extended to 637 DIPs inside a 286-DIP client root. The regression
covers Crystal, Office 2007 and Office 2024, light/dark, LTR/RTL, short/tall/short
resizing with reopening, scroll offsets in all three content areas, anchor
reflow while open, local MaxHeight precedence and detach/reattach cleanup.
Native window resizing already dismisses File; the test preserves that behavior.

The broader runtime check also exposed captured-background measurement feedback:
when height was constrained, an old Image bitmap could enlarge the preview menu
to 751.2 DIPs in a 420-DIP-wide window. The host's `CrystalMenuBackdrop` now puts
its explicitly sized Image in a Canvas, keeping captured paint out of desired
size. The existing resize regression and blur pixel assertions pass, including
foreground exclusion, rounded clipping, scroll refresh and capture cleanup.
Capture remains host-owned slice 9 work; this does not add an integration API.

Final verification: the Release solution build passed with zero warnings/errors
for both runtime frameworks. All 418 eligible runtime tests passed, excluding
Writer names and `Every_ribbon_button_family_consumes_the_shared_hover_glass`.
The RibbonKit-only consumer passed its single STA/Application test, and the
visual test passed all 99 scenes without approval changes. Snapshot diagnostics
were inspected before correcting a stale height constraint after detachment;
the approved minimized appearance is preserved. Separate
`bin/MenuViewportCheck/Release/<framework>/` output keeps the user's running
build untouched. Documentation content/link review and `git diff --check` passed.

Fresh 200% light/dark screenshots must verify fully visible footer actions and
usable scrolling, followed by alignment/Esc checks after returning to 125%.
Broader motion/keyboard/DPI acceptance remains open; slice 6 is not complete.
No Writer code/tests, approval changes, manual Showcase launch, commit, push or
publication occurred.

Fresh footer appearance/return checkpoint: the user supplied light
`codex-clipboard-67890785-90da-40ee-98e0-4d4ff96ac9e4.png` and dark
`codex-clipboard-eb4af141-5af6-4a64-9a61-9833a1809dfa.png` from the updated
build. Both PNGs measure 1920×1104 pixels and show Save As at 200%. The frame
ends within the window; Options and Exit are fully visible in the rounded
footer. The navigation scrollbar is present; the pane heading and all four
commands/descriptions remain readable. The user confirmed alignment and Esc
closing in both themes after the requested return to 125%. This completes the
bounded footer appearance/return check. The screenshots do not establish live
scroll reachability; that check and broader motion/keyboard/DPI acceptance remain
pending, so slice 6 stays open. This documentation-only checkpoint passed
content/link review and `git diff --check`; no builds/tests were rerun, Showcase
was not launched by the agent, and no commit, push or publication occurred.

### 3.203 Application-menu scrollbar clicks preserve the open menu — 2026-09-30

During the live 200% scrolling check, the user reported that clicking a scroll
button closes File, while using the thumb does not. `RibbonApplicationMenu` listens
to unhandled `ButtonBase.Click` and requests dismissal. Native scrollbar arrows
and page regions are RepeatButtons and reach that handler; a Thumb uses drag
events. The new scrolling areas exposed this pre-existing command-click rule.

The shared handler now walks only the click source's ancestor path and returns
for a ScrollBar descendant. It neither marks the event handled nor suppresses
the routed scroll command. Split arrows/dropdown pane openers retain their
existing handled-click behavior, and ordinary commands inside ScrollViewers,
pane/footer commands and Esc retain dismissal. No public API, template, token
or host helper was added or changed for this correction.

The RibbonKit-only viewport regression now invokes the realized RepeatButton's
protected native `OnClick`, exercising both the bubbling Click and its routed
scroll command. It failed before the fix with `NavigationScroll: LineDown closed
File`. Afterward line-down, page-down and line-up change the offset while File
stays open in navigation, default and active panes, with active-pane ownership
preserved. Crystal, Office 2007 and Office 2024 run in light/dark and LTR/RTL.
The earlier direct `ScrollToBottom` check did not exercise button dismissal.

Offscreen UI Automation invocation depends on native activation/command requery
and was unavailable for these realized buttons in this STA fixture, so the test
uses reflection to call the native protected click method. This verifies routing
and command behavior, not physical pointer acceptance. Windows at the user's
current 200% setup also clamps a requested 950-DIP window to 552 DIPs; the resize
regression now requires the reopened menu to reclaim actual available space and
reduce its scroll range instead of assuming all content must fit without scrolling.

The Release solution build passed with zero warnings/errors for both runtime
frameworks, and the single RibbonKit-only consumer test passed. The eligible
runtime run passed 417 tests and failed
`Detached_rtl_lab_applies_crystal_and_restores_office_options` at its initial
Office RTL option-indicator assertion, before applying Crystal or opening File.
Separate-process reruns failed identically in both the new output and the prior
`MenuViewportCheck` output at the current 200% display setup. This existing
failure remains recorded; the current broader run is not a passing gate. Writer
names and the deferred hover-glass method remain excluded. Visual snapshots
were not rerun for this routing-only correction; the preceding 99-scene result
remains dated evidence. Documentation content/link review and `git diff --check`
passed. The updated
Showcase is in separate `bin/MenuScrollClickCheck/Release/net8.0-windows/` output.
Fresh arrow/page/drag/wheel scrolling at 200% in light/dark remains for the user;
prior footer appearance/125% return acceptance remains recorded. Broader
motion/keyboard/DPI checks remain pending, and slice 6 stays open. No Writer
code/tests, manual Showcase launch, commit, push or publication occurred.

The user subsequently confirmed that scrollbar buttons now scroll without
closing File in both themes. This closes the bounded live arrow-click regression
check. It does not add page-region, thumb-drag or wheel acceptance, and broader
keyboard/motion/DPI checks and the separate Office RTL test failure remain open.
This follow-up changed documentation only; content/link review and
`git diff --check` passed, with no builds/tests rerun or Showcase launch.

The user also reported different gallery-item layout at 200% and requested that
it and the existing RTL option-indicator failure be deferred for later
investigation. The gallery report's exact surface/geometry and both causes remain
unconfirmed; it is not assumed to be the earlier View-tab selection-clipping
issue. The [deferred layout list](../13-CRYSTAL-PORTABILITY-AND-ORB-PLAN.md#deferred-layout-observations-at-200)
records the observations and later comparison scope. This note changes
documentation only and does not change runtime behavior, test results or the
accepted File-menu checks.

The user then supplied `20260930-1141-49.7011675.mp4` and confirmed that keyboard
Tab navigation works and Esc closes File after tabbing. This is user-reported
acceptance of that sequence; separate Shift+Tab, footer reachability,
default-page restoration and recording theme/scale were not stated. Opening
motion in light/dark is the next bounded live review. Remaining scrolling,
keyboard/DPI gates and the deferred 200% layout observations retain their
scope. This documentation-only update passed content/link review and
`git diff --check`; no builds/tests were rerun or Showcase launched.

The user subsequently confirmed the requested repeated File open/Esc/quick-
reopen motion check passed in both themes. This closes the bounded normal
opening-motion review. The [remaining live review](../13-CRYSTAL-PORTABILITY-AND-ORB-PLAN.md#remaining-slice-6-live-review)
lists the other scrolling modes and remaining keyboard details, separately
from reduced-motion/open-menu-scaling/mixed-monitor gaps. The two 200% layout
observations remain deferred. This documentation-only checkpoint passed
content/link review and `git diff --check`; no builds/tests were rerun or
Showcase launched.

The user confirmed both remaining short checks passed: thumb/wheel/track
scrolling keeps File open with its footer visible, and reverse keyboard
traversal retains visible focus, reaches Options/Exit and cycles inside File.
This completes the bounded slice 6 QAT/message-bar/application-menu live review.
Slice 7 utility chrome and scrollbar promotion is next. Reduced motion,
open-menu scaling/mixed-monitor transitions and the two deferred 200% layout
observations remain open; no blanket final acceptance or passing broader
runtime gate is inferred. This documentation-only checkpoint passed content/
link review and `git diff --check`; no builds/tests were rerun or Showcase
launched by the agent.

### 3.204 Shared Crystal utility buttons and scrollbars — 2026-09-30

Slice 7 starts from a clean `codex/crystal-theme` checkout at `07bc890`. Shared
utility templates now own the accepted non-interactive rims for minimize, modal
close, QAT overflow, ribbon/tab scroll arrows and merged-caption buttons. The
rim is a sibling of Chrome and binds its bounds/corners without changing content
padding. Hover/checked rims use the control hover border; pressed paint wins.
Crystal's idle/hover arrow surfaces remain swapped as in the former helper;
Office keeps its original surfaces and zero active rim opacity. Default tab
arrow paint is now a shared style setter so explicit local button backgrounds
retain WPF precedence. Scoped source brushes update directly through a small
internal `UtilityChrome` material selector; no public API was added.

Crystal tokens opt native ScrollViewer scrollbars into the existing shared
vertical/horizontal templates, including Options and both customization pages.
Thickness is 14 DIPs, corners are 4 DIPs, and the thumb retains its one-DIP rim
and face plus 6/8/11-percent wash (15/20/28 alpha steps). Native tracks, routed
line/page commands, thumb dragging, focus and RTL remain in WPF. The designer-safe
keyed customization scrollbar adapter is retained. An attempted cross-file
BasedOn simplification failed the existing scope test and was reverted.

The consumer regression exposed a Freezable resource-scope trap: nested brushes
in a shared resource drawing could retain the first window's surface/wash, even
with a dialog-local token override; unshared XAML drawings also did not provide
reliable scope resolution. `ScrollBarMaterialConverter` composes only marked
Crystal tokens from brushes resolved on the realized Pill. Office and explicit
host thumb backgrounds pass through unchanged. Live resource updates, manually
merged light/dark dictionaries, a simultaneous Options window with a different
wash, local styles/paint/corners, both axes and native line/page routing pass in
the RibbonKit-only consumer. Its built-in customization list/tree and app-owned
Options content bars use the same template/material without Showcase references.

After that proof, `CrystalUtilityChrome`, `CrystalScrollBars` and
`CrystalCustomization` and their callers were removed. Generic paint and geometry
are now library-owned. The existing main-window raw tint and dialog/preview
readable-accent wash remain host policy through `ScrollBar.WashAccent`;
`CrystalPalette` avoids tinting the composite thumb twice and retains neutral
glyph paint. Contextual/tint policy remains slice 8, capture remains slice 9,
and the separate preview window/launch path remains until slice 10.

Final verification: the Release solution build passed with zero warnings/errors
for both runtime targets, all 418 eligible runtime tests passed using
`FullyQualifiedName!~Writer&FullyQualifiedName!~Every_ribbon_button_family_consumes_the_shared_hover_glass`,
and the single STA/Application RibbonKit-only consumer passed. All 105 visual
scenes passed; the existing 99 approvals/tolerances are unchanged. Six new
light/dark utility/scrollbar scenes at synthetic 100/200% and RTL were inspected
before adding approvals. Image inspection first caught scrollbar samples hidden
behind a modal body and intersecting standalone arrow corners; those fixture
errors were corrected before approval. Synthetic snapshots are not live pointer,
focus, monitor-transition or target-machine acceptance.

The initial solution build incorrectly used one OutputPath for both runtime
frameworks and collided net8/net9 assemblies. The corrected isolated output is
`bin/CrystalSlice7/Release/<framework>/` per project. The user launch build is
`samples/RibbonKit.Showcase/bin/CrystalSlice7/Release/net8.0-windows/RibbonKit.Showcase.exe`;
the existing MenuScrollClickCheck output remains untouched. Documentation/link
review and `git diff --check` passed. No Writer code/tests, public API baseline,
manual Showcase launch, commit, push or publication changed.

User light/dark review remains pending for utility hover/press/checked paint,
scrollbar arrows/track/thumb/wheel, Options/customization focus and RTL, merged
caption commands, and real DPI return. Reduced motion, open-menu scaling and
mixed-monitor transitions remain broader gaps. The earlier Office RTL checkbox
failure at the user's 200% setup passed in this automated run, but no fix or cause
was established; it and the distinct 200% gallery-layout report remain deferred.
The accepted slice 6 menu sizing, footer, capture-measurement and click-routing
contracts and bounded live reviews remain in force.

### 3.205 Customization scrollbar insets and slice 7 live review — 2026-09-30

The user accepted slice 7 review checks 1 and 2: utility/arrows and modal/merged
caption buttons. Check 3 paused at a screenshot-reported spacing mismatch.
Crystal's tree scrollbar had the desired small inset while list scrollbars were
flush; Office trees had excessive clearance relative to lists.

The shared customization frame previously combined the FrameInset token with
an extra literal TreeView Padding of 2. A style default replaces it with zero,
also neutralizing WPF's native 1-DIP TreeView padding. List padding is unchanged.
FrameInset supplies 2 DIPs in Crystal light/dark and the existing 1 DIP in every
Office variant. Crystal tree
clearance and Office list clearance are retained; the other controls match them.
The existing template binding still adds local or host-style padding, and scoped
FrameInset overrides remain dynamic. No runtime or public API was added.

The RibbonKit-only consumer now measures realized viewport and visible native
scrollbar bounds for both page types, all Office variants, Crystal light/dark,
manually scoped palettes and both directions. It also checks local padding plus
a scoped inset, host-style padding and restoration. Native scrollbar size
rounding at fractional DPI is allowed within half a device pixel; viewport
clearance stays exact. Final review removed redundant literal list padding and
neutralized native tree padding at style precedence, retaining host overrides.

The isolated Release solution build passed with zero warnings/errors, the
RibbonKit-only consumer passed, all 418 eligible runtime tests passed with the
same Writer/hover-method exclusions, and all 105 visual scenes passed against
unchanged approvals/tolerances. The solution and consumer checks use
`bin/CrystalSlice7Spacing/Release/<framework>/`; the new user executable is
`samples/RibbonKit.Showcase/bin/CrystalSlice7Spacing/Release/net8.0-windows/RibbonKit.Showcase.exe`.
The prior CrystalSlice7 and MenuScrollClickCheck outputs are preserved. The
initial geometry assertion needed corrections for native device-pixel rounding
and RTL ancestor coordinates; neither required another product change.
One consumer run stopped before customization at `NavigationScroll: LineDown
closed File`. A fresh-process rerun passed, including the final geometry and
host-style checks. Menu routing was unchanged; that one-off failure's cause is
unconfirmed and no menu fix is claimed.
Documentation links and `git diff --check` passed. No Writer work, Showcase
launch, commit, push or publication occurred.

Check 3 live scrolling, focus, RTL and real DPI return remains pending. The
separate Office RTL checkbox failure and 200% gallery-layout observation remain
deferred with unconfirmed causes. Checks 1–2 need not be repeated for this fix.

### 3.206 Customization scrollbar pixel gaps — 2026-10-01

The user's follow-up screenshot found unequal top/bottom scrollbar gaps in every
theme and excessive Crystal list-side clearance relative to the tree. The earlier
half-device-pixel geometry allowance had not established equal painted gaps.
The user accepted the latest scrollbar arrow/track/wheel/thumb, focus and RTL
check (follow-up number 2); spacing and 125% → 200% → 125% return will be repeated.
Original slice 7 utility/modal checks 1–2 remain accepted.

An offscreen RibbonKit-only probe reproduced fractional frame bounds, independently
rounded scrollbar chrome and column-dependent painted side gaps. Rounding the
page alone still left an odd inset pixel: WPF Border.Padding can subtract a
rounded combined width/height differently from an individual edge. The shared
frame now uses the existing FrameInset token as a transparent BorderThickness,
which rounds each edge independently. Both customization page styles default
UseLayoutRounding to true, while local values and host styles keep WPF precedence.
At 125%, Crystal's outer-edge gap is three pixels including its frame border;
Office's is two. Top, bottom and scrollbar-side gaps match between both controls.
The configured Crystal 2-DIP and Office 1-DIP inset tokens are unchanged. Native
presenters, scrollbars and commands remain; no runtime or public API was added.

The new RibbonKit-only regression checks actual scrollbar chrome bounds in both
customization pages for 96 theme/light-dark/scale/direction combinations at
100/125/150/200%, plus native scrolling. It refreshes DPI on the fully realized
visual tree and asserts native part DPI rather than testing only raster scale.
Default painted gaps are checked exactly, with no fractional-pixel allowance.
The existing local-padding/scoped-inset and host-style fixtures use a four-DIP
padding to distinguish host padding from the independently rounded inset border.

Final validation: Release solution compilation passed with zero warnings/errors
for both runtime targets, the RibbonKit-only consumer passed, all 418 eligible
runtime tests passed with the existing Writer/hover-method exclusions, and all
105 visual scenes passed against unchanged approvals/tolerances. Crystal
light/dark and Office 2024 renders at 125/200%, including RTL, were inspected;
before/after probe renders also confirmed the pixel-gap correction. The first
consumer run passed the new 96-variant gap matrix, then failed an old combined
inset expectation for host padding. The fixture/calculation was corrected to
round border, host padding and inset independently; no default-gap tolerance
was added. The full consumer rerun passed. Documentation/link review and
`git diff --check` passed. No manual Showcase launch, commit, push or publication
occurred. User spacing and real DPI-return acceptance remain pending.
The output is isolated under `bin/CrystalSlice7Pixels/Release/<framework>/`;
earlier CrystalSlice7, CrystalSlice7Spacing and MenuScrollClickCheck builds are
preserved. Writer and the deferred Office RTL checkbox/200% gallery issues remain
untouched. Broader reduced-motion, open-menu scaling and mixed-monitor review
remain open.

### 3.207 Slice 7 bounded live review complete — 2026-10-01

The user confirmed follow-up checks 1 (corrected customization scrollbar spacing)
and 3 (125% → 200% → 125% return) passed for both reviewed themes using the
CrystalSlice7Pixels build. Check 2 (native scrollbar arrow/track/wheel/thumb,
focus and RTL) was accepted earlier. Original utility/arrows and modal/merged
caption checks 1–2 also remain accepted. This completes the bounded slice 7 live
review, alongside its recorded Release, runtime, consumer and visual evidence.

This is a documentation-only acceptance update. Content/link review and
`git diff --check` passed; no build or test was repeated. Existing edits, test
coverage, approvals and build outputs are preserved. No Showcase launch,
commit, push or publication occurred.

The prior Office RTL checkbox failure at 200% and the distinct 200% gallery-layout
report remain deferred, with unconfirmed causes. Reduced motion, scaling while
an application menu stays open and mixed-monitor transitions remain broader
gates. Slices 8–10 and Writer RKWF-026 remain deferred to their own scopes;
this acceptance does not claim blanket Crystal completion.

### 3.208 Shared Crystal contextual material and scoped palettes — 2026-10-01

Slice 8 promotes the accepted contextual glass into the ordinary shared RibbonTab
template. An internal ContextualMaterial selector/converter derives selected and
hover surfaces, rims, readable text and reflective marker from the tab's solid
contextual color. Dynamic token inputs follow local palette replacement and the
scoped accent fallback. Derived paint does not write tab resources or public
ContextualSelectionBrush values. Explicit foreground/marker values and unmarked
scoped paint retain precedence. Gradient/custom brushes retain the ordinary
contextual renderer; qualified color dependency-property paths avoid missing-Color
binding errors. Matching keys exist in all twelve palettes, with Office opt-out
defaults and Crystal's accepted 0.85 unselected opacity.

The additive public API is ThemeManager.CreatePalette(RibbonTheme theme,
Color? accent = null, bool dark = false), with XML documentation and an Unshipped
baseline entry; the Shipped baseline is unchanged. Each call creates a fresh
scoped dictionary without changing global theme/preferences or raising Changed.
Office palettes reuse the existing accent derivation; SetAccent remains unchanged.
Crystal uses the promoted hue rotation/readability policy, preserving alpha,
glass highlight geometry, neutral text/glyphs, semantic message colors and the
6/8/11-percent composed scrollbar washes. Replacing/removing the owner's dictionary
updates/clears that scope without accumulating hue rounding or recoloring another
window. The generic name and Office support follow the user's naming correction.

CrystalContextualTab and its main, preview and MDI usages were removed after the
library-only consumer passed. Showcase's CrystalPalette delegates reusable control
tint to the new factory; only application document paint remains there. The main
window's raw scrollbar-wash choice remains an explicit token override. Capture,
popup/menu blur and document effects remain host-owned for slice 9. No broad
coordinator was added; the separate preview and launch path remain for slice 10.

Final evidence: the isolated Release solution build passed for both runtime targets
with zero warnings/errors; all 418 eligible runtime tests passed with the existing
Writer/deferred-hover exclusions; the single STA RibbonKit-only consumer passed.
Its existing 96-variant customization gap coverage is preserved. New consumer
checks cover mutable contextual colors, marker/foreground and scoped brush
overrides, gradient fallback without binding errors, live accent fallback, RTL,
simultaneous light/dark windows, replacement/removal isolation, no global Changed
event and accent parity for all ten Office light/dark combinations.

All 113 visual scenes pass. The existing 105 approvals and tolerances are unchanged;
eight new light/dark contextual 100/200/RTL and purple-tint scenes were captured and
inspected before adding approvals. Their fixture stays in an offscreen test window
through rendering, updates DPI on the actual visual root and asserts a visible
reflective marker. Explicit minimum widths keep the diagnostic labels readable;
this changes the fixture only. Earlier disconnected captures omitted the marker
and were not approved. Four initial runtime failures were obsolete helper-storage,
palette-layer or literal-XAML assertions; they were updated to check the shared
replacement and the full eligible rerun passed.

Logs are slice8-consumer-final.trx, slice8-runtime-complete.trx and
slice8-visual-final.trx under their test projects' TestResults directories. Build
outputs are isolated under bin/CrystalSlice8/Release/<framework>/; previous user
builds are preserved. Final content/link/diff review and git diff --check passed.
No Writer code/tests, manual Showcase launch, commit, push or publication occurred.
Live acceptance remains in the active plan. The deferred Office RTL checkbox/200%
gallery reports, broader motion/open-menu scaling/mixed-monitor gates and Writer
RKWF-026 retain their separate scopes and unconfirmed causes.

### 3.209 Popup border and shadow DPI diagnostics — 2026-10-01

An opt-in Showcase-only `--popup-dpi-trace` observer samples dropdown and inherited
split-button popup native DPI/HWND bounds, painted border bounds, pixel shadow
headroom, rounding/clip flags and shadow parameters at opening, after layout and
500 ms later. It logs window DPI changes and closing too. Normal startup does not
enable it; no runtime/public API, template, paint, placement or dismissal changed.
The isolated `bin/PopupDpiTrace/Release/net8.0-windows/` build has a click-to-run
`Start Popup DPI Trace.cmd` launcher and writes its log beside the executable.
This is temporary diagnosis, not a rendering fix.

The Release Showcase build passed with zero warnings/errors. All 19 focused popup
motion, dismissal and new geometry/logging checks passed. The new two probes use
ordinary shared controls with Office 2024/Crystal tokens and repeated synthetic
owner-DPI changes; native popup DPI remains the actual monitor's 125% in this
process. Measured bottom shadow headroom is 10 pixels as expected for 8 DIPs.
They did not reproduce real Display Settings timing or establish the cause of the
intermittent bottom-border/shadow report. Content/commands were reported intact;
rounding remains unconfirmed. No snapshot approvals changed, full suite was not
rerun and the agent did not launch Showcase. Current acceptance/report status is
maintained only in the active plan.

### 3.210 Popup margins follow the DPI pixel grid — 2026-10-01

The user's 200% → 150% → 125% native trace shows correct HWND/paint DPI and
16/12/10-pixel bottom shadow headroom. At 125%, the popup border acquires a
full-face layout clip (170.4 × 192 DIPs); the 200%/150% openings have no clip.
This identifies paint clipping despite sufficient native window space. WPF rounds
margins for measure/arrange while its layout-clip calculation uses stored margins.
The 2-DIP top margin lands between pixels at 125%.

An internal PopupBorder now coerces each outer margin edge to the actual visual
DPI grid when UseLayoutRounding is enabled, and recoerces after DPI/rounding
changes. Both shared dropdown and split-button PART_MenuHost templates use it.
Requested local values remain intact; opting out restores them. The explicit
implicit-Border style lookup preserves consumer styles. Native layout clipping,
placement, effects, content presenters, commands and dismissal remain WPF-owned;
there is no public API addition or broad popup coordinator.

Five runtime checks cover repeated DPI changes, requested values/rounding opt-out,
genuine clipping, shared fractional layout and rendered shadow pixels, plus
reopened native headroom after synthetic owner-DPI changes. Both rendered 125%
fixture PNGs were inspected. The RibbonKit-only consumer adds 16 dropdown/split
openings across Office 2024/Crystal, light/dark and LTR/RTL, including scoped brush
and implicit style overrides. These checks do not reproduce native Display
Settings timing; the exact reported sequence remains a live recheck in the plan.

The isolated Release solution build passed with zero warnings/errors. All 423
eligible runtime tests passed with the existing Writer/deferred-hover filter;
the complete RibbonKit-only consumer passed in 112 seconds; all 113 visual scenes
passed without changing approvals or tolerances. The consumer's bounded STA
deadline increased from 90 to 180 seconds after a fresh run exceeded 90 seconds;
its new popup checks took about one second and existing coverage was retained.
The first interrupted run included the user's computer sleep and is not counted
as product evidence. Temporary progress-file writes were removed before the pass.

Logs are popup-dpi-edge-runtime.trx, popup-dpi-edge-consumer-final.trx and
popup-dpi-edge-visual.trx under the respective test projects' TestResults folders.
Outputs use bin/PopupDpiEdgeFix/Release/<framework>/, preserving previous builds.
The Showcase output includes Start Popup DPI Edge Check.cmd and retains the
opt-in observer for the pending live recheck. No Writer code/tests, manual
Showcase launch, commit, push or publication occurred. Existing broader/deferred
acceptance gaps retain their scope.

### 3.211 Vertical split hover width and diagnostic cleanup — 2026-10-01

Vertical Paste's primary half reserved a transparent 1-DIP border in flat Office
themes while the arrow half reserved none. Six rendered light/dark checks for
Office 2013/2019/2024 reproduced a 96-pixel primary wash versus a 98-pixel arrow
wash at 125%. Their existing SplitVerticalPrimaryBorderThickness defaults now
match the flat themes' zero-width highlight border. Office 2007/2010 and Crystal
retain their existing rim metrics; scoped metric overrides remain effective.
Before/after fixture PNGs were inspected. No new token or public API was added.

The RibbonKit-only consumer now covers 32 popup openings across four themes,
light/dark and LTR/RTL, equal vertical-half widths/side borders and scoped metric
replacement/removal. The temporary Showcase DPI observer/startup switch and its
logging-only test assertions were removed; native headroom, layout clipping,
requested-margin and rendered-shadow checks remain.

The final isolated Release solution build passed with zero warnings/errors;
all 429 eligible runtime tests passed with the existing exclusions; the full
consumer passed on a fresh process; all 113 visual scenes passed. Approvals and
tolerances are unchanged. An initial consumer run failed an existing customization
focus assertion; the unchanged fresh-process retry passed and its cause remains
unconfirmed. No focus behavior or assertion was changed. Two old Office border
expectations were updated for the corrected default before the full runtime pass.

Outputs are bin/PopupDpiHoverFix/Release/<framework>/; Start Hover Review.cmd starts
the normal Showcase executable. Evidence logs are popup-hover-runtime-complete.trx,
popup-hover-consumer-retry.trx and popup-hover-visual.trx under the test projects'
TestResults folders. No Writer changes, app launch, commit, push or publication
occurred. Live results and remaining gates stay in the active plan.

### 3.212 Optional captured backdrops and scoped glass overlays — 2026-10-01

Slice 9's bounded audit separates reusable capture/paint from host content,
registration policy and native window integration. The additive public
`CapturedBackdrop(Control control, FrameworkElement source)` registers one
application menu, dropdown/split button or ribbon group. `Apply`, `Refresh` and
`Dispose` provide explicit opt-in and cleanup. Tokens alone cannot supply a host
capture visual or own event subscriptions; a per-control disposable registration
provides that seam without importing the main-window coordinator. XML comments
and Unshipped entries document the addition; the Shipped baseline is unchanged.

The shared engines retain the 6-DIP Gaussian blur, 24-DIP sampling margin,
stationary application-menu anchor, opacity-based foreground exclusion, Canvas
measurement exclusion and post-blur clip. The clip reuses shared rounded geometry,
including changed/nonuniform corner overrides. Popup capture shares the snapshot
primitive, retains opaque fallback outside the source, and uses the existing scoped
application-menu frame-band token for the accepted tint. Local backgrounds,
bindings and host SetCurrentValue overrides keep precedence. Closing/disable clears
only the integration's own current paint, restoring template resource evaluation.
Reopening, template replacement, scroll/resize and observed source/popup DPI changes
refresh capture. Unload releases engines; reload reattaches while enabled. Disposal
aborts queued work and removes rendering, theme, layout and source handlers.

`ThemeManager.CreateGlassOverlay(FrameworkElement scope, bool dark = false)`
provides the existing cross-theme brush treatment with the accepted opacity and
accent-wash values. It clones effective resources without changing global theme or
activating Acrylic. Owners remove the preceding overlay before regeneration after
theme/accent/palette changes, merge the new dictionary and remove it on disable.
Direct scoped/child overrides retain WPF precedence. The host owns capture source,
content paint and explicit Refresh timing, dynamic-control registration/disposal,
preferences and Windows backdrop activation. Core Crystal requires neither opt-in.

Showcase's CrystalMenuBackdrop, CrystalApplicationMenuBackdrop and
CrystalPopupBackdrop were retired after replacement checks. Main and separate
preview callers use the shared registration and dispose on window close.
AcrylicGlassPresentation retains only dictionary ownership; paint creation is now
shared. CrystalMainWindowPresentation retains app palette scope and control
discovery. Document effects/content and the preview window/launch path remain in
the host; slice 10 and Writer RKWF-026 remain separate.

Verification: the normal Release solution build passed for both runtime targets
with zero warnings/errors. All 431 eligible runtime tests passed with the retained
Writer/deferred-hover filter. The visual suite passed all 113 scenes with unchanged
approvals/tolerances. The final Release RibbonKit-only consumer passed in 88 seconds
without Showcase references/resources/helpers. It retains all prior checks and adds
light/dark/RTL popup/menu paint, enable/disable, scoped palette replacement/removal,
reopening, scroll/resize, nonmeasuring/input/focus exclusions, collapsed content,
bindings/current-value overrides, unload/reload/disposal and twelve theme/dark glass
variants. Runtime coverage additionally retains rendered blur/foreground/shadow
checks and covers template replacement, opacity binding, corner changes, queued
cancellation and synthetic root-DPI capture. DPI metadata is checked against the
observed source DPI; these tests do not reproduce native Display Settings timing.

Earlier consumer attempts intermittently closed File in existing viewport/menu
assertions. A diagnostic run passed; the final run preserves the original baseline
case order and appends the optional effects checks after it. Temporary tracing was
removed and no existing assertion was relaxed. The final uninstrumented pass does
not establish a cause or a product fix for those earlier closures. The filtered
solution attempt passed runtime/visual projects but hit this consumer failure;
the final consumer pass is a separate process, not an unfiltered full-suite pass.
Evidence is slice9-release.trx in runtime/visual TestResults and
slice9-consumer-final.trx in the consumer TestResults directory.

The normal Debug Showcase build succeeded with zero errors and twelve designer-copy
warnings: Visual Studio (PID 27316) holds the Debug RibbonKit.DesignTools.dll open.
The runtime and Showcase outputs were updated in their default folders; running
apps were preserved. Local review uses
samples/RibbonKit.Showcase/bin/Debug/net8.0-windows/RibbonKit.Showcase.exe.
No manual Showcase launch, Writer code/test edit, commit, push or publication
occurred. The active plan owns the pending slice 9 live review; broader motion,
open-menu scaling/mixed-monitor and the two deferred 200% observations retain their
separate scopes. Content/link/final diff review and git diff --check passed.

### 3.213 Main Showcase consolidation — 2026-10-01

Crystal portability slice 10 moves the useful material-study demonstrations into
the existing main Showcase. No runtime code, public API, Writer code/tests or
snapshot approvals/tolerances changed. The separate `CrystalPreviewWindow` XAML,
code-behind, View launch button and `--crystal` startup branch are retired.
The preview-only `CrystalBackstagePresentation` and `CrystalScreenTipPalette`
helpers are also removed; their resource-scope and binding regression checks now
use direct shared resources and ordinary WPF bindings.

| Former preview demonstration | Main Showcase surface |
| --- | --- |
| Crystal / Office 2024 comparison | Samples: Compare 2024; View retains all six themes, dark mode and accent gallery. |
| Tab arrows, body arrows and QAT overflow | Samples: Try layouts adds/removes temporary navigation tabs, saves/restores each Home group's CanResize value, and saves/restores QAT items, width and placement. |
| Checkbox, mixed state, radio grouping and disabled gallery | Samples: Options, Spacing and States; explicit source bindings survive group reparenting. Home keeps its existing disabled button/split/input-group demonstration. |
| Document title, scrolling content, edge fade and QAT underlay | Samples: Document title and Document effects operate on the existing editable RichTextBox. The title is explicitly bound into document and Backstage content. Only the added scrolling paragraphs are removed, preserving unrelated document edits. |
| Context tint change | Samples: Change context tint selects Picture Format and changes its ordinary ContextualColor; existing Picture and merged Chart Tools retain their shared material. |
| Styles, inputs, messages and customization | Existing Home and File flows; Samples adds gallery enable/disable and message add/dismiss controls. The same three-page Options dialog is reused by migrated tests. |
| File menu, Backstage sidebar/floating designs, tint and native effects | Existing View/File controls and scoped palette integration; nested host dropdowns now receive CapturedBackdrop registrations through logical-child discovery. |

`MainWindow.Samples.cs` contains the host demonstrations. Temporary QAT changes
do not write customization while active; restoring overflow persists the real
QAT. Opening Options first removes temporary tabs/overflow and restores Home
resizing. Unidentified navigation demo tabs and commands are omitted by the
existing customization serializer. No persistence format or library coordinator
was added. Document fade and underlay start disabled, preserving the ordinary
document layout until explicitly selected in Crystal. `CrystalDocumentEdgeFade`
now locates the RichTextBox's native ScrollViewer, tracks template replacement,
retains the document card's original margins and keeps the scrollbar below the
overlapping QAT. Theme changes, QAT placement and notices restore or reapply the
requested host effect. These are application document effects, not library
control paint.

The six former preview-window regression tests now construct the actual main
window offscreen, skip its user-preference load, suppress persistence, set a
known factory ribbon and reset application resources on cleanup. They cover
customization frames/bindings, editable document scrolling and retained edits,
fade/underlay restoration, direct/nested dropdown and collapsed-group captures,
messages, tab/body arrows, QAT overflow and reparented option groups. Main-window
checks passed before the preview files were removed. Tests reacquire the File
frame after main-window theme switching because its File-surface selection can
detach/retemplate that control; a stale part is not current paint evidence.

Final automation used normal output folders: Release Showcase/test compilation
passed, followed by all **431 eligible runtime tests** with the existing
`FullyQualifiedName!~Writer&FullyQualifiedName!~Every_ribbon_button_family_consumes_the_shared_hover_glass`
filter (`slice10-release.trx`). The separate RibbonKit-only consumer passed its
single aggregate test in 1m49s (`slice10-consumer.trx`) without Showcase resources
or helpers. The normal Debug Showcase build passed with **0 warnings / 0 errors**
and updated `samples/RibbonKit.Showcase/bin/Debug/net8.0-windows/RibbonKit.Showcase.exe`.
Visual snapshots were not rerun for this consumer-only consolidation; approvals
and tolerances are unchanged. No manual app launch, commit, push or publication
occurred. The active plan owns the pending main-window live review; broader
motion, open-menu scaling, mixed-monitor and deferred 200% observations remain
separate gates. Changed content/links and final diff were reviewed; git diff
--check passed.

### 3.214 Theme gallery clipping and deferred test corrections — 2026-10-01

The user authorized investigating the two deferred 200% observations and then
the excluded Office 2010 hover-glass contract. They clarified that the gallery
report concerned only the View-tab Theme gallery: its selected tile was clipped
at the edges. Other galleries did not exhibit that difference.

The actual six Showcase Theme tiles used 42-DIP-high content. Shared item padding,
border and margins made each tile 52 DIP tall, exceeding the strip viewport of
50.4 DIP at simulated 125% and 50 DIP at simulated 200%. The text-only Home Styles
tile measured 50 DIP and fit both viewports. The fix reduces only the six host
content panels to 40 DIP; shared gallery templates, tokens and selection paint
are unchanged. `ThemeGalleryLayoutTests` loads the actual Showcase markup,
detaches the Theme and Styles galleries into an offscreen fixture, suppresses
appearance persistence and disables animation for settled geometry. It checks
all six theme selections after popup open/close: tile and selected-border bounds,
icon/text spacing, retained selection and the Styles comparison. Effective tile
DPI is asserted after returning from the popup. Corrected Theme tiles measure
49.6 DIP at 125% and 50 DIP at 200%, fitting their respective viewports. Before
and after PNGs in `TestResults/theme-gallery-diagnostics` were inspected; no
snapshot approval or tolerance changed.

`Detached_rtl_lab_applies_crystal_and_restores_office_options` previously measured
hidden, zero-sized checkbox/radio content when a constrained window collapsed
the Inputs group. At a requested width of 720 DIP, the original assertion failed
at both simulated 125% and 200%: indicator and header screen coordinates were
identical and their widths were zero. The test now opens the group's native
flyout when collapsed, requires nonzero visible parts, and preserves the physical
LTR/RTL position assertion. Its matrix uses requested widths of 1800/720 DIP and
scales of 125%/200%, retaining Office, Crystal light/dark, File and customization
coverage. The simulated scale is also applied to a flyout's separate visual root
and asserted on the option. No runtime option-placement change was required.

The unchanged Office 2010 consumer-count test reproduced its dropdown failure:
three references to `Control.HoverBackground` rather than the required five.
The two split halves correctly use `Control.SplitActiveHover`. That token has
the same Office 2010 gradient, including the bottom inner glow. The test now
counts both semantic hover consumers without lowering any family threshold.
Additional checks guard the split token's bottom glow and compare resolved
ordinary/split gradients in Office 2010 light/dark using only library resources.
The former hover exclusion is removed from the current verification command;
historical filtered results remain unchanged.

Release test compilation and the 33-case focused run passed
(`deferred-issues-focused.trx`); the final strengthened RTL class passed all five
cases (`rtl-root-dpi-after.trx`). The final Release runtime run passed **443 tests,
0 failed / 0 skipped** with only `FullyQualifiedName!~Writer`
(`deferred-issues-runtime-final.trx`). This includes all four hover-contract
family cases. The normal Debug Showcase build passed with **0 warnings / 0 errors**
and updated its default review executable. Visual snapshots, the separate
portability consumer and the full solution suite were not rerun for this host
content/test-only change. No runtime/public API or Writer code/tests changed.

Synthetic visual scaling uses `VisualTreeHelper.SetRootDpi` and constrained
fixture widths; Windows display settings were not changed. Popup HWND DPI,
native Display Settings timing and mixed-monitor transitions are not simulated
by those visual-root checks. Native 200% Theme gallery visual acceptance remains
with the user in the active plan, as do the separate broader motion/open-menu
gates. No manual Showcase launch, commit, push or publication occurred.

### 3.215 Gallery selected-row retention after tab reload — 2026-10-01

The user's recording shows the View Theme gallery displaying the selected Office
2024 tile through several tab visits, then showing its first Crystal tile after
Home → Ribbon Lab → View. A RibbonKit-only reproduction establishes that the
selection survives while the native strip scroll offset resets to zero when the
tab content reloads. Four Office 2024/Crystal light/dark cases failed before the
fix at simulated 125%/200%; the selected fifth tile lay roughly 200 DIP below
the 50-DIP viewport although `SelectedItem` still referenced that tile.

`InRibbonGallery` now queues its existing generation-guarded viewport refresh
when a closed gallery with a selection loads, and when its template is rebuilt
while loaded. The settled strip reveals the selected row without animation.
The restoration target adds the current vertical offset to the tile's viewport
position, keeping the absolute row calculation correct. No selection mutation,
new public API, Showcase patch or theme/template paint change was introduced.
The separate permanent strip/popup scrollers remain intact, and opening the
popup still resets its own page to zero.

`InRibbonGallerySelectionTests` exercises the recorded pattern with ordinary
RibbonKit tabs and two galleries, then replaces the template without unloading
the gallery. It checks the same selected item, full selected-border visibility
and no extra selection events. Manual browsing away from the selected row
survives unrelated resize/layout while the gallery stays loaded. The focused
run passed 14 cases: four new retention cases, two Theme tile clipping cases,
and eight existing shared gallery/DPI reproductions from
`WriterConsumerFrictionTests` (`gallery-row-focused.trx`). No Writer application
code or tests were edited.

`GallerySelectionPortabilityChecks` runs at the end of the existing separate
consumer aggregate. It uses only RibbonKit resources, `ItemsSource`, a host data
template and a two-way bound selected index. All six themes, light/dark, LTR/RTL
and simulated 125%/200% check restored selected bounds after tab reload and popup
open/close, retained binding/source value and the popup's independent viewport.

The normal Release solution build passed with **0 warnings / 0 errors** for both
runtime targets. The serialized solution test run used only the retained
`FullyQualifiedName!~Writer` exclusion: **447 runtime tests**, the visual snapshot
aggregate with unchanged approvals/tolerances, and the expanded RibbonKit-only
consumer aggregate all passed (`gallery-selection-release.trx` in each project's
TestResults). The consumer completed in 1m25s. Writer tests were excluded, so this
is a filtered solution pass rather than an unfiltered full-suite claim. The
normal Debug Showcase build also passed with **0 warnings / 0 errors** and updated
its default review executable.

The supplied recording was inspected through extracted frames. No manual
Showcase launch, commit, push or publication occurred. Live confirmation of its
tab-switch sequence remains in the active plan; synthetic visual DPI coverage
does not close native popup/monitor-transition or broader motion acceptance.

### 3.217 Crystal pre-merge scope and resource cleanup — 2026-10-04

The project-wide audit reproduced a QAT policy leak: an independent Crystal
window palette inherited the global accented Office 2019 icon treatment because
`Ribbon.UpdateQatButtonContext` read global theme flags. The shared Ribbon style
now resolves `QatTitleBarColored` and `QatTabRowColored` metrics in its own resource
scope. ThemeManager sets these alongside application accent-band overrides;
every base palette supplies neutral defaults. Internal attached selectors refresh
existing QAT items when effective scoped values change. No public C# API was added.
The RibbonKit-only consumer covers independent Crystal light/dark scopes against
global Office 2019, Office 2024 and Crystal, every QAT placement, nearer metric
overrides, palette removal and accent-band toggling.

`CrystalMainWindowPresentation` remains Showcase's host adapter for palette and
optional `CapturedBackdrop` registration. Its previous tab-only discovery missed
QAT dropdowns and additions to existing groups, and retained removed dropdowns.
It now watches the tab, group, QAT and nested ItemsControl collections while enabled,
reconciles registrations and disposes removed captures. Disabling or closing
detaches collection handlers and disposes the remaining registrations. A focused
test checks an actual QAT popup's captured brush, subsequent additions/removals,
collection resets, disable/re-enable and close. Capture implementation remains in
the existing optional library contract; document fade/underlay paint is host-owned.

`Controls.ActionTemplates.xaml` holds the shared message/Crystal Backstage action
template, and `Controls.Actions.xaml` holds its styles and interaction states.
Office palettes use its standard defaults; Crystal light
and dark select its glass defaults with thin style references. Geometry, access-key
recognition and disabled opacity use typed metrics. Idle paint uses ordinary
setters so a derived host style retains its WPF precedence; it is not supplied by
a material trigger. The existing message style and scoped template keys are kept,
including direct Button values and live brush lookup. Palette dictionaries merge
the shared actions so manually merged palettes also expose the keyed resources.
The dark palette merges them directly because tint construction can realize its
styles before attaching the combined palette. The message template also merges
the shared template dictionary in its own stable scope: replacing a palette's
style must not replace the button template and discard local part values.
The separate consumer retains light/dark and all-Office action coverage, adds
disabled opacity checks and verifies replacement/removal of the existing scoped
template key.

The Crystal Sidebar/Floating layouts use `RibbonKit.Metrics.Backstage.*` for
navigation, action and container geometry. Every base palette supplies the former
values, preserving an independently selected Backstage design under an Office
palette. The fixed `CRYSTAL` text is removed from the reusable floating template.
Shared-resource tests check back-action paint and scoped corner, padding, floating
margin and sidebar-width replacement/removal. Stale Office 2007 orb and Writer
glyph-migration wording is corrected in the public/theme documentation.

Normal Release and Debug solution builds passed with **0 warnings / 0 errors**
for both runtime targets and refreshed the default Showcase/Writer review outputs.
The final serialized, unfiltered Release solution run passed **466 runtime tests**,
**482 Writer tests**, the visual aggregate covering **113 approved scenes**, and
the RibbonKit-only consumer aggregate in **2m28s**: **950 tests, 0 failed / 0 skipped**.
Each test project records `crystal-cleanup-release.trx` in its TestResults folder.
The final consumer also passed independently in 2m42s
(`crystal-cleanup-consumer.trx`). Visual approvals and tolerances are unchanged.
All **338** shared brush/metric/effect references resolve across the twelve
effective light/dark palettes; the 31 new metric defaults have matching types and
values across all Office bases. The final diff/content/links were reviewed and
`git diff --check` passed.

The agent did not manually launch Showcase or Writer, commit, push or publish.
The active plan owns bounded live review of the revised Backstage appearance and
dynamic popup/QAT capture. Existing native DPI, motion, open-menu scaling and
mixed-monitor gates retain their separate scope; automated proof does not close them.

The follow-up Showcase review needed a visible disabled message action. Main
Showcase now adds a third, initially closed `CONTENT BLOCKED` message through
the existing Add message control. Its Enable Content action uses an unbound
host-owned routed command, so native command routing disables only the action;
the row and dismiss button stay enabled. Dismiss messages includes the new row.
No shared control, template, token, Writer code or RibbonKit public API changed.
The existing sample markup check now expects three main-window messages and
two RTL-lab messages. The existing Crystal integration fixture opens all three,
checks the disabled action's 0.4 opacity and enabled row/dismissal, then verifies
Dismiss messages closes the new row. All seven focused existing tests passed.
Normal Release and Debug Showcase builds passed with zero warnings/errors and
updated the default review executable. The full suite and visual snapshots were
not repeated for this host-only example; its live appearance remains in the plan.

### 3.218 Keyboard focus and application-menu navigation — 2026-10-04

The user reported a dotted focus rectangle around the whole Ribbon, duplicate Tab
stops on ordinary application-menu navigation rows, an unreachable split arrow,
and Enter failing on a focused row. `Ribbon` and `RibbonApplicationMenuItem`
inherited focusability from `Control`; each navigation row also contained a native
Button, while its arrow explicitly disabled focus. The container had no native
button activation behavior.

The shared defaults now exclude the Ribbon and navigation-row containers from
keyboard focus and Tab traversal. Native template buttons own Enter/Space,
command execution and Click routing. Plain commands and dropdown rows have one
Tab stop; split commands and their arrows have separate stops. A dropdown's primary
target spans the whole row, with a token-sized spacer preserving the header's
original layout area. Keyboard navigation claims the same panes as pointer hover.

The menu keeps a fallback focus target without an outline for empty content, excluded
from the cyclic Tab order. Opening defers focus to the first enabled action until
layout realizes its template. This initial transfer does not claim a pane: opening
still shows the default page, including menus whose first item has pane content.
That distinction preserves the existing viewport/default-page contract. Subsequent
navigation updates pane ownership; pane commands do not reset their own pane.
Scrollbar-button dismissal exemptions and native scrolling remain intact.

`Controls.Focus.xaml` supplies one solid, inset, theme-colored WPF focus adorner.
Ribbon buttons/toggles/options, tabs, split/dropdown template buttons, menu rows,
gallery tiles and utility actions reference it from their shared dictionaries.
Its brush and rounding use existing input-focus and control-corner tokens.
WPF retains keyboard-only focus display, rather than adding persistent mouse-focus
outlines. The menu fallback suppresses its own focus visual. No public API or
Showcase/Writer implementation was added.

The RibbonKit-only consumer regression traverses ordinary, disabled, separator,
split and dropdown rows, pane commands and the footer in both directions. It checks
focus cycling without entering the document, native Enter/Space Click activation,
routed command/parameter execution, command dismissal, pane-open retention and Esc,
across all six themes, light/dark and LTR/RTL. It also realizes WPF's actual focus
adorner and checks its brush and bounds. Optional `RIBBONKIT_FOCUS_DIAGNOSTICS`
exports Crystal command/arrow/dropdown PNGs. All six light/dark diagnostic images
were inspected; the split ring surrounds the arrow alone and the dropdown ring
surrounds the full row.

The offscreen fixture forces WPF's keyboard-focus visual display through its
internal keyboard-navigation hook and raises routed key events. This verifies
realized control/template behavior, rather than physical keyboard or target-machine
appearance acceptance. Native Showcase keyboard review, DPI/mixed-monitor behavior
and final visual acceptance remain separate live gates.

Normal Release and Debug solution builds passed with zero warnings/errors for
both runtime targets and refreshed the default Showcase/Writer review outputs.
The final serialized Release solution run passed **950 tests, 0 failed / 0 skipped**:
**466 runtime**, **482 Writer**, the visual aggregate covering **113 approved scenes**,
and the RibbonKit-only consumer aggregate. Results are recorded as
`keyboard-focus-release-final.trx` in each test project's TestResults folder.
The final consumer also passed independently (`keyboard-focus-consumer.trx`).
Visual approvals/tolerances and the public API baseline are unchanged. The final
diff and documentation pointer were reviewed; `git diff --check` passed.
Showcase/Writer were not manually launched, and nothing was committed, pushed or
published.

### 3.219 QAT order, menu traversal and Backstage focus cycling — 2026-10-04

After reviewing §3.218, the user accepted the focus changes except for lower-QAT
ordering, dropdown/application-menu traversal and Backstage wrapping/action focus.
Their recording was reviewed. The follow-up remains shared RibbonKit behavior;
no Showcase/Writer glue or public API was added, and the accepted focus paint is
unchanged.

WPF walks panel children in paint order. The body is raised over a lower QAT's
shadow and the headers are raised over the body for connected-tab chrome. Local
navigation groups and TabIndex values now order the shared visual roots as
headers, body commands, lower QAT. They leave ZIndex and geometry intact. The
template root is the group instead of the TabControl, whose remembered selected
header otherwise overrides the first entry point. A tab-row QAT still precedes
body commands, including after changing placements.

Dropdowns and inherited split dropdowns transfer focus into their realized popup
items after opening. Tab/reverse traversal and arrows cycle within the menu;
Home/End select its ends. Up/Down can open to the last/first item. Closing while
focus belongs to the menu returns it to the opener. Direct popup Escape handling
uses the existing dismissal stack, preserving nested flyouts and borrowed-item
return. Arrow handling precedes ScrollViewer consumption for menu buttons while
embedded editors retain their normal input.

Application-menu Up/Down/Home/End traverse enabled navigation rows. Horizontal
arrows visit a split arrow and its pane and return to the navigation target,
mirrored in RTL. Pane buttons also support vertical traversal. Ordinary commands
can enter the default pane. The existing cyclic Tab order, Enter/Space activation,
default-page-on-open behavior and scrollbar dismissal exemption remain intact.

Backstage action containers coerce IsSelected to false. WPF TabItem previously
selected an action during PreviewGotKeyboardFocus; reverting that selection also
returned focus to the previous page, preventing traversal beyond Options to Exit.
Action focus now preserves the selected page and Enter/Space performs the action.
Cycles are scoped to each shared template's visual root so wrapping visits the
return button and first navigation item instead of the remembered last page.
The design-owned hidden return buttons remain hidden.

`KeyboardNavigationPortabilityChecks` extends the RibbonKit-only consumer with
WPF InputManager key processing, including native navigation post-processing;
raising routed events alone had missed these transitions. Reverse traversal uses
WPF MoveFocus. Coverage includes all six themes and LTR/RTL, placement round trips,
disabled rows/separators, dropdown and split activation/command parameters,
application-menu pane traversal, repeated cycles and all seven Backstage designs.
The nested QAT case checks inner-first Escape dismissal, focus recovery and
borrowed-item return. Synthetic/offscreen evidence remains separate from physical
keyboard and target-machine acceptance.

Normal Release and Debug solution builds passed with zero warnings/errors and
refreshed the default consumer review outputs. The serialized Release solution
invocation passed **466 runtime tests**, **482 Writer tests** and the visual
aggregate covering **113 approved scenes** (`keyboard-navigation-release-final.trx`).
Its consumer failure was an offscreen fixture setup error: the outer QAT chevron
is intentionally nonfocusable, and the nested popup needed a focused owner surface
before opening. The corrected RibbonKit-only consumer then passed independently
in a fresh process (`keyboard-navigation-consumer.trx`, 2m33s), including nested
Escape and the full existing portability aggregate. The combined final results
are **950 passing tests, 0 skipped**; the affected consumer was rerun rather than
repeating unchanged passing projects.

Visual approvals/tolerances and the public API baseline are unchanged. The final
diff and documentation links were reviewed; `git diff --check` passed. Showcase
and Writer were not manually launched. Physical keyboard/DPI review remains a
live gate for the user; these follow-up changes are uncommitted.

### 3.220 File-first entry, QAT overflow and Backstage opening focus — 2026-10-04

The next recording exposed three remaining cases: File followed the selected tab
and minimize toggle, zero-slot QAT commands remained keyboard stops, and opening
Backstage focused its whole surface with WPF's dotted outline.

The shared header template now orders the File layer, tab-row QAT, tab strip and
minimize toggle explicitly, retaining their paint order and geometry. Native Tab
entry from document content starts at File; selected-tab navigation and the lower
QAT order from §3.219 remain intact.

RibbonQuickAccessPanel temporarily excludes each overflowed original subtree from
Tab, Ctrl+Tab and directional navigation and suppresses the root's focusability.
Visibility, enabled state and proxy commands remain application-owned. Current-value
overrides preserve bindings and local/style values, observe application updates
while hidden, and are released when items fit again, leave the toolbar, or the
panel becomes hidden/unloaded. InvalidateProperty removes the temporary override;
ClearValue would discard an application's local value or binding.
Template replacement releases the old panel's overrides before another panel
can claim the same live items.

Backstage opening enters its visible Back button or the first available action
after its template/layout is realized. WPF's automatic selected-header focus during
container generation is included in that opening transition. The root has no focus
adorner. Shared Backstage item styles, Crystal return actions and the Classic2007
orb return proxy use the existing theme-colored keyboard outline. No public API,
Showcase helper, theme token, visual approval or tolerance was added.
The shared style is resolved statically by the dictionaries; only its palette and
metric tokens remain dynamic. A dynamic lookup of the style key from a native
Button in the adorner branch fell back to WPF's default dotted focus style.

The RibbonKit-only keyboard consumer now covers File entry from document content,
selected-tab preservation, closed QAT overflow in both directions, width recovery,
binding/local-value retention and removal, and Enter opening/reopening with real
Backstage focus adorners across all six themes, LTR/RTL and seven designs.

Normal Release and Debug solution builds passed with zero warnings/errors. The
serialized full Release solution run passed **950 tests, 0 skipped**: **466 runtime**,
**482 Writer**, the visual aggregate covering **113 approved scenes**, and the
RibbonKit-only consumer aggregate (`keyboard-entry-release-final.trx`). The consumer
completed in **3m04s**, including minimized entry, QAT template replacement and the
explicit shared Crystal Backstage styles. Its existing STA aggregate timeout was
raised from 180 to 300 seconds after the expanded render-diagnostic run reached
the old cap; assertions and visual approvals/tolerances remain unchanged.

Real focus adorners were checked for their solid theme brush and action bounds,
and rendered Crystal Back/row outlines were inspected. These offscreen checks
do not establish physical keyboard or DPI acceptance. The default Debug Showcase
output is refreshed for user review; Showcase/Writer were not manually launched.
The final diff and documentation links were reviewed and git diff --check passed.
These follow-up changes remain uncommitted.

### 3.221 Active Backstage entry and a single focus outline — 2026-10-04

User review identified duplicate outlines on Crystal Back and navigation items
and changed the opening preference from Back to the active page. Backstage now
focuses its selected visible/enabled navigation container after realization,
preserving the selected page. If unavailable it tries the first available item,
then Back, retaining the root only as an empty-surface Escape fallback.

Crystal navigation's legacy focused accent-border trigger is removed. The Crystal
Backstage action style retains its ordinary glass border when focused, overriding
the inherited glass-action accent emphasis. The shared keyboard adorner is the
single focus outline; selection, hover and unrelated message actions retain their
existing paint. No public API, palette token or consumer helper is added.

The RibbonKit-only keyboard checks open on a non-first selected page, preserve
selection, reopen on the last active page, fall back from a disabled selection,
and check native focus adorners and unchanged ordinary template borders while
focus moves between page content, Back and navigation. The matrix includes all
six themes, LTR/RTL, seven designs and
the explicit shared Crystal styles. Optional render diagnostics capture Crystal
Back/row outlines for inspection without repeatedly rendering every palette.

Normal Release and Debug solution builds passed with zero warnings/errors. Final
passing evidence covers **950 tests, 0 skipped**: **466 runtime**, **482 Writer**,
the visual aggregate covering **113 approved scenes**, and the RibbonKit-only
consumer aggregate. The initial serialized solution run had two Writer keyboard
failures; both passed in fresh targeted runs. The F10 check identified a held Ctrl
key, which the user confirmed and released before its passing retry.
The consumer fixture now drains the selected-content DataBind update before
focusing page content; its final Release run passed in **3m06s**
(`backstage-active-focus-consumer-final.trx`). No already-passing project was rerun.

Real focus adorners and unchanged template borders passed throughout the matrix.
Rendered Crystal Back and navigation outlines were inspected, including RTL.
Visual approvals/tolerances and the public API baseline are unchanged. The normal
Debug Showcase output is refreshed; Showcase/Writer were not manually launched.
Physical keyboard/DPI acceptance remains with the user. Documentation links and
the final diff were reviewed; git diff --check passed. Changes remain uncommitted.

### 3.222 Animated focus placement and collapsed-group navigation — 2026-10-04

User review exposed a focus outline lagging the Backstage slide and an inaccessible
collapsed-group flyout. The supplied recording was inspected. Realized native
focus tests reproduced a 15-DIP intermediate-slide offset in LTR and RTL; group
tests showed focus remaining on the collapsed opener after Enter.

The shared keyboard-focus template now opts into an internal placement tracker.
While its native WPF adorner is loaded, rendering ticks compare the adorned
target's full coordinate basis and render size. Changed placement refreshes the
target's adorner layer, matching the existing KeyTip correction in §3.3. Unchanged
frames perform no layout update. Unloading/disabling releases the rendering
subscription, and a newly loaded outline starts fresh. WPF still owns keyboard-only
display; there is no fixed animation delay or new public API.

RibbonGroup now moves focus into its realized flyout, cycles native Tab/reverse
and spatial arrow navigation, and lets Up/Down open at the last/first command.
Home/End visit the flyout's ends; embedded editors keep their normal arrow handling.
Escape uses the existing nested dismissal stack and returns to the opener.
Command invocation retains deferred closure/reparenting and restores focus when
owned by the flyout; closing after focus leaves preserves the new target.
Template replacement detaches the old keyboard handlers. No Showcase/Writer helper
or palette change is introduced.

Six native runtime regressions cover intermediate/final slide placement, scaling
and focus-visual removal/recreation in Crystal and Office 2024, LTR/RTL; collapsed
groups cover command traversal, disabled commands, an editor, the dialog launcher,
nested dropdown Escape, Up/Down entry and invocation/focus return. The first four
regressions failed before the production fix and passed afterward. The independent
RibbonKit-only consumer additionally checks native Tab cycles, reverse traversal,
Enter/Space entry, Home/End, real focus adorners and external focus preservation
for collapsed groups across all six themes and both text directions.

The normal Release solution build passed for both runtime targets with zero
warnings/errors. The serialized full Release solution run passed **956 tests,
0 failed / 0 skipped**: **472 runtime**, **482 Writer**, the visual aggregate
covering **113 approved scenes**, and the independent consumer aggregate
(`focus-motion-group-release.trx` in each project's TestResults folder).
The consumer completed in **3m50s**; its existing 300-second timeout and all visual
approvals/tolerances remain unchanged.

At this checkpoint, the normal Debug solution refresh was blocked while Showcase and its Visual Studio
debugger held the consumer's RibbonKit.dll. The user was asked to stop debugging
and close Showcase; that preview contained the previous build until the refresh in §3.223.
No application was manually launched or closed. Physical keyboard, live motion
and DPI acceptance remain with the user. The final diff and new documentation
pointer were reviewed; git diff --check passed. Changes remain uncommitted.

### 3.223 Circular Back and application-orb focus — 2026-10-04

User review accepted the preceding keyboard changes except for rectangular focus
outlines on circular buttons. The two supplied screenshots show the circular
Back action and the Office 2007 orb; the orb's visible disc extends above its
shorter tab-row button slot.

Controls.Focus now provides shared circular and application-orb focus styles.
The circular style draws an inset vector ellipse. The orb style mirrors the
existing ApplicationOrbSize and ApplicationOrbMargin tokens with top alignment,
so the ring follows the sphere's overhang instead of the rectangular hit-test
slot. Both keep the existing input-focus brush, one-DIP stroke and loaded-only
animation tracking. The Backstage disc uses the circular style; the application
button selects the orb style only in Orb mode. Returning to Tab mode restores
the ordinary rectangular focus style. The Classic2007 return proxy already copies
the application's resolved focus style and uses the same sphere template, so it
inherits the correction. No public API, palette token or host helper was added.

Two realized native-focus regressions cover the real orb, its Classic2007 proxy,
an outline Back disc, a glass Back disc and restoration of File-tab focus in LTR
and RTL. They fail before the new styles. Bounds are compared with the visible
disc inset, allowing at most one physical pixel for separate native-focus and
ribbon layout rounding at fractional DPI. The captured actual rings were inspected
before establishing that bound. The shape fixture disables/restores motion to
keep successive design changes independent; the existing motion regressions
continue to cover render tracking. Its PNGs show circular rings in both directions.

The independent consumer asserts circular native adorners for the Backstage disc
and verifies explicit Orb/Tab switching across all six themes, light/dark and
LTR/RTL. Rectangular controls retain their existing brush/bounds checks.

Normal Debug and Release solution builds passed for both runtime targets with zero
warnings/errors after the user released the file locks. The default Debug Showcase
and Writer outputs are refreshed, including the preceding motion/group changes.
Final passing evidence covers **958 tests, 0 skipped**: **474 runtime**, **482 Writer**,
the visual aggregate covering **113 approved scenes**, and the RibbonKit-only
consumer aggregate (`circular-focus-release.trx`). The initial full run had one
Writer editor-focus assertion failure; it passed in a fresh targeted process
(`circular-focus-writer-retry.trx`) without changing Writer code or assertions.
Its transient cause was not confirmed. The independent consumer passed in **3m13s**.

The circular Back, real orb and proxy PNGs were inspected in LTR/RTL. Visual
approvals/tolerances and the public API baseline are unchanged. No application
was manually launched. Live circular-focus and DPI acceptance remain with the user.
The final diff and new documentation pointer were reviewed; git diff --check passed.
Changes remain uncommitted.

### 3.224 Focus outlines follow rendered button surfaces — 2026-10-05

User screenshots showed an offset focus outline on the Colored Title Bar toggle
and mismatched corners on a Crystal Backstage navigation row. The native focus
template used the whole control's bounds and a generic corner radius; shared
templates paint their visible Chrome border inside that slot with independent
margins and corner geometry.

FocusVisualTracking now measures the realized Chrome border for ordinary shared
actions and the visible OrbFill ellipse for application orbs and Classic2007
proxies. Other templates retain the control-bounds fallback. A two-DIP inset
contour follows the surface, with correspondingly reduced border corner radii.
The focus template uses a vector border or ellipse in a local LTR canvas and maps
the surface's full coordinate basis into it. This preserves margins, alignment,
RTL, scale and rotation without a separate layout-rounding approximation. Orb
focus reuses the circular style instead of duplicating sphere layout metrics.
Loaded-only layout/render tracking refreshes geometry and motion; unloading or
disabling the tracker releases both subscriptions. WPF still owns keyboard-only
display, and the existing focus brush follows the palette. No public API, theme
token, Showcase helper or input behavior was added.

Four new native-focus regressions cover Office2024/Crystal and LTR/RTL. They fail
on the former toggle bounds, then verify toggle, gallery-tile and Crystal navigation
surfaces, including changed margins, unequal corners and render transforms while
focus remains on the same control. All **12 focused interaction cases** passed,
including the existing motion, circular/proxy and collapsed-group cases. Disc
and border geometry now agree within **0.1 DIP**, replacing the previous one-
physical-pixel circle approximation. Actual before/after, transformed RTL,
circular and independent-consumer light/dark PNGs were inspected.

The independent RibbonKit-only consumer checks realized toggle and gallery-tile
focus across all six themes, light/dark and LTR/RTL, plus the existing navigation,
Backstage and orb matrix. The helper now processes the focus template's Loaded
event before measuring it, and compares popup geometry within its own adorner
instead of across separate presentation roots. Shape checks run first; any
navigation failure records the last key, modifiers and focus/window state.
These are verification changes, not input-behavior workarounds.

Normal Debug and Release solution builds passed for both runtime targets with
zero warnings/errors. Passing evidence covers **962 tests, 0 skipped** across
the full run and targeted retries: **478 runtime**, **482 Writer**, the visual
aggregate covering **113 approved scenes**, and the complete independent consumer.
The initial visual run reached its 30-second limit; a separate unchanged retry
passed in 26 seconds (`surface-focus-visual-retry.trx`). The Writer F10 check
detected Alt during the initial run and passed unchanged in a fresh process
(`surface-focus-writer-retry.trx`). After correcting the geometry helper, two
consumer attempts lost focus at different existing navigation assertions; no
runtime navigation code or assertions were weakened. The final full consumer
passed in **4m39s** (`surface-focus-consumer-final.trx`). The cause of those
intermittent input failures was not confirmed. This is combined passing evidence,
not a claim that the initial full-suite invocation was clean.

Default Debug outputs are refreshed. Public API and visual approvals/tolerances
are unchanged. Final appearance and native DPI/mixed-monitor acceptance remain
with the user; no application was manually launched. The final diff and history
pointer were reviewed and git diff --check passed. This correction is uncommitted
on top of the user's `7cc425c` keyboard-improvements checkpoint.

## 3.225 Focus contrast on accent-filled rails and headers — 2026-10-05

Office 2013/2019 Classic Backstage rows used the accent focus brush over an
accent-derived selected/hover fill. Office 2019's colored title-bar option also
painted the ribbon header band with that same accent, hiding focus on File,
unselected tab headers and the collapse button. Native-focus checks reproduced
the exact blue-on-blue failure with a contrast ratio of 1.

Shared templates now mark the underlying focus surface for File, ribbon tabs,
the collapse button, Classic Backstage rows and the shared Back button. The
internal focus tracker composites a solid Chrome fill over that surface and
keeps the ordinary focus brush when it provides at least 3:1 contrast. Otherwise
it selects the more contrasting KeyboardFocus.Light or KeyboardFocus.Dark brush;
both keys exist in all twelve theme token dictionaries. Selected tabs with a
neutral fill retain the accent outline when readable. Orb focus retains its
existing treatment. HeaderChrome is recognized alongside Chrome for tab geometry.

Palette, hover/selection fill and colored-title-bar changes refresh the native
adorner while focus stays on the same control. Navigation fills, text colors,
keyboard behavior and public C# API are unchanged. The implementation lives in
RibbonKit and requires no Showcase resources or host patch.

Four new native-focus cases cover Office 2013/2019, light/dark, LTR/RTL and blue,
purple and pale-yellow accents, including selected/hovered Backstage rows, Back,
File, neutral/colored tab headers and the collapse button. The independent
RibbonKit-only consumer also checks the realized outlines and geometry on both
themes' colored headers and Classic rails.

Normal Debug and Release solution builds passed for both runtime targets with
zero warnings/errors; default Debug consumer outputs are refreshed. Passing
Release evidence covers **966 tests, 0 skipped**: **482 runtime**, **482 Writer**,
the visual aggregate (**113 unchanged approved scenes**), and the independent
consumer. The first two consumer runs missed Space activation at different
existing assertions. The user confirmed they were interacting with the keyboard;
after leaving input idle, the complete consumer passed in **3m21s**
(`contrast-focus-consumer-final.trx`). Assertions and runtime input behavior were
not weakened. The consumer fixture switches its File surface from the application
menu to Backstage before inspecting the new rail cases, and activation failures
now include modifier/focus diagnostics. These totals combine the full run with
that consumer retry, rather than describing the initial invocation as clean.

Actual before/after blue, purple, pale-accent, dark and RTL focus renders were
inspected, including the independent consumer's colored header. No snapshot
approvals or tolerances changed. Final appearance and native DPI/mixed-monitor
acceptance remain with the user; no app was manually launched. Changes remain
uncommitted on the user's keyboard-improvements branch.

## 3.226 Shared Compact/Touch density — 2026-10-07

Added the documented `RibbonDensity` enum and inherited attached `Ribbon.Density`
property, with Compact as the default and additive entries in the unshipped API
baseline. Density is independent of theme, DPI, command size and customization
state. Hosts own the selector and preference persistence; local values retain
ordinary WPF precedence. Showcase demonstrates the library setting through
View → Touch mode and saves it with appearance preferences.

One shared template set consumes matching Touch metrics in all six base
palettes; dark overlays inherit geometry. Commands, split halves, input boxes,
tabs, launchers, QAT openers, File navigation and shared actions use 44-DIP targets.
The body sizes to its command stacks with a 136-DIP minimum. In-ribbon galleries
retain one tile row with a single 44-DIP popup opener beside it. Long dropdown
menus gain a scrollable viewport, and native vertical panning is enabled in menu
and gallery viewers. Popup, detached title-bar QAT, native context menu and
Backstage roots explicitly follow the owning density. Outside-touch dismissal
shares existing popup rules and leaves the routed event available to the target.

Switching density invalidates the adaptive width cache and the group's measurement
subtree. Clearing the cache alone left stale widths when an individual command
changed density while the group remained in its Large state; the independent
consumer reproduces collapse and recovery for both local and ribbon-wide changes.
The gallery opener column uses GridLength, and body-scroll arrows set MinWidth so
the template's local compact Width cannot leave a narrow touch target.

The RibbonKit-only consumer verifies both modes across all themes, light/dark and
LTR/RTL, split geometry, retained values, gallery viewport/opener layout, all QAT
placements, overflow and context menus, File surfaces, token overrides, adaptive
reduction and routed outside-touch dismissal. Physical input and mouse promotion
remain separate acceptance gates. Current evidence and remaining live review are
recorded in the [active touch plan](../14-TOUCH-DENSITY-PLAN.md#verification-and-acceptance).

The user's screenshots led to a geometry cleanup: large commands share a 96-DIP
height with 36-DIP icons, small/QAT icons grow to 20 DIP, and horizontal split
content centers within its primary target. Bottom-aligning the tab panel removes
the enlarged-row seam at the connected notch/hover foot. Application-menu fills
and arrow spacers match the actual touch arrow width; 56-DIP rows and a wider
nav column avoid shrinking the Office2007 menu or clipping labels. Shared and
Crystal Backstage rows use larger text/height, separators stretch with command
stacks, and the orb/proxy share adjusted touch geometry.

A 96-DIP separator minimum also enlarged the compact Font row, despite its local
20-DIP Height. Using a 44-DIP minimum with stretch follows the adjacent controls:
the Font row stays button-height while a large-command divider grows with the
taller stack. The actual Showcase check compares the separator to its neighboring
button, rather than only checking the separator's own outer height.

The expanded consumer exposed an Office2007/Classic2007 layout loop. Ribbon
reconciles the orb proxy from LayoutUpdated; reassigning identical templates,
focus styles and direction or remeasuring a valid proxy repeatedly invalidated
layout. Stable proxy bounds now skip adorner invalidation, and reconciliation
updates only changed values. The all-theme/Backstage matrix reaches layout idle.

The final full consumer exposed a separate fixture race in the existing Classic
focus cases: after closing Backstage it immediately cleared the content and
restored the application menu, while the exit animation still owned the old
adorner. A later Backstage could therefore try to focus a row outside the realized
surface. The fixture now pumps the dispatcher until that surface detaches before
replacing it, with a bounded timeout and unchanged focus/activation assertions.
Initial-focus failures include the row's visibility and visual-parent diagnostics.

Cleanup verification stayed touch-specific: six actual Showcase theme cases and
the independent consumer's expanded light/dark/LTR/RTL matrix, including all
seven Backstage designs. Actual Showcase, File-menu, orb and Backstage renders
were inspected. Normal Debug outputs are refreshed. The final Release suite and
remaining live gates are recorded in the active plan; snapshot approvals and
tolerances remain unchanged. No app was manually launched or changes committed.

The next eight screenshot refinements aligned tab-shaped File buttons with the
tab panel's height/padding and allowed modal Close labels to determine width.
Large targets now use 136 DIP to match three 44-DIP command rows including their
margins. Horizontal dropdowns use a 56-DIP minimum, a correctly sized icon wrapper
and more icon/arrow spacing. Navigation fonts return to their ordinary size;
only the touch areas grow. Classic2007 navigation receives extra top clearance.

The shared default in-ribbon panel derives equal touch cells from natural tile
width and the native strip viewport, filling one row's height and width. Compact,
expanded and explicitly sized panels retain WrapPanel layout. Resizing initially
left the old scroll offset in place, cutting the selected theme across two rows.
Cell geometry and density changes now use the existing deferred gallery refresh
to reveal the selected row. Focused checks assert selection visibility after
density changes, popup return and strip resizing, alongside geometry and normal
font sizes. This pass used touch-only tests; the full consumer retry stays deferred
under the user's instruction recorded in the active plan.

Further screenshot feedback exposed the cost of uniform touch File geometry:
Office2013/2019 acquired a leading gutter, while Office2024/Crystal lost their
rounded-body inset and every tab-shaped File target became too narrow. Shared
touch tokens now retain those theme differences, with a 64-DIP minimum width and
wider theme-specific caption padding. The Office2007 application's touch top band
grows to keep its first menu row below the enlarged orb.

Classic2007 Backstage clearance previously moved the navigation background's
outer margin below the content pane, exposing a different-colored shell strip.
The rail returns to the content pane's 36-DIP top edge; 16-DIP inner padding keeps
the first row clear of the orb. Both the actual Showcase surface and the independent
consumer check painted-edge alignment. Separately requested shared changes give
in-ribbon galleries four-DIP horizontal outer spacing and flatten Office2010's
lower QAT in light/dark themes using the body gradient's end color. These defaults
apply in compact and touch modes, retain local/scoped overrides, and intentionally
affect compact visual scenes. The current plan records focused evidence and the
still-deferred full/native/visual gates; snapshot approvals were not changed.

The next touch pass fixes dropdown/split alignment in QAT overflow by moving
centering from a nested template trigger to the outer style's density default.
The existing overflow proxy's local left alignment now reaches its native content
presenter, while normal ribbon commands stay centered. No proxy/factory API changed.
Message rows use a 52-DIP minimum, 44-DIP action/dismiss targets and 20-DIP icons;
message/action fonts, command bindings and dismissal behavior are retained.

The shared window template reserves a 46-DIP title band and native caption area
when retained title-bar content has touch density. Backstage hides that content's
presenter without removing its density, so the title and caption buttons no longer
shrink during the transition. Compact density or moving the QAT away from the title
clears the reservation. The six Showcase cases and RibbonKit-only touch consumer
pass in normal Debug and Release, including the all-theme/light-dark/LTR-RTL and
seven-design Backstage matrix. Actual message/title and overflow renders were
inspected; the active plan retains full/native retries and live appearance gates.

The caption styles still fixed their buttons at 34 DIP despite the enlarged title
band. Four setters in the existing touch-title trigger now give minimize,
maximize/restore and close the same 46-DIP height as that band, including their
hover surfaces. A single Debug consumer check with shared resources verifies
painted height, hidden title content, compact return and moving the QAT out of the
title bar. Broader testing remains deferred under the user's minimal-test request.

Office2010 Aero's side bevel still began at the compact 69-DIP title/tab offset,
leaving two short rules protruding into the taller touch tab row. The owning ribbon
now supplies the rendered header bottom in frame coordinates during its existing
layout callback. Private window/template plumbing uses that edge only for
Office2010 Aero touch geometry, preserves the palette's horizontal/bottom margins,
and resumes the original token on compact return or owner detachment. Equal values
do not invalidate layout repeatedly; a secondary ribbon below the main header
cannot push the bevel into document content. No public API changed. One focused
Debug consumer test with RibbonKit resources covers all QAT placements, larger
header text, RTL, scoped margins and compact restoration; actual renders were
inspected and normal Debug output refreshed.

Full Release validation on 2026-10-07 resumed the previously deferred consumer
gate. All 974 tests passed (490 runtime, 482 Writer, the full independent consumer,
and the visual aggregate covering 113 scenes), with no failures/skips. The consumer
ran in its default scope and completed in 2m56s. Both runtime targets built without
warnings/errors; package validation also passed for clean net8/net9 WPF consumers.

The initial run exposed two stale structural/paint contracts: one assumed a single
WindowChrome despite the new touch-caption branch, and another assumed Office2010's
QAT retained a gradient. The updated contracts verify both caption configurations'
shared native settings and the flat QAT's match to the body ramp's end color in
light/dark palettes. The full runtime retry passed. A deterministic capture of all
113 visual scenes isolated the sole comparison failure to that intentional flat
QAT strip in `office2010-message-bar-connected-100`; its actual/diff images were
inspected before updating only that approved PNG. The final aggregate passed with
unchanged comparison tolerances and no product-code edits during validation.

A later touch recording exposed two adaptive-layout pitfalls. A width probe can
expand a collapsed group while its flyout is open; waiting for Popup.Closed leaves
the normal host empty during that synchronous probe, caching a false expanded
width. RibbonGroup now reclaims the flyout content before measuring, with shared
nested-popup cleanup and preserved focus-return bookkeeping. Separately, resetting
the scroller's reported width on every measure loses overflow when WPF short-circuits
the child's unchanged measure. The last report now survives for the same child;
empty panels report zero and replacing the child clears the report.

The flyout regression failed in all six themes before the fix; an independent
cached-measure case also reproduced the missing arrows. Focused coverage includes
both initial densities, repeated switches from open flyouts, actual Showcase
serialized-layout/touch initialization, non-resizable overflow and empty/replaced
content. A clean Release solution build and 64 focused Release checks passed.
The RibbonKit-only touch consumer and rendered startup/overflow evidence are
tracked in the active touch plan. The earlier 974-test full checkpoint predates
this correction; no public API, theme metrics or visual approval changed.

The later Office2024 lower-QAT seam correction clips its upward blur in a parent
visual, after the child's DropShadowEffect renders. Clipping the effect-bearing
border alone would occur before that effect. A matching boolean token enables
the clip for expanded, flush Office2024 drawers while keeping the outer halo,
minimized floating shadow, scoped top insets and other palettes' existing paint.
One shared-resource Debug regression covers both densities and light/dark paint,
retained outer/minimized shadows and Crystal/Office2010 isolation; actual touch
renders were inspected. No public API, snapshot approval or full-suite retry changed.

The density-animation follow-up appends `RibbonAnimationAction.DensityChange = 15`
to the unshipped API, retaining every shipped action value. A ribbon queues one
transition after inherited geometry invalidation and deferred consumer-host layout,
then completes the window/adaptive layout before a gentle 90%-to-rest opacity settle.
Subtle uses 160 ms; Expressive uses the shared 1.4 timing and 1.8 travel multipliers.
Only an expanded below-ribbon QAT translates (four DIP at Subtle), using a private
layer that restores its existing transform and bindings. An open QAT popup/context
menu suppresses that glide so its native anchor stays at final layout geometry.

The first-render gate matters: hosts such as Showcase restore preferences in
`Loaded`, after `IsLoaded` becomes true. Those startup changes must still snap.
Pending/current transitions are replaced on live toggles; a generation check guards
layout reentrancy. Unload, root-template replacement, motion-policy/system changes,
theme/QAT moves and minimize release the transition. Replacing the root template
also reacquires the nested tab/QAT/body hosts instead of retaining detached visuals.
Opacity animation does not overwrite its base/binding; completion clears the clock.

The static Office2024 shadow fixture now disables only density motion while comparing
its two raster samples, then restores the prior action override. Comparing samples
from different opacity frames caused a two-channel-step mismatch; its tolerance,
clip geometry and approvals are unchanged. An independent RibbonKit-only consumer
keeps motion enabled for startup/completion/interruption and adaptive/popup coverage
across six themes and every QAT placement. Focused results and remaining live gates
are recorded in the [active touch plan](../14-TOUCH-DENSITY-PLAN.md#live-density-transition--2026-10-07).
The full historical 974-test checkpoint precedes this animation; no new full-suite
or native keyboard/input gate is claimed.

The 2026-10-08 recording review found the initial 90% fast ease-out too faint beside
the QAT glide. The approved refinement deepens the settle to the theme-switch's 85%
opacity and gives only DensityChange a frozen cubic EaseInOut curve at both levels.
Durations, travel, final geometry and lifecycle handling remain as before. The
focused easing/policy test and live RibbonKit-only transition consumer passed in
Release, including a stronger early-opacity observation and completion/interruption.
Normal Debug Showcase output is refreshed for renewed visual review; no full-suite
or native-input retry ran.

The next Touch screenshot exposed the dialog launcher's 44-DIP height enlarging
its group's caption band relative to adjacent groups. Removing only that shared
template height setter retains the normal 14-DIP launcher height while keeping
its 44-DIP Touch width and mirrored spacer. No API or theme token changes.
The independent launcher consumer checks matching neighboring caption bands and
baselines, DPI-rounded dimensions, density reversals and visibility across all
six themes, light/dark modes and both text directions. That Release aggregate and
the broader Touch consumer passed; an Office2007 render was inspected. Normal
Release solution and Debug Showcase builds have zero warnings/errors. The active
touch plan records the evidence and separate user review; no full-suite, snapshot
approval or native-input retry ran.
