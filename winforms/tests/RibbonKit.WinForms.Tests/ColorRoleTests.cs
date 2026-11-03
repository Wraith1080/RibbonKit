using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ColorRoleTests
{
    [Theory] [InlineData(false, 96)] [InlineData(true, 96)] [InlineData(false, 144)] [InlineData(true, 144)]
    public void Body_popup_editor_and_menu_roles_are_independent(bool office, int dpi) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        var original = new RibbonTheme { Surface = Color.Ivory, Foreground = Color.DarkSlateGray };
        var custom = original with
        {
            BodyBackground = new(Color.LightBlue, Color.SteelBlue), BodyBorder = Color.Purple,
            PopupBackground = Color.Honeydew, EditorBackground = Color.LemonChiffon,
            EditorForeground = Color.DarkRed, EditorBorder = Color.OrangeRed,
            MenuHeadingBackground = new(Color.MidnightBlue), MenuHeadingForeground = Color.White
        };
        using var image = new Bitmap(240, 100); using var graphics = Graphics.FromImage(image);
        var context = new RibbonPaintContext(graphics, new(0, 0, 240, 100), custom, SystemFonts.MessageBoxFont!, dpi);
        graphics.Clear(Color.White); painter.DrawBody(context);
        Assert.NotEqual(image.GetPixel(100, 15), image.GetPixel(100, 85));
        Assert.True(Count(image, Color.Purple) > 50);
        graphics.Clear(Color.White); painter.DrawPopupFrame(context);
        Assert.Equal(Color.Honeydew.ToArgb(), image.GetPixel(100, 50).ToArgb());
        var combo = new RibbonComboBoxItem { Text = "Label" }; combo.Items.Add("First");
        graphics.Clear(Color.White); painter.DrawComboBox(context, combo, new(65, 12, 165, 65));
        Assert.True(Count(image, Color.LemonChiffon) > 500); Assert.True(Count(image, Color.OrangeRed) > 50);
        Assert.True(Count(image, Color.DarkRed) > 5);
        graphics.Clear(Color.White); painter.DrawComboBoxItem(context, combo, 0, false);
        Assert.Equal(Color.Honeydew.ToArgb(), image.GetPixel(235, 50).ToArgb());
        Assert.True(Count(image, Color.DarkRed) > 5);
        graphics.Clear(Color.White); painter.DrawMenuHeader(context, "Heading");
        Assert.True(Count(image, Color.MidnightBlue) > 1000); Assert.True(Count(image, Color.White) > 5);
        using var baseline = new Bitmap(240, 100); using var before = Graphics.FromImage(baseline);
        before.Clear(Color.White); painter.DrawGalleryHeading(context with { Graphics = before, Theme = original }, "Gallery");
        graphics.Clear(Color.White); painter.DrawGalleryHeading(context, "Gallery");
        EqualPixels(baseline, image);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Unset_roles_retain_legacy_surface_and_heading_paint(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        var theme = new RibbonTheme { Surface = Color.PapayaWhip, Foreground = Color.Navy, Border = Color.Salmon };
        var explicitFallbacks = theme with { BodyBackground = new(theme.Surface), PopupBackground = theme.Surface,
            EditorBackground = theme.Surface, EditorForeground = theme.Foreground, EditorBorder = theme.Border };
        Action<RibbonPaintContext>[] draws = [c => painter.DrawBody(c), c => painter.DrawPopupFrame(c),
            c => painter.DrawComboBox(c, new RibbonComboBoxItem { Text = "Choice" }, new(70, 20, 160, 50)),
            c => painter.DrawMenuHeader(c, "Heading")];
        foreach (var draw in draws)
        {
            using var first = new Bitmap(240, 100); using var second = new Bitmap(240, 100);
            using var a = Graphics.FromImage(first); using var b = Graphics.FromImage(second); a.Clear(Color.White); b.Clear(Color.White);
            var context = new RibbonPaintContext(a, new(0, 0, 240, 100), theme, SystemFonts.MessageBoxFont!, 96);
            draw(context); draw(context with { Graphics = b, Theme = explicitFallbacks }); EqualPixels(first, second);
        }
    });

    [Fact]
    public void Palette_updates_retain_native_fields_selection_and_handles() => FoundationTests.Sta(() =>
    {
        using var host = new Form { Size = new(1100, 600) }; using var ribbon = new RibbonControl();
        var combo = new RibbonComboBoxItem { Text = "Choice" }; combo.Items.Add("First"); combo.Items.Add("Second"); combo.SelectedIndex = 1;
        var text = new RibbonTextBoxItem { Text = "Name", Value = "Retained text" };
        var group = new RibbonGroup(); group.Items.Add(combo); group.Items.Add(text); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        host.Controls.Add(ribbon); _ = host.Handle; _ = ribbon.Handle; host.PerformLayout(); ribbon.SyncEditors();
        var nativeCombo = ribbon.Editors.EditorFor(combo)!; var nativeText = ribbon.Editors.EditorFor(text)!;
        nint comboHandle = nativeCombo.Handle, textHandle = nativeText.Handle;
        var bounds = ribbon.Snapshot.BodyFrameBounds;
        ribbon.Theme = ribbon.Theme with { BodyBackground = new(Color.LightBlue, Color.SteelBlue),
            EditorBackground = Color.Beige, EditorForeground = Color.Maroon, PopupBackground = Color.Honeydew };
        ribbon.SyncEditors();
        Assert.Equal(comboHandle, nativeCombo.Handle); Assert.Equal(textHandle, nativeText.Handle);
        Assert.Equal(1, nativeCombo.SelectedIndex); Assert.Equal("Retained text", nativeText.Text);
        Assert.Equal(Color.Beige, nativeCombo.BackColor); Assert.Equal(Color.Beige, nativeText.BackColor);
        Assert.Equal(Color.Maroon, nativeCombo.ForeColor); Assert.Equal(Color.Maroon, nativeText.ForeColor);
        Assert.Equal(bounds, ribbon.Snapshot.BodyFrameBounds);
        ribbon.Theme = RibbonThemes.Office2024Light; ribbon.SyncEditors();
        Assert.Equal(ribbon.Theme.Surface, nativeCombo.BackColor); Assert.Equal(ribbon.Theme.Foreground, nativeCombo.ForeColor);
        Assert.Equal(comboHandle, nativeCombo.Handle);
    });

    [Fact]
    public void Explicit_native_backgrounds_require_opacity_without_restricting_legacy_surface() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var theme = ribbon.Theme;
        foreach (var invalid in new[] { theme with { PopupBackground = Color.Transparent }, theme with { EditorBackground = Color.Transparent },
            theme with { TitleBarBackground = Color.Transparent }, theme with { BodyBackground = new(Color.White, Color.Transparent) } })
        { Assert.Throws<ArgumentException>(() => ribbon.Theme = invalid); Assert.Same(theme, ribbon.Theme); }
        ribbon.Theme = theme with { Surface = Color.Transparent };
        Assert.Equal(Color.Transparent, ribbon.Theme.Surface);
    });

    private static int Count(Bitmap image, Color color)
    {
        int count = 0; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
            if (image.GetPixel(x, y).ToArgb() == color.ToArgb()) count++;
        return count;
    }
    private static void EqualPixels(Bitmap a, Bitmap b)
    {
        for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++) Assert.Equal(a.GetPixel(x, y), b.GetPixel(x, y));
    }
}
