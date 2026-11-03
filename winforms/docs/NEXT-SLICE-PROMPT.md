# Starting prompt: native RibbonKit.WinForms Application Menu

Continue work in:

`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

Prepared **2026-11-03**. Implement the next bounded slice: **a native Application
Menu replacing the File button's simple command-dropdown presentation, with a
dedicated designer workflow**. Backstage remains the other File presentation.
Application Menu comes before geometry consolidation, then additional themes.
Stop for user review after this Application Menu slice.

Start with a bounded review and present the proposed native API, ownership,
layout and designer contract before runtime/public API edits. Then implement the
authorized slice through appropriate verification. Resolve routine choices
yourself; ask only when missing information materially changes scope,
correctness or compatibility. Do not stop after another general roadmap.

## Live baseline and boundaries

- Start with `git status --short` and verify branch/HEAD. At preparation:
  **`codex/Ribbonkit-Winform`**, HEAD
  **`779f529b83de8835af95850eced5a8794d309c80`**
  (`fix gallery scrollbar and toggle in popup`). The user committed the accepted
  gallery scrollbar and state-command popup fixes while this handoff was being
  prepared. Only `APPLICATION-MENU-PLAN.md`, `NEXT-SLICE-PROMPT.md` and
  `STATE-COMMAND-POPUPS.md` under `winforms/docs/` have documentation edits at
  handoff. Preserve all live tracked/untracked changes. The user may commit again
  before the next chat, so recheck rather than restoring this baseline or assuming
  the checkout is still dirty.
- Read root `AGENTS.md` and `winforms/AGENTS.md`. Use one agent. No commit, push,
  publish, branch switch, stash/reset/clean or disposal of existing work.
- Implement only under `winforms/`. WPF sources are read-only references. Do not
  modify/build/test WPF or add WPF hosting/runtime dependencies.
- Use normal Debug/default outputs for review. Reuse the permanent Designer
  Sample through `winforms/eng/Update-DesignerSample.ps1`; preserve its authored
  Forms/UserControls/resources. Do not create another user review app.
- Shared presentation, measurement, input, designer support and extension points
  belong in the library. Hosts own commands, document/recent-file data,
  persistence, icons and application lifetime. Verify a public-API consumer
  without Showcase helpers/resources.

## Read bounded current sources

- `winforms/docs/APPLICATION-MENU-PLAN.md` — selected replacement direction.
- `winforms/docs/STATE-COMMAND-POPUPS.md` — latest accepted activation policy.
- `winforms/docs/APPEARANCE-COLOR-ROLES.md` and affected portions of
  `winforms/docs/M4I2-APPEARANCE-INVENTORY.md` — shared appearance/fallbacks.
- Relevant File/popup/content/designer sections of `winforms/README.md` and
  `docs/21-NATIVE-WINFORMS-PLAN.md`. Follow applicable design-index/history
  pointers without loading the entire project history.
- Native starting points: `RibbonControl.Backstage.cs`, File/open/activation paths
  in `RibbonControl.cs`, `Model/RibbonBackstage.cs` (`RibbonFileMenuMode`), FileMenu/
  dropdown models, retained content initialization, popup/window/menu layout,
  editors, input/KeyTips/accessibility and shared renderers/theme.
- Designer starting points: `RibbonFileDesignSettings.cs`,
  `RibbonDesignCommitter.cs`, protocol/draft models, client editor and existing
  inert popup/Backstage previews.

WPF references, all read-only:

- `src/RibbonKit/Controls/RibbonApplicationMenu.cs` — default/active right pane,
  per-item pane content/header, split primary/arrow and footer concepts.
- `src/RibbonKit/Themes/Controls.ApplicationMenu.xaml` — command column, divider,
  right-pane headers/viewports/scrolling, fixed footer and frame ownership.
- `src/RibbonKit/Controls/ApplicationMenuGeometryConverter.cs` and
  `src/RibbonKit/Themes/Tokens.Office2024.xaml` — dimensions and screen budgeting.
- Relevant examples in `samples/RibbonKit.Showcase` and test sources such as
  `ApplicationMenuHoverTests.cs`, `ApplicationMenuLayeringTests.cs` and
  `ApplicationMenuViewportChecks.cs`, as behavior evidence only.

Current WPF Office 2024 reference values: navigation width **150**, pane width
capped at **340**, item height **40**, footer height **36**, in 96-DPI logical
units. Translate the intent through native font measurement, Compact/Touch and
work-area constraints. The user allows better native ideas: explain meaningful
departures with measurements or a comparison. Use one coherent measurement for
paint, hit testing, keyboard/UIA bounds, scrolling and popup placement. Do not
transplant WPF templates or force reference dimensions regardless of content/DPI.

## Bounded implementation

1. Replace the existing File command-list presentation, rather than adding a
   third independent File mode. Reuse `FileMenu.Items`, commands, bindings,
   identities and opening/closing/cancellation semantics where applicable.
   Preserve `CommandMenu` enum/numeric/serialized compatibility and published
   property contracts; do not silently rename/remove them. Keep Backstage and
   ordinary dropdown/split controls elsewhere supported.
2. Propose the smallest native model for left commands/navigation, a default
   right pane (for example app-supplied recent documents), item-specific right
   panes with optional headers, and footer actions. Define leaf action versus
   pane selection, split primary/arrow behavior, empty/default states, collection
   ownership and source identity. Borrow useful WPF behavior, not its framework.
3. Define native Control/UserControl hosting before coding: app-owned instances
   versus library-created types, initialization timing, retention across pane
   switches/reopening, validation, focus, clipping, disposal and failure behavior.
   Reuse accepted retained-content patterns where appropriate. Preserve one real
   editor/control owner and native HWNDs. Designer previews must not instantiate
   or execute arbitrary application content.
4. Implement a reusable measured two-pane layout with a reachable footer. Budget
   width/height against the work area and DPI; handle long captions, large fonts,
   empty panes and overflow. Keep headers, viewports, scrolling and footer
   ownership explicit. Reuse native popup/frame/shadow/motion infrastructure
   where suitable, including reduced-motion fallback and input settlement.
5. Use shared renderer/theme roles and compatible defaults. Native application
   controls keep application styling. Add only settings this component requires;
   do not begin broad geometry consolidation, theme authoring, an appearance
   coordinator or orb/theme-variant expansion.
6. Add a dedicated, discoverable Application Menu designer workflow for command,
   section, pane and footer authoring, Properties selection and inert preview.
   Define generated-code defaults/reset, serialization, Apply/Cancel/no-op,
   one-transaction changes, Undo/Redo and save/reopen. Reuse the existing designer
   protocol/commit mechanisms and package layout. Preview interaction selects
   draft content without invoking commands or constructing native app pages.
7. Cover pointer, keyboard/KeyTips, split/submenu navigation, pane switching,
   scroll/reveal, footer reachability, Escape/outside-click dismissal, focus
   return and accessibility. Preserve checked/disabled/focus states and one
   command invocation per action. Callbacks/source removal must not orphan a
   popup or reopen one deliberately closed.
8. Demonstrate through public APIs in normal samples and an independent consumer.
   Add XML docs, designer metadata and intentional additive
   `PublicAPI.Unshipped.txt` entries; preserve the shipped baseline. Update the
   native README and active Application Menu plan with the final contract,
   actual evidence and remaining gates.

## Accepted behavior to preserve

- Built-in gallery opening captures include the native scrollbar track/thumb:
  no first-opening black band. Keep accepted card fade/item slide, custom/
  inline/QAT gallery hosting and retained owner semantics.
- Toggles, checkboxes, radios and split primaries with `CheckOnClick=true` retain
  group and More Commands/QAT overflow containers, including ordinary collapsed
  and Simplified group flyouts. Ordinary/nonchecking actions dismiss; arrows,
  nested menu commands and explicit callback closure keep existing semantics.
  Retain the same session/focus and state-before-Click order. The user confirmed
  the correction works on 2026-11-03.
- Preserve retained dropdown/split UserControls, gallery synchronization and
  stable native ComboBox text/arrow/list painting. Avoid speculative HWND
  recreation, repaint loops or host-owned popup workarounds.
- Keep RibbonForm/TitleBar QAT, ordinary Form fallback, actual Mica/Acrylic,
  title-bar color/tint/reset/contrast, Backstage caption synchronization, native
  editors, popup stacking and motion cancellation coherent.
- Preserve command/QAT/customization identity, Apply/Cancel/profiles and
  Compact/Touch. Groups remain addable in Customize QAT only. File alignment,
  hit targets and popup anchors remain measured, not paint-only offsets.

## Evidence and completion

Previous results are context, not fresh Application Menu evidence:

- Accepted gallery correction: two new first/reopen raster cases and 12 public
  package opening/reopening scopes passed across Solid/Mica/Acrylic and Compact/
  Touch at actual 120 DPI, with 132 composed-screen samples. See its plan record.
- Accepted state-command correction: **24 new + 40 related distinct focused
  Debug cases** passed, plus an independent public-API native QAT group scope at
  120 DPI. See `STATE-COMMAND-POPUPS.md`.
- Designer Sample package at preparation:
  **`0.1.0-designer.local20261103T101745293Z`**. Recheck `.local/last-refresh.json`.
- Earlier full Release results and intermittent focus/order failures are recorded
  in the plans. Passing isolated retries do not establish a clean aggregate.

Run meaningful new/affected native checks and applicable API/Release/designer-
package gates. Favor focused selections; broader checks require an affected
contract or explicit gate. Do not run WPF checks or repeat unrelated passed
campaigns. Verify public-only consumer behavior, generated designer code/package
contents and actual captures. Refresh the permanent Designer Sample in normal
Debug for review without replacing authored content.

Serialize foreground/input runs and verify fixture ownership. Stop input on
focus loss; do not close user apps or add production focus-stealing workarounds.
Capture motion off the UI thread and save after playback where needed. Await
GUI process exit before another foreground run.

Report build/API, focused tests, package/independent-consumer, actual VS and live
native evidence separately. Verify effective Mica/Acrylic; Solid fallback is not
material acceptance. Separate synthetic DPI from actual display/mixed-monitor,
physical keyboard/touch/IME and user acceptance. Stop after this bounded
Application Menu slice for review. No geometry consolidation, new themes,
commit, push or publication in this session.
