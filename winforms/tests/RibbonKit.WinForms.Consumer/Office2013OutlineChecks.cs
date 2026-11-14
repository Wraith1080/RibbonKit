using RibbonKit.WinForms;

namespace IndependentConsumer;

// Copied into a package-only application, without internals or sample helpers.
internal static class Office2013OutlineChecks
{
    internal static int Run(string directory, bool nativeOnly = false, bool paintOnly = false)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
            if (!nativeOnly) foreach (bool office in new[] { false, true }) foreach (int dpi in new[] { 96, 120, 144, 192 }) Paint(office, dpi);
            if (!paintOnly) foreach (var mode in Enum.GetValues<RibbonLayoutMode>()) foreach (var density in Enum.GetValues<RibbonDensity>()) Native(mode, density);
            log.Add("LIMIT: synthetic-DPI direct paint and offscreen native windows; no physical input, actual VS, desktop material pixels, mixed-monitor or user visual acceptance.");
            File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 0;
        }
        catch (Exception error) { log.Add("FAIL: " + error); File.WriteAllLines(Path.Combine(directory, "result.log"), log); return 1; }

        void Paint(bool office, int dpi)
        {
            int S(int n) => (int)Math.Round(n * dpi / 96d);
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(800), S(340)); using var g = Graphics.FromImage(image); g.Clear(Color.White);
            for (int row = 0; row < 2; row++) for (int col = 0; col < 2; col++)
            {
                var theme = col == 0 ? RibbonThemes.Office2013Light : RibbonThemes.Office2013Light with
                { BodyBorder = Color.Navy, BodyAppearance = new() { BorderWidth = 3 }, HeaderAppearance = new() { ConnectSelectedTabToBody = true, FilledBorderColor = Color.DarkRed, FilledBorderWidth = 2 } };
                var header = new Rectangle(S(60 + col * 400), S(10 + row * 170), S(140), S(40));
                var body = new Rectangle(S(5 + col * 400), header.Bottom, S(390), S(110));
                var clip = row == 0 ? header : new Rectangle(header.Left + S(35), header.Top, S(85), header.Height);
                int stroke = col == 0 ? 1 : S(2);
                var gap = new Rectangle(Math.Max(header.Left + stroke, clip.Left), body.Top,
                    Math.Min(header.Right - stroke, clip.Right) - Math.Max(header.Left + stroke, clip.Left), body.Height);
                var c = new RibbonPaintContext(g, body, theme, SystemFonts.MessageBoxFont!, dpi) { HeaderBodyConnectionBounds = gap };
                renderer.DrawBody(c); using var tab = new RibbonTab { Text = "Selected tab" };
                var saved = g.Save(); g.SetClip(clip); renderer.DrawTab(c with { Bounds = header }, tab, true); g.Restore(saved);
                int center = gap.Left + gap.Width / 2;
                Require(image.GetPixel(center, header.Top).ToArgb() == (col == 0 ? theme.BodyBorder : Color.DarkRed).ToArgb(), "Selected header top outline");
                Require(image.GetPixel(center, header.Bottom - 1).ToArgb() == Color.White.ToArgb() && image.GetPixel(center, body.Top).ToArgb() == Color.White.ToArgb(), "Open header bottom/body top without a double seam");
                Require(image.GetPixel(body.Left + S(10), body.Top).ToArgb() == theme.BodyBorder.ToArgb(), "Body top continues outside the selected tab");
                Require(image.GetPixel(body.Left, body.Top + S(20)).ToArgb() == theme.BodyBorder.ToArgb() && image.GetPixel(body.Right - 1, body.Bottom - S(20)).ToArgb() == theme.BodyBorder.ToArgb(), "Body side outlines");
                Require(image.GetPixel(body.Left + S(10), body.Bottom - 1).ToArgb() == theme.BodyBorder.ToArgb(), "Body bottom outline");
            }
            image.Save(Path.Combine(directory, $"outline-{(office ? "office" : "neutral")}-{dpi}.png"));
            log.Add($"PASS: public {(office ? "Office" : "neutral")} renderer/{dpi} DPI; reference and authored borders, full/clipped selected tabs, open seam and body perimeter.");
        }

        void Native(RibbonLayoutMode mode, RibbonDensity density)
        {
            using var form = new RibbonForm { Text = "Public Office 2013 outline", StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1080, 620), ShowInTaskbar = false };
            using var renderer = new Recorder();
            using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2013Light, Renderer = renderer, LayoutMode = mode, Density = density, AnimationLevel = RibbonAnimationLevel.None };
            var home = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Editing" };
            var field = new RibbonComboBoxItem { Text = "Name", IsEditable = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, Value = "Retained outline field" };
            group.Items.Add(field);
            group.Items.Add(new RibbonToggleItem { Text = "Bold", Checked = true, Icon = RibbonStockIcons.Create("bold") }); home.Groups.Add(group); ribbon.Tabs.Add(home);
            for (int i = 0; i < 5; i++) ribbon.Tabs.Add(new RibbonTab { Text = i == 4 ? "A selected tab wider than the available viewport for clipping" : "Document layout " + i, IsContextual = i == 3, ContextualColor = Color.Purple });
            ribbon.FileMenu.Visible = true; ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save", Icon = RibbonStockIcons.Create("save") });
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
            var editor = Descendants(ribbon).OfType<ComboBox>().Single(); editor.Focus(); editor.Select(2, 5);
            var handle = editor.Handle; var parent = editor.Parent; string value = editor.Text;
            foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
            foreach (bool colored in new[] { false, true })
            {
                ribbon.QuickAccessPosition = position; form.TitleBarColor = colored ? Color.Purple : Color.Empty; ribbon.Theme = RibbonThemes.Office2013Light with { TitleBarForeground = colored ? Color.White : Color.Empty };
                ribbon.SelectedTab = home; Capture($"{mode}-{density}-{position}-{colored}-home");
                ribbon.SelectedTab = ribbon.Tabs[3]; Capture($"{mode}-{density}-{position}-{colored}-contextual");
                ribbon.SelectedTab = home;
                Require(editor.Handle == handle && ReferenceEquals(parent, editor.Parent) && editor.Text == value && editor.SelectionStart == 2 && editor.SelectionLength == 5, "Native editor handle/parent/text/selection retained");
            }
            form.Width = 490; ribbon.SelectedTab = ribbon.Tabs[^1]; Capture($"{mode}-{density}-off-viewport");
            ribbon.SelectedTab = home; home.Text = "A selected tab wider than the available viewport for partial clipping";
            foreach (var other in ribbon.Tabs.Where(t => !ReferenceEquals(t, home))) other.Visible = false;
            Capture($"{mode}-{density}-clipped"); home.Text = "Home";
            foreach (var other in ribbon.Tabs) other.Visible = true;
            ribbon.SelectedTab = home; var basis = ribbon.Theme;
            ribbon.Theme = basis with { HeaderAppearance = basis.HeaderAppearance! with { ConnectSelectedTabToBody = false } };
            Capture($"{mode}-{density}-connection-off", false);
            ribbon.Theme = basis; Capture($"{mode}-{density}-connection-reset");
            ribbon.IsMinimized = true;
            using (var minimized = new Bitmap(ribbon.Width, ribbon.Height)) { ribbon.DrawToBitmap(minimized, new(Point.Empty, minimized.Size)); Require(renderer.Tab.HeaderBodyConnectionBounds.IsEmpty, "Minimized header has no body opening"); }
            ribbon.IsMinimized = false; Capture($"{mode}-{density}-restored");
            ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
            ribbon.SelectedTab = ribbon.Tabs[1];
            bool animated = ribbon.IsAnimating;
            if (animated)
            {
                // The selection setter synchronously publishes motion frame zero.
                // Inspect its public body hook before another root paint can hide
                // a missing connection in the transient layer.
                var opening = renderer.Body.HeaderBodyConnectionBounds;
                Require(opening.Height > 0 && opening.Left >= renderer.Tab.Bounds.Left && opening.Right <= renderer.Tab.Bounds.Right && opening.Top == renderer.Tab.Bounds.Bottom, "First live Expressive/SlideDown frame uses the committed selected-tab connection");
                var watch = System.Diagnostics.Stopwatch.StartNew();
                while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1500) { Application.DoEvents(); Thread.Sleep(1); }
                Require(!ribbon.IsAnimating, "Live tab content motion completes");
            }
            using var rootStyles = new StyleProbe(ribbon.Handle); using var hostStyles = new StyleProbe(form.Handle);
            int completed = 0, interrupted = 0;
            if (animated) for (int i = 0; i < 12; i++)
            {
                var next = ribbon.Tabs[i % 3]; if (ReferenceEquals(next, ribbon.SelectedTab)) continue;
                ribbon.SelectedTab = next; Require(ribbon.IsAnimating, "Repeated tab selection still animates");
                if (i % 3 == 0)
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1500) { Application.DoEvents(); Thread.Sleep(1); }
                    Require(!ribbon.IsAnimating, "Repeated tab animation finishes"); completed++;
                }
                else
                {
                    ribbon.SelectedTab = ribbon.Tabs[(i + 1) % 3];
                    Require(!ribbon.IsAnimating, "Rapid selection commits without leaving a drawing behind"); interrupted++;
                }
                Require(editor.Handle == handle && ReferenceEquals(parent, editor.Parent) && editor.Text == value, "Repeated handoff retains the native editor");
            }
            Require(rootStyles.Adds == 0 && rootStyles.Removals == 0 && hostStyles.Adds == 0 && hostStyles.Removals == 0, "Stable tab handoffs do not relayer the live ribbon or form");
            File.WriteAllText(Path.Combine(directory, $"handoff-{mode}-{density}.json"), System.Text.Json.JsonSerializer.Serialize(new { completed, interrupted, RibbonLayerAdds = rootStyles.Adds, RibbonLayerRemovals = rootStyles.Removals, HostLayerAdds = hostStyles.Adds, HostLayerRemovals = hostStyles.Removals }));
            ribbon.AnimationLevel = RibbonAnimationLevel.None;
            log.Add($"PASS: {mode}/{density}, actual {ribbon.DeviceDpi} DPI; three QAT placements, neutral/colored captions, contextual/clipped tabs, connection off/Reset, minimized/restored, public frame payload and native editor retention. Live Expressive/SlideDown first-frame connection: {(animated ? "PASS" : "not run: system/eligibility policy kept selection immediate")}; repeated handoffs {completed} complete/{interrupted} interrupted; zero live ribbon/form layer-style changes.");

            void Capture(string name, bool connected = true)
            {
                renderer.Tab = default; renderer.TabClip = Rectangle.Empty;
                using var capture = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(capture, new(Point.Empty, capture.Size));
                var body = renderer.Body.Bounds; var gap = renderer.Body.HeaderBodyConnectionBounds;
                capture.Save(Path.Combine(directory, name + ".png"));
                Require(body.Width > 0 && body.Height > 0, "Actual body hook received its frame");
                bool visible = renderer.Tab.Bounds.Width > 0;
                Require(connected && visible ? gap.Height > 0 : gap.IsEmpty, $"Connection payload follows policy: {name}; body={body}; tab={renderer.Tab.Bounds}; clip={renderer.TabClip}; gap={gap}");
                if (connected && visible)
                {
                    Require(gap.Left >= renderer.TabClip.Left && gap.Right <= renderer.TabClip.Right && gap.Top == body.Top, "Public opening stays in the selected tab viewport");
                    int center = gap.Left + gap.Width / 2;
                    Require(capture.GetPixel(center, body.Top).ToArgb() == Color.White.ToArgb(), "Native body hook leaves the selected opening clear");
                    Require(capture.GetPixel(center, renderer.Tab.Bounds.Bottom - 1).ToArgb() == Color.White.ToArgb(), "Native selected header bottom stays open");
                }
                else Require(capture.GetPixel(visible ? renderer.Tab.Bounds.Left + renderer.Tab.Bounds.Width / 2 : body.Left + body.Width / 2, body.Top).ToArgb() == ribbon.Theme.BodyBorder.ToArgb(), "Opt-out or an off-viewport selection keeps the body top complete");
                capture.Save(Path.Combine(directory, name + ".png"));
            }
        }
    }

    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal RibbonPaintContext Body, Tab; internal Rectangle TabClip;
        public override void DrawBody(RibbonPaintContext c) { Body = c; base.DrawBody(c); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected)
        { if (selected) { Tab = c; TabClip = Rectangle.Ceiling(c.Graphics.VisibleClipBounds); } base.DrawTab(c, tab, selected); }
    }
    private sealed class StyleProbe : NativeWindow, IDisposable
    {
        internal int Adds, Removals;
        internal StyleProbe(nint handle) => AssignHandle(handle);
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x7D && m.WParam == -20)
            {
                var styles = System.Runtime.InteropServices.Marshal.PtrToStructure<Styles>(m.LParam);
                if ((styles.Old & 0x80000) == 0 && (styles.New & 0x80000) != 0) Adds++;
                if ((styles.Old & 0x80000) != 0 && (styles.New & 0x80000) == 0) Removals++;
            }
            base.WndProc(ref m);
        }
        public void Dispose() => ReleaseHandle();
    }
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct Styles { internal uint Old, New; }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
}
