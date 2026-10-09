using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class GalleryRowScrollingTests
{
    [Theory]
    [InlineData(true, FlowDirection.LeftToRight, RibbonDensity.Compact, false)]
    [InlineData(true, FlowDirection.RightToLeft, RibbonDensity.Compact, false)]
    [InlineData(false, FlowDirection.LeftToRight, RibbonDensity.Compact, false)]
    [InlineData(false, FlowDirection.RightToLeft, RibbonDensity.Compact, false)]
    [InlineData(true, FlowDirection.LeftToRight, RibbonDensity.Touch, false)]
    [InlineData(true, FlowDirection.RightToLeft, RibbonDensity.Touch, false)]
    [InlineData(false, FlowDirection.LeftToRight, RibbonDensity.Touch, false)]
    [InlineData(false, FlowDirection.RightToLeft, RibbonDensity.Touch, false)]
    [InlineData(true, FlowDirection.LeftToRight, RibbonDensity.Compact, true)]
    [InlineData(true, FlowDirection.RightToLeft, RibbonDensity.Compact, true)]
    public void Reversing_at_either_end_keeps_each_tile_row_aligned(bool grouped, FlowDirection flow,
        RibbonDensity density, bool animated) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024);
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = animated ? RibbonAnimationLevel.Subtle : RibbonAnimationLevel.None;
        var items = Enumerable.Range(0, grouped ? 6 : 12).Select(i => new RibbonGalleryItem
        {
            Tag = grouped ? i == 0 ? "Modern" : i < 4 ? "Office Modern" : "Office Legacy" : "Styles",
            Content = new TextBlock { Text = $"Item {i}", Width = 112,
                Height = grouped ? 40 : 40 + (i / 2 % 2) * 5 },
        }).ToArray();
        var gallery = new InRibbonGallery { Width = grouped ? 158 : 320, SelectedIndex = 0 };
        if (grouped)
        {
            var view = new ListCollectionView(items); view.GroupDescriptions.Add(new PropertyGroupDescription("Tag"));
            gallery.ItemsSource = view;
        }
        else foreach (var item in items) gallery.Items.Add(item);
        Ribbon.SetDensity(gallery, density);
        var window = new Window { Content = gallery, FlowDirection = flow, Width = 650, Height = 250,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false, UseLayoutRounding = true };
        try
        {
            window.Show(); VisualTreeHelper.SetRootDpi(window, new DpiScale(1.25, 1.25)); Layout();
            var strip = Part<ScrollViewer>(gallery, "PART_ScrollViewer");
            var up = Part<ButtonBase>(gallery, "PART_LineUp"); var down = Part<ButtonBase>(gallery, "PART_LineDown");
            var rows = grouped ? items : items.Where((_, i) => i % 2 == 0).ToArray();
            Assert.True(strip.ScrollableHeight > strip.ViewportHeight * 3);
            for (int cycle = 0; cycle < 2; cycle++)
            {
                strip.ScrollToVerticalOffset(strip.ScrollableHeight); Layout();
                for (int row = rows.Length - 2; row >= 0; row--)
                {
                    Press(up); AssertAligned(row);
                }
                Press(up); Assert.Equal(0, strip.VerticalOffset, 3);
                for (int row = 1; row < rows.Length - 1; row++)
                {
                    Press(down); AssertAligned(row);
                }
                Press(down); Assert.Equal(strip.ScrollableHeight, strip.VerticalOffset, 3);
                Press(down); Assert.Equal(strip.ScrollableHeight, strip.VerticalOffset, 3);
                Assert.Equal(0, gallery.SelectedIndex);
            }

            void Press(ButtonBase button)
            {
                button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                if (animated) Pump(220);
                Layout();
            }
            void AssertAligned(int row)
            {
                double top = rows[row].TransformToAncestor(strip).Transform(default).Y;
                Assert.True(Math.Abs(top) < 0.1,
                    $"grouped={grouped}, {flow}/{density}, row={row}: top={top}, offset={strip.VerticalOffset}, viewport={strip.ViewportHeight}");
            }
        }
        finally { window.Close(); RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication(); }
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    private static T Part<T>(Control owner, string name) where T : DependencyObject =>
        Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));

    private static void Pump(int milliseconds)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(milliseconds) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame);
    }
}
