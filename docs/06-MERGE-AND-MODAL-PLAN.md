# Tab Merging & Modal Tabs — Design Plan (Phase 7)

> **Status: BUILT and user-verified (2026-07-27).** All four steps of §6 shipped — P7.1 modal tabs,
> P7.2 tab merging, P7.3 group contributions + activation + QAT parking, P7.4 MDI wiring (which also
> closed MDI M4). This document is kept as the design record; the **as-built** account, including
> where reality diverged from the plan, is [`../04-DESIGN-NOTES.md`](../04-DESIGN-NOTES.md)
> §3.32–§3.34. The one part not delivered is §7's unit tests.
>
> Written 2026-07-27 · Companion to [`03-ROADMAP.md`](03-ROADMAP.md) Phase 7
> and [`01-ARCHITECTURE.md`](01-ARCHITECTURE.md) §8.
>
> **Where the build diverged from this plan:**
> - **§5.5 (restoring host-tab group order) was a non-issue.** Unmerge removes groups by
>   *reference*, so the host's own groups close back up correctly and two sources can unmerge in
>   any order. No index bookkeeping was needed.
> - **No `ModalClose.*` tokens were added.** The close affordance reuses `TabStrip.Foreground` /
>   `TabStrip.ControlHoverBackground` / `ControlCornerRadius` and themes across all four
>   generations for free. The merged-caption buttons do the same.
> - **The activation trigger is `Target` + `IsActive` on the source**, not an attached property on
>   an arbitrary element — that left "which ribbon?" ambiguous. An attached
>   `RibbonMergeSource.Source` exists as well, so a child can *carry* its source for a host that
>   tracks activation; that is what `MdiContainer` uses.
> - **Minimize and the backstage are blocked by reverting, not coercing** (§3.32) — coercion leaves
>   a stale base value that springs back on exit.
> - **Two things the plan didn't anticipate:** the tab-strip-row reflow rule (§3.36), which broke
>   twice; and that the QAT overflow flyout must reuse its proxies or it strands borrowed
>   drop-down menus (§3.35).

The last two power features before the v1.0 API freeze, and the last genuinely new mechanism in
the library — every other remaining item (Office 2007, dark mode, RTL, localization, the visual
regression suite) repeats a pattern already solved. Both features are also the two differentiators
[`00-PLANNING-OVERVIEW.md`](00-PLANNING-OVERVIEW.md) §3 claims over Fluent.Ribbon, and tab merging
is the dependency that unblocks MDI milestone M4.

Architecture §8's standing instruction governs the whole design: **isolate both behind their own
services so core layout never special-cases them.** No `if (isModal)` inside `RibbonGroupsPanel`,
`ReductionAlgorithm`, or the tab templates.

---

## 0. Scope decisions (locked)

| Question | Decision | Why |
|---|---|---|
| Merge trigger | **Both** — a `RibbonMergeSource` attached property that auto-merges on child activation, *plus* imperative `Merge()` / `Unmerge()` | Mirrors the injection decision already locked for MDI. The attached path is a thin wrapper over the imperative one, so there is one code path to test. |
| Merge granularity | **Whole tabs *and* groups into existing host tabs** | Real Office does both — a child adds its own tabs *and* can inject a group into the host's Home tab. Building tab-only first and retrofitting group merge later would mean reworking the ordering model, which is the expensive part. |
| Modal tab scope | **Office Print Preview behaviour** — other tabs hidden, QAT stays, application/File button hidden, minimize blocked, backstage blocked | What Word actually does; least surprising. Flags can be added later without breaking anyone. |
| Merged tabs vs customization | **Invisible to customization entirely** — excluded from serialization and from the Customize-the-Ribbon tree | Merged tabs belong to a transient child. Persisting their order or visibility would restore stale state into a ribbon whose source no longer exists. See §5, this is the single most dangerous interaction in the feature. |

Explicitly **out of scope**: merging QAT items from a source (the child's commands can be added to
the QAT by the user once merged, through the existing mechanism); merging backstage pages; nested
merge sources (a merged child that itself hosts merge sources).

---

## 1. Where it lives

```
src/RibbonKit/
  Controls/
    RibbonMergeSource.cs      NEW — the contribution container + attached property
    RibbonMergeService.cs     NEW — merge/unmerge bookkeeping, owned by Ribbon
    RibbonModalScope.cs       NEW — modal-tab state machine + events
    Ribbon.cs                 EDIT — Merge/Unmerge/EnterModal/ExitModal surface, service wiring
    RibbonTab.cs              EDIT — IsModal, CanClose, IsMerged (read-only)
    RibbonCustomizationSerializer.cs  EDIT — skip merged tabs, use authored visibility
    RibbonCustomizePage.cs    EDIT — filter merged tabs out of the tree
  Themes/
    Office2024.xaml           EDIT — modal tab close button in the tab strip
```

Following the house convention, the new controls are lookless and templated in the shared
`Office2024.xaml` dictionary; anything visual reads tokens, never literal colors.

---

## 2. Tab merging

### 2.1 The contribution container — `RibbonMergeSource`

```csharp
[ContentProperty(nameof(Tabs))]
public class RibbonMergeSource : FrameworkElement   // not in the visual tree; a declarative bag
{
    public ObservableCollection<RibbonTab> Tabs { get; }              // whole tabs to contribute
    public ObservableCollection<RibbonGroupContribution> Groups { get; } // groups into host tabs

    public int Order { get; set; }          // ordering hint, see §2.3
    public bool IsMerged { get; }           // read-only, set by the service

    // Attached, for the automatic path:
    public static RibbonMergeSource GetMergeSource(DependencyObject d);
    public static void SetMergeSource(DependencyObject d, RibbonMergeSource value);
}

public class RibbonGroupContribution : DependencyObject
{
    public string TargetTabId { get; set; }   // host tab's Ribbon.CommandId
    public RibbonGroup Group { get; set; }
    public int Order { get; set; }            // position hint within the target tab
}
```

`RibbonMergeSource` deriving from `FrameworkElement` (rather than `DependencyObject`) gives it
resource inheritance and a `DataContext`, so a merged tab's bindings resolve against the child's
view model — which is the whole point in the MVVM case.

### 2.2 The service — `RibbonMergeService`

Owned by `Ribbon`, one instance per ribbon, internal. Responsibilities:

- `Merge(RibbonMergeSource source)` — insert the source's tabs into `Ribbon.Tabs` at the position
  the ordering hint dictates; insert each group contribution into its target tab's `Groups`.
- `Unmerge(RibbonMergeSource source)` — remove exactly what was inserted and restore the host tabs'
  original group order.
- Track, per source, a `MergeRecord`: the tabs inserted, the (target tab, group, original index)
  triples for group contributions, and whether the selected tab at merge time belonged to the source.
- Re-assert the merge if the tab collection is rebuilt underneath it (see §5.3).

`Ribbon.Tabs` is a read-only `ObservableCollection<RibbonTab>` exposed through `TabsPropertyKey`;
the service mutates the same collection the app does, so all existing machinery — `OnTabsCollectionChanged`,
the selection re-assert, `FindFirstVisibleTab()` — keeps working with no changes.

### 2.3 Ordering

Merged tabs need a position that is **stable across repeated merge/unmerge cycles** and independent
of how many other sources are currently merged. Index arithmetic at merge time does not give that.

Proposal: an `Order` int on the source (default 0), and insertion by a stable comparison against
the tabs already present:

- Host-declared tabs have an implicit order of 0 and always sort before merged tabs of order ≥ 0.
- Merged tabs sort by `(Order, merge sequence number)` — so two sources with the same `Order` land in
  the order they were merged, and a source that unmerges and re-merges returns to the same slot
  relative to its peers.
- A negative `Order` places a source's tabs *before* the host's own tabs (Office does this for some
  add-ins; cheap to support, and impossible to add later without changing the default meaning of 0).

Contextual tabs are unaffected — they're host-declared and keep their authored position.

### 2.4 The automatic path

`RibbonMergeSource.MergeSource` attached to any `FrameworkElement`. The service watches that
element for activation and merges/unmerges accordingly. "Activation" is deliberately *not* WPF
focus — focus moves to ribbon buttons constantly (see the RichTextBox lesson in
[`04-DESIGN-NOTES.md`](../04-DESIGN-NOTES.md) §3.28), which would thrash the merge. Instead:

- `MdiContainer.ActiveDocument` changing drives it in the MDI case (the container already raises
  `ActiveDocumentChanged`).
- For non-MDI hosts, an explicit `RibbonMergeSource.SetActive(element, bool)` attached property the
  app toggles.

So the automatic path is really "declarative, driven by an activation signal", not "magic".

### 2.5 Merged-tab appearance

Reuse `RibbonTab.IsContextual` / `ContextualColor` / the read-only `ContextualBrush` rather than
adding a parallel tint mechanism. A merge source that wants its tabs visually distinguished sets
`IsContextual` on them and a `ContextualColor`; the existing per-theme contextual rendering handles
2024 / 2019 / 2013 / 2010 already. No new tokens.

---

## 3. Modal tabs

### 3.1 API

```csharp
// RibbonTab
public bool IsModal { get; set; }     // this tab, when entered, is exclusive
public bool CanClose { get; set; }    // shows the close (×) affordance in the tab strip

// Ribbon
public RibbonTab? ModalTab { get; }               // read-only, null when not modal
public void EnterModal(RibbonTab tab);
public void ExitModal();
public event EventHandler<RibbonModalEventArgs>? ModalEntering;  // cancellable
public event EventHandler<RibbonModalEventArgs>? ModalEntered;
public event EventHandler<RibbonModalEventArgs>? ModalExiting;   // cancellable
public event EventHandler<RibbonModalEventArgs>? ModalExited;
```

Cancellable enter/exit matters for the Print Preview case: the app may want to refuse exit while a
print job is spooling, or refuse entry when there's no document.

### 3.2 What entering modal mode does

| Element | Behaviour | Mechanism |
|---|---|---|
| Other tabs | Hidden | `Visibility = Collapsed`, prior value recorded (§5.1) |
| The modal tab | Selected, shows a close button if `CanClose` | Template trigger on `IsModal` + `CanClose` |
| Quick Access Toolbar | Stays visible and functional | No change |
| Application / File button | Hidden | `Ribbon` sets its host collapsed while `ModalTab != null` |
| Minimize | Blocked | `IsMinimized` setter no-ops; double-click and Ctrl+F1 ignored |
| Backstage | Blocked | `IsBackstageOpen` setter no-ops while modal |
| KeyTips | Only the modal tab's chain | Free — `KeyTipService` already filters on `tab.Visibility == Visible` |
| Contextual tabs | Hidden like any other tab, restored on exit | Same recorded-visibility path |

The KeyTips row is worth calling out: `KeyTipService` builds its chain by iterating `_ribbon.Tabs`
and skipping anything not `Visibility.Visible`, so hiding tabs the ordinary way gives correct
KeyTip behaviour for free. The only requirement is that the service rebuilds its adorners after
the mode change — a `RefreshSelectionVisuals()`-style call.

### 3.3 Selection

`Ribbon` already reselects the first visible tab when the selected tab becomes invisible
(`OnTabIsVisibleChanged` → `FindFirstVisibleTab()`). Entering modal mode therefore needs to select
the modal tab **before** collapsing the others, or the intermediate state will briefly select the
wrong tab. Exiting restores the tab that was selected before entering, falling back to
`FindFirstVisibleTab()` if that tab is gone (it may have been unmerged in the meantime).

---

## 4. Theming & visuals

Both features are mostly logic, but three visual pieces are needed:

1. **Modal tab close button** — a small × at the right end of the tab strip row (Office puts it at
   the far right of the strip, not on the tab itself). New tokens: `Metrics.ModalClose.*` and
   `Brushes.ModalClose.{Foreground,HoverBackground}` in all four token dictionaries.
2. **Selection visuals must be refreshed** after any merge, unmerge, or modal transition. The 2010
   and 2013 themes draw the connected-tab notch via `RibbonTabControl.UpdateConnectNotch`, and the
   sliding underline is a shared animated element — both are positioned from the selected tab's
   transform, so a tab-collection change invalidates them. `Ribbon.OnThemeConfigurationChanged`
   already calls `RefreshSelectionVisuals()`; merge/modal transitions must call the same thing.
   **This is the most likely source of "the notch is in the wrong place" bug reports.**
3. **Strip scroll** — merging can push the tab strip past its width. `RibbonScrollContentHost`
   handles the chevrons already (§3.25), but the merge must invalidate its extent, and after a
   merge that adds the now-selected tab the strip should scroll that tab into view.

Merged tabs animate in with the existing `RibbonAnimationAction.ContextualTab` open animation
(reusing what `RibbonTab.OnIsVisibleChanged` already plays). Modal enter/exit gets a cross-fade of
the tab strip, honouring the reduced-motion switch like everything else. House rule holds:
transform and opacity only, never `Width`/`Height`/`Margin`.

---

## 5. Sharp edges (read before writing code)

### 5.1 The serializer captures live visibility — modal mode would corrupt saved state

`RibbonCustomizationSerializer.Capture` writes `Visible = tab.Visibility == Visibility.Visible`
for every tab. **Saving the ribbon state while a modal tab is active would persist every other tab
as hidden**, and `ApplyLayout` faithfully restores `tab.Visibility = Collapsed` on the next run —
a ribbon with one tab and no way back. Contextual tabs have a milder version of the same problem
today, but modal mode makes it systematic.

Fix: an internal notion of **authored visibility**. `RibbonModalScope` records each tab's
`Visibility` before collapsing it; the serializer reads `Ribbon.GetAuthoredVisibility(tab)`, which
returns the recorded value while modal and the live value otherwise. Same helper covers any future
code that hides tabs temporarily.

### 5.2 The serializer would happily persist merged tabs

`Capture` iterates all of `ribbon.Tabs` with no notion of provenance. A merged tab would be written
with whatever `Ribbon.CommandId` it carries (or none). On the next run `ApplyLayout` looks the id up
in `builtInTabs`, fails, and hits `continue` — so it is *silently dropped*, which is survivable —
but the saved order still contains a phantom entry, and any group contribution merged into a host
tab **would be captured as part of that host tab's layout** and then re-created as a customization
on restore. That one is not survivable.

Fix, per the locked decision: mark merged tabs and contributed groups with a read-only
`Ribbon.GetIsMerged(element)`, and have `Capture` skip them entirely — both at tab level and inside
`ReconcileGroups`' capture counterpart. `RibbonCustomizePage` filters on the same flag so merged
content never appears in the customize tree.

### 5.3 `ApplyLayout` clears and rebuilds `Ribbon.Tabs`

Step 4 of `ApplyLayout` does `ribbon.Tabs.Clear()` then re-adds the desired tabs followed by any
extras. Merged tabs would land in `extraTabs` and be re-appended **at the end**, losing their merge
ordering, and any `MergeRecord` holding indices would be stale.

Fix: the merge service subscribes to the tab collection and re-asserts merged positions after a
bulk rebuild, or — simpler and probably better — `Apply` unmerges every source first, applies the
layout, then re-merges. Decide this during implementation; the re-merge approach is easier to
reason about and customization application is a rare, user-initiated operation.

### 5.4 QAT proxies point at controls that can leave the tree

The QAT holds proxies to source controls (§3.19), including dropdown/split proxies that borrow the
source's menu. If a user adds a merged control to the QAT and the source then unmerges, the proxy
points at an orphan. Options: drop those QAT entries on unmerge (surprising — they vanish), or park
them disabled and restore on re-merge (better, matches how Office greys out unavailable commands).
Either way `RibbonCustomizationSerializer` must not persist a QAT entry whose source is merged —
same `IsMerged` filter as §5.2.

### 5.5 Unmerge must restore host-tab group order exactly

Group contributions insert into a host tab's `Groups` collection. If two sources contribute into the
same tab and unmerge in the opposite order, naive index restoration puts groups back in the wrong
places. Store `(group, originalIndexInHostTab)` and restore by re-inserting relative to the
surviving host-declared groups, not by absolute index.

### 5.6 Merging while modal, and modal on a merged tab

Both are reachable: a child activates while Print Preview is open, or a merged tab is itself modal.
Rule: **modal mode wins** — a merge that happens while modal inserts tabs in the collapsed state and
records them as modal-hidden, so exiting modal reveals them correctly. Unmerging the tab that is
currently modal forces `ExitModal()` first (non-cancellable in that path, since the tab is going away).

### 5.7 Design-time

The Ribbon Editor (§3.23) and the design-time preview enumerate tabs from the XAML tree. Merged
tabs don't exist at design time (no activation), which is fine — but the editor must not choke on a
`RibbonMergeSource` declared in the XAML. Simplest: the editor ignores merge sources entirely in v1.

---

## 6. Phased build

Each phase ends with a showcase page and tests green, matching the house rhythm.

- **P7.1 — Modal tabs.** `IsModal` / `CanClose` on `RibbonTab`, `RibbonModalScope`, enter/exit with
  cancellable events, authored-visibility helper (§5.1), close button template + tokens in all four
  themes, blocked minimize/backstage. *Exit:* a Print Preview tab in the showcase enters and exits
  cleanly; saving state while modal round-trips correctly; KeyTips show only the modal chain.
  *Why first:* smaller, self-contained, and it builds the hide/restore-a-subset-of-tabs plumbing that
  merging then reuses.

- **P7.2 — Tab merging (whole tabs).** `RibbonMergeSource`, `RibbonMergeService`, imperative
  `Merge()`/`Unmerge()`, ordering model (§2.3), serializer and customize-page exclusion (§5.2, §5.3),
  selection-visual refresh (§4). *Exit:* the showcase merges and unmerges a source repeatedly with
  stable ordering, correct notch/underline in 2010 and 2013, and no leakage into saved state.

- **P7.3 — Group contributions + the attached path.** `RibbonGroupContribution`, host-tab group
  restore (§5.5), the `MergeSource` attached property and activation signal (§2.4), QAT proxy
  handling (§5.4). *Exit:* a source injects a group into the host's Home tab and removes it cleanly;
  two sources into the same tab unmerge in any order.

- **P7.4 — MDI M4 wiring.** Drive merging from `MdiContainer.ActiveDocument`, then the *separate*
  caption-merge site for a maximized child's icon and window buttons. *Exit:* maximizing an MDI child
  reproduces authentic Office MDI — child caption in the ribbon row, its tabs merged.

---

## 7. Testing

Unit-testable without opening rendered windows, keeping the suite deterministic and CI-friendly:

- Ordering: merge A(0), B(0), C(-1) in every permutation → same final tab order.
- Merge/unmerge round-trip: tab collection identical to the starting state after N cycles.
- Group contribution restore with two sources into one host tab, unmerged both ways.
- Serializer: capture-while-modal round-trips to the pre-modal visibility; merged tabs and their
  groups never appear in the JSON.
- Modal: enter/exit selection restoration, cancellation honoured, unmerge-of-modal-tab forces exit.

Visual, on Windows: notch and underline position after merge/unmerge and modal transitions in 2010
and 2013 at 100–200% DPI; strip scroll when merging past the available width; close-button hover
in all five themes.

---

## 8. Open questions to settle during P7.1

- **Close button placement** — far right of the tab strip row (Word's Print Preview) or on the modal
  tab itself? The former needs a slot in the tab strip template that's empty the rest of the time.
- **`Ribbon.Merge` return value** — void, or a disposable merge handle that unmerges on `Dispose()`?
  The handle is tidier for `using` scopes but adds a type to the frozen v1 surface.
- **Does `ExitModal` need a reason?** (user clicked ×, app called it, tab unmerged) — cheap to add to
  `RibbonModalEventArgs` now, awkward later.
- **Automation peers** — does a merged tab need to announce its provenance to UIA, or is it
  indistinguishable from a host tab? Leaning indistinguishable.
