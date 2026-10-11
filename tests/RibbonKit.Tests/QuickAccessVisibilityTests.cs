using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Data;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed class QuickAccessVisibilityTests
{
    [Fact]
    public void Hidden_qat_round_trips_commands_order_and_placement_and_accepts_older_layouts() => Sta.Run(() =>
    {
        var source = Create();
        source.IsQuickAccessVisible = false;
        source.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        source.QuickAccessItems.Move(0, 1);
        string saved = RibbonCustomizationSerializer.Serialize(source);
        var target = Create();
        RibbonCustomizationSerializer.Apply(target, saved);
        Assert.False(target.IsQuickAccessVisible);
        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, target.QuickAccessPosition);
        Assert.Equal(new[] { "redo", "undo" }, target.QuickAccessItems.Cast<FrameworkElement>().Select(Ribbon.GetCommandId));
        Assert.Equal(saved, RibbonCustomizationSerializer.Serialize(target));
        var legacy = JsonNode.Parse(saved)!.AsObject(); legacy.Remove("IsQuickAccessVisible");
        foreach (bool visible in new[] { true, false })
        {
            target.IsQuickAccessVisible = visible;
            RibbonCustomizationSerializer.Apply(target, legacy.ToJsonString());
            Assert.Equal(visible, target.IsQuickAccessVisible);
        }
        legacy["IsQuickAccessVisible"] = "invalid";
        string beforeInvalid = RibbonCustomizationSerializer.Serialize(target);
        RibbonCustomizationSerializer.Apply(target, legacy.ToJsonString());
        Assert.Equal(beforeInvalid, RibbonCustomizationSerializer.Serialize(target));
    });

    [Fact]
    public void Successful_add_reveals_the_toolbar_without_replacing_a_visibility_binding() => Sta.Run(() =>
    {
        var ribbon = Create();
        var preference = new System.Windows.Controls.Button { IsEnabled = false };
        ribbon.SetBinding(Ribbon.IsQuickAccessVisibleProperty, new Binding("IsEnabled")
            { Source = preference, Mode = BindingMode.TwoWay });
        var binding = BindingOperations.GetBindingExpression(ribbon, Ribbon.IsQuickAccessVisibleProperty);
        Assert.False(ribbon.IsQuickAccessVisible);
        Assert.False(ribbon.AddToQuickAccess(new System.Windows.Controls.TextBox()));
        Assert.False(ribbon.IsQuickAccessVisible);
        var undo = (FrameworkElement)ribbon.QuickAccessItems[0];
        ribbon.QuickAccessItems.Remove(undo);
        Assert.True(ribbon.AddToQuickAccess(undo));
        Assert.True(ribbon.IsQuickAccessVisible);
        Assert.Same(binding, BindingOperations.GetBindingExpression(ribbon, Ribbon.IsQuickAccessVisibleProperty));
    });

    private static Ribbon Create()
    {
        var ribbon = new Ribbon();
        foreach (string id in new[] { "undo", "redo" })
        {
            var button = new RibbonButton { Header = id };
            Ribbon.SetCommandId(button, id); ribbon.QuickAccessItems.Add(button);
        }
        return ribbon;
    }
}
