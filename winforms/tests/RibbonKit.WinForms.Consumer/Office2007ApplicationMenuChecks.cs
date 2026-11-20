using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace IndependentConsumer;

// Public API only. Also compiled against the retained G4 package for old-family paint comparison.
internal static class Office2007ApplicationMenuChecks
{
    internal static int Run(string directory, bool current = true)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); Directory.CreateDirectory(directory);
        try
        {
            Defaults(directory);
#if !G4_BASELINE
            if (current)
            {
                using var form = new RibbonForm(); using var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None };
                form.Controls.Add(ribbon); form.Ribbon = ribbon; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
                var split = new RibbonSplitButtonItem { Text = "Save As" }; int primaryCalls = 0, footerCalls = 0; split.Click += (_, _) => primaryCalls++;
                var nested = new RibbonDropDownItem { Text = "More formats" }; nested.Items.Add(new RibbonButtonItem { Text = "Format" }); split.Items.Add(nested);
                for (int i = 0; i < 35; i++) split.Items.Add(new RibbonButtonItem { Text = "Format " + i });
                using var body = new UserControl { Size = new(450, 350) }; var field = new TextBox { Text = "Retained native content", Dock = DockStyle.Top }; body.Controls.Add(field);
                var custom = new RibbonDropDownItem { Text = "Native page", Content = body, DropDownHeader = "Application content" };
                var disabled = new RibbonDropDownItem { Text = "Disabled parent", Enabled = false }; var disabledChild = new RibbonButtonItem { Text = "Disabled child" }; disabled.Items.Add(disabledChild);
                int disabledCalls = 0; disabledChild.Click += (_, _) => disabledCalls++;
                ribbon.FileMenu.Items.AddRange(new RibbonButtonItem { Text = "New" }, split, custom, disabled);
                ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent Documents"; ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Text = "Recent report" });
                var footer = new RibbonButtonItem { Text = "Options" }; footer.Click += (_, _) => footerCalls++; ribbon.ApplicationMenu.FooterItems.Add(footer);
                VerifySample(form, ribbon, directory, "independent");
                ribbon.FileMenu.ShowDropDown(); custom.ShowDropDown(); Application.DoEvents();
                var hwnd = field.Handle; var parent = field.Parent; field.Select(2, 5);
                split.ShowDropDown(); custom.ShowDropDown(); Application.DoEvents();
                Require(field.Handle == hwnd && ReferenceEquals(parent, field.Parent) && field.Text == "Retained native content" && field.SelectionStart == 2 && field.SelectionLength == 5, "Native editor retains HWND/parent/text/selection across panes");
                bool veto = true; field.Validating += (_, e) => e.Cancel = veto;
                split.ShowDropDown(); Require(custom.IsDropDownOpen && !split.IsDropDownOpen, "Native validation vetoes a pane switch");
                veto = false; split.ShowDropDown(); Require(split.IsDropDownOpen && primaryCalls == 0, "Pane selection never executes split primary");
                var root = Surfaces().Single(); nested.ShowDropDown(); Application.DoEvents(); Require(Surfaces().Count == 2, "Nested menu opens independently");
                Escape(Surfaces().Single(c => !ReferenceEquals(c, root))); Require(ribbon.FileMenu.IsDropDownOpen && !nested.IsDropDownOpen, "First Escape closes deepest menu");
                Escape(root); Require(!ribbon.FileMenu.IsDropDownOpen, "Second Escape closes File");
                ribbon.FileMenu.ShowDropDown(); split.ShowDropDown(); Child(Surfaces().Single().AccessibilityObject, split.Text).DoDefaultAction();
                Require(primaryCalls == 1 && !ribbon.FileMenu.IsDropDownOpen, "Split primary invokes once and dismisses File");
                ribbon.FileMenu.ShowDropDown(); Child(Surfaces().Single().AccessibilityObject, footer.Text).DoDefaultAction();
                disabledChild.PerformClick();
                Require(footerCalls == 1 && !ribbon.FileMenu.IsDropDownOpen && disabledCalls == 0, "Footer invokes once and disabled inheritance remains");
                File.AppendAllText(Path.Combine(directory, "result.log"), "PASS: independent split/footer actions, nested Escape ordering, disabled inheritance, native validation and HWND/parent/text/selection retention.\n");
            }
#endif
            return 0;
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(directory, "failure.log"), error.ToString()); return 1; }
    }

    private static void Defaults(string directory)
    {
        foreach (var (name, theme) in new[] { ("2024", RibbonThemes.Office2024Light), ("2019", RibbonThemes.Office2019Light), ("2013", RibbonThemes.Office2013Light), ("2010", RibbonThemes.Office2010Blue) })
        foreach (bool office in new[] { false, true }) foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int n) => (int)Math.Round(n * dpi / 96d, MidpointRounding.AwayFromZero);
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(500), S(330)); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
            var c = new RibbonPaintContext(g, new(0, 0, image.Width, image.Height), theme, Control.DefaultFont, dpi)
            {
#if !G4_BASELINE
                IsApplicationMenu = true
#endif
            };
            painter.DrawPopupFrame(c);
            painter.DrawApplicationMenuBackground(c with { Bounds = new(1, 1, image.Width - 2, image.Height - 2) }, new(S(6) + 1, S(28) + 1, S(140), S(250)), new(S(163), S(60), S(320), S(216)), new(S(6), S(290), S(488), S(30)));
            image.Save(Path.Combine(directory, $"default-{name}-{office}-{dpi}.png"));
        }
        File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: 32 old-family Application Menu/frame paint images across both painters and four DPIs.\n");
    }

#if !G4_BASELINE
    internal static void VerifySample(RibbonForm form, RibbonControl ribbon, string directory, string name)
    {
        Directory.CreateDirectory(directory); var originalRenderer = ribbon.Renderer; var originalShape = ribbon.ApplicationButtonShape; var originalRoute = ribbon.FileMenuMode;
        using var painter = new Recorder(); ribbon.Renderer = painter; ribbon.AnimationLevel = RibbonAnimationLevel.None;
        var tile = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonGalleryItem>().SelectMany(g => g.Items).FirstOrDefault(i => i.Text == "Office 2007 Blue");
        if (tile is null) ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2007Blue; else tile.PerformClick();
        ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu; ribbon.FileMenu.Visible = true;
        form.StartPosition = FormStartPosition.Manual; form.Bounds = new(-16000, -16000, 1100, 740); form.ShowInTaskbar = false; form.Show(); Application.DoEvents();
        var split = ribbon.FileMenu.Items.OfType<RibbonSplitButtonItem>().First(i => i.Visible && i.Enabled);
        int scopes = 0;
        try
        {
            foreach (var mode in Enum.GetValues<RibbonLayoutMode>()) foreach (var density in Enum.GetValues<RibbonDensity>()) foreach (var shape in Enum.GetValues<RibbonApplicationButtonShape>())
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.ApplicationButtonShape = shape;
                painter.Frames.Clear(); ribbon.FileMenu.ShowDropDown(); Application.DoEvents(); var surface = Surfaces().Single(); var popup = surface.Parent!; var origin = popup.Location;
                Capture(surface, $"{name}-{mode}-{density}-{shape}-default");
                Require(painter.Background.IsApplicationMenu && painter.Frames.Count > 0 && painter.Frames.All(c => c.IsApplicationMenu), "Actual menu surface and native/layered frame hooks carry root role");
                Require(painter.Frames.Any(c => c.Bounds.Left > 0), "Layered frame hook retains the menu role");
                split.ShowDropDown(); Application.DoEvents(); Require(split.IsDropDownOpen && popup.Location == origin, "Pane switching retains the menu origin");
                Require(surface.Controls.OfType<VScrollBar>().Any() || split.Items.Count < 15, "Large panes retain native scrolling");
                Capture(surface, $"{name}-{mode}-{density}-{shape}-pane");
                if (scopes == 0) StateAtlas(painter, split, directory, ribbon.DeviceDpi);
                form.ClientSize = new(1050, 700); Application.DoEvents(); ribbon.FileMenu.CloseDropDown();
                Require(!ribbon.FileMenu.IsDropDownOpen, "Resize/close releases menu state"); form.ClientSize = new(1100, 740); scopes++;
            }
            File.AppendAllText(Path.Combine(directory, "result.log"), $"PASS: {name}; {scopes} native scopes at {ribbon.DeviceDpi} DPI: Classic/Simplified, Compact/Touch, Orb/rectangular; default/active panes, measured origins, native scrolling, resize/close and actual surface/native/layered renderer hooks.\nLIMIT: offscreen/native evidence; live menu appearance review, physical input, actual VS, material pixels and mixed-monitor acceptance remain separate.\n");
        }
        finally { ribbon.FileMenu.CloseDropDown(); ribbon.Renderer = originalRenderer; ribbon.ApplicationButtonShape = originalShape; ribbon.FileMenuMode = originalRoute; }
        void Capture(Control surface, string file) { using var image = new Bitmap(surface.Width, surface.Height); surface.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(directory, file + ".png")); }
    }
    private static void StateAtlas(Recorder painter, RibbonSplitButtonItem split, string directory, int dpi)
    {
        var (primary, arrow) = painter.Splits[split]; var whole = Rectangle.Union(primary.Bounds, arrow.Bounds);
        using var image = new Bitmap(whole.Width, whole.Height * 4); using var g = Graphics.FromImage(image); g.Clear(Color.FromArgb(250, 250, 250));
        for (int state = 0; state < 4; state++)
        {
            var p = primary.Bounds; p.Offset(-whole.Left, state * whole.Height - whole.Top); var a = arrow.Bounds; a.Offset(-whole.Left, state * whole.Height - whole.Top);
            painter.DrawSplitItem(primary with { Graphics = g, Bounds = p, Hot = state == 1, Pressed = state == 3 }, arrow with { Graphics = g, Bounds = a, Hot = state == 2 }, split, RibbonItemSize.Medium, true);
        }
        int mainX = Math.Min(primary.Bounds.Width - 5, Math.Max(4, (int)Math.Round(4 * dpi / 96d)));
        int arrowX = arrow.Bounds.Left - whole.Left + arrow.Bounds.Width / 2;
        int y = whole.Height / 2;
        // Main sample is before the large-icon slot; arrow sample is above the glyph.
        y = Math.Max(4, y / 2);
        var dim = image.GetPixel(mainX, y); var hot = image.GetPixel(mainX, whole.Height + y); var press = image.GetPixel(mainX, 3 * whole.Height + y);
        Require(dim != hot && press != hot && image.GetPixel(arrowX, y) == image.GetPixel(arrowX, whole.Height + y), "Open split dims only its primary, hover is gold and primary press is deeper while arrow stays gold");
        image.Save(Path.Combine(directory, "split-states-" + dpi + ".png"));
    }
    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal RibbonPaintContext Background;
        internal readonly List<RibbonPaintContext> Frames = [];
        internal readonly Dictionary<RibbonSplitButtonItem, (RibbonPaintContext Primary, RibbonPaintContext Arrow)> Splits = [];
        public override void DrawPopupFrame(RibbonPaintContext c) { Frames.Add(c); base.DrawPopupFrame(c); }
        public override void DrawApplicationMenuBackground(RibbonPaintContext c, Rectangle n, Rectangle p, Rectangle f) { Background = c; base.DrawApplicationMenuBackground(c, n, p, f); }
        public override void DrawSplitItem(RibbonPaintContext p, RibbonPaintContext a, RibbonSplitButtonItem item, RibbonItemSize size, bool menu) { if (menu) Splits[item] = (p, a); base.DrawSplitItem(p, a, item, size, menu); }
    }
    private static void Escape(Control control) { control.Focus(); var key = Message.Create(control.Handle, 0x100, (nint)Keys.Escape, 0); Require(control.PreProcessMessage(ref key), "Escape routed through native menu"); Application.DoEvents(); }
    private static List<Control> Surfaces()
    {
        var result = new List<Control>(); EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (Control.FromHandle(window) is ToolStripDropDown { Visible: true } popup) foreach (var host in popup.Items.OfType<ToolStripControlHost>()) if (host.Control.GetType().Name == "RibbonPopupSurface") result.Add(host.Control); return true; }, 0); return result;
    }
    private static AccessibleObject Child(AccessibleObject root, string text) { for (int i = 0; i < root.GetChildCount(); i++) if (root.GetChild(i) is { } child && child.Name == text) return child; throw new InvalidOperationException("Missing accessibility row: " + text); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private delegate bool WindowCallback(nint window, nint argument);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, WindowCallback callback, nint argument);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
#endif
}
