using System.Reflection;
using System.Windows;
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

        Assert.Same(ribbon, button.Tag);
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
    public void Switching_an_open_backstage_moves_and_restores_the_same_orb_button() => Sta.Run(() =>
    {
        var backstage = new Backstage { Design = RibbonBackstageDesign.Glass2007 };
        var ribbon = new Ribbon
        {
            ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
            Backstage = backstage,
            IsBackstageOpen = true,
        };
        var orb = new FrameworkElementFactory(typeof(Grid), "Orb");
        orb.SetValue(UIElement.VisibilityProperty, Visibility.Collapsed);
        var chrome = new FrameworkElementFactory(typeof(Border), "Chrome");
        var underline = new FrameworkElementFactory(typeof(Border), "HoverUnderline");
        var templateRoot = new FrameworkElementFactory(typeof(Grid));
        templateRoot.AppendChild(chrome);
        templateRoot.AppendChild(underline);
        templateRoot.AppendChild(orb);
        var button = new ToggleButton
        {
            Width = 48,
            Height = 48,
            Template = new ControlTemplate(typeof(ToggleButton)) { VisualTree = templateRoot },
        };
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

        Assert.IsType<Border>(Assert.Single(buttonHost.Children));
        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(adorner));
        Assert.Same(button, VisualTreeHelper.GetChild(adorner, 1));
        Assert.Equal(Visibility.Visible, FindTemplatePart(button, "Orb").Visibility);
        Assert.Equal(Visibility.Collapsed, FindTemplatePart(button, "Chrome").Visibility);

        backstage.Design = RibbonBackstageDesign.Glass2007;

        Assert.Same(button, Assert.Single(buttonHost.Children));
        Assert.Equal(1, VisualTreeHelper.GetChildrenCount(adorner));
        Assert.Equal(Visibility.Collapsed, FindTemplatePart(button, "Orb").Visibility);

        adorner.Detach();
    });

    [Fact]
    public void Startup_Classic_then_other_open_backstage_keeps_orb_in_the_covered_ribbon_layer() => Sta.Run(() =>
    {
        var backstage = new Backstage { Design = RibbonBackstageDesign.Classic2007 };
        var ribbon = new Ribbon
        {
            ApplicationButtonShape = RibbonApplicationButtonShape.Orb,
            Backstage = backstage,
            IsBackstageOpen = true,
        };
        var button = new ToggleButton { Width = 48, Height = 48 };
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
        Assert.Equal(Visibility.Visible, button.Visibility);
        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(adorner));

        backstage.Design = RibbonBackstageDesign.Glass2007;

        Assert.Same(button, Assert.Single(buttonHost.Children));
        Assert.Equal(1, VisualTreeHelper.GetChildrenCount(adorner));
        Assert.Equal(Visibility.Visible, button.Visibility);

        backstage.Design = RibbonBackstageDesign.Classic2007;

        Assert.Equal(Visibility.Visible, button.Visibility);
        Assert.Equal(2, VisualTreeHelper.GetChildrenCount(adorner));

        adorner.DetachApplicationButton();
        InvokePrivate(ribbon, "RestoreApplicationButtonFromOverlay");
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
        InvokePrivate(ribbon, "ReconcileClassicBackstageApplicationButton", false);

    private static void InvokePrivate(object instance, string name, params object[] arguments)
    {
        MethodInfo method = instance.GetType().GetMethod(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method '{name}' was not found.");
        method.Invoke(instance, arguments);
    }

    private static FrameworkElement FindTemplatePart(Control root, string name)
    {
        root.ApplyTemplate();
        return Assert.IsAssignableFrom<FrameworkElement>(root.Template.FindName(name, root));
    }
}
