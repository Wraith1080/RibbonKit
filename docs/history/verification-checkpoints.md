# Historical verification checkpoints

> Recorded results, not a current test run. Later checkpoints qualify or supersede earlier ones.
> See [current status](../../04-DESIGN-NOTES.md#5-current-state--next-steps).

- 2026-08-13: 347 logic tests, one visual test covering 63 approved images, and zero build
  warnings/errors.
- 2026-08-20: 350 logic tests, one visual test covering 63 approved images, and zero build
  warnings/errors; Office 2010 Aero live visual approval remains pending.
- 2026-08-20 after §3.98: 355 logic tests, one visual test covering 63 approved images, and zero
  build warnings/errors; Office 2010 Aero live visual approval remains pending.
- 2026-08-20 after §3.99: 356 logic tests, one visual test covering 63 approved images, and zero
  build warnings/errors; Writer W0-A live launch and editor input passed.
- 2026-08-20 after §3.100: 380 logic tests, one visual test covering 63 approved images, and zero
  build warnings/errors; Writer W0-B passed 25/25 focused tests across five consecutive runs.
- 2026-08-20 after §3.101: 400 logic tests, one visual test covering 63 approved images, and zero
  build warnings/errors; Writer W0-C passed 45/45 focused tests across five consecutive runs.
- 2026-08-20 after §3.102: 446 logic tests, one visual test covering 63 approved images, and zero
  build warnings/errors; Writer passed 91/91 focused tests, including 46 W0-D shell cases, and the
  native TXT/RTF/Backstage/QAT/dirty-close surface gate passed.
- 2026-08-20 after §3.103: 463 logic tests, one visual test covering 63 approved images, and zero
  build warnings/errors; Writer passed 108/108 tests, W1-A passed 17/17 across five consecutive runs,
  and its separately shown native-editor formatting/state gate passed.
- 2026-08-20 after §3.104: 493 logic tests, one visual test covering 63 approved images, and zero
  build warnings/errors; Writer passed 138/138 tests, W1-B passed 30/30 across five consecutive runs,
  and its separately shown native-editor utility/status gate passed.
- 2026-08-21 after §3.105: 493 logic tests, one visual test covering 63 approved images, and zero
  build warnings/errors; Writer passed 138/138 tests and the actual mouse/keyboard, complete Home
  KeyTip, QAT/minimized-ribbon, selection-state, editing-utility and file-lifecycle surface gate passed.
- 2026-08-24 after §3.106: 495 logic tests, one visual test covering 63 approved images, and zero
  build warnings/errors; Writer passed 140/140 tests and its standard/narrow restrained iconography,
  Backstage recent-row hover/activation, minimized-ribbon/QAT and themed native message-box surface
  gate passed.
- 2026-08-24 after §3.107: 524 logic tests, one visual test covering 63 approved images, and zero
  build warnings/errors; Writer passed 169/169 tests, W1-D received user visual acceptance and W2-A's
  immutable page presets, conversions, orientation and margin-validation gate passed.
- 2026-08-24 after §3.108: 547 logic tests, one visual test covering 63 approved images, and zero
  build warnings/errors; Writer passed 192/192 tests and W2-B's bounded, atomic, versioned `.rkw`
  persistence and data-only native-load safety gate passed.
- 2026-08-24 after §3.109 acceptance: the inventory is 555 logic tests plus one visual test covering
  63 approved images. W2-C's final code passed its 9 focused/window-integration tests and then the complete Writer
  suite **200/200** after the external clipboard lock cleared; RibbonKit's 355 tests and the visual test passed with
  a zero-warning solution build. Live mixed-monitor movement remains deferred to W4-C because one display is available.
- 2026-08-24 after §3.110 acceptance: the inventory is 574 logic tests plus one visual test covering 63 approved
  images. W2-D's 19 focused preview/printing tests and complete Writer suite **219/219** pass; A4 and Letter each
  produced five clean Microsoft Print to PDF pages from the same fixed paginator used by preview. The solution builds
  with zero warnings/errors and the unchanged RibbonKit/visual suites pass **355/355** and **1/1** respectively.
- 2026-08-24 after §3.111 acceptance: the inventory is 586 logic tests plus one visual test covering 63 approved
  images. W2-E's 20 focused Page/View, preview and window-integration tests and complete Writer suite **231/231** pass;
  the solution builds with zero warnings/errors and the unchanged RibbonKit/visual suites pass **355/355** and
  **1/1** respectively. Live Page/View, Custom Margins, preview, narrow/RTL, keyboard and Backstage surfaces passed at
  125% DPI; external main-ribbon leaf traversal remains the RKWF-006 investigation.
- 2026-08-24 after §3.111a correction: the inventory is 588 logic tests plus one visual test covering 63 approved
  images. Writer passes **233/233**, including the 8-test modal-preview, suspended-pagination, print-setup and window
  integration gate; the solution builds with zero warnings/errors and RibbonKit/visual remain **355/355** and
  **1/1**. The actual 125%-DPI View, modal preview and fitted Writer-owned Microsoft Print to PDF setup surfaces pass.
- 2026-08-24 after §3.111b correction: the inventory is 589 logic tests plus one visual test covering 63 approved
  images. Writer passes **234/234**, including the post-Loaded paper-margin regression; the solution builds with zero
  warnings/errors and RibbonKit/visual remain **355/355** and **1/1**. Cold-launch/DPI observation remains for W4-C.
- 2026-08-24 after §3.111c correction: the inventory remains 589 logic tests plus one visual test covering 63 approved
  images. Writer passes **234/234** with cold-start focus/insertion-default and large-control assertions folded into
  existing cases; the solution builds with zero warnings/errors and RibbonKit/visual remain **355/355** and **1/1**.
- 2026-08-24 after §3.112: the inventory is 614 logic tests plus one visual test covering 63 approved images. W0-E's
  focused document-profile/transition gate passes **25/25** and Writer passes **259/259**; the solution builds with
  zero warnings/errors and RibbonKit/visual remain **355/355** and **1/1**. W0-F is next and has not started.
- 2026-08-24 after §3.113: the inventory remains 614 logic tests plus one visual test covering 63 approved images.
  Writer passes **259/259** with W0-F assertions folded into existing shell/window facts; the solution builds with
  zero warnings/errors and RibbonKit/visual remain **355/355** and **1/1**. Standard and 800×900 live New/profile
  surfaces passed at the available scale; W2-F is next and has not started.
- 2026-08-25 after §3.114: the inventory is 636 logic tests plus one visual test covering 63 approved images. W2-F's
  focused ruler/drag gate passes **22/22** and Writer passes **281/281**; the solution builds with zero warnings/errors
  and RibbonKit/visual remain **355/355** and **1/1**. The actual Paper/Continuous, immediate-focus, ruler alignment,
  neutral-gray guide, clipping and View KeyTip surfaces passed at the available 125% scale. External leaf traversal
  remains the RKWF-006 investigation and mixed-monitor/DPI movement remains deferred to W4-C hardware verification.
- 2026-08-26 after §§3.115-3.116: the inventory is 688 logic tests plus one visual test covering 63 approved images.
  The combined W3-A/W3-B security, persistence and table-core gate passes **71/71**, and Writer passes **333/333** on
  repeated runs after its realized-window tests joined the serialized Writer UI collection. The solution builds with
  zero warnings/errors and RibbonKit/visual remain **355/355** and **1/1**. No RibbonKit runtime gap or change was
  required; W3-C owns the next live ribbon/table interaction surface.
- 2026-08-26 after §3.117: the inventory is 696 logic tests plus one visual test covering 63 approved images. W3-C's
  focused gate passes **19/19**, Writer passes **341/341**, RibbonKit remains **355/355**, and the visual suite remains
  **1/1** with a zero-warning solution build. The real 125%-scale Writer window passed app-owned Picture,
  Hyperlink/Date-Time dialog insertion and focus recovery; mouse/keyboard/UIA table-grid paths; table Tab routing;
  contextual tools; standard, 620-DIP narrow, minimized and RTL layouts; and a three-page table preview. RKWF-013
  records the app-owned opaque gallery-popup workaround; no RibbonKit runtime file changed. Mixed-monitor movement,
  external leaf traversal and a live High Contrast switch remain bounded follow-up evidence.
- 2026-08-27 after the §3.117 corrective follow-up: the inventory is 698 logic tests plus one visual test covering
  63 approved images. Writer passes **343/343**, RibbonKit passes **355/355**, the visual suite passes **1/1**, and
  the solution build has zero warnings/errors. The real available 125%-scale Writer window passed all four corrected
  surfaces: compact fixed insert dialogs, ruler-hidden separation, the three-row quick gallery plus Custom Table,
  and visible default/All Borders cell grids after a live No Borders transition. No `src/RibbonKit/**` file changed.
- 2026-08-27 after §3.119 implementation: the inventory is 736 logic tests plus one visual test covering 63 approved
  images. Writer passes **381/381**, RibbonKit passes **355/355**, the visual suite passes **1/1**, and the Debug solution
  build has zero warnings/errors. Independent Luna review findings were resolved. Live reacceptance of the native
  Font/Color dialogs, corrected Paragraph dialog and representative ribbon/context-menu states remains pending.
- 2026-08-28 after the §3.119 corrective dialog/search pass: the inventory is 739 logic tests plus one visual test
  covering 63 approved images. Writer passes **384/384**, RibbonKit passes **355/355**, the visual suite passes **1/1**,
  and the Debug solution build has zero warnings/errors. Writer now uses RibbonKit-themed app-owned Font, Color and
  Paragraph dialogs with no WinForms dependency; live visual reacceptance of those dialogs remains pending.
- 2026-08-28 after the second §3.119 live follow-up: the inventory is 743 logic tests plus one visual test covering
  63 approved images. Writer passes **388/388**, RibbonKit passes **355/355**, the visual suite passes **1/1**, and the
  Debug solution build has zero warnings/errors. The quick colour menu now exposes only base colours; More Colors has
  an HSV field/hue strip; and Font supports single/double strikethrough plus superscript/subscript with native undo and
  strictly validated `.rkw` round-trip. Refreshed live visual acceptance remains pending.
- 2026-08-28 after §3.120: the inventory is 751 logic tests plus one visual test covering 63 approved images. Writer
  passes **396/396**, RibbonKit passes **355/355** and visual passes **1/1**. The solution build succeeds with zero errors;
  12 design-tools copy warnings are caused only by the DLL being held open by Visual Studio process 33288, while the
  Writer project build is zero-warning. W3-D's native table/schema-v2 and TXT/RTF compatibility gate is accepted; the
  hue/preview addendum has realized regression coverage, and W1-E live visual reacceptance remains pending.
- 2026-08-28 after §3.121: the inventory is 756 logic tests plus one visual test covering 63 approved images. The W3-E1
  focused structured-context/state gate passes **17/17**. The original full Writer gate passed **401/401**; after the
  loaded-picture undo correction, the proportional rerun passed **400/400** together plus the process-focus case
  **1/1** in isolation. RibbonKit passes **355/355**, visual passes **1/1**, and the solution build has zero
  warnings/errors. W3-E2 and the full W3-E live matrix remain pending.
- 2026-08-29 after §3.122: the inventory is 789 logic tests plus one visual test covering 63 approved images. The focused
  W3-E2a gate passes **34/34**, Writer passes **434/434** in one run, RibbonKit passes **355/355**, visual passes **1/1**,
  and the solution build has zero warnings/errors. Actual Writer pointer/ribbon acceptance, table adorners and the full
  W3-E live matrix remain pending.
- 2026-08-29 after §3.123 implementation: at the explicitly requested minimal verification level, the new table-resize
  gate passes **3/3**, the existing real-tree MainWindow case passes **1/1**, and the Writer project builds with zero
  warnings/errors. The complete Writer/RibbonKit/visual/solution gates were intentionally not rerun, so §3.122 remains
  the latest full inventory checkpoint. Live table-grip and complete W3-E acceptance remain pending.
- 2026-08-29 after §3.124 and its Showcase examples: the inventory is **803 logic tests plus one visual test covering
  63 approved images**. RibbonKit passes **364/364**, Writer passes **439/439**, visual passes **1/1**, and the Debug
  solution build has zero warnings/errors. The four Writer-promoted corrections have focused realized coverage, but
  their documented live
  theme/DPI/RTL/High Contrast and Backstage-close interaction matrix remains pending.
- 2026-08-29 after §3.125: the inventory is **804 logic tests plus one visual test covering 63 approved images**.
  RibbonKit passes **365/365**, Writer passes **439/439**, visual passes **1/1**, and the Debug solution build has zero
  warnings/errors. Live Office 2010 Aero active/inactive color-continuity acceptance remains pending.
- 2026-08-30 after §3.126: the inventory remains **804 logic tests plus one visual test covering 63 approved images**.
  The existing focused localization test passes **1/1** and the Showcase Debug build has zero warnings/errors. The
  separator has user-accepted theme-switching and collapsed-flyout coverage; RTL and 100-200% DPI remained pending
  at that checkpoint. High Contrast is separate future whole-surface accessibility work, not an RKWF-005 gate.
- 2026-08-30 after §3.127: the inventory is **806 logic tests plus one visual test covering 63 approved images**.
  RibbonKit passes **367/367**, Writer passes **439/439**, visual passes **1/1**, and the Debug solution build has zero
  warnings/errors. RKWF-005/013/016 have the later user-accepted theme/light-dark and 100-200% DPI coverage recorded
  in the friction log; RKWF-018 later passed its real mixed-DPI transition recheck, so Packet 2 may begin.
- 2026-08-30 after §3.128: the inventory is **831 logic tests plus one visual test covering 63 approved images**.
  RibbonKit passes **392/392**, Writer passes **439/439**, visual passes **1/1**, and the Release solution build has zero
  warnings/errors. The themed scrollbar/dialog gate's twenty-one focused cases, Track-level minimum, full-width vertical Pill, contained bottom button,
  independent rail/button/thumb radius API, and realized popup overflow path pass; live cross-theme, DPI, RTL and High
  Contrast geometry is user-accepted. The scoped Customize Ribbon scrollbar comparison is also live-accepted; the
  QAT scrollbar, final Office 2013/2019 square-token visual recheck, and modern visible action-button chrome comparison
  remain pending live confirmation.
