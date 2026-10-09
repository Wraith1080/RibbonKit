# Library history: Host effects and keyboard focus

> Archived implementation evidence. Dates, test counts and pending work describe
> their original checkpoints; later records may supersede them.

[History directory](../01-library.md) · [Current status](../../../04-DESIGN-NOTES.md#5-current-state--next-steps) · [Working agreement](../../../AGENTS.md).

### 3.212 Optional captured backdrops and scoped glass overlays — 2026-10-01

Slice 9's bounded audit separates reusable capture/paint from host content,
registration policy and native window integration. The additive public
`CapturedBackdrop(Control control, FrameworkElement source)` registers one
application menu, dropdown/split button or ribbon group. `Apply`, `Refresh` and
`Dispose` provide explicit opt-in and cleanup. Tokens alone cannot supply a host
capture visual or own event subscriptions; a per-control disposable registration
provides that seam without importing the main-window coordinator. XML comments
and Unshipped entries document the addition; the Shipped baseline is unchanged.

The shared engines retain the 6-DIP Gaussian blur, 24-DIP sampling margin,
stationary application-menu anchor, opacity-based foreground exclusion, Canvas
measurement exclusion and post-blur clip. The clip reuses shared rounded geometry,
including changed/nonuniform corner overrides. Popup capture shares the snapshot
primitive, retains opaque fallback outside the source, and uses the existing scoped
application-menu frame-band token for the accepted tint. Local backgrounds,
bindings and host SetCurrentValue overrides keep precedence. Closing/disable clears
only the integration's own current paint, restoring template resource evaluation.
Reopening, template replacement, scroll/resize and observed source/popup DPI changes
refresh capture. Unload releases engines; reload reattaches while enabled. Disposal
aborts queued work and removes rendering, theme, layout and source handlers.

`ThemeManager.CreateGlassOverlay(FrameworkElement scope, bool dark = false)`
provides the existing cross-theme brush treatment with the accepted opacity and
accent-wash values. It clones effective resources without changing global theme or
activating Acrylic. Owners remove the preceding overlay before regeneration after
theme/accent/palette changes, merge the new dictionary and remove it on disable.
Direct scoped/child overrides retain WPF precedence. The host owns capture source,
content paint and explicit Refresh timing, dynamic-control registration/disposal,
preferences and Windows backdrop activation. Core Crystal requires neither opt-in.

Showcase's CrystalMenuBackdrop, CrystalApplicationMenuBackdrop and
CrystalPopupBackdrop were retired after replacement checks. Main and separate
preview callers use the shared registration and dispose on window close.
AcrylicGlassPresentation retains only dictionary ownership; paint creation is now
shared. CrystalMainWindowPresentation retains app palette scope and control
discovery. Document effects/content and the preview window/launch path remain in
the host; slice 10 and Writer RKWF-026 remain separate.

Verification: the normal Release solution build passed for both runtime targets
with zero warnings/errors. All 431 eligible runtime tests passed with the retained
Writer/deferred-hover filter. The visual suite passed all 113 scenes with unchanged
approvals/tolerances. The final Release RibbonKit-only consumer passed in 88 seconds
without Showcase references/resources/helpers. It retains all prior checks and adds
light/dark/RTL popup/menu paint, enable/disable, scoped palette replacement/removal,
reopening, scroll/resize, nonmeasuring/input/focus exclusions, collapsed content,
bindings/current-value overrides, unload/reload/disposal and twelve theme/dark glass
variants. Runtime coverage additionally retains rendered blur/foreground/shadow
checks and covers template replacement, opacity binding, corner changes, queued
cancellation and synthetic root-DPI capture. DPI metadata is checked against the
observed source DPI; these tests do not reproduce native Display Settings timing.

Earlier consumer attempts intermittently closed File in existing viewport/menu
assertions. A diagnostic run passed; the final run preserves the original baseline
case order and appends the optional effects checks after it. Temporary tracing was
removed and no existing assertion was relaxed. The final uninstrumented pass does
not establish a cause or a product fix for those earlier closures. The filtered
solution attempt passed runtime/visual projects but hit this consumer failure;
the final consumer pass is a separate process, not an unfiltered full-suite pass.
Evidence is slice9-release.trx in runtime/visual TestResults and
slice9-consumer-final.trx in the consumer TestResults directory.

The normal Debug Showcase build succeeded with zero errors and twelve designer-copy
warnings: Visual Studio (PID 27316) holds the Debug RibbonKit.DesignTools.dll open.
The runtime and Showcase outputs were updated in their default folders; running
apps were preserved. Local review uses
samples/RibbonKit.Showcase/bin/Debug/net8.0-windows/RibbonKit.Showcase.exe.
No manual Showcase launch, Writer code/test edit, commit, push or publication
occurred. The active plan owns the pending slice 9 live review; broader motion,
open-menu scaling/mixed-monitor and the two deferred 200% observations retain their
separate scopes. Content/link/final diff review and git diff --check passed.

### 3.213 Main Showcase consolidation — 2026-10-01

Crystal portability slice 10 moves the useful material-study demonstrations into
the existing main Showcase. No runtime code, public API, Writer code/tests or
snapshot approvals/tolerances changed. The separate `CrystalPreviewWindow` XAML,
code-behind, View launch button and `--crystal` startup branch are retired.
The preview-only `CrystalBackstagePresentation` and `CrystalScreenTipPalette`
helpers are also removed; their resource-scope and binding regression checks now
use direct shared resources and ordinary WPF bindings.

| Former preview demonstration | Main Showcase surface |
| --- | --- |
| Crystal / Office 2024 comparison | Samples: Compare 2024; View retains all six themes, dark mode and accent gallery. |
| Tab arrows, body arrows and QAT overflow | Samples: Try layouts adds/removes temporary navigation tabs, saves/restores each Home group's CanResize value, and saves/restores QAT items, width and placement. |
| Checkbox, mixed state, radio grouping and disabled gallery | Samples: Options, Spacing and States; explicit source bindings survive group reparenting. Home keeps its existing disabled button/split/input-group demonstration. |
| Document title, scrolling content, edge fade and QAT underlay | Samples: Document title and Document effects operate on the existing editable RichTextBox. The title is explicitly bound into document and Backstage content. Only the added scrolling paragraphs are removed, preserving unrelated document edits. |
| Context tint change | Samples: Change context tint selects Picture Format and changes its ordinary ContextualColor; existing Picture and merged Chart Tools retain their shared material. |
| Styles, inputs, messages and customization | Existing Home and File flows; Samples adds gallery enable/disable and message add/dismiss controls. The same three-page Options dialog is reused by migrated tests. |
| File menu, Backstage sidebar/floating designs, tint and native effects | Existing View/File controls and scoped palette integration; nested host dropdowns now receive CapturedBackdrop registrations through logical-child discovery. |

`MainWindow.Samples.cs` contains the host demonstrations. Temporary QAT changes
do not write customization while active; restoring overflow persists the real
QAT. Opening Options first removes temporary tabs/overflow and restores Home
resizing. Unidentified navigation demo tabs and commands are omitted by the
existing customization serializer. No persistence format or library coordinator
was added. Document fade and underlay start disabled, preserving the ordinary
document layout until explicitly selected in Crystal. `CrystalDocumentEdgeFade`
now locates the RichTextBox's native ScrollViewer, tracks template replacement,
retains the document card's original margins and keeps the scrollbar below the
overlapping QAT. Theme changes, QAT placement and notices restore or reapply the
requested host effect. These are application document effects, not library
control paint.

The six former preview-window regression tests now construct the actual main
window offscreen, skip its user-preference load, suppress persistence, set a
known factory ribbon and reset application resources on cleanup. They cover
customization frames/bindings, editable document scrolling and retained edits,
fade/underlay restoration, direct/nested dropdown and collapsed-group captures,
messages, tab/body arrows, QAT overflow and reparented option groups. Main-window
checks passed before the preview files were removed. Tests reacquire the File
frame after main-window theme switching because its File-surface selection can
detach/retemplate that control; a stale part is not current paint evidence.

Final automation used normal output folders: Release Showcase/test compilation
passed, followed by all **431 eligible runtime tests** with the existing
`FullyQualifiedName!~Writer&FullyQualifiedName!~Every_ribbon_button_family_consumes_the_shared_hover_glass`
filter (`slice10-release.trx`). The separate RibbonKit-only consumer passed its
single aggregate test in 1m49s (`slice10-consumer.trx`) without Showcase resources
or helpers. The normal Debug Showcase build passed with **0 warnings / 0 errors**
and updated `samples/RibbonKit.Showcase/bin/Debug/net8.0-windows/RibbonKit.Showcase.exe`.
Visual snapshots were not rerun for this consumer-only consolidation; approvals
and tolerances are unchanged. No manual app launch, commit, push or publication
occurred. The active plan owns the pending main-window live review; broader
motion, open-menu scaling, mixed-monitor and deferred 200% observations remain
separate gates. Changed content/links and final diff were reviewed; git diff
--check passed.

### 3.214 Theme gallery clipping and deferred test corrections — 2026-10-01

The user authorized investigating the two deferred 200% observations and then
the excluded Office 2010 hover-glass contract. They clarified that the gallery
report concerned only the View-tab Theme gallery: its selected tile was clipped
at the edges. Other galleries did not exhibit that difference.

The actual six Showcase Theme tiles used 42-DIP-high content. Shared item padding,
border and margins made each tile 52 DIP tall, exceeding the strip viewport of
50.4 DIP at simulated 125% and 50 DIP at simulated 200%. The text-only Home Styles
tile measured 50 DIP and fit both viewports. The fix reduces only the six host
content panels to 40 DIP; shared gallery templates, tokens and selection paint
are unchanged. `ThemeGalleryLayoutTests` loads the actual Showcase markup,
detaches the Theme and Styles galleries into an offscreen fixture, suppresses
appearance persistence and disables animation for settled geometry. It checks
all six theme selections after popup open/close: tile and selected-border bounds,
icon/text spacing, retained selection and the Styles comparison. Effective tile
DPI is asserted after returning from the popup. Corrected Theme tiles measure
49.6 DIP at 125% and 50 DIP at 200%, fitting their respective viewports. Before
and after PNGs in `TestResults/theme-gallery-diagnostics` were inspected; no
snapshot approval or tolerance changed.

`Detached_rtl_lab_applies_crystal_and_restores_office_options` previously measured
hidden, zero-sized checkbox/radio content when a constrained window collapsed
the Inputs group. At a requested width of 720 DIP, the original assertion failed
at both simulated 125% and 200%: indicator and header screen coordinates were
identical and their widths were zero. The test now opens the group's native
flyout when collapsed, requires nonzero visible parts, and preserves the physical
LTR/RTL position assertion. Its matrix uses requested widths of 1800/720 DIP and
scales of 125%/200%, retaining Office, Crystal light/dark, File and customization
coverage. The simulated scale is also applied to a flyout's separate visual root
and asserted on the option. No runtime option-placement change was required.

The unchanged Office 2010 consumer-count test reproduced its dropdown failure:
three references to `Control.HoverBackground` rather than the required five.
The two split halves correctly use `Control.SplitActiveHover`. That token has
the same Office 2010 gradient, including the bottom inner glow. The test now
counts both semantic hover consumers without lowering any family threshold.
Additional checks guard the split token's bottom glow and compare resolved
ordinary/split gradients in Office 2010 light/dark using only library resources.
The former hover exclusion is removed from the current verification command;
historical filtered results remain unchanged.

Release test compilation and the 33-case focused run passed
(`deferred-issues-focused.trx`); the final strengthened RTL class passed all five
cases (`rtl-root-dpi-after.trx`). The final Release runtime run passed **443 tests,
0 failed / 0 skipped** with only `FullyQualifiedName!~Writer`
(`deferred-issues-runtime-final.trx`). This includes all four hover-contract
family cases. The normal Debug Showcase build passed with **0 warnings / 0 errors**
and updated its default review executable. Visual snapshots, the separate
portability consumer and the full solution suite were not rerun for this host
content/test-only change. No runtime/public API or Writer code/tests changed.

Synthetic visual scaling uses `VisualTreeHelper.SetRootDpi` and constrained
fixture widths; Windows display settings were not changed. Popup HWND DPI,
native Display Settings timing and mixed-monitor transitions are not simulated
by those visual-root checks. Native 200% Theme gallery visual acceptance remains
with the user in the active plan, as do the separate broader motion/open-menu
gates. No manual Showcase launch, commit, push or publication occurred.

### 3.215 Gallery selected-row retention after tab reload — 2026-10-01

The user's recording shows the View Theme gallery displaying the selected Office
2024 tile through several tab visits, then showing its first Crystal tile after
Home → Ribbon Lab → View. A RibbonKit-only reproduction establishes that the
selection survives while the native strip scroll offset resets to zero when the
tab content reloads. Four Office 2024/Crystal light/dark cases failed before the
fix at simulated 125%/200%; the selected fifth tile lay roughly 200 DIP below
the 50-DIP viewport although `SelectedItem` still referenced that tile.

`InRibbonGallery` now queues its existing generation-guarded viewport refresh
when a closed gallery with a selection loads, and when its template is rebuilt
while loaded. The settled strip reveals the selected row without animation.
The restoration target adds the current vertical offset to the tile's viewport
position, keeping the absolute row calculation correct. No selection mutation,
new public API, Showcase patch or theme/template paint change was introduced.
The separate permanent strip/popup scrollers remain intact, and opening the
popup still resets its own page to zero.

`InRibbonGallerySelectionTests` exercises the recorded pattern with ordinary
RibbonKit tabs and two galleries, then replaces the template without unloading
the gallery. It checks the same selected item, full selected-border visibility
and no extra selection events. Manual browsing away from the selected row
survives unrelated resize/layout while the gallery stays loaded. The focused
run passed 14 cases: four new retention cases, two Theme tile clipping cases,
and eight existing shared gallery/DPI reproductions from
`WriterConsumerFrictionTests` (`gallery-row-focused.trx`). No Writer application
code or tests were edited.

`GallerySelectionPortabilityChecks` runs at the end of the existing separate
consumer aggregate. It uses only RibbonKit resources, `ItemsSource`, a host data
template and a two-way bound selected index. All six themes, light/dark, LTR/RTL
and simulated 125%/200% check restored selected bounds after tab reload and popup
open/close, retained binding/source value and the popup's independent viewport.

The normal Release solution build passed with **0 warnings / 0 errors** for both
runtime targets. The serialized solution test run used only the retained
`FullyQualifiedName!~Writer` exclusion: **447 runtime tests**, the visual snapshot
aggregate with unchanged approvals/tolerances, and the expanded RibbonKit-only
consumer aggregate all passed (`gallery-selection-release.trx` in each project's
TestResults). The consumer completed in 1m25s. Writer tests were excluded, so this
is a filtered solution pass rather than an unfiltered full-suite claim. The
normal Debug Showcase build also passed with **0 warnings / 0 errors** and updated
its default review executable.

The supplied recording was inspected through extracted frames. No manual
Showcase launch, commit, push or publication occurred. Live confirmation of its
tab-switch sequence remains in the active plan; synthetic visual DPI coverage
does not close native popup/monitor-transition or broader motion acceptance.

### 3.217 Crystal pre-merge scope and resource cleanup — 2026-10-04

The project-wide audit reproduced a QAT policy leak: an independent Crystal
window palette inherited the global accented Office 2019 icon treatment because
`Ribbon.UpdateQatButtonContext` read global theme flags. The shared Ribbon style
now resolves `QatTitleBarColored` and `QatTabRowColored` metrics in its own resource
scope. ThemeManager sets these alongside application accent-band overrides;
every base palette supplies neutral defaults. Internal attached selectors refresh
existing QAT items when effective scoped values change. No public C# API was added.
The RibbonKit-only consumer covers independent Crystal light/dark scopes against
global Office 2019, Office 2024 and Crystal, every QAT placement, nearer metric
overrides, palette removal and accent-band toggling.

A 2026-10-08 startup follow-up reproduced `QatTabRowColored` throwing when the
saved tab-row QAT was restored before the first theme apply. Removing the manual
App.xaml palette synchronously invalidated the style selectors; their callback
read a second selector during the temporary resource gap. `ThemeManager.Apply`
now merges the replacement before retiring earlier dictionaries, and first-call
cleanup excludes that replacement. No selector fallback, deferred refresh or
public API change was needed. Actual Showcase startup and independent consumer
checks passed; current evidence and deferred gates are in the
[touch plan](../../14-TOUCH-DENSITY-PLAN.md#startup-theme-replacement--2026-10-08).

`CrystalMainWindowPresentation` remains Showcase's host adapter for palette and
optional `CapturedBackdrop` registration. Its previous tab-only discovery missed
QAT dropdowns and additions to existing groups, and retained removed dropdowns.
It now watches the tab, group, QAT and nested ItemsControl collections while enabled,
reconciles registrations and disposes removed captures. Disabling or closing
detaches collection handlers and disposes the remaining registrations. A focused
test checks an actual QAT popup's captured brush, subsequent additions/removals,
collection resets, disable/re-enable and close. Capture implementation remains in
the existing optional library contract; document fade/underlay paint is host-owned.

`Controls.ActionTemplates.xaml` holds the shared message/Crystal Backstage action
template, and `Controls.Actions.xaml` holds its styles and interaction states.
Office palettes use its standard defaults; Crystal light
and dark select its glass defaults with thin style references. Geometry, access-key
recognition and disabled opacity use typed metrics. Idle paint uses ordinary
setters so a derived host style retains its WPF precedence; it is not supplied by
a material trigger. The existing message style and scoped template keys are kept,
including direct Button values and live brush lookup. Palette dictionaries merge
the shared actions so manually merged palettes also expose the keyed resources.
The dark palette merges them directly because tint construction can realize its
styles before attaching the combined palette. The message template also merges
the shared template dictionary in its own stable scope: replacing a palette's
style must not replace the button template and discard local part values.
The separate consumer retains light/dark and all-Office action coverage, adds
disabled opacity checks and verifies replacement/removal of the existing scoped
template key.

The Crystal Sidebar/Floating layouts use `RibbonKit.Metrics.Backstage.*` for
navigation, action and container geometry. Every base palette supplies the former
values, preserving an independently selected Backstage design under an Office
palette. The fixed `CRYSTAL` text is removed from the reusable floating template.
Shared-resource tests check back-action paint and scoped corner, padding, floating
margin and sidebar-width replacement/removal. Stale Office 2007 orb and Writer
glyph-migration wording is corrected in the public/theme documentation.

Normal Release and Debug solution builds passed with **0 warnings / 0 errors**
for both runtime targets and refreshed the default Showcase/Writer review outputs.
The final serialized, unfiltered Release solution run passed **466 runtime tests**,
**482 Writer tests**, the visual aggregate covering **113 approved scenes**, and
the RibbonKit-only consumer aggregate in **2m28s**: **950 tests, 0 failed / 0 skipped**.
Each test project records `crystal-cleanup-release.trx` in its TestResults folder.
The final consumer also passed independently in 2m42s
(`crystal-cleanup-consumer.trx`). Visual approvals and tolerances are unchanged.
All **338** shared brush/metric/effect references resolve across the twelve
effective light/dark palettes; the 31 new metric defaults have matching types and
values across all Office bases. The final diff/content/links were reviewed and
`git diff --check` passed.

The agent did not manually launch Showcase or Writer, commit, push or publish.
The active plan owns bounded live review of the revised Backstage appearance and
dynamic popup/QAT capture. Existing native DPI, motion, open-menu scaling and
mixed-monitor gates retain their separate scope; automated proof does not close them.

The follow-up Showcase review needed a visible disabled message action. Main
Showcase now adds a third, initially closed `CONTENT BLOCKED` message through
the existing Add message control. Its Enable Content action uses an unbound
host-owned routed command, so native command routing disables only the action;
the row and dismiss button stay enabled. Dismiss messages includes the new row.
No shared control, template, token, Writer code or RibbonKit public API changed.
The existing sample markup check now expects three main-window messages and
two RTL-lab messages. The existing Crystal integration fixture opens all three,
checks the disabled action's 0.4 opacity and enabled row/dismissal, then verifies
Dismiss messages closes the new row. All seven focused existing tests passed.
Normal Release and Debug Showcase builds passed with zero warnings/errors and
updated the default review executable. The full suite and visual snapshots were
not repeated for this host-only example; its live appearance remains in the plan.

### 3.218 Keyboard focus and application-menu navigation — 2026-10-04

The user reported a dotted focus rectangle around the whole Ribbon, duplicate Tab
stops on ordinary application-menu navigation rows, an unreachable split arrow,
and Enter failing on a focused row. `Ribbon` and `RibbonApplicationMenuItem`
inherited focusability from `Control`; each navigation row also contained a native
Button, while its arrow explicitly disabled focus. The container had no native
button activation behavior.

The shared defaults now exclude the Ribbon and navigation-row containers from
keyboard focus and Tab traversal. Native template buttons own Enter/Space,
command execution and Click routing. Plain commands and dropdown rows have one
Tab stop; split commands and their arrows have separate stops. A dropdown's primary
target spans the whole row, with a token-sized spacer preserving the header's
original layout area. Keyboard navigation claims the same panes as pointer hover.

The menu keeps a fallback focus target without an outline for empty content, excluded
from the cyclic Tab order. Opening defers focus to the first enabled action until
layout realizes its template. This initial transfer does not claim a pane: opening
still shows the default page, including menus whose first item has pane content.
That distinction preserves the existing viewport/default-page contract. Subsequent
navigation updates pane ownership; pane commands do not reset their own pane.
Scrollbar-button dismissal exemptions and native scrolling remain intact.

`Controls.Focus.xaml` supplies one solid, inset, theme-colored WPF focus adorner.
Ribbon buttons/toggles/options, tabs, split/dropdown template buttons, menu rows,
gallery tiles and utility actions reference it from their shared dictionaries.
Its brush and rounding use existing input-focus and control-corner tokens.
WPF retains keyboard-only focus display, rather than adding persistent mouse-focus
outlines. The menu fallback suppresses its own focus visual. No public API or
Showcase/Writer implementation was added.

The RibbonKit-only consumer regression traverses ordinary, disabled, separator,
split and dropdown rows, pane commands and the footer in both directions. It checks
focus cycling without entering the document, native Enter/Space Click activation,
routed command/parameter execution, command dismissal, pane-open retention and Esc,
across all six themes, light/dark and LTR/RTL. It also realizes WPF's actual focus
adorner and checks its brush and bounds. Optional `RIBBONKIT_FOCUS_DIAGNOSTICS`
exports Crystal command/arrow/dropdown PNGs. All six light/dark diagnostic images
were inspected; the split ring surrounds the arrow alone and the dropdown ring
surrounds the full row.

The offscreen fixture forces WPF's keyboard-focus visual display through its
internal keyboard-navigation hook and raises routed key events. This verifies
realized control/template behavior, rather than physical keyboard or target-machine
appearance acceptance. Native Showcase keyboard review, DPI/mixed-monitor behavior
and final visual acceptance remain separate live gates.

Normal Release and Debug solution builds passed with zero warnings/errors for
both runtime targets and refreshed the default Showcase/Writer review outputs.
The final serialized Release solution run passed **950 tests, 0 failed / 0 skipped**:
**466 runtime**, **482 Writer**, the visual aggregate covering **113 approved scenes**,
and the RibbonKit-only consumer aggregate. Results are recorded as
`keyboard-focus-release-final.trx` in each test project's TestResults folder.
The final consumer also passed independently (`keyboard-focus-consumer.trx`).
Visual approvals/tolerances and the public API baseline are unchanged. The final
diff and documentation pointer were reviewed; `git diff --check` passed.
Showcase/Writer were not manually launched, and nothing was committed, pushed or
published.

### 3.219 QAT order, menu traversal and Backstage focus cycling — 2026-10-04

After reviewing §3.218, the user accepted the focus changes except for lower-QAT
ordering, dropdown/application-menu traversal and Backstage wrapping/action focus.
Their recording was reviewed. The follow-up remains shared RibbonKit behavior;
no Showcase/Writer glue or public API was added, and the accepted focus paint is
unchanged.

WPF walks panel children in paint order. The body is raised over a lower QAT's
shadow and the headers are raised over the body for connected-tab chrome. Local
navigation groups and TabIndex values now order the shared visual roots as
headers, body commands, lower QAT. They leave ZIndex and geometry intact. The
template root is the group instead of the TabControl, whose remembered selected
header otherwise overrides the first entry point. A tab-row QAT still precedes
body commands, including after changing placements.

Dropdowns and inherited split dropdowns transfer focus into their realized popup
items after opening. Tab/reverse traversal and arrows cycle within the menu;
Home/End select its ends. Up/Down can open to the last/first item. Closing while
focus belongs to the menu returns it to the opener. Direct popup Escape handling
uses the existing dismissal stack, preserving nested flyouts and borrowed-item
return. Arrow handling precedes ScrollViewer consumption for menu buttons while
embedded editors retain their normal input.

Application-menu Up/Down/Home/End traverse enabled navigation rows. Horizontal
arrows visit a split arrow and its pane and return to the navigation target,
mirrored in RTL. Pane buttons also support vertical traversal. Ordinary commands
can enter the default pane. The existing cyclic Tab order, Enter/Space activation,
default-page-on-open behavior and scrollbar dismissal exemption remain intact.

Backstage action containers coerce IsSelected to false. WPF TabItem previously
selected an action during PreviewGotKeyboardFocus; reverting that selection also
returned focus to the previous page, preventing traversal beyond Options to Exit.
Action focus now preserves the selected page and Enter/Space performs the action.
Cycles are scoped to each shared template's visual root so wrapping visits the
return button and first navigation item instead of the remembered last page.
The design-owned hidden return buttons remain hidden.

`KeyboardNavigationPortabilityChecks` extends the RibbonKit-only consumer with
WPF InputManager key processing, including native navigation post-processing;
raising routed events alone had missed these transitions. Reverse traversal uses
WPF MoveFocus. Coverage includes all six themes and LTR/RTL, placement round trips,
disabled rows/separators, dropdown and split activation/command parameters,
application-menu pane traversal, repeated cycles and all seven Backstage designs.
The nested QAT case checks inner-first Escape dismissal, focus recovery and
borrowed-item return. Synthetic/offscreen evidence remains separate from physical
keyboard and target-machine acceptance.

Normal Release and Debug solution builds passed with zero warnings/errors and
refreshed the default consumer review outputs. The serialized Release solution
invocation passed **466 runtime tests**, **482 Writer tests** and the visual
aggregate covering **113 approved scenes** (`keyboard-navigation-release-final.trx`).
Its consumer failure was an offscreen fixture setup error: the outer QAT chevron
is intentionally nonfocusable, and the nested popup needed a focused owner surface
before opening. The corrected RibbonKit-only consumer then passed independently
in a fresh process (`keyboard-navigation-consumer.trx`, 2m33s), including nested
Escape and the full existing portability aggregate. The combined final results
are **950 passing tests, 0 skipped**; the affected consumer was rerun rather than
repeating unchanged passing projects.

Visual approvals/tolerances and the public API baseline are unchanged. The final
diff and documentation links were reviewed; `git diff --check` passed. Showcase
and Writer were not manually launched. Physical keyboard/DPI review remains a
live gate for the user; these follow-up changes are uncommitted.

### 3.220 File-first entry, QAT overflow and Backstage opening focus — 2026-10-04

The next recording exposed three remaining cases: File followed the selected tab
and minimize toggle, zero-slot QAT commands remained keyboard stops, and opening
Backstage focused its whole surface with WPF's dotted outline.

The shared header template now orders the File layer, tab-row QAT, tab strip and
minimize toggle explicitly, retaining their paint order and geometry. Native Tab
entry from document content starts at File; selected-tab navigation and the lower
QAT order from §3.219 remain intact.

RibbonQuickAccessPanel temporarily excludes each overflowed original subtree from
Tab, Ctrl+Tab and directional navigation and suppresses the root's focusability.
Visibility, enabled state and proxy commands remain application-owned. Current-value
overrides preserve bindings and local/style values, observe application updates
while hidden, and are released when items fit again, leave the toolbar, or the
panel becomes hidden/unloaded. InvalidateProperty removes the temporary override;
ClearValue would discard an application's local value or binding.
Template replacement releases the old panel's overrides before another panel
can claim the same live items.

Backstage opening enters its visible Back button or the first available action
after its template/layout is realized. WPF's automatic selected-header focus during
container generation is included in that opening transition. The root has no focus
adorner. Shared Backstage item styles, Crystal return actions and the Classic2007
orb return proxy use the existing theme-colored keyboard outline. No public API,
Showcase helper, theme token, visual approval or tolerance was added.
The shared style is resolved statically by the dictionaries; only its palette and
metric tokens remain dynamic. A dynamic lookup of the style key from a native
Button in the adorner branch fell back to WPF's default dotted focus style.

The RibbonKit-only keyboard consumer now covers File entry from document content,
selected-tab preservation, closed QAT overflow in both directions, width recovery,
binding/local-value retention and removal, and Enter opening/reopening with real
Backstage focus adorners across all six themes, LTR/RTL and seven designs.

Normal Release and Debug solution builds passed with zero warnings/errors. The
serialized full Release solution run passed **950 tests, 0 skipped**: **466 runtime**,
**482 Writer**, the visual aggregate covering **113 approved scenes**, and the
RibbonKit-only consumer aggregate (`keyboard-entry-release-final.trx`). The consumer
completed in **3m04s**, including minimized entry, QAT template replacement and the
explicit shared Crystal Backstage styles. Its existing STA aggregate timeout was
raised from 180 to 300 seconds after the expanded render-diagnostic run reached
the old cap; assertions and visual approvals/tolerances remain unchanged.

Real focus adorners were checked for their solid theme brush and action bounds,
and rendered Crystal Back/row outlines were inspected. These offscreen checks
do not establish physical keyboard or DPI acceptance. The default Debug Showcase
output is refreshed for user review; Showcase/Writer were not manually launched.
The final diff and documentation links were reviewed and git diff --check passed.
These follow-up changes remain uncommitted.

### 3.221 Active Backstage entry and a single focus outline — 2026-10-04

User review identified duplicate outlines on Crystal Back and navigation items
and changed the opening preference from Back to the active page. Backstage now
focuses its selected visible/enabled navigation container after realization,
preserving the selected page. If unavailable it tries the first available item,
then Back, retaining the root only as an empty-surface Escape fallback.

Crystal navigation's legacy focused accent-border trigger is removed. The Crystal
Backstage action style retains its ordinary glass border when focused, overriding
the inherited glass-action accent emphasis. The shared keyboard adorner is the
single focus outline; selection, hover and unrelated message actions retain their
existing paint. No public API, palette token or consumer helper is added.

The RibbonKit-only keyboard checks open on a non-first selected page, preserve
selection, reopen on the last active page, fall back from a disabled selection,
and check native focus adorners and unchanged ordinary template borders while
focus moves between page content, Back and navigation. The matrix includes all
six themes, LTR/RTL, seven designs and
the explicit shared Crystal styles. Optional render diagnostics capture Crystal
Back/row outlines for inspection without repeatedly rendering every palette.

Normal Release and Debug solution builds passed with zero warnings/errors. Final
passing evidence covers **950 tests, 0 skipped**: **466 runtime**, **482 Writer**,
the visual aggregate covering **113 approved scenes**, and the RibbonKit-only
consumer aggregate. The initial serialized solution run had two Writer keyboard
failures; both passed in fresh targeted runs. The F10 check identified a held Ctrl
key, which the user confirmed and released before its passing retry.
The consumer fixture now drains the selected-content DataBind update before
focusing page content; its final Release run passed in **3m06s**
(`backstage-active-focus-consumer-final.trx`). No already-passing project was rerun.

Real focus adorners and unchanged template borders passed throughout the matrix.
Rendered Crystal Back and navigation outlines were inspected, including RTL.
Visual approvals/tolerances and the public API baseline are unchanged. The normal
Debug Showcase output is refreshed; Showcase/Writer were not manually launched.
Physical keyboard/DPI acceptance remains with the user. Documentation links and
the final diff were reviewed; git diff --check passed. Changes remain uncommitted.

### 3.222 Animated focus placement and collapsed-group navigation — 2026-10-04

User review exposed a focus outline lagging the Backstage slide and an inaccessible
collapsed-group flyout. The supplied recording was inspected. Realized native
focus tests reproduced a 15-DIP intermediate-slide offset in LTR and RTL; group
tests showed focus remaining on the collapsed opener after Enter.

The shared keyboard-focus template now opts into an internal placement tracker.
While its native WPF adorner is loaded, rendering ticks compare the adorned
target's full coordinate basis and render size. Changed placement refreshes the
target's adorner layer, matching the existing KeyTip correction in §3.3. Unchanged
frames perform no layout update. Unloading/disabling releases the rendering
subscription, and a newly loaded outline starts fresh. WPF still owns keyboard-only
display; there is no fixed animation delay or new public API.

RibbonGroup now moves focus into its realized flyout, cycles native Tab/reverse
and spatial arrow navigation, and lets Up/Down open at the last/first command.
Home/End visit the flyout's ends; embedded editors keep their normal arrow handling.
Escape uses the existing nested dismissal stack and returns to the opener.
Command invocation retains deferred closure/reparenting and restores focus when
owned by the flyout; closing after focus leaves preserves the new target.
Template replacement detaches the old keyboard handlers. No Showcase/Writer helper
or palette change is introduced.

Six native runtime regressions cover intermediate/final slide placement, scaling
and focus-visual removal/recreation in Crystal and Office 2024, LTR/RTL; collapsed
groups cover command traversal, disabled commands, an editor, the dialog launcher,
nested dropdown Escape, Up/Down entry and invocation/focus return. The first four
regressions failed before the production fix and passed afterward. The independent
RibbonKit-only consumer additionally checks native Tab cycles, reverse traversal,
Enter/Space entry, Home/End, real focus adorners and external focus preservation
for collapsed groups across all six themes and both text directions.

The normal Release solution build passed for both runtime targets with zero
warnings/errors. The serialized full Release solution run passed **956 tests,
0 failed / 0 skipped**: **472 runtime**, **482 Writer**, the visual aggregate
covering **113 approved scenes**, and the independent consumer aggregate
(`focus-motion-group-release.trx` in each project's TestResults folder).
The consumer completed in **3m50s**; its existing 300-second timeout and all visual
approvals/tolerances remain unchanged.

At this checkpoint, the normal Debug solution refresh was blocked while Showcase and its Visual Studio
debugger held the consumer's RibbonKit.dll. The user was asked to stop debugging
and close Showcase; that preview contained the previous build until the refresh in §3.223.
No application was manually launched or closed. Physical keyboard, live motion
and DPI acceptance remain with the user. The final diff and new documentation
pointer were reviewed; git diff --check passed. Changes remain uncommitted.

### 3.223 Circular Back and application-orb focus — 2026-10-04

User review accepted the preceding keyboard changes except for rectangular focus
outlines on circular buttons. The two supplied screenshots show the circular
Back action and the Office 2007 orb; the orb's visible disc extends above its
shorter tab-row button slot.

Controls.Focus now provides shared circular and application-orb focus styles.
The circular style draws an inset vector ellipse. The orb style mirrors the
existing ApplicationOrbSize and ApplicationOrbMargin tokens with top alignment,
so the ring follows the sphere's overhang instead of the rectangular hit-test
slot. Both keep the existing input-focus brush, one-DIP stroke and loaded-only
animation tracking. The Backstage disc uses the circular style; the application
button selects the orb style only in Orb mode. Returning to Tab mode restores
the ordinary rectangular focus style. The Classic2007 return proxy already copies
the application's resolved focus style and uses the same sphere template, so it
inherits the correction. No public API, palette token or host helper was added.

Two realized native-focus regressions cover the real orb, its Classic2007 proxy,
an outline Back disc, a glass Back disc and restoration of File-tab focus in LTR
and RTL. They fail before the new styles. Bounds are compared with the visible
disc inset, allowing at most one physical pixel for separate native-focus and
ribbon layout rounding at fractional DPI. The captured actual rings were inspected
before establishing that bound. The shape fixture disables/restores motion to
keep successive design changes independent; the existing motion regressions
continue to cover render tracking. Its PNGs show circular rings in both directions.

The independent consumer asserts circular native adorners for the Backstage disc
and verifies explicit Orb/Tab switching across all six themes, light/dark and
LTR/RTL. Rectangular controls retain their existing brush/bounds checks.

Normal Debug and Release solution builds passed for both runtime targets with zero
warnings/errors after the user released the file locks. The default Debug Showcase
and Writer outputs are refreshed, including the preceding motion/group changes.
Final passing evidence covers **958 tests, 0 skipped**: **474 runtime**, **482 Writer**,
the visual aggregate covering **113 approved scenes**, and the RibbonKit-only
consumer aggregate (`circular-focus-release.trx`). The initial full run had one
Writer editor-focus assertion failure; it passed in a fresh targeted process
(`circular-focus-writer-retry.trx`) without changing Writer code or assertions.
Its transient cause was not confirmed. The independent consumer passed in **3m13s**.

The circular Back, real orb and proxy PNGs were inspected in LTR/RTL. Visual
approvals/tolerances and the public API baseline are unchanged. No application
was manually launched. Live circular-focus and DPI acceptance remain with the user.
The final diff and new documentation pointer were reviewed; git diff --check passed.
Changes remain uncommitted.

### 3.224 Focus outlines follow rendered button surfaces — 2026-10-05

User screenshots showed an offset focus outline on the Colored Title Bar toggle
and mismatched corners on a Crystal Backstage navigation row. The native focus
template used the whole control's bounds and a generic corner radius; shared
templates paint their visible Chrome border inside that slot with independent
margins and corner geometry.

FocusVisualTracking now measures the realized Chrome border for ordinary shared
actions and the visible OrbFill ellipse for application orbs and Classic2007
proxies. Other templates retain the control-bounds fallback. A two-DIP inset
contour follows the surface, with correspondingly reduced border corner radii.
The focus template uses a vector border or ellipse in a local LTR canvas and maps
the surface's full coordinate basis into it. This preserves margins, alignment,
RTL, scale and rotation without a separate layout-rounding approximation. Orb
focus reuses the circular style instead of duplicating sphere layout metrics.
Loaded-only layout/render tracking refreshes geometry and motion; unloading or
disabling the tracker releases both subscriptions. WPF still owns keyboard-only
display, and the existing focus brush follows the palette. No public API, theme
token, Showcase helper or input behavior was added.

Four new native-focus regressions cover Office2024/Crystal and LTR/RTL. They fail
on the former toggle bounds, then verify toggle, gallery-tile and Crystal navigation
surfaces, including changed margins, unequal corners and render transforms while
focus remains on the same control. All **12 focused interaction cases** passed,
including the existing motion, circular/proxy and collapsed-group cases. Disc
and border geometry now agree within **0.1 DIP**, replacing the previous one-
physical-pixel circle approximation. Actual before/after, transformed RTL,
circular and independent-consumer light/dark PNGs were inspected.

The independent RibbonKit-only consumer checks realized toggle and gallery-tile
focus across all six themes, light/dark and LTR/RTL, plus the existing navigation,
Backstage and orb matrix. The helper now processes the focus template's Loaded
event before measuring it, and compares popup geometry within its own adorner
instead of across separate presentation roots. Shape checks run first; any
navigation failure records the last key, modifiers and focus/window state.
These are verification changes, not input-behavior workarounds.

Normal Debug and Release solution builds passed for both runtime targets with
zero warnings/errors. Passing evidence covers **962 tests, 0 skipped** across
the full run and targeted retries: **478 runtime**, **482 Writer**, the visual
aggregate covering **113 approved scenes**, and the complete independent consumer.
The initial visual run reached its 30-second limit; a separate unchanged retry
passed in 26 seconds (`surface-focus-visual-retry.trx`). The Writer F10 check
detected Alt during the initial run and passed unchanged in a fresh process
(`surface-focus-writer-retry.trx`). After correcting the geometry helper, two
consumer attempts lost focus at different existing navigation assertions; no
runtime navigation code or assertions were weakened. The final full consumer
passed in **4m39s** (`surface-focus-consumer-final.trx`). The cause of those
intermittent input failures was not confirmed. This is combined passing evidence,
not a claim that the initial full-suite invocation was clean.

Default Debug outputs are refreshed. Public API and visual approvals/tolerances
are unchanged. Final appearance and native DPI/mixed-monitor acceptance remain
with the user; no application was manually launched. The final diff and history
pointer were reviewed and git diff --check passed. This correction is uncommitted
on top of the user's `7cc425c` keyboard-improvements checkpoint.

## 3.225 Focus contrast on accent-filled rails and headers — 2026-10-05

Office 2013/2019 Classic Backstage rows used the accent focus brush over an
accent-derived selected/hover fill. Office 2019's colored title-bar option also
painted the ribbon header band with that same accent, hiding focus on File,
unselected tab headers and the collapse button. Native-focus checks reproduced
the exact blue-on-blue failure with a contrast ratio of 1.

Shared templates now mark the underlying focus surface for File, ribbon tabs,
the collapse button, Classic Backstage rows and the shared Back button. The
internal focus tracker composites a solid Chrome fill over that surface and
keeps the ordinary focus brush when it provides at least 3:1 contrast. Otherwise
it selects the more contrasting KeyboardFocus.Light or KeyboardFocus.Dark brush;
both keys exist in all twelve theme token dictionaries. Selected tabs with a
neutral fill retain the accent outline when readable. Orb focus retains its
existing treatment. HeaderChrome is recognized alongside Chrome for tab geometry.

Palette, hover/selection fill and colored-title-bar changes refresh the native
adorner while focus stays on the same control. Navigation fills, text colors,
keyboard behavior and public C# API are unchanged. The implementation lives in
RibbonKit and requires no Showcase resources or host patch.

Four new native-focus cases cover Office 2013/2019, light/dark, LTR/RTL and blue,
purple and pale-yellow accents, including selected/hovered Backstage rows, Back,
File, neutral/colored tab headers and the collapse button. The independent
RibbonKit-only consumer also checks the realized outlines and geometry on both
themes' colored headers and Classic rails.

Normal Debug and Release solution builds passed for both runtime targets with
zero warnings/errors; default Debug consumer outputs are refreshed. Passing
Release evidence covers **966 tests, 0 skipped**: **482 runtime**, **482 Writer**,
the visual aggregate (**113 unchanged approved scenes**), and the independent
consumer. The first two consumer runs missed Space activation at different
existing assertions. The user confirmed they were interacting with the keyboard;
after leaving input idle, the complete consumer passed in **3m21s**
(`contrast-focus-consumer-final.trx`). Assertions and runtime input behavior were
not weakened. The consumer fixture switches its File surface from the application
menu to Backstage before inspecting the new rail cases, and activation failures
now include modifier/focus diagnostics. These totals combine the full run with
that consumer retry, rather than describing the initial invocation as clean.

Actual before/after blue, purple, pale-accent, dark and RTL focus renders were
inspected, including the independent consumer's colored header. No snapshot
approvals or tolerances changed. Final appearance and native DPI/mixed-monitor
acceptance remain with the user; no app was manually launched. Changes remain
uncommitted on the user's keyboard-improvements branch.
