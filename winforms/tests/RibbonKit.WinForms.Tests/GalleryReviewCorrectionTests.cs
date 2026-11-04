using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class GalleryReviewCorrectionTests
{
    [Theory]
    [InlineData("button", RibbonLayoutMode.Classic)] [InlineData("button", RibbonLayoutMode.Simplified)]
    [InlineData("split", RibbonLayoutMode.Classic)] [InlineData("split", RibbonLayoutMode.Simplified)]
    [InlineData("qat", RibbonLayoutMode.Classic)] [InlineData("qat", RibbonLayoutMode.Simplified)]
    public void Pointer_command_cleanup_keeps_its_new_gallery_open(string route, RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1300, Height = 700 };
        using var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None, LayoutMode = mode }; form.Controls.Add(ribbon);
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Styles", CanResize = false }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var gallery = new RibbonGalleryItem { Text = "Styles", IsInRibbon = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        for (int i = 0; i < 15; i++) gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + i });
        var first = (RibbonGalleryTile)gallery.Items[0]; gallery.SelectedItem = first;
        var button = new RibbonButtonItem { Text = "Open gallery", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var split = new RibbonSplitButtonItem { Text = "Open gallery", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        group.Items.AddRange(gallery, route == "split" ? split : button); int calls = 0, closes = 0;
        button.Click += (_, _) => { calls++; gallery.ShowDropDown(); }; split.Click += (_, _) => { calls++; gallery.ShowDropDown(); };
        gallery.DropDownClosed += (_, _) => closes++;
        var qat = new RibbonQuickAccessReference { Source = button }; if (route == "qat") ribbon.QuickAccessItems.Add(qat);
        form.Show(); form.Activate(); Application.DoEvents();
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; Application.DoEvents();
            var element = route == "qat" ? (RibbonElement)qat : route == "split" ? split : button;
            var target = ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, element) && !t.DropDownPart);
            Pointer(ribbon, target.VisibleBounds); Assert.Equal(1, calls); Assert.True(gallery.IsDropDownOpen); Assert.Equal(0, closes);
            // Assert synchronous command cleanup here. Foreground/message-loop
            // retention is the separate package consumer's shown-window check.
            var popup = ribbon.Popups.Top!; Assert.Same(first, gallery.SelectedItem);
            var last = popup.Targets.Last(t => t.Element is RibbonGalleryTile); popup.FocusElement(last.Element!);
            var enter = Message.Create(popup.Handle, 0x100, (nint)Keys.Enter, 0); Assert.True(popup.PreProcessMessage(ref enter));
            Assert.Same(last.Element, gallery.SelectedItem); Assert.False(gallery.IsDropDownOpen); Assert.Equal(1, closes);
            gallery.SelectedItem = first; calls = closes = 0;
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Expand_bar_is_centered_bounded_and_thicker_at_each_density_and_dpi(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        using var font = new Font("Segoe UI", 9);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (bool roomier in new[] { false, true })
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var theme = RibbonThemes.Office2024Light with { GalleryUtilityWidth = roomier ? 40 : null };
            int width = S(new RibbonGalleryMetrics(theme, density).UtilityWidth), height = S(28);
            using var bitmap = new Bitmap(width + S(8), height + S(8)); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Color.White);
            var bounds = new Rectangle(S(4), S(4), width, height);
            painter.DrawGalleryStripButton(new(graphics, bounds, theme, font, dpi) { Density = density }, RibbonGalleryStripButton.Expand, true);
            int barY = bounds.Top + (height - S(18)) / 2 + S(4);
            var ink = new List<Point>();
            for (int y = barY - S(2); y <= barY + S(1); y++)
            for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).ToArgb() != Color.White.ToArgb()) ink.Add(new(x, y));
            Assert.NotEmpty(ink); int left = ink.Min(p => p.X), right = ink.Max(p => p.X), top = ink.Min(p => p.Y), bottom = ink.Max(p => p.Y);
            Assert.InRange(right - left + 1, S(8), S(12)); Assert.True(bottom - top + 1 >= S(2));
            Assert.InRange(Math.Abs((left + right) / 2d - (bounds.Left + (bounds.Width - 1) / 2d)), 0, 1);
            Assert.All(ink, p => Assert.True(bounds.Contains(p)));
            if (Environment.GetEnvironmentVariable("RIBBONKIT_GALLERY_REVIEW_CAPTURE") is { Length: > 0 } output)
            { Directory.CreateDirectory(output); bitmap.Save(Path.Combine(output, $"expand-{office}-{density}-{roomier}-{dpi}.png")); }
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Command_in_an_existing_popup_can_replace_it_with_a_gallery(bool split) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1200, Height = 700 }; using var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; form.Controls.Add(ribbon);
        var gallery = GalleryTests.Attach(ribbon); gallery.IsInRibbon = true;
        gallery.Items.AddRange(new RibbonGalleryTile { Text = "First" }, new RibbonGalleryTile { Text = "Second" });
        var menu = new RibbonDropDownItem { Text = "Actions" }; ((RibbonGroup)gallery.Parent!).Items.Add(menu);
        var button = new RibbonButtonItem { Text = "Open styles" }; var primary = new RibbonSplitButtonItem { Text = "Open styles" };
        menu.Items.Add(split ? primary : button); int calls = 0;
        button.Click += (_, _) => { calls++; gallery.ShowDropDown(); }; primary.Click += (_, _) => { calls++; gallery.ShowDropDown(); };
        form.Show(); Application.DoEvents(); menu.ShowDropDown(); var original = ribbon.Popups.Top!;
        Pointer(original, original.Targets.Single(t => !t.DropDownPart).VisibleBounds);
        Assert.Equal(1, calls); Assert.True(original.IsDisposed); Assert.True(gallery.IsDropDownOpen); Assert.Same(gallery, ribbon.Popups.Top!.GallerySource);
        gallery.CloseDropDown(); Assert.False(ribbon.Popups.HasOpen);
    });

    private static void Pointer(Control control, Rectangle bounds)
    {
        var point = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2); nint position = (nint)(point.Y << 16 | point.X);
        SendMessage(control.Handle, 0x201, 1, position); SendMessage(control.Handle, 0x202, 0, position);
    }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
}
