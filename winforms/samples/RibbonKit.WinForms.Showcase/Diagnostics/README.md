# Showcase diagnostics

Capture, tracing and regression-check implementations live here. Feature setup,
application handlers, persistence and the normal Designer files remain beside
`MainForm.cs`.

These files are parts of the same `MainForm` partial class, compiled by the normal
project. They inspect the actual Showcase controls rather than creating a second
sample hierarchy. `Program.cs` dispatches the diagnostic command switches.
A normal launch does not run them. `--capture-backstage-opening <directory>`
checks Compact/Touch opening and hover with Solid, Mica and Acrylic, including
clear/opaque material rails. It samples compositor frames for tall black strips
and checks that Showcase visibly owns the capture area. Launch the compiled
Showcase executable so its native compatibility manifest applies.

`--capture-backstage-opaque <directory>` records complete opening/closing slides
for Mica/Acrylic in restored and maximized windows, checks vertical/horizontal
black strips, and logs native paint/erase counts for the actual Notes field.
Those counts diagnose redundant redraw; they do not certify the absence of rare
flicker or measure display frame pacing.

Some switches show windows or send pointer/keyboard input. Use those only during
an authorized, undisturbed review interval. Hidden layout/preferences checks do
not show windows. Logs and captures belong under ignored `winforms/artifacts/`,
not in this source folder.
