using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class ShowcaseFeatureBindingTests
{
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
}
