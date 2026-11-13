# Earlier theme rewrite: effect adjustability audit

**2026-11-13: All identified A00–A17 corrections are implemented and verified.**
The initial source review changed documentation only. The user then authorized
the first correction batch; its current evidence is in the
[themes plan](THEMES-PLAN.md#backstage-fill-routing-and-motion-overrides--2026-11-11).
The C1 implementation was preserved. A17 followed in the
[D6 tip record](THEMES-PLAN.md#tip-presentation-and-backstage-badge-placement-d6--2026-11-12);
A05 followed in [D8a caption tint/title ink](THEMES-PLAN.md#caption-tint-and-title-ink-d8a--2026-11-13).
A10 followed in the [page-preference record](THEMES-PLAN.md#backstage-preferred-page-width-a10--2026-11-13).

Scope is B1 registration, B2 layout inputs/defaults, B3 caption/motion policy and
C1 header presentation, including the shared paint/motion helpers those paths
call. The committed baseline is `6162571` on `codex/Ribbonkit-Winform`, plus the
uncommitted C1 changes at initial review. Other control surfaces were not audited
as complete themes. C2 subsequently exposes its frame effects in its own
[implementation record](THEMES-PLAN.md#body-and-connected-frames-c2--2026-11-11).
Sample demonstration values, preview placeholder
decoration and test constants are not runtime theming gaps.

An effect counts as adjustable when an application can set its visual value
through an existing public setting and the affected drawing/layout path consumes
it. Moving a literal into an internal profile is useful architectural separation,
but does not provide an application setting. A whole-renderer override remains an
extension point, not a substitute for a simple theme setting.

## Findings and adjustment candidates

The following are source-confirmed gaps or coupled visual choices. Suggested
controls describe the capability; exact public API names remain to be chosen
during the selected implementation batch. New overrides should preserve the
current family default at null/Empty, remain per-ribbon, validate before mutation
and resolve through the existing registration/measurement paths.

| ID | Audited choice before correction | Adjustment/fix to provide | Owning source |
| --- | --- | --- | --- |
| A00 | Inset Backstage state fills in material/composited drawing bypass `BackstageNavigationHover` / `BackstageNavigationSelected`: the helper substitutes generic backdrop fills. Full-width rows take a different path. | Honor explicit navigation fills before material fallback, retaining current null behavior and authored alpha. This is a routing correction for existing settings, not a reason to add duplicate settings. | [Backstage renderer helper](../src/RibbonKit.WinForms/Rendering/RibbonBackstageRenderer.cs), [surface paint theme](../src/RibbonKit.WinForms/Backstage/RibbonBackstageSurface.cs), [generic backdrop fallback](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.cs). |
| A01 | B3 pane duration is 220/308 ms; title motion uses the same duration. Page duration is 130/182 ms. Values live only in internal profiles. | Authored transition durations, with current Subtle/Expressive defaults. Decide explicitly whether title duration stays coupled by default; do not add another clock. Define immediate/zero duration behavior before accepting it. | [Registration](../src/RibbonKit.WinForms/Rendering/RibbonPresentationResolver.cs), [motion profile](../src/RibbonKit.WinForms/Rendering/RibbonBackstageMotionPolicy.cs), [title clock](../src/RibbonKit.WinForms/Animation/RibbonCaptionTitleMotion.cs). |
| A02 | Pane travel, title and page easing use cubic easing; pane fade uses smooth-step. The curve enum/profile is internal. | Selectable validated visual curves using a bounded shared value type/profile. Keep clocks, interruption and reduced-motion execution shared. | [Motion policy](../src/RibbonKit.WinForms/Rendering/RibbonBackstageMotionPolicy.cs). |
| A03 | Pane opening/closing always fades across the native opacity range while sliding. `AnimationLevel.None` disables both; travel ratio zero provides fade-only, but there is no slide-only choice. | Independent pane fade enable/amount. Preparation's starting opacity and progress/completion must consume the same policy. Keep the native minimum alpha guard; retain opaque title ink. | [Pane preparation/progress](../src/RibbonKit.WinForms/Animation/RibbonBackstageMotion.cs), [opacity policy](../src/RibbonKit.WinForms/Rendering/RibbonBackstageMotionPolicy.cs). |
| A04 | Backstage page SlideDown travel is 8/12 logical pixels, selected only by animation level. | Authored page-slide distance with existing level-dependent defaults, separate from pane/title ratios. | [Registration and profile](../src/RibbonKit.WinForms/Rendering/RibbonPresentationResolver.cs), [page motion](../src/RibbonKit.WinForms/Animation/RibbonBackstagePageMotion.cs). |
| A05 — implemented D8a | Separate-caption tint resolves through the existing family policy and nullable/Empty BackstageAppearance members. | `CaptionTintEnabled`, `CaptionTintColor` (alpha/Transparent), `CaptionTintOpacity` (finite 0..1) expose independent choices. Defaults retain 2024 tint/full-height pane ownership and Form/theme authored caption precedence. Independent Reset retains navigation decoration; `TitleBarTintOpacity` stays separate. | [D8a evidence](THEMES-PLAN.md#caption-tint-and-title-ink-d8a--2026-11-13), [caption policy](../src/RibbonKit.WinForms/Rendering/RibbonBackstageCaptionPolicy.cs). |
| A06 | Backstage selected marker has fixed left inset 2, width 3, top inset 10 and total height deduction 20 logical pixels. Only visibility/radius and palette are adjustable. | Marker dimensions/insets and, if independent ink is needed, an explicit marker-color fallback. Keep the existing fractional-DPI rounding at null. | [Backstage entry drawing](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.Backstage.cs). |
| A07 | Back circle has fixed 2-logical-pixel inner clearance and 1.5-logical-pixel outline. Arrow coordinates use fixed 5/6-pixel extents and a 1.5-pixel stroke. Changing the Back target size does not scale the arrow. | Back visual inset, circle/arrow stroke and glyph size/scale. Keep target size separate; parameterize glyph dimensions rather than exposing every vector point. | [Back button drawing](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.Backstage.cs). |
| A08 | Full-width navigation-state fills force radius/border to zero/transparent; inset states use the active renderer's generic chrome. | Independent navigation-state corner/border appearance, with current full-width square/borderless and inset defaults. Width coverage and paint shape should remain separate choices. | [2019 sibling](../src/RibbonKit.WinForms/Rendering/RibbonBackstage2019Renderer.cs), [2024 sibling](../src/RibbonKit.WinForms/Rendering/RibbonBackstage2024Renderer.cs), [shared state helper](../src/RibbonKit.WinForms/Rendering/RibbonBackstageRenderer.cs). |
| A09 | B2 resolves row text clearance 16, separator row height 9 and trailing text clearance 8 directly from internal defaults, without theme overrides. | Nullable Backstage spacing overrides. Existing `MenuSeparatorHeight` affects menu metrics, not Backstage separator rows; avoid silently changing an existing menu-only setting's scope. Separator horizontal padding already has a consumed setting. | [Defaults](../src/RibbonKit.WinForms/Layout/RibbonBackstageLayoutDefaults.cs), [resolved metrics](../src/RibbonKit.WinForms/Layout/RibbonBackstageMetrics.cs), [shared layout](../src/RibbonKit.WinForms/Layout/RibbonBackstageLayoutProvider.cs). |
| A10 — implemented | Preferred page space resolves through Backstage metrics and the existing width-negotiation policy. | `BackstagePreferredMinimumPageWidth` accepts 0..1024 logical pixels; null retains family 240, zero removes the preference. Existing Auto/fixed navigation constraints, narrow/available-space clamps and native owners remain. Reset is independent; no geometry campaign was reopened. | [A10 evidence](THEMES-PLAN.md#backstage-preferred-page-width-a10--2026-11-13), [layout provider](../src/RibbonKit.WinForms/Layout/RibbonBackstageLayoutProvider.cs). |
| A11 | C1 filled tab/File state chrome has configurable fills/corners, but a border is always transparent with zero width. | Optional state border color/thickness if a theme needs it; retain borderless defaults and unchanged hit targets/body joins. Reuse a small state-appearance contract where appropriate rather than multiplying unrelated flags. | [Filled header sibling](../src/RibbonKit.WinForms/Rendering/RibbonHeader2019Renderer.cs). |
| A12 | Configured header scrollers force square corners and a one-device-pixel border. The shared horizontal arrow uses fixed 6-by-10 logical dimensions and 1.5-pixel stroke. | Scroller corner/border thickness and shared arrow size/stroke controls. Existing width, fill and border-color settings are already present; arrow work should reuse the shared primitive rather than duplicate a header-only glyph implementation. | [Scroller drawing](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.cs), [arrow primitive](../src/RibbonKit.WinForms/Rendering/RibbonArrowPainter.cs). |
| A13 | Unselected contextual tab ink applies a fixed 0.6 multiplier to authored color alpha. | Contextual unselected-opacity override, default 0.6. Keep selected contextual colors and C1's explicit selected ink precedence. This is distinct from material underline hover opacity. | [Contextual ink helper](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.cs). |
| A14 | C1 uses the existing header-arrival motion path: slide travel is 8/12 logical pixels, shared with tab content; expressive underline spring amplitude is fixed at 0.35. | Authored slide travel and indicator spring amount, retaining existing sharing/defaults. Coordinate this with the shared motion profile; do not create a header-only timer or duplicate body motion machinery. | [Motion scene constants and header/indicator rendering](../src/RibbonKit.WinForms/Animation/RibbonMotionScene.cs). |
| A15 | Open CommandMenu File highlight has fixed 3-logical-pixel inset. Fill follows global Accent and foreground is automatically black/white. Radius already has a setting. | Highlight inset and optional surface-specific fill/ink overrides, keeping current Accent/automatic-contrast fallbacks. Preserve contrast calculation as the default algorithm rather than exposing its mathematical coefficients. | [File highlight helper](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.ApplicationMenu.cs). |
| A16 | Shared focus drawing uses a fixed 2-device-pixel inset; Office focus outline thickness follows the drawing primitive's default. Corner radius already has a setting. | Shared focus inset/stroke settings if exposed, with existing native/Office defaults and visible keyboard-focus behavior. This helper affects more than headers, so keep one shared owner. | [Office focus](../src/RibbonKit.WinForms/Rendering/RibbonOffice2024Renderer.cs), [neutral focus](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.cs). |
| A17 | Backstage KeyTip badge size has padding settings, but placement keeps fixed 4-logical-pixel right clearance and 8/4-pixel size deductions. | Independent badge placement clearance, consuming shared tip geometry and retaining short-slot safety clamps. Preview-only placeholder padding is not this runtime gap. | [Backstage entry badge placement](../src/RibbonKit.WinForms/Backstage/RibbonBackstageSurface.cs), [existing badge metrics](../src/RibbonKit.WinForms/Layout/RibbonTipMetrics.cs). |

A00 was reproduced by four failing material/inset paint cases and is corrected
in the first batch. Its explicit/default fill and alpha evidence, unchanged-default
comparison and native package checks are recorded in the themes plan. A01–A04
are now exposed through `RibbonTheme.BackstageMotion`; title duration follows pane
duration, with no new clock. The table retains the original findings for context.

A06–A09 now use `RibbonTheme.BackstageAppearance` for marker/Back/navigation-state
decoration, plus `BackstageRowTextVerticalPadding`, `BackstageSeparatorHeight`
and `BackstageTrailingPadding` for shared spacing. Explicit null/Empty fallbacks,
alpha, zero/hide, renderer hooks and available-space clamps are retained. Current
evidence is in the [decoration/spacing record](THEMES-PLAN.md#backstage-decoration-and-spacing-overrides--2026-11-11).

A11–A16 now use `HeaderAppearance`, shared `Chevrons`, `FocusInset`/
`FocusStrokeWidth`, `TabContentSlideDistance` and `HeaderIndicatorSpringAmount`.
The [header/shared-effects record](THEMES-PLAN.md#header-and-shared-effect-overrides--2026-11-11)
retains exact default comparisons, authored-effect checks, API/range/reset
contracts and native package evidence. A05 is implemented with caption/title ownership in D8a;
A10 is implemented as a separate page-width preference. A17 is implemented by TipAppearance's
independent Backstage right/width/height clearance values through shared badge
metrics; existing padding is retained. D6 records default/short-slot/override/
reset and native/package evidence.

## Existing controls that already satisfy the request

- B1's style selectors, full-width navigation, accent/navigation palette,
  selected/disabled ink, monochrome icons, marker visibility, page border/radius
  and Back/marker radius already have settings. The 22%/28% derived accent mixes
  have direct public hover/selected fill overrides; new mix-percentage knobs are
  unnecessary to make those fills adjustable. Check A00's material consumption.
- B2 already resolves authored row minimum, navigation padding, Back size/inset/
  gap, icon inset/size/text gap, Auto width minimum, footer/page padding and font
  minimum. `RibbonBackstage` also exposes navigation width/minimum/maximum and
  material navigation opacity. Those settings should be reused, not duplicated.
- B3 already exposes pane travel ratio and title animated-distance ratio, title
  centering/coverage, global animation enable/level and Fade/SlideDown content
  style. The 15%/25% profile fallbacks therefore are not missing ratio settings.
- C1's six new effects cover selected fill/ink, filled corners, underline
  thickness/bottom clearance and hover ink. Prior settings cover hover/pressed
  fills, inactive text, indicator padding/radius, tab spacing/minimum, caption
  extension, scroller size/fill/border color and material hover opacity. Fixed
  defaults behind these consumed overrides are not themselves audit failures.
- Authored palette values such as `Surface`, `Accent`, `Border` and generic
  backdrop fills are already changeable. A new per-surface role is justified by
  needed independence, not by the mere presence of a palette fallback.

## Measurements, guards and intentional contracts

The internal 18-logical-pixel scrollbar allowance is a measurement concern.
Prefer supplying an actual native scrollbar requirement through B2's measurement
adapter, preserving existing fallback/parity until verified. It should not become
a fake scrollbar-width theme knob. Extra visual clearance belongs in spacing.

Keep native opacity's minimum alpha 1 (alpha zero changes child-window hit
testing), 16 ms timer pacing, HWND/compositor styles, repaint ordering, region
intersection, validation/cancellation, reduced-motion gates, overflow arithmetic,
Touch target minima and available-space bounds internal. The existing half-space
narrow-host negotiation is an algorithm; A10 concerns its preferred page size,
not replacing safe arrangement with arbitrary settings. Existing documented
radius/border/shadow caps remain separate safety/compatibility decisions.

Title ink remains opaque under the accepted contract. The supplied closing-title
flicker is a separate rendering defect; an opacity setting must not be used to
mask it. Preserve its ledger entry for the title work. Diagnostic preview labels,
their placeholder padding and sample colors remain consumer/tool presentation.

## Recommended correction order and evidence

1. Correct A00's existing-setting routing, then expose B3 motion choices A01–A04.
   Keep current family values at null and resolve policy once per transition.
   Validate ranges/curves and preserve interruption, zero/snap, reduced-motion,
   native editor ownership and opaque title behavior.
2. Backstage decoration/spacing A06–A09 and caption choices A05 are implemented.
   A10's separate layout preference is also implemented; preserve its default and safety clamps.
3. C1/shared helper gaps A11–A16 are implemented; arrow, focus and motion ownership
   remain shared. A17 is implemented with shared tip-placement work, without duplicating
   existing badge padding roles. Reuse state/metric groups when they avoid an
   excessive list of unrelated theme flags.
4. Resume C2 after the selected backfill, or follow the user's chosen scope.
   Carry the same audit checklist into
   later surface extraction so an internal profile does not get mistaken for a
   public setting.

Each implementation batch needs appropriate XML/Unshipped API declarations,
assignment-before-mutation validation, independent null/reset behavior, exact
unchanged-default comparison and focused affected native/package checks. Preserve
fractional-DPI arithmetic at null, actual public renderer precedence, candidate
themes and ordinary/material paths. Do not repeat the full suite/native matrix.

Verification for the initial audit was source/commit-scope inspection, existing public
setting/call-path checks, documentation/link review and `git diff --check` only.
No runtime/sample/test code changed during that initial review. The subsequent
A00–A04/A06–A09/A11–A16 implementation and their builds/tests/package evidence are recorded
in the themes plan. Earlier C1 evidence still belongs to C1; remaining candidates
are not established as implemented by these batches' checks.
