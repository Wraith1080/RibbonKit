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
| Planning | B1–B3, C1–C2 and audit corrections A00–A04/A06–A09/A11–A16 are implemented. D1 commands/editors is next proposed; caption/layout/tip candidates remain separate and later family order is provisional. |
| Earlier-slice adjustability audit | **2026-11-11: A11–A16 header/shared effects implemented and verified.** Current evidence is in the [header/shared record](#header-and-shared-effect-overrides--2026-11-11). Earlier evidence remains below. The [audit](THEME-EFFECT-ADJUSTABILITY-AUDIT.md) retains A05/A10/A17 as separate candidates. |
| Runtime/API/rendering | C2 uses explicit neutral/Office frame defaults, resolved metrics and shared body/group-frame/QAT/message helpers without a new family selector. `BodyAppearance` adds independently resettable borders, dividers and joins; actual renderer measurements/painting and native ownership remain authoritative. Earlier settings are retained. |
| Designer/sample/package consumer | Normal Debug runtime/designer/client/sample builds pass without warnings/errors; the permanent sample is refreshed. **Body and connected frames** is appended to **Theme effects 2**; the Office 2019 tab retains its seven groups. Continue using the new tab until the user says it is full. Authored Forms/UserControls/resources remain intact; actual VS is separate. |
| Focused Release verification | C2: **49 distinct relevant Release cases passed across retained runs**, including authored frame values/reset, group hooks/reduction, native joins/editor state, radius/shadow footprint, QAT/message seams, message motion, material frames and borrowed group popups. Earlier evidence remains below; no broad suite ran. |
| Two-family default parity | C2: **80 PNG/geometry pairs match exactly**: 16 four-DPI neutral/Office frame/group sheets plus 64 native Solid/Mica Classic/Simplified Compact/Touch minimized/expanded wide/narrow states. Empty frame records also match null in the paint sheets. Earlier comparisons remain below. |
| Composed native package verification | C2: **4/4 public-package scopes plus demo-tab checks passed** at actual 120 DPI for both light families in Solid/Mica. Each scope checks eight mode/density/minimized states with joins on/off, native border/divider hooks, reset, message motion and retained editor/QAT sources. Offscreen timer/print checks do not establish visible frame-pacing or physical-input acceptance; no full native matrix ran. |
| Backstage review correction | **2026-11-11: navbar blank-frame and left rounded-corner black-bar fixes implemented and verified; user review pending.** Current focused/package evidence is in the [page-motion follow-up](BACKSTAGE-PAGE-MOTION.md#navbar-blank-frame-and-rounded-edge-follow-up--2026-11-11). |
| Deferred title review issue | **2026-11-11: user reported title-text flicker while closing Backstage and allowed deferral to title theming work.** Their `20261111-0345-01.1714250.mp4` confirms the Office 2019 close at approximately **7.03–7.37 s**: the title moves left, its leading text becomes pale/white over the lightening caption, then dark ink returns. Native-rate frames and timestamps are retained under `artifacts/backstage-title-flicker/recording/`. Caption foreground contrast/clip handoff as the navigation fades is the source-inspection lead, not a verified root cause. Preserve opaque, legible title ink throughout closing when addressing it. No title runtime change or new build/test was made for this recording review; the page/corner correction does not address it. |
| Broader Release suite | Initial implementation: 1,485 passed / 6 failed / 1,491 total (historical evidence above). Correction run **stopped at the user's request**, 1,089 passed / 3 failed / 1,092 executed; incomplete, no full-suite result claimed. Do not restart it. |
| User appearance review | **2026-11-11: user confirmed everything works in the delivered A11–A16 header/shared-effects batch and authorized continuation with C2.** Earlier A06–A09/A00–A04/B1 acceptance is retained at its recorded scope. C2 review is pending; actual VS, physical input, mixed-monitor and all-material gates remain separate. Geometry acceptance stays in its own plan. |
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

C2 human appearance review is pending. Actual VS, physical input, visible flicker/
frame pacing, IME and mixed-monitor acceptance remain separate. No broad suite,
full native matrix, WPF build/test, commit or push ran. D1 commands/editors is
next proposed; A05 caption, A10 page-width preference, A17 badge placement and the
known closing-title ink flicker remain separate.
