using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace IndependentConsumer;

// Uses only public models, renderer callbacks and ordinary native controls.
internal static class StateAppearanceChecks
{
    internal static int Run(string directory, bool nativeOnly = false, bool? officeOnly = null, bool rasterOnly = false)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
            foreach (bool office in new[] { false, true })
            {
                if (officeOnly is { } selected && selected != office) continue;
                using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
                foreach (int dpi in nativeOnly ? Array.Empty<int>() : new[] { 96, 120, 144, 192 })
                {
                    string name = $"{(office ? "Office" : "Neutral")}-{dpi}";
                    var theme = new RibbonTheme();
                    var custom = theme with { DisabledForeground = Color.Purple };
                    using var baseline = DrawMatrix(renderer, theme, dpi, false);
                    using var explicitGray = DrawMatrix(renderer, theme with { DisabledForeground = SystemColors.GrayText }, dpi, false);
                    Require(Equal(baseline, explicitGray), name + " Empty keeps system-gray default exactly");
                    using var authored = DrawMatrix(renderer, custom, dpi, false);
                    for (int cell = 0; cell < 24; cell++)
                    {
                        int S(int value) => (int)Math.Round(value * dpi / 96d);
                        var bounds = new Rectangle(S(cell % 4 * 220), S(cell / 4 * 80), S(220), S(80));
                        using var before = baseline.Clone(bounds, PixelFormat.Format32bppArgb); using var after = authored.Clone(bounds, PixelFormat.Format32bppArgb);
                        Require(cell == 23 ? Equal(before, after) : !Equal(before, after), name + " disabled role / raster exclusion in cell " + cell);
                    }
                    using var reset = DrawMatrix(renderer, custom with { DisabledForeground = Color.Empty }, dpi, false);
                    Require(Equal(baseline, reset), name + " reset restores default exactly");
                    using var enabled = DrawMatrix(renderer, theme, dpi, true);
                    using var enabledCustom = DrawMatrix(renderer, custom, dpi, true);
                    Require(Equal(enabled, enabledCustom), name + " enabled/focus/state drawing is unchanged");
                    CheckExistingRoles(renderer, dpi);
                    baseline.Save(Path.Combine(directory, name + "-default.png")); authored.Save(Path.Combine(directory, name + "-disabled.png"));
                    enabled.Save(Path.Combine(directory, name + "-enabled-states.png"));
                    log.Add("PASS: " + name + "; 24 painted layers, default/custom/reset, enabled invariance, existing state/focus roles");
                }
                if (!rasterOnly) CheckNativeFields(renderer, office ? "Office" : "Neutral", directory, log);
            }
            File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 0;
        }
        catch (Exception error) { log.Add("FAIL: " + error); File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 1; }
    }

    private static Bitmap DrawMatrix(RibbonRenderer renderer, RibbonTheme theme, int dpi, bool enabled)
    {
        int S(int value) => (int)Math.Round(value * dpi / 96d);
        using var font = new Font("Segoe UI", S(14), FontStyle.Bold, GraphicsUnit.Pixel);
        using var ribbon = new RibbonControl();
        var group = new RibbonGroup { Text = "Group", Enabled = enabled, Icon = new InkIcon() };
        var tab = new RibbonTab { Text = "Tab", Enabled = enabled }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var button = new RibbonButtonItem { Text = "Command", MenuDescription = "Description", Icon = new InkIcon() };
        var toggle = new RibbonToggleItem { Text = "Checked", Checked = true };
        var split = new RibbonSplitButtonItem { Text = "Split", Checked = true, Icon = new InkIcon() };
        var check = new RibbonCheckBoxItem { Text = "Check", Checked = true };
        var mixed = new RibbonCheckBoxItem { Text = "Mixed", CheckState = CheckState.Indeterminate };
        var radio = new RibbonRadioButtonItem { Text = "Radio", Checked = true };
        var text = new RibbonTextBoxItem { Text = "Text" };
        var combo = new RibbonComboBoxItem { Text = "Choice" }; combo.Items.Add("HH Choice");
        var tile = new RibbonGalleryTile { Text = "Gallery", Icon = new InkIcon() };
        var gallery = new RibbonGalleryItem(); gallery.Items.Add(tile);
        group.Items.AddRange(button, toggle, split, check, mixed, radio, text, combo, gallery);
        var reference = new RibbonQuickAccessReference { Source = button }; ribbon.QuickAccessItems.Add(reference);
        var splitReference = new RibbonQuickAccessDropDownReference { Source = split }; ribbon.QuickAccessItems.Add(splitReference);
        ribbon.FileMenu.Enabled = enabled;
        using var message = new RibbonMessage { Title = "Title", Text = "Message", Enabled = enabled };
        message.Action.Text = "Action";
        using var raster = new Bitmap(16, 16); using (var drawing = Graphics.FromImage(raster)) drawing.Clear(Color.Coral);
        var rasterCommand = new RibbonButtonItem { Image = raster }; group.Items.Add(rasterCommand);
        Action<RibbonPaintContext>[] cases =
        [
            c => renderer.DrawItem(c, button, RibbonItemSize.Medium, false),
            c => renderer.DrawItem(c, button, RibbonItemSize.Medium, true),
            c => renderer.DrawItem(c, toggle, RibbonItemSize.Medium, false),
            c => renderer.DrawSplitItem(c with { Bounds = c.Bounds with { Width = c.Bounds.Width - S(28) } }, c with { Bounds = new(c.Bounds.Right - S(28), c.Bounds.Top, S(28), c.Bounds.Height) }, split, RibbonItemSize.Medium, false),
            c => renderer.DrawItem(c, check, RibbonItemSize.Medium, false),
            c => renderer.DrawItem(c, mixed, RibbonItemSize.Medium, false),
            c => renderer.DrawItem(c, radio, RibbonItemSize.Medium, false),
            c => renderer.DrawTextBox(c, text, new(c.Bounds.Left + S(70), c.Bounds.Top + S(8), c.Bounds.Width - S(70), S(30))),
            c => renderer.DrawComboBox(c, combo, new(c.Bounds.Left + S(70), c.Bounds.Top + S(8), c.Bounds.Width - S(70), S(30))),
            c => renderer.DrawComboBoxItem(c, combo, 0, true),
            c => renderer.DrawFileButton(c, ribbon.FileMenu),
            c => renderer.DrawTab(c, tab, true),
            c => renderer.DrawGroup(c, group, true),
            c => renderer.DrawQuickAccessItem(c, reference, false),
            c => renderer.DrawQuickAccessItem(c, reference, true),
            c => renderer.DrawQuickAccessSplitItem(c with { Bounds = c.Bounds with { Width = c.Bounds.Width - S(28) } }, c with { Bounds = new(c.Bounds.Right - S(28), c.Bounds.Top, S(28), c.Bounds.Height) }, splitReference, false),
            c => renderer.DrawGalleryTile(c, tile, true),
            c => renderer.DrawGalleryStripButton(c, RibbonGalleryStripButton.Expand, enabled),
            c => renderer.DrawOverflowScrollButton(c, false, enabled),
            c => renderer.DrawBackstageEntry(c, button, true),
            c => renderer.DrawMessageAction(c, message.Action),
            c => renderer.DrawMessageClose(c, enabled),
            c => renderer.DrawMessage(c, message, new(c.Bounds.Left + S(30), c.Bounds.Top, S(100), S(22)), new(c.Bounds.Left + S(30), c.Bounds.Top + S(24), S(130), S(22)), new(c.Bounds.Left + S(5), c.Bounds.Top + S(10), S(20), S(20)), true),
            c => renderer.DrawItem(c with { Hot = false, Pressed = false, Focused = false }, rasterCommand, RibbonItemSize.Medium, false)
        ];
        string[] names = ["Command", "Menu + description", "Checked toggle", "Checked split", "Checkbox", "Mixed checkbox", "Radio", "TextBox frame/label", "Combo frame/label/arrow", "Choice selected row", "File", "Selected tab", "Collapsed group", "QAT command", "QAT menu row", "QAT split", "Selected gallery tile", "Gallery utility", "Overflow scroll", "Selected Backstage row", "Message action", "Message close", "Message title/body", "Raster artwork"];
        var image = new Bitmap(S(880), S(480));
        using var graphics = Graphics.FromImage(image); graphics.Clear(theme.Surface);
        for (int index = 0; index < cases.Length; index++)
        {
            var cell = new Rectangle(S(index % 4 * 220), S(index / 4 * 80), S(220), S(80));
            var bounds = new Rectangle(cell.Left + S(6), cell.Top + S(22), cell.Width - S(12), S(52));
            using var label = new SolidBrush(Color.Black); graphics.DrawString(names[index], SystemFonts.DefaultFont, label, cell.Left + S(5), cell.Top + S(2));
            var state = graphics.Save(); graphics.SetClip(bounds);
            cases[index](new(graphics, bounds, theme, font, dpi, Hot: true, Pressed: enabled && index % 3 == 0, Focused: true));
            graphics.Restore(state);
        }
        return image;
    }

    private static void CheckExistingRoles(RibbonRenderer renderer, int dpi)
    {
        var theme = new RibbonTheme { Accent = Color.Magenta, Border = Color.Green, Hover = new(Color.Red), Pressed = new(Color.Blue), Checked = new(Color.Gold) };
        using var image = new Bitmap(160, 50); using var graphics = Graphics.FromImage(image);
        using var text = new RibbonTextBoxItem(); using var combo = new RibbonComboBoxItem(); using var button = new RibbonToggleItem();
        var bounds = new Rectangle(0, 0, 160, 50); var context = new RibbonPaintContext(graphics, bounds, theme, SystemFonts.DefaultFont, dpi);
        renderer.DrawTextBox(context, text, bounds); Require(image.GetPixel(80, 1).ToArgb() == theme.Border.ToArgb(), "TextBox idle border");
        renderer.DrawTextBox(context with { Focused = true }, text, bounds); Require(image.GetPixel(80, 1).ToArgb() == theme.Accent.ToArgb(), "TextBox focus accent");
        renderer.DrawComboBox(context with { Focused = true }, combo, bounds); Require(image.GetPixel(80, 1).ToArgb() == theme.Accent.ToArgb(), "Combo focus accent");
        foreach (int state in new[] { 0, 1, 2 })
        {
            button.Checked = state == 2; graphics.Clear(theme.Surface);
            renderer.DrawItem(context with { Hot = state == 0, Pressed = state == 1 }, button, RibbonItemSize.Medium, false);
            Require(image.GetPixel(80, 10).ToArgb() == (state == 0 ? theme.Hover.Start : state == 1 ? theme.Pressed.Start : theme.Checked.Start).ToArgb(), "Existing command state fill");
        }
        graphics.Clear(theme.Surface); using var noFocus = (Bitmap)image.Clone(); renderer.DrawFocus(context);
        // ControlPaint chooses contrasting system dots from the neutral renderer's
        // Foreground/Surface inputs; they need not equal the literal Foreground.
        Require(!Equal(image, noFocus), "Existing visible focus treatment");
        if (renderer is RibbonOffice2024Renderer) Require(Count(image, theme.Accent) > 0, "Office focus accent");
    }

    private static void CheckNativeFields(RibbonRenderer renderer, string name, string directory, List<string> log)
    {
        using var form = new Form { Size = new(1100, 600), StartPosition = FormStartPosition.CenterScreen, Text = "RibbonKit state appearance check" };
        using var ribbon = new RibbonControl { Renderer = renderer };
        var text = new RibbonTextBoxItem { Id = "text", Text = "Text", Value = "HH Native text" };
        var editable = new RibbonComboBoxItem { Id = "editable", Text = "Editable", IsEditable = true, Value = "HH Free text" };
        var selection = new RibbonComboBoxItem { Id = "selection", Text = "Choice" }; selection.Items.Add("HH Native choice"); selection.SelectedIndex = 0;
        var group = new RibbonGroup(); group.Items.AddRange(text, editable, selection); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
        var editors = Descendants(ribbon).Where(c => c is TextBox or ComboBox).ToArray();
        Require(editors.Length == 3, name + " three real native editors");
        var handles = editors.Select(c => c.Handle).ToArray(); var parents = editors.Select(c => c.Parent).ToArray(); var bounds = editors.Select(c => c.Bounds).ToArray();
        var nativeText = (TextBox)editors.Single(c => c.Name == "text"); var nativeEdit = (ComboBox)editors.Single(c => c.Name == "editable"); var nativeChoice = (ComboBox)editors.Single(c => c.Name == "selection");
        nativeText.Select(3, 4); nativeEdit.Select(3, 4);
        group.Enabled = false;
        Application.DoEvents(); nativeChoice.Update();
        using var baseline = Capture(nativeChoice);
        var theme = ribbon.Theme; int invalidations = 0; nativeChoice.Invalidated += (_, _) => invalidations++;
        ribbon.Theme = theme with { DisabledForeground = Color.Purple };
        Require(invalidations > 0, name + " disabled native selection is invalidated on palette change");
        Application.DoEvents(); nativeChoice.Update();
        using var custom = Capture(nativeChoice);
        custom.Save(Path.Combine(directory, name + "-native-disabled-selection.png"));
        // Native Segoe UI uses ClearType. Thin strokes have subpixel coverage
        // rather than any pixels equal to the requested color exactly.
        Require(PurpleInk(custom) > 0 && !Equal(custom, baseline), name + " disabled selection-only text is owner-drawn");
        using var windowsText = Capture(nativeText); using var windowsEdit = CaptureEdit(nativeEdit);
        windowsText.Save(Path.Combine(directory, name + "-native-disabled-textbox.png")); windowsEdit.Save(Path.Combine(directory, name + "-native-disabled-edit.png"));
        Require(PurpleInk(windowsText) == 0 && PurpleInk(windowsEdit) == 0 && nativeText.ForeColor == SystemColors.GrayText && nativeEdit.ForeColor == SystemColors.GrayText,
            name + " disabled EDIT layers remain Windows-owned");
        Require(Count(windowsText, Color.White) < windowsText.Width * windowsText.Height - 4 && Count(windowsEdit, Color.White) < windowsEdit.Width * windowsEdit.Height - 4, name + " native EDIT text was painted");
        ribbon.Theme = theme;
        Application.DoEvents(); nativeChoice.Update();
        using var reset = Capture(nativeChoice); Require(Equal(reset, baseline) && PurpleInk(reset) == 0, name + " native selection reset");
        group.Enabled = true; text.ReadOnly = true;
        Require(nativeText.ReadOnly && nativeText.Enabled, name + " read-only stays enabled");
        Require(editors.Select(c => c.Handle).SequenceEqual(handles) && editors.Select(c => c.Parent).SequenceEqual(parents) && editors.Select(c => c.Bounds).SequenceEqual(bounds), name + " handles, parents and bounds retained");
        Require(nativeText.Text == "HH Native text" && nativeEdit.Text == "HH Free text" && nativeChoice.SelectedIndex == 0, name + " values and index retained");
        Require(nativeText.SelectionStart == 3 && nativeText.SelectionLength == 4 && nativeEdit.SelectionStart == 3 && nativeEdit.SelectionLength == 4, name + " native text selection retained");
        log.Add($"PASS: {name} native fields at actual {ribbon.DeviceDpi} DPI; owner-drawn disabled selection, Windows-owned EDIT limits, palette refresh/reset, handles/parents/bounds/text/selection/read-only retained");

        Bitmap Capture(Control control)
        {
            // Print only the field's native text region. Printing the complete
            // ComboBox ignores its HWND clip and includes its unused square arrow.
            using var measure = control.CreateGraphics();
            var clip = control.Region is { } region ? Rectangle.Round(region.GetBounds(measure)) : control.ClientRectangle;
            using var printed = new Bitmap(control.Width, control.Height);
            control.DrawToBitmap(printed, control.ClientRectangle);
            return printed.Clone(clip, PixelFormat.Format32bppArgb);
        }
    }

    private static IEnumerable<Control> Descendants(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static Bitmap CaptureEdit(ComboBox combo)
    {
        // WM_PRINT on the ComboBox root includes its hidden owner-drawn selection
        // underneath the real EDIT. Inspect that EDIT HWND itself instead.
        var info = new ComboInfo { Size = Marshal.SizeOf<ComboInfo>() };
        Require(GetComboBoxInfo(combo.Handle, ref info) && info.Edit != 0, "Native ComboBox EDIT handle");
        Require(GetClientRect(info.Edit, out var rectangle), "Native EDIT client bounds");
        var image = new Bitmap(Math.Max(1, rectangle.Right), Math.Max(1, rectangle.Bottom));
        using var graphics = Graphics.FromImage(image); nint dc = graphics.GetHdc();
        try { SendMessage(info.Edit, 0x317, dc, 0x1C); } // WM_PRINT: client, background, children
        finally { graphics.ReleaseHdc(dc); }
        return image;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct ComboInfo { internal int Size; internal NativeRect Item, Button; internal int ButtonState; internal nint Combo, Edit, List; }
    [DllImport("user32.dll")] private static extern bool GetComboBoxInfo(nint combo, ref ComboInfo info);
    [DllImport("user32.dll")] private static extern bool GetClientRect(nint window, out NativeRect bounds);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    private static int Count(Bitmap image, Color color)
    {
        int count = 0; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == color.ToArgb()) count++;
        return count;
    }
    private static int PurpleInk(Bitmap image)
    {
        int count = 0;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
        {
            var color = image.GetPixel(x, y);
            if (color.R - color.G > 70 && color.B - color.G > 70 && Math.Abs(color.R - color.B) < 20) count++;
        }
        return count;
    }
    private static bool Equal(Bitmap first, Bitmap second)
    {
        if (first.Size != second.Size) return false;
        byte[] Read(Bitmap bitmap)
        {
            var data = bitmap.LockBits(new(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try { var bytes = new byte[Math.Abs(data.Stride) * data.Height]; Marshal.Copy(data.Scan0, bytes, 0, bytes.Length); return bytes; }
            finally { bitmap.UnlockBits(data); }
        }
        return Read(first).SequenceEqual(Read(second));
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class InkIcon : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { using var brush = new SolidBrush(context.Foreground); graphics.FillRectangle(brush, bounds); }
    }
}
