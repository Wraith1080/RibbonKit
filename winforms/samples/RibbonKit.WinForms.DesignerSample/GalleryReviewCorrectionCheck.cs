using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class GalleryReviewCorrectionCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>(); Exception? failure = null;
        // Keep the source and review action presented together. Programmatic
        // ShowDropDown still requires an actual current presentation anchor.
        using var form = new DesignerForm { Width = 1700 }; var ribbon = form.Controls.OfType<RibbonControl>().Single();
        ribbon.AnimationLevel = RibbonAnimationLevel.None; form.EnableGalleryBackstageGeometryDemo();
        var tab = ribbon.Tabs.Single(t => t.Text == "Gallery geometry");
        var gallery = tab.Groups[1].Items.OfType<RibbonGalleryItem>().Single();
        var cards = tab.Groups[2].Items.OfType<RibbonGalleryItem>().Single(); var open = tab.Groups[3].Items[1];
        if (gallery.SimplifiedPlacement != RibbonSimplifiedPlacement.Inline || cards.SimplifiedPlacement != RibbonSimplifiedPlacement.Inline)
            throw new InvalidOperationException("Review galleries must start pinned in Simplified.");
        int opened = 0, closed = 0, commits = 0; gallery.DropDownOpening += (_, _) => opened++; gallery.DropDownClosed += (_, _) => closed++; gallery.SelectionCommitted += (_, _) => commits++;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); SetForegroundWindow(form.Handle); await Task.Delay(150); RequireOwner();
                foreach (bool office in new[] { false, true })
                foreach (var density in Enum.GetValues<RibbonDensity>())
                foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
                foreach (bool roomier in new[] { false, true })
                {
                    using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
                    ribbon.Renderer = painter; ribbon.Density = density; ribbon.LayoutMode = mode;
                    ribbon.Theme = roomier ? GalleryBackstageGeometrySettings.Gallery(RibbonThemes.Office2024Light) : RibbonThemes.Office2024Light;
                    await Task.Delay(40); RequireOwner();
                    using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
                    // Use the public renderer callback to find the actual review
                    // button, then send local native messages to this fixture.
                    var record = new ButtonRecord(open); using RibbonRenderer observer = office ? new Office(record) : new Neutral(record);
                    ribbon.Renderer = observer; ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
                    if (record.Bounds.IsEmpty) throw new InvalidOperationException("The review action is not presented.");
                    int beforeOpen = opened, beforeClose = closed; var selection = gallery.SelectedItem;
                    var point = new Point(record.Bounds.Left + record.Bounds.Width / 2, record.Bounds.Top + record.Bounds.Height / 2);
                    nint position = (nint)(point.Y << 16 | point.X); SendMessage(ribbon.Handle, 0x201, 1, position); SendMessage(ribbon.Handle, 0x202, 0, position);
                    if (!gallery.IsDropDownOpen || opened != beforeOpen + 1 || closed != beforeClose)
                        throw new InvalidOperationException($"Gallery did not remain open: renderer={office}, density={density}, mode={mode}, roomier={roomier}, openingDelta={opened - beforeOpen}, closedDelta={closed - beforeClose}.");
                    await Task.Delay(200); RequireOwner();
                    if (!gallery.IsDropDownOpen || !ReferenceEquals(selection, gallery.SelectedItem) || commits != 0) throw new InvalidOperationException("Gallery did not retain its visible popup and selection through the message loop.");
                    image.Save(Path.Combine(output, $"review-{office}-{density}-{mode}-{roomier}.png"));
                    gallery.CloseDropDown(); await Task.Delay(30); RequireOwner();
                    if (gallery.IsDropDownOpen || closed != beforeClose + 1) throw new InvalidOperationException("Explicit close did not close exactly once.");
                    evidence.Add($"PASS renderer={office} density={density} mode={mode} roomier={roomier} nativeDpi={ribbon.DeviceDpi}: real sample-button click, one opening, 200ms message-loop retention, same source selection, explicit close once.");
                }
            }
            catch (Exception error) { failure = error; }
            finally { File.WriteAllLines(Path.Combine(output, "result.log"), evidence); form.Close(); }
        };
        Application.Run(form); if (failure is not null) throw failure;
        void RequireOwner()
        {
            nint foreground = GetForegroundWindow();
            if (foreground != form.Handle && GetAncestor(foreground, 3) != form.Handle)
                throw new InvalidOperationException("Foreground lost; stopped the shown native check without sending further input.");
        }
    }
    private sealed class ButtonRecord(RibbonItem item) { internal readonly RibbonItem Item = item; internal Rectangle Bounds; }
    private sealed class Neutral(ButtonRecord record) : RibbonRenderer
    { public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { if (ReferenceEquals(item, record.Item)) record.Bounds = c.Bounds; base.DrawItem(c, item, size, menu); } }
    private sealed class Office(ButtonRecord record) : RibbonOffice2024Renderer
    { public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { if (ReferenceEquals(item, record.Item)) record.Bounds = c.Bounds; base.DrawItem(c, item, size, menu); } }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
}
