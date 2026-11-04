using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class LargeCommandGeometryCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            var r = new Record(); using RibbonRenderer painter = office ? new Office(r) : new Flat(r);
            using var ribbon = new RibbonControl { Width = 1400, Renderer = painter, Density = density, AnimationLevel = RibbonAnimationLevel.None };
            var group = new RibbonGroup { Text = "Large layouts", CanResize = false }; var tab = new RibbonTab { Text = "Commands" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            group.Items.Add(new RibbonButtonItem { Text = "Small", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small) });
            RibbonItem[] items = [new RibbonButtonItem { Text = "Large button" }, new RibbonDropDownItem { Text = "Large dropdown" },
                new RibbonSplitButtonItem { Text = "Large paste" }, new RibbonSplitButtonItem { Text = "Vertical paste", Layout = RibbonSplitButtonLayout.Vertical },
                new RibbonSplitButtonItem { Text = "Keep source formatting", Layout = RibbonSplitButtonLayout.Vertical }, new RibbonButtonItem { Text = "Text only" }];
            foreach (var item in items) { if (item != items[^1]) item.Icon = RibbonStockIcons.Create("open"); item.SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large); group.Items.Add(item); }
            var original = ribbon.Theme; var before = Draw("default");
            ribbon.Theme = original with { CommandContentHorizontalPadding = 12, CommandContentVerticalPadding = 16 }; _ = Draw("roomier");
            var reference = items[4]; Require(r.Lines[reference].Count == 2, "Wrapped reference caption");
            int iconTop = r.Images[reference].Top, captionTop = r.Lines[reference][0].Top, arrowTop = r.Arrows[reference].Top, bandBottom = r.Arrows[reference].Bottom;
            foreach (var item in items)
            {
                var text = r.Lines[item].Aggregate(Rectangle.Union); bool vertical = item is RibbonSplitButtonItem { Layout: RibbonSplitButtonLayout.Vertical };
                var bounds = r.Bounds[item]; Require(r.Lines[item][0].Top == captionTop, "Large caption baselines align, including unused icon/arrow bands");
                if (r.Images.TryGetValue(item, out var image) && !image.IsEmpty) Require(image.Top == iconTop, "Large icon rows align: " + item.Text);
                Require(Math.Abs(iconTop + bandBottom - bounds.Top - bounds.Bottom) <= 1, "The common Large band reservation is centered");
                if (vertical) { var parts = r.Parts[(RibbonSplitButtonItem)item]; Require(r.Arrows[item].Top == arrowTop && parts.Primary.Contains(r.Images[item]) && parts.DropDown.Contains(text) && parts.DropDown.Contains(r.Arrows[item]), "Short/wrapped vertical split arrows and action regions align"); }
            }
            ribbon.Theme = original; Require(before.SequenceEqual(Draw("reset")), "Default pixels restored");
            evidence.Add($"PASS: {office}/{density}; native DPI={ribbon.DeviceDpi}; automatic Large shared icon/caption/arrow bands, short/wrapped/iconless alignment, action regions and exact reset.");

            byte[] Draw(string state)
            {
                r.Clear(); using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle);
                using var stream = new MemoryStream(); image.Save(stream, System.Drawing.Imaging.ImageFormat.Png); var png = stream.ToArray();
                File.WriteAllBytes(Path.Combine(output, $"large-public-{office}-{density}-{state}.png"), png); return png;
            }
        }
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Record
    {
        internal RibbonItem? Current;
        internal readonly Dictionary<RibbonItem, Rectangle> Bounds = [], Images = [], Arrows = [];
        internal readonly Dictionary<RibbonItem, List<Rectangle>> Lines = [];
        internal readonly Dictionary<RibbonSplitButtonItem, (Rectangle Primary, Rectangle DropDown)> Parts = [];
        internal void Clear() { Current = null; Bounds.Clear(); Images.Clear(); Arrows.Clear(); Lines.Clear(); Parts.Clear(); }
        internal void Text(Rectangle bounds) { if (Current is { } item) { if (!Lines.TryGetValue(item, out var lines)) Lines[item] = lines = []; lines.Add(bounds); } }
    }
    private sealed class Flat(Record r) : RibbonRenderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Current = item; r.Bounds[item] = c.Bounds; try { base.DrawItem(c, item, size, menu); } finally { r.Current = null; } }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Current = item; r.Bounds[item] = Rectangle.Union(p.Bounds, d.Bounds); r.Parts[item] = (p.Bounds, d.Bounds); try { base.DrawSplitItem(p, d, item, size, menu); } finally { r.Current = null; } }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { r.Images[item] = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Text(c.Bounds); base.DrawText(c, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { if (r.Current is { } item) r.Arrows[item] = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
    private sealed class Office(Record r) : RibbonOffice2024Renderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Current = item; r.Bounds[item] = c.Bounds; try { base.DrawItem(c, item, size, menu); } finally { r.Current = null; } }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext d, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { r.Current = item; r.Bounds[item] = Rectangle.Union(p.Bounds, d.Bounds); r.Parts[item] = (p.Bounds, d.Bounds); try { base.DrawSplitItem(p, d, item, size, menu); } finally { r.Current = null; } }
        protected override void DrawItemImage(RibbonPaintContext c, RibbonItem item, RibbonItemSize size) { r.Images[item] = c.Bounds; base.DrawItemImage(c, item, size); }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { r.Text(c.Bounds); base.DrawText(c, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext c, Color color) { if (r.Current is { } item) r.Arrows[item] = c.Bounds; base.DrawDropDownArrow(c, color); }
    }
}
