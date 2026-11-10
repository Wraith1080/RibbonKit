# Native WinForms theme architecture

**Draft for discussion, 2026-11-10. The user requested planning before continuing
the broader theme rewrite. No further runtime changes or tests are part of this
planning step.**

The [themes plan](THEMES-PLAN.md) retains appearance requirements and acceptance.
This document proposes implementation boundaries and a migration sequence; it
does not accept the current visuals or schedule another theme family.

## Problem and intended result

Office 2019's Backstage changes altered Office 2024's caption coverage and removed
its animated neutral grey caption tint. Separating their settings alone did not
protect the established presentation. New themes need explicit ownership of their
visual differences while retaining the same control behavior and customization.

The proposed result is a small neutral foundation, sibling presentation classes,
and immutable theme definitions that compose those presentations with colors and
metrics. A new palette should reuse an existing presentation where appropriate.
A different visual structure should add a sibling implementation.

## Proposed ownership

| Layer | Owns | Boundary |
| --- | --- | --- |
| Theme definition | Palette, fills, logical metrics, and selection of presentation families. | Preserve `RibbonTheme`, built-in selection, application theme and Reset semantics. Keep settings independently adjustable. |
| Pure layout base | Space negotiation, DPI arithmetic and a measured result consumed by drawing, hit testing and native controls. | No Office-specific default padding, page frame, caption coverage or selection shape. Different structures can override measurement. |
| Pure presentation base | Drawing contracts and access to neutral primitives and existing application renderer hooks. | No Office 2019/2024 palette or visual defaults, and no inheritance from one Office family to another. |
| Family implementations | Header/selection treatment, frames, Backstage structure, caption treatment and style defaults. | Sibling classes with separate files. A change to one family has an explicit verification scope for the others. |
| Shared control/runtime | Commands, selection, keyboard/accessibility, validation, ownership, native page/editor handles and animation lifecycle. | Theme switching changes presentation without replacing application controls or mutating authored state. |
| Host integration | Native window controls, resize/drag, material composition, and applying the selected caption policy. | `RibbonForm` consumes a presentation policy; a family-specific effect must not become unconditional window behavior. |

Keep the existing neutral `RibbonRenderer` primitives and public virtual hooks.
Prefer a small pure Backstage presentation base over inheriting the complete
ribbon renderer for each Backstage style. Keep family implementations internal
initially; another application can still supply its public `RibbonRenderer` and
immutable theme settings. A new public extension contract needs a concrete
consumer requirement and a separate compatibility decision.

## Composition and selection

1. Resolve a theme definition once into presentation choices for headers/body,
   commands, popups, galleries, Backstage and library dialogs.
2. Preserve explicit application-supplied renderer precedence. Family helpers
   must continue invoking that renderer's text, focus, separator and metric hooks.
3. Keep layout and rendering selections together in one resolver/registration
   point. Avoid independent switches that can pair one family's layout with
   another family's drawing accidentally.
4. Keep palette changes independent from structure. A new color scheme can reuse
   Modern or Classic; a structurally different Backstage adds a sibling pair.
5. Keep current `RibbonTheme` properties as the compatibility surface. Grouping
   their internal resolved values is preferable to immediately renaming/removing
   public properties. Validate choices before assignment and retain per-ribbon
   isolation, custom clones and Reset.

The draft `RibbonBackstageStyle` selector is a possible first step, not a frozen
design. Review its naming, placement and resolver before expanding it to other
surfaces. Pure bases should expose contracts; registration belongs in the resolver.

## Visual contracts to protect

| Area | Office 2024 | Office 2019 |
| --- | --- | --- |
| Backstage placement | Below a separate title bar. | Real navigation and page panes extend behind a transparent caption foreground. |
| Active Backstage caption | Neutral grey derived from navigation background, sliding/fading with Backstage. An authored Form/theme caption color retains precedence. Closing restores the ordinary caption. | Pane backgrounds supply their own caption coverage; no duplicated colored-caption animation. |
| Title movement | Preserve established ordinary title placement. | Place 75% toward the destination, slide the final 25% without text fade on opening/closing; recenter immediately on resize/maximize/restore. |
| Navigation/page | Existing inset selections, indicator and page frame. | Flat accent rail, full-width state fills, white icon silhouettes and flush page. |
| Other surfaces | Preserve established defaults. | Square theme-owned elements, circular Back exception, edge-to-edge body with retained inner padding, bordered/shadowed popups. |

Animation code owns clocks, interruption, validation/cancellation, reduced motion
and lifetime. A presentation supplies distances and caption policy. Keep pane
motion, caption tint and title position on consistent progress where they must
present together; do not create unrelated color animations to imitate real panes.

## Migration slices

| Slice | Work | Completion evidence |
| --- | --- | --- |
| A. Baseline and contracts | Inventory existing appearance owners and document the two accepted design targets, caption precedence and current exceptions. Review the draft Backstage extraction against this architecture. | A small ownership map and retained reference scenes. Identify which existing checks protect each boundary. |
| B. Backstage pilot | Finish a pure base, one layout/renderer pair per structural family and one resolver. Include the Office 2024 grey-caption regression fix. Preserve shared native-page and motion behavior. | Only affected layout/rendering/caption/ownership tests, plus a focused package-only comparison of the two layouts. User appearance review remains separate. |
| C. Headers and body | Move family-specific tab/header/body presentation behind the same composition pattern, preserving geometry, QAT joins, scroller borders and clipping. | Affected header/body/QAT checks and a small comparison scene for each existing family. |
| D. Remaining surfaces | Migrate commands, popups/Application Menu, galleries, tips and dialogs in bounded slices. Extract an owner only where structure differs; retain reusable neutral primitives. | Checks for the changed surface and unchanged-family comparison. Preserve borrowed editors, presenters and icons. |
| E. Catalog growth | Add the next selected light theme through definitions and existing presentation families where possible. Add sibling classes only for actual structural differences. | Independent-consumer use and focused visual/state checks. Dark variants remain deferred. |

Do not migrate every surface in one patch or implement another theme during the
architecture discussion. Avoid prebuilding a new framework for hypothetical
themes; use the Backstage pilot to establish the smallest useful contracts.

## Verification and delivery budget

- Select tests by changed surface and behavior. Reuse existing evidence and avoid
  broad suites/native matrices during bounded slices, as requested by the user.
- Protect the unchanged family with focused layout/paint assertions and, when
  motion changes, a short real-frame check. Static screenshots alone do not prove
  synchronization, retention or interruption behavior.
- Test custom settings and application renderer hooks, along with family switching
  while a surface is open. Keep checks meaningful rather than mirroring helper code.
- Use normal Debug output for review and Release for selected automated checks.
  Refresh the permanent Designer Sample only after an implementation slice changes.
- Keep WPF read-only, authored Forms/resources intact and Showcase-independent
  portability. No commits, pushes or publishing without a request.

## Current draft checkpoint

The committed starting point for this planning discussion is `a24a831` on
`codex/Ribbonkit-Winform`. A Backstage extraction and neutral-caption correction
were implemented before the user requested planning and remain **uncommitted**.
Do not treat them as an accepted architecture or revert them as part of planning.

The draft introduces sibling `RibbonBackstage2019Layout` / `2024Layout` providers
and sibling `RibbonBackstage2019Renderer` / `2024Renderer` implementations. Both
renderer classes derive from a pure `RibbonBackstageRenderer`; both layout classes
derive from `RibbonBackstageLayoutProvider`. The draft still needs review against
the single-resolver and family-ownership boundaries proposed above.

Evidence already obtained before planning: the first focused run passed 30 cases
and failed two new named-color identity assertions. Correcting those assertions
to compare actual color values produced a passing seven-case follow-up; these
runs overlap. Two package-only Solid display scopes passed at actual 120 DPI:
2019 retained full-height pane alignment and 2024 restored its animated neutral
grey caption and close restoration. Evidence is under
`artifacts/backstage-family-isolation/native/`. Debug Showcase/sample builds passed
without warnings/errors; local sample package is
`0.1.0-designer.local20261110T150147762Z`. Human appearance acceptance remains pending.

Next: discuss this structure and the Backstage pilot before extending the rewrite.
