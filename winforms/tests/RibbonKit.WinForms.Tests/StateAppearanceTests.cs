using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class StateAppearanceTests
{
    [Fact]
    public void Disabled_selection_palette_refreshes_native_text_without_recreating_the_editor() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Size = new(1000, 600) };
        using var ribbon = new RibbonControl();
        var choice = new RibbonComboBoxItem { Enabled = false }; choice.Items.Add("Retained selection"); choice.SelectedIndex = 0;
        var group = new RibbonGroup(); group.Items.Add(choice); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon); _ = form.Handle; _ = ribbon.Handle; form.PerformLayout(); ribbon.SyncEditors();
        var editor = ribbon.Editors.EditorFor(choice)!; nint handle = editor.Handle;
        int invalidations = 0; editor.Invalidated += (_, _) => invalidations++;
        ribbon.Theme = ribbon.Theme with { DisabledForeground = Color.Purple }; ribbon.SyncEditors();
        Assert.True(invalidations > 0, "The disabled owner-drawn selection must repaint when its color changes.");
        Assert.Equal(handle, editor.Handle); Assert.Equal(0, editor.SelectedIndex); Assert.False(editor.Enabled);
        invalidations = 0; ribbon.SyncEditors(); ribbon.SyncEditors();
        Assert.Equal(0, invalidations);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Disabled_message_close_ignores_pointer_and_focus_states(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        using var idle = new Bitmap(48, 40); using var stale = new Bitmap(48, 40);
        using var first = Graphics.FromImage(idle); using var second = Graphics.FromImage(stale);
        var theme = new RibbonTheme { DisabledForeground = Color.Purple };
        first.Clear(theme.Surface); second.Clear(theme.Surface);
        renderer.DrawMessageClose(new(first, new(0, 0, 48, 40), theme, SystemFonts.DefaultFont, 96), false);
        renderer.DrawMessageClose(new(second, new(0, 0, 48, 40), theme, SystemFonts.DefaultFont, 96, Hot: true, Pressed: true, Focused: true), false);
        for (int y = 0; y < idle.Height; y++) for (int x = 0; x < idle.Width; x++)
            Assert.Equal(idle.GetPixel(x, y), stale.GetPixel(x, y));
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Material_disabled_ink_uses_the_authored_role_without_active_tints(bool office) => FoundationTests.Sta(() =>
    {
        using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
        using var ribbon = new RibbonControl();
        var icon = new RecordingIcon();
        var command = new RibbonButtonItem { Text = "Disabled", Enabled = false, Icon = icon };
        var group = new RibbonGroup(); group.Items.Add(command); var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var entry = new RibbonQuickAccessReference { Source = command }; ribbon.QuickAccessItems.Add(entry);
        var theme = new RibbonTheme { DisabledForeground = Color.FromArgb(160, 75, 20), QuickAccessBackdropOpacity = .1f };
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (bool backstage in new[] { false, true })
        {
            using var idle = new Bitmap(240, 64); using var stale = new Bitmap(240, 64);
            using var first = Graphics.FromImage(idle); using var second = Graphics.FromImage(stale);
            first.Clear(Color.Transparent); second.Clear(Color.Transparent);
            var context = new RibbonPaintContext(first, new(0, 0, 240, 64), theme, SystemFonts.DefaultFont, dpi)
                { WindowBackdrop = true, CompositedText = true };
            Draw(context);
            Assert.Equal(theme.DisabledForeground, icon.Color); Assert.False(icon.Enabled);
            Draw(context with { Graphics = second, Hot = true, Pressed = true, Focused = true });
            for (int y = 0; y < idle.Height; y++) for (int x = 0; x < idle.Width; x++)
                Assert.Equal(idle.GetPixel(x, y), stale.GetPixel(x, y));

            void Draw(RibbonPaintContext paint)
            {
                if (backstage) renderer.DrawBackstageEntry(paint, command, false);
                else renderer.DrawQuickAccessItem(paint, entry, false);
            }
        }
    });

    private sealed class RecordingIcon : IRibbonIcon
    {
        internal Color Color;
        internal bool Enabled;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        {
            Color = context.Foreground; Enabled = context.Enabled;
            using var fill = new SolidBrush(context.Foreground); graphics.FillRectangle(fill, bounds);
        }
    }
}
