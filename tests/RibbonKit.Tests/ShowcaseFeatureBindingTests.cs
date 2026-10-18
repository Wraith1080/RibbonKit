using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Localization;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class ShowcaseFeatureBindingTests
{
    [Theory]
    [InlineData("CanUserCustomizeRibbon", RibbonString.CustomizeRibbon)]
    [InlineData("CanUserCustomizeQuickAccess", RibbonString.CustomizeQuickAccessToolbar)]
    [InlineData("CanUserMinimizeRibbon", RibbonString.CollapseRibbon)]
    [InlineData("CanUserChangeLayoutMode", RibbonString.SimplifiedRibbon)]
    [InlineData("IsSimplifiedRibbonEnabled", RibbonString.SimplifiedRibbon)]
    [InlineData("CanUserToggleQuickAccessVisibility", RibbonString.HideQuickAccessToolbar)]
    public void Right_click_with_disabled_policy_has_no_detached_menu_alignment_bindings(string policy, RibbonString caption) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(showcaseResources: true), RibbonTheme.Office2024);
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        var window = new MainWindow { Width = 1540, Height = 900, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false };
        var type = typeof(MainWindow);
        window.Loaded -= (RoutedEventHandler)type.GetMethod("OnWindowLoaded",
            BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate(typeof(RoutedEventHandler), window);
        type.GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
        using var errors = new StringWriter();
        using var listener = new TextWriterTraceListener(errors);
        var trace = PresentationTraceSources.DataBindingSource;
        var previous = trace.Switch.Level;
        ContextMenu? menu = null;
        try
        {
            window.Show(); Layout();
            var ribbon = window.MainRibbon;
            var property = typeof(Ribbon).GetProperty(policy)!;
            property.SetValue(ribbon, false); Layout();
            trace.Listeners.Add(listener); PresentationTraceSources.Refresh(); trace.Switch.Level = SourceLevels.Warning;
            var probe = new TextBlock();
            probe.SetBinding(TextBlock.TextProperty, new Binding("MenuAlignmentTraceProbeMissing") { Source = new object() });
            _ = probe.Text; Layout(); listener.Flush();
            Assert.Contains("MenuAlignmentTraceProbeMissing", errors.ToString());
            errors.GetStringBuilder().Clear();

            // Raise the native routed opening event on the actual Showcase Ribbon,
            // then on one of its commands; this exercises its right-click handler.
            foreach (UIElement target in new UIElement[] { ribbon, window.TouchModeSelector })
            {
                if (target == window.TouchModeSelector)
                {
                    ribbon.SelectedTab = ribbon.Tabs.Single(tab => Equals(tab.Header, "View")); Layout();
                }
                menu = OpenMenu(target); Layout();
                Assert.DoesNotContain(menu.Items.OfType<MenuItem>(), item => Equals(item.Header, RibbonLocalization.GetString(caption)));
                menu.IsOpen = false; Layout();
                menu = null;
            }
            property.SetValue(ribbon, true); Layout();
            menu = OpenMenu(ribbon); Layout();
            Assert.Contains(menu.Items.OfType<MenuItem>(), item => Equals(item.Header, RibbonLocalization.GetString(caption)));
            menu.IsOpen = false; Layout();
            listener.Flush();
            Assert.DoesNotContain("HorizontalContentAlignment", errors.ToString());
            Assert.DoesNotContain("VerticalContentAlignment", errors.ToString());

            ContextMenu OpenMenu(UIElement target)
            {
                var constructor = typeof(ContextMenuEventArgs).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Single(ctor => ctor.GetParameters().Length == 2);
                var args = (ContextMenuEventArgs)constructor.Invoke(new object[] { target, true });
                target.RaiseEvent(args); Layout(); Assert.True(args.Handled);
                return PresentationSource.CurrentSources.Cast<PresentationSource>().OfType<HwndSource>()
                    .SelectMany(source => Descendants(source.RootVisual)).OfType<ContextMenu>().Single(item => item.IsOpen);
            }

            void Layout()
            {
                Sta.Drain(DispatcherPriority.ApplicationIdle); window.UpdateLayout();
                Sta.Drain(DispatcherPriority.ApplicationIdle);
            }
        }
        finally
        {
            if (menu is not null) menu.IsOpen = false;
            window.Close(); Sta.Drain(DispatcherPriority.ApplicationIdle); listener.Flush();
            string directory = Path.Combine(AppContext.BaseDirectory, "TestResults", "showcase");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "context-menu-binding-trace-" + policy + ".txt"), errors.ToString());
            trace.Listeners.Remove(listener); PresentationTraceSources.Refresh(); trace.Switch.Level = previous;
            RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication();
        }
    });

    [Fact]
    public void Feature_selectors_keep_their_owner_while_detached_in_simplified_and_in_qat() => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(showcaseResources: true), RibbonTheme.Office2024);
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        using var errors = new StringWriter();
        using var listener = new TextWriterTraceListener(errors);
        var trace = PresentationTraceSources.DataBindingSource;
        var previous = trace.Switch.Level;
        trace.Listeners.Add(listener);
        PresentationTraceSources.Refresh();
        trace.Switch.Level = SourceLevels.Warning;
        MainWindow? window = null;
        try
        {
            // Prove that diagnostics are live before asserting an empty capture.
            var probe = new TextBlock();
            probe.SetBinding(TextBlock.TextProperty, new Binding("FeatureSelectorTraceProbeMissing") { Source = new object() });
            _ = probe.Text;
            Sta.Drain(); listener.Flush();
            Assert.Contains("FeatureSelectorTraceProbeMissing", errors.ToString());
            errors.GetStringBuilder().Clear();

            window = new MainWindow { Width = 1540, Height = 900, Left = -10000, Top = -10000,
                ShowActivated = false, ShowInTaskbar = false };
            // Do not read or write the user's saved appearance/customization in this test.
            var type = typeof(MainWindow);
            window.Loaded -= (RoutedEventHandler)type.GetMethod("OnWindowLoaded",
                BindingFlags.Instance | BindingFlags.NonPublic)!.CreateDelegate(typeof(RoutedEventHandler), window);
            type.GetField("_restoringAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, true);
            var ribbon = window.MainRibbon;
            var toggle = window.SimplifiedRibbonToggle;
            var density = window.TouchModeSelector;
            Layout(); AssertNoSelectorErrors(); // View is not selected or realized yet.

            Assert.True(ribbon.AddToQuickAccess(toggle));
            Assert.True(ribbon.AddToQuickAccess(density));
            var qatToggle = ribbon.QuickAccessItems.OfType<RibbonToggleButton>()
                .Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), toggle));
            var qatDensity = ribbon.QuickAccessItems.OfType<RibbonDropDownButton>()
                .Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), density));
            CheckPermissions(toggle, density);
            Assert.False(toggle.IsLoaded);

            window.Show(); Layout();
            ribbon.SelectedTab = ribbon.Tabs.Single(tab => Equals(tab.Header, "View"));
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            {
                ribbon.LayoutMode = mode; Layout();
                RibbonToggleButton activeToggle = toggle;
                RibbonDropDownButton activeDensity = density;
                if (mode == RibbonLayoutMode.Simplified)
                {
                    var strip = ((RibbonGroupsHost)ribbon.SelectedTab.Content).SimplifiedToolBar!;
                    activeToggle = strip.Items.OfType<RibbonToggleButton>()
                        .Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), toggle));
                    activeDensity = strip.Items.OfType<RibbonDropDownButton>()
                        .Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), density));
                }
                CheckPermissions(activeToggle, activeDensity);
                qatDensity.IsDropDownOpen = true; Layout();
                Assert.Equal(2, qatDensity.Items.Count);
                qatDensity.IsDropDownOpen = false; Layout();
                Assert.Equal(2, density.Items.Count);
                AssertNoSelectorErrors();
            }

            void CheckPermissions(RibbonToggleButton activeToggle, RibbonDropDownButton activeDensity)
            {
                foreach (bool allowed in new[] { false, true })
                {
                    ribbon.CanUserChangeLayoutMode = allowed;
                    ribbon.CanUserChangeDensity = allowed;
                    Layout();
                    Assert.Equal(allowed, toggle.IsEnabled); Assert.Equal(allowed, activeToggle.IsEnabled);
                    Assert.Equal(allowed, qatToggle.IsEnabled);
                    Assert.Equal(allowed, density.IsEnabled); Assert.Equal(allowed, activeDensity.IsEnabled);
                    Assert.Equal(allowed, qatDensity.IsEnabled);
                }
                ribbon.IsSimplifiedRibbonEnabled = false; Layout();
                Assert.False(toggle.IsEnabled); Assert.False(qatToggle.IsEnabled);
                Assert.True(density.IsEnabled); Assert.True(qatDensity.IsEnabled);
                ribbon.IsSimplifiedRibbonEnabled = true; Layout();
                Assert.True(toggle.IsEnabled); Assert.True(qatToggle.IsEnabled);
            }

            void Layout()
            {
                Sta.Drain(DispatcherPriority.ApplicationIdle);
                window!.UpdateLayout();
                Sta.Drain(DispatcherPriority.ApplicationIdle);
            }

            void AssertNoSelectorErrors()
            {
                listener.Flush();
                foreach (string path in new[] { "IsSimplifiedRibbonEnabled", "CanUserChangeLayoutMode", "CanUserChangeDensity" })
                    Assert.DoesNotContain("BindingExpression:Path=" + path, errors.ToString());
            }
        }
        finally
        {
            window?.Close(); Sta.Drain(DispatcherPriority.ApplicationIdle);
            listener.Flush();
            string directory = Path.Combine(AppContext.BaseDirectory, "TestResults", "showcase");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "feature-selector-binding-trace.txt"), errors.ToString());
            trace.Listeners.Remove(listener); PresentationTraceSources.Refresh(); trace.Switch.Level = previous;
            RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication();
        }
    });

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        yield return root;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
}
