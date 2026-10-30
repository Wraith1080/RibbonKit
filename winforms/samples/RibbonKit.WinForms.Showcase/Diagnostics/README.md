# Showcase diagnostics

Capture, tracing and regression-check implementations live here. Feature setup,
application handlers, persistence and the normal Designer files remain beside
`MainForm.cs`.

These files are parts of the same `MainForm` partial class, compiled by the normal
project. They inspect the actual Showcase controls rather than creating a second
sample hierarchy. `Program.cs` retains the existing diagnostic command switches;
their arguments and behavior are unchanged. A normal launch does not run them.

Some switches show windows or send pointer/keyboard input. Use those only during
an authorized, undisturbed review interval. Hidden layout/preferences checks do
not show windows. Logs and captures belong under ignored `winforms/artifacts/`,
not in this source folder.
