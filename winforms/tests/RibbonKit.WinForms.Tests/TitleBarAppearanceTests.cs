using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class TitleBarAppearanceTests
{
    [Fact]
    public void Designer_color_picker_serializes_resets_and_reports_changes() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm(); var properties = TypeDescriptor.GetProperties(form);
        var color = properties[nameof(form.TitleBarColor)]!; var opacity = properties[nameof(form.TitleBarTintOpacity)]!;
        Assert.IsType<ColorConverter>(color.Converter); Assert.True(color.IsBrowsable);
        Assert.False(color.ShouldSerializeValue(form)); Assert.False(opacity.ShouldSerializeValue(form));
        int colors = 0, opacities = 0; form.TitleBarColorChanged += (_, _) => colors++; form.TitleBarTintOpacityChanged += (_, _) => opacities++;
        color.SetValue(form, Color.Navy); opacity.SetValue(form, .35f);
        Assert.True(color.ShouldSerializeValue(form)); Assert.True(opacity.ShouldSerializeValue(form));
        Assert.Single(Serialize(form, color).OfType<CodeAssignStatement>()); Assert.Single(Serialize(form, opacity).OfType<CodeAssignStatement>());
        form.TitleBarColor = Color.Navy; form.TitleBarTintOpacity = .35f;
        Assert.Equal(1, colors); Assert.Equal(1, opacities);
        color.ResetValue(form); opacity.ResetValue(form);
        Assert.True(form.TitleBarColor.IsEmpty); Assert.Equal(.6f, form.TitleBarTintOpacity);
        Assert.Empty(Serialize(form, color).OfType<CodeAssignStatement>()); Assert.Empty(Serialize(form, opacity).OfType<CodeAssignStatement>());
        Assert.Equal(2, colors); Assert.Equal(2, opacities);
        Assert.Throws<ArgumentException>(() => form.TitleBarColor = Color.Transparent);
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, -.1f, 1.1f })
            Assert.Throws<ArgumentOutOfRangeException>(() => form.TitleBarTintOpacity = invalid);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Color_theme_fallback_contrast_and_material_coverage_are_independent_of_layout(bool material) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm(); var theme = new RibbonTheme { Background = Color.Ivory };
        Assert.Equal(material ? Color.Transparent : Color.Ivory, form.CaptionColor(theme, material));
        Assert.Same(theme, form.CaptionTheme(theme, material));
        theme = theme with { TitleBarBackground = Color.Navy };
        form.TitleBarTintOpacity = 1;
        Assert.Equal(Color.Navy.ToArgb(), form.CaptionColor(theme, material).ToArgb());
        var caption = form.CaptionTheme(theme, material);
        Assert.Equal(Color.White, caption.Foreground); Assert.Equal(Color.Transparent, caption.Background);
        Assert.Same(caption, form.CaptionTheme(theme, material)); Assert.Equal(theme.Surface, Color.White);
        form.TitleBarColor = Color.Gold;
        Assert.Equal(Color.Gold.ToArgb(), form.CaptionColor(theme, material).ToArgb());
        Assert.Equal(Color.Black, form.CaptionTheme(theme, material).Foreground);
        Assert.Equal(Color.Purple, form.CaptionTheme(theme with { TitleBarForeground = Color.Purple }, material).Foreground);
        form.TitleBarTintOpacity = .4f;
        Assert.Equal(material ? 102 : 255, form.CaptionColor(theme, material).A);
        form.TitleBarTintOpacity = 0;
        Assert.Equal(material ? 0 : 255, form.CaptionColor(theme, material).A);
        form.TitleBarColor = Color.Empty;
        Assert.Equal(Color.Navy.ToArgb() & 0xFFFFFF, form.CaptionColor(theme, material).ToArgb() & 0xFFFFFF);
    });

    [Theory] [InlineData(RibbonQuickAccessPosition.TitleBar)] [InlineData(RibbonQuickAccessPosition.TabRow)] [InlineData(RibbonQuickAccessPosition.BelowRibbon)]
    public void Only_titlebar_qat_adopts_caption_contrast_without_recreating_native_fields(RibbonQuickAccessPosition position) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Size = new(1000, 600) }; using var painter = new CaptionRecorder();
        using var ribbon = new RibbonControl { Renderer = painter, QuickAccessPosition = position, AnimationLevel = RibbonAnimationLevel.None };
        var command = new RibbonButtonItem { Text = "Save" }; var combo = new RibbonComboBoxItem(); combo.Items.Add("Retained"); combo.SelectedIndex = 0;
        var group = new RibbonGroup(); group.Items.Add(command); group.Items.Add(combo); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command }); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        form.Show(); Application.DoEvents(); ribbon.SyncEditors();
        var native = ribbon.Editors.EditorFor(combo)!; nint handle = native.Handle; var bounds = ribbon.Snapshot.QuickAccessBounds;
        var theme = ribbon.Theme; form.TitleBarColor = Color.Navy;
        using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
        Assert.Equal(position == RibbonQuickAccessPosition.TitleBar ? Color.White : theme.Foreground, painter.QatTheme!.Foreground);
        Assert.Same(theme, painter.BodyTheme); Assert.Same(theme, ribbon.Theme);
        Assert.Equal(handle, native.Handle); Assert.Equal(0, native.SelectedIndex); Assert.Equal(bounds, ribbon.Snapshot.QuickAccessBounds);
        form.TitleBarColor = Color.Empty;
        ribbon.DrawToBitmap(image, new(Point.Empty, image.Size)); Assert.Same(theme, painter.QatTheme);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Standalone_caption_paints_solid_color_or_partial_alpha_tint(bool material) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { ClientSize = new(800, 300), TitleBarColor = Color.Navy, TitleBarTintOpacity = .4f };
        using var image = new Bitmap(800, form.CaptionHeight); using var graphics = Graphics.FromImage(image);
        graphics.Clear(Color.Transparent); form.PaintCaption(graphics, null, material);
        var pixel = image.GetPixel(350, 2);
        Assert.Equal(material ? 102 : 255, pixel.A); Assert.InRange(pixel.B, Color.Navy.B - 1, Color.Navy.B + 1);
    });

    [Theory] [InlineData(RibbonWindowBackdrop.None)] [InlineData(RibbonWindowBackdrop.Mica)] [InlineData(RibbonWindowBackdrop.Acrylic)]
    public void Backstage_overlay_and_restore_cutout_compose_over_the_colored_caption(RibbonWindowBackdrop backdrop) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(150, 100, 950, 650), Backdrop = backdrop, TitleBarColor = Color.Navy };
        using var page = new Panel(); using var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.FileMenu.Visible = true;
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page }); ribbon.Backstage.NavigationBackgroundOpacity = .65f;
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        ribbon.IsBackstageOpen = true;
        foreach (double progress in new[] { .25, .5, 1 })
        {
            ribbon.BackstageOpeningMotion!.SetProgress(progress);
            using var normal = new Bitmap(ribbon.Width, ribbon.WindowCaptionHeight); using var cutout = new Bitmap(ribbon.Width, ribbon.WindowCaptionHeight);
            using var a = Graphics.FromImage(normal); using var b = Graphics.FromImage(cutout); a.Clear(Color.Transparent); b.Clear(Color.Red);
            b.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            ribbon.PaintCaptionBackground(a, new(Point.Empty, normal.Size), ribbon.HasWindowBackdrop);
            ribbon.PaintCaptionBackground(b, new(Point.Empty, cutout.Size), ribbon.HasWindowBackdrop);
            int edge = ribbon.CaptionOverlayBounds.Right;
            foreach (int x in new[] { Math.Max(0, edge - 1), Math.Min(ribbon.Width - 1, edge + 1) })
                Assert.Equal(normal.GetPixel(x, 2), cutout.GetPixel(x, 2));
            if (edge < ribbon.Width) Assert.Equal(form.CaptionColor(ribbon.Theme, ribbon.HasWindowBackdrop).A, normal.GetPixel(edge, 2).A);
        }
    });

    private static CodeStatement[] Serialize(RibbonForm form, PropertyDescriptor property)
    {
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(form, "window");
        var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonForm), typeof(CodeDomSerializer))!;
        var statements = serializer.SerializeMember(manager, form, property);
        Assert.Empty(serialization.Errors.Cast<object>()); return statements.Cast<CodeStatement>().ToArray();
    }
    private sealed class CaptionRecorder : RibbonOffice2024Renderer
    {
        internal RibbonTheme? QatTheme, BodyTheme;
        public override void DrawQuickAccessToolbar(RibbonPaintContext context, RibbonQuickAccessPosition position) { QatTheme = context.Theme; base.DrawQuickAccessToolbar(context, position); }
        public override void DrawBody(RibbonPaintContext context) { BodyTheme = context.Theme; base.DrawBody(context); }
    }
}
