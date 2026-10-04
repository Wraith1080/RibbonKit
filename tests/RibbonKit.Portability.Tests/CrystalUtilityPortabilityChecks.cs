using System.Reflection;
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

namespace RibbonKit.Portability.Tests;

// The existing single STA consumer owns Application. No Showcase resource or helper.
internal static class CrystalUtilityPortabilityChecks
{
    internal static void Verify(Application application)
    {
        var animation = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var ribbon = new Ribbon { QuickAccessMaxWidth = 45, Backstage = new Backstage() };
        var home = new RibbonTab { Header = "Home" };
        for (int i = 0; i < 8; i++)
        {
            var group = new RibbonGroup { Header = "Commands " + i, CanResize = false };
            group.Items.Add(new RibbonButton { Header = "A long command " + i });
            home.Groups.Add(group);
            ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save " + i });
            ribbon.Tabs.Add(new RibbonTab { Header = "Long tab " + i });
        }
        ribbon.Tabs.Insert(0, home);
        ribbon.SelectedTab = home;
        ribbon.ShowMergedCaption(null, "Document");
        var viewer = new ScrollViewer { Height = 100, Width = 210,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            Content = new Border { Width = 700, Height = 700 } };
        var panel = new StackPanel();
        panel.Children.Add(ribbon);
        panel.Children.Add(viewer);
        var window = new RibbonWindow { Content = panel, Width = 520, Height = 400,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            ThemeManager.SetDarkMode(application, false);
            window.Show();
            Layout();
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var minimize = Part<ToggleButton>(tabs, "MinimizeToggle");
            var qat = Part<RibbonQuickAccessToolBar>(tabs, "QatTabRowHost");
            Assert.True(qat.HasOverflow);
            var overflow = Part<ToggleButton>(qat, "PART_OverflowButton");
            var bodyScroll = Part<RibbonScrollContentHost>(tabs, "PART_ContentScroll");
            var tabScroll = Part<RibbonScrollContentHost>(tabs, "PART_TabScroll");
            var arrows = new[] { tabScroll, bodyScroll }.SelectMany(host =>
                ((Grid)VisualTreeHelper.GetParent(host)).Children.OfType<RepeatButton>()).ToArray();
            Assert.Equal(4, arrows.Length);
            var captionButtons = Descendants<Button>(tabs).Where(b => b.Command == ribbon.MergedCaptionCommand).ToArray();
            Assert.Equal(3, captionButtons.Length);
            var utilityButtons = new ButtonBase[] { minimize, overflow }.Concat(arrows).Concat(captionButtons).ToArray();
            foreach (bool dark in new[] { false, true, false })
            {
                ThemeManager.SetDarkMode(application, dark);
                foreach (var direction in new[] { FlowDirection.LeftToRight, FlowDirection.RightToLeft })
                {
                    window.FlowDirection = direction;
                    Layout();
                    foreach (var button in utilityButtons) CheckRim(button, 1);
                    foreach (var arrow in arrows)
                    {
                        var chrome = Part<Border>(arrow, "Chrome");
                        Assert.Same(arrow.FindResource(arrow.Tag is "BodyScroll"
                            ? "RibbonKit.Brushes.TabStrip.ControlHoverBackground" : "RibbonKit.Brushes.TabStrip.ControlHoverBackground"), chrome.Background);
                        SetState(arrow, typeof(UIElement), "IsMouseOverPropertyKey", true);
                        Assert.Same(arrow.FindResource("RibbonKit.Brushes.TabStrip.ScrollButtonBackground"), chrome.Background);
                        SetState(arrow, typeof(ButtonBase), "IsPressedPropertyKey", true);
                        Assert.Same(arrow.FindResource("RibbonKit.Brushes.TabStrip.ControlPressedBackground"), chrome.Background);
                        SetState(arrow, typeof(ButtonBase), "IsPressedPropertyKey", false);
                        SetState(arrow, typeof(UIElement), "IsMouseOverPropertyKey", false);
                        Assert.Equal(arrow.Tag is "BodyScroll" ? 32d : 22d, arrow.Width);
                        Assert.Equal(chrome.CornerRadius, Part<Border>(arrow, "CrystalUtilityRim").CornerRadius);
                    }
                    foreach (var orientation in new[] { Orientation.Vertical, Orientation.Horizontal })
                        CheckScrollBar(viewer, orientation);
                }
            }
            // Native routed scrolling is retained in both axes.
            foreach (string name in new[] { "PART_VerticalScrollBar", "PART_HorizontalScrollBar" })
            {
                var bar = Part<ScrollBar>(viewer, name);
                NativeClick(Part<RepeatButton>(bar, "IncreaseButton"));
                Layout();
                Assert.True(name.Contains("Vertical") ? viewer.VerticalOffset > 0 : viewer.HorizontalOffset > 0);
                var track = Assert.IsAssignableFrom<Track>(bar.Template.FindName("PART_Track", bar));
                NativeClick(track.IncreaseRepeatButton);
                Layout();
                Assert.True(name.Contains("Vertical") ? viewer.VerticalOffset > 16 : viewer.HorizontalOffset > 16);
            }
            Assert.True(bodyScroll.ScrollRightCommand.CanExecute(null));
            bodyScroll.ScrollRightCommand.Execute(null);
            Assert.True(bodyScroll.Offset > 0);
            Assert.True(tabScroll.ScrollRightCommand.CanExecute(null));
            tabScroll.ScrollRightCommand.Execute(null);
            Assert.True(tabScroll.Offset > 0);
            minimize.IsChecked = true;
            Layout();
            Assert.True(ribbon.IsMinimized);
            Assert.Equal(1, Part<Border>(minimize, "CrystalUtilityRim").Opacity);
            minimize.IsChecked = false;
            overflow.IsChecked = true;
            Layout();
            Assert.True(Part<Popup>(qat, "PART_OverflowPopup").IsOpen);
            overflow.IsChecked = false;
            var modal = new RibbonTab { Header = "Preview", IsModal = true };
            ribbon.Tabs.Add(modal);
            Assert.True(ribbon.EnterModal(modal));
            Layout();
            var close = Part<Button>(tabs, "PART_ModalClose");
            CheckRim(close, 1);
            NativeClick(close);
            Layout();
            Assert.False(ribbon.IsModal);
            int captionActions = 0;
            ribbon.MergedCaptionActionRequested += (_, _) => captionActions++;
            foreach (var caption in captionButtons) NativeClick(caption);
            Assert.Equal(3, captionActions);

            // Existing parts track scoped keys; local paint/corners and native styles win.
            var rim = Part<Border>(minimize, "CrystalUtilityRim");
            var scoped = new SolidColorBrush(Colors.Magenta);
            ribbon.Resources["RibbonKit.Brushes.Control.HoverBorder"] = scoped;
            SetState(minimize, typeof(UIElement), "IsMouseOverPropertyKey", true);
            Assert.Same(scoped, rim.BorderBrush);
            rim.BorderBrush = Brushes.Lime;
            Part<Border>(minimize, "Chrome").CornerRadius = new CornerRadius(9);
            ThemeManager.SetDarkMode(application, true);
            Layout();
            Assert.Same(Brushes.Lime, rim.BorderBrush);
            Assert.Equal(new CornerRadius(9), rim.CornerRadius);
            rim.ClearValue(Border.BorderBrushProperty);
            Part<Border>(minimize, "Chrome").ClearValue(Border.CornerRadiusProperty);
            ribbon.Resources.Remove("RibbonKit.Brushes.Control.HoverBorder");
            SetState(minimize, typeof(UIElement), "IsMouseOverPropertyKey", false);
            var firstArrow = arrows[0];
            firstArrow.Resources["RibbonKit.Brushes.TabStrip.ControlHoverBackground"] = Brushes.Lime;
            Assert.Same(Brushes.Lime, Part<Border>(firstArrow, "Chrome").Background);
            firstArrow.Resources.Remove("RibbonKit.Brushes.TabStrip.ControlHoverBackground");
            firstArrow.Background = Brushes.Magenta;
            SetState(firstArrow, typeof(UIElement), "IsMouseOverPropertyKey", true);
            Assert.Same(Brushes.Magenta, Part<Border>(firstArrow, "Chrome").Background);
            firstArrow.ClearValue(Control.BackgroundProperty);
            SetState(firstArrow, typeof(UIElement), "IsMouseOverPropertyKey", false);
            var vertical = Part<ScrollBar>(viewer, "PART_VerticalScrollBar");
            var crystalScrollTemplate = vertical.Template;
            var explicitStyle = new Style(typeof(ScrollBar));
            explicitStyle.Setters.Add(new Setter(FrameworkElement.WidthProperty, 21d));
            vertical.Style = explicitStyle;
            Layout();
            Assert.Equal(21, vertical.Width);
            vertical.ClearValue(FrameworkElement.StyleProperty);

            foreach (var theme in new[] { RibbonTheme.Office2007, RibbonTheme.Office2010, RibbonTheme.Office2013,
                         RibbonTheme.Office2019, RibbonTheme.Office2024 })
            foreach (bool dark in new[] { false, true })
            {
                ThemeManager.Apply(application, theme);
                ThemeManager.SetDarkMode(application, dark);
                Layout();
                foreach (var button in utilityButtons) CheckRim(button, 0);
                foreach (var arrow in arrows)
                    Assert.Same(arrow.Background, Part<Border>(arrow, "Chrome").Background);
                Assert.NotSame(crystalScrollTemplate, vertical.Template);
            }
            // A manually scoped palette works with Office at application scope.
            foreach (bool dark in new[] { false, true })
            {
                var lightTokens = new ResourceDictionary { Source = new Uri("/RibbonKit;component/Themes/Tokens.Crystal.Light.xaml", UriKind.Relative) };
                window.Resources.MergedDictionaries.Add(lightTokens);
                ResourceDictionary? darkTokens = null;
                if (dark)
                {
                    darkTokens = new ResourceDictionary { Source = new Uri("/RibbonKit;component/Themes/Tokens.Crystal.Dark.xaml", UriKind.Relative) };
                    window.Resources.MergedDictionaries.Add(darkTokens);
                }
                Layout();
                foreach (var button in utilityButtons) CheckRim(button, 1);
                CheckScrollBar(viewer, Orientation.Vertical);
                if (darkTokens != null) window.Resources.MergedDictionaries.Remove(darkTokens);
                window.Resources.MergedDictionaries.Remove(lightTokens);
            }
            // An app-owned tall Options page receives the same native scrolling.
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            var options = new RibbonOptionsDialog { Width = 700, Height = 340,
                Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
            var page = new RibbonOptionsPage { Header = "Editor", Content = new Border { Height = 1000 } };
            options.Pages.Add(page);
            options.SelectedPage = page;
            try
            {
                options.Show();
                foreach (bool dark in new[] { false, true })
                {
                    ThemeManager.SetDarkMode(application, dark);
                    Layout();
                    options.UpdateLayout();
                    var scroll = Part<ScrollViewer>(options, "PART_ContentScroll");
                    CheckScrollBar(scroll, Orientation.Vertical);
                    var scopedWash = new SolidColorBrush(dark ? Colors.Orange : Colors.SeaGreen);
                    options.Resources["RibbonKit.Brushes.ScrollBar.WashAccent"] = scopedWash;
                    Layout();
                    var paintedThumb = Part<Thumb>(Part<ScrollBar>(scroll, "PART_VerticalScrollBar"), "Thumb");
                    var paintedGroup = Assert.IsType<DrawingGroup>(Assert.IsType<DrawingBrush>(Part<Border>(paintedThumb, "Pill").Background).Drawing);
                    var paintedWash = Assert.IsType<GeometryDrawing>(Assert.IsType<DrawingGroup>(paintedGroup.Children[1]).Children[0]);
                    Assert.Equal(scopedWash.Color, Assert.IsType<SolidColorBrush>(paintedWash.Brush).Color);
                    options.Resources.Remove("RibbonKit.Brushes.ScrollBar.WashAccent");
                    NativeClick(Part<RepeatButton>(Part<ScrollBar>(scroll, "PART_VerticalScrollBar"), "IncreaseButton"));
                    Layout();
                    Assert.True(scroll.VerticalOffset > 0);
                }
            }
            finally { options.Close(); }
        }
        finally
        {
            window.Close();
            RibbonAnimation.GlobalLevel = animation;
            ThemeManager.SetDarkMode(application, false);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
        }

        void Layout()
        {
            application.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            window.UpdateLayout();
            application.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        }
    }

    private static void CheckRim(ButtonBase button, double opacity)
    {
        var rim = Part<Border>(button, "CrystalUtilityRim");
        Assert.False(rim.IsHitTestVisible);
        Assert.Equal(0, rim.Opacity);
        var before = button.RenderSize;
        SetState(button, typeof(UIElement), "IsMouseOverPropertyKey", true);
        Assert.Equal(opacity, rim.Opacity);
        Assert.Same(button.FindResource("RibbonKit.Brushes.Control.HoverBorder"), rim.BorderBrush);
        SetState(button, typeof(ButtonBase), "IsPressedPropertyKey", true);
        Assert.Same(button.FindResource("RibbonKit.Brushes.Control.PressedBorder"), rim.BorderBrush);
        Assert.Equal(opacity, rim.Opacity);
        Assert.Equal(before, button.RenderSize);
        SetState(button, typeof(ButtonBase), "IsPressedPropertyKey", false);
        SetState(button, typeof(UIElement), "IsMouseOverPropertyKey", false);
    }

    private static void CheckScrollBar(ScrollViewer viewer, Orientation orientation)
    {
        var bar = Part<ScrollBar>(viewer, orientation == Orientation.Vertical ? "PART_VerticalScrollBar" : "PART_HorizontalScrollBar");
        bar.ApplyTemplate();
        Assert.Equal("RibbonKit.Controls.RibbonScrollBarTrack", Assert.IsAssignableFrom<Track>(bar.Template.FindName("PART_Track", bar)).GetType().FullName);
        Assert.Equal(14, orientation == Orientation.Vertical ? bar.Width : bar.Height);
        Assert.Equal(new CornerRadius(4), RibbonScrollBar.GetThumbCornerRadius(bar));
        Assert.Equal(new CornerRadius(4), RibbonScrollBar.GetButtonCornerRadius(bar));
        Assert.Equal(new CornerRadius(4), RibbonScrollBar.GetRailCornerRadius(bar));
        var thumb = Part<Thumb>(bar, "Thumb");
        var paint = Assert.IsType<DrawingBrush>(Part<Border>(thumb, "Pill").Background);
        var drawing = Assert.IsType<DrawingGroup>(paint.Drawing);
        Assert.Equal(Assert.IsType<SolidColorBrush>(bar.FindResource("RibbonKit.Brushes.Control.SurfaceBackground")).Color,
            Assert.IsType<SolidColorBrush>(Assert.IsType<GeometryDrawing>(drawing.Children[0]).Brush).Color);
        Assert.Equal(15 / 255d, Assert.IsType<DrawingGroup>(drawing.Children[1]).Opacity);
        Assert.Equal(new Thickness(1), thumb.BorderThickness);
        SetState(thumb, typeof(UIElement), "IsMouseOverPropertyKey", true);
        Assert.Equal(20 / 255d, Assert.IsType<DrawingGroup>(Assert.IsType<DrawingGroup>(Assert.IsType<DrawingBrush>(thumb.Background).Drawing).Children[1]).Opacity);
        SetState(thumb, typeof(Thumb), "IsDraggingPropertyKey", true);
        Assert.Equal(28 / 255d, Assert.IsType<DrawingGroup>(Assert.IsType<DrawingGroup>(Assert.IsType<DrawingBrush>(thumb.Background).Drawing).Children[1]).Opacity);
        SetState(thumb, typeof(Thumb), "IsDraggingPropertyKey", false);
        SetState(thumb, typeof(UIElement), "IsMouseOverPropertyKey", false);
        thumb.Background = Brushes.Magenta;
        Assert.Same(Brushes.Magenta, Part<Border>(thumb, "Pill").Background);
        SetState(thumb, typeof(UIElement), "IsMouseOverPropertyKey", true);
        Assert.Same(Brushes.Magenta, Part<Border>(thumb, "Pill").Background);
        SetState(thumb, typeof(UIElement), "IsMouseOverPropertyKey", false);
        thumb.ClearValue(Control.BackgroundProperty);
    }

    private static T Part<T>(Control owner, string name) where T : FrameworkElement
    {
        owner.ApplyTemplate();
        return Assert.IsType<T>(owner.Template.FindName(name, owner));
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
    private static void SetState(DependencyObject control, Type type, string name, bool value) =>
        control.SetValue(Assert.IsType<DependencyPropertyKey>(type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)), value);
    private static void NativeClick(ButtonBase button) => typeof(ButtonBase).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, null);
}
