using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace IndependentConsumer;

// This branch compiles against the retained F3 package too. New API/native
// scopes are enabled only for the current package and explicit sample checks.
internal static class Office2010BackstageChecks
{
    internal static int Run(string directory)
    {
        Directory.CreateDirectory(directory);
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
            foreach (var (name, theme) in new[] { ("2013", RibbonThemes.Office2013Light), ("2019", RibbonThemes.Office2019Light), ("2024", RibbonThemes.Office2024Light) })
            foreach (bool office in new[] { false, true }) foreach (int dpi in new[] { 96, 144 })
            {
                using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
                using var image = new Bitmap(700, 400); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
                var c = new RibbonPaintContext(g, new(0, 0, image.Width, image.Height), theme, SystemFonts.MessageBoxFont!, dpi);
                painter.DrawBackstageBackground(c, 230);
                painter.DrawBackstageEntry(c with { Bounds = new(20, 15, 44, 44), BackgroundBounds = c.Bounds }, null, false);
                for (int i = 0; i < 4; i++)
                {
                    using var entry = new RibbonButtonItem { Text = "Entry " + i };
                    painter.DrawBackstageEntry(c with { Bounds = new(0, 90 + i * 50, 230, 46), BackgroundBounds = c.Bounds, Hot = i == 1, Pressed = i == 2 }, entry, i == 0);
                }
                image.Save(Path.Combine(directory, $"legacy-{name}-{office}-{dpi}.png"));
            }
#if RKWF_F4
            using var form = new RibbonForm(); using var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None, QuickAccessVisible = true };
            var home = new RibbonTab { Text = "Home" }; var edit = new RibbonTab { Text = "Edit" }; var group = new RibbonGroup { Text = "Commands" };
            var save = new RibbonButtonItem { Text = "Save" }; int invoked = 0; save.Click += (_, _) => invoked++;
            group.Items.Add(save); home.Groups.Add(group); ribbon.Tabs.AddRange(home, edit); ribbon.FileMenu.Visible = true;
            ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = save });
            using var notes = new RichTextBox { Text = "Public-package native content", Dock = DockStyle.Fill };
            using var print = new TextBox { Text = "Public-package print settings", Dock = DockStyle.Fill };
            ribbon.Backstage.Items.AddRange(new RibbonBackstagePageItem { Text = "Home", Content = notes }, new RibbonBackstagePageItem { Text = "Print", Content = print }); ribbon.Backstage.FooterItems.Add(new RibbonButtonItem { Text = "Options" });
            form.Controls.Add(ribbon); form.Ribbon = ribbon;
            Verify(form, ribbon, Path.Combine(directory, "native"), "independent");
            using var qatProbe = new Probe(); var previous = ribbon.Renderer; ribbon.Renderer = qatProbe; _probe = qatProbe;
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar; ribbon.IsBackstageOpen = true; CaptureRibbon(ribbon);
            var qat = qatProbe.Qat.First(); Click(ribbon, qat); Require(invoked == 1 && !ribbon.IsBackstageOpen, "Exposed QAT uses its source action and close validation");
            ribbon.Renderer = previous; _probe = null;
#endif
            File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: legacy Backstage paint captured.\n"); return 0;
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(directory, "failure.log"), error.ToString()); return 1; }
    }
#if RKWF_F4
    private static Probe? _probe;
    internal static void Verify(RibbonForm form, RibbonControl ribbon, string directory, string name)
    {
        Directory.CreateDirectory(directory);
        var tile = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonGalleryItem>().SelectMany(g => g.Items).FirstOrDefault(i => i.Text == "Office 2010 Blue");
        if (tile is not null) tile.PerformClick(); else ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2010Blue;
        ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
        var previousRenderer = ribbon.Renderer; var probe = new Probe(); _probe = probe; ribbon.Renderer = probe;
        form.StartPosition = FormStartPosition.Manual; form.Bounds = new(-16000, -16000, 1150, 760); form.ShowInTaskbar = false;
        form.Show(); Application.DoEvents();
        var logical = ribbon.SelectedTab!; var other = ribbon.Tabs.First(t => !ReferenceEquals(t, logical) && t.Visible && t.Enabled);
        var page = ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().First(p => p.Content is not null && p.Visible);
        ribbon.IsBackstageOpen = true; Application.DoEvents();
        var pane = AncestorUnder(page.Content!, form);
        var editors = Descendants(page.Content!).OfType<TextBoxBase>().ToArray();
        var state = editors.Select(c => (Control: c, Handle: c.Handle, Parent: c.Parent, c.Text, c.SelectionStart, c.SelectionLength, Rtf: (c as RichTextBox)?.Rtf)).ToArray();
        try
        {
            foreach (var mode in Enum.GetValues<RibbonLayoutMode>()) foreach (var density in Enum.GetValues<RibbonDensity>()) foreach (var position in Enum.GetValues<RibbonQuickAccessPosition>())
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.QuickAccessPosition = position;
                form.ClientSize = new(1100, 720); Application.DoEvents(); CaptureRibbon(ribbon);
                int top = form.PointToClient(ribbon.PointToScreen(new(0, probe.HeaderBottom))).Y;
                Require(pane.Top == top && pane.Width == form.ClientSize.Width && pane.Bottom == form.ClientSize.Height, "Pane follows measured headers in host-client coordinates");
                Require(form.Controls.GetChildIndex(pane) < form.Controls.GetChildIndex(ribbon), "Live native pane stays in front of covered ribbon body");
                Require(ReferenceEquals(logical, ribbon.SelectedTab) && probe.SelectedTabs == 0, "Only visual selection is suspended");
                Require(!pane.Controls.Cast<Control>().Any(c => c.AccessibleName == "Back" && c.Visible), "Redundant Back target hidden");
                if (position != RibbonQuickAccessPosition.BelowRibbon && ribbon.QuickAccessItems.Count > 0) Require(probe.Qat.Count > 0, "Exposed QAT stays painted");
                Retained();
            }
            if (ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().FirstOrDefault(p => !ReferenceEquals(p, page) && p.Content is not null && p.Visible && p.Enabled) is { } alternate)
            {
                ribbon.Backstage.SelectedPage = alternate; Application.DoEvents(); ribbon.Backstage.SelectedPage = page; Application.DoEvents(); Retained();
            }
            ribbon.LayoutMode = RibbonLayoutMode.Classic; ribbon.Density = RibbonDensity.Compact; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            CaptureForm("default");
            ribbon.Theme = ribbon.Theme with { Accent = Color.Purple }; Application.DoEvents();
            using (var image = new Bitmap(pane.Width, pane.Height))
            {
                pane.DrawToBitmap(image, new(Point.Empty, image.Size));
                var theme = ribbon.Theme; var recipe = theme.IdentityAccent!;
                var open = theme.FileButtonAppearance!.OpenBackground ?? recipe.AdaptFill(recipe.FileOpenBackground, theme.Accent, recipe.FileStrength);
                Require(image.GetPixel(image.Width - 1, 0).ToArgb() == open.End.ToArgb(), "Top seam follows effective open File ramp");
                image.Save(Path.Combine(directory, name + "-purple-pane.png"));
            }
            CaptureForm("purple"); Retained(); ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2010Blue;
            CaptureRibbon(ribbon); Click(ribbon, probe.Tabs[other]); Require(!ribbon.IsBackstageOpen && ReferenceEquals(ribbon.SelectedTab, other), "Native other-tab click closes and selects");
            ribbon.IsBackstageOpen = true; CaptureRibbon(ribbon); Click(ribbon, probe.Tabs[other]); Require(!ribbon.IsBackstageOpen && ReferenceEquals(ribbon.SelectedTab, other), "Native selected-tab click closes only");
            ribbon.IsBackstageOpen = true; CaptureRibbon(ribbon); Click(ribbon, probe.File); Require(!ribbon.IsBackstageOpen, "Native File click closes");
            ribbon.IsBackstageOpen = true;
            if (editors.FirstOrDefault() is { } validator)
            {
                System.ComponentModel.CancelEventHandler cancel = (_, e) => e.Cancel = true;
                validator.Validating += cancel;
                try { CaptureRibbon(ribbon); Click(ribbon, probe.Tabs[logical]); Require(ribbon.IsBackstageOpen && ReferenceEquals(ribbon.SelectedTab, other), "Validation cancels exposed-header close/select"); }
                finally { validator.Validating -= cancel; }
            }
            var custom = ribbon.Theme with { BackstageAppearance = new() { BackButtonVisible = true, TopSeamHeight = 7, TopSeamBackground = new(Color.Lime), PageDividerShadowOpacity = 0 } };
            ribbon.Theme = custom; Application.DoEvents(); Require(pane.Controls.Cast<Control>().Any(c => c.AccessibleName == "Back" && c.Visible), "Independent Back visibility override");
            ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; ribbon.BuiltInTheme = null;
            Require(ReferenceEquals(custom, ribbon.Theme), "Application Reset retains authored Backstage roles");
            CaptureForm("overrides"); Retained();
            ribbon.IsBackstageOpen = false;
            File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: " + name + "; 12 layout/density/QAT scopes, measured below-tab anchor, exposed tab/File/QAT paint/input, validation cancellation, native editor handles/parents/text/RTF/selection, live accent seam and independent Back/appearance/Application Reset.\nLIMIT: direct paint/offscreen native evidence; no actual VS, physical input/IME, continuous visible motion, desktop materials, native corners or mixed-monitor acceptance.\n");
        }
        finally { ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsBackstageOpen = false; ribbon.Renderer = previousRenderer; probe.Dispose(); }
        _probe = null;
        void Retained()
        {
            foreach (var s in state) Require(s.Control.Handle == s.Handle && ReferenceEquals(s.Control.Parent, s.Parent) && s.Control.Text == s.Text && s.Control.SelectionStart == s.SelectionStart && s.Control.SelectionLength == s.SelectionLength && (s.Rtf is null || ((RichTextBox)s.Control).Rtf == s.Rtf), "Native content ownership retained");
        }
        void CaptureForm(string suffix)
        {
            Application.DoEvents(); using var image = new Bitmap(form.Width, form.Height); form.DrawToBitmap(image, new(Point.Empty, image.Size));
            // DrawToBitmap prints overlapping children in a different order from
            // their HWND z-order. Compose the live front pane last explicitly.
            using var front = new Bitmap(pane.Width, pane.Height); pane.DrawToBitmap(front, new(Point.Empty, front.Size));
            using var graphics = Graphics.FromImage(image); var location = pane.PointToScreen(Point.Empty); location.Offset(-form.Left, -form.Top); graphics.DrawImageUnscaled(front, location);
            image.Save(Path.Combine(directory, name + "-" + suffix + ".png"));
        }
    }
    private static void CaptureRibbon(RibbonControl ribbon)
    {
        _probe!.Clear(); Application.DoEvents(); using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
    }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal readonly Dictionary<RibbonTab, Rectangle> Tabs = new(); internal readonly List<Rectangle> Qat = new(); internal Rectangle File; internal int SelectedTabs; internal int HeaderBottom;
        internal void Clear() { Tabs.Clear(); Qat.Clear(); SelectedTabs = 0; HeaderBottom = 0; }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { Tabs[tab] = c.Bounds; HeaderBottom = Math.Max(HeaderBottom, c.Bounds.Bottom); if (selected) SelectedTabs++; base.DrawTab(c, tab, selected); }
        public override void DrawFileButton(RibbonPaintContext c, RibbonDropDownItem menu) { File = c.Bounds; HeaderBottom = Math.Max(HeaderBottom, c.Bounds.Bottom); base.DrawFileButton(c, menu); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { Qat.Add(c.Bounds); base.DrawQuickAccessItem(c, entry, menu); }
    }
    private static Control AncestorUnder(Control content, Control parent) { var c = content; while (c.Parent is { } next && !ReferenceEquals(next, parent)) c = next; return c; }
    private static IEnumerable<Control> Descendants(Control root) { yield return root; foreach (Control child in root.Controls) foreach (var item in Descendants(child)) yield return item; }
    private static void Click(Control control, Rectangle bounds) { int x = bounds.Left + bounds.Width / 2, y = bounds.Top + bounds.Height / 2; nint point = (nint)((y << 16) | (x & 65535)); SendMessage(control.Handle, 0x201, 1, point); SendMessage(control.Handle, 0x202, 0, point); Application.DoEvents(); }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
#endif
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
