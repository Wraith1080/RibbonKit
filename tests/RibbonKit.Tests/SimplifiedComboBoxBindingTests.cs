using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed partial class SimplifiedCustomizationTests
{
    [Fact]
    public void Combo_cleanup_restores_reused_item_styling_and_preserves_authored_alignment_values() => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024);
        var combo = new RibbonComboBox { ItemsSource = new[] { "First", "Second" }, SelectedIndex = 0 };
        var window = new Window { Content = combo, Width = 400, Height = 200, ShowActivated = false, ShowInTaskbar = false, Left = -10000, Top = -10000 };
        try
        {
            window.Show(); combo.IsDropDownOpen = true; Layout();
            var generated = Assert.IsType<ComboBoxItem>(combo.ItemContainerGenerator.ContainerFromIndex(0));
            combo.ItemsSource = new[] { "Replacement" }; Layout();
            combo.ItemsSource = null; combo.Items.Add(generated); combo.SelectedItem = generated; Layout();
            Assert.NotNull(generated.Template); Assert.True(generated.ActualHeight > 0);
            Assert.Equal(DependencyProperty.UnsetValue, generated.ReadLocalValue(FrameworkElement.OverridesDefaultStyleProperty));
            var authored = new ComboBoxItem { Content = "Authored", HorizontalContentAlignment = HorizontalAlignment.Right,
                VerticalContentAlignment = VerticalAlignment.Bottom, OverridesDefaultStyle = false };
            combo.Items.Add(authored); Layout(); combo.Items.Remove(authored); Layout();
            Assert.Equal(HorizontalAlignment.Right, authored.HorizontalContentAlignment);
            Assert.Equal(VerticalAlignment.Bottom, authored.VerticalContentAlignment); Assert.False(authored.OverridesDefaultStyle);
        }
        finally { window.Close(); Sta.Drain(); Sta.ResetApplication(); }
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    [Theory]
    [InlineData(RibbonTheme.Office2024, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.Office2010, FlowDirection.LeftToRight)]
    [InlineData(RibbonTheme.CrystalLight, FlowDirection.RightToLeft)]
    public void Moved_tab_apply_does_not_leave_combo_item_alignment_bindings_without_an_owner(RibbonTheme theme, FlowDirection direction) => Sta.Run(() =>
    {
        using var host = new Host(); var custom = AddMoveTab(host, "Tools", "tools", true);
        ThemeManager.Apply(Application.Current, theme); host.Page.FlowDirection = direction;
        host.Page.CancelChanges(); SelectMoveTab(host, custom);
        var source = host.Part<RibbonComboBox>("PART_SourceTabSelector");
        var destination = host.Part<RibbonComboBox>("PART_TabSelector");
        // Generate native popup containers first, as after selecting a custom tab in Options.
        foreach (var selector in new[] { source, destination })
        { selector.IsDropDownOpen = true; host.Layout(); selector.IsDropDownOpen = false; host.Layout(); }
        using var errors = new StringWriter(); using var listener = new TextWriterTraceListener(errors);
        var trace = PresentationTraceSources.DataBindingSource; var previous = trace.Switch.Level;
        trace.Switch.Level = SourceLevels.Warning; trace.Listeners.Add(listener);
        PresentationTraceSources.Refresh(); trace.Switch.Level = SourceLevels.Warning;
        try
        {
            var probe = new TextBlock(); probe.SetBinding(TextBlock.TextProperty, new Binding("AlignmentTraceProbeMissing") { Source = new object() });
            _ = probe.Text; host.Layout(); listener.Flush(); Assert.Contains("AlignmentTraceProbeMissing", errors.ToString());
            var item = MoveMenuItem(host);
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var dialog = Application.Current.Windows.OfType<RibbonCustomizeMoveTabDialog>().Single();
                try
                {
                    var position = (RibbonComboBox)dialog.Template.FindName("PART_PositionSelector", dialog);
                    var target = (RibbonComboBox)dialog.Template.FindName("PART_TargetTabSelector", dialog);
                    foreach (var selector in new[] { position, target })
                    { selector.IsDropDownOpen = true; host.Layout(); selector.IsDropDownOpen = false; host.Layout(); }
                    position.SelectedIndex = 0;
                    ((ButtonBase)dialog.Template.FindName("PART_OkButton", dialog)).RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                }
                finally { if (dialog.IsVisible) dialog.Close(); }
            }));
            item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); host.Part<Button>("PART_EditButton").ContextMenu.IsOpen = false; host.Layout();
            foreach (var selector in new[] { source, destination })
            { selector.IsDropDownOpen = true; host.Layout(); selector.IsDropDownOpen = false; host.Layout(); }
            var containers = new[] { source, destination }.SelectMany(selector => selector.Items.Cast<object>()
                .Select(item => selector.ItemContainerGenerator.ContainerFromItem(item))).OfType<ComboBoxItem>().ToArray();
            Assert.NotEmpty(containers);
            errors.GetStringBuilder().Clear();
            host.Page.ApplyChanges(); host.Layout(); listener.Flush();
            foreach (var container in containers)
            {
                _ = container.HorizontalContentAlignment;
                _ = container.VerticalContentAlignment;
                BindingOperations.GetBindingExpression(container, Control.HorizontalContentAlignmentProperty)?.UpdateTarget();
                BindingOperations.GetBindingExpression(container, Control.VerticalContentAlignmentProperty)?.UpdateTarget();
            }
            host.Layout(); listener.Flush();
            string captured = errors.ToString();
            string path = Path.Combine(AppContext.BaseDirectory, "TestResults", "simplified-customization"); Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "move-apply-binding-trace-" + theme + ".txt"), captured);
            Assert.DoesNotContain("HorizontalContentAlignment", captured); Assert.DoesNotContain("VerticalContentAlignment", captured);
            Assert.Same(custom, host.Ribbon.Tabs[0]);
        }
        finally { trace.Listeners.Remove(listener); PresentationTraceSources.Refresh(); trace.Switch.Level = previous; }
    });
}
