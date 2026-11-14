using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Public package APIs, native local messages and paint callbacks only. No global input.
internal static class TabHeaderClippingCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        foreach (int qat in new[] { 0, 1, 2 })
        {
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Neutral(record);
            using var form = new Form { Width = 1020, Height = 500, StartPosition = FormStartPosition.Manual, Location = new(80, 80) };
            var ribbon = new RibbonControl { Density = density, LayoutMode = mode, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None,
                QuickAccessVisible = qat != 0, QuickAccessPosition = RibbonQuickAccessPosition.TabRow, QuickAccessMaxWidth = qat == 2 ? 80 : 300 };
            ribbon.FileMenu.Visible = true;
            foreach (string title in new[] { "Menu geometry", "Illustrated menus", "Command splits", "Command content", "Application Menu geometry", "Gallery geometry", "Backstage geometry", "Frame geometry" }) ribbon.Tabs.Add(new RibbonTab { Text = title });
            for (int i = 0; i < 4; i++) ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save " + i, Icon = RibbonStockIcons.Create("save") });
            var sources = ribbon.QuickAccessItems.ToArray(); var tabs = ribbon.Tabs.ToArray(); form.Controls.Add(ribbon); form.Show(); Application.DoEvents();
            using (var initial = Draw()) { }
            Require(!record.Next.IsEmpty, "Tab forward scroller must be present.");
            Require(qat != 2 || !record.Overflow.IsEmpty, "QAT overflow must be present.");
            var viewport = record.Viewport; var file = record.File; var quick = record.Quick;
            for (int click = 1; click <= 2; click++)
            {
                var point = new Point(record.Next.Left + record.Next.Width / 2, record.Next.Top + record.Next.Height / 2);
                nint location = (nint)((point.Y << 16) | (point.X & 0xffff)); SendMessage(ribbon.Handle, 0x201, 1, location); SendMessage(ribbon.Handle, 0x202, 0, location); Application.DoEvents();
                using var tagged = Draw(); int escaped = 0, visible = 0;
                for (int y = viewport.Top; y < viewport.Bottom; y++) for (int x = 0; x < tagged.Width; x++)
                { var c = tagged.GetPixel(x, y); if (c.R > 160 && c.B > 160 && c.G < 120) { if (viewport.Contains(x, y)) visible++; else escaped++; } }
                string name = $"{office}-{density}-{mode}-{qat}-{click}"; tagged.Save(Path.Combine(output, "tagged-" + name + ".png"));
                record.Tag = false; using (var actual = Draw()) actual.Save(Path.Combine(output, "actual-" + name + ".png")); record.Tag = true;
                Require(visible > 20 && escaped == 0, $"{name}: {escaped} tab glyph pixels escaped the viewport.");
                Require(record.Partial && viewport == record.Viewport && file == record.File && quick == record.Quick, "Scroll must expose a partially visible tab without moving File/QAT/viewport.");
            }
            Require(sources.SequenceEqual(ribbon.QuickAccessItems) && tabs.SequenceEqual(ribbon.Tabs), "Sources and tab ownership retained.");
            evidence.Add($"PASS renderer={office} density={density} mode={mode} qat={qat} nativeDpi={ribbon.DeviceDpi}: two real tab-scroll clicks; glyph clipping beside File/QAT/overflow; stable slots and owners."); form.Close();
            Bitmap Draw() { record.Partial = false; var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size)); return image; }
        }
        evidence.Add("Package-only native fixture; no global input or runtime internals. Composited glyph clipping and synthetic 96/120/144/192 DPI are separate unit evidence. VS, actual material hardware/mixed-monitor/physical-input review remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Record
    {
        internal Rectangle Viewport, Next, File, Quick, Overflow; internal bool InTab, Partial, Tag = true;
        internal void Tab(RibbonPaintContext c) { Viewport = Rectangle.Round(c.Graphics.ClipBounds); Partial |= c.Bounds.Left < Viewport.Left; InTab = true; }
        internal void Scroll(RibbonPaintContext c, bool backward) { if (!backward && c.Bounds.Top == Viewport.Top) Next = c.Bounds; }
    }
    private sealed class Neutral(Record r) : RibbonRenderer
    {
        public override void DrawTab(RibbonPaintContext c, RibbonTab t, bool selected) { r.Tab(c); try { base.DrawTab(c, t, selected); } finally { r.InTab = false; } }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => base.DrawText(c, text, r.InTab && r.Tag ? Color.Magenta : color, flags);
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem item) { r.File = c.Bounds; base.DrawFileButton(c, item); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition p) { r.Quick = c.Bounds; base.DrawQuickAccessToolbar(c, p); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawScrollButton(RibbonPaintContext c, bool backward) { r.Scroll(c, backward); base.DrawScrollButton(c, backward); }
    }
    private sealed class Office(Record r) : RibbonOffice2024Renderer
    {
        public override void DrawTab(RibbonPaintContext c, RibbonTab t, bool selected) { r.Tab(c); try { base.DrawTab(c, t, selected); } finally { r.InTab = false; } }
        public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) => base.DrawText(c, text, r.InTab && r.Tag ? Color.Magenta : color, flags);
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem item) { r.File = c.Bounds; base.DrawFileButton(c, item); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition p) { r.Quick = c.Bounds; base.DrawQuickAccessToolbar(c, p); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
        public override void DrawScrollButton(RibbonPaintContext c, bool backward) { r.Scroll(c, backward); base.DrawScrollButton(c, backward); }
    }
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
}
