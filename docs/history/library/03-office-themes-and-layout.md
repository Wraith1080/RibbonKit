# Library history: Office themes and layout

> Archived implementation evidence. Dates, test counts and pending work describe
> their original checkpoints; later records may supersede them.

[History directory](../01-library.md) · [Current status](../../../04-DESIGN-NOTES.md#5-current-state--next-steps) · [Working agreement](../../../AGENTS.md).

### 3.27 Office 2010 ("Blue") theme — the first gradient theme

Office 2010 introduced generation-specific gradients through the shared
DynamicResource token layer.

- Reserve border thickness at rest and change brushes on hover/press. Changing
  thickness in a trigger caused one-DIP content jitter and misleading tab reflow.
- Title-bar gradient endpoints match the tab strip in light/Black palettes.
  Released hover/checked glass has a narrow bright lower stop; pressed states
  use recessed three-stop ramps without that glow.
- File's rest/hover/open surfaces use smooth ramps with a separate faint radial
  inner rim; press hides that rim. Custom accent derivation uses matching
  ApplicationButtonGel/PressedGel profiles rather than a generic solid brush.
- Glass reaches dropdown/split halves, collapsed groups, gallery buttons,
  menu/combo rows and shared dialog actions through common tokens and templates.
- Classic2010 navigation has a square selected row with concentrated radial
  glass and a clipped left-side content shadow. Content/text and shadow metrics
  remain theme-scoped. The early page-foreground issue is recorded in
  [§3.28](03-office-themes-and-layout.md#328-backstage-page-text-colour--ribbon-focus-richtextbox--2026-07-21); the working connected-tab notch replaces the failed negative-margin
  attempts in [§3.29](03-office-themes-and-layout.md#329-connected-tab-body-border-cut-20102013--the-notch--2026-07-23).
- The 2007/2010 Black application menus own their dark surfaces and separate
  menu text tokens; their hybrid ribbons still use dark command text.

The runtime package also gained build-only DesignTools packaging under each
runtime TFM's Design directory, with the toolbox manifest. This describes package
construction, not NuGet.org publication.

Office2010ThemeContractTests and focused button-state/Backstage/menu snapshots
record deterministic glass, token and shared-template evidence. Historical
automated verification is separate from native hover/keyboard and live acceptance.

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

The monolithic Office2024 template dictionary was split by control family;
Office2024.xaml became the aggregator and Generic.xaml continued to load it.
The initially experimental split is now the shared-template architecture recorded
in AGENTS.md.

The important failure was StaticResource scope: a merged dictionary cannot
resolve a resource merely because a sibling is listed earlier in the parent.
A depending part must merge its dependency locally. The original dependencies
were RibbonChrome → Shared and Customize → OptionsDialog.

Binding.Converter is a CLR property, so DynamicResource cannot replace its
StaticResource reference. Keep keys before use, BasedOn chains within their
resolvable scope, every part in the aggregator, and implicit TargetType styles
unique. These errors can appear only when a template is realized.

ThemeDictionaryScopeTests guards missing local dependencies, forward references,
unregistered parts, duplicate implicit styles and cross-file BasedOn references.
Mutation checks demonstrated each failure; commented references were ignored.
The 2026-07-27 laptop review found no designer/editor slowdown.
