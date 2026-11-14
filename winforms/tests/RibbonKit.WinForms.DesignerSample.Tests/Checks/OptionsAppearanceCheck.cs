using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class OptionsAppearanceCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop; using var painter = new Probe(); using var owner = new RibbonForm { Bounds = new(-16000, -16000, 1200, 700), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false, Backdrop = backdrop }; using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None }; owner.Controls.Add(ribbon); owner.Ribbon = ribbon; owner.Show(); Application.DoEvents(); var original = ribbon.Theme; int states = 0;
            var settings = new RibbonOptionsAppearanceSettings { NavigationCornerRadius = 10, NavigationSelectionInset = 3, NavigationTextPadding = 20, NavigationSelectedBackground = new(Color.LightYellow), NavigationFocusInset = 5, NavigationMarkerWidth = 6, NavigationMarkerInset = 10, NavigationMarkerFill = new(Color.DarkMagenta), DividerWidth = 3, DividerColor = Color.DarkBlue };
            var tab = new RibbonTab { Id = "options.home", Text = "Home" }; var commands = new RibbonGroup { Id = "options.commands", Text = "Commands" }; var save = new RibbonButtonItem { Id = "options.save", Text = "Save" }; var undo = new RibbonButtonItem { Id = "options.undo", Text = "Undo" }; commands.Items.AddRange(save, undo); tab.Groups.Add(commands); ribbon.Tabs.Add(tab); ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = save }); ribbon.CommandCatalog.CaptureDefaults(); Application.DoEvents();
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original;
                using var dialog = new RibbonOptionsDialog(ribbon) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; var page = new RibbonOptionsPage { Text = "General", BackColor = original.Surface }; var editor = new TextBox { Text = "Retained native Options draft", Bounds = new(12, 12, 220, 24) }; page.Controls.Add(editor); dialog.Pages.Add(page); dialog.Pages.Add(new RibbonOptionsPage { Text = "Advanced" }); bool valid = false; int applied = 0, cancelled = 0; string checkpoint = "Original"; page.Applying += (_, e) => e.Cancel = !valid; page.Applied += (_, _) => { applied++; checkpoint = editor.Text; }; page.Cancelled += (_, _) => { cancelled++; editor.Text = checkpoint; };
                dialog.Show(owner); Application.DoEvents(); var nav = Descendants(dialog).OfType<ListBox>().Single(c => c.Name == "OptionsNavigation"); var rail = nav.Parent!; var footer = Descendants(dialog).OfType<FlowLayoutPanel>().Single(); Require(nav.Focus(), "Native Options navigation could not receive focus."); byte[] before = Pixels(nav); var geometry = Geometry(dialog); nint navHandle = nav.Handle, editorHandle = editor.Handle, dialogHandle = dialog.Handle; var parent = editor.Parent; editor.Select(2, 5); int radiusCalls = painter.Radii;
                ribbon.Theme = original with { OptionsAppearance = settings }; Require(nav.Focus(), "Options navigation focus was lost.");
                using (var image = Print(nav))
                {
                    var row = nav.GetItemRectangle(0); int S(int n) => (int)Math.Ceiling(n * dialog.DeviceDpi / 96d);
                    Require(image.GetPixel(0, row.Top + row.Height / 2).ToArgb() == Color.DarkMagenta.ToArgb(), "Actual native marker bypassed settings."); Require(image.GetPixel(row.Right - S(8), row.Top + row.Height / 2).ToArgb() == Color.LightYellow.ToArgb(), "Actual native selected fill bypassed settings."); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-navigation.png"));
                }
                using (var image = Print(rail)) Require(image.GetPixel(image.Width - 2, image.Height / 2).ToArgb() == Color.DarkBlue.ToArgb(), "Actual rail divider bypassed settings."); using (var image = Print(footer)) Require(image.GetPixel(image.Width / 2, 1).ToArgb() == Color.DarkBlue.ToArgb(), "Actual footer divider bypassed settings.");
                Require(painter.Radii > radiusCalls && Geometry(dialog).SequenceEqual(geometry) && editor.Handle == editorHandle && nav.Handle == navHandle && dialog.Handle == dialogHandle && ReferenceEquals(parent, editor.Parent) && editor.Text == "Retained native Options draft" && editor.SelectionStart == 2 && editor.SelectionLength == 5 && ReferenceEquals(page, dialog.SelectedPage), "Decoration changed native targets, page ownership or editor state.");
                ribbon.Theme = original; nav.Focus(); Require(Pixels(nav).SequenceEqual(before), "Group Reset did not restore exact native navigation pixels.");
                Button("ApplyCustomization").PerformClick(); Require(applied == 0, "Invalid Apply committed."); valid = true; Button("ApplyCustomization").PerformClick(); Require(applied == 1, "Valid Apply did not commit once."); editor.Text = "Unapplied draft"; valid = false; Button("AcceptCustomization").PerformClick(); Require(dialog.Visible && ReferenceEquals(dialog.SelectedPage, page), "Invalid OK did not keep the selected page open."); Button("CancelCustomization").PerformClick(); Require(cancelled == 1 && checkpoint == "Retained native Options draft", "Cancel lost the applied checkpoint."); states++;
                Button Button(string name) => Descendants(dialog).OfType<Button>().Single(b => b.Name == name);
            }
            ribbon.Theme = original with { OptionsAppearance = settings };
            using (var builtIn = new RibbonQuickAccessCustomizationForm(ribbon, "RibbonKit.Options.Check") { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false })
            {
                builtIn.Show(owner); Application.DoEvents(); var pages = builtIn.Pages.ToArray(); var sources = ribbon.QuickAccessItems.ToArray(); var draft = builtIn.Editor.Session.Draft; draft.QuickAccess.Add(new() { CommandId = undo.Id }); builtIn.Editor.RefreshDraft(); var native = Descendants(builtIn).Where(c => c is TextBox or ListBox or TreeView).ToDictionary(c => c, c => c.Handle); using var image = Print(builtIn); image.Save(Path.Combine(output, $"{scope}-customization.png")); ribbon.Theme = original;
                Require(pages.SequenceEqual(builtIn.Pages) && ReferenceEquals(draft, builtIn.Editor.Session.Draft) && draft.QuickAccess.Count == 2 && draft.QuickAccess.Any(e => e.CommandId == undo.Id) && native.All(p => !p.Key.IsDisposed && p.Key.Handle == p.Value) && sources.Length == 1 && sources.SequenceEqual(ribbon.QuickAccessItems), "Built-in customization lost native controls, pending Undo draft or Save source."); Descendants(builtIn).OfType<Button>().Single(b => b.Name == "CancelCustomization").PerformClick(); Require(sources.SequenceEqual(ribbon.QuickAccessItems), "Cancel applied a customization draft.");
            }
            ribbon.Theme = original with { OptionsAppearance = settings with { NavigationMarkerWidth = null } }; Require(ribbon.Theme.OptionsAppearance!.DividerWidth == 3, "Marker member Reset cleared dividers."); ribbon.Theme = ribbon.Theme with { OptionsAppearance = null }; Require(ribbon.Theme.OptionsAppearance is null, "Options group Reset failed.");
            evidence.Add($"PASS {scope}: effective owner={owner.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states; actual native navigation/rail/footer paint, radius hook, exact Reset pixels, unchanged dialog/row/page/editor geometry and HWND/parent/value/selection; invalid Apply/OK, valid Apply checkpoint and Cancel; built-in customization pages/native controls/draft/source retention and member/group Reset."); owner.Close();
        }
evidence.Add("Synchronous offscreen public-package checks. Native message-loop/foreground timing, physical input/IME, actual VS, external UIA and mixed-monitor review remain separate. Options native window/backdrop and application page styling remain with their existing owners."); File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static Bitmap Print(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, new(Point.Empty, image.Size)); return image; }
    private static byte[] Pixels(Control control) { using var image = Print(control); using var stream = new MemoryStream(); image.Save(stream, System.Drawing.Imaging.ImageFormat.Png); return stream.ToArray(); }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static string[] Geometry(Control root) => Descendants(root).Select(c => c.GetType().Name + ":" + c.Name + ":" + c.Bounds).ToArray();
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Probe : RibbonOffice2024Renderer
    { internal int Radii; public override int GetPopupCornerRadius(RibbonTheme theme, int dpi) { Radii++; return base.GetPopupCornerRadius(theme, dpi); } }
}
