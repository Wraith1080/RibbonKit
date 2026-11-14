using System.Text.Json;
using RibbonKit.WinForms;

namespace IndependentConsumer;

internal static class Office2010ReviewChecks
{
    internal static int Run(string directory)
    {
        Directory.CreateDirectory(directory);
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
            PaintEvidence(directory);
#if RKWF_REVIEW
            using var form = new RibbonForm(); using var ribbon = new RibbonControl();
            var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(new RibbonGroup { Text = "Commands" }); ribbon.Tabs.Add(tab);
            using var notes = new RichTextBox { Text = "Independent native notes", Dock = DockStyle.Fill };
            ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = notes }); ribbon.FileMenu.Visible = true;
            form.Controls.Add(ribbon); form.Ribbon = ribbon; Verify(form, ribbon, Path.Combine(directory, "native"), "independent");
#endif
            return 0;
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(directory, "failure.log"), error.ToString()); return 1; }
    }
    // Deliberately uses only pre-review public APIs so the retained package can
    // reproduce clipping/missing utility outlines without a source checkout.
    private static void PaintEvidence(string directory)
    {
        using var renderer = new RibbonOffice2024Renderer(); using var image = new Bitmap(320, 60); using var g = Graphics.FromImage(image); using var font = new Font("Segoe UI", 18);
        g.Clear(Color.White); var clip = new Rectangle(40, 0, 70, 60); g.SetClip(clip);
        renderer.DrawText(new(g, new(0, 0, 300, 60), RibbonThemes.Office2024Light, font, 96), "MMMMMMMMMMMMMMMM", Color.Magenta, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
        int leaked = 0;
        for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (!clip.Contains(x, y) && image.GetPixel(x, y).G < 200) leaked++;
        image.Save(Path.Combine(directory, "body-text-clip.png"));
        g.ResetClip(); g.Clear(Color.White); var theme = RibbonThemes.Office2010Blue;
        renderer.DrawGalleryStripButton(new(g, new(0, 0, 100, 55), theme, font, 96, Hot: true), RibbonGalleryStripButton.Down, true);
        int border = 0; var ink = theme.CommandAppearance!.StateChrome!.HoverBorderColor;
        for (int y = 0; y < 55; y++) for (int x = 0; x < 100; x++) if (image.GetPixel(x, y).ToArgb() == ink.ToArgb()) border++;
        image.Save(Path.Combine(directory, "gallery-utility.png"));
        File.WriteAllText(Path.Combine(directory, "paint.json"), JsonSerializer.Serialize(new { LeakedTextPixels = leaked, GalleryUtilityBorderPixels = border }, new JsonSerializerOptions { WriteIndented = true }));
#if RKWF_REVIEW
        Require(leaked == 0 && border > 0, "Viewport text clipping and utility state outline");
#endif
    }
#if RKWF_REVIEW
    internal static void Verify(RibbonForm form, RibbonControl ribbon, string directory, string name)
    {
        Directory.CreateDirectory(directory); ribbon.AnimationLevel = RibbonAnimationLevel.None;
        var tile = Tiles().FirstOrDefault(t => t.Text == "Office 2010 Blue");
        if (tile is null) ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2010Blue; else tile.PerformClick();
        ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
        form.StartPosition = FormStartPosition.Manual; form.Bounds = new(-16000, -16000, 1000, 700); form.ShowInTaskbar = false; form.Show(); Application.DoEvents();
        var page = ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().First(p => p.Content is not null && p.Visible);
        ribbon.IsBackstageOpen = true; Application.DoEvents();
        var pane = page.Content!; while (pane.Parent is { } next && !ReferenceEquals(next, form)) pane = next;
        var row = Descendants(pane).Single(c => c is Button && c.AccessibleName == page.Text);
        var before = RowPixel(); var beforeAccent = ribbon.Theme.Accent; var beforeSelection = ReferenceEquals(page, ribbon.Backstage.SelectedPage);
        var editors = Descendants(page.Content!).OfType<TextBoxBase>().Select(c => (Control: c, Handle: c.Handle, Parent: c.Parent, c.Text)).ToArray();
        ribbon.IsBackstageOpen = false;
        var accent = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonGalleryItem>()
            .Where(g => g.Id.Contains("accent", StringComparison.OrdinalIgnoreCase) || g.Text.Contains("Accent", StringComparison.OrdinalIgnoreCase))
            .SelectMany(g => g.Items).OfType<RibbonGalleryTile>().FirstOrDefault(t => t.Text == "Purple");
        string action = "direct theme";
        if (accent is null) ribbon.Theme = ribbon.Theme with { Accent = Color.Purple };
        else
        {
            ribbon.SelectedTab = ribbon.Tabs.Single(t => t.Groups.Any(g => g.Items.Any(i => i is RibbonGalleryItem gallery && gallery.Items.Contains(accent))));
            var source = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonGalleryItem>().Single(g => g.Items.Contains(accent)); int commits = 0;
            EventHandler observer = (_, _) => commits++;
            source.SelectionCommitted += observer;
            accent.PerformClick();
            source.SelectionCommitted -= observer;
            action = $"commits={commits}; selection={source.SelectedItem?.Text}; tile={accent.Id}; enabled={accent.Enabled}/{source.Enabled}; visible={accent.Visible}/{source.Visible}; open={ribbon.IsBackstageOpen}; level={ribbon.AnimationLevel}";
        }
        ribbon.IsBackstageOpen = true; Application.DoEvents();
        File.WriteAllText(Path.Combine(directory, "accent-trace.log"), $"Before {beforeAccent} row={before} selected={beforeSelection}; after {ribbon.Theme.Accent} row={RowPixel()} selected={ReferenceEquals(page, ribbon.Backstage.SelectedPage)} recipe={ribbon.Theme.IdentityAccent is not null}; style={ribbon.Theme.BackstageStyle}; {action}");
        Require(before != RowPixel(), "Actual sample accent gallery changes selected native Backstage row");
        Capture(pane, "accent-backstage"); ribbon.IsBackstageOpen = false;
        foreach (var s in editors) Require(s.Control.Handle == s.Handle && ReferenceEquals(s.Parent, s.Control.Parent) && s.Control.Text == s.Text, "Native page ownership retained");

        var existing = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonGalleryItem>().FirstOrDefault(g => g.DropDownPresenterType is not null || g.DropDownPresenterFactory is not null);
        var gallery = existing ?? new RibbonGalleryItem { Text = "Review presenter" };
        RibbonGroup? fixture = null;
        if (existing is null)
        {
            fixture = new RibbonGroup { Text = "Review fixture" }; fixture.Items.Add(gallery); ribbon.Tabs[0].Groups.Add(fixture);
            gallery.Items.AddRange(Enumerable.Range(0, 9).Select(i => new RibbonGalleryTile { Text = "Item " + i }).ToArray()); gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0];
        }
        var previous = gallery.DropDownPresenterFactory; var previousType = gallery.DropDownPresenterType; Control? view = null; using var probe = new Probe(); var renderer = ribbon.Renderer; ribbon.Renderer = probe;
        gallery.DropDownPresenterType = null;
        gallery.DropDownPresenterFactory = () => view = previous?.Invoke() ?? (previousType is { } type ? (Control)Activator.CreateInstance(type)! : new View());
        try
        {
            foreach (var theme in new[] { RibbonThemes.Office2010Blue, RibbonThemes.Office2013Light, RibbonThemes.Office2019Light, RibbonThemes.Office2024Light })
            {
                ribbon.Theme = theme; ribbon.SelectedTab = ribbon.Tabs.Single(t => t.Groups.Any(g => g.Items.Contains(gallery))); Application.DoEvents();
                gallery.ShowDropDown(); Application.DoEvents(); Require(view is { IsDisposed: false, Parent: not null }, "Real presenter opens");
                var surface = view!.Parent!.Parent!; probe.Frames = probe.Tiles = 0; using var pixels = new Bitmap(surface.Width, surface.Height); surface.DrawToBitmap(pixels, new(Point.Empty, pixels.Size));
                Require(probe.Frames > 0, "Actual native popup uses shared presenter frame hook");
                var ink = theme.EditorBorder.IsEmpty ? theme.Border : theme.EditorBorder;
                Require(pixels.GetPixel(probe.Frame.Left + probe.Frame.Width / 2, probe.Frame.Top).ToArgb() == ink.ToArgb(), "Theme border separates complete card viewport");
                Require(probe.Tiles > 0, "Actual custom cards consume the shared tile-state hook");
                pixels.Save(Path.Combine(directory, name + "-presenter-" + theme.BackstageStyle + ".png")); gallery.CloseDropDown(); Application.DoEvents();
            }
        }
        finally { gallery.CloseDropDown(); gallery.DropDownPresenterFactory = previous; gallery.DropDownPresenterType = previousType; ribbon.Renderer = renderer; if (fixture is not null) { ribbon.Tabs[0].Groups.Remove(fixture); fixture.Dispose(); } }
        File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: " + name + "; actual theme/accent action, native selected-row recolor and editor retention; all four theme popup viewport borders, custom-card shared state paint and native presenter lifecycle.\nLIMIT: offscreen native/direct-paint evidence; actual VS, continuous visible motion, physical input/IME and mixed-monitor review are separate.\n");
        IEnumerable<RibbonGalleryTile> Tiles() => ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonGalleryItem>().SelectMany(g => g.Items).OfType<RibbonGalleryTile>();
        Color RowPixel() { using var image = new Bitmap(row.Width, row.Height); row.DrawToBitmap(image, new(Point.Empty, image.Size)); return image.GetPixel(row.Width / 2, row.Height - 4); }
        void Capture(Control c, string suffix) { using var image = new Bitmap(c.Width, c.Height); c.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(directory, name + "-" + suffix + ".png")); }
    }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal int Frames, Tiles; internal Rectangle Frame;
        public override void DrawPopupContentFrame(RibbonPaintContext c) { Frames++; Frame = c.Bounds; base.DrawPopupContentFrame(c); }
        public override void DrawGalleryTileState(RibbonPaintContext c, RibbonGalleryTile tile, bool selected) { Tiles++; base.DrawGalleryTileState(c, tile, selected); }
    }
    private sealed class View : UserControl, IRibbonGalleryPresenter
    {
        private RibbonGalleryPresenterContext _context = null!;
        public void Synchronize(RibbonGalleryPresenterContext context) { _context = context; Invalidate(); }
        public override Size GetPreferredSize(Size proposedSize) => new(300, 170);
        public Rectangle GetItemBounds(RibbonGalleryTile item) { int i = _context.Gallery.Items.IndexOf(item); return new(i % 2 * 145 + 4, i / 2 * 55 + 4, 140, 50); }
        public RibbonGalleryTile? HitTest(Point p) => _context.Gallery.Items.OfType<RibbonGalleryTile>().FirstOrDefault(t => GetItemBounds(t).Contains(p));
        public RibbonGalleryTile? Navigate(RibbonGalleryTile? current, Keys key) => _context.Gallery.Items.OfType<RibbonGalleryTile>().FirstOrDefault();
        public void Reveal(RibbonGalleryTile item) { } public bool CanScrollBackward => false; public bool CanScrollForward => true; public void ScrollView(RibbonGalleryScroll amount) { }
        protected override void OnPaint(PaintEventArgs e) { base.OnPaint(e); foreach (var item in _context.Gallery.Items.OfType<RibbonGalleryTile>()) _context.Renderer.DrawGalleryTileState(new(e.Graphics, GetItemBounds(item), _context.Theme, Font, DeviceDpi), item, ReferenceEquals(item, _context.Gallery.SelectedItem)); }
    }
    private static IEnumerable<Control> Descendants(Control c) { foreach (Control child in c.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
#endif
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
