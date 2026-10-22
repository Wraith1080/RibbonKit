using System.Windows.Controls;
using System.Windows.Data;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed class SimplifiedComboReadOnlyLabelTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Display_labels_are_one_way_while_selection_remains_source_owned(bool authoredContainers) => Sta.Run(() =>
    {
        var source = new RibbonComboBox { IsEditable = true };
        if (authoredContainers)
        {
            foreach (string label in new[] { "Calibri", "Arial" })
            {
                var item = new ComboBoxItem { Content = label };
                TextSearch.SetText(item, label);
                source.Items.Add(item);
            }
        }
        else
        {
            source.DisplayMemberPath = nameof(Choice.DisplayName);
            source.ItemsSource = new[] { new Choice("Calibri"), new Choice("Arial") };
        }
        source.SelectedIndex = 0;
        var proxy = new RibbonComboBoxSimplifiedProxy(source);
        var row = Assert.IsType<ComboBoxItem>(proxy.Items[0]);
        Assert.Equal("Calibri", TextSearch.GetText(row));
        Assert.Equal(BindingMode.OneWay, BindingOperations.GetBinding(row, TextSearch.TextProperty)!.Mode);
        proxy.SelectedIndex = 1;
        Assert.Equal(1, source.SelectedIndex);
        Assert.Equal("Arial", source.Text);
        Assert.Equal("Calibri", TextSearch.GetText(row));
    });

    private sealed record Choice(string DisplayName);
}
