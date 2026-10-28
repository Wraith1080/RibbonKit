using System.ComponentModel;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Application activation, public models and renderer geometry only. This same
// source can run in the independent package consumer without Showcase resources.
internal static class MergeInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    internal static int Run(string directory, bool flat = false)
    {
        directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory);
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        var record = new Recording(); using RibbonRenderer painter = flat ? new Flat(record) : new Office(record);
        using var form = new WF.Form { Text = "Independent merge consumer", Width = 1200, Height = 550, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = painter }; var home = new RibbonTab { Id = "home", Text = "Home", KeyTip = "H" }; var other = new RibbonTab { Id = "other", Text = "Other", KeyTip = "O" };
        var baseGroup = new RibbonGroup { Text = "Host commands" }; var baseAction = new RibbonButtonItem { Text = "Host action", KeyTip = "A" }; baseGroup.Items.Add(baseAction); home.Groups.Add(baseGroup); ribbon.Tabs.AddRange(home, other);
        using var source = new RibbonMergeSource { Text = "Child document", Order = 10 };
        var tools = new RibbonTab { Text = "Chart Design", KeyTip = "T", IsContextual = true, ContextualColor = Color.MediumVioletRed };
        var group = new RibbonGroup { Text = "Chart tools" }; var field = new RibbonTextBoxItem { Id = "child.name", Text = "Series", KeyTip = "N" };
        var apply = new RibbonButtonItem { Text = "Apply chart", KeyTip = "A" };
        var gallery = new RibbonGalleryItem { Text = "Styles", KeyTip = "G", IsInRibbon = true, InRibbonColumnCount = 2, ColumnCount = 2 };
        var light = new RibbonGalleryTile { Text = "Light" }; var dark = new RibbonGalleryTile { Text = "Dark" }; gallery.Items.AddRange(light, dark); gallery.SelectedItem = light;
        group.Items.AddRange(field, apply, gallery); tools.Groups.Add(group); source.Tabs.Add(tools);
        var contribution = new RibbonGroupContribution { TargetTabId = home.Id, Order = -1 }; contribution.Group.Text = "Child contribution";
        var contributedAction = new RibbonButtonItem { Text = "Child action", KeyTip = "C" }; contribution.Group.Items.Add(contributedAction); source.Groups.Add(contribution);
        using var binding = new WF.BindingSource(); var state = new State(); binding.DataSource = state;
        field.DataBindings.Add(new WF.Binding(nameof(field.Value), binding, nameof(State.Name), true, WF.DataSourceUpdateMode.OnPropertyChanged));
        var commandRef = new RibbonQuickAccessReference { Source = apply }; var galleryRef = new RibbonQuickAccessDropDownReference { Source = gallery };
        ribbon.QuickAccessItems.AddRange(commandRef, galleryRef);
        var document = new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "The application activates the child context with the checkbox below. Moving focus does not activate a source. Host collections keep authored content; the source owns its transient models and bindings." };
        var context = new WF.CheckBox { Dock = WF.DockStyle.Bottom, Height = 40, Text = "Application: chart context active" };
        context.CheckedChanged += (_, _) => { if (context.Checked) ribbon.Merge(source); else ribbon.Unmerge(source); };
        form.Controls.Add(document); form.Controls.Add(context); form.Controls.Add(ribbon);
        int actions = 0, contributed = 0, commits = 0, unavailable = 0; apply.Click += (_, _) => actions++; contributedAction.Click += (_, _) => contributed++; gallery.SelectionCommitted += (_, _) => commits++;
        var log = new List<string>(); Exception? error = null; bool completed = false; var cursor = WF.Cursor.Position;
        EventHandler<FirstChanceExceptionEventArgs> exceptions = (_, e) => { if (e.Exception is COMException { HResult: unchecked((int)0x80040201) }) unavailable++; };
        AppDomain.CurrentDomain.FirstChanceException += exceptions;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); form.Activate(); SetForegroundWindow(form.Handle);
                for (int i = 0; i < 20 && GetForegroundWindow() != form.Handle; i++) await Task.Delay(50);
                Guard(); document.Focus(); ribbon.Refresh();
                await Click(ribbon, record.Qat[commandRef]); Check(actions == 0 && !source.IsMerged, "Unmerged QAT reference is parked and cannot execute");
                await Click(context, context.ClientRectangle); ribbon.Refresh();
                Check(source.IsMerged && source.MergedInto == ribbon && ribbon.SelectedTab == home, "Application activation merges the source without changing surviving host selection");
                Check(ribbon.Tabs.SequenceEqual(new[] { home, other }) && home.Groups.Single() == baseGroup && source.Tabs.Single() == tools, "Authored collections and source ownership remain intact");
                Check(ribbon.GetPresentedGroups(home).SequenceEqual(new[] { contribution.Group, baseGroup }), "Negative contribution order places a group before host groups");
                await Click(ribbon, record.Items[contributedAction]); Check(contributed == 1 && source.IsMerged, "Contributed command invokes its original handler once without focus-driven unmerge");
                await Capture("merged-host-group"); await Click(ribbon, record.Tabs[tools]); ribbon.Refresh();
                var editor = Tree(ribbon).OfType<WF.TextBox>().Single(c => c.Name == field.Id); nint handle = editor.Handle;
                await Click(editor, editor.ClientRectangle); await Key("^aChanged series{ENTER}");
                Check(state.Name == "Changed series" && field.Value == state.Name && source.IsMerged, "Merged native field retains its child binding while focus moves");
                editor.Select(2, 3); await Capture("merged-native-field");
                await Click(ribbon, record.Qat[galleryRef]); Check(galleryRef.IsDropDownOpen, "Merged gallery opens through its original QAT reference"); await Key("{END}{ENTER}");
                Check(commits == 1 && gallery.SelectedItem == dark && !galleryRef.IsDropDownOpen, "Merged gallery commits its source once and dismisses the popup");
                await Click(context, context.ClientRectangle); ribbon.Refresh();
                Check(!source.IsMerged && ribbon.SelectedTab == other && !editor.IsDisposed && source.Tabs.Single() == tools, "Unmerge falls back deterministically and parks the original editor");
                await Click(ribbon, record.Qat[commandRef]); await Click(ribbon, record.Qat[galleryRef]);
                Check(actions == 0 && !galleryRef.IsDropDownOpen && ribbon.QuickAccessItems.Count == 2, "Parked command/gallery references remain present and disabled");
                await Capture("parked-qat"); await Click(context, context.ClientRectangle); ribbon.Refresh(); await Click(ribbon, record.Tabs[tools]);
                var restored = Tree(ribbon).OfType<WF.TextBox>().Single(c => c.Name == field.Id);
                Check(ReferenceEquals(editor, restored) && restored.Handle == handle && restored.Text == "Changed series", "Remerge reuses the native editor and child data");
                ribbon.SelectedTab = home; ribbon.Refresh(); await Click(ribbon, record.Qat[commandRef]); Check(actions == 1 && ribbon.SelectedTab == home, "Revived QAT command executes once while its source tab is inactive");
                ribbon.QuickAccessMaxWidth = 28; ribbon.Refresh(); await Click(ribbon, record.Overflow); var overflow = Popup(); overflow.Refresh();
                await Click(overflow, record.MenuQat[galleryRef]); await Key("{HOME}{ENTER}");
                Check(commits == 2 && gallery.SelectedItem == light && !overflow.Visible, "Merged gallery uses the normal overflow stack and source commit");
                ribbon.QuickAccessMaxWidth = 240; ribbon.Refresh(); await Click(ribbon, record.Qat[galleryRef]);
                source.Dispose(); Check(!galleryRef.IsDropDownOpen && editor.IsDisposed && ribbon.GetPresentedTabs().SequenceEqual(new[] { home, other }), "Source disposal closes projections, releases its native cache and leaves host content intact");
                Check(RibbonControl.GetMergeSource(commandRef) == source && actions == 1 && contributed == 1, "Parked QAT keeps transient provenance without extra execution");
                Check(unavailable == 0, "Merge, native input, popup removal and disposal raise zero unavailable-element exceptions");
                log.Add($"PASS: {(flat ? "Neutral" : "Office")} merge consumer at {form.DeviceDpi} DPI"); completed = true;
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
        async Task Key(string keys) { Guard(); WF.SendKeys.SendWait(keys); await Task.Delay(100); }
        async Task Click(WF.Control c, Rectangle bounds)
        {
            Guard(); bounds = Rectangle.Intersect(c.ClientRectangle, bounds); Check(!bounds.IsEmpty, "Pointer target is visible");
            WF.Cursor.Position = c.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(60); Guard();
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(100);
        }
        async Task Capture(string name)
        {
            await Task.Delay(60); Guard(); var bounds = form.Bounds;
            if (WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup) bounds = Rectangle.Union(bounds, popup.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap); graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
    private static IEnumerable<WF.Control> Tree(WF.Control c) => c.Controls.Cast<WF.Control>().SelectMany(child => new[] { child }.Concat(Tree(child)));
    private static WF.Control Popup() => WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown popup ? popup.Items.OfType<WF.ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected a library popup.");
    private sealed class State : INotifyPropertyChanged
    {
        private string _name = "Revenue";
        public string Name { get => _name; set { _name = value; PropertyChanged?.Invoke(this, new(nameof(Name))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonTab, Rectangle> Tabs = [];
        internal readonly Dictionary<RibbonItem, Rectangle> Items = [];
        internal readonly Dictionary<RibbonElement, Rectangle> Qat = [], MenuQat = [];
        internal Rectangle Overflow;
        internal void Entry(RibbonPaintContext c, RibbonElement entry, bool menu) => (menu ? MenuQat : Qat)[entry] = c.Bounds;
    }
    private sealed class Office(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tabs[tab] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
    }
    private sealed class Flat(Recording r) : RibbonRenderer
    {
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.Tabs[tab] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Items[item] = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu) { r.Entry(c, entry, menu); base.DrawQuickAccessItem(c, entry, menu); }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c) { r.Overflow = c.Bounds; base.DrawQuickAccessOverflow(c); }
    }
}
