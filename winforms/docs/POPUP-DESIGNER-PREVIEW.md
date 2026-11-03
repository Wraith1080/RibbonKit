# Inert dropdown and split-button designer preview

Status, **2026-11-03: implemented and packaged; actual Visual Studio and user review pending**.
Baseline: clean `codex/Ribbonkit-Winform` at
`bc5b05a733492c02b51dc58708952443a9fc595d`.
This is the second user-authorized follow-up after the inline gallery class picker.
It stops before appearance consolidation and themes, as requested.

## Review workflow

1. Reopen the refreshed permanent Designer Sample solution and build the consumer.
2. Select a dropdown or split button, then use **Edit popup…** in its designer
   actions. This opens the existing Edit Ribbon workflow. Alternatively, select
   the command or one of its menu children in Edit Ribbon and choose **Preview popup…**
   from the toolbar or tree context menu.
3. Click a preview row to select its draft Properties. Click a dropdown/split
   arrow to preview its submenu; **Parent menu** returns. Disabled rows and
   separators remain selectable; authored-hidden rows stay hidden in the image.
4. Edit properties, menu children, order, icons or choices using the existing
   controls. The image refreshes after an authored change; selection alone only
   moves the outline. The preview scrolls within its pane and the divider adjusts
   the space shared with Properties.
5. **Close preview** changes no declaration. **Cancel** discards draft edits;
   **OK** uses the existing single designer transaction and Undo/Redo behavior.

TextBox/ComboBox rows are static representations. Clicking them selects the
component rather than editing/validating runtime input. Preview clicks never
execute a command, change checked/selected values, or open a live ribbon popup.

A declared or application-provided UserControl body uses a labelled placeholder.
Design it in its ordinary UserControl designer. No application control is created,
captured, painted or reparented for the preview. Clearing ContentType in the draft
previews the preserved menu rows without changing the live declaration.

## Implementation boundary

The smallest surface is an image pane inside Edit Ribbon, keeping the existing
tree and Properties editor authoritative. This increment does not add a second
menu editor or a floating overlay on the Form design surface. Backstage's accepted
retained preview is unchanged. The shortcut also resolves nested commands and QAT
menu sources to their original declaration.

The server renders detached design-mode declarations with the existing popup
measurement and shared renderers. It reads the measured header rectangle through
one internal accessor; runtime popup behavior/public APIs are unchanged. Native
fields are painted statically with shared field geometry. The existing buffered
text path produces opaque PNGs without damaged glyph edges. Images use its bounded
four-megapixel budget; exceptionally large menus ask for a submenu preview instead.

The designer response adds an image and keyed hit rectangles; no preview field is
added to the authored snapshot, generated code or customization JSON. A request
copy uses the existing transient SelectionKey to select the menu to render.
Selection/navigation adds no Undo unit. Stale or temporarily invalid drafts show
a preview error without activating a runtime surface or crashing the click path.

## Verification

- **15 new focused cases pass**: dropdown/split and Compact/Touch rendering,
  fields, headers, descriptions, separators, hidden/disabled rows, submenu hits,
  declared/borrowed placeholders, draft body reset, no commands/validation/runtime
  popups, draft selection/Cancel, real framework Undo/Redo, response serialization,
  stale input/UI handling, structural refresh, shortcut source resolution, shown
  pane layout and synthetic 100/150/200-percent resizing. The final group passed
  14/14 in `popup-designer-preview-final.trx`; the added reset case and strengthened
  stale-click case passed separately in `popup-designer-preview-content-reset.trx`
  and `popup-designer-preview-stale-ui.trx`.
- Related designer checks passed **125/129** initially. Four layout assertions
  compared the grid's coordinates with its former parent after it moved into the
  split pane. They now compare the header and property host in the same container;
  only those four cases were rerun, passing **4/4** in `popup-designer-preview-layout.trx`.
  The other passing cases were retained. This is focused evidence across runs,
  not one clean aggregate or a full-suite result.
- Normal Debug runtime, server, net472 client, package and Designer Sample builds
  passed without warnings/errors. The sample was refreshed in place; authored
  Forms/UserControls were preserved. Current local package:
  `0.1.0-designer.local20261103T040446039Z`.
- The package probe loads the actual packaged server assembly and checks six
  menus: ordinary, nested split and custom-body placeholder × Compact/Touch.
  All passed without runtime popups, commands, validation, body construction or
  changed editor values. It checks that the net472 client is packaged. Reflection
  probes internal designer endpoints; this is not a runtime public-API consumer
  or actual VS out-of-process interaction. Evidence:
  `artifacts/popup-designer-preview/package-review/`.
- Static dropdown, nested and custom-body images, the shown editor pane and
  packaged menu images were inspected. Captures are under
  `artifacts/popup-designer-preview/final/` and `package-review/`.

Actual VS smart-tag/context-menu interaction, save/reopen, Undo/Redo and mixed-DPI
review remain separate. No full suite, Release, WPF, commit or push work ran.
The next slice is bounded appearance consolidation, followed by themes.
