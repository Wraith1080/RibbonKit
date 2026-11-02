using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class QuickAccessGalleryPresenterCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var form = new DesignerForm { StartPosition = FormStartPosition.Manual, Location = new(180, 120) };
        var ribbon = form.Controls.OfType<RibbonControl>().Single(); var (tab, gallery) = form.GalleryPresenterExample;
        var reference = ribbon.QuickAccessItems.OfType<RibbonQuickAccessDropDownReference>().Single(entry => ReferenceEquals(entry.Source, gallery));
        Require(gallery.UseDropDownPresenterInQuickAccess && gallery.DropDownPresenterType == typeof(GalleryStylesControl) && reference.KeyTip == "3", "Designer-authored source opt-in, existing popup type and QAT reference");
        ribbon.SelectedTab = tab; Exception? failure = null;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); await Task.Delay(500); Require(GetForegroundWindow() == form.Handle, "Fixture foreground unavailable; capture stopped");
                var inline = Descendants(form).OfType<GalleryStylesStripControl>().Single(); nint inlineHandle = inline.Handle;
                var inlineCards = inline.Controls.OfType<GalleryStyleTileControl>().ToArray();
                inline.ScrollView(RibbonGalleryScroll.RowForward); int row = inline.ViewedRow; int commits = 0;
                gallery.SelectionCommitted += (_, _) => commits++;
                await OpenFromQat(); var first = FindPopup() ?? throw new InvalidOperationException("QAT did not construct the existing popup class.");
                var firstContext = first.Context;
                Require(reference.IsDropDownOpen && firstContext.IsQuickAccess && firstContext.Presentation == RibbonGalleryPresentation.DropDown && ReferenceEquals(firstContext.Gallery, gallery), "QAT popup context identifies route and original source");
                Require(inline.Visible && inline.Handle == inlineHandle && !inline.Context.IsActive, "Separate retained inline view remains visible but suspended");
                await Capture("qat-and-inline", form);
                PostKey(first, Keys.End); await Task.Delay(100); var last = (RibbonGalleryTile)gallery.Items[^1];
                Require(ReferenceEquals(firstContext.FocusedItem, last) && ReferenceEquals(gallery.SelectedItem, gallery.Items[0]), "Queued keyboard preview does not select");
                await Capture("qat-last-row", first.TopLevelControl ?? first);
                PostKey(first, Keys.Escape); await Task.Delay(100);
                Require(first.IsDisposed && !firstContext.IsActive && !reference.IsDropDownOpen && inline.ViewedRow == row && inline.Handle == inlineHandle, "Escape disposes only popup and preserves inline row and native handle");
                Require(inlineCards.SequenceEqual(inline.Controls.OfType<GalleryStyleTileControl>()), "Inline native card identities survive QAT expansion");
                await OpenFromQat(); var second = FindPopup() ?? throw new InvalidOperationException("QAT did not reopen after Escape."); Require(!ReferenceEquals(first, second), "Reopening uses a fresh popup-owned control");
                var card = second.Controls.OfType<GalleryStyleTileControl>().First(view => second.ClientRectangle.Contains(view.Bounds));
                var label = card.Controls.OfType<Label>().Single(child => child.Name == "titleLabel"); var clicked = (RibbonGalleryTile)card.Tag!;
                _ = SendMessage(label.Handle, 0x201, 1, (nint)(5 | 5 << 16)); _ = SendMessage(label.Handle, 0x202, 0, (nint)(5 | 5 << 16));
                Require(commits == 1 && ReferenceEquals(gallery.SelectedItem, clicked) && second.IsDisposed, "Native child-label click commits once and disposes QAT popup");
                ribbon.SelectedTab = ribbon.Tabs.First(candidate => !ReferenceEquals(candidate, tab)); var inactive = ribbon.SelectedTab;
                await OpenFromQat(); var third = FindPopup()!;
                Require(third.Context.IsQuickAccess && ReferenceEquals(ribbon.SelectedTab, inactive), "QAT opens while the source tab stays inactive");
                PostKey(third, Keys.End); await Task.Delay(75); PostKey(third, Keys.Enter); await Task.Delay(100);
                Require(commits == 2 && third.IsDisposed && ReferenceEquals(gallery.SelectedItem, last) && ReferenceEquals(ribbon.SelectedTab, inactive), "Queued keyboard commit runs once without selecting the source tab");
                // The joined BelowRibbon toolbar deliberately uses the full
                // row. Use the existing bounded TabRow mode to force overflow.
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; ribbon.QuickAccessMaxWidth = 28;
                await OpenFromQat(overflow: true); var overflowView = FindPopup()!;
                Require(overflowView.Context.IsQuickAccess && reference.IsDropDownOpen, "QAT overflow reaches the same custom popup class");
                await Capture("qat-overflow", form);
                var stale = overflowView.Context; gallery.UseDropDownPresenterInQuickAccess = false;
                Require(overflowView.IsDisposed && !stale.IsActive && !stale.Commit(last) && !reference.IsDropDownOpen, "Changing QAT policy closes the affected view and retires callbacks");
                PostKey(ribbon, Keys.Escape); await Task.Delay(100); // Close the retained overflow parent.
                await OpenFromQat(overflow: true);
                Require(reference.IsDropDownOpen && FindPopup() is null, "Opt-out restores the built-in QAT popup without another custom class");
                gallery.CloseDropDown(); PostKey(ribbon, Keys.Escape); await Task.Delay(100);
                Require(commits == 2 && gallery.Items.Count == 18 && inline.Handle == inlineHandle && !inline.IsDisposed, "Source, retained inline handles and commit count survive all QAT routes");
                log.WriteLine($"PASS: package-only public API; designer-authored compact QAT launcher uses existing GalleryStylesControl; original 18 source tiles/selection; actual DPI={form.DeviceDpi}; direct/reopen/inactive-tab/overflow routes; native child click and queued keyboard; two commits; fresh popup disposal; stale policy callbacks; built-in opt-out; retained inline/card handles and row. Actual VS, physical input and mixed-monitor review remain separate.");
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { form.Close(); }
        };
        Application.Run(form); if (failure is not null) throw failure;

        async Task OpenFromQat(bool overflow = false)
        {
            // Escape from a KeyTip-opened popup can return to its parent KeyTip
            // layer. Exit that layer before the next independent F10 gesture.
            PostKey(ribbon, Keys.Escape); await Task.Delay(50);
            PostKey(ribbon, Keys.F10); await Task.Delay(75);
            if (overflow)
            {
                PostKey(ribbon, Keys.D0); await Task.Delay(100);
                bool found = false;
                EnumThreadWindows(GetCurrentThreadId(), (window, _) =>
                {
                    if (GetAncestor(window, 3) == form.Handle && Control.FromHandle(window)?.AccessibleName?.StartsWith("More Quick Access commands", StringComparison.Ordinal) == true) found = true;
                    return true;
                }, 0);
                Require(found, "The overflow parent must be open before choosing its gallery row");
            }
            PostKey(ribbon, Keys.D3); await Task.Delay(250);
        }
        GalleryStylesControl? FindPopup()
        {
            GalleryStylesControl? found = null;
            EnumThreadWindows(GetCurrentThreadId(), (window, _) =>
            {
                if (GetAncestor(window, 3) == form.Handle && Control.FromHandle(window) is { } root)
                    found = Descendants(root).OfType<GalleryStylesControl>().FirstOrDefault();
                return found is null;
            }, 0);
            return found;
        }
        async Task Capture(string name, Control root)
        {
            await Task.Delay(50); Require(GetForegroundWindow() == form.Handle || GetAncestor(GetForegroundWindow(), 3) == form.Handle, "Foreground lost; capture stopped");
            root.Refresh(); _ = GdiFlush(); _ = DwmFlush(); var area = root.RectangleToScreen(root.ClientRectangle);
            using var image = new Bitmap(area.Width, area.Height); using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
            image.Save(Path.Combine(output, name + ".png"));
        }
    }
    private static IEnumerable<Control> Descendants(Control root) => new[] { root }.Concat(root.Controls.Cast<Control>().SelectMany(Descendants));
    private static void PostKey(Control control, Keys key)
    { _ = PostMessage(control.Handle, 0x100, (nint)key, 0); _ = PostMessage(control.Handle, 0x101, (nint)key, 0); }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private delegate bool WindowCallback(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, WindowCallback callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("gdi32.dll")] private static extern bool GdiFlush();
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
}
