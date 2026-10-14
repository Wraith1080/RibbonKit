using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Localization;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class SimplifiedHostIntegrationTests
{
    [Fact]
    public void Rtl_customize_request_opens_current_editor_and_qat_keeps_both_editors() => Sta.Run(() =>
    {
        var app = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(app, RibbonTheme.Office2024);
        var demo = new LocalizationRtlDemo
        { Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false };
        try
        {
            demo.Show();
            demo.RightToLeftToggle.IsChecked = true;
            demo.DemoRibbon.LayoutMode = RibbonLayoutMode.Simplified;
            Sta.Drain();
            var tab = demo.DemoRibbon.SelectedTab!;
            Assert.Contains(demo.DemoRibbon.GetSimplifiedChoices(tab), choice => choice.Source == demo.RightToLeftToggle);
            Assert.Contains(demo.DemoRibbon.GetSimplifiedChoices(tab), choice => choice.Source == demo.TouchModeSelector);

            Exception? failure = null;
            bool reviewed = false;
            demo.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                var dialog = demo.OwnedWindows.OfType<RibbonOptionsDialog>().Single();
                try
                {
                    var editor = Assert.IsType<RibbonSimplifiedCustomizePage>(dialog.SelectedPage!.Content);
                    Assert.Same(demo.DemoRibbon, editor.Ribbon);
                    Assert.False(string.IsNullOrEmpty(editor.ResetLayout));
                    Assert.True(editor.IsEnabled);
                    Assert.Equal(FlowDirection.RightToLeft, dialog.FlowDirection);
                    reviewed = true;
                }
                catch (Exception ex) { failure = ex; }
                finally { dialog.Close(); }
            }));
            var menu = demo.DemoRibbon.CreateRibbonContextMenu(null);
            var customize = menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header,
                RibbonLocalization.GetString(RibbonString.CustomizeRibbon)));
            customize.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            Assert.True(reviewed);

            var options = demo.CreateOptionsDialog(true);
            try
            {
                options.Show();
                Sta.Drain();
                Assert.IsType<RibbonQuickAccessPage>(options.SelectedPage!.Content);
                var simplified = options.Pages.Single(page => page.Content is RibbonSimplifiedCustomizePage);
                var classic = options.Pages.Single(page => page.Content is RibbonCustomizePage);
                Assert.Equal(Visibility.Visible, simplified.Visibility);
                Assert.Equal(Visibility.Collapsed, classic.Visibility);
                options.SelectedPage = simplified;
                demo.DemoRibbon.LayoutMode = RibbonLayoutMode.Classic;
                Sta.Drain();
                Assert.Same(classic, options.SelectedPage);
                Assert.Equal(Visibility.Visible, classic.Visibility);
            }
            finally { options.Close(); }
        }
        finally { demo.Close(); Sta.ResetApplication(); }
    });

    [Fact]
    public void Mdi_defaults_project_window_and_merged_document_commands_without_changing_targets() => Sta.Run(() =>
    {
        var app = Sta.UseApplication(showcaseResources: true);
        ThemeManager.Apply(app, RibbonTheme.Office2024);
        var demo = new MdiDemo
        { Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false };
        try
        {
            demo.Show();
            demo.DemoRibbon.LayoutMode = RibbonLayoutMode.Simplified;
            var windowTab = demo.DemoRibbon.Tabs.Single(tab => Equals(tab.Header, "Window"));
            demo.DemoRibbon.SelectedTab = windowTab;
            Sta.Drain();
            Assert.Equal(5, demo.DemoRibbon.GetSimplifiedChoices(windowTab).Count);
            Assert.Equal(5, ((RibbonGroupsHost)windowTab.Content).SimplifiedToolBar!.Items.OfType<RibbonButton>().Count());

            var documents = demo.Mdi.Items.OfType<MdiDocument>().ToArray();
            foreach (var document in documents.Concat(documents.Take(1)))
            {
                demo.Mdi.ActivateDocument(document);
                var tab = document.MergeSource!.Tabs.Single();
                demo.DemoRibbon.SelectedTab = tab;
                Sta.Drain();
                demo.UpdateLayout();
                var source = Assert.IsType<RibbonButton>(Assert.Single(tab.Groups.Single().Items));
                Assert.Equal(RibbonSimplifiedPlacement.Inline, Ribbon.GetSimplifiedPlacement(source));
                var proxy = ((RibbonGroupsHost)tab.Content).SimplifiedToolBar!.Items.OfType<RibbonButton>()
                    .Single(button => Ribbon.GetQuickAccessSource(button) == source);
                Assert.True(proxy.IsVisible);
                var editor = Assert.IsType<TextBox>(document.Content);
                editor.Text = "mixed case";
                var other = Assert.IsType<TextBox>(documents.Single(item => item != document).Content);
                string otherText = other.Text;
                proxy.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, proxy));
                Sta.Drain();
                Assert.Equal("MIXED CASE", editor.Text);
                Assert.Equal(otherText, other.Text);
            }
        }
        finally { demo.Close(); Sta.ResetApplication(); }
    });
}
