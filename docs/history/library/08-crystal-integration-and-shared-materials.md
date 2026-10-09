# Library history: Crystal integration and shared materials

> Archived implementation evidence. Dates, test counts and pending work describe
> their original checkpoints; later records may supersede them.

[History directory](../01-library.md) · [Current status](../../../04-DESIGN-NOTES.md#5-current-state--next-steps) · [Working agreement](../../../AGENTS.md).

### 3.184 Crystal Light MDI Demo integration — 2026-09-27

The detached MDI Demo had only the app-wide Crystal base palette. Its child
captions, borders and client area inherited Office 2024 MDI values, while its
white host background, text editors and document tabs missed the main Showcase's
local Crystal tint. Existing `Mdi.xaml` templates already consume MDI tokens, so
Crystal Light now overrides those keys without a new template or public API.
Showcase scopes the tint and editor paint to the demo window, enables its contextual
document tabs, and updates open demos when the main theme or tint changes. Leaving
Crystal removes that window scope and restores the Office presentation.

Two focused checks passed: one realizes a child and verifies its token-backed
caption, editor and merged tab through tint and theme changes; the other verifies
all reused MDI keys resolve in every Office base and dark variant. The Showcase
Release build passed. Live MDI appearance and DPI/RTL remain for user screenshots.

### 3.185 Connected first tab with no File button — 2026-09-27

The MDI screenshot showed the first selected tab's left outline ending above the
rounded top-left ribbon body corner. The same layout occurs when modal Print
Preview hides File. The shared `RibbonTabControl` template now insets the first
visible header-row item when both the application button and merged caption icon
are collapsed. A tab-row QAT takes the inset, keeping the normal gap from QAT
to tab; with the QAT in the title bar or below the ribbon, the tab panel takes
the inset. The selected-tab marker and connected body notch follow the measured
tab. RTL uses an inset on the opposite side. The body retains its rounded
corners, including with a below-ribbon QAT or message row. A visible merged
caption icon already reserves the leading space, so both use normal margins.

Every Office base palette defines the tab and QAT LTR/RTL margins, inherited by
its dark variant. Office 2007 adds a small leading inset; the flat
2010/2013/2019 and pill-tab 2024 palettes retain their normal spacing. Crystal
Light uses a larger inset to clear its 14-DIP body radius. Focused
realized-control checks cover File visibility, merged caption icon, tab-row QAT
spacing, RTL, below-ribbon QAT, Office 2007, Office 2024, and modal Print
Preview. Live screenshot acceptance remains with the user.

### 3.186 Crystal Light Localization/RTL lab and Backstage templates — 2026-09-28

The detached Localization/RTL lab inherited Crystal's shared control tokens, but
its host-owned menu, Backstage, message, popup and QAT presentation did not use
the main Showcase's Crystal adapters. Its built-in Options dialog also missed
the Crystal customization styling. The lab now reuses
`CrystalMainWindowPresentation` and `CrystalCustomization` instead of duplicating
their brushes or templates. `MainWindow` forwards theme and tint changes to the
open lab, as it does for MDI demos. The lab retains its independent RTL direction,
pseudo-localization provider and File-surface subscription.

One focused offscreen check covers a realized RTL lab, tinted Crystal resources,
the application menu, styled Quick Access Options, tint replacement and Office
restoration. The lab integration is Showcase-only. Live popup, menu and dialog
appearance remains for user screenshot review.

The Crystal Backstage templates had fixed English Back/File labels. Both designs
now bind control-owned labels, tooltips and automation names to live
`RibbonString` values, and the Back arrow follows the inherited direction.
WPF already mirrors the Backstage layouts, check box/radio indicators and
customization tree through `FlowDirection`; no column or dock swaps are needed.
An initial check measured positions relative to an RTL element and led to
unnecessary swaps. A corrected check uses physical screen coordinates and
verifies the original layouts, both Backstage designs, live provider changes,
the option indicators, customization tree and return to LTR. The shared template
change adds no public API or theme tokens. Application-authored demo labels
remain the host's responsibility; live visual acceptance is still pending.

### 3.187 Crystal Light Print Preview host paint — 2026-09-28

Print Preview already uses RibbonKit's shared modal tab, so its ribbon inherits
Crystal styling without a new template or adapter. Its Showcase-owned preview
canvas, page border and page text had fixed grayscale brushes. The Showcase app
now defines their Office baseline as host resources, and its window-scoped
Crystal palette overrides those resources. The paper remains white. Crystal's
existing tint rotation updates the canvas and border while preserving readable
page text; leaving Crystal restores the original Office paint.

One focused noninteractive check covers modal entry/exit, a tint change and
baseline paint under every Office theme and supported dark variant. There is no
RibbonKit runtime or public API change, and no new shared theme token. Live
Print Preview appearance remains for user screenshot review.

### 3.188 Crystal dark palette — 2026-09-28

`ThemeManager.SetDarkMode` now loads `Tokens.Crystal.Dark.xaml` for the existing
`RibbonTheme.CrystalLight` value. The overlay reuses the Office 2024 dark baseline
for unchanged controls and replaces Crystal's shared glass, MDI, Backstage, KeyTip,
message and connected-tab brushes. It preserves the light palette's geometry and
drawing structure, which the Showcase contextual adapter uses for its marker.
There is no new public theme value, shared template or token key. Every shared key
in the overlay exists in all five Office palettes and their dark variants.

Showcase adds a dark window palette and dark resource scopes for its existing
input and option styles. The theme toggle rebuilds the active tint and refreshes
open MDI and Localization/RTL demos. Print Preview's canvas darkens while its
white paper and ink remain readable. Contextual tabs derive their dark treatment
from the actual palette in their resource scope, so the separate light-only
Crystal preview stays light even when the application has a dark preference.

Focused offscreen checks cover runtime light/dark/Office switching, dark host
styles, contextual markers, MDI, Print Preview and the RTL lab. The Showcase
was not launched; live material, contrast and DPI acceptance remain for user
screenshots. Chart Tools and the Options Editor need no further phase-two edit by
user direction; the deferred Office Glass and View overflow reviews remain open.

### 3.189 Crystal duplicate audit — 2026-09-28

The user accepted Crystal dark mode in the main Showcase, MDI Demo,
Localization/RTL lab, and Print Preview screenshots. This records those reviewed
surfaces only; popup states and DPI scales remain unreviewed. Chart Tools and the
Options Editor need no further phase-two work.

`Crystal.OptionTemplates.xaml` still copies much of the shared check/radio
structure, including native content, mark, hover and disabled behavior. Its
separate focus ring and selected glass lens are distinct from the shared
accent-filled indicators, including in the dark palette. Replacing these
templates with the shared ones would change the accepted appearance, so they
remain Showcase-scoped. They inherit `FlowDirection` and display the caller's
header; they add no localization branch. The shared Crystal Backstage dictionary
already owns both layouts, localized labels, direction-aware Back glyph and selection
behavior. The Showcase `Crystal.Backstage.xaml` dictionary only merged it.
Customization and message actions now merge the shared dictionary directly;
the wrapper was removed. No RibbonKit runtime template, token or public API
changed.

The wider pass found shared tokens and templates already driving ordinary
ribbon controls, split hover, MDI chrome, Backstage and scrollbars. Showcase's
`Crystal.ControlStyles.xaml` scopes glass input, gallery and ScreenTip paint;
`Crystal.Customize.xaml` styles Showcase's dialog presentation. The backdrop
capture, QAT/document underlay, menu shadow, utility rim and detached preview
bindings depend on host visuals or content, so they remain Showcase helpers.
These are distinct presentation choices rather than further safe wrappers to
remove in this slice.

Presentation wiring remains spread across these Showcase owners:

- `MainWindow.ApplyTheme` and `RefreshCrystalTint` apply the standard shell
  adapter; `UpdateDetachedDemoThemes` forwards changes to open demos.
- `LocalizationRtlDemo.ApplyCrystal` separately creates the same shell adapter;
  `MdiDemo.ApplyCrystal` scopes its own palette, editors and contextual tabs.
- `CrystalPreviewWindow.UpdateCrystalDetails` applies each preview adapter for
  comparison; its Backstage helper retains the document-title binding.
- `MainWindow.OpenOptionsDialog` and the RTL lab's Options path opt into
  `CrystalCustomization` for their own dialogs. Print Preview uses the shared
  modal-tab template and Showcase page-paint resources.

The next bounded slice is an optional Showcase-only registration for the
standard shell: add a small `CrystalShellRegistration` helper that constructs
`CrystalMainWindowPresentation` from the explicit window, ribbon, message bar,
menu and Backstage references supplied after `InitializeComponent`. MainWindow
and LocalizationRtlDemo each keep their own registration and call its `Apply`
on theme/tint changes; closing a detached window releases its registration.
Keep MDI's editor/contextual-tab treatment, preview comparison, and dialog
styling in their owning hosts. There is no need for global discovery, a broad
coordinator or a RibbonKit API. The deferred Office Glass, View overflow, 200%
gallery clipping and Office 2010 hover checks remain separate.

### 3.190 Crystal customization list frames — 2026-09-28

The user's main Showcase screenshot confirmed the message panel, and the Options
dialog screenshot showed square outer frames on the Customize Ribbon command
list and tree. The requested round treatment also applies to both Quick Access
Toolbar lists. All four controls are template parts of RibbonKit's shared
customization pages; their data, navigation and scrolling remain owned there.

Showcase's `CrystalCustomization` adapter now applies one local rounded frame
template to those parts. It preserves each control's background, border,
padding, item presenter and scroll viewer, while the Crystal row styles remain
unchanged. No runtime template, theme token or public API changed. The Showcase
Release build and three focused checks passed in separate processes: frame and
scrolling on both pages, existing tree behavior, and the detached RTL Options
path. The dialog was not visually reaccepted at the new corners; live review
and DPI scales remain with the user.

### 3.191 Showcase presentation portability audit — 2026-09-28

A Showcase presentation inventory exposed a gap after Crystal became a public
`RibbonTheme` choice: `ThemeManager.Apply` installs shared palettes, while the
sample still supplies several generic control appearances through local styles,
template-part changes and a `CrystalContextualTab` subclass. Another consumer
can use RibbonKit and its shared Crystal palette and Backstage designs, but
theme selection alone will not reproduce all of Showcase's Crystal visuals.
The proposed Showcase-only adapter registration would organize sample wiring
without closing that gap and is superseded in the future-themes plan.

The reusable candidates are input, option, ScreenTip and customization styles;
QAT, message, application-menu, utility and scrollbar chrome; contextual-tab
material; and full-material tint policy. They require bounded promotion to
RibbonKit with Office parity, RTL/localization and another-consumer checks.
Menu/popup snapshot blur depends on host visuals and should remain explicitly
optional pending a reusable integration contract. `AcrylicGlassPresentation`
is the only other named Showcase presentation adapter; it provides an optional
Glass look rather than the baseline Office themes. Preview comparison,
document-edge fade, MDI editor content, Print Preview paint, icons and
preferences remain application-owned. This audit changed documentation only;
no runtime/public API behavior or visual acceptance is claimed.

The same inventory also checked non-Crystal theme wiring. Showcase explicitly
selects the built-in `RibbonApplicationButtonShape.Orb` for Office 2007, picks
the application menu as its conventional File surface, and exposes optional
2007/2010 Aero frames, Backstage designs and DWM backdrops. The orb template,
menu, frame and Backstage behavior live in RibbonKit; Showcase chooses among
their public options and mirrors the File-surface state to its RTL lab. An app
using `ThemeManager.Apply(Office2007)` must opt into the orb and choose its
File surface separately. Those host choices were omitted from the initial
portability summary and are distinct from the Showcase-only Crystal styling
listed above.

The orb's built-in four-square glyph is fixed in the shared template. Writer's
`RKWF-026` records a separate missing host-level glyph override and its
app-owned workaround; Showcase does not have an orb-glyph adapter. No new
runtime API is approved by this inventory.

### 3.192 Theme-native Office 2007 orb default — 2026-09-28

The shared Ribbon style now reads a typed application-button-shape token. Office
2007 supplies `Orb`; the other base Office palettes supply `Tab`. Dark variants
inherit that value, and Crystal inherits Office 2024's `Tab`. WPF invalidates the
style's `DynamicResource` when `ThemeManager.Apply` or a consumer's merged token
dictionary changes, so an existing ribbon follows the theme without a new
effective-shape property. A local `ApplicationButtonShape` remains authoritative
through switches, and `ClearValue` resumes the token default. No public symbol
was added or changed. Showcase no longer assigns the shape during theme changes;
its application-menu preference and RTL lab forwarding remain separate. Writer
still restores its saved local shape using its existing normalization policy.

A new test project referencing RibbonKit alone proves the loaded control's shape
and realized orb across every theme, Office 2007 black, manual token changes,
explicit `Tab`/`Orb`, `ClearValue`, and Classic2007 proxy attachment. The designer
preview's local token lookup and existing Classic orb lifecycle checks passed in
six focused tests; six focused Writer appearance-preference tests passed. The
Release solution build passed with zero warnings and errors. The full solution
test run did not pass: the portability test passed, while the runtime and Writer
suites reported contract, source-inspection and WPF cross-thread failures outside
the new consumer test. The Office 2007 snapshot differed at 929 pixels in tab
and QAT detail; the same mismatch remained with the new style setter temporarily
removed. Its approved image was not changed. No Showcase live window, DPI,
popup or target-machine visual acceptance was run.

### 3.193 Theme-owned no-application header inset and snapshot renewal — 2026-09-28

The shared ribbon template already selected separate LTR/RTL no-application
tab and tab-row QAT margins from each theme. Comparing each value to its normal
margin showed zero additional inset in Office 2010, 2013, 2019 and 2024; Office
2007 adds four DIPs and Crystal adds 22 to clear their rounded body corners.
Their dark variants inherit the base values. No new token, public API or runtime
geometry change was needed. The template comment now states that a theme can
keep its normal margin when it needs no extra inset.

A consumer test referencing RibbonKit without Showcase verifies the realized
tab/QAT margins and corner clearance where needed, with and without the File
surface, tab-row QAT or no row QAT, LTR/RTL, all light/dark themes, live
theme switches and a manually merged token dictionary. It shares one STA WPF
`Application` with the orb-default test; WPF rejects a second `Application` in
the same test process.

The visual harness can now capture every scene and its mismatch diff without
changing approved images. All 63 current scenes were compared against their
approvals. Only nine Office 2007 images differed, at the header QAT and tabs;
the ribbon body and message surfaces matched. Those nine approvals were renewed
after reviewing their actual/diff PNGs. The Release solution build and focused
visual and portability tests passed. The full solution test run remained red:
one deferred Office 2010 hover-glass contract and seven Writer failures outside
this slice. No Showcase live window, real DPI, popup or user screenshot
acceptance was run; Writer integration remains deferred.

### 3.194 Portable application-orb glyph template — 2026-09-28

`Ribbon.ApplicationOrbGlyphTemplate` is an additive nullable `DataTemplate`
dependency property in the unshipped API list. `null` keeps the existing
four-square vector. The shared `ApplicationOrbChrome` still owns the sphere,
theme state brushes and named `OrbGlyph` rotation target; only the glyph child
changes. The real application button and the private Classic2007 Back proxy
each bind to the Ribbon's property through their own internal button state, so
changing or clearing it updates both without sharing a visual or depending on
a Ribbon ancestor after the real button moves into the application-menu overlay.
The proxy's content remains an inert string, with localized Back tooltip and
automation name. Writer's saved appearance and W-glyph workaround are unchanged;
RKWF-026 remains open for its separate migration.

A RibbonKit-only consumer test realized custom vector templates in both buttons,
verified separate glyph instances and unchanged sphere visuals, changed the
template while Classic2007 Backstage was open, restored the default with `null`,
checked the glyph-only rotation target and accessible Back name, and exercised
the application-menu and ordinary Backstage paths. Ten focused Classic/language
tests and the visual test covering 63 approved scenes passed. The Release
solution build passed with zero warnings or errors. The runtime suite passed
434 tests with the deferred Office 2010 hover-contract method excluded; no
Writer tests were run by user direction. The 1.0.0 package packed and validated
for clean net8/net9 WPF consumption after the validator allowed NuGet's current
`nuget.psmdcp` metadata filename. No Showcase window, live screenshot, real DPI
or popup acceptance was run.

### 3.195 Shared Crystal control resources — 2026-09-28

Crystal menu rows, combo/text inputs, galleries, check/radio controls and
ScreenTips now use the shared `Controls.*.xaml` templates with dedicated
`Tokens.Office*.xaml` and `Tokens.Crystal.*.xaml` keys. Office tokens preserve
their prior light/dark paint and accent behavior. The option templates retain
separate selected lenses and focus rings; Crystal's detached ScreenTip surface,
reflection and shadow resolve through theme resources. Showcase no longer merges
its copied control styles or input/option templates. Its tint palette now sets
the shared material keys, while its preview-specific ScreenTip tint scope remains
host-owned. This slice adds no public API.

The gallery popup needs a concrete brush because its content leaves the ribbon's
resource tree. Its resolver now uses the dedicated popup token and still honors
a nearer host-scoped ribbon-content background, including a local override from
an existing consumer. A WPF `StaticResource` element used as a dictionary alias
did not register a usable brush key; the final tokens use actual brush entries.

The Release solution build passed with zero warnings/errors. The non-Writer
runtime suite passed 417 tests with the separately deferred Office 2010
hover-glass contract excluded. The RibbonKit-only consumer test passed with
Crystal light/dark, Office restoration, manual token merges, RTL labels,
detached ScreenTip, gallery popup theme switching and a local popup override.
The visual snapshot test passed its approved scene matrix. An earlier broad
runtime run unintentionally included Writer-named consumer-friction tests and
found the local gallery-popup regression; that test was not rerun after the
fix. The final runtime run excluded Writer-named tests, and the Writer test
project was not run by user direction. No Showcase live window, user screenshot,
DPI or target-machine popup acceptance was run. The accepted earlier Crystal
dark screenshots do not close those remaining gates.

### 3.196 Shared Crystal customization pages — 2026-09-30

Both built-in customization pages now obtain Crystal list/tree frames, rows,
navigation and action materials from shared Options/customization templates and
theme tokens. The tree retains its view-model expansion/selection bindings and
native scroll viewers; selection and keyboard focus use distinct border states.
Crystal primary actions retain the semibold label and 12-percent accent wash.
Separate primary/secondary corner tokens preserve the existing Office button
sizes. No public C# API was added or changed.

Showcase's `Crystal.Customize.xaml` was removed, along with its realized
template-part edits. `CrystalCustomization` now only scopes the preview's
scrollbar tint. That remaining visual difference belongs to slice 7; the
RibbonKit-only consumer does not depend on the helper. Application-provided
Options content and dialog-opening policy remain in the host. The plan also
records the user's final consolidation requirement: after the promotion slices,
move important Crystal preview-only demonstrations to the main Showcase before
removing the separate preview window and launch path.

The first snapshot run stopped at the Office 2024 RTL QAT customization scene.
Its approved, actual and diff PNGs showed fewer visible commands because Crystal
row margins had been applied to Office. Inspecting the native WPF template
established the original compact padding, margins and frame inset. Those values
now have Office tokens, while Crystal keeps its prior spacing. The final visual
test passed all 63 approved scenes without changing approvals or tolerances.

The Release solution build passed with zero warnings/errors. The final runtime
run passed 417 tests, excluding Writer-named tests and the separately deferred
Office 2010 hover-glass contract. The earlier focused Crystal/localization/dark
template run passed 34 tests. The RibbonKit-only consumer test passed with both
customization pages, light/dark switches, every Office baseline, manually merged
light/dark dictionaries, native scrolling, custom accent, localized RTL headers,
and navigation/list/tree/action focus separate from selection. Theme XML parsed
without duplicate direct resource keys. Diff review and `git diff --check`
passed. Writer tests, live Showcase, new screenshots, real DPI transitions and
target-machine popup acceptance were not run; those gates remain unclaimed.

### 3.197 Shared Crystal QAT drawer — 2026-09-30

Slice 6's first bounded pass moves the reusable QAT behavior from Showcase into
the shared ribbon template and Crystal light/dark tokens. The drawer keeps its
16-DIP inset from the default body, open upper rim, 10-DIP lower corners, compact
padding, 32-DIP height reserve, flipped glass border and zero-depth shadow.
Minimization uses a complete rim and 3-DIP gap; message-state trigger priority
keeps the existing drawer spacing and upper-corner geometry. Dedicated
`ContentCornerRadiusQatBelow` and `ContentZIndexQatBelow` resources coordinate
the body rounding and seam shadow while keeping the application menu above both.
`QatExtender.Border` and `QatExtenderShadow` separate drawer paint from body paint.
Office base/dark tokens retain their previous realized values. New metrics have
matching defaults in every base palette; dark palettes inherit unchanged metrics.
No public C# API or shipped API baseline changed.

A RibbonKit-only consumer first reproduced the missing lower body rounding,
then verified the shared replacement on its existing single STA/Application.
It exercises all placements, physical RTL ordering, mixed source-linked button,
dropdown and split proxies, stable drawer height, real minimized body collapse,
message combinations, overflow menu borrowing/return, light/dark and Office
switches, manual dictionaries and resource/local-value precedence. The dark
message case exposed the Office dark dictionary overriding Crystal's drawer
continuation margin; Crystal dark now explicitly reasserts those two metrics.
The layering regression now verifies the QAT seam token is conditional on
BelowRibbon and remains below the application-menu overlay for every theme.

After consumer verification, `CrystalQuickAccess` and its main/preview callers
were removed. The document-under-QAT and fade effects, comparison controls and
preview content remain in Showcase. Its message-only body rounding is retained
as a scoped `ContentCornerRadiusTop` override until the next message-bar pass.
Application-menu promotion also remains open within slice 6. Slice 10 still
requires moving important preview-only demonstrations into the main Showcase
before removing the separate preview window and launch path; neither was removed.

The Release solution build passed with zero warnings/errors. The focused run
passed 100 tests; the final runtime run passed 417 tests. Both exclude
Writer-named runtime tests and the separately deferred Office 2010 hover-contract
method. The RibbonKit-only consumer test passed. The final visual comparison
passed all 77 scenes. Its original 63 approvals and tolerances are unchanged;
14 new light/dark QAT baselines cover expanded/minimized, message combinations,
RTL, tab-row overflow and synthetic 100/200% rendering. Actual-image inspection
found that disconnected minimized fixtures still painted the body. The fixtures
now realize Ribbon's loaded lifecycle in an offscreen test window, reparent the
ribbon into a fresh capture root to avoid native-window offsets, and assert body
collapse before rendering. The corrected actual images were reviewed before
adding the new baselines. No existing scene differed in the full capture pass.
Theme XML parsed without duplicate direct resource keys. Final diff review and
`git diff --check` passed.

Writer tests and manual Showcase launches were not run. User-supplied QAT visual
acceptance, real DPI transitions and target-machine popup acceptance remain
pending; earlier Crystal dark screenshots and slice 5's automated checkpoint
do not establish these gates. Office Glass, View-tab overflow, 200% gallery
clipping, the Office 2010 hover investigation and Writer RKWF-026 stay deferred.

### 3.198 Minimized Crystal QAT corners above messages — 2026-09-30

The combined BelowRibbon/minimized/message state gained the dynamic
QatExtenderCornerRadiusMinimizedMessageBar metric. Crystal light/dark retain four
10-DIP corners; Office defaults retain their connected geometry. The independent
consumer reproduced square upper corners before the fix and checks placements,
RTL, theme switches, overflow, resource replacement and explicit local precedence.
No public C# API changed.

Recorded automated evidence: clean Release build, 100 focused checks, 417 eligible
runtime checks and the independent consumer. The runtime selections excluded
Writer names and the then-deferred Office 2010 hover-glass method. Only two
minimized-message approvals changed after actual/diff review (317/315 pixels in
light/dark); the other 75 images were identical and tolerances stayed unchanged.
The final 77-scene comparison passed.

The completed bounded live review covered both Crystal palettes:

- Four rounded minimized/message QAT corners and readable Paste/Select popups.
- Tab-row, title-bar and below-ribbon placement at 125% Windows scaling.
- A closed-popup 125% → 200% → 125% round trip, with correct alignment and Esc
  after reopening Select; the 200% maximized captures were 1920×1104 pixels.
- Overflow with nested Paste/Select, two-step Esc, reopening and item return.
- User-confirmed cleanup/restoration of the normal five commands.

Keeping a popup open across a scale change and mixed-monitor transitions were not
established by those captures. Current acceptance is maintained in the
[Crystal plan](../../13-CRYSTAL-PORTABILITY-AND-ORB-PLAN.md#current-status-and-acceptance).

### 3.199 Shared Crystal message bars — 2026-09-30

Shared message templates replaced Showcase's CrystalMessagePresentation and
preview-only body-corner override. The independent consumer first reproduced
5-DIP actions instead of the accepted 12-DIP Crystal corners.

A message-scoped ActionButton theme style uses one shared template, matching
ActionCornerRadius/ActionRecognizesAccessKey metrics and live brushes. Crystal
dark explicitly selects its action style; Office palettes retain their geometry
and disabled opacity. Crystal body corners remain 14 DIPs above messages. Host
content, commands and comparison policy stay outside the library; local/scoped
styles and paint preserve normal WPF precedence. The action template is a
message-part extension point, not a general application-button style.

The consumer covers ItemsSource, QAT placements, minimized/RTL states, independent
notices, wrapping, empty/reopen cleanup, automation invocation, disabled and
synthetic hover/press/focus states, manual dictionaries and local overrides.
Theme/style replacement can recreate chrome; assertions reacquire live parts.
Recorded validation passed the Release build, 100 focused and 417 eligible runtime
checks with the prior exclusions, the independent consumer and 85 scenes. Eight
new inspected message approvals were added; the previous 77 and tolerances stayed
unchanged.

The user completed expanded and minimized light/dark review at 125%: readable
rounded notices, independent action/close dismissal, reopening/restoration and
no leftover shadows. Those confirmations did not establish 200% or mixed-monitor
message behavior or broad motion/keyboard acceptance. Current gates belong to the
[Crystal plan](../../13-CRYSTAL-PORTABILITY-AND-ORB-PLAN.md#current-status-and-acceptance).
