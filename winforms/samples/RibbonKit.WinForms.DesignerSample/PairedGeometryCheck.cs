using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Public, package-only automatic command layout/paint. Native Application Menu
// container/hosting/input is verified separately by the library's STA tests.
internal static class PairedGeometryCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        {
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
            using var ribbon = new RibbonControl { Width = 2200, Density = density, LayoutMode = mode, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None };
            var group = new RibbonGroup { CanResize = false }; var tab = new RibbonTab();
            var small = new RibbonToggleItem { Text = "Bold", Icon = RibbonStockIcons.Create("bold"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small), SimplifiedSize = RibbonItemSize.Small, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var medium = new RibbonButtonItem { Text = "Open document", Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var split = new RibbonSplitButtonItem { Text = "Paste", Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            split.Items.Add(new RibbonButtonItem { Text = "Text only" }); group.Items.AddRange(small, medium, split); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            var original = ribbon.Theme; var before = Draw("default"); var old = record.Bounds.ToDictionary(p => p.Key, p => p.Value);
            ribbon.Theme = original with { CommandContentHorizontalPadding = 12, CommandContentVerticalPadding = 16,
                ApplicationMenuNavigationMinimumRowHeight = 64, ApplicationMenuPaneMinimumRowHeight = 72, ApplicationMenuFooterMinimumRowHeight = 52,
                ApplicationMenuPaneHorizontalPadding = 22, ApplicationMenuFooterItemGap = 12 };
            _ = Draw("override"); int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
            using (var menu = new Bitmap(S(600), S(360)))
            using (var graphics = Graphics.FromImage(menu))
            {
                var nav = new Rectangle(S(5) + 1, S(5) + 1, S(162), S(330));
                var pane = new Rectangle(nav.Right + 1 + S(22), nav.Top + S(32), S(320), S(290));
                painter.DrawApplicationMenuBackground(new(graphics, new(Point.Empty, menu.Size), ribbon.Theme, ribbon.Font, ribbon.DeviceDpi), nav, pane, Rectangle.Empty);
                Require(menu.GetPixel(nav.Right, nav.Top + nav.Height / 2).ToArgb() == ribbon.Theme.Border.ToArgb(), "Public Application Menu background joins the roomier navigation boundary");
                menu.Save(Path.Combine(output, $"public-application-join-{office}-{density}-{mode}.png"));
            }
            foreach (var item in new RibbonItem[] { small, medium, split })
            {
                Require(record.Bounds.TryGetValue(item, out var row), "Every automatic command painted");
                Require(row.Height >= Math.Max(ribbon.Font.Height, S(16)) + 2 * S(16), "Padded command height");
                Require(row.Width >= old[item].Width + 2 * (S(12) - S(5)), "Padded command/primary width budget");
            }
            ribbon.Theme = original; Require(before.SequenceEqual(Draw("reset")), "Exact default paint restored");
            try { ribbon.Theme = original with { ApplicationMenuFooterItemGap = -1 }; throw new InvalidOperationException("Invalid Application Menu gap accepted"); }
            catch (ArgumentOutOfRangeException) { Require(ribbon.Theme == original, "Atomic theme rejection"); }
            evidence.Add($"PASS: {office}/{density}/{mode}; native DPI={ribbon.DeviceDpi}; automatic command layout/paint, combined seven-role assignment, Application Menu background join and exact reset.");

            byte[] Draw(string label)
            {
                record.Bounds.Clear(); using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle);
                using var stream = new MemoryStream(); pixels.Save(stream, System.Drawing.Imaging.ImageFormat.Png); var png = stream.ToArray();
                File.WriteAllBytes(Path.Combine(output, $"public-paired-{office}-{density}-{mode}-{label}.png"), png); return png;
            }
        }
        using (var form = new DesignerForm()) form.VerifyPairedDemoContract();
        evidence.Add("PASS: permanent sample apply/reset independence, mode toggles, original File/default/footer/QAT models and prior geometry. No shown windows, global input, internals or Showcase helpers. Application Menu native layout/scroll/hover/retained HWND is library STA evidence; actual VS, input and display acceptance remains separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Record { internal readonly Dictionary<RibbonItem, Rectangle> Bounds = []; }
    private sealed class Flat(Record record) : RibbonRenderer
    {
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu) { if (!menu) record.Bounds[item] = context.Bounds; base.DrawItem(context, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext arrow, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { if (!menu) record.Bounds[item] = primary.Bounds; base.DrawSplitItem(primary, arrow, item, size, menu); }
    }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    {
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu) { if (!menu) record.Bounds[item] = context.Bounds; base.DrawItem(context, item, size, menu); }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext arrow, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { if (!menu) record.Bounds[item] = primary.Bounds; base.DrawSplitItem(primary, arrow, item, size, menu); }
    }
}
