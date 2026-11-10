# Native WinForms themes

Status, **2026-11-10: Office 2019 Light is implemented and ready for user review.
Focused and package/native checks passed; broader-suite limits are recorded below.
Dark variants remain deferred.**

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
- First-theme public API/shared presentation changes require native Debug and
  Release solution builds and a native unit/STA suite run against the final
  Release build. Use focused regressions during development; repeat/broaden only
  for changed code, failures or unresolved risk. Do not run the parent WPF solution.
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
| Planning | Source inventory/proposed native contract documented, including both supplied header references, zero corner radii, an edge-to-edge body background with zero outer margin and retained inner padding, minimal resting borders and bordered/shadowed floating popups; later family order is provisional. |
| Runtime/API/rendering | Implemented; native Debug and Release solution builds passed without warnings/errors. |
| Designer/sample/package consumer | Refreshed normal Debug sample; independent Release package restored/built; six generated Form/RibbonForm initializers compiled and reopened successfully. |
| Focused Release verification | **105 passed**, including new theme/role behavior and related header/clipping/color/editor/renderer/designer/frame checks. `office2019-final-focused.trx`. |
| Office 2024 default parity | **32 PNG/measurement pairs unchanged**, both renderers/densities, expanded/minimized, synthetic 96/120/144/192 DPI; captured before runtime changes in `before/` and compared by the focused run. |
| Composed native package verification | **16/16 passed** at actual 120 DPI: ordinary Form/RibbonForm, Compact/Touch, Classic/Simplified, Solid/Mica/Acrylic. `package-verified/consumer-final/results.json`. |
| Broader Release suite | **1,485 passed / 6 failed / 1,491 total** on the final source. `office2019-final-full-release.trx`; baseline comparisons and limits are described above. |
| User appearance review | Pending the refreshed sample review described above; geometry acceptance stays in its own plan. |
| Actual VS/physical input/mixed-monitor | Not run for themes; report separately. |

Record later evidence and confirmation here once, with date/revision/scope,
artifact locations and limits. Keep [NEXT-SLICE-PROMPT](NEXT-SLICE-PROMPT.md) a
short pointer to this plan.
