# Library history: Office 2007 and popup motion

> Archived implementation evidence. Dates, test counts and pending work describe
> their original checkpoints; later records may supersede them.

[History directory](../01-library.md) · [Current status](../../../04-DESIGN-NOTES.md#5-current-state--next-steps) · [Working agreement](../../../AGENTS.md).

### 3.38 Office 2007 theme — the last generation, and the one that changed the templates — 2026-07-27

Office 2007's palette was measured from user-supplied reference screenshots.
Its identifying paint is a light/dark/light valley with a hard gradient crease;
pressed shifts gold to orange while keeping that profile. Office 2010's smooth
Gel and 2007's creased Glass accent helpers therefore remain separate. The tab
strip is deliberately flat, and group boxes replace separators.

Shared-template lessons:

- Put group-box chrome outside PART_NormalHost so only content is rehomed into
  a collapsed flyout. Put GroupPadding on ItemsPresenter so the label spans the box.
- Keep collapsed-button highlight thickness reserved; swapping it recreates hover
  jitter. Group separation has its own opacity metric because the old brush also
  served menu separators and gallery borders.
- An orb overhanging the caption needs WindowChrome.IsHitTestVisibleInChrome;
  otherwise its upper half starts a window drag. Hide it for Backstage while
  preserving the distinct application-menu layering rules.
- The soft dark edge is a shadow, not a heavy border. Refresh selection/notch
  geometry after Backstage closes because hidden ribbon layout can become stale.

The prototype required hosts to choose Orb explicitly and supplied a built-in
logo. Those policies were superseded by the [theme-native default](08-crystal-integration-and-shared-materials.md#3192-theme-native-office-2007-orb-default--2026-09-28) and
[portable glyph template](08-crystal-integration-and-shared-materials.md#3194-portable-application-orb-glyph-template--2026-09-28); the old recommendation is not current guidance.
The formerly deferred menu, dark palettes, window frame and Classic2007 surfaces
are covered by their later records, including [the menu](05-application-menu-localization-and-snapshots.md#346-the-real-office-2007-application-menu--a-control-that-had-to-sit-behind-the-orb--2026-07-28) and
[the window-frame plan](06-messages-release-and-window-frames.md#388-office-2007-opaque-and-aero-inspired-window-frame-plan--2026-08-12).

### 3.39 Stranded menus, take two: the borrow must not hang off `Popup.Closed` — 2026-07-27

§3.35 fixed proxy REBUILDING as the cause of empty source menus. Caching the entries was necessary
but not sufficient — a second, independent path stranded the same items, with the same
who-would-guess symptom: open the QAT overflow flyout, open a drop-down or split entry's menu inside
it, then dismiss everything with one click somewhere else in the window. From then on the ORIGINAL
button back in the ribbon opens onto an empty menu, which reads to the user as "the popup won't show
up at all". Nothing is wrong at the place they clicked.

**The mechanism.** WPF coerces `Popup.IsOpen` to false while a popup is not loaded. Closing the
overflow flyout unloads everything inside it, so the entry's own popup is coerced shut — and a
coerced value never travels back through the template's `IsOpen` binding. The entry is left with
`IsDropDownOpen == true` describing a popup that is already gone. Every consequence follows from
that one desync:

- The old return path was `Popup.Closed` → `BeginInvoke(Background)` → `if (!IsDropDownOpen) return
  the items`. The guard exists for a fast close→reopen, but here it read the STALE true and skipped.
- `OnOverflowClosed`'s `IsDropDownOpen: true` → `SetCurrentValue(false)` rescue was a no-op that the
  popup could never see (its effective value was already false), so no second `Closed` was raised and
  there was no second chance to return.
- The items stayed in a proxy that is only reachable through a flyout that rebuilds its list on every
  open. Nothing ever asked that proxy to close again, so the source menu stayed empty for the rest of
  the session.

**The fix is to make the PROPERTY the contract and the popup an implementation detail.**
`OnIsDropDownOpenChanged` now schedules the return on the false transition (it already borrowed on
the true one), so the round trip is symmetric and does not care whether a popup exists at all.
`OnPopupClosed` first SYNCS `IsDropDownOpen` when it finds it still true — that is the moment we
learn the popup was shut behind our back, and correcting it there both frees the return and stops
the entry from looking pressed (and from auto-popping its menu the next time the flyout opens, since
the stale local `IsOpen=true` would otherwise re-assert on load). `EnsureBorrowedItemsReturned()` is
the explicit hook for hosts: it closes the dropdown if needed and drives the return itself.
`RibbonQuickAccessToolBar` calls it for EVERY cached entry on close and before pruning one, instead
of testing `IsDropDownOpen` — the property it cannot trust. A final `Unloaded` net covers a host that
disappears without the popup raising `Closed` at all; it only fires when the popup is genuinely not
open, so a theme swap re-templating under an open menu is left alone. All four paths are idempotent
and re-guarded at Background priority, so overlapping requests move the menu exactly once.

**Rule of thumb:** `Popup.Closed` tells you the popup closed, not that the control agreed to it. Any
state a RibbonKit flyout owns must be reconciled on the CONTROL's property, and any popup that can
be nested inside another popup will one day be closed by its host rather than by itself.

**First unit tests.** This is also where the test project stopped being a smoke test.
`tests/RibbonKit.Tests/Sta.cs` runs a body on an STA thread with a live dispatcher plus a `Drain()`
that pumps queued Background work — no `Application`, no window, so it runs on a CI agent.
`DropDownBorrowTests` pins the whole borrow protocol (including this regression, which fails against
the old code) and `QuickAccessOverflowTests` pins the panel's measure/arrange rules and the command
proxy factory. `[assembly: InternalsVisibleTo("RibbonKit.Tests")]` was added for this: the contracts
worth testing here — `BorrowMenuFrom`, `EnsureBorrowedItemsReturned`, `OverflowedChildren`,
`CreateCommandProxy` — are deliberately not public API, and widening the surface to test them would
be the wrong trade. Testing without popups is not a compromise either; it is the point, since the
bug was caused by trusting the popup in the first place.

### 3.40 Chrome polish batch — pressed states, caption glass, and four hijacked tokens — 2026-07-28

Nine user-reported defects in one arc. They look unrelated in a screenshot and mostly are not: four
of them are the same mistake, which is worth naming before the list.

**The pattern: chrome that borrows a token another system owns.** A brush like
`Ribbon.Background` reads like "the strip's colour", so small chrome bound to it directly. But two
subsystems rewrite that key out from under a consumer — `ThemeManager` repaints it with the accent
on a coloured 2019 strip, and 2024 sets it `Transparent` so a Mica/Acrylic backdrop can show through
the band. Anything that borrowed it inherited both. **Chrome gets its own token whenever its intent
differs from the surface it sits on**, even when the two happen to share a value today.

**1 — No pressed state anywhere on the tab-strip chrome.** Six buttons (modal-tab close, minimize
chevron, the three merged-caption window buttons, the QAT overflow ») had hover and nothing else, so
a click never registered. New `RibbonKit.Brushes.TabStrip.ControlPressedBackground` in all five
generations: a step darker for the flat themes, the hover glass INVERTED (dark top face, specular
foot) for 2010, and the same inverted with 2007's hard crease. ⚠ **The `IsPressed` trigger must be
LAST in the block** — the pointer is over the button for the whole time it is held, so a trigger
placed before `IsMouseOver` (or before `IsChecked` on the overflow toggle) is immediately overwritten
and the press never shows.

**2 — Solid hover/press chips floating on Mica.** DWM composites a backdrop *beneath* the window, so
a solid `#E6E6E6` wash reads as an opaque sticker on the material. `ApplyTitleBarOverride`'s backdrop
branch now also swaps the caption buttons, the tab-strip chrome and the File button's pressed fill
for low-alpha black (`#1F000000` / `#33000000`), which tints the backdrop instead of covering it.
⚠ `ApplicationButton.PressedBackground` is **owned by the accent system** (2013 flat mixes, 2010 a
gel, 2007 glass), which runs first — clearing it unconditionally deletes the value that pass just
derived. The clear is guarded by `ReferenceEquals(resources[key], BackdropControlPressed)` so only
our own override is removed.

**3 — The 2024 File button's pressed fill had square bottom corners.** It was reusing
`TabCornerRadius`, right for 2010/2007/2013 whose File button is physically connected to the ribbon,
wrong for 2024 where it floats above the card. Split out as
`RibbonKit.Metrics.ApplicationButtonCornerRadius`; only 2024's value changes.

**4 — "Close Print Preview" sat flush against the window edge.** `PART_ModalClose` margin
`0,6,2,0` → `0,6,8,0`, matching the minimize chevron. Safe because the two are mutually exclusive —
the chevron collapses via the `IsModal` DataTrigger.

**5 — 2007/2010 caption buttons were flat chips on a glass caption.** All four
`CaptionButton.*Background` keys became gradients: hover lit from above, pressed INVERTED, close the
same recipe in red, with 2007 carrying the hard crease and 2010 a smooth ramp — the same
`Glass()`/`Gel()` split the code already makes. Close pressed stays LIGHTER than close hover, the
convention the other generations follow. Shared with `RibbonKit.MdiCloseButton`, which wants the
identical look.

**6 — A colored title bar flattened 2007 and 2010.** `SetAccentedTitleBar` wrote `Frozen(accent)`
for every theme; right for the flat generations, wrong for the two whose uncolored caption is glass —
it turned the top 34px into 2013. Each now keeps its own gradient SHAPE re-hued to the accent: 2010
reuses `CaptionRamp`, 2007 gets a new `CaptionValley` helper matching its own token (light lip,
deeper band at 0.28, bright specular foot). Not `CaptionRamp` (ends dark, loses 2007's bright bottom
edge) and not `Glass` (the hard crease belongs on a button, not a window-wide caption). Caption
buttons follow with `Gel`/`Glass` in the accent hue. ⚠ Every white mix here stays ≤ 0.30: the accented
caption draws white text and glyphs over it.

**7 — The QAT overflow chevron stayed dark on an accent title bar, in every theme but 2019.**
`UpdateQatButtonContext` only walked `QuickAccessItems`; the » lives in
`RibbonQuickAccessToolBar`'s own TEMPLATE and is not an item, so it never received
`Ribbon.QatOnColoredSurface` or the band brushes. 2019 hid the bug — its colored strip repaints
`TabStrip.Foreground` white app-wide, which the chevron's stroke happens to use. New
`ApplyQatSurfaceContext(host, colored, hoverKey, pressedKey)` does for a HOST what the loop does for
an item; because the attached property **inherits**, setting it on the host carries it into the
template. Called per host with its own flag (`_titleBarQatHost` → `titleBarColored`, the new cached
`_qatTabRowHost` → `tabRowColored`, `_qatBelowHost` → always false). ⚠ Resolve the brushes via
`TryFindResource` on the RIBBON, not the host — a title-bar host lives outside the ribbon's visual
tree — and never store null: a `Border` whose `Background` trigger sets null drops out of hit-testing
and the click falls through to the WindowChrome caption.

**8 — A minimized ribbon had no bottom edge in 2007/2010/2013.** Collapsing the body takes its
outline with it, and in the bordered generations the tab strip then butts straight into the app's
content. New `MinimizedDivider` Border in the tab control's **body row** (so the hairline lands
exactly where the body's top border was), painted in `Ribbon.Border`. Per-theme opt-out is a height
token — `MinimizedDividerHeight` is 1 for the three, **0** for 2019 (tinted band) and 2024 (floating
card), and zero costs them no layout height either, the same idiom as a zeroed
`ContextualUnderlineHeight`. ⚠ It triggers on `Ribbon.IsMinimized`, **not** on
`ContentHost.Visibility`: the collapse is animated in code (slide, fade, *then* Collapsed), so keying
off Visibility would pop the line several frames late.

**9 — The tab-strip scroll chevrons: wrong fill, and floating above the tabs.** The fill was
`Ribbon.Background`, so 2024 rendered a bare outline with tabs sliding under it and an accented 2019
strip produced an accent block still carrying a dark glyph. New
`RibbonKit.Brushes.TabStrip.ScrollButtonBackground`: 2007/2010/2013 restate what they already
rendered, 2019 and 2024 become white so the chip reads as the selected tab. The vertical offset had a
real cause rather than needing a nudge — the tabs live inside `PART_TabScroll` and are inset by
`TabStripMargin`, while the chevrons are its SIBLINGS and stretch to the full row, so they floated
above the tabs by exactly that top inset (2007/2010: 2, 2019/2024: 4, **2013: 0**). The user
independently reported "too high in every theme except 2013" — the one theme with a top of 0, which
is what confirmed the diagnosis. Binding their `Margin` to the same `TabStripMargin` token makes them
track the strip everywhere, and keeps them correct if a margin is ever retuned.

**Also: modal tabs no longer appear in Customize Ribbon.** `RebuildTree` excluded contextual and
merged tabs and had no modal case, so Print Preview sat in the list offering a visibility checkbox
for a tab the ribbon re-hides. `RibbonTab.IsModal` already existed, so the filter needed no new API —
the three inline copies of the predicate collapsed into `IsCustomizableTab`. ⚠ **The one-line filter
alone would have created a worse bug.** `CanMove`/`MoveSelected` counted positions in the RAW
`ribbon.Tabs`; hiding a tab from the tree while still counting it for reordering means Up/Down can
swap the selection with an entry the user cannot see. That was live in the showcase (Print Preview is
declared last, so "move Favorite down" traded places with it). Both now work in the filtered list and
the move targets the NEIGHBOUR'S raw index instead of ±1, so `ObservableCollection.Move` lands the
tab where the neighbour was and anything excluded in between shuffles along. The flaw already existed
for contextual and merged tabs; the modal filter merely made it reachable by default.

**Rule of thumb from this batch:** when a fix hides something from a list, check every OTHER
operation that indexes the same collection. Filtering a view is half a change.

### 3.41 Two ordering bugs: an Effect painted over, and a FLIP that flashed — 2026-07-28

Both are about *when* WPF draws, not what. They were reported as unrelated cosmetic glitches and
share no code, but the debugging lesson is the same, so they are recorded together.

**The ribbon's drop shadow only appeared under Mica.** The 2007/2010/2024 bodies carry a real
`DropShadowEffect` (`Effects.ContentShadow`), and an **Effect renders outside the element's layout
bounds** — straight into whatever sits below. Panel siblings paint in declaration order, and a host's
content area is declared after the ribbon, so its opaque background covered the shadow. Mica hid the
bug rather than enabling the feature: the showcase sets `MainContentArea.Background = Transparent`
while a backdrop is on and back to `White` when off, so the shadow only ever survived in backdrop
mode — which made a paint-order bug look like a backdrop feature. Confirmed from the screenshot
pixels: directly under the body border, Mica ON gave `dbe3e6 → e0e8eb → e5ecf0 → e8f0f4 → eaf2f6`
(a shadow fading downward) and Mica OFF gave five rows of flat `ffffff`.

Fixed with `<Setter Property="Panel.ZIndex" Value="1" />` in the `Ribbon` implicit style — the ribbon
paints last among its siblings. Nothing was clipping the shadow; it only needed to be drawn later.
Library-side rather than showcase-side on purpose: every consumer that puts content below a ribbon
hits this, and an app author would sooner conclude the theme has no shadow. It sits beside the
existing `VerticalAlignment="Top"` setter — the same kind of defensive layout default, and a local
value still beats it.

**The window title now glides when the backstage hides the QAT.** `Ribbon` sets
`IsTitleBarContentVisible = false` while the backstage is open, the template collapses the
quick-access slot, and the title — which lives in the `*` column between that slot and the caption
buttons — teleported sideways by half the slot's width. `PART_Title` is now a declared template part
and `RibbonWindow` animates the difference via the new `RibbonMotion.AnimateTranslateX` (the
horizontal twin of `AnimateTranslateY`), on `RibbonAnimationAction.Backstage` timing so it moves with
the backstage rather than on its own clock.

It measures rather than computes: the shift involves an Auto column, a themed margin (2007 insets the
slot to clear the overhanging orb) and a trimmed `TextBlock`, so hand-computed geometry would drift
from what renders. ⚠ **The two measurements are deliberately asymmetric** — BEFORE *includes* any
transform still running from a previous toggle (that is where the title visually is), AFTER
*subtracts* it (that transform is about to be replaced; we want the resting position). Reading both
the same way makes a fast open-close-open sequence jump.

**And then it flickered, intermittently — two independent one-frame bugs, both needed fixing.**
The symptom was the title snapping to its destination for a frame before animating properly.

1. **`DispatcherPriority.Loaded` runs AFTER `Render`.** The first version took its second measurement
   on a dispatcher hop, so layout and rendering both completed before any offset was set and the
   composition thread could present a frame with the title already home. Replaced with a one-shot
   `LayoutUpdated` handler, which fires at the end of the arrange pass, inside the frame that is
   about to be presented. **Never use a dispatcher hop for a FLIP in WPF.**
2. **The animation clock had not ticked.** WPF ticks the timing manager at the START of a render
   frame, before layout — so an animation begun during that frame's layout is first ticked on the
   NEXT frame, and until then the property falls back to its BASE value, which was 0: the
   destination. `AnimateTranslateX` now seeds `translate.X = fromX` immediately before
   `BeginAnimation`; the animation outranks the base value as soon as the clock catches up.

Intermittency was the tell — whether a frame is presented in either gap depends on render-thread
timing. Fixing only one would have left a shorter flash.

Bookkeeping worth keeping: `_titleShiftPending` stops a double subscription when a toggle arrives
before the layout pass (the newest BEFORE reading wins, since nothing has moved yet), and
`OnApplyTemplate` unsubscribes while the OLD part is still in hand or the handler pins a discarded
element.

**Also shipped here: the combo box drop-down fades and slides down.**
`RibbonAnimationAction.DropdownMenu` (130ms, 8px) had been declared with timings and **zero
consumers** — every flyout opened instantly. `RibbonComboBox.OnDropDownOpened` now calls
`RibbonMotion.PlayOpen(_popupRoot, DropdownMenu, RibbonSlideFrom.Top)`;
`RibbonDropDownButton`, `RibbonSplitButton` and `RibbonMenuItem` are the same three lines each if
they should follow. ⚠ **A `Popup` clips its child's `RenderTransform`**: the popup's window is sized
to the child's LAYOUT size and a transform does not grow it, so sliding the border up from -8 sliced
its top 8px against the window edge. Fixed with a matched pair — `Popup.VerticalOffset="-10"` plus a
10px larger top margin on the child — which leaves the resting position pixel-identical while giving
the slide room. Keep 10 > the slide offset if that is ever raised, and expect to repeat the trick for
any other flyout given a slide. Open only: a close animation would mean holding the popup alive past
the close, and `ComboBox`'s built-in mouse-capture management assumes the popup closes when it says
so.

### 3.42 Every flyout now opens as a whole surface — and the DPI manifest — 2026-07-28

Flyout motion separates surface opacity from content translation. A Popup HWND
is sized to layout bounds; moving its surface clips it. Extra child margins also
behave differently in plain Popup and managed ComboBox/ContextMenu hosts, so
shared margin/placement compensation is not portable.

PlayFlyoutOpen leaves the surface transform at identity, seeds Opacity to zero
before the first animation tick, and slides content inside the existing padding.
Completion restores the base opacity before clearing its clock; Rest also returns
disabled/interrupted surfaces to a visible resting state. The all-level
PopupMotionTests surface fixture now checks opacity and content offset too;
[the test audit](12-keytips-layout-and-validation.md#3239-test-suite-redundancy-review--2026-10-09) records that consolidation.

Controls with an existing Opened hook invoke motion there. RibbonPopupMotion
supplies AnimateOpen/OpenAction only for otherwise unhooked Popup/ContextMenu
surfaces; unsubscribe before resubscribing on template changes. ContextMenu itself
is the child of WPF's private popup. Native menu animation is suppressed with a
reference-counted, lifetime-scoped application resource override and restored on
close, so disabled RibbonKit motion is instant. Windows review verified this on
2026-08-01.

Collapsed-group command dismissal is deferred until after routed click dispatch
because closing reparents the command grid. Identify openers through their
TemplatedParent rather than a popup-crossing visual-tree walk. Selection browsing
is not a committed command. Dismiss nested flyouts before returning their content.
The QAT overflow surface locally resets QatOnColoredSurface: inherited host flags
otherwise leak into its popup and give ordinary menu entries accent-band paint.

Showcase's manifest declares per-monitor-v2 awareness; the library cannot choose
process DPI awareness for a consumer. This fixed bitmap stretching on live scale changes.

### 3.43 Split button: a vertical arrangement, and halves that acknowledge each other — 2026-07-28

Two changes to `RibbonSplitButton`, both about it reading as ONE control rather than two buttons
that happen to touch.

**Vertical arrangement (Large only).** Icon on top — the command half — with the caption and chevron
stacked beneath it on the drop-down half, which is Office's large Paste button. New public API:
`RibbonSplitButtonLayout` (`Horizontal` default / `Vertical`) and `RibbonSplitButton.Layout`, plus a
read-only `IsVerticalLayout`.

`IsVerticalLayout` is the piece worth keeping. `Layout` alone is not enough, because vertical is only
honoured at `Large` and the sizing engine steps a button down to `Medium` as its group narrows — so
the real condition is `Layout == Vertical && Size == Large`, and it has to be re-evaluated whenever
EITHER changes. `Size` is declared by `RibbonDropDownButton`, so the derived class re-registers it:

```csharp
SizeProperty.OverrideMetadata(typeof(RibbonSplitButton),
    new FrameworkPropertyMetadata(RibbonControlSize.Large, FrameworkPropertyMetadataOptions.AffectsMeasure, OnLayoutInputChanged));
```

⚠ `OverrideMetadata` **replaces** the base metadata rather than merging it, so the default and
`AffectsMeasure` are re-stated there deliberately — dropping either would break the sizing engine in
a way that only shows up on resize.

Publishing one flag instead of testing the pair in the template is not a convenience. The
vertical-only differences live in **three separate namescopes** — the outer template and the nested
template of each half — so the alternative was the same two-condition `MultiDataTrigger` written six
times, with six chances for the halves to disagree about which way round the button is.

**One grid, re-spanned; not two panels.** The halves stay put and the vertical trigger moves them
from `Column 0 | Column 1` to `Row 0 / Row 1`, with `ChevronColumn` pinned to 0 and the chevron
half's `Width` set to `Auto` (= `NaN`) so it stretches instead of staying a 15px sliver. A second
panel would have needed a second `PART_Primary` / `PART_Toggle`, and a template part may only be
named once per namescope.

The nested half templates read `IsVerticalLayout` through an **AncestorType** binding, not a
`TemplateBinding` — inside the `Button`'s own `ControlTemplate` the templated parent is the *button*,
not the split button. Same reason the vertical caption binds `Header` that way.

The caption is `TextWrapping="NoWrap"` + `CharacterEllipsis` at `MaxWidth="76"`. One line is the
contract, not a limitation: a second line here would make the two halves different heights whenever
the text was long, and 76 is `RibbonButton`'s Large width, so a vertical split button lines up with
the plain large buttons beside it.

**Companion highlight.** Hovering or pressing either half now marks the other one. Three tokens per
theme carry the whole difference:

| generation | `CompanionBackground` | `CompanionBorder` | `CompanionGlow` |
| --- | --- | --- | --- |
| 2007 | `Transparent` | `#C1A877` | `#CCFFEDC2` |
| 2010 | `Transparent` | `#E9C25E` | `#CCFFF0CB` |
| 2013 | `#F4F7FC` | `Transparent` | `Transparent` |
| 2019 | `#EFEEED` | `Transparent` | `Transparent` |
| 2024 | `#F2F2F2` | `Transparent` | `Transparent` |

The two treatments are genuinely different, not scaled versions of each other. The gradient
generations draw the amber outline plus a **1px glow rim just inside it, and no fill at all** — the
first cut washed the whole half in pale amber and the result was that a merely-adjacent button read
as almost as hot as the hovered one, which defeats the point of distinguishing them. The flat
generations have no highlight border to glow inside, so they take a lighter version of their own
hover fill and leave both border and glow `Transparent`.

Because the *theme* decides what companion means, the template needs three setters and four
triggers — identical across all five generations — instead of per-theme branching.

⚠ The glow is an **overlay in the same grid cell, not a nested container**. A real inner border would
inset the content by 1px in every state, so the icon would visibly shift the moment the other half
was hovered. `CompanionRim` draws only its 1px edge, is `IsHitTestVisible="False"`, and binds
`CornerRadius` to `Chrome` by `ElementName` so it tracks the horizontal/vertical switch for free.

⚠ The mark is carried on `Tag`. An outer `TargetName` cannot reach a half's `Chrome`, which lives in
that half's own namescope, so the outer triggers set `Tag="Companion"` on the sibling and each half's
template reacts. `Tag` is free on these two template-private buttons and costs no public API before
the Phase 8 freeze. The companion trigger is declared FIRST in each half so the half's own
hover/press always wins when both apply — press the command half, then drag onto the chevron.

New tokens: `Control.CompanionBackground`, `Control.CompanionBorder`,
`Metrics.SplitTopCornerRadius`, `Metrics.SplitBottomCornerRadius` — four per file, all five files.
The showcase's Paste button now ships `Layout="Vertical"`, which also demonstrates the fallback:
narrow the window and it returns to horizontal as it reduces to Medium.

**Design-time.** The Ribbon Editor's property panel gained a **gated** row: `PropSpec` now takes an
optional `AppliesTo` predicate and `BuildProps` skips rows that fail it, so "Split layout" appears
only when the selected button could actually render Large. Offering `Vertical` on a button that
can never be Large would write a property with no visible effect, and the author would have no way
to tell that apart from a bug in the control.

⚠ The gate is `CanRenderLarge`, which tests `Size == Large` **OR** `SizeDefinition` mentioning
Large — not `Size` alone. The sizing engine owns `Size` whenever a definition is present, so a
`Size`-only test would hide the row on exactly the buttons most likely to want it.

`AfterPropertyCommitted` runs after a `Size` / `SizeDefinition` edit: if the button can no longer be
Large it resets a `Vertical` layout to `Horizontal` and rebuilds the panel, so the row appears and
disappears as you edit rather than only on reselect. Two deliberate limits on that: it is driven by
an explicit EDIT, never by selection (silently rewriting a property because someone clicked a node
would put a surprise entry on the undo stack), and **the runtime control never coerces `Layout` at
all** — it falls back to horizontal while remembering the author's choice, so a button that reduces
to Medium and back is unchanged.

VS Properties window: `Layout` is described under the RibbonKit category with the Large-only rule
spelled out, and the computed `IsVerticalLayout` is `Browsable(false)` — it exists for templates and
triggers, not for authoring.

### 3.44 The shared easing function was never frozen — 2026-07-28

`dotnet test` failed 9 of 59, every one of them in `PopupMotionTests`, with

> System.InvalidOperationException : The calling thread cannot access this object because a different
> thread owns it

thrown from `Clock.AllocateClock` → `Timeline.GetCurrentValueAsFrozenCore` → `Freezable.CloneCoreCommon`.
Nothing in that stack names the culprit, which is what made it worth writing down.

**Cause.** `RibbonAnimation.SharedCubicOut` — the single `CubicEase` every transition reuses — was
constructed but never frozen. An `IEasingFunction` is a `Freezable`, so an unfrozen one takes thread
affinity from whichever thread first touched the class. Starting an animation **clones the timeline
and everything hanging off it**, so any LATER thread that builds a clock trips `VerifyAccess` on the
easing function. `Sta.Run` gives each test its own STA thread, so test #1 claimed the ease and every
test after it failed.

**This was not a test-only bug.** WPF supports a second window on its own dispatcher thread, and
every RibbonKit control in such a window would have thrown on its first hover. The suite found a real
defect in code that had shipped since the animation system was built.

**Fix:** freeze it (`Frozen(new CubicEase { ... })`, with a tiny generic helper — the same idiom
`RibbonEditorWindow.DropAdorner` already uses for its pens and brushes). A frozen `Freezable` has no
dispatcher affinity at all. ⚠ **Any shared `Freezable` added to the animation layer needs the same
treatment**, and the failure will point somewhere else when it doesn't.

`PopupMotionTests.A_transition_starts_on_any_thread` runs a transition on two successive STA threads.
Every other test in the file already crossed threads — they all failed together — so that one exists
purely so the NAME says what broke.

**Separately, two of the nine were the test's own fault.** `The_surface_is_never_transformed` reported
"carries a MatrixTransform" for a Border nobody had touched, because
⚠ **`UIElement.RenderTransform` defaults to `Transform.Identity`, which IS a `MatrixTransform`** — it
is not null. The helper was type-switching (`null` / `TranslateTransform` at 0,0 / `ScaleTransform` at
1,1) and treating everything else as transformed. Now it tests the MATRIX —
`transform is null || transform.Value.IsIdentity` — which is both shorter and correct for every
transform type, including the identity one WPF hands out by default.

### 3.45 Proxies never mirrored their source's enabled state — 2026-07-28

Disabling a ribbon command in code left every COPY of it live: the quick-access proxy, its overflow
entry, and any custom-group proxy stayed enabled and still invoked the command the app had just
switched off. The ribbon button greyed exactly as intended, which is what made it easy to miss —
nothing looks broken until someone uses the copy.

`Ribbon.CreateCommandProxy` copies icon/header/ScreenTip values and wires behaviour, but nothing ever
linked `IsEnabled`. One fix there covers all three surfaces: the QAT (`AddToQuickAccess`), the
overflow flyout (`RibbonQuickAccessToolBar.GetOrCreateEntry`) and custom groups
(`RibbonCustomizePage`) all build their copies through that one factory.

**Binding to the source's `IsEnabled` picks up the COERCED value, which is what we want.** A button
inside a group the app disabled reports `false` even though its own property was never touched, so
its proxies grey with it — the case an `IsEnabledCore`-style check on the source's local value would
have missed.

**⚠ It is a `MultiBinding`, and that is the whole design.** Two independent things disable a proxy:
its source, and `IsCommandParkedProperty` — the flag for a merged source that has stepped out and
should grey like Office rather than vanish (§3.33). They cannot be separate writes to the same
property, because **assigning a value to a property that carries a one-way binding CLEARS that
binding**. The merge service's old `proxy.IsEnabled = false` would have severed the source mirror on
the first park and never restored it — a bug that only appears after an unrelated feature is used
once. Combining both inputs into a single expression removes the ordering question entirely;
`SetProxiesEnabled` now sets the park flag and lets the binding recompute.

**⚠ And parking still missed the overflow flyout, one hop further out.** An overflow entry is a proxy
of the ORIGINAL command (a proxy of a QAT proxy would mirror the mirror), so the fix above gave it
the source's enabled state correctly — but the merge service only ever sets the park flag on
elements in `QuickAccessItems`, and an overflow entry is not one. Its twin in the strip greyed while
it stayed live.

Fixed by delegating one level: `RibbonQuickAccessToolBar.GetOrCreateEntry` re-binds the entry's
`IsEnabled` to the **strip item it stands for**, replacing the source binding
`CreateCommandProxy` installed. The strip item already combines both reasons, so the entry inherits
source-disable AND parking together — and any future third reason for free. Content and behaviour
still come from the source; only the enabled state is delegated. The general shape worth keeping:
**when B is a stand-in for A, derive B's state from A, not from what A derived its own state from.**

New public API: read-only attached `Ribbon.IsCommandParked` (+ `GetIsCommandParked`), with an
internal setter. `ProxyMirrorTests` covers source-disable, restore, park/revive, the
already-disabled-at-creation case, and — the one that guards the design —
`Parking_does_not_sever_the_source_mirror`, which fails if anyone splits the two inputs back into
two writes while the other four still pass. The overflow hop is NOT unit-tested: entries are only
built inside `OnOverflowOpened`, which needs the popup and panel template parts, and the harness
deliberately never opens a real popup. It is on the manual checklist instead.
