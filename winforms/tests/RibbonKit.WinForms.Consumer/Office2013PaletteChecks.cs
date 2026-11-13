using System.ComponentModel;
using RibbonKit.WinForms;

namespace IndependentConsumer;

// A package-only application: public controls, models and renderer entry points.
internal static class Office2013PaletteChecks
{
    internal static int Run(string directory, bool nativeOnly = false, bool paintOnly = false)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
            if (!nativeOnly) foreach (bool office in new[] { false, true }) foreach (int dpi in new[] { 96, 120, 144, 192 }) Paint(office, dpi);
            if (!paintOnly) foreach (var mode in Enum.GetValues<RibbonLayoutMode>()) foreach (var density in Enum.GetValues<RibbonDensity>())
            foreach (var route in Enum.GetValues<RibbonFileMenuMode>()) Native(mode, density, route);
            File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 0;
        }
        catch (Exception error) { log.Add("FAIL: " + error); File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 1; }

        void Paint(bool office, int dpi)
        {
            int S(int value) => (int)Math.Round(value * dpi / 96d);
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(800), S(220)); using var g = Graphics.FromImage(image); g.Clear(Color.White);
            int row = 0;
            foreach (var accent in new[] { Color.FromArgb(43, 87, 154), Color.DarkOrange, Color.ForestGreen })
            {
                var theme = RibbonThemes.Office2013Light with { Accent = accent };
                var glass = theme with { Surface = Color.Transparent };
                Require(glass.Hover.Start.A is > 0 and < 255 && glass.Hover.Start.R == accent.R, "Derived accent wash retains alpha over a transparent surface");
                using var owner = new RibbonControl { Theme = theme }; var tab = new RibbonTab(); var group = new RibbonGroup();
                using var toggle = new RibbonToggleItem(); group.Items.Add(toggle); tab.Groups.Add(group); owner.Tabs.Add(tab);
                using var qat = new RibbonQuickAccessReference { Source = toggle }; owner.QuickAccessItems.Add(qat);
                using var file = new RibbonDropDownItem { Text = "File" };
                int column = 0;
                foreach (var (hot, pressed, check) in new[] { (true, false, false), (true, true, false), (false, false, true), (true, false, true) })
                {
                    toggle.Checked = check; var bounds = new Rectangle(S(10 + column * 90), S(10 + row * 70), S(80), S(54));
                    var c = new RibbonPaintContext(g, bounds, theme, SystemFonts.MessageBoxFont!, dpi, Hot: hot, Pressed: pressed);
                    painter.DrawItem(c, toggle, RibbonItemSize.Small, false);
                    var before = image.GetPixel(bounds.Left + S(6), bounds.Top + S(6));
                    painter.DrawQuickAccessItem(c, qat, false);
                    Require(before.ToArgb() == image.GetPixel(bounds.Left + S(6), bounds.Top + S(6)).ToArgb(), "QAT shares command state palette");
                    column++;
                }
                foreach (var (hot, pressed) in new[] { (false, false), (true, false), (true, true) })
                {
                    var bounds = new Rectangle(S(390 + (column - 4) * 90), S(10 + row * 70), S(80), S(54));
                    painter.DrawFileButton(new(g, bounds, theme, SystemFonts.MessageBoxFont!, dpi, Hot: hot, Pressed: pressed), file);
                    var color = image.GetPixel(bounds.Left + S(5), bounds.Top + S(5));
                    Require(!hot && !pressed ? color.ToArgb() == accent.ToArgb() : color.R + color.G + color.B < accent.R + accent.G + accent.B, "File remains accent-filled and darkens for interaction");
                    column++;
                }
                var alpha = new RibbonFill(Color.FromArgb(70, Color.Red), Color.Transparent);
                var authored = theme with { Hover = alpha, Pressed = new(Color.Transparent), CheckedHover = new(Color.Lime) };
                var recolored = authored with { Accent = Color.Purple };
                Require(recolored.Hover == alpha && recolored.Pressed.Start == Color.Transparent && recolored.CheckedHover!.Value.Start == Color.Lime, "Explicit fills survive accent changes");
                Require((recolored with { HoverOverride = null }).Hover != alpha && recolored.Pressed.Start == Color.Transparent, "Independent Hover Reset");
                var fileOverride = theme with { FileButtonAppearance = theme.FileButtonAppearance! with { Background = new(Color.Transparent), HoverBackground = new(Color.Transparent) } };
                using var transparent = new Bitmap(S(100), S(60)); using var tg = Graphics.FromImage(transparent); tg.Clear(Color.Magenta);
                painter.DrawFileButton(new(tg, new(0, 0, transparent.Width, transparent.Height), fileOverride, SystemFonts.MessageBoxFont!, dpi, Hot: true), file);
                Require(transparent.GetPixel(S(5), S(5)).ToArgb() == Color.Magenta.ToArgb(), "Explicit Transparent File fill");
                row++;
            }
            image.Save(Path.Combine(directory, $"states-{(office ? "office" : "neutral")}-{dpi}.png"));
            log.Add($"PASS: public {(office ? "Office" : "neutral")} renderer/{dpi} DPI; three accents, hover/press/checked/checked-hover, QAT and idle/hover/pressed File; explicit alpha/gradient/Transparent and independent Reset");
        }

        void Native(RibbonLayoutMode mode, RibbonDensity density, RibbonFileMenuMode route)
        {
            using var form = new RibbonForm { Text = "Public Office 2013 palette consumer", StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1050, 650), ShowInTaskbar = false };
            using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2013Light, LayoutMode = mode, Density = density, FileMenuMode = route, AnimationLevel = RibbonAnimationLevel.None };
            var home = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Editing" };
            var field = new RibbonComboBoxItem { Id = "field", Text = "Name", IsEditable = true, Value = "Retained public field", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            group.Items.Add(field); home.Groups.Add(group); ribbon.Tabs.Add(home);
            ribbon.FileMenu.Visible = true; ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Open" });
            using var page = new TextBox { Text = "Retained page" }; ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
            var editor = Children(ribbon).OfType<ComboBox>().Single(c => c.Name == "field"); var handle = editor.Handle;
            Require(field.Value == "Retained public field", "Initial editable choice is configured before theme changes");
            ribbon.IsBackstageOpen = true; var pageHandle = page.Handle; page.Select(2, 4); ribbon.IsBackstageOpen = false;
            foreach (var accent in new[] { Color.FromArgb(43, 87, 154), Color.DarkOrange, Color.ForestGreen })
            {
                var application = RibbonThemes.Office2013Light with { Accent = accent };
                ribbon.Theme = application;
                ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
                TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!.ResetValue(ribbon);
                Require(ReferenceEquals(application, ribbon.Theme) && form.EffectiveWindowCornerPreference == RibbonWindowCornerPreference.Square, "Application theme and corner Reset");
                Require(!ribbon.Theme.ExtendTitleBarColorToHeader && ribbon.Theme.Background == Color.White, "Separate white tab band");
                form.TitleBarColor = accent;
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    ribbon.FileMenu.PerformClick(); Require(ribbon.FileMenu.IsDropDownOpen, "Real File opens via " + route);
                    using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(100, 50); using var g = Graphics.FromImage(image);
                    var c = new RibbonPaintContext(g, new(0, 0, 100, 50), ribbon.Theme, SystemFonts.MessageBoxFont!, 96);
                    painter.DrawFileButton(c, ribbon.FileMenu); var opened = image.GetPixel(5, 5);
                    Require(opened.R + opened.G + opened.B < accent.R + accent.G + accent.B, "Open File darkens for both routes");
                    ribbon.FileMenu.CloseDropDown(); Require(!ribbon.FileMenu.IsDropDownOpen, "Real File closes");
                    painter.DrawFileButton(c, ribbon.FileMenu); Require(image.GetPixel(5, 5).ToArgb() == accent.ToArgb(), "Closed File returns to accent rather than neutral");
                }
                Require(field.Value == "Retained public field" && editor.Handle == handle && page.Handle == pageHandle && page.Text == "Retained page" && page.SelectionStart == 2 && page.SelectionLength == 4,
                    $"Native state retained: value='{field.Value}', editorHandle={editor.Handle == handle}, pageHandle={page.Handle == pageHandle}, pageText='{page.Text}', selection={page.SelectionStart}/{page.SelectionLength}");
            }
            ribbon.Theme = RibbonThemes.Office2013Light;
            var before = ribbon.Theme;
            try { ribbon.Theme = before with { AccentStates = new() { CheckedOpacity = double.NaN } }; throw new InvalidOperationException("Invalid coverage accepted"); }
            catch (ArgumentOutOfRangeException) { Require(ReferenceEquals(before, ribbon.Theme), "Invalid theme assignment is atomic"); }
            using var caption = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(caption, new(Point.Empty, caption.Size));
            Require(caption.GetPixel(caption.Width / 2, (int)Math.Round(48 * form.DeviceDpi / 96d)).ToArgb() == Color.White.ToArgb(), "Colored caption does not extend into 2013 tab band");
            caption.Save(Path.Combine(directory, $"native-{mode}-{density}-{route}.png"));
            log.Add($"PASS: actual {form.DeviceDpi} DPI/{mode}/{density}/{route}; three accents, open/close/reopen, permanent File color, application Reset, separate colored caption, retained native field/page handles/text/selection");
        }
    }
    private static IEnumerable<Control> Children(Control parent) => parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Children(c)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
