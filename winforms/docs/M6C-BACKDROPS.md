# M6C native window backdrops

Status, **2026-10-30: deferred by the user after unstable visual behavior**.
The user accepted M6B's final frame/group/Backstage/editor motion fixes on
2026-10-29 and authorized M6C. During review the user expanded the backdrop to
the form background and explicitly retained the opaque main ribbon body.
The user prefers the stable pre-M6C ribbon, will handle branching, and wants this
experiment revisited after the remaining work. M6C has not received visual
acceptance; passing focused checks below do not override that decision.

## Deferral and resume checkpoint

- **Stable reference:** `c8515a2` (`Flickering Fix`), the committed pre-M6C HEAD
  observed at deferral on `codex/Ribbonkit-Winform`. M6B's accepted frame, width-group,
  Backstage and editor-motion fixes are the baseline to preserve.
- M6C work was still tracked/untracked local changes at this checkpoint. The user
  owns the experimental branch creation/name and the return to the stable checkout.
  No branch switch, commit, reset, stash or source rollback was performed for this
  documentation update. Recheck live Git state when resuming; do not assume the
  branch or dirty state remains the same.
- Keep the backdrop API, opaque GDI host, native editor/Backstage composition,
  motion masking/capture and caption-repaint follow-up together as the experiment
  for now. The caption fix's isolated test pass does not establish acceptance of
  the whole rendering path or authorize promoting it into the stable baseline.
- **Outstanding symptom:** the Showcase document intermittently flickers during
  ribbon/Backstage animation, more likely with pointer movement. Controlled captures
  missed the visible blink, including before the caption repair. Its visual cause
  and resolution remain unconfirmed. Native form/background redirection and the
  new layered/composited content host remain investigation areas, not proven causes.
- **Required appearance on a later revisit:** native material in exposed form and
  ribbon background; the main ribbon body remains opaque. Native fields, Backstage
  pages and document content need opaque readable surfaces. Full translucent
  command panels were explicitly declined.

When the user explicitly resumes M6C, begin with a bounded reproduction against
the stable reference and the experimental branch. Include pointer sweeps across
caption/ribbon/document during tab, minimize/density and Backstage open/close
transitions. Inspect intermediate composed frames and paint/erase activity, then
isolate one rendering change at a time. Preserve real native editor identity,
validation, focus and values; a settled screenshot or logical HWND rectangle alone
does not prove freedom from flicker. Do not continue feature expansion first.

Useful implementation entry points:

| Area | Sources |
| --- | --- |
| Native material and GDI content | `RibbonForm.Backdrop.cs`, `RibbonBackdropPanel.cs`, `Rendering/RibbonBackdropBuffer.cs` |
| Ribbon paint, native editors and Backstage | `RibbonControl.Window.cs`, `RibbonControl.cs`, `Editors/RibbonEditorManager.cs`, `Backstage/RibbonBackstageSurface.cs` |
| Drawing/opacity coordination | `Animation/RibbonDrawingHold.cs`, `Animation/RibbonGeometryMotion.cs`, `Animation/RibbonMotionScene.cs` |
| Actual consumer and diagnostics | `samples/RibbonKit.WinForms.Showcase/MainForm.Backdrops.cs`, `samples/RibbonKit.WinForms.Showcase/MainForm.BackstageMotionCapture.cs`, `tests/RibbonKit.WinForms.Consumer/WindowBackdropChecks.cs`, `tests/RibbonKit.WinForms.Tests/WindowBackdropTests.cs` |

Runtime paths in the table are relative to `winforms/src/RibbonKit.WinForms/`;
sample/test paths are relative to `winforms/`. The API remains an unaccepted
Unshipped proposal on this experimental implementation.

`winforms/artifacts/` captures and test `TestResults/` are ignored local evidence;
the branch does not carry those files. Preserve any needed raw captures separately
when moving the experiment; the scope/result summaries below are the durable
record. Await completed builds before launching a diagnostic, run one native UI
check at a time, and close the Showcase before rebuilding its normal Debug output
to avoid DLL locks. Foreground-guard aborts and interrupted runs are not passes.

## Public contract and ownership

- `RibbonForm.Backdrop`: `RibbonWindowBackdrop.None` (default), `Mica`, `Acrylic`.
  Native material fills exposed form/ribbon background. The main command body,
  messages, popups, native editors and Backstage pages retain opaque surfaces.
  The authored theme's Background and Surface are unchanged.
- `EffectiveBackdrop` reports the setting successfully applied to the current
  handle; unsupported/unsafe contexts retain the preference with solid fallback.
  Windows may itself render an applied material as solid according to OS policy.
- `BackdropChanged` reports requested-preference changes. The application can
  adjust its own content margins. No host handler completes library animation.
- `RibbonBackdropPanel : Panel` is an explicit opaque content surface for native
  GDI application controls over material. It composes its native child drawing at
  alpha 255, retaining native input, handles and ownership. The application owns
  the panel, its controls, data and layout. Use an opaque BackColor and the Windows
  8-or-later layered-child compatibility manifest already required by motion.
- RibbonForm owns DWM setup, handle/system/DPI refresh and cleanup after opt-in.
  It does not change application Opacity/TransparencyKey, reparent application
  content or assume ownership of icons, commands, persistence or DPI startup.
  Plain Forms remain supported. No WPF hosting, screen-capture blur, undocumented
  composition attributes or broad Glass/theme campaign was added.

The documented [DWM backdrop choices](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwm_systembackdrop_type)
require Windows 11 build 22621. See the
[custom-frame alpha guidance](https://learn.microsoft.com/en-us/windows/win32/dwm/customframe).
Unsupported OS/design contexts, high contrast, non-top-level/borderless/layered
Forms, application opacity/key and drawing failures use solid fallback.
Drawing is bounded to 4,194,304 pixels; a failed paint retries after a new
preference or handle. Full native OS-policy/contrast-theme compatibility is not claimed.

## Native composition and motion decisions

The header-only candidate was expanded at the user's request. Its acceptance
evidence is retained as diagnostics; it does not establish the full-form result.
The translucent-command-panel experiment was removed when the user clarified
that only the surrounding ribbon background should show material.

The library paints material-aware ribbon pixels through one reusable top-down
32-bit DIB on the existing HWND. Composited theme text preserves black glyph alpha;
the graphics context must address the real top-down DC. An intermediate bitmap
HDC inverted some diagnostic labels. Window paint uses transparent Background
without changing the opaque Surface role or the authored immutable theme.

GDI content needs an opaque composited surface. Native editor presenters use an
owned alpha-255 hold; Backstage upgrades its existing stable opacity surface.
Application content uses the public panel. Native styles are borrowed only after
the HWND belongs to the current native root; parked handles during Form recreation
are deferred. Reapplying native material refreshes panel drawing so GDI text does
not remain as a black rectangle. No native editor handles are recreated to animate
or change material.

Layering the whole ribbon blackened the DWM material. Material mode keeps the
caption/tab strip live, committing its geometry/indicator immediately. During
geometry playback the library masks only real body drawing and lowers its already
composited editor surfaces to alpha 1. An inert drawing covers the committed body;
real drawing/alpha 255 returns before the overlay is removed. Existing body/group,
popup and Backstage motion policies remain. Solid retains its full header motion.
Material snapshots copy the original DIB alpha directly; printing through a bitmap
HDC otherwise converted transparent pixels into opaque black. Captured taller
headers/QAT strips are excluded from material body motion during density changes.

Showcase consumes these public APIs. It places its existing document/status
controls in the shared panel and owns transparent surrounding margins. View /
Window / Backdrop selects Solid, Mica or Acrylic. Appearance is not persisted by
the library or included in ribbon customization JSON.

## Document pointer-repaint follow-up - 2026-10-30

The user reported intermittent document flicker during ribbon/Backstage animation,
more likely while moving the pointer. The main Showcase document is a native
multiline TextBox in this checkout. Controlled samples did not reproduce a visible
blink, so sampled success is not a claim that every intermittent case is closed.

A concrete defect was reproduced: repeated nonclient caption motion invalidated
the entire client, including the document, even when hover state was unchanged.
Caption title/activation/button/hover updates now invalidate only the measured
caption bands, and unchanged hover does not invalidate again. The regression
failed **2/2** before and passed **2/2** after in
`m6c-caption-hover-before.trx` / `m6c-caption-hover-fixed.trx`.

`artifacts/m6c-document-hover-fixed/` captures controlled pointer sweeps across
caption, tabs and document, tab motion with pointer movement, and Backstage close.
All five scopes have zero sampled visible document changes. Before-fix samples
also happened to be stable; these captures support current behavior, while the
invalidation regression proves the bounded repair. Human visual review remains open.
The last two full-form density capture attempts stopped when foreground ownership
changed (`m6c-full-opaque-density-final` and `m6c-full-opaque-density-retry`). They
are not passes. The focused density raster and width-playback evidence remains
separate; no further input was sent after the capture guard stopped the run.

## Focused evidence

- Normal native Debug solution builds passed with zero warnings/errors, including
  API/XML and the separate design assembly. Additions are in Unshipped; shipped
  baselines were not edited.
- Full-form focused tests in `m6c-full-form-tests.trx` initially passed **18/20**.
  The density raster assertion was corrected to distinguish translucent shadow
  from opaque black and passed in `m6c-full-form-tail-fixed.trx`; the Backstage
  fixture was configured to actually open File/Backstage and passed **1/1** in
  `m6c-open-backstage-fixed.trx`. These are separate repaired scopes, not one clean
  aggregate. They include the requested opaque-body/transparent-surround contract,
  native text alpha, policy/metadata, composition restart, width playback and
  retained editor identity.
- `m6c-full-form-related.trx`: **33/33** affected Backstage, TextBox, ComboBox and
  RibbonForm checks. Earlier `m6c-related-motion.trx` retains **65/65** geometry,
  minimize and previous window-motion scopes; selections overlap.
- At actual **120 DPI on Windows build 26300**, public-only Office and neutral
  checks passed all three choices in Compact/Touch in
  `artifacts/m6c-full-office-native-settled/` and
  `artifacts/m6c-full-neutral-native-settled/`. They assert that Backstage actually
  opens, native handles/values and authored state survive, application GDI text
  remains dark over a white surface, native opacity/key/border fallbacks recover,
  and handle recreation/maximize/restore work. Native window captures wait for
  compositor stability, not just committed HWND bounds; early OS zoom/restore
  frames produced false pixel failures and remain diagnostics.
- `artifacts/m6c-full-simplified-parked-field/` adds the public-only Simplified
  path. It establishes handles in Classic before switching: the TextBox remains
  parked/retained while the supported ComboBox is inline. This does not claim
  inline TextBox support.
- Actual Showcase full-form captures in `artifacts/m6c-opaque-body-showcase/`
  passed all material/density combinations with real Inputs/Info/Print pages,
  tab/minimize intermediates and retained native editor handles/data. Images
  confirm an opaque white main body with material around it and the content card.
- `artifacts/m6c-full-form-interactions/` passed actual Back hover/click, native
  name commit/font choice and physical width drags under Acrylic in both densities;
  group playback was sampled in both directions. Typing/drag counts establish
  input and identity, not automatic pixel-stability acceptance.
- `artifacts/m6c-caption-native-input/` passed real caption drag, edge resize,
  maximize hover/click, restore, system menu, minimize, host fallback/reassociation
  and close after the caption invalidation repair.

Native runner/consumer `--backdrop-checks`, `--backdrop-checks-flat` and
`--backdrop-simplified-checks` take an evidence directory. Showcase diagnostic
switches cover real page, menu, density, pointer and interaction scopes separately
so passed prefixes need not repeat.

## Remaining limits

Human visual acceptance, physical mixed-monitor/DPI migration, other display
scales, actual older Windows/high-contrast/transparency-policy behavior, broader
theme/RTL/designer/accessibility compatibility and performance/resource budgets
remain separate. Actual new native-material appearance is not inferred from pure
policy or geometry tests. No WPF runtime/build, full suite, Release or package
campaign was run; the independent source is linked into the native runner rather
than freshly built from a package.
