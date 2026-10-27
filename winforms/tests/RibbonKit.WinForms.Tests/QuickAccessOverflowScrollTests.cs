using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class QuickAccessOverflowScrollTests
{
    [DllImport("user32.dll")] private static extern nint SendMessage(nint handle, uint message, nint wParam, nint lParam);

    [Fact]
    public void Bands_keep_commands_clear_and_scroll_by_row_with_mouse_wheel_and_keyboard() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900, Height = 600 }; using var renderer = new Recorder();
        var ribbon = new RibbonControl { QuickAccessMaxWidth = 28, Renderer = renderer }; form.Controls.Add(ribbon);
        var commands = Enumerable.Range(0, 20).Select(i => new RibbonButtonItem { Text = "Command " + i }).ToArray();
        commands[0].MenuDescription = "A taller first row verifies row-based scrolling.";
        ribbon.QuickAccessItems.AddRange(commands); int calls = 0; foreach (var item in commands) item.Click += (_, _) => calls++;
        using var popup = new RibbonPopupSurface(ribbon, ribbon.QuickAccessOverflow, new(320, 220));
        popup.Location = new(20, ribbon.Bottom + 10); form.Controls.Add(popup); form.Show(); Application.DoEvents();
        Assert.False(popup.AutoScroll); Assert.False(popup.VerticalScroll.Visible); Assert.False(popup.HorizontalScroll.Visible);
        Assert.False(popup.OverflowUpBounds.IsEmpty); Assert.False(popup.OverflowDownBounds.IsEmpty);
        var first = popup.Targets[0]; var second = popup.Targets[1];
        Assert.Equal(popup.OverflowViewport.Top + RibbonLayoutEngine.Scale(4, ribbon.DeviceDpi), first.Bounds.Top);
        Assert.All(popup.Targets, target =>
        {
            Assert.False(target.VisibleBounds.IntersectsWith(popup.OverflowUpBounds));
            Assert.False(target.VisibleBounds.IntersectsWith(popup.OverflowDownBounds));
            Assert.Equal(popup.OverflowViewport, target.Clip);
        });
        using var bitmap = new Bitmap(popup.Width, popup.Height); popup.DrawToBitmap(bitmap, popup.ClientRectangle);
        Assert.False(renderer.UpEnabled); Assert.True(renderer.DownEnabled);
        Click(popup, popup.OverflowDownBounds);
        Assert.Equal(first.Bounds.Top, popup.Targets[1].Bounds.Top); Assert.Equal(second.Bounds.Top - first.Bounds.Top, first.Bounds.Top - popup.Targets[0].Bounds.Top);
        Assert.Equal(0, calls);
        int beforeWheel = popup.Targets[0].Bounds.Top;
        SendMessage(popup.Handle, 0x20A, unchecked((nint)(-120 << 16)), 0);
        Assert.True(popup.Targets[0].Bounds.Top < beforeWheel); Assert.Equal(0, calls);
        popup.FocusElement(commands[^1]); var last = popup.Targets[^1];
        Assert.True(popup.OverflowViewport.Contains(last.Bounds));
        popup.DrawToBitmap(bitmap, popup.ClientRectangle); Assert.True(renderer.UpEnabled); Assert.False(renderer.DownEnabled);
        Assert.False(popup.ScrollOverflow(true));
        popup.FocusElement(commands[0]); Assert.True(popup.OverflowViewport.Contains(popup.Targets[0].Bounds));
        popup.DrawToBitmap(bitmap, popup.ClientRectangle); Assert.False(renderer.UpEnabled); Assert.True(renderer.DownEnabled);
        Assert.False(popup.ScrollOverflow(false));
    });

    [Fact]
    public void Short_overflow_reserves_no_bands_and_clamps_long_captions_to_the_work_area() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { QuickAccessMaxWidth = 28 };
        ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = new string('W', 120) });
        using var popup = new RibbonPopupSurface(ribbon, ribbon.QuickAccessOverflow, new(240, 500)); popup.CreateControl();
        Assert.True(popup.OverflowUpBounds.IsEmpty); Assert.True(popup.OverflowDownBounds.IsEmpty);
        Assert.False(popup.AutoScroll); Assert.False(popup.VerticalScroll.Visible); Assert.False(popup.HorizontalScroll.Visible);
        Assert.Equal(popup.ClientRectangle, popup.OverflowViewport);
        Assert.All(popup.Targets, target => Assert.True(popup.ClientRectangle.Contains(target.Bounds)));
        Assert.True(popup.Width < 240);
    });

    [Fact]
    public void Both_renderers_paint_opaque_bands_in_idle_disabled_hover_and_pressed_states() => FoundationTests.Sta(() =>
    {
        using var office = new RibbonOffice2024Renderer(); using var neutral = new RibbonRenderer(); using var font = new Font("Segoe UI", 9);
        var theme = RibbonThemes.Office2024Light with { Surface = Color.FromArgb(60, Color.White) };
        foreach (var renderer in new RibbonRenderer[] { office, neutral })
        {
            var fills = new List<int>();
            foreach (var state in new[] { (true, false, false), (false, false, false), (true, true, false), (true, false, true) })
            {
                using var bitmap = new Bitmap(80, 20); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(Color.Transparent);
                renderer.DrawOverflowScrollButton(new(graphics, new(0, 0, bitmap.Width, bitmap.Height), theme, font, 96,
                    Hot: state.Item2, Pressed: state.Item3), true, state.Item1);
                for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++) Assert.Equal(255, bitmap.GetPixel(x, y).A);
                fills.Add(bitmap.GetPixel(10, 10).ToArgb());
            }
            Assert.Equal(Color.White.ToArgb(), fills[0]); Assert.Equal(fills[0], fills[1]);
            Assert.Equal(theme.Hover.Start.ToArgb(), fills[2]); Assert.Equal(theme.Pressed.Start.ToArgb(), fills[3]);
            Assert.NotEqual(fills[0], fills[2]); Assert.NotEqual(fills[2], fills[3]);
        }
    });

    [Fact]
    public void Scrollers_pause_for_an_open_child_menu_and_resume_after_it_closes() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900, Height = 600 }; var ribbon = new RibbonControl { QuickAccessMaxWidth = 28 }; form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup(); var source = new RibbonDropDownItem { Text = "Select" };
        source.Items.Add(new RibbonButtonItem { Text = "All" }); group.Items.Add(source); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var reference = new RibbonQuickAccessDropDownReference { Source = source }; ribbon.QuickAccessItems.Add(reference);
        for (int i = 0; i < 70; i++) ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Command " + i });
        form.Show(); Application.DoEvents();
        ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccessOverflow)); var popup = ribbon.Popups.Top!;
        int before = popup.Targets[0].Bounds.Top;
        ribbon.Activate(popup.Targets.Single(t => ReferenceEquals(t.Element, reference)), popup);
        Assert.True(reference.IsDropDownOpen); Assert.False(popup.ScrollOverflow(true)); Assert.Equal(before, popup.Targets[0].Bounds.Top);
        source.CloseDropDown(); Assert.Same(popup, ribbon.Popups.Top); Assert.True(popup.ScrollOverflow(true));
        Assert.True(popup.Targets[0].Bounds.Top < before);
        ribbon.QuickAccessVisible = false; Assert.True(popup.IsDisposed); Assert.False(ribbon.Popups.HasOpen);
    });

    private static void Click(Control control, Rectangle bounds)
    {
        nint point = (nint)(((bounds.Top + bounds.Height / 2) << 16) | ((bounds.Left + bounds.Width / 2) & 0xffff));
        SendMessage(control.Handle, 0x201, 1, point); SendMessage(control.Handle, 0x202, 0, point);
    }
    private sealed class Recorder : RibbonOffice2024Renderer
    {
        internal bool UpEnabled, DownEnabled;
        public override void DrawOverflowScrollButton(RibbonPaintContext context, bool upward, bool enabled)
        {
            if (upward) UpEnabled = enabled; else DownEnabled = enabled;
            base.DrawOverflowScrollButton(context, upward, enabled);
        }
    }
}
