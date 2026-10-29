using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class RendererBoundaryTests
{
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [Fact]
    public void Default_and_custom_renderers_keep_flat_and_office_styles_distinct() => FoundationTests.Sta(() =>
    {
        using var flat = new RibbonRenderer(); using var office = new RibbonOffice2024Renderer();
        using var ribbon = new RibbonControl();
        Assert.IsType<RibbonOffice2024Renderer>(ribbon.Painter);
        ribbon.Renderer = flat;
        Assert.Same(flat, ribbon.Painter);
        ribbon.Renderer = null;
        Assert.IsType<RibbonOffice2024Renderer>(ribbon.Painter);
        using var flatBitmap = new Bitmap(100, 40); using var officeBitmap = new Bitmap(100, 40);
        using var flatGraphics = Graphics.FromImage(flatBitmap); using var officeGraphics = Graphics.FromImage(officeBitmap);
        Assert.Equal(Color.White, ribbon.Theme.Background);
        var theme = RibbonThemes.Office2024Light with { Background = Color.Gainsboro };
        flatGraphics.Clear(theme.Surface); officeGraphics.Clear(theme.Surface);
        using var tab = new RibbonTab { Text = "Home" };
        var context = new RibbonPaintContext(flatGraphics, new(0, 0, 100, 40), theme, SystemFonts.DefaultFont, 96);
        flat.DrawTab(context, tab, true); office.DrawTab(context with { Graphics = officeGraphics }, tab, true);
        Assert.Equal(theme.Surface.ToArgb(), flatBitmap.GetPixel(20, 38).ToArgb());
        Assert.Equal(theme.Accent.ToArgb(), officeBitmap.GetPixel(50, 38).ToArgb());
        officeGraphics.Clear(theme.Background);
        office.DrawTab(context with { Graphics = officeGraphics, Hot = true, Pressed = true, Focused = true }, tab, true);
        Assert.Equal(theme.Background.ToArgb(), officeBitmap.GetPixel(12, 3).ToArgb());
        Assert.Equal(theme.Accent.ToArgb(), officeBitmap.GetPixel(50, 38).ToArgb());
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (string label in new[] { "Home", "Insert", "Longer tab header" })
        foreach (int extra in new[] { 0, 1 })
        foreach (int state in new[] { 0, 1, 2, 3 })
        {
            int S(int n) => (int)Math.Round(n * dpi / 96d);
            using var font = new Font("Segoe UI", 9f * dpi / ribbon.DeviceDpi);
            int labelWidth = TextRenderer.MeasureText(label, font, new(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
            var bounds = new Rectangle(0, 0, Math.Max(S(180), labelWidth + S(28)) + extra, S(32));
            using var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(theme.Background); tab.Text = label;
            office.DrawTab(new(graphics, bounds, theme, font, dpi, Hot: state == 1, Pressed: state == 2, Focused: state == 3), tab, state == 0);
            var ink = Rectangle.Empty;
            for (int y = bounds.Bottom - S(6); y < bounds.Bottom; y++)
            for (int x = 0; x < bounds.Width; x++)
                if (bitmap.GetPixel(x, y).ToArgb() != theme.Background.ToArgb())
                    ink = ink.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(ink, new(x, y, 1, 1));
            Assert.Equal(labelWidth + 2 * S(4), ink.Width);
            Assert.Equal(S(3), ink.Height);
            Assert.Equal((bounds.Width - ink.Width) / 2, ink.Left);
            Assert.Equal(bounds.Bottom - S(1), ink.Bottom);
            Assert.NotEqual(theme.Accent.ToArgb(), bitmap.GetPixel(ink.Left, ink.Top).ToArgb());
            Assert.Equal((state == 0 ? theme.Accent : theme.SecondaryText).ToArgb(), bitmap.GetPixel(ink.Left + ink.Width / 2, ink.Top).ToArgb());
            for (int y = 0; y < ink.Height; y++)
            for (int x = 0; x < ink.Width; x++)
            {
                Assert.Equal(bitmap.GetPixel(ink.Left + x, ink.Top + y), bitmap.GetPixel(ink.Right - 1 - x, ink.Top + y));
                Assert.Equal(bitmap.GetPixel(ink.Left + x, ink.Top + y), bitmap.GetPixel(ink.Left + x, ink.Bottom - 1 - y));
            }
            graphics.Clear(theme.Background); tab.Enabled = false;
            office.DrawTab(new(graphics, bounds, theme, font, dpi, Hot: true, Pressed: true, Focused: true), tab, true);
            Assert.Equal(theme.Background.ToArgb(), bitmap.GetPixel(ink.Left + ink.Width / 2, ink.Top).ToArgb());
            tab.Enabled = true;
        }
        flatGraphics.Clear(theme.Surface); officeGraphics.Clear(theme.Surface);
        using var group = new RibbonGroup();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int extra in new[] { 0, 1 })
        foreach (bool collapsed in new[] { false, true })
        foreach (var painter in new[] { flat, office })
        foreach (int state in new[] { 0, 1, 2 })
        {
            int S(int n) => (int)Math.Round(n * dpi / 96d);
            var bounds = new Rectangle(3, 5, S(120) + extra, S(112) + extra);
            using var bitmap = new Bitmap(bounds.Right + 3, bounds.Bottom + 3);
            using var graphics = Graphics.FromImage(bitmap);
            var separatorTheme = theme with { Border = Color.Magenta };
            graphics.Clear(separatorTheme.Surface);
            var command = collapsed ? Rectangle.FromLTRB(bounds.Left + S(3), bounds.Top + S(6), bounds.Right - S(3), bounds.Bottom - S(8)) : bounds;
            painter.DrawGroup(new(graphics, command, separatorTheme, ribbon.Font, dpi, Hot: state == 1, Pressed: state == 2) { GroupSeparatorBounds = bounds }, group, collapsed);
            for (int y = 0; y < bitmap.Height; y++)
            {
                bool line = ReferenceEquals(painter, office) && y >= bounds.Top + S(12) && y < bounds.Bottom - S(12);
                Assert.Equal((line ? separatorTheme.Border : separatorTheme.Surface).ToArgb(), bitmap.GetPixel(bounds.Right - 1, y).ToArgb());
                Assert.Equal(separatorTheme.Surface.ToArgb(), bitmap.GetPixel(bounds.Right - 2, y).ToArgb());
                Assert.Equal(separatorTheme.Surface.ToArgb(), bitmap.GetPixel(bounds.Right, y).ToArgb());
            }
        }
        using var button = new RibbonButtonItem();
        flat.DrawItem(context with { Hot = true }, button, RibbonItemSize.Medium, false);
        office.DrawItem(context with { Graphics = officeGraphics, Hot = true }, button, RibbonItemSize.Medium, false);
        Assert.Equal(theme.Hover.Start.ToArgb(), flatBitmap.GetPixel(1, 1).ToArgb());
        Assert.Equal(theme.Surface.ToArgb(), officeBitmap.GetPixel(1, 1).ToArgb());
        using var combo = new RibbonComboBoxItem();
        foreach (int dpi in new[] { 120, 192 })
        {
            foreach (var painter in new[] { flat, office })
            {
                var field = new Rectangle(0, 0, 100, 40);
                flatGraphics.Clear(theme.Surface);
                var editor = context with { Bounds = field, DeviceDpi = dpi };
                painter.DrawComboBox(editor, combo, field);
                Assert.Equal(theme.Border.ToArgb(), flatBitmap.GetPixel(50, 1).ToArgb());
                for (int y = 1; y < 7; y++)
                    for (int x = 1; x < 7; x++)
                    {
                        Assert.Equal(flatBitmap.GetPixel(x, y), flatBitmap.GetPixel(99 - x, y));
                        Assert.Equal(flatBitmap.GetPixel(x, y), flatBitmap.GetPixel(x, 39 - y));
                    }
                painter.DrawComboBox(editor with { Hot = true }, combo, field);
                Assert.Equal(theme.Hover.Start.ToArgb(), flatBitmap.GetPixel(85, 8).ToArgb());
                painter.DrawComboBox(editor with { Hot = true, Pressed = true }, combo, field);
                Assert.Equal(theme.Pressed.Start.ToArgb(), flatBitmap.GetPixel(85, 8).ToArgb());
                combo.Enabled = false;
                painter.DrawComboBox(editor with { Hot = true, Pressed = true, Focused = true }, combo, field);
                Assert.Equal(theme.Border.ToArgb(), flatBitmap.GetPixel(50, 1).ToArgb());
                Assert.Equal(theme.Surface.ToArgb(), flatBitmap.GetPixel(85, 8).ToArgb());
                combo.Enabled = true;
            }
        }
    });

    [Fact]
    public void Body_scroll_and_popup_surface_painting_is_owned_by_the_custom_renderer() => FoundationTests.Sta(() =>
    {
        CheckBodyScrollChrome();
        using var renderer = new SurfaceRecorder();
        using var form = new Form { Width = 240, Height = 400 };
        var ribbon = new RibbonControl();
        var first = new RibbonTab { Text = "A very long first tab" };
        for (int i = 0; i < 5; i++)
        {
            var group = new RibbonGroup { Text = "Group " + i, CanResize = false };
            group.Items.Add(new RibbonButtonItem { Text = "Command " + i });
            first.Groups.Add(group);
        }
        ribbon.Tabs.AddRange(first, new RibbonTab { Text = "A very long second tab" }); form.Controls.Add(ribbon);
        form.Show(); Application.DoEvents();
        var geometry = ribbon.Snapshot.Targets.Select(t => (t.Identity, t.Bounds)).ToArray();
        ribbon.Renderer = renderer;
        using var bitmap = new Bitmap(ribbon.Width, ribbon.Height);
        ribbon.DrawToBitmap(bitmap, ribbon.ClientRectangle);
        Assert.True(renderer.BodyCount > 0 && renderer.ScrollCount >= 4 && renderer.BodyScrollCount >= 2 && renderer.TabScrollCount >= 2);
        Assert.False(renderer.ScrollHot || renderer.ScrollPressed);
        var body = ribbon.Snapshot.BodyViewport;
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(body.Left + body.Width / 2, body.Top + body.Height / 2).ToArgb());
        var scroll = ribbon.Snapshot.Targets.Single(t => t.Kind == Layout.TargetKind.BodyNext);
        Assert.Equal(Color.Orange.ToArgb(), bitmap.GetPixel(scroll.Bounds.Left + 2, scroll.Bounds.Top + 2).ToArgb());
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(scroll.Bounds.Left + scroll.Bounds.Width / 2, scroll.Bounds.Top - 1).ToArgb());
        Assert.Equal(geometry, ribbon.Snapshot.Targets.Select(t => (t.Identity, t.Bounds)).ToArray());
        int x = scroll.Bounds.Left + scroll.Bounds.Width / 2, y = scroll.Bounds.Top + scroll.Bounds.Height / 2;
        nint position = (nint)((y << 16) | (x & 0xffff));
        renderer.ScrollBounds = scroll.Bounds;
        SendMessage(ribbon.Handle, 0x200, 0, position);
        ribbon.DrawToBitmap(bitmap, ribbon.ClientRectangle);
        Assert.True(renderer.ScrollHot); Assert.False(renderer.ScrollPressed);
        SendMessage(ribbon.Handle, 0x201, 1, position);
        ribbon.DrawToBitmap(bitmap, ribbon.ClientRectangle);
        Assert.True(renderer.ScrollPressed);
        ribbon.Capture = false;
        using var popup = new RibbonPopupSurface(ribbon, first.Groups[0], new(1000, 800));
        using var popupBitmap = new Bitmap(popup.Width, popup.Height);
        popup.DrawToBitmap(popupBitmap, popup.ClientRectangle);
        Assert.True(renderer.PopupCount > 0);
        Assert.Equal(Color.Cyan.ToArgb(), popupBitmap.GetPixel(0, 0).ToArgb());
    });

    private static void CheckBodyScrollChrome()
    {
        using var flat = new RibbonRenderer(); using var office = new RibbonOffice2024Renderer();
        var theme = RibbonThemes.Office2024Light;
        foreach (var painter in new[] { flat, office })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int extra in new[] { 0, 1 })
        foreach (int state in new[] { 0, 1, 2 })
        foreach (bool backward in new[] { false, true })
        {
            int S(int value) => (int)Math.Round(value * dpi / 96d);
            var bounds = new Rectangle(0, 0, S(22) + extra, S(100) + extra);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(theme.Surface);
            var context = new RibbonPaintContext(graphics, bounds, theme, SystemFonts.DefaultFont, dpi, Hot: state == 1, Pressed: state == 2)
                { IsBodyScrollButton = true };
            painter.DrawScrollButton(context, backward);
            var border = state == 0 ? theme.PopupBorder : theme.Accent;
            var fill = state == 0 ? theme.Surface : state == 1 ? theme.Hover.Start : theme.Pressed.Start;
            Assert.Equal(border.ToArgb(), bitmap.GetPixel(bounds.Width / 2, 1).ToArgb());
            Assert.Equal(fill.ToArgb(), bitmap.GetPixel(bounds.Width / 2, S(12)).ToArgb());
            var ink = Rectangle.Empty;
            for (int y = bounds.Height / 2 - S(10); y < bounds.Height / 2 + S(10); y++)
            for (int x = S(3); x < bounds.Width - S(3); x++)
                if (bitmap.GetPixel(x, y).ToArgb() != fill.ToArgb())
                    ink = ink.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(ink, new(x, y, 1, 1));
            Assert.InRange(ink.Width, S(6) - 1, S(6) + 2);
            Assert.InRange(ink.Height, S(10) - 1, S(10) + 2);
            Assert.InRange(Math.Abs((ink.Left + ink.Right - bounds.Width) / 2d), 0, .75);
            Assert.InRange(Math.Abs((ink.Top + ink.Bottom - bounds.Height) / 2d), 0, .75);
            for (int y = 0; y < S(8); y++)
            for (int x = 0; x < Math.Min(S(8), bounds.Width / 2); x++)
            {
                Assert.Equal(bitmap.GetPixel(x, y), bitmap.GetPixel(bounds.Right - 1 - x, y));
                Assert.Equal(bitmap.GetPixel(x, y), bitmap.GetPixel(x, bounds.Bottom - 1 - y));
                Assert.Equal(bitmap.GetPixel(x, y), bitmap.GetPixel(bounds.Right - 1 - x, bounds.Bottom - 1 - y));
            }
            graphics.Clear(theme.Surface);
            painter.DrawScrollButton(context with { IsBodyScrollButton = false }, false);
            Assert.Equal((painter == office ? fill : theme.Surface).ToArgb(), bitmap.GetPixel(bounds.Width / 2, 1).ToArgb());
        }
    }

    [Fact]
    public void Shared_content_hooks_receive_nonoverlapping_image_text_and_arrow_slots() => FoundationTests.Sta(() =>
    {
        using var renderer = new ContentRecorder(); using var image = new Bitmap(16, 16);
        using var item = new RibbonDropDownItem { Text = "Select", Image = image };
        using var bitmap = new Bitmap(220, 40); using var graphics = Graphics.FromImage(bitmap);
        renderer.DrawItem(new(graphics, new(0, 0, 220, 40), RibbonThemes.Office2024Light, SystemFonts.DefaultFont, 96), item, RibbonItemSize.Medium, false);
        Assert.Equal("Select", renderer.Text);
        Assert.True(renderer.ImageBounds.Width > 0 && renderer.ArrowBounds.Width > 0);
        Assert.True(renderer.ImageBounds.Right <= renderer.TextBounds.Left);
        Assert.True(renderer.TextBounds.Right <= renderer.ArrowBounds.Left);
    });

    private sealed class SurfaceRecorder : RibbonRenderer
    {
        internal int BodyCount, ScrollCount, BodyScrollCount, TabScrollCount, PopupCount;
        internal Rectangle ScrollBounds;
        internal bool ScrollHot, ScrollPressed;
        public override void DrawSurface(RibbonPaintContext context, bool popup) { if (popup) PopupCount++; Fill(context, popup ? Color.Cyan : Color.Magenta); }
        public override void DrawBody(RibbonPaintContext context) { BodyCount++; Fill(context, Color.Lime); }
        public override void DrawScrollButton(RibbonPaintContext context, bool backward)
        {
            ScrollCount++;
            if (context.IsBodyScrollButton) BodyScrollCount++; else TabScrollCount++;
            if (ScrollBounds.IsEmpty || context.Bounds == ScrollBounds) { ScrollHot = context.Hot; ScrollPressed = context.Pressed; }
            Fill(context, Color.Orange);
        }
        public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected) { }
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed) { }
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu) { }
        private static void Fill(RibbonPaintContext context, Color color) { using var brush = new SolidBrush(color); context.Graphics.FillRectangle(brush, context.Bounds); }
    }
    private sealed class ContentRecorder : RibbonRenderer
    {
        internal Rectangle ImageBounds, TextBounds, ArrowBounds;
        internal string Text = "";
        protected override void DrawItemImage(RibbonPaintContext context, RibbonItem item, RibbonItemSize size) => ImageBounds = context.Bounds;
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { Text = text; TextBounds = context.Bounds; }
        public override void DrawDropDownArrow(RibbonPaintContext context, Color color) => ArrowBounds = context.Bounds;
    }
}
