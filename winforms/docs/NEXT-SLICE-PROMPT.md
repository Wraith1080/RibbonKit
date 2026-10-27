# Continuation prompt: native RibbonKit.WinForms M2E ScreenTips

Continue the native RibbonKit.WinForms implementation in:

`C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

This handoff was prepared on **2026-10-27**. Implement the next bounded component
slice, **M2E ScreenTips**. Begin with a short description of the proposed public
API and interaction contract, then carry the slice through implementation and
appropriate verification. Resolve routine choices without another approval.

## Working agreement

- Start with `git status --short`; the existing branch is
  `codex/Ribbonkit-Winform`. Preserve all existing tracked/untracked work,
  including `MainForm.resx`. The earlier slices are present in this dirty checkout;
  do not reset, clean, stash or recreate them.
- Read root `AGENTS.md` and `winforms/AGENTS.md`. Work with one agent. Native code,
  samples, designer and tests live under `winforms/`. WPF runtime/build/CI stay
  unchanged; WPF is a behavioral reference, not a dependency.
- No commit, push, publication, branch switch or new worktree. Use the normal
  Debug output for Showcase review and normal Release outputs for required checks.
- Keep reusable features in the native library. Showcase supplies commands,
  content, icons and data; another ordinary Form must attain the same behavior
  through public APIs without Showcase resources, helpers or internal access.
- Accessibility/Narrator compatibility is explicitly deferred while structure
  is implemented. Preserve the existing foundation bridge, but do not add new
  providers, patterns, screen-reader work or accessibility acceptance gates.
  Animation, native touch/IME tests, Mica and custom window chrome are also outside
  this slice. Actual VS authoring and additional physical DPI/monitor gates remain
  separate; don't claim them from automated/offscreen checks.

## Read the current plan in bounded sections

- `docs/21-NATIVE-WINFORMS-PLAN.md`: current banner, native model/rendering
  contracts, §6 roadmap and "Deferred theming consolidation".
- `winforms/docs/M2-STATUS.md`: opening status/next-slice section, M2D options,
  checkmark centering and body-scroll-button follow-ups. Consult other sections
  only when needed; the ledger contains superseded historical test counts.
- `winforms/README.md`: caption fitting, check/radio APIs, renderer customization,
  and verification/package-consumer guidance.
- `winforms/docs/M1-STATUS.md`: only any open gate directly relevant to this slice.

## Current implementation and fixes to preserve

M1 foundation and M2A split buttons, M2B TextBox, M2C ComboBox/custom choice popup,
and M2D compact checkbox/radio models are implemented. Human review and designer/
hardware acceptance remain separately recorded; implementation is not full M1/M2
acceptance. Richer menu rows, minimize/Ctrl+F1 and optional ICommand remain later.

- Rendering uses `RibbonRenderer` as the neutral foundation and
  `RibbonOffice2024Renderer` as the default Office presentation. Both share
  measured geometry and public drawing hooks.
- Large labels use at most two compact rows and ellipsize within
  `RibbonTheme.LargeButtonMaxWidth` (108 logical pixels by default), preserving
  the complete model `Text`. Wrapped vertical dropdown/split arrows sit beside
  the second row. Keep the fixed ribbon body height and split joined-edge fixes.
- Dropdown arrows are 8 x 6 logical pixels. Body-scroll chevrons are 6 x 10 with
  DPI-scaled strokes. Body scrollers use `PopupBorder` at rest, accent borders on
  hover/press and uniform default clearance. Their public theme setting is
  `BodyScrollButtonVerticalInset`: default 2, zero for full height, negatives
  rejected, oversized values clamped. Tab-strip styling is separate.
- Office popups have rounded frames and soft shadows; neutral popups remain
  square. ComboBox choices use the shared popup treatment and retain native
  ListBox scrolling/search. Disabled commands reject interaction state and
  execution; existing popups dismiss when their host deactivates.
- Keyboard focus outlines appear for keyboard navigation. Pointer clicks should
  not leave keyboard outlines throughout the ribbon.
- Checkbox/radio marks were centered optically on both axes. Three-state
  `CheckState` bindings use `formattingEnabled: false`; radio peers clear before
  publishing the new selection to avoid a BindingSource rollback.
- Preserve editor/startup flicker repairs: painting does not synchronize or
  reposition native editors, reset selection or change layout. Native ComboBox
  placement does not repeatedly force its preferred height. Avoid redundant
  child-HWND movement and selection updates.
- Rounded rendering uses the mirrored analytic painter. Inspect actual corner
  pixels and edge joins at odd/even bounds and representative DPI; neither GDI
  helpers nor a shared path proves symmetry.

## Next slice: ScreenTips

Keep the first version bounded and library-owned:

1. Propose a small, ordinary WinForms/designer-friendly metadata API for a title
   and optional description. Reuse the full item caption where appropriate,
   including a fallback for truncated Large labels. Keep metadata separate from
   command state and never replace model `Text` with the displayed ellipsis.
2. Start with delayed, passive hover tips. Cover representative buttons,
   dropdown/split items, check/radio items and native editor items, on the ribbon
   and library popup surfaces. Avoid inventing separate per-half tip APIs without
   a concrete need. Disabled-item descriptions may be shown passively without
   enabling hover/press/activation visuals or command execution.
3. Tips must not activate, take focus/capture, execute commands or disrupt native
   text selection. Cancel stale delayed shows and close/reposition consistently
   on pointer changes, command/menu activation, keyboard interaction, host
   deactivation, layout/DPI changes, model removal and disposal. Preserve the
   existing nested-popup Escape/focus behavior and work-area bounds.
4. Use existing shared appearance/popup primitives where suitable, with renderer
   customization available to another consumer. Keep the implementation free of
   Showcase-only helpers and new global coordinators. Images, help-link actions,
   pinned/interactive tips and broader help services can remain subsequent work.
5. Demonstrate a truncated full caption, an authored description, a disabled
   command and a popup/editor example. Document actual supported scope and any
   remaining gap instead of claiming universal coverage.

## Theming decision: explicitly deferred

The user approved comprehensive theming **after most main component slices**,
before final visual polishing and native API stabilization. Do not start it in
this slice. Keep appearance in shared renderers and logical spacing in shared
layout; note new constants for the later inventory. That later pass owns state
colors/fills, borders, padding/margins, independent corner radii, shadow
blur/opacity/offset, typography and indicator metrics, with Office/neutral
coverage. Backdrop blur/Mica remains optional later host integration.

## Verification checkpoint and completion

Latest recorded evidence: **62/62 Release unit/STA tests passed**; normal Debug/
Release solution/API/XML and fresh isolated package restore/build passed without
warnings/errors. Both shown Office/neutral package-consumer scopes passed on the
actual **120 DPI (125%)** display, including scroller frames/spacing, gap clicks,
scrolling and runtime inset changes. Synthetic rendering covers 96/120/144/192
DPI and odd/even bounds. These are prior results, not a fresh acceptance run for
the next slice. Evidence is in the ledger's latest body-scroll section and ignored
`winforms/TestResults/body-scroll-refinement-*` files.

Extend existing essential tests where useful; add cases only for distinct failure
modes. The suite was intentionally reduced from over 100 cases. Validate delayed
tip cancellation/lifetime, full-caption behavior and focus/editor preservation,
plus affected geometry/rendering. Follow the native API/XML/Unshipped and required
Release checks for any public additions. Rebuild the ordinary Debug Showcase.
Verify shared behavior in a fresh isolated package consumer, with Office and
neutral presentation, not only the Showcase. Keep native-input foreground guards;
do not send input after the validation window loses ownership. Do not close a
user-owned Showcase instance without authorization if it locks the outputs.

Record concise results/limitations in the M2 ledger and public usage in the native
README. Separate automated, shown native, synthetic DPI and human/hardware
acceptance. Once the necessary checks pass, do not rerun them without a new reason.
