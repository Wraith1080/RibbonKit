using System.Reflection;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Live control-state coverage for the Classic 2007 application-orb host.</summary>
public class ClassicBackstageOrbLifecycleTests
{
    [Fact]
    public void Application_button_state_binding_remains_live_without_visual_ancestry() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        var button = new ToggleButton();

        MethodInfo ensureBindings = typeof(Ribbon).GetMethod(
            "EnsureApplicationButtonOwnerBindings",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Application-button binding helper was not found.");
        ensureBindings.Invoke(ribbon, new object[] { button });

        Assert.Null(button.Tag);
        Assert.False(button.IsChecked);

        ribbon.IsBackstageOpen = true;
        Sta.Drain();
        Assert.True(button.IsChecked);

        button.IsChecked = false;
        Sta.Drain();
        Assert.False(ribbon.IsBackstageOpen);
        Assert.IsType<BindingExpression>(
            BindingOperations.GetBindingExpression(button, ToggleButton.IsCheckedProperty));
    });

    [Fact]
    public void Switching_an_open_backstage_keeps_the_real_button_in_place_and_swaps_only_the_proxy() => Sta.Run(() =>
    {
        var backstage = new Backstage { Design = RibbonBackstageDesign.Glass2007 };
        var ribbon = new Ribbon
        {
            ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
            Backstage = backstage,
            IsBackstageOpen = true,
        };
        ToggleButton button = CreateApplicationButton();
        var buttonHost = new StackPanel();
        var adornedRoot = new Grid { Width = 500, Height = 300 };
        buttonHost.Children.Add(button);
        adornedRoot.Children.Add(buttonHost);

        var available = new Size(500, 300);
        adornedRoot.Measure(available);
        adornedRoot.Arrange(new Rect(available));
        adornedRoot.UpdateLayout();

        var adorner = new BackstageAdorner(adornedRoot, backstage, ribbon);
        SetPrivateField(ribbon, "_backstageAdorner", adorner);
        SetPrivateField(ribbon, "_applicationButton", button);

        backstage.Design = RibbonBackstageDesign.Classic2007;

        Assert.Same(button, Assert.Single(buttonHost.Children));
        Assert.Equal(0d, button.Opacity);
        Assert.False(button.IsHitTestVisible);
        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(adorner));
        var proxy = Assert.IsType<Button>(GetPrivateField(ribbon, "_classicBackstageOrbProxy"));
        Assert.Same(proxy, VisualTreeHelper.GetChild(adorner, 1));
        Assert.NotSame(button, proxy);
        Assert.Equal(string.Empty, proxy.Content);
        Assert.NotSame(ribbon, proxy.Content);
        Assert.Same(
            Assert.IsType<ContentPresenter>(button.Template.FindName("Orb", button)).ContentTemplate,
            proxy.ContentTemplate);
        Assert.Equal(
            -360d,
            Assert.IsType<double>(GetPrivateField(ribbon, "_pendingClassicBackstageOrbRotation")));
        string automationName = AutomationProperties.GetName(proxy);
        Assert.False(string.IsNullOrWhiteSpace(automationName));
        Assert.Equal(proxy.ToolTip, automationName);
        proxy.ApplyTemplate();
        var proxyPresenter = Assert.IsType<ContentPresenter>(VisualTreeHelper.GetChild(proxy, 0));
        Assert.Equal(VerticalAlignment.Top, proxyPresenter.VerticalAlignment);
        Assert.Same(proxy.ContentTemplate, proxyPresenter.ContentTemplate);

        backstage.Design = RibbonBackstageDesign.Glass2007;

        Assert.Same(button, Assert.Single(buttonHost.Children));
        Assert.Equal(1, VisualTreeHelper.GetChildrenCount(adorner));
        Assert.Equal(0d, button.Opacity);
        Assert.False(button.IsHitTestVisible);

        SetPrivateField(ribbon, "_backstageClosing", true);
        ribbon.IsBackstageOpen = false;
        InvokeReconcile(ribbon);

        Assert.Equal(1d, button.Opacity);
        Assert.True(button.IsHitTestVisible);
        SetPrivateField(ribbon, "_backstageClosing", false);

        backstage.Design = RibbonBackstageDesign.Classic2007;
        ribbon.IsBackstageOpen = true;

        Assert.Same(button, Assert.Single(buttonHost.Children));
        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(adorner));
        Assert.Same(proxy, VisualTreeHelper.GetChild(adorner, 1));

        InvokePrivate(ribbon, "SetBackstageApplicationButtonSuppressed", false);
        adorner.Detach();
    });

    [Fact]
    public void Startup_Classic_then_other_open_backstage_keeps_the_real_orb_suppressed_without_reparenting() => Sta.Run(() =>
    {
        var backstage = new Backstage { Design = RibbonBackstageDesign.Classic2007 };
        var ribbon = new Ribbon
        {
            ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
            Backstage = backstage,
            IsBackstageOpen = true,
        };
        ToggleButton button = CreateApplicationButton();
        var buttonHost = new StackPanel();
        var adornedRoot = new Grid { Width = 500, Height = 300 };
        buttonHost.Children.Add(button);
        adornedRoot.Children.Add(buttonHost);

        var available = new Size(500, 300);
        adornedRoot.Measure(available);
        adornedRoot.Arrange(new Rect(available));
        adornedRoot.UpdateLayout();

        var adorner = new BackstageAdorner(adornedRoot, backstage, ribbon);
        SetPrivateField(ribbon, "_backstageAdorner", adorner);
        SetPrivateField(ribbon, "_applicationButton", button);

        InvokeReconcile(ribbon);
        Assert.Same(button, Assert.Single(buttonHost.Children));
        Assert.Equal(0d, button.Opacity);
        Assert.False(button.IsHitTestVisible);
        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(adorner));

        backstage.Design = RibbonBackstageDesign.Glass2007;

        Assert.Same(button, Assert.Single(buttonHost.Children));
        Assert.Equal(1, VisualTreeHelper.GetChildrenCount(adorner));
        Assert.Equal(0d, button.Opacity);
        Assert.False(button.IsHitTestVisible);

        backstage.Design = RibbonBackstageDesign.Classic2007;

        Assert.Same(button, Assert.Single(buttonHost.Children));
        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(adorner));

        InvokePrivate(ribbon, "SetBackstageApplicationButtonSuppressed", false);
        Assert.Equal(1d, button.Opacity);
        Assert.True(button.IsHitTestVisible);
        adorner.Detach();
    });

    private static void SetPrivateField(object instance, string name, object value)
    {
        FieldInfo field = instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{name}' was not found.");
        field.SetValue(instance, value);
    }

    private static void InvokeReconcile(Ribbon ribbon) =>
        InvokePrivate(ribbon, "ReconcileClassicBackstageOrbProxy", false);

    private static ToggleButton CreateApplicationButton()
    {
        var chrome = new FrameworkElementFactory(typeof(Grid));
        var orb = new FrameworkElementFactory(typeof(ContentPresenter), "Orb");
        var orbRoot = new FrameworkElementFactory(typeof(Grid), "OrbGlyph");
        var orbTemplate = new DataTemplate { VisualTree = orbRoot };
        orb.SetValue(ContentPresenter.ContentTemplateProperty, orbTemplate);
        chrome.AppendChild(orb);
        return new ToggleButton
        {
            Width = 48,
            Height = 48,
            Template = new ControlTemplate(typeof(ToggleButton)) { VisualTree = chrome },
        };
    }

    private static object? GetPrivateField(object instance, string name)
    {
        FieldInfo field = instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Field '{name}' was not found.");
        return field.GetValue(instance);
    }

    private static void InvokePrivate(object instance, string name, params object[] arguments)
    {
        MethodInfo method = instance.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method '{name}' was not found.");
        method.Invoke(instance, arguments);
    }

}
