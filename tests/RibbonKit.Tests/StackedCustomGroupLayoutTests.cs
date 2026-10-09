using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Layout;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class StackedCustomGroupLayoutTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2024, FlowDirection.LeftToRight, false)]
    [InlineData(RibbonTheme.Office2024, FlowDirection.RightToLeft, false)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.LeftToRight, false)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.RightToLeft, false)]
    [InlineData(RibbonTheme.Office2024, FlowDirection.LeftToRight, true)]
    [InlineData(RibbonTheme.Office2024, FlowDirection.RightToLeft, true)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.LeftToRight, true)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.RightToLeft, true)]
    public void Extra_touch_commands_start_new_columns_without_growing_the_ribbon(RibbonTheme theme, FlowDirection flow, bool startTouch) => Sta.Run(() =>
    {
        var application = Sta.UseApplication(); ThemeManager.Apply(application, theme);
        var animation = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var ribbon = new Ribbon { FlowDirection = flow, Density = startTouch ? RibbonDensity.Touch : RibbonDensity.Compact };
        var sourceTab = new RibbonTab { Header = "Source" }; var sources = new RibbonGroup { Header = "Source commands" };
        sourceTab.Groups.Add(sources); ribbon.Tabs.Add(sourceTab);
        var font = new RibbonComboBox { Header = "Font", Icon = Icon() }; font.Items.Add("Calibri");
        var size = new RibbonComboBox { Header = "Font Size", Icon = Icon() }; size.Items.Add("11");
        var mode = new RibbonDropDownButton { Header = "Touch/Mouse Mode", Icon = Icon() }; mode.Items.Add(new RibbonMenuItem { Header = "Touch" });
        var gallery = new InRibbonGallery { Header = "Styles", Icon = Icon() }; gallery.Items.Add(new RibbonGalleryItem { Content = "Normal" });
        var commands = new FrameworkElement[] { font, size, mode, gallery,
            new RibbonButton { Header = "Undo", Icon = Icon() }, new RibbonButton { Header = "Redo", Icon = Icon() }, new RibbonButton { Header = "Save", Icon = Icon() } };
        foreach (var command in commands) sources.Items.Add(command);
        var custom = new RibbonTab { Header = "Custom" }; var group = new RibbonGroup { Header = "Custom group", Layout = RibbonGroupLayout.Stacked, CanResize = false };
        Ribbon.SetIsCustom(group, true); custom.Groups.Add(group); ribbon.Tabs.Add(custom); ribbon.SelectedTab = custom;
        var copies = commands.Select(command => ribbon.CreateCommandProxy(command, RibbonControlSize.Medium)).ToArray();
        foreach (var copy in copies.Take(3)) group.Items.Add(copy);
        var window = new Window { Content = ribbon, Width = 1100, Height = 600, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout(); VerifyDensity();
            ribbon.Density = ribbon.Density == RibbonDensity.Touch ? RibbonDensity.Compact : RibbonDensity.Touch; Layout(); VerifyDensity();
            ribbon.Density = RibbonDensity.Compact; Layout(); VerifyDensity();
            ribbon.Density = RibbonDensity.Touch; Layout(); VerifyDensity();

            // The same items panel is reparented into the collapsed group's popup.
            foreach (var copy in copies.Skip(3)) group.Items.Add(copy);
            group.CanResize = true; group.ReductionMode = RibbonGroupReductionMode.Collapse; window.Width = 220; Layout();
            Assert.Equal(RibbonGroupSizeState.Collapsed, group.SizeState);
            Part<ToggleButton>(group, "PART_CollapsedButton").IsChecked = true; Layout();
            Assert.True(Part<Popup>(group, "PART_Popup").IsOpen); VerifyColumns();
        }
        finally { window.Close(); Sta.Drain(); Sta.ResetApplication(); RibbonAnimation.GlobalLevel = animation; }

        void VerifyDensity()
        {
            Assert.Equal(3, group.Items.Count);
            double height = ribbon.ActualHeight;
            foreach (var copy in copies.Skip(3))
            {
                group.Items.Add(copy); Layout();
                Assert.InRange(ribbon.ActualHeight, height - 0.5, height + 0.5);
                VerifyColumns();
            }
            group.Items.Remove(copies[1]); copies[1].Visibility = Visibility.Collapsed; group.Items.Insert(1, copies[1]); Layout();
            var visible = copies.Where(copy => copy.Visibility != Visibility.Collapsed).ToArray();
            Assert.InRange(LayoutInformation.GetLayoutSlot(visible[3]).Y, -0.5, 0.5);
            copies[1].Visibility = Visibility.Visible; Layout(); VerifyColumns();
            foreach (var copy in copies.Skip(3)) group.Items.Remove(copy);
            Layout(); Assert.InRange(ribbon.ActualHeight, height - 0.5, height + 0.5);
        }
        void VerifyColumns()
        {
            var panel = ParentPanel(copies[0]);
            for (int i = 0; i < group.Items.Count; i++)
            {
                var slot = LayoutInformation.GetLayoutSlot(copies[i]);
                Assert.Equal(i % 3 == 0, Math.Abs(slot.Y) < 0.5);
                Assert.InRange(slot.X, LayoutInformation.GetLayoutSlot(copies[(i / 3) * 3]).X - 0.5, LayoutInformation.GetLayoutSlot(copies[(i / 3) * 3]).X + 0.5);
                if (ribbon.Density == RibbonDensity.Touch) Assert.True(copies[i].ActualHeight >= 44);
                if (i >= 3 && i % 3 == 0)
                {
                    var current = copies[i].TranslatePoint(new Point(), window).X;
                    var previous = copies[i - 3].TranslatePoint(new Point(), window).X;
                    Assert.True(flow == FlowDirection.LeftToRight ? current > previous : current < previous);
                }
                Assert.True(slot.Bottom <= panel.ActualHeight + 0.5);
            }
        }
        void Layout() { Sta.Drain(DispatcherPriority.ApplicationIdle); window.UpdateLayout(); Sta.Drain(DispatcherPriority.ApplicationIdle); }
    });

    private static Panel ParentPanel(FrameworkElement element)
    {
        for (var parent = VisualTreeHelper.GetParent(element); parent is not null; parent = VisualTreeHelper.GetParent(parent))
            if (parent is Panel { IsItemsHost: true } panel) return panel;
        throw new InvalidOperationException("No items panel for the custom command.");
    }
    private static T Part<T>(Control control, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(control.Template.FindName(name, control));
    private static DrawingImage Icon() => new(new GeometryDrawing(Brushes.SteelBlue, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));
}
