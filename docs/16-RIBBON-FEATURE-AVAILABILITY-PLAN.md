# Ribbon feature availability

> F1 contract reviewed and the eight-property F2 implementation authorized by the user
> on 2026-10-15, on `codex/api-and-component-updates`. Current verification is below.
> Toolbox definitions and host transition notifications remain subsequent tracks.

## Approved contract

All eight Boolean dependency properties default to `true` and support ordinary
XAML, styles and bindings. They apply per Ribbon. Designer edits are authored
runtime policy, with one Model API undo transaction per edit.

| Property | Disabled behavior |
| --- | --- |
| `IsSimplifiedRibbonEnabled` | Effective Classic layout; preserve the requested LayoutMode and its binding for re-enable. Explicit attached child scopes cannot bypass the owning Ribbon. |
| `CanUserCustomizeRibbon` | No user Classic or Simplified editing, pin/remove actions or customization request route. QAT customization is independent. |
| `CanUserCustomizeQuickAccess` | No user QAT addition, removal or reorder; preserve existing membership and objects. |
| `CanUserRepositionQuickAccess` | No user title-bar, tab-row or BelowRibbon placement changes. |
| `CanUserToggleQuickAccessVisibility` | No user Show/Hide QAT action. |
| `CanUserMinimizeRibbon` | No user minimize/restore action; does not force expanded layout. |
| `CanUserChangeLayoutMode` | No user layout toggle; host-selected Simplified remains usable when its capability is enabled. |
| `CanUserChangeDensity` | Host-owned Compact/Touch selectors bind to this permission; host-selected density remains usable. |

```xaml
<rk:Ribbon IsSimplifiedRibbonEnabled="{Binding SupportsSimplified}"
           CanUserCustomizeRibbon="False"
           CanUserRepositionQuickAccess="False"
           QuickAccessPosition="BelowRibbon" />
```

The seven `CanUser` settings govern user operations. Host-authored collections,
state properties, `AddToQuickAccess`, and serializer `Apply` remain application
configuration/restoration APIs. `PinToSimplifiedRibbon` and
`RemoveFromSimplifiedRibbon` remain host operations but reject unavailable
Simplified capability. These mutable properties are application policy, not a
security boundary against code that owns the controls.

The minimize header chevron and tab double-click enforce minimization permission;
the chevron follows host state with OneWay binding and a guarded click handler.
Library menus omit disallowed actions and recheck permission when invoked,
including cached QAT menus and stale menu references. Standalone customization
pages observe owner policy even before Loaded and preserve a host's own IsEnabled
setting. Simplified Apply rejects disabled capability/customization before any
mutation, retains its draft, and rebases before later Apply. Classic visibility
checkboxes and individual operation handlers also enforce permission.

User Import/Reset respects QAT membership, placement and visibility permissions
independently, including whole-layout imports through either Ribbon editor.
Locked QAT membership is skipped rather than rebuilt, preserving live objects.
User addition also preserves a hidden QAT when user visibility changes are locked;
the host AddToQuickAccess API retains its existing reveal behavior.
Application startup restoration through serializer Apply deliberately remains
available. Policy properties are never serialized as end-user customization.

## Simplified capability lifecycle

Coercion preserves the requested LayoutMode, including a TwoWay binding's source.
Re-enable evaluates the latest request; it does not restore a separate saved mode.
Private inherited capability state covers explicitly scoped library presentation;
each nested Ribbon establishes its own independent boundary. Collection-owned tabs
receive that state before their templates supply a logical parent, and release it
on removal/Reset. Existing command placement, size/label and saved profiles remain
intact while inactive.

Disabling an active Simplified layout uses existing deferred popup cleanup after
click/command dispatch. Borrowed native content returns, Simplified copies and
full-group references release, motion/KeyTip levels cancel, and ordinary QAT
membership remains intact. Focus returns after layout to a usable Classic source
or tab, with generation and external-focus checks. No animation delay is required.

Custom templates and host-owned selectors must honor policy. Showcase's main
layout/density selectors demonstrate the public contract; the host continues to
own their selection and appearance persistence. RibbonKit cannot discover and
remove arbitrary application UI.

## Designer and subsequent tracks

Properties metadata and Ribbon Editor expose the eight authored Booleans. The
root property panel now supports those settings; command placement/label rows
remain editable with an inactive explanation while Simplified is unavailable.
Keep net472/string-type/Model API isolation and accepted Inline/Medium defaults.

The complete include/exclude matrix and package-versus-project-reference discovery
review precede any [Toolbox implementation](17-TOOLBOX-ITEM-DEFINITION-PLAN.md).
[Host transition notifications](18-HOST-LAYOUT-TRANSITION-PLAN.md) require their own
API discussion. Context-menu master switches, import/export/reset switches and
fine-grained tab/group permissions are deferred, not part of this implementation.

## Verification and acceptance

F2 is implemented. User review on 2026-10-15 confirmed the eight original gates
were working properly and the reported post-implementation binding problems were
resolved. That behavior is accepted within the user's reviewed scope. Automated
evidence below remains assembled from one full Release run and final affected
checks; the specific designer/DPI/reduced-motion gates below remain separate.

Recorded 2026-10-15 on `codex/api-and-component-updates`, based on clean `2f8f525`:

| Gate | Result |
| --- | --- |
| Final normal Release solution build | Passed, 0 warnings/errors, net8/net9 runtime and net472 DesignTools; API/XML analyzers enabled |
| Full Release runtime project | 845/846 passed. The existing RTL Classic overflow-opening focus case failed at line 42; its exact isolated retry passed 1/1 in a fresh process. Cause remains unestablished; this is not a clean full-project pass |
| Full Writer project | 482/482 passed; retained without repeating the project |
| Full independent consumer | Aggregate passed, with 38 reported scopes; includes 224 Simplified layout combinations, 112 customization combinations and 12 tab-move cases. Retained without repeating its passing prefix |
| Final affected runtime | 28/28 passed after final minimize-chevron/tab-double-click, QAT visibility/addition, focus and host-selector changes; includes inherited-scope reparenting, host binding preservation, minimized theme geometry and the three additional policy cases. In total, all 22 new policy cases have passing evidence across the full run/final selection |
| Final feature consumer | Passed: 12 public-only combinations over Office2010, Office2024Flat and Crystal, LTR/RTL and Compact/Touch, with two independently configured Ribbons, authored XAML, scoped coercion, re-enable and hidden/disabled minimize chevron |
| Full visual project | Four selection/harness tests passed; matrix stopped at `crystal-light-qat-below-100`, 5,551 significant pixels. Its actual/diff were inspected and its actual PNG is byte-for-byte unchanged from the retained pre-slice image. The two known Crystal comparisons remain open; later matrix scenes were not rerun/captured in this slice |
| Packaging | Normal Release pack and archive/designer placement validation passed; clean net8/net9 WPF package consumers built with 0 warnings/errors. Packaged runtime/DesignTools DLLs match final normal Release output. Previous packages preserved; current pair selected in a validation-input directory |
| Normal Debug review output | Full Debug solution build passed, 0 warnings/errors, including Showcase and Writer; no task-specific configuration/output and no application launch/live review |
| Showcase selector binding repair, 2026-10-15, after `977c3e6` | Reproduced all three reported ElementName source errors before View was realized. Explicit-owner bindings passed 7/7 focused Release cases: validated binding-trace regression across detached, Classic/Simplified and QAT presentations with disable/re-enable, plus the six existing density/QAT cases. Normal Debug Showcase build succeeded, 0 errors and 12 designer-copy warnings because Visual Studio held DesignTools.dll; no process was closed. No runtime/public API change or live/native acceptance claim |
| Context-menu binding repair, 2026-10-15, after `f5343d7` | Reproduced both reported MenuItem alignment errors through Showcase's routed right-click path with CanUserCustomizeRibbon disabled. Conditional entries are now created only when included. 12/12 focused Release regressions passed, including six Showcase policy cases over background/command menus and re-enable, plus existing stale-handler/user-add checks. Independent consumer passed 12 theme/direction/density combinations with validated empty menu binding traces, re-enable and empty-menu suppression. Normal Release net8/net9 library build passed, 0 warnings/errors. Normal Debug Showcase build succeeded, 0 errors and the same 12 Visual Studio designer-copy lock warnings. No new public API; physical input remains separate |
| RTL lab preview follow-up, 2026-10-15, after `d04b4ab` | Reproduced all three reported ElementName errors during window startup. Explicit-source preview bindings passed 2/2 focused Release cases: validated empty binding trace with live font/style/sample updates before tab realization, direct and QAT selection, LTR/RTL and Classic/Simplified; existing RTL native gallery reopen regression also passed. Normal Debug Showcase build succeeded, 0 errors and the same 12 Visual Studio designer-copy lock warnings. Host wiring only, no runtime/public API change; original bindings predate feature flags, but first occurrence of the errors is unestablished |

Logs are `TestResults/feature-policy-20261015-*.log`; full and final-selection TRX
files are in `TestResults/feature-policy-20261015/`. Passed projects/scopes were
retained. Final source changes were checked with the affected selection and the
feature consumer, not another complete solution run. No approvals/tolerances were
changed. Nothing committed, pushed, merged, published or scheduled.

The binding-repair logs are `TestResults/showcase-feature-binding-*-20261015.log`;
the pre-fix trace is `TestResults/showcase-feature-binding-before-trace-20261015.txt`.
Its regression writes the post-fix trace under the normal Release test output's
`TestResults/showcase/feature-selector-binding-trace.txt`. Earlier full-run and
independent-consumer evidence remains retained; the binding repair changed only
Showcase selector wiring and did not repeat those gates.

The context-menu follow-up logs are `TestResults/context-menu-binding-*-20261015.log`;
the pre-fix capture is `TestResults/context-menu-binding-before-trace-20261015.txt`.
The six Showcase trace files are in the normal Release runtime test output's
`TestResults/showcase/`; the independent consumer's twelve traces are in its
normal Release output's `TestResults/feature-availability/`. All post-fix traces
were empty. This bounded library fix retained prior full-suite, visual and
package evidence without repeating those gates.

The RTL preview logs are `TestResults/rtl-selection-preview-*-20261015.log`;
retained before/after traces are `TestResults/rtl-selection-preview-*-trace-20261015.txt`.
The post-fix trace is empty. No independent-consumer or broader gates were
repeated for these host-owned preview bindings; existing evidence remains retained.

New focused coverage includes defaults, binding/source preservation, explicit
scopes/reparenting, nested Ribbons, direct/stale menu handlers, detached page
eligibility, inert/re-enabled drafts, independent import scopes, popup return and
focus. The independent public-only consumer checks two Ribbons across Office2010,
Office2024Flat and Crystal, both directions and densities (12 combinations).

Live Visual Studio property/edit/undo/reload acceptance, genuine
mixed-monitor DPI and OS reduced-motion review remain separate gates. Existing
Simplified acceptance and assembled end-of-feature evidence are retained in
[plan 15](15-SIMPLIFIED-RIBBON-PLAN.md#end-of-feature-validation-and-repairs--2026-10-14).
Its two residual Crystal QAT comparisons remain open; neither the earlier feature
nor this slice is described as a clean single full-suite pass without that evidence.
