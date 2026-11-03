using System.Drawing.Imaging;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GalleryScrollbarMotionTests
{
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
}
