using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class PopupHoverBindingTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Utility_and_orb_hover_bindings_follow_qat_suppression_without_binding_errors(bool utility) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(), utility ? RibbonTheme.CrystalLight : RibbonTheme.Office2007);
        var motion = RibbonAnimation.GlobalLevel;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        using var errors = new StringWriter();
        using var listener = new TextWriterTraceListener(errors);
        var bindingTrace = PresentationTraceSources.DataBindingSource;
        var previousLevel = bindingTrace.Switch.Level;
        bindingTrace.Switch.Level = SourceLevels.Error; bindingTrace.Listeners.Add(listener);
        var command = new RibbonDropDownButton { Header = "Choices" };
        command.Items.Add(new RibbonMenuItem { Header = "Choice" });
        var ribbon = new Ribbon { Backstage = new Backstage() };
        if (!utility) ribbon.ApplicationButtonShape = RibbonKit.RibbonApplicationButtonShape.Orb;
        var home = new RibbonTab { Header = "Home" };
        var group = new RibbonGroup { Header = "Commands" }; group.Items.Add(command); home.Groups.Add(group); ribbon.Tabs.Add(home);
        Assert.True(ribbon.AddToQuickAccess(command));
        var qat = Assert.IsAssignableFrom<RibbonDropDownButton>(Assert.Single(ribbon.QuickAccessItems));
        var window = new Window { Content = ribbon, Width = 900, Height = 350,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); Layout();
            var tabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
            ButtonBase button;
            FrameworkElement paint;
            BindingBase suppressionBinding;
            if (utility)
            {
                var modal = new RibbonTab { Header = "Preview", IsModal = true }; ribbon.Tabs.Add(modal);
                Assert.True(ribbon.EnterModal(modal)); Layout();
                button = Part<Button>(tabs, "PART_ModalClose");
                paint = Part<Border>(button, "CrystalUtilityRim");
                suppressionBinding = paint.Style.Triggers.OfType<MultiDataTrigger>().Single().Conditions[1].Binding;
            }
            else
            {
                button = Part<ToggleButton>(tabs, Ribbon.ApplicationButtonPartName);
                paint = Descendants<System.Windows.Shapes.Ellipse>(button).Single(ellipse => ellipse.Name == "OrbFill");
                var template = Part<ContentPresenter>(button, "Orb").ContentTemplate;
                suppressionBinding = template.Triggers.OfType<MultiDataTrigger>().Single().Conditions[1].Binding;
            }
            // Evaluate the template's exact binding on the same native target. This
            // exposes failed lookups even when the hover condition is inactive.
            BindingOperations.SetBinding(paint, FrameworkElement.TagProperty, suppressionBinding); Layout();
            Assert.Equal(false, paint.Tag);
            qat.IsDropDownOpen = true; Layout();
            Assert.True(RibbonPopupInteraction.GetSuppressHover(button)); Assert.Equal(true, paint.Tag);
            var pressedKey = (DependencyPropertyKey)typeof(ButtonBase).GetField("IsPressedPropertyKey", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            button.SetValue(pressedKey, true);
            if (utility) Assert.Equal(1, paint.Opacity);
            else Assert.Same(button.FindResource("RibbonKit.Brushes.ApplicationOrb.PressedBackground"), ((System.Windows.Shapes.Ellipse)paint).Fill);
            button.SetValue(pressedKey, false);
            qat.IsDropDownOpen = false; Layout(); Assert.Equal(false, paint.Tag);
            listener.Flush(); Assert.DoesNotContain("SuppressHover", errors.ToString());
        }
        finally
        {
            window.Close(); Sta.Drain(); bindingTrace.Listeners.Remove(listener); bindingTrace.Switch.Level = previousLevel;
            RibbonAnimation.GlobalLevel = motion; Sta.ResetApplication();
        }
        void Layout() { Sta.Drain(); window.UpdateLayout(); Sta.Drain(); }
    });

    private static T Part<T>(Control owner, string name) where T : FrameworkElement
    {
        owner.ApplyTemplate(); return Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }
}
