using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Exercises public theme settings through the permanent package-only consumer.
internal static class BackstageAppearanceCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop;
            using var form = new RibbonForm { Text = "Backstage appearance settings", StartPosition = FormStartPosition.Manual,
                Bounds = new(-16000, -16000, 1050, 700), ShowInTaskbar = false, Backdrop = backdrop };
            using var painter = new Probe();
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None };
            using var page = new Panel(); var field = new TextBox { Text = "Retained appearance input", Bounds = new(20, 20, 270, 28) }; page.Controls.Add(field);
            var first = new RibbonBackstagePageItem { Text = "Info", Content = page }; var second = new RibbonBackstagePageItem { Text = "Print" };
            ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
            ribbon.Backstage.Items.AddRange(first, new RibbonMenuSeparatorItem(), second); ribbon.Backstage.FooterItems.Add(new RibbonButtonItem { Text = "Options" });
            ribbon.Backstage.NavigationBackgroundOpacity = .65f;
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true;
            var surface = Descendants(form).Single(control => control.AccessibleName == "Backstage");
            var info = Descendants(surface).OfType<Button>().Single(control => control.Text == "Info");
            var back = Descendants(surface).OfType<Button>().Single(control => control.Text == "Back");
            var separator = Descendants(surface).Single(control => control.AccessibleRole == AccessibleRole.Separator);
            var original = ribbon.Theme; var target = back.Bounds; var originalRow = info.Bounds; var originalSeparator = separator.Bounds;
            nint handle = field.Handle; var parent = field.Parent; field.Select(2, 5);
            var settings = new RibbonBackstageAppearanceSettings { MarkerLeftInset = 12, MarkerWidth = 6, MarkerTopInset = 7, MarkerBottomInset = 9,
                MarkerColor = Color.Magenta, BackVisualInset = 7, BackOutlineWidth = 0, BackGlyphScale = 0,
                NavigationCornerRadius = 0, NavigationBorderWidth = 3, NavigationBorderColor = Color.DarkOrange };
            foreach (bool fullWidth in new[] { false, true })
            {
                ribbon.Theme = original with { BackstageAppearance = settings, BackstageFullWidthNavigation = fullWidth,
                    BackstageSelectionIndicatorVisible = true, NavigationMarkerCornerRadius = 0 };
                painter.Check = true; painter.FullWidth = fullWidth; painter.RowPaints = painter.BackPaints = 0;
                Print(info, Path.Combine(output, scope + "-" + fullWidth + "-row.png"));
                Print(back, Path.Combine(output, scope + "-" + fullWidth + "-back.png"));
                Require(painter.Failure is null, "Navigation paint verification failed: " + painter.Failure);
                Require(painter.RowPaints > 0 && painter.BackPaints > 0, "Native navigation callbacks were not observed.");
                Require(back.Bounds == target && info.Bounds == originalRow, "Appearance settings changed native click targets.");
            }
            painter.Check = false;
            ribbon.Theme = ribbon.Theme with { BackstageRowTextVerticalPadding = 70, BackstageSeparatorHeight = 21, BackstageTrailingPadding = 30 };
            Application.DoEvents(); Require(info.Height > originalRow.Height, "Font clearance did not reach native rows.");
            Require(separator.Height == (int)Math.Round(21 * ribbon.DeviceDpi / 96d), "Separator height did not reach its native control.");
            ribbon.Theme = ribbon.Theme with { BackstageSeparatorHeight = 0 }; Application.DoEvents(); Require(separator.Height == 0, "Zero separator height did not remove its space.");
            ribbon.Backstage.SelectedPage = second; ribbon.Backstage.SelectedPage = first;
            Require(field.Handle == handle && ReferenceEquals(field.Parent, parent) && field.Text == "Retained appearance input" && field.SelectionStart == 2 && field.SelectionLength == 5,
                "Appearance/spacing or page selection changed native editor identity, text or selection.");
            ribbon.Theme = original; Application.DoEvents();
            Require(back.Bounds == target && info.Bounds == originalRow && separator.Bounds == originalSeparator, "Reset did not restore original native navigation geometry.");
            ribbon.IsBackstageOpen = false; ribbon.IsBackstageOpen = true;
            Require(field.Handle == handle && ReferenceEquals(first.Content, page), "Close/reopen replaced application content.");
            evidence.Add($"PASS {scope}: requested={backdrop}, effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; native full-width/inset marker/border/Back callbacks, unchanged targets, row/separator spacing, zero separator, reset and retained editor state.");
            form.Close();
        }
        evidence.Add("Public-package offscreen native/print checks; visible frame pacing, actual VS, physical input, IME and mixed-monitor review remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Print(Control control, string path)
    {
        using var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, control.ClientRectangle); image.Save(path);
    }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check, FullWidth;
        internal int RowPaints, BackPaints;
        internal Exception? Failure;
        public override void DrawBackstageEntry(RibbonPaintContext context, RibbonItem? item, bool selected)
        {
            if (Check && (item is null || selected && item.Text == "Info"))
            {
                try { VerifyCallback(context, item, selected); }
                catch (Exception error) { Failure ??= error; }
            }
            base.DrawBackstageEntry(context, item, selected);
        }
        private void VerifyCallback(RibbonPaintContext context, RibbonItem? item, bool selected)
        {
                // Replay only the library-owned navigation callback. Native
                // application pages are never printed or replaced.
                using var image = new Bitmap(context.Bounds.Width, context.Bounds.Height); using var graphics = Graphics.FromImage(image);
                var replay = context with { Graphics = graphics, Bounds = new(Point.Empty, image.Size), Focused = false, Hot = false, Pressed = false };
                base.DrawBackstageEntry(replay, item, selected);
                int S(int n) => (int)Math.Round(n * context.DeviceDpi / 96d);
                if (item is null)
                {
                    BackPaints++;
                    var rail = context.Theme.BackstageNavigationUsesAccent ? context.Theme.Accent : context.Theme.BackstageNavigationBackground;
                    var actual = image.GetPixel(image.Width / 2, image.Height / 2);
                    // A translucent premultiplied DIB may round RGB on its
                    // source-copy round trip. It must retain the rail, not ink.
                    Require(Math.Abs(actual.R - rail.R) <= 1 && Math.Abs(actual.G - rail.G) <= 1 && Math.Abs(actual.B - rail.B) <= 1,
                        $"Zero Back glyph/outline still painted ink: actual={actual}, rail={rail}.");
                }
                else
                {
                    RowPaints++; int inset = FullWidth ? 0 : 1;
                    Require(image.GetPixel(inset, image.Height / 2).ToArgb() == Color.DarkOrange.ToArgb(), "Authored navigation border was not consumed.");
                    Require(image.GetPixel(S(13), S(8)).ToArgb() == Color.Magenta.ToArgb(), "Authored marker size/inset/color was not consumed.");
                }
        }
    }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
