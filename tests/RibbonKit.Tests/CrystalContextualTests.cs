using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using Xunit;

namespace RibbonKit.Tests;

public class CrystalContextualTests
{
    [Fact]
    public void Crystal_runtime_tokens_style_shared_button_without_showcase_resources() => Sta.Run(() =>
    {
        var host = new StackPanel();
        var office = new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) };
        var crystal = new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Crystal.Light.xaml", UriKind.Relative) };
        foreach (object key in office.Keys)
            Assert.NotNull(crystal[key]);

        host.Resources.MergedDictionaries.Add(crystal);
        var button = new RibbonButton { Header = "Crystal" };
        host.Children.Add(button);
        host.Measure(new Size(200, 100));
        host.Arrange(new Rect(0, 0, 200, 100));
        host.UpdateLayout();
        var hover = Assert.IsType<Border>(button.Template.FindName("HoverWash", button));
        Assert.Same(crystal["RibbonKit.Brushes.Control.HoverBackground"], hover.Background);
        Assert.Same(crystal["RibbonKit.Brushes.Control.HoverBorder"], hover.BorderBrush);
        Assert.Equal(new CornerRadius(8), hover.CornerRadius);
        Assert.IsType<DrawingBrush>(hover.BorderBrush);
    });

    [Fact]
    public void Crystal_customize_tree_opens_without_missing_ancestor_bindings() => Sta.Run(() =>
    {
        var window = new CrystalPreviewWindow { Width = 760, Height = 480,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            Sta.Drain();
            var dialog = window.CreateCustomizationDialog(false);
            try
            {
                var page = Assert.IsType<RibbonCustomizePage>(dialog.SelectedPage!.Content);
                page.ApplyTemplate();
                var tree = Assert.IsType<TreeView>(page.Template.FindName("PART_Tree", page));
                var treeStyle = Assert.IsType<Style>(dialog.FindResource("Crystal.Customize.TreeContainer"));
                Assert.Same(treeStyle, tree.ItemContainerStyle);
                Assert.Same(treeStyle, tree.Resources[typeof(TreeViewItem)]);
                foreach (var property in new[] { Control.ForegroundProperty,
                    Control.HorizontalContentAlignmentProperty, Control.VerticalContentAlignmentProperty })
                    Assert.DoesNotContain(treeStyle.Setters.OfType<Setter>(), setter =>
                        setter.Property == property && setter.Value is BindingBase);
                Sta.Drain();
                dialog.Show();
                Sta.Drain();
                dialog.UpdateLayout();
                var row = Assert.IsType<TreeViewItem>(tree.ItemContainerGenerator.ContainerFromIndex(0));
                Assert.Same(treeStyle, row.Style);
                foreach (var property in new[] { Control.ForegroundProperty,
                    Control.HorizontalContentAlignmentProperty, Control.VerticalContentAlignmentProperty })
                    Assert.Null(BindingOperations.GetBindingBase(row, property));
                row.IsExpanded = true;
                Sta.Drain();
                var child = Assert.IsType<TreeViewItem>(row.ItemContainerGenerator.ContainerFromIndex(0));
                Assert.Same(treeStyle, child.Style);
                foreach (var property in new[] { Control.ForegroundProperty,
                    Control.HorizontalContentAlignmentProperty, Control.VerticalContentAlignmentProperty })
                    Assert.Null(BindingOperations.GetBindingBase(child, property));
            }
            finally { dialog.Close(); }
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Crystal_document_length_toggle_provides_scrollable_sample_text() => Sta.Run(() =>
    {
        var window = new CrystalPreviewWindow { Width = 760, Height = 480,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            Sta.Drain();
            var viewer = (ScrollViewer)window.FindName("CrystalDocumentScroll");
            var extra = (ItemsControl)window.FindName("DocumentScrollDemo");
            var toggle = (RibbonMenuItem)window.FindName("DocumentLengthToggle");
            double originalExtent = viewer.ExtentHeight;
            Assert.Equal(Visibility.Collapsed, extra.Visibility);
            Assert.NotEmpty(extra.Items);

            toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Sta.Drain();
            window.UpdateLayout();
            Assert.Equal(Visibility.Visible, extra.Visibility);
            Assert.True(viewer.ExtentHeight > originalExtent + 200);
            Assert.Equal("Remove scrolling text", toggle.Header);
            viewer.ScrollToBottom();
            Sta.Drain();
            Assert.True(viewer.VerticalOffset > 0);

            toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Sta.Drain();
            window.UpdateLayout();
            Assert.Equal(Visibility.Collapsed, extra.Visibility);
            Assert.Equal(originalExtent, viewer.ExtentHeight, 1);
            Assert.Equal("Add scrolling text", toggle.Header);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Crystal_document_edge_fade_tracks_scrolling_toggle_and_comparison() => Sta.Run(() =>
    {
        var window = new CrystalPreviewWindow { Width = 760, Height = 480,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            Sta.Drain();
            var viewer = (ScrollViewer)window.FindName("CrystalDocumentScroll");
            var ribbon = (Ribbon)window.FindName("PreviewRibbon");
            var qat = Assert.IsType<Border>(ribbon.Template.FindName("QatBelowHost", ribbon));
            var scrollBar = Assert.IsType<ScrollBar>(
                viewer.Template.FindName("PART_VerticalScrollBar", viewer));
            var presenter = Assert.IsType<ScrollContentPresenter>(
                viewer.Template.FindName("PART_ScrollContentPresenter", viewer));
            Assert.True(viewer.Margin.Top < -qat.ActualHeight);
            Assert.Equal(-viewer.Margin.Top, scrollBar.Margin.Top, 1);
            Assert.InRange(Math.Abs(viewer.TranslatePoint(new Point(), window).Y -
                qat.TranslatePoint(new Point(), window).Y), 0, 1);
            Assert.IsType<LinearGradientBrush>(presenter.OpacityMask);
            Assert.True(viewer.ScrollableHeight > 30);
            viewer.ScrollToVerticalOffset(30);
            Sta.Drain();
            Assert.True(viewer.VerticalOffset > 0);
            var fade = Assert.IsType<LinearGradientBrush>(presenter.OpacityMask);
            Assert.Equal(BrushMappingMode.Absolute, fade.MappingMode);
            Assert.Equal(qat.ActualHeight + 24, fade.EndPoint.Y, 1);
            Assert.Equal(3, fade.GradientStops.Count);
            Assert.Equal(qat.ActualHeight / fade.EndPoint.Y, fade.GradientStops[1].Offset, 2);
            Assert.InRange(fade.GradientStops[0].Color.A, (byte)1, (byte)254);

            var qatToggle = (RibbonMenuItem)window.FindName("DocumentQatUnderlayToggle");
            qatToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Sta.Drain();
            Assert.Equal(new Thickness(), viewer.Margin);
            Assert.Equal(new Thickness(), scrollBar.Margin);
            Assert.Equal(24, Assert.IsType<LinearGradientBrush>(presenter.OpacityMask).EndPoint.Y);
            qatToggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Sta.Drain();
            Assert.True(viewer.Margin.Top < -qat.ActualHeight);

            var toggle = (RibbonMenuItem)window.FindName("DocumentFadeToggle");
            toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Null(presenter.OpacityMask);
            Assert.True(viewer.VerticalOffset > 0);
            toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(qat.ActualHeight + 24,
                Assert.IsType<LinearGradientBrush>(presenter.OpacityMask).EndPoint.Y, 1);

            var compare = (RibbonToggleButton)window.FindName("CompareToggle");
            compare.IsChecked = true;
            Assert.Null(presenter.OpacityMask);
            Assert.False(toggle.IsEnabled);
            Assert.False(qatToggle.IsEnabled);
            Assert.Equal(new Thickness(), viewer.Margin);
            Assert.Equal(new Thickness(), scrollBar.Margin);
            compare.IsChecked = false;
            Sta.Drain();
            Assert.True(viewer.Margin.Top < -qat.ActualHeight);
            Assert.NotNull(presenter.OpacityMask);
            Assert.True(toggle.IsEnabled);
            Assert.True(qatToggle.IsEnabled);

            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            Sta.Drain();
            Assert.Equal(new Thickness(), viewer.Margin);
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            Sta.Drain();
            Assert.True(viewer.Margin.Top < -qat.ActualHeight);

            var previousMotion = RibbonKit.Animation.RibbonAnimation.GetActionOverride(
                RibbonKit.Animation.RibbonAnimationAction.MessageBar);
            RibbonKit.Animation.RibbonAnimation.SetActionLevel(
                RibbonKit.Animation.RibbonAnimationAction.MessageBar,
                RibbonKit.Animation.RibbonAnimationLevel.None);
            try
            {
                var message = (RibbonMessage)window.FindName("CrystalProtectedMessage");
                message.IsOpen = true;
                Sta.Drain();
                Assert.Equal(new Thickness(), viewer.Margin);
                message.IsOpen = false;
                Sta.Drain();
                Assert.True(viewer.Margin.Top < -qat.ActualHeight);
            }
            finally
            {
                if (previousMotion is { } level)
                    RibbonKit.Animation.RibbonAnimation.SetActionLevel(
                        RibbonKit.Animation.RibbonAnimationAction.MessageBar, level);
                else RibbonKit.Animation.RibbonAnimation.ClearActionLevel(
                    RibbonKit.Animation.RibbonAnimationAction.MessageBar);
            }

            viewer.ScrollToTop();
            Sta.Drain();
            Assert.IsType<LinearGradientBrush>(presenter.OpacityMask);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Crystal_popup_backdrop_blurs_a_real_menu_without_blurring_its_items() => Sta.Run(() =>
    {
        var canvas = new Canvas { Width = 280, Height = 240, Background = Brushes.White };
        canvas.Children.Add(new Border { Width = 140, Height = 240, Background = Brushes.Black });
        var button = new RibbonDropDownButton { Header = "Arrange", Width = 120, Height = 36 };
        button.Items.Add(new RibbonMenuItem { Header = "Bring to front" });
        Canvas.SetLeft(button, 80);
        Canvas.SetTop(button, 20);
        canvas.Children.Add(button);
        var window = new Window { Content = canvas, SizeToContent = SizeToContent.WidthAndHeight,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        window.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        var backdrop = new CrystalPopupBackdrop(window, button, "PART_MenuHost");
        try
        {
            window.Show();
            backdrop.Apply(true);
            button.IsDropDownOpen = true;
            Sta.Drain();
            window.UpdateLayout();
            Sta.Drain();
            var popup = Assert.IsType<Popup>(button.Template.FindName("PART_Popup", button));
            Assert.True(popup.IsOpen);
            var host = Assert.IsType<Border>(button.Template.FindName("PART_MenuHost", button));
            var material = Assert.IsType<DrawingBrush>(host.Background);
            var drawing = Assert.IsType<DrawingGroup>(material.Drawing);
            var image = Assert.IsType<ImageDrawing>(drawing.Children[1]);
            var crop = Assert.IsType<System.Windows.Media.Imaging.CroppedBitmap>(image.ImageSource);
            Assert.True(crop.PixelWidth > 70);
            Assert.NotNull(host.Child); // The native menu items stay above the blurred background.

            // Use fixed owner coordinates for pixel evidence: WPF can clamp a popup
            // from an offscreen test window onto a different monitor.
            var dpi = VisualTreeHelper.GetDpi(canvas);
            var fixedCrop = Assert.IsType<System.Windows.Media.Imaging.CroppedBitmap>(
                CrystalPopupBackdrop.CaptureBlurred(canvas, new Rect(80, 80, 120, 80), dpi));
            int edgePixel = (int)Math.Round(60 * dpi.DpiScaleX);
            var pixel = new byte[4];
            fixedCrop.CopyPixels(new Int32Rect(edgePixel, fixedCrop.PixelHeight / 2, 1, 1), pixel, 4, 0);
            Assert.InRange(pixel[0], 1, 254); // A softened black/white boundary.
            Assert.Equal(pixel[0], pixel[2]);

            backdrop.Apply(false);
            Assert.NotSame(material, host.Background);
            Assert.Same(window.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), host.Background);
        }
        finally
        {
            button.IsDropDownOpen = false;
            backdrop.Remove();
            window.Close();
        }
    });

    [Fact]
    public void Crystal_preview_frosts_dropdown_and_collapsed_group_then_restores_comparison() => Sta.Run(() =>
    {
        var window = new CrystalPreviewWindow { Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            Sta.Drain();
            var arrange = (RibbonDropDownButton)window.FindName("ArrangeButton");
            arrange.IsDropDownOpen = true;
            Sta.Drain();
            var menuHost = (Border)arrange.Template.FindName("PART_MenuHost", arrange);
            var blueMenu = Assert.IsType<DrawingBrush>(menuHost.Background);
            var accent = (RibbonDropDownButton)window.FindName("AccentSelector");
            ((RibbonMenuItem)accent.Items[1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Sta.Drain();
            var greenMenu = Assert.IsType<DrawingBrush>(menuHost.Background);
            Assert.NotSame(blueMenu, greenMenu);
            var layers = Assert.IsType<DrawingGroup>(greenMenu.Drawing).Children;
            var fallback = Assert.IsType<GeometryDrawing>(layers[0]);
            Assert.Same(window.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), fallback.Brush);
            var tint = Assert.IsType<GeometryDrawing>(layers[2]);
            var popupTint = Assert.IsType<LinearGradientBrush>(tint.Brush);
            Assert.Same(window.FindResource("Crystal.Brushes.FrostedFrame"), popupTint);
            arrange.IsDropDownOpen = false;
            Sta.Drain();
            Assert.Same(window.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), menuHost.Background);

            window.Width = 420;
            Sta.Drain();
            window.UpdateLayout();
            var home = (RibbonTab)window.FindName("HomeTab");
            var group = home.Groups.First(g => g.SizeState == RibbonGroupSizeState.Collapsed);
            var toggle = (ToggleButton)group.Template.FindName("PART_CollapsedButton", group);
            toggle.IsChecked = true;
            Sta.Drain();
            var groupHost = (Border)group.Template.FindName("PART_PopupHost", group);
            var groupMaterial = Assert.IsType<DrawingBrush>(groupHost.Background);
            var groupTint = Assert.IsType<LinearGradientBrush>(Assert.IsType<GeometryDrawing>(
                Assert.IsType<DrawingGroup>(groupMaterial.Drawing).Children[2]).Brush);
            Assert.Same(popupTint, groupTint);
            Assert.NotNull(groupHost.Child);

            ((RibbonToggleButton)window.FindName("CompareToggle")).IsChecked = true;
            Sta.Drain();
            Assert.Same(window.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), groupHost.Background);
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Crystal_menu_backdrop_blurs_background_without_capturing_foreground() => Sta.Run(() =>
    {
        var scene = new Canvas { Width = 240, Height = 180, Background = Brushes.White };
        var dark = new Border { Width = 120, Height = 180, Background = Brushes.Black };
        scene.Children.Add(dark);
        var scroller = new ScrollViewer { Width = 10, Height = 50, Content = new Border { Height = 400 } };
        Canvas.SetLeft(scroller, 225);
        scene.Children.Add(scroller);
        var frame = new Border { Background = Brushes.Red, CornerRadius = new CornerRadius(14) };
        var wrapper = new Grid();
        wrapper.Children.Add(new Grid()); // Shadow slot.
        wrapper.Children.Add(frame);
        var host = new Border { Width = 180, Height = 120, Child = wrapper };
        Canvas.SetLeft(host, 30);
        Canvas.SetTop(host, 30);
        scene.Children.Add(host);
        var window = new Window { Content = scene, SizeToContent = SizeToContent.WidthAndHeight,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        CrystalMenuBackdrop? backdrop = null;
        try
        {
            window.Show();
            window.UpdateLayout();
            int visibilityChanges = 0;
            frame.IsVisibleChanged += (_, _) => visibilityChanges++;
            backdrop = new CrystalMenuBackdrop(scene, host, frame, wrapper);
            Layout();
            var image = Assert.IsType<Image>(backdrop.Layer.Children[0]);
            Assert.NotNull(image.Source);
            Assert.False(backdrop.Layer.IsHitTestVisible);
            Assert.Null(frame.Effect);
            Assert.Equal(Visibility.Visible, wrapper.Visibility);
            Assert.Equal(1d, wrapper.Opacity);
            Assert.Equal(0, visibilityChanges);
            var rendered = Render(backdrop.Layer);
            // The black/white boundary behind the opaque red foreground becomes a soft ramp.
            var left = Pixel(rendered, 86, 60);
            var right = Pixel(rendered, 94, 60);
            Assert.InRange(left[0], 1, 126);
            Assert.InRange(right[0], 128, 254);
            Assert.Equal(left[0], left[2]); // No red foreground or menu feedback in the capture.
            Assert.Equal(0, Pixel(rendered, 0, 0)[3]); // Rounded clip after the blur.
            Assert.Equal(255, Pixel(Render(frame), 90, 60)[2]);
            Assert.Equal(0, Pixel(Render(frame), 90, 60)[0]);
            var firstSnapshot = image.Source;
            dark.Background = Brushes.Blue;
            scroller.ScrollToVerticalOffset(20);
            Layout();
            Assert.True(scroller.VerticalOffset > 0);
            Assert.NotSame(firstSnapshot, image.Source);
            Assert.Equal(255, Pixel(Render(backdrop.Layer), 30, 60)[0]);
            // Moving/resizing the menu updates the crop, rather than stretching the old snapshot.
            Canvas.SetLeft(host, 60);
            host.Width = 150;
            Layout();
            Assert.Equal(new Rect(frame.RenderSize), Assert.IsType<RectangleGeometry>(backdrop.Layer.Clip).Rect);
            Assert.True(Pixel(Render(backdrop.Layer), 100, 60)[2] > 245);
            host.Visibility = Visibility.Hidden;
            Layout();
            Assert.Null(image.Source);
            host.Visibility = Visibility.Visible;
            Layout();
            Assert.NotNull(image.Source);
            backdrop.Remove();
            Assert.Null(image.Source);
            Assert.False(wrapper.Children.Contains(backdrop.Layer));
        }
        finally { backdrop?.Remove(); window.Close(); }
        void Layout() { window.UpdateLayout(); Sta.Drain(); window.UpdateLayout(); }
        static System.Windows.Media.Imaging.RenderTargetBitmap Render(FrameworkElement element)
        {
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)element.ActualWidth,
                (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(element);
            return bitmap;
        }
        static byte[] Pixel(System.Windows.Media.Imaging.BitmapSource bitmap, int x, int y)
        {
            var pixel = new byte[4];
            bitmap.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
            return pixel;
        }
    });

    [Fact]
    public void Crystal_keytip_glass_tracks_window_palette_and_restores_baseline() => Sta.Run(() =>
    {
        var target = new Button { Content = "Paste", Width = 80, Height = 40 };
        var decorator = new AdornerDecorator { Child = target };
        var window = new Window { Content = decorator, Width = 300, Height = 200,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        window.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        KeyTipAdorner? adorner = null;
        try
        {
            window.Show();
            adorner = new KeyTipAdorner(target, "P");
            decorator.AdornerLayer.Add(adorner);
            Layout();
            var badge = (Border)VisualTreeHelper.GetChild(adorner, 0);
            var label = (TextBlock)badge.Child;
            Assert.Equal("P", label.Text);
            Assert.False(adorner.IsHitTestVisible);
            Assert.Equal(new CornerRadius(3), badge.CornerRadius);
            var blue = Assert.IsType<DrawingBrush>(badge.Background);
            Assert.Same(window.FindResource("RibbonKit.Brushes.KeyTip.Border"), badge.BorderBrush);
            Assert.Same(window.FindResource("RibbonKit.Brushes.KeyTip.Foreground"), label.Foreground);
            var originalSize = badge.RenderSize;
            var originalTextColor = ((SolidColorBrush)label.Foreground).Color;

            window.Resources.MergedDictionaries[1] = CrystalPalette.Create(Colors.Purple);
            Layout();
            Assert.NotSame(blue, badge.Background);
            Assert.Same(window.FindResource("RibbonKit.Brushes.KeyTip.Background"), badge.Background);
            Assert.Same(window.FindResource("RibbonKit.Brushes.KeyTip.Border"), badge.BorderBrush);
            Assert.Equal(originalTextColor, ((SolidColorBrush)label.Foreground).Color);
            Assert.Equal(originalSize, badge.RenderSize);
            adorner.Dimmed = true;
            Assert.Equal(0.3, adorner.Opacity);
            adorner.Dimmed = false;
            Assert.Equal(1d, adorner.Opacity);

            window.Resources.MergedDictionaries.RemoveAt(1);
            Layout();
            Assert.IsType<SolidColorBrush>(badge.Background);
            Assert.Same(window.FindResource("RibbonKit.Brushes.KeyTip.Background"), badge.Background);
            Assert.Same(window.FindResource("RibbonKit.Brushes.KeyTip.Border"), badge.BorderBrush);
            Assert.Equal(originalSize, badge.RenderSize);
        }
        finally
        {
            if (adorner != null) decorator.AdornerLayer.Remove(adorner);
            window.Close();
        }
        void Layout() { Sta.Drain(); window.UpdateLayout(); }
    });

    [Fact]
    public void Crystal_below_ribbon_quick_access_drawer_restores_baseline() => Sta.Run(() =>
    {
        // Initialize WPF resource handling before loading pack resources in an isolated run.
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon };
        var templates = new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Office2024.xaml", UriKind.Relative) };
        ribbon.Style = (Style)templates[typeof(Ribbon)];
        ribbon.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        ribbon.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        var tab = new RibbonTab { Header = "A very long tab name lorem ipsum abcdefghijklmn" };
        var group = new RibbonGroup { Header = "Commands" };
        group.Items.Add(new RibbonButton { Header = "Paste" });
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        var context = new CrystalContextualTab { Header = "Picture", IsContextual = true, ContextualColor = Brushes.Teal };
        context.Groups.Add(new RibbonGroup { Header = "Picture commands" });
        ribbon.Tabs.Add(context);
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Copy", Size = RibbonControlSize.Small });
        var window = new Window { Content = ribbon, Width = 800, Height = 300,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            Layout();
            var tabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
            var body = (Border)tabs.Template.FindName("ContentHost", tabs);
            var originalShadow = body.Effect;
            CrystalQuickAccess.Apply(ribbon, true);
            Layout();
            var panel = (Border)ribbon.Template.FindName("QatBelowHost", ribbon);
            Assert.Equal(new CornerRadius(14), body.CornerRadius);
            Assert.Equal(new CornerRadius(0, 0, 10, 10), panel.CornerRadius);
            Assert.Equal(new Thickness(1, 0, 1, 1), panel.BorderThickness);
            Assert.Same(ribbon.FindResource("RibbonKit.Brushes.Tab.HoverBackground"), panel.Background);
            Assert.Same(ribbon.FindResource("Crystal.Brushes.QuickAccessBorder"), panel.BorderBrush);
            var qatRim = Assert.IsType<DrawingBrush>(panel.BorderBrush);
            var hoverRim = Assert.IsType<DrawingBrush>(ribbon.FindResource("RibbonKit.Brushes.Tab.HoverBorder"));
            Assert.Equal(new Point(0.5, 1), qatRim.RelativeTransform.Transform(new Point(0.5, 0)));
            Assert.True(hoverRim.RelativeTransform.Value.IsIdentity);
            Assert.Equal(hoverRim.Opacity, qatRim.Opacity);
            Assert.Same(originalShadow, body.Effect);
            Assert.False(tabs.Resources.Contains("RibbonKit.Effects.ContentShadow"));
            Assert.True(Panel.GetZIndex(tabs) > Panel.GetZIndex(panel));
            Assert.Same(ribbon.FindResource("Crystal.Effects.QuickAccessShadow"), panel.Effect);
            Assert.Equal(0d, ((System.Windows.Media.Effects.DropShadowEffect)panel.Effect).ShadowDepth);
            Assert.Equal(body.ActualWidth - 32, panel.ActualWidth, 1);
            Assert.Equal(body.TranslatePoint(new Point(), ribbon).X + 16, panel.TranslatePoint(new Point(), ribbon).X, 1);
            Assert.InRange(panel.TranslatePoint(new Point(), ribbon).Y - body.TranslatePoint(new Point(0, body.ActualHeight), ribbon).Y, -0.5, 0.5);
            Assert.Equal(new Thickness(8, 2, 8, 2), panel.Padding);
            var blue = panel.Background;
            var blueRim = panel.BorderBrush;
            ribbon.SelectedTab = context;
            Layout();
            var contextHeader = (Border)context.Template.FindName("HeaderChrome", context);
            Assert.NotSame(contextHeader.Background, panel.Background);
            Assert.Same(blue, panel.Background);
            Assert.Same(blueRim, panel.BorderBrush);
            context.ContextualColor = Brushes.Coral;
            Layout();
            Assert.Same(blue, panel.Background);
            Assert.Same(blueRim, panel.BorderBrush);
            Assert.Same(originalShadow, body.Effect);
            ribbon.Resources.MergedDictionaries[1] = CrystalPalette.Create(Colors.Purple);
            Layout();
            Assert.NotSame(blue, panel.Background);
            Assert.Same(ribbon.FindResource("RibbonKit.Brushes.Tab.HoverBackground"), panel.Background);
            Assert.NotSame(contextHeader.Background, panel.Background);
            ribbon.SelectedTab = tab;
            Layout();
            Assert.Same(ribbon.FindResource("RibbonKit.Brushes.Tab.HoverBackground"), panel.Background);
            Assert.Same(ribbon.FindResource("Crystal.Brushes.QuickAccessBorder"), panel.BorderBrush);
            Assert.Equal(new Point(0.5, 1), panel.BorderBrush.RelativeTransform.Transform(new Point(0.5, 0)));
            Assert.Same(ribbon.FindResource("Crystal.Effects.QuickAccessShadow"), panel.Effect);
            Assert.Same(ribbon.FindResource("RibbonKit.Effects.ContentShadow"), body.Effect);
            ribbon.IsMinimized = true;
            Layout();
            Assert.Equal(new CornerRadius(10), panel.CornerRadius);
            Assert.Equal(new Thickness(1), panel.BorderThickness);
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            Layout();
            Assert.Equal(Visibility.Collapsed, panel.Visibility);
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            ribbon.IsMinimized = false;
            CrystalQuickAccess.Apply(ribbon, false);
            ribbon.Resources.MergedDictionaries.RemoveAt(1);
            Layout();
            Assert.Equal(HorizontalAlignment.Stretch, panel.HorizontalAlignment);
            Assert.Equal(new CornerRadius(0, 0, 8, 8), panel.CornerRadius);
            Assert.Equal(new Thickness(7, 0, 7, 7), panel.Margin);
            Assert.NotNull(panel.Effect);
            Assert.Equal(0, Panel.GetZIndex(tabs));
            Assert.Single(ribbon.QuickAccessItems);
        }
        finally { window.Close(); }
        void Layout() { Sta.Drain(); window.UpdateLayout(); }
    });

    [Fact]
    public void Crystal_preview_enable_options_updates_both_reparented_groups() => Sta.Run(() =>
    {
        var window = new CrystalPreviewWindow { Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            var ribbon = (Ribbon)window.FindName("PreviewRibbon");
            var utilityTabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
            var minimize = (System.Windows.Controls.Primitives.ToggleButton)utilityTabs.Template.FindName("MinimizeToggle", utilityTabs);
            Sta.Drain();
            Assert.NotNull(FindUtilityRim(minimize));
            Assert.Equal(0d, FindUtilityRim(minimize)!.Opacity);
            CheckCrystalMessages(window, ribbon, utilityTabs);
            CheckCrystalScrollBars(window, ribbon);
            CheckCrystalApplicationMenu(window, ribbon);
            Assert.True(window.MinWidth <= 420);
            var home = (RibbonTab)window.FindName("HomeTab");
            var bodyPreview = (RibbonMenuItem)window.FindName("BodyScrollPreviewToggle");
            // A pre-existing fixed group must remain fixed when the preview is disabled.
            home.Groups[0].CanResize = false;
            bodyPreview.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.Width = 540;
            Sta.Drain();
            window.UpdateLayout();
            foreach (var group in home.Groups)
            {
                Assert.False(group.CanResize);
                Assert.Equal(RibbonGroupSizeState.Large, group.SizeState);
            }
            var bodyScroll = (RibbonKit.Layout.RibbonScrollContentHost)utilityTabs.Template.FindName("PART_ContentScroll", utilityTabs);
            Assert.True(bodyScroll.CanScrollRight);
            Assert.True(bodyScroll.ScrollRightCommand.CanExecute(null));
            bodyScroll.SetCurrentValue(RibbonKit.Layout.RibbonScrollContentHost.OffsetProperty, 72d);
            Sta.Drain();
            window.UpdateLayout();
            Sta.Drain();
            Assert.True(bodyScroll.CanScrollLeft);
            int bodyArrowCount = 0;
            var bodyHost = (Border)utilityTabs.Template.FindName("ContentHost", utilityTabs);
            foreach (UIElement child in ((Grid)VisualTreeHelper.GetParent(bodyScroll)).Children)
                if (child is System.Windows.Controls.Primitives.RepeatButton arrow)
                {
                    Assert.NotNull(FindUtilityRim(arrow));
                    var chrome = (Border)arrow.Template.FindName("Chrome", arrow);
                    Assert.Same(window.FindResource("RibbonKit.Brushes.TabStrip.ControlHoverBackground"), chrome.Background);
                    var bodyRadius = (CornerRadius)window.FindResource("Crystal.Metrics.BodyScrollCornerRadius");
                    Assert.Equal(32d, arrow.Width);
                    Assert.Equal(bodyRadius, chrome.CornerRadius);
                    Assert.Equal(bodyRadius, FindUtilityRim(arrow)!.CornerRadius);
                    Assert.True(chrome.ActualWidth > bodyRadius.TopLeft + bodyRadius.TopRight);
                    // The nominal 3-DIP inset stays inside the outline. Layout rounding
                    // may distribute one extra physical pixel to the trailing edge.
                    var arrowBounds = chrome.TransformToAncestor(bodyHost).TransformBounds(new Rect(chrome.RenderSize));
                    var dpi = VisualTreeHelper.GetDpi(bodyHost);
                    Assert.InRange(Math.Abs(arrowBounds.Top - 3), 0, 1 / dpi.DpiScaleY);
                    Assert.InRange(Math.Abs(bodyHost.ActualHeight - arrowBounds.Bottom - 3), 0, 1 / dpi.DpiScaleY);
                    if (arrow.HorizontalAlignment == HorizontalAlignment.Left)
                        Assert.InRange(Math.Abs(arrowBounds.Left - 3), 0, 1 / dpi.DpiScaleX);
                    else
                        Assert.InRange(Math.Abs(bodyHost.ActualWidth - arrowBounds.Right - 3), 0, 1 / dpi.DpiScaleX);
                    CrystalUtilityChrome.Apply(window, false);
                    Assert.Equal(22d, arrow.Width);
                    Assert.Equal((CornerRadius)window.FindResource("RibbonKit.Metrics.ControlCornerRadius"), chrome.CornerRadius);
                    CrystalUtilityChrome.Apply(window, true);
                    Assert.Equal(32d, arrow.Width);
                    Assert.Equal(bodyRadius, chrome.CornerRadius);
                    bodyArrowCount++;
                }
            Assert.Equal(2, bodyArrowCount);
            bodyPreview.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.False(home.Groups[0].CanResize);
            for (int i = 1; i < home.Groups.Count; i++) Assert.True(home.Groups[i].CanResize);
            home.Groups[0].CanResize = true;
            Sta.Drain();
            window.UpdateLayout();
            Assert.Contains(home.Groups, group => group.SizeState != RibbonGroupSizeState.Large);
            window.Width = 1080;
            Sta.Drain();
            window.UpdateLayout();
            int originalTabCount = ribbon.Tabs.Count;
            int originalQatCount = ribbon.QuickAccessItems.Count;
            var originalPosition = ribbon.QuickAccessPosition;
            double originalQatWidth = ribbon.QuickAccessMaxWidth;
            var scrollPreview = (RibbonMenuItem)window.FindName("ScrollPreviewToggle");
            var overflowPreview = (RibbonMenuItem)window.FindName("OverflowPreviewToggle");
            scrollPreview.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Sta.Drain();
            window.UpdateLayout();
            var tabScroll = (RibbonKit.Layout.RibbonScrollContentHost)utilityTabs.Template.FindName("PART_TabScroll", utilityTabs);
            Assert.True(tabScroll.CanScrollRight);
            window.Width = 540;
            Sta.Drain();
            window.UpdateLayout();
            Assert.InRange(window.ActualWidth, 420, 550);
            Assert.True(ribbon.Tabs.Count > originalTabCount);
            Assert.True(tabScroll.CanScrollRight);
            Assert.True(tabScroll.ScrollRightCommand.CanExecute(null));
            tabScroll.SetCurrentValue(RibbonKit.Layout.RibbonScrollContentHost.OffsetProperty, 72d);
            Sta.Drain();
            window.UpdateLayout();
            Sta.Drain();
            Assert.True(tabScroll.CanScrollLeft);
            int arrowCount = 0;
            foreach (UIElement child in ((Grid)VisualTreeHelper.GetParent(tabScroll)).Children)
                if (child is System.Windows.Controls.Primitives.RepeatButton arrow)
                {
                    Assert.NotNull(FindUtilityRim(arrow));
                    var arrowChrome = (Border)arrow.Template.FindName("Chrome", arrow);
                    Assert.Equal(22d, arrow.Width);
                    Assert.Equal((CornerRadius)window.FindResource("RibbonKit.Metrics.ControlCornerRadius"), arrowChrome.CornerRadius);
                    Assert.Same(window.FindResource("RibbonKit.Brushes.TabStrip.ControlHoverBackground"),
                        arrowChrome.Background);
                    CrystalUtilityChrome.Apply(window, false);
                    Assert.Same(arrow.Background, arrowChrome.Background);
                    CrystalUtilityChrome.Apply(window, true);
                    Assert.Same(window.FindResource("RibbonKit.Brushes.TabStrip.ControlHoverBackground"),
                        arrowChrome.Background);
                    arrowCount++;
                }
            Assert.Equal(2, arrowCount);
            scrollPreview.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(originalTabCount, ribbon.Tabs.Count);
            window.Width = 1080;
            overflowPreview.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Sta.Drain();
            window.UpdateLayout();
            var qat = (RibbonQuickAccessToolBar)utilityTabs.Template.FindName("QatTabRowHost", utilityTabs);
            Assert.True(qat.HasOverflow);
            Assert.Equal(originalQatCount + 4, ribbon.QuickAccessItems.Count);
            var overflowButton = qat.OverflowButton!;
            Assert.NotNull(FindUtilityRim(overflowButton));
            overflowButton.IsChecked = true;
            Sta.Drain();
            var utilityRim = FindUtilityRim(overflowButton)!;
            Assert.Equal(1d, utilityRim.Opacity);
            Assert.False(utilityRim.IsHitTestVisible);
            Assert.Same(window.FindResource("RibbonKit.Brushes.Control.HoverBorder"), utilityRim.BorderBrush);
            Assert.NotEmpty(qat.OverflowEntries);
            overflowButton.IsChecked = false;
            CrystalUtilityChrome.Apply(window, false);
            Assert.Null(FindUtilityRim(minimize));
            Assert.Null(FindUtilityRim(overflowButton));
            CrystalUtilityChrome.Apply(window, true);
            Assert.NotNull(FindUtilityRim(minimize));
            Assert.NotNull(FindUtilityRim(overflowButton));
            overflowPreview.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(originalQatCount, ribbon.QuickAccessItems.Count);
            Assert.Equal(originalPosition, ribbon.QuickAccessPosition);
            Assert.Equal(originalQatWidth, ribbon.QuickAccessMaxWidth);
            ribbon.SelectedIndex = 2;
            Sta.Drain(DispatcherPriority.Render);
            var toggle = (RibbonToggleButton)window.FindName("EnableOptionsToggle");
            var options = (StackPanel)window.FindName("CrystalOptionsPanel");
            var spacing = (StackPanel)window.FindName("CrystalSpacingPanel");
            toggle.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, false);
            Sta.Drain();
            Assert.False(options.IsEnabled);
            Assert.False(spacing.IsEnabled);
            foreach (UIElement item in options.Children) Assert.False(item.IsEnabled);
            foreach (UIElement item in spacing.Children) Assert.False(item.IsEnabled);
            toggle.SetCurrentValue(System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty, true);
            Sta.Drain();
            Assert.True(options.IsEnabled);
            Assert.True(spacing.IsEnabled);
            var dialog = window.CreateCustomizationDialog(false);
            try
            {
                dialog.Show();
                Sta.Drain();
                dialog.UpdateLayout();
                Assert.Same(window, dialog.Owner);
                Assert.Equal(2, dialog.Pages.Count);
                Assert.Same(dialog.Pages[0], dialog.SelectedPage);
                var selectedNavigation = dialog.Pages[0];
                Assert.Equal(Visibility.Visible,
                    ((Rectangle)selectedNavigation.Template.FindName("CrystalMarker", selectedNavigation)).Visibility);
                Assert.Same(dialog.FindResource("RibbonKit.Brushes.Tab.SelectedBackground"),
                    ((Border)selectedNavigation.Template.FindName("Chrome", selectedNavigation)).Background);
                foreach (var name in new[] { "PART_OkButton", "PART_CancelButton" })
                {
                    var action = (Button)dialog.Template.FindName(name, dialog);
                    Assert.Same(action.FindResource("RibbonKit.Brushes.Control.CheckedBackground"),
                        ((Border)action.Template.FindName("Chrome", action)).Background);
                    if (name == "PART_OkButton")
                    {
                        Assert.Same(dialog.FindResource("RibbonKit.Brushes.Text.Primary"), action.Foreground);
                        Assert.NotSame(dialog.FindResource("RibbonKit.Brushes.Control.CheckedBackground"),
                            action.FindResource("RibbonKit.Brushes.Control.CheckedBackground"));
                    }
                }
                var page = Assert.IsType<RibbonCustomizePage>(dialog.SelectedPage!.Content);
                Assert.Same(ribbon, page.Ribbon);
                Assert.False(string.IsNullOrEmpty(page.ResetLayout));
                Assert.NotNull(page.Template);
                var available = (ListBox)page.Template.FindName("PART_AvailableList", page);
                available.SelectedIndex = 0;
                Sta.Drain(DispatcherPriority.Render);
                var row = (ListBoxItem)available.ItemContainerGenerator.ContainerFromIndex(0);
                Assert.Same(dialog.FindResource("RibbonKit.Brushes.Control.CheckedBackground"),
                    ((Border)row.Template.FindName("Row", row)).Background);
                var tree = (TreeView)page.Template.FindName("PART_Tree", page);
                var treeRow = (TreeViewItem)tree.ItemContainerGenerator.ContainerFromIndex(0);
                treeRow.IsSelected = true;
                treeRow.IsExpanded = true;
                Sta.Drain(DispatcherPriority.Render);
                Assert.NotNull(treeRow.Template.FindName("PART_Header", treeRow));
                Assert.Same(dialog.FindResource("RibbonKit.Brushes.Control.CheckedBackground"),
                    ((Border)treeRow.Template.FindName("Row", treeRow)).Background);
                var scrollBar = FindScrollBar(available);
                Assert.NotNull(scrollBar);
                Assert.Equal(14d, scrollBar.Width);
                Assert.Equal(Colors.Transparent, ((SolidColorBrush)scrollBar.Background).Color);
                Assert.IsType<DrawingBrush>(scrollBar.FindResource("RibbonKit.Brushes.ScrollBar.Thumb"));
                Assert.Same(window.FindResource("RibbonKit.Brushes.Window.Background"),
                    dialog.FindResource("RibbonKit.Brushes.Window.Background"));
                dialog.SelectedPage = dialog.Pages[1];
                Sta.Drain();
                Assert.Same(ribbon, Assert.IsType<RibbonQuickAccessPage>(dialog.SelectedPage.Content).Ribbon);
                Assert.Equal(Visibility.Collapsed,
                    ((Rectangle)selectedNavigation.Template.FindName("CrystalMarker", selectedNavigation)).Visibility);
                Assert.Equal(Visibility.Visible,
                    ((Rectangle)dialog.Pages[1].Template.FindName("CrystalMarker", dialog.Pages[1])).Visibility);
            }
            finally { dialog.Close(); }
            var quickAccessDialog = window.CreateCustomizationDialog(true);
            Assert.Same(quickAccessDialog.Pages[1], quickAccessDialog.SelectedPage);
            quickAccessDialog.Close();
        }
        finally { window.Close(); }

        static System.Windows.Controls.Primitives.ScrollBar? FindScrollBar(DependencyObject root)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is System.Windows.Controls.Primitives.ScrollBar bar && bar.Orientation == Orientation.Vertical) return bar;
                if (FindScrollBar(child) is { } found) return found;
            }
            return null;
        }
    });

    [Fact]
    public void Crystal_options_preserve_marks_grouping_and_values_across_tint_and_comparison() => Sta.Run(() =>
    {
        var check = new RibbonCheckBox { Header = "Guides", IsThreeState = true, IsChecked = true };
        var first = new RibbonRadioButton { Header = "Comfortable", GroupName = "Spacing", IsChecked = true };
        var second = new RibbonRadioButton { Header = "Compact", GroupName = "Spacing" };
        var panel = new StackPanel();
        panel.Children.Add(check);
        panel.Children.Add(first);
        panel.Children.Add(second);
        var window = new Window { Content = panel, Width = 300, Height = 200,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        window.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Render);
            var indicator = (Border)check.Template.FindName("Indicator", check);
            Assert.Equal(new CornerRadius(3), indicator.CornerRadius);
            var blue = Assert.IsType<DrawingBrush>(indicator.Background);
            Assert.Equal(Visibility.Visible, ((Path)check.Template.FindName("CheckMark", check)).Visibility);
            check.IsChecked = null;
            Sta.Drain();
            Assert.Equal(Visibility.Visible, ((Rectangle)check.Template.FindName("IndeterminateMark", check)).Visibility);
            Assert.Equal(Visibility.Collapsed, ((Path)check.Template.FindName("CheckMark", check)).Visibility);
            second.IsChecked = true;
            Assert.False(first.IsChecked);
            window.Resources.MergedDictionaries[1] = CrystalPalette.Create(Colors.Purple);
            Sta.Drain();
            indicator = (Border)check.Template.FindName("Indicator", check);
            var purple = Assert.IsType<DrawingBrush>(indicator.Background);
            var blueBody = (LinearGradientBrush)((GeometryDrawing)((DrawingGroup)blue.Drawing).Children[0]).Brush;
            var purpleBody = (LinearGradientBrush)((GeometryDrawing)((DrawingGroup)purple.Drawing).Children[0]).Brush;
            Assert.NotEqual(blueBody.GradientStops[0].Color, purpleBody.GradientStops[0].Color);
            Assert.IsType<DrawingBrush>(((Ellipse)second.Template.FindName("Indicator", second)).Fill);
            Assert.Same(second.FindResource("Crystal.Options.SelectedBorder"),
                ((Ellipse)second.Template.FindName("Indicator", second)).Stroke);
            window.Activate();
            Assert.True(second.Focus());
            Sta.Drain();
            var focusRing = (Ellipse)second.Template.FindName("FocusRing", second);
            Assert.Equal(1d, focusRing.Opacity);
            Assert.False(focusRing.IsHitTestVisible);
            Assert.Equal(Colors.Transparent,
                ((SolidColorBrush)((Border)second.Template.FindName("Chrome", second)).BorderBrush).Color);
            Assert.True(check.Focus());
            Sta.Drain();
            Assert.Equal(1d, ((Border)check.Template.FindName("FocusRing", check)).Opacity);
            Assert.Equal(0d, focusRing.Opacity);
            panel.IsEnabled = false;
            Assert.Equal(0.4, check.Opacity);
            Assert.Equal(0.4, second.Opacity);
            panel.IsEnabled = true;
            window.Resources.MergedDictionaries.RemoveAt(1);
            Sta.Drain();
            Assert.Null(check.IsChecked);
            Assert.True(second.IsChecked);
            Assert.IsType<SolidColorBrush>(((Border)check.Template.FindName("Indicator", check)).Background);
            Assert.IsType<SolidColorBrush>(((Ellipse)second.Template.FindName("Indicator", second)).Fill);
            Assert.Null(check.Template.FindName("FocusRing", check));
        }
        finally { window.Close(); }
    });

    [Fact]
    public void Contextual_marker_tracks_override_replacement_clear_and_color_changes() => Sta.Run(() =>
    {
        var context = new RibbonTab { Header = "Picture", IsContextual = true, ContextualColor = Brushes.Teal };
        var ordinary = new RibbonTab { Header = "Home" };
        var host = Host(context, ordinary);
        using var shown = new ShownHost(host);
        Layout(host);
        var marker = (Rectangle)host.Template.FindName("PART_TabMarker", host);
        Assert.Same(context.ContextualBrush, marker.Fill);

        var glass = new LinearGradientBrush(Colors.White, Colors.Teal, 90);
        context.ContextualSelectionBrush = glass;
        Sta.Drain();
        Assert.Same(glass, marker.Fill);
        Assert.Same(Brushes.Teal, context.ContextualBrush);
        context.ContextualSelectionBrush = Brushes.Violet;
        Sta.Drain();
        Assert.Same(Brushes.Violet, marker.Fill);
        context.ContextualSelectionBrush = null;
        context.ContextualColor = Brushes.Purple;
        Sta.Drain();
        Assert.Same(Brushes.Purple, marker.Fill);

        host.SelectedItem = ordinary;
        Layout(host);
        Assert.Same(Brushes.Blue, marker.Fill);
    });

    [Fact]
    public void Crystal_palette_follows_context_color_and_restores_standard_mode() => Sta.Run(() =>
    {
        var context = new CrystalContextualTab { Header = "Picture", IsContextual = true, ContextualColor = Brushes.Teal };
        var ordinary = new RibbonTab { Header = "Home" };
        var host = Host(context, ordinary);
        host.Resources.Remove("RibbonKit.Brushes.Tab.SelectedUnderline");
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit.Showcase;component/Themes/Crystal.Light.xaml", UriKind.Relative) });
        using var shown = new ShownHost(host);
        Layout(host);
        var marker = (Rectangle)host.Template.FindName("PART_TabMarker", host);
        var teal = Assert.IsType<RadialGradientBrush>(context.Resources["RibbonKit.Brushes.Tab.SelectedBackground"]);
        Assert.IsType<DrawingBrush>(marker.Fill);
        Assert.Same(context.ContextualSelectionBrush, marker.Fill);
        Assert.False(ordinary.Resources.Contains("RibbonKit.Brushes.Tab.SelectedBackground"));

        context.ContextualColor = Brushes.Purple;
        Sta.Drain();
        var purple = Assert.IsType<RadialGradientBrush>(context.Resources["RibbonKit.Brushes.Tab.SelectedBackground"]);
        Assert.NotEqual(teal.GradientStops[2].Color, purple.GradientStops[2].Color);
        Assert.Same(context.ContextualSelectionBrush, marker.Fill);
        var mutableTint = new SolidColorBrush(Colors.Teal);
        context.ContextualColor = mutableTint;
        Sta.Drain();
        var beforeMutation = context.ContextualSelectionBrush;
        mutableTint.Color = Colors.Goldenrod;
        Sta.Drain();
        Assert.NotSame(beforeMutation, context.ContextualSelectionBrush);
        Assert.Same(context.ContextualSelectionBrush, marker.Fill);
        context.ContextualColor = Brushes.Purple;
        context.CrystalEnabled = false;
        Sta.Drain();
        Assert.False(context.Resources.Contains("RibbonKit.Brushes.Tab.SelectedBackground"));
        Assert.Null(context.ContextualSelectionBrush);
        Assert.Same(Brushes.Purple, marker.Fill);
        context.CrystalEnabled = true;
        Sta.Drain();
        Assert.IsType<DrawingBrush>(marker.Fill);
    });

    [Fact]
    public void Crystal_input_styles_are_scoped_and_comparison_preserves_values() => Sta.Run(() =>
    {
        var root = new StackPanel();
        root.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        var crystal = new ResourceDictionary
        { Source = new Uri("/RibbonKit.Showcase;component/Themes/Crystal.Light.xaml", UriKind.Relative) };
        root.Resources.MergedDictionaries.Add(crystal);
        var text = new RibbonTextBox { Text = "Keep my title" };
        var combo = new RibbonComboBox { ItemsSource = new[] { "Segoe UI", "Georgia" }, SelectedIndex = 1 };
        var button = new RibbonButton { Header = "Ordinary button" };
        root.Children.Add(text);
        root.Children.Add(combo);
        root.Children.Add(button);
        root.Measure(new Size(500, 200));
        root.Arrange(new Rect(0, 0, 500, 200));
        root.UpdateLayout();

        var textChrome = (Border)text.Template.FindName("Chrome", text);
        var comboChrome = (Border)combo.Template.FindName("Chrome", combo);
        var buttonChrome = (Border)button.Template.FindName("Chrome", button);
        Assert.IsType<DrawingBrush>(textChrome.Background);
        Assert.IsType<DrawingBrush>(comboChrome.Background);
        Assert.Equal(new CornerRadius(4), textChrome.CornerRadius);
        Assert.Equal(new CornerRadius(4), comboChrome.CornerRadius);
        Assert.Equal(new CornerRadius(8), buttonChrome.CornerRadius);
        Assert.IsType<SolidColorBrush>(root.FindResource("RibbonKit.Brushes.Control.SurfaceBackground"));

        root.Resources.MergedDictionaries.Remove(crystal);
        root.UpdateLayout();
        Sta.Drain();
        Assert.Equal("Keep my title", text.Text);
        Assert.Equal("Georgia", combo.SelectedItem);
        Assert.IsType<SolidColorBrush>(((Border)text.Template.FindName("Chrome", text)).Background);
        Assert.IsType<SolidColorBrush>(((Border)combo.Template.FindName("Chrome", combo)).Background);
    });

    [Fact]
    public void Crystal_accent_switches_live_inputs_without_losing_values_or_original_palette() => Sta.Run(() =>
    {
        const string surface = "RibbonKit.Brushes.Ribbon.ContentBackground";
        var root = new StackPanel();
        root.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        var blue = CrystalPalette.Create(CrystalPalette.Blue);
        root.Resources.MergedDictionaries.Add(blue);
        var text = new RibbonTextBox { Text = "Keep this title" };
        var combo = new RibbonComboBox { ItemsSource = new[] { "Arial", "Georgia" }, SelectedIndex = 1 };
        root.Children.Add(text);
        root.Children.Add(combo);
        void LayoutInputs()
        {
            root.Measure(new Size(500, 200));
            root.Arrange(new Rect(0, 0, 500, 200));
            root.UpdateLayout();
            Sta.Drain();
        }
        LayoutInputs();
        var original = ((LinearGradientBrush)blue[surface]).GradientStops[2].Color;
        static Color InputEdge(Brush brush) => ((RadialGradientBrush)((GeometryDrawing)
            ((DrawingGroup)((DrawingBrush)brush).Drawing).Children[0]).Brush).GradientStops[2].Color;
        Color originalInput = InputEdge(((Border)text.Template.FindName("Chrome", text)).Background);
        foreach (string hex in new[] { "#287E78", "#8659A5", "#AC731C" })
        {
            var tinted = CrystalPalette.Create((Color)ColorConverter.ConvertFromString(hex));
            root.Resources.MergedDictionaries[1] = tinted;
            LayoutInputs();
            Assert.NotEqual(original, ((LinearGradientBrush)tinted[surface]).GradientStops[2].Color);
            Assert.Equal(original, ((LinearGradientBrush)blue[surface]).GradientStops[2].Color);
            var inputBackground = (DrawingBrush)text.FindResource("RibbonKit.Brushes.Control.SurfaceBackground");
            Assert.NotEqual(originalInput, InputEdge(inputBackground));
            Assert.Same(inputBackground, ((Border)text.Template.FindName("Chrome", text)).Background);
            Assert.IsType<DrawingBrush>(((Border)combo.Template.FindName("Chrome", combo)).Background);
            Assert.Equal("Keep this title", text.Text);
            Assert.Equal("Georgia", combo.SelectedItem);
            Assert.Equal(new CornerRadius(4), ((Border)text.Template.FindName("Chrome", text)).CornerRadius);
            // Pure-white reflection stays white in every hue variant.
            var rim = (DrawingGroup)((DrawingBrush)tinted["RibbonKit.Brushes.Control.HoverBorder"]).Drawing;
            var glint = (RadialGradientBrush)((GeometryDrawing)rim.Children[1]).Brush;
            Assert.Equal(Colors.White, glint.GradientStops[0].Color);
            Color foreground = ((SolidColorBrush)tinted["RibbonKit.Brushes.Tab.SelectedForeground"]).Color;
            foreach (var stop in ((RadialGradientBrush)tinted["RibbonKit.Brushes.Tab.SelectedBackground"]).GradientStops)
                Assert.True((Luminance(stop.Color) + 0.05) / (Luminance(foreground) + 0.05) >= 4.5);
        }
        root.Resources.MergedDictionaries.RemoveAt(1);
        LayoutInputs();
        Assert.IsType<SolidColorBrush>(((Border)text.Template.FindName("Chrome", text)).Background);
        root.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        LayoutInputs();
        Assert.Equal(original, ((LinearGradientBrush)root.FindResource(surface)).GradientStops[2].Color);
        Assert.Equal("Keep this title", text.Text);
    });

    [Fact]
    public void Crystal_global_accent_preserves_contextual_tab_tint() => Sta.Run(() =>
    {
        var context = new CrystalContextualTab { Header = "Picture", IsContextual = true, ContextualColor = Brushes.Teal };
        var host = Host(context);
        host.Resources.Remove("RibbonKit.Brushes.Tab.SelectedUnderline");
        host.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        host.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        using var shown = new ShownHost(host);
        Layout(host);
        Color before = ((RadialGradientBrush)context.Resources["RibbonKit.Brushes.Tab.SelectedBackground"]).GradientStops[2].Color;
        host.Resources.MergedDictionaries[1] = CrystalPalette.Create(Colors.Purple);
        context.CrystalEnabled = true;
        Layout(host);
        Assert.Same(Brushes.Teal, context.ContextualBrush);
        Assert.Equal(before, ((RadialGradientBrush)context.Resources["RibbonKit.Brushes.Tab.SelectedBackground"]).GradientStops[2].Color);
        var marker = (Rectangle)host.Template.FindName("PART_TabMarker", host);
        Assert.Same(context.ContextualSelectionBrush, marker.Fill);
    });

    [Fact]
    public void Crystal_backstage_uses_tinted_materials_and_restores_baseline_without_losing_selection() => Sta.Run(() =>
    {
        var stage = new Backstage { Design = RibbonBackstageDesign.Modern };
        stage.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        stage.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        var home = new BackstageTabItem { Header = "Home", Content = new TextBlock { Text = "Document" } };
        var appearance = new BackstageTabItem { Header = "Appearance", Content = new TextBlock { Text = "Tint" } };
        stage.Items.Add(home);
        stage.Items.Add(appearance);
        stage.SelectedItem = appearance;
        void LayoutStage()
        {
            stage.Measure(new Size(800, 500));
            stage.Arrange(new Rect(0, 0, 800, 500));
            stage.UpdateLayout();
            Sta.Drain();
        }
        LayoutStage();
        var nav = (Border)stage.Template.FindName("NavColumn", stage);
        var chrome = (Border)appearance.Template.FindName("Chrome", appearance);
        var original = Assert.IsType<LinearGradientBrush>(nav.Background).GradientStops[2].Color;
        Assert.Same(appearance, stage.SelectedItem);
        Assert.True(appearance.IsSelected);
        Assert.Equal(RibbonBackstageDesign.Modern, Backstage.GetDesign(appearance));
        Assert.IsType<DrawingBrush>(stage.FindResource("RibbonKit.Brushes.Backstage.Modern.ItemSelected"));
        var selection = Assert.IsType<DrawingBrush>(chrome.Background);
        Assert.IsType<DrawingBrush>(Assert.IsType<GeometryDrawing>(selection.Drawing).Brush);
        Assert.Equal(new CornerRadius(4), chrome.CornerRadius);
        stage.Resources.MergedDictionaries[1] = CrystalPalette.Create(Colors.Purple);
        LayoutStage();
        Assert.NotEqual(original, Assert.IsType<LinearGradientBrush>(nav.Background).GradientStops[2].Color);
        Assert.Same(appearance, stage.SelectedItem);
        Assert.Same(stage.FindResource("RibbonKit.Brushes.Backstage.Modern.ItemSelected"), chrome.Background);
        Assert.Same(stage.FindResource("RibbonKit.Brushes.Backstage.ContentBackground"), stage.Background);
        stage.Resources.MergedDictionaries.RemoveAt(1);
        LayoutStage();
        Assert.IsType<SolidColorBrush>(nav.Background);
        Assert.IsType<SolidColorBrush>(chrome.Background);
        Assert.Same(appearance, stage.SelectedItem);
    });

    [Fact]
    public void Crystal_floating_backstage_keeps_document_binding_navigation_and_comparison_when_detached() => Sta.Run(() =>
    {
        var stage = new Backstage { Design = RibbonBackstageDesign.Modern };
        var titleInput = new RibbonTextBox { Text = "A calmer workspace" };
        var title = new TextBlock();
        var home = new BackstageTabItem { Header = "Overview", Content = title };
        var appearance = new BackstageTabItem { Header = "Appearance", Content = new TextBlock { Text = "Tint" } };
        var about = new BackstageTabItem { Header = "About", Placement = BackstageItemPlacement.Bottom };
        stage.Items.Add(home);
        stage.Items.Add(appearance);
        stage.Items.Add(about);
        stage.SelectedItem = home;
        var presentation = new CrystalBackstagePresentation(stage, new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) }, title, titleInput);
        var blue = CrystalPalette.Create(CrystalPalette.Blue);
        presentation.Apply(blue);
        void LayoutStage()
        {
            stage.Measure(new Size(680, 440));
            stage.Arrange(new Rect(0, 0, 680, 440));
            stage.UpdateLayout();
            Sta.Drain();
        }
        LayoutStage();
        Assert.Equal(CrystalBackstageLayout.Sidebar, presentation.Layout);
        Assert.NotNull(stage.Template.FindName("NavColumn", stage));
        var sidebarHome = home.TransformToAncestor(stage).Transform(new Point());
        var sidebarAppearance = appearance.TransformToAncestor(stage).Transform(new Point());
        var sidebarAbout = about.TransformToAncestor(stage).Transform(new Point());
        Assert.Equal(sidebarHome.X, sidebarAppearance.X);
        Assert.True(sidebarAppearance.Y > sidebarHome.Y);
        Assert.True(sidebarAbout.Y > sidebarAppearance.Y + appearance.ActualHeight);
        presentation.SetLayout(CrystalBackstageLayout.Floating);
        LayoutStage();
        Assert.Equal("A calmer workspace", title.Text);
        titleInput.Text = "Changed while File is open";
        Sta.Drain();
        Assert.Equal(titleInput.Text, title.Text);
        var first = home.TransformToAncestor(stage).Transform(new Point());
        var second = appearance.TransformToAncestor(stage).Transform(new Point());
        var third = about.TransformToAncestor(stage).Transform(new Point());
        Assert.Equal(first.Y, second.Y);
        Assert.Equal(first.Y, third.Y);
        Assert.True(first.X < second.X && second.X < third.X);
        Assert.True(third.X + about.ActualWidth <= stage.ActualWidth);
        Assert.Null(stage.Template.FindName("NavColumn", stage));
        int backRequests = 0;
        stage.BackRequested += (_, _) => backRequests++;
        ((Button)stage.Template.FindName("PART_BackButton", stage)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(1, backRequests);
        stage.SelectedItem = appearance;
        var purple = CrystalPalette.Create(Colors.Purple);
        presentation.Apply(purple);
        LayoutStage();
        Assert.Same(appearance, stage.SelectedItem);
        Assert.Same(purple["RibbonKit.Brushes.Window.Background"], stage.FindResource("RibbonKit.Brushes.Window.Background"));
        presentation.SetLayout(CrystalBackstageLayout.Sidebar);
        LayoutStage();
        Assert.Same(appearance, stage.SelectedItem);
        Assert.NotNull(stage.Template.FindName("NavColumn", stage));
        presentation.Apply(null);
        LayoutStage();
        Assert.NotNull(stage.Template.FindName("NavColumn", stage));
        Assert.Same(appearance, stage.SelectedItem);
        presentation.Apply(blue);
        LayoutStage();
        Assert.Equal(CrystalBackstageLayout.Sidebar, presentation.Layout);
        Assert.NotNull(stage.Template.FindName("NavColumn", stage));
        presentation.SetLayout(CrystalBackstageLayout.Floating);
        LayoutStage();
        Assert.Null(stage.Template.FindName("NavColumn", stage));
        stage.SelectedItem = home;
        LayoutStage();
        Assert.Equal(titleInput.Text, title.Text);
    });

    [Fact]
    public void Crystal_file_rim_uses_tab_resources_and_restores_baseline_overlay() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        var templates = new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Office2024.xaml", UriKind.Relative) };
        ribbon.Style = (Style)templates[typeof(Ribbon)];
        ribbon.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        ribbon.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        ribbon.Measure(new Size(800, 200));
        ribbon.Arrange(new Rect(0, 0, 800, 200));
        CrystalFileHover.Apply(ribbon, true);
        var tabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
        var button = (System.Windows.Controls.Primitives.ToggleButton)tabs.Template.FindName("PART_ApplicationButton", tabs);
        var rim = (Border)button.Template.FindName("InnerRim", button);
        Assert.Same(ribbon.FindResource("RibbonKit.Brushes.Tab.HoverBorder"), rim.BorderBrush);
        Assert.Equal(new Thickness(1), rim.BorderThickness);
        var chrome = (Border)button.Template.FindName("Chrome", button);
        Assert.Equal(new CornerRadius(8), chrome.CornerRadius);
        Assert.Equal(chrome.CornerRadius, rim.CornerRadius);
        Assert.Equal(0d, rim.Opacity);
        var baselineSize = button.DesiredSize;
        ribbon.Resources.MergedDictionaries[1] = CrystalPalette.Create(Colors.Purple);
        Sta.Drain();
        Assert.Same(ribbon.FindResource("RibbonKit.Brushes.Tab.HoverBorder"), rim.BorderBrush);
        CrystalFileHover.Apply(ribbon, false);
        ribbon.Resources.MergedDictionaries.RemoveAt(1);
        ribbon.UpdateLayout();
        Assert.Same(ribbon.FindResource("RibbonKit.Brushes.ApplicationButton.InnerGlow"), rim.BorderBrush);
        Assert.Equal(new Thickness(0), rim.BorderThickness);
        Assert.Equal((CornerRadius)ribbon.FindResource("RibbonKit.Metrics.ApplicationButtonCornerRadius"), chrome.CornerRadius);
        Assert.Equal(chrome.CornerRadius, rim.CornerRadius);
        Assert.Null(rim.Style);
        Assert.Equal(baselineSize, button.DesiredSize);
    });

    [Fact]
    public void Crystal_minimized_tab_shapes_follow_state_tint_and_comparison() => Sta.Run(() =>
    {
        var templates = new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Office2024.xaml", UriKind.Relative) };
        var ribbon = new Ribbon { Style = (Style)templates[typeof(Ribbon)] };
        ribbon.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        ribbon.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        ribbon.Tabs.Add(new RibbonTab { Header = "Home" });
        ribbon.Tabs.Add(new CrystalContextualTab { Header = "Picture", IsContextual = true, ContextualColor = Brushes.Teal });
        var window = new Window { Content = ribbon, Width = 800, Height = 300,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show();
            CrystalTabShape.Apply(ribbon, true);
            Check(new CornerRadius(8, 8, 0, 0), new Thickness(1, 1, 1, 0));
            ribbon.IsMinimized = true;
            Check(new CornerRadius(8), new Thickness(1));
            ribbon.Resources.MergedDictionaries[1] = CrystalPalette.Create(Colors.Purple);
            Check(new CornerRadius(8), new Thickness(1));
            ribbon.IsMinimized = false;
            Check(new CornerRadius(8, 8, 0, 0), new Thickness(1, 1, 1, 0));
            ribbon.IsMinimized = true;
            CrystalTabShape.Apply(ribbon, false);
            ribbon.Resources.MergedDictionaries.RemoveAt(1);
            Check((CornerRadius)ribbon.FindResource("RibbonKit.Metrics.TabCornerRadius"),
                (Thickness)ribbon.FindResource("RibbonKit.Metrics.TabSelectedBorderThickness"));
            ribbon.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
            CrystalTabShape.Apply(ribbon, true);
            Check(new CornerRadius(8), new Thickness(1));
        }
        finally { window.Close(); }

        void Check(CornerRadius radius, Thickness border)
        {
            Sta.Drain(DispatcherPriority.Render);
            window.UpdateLayout();
            foreach (var tab in ribbon.Tabs)
            {
                var chrome = (Border)tab.Template.FindName("HeaderChrome", tab);
                Assert.Equal(radius, chrome.CornerRadius);
                Assert.Equal(border, chrome.BorderThickness);
            }
        }
    });

    [Fact]
    public void Crystal_gallery_preserves_tile_selection_and_scoped_chrome_across_popup_and_tint_changes() => Sta.Run(() =>
    {
        var gallery = new InRibbonGallery { Width = 282, SelectedIndex = 0 };
        for (int i = 0; i < 6; i++)
            gallery.Items.Add(new RibbonGalleryItem { Content = new TextBlock { Text = $"Style {i}", Width = 72 } });
        var window = new Window { Width = 400, Height = 200, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false, Content = gallery };
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        window.Resources.MergedDictionaries.Add(CrystalPalette.Create(CrystalPalette.Blue));
        try
        {
            window.Show();
            Sta.Drain(DispatcherPriority.Render);
            var tile = (RibbonGalleryItem)gallery.Items[0];
            var chrome = (Border)tile.Template.FindName("Chrome", tile);
            Assert.Equal(new CornerRadius(5), chrome.CornerRadius);
            Assert.IsType<DrawingBrush>(tile.FindResource("RibbonKit.Brushes.Group.Separator"));
            Assert.IsType<SolidColorBrush>(window.FindResource("RibbonKit.Brushes.Group.Separator"));
            Assert.IsType<DrawingBrush>(chrome.Background);
            gallery.IsDropDownOpen = true;
            Sta.Drain(DispatcherPriority.Render);
            var popup = (System.Windows.Controls.Primitives.Popup)gallery.Template.FindName("PART_Popup", gallery);
            var popupHost = (Border)gallery.Template.FindName("PART_PopupHost", gallery);
            Assert.True(popup.IsOpen);
            Assert.IsType<DrawingBrush>(popupHost.BorderBrush);
            Assert.IsType<DrawingBrush>(popupHost.Background);
            Assert.Same(gallery.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"), popupHost.Background);
            var strip = (Border)((Grid)VisualTreeHelper.GetChild(gallery, 0)).Children[0];
            Assert.IsType<DrawingBrush>(strip.Background);
            Assert.Same(gallery.FindResource("RibbonKit.Brushes.Control.SurfaceBackground"), strip.Background);
            Assert.IsType<SolidColorBrush>(window.FindResource("RibbonKit.Brushes.Control.SurfaceBackground"));
            Assert.IsType<LinearGradientBrush>(window.FindResource("RibbonKit.Brushes.Ribbon.ContentBackground"));
            Assert.Equal(new CornerRadius(8), popupHost.CornerRadius);
            gallery.IsDropDownOpen = false;
            window.Resources.MergedDictionaries[1] = CrystalPalette.Create(Colors.Purple);
            Sta.Drain(DispatcherPriority.Render);
            Assert.Same(tile, gallery.SelectedItem);
            Assert.Equal(new CornerRadius(5), ((Border)tile.Template.FindName("Chrome", tile)).CornerRadius);
            Assert.Same(window.FindResource("RibbonKit.Brushes.Tab.HoverBorder"), tile.FindResource("RibbonKit.Brushes.Group.Separator"));
            gallery.IsDropDownOpen = true;
            Sta.Drain(DispatcherPriority.Render);
            popupHost = (Border)gallery.Template.FindName("PART_PopupHost", gallery);
            Assert.Same(window.FindResource("Crystal.Brushes.GalleryBorder"), popupHost.BorderBrush);
            Assert.Same(window.FindResource("Crystal.Brushes.GallerySurface"), popupHost.Background);
            strip = (Border)((Grid)VisualTreeHelper.GetChild(gallery, 0)).Children[0];
            Assert.Same(popupHost.Background, strip.Background);
            gallery.IsDropDownOpen = false;
            window.Resources.MergedDictionaries.RemoveAt(1);
            Sta.Drain(DispatcherPriority.Render);
            Assert.Same(tile, gallery.SelectedItem);
            Assert.IsType<SolidColorBrush>(((Border)tile.Template.FindName("Chrome", tile)).Background);
        }
        finally
        {
            gallery.IsDropDownOpen = false;
            window.Close();
        }
    });

    [Fact]
    public void Crystal_screen_tip_tracks_local_palette_while_open_and_restores_baseline() => Sta.Run(() =>
    {
        var owner = new Button { Content = "Preview" };
        var window = new Window { Content = owner, Width = 200, Height = 100, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false };
        var tip = new RibbonScreenTip { Title = "Document title", Description = "Edit the heading in your document and File panel.", PlacementTarget = owner };
        owner.ToolTip = tip;
        var scope = new CrystalScreenTipPalette(new ResourceDictionary
        { Source = new Uri("/RibbonKit;component/Themes/Tokens.Office2024.xaml", UriKind.Relative) });
        scope.Attach(tip);
        scope.Attach(tip);
        Assert.Single(tip.Resources.MergedDictionaries);
        var blue = CrystalPalette.Create(CrystalPalette.Blue);
        scope.Apply(blue);
        try
        {
            window.Show();
            tip.IsOpen = true;
            Sta.Drain(DispatcherPriority.Render);
            var border = (Border)VisualTreeHelper.GetChild(tip, 0);
            Assert.IsType<DrawingBrush>(border.Background);
            Assert.IsType<SolidColorBrush>(border.BorderBrush);
            Assert.Equal(new CornerRadius(8), border.CornerRadius);
            var inner = (Border)tip.Template.FindName("InnerReflection", tip);
            Assert.Equal(new CornerRadius(7), inner.CornerRadius);
            Assert.False(inner.IsHitTestVisible);
            Assert.Equal(new Thickness(1), border.BorderThickness);
            Assert.IsType<System.Windows.Media.Effects.DropShadowEffect>(border.Effect);
            var title = (TextBlock)tip.Template.FindName("TitleText", tip);
            Assert.Equal(tip.Title, title.Text);
            tip.Title = null;
            Sta.Drain();
            Assert.Equal(Visibility.Collapsed, title.Visibility);
            tip.Title = "Document title";
            string? description = tip.Description;
            tip.Description = null;
            Sta.Drain();
            Assert.Equal(Visibility.Collapsed, ((TextBlock)tip.Template.FindName("DescriptionText", tip)).Visibility);
            tip.Description = description;
            var purple = CrystalPalette.Create(Colors.Purple);
            scope.Apply(purple);
            Sta.Drain(DispatcherPriority.Render);
            border = (Border)VisualTreeHelper.GetChild(tip, 0);
            Assert.Same(purple["Crystal.Brushes.ScreenTipSurface"], border.Background);
            Assert.Same(purple["Crystal.Brushes.ScreenTipBorder"], border.BorderBrush);
            Assert.True(tip.IsOpen);
            scope.Apply(null);
            Sta.Drain(DispatcherPriority.Render);
            border = (Border)VisualTreeHelper.GetChild(tip, 0);
            Assert.IsType<SolidColorBrush>(border.Background);
            Assert.IsType<SolidColorBrush>(border.BorderBrush);
            Assert.Equal("Document title", tip.Title);
        }
        finally
        {
            tip.IsOpen = false;
            window.Close();
        }
    });

    private static void CheckCrystalApplicationMenu(CrystalPreviewWindow window, Ribbon ribbon)
    {
        var toggle = (RibbonMenuItem)window.FindName("ApplicationMenuPreviewToggle");
        var menu = (RibbonApplicationMenu)window.FindName("CrystalFileMenu");
        Assert.Null(ribbon.ApplicationMenu);
        toggle.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Same(menu, ribbon.ApplicationMenu);
        ribbon.IsBackstageOpen = true;
        LayoutMenu();
        Assert.True(ribbon.IsApplicationMenuOpen);
        var frame = (Border)menu.Template.FindName("Frame", menu);
        Assert.Equal(new CornerRadius(14), frame.CornerRadius);
        Assert.Null(frame.Effect);
        var frameWrapper = Assert.IsType<Grid>(VisualTreeHelper.GetParent(frame));
        var shadowLayer = Assert.IsType<Grid>(frameWrapper.Children[0]);
        var caster = Assert.IsType<Border>(shadowLayer.Children[0]);
        Assert.Equal(0d, Assert.IsType<System.Windows.Media.Effects.DropShadowEffect>(caster.Effect).ShadowDepth);
        Assert.False(shadowLayer.IsHitTestVisible);
        CheckMenuShadowPixels(shadowLayer);
        var backdropLayer = Assert.IsType<Grid>(frameWrapper.Children[1]);
        Assert.Equal("CrystalMenuBackdrop", backdropLayer.Name);
        Assert.NotNull(Assert.IsType<Image>(backdropLayer.Children[0]).Source);
        Assert.Same(window.FindResource("Crystal.Brushes.FrostedFrame"), frame.Background);
        Assert.NotSame(((Border)ribbon.Template.FindName("QatBelowHost", ribbon)).Background, frame.Background);
        Assert.Same(window.FindResource("RibbonKit.Brushes.Control.HoverBorder"), frame.BorderBrush);
        foreach (var band in new[] { "TopBand", "FooterBand" })
            Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(
                ((Border)menu.Template.FindName(band, menu)).Background).Color);
        RibbonApplicationMenuSeparator? separator = null;
        foreach (var entry in menu.Items)
            if (entry is RibbonApplicationMenuSeparator line) separator = line;
        Assert.NotNull(separator);
        Assert.Equal(new Thickness(12, 4, 12, 4), separator.Margin);
        var pane = (Border)menu.Template.FindName("PART_Pane", menu);
        var innerContent = (Grid)VisualTreeHelper.GetParent(pane);
        var innerOutline = (Border)VisualTreeHelper.GetParent(innerContent);
        var innerRim = (Border)VisualTreeHelper.GetParent(innerOutline);
        var activePage = (Border)menu.Template.FindName("ActivePage", menu);
        Assert.Equal(new CornerRadius(10), innerOutline.CornerRadius);
        Assert.Equal(new CornerRadius(11), innerRim.CornerRadius);
        Assert.Equal(9d, Assert.IsType<RectangleGeometry>(innerContent.Clip).RadiusX);
        Assert.Equal(new Thickness(), activePage.BorderThickness);
        var split = (RibbonApplicationMenuItem)window.FindName("CrystalSaveAsMenuItem");
        var print = (RibbonApplicationMenuItem)window.FindName("CrystalPrintMenuItem");
        Assert.True(split.IsSplitPresentation);
        Assert.False(print.IsSplitPresentation);
        menu.NotifyItemHoverChanged(split, true);
        LayoutMenu();
        Assert.Same(split, menu.ActiveItem);
        Assert.NotNull(((Grid)split.Template.FindName("Root", split)).Clip);
        menu.NotifyItemHoverChanged(print, true);
        LayoutMenu();
        Assert.Same(print, menu.ActiveItem);
        var printPane = (StackPanel)print.Content;
        Assert.False(((RibbonApplicationMenuPaneItem)printPane.Children[1]).IsEnabled);
        ((RibbonApplicationMenuPaneItem)printPane.Children[0]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.False(ribbon.IsBackstageOpen);
        ribbon.IsBackstageOpen = true;
        window.Width = 420;
        LayoutMenu();
        Assert.True(menu.ActualWidth < window.ActualWidth);
        Assert.Equal(new Rect(innerContent.RenderSize), Assert.IsType<RectangleGeometry>(innerContent.Clip).Rect);
        var accent = (RibbonDropDownButton)window.FindName("AccentSelector");
        var oldFace = frame.Background;
        ((RibbonMenuItem)accent.Items[1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        LayoutMenu();
        Assert.NotSame(oldFace, frame.Background);
        Assert.Same(window.FindResource("Crystal.Brushes.FrostedFrame"), frame.Background);
        var compare = (RibbonToggleButton)window.FindName("CompareToggle");
        compare.IsChecked = true;
        compare.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        LayoutMenu();
        Assert.Equal(new CornerRadius(8), frame.CornerRadius);
        Assert.IsType<Border>(VisualTreeHelper.GetParent(frame));
        Assert.NotNull(frame.Effect);
        Assert.Equal(new Thickness(), separator.Margin);
        Assert.Equal(new CornerRadius(), innerOutline.CornerRadius);
        Assert.Null(innerContent.Clip);
        Assert.Equal(new Thickness(1), activePage.BorderThickness);
        Assert.Equal(new Thickness(0, 0, 2, 2), activePage.Margin);
        Assert.Null(((Grid)split.Template.FindName("Root", split)).Clip);
        compare.IsChecked = false;
        compare.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        ((RibbonMenuItem)accent.Items[0]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        window.Width = 1080;
        LayoutMenu();
        Assert.Equal(new CornerRadius(14), frame.CornerRadius);
        var footer = Assert.IsType<StackPanel>(menu.FooterContent);
        ((RibbonApplicationMenuButton)footer.Children[0]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.Null(ribbon.ApplicationMenu);
        Assert.False(ribbon.IsBackstageOpen);
        LayoutMenu();
        void LayoutMenu() { Sta.Drain(); window.UpdateLayout(); }
    }

    private static void CheckMenuShadowPixels(Grid shadow)
    {
        const int padding = 24;
        int width = (int)Math.Ceiling(shadow.ActualWidth);
        int height = (int)Math.Ceiling(shadow.ActualHeight);
        var image = new System.Windows.Media.Imaging.RenderTargetBitmap(width + padding * 2,
            height + padding * 2, 96, 96, PixelFormats.Pbgra32);
        var drawing = new DrawingVisual();
        using (var context = drawing.RenderOpen())
            context.DrawRectangle(new VisualBrush(shadow)
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(-padding, -padding, width + padding * 2, height + padding * 2),
                Stretch = Stretch.Fill,
            }, null, new Rect(0, 0, image.PixelWidth, image.PixelHeight));
        image.Render(drawing);
        // Verify actual rendered shadow on every edge, with no opaque caster under the glass.
        Assert.True(Alpha(padding + width / 2, padding - 2) > 0, "Missing top shadow");
        Assert.True(Alpha(padding + width / 2, padding + height + 2) > 0, "Missing bottom shadow");
        Assert.True(Alpha(padding - 2, padding + height / 2) > 0, "Missing left shadow");
        Assert.True(Alpha(padding + width + 2, padding + height / 2) > 0, "Missing right shadow");
        Assert.Equal(0, Alpha(padding + width / 2, padding + height / 2));
        int Alpha(int x, int y)
        {
            var pixel = new byte[4];
            image.CopyPixels(new Int32Rect(x, y, 1, 1), pixel, 4, 0);
            return pixel[3];
        }
    }

    private static void CheckCrystalScrollBars(CrystalPreviewWindow window, Ribbon ribbon)
    {
        var stage = (Backstage)window.FindName("CrystalBackstage");
        var compare = (RibbonToggleButton)window.FindName("CompareToggle");
        var viewers = new[] { "CrystalDocumentScroll", "CrystalOverviewScroll", "CrystalAppearanceScroll", "CrystalAboutScroll" };
        int previousSelection = stage.SelectedIndex;
        try
        {
            for (int i = 0; i < viewers.Length; i++)
            {
                var viewer = (ScrollViewer)window.FindName(viewers[i]);
                double originalHeight = viewer.Height;
                try
                {
                    if (i > 0)
                    {
                        stage.SelectedIndex = i - 1;
                        ribbon.IsBackstageOpen = true;
                    }
                    viewer.Height = 80;
                    LayoutScroll();
                    var bar = (System.Windows.Controls.Primitives.ScrollBar)viewer.Template.FindName("PART_VerticalScrollBar", viewer);
                    Assert.Equal(Visibility.Visible, bar.Visibility);
                    Assert.Same(bar.FindResource("RibbonKit.VerticalScrollBarTemplate"), bar.Template);
                    Assert.Equal(14d, bar.Width);
                    Assert.Equal(Colors.Transparent, Assert.IsType<SolidColorBrush>(bar.Background).Color);
                    Assert.IsType<DrawingBrush>(bar.FindResource("RibbonKit.Brushes.ScrollBar.Thumb"));
                    viewer.ScrollToVerticalOffset(12);
                    LayoutScroll();
                    Assert.True(viewer.VerticalOffset > 0);
                    var crystalTemplate = bar.Template;
                    compare.IsChecked = true;
                    compare.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    LayoutScroll();
                    Assert.NotSame(crystalTemplate, bar.Template);
                    compare.IsChecked = false;
                    compare.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    LayoutScroll();
                    Assert.Same(bar.FindResource("RibbonKit.VerticalScrollBarTemplate"), bar.Template);
                }
                finally
                {
                    viewer.Height = originalHeight;
                    viewer.ScrollToTop();
                }
            }
        }
        finally
        {
            ribbon.IsBackstageOpen = false;
            stage.SelectedIndex = previousSelection;
            LayoutScroll();
        }
        void LayoutScroll() { Sta.Drain(); window.UpdateLayout(); }
    }

    private static void CheckCrystalMessages(CrystalPreviewWindow window, Ribbon ribbon, RibbonTabControl tabs)
    {
        var previousMotion = RibbonKit.Animation.RibbonAnimation.GetActionOverride(RibbonKit.Animation.RibbonAnimationAction.MessageBar);
        RibbonKit.Animation.RibbonAnimation.SetActionLevel(RibbonKit.Animation.RibbonAnimationAction.MessageBar,
            RibbonKit.Animation.RibbonAnimationLevel.None);
        try
        {
            var bar = (RibbonMessageBar)window.FindName("CrystalMessageBar");
            var first = (RibbonMessage)window.FindName("CrystalProtectedMessage");
            var second = (RibbonMessage)window.FindName("CrystalSecurityMessage");
            var show = (RibbonMenuItem)window.FindName("MessagePreviewButton");
            var drawer = (Border)ribbon.Template.FindName("QatBelowHost", ribbon);
            var drawerRadius = drawer.CornerRadius;
            var drawerShadow = drawer.Effect;
            Assert.False(bar.HasOpenMessages);
            show.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            LayoutMessages();
            Assert.True(first.IsOpen);
            Assert.False(second.IsOpen);
            Assert.True(ribbon.HasOpenMessages);
            var root = (Border)first.Template.FindName("PART_Root", first);
            Assert.Equal(new CornerRadius(10), root.CornerRadius);
            Assert.IsType<LinearGradientBrush>(root.Background);
            Assert.Equal(drawerRadius, drawer.CornerRadius);
            Assert.Same(drawerShadow, drawer.Effect);
            var drawerBounds = drawer.TransformToAncestor(ribbon).TransformBounds(new Rect(drawer.RenderSize));
            var noticeBounds = root.TransformToAncestor(ribbon).TransformBounds(new Rect(root.RenderSize));
            Assert.True(noticeBounds.Top > drawerBounds.Bottom);
            show.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            LayoutMessages();
            Assert.True(second.IsOpen);
            var secondRoot = (Border)second.Template.FindName("PART_Root", second);
            Assert.Equal(new CornerRadius(10), secondRoot.CornerRadius);
            var action = (Button)first.Template.FindName("PART_ActionButton", first);
            Assert.Same(action.FindResource("Crystal.Backstage.Action"), action.Style);
            action.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            LayoutMessages();
            Assert.False(first.IsOpen);
            Assert.True(second.IsOpen);
            show.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            LayoutMessages();
            Assert.True(first.IsOpen);
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            LayoutMessages();
            var body = (Border)tabs.Template.FindName("ContentHost", tabs);
            Assert.Equal(new CornerRadius(14), body.CornerRadius);
            Assert.True(root.TranslatePoint(new Point(), ribbon).Y > body.TranslatePoint(new Point(0, body.ActualHeight), ribbon).Y);
            var compare = (RibbonToggleButton)window.FindName("CompareToggle");
            compare.IsChecked = true;
            compare.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            LayoutMessages();
            Assert.Equal(new CornerRadius(), root.CornerRadius);
            Assert.NotSame(action.FindResource("Crystal.Backstage.Action"), action.Style);
            compare.IsChecked = false;
            compare.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            LayoutMessages();
            Assert.Equal(new CornerRadius(10), root.CornerRadius);
            foreach (var message in new[] { first, second })
                ((Button)message.Template.FindName("PART_CloseButton", message)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            LayoutMessages();
            Assert.False(ribbon.HasOpenMessages);
            Assert.Equal(new Thickness(), bar.Margin);
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
            LayoutMessages();
            Assert.Equal(drawerRadius, drawer.CornerRadius);
        }
        finally
        {
            if (previousMotion is { } level)
                RibbonKit.Animation.RibbonAnimation.SetActionLevel(RibbonKit.Animation.RibbonAnimationAction.MessageBar, level);
            else RibbonKit.Animation.RibbonAnimation.ClearActionLevel(RibbonKit.Animation.RibbonAnimationAction.MessageBar);
        }
        void LayoutMessages() { Sta.Drain(); window.UpdateLayout(); }
    }

    private static Border? FindUtilityRim(System.Windows.Controls.Primitives.ButtonBase button)
    {
        if (button.Template.FindName("Chrome", button) is Border { Child: Grid grid })
            foreach (UIElement child in grid.Children)
                if (child is Border { Name: "CrystalUtilityRim" } rim) return rim;
        return null;
    }

    private static double Luminance(Color color)
    {
        static double Linear(byte b)
        {
            double c = b / 255d;
            return c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
    }

    private static RibbonTabControl Host(params RibbonTab[] tabs)
    {
        var host = new RibbonTabControl
        {
            Width = 760, Height = 150, ItemsSource = tabs, SelectedIndex = 0,
            Template = (ControlTemplate)XamlReader.Parse("""
                <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:rk="clr-namespace:RibbonKit.Controls;assembly=RibbonKit" TargetType="{x:Type rk:RibbonTabControl}">
                    <Grid>
                        <TabPanel IsItemsHost="True" />
                        <Rectangle x:Name="PART_TabMarker" Height="3" HorizontalAlignment="Left" VerticalAlignment="Top">
                            <Rectangle.RenderTransform><TranslateTransform x:Name="PART_TabMarkerTranslate" /></Rectangle.RenderTransform>
                        </Rectangle>
                    </Grid>
                </ControlTemplate>
                """),
        };
        host.Resources["RibbonKit.Brushes.Tab.SelectedUnderline"] = Brushes.Blue;
        return host;
    }

    private static void Layout(RibbonTabControl host)
    {
        host.ApplyTemplate();
        host.Measure(new Size(760, 150));
        host.Arrange(new Rect(0, 0, 760, 150));
        host.UpdateLayout();
        Sta.Drain(DispatcherPriority.Loaded);
    }

    private sealed class ShownHost : IDisposable
    {
        private readonly Window _window;
        public ShownHost(RibbonTabControl host)
        {
            _window = new Window { Content = host, Width = 800, Height = 200,
                ShowInTaskbar = false, ShowActivated = false, Left = -10000, Top = -10000 };
            _window.Show();
        }
        public void Dispose() => _window.Close();
    }
}
