using System.Windows;
using System.Windows.Controls;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards nested StaysOpen flyout dismissal on a shared owner window.</summary>
public class PopupDismissHelperTests
{
    [Fact]
    public void Escape_closes_nested_flyouts_from_the_inside_out() => Sta.Run(() =>
    {
        var window = new Window();
        var outerOwner = new Border();
        var innerOwner = new Border();
        window.Content = new StackPanel { Children = { outerOwner, innerOwner } };

        Assert.Same(window, Window.GetWindow(outerOwner));
        Assert.Same(window, Window.GetWindow(innerOwner));

        int outerCloses = 0;
        int innerCloses = 0;
        PopupDismissHelper? outer = null;
        PopupDismissHelper? inner = null;

        outer = new PopupDismissHelper(
            outerOwner,
            () => null,
            () =>
            {
                outerCloses++;
                outer!.OnClosed();
            });
        inner = new PopupDismissHelper(
            innerOwner,
            () => null,
            () =>
            {
                innerCloses++;
                inner!.OnClosed();
            });

        try
        {
            outer.OnOpened();
            inner.OnOpened();

            // The overflow helper was registered first, but it must not consume the first Escape.
            Assert.False(outer.TryDismissTopmostForEscape());
            Assert.Equal(0, outerCloses);

            Assert.True(inner.TryDismissTopmostForEscape());
            Assert.Equal(1, innerCloses);
            Assert.Equal(0, outerCloses);

            // Once the nested menu is gone, the next Escape belongs to the overflow.
            Assert.True(outer.TryDismissTopmostForEscape());
            Assert.Equal(1, outerCloses);
        }
        finally
        {
            inner.OnClosed();
            outer.OnClosed();
        }
    });

    [Fact]
    public void Unloading_a_nested_owner_cannot_leave_an_escape_handler_registered() => Sta.Run(() =>
    {
        var window = new Window();
        var outerOwner = new Border();
        var nestedOwner = new Border();
        window.Content = new StackPanel { Children = { outerOwner, nestedOwner } };

        int nestedCloses = 0;
        PopupDismissHelper? outer = null;
        var nested = new PopupDismissHelper(
            nestedOwner,
            () => null,
            () => nestedCloses++); // Deliberately does not report Popup.Closed.
        outer = new PopupDismissHelper(
            outerOwner,
            () => null,
            () => outer!.OnClosed());

        try
        {
            outer.OnOpened();
            nested.OnOpened();

            nestedOwner.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

            Assert.Equal(1, nestedCloses);
            Assert.False(nested.TryDismissTopmostForEscape());
            Assert.True(outer.TryDismissTopmostForEscape());
        }
        finally
        {
            nested.OnClosed();
            outer.OnClosed();
        }
    });
}
