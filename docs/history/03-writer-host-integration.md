# Writer host integration history

> Current evidence and manual acceptance belong in the
> [Writer integration checkpoint](../19-WRITER-HOST-INTEGRATION.md).

## 2026-10-22 — Preferences and shared layout motion

Writer uses an inherited window density scope and explicit detached-window scopes
for View's Mouse/Touch choice. Restoration occurs during window construction,
before presentation; first-render readiness remains RibbonKit-owned. Appearance
retains the selected View density through edits/defaults. New preference fields
are additive schema-v1 properties, with Compact and WhiteOverlay defaults, so
existing settings remain readable without a schema migration.

The follow-up selector presentation uses the same shared RibbonDropDownButton
and descriptive, illustrated Mouse/Touch menu pattern as Showcase. Writer owns
the icon resources and preference handler. Density inheritance highlights the
active row even when the menu is borrowed by QAT; the `Writer.View.InputMode`
command identity remains stable for saved customization.

Office2024Flat follows all generic backdrop/glass/accent APIs. The only 2024-only
Writer presentation branch requiring extension was active-backdrop ruler
transparency. The Office workspace and paper edge remain neutral; Crystal's
underlay/fade/edge policy remains Crystal-only. QAT icon appearance sets the
existing public property and uses the shared template, including its separate
color glow and adaptive artwork.

Settings keeps its existing appearance and ribbon checkpoints. Apply advances
the appearance rollback snapshot; the Simplified page's Apply advances only the
ribbon checkpoint. No new mode selector, command default redesign or schema
coupling was introduced. Find/Replace's remaining native presentation now uses
the same shared dialog surfaces and inputs as the other detached Writer forms.

The separately approved WriterDocumentTransition subscribes to Preparing,
Starting and Ended. Real layout commits immediately. Inert vector drawings move
paper, ruler and preview without scaling text, while a clip follows the occupied
ribbon boundary and stops above the status bar. Supersession samples current
presentation and guards terminal cleanup by ID; source opacity is hidden with a
composed clock so authored bindings/animations/transforms/clips survive release.

Snapshotting skips invisible children and rejects native/media/live-brush content,
composition, nontrivial root transforms and more than 4,000 realized visual nodes.
Theme/view/document changes, unload and geometry invalidation release Writer's
presentation. No host timer, lifecycle deferral/completion, Showcase helper or
new runtime API is involved. Crystal's established underlay remains live after
playback; temporary document paint never covers the moving QAT.

Focused tests use temporary preference files, real offscreen WPF windows,
nonempty selected text, 110% zoom and shared-clock sampling. Mixed-monitor DPI
and human visual acceptance remain separate.
The two-line shared Simplified label binding correction is recorded in
[library history](library/14-simplified-ribbon.md#read-only-choice-labels--2026-10-22)
and RKWF-050; it was authorized after a fresh-process reproduction.
