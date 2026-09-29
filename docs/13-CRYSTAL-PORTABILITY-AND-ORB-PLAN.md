# Crystal portability and Office 2007 orb integration plan

> Status: slices 1–4 implemented on 2026-09-28; slice 5 implemented on
> 2026-09-30 with live visual review pending. Slice 6's QAT pass is implemented
> on 2026-09-30 with live review pending; its message-bar/application-menu passes
> and slices 7–10 remain proposed.
> The orb default adds no public API and has no live visual acceptance yet.
> The glyph-template property is additive and has no live visual acceptance yet.
> Writer integration is deferred by user direction. Keep its current saved
> appearance and W-glyph workaround; RKWF-026 remains open.
> The Crystal duplicate and portability audits are in `04-DESIGN-NOTES.md`
> §3.189–§3.191. Existing Office 2007 behavior is recorded in §3.94 and
> `07-OFFICE-2007-THEME-PLAN.md`.

## Outcome and boundary

A separate WPF application using RibbonKit's shared templates and
`ThemeManager.Apply(RibbonTheme.CrystalLight)` must receive the reusable Crystal control
appearance without referencing `RibbonKit.Showcase`, copying its resources or
calling its presentation helpers. Crystal dark mode must follow the same path.
Keep preview documents, Print Preview paint, application icons, saved preferences,
MDI editor content and other host-owned content in each application. Promote one
control family at a time; no Showcase-only theme coordinator is required.

Office 2007 shows the built-in orb when the application selects its theme
and has not explicitly set `Ribbon.ApplicationButtonShape`. The existing
property and `RibbonApplicationButtonShape` enum remain public and authoritative
for an explicit host choice, including a File tab on Office 2007 or an orb on
another theme. Switching themes must update the default, while clearing a
local value must resume following the theme. This changes the effective default
for Office 2007; it does not require changing the enum values or the shipped
public signature. It is still a behavior change for consumers that currently
rely on the Office 2007 File tab by default; setting the existing property to
`Tab` preserves that appearance. Choosing an application menu, Backstage
design, Aero frame or DWM backdrop remains an independent host decision.

The public `Ribbon.ApplicationOrbGlyphTemplate` property has type
`DataTemplate?`. `null` retains the current four-square vector glyph. A host
template replaces only the glyph inside the shared orb sphere; the sphere,
theme brushes, hit testing, accessibility, pressed states and Classic2007
transition stay in RibbonKit. The real orb and its private Classic2007 Backstage
proxy each instantiate the host template independently, so no visual is
reparented. `ApplicationButtonHeader` remains the localized or host-provided
accessible name/tooltip, and the proxy retains localized Back semantics. The
property has XML documentation and an entry in `PublicAPI.Unshipped.txt`; the
shipped baseline is unchanged.

## Implementation slices

1. **Theme-native orb default (implemented).** Every base theme dictionary has a
   typed application-button-shape default: `Orb` for Office 2007 and `Tab` for
   the others, with dark variants inheriting the same value. The shared `Ribbon`
   style binds to that resource so a local property value has normal WPF
   precedence. Resource switches update an existing ribbon through the style's
   `DynamicResource` setter. Showcase's theme-switch assignment was removed.
   Its File surface preference remains separate, and the RTL lab mirrors the
   resolved shape.
   Writer currently normalizes its saved shape to the selected theme and sets
   a local value. That consumer policy was preserved in this slice. Any Writer
   change is a separately scheduled follow-up.
2. **Theme-specific header-row inset (implemented).** The shared template uses
   each theme's `TabStripMarginNoApplication` and
   `QatTabRowMarginNoApplication` tokens, with RTL counterparts; dark variants
   inherit their base theme's values. A zero *additional* inset means the
   no-application margin equals the normal tab/QAT margin, preserving vertical
   and trailing spacing rather than setting the whole `Thickness` to zero.
   The audit found no token change necessary: Office 2010–2024 have zero extra
   inset, Office 2007 uses four DIPs, and Crystal uses 22 to clear rounded
   corners. A RibbonKit-only consumer test covers no File surface, QAT in the
   tab row or absent, LTR/RTL, theme switches, manually merged tokens and dark
   variants. All 63 captured scenes were compared; the nine differing Office
   2007 actual/diff pairs were reviewed and their approvals renewed to the
   current shared geometry. This slice adds no public property and does not
   change Writer.
3. **Orb glyph hook (implemented).** `RibbonKit.Templates.ApplicationOrbChrome`
   keeps the shared sphere and selects the built-in four squares or a host
   `DataTemplate` inside `OrbGlyph`. The real button and Classic2007 proxy bind
   to the same property while instantiating separate visuals. The proxy retains
   its inert content, localized Back name and glyph-only rotation. A consumer
   without Showcase resources verifies live template changes before and after
   Backstage opens, `null` fallback, menu and ordinary Backstage paths, and
   separate glyph visuals. Writer's W-mark migration from its full-chrome
   template and post-render injection is deferred. RKWF-026 remains open until
   that separate Writer work and its consumer checks pass. Showcase needs no
   orb adapter.
4. **Crystal control resources (implemented; live visual review pending).**
   Shared templates and theme tokens now paint menu rows, combo/text inputs,
   galleries, check/radio lenses and ScreenTips. The selected option lens and
   keyboard focus remain separate. Office palettes retain their light/dark
   appearance and custom accent behavior. Showcase no longer merges
   `Crystal.ControlStyles.xaml`, `Crystal.Inputs*`, `Crystal.Options*` or
   `Crystal.OptionTemplates.xaml`; its local tint variant sets shared tokens.
   A RibbonKit-only consumer realizes these families with light/dark and Office
   switches, manually merged tokens, RTL labels, popup open/close and a detached
   ScreenTip without `CrystalScreenTipPalette`. A locally scoped gallery popup
   background retains precedence over application theme tokens. Showcase's
   preview-specific ScreenTip tint scope remains a host choice. User screenshots
   are still required for live visual acceptance; no DPI acceptance is claimed.
5. **Customization pages (implemented; live visual review pending).** Shared
   Options and customization templates now provide Crystal's rounded list/tree
   frames, rows, navigation marker and actions through theme resources. Both
   built-in pages retain native scrolling, state bindings, RTL and localized
   labels. Office tokens retain compact list spacing and their existing primary
   and secondary action corner sizes; the 63-scene snapshot matrix passes without
   renewing approvals. The RibbonKit-only consumer verifies light/dark, focus
   separate from selection, scrolling, custom accent, Office restoration and
   manually merged light/dark dictionaries. `Crystal.Customize.xaml` and the
   template-part edits were removed. `CrystalCustomization` now only scopes the
   preview's scrollbar tint, which remains a slice 7 gap. The app-provided Editor
   page and dialog-opening policy stay in Showcase. User screenshots remain the
   live visual acceptance gate.
6. **Shared shell chrome (QAT pass implemented; slice remains open).** Shared
   ribbon templates and Crystal light/dark tokens now supply the below-ribbon
   QAT drawer: 16-DIP inset relative to the default body, open top rim, 10-DIP
   lower corners, 32-DIP minimum height, flipped glass border and zero-depth
   shadow. The minimized drawer retains its complete rim and 3-DIP top gap;
   existing message-state priority retains the drawer's open upper corners and
   spacing. `ContentCornerRadiusQatBelow` coordinates the rounded Crystal body
   independently of message-only body geometry. `ContentZIndexQatBelow` raises
   its seam shadow below the application-menu overlay. Office palettes retain
   their previous geometry, paint and shadow values. All new shared keys have
   matching Office defaults, and `DynamicResource` updates and explicit local
   value precedence are verified. This pass adds no public C# API.
   `CrystalQuickAccess` and its main/preview callers were removed after the
   RibbonKit-only consumer proved the replacement. Document underlay/fade and
   preview comparisons stay in Showcase; their optional host contract remains
   slice 9 work. The preview's message-only body rounding remains a scoped
   `ContentCornerRadiusTop` override in `Crystal.Light.xaml`, a gap for the next
   message-bar pass. The separate preview window and its launch path remain.
   In subsequent bounded passes, replace `CrystalMessagePresentation` and the
   control-paint/geometry portion of `CrystalApplicationMenuPresentation` with
   shared message and menu template/token behavior. Preserve QAT placement/minimized states, message
   actions, menu split rows and width/clip behavior. Keep application-menu
   content and the choice between menu and Backstage with the host. Treat the
   outside shadow and captured backdrop separately from ordinary menu paint.
7. **Utility chrome and scrollbars.** Move the generic utility-button rim and
   arrow states from `CrystalUtilityChrome` into shared templates. Express
   `CrystalScrollBars` paint and metrics through the existing shared scrollbar
   template and theme tokens, including native scrollbars in Options pages.
   Stop editing realized `Chrome`, arrow and scrollbar parts from Showcase.
8. **Contextual material and tint.** Make an ordinary `RibbonTab` render the
   accepted Crystal contextual surface from its contextual color; retire
   `CrystalContextualTab`. Decide and document whether custom Crystal tint is
   a supported library feature. If yes, move `CrystalPalette`'s whole-material
   tint/readability policy into reusable theming while preserving normal
   `ThemeManager.SetAccent` behavior for Office themes. Scope tint updates and
   cleanup per window or control where required; do not let one preview tint
   recolor unrelated windows.
9. **Optional host effects.** Design a separate opt-in integration, only if
   desired, for `CrystalMenuBackdrop`, `CrystalPopupBackdrop` and the host-owned
   capture/update lifecycle. Likewise evaluate `AcrylicGlassPresentation` as
   an optional cross-theme glass treatment. Core Crystal control styling must
   work without a capture source, Acrylic or a running Showcase. Retain
   `CrystalMainWindowPresentation` only for any genuinely app-owned effects;
   do not copy it into RibbonKit as a broad coordinator.
10. **Showcase consolidation after slices 5–9.** Inventory features still
    exclusive to `CrystalPreviewWindow`, move the important demonstration and
    comparison controls into the main Showcase window, and remove the separate
    Crystal preview window and its launch path after the main window covers them.
    Keep application-owned document content and optional host effects in Showcase;
    verify the main-window flows before deleting preview-only code and tests.

The Crystal Backstage designs and shared MDI templates are already in
RibbonKit. `CrystalBackstagePresentation` primarily selects the preview layout,
scopes its palette and binds preview document data; those host tasks do not
become library features. Reassess only a demonstrated detached-resource gap.

## Verification and completion gates

- The slice 6 QAT checkpoint on 2026-09-30 passed a Release solution build with
  zero warnings/errors, 100 focused tests and 417 eligible runtime tests. Both
  runtime runs exclude Writer-named tests and the deferred
  `Every_ribbon_button_family_consumes_the_shared_hover_glass` method.
  The single STA/WPF Application RibbonKit-only consumer test passed with every
  placement, minimized/message combination, mixed button/dropdown/split proxies,
  source-menu borrowing in overflow, physical RTL order, repeated light/dark and
  Office switches, manual token merges and scoped/local precedence.
  All 77 snapshot scenes passed: the original 63 approvals are unchanged, and
  14 new Crystal QAT scenes were added after actual-image review. They cover
  light/dark, expanded/minimized, message presence, RTL, tab-row overflow and
  synthetic 100/200% rendering. Image inspection first exposed false minimized
  fixtures; those now realize the control's loaded lifecycle in an offscreen
  test window and assert that the body is collapsed before bitmap capture.
  No Writer tests, manual Showcase launch, user screenshot acceptance, real DPI
  transitions or target-machine popup acceptance ran. These automated results
  do not close slice 5's pending live review or the remaining slice 6 passes.

- For the header-row inset slice, verify each theme's normal and
  no-application margins in a consumer without Showcase resources. Exercise
  QAT in the tab row and absent, LTR/RTL, light/dark variants and live theme
  switches. Inspect the actual/diff PNGs across the visual snapshot matrix
  before changing any approvals; report which scenes ran. Keep live screenshot
  acceptance with the user and do not infer DPI or popup acceptance from the
  existing Crystal screenshots.
- For the orb slices, test Office 2007 light/black default, every other theme's
  tab default, repeated theme switches, explicit `Tab`/`Orb` overrides,
  `ClearValue`, manually merged token dictionaries, designer recognition, the
  application menu, ordinary Backstage and Classic2007 proxy lifecycle.
  Test a custom vector glyph, `null` fallback, changed template after load,
  glyph-only rotation and localized Back/accessible names. Writer's migrated W
  is a deferred, separate acceptance gate for RKWF-026, not a gate for the
  library-only glyph hook.
- For each Crystal control family, realize it in a small consumer that uses
  RibbonKit resources without Showcase resources. Test light/dark switching,
  return to every Office baseline, accent/tint where supported, RTL,
  localization, keyboard focus, popup lifetime and cleanup. Add targeted
  regression tests for state or template behavior; avoid tests that merely
  restate XAML. Keep separate screenshots for visual acceptance.
- Public API and shared-template slices follow `CONTRIBUTING.md`: update XML
  documentation and `PublicAPI.Unshipped.txt`, build and test the solution in
  Release, review the diff and run `git diff --check`. Record focused tests,
  full-suite results and user-supplied live screenshots separately. Do not
  claim unreviewed DPI, popup or hover states. Package/designer distribution
  checks apply when those surfaces change.

Completion means a fresh non-Showcase consumer gets the shared Crystal
appearance and Office 2007's default orb, while an explicit shape or glyph
choice behaves consistently in the main ribbon and Classic2007 Backstage.
Remove each Showcase helper only after its replacement is verified; leave
host-owned effects and content in the host.
