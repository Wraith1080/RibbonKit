using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Public-package consumer check; no Showcase resources or runtime internals.
internal static class HeaderPresentationCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        {
            using var painter = new Probe();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1100, 650), ShowInTaskbar = false,
                Backdrop = backdrop, TitleBarColor = Color.FromArgb(43, 87, 154) };
            using var ribbon = new RibbonControl { Renderer = painter, BuiltInTheme = family, AnimationLevel = RibbonAnimationLevel.None,
                QuickAccessPosition = RibbonQuickAccessPosition.TabRow, QuickAccessMaxWidth = 80 };
            ribbon.FileMenu.Visible = true; ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "New" });
            var home = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Native input", CanResize = false };
            var field = new RibbonTextBoxItem { Text = "Name", Value = "Retain native input" }; group.Items.Add(field); home.Groups.Add(group); ribbon.Tabs.Add(home);
            for (int i = 0; i < 4; i++) ribbon.Tabs.Add(new RibbonTab { Text = "Document layout " + i, IsContextual = i == 1, ContextualColor = Color.Purple });
            for (int i = 0; i < 4; i++) ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save " + i, Icon = RibbonStockIcons.Create("save") });
            var sources = ribbon.QuickAccessItems.ToArray(); var tabs = ribbon.Tabs.ToArray();
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
            var editor = Descendants(ribbon).OfType<TextBox>().Single(control => control.Text == field.Value);
            nint handle = editor.Handle; var parent = editor.Parent; editor.Select(2, 4);
            var original = ribbon.Theme; string scope = family + "-" + backdrop;
            foreach (int width in new[] { 1100, 360 })
            {
                form.ClientSize = new(width, 650); Application.DoEvents();
                using (var image = Draw(scope + "-" + width + "-default")) { }
                var viewport = painter.Viewport; var file = painter.File; var quick = painter.Quick; var bounds = painter.Tabs.OrderBy(pair => pair.Key.Text).Select(pair => pair.Value).ToArray();
                if (width == 360)
                {
                    Require(!painter.Next.IsEmpty, "The narrow header must retain its scroller.");
                    var point = new Point(painter.Next.Left + painter.Next.Width / 2, painter.Next.Top + painter.Next.Height / 2);
                    nint location = (nint)((point.Y << 16) | (point.X & 0xffff));
                    SendMessage(ribbon.Handle, 0x201, 1, location); SendMessage(ribbon.Handle, 0x202, 0, location); Application.DoEvents();
                    painter.Tag = true; using (var tagged = Draw(scope + "-" + width + "-clipping"))
                    {
                        int escaped = 0, ink = 0;
                        for (int y = viewport.Top; y < viewport.Bottom; y++) for (int x = 0; x < tagged.Width; x++)
                        {
                            var color = tagged.GetPixel(x, y);
                            if (color.R > 160 && color.B > 160 && color.G < 120) { if (viewport.Contains(x, y)) ink++; else escaped++; }
                        }
                        Require(ink > 20 && escaped == 0, "Scrolled header glyphs must stay inside the File/QAT/scroller viewport.");
                    }
                    painter.Tag = false;
                    Require(viewport == painter.Viewport && file == painter.File && quick == painter.Quick, "A scroll changed fixed header reservations.");
                    bounds = painter.Tabs.OrderBy(pair => pair.Key.Text).Select(pair => pair.Value).ToArray();
                }
                ribbon.Theme = original with { HeaderPresentation = original.HeaderPresentation == RibbonHeaderPresentation.Filled ? RibbonHeaderPresentation.Underline : RibbonHeaderPresentation.Filled,
                    HeaderSelectedBackground = new(Color.LightBlue), HeaderSelectedForeground = Color.DarkMagenta, HeaderFilledCornerRadius = 8,
                    HeaderIndicatorThickness = 6, HeaderIndicatorBottomInset = 4, HeaderIndicatorHoverColor = Color.Teal };
                using (var image = Draw(scope + "-" + width + "-effects")) { }
                Require(viewport == painter.Viewport && file == painter.File && quick == painter.Quick && bounds.SequenceEqual(painter.Tabs.OrderBy(pair => pair.Key.Text).Select(pair => pair.Value)),
                    "Changing only header effects changed measured targets.");
                Require(painter.LastPresentation == ribbon.Theme.HeaderPresentation && painter.TextCalls > 0, "Candidate settings or public renderer text hooks were bypassed.");
                Require(ReferenceEquals(ribbon.SelectedTab, home) && sources.SequenceEqual(ribbon.QuickAccessItems) && tabs.SequenceEqual(ribbon.Tabs), "Header settings changed model ownership/selection.");
                Require(editor.Handle == handle && ReferenceEquals(editor.Parent, parent) && editor.Text == field.Value && editor.SelectionStart == 2 && editor.SelectionLength == 4,
                    "Header settings changed the authored editor handle, parent, value or selection.");
                ribbon.Theme = original;
                ribbon.FileMenu.ShowDropDown(); Require(ribbon.FileMenu.IsDropDownOpen, "The File menu did not open.");
                using (var image = Draw(scope + "-" + width + "-file-open")) { }
                ribbon.FileMenu.CloseDropDown();
            }
            evidence.Add($"PASS {scope}: requested={backdrop}, effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; normal/narrow headers, native scroll clipping, open File, public effects, unchanged targets and retained models/editor state.");
            Bitmap Draw(string name)
            {
                painter.Reset(); var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle);
                image.Save(Path.Combine(output, name + ".png")); return image;
            }
        }
        evidence.Add("Public package APIs only. Offscreen native Forms; actual VS, visible material hardware, physical input and mixed-monitor review remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal readonly Dictionary<RibbonTab, Rectangle> Tabs = [];
        internal Rectangle Viewport, File, Quick, Next;
        internal RibbonHeaderPresentation LastPresentation;
        internal bool Tag, InTab; internal int TextCalls;
        internal void Reset() { Tabs.Clear(); Viewport = File = Quick = Next = Rectangle.Empty; TextCalls = 0; }
        public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected)
        {
            Tabs[tab] = context.Bounds; Viewport = Rectangle.Round(context.Graphics.ClipBounds); LastPresentation = context.Theme.HeaderPresentation;
            InTab = true; try { base.DrawTab(context, tab, selected); } finally { InTab = false; }
        }
        public override void DrawFileButton(RibbonPaintContext context, RibbonDropDownItem menu) { File = context.Bounds; base.DrawFileButton(context, menu); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext context, RibbonQuickAccessPosition position) { Quick = context.Bounds; base.DrawQuickAccessToolbar(context, position); }
        public override void DrawScrollButton(RibbonPaintContext context, bool backward) { if (!backward && context.Bounds.Top == Viewport.Top) Next = context.Bounds; base.DrawScrollButton(context, backward); }
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { TextCalls++; base.DrawText(context, text, InTab && Tag ? Color.Magenta : color, flags); }
    }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint parameter, nint location);
}
