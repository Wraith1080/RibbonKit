using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>
/// The menu-BORROWING protocol between a dropdown (or split) button and the proxies that mirror it
/// — design notes §3.19. A <see cref="RibbonMenuItem"/> is a single-parent element, so a proxy
/// cannot share the source's menu: it MOVES the items into itself while its flyout is open and
/// moves them back when it closes.
/// </summary>
/// <remarks>
/// <para>
/// Everything here is about that move going home again. When it doesn't, the failure lands
/// somewhere else entirely and looks unrelated: the SOURCE — the original button sitting in the
/// ribbon — opens onto an empty menu, for the rest of the session, with nothing wrong at the
/// place the user actually clicked.
/// </para>
/// <para>
/// These tests deliberately never construct a popup. The return path used to hang off
/// <c>Popup.Closed</c>, and that is precisely what broke: WPF coerces <c>Popup.IsOpen</c> to false
/// when a popup is unloaded — which is what the QAT overflow flyout closing does to a proxy living
/// inside it — and the coerced value never travels back through the template binding. The popup
/// was shut, <c>IsDropDownOpen</c> still said open, no further Closed was ever raised, and the
/// items were stranded. The contract now hangs off the PROPERTY, so a templateless button is a
/// perfectly good subject.
/// </para>
/// </remarks>
public class DropDownBorrowTests
{
    [Fact]
    public void Opening_a_proxy_takes_the_sources_menu() => Sta.Run(() =>
    {
        (RibbonDropDownButton source, RibbonDropDownButton proxy) = ProxyPair("Cut", "Copy", "Paste");

        proxy.IsDropDownOpen = true;

        Assert.Empty(source.Items);
        Assert.Equal(new[] { "Cut", "Copy", "Paste" }, Headers(proxy));
        Assert.True(proxy.HasBorrowedItems);
    });

    [Fact]
    public void Closing_a_proxy_gives_the_menu_back_in_order() => Sta.Run(() =>
    {
        (RibbonDropDownButton source, RibbonDropDownButton proxy) = ProxyPair("Cut", "Copy", "Paste");

        proxy.IsDropDownOpen = true;
        proxy.IsDropDownOpen = false;
        Sta.Drain();

        Assert.Empty(proxy.Items);
        Assert.Equal(new[] { "Cut", "Copy", "Paste" }, Headers(source));
        Assert.False(proxy.HasBorrowedItems);
    });

    [Fact]
    public void The_return_is_deferred_not_immediate() => Sta.Run(() =>
    {
        // §3.19: a menu item must never be reparented mid-dispatch — the click that closed the
        // dropdown may still be bubbling through the item being moved. The return is queued at
        // Background priority, so before the queue is pumped nothing has moved yet.
        (RibbonDropDownButton source, RibbonDropDownButton proxy) = ProxyPair("Cut", "Copy");

        proxy.IsDropDownOpen = true;
        proxy.IsDropDownOpen = false;

        Assert.Empty(source.Items);

        Sta.Drain();

        Assert.Equal(2, source.Items.Count);
    });

    [Fact]
    public void A_host_that_closes_behind_the_property_still_gets_the_menu_back() => Sta.Run(() =>
    {
        // THE REGRESSION. This is the QAT overflow flyout being dismissed by a click elsewhere in
        // the window while one of its dropdown/split entries has its menu open. The entry's popup
        // is coerced shut by the unload, so IsDropDownOpen is left stuck at true and no Closed will
        // ever arrive — asking the entry to close is a no-op the popup cannot see. The host has to
        // drive the return itself, and the property has to be corrected on the way.
        (RibbonDropDownButton source, RibbonDropDownButton proxy) = ProxyPair("Cut", "Copy", "Paste");

        proxy.IsDropDownOpen = true;

        proxy.EnsureBorrowedItemsReturned();
        Sta.Drain();

        Assert.False(proxy.IsDropDownOpen);
        Assert.False(proxy.HasBorrowedItems);
        Assert.Equal(new[] { "Cut", "Copy", "Paste" }, Headers(source));
        Assert.Empty(proxy.Items);
    });

    [Fact]
    public void Reopening_before_the_return_lands_keeps_the_menu_in_the_proxy() => Sta.Run(() =>
    {
        // A close→reopen faster than the dispatcher (double-click on the chevron, or a flyout that
        // closes and reopens in the same gesture) must not yank the menu out of a dropdown that is
        // open again by the time the queued return runs.
        (RibbonDropDownButton source, RibbonDropDownButton proxy) = ProxyPair("Cut", "Copy");

        proxy.IsDropDownOpen = true;
        proxy.IsDropDownOpen = false;
        proxy.IsDropDownOpen = true;
        Sta.Drain();

        Assert.Empty(source.Items);
        Assert.Equal(2, proxy.Items.Count);
        Assert.True(proxy.HasBorrowedItems);
    });

    [Fact]
    public void Returning_twice_moves_the_menu_once() => Sta.Run(() =>
    {
        // Belt and braces overlap: the flyout's close handler, the entry's unload and the popup's
        // own Closed can all ask for the same return. Whichever order they arrive in, the items
        // must land in the source exactly once.
        (RibbonDropDownButton source, RibbonDropDownButton proxy) = ProxyPair("Cut", "Copy", "Paste");

        proxy.IsDropDownOpen = true;
        proxy.EnsureBorrowedItemsReturned();
        proxy.EnsureBorrowedItemsReturned();
        Sta.Drain();
        proxy.EnsureBorrowedItemsReturned();
        Sta.Drain();

        Assert.Equal(new[] { "Cut", "Copy", "Paste" }, Headers(source));
        Assert.Empty(proxy.Items);
    });

    [Fact]
    public void A_dropdown_that_owns_its_menu_is_never_raided() => Sta.Run(() =>
    {
        // The same call is made against every cached overflow entry, plain buttons included, and
        // against dropdowns that are sources rather than proxies. It must be inert for them.
        var ordinary = new RibbonDropDownButton();
        ordinary.Items.Add(new RibbonMenuItem { Header = "Cut" });
        ordinary.Items.Add(new RibbonMenuItem { Header = "Copy" });

        ordinary.EnsureBorrowedItemsReturned();
        Sta.Drain();

        Assert.Equal(new[] { "Cut", "Copy" }, Headers(ordinary));
        Assert.False(ordinary.HasBorrowedItems);
    });

    [Fact]
    public void A_split_button_proxy_follows_the_same_protocol() => Sta.Run(() =>
    {
        // RibbonSplitButton derives from RibbonDropDownButton, and the user-visible bug was
        // reported against split buttons first — its chevron borrows exactly like a dropdown's.
        var source = new RibbonSplitButton();
        source.Items.Add(new RibbonMenuItem { Header = "Paste Special" });

        var proxy = new RibbonSplitButton();
        proxy.BorrowMenuFrom(source);

        proxy.IsDropDownOpen = true;
        Assert.Empty(source.Items);

        proxy.EnsureBorrowedItemsReturned();
        Sta.Drain();

        Assert.Equal(new[] { "Paste Special" }, Headers(source));
    });

    [Fact]
    public void A_menu_borrowed_twice_in_a_row_survives_both_trips() => Sta.Run(() =>
    {
        // Open the flyout, use an entry, dismiss, do it again — the reported reproduction loop.
        (RibbonDropDownButton source, RibbonDropDownButton proxy) = ProxyPair("Cut", "Copy", "Paste");

        for (int round = 0; round < 3; round++)
        {
            proxy.IsDropDownOpen = true;
            Assert.Equal(3, proxy.Items.Count);

            proxy.EnsureBorrowedItemsReturned();
            Sta.Drain();

            Assert.Equal(new[] { "Cut", "Copy", "Paste" }, Headers(source));
        }
    });

    private static (RibbonDropDownButton Source, RibbonDropDownButton Proxy) ProxyPair(params string[] headers)
    {
        var source = new RibbonDropDownButton();
        foreach (string header in headers)
        {
            source.Items.Add(new RibbonMenuItem { Header = header });
        }

        var proxy = new RibbonDropDownButton();
        proxy.BorrowMenuFrom(source);

        return (source, proxy);
    }

    private static string[] Headers(System.Windows.Controls.ItemsControl control) =>
        control.Items.Cast<RibbonMenuItem>().Select(item => item.Header ?? string.Empty).ToArray();
}
