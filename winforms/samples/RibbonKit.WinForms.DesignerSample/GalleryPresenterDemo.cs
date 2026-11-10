using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class GalleryPresenterDemo
{
    internal static void Verify(string output, int entryCount = 18)
    {
        Directory.CreateDirectory(output); using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var form = new DesignerForm { StartPosition = FormStartPosition.Manual, Location = new(180, 120) };
        var status = form.Controls.OfType<StatusStrip>().Single().Items["statusLabel"]!;
        var ribbon = form.Controls.OfType<RibbonControl>().Single(); var (tab, gallery) = form.GalleryPresenterExample;
        Require(ribbon.Tabs.Contains(tab) && tab.Groups.Any(group => group.Items.Contains(gallery)) && gallery.Items.Count == 18,
            "Designer-authored tab, gallery and tile declarations exist before Form.Load");
        Require(ReferenceEquals(gallery.SelectedItem, gallery.Items[0]) && gallery.Items.Select(item => item.Id).Distinct().Count() == 18,
            "Designer-authored initial selection and stable tile identities");
        ribbon.SelectedTab = tab;
        Require(gallery.DropDownPresenterType == typeof(GalleryStylesControl) && gallery.DropDownPresenterFactory is null,
            "Designer-authored presenter class without a runtime factory hookup");
        if (entryCount is < 18 or > 10_000) throw new ArgumentOutOfRangeException(nameof(entryCount));
        ribbon.BeginUpdate();
        try
        {
            for (int i = gallery.Items.Count; i < entryCount; i++)
                gallery.Items.Add(new RibbonGalleryTile { Id = $"stress-style-{i}", Text = $"Style {i + 1}" });
        }
        finally { ribbon.EndUpdate(); }
        using (var preview = new GalleryStylesControl())
        {
            Require(preview.Controls.OfType<GalleryStyleTileControl>().Count() == 6, "Six real tile controls exist without a runtime context");
            Require(Descendants(preview).OfType<Label>().Count(label => label.Name == "titleLabel") == 6, "Tile contents are native designer-editable labels");
            using var image = new Bitmap(preview.Width, preview.Height);
            preview.DrawToBitmap(image, preview.ClientRectangle); image.Save(Path.Combine(output, "designer-cards.png"));
        }
        gallery.DropDownClosed += (_, _) => log.WriteLine($"Popup closed; foreground={GetForegroundWindow()}; owner={form.Handle}; source visible={gallery.Visible}, enabled={gallery.Enabled}.");
        Exception? failure = null;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); await Task.Delay(500);
                Require(GetForegroundWindow() == form.Handle, "Foreground unavailable before opening; capture stopped");
                gallery.ShowDropDown(); await Task.Delay(250);
                var first = FindPresenter(); var context = first.Context;
                Require(!first.IsDisposed, "Popup closed before capture; check foreground/dismissal log");
                nint handle = first.Handle;
                var firstCards = first.Controls.OfType<GalleryStyleTileControl>().ToArray();
                Require(firstCards.Where(card => card.Tag is not null).All(card => gallery.Items.Contains((RibbonGalleryTile)card.Tag!)), "Native tile views refer to original source items");
                Require(firstCards.Where(card => !card.Visible).All(card => !card.IsHandleCreated), "Offscreen cards do not create native windows");
                if (entryCount >= 200) Require(firstCards.Length < 50, "Unvisited rows do not allocate card trees for all source items");
                var firstHandles = firstCards.Where(card => card.Visible).ToDictionary(card => card, card => card.Handle);
                Require(context.IsActive && ReferenceEquals(context.Gallery, gallery), "One active view of the original source");
                Require(context.Dpi == form.DeviceDpi && first.Parent is not null, "Native parenting and DPI context");
                await Capture("initial", first);
                int previews = 0, cancels = 0, commits = 0; gallery.ItemPreview += (_, _) => previews++; gallery.ItemPreviewCancelled += (_, _) => cancels++; gallery.SelectionCommitted += (_, _) => commits++;
                var last = (RibbonGalleryTile)gallery.Items[^1]; var original = gallery.SelectedItem;
                Require(context.Preview(last) && ReferenceEquals(gallery.SelectedItem, original), "Preview leaves committed selection intact");
                Require(status.Text == "Preview: " + last.Text, "Designer-wired preview event");
                Require(context.Preview(null) && cancels == 1, "Preview cancellation");
                gallery.SelectedItem = last; Require(commits == 0 && first.GetItemBounds(last).IntersectsWith(first.ClientRectangle), "Programmatic selection reveals without committing");
                Require(first.Handle == handle, "Live native handle retained through synchronization and reveal");
                Require(firstCards.All(card => !card.IsDisposed && ReferenceEquals(card.Parent, first)), "Selection and reveal retain visited tile controls");
                await Capture("last-selected", first);
                gallery.SelectedItem = original;
                Require(firstHandles.All(pair => pair.Key.IsHandleCreated && pair.Key.Handle == pair.Value), "Returning to a visited row retains its native card handles");
                gallery.SelectedItem = last;
                var allCards = first.Controls.OfType<GalleryStyleTileControl>().ToArray();
                Require(context.Commit(last) && commits == 1 && first.IsDisposed && !context.IsActive, "Accepted source commit and view disposal");
                Require(allCards.All(card => card.IsDisposed), "Popup close releases all visited and unused tile views");
                Require(status.Text == "Selected: " + last.Text, "Designer-wired commit event");
                Require(!context.Commit(last) && !context.Preview(last), "Old context is inert");
                gallery.ShowDropDown(); await Task.Delay(250); var current = FindPresenter(); Require(!ReferenceEquals(current, first), "Reopen constructs another view");
                Require(ReferenceEquals(gallery.SelectedItem, last), "Source selection retained across views");
                current!.ScrollView(RibbonGalleryScroll.PageBackward); await Capture("reopened-page-up", current);
                var clicked = current.Controls.OfType<GalleryStyleTileControl>().First(card => card.Visible && current.ClientRectangle.Contains(card.Bounds));
                var label = clicked.Controls.OfType<Label>().Single(child => child.Name == "titleLabel");
                var clickedItem = (RibbonGalleryTile)clicked.Tag!;
                _ = SendMessage(label.Handle, 0x204, 2, (nint)(5 | 5 << 16));
                _ = SendMessage(label.Handle, 0x205, 0, (nint)(5 | 5 << 16));
                Require(commits == 1 && gallery.IsDropDownOpen, "Right-clicking a child does not commit");
                _ = SendMessage(label.Handle, 0x201, 1, (nint)(5 | 5 << 16));
                _ = SendMessage(label.Handle, 0x202, 0, (nint)(unchecked((ushort)-1000) | 5 << 16));
                Require(commits == 1 && gallery.IsDropDownOpen, "Releasing outside the card does not commit");
                _ = SendMessage(label.Handle, 0x201, 1, (nint)(5 | 5 << 16));
                _ = SendMessage(label.Handle, 0x202, 0, (nint)(5 | 5 << 16));
                Require(commits == 2 && ReferenceEquals(gallery.SelectedItem, clickedItem), "Native child-label click commits its source exactly once");
                Require(current.IsDisposed && gallery.Items.Count == entryCount, "Native child click closes and releases the view; source items retained");
                log.WriteLine($"PASS: package-only designer-selected UserControl class; six designer-visible cards; {entryCount} source entries; initial card trees={firstCards.Length}, initial native cards={firstHandles.Count}; source hierarchy/selection and wired events; native viewport/DPI={form.DeviceDpi}; preview={previews}, cancel={cancels}, commit={commits}; offscreen lazy allocation, native child-label click, distant reveal, visited handle retention, fresh reopening, stale requests and disposal. Actual VS and physical input remain separate.");
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { form.Close(); }
        };
        Application.Run(form); if (failure is not null) throw failure;
        GalleryStylesControl FindPresenter()
        {
            GalleryStylesControl? found = null;
            // Inspect only this fixture's owned native windows using public
            // framework/Win32 APIs. No runtime reflection or replacement factory.
            EnumThreadWindows(GetCurrentThreadId(), (window, _) =>
            {
                if (GetAncestor(window, 3) == form.Handle && Control.FromHandle(window) is { } root)
                    found = Descendants(root).OfType<GalleryStylesControl>().FirstOrDefault();
                return found is null;
            }, 0);
            return found ?? throw new InvalidOperationException("The selected gallery class was not created.");
        }
        static IEnumerable<Control> Descendants(Control root) => new[] { root }.Concat(root.Controls.Cast<Control>().SelectMany(Descendants));
        async Task Capture(string name, Control view)
        {
            await Task.Delay(50);
            if (GetForegroundWindow() != form.Handle && !form.OwnedForms.Any(owned => owned.Handle == GetForegroundWindow()))
            {
                // ToolStrip popups are owned native windows, not necessarily Forms.
                nint rootOwner = GetAncestor(GetForegroundWindow(), 3);
                Require(rootOwner == form.Handle, "Foreground lost; capture stopped");
            }
            view.Refresh(); _ = GdiFlush(); _ = DwmFlush();
            var popup = view.TopLevelControl ?? view.Parent!;
            var area = popup.RectangleToScreen(popup.ClientRectangle);
            using var image = new Bitmap(area.Width, area.Height); using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
            image.Save(Path.Combine(output, name + ".png"));
        }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    private delegate bool WindowCallback(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, WindowCallback callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("gdi32.dll")] private static extern bool GdiFlush();
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
}
