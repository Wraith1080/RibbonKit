# Native Application Menu

Status, **2026-11-03: selected as the next feature, before geometry consolidation;
implementation and its detailed public/designer contract are pending**.

The user clarified that Application Menu should replace the File button's simple
command-dropdown presentation. The current native File surface has CommandMenu
and Backstage choices; CommandMenu is an ordinary dropdown over `FileMenu.Items`
without a dedicated Application Menu designer. This is separate from dropdowns
and split buttons used elsewhere in the ribbon.

## Agreed direction

- Use the existing File-menu path for a floating, two-pane Application Menu.
  Reuse the authored File commands and their event/command identities. Preserve
  compatibility with the existing `CommandMenu` enum/serialized preference and
  shipped public API; replacing the presentation does not require deleting that
  value or adding a third independent File presentation. Backstage stays the
  other choice.
- Define main command/navigation, right-pane content and footer actions before
  implementation. The application owns document/recent-file data and commands;
  the library owns reusable presentation, navigation, popup lifetime and host
  slots. Reuse existing menu/dropdown infrastructure where suitable.
- Add a dedicated designer workflow for Application Menu, including collection
  editing, selection/properties and an inert preview. Establish generated-code
  defaults/reset, retained custom content ownership, Apply/Cancel/no-op behavior,
  Undo/Redo and save/reopen before adding APIs.
- Cover keyboard/KeyTips, split/submenu commands, scroll/reveal, focus return,
  dismissal and accessibility alongside normal pointer use. Keep native hosted
  controls live, and account for Solid/Mica/Acrylic, density and DPI.
- Agree on one initial layout and the additive API/designer contract next. Orb
  variants, additional themes and a broad geometry settings API are later work.

Application Menu's columns, content slots, footer, scrolling and popup placement
need to exist before their measurements enter geometry consolidation. Sequence:
state-command popup fix and review → Application Menu and review → geometry
consolidation → additional themes. No Application Menu runtime/designer code has
been implemented by this planning record.
