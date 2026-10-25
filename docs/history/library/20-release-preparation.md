# Library history: Release preparation

> Versioning, package provenance and release-tooling records. Current acceptance
> belongs in the [1.1 plan](../../20-V1.1-ACCEPTANCE-AND-RELEASE-PLAN.md).

[History directory](../01-library.md)

### 3.282 Shared 1.1 version and API baseline promotion — 2026-10-25

Moved `VersionPrefix=1.1.0` to `Directory.Build.props`, removing the runtime's
local 1.0 override so runtime, samples and design tooling inherit one version.
The GitHub-release helper defaults to 1.1.0. Promoted 264 reviewed additive API
entries into Shipped; all 1,093 earlier entries remain as the exact original
prefix. Unshipped contains only its nullable directive for future work. No runtime
behavior, target framework, package layout or public signature changed here.

The 1.1 notes precede the preserved complete 1.0 release record. README retains
the published 1.0 badge/link while identifying the preparing checkout; publication
is a separate action. CONTRIBUTING describes API promotion without permitting
removals/signature changes to be hidden in the frozen baseline.

Normal Release/API/XML validation and the newly versioned package checks pass.
Clean net8/net9 consumers construct all 15 manifest controls and finish their
existing runtime smoke. An unchanged net8 1.0 consumer binary also launches with
the 1.1 replacement runtime. Payload hashes, 1.1 assembly versions, NuSpec revision
and embedded Source Link are checked separately from accepted functional evidence.

`Validate-Package.ps1` replaces `TestResults/package-consumer`; preserve a reviewed
project before invoking it. This run temporarily moved A7's reviewed project after
checking workspace boundaries, retained the fresh consumer separately, restored
A7 in `finally` and verified all nine top-level review files unchanged. Current
Writer settings were kept at the user's request; prior backups were retained.

Dirty-checkout assets are local candidates: NuSpec/Source Link still identify
HEAD. Commit reviewed changes and rebuild/revalidate before publishing, so package
provenance identifies the exact release source. A9 owns asset hashes, current
evidence and remaining release actions; this history is not a second sign-off list.
