using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class DropDownPressedVisualTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Initially_open_controls_have_pressed_paint_before_the_first_popup_is_created(bool split) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024);
        RibbonDropDownButton button = split ? new RibbonSplitButton() : new RibbonDropDownButton();
        button.Header = "Choices"; button.IsDropDownOpen = true;
        button.Items.Add(new RibbonMenuItem { Header = "Choice" });
        var window = new Window { Content = button, Width = 300, Height = 250,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            button.ApplyTemplate();
            var toggle = Assert.IsType<ToggleButton>(button.Template.FindName("PART_Toggle", button));
            toggle.ApplyTemplate();
            var chrome = Assert.IsType<Border>(toggle.Template.FindName("Chrome", toggle));
            var popup = Assert.IsType<Popup>(button.Template.FindName("PART_Popup", button));
            var paint = new List<Brush>();
            popup.Placement = PlacementMode.Custom;
            popup.CustomPopupPlacementCallback = (size, target, offset) =>
            {
                if (popup.IsOpen) paint.Add(chrome.Background);
                return new[] { new CustomPopupPlacement(new Point(0, target.Height), PopupPrimaryAxis.None) };
            };
            window.Show(); window.UpdateLayout(); Sta.Drain();
            Assert.True(popup.IsOpen); Assert.NotEmpty(paint);
            Assert.All(paint, brush => Assert.Same(button.FindResource("RibbonKit.Brushes.Control.PressedBackground"), brush));
        }
        finally { button.IsDropDownOpen = false; window.Close(); Sta.ResetApplication(); }
    });

    [Fact]
    public void Opening_and_closing_preserve_external_owner_and_opener_bindings() => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024);
        var state = new ToggleButton { IsChecked = false };
        var button = new RibbonDropDownButton { Header = "Choices" };
        button.Items.Add(new RibbonMenuItem { Header = "Choice" });
        button.SetBinding(RibbonDropDownButton.IsDropDownOpenProperty,
            new Binding(nameof(ToggleButton.IsChecked)) { Source = state, Mode = BindingMode.TwoWay });
        var window = new Window { Content = button, Width = 300, Height = 250,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain(); window.UpdateLayout();
            var toggle = Assert.IsType<ToggleButton>(button.Template.FindName("PART_Toggle", button));
            var popup = Assert.IsType<Popup>(button.Template.FindName("PART_Popup", button));
            int checks = 0, unchecks = 0;
            toggle.Checked += (_, _) => checks++;
            toggle.Unchecked += (_, _) => unchecks++;
            Click(); Check(true);
            state.IsChecked = false; Check(false);
            state.IsChecked = true; Check(true);
            Click(); Check(false);
            state.IsChecked = true; Check(true);
            popup.SetCurrentValue(Popup.IsOpenProperty, false); Check(false);
            Assert.Equal(3, checks); Assert.Equal(3, unchecks);

            void Click() => typeof(ToggleButton).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(toggle, null);
            void Check(bool open)
            {
                Sta.Drain();
                Assert.Equal(open, button.IsDropDownOpen); Assert.Equal(open, toggle.IsChecked);
                Assert.Equal(open, state.IsChecked); Assert.Equal(open, popup.IsOpen);
                Assert.True(BindingOperations.IsDataBound(button, RibbonDropDownButton.IsDropDownOpenProperty));
                Assert.True(BindingOperations.IsDataBound(toggle, ToggleButton.IsCheckedProperty));
                Assert.True(BindingOperations.IsDataBound(popup, Popup.IsOpenProperty));
            }
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });

    public static IEnumerable<object[]> Openers()
    {
        foreach (var theme in new[] { RibbonTheme.Office2007, RibbonTheme.Office2010,
            RibbonTheme.Office2024, RibbonTheme.Office2024Flat, RibbonTheme.CrystalLight })
            foreach (int kind in new[] { 0, 1, 2 })
                yield return new object[] { theme, kind };
    }

    [Theory]
    [MemberData(nameof(Openers))]
    public void Opener_has_pressed_paint_during_native_popup_creation(RibbonTheme theme, int kind) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), theme);
        var button = kind == 0 ? new RibbonDropDownButton { Header = "Select" }
            : new RibbonSplitButton { Header = "Paste", Layout = kind == 1
                ? RibbonSplitButtonLayout.Horizontal : RibbonSplitButtonLayout.Vertical };
        button.Items.Add(new RibbonMenuItem { Header = "Choice" });
        var window = new Window { Content = button, Width = 300, Height = 250,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Sta.Drain(); window.UpdateLayout();
            var toggle = Assert.IsType<ToggleButton>(button.Template.FindName("PART_Toggle", button));
            var chrome = Assert.IsType<Border>(toggle.Template.FindName("Chrome", toggle));
            var popup = Assert.IsType<Popup>(button.Template.FindName("PART_Popup", button));
            var placementPaint = new List<(bool Open, Brush Background, Brush Border)>();
            popup.Placement = PlacementMode.Custom;
            popup.CustomPopupPlacementCallback = (size, target, offset) =>
            {
                // Placement runs while the native window is being created, before
                // Popup.Opened. Checking only after Opened misses the transient flash.
                // Paint follows the owner's intent while the IsChecked transfer finishes.
                // Assert outside the binding callback: WPF can swallow its exceptions.
                if (popup.IsOpen) placementPaint.Add((button.IsDropDownOpen, chrome.Background, chrome.BorderBrush));
                return new[] { new CustomPopupPlacement(new Point(0, target.Height), PopupPrimaryAxis.None) };
            };

            Click(); CheckOpening();
            // Closing the native popup must still reach the public two-way property.
            popup.IsOpen = false; Sta.Drain();
            Assert.False(button.IsDropDownOpen); Assert.False(toggle.IsChecked);
            button.IsDropDownOpen = true; CheckOpening();
            Click(); Sta.Drain();
            Assert.False(button.IsDropDownOpen); Assert.False(popup.IsOpen);
            // A quick reopen keeps both bindings and the pressed visual intact.
            button.IsDropDownOpen = true; button.IsDropDownOpen = false; Click(); CheckOpening();
            Assert.True(System.Windows.Data.BindingOperations.IsDataBound(toggle, ToggleButton.IsCheckedProperty));
            Assert.True(System.Windows.Data.BindingOperations.IsDataBound(popup, Popup.IsOpenProperty));

            void Click() => typeof(ToggleButton).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(toggle, null);
            void CheckOpening()
            {
                Sta.Drain();
                Assert.True(button.IsDropDownOpen); Assert.True(popup.IsOpen); Assert.True(toggle.IsChecked);
                Assert.NotEmpty(placementPaint);
                foreach (var paint in placementPaint)
                {
                    Assert.True(paint.Open);
                    Assert.Same(button.FindResource("RibbonKit.Brushes.Control.PressedBackground"), paint.Background);
                    Assert.Same(button.FindResource("RibbonKit.Brushes.Control.PressedBorder"), paint.Border);
                }
                placementPaint.Clear();
            }
        }
        finally { window.Close(); Sta.ResetApplication(); }
    });
}
