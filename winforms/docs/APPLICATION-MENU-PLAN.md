# Native Application Menu

Status, **2026-11-03: user accepted the current native Application Menu appearance
and interaction, including hover pane ownership, split hover/selection fills,
heading bands and framed/background treatment. Focused checks and package/native
verification passed.
The broader Release suite is not clean; see the evidence below. Geometry
consolidation is next; its [starting prompt](NEXT-SLICE-PROMPT.md) is prepared**.
The preceding state-command popup correction is user-accepted. The current
Application Menu contract and implementation evidence are recorded below.

The user clarified that Application Menu should replace the File button's simple
command-dropdown presentation. At the implementation baseline, native File had CommandMenu
and Backstage choices; CommandMenu was an ordinary dropdown over `FileMenu.Items`
without a dedicated Application Menu designer. This is separate from dropdowns
and split buttons used elsewhere in the ribbon.

## Agreed direction

- Use the existing File-menu path for a floating, two-pane Application Menu.
  Reuse the authored File commands and their event/command identities. Preserve
  compatibility with the existing `CommandMenu` enum/serialized preference and
  shipped public API; replacing the presentation does not require deleting that
  value or adding a third independent File presentation. Backstage stays the
  other choice.
- Define main command/navigation, right-pane content and footer actions before
  implementation. The application owns document/recent-file data and commands;
  the library owns reusable presentation, navigation, popup lifetime and host
  slots. Reuse existing menu/dropdown infrastructure where suitable.
- Add a dedicated designer workflow for Application Menu, including collection
  editing, selection/properties and an inert preview. Establish generated-code
  defaults/reset, retained custom content ownership, Apply/Cancel/no-op behavior,
  Undo/Redo and save/reopen before adding APIs.
- Cover keyboard/KeyTips, split/submenu commands, scroll/reveal, focus return,
  dismissal and accessibility alongside normal pointer use. Keep native hosted
  controls live, and account for Solid/Mica/Acrylic, density and DPI.
- The final additive API/layout/designer contract is recorded below. Orb
  variants, additional themes and a broad geometry settings API are later work.
- The user authorized using the WPF control as a dimension/layout reference and
  making better native choices where justified. Read `RibbonApplicationMenu.cs`,
  `Controls.ApplicationMenu.xaml`, `ApplicationMenuGeometryConverter.cs` and the
  Office 2024 tokens as references only. Current Office 2024 token starting points
  are a 150-unit navigation column, pane width capped at 340, 40-unit rows and a
  36-unit footer, in 96-DPI logical units. Translate the intent through native
  measurement, Compact/Touch metrics and work-area constraints; do not transplant
  WPF templates or impose those values regardless of content/DPI.

Application Menu's columns, content slots, footer, scrolling and popup placement
need to exist before their measurements enter geometry consolidation. Sequence:
state-command popup fix and review Ã¢â€ â€™ Application Menu and review Ã¢â€ â€™ geometry
consolidation Ã¢â€ â€™ additional themes. The implementation contract and evidence below supersede the original
planning-only checkpoint.

## Native contract presented before implementation - 2026-11-03

Baseline: `codex/Ribbonkit-Winform`, `779f529b83de8835af95850eced5a8794d309c80`.
Preserve the three existing documentation changes. Implementation is confined to
`winforms/`; WPF remains a read-only layout/behavior reference.

`CommandMenu = 0` retains its enum and serialization identity. `FileMenu.Items`
continues owning left commands. A stable owned `ApplicationMenu` adds a stable
`DefaultPane` (a `RibbonDropDownItem`) and owned `FooterItems`. Dropdowns in the
left column select a right pane using their existing Items, DropDownHeader and
Content/ContentType; split primaries invoke and their arrows select. Right-pane
nested dropdowns stay normal submenus. Leaf actions close and invoke once.
Explicit FileMenu.Content/ContentType remains the compatible whole-body override.
Empty/default panes are valid. Selection is transient and starts at DefaultPane.

Reuse the retained content contract: explicit native Controls are borrowed;
public parameterless UserControl types are initialized before Form.Load, owned
by their declaration, retained between pane switches/reopen and disposed on
replacement/reset/declaration disposal. Never copy/recreate HWNDs. Native page
validation vetoes pane changes/actions. Forced structural/lifetime teardown
still closes. Creation exceptions propagate and must not leave a popup session.
Designer previews never construct application content, execute commands or bind.

Start from WPF 150/340/40/36 logical units for navigation/pane/row/footer.
Measure native fonts and icons, enlarge Touch rows/arrow targets, cap to work area
and keep navigation, pane and footer scrolling separate. All consumers of bounds
(paint, pointer, keys, KeyTips, UIA and popup placement) share the measured result.
Reuse shared menu colors/renderer, popup frame/shadow/motion and reduced motion.
No broad geometry/theme API is introduced.

Edit Application Menu is a dedicated draft/tree/Properties/inert-preview editor.
Existing components retain identity, events, bindings and custom content. Cancel
and no-op do not mutate; an effective edit uses one designer transaction and
nested/root change notifications. Generated code uses stable content collections,
ContentType defaults/reset and native component names. Save/reopen and Undo/Redo
have explicit automated scopes; actual VS review remains separately reported.

Stop for review after this feature. Geometry consolidation and themes remain
next work and are not authorized by this implementation contract.

## Implementation and native differences

- `ApplicationMenu` and `DefaultPane` are retained owned content models, with
  no third File mode and no changes to `PublicAPI.Shipped.txt`. The intentional
  additive API is the model, its default pane/footer, the ribbon property and
  `DrawApplicationMenuBackground`. Existing commands, IDs, bindings and callbacks
  remain on their original models. Existing File Content/ContentType is the
  whole-body override; Backstage stays independent.
- A single layout result owns both command columns, optional pane heading,
  footer, scroll offsets and target clips. Navigation measures captions/icon/
  arrow/scrollbar space from a 150-unit starting width, capped at 260. Pane width
  is capped at 340. Navigation rows start at 40, plain pane rows at 28 (Touch 44),
  and illustrated choices grow for 32-unit artwork and measured descriptions.
  Footer rows start at 30 (Touch 44), wrap, and use a separately
  reserved scroll viewport when needed. Large font/narrow-work-area tests cover
  reachability without requiring every caption to fit untruncated.
- File budgets height on the larger usable side of its anchor, rather than
  consuming the entire monitor height and covering the File header. Existing
  popup placement/frame/shadow/motion and reduced-motion fallback are reused.
- Pane selection follows WPF's main-navigation hover ownership, plus existing
  click, arrow, Right and KeyTip activation. Hovering a plain command restores the
  default pane; gaps, separators, disabled rows and pane/footer transit are neutral.
  Native validation can veto hover switches and default resets. Every opening
  starts with the default pane. Leaf and split-primary actions close before invoking;
  nested right-pane menus keep the existing stack and Escape ordering.
- App-owned native controls remain borrowed, with their own appearance/layout.
  The library's content viewport uses the shared popup background, clips and
  scrolls the real body, and detaches it before disposal. It does not reset a
  UserControl's DPI-scaled Size on detach or recreate its HWND. Validation can
  veto switches/actions/Escape/outside dismissal; forced model teardown closes.
- The dedicated editor is available through the ribbon smart tag and property
  editor. Its tree separates left commands, default pane and footer, supports
  nested menus and separators, component names, scalar properties, content type
  selection and icons. Preview clicks select draft entries; native content is
  always a placeholder, including borrowed bodies and the File whole-body override.
  It never invokes commands, constructs app UserControls or reparents app content.
- OK applies one transaction. Cancel and selection-only/no-op create none.
  Existing components retain bindings/events/native content; source removal
  respects QAT references. Framework Undo/Redo includes default-header reset
  and structural additions. The default pane uses a flat, design-only nested
  site identity for reliable framework absolute serialization; normal generated
  code still uses `ribbon.ApplicationMenu.DefaultPane`. The accepted component
  is selected after the modal editor returns, so VS does not restore its old
  Properties owner. Actual VS interaction remains a distinct gate.
- Native accessibility exposes measured menu rows, split arrows, disabled/
  checked/focus/expanded states, scroll reveal and the real native content region
  through WinForms' HWND/MSAA bridge. External UIA/screen-reader certification
  remains open; the same-process public accessibility checks are not that gate.

## Verification - 2026-11-03

- Normal native Debug and Release solution builds passed with **zero warnings
  and errors**, including XML/public API analyzers, designer server, net472 client
  and both protocol targets. The shipped public API file is unchanged.
- `application-menu-focused.trx`: **96/96 focused Debug cases passed**, including
  Application Menu, File lifecycle, retained dropdown bodies, popup designer/menu
  presentation and all 24 accepted state-command popup cases. Evidence is in
  `tests/RibbonKit.WinForms.Tests/TestResults/`.
- `application-menu-final-contracts.trx`: **32/32 focused Release cases passed**
  after final runtime measurement/accessibility changes, including **24 new
  Application Menu/designer cases**, QAT group reuse and File designer settings.
  `application-menu-designer-final.trx` covers the later designer-only placeholder,
  selection and outside-ID reservation refinements. A final ordinary-popup
  validation-boundary correction adds one regression: it keeps Application Menu
  validation from changing ordinary custom dropdown dismissal. After that change,
  `application-menu-validation-boundary.trx` passed **66/66 affected Debug cases**
  and `application-menu-final-release-scoped.trx` passed **25/25 Application Menu
  Release cases**. These selections overlap; do not sum their counts.
- The initial aggregate found a popup-toggle guard regression and the expected
  designer-label change. Both were corrected. The final broader Release run,
  `application-menu-release-final.trx`, was **1136 passed / 6 failed / 1142 total**.
  All 24 new Application Menu cases passed in that aggregate. It is not a clean
  full-suite pass and predates the last designer-only refinements, the ordinary-
  popup validation boundary correction and its additional regression.
- A read-only archive of original HEAD `779f529` was built outside the checkout
  with normal Release outputs. In `artifacts/application-menu/baseline/`, the
  original HEAD reproduces the material-on-open-Backstage opacity failure,
  immediate Backstage page-motion assertion and group/minimized customization
  context failure. A second matching selection also reproduces the minimized
  Simplified group-motion failure. Group motion varies by run/order: the first
  baseline selection passed both variants, while the current focused retry
  failed both. The tree-hover raster case passed focused on both versions after
  failing the current aggregate. These findings leave the aggregate gate open;
  they do not justify modifying accepted group/Backstage behavior in this slice.
- `eng/Test-DesignerProtocol.ps1` passed registration of all three unique
  endpoints in the **actual VS 18.10.0.0 client registry/composition runtime**.
  This checks loading/protocol registration, not opening the editor inside VS.
- `eng/Test-ApplicationMenu.ps1` restores a new public-only consumer outside the
  checkout with an isolated package cache, compiling only the bounded Application
  Menu fixture and its entry point. It references no Showcase code/resources or
  internals. Package inspection verified runtime/XML and client/server/protocol
  design payloads. This scoped consumer does not depend on unrelated legacy
  consumer fixtures that currently use internal-only members.
- **12/12 public-package native scopes passed at actual 120 DPI:** Office and
  neutral renderer x Solid/effective Mica/effective Acrylic x Compact/Touch.
  Scopes include pointer opening/footer/outside dismissal, F10/File/pane KeyTips,
  native editor message input, same HWND/text after switches/reopen, disabled
  accessibility state, scroll reveal with the footer still visible, split
  state-before-Click/one invocation, and right-pane submenu Escape ordering.
  Material scopes explicitly verified `EffectiveBackdrop`; Solid fallback was
  not counted as Mica/Acrylic. Evidence: `artifacts/application-menu/native/`.
- **36 actual captures** (default/native/Save panes) were saved by those scopes.
  Representative Office Solid Compact, Office Acrylic Touch and neutral Mica
  Touch captures were inspected, including column/footer clips, native content,
  popup anchoring and frame corners at actual 120 DPI. This is fixture evidence,
  not user approval or a physical multi-DPI campaign. The captures prompted the
  final anchor-height and content-viewport-background corrections. The complete
  12-scope matrix was repeated against the final package after the ordinary-popup
  validation boundary correction.

## Initial implementation checkpoint

The initial permanent Designer Sample checkpoint used local package
**`0.1.0-designer.local20261103T112055535Z`**, refreshed in place in normal Debug.
That checkpoint's 12-scope native matrix used this same package. The package entries are
recorded in `artifacts/application-menu/package-contents.txt` and the final audit
is `artifacts/application-menu/final-package-audit.json`.
`--verify-application-menu` passed against this package: its generated declarations,
pre-Form.Load ContentType initialization, one body per source, retained HWND/text,
split pane and footer event/dismissal all passed. Evidence is
`artifacts/application-menu/designer-sample/result.log`.
A normal Release local package was also built and packed successfully.

The refresh kept
its authored Forms, UserControls, resources, Backstage startup choice and accepted
gallery/editor examples. It now also declares File panes, a split Save command,
recent documents and footer actions. Choose **File opens -> Application Menu**
and **Edit Application Menu...** to review. Showcase starts in Application Menu
and provides a Backstage/return action through public APIs.

Pending: actual VS editor interaction, Properties selection, Undo/Redo and
save/reopen; external UIA/screen readers; physical keyboard/touch/IME, RTL and
actual display/mixed-monitor DPI beyond the fixture's 120 DPI; human visual and
usability acceptance; and resolution of the broader aggregate failures above.
No WPF changes/checks, geometry consolidation, additional themes, commit, push or
publication were performed.

## Bounded visual refinement checkpoint - 2026-11-03

Starting baseline: `codex/Ribbonkit-Winform`,
`f633aec3952b7dc2681f91110945d7aa571e50c1`, clean checkout. The user's WPF screenshots
showed the native sample was too bare. The visual contract was presented before
editing: preserve the public model, designer protocol and retained content;
scope layout/paint to Application Menu using shared appearance roles; enrich the
permanent sample and Showcase through public declarations. WPF remained read-only.

Requested-fix checklist, implemented and awaiting human review:

- Continuous rounded inner border around both columns, aligned divider and pane
  insets. The inner fill and row artwork are clipped inside the border so all
  four corner strokes remain intact. Scroll-stress capture exposed GDI text
  escaping a viewport; Application Menu text now preserves that clip too.
- Large navigation/illustrated icons, accent bold headings with a rule, compact
  recent-document rows and measured rich choice descriptions. Native font, Touch
  and work-area constraints take precedence over WPF reference dimensions.
- Right-aligned framed Options/Exit footer actions and accent-filled open File.
  No new theme, geometry settings or public API was introduced.
- Split primary/arrow targets share top/bottom bounds. A reproduced paint mismatch
  made the arrow two device pixels taller at 120 DPI; both sections now clip one
  inset perimeter. Open/selected state is separate from actual pointer press:
  an unhovered selected primary blends toward the popup background, while the
  arrow keeps the darker open fill. The later WPF-hover follow-up below supersedes
  this checkpoint's independent-half hover treatment.
- Comparable New/Open/Save/Save As/Print/Prepare/Send/Publish/Close examples,
  numbered recent documents and illustrated format choices. The Designer Sample
  preserves original Open/native-body, split/event identities, Forms/UserControls,
  resources and Backstage startup choice. Hosts still own icons/data/commands.

Final focused Release evidence is `application-menu-visual-frame-final-release.trx`:
**63/63 passed**, covering Application Menu/designer, File lifecycle and ordinary
split chrome. It includes **32 appearance cases** for both renderers, Compact/Touch,
inner-frame corner strokes and split paint at synthetic 96/120/144/192 DPI,
footer/row/icon measurement and opaque popup text clipping. Earlier overlapping
visual selections are retained, not added to this count. Normal native Debug and
Release solution builds and local Release packing passed with zero warnings/errors.
The broader full suite was not repeated; its earlier six failures remain open.

The permanent Designer Sample was refreshed in place to
**`0.1.0-designer.local20261103T131408521Z`** in normal Debug. Its generated-code,
pre-Load native-content initialization, retained HWND/text, split pane and footer
event/dismissal check passed. Actual default/native/Save As captures are in
`artifacts/application-menu/visual-refinement/designer-sample/`.

The final package-only native matrix passed **12/12 scopes** at actual 120 DPI:
Office/neutral renderer x Solid/effective Mica/effective Acrylic x Compact/Touch.
Its **36 captures**, package inventory and consumer path are under
`artifacts/application-menu/visual-refinement/`. Final representative sample and
native material/Touch captures were inspected for framing, split fills/perimeters,
clipped overflow and retained native content. These are fixture checks, not actual
VS editing or physical multi-monitor/touch acceptance. The pending gates listed
above remain open. **Stop for user review before geometry consolidation.**

## WPF hover parity follow-up - 2026-11-03

The user's recording and Print screenshot clarified the intended interaction and
supersede the initial explicit-only pane-selection choice. The native contract
was presented before editing: change main-navigation hover ownership and shared
paint/layout while preserving public APIs, designer serialization, command
activation and retained native controls. Existing visual work was preserved.
WPF `NotifyItemHoverChanged` and physical split-template triggers were read-only
references; no WPF changes or checks ran.

- Entering an enabled navigation dropdown or either split half selects its pane
  immediately without invoking. Entering a plain command restores DefaultPane.
  Right-pane/footer transit, separators, disabled rows, gaps and row leave keep
  the current selection. Reopening still starts on DefaultPane.
- Both split halves use the same normal hover fill when either is hovered.
  With its pane selected and neither half hovered, the primary is subdued and
  the arrow is darker. Real press, command invocation and separate action targets
  remain. Hovering a selected ordinary dropdown also uses normal hover fill.
- Selected-pane headings use a shaded band spanning the right pane, clipped
  inside the inner frame. The default heading retains its inset rule treatment.
  Existing explicit heading/background roles still apply.
- The surrounding inset/footer matches the default pane background in both
  default and selected states; inner frame/corner strokes remain intact.
- Native validation vetoes hover switches/default resets. A successful switch
  closes obsolete nested submenus, retains native controls/HWND/text, and
  preserves navigation/footer scroll offsets. Hover does not focus a pane.

`application-menu-hover-parity-release.trx` passed **85/85 focused Release cases**:
Application Menu/designer, File lifecycle, split chrome, popup hover and color
roles. This includes 40 appearance cases and four new hover/lifetime/scroll cases;
it overlaps prior selections and is not an aggregate-suite claim. Debug/Release
native solution builds and Release packing passed with zero warnings/errors.

The permanent Designer Sample uses normal Debug package
**`0.1.0-designer.local20261103T135514205Z`**. Its generated-content/retention/split/
footer check passed and saved default/Print/native/Save As captures. The independent
public-package consumer passed **12/12 scopes** at actual 120 DPI: both renderers
x Solid/effective Mica/effective Acrylic x Compact/Touch. It now exercises real
pointer hover on dropdown/main/arrow, pane/gap/disabled transit and plain-command
default restoration, alongside the existing key/command/content/dismissal checks.

Evidence: `artifacts/application-menu/hover-parity/`, including 84 native captures,
four sample captures and the package inventory. Actual split main/arrow hover,
selected-unhovered, restored-default and material/Touch captures were inspected,
as were the sample Print/Save As bands. Synthetic paint checks cover 96/120/144/192
DPI; they do not replace physical display/Touch or mixed-monitor review.
Scoped whitespace review passed; the existing VS-generated `// ` comment-line
spacing in `DesignerForm.Designer.cs` was preserved as an explicit exception.
The earlier six full-suite failures and the pending VS/hardware/accessibility
gates remain open. The user review above covers the requested Application Menu
appearance and interaction, rather than those separate gates.
