# Library history: Feature availability



Current evidence and remaining acceptance are in the [active plan](../../16-RIBBON-FEATURE-AVAILABILITY-PLAN.md).



### 3.256 Ribbon capability and user-operation policy — 2026-10-15



The user approved eight default-true Boolean properties: one Simplified capability

and seven independent user permissions for Ribbon/QAT customization, QAT position

and visibility, minimize/restore, layout mode and density. Keep host configuration

and startup restoration distinct from user operations; no generic feature registry

or secondary/fine-grained switches were introduced.



LayoutMode coercion retains its WPF base request and binding. Private inherited

capability state blocks explicit child scopes and respects nested Ribbon boundaries.

Tabs are collection-owned before templates provide logical parents, so propagate

capability to tab roots and clear it on removal/Reset. Test visual enforcement in

a realized consumer: a group's ItemsSource does not parent its controls before

containers are realized.



The header minimize chevron uses a guarded click and OneWay state binding; hiding
it alone would leave a direct TwoWay mutation route. Tab double-click also checks
permission and preserves the host binding with SetCurrentValue.

User menu handlers guard stale references as well as omitting disallowed entries.

Detached pages bind owner permission for eligibility, preserving host IsEnabled.

Public Simplified Apply cannot mutate disabled policy/capability and retains its

draft. User Import/Reset skips locked QAT scopes independently; public serializer

Apply remains administrative. Skip locked membership instead of serializing and

rebuilding the same list, which would replace live proxy objects. User Add must
also respect a locked hidden QAT; use a separate internal user route while the
public host Add API retains its existing reveal behavior.



Existing deferred mode cleanup returns borrowed popup content after invocation.

Focus restoration waits for committed layout, uses source identity, and guards

superseded callbacks or focus moved outside the Ribbon. ContextIdle tests must

drain below Background; the ordinary helper's default does not execute that work.



The root designer property panel now exposes policies without changing the

net472/string-type/Model API boundary or accepted Inline/Medium defaults. Main

Showcase selectors honor the public permissions. A public-only two-Ribbon

consumer covers three palettes, both directions and densities. Dated results and

native/designer limits belong in the active plan.

Showcase policy selectors originally used `ElementName=MainRibbon`. Commands on
unselected, collection-owned tabs have no Window namescope connection yet, so all
three policy bindings logged source-resolution errors at startup. Bind directly
to the owned Ribbon instance after InitializeComponent instead. A OneWay
MultiBinding combines Simplified capability and layout permission; the existing
source-enabled mirroring carries changes to Simplified/QAT copies while popup
content is borrowed. This is host selector wiring, with no runtime API change.

