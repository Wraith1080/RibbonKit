using RibbonKit.WinForms;

internal static class Program
{
    private static readonly List<string> Log = [];
    [STAThread]
    private static int Main(string[] args)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        if (args.ElementAtOrDefault(0) == "--orb-uia-client") return OrbUiaClient.Run(args[1], args[2], args[3]);
        var directory = args[0]; Directory.CreateDirectory(directory);
        try
        {
            if (args.ElementAtOrDefault(1) is "orb-only" or "orb-features-only")
            {
                if (args[1] == "orb-only") OrbCompositionChecks.Defaults(directory, Log);
#if G2
                OrbCompositionChecks.Features(directory, Log);
#endif
            }
            else
            {
                if (args.ElementAtOrDefault(1) != "features-only") Defaults(directory);
#if G2
                Features(directory);
#endif
            }
            Log.Add("LIMIT: direct paint/offscreen native evidence; live visual review, actual VS, physical input/IME and mixed-monitor behavior remain separate.");
            return 0;
        }
        catch (Exception error) { Log.Add("FAIL: " + error); return 1; }
        finally { File.WriteAllLines(Path.Combine(directory, "result.log"), Log); }
    }
    private static void Defaults(string directory)
    {
        int count = 0;
        foreach (var (name, theme) in new[] { ("2024", RibbonThemes.Office2024Light), ("2019", RibbonThemes.Office2019Light), ("2013", RibbonThemes.Office2013Light), ("2010", RibbonThemes.Office2010Blue) })
        foreach (bool office in new[] { false, true }) foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(700, 310); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
            using var group = new RibbonGroup { Text = "Default caption" };
            for (int i = 0; i < 8; i++)
            {
                var c = new RibbonPaintContext(g, new(5 + i % 4 * 175, 5 + i / 4 * 150, 165, 140), theme, Control.DefaultFont, dpi, Hot: i is 2 or 5, Pressed: i == 3);
                var state = g.Save(); g.SetClip(c.Bounds);
                painter.DrawBody(c); painter.DrawGroup(c, group, i >= 4);
                if (i < 4) painter.DrawItem(c with { Bounds = Rectangle.Inflate(c.Bounds, -10, -25) }, new RibbonToggleItem { Checked = i == 1 }, RibbonItemSize.Large, false);
                g.Restore(state);
            }
            var file = Path.Combine(directory, $"default-{name}-{office}-{dpi}.png"); image.Save(file); count++;
#if G2
            using var emptyImage = new Bitmap(image.Width, image.Height); using var emptyGraphics = Graphics.FromImage(emptyImage); emptyGraphics.Clear(Color.Magenta);
            for (int i = 0; i < 8; i++)
            {
                var c = new RibbonPaintContext(emptyGraphics, new(5 + i % 4 * 175, 5 + i / 4 * 150, 165, 140), theme with { BodyAppearance = (theme.BodyAppearance ?? new()) with { GroupPanel = new() } }, Control.DefaultFont, dpi, Hot: i is 2 or 5, Pressed: i == 3);
                var state = emptyGraphics.Save(); emptyGraphics.SetClip(c.Bounds);
                painter.DrawBody(c); painter.DrawGroup(c, group, i >= 4);
                if (i < 4) painter.DrawItem(c with { Bounds = Rectangle.Inflate(c.Bounds, -10, -25) }, new RibbonToggleItem { Checked = i == 1 }, RibbonItemSize.Large, false);
                emptyGraphics.Restore(state);
            }
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) Require(image.GetPixel(x, y) == emptyImage.GetPixel(x, y), "Empty group settings retain default pixels");
#endif
        }
        Log.Add($"PASS: {count} default public-painter images (four families, both renderers, four DPI values, expanded/collapsed/rest/hot/pressed/checked); current empty records also match.");
    }
#if G2
    private static void Features(string directory)
    {
        var crease = new RibbonFill(Color.White, Color.Black) { IntermediateStops = RibbonGradientStops.WithHardTransitions([new(.5, Color.White), new(.5, Color.Black)]) };
        foreach (bool office in new[] { false, true }) foreach (bool horizontal in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(65, 65); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
            painter.DrawSurface(new(g, new(0, 16, 65, 33), RibbonThemes.Office2010Blue with { BackgroundFill = crease with { Direction = horizontal ? RibbonGradientDirection.Horizontal : RibbonGradientDirection.Vertical } }, Control.DefaultFont, 144) { BackgroundBounds = new(0, 0, 65, 65) }, false);
            for (int i = 16; i < 49; i++) Require(image.GetPixel(horizontal ? i : 32, horizontal ? 32 : i).ToArgb() == (i < 32 ? Color.White : Color.Black).ToArgb(), "Exact hard transition and full-surface coordinates");
            Require(image.GetPixel(32, 15).ToArgb() == Color.Magenta.ToArgb(), "Clipped surface stays contained");
        }
        Log.Add("PASS: both public painters retain exact repeated offsets on both axes under a partial surface clip.");
        using var host = new Form { Bounds = new(-16000, -16000, 1100, 700), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var hooks = new Hooks(); using var ribbon = new RibbonControl { Renderer = hooks, AnimationLevel = RibbonAnimationLevel.None };
        var tab = new RibbonTab { Text = "Consumer" }; var group = new RibbonGroup { Text = "Native editor", CanResize = false };
        var field = new RibbonTextBoxItem { Id = "native-field", Text = "Name", Value = "Retained selection", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        group.Items.Add(field); tab.Groups.Add(group);
        for (int i = 0; i < 4; i++)
        {
            var section = new RibbonGroup { Text = "Commands " + i };
            for (int j = 0; j < 5; j++) section.Items.Add(new RibbonButtonItem { Text = "Command " + j, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline });
            tab.Groups.Add(section);
        }
        ribbon.Tabs.Add(tab); host.Controls.Add(ribbon); host.Show(); Application.DoEvents();
        var native = Children(ribbon).OfType<TextBox>().Single(t => t.Name == field.Id); var handle = native.Handle; var parent = native.Parent; native.Select(2, 4);
        var panel = new RibbonGroupPanelAppearanceSettings { Background = new(Color.FromArgb(222, 232, 245), Color.FromArgb(217, 232, 246)) { IntermediateStops = new([new(.16, Color.FromArgb(199, 216, 237))]) }, BorderColor = Color.FromArgb(168, 191, 212), InnerBorderColor = Color.FromArgb(228, 236, 245), LabelBackground = new(Color.FromArgb(193, 217, 241)), LabelForeground = Color.FromArgb(21, 66, 139) };
        int scopes = 0;
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>()) foreach (var density in Enum.GetValues<RibbonDensity>()) foreach (int width in new[] { 1100, 500 })
        {
            host.ClientSize = new(width, 700); ribbon.LayoutMode = mode; ribbon.Density = density; var original = ribbon.Theme;
            hooks.Items.Clear(); using var before = Print(ribbon); var itemBounds = hooks.Items.ToArray(); var height = ribbon.Height; var nativeBounds = native.Bounds; bool nativeVisible = native.Visible;
            ribbon.Theme = original with { BodyAppearance = (original.BodyAppearance ?? new()) with { GroupPanel = panel } };
            hooks.Items.Clear(); hooks.Panels.Clear(); using var after = Print(ribbon);
            Require(itemBounds.SequenceEqual(hooks.Items), "Measured item bounds stay unchanged"); Require(ribbon.Height == height, "Decoration retains ribbon height");
            Require(hooks.Panels.Count > 0 && hooks.Panels.All(c => !c.GroupFrameBounds.IsEmpty), "Public decoration hook receives measured frames");
            Require(native.Handle == handle && ReferenceEquals(parent, native.Parent) && native.Text == "Retained selection" && native.SelectionStart == 2 && native.SelectionLength == 4, "Native editor HWND/parent/text/selection survive decoration");
            Require(native.Bounds == nativeBounds && native.Visible == nativeVisible, "Native editor bounds and visibility survive decoration");
            after.Save(Path.Combine(directory, $"panel-{mode}-{density}-{width}.png"));
            ribbon.Theme = original; hooks.Items.Clear(); using var reset = Print(ribbon); Require(itemBounds.SequenceEqual(hooks.Items), "Reset retains targets");
            scopes++;
        }
        Log.Add($"PASS: {scopes} public-only native scopes at {ribbon.DeviceDpi} DPI: Classic/Simplified, Compact/Touch, wide/reduced; hook geometry, item bounds, Reset and editor HWND/parent/text/selection retained.");
    }
    private sealed class Hooks : RibbonOffice2024Renderer
    {
        internal List<(string Id, Rectangle Bounds)> Items { get; } = [];
        internal List<RibbonPaintContext> Panels { get; } = [];
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu) { Items.Add((item.Id, context.Bounds)); base.DrawItem(context, item, size, menu); }
        public override void DrawGroupPanel(RibbonPaintContext context, RibbonGroup group, bool collapsed) { Panels.Add(context); base.DrawGroupPanel(context, group, collapsed); }
    }
    private static IEnumerable<Control> Children(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Children(c)));
    private static Bitmap Print(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, control.ClientRectangle); return image; }
#endif
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
