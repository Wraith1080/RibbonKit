# Library history: Glass ribbon continuations

> Archived implementation decisions. Current evidence and human review belong in
> the [Glass plan](../../09-FUTURE-THEMES-PLAN.md#ribbon-continuation-material--2026-10-24).

[History directory](../01-library.md) · [Design index](../../../04-DESIGN-NOTES.md#3212-optional-captured-backdrops-and-scoped-glass-overlays--2026-10-01).

### Modern Office ribbon continuations — 2026-10-24

The approved material policy adds a Ribbon target and overflow-toolbar support
to the existing CapturedBackdrop constructor, without new public members.
Ribbon registration discovers groups, realized Simplified toolbars and the
active constrained QAT, including its detached title-bar host. Layout discovery
handles template/tab/placement changes; owned registrations are retired on
removal, disable, unload and disposal. Command menus, combo lists and galleries
are excluded from the Ribbon registration.

Two metrics are defined in every palette. SupportsGlassFlyouts is true only for
Office 2013/2019/2024/Flat; FlyoutGlassOpacity defaults to 1 and becomes 0.84 in
their scoped Glass overlay. Capture scales a clone of the effective frame tint,
preserving gradients, sharp foreground, local-background precedence, blur radius,
sampling margin and opaque fallback outside the supplied host. Both overflow
templates name their existing Border PART_OverflowPopupHost. Crystal's separate
capture registrations and legacy Aero behavior are unchanged.

Showcase's existing overlay owner and Writer's appearance scope own the Ribbon
registration. Writer's separate explicit popup-blur option retains dropdowns;
groups use a single registration when modern Glass is active. Legacy Glass is
suspended without clearing its saved modern choice. No persistence schema changes.

Offscreen HWND popup placement can move beyond the capture source and exercise
only opaque fallback: visible-window renders are needed to inspect actual frost.
Direct RTL frame bitmaps omit ancestor transform context; they do not establish
native RTL text acceptance. New material checks should sample the background
and command ink separately, rather than inferring contrast from a brush type.
