# Native WinForms: Office 2019 Light review

Updated **2026-11-10**. The [native themes plan](THEMES-PLAN.md) owns the selected
first theme, proposed contract, implementation slices and future acceptance.
**Implementation is delivered for user review; dark variants remain deferred.**

Workspace: `C:\Users\LENOVO\Claude\Projects\Professional Ribbon Custom Control for WPF`

## Starting point

- Branch at planning entry: `codex/Ribbonkit-Winform`.
- Planning baseline: `0ddf85be0ca6b0f502543726f8612050b06c0991`, clean at entry.
  Recheck live branch/HEAD/worktree before implementation; preserve later edits.
- The catalog offers Office 2024 Light and Office 2019 Light. The themes plan
  owns current evidence, limits and the pending appearance review.
- Previous geometry and QAT/tab-header correction evidence and review results
  remain in the [geometry plan](GEOMETRY-CONSOLIDATION-PLAN.md). No repeated review,
  or geometry expansion is needed for a theme review.

## Review delivery

Reopen the existing Designer Sample solution and launch the normal Debug output
with `--office2019-theme-demo`. The sample was refreshed in place; its package
identity and results are recorded once in the themes plan.

1. Compare Office 2019/Office 2024, Application theme and Custom 2019 accent.
2. Compare blue/neutral title bars: 2019 extends color into the tab band; the
   selected tab remains white. Check File, contextual tabs, QAT and utilities.
3. Check square corners, the full-width body with inner padding, fine separators,
   and popup borders/shadows. Try both densities/layouts and narrow widths.
4. Open existing galleries/Backstage and check retained native content. In actual
   VS, separately review selection, previews, Save/reopen and Undo/Redo.

Record confirmation in the themes plan's existing review row. Do not refresh the
package or repeat builds/tests for a routine acceptance. Further theme families
require a later bounded instruction after this review.

## Preserved boundaries

Use one agent and work under `winforms/`. Use normal Debug/Release outputs and the
permanent `samples/RibbonKit.WinForms.DesignerSample` for review. Preserve authored
Forms/UserControls/resources, application page styling and later user edits. Leave
actual Visual Studio review to the user; no computer-use tools. No WPF
edit/build/test, WPF dependencies, commit, push, publication, branch switch or
destructive worktree cleanup.

Preserve command ownership/state-before-Click/single invocation, QAT sources and
placements, native field values/selection/handles, gallery presenter ownership,
Application Menu joins/hover/default-pane behavior, popup retention and accepted
Large-command shared bands. Themes must use the shared library appearance and
geometry contracts, with no required Showcase helper or renderer assignment.
