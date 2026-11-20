using RibbonKit.WinForms;

namespace IndependentConsumer;

// Compiles unchanged in a package-only application and in the two sample check hosts.
internal static class Office2007ShellChecks
{
    internal static int Run(string directory)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory);
        try
        {
            using var form = new RibbonForm(); using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2007Blue, AnimationLevel = RibbonAnimationLevel.None };
            var home = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Independent commands", CanResize = false };
            var button = new RibbonButtonItem { Text = "Save", Icon = RibbonStockIcons.Create("save"), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var field = new RibbonTextBoxItem { Id = "retained-field", Text = "Name", Value = "Native selection", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            group.Items.AddRange(button, new RibbonToggleItem { Text = "Bold", Checked = true }, new RibbonSplitButtonItem { Text = "Paste" }, field);
            home.Groups.Add(group); ribbon.Tabs.Add(home); ribbon.Tabs.Add(new RibbonTab { Text = "Insert" });
            ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = button }); ribbon.QuickAccessVisible = true;
            ribbon.FileMenu.Visible = true; ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Open" });
            using var pageField = new TextBox { Text = "Retained page", Dock = DockStyle.Fill };
            ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = pageField });
            form.Controls.Add(ribbon); form.Ribbon = ribbon;
            PaintAtlas(directory);
            VerifyCore(form, ribbon, directory, "independent");
            var native = Descendants(ribbon).OfType<TextBox>().Single(c => c.Name == field.Id);
            native.Select(2, 4); var handle = native.Handle; var parent = native.Parent;
            foreach (var theme in new[] { RibbonThemes.Office2024Light, RibbonThemes.Office2007Blue, RibbonThemes.Office2010Blue, RibbonThemes.Office2007Blue })
            {
                ribbon.Theme = theme; Application.DoEvents();
                Require(native.Handle == handle && ReferenceEquals(parent, native.Parent) && native.Text == field.Value && native.SelectionStart == 2 && native.SelectionLength == 4, "Preset changes retain native ribbon editor HWND/parent/text/selection");
            }
            File.AppendAllText(Path.Combine(directory, "result.log"), "PASS: independent package-only model; native ribbon editor retained across four preset changes.\n");
            return 0;
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(directory, "failure.log"), error.ToString()); return 1; }
    }

    internal static void VerifySample(RibbonForm form, RibbonControl ribbon, string directory, string name)
    {
        Directory.CreateDirectory(directory);
        var items = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).ToArray();
        var themes = items.OfType<RibbonGalleryItem>().Single(g => g.Items.Any(i => i.Text == "Office 2007 Blue"));
        var tile = themes.Items.Single(i => i.Text == "Office 2007 Blue");
        var accents = items.OfType<RibbonGalleryItem>().Single(g => g.Text == "Accent colors");
        var title = items.OfType<RibbonToggleItem>().Single(i => i.Text == "Color title bar");
        if (title.Checked) title.PerformClick();
        var route = ribbon.FileMenuMode; var frame = form.FrameAppearance; var backdrop = form.Backdrop;
        var application = ribbon.Theme with { Accent = Color.DarkViolet }; ribbon.Theme = application;
        tile.PerformClick(); Require(ReferenceEquals(ribbon.Theme, RibbonThemes.Office2007Blue) && ReferenceEquals(themes.SelectedItem, tile), "Authored 2007 tile applies shared preset");
        Require(route == ribbon.FileMenuMode && frame == form.FrameAppearance && backdrop == form.Backdrop, "Family selection preserves File route/frame/material preferences");
        foreach (var color in new[] { "Purple", "Teal" })
        {
            accents.Items.Single(i => i.Text == color).PerformClick();
            Require(ReferenceEquals(themes.SelectedItem, tile) && ribbon.Theme.Hover == RibbonThemes.Office2007Blue.Hover && ribbon.Theme.Pressed == RibbonThemes.Office2007Blue.Pressed, "Accent retains selected family and gold states");
            title.PerformClick(); Require(title.Checked && form.TitleBarColor.IsEmpty && ribbon.Theme.TitleBarForeground == Color.White, "Colored caption uses shared recipe and white ink");
            Require(ribbon.Theme.TitleBarBackgroundFill == ribbon.Theme.IdentityAccent!.CreateCaptionFill(ribbon.Theme.Accent), "Caption uses shared identity ramp");
            title.PerformClick(); Require(!title.Checked && ribbon.Theme.TitleBarBackgroundFill == RibbonThemes.Office2007Blue.TitleBarBackgroundFill, "Title Reset restores neutral valley independently");
        }
        accents.Items.Single(i => i.Text == "Default").PerformClick(); Require(ribbon.BuiltInTheme == RibbonBuiltInTheme.Office2007Blue, "Accent Reset restores built-in ownership");
        themes.Items.Single(i => i.Text == "Application theme").PerformClick(); Require(ReferenceEquals(application, ribbon.Theme), "Gallery Application Reset");
        tile.PerformClick(); VerifyCore(form, ribbon, directory, name);
        File.AppendAllText(Path.Combine(directory, "result.log"), "PASS: actual authored theme/accent/title actions, family selection and independent Reset; frame/material/File preferences preserved.\n");
    }

    private static void VerifyCore(RibbonForm form, RibbonControl ribbon, string directory, string name)
    {
        form.StartPosition = FormStartPosition.Manual; form.Bounds = new(-16000, -16000, 1200, 740); form.ShowInTaskbar = false;
        ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.QuickAccessVisible = true;
        form.Show(); Application.DoEvents();
        var tabs = ribbon.Tabs.ToArray(); var commands = tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).ToArray();
        ribbon.SelectedTab = tabs[0];
        int scopes = 0;
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>()) foreach (var density in Enum.GetValues<RibbonDensity>()) foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
        {
            ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.QuickAccessPosition = position; Application.DoEvents();
            Require(ribbon.EffectiveApplicationButtonShape == RibbonApplicationButtonShape.Orb && !ribbon.ApplicationButtonBounds.IsEmpty, "2007 Orb has a measured footprint");
            Require(ribbon.Tabs.SequenceEqual(tabs) && tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).SequenceEqual(commands), "Layout/density/placement retain authored models");
            Capture(ribbon, Path.Combine(directory, $"{name}-{mode}-{density}-{position}.png")); scopes++;
        }
        ribbon.LayoutMode = RibbonLayoutMode.Classic; ribbon.Density = RibbonDensity.Compact; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
        ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Rectangular; Application.DoEvents();
        Require(ribbon.EffectiveApplicationButtonShape == RibbonApplicationButtonShape.Rectangular, "Explicit rectangular shape wins");
        Capture(ribbon, Path.Combine(directory, name + "-rectangular.png"));
        System.ComponentModel.TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.ApplicationButtonShape)]!.ResetValue(ribbon);
        Require(ribbon.EffectiveApplicationButtonShape == RibbonApplicationButtonShape.Orb, "Shape Reset resumes the preset Orb");
        var route = ribbon.FileMenuMode;
        foreach (var fileRoute in Enum.GetValues<RibbonFileMenuMode>())
        {
            ribbon.FileMenuMode = fileRoute; ribbon.FileMenu.PerformClick(); Application.DoEvents();
            Require(ribbon.FileMenu.IsDropDownOpen, "Existing File route opens with Orb");
            ribbon.FileMenu.CloseDropDown(); Application.DoEvents();
        }
        ribbon.FileMenuMode = route;
        File.WriteAllText(Path.Combine(directory, "result.log"), $"PASS: {name}; {scopes} native scopes at {ribbon.DeviceDpi} DPI; Classic/Simplified, Compact/Touch, all QAT placements, models retained, shape override/Reset, both File routes.\nLIMIT: offscreen print/native evidence; live normal-ribbon review, actual VS, physical input and mixed-monitor acceptance remain separate. Dedicated menu/Options/Aero palettes are later slices.\n");
        form.Hide();
    }
    private static void Capture(RibbonControl ribbon, string path) { using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(path); }
    private static void PaintAtlas(string directory)
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 }) foreach (bool office in new[] { false, true })
        {
            int S(int n) => (int)Math.Round(n * dpi / 96d);
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(740), S(190)); using var g = Graphics.FromImage(image); g.Clear(Color.White);
            var t = RibbonThemes.Office2007Blue;
            var c = new RibbonPaintContext(g, new(S(4), S(40), S(732), S(146)), t, Control.DefaultFont, dpi)
            { HeaderBodyConnectionBounds = new(S(71), S(40), S(98), S(1)) };
            painter.DrawBody(c);
            using var group = new RibbonGroup { Text = "Framed group" };
            var frame = new Rectangle(S(10), S(45), S(170), S(134));
            painter.DrawGroup(c with { Bounds = frame, GroupFrameBounds = frame, GroupCaptionBounds = new(frame.Left, frame.Bottom - S(24), frame.Width, S(24)) }, group, false);
            using var button = new RibbonToggleItem { Text = "" };
            for (int i = 0; i < 4; i++)
            {
                button.Checked = i >= 2;
                painter.DrawItem(c with { Bounds = new(S(190 + i * 85), S(55), S(75), S(100)), Hot = i is 0 or 3, Pressed = i == 1 }, button, RibbonItemSize.Large, false);
            }
            using var file = new RibbonDropDownItem { Text = "" };
            for (int i = 0; i < 3; i++) painter.DrawFileButton(c with { Bounds = new(S(550 + i * 60), S(60), S(46), S(46)), Hot = i == 1, Pressed = i == 2 }, file);
            using var tab = new RibbonTab { Text = "Selected" };
            painter.DrawTab(c with { Bounds = new(S(70), S(10), S(100), S(30)), HeaderBodyConnectionBounds = new(S(71), S(40), S(98), S(1)) }, tab, true);
            image.Save(Path.Combine(directory, $"paint-{office}-{dpi}.png"));
        }
    }
    private static IEnumerable<Control> Descendants(Control root) { foreach (Control child in root.Controls) { yield return child; foreach (var c in Descendants(child)) yield return c; } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
