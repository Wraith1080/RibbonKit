using System.ComponentModel;
using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class WindowBackdropTests
{
    [Theory] [InlineData(RibbonWindowBackdrop.None)] [InlineData(RibbonWindowBackdrop.Acrylic)]
    public void Caption_pointer_tracking_does_not_invalidate_document_or_repeat_unchanged_hover(RibbonWindowBackdrop backdrop) => FoundationTests.Sta(() =>
    {
        using var form = new CaptionProbe { Width = 900, Height = 550, Backdrop = backdrop }; using var ribbon = new RibbonControl();
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        List<Rectangle> host = [], header = []; form.Invalidated += (_, e) => host.Add(e.InvalidRect); ribbon.Invalidated += (_, e) => header.Add(e.InvalidRect);
        form.CaptionMove(2); form.CaptionMove(2); Assert.Empty(host); Assert.Empty(header);
        form.CaptionMove(8); form.CaptionMove(8);
        Assert.Single(host); Assert.Single(header);
        Assert.All(host, bounds => Assert.True(bounds.Bottom <= form.CaptionHeight, "Caption hover invalidated the document area."));
        Assert.All(header, bounds => Assert.True(bounds.Bottom <= ribbon.WindowCaptionHeight, "Caption hover invalidated command/document geometry."));
    });
    private sealed class CaptionProbe : RibbonForm
    { internal void CaptionMove(int hit) { var message = Message.Create(Handle, 0xA0, hit, 0); WndProc(ref message); } }
    [Theory]
    [InlineData(0, true)] [InlineData(1, false)] [InlineData(2, false)] [InlineData(4, false)]
    [InlineData(8, false)] [InlineData(16, false)] [InlineData(32, false)] [InlineData(64, false)]
    public void Unsafe_backdrop_policy_uses_solid_fallback(int reason, bool expected) => Assert.Equal(expected,
        RibbonForm.CanUseBackdrop(reason != 1, reason == 2, reason != 4, reason != 8, reason == 16 ? .9 : 1, reason == 32 ? Color.Magenta : Color.Empty, reason == 64));

    [Fact]
    public void Preference_does_not_create_a_handle_and_effective_value_is_not_serialized() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Backdrop = RibbonWindowBackdrop.Mica };
        Assert.False(form.IsHandleCreated); Assert.Equal(RibbonWindowBackdrop.None, form.EffectiveBackdrop);
        Assert.Throws<ArgumentOutOfRangeException>(() => form.Backdrop = (RibbonWindowBackdrop)99);
        Assert.Equal(RibbonWindowBackdrop.None, ((DefaultValueAttribute)TypeDescriptor.GetProperties(form)[nameof(form.Backdrop)]!.Attributes[typeof(DefaultValueAttribute)]!).Value);
        Assert.Equal(DesignerSerializationVisibility.Hidden, ((DesignerSerializationVisibilityAttribute)TypeDescriptor.GetProperties(form)[nameof(form.EffectiveBackdrop)]!.Attributes[typeof(DesignerSerializationVisibilityAttribute)]!).Visibility);
    });

    [Fact]
    public void Material_header_has_transparent_background_and_upright_opaque_black_glyphs() => FoundationTests.Sta(() =>
    {
        using var output = new Bitmap(160, 110); using var graphics = Graphics.FromImage(output); using var buffer = new RibbonBackdropBuffer();
        using var font = new Font("Segoe UI", 24, FontStyle.Regular, GraphicsUnit.Pixel);
        buffer.Paint(graphics, output.Size, 70, Color.White, canvas => RibbonBackdropBuffer.DrawText(
            new(canvas, new(10, 6, 100, 50), RibbonThemes.Office2024Light, font, 96), "F", Color.Black, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine));
        Assert.Equal(0, buffer.Image.GetPixel(140, 5).A); Assert.Equal(Color.White.ToArgb(), buffer.Image.GetPixel(140, 90).ToArgb());
        var ink = new List<Point>();
        for (int y = 0; y < 60; y++) for (int x = 0; x < 100; x++) if (buffer.Image.GetPixel(x, y).A > 200) ink.Add(new(x, y));
        Assert.True(ink.Count > 30);
        int top = ink.Min(p => p.Y), bottom = ink.Max(p => p.Y), right = ink.Max(p => p.X);
        Assert.True(ink.Count(p => p.Y < top + 4 && p.X >= right - 4) > ink.Count(p => p.Y > bottom - 4 && p.X >= right - 4), "The top bar of F must remain at the top of the top-down DIB.");
        Assert.All(ink, p => Assert.Equal(0, buffer.Image.GetPixel(p.X, p.Y).R));
    });

    [Fact]
    public void Material_motion_keeps_native_header_live_and_does_not_layer_the_ribbon() => FoundationTests.Sta(() =>
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) || SystemInformation.HighContrast) return;
        using var form = new RibbonForm { Width = 900, Height = 550, Backdrop = RibbonWindowBackdrop.Mica };
        using var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.Expressive };
        ribbon.Tabs.AddRange(new RibbonTab { Text = "Home" }, new RibbonTab { Text = "View" });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        Assert.Equal(RibbonWindowBackdrop.Mica, form.EffectiveBackdrop);
        var authored = ribbon.Theme; var bounds = ribbon.Snapshot.Targets.Select(t => t.Bounds).ToArray();
        foreach (var value in new[] { RibbonWindowBackdrop.Acrylic, RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            form.Backdrop = value; Application.DoEvents();
            Assert.Same(authored, ribbon.Theme); Assert.Equal(bounds, ribbon.Snapshot.Targets.Select(t => t.Bounds).ToArray());
            Assert.Equal(0, DwmGetWindowAttribute(form.Handle, 38, out int native, sizeof(int)));
            Assert.Equal(value == RibbonWindowBackdrop.Mica ? 2 : value == RibbonWindowBackdrop.Acrylic ? 3 : 1, native);
        }
        ribbon.Motion.SystemPolicyOverride = true;
        ribbon.SelectedTab = ribbon.Tabs[1]; Assert.True(ribbon.IsAnimating);
        Assert.False(RibbonDrawingHold.HasOpacity(ribbon.Handle));
        using (var frame = ribbon.Motion.CaptureFrame()) Assert.Equal(0, frame!.GetPixel(20, 10).A);
        ribbon.SettleMotion(); form.Backdrop = RibbonWindowBackdrop.None;
        ribbon.SelectedTab = ribbon.Tabs[0]; Assert.True(ribbon.IsAnimating); Assert.True(RibbonDrawingHold.HasOpacity(ribbon.Handle));
        ribbon.SettleMotion();
    });
    [Fact]
    public void Standalone_material_caption_preserves_application_client_paint() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 500, Height = 300, Backdrop = RibbonWindowBackdrop.Mica };
        form.Paint += (_, e) => e.Graphics.FillRectangle(Brushes.Lime, 30, form.CaptionHeight + 20, 30, 30);
        form.Show(); Application.DoEvents(); using var image = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
        form.DrawToBitmap(image, form.ClientRectangle);
        Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(40, form.CaptionHeight + 30).ToArgb());
    });

    [Fact]
    public void Full_window_material_exposes_ribbon_background_but_keeps_main_body_opaque() => FoundationTests.Sta(() =>
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) || SystemInformation.HighContrast) return;
        using var form = new RibbonForm { Width = 900, Height = 550, Backdrop = RibbonWindowBackdrop.Acrylic };
        using var ribbon = new RibbonControl(); ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        using var scene = new RibbonMotionScene(ribbon); var body = ribbon.Snapshot.BodyFrameBounds;
        Assert.Equal(Color.White.ToArgb(), scene.Image.GetPixel(body.Right - 30, body.Top + 30).ToArgb());
        Assert.True(scene.Image.GetPixel(1, body.Top + body.Height / 2).A < 100, "The surrounding ribbon background must expose the native material.");
        Assert.Equal(Color.White, ribbon.Theme.Background); Assert.Equal(Color.White, ribbon.Theme.Surface);
    });

    [Fact]
    public void Enabling_material_on_an_open_backstage_keeps_opaque_gdi_composition_owned_by_the_surface() => FoundationTests.Sta(() =>
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) || SystemInformation.HighContrast) return;
        using var form = new RibbonForm { Width = 900, Height = 550 }; using var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage }; ribbon.FileMenu.Visible = true;
        var page = new Panel(); page.Controls.Add(new TextBox { Text = "Retained native page", Width = 300 });
        ribbon.Backstage.Visible = true; ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); ribbon.IsBackstageOpen = true; Application.DoEvents();
        Assert.True(ribbon.IsBackstageOpen); var surface = ribbon.BackstageSurface!; nint child = page.Controls[0].Handle;
        form.Backdrop = RibbonWindowBackdrop.Mica; Application.DoEvents();
        Assert.Equal(RibbonWindowBackdrop.Mica, form.EffectiveBackdrop);
        Assert.True(RibbonDrawingHold.HasOpacity(surface.Handle)); Assert.Equal(child, page.Controls[0].Handle);
        Assert.Equal("Retained native page", page.Controls[0].Text);
        ribbon.IsBackstageOpen = false; form.Backdrop = RibbonWindowBackdrop.None;
    });

    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);

    [Fact]
    public void Composition_restart_reapplies_native_setting_even_when_preference_is_unchanged() => FoundationTests.Sta(() =>
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) || SystemInformation.HighContrast) return;
        using var form = new RibbonForm { Width = 500, Height = 300, Backdrop = RibbonWindowBackdrop.Mica }; form.Show(); Application.DoEvents();
        int none = 1; Assert.Equal(0, DwmSetWindowAttribute(form.Handle, 38, ref none, sizeof(int)));
        SendMessage(form.Handle, 0x31E, 0, 0);
        Assert.Equal(0, DwmGetWindowAttribute(form.Handle, 38, out int applied, sizeof(int))); Assert.Equal(2, applied);
        Assert.Equal(RibbonWindowBackdrop.Mica, form.Backdrop); Assert.Equal(RibbonWindowBackdrop.Mica, form.EffectiveBackdrop);
    });

    [Fact]
    public void Taller_material_header_is_not_copied_into_the_body_during_density_motion() => FoundationTests.Sta(() =>
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) || SystemInformation.HighContrast) return;
        using var form = new RibbonForm { Width = 1000, Height = 600, Backdrop = RibbonWindowBackdrop.Mica };
        using var ribbon = new RibbonControl { Density = RibbonDensity.Touch };
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        using var from = new RibbonMotionScene(ribbon); ribbon.Density = RibbonDensity.Compact; using var to = new RibbonMotionScene(ribbon);
        Assert.True(from.Header.Bottom > to.Header.Bottom);
        var bounds = Rectangle.Union(from.Bounds, to.Bounds);
        using var image = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(image); using var alpha = new System.Drawing.Imaging.ImageAttributes();
        RibbonMotionScene.Render(graphics, bounds, ribbon, from, to, 0, alpha);
        for (int y = to.Header.Bottom + 1; y < from.Header.Bottom; y++)
        {
            var pixel = image.GetPixel(image.Width - 150, y);
            Assert.True(pixel.A < 100 || pixel.R > 80 && pixel.G > 80 && pixel.B > 80, $"Captured material header leaked opaque black into body row {y}: {pixel}.");
        }
    });
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wparam, nint lparam);

    [Theory]
    [InlineData(RibbonWindowBackdrop.Mica, RibbonDensity.Compact)] [InlineData(RibbonWindowBackdrop.Mica, RibbonDensity.Touch)]
    [InlineData(RibbonWindowBackdrop.Acrylic, RibbonDensity.Compact)] [InlineData(RibbonWindowBackdrop.Acrylic, RibbonDensity.Touch)]
    public void Material_width_changes_keep_group_motion_and_native_editor_identity(RibbonWindowBackdrop material, RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 1200, Height = 650, Backdrop = material };
        using var ribbon = new RibbonControl { Density = density, AnimationLevel = RibbonAnimationLevel.Expressive };
        var field = new RibbonComboBoxItem { Text = "Font", IsEditable = true, Value = "Retained" };
        var tab = new RibbonTab { Text = "Home" }; var fields = new RibbonGroup { Text = "Fields" }; fields.Items.AddRange(field, new RibbonButtonItem { Text = "Copy" });
        var other = new RibbonGroup { Text = "Other", CanResize = false }; other.Items.Add(new RibbonButtonItem { Text = "Another command" });
        tab.Groups.AddRange(fields, other); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        ribbon.Motion.SystemPolicyOverride = true; ribbon.GroupMotion.ActiveOwnerOverride = true; nint editor = ribbon.Editors.EditorFor(field)!.Handle;
        form.Width = 250; Assert.True(ribbon.GroupMotion.IsRunning, ribbon.GroupMotion.LastSkipReason); ribbon.GroupMotion.Settle();
        form.Width = 1200; Assert.True(ribbon.GroupMotion.IsRunning, ribbon.GroupMotion.LastSkipReason); ribbon.GroupMotion.Settle();
        Assert.Equal(editor, ribbon.Editors.EditorFor(field)!.Handle); Assert.Equal("Retained", field.Value);
    });
}
