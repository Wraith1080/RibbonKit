# RibbonKit Writer — Functional Reference Application Plan

> **Status:** approved post-v1 application plan. W0-A through W0-D, W1-A through W1-D and W2-A through
> W2-E are accepted on the available hardware. W0-E/W0-F now own format-aware document profiles and
> the pictured Backstage New surface; W0-E is next and W2-F waits for W0-F. Live mixed-monitor DPI
> movement remains deferred to W4-C. No later implementation is implied.
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
- Editable page-by-page WYSIWYG layout. WPF `RichTextBox` remains a continuous editing surface;
  pagination is a preview and print concern.
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

- Insert grid picker, initially 1×1 through 8×8.
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
- Font: family/size combos, bold/italic/underline, text colour and highlight
- Paragraph: alignment, lists, indent and spacing
- Styles: a small live-preview gallery for Normal, Title and headings
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
- **Picture Tools** only after image selection, sizing and removal are reliable. Do not expose dead
  crop/correction commands merely to imitate Word.

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

### W2 — Native format and paper model

- Versioned `.rkw` round-trip.
- Page settings with A4/Letter/Legal presets, orientation and margins.
- Centred paper edit surface, paginated preview and printing.
- Custom-margin UI, non-printing margin guides and a zoom/DPI-aware horizontal ruler.

### W3 — Structured content

- Images and hyperlinks.
- Table insertion and contextual Table Tools editing.
- Native-format round-trips and RTF compatibility fixtures.

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
- Preview clone isolation and deterministic pagination inputs.
- Separation of ribbon customization, document settings and application appearance.

Manual Windows acceptance should include:

- Create, format, save, close and reopen a multi-page native document containing an image and table.
- Export representative documents to RTF and text with clearly communicated fidelity changes.
- Print preview and physical/PDF printing for A4 and Letter with normal and custom margins.
- Margin-guide/ruler alignment while zooming and horizontally scrolling, including drag cancellation,
  high contrast and focused RTL content.
- Keyboard-only editing and complete KeyTip traversal, including contextual Table Tools.
- 100/125/150/175/200% DPI, live monitor changes, light/dark themes and focused RTL text/table input.
- Narrow-window ribbon reduction, minimized ribbon, QAT customization and Backstage document commands.
- Before/after review of the actual Writer window: icon metaphors and stroke weight, primary/secondary
  command hierarchy, group balance, colour/highlight state cues, disabled contrast, QAT legibility and
  status-bar spacing must be visually approved rather than inferred from application startup.

The project is complete only when it is comfortable to use for an ordinary multi-page note or letter,
not merely when every ribbon button has a handler.
