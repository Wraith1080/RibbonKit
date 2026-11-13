# Native WinForms themes

**Architecture B3 implemented, 2026-11-11:** Backstage caption and visual motion
choices now resolve through the same registration as its sibling layout/drawing
helpers. Existing clocks, lifecycle and measured geometry are preserved. The
[architecture plan](THEME-ARCHITECTURE-PLAN.md) owns later boundaries and migration
order; [B3 evidence](#backstage-caption-and-motion-policy-b3--2026-11-11) is below.
Existing appearance requirements and review scope remain in the ledger.

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
| Planning | B1–B3, C1–C2, D1a–D1c, D2–D3, D4a–D4c, D5a–D5b, D6, D7a–D7f and audit corrections A00–A04/A06–A09/A11–A17 are implemented. Next proposed is D8 caption/title appearance: source-review A05 tint settings and the deferred Backstage closing-title ink/clip issue. A10 page-layout preference remains separate. Later family order is provisional. |
| Earlier-slice adjustability audit | **2026-11-11: A11–A16 header/shared effects implemented and verified.** Evidence is in the [header/shared record](#header-and-shared-effect-overrides--2026-11-11). **2026-11-12: A17 badge placement followed in D6.** The [audit](THEME-EFFECT-ADJUSTABILITY-AUDIT.md) retains A05/A10 as separate candidates. |
| Runtime/API/rendering | D7f square follow-up fixes label-dependent icon-picker highlights. None and artwork now use one square native icon cell, selection/focus rectangle and content clearance. Artwork size resizes both cells; inset constrains both. Existing items/handles/selection/names/tooltips/scrolling and group validation/OK/Cancel remain. D8 caption/title is next after review. |
| Designer/sample/package consumer | Normal Debug runtime/designer/client/sample builds pass without warnings/errors; package `0.1.0-designer.local20261113T034156273Z` refreshes the permanent sample. Theme effects 10 retains its three picker groups; Smaller/Larger picker tiles now demonstrates the shared sizing. Tabs 1–9 retain seven/five/four/seven/six/three/eight/five/six groups. Authored Forms/UserControls/resources/project files remain intact. |
| Focused Release verification | Square follow-up: **20 distinct latest relevant cases pass**, combining the initial 14 passing cases with six corrected native cases. Coverage includes shared None/artwork pixels, size/inset/extreme clamps, four-DPI paint/metrics, 9/14-point native grids, wrapping/scrolling/queued clicks, in-process accessible names, handles/items/selection and group OK/Cancel. Fixture corrections and the interrupted synchronous-click probe are in the [follow-up](#group-icon-picker-square-geometry-follow-up--2026-11-13). No broad suite ran. |
| Two-family paint/geometry | The square correction intentionally changes D7f default geometry; its original 16 byte comparisons remain historical. Emulated paint/metrics cover 96/120/144/192 DPI; native font/grid/hit/name tests run at 96 DPI and package scopes at actual 120 DPI. Matched None/artwork captures at small/large sizes and multiple insets were inspected in both light themes. No physical mixed-monitor matrix ran. |
| Composed native package verification | Square follow-up: **4/4 public-package scopes plus review-tab checks pass**, both light families with None/Mica owners at actual 120 DPI, each Classic/Simplified x Compact/Touch. Actual None/artwork selections remain square and adapt at sizes 16/48 and insets 0/4; native targets, metadata, deduplication, ownership, reset independence, validation and draft OK/Cancel pass. These are offscreen checks; live/native acceptance remains separate. |
| Backstage review correction | **2026-11-11: navbar blank-frame and left rounded-corner black-bar fixes implemented and verified; user review pending.** Current focused/package evidence is in the [page-motion follow-up](BACKSTAGE-PAGE-MOTION.md#navbar-blank-frame-and-rounded-edge-follow-up--2026-11-11). |
| Deferred title review issue | **2026-11-11: user reported title-text flicker while closing Backstage and allowed deferral to title theming work.** Their `20261111-0345-01.1714250.mp4` confirms the Office 2019 close at approximately **7.03–7.37 s**: the title moves left, its leading text becomes pale/white over the lightening caption, then dark ink returns. Native-rate frames and timestamps are retained under `artifacts/backstage-title-flicker/recording/`. Caption foreground contrast/clip handoff as the navigation fades is the source-inspection lead, not a verified root cause. Preserve opaque, legible title ink throughout closing when addressing it. No title runtime change or new build/test was made for this recording review; the page/corner correction does not address it. |
| Broader Release suite | Initial implementation: 1,485 passed / 6 failed / 1,491 total (historical evidence above). Correction run **stopped at the user's request**, 1,089 passed / 3 failed / 1,092 executed; incomplete, no full-suite result claimed. Do not restart it. |
| User appearance review | **2026-11-11: user reported the delivered C2 body/connected frames look good and authorized continuation.** Earlier A11–A16/A06–A09/A00–A04/B1 acceptance is retained at its recorded scope. The user declared Theme effects 2 full; D1a/D1b review controls remain on Theme effects 3. **2026-11-12: user reported D1b looks good and authorized continuation to native editor appearance.** D1a review remains at its earlier recorded scope. The user requested a new review tab for D1c; use Theme effects 4 and preserve tabs 1–3. **2026-11-12: user reported D2 works perfectly and authorized continuation to ordinary menu decoration.** **2026-11-12: user reported D3 looks good and authorized Application Menu continuation, with new review controls on Theme effects 5.** **2026-11-12: user reported D4a Application Menu state effects look good on Theme effects 5.** **2026-11-12: user confirmed every D4b surface/heading option works and authorized continuation.** **2026-11-12: user reported D4c looks good and everything works, authorized gallery continuation and requested its controls on a new Theme effects 6 tab.** **2026-11-12: user reported everything works for D5a and authorized continuation; then declared tab 6 full and requested D5b controls on a new tab. D5b controls remain on Theme effects 7.** **2026-11-12: user reported everything checks out for D5b and authorized continuation to D6. D6 controls remain on Theme effects 7.** **2026-11-12: user reported everything works well for D6 and authorized continuation. D7a controls remain on Theme effects 7.** **2026-11-12: user confirmed all D7a options work, including combined use, authorized continuation and requested D7b on a new tab.** **2026-11-13: user reported D7b works perfectly and authorized continuation.** **2026-11-13: user confirmed everything works for D7c, authorized continuation and requested the next controls on a new Theme effects 9 tab.** **2026-11-13: user confirmed everything works for D7d and authorized continuation.** **2026-11-13: user confirmed everything works for D7e, authorized continuation and requested the next controls on a new Theme effects 10 tab.** **2026-11-13: user reported mismatched None/artwork selection shapes and requested shared square geometry that follows inset/size changes. The correction is implemented and awaits review on Theme effects 10.** Actual VS, physical input, mixed-monitor and all-material gates remain separate. Geometry acceptance stays in its own plan. |
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

## Backstage registration B1 — 2026-11-10

After the architecture discussion, the user authorized its first implementation
slice and asked whether geometry consolidation remained useful. It remains the
shared measurement/override foundation. This slice changes selection ownership,
with no geometry algorithm, family drawing, caption or motion calculation change.
The closed geometry sequence and its acceptance are not reopened.

`RibbonPresentationResolver` now registers Modern's 2024 layout/drawing helpers
and Classic's 2019 helpers as immutable pairs. The layout base no longer chooses
concrete Office classes; host bounds, measured layout and metrics use the same
mapping as the renderer's existing drawing/frame/caption adapter. There is one
style switch. Helpers are stateless and can be shared; native controls/motion
remain ribbon/window-owned and mutable drawing caches remain renderer-owned.
Public APIs, application/Reset semantics and explicit renderer precedence are
unchanged. Standalone measurement still defaults to Modern.

Regression coverage adds mixed style/coverage settings with open native pages,
actual surface dispatch to application background/entry overrides, custom
border/radius policy and same-renderer reassignment, explicit flat/default
renderer switching, and candidate-theme preview geometry/painting. Hidden and
disabled `ContentType` preview coverage now exercises both built-ins. Preview
checks retain the authored live theme and create no native page handles/content.

Only the selected Release cases ran:

- `BackstageStyleIsolationTests`, the 2024 caption-boundary/switching case, the
  two-family real-pane/handle case, application theme/clone/Reset ownership,
  candidate-theme preview pairing and no-content-creation preview cases.
- `artifacts/theme-registration/tests/backstage-registration.trx`: 17 passed,
  two new mixed-settings cases failed an incorrect reference-identity assertion.
  Existing `Theme` semantics can retain an equal effective instance while
  recording the newly assigned application theme. The assertion now checks
  effective value equality and application-theme identity; runtime behavior was
  preserved. The two-case `backstage-registration-mixed-settings.trx` follow-up
  passed. A missing namespace import in the new preview test had been corrected
  before this run executed.
- Review strengthened the custom-renderer check to require callbacks from the
  actual native surface. Only those two cases reran and passed in
  `backstage-registration-native-hooks.trx`. These overlap the first run:
  **19 distinct relevant cases have passing latest results, not 21 or 23.**

The permanent Designer Sample was refreshed once through the normal Debug
workflow to `0.1.0-designer.local20261110T152350085Z`. Runtime/designer server,
designer client and package-only sample builds passed with zero warnings/errors;
the affected Release runtime/test build also passed. Authored sample code,
Forms/UserControls/resources and project files were not changed. No whole-solution
build, broad suite, full native matrix or WPF build/test ran.

The existing package-only `--verify-backstage-sync` probe passed **2/2 Solid
scopes at actual 120 DPI** using that refreshed package. Office 2019 captured
eight opening/closing frames with seven measurable pane-edge pairs, maximum
caption/body delta **0 px**. Office 2024 captured nine transition frames and
verified its separate caption's grey tint/reveal plus settled open and restored
closed colors. Both required slide motion and retained native page handles,
parent and text. Evidence is in `artifacts/theme-registration/native/`.
Representative opening frames and 2024's settled open/closed captions were
visually inspected. These are bounded results on this display; current human
review is in the ledger. Actual VS, physical input, mixed-monitor and all-material
acceptance are not established by this probe.

B1's automated/package implementation record ends here. Subsequent layout-input
work is recorded below; the architecture plan owns the remaining slice sequence.

## Explicit Backstage layout inputs B2 — 2026-11-11

B2 began on `codex/Ribbonkit-Winform` at `4cf1501` (`B1 Rewrite`), with a clean
tree. The shared Backstage layout now accepts a value-only input containing
resolved metrics, DPI/font height, authored width limits, maximum visible/revealed
label width, main/footer row and separator counts, caption reservation and the
active renderer's clamped material shadow clearance. It no longer reads live
models, themes, fonts, windows or renderers, and does not select a family.

The measurement adapter captures those facts once through B1's registration.
Each sibling layout supplies an immutable defaults record; resolved metrics hold
only values. Modern's default footer clearance still follows the measured,
clamped navigation inset; Classic keeps its independent 14-logical-pixel
clearance. Explicit footer overrides, Touch minima, empty-label reservations,
Auto/explicit widths, font growth, narrow/short-host fallback and DPI rounding
retain their prior meaning.

Host bounds are calculated by the ribbon's window adapter. Page padding is
resolved into the layout snapshot and shared by native content placement,
top-left clipping and both inert designer preview routes. Application renderer
border/radius hooks and Modern's one-device-pixel default border remain intact.
No public API, catalog, command/input/validation, authored page state, drawing
style, caption policy or transition timing was changed.

Relevant Release verification is under `artifacts/theme-layout-inputs/tests/`:

- Before editing runtime code, the existing default measurement/paint case
  captured a fresh baseline. The final comparison preserves **32 exact PNG/JSON
  pairs**, neutral/Office renderers, Compact/Touch, 96/120/144/192 DPI and two font
  sizes. Files are in `before-defaults/` and `after-defaults/` beside `tests/`.
- `backstage-layout-inputs.trx`: 51 passed; one existing native-page clip test
  inspected an obsolete parent hop. The committed source already attached the
  clip to `BackstageSurface.PageArea`, above the content viewport. The test now
  inspects that actual owner, retaining its radius and handle/state assertions;
  only that case reran and passed in `backstage-clip-owner.trx`. Runtime clipping
  was not changed to satisfy this assertion.
- Final snapshot page-padding integration passed 22 selected cases in
  `backstage-page-padding.trx`, including the exact baseline comparison, native
  retention/validation, custom frame hooks and inert preview bounds. These runs
  overlap: **54 distinct relevant cases have passing latest results**. Ten new
  input cases cover detached measurements surviving model/font disposal,
  revealed footer labels/separators, clamped/explicit footer clearance, empty
  captions and live application shadow hooks. No broad suite ran.

Affected Release runtime/tests and normal Debug runtime/designer server/client/
sample builds passed with zero warnings/errors. The final permanent sample
package is `0.1.0-designer.local20261110T230634876Z`; an intermediate package was
replaced after the final page-padding integration. Authored Forms/UserControls,
resources, sample code and project files were preserved. No full solution,
WPF, full native matrix, physical input or actual VS verification ran.

The refreshed public-package probe passed **2/2 Solid scopes at actual 120 DPI**.
Each theme captured eight opening/closing frames. Office 2019 had seven measurable
pane-edge pairs with maximum caption/body delta **0 px**; Office 2024 retained
its separate title bar, animated grey tint/reveal and settled close restoration.
Both required actual pane travel and retained native handles, parents and text.
Representative opening frames and the restored 2024 caption were inspected.
Evidence is under `artifacts/theme-layout-inputs/native/`. This is bounded display
evidence; current human review and remaining hardware/VS gates are in the ledger.

B2's focused automated/package implementation record ends here. Caption/motion
policy extraction is recorded below; the architecture plan owns the next slice.

## Backstage caption and motion policy B3 — 2026-11-11

B3 began with a clean tree on `codex/Ribbonkit-Winform` at `bf7b202`
(`B2  Rewrite Done`). Each Backstage registration now includes immutable caption
and motion profiles alongside its sibling layout/drawing helpers. Profiles
resolve the existing theme flags, colors, ratios and animation level into
policies; the window adapter supplies live caption/color/material/pane facts.
Caption painting no longer lives in either the renderer base or the 2024 renderer,
and the policies retain no ribbon, Form or native-control references.

Modern alone supplies the neutral separate-caption tint. The authored Form color
still precedes theme color; either suppresses that tint. Full-height panes supply
their own background. Title coverage and centering remain independent theme flags,
including custom settings opposite to a built-in's defaults. Ordinary Forms keep
their caption-free fallback. Tint reveal/alpha still follow actual pane geometry
and opacity, including material navigation coverage and close restoration.

The three existing pane/title/page engines execute captured visual policies.
The existing pane/title 220/308 ms durations, 15% pane travel, 25% title travel,
cubic travel/title easing and smooth-step pane fade remain unchanged. Native-page
transitions retain 130/182 ms, 8/12 logical-pixel travel and cubic easing.
`TabContentAnimation`, reduced motion, validation, interruption/reversal,
ownership/disposal, opaque title ink and immediate resize/maximize/restore
centering are preserved. No clocks, native services, B2 geometry or public APIs
were replaced.

Verification stayed bounded:

- **75 relevant Release cases passed** in
  `artifacts/theme-caption-motion-policy/tests/backstage-caption-motion-policy.trx`.
  They cover neutral/authored/material captions, title placement/opacity/reversal,
  QAT state, ratio validation, page motion, cancellation and retained native state.
  Ten new cases check observable quarter-progress pane/title curves in both
  directions, mixed family/coverage settings, ordinary Forms and both families'
  Subtle/Expressive page duration/travel/fade. No broad suite ran.
- Normal Debug runtime/designer server/client/sample builds and the affected
  Release runtime/test build passed with zero warnings/errors. The permanent
  sample was refreshed to `0.1.0-designer.local20261110T232648658Z`. Only its
  focused public-package diagnostic was extended; authored Forms/UserControls,
  resources, project files and Showcase code were preserved.
- The package-only diagnostic passed **2/2 Solid scopes at actual 120 DPI**.
  Office 2019 captured seven opening/closing frames with seven measurable pane-edge
  pairs, maximum caption/body delta **0 px**. Office 2024 captured nine transition
  frames and verified its separate grey-caption reveal/open/close restoration.
  Each scope additionally captured nine policy states: Form-over-theme and
  theme-only caption colors, shrink/grow resize, pending-close reversal and a
  final close raised exactly once. Native handles/parents/text were retained.
  Files are under `artifacts/theme-caption-motion-policy/native/`; representative
  authored-caption, resized and reversal/restoration captures were inspected.

These results establish B3's focused automated/package scope. Human appearance,
actual VS, physical input, mixed-monitor and all-material acceptance remain in
the ledger. No full solution, full native matrix or WPF build/test ran. C1 header
presentation is next; body work and further theme families remain separate.

## Header presentation C1 — 2026-11-11

C1 began with a clean tree on `codex/Ribbonkit-Winform` at `6162571`
(`Backstage Rewrite fix`). The user's continuation authorized header extraction;
their follow-up requires fixed visual effects to become adjustable theme settings
during the rewrite. Existing appearance remains the default, and the closing-title
issue stays deferred in the ledger.

`RibbonPresentationResolver.Header` pairs sibling
[Filled/2019](../src/RibbonKit.WinForms/Rendering/RibbonHeader2019Renderer.cs) and
[Underline/2024](../src/RibbonKit.WinForms/Rendering/RibbonHeader2024Renderer.cs)
drawing with explicit immutable geometry defaults. Selection uses only the
candidate theme's `HeaderPresentation`, independent of palette, built-in identity,
corner radius and caption extension. Neither sibling inherits the other; the
base declares drawing contracts without a style recipe. The public
`RibbonOffice2024Renderer` remains the compatible facade, including inherited
application subclasses. Explicit flat renderers retain their existing behavior.

Resolved [header metrics](../src/RibbonKit.WinForms/Layout/RibbonHeaderMetrics.cs)
contain numeric values, not a theme, ribbon or window reference. The existing
strip engine retains text measurement, tab/File target sizes, QAT reservations,
Touch minima, narrow-host clamps, overflow and clipping. Both registered styles
currently share their explicit strip defaults. Public owner text/focus/indicator
hooks, contextual/disabled states, open File handling and candidate previews
remain on the actual application renderer. Colored caption extension stays an
independent existing setting; no caption/title or motion engine was rewritten.

Six additive public `RibbonTheme` settings implement the user's effects request:
`HeaderSelectedBackground`, `HeaderSelectedForeground`, `HeaderFilledCornerRadius`,
`HeaderIndicatorThickness`, `HeaderIndicatorBottomInset` and
`HeaderIndicatorHoverColor`. Their defaults, scope and interaction with existing
roles are documented in the [native README](../README.md). Radius/thickness accept
0..64 logical pixels; bottom inset accepts 0..1024. Null/Empty retains existing
behavior, zero thickness removes the built-in underline, and values clamp inside
measured header bounds. Explicit selected ink also applies to selected contextual
tabs; disabled ink retains precedence. Existing hover/pressed fills, indicator
padding/radius, material opacity and scroller settings remain independent.
XML documentation, additive Unshipped API entries and assignment validation are
included; the shipped API baseline is untouched.

Verification is under `artifacts/theme-header-c1/`:

- **47 distinct focused Release cases pass across retained runs.**
  `tests/header-effects.trx` records 41 cases, including 13 new C1 cases covering
  before/after drawing, owner hooks, mixed candidate settings, authored effects
  and rejected assignments. Selected existing cases cover header clipping,
  scrollers, QAT/overflow, contextual colors and inert theme previews.
  `tests/header-motion.trx` adds six existing cases for bidirectional indicator
  motion, exact settling and one interpolated indicator through QAT/density
  changes. Passing scopes were not rerun for a combined log.
- **48 PNG/geometry pairs match exactly** between fresh `before/` and `after/`:
  32 state sheets cover both light themes and opposite-style clones at synthetic
  96/120/144/192 DPI through ordinary/composited drawing; 16 native-strip sheets
  cover both light themes, both densities, normal/narrow widths and colored/
  neutral captions. Comparisons include indicator bounds, File, QAT, scrollers,
  tab extent and viewport targets. New effect assertions independently verify
  changed pixels/geometry and stable hit targets, rather than updating defaults.
- Normal Release runtime/test and normal Debug runtime/designer server/client/
  sample builds pass with zero warnings/errors. The permanent Designer Sample
  was refreshed in place; its current local package is recorded in
  `samples/RibbonKit.WinForms.DesignerSample/.local/last-refresh.json`.
  Its optional `--office2019-theme-demo` now includes a **Header effects** group
  with style, line, corner, color and reset actions. Only supplemental demo/check
  code changed; authored Forms/UserControls, resources and project files were
  preserved.
- The public-package `--verify-header-presentation <output>` check passes **4/4
  scopes** at actual **120 DPI**: both light families in Solid/Mica, with requested
  Mica effective on this machine. Each covers normal/narrow headers, a native
  scroll click and glyph clipping beside File/QAT/scrollers, open File, mixed
  header effects and stable targets/models. Native editor handle, parent, text
  and selection remain intact. Results and PNGs are in `package/`; representative
  ordinary/material default/custom captures were inspected. The fixture references
  only the public package, with no runtime internals or Showcase helpers/resources.

This establishes the bounded automated/package scope. Offscreen Forms and
composited snapshots do not establish visible material contrast, frame pacing,
actual VS, physical input, IME or mixed-monitor acceptance. C1 human appearance
review is pending; no full suite, full native matrix, WPF check, commit or push
ran. C2 body/connected frames is next, retaining the adjustable-effects requirement
and the geometry consolidation foundation.

## Backstage fill routing and motion overrides — 2026-11-11

The user authorized the first audit correction batch, A00–A04, after the source
review. Entry retained the existing uncommitted C1/audit work on
`codex/Ribbonkit-Winform` at `6162571`. This batch does not implement the remaining
decoration/shared-helper candidates or the separately deferred closing-title
ink defect.

Inset Backstage material/composited rows now prefer explicit navigation
hover/selected fills before generic backdrop fallback, preserving authored alpha.
The surface adapter leaves un-authored inset material fills null instead of
materializing derived accent colors as apparent overrides. This distinction
retains existing default material colors while honoring actual application
settings. Solid/full-width default drawing and actual public renderer hooks
remain unchanged.

`RibbonTheme.BackstageMotion` holds a nullable immutable
`RibbonBackstageMotionSettings` record with duration, page-distance, four curve
choices and pane-fade-strength overrides. `RibbonBackstageEasing` exposes the two
existing curves plus Linear. Each null member resolves from the registered
family profile. Pane/title timing remains coupled; title ink stays opaque.
Zero pane duration skips both pane/title motion and unused opening-background
capture. Zero page duration uses immediate selection; zero page travel removes
movement while leaving its positive duration independent. Fade strength zero
starts native opacity at 255 and retains it for slide-only pane motion; page
Fade remains separately selected by `TabContentAnimation`.

Preparation and progress use the same captured policy. Existing timers, native
alpha-1 hit-testing safeguard, cancellation, reduced motion, page buffering and
repaint/ownership remain shared. Duration/curve/strength/distance validation runs
before theme or BuiltInTheme mutation. Defaults and reset behavior are documented
with an example in the [native README](../README.md). XML and additive Unshipped
API entries are included; the shipped API baseline is untouched.

Evidence is under `artifacts/theme-effects-backfill/`:

- `tests/fills-before.trx` reproduces four failures for explicit inset material
  fills; full-width cases already honored them. `fills-and-default-policy.trx`
  passes 19 cases after correction, including all eight fill-route/alpha cases,
  the default comparison and ten prior B3 policy cases.
- **16 PNG/geometry pairs match exactly** between fresh `before/` and `after/`,
  both light families, both coverage styles, accent/neutral navigation and
  Solid/Mica. The native fixture's effective material and geometry are retained
  alongside each state sheet.
- `tests/motion-settings-and-lifecycle.trx` passes 66 cases: 16 new authored
  settings/validation/reset cases and 50 existing pane/title/page, native editor
  repaint, handoff and exposed-region cases. After making fade strength an
  explicit family fallback and extending member-reset assertions,
  `tests/motion-profile-final.trx` passes the affected 26 policy/settings cases.
  These retained runs establish **85 distinct cases**; repeated cases are not
  counted again. Release runtime/test builds pass with zero warnings/errors.
- Normal Debug runtime/designer server/client/sample builds pass without
  warnings/errors. The permanent sample is refreshed in place; its local package
  is recorded in `.local/last-refresh.json`. The optional Office 2019 demo has a
  **Backstage motion** group using public settings. Authored Forms/UserControls,
  resources and project files were preserved.
- The new public-package `--verify-backstage-effects <output>` check passes
  **4/4 scopes** for both light families in Solid/Mica at actual **120 DPI**,
  with requested Mica effective. It observes real timer completion and native
  opacity, custom page travel, independent page Fade, zero-duration open/page/
  close, one close event, reset and retained native handle/parent/text/selection.
  Selected-row callbacks replay their resolved public paint context to verify
  fill/alpha; only the library-owned navigation button is explicitly printed
  for offscreen material paint, with no application-page capture. Timer samples
  are not compositor-frame evidence. Results and representative fill PNGs are
  under `package-final/`.

These are bounded automated/package results; human appearance, visible material
contrast/frame pacing, actual VS, physical input, IME and mixed-monitor acceptance
remain separate. No full suite/native matrix, WPF build/test, commit or push ran.
The subsequent A06–A09 implementation is recorded below. A05's caption choices
and the known title defect retain their separate title scope.

## Backstage decoration and spacing overrides — 2026-11-11

This continuation implements audit A06–A09 on the same branch/HEAD and preserves
the prior uncommitted slices. `RibbonTheme.BackstageAppearance` is a nullable
immutable `RibbonBackstageAppearanceSettings` record with independently resettable
marker dimensions/insets/ink, Back circle inset/outline, arrow scale/stroke and
navigation-state radius/border ink/width. Existing marker visibility, shared
marker/Back radii, Back target size and full-width coverage remain independent.
Zero widths/scale can hide decoration; Empty ink follows the documented fallback.
Alpha is consumed in Solid/material painting. Validation precedes theme or
BuiltInTheme mutation; XML/additive Unshipped entries leave shipped API untouched.

The three direct spacing overrides are `BackstageRowTextVerticalPadding`,
`BackstageSeparatorHeight` and `BackstageTrailingPadding`. They resolve through
existing Backstage metrics into row/font minima, separators, footer/scroll layout,
Auto navigation width and entry text bounds. `MenuSeparatorHeight` stays menu-only.
Page-width preference, native lifecycle and geometry engines were not rewritten.

Marker top/bottom clearances retain summed DPI rounding; Back vector extents retain
their separate rounding. Inset state drawing still calls the actual renderer's
virtual chrome hook, which can replace built-in drawing. Inherited flat/Office
primitives consume authored appearance through a scoped internal context flag;
other chrome is unaffected. Full-width defaults keep their square, borderless
recipe. Clipping contains authored markers/arrows within their row/Back targets.

Evidence is under `artifacts/backstage-appearance-settings/`:

- Fresh `before/` and `after/` contain **32 exact PNG/geometry pairs**: two light
  families, flat/Office renderers, full-width/inset rows, four DPI levels
  (96/120/144/192), six visual states and both density layouts. The subsequent
  `tests/empty-group-defaults.trx` also verifies empty-group/null pixel equivalence.
- `tests/appearance-focused.trx` passed 67/71; four new spacing-fixture cases
  initially requested an invalid minimum navigation width of 1. Correcting the
  fixture to the existing minimum of 80 required no runtime change.
  `tests/appearance-settings-final.trx` passes all 40 affected appearance/fill
  cases, including the corrected four. Together these retained runs establish
  **71 distinct passing relevant cases**, with no repeated-case inflation.
  Coverage includes 31 appearance/default/spacing/reset/validation cases and
  existing fill, typography, layout-input, navigation and family-isolation cases.
  Normal Release runtime/test builds pass without warnings/errors.
- Normal Debug runtime/designer server/client/package/sample refresh passes
  without warnings/errors. The permanent package consumer adds
  `--verify-backstage-appearance <output>`; its **4/4 scopes pass** for both
  light families in Solid/Mica at actual **120 DPI**, with Mica effective.
  It prints only library-owned navigation buttons and replays their public
  renderer context, checking both width-coverage modes. Actual native targets,
  row/separator bounds, zero-height separators, group/spacing reset, selection,
  editor HWND/parent/text/selection and close/reopen are checked. Results/PNGs
  are under `package-final/`; no application page is captured.
- The initial package diagnostic exposed a JIT dialog reported by the user:
  its Back assertion threw from a paint callback on a one-RGB-step translucent
  DIB round trip. The diagnostic now records assertion failures outside callbacks,
  captures unexpected UI-thread exceptions to the failure log and allows that
  bounded premultiplication rounding. The corrected four-scope run completes
  without the dialog. This was a consumer verification defect, not a change to
  runtime alpha/default rendering.
- The optional theme demo adds **Backstage appearance** controls for marker
  visibility/size/color, Back arrow, navigation outlines/rounding, spacing and
  reset. Visibility remains separate from the appearance group. Authored
  Forms/UserControls/resources and project files are preserved; the current
  local package identity remains recorded by `.local/last-refresh.json`.

Current appearance acceptance is tracked in the ledger. Visible frame pacing,
actual VS, physical input, IME and mixed-monitor acceptance remain separate.
No full suite/native matrix, WPF checks, commit or push ran. Next proposed work
is A11–A16 header/shared helper effects; A05 caption, A10 page-width preference,
A17 shared badge placement and the closing-title ink defect remain separate.

## Header and shared effect overrides — 2026-11-11

This continuation implements audit A11–A16 while preserving the staged prior
slices on `codex/Ribbonkit-Winform` at `6162571`. Additive, documented APIs are
tracked in Unshipped; the shipped baseline and WPF remain unchanged.

- `HeaderAppearance` is an immutable `RibbonHeaderAppearanceSettings` record for
  Filled tab/File state border ink/width, header-scroller radius/width, contextual
  inactive opacity and open CommandMenu File inset/fill/ink. Null/Empty retains
  existing defaults; authored alpha and zero borders are consumed. Filled headers
  retain their original open-File recipe unless File-highlight members are
  explicitly authored, then use the shared highlight. Highlight radius remains
  independently controlled by its existing setting. Automatic ink retains the
  black/white contrast algorithm, using the authored fill's first color.
- Shared `Chevrons` is an immutable `RibbonChevronSettings` record for long/short
  dimensions and stroke. Header/body scroll, overflow, minimize, submenu and
  gallery paths pass settings into the same horizontal/vertical primitive.
  Existing utility bounds clamp size; zero hides the glyph. Filled triangles
  and the Back arrow keep their separate owners. Header scroller shape/width
  overrides do not leak into body scrollers or alter targets.
- `FocusInset` and `FocusStrokeWidth` share one resolved owner. Null preserves
  the two-device-pixel inset, native dotted neutral focus and one-device-pixel
  Office outline. Authored inset is logical and clamps inside short targets;
  authored widths are positive, with dotted neutral painting and existing Office
  radius support. Public focus/chrome/text/scroller hooks retain precedence.
- `TabContentSlideDistance` feeds both body selection and header arrival through
  the existing scene renderer; `HeaderIndicatorSpringAmount` controls Expressive
  marker overshoot. Theme values are captured in a value-only scene policy;
  animation-level defaults, clocks, interruption, reduced motion, opacity, native
  ownership and clip/indicator-width safeguards remain shared. Zero travel or
  spring removes that effect without creating another timer or changing Fade.

All groups/members reset independently and validate before theme/BuiltInTheme
mutation. Examples, ranges, defaults and effect coverage are in the native README.

Evidence under `artifacts/shared-theme-effects/`:

- Fresh `header-before/` / `header-after/` contain **48 exact PNG/geometry pairs**:
  header states at 96/120/144/192 DPI in ordinary/composited drawing, selector/
  palette combinations and wide/narrow strip geometry including both densities
  and colored captions. `before/` / `after/` add **32 identical shared-effect
  PNGs** across two light families, flat/Office renderers, both paint paths and
  four DPI levels. Empty header/chevron records also match null exactly.
- `tests/effects-focused.trx` initially passed 69/70; the new Filled-File case
  exposed its separate drawing route. Explicit authored-highlight routing is
  corrected while defaults stay unchanged. `effects-authored-final.trx` passes
  **44/44** affected cases, including the corrected File case and six new actual
  scene cases for zero/custom header/body travel and zero/stronger spring.
  `limits-empty-groups.trx` passes **13/13** after extending valid maximum/null
  and empty-group checks. These retained runs establish **76 distinct passing
  relevant cases**, counting old default-argument cases once. Other checks cover
  header/utility geometry, focus/hooks, contextual ink, clipping, native motion
  lifecycle and editor retention. Normal Release builds pass without warnings.
- Normal Debug runtime/designer server/client/package/sample refresh succeeds
  with zero warnings/errors, using normal outputs and preserving authored files.
  Current local package is `0.1.0-designer.local20261111T055838136Z`, recorded in
  `.local/last-refresh.json`. The public-package
  `--verify-shared-theme-effects <output>` check passes **4/4 native scopes** at
  actual **120 DPI**, both families in Solid/Mica with Mica effective. Selected
  tab/scroller/File contexts are checked via actual public renderer callbacks;
  assertion failures are recorded outside paint callbacks. Wide/narrow headers,
  actual shared motion, member/group reset and native HWND/parent/text/selection
  are checked. `package-final/` retains results and PNGs; timer samples are not
  compositor-frame evidence.
- The same consumer checks **Theme effects 2** placement, original Office 2019
  group count, public setting buttons, reset and idempotent initialization.
  Future review groups should use that tab until the user declares it full.

Current appearance acceptance is tracked in the ledger. Actual VS, physical input, visible
frame pacing, IME and mixed-monitor acceptance remain separate. No broad suite,
native matrix, WPF validation, commit or push ran. C2 is next proposed; A05 caption,
A10 page-width preference, A17 shared badge placement and the known closing-title
ink defect retain their separate scope.

## Body and connected frames C2 — 2026-11-11

C2 entered on a clean `codex/Ribbonkit-Winform` at `bee7a08`, after the prior
slices had been committed outside this continuation. It retains the consolidated
layout engine and snapshots, reduction, content padding, native editors/presenters,
popup anchoring and public renderer precedence. Both light definitions compose
the same body structure; no public body selector or palette/radius inference was
added. This continuation itself does not commit, stage or push.

`RibbonPresentationResolver` supplies explicit neutral/Office
`RibbonBodyFrameDefaults`. `RibbonBodyFrameMetrics` resolves value-only radius,
shadow clearance, border policy and widths; `RibbonBodyRenderer` shares body,
connected shadow, below-QAT separator, message-frame and Classic group-frame
drawing. The public Office renderer is a forwarding facade, still calling actual
virtual body radius/clearance and body/QAT/message/group hooks. Neutral drawing
keeps its flat defaults and authored-fill/border behavior. Original null DPI
rounding, 2019's edge-to-edge bottom-border body and joined square edges remain.

`RibbonTheme.BodyAppearance` holds an immutable `RibbonBodyAppearanceSettings`
record: body/message border widths, group-divider width/ink, joined QAT separator
width/inset/ink and nullable QAT/message join preferences. Null/Empty resets
independently, explicit zero hides decoration and colors retain alpha. Validation
precedes theme/BuiltInTheme mutation. New joins use existing shared measurement
and invalidation and are gated by actual renderer capabilities and placement
constraints. Existing body border/radius/shadow/margin/inner-padding, message-side
border and separator vertical-inset roles remain independent and consumed.

Classic group frames and Simplified dividers use the additive public
`DrawGroupSeparator` hook on the actual application renderer. Decoration widths
do not rewrite reduction, target widths, column gaps or popup source anchors.
Command/caption content arrangement and their remaining visual metrics remain in
the shared command owners for the later D surfaces; C2 extracts frame decoration,
not those control recipes. Message content clips respect the authored outer-frame
stroke; row rules/actions retain their existing owners. XML/API declarations are
additive in Unshipped; the shipped baseline is unchanged.

Evidence under `artifacts/body-presentation-c2/`:

- Fresh `before/` / `after/` contain **80 exact PNG/geometry pairs**. Sixteen sheets
  cover both palettes, neutral/Office renderers at 96/120/144/192 DPI with body,
  standalone/joined QAT/messages and group frames. Sixty-four native comparisons
  cover both families, Solid/Mica, Classic/Simplified, Compact/Touch, expanded/
  minimized and 1000/380-wide hosts, including body/frame/viewport/target geometry.
  Empty override records also match null in the paint sheets.
- `tests/c2-focused.trx` passed 32/33; the new Simplified editor fixture initially
  queried an editor before its normal Classic realization. Correcting the fixture
  to realize once, then switch modes, required no runtime workaround.
  `joins-and-message-motion.trx` passes **14/14**, including the corrected case,
  immutable/reset behavior and existing message-motion ownership/join checks.
  `frame-hooks-and-borrowing.trx` passes **5/5** for actual radius/shadow hooks,
  QAT seam ownership and borrowed group bodies. `frame-policy-final.trx` passes
  **10/10** after checking empty/default equivalence, replacing divider drawing
  through the public hook and moving the unchanged connected-shadow algorithm.
  Together these establish **49 distinct passing relevant Release cases**,
  counting repeats once. Existing material-frame, reduction/padding, QAT/message
  and native editor checks are included. Builds pass with zero warnings/errors.
- Normal Debug runtime/designer server/client/package/sample refresh passes
  without warnings/errors, using normal outputs and preserving authored files.
  Current package is `0.1.0-designer.local20261111T062849153Z`, recorded in the
  sample's `.local/last-refresh.json`.
- The public-package `--verify-body-presentation <output>` check passes **4/4
  native scopes** at actual **120 DPI**, both light families in Solid/Mica with
  Mica effective. Each scope checks eight mode/density/minimized states with
  joined/separate geometry, actual body/group-divider/QAT/message callbacks,
  authored border/divider widths, member/group reset, real message motion and
  retained editor HWND/parent/text/selection and QAT declaration identity.
  `package-final/` retains logs/PNGs; timer samples are not compositor frames.
  Diagnostic assertions are recorded outside native paint callbacks.
- The consumer also verifies that the new **Body and connected frames** group is
  appended once to **Theme effects 2**, with preview, public settings and reset.
  The original Office 2019 tab retains seven groups. Shared review initialization
  now checks its own group identity so later groups can share the tab safely.

Current appearance acceptance is tracked in the ledger. Actual VS, physical input, visible flicker/
frame pacing, IME and mixed-monitor acceptance remain separate. No broad suite,
full native matrix, WPF build/test, commit or push ran. D1 commands/editors is
next proposed; A05 caption, A10 page-width preference, A17 badge placement and the
known closing-title ink flicker remain separate.

## Command-state and split chrome D1a — 2026-11-12

The first bounded D slice extracts ordinary command-state backgrounds and split
chrome; it does not claim native editors or all command content are rewritten.
During this work the user committed C2 as `f83a0ef` (`C2 Done`). HEAD/worktree were
rechecked and that commit preserved. This continuation does not commit, stage,
push or replace authored Forms/UserControls/resources.

`RibbonCommandAppearanceMetrics` resolves value-only device metrics from explicit
shared defaults and immutable `RibbonTheme.CommandAppearance`. The active neutral/
Office drawing primitive supplies its own default radius, so no palette/header
selector or new command-family selector is inferred. Null retains one-device-pixel
inset, outline and separator recipes with their existing ink. Explicit corner,
border, inset, split separator width/clearance/ink and resting companion fill reset
independently and validate before theme/BuiltInTheme mutation.

`RibbonCommandRenderer` draws both ordinary and borrowed QAT split halves through
the actual application's virtual chrome hook, clipping each half to the common
perimeter. Checked/open/hover/press/disabled precedence and existing fill roles
remain; explicit resting-half alpha/gradients precede material fallback while
active QAT fills retain their established backdrop opacity. Ordinary item/menu
state backgrounds, compact ComboBox openers, check/radio row backgrounds and QAT
command/overflow states use the scoped command-chrome path. Native editor frames,
indicator glyphs, header/body/group frames and Application Menu recipes retain
their owners. Content/target measurement, split actions/KeyTips, commands, popup
sources and native editor lifetime are unchanged.

Public item/split/QAT entry points, protected chrome and text/focus/image hooks
remain authoritative. A custom chrome override can replace the built-in drawing.
Examples and ranges are in the native README; XML and additive Unshipped API
entries leave the shipped baseline unchanged.

Evidence under `artifacts/command-appearance-d1a/`:

- Fresh `before/` / `after/` contain **32 exact PNG/geometry pairs** across both
  light palettes, flat/Office renderers, ordinary/composited contexts and four
  DPI levels. Sheets cover ordinary command/menu states, horizontal/vertical
  splits and QAT checked/inactive recipes. `tests/empty-defaults-final.trx`
  passes empty-record/null equivalence against those same baselines.
- `tests/commands-focused.trx` passed 30/34; the four new authored QAT fixtures
  initially used unowned references, then expected an opaque material hover
  without its existing backdrop-opacity factor. Correcting the fixture to use
  owned references and the established opacity required no runtime workaround.
  `qat-alpha-and-routing.trx` passes **8/8**: all four authored seam/alpha cases
  and four existing QAT action/menu/editor routing cases. Together these retained
  runs establish **38 distinct passing relevant Release cases**, with repeats
  counted once. Checks also cover native action/UIA geometry, source identity,
  disabled behavior, custom hooks, reset, atomic validation and editor retention.
  Release runtime/test builds pass with zero warnings/errors.
- Normal Debug runtime/designer server/client/package/sample refresh passes
  without warnings/errors. Current package is
  `0.1.0-designer.local20261111T070450978Z`, recorded in `.local/last-refresh.json`.
  The public-package `--verify-command-appearance <output>` check passes **4/4
  scopes** at actual **120 DPI**, both light families in Solid/Mica with Mica
  effective. Each checks four Classic/Simplified × Compact/Touch states through
  actual root/QAT split callbacks, authored borders/seams, unchanged action
  geometry, independent primary/menu opening, projected source/KeyTip, member/
  group reset and retained editor HWND/parent/text/selection. Logs and PNGs are
  under `package-final/`.
- The package diagnostic uses public WinForms message filtering followed by
  control preprocessing for projected KeyTips. F10 is handled by the library's
  installed message filter, not `PreProcessMessage` alone; direct offscreen mouse
  checks can also encounter normal ToolStrip dismissal. No runtime input change
  was introduced to accommodate the diagnostic. Assertions are collected outside
  paint callbacks and native exceptions are logged instead of showing JIT UI.
- The same consumer checks **Theme effects 3** initialization, public review
  buttons/reset and idempotence, while the two full tabs retain seven/five
  groups. `ThemeEffectsReviewTab` now denotes tab 3; `ThemeEffects2ReviewTab`
  pins earlier shared/body groups to tab 2. Continue adding review controls to
  tab 3 until the user declares it full.

D1a human appearance review is pending. Actual VS, physical input, visible frame
pacing, IME and mixed-monitor acceptance remain separate. No broad suite, full
native matrix or WPF build/test ran. D1b was subsequently delivered below;
native editor recipes, A05/A10/A17 and the known title-ink flicker remain separate.


## Command indicators and raster treatment D1b — 2026-11-12

The user authorized continuation on clean `codex/Ribbonkit-Winform` at `2581bb8`
(their committed D1a). This bounded content slice extracts the shared check/radio,
Quick Access mixed-bar and disabled-raster recipes into
`RibbonCommandContentRenderer`. Filled triangles still use the shared
`RibbonArrowPainter`; the public renderer entries and protected image hook remain
on the actual renderer. The light families share these structures, so no family
selector or parallel layout engine was added. Office checkbox radius still uses
`CheckBoxCornerRadius`; canonical tick geometry and optical centering remain.

`Theme.CommandContent` accepts immutable `RibbonCommandContentSettings`:

- `IndicatorBorderWidth` / `IndicatorBorderColor`: null retains one device pixel;
  Empty retains existing enabled/active/disabled ink. Width 0..32 logical pixels
  clamps inside the measured slot; zero/Transparent hide the border.
- `CheckMarkColor`, `CheckMarkStrokeWidth`, `CheckMarkScale`: authored ink/alpha,
  stroke 0..32 and scale 0..4. Null retains existing checkbox tick/mixed-bar
  recipes (1.5 logical pixels, minimum one device pixel) and QAT mixed bar
  (6 × 2 logical pixels). QAT uses scale for bar width and stroke for bar height.
  Empty ink keeps Surface for checkboxes, Foreground/DisabledColor for QAT.
  Zero stroke/scale hides the mark without changing checked/mixed state.
- `RadioDotInset` / `RadioDotColor`: null retains four logical pixels and
  Accent/DisabledColor. Inset 0..1024; zero fills the circle, consuming the slot
  hides the dot. Existing concentric slot arithmetic is retained.
- `DropDownGlyphWidth` / `DropDownGlyphHeight`: null retains 8 × 6 logical pixels;
  authored 0..64 dimensions clamp to the existing slot, zero hides the triangle.
  Actual caller ink, public arrow hooks and separate `Chevrons` stay authoritative.
- `DisabledImageOpacity` / `DisabledImageGrayscaleAmount`: null retains ordinary
  grayscale opacity 0.55 and the framework recipe for independently disabled QAT
  references. Authoring either finite 0..1 role selects the shared matrix for
  both raster paths, with independent null fallbacks 0.55 opacity / 1 grayscale.
  Grayscale zero retains source color; intermediate amounts partly desaturate.
  Enabled images and vector-icon Draw contexts remain unchanged; borrowed images are never disposed or rewritten.

All numeric overrides validate before Theme/BuiltInTheme mutation; doubles must
be finite. Members reset independently with null/Empty; `CommandContent = null`
resets only this group. Existing Accent/Surface/DisabledForeground/Pressed roles
still supply state fills. There is no duplicate color policy or animation clock.
Shared image/triangle hooks also serve their existing gallery, popup and
Application Menu callers; this does not claim a rewrite of those surfaces.
Library options-button image painting remains with its later dialog owner.
Text wrapping, icon slots/padding, split targets, focus, native editor frames,
caption content and Application Menu chrome keep their owners. Theme assignment
retains its existing popup-close lifecycle and retained menu models can reopen.

Relevant evidence is under `artifacts/command-content-d1b/`:

- `tests/content-focused.trx`: **37 passed** (21 initial D1b cases and 16 existing
  option/arrow/QAT/menu cases). Four simulated DPIs cover mark alpha/hide/clips,
  concentric dots, triangle dimensions, mixed bars, ordinary/projected disabled
  raster opacity and vector ownership. Validation checks every numeric role,
  NaN/infinities and mutation ordering; public hook and Reset checks are included.
- `before-defaults/` and `after-defaults/`: **16 exact PNG/geometry pairs** for
  both light themes × flat/Office rendering × 96/120/144/192 DPI. Null and empty
  groups both match. `tests/after-defaults.trx` initially verified extraction;
  `tests/content-final.trx` retains final default parity. Its expanded hook
  fixture initially counted an earlier empty image callback; the counter was
  reset before the targeted call. `tests/hooks-final.trx` passes the corrected
  hook scope without rerunning the already-passed batch.
  `tests/raster-final.trx` passes all 13 affected default/raster/validation cases
  after adding independent grayscale strength, including a new validation case.
  **38 distinct relevant cases (22 new, 16 existing) have passing latest results.**
- `package-raster-final/result.log`: **4/4 package-only scopes** at actual 120 DPI,
  Office 2019/2024 Light × None/Mica, each Classic/Simplified × Compact/Touch.
  Actual renderer callbacks replay authored check/radio/triangle/raster/QAT
  effects; native slots and editor HWND/parent/value/selection remain unchanged.
  Models, independently disabled reference/source state, independent/group Reset
  and existing theme-close/menu-reopen behavior are checked. An initial probe
  incorrectly required a popup to stay open during theme assignment; source
  inspection confirmed the pre-existing close policy, and only the probe changed.
  Native callback assertions are collected and logged without JIT dialogs.
- Normal Release runtime/test and normal Debug runtime/designer server/client/
  permanent sample builds passed with zero warnings/errors. The local sample
  package is `0.1.0-designer.local20261111T175913865Z` (UTC version timestamp).
  Its new **Try indicators / images** and **Command content effects** groups use
  **Theme effects 3**; full tabs retain seven/five groups. Public buttons/reset,
  idempotence and the rendered review tab were checked. Authored Form/UserControl/
  resource/project files are retained. The earlier D1a probe now measures its own
  groups and idempotence without treating all of tab 3 as exclusively D1a.

D1a–D1b human appearance review is pending. Actual VS, physical input, visible
frame pacing, IME and mixed-monitor checks remain separate. No full suite/native
matrix or WPF build/test ran. D1c native editor appearance is next proposed;
A05/A10/A17 and the known Backstage closing-title ink flicker remain separate.


## Native editor appearance D1c — 2026-11-12

The user confirmed D1b looks good, authorized continuation and requested a new
review tab. Work started clean on `codex/Ribbonkit-Winform` at their D1b commit
`2a38995`. D1c extracts TextBox/ComboBox field chrome, clipped ComboBox button
states and choice-row paint into shared `RibbonEditorRenderer`. Both light
families share these recipes; no new family selector or layout engine was added.
`RibbonEditorAppearanceMetrics` resolves explicit common defaults from
`RibbonPresentationResolver.EditorChrome`, preserving the actual neutral/Office
primitive's radius and all null device-pixel arithmetic.

`Theme.EditorAppearance` accepts immutable `RibbonEditorAppearanceSettings`:

- `CornerRadius`: optional 0..64 logical pixels for fields; null keeps square
  neutral or the actual Office renderer's existing global radius.
- `BorderWidth`: optional 0..32 logical pixels, clamped inside the painted field;
  null keeps one device pixel when ink exists; zero hides it.
- `Inset`: optional 0..1024 logical pixels, clamped inside the measured field;
  null keeps one device pixel. This decorates the frame, preserving native input
  bounds and opaque text/selection surfaces; it does not add input padding.
- `ActiveBorderColor`: authored alpha/Transparent or Empty for Accent. Existing
  TextBox focus and ComboBox hot/focus/press/open eligibility remain unchanged;
  disabled masking and resting `EditorBorder` ink retain their existing rules.
- `ChoiceCornerRadius` / `ChoiceInset`: independently resettable active-row shape;
  0..64 / 0..1024 logical pixels. Null retains the renderer radius / one device
  pixel inset; zero inset fills the unchanged choice target.
- `ChoiceTextHorizontalPadding`: optional 0..1024 logical pixels on each side,
  clamped to available width. Null retains six logical pixels. Native input text
  insets, choice heights, field widths and hit targets retain their existing roles.

Numeric roles validate before Theme/BuiltInTheme mutation. Null members/Empty
ink reset independently; `EditorAppearance = null` resets only this group.
`EditorBackground` / `EditorForeground` / `EditorBorder`, Hover/Pressed/Checked,
D1b filled-arrow settings and existing editor/choice geometry roles are reused.
The native editor manager, binding/validation, IME, selection and presenter/list
ownership code is unchanged. Scoped internal chrome context reaches the actual
protected `DrawChrome`; custom chrome replacement and public field/choice/text/
arrow overrides retain precedence. Choice settings do not recolor or rearrange
the native ComboBox selection area. Command/header/Backstage chrome is separate.

Relevant evidence is under `artifacts/editor-appearance-d1c/`:

- `tests/before-defaults.trx` / `tests/after-defaults.trx`: two existing default
  cases expanded across both light themes; **64 exact PNG/geometry pairs** in
  `before-defaults/` and `after-defaults/`. Flat/Office rendering, Compact/Touch,
  9/14-point fonts and 96/120/144/192 DPI all match for null and empty records.
- `tests/editors-focused.trx`: **51 passed**, including 15 new authored-appearance
  cases (field outlines/alpha/hide/inset/clamps and four-corner symmetry, state
  fills/disabled masks, independent choice shape/padding, atomic range validation,
  immutable reset and actual renderer hooks). The 36 retained cases cover native
  value/binding/validation/Escape, compact/QAT choices, editor/inline geometry,
  menu alignment and first-paint selection in Solid/Mica/Acrylic. Together with
  the two default cases, **53 distinct relevant cases passed**. No passed batch
  was restarted to verify the later sample-only adjustments.
- `package-final/result.log`: **4/4 package-only scopes**, Office 2019/2024 Light
  x None/Mica, actual 120 DPI, each Classic/Simplified x Compact/Touch. Actual
  TextBox/ComboBox/choice callbacks replay authored borders/radius/inset/padding;
  native fields retain measured slots, HWND/parent/bounds, text/selection/read-only
  state, choice owners, validation rejection/commit and member/group reset.
  Eight of 16 samples had editor focus; all preserve the established focus state.
  Initial probes assumed all samples were focused, including unfocused/parked
  presentations. The probe now captures baseline focus and uses public command
  activation; no runtime focus workaround was introduced. The package fixture
  uses public Enabled values under enabled owners, not internal EffectiveEnabled.
- Normal Release runtime/test and normal Debug runtime/designer server/client/
  permanent sample builds pass without warnings/errors. Final local package:
  `0.1.0-designer.local20261111T181632630Z`. The in-place refresh retains authored
  Forms/UserControls/resources/project files. `demo-final/result.log` checks the
  final review presets/reset/idempotence after the sample-only inset adjustment.
  New **Theme effects 4** contains **Try text fields**, **Try choices** and
  **Editor appearance effects**; prior tabs retain seven/five/four groups.
  `ThemeEffectsReviewTab` now denotes tab 4; `ThemeEffects3ReviewTab` pins D1a/D1b
  controls to tab 3. Rendered review/native captures were inspected. Native child
  printing can overlay the library frame, so replay checks establish its paint
  separately; live appearance of composed native fields remains human review.

D1c appearance review is pending. Actual VS, physical input/IME, visible frame
pacing and mixed-monitor checks remain separate. No full suite/native matrix or
WPF build/test ran. D2 ordinary popup-frame appearance is next proposed;
Application Menu and later surfaces remain separate. A05/A10/A17 and the known
Backstage closing-title ink flicker retain their deferred scope.


## Ordinary popup-frame appearance D2 — 2026-11-12

The user authorized continuation from their clean D1c commit `41fd2aa` on
`codex/Ribbonkit-Winform`. This bounded slice extracts shared floating-popup
frame/shadow recipes into `RibbonPopupFrameRenderer`, with value-only
`RibbonPopupFrameMetrics` and explicit `RibbonPresentationResolver.PopupFrame`
defaults. Neutral and Office reuse this structure through the actual renderer's
radius hooks; no family selector or replacement layout engine was added.

`Theme.PopupAppearance` accepts immutable `RibbonPopupAppearanceSettings`:

- `BorderWidth`: optional 0..32 logical pixels; null keeps one device pixel.
  Zero hides paint while keeping the legacy one-device-pixel content clearance.
  Wider borders reserve space around the existing content, subject to work-area
  clamps, rather than drawing underneath native editors or choice rows.
- `ShadowHorizontalOffset`: optional -32..32 logical pixels; null keeps zero.
  Existing `PopupShadowVerticalOffset` and `PopupShadowBlur` remain independent.
  Authored offset/blur clearance uses scaled blur plus the larger absolute offset.
  All-null offsets/blur retain legacy summed eight-logical-pixel rounding.
- `ShadowFalloff`: optional finite 0.1..16; null keeps 2.5. Higher values
  concentrate the Gaussian shadow near the frame; lower values soften it within
  the same blur footprint. The shared painter's half-device-pixel safety floor
  remains. Its bounded cache now keys by falloff; body shadows retain 2.5.

Numeric settings validate before Theme/BuiltInTheme mutation. Members reset
independently with null; `PopupAppearance = null` resets only this group. Existing
`PopupBackground`, `PopupBorder`, `PopupCornerRadius`, `PopupShadowEnabled`,
`PopupShadowColor`, blur/vertical offset and system shadow policy are reused.
The native opaque popup body and layered shadow/frame still have their existing
owners. Public frame/shadow/radius/clearance overrides retain precedence.

The additive public `GetPopupBorderWidth(theme, dpi)` virtual hook supplies device
pixels to both paint and content reservation. Consumers bound it to 128 logical
pixels, retain at least one device pixel of content clearance, and clamp that
clearance for narrow work areas. Popup shell padding, hosted size budgets and
hosted corner masks consume this value together. Menu/custom-body sizing retains
its existing algorithms and native ownership. Choice-list height/width fitting
uses the same border and now also consumes D1c's authored horizontal text padding
instead of a second fixed six-pixel measurement. Passive tip measurement adds
only clearance above its legacy one-pixel frame to existing tip padding.
The shared outer frame also serves galleries, tips and Application Menu callers;
this does not claim their interior recipes are rewritten. Activation, stack,
placement, clocks and borrowed content ownership are unchanged.

Relevant evidence is under `artifacts/popup-appearance-d2/`:

- `tests/before-defaults.trx` / `tests/after-defaults.trx`: **32 exact PNG/geometry
  pairs** in `before-defaults/` and `after-defaults/`, both light themes x flat/
  Office x 96/120/144/192 DPI x even/odd dimensions. Null and empty records match
  saved frame/shadow pixels, radius/clearance, native shell padding and placement.
- `tests/popup-focused.trx`: **33 passed**. Fourteen new authored cases cover
  border width/alpha/hide/four-corner symmetry, horizontal offset/footprint,
  falloff/cache, unchanged body shadow, actual custom border hooks, native menu/
  choice/tip clearance, retained editor state, work-area clamps, atomic validation,
  immutable reset and frame/shadow replacement hooks. Nineteen existing cases
  cover popup HWND creation, choice mouse/keyboard focus, frame/clearance geometry,
  passive tips and gallery/menu motion. With the default case, **34 distinct
  relevant cases passed**; no broad suite was rerun.
- `package-final2/result.log`: **4/4 public-package scopes**, Office 2019/2024 Light
  x None/Mica, actual 120 DPI, each Classic/Simplified x Compact/Touch. Actual
  frame/shadow callbacks replay the selected effects. Authored outer border space
  preserves content size where it fits, nested close preserves its parent, native
  text HWND/parent/value/selection persist, an application-owned body returns
  unparented without disposal, and native choice selection commits through the
  retained ComboBox. Member/group reset and public review controls/idempotence
  pass. Native shadow policy was enabled in these scopes.
  Package fixtures respect the existing unparented Content contract and test
  pixels outside the allocated shadow rectangle, not its final allowed pixel.
  No runtime workaround was introduced for either fixture correction; callback
  assertions are collected/logged outside paint callbacks to avoid JIT dialogs.
- Normal Release runtime/test and normal Debug runtime/designer server/client/
  permanent sample builds pass with zero warnings/errors. The in-place local
  package is `0.1.0-designer.local20261111T230703038Z`. New **Try popup frames**
  and **Popup frame effects** groups use **Theme effects 4**; earlier tabs retain
  seven/five/four groups and tab 4 has five. Rendered frames/review controls were
  inspected; groups outside the current width use the existing body scrollers.
  The user has not declared tab 4 full. Authored Forms/UserControls/resources/
  project files are retained.

D2 human appearance review is pending; earlier review scope remains in the
ledger. Actual VS, physical input/IME, visible frame pacing and mixed-monitor
checks remain separate. No full suite/native matrix or WPF build/test ran.
D3 ordinary menu decoration (headers, separators and scroll bands) is next
proposed; Application Menu interiors and later surfaces remain separate.
A05/A10/A17 and the Backstage closing-title ink flicker retain their deferred scope.


## Ordinary menu decoration D3 — 2026-11-12

The user confirmed D2 works perfectly and authorized continuation. Work started
clean on `codex/Ribbonkit-Winform` at their D2 commit `2d23145`. D3 extracts ordinary
menu headings, horizontal separators and Quick Access/More scroll-band paint into
`RibbonMenuDecorationRenderer`, with value-only `RibbonMenuAppearanceMetrics`
and explicit common primitive defaults. The actual neutral/Office renderer still
supplies its primitive radius and original heading palette fallbacks. Both light
themes share these structures; no family selector or layout engine was added.

`Theme.MenuAppearance` accepts immutable `RibbonMenuAppearanceSettings`:

- `HeaderCornerRadius` / `HeaderInset` / `HeaderBorderWidth` / `HeaderBorderColor`:
  independently authored heading shape and outline; ranges 0..64 / 0..1024 /
  0..32 logical pixels. Null keeps actual renderer corners and one-device-pixel
  inset/outline when ink exists. Empty border ink keeps the transparent outline;
  authored alpha is supported. Shape alone does not invent a neutral fill.
  Existing `MenuHeadingBackground` and `MenuHeadingForeground` remain in use.
- `HeaderBold`: null/true keeps host styles plus Bold; false removes Bold while
  preserving the other host styles. Existing header/text bounds remain unchanged.
- `SeparatorThickness` / `SeparatorColor`: optional centered line 0..32 logical
  pixels, clamped to its existing row; null retains one device pixel. Empty ink
  follows Border, zero/Transparent hides paint. Existing separator row height and
  horizontal padding retain their own roles.
- `OverflowBackground`: optional opaque solid/gradient band backing. Null retains
  opaque PopupBackground/Surface. Both stops must stay opaque so scrolling rows
  cannot show through; invalid alpha is rejected before theme mutation.
- `OverflowCornerRadius` / `OverflowInset` / `OverflowBorderWidth` /
  `OverflowBorderColor`: active-state decoration, with the same radius/inset/width
  ranges and null/Empty defaults as headings. Existing Hover/Pressed fills,
  Foreground/DisabledForeground and Chevrons remain authoritative. Disabled/idle
  bands keep their backing and glyph, without synthesizing active chrome.

All numeric values validate before Theme/BuiltInTheme mutation. Members reset
independently with null/Empty; `MenuAppearance = null` resets only this group.
Radii, insets and strokes clamp to available paint bounds. Row/header/band targets,
clipping, keyboard/automation, scrolling repeat and native ownership are unchanged.

The `DrawMenuHeader` -> actual `DrawGalleryHeading` extension path is preserved.
Built-in gallery-heading methods delegate only ordinary-menu contexts to the new
helper; real gallery headings keep their existing font/color/background behavior.
Scoped chrome still reaches actual protected DrawChrome and public DrawText hooks.
Application Menu headings use their separate path. A scoped Backstage separator
marker keeps the shared DrawMenuSeparator override while excluding new ordinary
menu color/thickness settings. Application Menu and vertical group separators
retain their previous recipes, including default device-pixel thickness.

Relevant evidence is under `artifacts/menu-decoration-d3/`:

- `tests/before-defaults.trx` / `tests/after-defaults.trx`: **80 exact PNG/geometry
  pairs**, both light palettes, neutral/Office renderers, Compact/Touch, four
  DPIs and 9/14-point fonts for paint, plus actual ordinary/overflow containers.
  The existing capture was expanded with both families and separator ink. Null
  and empty records match all saved defaults; no approvals/tolerances changed.
- `tests/menu-focused.trx`: **75 passed**, including 23 new cases for heading
  shape/ink/bold, separator center/alpha/hide/clamps and surface isolation,
  opaque gradient backing, endpoint masking, atomic range/alpha validation,
  immutable reset, actual renderer hooks and retained native rows/editor/UIA.
  The 52 retained cases comprise 40 Application Menu appearance cases plus 12
  menu/container/overflow/gallery cases. Together with the default case,
  **76 distinct relevant cases passed**. Earlier passed slices were not rerun.
- `package-final3/result.log`: **4/4 package-only scopes**, Office 2019/2024 Light
  x None/Mica at actual 120 DPI, each Classic/Simplified x Compact/Touch.
  Actual callbacks replay authored heading weight/outline, separator thickness/
  ink and band backing/state outline. Measured rows, native control/band geometry,
  editor HWND/parent/value/selection, nested owner state and Home/End endpoints
  remain intact; navigation invokes no commands. Member/group reset passes.
  The probe uses public keyboard/message routes (Escape, F10/0, Home/End), not
  unsupported MSAA child enumeration or direct ToolStrip.Close on an internally
  owned popup. Prior KeyTip hints are escaped before starting another session.
  Library tests retain UIA assertions; package geometry uses actual paint hooks
  and native bounds. No runtime keyboard/lifecycle workaround was introduced.
  Callback assertions are collected/logged without JIT dialogs.
- Normal Release runtime/test and normal Debug runtime/designer server/client/
  permanent sample builds pass without warnings/errors. Refreshed package:
  `0.1.0-designer.local20261111T233653771Z`. New **Try menu decoration** and
  **Menu decoration effects** groups remain on **Theme effects 4** (seven groups);
  earlier tabs retain seven/five/four groups. Public buttons/reset/idempotence,
  temporary toolbar entries and exact restoration of original toolbar identities/
  settings pass. The sample owns only its temporary entries and restores on Form
  close. Rendered menus/bands/review controls were inspected; keyboard captures
  include KeyTip hints. Use existing body scrollers for offscreen groups; the
  user has not declared this tab full. Authored Forms/UserControls/resources/
  project files remain intact.

D3 user review scope is recorded in the current ledger. Actual VS, physical
input/IME, visible frame pacing and mixed-monitor checks remain separate. No full
suite/native matrix or WPF build/test ran. D4a was subsequently delivered below;
galleries, tips and library dialogs retain later scopes. A05/A10/A17 and the
Backstage closing-title ink flicker remain separately deferred.

## Application Menu command-state and split chrome D4a — 2026-11-12

Work started clean on `codex/Ribbonkit-Winform` at the user's D3 commit
`b737db4`. D4a bounds Application Menu interior work to navigation/pane command
states, split halves/dividers and footer outlines. `RibbonApplicationMenuChromeRenderer`
shares the existing recipes over resolved paint metrics and explicit defaults.
Both light themes use the same structure; no family selector or layout engine
was added. Backgrounds, inner frame, headings and content measurement remain for
later bounded work. D2 still owns the shared outer popup frame.

`Theme.ApplicationMenuAppearance` accepts immutable
`RibbonApplicationMenuAppearanceSettings`:

- `RowCornerRadius` / `RowInset` / `RowBorderWidth` / `RowBorderColor`: active
  navigation/pane state shape, ranges 0..64 / 0..1024 / 0..32 logical pixels.
  Null retains square rows, a one-device-pixel inset and zero-width transparent
  outline. Set both width and ink to add an outline. Alpha is retained; shape
  does not activate idle rows or change content/focus/target bounds.
- `FooterInset` / `FooterBorderWidth` / `FooterBorderColor`: footer perimeter,
  ranges 0..1024 / 0..32 logical pixels. Null keeps one-device-pixel inset and
  outline, including the old transparent-border rim. Empty ink follows
  PopupBorder; explicit zero removes the outline. Existing
  `ApplicationMenuFooterCornerRadius` remains the radius owner.
- `SplitDividerWidth` / `SplitDividerInset` / `SplitDividerColor`: active seam,
  ranges 0..32 / 0..1024 logical pixels. Null retains the original one-device-pixel
  line and end clearances; Empty ink follows Border. Zero width/Transparent
  hides paint. Existing `ApplicationMenuSplitDividerVisible` remains authoritative,
  including Office 2019's false default. Authored insets clamp to half the row:
  the maximum leaves one centered pixel for an odd height, none for an even height.
- `SelectedPrimaryTintStrength`: finite 0..1 Pressed contribution to the open,
  unhovered split primary half; null keeps 0.25 and the original opaque channel
  mix. `SelectedPrimaryBackground` optionally supplies an alpha/gradient fill
  instead. Hover/press, expanded arrow and checked-state precedence are preserved.

All ranges validate before Theme/BuiltInTheme mutation. Members reset with
null/Empty; the group resets with `ApplicationMenuAppearance = null`. Other
settings, including divider visibility, remain independent. Authored dimensions
clamp inside existing paint bounds. Split halves clip one shared perimeter;
default divider rasterization retains the original Pen path. The extraction
retains Application Menu's direct rounded primitive path, rather than claiming a
new protected DrawChrome hook. Public item/split/text/image/arrow/focus callbacks
continue through the actual application renderer. Models, native editor/content
ownership, layout, input, validation and accessibility remain shared.

Evidence is under `artifacts/application-chrome-d4a/`:

- `tests/before-defaults.trx` / `tests/after-defaults.trx`: **32 exact PNG/expanded-target
  pairs**, both light palettes, flat/Office renderers, Compact/Touch and four DPIs.
  Ten states cover navigation/pane/footer, idle/hot/pressed/checked/disabled,
  selected splits, uniform hover and the transparent footer rim. Null and empty
  records match the saved defaults; no tolerance or approval changed.
- `tests/application-chrome-focused.trx`: **39 passed / 1 failed**. The failure was
  a new test expecting no divider for a clamped odd-height row; the existing safe
  clamp correctly leaves one pixel. Only that expectation changed.
  `tests/split-clamp-final.trx`: the affected **four cases passed**. Latest results
  cover **41 distinct passing cases** including the default case: 19 new shape/
  alpha/hide/clamp/tint/validation/reset/hook/native-state cases and 21 retained
  Application Menu appearance, hover, command/footer, borrowed-control and inert
  designer-preview cases. Passed cases were not repeated.
- `package-final3/result.log`: **4/4 public-package scopes**, both light families
  x None/Mica at actual 120 DPI, each Classic/Simplified x Compact/Touch. Actual
  active row/footer/split callbacks replay authored outline, divider and selected
  fill. Measured slots and surface sizes remain unchanged. Accessibility primary/
  footer invocation through WinForms AccessibleObject retains toggle, source
  identity and dismissal; this is not an external UIA client run. Borrowed native
  pane HWND/parent/value/selection survive pane switches and Theme reset; closing
  returns the undisposed content. The probe activates its offscreen owner before
  opening and respects idle-state eligibility; it explicitly enables the existing
  divider flag when checking authored seams. No runtime lifecycle workaround was
  added. Callback errors are collected outside native paint to avoid JIT dialogs.
- Normal Release runtime/test and normal Debug runtime/designer server/client/
  permanent sample builds passed. Refreshed package:
  `0.1.0-designer.local20261112T000940555Z`. New **Try Application Menu** and
  **Application Menu state effects** groups are on **Theme effects 5**; earlier
  tabs retain seven/five/four/seven groups. Public presets, independent fill/group
  reset, divider visibility, existing authored File/split preview, prior File-mode
  restore and idempotence pass. Menu/review captures were inspected. Use existing
  tab/body scrollers when needed; no earlier group was moved. Authored Forms,
  UserControls, resources and project files remain intact.

D4a user appearance review scope is recorded in the current ledger. External UIA,
actual VS, physical input/IME, visible frame pacing and mixed-monitor checks remain
separate. No full suite/native matrix or WPF build/test ran. D4b followed below.
Galleries, tips, library dialogs, A05/A10/A17 and the Backstage closing-title ink
flicker remain separate.

## Application Menu backgrounds, inner frame and headings D4b — 2026-11-12

Work continued on `codex/Ribbonkit-Winform` from the user's D4a commit `49329fe`,
preserving the uncommitted D4a review notes. D4b extracts background/inner-frame/
navigation-divider and heading recipes into `RibbonApplicationMenuSurfaceRenderer`
and resolved paint metrics with explicit common defaults. Both light families
share this structure; no selector or parallel layout engine was added.

The existing immutable `Theme.ApplicationMenuAppearance` group adds:

- `SurroundBackground`, `NavigationBackground`, `DefaultPaneBackground` and
  `ActivePaneBackground`: independent solid/gradient fills. Null retains the old
  surrounding/default-pane tint and PopupBackground/Surface navigation/selected
  pane. An opaque derived backing is established first; authored alpha composites
  over it through the existing layers. Native application Controls keep their own
  styling. `DefaultPaneTintStrength` is a finite 0..1 Border contribution; null
  retains 0.125 and the old opaque integer channel result. Explicit default-pane
  fill takes precedence for the pane; surrounding fill remains independent.
- `InnerFrameCornerRadius`, `InnerFrameBorderWidth`, `InnerFrameBorderColor`:
  ranges 0..64 / 0..32 logical pixels and alpha-capable ink. Null follows the actual
  renderer's popup radius minus one device pixel and a one-device-pixel Border
  stroke; Empty ink follows Border. Zero/Transparent hides paint. Resolved clips
  protect that stroke while retaining at least the old one-device-pixel inset.
  The background, heading and row paint use the same clip geometry; command
  targets, measured columns and native content bounds do not move.
- `NavigationDividerWidth`, `NavigationDividerInset`, `NavigationDividerColor`:
  ranges 0..32 / 0..1024 logical pixels. Null keeps the old one-device-pixel line
  and end clearances; Empty ink follows Border. Zero/Transparent hides paint.
  The measured divider position is retained and ink clips inside the inner frame.
- `HeaderCornerRadius`, `HeaderBold`, `DefaultHeaderTextInset`,
  `ActiveHeaderTextInset`, `HeaderRuleThickness`, `HeaderRuleColor`: square fill,
  host styles plus Bold, 2/10-logical-pixel text insets and one-device-pixel Border
  bottom rule at null. False removes Bold while retaining other host styles.
  Radius/inset/thickness ranges are 0..64 / 0..1024 / 0..32 logical pixels;
  authored text inset and rule thickness clamp inside the unchanged header.
  Zero/Transparent hides the rule; authored ink retains alpha. Existing
  MenuHeadingBackground/Foreground and selected-pane GalleryHeadingBackground
  keep their precedence and fallbacks.

All ranges validate before Theme/BuiltInTheme mutation. Members reset with
null/Empty; group Reset retains its existing meaning. Default Pen rasterization
is preserved for the navigation divider and heading rule. Actual public
DrawApplicationMenuBackground, DrawMenuHeader, DrawText and GetPopupCornerRadius
remain the entry/fallback owners; Application Menu still uses its direct rounded
primitive path rather than adding protected DrawChrome interception. Ordinary
menus, gallery headings, D4a row/split state and D2 outer frames retain their owners.

Evidence under `artifacts/application-surface-d4b/`:

- `tests/before-defaults-corrected.trx`, `tests/after-defaults.trx` and
  `tests/after-empty.trx`: **32 exact PNGs** across both families, flat/Office
  renderers, four DPIs and two font sizes, each containing default and selected
  panes/headings. Each pane is painted in its normal origin-zero surface before
  composition. Null and empty groups match saved defaults; no tolerances changed.
- `tests/surface-focused.trx`: **36 passed**, 17 new cases plus 19 retained
  cases. Coverage includes separate fills/tint endpoints, alpha, zero/hide,
  symmetric four-corner strokes, clipping, heading styles/insets/rules,
  ordinary-menu isolation, atomic range validation, immutable/reset/application
  restoration, retained native targets/content and inert designer previews.
  Together with the default-comparison case, **37 distinct cases passed**.
- `package-final2/result.log`: **4/4 public-package scopes**, both families x
  None/Mica at actual 120 DPI, each Classic/Simplified x Compact/Touch. Actual
  background/header/text callbacks replay authored fills, inner frame/divider,
  heading weight/insets and rule. Default/selected measured slots/sizes stay
  unchanged; native pane HWND/parent/value/selection/source survive switching and
  reset, and closing returns borrowed content undisposed. The diagnostic checks
  synchronous public model/paint/state operations before the normal queued
  dismissal when its offscreen host loses foreground. It does not alter runtime
  activation/lifecycle or claim foreground-input acceptance. Native callback
  failures are collected outside painting to avoid JIT dialogs.
- Normal Release runtime/test and Debug runtime/designer server/client/sample
  builds passed without warnings/errors. Refreshed package:
  `0.1.0-designer.local20261112T014918540Z`. **Application Menu surfaces** and
  **Application Menu headings** add two groups to **Theme effects 5** (four total).
  Earlier tabs remain seven/five/four/seven groups. Public presets, hide commands,
  independent surface/heading resets preserving D4a roles, authored File/default/
  selected previews, prior-mode restore and idempotence pass. Captured menus and
  review controls were inspected. Existing tab/body/group reduction remains in
  use; the user has not declared this tab full. Authored Forms/UserControls/
  resources/project files remain intact.

D4b user appearance review scope is recorded in the current ledger. External UIA, actual VS, physical input/
IME, visible frame pacing and mixed-monitor checks remain separate. No full suite,
native matrix or WPF build/test ran. D4c followed below.
Galleries, tips, library dialogs, A05/A10/A17 and closing-title ink flicker remain
separate.

## Application Menu content presentation D4c — 2026-11-12

Work started clean on `codex/Ribbonkit-Winform` at the user's D4b commit
`d30d11c`. D4c extracts content drawing into `RibbonApplicationMenuContentRenderer`
and component-scoped metrics with explicit defaults. Row/footer measurement,
navigation preferred width, split expansion and painting consume the same resolved
settings. Legacy summed rounding and differing null allowances remain compatible;
no layout engine, family selector, native editor manager or popup lifecycle changed.

`Theme.ApplicationMenuContent` accepts immutable `RibbonApplicationMenuContentSettings`:

- `LargeIconSize` / `SmallIconSize`: navigation/illustrated 32 and ordinary-pane/
  footer 16 logical pixels at null. Zero hides artwork; authored slots clamp inside
  the command. `LargeIconInset` / `SmallIconInset` default to 8/6. `IconTextGap`
  defaults to 8. `LargeTextInset` defaults to 52; authored content reserves artwork
  plus gap where present. `TextInset` defaults to 6 for icon-free ordinary rows.
  `TextTrailingPadding` preserves 6-pixel paint/8-pixel description-measurement
  allowances at null; explicit values unify them.
- `RowTextVerticalPadding` / `IconVerticalPadding`: total padding, defaults 10/8.
  Existing row minima remain authoritative; native fields retain their shared
  frame metrics. Authored icon/indicator sizes are included in measured rows and
  footer preferred width. `FooterHorizontalPadding` defaults to the old centered
  paint/preferred-width recipes; explicit values reserve symmetric padding around
  the measured label/artwork pack. Null keeps all old rounding/allowances.
- `DescriptionGap` / `DescriptionVerticalInset`: defaults 2/4. Explicit vertical
  inset is also reserved by row measurement. `DescriptionMaxLines` defaults to 2;
  zero hides descriptions and removes their measured height/gap. `RichTitleBold`
  null/true adds Bold for described illustrated choices; false removes Bold and
  preserves other host styles. Existing foreground/secondary/disabled ink and
  public text hooks retain their roles.
- `ArrowColumnWidth`: null keeps 26 Compact/44 Touch logical pixels. Authored
  values retain the 44-logical-pixel Touch minimum; split targets clamp to half
  the row. Actual source/action regions and arrow painting use the same width.
  Existing D1b filled-arrow/Chevrons settings still own the glyph itself.
- `SeparatorHeight` / `SeparatorHorizontalPadding` / `SeparatorThickness` /
  `SeparatorColor`: defaults 9/8 logical pixels, one device pixel, and Border ink.
  Zero height removes row space (or the footer separator's width); zero thickness/
  Transparent hides ink. Authored alpha is retained; inset/stroke clamp inside the
  existing slot. Ordinary menus and Backstage keep their own settings and recipes.

Dimensions are 0..1024 logical pixels, arrow width 1..1024, line cap/stroke 0..32.
Validation precedes Theme/BuiltInTheme mutation. Members reset independently with
null/Empty; `ApplicationMenuContent = null` resets only content, preserving D4a/D4b
appearance. Shared image/indicator/focus/arrow helpers remain actual renderer
hooks, including D1b disabled-raster treatment. An internal bridge retains the
protected DrawItemImage extension path; it does not add public interception APIs.

Package inspection exposed one missing context: the native popup separator branch
had sent ordinary-menu context even inside Application Menu. It now supplies the
measured ApplicationMenuPart, preserving actual DrawItem/DrawMenuSeparator hooks
and consuming the correct component settings. A live-surface regression checks
navigation, pane and footer markers plus actual authored ink. Empty/default
Application Menu separator paint remains the same centered device-pixel recipe.

Evidence under `artifacts/application-content-d4c/`:

- `tests/before-defaults.trx`, `tests/after-defaults.trx`, `tests/after-empty.trx`:
  **64 exact PNG/content-slot/measurement pairs**, both families, flat/Office
  renderers, both densities, four DPIs and two font sizes. Cases include rich/plain
  labels/descriptions, navigation/pane/footer, vector/enabled/disabled raster,
  check/radio, split/arrow/focus slots and separator paint. Saved logs also retain
  row/footer measurements and actual layout output. Null and empty records match;
  no tolerances/approvals changed. Later follow-ups preserve those null branches.
- `tests/content-focused.trx`: **36 passed**, initially 27 new and nine existing
  content/appearance/ownership/preview cases. `tests/content-preview-final.trx`
  passes the enhanced authored-content inert-preview case. `tests/live-separators-final.trx`
  adds one passing actual-surface case. `tests/footer-geometry-final.trx`: five
  passed, one new footer-indicator/Touch-minimum case plus four affected authored
  icon cases. `tests/footer-clamp-final.trx` passes the four authored-icon cases
  with an additional extreme footer-padding/artwork clamp check. Latest results
  cover **39 distinct passing cases** including the
  default comparison: 29 new + nine retained + default. Passed unrelated scopes
  were not repeated.
- `package-final3/result.log`: **4/4 package-only scopes**, both families x None/
  Mica at actual 120 DPI, each Classic/Simplified x Compact/Touch. Actual callbacks
  confirm authored icons, gutters, description gap, title weight, arrow targets
  and live separator ink/thickness. Reset restores original measured rows/sizes.
  AccessibleObject primary invocation preserves source action/dismissal; this is
  not an external UIA client run. Native field HWND/parent/frame height/value/
  selection and borrowed body HWND/parent/actual autoscaled bounds/value/selection
  survive switches/reset; closing returns borrowed content undisposed. Diagnostic
  borrowed images outlive the ribbon. Checks stay synchronous before the normal
  queued foreground-loss dismissal; runtime activation/lifecycle is unchanged.
- Release runtime/test and normal Debug runtime/designer server/client/sample
  builds passed. One refresh encountered a transient MSB3101 assembly-reference
  cache lock; the targeted final sample build passed with zero warnings/errors.
  Refreshed package: `0.1.0-designer.local20261112T022626671Z`. New **Application
  Menu content / Application Menu descriptions** groups make six on **Theme effects
  5**; earlier tabs remain seven/five/four/seven. Public presets, zero-description
  hide, member/group Reset preserving appearance, existing File/illustrated
  previews, prior-mode restore and idempotence pass. Final menus/review controls
  were inspected; group reduction remains in use and does not establish fullness.
  Authored Forms/UserControls/resources/project files remain intact.

D4c user appearance review scope is recorded in the current ledger. External UIA, actual VS, physical input/
IME, visible frame pacing and mixed-monitor gates remain separate. No full suite,
native matrix or WPF build/test ran. D5a followed below. Tips, library dialogs,
A05/A10/A17 and the Backstage
closing-title ink flicker remain separate.

## Gallery tile state and headings D5a — 2026-11-12

Work started clean on `codex/Ribbonkit-Winform` at the user's D4c commit `8d1151e`.
D5a bounds gallery work to built-in tile-state chrome and noninteractive gallery
headings. `RibbonGalleryRenderer` shares those recipes, with resolved paint
metrics and explicit primitive defaults. The neutral/Office entry points retain
their existing primitive radius, heading-fill and text-color fallbacks. Both
light themes use these existing structures; no family selector was added.

`Theme.GalleryAppearance` accepts immutable `RibbonGalleryAppearanceSettings`:

- `TileCornerRadius` / `TileInset` / `TileBorderWidth` / `TileBorderColor`: active
  tile-state decoration. Radius/inset/stroke ranges are 0..64 / 0..1024 / 0..32
  logical pixels. Null retains actual renderer corners, a one-device-pixel inset
  and a one-device-pixel outline when ink exists. Empty ink retains Accent on
  selected tiles and transparent ink otherwise. Explicit alpha is retained;
  zero/Transparent hides the outline. Idle tiles are not activated by shape or
  border settings. Pressed > Hover > Checked and disabled-state eligibility remain.
- `HeaderCornerRadius` / `HeaderInset` / `HeaderBorderWidth` / `HeaderBorderColor`:
  the same ranges and primitive fallback rules for headings. Empty ink retains a
  transparent outline; explicit ink can add a neutral heading outline without
  inventing a fill. Shape alone does not create neutral heading chrome.
- `HeaderBackground`: optional alpha/gradient fill for either renderer. Null
  retains no neutral fill and existing GalleryHeadingBackground for Office.
  Neutral SecondaryText and Office GalleryHeadingForeground remain the ink roles.
- `HeaderBold`: null/true adds Bold to host styles; false removes Bold while
  preserving other styles. Existing heading text padding and measured height are
  unchanged; heading Hot/Pressed/Focused flags are cleared as before.

Validation precedes Theme/BuiltInTheme mutation. Members reset with null/Empty;
`GalleryAppearance = null` resets only this group. Paint insets, strokes and
corners clamp inside existing bounds. Both tile and heading decoration enter the
actual protected DrawChrome override; inherited primitives consume the scoped
settings only when authored. A replacing override can veto built-in decoration.
Actual public tile/heading/text/focus and protected image hooks remain in use.
Empty records retain the old primitive path and exact rounding.

Tile artwork/label placement, aspect fitting, existing state fills, minimum rows,
gallery layout/targets, inline frame/utility drawing, selection/preview/commit,
scrolling and presenter/native ownership stay with their existing owners. The
ordinary menu heading path still reaches actual DrawGalleryHeading and then its
separate D3 helper. D5a settings do not leak into ordinary menus or strip chrome.
Application-owned presenter visuals remain application-owned and may explicitly
call shared public renderer hooks. Existing editable gallery cards are retained.

Evidence under `artifacts/gallery-appearance-d5a/`:

- `tests/before-defaults.trx`, `tests/after-defaults.trx`, `tests/after-empty.trx`:
  **64 exact PNG/content-slot/layout pairs**, both families, flat/Office renderers,
  both densities, four DPIs and two font sizes. Nine tile states include selected,
  hot, pressed, disabled and focus combinations with raster artwork; real headings
  and ordinary-menu fallback are captured. Logs retain text/image/focus slots and
  grouped-grid targets/headings. Null and empty records match saved defaults;
  no approvals/tolerances changed.
- `tests/gallery-focused.trx`: **25 passed**, 16 new shape/state/alpha/hide/clamp/
  four-corner/content-slot/heading-weight/isolation/validation/reset/actual-override
  cases plus nine retained heading/artwork/preview/inline/QAT/presenter/designer
  cases. `tests/authored-preview-final.trx`: the two affected designer discovery/
  preview cases pass with authored gallery settings, without constructing user
  controls. Including the default-comparison case, **26 distinct cases passed**.
  Passed unrelated scopes were not repeated.
- `package/result.log`: **4/4 package-only scopes**, both families x None/Mica at
  actual 120 DPI, each Classic/Simplified x Compact/Touch. Actual tile/heading/
  protected-chrome/text callbacks replay authored outline/fill/weight. Popup and
  inline measured slots and source selection remain unchanged. The same custom
  inline presenter and editor HWND/parent/value/selection survive theme/reset/mode
  changes; two active presenter preview/cancel/commit sequences per scope reach
  original source events once. Member/group Reset passes. These are synchronous
  offscreen checks before normal queued foreground-loss dismissal, with no
  activation/lifecycle workaround or external UIA claim.
- Release runtime/test and normal Debug runtime/designer server/client/sample
  builds passed without warnings/errors. Refreshed package:
  `0.1.0-designer.local20261112T031657546Z`. New **Try built-in galleries / Gallery
  tile states / Gallery headings** groups are on **Theme effects 6**. The existing
  Application Menu controls/verifiers are pinned through ThemeEffects5ReviewTab;
  earlier tabs retain seven/five/four/seven/six groups. Built-in popup/inline
  previews, public presets/hide/independent resets, source selection and
  idempotence pass. Captured menus and review controls were inspected. Use existing
  tab/body/group scrolling and reduction when needed. Authored Forms/UserControls/
  resources/project files remain intact.

D5a user review is recorded in the current ledger. External UIA, actual VS, physical input/
IME, visible frame pacing and mixed-monitor acceptance remain separate. No full
suite/native matrix or WPF build/test ran. D5b followed below. Tips, library
dialogs, A05/A10/A17 and the Backstage closing-title ink flicker remain separate.

## Gallery content and inline frame utilities D5b — 2026-11-12

Work started clean at the user's D5a commit `9b45f12` on
`codex/Ribbonkit-Winform`. `RibbonGalleryContentRenderer` shares built-in tile
content and inline utility glyph recipes; `RibbonGalleryContentMetrics` supplies
paint slots and popup minimum-height inputs over the existing gallery layout.
The D5a chrome helper now also scopes strip/utility settings through the actual
protected DrawChrome override. Both light themes reuse these structures without
a new family selector.

`Theme.GalleryContent` accepts immutable `RibbonGalleryContentSettings`:

- `LabelMaxLines`: 0..32, null two. Zero hides labels and removes their measured
  line allowance. Inline labels clamp to the fixed native viewport.
- `LabelAlignment`: any defined ContentAlignment, null MiddleCenter; existing
  wrap/ellipsis flags and label slot remain in use.
- `ArtworkHeightReserve`: 0..1024 logical pixels, plus triple existing TilePadding.
  Null preserves the legacy 12 paint / 24 popup-measure allowances; an explicit
  value unifies those allowances. `ArtworkHorizontalInset` / `ArtworkTopInset`
  are 0..1024, null TilePadding + 4 / + 2. Explicit top inset also adjusts popup
  minimum height. Paint slots clamp inside tile bounds; aspect fitting remains.
- `ExpandGlyphHeight` / `ExpandBarWidth`: 0..64, null 18 / 10. Zero height hides
  expand chevron and bar; zero width hides only the bar. `ExpandBarInset` is
  0..1024, null 4; consuming the slot hides the bar. `ExpandBarStrokeWidth` is
  finite 0..32, null the existing rounded two-pixel stroke with one-device-pixel
  minimum. Explicit zero hides it; authored bar geometry clips to the action.
- `UtilityGlyphColor`: Empty keeps enabled Foreground / DisabledColor; alpha is
  honored and Transparent hides glyph paint. Existing Chevrons retains arrow
  length/depth/stroke; Up/Down slots and all action targets remain unchanged.

`GalleryAppearance` adds independently resettable `StripCornerRadius`, `StripInset`,
`StripBorderWidth`, `StripBorderColor`, `StripBackground`, plus `UtilityCornerRadius`,
`UtilityInset`, `UtilityBorderWidth`, `UtilityBorderColor`. Radius/inset/stroke
ranges are 0..64 / 0..1024 / 0..32. Null preserves actual primitive corners and
one-device-pixel inset/stroke when ink exists. Empty strip ink follows PopupBorder;
null fill follows Surface. Empty utility ink remains transparent; Hover/Pressed
fills and disabled eligibility retain existing policy. Alpha, zero/hide and
clamps are supported. Paint decoration does not move native padding/targets.

Validation precedes Theme/BuiltInTheme mutation. Members reset with null/Empty;
`GalleryContent = null` preserves GalleryAppearance, and scoped frame resets
preserve D5a tile/heading settings. Actual public tile/frame/utility/text/focus
and protected image/chrome hooks remain authoritative, including replacement
overrides. D1b raster treatment remains shared; borrowed images outlive consumers.
Application presenter visuals, source selection, preview/cancel/commit, native
controls, scrolling, inline targets and editor lifecycle remain with their owners.

Evidence under `artifacts/gallery-content-d5b/`:

- `tests/before-defaults.trx`, `after-defaults.trx`, `after-empty.trx`: **32 exact
  PNG/content-slot/layout pairs**, both light families, flat/Office renderers,
  Compact/Touch and four DPIs. Each image contains 9/14-point raster/vector/disabled
  content, strip frame and Up/Down/Expand idle/hot/pressed/disabled states. Null
  and both empty groups match the saved defaults; no approvals/tolerances changed.
- `tests/gallery-content-focused.trx`: 43 passed / one new glyph-test assertion
  failed because it required an exact opaque pixel from an antialiased Chevron
  at 96 DPI. The assertion now checks visible ink; `glyph-ink-final.trx` passes
  the four affected DPI cases. No runtime change was required for that failure.
  `authored-preview-final.trx` passes the two affected designer discovery/draft/
  preview/apply/reset cases with authored content/frame settings, without creating
  user Controls. The **45 distinct latest passing cases** comprise 27 new authored,
  17 retained and one default-comparison case. Passed unrelated scopes were retained.
- `package/result.log`: **4/4 package-only scopes**, both light families x None/
  Mica at actual 120 DPI, each Classic/Simplified x Compact/Touch. Actual content/
  image/text/frame/utility/chrome callbacks verify alignment, reserve, aspect
  fitting and authored outlines/glyphs. Reset restores popup geometry; inline
  targets remain unchanged. The same custom presenter/editor HWND/parent/value/
  selection and source selection survive settings/reset/mode changes. Two active
  preview/cancel/commit sequences per scope reach original source events once.
  These are synchronous offscreen checks before queued foreground-loss dismissal,
  with no runtime activation/lifecycle workaround or external UIA claim.
- Release runtime/test builds pass without warnings/errors; normal Debug runtime/
  designer server/client/sample refresh succeeds. Local package:
  `0.1.0-designer.local20261112T045453047Z`. The four new review groups are on
  **Theme effects 7**; D5a is pinned through ThemeEffects6ReviewTab. Earlier tabs
  retain seven/five/four/seven/six/three groups. Public presets/hide/partial/group
  Reset, preview source selection and idempotence pass. Representative popup,
  inline and review-tab captures were inspected. Authored Forms/UserControls/
  resources/project files remain intact.

D5b user review is recorded in the ledger. Actual VS, physical
input/IME, visible frame pacing, mixed-monitor and external UIA gates are separate.
No full suite/native matrix or WPF build/test ran. D6 followed below; library
dialogs, A05/A10 and Backstage closing-title ink remain separate.

## Tip presentation and Backstage badge placement D6 — 2026-11-12

Work started clean at the user's D5b commit `3bc3680`. `RibbonTipRenderer` shares
ScreenTip/KeyTip recipes over the actual public renderer; `RibbonTipMetrics`
consumes explicit resolver defaults and supplies title weight, paint bounds,
outline stroke and Backstage placement. Both light families use these structures;
no family selector or native manager/keyboard rewrite was added.

`Theme.TipAppearance` accepts immutable `RibbonTipAppearanceSettings`:

- `ScreenTipTitleBold`: null/true adds Bold to host styles; false removes it while
  preserving other styles. The same resolved style measures and paints the title.
- `KeyTipInset`: 0..1024 logical pixels, null zero. `KeyTipBorderWidth`: 0..32,
  null one device pixel; zero hides the outline. Paint dimensions clamp inside
  the badge without moving text/command targets. Existing alpha/gradient palette
  roles and Office KeyTipCornerRadius remain; neutral badges retain square corners.
- A17 `BackstageKeyTipRightClearance` / `BackstageKeyTipHorizontalClearance` /
  `BackstageKeyTipVerticalClearance`: 0..1024, null 4/8/4 logical pixels. Horizontal/
  vertical values are total available-width/height deductions, independently of
  existing badge text padding. Zero permits the full dimension; consuming it hides
  the badge. Legacy summed fractional-DPI rounding and short-slot clamps remain.

Validation precedes Theme/BuiltInTheme mutation. Null resets each member;
`TipAppearance = null` preserves existing tip colors/corners/padding and unrelated
gallery settings. Actual DrawScreenTip/DrawKeyTip/DrawText overrides remain
authoritative. Badges retain the direct rounded primitive path; no new protected
DrawChrome interception was introduced. ScreenTip frame/shadow/clearance uses D2's
existing public hooks. Passive delay/lifetime, keyboard/accessibility routes,
native page/editor ownership and source metadata remain shared.

Evidence under `artifacts/tip-appearance-d6/`:

- `tests/before-defaults.trx`, `after-defaults.trx`, `empty-and-preview.trx`:
  **32 exact default PNG/text-slot/measurement pairs**, both families, flat/Office,
  both densities and four DPIs, each image with 9/14-point content. Null and empty
  settings match saved defaults without changing approvals/tolerances.
- `tests/tip-focused.trx`: **28 passed**, including 20 new authored title/font/
  measurement, outline/alpha/hide/clamp/four-corner/actual-hook, A17 legacy-rounding/
  short-target, atomic-validation/Reset and actual Backstage/passive-window native
  ownership cases; eight retained tip/geometry/native-lifetime cases also pass.
  `empty-and-preview.trx` adds two candidate-theme preview cases with authored tip
  settings and no application page paint, commands or ownership changes. Including the default case,
  **31 distinct cases passed**. Final review added an explicit empty-badge paint
  guard so a consuming clearance hides both outline and text. `zero-slot-final.trx`
  passes the four affected badge/DPI cases and the unchanged-default comparison;
  other passing scopes were retained. Release builds pass without warnings/errors.
- `package-current/result.log`: **4/4 public-package scopes**, both families
  x None/Mica, actual 120 DPI, each Classic/Simplified x Compact/Touch. Actual root/
  Backstage badge/text callbacks verify authored outlines/right clearance; command,
  root-badge and page bounds stay unchanged. The same native editor HWND/parent/
  value/selection and selected page survive settings/reset. Reset restores exact
  placement. The fixture establishes native focus before the public F10/key route;
  an initial fixture failure without that focus was corrected in the fixture only.
  Public ScreenTip rendering is checked here; real passive-window focus/ownership
  is covered by STA, with no physical-hover claim. Callback failures are reported
  outside paint. Captures render the Backstage control itself rather than relying
  on whole-Form DrawToBitmap child composition. Mica offscreen capture is not live
  material/frame-pacing evidence.
- Normal Debug runtime/designer server/client/sample builds pass without warnings/
  errors. A sample-only override-signature compile error was corrected; passing
  runtime/designer builds were retained. Refreshed package:
  `0.1.0-designer.local20261112T054412038Z`. **Try tips and badges / Tip presentation**
  are on **Theme effects 7**, now six groups; all earlier tabs retain their groups.
  Presets/hide/placement and whole-group Reset, File preview/restore and idempotence
  pass. Representative badge/content/review captures were inspected. Authored
  Forms/UserControls/resources/project files remain intact.

D6 user review is recorded in the ledger. Physical hover/input/IME,
actual VS, mixed-monitor, visible frame pacing and external UIA are separate.
No broad suite/native matrix or WPF build/test ran. D7a followed below; A05
caption/title tint, A10 page minimum preference and the deferred Backstage
closing-title ink flicker remain separate.

## Options navigation and dividers D7a — 2026-11-12

Work started clean at the user's D6 commit `5e5f36d`. D7 is bounded first to the
library Options navigation rail and footer rules. `RibbonOptionsRenderer` shares
the existing row/marker/text/focus and divider recipes; value-only
`RibbonOptionsAppearanceMetrics` consumes explicit resolver defaults. Both light
families reuse those recipes with no new family selector or dialog coordinator.

`Theme.OptionsAppearance` accepts immutable `RibbonOptionsAppearanceSettings`:

- `NavigationCornerRadius` / `NavigationSelectionInset`: 0..64 / 0..1024 logical
  pixels. Null uses actual GetPopupCornerRadius / two-pixel vertical selection inset.
- `NavigationTextPadding` / `NavigationFocusInset`: 0..1024, null 12 logical pixels
  horizontally / one device pixel for native focus. Authored values clamp inside
  existing rows. Native target bounds and focus style do not change.
- `NavigationSelectedBackground`: optional alpha/gradient fill, null Checked.
  Existing BackstageNavigationBackground, selected Accent and ordinary Foreground
  remain the rail/text roles.
- `NavigationMarkerWidth` / `NavigationMarkerInset` / `NavigationMarkerFill`: width
  0..64, null 3; per-end inset 0..1024, null legacy top 7 / summed 14 height deduction;
  fill null Accent. Existing NavigationMarkerCornerRadius remains. Zero width,
  Transparent fill or a consuming inset hides marker paint. Authored insets use
  equal ends; legacy ceiling-based fractional-DPI rounding remains at null.
- `DividerWidth` / `DividerColor`: width 0..32, null one device pixel; Empty ink
  follows Border. Zero/Transparent hides paint and alpha is retained. Authored
  stripes clamp inside unchanged rail/footer panels; null keeps the legacy GDI line.

Validation precedes Theme/BuiltInTheme mutation. Members reset with null/Empty;
`OptionsAppearance = null` preserves existing button corners, palette and tip/
gallery settings. The actual popup-radius hook remains. Native Options text
continues through its direct painter; no new DrawText/command-chrome interception
was introduced. The dialog retains its rounded-painter cache and disposal.
Dialog sizing, heading typography, buttons/fields/lists/choices, application page
styling, native handles and validation/Apply/Cancel lifetime retain their owners.

Evidence under `artifacts/options-navigation-d7a/`:

- `tests/before-defaults.trx`, `after-defaults.trx`, `empty-defaults.trx`: **16 exact
  native PNG/control-geometry pairs**, both light families, flat/Office, both
  densities and 9/14-point fonts at actual 120 DPI. Captures invoke actual native
  ListBox owner-draw and rail/footer paint callbacks; null and empty groups match
  saved paint, dialog/page/editor/row/footer metrics with no approval changes.
- `tests/options-focused.trx`: **30 passed / one retained choice case failed**.
  An isolated `choice-final.trx` also failed at a different focus/selection assertion.
  The fixture now keeps the owner offscreen, drains initial Show work and explicitly
  establishes combo focus before opening. `choice-owner-final.trx` passes that
  affected case; no Options popup/choice runtime code changed. Together with the
  default comparison, **32 distinct latest cases pass**: 22 new authored and nine
  retained cases plus one default case. This is not a clean initial aggregate run;
  other passing scopes were retained. Release builds pass without warnings/errors.
- New cases cover selection shape/alpha, marker hide, native focus inset, divider
  orientation/alpha/zero/clamps, actual radius-hook and direct-text precedence,
  validation/reset and retained native controls/checkpoints. Four-corner paint,
  short-target clamps and legacy ceiling/summed-marker arithmetic pass at
  96/120/144/192 DPI. This is emulated-DPI paint/metric evidence, separate from the
  native 120-DPI captures and mixed-monitor acceptance.
- `package-current/result.log`: **4/4 public-package scopes**, both light families
  with None/Mica owner windows at actual 120 DPI, each Classic/Simplified x
  Compact/Touch. Actual native navigation/rail/footer paint and radius hooks,
  authored fill/marker/rules and exact native Reset pixels pass. Dialog/page/row/
  editor bounds and HWND/parent/value/selection stay unchanged. Invalid Apply/OK,
  valid Apply and Cancel checkpoints pass. Built-in customization retains its
  pages, native controls, pending Undo draft and live Save QAT source through
  appearance reset; Cancel does not apply the draft. These are synchronous
  offscreen checks, without a foreground/input/IME/UIA claim.
- Normal Debug runtime/designer server/client/sample builds pass without warnings/
  errors; local package `0.1.0-designer.local20261112T061645079Z` refreshes the
  permanent sample. **Try Options dialogs / Options navigation** are on **Theme
  effects 7**, now eight groups; earlier tabs remain intact. Public presets/hide,
  marker/group resets, two-page preview and idempotence pass. Captures use full
  window bounds so native title/footer regions are retained; representative
  navigation, Options/customization and review-tab images were inspected.
  Authored Forms/UserControls/resources/project files remain intact.

D7a user review is recorded in the ledger. Physical input/IME,
actual VS, external UIA, mixed-monitor and visible pacing remain separate. No broad
suite/native matrix or WPF build/test ran. D7b followed below. A05/A10 and the
Backstage closing-title ink flicker remain separate.

## Options buttons and heading typography D7b — 2026-11-13

Work started clean at the user's D7a commit `1779dcf`. Build, comparison and package
evidence below was captured on 2026-11-12; the delivery record was finalized on
2026-11-13. `RibbonOptionsButtonRenderer` shares the existing native button recipe
over the control's owned rounded painter. `RibbonOptionsShellMetrics` resolves
button paint/focus bounds and heading font/padding from explicit defaults.
No new family selector, command-renderer interception or dialog coordinator was added.

`OptionsAppearance` adds independently resettable roles:

- `ButtonInset` / `ButtonBorderWidth` / `ButtonBorderColor` / `ButtonFocusInset`:
  inset/focus ranges 0..1024 and stroke 0..32 logical pixels. Null retains zero
  paint inset, ordinary one-device-pixel border when ink exists, accent border
  zero and focus inset four device pixels. Empty ink keeps PopupBorder / accent
  fill's first color. Alpha, zero/hide and clamps are supported; content/hit bounds
  remain unchanged. Existing OptionsButtonCornerRadius and actual fallback radius
  remain authoritative.
- `ButtonBackground` / `AccentButtonBackground`: optional enabled idle alpha/
  gradient fills, null Surface / Accent. Existing Hover/Pressed/disabled recipes
  and foreground roles remain. A null-corner ordinary button keeps native paint
  until an ordinary-button override is authored. Accent-only fill, headings and
  navigation do not opt it into shared paint; member Reset restores native paint.
- `HeadingFontSizeIncrement` / `HeadingMinimumFontSize`: finite 0..64 / 1..128
  points, null 5 / 14. `HeadingBottomPadding`: 0..1024 logical pixels, null 12;
  zero removes the gap. Null `HeadingBold` keeps the legacy Bold-only style;
  explicit true adds Bold to host styles, false removes Bold and keeps other styles.
  Heading font and gap use the existing construction/load/font/DPI layout and
  page arrangement. Choose settings before opening; no live theme coordinator
  was introduced. Existing owned-font retention/disposal remains.

Validation precedes Theme/BuiltInTheme mutation. Native Button properties,
image borrowing/disabled treatment, text alignment, direct text/focus painting,
keyboard/IButtonControl/default-cancel ownership and validation remain shared.
Partial button resets retain headings/navigation; heading resets retain buttons/
navigation. Whole `OptionsAppearance = null` retains existing corners/palette and
unrelated tip/gallery settings. Field/list frames and choice input remain separate.

Evidence under `artifacts/options-shell-d7b/`:

- `tests/before-defaults.trx`, `after-defaults.trx`, `empty-defaults.trx`: **16 exact
  native PNG/control-geometry pairs**, both light families, flat/Office, both
  densities and 9-point regular / 14-point italic host fonts at actual 120 DPI.
  Actual button callbacks cover ordinary/accent idle/hot/pressed/disabled states
  and borrowed raster artwork; actual heading fonts/padding and native dialog/
  page/footer metrics are retained. Null/empty settings match the saved result;
  no approvals/tolerances changed.
- `tests/shell-focused.trx`: **22/22 passed**, 13 new button shape/state/alpha/
  hide/clamp/four-corner/focus, scoped native fallback/image ownership/reset,
  atomic size/range/finite validation, heading style and native owner/draft cases,
  plus nine retained navigation/geometry/validation/Apply/Cancel cases. Including
  the default comparison, **23 distinct cases passed**. Authored button paint/
  focus is checked at 96/120/144/192 DPI. Release builds pass without warnings/errors.
- `package/result.log`: **4/4 public-package scopes**, both light families with
  None/Mica owner windows at actual 120 DPI, each Classic/Simplified x Compact/Touch.
  Actual native ordinary/accent outline/fill and heading size/weight/padding pass.
  Button reset retains native targets, handles, parent, value, selection, selected
  page and default/cancel owners. Invalid Apply/OK, valid Apply and Cancel checkpoint
  checks pass. Heading defaults restore on reopening. Accepted navigation survives
  partial resets; built-in customization retains pending Undo draft, live Save QAT
  source and native controls. These are synchronous offscreen checks, with no
  physical-input/IME, actual-VS, UIA or live-material/frame-pacing claim.
- Normal Debug runtime/designer server/client/sample builds pass without warnings/
  errors; local package `0.1.0-designer.local20261112T064751236Z` refreshes the
  permanent sample. Requested new **Theme effects 8** contains **Try Options shell /
  Options buttons / Options headings**. Accepted D5b/D6/D7a builders and verifiers
  are pinned through ThemeEffects7ReviewTab; tabs 1–7 retain seven/five/four/seven/
  six/three/eight groups. Presets/hide/independent resets, preview and idempotence
  pass; representative dialog/customization/new-tab captures were inspected.
  Authored Forms/UserControls/resources/project files remain intact.

D7b user appearance review is recorded in the ledger. Physical input/IME, actual
VS, external UIA, mixed-monitor and visible pacing remain separate. No broad
suite/native matrix or WPF build/test ran. D7c followed below. A05/A10 and Backstage
closing-title ink remain separate.

## Options frames and choice presentation D7c — 2026-11-13

Work started clean at the user's D7b commit `c97c30a`. Shared
`RibbonOptionsFieldRenderer` and value-only `RibbonOptionsFieldMetrics` resolve
the existing field/list wrapper and closed-choice presentation. There is no new
family selector, native editor replacement or dialog/theme coordinator.

`OptionsAppearance` adds three independently resettable members:

- `FrameCornerRadius`: 0..64 logical pixels. Null retains the actual popup-radius /
  command-radius minimum. Native list/tree/icon-list child regions consume the
  same resolved corner and content clearance.
- `FrameBorderWidth`: 0..32 logical pixels. Null retains one device pixel; zero
  hides the stroke but retains minimum one-device-pixel native clearance. Authored
  wider outlines reserve real wrapper Padding and enough search/name-field height
  for the native TextBox. Width clamps to the available wrapper. Reset restores
  the prior native padding, including scaling performed while an override was active.
- `FrameBorderColor`: Empty follows PopupBorder. Alpha and Transparent are
  supported; hidden ink retains its reserved content clearance. Existing Surface
  and native child background owners remain.

Existing `EditorTextInset` / `EditorForeground` now style search/name text and
closed-choice values. Search null keeps four logical pixels with each owner's
legacy ceiling/summed rounding; closed-choice null keeps six logical pixels.
Enabled choice ink follows EditorTextColor; disabled ink retains SystemColors.GrayText.
Existing EditorAppearance field inset/stroke also clear the closed-choice value;
no duplicate Options choice API was introduced. Actual DrawComboBox / DrawText,
arrow/button geometry and D1c popup-row roles remain authoritative. Zero-size text
slots skip painting. Icon-picker selected-tile chrome remains a separate owner.

Validation precedes Theme/BuiltInTheme mutation. Native handles/parent, value,
selection, validation, draft and Apply/Cancel, staged preview/Escape/Enter and
popup lifetime retain their owners. Choose geometry settings before opening;
existing layout/font/DPI refresh consumes them. This does not introduce live
remeasurement of every open dialog or application-owned page styling.

Evidence under `artifacts/options-fields-d7c/`:

- `tests/before-defaults.trx`, `after-defaults.trx` and
  `empty-and-wide-native.trx`: **32 exact native PNG/control-geometry/region/text-slot
  pairs**, both light families, flat/Office renderers, Classic/Simplified,
  Compact/Touch and 9/14-point fonts at actual 120 DPI. Null and empty groups match
  saved frame/choice paint and geometry without approval/tolerance changes.
- `tests/fields-focused.trx` **24/24**, `empty-and-wide-native.trx` **5/5** and
  `scaled-padding-final.trx` **2/2** pass. Together, **28 distinct latest cases**
  pass: 17 new authored cases, ten retained Options/editor/choice cases and one
  default comparison. The expanded four-case native theory supersedes its two
  earlier mode-only display names; those old results are not additional cases.
  Coverage includes frame shape/alpha/hide/four corners, tiny clamps, legacy search
  rounding, actual choice hooks/editor roles/field clearance, atomic range checks,
  native reset/regions/handles and scaled padding. Real customization and group-name
  inputs fit both 8-pixel/9-point and maximum 32-pixel/14-point profiles. Authored
  paint/metrics cover 96/120/144/192 DPI; search rounding also covers 110/125 DPI.
  Release builds pass without warnings/errors.
- `package/result.log`: **4/4 public-package scopes**, both light families with
  None/Mica owner windows at actual 120 DPI, each Classic/Simplified x Compact/Touch.
  Actual native outline/child clearance/regions, editor ink/value slots, retained
  HWND/parent/draft/live QAT sources, Cancel and independent/group Reset pass.
  Eight native choice preview/Escape/Enter sequences retain staging and emit one
  commit event each. The offscreen fixture establishes focus and checks synchronous
  state before ordinary queued foreground-loss dismissal; no runtime activation or
  choice-lifetime workaround was added.
- Normal Debug runtime/designer server/client builds pass without warnings/errors.
  A sample diagnostic's source-type compile error was corrected; the passing
  builds were retained and only package/sample refresh was repeated. Package
  `0.1.0-designer.local20261113T015240299Z` and the sample Debug build pass without
  warnings/errors. **Try frames and choices / Options frames and text** are on
  **Theme effects 8**, now five groups. The preview uses isolated application-owned
  sample models and the public customization dialog, preserving the real ribbon's
  mode and sources. Presets/hide/independent resets and idempotence pass. Earlier
  tabs retain their groups. Representative preview/page/choice/row/tab captures
  were inspected; authored Forms/UserControls/resources/project files remain intact.

D7c user appearance review is recorded in the ledger. Physical input/IME, actual VS,
external UIA, mixed-monitor and visible pacing remain separate. No broad suite,
native matrix or WPF build/test ran. D7d followed below. A05/A10 and Backstage
closing-title ink remain separate; no new theme family is selected.

## Customization command-list rows D7d — 2026-11-13

Work started clean at the user's D7c commit `1505210`. Source review selected the
shared QAT/Classic/Simplified command-list recipe; native tree decoration and
icon-picker tiles remain separate. `RibbonOptionsRowRenderer` shares selection,
borrowed artwork and direct native text drawing over unchanged native rows.
`RibbonOptionsRowMetrics` supplies resolved icon/text slots from explicit defaults.
The existing native DrawFocusRectangle call stays with each owner; no renderer
interception, new family selector, native control replacement or coordinator was added.

`OptionsAppearance` adds independently resettable roles:

- `RowTextPadding` / `RowIconTextGap`: 0..1024 logical pixels. Null retains QAT/
  Classic 6 and Simplified 8 with legacy ceiling rounding. Authored values use
  shared logical scaling and clamp to content space. Zero removes the padding/gap.
- `RowIconSize`: 0..256 logical pixels. Null retains 20 with legacy ceiling rounding;
  authored sizes clamp inside the available width and existing native row height.
  Zero hides vector/group/raster paint and reclaims both the icon slot and its gap.
  Missing artwork retains its slot unless zero is authored, preserving defaults.
- `RowCornerRadius`: 0..64 logical pixels; null/square default is zero. Selection
  shape does not change native row targets, heights or focus rectangles.
- `RowSelectedBackground`: optional alpha/gradient fill. Null retains the solid
  Checked.Start recipe; authored selection paint composites over Surface and
  Transparent hides it. Existing Foreground and unselected Surface remain.
- `RowHeadingBold`: null retains Simplified's legacy Bold-only row-header style;
  explicit true adds Bold to host styles, false removes Bold while preserving other
  styles. Existing owned fonts and construction/font/DPI layout remain; choose
  heading presets before opening. Dialog title typography remains independent.

Validation precedes Theme/BuiltInTheme mutation. Member/content/selection resets
retain each other and earlier frame/button/navigation roles; whole OptionsAppearance
Reset retains unrelated palette/tip settings. Native handles/parent/selection,
row geometry/keyboard focus, source icon/image ownership, validation and isolated
draft Apply/Cancel remain with their existing owners. The tree's explicit image
slots reuse the unchanged icon-drawing primitive without consuming list row metrics.

Evidence under `artifacts/options-rows-d7d/`:

- `tests/before-defaults.trx`, `after-defaults.trx` and the empty-group comparison
  in `tests/rows-focused.trx`: **16 exact native PNG/control-geometry/icon-slot
  pairs**, both light families, Classic/Simplified, Compact/Touch and 9/14-point
  italic fonts at actual 120 DPI. Actual ListBox DrawItem callbacks cover all
  command-list owners, selected/unselected/focused/NoFocusRect states, vector/group/
  raster/plain rows and Simplified headers. No approval/tolerance changes.
- `tests/rows-focused.trx`: **24/24 passed**, 16 new authored cases, seven retained
  Options/customization/validation/ownership cases and one default comparison.
  New cases cover gradient/alpha/Transparent, four matching corners, safe small-row
  slots/zero icon reclamation, ceiling rounding, heading styles, atomic range checks,
  member/group/built-in Reset, actual native icon routing/hide and retained row
  geometry/handles/parent/selection/drafts/sources. Authored paint/slots cover
  96/120/144/192 DPI; rounding also covers 110/125 DPI. Release builds pass without
  warnings/errors. Initial baseline fixture reference-type compile errors were
  corrected before the successful baseline; runtime paint was unchanged then.
  Final review tightened padding when only icon size/gap is authored on a tiny
  row. `tests/narrow-single-overrides-final.trx` passes the four affected cases,
  expanded to cover each isolated content override as well as combined settings.
  There are still **24 distinct latest passing cases**; prior passing scopes were retained.
- `package-final/result.log`: **4/4 public-package scopes**, both light families with
  None/Mica owner windows at actual 120 DPI, each Classic/Simplified x Compact/Touch.
  Across **56 native list paints**, actual icon sizes/zero hide, selection fill/
  Transparent, direct native text and retained HWND/parent/bounds/row height/
  selection pass. Pending QAT Add remains isolated; Cancel preserves live sources.
  Partial/group Reset retains earlier roles and borrowed raster ownership.
- Normal Debug runtime/designer server/client/sample builds pass without warnings/
  errors. A sample-only diagnostic reference to an internal change flag was replaced
  by a public draft-count check; passing runtime/designer builds were retained.
  Final narrow-row changes were rebuilt in normal Debug, the package/sample refreshed
  and the four package scopes rechecked. Package
  `0.1.0-designer.local20261113T022152586Z` refreshes the permanent sample.
  The requested new **Theme effects 9** has **Try command rows / Command row content /
  Command row selection**, three groups. D7b/D7c builders/verifiers are pinned to
  ThemeEffects8ReviewTab, retaining its five groups. Earlier tabs remain intact.
  Public presets/hide/content/selection/all-row resets, two isolated mode previews,
  real ribbon mode/sources and idempotence pass. Representative row/preview/tab
  captures were inspected. Authored Forms/UserControls/resources/project files remain intact.

D7d user appearance review is recorded in the ledger. Physical input/IME, actual VS,
external UIA, mixed-monitor and visible pacing remain separate. No broad suite,
native matrix or WPF build/test ran. D7e followed below. Icon-picker tiles, A05/A10
and Backstage closing-title ink remain separate; no new theme family is selected.

## Customization tree decoration D7e — 2026-11-13

Work started clean at the user's D7d commit `60654ff`. `RibbonOptionsTreeRenderer`
shares the native tree's selection, expander, checkbox, borrowed icon, direct text
and focus recipe. Value-only `RibbonOptionsTreeMetrics` resolves decoration slots
over the retained native row/indent/scroll geometry. No new family selector,
native tree replacement or input/lifecycle coordinator was added.

`OptionsAppearance` adds independently resettable roles:

- `TreeCornerRadius` / `TreeSelectedBackground`: radius 0..64 logical pixels, null
  zero; optional alpha/gradient fill, null solid Checked.Start. Authored selection
  composites over existing Surface; Transparent hides it. List-row selection stays separate.
- `TreeCheckSize`: 0..64, null Compact 18 / Touch 24 with existing slot clearance.
  Authored sizes clamp inside the native check slot and row. Zero hides paint while
  retaining the target. Visible checkboxes use the actual DrawCheckBoxIndicator
  override with the same enabled/checked/hot/pressed/focus context and density.
- `TreeIconSize` / `TreeIconTextGap`: 0..256 / 0..1024, null 16 / 2. Authored size/
  gap fits the existing reservation to the left of native text; zero size hides
  borrowed artwork. Native text bounds, targets, indentation and scrolling do not move.
- `TreeFocusInset`: 0..1024, null one device pixel. Authored inset clamps inside
  the row. Existing native focus style, colors and cue eligibility remain.

Expanders reuse shared `Chevrons.Length` / `Depth` / `StrokeWidth`, including zero/
hide and existing atomic validation. Null keeps tree Compact/Touch long sizes
12/16, legacy half-length depth and strokes 1.6/1.8 with a device-pixel minimum.
Authored geometry clips/fits the existing expander slot, preserving both arrow
orientations and hit targets. Shared-chevron presets also affect existing ribbon
utilities and reset independently. Native Foreground/SecondaryText/Checked/Surface
roles and disabled SystemColors.GrayText remain; direct text is not rerouted into
a public DrawText interception. Existing native double buffering and DrawNode
event dispatch remain with the tree.

Validation precedes Theme/BuiltInTheme mutation. Partial tree glyph/selection resets
retain each other and earlier list/frame/navigation settings. Whole OptionsAppearance
Reset retains shared Chevrons and unrelated palette/tip roles. The owned rounded
painter is disposed with the tree; application artwork remains borrowed. Drafts,
validation, native selection/expansion/keyboard/scrolling and Apply/Cancel retain their owners.

Evidence under `artifacts/options-tree-d7e/`:

- `tests/before-defaults.trx`, `after-defaults.trx` and the empty-group comparison
  in `tests/tree-focused.trx`: **16 exact native PNG/control-geometry/indicator-hook
  pairs**, both light families, flat/Office renderers, Compact/Touch and 9/14-point
  italic fonts at actual 120 DPI. Actual native tree callbacks cover expanded/
  collapsed, enabled/disabled and selected focus states, icon slots and actual
  indicator contexts. Null OptionsAppearance/Chevrons and empty groups match;
  no approvals/tolerances changed.
- `tests/tree-focused.trx`: **21 passed / seven failed** initially. Four glyph-hide
  fixtures incorrectly inspected PNG encoding bytes as pixels, and two input
  fixtures compared intentionally hidden icon geometry with the original icon;
  those assertions were corrected. The retained buffer check passes independently
  in `tree-native-buffer-isolated.trx`; its mixed-run failure is consistent with
  visual-style initialization order and was not treated as a runtime regression.
  `tree-authored-final.trx` passes **10/10** affected/refined paint/input cases;
  `tree-reset-final.trx` passes the refined member-reset case. Prior passing scopes
  are retained. **28 distinct latest cases pass**, 20 new authored, seven retained
  and one default case; this is not a clean initial aggregate run.
- Authored cases cover four matching corners, gradient/alpha/Transparent, native
  focus eligibility/inset, both expander orientations/zero hide/clip, retained native
  text/check/expand targets, isolated extreme glyph/spacing overrides, atomic ranges,
  reset independence and actual hot/pressed/check hooks. Native hidden check and
  expander targets still toggle once through double-click sequences. Authored paint/
  slots cover 96/120/144/192 DPI. Release builds pass without warnings/errors.
- `package-final/result.log`: **4/4 public-package scopes**, both light families
  with None/Mica owner windows at actual 120 DPI, each Compact/Touch. Actual tree/
  indicator/icon paint and direct native text, zero/Transparent hide, native Space,
  hidden check/expander targets, retained HWND/parent/nodes/text bounds/indent/row
  height/draft and pending visibility edit/Cancel pass. The package fixture's first
  collapse-event count included descendants; it was corrected to count the clicked
  root only. No runtime input/activation workaround was added.
- Normal Debug runtime/designer server/client/sample builds pass without warnings/
  errors. Package checks used `0.1.0-designer.local20261113T023924855Z`; a final XML-only
  clarification of shared-chevron surface defaults refreshed the package/sample to
  `0.1.0-designer.local20261113T024240443Z` without repeating passed behavioral scopes.
  **Try customization tree / Tree glyphs / Tree selection and focus** use **Theme
  effects 9**, now six groups; its earlier three row groups and tabs 1–8 remain.
  Public presets/hide/glyph/selection/all-tree/shared-chevron resets, isolated public-
  model tree preview, real ribbon mode/sources and idempotence pass. Representative
  tree/preview/tab captures were inspected. Authored Forms/UserControls/resources/
  project files remain intact.

D7e user appearance review is recorded in the ledger. Physical input/IME, actual VS,
external UIA, mixed-monitor and visible pacing remain separate. No broad suite,
native matrix or WPF build/test ran. D7f followed below. A05/A10 and Backstage
closing-title ink remain separate; no new family is selected.

## Group icon-picker tiles D7f — 2026-11-13

Work started clean at the user's D7e commit `7f06db8`. Shared
`RibbonOptionsIconPickerRenderer` and value-only `RibbonOptionsIconPickerMetrics`
extract the group dialog's native tile selection/content recipe. The existing
ListView still owns cells, item identity, selection, focus, scrolling and disposal.
No new family selector, public dialog API, input owner or layout coordinator was added.

`OptionsAppearance` adds seven independently resettable members:

- `IconPickerCornerRadius` / `IconPickerInset`: 0..64 / 0..1024 logical pixels.
  Null keeps the actual popup/command-radius minimum and one-device-pixel selection
  inset. Authored inset clamps inside the existing cell; zero removes it. Selection
  inset does not move artwork, None text or native cells.
- `IconPickerBorderWidth` / `IconPickerBorderColor`: width 0..32, null one device
  pixel; Empty ink follows Border. Alpha/Transparent and zero/hide are supported;
  widths clamp inside the selection paint bounds.
- `IconPickerSelectedBackground`: optional alpha/gradient fill, null Checked.
  Existing Surface backing remains. A transparent fill retains an independently
  configured outline; zero outline and transparent fill hide both decorations.
- `IconPickerIconSize`: 0..256, null Compact 24 / Touch 32 with legacy ceiling
  rounding. Authored artwork fits the existing native cell; zero hides it without
  substituting a None label or changing item identity/spacing.
- `IconPickerTextInset`: 0..1024, null zero, for the None label's paint bounds.
  Font/Foreground/centering retain their owners. Native focus painting remains
  direct, with its existing selected/focused eligibility and full-cell target.

Validation precedes Theme/BuiltInTheme mutation. Actual radius fallback is queried
only for selected tiles, retaining the previous hook lifetime; direct native text
is not rerouted through DrawText. Selection/content/all-picker resets retain each
other and earlier tree/list/frame settings. Whole OptionsAppearance Reset retains
unrelated theme roles. Native item population, selected shared-artwork deduplication,
borrowed vector/raster ownership, group-name/layout validation, normalization and
draft OK/Cancel remain unchanged. No authored Forms/resources/project files were replaced.

Evidence under `artifacts/options-picker-d7f/`:

- `tests/before-defaults.trx`, `after-defaults.trx` and
  `tests/empty-defaults-isolated.trx`: **16 exact native PNG/control-geometry/icon-
  slot/cell-spacing pairs**, both light families, flat/Office renderers,
  Compact/Touch and 9/14-point italic fonts at actual 120 DPI. Actual tile callbacks
  use native item bounds, selection/focus/enabled state and borrowed artwork;
  selected shared-artwork identity and deduplication remain. Null and empty groups
  match saved results without approval/tolerance changes.
- `tests/picker-focused.trx`: **19 passed / five failed** initially. Four new
  content fixtures assumed pure-black text instead of the theme's existing ink;
  their hidden-artwork/None checks now compare against the white backing and pass
  **4/4** in `picker-content-final.trx`. The empty/default comparison failed in the
  mixed batch but passes all 16 pairs in a fresh process via
  `empty-defaults-isolated.trx`; no runtime paint workaround or approval change was
  added for that run-order variance. Prior passing scopes were retained.
  **24 distinct latest cases pass**, 16 new authored, seven retained and one default;
  this is not a clean initial aggregate run.
- New authored cases cover matching four-corner selection paint, gradient/alpha/
  transparent/zero outline, tiny clamps, independent content/None bounds, zero-artwork
  semantics, atomic ranges, actual selected-only radius and direct text hooks,
  member/group/built-in Reset, retained native cells/spacing/items/HWND/parent/
  selection, empty-name validation, accepted metadata/size normalization and Cancel.
  Authored paint/metrics cover 96/120/144/192 DPI. Release builds pass without warnings/errors.
- `package-final/result.log`: **4/4 public-package scopes**, both light families
  with None/Mica owner windows at actual 120 DPI, each Classic/Simplified x Compact/Touch.
  Public RibbonCustomizationEditor/New Group routing opens the actual modal group
  dialog; actual picker paint/radius/direct text, authored sizes/zero hide, native
  deduplication/cells/spacing/items/HWND/parent/selection, reset independence,
  invalid-name rejection, OK/Cancel draft metadata and live source/artwork ownership
  pass. The inspection timer closes only an unfinished dialog; its initial cleanup
  incorrectly replaced a pending OK result with Cancel and was corrected in the
  diagnostic. No runtime modal/input workaround was added.
- Normal Debug runtime/designer server/client/sample builds pass without warnings/
  errors. A sample-only reference to internal model Parent was replaced by a public
  collection/reference check, retaining passing runtime/designer builds. Package
  `0.1.0-designer.local20261113T031321233Z` refreshes the permanent sample.
  Requested new **Theme effects 10** contains **Try group icon picker / Icon picker
  selection / Icon picker content**, three groups. D7d/D7e builders/verifiers are
  pinned through ThemeEffects9ReviewTab; its six groups and earlier tabs remain.
  Presets/hide/independent/all-picker resets, isolated public-model group preview,
  real ribbon mode/sources and idempotence pass. The preview uses the public
  customization form's New Group action and closes its parent afterward.
  Representative picker/dialog/tab captures were inspected.

D7f appearance review is pending in the ledger. Physical input/IME, actual VS,
external UIA, mixed-monitor and visible pacing remain separate. No broad suite,
native matrix or WPF build/test ran. Next proposed is **D8 caption/title appearance**:
inspect A05 tint eligibility/color/strength and the recorded closing-title ink/clip
handoff before choosing a bounded contract. Preserve Office 2024's animated neutral
tint, authored Form/theme caption precedence, full-height Office 2019 panes and
existing pane/title clocks. A10 preferred page minimum and new families remain
separate. Keep tab 10 until directed otherwise.

## Group icon-picker square geometry follow-up — 2026-11-13

The user reported unequal None/artwork selection shapes and requested that None
also follow inset/size changes. The original D7f remains uncommitted on baseline
`7f06db8`; this correction preserves that work. Native measurement reproduced the
cause: with 44-pixel icon spacing, native label-dependent item bounds were 44×50
for None and 44×65 for a wrapped command name. Focus could enlarge those metadata
bounds further. Using them directly for tile paint/focus caused the mismatch.

The picker now uses [LVS_EX_HIDELABELS](https://learn.microsoft.com/en-us/windows/win32/controls/extended-list-view-styles)
with its existing native double buffering. Item text/tooltips remain intact, but
native label layout no longer determines the tile. The control measures the
native icon margin, sizes its owned placeholder image list and sets matching
horizontal/vertical spacing. Paint and focus use one canonical square anchored to
the native icon rectangle, including for None. No native ListView or item is replaced.

Positive IconPickerIconSize resizes the common cells for None/artwork with existing
density/font minima, capped at 256 device pixels for the native image list. Zero
hides artwork with the default safe footprint. Selection inset and outline now
reserve shared content clearance; None's optional extra text inset applies inside
that region. Cell metrics refresh before native paint/print and through existing
layout/font/DPI paths, guarded against reentry. Selected items remain visible and
retain native ownership. No new public setting or global coordinator was added.

This intentionally revises D7f's original fixed-cell/content-independent-inset
contract and changes its default geometry. The original 16 before/after pairs are
historical evidence for the extraction, not approval of the corrected shape;
their native records show 96 DPI, distinct from the 120-DPI package captures.

Evidence under `artifacts/options-picker-square/`:

- `tests/square-focused.trx`: 14 passed / six failed initially. The failures compared
  native full item/label metadata rectangles with the new icon tile. Corrected
  checks use the native icon rectangles that actually define paint/hit cells.
  A synchronous synthetic mouse-down then stalled in the native drag loop; its
  test host was stopped and `square-native-final.trx` is an aborted run, not a pass.
  Queuing matching down/up messages through the native pump fixes that fixture;
  `square-native-queued.trx` passes **6/6**. Earlier passing scopes were retained:
  **20 distinct latest relevant cases pass**. No runtime input workaround was added.
- New cases verify shared None/artwork selection pixels, monotonically adaptive
  square sizes, zero/default footprint, inset/outline clearance and safe extreme
  clamps at 96/120/144/192 DPI. Native Compact/Touch, 9/14-point tests at actual
  96 DPI cover 25 items with long names, multiple rows, scrolling, square icon hit
  rectangles, queued clicks, font changes, stable HWND/parent/items/selection and
  retained in-process accessible names. Existing four-corner/alpha/content and
  group validation/OK/Cancel cases also pass. Release builds have no warnings/errors.
- `package/result.log`: **4/4 public-package scopes**, both light families with
  None/Mica owners at actual 120 DPI, each Classic/Simplified x Compact/Touch.
  Native None/artwork selection captures at sizes 16/48 and insets 0/4 match the
  common square layout and native targets. Metadata/deduplication, item/handle/parent/
  selection retention, member/group Reset, validation, draft OK/Cancel and borrowed
  artwork remain. Representative paired None/artwork and dialog captures were inspected.
- Normal Debug runtime/designer server/client/sample builds pass without warnings/
  errors. Package `0.1.0-designer.local20261113T034156273Z` refreshes the permanent
  sample. Theme effects 10 retains three groups with Smaller/Larger picker tiles;
  tab 9 and earlier accepted groups remain. No WPF, full suite or physical DPI/
  IME/external-UIA/actual-VS gates ran. No authored Forms/resources/project files changed.

User review of the requested square/None adaptation correction is pending. Finish
that review before proceeding to the proposed D8 caption/title work.
