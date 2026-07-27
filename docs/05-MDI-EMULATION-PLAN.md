# MDI Emulation Plan

An in-window Multiple Document Interface for RibbonKit: themed "child windows"
that float, resize, cascade/tile, minimize, and maximize inside the client area
of a host — plus a switchable tabbed-documents view — with the active child
merging its caption controls into the ribbon the way classic Office/WinForms MDI
did. Consumers inject arbitrary content (a `UserControl`, or a view-model +
`DataTemplate`) and RibbonKit wraps it in chrome that tracks the current theme.

WPF has no native MDI (it left with WinForms' `Form.IsMdiContainer`), so this is
an emulation over an in-visual-tree host, not a wrapper over OS child windows.

> **Status:** design only. This is a *post-v1 power feature* — it depends on the
> tab-merging API (roadmap Phase 7), `RibbonState` persistence (Phase 5), the
> token theme layer, and `RibbonWindow`'s `WindowChrome`/DPI work already in
> the plan. Nothing here should land before those subsystems exist.

## 0. Scope decisions (locked)

These four were decided up front because each one roughly doubles or halves the
work of a subsystem, and they set the shape of everything below.

| Decision | Choice | Consequence |
| --- | --- | --- |
| Document paradigm | **Classic MDI + a switchable tabbed-documents mode** | One document model (`MdiDocument`), two presenters over it — a free-floating `Canvas` host and a tab host. Both bind the same items collection. |
| Maximized child | **Merge caption into the ribbon** (authentic Office MDI) | The hard part. When a child is maximized, its system icon goes to the left of the ribbon and its minimize/restore/close buttons dock at the right end of the tab-strip row. Requires a contract between the MDI host and the `Ribbon`. |
| Injection contract | **Support both** — direct instances *and* MVVM `ItemsSource` + `DataTemplate` | Matches the house pattern ("all items containers support `ItemsSource` + templates"). Imperative `AddDocument(control)` sits on top of the same `MdiDocument` collection the binding fills. |
| Layout persistence | **Yes — save/restore** | Child positions, sizes, window state, z-order and active document serialize through the existing `RibbonState` (JSON) mechanism, not a new one. |

### Why MDI at all (recorded so we don't relitigate it)

MDI is a dated paradigm; modern Office-style apps mostly moved to tabbed
documents or docking. We are building it deliberately for the *authentic
retro-Office feel*, and we hedge by shipping the tabbed-documents mode over the
same document model — so an app that outgrows the floating-window metaphor flips
a property instead of rewriting. Docking (AvalonDock-style split panes) is
explicitly **out of scope**; if it's ever wanted it's a third presenter, not a
change to the model.

## 1. Where it lives

```
src/RibbonKit/
├── Controls/
│   ├── MdiContainer.cs        # the host: owns the document collection + active doc
│   ├── MdiChild.cs            # one themed child-window chrome (Control, re-templatable)
│   ├── MdiDocument.cs         # the model: content + title + icon + placement + state
│   └── MdiTabStrip.cs         # tabbed-documents presenter (thin; may reuse RibbonTabControl visuals)
├── Layout/
│   └── MdiCanvasPanel.cs      # placement/z-order panel behind MdiContainer
├── State/
│   └── MdiLayoutState.cs      # (de)serializes placement into RibbonState JSON
└── Themes/
    ├── Office2024.xaml        # + MdiChild / MdiContainer default templates
    └── Tokens.Office*.xaml    # + MdiChild.* token family per theme
```

Naming and conventions follow the existing controls exactly: `Control`-derived,
`DefaultStyleKeyProperty.OverrideMetadata` in a static ctor, `[TemplatePart]`
attributes with `PART_`-prefixed names, tokens referenced via `DynamicResource`
so a `ThemeManager.Apply` swap re-colors the chrome instantly with no per-theme
template duplication.

## 2. The three-part architecture

### 2.1 Model — `MdiDocument`

A lightweight, bindable object (not a visual). It carries everything both
presenters and the serializer need:

- `Content` (object — a `UserControl`, any `FrameworkElement`, or a view-model
  resolved by `ContentTemplate`/`ContentTemplateSelector`)
- `Title`, `Icon` (mirrors `RibbonControl`'s `Header`/`Icon` conventions)
- `Placement`: `Rect` (DIPs, relative to the container) + `WindowState`
  (Normal/Minimized/Maximized) + z-index
- `IsActive`, `CanClose`, `IsModified` (for the `*` dirty marker + close prompt)
- optional `RibbonMergeSource` — the tabs/groups this document contributes to
  the host ribbon when it is the active document (see §4)

`MdiContainer.Documents` is an `ObservableCollection<MdiDocument>` exposed as
`ItemsSource` for the MVVM path; `AddDocument(FrameworkElement, title)` and
`AddDocument(MdiDocument)` are the imperative path and simply add to the same
collection. This is the "support both" contract.

### 2.2 Host — `MdiContainer`

An `ItemsControl`-style host whose `ItemsPanel` is `MdiCanvasPanel` in floating
mode. Responsibilities:

- **Placement & z-order** via `Canvas.Left/Top` + `Panel.ZIndex`; activation
  brings a child to front and flips `IsActive`.
- **Arrange commands**: `CascadeCommand`, `TileHorizontalCommand`,
  `TileVerticalCommand`, `ArrangeIconsCommand` (lay out the minimized strip).
  These are `RoutedUICommand`s so a ribbon button binds straight to them — the
  natural "Window" menu/group.
- **Active-document tracking**: exactly one active child; drives caption
  highlight, ribbon merge, and which document the tabbed view shows selected.
- **New-child placement policy**: cascade offset from the last child, clamped
  to the visible client area (reuse the work-area clamping thinking from
  `RibbonWindow.UpdateMaximizeInset`, but against the container bounds, not the
  monitor).
- **Mode switch**: `DocumentMode = Mdi | Tabbed` swaps the presenter without
  touching `Documents`.

### 2.3 Child chrome — `MdiChild`

A `Control` with a fully re-templatable chrome. This is where the visual work
concentrates. Template parts:

- `PART_TitleBar` — drag-move (thumb-based); double-click toggles maximize;
  hosts the icon, title, and the child's min/restore/close buttons.
- `PART_ResizeGrid` — eight `Thumb`s (edges + corners) for resize; disabled
  when maximized or minimized. Enforce `MinWidth/MinHeight`.
- `PART_ContentHost` — a `ContentPresenter` bound to `MdiDocument.Content`.

It mirrors the patterns already proven in `RibbonWindow`: caption buttons bind
the same `SystemCommands`-style verbs (adapted to per-child state rather than
`Window`), and the active/inactive caption look is a template trigger on
`IsActive`. Because it's `DynamicResource`-driven, a maximized child that has
merged into the ribbon simply hides its own `PART_TitleBar` buttons via a
trigger — no code path duplicates the buttons.

## 3. Theming — reuse first, add a small token family

The chrome should theme "for free" by leaning on tokens that already exist, and
add only what's genuinely MDI-specific. Reusable today:

- `RibbonKit.Brushes.CaptionButton.HoverBackground` / `PressedBackground`,
  `CaptionButton.CloseHoverBackground` / `ClosePressedBackground` — the child's
  min/restore/close buttons.
- `RibbonKit.Brushes.TitleBar.Background` / `Foreground` — inactive child caption.
- `RibbonKit.Brushes.Ribbon.Border`, `...Metrics.ControlCornerRadius`,
  `...Effects.ContentShadow` — child border, corners, and the drop shadow that
  lifts the active child.
- `RibbonKit.Brushes.Accent` — active-caption accent (matches how 2019/2013 tint
  their bands).

New token family (add to every `Tokens.Office*.xaml`, one value each):

```
RibbonKit.Brushes.MdiChild.ActiveCaptionBackground
RibbonKit.Brushes.MdiChild.ActiveCaptionForeground
RibbonKit.Brushes.MdiChild.InactiveCaptionBackground   (may alias TitleBar.Background)
RibbonKit.Brushes.MdiChild.InactiveCaptionForeground
RibbonKit.Brushes.MdiClient.Background                  (the MDI area behind the children)
RibbonKit.Metrics.MdiChild.CaptionHeight
RibbonKit.Metrics.MdiChild.ResizeBorderThickness
RibbonKit.Metrics.MdiChild.CornerRadius                 (0 for flat 2013/2019, rounded for 2024)
```

Per-generation flavor falls out of the existing token switches: 2010 gets its
gradient/glass caption and gold hover, 2013/2019 go flat with square corners,
2024 gets rounded corners and the soft shadow — all by giving these keys
different values in each token dictionary, exactly like the rest of the suite.

## 4. The hard part — maximized child merges into the ribbon

Classic MDI, when a child is maximized, does two distinct things that people
conflate:

1. **Caption merge** — the child's system icon appears at the far left of the
   menu/ribbon row and its minimize/restore/close buttons dock at the far right
   of that same row. The child's own title bar disappears.
2. **Tab/command merge** — the active document's ribbon tabs and groups appear
   in the host ribbon. This is *already* RibbonKit's tab-merging feature
   (`RibbonMergeSource` + `Merge()/Unmerge()`), which the architecture doc
   already names "MDI child" as the motivating case for.

Keeping these separate is the key design move:

- **Tab/command merge** is not MDI-specific and must not be reimplemented here.
  `MdiContainer` calls `ribbon.Merge(activeDocument.MergeSource)` on activation
  and `Unmerge` on deactivation. Wiring only; the mechanism is Phase 7's.
- **Caption merge** *is* MDI-specific and needs a small, explicit contract on
  the ribbon so the MDI host can inject the maximized child's icon + caption
  buttons without the MDI code reaching into ribbon internals:

  ```csharp
  // on Ribbon (or a RibbonCaptionMergeSite attached to it)
  void ShowMergedCaption(object icon, MdiCaptionButtons buttons);
  void ClearMergedCaption();
  ```

  The ribbon already owns a title-bar region and caption-button visuals
  (`RibbonWindow` + the tab-strip row), so the merge site is a placement target,
  not new chrome. The MDI host drives it purely from active-document state.

**Modal tabs interaction:** a maximized MDI child that owns a modal tab
(Print-Preview-style) composes cleanly — modal-tab scope (Phase 7's
`RibbonModalScope`) hides sibling tabs while the merged caption stays. Worth an
explicit test; no new mechanism.

This section is the schedule risk. If Phase 7's tab-merging or modal-tab APIs
aren't final, build MDI with **"maximize fills the client area, caption stays on
the child"** first (10% of the effort, covers most real use), and add caption
merge as a fast-follow once the merge site exists.

## 5. Persistence — ride on `RibbonState`

No new storage. `MdiLayoutState` contributes an object to the existing
`RibbonState` JSON: an ordered list of `{ documentKey, rect, windowState,
zIndex, isActive }`. The consumer supplies a stable `documentKey` per document
(they own document identity — RibbonKit can't guess it), and on restore the host
re-applies placement to matching documents and silently drops entries whose
document no longer exists. `DocumentMode` and the minimized-strip arrangement
persist alongside. This reuses the serializer, the storage-location choice, and
the versioning story already designed for QAT/customization state.

## 6. Airspace, DPI, and other sharp edges

- **Airspace** — injected `WebView2`, D3DImage, or hosted WinForms content won't
  clip under a floating child or z-order correctly (classic WPF airspace). Not
  solvable in general; **document the limitation** and offer the tabbed mode
  (no overlap) as the escape hatch for such content. Detect and warn in a debug
  build if a known airspace-breaking child is floated.
- **DPI** — placement is stored in DIPs so it's resolution-independent; verify
  per-monitor v2 moves (a child dragged toward a different-DPI monitor stays
  correct) and re-clamp on `OnDpiChanged`, mirroring `RibbonWindow`.
- **Focus/activation** — clicking anywhere in a child activates it; keyboard
  `Ctrl+Tab`/`Ctrl+F6` cycles documents (Office convention). One active child;
  restore focus into the child's content on activation.
- **Performance** — virtualize the tabbed presenter; the floating host won't
  virtualize (children have positions) so cap-and-warn on very large document
  counts rather than pretending to scale to hundreds of floating windows.
- **Design-time** — guard live services with `DesignerProperties.GetIsInDesignMode`
  and give `MdiContainer`/`MdiChild` sensible default content so the XAML
  designer previews per theme, consistent with the section-9 design-time story.

## 7. Phased build (slots after the dependencies exist)

Each phase ends with a showcase page and tests green, matching the house rhythm.

- **M0 — Model + floating host.** `MdiDocument`, `MdiContainer`, `MdiCanvasPanel`,
  `MdiChild` chrome with drag/resize/activate/close and normal-state placement.
  One theme (2024). *Exit:* open/move/resize/close several injected `UserControl`s.
- **M1 — Window management.** Minimize (to bottom strip), maximize-fills-client,
  cascade/tile/arrange commands, `Ctrl+Tab` cycling, min/max sizes. *Exit:* the
  "Window" ribbon group drives everything; keyboard cycles documents.
- **M2 — Theming + MVVM.** `MdiChild.*` token family across all shipped themes;
  `ItemsSource` + `DataTemplate` injection path proven alongside the imperative
  one. *Exit:* theme switch re-skins children live; MVVM demo in showcase.
- **M3 — Tabbed mode + persistence.** `DocumentMode` switch with `MdiTabStrip`;
  `MdiLayoutState` through `RibbonState`. *Exit:* flip Mdi⇄Tabbed with no data
  loss; layout survives app restart.
- **M4 — Caption merge (needs Phase 7).** Wire `Ribbon.Merge` for tabs and the
  caption-merge site for the maximized child's icon + buttons; modal-tab
  interaction test. *Exit:* maximizing a child reproduces authentic Office MDI —
  child caption in the ribbon row, its tabs merged.

## 8. Open questions to settle before M0

- **Container ⇄ ribbon coupling:** does `MdiContainer` require a `RibbonWindow`
  ancestor, or work under a plain `Window` (with caption merge simply disabled)?
  Recommend the latter — graceful degradation, same as the ribbon's own
  `RibbonWindow`-optional stance.
- **Merge-site owner:** does the caption-merge target live on `Ribbon`,
  `RibbonWindow`, or a separate attached `RibbonCaptionMergeSite`? Leaning
  attached, so a non-ribbon host could still receive a merged caption.
- **Document identity for persistence:** confirm the consumer-supplied
  `documentKey` contract (versus RibbonKit generating one) — needed before
  `MdiLayoutState`.
