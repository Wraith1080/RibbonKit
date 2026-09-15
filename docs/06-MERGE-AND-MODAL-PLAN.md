# Tab merging and modal tabs

Phase 7 shipped in 2026-07; the automated invariant suite followed in 2026-08.
This is the retained implementation contract, replacing superseded API sketches and
pre-implementation questions. Detailed decisions remain in design-history §§3.32–3.36
via the [index](../04-DESIGN-NOTES.md#3-implemented-features-chronological-with-pitfalls).

## 2. Merge ownership and activation

`RibbonMergeSource` contributes whole tabs and groups to existing host tabs.
`RibbonMergeService` owns insertion/removal and source records; core adaptive layout
has no modal/merge special cases. `Target` plus `IsActive` provides declarative
activation, while `Ribbon.Merge`/`Unmerge` remains the imperative path.
The attached `RibbonMergeSource.Source` lets a child carry its source for an
activation-aware host such as MDI. WPF keyboard focus is not document activation.

Preserve source data context, ordering and identity across repeated merge/unmerge.
Remove contributed groups by reference; no original-index restoration is needed
for host groups. Check negative/equal ordering and multiple sources against the
service's actual comparison policy rather than the retired speculative algorithm.

Merged content is transient: exclude tabs, contributed groups and their QAT entries
from structural persistence and the customization tree. Park unavailable QAT proxies
disabled on unmerge and revive them when the source returns. Keep borrowed menu
ownership intact; rebuilding overflow proxies can strand a dropdown.

## 3. Modal lifetime

`RibbonModalScope` manages entry/exit and prior selection/visibility. The active modal
tab is exclusive; QAT stays available, File is hidden, and minimize/Backstage opening
is blocked. Revert blocked values rather than coercing them: a coerced base value can
unexpectedly reappear on exit. Select the modal tab before hiding other tabs, and
restore the prior valid selection or a deterministic visible fallback afterward.

Use authored visibility (`Ribbon.GetAuthoredVisibility`) when saving customization.
Persisting temporary collapsed values would reopen the app with normal tabs hidden.
Merge while modal records hidden tabs for later restoration; removing the active
modal tab forces a coherent exit even if an ordinary exit could be cancelled.

## 4. Layout and visuals

- Refresh the sliding underline and Office 2010/2013 connected notch after merge,
  visibility, reorder and modal transitions. Update strip extent/selection scrolling.
- Keep tab-row visibility/reflow correct when QAT, merged captions and File change.
- The modal close affordance and merged caption reuse shared foreground/hover/radius
  tokens. The old proposed `ModalClose.*` token family was not added.
- KeyTips must reflect visible modal content. Motion honors reduced motion and uses
  opacity/transforms. Designer enumeration must tolerate declarative merge sources
  without running live activation.

## 5. Sharp edges

Numbering here preserves references embedded in the source comments.

### 5.1 Authored visibility

Serialize `Ribbon.GetAuthoredVisibility`, not temporary modal hiding.

### 5.2 Transient provenance

Exclude merged tabs, contributed groups and their QAT entries from customization.

### 5.3 Customization rebuild

`RibbonCustomizationSerializer.Apply` calls `UnmergeAll` before rebuilding and
`Remerge` afterward. Preserve this pairing rather than reusing stale records/indices.

### 5.4 QAT lifetime

Park disabled proxies while a merged source is away; revive on return and preserve
borrowed dropdown ownership. Overflow must not strand source menus.

### 5.5 Group order

Remove contributed groups by reference. Host groups need no saved-index restoration.

### 5.6 Modal and merged tab removal

Record tabs merged during modal hiding for restoration; force modal exit before
removing the active modal tab.

### 5.7 Designer

Declarative sources must remain readable without live activation.

## 7. Testing

Cover repeated merge/unmerge, simultaneous sources, group ordering, selection removal,
customization capture/apply during merge/modal state, QAT park/revive and overflow,
modal cancellation/removal, and active MDI child switching. Inspect connected-tab
geometry, RTL/overflow and keyboard behavior on the relevant live surface.
Use [CONTRIBUTING.md](../CONTRIBUTING.md#proportional-validation) for commands and
scope; prior recorded approval is not a fresh test result.

Backstage-page merging, automatic source-QAT merging and nested merge sources were
excluded from the original scope. Richer projection work remains a separate candidate.
