# ComboBox presentation and designer rearrangement

Status, **2026-11-02: accepted by the user for the reviewed four-point ComboBox,
move and size-policy workflow**. Broader platform gates below remain separate.
This increment addresses the four notes from the user's popup/layout review.

| Request | Result and authoring |
| --- | --- |
| Hide a ComboBox label without erasing its command name | Set **ShowLabel = False**. Keep **Text** meaningful, for example Font family. Text remains available to QAT, customization, compact presentations and accessibility. |
| Move existing commands/groups across groups/tabs | Select the item and choose toolbar or context-menu **Move to…**. Select a destination and, for a TwoRows/ThreeRows group, its explicit row. |
| Remove size policies that grow during reduction | New dropdown choices are the ten non-growing combinations, including the three fixed sizes. A previously authored nonmonotonic value remains available on that component so unrelated edits preserve it. |
| Reduce a ComboBox into a dropdown button | Medium is the native field; Small is an icon/initial plus arrow that opens the existing chooser, like QAT. Large continues to use the Medium field for compatibility. |

## Moving between layouts

Commands can move between compatible groups and menus, including those in another
tab. Groups can move to another tab with their layout and children intact. The
picker excludes self/descendant destinations and incompatible collections; QAT
references/catalog declarations retain their separate authoring workflow. Existing
Move up/down actions continue to reorder within the current owner.

Moving into explicit rows offers First/Second, or First/Second/Third. Third maps
to Second when moving to TwoRows. Moving back to Columns preserves Row, which
Columns ignores. The move appends the item to the destination collection; use
Move up/down there to refine its order. SizePolicy is preserved, with an explicit
reminder that Large buttons span rows. Set Medium or Small when a button should
belong to one horizontal row. No automatic size conversion is applied.

Moves operate on the detached draft until OK. Existing component names, IDs,
handlers, bindings and QAT source references retain the same objects. The move,
row assignment and other edits share the existing designer Undo/Redo transaction.

## ComboBox behavior

`RibbonComboBoxItem.ShowLabel` is the only new public runtime property. It defaults
to true and changes the field label and its space reservation, not Text or Value.
Custom-group copies synchronize ShowLabel while retaining their command names.
Small presentation uses the same model/validation/binding owner and themed,
nonactivating choice popup as QAT. It parks and later restores the retained native
field rather than creating another editor for the compact button. Editable free
text is preserved; the compact popup chooses existing values, while text entry
stays in the expanded field. Menu rows retain their native field layout.

Classic group reduction, explicit rows, full-group flyouts and Simplified Small
presentations use the compact button. For adaptive Medium-to-Small reduction,
set the group's **ReductionMode = Resize or ResizeThenCollapse**. The default
Collapse mode still goes directly to a group opener. Selecting fixed Small shows
the compact button without requiring a reduction step. Existing renderer DrawItem
and foundation expand/collapse handling cover the compact presentation.

## Focused verification

- Debug runtime, design server/client, public API/XML checks and the refreshed
  package-only Designer Sample builds passed cleanly.
- 38 existing affected cases passed for designer UX, ComboBox/QAT chooser,
  nonactivating popup focus and group layout. A second bounded run passed 31 cases
  for the new compact chooser/moves and affected Simplified/customization behavior.
  Its two hidden-dialog button tests needed shown test Forms; both passed after
  correcting that fixture. Subsequent focused checks passed for popup/Simplified
  mutation guards, corrected move captures, and mixed-field hidden-label width.
  These are separate focused runs, not a full-suite result.
- Geometry covers Compact/Touch at 96/120/144/192 DPI and all three group layouts.
  Behavioral checks cover actual reduction/restoration, validation, Escape,
  accepted selection, native handle reuse, custom copies, command identity,
  cyclic/incompatible moves, cross-tab row assignment, Cancel, Undo/Redo and
  ordinary ShowLabel serialization. Move-picker captures were inspected.
- The sample's public-API `--verify-combo-layout` check passed at actual 120 DPI
  in Compact and Touch. Captures of hidden/visible labels, compact buttons and
  the named custom-group copy were inspected. One initial restoration assertion
  failed without recording its individual values; the diagnostic reruns did not
  reproduce it. The native-state log records the restored value/text and handle
  for each density. Live/native review remains a separate gate.

Artifacts are under `winforms/artifacts/designer-rearrangement/` and
`winforms/artifacts/combo-layout/`. The user's authored Form files are preserved.
No full native or WPF suite was run. Actual VS save/reopen, keyboard review and
physical mixed-monitor/DPI behavior remain for review.

From the repository root:

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug --no-build -- --verify-combo-layout winforms/artifacts/combo-layout
```
