# M5 native customization and Simplified Ribbon

Status: **M5A catalog and QAT transaction foundation implemented; human review pending**, 2026-10-28.

## M5A catalog and QAT transaction foundation — 2026-10-28

The user authorized continuation and confirmed focused verification only. M5A
starts with a usable QAT engine; M5B adds the right-click customization menu and
QAT editor UI. M5C extends the same identity/transaction foundation to structural
Ribbon editing. This slice implements no context menu or customization window.

Public contract proposed before implementation:

- Stable owned `RibbonControl.CommandCatalog`, with owned application `Commands`
  for sources that need no ordinary ribbon slot, and available-entry snapshots for
  QAT versus Ribbon customization. Groups occur only in the QAT list. Discover
  authored groups/direct commands and QAT sources; deduplicate by source identity.
- Preserve QAT-only sources removed by customization, including Undo/Redo, so both
  available lists can restore them. Projections borrow source models; native
  fields, values, command handlers and bindings retain their existing owners.
- Application-authored source IDs must be explicit and stable for JSON export.
  Generated runtime IDs continue working for in-process drafts. No caption/path
  fallback, WPF JSON compatibility, automatic application storage or appearance
  persistence is introduced. Changing stable IDs requires application migration.
- A detached QAT draft describes membership/order and toolbar visibility/placement.
  Apply preflights all sources/identities and batches accepted changes. Invalid
  focused native input blocks Apply. Cancel discards only unapplied edits; the
  latest successful Apply is the checkpoint. Reset uses captured catalog defaults.
  Parallel/external permanent QAT changes make a session stale until Reload.
- Versioned, application-scoped JSON uses data-only entries. Reject malformed,
  duplicate-property, foreign, future-version and unknown-command input before
  touching live models. Import changes only the draft. Merge/modal/notification/
  File/Backstage content and transient references are excluded; current transient
  QAT declarations survive Apply without entering saved data.
- UI/menu entry points, custom tabs/groups, proxy presentation in Ribbon groups,
  Simplified layout, animation, themes, localization/RTL and actual VS review
  remain their planned slices. Verify only catalog, draft/serialization/Apply and
  affected QAT source/native-editor/merge boundaries in this slice.

Implemented behavior:

- Catalog discovery includes authored groups/direct commands, declared application
  Commands and ordinary QAT sources, deduplicated by source identity. Display
  labels remain source-backed. File/Backstage/notification and merge/modal sources
  are excluded. TextBox sources are Ribbon-only; groups are QAT-only. Existing
  button/check/radio, dropdown/gallery/split, ComboBox and group projections are
  reused. No generic projection-provider API was introduced.
- Explicit source-ID assignment is tracked independently of generated runtime IDs.
  Runtime-only drafts still work; Export/Import require stable selected source IDs.
  Presentation keys distinguish duplicate source presentations; explicit reference
  IDs are preferred, otherwise authored source/occurrence keys are retained.
  Session-created presentation keys are stored in JSON. GetQuickAccessEntryId maps
  live permanent entries for the future context menu without assuming source IDs
  and presentation IDs are equal.
- Removing QAT-only owned commands through Apply moves them into catalog Commands,
  preserving the same component, handlers and bindings. Reset can return the same
  model to its original QAT slot. Removed presentations are retained for reuse and
  disposed with the catalog when still detached. Ordinary collection Remove keeps
  its existing detach semantics; application-only sources should be declared in
  Commands when they must remain cataloged independently of toolbar membership.
- Drafts contain only permanent membership/order and toolbar visibility/placement.
  Existing authored presentation models/aliases are reused. Newly added entries
  use source-backed default labels; this version supplies no rename UI or alias
  persistence schema. Source values, checked/selection/enabled/visible state,
  captions, bindings, theme/density, transient scope and application content are
  not restored from JSON. Host-authored reference metadata should have stable
  explicit IDs when its identity must survive changes to application declarations.
- Apply validates identities, sources and stale checkpoints before mutation, commits
  focused native ribbon input, revalidates after host validation callbacks, and
  batches the accepted topology. Invalid input returns false. Validation/identity
  changes during commit are rejected. A structural rollback restores the previous
  toolbar for an Apply-time host event failure; repeatedly throwing rollback event
  handlers remain application behavior rather than a general exception-isolation
  promise. Current transient QAT references retain their instances/relative slots.
- Cancel discards only edits after the latest Apply. Reload rebases external
  permanent changes; transient changes do not stale the draft. Reset stages the
  defaults captured before preferences were loaded. Import stages only validated
  native JSON; storage is application-owned and Applied is the persistence cue.
  JSON uses an explicit native discriminator, version and application identity,
  string placement values, required fields and duplicate/unknown-property rejection.

Relevant evidence:

- Normal Debug solution build passed without warnings/errors, including runtime
  XML/API-baseline and designer compilation. No full/Release/WPF/package campaign
  was run, following the user's focused-testing direction.
- Seven focused unit/STA cases passed in
  `tests/RibbonKit.WinForms.Tests/TestResults/m5a-customization-focused.trx`:
  catalog policy, QAT-only recovery, detached/Apply/Cancel/Reset/restart behavior,
  malformed/foreign/future/unknown JSON and duplicate IDs, transient preservation,
  external-stale detection, native-input veto/HWND retention, validation-callback
  source mutation, explicit persistence identities and host-event rollback.
  After moving rejected-plan cleanup before the identity-conflict throw, the
  affected rejection case passed separately in `m5a-rejected-plan-cleanup.trx`.
  No passed unrelated scope was repeated.
- Public-only Office and neutral consumers passed at actual 120 DPI under
  `TestResults/visual/m5a-customization-{office,neutral}/`. They verify invalid input
  blocking, successful Apply/Applied counts, shared toggle binding, single native
  editor borrowing through a customized whole-group QAT entry, removed-command
  recovery in both catalogs, reset to the original command instance/handler,
  retained native HWND/value and draft-only JSON import. Runs used project outputs,
  not a fresh package. Their captures and exported data-only profile were inspected.
- Showcase **Merging → QAT drafts** demonstrates a preset detached change, Apply,
  Cancel, Reset and application-owned in-memory Save/Restore. It uses only public
  engine APIs. Actual before/unapplied/applied/cancel/reset/import captures were
  inspected under `TestResults/visual/m5a-customization-showcase/`. This preset demo
  is not the M5B customization editor or context menu.

Remaining work: human review; M5B reusable right-click menu and QAT customization
screen; M5C structural Ribbon draft/schema/proxy editing; M5D Simplified integration;
actual VS, fresh-package, physical mixed-monitor and separate accessibility gates.
WPF runtime/build/CI are unchanged. No commit, push or application file persistence
was performed by the runtime.
