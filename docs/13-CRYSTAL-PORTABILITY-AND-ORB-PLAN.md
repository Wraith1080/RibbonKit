# Crystal portability and Office 2007 orb integration plan

> Status: slice 1 implemented on 2026-09-28; slices 2–9 remain proposed.
> The orb default adds no public API and has no live visual acceptance yet.
> The glyph-template property described below remains a proposal.
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

Add one proposed public property, `Ribbon.ApplicationOrbGlyphTemplate` of type
`DataTemplate?`. `null` retains the current four-square vector glyph. A host
template replaces only the glyph inside the shared orb sphere; the sphere,
theme brushes, hit testing, accessibility, pressed states and Classic2007
transition stay in RibbonKit. The real orb and its private Classic2007 Backstage
proxy each instantiate the host template independently, so no visual is
reparented. `ApplicationButtonHeader` remains the localized or host-provided
accessible name/tooltip, and the proxy retains localized Back semantics. The
exact API name and implementation require focused review before coding;
document the chosen property with XML comments and add it to
`PublicAPI.Unshipped.txt`, never the shipped baseline.

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
2. **Theme-specific header-row inset.** Audit the no-application header layout
   before renewing visual snapshots. The shared template already uses each
   theme's `TabStripMarginNoApplication` and `QatTabRowMarginNoApplication`
   tokens, with RTL counterparts; dark variants inherit their base theme's
   values. Keep those margins theme-owned and preserve the normal vertical and
   trailing spacing. For a theme needing no extra inset, set its no-application
   margin equal to the normal tab/QAT margin (zero *additional* inset), rather
   than setting the whole `Thickness` to zero. Give a theme an extra leading
   inset only where its actual header/body geometry calls for it. Check the
   current Office 2007 four-DIP shift against its intended corner treatment;
   do not assume the older approved image is correct or approve the new image
   without review. Cover no File surface, QAT in the tab row or absent, LTR/RTL,
   theme switches and dark variants. This is a shared-token adjustment, with
   no new public property and no Writer work.
3. **Orb glyph hook.** Keep `RibbonKit.Templates.ApplicationOrbChrome` as the
   shared sphere. Replace its fixed glyph child with a default/host-template
   choice inside the named `OrbGlyph` container, then pass the selected glyph
   template into the Classic2007 proxy when it is created or refreshed. Verify
   a live property change while the orb is present and after Backstage opens.
   Verify the library hook in a consumer without Showcase resources. Writer's
   W-mark migration from its full-chrome template and post-render injection is
   deferred. Leave RKWF-026 open until that separate Writer work and its
   consumer checks pass. Showcase needs no orb adapter.
4. **Crystal control resources.** Promote the reusable portions of
   `Crystal.ControlStyles.xaml`, `Crystal.Inputs*`, `Crystal.Options*` and
   `Crystal.OptionTemplates.xaml` into shared styles/tokens. Handle menu rows,
   combo/text inputs, galleries, check/radio lenses and ScreenTips as separate
   small edits. Preserve the distinct selected and focus visuals of the
   check/radio templates; do not discard them as mere copies. Keep theme-key
   parity and Office light/dark appearance. Make detached ScreenTips resolve
   the current theme without `CrystalScreenTipPalette` in a consumer.
5. **Customization pages.** Move the reusable `Crystal.Customize.xaml` list,
   tree, navigation and action visuals into RibbonKit's Options templates and
   theme resources. Keep the built-in pages, native scrolling, item behavior,
   RTL and localized labels. Remove `CrystalCustomization` template-part edits
   only after both Customize Ribbon and Quick Access pages match their accepted
   appearance, including rounded list/tree frames. The app-provided Editor page
   and dialog-opening policy stay in Showcase.
6. **Shared shell chrome.** In bounded passes, replace `CrystalQuickAccess`,
   `CrystalMessagePresentation` and the control-paint/geometry portion of
   `CrystalApplicationMenuPresentation` with shared QAT, message and menu
   template/token behavior. Preserve QAT placement/minimized states, message
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

The Crystal Backstage designs and shared MDI templates are already in
RibbonKit. `CrystalBackstagePresentation` primarily selects the preview layout,
scopes its palette and binds preview document data; those host tasks do not
become library features. Reassess only a demonstrated detached-resource gap.

## Verification and completion gates

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
