# RibbonKit — Design Notes & Session Context

> Documentation reconciled 2026-09-08 against the checkout and recorded evidence through
> 2026-09-05. No new build, test, or live acceptance is implied.

## 1. Project Overview

RibbonKit is an MIT-licensed WPF ribbon library distributed through GitHub Releases.
The runtime targets `net8.0-windows` and `net9.0-windows`; the separate Visual Studio
design-tools assembly targets `net472`. Showcase is the component lab and Writer is
the sustained rich-text consumer. See [README](README.md) for use and supported features.

## 2. Core Architecture

The maintained [architecture reference](docs/01-ARCHITECTURE.md) describes source layout
and subsystem contracts. `Themes/Office2024.xaml` is the shared `Controls.*.xaml`
aggregator, not an experimental split or a separate template set per theme.
All five Office generations have token dictionaries and dark/black variants.

Read the relevant historical entry below before modifying a subsystem. In particular:

- Template/resource scope: §3.37 and later popup/scrollbar corrections.
- Merge/modal/QAT lifetimes and selection geometry: §§3.32–3.36.
- Window maximize/DPI, Snap Layouts and frame composition: §§3.12, 3.18, 3.42, 3.89–3.98.
- Writer lifecycle, persistence and editing: §§3.99–3.136.
- Opt-in editable pagination and measured limits: §§3.137–3.155.

## 3. Implemented Features (chronological, with pitfalls)

This index preserves numbered references and heading anchors. Follow the matching
entry's link for its implementation detail; do not load all history for routine work.
Historical pending items are superseded only by later evidence, never by a plan.

### 3.1 Core ribbon skeleton

[Decision, pitfalls and evidence](docs/history/01-library.md#31-core-ribbon-skeleton).

### 3.2 Minimized QAT card (2024)

[Decision, pitfalls and evidence](docs/history/01-library.md#32-minimized-qat-card-2024).

### 3.3 KeyTips (Alt / F10)

[Decision, pitfalls and evidence](docs/history/01-library.md#33-keytips-alt--f10).

### 3.4 Contextual tabs = custom coloring (no marker line)

[Decision, pitfalls and evidence](docs/history/01-library.md#34-contextual-tabs--custom-coloring-no-marker-line).

### 3.5 Colored title bar + accent customization

[Decision, pitfalls and evidence](docs/history/01-library.md#35-colored-title-bar--accent-customization).

### 3.6 2019 modernization & hover consistency

[Decision, pitfalls and evidence](docs/history/01-library.md#36-2019-modernization--hover-consistency).

### 3.7 Large-button label alignment

[Decision, pitfalls and evidence](docs/history/01-library.md#37-large-button-label-alignment).

### 3.8 QAT placement (TitleBar / TabRow / BelowRibbon) + context menu

[Decision, pitfalls and evidence](docs/history/01-library.md#38-qat-placement-titlebar--tabrow--belowribbon--context-menu).

### 3.9 QAT white icons on colored surfaces

[Decision, pitfalls and evidence](docs/history/01-library.md#39-qat-white-icons-on-colored-surfaces).

### 3.10 Animation system (global + per-action)

[Decision, pitfalls and evidence](docs/history/01-library.md#310-animation-system-global--per-action).

### 3.11 Backstage redesign (Modern 2024) + icons

[Decision, pitfalls and evidence](docs/history/01-library.md#311-backstage-redesign-modern-2024--icons).

### 3.12 Mica (Windows 11 system backdrop) — EXPERIMENTAL

[Decision, pitfalls and evidence](docs/history/01-library.md#312-mica-windows-11-system-backdrop--experimental).

### 3.13 UI polish fixes

[Decision, pitfalls and evidence](docs/history/01-library.md#313-ui-polish-fixes).

### 3.14 XAML design-time preview (active tab + backstage)

[Decision, pitfalls and evidence](docs/history/01-library.md#314-xaml-design-time-preview-active-tab--backstage).

### 3.15 QAT customization + extensible options dialog (Word-Options style)

[Decision, pitfalls and evidence](docs/history/01-library.md#315-qat-customization--extensible-options-dialog-word-options-style).

### 3.16 "Customize the Ribbon" structure page

[Decision, pitfalls and evidence](docs/history/01-library.md#316-customize-the-ribbon-structure-page).

### 3.17 Customization persistence (serialize / restore / Reset)

[Decision, pitfalls and evidence](docs/history/01-library.md#317-customization-persistence-serialize--restore--reset).

### 3.18 QAT/dialog polish batch (context menus, persistence, maximize, layout, hover)

[Decision, pitfalls and evidence](docs/history/01-library.md#318-qatdialog-polish-batch-context-menus-persistence-maximize-layout-hover).

### 3.19 Dropdown proxies (real dropdown that borrows the source's menu)

[Decision, pitfalls and evidence](docs/history/01-library.md#319-dropdown-proxies-real-dropdown-that-borrows-the-sources-menu).

### 3.20 Large-button label: inline dropdown chevron + multi-line ellipsis

[Decision, pitfalls and evidence](docs/history/01-library.md#320-large-button-label-inline-dropdown-chevron--multi-line-ellipsis).

### 3.21 Backstage: footer items, button items, design-time page preview

[Decision, pitfalls and evidence](docs/history/01-library.md#321-backstage-footer-items-button-items-design-time-page-preview).

### 3.22 Design-time smart tags / quick actions (XAML designer) — VERIFIED IN VS

[Decision, pitfalls and evidence](docs/history/01-library.md#322-design-time-smart-tags--quick-actions-xaml-designer--verified-in-vs).

### 3.23 Ribbon Editor dialog (design-time) + tab-preview feasibility

[Decision, pitfalls and evidence](docs/history/01-library.md#323-ribbon-editor-dialog-design-time--tab-preview-feasibility).

### 3.24 Animation polish batch — all six remaining transitions wired

[Decision, pitfalls and evidence](docs/history/01-library.md#324-animation-polish-batch--all-six-remaining-transitions-wired).

### 3.25 Ribbon horizontal scroll (tab strip + groups row)

[Decision, pitfalls and evidence](docs/history/01-library.md#325-ribbon-horizontal-scroll-tab-strip--groups-row).

### 3.26 Modern context menus (ribbon-item + QAT right-click)

[Decision, pitfalls and evidence](docs/history/01-library.md#326-modern-context-menus-ribbon-item--qat-right-click).

### 3.27 Office 2010 ("Blue") theme — the first gradient theme

[Decision, pitfalls and evidence](docs/history/01-library.md#327-office-2010-blue-theme--the-first-gradient-theme).

### 3.28 Backstage page-text colour + ribbon focus (RichTextBox) — 2026-07-21

[Decision, pitfalls and evidence](docs/history/01-library.md#328-backstage-page-text-colour--ribbon-focus-richtextbox--2026-07-21).

### 3.29 Connected-tab body-border cut (2010/2013) — the "notch" — 2026-07-23

[Decision, pitfalls and evidence](docs/history/01-library.md#329-connected-tab-body-border-cut-20102013--the-notch--2026-07-23).

### 3.30 Backstage Mica pass-through (Modern/2024) — hide, don't blur — 2026-07-23

[Decision, pitfalls and evidence](docs/history/01-library.md#330-backstage-mica-pass-through-modern2024--hide-dont-blur--2026-07-23).

### 3.31 Office 2024 default (Auto) accent aligned to #2B579A — 2026-07-23

[Decision, pitfalls and evidence](docs/history/01-library.md#331-office-2024-default-auto-accent-aligned-to-2b579a--2026-07-23).

### 3.32 Modal tabs (Print-Preview mode) — Phase 7 P7.1 — 2026-07-27

[Decision, pitfalls and evidence](docs/history/01-library.md#332-modal-tabs-print-preview-mode--phase-7-p71--2026-07-27).

### 3.33 Tab merging + group contributions — Phase 7 P7.2/P7.3 — 2026-07-27

[Decision, pitfalls and evidence](docs/history/01-library.md#333-tab-merging--group-contributions--phase-7-p72p73--2026-07-27).

### 3.34 MDI ⇄ ribbon integration: tab merge + caption merge — MDI M4 — 2026-07-27

[Decision, pitfalls and evidence](docs/history/01-library.md#334-mdi--ribbon-integration-tab-merge--caption-merge--mdi-m4--2026-07-27).

### 3.35 Quick access toolbar overflow — 2026-07-27

[Decision, pitfalls and evidence](docs/history/01-library.md#335-quick-access-toolbar-overflow--2026-07-27).

### 3.36 Selection visuals: the tab-strip-row reflow rule

[Decision, pitfalls and evidence](docs/history/01-library.md#336-selection-visuals-the-tab-strip-row-reflow-rule).

### 3.37 Splitting Office2024.xaml into Controls.*.xaml parts — 2026-07-27 — ⚗ EXPERIMENTAL

[Decision, pitfalls and evidence](docs/history/01-library.md#337-splitting-office2024xaml-into-controlsxaml-parts--2026-07-27---experimental).

### 3.38 Office 2007 theme — the last generation, and the one that changed the templates — 2026-07-27

[Decision, pitfalls and evidence](docs/history/01-library.md#338-office-2007-theme--the-last-generation-and-the-one-that-changed-the-templates--2026-07-27).

### 3.39 Stranded menus, take two: the borrow must not hang off `Popup.Closed` — 2026-07-27

[Decision, pitfalls and evidence](docs/history/01-library.md#339-stranded-menus-take-two-the-borrow-must-not-hang-off-popupclosed--2026-07-27).

### 3.40 Chrome polish batch — pressed states, caption glass, and four hijacked tokens — 2026-07-28

[Decision, pitfalls and evidence](docs/history/01-library.md#340-chrome-polish-batch--pressed-states-caption-glass-and-four-hijacked-tokens--2026-07-28).

### 3.41 Two ordering bugs: an Effect painted over, and a FLIP that flashed — 2026-07-28

[Decision, pitfalls and evidence](docs/history/01-library.md#341-two-ordering-bugs-an-effect-painted-over-and-a-flip-that-flashed--2026-07-28).

### 3.42 Every flyout now opens as a whole surface — and the DPI manifest — 2026-07-28

[Decision, pitfalls and evidence](docs/history/01-library.md#342-every-flyout-now-opens-as-a-whole-surface--and-the-dpi-manifest--2026-07-28).

### 3.43 Split button: a vertical arrangement, and halves that acknowledge each other — 2026-07-28

[Decision, pitfalls and evidence](docs/history/01-library.md#343-split-button-a-vertical-arrangement-and-halves-that-acknowledge-each-other--2026-07-28).

### 3.44 The shared easing function was never frozen — 2026-07-28

[Decision, pitfalls and evidence](docs/history/01-library.md#344-the-shared-easing-function-was-never-frozen--2026-07-28).

### 3.45 Proxies never mirrored their source's enabled state — 2026-07-28

[Decision, pitfalls and evidence](docs/history/01-library.md#345-proxies-never-mirrored-their-sources-enabled-state--2026-07-28).

### 3.46 The real Office 2007 application menu — a control that had to sit BEHIND the orb — 2026-07-28

[Decision, pitfalls and evidence](docs/history/01-library.md#346-the-real-office-2007-application-menu--a-control-that-had-to-sit-behind-the-orb--2026-07-28).

### 3.47 Nested popup Escape is a stack, not five independent window handlers — 2026-08-01

[Decision, pitfalls and evidence](docs/history/01-library.md#347-nested-popup-escape-is-a-stack-not-five-independent-window-handlers--2026-08-01).

### 3.48 Visual-regression snapshots: the complete theme × DPI matrix — 2026-08-01

[Decision, pitfalls and evidence](docs/history/01-library.md#348-visual-regression-snapshots-the-complete-theme--dpi-matrix--2026-08-01).

### 3.49 Dark/Black variants for every generation — 2026-08-01/02

[Decision, pitfalls and evidence](docs/history/01-library.md#349-darkblack-variants-for-every-generation--2026-08-0102).

### 3.50 Dark live-switch corrections from the 100% showcase pass — 2026-08-01

[Decision, pitfalls and evidence](docs/history/01-library.md#350-dark-live-switch-corrections-from-the-100-showcase-pass--2026-08-01).

### 3.51 First deterministic RTL snapshot slice — 2026-08-01

[Decision, pitfalls and evidence](docs/history/01-library.md#351-first-deterministic-rtl-snapshot-slice--2026-08-01).

### 3.52 Localization foundation + RTL ribbon context menus — 2026-08-02

[Decision, pitfalls and evidence](docs/history/01-library.md#352-localization-foundation--rtl-ribbon-context-menus--2026-08-02).

### 3.53 Customize/Options localization + RTL action snapshot — 2026-08-02

[Decision, pitfalls and evidence](docs/history/01-library.md#353-customizeoptions-localization--rtl-action-snapshot--2026-08-02).

### 3.54 Chrome tooltip localization — 2026-08-02

[Decision, pitfalls and evidence](docs/history/01-library.md#354-chrome-tooltip-localization--2026-08-02).

### 3.55 Live localized default File label — 2026-08-02

[Decision, pitfalls and evidence](docs/history/01-library.md#355-live-localized-default-file-label--2026-08-02).

### 3.56 Localized conventional application-menu footer actions — 2026-08-02

[Decision, pitfalls and evidence](docs/history/01-library.md#356-localized-conventional-application-menu-footer-actions--2026-08-02).

### 3.57 Application-button width now reflows the selection visuals — 2026-08-02

[Decision, pitfalls and evidence](docs/history/01-library.md#357-application-button-width-now-reflows-the-selection-visuals--2026-08-02).

### 3.58 Representative bidirectional content in RTL Backstage — 2026-08-03

[Decision, pitfalls and evidence](docs/history/01-library.md#358-representative-bidirectional-content-in-rtl-backstage--2026-08-03).

### 3.59 Live RTL Backstage rail, slide and title transition — 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#359-live-rtl-backstage-rail-slide-and-title-transition--2026-08-08).

### 3.60 Localization/RTL lab follows the Showcase File-surface policy — 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#360-localizationrtl-lab-follows-the-showcase-file-surface-policy--2026-08-08).

### 3.61 Live RTL popup/window verification closes Phase 6 — 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#361-live-rtl-popupwindow-verification-closes-phase-6--2026-08-08).

### 3.62 Flat-theme application-menu footer buttons retain their outline — 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#362-flat-theme-application-menu-footer-buttons-retain-their-outline--2026-08-08).

### 3.63 Failure-only CI artifacts for cross-machine snapshot diagnosis — 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#363-failure-only-ci-artifacts-for-cross-machine-snapshot-diagnosis--2026-08-08).

### 3.63a Responsive Ribbon Editor + application-menu authoring — VERIFIED IN VS, 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#363a-responsive-ribbon-editor--application-menu-authoring--verified-in-vs-2026-08-08).

### 3.64 Backstage sheet depth and the 2010 colored caption — 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#364-backstage-sheet-depth-and-the-2010-colored-caption--2026-08-08).

### 3.65 Repeatable Ribbon message bar — 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#365-repeatable-ribbon-message-bar--2026-08-08).

### 3.66 Dark/Black Backstage rails use generation-matched neutrals â€” 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#366-darkblack-backstage-rails-use-generation-matched-neutrals-â-2026-08-08).

### 3.67 Compact RibbonCheckBox and RibbonRadioButton — 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#367-compact-ribboncheckbox-and-ribbonradiobutton--2026-08-08).

### 3.68 Compact RibbonTextBox — 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#368-compact-ribbontextbox--2026-08-08).

### 3.69 KeyTip resolution is typeable and prefix-free — 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#369-keytip-resolution-is-typeable-and-prefix-free--2026-08-08).

### 3.70 Explicit KeyTips inside arbitrary File-surface content — 2026-08-08

[Decision, pitfalls and evidence](docs/history/01-library.md#370-explicit-keytips-inside-arbitrary-file-surface-content--2026-08-08).

### 3.71 Showcase appearance preferences stay separate from ribbon customization — 2026-08-10

[Decision, pitfalls and evidence](docs/history/01-library.md#371-showcase-appearance-preferences-stay-separate-from-ribbon-customization--2026-08-10).

### 3.72 Customization serializer round-trip and foreign-JSON hardening — 2026-08-10

[Decision, pitfalls and evidence](docs/history/01-library.md#372-customization-serializer-round-trip-and-foreign-json-hardening--2026-08-10).

### 3.73 Reduction thresholds, priority order, and malformed-width coverage — 2026-08-10

[Decision, pitfalls and evidence](docs/history/01-library.md#373-reduction-thresholds-priority-order-and-malformed-width-coverage--2026-08-10).

### 3.74 Visual Studio debugger distorted live-resize performance — 2026-08-10

[Decision, pitfalls and evidence](docs/history/01-library.md#374-visual-studio-debugger-distorted-live-resize-performance--2026-08-10).

### 3.75 Phase 8 API review and freeze — 2026-08-10

[Decision, pitfalls and evidence](docs/history/01-library.md#375-phase-8-api-review-and-freeze--2026-08-10).

### 3.76 Repository-native v1 documentation gate — 2026-08-10

[Decision, pitfalls and evidence](docs/history/01-library.md#376-repository-native-v1-documentation-gate--2026-08-10).

### 3.77 NuGet and Showcase identity icon — 2026-08-11

[Decision, pitfalls and evidence](docs/history/01-library.md#377-nuget-and-showcase-identity-icon--2026-08-11).

### 3.78 Office 2007 application-menu open layout stability — 2026-08-11

[Decision, pitfalls and evidence](docs/history/01-library.md#378-office-2007-application-menu-open-layout-stability--2026-08-11).

### 3.79 Source Link and symbol package verification — 2026-08-11

[Decision, pitfalls and evidence](docs/history/01-library.md#379-source-link-and-symbol-package-verification--2026-08-11).

### 3.80 Portable local package output — 2026-08-11

[Decision, pitfalls and evidence](docs/history/01-library.md#380-portable-local-package-output--2026-08-11).

### 3.81 Deterministic package versioning — 2026-08-11

[Decision, pitfalls and evidence](docs/history/01-library.md#381-deterministic-package-versioning--2026-08-11).

### 3.82 NuGet package and clean-consumer release gate — 2026-08-11

[Decision, pitfalls and evidence](docs/history/01-library.md#382-nuget-package-and-clean-consumer-release-gate--2026-08-11).

### 3.83 Final live performance and installed-package pass — 2026-08-11

[Decision, pitfalls and evidence](docs/history/01-library.md#383-final-live-performance-and-installed-package-pass--2026-08-11).

### 3.84 Local v1.0.0 GitHub-release candidate — 2026-08-11

[Decision, pitfalls and evidence](docs/history/01-library.md#384-local-v100-github-release-candidate--2026-08-11).

### 3.85 GitHub v1.0.0 community release — 2026-08-11

[Decision, pitfalls and evidence](docs/history/01-library.md#385-github-v100-community-release--2026-08-11).

### 3.86 Post-v1 custom-control projection and future-theme plans — 2026-08-12

[Decision, pitfalls and evidence](docs/history/01-library.md#386-post-v1-custom-control-projection-and-future-theme-plans--2026-08-12).

### 3.87 RibbonKit Writer functional reference-app plan — 2026-08-12

[Decision, pitfalls and evidence](docs/history/01-library.md#387-ribbonkit-writer-functional-reference-app-plan--2026-08-12).

### 3.87a RibbonKit Writer Luna execution decomposition — 2026-08-20

[Decision, pitfalls and evidence](docs/history/01-library.md#387a-ribbonkit-writer-luna-execution-decomposition--2026-08-20).

### 3.88 Office 2007 opaque and Aero-inspired window-frame plan — 2026-08-12

[Decision, pitfalls and evidence](docs/history/01-library.md#388-office-2007-opaque-and-aero-inspired-window-frame-plan--2026-08-12).

### 3.89 Office 2007 opaque window baseline — reference correction — 2026-08-12

[Decision, pitfalls and evidence](docs/history/01-library.md#389-office-2007-opaque-window-baseline--reference-correction--2026-08-12).

### 3.90 Office 2007 Aero-inspired window-frame prototype — 2026-08-12

[Decision, pitfalls and evidence](docs/history/01-library.md#390-office-2007-aero-inspired-window-frame-prototype--2026-08-12).

### 3.91 RibbonKit-original Glass2007 Backstage prototype — 2026-08-12

[Decision, pitfalls and evidence](docs/history/01-library.md#391-ribbonkit-original-glass2007-backstage-prototype--2026-08-12).

### 3.92 Separate Classic2007 Backstage concept — 2026-08-13

[Decision, pitfalls and evidence](docs/history/01-library.md#392-separate-classic2007-backstage-concept--2026-08-13).

### 3.93 S7 Office 2007 window-frame final verification — 2026-08-13

[Decision, pitfalls and evidence](docs/history/01-library.md#393-s7-office-2007-window-frame-final-verification--2026-08-13).

### 3.94 Classic2007 Backstage orb proxy and seamless design switching — 2026-08-13

[Decision, pitfalls and evidence](docs/history/01-library.md#394-classic2007-backstage-orb-proxy-and-seamless-design-switching--2026-08-13).

### 3.95 Office 2010 Backstage begins below the tab headers — 2026-08-13

[Decision, pitfalls and evidence](docs/history/01-library.md#395-office-2010-backstage-begins-below-the-tab-headers--2026-08-13).

### 3.96 Office 2010 Backstage tab-strip polish — 2026-08-13

[Decision, pitfalls and evidence](docs/history/01-library.md#396-office-2010-backstage-tab-strip-polish--2026-08-13).

### 3.97 Office 2010 Aero-inspired frame prototype — 2026-08-20

[Decision, pitfalls and evidence](docs/history/01-library.md#397-office-2010-aero-inspired-frame-prototype--2026-08-20).

### 3.98 Collapsed-group and Classic2007 follow-up fixes — 2026-08-20

[Decision, pitfalls and evidence](docs/history/01-library.md#398-collapsed-group-and-classic2007-follow-up-fixes--2026-08-20).

### 3.99 RibbonKit Writer W0-A application scaffold — 2026-08-20

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#399-ribbonkit-writer-w0-a-application-scaffold--2026-08-20).

### 3.100 RibbonKit Writer W0-B document lifetime model — 2026-08-20

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3100-ribbonkit-writer-w0-b-document-lifetime-model--2026-08-20).

### 3.101 RibbonKit Writer W0-C TXT/RTF persistence and recent files — 2026-08-20

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3101-ribbonkit-writer-w0-c-txtrtf-persistence-and-recent-files--2026-08-20).

### 3.102 RibbonKit Writer W0-D shell, Backstage and file-command integration — 2026-08-20

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3102-ribbonkit-writer-w0-d-shell-backstage-and-file-command-integration--2026-08-20).

### 3.103 RibbonKit Writer W1-A formatting command and selection-state engine — 2026-08-20

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3103-ribbonkit-writer-w1-a-formatting-command-and-selection-state-engine--2026-08-20).

### 3.104 RibbonKit Writer W1-B editing utilities and status state — 2026-08-20

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3104-ribbonkit-writer-w1-b-editing-utilities-and-status-state--2026-08-20).

### 3.105 RibbonKit Writer W1-C editing ribbon integration — 2026-08-21

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3105-ribbonkit-writer-w1-c-editing-ribbon-integration--2026-08-21).

### 3.106 RibbonKit Writer W1-D iconography and first visual-polish candidate — 2026-08-21

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3106-ribbonkit-writer-w1-d-iconography-and-first-visual-polish-candidate--2026-08-21).

### 3.107 RibbonKit Writer W2-A page-settings model — 2026-08-24

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3107-ribbonkit-writer-w2-a-page-settings-model--2026-08-24).

### 3.108 RibbonKit Writer W2-B versioned native persistence — 2026-08-24

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3108-ribbonkit-writer-w2-b-versioned-native-persistence--2026-08-24).

### 3.109 RibbonKit Writer W2-C centred paper editing surface — 2026-08-24

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3109-ribbonkit-writer-w2-c-centred-paper-editing-surface--2026-08-24).

### 3.110 RibbonKit Writer W2-D stable preview, pagination and printing — 2026-08-24

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3110-ribbonkit-writer-w2-d-stable-preview-pagination-and-printing--2026-08-24).

### 3.111 RibbonKit Writer W2-E Page/View ribbon and preview integration — 2026-08-24

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3111-ribbonkit-writer-w2-e-pageview-ribbon-and-preview-integration--2026-08-24).

### 3.111a Writer W2-E modal-preview, typing-performance and print-setup correction — 2026-08-24

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3111a-writer-w2-e-modal-preview-typing-performance-and-print-setup-correction--2026-08-24).

### 3.111b Writer startup paper invariant and document-profile plan insertion — 2026-08-24

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3111b-writer-startup-paper-invariant-and-document-profile-plan-insertion--2026-08-24).

### 3.111c Writer cold-start editing state and ribbon-density correction — 2026-08-24

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3111c-writer-cold-start-editing-state-and-ribbon-density-correction--2026-08-24).

### 3.112 RibbonKit Writer W0-E document profiles and format-transition policy — 2026-08-24

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3112-ribbonkit-writer-w0-e-document-profiles-and-format-transition-policy--2026-08-24).

### 3.113 RibbonKit Writer W0-F New gallery and capability-aware command projection — 2026-08-24

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3113-ribbonkit-writer-w0-f-new-gallery-and-capability-aware-command-projection--2026-08-24).

### 3.114 RibbonKit Writer W2-F margin guides and interactive horizontal ruler — 2026-08-25

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3114-ribbonkit-writer-w2-f-margin-guides-and-interactive-horizontal-ruler--2026-08-25).

### 3.115 RibbonKit Writer W3-A portable images, hyperlinks and date/time — 2026-08-26

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3115-ribbonkit-writer-w3-a-portable-images-hyperlinks-and-datetime--2026-08-26).

### 3.116 RibbonKit Writer W3-B simple FlowDocument table core — 2026-08-26

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3116-ribbonkit-writer-w3-b-simple-flowdocument-table-core--2026-08-26).

### 3.117 RibbonKit Writer W3-C Insert, table interaction and contextual Table Tools — 2026-08-26

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3117-ribbonkit-writer-w3-c-insert-table-interaction-and-contextual-table-tools--2026-08-26).

### 3.118 RibbonKit Writer structured-object interaction planning correction — 2026-08-27

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3118-ribbonkit-writer-structured-object-interaction-planning-correction--2026-08-27).

### 3.119 RibbonKit Writer W1-E Home formatting completion — 2026-08-27

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3119-ribbonkit-writer-w1-e-home-formatting-completion--2026-08-27).

### 3.120 RibbonKit Writer final formatting/preview polish and W3-D structured-content round-trip — 2026-08-28

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3120-ribbonkit-writer-final-formattingpreview-polish-and-w3-d-structured-content-round-trip--2026-08-28).

### 3.121 RibbonKit Writer W3-E1 structured-context and contextual-state foundation — 2026-08-28

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3121-ribbonkit-writer-w3-e1-structured-context-and-contextual-state-foundation--2026-08-28).

### 3.122 RibbonKit Writer W3-E2a explicit picture selection, Picture Tools and resizing — 2026-08-29

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3122-ribbonkit-writer-w3-e2a-explicit-picture-selection-picture-tools-and-resizing--2026-08-29).

### 3.123 RibbonKit Writer W3-E2b table selection and direct resizing — 2026-08-29

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3123-ribbonkit-writer-w3-e2b-table-selection-and-direct-resizing--2026-08-29).

### 3.124 Writer-promoted RibbonKit friction corrections — 2026-08-29

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3124-writer-promoted-ribbonkit-friction-corrections--2026-08-29).

### 3.125 Showcase Ribbon Lab split and Office 2010 inactive frame continuity — 2026-08-29

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3125-showcase-ribbon-lab-split-and-office-2010-inactive-frame-continuity--2026-08-29).

### 3.126 Localization/RTL separator acceptance surface — 2026-08-30

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3126-localizationrtl-separator-acceptance-surface--2026-08-30).

### 3.127 Live-DPI InRibbonGallery viewport and side-button correction — 2026-08-30

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3127-live-dpi-inribbongallery-viewport-and-side-button-correction--2026-08-30).

### 3.128 Theme-aware scrollbar control and gallery overflow — 2026-08-30

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3128-theme-aware-scrollbar-control-and-gallery-overflow--2026-08-30).

### 3.129 RibbonKit Writer table selection normalization and vertical cell alignment — 2026-08-30

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3129-ribbonkit-writer-table-selection-normalization-and-vertical-cell-alignment--2026-08-30).

### 3.130 RibbonKit Writer alignment range and stable table adorners — 2026-08-30

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3130-ribbonkit-writer-alignment-range-and-stable-table-adorners--2026-08-30).

### 3.131 RibbonKit Writer table-placement persistence — 2026-08-30

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3131-ribbonkit-writer-table-placement-persistence--2026-08-30).

### 3.132 RibbonKit Writer table-adorners under editor zoom — 2026-08-30

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3132-ribbonkit-writer-table-adorners-under-editor-zoom--2026-08-30).

### 3.133 RibbonKit Writer semantic table placement across editing views — 2026-08-31

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3133-ribbonkit-writer-semantic-table-placement-across-editing-views--2026-08-31).

### 3.134 RibbonKit Writer pictured virtual-printer submission — 2026-08-31

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3134-ribbonkit-writer-pictured-virtual-printer-submission--2026-08-31).

### 3.135 RibbonKit Writer W3-E live acceptance closure — 2026-08-31

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3135-ribbonkit-writer-w3-e-live-acceptance-closure--2026-08-31).

### 3.136 RibbonKit Writer W4-A customization and appearance persistence — 2026-08-31

[Decision, pitfalls and evidence](docs/history/02-writer-foundation.md#3136-ribbonkit-writer-w4-a-customization-and-appearance-persistence--2026-08-31).

### 3.137 RibbonKit Writer W2-G editable-pagination feasibility — 2026-08-31

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3137-ribbonkit-writer-w2-g-editable-pagination-feasibility--2026-08-31).

### 3.138 RibbonKit Writer W2-G public page-geometry map spike — 2026-08-31

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3138-ribbonkit-writer-w2-g-public-page-geometry-map-spike--2026-08-31).

### 3.139 RibbonKit Writer W2-G paragraph caret/selection compositor prototype — 2026-08-31

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3139-ribbonkit-writer-w2-g-paragraph-caretselection-compositor-prototype--2026-08-31).

### 3.140 RibbonKit Writer W2-G dedicated-STA layout-worker spike — 2026-08-31

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3140-ribbonkit-writer-w2-g-dedicated-sta-layout-worker-spike--2026-08-31).

### 3.141 RibbonKit Writer W2-G latest-only worker coalescing and cancellation — 2026-08-31

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3141-ribbonkit-writer-w2-g-latest-only-worker-coalescing-and-cancellation--2026-08-31).

### 3.142 RibbonKit Writer W2-G paragraph focus and native-command bridge — 2026-08-31

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3142-ribbonkit-writer-w2-g-paragraph-focus-and-native-command-bridge--2026-08-31).

### 3.143 RibbonKit Writer W2-G paragraph page-setting reflow and live anchors — 2026-08-31

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3143-ribbonkit-writer-w2-g-paragraph-page-setting-reflow-and-live-anchors--2026-08-31).

### 3.144 RibbonKit Writer W2-G paragraph input-services prototype batch — 2026-08-31

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3144-ribbonkit-writer-w2-g-paragraph-input-services-prototype-batch--2026-08-31).

### 3.145 RibbonKit Writer W2-G structured-content and viewport prototype batch — 2026-08-31

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3145-ribbonkit-writer-w2-g-structured-content-and-viewport-prototype-batch--2026-08-31).

### 3.146 RibbonKit Writer W2-G lifecycle closure and production feasibility decision — 2026-08-31

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3146-ribbonkit-writer-w2-g-lifecycle-closure-and-production-feasibility-decision--2026-08-31).

### 3.147 RibbonKit Writer W2-G opt-in production compositor and LTR live decision — 2026-09-01

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3147-ribbonkit-writer-w2-g-opt-in-production-compositor-and-ltr-live-decision--2026-09-01).

### 3.148 RibbonKit Writer W2-G page-local resize and page-chrome hardening — 2026-09-01

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3148-ribbonkit-writer-w2-g-page-local-resize-and-page-chrome-hardening--2026-09-01).

### 3.149 RibbonKit Writer W2-G immutable table boundaries and accessible resize projection — 2026-09-01

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3149-ribbonkit-writer-w2-g-immutable-table-boundaries-and-accessible-resize-projection--2026-09-01).

### 3.150 RibbonKit Writer W2-G structural table matrix and safe Auto-column fallback — 2026-09-01

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3150-ribbonkit-writer-w2-g-structural-table-matrix-and-safe-auto-column-fallback--2026-09-01).

### 3.151 RibbonKit Writer W2-G keyboard resize, worker telemetry and live scalability boundary — 2026-09-01

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3151-ribbonkit-writer-w2-g-keyboard-resize-worker-telemetry-and-live-scalability-boundary--2026-09-01).

### 3.152 RibbonKit Writer W2-G staged publication and spelling-cliff correction — 2026-09-01

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3152-ribbonkit-writer-w2-g-staged-publication-and-spelling-cliff-correction--2026-09-01).

### 3.153 RibbonKit Writer W2-G reusable layout session and bounded directional page cache — 2026-09-04

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3153-ribbonkit-writer-w2-g-reusable-layout-session-and-bounded-directional-page-cache--2026-09-04).

### 3.154 RibbonKit Writer W2-G decoded page-memory and mixed-content plateau gate — 2026-09-04

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3154-ribbonkit-writer-w2-g-decoded-page-memory-and-mixed-content-plateau-gate--2026-09-04).

### 3.155 RibbonKit Writer W2-G saved-document and reduced-cache feasibility — 2026-09-05

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3155-ribbonkit-writer-w2-g-saved-document-and-reduced-cache-feasibility--2026-09-05).

### 3.156 RibbonKit Writer W2-G speculative admission and page-cost timing — 2026-09-09

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3156-ribbonkit-writer-w2-g-speculative-admission-and-page-cost-timing--2026-09-09).

### 3.157 RibbonKit Writer W2-G insertion traversal profiling and exact-map parity — 2026-09-09

[Decision, pitfalls and evidence](docs/history/03-writer-pagination.md#3157-ribbonkit-writer-w2-g-insertion-traversal-profiling-and-exact-map-parity--2026-09-09).

### 3.158 RibbonKit Writer W2-G native caret page following — 2026-09-09

[Implementation and evidence](docs/history/03-writer-pagination.md#3158-ribbonkit-writer-w2-g-native-caret-page-following--2026-09-09).

### 3.159 RibbonKit Writer default paginated Paper — 2026-09-09

[Default activation and verification](docs/history/03-writer-pagination.md#3159-ribbonkit-writer-default-paginated-paper--2026-09-09).

### 3.160 RibbonKit Writer blank-page click correction — 2026-09-09

[Reproduction and correction](docs/history/03-writer-pagination.md#3160-ribbonkit-writer-blank-page-click-correction--2026-09-09).

### 3.161 RibbonKit Writer typing presentation and caret blink — 2026-09-09

[Correction and verification](docs/history/03-writer-pagination.md#3161-ribbonkit-writer-typing-presentation-and-caret-blink--2026-09-09).

## 4. Workflow / Session Conventions

Use [AGENTS.md](AGENTS.md), [proportional validation](CONTRIBUTING.md#proportional-validation),
and the [WPF workflow skill](.agents/skills/ribbonkit-wpf-workflow/SKILL.md).
Keep current status here, product scope in the relevant plan, and dated evidence in
`docs/history/`. Add future numbered implementation entries to the relevant history
file and this index; update §5 only as supported by verification.

## 5. Current State & Next Steps

> Authoritative summary of recorded evidence through 2026-09-09.
> Counts below are dated results with their stated verification scope.

### Complete

- Library phases 0–8 and the `v1.0.0` GitHub release are complete (§3.85). The checkout
  includes later improvements; the released package is not a claim to contain every
  subsequent repository change.
- Five Office themes and dark/black variants, localization/RTL, KeyTips, customization,
  merge/modal services, design tooling, and package validation have recorded gates.
  Office 2007 S0–S9, including the optional frame and Glass2007/Classic2007 Backstage,
  are complete through §3.94. MDI floating children and caption/tab merging are complete
  (M0/M4); remaining MDI work is listed below.
- Writer W0-A–F, W1-A–D, W2-A–F, W3-A–E, and W4-A are accepted at their recorded scope
  and available hardware. They cover document/profile lifecycle, safe TXT/RTF/native
  persistence, editing, paper/ruler/preview/print, pictures/hyperlinks/tables,
  contextual resizing, and separate Settings/appearance/customization persistence.
  W3-E closes at §3.135; W4-A closes at §3.136.
- Shared consumer corrections and remaining narrower acceptance gates are indexed in
  the [Writer friction log](docs/12-RIBBONKIT-WRITER-CONSUMER-FRICTION-LOG.md).
  “Corrected” does not establish every theme/DPI/RTL gate.

### Remaining or intentionally deferred

- **Paginated editing is now the default Paper view, by user direction.** One live editor owns input/history;
  immutable clone-backed pages use a dedicated STA layout session, latest-only
  cancellation/coalescing, bounded retention and current-generation interaction.
  LTR editing, page-local table/picture resizing, staged spelling, empty replacement,
  bounded scrolling/cache work and native caret page following have recorded evidence
  through §3.161. Typing retains the last page image until its replacement is ready;
  the caret follows Windows blink timing. The existing interactive ruler and context menu are connected to the
  compositor; diagnostic telemetry remains opt-in. `--writer-classic-paper` restores
  the prior Paper surface. The surrounding workspace now follows the appearance backdrop
  brush; its focused realized-window regression passes 1/1 with unchanged page images.
  Default activation does not close W2-G acceptance.
- **W2-G implementation continues; rapid-scroll benchmarking is deferred.** The proposed
  fixed-cadence probe is optional validation, not a prerequisite for implementing pagination.
  Native caret navigation now follows the paginated viewport, including after edits/reflow,
  while ordinary scrolling leaves the caret unchanged. Long-document authoring acceptance,
  cold-page latency and native working-set high-water remain open (RKWF-037/038/039/040).
- **Genuine OS IME and production RTL** remain a later paired input/geometry slice.
  W4-B retains integrated acceptance work; the default-Paper decision is made. W4-C includes live
  mixed-monitor/DPI and physical-printer checks; earlier single-display acceptance
  does not close them. W5 remains a distribution decision after sustained use.
- **W1-E:** Home formatting and the corrected Font/Color/Paragraph dialogs have
  automated evidence through §3.119, but live visual reacceptance remains unrecorded.
  Named styles remain unsupported without a complete style/persistence contract.
- **Library:** MDI arrange/cycle commands, full MVVM proof, tabbed mode and persistence
  (M1–M3); Office 2010 Aero live visual approval (§3.97/§3.125); optional designer
  scalar reset actions; touch density, richer QAT/custom-control projections and
  future themes. Complete Windows contrast-theme support is not claimed.
- Automatic `Icons.xaml` discovery remains best-effort; the manual browser is the
  fallback for no match, ambiguity, inaccessible paths or parse failure.

### Verification checkpoint

- Latest correction: §3.160 blank-page click regression, focused tests **4/4**, Release
  Writer build **0 warnings / 0 errors**. The blockless-page crash was reproduced before fixing.
- Latest recorded full solution gate in this checkpoint list: §3.128 (2026-08-30),
  Release build with zero warnings/errors; RibbonKit 392/392, Writer 439/439, visual
  1/1 covering 63 approved images. These are historical counts, not today's inventory.
- Latest W2-G evidence: §3.159 (2026-09-09), pagination production tests **42/42**;
  isolated default-window, centered-table view-switch and scrollbar tests **3/3**.
  The default-window check includes multiple pages, native ruler margin commit, context
  selection, and Paper/Continuous/Preview switching. Its rendered page/ruler was inspected.
  A combined run hit WPF cross-thread theme caching; an isolated broad window contract
  fails on its old Settings-menu expectation with both default and classic Paper.
  No full-suite, solution-build, physical input, OS IME, RTL or long-document acceptance
  claim follows from the default switch. §3.157 retains its geometry/cache limits.
- Earlier full/focused results and acceptance limits remain in the
  [checkpoint ledger](docs/history/verification-checkpoints.md) and numbered history.
  Rerun proportionate checks before claiming current results.
