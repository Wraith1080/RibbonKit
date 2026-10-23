using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using RibbonKit.Controls;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Shell;

public sealed partial class MainWindowIntegrationTests
{
    [Theory]
    [InlineData("qat", false, false)]
    [InlineData("qat", true, false)]
    [InlineData("custom", false, false)]
    [InlineData("custom", true, false)]
    [InlineData("overflow", false, false)]
    [InlineData("overflow", true, false)]
    [InlineData("overflow", false, true)]
    [InlineData("overflow", true, true)]
    public async Task CopiedFontPresetFormatsTheRetainedEditorSelection(string placement, bool family, bool caret)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            try
            {
                fixture.Show();
                fixture.Window.Width = 1800;
                var source = Assert.IsType<RibbonComboBox>(fixture.Window.FindName(family ? "FontFamilyCombo" : "FontSizeCombo"));
                RibbonDropDownButton copy;
                if (placement == "custom")
                {
                    var group = new RibbonGroup { Header = "Favorites", CanResize = false };
                    Ribbon.SetIsCustom(group, true);
                    copy = Assert.IsAssignableFrom<RibbonDropDownButton>(typeof(Ribbon).GetMethod("CreateCommandProxy",
                        BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(fixture.Ribbon,
                            new object[] { source, RibbonControlSize.Medium, false }));
                    group.Items.Add(copy);
                    ((RibbonTab)fixture.Window.FindName("HomeTab")).Groups.Add(group);
                }
                else
                {
                    fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
                    Assert.True(fixture.Ribbon.AddToQuickAccess(source));
                    copy = Assert.Single(fixture.Ribbon.QuickAccessItems.OfType<RibbonDropDownButton>(),
                        item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), source));
                    if (placement == "overflow")
                        fixture.Ribbon.QuickAccessMaxWidth = 40;
                }
                fixture.Editor.Document.Blocks.Clear();
                var run = new Run(caret ? "before " : "retain this selection");
                fixture.Editor.Document.Blocks.Add(new Paragraph(run));
                fixture.Editor.Focus();
                fixture.Editor.Selection.Select(caret ? run.ContentEnd : run.ContentStart, run.ContentEnd);
                fixture.Window.EditingController.RefreshState();
                await PumpAsync();
                var start = fixture.Editor.Document.ContentStart.GetOffsetToPosition(fixture.Editor.Selection.Start);
                var end = fixture.Editor.Document.ContentStart.GetOffsetToPosition(fixture.Editor.Selection.End);
                if (placement == "overflow")
                {
                    var toolbar = Assert.Single(FindVisualDescendants<RibbonQuickAccessToolBar>(fixture.Ribbon),
                        bar => bar.IsVisible);
                    Assert.True(toolbar.HasOverflow);
                    var opener = Assert.IsAssignableFrom<ToggleButton>(toolbar.Template.FindName("PART_OverflowButton", toolbar));
                    opener.IsChecked = true;
                    await PumpAsync();
                    var host = Assert.IsAssignableFrom<ItemsControl>(toolbar.Template.FindName("PART_OverflowHost", toolbar));
                    copy = Assert.Single(host.Items.OfType<RibbonDropDownButton>(),
                        item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), source));
                }
                copy.IsDropDownOpen = true;
                await PumpAsync();
                Assert.Equal(start, fixture.Editor.Document.ContentStart.GetOffsetToPosition(fixture.Editor.Selection.Start));
                Assert.Equal(end, fixture.Editor.Document.ContentStart.GetOffsetToPosition(fixture.Editor.Selection.End));
                var choiceIndex = family
                    ? source.Items.OfType<WriterFontChoice>().ToList().FindIndex(choice => choice.SourceName == "Arial")
                    : source.Items.OfType<ComboBoxItem>().ToList().FindIndex(item => Equals(item.Content, "24"));
                Assert.True(choiceIndex >= 0);
                Invoke(Assert.IsAssignableFrom<Button>(copy.Items[choiceIndex]));
                await PumpAsync();
                if (placement == "overflow")
                {
                    var toolbar = Assert.Single(FindVisualDescendants<RibbonQuickAccessToolBar>(fixture.Ribbon), bar => bar.IsVisible);
                    Assert.False(Assert.IsType<Popup>(toolbar.Template.FindName("PART_OverflowPopup", toolbar)).IsOpen);
                    Assert.True(fixture.Editor.IsKeyboardFocusWithin);
                }
                Assert.Equal(caret ? "" : "retain this selection", fixture.Editor.Selection.Text);
                if (family)
                    Assert.Equal("Arial", Assert.IsType<System.Windows.Media.FontFamily>(
                        fixture.Editor.Selection.GetPropertyValue(TextElement.FontFamilyProperty)).Source);
                else
                    Assert.Equal(32d, fixture.Editor.Selection.GetPropertyValue(TextElement.FontSizeProperty));
            }
            finally { fixture.Shell.CurrentDocument.MarkClean(); }
        }, TimeSpan.FromSeconds(30));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task OriginalFontEditingRetainsExplicitCommitBoundaries(bool family, bool simplified)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            try
            {
                fixture.Show(); fixture.Window.Width = 1800;
                if (simplified) { fixture.Ribbon.LayoutMode = RibbonLayoutMode.Simplified; await PumpAsync(); }
                fixture.Editor.Document.Blocks.Clear();
                fixture.Editor.Document.Blocks.Add(new Paragraph(new Run("format these words")));
                fixture.Editor.SelectAll(); fixture.Editor.Focus();
                fixture.Window.EditingController.RefreshState(); await PumpAsync();
                var source = Assert.IsType<RibbonComboBox>(fixture.Window.FindName(family ? "FontFamilyCombo" : "FontSizeCombo"));
                var combo = simplified ? Assert.Single(FindVisualDescendants<RibbonComboBox>(fixture.Ribbon),
                    item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), source)) : source;
                var property = family ? TextElement.FontFamilyProperty : TextElement.FontSizeProperty;
                var original = fixture.Editor.Selection.GetPropertyValue(property);
                combo.ApplyTemplate();
                Assert.True(Assert.IsType<TextBox>(combo.Template.FindName("PART_EditableTextBox", combo)).Focus());
                combo.IsDropDownOpen = true;
                combo.SelectedIndex = family
                    ? source.Items.OfType<WriterFontChoice>().ToList().FindIndex(choice => choice.SourceName == "Arial")
                    : source.Items.OfType<ComboBoxItem>().ToList().FindIndex(item => Equals(item.Content, "24"));
                Assert.Equal(original, fixture.Editor.Selection.GetPropertyValue(property));
                combo.IsDropDownOpen = false; await PumpAsync();
                AssertCurrent(family ? "Arial" : "24");

                combo.ApplyTemplate();
                var textBox = Assert.IsType<TextBox>(combo.Template.FindName("PART_EditableTextBox", combo));
                Assert.True(textBox.Focus());
                textBox.Text = family ? "Ver" : "1";
                fixture.Window.EditingController.RefreshState();
                Assert.Equal(family ? "Ver" : "1", combo.Text);
                AssertCurrent(family ? "Arial" : "24");
                textBox.Text = family ? "Verdana" : "18";
                combo.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(combo),
                    Environment.TickCount, Key.Enter) { RoutedEvent = Keyboard.KeyDownEvent });
                await PumpAsync(); AssertCurrent(family ? "Verdana" : "18");

                Assert.True(textBox.Focus());
                textBox.Text = family ? "Tahoma" : "20";
                Assert.True(fixture.Editor.Focus());
                await PumpAsync(); AssertCurrent(family ? "Tahoma" : "20");

                void AssertCurrent(string expected)
                {
                    var value = fixture.Editor.Selection.GetPropertyValue(property);
                    if (family) Assert.Equal(expected, Assert.IsType<System.Windows.Media.FontFamily>(value).Source);
                    else Assert.Equal(WriterEditingRibbonController.PointsToDips(double.Parse(expected)), value);
                }
            }
            finally { fixture.Shell.CurrentDocument.MarkClean(); }
        }, TimeSpan.FromSeconds(30));
    }
}
