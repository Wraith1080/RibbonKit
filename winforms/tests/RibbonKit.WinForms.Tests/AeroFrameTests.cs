using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class AeroFrameTests
{
    [Theory]
    [InlineData(96)]
    [InlineData(120)]
    [InlineData(144)]
    [InlineData(192)]
    public void Split_frame_paint_matches_one_gradient_at_each_dpi(int dpi) => FoundationTests.Sta(() =>
    {
        int Scale(int value) => RibbonLayoutEngine.Scale(value, dpi);
        using var form = new RibbonForm();
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue };
        form.Controls.Add(ribbon);
        form.Ribbon = ribbon;
        SendMessage(form.Handle, 0x86, 1, 0);
        var band = new Rectangle(0, 0, Scale(320), Scale(36) + Scale(ribbon.Theme.TabHeight));
        using var whole = new Bitmap(band.Width, band.Height);
        using var parts = new Bitmap(band.Width, band.Height);
        using var wholeGraphics = Graphics.FromImage(whole);
        using var partGraphics = Graphics.FromImage(parts);
        var frame = ribbon.Theme.AeroFrameAppearance!;
        var context = new RibbonPaintContext(wholeGraphics, band, ribbon.Theme, ribbon.Font, dpi);
        form.PaintAeroFrameBand(context, band, frame);
        int join = Scale(36);
        form.PaintAeroFrameBand(context with { Graphics = partGraphics, Bounds = band with { Height = join } }, band, frame);
        form.PaintAeroFrameBand(context with { Graphics = partGraphics, Bounds = Rectangle.FromLTRB(0, join, band.Right, band.Bottom) }, band, frame);
        for (int y = 0; y < band.Height; y++)
            Assert.Equal(whole.GetPixel(band.Width / 2, y), parts.GetPixel(band.Width / 2, y));
    });

    [Fact]
    public void Frame_preferences_reset_and_invalid_palettes_are_atomic() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm();
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue };
        form.Controls.Add(ribbon);
        form.Ribbon = ribbon;
        var properties = TypeDescriptor.GetProperties(form);
        var appearance = properties[nameof(form.FrameAppearance)]!;
        var tint = properties[nameof(form.AeroFrameTint)]!;
        var intensity = properties[nameof(form.AeroFrameTintIntensity)]!;
        Assert.False(appearance.ShouldSerializeValue(form));
        Assert.False(tint.ShouldSerializeValue(form));
        Assert.False(intensity.ShouldSerializeValue(form));
        int events = 0;
        form.FrameAppearanceChanged += (_, _) => events++;
        form.FrameAppearance = RibbonWindowFrameAppearance.Office2010Aero;
        form.AeroFrameTint = Color.MediumPurple;
        form.AeroFrameTintIntensity = .45f;
        Assert.True(appearance.ShouldSerializeValue(form));
        Assert.True(tint.ShouldSerializeValue(form));
        Assert.True(intensity.ShouldSerializeValue(form));

        Assert.Throws<ArgumentOutOfRangeException>(() => form.FrameAppearance = (RibbonWindowFrameAppearance)10);
        Assert.Throws<ArgumentOutOfRangeException>(() => form.AeroFrameTintIntensity = float.NaN);
        Assert.Throws<ArgumentOutOfRangeException>(() => form.AeroFrameTintIntensity = 1.1f);
        Assert.Throws<ArgumentException>(() => form.AeroFrameTint = Color.Transparent);
        Assert.Equal(.45f, form.AeroFrameTintIntensity);
        Assert.Equal(Color.MediumPurple, form.AeroFrameTint);
        var accepted = ribbon.Theme;
        Assert.Throws<ArgumentException>(() => ribbon.Theme = accepted with
            { AeroFrameAppearance = accepted.AeroFrameAppearance! with { ActiveBackground = new(Color.Transparent) } });
        Assert.Same(accepted, ribbon.Theme);

        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        Assert.Equal(RibbonWindowFrameAppearance.Standard, form.EffectiveFrameAppearance);
        Assert.Equal(RibbonWindowFrameAppearance.Office2010Aero, form.FrameAppearance);
        appearance.ResetValue(form);
        tint.ResetValue(form);
        intensity.ResetValue(form);
        Assert.Equal(RibbonWindowFrameAppearance.Standard, form.FrameAppearance);
        Assert.Equal(Color.Empty, form.AeroFrameTint);
        Assert.Equal(.16f, form.AeroFrameTintIntensity);
        Assert.Equal(2, events);
    });

    [Theory]
    [InlineData(RibbonDensity.Compact, RibbonQuickAccessPosition.TitleBar)]
    [InlineData(RibbonDensity.Touch, RibbonQuickAccessPosition.TabRow)]
    [InlineData(RibbonDensity.Compact, RibbonQuickAccessPosition.BelowRibbon)]
    public void Opaque_frame_is_one_band_and_retains_window_and_backstage_owners(RibbonDensity density, RibbonQuickAccessPosition qat) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 920, 680) };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue,
            Density = density, QuickAccessPosition = qat, FileMenuMode = RibbonFileMenuMode.Backstage };
        using var notes = new TextBox { Text = "Retained", Dock = DockStyle.Top };
        var page = new RibbonBackstagePageItem { Text = "Info", Content = notes };
        using var document = new RibbonBackdropPanel { Dock = DockStyle.Fill, BackColor = Color.White };
        ribbon.FileMenu.Visible = true;
        ribbon.Backstage.Items.Add(page);
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save" });
        form.Controls.Add(document);
        form.Controls.Add(ribbon);
        form.Ribbon = ribbon;
        form.Show(); Application.DoEvents();
        nint hwnd = form.Handle, ribbonHandle = ribbon.Handle;
        var before = form.Caption;
        int maxHit = form.CaptionHitTest(Center(before.Targets.Single(target => target.Hit == 9).Bounds));
        SendMessage(hwnd, 0x86, 1, 0); // WM_NCACTIVATE
        using var standard = Paint(ribbon);
        form.FrameAppearance = RibbonWindowFrameAppearance.Office2010Aero;
        if (SystemInformation.HighContrast) { Assert.Null(form.AeroFrameSettings); return; }
        using var aero = Paint(ribbon);
        int x = aero.Width / 2, join = ribbon.WindowCaptionHeight;
        Color above = aero.GetPixel(x, join - 1), below = aero.GetPixel(x, join);
        Assert.True(Difference(above, below) <= 4, $"Frame joined with a seam at y={join}: {above} / {below}.");
        Assert.NotEqual(aero.GetPixel(x, 5), aero.GetPixel(x, ribbon.BackdropHeaderHeight - 2));
        Assert.Equal(255, above.A);
        Assert.NotEqual(standard.GetPixel(x, join - 1), above);
        Assert.Equal(standard.GetPixel(x, aero.Height - 2), aero.GetPixel(x, aero.Height - 2));
        int inset = RibbonLayoutEngine.Scale(6, form.DeviceDpi);
        var content = new Rectangle(inset, 0, form.ClientSize.Width - 2 * inset, form.ClientSize.Height - inset);
        Assert.Equal(content, form.DisplayRectangle);
        Assert.Equal(content.Left, ribbon.Left);
        Assert.Equal(content.Right, ribbon.Right);
        Assert.Equal(Rectangle.FromLTRB(content.Left, ribbon.Bottom, content.Right, content.Bottom), document.Bounds);
        Assert.Equal(before.Buttons with { X = before.Buttons.X - 2 * inset }, form.Caption.Buttons);
        Assert.Equal(9, maxHit);
        var maxPoint = Center(form.Caption.Targets.Single(target => target.Hit == 9).Bounds);
        maxPoint.Offset(inset, 0);
        Assert.Equal(maxHit, form.CaptionHitTest(maxPoint));
        if (qat == RibbonQuickAccessPosition.TitleBar)
        {
            var quickAccess = Center(ribbon.Snapshot.QuickAccessBounds);
            quickAccess.Offset(inset, 0);
            Assert.Equal(1, form.CaptionHitTest(quickAccess));
        }
        Assert.Equal(10, form.CaptionHitTest(new(1, form.ClientSize.Height / 2)));

        form.AeroFrameTint = Color.Purple;
        form.AeroFrameTintIntensity = 1;
        using (var tinted = Paint(ribbon)) Assert.Equal(Color.Purple.ToArgb(), tinted.GetPixel(x, join).ToArgb());
        using (var framePaint = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
        {
            RibbonMotionScene.PrintClient(form, framePaint);
            Assert.Equal(Color.Purple.ToArgb(), framePaint.GetPixel(inset / 2, form.ClientSize.Height / 2).ToArgb());
            Assert.Equal(Color.Purple.ToArgb(), framePaint.GetPixel(form.ClientSize.Width - 1 - inset / 2, form.ClientSize.Height / 2).ToArgb());
            Assert.Equal(Color.Purple.ToArgb(), framePaint.GetPixel(form.ClientSize.Width / 2, form.ClientSize.Height - 1 - inset / 2).ToArgb());
        }
        SendMessage(hwnd, 0x86, 0, 0);
        using (var inactive = Paint(ribbon)) Assert.NotEqual(Color.Purple.ToArgb(), inactive.GetPixel(x, join).ToArgb());
        SendMessage(hwnd, 0x86, 1, 0);

        ribbon.IsBackstageOpen = true;
        var surface = ribbon.BackstageSurface!;
        Assert.Equal(Rectangle.FromLTRB(content.Left, ribbon.BackdropHeaderHeight, content.Right, content.Bottom), surface.Bounds);
        var origin = surface.Location;
        surface.MoveForMotion(new(origin.X - 12, origin.Y));
        Assert.False(surface.Region!.IsVisible(11, 0));
        Assert.True(surface.Region.IsVisible(12, 0));
        surface.MoveForMotion(origin, clipFrame: false);
        Assert.Null(surface.Region);
        nint editorHandle = notes.Handle;
        var editorParent = notes.Parent;
        ribbon.IsBackstageOpen = false;
        form.FrameAppearance = RibbonWindowFrameAppearance.Standard;
        Assert.Equal(0, ribbon.Left);
        Assert.Equal(form.ClientSize.Height, document.Bottom);
        using var reset = Paint(ribbon);
        Assert.Equal(standard.GetPixel(x, join - 1), reset.GetPixel(x, join - 1));
        Assert.Equal(standard.GetPixel(x, join), reset.GetPixel(x, join));
        Assert.Equal(hwnd, form.Handle);
        Assert.Equal(ribbonHandle, ribbon.Handle);
        Assert.Equal(editorHandle, notes.Handle);
        Assert.Same(editorParent, notes.Parent);
        Assert.Equal("Retained", notes.Text);
    });

    [Theory]
    [InlineData(RibbonWindowBackdrop.Acrylic)]
    [InlineData(RibbonWindowBackdrop.Mica)]
    public void Material_replaces_one_active_band_and_leaves_body_opaque(RibbonWindowBackdrop backdrop) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 900, 680), Backdrop = backdrop,
            FrameAppearance = RibbonWindowFrameAppearance.Office2010Aero, AeroFrameTintIntensity = .25f };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue };
        var tab = new RibbonTab { Text = "Home" };
        tab.Groups.Add(new RibbonGroup { Text = "Commands" });
        ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon); form.Ribbon = ribbon;
        form.Show(); Application.DoEvents();
        SendMessage(form.Handle, 0x86, 1, 0);
        if (form.EffectiveBackdrop == RibbonWindowBackdrop.None)
        {
            using var opaque = Paint(ribbon);
            Assert.Equal(backdrop, form.Backdrop);
            Assert.Equal(255, opaque.GetPixel(opaque.Width / 2, ribbon.WindowCaptionHeight).A);
            return;
        }
        using var material = new Bitmap(ribbon.Width, ribbon.Height, PixelFormat.Format32bppPArgb);
        ribbon.Refresh();
        ribbon.CopyBackdropSnapshot(material);
        int x = material.Width / 2, y = ribbon.WindowCaptionHeight;
        Assert.InRange(material.GetPixel(x, y - 1).A, 63, 65);
        Assert.Equal(material.GetPixel(x, y - 1), material.GetPixel(x, y));
        Assert.Equal(255, material.GetPixel(x, material.Height - 2).A);
        SendMessage(form.Handle, 0x86, 0, 0);
        ribbon.Refresh(); ribbon.CopyBackdropSnapshot(material);
        Assert.Equal(255, material.GetPixel(x, y).A);
        Assert.Equal(backdrop, form.EffectiveBackdrop);
        form.FrameAppearance = RibbonWindowFrameAppearance.Standard;
        Assert.Equal(backdrop, form.Backdrop);
        Assert.Equal(backdrop, form.EffectiveBackdrop);
    });

    private static Bitmap Paint(RibbonControl ribbon)
    {
        var image = new Bitmap(ribbon.Width, ribbon.Height);
        ribbon.DrawToBitmap(image, ribbon.ClientRectangle);
        return image;
    }

    private static Point Center(Rectangle bounds) => new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
    private static int Difference(Color a, Color b) => Math.Max(Math.Abs(a.R - b.R), Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B)));

    [DllImport("user32.dll", EntryPoint = "SendMessageW")]
    private static extern nint SendMessage(nint window, int message, nint parameter, nint flags);
}
