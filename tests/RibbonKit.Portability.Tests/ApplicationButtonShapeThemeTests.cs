using System.ComponentModel;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

/// <summary>A library-only consumer: this project has no Showcase reference or resources.</summary>
public sealed class ApplicationButtonShapeThemeTests
{
    private static void VerifyNoApplicationHeaderInset(Application application)
    {
        var backstage = new Backstage();
        var ribbon = new Ribbon { Backstage = backstage,
            QuickAccessPosition = RibbonQuickAccessPosition.TabRow };
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Save" });
        var home = new RibbonTab { Header = "Home" };
        home.Groups.Add(new RibbonGroup { Header = "Clipboard" });
        ribbon.Tabs.Add(home);
        ribbon.SelectedTab = home;
        var window = new RibbonWindow
        {
            Content = ribbon,
            Width = 700,
            Height = 350,
            Left = -10000,
            Top = -10000,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            foreach ((RibbonTheme theme, double inset) in new[]
            {
                (RibbonTheme.Office2007, 4d),
                (RibbonTheme.Office2010, 0d),
                (RibbonTheme.Office2013, 0d),
                (RibbonTheme.Office2019, 0d),
                (RibbonTheme.Office2024, 0d),
                (RibbonTheme.CrystalLight, 22d),
            })
            {
                foreach (bool dark in new[] { false, true })
                {
                    ThemeManager.Apply(application, theme);
                    ThemeManager.SetDarkMode(application, dark);
                    ribbon.Backstage = backstage;
                    ribbon.FlowDirection = FlowDirection.LeftToRight;
                    ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
                    Drain();
                    window.UpdateLayout();

                    var tabs = Assert.IsType<RibbonTabControl>(
                        ribbon.Template.FindName("TabControlHost", ribbon));
                    var panel = Assert.IsType<TabPanel>(
                        tabs.Template.FindName("PART_TabItemsPanel", tabs));
                    var qat = Assert.IsType<RibbonQuickAccessToolBar>(
                        tabs.Template.FindName("QatTabRowHost", tabs));
                    var file = Assert.IsType<ToggleButton>(
                        tabs.Template.FindName("PART_ApplicationButton", tabs));
                    var body = Assert.IsType<Border>(
                        tabs.Template.FindName("ContentHost", tabs));
                    var normalTabMargin = Assert.IsType<Thickness>(
                        ribbon.FindResource("RibbonKit.Metrics.TabStripMargin"));
                    var normalQatMargin = new Thickness(2, 4, 4, 0);
                    Assert.Equal(Visibility.Visible, file.Visibility);
                    Assert.Equal(normalTabMargin, panel.Margin);
                    Assert.Equal(normalQatMargin, qat.Margin);

                    ribbon.Backstage = null;
                    Drain();
                    window.UpdateLayout();
                    Assert.Equal(Visibility.Collapsed, file.Visibility);
                    Assert.Equal(normalTabMargin, panel.Margin);
                    Assert.Equal(LeadingInset(normalQatMargin, inset, false), qat.Margin);
                    if (inset > 0)
                        AssertPastCorner(qat, body, tabs, false);

                    ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
                    Drain();
                    window.UpdateLayout();
                    Assert.Equal(Visibility.Collapsed, qat.Visibility);
                    Assert.Equal(LeadingInset(normalTabMargin, inset, false), panel.Margin);
                    if (inset > 0)
                        AssertPastCorner(home, body, tabs, false);

                    ribbon.FlowDirection = FlowDirection.RightToLeft;
                    Drain();
                    window.UpdateLayout();
                    Assert.Equal(LeadingInset(normalTabMargin, inset, true), panel.Margin);
                    if (inset > 0)
                        AssertPastCorner(home, body, tabs, true);

                    ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
                    Drain();
                    window.UpdateLayout();
                    Assert.Equal(normalTabMargin, panel.Margin);
                    Assert.Equal(LeadingInset(normalQatMargin, inset, true), qat.Margin);
                    if (inset > 0)
                        AssertPastCorner(qat, body, tabs, true);
                }
            }

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            ThemeManager.SetDarkMode(application, false);
            ribbon.Backstage = null;
            ribbon.FlowDirection = FlowDirection.LeftToRight;
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
            Drain();
            var liveTabs = Assert.IsType<RibbonTabControl>(
                ribbon.Template.FindName("TabControlHost", ribbon));
            var liveQat = Assert.IsType<RibbonQuickAccessToolBar>(
                liveTabs.Template.FindName("QatTabRowHost", liveTabs));
            Assert.Equal(new Thickness(2, 4, 4, 0), liveQat.Margin);
            var office2007Tokens = Tokens("Office2007");
            ribbon.Resources.MergedDictionaries.Add(office2007Tokens);
            Drain();
            Assert.Equal(new Thickness(6, 4, 4, 0), liveQat.Margin);
            ribbon.Resources.MergedDictionaries.Remove(office2007Tokens);
            Drain();
            Assert.Equal(new Thickness(2, 4, 4, 0), liveQat.Margin);
        }
        finally
        {
            window.Close();
            ThemeManager.SetDarkMode(application, false);
        }
    }

    [Fact]
    public void Theme_default_and_local_value_follow_wpf_precedence() => RunSta(() =>
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        ThemeManager.Apply(application, RibbonTheme.Office2024);
        var ribbon = new Ribbon
        {
            Backstage = new Backstage { Design = RibbonBackstageDesign.Classic2007 },
        };
        var window = new RibbonWindow
        {
            Content = ribbon,
            Width = 600,
            Height = 300,
            Left = -10000,
            Top = -10000,
            ShowActivated = false,
            ShowInTaskbar = false,
        };
        try
        {
            window.Show();
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Tab);

            foreach (RibbonTheme theme in Enum.GetValues<RibbonTheme>())
            {
                ThemeManager.Apply(application, theme);
                Drain();
                AssertShape(ribbon, theme == RibbonTheme.Office2007
                    ? RibbonApplicationButtonShape.Orb
                    : RibbonApplicationButtonShape.Tab);
            }

            ThemeManager.Apply(application, RibbonTheme.Office2007);
            ThemeManager.SetDarkMode(application, true);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);
            ThemeManager.Apply(application, RibbonTheme.Office2024);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Tab);

            ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Tab;
            ThemeManager.Apply(application, RibbonTheme.Office2007);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Tab);
            ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Orb;
            ThemeManager.Apply(application, RibbonTheme.CrystalLight);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);
            ribbon.ClearValue(Ribbon.ApplicationButtonShapeProperty);
            Drain();
            Assert.Same(DependencyProperty.UnsetValue,
                ribbon.ReadLocalValue(Ribbon.ApplicationButtonShapeProperty));
            AssertShape(ribbon, RibbonApplicationButtonShape.Tab);
            ThemeManager.Apply(application, RibbonTheme.Office2007);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);

            ThemeManager.Apply(application, RibbonTheme.Office2024);
            Drain();
            var tokens = Tokens("Office2007");
            ribbon.Resources.MergedDictionaries.Add(tokens);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);
            var black = Tokens("Office2007.Dark");
            ribbon.Resources.MergedDictionaries.Add(black);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);
            ribbon.Resources.MergedDictionaries.Remove(black);
            ribbon.Resources.MergedDictionaries.Remove(tokens);
            tokens = Tokens("Office2019");
            ribbon.Resources.MergedDictionaries.Add(tokens);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Tab);
            ribbon.Resources.MergedDictionaries.Remove(tokens);

            ThemeManager.Apply(application, RibbonTheme.Office2007);
            Drain();
            AssertShape(ribbon, RibbonApplicationButtonShape.Orb);
            ribbon.IsBackstageOpen = true;
            Drain();
            var field = typeof(Ribbon).GetField("_classicBackstageOrbProxy",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            var proxy = Assert.IsType<Button>(field.GetValue(ribbon));
            Assert.NotNull(VisualTreeHelper.GetParent(proxy));
            ribbon.IsBackstageOpen = false;
            Drain();
            ribbon.ApplicationButtonShape = RibbonApplicationButtonShape.Tab;
            ribbon.IsBackstageOpen = true;
            Drain();
            Assert.Null(VisualTreeHelper.GetParent(proxy));

            VerifyNoApplicationHeaderInset(application);

            ribbon.IsBackstageOpen = false;
            ribbon.ClearValue(Ribbon.ApplicationButtonShapeProperty);
            ThemeManager.Apply(application, RibbonTheme.Office2007);
            Drain();
            VerifyApplicationOrbGlyphTemplate(ribbon, window);
        }
        finally
        {
            window.Close();
            ThemeManager.SetDarkMode(application, false);
            application.Shutdown();
        }
    });

    private static ResourceDictionary Tokens(string name) => new()
    {
        Source = new Uri($"/RibbonKit;component/Themes/Tokens.{name}.xaml", UriKind.Relative),
    };

    private static void VerifyApplicationOrbGlyphTemplate(Ribbon ribbon, RibbonWindow window)
    {
        Assert.Null(ribbon.ApplicationOrbGlyphTemplate);
        Assert.Equal(typeof(DataTemplate), TypeDescriptor.GetProperties(typeof(Ribbon))[
            nameof(Ribbon.ApplicationOrbGlyphTemplate)]?.PropertyType);
        var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
        var button = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
        var realGlyph = NamedDescendant<Viewbox>(button, "OrbGlyph");
        var realSphere = NamedDescendant<Ellipse>(button, "OrbFill");
        Assert.Equal(Visibility.Visible, NamedDescendant<Grid>(button, "DefaultOrbGlyph").Visibility);
        Assert.Equal(Visibility.Collapsed,
            NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph").Visibility);

        var first = GlyphTemplate(Brushes.Gold);
        ribbon.ApplicationOrbGlyphTemplate = first;
        Drain();
        window.UpdateLayout();
        var realCustom = NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph");
        Assert.Same(first, realCustom.ContentTemplate);
        Assert.Equal(Visibility.Collapsed, NamedDescendant<Grid>(button, "DefaultOrbGlyph").Visibility);
        realCustom.ApplyTemplate();
        Drain();
        window.UpdateLayout();
        Assert.True(VisualTreeHelper.GetChildrenCount(realCustom) > 0,
            "The custom orb glyph template did not produce a visual.");
        var realMark = Descendant<Path>(realCustom);
        Assert.Same(Brushes.Gold, realMark.Fill);
        Assert.Same(realSphere, NamedDescendant<Ellipse>(button, "OrbFill"));

        ribbon.IsBackstageOpen = true;
        Drain();
        window.UpdateLayout();
        var field = typeof(Ribbon).GetField("_classicBackstageOrbProxy",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var proxy = Assert.IsType<Button>(field.GetValue(ribbon));
        Assert.NotNull(VisualTreeHelper.GetParent(proxy));
        Assert.Equal(string.Empty, proxy.Content);
        Assert.Same(
            Assert.IsType<ContentPresenter>(button.Template.FindName("Orb", button)).ContentTemplate,
            proxy.ContentTemplate);
        var proxyGlyph = NamedDescendant<Viewbox>(proxy, "OrbGlyph");
        var proxySphere = NamedDescendant<Ellipse>(proxy, "OrbFill");
        var proxyCustom = NamedDescendant<ContentPresenter>(proxy, "CustomOrbGlyph");
        var proxyMark = Descendant<Path>(proxyCustom);
        Assert.Same(Brushes.Gold, proxyMark.Fill);
        Assert.NotSame(realMark, proxyMark);
        Assert.Contains(Assert.IsType<TransformGroup>(proxyGlyph.RenderTransform).Children,
            transform => transform is RotateTransform);
        Assert.True(proxySphere.RenderTransform.Value.IsIdentity);
        Assert.Same(VisualTreeHelper.GetParent(proxyGlyph), VisualTreeHelper.GetParent(proxySphere));
        Assert.False(string.IsNullOrWhiteSpace(AutomationProperties.GetName(proxy)));
        Assert.Equal(proxy.ToolTip, AutomationProperties.GetName(proxy));

        var second = GlyphTemplate(Brushes.DeepSkyBlue);
        ribbon.ApplicationOrbGlyphTemplate = second;
        Drain();
        window.UpdateLayout();
        Assert.Same(second,
            NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph").ContentTemplate);
        Assert.Same(second,
            NamedDescendant<ContentPresenter>(proxy, "CustomOrbGlyph").ContentTemplate);
        Assert.NotSame(Descendant<Path>(realCustom), Descendant<Path>(proxyCustom));
        Assert.Same(Brushes.DeepSkyBlue, Descendant<Path>(realCustom).Fill);
        Assert.Same(Brushes.DeepSkyBlue, Descendant<Path>(proxyCustom).Fill);
        Assert.Same(realGlyph, NamedDescendant<Viewbox>(button, "OrbGlyph"));
        Assert.Same(proxySphere, NamedDescendant<Ellipse>(proxy, "OrbFill"));

        ribbon.ApplicationOrbGlyphTemplate = null;
        Drain();
        window.UpdateLayout();
        Assert.Equal(Visibility.Visible, NamedDescendant<Grid>(button, "DefaultOrbGlyph").Visibility);
        Assert.Equal(Visibility.Visible, NamedDescendant<Grid>(proxy, "DefaultOrbGlyph").Visibility);
        Assert.Equal(Visibility.Collapsed,
            NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph").Visibility);
        Assert.Equal(Visibility.Collapsed,
            NamedDescendant<ContentPresenter>(proxy, "CustomOrbGlyph").Visibility);
        ribbon.IsBackstageOpen = false;
        Drain();

        ribbon.ApplicationOrbGlyphTemplate = first;
        ribbon.ApplicationMenu = new RibbonApplicationMenu();
        ribbon.IsBackstageOpen = true;
        Drain();
        window.UpdateLayout();
        Assert.True(ribbon.IsApplicationMenuOpen);
        Assert.Same(first, NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph").ContentTemplate);
        Assert.Same(Brushes.Gold,
            Descendant<Path>(NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph")).Fill);
        ribbon.IsBackstageOpen = false;
        ribbon.ApplicationMenu = null;
        Drain();

        var backstage = Assert.IsType<Backstage>(ribbon.Backstage);
        backstage.Design = RibbonBackstageDesign.Glass2007;
        ribbon.IsBackstageOpen = true;
        Drain();
        ribbon.IsBackstageOpen = false;
        Drain();
        Assert.Same(first, NamedDescendant<ContentPresenter>(button, "CustomOrbGlyph").ContentTemplate);
    }

    private static DataTemplate GlyphTemplate(Brush fill)
    {
        var mark = new FrameworkElementFactory(typeof(Path));
        mark.SetValue(Path.DataProperty, Geometry.Parse("M0,0 L16,0 L16,16 Z"));
        mark.SetValue(Shape.FillProperty, fill);
        mark.SetValue(FrameworkElement.WidthProperty, 16d);
        mark.SetValue(FrameworkElement.HeightProperty, 16d);
        return new DataTemplate { VisualTree = mark };
    }

    private static T NamedDescendant<T>(DependencyObject root, string name)
        where T : FrameworkElement
    {
        FrameworkElement? found = FindNamedDescendant(root, name);
        return Assert.IsType<T>(found);
    }

    private static T Descendant<T>(DependencyObject root) where T : FrameworkElement
    {
        T? found = FindDescendant<T>(root);
        return Assert.IsType<T>(found);
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : FrameworkElement
    {
        if (root is T matching)
            return matching;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            T? found = FindDescendant<T>(VisualTreeHelper.GetChild(root, i));
            if (found is not null)
                return found;
        }
        return null;
    }

    private static FrameworkElement? FindNamedDescendant(DependencyObject root, string name)
    {
        if (root is FrameworkElement element && element.Name == name)
            return element;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            FrameworkElement? found = FindNamedDescendant(VisualTreeHelper.GetChild(root, i), name);
            if (found is not null)
                return found;
        }
        return null;
    }

    private static Thickness LeadingInset(Thickness margin, double inset, bool rtl) => rtl
        ? new Thickness(margin.Left, margin.Top, margin.Right + inset, margin.Bottom)
        : new Thickness(margin.Left + inset, margin.Top, margin.Right, margin.Bottom);

    private static void AssertPastCorner(FrameworkElement element, Border body,
        RibbonTabControl tabs, bool rtl)
    {
        Rect bounds = element.TransformToVisual(tabs).TransformBounds(
            new Rect(0, 0, element.ActualWidth, element.ActualHeight));
        Rect bodyBounds = body.TransformToVisual(tabs).TransformBounds(
            new Rect(0, 0, body.ActualWidth, body.ActualHeight));
        if (rtl)
            Assert.True(bounds.Right <= bodyBounds.Right - body.CornerRadius.TopRight + 0.5);
        else
            Assert.True(bounds.Left >= bodyBounds.Left + body.CornerRadius.TopLeft - 0.5);
    }

    private static void AssertShape(Ribbon ribbon, RibbonApplicationButtonShape expected)
    {
        Assert.Equal(expected, ribbon.ApplicationButtonShape);
        var tabs = Assert.IsType<RibbonTabControl>(ribbon.Template.FindName("TabControlHost", ribbon));
        var button = Assert.IsType<ToggleButton>(tabs.Template.FindName("PART_ApplicationButton", tabs));
        button.ApplyTemplate();
        var orb = Assert.IsType<ContentPresenter>(button.Template.FindName("Orb", button));
        Assert.Equal(expected == RibbonApplicationButtonShape.Orb
            ? Visibility.Visible : Visibility.Collapsed, orb.Visibility);
    }

    private static void Drain() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Background);

    private static void RunSta(Action body)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try { body(); }
            catch (Exception exception) { failure = ExceptionDispatchInfo.Capture(exception); }
            finally { Dispatcher.CurrentDispatcher.InvokeShutdown(); }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(90)))
            throw new TimeoutException("The ribbon portability test did not finish within 90 seconds.");
        failure?.Throw();
    }
}
