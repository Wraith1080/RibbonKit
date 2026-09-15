# RibbonKit Writer product contract

Writer is a separate `net8.0-windows` rich-text sample and sustained RibbonKit consumer.
Showcase remains the exhaustive component laboratory. This document owns product
scope; [design notes §5](../04-DESIGN-NOTES.md#5-current-state--next-steps) owns progress
and acceptance limits, and the [packet map](11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md)
owns remaining delivery boundaries. Features listed here do not close unverified gates.

## 1. Document and application state

Keep three states separate: native `FlowDocument` content and immutable page settings;
file identity/format/dirty state; and app appearance/ribbon customization.
Host responsibilities include the DPI manifest, icons, file IO, safe persistence,
focus recovery and editing-command state. Writer concepts do not belong in RibbonKit APIs.

| Profile | Contract |
| --- | --- |
| Plain Text (`.txt`) | Character content; formatting, structures and page metadata cannot round-trip. |
| Rich Text (`.rtf`) | Supported formatting; Writer-only layout/structured fidelity is unavailable or best effort. Conversion reports loss. |
| RibbonKit Writer (`.rkw`) | Fidelity format for supported text, pictures, safe hyperlinks, tables and page settings. |

The native ZIP contains `content.xamlpackage`, `document-settings.json` and a manifest.
The accepted content schema supports v2 tables and v1 text fixtures; outer manifest
and settings remain v1. Bounded loading rejects foreign/executable content, invalid
schemas, inconsistent versions, unsafe children and corrupt table grids before
replacing the current session. Save/replace is atomic. Failed/cancelled operations
preserve identity and the current document.

Backstage New uses three accessible pictured profile cards. The profile determines
command capability and the initial Save extension; Ctrl+N uses the configured default.
Save As changes identity only after success and warns for lower fidelity. Profiles
are not content templates. Recovery, if added later, must use separate app data and
must never silently overwrite the source document.

## 2. Editing and command surface

Provide New/Open/Save/Save As, recent documents, dirty title/unsaved-close handling,
clipboard/native Undo/Redo, selection-sensitive font/paragraph formatting,
find/replace, spelling, counts and zoom. Stable command IDs, KeyTips, ScreenTips and
UIA names are part of the integration contract.

Home uses installed-font preview/search, validated arbitrary size entry, last-used
foreground/highlight actions, base/recent colors and an accessible More Colors picker.
App-owned Font/Color/Paragraph dialogs preserve mixed/unset state, preview valid
values and apply one undoable change; Cancel leaves content/history unchanged.
Single/double strikethrough and super/subscript require native round-trip support.
Named styles remain absent until a complete style/persistence contract exists.

Editor Tab/Shift+Tab handles paragraph/list indentation with explicit literal-tab and
keyboard-exit paths. Table-cell navigation takes precedence inside tables.
Context menus capture and revalidate a document-bound target; ordinary text retains
shared/spelling actions while pictures/tables/hyperlinks add only valid operations.
Reject stale targets after replacement, deletion or history traversal.

## 3. Page presentations

`DocumentPageSettings` uses DIPs (96 per inch), A4/Letter/Legal/custom dimensions,
drift-free orientation and validated margins. Page metadata is document state.

- **Continuous:** the native workspace-filling editor.
- **Default Paper:** one centered sheet with the selected width and minimum page
  height, growing downward with content. The dotted margin guide follows that growth.
  Margins and zoom affect presentation; switching views preserves content, selection,
  focus and history. No presentation inserts artificial content or creates separate editors.
- **Preview/print:** an isolated document/snapshot with the same logical page inputs.
  Preserve deterministic page count/break parity and stable navigation; no live-editor
  paginator mutation. For pictured output, preserve the corrected isolated flow-print
  path rather than re-serializing fixed XPS bitmap resources (history §3.135/RKWF-025).

Page provides presets, orientation, page color and transactional Custom Margins.
View owns zoom and supported presentation/ruler/guide toggles; the status zoom remains
available. Non-printing ruler/guides follow page, zoom and horizontal scroll geometry
and never enter content, clipboard, native files or print. Margin drags preview,
commit one validated update and roll back on Escape/capture loss; paragraph indent
drags preserve native Undo/mixed-selection semantics. Vertical rulers, mirrored/gutter
margins and editable tab stops remain outside the first contract.

Use the app-owned print setup with the existing preview. Validate actual printer
queue/ticket/imageable-area constraints without silently altering logical margins.
Physical-driver and mixed-monitor acceptance remain separate from PDF/single-display evidence.

## 4. Structured content

Insert supports pictures, safe hyperlinks, date/time and native FlowDocument tables.
The quick table picker is 1×1–3×8; Custom Table accepts 1×1–8×8. Tables retain valid
row-group/span occupancy, predictable caret resolution, row/column mutation,
rectangular merge/split, final-cell Tab row creation, cell alignment, border/fill,
padding and bounded sizing/distribution. RTF does not promise native table fidelity.

Table Tools/Picture Tools remain stable across ribbon focus and intermediate
selection events during mutation. Publish one final context after caret recovery;
collapse stale context after actual deletion/replacement or leaving the object.

Non-printing adorners provide table selection/row/column/overall grips and eight
picture handles. Picture corners preserve aspect ratio; edges change one axis.
Table row sizing is a content-based approximation, not a nonexistent `TableRow.Height`.
Respect minimums, bounds, spans, local zoom/scroll/RTL geometry and semantic table
placement. A resize previews locally, commits one native undo unit and rolls back on
Escape, capture loss or invalidation. Provide ribbon/keyboard/UIA alternatives.
The opt-in paginator rejects untrusted Auto/star horizontal boundaries rather than
inventing resize handles; its explicit-column policy is narrower than default editing.

## 5. Settings and appearance

The host dialog is **Settings** with an app-owned **Appearance** page and built-in
**Customize Ribbon**/**Quick Access Toolbar** pages. Separate versioned files hold
appearance and ribbon structure. Appearance covers enabled themes/palettes, accent,
title bar, Backstage/translucency, compatible backdrop/frame/File shape, motion and
view preferences. Unsupported combinations keep a valid fallback and clear disabled state.

Preview changes transactionally: Apply/OK persists, Cancel restores the opening
snapshot, and Appearance defaults do not reset ribbon structure or documents.
Retheme the open dialog without losing its page, focus or pending values.
Structural Import/Export/Reset cannot leak appearance or transient merge/modal state.

## 6. Boundaries and remaining acceptance

No DOC/DOCX compatibility, OLE/COM activation, macros, mail merge, tracked changes,
comments/collaboration, cloud sync, or Word-compatible section/header/footer/footnote
layout is promised. Do not add decorative commands without working model, undo and
persistence contracts. Editable multipage editing was canceled due to bloat.

Remaining integration covers native reopen/corrupt-input safety; TXT/RTF loss;
A4/Letter and physical printing; keyboard/KeyTips/UIA; context/resize cancellation and
history; 100/125/150/175/200% DPI and live monitor changes; relevant RTL and genuine OS
IME; reduced motion, narrow/minimized/QAT/Backstage behavior; and measured startup,
resize and long-document work outside the debugger. Whole-ribbon Windows contrast-theme
support remains unclaimed. Follow [proportional validation](../CONTRIBUTING.md#proportional-validation)
and the specific packet's gates; startup alone is not visual acceptance.

W5 decides sample-only versus a portable GitHub artifact after sustained ordinary
long-document editing and preview/print use. Writer does not change the library release cadence or authorize publication.
