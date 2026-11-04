using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Observe real automatic layout through public paint callbacks in the package
// consumer. No shown windows, internals, external input or Showcase dependencies.
internal static class ChromeGeometryCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        foreach (var placement in new[] { RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon })
        {
            var r = new Record(); using RibbonRenderer painter = office ? new Office(r) : new Flat(r);
            using var ribbon = new RibbonControl { Width = 1600, Renderer = painter, Density = density, LayoutMode = mode,
                QuickAccessPosition = placement, AnimationLevel = RibbonAnimationLevel.None };
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Formatting", CanResize = false };
            for (int i = 0; i < 4; i++) group.Items.Add(new RibbonButtonItem { Text = "Open " + i, Icon = RibbonStockIcons.Create("open"),
                SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline });
            tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.Tabs.Add(new RibbonTab { Text = "Document layout" });
            ribbon.FileMenu.Visible = true; ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "New" });
            for (int i = 0; i < 5; i++) ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Quick " + i, Icon = RibbonStockIcons.Create("open") });
            var message = new RibbonMessage { Title = "Notice", Text = "Document settings can be reviewed here." }; message.Action.Text = "Review"; ribbon.MessageBar.Items.Add(message);
            var original = ribbon.Theme; var qat = ribbon.QuickAccessItems.ToArray(); var commands = group.Items.ToArray();
            var before = Draw("default"); var oldGroup = r.Group;
            ribbon.Theme = ChromeGeometrySettings.Utilities(ChromeGeometrySettings.Header(ChromeGeometrySettings.Body(original)));
            _ = Draw("override"); int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
            Require(r.Tabs.Count == 2 && r.Tabs.Values.All(b => b.Width >= S(90)), "Automatic tab minima");
            if (mode == RibbonLayoutMode.Classic) Require(r.Group.Width >= oldGroup.Width + 2 * (S(12) - S(6)), "Automatic group width reserves padding");
            var quick = r.Quick.Values.OrderBy(b => b.X).ToArray();
            Require(quick.Length > 0, "Automatic QAT entries remain visible");
            for (int i = 1; i < quick.Length; i++) Require(quick[i].Left - quick[i - 1].Right == S(6), "Automatic QAT gap");
            Require(r.Text.Left - r.Title.Right == S(12), "Automatic message title/text gap");
            Require(r.Action.Left >= r.Text.Right + S(18) && !r.Action.IntersectsWith(r.Text), "Automatic message action clearance");
            Require(qat.SequenceEqual(ribbon.QuickAccessItems) && commands.SequenceEqual(group.Items), "Original model ownership");
            ribbon.Theme = ChromeGeometrySettings.ResetUtilities(ChromeGeometrySettings.ResetHeader(ChromeGeometrySettings.ResetBody(ribbon.Theme)));
            Require(ribbon.Theme == original && before.SequenceEqual(Draw("reset")), "Independent resets restore exact default pixels");
            evidence.Add($"PASS: {office}/{density}/{mode}/{placement}; native DPI={ribbon.DeviceDpi}; automatic body/group/tab/QAT/message paint, source identity and exact reset.");

            byte[] Draw(string state)
            {
                r.Clear(); using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, ribbon.ClientRectangle);
                using var stream = new MemoryStream(); pixels.Save(stream, System.Drawing.Imaging.ImageFormat.Png); var png = stream.ToArray();
                File.WriteAllBytes(Path.Combine(output, $"chrome-public-{office}-{density}-{mode}-{placement}-{state}.png"), png); return png;
            }
        }
        using (var form = new DesignerForm()) form.VerifyChromeDemoContract();
        evidence.Add("PASS: permanent demo presets/reset independence, earlier editor/command/menu roles, message action/dismiss/reopen, QAT cycle, layout mode and authored model ownership. Library STA separately covers TitleBar/native HWND, synthetic DPI/font/reduction and target contracts. Actual VS/input/hardware acceptance remains separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Record
    {
        internal readonly Dictionary<RibbonTab, Rectangle> Tabs = []; internal readonly Dictionary<RibbonElement, Rectangle> Quick = [];
        internal Rectangle Group, Title, Text, Action;
        internal void Clear() { Tabs.Clear(); Quick.Clear(); Group = Title = Text = Action = Rectangle.Empty; }
    }
    private sealed class Flat(Record r) : RibbonRenderer
    {
        public override void DrawTab(RibbonPaintContext c, RibbonTab t, bool selected) { r.Tabs[t] = c.Bounds; base.DrawTab(c, t, selected); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup g, bool collapsed) { r.Group = c.Bounds; base.DrawGroup(c, g, collapsed); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu) { if (!menu) r.Quick[item] = c.Bounds; base.DrawQuickAccessItem(c, item, menu); }
        public override void DrawMessage(RibbonPaintContext c, RibbonMessage m, Rectangle title, Rectangle text, Rectangle icon, bool first) { r.Title = title; r.Text = text; base.DrawMessage(c, m, title, text, icon, first); }
        public override void DrawMessageAction(RibbonPaintContext c, RibbonButtonItem a) { r.Action = c.Bounds; base.DrawMessageAction(c, a); }
    }
    private sealed class Office(Record r) : RibbonOffice2024Renderer
    {
        public override void DrawTab(RibbonPaintContext c, RibbonTab t, bool selected) { r.Tabs[t] = c.Bounds; base.DrawTab(c, t, selected); }
        public override void DrawGroup(RibbonPaintContext c, RibbonGroup g, bool collapsed) { r.Group = c.Bounds; base.DrawGroup(c, g, collapsed); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu) { if (!menu) r.Quick[item] = c.Bounds; base.DrawQuickAccessItem(c, item, menu); }
        public override void DrawMessage(RibbonPaintContext c, RibbonMessage m, Rectangle title, Rectangle text, Rectangle icon, bool first) { r.Title = title; r.Text = text; base.DrawMessage(c, m, title, text, icon, first); }
        public override void DrawMessageAction(RibbonPaintContext c, RibbonButtonItem a) { r.Action = c.Bounds; base.DrawMessageAction(c, a); }
    }
}
