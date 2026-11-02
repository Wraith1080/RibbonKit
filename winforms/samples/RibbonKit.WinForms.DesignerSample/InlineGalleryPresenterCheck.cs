using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class InlineGalleryPresenterCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using (var preview = new GalleryStylesStripControl())
        {
            Require(preview.Controls.OfType<GalleryStyleTileControl>().Count() == 3, "Three real cards are present in the standalone strip designer");
            using var image = new Bitmap(preview.Width, preview.Height); preview.DrawToBitmap(image, preview.ClientRectangle);
            image.Save(Path.Combine(output, "designer-inline-cards.png"));
        }
        using var form = new DesignerForm { StartPosition = FormStartPosition.Manual, Location = new(180, 120) };
        var ribbon = form.Controls.OfType<RibbonControl>().Single(); var (tab, gallery) = form.GalleryPresenterExample;
        Require(gallery.IsInRibbon && gallery.InRibbonPresenterType == typeof(GalleryStylesStripControl), "Designer-authored inline class assignment");
        Require(gallery.DropDownPresenterType == typeof(GalleryStylesControl), "Existing popup class remains independent");
        ribbon.SelectedTab = tab; Exception? failure = null;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); await Task.Delay(500); Require(GetForegroundWindow() == form.Handle, "Fixture foreground unavailable");
                var inline = Descendants(form).OfType<GalleryStylesStripControl>().Single(); var context = inline.Context;
                Require(context.IsActive && context.Presentation == RibbonGalleryPresentation.InRibbon && ReferenceEquals(context.Gallery, gallery), "Active inline view uses original source");
                nint handle = inline.Handle; var cards = inline.Controls.OfType<GalleryStyleTileControl>().ToArray();
                Require(cards.Length == gallery.Items.Count && cards.All(card => gallery.Items.Contains((RibbonGalleryTile)card.Tag!)), "All native cards bind original source tiles");
                Require(!cards[^1].IsHandleCreated, "An unvisited offscreen row does not create native windows on first opening");
                var firstRow = cards.Where(card => card.Visible).ToDictionary(card => card, card => card.Handle);
                inline.ScrollView(RibbonGalleryScroll.RowForward); inline.ScrollView(RibbonGalleryScroll.RowBackward);
                Require(firstRow.All(pair => pair.Key.Visible && pair.Key.Handle == pair.Value), "Browsing retains the visited native card objects and handles");
                await Capture("inline-initial", form);
                int commits = 0; gallery.SelectionCommitted += (_, _) => commits++;
                inline.ScrollView(RibbonGalleryScroll.RowForward); int row = inline.ViewedRow;
                Require(row == 1 && ReferenceEquals(gallery.SelectedItem, gallery.Items[0]), "Inline browsing does not select");
                Require(context.FocusItem((RibbonGalleryTile)gallery.Items[3]), "Focus the browsed row without selecting");
                gallery.ShowDropDown(); await Task.Delay(250); var popup = FindPopup(); var popupContext = popup.Context;
                Require(!context.IsActive && inline.Visible && !inline.IsDisposed && popupContext.IsActive, "Visible inline view is suspended under its separate popup");
                Require(ReferenceEquals(popupContext.Gallery, gallery) && popupContext.Presentation == RibbonGalleryPresentation.DropDown, "Popup shares original source and has separate presentation context");
                Require(!context.Commit((RibbonGalleryTile)gallery.Items[0]), "Suspended inline context rejects input");
                await Capture("inline-and-expanded", popup.TopLevelControl ?? popup);
                gallery.CloseDropDown(); await Task.Delay(100);
                Require(popup.IsDisposed && !inline.IsDisposed && inline.ViewedRow == row && inline.Handle == handle, "Cancel disposes only the popup and preserves inline row and native handle");
                Require(!context.IsActive && inline.Context.IsActive && inline.Focused, "Reactivation supplies a fresh context and returns native focus");
                await Capture("inline-cancelled", form);
                var last = (RibbonGalleryTile)gallery.Items[^1]; gallery.SelectedItem = last;
                Require(commits == 0 && inline.GetItemBounds(last).IntersectsWith(inline.ClientRectangle), "Programmatic selection reveals without a commit");
                Require(cards.SequenceEqual(inline.Controls.OfType<GalleryStyleTileControl>()), "Selection retains native card instances");
                var visible = cards.First(card => inline.ClientRectangle.Contains(card.Bounds));
                var label = visible.Controls.OfType<Label>().Single(child => child.Name == "titleLabel"); var clicked = (RibbonGalleryTile)visible.Tag!;
                label.MouseDown += (_, e) => log.WriteLine($"Child down: {e.Button}; active={inline.Context.IsActive}; capture={label.Capture}");
                label.MouseUp += (_, e) => log.WriteLine($"Child up: {e.Button}; active={inline.Context.IsActive}; capture={label.Capture}; commits={commits}; selected={gallery.SelectedItem?.Text}");
                label.MouseCaptureChanged += (_, _) => log.WriteLine($"Child capture changed: {label.Capture}");
                Click(label, MouseButtons.Right, 5); Require(commits == 0, "Native child right click does not commit");
                // The ordinary ribbon context menu is a valid right-click result.
                PostKey(inline, Keys.Escape); await Task.Delay(100);
                Require(inline.Context.IsActive, "Dismiss the existing customization menu before the next gesture");
                Click(label, MouseButtons.Left, -1000); Require(commits == 0, "Native child outside release does not commit");
                Click(label, MouseButtons.Left, 5); Require(commits == 1 && ReferenceEquals(gallery.SelectedItem, clicked), "Native child left click commits once through original source");
                PostKey(inline, Keys.End); await Task.Delay(100);
                Require(ReferenceEquals(gallery.SelectedItem, clicked) && ReferenceEquals(inline.Context.FocusedItem, last), "Native queued keyboard navigation previews without selecting");
                PostKey(inline, Keys.Enter); await Task.Delay(100);
                Require(commits == 2 && ReferenceEquals(gallery.SelectedItem, last) && !inline.IsDisposed, "Native queued Enter commits once and retains inline body");
                var active = inline.Context; var other = ribbon.Tabs.First(candidate => !ReferenceEquals(candidate, tab)); ribbon.SelectedTab = other;
                Require(!inline.Visible && !inline.IsDisposed && !active.IsActive, "Hidden tab retains control and invalidates context");
                ribbon.SelectedTab = tab; Require(inline.Handle == handle && inline.Context.IsActive, "Return to tab reuses native handle");
                await Capture("inline-last-selected", form);
                gallery.IsInRibbon = false;
                Require(inline.IsDisposed && cards.All(card => card.IsDisposed) && gallery.Items.Count == 18, "Turning off inline disposes views and preserves source items");
                log.WriteLine($"PASS: package-only public API; designer-authored separate inline/popup classes; original 18 source tiles; actual DPI={form.DeviceDpi}; deferred offscreen native windows; retained native inline/card handles through row browsing; independent popup lifetime; cancel row/focus; preview/programmatic selection/native child click/queued keyboard; exactly {commits} commits; hidden-context invalidation; owned disposal. Physical input, actual VS and mixed-monitor acceptance remain separate.");
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { form.Close(); }
        };
        Application.Run(form); if (failure is not null) throw failure;

        GalleryStylesControl FindPopup()
        {
            GalleryStylesControl? found = null;
            EnumThreadWindows(GetCurrentThreadId(), (window, _) =>
            {
                if (GetAncestor(window, 3) == form.Handle && Control.FromHandle(window) is { } root)
                    found = Descendants(root).OfType<GalleryStylesControl>().FirstOrDefault();
                return found is null;
            }, 0);
            return found ?? throw new InvalidOperationException("Separate popup presenter was not created.");
        }
        async Task Capture(string name, Control root)
        {
            await Task.Delay(50); Require(GetForegroundWindow() == form.Handle || GetAncestor(GetForegroundWindow(), 3) == form.Handle, "Foreground lost; capture stopped");
            root.Refresh(); _ = GdiFlush(); _ = DwmFlush(); var area = root.RectangleToScreen(root.ClientRectangle);
            using var image = new Bitmap(area.Width, area.Height); using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
            image.Save(Path.Combine(output, name + ".png"));
        }
    }
    private static void Click(Control control, MouseButtons button, int releaseX)
    {
        _ = SendMessage(control.Handle, button == MouseButtons.Left ? 0x201u : 0x204u, button == MouseButtons.Left ? 1 : 2, (nint)(5 | 5 << 16));
        _ = SendMessage(control.Handle, button == MouseButtons.Left ? 0x202u : 0x205u, 0, (nint)(unchecked((ushort)releaseX) | 5 << 16));
    }
    private static void PostKey(Control control, Keys key)
    { _ = PostMessage(control.Handle, 0x100, (nint)key, 0); _ = PostMessage(control.Handle, 0x101, (nint)key, 0); }
    private static IEnumerable<Control> Descendants(Control root) => new[] { root }.Concat(root.Controls.Cast<Control>().SelectMany(Descendants));
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
