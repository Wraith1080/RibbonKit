using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class GalleryHostPresentationCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var probe = new GroupProbe();
        using var form = new DesignerForm { StartPosition = FormStartPosition.Manual, Location = new(100, 80) };
        var ribbon = form.Controls.OfType<RibbonControl>().Single(); var (tab, gallery) = form.GalleryPresenterExample;
        int originalQatWidth = ribbon.QuickAccessMaxWidth;
        var group = tab.Groups.Single(group => group.Items.Contains(gallery)); probe.Source = group; ribbon.Renderer = probe;
        ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.SelectedTab = tab; Exception? failure = null; int commits = 0;
        gallery.SelectionCommitted += (_, _) => commits++;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); await Task.Delay(120);
                var permanent = Descendants(form).OfType<GalleryStylesStripControl>().Single(); nint permanentHandle = permanent.Handle;
                gallery.ShowDropDown(); await Task.Delay(100); var expanded = FindOwned<GalleryStylesControl>(form);
                CheckCards(expanded); await Capture("expanded-initial", expanded);
                PostKey(expanded, Keys.End); await Task.Delay(70); CheckCards(expanded); await Capture("expanded-last-page", expanded);
                gallery.CloseDropDown(); Require(expanded.IsDisposed && !permanent.IsDisposed, "Expanded and permanent views have separate lifetimes");
                RibbonQuickAccessGroupReference? groupReference = null;
                foreach (bool addedToQat in new[] { false, true })
                {
                    if (addedToQat) { groupReference = new RibbonQuickAccessGroupReference { Source = group, KeyTip = "8" }; ribbon.QuickAccessItems.Add(groupReference); }
                    form.Width = 350; await Task.Delay(80); probe.CollapsedBounds = Rectangle.Empty; ribbon.Refresh(); await Task.Delay(50);
                    Require(!probe.CollapsedBounds.IsEmpty, "The actual collapsed source-group button is painted");
                    Click(ribbon, new(probe.CollapsedBounds.Left + probe.CollapsedBounds.Width / 2, probe.CollapsedBounds.Top + probe.CollapsedBounds.Height / 2));
                    await Task.Delay(100); var temporary = FindOwned<GalleryStylesStripControl>(form);
                    CheckBody(temporary, false); CheckCards(temporary); Require(!ReferenceEquals(temporary, permanent), "Collapsed group owns a separate inline UserControl");
                    await Capture(addedToQat ? "collapsed-group-with-qat-reference" : "collapsed-group", temporary);
                    await ExpandAndCancel(temporary, false, addedToQat ? "collapsed-with-qat-expanded" : "collapsed-expanded");
                    ClickCard(temporary); await Task.Delay(70); Require(temporary.IsDisposed, "Committing a collapsed-group tile disposes its session body");
                }
                form.Width = 1030; ribbon.SelectedTab = ribbon.Tabs.First(candidate => candidate != tab); var inactiveTab = ribbon.SelectedTab;
                await OpenQatGroup(false); var qat = FindOwned<GalleryStylesStripControl>(form); CheckBody(qat, true); CheckCards(qat); await Capture("qat-group", qat);
                await ExpandAndCancel(qat, true, "qat-group-expanded"); ClickCard(qat); await Task.Delay(70);
                Require(qat.IsDisposed && ReferenceEquals(ribbon.SelectedTab, inactiveTab), "QAT group commit closes its body without selecting the source tab");
                ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; ribbon.QuickAccessMaxWidth = 28;
                await OpenQatGroup(true); var overflow = FindOwned<GalleryStylesStripControl>(form); CheckBody(overflow, true); CheckCards(overflow); await Capture("qat-overflow-group", overflow);
                PostKey(overflow, Keys.Escape); await Task.Delay(70); PostKey(ribbon, Keys.Escape); await Task.Delay(70);
                Require(overflow.IsDisposed, "QAT overflow group body retires on Escape");
                ribbon.QuickAccessItems.Remove(groupReference!); groupReference!.Dispose(); ribbon.QuickAccessMaxWidth = originalQatWidth;
                ribbon.SelectedTab = tab; ribbon.IsMinimized = true; PostKey(ribbon, Keys.Escape); await Task.Delay(50); PostKey(ribbon, Keys.F10); await Task.Delay(60); PostKey(ribbon, Keys.G); await Task.Delay(120);
                var minimized = FindOwned<GalleryStylesStripControl>(form); CheckBody(minimized, false); CheckCards(minimized); await Capture("minimized-tab", minimized);
                PostKey(minimized, Keys.Escape); await Task.Delay(75); Require(minimized.IsDisposed, "Minimized body is session-owned");
                Require(permanent.Handle == permanentHandle && !permanent.IsDisposed && gallery.Items.Count == 18 && commits == 3, "Original inline handle, source items and exactly-once commits survive all routes");
                log.WriteLine($"PASS: package-only public consumer; actual DPI={form.DeviceDpi}; all 18 authored/dynamic card preview labels contained on initial and last pages; custom collapsed group with/without QAT reference; direct/overflow QAT group; minimized-tab body; separate controls; F4/Escape restoration; native child-label commit; inactive source tab; original permanent handle and 18 source tiles; three commits. Physical input, mixed-monitor DPI and actual VS remain separate.");
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { form.Close(); }
        };
        Application.Run(form); if (failure is not null) throw failure;

        void CheckCards(Control view)
        {
            bool valid = true;
            foreach (var card in view.Controls.OfType<GalleryStyleTileControl>())
            {
                var paper = card.Controls.Cast<Control>().Single(child => child.Name == "paperPanel");
                var preview = paper.Controls.Cast<Control>().Single(child => child.Name == "previewLabel");
                log.WriteLine($"{view.GetType().Name}/{card.Caption}: card={card.Bounds}; paper={paper.Bounds}; preview={preview.Bounds}; dpi={card.DeviceDpi}");
                // The accepted shorter inline card can clip blank label space.
                // Verify its centered text; expanded cards contain the full label.
                var text = TextRenderer.MeasureText(preview.Text, preview.Font, Size.Empty, TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
                var textBounds = new Rectangle(preview.Left + (preview.Width - text.Width) / 2, preview.Top + (preview.Height - text.Height) / 2, text.Width, text.Height);
                valid &= paper.ClientRectangle.Contains(view is GalleryStylesControl ? preview.Bounds : textBounds);
            }
            Require(valid, "Every authored/dynamic card keeps its native preview text visible inside the paper panel");
        }
        void CheckBody(GalleryStylesStripControl view, bool qat)
        {
            Require(view.Context.IsActive && view.Context.Presentation == RibbonGalleryPresentation.InRibbon && view.Context.IsQuickAccess == qat && ReferenceEquals(view.Context.Gallery, gallery), "Temporary body has correct original source and inline/QAT context");
        }
        async Task ExpandAndCancel(GalleryStylesStripControl body, bool qat, string capture)
        {
            body.ScrollView(RibbonGalleryScroll.RowForward); int row = body.ViewedRow; nint handle = body.Handle;
            body.Focus(); PostKey(body, Keys.F4); await Task.Delay(100); var popup = FindOwned<GalleryStylesControl>(form);
            Require(popup.Context.IsQuickAccess == qat && popup.Context.Presentation == RibbonGalleryPresentation.DropDown && !body.Context.IsActive, "Expanded popup preserves route and suspends its inline opener");
            CheckCards(popup); await Capture(capture, popup); PostKey(popup, Keys.Escape); await Task.Delay(100);
            Require(popup.IsDisposed && body.Context.IsActive && body.ViewedRow == row && body.Handle == handle && body.ContainsFocus, "Escape restores the same temporary native body, browsed row and focus");
        }
        void ClickCard(GalleryStylesStripControl body)
        {
            var card = body.Controls.OfType<GalleryStyleTileControl>().First(card => body.ClientRectangle.Contains(card.Bounds));
            var label = card.Controls.Cast<Control>().Single(child => child.Name == "titleLabel"); int before = commits;
            Click(label, new(4, 4)); Require(commits == before + 1 && ReferenceEquals(gallery.SelectedItem, card.Tag), "Native child label commits its original tile exactly once");
        }
        async Task OpenQatGroup(bool overflow)
        {
            PostKey(ribbon, Keys.Escape); await Task.Delay(50); PostKey(ribbon, Keys.F10); await Task.Delay(70);
            if (overflow) { PostKey(ribbon, Keys.D0); await Task.Delay(80); }
            PostKey(ribbon, Keys.D8); await Task.Delay(120);
        }
        async Task Capture(string name, Control view)
        {
            await Task.Delay(70); var bounds = Rectangle.Union(form.Bounds, view.TopLevelControl!.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(output, name + ".png"));
        }
    }
    private sealed class GroupProbe : RibbonOffice2024Renderer
    {
        internal RibbonGroup? Source;
        internal Rectangle CollapsedBounds;
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
        { if (ReferenceEquals(group, Source) && collapsed) CollapsedBounds = context.Bounds; base.DrawGroup(context, group, collapsed); }
    }
    private static T FindOwned<T>(Form owner) where T : Control
    {
        T? found = null;
        EnumThreadWindows(GetCurrentThreadId(), (window, _) =>
        {
            if (GetAncestor(window, 3) == owner.Handle && Control.FromHandle(window) is { } control)
                found ??= Descendants(control).OfType<T>().FirstOrDefault(view => view.Visible && !ReferenceEquals(view.TopLevelControl, owner));
            return true;
        }, 0);
        return found ?? throw new InvalidOperationException("Expected native gallery view: " + typeof(T).Name);
    }
    private static IEnumerable<Control> Descendants(Control parent) => new[] { parent }.Concat(parent.Controls.Cast<Control>().SelectMany(Descendants));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Click(Control control, Point point) { nint position = (nint)((point.Y << 16) | (point.X & 0xffff)); _ = SendMessage(control.Handle, 0x201, 1, position); _ = SendMessage(control.Handle, 0x202, 0, position); }
    private static void PostKey(Control control, Keys key) { _ = PostMessage(control.Handle, 0x100, (nint)key, 0); _ = PostMessage(control.Handle, 0x101, (nint)key, 0); }
    private delegate bool EnumWindow(nint window, nint data);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint data);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
