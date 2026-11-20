# Native Office 2007 Blue appearance contract

**2026-11-20 - G1 proposed for review; documentation only.** No Office 2007
runtime preset, public API, sample tile or package is delivered by this slice.
Current family acceptance belongs in the
[themes ledger](THEMES-PLAN.md#current-status-and-review-ledger).
Review this contract and slice order before runtime changes.

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
| Fills / state chrome | Immutable multi-stop fills, opacity mapping, full-surface coordinates, rims and independent state roles. | Exact hard transitions: `RibbonGradientStops` currently requires strictly increasing offsets. Design an additive representation and shared painter/sampler support; preserve old constructor validation and avoid epsilon-stop approximations. |
| Ribbon / tabs / QAT | Connected filled tabs, body joins, reduction, density/DPI snapshots, public renderer hooks and three QAT placements. | Opt-in framed group panels and label bands. Current `DrawGroupFrame` paints a separator, and `RibbonBodyAppearanceSettings` has no group-panel fill contract. |
| File | Shared File command, routes, state fills, accessibility and rectangular geometry. | Theme-owned shape with explicit override/Reset, sphere/glyph painting, overlap metrics, QAT/title reservation and matching native hit testing. |
| Application Menu | Native two-pane model, rich state/frame roles, footer, scrolling, nested popups and native custom content. | Orb-above-menu composition/anchor and any missing independent band metrics, then the 2007 palette. Recoloring the existing popup alone cannot establish overlap parity. |
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
| G1 - this document | Review palette, Orb/menu behavior, support gaps and sequence. **Stop before runtime changes.** |
| G2 - shared paint foundations | Exact hard transitions and opt-in group-panel/label-band decoration. Focused painter/sampler, clip, DPI and old-default compatibility checks; no layout-engine replacement. |
| G3 - application button and menu composition | Shared shape/override, Orb geometry/glyph, caption hit testing, QAT reservation and Orb/menu layering/anchor. Prove one command owner, keyboard/UIA and native-content retention in an independent consumer before a family tile. |
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

G1 validation is source/reference review, changed-link inspection and
`git diff --check`. No build, runtime test, app launch or desktop capture is
needed for this documentation-only proposal. Await the user's design review
before G2 or any other runtime implementation.
