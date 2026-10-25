# Writer host integration history

> Current evidence and manual acceptance belong in the
> [Writer integration checkpoint](../19-WRITER-HOST-INTEGRATION.md).

## 2026-10-22 — Preferences and shared layout motion

Writer uses an inherited window density scope and explicit detached-window scopes
for View's Mouse/Touch choice. Restoration occurs during window construction,
before presentation; first-render readiness remains RibbonKit-owned. Appearance
retains the selected View density through edits/defaults. New preference fields
are additive schema-v1 properties, with Compact and WhiteOverlay defaults, so
existing settings remain readable without a schema migration.

The follow-up selector presentation uses the same shared RibbonDropDownButton
and descriptive, illustrated Mouse/Touch menu pattern as Showcase. Writer owns
the icon resources and preference handler. Density inheritance highlights the
active row even when the menu is borrowed by QAT; the `Writer.View.InputMode`
command identity remains stable for saved customization.

Office2024Flat follows all generic backdrop/glass/accent APIs. The only 2024-only
Writer presentation branch requiring extension was active-backdrop ruler
transparency. The Office workspace and paper edge remain neutral; Crystal's
underlay/fade/edge policy remains Crystal-only. QAT icon appearance sets the
existing public property and uses the shared template, including its separate
color glow and adaptive artwork.

Settings keeps its existing appearance and ribbon checkpoints. Apply advances
the appearance rollback snapshot; the Simplified page's Apply advances only the
ribbon checkpoint. No new mode selector, command default redesign or schema
coupling was introduced. Find/Replace's remaining native presentation now uses
the same shared dialog surfaces and inputs as the other detached Writer forms.

The separately approved WriterDocumentTransition subscribes to Preparing,
Starting and Ended. Real layout commits immediately. Inert vector drawings move
paper, ruler and preview without scaling text, while a clip follows the occupied
ribbon boundary and stops above the status bar. Supersession samples current
presentation and guards terminal cleanup by ID; source opacity is hidden with a
composed clock so authored bindings/animations/transforms/clips survive release.

Snapshotting skips invisible children and rejects native/media/live-brush content,
composition, nontrivial root transforms and more than 4,000 realized visual nodes.
Theme/view/document changes, unload and geometry invalidation release Writer's
presentation. No host timer, lifecycle deferral/completion, Showcase helper or
new runtime API is involved. Crystal's established underlay remains live after
playback; temporary document paint never covers the moving QAT.

Focused tests use temporary preference files, real offscreen WPF windows,
nonempty selected text, 110% zoom and shared-clock sampling. Mixed-monitor DPI
and human visual acceptance remain separate.
The two-line shared Simplified label binding correction is recorded in
[library history](library/14-simplified-ribbon.md#read-only-choice-labels--2026-10-22)
and RKWF-050; it was authorized after a fresh-process reproduction.

## 2026-10-23 — Caret formatting and copied font commit boundaries

RKWF-055/056 separate native editor formatting from host combo commit glue. A
plain TextRange at empty selection endpoints cannot replace TextSelection's
pending insertion properties. The adapter's inline, Font dialog and Clear
Formatting paths now mutate TextSelection and project its effective values at a
caret. WPF owns word formatting, subsequent typing, caret movement and replacement
document defaults. Empty-document toggles now report native uniform values.

QAT/custom/overflow font menus deliberately project selection only; their source
does not open or raise DropDownClosed. Writer commits source SelectionChanged
only outside an active native edit and ignores state-projection changes. Classic
and Simplified native editors keep their explicit close/Enter/focus-loss
boundaries. The controller finds the related Simplified editor through the public
QuickAccessSource link during preview focus, subscribes once and removes its
handlers on disposal. Partial editor text survives state refresh. No private
RibbonKit member, new runtime API, selection snapshot or host timing delay is used.

The original ten reproductions failed at the formatting assertions, including all
six family/size menu-copy combinations; retained selection assertions passed.
The later Simplified boundary check caught eager source-selection commits during
native editing and drove the explicit projected-editor handling. Synthetic text
insertion requires initial dispatcher/window readiness. Editable input tests use
PART_EditableTextBox rather than assigning the projected ComboBox.Text property,
which exercises a different binding-update order. These checks do not establish
physical keyboard/OS IME acceptance. Current evidence, normal Debug rebuild and
native recheck remain in the
[active acceptance plan](../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#writer-caret-formatting-and-copied-font-commits--2026-10-23).

## 2026-10-24 — Search-result focus

W7's Find/Replace review found that selecting a search match while its modeless
dialog retains activation hides the editor's normal blue selection. Writer's
dialog now activates the window containing the editor and focuses the editor
after a successful Find Next. The dialog remains open with its query intact;
repeated invocation advances and wraps normally. Empty or missing queries keep
dialog focus so the user can correct them.

Focus belongs to the dialog's completion path, leaving the deterministic search
service and shared RibbonKit controls unchanged. This does not enable inactive
selection highlighting for other Writer popups or alter replacement behavior.
Regression coverage uses the real dialog and a separate native editor window,
checking window activation, keyboard focus, preserved query, repeated/wrapped
matches and failed-search focus in Find-only and Replace presentations. These
are synthetic button invocations, not physical input or final visual acceptance.
Current evidence and the user's recheck belong in the
[acceptance plan](../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#writer-search-result-focus--2026-10-24),
with the consumer observation tracked as RKWF-064.

## 2026-10-24 — Same-caret mouse-click formatting

RKWF-065 extends the accepted RKWF-055 insertion-state correction to an additional
gesture: choosing formatting, then clicking the unchanged caret or empty document
before typing. WPF's [TextEditorMouse](https://github.com/dotnet/wpf/blob/v8.0.31/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Documents/TextEditorMouse.cs)
explicitly clears springloaded typing properties during mouse placement, even
when the insertion position stays the same. Focus-only coverage did not exercise
that reset.

WriterEditingAdapter now marks explicit inline, Font-dialog and Clear Formatting
operations at an empty caret. A small WriterCaretFormattingRetention helper
captures the effective native properties on plain left-button down and restores
only changed values after release if the document and insertion point are still
unchanged. It does not handle or block mouse input. Selection/caret movement,
document edits/replacement, availability changes and disposal discard retention.
Typing and new caret locations still use native WPF behavior; there is no global
sticky font preference, document-default rewrite or runtime control change.

Regression coverage includes empty/populated documents, inline/dialog/clear paths,
subsequent native text composition, hit-tested blank-area insertion positions and
no extra Undo step for same-word clicks. Negative cases cover caret movement,
selection, intervening edits, document replacement, read-only/disabled changes and
disposal. Tests raise routed mouse events and invoke WPF's pending-format reset
through reflection; production uses public RichTextBox events/properties only.
These checks do not establish physical pointer input or the final Writer visual
recheck. Exact current evidence is in the
[acceptance plan](../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#writer-same-caret-mouse-formatting--2026-10-24).

## 2026-10-24 — Semantic table-picker layout

RKWF-063 exposed a mismatch between Writer's table dimensions and a general
wrapping gallery. The wide Custom Table footer determined the Touch strip's
uniform cell width, leaving one stretched choice. Expanded Touch targets then
wrapped eight logical columns into six visual columns, producing a zigzag preview.

Writer now authors the gallery's public ItemsPanel as a normal WPF Grid with eight
Auto columns and three quick-choice rows. Each existing item gets its logical
Grid.Row/Column; the footer spans a fourth row and no longer determines tile size.
The footer uses an app-owned content template and the existing shared Touch action
button style, keeping a full-width themed command with a 44-DIP Touch minimum.
Gallery/popup widths account for eight natural cell widths, opener/chrome and a
small fractional-pixel rounding allowance. The existing 54-DIP strip retains one
visible row. Selection/preview, keyboard mapping, deferred commits, IDs, settings
and the borrowed native presenter remain unchanged; no runtime source/API changes.

Geometry checks cover the native strip/popup, QAT in a 620-DIP window, custom-group
and Simplified copies, density round trips and presenter return. A separate action
check verifies a single 2×3 choice inserts exactly one table of that size. The
fixture must use the RKW profile so table editing is enabled. Initial diagnostic
captures used a plain-text profile and were unsuitable enabled-state evidence.

Capture the complete PopupRoot when reviewing RTL popups: rendering an inner RTL
border alone mirrored its text in RenderTargetBitmap. Including the full flow
transform chain corrected the capture without any UI flow-direction workaround.
These are WPF rendering/geometry checks; actual hover, pointer choice and final
Writer acceptance remain separate in the
[active plan](../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#writer-table-picker-layout--2026-10-24).
