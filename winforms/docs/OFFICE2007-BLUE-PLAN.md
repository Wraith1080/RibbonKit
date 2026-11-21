# Native Office 2007 Blue appearance contract

**2026-11-21 - G6 complete; Backstage accent/Orb KeyTips and a joined colored standard caption are delivered before G7.**
G1 remains the appearance reference. G2's additive APIs and focused evidence are
[recorded below](#g2-shared-paint-foundations--2026-11-20). No Office 2007
runtime preset or sample tile is delivered by G2.
Current family acceptance belongs in the
[themes ledger](THEMES-PLAN.md#current-status-and-review-ledger).
The [G3 composition record](#g3-application-button-and-menu-composition--2026-11-21) follows G2.
The [G4 delivery](#g4-normal-blue-preset-and-ribbon--2026-11-21) composes these foundations
into the preset and both permanent View galleries. The
[G5 delivery](#g5-application-menu-palette--2026-11-21) adds the dedicated menu palette.
The [G6 delivery](#g6-options-and-companion-surfaces--2026-11-21) covers customization,
editor/gallery/menu/tip roles and the requested blue dialog/popup backing.
Its [gradient and thin-button-outline correction](#g6-gradient-and-button-outline-correction--2026-11-21)
replaces the initial flat backing with the WPF reference ramp, including native wells.
The [caption hover-outline completion](#g6-caption-hover-outlines-and-slice-completion--2026-11-21)
finishes the requested G6 corrections. Live review remains separate from recorded checks.
The [framed Backstage addition](#framed-backstage-workspace-before-g7--2026-11-21)
implements the subsequent design proposal while G7 remains paused.
The [animated-paint correction](#orb-only-return-and-animated-paint-correction--2026-11-21)
supersedes the initial separate return button and fixes the retained header paint.
The [accent, KeyTip and caption-join follow-up](#backstage-accent-orb-keytips-and-a-joined-standard-caption--2026-11-21)
extends that implementation without starting G7.
Current family acceptance and continuation authorization remain in the themes ledger.

## Reference and boundary

Use the current WPF [Office 2007 tokens](../../src/RibbonKit/Themes/Tokens.Office2007.xaml),
[shared ribbon/Orb template](../../src/RibbonKit/Themes/Controls.RibbonChrome.xaml)
and [Application Menu template](../../src/RibbonKit/Themes/Controls.ApplicationMenu.xaml)
as read-only design references. The
[WPF theme record](../../docs/07-OFFICE-2007-THEME-PLAN.md) explains the measured
appearance; its historical measurements and comments do not override current
tokens. The [Orb/menu implementation record](../../docs/history/library/05-application-menu-localization-and-snapshots.md)
also identifies layering and input pitfalls. This is a native design contract,
not a WPF hosting plan or a claim of pixel parity.

Deliver normal opaque Office 2007 Blue first: blue title valley, flat pale-blue
tab band, connected tabs, individually framed groups, silver Orb and gold/orange
feedback. Native editors retain their actual HWNDs, white wells, selection,
focus, validation and accessibility. Commands, icons, document content and saved
preferences remain host-owned. All reusable appearance and geometry belong in
the library and must work without Showcase helpers.

Theme selection must not enable Aero, Mica or Acrylic, or replace an authored
File route. Backstage remains available through the existing explicit route;
WPF's later Glass/Classic Backstage extensions, dark variants, grain effects
and a new rendering backend are outside this first family contract.

## Normal opaque palette and ribbon states

Colors below are opaque sRGB. Gradients run top to bottom; `offset: color`
denotes a stop. Repeated offsets mean an intentional hard color transition.
The body and group valleys are distinct from the flat tab band.

| Role | Proposed native appearance from current WPF tokens |
| --- | --- |
| Caption | `0: #E4EBF6`, `.14: #D5E5FA`, `.28: #CADEF7`, `1: #E4EFFD`; blue title ink `#15428B`. |
| Tab band | Flat `#BFDBFF`; ordinary tabs reveal it. |
| Ribbon body / joined QAT extender | `0: #DBE6F4`, `.17: #C8D9ED`, `1: #E3F4FF`; outline `#8DB2E3`. |
| Selected tab | `0: #F0F6FF`, `.08: #F6FAFF`, `.15: #EBF3FE`, `1: #DBE6F5`; blue ink, outlined top/sides and open foot joined to the body. |
| Group panel | `0: #DEE8F5`, `.16: #C7D8ED`, `1: #D9E8F6`; border `#A8BFD4`, inner highlight `#E4ECF5`. |
| Group label band | `#C1D9F1` with blue `#15428B` text; spans the panel beneath its items. |
| Ordinary text / editors | `#1E1E1E`; white native editor wells. Accent `#1E5A9C` is separate from gold feedback. |
| Hover | `0: #FFFFF7`, `.38: #FFE798`, `.38: #FFD758`, `.48: #FFD350`, `1: #FFE89F`; border `#C1A877`. |
| Pressed | `0: #FEB96C`, `.40: #FDA461`, `.40: #FC8F3D`, `.63: #FC973D`, `1: #FEBE63`; border `#B6A892`. |
| Checked | `0: #FFF3D2`, `.38: #FFDE84`, `.38: #FFCB43`, `.48: #FFC739`, `1: #FFDC8A`; border `#C19A4B`. |
| Checked + hover | `0: #FFEEC0`, `.38: #FFD670`, `.38: #FFC12E`, `.48: #FFBD26`, `1: #FFD478`; retain checked identity. |

Pressed feedback retains the light/dark/light gel profile with deeper orange;
do not invert the hover ramp. Ordinary command text and icons retain their
normal ink. Gold states do not follow the optional identity-accent recoloring.
Inner highlights remain separate, adjustable decoration roles. A split button's
active half gets the appropriate state fill; its companion stays transparent
with a border/highlight rather than receiving a whole-button gold wash.

Disabled commands use the existing native disabled-ink policy, suppress hot and
pressed feedback, and retain checked meaning without suggesting an enabled
action. Keyboard focus, KeyTips and UI Automation remain independent of hover.
High contrast retains the existing system-color policy. Contextual tab bands
keep their category hue and shared visibility/selection behavior.

Tabs use approximately 3-logical-pixel top corners and square joined feet.
Group panels use a 1-logical-pixel rim and approximately 3-pixel corners, with
the label band tucked inside the bottom rim. Reserve frame space consistently
through group reduction; do not move item hit targets when a frame appears.
Panel content and caption clips must derive from the measured group snapshot,
including collapsed groups. Simplified mode keeps the shared reduction/layout
policy and uses separators between groups rather than rectangular panels. The
panel visibility setting remains independent from Classic frames and label bands.

Keep the ribbon flush with its normal host layout. Below-ribbon QAT joins the
body without a doubled top border. A standard window has no permanently painted
six-pixel Aero perimeter. Reuse native font-aware row sizes, density, minimum
targets, DPI rounding and caption metrics; WPF's older measurements are not a
reason to force a shorter native caption or rewrite group layout.

## File, Orb and Application Menu

The family default is an overlapping round Orb. A future explicit rectangular
File choice must win over that default; clearing the override resumes the
theme choice. Shape and File route are separate: a host's saved CommandMenu or
Backstage preference survives theme changes. Do not infer routing from shape.

| Orb role | Proposed appearance / behavior |
| --- | --- |
| Resting sphere | Silver/pearl: `0: #FBFCFD`, `.18: #F5F7F9`, `.5: #DDE4EC`, `.82: #EFF3F7`, `1: #FFFFFF`. |
| Hover sphere | Whole sphere turns gold: `0: #FFFEFC`, `.18: #FFF8D4`, `.5: #F7C93E`, `.82: #F7D96F`, `1: #FDFAB7`. |
| Pressed / menu open | Amber: `0: #F5D98A`, `.18: #EFBE4A`, `.5: #D8931A`, `.82: #E8A82A`, `1: #F7D06A`; open feedback lasts until the surface closes. |
| Rim and shadow | Pale rim `#F0F4F9`, gold hover rim `#FFEA37`, soft blue-gray shadow; independently adjustable and clipped without a heavy dark outline. |
| Reference geometry | WPF diameter 46 logical pixels, margin `6,-18,6,0`; Touch diameter 52. Adapt overlap to the native caption/tab snapshot and minimum targets rather than copying a negative margin into HWND bounds. |
| Glyph | Shared four-square vector fallback, with an explicit host icon/glyph extension. Borrow application icons; preserve their ownership. The default is not an automatically substituted Form icon. |

The Orb occupies caption/tab space. Reserve its full visual footprint for QAT,
title text, tabs and caption dragging. Its upper half remains clickable rather
than becoming a window-drag target; geometry, visual clip, focus cue and native
hit testing must agree. Keep one logical File command, accessible name, KeyTip
and open state even if composition requires a separate passive visual. Native
capture, cancellation, deactivation, theme switches and disposal must release
input ownership once. Do not duplicate command execution or reparent live native
editor content to manufacture the overlap.

An explicit rectangular File uses a blue creased fill rather than the Orb
palette: idle `#6FB4E4` to `#3585C6`, hard change at `.45` to `#1F63A6`, ending
`#4C93CD`; lighter hover and deeper pressed variants follow the WPF tokens.
Menu-open uses `#1E5A9C` with white text. Keep those roles independent from the
Orb states and from the existing rectangular File width override.

The classic Application Menu is a two-pane command surface beneath the Orb,
with title/QAT still visible. The Orb stays above the menu frame; the rest of
the tabs do not. The WPF anchor is the button's top-left plus eight logical
pixels vertically, not an ordinary below-button dropdown. Native implementation
must measure this relationship, constrain the popup to the working area and
retain its origin while panes change. Native caption hosts floor the requested
frame origin at the caption bottom before calculating Orb/content clearance;
otherwise the caption exclusion would cut away the menu's top frame. The
existing above-caption fallback still handles insufficient space below.
The WPF same-window overlay/reparenting
technique is not itself a native implementation contract: resolving the current
popup-HWND versus overlapping Orb layering is a shared-library implementation
gate before exposing the complete family.

| Menu role | Proposed palette / frame |
| --- | --- |
| Outer frame | `#9BAFCA` edge, white inner rims and `#BED3EC` side band; rounded outer corners near 3 logical pixels. |
| Top band | `0: #D7E5F7`, `.44: #D5E2F3`, `.44: #C6D7EE`, `1: #B7CBE6`; WPF reference height 18. |
| Navigation / pane | Navigation `#FAFAFA`; pane backing `#E9EAEE`, content `#FAFAFA`, pane border `#9F9F9F`; blue heading ink. |
| Footer | `0: #BDD3EF`, `.47: #BFD5F1`, `.47: #B1CAEA`, `.96: #CFE0F6`, `1: #C9EEFF`; blue creased footer buttons and soft popup shadow. |
| Row states | Gold hover/selection and deeper pressed feedback; split command/arrow affordances remain distinct, with a subdued companion while its pane is active. |

Reuse the native model, source collections, panes, scrolling, nested popups,
Options/Exit footer commands and accessible objects. Selecting a pane must not
execute a split command's primary action. Disabled parents disable descendants;
Escape closes the deepest active surface before returning to File. Preserve
native custom-content focus/validation and borrowed model/icon ownership.
Options, galleries, menus and tips take the family palette through shared
appearance roles; native scrollbar behavior remains system-owned. Do not copy
WPF custom scrollbars or its visual tree into the native dialogs.

## Future palette on the shared Aero frame

Office 2007 will supply its own `RibbonAeroFrameAppearanceSettings` through
`RibbonTheme.AeroFrameAppearance`. Reuse
[the existing frame contract](../src/RibbonKit.WinForms/Rendering/RibbonAeroFrameAppearanceSettings.cs)
and [native frame owner](../src/RibbonKit.WinForms/RibbonForm.FrameAppearance.cs),
without an Office 2007 form subclass or an Office 2010-only color/eligibility check.

- Proposed `RestoredFrameThickness` is **6 logical pixels**, explicitly owned by
  the 2007 palette and adjustable independently of 2010. The shared frame insets
  left/right/bottom only when restored; maximize removes those insets. Preserve
  native resize, restore, work-area and caption-button ownership; no Form.Region.
- WPF reference colors are active fallback `#9BBBE3`, inactive `#BCC8D6` and tint
  `#1A78A5`. Use a separate 2007 palette with these reference anchors, including
  its own border inks; review the final Solid recipe in the later Aero slice.
- Current native **Solid uses `InactiveBackground` for active and inactive
  windows**. `ActiveBackground` estimates material contrast; it is not an active
  Solid fill. Retain this shared policy and specify the 2007 Solid appearance in
  its own `InactiveBackground`, rather than assuming the WPF active/inactive
  brushes select automatically.
- Frame selection and Solid/Mica/Acrylic selection remain independent host
  preferences. Form tint overrides, intensity, Reset, high-contrast and
  unsupported/designer/child-window fallbacks keep their current precedence.
- The current native frame spans caption, tab band and restored edges. WPF's
  opaque-tab/material-caption split, diagonal reflection and tiled grain are
  additional effects, not supplied by the current palette API. They are deferred
  unless separately reviewed; normal opaque `#BFDBFF` tabs remain the baseline.

The existing public `Office2010Aero` enum value already selects this shared
frame without a theme-family check. Preserve its identity and saved values;
sample wording can remain the generic Aero frame. A rename is not required
to supply a 2007 palette.

## Native support and implementation gaps

| Area | Already available | Later work |
| --- | --- | --- |
| Fills / state chrome | Immutable multi-stop fills, opacity mapping, full-surface coordinates, rims and independent state roles; G2 adds exact repeated-offset stops through an opt-in factory. | The existing constructor still requires strictly increasing offsets. G4 opts into hard transitions explicitly. |
| Ribbon / tabs / QAT | Connected filled tabs, body joins, reduction, density/DPI snapshots, public renderer hooks and three QAT placements; G2 adds optional framed group panels and label bands. | G4 composes the 2007 roles; existing themes retain their separators and captions. |
| File | Shared File command/routes and rectangular paint; G3 adds theme-owned shape, explicit override/Reset, sphere/glyph/focus hooks, overlap reservations and circular input. | G4 supplies the family palette/default and preserves an explicit rectangular override and saved File route. |
| Application Menu | Native two-pane model, state/frame roles, footer/scrolling/nested popups/custom content; G3 adds native Orb/caption cutouts, measured origin and top-band content clearance. G5 supplies the blue band/frame/footer palette. | Menu appearance review is pending. Native font/DPI clearances are not a claim of WPF pixel parity. |
| Other surfaces | Shared menu/gallery/tip/editor and Options appearance pipelines; real native editors and dialog drafts. | Compose and review their own 2007 roles, especially creased fills and primary/secondary button feedback. |
| Aero | Theme-owned palette/edge width and native frame/material owner; restored/maximized transitions. | A separately reviewed 2007 palette using that owner. Reflection/grain and a different material/tab split remain deferred. |
| Consumers / designer | Permanent View galleries in both samples, persistence and independent package-only verification. | G4 appends identity 4 without renumbering existing values and updates serialization, inert preview and both galleries. No diagnostic tabs. |

Keep new settings optional with existing defaults and explicit override/Reset
precedence. Any public additions need XML documentation and an additive API
baseline update. Decide their exact names in the relevant implementation slice;
this document does not introduce runtime APIs.

## Proposed slice order and review stops

| Slice | Deliverable and bounded gate |
| --- | --- |
| G1 - this document | Palette, Orb/menu behavior, support gaps and sequence recorded. The subsequent user request authorizes G2 shared foundations only. |
| G2 - shared paint foundations | **Implemented.** Exact hard transitions and opt-in group-panel/label-band decoration. Focused painter/sampler, clip, DPI and old-default compatibility evidence below; no layout-engine replacement. |
| G3 - application button and menu composition | **Implemented.** Shared shape/override, Orb geometry/glyph, caption hit testing, QAT reservation and native Orb/menu cutouts/anchor. Independent keyboard/UIA/native-content evidence below; no family tile yet. |
| G4 - normal Blue preset and ribbon | **Implemented.** Family identity, opaque palette, connected tabs, groups, rectangular override and Orb states; exposed in both permanent View galleries. Current review and continuation authorization are in the themes ledger. |
| G5 - Application Menu palette | **Implemented.** Frame/top/footer bands and gold/split feedback on the G3 composition. Current menu review and continuation authorization are in the themes ledger; focused native-content and caption-clearance evidence is below. |
| G6 - Options and companion surfaces | **Implementation complete.** Options/customization, editor, gallery, menu and tip roles; full-surface valley backgrounds, thin dialog action outlines, split/QAT/overflow feedback, Simplified separators and hover-only caption rims. Focused evidence and known baseline limits below. Current user review belongs in the ledger; G7 has not started. |
| Backstage addition before G7 | **Implemented separately.** Creased return header and one framed navigation/page workspace, a single divider, gold navigation and equal 4-pixel top/side row and separator clearance. Existing native pages and File routing remain. Focused evidence below; runtime appearance review is separate. |
| G7 - composed normal-theme review | Refresh the existing Designer Sample in place and verify both samples plus a consumer without sample resources. Human review covers Classic/Simplified, density, state feedback, File routes and resize/maximize/restore. |
| G8 - separate Aero palette and review | After normal acceptance, supply the 2007 palette and edge width to the shared frame. Review restored edges, edge resizing, maximize/restore, Solid/material choices and Backstage with background load. |

For each later slice, use normal Debug review outputs and only affected focused
checks; preserve passed families and retired diagnostic tabs. Keep build/test,
sampled desktop frames, actual VS designer, physical input/IME, mixed-monitor
and human acceptance evidence separate. Retain the existing clipped ribbon
repaint and full Solid Backstage preparation rather than reopening the renderer
or animation architecture without a concrete regression.

G1 validation was source/reference review, changed-link inspection and
`git diff --check`. No build, runtime test, app launch or desktop capture is
needed for that documentation-only proposal. The user's subsequent G2 request
authorizes only the shared paint foundations below.

## G2 shared paint foundations — 2026-11-20

Entry: clean `codex/Ribbonkit-Winform`, HEAD `f239e3e` (`Office 2007 Planning`),
with `ed382ff` retained. Changes stay under `winforms/`; WPF resources are read-only
references. No preset, Orb/menu composition, diagnostic tabs or new sample UI.

### Public contract and implementation

- `RibbonGradientStops.WithHardTransitions(stops)` snapshots nondecreasing,
  finite intermediate offsets strictly between 0 and 1. Repeated offsets are
  stored exactly in authored order. The first color ends the incoming ramp;
  the last starts the outgoing ramp and wins at the exact offset. Multiple
  coincident stops are supported. The original constructor still rejects
  duplicates, unordered offsets and endpoints. There is no epsilon approximation.
  Equality/hash codes remain structural, independent of construction path;
  endpoints, deconstruction, axis, empty normalization and all-stop opacity/color
  mapping retain their existing contracts. Both shared painters use the sampler.
  `BackgroundBounds` still anchors partial surface repaint to the full gradient.
- `RibbonBodyAppearanceSettings.GroupPanel` accepts an immutable
  `RibbonGroupPanelAppearanceSettings`. Background/label fills allow alpha and
  hard or smooth gradients. Independent outer/inner inks, widths, radius,
  panel/inner insets and expanded label ink are adjustable. Null or an empty
  record retains the old rendering; geometry-only edits on an otherwise empty
  record are inert. Explicit Transparent fills/inks can opt into invisible
  decoration; width zero hides its rim. Group/member `with` edits and clearing
  the optional record provide independent Reset. Validation is atomic before
  assigning the theme; no existing preset changes.
- `RibbonRenderer.DrawGroupPanel` is a replaceable public decoration hook, called
  before group content and command painting. Public context
  `GroupFrameBounds`, `GroupCaptionBounds` and `IsSimplifiedGroup` expose the
  measured input; standalone painting may omit bounds to use existing fallback
  geometry. `DrawGroup`, text, command, separator and editor hooks remain.
- Classic expanded/collapsed panels share the original full group frame,
  inset consistently without moving opener targets. Only expanded Classic
  groups have a label band. It spans the existing caption area inside the outer
  rim and clips to the same rounded interior. Gradients use the band's own
  coordinates. Expanded caption ink follows SecondaryText unless overridden;
  disabled framed captions use DisabledColor. Existing unframed captions retain
  their exact rendering. Inner highlights overlay both panel and label.
- Simplified mode frames each contiguous measured inline group run and existing
  borrowed-group opener, without adding a caption band. Existing group dividers
  are suppressed only while panel decoration is enabled. Root and minimized-body
  popup paths use the same derivation, translated by existing scroll offsets.
  The layout engine, item/native editor bounds, clips, hit targets, density,
  reduction and body scroll negotiation are unchanged. Large authored rims and
  insets clamp to the measured panel; they do not reserve new content space.
- The rounded raster cache includes local fill coordinates for label bands.
  Ordinary callers keep their existing local coordinates/cache behavior. Caller
  clips and graphics state are restored; transparent fills/rims compose without
  clearing other content. Additions are documented in XML and
  `PublicAPI.Unshipped.txt`; the shipped baseline remains untouched.

Example using only library APIs (this is not a built-in Office 2007 preset):

```csharp
var crease = new RibbonFill(Color.White, Color.Black)
{
    IntermediateStops = RibbonGradientStops.WithHardTransitions(
        [new(.5, Color.White), new(.5, Color.Black)])
};
ribbon.Theme = ribbon.Theme with
{
    Hover = crease,
    BodyAppearance = (ribbon.Theme.BodyAppearance ?? new()) with
    {
        GroupPanel = new()
        {
            Background = new(Color.LightBlue), BorderColor = Color.SteelBlue,
            InnerBorderColor = Color.White, LabelBackground = new(Color.AliceBlue),
            LabelForeground = Color.Navy, CornerRadius = 3, Inset = 2
        }
    }
};
```

### Focused evidence and remaining review

The independent public-only consumer is
`tests/RibbonKit.WinForms.PaintConsumer`; it references the native
library and framework APIs, with no Showcase resources/helpers or friend access.
`eng/Test-PaintFoundation.ps1` compares its default images with a retained pre-G2
assembly and runs the current opt-in/native scopes. It uses normal Debug/Release
outputs; its baseline reference does not change branches or rebuild old source.

Evidence is under `winforms/artifacts/office2007-g2/`:

| Scope | Result and limit |
| --- | --- |
| Builds/API | Normal Debug runtime/public consumer and Release runtime/test projects build with zero warnings/errors on the final checks. API/XML analyzers pass; only Unshipped receives additions. Initial consumer restores emitted NU1900 because the vulnerability endpoint was unreachable; the final Debug build used cached packages with `-p:NuGetAudit=false`, not a fresh online audit. |
| Existing regression selection | **58/58 pass**: `GradientFoundationTests`, `BodyPresentationTests`, `CollapsedGroupSeparatorTests`, `RendererBoundaryTests`, `DensityTests`, `TabHeaderClippingTests`. `direct-results.log` retains this scope. |
| G2 regressions | Latest **27/27 pass**, `PaintFoundationTests`, in `direct-paint-final.log`: strict/additive validation, immutable structural equality/hash, coincident-stop ordering/right-continuity, endpoint edits, opacity/axis mapping, exact vertical/horizontal paint and full-surface clips at 96/120/144/192 DPI; alpha commands, measured panel/rim/band paint, caller clip/state restoration, corner containment, band-local hard gradients, label/disabled ink, zero/extreme/tiny decoration, atomic assignment, unchanged snapshots/targets/reduction and native editor HWND/parent/value/selection. Classic/Simplified root and minimized-body popup hooks pass. |
| Initial test fixture and latest totals | The first in-process selection passed 78 and failed one newly added fixture: starting directly in Simplified overflowed the field before a native handle existed. The fixture now establishes the native editor in Classic before switching, matching the retained-editor contract. Its final run and the six added corner/popup cases pass. Latest union is **85 distinct passing cases**, not 79 + 27. Initial failure evidence is retained; no runtime workaround was added for the fixture. |
| VSTest transport | `tests/g2-paint.trx` records an aborted zero-case run. The diagnostic retry identifies test-host `SocketException (10013)` connecting to the runner; it was cancelled once diagnosed. `vstest-diagnostic*.log` retains the evidence. The same built test assembly was discovered/executed by `XunitFrontController` with in-process discovery/execution and parallelism disabled. Runner source and normal Release output are retained under `direct-runner/`. These are actual xUnit results, not a successful `dotnet test`/TRX run or full-suite evidence. |
| Independent defaults | `consumer-debug/comparison.json` and `result.log`: **32 byte-exact PNG pairs**, four existing families x two renderers x four DPI values, covering expanded/collapsed groups and resting/hot/pressed/checked command paint. Current null/empty group settings also match. Baseline is the retained package `0.1.0-designer.local20261120T135235944Z`, DLL SHA-256 `E15EB57C86AF0B348EFCE645F891F4A1158892E2F8983B43207C64329CBAF829`. It is a pre-G2 binary comparison, not a rebuilt source checkout. |
| Independent features/native retention | `consumer-debug/current/result.log`: exact public hard-stop paint on both axes under partial surface clips; **8/8** offscreen native scopes at actual **120 DPI** (Classic/Simplified x Compact/Touch x wide/reduced), public measured hooks, unchanged item bounds/height, Reset and retained editor HWND/parent/text/selection. `consumer-retention/result.log` reruns only G2 features with added native bounds/visibility assertions; **8/8 pass**. No Showcase resources/helpers or friend access. |
| Image inspection | Inspected generated `panel-Classic-Compact-1100.png`, `panel-Simplified-Compact-1100.png` and `panel-Classic-Touch-500.png` under `consumer-debug/current/`: expanded label bands, collapsed frames, compact runs and viewport clipping. Four-DPI raster assertions inspect all four corner masks/label containment. These are consumer-generated images, not desktop captures or human theme acceptance. |

Reproduction from `winforms/` uses normal outputs:

```powershell
dotnet build tests/RibbonKit.WinForms.Tests/RibbonKit.WinForms.Tests.csproj -c Release --no-restore -m:1
dotnet test tests/RibbonKit.WinForms.Tests/RibbonKit.WinForms.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~PaintFoundationTests|FullyQualifiedName~GradientFoundationTests|FullyQualifiedName~BodyPresentationTests|FullyQualifiedName~CollapsedGroupSeparatorTests|FullyQualifiedName~RendererBoundaryTests|FullyQualifiedName~DensityTests|FullyQualifiedName~TabHeaderClippingTests'
# This session's socket-free execution of that selection:
dotnet artifacts/office2007-g2/direct-runner/bin/Release/net10.0-windows/DirectRunner.dll 'PaintFoundationTests,GradientFoundationTests,BodyPresentationTests,CollapsedGroupSeparatorTests,RendererBoundaryTests,DensityTests,TabHeaderClippingTests'
./eng/Test-PaintFoundation.ps1 -BaselineAssembly './artifacts/designer-sample/packages/ribbonkit.winforms/0.1.0-designer.local20261120t135235944z/lib/net10.0-windows7.0/RibbonKit.WinForms.dll' -Configuration Debug
```

The Designer Sample needs no refresh for this foundation slice: it has no new
theme selector, authored property or review UI to consume. Its existing package,
Forms/resources and retired diagnostic tabs are preserved. Backstage, frame/File
route persistence, repaint-pressure and title-alignment implementations are
untouched. The user's Zoom-loaded black-bar confirmation remains solely in the
existing themes ledger, separate from automated or sampled evidence.

Direct paint/offscreen native checks do not establish actual VS designer,
physical input/IME, mixed-monitor behavior or human appearance acceptance.
No desktop capture, Backstage re-investigation, WPF build or unrelated matrix ran.
The current sequence and authorization are in the themes ledger. G3 follows below; the Office 2007 preset remains pending.

## G3 application button and menu composition — 2026-11-21

Entry: clean `codex/Ribbonkit-Winform` at user commit `df38b3f` (`G2 2007
Theme`). The shared foundations are reusable library behavior. No Office 2007
built-in identity, palette/tile, sample-only presentation or Aero activation is
introduced. G4 remains the normal Blue preset/ribbon slice.

### Shape, drawing and measured input

- Additive `RibbonApplicationButtonShape` has `Rectangular = 0` and `Orb = 1`.
  `RibbonTheme.ApplicationButtonShape` retains Rectangular for every existing
  theme. Nullable `RibbonControl.ApplicationButtonShape` overrides that default;
  WinForms property Reset resumes the application theme. It never rewrites
  `FileMenuMode`, File models, commands, names, KeyTips or saved host preferences.
  `EffectiveApplicationButtonShape` and `ApplicationButtonBounds` expose the
  resolved shape and measured device-pixel bounds.
- Optional immutable `RibbonApplicationButtonAppearanceSettings` provides
  Compact/Touch diameter, caption overlap, leading/trailing clearance, menu
  origin/minimum band, idle/hot/pressed/open/disabled fills, state rims, inner
  highlight, glyph ink/size and shadow ink/blur. Null fills/Empty inks use the
  documented shared fallback; zero widths/size/blur hide their decoration.
  Explicit alpha, gradients and Transparent remain authoritative. All geometry
  and enum validation runs before Theme/override mutation. Existing rectangular
  File appearance and minimum-width settings remain independent.
- Both renderers use shared mirrored circle paint, persistent open feedback for
  either File route and a four-square vector fallback. Borrowed
  `ApplicationButtonIcon` and virtual `DrawApplicationButtonGlyph` replace only
  the glyph; the Form icon is never silently substituted. `DrawFileButton` and
  `DrawFocus` remain replaceable hooks. Circular focus consumes the existing
  FocusInset/FocusStrokeWidth/Accent roles; other command focus is unchanged.
- One measured File target owns input and automation. The Orb's full footprint,
  including visible shadow clearance, is reserved for tabs, caption text and
  TitleBar/TabRow QAT. The File paint clip covers only the sphere/visible shadow, so unrelated body
  invalidations skip both sphere and borrowed-glyph drawing. The host's ordinary
  small caption icon yields to the Orb;
  the Form retains ownership and native system-menu/window semantics. Caption
  overlap is bounded by the existing native resize metrics. Ordinary Forms use
  a tab-band circle. Very narrow integrated hosts also fall back into the tab
  band to avoid caption buttons; crowded Touch headers retain the existing
  available-width clamp, including omission when no File slot fits.
- Circle containment is shared by pointer hit testing, caption HTCLIENT handling
  (including disabled File) and UIA point lookup. Square corner pixels do not
  become File input. Shape/theme changes cancel an armed File press and release
  capture. The existing popup/capture/deactivation/disposal owners remain.
  Orb automation publishes exactly one File child with the original model ID,
  name and KeyTip. ExpandCollapse now also reports/closes the actual Backstage
  route; repeated Expand does not reopen it.

### Native Application Menu composition

- The real Orb stays in RibbonControl. The two-pane popup HWND and its hosted
  surface exclude its screen-space circle/visible-shadow footprint and the
  retained caption strip. The layered popup frame/shadow excludes exactly the
  same regions. This leaves the real File, title and QAT exposed without another
  command owner, visual proxy, HWND input forwarding or editor reparenting.
- Menu origin starts at Orb top-left plus the configurable logical offset
  (default `0,8`). It clamps to the existing working-area policy and budgets
  height below that origin. If that leaves no useful content area, the menu opens
  above the caption/Orb; that fallback pins its initial measured height for the
  session so large/small pane switches retain the same origin. Width/height
  remain constrained by the working area. The minimum top band (default 18 logical
  pixels) grows from measured Orb/caption clearance so targets/native editors do
  not sit beneath the cutout. This is a native font/DPI adaptation, not a claim
  of exact WPF menu-band dimensions.
- The normal native navigation/panes, split actions, nested menus, scrolling,
  validation, footer and borrowed content keep their owners. Native popup/child
  Regions and layered masks refresh with popup geometry. The new overlapping
  composition opens immediately: ordinary dropdown snapshot motion would cover
  the live Orb/caption during entrance. Existing non-Orb popup motion is retained.
- Root `FileMenu.Content` overrides keep their existing ordinary custom-popup
  placement; the cutout composition applies to the two-pane Application Menu.
  Native custom content within its panes is supported and verified. Top-band
  color/frame recipes and the final normal Blue appearance belong to G4/G5.

Example using library APIs, without a built-in 2007 preset:

```csharp
ribbon.Theme = ribbon.Theme with
{
    ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
    ApplicationButtonAppearance = new()
    {
        Background = new(Color.White, Color.LightGray),
        HoverBackground = new(Color.LightYellow, Color.Gold),
        PressedBackground = new(Color.Orange, Color.DarkOrange),
        BorderColor = Color.Silver, GlyphColor = Color.SteelBlue
    }
};
// An explicit rectangle wins until property Reset or assigning null.
ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Rectangular;
```

### Focused evidence and remaining limits

Evidence is under `winforms/artifacts/office2007-g3/`. Latest selected results
are **164 distinct passes and one pre-existing failure** out of 165 cases, with
no skipped cases. This is the union of the retained selections, not a full run.
All **28 G3 cases pass**; the 137 affected existing cases have 136 passes and
the separately reproduced old failure below. Earlier failure/repair logs remain.

- Normal Debug runtime/public consumer and Release runtime/test builds use their
  default outputs and finish with zero warnings/errors (`debug-edge-build.log`,
  `release-edge-build.log`). Initial consumer restores emitted NU1900 because the
  vulnerability endpoint was unreachable; cached-package restores with
  `-p:NuGetAudit=false` and final matching builds are separate from an online
  package audit. XML/API analyzers verify the documented additions in
  `PublicAPI.Unshipped.txt`; the shipped baseline and built-in identities remain.
- The focused in-process xUnit runner from G2 discovers and executes the actual
  built test assembly without VSTest's denied test-host socket. Logs retain each
  selection: `focused-initial.log`, `focused-final.log`, `focused-delivery.log`
  and `focused-edge-final.log`; `focused-scope-summary.json` deduplicates their
  latest case results. Final G3 plus Application Menu selection passes **40/40**
  (28 G3 + 12 existing); the final G3/caption selection before the added edge case
  passed **42/42**. Other affected initial checks cover File, native captions,
  application content/joins, popup creation/layout/motion, designer File settings,
  G2 paint/gradients and renderer boundaries. These are not full-suite or
  successful VSTest/TRX evidence.
- `consumer-debug/comparison.json` and `result.log` retain **32 byte-exact
  rectangular File image pairs**: all four existing families, both renderers,
  96/120/144/192 DPI and idle/hot/pressed/disabled/focused states. Baseline is the
  pre-G2 package `0.1.0-designer.local20261120T135235944Z` identified in G2.
  Its shared old-default paint remains unchanged.
- The public-only `PaintConsumer` passes **12 native layout scopes at actual
  120 DPI**: Classic/Simplified x Compact/Touch x three QAT placements, upper
  Orb native hit testing and retained field HWND/parent/text/selection. It checks
  one File declaration, persistent open state, split-pane primary suppression,
  menu/native-content HWND/parent/text/selection, retained origin, shape
  override/Reset and F10/F KeyTip input. A hidden second process uses native
  UIAutomationCore COM to find one File, retain its runtime ID and exercise real
  ExpandCollapse state. `consumer-edge-final/result.log` is the final current-code
  native/UIA verification; earlier default-image pairs remain in `consumer-debug/`.
  No WPF UIA assemblies or Showcase resources/helpers.
- The old `PopupLayoutTests.Single_column_group_fits_commands_and_uses_the_right_edge_for_paint_and_input(18)`
  hover-color assertion fails identically against current and retained old DLLs
  (expected `-1644826`, actual `-3878685`). `popup-baseline-result.log` and the
  isolated `popup-baseline/` fixture retain that reproduction. It assumes ordinary
  hover ink for a checked toggle. No approval, paint tolerance or unrelated test
  was changed to hide it.
- Inspected consumer-generated Classic/Compact/TitleBar, Classic/Touch/TabRow and
  Simplified/Compact/BelowRibbon images. Raster assertions cover four-corner
  symmetry, clipping and separate state/glyph/focus roles at four DPI values;
  hidden native popup/child Regions verify the Orb/caption cutouts. These are
  direct-print/automated evidence, not desktop captures or visual acceptance.

Run from `winforms/`:

```powershell
dotnet build tests/RibbonKit.WinForms.Tests/RibbonKit.WinForms.Tests.csproj -c Release --no-restore -m:1
dotnet test tests/RibbonKit.WinForms.Tests/RibbonKit.WinForms.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~ApplicationButtonCompositionTests'
./eng/Test-PaintFoundation.ps1 -BaselineAssembly './artifacts/designer-sample/packages/ribbonkit.winforms/0.1.0-designer.local20261120t135235944z/lib/net10.0-windows7.0/RibbonKit.WinForms.dll' -ApplicationButton -Configuration Debug
```

The Designer Sample is not refreshed for this API/geometry foundation: its
authored resources/package and retired diagnostic tabs remain intact. G4 adds
the preset and sample galleries, including the in-place package refresh when
needed. Actual VS designer, physical input/IME, mixed-monitor DPI, material pixels
and human normal-theme acceptance remain separate. No desktop capture or
Backstage black-bar/performance re-investigation ran. No commit, push or branch
change. **G3 completes the shared composition foundation; G4 is next.**

## G4 normal Blue preset and ribbon — 2026-11-21

Implemented on `codex/Ribbonkit-Winform`, starting from the user's clean
`037ac25` (`G3 2007 Theme`). All source/document edits stay under `winforms/`.
No commit, staging, branch change or WPF build. Current acceptance is recorded
once in the themes ledger; this record describes implementation and evidence.

- Added `RibbonThemes.Office2007Blue` and append-only
  `RibbonBuiltInTheme.Office2007Blue = 4`; prior identities remain 0..3.
  XML documentation and `PublicAPI.Unshipped.txt` cover both additions.
- Composed G1's exact opaque caption/body/group valleys, flat `#BFDBFF` tab band,
  selected-tab highlights/open foot, blue text and body outline through existing
  shared appearance roles. G2 supplies the measured frames/label bands, 1-logical
  rims and 3-logical corners; item slots/reduction/layout remain unchanged.
- Hover, pressed, checked and checked-hover use exact duplicate offsets; no
  near-adjacent substitutes. Gold feedback and the Orb do not follow accent
  recoloring. Ordinary command ink/native white editor wells remain independent.
  Split companions retain transparent fills and the shared perimeter/rim.
- G3's default Orb now has the reference silver idle, gold hover, amber
  pressed/open, rim and blue-gray shadow. Its measured Compact/Touch geometry,
  glyph hook, borrowed icon, focus/input ownership and explicit shape Reset remain.
  Rectangular File uses the independent exact blue creased identity ramps,
  white text and solid blue open fill. The optional colored-title recipe uses
  that blue identity ramp; switching it off restores the neutral caption valley.
- Added both permanent View gallery tiles and designer choice parsing. The shared
  sample helper uses the public preset; all drawing remains reusable library
  behavior. Sample startup theme and saved File/frame/material choices remain.
- A real Designer test-host run exposed a stack overflow when modern Backstage
  queried a caption QAT slot: Orb measurement queried `ShowQuickAccess`, which
  resolved the same caption slot. Measurement now queries QAT visibility only
  when reserving a **tab-row** QAT. A focused regression opens/closes modern
  Backstage with the default Orb and caption QAT twice and prints the ribbon.
  The original crash stderr and failing sample output remain in the artifacts;
  both final sample paths pass. This is a bounded composition correction, not a
  reopening of the accepted black-bar/performance investigation.

Focused evidence under `winforms/artifacts/office2007-g4/`:

| Evidence | Observed result and scope |
| --- | --- |
| `focused.log`, `focused-joins.log`, `focused-qat-fix.log`, `focused-summary.json` | **145 distinct passes, zero failures/skips** using actual xUnit discovery/execution in the retained in-process runner. G4's 14 cases cover exact tokens, both painters/four DPIs, hard state sampling, split transparency, rectangular File, measured panel clipping/rims, selected-tab/body joins, family/shape/application Reset, preferences, inert preview and the caption-QAT regression. Affected G2/G3, renderer/header, Office2010, designer serialization/Undo/Redo and RibbonForm cases also pass. This is not a full-suite run. |
| `release-build-qat-fix.log` and final Debug build logs | Release runtime/protocol/tests and in-process runner; Debug runtime, designer server/net472 client, Showcase, permanent Designer Sample and its dedicated check host all build with zero warnings/errors. Additive public API analyzer checks pass. |
| `public-final/evidence/result.log` | Isolated **package-only** consumer, without Showcase resources/helpers: 12 native scopes at actual 120 DPI (Classic/Simplified × Compact/Touch × all three QAT positions), authored model identity, Orb/rectangular override/Reset and both File routes. Native ribbon editor HWND/parent/text/selection survive four preset changes. |
| `showcase-final/`, `designer-final/` | Each actual authored sample path passes the same 12 scopes, real theme/accent/title gallery actions, family selection and independent Application/accent/caption Reset. Showcase also verifies fresh startup restore for family/accent/title and saved File/frame/material preferences using isolated settings. No diagnostic tabs or test methods were added to normal Designer Sample startup. |
| `public-final/evidence/paint-*.png` | Eight direct-paint atlases: both public painters × 96/120/144/192 DPI. Inspected generated ribbon/panel/state/Orb images and actual sample prints; automated tests check exact pixels, clips, border widths and join openings. This is sampled/offscreen evidence, not desktop capture or human appearance acceptance. |
| `default-compatibility/comparison.json` | **32 byte-exact pre-G2/current paint pairs** across four old families, both painters and four DPIs, plus eight existing public-only native group scopes. No old palette was edited. |
| `designer-refresh-final.log` | Refreshed the existing Designer Sample in place using `eng/Update-DesignerSample.ps1 -Configuration Debug -NoBuild`, package `0.1.0-designer.local20261120T190545758Z`. Authored Forms/UserControls/resources remain; normal Debug output is ready. |

The first independent restore retried blocked NuGet lookups before succeeding;
the retained verifier now uses the existing restored package cache and final
restore/build completes without warnings. VSTest's previously recorded test-host
socket restriction was not retried; the in-process runner executes real xUnit
cases. The known old checked-hover popup assertion from G3 is outside this
selected run, unchanged and not relabeled as passing.

Reproduce from `winforms/` with normal configurations/output folders:

```powershell
dotnet build tests/RibbonKit.WinForms.Tests/RibbonKit.WinForms.Tests.csproj -c Release -m:1 -p:NuGetAudit=false
dotnet artifacts/office2007-g2/direct-runner/bin/Release/net10.0-windows/DirectRunner.dll 'Office2007ThemeTests,ApplicationButtonCompositionTests,RibbonFormTests'
./eng/Test-Office2007Shell.ps1 -Configuration Debug
./eng/Test-DesignerSample.ps1 -Configuration Debug -Check --verify-office2007-shell
```

Review through **View > Themes > Office 2007 Blue** in either existing sample.
Normal ribbon appearance is pending live review before G5. Dedicated Application
Menu bands/frame/footer palette, G6 Options/companion surfaces and the G8 Aero
palette are not delivered here; those surfaces currently use shared fallback
roles. No Aero/material is enabled by theme selection. Actual Visual Studio
designer, physical input/IME, mixed-monitor DPI and material-pixel acceptance
remain separate. No desktop capture, full matrix or unrelated WPF work ran.

## G5 Application Menu palette — 2026-11-21

Implemented from the user's clean `245fa9a` (`G4 2007 Theme`) on
`codex/Ribbonkit-Winform`. The themes ledger owns the user's G4 review and G5
continuation authorization. All edits remain within `winforms/`; no commit,
staging, branch change, WPF build, diagnostic tab or desktop capture.

The preset now composes the G1 menu palette through shared appearance settings:
flat `#BED3EC` side band, exact creased `.44` blue top band, exact `.47`/`.96`
footer valley, white inner rim, `#9BAFCA` outer ink, pale navigation and distinct
default/active panes, `#9F9F9F` inner frame, blue headings, gray navigation separators and independent blue
creased resting footer buttons. Gold/orange feedback stays independent of
identity-accent recoloring. An open split primary has an approximately 32%
gold wash (82/255 alpha), its arrow retains full gold, and pressing one half
retains that half's deeper feedback. Native scrolling and soft popup shadow
continue through the existing shared owners.

Four additive public settings/context members are documented in XML and
`PublicAPI.Unshipped.txt`:

| Addition | Contract |
| --- | --- |
| `ApplicationMenuAppearance.TopBandBackground` | Null retains SurroundBackground. Paint spans the complete band above the measured inner frame, even with a partial repaint clip. No new fixed-height geometry or target reservation. |
| `ApplicationMenuAppearance.OuterBorderColor` | Empty follows PopupBorder; Transparent hides ink. Applies only to the root two-pane menu, preserving ordinary/nested popups and root File custom-content overrides. Existing renderer width/radius hooks win. |
| `ApplicationMenuAppearance.FooterIdleBorderColor` | Empty follows PopupBorder. Supplies idle/disabled ink before state chrome; explicit FooterBorderColor remains authoritative for every state. This permits blue resting rims and gold active rims without changing old precedence. |
| `RibbonPaintContext.IsApplicationMenu` | Default false. Actual root menu surface, native frame and layered frame contexts carry true; the layered cache includes the role. Renderer hooks keep the original Theme and existing paint/geometry owners. |

G3's measured Orb/caption/native/layered exclusions and placement remain intact.
No File routing or window/material preference is changed by this palette. Root
File custom-content overrides retain their ordinary popup path; native controls
inside two-pane menu pages retain their real parent/handle and validation path.

Focused evidence under `winforms/artifacts/office2007-g5/`:

| Evidence | Observed result and scope |
| --- | --- |
| `focused.log`, `focused-final.log`, `focused-separators.log`, `focused-summary.json` | **199 distinct passes, zero remaining failures/skips**, actual xUnit discovery/execution in the retained in-process runner. G5's 13 cases cover exact stops/ink, both painters at 96/120/144/192 DPI, complete-band sampling under clips, root-only border roles, Empty/Transparent/null resets, footer state/explicit-ink precedence and old-family frame compatibility. Affected Application Menu content/surfaces/actions/joins, native window creation, frame geometry, G3 and G4 regressions also pass. The first four failures were a new fixture omitting its default-pane flag; correcting that fixture and adding footer samples passes all four. No paint tolerance or approval was changed. |
| `debug-server-separators.log`, `release-build-separators.log`, `showcase-build-separators.log`, `designer-host-separators.log` | Normal Debug runtime/designer server, Showcase and dedicated Designer check host; normal Release runtime/protocol/tests/runner compile with zero warnings/errors. Additive public API checks pass. The net472 designer client is unchanged and its existing Debug output is reused. |
| `public-delivery/current/evidence/result.log` | Independent **package-only** consumer with no Showcase resources/helpers passes eight native scopes at actual 120 DPI: Classic/Simplified × Compact/Touch × Orb/rectangular. Real surface/native/layered hooks carry the root role; pane origins remain stable, large panes retain native scrolling, and resizing followed by closing releases state. Actual split/footer actions invoke once, disabled inheritance holds, nested Escape closes deepest first, native validation vetoes pane switching, and page editor HWND/parent/text/selection survive pane changes. |
| `public-delivery/comparison.json` | **32 byte-exact G4/current old-family Application Menu/frame paint pairs**: four earlier families × both public painters × four DPI values. G4 package `0.1.0-designer.local20261120T190545758Z` is retained. Its compiled consumer excludes new APIs through a baseline-only compilation symbol; current checks use the new public contract. |
| `showcase-delivery/`, `designer-delivery/` | Both actual authored sample paths pass eight native scopes each using the existing Office 2007 tile and File models. Default/active pane prints and split-state atlases are retained. Inspected independent default menu/split states and sample menu prints; these are generated offscreen evidence, not desktop capture or live appearance acceptance. |
| `designer-refresh-separators.log` | Existing Designer Sample refreshed **in place**, normal Debug, package `0.1.0-designer.local20261120T225131490Z`; authored Forms/UserControls/resources preserved. Check dispatch remains under the dedicated test host. |

The initial consumer compilation caught use of an internal enabled-state helper;
the final portable source uses only public Enabled/action behavior and verifies
inherited disabling by the absence of command execution. Retain the earlier
failed compile logs. The known old checked-hover popup assertion from G3 is
outside this selected scope and remains unchanged; no full-suite result is
claimed. The previously recorded VSTest socket restriction was not retried.

From `winforms/`, normal configurations and default output folders:

```powershell
dotnet build tests/RibbonKit.WinForms.Tests/RibbonKit.WinForms.Tests.csproj -c Release -m:1 -p:NuGetAudit=false
dotnet artifacts/office2007-g2/direct-runner/bin/Release/net10.0-windows/DirectRunner.dll 'Office2007ApplicationMenuTests,ApplicationMenuTests,ApplicationMenuContentTests,ApplicationMenuJoinTests,FrameGeometryTests'
./eng/Test-Office2007ApplicationMenu.ps1 -BaselinePackageVersion '0.1.0-designer.local20261120T190545758Z' -Configuration Debug
./eng/Test-DesignerSample.ps1 -Configuration Debug -Check --verify-office2007-menu
```

Select **View > Themes > Office 2007 Blue** and open the existing Application Menu
File route for live review. A saved Backstage route still opens Backstage; shape
does not choose routing. The G5 review checkpoint and subsequent G6 authorization
are recorded in the themes ledger. G8 Aero remains separate. Actual VS,
physical input/IME, mixed-monitor DPI, desktop material pixels and visible motion
acceptance remain separate. Accepted Backstage black-bar/performance fixes were
not re-investigated.

## G5 caption clearance correction — 2026-11-21

The user reported that the menu looks good but supplied a screenshot showing
its top beneath the caption. The native code anchored the popup at the Orb's
top plus MenuOffset while excluding the **whole caption** from both native and
layered paint. With a normal on-screen caption, that requested origin sits
inside the excluded band, removing the top border and upper portion of its fill.
Earlier consumer hosts at `(-16000,-16000)` were work-area-clamped away from
their captions; their successful printed menu images did not verify this overlap.

The shared composition now floors a below-opening menu's requested Y at the
caption bottom **before** computing content clearance and the work-area fallback.
The Orb still overlaps the frame; its real input and cutout remain. Where the
Orb determines top clearance, moving the origin lowers the frame and shrinks
the top band by the same amount, retaining measured command/header/footer screen
positions. Explicit lower offsets and ordinary hosts retain their requested
placement; the existing above-caption/pinned-height path remains. No new API,
window, renderer, layout engine or reparenting was added. MenuOffset's XML and
README contract now state that caption clearance is authoritative.

Evidence under `winforms/artifacts/office2007-g5-caption/`:

- Six new `ApplicationMenuCaptionPlacementTests` cases cover Compact/Touch at
  96/120/144/192 DPI, before/after frame pixels through the shared exclusion and
  painter, actual measured target/footer screen positions, lower offsets, plain
  hosts, working-area/above-caption fallback, and hidden native window/hosted
  surface Regions. The old origin loses the far-side top pixel; the corrected
  origin preserves the exact menu-border ink while retaining the Orb cutout.
  Generated frame-only PNGs in `paint/` are regression evidence, not desktop captures.
- `focused.log` records **62 passes / 1 known old failure / 63 cases** with actual
  in-process xUnit execution. The existing 18-point checked-hover popup assertion
  still reports expected `-1644826`, actual `-3878685`, exactly the retained G3
  baseline reproduction. Its paint/test/tolerance was not changed. Latest focused
  placement results and build output are in `placement-final.log` and
  `release-build-final.log`; this is not a full-suite result.
- Normal Debug runtime/designer server, Showcase and the dedicated Designer
  test host compile cleanly. The persistent Designer Sample is refreshed in place
  (`designer-refresh.log`, package `0.1.0-designer.local20261120T230046568Z`).
- `public-final/`, `showcase/` and `designer/` retain the focused package/sample
  reruns: each passes eight native scopes at 120 DPI. The independent consumer
  also passes actions, nested Escape, validation/editor retention and 32 byte-exact
  old-family frame/menu comparisons. Two initial offscreen consumer attempts
  found no visible popup; the activation-diagnostic replay and final uninstrumented
  rerun pass, with earlier failure logs retained and no assertions weakened.
  These offscreen scopes check regression safety; the six geometry/Region cases
  specifically reproduce the on-screen caption relationship.

Current user review and final evidence remain in the themes ledger. No desktop
capture or computer-use tool was used, and accepted Backstage fixes were not
reopened. The themes ledger owns the follow-up live result and continuation.

## G6 Options and companion surfaces - 2026-11-21

This is the initial G6 delivery record. Its solid body/popup backing, white list
wells and logical-width button outlines are superseded by the
[gradient/thin-outline correction below](#g6-gradient-and-button-outline-correction--2026-11-21).
Keep these earlier evidence paths as historical results, not final acceptance.

G6 composes the existing shared Options/customization, editor, gallery, menu and
passive tip pipelines in `RibbonThemes.Office2007Blue`. Native layout, density,
DPI, input, draft sessions, validation, accessible objects and renderer hooks keep
their existing owners. No sample-only styling or layout engine was added.

The reference remains the read-only WPF Office 2007 tokens and shared Options
styles, adapted to native controls. The user's background follow-up deliberately
changes the initial white dialog backing: the **customization body and ordinary
popup backing now share the Application Menu surround `#BED3EC`**. The Options
footer shares the exact menu footer gradient, including its repeated `.47` stops.
This uses existing `BackgroundColor`, `FooterBackground`, `PopupBackground` and
`MenuAppearance.OverflowBackground` roles. Library secondary group/rename dialogs
follow the same body color. The navigation rail remains `#F5F4F3`; native field,
list/tree and gallery-strip wells remain white. ScreenTips retain their separate
near-white `#FBFDFF` fill and `#8DB2E3` rim. These are explicit opaque fills, not
unpainted areas or transparency workarounds.

Ordinary dialog actions use `#FBFDFF`, `.48 #E1EDF9`, `.54 #BCD3EA`, `#E9F2FB`,
with idle rim `#7D9EC0`, gold hover and orange pressed feedback. Primary OK uses
the creased rectangular-File reference blue (`.45 #3585C6`, `.45 #1F63A6`), rim
`#17578F`, white text, a 22% white hover wash and a 16% black pressed wash. All
stops/axis/alpha survive mapping. These explicit primary fills retain reference
blue when Accent changes; applications can override/reset each role. Gold row,
tree, navigation and icon-picker selections stay independent from Accent.
Explicit button/editor/gallery outline widths are one logical pixel; existing
Options row/frame defaults keep their one-device-pixel policy. Native scrollbar
skin, nonclient captions and application-owned page styling remain with their
existing owners.

Four additive public members have XML documentation and eight getter/init entries
in `PublicAPI.Unshipped.txt`; the shipped baseline is unchanged:

| Member | Contract |
| --- | --- |
| `RibbonStateChromeSettings.IdleBorderColor` | Replaces idle fallback where a surface already paints idle chrome. Empty restores fallback; Transparent hides ink. Active-state fallback stays independent; explicit surface ink/width wins. Does not make idle commands active. |
| `RibbonTipAppearanceSettings.ScreenTipBackground` | Optional opaque fill, including exact gradients. Null restores PopupBackground/Surface. Every stop validates before Theme/BuiltInTheme mutation. |
| `RibbonTipAppearanceSettings.ScreenTipBorderColor` | Independent frame ink, including alpha. Empty restores PopupBorder; Transparent hides ink. Width/radius hooks retain ownership. |
| `RibbonPaintContext.IsScreenTip` | Flags both native passive-tip and layered-edge frame/shadow hooks without replacing the theme. Defaults false; ordinary popups, root Application Menu and KeyTips retain their roles. The layered cache includes this scope. |

Focused evidence is under `winforms/artifacts/office2007-g6/`:

| Evidence | Observed result and scope |
| --- | --- |
| `focused-background.log` | **232/232 pass, zero failures/skips**, in one final affected run after the background change. Actual xUnit discovery/execution uses the retained in-process runner because the recorded VSTest socket restriction remains; no assertion or tolerance was bypassed. Includes 16 G6 cases, Options shell/list/tree/field/icon-picker/popup contracts, editor/gallery/menu/tip behavior, G5 menu and caption-placement regressions. Direct paint/sampling/clip checks cover 96/120/144/192 DPI and both shared painters. |
| `public-background-final.log`, `public-background-final/comparison.json` | Package-only independent consumer passes four native Options scopes at actual 120 DPI (Compact/Touch x Classic/Simplified), mode-appropriate customization and QAT pages, actual button/list states, native HWND/parent/text/selection/scroll retention, independent drafts, rejected/successful Apply and Cancel checkpoints. Actual native content backing and ordinary popup pixels are asserted; four-DPI public paint outputs also pass. Secondary group/icon-picker and rename dialogs pass shared-name-frame/blank-name validation and Reset/Cancel. QAT Apply/checkpoint/Reset/Cancel and rejected/accepted OK commit pass. **40 byte-exact G5/current old-family pairs** cover 32 tip/gallery/menu sheets and eight real Options button images. No Showcase resources/helpers are referenced. |
| `showcase-background-final/result.log`, `designer-background-final/result.log` | Both actual authored sample paths pass four native Options scopes each at 120 DPI, including the blue body, button/list state paint, all mode-appropriate pages, ownership and draft/validation checks. Checks stay in the dedicated Designer Sample test host; no diagnostic tabs or checks were added to the ordinary Designer Sample. |
| `release-background-build.log`, `debug-background-build-final.log`, `showcase-background-build-final.log`, `designer-background-host-build-final.log` | Normal Release runtime/tests/direct runner and normal Debug runtime/designer server, Showcase and dedicated package-only host build with zero warnings/errors. The unchanged net472 designer-client output is reused; no new client or actual VS review is claimed. |
| `designer-background-refresh.log` | Existing Designer Sample refreshed in place with **`0.1.0-designer.local20261121T024358170Z`**, normal Debug/default output. Authored Forms/UserControls/resources were preserved. |

The first new button cases exposed an absent disabled-primary fallback stroke;
authoring the existing `ButtonBorderWidth = 1` preset role fixes it. Initial
consumer fixtures also used an incorrect control name and assumed both layout
pages coexist; they now follow the mode-specific public dialog contract. The
background follow-up's first ordinary-popup fixture placed its opener after a
reduced 35-command group, so no surface opened; a dedicated small visible test
tab fixes the fixture. Earlier logs/outputs (`focused.log`, `focused-final.log`,
`public/`, `public-final/`, `public-background/`) are retained separately. No
production activation workaround, weakened assertion or relaxed tolerance was
introduced for these fixture failures.

Reproduction from `winforms/` (normal SDK/configuration/output locations):

```powershell
# Build the actual xUnit in-process runner, then select the affected classes.
dotnet build artifacts/office2007-g2/direct-runner/DirectRunner.csproj -c Release -m:1 -p:NuGetAudit=false
dotnet artifacts/office2007-g2/direct-runner/bin/Release/net10.0-windows/DirectRunner.dll 'Office2007CompanionTests,Office2010OptionsTests,OptionsShellAppearanceTests,OptionsRowAppearanceTests,OptionsTreeAppearanceTests,OptionsFieldAppearanceTests,TipAppearanceTests,ScreenTipTests,Office2007ApplicationMenuTests,ApplicationMenuCaptionPlacementTests,EditorAppearanceTests,GalleryAppearanceTests,MenuAppearanceTests,OptionsPopupTests,OptionsIconPickerAppearanceTests'
./eng/Test-Office2007Companions.ps1 -BaselinePackageVersion '0.1.0-designer.local20261120T230046568Z' -Configuration Debug
./eng/Test-DesignerSample.ps1 -Configuration Debug -Check --verify-office2007-companions
# Showcase also accepts --verify-office2007-companions <fresh-output-directory>.
```

Generated native prints were inspected for Compact/Classic, Touch/Simplified,
secondary group controls, companion paint and the ordinary popup backing. This
is offscreen/native-print evidence, not desktop capture or human acceptance.
No full-suite, WPF build, actual VS, physical input/IME, mixed-monitor transition,
continuous visible-motion or desktop-material result is claimed. Existing
renderer width/radius/shadow policy and app page styles remain independent.
The known pre-G2 checked-hover popup-color failure remains outside this selected
scope; it was not rerun or changed. Accepted Backstage black-bar/performance,
alignment and saved frame/File-route fixes were not reopened.

Current live review belongs only in the themes ledger. **Stop at G6 review before
G7 composed normal-theme review.** G8's Aero palette remains a separate slice.

## G6 gradient and button-outline correction - 2026-11-21

The user's native/WPF comparison identified a real G6 appearance gap: using the
Application Menu's **solid frame-band color** did not supply the reference's
**content gradient**. The reference popup/gallery and Options content brush is
`0 #DBE6F4`, `.17 #C8D9ED`, `1 #E3F4FF`. The correction uses that complete ramp
through `PopupBackgroundFill` and `MenuAppearance.OverflowBackground`, covering
ordinary/nested/context menus, galleries, choice dropdowns and group flyouts.
The existing measured popup/row background coordinates and actual renderer hooks
remain in use. The dedicated Application Menu bands/panes keep their own palette.

Two additive immutable Options roles, `BackgroundFill` and `ListBackgroundFill`,
add four getter/init entries to Unshipped with XML documentation. Null restores
the matching Color/Surface fallback; assigning BackgroundColor/ListBackgroundColor
clears only its matching fill. All stops must be opaque, with validation before
Theme/BuiltInTheme mutation. Existing families leave these roles null. Choose
native dialog construction settings before opening, as with existing background
colors. Application pages retain their own styling.

Library-owned body containers sample one content canvas. Native list/tree/icon
cells and their unused space sample each complete client viewport, including
buffer-local rows, partial clips and resized/scrolled controls. Paint-only native
erase/unused-space handling retains HWNDs, selection, draft data, hit targets and
scrollbars. Mouse, programmatic and keyboard scrolling invalidate the gradient
well so native pixel copies do not move its colors. List empty-space clipping
queries visible rows only. The navigation rail stays flat; text editor wells
and the inline gallery strip remain white. Secondary group/rename bodies and
icon wells use the same reusable roles. No sample resources or app-page traversal
is required.

The thick button frame came from the preset's DPI-scaled logical outer stroke
and primary inner ring. The preset now uses the existing device-pixel default
outline path and omits that extra idle/primary ring. A primary with authored
idle outline ink also retains a one-device-pixel disabled outline. Explicit
ButtonBorderColor/Width, transparent/zero overrides and keyboard focus cues keep
their authority. The old families' default button paint remains unchanged.

Final evidence under `winforms/artifacts/office2007-g6/`:

| Evidence | Result and boundary |
| --- | --- |
| `gradient-input-focused.log` | **275/275 pass**, zero failures/skips, actual xUnit in-process execution. Includes 16 new gradient/thin-outline cases, the original G6 contracts, affected Options/editor/gallery/menu/tip regressions, G2 paint foundations and G5 menu/caption placement. Samples at 96/120/144/192 DPI verify complete-well clipping and buffer coordinates, opacity/atomic validation, Color/null reset semantics, one-device-pixel default rims and explicit wider logical widths. Native cases cover menus, galleries, collapsed groups, both densities/layouts, empty wells, scroll/resize, keyboard End selection/full-well invalidation and handle/parent retention. |
| `public-gradient-input-final.log`, `public-gradient-input-final/comparison.json` | Independent public-only package consumer passes four native Options scopes at actual 120 DPI, exact body/list/popup gradient pixels, four-DPI public paint, secondary icon/rename dialogs, native ownership and drafts/validation/Apply/OK/Cancel/Reset. **40 byte-exact G5/current old-family image pairs** pass. No Showcase resources/helpers. |
| `showcase-gradient-input-final/result.log`, `designer-gradient-input-final/result.log` | Both existing sample paths pass four native Options scopes each at 120 DPI, with exact body/list gradient assertions plus prior state, editor and draft contracts. Dedicated Designer test host only; authored ordinary sample startup stays unchanged. |
| `gradient-input-release.log`, `gradient-input-debug.log`, `gradient-input-showcase-build.log`, `gradient-input-host-build.log` | Normal Release runtime/tests/runner and normal Debug runtime/designer server, Showcase and dedicated package host build cleanly. Unchanged designer-client output is reused. |
| `gradient-input-designer-refresh.log` | Existing Designer Sample refreshed in place with **`0.1.0-designer.local20261121T034028646Z`**, preserving authored Forms/UserControls/resources and default Debug output. |

The native Options, gallery, popup and button outputs were inspected, including
unused wells and all button corners in the generated native prints. A generated
print caught the navigation rail's unused area initially being treated as a list
well; the shared native fill path now excludes navigation. Initial new fixtures
also used the wrong Simplified search name and rounded an explicit logical width
instead of applying its established ceiling rule at 144 DPI; those fixtures were
corrected without relaxing pixels or tolerances. Earlier `gradient-*` and
`public-gradient-final/` evidence is retained separately. The final selected run
is clean; no full suite or VSTest socket success is claimed.

Reproduction uses the G6 commands above, adding `Office2007GradientSurfaceTests`
and `PaintFoundationTests` to the affected xUnit selection. The existing
`Test-Office2007Companions.ps1` and `--verify-office2007-companions` sample modes
now assert gradients as well as the original state/draft contracts.

Native print/message evidence remains separate from live appearance acceptance,
actual VS, physical input/IME, mixed-monitor DPI and desktop/continuous-motion
review. Native scrollbars and caption policy are retained; no WPF build, desktop
capture, layout-engine rewrite or Backstage investigation was performed. Keep G6
at review before G7; the Aero palette remains G8.

## G6 split, QAT, Simplified groups and overflow correction - 2026-11-21

The user identified two more G6 paint gaps, then added Simplified group separators
and overflow feedback to the same session. This remains G6, not G7 continuation.

- A vertical split sampled one whole-command ramp under its two clips, so the
  arrow half showed only the nearly uniform lower portion. Additive
  `CommandAppearance.SplitFillUsesPartBounds` enables a complete fill ramp per
  half while keeping the shared perimeter, joined edge, separator, actual bounds
  and renderer callbacks. It is true for 2007 and false by default. Existing
  BackgroundBounds carries the part's paint canvas to chrome overrides. Both
  vertical/horizontal splits and QAT projections reuse the shared painter.
- Native caption adaptation rewrites Hover/Pressed/Checked and composited QAT
  feedback also used the caption backdrop recipe. Four optional CommandAppearance
  QuickAccess hover/pressed/checked/checked-hover fills now precede that adaptation.
  The 2007 preset authors its gold/orange ramps and opacity 1. Alpha, duplicate
  stops, direction, null/member reset and QuickAccessBackdropOpacity remain
  independently adjustable. Ordinary menus and header utilities retain their
  existing recipes; QAT borders/rims still use shared command appearance.
- Additive `BodyAppearance.GroupPanel.ShowInSimplified` defaults true to preserve
  existing compact-panel consumers. Office 2007 sets it false: Classic frames and
  labels remain, Simplified uses its existing measured separators. Shared hooks
  retain group-frame context, and opt-in popup dividers use the same separator
  color/width roles as the ribbon. No layout/reduction/DPI/editor targets change.
- QAT and Simplified overflow use the gold hover and orange pressed/open fills.
  Their existing open-state retention, pointer/keyboard routing and popup ownership
  stay with the control. No diagnostic tabs or sample-only paint were introduced.

The six public members have XML documentation and twelve getter/init entries in
Unshipped. The shipped API baseline is unchanged. Remaining appearance review
belongs in the themes ledger; current verification is recorded below.

Final evidence for this follow-up is under `artifacts/office2007-g6/`:

| Evidence | Observed scope/result |
| --- | --- |
| `command-feedback-focused-complete.log`, `command-feedback-release-complete.log` | **117/117 pass**, zero skips/failures in the final selected xUnit run; normal Release build is clean. The 19 new cases cover both shared painters, whole/per-part gradient coordinates and clips, caption-derived QAT colors, pressed/checked/checked-hover/overflow states, horizontal QAT split fills, all-stop alpha/direction/reset, menu/header isolation, Classic-frame parity, and native Compact/Touch separators/open-overflow/target retention. Direct paint checks use 96/120/144/192 DPI. Affected geometry, gradient foundations, Simplified/source ownership, QAT and preset regressions also pass. |
| `public-command-feedback-final/`, `public-command-feedback-final.log` | Public-only package consumer passes native QAT/overflow hover/press and four-DPI split-half/QAT paint, plus prior Options four-scope/native editor/draft/validation/secondary-dialog checks. **40 byte-exact old-family pairs** match the retained G5 package; the 32 painter sheets now also include checked-hover split and QAT states. No Showcase resources/helpers are referenced. |
| `showcase-command-feedback/result.log`, `designer-command-feedback/result.log` | Both actual authored sample paths pass the new native QAT/overflow feedback checks and public split canvas/paint checks, plus the four native Options scopes each at actual 120 DPI. Generated native prints were inspected; no desktop capture. |
| `command-feedback-debug-server.log`, `command-feedback-showcase-build-final.log`, `command-feedback-host-build-final.log`, `command-feedback-designer-refresh.log` | Normal Debug runtime/designer server, Showcase and dedicated host are clean. Designer Sample refreshed in place with **`0.1.0-designer.local20261121T041212160Z`**; authored Forms/UserControls/resources retained. Unchanged net472 client output is reused. |

The initial broader run (`command-feedback-focused.log`) selected two existing
SplitChromeRenderingTests assertions that expect ordinary hover `#E6E6E6` even
for checked hover. Actual output is `#C4D0E3`; the retained G5/current independent
frames both produce exactly that ARGB at the checked-hover sample
(`legacy-split-pixels.json`), and all expanded default image pairs match. Those
two legacy assertion failures remain recorded separately, without edits to their
expectations or tolerances. The final 117-case selection does not claim they pass.

Initial new QAT fixtures also used an unattached source reference, correctly
rendered unavailable; they now use a real shared command owner. The overflow
probe retains the pressed observation across multiple callbacks and checks the
open snapshot before queued offscreen deactivation. A public-consumer interaction
fixture originally left a click affecting its following popup check; the new
interaction scope now runs last. Earlier attempts/logs remain intact. These are
verification-fixture corrections, not runtime activation workarounds.

Reproduce with the existing G6 package/sample commands. The affected xUnit class
selection is `Office2007CommandFeedbackTests,CommandSplitGeometryTests,GradientFoundationTests,PaintFoundationTests,SimplifiedRibbonTests,QuickAccessTests,QuickAccessFrameTests,Office2007ThemeTests`.
Current evidence is focused xUnit/native-print/message execution, not a full
suite, actual VS, physical input/IME, mixed-monitor, desktop material or continuous
motion review. No WPF build, diagnostic tabs or accepted Backstage investigation
was introduced. Stop for the user's remaining G6 visual review before G7/G8.

## G6 caption hover outlines and slice completion - 2026-11-21

This finishes G6 with the clarified caption-button request: outlines appear on
hover, with the corresponding pressed rim retained while the pointer is inside.
Idle and pressed-outside buttons stay borderless. The preset supplies blue
hover/pressed border inks through existing `CaptionButtonAppearance.StateChrome`
and independent red Close inks through `CloseStateChrome`. Existing creased fills,
two-logical-pixel inset, three-pixel radius and one-device-pixel stroke remain.
No new public member or API baseline entry is required for this palette change.

The shared caption renderer, restore-glyph cutout, native hit testing, capture,
cancellation and window actions are unchanged. Callers can independently override
or reset both chrome groups; Transparent still suppresses a state rim. No idle
outline or new window-frame decoration was added.

Final follow-up evidence under `artifacts/office2007-g6/`:

| Evidence | Observed scope/result |
| --- | --- |
| `caption-outline-focused-final.log` | **55/55 pass**, zero failures/skips, actual xUnit in-process execution. Nine new cases cover 96/120/144/192 DPI, all three buttons, hover/press/idle/pressed-outside, opaque/alpha planes, one-device-pixel rims, symmetric corner alpha, exact underlying ramp samples, Transparent overrides, restore underlay, caller clipping and independent resets. Affected caption appearance, Options/caption refinement, command feedback and menu-placement checks also pass. |
| `public-caption-outline.log`, `public-caption-outline/comparison.json` | Independent public-only package consumer passes native minimize/maximize-or-restore/close hover and pressed paints, cancellation and unchanged HWND/bounds/window state. Prior four-scope Options, native editor/draft/validation, secondary-dialog, popup-gradient and command-feedback checks pass. **40 byte-exact retained-G5/current old-family image pairs** match, without Showcase resources/helpers. |
| `showcase-caption-outline/result.log`, `designer-caption-outline/result.log` | Both existing sample paths pass native caption state paints and prior command/Options checks, including four Options scopes each at actual 120 DPI. The dedicated Designer test host carries the checks; ordinary sample startup and authored forms stay unchanged. |
| `caption-outline-release-final.log`, `caption-outline-debug.log`, `caption-outline-showcase-build.log`, `caption-outline-host-build.log` | Normal Release runtime/tests/runner and Debug runtime/designer server, Showcase and dedicated package host build with zero warnings/errors. Unchanged net472 designer-client output is reused. |
| `caption-outline-designer-refresh.log` | Designer Sample refreshed in place with **`0.1.0-designer.local20261121T044540709Z`**, preserving authored Forms/UserControls/resources and normal Debug output. |

The initial selected run (`caption-outline-focused.log`) also exposed three
existing `CaptionButtonBackdropTests` failures: two integrated opaque cases
expect alpha 255 but observe 0, and the integrated material restore case expects
24 but observes 46. An isolated actual-xUnit host using the retained G5 runtime
reproduces exactly those three failures, with **5/8 passing**
(`caption-backdrop-retained-g5.log`, clean build in `caption-baseline-build.log`).
They remain unresolved baseline assertions; the final 55-case selection does not
claim they pass. The earlier two checked-hover split assertion limits also remain
recorded above. No baseline expectations or tolerances were changed.

Four initial new fixture failures compared named Transparent's ARGB against a
cleared bitmap's different zero-alpha RGB. The fixture now compares the untouched
bitmap pixel captured before drawing; the clip assertion remains exact. Earlier
attempts are retained. Native consumer checks send hover/down/cancel messages
without a button-up action and verify that native window identity/state remains.
Generated close and maximize hover prints were inspected in the existing samples.

Reproduce with the existing G6 package/sample commands; the caption-focused
xUnit selection is `Office2007CaptionOutlineTests,CaptionButtonAppearanceTests,OptionsAndCaptionRefinementTests,Office2007CommandFeedbackTests,ApplicationMenuCaptionPlacementTests`.
VSTest socket restrictions still use the retained actual-xUnit in-process runner;
no full-suite result is claimed. Native print/message and four-DPI paint evidence
do not establish actual VS, physical input/IME, mixed-monitor, desktop material
or continuous-motion acceptance. G6 implementation is complete; stop before G7's
composed normal-theme review. G8 Aero and accepted Backstage work remain separate.

## Framed Backstage workspace before G7 - 2026-11-21

Entry: clean `codex/Ribbonkit-Winform` at the user's `091ac85` (`G6 2007 Theme`).
This implements the subsequent native Backstage design proposal and its padding
revision, separately from G7. The proposed document preview/tasks are illustrative
application content; the library supplies the reusable shell and keeps existing
Info/Print controls, data and actions. Current design approval and runtime review
status belong only in the themes ledger.

### Appearance, geometry and public contract

- Appended `RibbonBackstageStyle.Office2007 = 3` selects the new presentation
  independently of palette. The Office2007Blue preset selects it when a host's
  existing route opens Backstage. It does not change FileMenuMode, an explicit
  rectangular File override or window material/frame preferences.
- A 48-logical-pixel blue header uses the File reference crease: `#6FB4E4`,
  `.45 #3585C6`, `.45 #1F63A6`, `#4C93CD`, with white text. The measured File visual
  continues through the caption/header join using the existing File/glyph painter;
  it is not reparented. The native return target retains validation, cancellation,
  keyboard focus and KeyTips. The real upper Orb remains a File return target.
  Hiding Back removes its target/KeyTip while retaining File paint and lower-File
  pointer return. Native caption buttons and their hover-only rims are unchanged.
- One shared blue rim encloses the navigation and white page, with one white inner
  highlight and one divider. No separately boxed page remains. Frame gutter is
  **8 logical pixels** on all sides, radius **3**, and default outer/divider and
  inner strokes are **one device pixel**. Explicit stroke widths remain logical.
  The divider stops inside the shared highlight. Native page content retains
  16-pixel padding and the existing renderer's page-border/corner hooks.
- Navigation samples the full interior valley `#DBE6F4`, `.17 #C8D9ED`, `#E3F4FF`.
  Gold selected/hover and orange pressed rows reuse the preset ramps and state
  rims. `BackstageNavigationPadding` defaults to **4 logical pixels** in this
  presentation and controls both side clearances and the first row's top gap.
  Separators use the same row bounds with zero extra inset. Footer pinning, short
  host fallback, native scrolling, font-aware density and DPI negotiation remain
  in the shared layout algorithm; the new family arranges it inside one measured
  workspace snapshot. Snapshot values survive model/font disposal.
- `BackstageAppearance.Workspace` accepts immutable
  `RibbonBackstageWorkspaceSettings`: independently resettable header fill/height/
  foreground, localized ReturnText/width, frame gutter, border/divider and inner
  inks/widths, radius, navigation StateChrome and extra separator inset. Null/Empty,
  Transparent, zero and explicit logical widths have documented meanings. Header
  alpha composites once over an opaque navigation-color base, including repeated
  native parent/child paint. Both gradient axes and exact hard stops survive.
  Validation is atomic before Theme/BuiltInTheme changes. Other styles ignore
  workspace roles. The shipped API baseline is unchanged; 39 additive Unshipped
  entries describe the record, accessor, appended identity and header hook.
- Virtual `DrawBackstageHeader` receives the measured File bounds and preserves
  the existing Back-entry, File-button, glyph and text hooks. Neutral and Office
  public painters share the frame implementation. Designer previews use the same
  geometry/paint. No sample helper, WPF hosting, diagnostic tab or layout-engine
  replacement implements the effect.
- This is an opaque workspace even on a material host. The host's material and
  stored navigation-opacity preference remain available when another style is
  selected. It reuses the existing Solid surface path and clocks. Established
  styles retain their existing material/page ownership. Native page HWNDs,
  parents, values, focus/validation and model identity survive navigation, close,
  reopen, resize and presentation changes. No application content is traversed
  for automatic theming and no document-protection/version feature is fabricated.

### Focused evidence and remaining limits

Final evidence is under `artifacts/office2007-backstage/`:

| Evidence | Observed scope/result |
| --- | --- |
| `focused-delivery.log`, `release-delivery.log` | **133/133 pass**, zero failures/skips, actual xUnit in-process execution; clean normal Release build. Seventeen new cases cover 96/120/144/192 DPI, both densities, frame/divider/gradient/crease/separator pixels, exact clips, four-corner alpha symmetry, wider/zero/null/reset/atomic contracts, frozen measurement inputs and tiny/short bounds. Native None/Mica cases retain host preference, opaque workspace, pages/editors/validation and theme switching; hidden-Back File return and existing renderer hooks pass. Existing affected layout/navigation/fill/appearance/page preference/designer/caption/File/preset cases also pass. |
| `public-delivery.log`, `public-delivery/comparison.json` | Independent public-only package consumer passes four Backstage scopes at actual 120 DPI: Classic/Simplified and Compact/Touch, equal native row/separator insets, front-order/visibility, native page/editor handle/parent/value retention, validation, return/reopen/narrow resize and both File shapes. Real caption-overlap Orb pointer return passes. **32 byte-exact retained-G6/current old-family paint pairs**, both public painters at four DPIs, match. No Showcase resources/helpers. |
| `showcase-delivery/result.log`, `designer-delivery/result.log` | Both existing sample paths pass the same four Backstage scopes at 120 DPI through the linked public-only verifier. The ordinary Designer Sample stays test-free; its dedicated package test host runs the checks. |
| `debug-delivery.log`, `showcase-build-delivery.log`, `host-build-delivery.log`, `designer-refresh-delivery.log` | Normal Debug runtime/designer server and sample/host builds are clean. Designer Sample refreshed in place with **`0.1.0-designer.local20261121T083542277Z`**; authored Forms/UserControls/resources remain. The unchanged net472 client output is reused. NuGet audit is disabled for focused local builds/restores; earlier refreshes retain the restricted-path NU1900 warning in their logs. |

Initial enum-wide tests assumed every non-Modern style had the established flat
families' page origin and 14-pixel footer gap. They now assert the appended framed
family's measured origin and four-pixel gap explicitly, preserving every old-family
expectation. Two new separator probes initially sampled the neighboring pixel for
even-height rows; they now use the existing one-device-pixel center rule. The first
independent fixture also omitted FileMenu.Visible; it now explicitly enables its
test declaration. No expected reference pixels or tolerances were relaxed.

Whole-Form DrawToBitmap images in `public-final/` are rejected visual evidence:
they print hidden sibling pages and reverse covered sibling order. The final
verifier prints each visible HWND once and composes visible children in native
Z order with their actual regions. It separately asserts front-order and content
visibility; no runtime activation/Z-order workaround was added. See
`REJECTED-NATIVE-PRINTS.txt`; preserve earlier attempts. Corrected native prints
were inspected for both samples. These are generated native paints, not desktop
captures. Earlier G6 split/caption baseline assertion limits remain in their own
records and are not represented as passing here.

Reproduction from `winforms/`, using normal configurations/default outputs:

```powershell
dotnet build artifacts/office2007-g2/direct-runner/DirectRunner.csproj -c Release -m:1 -p:NuGetAudit=false
dotnet artifacts/office2007-g2/direct-runner/bin/Release/net10.0-windows/DirectRunner.dll 'Office2007BackstageTests,BackstageLayoutInputTests,BackstageNavigationTests,BackstageAppearanceSettingsTests,BackstageFillSettingsTests,BackstagePagePreferenceTests,BackstageDesignPreviewTests,BackstageCaptionTests,BackstageFilePresentationTests,Office2007ThemeTests'
./eng/Test-Office2007Backstage.ps1 -BaselinePackageVersion '0.1.0-designer.local20261121T044540709Z' -Configuration Debug
./eng/Test-DesignerSample.ps1 -Configuration Debug -Check --verify-office2007-backstage
# Showcase accepts --verify-office2007-backstage <fresh-output-directory>.
```

VSTest's previously recorded socket restriction still uses the actual-xUnit
in-process runner. This is focused Backstage evidence, not the G7 composed ribbon
review or a full suite. Live appearance, actual VS, physical input/IME, mixed
monitors, continuous motion/material and background-load acceptance remain
separate. No WPF build, computer-use tool or desktop capture ran. Accepted
Backstage black-bar/performance/title-alignment and saved frame/File-route fixes
remain intact; their investigations were not reopened. **G7 remains paused** and
the separate Aero palette remains G8.

## Orb-only return and animated-paint correction - 2026-11-21

The reported opening corruption persisted until hover caused a repaint. This
follow-up removes the separate Back button from Office 2007's default layout and
fixes two independently reproduced retained-paint defects. Current user review
and the requested interaction change are recorded only in the themes ledger.

- The passive Orb used live screen-to-client conversion while the pane moved.
  Its local paint bounds therefore shifted inside a drawing Windows was retaining
  and moving as a unit. Exposed-strip repaint could leave mixed old/new positions.
  Header paint now compensates for the pane's movement from its settled origin;
  the same local Orb rectangle is supplied to every paint throughout travel.
  No full-window repaint on every frame or hover repair is introduced.
- An opaque Office2007 workspace on a material host used Solid drawing but the
  host's material preparation decision. It moved before the complete drawing was
  seeded, and even a nonanimated pane unnecessarily acquired a whole-surface
  material hold. One internal effective-mode decision now controls surface
  painting, context flags, initial holds, background preparation and motion.
  Office2007 follows the existing Solid preparation-before-move path while the
  host's material preference remains. Established families resolve exactly as
  before; their accepted preparation and repaint architecture is preserved.
- The preset and new family's null defaults hide BackButtonVisible. The Orb is
  the return control, retaining both its real caption segment and the surface
  segment's existing native pointer/validation handling. No separate Back label,
  hit target or KeyTip appears by default. Explicit BackButtonVisible=true remains
  an adjustable opt-in using the existing public API. Static designer previews
  still draw the File visual when the optional Back target is absent. Header,
  frame, divider, gold navigation and the equal four-pixel insets remain.

Evidence under `artifacts/office2007-backstage-paint/`:

| Evidence | Result and boundary |
| --- | --- |
| `coordinates-repro.log` | Before the coordinate/mode fixes, **16/21 pass and five fail**. None-host Subtle/Expressive probes observe Orb bounds changing from X=6 to X=97 during paint. Mica Subtle/Expressive first paint is at moved X=-148 instead of settled X=0; the nonanimated Mica invariant also detects an unnecessary root layer. `repro.log` retains the earlier three-failure mode-only probe. These are regression evidence, not passing results. |
| `focused-final.log`, `release-final.log` | **152/152 pass**, zero skips/failures; normal Release runtime/tests/runner build cleanly. Four new forced-animation cases at offscreen native hosts cover None/Mica and Subtle/Expressive, initial full-surface paint at the settled origin with alpha 1, stable Orb coordinates through partial-progress/exposed repaint, Orb-only default and retained native editor identity. Existing layout, navigation, appearance, fill, caption/File, motion/page-motion/repaint and designer-preview scopes pass. The Mica nonanimated case and optional Back/File/text/glyph hooks remain covered. |
| `public.log`, `public/comparison.json` | Independent public-only package consumer passes four Backstage scopes at actual 120 DPI, checks no visible duplicate Back button, validates through Orb return, retains native pages/editors and routing, and passes **32 byte-exact retained-G6/current old-family image pairs**. |
| `showcase/result.log`, `designer/result.log` | Both actual sample paths pass the four Backstage scopes with Orb return replacing the earlier Back-button click. Corrected native print inspected without the separate return label; this is not continuous-motion acceptance. |
| `debug.log`, `showcase-build.log`, `host-build.log`, `designer-refresh.log` | Normal Debug runtime/designer server and sample/test-host builds are clean. Designer Sample refreshed in place with **`0.1.0-designer.local20261121T085524906Z`**; authored Forms/UserControls/resources remain. Unchanged net472 client output reused. |

Reproduce using the previous package/sample commands and the affected xUnit
selection `Office2007BackstageTests,BackstageLayoutInputTests,BackstageNavigationTests,BackstageAppearanceSettingsTests,BackstageFillSettingsTests,BackstageCaptionTests,BackstageFilePresentationTests,BackstageMotionTests,BackstagePageMotionTests,BackstagePageRepaintTests,BackstageDesignPreviewTests`.
The actual-xUnit in-process runner remains in use. The new reproductions use
offscreen windows; final visual artifacts are native prints. No desktop recording,
Zoom investigation, WPF build, diagnostic tab or new public API was added by this
correction. Live animation/appearance, actual VS, physical input/IME and mixed
monitors remain separate. **G7 remains paused.**

## Backstage accent, Orb KeyTips and a joined standard caption - 2026-11-21

The header's fixed preset fill previously blocked the identity recipe. The preset
now leaves Workspace.HeaderBackground null: the existing effective File reference
and FileStrength rebase the full creased header onto Theme.Accent. At the default
accent, the reference blue and repeated .45 stops remain exact. An explicit
Workspace.HeaderBackground wins, then an explicit File background, then the
identity recipe/Accent fallback. Null resets and zero FileStrength retain their
documented authority. Navigation, gold feedback, Orb palette, Options primary
actions and the neutral caption remain independently authored.

Office2007 Backstage now includes the active File declaration in its existing
KeyTip resolver, before navigation/footer candidates. Alt/F10 paints the resolved
badge at the visible foot of the Orb, through the shared DrawKeyTip hook and
appearance metrics. The real File key is used (normally F); authored multi-letter
keys, prefix filtering and collisions remain unique. Completing the File KeyTip
returns through the existing native page validation/close path. F10, Tab and
pointer dismissal hide it with the navigation badges. No new model, Back button
or input-owning overlay is introduced. The saved editor value/handle/parent and
the previous paint-coordinate/opaque-preparation correction remain.

The colored standard caption previously restarted the same blue crease in the
Backstage header. When their effective vertical reference fills match, one
gradient now spans the caption and header. Intermediate stop offsets are mapped
exactly so the caption crease stays at its original pixel height; the final ramp
extends through the header. There is one crease and no second bright band, while
the title keeps its dark backing. Caption text, native caption buttons and their
hover outlines keep their measured positions. Caption tint alpha is preserved.

`Workspace.JoinColoredCaption` is additive: null/true enables this matching-fill
join; false opts out. Neutral captions, actual Aero frames, explicit Form caption
colors, explicit workspace header overrides, horizontal fills and top seams keep
their separate surfaces. A public additive
`RibbonPaintContext.BackstageHeaderBackgroundBounds` exposes the complete gradient
canvas, including the caption above the header. Header content still uses its
existing BackgroundBounds. Native partial paints and custom renderer hooks can
therefore share the full surface without changing hit targets. Four new getter/
init entries are in Unshipped; the shipped API baseline is unchanged. No Aero
preset or frame implementation is added by this follow-up.

Final focused evidence is under `artifacts/office2007-backstage-accent-keytips/`:

| Evidence | Result and scope |
| --- | --- |
| `focused-delivery.log`, `release-delivery.log` | **184/184 pass**, zero failures/skips; clean normal Release build. Fifteen new cases cover default/recolored creases, both shared painters at 96/120/144/192 DPI, reset/override/strength contracts, Orb-badge bounds, authored-prefix collisions and validation, joined-caption/header pixels, tint alpha and separate-surface authority. Existing affected Backstage, caption-outline, Options/caption, preset, accent and tip contracts pass. The actual-xUnit in-process runner is used; this is not a full-suite run. |
| `public-delivery.log`, `public-delivery/comparison.json` | Independent package-only consumer passes four Compact/Touch x Classic/Simplified native scopes at 120 DPI, accent/reset, joined-canvas delivery, F10 badge paint/toggle/validated return and previous native page/editor/reopen/resize/File-route contracts. **32 byte-exact** old-family image pairs match retained G6 `0.1.0-designer.local20261121T044540709Z`. No Showcase resources/helpers or private caption geometry are used. |
| `showcase-delivery/result.log`, `designer-delivery/result.log` | Both actual sample paths pass the same four native scopes each at 120 DPI; no failure log. The ordinary Designer Sample remains test-free; its separate package test host owns verification. |
| `debug-delivery.log`, `showcase-build-delivery.log`, `host-build-delivery.log`, `designer-refresh-delivery.log` | Clean normal Debug runtime/designer-server/sample/test-host builds. Designer Sample refreshed in place with **`0.1.0-designer.local20261121T143341530Z`**; authored Forms/UserControls/resources preserved. Unchanged net472 designer client output reused. |

The background native verifier uses public Control.PreProcessMessage for F10 and
the File key. Its unfocused host correctly fails the existing global shortcut
filter; the earlier `public-final/` and `public-focus/` attempts retain that fixture
limit. No window activation or focus-guard bypass was added. Office2007's native
control preprocessing now handles F10 as well as the existing focused global
input route. Earlier compile/filter attempts remain separate and are not counted
as executed passes; `focused-complete.log` predates that final local-route change.

Representative joined-header, Orb-badge and green-accent visible-HWND prints were
inspected. Surface-only prints deliberately crop the caption and upper Orb; exact
common-caption/header pixels are covered by the focused native/painter cases.
Physical Alt/F10, actual VS, input/IME, mixed-monitor,
desktop-material/continuous-motion and live appearance acceptance remain separate.
Accepted Backstage paint/performance work and retired diagnostic tabs remain.
**G7 remains paused.**
