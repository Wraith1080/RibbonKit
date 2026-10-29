using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class RibbonFormTests
{
    [Fact]
    public void Association_fallback_switching_and_disposal_preserve_one_command_owner() => FoundationTests.Sta(() =>
    {
        using var first = new RibbonForm { Width = 1000 };
        using var second = new RibbonForm { Width = 1000 };
        using var ordinary = new Form { Width = 1000 };
        var ribbon = Create(); ordinary.Controls.Add(ribbon);
        var entries = ribbon.QuickAccessItems.ToArray(); var source = ((RibbonQuickAccessReference)entries[0]).Source!;
        Assert.Equal(RibbonQuickAccessPosition.TitleBar, ribbon.QuickAccessPosition);
        Assert.Equal(RibbonQuickAccessPosition.TabRow, ribbon.EffectiveQuickAccessPosition);
        Assert.Throws<ArgumentException>(() => first.Ribbon = ribbon);
        first.Controls.Add(ribbon); first.Ribbon = ribbon; first.Show(); Application.DoEvents();
        Assert.Equal(RibbonQuickAccessPosition.TitleBar, ribbon.EffectiveQuickAccessPosition);
        Assert.Equal(first.CaptionHeight, ribbon.Snapshot.TabViewport.Top);
        Assert.True(ribbon.Snapshot.QuickAccessBounds.Bottom <= ribbon.Snapshot.TabViewport.Top);
        ribbon.QuickAccessVisible = false; first.Ribbon = null;
        Assert.False(source.IsDisposed); Assert.Equal(entries, ribbon.QuickAccessItems);
        Assert.Equal(RibbonQuickAccessPosition.TitleBar, ribbon.QuickAccessPosition);
        second.Controls.Add(ribbon); second.Ribbon = ribbon; second.Show(); Application.DoEvents(); ribbon.QuickAccessVisible = true;
        Assert.Equal(RibbonQuickAccessPosition.TitleBar, ribbon.EffectiveQuickAccessPosition);
        Assert.Same(ribbon.Tabs[0].Groups[0], source.Parent);
        ribbon.Dock = DockStyle.None;
        Assert.Null(second.Ribbon); Assert.Equal(RibbonQuickAccessPosition.TabRow, ribbon.EffectiveQuickAccessPosition);
        ribbon.Dock = DockStyle.Top; first.Controls.Add(ribbon); first.Ribbon = ribbon;
        ordinary.Controls.Add(ribbon);
        Assert.Null(first.Ribbon); Assert.Equal(entries, ribbon.QuickAccessItems);
        ribbon.Dispose(); Assert.True(source.IsDisposed);
    });

    [Theory]
    [InlineData(RibbonDensity.Compact, RibbonLayoutMode.Classic)]
    [InlineData(RibbonDensity.Touch, RibbonLayoutMode.Classic)]
    [InlineData(RibbonDensity.Compact, RibbonLayoutMode.Simplified)]
    [InlineData(RibbonDensity.Touch, RibbonLayoutMode.Simplified)]
    public void Measured_caption_targets_tabs_messages_and_minimized_anchors_are_disjoint(RibbonDensity density, RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 1000 }; var ribbon = Create();
        ribbon.Density = density; ribbon.LayoutMode = mode; ribbon.FileMenu.Visible = true;
        ribbon.MessageBar.Items.Add(new RibbonMessage { Title = "Notice", Text = "Window integration" });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        foreach (bool minimized in new[] { false, true })
        {
            ribbon.IsMinimized = minimized;
            var snapshot = ribbon.Snapshot;
            Assert.Equal(RibbonLayoutEngine.Scale(ribbon.Theme.BodyMargin.Left, ribbon.DeviceDpi), snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu).Bounds.Left);
            Assert.Equal(form.CaptionHeight, snapshot.TabViewport.Top);
            Assert.False(snapshot.QuickAccessBounds.IntersectsWith(snapshot.TabViewport));
            Assert.False(snapshot.QuickAccessBounds.IntersectsWith(snapshot.MessageBounds));
            Assert.True(snapshot.QuickAccessBounds.Right <= form.Caption.Buttons.Left);
            foreach (var target in snapshot.Targets.Where(t => t.Kind == TargetKind.QuickAccess)) Assert.Equal(1, form.CaptionHitTest(Center(target.Bounds)));
            Assert.Equal(2, form.CaptionHitTest(new(form.Caption.Buttons.Left - 10, form.CaptionHeight / 2)));
            if (minimized)
            {
                var target = snapshot.Targets.First(t => t.Kind == TargetKind.Tab); ribbon.Activate(target);
                Assert.True(ribbon.Popups.HasOpen);
                Assert.True(ribbon.Popups.Top!.PointToScreen(Point.Empty).Y >= ribbon.PointToScreen(new(0, snapshot.TabViewport.Bottom)).Y);
                ribbon.Popups.CloseAll(false);
                Assert.False(ribbon.MotionBottomConnected);
            }
        }
    });

    [Fact]
    public void Caption_metrics_and_hit_zones_scale_without_button_overlap() => FoundationTests.Sta(() =>
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int width in new[] { 180, 601, 1000 })
        {
            var geometry = RibbonCaptionGeometry.Measure(RibbonLayoutEngine.Scale(width, dpi), RibbonLayoutEngine.Scale(36, dpi), dpi, true, true, true, true);
            Assert.Equal(3, geometry.Targets.Length);
            foreach (var (bounds, _) in geometry.Targets)
            {
                Assert.Equal(0, bounds.Top); Assert.Equal(RibbonLayoutEngine.Scale(36, dpi), bounds.Height);
                Assert.True(bounds.Left >= geometry.ContentLeft); Assert.True(bounds.Right <= RibbonLayoutEngine.Scale(width, dpi));
                Assert.Single(geometry.Targets, t => t.Bounds.IntersectsWith(bounds));
            }
        }
        using var form = new RibbonForm { Width = 900 }; form.Show(); Application.DoEvents();
        foreach (var (bounds, hit) in form.Caption.Targets) Assert.Equal(hit, form.CaptionHitTest(Center(bounds)));
        Assert.Equal(12, form.CaptionHitTest(new(200, 1)));
        form.WindowState = FormWindowState.Maximized; Application.DoEvents();
        Assert.Equal(2, form.CaptionHitTest(new(200, 1)));
    });

    [Fact]
    public void Profiles_and_context_commands_retain_TitleBar_and_old_values() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 900 }; var ribbon = Create(); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        using var menu = ribbon.CreateCustomizationMenu(null, true);
        Assert.Contains(menu.Items, i => i.Id == "TitleBarQuickAccess"); Assert.Contains(menu.Items, i => i.Id == "TabRowQuickAccess");
        using var session = ribbon.CommandCatalog.BeginCustomization("window");
        var json = RibbonCustomizationSerializer.Serialize(session.Draft);
        var state = RibbonCustomizationSerializer.Deserialize(json, "window");
        Assert.Equal(RibbonQuickAccessPosition.TitleBar, state.QuickAccessPosition);
        session.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; Assert.True(session.Apply());
        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, ribbon.QuickAccessPosition);
        form.Ribbon = null;
        using var fallbackMenu = ribbon.CreateCustomizationMenu(null, true);
        Assert.DoesNotContain(fallbackMenu.Items, i => i.Id == "TitleBarQuickAccess");
        Assert.Equal(0, (int)RibbonQuickAccessPosition.TabRow); Assert.Equal(1, (int)RibbonQuickAccessPosition.BelowRibbon);
        Assert.Equal(DesignerSerializationVisibility.Hidden, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.EffectiveQuickAccessPosition)]!.SerializationVisibility);
    });

    [Fact]
    public void Narrow_or_unsupported_caption_falls_back_and_recovers_without_losing_preferences() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 1000 }; var ribbon = Create(); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        form.Width = 140; Application.DoEvents();
        Assert.Equal(RibbonQuickAccessPosition.TabRow, ribbon.EffectiveQuickAccessPosition);
        Assert.Contains(ribbon.Snapshot.Targets, t => t.Kind is TargetKind.QuickAccess or TargetKind.QuickAccessOverflow && !t.VisibleBounds.IsEmpty);
        form.Width = 1000; Assert.Equal(RibbonQuickAccessPosition.TitleBar, ribbon.EffectiveQuickAccessPosition);
        form.Padding = new Padding(8); Application.DoEvents();
        Assert.Equal(RibbonQuickAccessPosition.TabRow, ribbon.EffectiveQuickAccessPosition);
        Assert.Equal(0, ribbon.WindowCaptionHeight); Assert.True(ribbon.Top >= form.CaptionHeight);
        form.Padding = Padding.Empty; Application.DoEvents();
        Assert.Equal(RibbonQuickAccessPosition.TitleBar, ribbon.EffectiveQuickAccessPosition);
        using var top = new Panel { Dock = DockStyle.Top, Height = 20 }; form.Controls.Add(top); Application.DoEvents();
        Assert.Equal(RibbonQuickAccessPosition.TabRow, ribbon.EffectiveQuickAccessPosition);
        form.Controls.Remove(top); Application.DoEvents(); Assert.Equal(RibbonQuickAccessPosition.TitleBar, ribbon.EffectiveQuickAccessPosition);
        form.FormBorderStyle = FormBorderStyle.None; Application.DoEvents(); Assert.Equal(RibbonQuickAccessPosition.TabRow, ribbon.EffectiveQuickAccessPosition);
        form.FormBorderStyle = FormBorderStyle.Sizable; Application.DoEvents(); Assert.Equal(RibbonQuickAccessPosition.TitleBar, ribbon.EffectiveQuickAccessPosition);
    });

    [Fact]
    public void TitleBar_uses_existing_validation_KeyTips_overflow_and_popup_ownership() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 900 }; var ribbon = Create(); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var field = new RibbonTextBoxItem { Text = "Name", Value = "valid" }; ribbon.Tabs[0].Groups[0].Items.Add(field);
        var command = ((RibbonQuickAccessReference)ribbon.QuickAccessItems[0]).Source!; int clicks = 0; command.Click += (_, _) => clicks++;
        bool invalid = true; field.ValueValidating += (_, e) => e.Cancel = invalid;
        form.Show(); Application.DoEvents(); ribbon.Editors.Focus(field); ribbon.Editors.EditorFor(field)!.Text = "changed";
        var target = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccess);
        ribbon.Activate(target); Assert.Equal(0, clicks); Assert.True(ribbon.Editors.ContainsFocus);
        invalid = false; ribbon.Activate(target); Assert.Equal(1, clicks); Assert.Equal("changed", field.Value);
        ribbon.ToggleKeyTips(); Assert.True(ribbon.KeyTipsActive); ribbon.ProcessRibbonKey(Keys.D1); Assert.Equal(2, clicks);
        ribbon.QuickAccessMaxWidth = 28;
        for (int i = 0; i < 12; i++) ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Action " + i });
        var overflow = ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccessOverflow);
        Assert.False(overflow.VisibleBounds.IsEmpty); ribbon.Activate(overflow); Assert.True(ribbon.Popups.HasOpen);
        form.Ribbon = null; Assert.False(ribbon.Popups.HasOpen); Assert.False(command.IsDisposed);
    });

    [Fact]
    public void Backstage_and_geometry_drawings_keep_the_caption_band_and_editor_identity() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 900, Height = 550 }; var ribbon = Create(); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        using var page = new TextBox { Text = "Document state" }; ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.FileMenu.Visible = true; form.Show(); Application.DoEvents();
        ribbon.IsBackstageOpen = true; Assert.Equal(form.CaptionHeight, ribbon.BackstageSurface!.Top); Assert.Equal(form.ClientSize.Height, ribbon.BackstageSurface.Bottom);
        nint handle = page.Handle; form.Width += 20; Assert.Equal(form.CaptionHeight, ribbon.BackstageSurface.Top);
        ribbon.Density = RibbonDensity.Touch; Assert.Equal(form.CaptionHeight, ribbon.BackstageSurface.Top);
        form.Ribbon = null; Assert.Equal(form.CaptionHeight, ribbon.BackstageSurface.Top);
        form.Ribbon = ribbon; Assert.Equal(form.CaptionHeight, ribbon.BackstageSurface.Top); ribbon.IsBackstageOpen = false;
        Assert.Equal(handle, page.Handle); Assert.Equal("Document state", page.Text);
        using var from = new RibbonMotionScene(ribbon); ribbon.IsMinimized = true; using var to = new RibbonMotionScene(ribbon);
        Assert.Equal(form.CaptionHeight, to.Header.Top);
        using var frame = new Bitmap(ribbon.Width, from.Image.Height); using var graphics = Graphics.FromImage(frame); using var alpha = new System.Drawing.Imaging.ImageAttributes();
        RibbonMotionScene.Render(graphics, to.Bounds, ribbon, from, to, .5, alpha);
        for (int y = 2; y < to.Header.Top - 2; y += 3)
        for (int x = 2; x < to.Image.Width - 2; x += 3) Assert.Equal(to.Image.GetPixel(x, y).ToArgb(), frame.GetPixel(x, y).ToArgb());
    });

    private static Point Center(Rectangle bounds) => new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

    [Fact]
    public void TitleBar_moves_are_immediate_while_existing_tabrow_lower_context_motion_is_retained() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 900, Height = 550 }; var ribbon = Create(); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.Motion.SystemPolicyOverride = true;
        using (var menu = ribbon.CreateCustomizationMenu(null, true)) ((RibbonButtonItem)menu.Items.Single(i => i.Id == "TabRowQuickAccess")).PerformClick();
        Assert.Equal(RibbonQuickAccessPosition.TabRow, ribbon.QuickAccessPosition); Assert.False(ribbon.IsAnimating);
        using (var menu = ribbon.CreateCustomizationMenu(null, true)) ((RibbonButtonItem)menu.Items.Single(i => i.Id == "MoveQuickAccess")).PerformClick();
        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, ribbon.QuickAccessPosition); Assert.True(ribbon.IsAnimating); ribbon.SettleMotion();
        using (var menu = ribbon.CreateCustomizationMenu(null, true)) ((RibbonButtonItem)menu.Items.Single(i => i.Id == "TitleBarQuickAccess")).PerformClick();
        Assert.Equal(RibbonQuickAccessPosition.TitleBar, ribbon.QuickAccessPosition); Assert.False(ribbon.IsAnimating);
    });

    private static RibbonControl Create()
    {
        var ribbon = new RibbonControl { QuickAccessPosition = RibbonQuickAccessPosition.TitleBar };
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Tools" }; var command = new RibbonToggleItem { Text = "Bold" };
        group.Items.Add(command); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command });
        return ribbon;
    }
}
