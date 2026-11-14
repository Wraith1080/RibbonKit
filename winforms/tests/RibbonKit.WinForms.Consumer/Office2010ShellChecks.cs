using RibbonKit.WinForms;

namespace IndependentConsumer;

// Public API only. The package entry point owns its model; ordinary samples also
// invoke the focused sample path to check their actual gallery event wiring.
internal static class Office2010ShellChecks
{
    internal static int Run(string directory)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory);
        try
        {
            using var form = new RibbonForm(); using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue, AnimationLevel = RibbonAnimationLevel.None };
            var home = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" };
            group.Items.AddRange(new RibbonButtonItem { Text = "Apply", Icon = RibbonStockIcons.Create("save") }, new RibbonToggleItem { Text = "Bold", Checked = true, Icon = RibbonStockIcons.Create("bold") }, new RibbonSplitButtonItem { Text = "Paste" });
            var combo = new RibbonComboBoxItem { Text = "Font", Value = "Calibri", IsEditable = true }; combo.Items.Add("Calibri"); group.Items.Add(combo);
            home.Groups.Add(group); ribbon.Tabs.Add(home); ribbon.Tabs.Add(new RibbonTab { Text = "Insert" });
            ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = (RibbonButtonItem)group.Items[0] }); ribbon.QuickAccessVisible = true;
            ribbon.FileMenu.Visible = true; ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Open" });
            using var editor = new RichTextBox { Text = "Independent native content", Dock = DockStyle.Fill };
            ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = editor });
            form.Controls.Add(ribbon); form.Ribbon = ribbon;
            VerifyCore(form, ribbon, directory, "independent package");
            var app = ribbon.Theme with { Hover = new(Color.Lime), TitleBarBackgroundFill = new(Color.Navy) }; ribbon.Theme = app;
            ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; ribbon.BuiltInTheme = null; Require(ReferenceEquals(app, ribbon.Theme), "Application Reset retains authored theme");
            var before = ribbon.Theme;
            try { ribbon.Theme = before with { IdentityAccent = new() { FileStrength = double.NaN } }; throw new InvalidOperationException("Invalid identity strength accepted"); }
            catch (ArgumentOutOfRangeException) { Require(ReferenceEquals(before, ribbon.Theme), "Invalid identity assignment is atomic"); }
            File.AppendAllText(Path.Combine(directory, "result.log"), "PASS: independent model/package, application Reset and atomic validation.\n");
            return 0;
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(directory, "failure.log"), error.ToString()); return 1; }
    }

    internal static void VerifySample(RibbonForm form, RibbonControl ribbon, string directory, string name)
    {
        Directory.CreateDirectory(directory);
        var items = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).ToArray();
        var themes = items.OfType<RibbonGalleryItem>().Single(g => g.Items.Any(i => i.Text == "Office 2010 Blue"));
        var tile = themes.Items.Single(i => i.Text == "Office 2010 Blue");
        var accents = items.OfType<RibbonGalleryItem>().Single(g => g.Text == "Accent colors");
        var title = items.OfType<RibbonToggleItem>().Single(i => i.Text == "Color title bar");
        if (title.Checked) title.PerformClick();
        var application = ribbon.Theme with { Accent = Color.DarkViolet }; ribbon.Theme = application;
        tile.PerformClick(); Require(ReferenceEquals(ribbon.Theme, RibbonThemes.Office2010Blue) && ReferenceEquals(themes.SelectedItem, tile), "Authored 2010 tile applies shared preset");
        foreach (var color in new[] { "Purple", "Teal" })
        {
            accents.Items.Single(i => i.Text == color).PerformClick();
            var accent = ribbon.Theme.Accent; Require(ReferenceEquals(themes.SelectedItem, tile), "Accent retains selected family");
            Require(ribbon.Theme.Hover == RibbonThemes.Office2010Blue.Hover && ribbon.Theme.Checked == RibbonThemes.Office2010Blue.Checked, "Accent retains gold command states");
            title.PerformClick(); Require(title.Checked && form.TitleBarColor.IsEmpty && ribbon.Theme.TitleBarForeground == Color.White, "Shared dimensional colored caption with white ink");
            Require(ribbon.Theme.TitleBarBackgroundFill == ribbon.Theme.IdentityAccent!.CreateCaptionFill(accent), "Caption uses public identity recipe");
            Capture(form, ribbon, directory, name + "-" + color + "-caption");
            title.PerformClick(); Require(!title.Checked && ribbon.Theme.TitleBarBackgroundFill == RibbonThemes.Office2010Blue.TitleBarBackgroundFill && ribbon.Theme.Accent == accent, "Title off restores neutral caption independently");
        }
        accents.Items.Single(i => i.Text == "Default").PerformClick(); Require(ribbon.BuiltInTheme == RibbonBuiltInTheme.Office2010Blue, "Accent Reset restores built-in ownership");
        themes.Items.Single(i => i.Text == "Application theme").PerformClick(); Require(ReferenceEquals(application, ribbon.Theme), "Gallery Application Reset");
        tile.PerformClick();
        VerifyCore(form, ribbon, directory, name);
        File.AppendAllText(Path.Combine(directory, "result.log"), "PASS: actual authored theme/accent/title actions, family selection, independent Reset and borrowed SVG tile.\n");
    }

    private static void VerifyCore(RibbonForm form, RibbonControl ribbon, string directory, string name)
    {
        form.StartPosition = FormStartPosition.Manual; form.Bounds = new(-16000, -16000, 1100, 740); form.ShowInTaskbar = false;
        form.Show(); Application.DoEvents();
        Require(form.EffectiveWindowCornerPreference == RibbonWindowCornerPreference.Rounded, "Rounded request follows theme");
        var tabs = ribbon.Tabs.ToArray(); var commands = tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).ToArray();
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.LayoutMode = mode; ribbon.Density = density;
            ribbon.SelectedTab = tabs[0]; Capture(form, ribbon, directory, name + "-" + mode + "-" + density);
            ribbon.SelectedTab = tabs[1]; ribbon.SelectedTab = tabs[0]; Application.DoEvents();
            Require(ribbon.Tabs.SequenceEqual(tabs) && tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).SequenceEqual(commands), "Layout/theme retain source identities");
        }
        ribbon.LayoutMode = RibbonLayoutMode.Classic; ribbon.Density = RibbonDensity.Compact;
        foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
        { ribbon.QuickAccessPosition = position; Capture(form, ribbon, directory, name + "-qat-" + position); }
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        var page = ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().FirstOrDefault();
        if (page is not null)
        {
            ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.FileMenu.PerformClick(); Application.DoEvents(); ribbon.FileMenu.CloseDropDown(); Application.DoEvents();
        }
        var controls = page?.Content is { } content ? Descendants(content).OfType<TextBoxBase>().ToArray() : Array.Empty<TextBoxBase>();
        var state = controls.Select(c => (Control: c, Handle: c.Handle, Parent: c.Parent, c.Text, c.SelectionStart, c.SelectionLength)).ToArray();
        foreach (var route in Enum.GetValues<RibbonFileMenuMode>())
        {
            ribbon.FileMenuMode = route;
            ribbon.FileMenu.PerformClick(); Application.DoEvents(); Require(ribbon.FileMenu.IsDropDownOpen, "File route opens");
            Capture(form, ribbon, directory, name + "-file-" + route); ribbon.FileMenu.CloseDropDown(); Application.DoEvents();
            foreach (var s in state) Require(s.Control.Handle == s.Handle && ReferenceEquals(s.Control.Parent, s.Parent) && s.Control.Text == s.Text && s.Control.SelectionStart == s.SelectionStart && s.Control.SelectionLength == s.SelectionLength, "Native page editor retention");
        }
        using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(880, 210); using var g = Graphics.FromImage(image); g.Clear(Color.White);
        using var button = new RibbonButtonItem { Text = "Hover" }; using var check = new RibbonToggleItem { Text = "Checked", Checked = true }; using var file = new RibbonDropDownItem { Text = "File" };
        var context = new RibbonPaintContext(g, new(8, 8, 130, 80), ribbon.Theme, SystemFonts.MessageBoxFont!, 96);
        painter.DrawItem(context with { Hot = true }, button, RibbonItemSize.Large, false);
        painter.DrawItem(context with { Bounds = new(154, 8, 130, 80), Pressed = true }, button, RibbonItemSize.Large, false);
        painter.DrawItem(context with { Bounds = new(300, 8, 130, 80) }, check, RibbonItemSize.Large, false);
        painter.DrawGalleryTile(context with { Bounds = new(446, 8, 130, 80) }, new RibbonGalleryTile { Text = "Selected" }, true);
        foreach (var (x, hot, press) in new[] { (8, false, false), (154, true, false), (300, false, true) }) painter.DrawFileButton(context with { Bounds = new(x, 112, 130, 72), Hot = hot, Pressed = press }, file);
        image.Save(Path.Combine(directory, name + "-paint-states.png"));
        using var popup = new Bitmap(430, 310); using var pg = Graphics.FromImage(popup); pg.Clear(Color.White);
        var pc = new RibbonPaintContext(pg, new(2, 2, 426, 306), ribbon.Theme, SystemFonts.MessageBoxFont!, 96);
        painter.DrawPopupFrame(pc);
        painter.DrawGalleryHeading(pc with { Bounds = new(10, 10, 410, 30) }, "Gallery and choices");
        painter.DrawItem(pc with { Bounds = new(10, 48, 410, 36), Hot = true }, new RibbonButtonItem { Text = "Paste" }, RibbonItemSize.Small, true);
        painter.DrawItem(pc with { Bounds = new(10, 90, 410, 36) }, new RibbonButtonItem { Text = "Paste Special..." }, RibbonItemSize.Small, true);
        using var choice = new RibbonComboBoxItem(); choice.Items.Add("Calibri"); choice.Items.Add("Segoe UI");
        painter.DrawComboBoxItem(pc with { Bounds = new(10, 142, 410, 36), BackgroundBounds = pc.Bounds }, choice, 0, true);
        painter.DrawComboBoxItem(pc with { Bounds = new(10, 182, 410, 36), BackgroundBounds = pc.Bounds }, choice, 1, false);
        painter.DrawGalleryTile(pc with { Bounds = new(10, 234, 130, 62) }, new RibbonGalleryTile { Text = "Selected tile" }, true);
        painter.DrawGalleryTile(pc with { Bounds = new(150, 234, 130, 62), Hot = true }, new RibbonGalleryTile { Text = "Hovered tile" }, false);
        popup.Save(Path.Combine(directory, name + "-popup-paint.png"));
        File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: " + name + "; shell/model retention, four layout/density scopes, three QAT positions, both File routes and native page editor handles/values. DPI=" + form.DeviceDpi + ".\nLIMIT: direct print/offscreen native evidence; below-tabs Backstage, actual VS, physical input, visible animation, desktop materials, four-corner silhouette and mixed-monitor acceptance remain separate.\n");
        form.Hide();
    }
    private static IEnumerable<Control> Descendants(Control root) { yield return root; foreach (Control child in root.Controls) foreach (var c in Descendants(child)) yield return c; }
    private static void Capture(RibbonForm form, RibbonControl ribbon, string directory, string name)
    {
        if (!form.IsHandleCreated) { form.StartPosition = FormStartPosition.Manual; form.Location = new(-16000, -16000); form.ShowInTaskbar = false; form.Show(); }
        Application.DoEvents(); using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(directory, name + ".png"));
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
