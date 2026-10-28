using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MessageTests
{
    [Fact]
    public void Messages_keep_owned_identity_bindings_and_idempotent_dismissal() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BindingContext = new BindingContext() }; using var other = new RibbonControl();
        var message = new RibbonMessage { Title = "Notice", Text = "Application-owned text" }; message.Action.Text = "Review";
        var state = new State(); using var source = new BindingSource { DataSource = state };
        ribbon.MessageBar.Items.Add(message);
        ribbon.BindingContext = new BindingContext(); Assert.Same(ribbon.BindingContext, message.BindingContext); Assert.Same(ribbon.BindingContext, message.Action.BindingContext);
        message.DataBindings.Add(new Binding(nameof(message.IsOpen), source, nameof(state.Open), false, DataSourceUpdateMode.OnPropertyChanged));
        int dismissed = 0, changed = 0, clicks = 0;
        message.Dismissed += (_, _) => dismissed++; ribbon.MessageBar.HasOpenMessagesChanged += (_, _) => changed++;
        message.Action.Click += (_, _) => clicks++;
        Assert.Same(ribbon.MessageBar, ribbon.MessageBar); Assert.Same(message, message.Action.Parent);
        Assert.True(ribbon.MessageBar.HasOpenMessages); Assert.Throws<InvalidOperationException>(() => other.MessageBar.Items.Add(message));
        message.Action.PerformClick(); Assert.Equal(1, clicks); Assert.True(message.IsOpen);
        message.Dismiss(); message.Dismiss(); Assert.False(state.Open); Assert.False(ribbon.MessageBar.HasOpenMessages); Assert.Equal(1, dismissed); Assert.Equal(1, changed);
        message.Action.PerformClick(); Assert.Equal(1, clicks);
        state.Open = true; source.ResetBindings(false); Assert.True(message.IsOpen); Assert.True(ribbon.MessageBar.HasOpenMessages); Assert.Equal(2, changed);
        message.IsDismissible = false; Assert.DoesNotContain(ribbon.Snapshot.Targets, t => t.Kind == TargetKind.MessageClose);
        message.Dismiss(); Assert.Equal(2, dismissed); Assert.Single(ribbon.MessageBar.Items);
        var image = new Bitmap(10, 10); message.Image = image; message.Dispose(); Assert.Empty(ribbon.MessageBar.Items); Assert.True(message.Action.IsDisposed);
        Assert.Equal(10, image.Width); image.Dispose();
        Assert.Equal(DesignerSerializationVisibility.Content, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.MessageBar)]!.SerializationVisibility);
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Message_frames_follow_body_and_qat_presence_and_restore_the_exact_closed_geometry(bool office) => FoundationTests.Sta(() =>
    {
        var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Neutral(record);
        using var ribbon = new RibbonControl { Width = 900, Renderer = painter }; var tab = new RibbonTab(); tab.Groups.Add(new RibbonGroup()); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save" });
        var first = new RibbonMessage { Title = "Notice", Text = "Review the document." }; first.Action.Text = "Review";
        var second = new RibbonMessage { Text = "A second message remains independently open." };
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (var placement in new[] { RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon })
        foreach (bool visible in new[] { false, true })
        foreach (bool minimized in new[] { false, true })
        {
            ribbon.Density = density; ribbon.QuickAccessPosition = placement; ribbon.QuickAccessVisible = visible; ribbon.IsMinimized = minimized;
            var baseline = ribbon.Snapshot; int height = ribbon.Height;
            first.IsOpen = second.IsOpen = true; ribbon.MessageBar.Items.AddRange(first, second);
            var layout = ribbon.Snapshot;
            Assert.Equal(baseline.BodyBounds, layout.BodyBounds); Assert.Equal(baseline.QuickAccessBounds, layout.QuickAccessBounds);
            Assert.Equal(2, layout.Messages.Count); Assert.Equal(layout.Messages[0].Bounds.Bottom, layout.Messages[1].Bounds.Top);
            bool connected = office && (!minimized || visible && placement == RibbonQuickAccessPosition.BelowRibbon);
            int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi);
            Assert.Equal(office ? S(ribbon.Theme.BodyMargin.Left) : 0, layout.MessageBounds.Left);
            Assert.Equal(office ? minimized ? visible && placement == RibbonQuickAccessPosition.BelowRibbon ? layout.QuickAccessBounds.Bottom : layout.TabViewport.Bottom + S(ribbon.Theme.BodyMargin.Top) : layout.BodyFrameBounds.Bottom : height,
                layout.MessageBounds.Top);
            using (var bitmap = new Bitmap(ribbon.Width, ribbon.Height)) ribbon.DrawToBitmap(bitmap, ribbon.ClientRectangle);
            Assert.Equal(connected, record.TopConnected); if (office && (!minimized || visible && placement == RibbonQuickAccessPosition.BelowRibbon)) Assert.True(record.BottomConnected);
            ribbon.Focus(); ribbon.FocusElement(first.Close); ribbon.Activate(layout.Targets.Single(t => t.Element == first.Close));
            Assert.False(first.IsOpen); Assert.True(second.IsOpen); Assert.Single(ribbon.Snapshot.Messages);
            Assert.NotSame(first.Close, ribbon.FocusedElement);
            second.Dismiss(); Assert.Equal(height, ribbon.Height); Assert.Equal(baseline.BodyBounds, ribbon.Snapshot.BodyBounds); Assert.Equal(baseline.QuickAccessBounds, ribbon.Snapshot.QuickAccessBounds);
            ribbon.MessageBar.Items.Clear();
        }
    });

    [Fact]
    public void Wrapped_message_text_actions_and_close_targets_remain_disjoint_at_dpi_and_touch_sizes() => FoundationTests.Sta(() =>
    {
        using var probe = new Control();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int points in new[] { 9, 14 })
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (int width in new[] { 160, 400, 900 })
        foreach (string body in new[] { "Short", "Review files from untrusted locations before enabling document editing." })
        foreach (string action in new[] { "X", "Enable editing" })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            using var font = new Font("Segoe UI", points * dpi / (float)probe.DeviceDpi);
            using var ribbon = new RibbonControl { Font = font, Density = density };
            var message = new RibbonMessage { Title = body == "Short" ? "" : "Protected view", Text = body }; message.Action.Text = action;
            ribbon.MessageBar.Items.Add(message);
            var row = Assert.Single(RibbonMessageLayout.Measure(ribbon, S(width), dpi).Rows);
            Assert.True(row.Bounds.Contains(row.Title)); Assert.True(row.Bounds.Contains(row.Text)); Assert.True(row.Bounds.Contains(row.Action)); Assert.True(row.Bounds.Contains(row.Close));
            Assert.False(row.Title.IntersectsWith(row.Text)); Assert.False(row.Action.IntersectsWith(row.Title)); Assert.False(row.Action.IntersectsWith(row.Text)); Assert.False(row.Action.IntersectsWith(row.Close));
            Assert.True(row.Close.Height >= S(density == RibbonDensity.Touch ? 44 : 22));
            if (density == RibbonDensity.Touch) Assert.True(row.Action.Width >= S(44));
        }
    });

    [Fact]
    public void Attached_and_standalone_message_corners_are_symmetric_and_the_qat_join_is_square() => FoundationTests.Sta(() =>
    {
        using var painter = new RibbonOffice2024Renderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int odd in new[] { 0, 1 })
        foreach (bool connected in new[] { false, true })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi); var bounds = new Rectangle(S(10), S(10), S(220) + odd, S(48) + odd);
            var theme = RibbonThemes.Office2024Light with { BodyShadowColor = Color.Transparent };
            using var bitmap = new Bitmap(bounds.Right + S(10), bounds.Bottom + S(10)); using var g = Graphics.FromImage(bitmap); g.Clear(theme.Background);
            painter.DrawMessageBar(new(g, bounds, theme, Control.DefaultFont, dpi) { IsTopEdgeConnected = connected });
            for (int y = 0; y < S(8); y++) for (int x = 0; x < S(8); x++)
            {
                Assert.Equal(bitmap.GetPixel(bounds.Left + x, bounds.Top + y), bitmap.GetPixel(bounds.Right - 1 - x, bounds.Top + y));
                Assert.Equal(bitmap.GetPixel(bounds.Left + x, bounds.Bottom - 1 - y), bitmap.GetPixel(bounds.Right - 1 - x, bounds.Bottom - 1 - y));
                if (!connected) Assert.Equal(bitmap.GetPixel(bounds.Left + x, bounds.Top + y), bitmap.GetPixel(bounds.Left + x, bounds.Bottom - 1 - y));
            }
            if (connected) Assert.Equal(theme.MessageBorder.ToArgb(), bitmap.GetPixel(bounds.Left, bounds.Top).ToArgb());
            g.Clear(theme.Background); painter.DrawBody(new(g, bounds, theme, Control.DefaultFont, dpi) { IsBottomEdgeConnected = true });
            Assert.Equal(theme.Border.ToArgb(), bitmap.GetPixel(bounds.Left, bounds.Bottom - 1).ToArgb()); Assert.Equal(theme.Border.ToArgb(), bitmap.GetPixel(bounds.Right - 1, bounds.Bottom - 1).ToArgb());
        }
    });

    [Fact]
    public void Minimized_not_below_messages_reflow_on_width_changes_and_body_shadow_stays_above_the_join() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 900, IsMinimized = true };
        ribbon.Tabs.Add(new RibbonTab()); ribbon.QuickAccessItems.Add(new RibbonButtonItem());
        var message = new RibbonMessage { Title = "Notice", Text = "Review the document before enabling editing from this location." }; message.Action.Text = "Review";
        ribbon.MessageBar.Items.Add(message);
        int wide = ribbon.Height; ribbon.Width = 240;
        Assert.True(ribbon.Height > wide); Assert.True(ribbon.Snapshot.MessageBounds.Top >= ribbon.Snapshot.TabViewport.Bottom);
        Assert.Equal(ribbon.Snapshot.MessageBounds.Bottom + RibbonLayoutEngine.Scale(ribbon.Theme.BodyMargin.Bottom, ribbon.DeviceDpi), ribbon.Height);
        ribbon.Width = 900; Assert.Equal(wide, ribbon.Height);
        using var painter = new RibbonOffice2024Renderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int odd in new[] { 0, 1 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var theme = RibbonThemes.Office2024Light with { BodyShadowColor = Color.FromArgb(180, 0, 0, 0) };
            var body = new Rectangle(S(10), S(10), S(220) + odd, S(70) + odd);
            var messages = new Rectangle(body.Left, body.Bottom, body.Width, S(60));
            using var expected = new Bitmap(messages.Right + S(10), messages.Bottom + S(10)); using var actual = new Bitmap(expected.Width, expected.Height);
            using (var g = Graphics.FromImage(expected)) { g.Clear(theme.Background); painter.DrawMessageBar(new(g, messages, theme, Control.DefaultFont, dpi) { IsTopEdgeConnected = true }); }
            using (var g = Graphics.FromImage(actual))
            {
                g.Clear(theme.Background); painter.DrawMessageBar(new(g, messages, theme, Control.DefaultFont, dpi) { IsTopEdgeConnected = true });
                painter.DrawBody(new(g, body, theme, Control.DefaultFont, dpi) { IsBottomEdgeConnected = true });
            }
            for (int y = messages.Top; y < messages.Top + S(10); y++)
            for (int x = messages.Left; x < messages.Right; x++) Assert.Equal(expected.GetPixel(x, y), actual.GetPixel(x, y));
        }
    });

    private sealed class State { public bool Open { get; set; } = true; }
    private sealed class Record { internal bool TopConnected, BottomConnected; }
    private sealed class Office(Record r) : RibbonOffice2024Renderer
    {
        public override void DrawMessageBar(RibbonPaintContext c) { r.TopConnected = c.IsTopEdgeConnected; base.DrawMessageBar(c); }
        public override void DrawBody(RibbonPaintContext c) { r.BottomConnected = c.IsBottomEdgeConnected; base.DrawBody(c); }
    }
    private sealed class Neutral(Record r) : RibbonRenderer
    {
        public override void DrawMessageBar(RibbonPaintContext c) { r.TopConnected = c.IsTopEdgeConnected; base.DrawMessageBar(c); }
    }
}
