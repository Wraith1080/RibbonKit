# Writer host integration for RibbonKit 1.1

> Implemented locally with focused verification, 2026-10-22. The user approved the document-area
> animation proposal in this chat and authorized focused straggler fixes after
> the Simplified font-label failure was reproduced. Overall 1.1 acceptance and
> release stay in the original chat. No version change or publication.

## Preferences and presentation

- View's Touch/Mouse Mode dropdown matches Showcase's large button, heading,
  illustrated Mouse/Touch rows, descriptions and selected-row highlight. Its
  choices map to `RibbonDensity.Compact` / `Touch`.
  Writer saves it in appearance.json and restores it in window construction,
  before Show and RibbonKit's first-render readiness. Detached settings/dialog
  scopes receive it explicitly; a Window Owner does not supply inheritance.
- Appearance offers Office 2024 Flat in light/dark, using the shared theme,
  backdrop, glass, accent and Backstage APIs. Flat uses Writer's neutral Office
  workspace and steady paper edge, and shares 2024's transparent-ruler policy
  only when a backdrop is actually active. Crystal-specific overlap stays Crystal-only.
- The QAT icon choices set `Ribbon.QuickAccessIconAppearance`; the shared QAT
  presentation owns White Overlay, Color with Glow and Adaptive Color.
- Density and icon appearance are additive schema-v1 fields. Missing values
  default to Compact and WhiteOverlay. No schema bump, destructive migration or
  ribbon-layout rewrite is needed. Existing fields survive round trips.
- Appearance preview, Apply/OK/Cancel and restart use the existing transaction.
  Cancel returns to the latest Apply checkpoint. Appearance defaults retain the
  separate View input-mode choice. Simplified customization keeps its existing
  independent draft, defaults and Apply checkpoint.

## Approved document-animation design

Writer owns a small presenter for `DocumentPresentationHost`, containing paper,
the ruler, margin guide, native WPF editor and the fixed-page preview. It listens
to the existing Preparing/Starting/Ended events and binds its drawing to
`BoundaryMotion.Progress`. It never controls a RibbonKit clock or completion.

Preparing holds the old visible document drawing before final layout is exposed.
Starting measures Writer's actual final host bounds in CoordinateRoot, captures
its committed presentation and moves it from the previous visible origin to the
new origin. Text and page artwork keep their natural scale. The real editor,
document, selection, undo and zoom remain in their committed tree. Workspace
paint fills the exposed area; the status bar stays stationary. No document
pagination, zoom or page-size mutation participates in the animation.

The same path covers Classic/Simplified, Compact/Touch and QAT movement into or
out of BelowRibbon, including TitleBar height changes. When occupied document
bounds are unchanged, no movement is invented. The top clip follows the occupied
ribbon boundary, and the bottom clip stops at the status bar. QAT decoration and
shadow remain library-owned. Crystal's existing underlay and fade mask return
with the committed live content; temporary Writer drawing must never cover QAT.

Interruption samples the currently presented origin/drawing, then scopes cleanup
to transition identity so Ended(A) cannot release B. Unload, theme/view/document
replacement, resize, DPI/RTL changes and input discard Writer's temporary drawing
and reveal committed content. Authored opacity, transforms, clips and bindings
must survive. The presenter changes only its own composed hide clock.

Startup and disabled/reduced motion snap. Unsafe native/media/VisualBrush
content, active IME composition, unavailable ancestry/layer, or excessive capture
work also snap. WPF RichTextBox is drawn through its WPF visuals; native caret,
IME candidate windows, HWND-hosted objects and other external surfaces cannot be
captured as ordinary vector content. No screen capture or native-editor replacement
is planned. Mixed-monitor DPI remains a separate live review item.

## Current verification and handoff

This is assembled focused evidence from 2026-10-22, not one full-suite run or
overall RibbonKit 1.1 acceptance. Initial failures were corrected and their
affected scopes rerun; passing independent scopes were retained.
Scope clarified by the user on 2026-10-22: native touch and IME testing are not
required for this slice.

| Gate | Result |
| --- | --- |
| Normal Debug Writer | Build passed, no warnings/errors; default `samples/RibbonKit.Writer/bin/Debug/net8.0-windows/` output |
| Preferences and backward compatibility | `WriterAppearancePreferencesTests`: 17/17 passed, including old schema-v1 fields, new defaults, enum validation, normalization and persistence separation |
| Appearance UI | `WriterAppearancePageTests`: 3/3 passed; wide/narrow arrangement, defaults retaining View density and shared controls |
| Actual host preferences | Three `WriterHostPreferences*` tests passed; startup snap, live preview, Apply → edit → Cancel, OK/restart, nonempty selection, dirty/page/zoom preservation, 12 Flat/palette/density/icon states and all ten detached dialog types |
| Document animation | Three `WriterDocumentMotion*` tests passed; 13 mode/density/QAT/preview/Crystal changes, three sampled frames each; source properties, playing supersession, resize, disabled motion, synthetic composition and unsafe-capture/root-transform fallback |
| Existing Writer interactions | Simplified settings draft/Apply/Cancel/restart, Crystal settings checkpoint/restart, view toggle persistence and Crystal native-scroll/underlay: 4/4 passed |
| Find/Replace | Existing service regressions: 14/14 passed; dialog participates in the detached-family rendering check |
| Dropdown presentation follow-up | Normal Debug build: 0 warnings/errors. Five affected checks passed across focused runs: direct/QAT menu behavior in Flat light and Crystal dark, persistence/checkpoint/restart, appearance rendering and shared document motion. Menu renders inspected; stable command identity retained |
| Writer total | 46 distinct tests passed across the original and dropdown follow-up focused runs; no full Writer/release campaign |
| Focused runtime | Read-only label regression, existing Simplified combo editors and dropdown grouping: 23/23 passed |
| Independent RibbonKit-only consumer | `SimplifiedCombos`: 1/1 aggregate passed in 84.0 seconds; 56 theme × palette × density × direction combinations, with simulated 150% root DPI in Touch |
| Render inspection | Inspected Appearance light/dark, Flat density/QAT examples, and representative paper/preview/Crystal motion frames. These are offscreen WPF renders and synthetic interaction, not human/native acceptance |

Reproduction and scope exceptions are RKWF-050/051/052 in the
[Writer friction log](12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md).
The shared runtime exception was reproduced in the existing Simplified test
alone before the user authorized its two binding-mode corrections. No version,
public API, theme template or grouped Writer font UI changed.

## Files changed

- Writer preferences/UI: `WriterAppearancePreferences.cs`,
  `WriterAppearancePage.xaml`, `WriterAppearancePage.xaml.cs`,
  `WriterAppearanceScope.cs` under `samples/RibbonKit.Writer/Appearance/`;
  `MainWindow.xaml`, `MainWindow.xaml.cs`, `MainWindow.Appearance.cs`, `Icons.xaml`;
  `Editing/WriterFindReplaceDialog.xaml`; `View/WriterDocumentTransition.cs`.
- Focused shared fix: `src/RibbonKit/Controls/RibbonComboBoxSimplifiedProxy.cs`.
- Tests: `tests/RibbonKit.Tests/SimplifiedComboReadOnlyLabelTests.cs`;
  Writer tests `Appearance/WriterAppearancePreferencesTests.cs`,
  `Appearance/WriterAppearancePageTests.cs`, `Shell/MainWindowIntegrationTests.cs`,
  `Shell/WriterHostPreferencesTests.cs`, `Shell/WriterDocumentTransitionTests.cs`.
- Documentation: this checkpoint; `README.md`; `04-DESIGN-NOTES.md`;
  `docs/10-RIBBONKIT-WRITER-PLAN.md`, `12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md`,
  `18-HOST-LAYOUT-TRANSITION-PLAN.md`, `history/03-writer-host-integration.md`,
  `history/library/14-simplified-ribbon.md`.

## Bring back to the original 1.1 chat

- [x] Review-ready Debug implementation of the three additions and the approved
  Writer document animation; existing Simplified defaults/customization retained.
- [x] Focused automated evidence and backward-compatible preferences recorded.
- [ ] Human review of Flat light/dark, accent/glass/native backdrops, all three
  QAT icon choices, and Mouse/Touch across Classic/Simplified and QAT placements.
- [ ] Keyboard, Windows reduced motion, narrow windows and native
  mixed-monitor DPI; inspect motion interruption and Crystal's underlay at rest.
- [ ] Decide remaining overall 1.1 acceptance/release gates in the original chat.
  Full release tests, packaging, printing/designer acceptance and publication
  were intentionally outside this slice. Nothing was committed or pushed.
