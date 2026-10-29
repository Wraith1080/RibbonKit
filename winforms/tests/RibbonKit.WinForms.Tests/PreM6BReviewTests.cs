using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class PreM6BReviewTests
{
    [Fact]
    public void Below_qat_buttons_are_square_and_share_the_header_target_size() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Width = 900 }; ribbon.Tabs.Add(new RibbonTab()); ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save" });
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            var header = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccess).Bounds;
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            var below = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccess).Bounds;
            Assert.Equal(header.Size, below.Size); Assert.Equal(below.Width, below.Height);
            Assert.Equal(RibbonLayoutEngine.Scale(RibbonDensityMetrics.For(density).QuickAccessRow, ribbon.DeviceDpi), ribbon.QuickAccessHeight);
        }
    });

    [Fact]
    public void Joined_message_has_no_top_stroke_and_uses_one_continuous_outer_shadow_at_each_scale() => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer();
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int odd in new[] { 0, 1 })
        {
            int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
            var upper = new Rectangle(S(14), S(12), S(220) + odd, S(90) + odd); var messages = new Rectangle(upper.Left, upper.Bottom, upper.Width, S(70) + odd);
            var theme = RibbonThemes.Office2024Light; using var image = new Bitmap(messages.Right + S(20), messages.Bottom + S(20)); using var g = Graphics.FromImage(image); g.Clear(theme.Background);
            var context = new RibbonPaintContext(g, messages, theme, Control.DefaultFont, dpi);
            Assert.True(renderer.PaintConnectedMessageShadow(context, upper, messages));
            renderer.DrawBody(context with { Bounds = upper, IsBottomEdgeConnected = true, SuppressShadow = true });
            renderer.DrawMessageBar(context with { IsTopEdgeConnected = true, SuppressShadow = true });
            Assert.Equal(theme.MessageBackground.Start.ToArgb(), image.GetPixel(messages.Left + messages.Width / 2, messages.Top).ToArgb());
            for (int x = upper.Left - S(5); x < upper.Left; x++)
                for (int y = upper.Bottom - S(3); y < upper.Bottom + S(4); y++) Assert.Equal(image.GetPixel(x, upper.Bottom - S(4)), image.GetPixel(x, y));
            g.Clear(theme.Background); renderer.DrawMessageBar(context);
            Assert.Equal(theme.MessageBorder.ToArgb(), image.GetPixel(messages.Left + messages.Width / 2, messages.Top).ToArgb());
        }
    });

    [Theory] [InlineData(RibbonDensity.Compact)] [InlineData(RibbonDensity.Touch)]
    public void All_customization_pages_keep_identical_command_columns_and_action_button_bounds(RibbonDensity density) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { Density = density }; var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(new RibbonGroup { Text = "Commands" }); ribbon.Tabs.Add(tab);
        using var qat = new RibbonQuickAccessCustomizationEditor(ribbon, "review"); using var classic = new RibbonCustomizationEditor(ribbon, "review"); using var simplified = new RibbonSimplifiedCustomizationEditor(ribbon, "review");
        foreach (int width in new[] { 900, 901, 1150 })
        foreach (var editor in new UserControl[] { qat, classic, simplified })
        {
            editor.Size = new(width, 600); editor.CreateControl(); editor.PerformLayout();
            var panes = Descendants(editor).OfType<RibbonCustomizationColumns>().Single(); var columns = panes.GetColumnWidths();
            Assert.InRange(Math.Abs(columns[0] - columns[2]), 0, 1);
            var other = Descendants(qat).OfType<RibbonCustomizationColumns>().Single().GetColumnWidths(); Assert.Equal(other, columns);
            var left = panes.GetControlFromPosition(0, 2)!; var right = panes.GetControlFromPosition(2, 2)!;
            Assert.InRange(Math.Abs(left.Width - right.Width), 0, 1);
            var expectedButtons = ActionBounds(qat); var actualButtons = ActionBounds(editor);
            Assert.Equal(expectedButtons, actualButtons);
            Assert.Equal(actualButtons[0].Top, actualButtons[2].Top); Assert.Equal(actualButtons[1].Top, actualButtons[3].Top);
        }
        static Rectangle[] ActionBounds(Control editor)
        {
            var columns = Descendants(editor).OfType<RibbonCustomizationColumns>().Single();
            return new[] { 1, 3 }.SelectMany(c => columns.GetControlFromPosition(c, 1)!.Controls.OfType<Button>())
                .Select(b => new Rectangle(editor.PointToClient(b.PointToScreen(Point.Empty)), b.Size)).ToArray();
        }
    });

    [Fact]
    public void Temporary_body_is_anchored_over_qat_messages_and_outside_dismissal_releases_the_real_popup() => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles(); using var form = new Form { Size = new(1100, 700) }; using var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" }; var field = new RibbonTextBoxItem { Text = "Draft", Value = "Retained" }; group.Items.Add(field); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save" }); ribbon.MessageBar.Items.Add(new RibbonMessage { Text = "Notice" }); ribbon.IsMinimized = true;
        form.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        foreach (var placement in Enum.GetValues<RibbonQuickAccessPosition>())
        {
            ribbon.QuickAccessPosition = placement; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
            ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Element == tab)); ribbon.Popups.SettleMotion(); var popup = ribbon.Popups.Top!; var window = (ToolStripDropDown)popup.TopLevelControl!;
            int S(int n) => RibbonLayoutEngine.Scale(n, ribbon.DeviceDpi); var screen = ribbon.PointToScreen(Point.Empty);
            Assert.Equal(screen.X + S(ribbon.LayoutTheme.BodyMargin.Left), window.Left); Assert.Equal(ribbon.Width - S(ribbon.LayoutTheme.BodyMargin.Horizontal), window.Width);
            Assert.Equal(screen.Y + S(ribbon.LayoutTheme.TabHeight + ribbon.LayoutTheme.BodyMargin.Top), window.Top); Assert.True(window.Top <= ribbon.PointToScreen(ribbon.Snapshot.MessageBounds.Location).Y);
            var handle = ribbon.Editors.EditorFor(field)!.Handle;
            ribbon.Popups.OutsidePress(form.PointToScreen(new(500, 600))); Assert.False(ribbon.Popups.HasOpen); Assert.True(ribbon.IsAnimating); Assert.True(popup.IsDisposed); Assert.True(ribbon.IsMinimized); Assert.Equal("Retained", field.Value);
            Assert.Equal(handle, ribbon.Editors.EditorFor(field)!.Handle); ribbon.SettleMotion(); Assert.False(ribbon.IsAnimating);
        }
    });
    [Fact]
    public void Temporary_dismissal_policies_and_resize_leave_no_live_or_animated_popup() => FoundationTests.Sta(() =>
    {
        using var renderer = new RibbonOffice2024Renderer(); using var form = new Form { Size = new(1000, 650) }; using var ribbon = new RibbonControl { Renderer = renderer, IsMinimized = true };
        var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(new RibbonGroup { Text = "Commands" }); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon); form.Show(); Application.DoEvents(); ribbon.Motion.SystemPolicyOverride = true;
        void Open() { ribbon.Activate(ribbon.Snapshot.Targets.Single(t => t.Element == tab)); ribbon.Popups.SettleMotion(); Assert.True(ribbon.Popups.HasOpen); }
        void Close() => ribbon.Popups.OutsidePress(form.PointToScreen(new(700, 550)));
        Open(); Close(); Assert.False(ribbon.IsAnimating); Assert.False(ribbon.Popups.HasOpen);
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.Motion.SystemPolicyOverride = false; Open(); Close(); Assert.False(ribbon.IsAnimating);
        ribbon.Motion.SystemPolicyOverride = true; Open(); Close(); Assert.True(ribbon.IsAnimating); form.Width -= 10; Assert.False(ribbon.IsAnimating);
        Open(); Close(); Assert.True(ribbon.IsAnimating); ribbon.AnimationLevel = RibbonAnimationLevel.None; Assert.False(ribbon.IsAnimating);
        ribbon.SettleMotion(); ribbon.SettleMotion(); Assert.False(ribbon.Popups.HasOpen);
    });
    private static IEnumerable<Control> Descendants(Control c) => c.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(Descendants(child)));
}
