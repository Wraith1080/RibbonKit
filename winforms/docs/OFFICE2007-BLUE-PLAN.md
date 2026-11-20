# Native Office 2007 Blue appearance contract

**2026-11-21 - G2/G3 shared foundations implemented.**
G1 remains the appearance reference. G2's additive APIs and focused evidence are
[recorded below](#g2-shared-paint-foundations--2026-11-20). No Office 2007
runtime preset or sample tile is delivered by G2.
Current family acceptance belongs in the
[themes ledger](THEMES-PLAN.md#current-status-and-review-ledger).
The [G3 composition record](#g3-application-button-and-menu-composition--2026-11-21) follows G2. The normal Blue preset/ribbon is next in G4; no foundation visual-review stop is required.

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
policy and needs an explicit compact panel treatment, not a second layout engine.

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
retain its origin while panes change. The WPF same-window overlay/reparenting
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
| Fills / state chrome | Immutable multi-stop fills, opacity mapping, full-surface coordinates, rims and independent state roles; G2 adds exact repeated-offset stops through an opt-in factory. | The existing constructor still requires strictly increasing offsets. The later preset must opt into hard transitions explicitly. |
| Ribbon / tabs / QAT | Connected filled tabs, body joins, reduction, density/DPI snapshots, public renderer hooks and three QAT placements; G2 adds optional framed group panels and label bands. | Compose the 2007 roles in G4. Existing themes retain their separators and captions. |
| File | Shared File command/routes and rectangular paint; G3 adds theme-owned shape, explicit override/Reset, sphere/glyph/focus hooks, overlap reservations and circular input. | Supply the family palette/default in G4; preserve an explicit rectangular override and saved File route. |
| Application Menu | Native two-pane model, state/frame roles, footer/scrolling/nested popups/custom content; G3 adds native Orb/caption cutouts, measured origin and top-band content clearance. | Apply the 2007 band/frame palette in G5. Native font/DPI clearances are not a claim of WPF pixel parity. |
| Other surfaces | Shared menu/gallery/tip/editor and Options appearance pipelines; real native editors and dialog drafts. | Compose and review their own 2007 roles, especially creased fills and primary/secondary button feedback. |
| Aero | Theme-owned palette/edge width and native frame/material owner; restored/maximized transitions. | A separately reviewed 2007 palette using that owner. Reflection/grain and a different material/tab split remain deferred. |
| Consumers / designer | Permanent View galleries in both samples, persistence and independent package-only verification. | Add the preset and append its built-in identity without renumbering existing values; update theme serialization, inert designer preview and both galleries. No diagnostic tabs. |

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
| G4 - normal Blue preset and ribbon | Add family identity, opaque palette, connected tabs, groups, rectangular override and Orb states; expose both permanent View galleries. Review the normal ribbon before downstream surface work. |
| G5 - Application Menu palette | Apply frame/top/footer bands and gold/split feedback on the G3 composition. Review opening, pane switching, nested menus, resizing and native content. |
| G6 - Options and companion surfaces | Apply Options/customization, editor, gallery, menu and tip roles through existing pipelines. Check drafts, validation, Apply/OK/Cancel/Reset and ownership. |
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
