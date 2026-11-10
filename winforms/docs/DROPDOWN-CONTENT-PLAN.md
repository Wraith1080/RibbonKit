# Retained dropdown and split-button UserControl bodies

Status, **2026-11-02: user accepted dropdown/split UserControl popup behavior; actual Visual Studio authoring review remains pending**.
The **2026-11-10** [native drawing-hold correction](#native-drawing-hold-correction--2026-11-10)
fixes the subsequently reported error 87; focused and live native checks pass,
with user review of this correction pending.
The user selected ordinary dropdown/split-button hosting after the gallery popup
work, approved retained content, and requested a popup that fits the body size.
This is native WinForms work. This increment adds runtime hosting and class
selection in Edit Ribbon. The separate
[inert popup designer preview](POPUP-DESIGNER-PREVIEW.md) is now implemented.

## Contract

- `RibbonDropDownItem.ContentType` selects a public, concrete UserControl with a
  public parameterless constructor. Split buttons inherit the same contract.
  Ribbon initialization creates all declared types before Form.Load, including
  hidden/disabled/nested declarations, without opening their popups. Late changes
  create content immediately, or when the active update batch ends. Designer
  discovery, drafts, previews and Undo do not run application constructors.
- The item owns a ContentType-created body. Source, nested-menu and QAT routes
  retain one instance; one source has at most one live popup. Reopening detaches
  and reattaches the same body, preserving native handles, text and selection.
  Reset/replacement and item disposal dispose owned bodies.
- `Content` also accepts an unparented, application-owned native Control when
  ContentType is null. Clear borrowed Content before selecting a type. Clearing,
  replacement and ribbon disposal detach borrowed content without disposing it.
  A weak ownership claim covers both dropdown and Backstage declarations, even
  while a body is unparented. Do not reparent a body while assigned.
- Custom content replaces the visible menu body; the existing Items collection,
  heading and presentation settings are preserved. Clearing custom content
  restores the original rows. There is no row/body composition mode. Galleries
  retain their separate presenter properties and reject Content/ContentType.
- The popup uses the body's native Size after normal UserControl DPI scaling,
  plus the existing frame and padding. Body size changes update the popup.
  Placement and frame size remain constrained to the monitor work area. The
  library adds no scrolling and does not shrink the body; applications should
  supply a body that fits the target monitor or implement internal scrolling.
- Native child controls own text input, Enter/button actions and validation.
  Tab/Shift+Tab traverse the body's controls and honor canceled validation.
  Escape closes a native ComboBox list first, then one ribbon popup level. The
  body can call its source's existing CloseDropDown method to dismiss; an Apply
  handler owns application validation and business actions. Escape/light dismiss
  do not implicitly apply the application form. Focus returns to the actual
  source/QAT opener. Split primary activation remains independent.
- Existing popup framing, placement, opening cancellation, dismissal and motion
  remain library-owned. The application owns body layout, data, font/colors,
  resources and commands. Native body accessibility uses the normal WinForms
  provider rather than the painted menu-row provider.

## Authoring and sample

Build the consumer, select a dropdown or split button in **Edit Ribbon**, then
choose **Data → ContentType**. The component Properties view has the same class
selector. `(None)` restores the menu rows. The stored declaration is:

```csharp
selectDropdown.ContentType = typeof(PopupOptionsControl);
```

Edit the body using its ordinary UserControl designer. The ribbon designer lists
metadata and never renders or constructs that arbitrary application body.
Application-created instances may instead be assigned to Content from code.

The permanent Designer Sample's **Home → Select** and **Paste arrow** use separate
instances of `PopupOptionsControl`, with ordinary designer-created native labels,
TextBox, ComboBox, CheckBox and Apply/Cancel buttons. Form code handles application
events. Original menu rows and the user's form/backdrop edits are preserved.
The source can be added to QAT through the existing customization flow.

## Verification, 2026-11-02

- Normal Debug runtime and designer builds passed with zero warnings/errors.
- New runtime regressions passed **17/17**: pre-Load initialization, late/batched
  declarations, ownership/replacement/disposal, invalid types, retained native
  state, QAT inactive-tab routing, split primary behavior, menu fallback, opening
  cancellation, disable/external disposal, native validation and constrained sizing.
- Designer checks cover metadata-only discovery, detached preview/Cancel, one
  Apply transaction, preserved menu children, unavailable/borrowed conflicts,
  real UndoEngine Undo/Redo and CodeDOM typeof/borrowed-reference serialization.
  Five passed initially; two serialization assertions expected a field reference
  where the real serializer emitted a variable reference. Those assertions were
  corrected and only the two failed cases rerun, passing **2/2**. This covers
  seven distinct cases across runs, not one initial aggregate pass.
- Independent package-only consumer passed before class-picker implementation:
  `artifacts/dropdown-content/consumer-runtime/result.log`. Queued native Tab and
  Escape, ComboBox first Escape, application validation/Apply, source and QAT
  routes passed at actual **120 DPI**. Each 320×268 logical body became 400×335
  native pixels; the popup surface was 410×345 including padding. Source and
  QAT split captures were inspected. The fixture uses the split reference's
  explicit DropDownKeyTip for its arrow; the primary KeyTip still invokes the
  primary command.
- Normal native Release solution build passed with zero warnings/errors. Its
  full test run passed **1,021/1,025**, including all 24 new runtime/designer
  cases. Four existing failures repeated in a focused recheck: Backstage material
  composition, Backstage page-switch motion, minimized Simplified group motion,
  and customization parent-flyout retention. All four also reproduced using the
  pre-change `0.1.0-designer.local20261102T110604504Z` runtime package with the same
  test binary; the non-minimized group variant failed there too. The current
  Release runtime was restored and its hash checked after this comparison.
  This is not a clean full-suite result or an investigation of those older issues.
  Evidence: `TestResults/dropdown-content-{full-release,release-rechecks,prechange-runtime}.trx`.
- Four additional focused cases cover source/QAT handover, real QAT overflow for
  dropdown and split, nested-menu focus return/native accessible children, and
  constructor-failure recovery. Three passed initially; the accessible-child
  check exposed a missing native child listing on the popup pane. The native
  accessible wrapper was added and only that failed case rerun, passing **1/1**.
  These four cases were added after the full run and are separate evidence.
- The final normal Release solution build also passed after the native child
  accessibility correction. The permanent Debug Designer Sample was refreshed
  in place to `0.1.0-designer.local20261102T113814113Z`, with clean designer/client,
  package and consumer builds. The final package-only native check passed again
  with Subtle popup motion enabled:
  `artifacts/dropdown-content/consumer-final/result.log`. The final dropdown
  capture was inspected. In total, 28 new focused cases are covered across the
  stated runs; the broader full run predates the final four routing cases.

Actual Visual Studio class discovery, save/reopen and Undo interaction, human
visual acceptance, physical mouse/touch/IME, RTL, external accessibility and
mixed-monitor DPI transitions remain separate review gates. Ordinary popup motion
is reused; no new animation policy or gallery interface is introduced.

## Native drawing-hold correction — 2026-11-10

The user reported `Win32Exception` / `Native child drawing hold failed: 87` in
the permanent Designer Sample. Its Windows compatibility manifest was already
correct. A first-chance exception observer reproduced six caught failures while
the earlier consumer's content/input assertions still passed. That earlier
"animated" pass did not establish that opening motion actually ran.

`InitializeContent` explicitly forced the popup surface HWND while it was still
unparented. Windows subsequently refused its layered opacity style: the measured
extended style remained `0x10000` rather than gaining `WS_EX_LAYERED`, and
SetLayeredWindowAttributes returned error 87. The library caught the exception
and opened immediately. The correction removes only that premature surface
handle access. `content.CreateControl()` still performs the body's native
initialization, Load and autoscaling before sizing; the surface gets its native
parent through the ToolStrip host before opening motion requests its handle.
The constructor-order speed improvement, retained body/field HWNDs, source/QAT
identity, gallery lazy cards, animation duration and public API are unchanged.

- Four new ordinary/split, fresh/precreated-body motion cases failed before the
  correction and pass after. They assert zero caught native drawing-hold failures,
  active opening motion, actual native opacity hold, cleanup and reopening.
  Two further cases preserve body Load failure/owner-removal behavior and cleanup.
- The final focused Release slice passed **160/160** across those cases and
  related content/routing, popup motion/window creation, custom gallery and
  Application Menu cases. This is not a full-suite run.
- The final package-only Designer Sample passed both **Subtle and Expressive**
  native dropdown/split checks at actual **120 DPI**. They now require active
  motion and zero first-chance drawing-hold exceptions, alongside retained native
  editor state, queued Tab/Escape, native ComboBox first-Escape, validation/Apply
  and inactive-tab QAT routes. The 1,000-entry gallery check also passed.
- Debug runtime/designer/client/package and sample builds passed cleanly. Refresh
  version: `0.1.0-designer.local20261110T041833936Z`.
- Evidence is under `artifacts/popup-drawing-hold/`: `before/`, `tests/`,
  `final-ready-animated/`, `final-ready-expressive/` and
  `final-ready-gallery-1000/`. Intermediate native keyboard attempts lost
  foreground to LDPlayer; those attempts are not passes. The final runs completed
  after the user made the desktop available. No other application was changed.

Reproduce the Expressive native scope after the usual Debug sample refresh:

```powershell
dotnet run --project winforms/samples/RibbonKit.WinForms.DesignerSample -c Debug --no-build -- --verify-dropdown-content-expressive winforms/artifacts/popup-drawing-hold/review
```

Actual Visual Studio debugging/user acceptance and the earlier physical-input,
external accessibility and mixed-monitor limits remain separate. No WPF changes,
commit or publication were made.
