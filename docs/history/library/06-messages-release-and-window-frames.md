# Library history: Messages, release and window frames

> Archived implementation evidence. Dates, test counts and pending work describe
> their original checkpoints; later records may supersede them.

[History directory](../01-library.md) · [Current status](../../../04-DESIGN-NOTES.md#5-current-state--next-steps) · [Working agreement](../../../AGENTS.md).

### 3.65 Repeatable Ribbon message bar — 2026-08-08

`RibbonMessageBar` is a lookless `ItemsControl`; ordinary `Items`/`ItemsSource` collection semantics
determine how many rows are present. For the connected Office presentation it is assigned through
`Ribbon.MessageBar`, which hosts it as the ribbon template's third row. A separate top-docked sibling
cannot alter the inner 2007/2024 card or below-ribbon QAT geometry and therefore leaves rounded feet,
shadows, and bottom gaps visible. The integrated path consumes `HasOpenMessages` to square the
preceding surface and remove only its lower gap: the ribbon/QAT keeps its depth shadow at the join,
while the complete message stack casts a second shadow from its final edge. When the last row closes,
the normal ribbon/QAT foot geometry returns.
The vertical items panel stacks every open `RibbonMessage`; one message closing does not disturb the
state of its siblings.

Each `RibbonMessage` exposes `Title`, `Message`, optional `Icon`, action content/command/parameter,
two-way-by-default `IsOpen`, and `IsDismissible`. The action and dismissal paths publish bubbling
`ActionClick` and `Dismissed` routed events, while `Dismiss()` is idempotent for code-driven closure.
The shared template provides the shield, wrapped text, action button, localized Close tooltip, and
per-item visibility triggers. Dedicated UI Automation peers expose the bar as a pane and each
polite live-region message as text named from its title/body. Four matching brush tokens exist in every light/dark generation;
historical dark themes retain the readable light warning surface while modern dark themes use a
muted dark ochre surface. Office 2007 uses a period-correct hard glass crease and Office 2010 a
smoother four-stop glass face; the flat generations retain restrained solid fills. The connected
bar mirrors the generation's horizontal card inset (7px in 2024, 2px in 2007), and the final open
row receives the theme's lower corner token (8px in 2024, 3px in 2007). Rows use a compact 34px
minimum with smaller action/close chrome, while wrapped text can still grow naturally. Inset
generations also carry explicit side outlines on every row, closing the combined card rather than
leaving yellow fill against an unbordered edge. The Close target reserves a transparent rounded
1px outline at rest and resolves a dedicated theme border on hover/press; this contains the heavy
2007/2010 state wash instead of rendering the large borderless rectangle seen in the first pass.
The target is 22×22 with an 8px trailing inset, matching the restrained Office 2024 close affordance
without letting its hover outline crowd the message card edge. Its diagonal glyph receives a
half-DIP optical offset down and right so anti-aliasing does not make the mathematically centered
path appear high-left beside the action button.

Message appearance/dismissal uses the dedicated `RibbonAnimationAction.MessageBar`: a restrained
160ms opacity/6-DIP vertical transform at the subtle level, with the normal expressive multiplier
and instant reduced-motion path inherited from `RibbonAnimation`. `IsOpen` remains the immediate
logical/bindable state, while read-only `IsPresented` keeps an exiting row, the stack shadow, and
the ribbon connection alive until `RibbonMotion.PlayClose` completes. No height, margin, or other
layout property is animated, and the translated template root is clipped to its row so stacked
messages never paint across each other. Rapid close/reopen changes invalidate the older completion callback,
so a newly reopened row cannot be collapsed by a stale exit. The Showcase and RTL lab now begin
with both sample rows closed; their ribbon-level Add Message commands reveal or reopen the next row
one at a time, making repeatability and both transition directions directly testable.

The first live entrance exposed the same intermittent destination-frame flash documented in
§3.41. The row became presentable and then queued `PlayOpen` at `DispatcherPriority.Loaded`, which
runs after Render; depending on frame timing, the fully opaque/resting row could be presented before
the queued animation rewound it. `BeginShow` now starts motion synchronously in the same dispatcher
turn as `IsPresented`, and shared `RibbonMotion.PlayOpen` seeds both base opacity and translation
before installing their clocks. Both animations self-release to ordinary resting values on
completion, so seeding cannot leave a row invisible or displaced if a later caller clears a clock.
The regression contracts pin the seeded start and prohibit another dispatcher hop in the message
entrance path.

A second first-use-only gap remained: rows authored closed can stay `Collapsed` without ever
instantiating their control template, so the initial `BeginShow` had no `PART_Root` for `PlayOpen`
even though later reopens did. The row now records a pending entrance, requests `ApplyTemplate`, and
lets `OnApplyTemplate` consume that request synchronously once the root exists. If realization is
deferred until layout, the row still has no visual to flash; the pending animation is installed
inside `OnApplyTemplate` before the new root can render. Close/unload clears the request, and the
already-realized reopen path remains immediate.

Template realization also raises `Loaded` for that first reveal. Without coordination,
`OnApplyTemplate` consumed the pending entrance and the following `OnLoaded` called `BeginShow`
again, producing a short first pass followed by a full restart. A per-open handled flag now makes
pending-template consumption and `Loaded` mutually exclusive animation owners. A logical close
resets the flag for the next open; No Motion marks the entrance handled while snapping to rest.

The first template attachment can itself produce an `Unloaded`/`Loaded` pair while `IsOpen` remains
true. Resetting the handled flag from `OnUnloaded` therefore still authorized a second entrance:
the first animation was cancelled quickly by `Rest`, then `Loaded` replayed it at full duration.
`BeginHide` is now the sole handled-flag reset point. `OnUnloaded` clears only an unrealized pending
request and rests the departing visual, preserving the one-animation-per-logical-open invariant
across template churn.

The persistent first-show restart ultimately exposed a separate control-flow error in
`OnApplyTemplate`, not another lifecycle race. Its original compound condition was
`if (IsOpen && !TryPlayPendingEntrance()) ... else if (IsPresented) BeginHide()`. When a pending
entrance was successfully consumed, the negated call made the whole condition false, so the attached
`else if` immediately entered `BeginHide` even though `IsOpen` was still true. That cancelled the
first clock, reset the per-open guard, and let `Loaded` start the normal entrance again. The open and
closed cases are now separate outer branches; successful pending consumption can no longer fall
through to the close path. A focused STA regression realizes a pending-open template and verifies
that it remains presented, reproducing the old failure without relying on frame timing.

Application-menu integration exposed one cross-branch depth leak. The original menu-open trigger
promoted the complete `TabControlHost` above the later QAT/message rows because every menu lived
inside the nested tab-strip template. That promotion also raised `ContentHost` and its ribbon-card
shadow; Office 2024's 12px shadow consequently painted a dark band over the QAT and open message.
Suppressing `ContentHost.Effect` removed the leak but also removed wanted depth, so the final fix
moves every shipping application menu into a direct Ribbon-template overlay above the QAT/message
siblings; the `TabControlHost` and its unchanged shadow stay at their normal depth. For the
historical orb, a measured placeholder preserves its nested layout slot while the real button is
temporarily reparented above the outer menu. Ribbon assigns the single menu object to exactly one
presenter, with the nested presenter retained only as a custom-template fallback. The combined 2024
rectangular and 2007 orb menu/message visuals are focused approvals rather than relying on isolated
scenes to imply correct
cross-branch ordering.

The Showcase and Localization/RTL lab each place two independently actionable rows through the
integrated property. The RTL lab uses mixed Arabic/English title, body, and action text and verifies
that logical start content mirrors right while action/Close move left. Seven focused logic contracts cover
the public control shape/defaults, idempotent aggregate/last-open state, connected template geometry,
pending-template entrance ownership, application-menu shadow isolation, vertical item parts/states,
ten-palette token parity, and initially empty/add-from-ribbon Showcase paths. Focused approvals cover
connected 2007/2010/2024 stacks, the mirrored 2024 stack, and the 2024 rectangular plus 2007 orb
open-menu/message compositions. Current automated baseline: 182 logic tests and 56 approved images.

### 3.66 Dark/Black Backstage rails use generation-matched neutrals â€” 2026-08-08

The Classic (2013-style) Backstage rail previously bound its entire navigation column directly to
`Accent`, so every dark generation retained a saturated blue slab even when the surrounding palette
was neutral. The shared template now consumes `Backstage.Classic.NavBackground`. All light themes
publish their established accent color and `ThemeManager.SetAccent` continues to derive it there;
all dark/Black overlays publish a matching neutral surface instead. The dark Classic hover and
selected-row brushes are neutral as well. Custom accents deliberately stop at the rail boundary in
dark mode, while page headings, the File button, and other intended accent consumers remain colored.

Office 2010/2007 Black had a separate gap: their overlays inherited the base generation's pale-blue
`Backstage.NavBackground` and blue selected glass for the Classic2010 design. Both overlays now
replace the rail, selected glass, and selected border with grayscale recipes. Office 2010 preserves
its smooth two-stop rail and radial selection glow; Office 2007 preserves its four-stop hard crease.
The blue glass back button remains the intentional accent affordance rather than part of the neutral
navigation sheet.

Nine focused logic cases pin shared-template routing, ten-palette token parity, strict grayscale
colors, and the distinct 2010/2007 gradient profiles. Three reviewed approvals cover Office 2013
Dark Gray Classic plus Office 2010/2007 Black Classic2010. Current automated baseline: 194 logic
tests and 59 approved images; the full Showcase-height live check remains useful for final tuning.

### 3.67 Compact RibbonCheckBox and RibbonRadioButton — 2026-08-08

The first pre-freeze input-control slice adds `RibbonCheckBox` and `RibbonRadioButton` as lookless
subclasses of WPF's real `CheckBox` and `RadioButton`. They deliberately have one compact form, like
`RibbonComboBox`, rather than artificial Large/Medium/Small layouts. Both expose RibbonKit's familiar
string `Header` plus rich ScreenTip properties; when Header is absent, the shared template falls back
to ordinary WPF `Content`, preserving standard content syntax. Native command, routed-event,
keyboard, three-state check-box, `GroupName`, and radio exclusivity behavior remain inherited.

The shared button dictionary supplies theme-aware square/check and circular/radio indicators,
keyboard-focus outlines, disabled state, and reduced-motion-aware hover/press washes. Existing accent,
text, surface, and interaction tokens carry the structure; the only new token is
`RibbonKit.Brushes.Input.Glyph`, defined by all ten light/dark generation dictionaries so checked
marks remain legible on the accent fill. No layout property is animated. The check template also
covers the indeterminate state.

Dedicated UI Automation peers retain the native CheckBox Toggle and RadioButton SelectionItem
patterns while naming controls from Header. KeyTip invocation now consumes SelectionItem after
Invoke/Toggle, so a radio KeyTip selects the target and applies normal `GroupName` exclusivity rather
than merely raising a routed click. The Visual Studio toolbox, surface context verbs, responsive
Ribbon Editor Add menu, friendly tree names, reorder/delete metadata, and option-specific property
rows all include both controls. The Showcase View tab has a compact Inputs group for direct theme,
hover, keyboard, ScreenTip, KeyTip, and mutual-exclusion checks. They are intentionally not promoted
to QAT/customization icon-command candidates in this slice.

Six focused logic tests cover lookless inheritance/style keys, ScreenTips, UIA patterns/names,
KeyTip behavior, ten-palette token parity, templates, and designer/toolbox wiring. Two deterministic
Office 2024 light/dark approvals cover checked, unchecked, indeterminate, selected, unselected, and
disabled states. Current automated baseline at this point was 200 logic tests and 61 approved images.

The first live hover/focus check exposed one template-layering mistake: `Chrome` owned the content
padding, so WPF inset every child—including `HoverWash` and `PressWash`—while the focus visual still
used the complete control bounds. Classic themes made the smaller amber box especially conspicuous.
`Chrome` is now unpadded and the mathematically equivalent `5,1,5,1` inset lives only on the named
content grid. Resting indicator/text geometry and desired size are unchanged; interaction washes now
fill the complete focus rectangle. The template contract pins that separation for both controls.

### 3.68 Compact RibbonTextBox — 2026-08-08

The next pre-freeze input slice adds `RibbonTextBox` as a lookless subclass of WPF's real `TextBox`,
again with one compact form rather than artificial ribbon sizes. `Header` supplies an optional label,
`InputWidth` sizes only the editor chrome, and the standard `Text`, selection, caret, validation,
binding, command, keyboard, scrolling, and IME contracts remain native. `IsReadOnly` therefore still
allows selection and copying. Rich ScreenTips and KeyTips follow the same surface conventions as the
other compact inputs; a text-box KeyTip transfers focus without changing its text or selection.

The shared dropdown/input dictionary reuses the existing control-surface, border, primary-text,
secondary-text, accent, and corner-radius resources, so no new palette token was required. Its
24-pixel input chrome matches `RibbonComboBox`; hover changes only the border and keyboard focus uses
the accent border, with no animation or layout-property transition. The required `PART_ContentHost`
remains the native `ScrollViewer`, preserving WPF editing behavior across all ten themes.

A dedicated automation peer retains Edit identity and the Value pattern while deriving its accessible
name from `Header`. The Visual Studio toolbox, group context verb, responsive Ribbon Editor Add menu,
friendly tree name, reorder/delete metadata, and property rows cover `InputWidth`, `Text`, `IsReadOnly`,
and `MaxLength`. The Showcase adds editable and read-only fields beside the option controls.

Five focused logic tests pin lookless inheritance/style key, native state and ScreenTips, UIA name and
Value pattern, KeyTip focus, required template part/resources, and designer/toolbox wiring. The two
existing Office 2024 light/dark input approvals now also cover editable, read-only, and disabled text
fields, keeping the visual corpus at 61 images. The initial automated baseline was 205 logic tests and
61 approved images. A ribbon slider was considered after this slice and is intentionally not planned.

The live follow-up passed at 100/125/150/175/200% DPI in both the Office 2024 and classic visual
profiles. RTL follow-up adds an `RTL Inputs` group to the Localization/RTL lab with inherited mixed
Arabic/Latin content plus a left-aligned Latin document identifier inside a still-mirrored control.
The checklist explicitly distinguishes component mirroring from inner text direction. A source
contract prevents either field from forcing the entire control LTR, and a focused Office 2024 RTL
approval pins group order, leading option indicators, and label/field reversal. The interactive RTL
caret, selection, mixed-direction text, explicitly LTR document identifier, and KeyTip focus checks
then passed in the live lab. Current automated baseline: 206 logic tests and 62 approved images.

### 3.69 KeyTip resolution is typeable and prefix-free — 2026-08-08

The per-level resolver now guarantees that every non-empty badge is independently typeable. The old
`AutoAssign` reserved only exact strings, so duplicate explicit keys never activated and an explicit
pair such as `F` / `FN` left the shorter key permanently waiting behind the longer prefix. Explicit
assignments are now reserved before automatic derivation; the first conflicting explicit assignment
in visual order wins, while later exact or prefix collisions fall back to their label and then the
standard fallback alphabet. Automatic keys avoid every reserved prefix as well as exact duplicates.

Resolution also now matches the actual input state machine. `KeyToChar` accepts A–Z and 0–9, but the
old derivation accepted any Unicode letter or digit, producing unreachable badges for fully Arabic
and other non-Latin labels. Authored keys are trimmed, normalized, and accepted only when every
character is typeable; label derivation skips non-typeable characters and uses the ASCII fallback
when necessary. The public `KeyTip.Keys` documentation records the typeable character contract and
the deterministic first-explicit-wins collision policy.

Ten focused cases cover explicit reservation/case normalization, same-initial label derivation,
exact and both directions of prefix collision, prefix-free automatic assignment, unlabeled fallback,
non-Latin labels, and untypeable explicit values. Full validation is green at 216 logic tests plus
the one visual test covering 62 approved images.

### 3.70 Explicit KeyTips inside arbitrary File-surface content — 2026-08-08

Backstage pages and application-menu panes are intentionally arbitrary content, but their KeyTip
levels previously recognized only surface navigation plus RibbonKit's two built-in application-menu
content controls. Setting `rk:KeyTip.Keys` on an ordinary WPF `Button`, toggle, text input, or custom
automation-aware `UIElement` inside a user-authored page therefore serialized correctly but produced
no badge.

Both File surfaces now share an explicit-content discovery rule. The walker follows only realized,
visible visual-tree branches, so a selected Backstage page and the active/default application-menu
pane contribute targets while hidden pages do not. Application-menu pane/footer items retain their
existing automatic indexing. Any other `UIElement` opts in only through a non-blank `KeyTip.Keys`,
which prevents arbitrary page layouts from being flooded with derived badges. Navigation rows are
excluded from the content walk because their own primary/arrow targets are registered separately;
the combined level then passes through the same prefix-free resolver from §3.69.

Selecting a Backstage page by KeyTip now keeps the terminal level active and rebuilds it at
`DispatcherPriority.Loaded`, after the selected-content presenter realizes the new page. This mirrors
the application menu's existing pane-refresh rule. The work also fixes Backstage action items:
`IsButton=True` now invokes the item's Click/Command path under KeyTips instead of trying to select
the action as a page.

Disabled targets remain visible in a stable KeyTip level but are never invoked. The activation state
machine checks effective `UIElement.IsEnabled` before changing levels or dismissing KeyTip mode, then
clears the typed prefix so another badge can be chosen. The shared `InvokeControl` helper repeats the
guard because QAT/customization proxies also call it directly; effective WPF enablement covers both a
locally disabled control and disablement inherited/coerced from a parent or command source.

The live **Disable Samples** follow-up exposed a separate invocation mismatch: UI Automation's
`Toggle`/`SelectionItem` patterns update checked/selected state but do not run
`ButtonBase.OnClick`. The toggle therefore looked checked under its KeyTip while its XAML `Click`
handler never disabled the sample controls. `RibbonToggleButton`, `RibbonCheckBox`, and
`RibbonRadioButton` now expose an internal KeyTip activation hook that calls their real `OnClick`
path. KeyTips therefore update state, raise routed `Click`, and execute `Command` in the same order as
mouse or Space activation; UIA peers remain unchanged for external automation clients.

The compact-input live pass then exposed a discovery mismatch: `RibbonCheckBox`,
`RibbonRadioButton`, and `RibbonTextBox` already had invocation support and authored Showcase keys,
but the root-level collector's ribbon-control type list predated those controls. They now participate
in normal ribbon KeyTip discovery, with auto-derived labels taken from `Header`; text-box activation
continues to transfer focus without changing the value.

A collapsed-group follow-up exposed one more lifecycle distinction: invoking any leaf previously
tore down every non-persistent KeyTip level, whose `OnExit` closes the collapsed-group flyout. That is
correct after commands but made a text-box KeyTip focus an editor that immediately disappeared. Leaf
teardown now preserves only the activated level for editors and nested-picker openers (`TextBox`,
`ComboBox`, and `InRibbonGallery`) while still removing its badges and ending KeyTip mode. Buttons,
checkboxes, and radio buttons retain normal command dismissal; Escape or light-dismiss still closes
the preserved flyout afterward.

The Showcase demonstrates both extension points with ordinary WPF buttons: **Create custom draft**
(`CD`) in the Backstage Home page and **Manage recent locations** (`MR`) in the application menu's
default pane. Six focused cases cover visible explicit Backstage targets, built-in plus explicit
application-menu targets, ordinary-button invocation, Backstage action invocation, and inherited
disabled-state blocking, plus native toggle Click/state behavior. Automated baseline: 222 logic tests
plus the one visual test covering 62 approved images; the two live examples remain the focused
interactive verification surface.

Two additional compact-input cases pin checkbox, radio-button, and text-box participation,
Header-based label derivation, and the containing-flyout preservation boundary. Current automated
baseline: 224 logic tests plus the one visual test covering 62 approved images.

### 3.71 Showcase appearance preferences stay separate from ribbon customization — 2026-08-10

The Showcase now persists its user-selected appearance in
`%LocalAppData%\RibbonKitShowcase\appearance-preferences.json`. This is deliberately a second file,
not an expansion of `RibbonCustomizationSerializer`: importing or resetting tab/group/QAT structure
must not unexpectedly change the application palette, swap its File surface, or activate an
operating-system window material.

`ShowcaseAppearancePreferences` is an app-owned, schema-versioned record covering the Office
generation, nullable custom accent (`null` means the theme default), dark variant, colored title bar,
Backstage design/translucency, Backstage versus application-menu surface, and one mutually-exclusive
None/Mica/Acrylic backdrop preference. Enums serialize as readable strings and accents normalize to
`#AARRGGBB`; corrupt JSON, unknown enum values, invalid colors, and future schema versions fall back
to the factory appearance without partially applying state.

Restore order is part of the contract: apply the theme first, then dark/accent/title-bar state, then
the Backstage design and explicit File-surface choice, and finally request the DWM backdrop. Theme
selection still chooses its conventional live surface (2007 menu for Office 2007, Backstage for the
other generations), but the saved explicit surface wins during startup. The stored backdrop is the
user's requested preference rather than only the last successful DWM state: on an unsupported
Windows build the toggle truthfully reverts off while the preference remains available for a later
supported launch. `ThemeManager.IsTitleBarBackdrop` is derived runtime state and is not serialized.

The runtime library gains no public API from this sample feature. Thirteen focused cases cover
complete JSON round-trip, factory defaults, schema/corruption rejection, enum rejection, and stable
accent normalization. Current automated baseline: 237 logic tests plus the one visual test covering
62 approved images.

### 3.72 Customization serializer round-trip and foreign-JSON hardening — 2026-08-10

A dedicated `RibbonCustomizationSerializerTests` suite now exercises the real public
`Serialize`/`Apply` boundary against separately constructed ribbons, rather than only checking the
merge/modal exclusions around it. The complete round trip covers built-in tab/group reorder and
renaming, authored visibility, custom tabs/groups, custom layout, renamed and resized button/toggle/
drop-down proxies, explicit and auto-derived command identities, borrowed icons, declared plus proxy
QAT ordering, QAT placement, newly shipped/contextual/id-less content preservation, missing-command
skips, baseline Reset, repeated Apply idempotence, and older JSON without `QuickAccessPosition`.

Two real defects fell out of that coverage:

- A syntactically valid but unrelated JSON object deserialized into an empty DTO because
  `System.Text.Json` ignores unknown properties. Applying `{}` or `{"theme":"dark"}` therefore
  stripped custom tabs/groups and cleared the QAT. `Apply` now verifies the root signature contains
  array-valued `Tabs` and `QuickAccess`, validates nested collections and enum values, and returns
  before unmerge/reconciliation for foreign, null-shaped, or otherwise invalid documents.
- `BuildIdentity` registered a custom group's borrowed icon under the custom group's own id before
  `FindIconId` ran. Depending on traversal order, serialization stored that self-reference; a fresh
  ribbon could not resolve it because the custom group did not exist until reconstruction. Custom
  groups no longer seed the icon identity map, so the stable built-in command/group id wins.

Twenty focused cases cover these contracts. Current automated baseline: 257 logic tests plus the
one visual test covering 62 approved images.

### 3.73 Reduction thresholds, priority order, and malformed-width coverage — 2026-08-10

The remaining adaptive-layout test gap is closed with 37 focused cases: 29 against the pure
`ReductionAlgorithm` and eight STA measurements through the real `RibbonGroupsPanel`. The panel
cases pin explicit priority (highest first), rightmost tie-breaking, unprioritized largest-first
ordering, fixed-group exclusion, the complete ResizeThenCollapse state map, and cache invalidation/
reprobe after a runtime width change.

The pure cases add exact-fit, fractional-DIP, equal/nearly-equal/non-monotonic state, empty/duplicate
custom order, null/empty table, invalid index, and invalid measurement coverage. Two hardening changes
were warranted:

- Layout comparisons now use a scale-aware epsilon equivalent to WPF's internal double-comparison
  pattern. A conceptual exact fit such as `0.1 + 0.2 == 0.3` no longer crosses a reduction threshold
  because of binary floating-point noise, and a near-identical probed state is skipped until a
  genuinely narrower state is found. Meaningful DIP differences and deliberately non-monotonic
  sequences retain the existing behavior.
- Public inputs are validated before the positive-infinity/empty fast paths: available width must be
  non-negative (positive infinity remains supported), every state width must be finite/non-negative,
  each group must expose a state, the combined large width must remain finite, and every reduction
  index must exist. The public XML documentation now records these exceptions.

No `RibbonGroupsPanel` ordering or cache behavior needed correction. Twenty-five new cases raise the
current automated baseline to 282 logic tests plus the one visual test covering 62 approved images.

### 3.74 Visual Studio debugger distorted live-resize performance — 2026-08-10

The initial screen-recording comparison appeared to show that RibbonKit resized materially more
slowly than Word. A resize-only shadow-suppression path and, later, lightweight body/QAT/message proxy
shadows were explored. Follow-up testing isolated the decisive variable: the slowdown occurred only
while the Showcase was attached to Visual Studio's debugger. Running with Ctrl+F5 or launching the
built executable removed the slowdown with opaque, Mica, and Acrylic backgrounds.

The workaround was therefore removed completely. RibbonKit has no `IsLiveResizing` public state or
native sizing hook, no resize-specific shadow switching, proxy geometry, or theme tokens, and no
resize-only marker/notch coalescing or application-menu placement guard. The normal themed
`DropShadowEffect` remains present throughout interactive resizing, preserving the intended visual
language without adding debugger-driven API and template complexity before the API freeze.

**Verification rule:** assess perceived WPF resize performance outside the debugger. Visual Studio's
managed debugger, XAML Hot Reload, and diagnostic tooling can materially alter UI-thread, layout, and
render cadence. Debug runs remain useful for correctness; Ctrl+F5 or the built executable is the
appropriate baseline for interaction performance. The current automated baseline is 283 logic tests
plus the one visual test covering 62 approved images.

### 3.75 Phase 8 API review and freeze — 2026-08-10

The v1 runtime surface is now frozen. `RibbonKit.csproj` references
`Microsoft.CodeAnalysis.PublicApiAnalyzers` 5.6.0 as a private analyzer and marks its compatibility,
nullability and baseline-integrity diagnostics as errors for both `net8.0-windows` and
`net9.0-windows`. `PublicAPI.Shipped.txt` captures the reviewed 1,094-line nullability-aware surface;
`PublicAPI.Unshipped.txt` starts empty except for `#nullable enable`. Compiler errors also enforce
missing public XML docs and broken `cref` references. Future compatible additions belong in the
unshipped file and must be reviewed deliberately; the shipped file is not edited to disguise a
breaking change.

The review retained the intentional WPF extension surfaces: lookless controls and their automation
peers, dependency/routed-event identifiers, layout panels used by templates, `IRibbonSizeAware`,
theme/localization/backdrop APIs, and the public animation primitives that let application-authored
controls honor RibbonKit's motion policy. The analyzer confirms that the surface has no oblivious
reference types and is identical across the two runtime TFMs.

One behavior needed correction before freezing. `Ribbon.AddToQuickAccess(FrameworkElement)` formerly
accepted an unknown control and fell into `CreateCommandProxy`'s generic UIA button, producing a
blank or misleading QAT entry even though the catalog never offered that type. It now returns
`false` unless the source is a `RibbonButton`, `RibbonToggleButton`, `RibbonSplitButton`, or
`RibbonDropDownButton` (and still returns false for a duplicate). The generic internal fallback
remains available to overflow a hand-declared QAT element without widening the supported automatic
projection claim. A test pins rejection of groups, combo boxes, galleries and arbitrary WPF buttons;
richer group/gallery/combo representations remain the post-v1 work recorded below.

The same zero-warning pass fixed two broken runtime XML-doc references and four real nullable-flow
warnings in the net472 design tools without changing their behavior. Verification: Release solution
build zero warnings/errors; 283 logic tests green; the visual test green across all 62 approvals.

### 3.76 Repository-native v1 documentation gate — 2026-08-10

The v1 documentation deliberately stays in the GitHub repository rather than introducing a separate
site and deployment stack for a single WPF control library. `README.md` is the maintained public
entry point: it now links directly to the getting-started, feature, theming, design-tool,
documentation and roadmap sections; records the current source-reference path before the v1 NuGet
package is published; and makes the first XAML sample pasteable by omitting application-specific icon
resources. It also explains that Office 2024 is the default theme and routes readers by task into the
Showcase and focused Markdown references.

The existing four screenshots already cover the useful product story—current ribbon, Backstage,
historical theming and the Visual Studio Ribbon Editor—so no decorative or redundant captures were
added. Screenshot generation remains available when a future feature needs visual explanation. The
obsolete documentation-site feature/roadmap entries were replaced with the completed repository-docs
gate. Link, image and sample validation are part of this gate; API-reference generation remains a
possible post-v1 addition rather than a release dependency.

### 3.77 NuGet and Showcase identity icon — 2026-08-11

The release package and executable now share a purpose-built RibbonKit identity. The deterministic
`assets/RibbonKit.svg` master uses a compact folded-ribbon `R` that remains recognizable at Windows
taskbar sizes; `RibbonKit.png` is the NuGet package icon and the multi-resolution `RibbonKit.ico`
contains 16 through 256 px frames. The Showcase embeds the ICO as its executable icon and assigns it
directly to `MainWindow`. Because `RibbonWindow` replaces the native caption, its shared template now
also binds `Window.Icon` into a 16-DIP leading image; setting the property alone populated the taskbar
but left the custom title bar empty. The icon remains visible when Backstage hides title-bar QAT
content, while a null icon collapses without leaving a new gap. When a hosted ribbon explicitly uses
the Office 2007 `Orb` application-button shape, it internally registers that state with its owning
`RibbonWindow`; the caption icon then collapses and returns its width to the QAT because the orb
already owns application identity. This is keyed to the actual shape rather than the theme, keeps
`Window.Icon` intact for the taskbar/executable, handles shape changes and unload/reparent cleanup,
and adds no post-freeze public API. NuGet's packaged README was already wired; SourceLink and final
package-content validation remain next.

### 3.78 Office 2007 application-menu open layout stability — 2026-08-11

Opening the two-pane menu could nudge the complete ribbon downward by a few DIPs. This was specific
to the orb path, not the new caption icon or Windows 11 maximize hook: the menu moves the real orb
above its outer overlay and leaves a placeholder in `ApplicationButtonLayer`, while rectangular File
tabs never take that path. The placeholder was initially sized from the button's `ActualHeight` plus
margin and then repeatedly recalculated after the button had moved into a differently constrained
overlay. That created a layout feedback loop; Office 2007's negative orb overhang made the changed
tab-row contribution visible.

The placeholder now reserves the button's pre-reparent `DesiredSize`, which is exactly the size
reported to its original panel and already includes margin, and keeps that reservation stable until
the button returns. Menu/overlay placement continues to follow the arranged placeholder, but no
longer writes post-reparent measurements back into the ribbon's layout. A focused STA case pins the
difference between desired layout contribution and a stretched post-arrange `ActualSize`.

The first correction stabilized the ribbon but exposed the other half of the distinction: giving the
placeholder an explicit desired height centered its origin inside the taller tab row, so the overlay
followed that origin and the orb itself moved down on open. The placeholder now uses its desired
height as a minimum while retaining stretch alignment, preserving the original slot origin. The
overlay separately keeps the pre-reparent arranged size (`ActualSize` plus margin), so the real orb
neither shrinks nor moves. The deterministic open/closed scene now pins the application button's X/Y
origin in addition to the ribbon, body, QAT and message-bar geometry.

### 3.79 Source Link and symbol package verification — 2026-08-11

Source Link is complete without an explicit `Microsoft.SourceLink.GitHub` dependency. RibbonKit is
built with the .NET 8-or-newer SDK, which supplies GitHub Source Link automatically; adding the
provider package would only override the SDK's bundled implementation. The existing
`PublishRepositoryUrl`, `IncludeSymbols`, and `SymbolPackageFormat=snupkg` settings are therefore the
intentional release configuration.

A clean Release pack was inspected rather than inferred from a successful build. Its NuSpec carries
the canonical repository URL, `git` type, branch, and exact commit. The `.snupkg` contains one
portable `RibbonKit.pdb` for each of `net8.0-windows` and `net9.0-windows`; both PDBs contain exactly
one Source Link record mapping all repository documents to the same commit-pinned
`raw.githubusercontent.com/Wraith1080/RibbonKit` URL. The generated assembly informational version
also includes that commit. Final package-content/consumer validation remains the next release item.

### 3.80 Portable local package output — 2026-08-11

The runtime project no longer sends every local pack to the original developer's machine-specific
`E:\NuGet` directory. Its default `PackageOutputPath` is now the repository-relative `artifacts/`
directory, matching the location already used explicitly by GitHub Actions. A plain
`dotnet pack src/RibbonKit/RibbonKit.csproj -c Release` therefore has the same discoverable output
location on any checkout while callers can still override it with `--output` when needed. Release
versioning and final package-content/consumer validation remain next.

### 3.81 Deterministic package versioning — 2026-08-11

Package versions no longer depend on the wall clock. The former
`1.0.0-dev-<yyyyMMddHHmm>` expression could give separate build and `--no-build` pack evaluations
different identities, made identical source commits produce differently named packages, and
prematurely presented routine local output as the v1 line. The project now composes its documented
current version from `VersionPrefix=0.1.0` and `VersionSuffix=alpha.1`, so normal builds and packs
consistently produce `0.1.0-alpha.1` and the assembly informational version adds only the Source Link
commit metadata.

Release automation may still set the standard `Version` MSBuild property explicitly (for example,
`-p:Version=1.0.0`) when a validated release is intentionally prepared. No tag-derived or automatic
publishing behavior was added. Final package-content and clean-consumer validation remain next.

### 3.82 NuGet package and clean-consumer release gate — 2026-08-11

`eng/Validate-Package.ps1` is now the repeatable post-pack gate. It requires exactly one matching
`.nupkg`/`.snupkg` pair, allowlists the package layout, rejects duplicate entries, source leakage,
PDB duplication and unexpected consumer dependencies, and verifies the license, readme, icon,
repository/commit, target-framework groups, toolbox manifest and matching symbol-package version.
Both runtime assemblies, XML documentation files, design-tools copies and portable PDBs must be in
their exact framework-specific locations.

The same gate creates a temporary package-reference-only WPF consumer with `nuget.org` and every
other feed cleared, restores RibbonKit solely from the local `artifacts/` directory into an isolated
package cache, and compiles real `urn:ribbonkit` XAML against both `net8.0-windows` and
`net9.0-windows`. The temporary project is removed after success and preserved on failure for
diagnosis. The local `0.1.0-alpha.1` validation completed with zero warnings or errors.

CI runs this gate after packing and retains both the main and symbol packages in its existing build
artifact; this does not publish either package to NuGet. Final live runtime/performance and Visual
Studio installed-package designer validation remain next.

### 3.83 Final live performance and installed-package pass — 2026-08-11

`eng/Measure-ShowcasePerformance.ps1` now captures the Release Showcase baseline by launching the
built executable directly, never through the Visual Studio debugger. Five process-start-to-input-idle
runs measured 711.88–747.12 ms (727.66 ms median). After one 160-resize cache-warmup sweep, three
identical 160-resize passes averaged 16.081 ms of process CPU per resize and 63.61% of one core. The
Showcase's large all-features visual tree allocated its expected WPF/render caches during warmup;
across the subsequent 480 resizes, working set and private memory changed by 10.12 MiB and 10.41 MiB
respectively rather than repeating the initial allocation on every pass. Results are written only to
ignored `TestResults/performance/showcase-release.json`; the live-GUI baseline is intentionally not a
machine-independent CI threshold.

The package validator's generated consumer is now a real executable as well as a compile probe. Its
`App.xaml` explicitly merges `Tokens.Office2024.xaml`, the complete `Office2024.xaml` shared-control
aggregator, and `Mdi.xaml`, so package URI and visible styling failures cannot hide behind the
assembly's implicit default theme. With `-RunConsumer`, it opens the package-installed RibbonWindow,
renders Backstage, opens and closes that surface, performs 120 width/layout changes, forces managed
collections, records a local JSON result, and exits. The final styled run reached `ContentRendered`
in 996.05 ms and retained 71,152 bytes (about 69 KiB) of managed memory across the exercise; both
target frameworks still compiled with zero warnings and errors from the isolated local package feed.

Visual Studio Community 2026 18.7.1 then opened that same package-only project from a clean IDE
process. Its isolated `WpfSurface` loaded `RibbonKit.dll` 0.1.0-alpha.1 from the designer cache, and
the live automation tree exposed the RibbonWindow, ribbon, tab, group, and buttons. Selecting the
ribbon produced the packaged design-tools commands (`Edit Ribbon…`, `Add Tab`, application-menu and
QAT actions); invoking `Edit Ribbon…` opened the net472 Ribbon Editor, rebuilt the one-tab model and
logged `RibbonEditorWindow: ready`. The user confirmed both the styled surface and editor visually.
At that point the final performance/install gate was complete and community launch was the remaining
Phase 8 item; §3.84 records the subsequent distribution decision.

### 3.84 Local v1.0.0 GitHub-release candidate — 2026-08-11

The public-launch policy changed at the user's direction: RibbonKit will not be submitted to
NuGet.org. If public distribution is requested later, the validated `.nupkg` and `.snupkg` will be
attached directly to a GitHub Release. Until then, no release, tag, push, or upload is performed.

Release metadata now defaults to `1.0.0`, attributes the package and assemblies neutrally to
`RibbonKit contributors`, and documents that development was performed primarily with AI coding
assistants under human direction, visual review, and automated verification. The same neutral
copyright notice is used by the MIT license; no individual is presented as the package author.

`eng/Prepare-GitHubRelease.ps1` is the repeatable, local-only release gate. It restores, builds,
tests, packs, invokes the isolated package validator, copies `RELEASE_NOTES.md`, and emits SHA-256
sums under ignored `artifacts/github-release-v1.0.0/`; it contains no publishing command. The first
candidate passed with zero build warnings or errors, 298/298 logic tests, the single visual test and
all 62 approvals, and isolated package consumption on both runtime TFMs. Its inspected NuSpec reports
version `1.0.0`, author `RibbonKit contributors`, MIT, the expected repository and release notes.

The prepared files remain local: `RibbonKit.1.0.0.nupkg` (567,192 bytes),
`RibbonKit.1.0.0.snupkg` (109,774 bytes), `RELEASE_NOTES.md`, and `SHA256SUMS.txt`. Because the release
changes are not yet committed, this candidate's Source Link repository commit still identifies the
current pre-release HEAD. If publication is ever authorized, commit first and rerun the preparation
script so the final assets point to the exact release commit. Local release preparation is complete;
public community launch is intentionally deferred.

### 3.85 GitHub v1.0.0 community release — 2026-08-11

The user subsequently authorized and published the GitHub-only community release. Tag `v1.0.0`
points at merge commit `6d6dd32`; the GitHub Release carries the main package, symbol package,
release notes/checksums, and GitHub's generated source archives. RibbonKit remains intentionally
absent from NuGet.org. The release-facing repository documentation now describes v1.0.0 as published
rather than as a local candidate. Deleting unrelated leftover patch/bug-note files after the tag is
ordinary repository cleanup and does not require a new binary release.

### 3.86 Post-v1 custom-control projection and future-theme plans — 2026-08-12

`docs/08-CUSTOM-CONTROL-INTEGRATION-PLAN.md` records an opt-in integration contract for arbitrary
controls hosted in `RibbonGroup`. Ordinary hosting remains requirement-free and `IRibbonSizeAware`
remains the optional adaptive-layout hook. A control that wants customization/QAT participation must
instead provide stable identity, display metadata, a small icon and a context-aware factory that
creates fresh source-bound projections for QAT strip, overflow and custom-group use. The provider
owns command/state binding and custom event cleanup; RibbonKit owns identity, persistence,
enabled/merge state, placement, KeyTip/automation context and deterministic disposal. The proposed
names are deliberately provisional until two distinct control shapes prove the lifecycle.

The same plan makes a small consumer-facing `DynamicResource` token subset the theme contract for
custom content; it rejects a copied palette snapshot that would go stale on a live theme switch.
`docs/09-FUTURE-THEMES-PLAN.md` records the complementary theme-intake and whole-surface verification
gate. Office 2021 is the leading candidate: the deliberately skipped sharp-edged midpoint between
RibbonKit's Office 2019 and Office 2024 themes. The user-approved reference is the pre-rounded UI with
Office 2019's compact square group/control geometry, saturated blue integrated title bar, white
ribbon, centered title-bar search and rectangular search flyout. It is explicitly not the later
Windows 11-style rounded visual refresh that already resembles Office 2024. The theme must remain
visibly distinct from both neighboring generations in a side-by-side matrix.

The ordered theme shortlist continues with **RibbonKit Aurora**, a signature dark-indigo theme using
matte surfaces, translucent state washes, a restrained blue/violet title band and middle-weight
rounding; **Warm Sand**, a non-white parchment/teal light theme; and **Graphite Copper**, a compact
charcoal/copper professional dark theme. Evergreen, Aubergine and Polar Slate remain exploratory.
High contrast stays a system-accessibility mode rather than an aesthetic theme. Exact palette anchors
and acceptance boundaries live in `docs/09-FUTURE-THEMES-PLAN.md` and remain provisional until
Showcase comparison.

### 3.87 RibbonKit Writer functional reference-app plan — 2026-08-12

`docs/10-RIBBONKIT-WRITER-PLAN.md` records a separate post-v1 `RibbonKit.Writer` application rather
than adding more product behaviour to the Showcase. The target is a genuinely usable lightweight
rich-text editor that exercises document lifetime, selection-sensitive command state, Backstage,
QAT, contextual tabs, customization, appearance persistence, accessibility and DPI in one coherent
consumer. The initial format boundary is `.txt`, `.rtf` and a versioned native `.rkw` package; the
native format owns complete page-settings, image and structured-content fidelity.

Paper size, orientation and margins belong to a document-owned `DocumentPageSettings` model applied
to `FlowDocument.PageWidth`, `PageHeight` and `PagePadding`. Editing remains a centred continuous
`RichTextBox` paper surface; a cloned document renders through the paginator for true page preview and
printing. The plan deliberately rejects fake editable page breaks and does not promise Word's fixed
layout engine.

Tables are in scope as native FlowDocument structure. Insert uses a small grid picker; a contextual
Table Tools tab owns row/column insertion and deletion, merge/split, cell sizing, alignment, padding,
borders and background. Table mutation helpers must preserve a valid document tree and predictable
caret position, with `.rkw` as the fidelity format and RTF interoperability treated as best effort.

OLE/COM compound objects are explicitly out of scope. Although FlowDocument can host WPF UI elements,
in-place OLE would add executable-content, COM activation, focus, storage, bitness, print and security
contracts unrelated to proving RibbonKit. Images and hyperlinks ship first; any later attachment is
an inert Writer-owned file card, not an activated embedded application.

### 3.87a RibbonKit Writer Luna execution decomposition — 2026-08-20

`docs/11-RIBBONKIT-WRITER-LUNA-EXECUTION-PLAN.md` decomposes the approved W0-W5 product milestones
into dependency-gated packets for later `gpt-5.6-luna` subagents. It deliberately creates no Writer
project or branch and schedules no agent. The functional scope remains owned by
`docs/10-RIBBONKIT-WRITER-PLAN.md`; §5 remains the live implementation-status authority.

The execution contract keeps solution/project files and primary application/ribbon XAML under
exclusive ownership, treats `src/RibbonKit/**` as read-only unless a separately approved runtime-gap
packet proves a genuine library need, and makes the lead agent responsible for diff review, full
build/tests and verification on the real Writer surface. Agent reports and plan checklists are not
implementation evidence. Native `.rkw` loading, FlowDocument table mutation and final Windows
acceptance have explicit higher-risk gates; W5 distribution remains a user/lead decision rather than
a delegatable coding task.

### 3.88 Office 2007 opaque and Aero-inspired window-frame plan — 2026-08-12

The deferred Office 2007 window frame now has a two-tier contract in
`docs/07-OFFICE-2007-THEME-PLAN.md` §6. The original reading here treated the blue beside the
document as a full-window band; §3.89 supersedes that geometry after row-by-row review of the
restored non-Aero capture. The guaranteed baseline keeps the window root flush, preserves the
existing opaque title gradient and adds only a quieter inactive title state. It must work without
transparency or a system material.

An optional Aero-inspired mode may layer a distinct restored-frame geometry plus translucent tint,
grain, reflection and active/inactive glass treatment. Where supported, the existing app-controlled Acrylic backdrop
may show through only the transparent frame/title regions; body and ribbon surfaces remain opaque,
including the measured `#BFDBFF` tab strip. Theme selection does not activate the material, and the
host's frame/backdrop choice remains separate from structural ribbon customization persistence.

The implementation stays inside the application. It must not use `AllowsTransparency`, desktop
capture/CPU blur, DWM injection, private composition hooks or downloaded symbols. DWMBlurGlass is a
visual reference only. The user plans to provide injected Task Manager and Paint captures because
their native DWM-painted frames expose useful tint, reflection, caption and inactive-state evidence;
the plan requests active/inactive, normal/maximized, detail crops and the injection settings.

Implement and approve the opaque frame first. The Aero enhancement follows as a separate prototype
and must fall back to the opaque result. Verification covers 100/125/150/175/200% DPI,
normal/maximized/restore, active/inactive, all resize edges, mixed-DPI monitor movement, caption
commands and the existing Windows 11 `HTMAXBUTTON` Snap Layout/non-client hover bridge. Actual
Acrylic composition is a live check rather than a deterministic snapshot baseline.

### 3.89 Office 2007 opaque window baseline — reference correction — 2026-08-12

The first implementation pass on `codex/office-2007-window-frame` misread the light-blue strip beside
the non-Aero Word document as a 5–7 DIP frame around the complete window. The supplied restored
reference (the Maximize button is visible) disproves that interpretation. Pixel rows through the
title, ribbon, document and status areas show wallpaper at x=0 and a consistent one-pixel dark edge at
x=1, but the title gradient and status background both begin at x=2. The wider `#B1C6E1` /
`#C2D9F7` / `#B0CBEF` / `#9BBBE3` stack appears only beside the ribbon/document workspace. It is
Office client-area bevel/padding, not global window chrome.

The uncommitted prototype was corrected immediately: `PART_WindowRoot` is again flush with the
physical-LTR `PhysicalWindowFrameHost`; the custom outline, full-window band and maximized top-band
overlay were removed. RibbonKit relies on the supported native `WindowChrome`/DWM edge rather than
painting a duplicate border. The opaque Office 2007 fallback remains the existing historical title
gradient plus opaque ribbon/document surfaces. Its 34-DIP caption metric is tokenized for the later
Aero experiment, and inactive Office 2007 windows provisionally lower only `TitleBarBand` opacity to
`0.78`. Other generations publish the same caption/state keys with neutral values, preserving the
shared template and token parity.

This correction also preserves the existing maximize implementation: no decorative inset or overlay
is added around `PART_WindowRoot`, so `RibbonWindow.UpdateMaximizeInset`, the `WM_GETMINMAXINFO`
fallback and the previously measured work-area compensation remain the sole maximize geometry.
Resize hit testing and the `HTMAXBUTTON`/non-client caption-state bridge are unchanged.

The Aero Word reference establishes a different composition: its material runs continuously from the
outer restored edge across the title region down to the opaque tab strip. The Task Manager/Paint
DWMBlurGlass captures are appearance references only; their supplied setup is Aero, radius 20,
ColorBalance 8%, BlurBalance 49%, AfterglowBalance 43% and reflection opacity 40%, with accent
override and custom reflection texture disabled. Section §3.90 records the separately implemented
Aero-inspired prototype.

After the correction, `dotnet build RibbonKit.sln --no-restore` is clean and `dotnet test
RibbonKit.sln --no-build --no-restore` passes **306 logic tests plus one visual test covering 62
approved images**. All five base token dictionaries have **188 identical keys**. A per-monitor-aware
real-HWND capture at the available 125% setting confirms that normal title/status surfaces now span
the complete client width and maximize adds no painted frame band. The remaining cross-DPI and
hands-on interaction gates are tracked with the Aero prototype in §3.90.

### 3.90 Office 2007 Aero-inspired window-frame prototype — 2026-08-12

The optional stage is now implemented as a separate, host-controlled appearance. The new public
`RibbonWindow.FrameAppearance` dependency property accepts `Default` or `Office2007Aero`; changing
it never requests a DWM material. `MicaHelper.TrySetBackdrop` independently reports its last accepted
material through the read-only `RibbonWindow.ActiveBackdrop` property so the shared template can
distinguish a real Acrylic composition from its deterministic fallback. No `RibbonBackdrop.Aero`
value was added.

`Controls.Window.xaml` still keeps `PART_WindowRoot` flush in the default/opaque mode. The explicit
Aero appearance alone creates a physical-LTR 6-DIP frame on the restored left, right and bottom
edges, with a blue tint, bright inner edge, restrained diagonal reflection and tiled grain. The same
overlays continue through the title band. Accepted Acrylic makes only that frame and title fill
transparent; the tab strip, ribbon, document surface and status area remain opaque. Without Acrylic,
the title now uses the same `#9BBBE3` Aero fallback brush as the restored frame rather than exposing
the ordinary Office 2007 title gradient; the inactive title/frame pair similarly shares the quieter
`#BCC8D6` fallback. Maximizing collapses the side/bottom frame and leaves the material title
treatment, matching the supplied Word, Task Manager and Paint evidence.

The five base dictionaries now carry **209 identical token keys**. Non-2007 themes publish neutral
transparent/zero frame values, so the project retains one lookless template. Office 2007 Black
overrides only its required frame/tint/foreground colours. Caption hover and pressed brushes also
flow through the existing non-client bridge, so `WM_NCHITTEST` can still return `HTMAXBUTTON` while
the custom button renders the Aero state.

The Showcase exposes `2007 Aero` separately from `Acrylic`. Its versioned appearance settings moved
to schema 3 and migrate schemas 1–2 with the reference tint defaults; this file remains independent
of ribbon customization Import/Export/Reset. Selecting Office 2007 enables the frame control but
does not check it and does not enable Acrylic. A saved Aero preference is dormant under other themes
and returns when Office 2007 is selected.

At the available real 125% per-monitor-v2 setting, live normal active, normal inactive and maximized
Acrylic captures pass: material is continuous only through the title/restored frame, the inactive
state is quieter, and the maximized side/bottom band disappears. The opaque Aero fallback also
passes at 125%. Direct `WM_NCHITTEST` probes return the expected eight resize codes (`HTLEFT` through
`HTBOTTOMRIGHT`) and `HTMAXBUTTON` over the maximize button. The full automated gate passes **313
logic tests plus one visual test covering 62 approved images**. Actual Acrylic remains deliberately
outside deterministic snapshots. The final multi-DPI, mixed-monitor and hands-on approval is recorded
in §3.93.

The first user review found the frame faithful but the composition too dense and identified bright
vertical seams beside the orb and Close button. The supported DWM Acrylic backdrop has no public
blur-density control, but RibbonKit's authored tint does. `RibbonWindow.AeroFrameTint` is a
host-overridable brush and `AeroFrameTintIntensity` is a validated 0–1 dependency property; the
Office 2007 token default is 0.16. The Showcase hosts an ordinary WPF Slider presenting that value as
0–100% and an explicit `Use accent color` checkbox. The latter resolves the current ribbon accent
into a local frame brush without making theme/accent selection automatically enable or recolor Aero.
No public `RibbonSlider` control was introduced.

The seams were not DPI rounding; they were the deliberate inner-bevel stroke incorrectly continuing
through the title. Its left/right strokes now begin below the 34-DIP caption so the title and frame
remain one uninterrupted material surface. Tint opacity is applied only to the tint layers, so the
slider does not weaken reflection, grain or the corrected inner highlight.
