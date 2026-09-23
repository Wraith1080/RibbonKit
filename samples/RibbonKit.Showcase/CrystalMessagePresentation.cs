using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;

namespace RibbonKit.Showcase;

/// <summary>Styles the existing message parts without replacing their behavior or template.</summary>
internal sealed class CrystalMessagePresentation
{
    private readonly RibbonMessageBar _bar;
    private readonly Dictionary<Button, Style> _originalActions = new();
    private bool _enabled;

    public CrystalMessagePresentation(RibbonMessageBar bar)
    {
        _bar = bar;
        bar.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("/RibbonKit.Showcase;component/Themes/Crystal.Backstage.xaml", UriKind.Relative) });
        foreach (RibbonMessage message in bar.Items)
        {
            message.Loaded += (_, _) => Update(message);
            message.IsVisibleChanged += (_, _) =>
            {
                if (!message.IsVisible) return;
                message.ApplyTemplate();
                Update(message);
            };
        }
    }

    public void Apply(bool enabled)
    {
        _enabled = enabled;
        foreach (RibbonMessage message in _bar.Items)
            if (message.IsLoaded) Update(message);
    }

    private void Update(RibbonMessage message)
    {
        if (message.Template?.FindName("PART_Root", message) is not Border root) return;
        if (_enabled)
        {
            root.SetResourceReference(Border.CornerRadiusProperty, "Crystal.Metrics.MessageCornerRadius");
            message.Margin = new Thickness(0, 2, 0, 2);
        }
        else
        {
            root.ClearValue(Border.CornerRadiusProperty);
            message.ClearValue(FrameworkElement.MarginProperty);
        }
        if (message.Template.FindName("PART_ActionButton", message) is not Button action) return;
        if (_enabled)
        {
            _originalActions.TryAdd(action, action.Style);
            action.SetResourceReference(FrameworkElement.StyleProperty, "Crystal.Backstage.Action");
        }
        else if (_originalActions.Remove(action, out var original)) action.Style = original;
    }
}
