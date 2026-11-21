using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace IndependentConsumer;

internal static class Office2007BackstageChecks
{
    internal static int Run(string directory, bool current)
    {
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
            Directory.CreateDirectory(directory); PaintDefaults(directory);
#if !G6_BASELINE
            if (current)
            {
                using var host = new RibbonForm { Text = "Independent Office 2007 Backstage", ClientSize = new(1200, 760) };
                using var ribbon = new RibbonControl { Dock = DockStyle.Top }; host.Controls.Add(ribbon); host.Ribbon = ribbon;
                ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
                ribbon.FileMenu.Visible = true;
                using var info = new Panel { BackColor = Color.White }; info.Controls.Add(new TextBox { Name = "DocumentName", Text = "Quarterly report.docx", Bounds = new(24, 70, 280, 30) });
                info.Controls.Add(new Label { Text = "Document information", AutoSize = true, Location = new(24, 24) });
                using var print = new Panel { BackColor = Color.White }; print.Controls.Add(new NumericUpDown { Name = "Copies", Value = 2, Bounds = new(24, 70, 100, 30) });
                ribbon.Backstage.Items.AddRange(new RibbonBackstagePageItem { Text = "Info", Content = info }, new RibbonButtonItem { Text = "New" },
                    new RibbonButtonItem { Text = "Open" }, new RibbonButtonItem { Text = "Save" }, new RibbonMenuSeparatorItem(), new RibbonBackstagePageItem { Text = "Print", Content = print });
                ribbon.Backstage.FooterItems.AddRange(new RibbonMenuSeparatorItem(), new RibbonButtonItem { Text = "Options" }, new RibbonButtonItem { Text = "Exit" });
                VerifySample(host, ribbon, directory, "independent");
            }
#endif
            File.AppendAllText(Path.Combine(directory, "result.log"), "PASS: 32 old-family Backstage painter sheets, both public renderers at four DPIs.\n");
            return 0;
        }
        catch (Exception error) { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "failure.log"), error.ToString()); return 1; }
    }

    private static void PaintDefaults(string directory)
    {
        var families = new[] { RibbonThemes.Office2024Light, RibbonThemes.Office2019Light, RibbonThemes.Office2013Light, RibbonThemes.Office2010Blue };
        for (int family = 0; family < families.Length; family++) foreach (int dpi in new[] { 96, 120, 144, 192 }) foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(800, 560); using var g = Graphics.FromImage(image);
            var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), families[family], SystemFonts.MessageBoxFont!, dpi);
            painter.DrawBackstageBackground(c, 230);
            using var selected = new RibbonBackstagePageItem { Text = "Info" }; using var hot = new RibbonButtonItem { Text = "Print" }; using var separator = new RibbonMenuSeparatorItem();
            painter.DrawBackstageEntry(c with { Bounds = new(8, 100, 214, 56), BackgroundBounds = c.Bounds }, selected, true);
            painter.DrawBackstageEntry(c with { Bounds = new(8, 160, 214, 56), BackgroundBounds = c.Bounds, Hot = true }, hot, false);
            painter.DrawBackstageEntry(c with { Bounds = new(8, 218, 214, 9), BackgroundBounds = c.Bounds }, separator, false);
            var frame = new Rectangle(10, 280, 780, 120);
            painter.DrawMessageBar(c with { Bounds = frame, IsTopEdgeConnected = true });
            using var notice = new RibbonMessage { Title = "NOTICE", Text = "Document settings" };
            for (int row = 0; row < 2; row++)
                painter.DrawMessage(c with { Bounds = new(10, 280 + row * 60, 780, 60), BackgroundBounds = frame, IsTopEdgeConnected = true }, notice,
                    new(50, 300 + row * 60, 100, 25), new(160, 300 + row * 60, 250, 25), new(20, 300 + row * 60, 20, 20), row == 0);
            notice.Action.Text = "Review";
            painter.DrawMessageAction(c with { Bounds = new(580, 295, 120, 30) }, notice.Action);
            painter.DrawMessageClose(c with { Bounds = new(730, 295, 24, 24), Hot = true }, true);
            image.Save(Path.Combine(directory, $"default-{family}-{dpi}-{office}.png"));
        }
    }

#if !G6_BASELINE
    internal static void VerifySample(RibbonForm host, RibbonControl ribbon, string directory, string name)
    {
        Directory.CreateDirectory(directory);
        ribbon.AnimationLevel = RibbonAnimationLevel.None;
        var oldRoute = ribbon.FileMenuMode; var oldShape = ribbon.ApplicationButtonShape;
        var oldBounds = host.Bounds; var oldTheme = ribbon.Theme; var oldDensity = ribbon.Density; var oldLayout = ribbon.LayoutMode;
        var oldRenderer = ribbon.Renderer; using var probe = new TipProbe();
        ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.Theme = RibbonThemes.Office2007Blue;
        host.Show(); Application.DoEvents();
        var pages = ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().Where(p => p.Content is not null).ToArray();
        Require(pages.Length >= 2, "Two existing authored native pages are available");
        var page = pages[0]; var second = pages[1]; int scopes = 0;
        try
        {
            ribbon.Renderer = probe;
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch }) foreach (var layout in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            {
                ribbon.Density = density; ribbon.LayoutMode = layout; ribbon.Backstage.SelectedPage = page;
                VerifyMessages(ribbon, probe, directory, $"{name}-{density}-{layout}");
                ribbon.IsBackstageOpen = true; Application.DoEvents();
                var surface = All(host).Single(c => c.GetType().Name == "RibbonBackstageSurface");
                var rows = All(surface).OfType<Button>().ToArray();
                var info = rows.Single(b => b.Text == page.Text);
                Require(!rows.Any(b => b.Visible && b.AccessibleName == "Back to document"), "Office2007 uses the Orb without a duplicate Back button");
                var navigation = info.Parent!; int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
                int side = S(ribbon.Theme.BackstageNavigationPadding ?? 4);
                Require(info.Left == side && navigation.ClientSize.Width - info.Right == side, "Native row side insets match");
                int frameTop = S(48) + S(8), interiorTop = frameTop + 2;
                Require(navigation.Top - interiorTop == side, "Native row top inset equals both side insets");
                foreach (var separator in All(surface).Where(c => c.GetType().Name == "SeparatorControl"))
                    Require(separator.Left == side && separator.Parent!.ClientSize.Width - separator.Right == side, "Native separators share balanced row insets");
                var content = page.Content!; var handle = content.Handle; var parent = content.Parent;
                var field = All(content).OfType<TextBox>().FirstOrDefault(); var editorHandle = field?.Handle; var text = field?.Text;
                int validations = 0; bool reject = false;
                System.ComponentModel.CancelEventHandler validation = (_, e) => { validations++; e.Cancel = reject; };
                if (field is not null) field.Validating += validation;
                try
                {
                    if (field is not null)
                    {
                        field.Focus(); Application.DoEvents(); reject = true; ClickSurfaceOrb(ribbon, surface); Application.DoEvents();
                        Require(ribbon.IsBackstageOpen && validations > 0, "Return respects native editor validation"); reject = false;
                    }
                    rows.Single(b => b.Text == second.Text).PerformClick(); Application.DoEvents();
                    Require(ReferenceEquals(ribbon.Backstage.SelectedPage, second), "Page navigation remains native");
                    ribbon.Backstage.SelectedPage = page; Application.DoEvents();
                    Require(content.Handle == handle && ReferenceEquals(content.Parent, parent) && (field is null || field.Handle == editorHandle && field.Text == text), "Native page/editor identity, parent and value survive navigation");
                    Require(host.Controls.GetChildIndex(surface) == 0 && content.Visible, "The native Backstage is in front and its selected content is visible");
                    using (var image = Print(surface)) image.Save(Path.Combine(directory, $"{name}-{density}-{layout}.png"));
                    VerifyAccentAndKeyTips(host, ribbon, surface, probe, directory, $"{name}-{density}-{layout}");
                    host.ClientSize = new(540, 420); Application.DoEvents();
                    Require(content.Handle == handle && content.Width >= 0 && content.Height >= 0, "Narrow layout retains native page");
                    host.ClientSize = new(1200, 760); Application.DoEvents();
                    FilterKey(surface, Keys.F10);
                    foreach (char key in FileTip(ribbon)) FilterKey(surface, key is >= '0' and <= '9' ? Keys.D0 + (key - '0') : Keys.A + (key - 'A'));
                    Application.DoEvents(); Require(!ribbon.IsBackstageOpen, "Orb KeyTip return closes Backstage after validation succeeds");
                    ribbon.IsBackstageOpen = true; Application.DoEvents(); Require(content.Handle == handle, "Reopening retains native content handle");
                    ribbon.IsBackstageOpen = false; scopes++;
                }
                finally { if (field is not null) field.Validating -= validation; }
            }
            ribbon.IsBackstageOpen = true; Application.DoEvents();
            ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Rectangular;
            Require(ribbon.EffectiveApplicationButtonShape == RibbonApplicationButtonShape.Rectangular && ribbon.FileMenuMode == RibbonFileMenuMode.Backstage, "Explicit File shape and route retain authority");
            ribbon.ApplicationButtonShape = null;
            var orb = ribbon.ApplicationButtonBounds;
            var orbPoint = new Point(orb.Left + orb.Width / 2, orb.Top + orb.Height / 4);
            nint packed = (nint)((orbPoint.Y << 16) | (orbPoint.X & 0xffff));
            SendMessage(ribbon.Handle, 0x200, 0, packed); SendMessage(ribbon.Handle, 0x201, 1, packed); SendMessage(ribbon.Handle, 0x202, 0, packed);
            Application.DoEvents(); Require(!ribbon.IsBackstageOpen, "The real caption-overlapping Orb remains a working return target");
            VerifyNativeOrbMotion(host, ribbon, directory);
            File.AppendAllText(Path.Combine(directory, "result.log"), $"PASS: {name}; {scopes} Office2007 Backstage scopes at {ribbon.DeviceDpi} DPI, equal row/separator insets, native accent header/reset, F10 Orb KeyTip paint/toggle/return, pages/editors/validation/reopen/narrow resize and retained File routing.\nLIMIT: native print/message evidence; live appearance, physical input/IME, actual VS, mixed monitors and motion acceptance remain separate.\n");
        }
        finally
        {
            ribbon.IsBackstageOpen = false; ribbon.Theme = oldTheme; ribbon.Density = oldDensity; ribbon.LayoutMode = oldLayout;
            ribbon.ApplicationButtonShape = oldShape; ribbon.FileMenuMode = oldRoute; ribbon.Renderer = oldRenderer; host.Bounds = oldBounds;
        }
    }
    private static void VerifyNativeOrbMotion(RibbonForm host, RibbonControl ribbon, string directory)
    {
        var theme = ribbon.Theme; var level = ribbon.AnimationLevel; var location = host.Location;
        try
        {
            // Inspect native visibility/z-order without activating the host or
            // capturing the desktop. A long duration leaves deterministic room
            // for several ordinary timer ticks before the explicit endpoint.
            host.Location = new(-16000, -16000);
            Application.DoEvents();
            ribbon.Theme = theme with { BackstageMotion = (theme.BackstageMotion ?? new()) with { PaneDuration = 4000 } };
            ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
            Application.DoEvents();
            ribbon.IsBackstageOpen = true;
            if (!ribbon.IsAnimating)
            {
                File.AppendAllText(Path.Combine(directory, "result.log"), "LIMIT: system motion policy disabled native Orb timer checks; no system preference was changed.\n"); return;
            }
            foreach (bool closing in new[] { false, true })
            {
                if (closing) ribbon.IsBackstageOpen = false;
                var plane = FindWindowEx(host.Handle, 0, null, "Ribbon motion");
                Require(plane != 0 && IsNativeShown(plane) && (!IsWindowVisible(host.Handle) || IsWindowVisible(plane)), $"The complete Orb plane is published before native pane motion; closing={closing}, handle={plane}, focus={host.ContainsFocus}, backstage={ribbon.IsBackstageOpen}; {NativeChildTitles(host.Handle)}");
                Require(GetWindowRect(plane, out var anchor), "Read native Orb plane bounds");
                var surface = All(host).Single(c => c.GetType().Name == "RibbonBackstageSurface");
                var clock = System.Diagnostics.Stopwatch.StartNew(); int samples = 0;
                while (clock.ElapsedMilliseconds < 120)
                {
                    Application.DoEvents();
                    Require(ribbon.IsAnimating && plane == FindWindowEx(host.Handle, 0, null, "Ribbon motion") && IsNativeShown(plane), "One shown Orb HWND survives successive pane ticks");
                    Require(GetWindowRect(plane, out var current) && anchor.Equals(current), "The native Orb plane stays stationary");
                    for (nint sibling = GetWindow(plane, 3); sibling != 0; sibling = GetWindow(sibling, 3)) Require(sibling != surface.Handle, "Orb plane stays above the fading native pane");
                    samples++;
                }
                Require(samples > 0, "Native motion frames were observed"); ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
                Require(FindWindowEx(host.Handle, 0, null, "Ribbon motion") == 0 && ribbon.IsBackstageOpen == !closing, "Native endpoint releases the plane and preserves File state");
            }
            File.AppendAllText(Path.Combine(directory, "result.log"), $"PASS: public native Orb opening/closing timer checks; one HWND with its shown-state flag, stationary bounds above the pane and clean endpoint release; owner-visible={IsWindowVisible(host.Handle)}.\n");
        }
        finally { ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsBackstageOpen = false; ribbon.Theme = theme; ribbon.AnimationLevel = level; host.Location = location; }
    }
    [StructLayout(LayoutKind.Sequential)] private readonly record struct NativeBounds(int Left, int Top, int Right, int Bottom);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint FindWindowEx(nint parent, nint after, string? className, string? title);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out NativeBounds bounds);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern nint GetWindowLong(nint window, int index);
    private static bool IsNativeShown(nint window) => ((IntPtr.Size == 8 ? GetWindowLongPtr(window, -16) : GetWindowLong(window, -16)).ToInt64() & 0x10000000) != 0;
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(nint window, System.Text.StringBuilder text, int maximum);
    private static string NativeChildTitles(nint parent)
    {
        var names = new List<string>();
        for (nint child = GetWindow(parent, 5); child != 0; child = GetWindow(child, 2))
        { var text = new System.Text.StringBuilder(256); GetWindowText(child, text, text.Capacity); names.Add($"{child}:{text}:{IsWindowVisible(child)}"); }
        return string.Join("; ", names);
    }
    private static void VerifyMessages(RibbonControl ribbon, TipProbe probe, string directory, string name)
    {
        using var first = new RibbonMessage { Title = "PROTECTED VIEW", Text = "Be careful with files from the Internet." };
        using var second = new RibbonMessage { Title = "SECURITY NOTICE", Text = "Macros are disabled in this document." };
        first.Action.Text = "Enable Editing"; second.Action.Text = "Review Settings";
        int actions = 0, dismissals = 0; first.Action.Click += (_, _) => actions++; first.Dismissed += (_, _) => dismissals++;
        try
        {
            ribbon.MessageBar.Items.AddRange(first, second); Application.DoEvents(); probe.Messages.Clear();
            using var pixels = Print(ribbon);
            Require(probe.Messages.TryGetValue(first, out var a) && probe.Messages.TryGetValue(second, out _), "Shared renderer receives both native message rows");
            var b = probe.Messages[second]; int inset = (int)Math.Round(5 * ribbon.DeviceDpi / 96d);
            foreach (var bounds in new[] { a, b })
            {
                int y = bounds.Height * 3 / 4; var expected = At(ribbon.Theme.MessageBackground, y / (bounds.Height - 1d));
                Require(pixels.GetPixel(bounds.Left + inset, bounds.Top + y).ToArgb() == expected.ToArgb(), "Each native message restarts the pale-gold reference ramp");
            }
            pixels.Save(Path.Combine(directory, name + "-messages.png"));
            first.Action.PerformClick(); Require(actions == 1 && first.IsOpen, "App-owned message action runs once without implicit dismissal");
            first.Dismiss(); Application.DoEvents(); Require(dismissals == 1 && second.IsOpen, "Dismissal preserves the other authored row");
        }
        finally { ribbon.MessageBar.Items.Remove(first); ribbon.MessageBar.Items.Remove(second); }
    }
    private static Color At(RibbonFill fill, double offset)
    {
        var points = new List<(double Offset, Color Color)> { (0, fill.Start) };
        if (fill.IntermediateStops is { } stops) points.AddRange(stops.Select(s => (s.Offset, s.Color)));
        points.Add((1, fill.End));
        var lower = points.Last(p => p.Offset <= offset); var upper = points.First(p => p.Offset > offset);
        double t = (offset - lower.Offset) / (upper.Offset - lower.Offset);
        int L(int a, int b) => (int)Math.Round(a + (b - a) * t);
        return Color.FromArgb(L(lower.Color.A, upper.Color.A), L(lower.Color.R, upper.Color.R), L(lower.Color.G, upper.Color.G), L(lower.Color.B, upper.Color.B));
    }
    private static void VerifyAccentAndKeyTips(RibbonForm host, RibbonControl ribbon, Control surface, TipProbe probe, string directory, string name)
    {
        int y = (int)Math.Round(24 * ribbon.DeviceDpi / 96d), x = surface.Width / 2;
        var original = ribbon.Theme;
        using var blue = Print(surface); var reference = blue.GetPixel(x, y);
        ribbon.Theme = original with { Accent = Color.FromArgb(35, 145, 75) }; Application.DoEvents();
        using (var green = Print(surface))
        {
            var pixel = green.GetPixel(x, y);
            Require(pixel.G > pixel.R + 20 && pixel.G > pixel.B + 20, "Native header follows a green accent while retaining its crease");
            green.Save(Path.Combine(directory, name + "-accent.png"));
        }
        ribbon.Theme = original; Application.DoEvents(); using (var reset = Print(surface)) Require(reset.GetPixel(x, y) == reference, "Accent reset restores the exact default header pixel");
        var priorCaption = host.TitleBarColor; var priorFrame = host.FrameAppearance;
        try
        {
            host.TitleBarColor = Color.Empty; host.FrameAppearance = RibbonWindowFrameAppearance.Standard;
            ribbon.Theme = original with { TitleBarBackgroundFill = original.IdentityAccent!.CreateCaptionFill(original.Accent), TitleBarForeground = Color.White };
            Application.DoEvents();
            using var joined = Print(surface);
            Require(probe.HeaderCanvas.Top == -surface.Top && probe.HeaderCanvas.Height == surface.Top + (int)Math.Round(48 * ribbon.DeviceDpi / 96d), "Colored standard caption and Backstage header expose one complete gradient canvas");
            joined.Save(Path.Combine(directory, name + "-joined-caption.png"));
        }
        finally { ribbon.Theme = original; host.TitleBarColor = priorCaption; host.FrameAppearance = priorFrame; }
        surface.SelectNextControl(null, true, true, true, true); probe.Tips.Clear(); FilterKey(surface, Keys.F10);
        using (var keys = Print(surface))
        {
            var orb = surface.RectangleToClient(ribbon.RectangleToScreen(ribbon.ApplicationButtonBounds));
            Require(probe.Tips.Any(t => t.Keys == FileTip(ribbon) && orb.Contains(t.Bounds) && surface.ClientRectangle.Contains(t.Bounds)), "Public KeyTip hook paints File's badge on the visible Orb");
            keys.Save(Path.Combine(directory, name + "-keytips.png"));
        }
        FilterKey(surface, Keys.F10); probe.Tips.Clear(); using (var hidden = Print(surface)) Require(probe.Tips.Count == 0, "F10 hides the Orb and navigation badges together");
    }
    private static string FileTip(RibbonControl ribbon) => string.IsNullOrWhiteSpace(ribbon.FileMenu.KeyTip) ? "F" : ribbon.FileMenu.KeyTip.Trim().ToUpperInvariant();
    private static void FilterKey(Control target, Keys key)
    {
        var message = Message.Create(target.Handle, 0x100, (nint)key, 0);
        Require(target.PreProcessMessage(ref message), "The public Backstage control keyboard route consumes " + key);
    }
    private sealed class TipProbe : RibbonOffice2024Renderer
    {
        internal List<(string Keys, Rectangle Bounds)> Tips = [];
        internal Rectangle HeaderCanvas;
        internal Dictionary<RibbonMessage, Rectangle> Messages = [];
        public override void DrawMessage(RibbonPaintContext context, RibbonMessage message, Rectangle title, Rectangle text, Rectangle icon, bool first)
        { Messages[message] = context.Bounds; base.DrawMessage(context, message, title, text, icon, first); }
        public override void DrawKeyTip(RibbonPaintContext context, string keys) { Tips.Add((keys, context.Bounds)); base.DrawKeyTip(context, keys); }
        public override void DrawBackstageHeader(RibbonPaintContext context, RibbonDropDownItem file, Rectangle bounds)
        { HeaderCanvas = context.BackstageHeaderBackgroundBounds; base.DrawBackstageHeader(context, file, bounds); }
    }
    private static Bitmap Print(Control control)
    {
        // DrawToBitmap prints hidden sibling pages and can reverse sibling Z
        // order. Print each visible HWND once, then compose in native Z order.
        var image = new Bitmap(Math.Max(1, control.Width), Math.Max(1, control.Height));
        using var g = Graphics.FromImage(image);
        nint dc = g.GetHdc();
        try { SendMessage(control.Handle, 0x317, dc, 0xE); }
        finally { g.ReleaseHdc(dc); }
        foreach (Control child in control.Controls.Cast<Control>().Where(c => c.Visible && c.Width > 0 && c.Height > 0).Reverse())
        {
            using var pixels = Print(child); var saved = g.Save();
            try
            {
                g.SetClip(control.ClientRectangle, System.Drawing.Drawing2D.CombineMode.Intersect);
                if (child.Region is { } region) { using var clip = region.Clone(); clip.Translate(child.Left, child.Top); g.SetClip(clip, System.Drawing.Drawing2D.CombineMode.Intersect); }
                g.DrawImageUnscaled(pixels, child.Location);
            }
            finally { g.Restore(saved); }
        }
        return image;
    }
    private static void ClickSurfaceOrb(RibbonControl ribbon, Control surface)
    {
        var orb = surface.RectangleToClient(ribbon.RectangleToScreen(ribbon.ApplicationButtonBounds));
        var point = new Point(orb.Left + orb.Width / 2, Math.Min(surface.Height - 1, orb.Bottom - 4));
        nint packed = (nint)((point.Y << 16) | (point.X & 65535));
        SendMessage(surface.Handle, 0x201, 1, packed); SendMessage(surface.Handle, 0x202, 0, packed);
    }
    private static IEnumerable<Control> All(Control root)
    {
        foreach (Control child in root.Controls) { yield return child; foreach (var descendant in All(child)) yield return descendant; }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
#endif
}
