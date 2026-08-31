using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterFormattingDialogTests
{
    [Fact]
    public void ParagraphDialogUsesFixedOwnerCenteredSurfaceAndPreservesUnsetState()
    {
        StaTestHelper.Run(() =>
        {
            var dialog = new WriterParagraphDialog();
            try
            {
                Assert.Equal(ResizeMode.NoResize, dialog.ResizeMode);
                Assert.Equal(SizeToContent.Height, dialog.SizeToContent);
                Assert.Equal(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);
                Assert.Equal(dialog.MinWidth, dialog.MaxWidth);
                Assert.Equal("WriterParagraphDialog", AutomationProperties.GetAutomationId(dialog));
                Assert.Null(dialog.Result);
                Assert.Same(dialog.TryFindResource("RibbonKit.Brushes.Control.SurfaceBackground"),
                    dialog.Background);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    [Fact]
    public void ParagraphDialogMeasuresAllContentInsideItsClientAreaAtCurrentDpi()
    {
        StaTestHelper.Run(() =>
        {
            var dialog = new WriterParagraphDialog
            {
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false
            };
            try
            {
                dialog.Show();
                dialog.UpdateLayout();
                dialog.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
                dialog.UpdateLayout();

                var root = Assert.IsType<Grid>(dialog.FindName("LayoutRoot"));
                var previewSurface = Assert.IsType<Border>(dialog.FindName("PreviewSurface"));
                var preview = Assert.IsType<StackPanel>(dialog.FindName("PreviewText"));
                var cancel = Assert.IsType<Button>(dialog.FindName("CancelButton"));
                var dpi = VisualTreeHelper.GetDpi(dialog);

                Assert.True(dpi.DpiScaleX > 0);
                Assert.True(dpi.DpiScaleY > 0);
                Assert.True(root.ActualHeight + root.Margin.Top + root.Margin.Bottom + 0.5 >=
                    root.DesiredSize.Height,
                    $"Root actual {root.ActualHeight:0.##}, desired {root.DesiredSize.Height:0.##}, " +
                    $"window {dialog.ActualHeight:0.##} at DPI {dpi.DpiScaleY:0.##}.");
                Assert.True(preview.ActualHeight + 0.5 >= preview.DesiredSize.Height,
                    $"Preview actual {preview.ActualHeight:0.##}, desired {preview.DesiredSize.Height:0.##}.");

                var previewBounds = preview.TransformToAncestor(root)
                    .TransformBounds(new Rect(preview.RenderSize));
                var surfaceBounds = previewSurface.TransformToAncestor(root)
                    .TransformBounds(new Rect(previewSurface.RenderSize));
                var cancelBounds = cancel.TransformToAncestor(root)
                    .TransformBounds(new Rect(cancel.RenderSize));
                Assert.True(previewBounds.Bottom <= surfaceBounds.Bottom - previewSurface.Padding.Bottom + 0.5);
                Assert.True(cancelBounds.Bottom <= root.ActualHeight + 0.5);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    [Fact]
    public void ThemedFontDialogValidatesEditableValuesAndReturnsFontEffectsAndColor()
    {
        StaTestHelper.Run(() =>
        {
            var initial = new WriterFontDialogResult(
                new FontFamily("Arial"),
                11,
                FontStyles.Normal,
                FontWeights.Normal,
                Underline: false,
                WriterStrikethroughStyle.None,
                WriterBaselineEffect.Normal,
                Color.FromRgb(0x12, 0x34, 0x56));
            var catalog = new WriterFontCatalog(
                () => [new FontFamily("Segoe UI")]);
            var dialog = new WriterFontDialog(initial, catalog);
            try
            {
                Assert.Equal(ResizeMode.NoResize, dialog.ResizeMode);
                Assert.Equal(SizeToContent.Height, dialog.SizeToContent);
                Assert.IsType<RibbonKit.Controls.RibbonComboBox>(dialog.FindName("FontFamilyBox"));
                Assert.IsType<RibbonKit.Controls.RibbonComboBox>(dialog.FindName("FontStyleBox"));
                Assert.IsType<RibbonKit.Controls.RibbonComboBox>(dialog.FindName("FontSizeBox"));

                var family = Assert.IsAssignableFrom<ComboBox>(dialog.FindName("FontFamilyBox"));
                var style = Assert.IsAssignableFrom<ComboBox>(dialog.FindName("FontStyleBox"));
                var size = Assert.IsAssignableFrom<ComboBox>(dialog.FindName("FontSizeBox"));
                var underline = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(
                    dialog.FindName("UnderlineBox"));
                var strike = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(
                    dialog.FindName("StrikethroughBox"));
                var doubleStrike = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(
                    dialog.FindName("DoubleStrikethroughBox"));
                var superscript = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(
                    dialog.FindName("SuperscriptBox"));
                var subscript = Assert.IsAssignableFrom<System.Windows.Controls.Primitives.ToggleButton>(
                    dialog.FindName("SubscriptBox"));
                var ok = Assert.IsType<Button>(dialog.FindName("OkButton"));
                Assert.NotNull(ok.Style);
                Assert.Contains(family.Items.Cast<WriterFontChoice>(), choice =>
                    string.Equals(choice.SourceName, "Arial", StringComparison.OrdinalIgnoreCase));

                family.Text = "Not installed";
                Assert.False(ok.IsEnabled);
                family.Text = "Segoe UI";
                size.Text = "18";
                style.SelectedIndex = 3;
                underline.IsChecked = true;
                strike.IsChecked = true;
                doubleStrike.IsChecked = true;
                superscript.IsChecked = true;
                Assert.True(ok.IsEnabled);
                Assert.False(strike.IsChecked);
                Assert.False(subscript.IsChecked);

                Assert.True(ShowHiddenModal(dialog, () =>
                    ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
                var result = Assert.IsType<WriterFontDialogResult>(dialog.Result);
                Assert.Equal("Segoe UI", result.Family.Source);
                Assert.Equal(18, result.SizePoints);
                Assert.Equal(FontStyles.Italic, result.Style);
                Assert.Equal(FontWeights.Bold, result.Weight);
                Assert.True(result.Underline);
                Assert.Equal(WriterStrikethroughStyle.Double, result.Strikethrough);
                Assert.Equal(WriterBaselineEffect.Superscript, result.BaselineEffect);
                Assert.Equal(initial.Color, result.Color);
                Assert.True(WriterFontDialog.AreEquivalent(result,
                    result with { Family = new FontFamily("SEGOE UI"), SizePoints = 18.004 }));
                Assert.False(WriterFontDialog.AreEquivalent(result, result with { SizePoints = 20 }));
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    [Fact]
    public void ThemedColorDialogAcceptsExactHexAndRgbValues()
    {
        StaTestHelper.Run(() =>
        {
            Assert.True(WriterColorDialog.TryParseHex("#123456", out var parsed));
            Assert.Equal(Color.FromRgb(0x12, 0x34, 0x56), parsed);
            Assert.False(WriterColorDialog.TryParseHex("123456", out _));
            Assert.False(WriterColorDialog.TryParseHex("#GG0000", out _));
            Assert.Equal(Colors.Red, WriterColorDialog.FromHsv(0, 1, 1));
            Assert.Equal(Colors.Lime, WriterColorDialog.FromHsv(120, 1, 1));
            Assert.Equal(Colors.Blue, WriterColorDialog.FromHsv(240, 1, 1));
            WriterColorDialog.ToHsv(parsed, out var hue, out var saturation, out var brightness);
            Assert.Equal(parsed, WriterColorDialog.FromHsv(hue, saturation, brightness));

            var dialog = new WriterColorDialog(Colors.Black);
            try
            {
                Assert.Equal(ResizeMode.NoResize, dialog.ResizeMode);
                Assert.Equal(SizeToContent.Height, dialog.SizeToContent);
                Assert.IsType<RibbonKit.Controls.RibbonTextBox>(dialog.FindName("HexBox"));
                Assert.IsType<RibbonKit.Controls.RibbonTextBox>(dialog.FindName("RedBox"));
                Assert.IsType<Border>(dialog.FindName("SaturationValueSurface"));
                Assert.IsType<Border>(dialog.FindName("HueSurface"));
                Assert.InRange(Assert.IsType<WrapPanel>(dialog.FindName("PalettePanel")).Children.Count,
                    1, 10);

                var hex = Assert.IsAssignableFrom<TextBox>(dialog.FindName("HexBox"));
                var ok = Assert.IsType<Button>(dialog.FindName("OkButton"));
                Assert.NotNull(ok.Style);
                hex.Text = "#ABCDEF";
                Assert.True(ok.IsEnabled);

                Assert.True(ShowHiddenModal(dialog, () =>
                    ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
                Assert.Equal(Color.FromRgb(0xAB, 0xCD, 0xEF), dialog.Result);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    [Fact]
    public void ThemedFontAndColorDialogsKeepControlsInsideTheirDpiSizedClientArea()
    {
        StaTestHelper.Run(() =>
        {
            var catalog = new WriterFontCatalog(() => [new FontFamily("Segoe UI")]);
            var font = new WriterFontDialog(
                new WriterFontDialogResult(
                    new FontFamily("Segoe UI"), 11, FontStyles.Normal, FontWeights.Normal,
                    false, WriterStrikethroughStyle.None, WriterBaselineEffect.Normal, Colors.Black),
                catalog)
            {
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false
            };
            var color = new WriterColorDialog(Colors.Blue)
            {
                Left = -10000,
                Top = -10000,
                Opacity = 0,
                ShowInTaskbar = false
            };
            try
            {
                font.Show();
                font.UpdateLayout();
                font.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
                font.UpdateLayout();
                var fontRoot = Assert.IsType<Grid>(font.FindName("LayoutRoot"));
                var typeface = Assert.IsType<Border>(font.FindName("TypefaceSection"));
                var family = Assert.IsAssignableFrom<ComboBox>(font.FindName("FontFamilyBox"));
                var familyBounds = family.TransformToAncestor(fontRoot)
                    .TransformBounds(new Rect(family.RenderSize));
                var typefaceBounds = typeface.TransformToAncestor(fontRoot)
                    .TransformBounds(new Rect(typeface.RenderSize));
                Assert.True(familyBounds.Right <= typefaceBounds.Right - typeface.Padding.Right + 0.5,
                    $"Font family right {familyBounds.Right:0.##}, card content right " +
                    $"{typefaceBounds.Right - typeface.Padding.Right:0.##}.");

                color.Show();
                color.UpdateLayout();
                color.Dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(() => { }));
                color.UpdateLayout();
                var colorRoot = Assert.IsType<Grid>(color.FindName("LayoutRoot"));
                var cancel = Assert.IsType<Button>(color.FindName("CancelButton"));
                var hueSurface = Assert.IsType<Border>(color.FindName("HueSurface"));
                var hueIndicator = Assert.IsType<Border>(color.FindName("HueIndicator"));
                var cancelBounds = cancel.TransformToAncestor(colorRoot)
                    .TransformBounds(new Rect(cancel.RenderSize));
                var hueIndicatorBounds = hueIndicator.TransformToAncestor(hueSurface)
                    .TransformBounds(new Rect(hueIndicator.RenderSize));
                Assert.True(cancelBounds.Bottom <= colorRoot.ActualHeight + 0.5);
                Assert.True(colorRoot.ActualHeight + colorRoot.Margin.Top + colorRoot.Margin.Bottom + 0.5 >=
                            colorRoot.DesiredSize.Height);
                Assert.InRange(Math.Abs(
                    (hueIndicatorBounds.Top + hueIndicatorBounds.Height / 2) - hueSurface.ActualHeight / 2),
                    0, 0.5);
            }
            finally
            {
                color.Close();
                font.Close();
            }
        });
    }

    [Fact]
    public void ParagraphMeasurementsUseEditablePointPresetCombosWithVisibleUnits()
    {
        StaTestHelper.Run(() =>
        {
            var dialog = new WriterParagraphDialog();
            try
            {
                var fields = new[]
                {
                    Assert.IsAssignableFrom<ComboBox>(dialog.FindName("LeftIndentBox")),
                    Assert.IsAssignableFrom<ComboBox>(dialog.FindName("RightIndentBox")),
                    Assert.IsAssignableFrom<ComboBox>(dialog.FindName("SpecialByBox")),
                    Assert.IsAssignableFrom<ComboBox>(dialog.FindName("SpacingBeforeBox")),
                    Assert.IsAssignableFrom<ComboBox>(dialog.FindName("SpacingAfterBox"))
                };
                foreach (var field in fields)
                {
                    Assert.True(field.IsEditable);
                    Assert.False(field.IsTextSearchEnabled);
                    Assert.True(field.Items.Count >= 8);
                    Assert.Contains("0", field.Items.Cast<string>());
                    Assert.Contains("12", field.Items.Cast<string>());
                }

                foreach (var unitName in new[]
                         {
                             "LeftIndentUnit", "RightIndentUnit", "SpecialByUnit",
                             "SpacingBeforeUnit", "SpacingAfterUnit"
                         })
                    Assert.Equal("pt", Assert.IsType<TextBlock>(dialog.FindName(unitName)).Text);

                var left = fields[0];
                left.Text = "12.5";
                Assert.Equal("12.5", left.Text);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    [Fact]
    public void ParagraphDialogRejectsInvalidNumbersAndReturnsAnImmutableResult()
    {
        StaTestHelper.Run(() =>
        {
            var dialog = new WriterParagraphDialog();
            try
            {
                var alignment = Assert.IsAssignableFrom<ComboBox>(dialog.FindName("AlignmentBox"));
                var left = Assert.IsAssignableFrom<ComboBox>(dialog.FindName("LeftIndentBox"));
                var special = Assert.IsAssignableFrom<ComboBox>(dialog.FindName("SpecialBox"));
                var by = Assert.IsAssignableFrom<ComboBox>(dialog.FindName("SpecialByBox"));
                var before = Assert.IsAssignableFrom<ComboBox>(dialog.FindName("SpacingBeforeBox"));
                var ok = Assert.IsType<Button>(dialog.FindName("OkButton"));
                Assert.NotNull(ok.Style);

                left.Text = "-1";
                Assert.False(ok.IsEnabled);
                left.Text = "Infinity";
                Assert.False(ok.IsEnabled);
                left.Text = "12.5";
                alignment.SelectedIndex = 3;
                special.SelectedIndex = 1;
                by.Text = "6";
                before.Text = "4";
                Assert.True(ok.IsEnabled);

                Assert.True(ShowHiddenModal(dialog, () =>
                    ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
                var result = Assert.IsType<WriterParagraphDialogResult>(dialog.Result);
                Assert.Equal(TextAlignment.Justify, result.Alignment);
                Assert.Equal(12.5d, result.LeftIndent.GetValueOrDefault());
                Assert.Equal(6d, result.SpecialBy.GetValueOrDefault());
                Assert.True(result.Hanging == false);
                Assert.Equal(4d, result.SpacingBefore.GetValueOrDefault());
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    [Fact]
    public void ParagraphApplyRaisesAfterCommitAndOkCommitsSeparately()
    {
        StaTestHelper.Run(() =>
        {
            var dialog = new WriterParagraphDialog();
            var apply = Assert.IsType<Button>(dialog.FindName("ApplyButton"));
            var ok = Assert.IsType<Button>(dialog.FindName("OkButton"));
            var left = Assert.IsAssignableFrom<ComboBox>(dialog.FindName("LeftIndentBox"));
            var eventSawResult = false;
            var eventSawOpenWindow = false;
            var appliedCount = 0;
            dialog.Applied += (_, _) =>
            {
                appliedCount++;
                eventSawResult = dialog.Result?.LeftIndent == 9d;
                eventSawOpenWindow = dialog.IsVisible;
                left.Text = "10";
                dialog.Dispatcher.BeginInvoke(DispatcherPriority.Input,
                    new Action(() => ok.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
            };
            left.Text = "9";

            try
            {
                Assert.True(ShowHiddenModal(dialog, () =>
                    apply.RaiseEvent(new RoutedEventArgs(Button.ClickEvent))));
                Assert.Equal(1, appliedCount);
                Assert.True(eventSawResult);
                Assert.True(eventSawOpenWindow);
                Assert.Equal(10d, dialog.Result?.LeftIndent ?? double.NaN);
            }
            finally
            {
                dialog.Close();
            }
        });
    }

    private static bool ShowHiddenModal(Window dialog, Action accept)
    {
        dialog.Left = -10000;
        dialog.Top = -10000;
        dialog.Opacity = 0;
        dialog.Loaded += OnLoaded;
        return dialog.ShowDialog() == true;

        void OnLoaded(object sender, RoutedEventArgs e) =>
            dialog.Dispatcher.BeginInvoke(DispatcherPriority.Input, accept);
    }
}
