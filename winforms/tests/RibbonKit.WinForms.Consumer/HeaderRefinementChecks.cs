using RibbonKit.WinForms;

namespace IndependentConsumer;

// A focused public-package check for the user's F3 header feedback. No sample
// resources, internal geometry access or unrelated composed review matrices.
internal static class HeaderRefinementChecks
{
    internal static int Run(string directory)
    {
        Directory.CreateDirectory(directory);
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
            using var painter = new RibbonOffice2024Renderer(); using var tab = new RibbonTab { Text = "" };
            foreach (int dpi in new[] { 96, 120, 144, 192 })
            {
                int S(int n) => (int)Math.Round(n * dpi / 96d);
                using var image = new Bitmap(S(420), S(150)); using var g = Graphics.FromImage(image); g.Clear(Color.White);
                var basis = RibbonThemes.Office2010Blue;
                foreach (bool pressed in new[] { false, true })
                {
                    var bounds = new Rectangle(S(pressed ? 125 : 10), S(10), S(100), S(40));
                    var c = new RibbonPaintContext(g, bounds, basis, SystemFonts.MessageBoxFont!, dpi, Hot: !pressed, Pressed: pressed);
                    painter.DrawTab(c, tab, false);
                    var border = pressed ? basis.HeaderAppearance!.FilledPressedBorderColor : basis.HeaderAppearance!.FilledHoverBorderColor;
                    Require(image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Bottom - 1).ToArgb() != border.ToArgb(), "Tab has no bottom state outline");
                    Require(image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top).ToArgb() == border.ToArgb(), "Tab retains top state outline");
                }
                var header = new Rectangle(0, S(75), image.Width, S(60));
                var context = new RibbonPaintContext(g, header, basis, SystemFonts.MessageBoxFont!, dpi) { BackgroundBounds = header };
                painter.DrawSurface(context, false);
                var slot = new Rectangle(S(350), header.Top, S(40), header.Height);
                using (var noise = new SolidBrush(Color.Magenta)) g.FillRectangle(noise, slot);
                painter.DrawMinimizeButton(context with { Bounds = slot }, false);
                for (int y = slot.Top; y < slot.Bottom; y++) Require(image.GetPixel(slot.Left, y) == image.GetPixel(slot.Left - S(5), y), "Minimize backing follows the full band ramp");
                image.Save(Path.Combine(directory, "public-header-" + dpi + ".png"));
            }
            foreach (var family in Enum.GetValues<RibbonBuiltInTheme>())
            {
                using var form = new RibbonForm { Bounds = new(-16000, -16000, 1000, 600), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
                using var ribbon = new RibbonControl { BuiltInTheme = family, AnimationLevel = RibbonAnimationLevel.None };
                var home = new RibbonTab { Text = "Home" }; var edit = new RibbonTab { Text = "Edit" }; var view = new RibbonTab { Text = "View" };
                ribbon.Tabs.AddRange(home, edit, view); ribbon.FileMenu.Visible = true;
                form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
                var basis = ribbon.Theme; Require((basis.FileButtonMinimumWidth ?? 50) == (family == RibbonBuiltInTheme.Office2024Light ? 50 : 64), "Family File minimum defaults");
                Capture("default");
                ribbon.Theme = basis with { FileButtonMinimumWidth = 112 }; Capture("wider");
                ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null;
                Require(ribbon.Theme.FileButtonMinimumWidth == 112, "Application Reset retains shared width");
                var before = ribbon.Theme;
                try { ribbon.Theme = before with { FileButtonMinimumWidth = 0 }; throw new InvalidOperationException("Invalid width accepted"); }
                catch (ArgumentOutOfRangeException) { Require(ReferenceEquals(before, ribbon.Theme), "Width validation is atomic"); }
                ribbon.Theme = basis;
                if (family == RibbonBuiltInTheme.Office2010Blue)
                {
                    foreach (bool minimized in new[] { false, true })
                    {
                        ribbon.IsMinimized = minimized; Application.DoEvents();
                        using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
                        int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
                        int headerBottom = ribbon.Height - (minimized ? 0 : S(basis.BodyHeight));
                        int y = headerBottom - S(basis.TabHeight) / 2;
                        Require(image.GetPixel(image.Width - 2, y) == image.GetPixel(image.Width - S(50), y), "Actual native minimize slot has no solid patch");
                    }
                }
                form.Hide();
                void Capture(string suffix)
                {
                    Application.DoEvents(); using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
                    image.Save(Path.Combine(directory, family + "-" + suffix + ".png"));
                }
            }
            File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: public tab top/side-only state outlines and continuous minimize backing at 96/120/144/192 paint DPI; four theme defaults/shared File minimum overrides, Application Reset, atomic rejection and native expanded/minimized header paint.\nLIMIT: direct paint/offscreen native evidence; no actual VS, physical input, visible animation, desktop materials, corner silhouette or mixed-monitor acceptance.\n");
            return 0;
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(directory, "failure.log"), error.ToString()); return 1; }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
