using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GalleryScrollbarMotionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Gallery_scroll_repaints_the_entire_gradient_viewport(bool wheel) => FoundationTests.Sta(() =>
    {
        using var host = new Form { Width = 800, Height = 600 };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue, AnimationLevel = RibbonAnimationLevel.None };
        var gallery = new RibbonGalleryItem { Text = "Styles", ColumnCount = 2, MaxVisibleRows = 2 };
        for (int i = 0; i < 12; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + i });
        var group = new RibbonGroup(); group.Items.Add(gallery);
        var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        host.Controls.Add(ribbon); host.Show();
        gallery.ShowDropDown();
        var surface = ribbon.Popups.Top!;
        Assert.True(surface.VerticalScroll.Visible);
        surface.Refresh();
        var tileBounds = surface.Targets[0].Bounds;
        Rectangle paintClip = Rectangle.Empty;
        surface.Paint += (_, e) => paintClip = e.ClipRectangle;

        SendScroll(down: true);
        Assert.True(surface.AutoScrollPosition.Y < 0);
        Assert.Equal(tileBounds.Y + surface.AutoScrollPosition.Y, surface.Targets[0].Bounds.Y);
        Assert.Equal(surface.ClientRectangle, paintClip);

        paintClip = Rectangle.Empty;
        SendScroll(down: false);
        Assert.Equal(Point.Empty, surface.AutoScrollPosition);
        Assert.Equal(tileBounds, surface.Targets[0].Bounds);
        Assert.Equal(surface.ClientRectangle, paintClip);

        void SendScroll(bool down)
        {
            const int wmMouseWheel = 0x020A, wmVScroll = 0x0115;
            const int lineUp = 0, lineDown = 1;
            if (wheel)
            {
                var screen = surface.PointToScreen(new Point(4, 4));
                SendMessage(surface.Handle, wmMouseWheel, (down ? -20 : 20) << 16, (screen.Y << 16) | (screen.X & 0xffff));
            }
            else SendMessage(surface.Handle, wmVScroll, down ? lineDown : lineUp, 0);
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void First_and_reopened_gallery_entrances_include_native_scrollbar_track_and_thumb(bool office) => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles();
        using var host = new Form { Bounds = new(160, 100, 1000, 700) };
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        using var ribbon = new RibbonControl { Renderer = painter, AnimationLevel = RibbonAnimationLevel.Expressive };
        var gallery = new RibbonGalleryItem { Text = "Styles", ColumnCount = 3, MaxVisibleRows = 2 };
        for (int i = 0; i < 30; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + i, GroupName = i < 6 ? "Document" : "Headings" });
        var group = new RibbonGroup(); group.Items.Add(gallery); var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        host.Controls.Add(ribbon); host.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        for (int opening = 0; opening < 2; opening++)
        {
            gallery.ShowDropDown();
            var surface = ribbon.Popups.Top!; var popup = (RibbonPopupWindow)surface.Parent!; var motion = popup.OpeningMotion!;
            Assert.True(surface.VerticalScroll.Visible); Assert.True(surface.Width > surface.ClientSize.Width); Assert.True(motion.FadesGallery);
            var origin = surface.PointToScreen(new(surface.ClientSize.Width, 0)); origin.Offset(-motion.Bounds.X, -motion.Bounds.Y);
            var bar = new Rectangle(origin, new(surface.Width - surface.ClientSize.Width, surface.ClientSize.Height));
            using var frame = new Bitmap(motion.Bounds.Width, motion.Bounds.Height, PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(frame)) using (var alpha = new ImageAttributes()) motion.Draw(graphics, alpha, 1);
            if (Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_SCROLLBAR_CAPTURE") is { Length: > 0 } directory)
            { Directory.CreateDirectory(directory); frame.Save(Path.Combine(directory, $"{(office ? "office" : "neutral")}-{opening}.png")); }
            int light = 0, darkest = 255, brightest = 0;
            for (int y = bar.Top + 2; y < bar.Bottom - 2; y++) for (int x = bar.Left + 2; x < bar.Right - 2; x++)
            {
                var pixel = frame.GetPixel(x, y); int brightness = (pixel.R + pixel.G + pixel.B) / 3;
                if (brightness > 100) light++; darkest = Math.Min(darkest, brightness); brightest = Math.Max(brightest, brightness);
            }
            Assert.True(light > bar.Width * bar.Height / 2, $"Opening {opening}: the captured native scrollbar must not be a black band.");
            Assert.True(brightest > 230 && darkest < 230, $"Opening {opening}: capture both the native track and thumb, rather than a blank surface fill.");
            gallery.CloseDropDown(); Assert.True(popup.IsDisposed);
        }
    });

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
}
