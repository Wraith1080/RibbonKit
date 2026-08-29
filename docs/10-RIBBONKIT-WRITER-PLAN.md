# RibbonKit Writer — Functional Reference Application Plan

> **Status:** approved post-v1 application plan. W0-A through W0-F, W1-A through W1-D, W2-A through W2-F,
> and W3-A through W3-C are accepted on the available hardware, including the 2026-08-26 W3-C corrective UI pass
> and its 2026-08-27 full regression/live visual reacceptance. W1-E Home formatting implementation and its automated
> Debug gate are complete; the corrective app-owned RibbonKit-themed Font/Color/Paragraph pass has passed its refreshed
> full Debug gate and awaits live visual reacceptance.
> W3-D structured-content round-trip and TXT/RTF compatibility fixtures are accepted through design-notes §3.120.
> W3-E has begun: the bounded W3-E1 foundation now supplies document-bound structured-object context menus and
> suppresses transient contextual-tab publication during app-owned table mutations. Its app-owned picture-removal
> bridge also preserves context-menu and Delete/Backspace undo/redo for images opened from native Recent documents,
> including preview and save/reopen.
> The bounded W3-E2a picture slice now adds explicit document-bound picture selection, a real size/remove-only
> Picture Tools tab and a non-printing eight-handle resize adorner with one-unit Undo/Redo. Its automated gate is
> accepted. The bounded W3-E2b implementation now adds a non-printing table selection adorner with column, row and
> overall resize grips plus one-unit Undo; its minimal focused gate passes. Live table-grip acceptance and the full
> W3-E matrix remain pending. W2-G is a planned high-risk true editable-pagination packet after
> W3-E; it must prove a real editing architecture and may not simulate pages with decorative breaks. Live mixed-monitor
> DPI movement remains deferred to W4-C. No later
> implementation is implied.
> Live status remains in `04-DESIGN-NOTES.md` §5.
> RibbonKit Writer is a separate functional sample, not another feature page inside
> `RibbonKit.Showcase`.
> The durable packet/dependency split for later Luna execution is in
> [`11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md`](11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md).

## 1. Purpose

RibbonKit Writer is a small but genuinely usable rich-text editor built as a complex consumer of
RibbonKit. It should demonstrate how an application coordinates document state, selection-sensitive
commands, Backstage, QAT, contextual tabs, customization, themes, DPI and accessibility over a long-
lived editing surface.

The existing Showcase remains the exhaustive component laboratory. Writer should use RibbonKit
features only where they make sense in a coherent product. It is not required to expose every theme,
File-surface variant, merge/modal scenario or diagnostic toggle in its primary interface.

Recommended project location and identity:

- Project: `samples/RibbonKit.Writer/RibbonKit.Writer.csproj`
- Product name: **RibbonKit Writer**
- Initial target: `net8.0-windows`
- Library consumption during source development: `ProjectReference` to `src/RibbonKit`
- Host responsibilities: PerMonitorV2 manifest, app-owned icons, appearance settings and document IO

## 2. Product boundary

Writer is a practical replacement for a lightweight formatted-text editor, not a Word clone or a
desktop-publishing engine.

### In scope

- `.txt` and `.rtf` open/save, with a native Writer format for complete fidelity.
- New, Open, Save, Save As, Print, recent files and unsaved-change protection.
- A format-aware New surface for Plain Text, Rich Text and RibbonKit Writer document profiles.
- Clipboard, undo/redo, font family/size, bold/italic/underline, foreground/highlight colour.
- Paragraph alignment, indentation, bullets and numbered lists.
- Find/replace, spelling support, word/character count and zoom.
- Images, hyperlinks and date/time insertion.
- Paper size, orientation and margins, plus a true paginated preview/print path.
- Native FlowDocument tables with insertion and contextual editing tools.
- Backstage, QAT, KeyTips, ScreenTips, ribbon minimization and customization persistence.
- App-owned theme, title-bar/backdrop and other appearance persistence kept separate from structural
  ribbon customization.

### Explicitly out of scope

- OLE/COM compound-document embedding or in-place activation.
- `.doc` or `.docx` compatibility.
- Macros, mail merge, tracked changes, comments, collaboration or cloud sync.
- A Word-compatible layout engine, section model, footnotes, citations, headers/footers or automatic
  tables of contents in the first version.
- Word-compatible page-by-page WYSIWYG is not part of the accepted W2-C surface. A bounded W2-G packet now owns
  investigation and delivery of true editable pages after structured-object behavior is stable. It must preserve
  native-quality editing behavior and may not present fake breaks that disagree with pagination.
- Making Writer-specific document concepts part of RibbonKit's runtime public API.

OLE is intentionally excluded even though a FlowDocument can host WPF `UIElement` content. OLE would
require COM storage, server activation, focus/keyboard routing, bitness and registration handling,
security policy, persistence and print integration. Images and hyperlinks cover the useful portable
document cases. A later attachment feature, if wanted, should be an inert Writer-owned file card that
opens explicitly rather than an in-place executable object.

## 3. Document model and formats

Keep document content, document layout and application appearance as separate state.

```text
WriterDocument
├── FlowDocument content
├── DocumentPageSettings
│   ├── paper size and custom dimensions
│   ├── portrait/landscape orientation
│   └── left/top/right/bottom margins
├── file path and format
└── dirty/recovery state
```

`DocumentPageSettings` should use device-independent units internally (96 DIP per inch) and expose
named A4, Letter and Legal presets plus custom dimensions. Apply the settings to the
`FlowDocument.PageWidth`, `PageHeight` and `PagePadding` properties rather than hiding page layout in
custom text attributes.

Format policy:

- **Text (`.txt`)** — plain content only. Formatting, images, tables and page settings cannot round-trip.
- **Rich Text (`.rtf`)** — interoperable formatted content. Treat advanced tables, embedded images and
  page-layout fidelity in other editors as best effort; do not make Writer metadata depend on private
  RTF control words.
- **RibbonKit Writer (`.rkw`)** — versioned ZIP container holding `content.xamlpackage`,
  `document-settings.json` and a small manifest. This is the fidelity format for paper settings,
  tables, images and future additive metadata. Loading must reject unsafe or unknown executable
  content rather than instantiate arbitrary application types.

Treat these formats as document profiles when creating or editing a document, not as content templates.
Plain Text exposes character-only editing and disables commands whose results cannot round-trip. Rich Text
enables the supported font and paragraph formatting surface but keeps Writer-only page metadata and structured
content unavailable. RibbonKit Writer exposes every feature currently implemented by Writer. A later template
catalog may place letter, note or report content presets inside any compatible profile without conflating the
template with its persistence format.

Backstage New should present the three profiles as labelled, keyboard/UIA-accessible cards with an app-owned
icon or small document preview. Creating a card commits that format as the untitled document's initial identity,
so Save preselects its matching extension. Save As may select another supported type; the active identity changes
only after a successful save, and moving to a lower-fidelity type requires a clear loss warning. Ctrl+N and any
one-click New projection should create the configured default profile rather than opening an inert chooser.

The serializer needs atomic save/replace, schema-version checks, corrupt/foreign-package handling and
round-trip tests. Autosave/recovery should use a separate application-data location and must never
silently replace the user's source file.

## 4. Paper-aware editing and pagination

Writer should have three coordinated presentations of the same document.

### Continuous Edit view

- Preserve the current native, workspace-filling `RichTextBox` presentation for distraction-free editing.
- Selection, caret, IME, spell-check and clipboard behaviour stay native to the same live editor.

### Paper view

- A fixed-width white paper surface centred over a neutral workspace.
- Width follows the selected logical paper size and zoom.
- Visible inner spacing follows the selected margins.
- An optional non-printing dotted content-boundary guide may outline the current margin rectangle. It
  is view chrome only: it must not enter the `FlowDocument`, clipboard, undo history, native package or print output.
- The editor remains one continuous scroll surface; it must not draw fake page breaks that disagree
  with the paginator.
- Switching between Continuous Edit and Paper must re-present the same live editor/document without
  replacing content or losing selection, caret, undo history or focus.

This describes the accepted W2-C implementation. Planned W2-G may replace Paper with a genuinely paginated editing
presentation only after an architecture proof preserves cross-page caret/selection, IME, spell-check, clipboard,
undo/redo and structured objects. Independent RichTextBoxes, injected blank blocks and decorative page gaps are not
acceptable substitutes for one authoritative document and paginator-consistent reflow.

### Print Layout / Preview

- Clone or serialize the editable FlowDocument into a preview document; one FlowDocument instance
  cannot be parented by both editor and viewer simultaneously.
- Set `PageWidth`, `PageHeight`, `PagePadding` and single-column policy on the preview document.
- Render through `FlowDocumentPageViewer`/`DocumentPaginator` with single-page and two-page viewing,
  page navigation and zoom.
- Print from the same page settings and paginator used by preview so preview and paper output agree.
- Rebuild preview after document or page-setting changes, but debounce expensive pagination while the
  user is typing.

The Page tab should expose paper size, orientation, margin presets, a validated **Custom Margins…** dialog
and page colour. Custom margins edit all four physical edges with immediate validation and a small preview;
Cancel leaves the document unchanged and Apply commits one `DocumentPageSettings` replacement. The View tab
should expose Continuous Edit, Paper and Print Layout/Preview switching plus one page, two pages, page width,
zoom, **Ruler** and **Margin Guides**.
W2-E moves the ribbon zoom controls out of Home/Editing into View instead of duplicating them; the status-bar
zoom readout/control remains available in every mode. Physical printer imageable-area limits must be reported
or clamped at print time rather than silently changing the logical document margins.

The ruler is a separate W2-F interaction packet after Page/View integration. Its first complete form is a
horizontal Paper-view ruler aligned to the same page, zoom and horizontal scroll geometry as the editor. It
shows calibrated ticks, shaded margin regions and first-line, hanging, left and right paragraph-indent markers.
Dragging a page-margin edge previews the change without repeatedly dirtying the document, commits one validated
page-setting update on release and rolls back on Escape or capture loss. Paragraph-indent drags use the native
editing undo path and preserve mixed-selection semantics. Ruler and guide visibility are app/view preferences,
not document content; W4-A may persist them with other Writer appearance settings.

The dotted guide and ruler are hidden from preview and print, remain physically centred for RTL text, use
theme/high-contrast resources, scale without pixel assumptions and do not intercept ordinary editor hit testing.
The first ruler does not promise a vertical ruler, mirrored/gutter margins or editable tab stops. Tab stops remain
deferred until Writer has a tested text-model and persistence contract rather than a decorative marker with no
effect. Reuse the prepared `Margins`, `Ruler` and `Gridlines` icon family where semantics remain clear; add a
dedicated margin-guide glyph only if live comparison shows `Gridlines` is misleading.

## 5. Table support

Tables are native FlowDocument structure:

```text
Table → TableRowGroup → TableRow → TableCell → Block content
```

The first complete table slice should include:

- Compact 1×1 through 3×8 quick grid picker plus a separated Custom Table action for validated 1×1 through 8×8 entry.
- Sensible default column widths, cell padding and theme-neutral borders.
- Tab/Shift+Tab cell navigation; Tab from the final cell adds a row.
- Insert row above/below and column left/right.
- Delete row, column or whole table.
- Merge selected rectangular cells and split a spanned cell using `RowSpan`/`ColumnSpan`.
- Cell horizontal/vertical alignment, background, border and padding commands.
- Distribute rows/columns and an optional first-row/header style.
- Pagination checks for tables that cross a page boundary.

When the caret or selection is inside a table, show a contextual **Table Tools** tab. Its **Layout**
groups should contain rows/columns, merge/split, cell size and alignment. A small **Design** group can
provide border/background presets without growing into a full table-style engine.

Table commands need a tested helper that resolves the current `TableCell`, row, row group and table
from a `TextPointer`. Structural edits must preserve a valid FlowDocument tree and restore the caret to
a predictable cell. `.rkw` is the authoritative fidelity format; `.rtf` table interchange is best
effort and should have focused compatibility fixtures rather than broad promises.

## 6. Ribbon information architecture

### Backstage

- Home/recent documents
- A New page with pictured profile cards for Plain Text, Rich Text and RibbonKit Writer documents
- Open, Save and Save As, with the current profile preselected but every supported type still available
- Print and paginated preview
- Document information and page setup summary
- Settings and Exit

### Settings dialog

The extensible RibbonKit customization/options dialog should be presented to Writer users with the
window caption **Settings**. Keep the built-in **Customize Ribbon** and **Quick Access Toolbar** pages,
and add an app-owned **Appearance** page rather than naming the page itself Settings.

The Appearance page should expose every supported RibbonKit appearance choice that Writer enables:

- Office theme generation: 2024, 2019, 2013, 2010 and 2007.
- The selected generation's light or dark/black palette, default or custom accent colour, and accented
  title-bar choice.
- Backstage design: Modern, Classic, Classic2010, Glass2007 and Classic2007, plus translucency where
  the selected window material and theme support it.
- Window backdrop: None, Mica, Acrylic or Tabbed/Mica Alt when supported by the current Windows build;
  unsupported choices remain visible but explain why they cannot currently be applied.
- Theme-compatible window frame and application-button presentation, including the Office 2007/2010
  Aero-inspired frame options and Tab/Orb shape only where the selected theme supports them.
- Ribbon animation level (None, Subtle or Expressive) and whether RibbonKit respects the Windows
  reduced-motion preference.

Appearance changes use an app-owned transactional preview: Apply/OK validates and persists them, while
Cancel restores the opening snapshot. A dedicated appearance-default action may reset this page, but
Ribbon customization Import/Export/Reset must not modify appearance, document page settings or content.
Changing theme must re-theme the open Settings dialog without losing the selected page, keyboard focus
or pending values. Do not expose dead switches merely to reproduce the Showcase; compatibility-dependent
controls should disable with an explanation and preserve a valid fallback.

### Home

- Clipboard: Paste split button, Cut, Copy, Format Painter if proven useful
- Font: an installed-font picker whose popup previews each family in its own face, remains searchable/editable,
  and retains the current/mixed selection even when it is outside a short recent/recommended section. The size
  picker offers the conventional Office range plus validated arbitrary entry and grow/shrink commands. The Font
  dialog supports underline, single/double strikethrough and mutually exclusive superscript/subscript only where
  state, undo and native-format round-trip are complete.
- Font colour and highlight use split/drop-down behaviour: the primary action reapplies the last colour, while a
  concise keyboard/UIA-accessible popup provides Automatic/No Color, one base swatch per standard colour and recent
  colours without accent/background or light/dark variants. **More Colors…** provides a pointer/keyboard-operable
  saturation/brightness field and hue strip alongside exact Hex/RGB entry. The foreground indicator must remain
  visible on the command icon.
- The Font group dialog launcher opens an app-owned transactional dialog for the character properties Writer
  genuinely supports, including an actual sample preview and honest mixed/unset states. Apply/OK creates one undo
  unit; Cancel leaves the document and history unchanged. Do not imitate unsupported Word effects with dead controls.
- Paragraph: alignment, lists, indent and spacing
- Editor keyboard routing: while the document editor owns focus, Tab/Shift+Tab at a paragraph or list boundary
  performs indent/outdent (including list nesting where valid) instead of transferring focus to another control.
  Literal-tab entry and an accessible, documented way to leave the editor by keyboard must remain explicit; table
  cells use the separate W3-C navigation contract rather than this paragraph path.
- Paragraph should be audited for a matching dialog-launcher path for indents, special first-line/hanging values,
  spacing and line spacing; add it only with complete validation, preview, undo and mixed-selection behaviour.
- Styles: a small live-preview gallery for Normal, Title and headings, only after style application, selection state,
  undo and native persistence are reliable.
- Editor context menu: replace the stock WPF surface with a Writer-owned modern menu whose ordinary-text path owns
  Undo/Redo, Cut/Copy/Paste, Select All, Clear Formatting and the supported Font/Paragraph launchers. Build the menu
  from a context snapshot taken where it opens so moving focus into the popup does not discard the editor target or
  selection. Preserve native spelling suggestions and supported spelling actions, live command enablement, profile
  gating, Shift+F10/the Context Menu key, RTL, High Contrast and keyboard/UIA operation. W1-E owns this base menu and
  its extension seam; W3-E adds table-, picture- and hyperlink-specific rows without duplicating the text commands.
- Editing: Find, Replace and Select

### Insert

- Table grid gallery
- Picture
- Hyperlink
- Date and time

### Page

- Paper size
- Orientation
- Margin presets and a validated **Custom Margins…** dialog
- Page colour

### View

- Continuous Edit/Paper/Print Layout
- One page/two pages/page width
- Zoom controls relocated from Home/Editing; keep the status-bar zoom control globally available
- Ruler and non-printing margin-guide toggles; enabled only in presentations that support them
- Spelling and status-bar visibility

### Contextual tabs

- **Table Tools** when the caret is in a table.
- Contextual identity must remain stable while focus moves from the editor into its ribbon tab or context menu and
  while an app-owned mutation emits transient selection changes. Collapse a contextual tab or choose a fallback tab
  only after the committed result is genuinely outside that object; never keep stale table/picture context after
  document replacement, deletion or undo.
- **Picture Tools** only after image selection, sizing and removal are reliable. Do not expose dead crop/correction
  commands merely to imitate Word.
- W3-E adds non-printing, zoom/scroll/DPI-aware selection adorners: picture edge/corner resize handles and a table
  selection grip plus row/column boundary and overall size grips. Picture corner handles preserve aspect ratio while
  edge handles change one axis; table grips reuse the bounded column-width and row-height-approximation contracts and
  respect content minimums. Dragging previews locally, commits one native undo unit on release and rolls back on Escape
  or capture loss. Ribbon size controls and keyboard/UIA alternatives remain available; handles must not serialize
  into `.rkw`, appear in preview/print or steal the stable structured-object context.

Every persistable command should have a stable `Ribbon.CommandId`; keyboard-relevant commands should
have explicit or reliably derived KeyTips, ScreenTips, standard WPF commands where available and UI
Automation names.

## 7. Architecture principles

- Use WPF commanding for editor operations; centralize selection-to-ribbon state synchronization so
  toggle buttons and combos reflect mixed, set and unset selection states.
- Keep file/document services independent of the window to make round-trip and dirty-state behaviour
  testable without UI automation.
- Keep page settings in the document model, not in ribbon controls or application appearance settings.
- Reuse `RibbonCustomizationSerializer` only for ribbon structure. Use an app-owned versioned settings
  file for theme/backdrop/window preferences.
- Do not add a RibbonKit runtime API to solve an application-only need. If Writer exposes a genuine
  library gap, record it in the [consumer-friction log](12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md)
  and test it separately before proposing an additive API.
- Reuse an app-owned `Icons.xaml`; the deferred stock-icon insertion feature is not a prerequisite.

## 8. Delivery milestones

These product milestones are decomposed into bounded, dependency-gated implementation packets in the
[Luna execution plan](11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md). That decomposition does not imply
that a packet, agent or implementation currently exists.

### W0 — Application shell and document lifetime

- Separate project, theme dictionaries, PerMonitorV2 manifest and RibbonWindow.
- New/Open/Save/Save As for `.txt` and `.rtf`.
- Dirty-state title, unsaved-close prompt, recent files and atomic saves.
- Format-aware document profiles, conversion/loss policy and a pictured Backstage New gallery whose
  selected profile controls supported commands and the default Save extension.

### W1 — Functional rich-text editing

- Clipboard, undo/redo, font and paragraph formatting.
- Selection-state synchronization, Find/Replace, spell-check and status counts.
- Backstage, QAT, KeyTips and ScreenTips.
- A coherent app-owned vector icon set and a first visual-hierarchy/density pass over the realized
  Home ribbon, QAT and status surface before those visuals become the baseline for later tabs.
- A second Home-formatting completion pass covering installed-font preview/search, the full validated size range,
  richer colour/highlight galleries and More Colors, a transactional Font dialog with sample preview, and an audit
  of promised-but-absent Styles, Paste split and paragraph-dialog behaviour without adding placeholder commands.
  This pass also owns capability-aware Tab/Shift+Tab paragraph and list indentation without trapping keyboard focus,
  plus the modern base editor context menu and its context-extension contract.

### W2 — Native format and paper model

- Versioned `.rkw` round-trip.
- Page settings with A4/Letter/Legal presets, orientation and margins.
- Centred paper edit surface, paginated preview and printing.
- Custom-margin UI, non-printing margin guides and a zoom/DPI-aware horizontal ruler.
- A later W2-G true editable-pagination packet, gated behind stable table/picture interaction and an architecture
  proof rather than simulated page breaks.

### W3 — Structured content

- Images and hyperlinks.
- An Insert tab for Table, Picture, Hyperlink and Date and Time, plus contextual Table Tools editing.
- Native-format round-trips and RTF compatibility fixtures.
- A structured-object interaction pass that keeps Table/Picture Tools stable across ribbon focus and transient native
  selection events, adds context-specific table/picture menu actions, and provides direct picture/table selection and
  resizing handles with transactional undo and non-printing adorners.

### W4 — Product integration and polish

- A **Settings**-captioned customization dialog with built-in ribbon/QAT pages and an app-owned
  **Appearance** page.
- Separate persistence for theme generation, dark/black palette, accent/title bar, Backstage design,
  backdrop/frame/application-button presentation and ribbon motion preferences.
- A final consistency pass over W1-D and all icons, command hierarchies and surfaces added by W2/W3.
- Accessibility, keyboard, RTL, DPI and reduced-motion passes.
- Outside-debugger startup, resize, long-document and pagination performance checks.

### W5 — Distribution decision

- Decide after sustained use whether Writer remains a source sample or receives a portable GitHub
  artifact. It must not delay or redefine the RibbonKit runtime release cadence.

## 9. Verification and acceptance

Automated coverage should include:

- `.txt`, `.rtf` and `.rkw` load/save/round-trip and corrupt-input behaviour.
- Profile-specific New, command availability, default-extension selection and successful/failed format transitions.
- Page-preset conversion, orientation swap, margin validation and schema migration.
- Custom-margin commit/cancel, ruler drag rollback, guide geometry and paragraph-indent undo/redo.
- Dirty-state transitions, atomic save failure and unsaved-close decisions.
- Formatting command state over uniform, mixed and empty selections.
- Table insertion, row/column mutation, merge/split and caret resolution.
- Context-menu composition and command state for ordinary text, spelling errors, hyperlinks, tables and pictures,
  including stale-target rejection when the document or selection changes while the menu is open.
- Picture/table adorner geometry and resize transactions across zoom, scroll, view changes, bounds, Escape/capture
  loss, undo/redo and save-close-reopen; preview and print must contain no interaction chrome.
- Preview clone isolation and deterministic pagination inputs.
- Separation of ribbon customization, document settings and application appearance.

Manual Windows acceptance should include:

- Create, format, save, close and reopen a multi-page native document containing an image and table.
- Export representative documents to RTF and text with clearly communicated fidelity changes.
- Print preview and physical/PDF printing for A4 and Letter with normal and custom margins.
- Margin-guide/ruler alignment while zooming and horizontally scrolling, including drag cancellation,
  high contrast and focused RTL content.
- Keyboard-only editing and complete KeyTip traversal, including contextual Table Tools.
- Mouse and keyboard context-menu invocation over text, misspellings, hyperlinks, table cells/borders and pictures;
  each target shows only its valid extra commands without losing the editor selection.
- Direct picture/table selection and resizing at representative zoom/DPI values, including a Table/Picture Tools
  command that retains its contextual page through focus transfer and transient mutation events.
- 100/125/150/175/200% DPI, live monitor changes, light/dark themes and focused RTL text/table input.
- Narrow-window ribbon reduction, minimized ribbon, QAT customization and Backstage document commands.
- Before/after review of the actual Writer window: icon metaphors and stroke weight, primary/secondary
  command hierarchy, group balance, colour/highlight state cues, disabled contrast, QAT legibility and
  status-bar spacing must be visually approved rather than inferred from application startup.

The project is complete only when it is comfortable to use for an ordinary multi-page note or letter,
not merely when every ribbon button has a handler.
