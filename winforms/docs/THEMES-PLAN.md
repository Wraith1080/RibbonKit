# Native WinForms themes

**Architecture planning, 2026-11-10:** the user requested planning before continuing
the broader rewrite. The [theme architecture draft](THEME-ARCHITECTURE-PLAN.md)
owns the proposed boundaries and migration order. The latest Backstage extraction
and neutral-caption correction remain uncommitted drafts; further implementation
has stopped for this planning discussion. Earlier appearance requirements and
evidence below remain in place.

Status, **2026-11-10: Office 2019 review corrections, the demo Backstage toggle,
native Showcase theme selector and WPF Classic-style Office 2019 Backstage are
implemented. Scroller borders are restored at the tab-header size. Office 2019
Backstage extends its real panes behind a transparent caption; Office 2024 keeps
its separate title bar above Backstage. The 2019 title moves 75% immediately,
then slides the remaining 25% without fading on open/close. Resize centering is
immediate. The Back button is circular. Verification
is recorded below; user appearance review remains pending. Dark variants remain deferred.**

The user rejected the fade-in-place workaround. The latest implementation restores
sliding and extends the real panes behind a transparent caption foreground; see
the theme-specific title-bar section at the end of this plan.

Planning baseline: `codex/Ribbonkit-Winform` at
`0ddf85be0ca6b0f502543726f8612050b06c0991`, with a clean worktree at entry.
Recheck the live checkout before implementation. The closed geometry sequence and
its subsequent QAT/tab-header review remain in the
[geometry plan](GEOMETRY-CONSOLIDATION-PLAN.md#tab-header-overlap-correction-2026-11-05).
This plan owns theme scope, future evidence and acceptance; it does not reopen
geometry consolidation.

## Direction and sequence

| Stage | Scope | Status |
| --- | --- | --- |
| Existing baseline | Office 2024 Light, including accepted geometry and clipping behavior. | Preserve. |
| First new theme | **Office 2019 Light**: neutral grey or title-bar-colored header band, white filled selected tab/body, zero corner radius on every theme-owned element, minimal resting borders and bordered/shadowed floating popups. | Selected by the user on 2026-11-10; both header treatments are in scope. |
| Further light families | Office 2013, Office 2010 Blue and Office 2007 Blue are candidates. A reasonable progression is 2013, then 2010, then 2007 as presentation becomes more involved. | Proposed order only; select each bounded family after review of the preceding work. |
| Other light designs | Office 2024 Flat and Crystal Light can be considered separately. Crystal needs its own material/presentation contract. | Unscheduled; no commitment to port the whole WPF catalog. |
| Dark/Black variants | Revisit after the light-theme phase, including native material mode, disabled EDIT text and built-in dialog controls. | Explicitly deferred by the user. |

The selected deliverable is a native Office 2019 light presentation, not complete
WPF feature/pixel parity. Use the repository's
[Office 2019 token dictionary](../../src/RibbonKit/Themes/Tokens.Office2019.xaml)
and [2019 modernization record](../../docs/history/library/01-core-controls.md#36-2019-modernization--hover-consistency)
as references. WPF resources remain read-only, with no runtime dependency, build
or test of WPF. Native text rendering and host contracts govern WinForms acceptance.

## Foundation and identified gaps

- [RibbonTheme](../src/RibbonKit.WinForms/Rendering/RibbonTheme.cs) is immutable and
  per ribbon, containing colors, fills and logical geometry. Its catalog and
  `RibbonBuiltInTheme` contained only `Office2024Light` at planning entry; the
  implementation adds `Office2019Light` to both.
- [RibbonControl](../src/RibbonKit.WinForms/RibbonControl.cs) retains the application
  theme separately from a built-in override. Direct `Theme` assignment clears
  the override; Reset restores the application theme. Preserve this contract.
- The default [Office 2024 renderer](../src/RibbonKit.WinForms/Rendering/RibbonOffice2024Renderer.cs)
  draws tabs/File with underlines. Existing corner, margin, fill and shadow roles
  express much of 2019's flat chrome. The added header presentation and body edge
  roles now provide filled headers and a bottom-only body border.
- The neutral [RibbonRenderer](../src/RibbonKit.WinForms/Rendering/RibbonRenderer.cs)
  demonstrates filled tabs but lacks Office group dividers and joined panels.
  Replacing the default with it is not the complete 2019 presentation.
- Some Office drawing paths retain local corner values, including focus,
  KeyTips and check indicators. Every theme-owned frame and state highlight must
  honor zero radius for 2019; remove any local minimum rounding from that path.
  Setting the general radius to zero alone does not cover every drawn element.
- Designer choices come from the enum, but
  [RibbonDesignCommitter.ParseTheme](../src/RibbonKit.WinForms.Design/RibbonDesignCommitter.cs)
  explicitly accepts the existing value. Each addition needs runtime selection,
  parsing, inert preview, generated-code and Undo/Redo coverage.
- [Shared color roles](APPEARANCE-COLOR-ROLES.md) already separate body, popup,
  editor, menu heading and caption colors. Use them before adding a setting.
  The [appearance inventory](M4I2-APPEARANCE-INVENTORY.md) maps owners; historical
  counts and pending reviews there are not current theme evidence.

## First-theme visual contract

These proposed targets come from the current WPF reference. They still require
native implementation, captures and review; listed values are not acceptance.

The user supplied two WPF examples on **2026-11-10**:
[neutral title bar/header](reference/themes/office2019-light-neutral-titlebar.png)
and [colored title bar/header](reference/themes/office2019-light-colored-titlebar.png).
They define the intended flat appearance for this slice. The blue example is the
same light theme with title-bar coloring active; it is not a dark variant.

| Surface/state | Office 2019 Light target | Native boundary |
| --- | --- | --- |
| Header surround | Neutral grey `#E6E6E6` with dark text when title-bar coloring is off. When active, the chosen caption color also fills the tab band, with contrasting caption/inactive-tab text; the blue reference uses the Office accent. | Shared theme/host integration; preserve optional material composition. |
| Body and ordinary wells | White `#FFFFFF`; body background spans the form's full client width with no outer margin. Inner content retains padding around groups and commands. | Existing body/editor/popup roles and shared frame measurement; distinguish the full-width background from its padded content viewport. |
| Text | Primary `#201F1E`, secondary `#605E5C`. | Preserve authored artwork and semantic colors. |
| Accent/outlines | Accent `#2B579A`; subtle separator/field outline color `#C8C6C4`. Almost borderless resting chrome. | Keep body, popup and editor roles independent; do not apply a full frame to every command or group. |
| Commands | Hover `#DEDCDA`, pressed `#CFCDCB`, checked `#CEDCF0`. | Audit split companion/open/checked combinations. |
| Tabs | White square selected tab with dark text, continuous with the white body in both header modes. Neutral hover fill `#EDEDED`; colored-header states remain readable against the caption color. No selected outline or 2024 underline. Contextual labels remain readable in both modes. | Shared presentation covers clipping, focus and animation. |
| File/header utilities | Flat fills; neutral hover `#EDEDED` and pressed `#D8D8D8`; contrasting text/glyphs and suitable state washes on the colored band. | Separate header roles where command fills differ; retain routing/closing state. |
| Corners/body frame | Zero corner radius on every theme-owned element; edge-to-edge white body with zero outer margin, retained inner padding, no enclosing card frame and no body/QAT shadow. Retain subtle group dividers and single horizontal body/QAT separators. | Reuse zero radii, `BodyMargin = Padding.Empty` and shadow roles. Preserve suitable `BodyContentHorizontalPadding`, group/command spacing, joined QAT/messages and edge ownership. |
| Menus/galleries/tips | Light surfaces, readable paired headings/text and square chrome. Floating popups retain a visible thin border and soft shadow so they remain distinct from underlying surfaces. | Map existing roles or document deliberate fallbacks. Popup border/shadow stay independent of the minimal body frame and disabled body shadow. |
| Backstage | Light palette and square page treatment on the current native structure. | Preserve pages/motion/clipping. A Classic Backstage layout is separate work. |

Both header treatments are part of the first theme. For Office 2019, a shared,
theme-owned policy extends active title-bar coloring into the tab band. Keep the
authored `RibbonForm.TitleBarColor` value and its precedence; do not rewrite the
theme or Form properties when deriving the header colors. Disabling/resetting
coloring restores the neutral band and dark text. Switching themes must restore
each theme's header policy; Office 2024 retains its current caption behavior.
Ordinary Forms keep the neutral header unless they opt into a documented shared
host integration. No Showcase-only recoloring helper is an acceptable dependency.

Minimal borders still include the reference's fine group dividers, native editor
and gallery outlines, selection/focus feedback and body/QAT separator lines.
Avoid enclosing group boxes, a rounded ribbon card, double horizontal rules or a
heavy outline around the white selected tab. The rounded document card and status
bar shown below the ribbon are application content, not theme deliverables.

**The body background has zero outer margin; its inner section retains padding.**
The white background starts at the form client's left edge and ends at its right
edge, with no inset card, outer gutter or reserved body-shadow clearance. Groups
and commands sit within a padded content viewport, with their own internal spacing
also retained. Do not set `BodyContentHorizontalPadding` to zero merely to make
the background full width. Content measurement, clipping and hit testing must
share the padded viewport while the outer surface remains edge to edge.

**User clarification, 2026-11-10: no rounded corners on any theme-owned element.**
Use zero radius for tabs/File, commands and split halves, editor and choice frames,
gallery tiles/headings/utilities, QAT/messages, Backstage page/Back button chrome,
customization chrome, focus/hover/pressed/checked outlines, ScreenTips, KeyTips and
all popup frames. Paint paths, clipping regions and shadows must agree on the
square outline; local fixed radii must not leave residual rounding.

**Floating popups keep both borders and shadows** to prevent blending into the
window or other light surfaces. This includes dropdown/nested menus, Application
Menu, group/minimized flyouts, gallery/choice popups and ScreenTips. Their corners
remain square. Use the existing popup border/shadow roles and matching reserved
shadow space; removing the body shadow must not suppress popup separation.

Keep native font/density minima and accepted command-content bands. Use existing
metric roles for a theme's frame differences; do not copy all WPF dimensions or
tune native hit targets to a WPF screenshot. Office 2024 defaults and authored
geometry retain their existing behavior.

## Theme and rendering contract

1. Propose additive `RibbonThemes.Office2019Light` and
   `RibbonBuiltInTheme.Office2019Light`. Append the enum value without renumbering
   `Office2024Light`. Add native Unshipped API entries and XML comments; preserve
   the shipped baseline.
2. Express missing concepts on the immutable theme: header presentation mode,
   paired header state colors where needed, optional colored-caption/header
   integration policy, and body-border edge policy. Finalize
   API names, defaults, fallbacks and validation in the first implementation
   contract before runtime edits. Avoid a general theme framework or speculative
   settings for 2007.
3. Preserve public renderer types and default selection. The default Office path
   consumes those theme values through shared drawing/measurement helpers. The
   neutral renderer keeps its deliberate style; custom renderers remain authoritative.
4. Presentation survives `RibbonThemes.Office2019Light with { Accent = ... }`.
   Do not infer style from the selected enum, palette identity, name or RGB.
   Built-in themes must not require a second renderer assignment.
5. Built-in selection replaces the whole effective theme without silently
   merging custom metrics. Reset restores the exact retained application theme,
   including metrics. Direct `Theme` assignment clears the selection; ribbons
   remain independent. Invalid assignments leave both values unchanged.
6. Reuse theme invalidation: settle stale motion, dismiss transient popups and
   refresh colors on reopening. Preserve model/QAT identity, gallery selection,
   native editor values/caret/selection/handles, page instances and bindings.
7. Filled headers leave no sliding underline or stale captured header after
   selection, scrolling, switching or interrupted motion. Office 2024 keeps its
   indicator. No new animation style; preserve immediate/reduced-motion results.
8. Resolve colored caption/header background, foreground and interaction states
   in the shared library, including TitleBar/TabRow QAT and header utilities.
   Keep selected-tab/body text dark on white and inactive-header text readable
   on the chosen caption color. Toggling/resetting color or switching theme must
   refresh every affected layer without stale colors or changes to source models.
9. Apply zero corner radius consistently across theme-owned rendering, native
   clipping and state highlights. Keep floating popup borders and shadows enabled
   independently of the body's flat frame and absent shadow.

Use existing Property Grid/Edit Ribbon choices, detached preview, Apply/Cancel
and single-transaction Undo/Redo. Preserve stale-draft rejection and generated
enum assignments. Custom `Theme` stays code-authored/nonserialized. Appearance
persistence stays host-owned, outside structural customization data.

## Coverage and ownership

| Area | Required first-theme coverage |
| --- | --- |
| Commands | Button, toggle, check/radio, horizontal/vertical split; disabled, hover, pressed, checked, open and keyboard focus. |
| Headers | Ordinary/contextual tabs, File/hidden File, minimize/modal-close, scrolling and clipped text beside every QAT placement/overflow. |
| Body/groups | Classic/Simplified, expanded/minimized, expanded/collapsed groups, dividers, scrollers and Large-command alignment. |
| QAT/messages | TitleBar/TabRow/BelowRibbon, inline/overflow, joined/standalone frames, stacking and square internal joins. |
| Floating content | Ordinary/nested/illustrated menus, Application Menu, overflow, group flyouts, headings/footer/scroll regions and borrowed editors. |
| Fields | TextBox/ComboBox, labels/arrow, selection-only text, choices, disabled/focus states; retain values and handles. |
| Galleries | Inline/dropdown/QAT, selection, headings/footer/scroll utilities. Artwork and custom presenter internals remain host-owned. |
| Backstage/tips | Rail/entries/Back button/page frame/caption coverage, ScreenTips and KeyTips. Page controls retain host styling. |
| Built-in options/customization | Readable library-owned surfaces, search, lists/tree/state images, frames and action menus. List OS-drawn buttons/scrollbars separately; library-owned dialogs are not application content. |
| Hosts/material | Ordinary Form; optional RibbonForm with neutral/colored 2019 caption and tab band, caption tint, Solid/Mica/Acrylic, title-bar QAT and fallback. No native frame or dark-material expansion. |
| Designer | Selection, inert ribbon/Backstage preview, serialization, Reset, Apply/Cancel, Undo/Redo and save/reopen. Never initialize/paint application pages or execute commands for preview. |

No arbitrary palette editor, application-wide recoloring, new Backstage layout,
Glass/Aero effect, localization/RTL expansion, system-theme auto-following or
dark/Black entry is included. Preserve keyboard/automation behavior; new providers
and screen-reader compatibility remain separate.

## Implementation slices

### Initial implementation contract — 2026-11-10

Add `RibbonHeaderPresentation` (`Underline = 0`, `Filled = 1`) and theme roles
`HeaderPresentation`, nullable `HeaderHover`/`HeaderPressed`, optional
`HeaderForeground`, `ExtendTitleBarColorToHeader` and `BodyBottomBorderOnly`.
Their defaults preserve the existing Office 2024 rendering. The 2019 preset uses
filled headers, square existing radii, zero outer body margin with retained inner
padding, bottom-only frame and transparent body shadow. Popup border/shadow roles
remain active. Append `Office2019Light` to the catalog and native enum.

Resolve optional caption/header integration as paint-time theme values in the
library. Keep body/selected-tab foreground and surface separate from contrasting
inactive header/QAT text. Preserve the explicit renderer override, immutable
application theme, host caption values and current 2024 behavior. Validate header
mode before any theme assignment. The preset explicitly sets decorative radii to
zero while null role values preserve legacy default pixels.

The user also requested configurable shared settings for additional hardcoded
appearance values encountered in this work. Add nullable `FocusCornerRadius`,
`KeyTipCornerRadius`, `CheckBoxCornerRadius`, `BackstageBackButtonCornerRadius`,
`NavigationMarkerCornerRadius`, `ApplicationMenuFooterCornerRadius`,
`ApplicationMenuFileButtonCornerRadius` and `HeaderIndicatorCornerRadius`.
Explicit values are 0..64 logical pixels; null preserves each drawing owner's
previous default, including legacy device-pixel rounding. The 2019 preset sets
all eight to zero. `ColoredHeaderHover`/`ColoredHeaderPressed` independently
override caption-following state fills. Shared validation rejects invalid values
before changing theme/application selection. Existing popup, content-spacing,
frame and shadow roles remain adjustable; retain intrinsic glyph construction
and native one-device-pixel frame rules.

| Slice | Deliverable | Exit evidence |
| --- | --- | --- |
| T1: presentation/preset | Finalize the bounded API contract, implement missing shared header/frame concepts and Office 2019 Light across the coverage table. | Native API/XML build, state/render/geometry regressions, unchanged 2024 defaults and identified gaps. |
| T2: designer/consumer | Complete catalog selection, switching/custom/Reset, designer integration and a permanent-sample comparison. | Generated-code/transaction checks, package consumer, composed captures and combined user review. |
| Later light family | Select the next theme and its differences after reviewing the first. | A new bounded contract; no automatic expansion to all candidates. |

T1/T2 may share a session, verification and one permanent-sample refresh. These
divisions do not justify calling a partly covered palette a completed theme.
The user authorized starting implementation after clarifying the visual contract.

## Verification for implementation

- One agent; native projects only; normal Debug/Release outputs; preserve authored
  Forms/UserControls/resources. Use the existing native verification harnesses;
  leave actual VS review to the user, with no computer-use tools. No commit,
  push or publication.
- Use native Debug/Release builds and only relevant focused regressions for
  changed behavior. On 2026-11-10 the user explicitly stopped the broader run
  because of usage cost and requested relevant tests only. Do not restart the
  full suite or broaden validation without an explicit request. Do not run the
  parent WPF solution.
- Extend existing `DesignerThemeTests`, `ColorRoleTests`, `StateAppearanceTests`,
  `RendererBoundaryTests` and affected header/QAT/motion/menu/editor/frame checks.
  Cover new behavior, both renderer paths, Compact/Touch, Classic/Simplified,
  custom clones, invalid values and per-ribbon isolation.
- Compare changed headers/frames at synthetic 96/120/144/192 DPI. Inspect four
  corners and joins. Verify Office 2024 default pixels/measurements, including
  tab-header clipping. Cover neutral and colored 2019 headers, readable state
  colors, white selected tabs, fine separators and absence of a body card/shadow.
  Assert zero outer margin and full client-width body background while retaining
  suitable inner content padding in both header modes, densities and layouts,
  including narrow windows and joined QAT/messages.
  Verify zero rounding on theme-owned frames/highlights/tips and native clip
  regions, including paths that previously imposed a fixed or minimum radius.
  Inspect floating popup borders and shadow clearance against both white and
  grey surfaces; square corners must remain visibly separated without clipping.
  Inspect actual/diff images before changing approvals.
- Pack normal Release outputs; verify a package-only public-API consumer without
  Showcase resources/helpers. Exercise both light themes, custom/Reset, QAT,
  fields, popups and Backstage. Include ordinary Form and representative RibbonForm
  Solid/Mica/Acrylic scopes. Exercise title-bar coloring on/off, color changes,
  reset and theme switching, retaining authored Form values and Office 2024's
  current behavior. Record actual DPI and effective backdrop.
- Inspect settled composed captures for native fields, page clips, material and
  shadows; `DrawToBitmap` alone cannot establish native composition. Serialize
  desktop-dependent checks and builds sharing outputs.
- Extend generated-theme-form consumer checks. Refresh the permanent sample in
  place with [Update-DesignerSample.ps1](../eng/Update-DesignerSample.ps1), providing
  one opt-in light-theme comparison through public APIs. Preserve normal startup;
  no separate review solution or task-specific build folders.
- User review: both light themes/custom/Reset, header fills/focus, QAT joins/clipping,
  menus, fields, gallery/Backstage, neutral/colored caption and tab band, minimal
  borders, zero corner radii, visible popup borders/shadows, caption tint and
  material states. Report actual
  VS save/reopen/Undo/Redo, physical input and mixed-monitor DPI separately; unit
  and generated-code checks cannot establish those results.

Follow [CONTRIBUTING](../../CONTRIBUTING.md) for API/proportional verification,
using the independent native solution. Native commands/evidence rules are in the
[native roadmap](../../docs/21-NATIVE-WINFORMS-PLAN.md#evidence-ledger-format-and-completion-rule).
Historical failures are not presumed fixed or still reproducible: compare the
baseline if encountered and distinguish new failures. Actual implementation
results are recorded below; automated/native scopes do not establish hardware
or actual Visual Studio acceptance.

### Implementation evidence — 2026-11-10

The implementation uses the existing Office renderer and immutable theme, with
**37 additive native API entries** and no shipped-baseline changes. Header paint
themes are cached separately from the application theme; caption color/tint
changes settle stale motion and invalidate the whole affected header. Filled
tabs report no underline geometry. Body/selection colors remain independent of
contrasting inactive-header/QAT colors. A clone retains the same presentation.

Native captures exercised actual **120 DPI** with effective Mica/Acrylic confirmed.
Inspected representative Solid/Mica/Acrylic, Classic/Simplified, Compact/Touch,
colored-header, popup and Backstage images. Popup corners are square and retain
border/shadow separation; body backgrounds reach both client edges with inner
content clearance. Native page styling and retained field/page handles survived
theme/color/layout/reset changes. This is not a physical mixed-monitor check.

The first isolated material probe exposed a test-host omission: its executable
had no Windows compatibility manifest, causing layered child-editor setup to fail
with Win32 error 87. Added an explicit manifest and included `.manifest` files in
consumer preparation. The permanent sample already carried the required manifest.
The corrected material probe and complete native matrix passed. The probe now
logs unexpected UI-thread failures and stops instead of opening an error dialog;
captures require foreground ownership by the fixture process, including its
floating editor windows. Simplified cases retain Classic-created field handles
and explicitly present their menu command inline. Header contrast checks account
for the authored material tint rather than assuming white text for every backdrop.

Evidence root: `artifacts/themes/office2019/`. The final public-package evidence
is `package-verified/consumer-final/`; generated-code evidence is
`package-verified/generated-forms/`. Earlier failed probe folders remain diagnostic
records and are not counted as passing scopes. The unchanged starting revision
was extracted under `baseline-source/` solely for focused failure comparisons.

The final broader run retains six failures: material-on-open-Backstage, opaque
Backstage layer lifecycle, a customization flyout, two Simplified group-motion
cases, and the native tree double-buffer check. The first five also failed on the
unchanged starting revision in a focused comparison. The tree check passed in
fresh isolated runs on both revisions, so its aggregate-only failure remains an
unresolved verification limit. A fresh current run of the earlier failed
methods retained only the two Backstage failures. No unrelated repair or clean
aggregate result is claimed. Comparison TRX files are retained in the current and
`baseline-source/` test-result folders.

The permanent sample was refreshed in place to Debug package
`0.1.0-designer.local20261110T063424871Z`. Launch its normal Debug output with
`--office2019-theme-demo`; normal startup and authored Forms/UserControls/resources
are preserved. Compare theme choices, blue/neutral caption and tab band, Mouse/
Touch, Classic/Simplified, inner padding, popup shadow, existing galleries and
Backstage. Stop for this review before another theme.

## Current status and review ledger

| Item | Status |
| --- | --- |
| Direction | **2026-11-10: user selected Office 2019 first and light themes before dark.** |
| Planning | Appearance contract retained. **2026-11-10: broader rewrite planning requested; see [architecture draft](THEME-ARCHITECTURE-PLAN.md).** Review the uncommitted Backstage pilot before widening implementation; later family order remains provisional. |
| Runtime/API/rendering | Implemented. Initial native Debug/Release solution builds passed. Latest bounded changes built through Release runtime/tests and Debug Showcase/designer/sample with zero warnings/errors; no full solution or broad suite was repeated. |
| Designer/sample/package consumer | Refreshed normal Debug sample; independent Release package restored/built; six generated Form/RibbonForm initializers compiled and reopened successfully. |
| Focused Release verification | Latest theme-boundary/title checks: **18 focused cases passed**, followed by **5 title cases passed** after the requested 75% placement/25% slide adjustment; four title cases overlap the earlier run. Earlier structural/page-motion evidence remains below. No broad suite ran. |
| Office 2024 default parity | **32 PNG/measurement pairs unchanged**, both renderers/densities, expanded/minimized, synthetic 96/120/144/192 DPI. Rechecked for review corrections against `office2019-review/before-frames/`; default options-button PNGs also unchanged. |
| Composed native package verification | **2/2 theme-specific live-frame scopes passed** at actual 120 DPI, opening/closing, Solid: full-height panes for 2019, a separate title bar above 2024. Nonzero pane travel and retained native page state were required. No full native matrix was repeated. |
| Broader Release suite | Initial implementation: 1,485 passed / 6 failed / 1,491 total (historical evidence above). Correction run **stopped at the user's request**, 1,089 passed / 3 failed / 1,092 executed; incomplete, no full-suite result claimed. Do not restart it. |
| User appearance review | **2026-11-10: user resumed the seven fixes, added a working demo Backstage toggle and requested a native Showcase theme selector.** Corrections are implemented below; appearance is not accepted yet. Geometry acceptance stays in its own plan. |
| Actual VS/physical input/mixed-monitor | Not run for themes; report separately. |

Record later evidence and confirmation here once, with date/revision/scope,
artifact locations and limits. Keep [NEXT-SLICE-PROMPT](NEXT-SLICE-PROMPT.md) a
short pointer to this plan.

## User review follow-up — 2026-11-10

The user initially deferred these observations, then explicitly resumed the work
on 2026-11-10. Preserve the existing uncommitted review notes and screenshots.

### Correction contract

Baseline: `bbb3b3cb1bb24dd1963d6786cf41a1e462a715ee` on the current native branch.
Add only per-ribbon immutable settings with legacy-preserving defaults:

| Setting | Existing/default behavior | Office 2019 preset |
| --- | --- | --- |
| `CollapsedGroupBorder` | Empty uses `Border` on active Classic/Simplified group openers. | Transparent, matching dropdown chrome. |
| `ApplicationMenuSplitDividerVisible` | True retains the action-region divider. | False; measured action regions stay separate. |
| `OptionsButtonCornerRadius` | Null retains system painting for ordinary library options buttons and existing shared painting for accent buttons. Explicit 0..64 logical pixels uses shared theme painting. | Zero for every library-owned dialog button. Native click/default/cancel/keyboard semantics remain. |
| `MessageSideBordersVisible` | True retains complete message frames. | False; keep horizontal separators and connected-edge ownership. |
| `HeaderScrollButtonsMatchTabHeight` / `HeaderScrollButtonWidth` | False/null keeps the density width. An explicit positive width wins; otherwise opt-in matching uses the effective tab height. Existing narrow-header clamps remain. | Match height, with no explicit width. |
| `HeaderScrollButtonBackground` / `HeaderScrollButtonBorder` | With no header-scroller overrides, Null/Empty retains legacy drawing. Configured full-slot chrome uses `PopupBorder` when the border is Empty; an explicit Transparent border remains available. Body scrollers are independent. | White surface fallback and visible `PopupBorder`, restored after the user's follow-up. |
| `ApplicationMenuWorkAreaInset` | Null retains the eight-logical-pixel popup work-area inset. Explicit 0..1024 is bounded by available work area. Only Application Menu placement uses it. | Zero, allowing a maximized File menu to meet the client left edge. |

The opt-in demo resets its authored white `TitleBarColor` to Empty before showing
Office 2019; authored Form/generated resources and normal startup remain unchanged.
Use shared radius/paint helpers for normal, transfer and footer buttons across
Classic, Simplified and QAT customization, including secondary edit/group dialogs.
Reuse existing palette/state roles; do not draw fake controls over native buttons.
Header measurement, viewport clipping, hit targets and paint consume one scroller
width. Popup placement and custom-content resize consume the same menu inset.
Public additions receive XML/Unshipped entries; preserve Office 2024 pixels and
the shipped baseline. Deliver one refreshed sample and a combined review.

- [x] **Collapsed group button:** remove its border and use the same visual
  treatment as an ordinary dropdown button.
- [x] **Default demo header:** startup previously appeared fully white. With
  title-bar coloring off, the caption and tab band should start in the neutral
  grey appearance.
- [x] **Application Menu split hover:** remove the divider line between the
  primary and dropdown areas when hovered.
- [x] **Customization dialog corners:** some buttons remained rounded. Investigate
  their drawing/ownership and make the applicable appearance adjustable through
  shared settings while giving Office 2019 square corners.
- [x] **Message panel edges:** remove the side borders for the edge-to-edge
  Office 2019 presentation.
- [x] **Header scroller size:** match the tab-header size so the scrollers align
  visually with the header row.
- [x] **Maximized Application Menu:** remove the unwanted left-side margin when
  opening the menu in a maximized window.
- [x] **Demo Backstage toggle:** enter the required Backstage File mode before
  opening, support closing, and restore the prior File routing afterward so the
  Application Menu remains available. Respect canceled opening/closing.
- [x] **Native Showcase selector:** View > Theme offers Office 2019 Light,
  Office 2024 Light and Application theme through the public `BuiltInTheme`
  property. Keep radio state synchronized and save/restore the optional choice
  in the application's existing preferences (older files keep the default).

Checked items mean implemented and automatically verified, pending user visual
acceptance. All nine new shared theme roles preserve the legacy defaults; the
Office 2019 preset opts into its flat presentation. Ordinary options buttons
remain actual WinForms Buttons, with shared theme painting only when their radius
is configured. All library-owned primary, footer, transfer and secondary-dialog
buttons consume that setting. Application-owned page controls retain their styling.

Saved user references:
[startup and collapsed groups](reference/themes/office2019-review-20261110-startup.png),
[Application Menu split](reference/themes/office2019-review-20261110-menu-split.png),
[customization buttons](reference/themes/office2019-review-20261110-customize.png),
[header scrollers](reference/themes/office2019-review-20261110-header-scrollers.png),
[maximized menu margin](reference/themes/office2019-review-20261110-maximized-menu.png).

### Correction verification — 2026-11-10

The focused before run reproduced ten failing cases and passed two baseline
cases (`office2019-review-before.trx`). The final focused run passed **60/60**
(`office2019-review-focused.trx`), covering the review fixes, explicit scroller
width/DPI/Touch behavior, per-ribbon settings and rejected geometry, theme switching,
designer behavior and Showcase preference compatibility. Office 2024's **32 frame
PNG/JSON pairs** and default options-button PNGs match the captured pre-change
pixels. Synthetic geometry/paint covers 96/120/144/192 DPI; this does not substitute
for mixed-monitor review.

Independent Release package verification passed **16/16 actual native scopes at
120 DPI**: ordinary Form/RibbonForm, both densities/layouts, Solid/Mica/Acrylic
where applicable, theme/color/reset switching and retained editor/page handles.
The four solid RibbonForm cases additionally check square customization pages,
tab-sized header scrollers, messages/groups and maximized Application Menu left
alignment. Captures of Classic/Simplified customization, the header/message/group
surface and maximized menu were inspected. Evidence is under
`artifacts/themes/office2019-review/native-final/`; the independent package/cache
is in `package-consumer/`. No Showcase resource/helper is required by those cases.

The actual Showcase selector/save/startup-restore probe passed for all three
choices without showing windows or modifying user preferences
(`showcase-selector-final/result.log`). The permanent sample's actual demo probe
passed neutral startup, toggle open/close, Back/Escape-equivalent close,
canceled opening/closing, original File routing and native-page identity
(`designer-demo/result.log`). Its normal Debug package was refreshed once to
`0.1.0-designer.local20261110T095457522Z`. Authored Forms/UserControls/resources
were preserved. Native Debug/Release solution builds and the final Showcase
Debug/Release builds passed with zero warnings/errors.

The broader correction run was stopped immediately when the user requested only
relevant tests to conserve usage. Its incomplete TRX is
`office2019-review-full.trx` (1,089 passed, three failed, 1,092 executed). The
failures were the customization-context flyout, enabling material on open
Backstage, and the TreeView double-buffer/expander case, all also listed in the
earlier implementation evidence. No fresh baseline comparison or additional
tests were run after that request. The completed focused and native results
above remain the evidence for this correction slice.

Review the refreshed Designer Sample with `--office2019-theme-demo`, including
the seven reported surfaces and Toggle Backstage. Review the native Showcase's
View > Theme selector. Actual VS designer, physical input and mixed-monitor
acceptance remain pending. Do not begin another theme until this review.

## Office 2019 Backstage and scroller follow-up — 2026-11-10

The user requested restoring visible header-scroller borders while keeping the
tab-height-sized slots, then explicitly selected the WPF Office 2019 Backstage
as the reference. They also required both the 2019 and 2024 Backstage layouts to
cover the complete integrated title bar and center the window title horizontally.
The later theme-specific correction below supersedes that both-layout title-bar
request. This section also supersedes the earlier borderless-scroller appearance.

The reference is WPF Showcase's Office 2019 -> Classic pairing in
`samples/RibbonKit.Showcase/MainWindow.xaml.cs`, the Classic template in
`src/RibbonKit/Themes/Controls.Backstage.xaml`, and its Office 2019 tokens/accent
overrides. WPF was read only; no WPF code, build or test was changed/run.

- Office 2019 selects an Accent-colored rail, white normal/selected navigation
  ink and icon silhouettes, full-width square hover/selected fills, and no Modern
  selection marker. Hover and selection follow the accent's WPF white/black mixes
  unless explicitly overridden. The library masks rich vector/raster icons at the
  current device size; hosts do not need a special renderer or icon helper.
- Auto width starts at 220 logical pixels, with icon inset 26, icon size 16,
  icon/text gap 12, Back size 36 and inset 20, gap below Back 24, and footer-bottom
  clearance 14. Existing authored width/minimum/maximum constraints, long-label
  Auto growth, Touch target minima, narrow-host constraints and short-host inline
  scrolling remain effective. The user subsequently requested a circular Back
  control, which is now the explicit exception to square 2019 styling. Host page
  controls retain their own styling and identity.
- The 2019 page has zero top/left border and radius. Navigation state fills reach
  their measured row edges. Modern 2024 keeps its existing rail, inset states,
  indicator and page frame. Neutral customization/options rails remain neutral.
- While Backstage is open, each pane's background continues into its part of the
  integrated caption: navigation color above navigation, `Surface` above the page.
  The split follows the live navigation/page boundary, including slide/fade motion.
  Symmetric reservations center the title on the window while protecting caption
  controls. The navigation portion uses navigation ink; the page portion uses the
  normal content ink. Closing restores ordinary title placement and the authored
  title-bar color. QAT suppression and native page ownership remain unchanged.
- Scrollers retain the measured tab-height width/height and full-slot painting.
  Their border now falls back to the shared `PopupBorder` in resting, hover and
  pressed states. Explicit per-ribbon border overrides still work.

New immutable per-ribbon settings are `BackstageNavigationUsesAccent`,
`BackstageFullWidthNavigation`, `BackstageNavigationForeground`,
`BackstageSelectedForeground`, `BackstageDisabledForeground`,
`BackstageNavigationHover`, `BackstageNavigationSelected`,
`BackstageSelectionIndicatorVisible`, `BackstageMonochromeIcons`,
`BackstageNavigationIconInset`, `BackstageNavigationIconSize`,
`BackstageAutoMinimumNavigationWidth`, `BackstageFooterBottomPadding`,
`BackstagePageBorderWidth` and `BackstageTitleCentered`. All have XML comments
and Unshipped accessors; the shipped API baseline is unchanged. Default values
retain Modern presentation except the intentionally centered Backstage title,
which defaults to true for both light layouts.

Verification was limited to the affected behavior: six scroller size/border/DPI/
override cases (`office2019-scroller-border.trx`) and 24 new/existing Backstage,
caption-motion and footer cases (`office2019-backstage-focused.trx`) passed.
Subsequent changes to icon silhouettes/Back focus and explicit neutral page borders
were checked only through the corresponding four-case and one-case filters
(`office2019-backstage-silhouette.trx`, `office2019-backstage-overrides.trx`).
Synthetic layout/paint includes 96/120/144/192 DPI. No broad suite was restarted.

The permanent sample was refreshed to normal Debug package
`0.1.0-designer.local20261110T102403592Z`; authored Forms/resources were preserved.
Its public-package probe passed only the two selected Solid RibbonForm scopes:
Compact/Classic and Touch/Simplified, actual 120 DPI. Retained native page/editor
handles/text, 2019/2024 title captures and scroller captures are in
`artifacts/themes/office2019-backstage/{compact-classic,touch-simplified}/`.
Both Backstage titles and the restored scroller borders were visually inspected.
The fixture deliberately uses a custom purple accent and a yellow native page;
the library retains application styling. No new all-material, mixed-monitor,
physical-input or actual VS acceptance is claimed.

Review using the existing Designer Sample `--office2019-theme-demo` and Toggle
Backstage, or native Showcase View > Theme then File. Check both light themes'
title bars, Classic rail/footer behavior and scroller borders. User acceptance
is pending; further testing stays bounded to changed behavior or a reported issue.

### Caption background clarification and circular Back — 2026-11-10

The user clarified that extending Backstage into the title bar means extending
both pane backgrounds upwards, giving each pane a full-height appearance. The
earlier solid-navigation-colored caption interpretation is superseded. Both light
layouts now use the shared navigation color above the rail and `Surface` above
the page, split at the actual displayed navigation edge. Page-side caption opacity
tracks page motion independently of transparent navigation coverage. Application-
owned controls may have their own backgrounds; these are preserved, while the
library pane/caption uses its shared `Surface` color.

Title and caption-button painting is clipped separately to the navigation, page
and unrevealed caption regions. Centering is unchanged; a title crossing the pane
boundary retains the appropriate ink on each side. The Back control again uses
its existing circular radius fallback. `BackstageBackButtonCornerRadius` remains
available for explicit per-ribbon overrides; all other square 2019 styling remains.
No new public API was needed for this correction.

Only **14 relevant Release cases** ran and passed (`backstage-pane-caption.trx`):
both light themes' two-color caption backgrounds and centered/cross-boundary title
ink, four renderer/DPI circular-Back/state cases, caption slide/fade with material
coverage, and colored-caption restore cutouts. A single Solid Compact/Classic
package-only RibbonForm case passed at actual 120 DPI; composed 2019/2024 captures
were inspected under `artifacts/themes/backstage-pane-caption/`. Native page/editor
handles and text were retained. No broad test suite or full native matrix ran.

The Showcase Debug build and refreshed permanent Designer Sample passed with zero
warnings/errors. Current local Debug package:
`0.1.0-designer.local20261110T105448575Z`. Authored Forms/resources remain intact.
User visual review remains pending.

## Backstage motion synchronization — 2026-11-10

**Superseded: the user rejected this fade-in-place workaround. Its tests are
historical evidence only; the structural implementation below is current.**

The user's `20261110-1057-08.1281332.mp4` recording shows the two-tone caption and
the native surface's navigation/page boundary arriving in different compositor
frames. Native-resolution extraction found up to **40 recorded pixels** of
horizontal separation during closing (around 1.567 seconds). Settled screenshots
and progress-state tests did not establish real presentation synchronization.
Extracted frames and measurement/contact-sheet scripts are retained locally under
`artifacts/backstage-sync/`.

The integrated full-screen layout now keeps its columns anchored and fades in
place. Ordinary hosts without integrated captions retain their prior 15% slide.
Each motion step paints the native surface and caption before publishing pane
opacity. The existing native page, controls, focus/validation, reduced-motion
policy and lifecycle remain in place; no screenshot proxy or content cloning was
introduced. Centered titles, per-pane caption backgrounds and circular Back controls
remain unchanged.

The new immutable shared `BackstageSlideDistanceRatio` is a nullable finite width
fraction from 0 to 1. Null selects the host-aware policy; zero requests stationary
fading; an explicit positive value opts into travel. XML documentation and two
Unshipped accessors were added; the shipped API baseline was not edited.

Only **12 focused Release cases** ran and passed (`backstage-synchronization.trx`):
both light presets' anchored opening/closing, actual native layer alpha, retained
handles/parent/text, per-ribbon ratio overrides/invalid values, existing ordinary-
host sliding, and caption/material tint behavior. No broad suite was restarted.

The permanent package-only sample's `--verify-backstage-sync <output>` checks
actual on-screen transition frames, with foreground ownership guarded. It warms
native page/font painting and requires measurable frames from both opening and
closing; nearly transparent low-contrast frames are not counted as measurable.
The final two Solid scopes at actual 120 DPI captured **26 frames**, of which
**17** exposed measurable caption/body edges: Office 2019 maximum delta **0 px**,
Office 2024 maximum **1 px** (its existing page border). Both themes retained native
page handles, parent and text. Raw PNGs, per-phase frame logs and scope summaries
are in `artifacts/backstage-sync/after-warm-final/`; the contact sheet was inspected.
An earlier foreground-guard stop and cold first-opening capture were corrected in
the diagnostic; neither is claimed as complete opening/closing evidence.

The Showcase Debug build and refreshed Designer Sample passed with zero warnings/
errors. Current local Debug package: `0.1.0-designer.local20261110T110839226Z`.
Authored Forms/resources were preserved. These are focused state checks plus
sampled real Solid frames on this display, not an all-hardware/material timing
claim. User review of the original reported transition remains pending.

## Real full-height panes behind a transparent caption — 2026-11-10

**The structure below is retained for Office 2019. The later theme-specific
correction restores Office 2024's separate title bar; both-theme results here are
historical evidence for the earlier scope.**

The user explicitly rejected the fade and separate colored-caption interpretation
in `20261110-1115-26.5858712.mp4`. They requested restoring slide motion and moving
the real navbar/content pane behind a transparent title bar with centered text.
The recording was inspected; its frame sheet is retained in
`artifacts/backstage-sync/second-recording/`.

Backstage now occupies the complete host client rectangle starting at Y=0.
The real page panel and rail extend into the caption area. Back/navigation items
and the native content viewport reserve the caption's measured height, so content
does not overlap window controls. Page-switch animation is clipped by that content
viewport; page controls keep their native handles, local layout and persistent
parent throughout animation. Existing width, footer, DPI and validation policies
remain in effect.

The title bar is a click-through, nonactivating foreground plane owned by the
library: only the application icon, centered title and caption controls are drawn.
Its background pixels are transparent. It reuses the shared caption painting and
real window hit testing; the full-height page/root forward caption hits to the
window for dragging, resizing and caption controls. Hover/activation/resize/theme
updates refresh the foreground, and closing/disposal releases it. A selected
32-bit DIB/DC preserves the actual caption font and glyph alpha. This is drawn
foreground chrome, not a replay/screenshot of page content.

The old caption-color animation was removed. The live full-height panes supply
their own backgrounds and slide together, with the caption foreground fixed above
them. Default travel is restored to 15% for all hosts. The existing shared
`BackstageSlideDistanceRatio` retains its nullable/0..1 contract: null now means
15% travel; zero is an explicit stationary-fade choice. No additional public API
was introduced in this structural correction.

Focused Release evidence: **13** caption/structure/ownership/hit-test cases passed
in `backstage-real-panes-caption.trx`; **23** page selection/interruption/motion
cases passed in `backstage-fullheight-page-motion.trx`. A subsequent four-case
filter (`backstage-transparent-caption-rendering.trx`) verified foreground alpha,
centered title ink and native slide after correcting the caption font's drawing
DC. Tests assert real full-height controls, visible nonzero slide travel, a
transparent foreground, retained native pages, QAT policy and caption hit routing.
They do not accept the rejected anchored-fade behavior. No broad suite ran.

The package-only real-time probe was updated to require both full-height pane
geometry and actual movement in opening and closing. At actual 120 DPI, both
Solid light themes passed: **20 captured frames**, **13 measurable edge pairs**,
maximum caption/body boundary delta **0 px** in each theme. Native page handles,
parents and text were retained. The final screenshots and per-phase logs are under
`artifacts/backstage-real-panes/final/`; title/font, moving pane edges and the
transparent caption were visually inspected. No all-material/hardware claim is
made from those two display scopes.

Normal Debug Showcase and permanent Designer Sample builds passed without warnings
or errors. Local Debug package for that structural check: `0.1.0-designer.local20261110T113741498Z`.
Authored Forms/resources were preserved. Review the restored slide in the existing
sample and Showcase; user appearance/input acceptance remains pending.

## Theme-specific title bars and short title slide — 2026-11-10

The user clarified that Office 2024 must keep a title bar above Backstage. This
supersedes the earlier request to extend both light layouts into the caption.
`BackstageExtendsIntoTitleBar` is now an independent shared theme setting:
false by default, true for Office 2019. The actual 2019 panes still extend to Y=0
behind the transparent foreground; 2024's real surface starts below its caption,
with its existing inset page treatment. `BackstageTitleCentered` is enabled by
2019 only; 2024 retains ordinary title placement and has no title-motion overlay.
Switching themes while open updates both bounds and foreground ownership without
recreating native page content. Either setting can be overridden per theme.

The user also requested the title follow Backstage's short-slide presentation:
place it 75% toward the destination immediately, then slide the final 25% on both
opening and closing, with no text opacity animation. The title reuses Backstage's
duration and ease-out cubic curve. Nullable `BackstageTitleSlideDistanceRatio`
configures the animated fraction (null = .25, zero = snap, one = full distance);
nonfinite/out-of-range values are rejected before assignment. Reduced motion,
AnimationLevel.None and forced/immediate closes commit immediately. Resize,
maximize and restore remeasure and recenter immediately without title animation.

Only **18 focused Release cases** ran for caption boundaries, resize, opening,
closing, reversal, QAT/native-page retention and shared overrides
(`backstage-theme-caption-boundaries.trx`). After the short-slide adjustment,
only the **5 title cases** reran (`backstage-title-short-slide.trx`), including
75% placement in both directions, opaque ink, configurable travel and policy.
Those counts overlap; they are not 23 distinct cases. No broad suite ran.

The refreshed package-only probe passed **2/2 Solid scopes** at actual 120 DPI.
Office 2019 captured eight opening/closing frames with seven measurable pane-edge
pairs, maximum delta **0 px**. Office 2024 captured eight frames and preserved
its ordinary colored title bar above the moving Backstage. Both retained native
handles, parents and text. Screenshots were inspected. Evidence is under
`artifacts/backstage-theme-boundaries/native-body-row/`. An earlier diagnostic
mistook an underlying selected-tab edge revealed by fading for the body pane edge;
that failed evidence is retained under `native/`. The comparison row now sits
below the ribbon so it measures the real pane edge against a uniform underlay.

Normal Debug Showcase and permanent Designer Sample builds passed with zero
warnings/errors. Current local Debug package:
`0.1.0-designer.local20261110T144252552Z`. Authored Forms/resources remain untouched.
User appearance/input review and actual VS review remain pending; no all-material,
mixed-monitor or all-hardware timing claim is made from these two display scopes.
