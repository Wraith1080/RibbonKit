# Professional Ribbon Custom Control for WPF — Planning Overview

> Status: Planning · Last updated: 2026-07-02
> Decisions locked: **Name: RibbonKit** · **.NET 8/9 only** · **Open source** · **Milestone 1 = core ribbon skeleton**

## 1. Vision

A modern, open-source WPF custom control library that recreates the Office Fluent UI Ribbon with high visual fidelity and a complete feature set: tabs, groups, adaptive button sizing, split/dropdown buttons, galleries, contextual tabs, backstage view, application button, quick access toolbar (QAT), combo boxes, minimize mode, tab merging, modal tabs, a runtime customize dialog, five Office theme generations (2007, 2010, 2013, 2019, 2024), per-monitor High-DPI support, and a first-class XAML design-time experience.

## 2. Guiding principles

The library is built as **lookless custom controls** (templated in `Generic.xaml` and theme dictionaries), never as UserControls, so consumers can restyle everything. The public API follows WPF conventions — dependency properties, routed events, `ICommand` support, `ItemsSource`/data-template friendliness — so the ribbon feels native to WPF developers and works cleanly with MVVM. Visual fidelity to real Office is a feature, not an afterthought: each theme is verified side-by-side against the corresponding Office release. Everything renders from vectors (geometries/`DrawingImage`), never bitmaps, so DPI scaling is free. Accessibility (UI Automation peers, keyboard navigation, KeyTips) is designed in from the start because it cannot be bolted on later.

## 3. Existing landscape (know before building)

Microsoft's built-in `System.Windows.Controls.Ribbon` is dated, visually stuck around Office 2010, and effectively unmaintained. **Fluent.Ribbon** is the leading open-source alternative and a valuable architectural reference (its size-reduction and KeyTip systems are worth studying). Commercial suites (DevExpress, Syncfusion, Telerik, Actipro) set the bar for polish and the customize dialog. Our differentiation: the full five-theme range including Office 2024, modern .NET only (no legacy baggage), tab merging and modal tabs (rare in open source), and a cleaner MVVM-first API. Pick a distinct name and namespace early to avoid collision with Fluent.Ribbon (see Phase 0 in the roadmap).

## 4. What we need to plan (the complete checklist)

**Product decisions** — name, license (MIT recommended), target framework (locked: net8.0-windows, add net9.0-windows), versioning policy (SemVer), what "v1.0" means.

**Repository & infrastructure** — GitHub repo layout, CI (build + test + package on every PR), NuGet publishing pipeline, demo/showcase app, contribution docs, issue templates, API docs generation.

**Architecture** — control class hierarchy, the adaptive sizing/layout engine (the hardest problem in the project), theming system, window-chrome integration, KeyTip/keyboard system, state persistence, design-time support. Detailed in `01-ARCHITECTURE.md`.

**Feature inventory & prioritization** — every control and behavior, sequenced so each phase produces something usable. Detailed in `02-FEATURES.md`.

**Roadmap** — phased milestones from empty repo to v1.0. Detailed in `03-ROADMAP.md`.

**Quality strategy** — unit tests for layout logic, visual regression snapshots per theme, UIA/accessibility audits, DPI test matrix (100/125/150/200%, mixed monitors), sample-app dogfooding.

## 5. Top technical risks (plan mitigation early)

1. **Adaptive size-reduction layout** — groups must shrink large→medium→small→collapsed in a defined order as width changes, without flicker, measured efficiently. This is the make-or-break subsystem; prototype it in Phase 1, not later.
2. **Window chrome integration** — QAT and contextual-tab headers live in the title bar in real Office. Custom `WindowChrome` interacts with DPI, maximize behavior, and snap layouts in fiddly ways.
3. **KeyTips** — a global keyboard-mode overlay with chained activation (Alt → H → F → S). Requires careful input routing and dismissal logic.
4. **Gallery virtualization** — large galleries (styles, colors) must virtualize inside popups without breaking keyboard nav or resizing.
5. **Theme-count maintenance** — 5+ themes multiply every visual bug. Mitigate with a shared token/resource layer so themes override colors and metrics, not templates.

## 6. Document map

| File | Contents |
|---|---|
| `00-PLANNING-OVERVIEW.md` | This document — vision, principles, risks |
| `01-ARCHITECTURE.md` | Solution structure, control hierarchy, subsystems |
| `02-FEATURES.md` | Full feature breakdown with priorities |
| `03-ROADMAP.md` | Phased milestones to v1.0 |
