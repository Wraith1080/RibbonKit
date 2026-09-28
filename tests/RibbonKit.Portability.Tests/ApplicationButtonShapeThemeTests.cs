using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

/// <summary>A library-only consumer: this project has no Showcase reference or resources.</summary>
public sealed class ApplicationButtonShapeThemeTests
{
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
        if (!thread.Join(TimeSpan.FromSeconds(30)))
            throw new TimeoutException("The application-button shape test did not finish within 30 seconds.");
        failure?.Throw();
    }
}
