# RibbonKit Writer — Functional Reference Application Plan

> **Status:** approved post-v1 application plan. W0-A through W0-D and W1-A are implemented: scaffold,
> document lifetime, TXT/RTF persistence, atomic saves, recent files, the live Backstage/QAT
> file-command shell, and the formatting command/selection-state engine. No later packet is implied.
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

The serializer needs atomic save/replace, schema-version checks, corrupt/foreign-package handling and
round-trip tests. Autosave/recovery should use a separate application-data location and must never
silently replace the user's source file.

## 4. Paper-aware editing and pagination

Writer should have two coordinated document views.

### Edit view

- A fixed-width white paper surface centred over a neutral workspace.
- Width follows the selected logical paper size and zoom.
- Visible inner spacing follows the selected margins.
- The editor remains one continuous scroll surface; it must not draw fake page breaks that disagree
  with the paginator.
- Selection, caret, IME, spell-check and clipboard behaviour stay native to `RichTextBox`.

### Print Layout / Preview

- Clone or serialize the editable FlowDocument into a preview document; one FlowDocument instance
  cannot be parented by both editor and viewer simultaneously.
- Set `PageWidth`, `PageHeight`, `PagePadding` and single-column policy on the preview document.
- Render through `FlowDocumentPageViewer`/`DocumentPaginator` with single-page and two-page viewing,
  page navigation and zoom.
- Print from the same page settings and paginator used by preview so preview and paper output agree.
- Rebuild preview after document or page-setting changes, but debounce expensive pagination while the
  user is typing.

The Layout tab should expose paper size, orientation, margins and page colour. View should expose Edit,
Print Layout, one page, two pages, page width and zoom actions. Physical printer imageable-area limits
must be reported or clamped at print time rather than silently changing the logical document margins.

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
- New, Open, Save and Save As
- Print and paginated preview
- Document information and page setup summary
- Options/appearance and Exit

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

### Layout

- Paper size
- Orientation
- Margins
- Page colour

### View

- Edit/Print Layout
- One page/two pages/page width
- Zoom
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
  library gap, document and test that gap separately before proposing an additive API.
- Reuse an app-owned `Icons.xaml`; the deferred stock-icon insertion feature is not a prerequisite.

## 8. Delivery milestones

These product milestones are decomposed into bounded, dependency-gated implementation packets in the
[Luna execution plan](11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md). That decomposition does not imply
that a packet, agent or implementation currently exists.

### W0 — Application shell and document lifetime

- Separate project, theme dictionaries, PerMonitorV2 manifest and RibbonWindow.
- New/Open/Save/Save As for `.txt` and `.rtf`.
- Dirty-state title, unsaved-close prompt, recent files and atomic saves.

### W1 — Functional rich-text editing

- Clipboard, undo/redo, font and paragraph formatting.
- Selection-state synchronization, Find/Replace, spell-check and status counts.
- Backstage, QAT, KeyTips and ScreenTips.

### W2 — Native format and paper model

- Versioned `.rkw` round-trip.
- Page settings with A4/Letter/Legal presets, orientation and margins.
- Centred paper edit surface, paginated preview and printing.

### W3 — Structured content

- Images and hyperlinks.
- Table insertion and contextual Table Tools editing.
- Native-format round-trips and RTF compatibility fixtures.

### W4 — Product integration and polish

- Ribbon customization and separate appearance persistence.
- Theme/dark/backdrop choices appropriate for a real application.
- Accessibility, keyboard, RTL, DPI and reduced-motion passes.
- Outside-debugger startup, resize, long-document and pagination performance checks.

### W5 — Distribution decision

- Decide after sustained use whether Writer remains a source sample or receives a portable GitHub
  artifact. It must not delay or redefine the RibbonKit runtime release cadence.

## 9. Verification and acceptance

Automated coverage should include:

- `.txt`, `.rtf` and `.rkw` load/save/round-trip and corrupt-input behaviour.
- Page-preset conversion, orientation swap, margin validation and schema migration.
- Dirty-state transitions, atomic save failure and unsaved-close decisions.
- Formatting command state over uniform, mixed and empty selections.
- Table insertion, row/column mutation, merge/split and caret resolution.
- Preview clone isolation and deterministic pagination inputs.
- Separation of ribbon customization, document settings and application appearance.

Manual Windows acceptance should include:

- Create, format, save, close and reopen a multi-page native document containing an image and table.
- Export representative documents to RTF and text with clearly communicated fidelity changes.
- Print preview and physical/PDF printing for A4 and Letter with normal and custom margins.
- Keyboard-only editing and complete KeyTip traversal, including contextual Table Tools.
- 100/125/150/175/200% DPI, live monitor changes, light/dark themes and focused RTL text/table input.
- Narrow-window ribbon reduction, minimized ribbon, QAT customization and Backstage document commands.

The project is complete only when it is comfortable to use for an ordinary multi-page note or letter,
not merely when every ribbon button has a handler.
