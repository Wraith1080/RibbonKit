# Native Backstage page content motion

Status, **2026-11-20: full Solid-pane preparation now precedes native travel.
Zoom-loaded before/after captures and focused checks pass; live review remains
in the [themes ledger](THEMES-PLAN.md#current-status-and-review-ledger).**
The earlier Solid-mode exposed-area correction was accepted on 2026-11-01.

The user's subsequent TextBox/RichTextBox flicker report is addressed by the
bounded repaint correction below. The user reported "looks good" after the
Solid-mode black-strip/rounded-corner correction and visible Info/Print review.
The remaining material, DPI and frame-pacing limits below still apply.

The user requested Fade/SlideDown when selecting another Backstage page, using
the existing tab-content preference, and a matching dropdown in Ribbon settings.
This supersedes the earlier immediate-page-switch contract. Accepted complete
Backstage opening/closing motion and caption/QAT behavior remain unchanged.

## Bounded contract

- Reuse `RibbonControl.TabContentAnimation` and `AnimationLevel`; no new public
  API or settings owner. Fade is the existing content-style default. None remains
  the animation-level default. System-disabled animation remains immediate.
- Fade brings the incoming native page body from near-zero to full opacity;
  SlideDown brings it down from 8/12 logical pixels above its final location for
  Subtle/Expressive, without a fade. Both use the existing tab-content cubic
  easing and 130/182 ms timing. The outgoing page is hidden after the incoming
  native drawing is ready, before the page-motion timer starts.
- Navigation, page frame and caption stay fixed during page changes. Selection
  commits once, with existing validation and focus routing. Opening/closing still
  moves/fades the entire Backstage surface using its accepted clock and travel.
- Animate the existing library-owned native page slot. Application Content keeps
  its parent, HWND, local bounds, data, binding, selection and disposal owner.
  No page bitmap capture, duplicate page or editor is introduced. Native opacity
  belongs only to the library slot and is restored after completion/interruption.
  Its opaque native redirection stays stable while visible and is released after
  hiding/disposal; the page subtree uses native descendant buffering.
- Initial selection, missing page content, None, system policy, update batches,
  unsafe host transparency/native setup and selection during an existing opening/
  closing or page transition settle immediately. Validation cancellation retains
  the old page. Rapid programmatic selection resolves to the newest page.
- Input, style/level changes, resize, model/content changes, hide/deactivation,
  handle destruction and disposal settle the committed presentation. Closing
  settles page motion before starting the complete-surface close. `IsAnimating`
  includes active page motion. The host retains its Windows compatibility
  manifest responsibility, described by the Designer Sample README.

## Designer settings

**Edit Ribbon → Ribbon → Ribbon settings → Tab content animation** offers
**Fade** and **SlideDown**. This same preference also controls expanded tab bodies,
arriving tab headers and ribbon group collapse/restore. Groups keep their existing
sideways slide. **Animation** remains the separate None/Subtle/Expressive choice.

The content-style choice uses the existing detached draft/protocol/transaction:
Cancel changes nothing, unchanged OK creates no undo entry, and mixed root edits
apply through one designer transaction. External style changes invalidate an open
draft. Reset restores Fade and omits the assignment; SlideDown serializes as:

```csharp
ribbon.TabContentAnimation = RibbonKit.WinForms.RibbonTabContentAnimation.SlideDown;
```

Designer images remain static placeholders and do not create native page bodies.

## Relevant verification

Normal Debug runtime/server/client/sample builds pass with no warnings/errors.
Evidence is under `TestResults/winforms-backstage-page-motion/`:

- **28/28** page/open-close cases in `page-motion-focused.trx`: 19 new page cases
  plus nine affected existing Backstage motion cases. Includes Fade/SlideDown on
  Solid/Mica/Acrylic, native handles/parents/field bounds/text/selection, stationary
  navigation/frame, no page capture, default/system/batch/validation policy,
  input, repeated selection, replacement/removal, style/level/resize/close and
  borrowed-content disposal. The prior immediate-page test now distinguishes an
  interrupted opening from an ordinary animated page change.
- **24/24** designer cases in `content-style-designer.trx`: root dropdown choices,
  detached static preview, Cancel/no-op OK, one framework UndoEngine unit including
  theme/level/style, invalid/numeric/stale rejection, JSON round-trip, CodeDOM
  default/Reset and four synthetic root-dialog scale/resize cases. The actual
  root-dialog PNG was inspected.
- **8/8** existing shared tab/header/group drawing cases in
  `shared-content-motion.trx`, including group slide at 96/120/144/192 synthetic DPI.
- These are **60 distinct focused cases across bounded runs**, not a full suite.
  The initial run's collection-assertion analyzer warning was corrected; subsequent
  focused builds have no warnings. No unrelated tests were repeated.
- The permanent package-only Designer Sample was refreshed in place. Its public
  API `--verify-animation` check passes against the packed library: authored
  Expressive tab motion, Backstage opening, Fade and SlideDown page transitions,
  then closing all start and finish on real UI timers. Native page owners and
  handles remain unchanged. See `package-native/result.log`. No Showcase helpers,
  private RibbonKit reflection, page capture or OS animation override is used.

Actual VS interaction, visible animation appearance, physical/mixed-monitor DPI
and frame pacing remain user review. The runtime check uses an offscreen Form.

For review, reopen the permanent Designer Sample solution, select Subtle or
Expressive plus Fade/SlideDown under Ribbon settings, accept and run it. Open
File, switch Info/Print in both directions, then check Back/open/close and native
field values/selection. The refresh preserves authored Form/UserControl resources.

## Native text repaint correction — 2026-11-01

The user clarified that the flicker occurs on Backstage page changes, including
native text/rich-text controls. A focused reproduction found forced visible
editor erases in the Fade path. The old path also invalidated all page descendants
on synchronization and explicitly erased/repainted the incoming subtree at motion
start. A managed buffered Panel does not buffer its native child HWNDs.

The library now buffers the page subtree at the owned slot in Solid mode; material
mode also retains its existing page-area compositor. Neither the Form nor authored
Content/editor styles are changed. Synchronization invalidates the navigation and
frame rather than the page subtree. Motion finishes pending paints without forcing
a visible erase. Both Fade and opaque SlideDown retain the slot's native drawing;
completion restores full opacity while keeping redirection stable. Native layering
is released once the slot is hidden/disposed. Live Content is not captured or
reparented, and no redraw freeze, animation delay or changed easing is introduced.

Windows' native descendant composition and redraw behavior are documented in
[extended window styles](https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles)
and [RedrawWindow](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-redrawwindow).

Focused evidence under `TestResults/winforms-backstage-repaint/`:

- `page-repaint-before.trx` and `before/` record the reproduction. Across three
  returns, each TextBox/RichTextBox received three library-forced visible erases
  and six paint messages in Fade. The Solid slot lacked native descendant buffering.
- The corrected Solid probe records **zero forced visible erases and three paints**
  per editor over the same three returns. Native show/exposure erases still exist,
  now inside the composed page paint; this is not a claim of zero native erases.
- Per-step assertions verify that progress updates do not repaint native
  TextBox/RichTextBox controls outside newly exposed pixels. Data, RTF, selection,
  handles and parents remain unchanged.
- **37 distinct focused cases pass across retained runs**: nine repaint cases for
  None/Fade/SlideDown across Solid/Mica/Acrylic, 19 page-motion cases and nine
  opening/closing cases. `page-repaint-final.trx` retains 31 passing cases; the six
  material buffer-boundary assertions were corrected to recognize their existing
  page-area compositor and pass in `page-repaint-material-final.trx`. Their native
  repaint/ownership assertions had already passed. Logs record requested/effective
  material and native composition flags. No full suite or unrelated tests ran.
- Normal Debug runtime/server/client/sample builds pass with zero warnings/errors.
  The permanent Designer Sample was refreshed in place. Its public package-only
  `--verify-animation` check now adds native RichTextBox controls in check mode
  only, preserving authored forms/resources. Both page styles start and complete
  on real timers, with native rich-text handles, RTF and selection retained. See
  `package-native/result.log`. The 130/182 ms duration/easing settings are unchanged.

The tests and package check use offscreen native Forms. Visible flicker/smoothness,
physical DPI, IME and compositor frame pacing remain live review; the counters do
not by themselves establish final visual acceptance. Reopen the updated permanent
Designer Sample solution and review Info/Print with None, Fade and SlideDown.

## SlideDown exposed-area correction — 2026-11-01

The user's black strip was reproduced in the visible permanent Designer Sample:
it persisted after the page slide completed and disappeared after a later repaint.
The native redirected page had initially drawn only its unclipped pixels. Sliding
it down exposed cached pixels that had never been painted. Rectangular exposed-row
invalidation removed the band but left small marks along the rounded top-left edge.

The slot now invalidates the difference between its old and new visible regions,
including the viewport's rounded clip, and finishes pending native paints before
publishing the moved frame. Completion and early settling use the same path.
The rest of the page retains its drawing; no full-page erase, capture, reparenting,
extra delay or animation-setting change is introduced. The existing native layer
ownership in Solid/Mica/Acrylic is retained.

Evidence under `TestResults/winforms-backstage-exposed-strip/`:

- The Solid cases in `exposed-strip-before.trx` failed to paint newly exposed rows.
- **20 relevant cases pass across retained runs**: four new Solid Subtle/Expressive
  progress/early-settle cases, six existing page style/material ownership cases and
  active-slide disposal pass in `exposed-strip-rounded-complete.trx`; all nine
  None/Fade/SlideDown native editor repaint cases pass in `exposed-strip-rounded.trx`.
  That latter run also contains four superseded rectangular-only assertions; the
  replacement assertions require the curved edge to repaint while keeping the
  page body outside the exposed band. Editors in the no-per-step-repaint check
  are positioned below that band. Passed scopes were not rerun solely for a log.
- Normal Debug runtime/designer/client/sample builds pass with no warnings/errors.
  The permanent package-only sample was refreshed in place. Its existing real-timer
  animation/native RichTextBox ownership/RTF/selection check passes in
  `package-native/result.log`.
- Visible computer-use review at the current 125% display scale reproduced the
  original band, then confirmed clean Print/Info/Print transitions and settled
  rounded corners with the final package. No Alt or focus change was used to repair
  the page. This is sampled visible evidence, not continuous frame-pacing acceptance.

New exposed-region paint coverage and visible review are scoped to Solid mode.
Material child-paint probes did not establish equivalent visible correctness under
nested native redirection; existing material state/editor regression checks pass,
but visible Mica/Acrylic review remains pending. Human smoothness/flicker acceptance,
IME and mixed-monitor DPI checks also remain pending.

## Navbar blank-frame and rounded-edge follow-up — 2026-11-11

The user's recording showed two consecutive blank content frames during the
first Info-to-Print switch (approximately 67 ms at 30 fps); the navigation stayed
painted. A visible public-package reproduction using the permanent Designer
Sample's authored controls and normal animated opening captured one blank frame
before correction. The initial page had not acquired a page-motion drawing layer,
and the outgoing page was hidden before the arriving native subtree was painted.

Motion preparation now retains the outgoing page's native drawing as well as the
incoming page's drawing. Synchronization shows and paints the arrival behind the
outgoing page, then brings it forward and hides the outgoing page. Native layer
creation stays inside the existing motion eligibility/error handling. Immediate
selection also paints the replacement before hiding the old page. Existing
timing, easing, validation, native editor buffering and hidden-slot layer release
are retained; runtime presentation uses no page screenshots or added delay.

The user also reported a black bar around the content pane's left rounded corner.
The visible reproduction captured black pixels in **19 of 45 sampled frames**,
including after the slide completed. Four existing exposed-strip cases failed on
the unchanged baseline. The page slot checked only its direct rectangular content
viewport, missing the rounded page-frame ancestor. Its visible-region calculation
now intersects ancestor clips in slot coordinates, so progress/completion/early
settling repaint the actual newly exposed curve without erasing the page body.

Evidence is under `artifacts/backstage-navbar-flicker/`:

- **39 focused Release cases pass** in `tests/page-and-corner-final.trx`: four new
  first-switch handoff cases for both light families with Expressive/None, the
  four existing rounded exposed-strip cases, native editor repaint/state, page
  motion/interruption/ownership, selection validation and opening interruption.
  The earlier failing handoff and rounded-edge baseline results are retained.
- Normal Debug runtime/designer server/client/sample builds pass with zero
  warnings/errors. The permanent package-only Designer Sample is refreshed to
  `0.1.0-designer.local20261111T033530237Z`; authored Forms/UserControls/resources
  were preserved.
- Six fresh visible Solid-mode sample launches at actual **120 DPI**, three per
  light family, pass **36 Info/Print switches and 299 sampled compositor frames**
  with **zero blank page frames and zero black-corner frames**. Results, frame
  counts and representative PNGs are in `corrected-Office2019Light-{1,2,3}/` and
  `corrected-Office2024Light-{1,2,3}/`. The corrected settled Print corner was also
  inspected against `corner-before/switch-0-Print-04.png`. Native editor handle,
  parents, text and selection remain intact. Captures that lost foreground
  ownership fail explicitly and are excluded from passing evidence.

The new `--verify-backstage-navbar-paint <output> [Office2019Light|Office2024Light]`
sample diagnostic uses public package APIs and a separate screen-capture thread
to observe synchronous native handoffs. It does not force page refreshes. Its
black-pixel check covers the authored sample's blank top-left margin, while its
blank-page check requires authored content ink throughout opaque SlideDown.
These are sampled visible checks, not continuous frame-pacing or human acceptance.
Visible material modes, other physical DPI/monitors, IME and actual VS remain
separate. No full suite, full native matrix or WPF checks ran.

## Whole-pane buffering and first page frame — 2026-11-20

Solid opening and closing use native descendant buffering for the whole moving
pane, including the navigation rail and live page. Motion retains valid drawing
and invalidates newly exposed strips. Material retains its alpha rail and
separate native page layer. The opening seeds the native drawing at minimum
opacity before applying the authored frame.

An incoming animated page remains nearly transparent until it is raised and its
pending native paint finishes. The prior page stays visible during preparation.
SlideDown publishes the prepared opaque frame, and Fade starts its existing
opacity transition. Application controls keep their original parents and handles.

- Six regression assertions failed on the previous code and passed with the fix.
  All 77 focused motion, handoff, Aero and native editor checks passed.
- At 120 DPI, the independent public-package app sampled 294 compositor frames
  across nine openings and 18 page changes. Office 2010 Aero Solid, Office 2024
  Solid and Office 2019 Acrylic produced no black strips in the sampled interior.
  Its native editor retained its parent, handle, text and selection.
- The packaged Designer Sample check sampled 49 frames across six Office 2024
  Info/Print clicks and found zero blank page or black corner frames. Showcase
  and the persistent Designer Sample built in Debug with package
  `0.1.0-designer.local20261120T090923674Z` used for the sample and package checks.

Evidence is under `artifacts/backstage-animation-black/`. The supplied recording
shows interrupted-looking transitions but does not contain a large black region
in its sampled frames. The desktop checks sample the compositor at one DPI and
do not establish continuous playback or mixed-monitor behavior.

## File during the Office 2024 Backstage close — 2026-11-20

The recording showed the blue open File underline returning under the outgoing
pane before the gray hover underline appeared. Closing now invalidates the
covered File bounds as the pane fades. The pointer determines its hover
appearance while the pane still owns input, and the same hover state is handed
back to the ribbon when Backstage closes. The exposed Office 2010 File button
keeps its established closing behavior.

All 49 focused checks passed. At 120 DPI, five closes in an independent
public-package app showed 26 stale blue File frames across 86 samples on
`0.1.0-designer.local20261120T090923674Z`. Five closes with the fix showed
zero blue File frames across 88 samples and retained the gray hover underline.
Captures are under `artifacts/backstage-file-close-flicker/`. Showcase and
Designer Sample rebuilt in Debug. The refreshed package is
`0.1.0-designer.local20261120T091838772Z`.

## Ribbon repaint pressure and motion preparation

**2026-11-20.** Current live review is recorded once in the
[themes ledger](THEMES-PLAN.md#current-status-and-review-ledger). This follow-up
keeps the live Backstage child pane, native page handles and parents, existing
travel/fades, input cancellation and reduced-motion policy. The Form itself
does not move. A large pane still incurs native drawing/composition work.

Caption-only invalidation previously invoked the command-body group and item
drawing hooks. The body and target loops also replaced the caller's Graphics
clip. Ribbon paint now intersects the damage/caller clip, skips the body for
caption-only damage, and skips targets whose viewport does not intersect the
damage. This avoids repeating command text/SVG work on caption animation ticks.
Pane and page clocks now start after their initial native drawing finishes;
slow preparation no longer spends the animation's time budget.

The separate backdrop title shift came from a missing SingleLine flag on group
captions: composited theme text did not vertically center that caption. The
same single-line caption contract now applies to opaque and material drawing.
Before the correction, raster comparisons put material text 3, 4, 5 and 6 pixels
higher at 96, 120, 144 and 192 paint DPI respectively. The four cases now pass.

Verification and limits:

- The Release test assembly passes **166 selected cases** covering the new
  caption clipping/alignment and slow-preparation checks, Backstage pane/page
  motion, native editor repaint/retention, File/caption handoff, Aero, affected
  text/header clipping and preference storage. Tests ran through the scoped
  in-process runner under `artifacts/backstage-opening-followup/Runner`, with
  the consumer's Windows compatibility manifest. Standard VSTest transport
  timed out before connecting; no clean VSTest/full-suite run is claimed. The
  earlier manifest-less diagnostic run was invalid for layered native children;
  retain its failures alongside the corrected run.
- Normal Debug runtime/designer server/net472 client, Showcase and Designer
  Sample build successfully. The first refresh had a transient designer-cache
  write warning; the subsequent normal sample build is clean. The refreshed
  captured public package is `0.1.0-designer.local20261120T130551952Z`. The final
  refresh after source line-ending normalization uses
  `0.1.0-designer.local20261120T131504919Z`, with the same implementation and clean
  normal Debug sample builds.
- At actual 120 DPI, the public-package capture host loads the actual sample
  forms with user storage disabled. Office 2024 maximized Showcase/Mica has
  **2,158 -> 0** caption-triggered body group/item drawing calls across ten
  openings and six Info/Print changes in each package. The before/after runs
  sampled 134/137 compositor frames with no detected dark strips. Corrected
  Designer Sample Solid/Acrylic runs each cover ten openings and six changes,
  sampling 135/137 frames with no detected dark strips. Page editor handles,
  parents, text and selection are checked separately from the drawing counts.
- A final detector pass includes the exact Office 2024 pane top below the
  45-device-pixel caption, with only a two-pixel outer safety margin at 120 DPI.
  Ten openings and six changes each in Showcase/Mica and Designer Sample/Solid
  sampled 136/140 frames with zero detected dark strips. This closes the earlier
  detector's gap at the pane boundary; it still does not establish continuous
  playback or reproduce the reported intermittent bar.
- Detector captures, per-transition UI gaps and paint counts are under
  `artifacts/backstage-opening-followup/`. Early foreground-interrupted runs
  are incomplete and are not pass evidence. Capture work affects timing;
  neither these counts nor sparse desktop samples establish continuous pacing,
  behavior while Zoom is running, WPF performance parity, actual VS or
  mixed-monitor acceptance. These runs did not reproduce the reported residual
  black bar, so it is not marked fixed.

Showcase's missing Aero and File-surface persistence is application-owned; see
the [preference follow-up](M5E-SHOWCASE-PERSISTENCE.md#frame-and-file-surface-preferences).

## Full Solid pane preparation under Zoom

**2026-11-20.** A faster, unsynchronized capture of the actual Designer Sample,
default renderer and View tab reproduced a vertical dark strip at the left edge
of the moving Solid pane with Zoom running. Full-window synchronized readback
was only sampling about 14 frames/second under this load. The upper-pane capture
reaches about 30 frames/second and checks the affected left edge separately from
legitimate black command artwork. Foreground plus window-at-point checks and
keeping the fixture pointer away from the taskbar exclude preview overlays;
the first Zoom run with taskbar previews is invalid evidence.

Solid previously moved the hidden pane partly outside the visible client before
its first native paint. Its new redirection cache could therefore start with
unpainted pixels that later travel exposed. Opening now keeps native alpha at
one while the full pane and its real children paint at their settled origin,
then applies the authored initial travel and publishes the frame. Subsequent
steps retain that drawing. No application-page bitmap, duplicate controls,
reparenting, new opacity preference or rendering backend is introduced. Material
keeps its existing separate page-layer path; closing and motion policies retain
their existing owners. Cancellation during preparation returns before moving
an already settled pane.

Suppressing native copy-bits for Solid movement alone did not fix the captured
strip; that trial was reverted. Preserve its captures as an investigation limit.

- The matched before/after Designer Sample cases each cover ten openings, six
  Info/Print changes and ten closes at actual 120 DPI with Zoom running. The
  prior binary shows **3 left-strip frames in 410 samples**; the corrected
  candidate shows **zero in 410**. Candidate Showcase/Office 2024 Solid and
  Designer Sample/Office 2010 Aero Solid scopes show zero in 404/406 samples.
  Captures and timings are under `artifacts/backstage-opening-followup/zoom-*`.
- Two new native regression cases fail before the correction: their first
  drawing starts at Left=-155. They now verify a full-origin first drawing at
  alpha one, retained authored travel, and zero paint/erase messages for native
  fields outside the exposed strip across four movement steps, with unchanged
  handles, parents, text and selection. A third case covers cancellation from
  the first paint. **98 selected Release cases pass** through the same
  compatibility-manifest in-process runner as the prior record; no new full
  suite or successful VSTest transport run is claimed.
- Normal Debug runtime/designer server, Showcase and Designer Sample builds
  pass with zero warnings/errors; the independent net472 client has no change
  and retains its prior clean build. Designer Sample is refreshed in place to
  `0.1.0-designer.local20261120T135235944Z`; authored source/resources and normal
  output folders are preserved.
- The refreshed public package also passes the three final 120-DPI Zoom-loaded
  capture scopes: actual Designer Sample/Office 2024 Solid, actual Showcase/
  Office 2010 Standard Solid, and an independent Office 2024 Solid model with
  no sample resources or helpers. Each covers ten openings, six page changes
  and ten closes; **424/424/426 samples show zero detected left-strip frames**.
  These are the `zoom-package-*` records, with native editor retention checked.

The captured strip is corrected in these sampled scopes. Continuous playback,
other hardware/DPI/material combinations and the user's final live review remain
separate; Office 2007 stays paused at the existing review boundary.
