using System.ComponentModel;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class GalleryInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    internal static int Run(string directory, bool flat = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        var record = new Recording(); using RibbonRenderer painter = flat ? new Flat(record) : new Office(record);
        using var form = new WF.Form { Text = "Independent dropdown gallery consumer", Width = 970, Height = 600, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = painter }; var tab = new RibbonTab { Text = "Home", KeyTip = "H" }; var group = new RibbonGroup { Text = "Galleries" };
        var gallery = new RibbonGalleryItem { Text = "Styles", KeyTip = "G", TileWidth = 96, TileHeight = 80, ColumnCount = 4, MaxVisibleRows = 2 };
        var field = new RibbonTextBoxItem { Text = "Name", Value = "Accepted", KeyTip = "D" }; field.ValueValidating += (_, e) => e.Cancel = field.Value == "";
        group.Items.AddRange(field, gallery); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "The application owns this preview. Hover changes the color temporarily; Escape or leaving restores the committed selection." };
        form.Controls.Add(document); form.Controls.Add(ribbon);
        var tiles = Enumerable.Range(0, 24).Select(i => new RibbonGalleryTile { Id = "gallery.tile." + i, Text = "Style " + i, KeyTip = "T" + i.ToString("D2"), GroupName = i < 12 ? "Document styles" : "More styles", Enabled = i != 1, Visible = i != 3, Icon = new Swatch(Color.FromArgb(230 - i * 3, 225 - i * 2, 245 - i * 3)) }).ToArray();
        gallery.Items.AddRange(tiles);
        using var source = new WF.BindingSource(); var state = new State { Choice = tiles[15] }; source.DataSource = state;
        gallery.DataBindings.Add(new WF.Binding(nameof(gallery.SelectedItem), source, nameof(state.Choice), true, WF.DataSourceUpdateMode.OnPropertyChanged));
        int previews = 0, cancels = 0, commits = 0, changes = 0, unavailable = 0;
        RibbonGalleryTile? preview = null;
        void ShowChoice(RibbonGalleryTile? tile) => document.BackColor = tile?.Icon is Swatch swatch ? swatch.Color : Color.White;
        gallery.ItemPreview += (_, e) => { previews++; preview = e.Item; ShowChoice(e.Item); };
        gallery.ItemPreviewCancelled += (_, _) => { cancels++; preview = null; ShowChoice(gallery.SelectedItem); };
        gallery.SelectedItemChanged += (_, _) => { changes++; ShowChoice(gallery.SelectedItem); };
        gallery.SelectionCommitted += (_, _) => { commits++; preview = null; };
        ShowChoice(gallery.SelectedItem);
        bool cancelOpening = true; gallery.DropDownOpening += (_, e) => e.Cancel = cancelOpening;
        var log = new List<string>(); Exception? error = null; bool completed = false; var cursor = WF.Cursor.Position;
        EventHandler<FirstChanceExceptionEventArgs> exceptions = (_, e) => { if (e.Exception is COMException { HResult: unchecked((int)0x80040201) }) unavailable++; };
        AppDomain.CurrentDomain.FirstChanceException += exceptions;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard(); document.Focus(); ribbon.Refresh(); await OpenGallery(); Check(!gallery.IsDropDownOpen, "Cancelled gallery opening creates no popup");
                cancelOpening = false; field.PerformClick(); var editor = Tree(ribbon).OfType<WF.TextBox>().Single(); editor.Text = "";
                await OpenGallery(); Check(!gallery.IsDropDownOpen && editor.Focused, "Pending native editor validation prevents gallery opening");
                editor.Text = "Accepted"; await Key("{ENTER}"); document.Focus();
                await Key("{F10}hg"); Check(gallery.IsDropDownOpen && gallery.SelectedItem == tiles[15] && previews == 0, "Gallery KeyTips open the selected row without changing selection or preview");
                Check(Visible(tiles[15]), "The selected tile is visible on opening"); await Capture("selected-row");
                await Key("{ESC}{F10}"); await OpenGallery(); // Exit KeyTips through their normal document-focus return, then start a pointer session.
                await Hover(tiles[14]); int wheelCancel = cancels; Guard(); mouse_event(0x0800, 0, 0, unchecked((uint)-120), 0); await Task.Delay(130);
                Check(cancels > wheelCancel && state.Choice == tiles[15] && commits == 0, "Mouse-wheel scrolling cancels the old preview without changing selection");
                gallery.CloseDropDown(); WF.Cursor.Position = document.PointToScreen(new(25, 30)); await OpenGallery(); int initialCancels = cancels;
                await Hover(tiles[14]); Check(preview == tiles[14] && state.Choice == tiles[15] && commits == 0, "Pointer preview changes application presentation without changing the bound selection");
                await Capture("pointer-preview"); WF.Cursor.Position = document.PointToScreen(new(25, 30)); await Task.Delay(100);
                Check(preview is null && gallery.IsDropDownOpen && cancels == initialCancels + 1 && state.Choice == tiles[15], "Leaving the gallery cancels preview without closing or committing");
                await Key("{ESC}"); Check(!gallery.IsDropDownOpen && cancels == initialCancels + 1 && commits == 0, "Escape after pointer cancellation raises no duplicate cancel or commit");
                await OpenGallery(); await Key("{HOME}{RIGHT}"); Check(preview == tiles[2] && state.Choice == tiles[15], "Keyboard preview skips disabled tiles and keeps committed selection separate");
                await Key("{PGDN}{END}"); Check(preview == tiles[23] && Visible(tiles[23]), "Keyboard scrolling reaches the last tile and keeps it visible");
                await Capture("keyboard-last"); Check(preview == tiles[23], "Capturing and painting do not overwrite keyboard preview with stationary-pointer hover");
                await Hover(tiles[23]); await Key("{LEFT}"); WF.Cursor.Position = document.PointToScreen(new(25, 30)); await Task.Delay(120);
                Check(preview is null && state.Choice == tiles[15], "Real pointer departure cancels a keyboard preview without changing selection");
                await Key("{END}");
                int priorCancel = cancels; await Key("{ESC}"); Check(cancels == priorCancel + 1 && state.Choice == tiles[15] && commits == 0, "Keyboard Escape restores the bound selection once");
                await OpenGallery(); await Click(Popup(), record.Tiles[tiles[14]]);
                Check(!gallery.IsDropDownOpen && state.Choice == tiles[14] && commits == 1, "Pointer selection commits the source and binding once, then closes");
                int selectedChanges = changes, cancelBeforeCommit = cancels;
                await OpenGallery(); await Click(Popup(), record.Tiles[tiles[14]]);
                Check(commits == 2 && changes == selectedChanges && cancels == cancelBeforeCommit, "Reselecting a tile commits once without changing selection or cancelling the accepted preview");
                await OpenGallery(); await Key("{HOME}"); await Click(Popup(), record.Tiles[tiles[1]]);
                Check(commits == 2 && gallery.IsDropDownOpen && state.Choice == tiles[14], "Disabled tiles cannot commit or dismiss the gallery");
                await Key("{END}"); priorCancel = cancels; gallery.SelectedItem = tiles[5];
                Check(preview is null && cancels == priorCancel + 1 && commits == 2 && state.Choice == tiles[5], "Programmatic source selection cancels preview without synthesizing a commit");
                gallery.CloseDropDown(); await OpenGallery(); Check(Visible(tiles[5]), "Reopening reveals a programmatically selected tile");
                await Key("{END}"); priorCancel = cancels; gallery.Items.Remove(tiles[23]);
                Check(!gallery.IsDropDownOpen && cancels == priorCancel + 1 && state.Choice == tiles[5], "Collection mutation closes stale geometry and cancels preview once"); gallery.Items.Add(tiles[23]);
                var parent = new RibbonDropDownItem { Text = "Nested galleries", KeyTip = "N" }; group.Items.Remove(gallery); parent.Items.Add(gallery); group.Items.Add(parent);
                parent.ShowDropDown(); await OpenGallery(); await Key("{HOME}"); await Capture("nested");
                await Key("{ESC}"); Check(parent.IsDropDownOpen && !gallery.IsDropDownOpen && state.Choice == tiles[5], "Nested Escape closes only the gallery and retains its source selection");
                await Key("{ESC}"); Check(!parent.IsDropDownOpen, "A second Escape closes the containing menu");
                parent.Items.Remove(gallery); group.Items.Remove(parent); group.Items.Add(gallery); parent.Dispose();
                await Key("{F10}hgt02"); Check(commits == 3 && state.Choice == tiles[2] && !gallery.IsDropDownOpen, "Multi-character tile KeyTips commit exactly once");
                await OpenGallery(); await Key("{END}"); gallery.Enabled = false;
                Check(!gallery.IsDropDownOpen && preview is null && commits == 3, "Disabling the source closes and cancels its gallery");
                gallery.Enabled = true; await OpenGallery(); await Key("{HOME}");
                priorCancel = cancels; await Click(document, new(15, 30, 80, 40));
                Check(!gallery.IsDropDownOpen && preview is null && cancels == priorCancel + 1 && commits == 3, "An outside press cancels active preview without committing");
                await OpenGallery(); await Key("{HOME}");
                ribbon.Dispose(); Check(preview is null && !gallery.IsDropDownOpen && tiles.All(t => t.Site is null), "Ribbon disposal cancels active preview and releases owned gallery models");
                Check(unavailable == 0, "Gallery interactions raise zero unavailable-element exceptions");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} dropdown gallery at {form.DeviceDpi} DPI"); completed = true;
            }
            catch (Exception e) { error = e; log.Add("FAIL: " + e); try { await Capture("failure"); } catch { } }
            finally { WF.Cursor.Position = cursor; form.Close(); }
        };
        try { WF.Application.Run(form); } finally { AppDomain.CurrentDomain.FirstChanceException -= exceptions; }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log);
        if (error is not null) Console.Error.WriteLine(error); else Console.WriteLine(string.Join(Environment.NewLine, log));
        return completed && error is null ? 0 : 1;
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); log.Add("PASS: " + message); }
        void Guard() { if (GetForegroundWindow() != form.Handle && WF.Control.FromHandle(GetForegroundWindow()) is not WF.ToolStripDropDown) throw new InvalidOperationException("Validation Form lost foreground focus."); }
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(110); }
        async Task OpenGallery()
        {
            Guard(); gallery.ShowDropDown(); await Task.Delay(90);
            if (gallery.IsDropDownOpen) Popup().Refresh();
        }
        WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected gallery/menu popup foreground.");
        bool Visible(RibbonGalleryTile tile) => Rectangle.Intersect(Popup().ClientRectangle, record.Tiles[tile]).Height > 0;
        async Task Hover(RibbonGalleryTile tile)
        {
            Guard(); var popup = Popup(); var bounds = Rectangle.Intersect(popup.ClientRectangle, record.Tiles[tile]); Check(!bounds.IsEmpty, "Pointer tile is visible");
            WF.Cursor.Position = popup.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(120);
        }
        async Task Click(WF.Control control, Rectangle bounds)
        {
            Guard(); var visible = Rectangle.Intersect(control.ClientRectangle, bounds); Check(!visible.IsEmpty, "Click target is visible");
            WF.Cursor.Position = control.PointToScreen(new(visible.Left + visible.Width / 2, visible.Top + visible.Height / 2)); await Task.Delay(50); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(110);
        }
        async Task Capture(string name)
        {
            await Task.Delay(70); Guard(); var bounds = form.Bounds;
            if (WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup) bounds = Rectangle.Union(bounds, popup.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap); graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static IEnumerable<WF.Control> Tree(WF.Control control) => control.Controls.Cast<WF.Control>().SelectMany(c => new[] { c }.Concat(Tree(c)));
    private sealed class State : INotifyPropertyChanged
    {
        private RibbonGalleryTile? _choice;
        public RibbonGalleryTile? Choice { get => _choice; set { if (_choice == value) return; _choice = value; PropertyChanged?.Invoke(this, new(nameof(Choice))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Swatch(Color color) : IRibbonIcon
    {
        internal Color Color => color;
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        {
            using var fill = new SolidBrush(context.Enabled ? color : SystemColors.GrayText); graphics.FillRectangle(fill, bounds);
            using var pen = new Pen(context.Foreground); graphics.DrawRectangle(pen, bounds.X, bounds.Y, Math.Max(0, bounds.Width - 1), Math.Max(0, bounds.Height - 1));
        }
    }
    private sealed class Recording { internal Dictionary<RibbonGalleryTile, Rectangle> Tiles = []; }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawGalleryTile(RibbonPaintContext c, RibbonGalleryTile tile, bool selected) { r.Tiles[tile] = c.Bounds; base.DrawGalleryTile(c, tile, selected); }
    }
    private sealed class Flat(Recording r) : RibbonRenderer
    {
        public override void DrawGalleryTile(RibbonPaintContext c, RibbonGalleryTile tile, bool selected) { r.Tiles[tile] = c.Bounds; base.DrawGalleryTile(c, tile, selected); }
    }
}
