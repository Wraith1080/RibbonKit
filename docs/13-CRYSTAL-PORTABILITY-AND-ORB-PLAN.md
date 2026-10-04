# Crystal portability and Office 2007 orb integration plan

> Status and live acceptance are maintained only in the table below. Technical
> evidence stays in the linked history; routine confirmations update one existing
> row or active review item, without another history entry or mirrored summaries.
> Follow [AGENTS.md's quick documentation workflow](../AGENTS.md#quick-documentation-and-acceptance-updates).

## Current status and acceptance

| Slice | Current state and remaining review |
| --- | --- |
| 1–4: orb, header inset and control materials | Implemented 2026-09-28. Orb default/glyph and broader control/popup live acceptance remain open. |
| 5: customization pages | Implemented 2026-09-30. Broader page appearance review remains open; scrollbar review is covered by slice 7. |
| 6: QAT, message bars and application menu | Bounded light/dark live review complete 2026-09-30. Includes placement/overflow, notice dismissal/restoration, menu commands/reset, narrow/minimized/RTL states, visible footer at 200%, keyboard/focus and native scrolling. [Evidence §3.197–§3.203](history/01-library.md#3197-shared-crystal-qat-drawer--2026-09-30). |
| 7: utility buttons and scrollbars | Bounded live review complete 2026-10-01 in both reviewed themes: utility/arrows, modal/merged-caption controls, customization spacing, native scrolling/focus/RTL and 125% → 200% → 125% return. [Final acceptance §3.207](history/01-library.md#3207-slice-7-bounded-live-review-complete--2026-10-01). |
| 8: contextual material and tint | Bounded live review, separate dropdown DPI-return recheck and vertical Paste hover-width review passed 2026-10-01. [Slice evidence §3.208](history/01-library.md#3208-shared-crystal-contextual-material-and-scoped-palettes--2026-10-01), [popup correction §3.210](history/01-library.md#3210-popup-margins-follow-the-dpi-pixel-grid--2026-10-01), [hover correction §3.211](history/01-library.md#3211-vertical-split-hover-width-and-diagnostic-cleanup--2026-10-01). |
| 9: optional host effects | Bounded captured-surface and Glass look live review accepted 2026-10-01 on the normal Debug build, per the user's overall confirmation that everything seems okay. Broader motion, open-menu scaling and mixed-monitor gates remain open. [Evidence §3.212](history/01-library.md#3212-optional-captured-backdrops-and-scoped-glass-overlays--2026-10-01). |
| 10: Showcase consolidation | Bounded functional live review confirmed 2026-10-01: the user reports the consolidated demonstrations work as before. Document fade and QAT underlay also work, with a different visual result because the main Showcase uses a padded editable document card rather than the former preview's scrolling page; the supplied comparison screenshots document that host-layout difference. The separate preview and launch path are retired. Broader motion, open-menu scaling and mixed-monitor gates remain open. [Evidence §3.213](history/01-library.md#3213-main-showcase-consolidation--2026-10-01). |
| Deferred layout and hover checks | Corrected and verified automatically 2026-10-01: Theme gallery selected-edge clipping, the constrained RTL fixture and the excluded Office 2010 hover contract. Native 200% Theme gallery visual acceptance remains open. [Evidence §3.214](history/01-library.md#3214-theme-gallery-clipping-and-deferred-test-corrections--2026-10-01). |
| Gallery selected-row retention | Bounded live review confirmed 2026-10-01: the user changed tabs for 25 seconds and the selected tile stayed visible, confirming the reported reset is fixed. Shared RibbonKit also has automated coverage for tab reload and template replacement. Native DPI/monitor transitions and broader motion gates retain their separate scope. [Evidence §3.215](history/01-library.md#3215-gallery-selected-row-retention-after-tab-reload--2026-10-01). |
| Writer integration and deferred automated gates | Complete at the agreed integration scope; the user confirmed final Writer visual acceptance 2026-10-04. Implemented Crystal light/dark, Sidebar/Floating, scoped tint, Settings preview/persistence/rollback, W glyph hook and optional shared effects. The accepted appearance includes the Crystal workspace/ruler treatment, conditional paper/QAT fade, solid paper outline, Print Preview and status-bar glass, and two-column Appearance layout; ruler/guide choices persist. Recorded unfiltered Release solution tests pass, including Writer and Office 2010 hover; visual approvals/tolerances are unchanged. Synthetic 100/125/150/175/200% rendering, DPI returns and open surfaces have automated coverage. Native 200% Theme gallery review, Windows reduced-motion switching, native open-popup scaling, mixed monitors, genuine IME/production RTL and physical printing remain separately deferred live gates. [Evidence §3.216](history/02-writer-foundation.md#3216-writer-crystal-integration-and-deferred-validation--2026-10-02). |
| Project-wide pre-merge cleanup | Implemented 2026-10-04: scoped QAT surface policy, dynamic Showcase capture lifecycle, shared action chrome and tokenized Crystal Backstage geometry without fixed branding. Unfiltered Release tests and normal Debug/Release builds pass; see [evidence §3.217](history/01-library.md#3217-crystal-pre-merge-scope-and-resource-cleanup--2026-10-04). The revised Backstage appearance and dynamic popup/QAT capture remain for bounded live review; existing native DPI, motion and mixed-monitor gates retain their separate scope. Document edge fade remains host-owned. |

### Slice 8 live review

Use `samples/RibbonKit.Showcase/bin/CrystalSlice8/Release/net8.0-windows/RibbonKit.Showcase.exe`.
The agent did not launch Showcase. Record future confirmations in these items only.

1. **Passed 2026-10-01.** In Crystal light/dark, show/select Picture Format and Chart Tools;
   check idle/hover/selected text, rims and reflective marker. Check merged document
   tool tabs in MDI. In the separate preview, select Picture/Table and change the
   contextual tint; only that tab should change.
2. **Passed 2026-10-01.** Change main glass tint, then inspect inputs, options/customization,
   File and gallery paint. Values/selection and contextual colors should survive;
   reset tint and switch to Office to check its usual accent appearance.
3. **Passed 2026-10-01.** Keep main and preview open with different tints. Change/reset the
   preview's Glass tint and Office comparison; main/MDI/dialog paint must retain
   their own scope. Check focus/RTL and 125% → 200% → 125% return on these surfaces.

Separate dropdown DPI report — **Passed 2026-10-01.** After using the corrected
build, the user reports no remaining overlay after 200% → 150% → 125% and reopening
dropdowns at 125%. The intervening failure screenshot used the older diagnostic
build. This accepts the reported bottom-edge/corner paint correction, without
extending the three tint/contextual checks or broader DPI gates.

Vertical Paste hover — **Passed 2026-10-01.** The user confirmed the primary/arrow
hover-width correction after the requested 125% Office light/dark and Crystal rim
review. The temporary DPI logger is retired. Future local review uses the normal
Debug configuration and output folder. See [implementation and automated
evidence §3.211](history/01-library.md#3211-vertical-split-hover-width-and-diagnostic-cleanup--2026-10-01).

Broader reduced-motion, open-menu scaling and mixed-monitor gates remain open.
The [two 200% observations](#deferred-layout-observations-at-200) remain deferred,
with their recorded scope. Writer's appearance and W-glyph migration are implemented;
its current validation and remaining live review are in the Writer row above.
This table does not establish blanket final acceptance.

For the earlier audit, see `04-DESIGN-NOTES.md` §3.189–§3.191; Office 2007
history remains in §3.94 and `07-OFFICE-2007-THEME-PLAN.md`.

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
   template-part edits were removed. Slice 7 subsequently promoted the native
   scrollbar material and retired `CrystalCustomization`; the existing app tint
   choice remains host policy; slice 8 supplies the reusable palette factory.
   The app-provided Editor page and
   dialog-opening policy stay in Showcase. Broader slice 5 appearance acceptance
   remains separate from the completed slice 7 scrollbar review.
6. **Shared shell chrome (implemented).** Shared templates/tokens supply the
   QAT drawer, minimized/message geometry and shadows; message action paint and
   rounding; and the application-menu frame, pane and split-row material.
   Menu sizing follows available width/client height, scrollable content retains
   a stationary footer, and captured host blur does not affect measurement.
   Click dismissal exempts only ScrollBar descendants; ordinary commands still
   dismiss File. Office appearance and explicit local/scoped overrides remain.
   `CrystalQuickAccess`, `CrystalMessagePresentation`,
   `CrystalApplicationMenuPresentation` and `CrystalMenuShadow` were retired
   after RibbonKit-only proof. Host document/capture integration remains for
   slice 9. Detailed implementation and checks are in
   [§3.197–§3.203](history/01-library.md#3197-shared-crystal-qat-drawer--2026-09-30);
   current live acceptance is in the table above.
7. **Utility chrome and scrollbars (implemented).** Shared templates supply
   idle/hover/pressed/checked rims for minimize, modal close, QAT overflow,
   ribbon/tab arrows and merged captions. Rims follow Chrome bounds without
   changing padding or command routing; Office retains its original material.
   Crystal native scrollbars use the existing shared template: 14-DIP thickness,
   4-DIP corners, one-DIP thumb rim and 6/8/11-percent accent washes.
   Internal material selectors preserve resource scope and explicit overrides;
   no public C# API was added. Native input, commands, focus and RTL remain WPF.
   Shared customization pages round layout and each inset edge independently,
   retaining 2-DIP Crystal/1-DIP Office tokens and host padding/style precedence.
   `CrystalUtilityChrome`, `CrystalScrollBars` and `CrystalCustomization` were
   retired after consumer proof; the designer-safe scrollbar adapter remains.
   Host tint policy stays in Showcase for slice 8; capture is slice 9.
   Detailed implementation and checks are in
   [§3.204–§3.207](history/01-library.md#3204-shared-crystal-utility-buttons-and-scrollbars--2026-09-30);
   current live acceptance is in the table above.

8. **Contextual material and tint (implemented).** Ordinary `RibbonTab` now
   derives the accepted solid-color glass surface, hover/rim/text and marker
   through the shared template and internal material converter. Resource
   selectors are dynamic; unmarked host paint and explicit foreground/marker
   values pass through. Gradient/custom contextual brushes keep the ordinary
   renderer. All themes have matching opt-in/default keys; Office remains unchanged.
   `CrystalContextualTab` and its main/preview/MDI callers were retired after
   RibbonKit-only proof. No public tab property was added or overwritten.
   Custom material tint is supported through the additive generic
   `ThemeManager.CreatePalette(RibbonTheme theme, Color? accent = null, bool dark = false)`.
   Each call starts from fresh theme tokens. Office reuses its existing accent
   policy; Crystal preserves the accepted hue rotation/readability policy,
   geometry, alpha, neutral text and semantic notice colors. Creation does not
   change global theme/preferences or other scopes; owners replace/remove their
   dictionary for updates/cleanup. `SetAccent` behavior is preserved.
   Showcase's `CrystalPalette` now delegates control tint to RibbonKit and retains
   application-owned document paint only. Main's raw scrollbar-wash choice remains
   an explicit host token override. Captured backdrops remain slice 9; the separate
   preview remains until slice 10. See the linked §3.208 evidence; live acceptance
   is maintained above.
9. **Optional host effects (implemented).** `CapturedBackdrop` registers one
   application menu, dropdown/split button or group against a host-supplied WPF
   capture source. `Apply`, `Refresh` and `Dispose` separate opt-in paint from
   host content/update ownership. Shared capture engines retain the accepted
   blur, nonmeasuring layer, foreground exclusion and clipped paint, and release
   captures on close/unload/disable/disposal. Explicit popup backgrounds and
   bindings keep precedence; scoped popup tint uses the existing shared
   application-menu frame-band token. The three Showcase capture helpers were
   retired after replacement coverage. `ThemeManager.CreateGlassOverlay` provides
   the accepted cross-theme brush treatment; `AcrylicGlassPresentation` retains
   only host dictionary replacement/removal. The host still activates native
   Acrylic, decides when to use glass, supplies document paint/refresh timing,
   registers dynamic controls and disposes registrations. Core Crystal remains
   usable without either opt-in. `CrystalMainWindowPresentation` retains palette
   scope and app control discovery; no library-wide coordinator was added.
   See [README's integration examples](../README.md#theming--rendering) and §3.212 for API
   rationale, automation evidence and test limitations.
10. **Showcase consolidation — implemented 2026-10-01.** The main **Samples** tab
    now owns the Office 2024 comparison, temporary navigation/body scrolling and
    QAT overflow, checkbox/radio/disabled-gallery states, document title and
    optional scrolling text, edge fade and QAT underlay. Its context-tint command
    selects Picture Format and changes that tab alone. Existing **View**, Home
    and File controls cover theme/dark/tint, Backstage layouts, inputs, styles,
    messages and customization. Main-window tests passed before removal of
    `CrystalPreviewWindow`, `--crystal`, and the preview-only Backstage/ScreenTip
    helpers; the regression checks now target main-window flows or direct shared
    resource scopes. Document paint and capture discovery remain host-owned.
    See §3.213 for the inventory and automated evidence.

The Crystal Backstage designs and shared MDI templates are already in
RibbonKit. Main Showcase retains layout selection, palette scoping and explicit
document bindings as host tasks. Independent palettes remain available through
`ThemeManager.CreatePalette`; consolidation introduces no runtime/public API.

### Slice 10 live review

Use the normal Debug build at
`samples/RibbonKit.Showcase/bin/Debug/net8.0-windows/RibbonKit.Showcase.exe`.
The agent did not launch Showcase. Record acceptance in the slice 10 status row.

1. Choose Crystal in **View**, then inspect **Samples** in light/dark and after
   changing tint. Try checkbox/radio states, Enable options, Disable gallery,
   Change context tint and Compare 2024. Check that values and selections survive.
2. In **Try layouts**, add/remove scroll arrows, keep Home groups expanded while
   narrowing the window, and try/restore QAT overflow. Check both message actions,
   File surfaces/layouts and customization from the existing controls.
3. Edit the document title/text; add/remove scrolling text. Enable document edge
   fade and QAT underlay in Crystal, scroll, change QAT placement, and add/dismiss
   messages. Switch to Office and back; verify restoration and continued editing.
   These document effects start disabled and remain Showcase-owned.

## Deferred layout observations at 200%

The two observations recorded on 2026-09-30 were investigated on 2026-10-01
under separate user authorization; the earlier slice 6 acceptance keeps its scope.

- **Theme gallery selected-edge clipping:** the user clarified that only the
  View-tab Theme gallery was affected: the selected tile was clipped at its
  edges at 200%. Other galleries did not show this difference. The Showcase
  tile content height is corrected, and all six theme selections fit the strip
  at simulated 125% and 200%, including after popup open/close. Native 200%
  visual acceptance remains open.
- **RTL option-indicator test:** the initial Office failure measured hidden,
  zero-sized content when the fixture's Inputs group collapsed. The test now
  opens the group flyout when needed and measures visible checkbox/radio parts.
  Wide/narrow cases pass at simulated 125% and 200%, including Crystal,
  light/dark and LTR/RTL checks. Runtime placement is unchanged. The earlier
  417 passed/1 failed result remains historical evidence in §3.203.

The separately deferred **Office 2010 hover-glass contract** is also corrected:
its dropdown coverage now includes the split-half hover tokens, retains the
five-consumer threshold, and verifies equivalent glass in light/dark. It ran
in the current runtime suite without the former hover exclusion.

Bounds, rendered diagnostics, test results and scope are recorded in
[§3.214](history/01-library.md#3214-theme-gallery-clipping-and-deferred-test-corrections--2026-10-01).
Synthetic visual DPI checks do not establish native popup DPI transitions,
reduced-motion behavior or mixed-monitor acceptance.

## Remaining slice 6 live review

Current acceptance is maintained in the table above. The original bounded
sequence and results remain in history §3.197–§3.203. Reduced motion, scaling
while File stays open and mixed-monitor transitions are separate broader gates.

## Slice 7 live review

Current acceptance is maintained in the table above; the original review and
spacing corrections remain in history §3.204–§3.207. The user-reviewed build is
`samples/RibbonKit.Showcase/bin/CrystalSlice7Pixels/Release/net8.0-windows/RibbonKit.Showcase.exe`.
The agent did not launch it. Do not rerun automated checks for a prose-only
confirmation; keep the deferred 200% observations separate.

## Slice 9 live review

Use the normal Debug executable:
`samples/RibbonKit.Showcase/bin/Debug/net8.0-windows/RibbonKit.Showcase.exe`.
The agent did not launch Showcase. The sequence below describes the bounded
review scope; current acceptance is recorded in the status table.

1. In Crystal light/dark, open File, Paste/Select and a collapsed
   group. Check sharp content, blur/tint, rounded edges and outside shadow;
   scroll File and the document, resize, close and reopen. Check footer commands,
   native scrolling, keyboard/Esc/focus and RTL.
2. Change tint/dark mode, reopen those surfaces, then switch to Office
   and back. In the separate preview, use Office comparison and restore Crystal.
   Check that capture paint clears and returns without stale tint or growing menus.
3. With native Acrylic active, toggle Glass look in Crystal and Office;
   change accent/dark mode and turn Acrylic off. Check ordinary paint restoration
   and independent window palettes. Repeat a 125% → 200% → 125% return and reopening
   for the captured surfaces. Open-menu scaling, mixed monitors and broader motion
   acceptance remain separate until explicitly reviewed.

## Verification and completion gates

Detailed dated results are retained in history, rather than copied into this
plan on each confirmation:

- [Slice 6 QAT through menu checks, §3.197–§3.203](history/01-library.md#3197-shared-crystal-qat-drawer--2026-09-30).
- [Slice 7 implementation and spacing checks, §3.204–§3.207](history/01-library.md#3204-shared-crystal-utility-buttons-and-scrollbars--2026-09-30).
  The final pixel-gap build recorded zero warnings/errors, 418 eligible runtime
  passes, the RibbonKit-only consumer's 96-variant gap matrix and all 105 visual
  scenes. Existing approvals/tolerances were preserved. These are dated results,
  not a reason to repeat successful checks for a documentation update.

For subsequent implementation, keep the following gates:

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
  has automated main/proxy coverage; current live acceptance is recorded in the
  Writer status row above, separately from the library-only glyph hook.
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
