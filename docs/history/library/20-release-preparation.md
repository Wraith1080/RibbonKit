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

#### CI diagnostics follow-up — 2026-10-25

Added manual dispatch and separate sequential steps for all four test projects,
without filters or continuation scopes. Each writes its own TRX report, uploaded
even after failure. Test-step conditions explicitly allow execution after another
test fails, provided Build succeeded and the run was not canceled; they do not use
`continue-on-error`, so failed tests still prevent packaging and fail the job.
Selection/capture variables are cleared at job scope and the existing failure PNG
artifact remains. The workflow uses read-only repository permissions.

Checkout 7.0.1, setup-dotnet 6.0.0 and upload-artifact 7.0.2 were checked against
their official manifests for Node 24 support. The old Node deprecation warning
does not establish the cause of the failing Test step. Actionlint 1.7.12 and local
PowerShell parsing pass; actual hosted execution is tracked separately in A9.
Do not substitute syntax checks for a passing GitHub run or weaken snapshots
without inspecting failure diagnostics.

#### Hosted WPF fixture assumptions and early repair checks — 2026-10-25

The first preview CI run completed all four projects and exposed assumptions
hidden by the local desktop: a connected UIA root, an empty shared Application
resource scope, expanded window tracking width, native popup room and a sample
motion policy initialized before its XAML checked handlers. Correct those fixture
preconditions explicitly. Writer density cases wait for the real End event, while
the 40-case dialog/theme/palette matrix keeps all assertions with its own 60-second bound
and phase timings; the shared STA timeout is unchanged. No product source changed.

Simulated root DPI did not isolate snapshot glyph/native-window paint from the
desktop DPI. An owned 96-DPI process reproduced the downloaded first CI PNG
byte-for-byte. The capture STA now scopes/restores its own canonical 96-DPI
awareness context while retaining every requested render scale, font policy and
pixel threshold. After actual/diff inspection, the 195 references were recalibrated;
all 195 images then matched byte-for-byte across 96/120-DPI process contexts and
each scene's repeated capture. Original references and diagnostics are retained.
This does not claim native display or mixed-monitor acceptance.

CI now frontloads four focused repair steps and records each result even if a
sibling fails. Every focus selection rejects zero matches. Their success gate
precedes all four original unfiltered projects; a failed prefix avoids the longer
phase while keeping CI red and uploading diagnostics. Once the full phase starts,
its projects still report after sibling failures. Step-scoped selection variables
cannot restrict the full phase. Focused/full reports stay separate and duplicate
execution must not be counted as new unique coverage. Current passing local scopes
and the required hosted rerun belong to A2/A3/A9 in the active acceptance plan.

#### QAT gallery raster-position assertions — 2026-10-25

The hosted consumer passed its repaired native-gallery origin matrix, then failed
the visible source strip's exact raster bounds during QAT borrowing. An owned
96-DPI process reproduced `(105,10,72,28)` versus `(105,9,72,28)` at simulated
125%: the viewport's logical bounds and the painted size were identical. The
ordinary local process passed the stricter comparison. Before/open captures
confirm adjacent-pixel snapping between live visuals and the frozen DrawingImage.

Keep logical viewport bounds, bitmap dimensions and swatch size exact, but allow
one output-bitmap pixel of painted translation in this live/frozen comparison.
Apply the same rule to nested overflow, retain selection/presenter-return checks,
and log matrix context with optional before/open images. This is a test-only
allowance; do not generalize it to snapshot thresholds, popup anchors or layout.
The 96-DPI visible-strip/input/ScreenTip scope passes; no product source changes.
CI places the complete GalleryQuickAccess scope first and adds it to the repair
success gate. The full consumer remains mandatory; current results and remaining
hosted validation belong to the active plan.

#### First-Apply fixtures and long matrix deadlines — 2026-10-25

Replacing Application.Resources alone does not reset ThemeManager's previous
Apply. A test of first-Apply cleanup must explicitly reset that shared state
before merging its manual startup dictionary; otherwise test order determines
whether the manager takes the first-call cleanup branch. Seeding a previous
Apply reproduced the Crystal fixture failure without a product defect.

Keep native focus fixtures intact across palette sweeps: creating a window per
palette introduced a missing-focus-adorner setup failure. The original long
contrast matrix instead opts into a measured 60-second body allowance through
the test helper's RunWithTimeout; default Sta.Run remains 30 seconds and all
assertions/render waits remain. Per-palette timing identifies slow combinations.

The full hosted consumer's 37 passing sections consumed 28 minutes, leaving too
little of its old aggregate deadline for customization and the remaining chain.
Give only the unfiltered run a bounded hour; do not extend focused/continuation
budgets or drop later sections. Per-theme customization progress distinguishes
continued work from a silent stall. The runtime repair group now runs first and
includes the new failing cases. Current results and the retained failed logs
belong to the active acceptance plan.
