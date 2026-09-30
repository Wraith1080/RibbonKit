# Crystal portability and Office 2007 orb integration plan

> Status: slices 1–4 implemented on 2026-09-28; slice 5 implemented on
> 2026-09-30 with live visual review pending. Slice 6's QAT pass is implemented
> on 2026-09-30 with the bounded live QAT review complete, including its
> minimized/message corner correction. Slice 6's message-bar pass is implemented
> on 2026-09-30 with the bounded light/dark live review complete: expanded and
> minimized stacked appearance, independent dismissal and reopening were verified.
> The application-menu pass is implemented on 2026-09-30; fresh light/dark
> screenshots verify its default page, Save As split pane and Publish dropdown.
> The user confirmed Esc closing, native command invocation and Recent Documents
> restoration on reopening in both themes. Narrow default and Save As pages fit
> in both themes, with clean description wrapping and visible footer actions.
> The user reported normal menu width returning after widening. Save As also
> fits above two notices with the ribbon minimized; Esc closing and default-page
> restoration are confirmed in both themes in that state. RTL default and Save
> As screenshots verify mirroring, mixed text and the lab's corrected wrapped
> label in both themes. RTL Esc closing, default-page restoration on reopening
> and return to LTR are also confirmed in both themes. Alignment and Esc closing
> after returning to 125% are confirmed. Fresh 200% light/dark screenshots show
> the corrected footer fully visible, completing its bounded appearance/return
> check. Scrollbar-arrow dismissal was then reproduced and corrected in the shared
> close handler. The user confirmed scrollbar buttons now scroll without closing
> File in both themes. The user also confirmed Tab navigation and Esc closing
> after tabbing. Repeated File open/Esc/quick-reopen motion also passed in both
> themes. The user then confirmed thumb/wheel/track scrolling and reverse keyboard
> traversal, including footer reachability and focus cycling. The bounded slice 6
> live review is complete. Reduced motion, broader DPI transitions and deferred
> layout issues remain open; this is not blanket final acceptance.
> Slice 7 utility chrome and scrollbars are implemented on 2026-09-30, with
> automated portability/visual checks passing. The bounded live review is complete
> on 2026-10-01: utility/modal controls, native scrolling/focus/RTL, corrected
> spacing and 125% → 200% → 125% return are accepted. The final spacing/DPI
> checks passed for both reviewed themes. Broader acceptance gaps remain open.
> Slices 8–10 remain open. Contextual/tint policy is still slice 8 work.
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
   template-part edits were removed. Slice 7 subsequently promoted the native
   scrollbar material and retired `CrystalCustomization`; the existing app tint
   choice remains host policy for slice 8. The app-provided Editor page and
   dialog-opening policy stay in Showcase. Broader slice 5 appearance acceptance
   remains separate from the completed slice 7 scrollbar review.
6. **Shared shell chrome (QAT, message-bar and application-menu passes implemented; bounded live review complete).** Shared
   ribbon templates and Crystal light/dark tokens now supply the below-ribbon
   QAT drawer: 16-DIP inset relative to the default body, open top rim, 10-DIP
   lower corners, 32-DIP minimum height, flipped glass border and zero-depth
   shadow. The minimized drawer retains its complete rim and four rounded
   corners, including above messages. Its 3-DIP top gap applies without messages;
   message presence retains the existing spacing. The combined-state
   `QatExtenderCornerRadiusMinimizedMessageBar` token supplies 10-DIP Crystal
   corners while preserving Office's connected message-row corners.
   `ContentCornerRadiusQatBelow` coordinates the rounded Crystal body
   independently of message-only body geometry. `ContentZIndexQatBelow` raises
   its seam shadow below the application-menu overlay. Office palettes retain
   their previous geometry, paint and shadow values. All new shared keys have
   matching Office defaults, and `DynamicResource` updates and explicit local
   value precedence are verified. This pass adds no public C# API.
   `CrystalQuickAccess` and its main/preview callers were removed after the
   RibbonKit-only consumer proved the replacement. Document underlay/fade and
   preview comparisons stay in Showcase; their optional host contract remains
   slice 9 work. Shared `ContentCornerRadiusTop` now retains the body's four
   14-DIP corners above messages; the preview override was removed. Message
   rows already used shared amber paint, spacing, rounding and shadows. Their
   action button now uses one shared template with a dynamic theme style token,
   `RibbonKit.Styles.MessageBar.ActionButton`, and `MessageBar.ActionCornerRadius`
   and `MessageBar.ActionRecognizesAccessKey` metrics. Crystal retains the accepted
   glass action paint, 12-DIP corners, hover/pressed/focus states and 0.4 disabled
   opacity. Office retains its existing paint, 0/2-DIP corners and 0.45 disabled
   opacity. Dynamic resources preserve scoped overrides and explicit action
   style/property choices; no public C# API was added. The single STA consumer
   verifies the replacement without Showcase; `CrystalMessagePresentation` and
   all callers were then removed. Message content, action policy and preview
   comparisons remain host-owned. The separate preview window and launch path remain.
   The application-menu pass now supplies shared translucent frame/pane paint,
   14-DIP outer corners, 11/10-DIP content rims, a 9-DIP inner clip and 8-DIP
   split-row clips. Crystal navigation remains 160 DIPs; its pane shrinks from
   300 to a 180-DIP minimum using the owning Ribbon's width, navigation width
   and a 32-DIP viewport allowance. Resource-declared, pre-templated menus and
   reassignment to another Ribbon receive current viewport sizing. Geometry
   bindings follow layout and dynamic tokens without realized-part patches.
   Shared outside-only shadow casting preserves the translucent interior;
   Office retains its whole-frame shadow and original compact geometry.
   New matching theme keys preserve scoped resources and local paint, size,
   margin, corner and effect choices. No public C# API was added.
   `CrystalApplicationMenuPresentation` and `CrystalMenuShadow` were removed
   after consumer verification. `CrystalApplicationMenuBackdrop` now attaches
   only the host's captured blur; its capture/lifecycle integration remains
   slice 9 work. Application-menu content, commands, comparison controls and
   the choice between menu and Backstage stay with the host. Fresh light/dark
   screenshots now verify default-page, Save As split and Publish dropdown
   appearance. Esc closing, split-primary/pane command invocation and default-page
   restoration on reopening are confirmed in both themes. Narrow default and
   Save As pages fit in both themes, including all four wrapped descriptions
   and visible footer actions. The user reported normal width restoration after
   widening. Save As appearance above two notices with the ribbon minimized,
   Esc closing and default-page restoration are verified in both themes in
   that state. RTL default and Save As screenshots verify mirrored placement,
   mixed text and the lab's full bilingual label on two lines. Its host-owned
   `HeaderTemplate` correction passes focused tests and fresh appearance review.
   The user confirmed RTL Esc closing, default-page restoration on reopening and
   return to LTR in both themes. The bounded RTL appearance/close/reset review is
   complete. The shared frame now follows available client height,
   with native scrolling for navigation/default/active content and a stationary
   footer. The captured backdrop no longer contributes to menu measurement.
   Fresh 200% light/dark screenshots show both footer actions fully visible;
   the user confirmed alignment/Esc closing after returning to 125% (§3.202).
   Live scrolling revealed scrollbar arrows dismissing File while thumb dragging
   kept it open. The shared click handler now exempts ScrollBar descendants;
   command clicks still dismiss (§3.203). The user confirmed scrollbar buttons
   now scroll without closing File in both themes. The user also confirmed Tab
   navigation and Esc closing after tabbing. Repeated File open/Esc/quick-reopen
   motion also passed in both themes. The user confirmed thumb/wheel/track
   scrolling and reverse keyboard traversal, including footer reachability and
   focus cycling. The bounded QAT/message-bar/application-menu live review is
   complete. Reduced motion, broader DPI transitions and deferred layout issues
   remain open; this does not establish blanket final acceptance.
   The separate preview window and slice 10 remain.
7. **Utility chrome and scrollbars (implemented; bounded live review complete).** Shared
   templates now supply non-interactive idle/hover/pressed/checked rims for
   minimize, modal close, QAT overflow, ribbon/tab scrolling arrows and merged
   caption buttons. Rims share Chrome's bounds/corners without altering content
   padding or command routing. Office keeps zero rim opacity and its existing
   arrow surfaces. Crystal retains the accepted idle/hover surface swap and
   pressed priority. The small internal `UtilityChrome` selector lets styles
   consume existing dynamic brushes directly, preserving local/scoped overrides.
   Crystal light/dark tokens opt native ScrollViewer bars into the existing
   shared template, including both built-in customization pages and app-owned
   Options content. Thickness is 14 DIPs; rail/button/thumb corners are 4 DIPs;
   the thumb has a one-DIP rim and the accepted 6/8/11-percent accent washes
   (15/20/28 alpha steps). The internal `ScrollBarMaterialConverter` composes
   only marked Crystal tokens from the realized thumb's resource scope;
   Office and explicit host thumb brushes pass through unchanged. A regression
   first exposed nested Freezable resources retaining another window's wash,
   then proved dialog-local tint isolation and updates. Native tracks, commands,
   hover/drag states, keyboard/focus and RTL behavior remain with WPF.
   `ScrollBar.WashAccent` separates paint from the existing main-window raw-tint/
   dialog readable-accent policy. This host policy stays in Showcase for slice 8;
   capture integration remains slice 9. No public C# API was added.
   After RibbonKit-only proof, `CrystalUtilityChrome`, `CrystalScrollBars` and
   `CrystalCustomization` and their callers were removed. The designer-safe
   customization scrollbar adapter remains and consumes the same shared tokens.
   Release solution compilation, all 418 eligible runtime tests, the single
   RibbonKit-only consumer and all 105 visual scenes pass. The existing 99
   approvals/tolerances are unchanged; six new light/dark utility/scrollbar
   scenes were inspected at 100/200% and RTL before adding approvals.
   The user accepted utility/arrows and modal/merged-caption review checks 1–2.
   The customization spacing correction makes all four list/tree frames use
   FrameInset (Crystal 2 DIPs, Office 1 DIP), removing the
   extra TreeView padding. See [§3.205](history/01-library.md#3205-customization-scrollbar-insets-and-slice-7-live-review--2026-09-30).
   The spacing build again passed with zero warnings/errors; the RibbonKit-only
   geometry/override check, 418 eligible runtime tests and 105 visual scenes pass
   with approvals and tolerances unchanged.
   A follow-up screenshot still showed unequal top/bottom and Crystal side gaps:
   the shared pages now round layout and use FrameInset as a transparent border
   that rounds each edge independently. Tokens are unchanged; local padding and
   host styles retain precedence. See [§3.206](history/01-library.md#3206-customization-scrollbar-pixel-gaps--2026-10-01).
   The isolated pixel-gap Release build passed with zero warnings/errors; the
   consumer's exact 96-variant gap matrix, 418 eligible runtime tests and 105
   visual scenes pass. Crystal/Office 125/200% renders were inspected, with
   approvals/tolerances unchanged.
   Native arrow/track/wheel/thumb scrolling, focus and RTL are accepted by the
   user. On 2026-10-01, the user confirmed spacing and 125% → 200% → 125%
   return passed for both reviewed themes, completing the bounded slice 7 live
   review. See [§3.207](history/01-library.md#3207-slice-7-bounded-live-review-complete--2026-10-01).
   Showcase was not launched by the agent; this acceptance update is prose only.
   The prior 200% Office RTL checkbox failure and gallery-layout report remain
   deferred despite the RTL method passing in this run. See [§3.204](history/01-library.md#3204-shared-crystal-utility-buttons-and-scrollbars--2026-09-30).
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

The user's normal-motion check passed in both themes on 2026-09-30: repeated
File opening, Esc closing and quick reopening, with no reported flicker,
position jump or leftover shadow. The user then confirmed both remaining short
File-menu checks passed, completing the bounded slice 6 live review:

- Thumb dragging, wheel scrolling and track clicks above/below the thumb move
  content without closing File and retain a visible footer. Arrow-button
  behavior was accepted earlier.
- Shift+Tab reverses traversal, focus stays visible, Options/Exit are reachable
  and focus cycles inside File. Tab navigation and Esc closing after tabbing
  were accepted earlier.

Reduced-motion behavior, a menu remaining open through a scaling change and
mixed-monitor DPI transitions remain broader acceptance gaps. The two 200%
layout observations above stay deferred by user direction. Do not infer these
gates from normal-motion or closed-menu scale-return acceptance. No build/test
rerun is needed for this documentation-only confirmation.

## Slice 7 live review

Use `samples/RibbonKit.Showcase/bin/CrystalSlice7Pixels/Release/net8.0-windows/RibbonKit.Showcase.exe`.
The agent did not launch it. The bounded review below is complete on 2026-10-01.

1. **Accepted by the user.** Hover and hold minimize, QAT overflow and tab/body arrows. The separate Crystal
   preview's **Preview controls** offers **Show QAT overflow**, **Show scroll arrows**
   and **Keep Home groups expanded**; narrow the window for the latter. Confirm
   idle/hover/pressed/open rims, scrolling commands and cleanup after disabling
   the demonstration toggles.
2. **Accepted by the user.** In main Showcase, enter **Print Preview** and check its close button, then use
   **MDI Demo** with a maximized child to check merged minimize/restore/close paint
   and commands. Confirm Tab/Shift+Tab focus and a switch back to Office.
3. **Accepted by the user.** The follow-up review verified matching
   top/bottom gaps and compact, equal Crystal list/tree side gaps;
   **native scrollbar input/focus/RTL accepted** for arrow/track/wheel/thumb
   scrolling and visible focus; **DPI return accepted** for 125% → 200% → 125%.
   The user confirmed spacing and DPI return passed for both reviewed themes.
   Original utility/modal checks 1–2 remain accepted. Keep deferred gallery/Office RTL
   observations separate. Reduced motion, open-menu scaling and mixed-monitor
   transitions remain broader gates.

## Verification and completion gates

- The slice 6 application-menu pass on 2026-09-30 reproduced the non-Showcase
  8-DIP frame instead of the accepted 14-DIP Crystal geometry. The Release
  solution build and affected visual-test rebuild passed with zero
  warnings/errors. The focused runtime run passed 116 tests; the eligible
  runtime run passed 417, excluding Writer names and the deferred hover method
  below. The RibbonKit-only single STA/Application consumer passed with
  resource-declared/pre-templated menus, all QAT placements, minimized/message
  combinations, split/dropdown pane and command routing, footer actions, RTL,
  light/dark/Office switches, manually merged palettes, resource replacement,
  explicit local values and menu reassignment between Ribbon owners. A rendered
  pixel check verifies shadow on all four outside edges and none at the center.
  All 99 snapshot scenes passed. The existing 85 captures are byte-identical;
  14 new menu images were inspected before adding approvals for light/dark
  default, split, dropdown, narrow, minimized, RTL and synthetic 100/200% scenes.
  Existing approvals and tolerances are unchanged. Shared menu XML and all 12
  palettes parsed without duplicate direct keys; final diff review and
  `git diff --check` passed. No fresh live menu screenshot, native keyboard,
  real menu DPI/motion or target-machine popup acceptance is claimed. No Writer
  tests, manual Showcase launch, commit, push or publication ran. Earlier QAT
  and message-bar live evidence remains bounded to those passes. Slice 6 is not
  marked complete; slices 7–10 and Writer RKWF-026 remain open. See
  [§3.200](history/01-library.md#3200-shared-crystal-application-menu--2026-09-30).

- The first live application-menu checkpoint on 2026-09-30 received light/dark
  screenshots for the requested 125% setup. Both show expanded Home, title-bar
  QAT and File open on Recent Documents without message rows. The outer frame,
  inner outline and footer retain rounded corners; headings, menu commands and
  footer actions are readable without visible clipping. The host's captured
  blur remains behind the menu content and the visible shadow stays outside
  the frame. Active split/dropdown panes, closing/reopening, narrow-window
  resizing, live RTL and real menu DPI/motion remain pending. This checkpoint
  changes documentation only: content/diff review and `git diff --check` passed;
  no build/test rerun or manual Showcase launch was needed. Slice 6 stays open.

- The subsequent light/dark Save As screenshots verify the rounded split-row
  silhouette and divider, active arrow, pane header and all four commands with
  wrapped descriptions inside the rounded outline. The user confirmed that Esc
  closes the menu cleanly and reopening File restores Recent Documents in both
  themes. Dropdown presentation, native command invocation, resizing, live RTL
  and menu DPI/motion checks remain pending. This documentation-only update
  passed content/diff review and `git diff --check`; no build/test rerun or
  manual Showcase launch was needed. Slice 6 remains open.

- Fresh light/dark Publish screenshots verify the dropdown's complete rounded
  row highlight while its pane is active, with no split divider or dimmed
  command half. The pane heading and both commands/descriptions fit inside the
  rounded outline without visible clipping. Native command invocation,
  narrow-window resizing, live RTL and menu DPI/motion remain pending. The
  records and diff were reviewed and `git diff --check` passed; no build/test
  rerun or manual Showcase launch was needed for this documentation update.

- The user confirmed the native command sequence in both light and dark:
  clicking the Save As command half, Save As arrow → Word Document and Publish
  → Blog each closes File and reports the selected command in Showcase's status
  line. Reopening after each restores Recent Documents. This verifies bounded
  split-primary/pane command handling and reset behavior. Narrow resizing,
  live RTL and menu motion/DPI checks remain pending; slice 6 is still open.
  This documentation-only update passed content/diff review and
  `git diff --check`; no builds/tests were rerun or Showcase launched by the agent.

- Narrow-window light/dark screenshots at the requested 125% setup are each
  518×850 pixels. Both show Recent Documents with a reduced pane width; the
  rounded frame fits within the window and footer actions remain visible.
  The user reported that the menu returns to normal width after enlarging the
  window. These establish narrow default-page appearance and reported width
  restoration; active-pane description wrapping, live RTL and menu motion/DPI
  checks remain pending. The earlier wide pair was retracted by the user and
  was not counted as narrow evidence. Records/diff review and
  `git diff --check` passed; no builds/tests were rerun or Showcase launched by
  the agent. Slice 6 remains open.

- Narrow Save As screenshots in light/dark are each 500×850 pixels. All four
  command descriptions wrap within the reduced pane without visible clipping;
  the heading, rounded split row, frame and both footer actions remain intact.
  This completes the bounded narrow default/Save As appearance check alongside
  the earlier reported width restoration. Live minimized/message menu review,
  RTL and broader motion/keyboard/DPI checks remain pending; slice 6 stays open.
  Documentation content/diff review and `git diff --check` passed; no builds/tests
  were rerun or Showcase launched by the agent.

- Minimized-ribbon Save As screenshots in light/dark are each 1350×850 pixels,
  with title-bar QAT and both Protected View and Security Notice visible.
  The menu overlays the notice rows and document correctly; the rounded frame,
  inner outline, active split row, four command descriptions and both footer
  actions remain intact without visible clipping. The user confirmed that Esc
  closes File cleanly and reopening restores Recent Documents in both themes
  in this combined state. This completes its bounded appearance/close/reopen
  check; live RTL and broader motion/keyboard/DPI checks remain pending, and
  slice 6 stays open. Documentation content/diff review and `git diff --check`
  passed; no builds/tests were rerun or Showcase launched by the agent.

- RTL default-page light/dark screenshots are each 1300×875 pixels. The menu
  anchors at File on the right, navigation/icons are on the right, the recent
  pane is on the left and footer actions mirror leftward. Mixed Arabic/English
  recent content stays readable, and `Report-2026-Q3.docx` remains explicitly
  LTR. The long bilingual Save As label is clipped in both captures. A focused
  reproduction confirmed characters outside its primary hit area; the lab now
  supplies a wrapping `HeaderTemplate` for that host-owned label. Release
  Showcase/runtime/test-project compilation and both focused localization tests
  passed using separate output so the user's running Showcase stayed untouched.
  Shared templates, tokens and APIs were unchanged by this correction. Fresh
  RTL label/pane acceptance, closing/reopening and broader live checks remain
  pending; slice 6 stays open. See [§3.201](history/01-library.md#3201-rtl-lab-bilingual-application-menu-header--2026-09-30).

- Fresh RTL Save As screenshots from the updated build are each 1300×875 pixels
  in light/dark. The complete bilingual navigation label is visible on two
  lines, the split arrow points left and its divider stays inside the rounded
  active row. Both mixed-language pane entries are readable on the left; the
  frame, inner outline and mirrored Exit/Options footer fit without visible
  clipping. This verifies the corrected label and RTL active-pane appearance
  alongside the earlier default-page review. RTL closing/reopening, return to
  LTR and broader motion/keyboard/DPI checks remain pending; slice 6 stays open.
  Documentation content/diff review and `git diff --check` passed; no builds/tests
  were rerun or Showcase launched by the agent for this screenshot checkpoint.

- The user then confirmed RTL Esc closing and Recent Documents restoration on
  reopening in both themes. After closing File and disabling Right-to-Left,
  reopening also restores normal LTR menu placement in both themes. This
  completes the bounded RTL appearance/close/reset review. Broader menu motion,
  keyboard and real DPI checks remain pending; slice 6 stays open. This
  documentation-only update passed content/diff review and `git diff --check`;
  no builds/tests were rerun or Showcase launched by the agent.

- The user confirmed alignment and Esc closing after returning to 125% in both
  themes. The accompanying 1920×1104 captures at 200% showed Options/Exit clipped
  below the window. The shared menu now follows available client height and
  scrolls navigation/default/active content while retaining a visible footer.
  A RibbonKit-only regression covers short/tall/short resizing, scroll offsets,
  anchor reflow, local limits and detach/reattach in Crystal/Office 2007/Office 2024,
  light/dark and LTR/RTL. Captured host blur no longer affects desired size.
  The Release solution build passed with zero warnings/errors; all 418 eligible
  runtime tests, the single RibbonKit-only consumer test and all 99 snapshot
  scenes passed without approval changes. Writer names and the deferred hover
  method remain excluded. Documentation content/link review and `git diff --check`
  passed. Separate framework-specific output preserves the user's running build.
  Fresh 200% footer/scrolling acceptance and the subsequent 125% return check
  remain pending; slice 6 stays open. See
  [§3.202](history/01-library.md#3202-application-menu-footer-at-limited-viewport-height--2026-09-30).

- Fresh light/dark screenshots from the updated build each measure 1920×1104
  pixels at 200%. The Save As heading and all four command descriptions fit;
  the navigation scrollbar is visible and Options/Exit are fully inside the
  rounded footer. The user confirmed alignment and Esc closing after returning
  to 125% in both themes. This completes the bounded footer appearance/return
  check. Live scroll reachability and broader motion/keyboard/DPI checks remain
  pending; slice 6 stays open. This documentation-only checkpoint passed
  content/link review and `git diff --check`; no builds/tests were rerun.

- Live scrollbar-arrow clicks dismissed File, while thumb dragging kept it
  open. The shared handler now exempts only ScrollBar descendants. The native
  click regression first reproduced dismissal, then passed line/page scrolling
  in navigation/default/active panes across Crystal/Office 2007/Office 2024,
  light/dark and LTR/RTL. The Release solution build passed with zero warnings/
  errors and the RibbonKit-only consumer test passed. The eligible runtime run
  passed 417 tests and failed the initial Office RTL option-indicator assertion
  in `Detached_rtl_lab_applies_crystal_and_restores_office_options`, before File
  opens. Isolated reruns failed identically in the new and prior build outputs
  at the current 200% setup; this existing failure remains open. The same Writer/
  deferred-hover exclusions apply. No snapshots were rerun for the routing-only
  correction. Documentation content/link review and `git diff --check` passed.
  Separate `MenuScrollClickCheck` output is ready for fresh light/dark scrolling
  acceptance; slice 6 remains open. See
  [§3.203](history/01-library.md#3203-application-menu-scrollbar-clicks-preserve-the-open-menu--2026-09-30).

- The user confirmed scrollbar buttons now scroll without closing File in both
  themes. This closes the bounded live arrow-click regression check. Page-region,
  thumb-drag and wheel acceptance were not newly reported; broader keyboard,
  motion and DPI checks remain open. The earlier Office RTL option-indicator
  failure remains recorded separately. This documentation-only checkpoint passed
  content/link review and `git diff --check`; no builds/tests were rerun or
  Showcase launched by the agent. See
  [§3.203](history/01-library.md#3203-application-menu-scrollbar-clicks-preserve-the-open-menu--2026-09-30).

- The user supplied `20260930-1141-49.7011675.mp4` and confirmed that keyboard
  Tab navigation works and Esc closes File after tabbing. This records the
  reported sequence; the message does not separately confirm Shift+Tab, footer
  reachability, default-page restoration, or the recording's theme/scale. The
  next bounded live review is opening motion in light/dark. Remaining scrolling,
  keyboard and DPI gates stay open; the two deferred 200% layout observations
  remain parked. Documentation content/link review and `git diff --check`
  passed; no builds/tests were rerun or Showcase launched by the agent.

- The user confirmed that the requested repeated File open/Esc/quick-reopen
  motion check passes in both themes. This closes the bounded normal opening-
  motion review. Remaining checks are listed above; reduced-motion and broader
  DPI behavior are not inferred. Documentation content/link review and
  `git diff --check` passed; no builds/tests were rerun or Showcase launched.

- The user confirmed both final short checks: thumb/wheel/track scrolling keeps
  File open with its footer visible, and reverse keyboard traversal retains
  visible focus, footer reachability and cycling inside File. The bounded
  slice 6 QAT/message-bar/application-menu live review is complete. Utility
  chrome and scrollbar promotion is next in slice 7. Reduced motion, broader
  DPI transitions and the two deferred 200% layout observations remain open;
  the recorded 417-pass/1-fail runtime result is unchanged. Documentation
  content/link review and `git diff --check` passed; no builds/tests were rerun
  or Showcase launched by the agent.

- The slice 6 message-bar pass on 2026-09-30 reproduced the non-Showcase action's
  5-DIP corners instead of the accepted 12-DIP corners. Its Release solution
  build and affected visual-test rebuild passed with zero warnings/errors.
  The focused runtime run passed 100 tests; the eligible runtime run passed 417,
  excluding Writer names and the deferred hover method below. The RibbonKit-only
  single STA/Application consumer passed, covering every QAT placement, minimized
  state, stacked/empty/reopened messages, wrapping, RTL, action command/parameter,
  independent dismissal, disabled and synthetic hover/pressed/focus states, live
  light/dark and Office switches, manual palettes, scoped metric replacement and
  explicit host style/property precedence. All 85 snapshot scenes passed after
  full capture/review: the existing 77 were byte-identical, and eight new Crystal
  message approvals cover light/dark stacks, wrapped body text, rounded actions,
  minimized body collapse, RTL and synthetic 100/200% rendering. No existing
  approval or tolerance changed in this pass. Fresh light/dark screenshots now
  verify expanded Home with TabRow QAT and both notices: body lower corners,
  separated rounded rows, readable action buttons and no visible clipping.
  The user confirmed the expanded action/close/reopen sequence in both themes:
  each action dismisses only its own row, the close target removes the remaining
  row, and Add Message restores both. Further light/dark screenshots verify the
  minimized below-ribbon QAT retains all four rounded corners above both notices,
  with clear row spacing and unclipped actions. The user also confirmed that each
  action dismisses only its own notice while minimized, then expanding, adding
  both notices and minimizing restores them without leftover shadows in both
  themes. This completes the bounded message-bar live review; broader motion,
  keyboard/DPI checks remain pending. Application-menu promotion
  remains open; slice 6 is not complete. See [§3.199](history/01-library.md#3199-shared-crystal-message-bars--2026-09-30).

- The minimized/message corner follow-up on 2026-09-30 first reproduced the
  square upper corners in the RibbonKit-only consumer, then passed the Release
  solution build with zero warnings/errors, 100 focused tests, 417 eligible
  runtime tests with the same exclusions below, and the single STA consumer
  test. Coverage includes live light/dark and Office restoration, manual
  dictionaries, combined-state resource replacement and local-value precedence.
  All 77 snapshot scenes passed. A full capture found exactly two changed PNGs;
  the light/dark minimized-message actual and diff images were inspected before
  renewing only those two approvals. The other 75 images were identical and
  tolerances were unchanged. The initial comparison also passed because this
  small corner change was within its tolerance; the consumer assertion provides
  the exact corner regression gate.

- The bounded live QAT review is complete on 2026-09-30. User screenshots cover
  Crystal light/dark below-ribbon expanded/minimized states with and without
  messages, including the corrected upper corners above Protected View;
  minimized TabRow and title-bar placement at 125%; ordinary commands, Paste
  split and Select dropdown in title-bar overflow; and direct/nested Paste and
  Select popups. The user confirmed Esc closing, the nested two-step Esc sequence
  and reopening with every source-menu command intact. The corrected light
  nested Select screenshot has no open message; its dark counterpart has one.
  Real 200% screenshots cover the maximized minimized/message QAT and Select
  popup in both themes. Showcase stayed open throughout 125% → 200% → 125%;
  Select was closed during the change, then reopened aligned and closed cleanly
  with Esc after the return. The user confirmed removal of the eight temporary
  commands and restoration of Save, Undo, Redo, Paste and Select below the ribbon,
  Home expanded and Windows scaling at 125%. Detailed screenshot evidence is in
  [§3.198](history/01-library.md#3198-minimized-crystal-qat-corners-above-messages--2026-09-30).
  A popup remaining open during a scale change, mixed-monitor transitions, other
  popup states and slice 5's customization review remain unverified. No manual
  Showcase launch or Writer tests were run by the agent. Subsequent screenshot
  and cleanup record updates changed documentation only; builds/tests were not
  rerun for those updates. This checkpoint did not cover slice 6's later passes.

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
