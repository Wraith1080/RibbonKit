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
| 8: contextual material and tint | Open; next implementation slice. |
| 9: optional host effects | Open; opt-in capture/Acrylic scope still to be decided. |
| 10: Showcase consolidation | Open; retain the separate preview window until this slice. |

Broader reduced-motion, open-menu scaling and mixed-monitor gates remain open.
The [two 200% observations](#deferred-layout-observations-at-200) remain deferred,
with unconfirmed causes. Writer's saved appearance and W-glyph workaround remain
deferred under RKWF-026. This table does not establish blanket final acceptance.

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
   choice remains host policy for slice 8. The app-provided Editor page and
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

## Deferred layout observations at 200%

On 2026-09-30 the user requested that both observations below be recorded for
later investigation, outside the current slice 6 menu acceptance work.

- **Gallery item layout:** the user reports a different gallery-item layout at
  200% scaling. The affected gallery, exact geometry difference and cause are
  not yet established. The earlier View-tab Theme gallery selection-clipping
  report in [the theme plan](09-FUTURE-THEMES-PLAN.md) may be related, but these
  reports are not assumed to share a cause. Later compare 125% and 200% with
  actual window/item/viewport bounds, icon-bearing and text-only items, and
  selection/wrapping/clipping before changing layout or snapshot approvals.
- **RTL option-indicator test:**
  `Detached_rtl_lab_applies_crystal_and_restores_office_options` fails its first
  Office 2024 RTL checkbox-position assertion at the current 200% setup, before
  Crystal is applied or File opens. It fails identically in the new and prior
  outputs (§3.203). Later compare actual option bounds and native test-window
  sizes at 125%/200% to distinguish a control defect from a constrained fixture.
  The cause is unconfirmed; the recorded broader run remains 417 passed/1 failed.

Neither item is fixed or accepted. The successful RTL File-menu checks and
scrollbar-button acceptance retain their original scope. No runtime change,
build or test run was made for this deferred-issue note.

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
