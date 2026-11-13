# Native WinForms theme architecture

**B1–B3, C1–C2, D1a–D1c, D2–D3, D4a–D4c, D5a–D5b, D6 and D7a–D7f implemented; latest delivery 2026-11-13.** Backstage uses one registration
for sibling layout/drawing helpers, explicit defaults, measured layout inputs and
caption/motion profiles. Headers register sibling Filled/Underline drawing with
explicit geometry defaults and adjustable effects. C2 resolves body/connected
frame data through shared helpers without another family selector. D1a extracts
ordinary command-state/split chrome. D1b extracts shared command indicators and
raster treatment with independently adjustable settings. D1c extracts native
field/choice paint with immutable appearance overrides, keeping the native editor
manager unchanged. D2 extracts shared popup frames/shadows with adjustable
borders, horizontal shadow offset/falloff and actual-renderer content clearance.
D3 extracts ordinary menu heading/separator/scroll-band decoration with adjustable
settings and preserved actual renderer hooks. D4a extracts Application Menu row/
footer/split state chrome with independently adjustable shape, outline, divider
and selected-primary fill/tint. D4b extracts backing/pane/frame/divider and heading
paint with shared clipping and independently adjustable settings. D4c extracts
content drawing and shared content measurement with immutable overrides, keeping
actual image/text/indicator/arrow/focus/separator hooks and native owners. D5a
extracts built-in gallery tile-state/headings with immutable appearance overrides
and actual protected chrome precedence. D5b shares gallery content/glyph drawing,
popup minimum-height inputs and inline frame/utility chrome, with independently
adjustable settings and existing targets/presenter ownership. D6 shares tip drawing,
title weight and badge metrics with adjustable outlines and A17 Backstage placement.
D7a shares Options navigation/divider drawing and resolved decoration metrics.
D7b shares native button painting and heading font/padding inputs with adjustable
settings and scoped ordinary-button opt-in. Native controls and draft lifecycle
retain their owners; headings keep existing construction/load/font/DPI layout.
D7c shares field/list wrapper drawing and native search/closed-choice text metrics.
Adjustable frame strokes reserve real child clearance; existing editor roles,
actual renderer hooks, handles/parent, draft and choice staging retain their owners.
D7d shares command-list row paint and icon/text metrics with adjustable content,
selection and Simplified heading weight. Native row geometry, direct text/focus,
borrowed artwork and draft ownership remain; tree/icon-picker decoration stays separate.
D7e shares tree state/content/glyph paint and resolved decoration slots. Tree
appearance members and existing shared Chevrons expose fixed effects while native
targets/text/indent/scrolling, actual checkbox hooks and node/draft ownership remain.
D7f shares group icon-picker selection/content drawing and resolved metrics with
adjustable shape/outline/fill/artwork/text inset. Its square-geometry follow-up
resolves one shared cell for None/artwork, including native icon hit rectangles;
size and inset changes affect both. Native items/selection, borrowed artwork,
actual radius fallback and group validation/OK/Cancel retain their owners.
The earlier-slice review found adjustability gaps. A00–A04 fill routing/motion
and A06–A09 decoration/spacing plus A11–A17 header/shared/tip overrides are implemented;
track the separate caption/layout candidates in the
[source audit and correction order](THEME-EFFECT-ADJUSTABILITY-AUDIT.md).
Current evidence and user review are recorded in the
[themes plan](THEMES-PLAN.md#group-icon-picker-tiles-d7f--2026-11-13).

The [themes plan](THEMES-PLAN.md) retains appearance requirements and acceptance.
This document proposes implementation boundaries and a migration sequence; it
does not accept the current visuals or schedule another theme family.

## Problem and intended result

Office 2019's Backstage changes altered Office 2024's caption coverage and removed
its animated neutral grey caption tint. Separating their settings alone did not
protect the established presentation. New themes need explicit ownership of their
visual differences while retaining the same control behavior and customization.

The proposed result is a small neutral foundation, sibling presentation classes,
and immutable theme definitions that compose those presentations with colors and
metrics. A new palette should reuse an existing presentation where appropriate.
A different visual structure should add a sibling implementation.

The [geometry consolidation](GEOMETRY-CONSOLIDATION-PLAN.md) remains the shared
foundation: adjustable metrics, DPI/rounding, layout snapshots, clipping, native
content bounds and regression contracts are reused. This migration changes
presentation ownership and selection. B1 changes no geometry calculation; later
extractions must preserve consolidated overrides and defaults rather than create
parallel layout engines or repeat the closed geometry campaign.

## Source assessment and reusable pieces

The pilot already had sibling inheritance. B1 unified composition, B2 made
measurement/defaults explicit, and B3 separated caption/motion policy from the
renderer and transition engines.
C1 applies the same registration boundary to header drawing and resolves header
geometry separately from the retained shared strip engine. Its implementation
and adjustable-effect API are recorded in the themes plan.

| Current owner | Retain | Remaining coupling |
| --- | --- | --- |
| [Backstage layout provider](../src/RibbonKit.WinForms/Layout/RibbonBackstageLayoutProvider.cs) and its 2019/2024 subclasses | One shared width, row, footer, narrow-host and DPI algorithm; sibling classes explicitly supply immutable defaults. | B2 removed live ribbon/model/font/renderer and Form access from `Build`; it consumes numeric inputs. Host bounds remain in the ribbon's window adapter and consume B3's resolved caption coverage. |
| [Backstage metrics](../src/RibbonKit.WinForms/Layout/RibbonBackstageMetrics.cs) | Validated nullable overrides, density minima and shared text/Auto-width arithmetic, resolved through the selected registration. | B2 metrics contain values rather than a theme reference or family selector. The measurement adapter gathers model/text/frame inputs once; the snapshot also retains the device page padding used by native clipping and previews. |
| [Backstage renderer base](../src/RibbonKit.WinForms/Rendering/RibbonBackstageRenderer.cs) and its 2019/2024 subclasses | Small sibling drawing strategies, borrowing the active public renderer's primitives and explicitly using a shared navigation-state helper. | B1 replaced the second selector with a resolver adapter. B3 removed live window/caption methods from both the base and the 2024 renderer; caption profile/frame painting are separate. |
| [Public Office renderer](../src/RibbonKit.WinForms/Rendering/RibbonOffice2024Renderer.cs) | Public virtual entry points and application subclass behavior. | It is also the default dispatcher for Office 2019. Its historical public name must not make it the base class for new family implementations. |
| [Window integration](../src/RibbonKit.WinForms/RibbonControl.Window.cs), [title integration](../src/RibbonKit.WinForms/RibbonForm.Title.cs) and [pane motion](../src/RibbonKit.WinForms/Animation/RibbonBackstageMotion.cs) | Real panes, transparent caption foreground, existing clocks, interruption and native ownership. | B3 resolves caption coverage/centering/tint and pane/title/page visual motion through the registration. Window integration gathers live facts; policies contain no ribbon/Form references. No timer or lifecycle consolidation was performed. |

Both existing entry-state implementations currently have the same recipe. Share
that recipe through an explicit helper when useful; do not duplicate it merely
to give each family a file. Siblings isolate decisions, not every line of code.
Existing inheritance tests prove the class relationship, not complete isolation
of selection, host integration or native presentation.

The parent design index's [Modern Backstage history](../../docs/history/library/01-core-controls.md#311-backstage-redesign-modern-2024--icons)
explains the Classic/Modern visual distinction. Native implementation and current
acceptance are owned by the native source and [themes plan](THEMES-PLAN.md).
WPF remains read-only and is not a migration dependency.

## Target responsibilities and dependencies

| Layer | Owns and consumes | Boundary |
| --- | --- | --- |
| Theme definition | Existing immutable `RibbonTheme`: colors/fills, adjustable logical metrics, structural selectors and authored policy values. Built-ins remain values in `RibbonThemes`. | No new public theme-definition hierarchy. Public Office 2024 defaults stay compatible; a neutral internal base need not remove those public defaults. |
| Family defaults | Immutable fallback values used only when an existing nullable setting is null, plus explicit structural choices. | No colors or live controls in layout defaults. Keep application values separate from fallbacks; never rewrite a theme to materialize defaults. |
| Pure layout base | Shared measurement, arrangement, scaling and safe clamps over explicit inputs; emits one layout snapshot for painting, hit testing, scrolling and native content. | No concrete-family registration, palette, window access or hidden Office defaults. Different structures may override arrangement; palette differences reuse it. |
| Pure presentation base | Drawing contracts and neutral, parameter-driven primitives; consumes resolved values and measured bounds. | No implicit Office selection recipe or window policy. A common state-paint helper is opt-in and receives its resolved fill/shape. |
| Family implementations | Sibling layout/drawing strategies and presentation-specific fallback/policy choices, initially the existing 2019 and 2024 classes. | Neither family inherits the other. Reuse shared algorithms and primitives; add an override only for a real structural difference. |
| Resolver and registration | One mapping from each surface selector to its matching layout/drawing pair; later extracted defaults and policy belong to that same registration. | Pure bases never choose concrete families. Resolve from effective theme settings, not built-in identity, theme name or renderer type. |
| Shared runtime | Commands, selection, binding, validation, focus, keyboard/accessibility, native controls, ownership and transition lifecycle. | Receives presentation results. Theme changes retain authored controls, state and handles and use existing refresh/settle paths. |
| Window adapter | Measures actual caption/host facts, invokes native hit testing and material/window services, applies resolved coverage/tint/title policy. | `RibbonForm` executes policy without deciding that all Backstage presentations share one Office design. Ordinary Form hosts remain supported. |
| Motion engines | Clocks, applying supplied timing/curves, progress, interruption/reversal, cancellation, reduced motion and disposal. | Consume resolved visual motion choices. Do not own family selection or create a second pane background to imitate real full-height panes. |

Dependency flow is **theme -> resolver -> family and resolved values -> shared
layout/drawing**, with host facts supplied by the window adapter. The runtime
retains the resulting geometry and feeds transition progress back to presentation.
Neither a pure base nor a family drawing helper reaches into `RibbonForm` to
discover which design is active.

For layout, the adapter gathers visibility/text measurements, authored width
constraints, caption reservation and effective frame insets from the active
renderer. The shared algorithm receives those values rather than a live Form or
reselecting a family. Keep existing rounding, density minima, short-host fallback
and safety clamps. Do not turn the current one-device-pixel Modern default border
into one logical pixel during this extraction.

Keep renderer-owned drawing caches and their existing disposal. Stateless
registrations can be shared; resolved geometry, motion, controls and mutable
caches cannot be global. Avoid a process-wide cache of application theme clones.

## One composition point and compatibility rules

The internal [RibbonPresentationResolver](../src/RibbonKit.WinForms/Rendering/RibbonPresentationResolver.cs)
returns a small `RibbonBackstagePresentation` registration containing the layout
provider and renderer. It resolves the selected layout's immutable defaults into
value-only metrics. The [measurement adapter](../src/RibbonKit.WinForms/Layout/RibbonBackstageLayoutMeasurement.cs)
gathers model/text/frame facts; shared layout consumes the resulting inputs.
B3 registers a [caption profile](../src/RibbonKit.WinForms/Rendering/RibbonBackstageCaptionPolicy.cs)
and [motion profile](../src/RibbonKit.WinForms/Rendering/RibbonBackstageMotionPolicy.cs)
beside each pair. Profiles resolve the existing theme flags, colors, overrides
and animation level into policies. There are no empty extension interfaces,
reflection discovery or public plugin framework.

| Backstage selector | Registered layout | Registered drawing |
| --- | --- | --- |
| `Modern` | `RibbonBackstage2024Layout` | `RibbonBackstage2024Renderer` |
| `Classic` | `RibbonBackstage2019Layout` | `RibbonBackstage2019Renderer` |

B1 removed `RibbonBackstageLayoutProvider.For` and replaced the switch in
`RibbonRenderer.BackstageStyleRenderer` with a forwarding adapter. Layout,
metrics, background/entry drawing, frame measurements and caption queries all
obtain the same registration for the same effective theme. Inert designer
previews use the same route, including their explicit candidate theme.

Preserve these rules throughout:

1. `Theme` assignment validates before changing application/effective theme or
   built-in selection. `BuiltInTheme = null`/Reset restores the last application
   theme; direct `Theme` assignment clears the built-in choice, even for equal
   values. Keep code-only/hidden theme serialization and per-ribbon isolation.
2. Keep `RibbonBackstageStyle.Modern/Classic` and existing properties unchanged
   for this migration. They already describe structure independently of palette;
   neither rename them nor expand them for hypothetical families now.
3. Every current nonnullable flag remains authoritative, including false.
   `BackstageStyle` must not silently enable title coverage, centering, accent
   navigation or monochrome icons. Nullable overrides retain their own fallback
   rules. A clone selecting Classic with title coverage disabled remains valid;
   selecting Modern on a 2019 clone must not reset its other authored settings.
4. Preserve `Painter = Renderer ?? existing default renderer` and the borrowed
   renderer's lifetime. The public `RibbonOffice2024Renderer` remains a compatible
   facade whose built-in methods delegate to internal siblings. Do not change
   the public `RibbonRenderer` flat presentation into Office painting.
5. Surface painting still enters through public virtual methods on the actual
   application renderer. Its background/entry/frame overrides take precedence;
   inherited built-in methods may delegate to the family. Text, focus, separator
   and metric hooks continue reaching that same owner. Do not dispatch family
   painting directly from a control and bypass those hooks.
6. Read one effective theme/registration for a measurement or paint operation;
   refresh it through existing theme, renderer, density and DPI invalidation.
   Open-surface switching uses the existing settle/refresh ordering, preserving
   native page identity and caption-foreground acquisition/release. Do not retain
  stale resolved frame values after renderer reassignment.
7. **User requirement, 2026-11-11:** while extracting a surface, replace remaining
   fixed visual effects with independently adjustable theme settings, preserving
   existing defaults and validation. Internal family profiles alone do not make
   an effect application-adjustable. Reuse existing settings before adding roles;
   keep native lifecycle, accessibility/safety clamps and timing execution shared.
   C1 adds selected-tab fill/ink, filled-state radius and underline thickness/
   bottom inset/hover ink. Apply this requirement to C2 and later slices too.
   The [retroactive audit](THEME-EFFECT-ADJUSTABILITY-AUDIT.md) records remaining
   B1–B3/C1/shared-helper gaps. A00–A04, A06–A09 and A11–A16 are implemented;
   A05/A10 retain separate scopes; A17 is implemented in D6. Backstage motion overrides use one immutable settings group,
   resolved with family defaults and captured by the existing engines.

The family registration binds compatible default pieces. Deliberate application
renderer overrides and independently authored settings remain supported escape
points, rather than mismatches to reject.

## Visual contracts to protect

| Area | Office 2024 | Office 2019 |
| --- | --- | --- |
| Backstage placement | Below a separate title bar. | Real navigation and page panes extend behind a transparent caption foreground. |
| Active Backstage caption | Neutral grey derived from navigation background, sliding/fading with Backstage. An authored Form/theme caption color retains precedence. Closing restores the ordinary caption. | Pane backgrounds supply their own caption coverage; no duplicated colored-caption animation. |
| Title movement | Preserve established ordinary title placement. | Place 75% toward the destination, slide the final 25% without text fade on opening/closing; recenter immediately on resize/maximize/restore. |
| Navigation/page | Existing inset selections, indicator and page frame. | Flat accent rail, full-width state fills, white icon silhouettes and flush page. |
| Other surfaces | Preserve established defaults. | Square theme-owned elements, circular Back exception, edge-to-edge body with retained inner padding, bordered/shadowed popups. |

B3 represents caption coverage, tint eligibility, title alignment and travel as
resolved values. Window integration supplies open/close state, actual pane
bounds/alpha, caption geometry, material state and authored color presence as
inputs. It preserves Form-color-then-theme-color precedence. The
Modern neutral tint applies only when neither is authored and the panes are
below the caption; it tracks the existing pane reveal/opacity and material rail
opacity. Classic's full-height panes supply their own backgrounds.

Retain 15% default pane travel, the current pane fade, the 25% title travel and
opaque title ink. Existing explicit zero/snap overrides remain effective. Moving
these choices into policy must not replace clocks or restart the rejected
fade-in-place design. Reuse existing progress/timing where effects must agree;
timer consolidation is outside this migration unless a focused reproduction
shows it is necessary.

Presentation policy supplies travel, opacity curves, easing and timing resolved
from the existing animation settings; engines execute those choices with the
shared lifecycle and motion-disable rules. Both families can reuse identical
policy values. B3 also moved the existing native-page travel/duration/curve into
the same profile; `TabContentAnimation` still selects Fade/SlideDown and its
existing validation, clipping, ownership and cancellation remain unchanged.

## Adding themes after the migration

- **Palette or spacing variant:** start from an existing `RibbonTheme` value with
  `with { ... }`, change its colors/fills/metrics and retain its structural
  selectors. No new layout/renderer class or built-in catalog entry is needed
  for an application-owned variant. A selected future built-in additionally
  needs catalog, designer selection/serialization and sample persistence work.
- **Different structure on one surface:** implement a sibling layout/drawing
  strategy for that surface and register the pair with its defaults/policy.
  Other surfaces reuse their existing registrations. Expose a new public selector
  value only when an actual selected theme needs it, with XML/API review and
  validation; do not infer structure from the palette's name or release year.
- **Application rendering:** retain the existing public renderer extension path.
  Internal family classes are not a new public extensibility promise. A new
  public family-registration API requires a concrete consumer need and its own
  compatibility decision.

Do not add a monolithic family subclass that duplicates the complete ribbon
layout engine. Future light-theme order remains unscheduled; dark variants stay
deferred. Definition values may compose existing surface families independently.

Apply this per surface rather than adding one all-or-nothing Office-family enum.
Headers already have `HeaderPresentation` for Underline/Filled pairing. For body
work, prefer resolved data for the existing border, margin, radius, shadow and
join choices; a different color or corner value alone needs no new strategy.
Assess an actual structural difference before adding a body selector. Do not
infer body or Backstage structure from a header choice or zero corner radius.

C2 implements this with explicit neutral/Office `RibbonBodyFrameDefaults`,
value-only `RibbonBodyFrameMetrics` and `RibbonBodyRenderer` frame algorithms.
The two light palettes share the body structure; no public body selector was
added. Existing public body radius/shadow measurements and frame/QAT/message/group
entry points remain on the actual renderer. Classic/Simplified divider painting
now shares the additive `DrawGroupSeparator` hook. `BodyAppearance` exposes frame
border widths, divider ink/metrics and nullable joins while preserving all null
defaults. Joins are gated by renderer capability and existing placement rules.
Command/caption arrangement and reduction, popup metrics and native ownership
remain in their existing shared owners for the later control-surface slices.

## Migration slices and relevant checks

The source map above completes planning inventory A; it does not establish new
automated evidence or visual acceptance. Implement the following separately.

| Slice | Bounded change and principal risk | Smallest relevant verification |
| --- | --- | --- |
| B1. Backstage registration — implemented | Register existing sibling pairs once and route current callers through it. Preserve existing algorithms, caption methods and motion. Risk: stale/mixed selection or bypassed application hooks. | Focused cases and the package-only comparison are recorded in the [themes plan](THEMES-PLAN.md#backstage-registration-b1--2026-11-10). The delivery criteria below remain the contract. |
| B2. Explicit layout inputs/defaults — implemented | Family fallbacks are immutable values; shared layout consumes measured content, host reservation and renderer shadow clearance. Page content padding is retained in the snapshot. Risk: DPI rounding, Auto width, footer fallback or native page inset changes. | Selected width/footer/spacing/frame/preview/ownership checks and exact default paint/geometry comparison are recorded in the [themes plan](THEMES-PLAN.md#explicit-backstage-layout-inputs-b2--2026-11-11). |
| B3. Caption and motion policy — implemented | Caption coverage/tint/title and pane/title/page visual motion resolve through registered profiles. Native services, clocks and lifecycle remain shared. Risk: the original 2024 caption regression, title fading, pane/caption drift, interruption or foreground leaks. | Focused caption/title/page/lifecycle regressions and two package-only Solid scopes with neutral/authored captions, open/close/reversal and resize are recorded in the [themes plan](THEMES-PLAN.md#backstage-caption-and-motion-policy-b3--2026-11-11). No full material matrix ran. |
| C1. Headers — implemented | Register sibling Filled/Underline drawing with explicit geometry defaults; keep the shared strip engine. Six additional theme roles make fixed header effects adjustable. Public renderer hooks, colored caption extension, File/QAT reservations and text clipping remain intact. | Focused tests, exact two-family before/after comparison and four package-consumer scopes are recorded in the [themes plan](THEMES-PLAN.md#header-presentation-c1--2026-11-11). |
| C2. Body and connected frames — implemented | Resolve body/group-frame decoration and connected-frame policy without changing reduction or ownership. Existing geometry, full-width 2019 body, native identity and public renderer hooks are retained; fixed frame effects have independently resettable settings. | Focused frame/group/QAT/message/motion and borrowed-popup checks, 80 exact image/geometry pairs and four package scopes are recorded in the [C2 record](THEMES-PLAN.md#body-and-connected-frames-c2--2026-11-11). No new geometry audit. |
| D1a. Ordinary command state/split chrome — implemented | Shared model-free perimeter/half-fill/separator drawing over existing bounds; immutable public appearance settings. Content, native fields, indicator glyphs and Application Menu recipes retain their owners. | 38 focused Release cases, 32 exact default image/geometry pairs and four package-consumer scopes plus tab checks are recorded in the [D1a record](THEMES-PLAN.md#command-state-and-split-chrome-d1a--2026-11-12). |
| D1b. Command indicators/raster treatment — implemented | Shared check/radio/QAT-mixed and disabled-raster recipes; adjustable borders, marks, dots, filled triangle dimensions and raster opacity/desaturation. Public hooks, native slots, model state and editor identity remain. No new family selector. | 38 focused Release cases, 16 exact default image/geometry pairs and four package scopes plus review-tab checks are recorded in the [D1b record](THEMES-PLAN.md#command-indicators-and-raster-treatment-d1b--2026-11-12). |
| D1c. Native editor appearance — implemented | Shared field/choice drawing and resolved decoration, adjustable field/choice borders/corners/insets/active ink/text padding. Actual renderer hooks and native editor state/lifecycle remain. | 53 focused Release cases, 64 exact default image/geometry pairs and four package scopes plus new-tab checks are recorded in the [D1c record](THEMES-PLAN.md#native-editor-appearance-d1c--2026-11-12). |
| D2. Ordinary popup-frame appearance — implemented | Shared frame/shadow metrics/drawing; adjustable border width, horizontal shadow offset and falloff. Actual renderer hooks, native body/choice/tip clearance and ownership remain. | 34 focused Release cases, 32 exact default paint/geometry pairs and four package scopes plus tab checks are recorded in the [D2 record](THEMES-PLAN.md#ordinary-popup-frame-appearance-d2--2026-11-12). |
| D3. Ordinary menu decoration — implemented | Shared heading/separator/scroll-band recipes with independently adjustable shape, weight, ink and opaque backing. Existing menu targets, native state, actual renderer overrides and separate gallery/Application Menu/Backstage paint remain. | 76 focused Release cases, 80 exact default paint/geometry pairs and four package scopes plus review/toolbar-restoration checks are recorded in the [D3 record](THEMES-PLAN.md#ordinary-menu-decoration-d3--2026-11-12). |
| D4a. Application Menu command state/split chrome — implemented | Shared row/footer perimeter and clipped split-half recipes; immutable shape/outline/divider/selected-primary settings. Existing fills, footer radius, explicit divider visibility, public item/content hooks and native geometry/state retain their owners. | 41 distinct passing latest Release cases, 32 exact default paint/target pairs and four package scopes plus new-tab checks are recorded in the [D4a record](THEMES-PLAN.md#application-menu-command-state-and-split-chrome-d4a--2026-11-12). |
| D4b. Application Menu backgrounds/inner frame/headings — implemented | Shared backing/pane/frame/divider and heading paint; resolved radius/stroke clipping shared with live/preview rows. Immutable settings reuse existing fill/ink/outer-frame roles; geometry and native owners remain. | 37 focused Release cases, 32 exact default PNGs and four synchronous offscreen package scopes plus review-tab checks are recorded in the [D4b record](THEMES-PLAN.md#application-menu-backgrounds-inner-frame-and-headings-d4b--2026-11-12). |
| D4c. Application Menu content — implemented | Shared content drawing/measurement with immutable icon/text/description/footer/arrow/separator overrides. Null/Empty preserves legacy rounding/allowances; authored metrics feed actual targets. Native fields/content and actual renderer hooks retain their owners. Live separators carry Application Menu context. | 39 distinct relevant passing latest cases, 64 exact default PNG/slot/measurement pairs and four package scopes plus review-tab checks are recorded in the [D4c record](THEMES-PLAN.md#application-menu-content-presentation-d4c--2026-11-12). |
| D5a. Gallery tile-state/headings — implemented | Shared built-in state/heading decoration over existing bounds with immutable GalleryAppearance settings. Actual primitive fallbacks, chrome/text/image/focus hooks, state eligibility, content/layout and presenter/native owners remain. | 26 distinct relevant cases, 64 exact default PNG/slot/layout pairs and four package scopes plus new-tab checks are recorded in the [D5a record](THEMES-PLAN.md#gallery-tile-state-and-headings-d5a--2026-11-12). |
| D5b. Gallery content/inline frame/utilities — implemented | Shared built-in content/glyph paint and popup minimum-height inputs with GalleryContent plus GalleryAppearance strip/utility settings. Preserve actual image/text/focus/chrome hooks, inline targets, state policy and native/custom presenters. | 45 distinct relevant latest passing cases, 32 exact default PNG/slot/layout pairs and four package scopes plus new-tab checks are recorded in the [D5b record](THEMES-PLAN.md#gallery-content-and-inline-frame-utilities-d5b--2026-11-12). |
| D6. Tips and Backstage badge placement — implemented | Shared tip drawing, title measurement weight and badge metrics; immutable TipAppearance outlines/insets and A17 clearances. Existing palette/corners/padding, actual renderer hooks, keyboard routes and passive/native lifetime remain. | 31 distinct relevant passing cases, 32 exact default PNG/slot/measurement pairs and four package scopes plus review-tab checks are recorded in the [D6 record](THEMES-PLAN.md#tip-presentation-and-backstage-badge-placement-d6--2026-11-12). |
| D7a. Options navigation and dividers — implemented | Shared native navigation state/marker/text/focus and rail/footer divider recipes with immutable OptionsAppearance. Preserve actual popup-radius hook, direct native text painting, layout/control identities and draft lifetime. | 32 distinct latest relevant passing cases, 16 exact native default PNG/geometry pairs and four package scopes plus review controls are recorded in the [D7a record](THEMES-PLAN.md#options-navigation-and-dividers-d7a--2026-11-12). |
| D7b. Options buttons and heading typography — implemented | Shared native button recipe and heading font/padding metrics; OptionsAppearance extensions reuse corners/state palettes and preserve scoped native fallback, direct image/text/focus, native button/default-cancel ownership and existing heading layout lifecycle. | 23 distinct relevant passing cases, 16 exact native default PNG/geometry pairs and four package scopes plus new-tab checks are recorded in the [D7b record](THEMES-PLAN.md#options-buttons-and-heading-typography-d7b--2026-11-13). |
| D7c. Options frames and choice presentation — implemented | Shared frame drawing and native search/closed-choice text metrics. OptionsAppearance adds radius/stroke/ink; real native clearance and search/name height consume authored outlines. Existing editor text/field/choice roles, actual renderer hooks and native controls/drafts retain their owners. | 28 distinct latest relevant passing cases, 32 exact native default PNG/geometry/region/text-slot pairs and four package scopes plus tab checks are recorded in the [D7c record](THEMES-PLAN.md#options-frames-and-choice-presentation-d7c--2026-11-13). |
| D7d. Customization command-list rows — implemented | Shared QAT/Classic/Simplified list drawing and icon/text metrics; adjustable row content, selection fill/corners and Simplified header weight. Direct native text/focus, borrowed artwork, native row targets/height and isolated drafts retain their owners. | 24 relevant passing cases, 16 exact native default PNG/geometry/icon-slot pairs and four package scopes plus new-tab checks are recorded in the [D7d record](THEMES-PLAN.md#customization-command-list-rows-d7d--2026-11-13). |
| D7e. Customization tree decoration — implemented | Shared tree drawing and pure decoration slots; adjustable tree selection/check/icon/gap/focus and existing shared Chevrons. Native expand/check/text targets, indentation/scrolling, direct native text/focus style, actual checkbox hooks, nodes and drafts retain their owners. | 28 distinct latest relevant passing cases, 16 exact native default PNG/geometry/hook pairs and four package scopes plus tab checks are recorded in the [D7e record](THEMES-PLAN.md#customization-tree-decoration-d7e--2026-11-13). Initial fixture/buffer retry evidence is separate from a clean aggregate. |
| D7f. Group icon-picker tiles — implemented; square correction awaits review | Shared tile drawing/metrics and adjustable selection/content. Follow-up square cells replace label-dependent rectangles; artwork size resizes None/artwork together and inset/outline clearance constrains both. Native items/handles/selection/scrolling, borrowed artwork, selected-only radius fallback, direct text/focus style and group validation/OK/Cancel retain their owners. | 20 distinct latest relevant passing cases, square native hit/scroll/name evidence and four refreshed package scopes are in the [square correction](THEMES-PLAN.md#group-icon-picker-square-geometry-follow-up--2026-11-13). The original D7f comparison is historical; corrected default geometry intentionally changes pixels. |
| D8. Caption/title appearance — next proposed for source review | Inspect A05 separate-caption tint eligibility/color/strength and the reported Backstage closing-title ink/clip handoff before choosing a bounded contract. Preserve ordinary/separate/full-height caption structure, explicit Form/theme caption precedence and existing pane/title clocks. | Focused caption/tint/contrast/clip/ownership and temporal close/reversal checks, unchanged stable-state comparisons and public-package scopes selected after source review. Preserve Office 2024 neutral animated tint and opaque title ink; physical frame-pacing review remains separate. A10 preferred page minimum and new families are outside this slice. |
| E. Selected catalog growth | Add one later chosen light definition, reusing surface registrations or adding an actually needed sibling. Risk: selection/Reset/serialization/persistence and unsupported consumer dependencies. | Definition/selection tests, relevant designer/generated-code checks, and a focused independent-consumer visual/state scope. Wait for existing appearance review before another theme. |

For each implementation slice, build affected native projects in their normal
outputs and select tests by actual dependency impact. Test classes above are
coverage sources, not instructions to run every listed class. Do not restart the
full suite or native matrix. Expand only for a new failure or unresolved risk.

## First implementation: B1 (implemented 2026-11-10)

Make the existing Backstage pairing authoritative before moving more behavior.
This is a selection refactor with unchanged intended appearance, not a new theme
or another neutral-caption fix; that fix is already committed.

1. Introduce the internal resolver/registration with the two existing sibling
   pairs. Move concrete construction/selection out of both base classes. Keep
   current renderer cache ownership/disposal; pass the active owner to helpers.
2. Route layout/metrics selection, default Office drawing/frame delegation and
   caption queries to that registration. Ensure standalone/default and designer
   candidate-theme paths also resolve through it. Preserve the current Modern
   fallback for calls with no attached ribbon at the adapter boundary.
3. Retain all public entry points, inheritance and precedence. Add behavioral
   regression cases for mixed style/coverage settings and custom frame metrics
   only where existing tests do not already cover those boundaries. Verify
   consistent geometry and owner callbacks, rather than merely testing that the
   resolver returns its own constants or expanding inheritance-only assertions.
4. Run the selected checks in B1 and one existing package-only comparison of
   2019/2024. Keep any new test/native filter explicit and record executed scope;
   use the permanent sample in normal Debug only when refreshing review delivery.

Completion criteria:

- One selection mapping covers both supported Backstage styles; no independent
  switch remains in layout, renderer or window code. Both families remain siblings.
- Built-ins, custom clones and mixed settings preserve their existing meaning.
  Invalid assignment preserves the prior theme/selection; application Reset and
  explicit flat/custom renderer behavior remain intact.
- Layout and paint agree on page frame geometry through the actual renderer's
  virtual hooks. Both ordinary and inert designer paths select the same family
  for the supplied theme, without constructing authored page content in previews.
- The focused comparison retains native page/editor handles and values, 2019's
  full-height panes and 2024's separate caption, including switching while open.
  Existing neutral/authored caption regression assertions still pass. Do not
  claim live timing evidence from static or synthetic checks.
- No intended pixel, motion, public API, serialization, sample-selection or
  authored Forms/resources change. Review the diff and record focused results
  and remaining human appearance/input gates in the themes plan.

B2/B3, C1–C2, D1a–D1c, D2–D3, D4a–D4c, D5a–D5b, D6 and D7a–D7f were delivered as separate slices.
D8 caption/title appearance and the deferred closing-title ink issue are next proposed for source review;
the consolidated geometry remains the foundation.

## Verification and delivery budget

- Select tests by changed surface and behavior. Reuse existing evidence and avoid
  broad suites/native matrices during bounded slices, as requested by the user.
- Protect the unchanged family with focused layout/paint assertions and, when
  motion changes, a short real-frame check. Static screenshots alone do not prove
  synchronization, retention or interruption behavior.
- Test custom settings and application renderer hooks, along with family switching
  while a surface is open. Keep checks meaningful rather than mirroring helper code.
- Use normal Debug output for review and Release for selected automated checks.
  Refresh the permanent Designer Sample only after an implementation slice changes.
- Keep WPF read-only, authored Forms/resources intact and Showcase-independent
  portability. No commits, pushes or publishing without a request.

## Current draft checkpoint

The planning discussion began at `a24a831` on `codex/Ribbonkit-Winform`. When the
starting prompt was prepared on 2026-11-10, HEAD was `47d42cd` (`Backstage fix`)
and the working tree was clean. The Backstage extraction and neutral-caption
correction are now committed. Do not treat that commit as architectural acceptance
or revert its fixes as part of planning. Recheck the live checkout before continuing.

The pilot introduced sibling `RibbonBackstage2019Layout` / `2024Layout` providers
and sibling `RibbonBackstage2019Renderer` / `2024Renderer` implementations. Both
renderer classes derive from a pure `RibbonBackstageRenderer`; both layout classes
derive from `RibbonBackstageLayoutProvider`. B1 added the single resolver;
the remaining family/host/default boundaries are covered by the later slices.

Evidence already obtained before planning: the first focused run passed 30 cases
and failed two new named-color identity assertions. Correcting those assertions
to compare actual color values produced a passing seven-case follow-up; these
runs overlap. Two package-only Solid display scopes passed at actual 120 DPI:
2019 retained full-height pane alignment and 2024 restored its animated neutral
grey caption and close restoration. Evidence is under
`artifacts/backstage-family-isolation/native/`. Debug Showcase/sample builds passed
without warnings/errors; local sample package is
`0.1.0-designer.local20261110T150147762Z`. Human appearance acceptance remains pending.

Planning refresh before the user's B1 go-ahead, 2026-11-10: branch/HEAD matched `codex/Ribbonkit-Winform`
at `47d42cd`. Entry had three modified planning documents; those edits are
preserved. Source inspection identified the separate selectors and remaining
host/default dependencies described above. No runtime/sample/test code changed
and no app, package refresh, build or test ran during that planning-only refresh.

The user then authorized B1 and asked whether geometry consolidation remained
useful. Its retained role is stated above. B1/B2 implementation and current
review/evidence are in the themes plan. B3 subsequently completed caption/motion
policy extraction. C1 subsequently extracted header presentation and exposed its
remaining fixed effects as theme settings. C2 subsequently extracted body/frame
decoration and connected policy while retaining consolidated geometry.
