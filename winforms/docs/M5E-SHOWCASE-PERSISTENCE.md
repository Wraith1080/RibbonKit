# M5E Showcase restart persistence

Status, **2026-10-30: implemented; focused storage and actual-process checks passed;
human restart review pending**. Application storage uses existing public library
sessions and events; no runtime API or customization schema changed.

## Stored state and ownership

Normal interactive Showcase startup restores from
`%LOCALAPPDATA%/RibbonKit/WinForms.Showcase/` after all commands, icons and Reset
defaults are authored, before the first display. Restoration suppresses ribbon
motion. Capture/measurement switches disable user storage; the persistence probe
requires its own explicit isolated directory.

- `ribbon-customization.json`: existing native version-three profile, application
  ID `ribbonkit.winforms.showcase`. QAT membership/order/placement/visibility,
  authored/custom tab and group preferences, custom commands/icons and independent
  Simplified selections. Successful `CustomizationApplied` saves a fresh live
  session, never an open editor's pending draft. Direct QAT placement/visibility
  events queue a capture after the current transaction completes or rolls back.
- `preferences.json`: separate version-one Showcase format for Classic/Simplified,
  Compact/Touch, ribbon minimized state, animation level/content style, editor word
  wrap, requested backdrop, transparent Backstage choice, normal window bounds/DPI
  and maximized state. Changes through appearance
  commands and Editor Apply save preferences; accepted window close captures both
  files. Window movement/resizing saves at ResizeEnd/close. A minimized application
  reopens in its previous normal/maximized state. Bounds scale to the destination
  DPI and fit the current monitor working area when a saved monitor is unavailable.

Document text, field values, selection, focus, open menus/Backstage, contextual and
merge/modal/message state stay transient. At the user's request on 2026-10-30,
`backdrop` and `transparentBackstage` are optional version-one additions. Existing
files use Solid/opaque defaults; unsupported native material still retains the
requested selection. A transparent Backstage choice restores zero rail opacity.

The focused follow-up passed 17 preference/storage cases and an isolated actual
Showcase hidden save/restore check. `--check-backdrop-preferences-hidden <fresh-dir>`
uses its own `isolated-preferences` subdirectory and refuses to overwrite an
existing diagnostic file. It never shows a window. The broader five-process probes
below are earlier evidence, not a repeated visible restart campaign for these fields.

Apply establishes the saved checkpoint. Later edits followed by Cancel cannot
overwrite it. Reset/Import remain draft actions until successful Apply; Reset
uses authored defaults captured before loading the user's profile. Files are
separate application concerns and are not a cross-file transaction.

## Failure behavior

Each file has a 1 MiB limit. A successful write flushes a unique sibling temporary
file and replaces the existing destination atomically. Failed replacement leaves
the prior destination intact; interrupted temporary files are ignored on startup.
Repeated unchanged saves avoid disk writes. Storage errors appear in the status
bar without blocking startup or close.

Missing files use defaults. Malformed, foreign, unsupported/future-version,
unknown-command or otherwise rejected files use defaults for that file and block
its automatic writes for the rest of the process. The original stays untouched;
the other file remains usable. To recover, close Showcase and move the rejected
file aside or correct it before restarting. There is no automatic schema migration
or application Reset UI in this slice. Multiple simultaneous instances retain
ordinary last-writer behavior.

## Verification, 2026-10-30

- Normal Debug Showcase build: passed, zero warnings/errors.
- `ShowcasePersistenceTests`: 11 passed. Atomic replacement, interrupted temp
  files, damaged/missing/foreign/future/invalid-enum input, invalid bounds, size
  limits, failed writes, independent file behavior and DPI/offscreen fitting.
- `eng/Test-ShowcasePersistence.ps1`: five real-process probes passed with
  isolated files: missing defaults; structural/Simplified customization plus real
  Options QAT/Editor Apply → more edits → Cancel; fresh-process restoration;
  malformed files; future-version files. Checks include immediate Apply save,
  direct QAT property changes, stable custom identities/icons, appearance/editor
  preferences and normal/maximized placement. Rejected files remained byte-for-byte
  unchanged after process shutdown.
- Raw ignored evidence: `artifacts/showcase-persistence/20261030-060844-415/`.
- After the M6C caption/maximize corrections, the affected Apply/save and fresh
  process restore probes passed again in
  `artifacts/m6c-resumed-20261030-061116/persistence-after-frame-fix/`.

Physical monitor/DPI migration and human visual/restart acceptance remain pending.
No WPF, full native suite, Release or package campaign ran. Backdrop flicker remains
a separate M6C investigation, not established fixed by these persistence checks.
