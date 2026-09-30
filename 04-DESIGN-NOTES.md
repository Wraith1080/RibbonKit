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
- Expanding Paper and margin-guide growth: §3.162.

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
Includes the 2026-09-16 animated Backstage badge-placement correction and focused verification.

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

### 3.137 Editable multipage editing — canceled

Canceled due to bloat. Implementation, dedicated tests and obsolete documentation removed.

### 3.162 RibbonKit Writer expanding Paper default and growing margin guide — 2026-09-15

[Correction and verification](docs/history/02-writer-foundation.md#3162-ribbonkit-writer-expanding-paper-default-and-growing-margin-guide--2026-09-15).

### 3.163 Writer Insert dialog theming — 2026-09-16

[Correction and verification](docs/history/02-writer-foundation.md#3163-writer-insert-dialog-theming--2026-09-16).

### 3.164 Crystal contextual tab study and compact button refinement — 2026-09-21

[Implementation and verification](docs/history/01-library.md#3164-crystal-contextual-tab-study-and-compact-button-refinement--2026-09-21).

### 3.165 Crystal Light theme selection — 2026-09-24

[Implementation and verification](docs/history/01-library.md#3165-crystal-light-theme-selection--2026-09-24).

### 3.166 Crystal main Showcase presentation — 2026-09-24

[Screenshot finding and focused correction](docs/history/01-library.md#3166-crystal-main-showcase-presentation--2026-09-24).

### 3.167 Crystal Light first-phase gaps — 2026-09-24

[Screenshot findings and focused implementation](docs/history/01-library.md#3167-crystal-light-first-phase-gaps--2026-09-24).

### 3.168 Crystal Backstage designs and shared split hover — 2026-09-27

[Independent layout entries and theme-token parity](docs/history/01-library.md#3168-crystal-backstage-designs-and-shared-split-hover--2026-09-27).

### 3.169 Crystal File hover rim in the shared template — 2026-09-27

[Template and token promotion](docs/history/01-library.md#3169-crystal-file-hover-rim-in-the-shared-template--2026-09-27).

### 3.170 Minimized tab shape in the shared template — 2026-09-27

[State-driven tab geometry and Office token parity](docs/history/01-library.md#3170-minimized-tab-shape-in-the-shared-template--2026-09-27).

### 3.171 Crystal body-scroll geometry in the shared template — 2026-09-27

[Body-arrow tokens and utility-helper boundary](docs/history/01-library.md#3171-crystal-body-scroll-geometry-in-the-shared-template--2026-09-27).

### 3.172 Compact Showcase theme and Backstage choices — 2026-09-27

[View-tab gallery, layout dropdown, and startup selection](docs/history/01-library.md#3172-compact-showcase-theme-and-backstage-choices--2026-09-27).

### 3.173 Crystal Acrylic surface transparency — 2026-09-27

[Ribbon body token split and Acrylic-only Crystal opacity](docs/history/01-library.md#3173-crystal-acrylic-surface-transparency--2026-09-27).

### 3.174 Optional Acrylic glass treatment across Showcase themes — 2026-09-27

[Shared token overlay, tab and split hover, and opt-in behavior](docs/history/01-library.md#3174-optional-acrylic-glass-treatment-across-showcase-themes--2026-09-27).

### 3.175 Crystal translucent Backstage option — 2026-09-27

[Crystal layout templates honor the existing Translucent setting](docs/history/01-library.md#3175-crystal-translucent-backstage-option--2026-09-27).

### 3.176 Acrylic hover accent and File wash — 2026-09-27

[Accent-tinted command hover and brighter File hover](docs/history/01-library.md#3176-acrylic-hover-accent-and-file-wash--2026-09-27).

### 3.177 Crystal selected-tab bridge — 2026-09-27

[Enable the existing foot and body notch for Crystal](docs/history/01-library.md#3177-crystal-selected-tab-bridge--2026-09-27).

### 3.178 Crystal hover tab closes below the header — 2026-09-27

[Shared hover outline tokens without tab or body movement](docs/history/01-library.md#3178-crystal-hover-tab-closes-below-the-header--2026-09-27).

### 3.179 Crystal hover and selected tabs share an open lower edge — 2026-09-27

[Connected hover geometry and minimized-tab preservation](docs/history/01-library.md#3179-crystal-hover-and-selected-tabs-share-an-open-lower-edge--2026-09-27).

### 3.180 Crystal hover foot clipping — 2026-09-27

[Hover foot stays inside the clipped tab strip](docs/history/01-library.md#3180-crystal-hover-foot-clipping--2026-09-27).

### 3.181 Crystal hover uses one translucent surface — 2026-09-27

[Remove the overlapping hover foot while retaining the selected connector](docs/history/01-library.md#3181-crystal-hover-uses-one-translucent-surface--2026-09-27).

### 3.182 Crystal hover rounding and accent-tinted QAT glass — 2026-09-27

[Round the measured hover chrome and give the QAT its own shared background token](docs/history/01-library.md#3182-crystal-hover-rounding-and-accent-tinted-qat-glass--2026-09-27).

### 3.183 Glass tab hover follows the accent — 2026-09-27

[Remove the fixed blue-white tab hover cast while keeping its quiet opacity](docs/history/01-library.md#3183-glass-tab-hover-follows-the-accent--2026-09-27).

### 3.184 Crystal Light MDI Demo integration — 2026-09-27

[Shared MDI tokens and detached Showcase presentation](docs/history/01-library.md#3184-crystal-light-mdi-demo-integration--2026-09-27).

### 3.185 Connected first tab with no File button — 2026-09-27

[Shared header-row QAT or tab inset for hidden application buttons](docs/history/01-library.md#3185-connected-first-tab-with-no-file-button--2026-09-27).

### 3.186 Crystal Light Localization/RTL lab and Backstage templates — 2026-09-28

[Reuse Showcase presentation in the detached lab and localize Crystal Backstage](docs/history/01-library.md#3186-crystal-light-localizationrtl-lab-and-backstage-templates--2026-09-28).

### 3.187 Crystal Light Print Preview host paint — 2026-09-28

[Reuse the shared modal tab and scope preview paint to Showcase](docs/history/01-library.md#3187-crystal-light-print-preview-host-paint--2026-09-28).

### 3.188 Crystal dark palette — 2026-09-28

[Dark token overlay, Showcase materials and focused switching checks](docs/history/01-library.md#3188-crystal-dark-palette--2026-09-28).

### 3.189 Crystal duplicate audit — 2026-09-28

[Keep distinct option lenses and remove the redundant Backstage resource wrapper](docs/history/01-library.md#3189-crystal-duplicate-audit--2026-09-28).

### 3.190 Crystal customization list frames — 2026-09-28

[Round the four list and tree frames in Showcase while retaining native scrolling](docs/history/01-library.md#3190-crystal-customization-list-frames--2026-09-28).

### 3.191 Showcase presentation portability audit — 2026-09-28

[Separate reusable Crystal control styling from optional host effects](docs/history/01-library.md#3191-showcase-presentation-portability-audit--2026-09-28).

### 3.192 Theme-native Office 2007 orb default — 2026-09-28

[Shared token default, explicit override and consumer proof](docs/history/01-library.md#3192-theme-native-office-2007-orb-default--2026-09-28).

### 3.193 Theme-owned no-application header inset and snapshot renewal — 2026-09-28

[Audit zero and rounded-corner insets across themes](docs/history/01-library.md#3193-theme-owned-no-application-header-inset-and-snapshot-renewal--2026-09-28).

### 3.194 Portable application-orb glyph template — 2026-09-28

[Shared orb chrome and live host glyph in the real button and Classic2007 proxy](docs/history/01-library.md#3194-portable-application-orb-glyph-template--2026-09-28).

### 3.195 Shared Crystal control resources — 2026-09-28

[Promote control materials and preserve local popup overrides](docs/history/01-library.md#3195-shared-crystal-control-resources--2026-09-28).

### 3.196 Shared Crystal customization pages — 2026-09-30

[Promote Options visuals, preserve Office density and verify a RibbonKit-only consumer](docs/history/01-library.md#3196-shared-crystal-customization-pages--2026-09-30).

### 3.197 Shared Crystal QAT drawer — 2026-09-30

[Promote QAT paint, geometry and body coordination without the Showcase helper](docs/history/01-library.md#3197-shared-crystal-qat-drawer--2026-09-30).

### 3.198 Minimized Crystal QAT corners above messages — 2026-09-30

[Preserve all four minimized drawer corners without changing Office geometry](docs/history/01-library.md#3198-minimized-crystal-qat-corners-above-messages--2026-09-30).

### 3.199 Shared Crystal message bars — 2026-09-30

[Promote message actions and body rounding without the Showcase adapter](docs/history/01-library.md#3199-shared-crystal-message-bars--2026-09-30).

### 3.200 Shared Crystal application menu — 2026-09-30

[Promote menu paint, responsive geometry and outside-only shadows while preserving host capture](docs/history/01-library.md#3200-shared-crystal-application-menu--2026-09-30).

### 3.201 RTL lab bilingual application-menu header — 2026-09-30

[Wrap the lab-owned Save As label through the existing header template](docs/history/01-library.md#3201-rtl-lab-bilingual-application-menu-header--2026-09-30).

### 3.202 Application-menu footer at limited viewport height — 2026-09-30

[Bound the shared frame, scroll content and keep captured blur out of measurement](docs/history/01-library.md#3202-application-menu-footer-at-limited-viewport-height--2026-09-30).

### 3.203 Application-menu scrollbar clicks preserve the open menu — 2026-09-30

[Exempt scrollbar buttons from command-click dismissal](docs/history/01-library.md#3203-application-menu-scrollbar-clicks-preserve-the-open-menu--2026-09-30).

## 4. Workflow / Session Conventions

Use [AGENTS.md](AGENTS.md), [proportional validation](CONTRIBUTING.md#proportional-validation),
and the [WPF workflow skill](.agents/skills/ribbonkit-wpf-workflow/SKILL.md).
Keep current status here, product scope in the relevant plan, and dated evidence in
`docs/history/`. Add future numbered implementation entries to the relevant history
file and this index; update §5 only as supported by verification.

## 5. Current State & Next Steps

> Authoritative summary of recorded evidence through 2026-09-30.
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

- **200% gallery layout and RTL option-indicator failure:** the user requested
  later investigation on 2026-09-30. Gallery items reportedly change layout;
  the separate initial Office RTL checkbox-position test fails in both new and
  prior outputs. Causes remain unconfirmed. See the
  [deferred layout list](docs/13-CRYSTAL-PORTABILITY-AND-ORB-PLAN.md#deferred-layout-observations-at-200).
- **Office 2010 hover-glass contract:** the unchanged dropdown consumer-count check
  reports 3 shared consumers where its threshold is 5 (§3.169). Investigate
  the contract and dropdown coverage separately; it is deferred by user direction.
- **Expanding Paper is the default again, by user direction (§3.162).** One native editor
  sits on a centered sheet with fixed page width and minimum page height. The sheet and
  dotted margin guide grow downward with content; preview/print retain physical pagination.

- **Genuine OS IME and production RTL** remain a later paired input/geometry slice.
  W4-B proceeds with expanding-Paper integration/hardening. W4-C includes live
  mixed-monitor/DPI and physical-printer checks; earlier single-display acceptance
  does not close them. W5 remains a distribution decision after sustained use.
- **W1-E:** Home formatting and the corrected Font/Color/Paragraph dialogs have
  automated evidence through §3.119, but live visual reacceptance remains unrecorded.
  Named styles remain unsupported without a complete style/persistence contract.
- **Library:** MDI arrange/cycle commands, full MVVM proof, tabbed mode and persistence
  (M1–M3); Office 2010 Aero live visual approval (§3.97/§3.125); optional designer
  scalar reset actions; touch density, richer QAT/custom-control projections and
  future themes. Complete Windows contrast-theme support is not claimed.
- **Crystal portability and Office 2007 orb defaults:** slices 1–5 of the
  [staged integration plan](docs/13-CRYSTAL-PORTABILITY-AND-ORB-PLAN.md) now
  give Office 2007 a theme-following orb default while preserving explicit
  `ApplicationButtonShape`, verify theme-owned header insets, and expose a
  portable orb glyph template. Shared Crystal control materials now cover menu
  rows, inputs, galleries, options, ScreenTips and built-in customization pages
  in a RibbonKit-only consumer. Slice 6's QAT pass now promotes drawer paint,
  geometry, body coordination and shadows (§3.197), with 417 eligible runtime
  tests, the RibbonKit-only consumer and 77 snapshot scenes passing. A user
  screenshot exposed square upper QAT corners when minimized above a message;
  the shared combined-state correction passes the same automated gates (§3.198).
  The completed bounded live QAT review covers light/dark drawer and placement
  states, mixed-command overflow, direct/nested Paste and Select menus, Esc
  closing and reopening with all commands. Real 200% screenshots and a live
  125% → 200% → 125% change/return passed with Showcase open and Select reopened
  after the return. The user confirmed temporary-command cleanup and restoration
  of the original five-command below-ribbon QAT at 125%. The message-bar pass
  now supplies shared action chrome and message-only body rounding (§3.199),
  removing its Showcase adapter after consumer verification. The Release solution
  build, 100 focused/417 eligible runtime tests, RibbonKit-only consumer and all
  85 snapshots pass. Fresh light/dark screenshots verify expanded Home with
  TabRow QAT, two separated notices and rounded actions. The user confirmed
  independent action/close dismissal and reopening in both themes. Minimized
  below-ribbon QAT/two-notice screenshots also verify four rounded QAT corners,
  row spacing and unclipped actions in light/dark. The user confirmed independent
  minimized action dismissal and restoration after expanding, adding both notices
  and minimizing without leftover shadows. The bounded message-bar live review
  is complete; broader motion/keyboard/DPI checks remain pending. The application-menu
  pass now promotes shared paint, rounded content/split rows, responsive sizing
  and outside-only shadows (§3.200), removing its paint/geometry and shadow
  helpers. The Release solution build, 116 focused/417 eligible runtime tests,
  RibbonKit-only consumer and all 99 snapshots pass. The previous 85 images are
  unchanged; 14 new menu approvals were inspected first. Fresh light/dark
  screenshots verify the default Recent Documents page, Save As split pane and
  Publish dropdown, rounded frame/footer and host backdrop. The user confirmed
  Esc closing, Save As primary/pane and Publish pane command invocation, and
  default-page restoration on reopening in both themes. Narrow default and Save
  As pages fit in both themes, with all four descriptions wrapping cleanly and
  both footer actions visible. The user reported normal width returning after
  widening. Save As appearance above two notices with the ribbon minimized,
  Esc closing and default-page restoration are verified in both themes in
  that state. RTL default and Save As screenshots verify mirrored placement,
  mixed text and the lab's full bilingual label on two lines. Its host-owned
  wrapping header template passes both focused localization tests and fresh
  appearance review (§3.201). The user confirmed RTL Esc closing, default-page
  restoration on reopening and return to LTR in both themes, completing the
  bounded RTL review. The shared frame now follows
  available client height, with scrolling navigation/default/active content and a
  stationary footer (§3.202). Captured host blur no longer contributes desired size.
  Fresh 200% light/dark screenshots show Options/Exit fully visible; alignment
  and Esc closing after returning to 125% are confirmed in both themes. The bounded
  footer appearance/return check is complete. Live scrolling exposed arrow clicks
  dismissing File while the thumb stayed open. The shared close handler now exempts
  only ScrollBar descendants (§3.203); command clicks retain dismissal. The user
  confirmed scrollbar buttons now scroll without closing File in both themes.
  Tab navigation and Esc closing after tabbing are also confirmed by the user.
  Repeated File open/Esc/quick-reopen motion passed in both themes. The user then
  confirmed thumb/wheel/track scrolling and reverse keyboard traversal, including
  footer reachability and focus cycling. The bounded slice 6 QAT/message-bar/
  application-menu live review is complete. Reduced motion, broader DPI
  transitions and deferred layout issues remain open; blanket final acceptance
  is not claimed. Utility chrome and scrollbar promotion is next in slice 7.
  Captured blur remains host integration work; utility chrome,
  contextual tint, optional host effects and final Showcase consolidation remain
  later slices. Writer's W-glyph migration stays deferred. Slice 5 customization,
  broader DPI and remaining popup acceptance stay open.
- Automatic `Icons.xaml` discovery remains best-effort; the manual browser is the
  fallback for no match, ambiguity, inaccessible paths or parse failure.

### Verification checkpoint

- Latest cleanup: 77 focused Writer tests passed across separate runs; Release Writer build
  **0 warnings / 0 errors**. The combined window run hit the known WPF WindowChrome
  cross-thread cache issue; the affected table check passed in a fresh process.
  No full-suite, new live UI, physical-printer, OS IME or mixed-DPI acceptance is claimed.
- Latest recorded full solution gate in this checkpoint list: §3.128 (2026-08-30),
  Release build with zero warnings/errors; RibbonKit 392/392, Writer 439/439, visual
  1/1 covering 63 approved images. These are historical counts, not today's inventory.
- Earlier full/focused results and acceptance limits remain in the
  [checkpoint ledger](docs/history/verification-checkpoints.md) and numbered history.
  Rerun proportionate checks before claiming current results.
