using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DisabledInteractionTests
{
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Disabled_row_never_receives_interaction_state_capture_focus_or_execution(bool inPopup) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900, Height = 600 };
        var ribbon = new RibbonControl();
        var tab = new RibbonTab();
        var group = new RibbonGroup();
        var first = new RibbonButtonItem { Text = "Enabled", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var disabled = new RibbonButtonItem { Text = "Unavailable", Enabled = false, SizePolicy = first.SizePolicy };
        int clicks = 0;
        disabled.Click += (_, _) => clicks++;
        group.Items.AddRange(first, disabled);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon);
        form.Show();
        Application.DoEvents();
        using var popup = inPopup ? new RibbonPopupSurface(ribbon, group, new(1200, 800)) : null;
        if (popup is not null)
        {
            form.Controls.Add(popup);
            popup.FocusElement(first);
        }
        else
            ribbon.FocusElement(first);
        using var renderer = new InteractionRecorder(disabled);
        ribbon.Renderer = renderer;
        Control surface = popup ?? (Control)ribbon;
        var target = (popup?.Targets ?? ribbon.Snapshot.Targets).Single(t => ReferenceEquals(t.Element, disabled));
        int x = target.Bounds.Left + target.Bounds.Width / 2, y = target.Bounds.Top + target.Bounds.Height / 2;
        nint position = (nint)((y << 16) | (x & 0xffff));
        SendMessage(surface.Handle, 0x200, 0, position);
        surface.Refresh();
        Assert.False(renderer.Last.Hot);
        SendMessage(surface.Handle, 0x201, 1, position);
        surface.Refresh();
        Assert.False(renderer.Last.Pressed);
        Assert.False(renderer.Last.Focused);
        Assert.False(surface.Capture);
        Assert.Same(first, popup?.FocusedElement ?? ribbon.FocusedElement);
        SendMessage(surface.Handle, 0x202, 0, position);
        Assert.Equal(0, clicks);
    });

    [Fact]
    public void Default_renderer_ignores_interaction_flags_on_disabled_commands() => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonRenderer();
        using var item = new RibbonButtonItem { Text = "Unavailable", Enabled = false };
        using var normal = new Bitmap(180, 35);
        using var interaction = new Bitmap(180, 35);
        using var normalGraphics = Graphics.FromImage(normal);
        using var interactionGraphics = Graphics.FromImage(interaction);
        normalGraphics.Clear(Color.White);
        interactionGraphics.Clear(Color.White);
        renderer.DrawItem(new(normalGraphics, new(0, 0, 180, 35), RibbonThemes.Office2024Light, SystemFonts.DefaultFont, 96), item, RibbonItemSize.Medium, true);
        renderer.DrawItem(new(interactionGraphics, new(0, 0, 180, 35), RibbonThemes.Office2024Light, SystemFonts.DefaultFont, 96, Hot: true, Pressed: true, Focused: true), item, RibbonItemSize.Medium, true);
        for (int y = 0; y < normal.Height; y++)
        for (int x = 0; x < normal.Width; x++)
            Assert.Equal(normal.GetPixel(x, y), interaction.GetPixel(x, y));
    });

    private sealed class InteractionRecorder(RibbonItem item) : RibbonRenderer
    {
        internal RibbonPaintContext Last;
        public override void DrawItem(RibbonPaintContext context, RibbonItem current, RibbonItemSize size, bool menu)
        {
            if (ReferenceEquals(item, current))
                Last = context;
            base.DrawItem(context, current, size, menu);
        }
    }
}
