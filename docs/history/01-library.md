# Library implementation history

> Historical records, moved from the design notes on 2026-09-08 and compacted on 2026-10-09.
> Dates, test counts and pending work describe their original checkpoints; later records may supersede them.

Use [current status](../../04-DESIGN-NOTES.md#5-current-state--next-steps) and
[AGENTS.md](../../AGENTS.md) for present work. Concise technical records live in the bounded
archives below. The original numbered headings and their link anchors remain available here.

Keep this file as a directory. Add implementation evidence to the appropriate archive;
when an archive would exceed roughly 800 lines, start another bounded file and update
this directory and the design-note link. Record routine acceptance in its active plan.

## Archives

Numbering is shared with the [Writer history](02-writer-foundation.md); gaps are intentional.

| Records | Archive |
| --- | --- |
| §3.1–§3.21 | [Core controls](library/01-core-controls.md) |
| §3.22–§3.26 | [Designer and context menus](library/02-designer-and-context-menus.md) |
| §3.27–§3.37 | [Office themes and layout](library/03-office-themes-and-layout.md) |
| §3.38–§3.45 | [Office 2007 and popup motion](library/04-office2007-and-popup-motion.md) |
| §3.46–§3.64 | [Application menu, localization and snapshots](library/05-application-menu-localization-and-snapshots.md) |
| §3.65–§3.90 | [Messages, release and window frames](library/06-messages-release-and-window-frames.md) |
| §3.91–§3.183 | [Backstage and Crystal foundation](library/07-backstage-and-crystal-foundation.md) |
| §3.184–§3.199 | [Crystal integration and shared materials](library/08-crystal-integration-and-shared-materials.md) |
| §3.200–§3.211 | [Crystal menus, scrollbars and DPI](library/09-crystal-menus-scrollbars-and-dpi.md) |
| §3.212–§3.225 | [Host effects and keyboard focus](library/10-host-effects-and-keyboard-focus.md) |
| §3.226–§3.233 | [Touch density and gallery projections](library/11-touch-density-and-gallery-projections.md) |
| §3.234–§3.241 | [KeyTips, layout and validation](library/12-keytips-layout-and-validation.md) |

## Numbered records

Each heading opens its archived technical record. Superseded to-do lists, repeated
status updates and investigation narration were removed during compaction.

### [3.1 Core ribbon skeleton](library/01-core-controls.md#31-core-ribbon-skeleton)

### [3.2 Minimized QAT card (2024)](library/01-core-controls.md#32-minimized-qat-card-2024)

### [3.3 KeyTips (Alt / F10)](library/01-core-controls.md#33-keytips-alt--f10)

### [3.4 Contextual tabs = custom coloring (no marker line)](library/01-core-controls.md#34-contextual-tabs--custom-coloring-no-marker-line)

### [3.5 Colored title bar + accent customization](library/01-core-controls.md#35-colored-title-bar--accent-customization)

### [3.6 2019 modernization & hover consistency](library/01-core-controls.md#36-2019-modernization--hover-consistency)

### [3.7 Large-button label alignment](library/01-core-controls.md#37-large-button-label-alignment)

### [3.8 QAT placement (TitleBar / TabRow / BelowRibbon) + context menu](library/01-core-controls.md#38-qat-placement-titlebar--tabrow--belowribbon--context-menu)

### [3.9 QAT white icons on colored surfaces](library/01-core-controls.md#39-qat-white-icons-on-colored-surfaces)

### [3.10 Animation system (global + per-action)](library/01-core-controls.md#310-animation-system-global--per-action)

### [3.11 Backstage redesign (Modern 2024) + icons](library/01-core-controls.md#311-backstage-redesign-modern-2024--icons)

### [3.12 Mica (Windows 11 system backdrop) — EXPERIMENTAL](library/01-core-controls.md#312-mica-windows-11-system-backdrop--experimental)

### [3.13 UI polish fixes](library/01-core-controls.md#313-ui-polish-fixes)

### [3.14 XAML design-time preview (active tab + backstage)](library/01-core-controls.md#314-xaml-design-time-preview-active-tab--backstage)

### [3.15 QAT customization + extensible options dialog (Word-Options style)](library/01-core-controls.md#315-qat-customization--extensible-options-dialog-word-options-style)

### [3.16 "Customize the Ribbon" structure page](library/01-core-controls.md#316-customize-the-ribbon-structure-page)

### [3.17 Customization persistence (serialize / restore / Reset)](library/01-core-controls.md#317-customization-persistence-serialize--restore--reset)

### [3.18 QAT/dialog polish batch (context menus, persistence, maximize, layout, hover)](library/01-core-controls.md#318-qatdialog-polish-batch-context-menus-persistence-maximize-layout-hover)

### [3.19 Dropdown proxies (real dropdown that borrows the source's menu)](library/01-core-controls.md#319-dropdown-proxies-real-dropdown-that-borrows-the-sources-menu)

### [3.20 Large-button label: inline dropdown chevron + multi-line ellipsis](library/01-core-controls.md#320-large-button-label-inline-dropdown-chevron--multi-line-ellipsis)

### [3.21 Backstage: footer items, button items, design-time page preview](library/01-core-controls.md#321-backstage-footer-items-button-items-design-time-page-preview)

### [3.22 Design-time smart tags / quick actions (XAML designer) — VERIFIED IN VS](library/02-designer-and-context-menus.md#322-design-time-smart-tags--quick-actions-xaml-designer--verified-in-vs)

### [3.23 Ribbon Editor dialog (design-time) + tab-preview feasibility](library/02-designer-and-context-menus.md#323-ribbon-editor-dialog-design-time--tab-preview-feasibility)

### [3.24 Animation polish batch — all six remaining transitions wired](library/02-designer-and-context-menus.md#324-animation-polish-batch--all-six-remaining-transitions-wired)

### [3.25 Ribbon horizontal scroll (tab strip + groups row)](library/02-designer-and-context-menus.md#325-ribbon-horizontal-scroll-tab-strip--groups-row)

### [3.26 Modern context menus (ribbon-item + QAT right-click)](library/02-designer-and-context-menus.md#326-modern-context-menus-ribbon-item--qat-right-click)

### [3.27 Office 2010 ("Blue") theme — the first gradient theme](library/03-office-themes-and-layout.md#327-office-2010-blue-theme--the-first-gradient-theme)

### [3.28 Backstage page-text colour + ribbon focus (RichTextBox) — 2026-07-21](library/03-office-themes-and-layout.md#328-backstage-page-text-colour--ribbon-focus-richtextbox--2026-07-21)

### [3.29 Connected-tab body-border cut (2010/2013) — the "notch" — 2026-07-23](library/03-office-themes-and-layout.md#329-connected-tab-body-border-cut-20102013--the-notch--2026-07-23)

### [3.30 Backstage Mica pass-through (Modern/2024) — hide, don't blur — 2026-07-23](library/03-office-themes-and-layout.md#330-backstage-mica-pass-through-modern2024--hide-dont-blur--2026-07-23)

### [3.31 Office 2024 default (Auto) accent aligned to #2B579A — 2026-07-23](library/03-office-themes-and-layout.md#331-office-2024-default-auto-accent-aligned-to-2b579a--2026-07-23)

### [3.32 Modal tabs (Print-Preview mode) — Phase 7 P7.1 — 2026-07-27](library/03-office-themes-and-layout.md#332-modal-tabs-print-preview-mode--phase-7-p71--2026-07-27)

### [3.33 Tab merging + group contributions — Phase 7 P7.2/P7.3 — 2026-07-27](library/03-office-themes-and-layout.md#333-tab-merging--group-contributions--phase-7-p72p73--2026-07-27)

### [3.34 MDI ⇄ ribbon integration: tab merge + caption merge — MDI M4 — 2026-07-27](library/03-office-themes-and-layout.md#334-mdi--ribbon-integration-tab-merge--caption-merge--mdi-m4--2026-07-27)

### [3.35 Quick access toolbar overflow — 2026-07-27](library/03-office-themes-and-layout.md#335-quick-access-toolbar-overflow--2026-07-27)

### [3.36 Selection visuals: the tab-strip-row reflow rule](library/03-office-themes-and-layout.md#336-selection-visuals-the-tab-strip-row-reflow-rule)

### [3.37 Splitting Office2024.xaml into Controls.*.xaml parts — 2026-07-27 — ⚗ EXPERIMENTAL](library/03-office-themes-and-layout.md#337-splitting-office2024xaml-into-controlsxaml-parts--2026-07-27---experimental)

### [3.38 Office 2007 theme — the last generation, and the one that changed the templates — 2026-07-27](library/04-office2007-and-popup-motion.md#338-office-2007-theme--the-last-generation-and-the-one-that-changed-the-templates--2026-07-27)

### [3.39 Stranded menus, take two: the borrow must not hang off `Popup.Closed` — 2026-07-27](library/04-office2007-and-popup-motion.md#339-stranded-menus-take-two-the-borrow-must-not-hang-off-popupclosed--2026-07-27)

### [3.40 Chrome polish batch — pressed states, caption glass, and four hijacked tokens — 2026-07-28](library/04-office2007-and-popup-motion.md#340-chrome-polish-batch--pressed-states-caption-glass-and-four-hijacked-tokens--2026-07-28)

### [3.41 Two ordering bugs: an Effect painted over, and a FLIP that flashed — 2026-07-28](library/04-office2007-and-popup-motion.md#341-two-ordering-bugs-an-effect-painted-over-and-a-flip-that-flashed--2026-07-28)

### [3.42 Every flyout now opens as a whole surface — and the DPI manifest — 2026-07-28](library/04-office2007-and-popup-motion.md#342-every-flyout-now-opens-as-a-whole-surface--and-the-dpi-manifest--2026-07-28)

### [3.43 Split button: a vertical arrangement, and halves that acknowledge each other — 2026-07-28](library/04-office2007-and-popup-motion.md#343-split-button-a-vertical-arrangement-and-halves-that-acknowledge-each-other--2026-07-28)

### [3.44 The shared easing function was never frozen — 2026-07-28](library/04-office2007-and-popup-motion.md#344-the-shared-easing-function-was-never-frozen--2026-07-28)

### [3.45 Proxies never mirrored their source's enabled state — 2026-07-28](library/04-office2007-and-popup-motion.md#345-proxies-never-mirrored-their-sources-enabled-state--2026-07-28)

### [3.46 The real Office 2007 application menu — a control that had to sit BEHIND the orb — 2026-07-28](library/05-application-menu-localization-and-snapshots.md#346-the-real-office-2007-application-menu--a-control-that-had-to-sit-behind-the-orb--2026-07-28)

### [3.47 Nested popup Escape is a stack, not five independent window handlers — 2026-08-01](library/05-application-menu-localization-and-snapshots.md#347-nested-popup-escape-is-a-stack-not-five-independent-window-handlers--2026-08-01)

### [3.48 Visual-regression snapshots: the complete theme × DPI matrix — 2026-08-01](library/05-application-menu-localization-and-snapshots.md#348-visual-regression-snapshots-the-complete-theme--dpi-matrix--2026-08-01)

### [3.49 Dark/Black variants for every generation — 2026-08-01/02](library/05-application-menu-localization-and-snapshots.md#349-darkblack-variants-for-every-generation--2026-08-0102)

### [3.50 Dark live-switch corrections from the 100% showcase pass — 2026-08-01](library/05-application-menu-localization-and-snapshots.md#350-dark-live-switch-corrections-from-the-100-showcase-pass--2026-08-01)

### [3.51 First deterministic RTL snapshot slice — 2026-08-01](library/05-application-menu-localization-and-snapshots.md#351-first-deterministic-rtl-snapshot-slice--2026-08-01)

### [3.52 Localization foundation + RTL ribbon context menus — 2026-08-02](library/05-application-menu-localization-and-snapshots.md#352-localization-foundation--rtl-ribbon-context-menus--2026-08-02)

### [3.53 Customize/Options localization + RTL action snapshot — 2026-08-02](library/05-application-menu-localization-and-snapshots.md#353-customizeoptions-localization--rtl-action-snapshot--2026-08-02)

### [3.54 Chrome tooltip localization — 2026-08-02](library/05-application-menu-localization-and-snapshots.md#354-chrome-tooltip-localization--2026-08-02)

### [3.55 Live localized default File label — 2026-08-02](library/05-application-menu-localization-and-snapshots.md#355-live-localized-default-file-label--2026-08-02)

### [3.56 Localized conventional application-menu footer actions — 2026-08-02](library/05-application-menu-localization-and-snapshots.md#356-localized-conventional-application-menu-footer-actions--2026-08-02)

### [3.57 Application-button width now reflows the selection visuals — 2026-08-02](library/05-application-menu-localization-and-snapshots.md#357-application-button-width-now-reflows-the-selection-visuals--2026-08-02)

### [3.58 Representative bidirectional content in RTL Backstage — 2026-08-03](library/05-application-menu-localization-and-snapshots.md#358-representative-bidirectional-content-in-rtl-backstage--2026-08-03)

### [3.59 Live RTL Backstage rail, slide and title transition — 2026-08-08](library/05-application-menu-localization-and-snapshots.md#359-live-rtl-backstage-rail-slide-and-title-transition--2026-08-08)

### [3.60 Localization/RTL lab follows the Showcase File-surface policy — 2026-08-08](library/05-application-menu-localization-and-snapshots.md#360-localizationrtl-lab-follows-the-showcase-file-surface-policy--2026-08-08)

### [3.61 Live RTL popup/window verification closes Phase 6 — 2026-08-08](library/05-application-menu-localization-and-snapshots.md#361-live-rtl-popupwindow-verification-closes-phase-6--2026-08-08)

### [3.62 Flat-theme application-menu footer buttons retain their outline — 2026-08-08](library/05-application-menu-localization-and-snapshots.md#362-flat-theme-application-menu-footer-buttons-retain-their-outline--2026-08-08)

### [3.63 Failure-only CI artifacts for cross-machine snapshot diagnosis — 2026-08-08](library/05-application-menu-localization-and-snapshots.md#363-failure-only-ci-artifacts-for-cross-machine-snapshot-diagnosis--2026-08-08)

### [3.63a Responsive Ribbon Editor + application-menu authoring — VERIFIED IN VS, 2026-08-08](library/05-application-menu-localization-and-snapshots.md#363a-responsive-ribbon-editor--application-menu-authoring--verified-in-vs-2026-08-08)

### [3.64 Backstage sheet depth and the 2010 colored caption — 2026-08-08](library/05-application-menu-localization-and-snapshots.md#364-backstage-sheet-depth-and-the-2010-colored-caption--2026-08-08)

### [3.65 Repeatable Ribbon message bar — 2026-08-08](library/06-messages-release-and-window-frames.md#365-repeatable-ribbon-message-bar--2026-08-08)

### [3.66 Dark/Black Backstage rails use generation-matched neutrals â€” 2026-08-08](library/06-messages-release-and-window-frames.md#366-darkblack-backstage-rails-use-generation-matched-neutrals-â-2026-08-08)

### [3.67 Compact RibbonCheckBox and RibbonRadioButton — 2026-08-08](library/06-messages-release-and-window-frames.md#367-compact-ribboncheckbox-and-ribbonradiobutton--2026-08-08)

### [3.68 Compact RibbonTextBox — 2026-08-08](library/06-messages-release-and-window-frames.md#368-compact-ribbontextbox--2026-08-08)

### [3.69 KeyTip resolution is typeable and prefix-free — 2026-08-08](library/06-messages-release-and-window-frames.md#369-keytip-resolution-is-typeable-and-prefix-free--2026-08-08)

### [3.70 Explicit KeyTips inside arbitrary File-surface content — 2026-08-08](library/06-messages-release-and-window-frames.md#370-explicit-keytips-inside-arbitrary-file-surface-content--2026-08-08)

### [3.71 Showcase appearance preferences stay separate from ribbon customization — 2026-08-10](library/06-messages-release-and-window-frames.md#371-showcase-appearance-preferences-stay-separate-from-ribbon-customization--2026-08-10)

### [3.72 Customization serializer round-trip and foreign-JSON hardening — 2026-08-10](library/06-messages-release-and-window-frames.md#372-customization-serializer-round-trip-and-foreign-json-hardening--2026-08-10)

### [3.73 Reduction thresholds, priority order, and malformed-width coverage — 2026-08-10](library/06-messages-release-and-window-frames.md#373-reduction-thresholds-priority-order-and-malformed-width-coverage--2026-08-10)

### [3.74 Visual Studio debugger distorted live-resize performance — 2026-08-10](library/06-messages-release-and-window-frames.md#374-visual-studio-debugger-distorted-live-resize-performance--2026-08-10)

### [3.75 Phase 8 API review and freeze — 2026-08-10](library/06-messages-release-and-window-frames.md#375-phase-8-api-review-and-freeze--2026-08-10)

### [3.76 Repository-native v1 documentation gate — 2026-08-10](library/06-messages-release-and-window-frames.md#376-repository-native-v1-documentation-gate--2026-08-10)

### [3.77 NuGet and Showcase identity icon — 2026-08-11](library/06-messages-release-and-window-frames.md#377-nuget-and-showcase-identity-icon--2026-08-11)

### [3.78 Office 2007 application-menu open layout stability — 2026-08-11](library/06-messages-release-and-window-frames.md#378-office-2007-application-menu-open-layout-stability--2026-08-11)

### [3.79 Source Link and symbol package verification — 2026-08-11](library/06-messages-release-and-window-frames.md#379-source-link-and-symbol-package-verification--2026-08-11)

### [3.80 Portable local package output — 2026-08-11](library/06-messages-release-and-window-frames.md#380-portable-local-package-output--2026-08-11)

### [3.81 Deterministic package versioning — 2026-08-11](library/06-messages-release-and-window-frames.md#381-deterministic-package-versioning--2026-08-11)

### [3.82 NuGet package and clean-consumer release gate — 2026-08-11](library/06-messages-release-and-window-frames.md#382-nuget-package-and-clean-consumer-release-gate--2026-08-11)

### [3.83 Final live performance and installed-package pass — 2026-08-11](library/06-messages-release-and-window-frames.md#383-final-live-performance-and-installed-package-pass--2026-08-11)

### [3.84 Local v1.0.0 GitHub-release candidate — 2026-08-11](library/06-messages-release-and-window-frames.md#384-local-v100-github-release-candidate--2026-08-11)

### [3.85 GitHub v1.0.0 community release — 2026-08-11](library/06-messages-release-and-window-frames.md#385-github-v100-community-release--2026-08-11)

### [3.86 Post-v1 custom-control projection and future-theme plans — 2026-08-12](library/06-messages-release-and-window-frames.md#386-post-v1-custom-control-projection-and-future-theme-plans--2026-08-12)

### [3.87 RibbonKit Writer functional reference-app plan — 2026-08-12](library/06-messages-release-and-window-frames.md#387-ribbonkit-writer-functional-reference-app-plan--2026-08-12)

### [3.87a RibbonKit Writer Luna execution decomposition — 2026-08-20](library/06-messages-release-and-window-frames.md#387a-ribbonkit-writer-luna-execution-decomposition--2026-08-20)

### [3.88 Office 2007 opaque and Aero-inspired window-frame plan — 2026-08-12](library/06-messages-release-and-window-frames.md#388-office-2007-opaque-and-aero-inspired-window-frame-plan--2026-08-12)

### [3.89 Office 2007 opaque window baseline — reference correction — 2026-08-12](library/06-messages-release-and-window-frames.md#389-office-2007-opaque-window-baseline--reference-correction--2026-08-12)

### [3.90 Office 2007 Aero-inspired window-frame prototype — 2026-08-12](library/06-messages-release-and-window-frames.md#390-office-2007-aero-inspired-window-frame-prototype--2026-08-12)

### [3.91 RibbonKit-original Glass2007 Backstage prototype — 2026-08-12](library/07-backstage-and-crystal-foundation.md#391-ribbonkit-original-glass2007-backstage-prototype--2026-08-12)

### [3.92 Separate Classic2007 Backstage concept — 2026-08-13](library/07-backstage-and-crystal-foundation.md#392-separate-classic2007-backstage-concept--2026-08-13)

### [3.93 S7 Office 2007 window-frame final verification — 2026-08-13](library/07-backstage-and-crystal-foundation.md#393-s7-office-2007-window-frame-final-verification--2026-08-13)

### [3.94 Classic2007 Backstage orb proxy and seamless design switching — 2026-08-13](library/07-backstage-and-crystal-foundation.md#394-classic2007-backstage-orb-proxy-and-seamless-design-switching--2026-08-13)

### [3.95 Office 2010 Backstage begins below the tab headers — 2026-08-13](library/07-backstage-and-crystal-foundation.md#395-office-2010-backstage-begins-below-the-tab-headers--2026-08-13)

### [3.96 Office 2010 Backstage tab-strip polish — 2026-08-13](library/07-backstage-and-crystal-foundation.md#396-office-2010-backstage-tab-strip-polish--2026-08-13)

### [3.97 Office 2010 Aero-inspired frame prototype — 2026-08-20](library/07-backstage-and-crystal-foundation.md#397-office-2010-aero-inspired-frame-prototype--2026-08-20)

### [3.98 Collapsed-group and Classic2007 follow-up fixes — 2026-08-20](library/07-backstage-and-crystal-foundation.md#398-collapsed-group-and-classic2007-follow-up-fixes--2026-08-20)

### [3.164 Crystal contextual tab study and compact button refinement — 2026-09-21](library/07-backstage-and-crystal-foundation.md#3164-crystal-contextual-tab-study-and-compact-button-refinement--2026-09-21)

### [3.165 Crystal Light theme selection — 2026-09-24](library/07-backstage-and-crystal-foundation.md#3165-crystal-light-theme-selection--2026-09-24)

### [3.166 Crystal main Showcase presentation — 2026-09-24](library/07-backstage-and-crystal-foundation.md#3166-crystal-main-showcase-presentation--2026-09-24)

### [3.167 Crystal Light first-phase gaps — 2026-09-24](library/07-backstage-and-crystal-foundation.md#3167-crystal-light-first-phase-gaps--2026-09-24)

### [3.168 Crystal Backstage designs and shared split hover — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3168-crystal-backstage-designs-and-shared-split-hover--2026-09-27)

### [3.169 Crystal File hover rim in the shared template — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3169-crystal-file-hover-rim-in-the-shared-template--2026-09-27)

### [3.170 Minimized tab shape in the shared template — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3170-minimized-tab-shape-in-the-shared-template--2026-09-27)

### [3.171 Crystal body-scroll geometry in the shared template — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3171-crystal-body-scroll-geometry-in-the-shared-template--2026-09-27)

### [3.172 Compact Showcase theme and Backstage choices — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3172-compact-showcase-theme-and-backstage-choices--2026-09-27)

### [3.173 Crystal Acrylic surface transparency — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3173-crystal-acrylic-surface-transparency--2026-09-27)

### [3.174 Optional Acrylic glass treatment across Showcase themes — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3174-optional-acrylic-glass-treatment-across-showcase-themes--2026-09-27)

### [3.175 Crystal translucent Backstage option — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3175-crystal-translucent-backstage-option--2026-09-27)

### [3.176 Acrylic hover accent and File wash — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3176-acrylic-hover-accent-and-file-wash--2026-09-27)

### [3.177 Crystal selected-tab bridge — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3177-crystal-selected-tab-bridge--2026-09-27)

### [3.178 Crystal hover tab closes below the header — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3178-crystal-hover-tab-closes-below-the-header--2026-09-27)

### [3.179 Crystal hover and selected tabs share an open lower edge — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3179-crystal-hover-and-selected-tabs-share-an-open-lower-edge--2026-09-27)

### [3.180 Crystal hover foot clipping — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3180-crystal-hover-foot-clipping--2026-09-27)

### [3.181 Crystal hover uses one translucent surface — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3181-crystal-hover-uses-one-translucent-surface--2026-09-27)

### [3.182 Crystal hover rounding and accent-tinted QAT glass — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3182-crystal-hover-rounding-and-accent-tinted-qat-glass--2026-09-27)

### [3.183 Glass tab hover follows the accent — 2026-09-27](library/07-backstage-and-crystal-foundation.md#3183-glass-tab-hover-follows-the-accent--2026-09-27)

### [3.184 Crystal Light MDI Demo integration — 2026-09-27](library/08-crystal-integration-and-shared-materials.md#3184-crystal-light-mdi-demo-integration--2026-09-27)

### [3.185 Connected first tab with no File button — 2026-09-27](library/08-crystal-integration-and-shared-materials.md#3185-connected-first-tab-with-no-file-button--2026-09-27)

### [3.186 Crystal Light Localization/RTL lab and Backstage templates — 2026-09-28](library/08-crystal-integration-and-shared-materials.md#3186-crystal-light-localizationrtl-lab-and-backstage-templates--2026-09-28)

### [3.187 Crystal Light Print Preview host paint — 2026-09-28](library/08-crystal-integration-and-shared-materials.md#3187-crystal-light-print-preview-host-paint--2026-09-28)

### [3.188 Crystal dark palette — 2026-09-28](library/08-crystal-integration-and-shared-materials.md#3188-crystal-dark-palette--2026-09-28)

### [3.189 Crystal duplicate audit — 2026-09-28](library/08-crystal-integration-and-shared-materials.md#3189-crystal-duplicate-audit--2026-09-28)

### [3.190 Crystal customization list frames — 2026-09-28](library/08-crystal-integration-and-shared-materials.md#3190-crystal-customization-list-frames--2026-09-28)

### [3.191 Showcase presentation portability audit — 2026-09-28](library/08-crystal-integration-and-shared-materials.md#3191-showcase-presentation-portability-audit--2026-09-28)

### [3.192 Theme-native Office 2007 orb default — 2026-09-28](library/08-crystal-integration-and-shared-materials.md#3192-theme-native-office-2007-orb-default--2026-09-28)

### [3.193 Theme-owned no-application header inset and snapshot renewal — 2026-09-28](library/08-crystal-integration-and-shared-materials.md#3193-theme-owned-no-application-header-inset-and-snapshot-renewal--2026-09-28)

### [3.194 Portable application-orb glyph template — 2026-09-28](library/08-crystal-integration-and-shared-materials.md#3194-portable-application-orb-glyph-template--2026-09-28)

### [3.195 Shared Crystal control resources — 2026-09-28](library/08-crystal-integration-and-shared-materials.md#3195-shared-crystal-control-resources--2026-09-28)

### [3.196 Shared Crystal customization pages — 2026-09-30](library/08-crystal-integration-and-shared-materials.md#3196-shared-crystal-customization-pages--2026-09-30)

### [3.197 Shared Crystal QAT drawer — 2026-09-30](library/08-crystal-integration-and-shared-materials.md#3197-shared-crystal-qat-drawer--2026-09-30)

### [3.198 Minimized Crystal QAT corners above messages — 2026-09-30](library/08-crystal-integration-and-shared-materials.md#3198-minimized-crystal-qat-corners-above-messages--2026-09-30)

### [3.199 Shared Crystal message bars — 2026-09-30](library/08-crystal-integration-and-shared-materials.md#3199-shared-crystal-message-bars--2026-09-30)

### [3.200 Shared Crystal application menu — 2026-09-30](library/09-crystal-menus-scrollbars-and-dpi.md#3200-shared-crystal-application-menu--2026-09-30)

### [3.201 RTL lab bilingual application-menu header — 2026-09-30](library/09-crystal-menus-scrollbars-and-dpi.md#3201-rtl-lab-bilingual-application-menu-header--2026-09-30)

### [3.202 Application-menu footer at limited viewport height — 2026-09-30](library/09-crystal-menus-scrollbars-and-dpi.md#3202-application-menu-footer-at-limited-viewport-height--2026-09-30)

### [3.203 Application-menu scrollbar clicks preserve the open menu — 2026-09-30](library/09-crystal-menus-scrollbars-and-dpi.md#3203-application-menu-scrollbar-clicks-preserve-the-open-menu--2026-09-30)

### [3.204 Shared Crystal utility buttons and scrollbars — 2026-09-30](library/09-crystal-menus-scrollbars-and-dpi.md#3204-shared-crystal-utility-buttons-and-scrollbars--2026-09-30)

### [3.205 Customization scrollbar insets and slice 7 live review — 2026-09-30](library/09-crystal-menus-scrollbars-and-dpi.md#3205-customization-scrollbar-insets-and-slice-7-live-review--2026-09-30)

### [3.206 Customization scrollbar pixel gaps — 2026-10-01](library/09-crystal-menus-scrollbars-and-dpi.md#3206-customization-scrollbar-pixel-gaps--2026-10-01)

### [3.207 Slice 7 bounded live review complete — 2026-10-01](library/09-crystal-menus-scrollbars-and-dpi.md#3207-slice-7-bounded-live-review-complete--2026-10-01)

### [3.208 Shared Crystal contextual material and scoped palettes — 2026-10-01](library/09-crystal-menus-scrollbars-and-dpi.md#3208-shared-crystal-contextual-material-and-scoped-palettes--2026-10-01)

### [3.209 Popup border and shadow DPI diagnostics — 2026-10-01](library/09-crystal-menus-scrollbars-and-dpi.md#3209-popup-border-and-shadow-dpi-diagnostics--2026-10-01)

### [3.210 Popup margins follow the DPI pixel grid — 2026-10-01](library/09-crystal-menus-scrollbars-and-dpi.md#3210-popup-margins-follow-the-dpi-pixel-grid--2026-10-01)

### [3.211 Vertical split hover width and diagnostic cleanup — 2026-10-01](library/09-crystal-menus-scrollbars-and-dpi.md#3211-vertical-split-hover-width-and-diagnostic-cleanup--2026-10-01)

### [3.212 Optional captured backdrops and scoped glass overlays — 2026-10-01](library/10-host-effects-and-keyboard-focus.md#3212-optional-captured-backdrops-and-scoped-glass-overlays--2026-10-01)

### [3.213 Main Showcase consolidation — 2026-10-01](library/10-host-effects-and-keyboard-focus.md#3213-main-showcase-consolidation--2026-10-01)

### [3.214 Theme gallery clipping and deferred test corrections — 2026-10-01](library/10-host-effects-and-keyboard-focus.md#3214-theme-gallery-clipping-and-deferred-test-corrections--2026-10-01)

### [3.215 Gallery selected-row retention after tab reload — 2026-10-01](library/10-host-effects-and-keyboard-focus.md#3215-gallery-selected-row-retention-after-tab-reload--2026-10-01)

### [3.217 Crystal pre-merge scope and resource cleanup — 2026-10-04](library/10-host-effects-and-keyboard-focus.md#3217-crystal-pre-merge-scope-and-resource-cleanup--2026-10-04)

### [3.218 Keyboard focus and application-menu navigation — 2026-10-04](library/10-host-effects-and-keyboard-focus.md#3218-keyboard-focus-and-application-menu-navigation--2026-10-04)

### [3.219 QAT order, menu traversal and Backstage focus cycling — 2026-10-04](library/10-host-effects-and-keyboard-focus.md#3219-qat-order-menu-traversal-and-backstage-focus-cycling--2026-10-04)

### [3.220 File-first entry, QAT overflow and Backstage opening focus — 2026-10-04](library/10-host-effects-and-keyboard-focus.md#3220-file-first-entry-qat-overflow-and-backstage-opening-focus--2026-10-04)

### [3.221 Active Backstage entry and a single focus outline — 2026-10-04](library/10-host-effects-and-keyboard-focus.md#3221-active-backstage-entry-and-a-single-focus-outline--2026-10-04)

### [3.222 Animated focus placement and collapsed-group navigation — 2026-10-04](library/10-host-effects-and-keyboard-focus.md#3222-animated-focus-placement-and-collapsed-group-navigation--2026-10-04)

### [3.223 Circular Back and application-orb focus — 2026-10-04](library/10-host-effects-and-keyboard-focus.md#3223-circular-back-and-application-orb-focus--2026-10-04)

### [3.224 Focus outlines follow rendered button surfaces — 2026-10-05](library/10-host-effects-and-keyboard-focus.md#3224-focus-outlines-follow-rendered-button-surfaces--2026-10-05)

## [3.225 Focus contrast on accent-filled rails and headers — 2026-10-05](library/10-host-effects-and-keyboard-focus.md#3225-focus-contrast-on-accent-filled-rails-and-headers--2026-10-05)

## [3.226 Shared Compact/Touch density — 2026-10-07](library/11-touch-density-and-gallery-projections.md#3226-shared-compacttouch-density--2026-10-07)

### [3.227 Gallery popup headings and QAT-only command recovery — 2026-10-08](library/11-touch-density-and-gallery-projections.md#3227-gallery-popup-headings-and-qat-only-command-recovery--2026-10-08)

### [3.228 Grouped Theme gallery with an independent popup width — 2026-10-08](library/11-touch-density-and-gallery-projections.md#3228-grouped-theme-gallery-with-an-independent-popup-width--2026-10-08)

### [3.229 Combo-box dropdown copies for QAT and customization — 2026-10-08](library/11-touch-density-and-gallery-projections.md#3229-combo-box-dropdown-copies-for-qat-and-customization--2026-10-08)

### [3.230 Gallery dropdown copies for QAT and customization — 2026-10-08](library/11-touch-density-and-gallery-projections.md#3230-gallery-dropdown-copies-for-qat-and-customization--2026-10-08)

### [3.231 Native gallery direction changes, popup origin and strip retention — 2026-10-08](library/11-touch-density-and-gallery-projections.md#3231-native-gallery-direction-changes-popup-origin-and-strip-retention--2026-10-08)

### [3.232 Native gallery dismissal preserves the viewed row — 2026-10-09](library/11-touch-density-and-gallery-projections.md#3232-native-gallery-dismissal-preserves-the-viewed-row--2026-10-09)

### [3.233 Sibling popup opening preserves other gallery rows — 2026-10-09](library/11-touch-density-and-gallery-projections.md#3233-sibling-popup-opening-preserves-other-gallery-rows--2026-10-09)

### [3.234 Combo-copy KeyTips hand control to the choice list — 2026-10-09](library/12-keytips-layout-and-validation.md#3234-combo-copy-keytips-hand-control-to-the-choice-list--2026-10-09)

### [3.235 Touch stacked custom groups keep three rows per column — 2026-10-09](library/12-keytips-layout-and-validation.md#3235-touch-stacked-custom-groups-keep-three-rows-per-column--2026-10-09)

### [3.236 Touch horizontal split buttons retain a larger primary action — 2026-10-09](library/12-keytips-layout-and-validation.md#3236-touch-horizontal-split-buttons-retain-a-larger-primary-action--2026-10-09)

### [3.237 Gallery row scrolling stays aligned after direction reversals — 2026-10-09](library/12-keytips-layout-and-validation.md#3237-gallery-row-scrolling-stays-aligned-after-direction-reversals--2026-10-09)

### [3.238 Focused local validation and visual scene selection — 2026-10-09](library/12-keytips-layout-and-validation.md#3238-focused-local-validation-and-visual-scene-selection--2026-10-09)

### [3.239 Test suite redundancy review — 2026-10-09](library/12-keytips-layout-and-validation.md#3239-test-suite-redundancy-review--2026-10-09)

### [3.240 Modern Office glass surfaces and legacy Aero choices — 2026-10-11](library/12-keytips-layout-and-validation.md#3240-modern-office-glass-surfaces-and-legacy-aero-choices--2026-10-11)

### [3.241 Gallery header contrast and balanced section spacing — 2026-10-11](library/12-keytips-layout-and-validation.md#3241-gallery-header-contrast-and-balanced-section-spacing--2026-10-11)
