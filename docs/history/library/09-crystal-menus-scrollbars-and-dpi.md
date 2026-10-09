# Library history: Crystal menus, scrollbars and DPI

> Archived implementation evidence. Dates, test counts and pending work describe
> their original checkpoints; later records may supersede them.

[History directory](../01-library.md) · [Current status](../../../04-DESIGN-NOTES.md#5-current-state--next-steps) · [Working agreement](../../../AGENTS.md).

### 3.200 Shared Crystal application menu — 2026-09-30

Shared application-menu templates and Crystal tokens replaced Showcase's
part-paint, row-geometry and shadow adapters. The independent consumer reproduced
8-DIP framing instead of Crystal's accepted 14-DIP corners before the promotion.
Rounded clips follow inset geometry and live layout/token changes.

Responsive sizing reserves 160 DIPs for navigation and a 32-DIP viewport allowance,
clamping the preferred 300-DIP pane to a 180-DIP minimum. A menu pre-templated in
Window.Resources could bypass the Ribbon ancestor lookup. Ribbon therefore
supplies its width through a private attached binding, releases the old binding
on reassignment, and keeps explicit local pane widths outside theme bounds legal.

A separate noninteractive shadow caster excludes the translucent frame interior.
Its halo follows blur/depth and nonuniform corner changes; Office keeps its prior
frame effect. Local paint/corners/effects/width/margins and scoped resource changes
retain precedence. Replace a used effect resource rather than mutating a frozen
WPF instance. Captured backdrop, content/commands and platform integration remain
host-owned; [the optional host contract](10-host-effects-and-keyboard-focus.md#3212-optional-captured-backdrops-and-scoped-glass-overlays--2026-10-01) and [Showcase consolidation](10-host-effects-and-keyboard-focus.md#3213-main-showcase-consolidation--2026-10-01)
subsequently replaced the old "later slice" instructions.

Recorded validation: clean Release solution/visual builds, 116 focused and 417
eligible runtime checks with the then-existing exclusions, and the RibbonKit-only
consumer without Showcase resources/helpers. It covered ownership preparation,
reassignment, narrow resizing, themes/QAT/minimized/message/RTL, local precedence,
command routing and outside-only rendered shadows. The 99-scene capture/comparison
passed: 14 inspected menu approvals were new, 85 existing images and tolerances
were unchanged. Synthetic invocation did not establish native input acceptance.

Bounded live review subsequently confirmed both Crystal palettes at 125%:
default, split Save As and whole-row Publish paint; native primary/pane commands;
Esc/default-page restoration; narrow default/Save As wrapping with visible footers;
and minimized ribbon with stacked notices. Retracted wide captures were excluded
from narrow evidence. Broader keyboard/motion/native DPI limits remain distinct.
The [active plan](../../13-CRYSTAL-PORTABILITY-AND-ORB-PLAN.md#current-status-and-acceptance)
owns current acceptance, including later RTL/footer work.

### 3.201 RTL lab bilingual application-menu header — 2026-09-30

The user supplied dark `codex-clipboard-4ccad49a-481d-4f3b-99dc-32c6f52f863a.png`
and light `codex-clipboard-9fb37f6e-a148-4e63-8f64-f74b1903d577.png`. Both PNGs
measure 1300×875 pixels and show the Localization/RTL lab's default menu with
Right-to-Left enabled and the embedded/application string provider. The menu
anchors at File on the right; navigation/icons remain on the right, recent
content is on the left and footer actions mirror leftward. Arabic/English
recent content is readable, and the explicitly LTR `Report-2026-Q3.docx` row
keeps its direction. The rounded frame, inner outline and footer remain intact.

The Save As row reveals a real label gap: `حفظ باسم — Save As` exceeds the
text space alongside the icon and split arrow. A focused realized-control
reproduction measured characters outside the primary hit area. This is long
host-owned bilingual content, so the lab now supplies a wrapping
`RibbonApplicationMenuItem.HeaderTemplate` for that one row. Shared navigation
width, menu geometry, colors, behavior and public APIs are unchanged by this
correction; another consumer can use the same existing extension point.

The regression measures every insertion position's character rectangle inside
the primary hit area in Crystal light/dark and LTR/RTL, including vertical fit.
It failed before wrapping and passes afterward. Release compilation of Showcase,
its runtime dependencies and the affected test project passed; both focused
`CrystalLocalizationIntegrationTests` passed. Separate `bin/RtlLabHeaderCheck/`
output preserves the user's running build. No Writer tests, full-suite rerun,
snapshot approval changes or manual Showcase launch occurred. Fresh screenshots
from the updated build are required for RTL label/pane acceptance; closing,
reopening, motion, broader keyboard and real menu DPI checks remain pending.
Slice 6 stays open. Final content/diff review and `git diff --check` passed;
no commit, push or publication occurred.

Fresh RTL Save As appearance checkpoint: the user supplied light
`codex-clipboard-fc5469ce-01ac-4d4b-80a5-e14a085c97d8.png` and dark
`codex-clipboard-0c465c35-51d2-4f44-84ae-7316f1d210d1.png` from the updated
build. Both PNGs measure 1300×875 pixels and show the Save As pane in the RTL
lab with the embedded/application provider. The full bilingual navigation
label is visible on two lines without clipping. Its left-pointing split arrow
and internal divider stay within the rounded active row. Both mixed-language
pane entries are readable on the left, and the frame, inner outline and
mirrored Exit/Options footer remain intact. This verifies the wrapping correction
and bounded RTL active-pane appearance alongside the previous default-page
review. RTL closing/reopening, return to LTR and broader motion/keyboard/real
menu DPI checks remain pending; slice 6 stays open. Documentation content/diff
review and `git diff --check` passed. No builds/tests were rerun, Showcase was
not launched by the agent, and no commit, push or publication occurred.

The user then confirmed that Esc closes File cleanly and reopening restores
Recent Documents in both themes in RTL. After closing File and turning
Right-to-Left off, reopening also returns the menu to normal LTR layout in
both themes. Together with the default/Save As screenshots, this completes
the bounded RTL appearance/close/reset review. Broader motion/keyboard and real
menu DPI acceptance remain pending; slice 6 stays open. This documentation-only
update passed content/diff review and `git diff --check`; no builds/tests were
rerun, Showcase was not launched by the agent, and no commit, push or publication
occurred.

### 3.202 Application-menu footer at limited viewport height — 2026-09-30

The user confirmed alignment and Esc closing in both themes after returning to
125%. The accompanying light `codex-clipboard-98fef5b3-93b6-42c9-bc83-47f05334b2ae.png`
and dark `codex-clipboard-7b26680c-b1f8-43e6-9cb4-08cd73232f35.png` measure
1920×1104 pixels and show Save As at 200%. Both Options/Exit footer buttons are
cut off at the window bottom. The return confirmation remains valid; these
captures do not establish a passing 200% footer or full menu DPI gate.

The ribbon's Canvas measures its overlay with infinite height. Ribbon now
publishes an internal available-height value from the menu anchor to the bottom
of the window's client-content presenter, accounting for menu/presenter margins.
The presenter matters when the content itself aligns to the top, including a
window containing only Ribbon. `PART_Frame.MaxHeight` follows that value;
navigation, default content and active-pane content use native ScrollViewers,
while the pane header and footer stay outside their scrolling areas. Layout
reflow refreshes the bound, and detaching/unloading the ribbon or replacing its
menu releases the previous window's constraint. Explicit local part limits keep
WPF precedence. There is no public API or new palette key; Office menus retain
their normal geometry when enough space is available.

A RibbonKit-only reproduction failed before the fix: in a 320-DIP-tall window,
the footer extended to 637 DIPs inside a 286-DIP client root. The regression
covers Crystal, Office 2007 and Office 2024, light/dark, LTR/RTL, short/tall/short
resizing with reopening, scroll offsets in all three content areas, anchor
reflow while open, local MaxHeight precedence and detach/reattach cleanup.
Native window resizing already dismisses File; the test preserves that behavior.

The broader runtime check also exposed captured-background measurement feedback:
when height was constrained, an old Image bitmap could enlarge the preview menu
to 751.2 DIPs in a 420-DIP-wide window. The host's `CrystalMenuBackdrop` now puts
its explicitly sized Image in a Canvas, keeping captured paint out of desired
size. The existing resize regression and blur pixel assertions pass, including
foreground exclusion, rounded clipping, scroll refresh and capture cleanup.
Capture remains host-owned slice 9 work; this does not add an integration API.

Final verification: the Release solution build passed with zero warnings/errors
for both runtime frameworks. All 418 eligible runtime tests passed, excluding
Writer names and `Every_ribbon_button_family_consumes_the_shared_hover_glass`.
The RibbonKit-only consumer passed its single STA/Application test, and the
visual test passed all 99 scenes without approval changes. Snapshot diagnostics
were inspected before correcting a stale height constraint after detachment;
the approved minimized appearance is preserved. Separate
`bin/MenuViewportCheck/Release/<framework>/` output keeps the user's running
build untouched. Documentation content/link review and `git diff --check` passed.

Fresh 200% light/dark screenshots must verify fully visible footer actions and
usable scrolling, followed by alignment/Esc checks after returning to 125%.
Broader motion/keyboard/DPI acceptance remains open; slice 6 is not complete.
No Writer code/tests, approval changes, manual Showcase launch, commit, push or
publication occurred.

Fresh footer appearance/return checkpoint: the user supplied light
`codex-clipboard-67890785-90da-40ee-98e0-4d4ff96ac9e4.png` and dark
`codex-clipboard-eb4af141-5af6-4a64-9a61-9833a1809dfa.png` from the updated
build. Both PNGs measure 1920×1104 pixels and show Save As at 200%. The frame
ends within the window; Options and Exit are fully visible in the rounded
footer. The navigation scrollbar is present; the pane heading and all four
commands/descriptions remain readable. The user confirmed alignment and Esc
closing in both themes after the requested return to 125%. This completes the
bounded footer appearance/return check. The screenshots do not establish live
scroll reachability; that check and broader motion/keyboard/DPI acceptance remain
pending, so slice 6 stays open. This documentation-only checkpoint passed
content/link review and `git diff --check`; no builds/tests were rerun, Showcase
was not launched by the agent, and no commit, push or publication occurred.

### 3.203 Application-menu scrollbar clicks preserve the open menu — 2026-09-30

During the live 200% scrolling check, the user reported that clicking a scroll
button closes File, while using the thumb does not. `RibbonApplicationMenu` listens
to unhandled `ButtonBase.Click` and requests dismissal. Native scrollbar arrows
and page regions are RepeatButtons and reach that handler; a Thumb uses drag
events. The new scrolling areas exposed this pre-existing command-click rule.

The shared handler now walks only the click source's ancestor path and returns
for a ScrollBar descendant. It neither marks the event handled nor suppresses
the routed scroll command. Split arrows/dropdown pane openers retain their
existing handled-click behavior, and ordinary commands inside ScrollViewers,
pane/footer commands and Esc retain dismissal. No public API, template, token
or host helper was added or changed for this correction.

The RibbonKit-only viewport regression now invokes the realized RepeatButton's
protected native `OnClick`, exercising both the bubbling Click and its routed
scroll command. It failed before the fix with `NavigationScroll: LineDown closed
File`. Afterward line-down, page-down and line-up change the offset while File
stays open in navigation, default and active panes, with active-pane ownership
preserved. Crystal, Office 2007 and Office 2024 run in light/dark and LTR/RTL.
The earlier direct `ScrollToBottom` check did not exercise button dismissal.

Offscreen UI Automation invocation depends on native activation/command requery
and was unavailable for these realized buttons in this STA fixture, so the test
uses reflection to call the native protected click method. This verifies routing
and command behavior, not physical pointer acceptance. Windows at the user's
current 200% setup also clamps a requested 950-DIP window to 552 DIPs; the resize
regression now requires the reopened menu to reclaim actual available space and
reduce its scroll range instead of assuming all content must fit without scrolling.

The Release solution build passed with zero warnings/errors for both runtime
frameworks, and the single RibbonKit-only consumer test passed. The eligible
runtime run passed 417 tests and failed
`Detached_rtl_lab_applies_crystal_and_restores_office_options` at its initial
Office RTL option-indicator assertion, before applying Crystal or opening File.
Separate-process reruns failed identically in both the new output and the prior
`MenuViewportCheck` output at the current 200% display setup. This existing
failure remains recorded; the current broader run is not a passing gate. Writer
names and the deferred hover-glass method remain excluded. Visual snapshots
were not rerun for this routing-only correction; the preceding 99-scene result
remains dated evidence. Documentation content/link review and `git diff --check`
passed. The updated
Showcase is in separate `bin/MenuScrollClickCheck/Release/net8.0-windows/` output.
Fresh arrow/page/drag/wheel scrolling at 200% in light/dark remains for the user;
prior footer appearance/125% return acceptance remains recorded. Broader
motion/keyboard/DPI checks remain pending, and slice 6 stays open. No Writer
code/tests, manual Showcase launch, commit, push or publication occurred.

The user subsequently confirmed that scrollbar buttons now scroll without
closing File in both themes. This closes the bounded live arrow-click regression
check. It does not add page-region, thumb-drag or wheel acceptance, and broader
keyboard/motion/DPI checks and the separate Office RTL test failure remain open.
This follow-up changed documentation only; content/link review and
`git diff --check` passed, with no builds/tests rerun or Showcase launch.

The user also reported different gallery-item layout at 200% and requested that
it and the existing RTL option-indicator failure be deferred for later
investigation. The gallery report's exact surface/geometry and both causes remain
unconfirmed; it is not assumed to be the earlier View-tab selection-clipping
issue. The [deferred layout list](../../13-CRYSTAL-PORTABILITY-AND-ORB-PLAN.md#deferred-layout-observations-at-200)
records the observations and later comparison scope. This note changes
documentation only and does not change runtime behavior, test results or the
accepted File-menu checks.

The user then supplied `20260930-1141-49.7011675.mp4` and confirmed that keyboard
Tab navigation works and Esc closes File after tabbing. This is user-reported
acceptance of that sequence; separate Shift+Tab, footer reachability,
default-page restoration and recording theme/scale were not stated. Opening
motion in light/dark is the next bounded live review. Remaining scrolling,
keyboard/DPI gates and the deferred 200% layout observations retain their
scope. This documentation-only update passed content/link review and
`git diff --check`; no builds/tests were rerun or Showcase launched.

The user subsequently confirmed the requested repeated File open/Esc/quick-
reopen motion check passed in both themes. This closes the bounded normal
opening-motion review. The [remaining live review](../../13-CRYSTAL-PORTABILITY-AND-ORB-PLAN.md#remaining-slice-6-live-review)
lists the other scrolling modes and remaining keyboard details, separately
from reduced-motion/open-menu-scaling/mixed-monitor gaps. The two 200% layout
observations remain deferred. This documentation-only checkpoint passed
content/link review and `git diff --check`; no builds/tests were rerun or
Showcase launched.

The user confirmed both remaining short checks passed: thumb/wheel/track
scrolling keeps File open with its footer visible, and reverse keyboard
traversal retains visible focus, reaches Options/Exit and cycles inside File.
This completes the bounded slice 6 QAT/message-bar/application-menu live review.
Slice 7 utility chrome and scrollbar promotion is next. Reduced motion,
open-menu scaling/mixed-monitor transitions and the two deferred 200% layout
observations remain open; no blanket final acceptance or passing broader
runtime gate is inferred. This documentation-only checkpoint passed content/
link review and `git diff --check`; no builds/tests were rerun or Showcase
launched by the agent.

### 3.204 Shared Crystal utility buttons and scrollbars — 2026-09-30

Slice 7 starts from a clean `codex/crystal-theme` checkout at `07bc890`. Shared
utility templates now own the accepted non-interactive rims for minimize, modal
close, QAT overflow, ribbon/tab scroll arrows and merged-caption buttons. The
rim is a sibling of Chrome and binds its bounds/corners without changing content
padding. Hover/checked rims use the control hover border; pressed paint wins.
Crystal's idle/hover arrow surfaces remain swapped as in the former helper;
Office keeps its original surfaces and zero active rim opacity. Default tab
arrow paint is now a shared style setter so explicit local button backgrounds
retain WPF precedence. Scoped source brushes update directly through a small
internal `UtilityChrome` material selector; no public API was added.

Crystal tokens opt native ScrollViewer scrollbars into the existing shared
vertical/horizontal templates, including Options and both customization pages.
Thickness is 14 DIPs, corners are 4 DIPs, and the thumb retains its one-DIP rim
and face plus 6/8/11-percent wash (15/20/28 alpha steps). Native tracks, routed
line/page commands, thumb dragging, focus and RTL remain in WPF. The designer-safe
keyed customization scrollbar adapter is retained. An attempted cross-file
BasedOn simplification failed the existing scope test and was reverted.

The consumer regression exposed a Freezable resource-scope trap: nested brushes
in a shared resource drawing could retain the first window's surface/wash, even
with a dialog-local token override; unshared XAML drawings also did not provide
reliable scope resolution. `ScrollBarMaterialConverter` composes only marked
Crystal tokens from brushes resolved on the realized Pill. Office and explicit
host thumb backgrounds pass through unchanged. Live resource updates, manually
merged light/dark dictionaries, a simultaneous Options window with a different
wash, local styles/paint/corners, both axes and native line/page routing pass in
the RibbonKit-only consumer. Its built-in customization list/tree and app-owned
Options content bars use the same template/material without Showcase references.

After that proof, `CrystalUtilityChrome`, `CrystalScrollBars` and
`CrystalCustomization` and their callers were removed. Generic paint and geometry
are now library-owned. The existing main-window raw tint and dialog/preview
readable-accent wash remain host policy through `ScrollBar.WashAccent`;
`CrystalPalette` avoids tinting the composite thumb twice and retains neutral
glyph paint. Contextual/tint policy remains slice 8, capture remains slice 9,
and the separate preview window/launch path remains until slice 10.

Final verification: the Release solution build passed with zero warnings/errors
for both runtime targets, all 418 eligible runtime tests passed using
`FullyQualifiedName!~Writer&FullyQualifiedName!~Every_ribbon_button_family_consumes_the_shared_hover_glass`,
and the single STA/Application RibbonKit-only consumer passed. All 105 visual
scenes passed; the existing 99 approvals/tolerances are unchanged. Six new
light/dark utility/scrollbar scenes at synthetic 100/200% and RTL were inspected
before adding approvals. Image inspection first caught scrollbar samples hidden
behind a modal body and intersecting standalone arrow corners; those fixture
errors were corrected before approval. Synthetic snapshots are not live pointer,
focus, monitor-transition or target-machine acceptance.

The initial solution build incorrectly used one OutputPath for both runtime
frameworks and collided net8/net9 assemblies. The corrected isolated output is
`bin/CrystalSlice7/Release/<framework>/` per project. The user launch build is
`samples/RibbonKit.Showcase/bin/CrystalSlice7/Release/net8.0-windows/RibbonKit.Showcase.exe`;
the existing MenuScrollClickCheck output remains untouched. Documentation/link
review and `git diff --check` passed. No Writer code/tests, public API baseline,
manual Showcase launch, commit, push or publication changed.

User light/dark review remains pending for utility hover/press/checked paint,
scrollbar arrows/track/thumb/wheel, Options/customization focus and RTL, merged
caption commands, and real DPI return. Reduced motion, open-menu scaling and
mixed-monitor transitions remain broader gaps. The earlier Office RTL checkbox
failure at the user's 200% setup passed in this automated run, but no fix or cause
was established; it and the distinct 200% gallery-layout report remain deferred.
The accepted slice 6 menu sizing, footer, capture-measurement and click-routing
contracts and bounded live reviews remain in force.

### 3.205 Customization scrollbar insets and slice 7 live review — 2026-09-30

The user accepted slice 7 review checks 1 and 2: utility/arrows and modal/merged
caption buttons. Check 3 paused at a screenshot-reported spacing mismatch.
Crystal's tree scrollbar had the desired small inset while list scrollbars were
flush; Office trees had excessive clearance relative to lists.

The shared customization frame previously combined the FrameInset token with
an extra literal TreeView Padding of 2. A style default replaces it with zero,
also neutralizing WPF's native 1-DIP TreeView padding. List padding is unchanged.
FrameInset supplies 2 DIPs in Crystal light/dark and the existing 1 DIP in every
Office variant. Crystal tree
clearance and Office list clearance are retained; the other controls match them.
The existing template binding still adds local or host-style padding, and scoped
FrameInset overrides remain dynamic. No runtime or public API was added.

The RibbonKit-only consumer now measures realized viewport and visible native
scrollbar bounds for both page types, all Office variants, Crystal light/dark,
manually scoped palettes and both directions. It also checks local padding plus
a scoped inset, host-style padding and restoration. Native scrollbar size
rounding at fractional DPI is allowed within half a device pixel; viewport
clearance stays exact. Final review removed redundant literal list padding and
neutralized native tree padding at style precedence, retaining host overrides.

The isolated Release solution build passed with zero warnings/errors, the
RibbonKit-only consumer passed, all 418 eligible runtime tests passed with the
same Writer/hover-method exclusions, and all 105 visual scenes passed against
unchanged approvals/tolerances. The solution and consumer checks use
`bin/CrystalSlice7Spacing/Release/<framework>/`; the new user executable is
`samples/RibbonKit.Showcase/bin/CrystalSlice7Spacing/Release/net8.0-windows/RibbonKit.Showcase.exe`.
The prior CrystalSlice7 and MenuScrollClickCheck outputs are preserved. The
initial geometry assertion needed corrections for native device-pixel rounding
and RTL ancestor coordinates; neither required another product change.
One consumer run stopped before customization at `NavigationScroll: LineDown
closed File`. A fresh-process rerun passed, including the final geometry and
host-style checks. Menu routing was unchanged; that one-off failure's cause is
unconfirmed and no menu fix is claimed.
Documentation links and `git diff --check` passed. No Writer work, Showcase
launch, commit, push or publication occurred.

Check 3 live scrolling, focus, RTL and real DPI return remains pending. The
separate Office RTL checkbox failure and 200% gallery-layout observation remain
deferred with unconfirmed causes. Checks 1–2 need not be repeated for this fix.

### 3.206 Customization scrollbar pixel gaps — 2026-10-01

The user's follow-up screenshot found unequal top/bottom scrollbar gaps in every
theme and excessive Crystal list-side clearance relative to the tree. The earlier
half-device-pixel geometry allowance had not established equal painted gaps.
The user accepted the latest scrollbar arrow/track/wheel/thumb, focus and RTL
check (follow-up number 2); spacing and 125% → 200% → 125% return will be repeated.
Original slice 7 utility/modal checks 1–2 remain accepted.

An offscreen RibbonKit-only probe reproduced fractional frame bounds, independently
rounded scrollbar chrome and column-dependent painted side gaps. Rounding the
page alone still left an odd inset pixel: WPF Border.Padding can subtract a
rounded combined width/height differently from an individual edge. The shared
frame now uses the existing FrameInset token as a transparent BorderThickness,
which rounds each edge independently. Both customization page styles default
UseLayoutRounding to true, while local values and host styles keep WPF precedence.
At 125%, Crystal's outer-edge gap is three pixels including its frame border;
Office's is two. Top, bottom and scrollbar-side gaps match between both controls.
The configured Crystal 2-DIP and Office 1-DIP inset tokens are unchanged. Native
presenters, scrollbars and commands remain; no runtime or public API was added.

The new RibbonKit-only regression checks actual scrollbar chrome bounds in both
customization pages for 96 theme/light-dark/scale/direction combinations at
100/125/150/200%, plus native scrolling. It refreshes DPI on the fully realized
visual tree and asserts native part DPI rather than testing only raster scale.
Default painted gaps are checked exactly, with no fractional-pixel allowance.
The existing local-padding/scoped-inset and host-style fixtures use a four-DIP
padding to distinguish host padding from the independently rounded inset border.

Final validation: Release solution compilation passed with zero warnings/errors
for both runtime targets, the RibbonKit-only consumer passed, all 418 eligible
runtime tests passed with the existing Writer/hover-method exclusions, and all
105 visual scenes passed against unchanged approvals/tolerances. Crystal
light/dark and Office 2024 renders at 125/200%, including RTL, were inspected;
before/after probe renders also confirmed the pixel-gap correction. The first
consumer run passed the new 96-variant gap matrix, then failed an old combined
inset expectation for host padding. The fixture/calculation was corrected to
round border, host padding and inset independently; no default-gap tolerance
was added. The full consumer rerun passed. Documentation/link review and
`git diff --check` passed. No manual Showcase launch, commit, push or publication
occurred. User spacing and real DPI-return acceptance remain pending.
The output is isolated under `bin/CrystalSlice7Pixels/Release/<framework>/`;
earlier CrystalSlice7, CrystalSlice7Spacing and MenuScrollClickCheck builds are
preserved. Writer and the deferred Office RTL checkbox/200% gallery issues remain
untouched. Broader reduced-motion, open-menu scaling and mixed-monitor review
remain open.

### 3.207 Slice 7 bounded live review complete — 2026-10-01

The user confirmed follow-up checks 1 (corrected customization scrollbar spacing)
and 3 (125% → 200% → 125% return) passed for both reviewed themes using the
CrystalSlice7Pixels build. Check 2 (native scrollbar arrow/track/wheel/thumb,
focus and RTL) was accepted earlier. Original utility/arrows and modal/merged
caption checks 1–2 also remain accepted. This completes the bounded slice 7 live
review, alongside its recorded Release, runtime, consumer and visual evidence.

This is a documentation-only acceptance update. Content/link review and
`git diff --check` passed; no build or test was repeated. Existing edits, test
coverage, approvals and build outputs are preserved. No Showcase launch,
commit, push or publication occurred.

The prior Office RTL checkbox failure at 200% and the distinct 200% gallery-layout
report remain deferred, with unconfirmed causes. Reduced motion, scaling while
an application menu stays open and mixed-monitor transitions remain broader
gates. Slices 8–10 and Writer RKWF-026 remain deferred to their own scopes;
this acceptance does not claim blanket Crystal completion.

### 3.208 Shared Crystal contextual material and scoped palettes — 2026-10-01

Slice 8 promotes the accepted contextual glass into the ordinary shared RibbonTab
template. An internal ContextualMaterial selector/converter derives selected and
hover surfaces, rims, readable text and reflective marker from the tab's solid
contextual color. Dynamic token inputs follow local palette replacement and the
scoped accent fallback. Derived paint does not write tab resources or public
ContextualSelectionBrush values. Explicit foreground/marker values and unmarked
scoped paint retain precedence. Gradient/custom brushes retain the ordinary
contextual renderer; qualified color dependency-property paths avoid missing-Color
binding errors. Matching keys exist in all twelve palettes, with Office opt-out
defaults and Crystal's accepted 0.85 unselected opacity.

The additive public API is ThemeManager.CreatePalette(RibbonTheme theme,
Color? accent = null, bool dark = false), with XML documentation and an Unshipped
baseline entry; the Shipped baseline is unchanged. Each call creates a fresh
scoped dictionary without changing global theme/preferences or raising Changed.
Office palettes reuse the existing accent derivation; SetAccent remains unchanged.
Crystal uses the promoted hue rotation/readability policy, preserving alpha,
glass highlight geometry, neutral text/glyphs, semantic message colors and the
6/8/11-percent composed scrollbar washes. Replacing/removing the owner's dictionary
updates/clears that scope without accumulating hue rounding or recoloring another
window. The generic name and Office support follow the user's naming correction.

CrystalContextualTab and its main, preview and MDI usages were removed after the
library-only consumer passed. Showcase's CrystalPalette delegates reusable control
tint to the new factory; only application document paint remains there. The main
window's raw scrollbar-wash choice remains an explicit token override. Capture,
popup/menu blur and document effects remain host-owned for slice 9. No broad
coordinator was added; the separate preview and launch path remain for slice 10.

Final evidence: the isolated Release solution build passed for both runtime targets
with zero warnings/errors; all 418 eligible runtime tests passed with the existing
Writer/deferred-hover exclusions; the single STA RibbonKit-only consumer passed.
Its existing 96-variant customization gap coverage is preserved. New consumer
checks cover mutable contextual colors, marker/foreground and scoped brush
overrides, gradient fallback without binding errors, live accent fallback, RTL,
simultaneous light/dark windows, replacement/removal isolation, no global Changed
event and accent parity for all ten Office light/dark combinations.

All 113 visual scenes pass. The existing 105 approvals and tolerances are unchanged;
eight new light/dark contextual 100/200/RTL and purple-tint scenes were captured and
inspected before adding approvals. Their fixture stays in an offscreen test window
through rendering, updates DPI on the actual visual root and asserts a visible
reflective marker. Explicit minimum widths keep the diagnostic labels readable;
this changes the fixture only. Earlier disconnected captures omitted the marker
and were not approved. Four initial runtime failures were obsolete helper-storage,
palette-layer or literal-XAML assertions; they were updated to check the shared
replacement and the full eligible rerun passed.

Logs are slice8-consumer-final.trx, slice8-runtime-complete.trx and
slice8-visual-final.trx under their test projects' TestResults directories. Build
outputs are isolated under bin/CrystalSlice8/Release/<framework>/; previous user
builds are preserved. Final content/link/diff review and git diff --check passed.
No Writer code/tests, manual Showcase launch, commit, push or publication occurred.
Live acceptance remains in the active plan. The deferred Office RTL checkbox/200%
gallery reports, broader motion/open-menu scaling/mixed-monitor gates and Writer
RKWF-026 retain their separate scopes and unconfirmed causes.

### 3.209 Popup border and shadow DPI diagnostics — 2026-10-01

An opt-in Showcase-only `--popup-dpi-trace` observer samples dropdown and inherited
split-button popup native DPI/HWND bounds, painted border bounds, pixel shadow
headroom, rounding/clip flags and shadow parameters at opening, after layout and
500 ms later. It logs window DPI changes and closing too. Normal startup does not
enable it; no runtime/public API, template, paint, placement or dismissal changed.
The isolated `bin/PopupDpiTrace/Release/net8.0-windows/` build has a click-to-run
`Start Popup DPI Trace.cmd` launcher and writes its log beside the executable.
This is temporary diagnosis, not a rendering fix.

The Release Showcase build passed with zero warnings/errors. All 19 focused popup
motion, dismissal and new geometry/logging checks passed. The new two probes use
ordinary shared controls with Office 2024/Crystal tokens and repeated synthetic
owner-DPI changes; native popup DPI remains the actual monitor's 125% in this
process. Measured bottom shadow headroom is 10 pixels as expected for 8 DIPs.
They did not reproduce real Display Settings timing or establish the cause of the
intermittent bottom-border/shadow report. Content/commands were reported intact;
rounding remains unconfirmed. No snapshot approvals changed, full suite was not
rerun and the agent did not launch Showcase. Current acceptance/report status is
maintained only in the active plan.

### 3.210 Popup margins follow the DPI pixel grid — 2026-10-01

The user's 200% → 150% → 125% native trace shows correct HWND/paint DPI and
16/12/10-pixel bottom shadow headroom. At 125%, the popup border acquires a
full-face layout clip (170.4 × 192 DIPs); the 200%/150% openings have no clip.
This identifies paint clipping despite sufficient native window space. WPF rounds
margins for measure/arrange while its layout-clip calculation uses stored margins.
The 2-DIP top margin lands between pixels at 125%.

An internal PopupBorder now coerces each outer margin edge to the actual visual
DPI grid when UseLayoutRounding is enabled, and recoerces after DPI/rounding
changes. Both shared dropdown and split-button PART_MenuHost templates use it.
Requested local values remain intact; opting out restores them. The explicit
implicit-Border style lookup preserves consumer styles. Native layout clipping,
placement, effects, content presenters, commands and dismissal remain WPF-owned;
there is no public API addition or broad popup coordinator.

Five runtime checks cover repeated DPI changes, requested values/rounding opt-out,
genuine clipping, shared fractional layout and rendered shadow pixels, plus
reopened native headroom after synthetic owner-DPI changes. Both rendered 125%
fixture PNGs were inspected. The RibbonKit-only consumer adds 16 dropdown/split
openings across Office 2024/Crystal, light/dark and LTR/RTL, including scoped brush
and implicit style overrides. These checks do not reproduce native Display
Settings timing; the exact reported sequence remains a live recheck in the plan.

The isolated Release solution build passed with zero warnings/errors. All 423
eligible runtime tests passed with the existing Writer/deferred-hover filter;
the complete RibbonKit-only consumer passed in 112 seconds; all 113 visual scenes
passed without changing approvals or tolerances. The consumer's bounded STA
deadline increased from 90 to 180 seconds after a fresh run exceeded 90 seconds;
its new popup checks took about one second and existing coverage was retained.
The first interrupted run included the user's computer sleep and is not counted
as product evidence. Temporary progress-file writes were removed before the pass.

Logs are popup-dpi-edge-runtime.trx, popup-dpi-edge-consumer-final.trx and
popup-dpi-edge-visual.trx under the respective test projects' TestResults folders.
Outputs use bin/PopupDpiEdgeFix/Release/<framework>/, preserving previous builds.
The Showcase output includes Start Popup DPI Edge Check.cmd and retains the
opt-in observer for the pending live recheck. No Writer code/tests, manual
Showcase launch, commit, push or publication occurred. Existing broader/deferred
acceptance gaps retain their scope.

### 3.211 Vertical split hover width and diagnostic cleanup — 2026-10-01

Vertical Paste's primary half reserved a transparent 1-DIP border in flat Office
themes while the arrow half reserved none. Six rendered light/dark checks for
Office 2013/2019/2024 reproduced a 96-pixel primary wash versus a 98-pixel arrow
wash at 125%. Their existing SplitVerticalPrimaryBorderThickness defaults now
match the flat themes' zero-width highlight border. Office 2007/2010 and Crystal
retain their existing rim metrics; scoped metric overrides remain effective.
Before/after fixture PNGs were inspected. No new token or public API was added.

The RibbonKit-only consumer now covers 32 popup openings across four themes,
light/dark and LTR/RTL, equal vertical-half widths/side borders and scoped metric
replacement/removal. The temporary Showcase DPI observer/startup switch and its
logging-only test assertions were removed; native headroom, layout clipping,
requested-margin and rendered-shadow checks remain.

The final isolated Release solution build passed with zero warnings/errors;
all 429 eligible runtime tests passed with the existing exclusions; the full
consumer passed on a fresh process; all 113 visual scenes passed. Approvals and
tolerances are unchanged. An initial consumer run failed an existing customization
focus assertion; the unchanged fresh-process retry passed and its cause remains
unconfirmed. No focus behavior or assertion was changed. Two old Office border
expectations were updated for the corrected default before the full runtime pass.

Outputs are bin/PopupDpiHoverFix/Release/<framework>/; Start Hover Review.cmd starts
the normal Showcase executable. Evidence logs are popup-hover-runtime-complete.trx,
popup-hover-consumer-retry.trx and popup-hover-visual.trx under the test projects'
TestResults folders. No Writer changes, app launch, commit, push or publication
occurred. Live results and remaining gates stay in the active plan.
