using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Observe real public-control painting and automatic layout, without showing a window.
internal static class CommandSplitGeometryCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        {
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Flat(record);
            using var ribbon = new RibbonControl { Width = 1800, Density = density, LayoutMode = mode, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None };
            var group = new RibbonGroup { CanResize = false }; var tab = new RibbonTab();
            RibbonSplitButtonItem[] splits = [Make("Small", RibbonItemSize.Small), Make("Medium paste", RibbonItemSize.Medium), Make("Large paste", RibbonItemSize.Large), Make("Vertical paste", RibbonItemSize.Large)];
            splits[3].Layout = RibbonSplitButtonLayout.Vertical; group.Items.AddRange(splits); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            var original = ribbon.Theme; var before = Draw("default"); var old = record.Parts.ToDictionary(p => p.Key, p => p.Value);
            ribbon.Theme = original with { CommandSplitArrowWidth = 48, CommandSplitPrimaryMinimumWidth = 104, CommandLargeSplitPrimaryMinimumWidth = 144 };
            _ = Draw("override"); int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
            Require(record.Parts.Count == 4, "All automatic split presentations were painted");
            foreach (var split in splits)
            {
                var parts = record.Parts[split];
                bool vertical = split.Layout == RibbonSplitButtonLayout.Vertical && mode == RibbonLayoutMode.Classic;
                if (vertical) { Require(parts.Primary.Size == old[split].Primary.Size && parts.Arrow.Size == old[split].Arrow.Size, "Large vertical dimensions retained"); continue; }
                Require(parts.Arrow.Width == S(48), "Automatic arrow slot uses public theme override");
                Require(parts.Primary.Width >= S(split == splits[2] && mode == RibbonLayoutMode.Classic ? 144 : 104), "Automatic primary minimum");
                Require(parts.Primary.Width >= old[split].Primary.Width && parts.Primary.Right == parts.Arrow.Left && !parts.Primary.IntersectsWith(parts.Arrow), "Caption budget and tiled action regions");
            }
            ribbon.Theme = ribbon.Theme with { CommandSplitArrowWidth = null, CommandSplitPrimaryMinimumWidth = null, CommandLargeSplitPrimaryMinimumWidth = null };
            Require(ribbon.Theme == original, "Independent three-role reset"); Require(before.SequenceEqual(Draw("reset")), "Exact default paint restored");
            try { ribbon.Theme = original with { CommandSplitArrowWidth = 0 }; throw new InvalidOperationException("Invalid split width accepted"); }
            catch (ArgumentOutOfRangeException) { Require(ribbon.Theme == original, "Rejected theme remains atomic"); }
            evidence.Add($"PASS: {office}/{density}/{mode}; public control native DPI={ribbon.DeviceDpi}; automatic layout/paint and exact reset.");

            byte[] Draw(string label)
            {
                record.Parts.Clear(); using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle);
                using var stream = new MemoryStream(); pixels.Save(stream, System.Drawing.Imaging.ImageFormat.Png); byte[] png = stream.ToArray();
                File.WriteAllBytes(Path.Combine(output, $"public-command-split-{office}-{density}-{mode}-{label}.png"), png); return png;
            }
        }
        using (var form = new DesignerForm()) form.VerifyCommandSplitDemoContract();
        evidence.Add("PASS: permanent demo apply/reset, primary state, mode toggle and QAT source ownership preserve prior editor/illustrated geometry and File mode. No shown windows, global input, internals or Showcase helpers. Library STA tests separately cover synthetic DPI, native hit/keyboard/UIA/action contracts. Actual VS/input/material/hardware acceptance remains separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static RibbonSplitButtonItem Make(string text, RibbonItemSize size)
    {
        var split = new RibbonSplitButtonItem { Text = text, Checked = true, Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(size),
            SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedSize = size == RibbonItemSize.Small ? RibbonItemSize.Small : RibbonItemSize.Medium };
        split.Items.Add(new RibbonButtonItem { Text = "Text only" }); return split;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Record { internal readonly Dictionary<RibbonSplitButtonItem, (Rectangle Primary, Rectangle Arrow)> Parts = []; }
    private sealed class Office(Record record) : RibbonOffice2024Renderer
    { public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext arrow, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { record.Parts[item] = (primary.Bounds, arrow.Bounds); base.DrawSplitItem(primary, arrow, item, size, menu); } }
    private sealed class Flat(Record record) : RibbonRenderer
    { public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext arrow, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { record.Parts[item] = (primary.Bounds, arrow.Bounds); base.DrawSplitItem(primary, arrow, item, size, menu); } }
}
