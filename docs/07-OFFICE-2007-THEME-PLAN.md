# Office 2007 Theme — Implementation Plan

> **Status: S0-S9 COMPLETE.** The guaranteed opaque no-material window baseline was
> corrected against the restored Word reference on `codex/office-2007-window-frame` on 2026-08-12
> and the separately selectable Aero-inspired prototype now passes its automated gate plus
> active/inactive, normal/maximized live checks plus proportional 100/125/150/175/200% geometry and
> real 125%↔150% mixed-monitor caption, resize, maximize/work-area and Snap Layout verification. The
> preserved RibbonKit-original `Glass2007` modern Backstage shell and
> the separate opaque `Classic2007` document-dashboard concept pass their automated contracts and
> live same-process/restart switch checks. The other original deferral,
> the two-pane application menu, shipped on 2026-07-28. See `04-DESIGN-NOTES.md` §§3.38, 3.88-3.89.
> Original header follows.
>
> **Status:** S0 complete, S1 not started. Written 2026-07-27; revised the same day after the user
> supplied eleven reference screenshots and the S0 audit ran. This is the last remaining item that
> can still force a change to the token layer or the shared templates — everything else left before
> v1.0 is additive. Build it before the Phase 8 API freeze.

**Scope locked with the user (2026-07-27):**

| Decision | Choice |
|---|---|
| Colour schemes | **Blue only.** Silver and Black are post-v1 token clones once the geometry is proven. |
| Office orb | **New `ApplicationButtonShape` DP** (`Tab` \| `Orb`) with a template trigger — mirrors the existing `Backstage.Design` pattern. |
| File menu | **`Classic2007` backstage design.** S0 proved the real dropdown menu does not exist (§2); building it is a separate work item, not part of this theme. |

---

## 1. Why this is cheaper than it looks — and where it is not

**Cheap, because the theme system already absorbs most of 2007.** `ThemeManager.Apply` builds its
URI from the enum name:

```csharp
Source = new Uri($"pack://application:,,,/RibbonKit;component/Themes/Tokens.{theme}.xaml", …)
```

So adding an `Office2007` member to `RibbonTheme` plus a `Themes/Tokens.Office2007.xaml` file is the
entire wiring. There is even a placeholder comment sitting at `ThemeManager.cs:25`:

```csharp
// Office2007 arrives in a later Phase 6 batch (see docs/03-ROADMAP.md).
```

The csproj is SDK-style with implicit globbing, so the new token file needs no project edit.

**Expensive, because 2007 is the only generation whose *shape* differs.** Three things are not
colours:

1. The round orb overlapping the title bar — **measured at 36–37px diameter, centred ~60% above the
   tab strip** (§5). This is the one piece of new public API.
2. Rounded tab tops (~2–3px, top corners only).
3. A window frame and a solid title bar that 2007 got from Aero and Windows 11 will not provide (§6).

---

## 2. S0 audit — completed 2026-07-27

### 2.1 The application menu does not exist ❌

`README.md` line 56 lists *"Application button + application menu | ✅"*. That row is **wrong**. The
compiled public API (`RibbonKit.xml`, 86 public types) contains no `ApplicationMenu`, no
`RecentDocuments`, and no dropdown mode on any existing control. `Backstage` is a `TabControl`
rendered into an adorner as a full-window overlay; the only variation point is
`RibbonKit.RibbonBackstageDesign` (`Classic` / `Modern` / `Classic2010`).

What the README row actually describes is the application *button* plus the backstage. **Fix the
README row as part of S6.**

**Historical consequence, later superseded:** this audit proposed a fourth
`RibbonBackstageDesign` value named `Classic2007`. It was not added before v1 because it duplicated
`Classic2010`; post-v1, `Glass2007=3` became the first distinct 2007 Backstage interpretation. The
name is now used by the separately approved concept as the additive fifth value
`Classic2007=4`, preserving serialized values. The real
two-pane dropdown menu — left command column, right "Recent Documents" pane, bottom
`Word Options` / `Exit Word` bar, all visible in `application_menu.png` — is a **new control**, and
belongs in its own work item after the theme lands. It is a genuine feature gap, not a theme gap.

### 2.2 Recovering the old gel values from git is no longer needed ✅

The original plan proposed digging the 4-stop "hard crease" gradients out of the Office 2010
first-pass commit. **Superseded.** The reference screenshots yielded exact measured values (§4),
which are strictly better than the values that were once guessed at for a different theme.

### 2.3 What the screenshots settled — and one correction

The eleven images were sampled pixel-by-pixel. Everything in §4 is measured, not estimated.

> ⚠ **Correction to the original plan.** It claimed *"Pressed inverts (darker at top = recessed)"*,
> carried over from the §3.27 write-up of 2010's first pass. **That is wrong for 2007.** Measured
> from `pressedbutton.png`, the pressed state keeps exactly the same *light top → dark waist → light
> bottom* valley as hover; it simply shifts from gold to saturated orange and starts more saturated
> at the top. Both states are valleys. Do not build an inverted pressed gradient.

### 2.4 Contextual colours — gap closed 2026-07-27

The first contextual reference was an Aero shot whose glass composited the desktop wallpaper into
the "Table Tools" band, making the sampled values meaningless. A **non-Aero, DPI-scaled** pair was
supplied afterwards and the band is now measured — see §4.4. Nothing outstanding.

### 2.5 DPI scaling verified ✅

The DPI-scaled non-Aero shot is at **125%**. Comparing it against the 96 DPI shot:

- **Every colour is bit-identical** — `#3B5A82` edge, `#E3EBF6`/`#CADEF7` title bar, `#BFDBFF`
  strip, `#8DB2E3` body border, the `#E7EFF8`/`#DFECF7` body highlight lines. The palette is
  DPI-independent.
- **Geometry scales linearly.** The orb measures **46px at 125%** against 36–37px at 100% — a ratio
  of 1.26. So the orb is a fixed DIP size and WPF's own scaling handles the rest: no per-DPI orb
  metrics, and no reason to expect trouble in the S6 DPI matrix.
- **Correction (2026-08-12):** the earlier ~9px "window frame" measurement sampled the bevel/padding
  beside the ribbon/document client area. The title and status bar begin immediately inside the
  one-pixel native edge, so the wider blue stack is not a full-window opaque frame.

---

## 3. The honest cost estimate

**Office 2010 took seven feedback passes** (§3.27). Read that section before starting — it is the
closest precedent and it catalogues the traps 2007 will re-set. The single biggest accelerator was
getting reference images early, and **that has now happened**, which is why this plan carries a real
palette instead of a recipe. Budget **2–3 visual passes**, down from the 3–4 originally estimated.

At planning time the gradients still needed a Windows visual pass; that verification later completed.

---

## 4. Measured palette — Office 2007 Blue

All values sampled from the supplied screenshots. Gradients are vertical
(`StartPoint="0,0" EndPoint="0,1"`).

### 4.1 Chrome

| Token | Value |
|---|---|
| `Ribbon.Background` (tab strip band) | **flat `#BFDBFF`** — a `SolidColorBrush`, *not* a gradient. Confirmed identical in the Aero and non-Aero shots. |
| `Ribbon.Border` | `#8DB2E3` (ribbon body top edge) |
| `Ribbon.ContentBackground` (groups area) | `0.00 #DBE6F4` → `0.17 #C8D9ED` → `1.00 #E3F4FF` |
| `TitleBar.Background` | `0.00 #E4EBF6` → `0.14 #D5E5FA` → `0.28 #CADEF7` → `1.00 #E4EFFD` |
| `TitleBar.Foreground` | `#15428B` |
| Restored non-Aero edge (see §6) | One native/system edge; no Office-painted full-window band. The `#9BBBE3` strip is client-area padding beside the ribbon/document only. |
| Client area below the ribbon | `#A3C2EA` |

**The 2007 signature is the "valley".** Both the title bar and the ribbon body run *light → dip →
light*, with the dark waist high up (17–28% down). Nothing in RibbonKit's four existing themes does
this. Get the valley right and the theme reads as 2007 even before the glass lands.

The body also carries two 1px inner highlight lines directly under its top border (`#E7EFF8`, then
`#DFECF7`) and a bright `#C0F9FF` line directly above its bottom border (`#96AAC4`). These are
edge details, not gradient stops — reuse the `Control.InnerGlow` mechanism rather than inventing
stops for them.

### 4.2 Tabs

| Token | Value |
|---|---|
| `Tab.SelectedBackground` | `0.00 #F0F6FF` → `0.08 #F6FAFF` → `0.15 #EBF3FE` → `1.00 #DBE6F5` |
| `Tab.SelectedBorderBrush` | `#8DB2E3` top, `#94C5EB` sides |
| `Tab.SelectedForeground`, `TabStrip.Foreground` | `#15428B` |
| Unselected tab | `Transparent` — the `#BFDBFF` strip shows through |
| `TabCornerRadius` | `3,3,0,0` |

**The connect mechanism is exactly RibbonKit's existing one, and the numbers prove it.** Sampling a
vertical line through the selected tab shows the body's top border (`#8DB2E3`) simply *absent* under
that tab, and the tab's own bottom stop is `#DBE6F5` — which is the body's top gradient colour
`#DBE6F4` to within one bit of rounding. That is the §3.29 notch mechanism
(`Transforms.TabConnect` + `Tab.ConnectFootSelected` + `Tab.ConnectNotch`), already verified at
100–200% DPI. **Reuse it. Do not reinvent it.**

**Hovering the selected tab does not change its fill.** It adds a gold outer border (`#D7B87F`), a
pale-yellow inner rim (`#FFFFBD`), and a 2px warm cream band (`#F6EAC5`) just inside the top edge.
This maps cleanly onto the existing `Control.HoverBorder` + `Control.InnerGlow` +
`ControlHighlightBorderThickness` trio.

### 4.3 The glass — hover and pressed

Measured on an 82px-tall large button. The crease is two `GradientStop`s at the **same offset**.

**Hover** (`Control.HoverBackground`) — gold:

| Offset | Colour | |
|---|---|---|
| 0.00 | `#FFFFF7` | near-white cream |
| 0.38 | `#FFE798` | end of the pale upper half |
| 0.38 | `#FFD758` | ← **hard crease**, instant step to saturated gold |
| 0.48 | `#FFD350` | darkest point |
| 1.00 | `#FFE89F` | lighter gold at the bottom |

- `Control.HoverBorder` `#C1A877` (sides), `#DDCF9B` (top), `#D3CFBC` (bottom)
- `Control.InnerGlow` `#FFF2C7`
- bottom highlight line `#FFFAB3`

**Pressed** (`Control.PressedBackground`) — orange, same valley shape:

| Offset | Colour | |
|---|---|---|
| 0.00 | `#FEB96C` | |
| 0.40 | `#FDA461` | |
| 0.40 | `#FC8F3D` | ← **hard crease** |
| 0.63 | `#FC973D` | darkest band |
| 1.00 | `#FEBE63` | |

- border `#B6A892`, inner rim `#FDAD10`, bottom line `#FDAD11`

Both states also carry a *subtle horizontal* sheen (edges ~`#FFD343`, centre ~`#FFD25D` on hover).
Ignore it — a vertical-only approximation is within a few RGB units and keeps every token a plain
`LinearGradientBrush`.

### 4.4 Contextual tab groups

Measured from the non-Aero Table Tools shot. The band is **not** a flat colour — it is a vertical
gradient that starts as the ordinary title bar and *fades into* the contextual hue:

| Element | Value |
|---|---|
| Band, top | matches `TitleBar.Background` exactly (`#E3EBF6` → `#D5E5F7`) |
| Band, ~40% down | `#CEDFED` — first visible divergence from the plain blue |
| Band, ~70% down | `#E7EBD4` → `#ECECBC` |
| Band, bottom | `#EEE583` |
| Band bottom edge | **`#FDE41B`** — a 1px saturated yellow line, the strongest signal of the group |
| Band side borders | `#C6D6E5`, 1px |
| Selected contextual tab | `0.00 #FFF6A9` → `1.00 #FFFAD5` (pale yellow, warm) |

Two implications for RibbonKit:

1. **The band blends into the title bar at its top; it does not sit on it as a block.** §3.4 records
   that contextual tabs are done as custom colouring with no marker line. The existing
   `Tab.ContextualBackground` / `ContextualHoverBackground` tokens are `SolidColorBrush`es in every
   theme; 2007 needs a gradient *and* a bottom edge line. The gradient is free (any `Brush` works at
   a token key — §2.1); **the edge line is not.** Decide in S4 whether to add a `Tab.ContextualEdge`
   key, and if so add it to all five theme files.
2. `#FDE41B` is the Table Tools *yellow*. Real 2007 has a per-group palette (purple for Picture
   Tools, and so on) and RibbonKit already binds contextual colouring to app state — so ship yellow
   as the default and keep the hue consumer-settable rather than baking it into the tokens alone.

### 4.5 Groups and the body border — ⚠ REVISED after the S1 visual check

The S1 build was reviewed on Windows on 2026-07-27 and the user flagged two structural gaps. Both
are real, and measuring them **corrected an earlier misreading in this document**.

#### ❌ Correction: 2007 has no group separator at all

An earlier revision recorded "`Group.Separator` `#9EBED9` with a `#ECF4FA` light companion (a
classic etched separator)". **That was wrong.** Sampling horizontally across a group boundary shows
this repeating run:

```
… #CCDCEE  #A8BFD4  #E4ECF5  #CCDCEE  #A8BFD4  #E4ECF5  #CCDCEE …
   body     border   inner     gap      border   inner     body
                     highlight                   highlight
```

That is **two adjacent group boxes**, not one separator. Every 2007 group is a bordered box with its
own inner highlight; what reads as a divider is simply where two boxes meet. So in 2007
`Group.Separator` should be **`Transparent`** and the boxes carry the visual weight.

#### Measured group box

| Element | Value |
|---|---|
| Group border | `#A8BFD4` (`#9CBEDA` alongside the label strip) |
| Inner highlight, 1px inside the border | `#E4ECF5` (`#EDF5FB` alongside the label) |
| Group body fill | `0.00 #DEE8F5` → `0.16 #C7D8ED` → `1.00 #D9E8F6` — the valley again |
| **Group label strip** | flat **`#C1D9F1`**, a clear step darker than the body above it |
| Label strip height | ~19px of the ~107px box |
| Corner radius | ~3px |

The label strip is the part the S1 build misses most visibly: right now the label sits on the same
surface as the group content, where 2007 gives it its own shaded band at the foot of the box.

#### The ribbon body border does loop

Confirmed: the body's left edge carries the same `#8DB2E3` border as its top, with a `#CEF2FA` inner
highlight just inside it, and the bottom-left corner rounds over ~3px. `ContentBorderThickness` is
already `1`, so the loop should draw — what is missing is **`ContentCornerRadius`, currently `0`;
set it to `3`**.

On the connect-foot worry: the radius only affects the four far corners, while the notch paints over
the top border wherever the selected tab sits. Those do not overlap unless the first tab is flush
against the body's top-left corner — worth one look during S4, but it is not the collision it
sounds like.

#### ✅ BUILT 2026-07-27 — the predicted template change, now landed

This was the moment §1 warned about: 2007 is the generation that forces a shared-template change.
RibbonKit had exactly **one** group token (`Group.Separator`). Delivered:

**Seven new keys** (one more than the six first scoped — see the note below), in all five theme
files, taking each from 95 to **102 keys**:

| Key | 2007 | Every other theme |
|---|---|---|
| `Brushes.Group.Background` | gradient `#DEE8F5` → `#C7D8ED` @0.16 → `#D9E8F6` | `Transparent` |
| `Brushes.Group.Border` | `#A8BFD4` | `Transparent` |
| `Brushes.Group.InnerHighlight` | `#E4ECF5` | `Transparent` |
| `Brushes.Group.LabelBackground` | `#C1D9F1` | `Transparent` |
| `Metrics.GroupBorderThickness` | `1` | `0` |
| `Metrics.GroupCornerRadius` | `3` | `0` |
| `Metrics.GroupLabelCornerRadius` | `0,0,2,2` | `0` |

**Why seven, not six.** The label band sits at the foot of a box with a 3px radius, so a
square-cornered band pokes past the rounded corners by about a pixel. `GroupLabelCornerRadius`
rounds only its bottom corners, one pixel tighter than the box so it tucks inside the rim. Cheaper
than clipping.

**Template** (`Themes/Controls.Groups.xaml`, the `RibbonGroup` `ControlTemplate`): the content grid
is now wrapped in a `GroupBox` `Border` carrying background, border, thickness and radius, then a
nested rim `Border` bound to `Group.InnerHighlight` — the same construction the button glass already
uses via `Control.InnerGlow` — and the label row is wrapped in a `Border` bound to
`Group.LabelBackground`. The `Margin="4,2,4,0"` moved from the grid onto the outer border so the box
takes the inset.

**Also changed for 2007 only:** `Group.Separator` → `Transparent` (per the correction above), and
`ContentCornerRadius` `0` → `3` for the looped body border.

**Safe because:** `PART_NormalHost` is a `Decorator` and `RibbonGroup` re-homes `_normalHost.Child`
as an untyped `UIElement` (`RibbonGroup.cs:430`), never casting to `Grid`, so the wrapper is
invisible to the flyout move. `Controls.Groups.xaml` contains **no** `StaticResource` references at
all, so the §3.37 sibling-scope rule cannot be violated by this edit.

**Verified:** all five token files at 102/102 identical keys; every file XML-valid, CRLF-clean and
free of `--` inside comments; every `DynamicResource` the template names is defined in the theme
files. Still needs `dotnet test` and a Windows look.

**Watch on first run:** the group box now also renders inside the collapsed-group flyout, since the
flyout receives the same subtree. That is arguably correct but was not designed for — check it.

---

## 5. The orb — the one piece of new API

### 5.1 What ships

```csharp
public enum RibbonApplicationButtonShape
{
    /// <summary>Rectangular File tab (2010 / 2013 / 2019 / 2024). Default.</summary>
    Tab,
    /// <summary>Round Office 2007 orb, overlapping the title bar.</summary>
    Orb,
}
```

plus `Ribbon.ApplicationButtonShape` as a dependency property defaulting to `Tab`.

### 5.2 Measured geometry

From the non-Aero screenshot, which has no glass contamination:

- **Diameter 36–37px** at 96 DPI (spans x=9→44, y=10→46).
- The title bar occupies y=3→31 and the tab strip y=33→55. The orb's centre lands at **y≈28** — just
  above the boundary. It overhangs **~22px up into the title bar** and ~14px down into the strip.
- Resting ring `#D8DEE8` with a `#8B97A8` outer edge.
- Hover ring **`#FEEA37`** — a bright yellow that is unmistakably the 2007 hover, plus a warm glow
  inside it.

### 5.3 ⚠ Three traps specific to this element

1. **The 22px upward overhang is the whole problem, and it is now quantified.** The orb *cannot* be
   laid out inside the tab-strip row at natural size. §3.27 already lost a round to
   `RibbonScrollContentHost` clipping exactly this kind of overhang. **Prototype the overhang in S3
   before styling anything** — a plain magenta ellipse with a negative top margin is enough to prove
   it. Fallback if it clips: draw the orb in the `RibbonWindow` title-bar layer instead of the tab
   strip.

2. **Do not wrap it in a new panel.** The File button sits in a `StackPanel` with
   `VerticalAlignment="Stretch"`, and the template carries an explicit comment explaining that
   `Stretch` — not `Center` — is load-bearing, because 2013 draws the File button as a solid block
   that must reach the bottom of the row. There is a recorded incident
   (`template_wrapper_alignment`) of a wrapper panel silently stealing an alignment a template
   element relied on. Add the orb **as a sibling inside the existing `Grid`** at
   `Controls.RibbonChrome.xaml:186`, not by re-parenting.

3. **Register it for the designer.** `RibbonKit.Design/Metadata.cs` and `PropertyMetadata.cs` drive
   the design-time experience and the Ribbon Editor (§3.22, §3.23). A new public DP the designer
   does not know about shows up as an unhandled property. Cheap now, annoying later.

### 5.4 Public-surface note

Phase 8 freezes the API. This theme adds one public enum (`RibbonApplicationButtonShape`), one
member on `RibbonTheme`, one member on `RibbonBackstageDesign`, and one DP. Name them as if they are
permanent — because after Phase 8 they are.

---

## 6. Windows 11 has no Aero — define a baseline and an optional enhancement

Ten of the eleven original references show Aero glass, while the non-Aero reference exposes the
colours Office drew when the operating system supplied no glass. The implementation therefore has
two deliberately separate levels. The first is the guaranteed historical baseline; the second is an
optional Aero-inspired presentation that may use a supported system backdrop but must never be
required for a correct Office 2007 window.

### 6.1 Guaranteed opaque no-material baseline

- Use the §4.1 `TitleBar.Background` valley for the solid title bar.
- Keep `PART_WindowRoot` flush with the physical-LTR host and let supported native `WindowChrome`/DWM
  supply its one-pixel edge. Do not paint a duplicate Office outline or a full-window blue band.
- Preserve the ribbon/document client bevel and padding in their existing templates. Those local
  surfaces must not inset the title bar or status bar.
- Use a quieter inactive title treatment. Keep its metric in the shared token contract with a neutral
  value in every other theme; do not fork the `RibbonWindow` template for Office 2007.

`RibbonWindow` already uses `WindowChrome` with themed caption buttons, so the frame is reachable,
but the opaque reference does not justify a new app-painted outer inset. The existing measured-margin
maximize fix (§2.3) remains the only geometry around `PART_WindowRoot` and must stay unchanged.

### 6.2 Optional Aero-inspired frame

The optional treatment layers Office 2007-authored visuals over the same geometry:

- translucent blue tint, bright inner edge, restrained grain and a static or position-derived
  reflection highlight;
- active/inactive tint and title/caption contrast based on the supplied references;
- the existing `RibbonBackdrop.Acrylic` path underneath transparent frame/title regions where the
  operating system supports it, with the document/body surfaces remaining opaque; and
- a visually coherent opaque result when Acrylic activation fails, transparency is disabled, or the
  host chooses not to use a system backdrop.

This remains an **app-owned appearance preference**, separate from ribbon customization Import,
Export and Reset. Choosing `RibbonTheme.Office2007` must not silently turn on a DWM material. Aero is
also a composed window appearance, not a new native backdrop kind. The prototype therefore adds
`RibbonWindow.FrameAppearance=Office2007Aero`, not `RibbonBackdrop.Aero`; the existing Acrylic
preference remains independently selectable.

Do not use `AllowsTransparency=True`, capture and CPU-blur the desktop, inject into DWM, hook private
composition functions, or depend on downloaded system symbols. `DWMBlurGlass` is a useful visual
reference for glass tint, reflection, caption glow and inactive-window treatment, but its global
injection architecture and source are not a RibbonKit dependency or implementation starting point.

The tab strip colour `#BFDBFF` is identical in both the Aero and non-Aero Office references. It stays
opaque in the implemented prototype; the optional material belongs to the restored frame/title
composition rather than bleeding through the ribbon.

### 6.3 Reference captures requested

The user can provide Task Manager and Paint screenshots with the DWMBlurGlass effect injected; both
applications let the reference show a DWM-painted frame without RibbonKit's own chrome obscuring it.
Treat these as appearance references, not as proof that the injected mechanism is safe to ship.
Capture, when practical:

1. active and inactive windows at 100% scaling;
2. the full normal window plus close crops of the top corners/caption buttons and a side/bottom edge;
3. a maximized window to show which bands disappear or change; and
4. the DWMBlurGlass blur, tint, opacity and reflection settings used for the captures.

The supplied captures cover normal/maximized Task Manager and Paint, the top of Aero Word, and the
DWMBlurGlass settings. They establish restored frame continuity, maximized frame removal and the
active/inactive delta; the injected implementation remains a visual reference only. Do not sample
wallpaper-contaminated pixels as opaque token colours.

### 6.4 RibbonKit-original Glass2007 Backstage

Post-v1, the user approved a distinct RibbonKit-authored `Glass2007` Backstage after reviewing the
Modern translucent rail over the new Aero-inspired frame. It is an optional full-window File surface,
not a historical-emulation claim: the two-pane orb application menu remains the Office 2007 default.
Theme selection does not select this design, enable translucency or request Acrylic.

The first stage reuses the shared Backstage template and existing generation-aware tokens. Opaque
mode paints the 2007 rail gradient; translucent mode exposes Acrylic only through the rail
beneath the same tint used by the Aero title and outer frame. The page sheet stays opaque. Navigation uses inset
two-DIP tiles, gold hover, hard-crease glass selection and an orb-derived back button. Live review
removed the authored rail/content strokes and the remaining top highlight because they read as
nested borders and produced a stray selected-row line. The larger three-sided outline shown in the
follow-up capture belonged to the Aero frame bevel and title-bottom highlight; both now collapse only
while Backstage hides the title content. Hover/selected tile outlines remain crisp, while their fills and the back-button disc
are 88% opaque so a small amount of rail color passes through. Stop for a live shell review before
redesigning template/recent-document page content.

The translucent rail must bind `RibbonWindow.AeroFrameTint` and its intensity. In inactive Backstage,
the title and rail retain `InactiveTitleBarOpacity`, while the outer frame uses the dedicated
`AeroInactiveBackstageFrameOpacity`. Its Office 2007 value is `1`: DWM supplies different suspended-
Acrylic fallback colors to the outermost frame and transparent client rail, and full frame tint
compensates them to the same sampled boundary color at 125%. Outside Backstage, inactive Aero keeps
the shared title/frame fallback base opaque, quiets title content separately, and applies
`AeroInactiveOverlayOpacity` to both authored tint layers. Using the separate
classic-nav brush or attenuating only one surface creates the same split. Relative
reflection/grain brushes also restart when separately mapped to the short
title fill and full-height frame border. They therefore appear only for an active normal window with
accepted Acrylic, and are suppressed for Backstage, inactive and opaque-fallback compositions.
Explicit opaque mode under `Office2007Aero` restores the local three-sided white bevel around its
gradient rail. The title-bottom highlight renders as the final non-hit-test title overlay, above
all caption-button backgrounds, and collapses with the frame bevel in Backstage.

### 6.5 Separate Classic2007 Backstage concept

The later `Classic2007` option is a second RibbonKit-authored thought experiment based on a supplied
concept image: an opaque skeuomorphic blue shell, larger command navigation, gold glass states and a
compact document dashboard. Nav and content share the same field. One continuous dark-blue perimeter,
a one-pixel divider and a continuous white inner highlight avoid compounded seams; a dedicated
full-frame shadow adds depth inside the eight-DIP gutter. The dashboard has its own balanced render
gutter so card shadows are not clipped by its `ScrollViewer`.

Classic reparents the real ribbon application button into the Backstage adorner rather than creating
a second orb. Only its four-color glyph rotates during open/close; sphere, rim and shadow stay fixed.
Owner bindings and design-change reconciliation guarantee that `2007 Modern`/`2007 Classic` switches
restore or host that same button correctly, including after restart. A restored `Office2007Aero`
host adds a local four-sided white shell/frame join bevel. It does not replace `Glass2007`; the
Showcase calls the preserved translucent design `2007 Modern` and this opaque design `2007 Classic`.
Both remain explicit choices, and the real two-pane application menu remains the Office 2007 theme
default.

---

## 7. Working rules that apply to every step

These are recorded failures, not general advice:

- **Never edit a XAML file through Python text mode.** The repo is CRLF; text mode silently rewrites
  to LF and breaks the diff safety check (`tooling_line_endings`). Binary mode only.
- **`StaticResource` does not cross sibling merged dictionaries.** If any new resource lands in a
  `Controls.*.xaml` part, the part that consumes it must merge it *locally* (§3.37). Runtime-only
  failure — it compiles fine and throws on load.
- **Run `dotnet test` after touching any theme dictionary.** `ThemeDictionaryScopeTests.cs` enforces
  the split rules and is the only thing between a mis-scoped resource and a crash on a machine you
  are not sitting at.
- **Token parity check** — after changing a base token dictionary, diff its key set against
  `Tokens.Office2024.xaml`. The five base dictionaries currently carry exactly **209 keys with
  identical sets** (verified with the Aero prototype). A missing key means a control
  silently renders with whatever the previous theme left behind:

  ```python
  import re, glob
  sets = {f: set(re.findall(r'x:Key="([^"]+)"', open(f, encoding='utf-8-sig').read()))
          for f in glob.glob('src/RibbonKit/Themes/Tokens.*.xaml')}
  base = sets['src/RibbonKit/Themes/Tokens.Office2024.xaml']
  for f, k in sets.items():
      print(f, len(k), 'missing:', sorted(base - k), 'extra:', sorted(k - base))
  ```

- **Build and test in the current Windows workspace.** Visual claims still need a Windows look before
  they are recorded as verified.

---

## 8. Accent handling

`ThemeManager.ApplyAccentOverrides` needs an `Office2007` case. Both 2010 decisions carry over:

- **Skip the checked/toggle highlight for 2007.** Line 248 currently reads
  `if (theme != RibbonTheme.Office2010)`. 2007's hot state is gold/orange regardless of the colour
  scheme, exactly as 2010's is — a custom accent recolours chrome, not the highlight. Extend the
  guard to cover both.
- **Re-derive accented surfaces as gradients, not flat solids.** `Gel(Color)`
  (`ThemeManager.cs:293`) produces 2010's smooth 3-stop profile. 2007 needs a hard-crease sibling —
  **add `Glass(Color)` alongside it rather than changing `Gel`**, which 2010 depends on.
- **Every accented token must be listed in `AccentOverrideKeys`** (`ThemeManager.cs:74–80`) or it
  leaks across a theme switch.

---

## 9. Staged milestones

Each stage ends with something visible on Windows. Do not batch them — 2010's feedback loop was long
partly because too much changed between looks.

| # | Stage | Contents | Exit criteria |
|---|---|---|---|
| ~~S0~~ | ~~Audit~~ | ✅ **Done 2026-07-27.** Application menu confirmed absent → `Classic2007`. Full palette measured incl. the contextual band. DPI scaling verified at 125%. Git recovery dropped as unnecessary. | — |
| ~~S1~~ | ~~Palette~~ | ✅ **Done 2026-07-27**, reviewed on Windows. `Office2007` enum member, `Tokens.Office2007.xaml` (95/95 parity), showcase button. The §4.3 glass shipped early with it, so S2 is refinement rather than construction. Review found the two §4.5 gaps. | — |
| ~~S2~~ | ~~Glass~~ | ✅ **Shipped with S1** — the measured crease values were already in hand, so splitting them out would have meant reviewing a flat 2007 that told us nothing. S2 became refinement. | — |
| ~~S3~~ | ~~Orb~~ | ✅ **Done.** Overhang works. Needed `WindowChrome.IsHitTestVisibleInChrome` (only the bottom half was clickable), a backstage-hide trigger, size 37→46, and a shadow instead of a border. | — |
| ~~S4~~ | ~~Geometry~~ | ✅ Group boxes + `ContentCornerRadius` 0→3 **built and verified 2026-07-27** (§4.5). The original domed-tab idea was rejected in favor of the measured flat 2007 tab strip (§3.38). The §6 window frame was split out as the plan's one remaining deliberate deferral so it can be implemented and maximize-tested independently. | — |
| ~~S5~~ | ~~Accent + backstage~~ | ✅ **Done.** `Glass()` helper beside `Gel()`, `Office2007` case, hot-state guard widened. `Classic2007` was deliberately not added before v1 because it duplicated `Classic2010`; the later post-v1 `Glass2007` work in S8 is a materially different translucent/opaque RibbonKit-original design. | — |
| ~~S6~~ | ~~Wiring + verify~~ | ✅ **Done.** Showcase button + `ApplyTheme` helper, README application-menu row corrected and the 2007 row marked ✅, design notes §3.38, roadmap and features updated. DPI matrix verified clean at 100/125/150/175/200%. | — |
| ~~S7~~ | ~~Post-v1 window frame~~ | ✅ **Complete 2026-08-13.** §6.1 has no app-painted full-window band in the default opaque baseline. §6.2 is independently selectable from Acrylic, falls back opaquely, distinguishes active/inactive state and collapses restored side/bottom bands when maximized. Tint brush/intensity remain host-controlled. The final gate covers proportional 100/125/150/175/200% geometry and real 125%↔150% mixed-monitor movement, all resize edges, caption commands, maximize/work-area compensation and Windows 11 Snap Layouts. | ✅ Passed. Actual Acrylic was approved live; deterministic fallback and geometry remain automated. |
| ~~S8~~ | ~~Post-v1 Glass2007 Backstage~~ | ✅ **Complete 2026-08-13.** Additive `Glass2007` design, shared-template translucent rail continuous with active/inactive Aero title material, conditional opaque-rail bevel, crisp subtly translucent states, gold hover, hard-crease selection, dark-palette contrast, opaque content sheet, Showcase selector and appearance persistence. | Automated template/API/persistence contracts and live active/inactive Acrylic/fallback review pass. |
| ~~S9~~ | ~~Post-v1 Classic2007 Backstage~~ | ✅ **Complete 2026-08-13.** Separate additive `Classic2007` design with one continuous opaque blue field, continuous frame/highlight/shadow, clean pane divider, the single real orb with glyph-only rotation, larger navigation, gold states and a compact Showcase-only document dashboard with unclipped shadows. `Glass2007=3` remains unchanged; `Classic2007=4` persists independently. | Shared-template/API/persistence/lifecycle contracts, live shell review and same-process/restart Modern/Classic switching pass. |

---

## 10. Risk register

| Risk | Likelihood | Mitigation |
|---|---|---|
| Orb overhang clipped by the tab scroll host | **High** — 22px of overhang, and this class of failure already happened in §3.27 | Prototype the overhang in S3 before styling. Fallback: `RibbonWindow` title-bar layer. |
| The Aero material frame perturbs the measured-margin maximize fix (§2.3) | Mitigated; S7 passed | The default root remains flush and the separate Aero frame collapses when maximized. Proportional 100/125/150/175/200% checks and real 125%↔150% maximized mixed-monitor verification preserve the measured work-area inset. |
| WPF screen coordinates retain the previous monitor transform after a DPI transition | Found and mitigated in S7 | Derive caption hit bounds from template-local DIPs, the native `ClientToScreen` origin and the current window `DpiScale`; focused tests cover all required scales, negative coordinates and mirrored corners, and real mixed-monitor Snap Layout verification confirms alignment. |
| The Aero reference tempts the implementation toward private DWM hooks or `AllowsTransparency` | Mitigated in prototype | The effect is app-local WPF token/overlay visuals plus the existing supported Acrylic path only. No injection, private composition API, desktop capture or CPU blur. |
| DWM material makes snapshot output machine-dependent | High for Acrylic | Approve the opaque frame and deterministic overlay in snapshots; verify the actual Acrylic composition live with reference screenshots rather than creating unstable pixel baselines. |
| Frame appearance leaks into ribbon customization persistence | Mitigated in prototype | Schema-3 app appearance preferences own backdrop/frame/tint state and migrate schemas 1–2; customization Import/Export/Reset remains structural. |
| A RibbonKit-original Backstage is mistaken for historical Office 2007 UI | Low | Name and document it as `Glass2007`, keep it explicitly selected, and retain the two-pane orb application menu as the Office 2007 default. |
| Glass2007 becomes a second Backstage template fork | Mitigated | Both completed 2007 Backstage designs remain additive trigger branches in the one shared template and consume the existing `DynamicResource` token contract. |
| Classic2007 is mistaken for historical Office 2007 behavior or replaces Glass2007 | Mitigated | Both additive enum values and Showcase selectors remain, labeled `2007 Classic` and `2007 Modern`; lifecycle tests prove switching preserves each surface and the same real orb. The supplied image remains documented as a concept rather than historical evidence. |
| The contextual band wants a gradient + edge line, but the tokens are solid brushes | Medium | Gradients drop in free at a token key; the `#FDE41B` edge line may need a new key (§4.4). Decide in S4; a new key goes into all five files. |
| **The group-box template change is the largest single edit in this theme** | **Confirmed, not a risk any more** | Six new keys across five theme files plus the `RibbonGroup` template (§4.5). Do it as its own commit, run `dotnet test` immediately after, and keep the flat themes zeroed. |
| A new resource breaks the split-dictionary scope rule | Medium | `dotnet test` after every dictionary edit. |
| Design-time preview degrades | Medium | The split is still ⚗ experimental (§3.37) with "designer gets slower/flakier" as an explicit exit criterion. 2007 is its first real load test — **if the designer degrades, that is a signal about the split, not about 2007.** Note it rather than working around it. |
| Adding public API right before the freeze | Low but permanent | Name the enums and DP as final in S3. |

---

## 11. What this unblocks

With the main 2007 theme done, all five generations ship and the token layer is proven against the
widest visual range it will ever face. The remaining v1 blockers and later additive work are:

1. ~~**Phase 7 unit tests**~~ — **completed 2026-08-01**; `RibbonMergeModalTests` covers the
   automated invariants from `06-MERGE-AND-MODAL-PLAN.md` §7.
2. ~~**The 2007 window frame**~~ — **completed in S7** with the corrected opaque baseline, optional
   Aero-inspired enhancement and independent maximize/Snap/mixed-DPI gate.
3. **Dark mode, RTL, localization and visual-regression snapshots** — the rest of Phase 6.
4. **Phase 8** — API freeze, repository metadata, README screenshots, and release packaging.
5. **MDI M1–M3 (post-v1)** — cascade/tile/Ctrl+Tab, MVVM demo, tabbed mode + persistence.
6. **Application-menu enhancements (additive)** — arrow-key navigation and a scrolling pane (§3.46).
