using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Showcase;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class RtlSelectionPreviewBindingTests
{
    [Fact]
    public void Preview_tracks_font_and_style_before_tab_realization_and_through_qat_selection() => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(showcaseResources: true), RibbonTheme.Office2024);
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        using var errors = new StringWriter();
        using var listener = new TextWriterTraceListener(errors);
        var trace = PresentationTraceSources.DataBindingSource;
        var previous = trace.Switch.Level;
        trace.Listeners.Add(listener); PresentationTraceSources.Refresh(); trace.Switch.Level = SourceLevels.Warning;
        LocalizationRtlDemo? window = null;
        try
        {
            var probe = new TextBlock();
            probe.SetBinding(TextBlock.TextProperty, new Binding("RtlSelectionTraceProbeMissing") { Source = new object() });
            _ = probe.Text; Sta.Drain(); listener.Flush();
            Assert.Contains("RtlSelectionTraceProbeMissing", errors.ToString());
            errors.GetStringBuilder().Clear();

            window = new LocalizationRtlDemo { Width = 1500, Height = 700, Left = -10000, Top = -10000,
                ShowActivated = false, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual };
            Layout(); AssertNoPreviewErrors();
            var ribbon = window.DemoRibbon;
            var combo = window.RtlFontCombo;
            var gallery = window.RtlStylesGallery;
            var previewGroup = window.RtlChoicesTab.Groups.Single(group => Ribbon.GetCommandId(group) == "loc.group.choicepreview");
            var preview = Assert.IsType<StackPanel>(Assert.Single(previewGroup.Items)).Children.OfType<TextBlock>().ToArray();
            Assert.Equal(3, preview.Length);
            Assert.False(combo.IsLoaded); AssertPreview();
            combo.SelectedIndex = 2; gallery.SelectedIndex = 1; Layout(); AssertPreview();

            Assert.True(ribbon.AddToQuickAccess(combo)); Assert.True(ribbon.AddToQuickAccess(gallery));
            var qatCombo = ribbon.QuickAccessItems.OfType<RibbonDropDownButton>()
                .Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), combo));
            var qatGallery = ribbon.QuickAccessItems.OfType<RibbonDropDownButton>()
                .Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), gallery));
            window.Show(); Layout();
            foreach (bool rtl in new[] { false, true })
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            {
                window.RightToLeftToggle.IsChecked = rtl;
                ribbon.LayoutMode = mode; ribbon.SelectedTab = window.RtlChoicesTab; Layout();
                combo.SelectedIndex = 1; gallery.SelectedIndex = 4; Layout(); AssertPreview();
                ribbon.SelectedTab = ribbon.Tabs[0]; Layout();

                // Select through QAT while the source tab and its preview are detached.
                qatCombo.IsDropDownOpen = true; Layout(); Assert.True(qatCombo.IsDropDownOpen);
                Assert.IsAssignableFrom<Button>(qatCombo.Items[2]).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                Layout(); Assert.Equal(2, combo.SelectedIndex); Assert.False(qatCombo.IsDropDownOpen); AssertPreview();
                qatGallery.IsDropDownOpen = true; Layout(); Assert.True(qatGallery.IsDropDownOpen);
                Assert.IsType<RibbonGalleryItem>(gallery.Items[1]).IsSelected = true; Layout();
                Assert.Equal(1, gallery.SelectedIndex); AssertPreview();
                qatGallery.IsDropDownOpen = false; Layout();
                ribbon.SelectedTab = window.RtlChoicesTab; Layout(); AssertPreview(); AssertNoPreviewErrors();
            }

            void AssertPreview()
            {
                string font = Assert.IsType<string>(Assert.IsType<ComboBoxItem>(combo.SelectedItem).Content);
                Assert.Equal("Font: " + font, preview[0].Text);
                Assert.Equal("Style: " + Assert.IsType<string>(gallery.SelectedValue), preview[1].Text);
                Assert.Equal(font, preview[2].FontFamily.Source);
            }

            void Layout()
            {
                Sta.Drain(DispatcherPriority.ApplicationIdle); window!.UpdateLayout();
                Sta.Drain(DispatcherPriority.ApplicationIdle);
            }

            void AssertNoPreviewErrors()
            {
                listener.Flush();
                Assert.DoesNotContain("ElementName=RtlFontCombo", errors.ToString());
                Assert.DoesNotContain("ElementName=RtlStylesGallery", errors.ToString());
            }
        }
        finally
        {
            window?.Close(); Sta.Drain(DispatcherPriority.ApplicationIdle); listener.Flush();
            string directory = Path.Combine(AppContext.BaseDirectory, "TestResults", "showcase");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "rtl-selection-preview-binding-trace.txt"), errors.ToString());
            trace.Listeners.Remove(listener); PresentationTraceSources.Refresh(); trace.Switch.Level = previous;
            RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication();
        }
    });
}
