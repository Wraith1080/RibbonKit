# Optional native gallery presenter and dropdown content hosting

Status, **2026-11-01: deferred proposal, recorded at the user's request**.
No implementation or public API is approved by this record. Resume with a bounded
API/ownership proposal; the existing built-in gallery remains the default.
This is native RibbonKit.WinForms work; WPF runtime/build/CI remain outside scope.
The deferred dropdown popup designer and custom UserControl hosting proposal is
recorded separately below. Neither extension is implemented by this document.

## Purpose and current boundary

Allow an application to supply a custom gallery body, potentially an ordinary
visually designed UserControl implementing a small gallery-specific contract.
RibbonKit retains gallery chrome and behavior while the presenter supplies
custom item layout and visuals.

Existing galleries already support renderer overrides for DrawGalleryTile and
DrawGalleryHeading, and custom IRibbonIcon artwork. This proposal addresses an
entire custom content control. It does not replace the existing renderer hooks.
The current [gallery model and behavior](../README.md#galleries-in-dropdowns-the-ribbon-and-quick-access)
remain the compatibility baseline.

## Proposed communication contract

Use typed operations and feedback, with exact member names to be reviewed later.
The following names describe intent, not shipped API:

| Direction | Proposed responsibility |
| --- | --- |
| Gallery → presenter | Supply the existing tile declarations, authoritative selection and availability; refresh theme, font, DPI and viewport context. |
| Gallery → presenter | Request row/page scrolling, directional/Home/End navigation, and revealing an item. The presenter resolves movement using its actual layout. |
| Presenter → gallery | Report a hovered/focused item for temporary preview, request an accepted item commit, and cancel its preview. |
| Presenter → gallery | Report scroll availability and usable item bounds/hit regions for chrome, focus, keyboard integration and accessibility. |

For example, clicking the gallery's down arrow requests one row of presenter
scrolling. Clicking a custom tile requests a commit through the gallery's
existing activation path. Scrolling, temporary preview and committed selection
remain distinct operations.

## State and integration rules

- Keep one source gallery model for Items, selected identity, enabled/visible
  state and bindings. A custom presenter displays that state rather than owning
  an independent competing selection model. Existing programmatic selection
  changes raise SelectedItemChanged without becoming user commits.
- Preserve SelectionCommitted, ItemPreview and ItemPreviewCancelled semantics,
  accepted-tile validation and command execution exactly once. Preview cancellation
  belongs to the requesting view; one view must not cancel another's active preview.
- RibbonKit retains popup opening/closing, validation, dismissal and source/QAT
  routing. Presenter input enters that same behavior path. Settle keyboard/focus
  routing explicitly so the same input is not processed twice.
- Define item identity, bounds, hit testing and accessible selection alongside
  navigation. A custom UserControl alone does not establish gallery keyboard or
  accessibility parity. Preserve the existing default presenter in every mode.
- Viewport/scroll state is transient presentation state. Do not add it to ribbon
  customization persistence as part of this slice.

## Native control lifetime and ownership

Inline and expanded galleries can be visible together. Plan separate presenter
instances for different simultaneous presentations, sharing the source model.
Each native Control has one parent; do not move a single live Control between
simultaneous views or clone source items, bindings or command state to imitate it.

A factory creating a presenter for each presentation is a candidate extension
point. Before implementation, explicitly settle who creates/disposes those views,
whether any borrowed-instance option is needed, creation timing (initialization
or presentation opening), reuse/caching, detach and source-disposal behavior.
Do not silently inherit Backstage's Content/ContentType lifetime rules: its
single page-body contract differs from gallery presentations. Application assets
remain application-owned under their existing contracts.

## First bounded increment

1. Review the smallest gallery-specific runtime contract and native API baseline
   additions, with compatible defaults and explicit ownership/creation timing.
2. Implement an opt-in **dropdown gallery** presenter and one example UserControl
   showing custom tiles, scrolling, hover preview and accepted selection through
   the existing source gallery. Keep the built-in path unchanged by default.
3. Verify the extension in an independent consumer using public APIs, without
   Showcase helpers or resources. Document the host's implementation obligations.
4. After that contract is verified, plan inline strips and QAT presentations with
   distinct view instances and shared selection/preview semantics. Designer
   presenter-class selection can follow the established runtime contract.

Do not include a general-purpose content-host framework, arbitrary replacement
data models, new themes, broad gallery/dialog restyling, new animation, localization
or RTL implementation in the first increment. Existing supported behavior still
needs regression coverage where the extension changes a shared path.

## Focused verification when resumed

Use normal Debug outputs and checks relevant to the actual change. Cover row/page
scroll boundaries and reported button availability; pointer and keyboard navigation;
selection versus preview/commit/cancel; programmatic selection reveal; hidden,
disabled, removed and disposed tiles; popup dismissal/validation; theme/DPI geometry;
and event delivery without duplicate execution.

Verify native ownership, subscription cleanup, factory creation/disposal and any
reuse policy, followed by independent-consumer behavior and applicable default
presenter regressions. Once inline/QAT are included, verify their simultaneous
view/state/focus interactions separately. Keep automated, generated-code/designer,
actual visual/native input and physical-DPI acceptance distinct. Preserve prior
passing evidence unless a change or unresolved risk warrants rerunning it.

## Dropdown popup designer and custom content — deferred addition, 2026-11-01

Recorded at the user's request alongside the gallery hosting proposal. Keep
ordinary dropdown/split-button menus and custom gallery presenters as distinct
contracts; they share native hosting concerns but have different content behavior.

### Current native capability

- Edit Ribbon already authors nested dropdown/split-button Items, including
  buttons, options, textboxes, comboboxes, separators and nested menus. There is
  no dedicated popup preview on the Form designer surface.
- Showcase's **Inputs → Edit in popup** uses RibbonTextBoxItem and
  RibbonComboBoxItem rows. It demonstrates library-hosted native editors, not an
  arbitrary application UserControl.
- RibbonDropDownItem currently exposes an owned Items collection. It has no
  arbitrary Control content or ContentType selection contract. Backstage's
  Content/ContentType feature does not implicitly apply to dropdowns.

### Stage 1: popup authoring access and designer preview

Reuse the existing Edit Ribbon nested-items editor. Consider an **Edit popup…**
contextual/smart-tag shortcut that selects the relevant dropdown branch, avoiding
a second competing editor for the same declaration. Preview dropdown and split
menus, their nested submenus, separators and static native field representations
using shared layout/rendering.

Preview clicks select authored components for Properties; they must not execute
commands, validate/commit runtime input or open the live runtime popup. Preview
open/close, menu navigation and selection remain transient and do not enter
generated code or the undo stack. Actual authoring retains the existing detached
draft, Cancel and single-transaction OK behavior. Determine the smallest feasible
surface-preview interaction contract before implementation.

### Stage 2: optional custom UserControl popup body

For richer layouts, design an ordinary UserControl separately and select its
class through the ribbon designer after a runtime hosting contract is agreed.
Also consider application-created instances assigned from code, following the
explicit ownership distinction used by Backstage. Exact public member names and
APIs remain proposals requiring review before implementation.

Settle these boundaries first:

- Keep owned Items as the compatible default. Specify whether custom content is
  exclusive with Items or may be composed with rows; do not silently choose or
  introduce both modes in the first increment.
- Define creation timing, eager initialization before Form.Load for declared
  classes, reuse across reopening, replacement/reset, detach and disposal. Review
  the eager-initialization choice explicitly; do not inherit a gallery's per-view
  factory lifetime automatically. Specify ownership of declared-type instances
  versus borrowed application instances.
- RibbonKit retains popup frame, placement, monitor bounds, DPI sizing, opening/
  closing, validation, dismissal and source/QAT routing. The application retains
  its content layout, data, bindings and business logic. Define preferred/minimum
  sizes, constrained-height scrolling and how content requests dismissal.
- Define focus entry/return and Tab, Escape, Enter and nested native ComboBox
  handling, including validation cancellation. Preserve native control handles,
  ownership, text and selection through reopening and routing changes. A live
  Control has one parent; settle any reuse or simultaneous-presentation policy.
- The ribbon designer selects the class and shows a placeholder/readout; body
  editing happens in the normal UserControl designer. Do not instantiate or
  capture arbitrary application content for an inert ribbon popup preview.

Verify the agreed runtime contract in an independent public-API consumer before
adding designer class selection and generated-code round trips. Include a small
UserControl example and relevant checks for initialization/ownership, reopening,
focus/validation/dismissal, source/QAT routing, native input state, constrained
geometry, existing menu compatibility, Undo/Redo and save/reopen. Separate actual
VS, visual/native input and physical-DPI review from automated evidence. This
stage does not require the gallery-specific tile/preview/scroll interface for
ordinary custom dropdown bodies.

This deferred record adds no current acceptance evidence and does not reorder the
existing designer presentation, appearance or other planned slices.
