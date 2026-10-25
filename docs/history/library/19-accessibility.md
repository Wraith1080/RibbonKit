# Library history: Accessibility

> Shared implementation details and regression pitfalls. Current acceptance belongs
> in the [1.1 plan](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md).

[History directory](../01-library.md)

### 3.276 Selected-tab command accessibility — 2026-10-24

The shared `RibbonTabControl` template's selected-content presenter was unnamed.
WPF's stock `TabItemAutomationPeer` retrieves `PART_SelectedContentHost` from its
owning TabControl before traversing the selected content. Without that name,
external Control View traversal stops at the tab/header even though direct leaf
peers and pointer/keyboard commands work. Restoring the standard part name reaches
the existing group/command peers; no replacement peer, public API or host glue is
needed. The selected-content aliases remain intact.

Naming the part also activates WPF's alignment propagation from the selected tab.
The old unnamed presenter stretched, while an unstyled tab alignment defaults to
Left/Top: a naming-only independent probe shrank a 972-DIP body to 133.6 DIPs.
The shared RibbonTab style now defaults both content alignments to Stretch,
retaining the original body geometry. Local values and application bindings
outrank those setters; do not replace consumer alignment bindings in code.

The regression traverses the stock tab peer hierarchy, rather than constructing
the leaf peer directly. It covers source Invoke/Toggle, bound names, CanExecute,
enabled state, source-to-QAT synchronization, two-way selected-tab changes and
binding identity across selection, plus explicit authored alignment precedence.
Four Office2024/Crystal LTR/RTL cases failed at leaf discovery before correction.
The first attempted fixture used the wrong outer template part and is retained
only as a setup failure. `ContentSource` uses TemplateBinding, so ordinary
`BindingOperations.IsDataBound` is not a valid alias assertion; test live content
identity across selection changes instead.

An owned offscreen public-only consumer and separate managed UIA client also
reproduce the pre-fix missing source leaves, then discover/invoke/toggle them
after correction. Final body width returns to 972 DIPs with live bindings retained.
This is external-process evidence without desktop input or user-app operation;
it is separate from actual Showcase/Writer Inspect acceptance. Source and logs
are retained under `TestResults/v1.1-tab-accessibility-20261024/`.

That consumer separately exposes QAT Bold as an `ItemsControlItem` DataItem without
a Toggle pattern. Source-to-copy state synchronization works, but it does not
establish accessible QAT activation. Keep RKWF-003 open rather than treating a
named/discoverable wrapper as an actionable control. The subsequent bounded
correction is recorded below. Backstage remains RKWF-004.

### 3.277 Actionable QAT and overflow automation — 2026-10-24

WPF's generic ItemsControl item peer reports DataItem semantics and does not
forward command patterns. `RibbonCommandItemsAutomationPeer` retains WPF's
ItemsControl/ItemAutomationPeer hierarchy, collection lifetime and native event
redirection, while forwarding each realized control's role, class and patterns.
The internal `RibbonOverflowItemsHost` uses the same peer for QAT/Simplified
overflow. It remains an ItemsControl with inherited layout, items, bindings and
style key; template part names are unchanged.

A plain forwarded Toggle pattern would only run native OnToggle, omitting the
copy's source Click/Command completion. Its internal provider therefore uses the
existing KeyTip activation path, rejecting disabled owners before activation and
reporting the original native ToggleState. Source-linked two-way state changes,
Checked/Unchecked, routed Click, CommandParameter and CommandTarget retain their
existing path; do not toggle the source again or attach a duplicate Command.
Other patterns retain the original providers.

The tab-row QAT is inside a TabControl template. The stock tab peer publishes tab
items only, so correcting item semantics alone still leaves that QAT undiscoverable
externally. `RibbonTabControlAutomationPeer` retains stock tab selection/content
behavior and adds the existing named QAT part. Retaining that part while hidden
lets WPF visibility filtering and subsequent placement changes reuse the bridge.
No bindings or authored item collections are rewritten.

Two additive protected OnCreateAutomationPeer overrides are recorded in Unshipped
for RibbonQuickAccessToolBar and RibbonTabControl. These implement an existing WPF
extension point; helper types/providers remain internal. Shipped API, public
settings, dependency properties and serialization schema are unchanged.

Eleven new runtime cases reproduce the original missing role/patterns, then cover
all QAT placements and LTR/RTL, QAT/Simplified overflow, disabled guards, live names,
binding identity, command counts, collection refresh and a hand-authored native
three-state toggle. Direct in-process peer queries must ResetChildrenCache after
item/selection changes when no native event listener exists; InvalidatePeer alone
does not synchronously rebuild those children. Preserve overflow's established
close-on-command behavior, including toggles, rather than assuming the collapsed
group's stay-open policy applies.

A separate managed UIA client against an owned public-only consumer verifies all
three placements, including Crystal/Touch/RTL: Button role, Toggle/Invoke,
disable/re-enable, live names, retained source/copy bindings, one command execution
per toggle, parameters, removal/re-addition and native structure notifications.
This uses owned offscreen windows and no desktop input or user-app operation.
Source/logs are retained in `TestResults/v1.1-qat-accessibility-20261024/`.

Structural tests that insist on the old exact ItemsControl type must instead
check the compatible part contract, or explicitly pin the new host where type
selection is the tested behavior. Keep their KeyTip, gallery-paint and keyboard
assertions intact. The older runtime/consumer combo and gallery keyboard fixtures also used
Down to open an overflow submenu after Escape; accepted menu navigation now
reserves Up/Down for sibling rows, so those steps must use the LTR/RTL forward key.
The consumer's last-choice check enters the submenu then uses End, preserving its
boundary/scroll assertions. The focused helper's AfterFeatureAvailability
continuation resumes at Simplified customization when an earlier continuation
exhausts its time budget; the original unfiltered chain and timeouts are unchanged.
Current combined/focused evidence and actual-consumer review
remain in the [active plan](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md).

### 3.278 Source toggle automation completes host actions — 2026-10-25

Correct source discovery and ToggleState do not prove host command execution.
Writer's original Bold peer inherited WPF's state-only Toggle implementation,
skipping the Click handler that applies document formatting. The QAT item provider
already used full activation, explaining the original/copy difference.

The existing RibbonToggleButtonAutomationPeer and RibbonCheckBoxAutomationPeer now
privately reimplement their existing IToggleProvider.Toggle operation: reject a
disabled owner, then call its established native OnClick path through
InvokeFromKeyTip. Native pattern lookup, ToggleState, state transitions and metadata
remain inherited. No public member/baseline, template or Writer glue changes are
needed. Keep this correction at the automation request boundary: changing
IsChecked through a binding must remain a state projection without invoking a
host command. QAT's provider still invokes the native path directly rather than
calling the source automation provider, so it cannot execute both paths twice.

Four runtime regressions cover toggle/checkbox and two-/three-state cycles,
state → Click → targeted RoutedCommand ordering, parameters, disabled rejection
and source binding identity. Install a routed command's target handler and target
binding before the Command binding evaluates CanExecute; otherwise the fixture
starts disabled and cannot reproduce the missing Click/Command stages.

Writer's regression compares the original leaf provider with the real QAT item
provider and asserts selected-text FontWeight, retained selection, checked state
and one source Click per action. Its older Toggle helper calls InvokeFromKeyTip
directly and therefore did not cover this automation-only hole. A separate UIA
client against a public-only owned consumer verifies source toggle, source checkbox
and QAT roles/actions, command counts, parameters, disabled guards and retained
bindings without desktop input or user-app operation. Focused results and remaining
manual acceptance belong to S18 in the [active plan](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md).

The final candidate continuation exposed CommandCopyChecks' obsolete ToggleAndClick
helper: public UIA Toggle already completes activation, so its added synthetic
Click executed the formatting command twice. The fixture now sends one Toggle
request and checks one source Click, alongside retained selection, formatting,
state and ordinary/split assertions. Its 72-layout independent consumer matrix
passes 144 Bold round trips and 288 ordinary/split executions. No provider/runtime
change is needed. The AfterDropDownScrollbars continuation preserves prior passes
and runs the remaining common assertions and later scopes; the default unfiltered
chain is unchanged. The [final candidate record](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#final-candidate-validation--2026-10-25)
owns the failed attempts and assembled evidence.

### 3.279 Backstage page-content automation — 2026-10-25

Backstage inherited the stock TabControl peer but its templates did not name the
selected-content presenter. External traversal therefore exposed navigation items
without native page controls. Naming PART_SelectedContentHost would also propagate
navigation-item content alignment into the page: Crystal's Center/Left defaults
and authored alignment bindings must not change the existing stretched body.

The internal BackstageAutomationPeer keeps stock TabControl navigation/selection
and appends the native peers beneath the existing ContentArea template host.
It preserves a custom host's own peer when present. A custom template with the
standard PART_SelectedContentHost already exposes its page and skips the bridge
to avoid duplication. No XAML, alignment, content ownership or Writer production
code changes are needed. Existing peers are invalidated after template application
and actual Backstage page selection; descendant list-selection events are excluded.
The one additive protected OnCreateAutomationPeer override is recorded in
PublicAPI.Unshipped; its helper remains internal and Shipped is unchanged.

Ten runtime cases reproduce missing page actions before correction, then cover
five layouts in LTR/RTL: native Invoke, close/reopen, live metadata, page selection,
two-way selection/alignment bindings and unchanged body width. Writer's two cases
traverse the parent peer to actual recent-document buttons and invoke their real
open/close command. Directly constructing a leaf peer does not test discovery.
Writer's fixture has no Application.Current; apply appearance through its window.

An owned public-only consumer plus a separate managed UIA client verifies native
Control View buttons and lists across Modern, Classic2010, Classic2007,
CrystalFloating and CrystalSidebar, including page refresh without manually
resetting the peer cache. Modern also changes templates and returns. This uses
owned offscreen windows without desktop input or user-app operation. File-based
test requests must use atomic publication and server acknowledgement; an earlier
unacknowledged request/file-lock failure was a harness error, not a UIA defect.
Focused evidence and actual-consumer acceptance remain in S20 of the
[active plan](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md).

#### Navigation and active-page regions — 2026-10-25

The authorized hierarchy refinement retains the File Tab root and native item
peers, placing navigation under a Group and the whole current page under a Pane.
The regions use the existing NavColumn/NavSurface and ContentArea template
elements for geometry; no visual elements, focus stops or content bindings are
added. Page names come from the selected navigation container's native peer.
Native SelectionItem providers retain the original Backstage selection container.
Navigation collection changes refresh the same grouping peer. Region helpers stay
internal; the only new API in this refinement is the appended localized string
RibbonString.BackstageNavigation (91). Earlier enum values remain unchanged.
Localization refresh uses weak property-change subscription.

Custom templates with PART_SelectedContentHost retain the stock peer tree without
duplicating page controls. A custom template exposing ContentArea without a known
navigation host still exposes its page through a Pane, leaving navigation native.
An existing custom content-host peer remains beneath the page region.

Invalidating a synthetic region peer is insufficient to refresh its descendants.
Resetting its cache synchronously from OnSelectionChanged is also too early:
the selected page is correct, but the presenter has not realized the new children,
leaving an empty page in external Control View. Coalesce one library-owned
DispatcherPriority.Loaded refresh after selection/template application, rebuild
the root/region child caches, and invalidate the peers. Do not add a fixed delay,
host workaround or reset caches in the external client. Direct test traversal
resets only its root cache when there is no native event listener.

The parent-tree cases now assert region roles, separation, names, focusability,
native actions and retained bindings across five layouts/LTR/RTL. Authored-template
and live-localization cases cover the fallbacks and string compatibility. Writer's
actual recent-document cases invoke from inside the page Pane. An independent
client also checks original selection ownership, page/list selection, added/removed
navigation items and theme/template round trips. Its request reader retries a
transient Windows file-lock during atomic publication; that harness error is
separate from the reproduced early page-cache refresh. Evidence and the pending
actual-consumer hierarchy check belong to S21 in the
[active plan](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md).

#### Notification-safe region refresh and native item ownership — 2026-10-25

The actual Inspect/debugger review stopped at the navigation cache reset inside
GetChildrenCore with WPF's recursive Automation Peer API exception. A cache reset
can publish structure notifications and reconnect item providers through their
parent while that parent is still servicing a public query. Discovery now only
queues work; all root/region resets run in the coalesced Loaded callback. Keep
that callback marked pending/executing until it finishes so its own discovery
cannot schedule another refresh. Do not catch and suppress the recursive error.

Notification-enabled traversal also exposed the old grouping's item ownership
problem: item peers created by File's ItemsControl peer were then put beneath a
plain Group. The navigation Group now derives internally from TabControlAutomationPeer
and owns its native TabItem peers directly. File forwards Selection/ItemContainer/
Scroll patterns to that owner and uses the same item peers for native item events.
A narrow SelectionItem provider delegates native state/operations and preserves
File as the original SelectionContainer. Existing template-host geometry, native
page controls, metadata, bindings and authored-template fallbacks remain intact;
this correction adds no public API, XAML or Writer production glue.

The new regression registers a real scoped native structure listener on an owned
offscreen window, reads Control View metadata from a worker, exercises fresh peer
discovery and repeated page/item changes, and records first-chance recursive errors.
It requires delivered notifications, stable page selection and the original File
selection provider's current item. The independent client also subscribes to
structure/property notifications and checks zero recursive exceptions across five
layouts. The earlier traversal-only passes did not cover these notification paths;
the initial listener probes did not reproduce the exact debugger stop. Actual
Inspect acceptance remains in S21 of the
[active plan](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md).

### 3.280 Simplified row automation discovery — 2026-10-25

Actual Showcase Inspect traversal found an empty Home ListView in Simplified mode.
RibbonGroupsHost inherited WPF's generic ItemsControl peer, which enumerates source
items. Those Classic groups are hidden while a sibling RibbonQuickAccessToolBar
displays Simplified command copies. Direct leaf-provider tests miss this parent-tree
discovery gap.

An internal RibbonGroupsHostAutomationPeer now publishes that existing toolbar's
native peer in Simplified mode, reusing its existing item providers and source-linked
commands. Classic uses ItemsControlAutomationPeer traversal with item roles/classes
equivalent to WPF's internal generic wrapper: List/ItemsControl and
DataItem/ItemsControlItem. The peer getter only reads; the existing Simplified refresh
invalidates an already-created peer after rebuilding. It does not reset child caches
inside discovery or add another refresh coordinator.

The standard protected OnCreateAutomationPeer override is the sole additive API,
recorded in Unshipped with Shipped unchanged. A custom peer is necessary because
the default source-items traversal cannot discover this sibling toolbar. No XAML,
visual layout, host glue, persistence or command-binding changes are needed.

Four parent-tree regressions reproduce the failure, then cover Office2024/Crystal
in LTR/RTL, Toggle/Invoke, disabled guards, live names, retained bindings and
tab/Classic/Simplified round trips. The existing selected-tab/QAT cases also pass.
WPF's generic wrapper/item peers are internal; the replacement supplies a matching
item factory rather than borrowing a separate peer's item children.
The final round-trip assertion filters actionable Button roles: native label Text
peers may share a command's name and do not represent duplicate actions.

A separate public-only consumer/client checks Control View with native structure
and property subscriptions across the same four scopes, including Touch in
Crystal RTL, QAT synchronization, capability coercion/re-enable, delivered events
and zero recorded recursive peer errors. It never resets provider caches or operates
user applications. Focused evidence, the Debug designer-copy lock limitation and
actual Showcase recheck remain in S22 of the
[active plan](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md).

### 3.281 Collapsed opener and minimize accessibility — 2026-10-25

The final bounded accessibility preparation reproduced three gaps: a collapsed
group's native ToggleButton opener had no accessible name; the TabControl peer
omitted its sibling minimize chevron; and that chevron's native state-only Toggle
provider skipped the Click handler that changes Ribbon.IsMinimized. Merely naming
or publishing the old chevron would leave its action ineffective.

The collapsed opener now binds AutomationProperties.Name to its group header.
The existing tab peer also publishes MinimizeToggle's own native peer, retaining
hidden parts so WPF's visibility filtering can reconnect after policy changes.
The shared chevron uses the existing RibbonToggleButton with the same explicit
ToggleButton style/template and one-way IsChecked binding. Its native activation
already runs OnClick once, then the existing policy-guarded handler changes the
host state and refreshes the part binding. The accessible name reuses the existing
localized MinimizeRibbon string. No new public API, strings, theme tokens, layout,
host glue or persistence are added.

Keep the part contract compatible with ToggleButton subclasses. Three fixtures
that required the exact ToggleButton type now accept its base contract, retaining
their focus geometry and visual assertions. Six new regressions reproduce the
name/action gaps; their corrected run also covers live/explicit names, Toggle
state, single Click completion, policy rejection and both host/part bindings.
Existing affected focus/utility cases and selected utility snapshots pass without
changing approvals or tolerances.

A public-only consumer and separate UIA client traverse the owned main HWND and
its owned collapsed-popup HWND, with native structure/property subscriptions.
They verify popup command discovery, toggle stay-open policy, ordinary Invoke
dismissal, QAT synchronization/use while minimized, restoration and the named
chevron's Toggle/policy behavior across Office/Crystal and LTR/RTL, including
Crystal RTL Touch density. Providers are never reset by the client. Windows file
readers must permit delete sharing when the harness atomically replaces state
files; a transient locked-state failure was harness-only and its log is retained.

The S23 checklist's proposed single-click temporary minimized ribbon does not
exist in the current implementation. Minimize/restore uses the chevron, tab double
click or Ctrl+F1; this slice tests that shipped behavior rather than adding a new
presentation. Focused evidence and actual-consumer acceptance belong to S23 in the
[active plan](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md).

The final candidate consumer run found one further exact-type assumption in
CrystalUtilityPortabilityChecks' MinimizeToggle lookup. That lookup now accepts
the ToggleButton base contract; other part-type checks and the full utility
paint/native-action assertions remain intact. This is a fixture correction, not
a chevron/runtime change. The existing continuation from CrystalUtility receives
the full consumer's bounded 30-minute allowance for its large remaining Simplified,
customization and popup matrices. Actual
run/build results and retained-prefix limits belong to the
[final candidate record](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md#final-candidate-validation--2026-10-25).
