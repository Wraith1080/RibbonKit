# Library history: Designer and context menus

> Archived implementation evidence. Dates, test counts and pending work describe
> their original checkpoints; later records may supersede them.

[History directory](../01-library.md) · [Current status](../../../04-DESIGN-NOTES.md#5-current-state--next-steps) · [Working agreement](../../../AGENTS.md).

### 3.22 Design-time smart tags / quick actions (XAML designer) — VERIFIED IN VS

The modern WPF designer integration uses a separate net472 DesignTools assembly,
identifies runtime controls by type-name strings and edits through the Visual
Studio Model API. Context-menu verbs were the proven delivery surface; the
custom adorner spike activated but did not paint in the isolated design surface.
The structure editor and working design-only preview followed in [§3.23](02-designer-and-context-menus.md#323-ribbon-editor-dialog-design-time--tab-preview-feasibility).

Keep these API pitfalls when maintaining the tooling:

- The two-argument TypeIdentifier constructor takes the XAML namespace, not the
  CLR namespace. A wrong identifier made CreateItem fail without a visible edit.
- Designer providers can swallow exceptions. Diagnostic logging is needed to
  distinguish an inert command from failed model access.
- Add children through explicit Tabs/Groups/Items collection properties.
  Enum names are strings resolved by the property's type converter.
- ModelProperty.Value indicates a local assignment; ComputedValue gives the
  effective value. UpdateItemStatus controls verb availability and checked state.
- The modern toolbox uses tools/VisualStudioToolsManifest.xml in the package;
  project-reference discovery is a different path.

The verified verbs cover creation, reorder and deletion of tabs, groups and
commands, Backstage/nav items, and QAT placement. Each edit has one undo scope.

### 3.23 Ribbon Editor dialog (design-time) + tab-preview feasibility

The code-built RibbonEditorWindow opens modally from a designer verb and edits
the ModelItem tree directly, with one undo scope per operation. The editor walks
Tabs, Groups, Items and panel Children, supports nested stacks and type-specific
properties, and keeps arbitrary rich Content out of the structural tree.

Design-only tab and Backstage/page preview uses DesignModeValueProvider plus
ValueTranslationService.InvalidateProperty. The provider is evaluated lazily;
registration alone did not refresh the surface. Writing SelectedIndex through
the ordinary model property serializes runtime XAML, and the API has no literal
design-namespace write path. Preview choices instead live in session state.
Inherited Backstage.SelectedIndex is registered/invalidated under both Backstage
and Selector. User verification confirmed surface changes without XAML/runtime
changes.

Model access lessons retained from the failed spikes:

- Properties[name] throws for an absent property. FindProperty must catch that
  case rather than expecting null; header-less controls still support Move/Delete.
- A scalar can be wrapped in a ModelItem. Determine scalar content from the
  ComputedValue type, not from Value being non-null. Caption editing selects
  Header, scalar Content, or a gallery item's Tag.
- Attached CommandId and KeyTip.Keys need a type-qualified PropertyIdentifier.
  They share the attached-property resolver; blank values clear the assignment.
- A raw StaticResourceExtension lost its ResourceKey during serialization.
  Create a markup-extension ModelItem, set its ResourceKey in the model, then
  assign that item. Windows read/write tests confirmed the resulting reference.

IconPickerDialog lists keys already used by the ribbon and can load Icons.xaml
for vector thumbnails. Conservative automatic discovery uses the current VS
process's DTE context and searches the document, project and solution locations;
ambiguous/missing files or parse failures retain the manual browser. Build/cache
folders and reparse points are skipped; the catalog is cached per design session.
The color picker writes converter-compatible brush text and tolerates bad input.

Diagnostics report construction/model failures instead of letting the designer
silently discard them. The editor, preview, icon references and mixed control
walks were user-verified on Windows.

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
