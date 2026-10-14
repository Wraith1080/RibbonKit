# Toolbox item definitions

> Proposed follow-on plan, requested 2026-10-14. Proceed after the
> [feature availability plan](16-RIBBON-FEATURE-AVAILABILITY-PLAN.md).
> This document authorizes no toolbox/runtime implementation or validation run.

## Goal

Refresh RibbonKit's declared Toolbox surface so application authors can discover
the supported controls, add them in appropriate locations, and receive consistent
defaults. Review the complete current authoring surface rather than only appending
the latest simplified-ribbon types.

Keep toolbox discovery, item creation defaults and feature availability separate:
the item list selects authorable types, designer initializers seed new items, and
runtime capability properties govern whether the feature can be used. Preserve
the accepted S6 Inline/label defaults and existing authored settings.

## Current boundary

- `src/RibbonKit/tools/VisualStudioToolsManifest.xml` is the package Toolbox
  allowlist, with RibbonKit as its Visual Studio/Blend category. It currently
  lists the ribbon, tabs/groups, command/input controls, galleries and Backstage.
- `src/RibbonKit/RibbonKit.csproj` packages that manifest under `tools/` and the
  net472 DesignTools assembly beside each runtime target in its `Design` folder.
- Project-reference Toolbox discovery reflects public controls and does not use
  that package allowlist. A change to the manifest alone does not prove the
  Showcase solution's Toolbox has been filtered or updated.
- `Metadata.cs` registers designer providers by string type name. Initializers
  and Model API creation paths provide defaults; the manifest does not write
  per-command XAML properties. Use ModelProperty.IsSet to preserve assignments.

## T1 — inventory and authoring decisions

Compare public control types, package entries, designer metadata, context/editor
Add actions and public documentation. Produce one reviewed include/exclude matrix
with a reason and valid parent/location for each candidate. Public visibility
alone does not make a control useful as a Toolbox item.

| Family | Review scope |
| --- | --- |
| Ribbon, tabs and groups | Retain the supported structure authoring workflow and starter content |
| Buttons, toggles, split/dropdowns, combo/text inputs and options | Confirm existing entries and type-appropriate defaults; retain native-only behavior where inline simplified copies are unsupported |
| Galleries and menu entries | Review parent-specific insertion, starter items and editability; avoid dropping child entries onto unrelated surfaces |
| Application-menu types | Review RibbonApplicationMenu and its command, pane, separator and footer controls, which are absent from the current manifest |
| Options/customization pages | Decide whether RibbonOptionsPage, RibbonQuickAccessPage, RibbonCustomizePage and RibbonSimplifiedCustomizePage belong in the Toolbox or are better created through existing editor/context actions |
| Message and auxiliary authoring controls | Assess RibbonMessageBar, RibbonMessage and RibbonScreenTip by intended ownership and insertion path |
| Windows/dialogs | Decide whether root declaration or existing host/editor creation is the appropriate route; do not expose a drop entry merely because a class derives from Window |
| Internal presentation and proxies | Keep template hosts, adaptive panels, generated copies, navigation chrome and implementation helpers out of the package authoring list |

Review whether category names, ordering, display names or Toolbox icons need any
change. Verify the installed designer SDK's supported metadata/schema before
choosing those details. Keep this review bounded; do not add new controls, runtime
APIs, new-item templates or generic drag/drop infrastructure implicitly.

## T2 — definitions and designer alignment

- Apply the approved matrix to the manifest and corresponding designer metadata.
  Keep entries unique, correctly qualified and backed by shipped runtime types.
- Add only the missing initializer/context/editor support required for approved
  authoring entries. Preserve the net472/string-type/Model API boundary and one
  undo operation per creation or property edit.
- Retain Inline placement and shown-label defaults for newly created supported
  commands. Existing properties, bindings and explicit Overflow/Small choices
  must survive copy, reload and undo/redo.
- Honor the feature-availability contract agreed in the preceding plan. Define
  whether an unavailable feature's authoring item is hidden or remains available
  with an explanation; either choice must preserve runtime enforcement and the
  author's prepared settings. Do not assume a static manifest can vary per ribbon.
- Preserve existing package/deployment paths. Update the setup guide for the
  actual delivered surface, including package versus project-reference behavior.
  Avoid maintaining a second stale type/count list in general documentation.

## T3 — focused verification and live acceptance

Check manifest XML, duplicate/missing entries and the intended exclusion set.
Build the affected designer/runtime outputs, pack locally and validate the
manifest and DesignTools paths for both runtime targets. Inspect a clean package
consumer that has no Showcase resources or helpers; package inspection alone
does not prove Visual Studio loaded the updated Toolbox definitions.

In a fresh Visual Studio session, verify the package consumer's Toolbox contents,
approved drop locations, representative controls from each changed family,
defaults, saved XAML reload, property editing and undo/redo. Separately check the
project-reference workflow and record its discovery limits. Cover the preceding
capability switch in enabled/disabled states where it affects authoring.

Reuse earlier passing evidence where behavior is unchanged. Run only affected
checks for a bounded definition/metadata change; use the broader API/build/package
gates if implementation changes public API or distribution behavior. Keep live
designer results distinct from builds, package inspection and runtime consumers.

## Sequencing and acceptance

The intended follow-on sequence is feature availability, then this Toolbox
definition refresh. T1's include/exclude matrix should be reviewable before T2
implementation. The current S6 defaults/toolbox repair remains accepted in the
[simplified plan](15-SIMPLIFIED-RIBBON-PLAN.md#s6-designer-defaults--2026-10-14).

The full simplified-feature test pass and its earlier unresolved native/full
gates remain deferred until the user explicitly requests testing. Creating this
plan does not start those gates, schedule work or authorize publishing a package.
