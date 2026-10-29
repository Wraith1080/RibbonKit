using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

internal static class GeometryMotionInteractionChecks
{
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetFocus();
    [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint parameter, out bool value, uint flags);
    [DllImport("gdi32.dll")] private static extern int GetDeviceCaps(nint dc, int index);
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(nint destination, int x, int y, int width, int height, nint source, int sourceX, int sourceY, uint operation);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint x, uint y, uint data, nuint extra);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    private delegate bool WindowVisitor(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, WindowVisitor visitor, nint state);
    internal static int Run(string directory, bool neutral, bool menusOnly = false, bool tabsOnly = false, RibbonTabContentAnimation tabAnimation = RibbonTabContentAnimation.Fade, bool headersOnly = false, RibbonAnimationLevel tabLevel = RibbonAnimationLevel.Subtle, bool minimizeOnly = false)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2); WF.Application.EnableVisualStyles(); WF.Application.SetUnhandledExceptionMode(WF.UnhandledExceptionMode.ThrowException);
        using var trace = new TextWriterTraceListener(Console.Out); Trace.Listeners.Add(trace);
        Directory.CreateDirectory(directory); List<string> log = []; List<object> timing = []; Exception? error = null; var recording = new Recording();
        using RibbonRenderer renderer = neutral ? new NeutralRecorder(recording) : new OfficeRecorder(recording);
        using var host = new WF.Form { Text = "Independent geometry motion prototype", Width = 1250, Height = 750, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer, AnimationLevel = RibbonAnimationLevel.Subtle, TabContentAnimation = tabAnimation }; recording.Ribbon = ribbon;
        var home = new RibbonTab { Id = "home", Text = "Home" }; var tools = new RibbonGroup { Id = "tools", Text = "Clipboard" };
        var copy = new RibbonButtonItem { Id = "copy", Text = "Copy", Icon = new SampleIcon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var combo = new RibbonComboBoxItem { Id = "zoom", Text = "Zoom", IsEditable = true, Value = "100", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; combo.Items.AddRange("100", "150"); tools.Items.AddRange(copy, combo); home.Groups.Add(tools);
        for (int i = 0; i < 3; i++) { var group = new RibbonGroup { Id = "group." + i, Text = "Commands " + i }; for (int j = 0; j < 6; j++) group.Items.Add(new RibbonButtonItem { Id = i + "." + j, Text = "Command " + j, Icon = new SampleIcon(), SimplifiedPlacement = j < 2 ? RibbonSimplifiedPlacement.Inline : RibbonSimplifiedPlacement.Overflow }); home.Groups.Add(group); }
        ribbon.Tabs.Add(home); ribbon.QuickAccessItems.AddRange(new RibbonQuickAccessReference { Source = copy }, new RibbonQuickAccessComboBoxReference { Source = combo });
        if (minimizeOnly) ribbon.MessageBar.Items.Add(new RibbonMessage { Title = "Notice", Text = "This message follows the ribbon and toolbar edge.", Icon = new SampleIcon() });
        if (tabsOnly)
        {
            var insert = new RibbonTab { Id = "insert", Text = "Insert" }; var group = new RibbonGroup { Id = "insert.tools", Text = "Illustrations" };
            group.Items.Add(new RibbonButtonItem { Id = "picture", Text = "Picture", Icon = new SampleIcon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); insert.Groups.Add(group); ribbon.Tabs.Add(insert);
        }
        if (menusOnly)
        {
            var mouseIcon = new SampleIcon(); var touchIcon = new SampleIcon();
            var selector = new RibbonDropDownItem { Id = "density.selector", Text = "Mouse", Icon = mouseIcon, DropDownHeader = "Optimize spacing between commands" };
            var mouse = new RibbonRadioButtonItem { Id = "density.mouse", Text = "Mouse", Checked = true }; var touch = new RibbonRadioButtonItem { Id = "density.touch", Text = "Touch" }; selector.Items.AddRange(mouse, touch); tools.Items.Add(selector);
            void Sync()
            {
                mouse.Checked = ribbon.Density == RibbonDensity.Compact; touch.Checked = !mouse.Checked; selector.Text = mouse.Checked ? "Mouse" : "Touch";
                var icon = mouse.Checked ? mouseIcon : touchIcon; if (!ReferenceEquals(selector.Icon, icon)) selector.Icon = icon;
            }
            mouse.Click += (_, _) => { ribbon.Density = RibbonDensity.Compact; Sync(); }; touch.Click += (_, _) => { ribbon.Density = RibbonDensity.Touch; Sync(); }; ribbon.DensityChanged += (_, _) => Sync();
            ribbon.QuickAccessItems.Add(new RibbonQuickAccessDropDownReference { Source = selector });
        }
        host.Controls.Add(new WF.TextBox { Dock = WF.DockStyle.Fill, Multiline = true, Text = "Application-owned content commits its layout immediately in this first motion pass. The library owns ribbon drawings and completion." }); host.Controls.Add(ribbon);
        var cursor = WF.Cursor.Position;
        host.Shown += async (_, _) =>
        {
            try
            {
                await Activate(); await Task.Delay(headersOnly || minimizeOnly || tabLevel == RibbonAnimationLevel.Expressive ? 300 : 100); host.PerformLayout(); ribbon.Refresh();
                bool enabled = SystemParametersInfo(0x1042, 0, out bool system, 0) && system;
                if (!enabled) { log.Add("SKIP: OS client-area animations are disabled; native opt-in playback was not forced."); return; }
                if (menusOnly) { await Menus(); return; }
                if (tabsOnly) { await Tabs(); return; }
                if (headersOnly) { await Headers(); return; }
                if (minimizeOnly) { await Minimize(); return; }
                using (var dc = host.CreateGraphics()) { var handle = dc.GetHdc(); try { log.Add($"Reported GDI refresh Hz={GetDeviceCaps(handle, 116)}; monitor={WF.Screen.FromControl(host).DeviceName}; DPI={ribbon.DeviceDpi}"); } finally { dc.ReleaseHdc(handle); } }
                var editor = Descendants(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id); nint editorHandle = editor.Handle;
                log.Add($"Native value='{editor.Text}', model='{combo.Value}', field={editor.Bounds}, parent={editor.Parent?.Bounds}");
                using (var endpoint = new Bitmap(ribbon.Width, ribbon.Height)) { ribbon.DrawToBitmap(endpoint, new(Point.Empty, endpoint.Size)); endpoint.Save(Path.Combine(directory, "initial-endpoint.png")); }
                await Measure("layout-first", () => ribbon.LayoutMode = RibbonLayoutMode.Simplified);
                await Measure("layout-return", () => ribbon.LayoutMode = RibbonLayoutMode.Classic);
                await Measure("touch-first", () => ribbon.Density = RibbonDensity.Touch);
                await Measure("compact-return", () => ribbon.Density = RibbonDensity.Compact);
                await Measure("qat-below", () => ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon);
                await Measure("qat-header", () => ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow);
                foreach (var operation in new[] { "layout", "touch", "qat" })
                {
                    ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
                    Rectangle before = ribbon.RectangleToScreen(ribbon.ClientRectangle);
                    if (operation == "layout") ribbon.LayoutMode = RibbonLayoutMode.Simplified;
                    else if (operation == "touch") ribbon.Density = RibbonDensity.Touch;
                    else ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
                    Check(ribbon.IsAnimating, operation + " visible drawing plane starts"); var bounds = Rectangle.Union(before, ribbon.RectangleToScreen(ribbon.ClientRectangle)); List<Bitmap> frames = [];
                    try
                    {
                        foreach (int delay in Enumerable.Repeat(12, 20)) { await Task.Delay(delay); var frame = new Bitmap(bounds.Width, bounds.Height); using (var g = Graphics.FromImage(frame)) { var source = GetDC(0); var destination = g.GetHdc(); try { Check(BitBlt(destination, 0, 0, bounds.Width, bounds.Height, source, bounds.X, bounds.Y, 0x40CC0020), "Visible frame capture"); } finally { g.ReleaseHdc(destination); ReleaseDC(0, source); } } frames.Add(frame); }
                        await Finished(); for (int i = 0; i < frames.Count; i++) frames[i].Save(Path.Combine(directory, operation + "-" + i + ".png"));
                    }
                    finally { foreach (var frame in frames) frame.Dispose(); }
                }
                Check(editor.Handle == editorHandle && ReferenceEquals(combo.Parent, tools), "Native editor handle and command owner survive all transitions");
                editor.Focus(); editor.Select(0, 0); nint focus = GetFocus(); ribbon.Density = RibbonDensity.Compact; Check(ribbon.IsAnimating && GetFocus() == focus, "Density motion retains focused native editor");
                WF.SendKeys.SendWait("x"); await Task.Delay(40); Check(!ribbon.IsAnimating && editor.Text.StartsWith("x", StringComparison.Ordinal), "Keyboard input settles motion and reaches the real editor");
                ribbon.LayoutMode = RibbonLayoutMode.Classic; ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; Check(!ribbon.IsAnimating, "Rapid geometry requests settle the newest committed state");
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.Density = RibbonDensity.Touch; Check(!ribbon.IsAnimating && ribbon.Density == RibbonDensity.Touch, "None policy retains immediate behavior");
                log.Add("PASS: public-only geometry prototype; timing is a paint proxy and is not smoothness acceptance.");
            }
            catch (Exception failure) { error = failure; log.Add(failure.ToString()); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); if (!menusOnly && !tabsOnly && !headersOnly && !minimizeOnly) File.WriteAllText(Path.Combine(directory, "timing.json"), JsonSerializer.Serialize(timing, new JsonSerializerOptions { WriteIndented = true })); if (menusOnly || tabsOnly || headersOnly || minimizeOnly) WF.Cursor.Position = cursor; host.Close(); }
        };
        try { WF.Application.Run(host); return error is null ? 0 : 1; }
        finally { Trace.Listeners.Remove(trace); }
        void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); log.Add("PASS: " + text); }
        async Task Activate() { for (int n = 0; n < 4 && GetForegroundWindow() != host.Handle; n++) { host.Activate(); SetForegroundWindow(host.Handle); await Task.Delay(150); } Check(GetForegroundWindow() == host.Handle, "Native host has foreground"); }
        async Task Finished() { var deadline = Stopwatch.StartNew(); while (ribbon.IsAnimating && deadline.ElapsedMilliseconds < 2000) await Task.Delay(2); Check(!ribbon.IsAnimating, "Library completes without host acknowledgement"); }
        async Task Minimize()
        {
            var editor = Descendants(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id); nint handle = editor.Handle; int changes = 0; ribbon.IsMinimizedChanged += (_, _) => changes++;
            foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
            foreach (var placement in Enum.GetValues<RibbonQuickAccessPosition>())
            {
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.LayoutMode = mode; ribbon.QuickAccessPosition = placement;
                ribbon.Density = placement == RibbonQuickAccessPosition.TabRow ? RibbonDensity.Compact : RibbonDensity.Touch;
                ribbon.AnimationLevel = placement == RibbonQuickAccessPosition.TabRow ? RibbonAnimationLevel.Subtle : RibbonAnimationLevel.Expressive;
                int expanded = ribbon.Height; int count = changes; ribbon.Refresh();
                foreach (bool minimized in new[] { true, false })
                {
                    string name = mode + "-" + placement + (minimized ? "-minimize" : "-restore"); var before = ribbon.RectangleToScreen(ribbon.ClientRectangle);
                    recording.Paints.Clear(); recording.QatFrames.Clear(); recording.MessageFrames.Clear(); await Click(ribbon, recording.MinimizeButton);
                    Check(ribbon.IsMinimized == minimized && ribbon.IsAnimating, name + " commits and plays from native utility input");
                    var canvas = Rectangle.Union(before, ribbon.RectangleToScreen(ribbon.ClientRectangle)); List<Bitmap> frames = [];
                    try
                    {
                        foreach (int delay in new[] { 12, 35, 35 }) { await Task.Delay(delay); frames.Add(ScreenFrame(canvas)); }
                        await Finished(); Check(recording.Paints.Count >= 3, name + " presents multiple body frames");
                        Check(recording.MessageFrames.Select(b => b.Y).Distinct().Count() > 2, name + " message frame follows the moving edge");
                        if (placement == RibbonQuickAccessPosition.BelowRibbon) Check(recording.QatFrames.Select(b => b.Y).Distinct().Count() > 2, name + " toolbar follows the moving edge");
                        for (int n = 0; n < frames.Count; n++) frames[n].Save(Path.Combine(directory, name + "-" + n + ".png"));
                        using var final = ScreenFrame(ribbon.RectangleToScreen(ribbon.ClientRectangle)); final.Save(Path.Combine(directory, name + "-final.png"));
                    }
                    finally { foreach (var frame in frames) frame.Dispose(); }
                    Check(editor.Handle == handle && ReferenceEquals(combo.Parent, tools) && ribbon.MessageBar.HasOpenMessages && ribbon.QuickAccessItems.Count == 2, name + " retains native editor, source, QAT and message state");
                    Check(GetForegroundWindow() == host.Handle, name + " retains the host foreground");
                }
                Check(changes == count + 2 && ribbon.Height == expanded, "Minimize and restore each publish once and restore the exact expanded height");
            }
            WF.SendKeys.SendWait("^{F1}"); await Task.Delay(20); Check(ribbon.IsMinimized && ribbon.IsAnimating, "Ctrl+F1 starts minimizing motion"); await Finished();
            await Task.Delay(WF.SystemInformation.DoubleClickTime + 50); ribbon.Refresh(); var tabBounds = recording.TabBounds[home.Id];
            WF.Cursor.Position = ribbon.PointToScreen(new(tabBounds.Left + tabBounds.Width / 2, tabBounds.Top + tabBounds.Height / 2));
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(70);
            mouse_event(2, 0, 0, 0, 0); mouse_event(4, 0, 0, 0, 0); await Task.Delay(20);
            Check(!ribbon.IsMinimized && ribbon.IsAnimating && Popup() is null, "Double-click restores with motion and closes the temporary body popup"); await Finished();
            ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.IsMinimized = true; Check(!ribbon.IsAnimating, "None policy keeps minimizing immediate");
            log.Add($"PASS: native minimize/restore, Classic/Simplified, TabRow/BelowRibbon QAT, Compact/Touch, Subtle/Expressive and message joins; DPI={ribbon.DeviceDpi}; renderer={(neutral ? "neutral" : "Office2024")}");
        }
        async Task Headers()
        {
            var context = new RibbonTab { Id = "context", Text = "Picture Format", IsContextual = true, ContextualColor = Color.Purple, Visible = false };
            var picture = new RibbonGroup { Id = "picture.tools", Text = "Picture tools" }; picture.Items.Add(new RibbonButtonItem { Id = "crop", Text = "Crop", Icon = new SampleIcon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); context.Groups.Add(picture); ribbon.Tabs.Add(context);
            using var source = new RibbonMergeSource { Order = -1 }; var chart = new RibbonTab { Id = "chart", Text = "Chart Design", IsContextual = true, ContextualColor = Color.Teal }; var data = new RibbonGroup { Id = "chart.data", Text = "Chart data" }; data.Items.Add(new RibbonButtonItem { Id = "chart.update", Text = "Update chart", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); chart.Groups.Add(data); source.Tabs.Add(chart);
            var show = new RibbonButtonItem { Id = "show.context", Text = "Picture", Icon = new SampleIcon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedPriority = 100 }; show.Click += (_, _) => { context.Visible = true; ribbon.SelectedTab = context; };
            var merge = new RibbonButtonItem { Id = "merge.chart", Text = "Chart", Icon = new SampleIcon(), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedPriority = 100 }; merge.Click += (_, _) => { ribbon.Merge(source); ribbon.SelectedTab = chart; }; tools.Items.AddRange(show, merge);
            var editor = Descendants(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id); nint handle = editor.Handle;
            foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
            foreach (var style in Enum.GetValues<RibbonTabContentAnimation>())
            {
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.LayoutMode = mode; ribbon.SelectedTab = home; ribbon.TabContentAnimation = style; ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
                foreach (var action in new[] { show, merge })
                {
                    recording.BodyItems.Clear(); ribbon.Refresh(); recording.Paints.Clear(); await Click(ribbon, recording.BodyItems[action.Id]);
                    string name = mode + "-" + style + "-" + action.Id; var expected = ReferenceEquals(action, show) ? context : chart;
                    Check(ReferenceEquals(ribbon.SelectedTab, expected) && ribbon.IsAnimating, name + $" coalesces header arrival and selection (selected={ribbon.SelectedTab?.Id}, playing={ribbon.IsAnimating})");
                    List<Bitmap> frames = [];
                    try
                    {
                        foreach (int delay in new[] { 8, 25, 30 }) { await Task.Delay(delay); frames.Add(ScreenFrame(ribbon.RectangleToScreen(ribbon.ClientRectangle))); }
                        await Finished(); Check(recording.Paints.Count >= 3, name + " presents multiple frames");
                        for (int n = 0; n < frames.Count; n++) frames[n].Save(Path.Combine(directory, name + "-" + n + ".png"));
                    }
                    finally { foreach (var frame in frames) frame.Dispose(); }
                    Check(editor.Handle == handle && ReferenceEquals(combo.Parent, tools) && (!ReferenceEquals(action, merge) || ReferenceEquals(RibbonControl.GetMergeSource(chart), source)), name + " retains source ownership and native editor handles");
                    if (ReferenceEquals(action, show)) context.Visible = false; else ribbon.Unmerge(source);
                    Check(!ribbon.IsAnimating && ReferenceEquals(ribbon.SelectedTab, home), "Removal keeps immediate selection fallback");
                }
            }
            log.Add($"PASS: contextual and merged header arrivals, Fade/SlideDown, Classic/Simplified, native pointer actions; DPI={ribbon.DeviceDpi}; renderer={(neutral ? "neutral" : "Office2024")}");
        }
        async Task Tabs()
        {
            var insert = ribbon.Tabs.Single(t => t.Id == "insert"); var editor = Descendants(ribbon).OfType<WF.ComboBox>().Single(c => c.Name == combo.Id); nint handle = editor.Handle;
            foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
            {
                ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.LayoutMode = mode; ribbon.SelectedTab = home; ribbon.AnimationLevel = tabLevel; ribbon.Refresh();
                foreach (var tab in new[] { insert, home })
                {
                    await Switch(mode + "-pointer-" + tab.Id, tab, async () => await Click(ribbon, recording.TabBounds[tab.Id]));
                    Check(editor.Handle == handle && ReferenceEquals(combo.Parent, tools), "Tab switch retains native editor handle and command ownership");
                }
                await Switch(mode + "-keyboard-insert", insert, async () => { WF.SendKeys.SendWait("{RIGHT}"); await Task.Delay(20); });
                ribbon.AnimationLevel = RibbonAnimationLevel.None; WF.SendKeys.SendWait("{LEFT}"); await Task.Delay(20); Check(ReferenceEquals(ribbon.SelectedTab, home) && !ribbon.IsAnimating, "None policy keeps keyboard selection immediate");
                if (!neutral)
                {
                    ribbon.Refresh(); var hover = recording.TabBounds[insert.Id]; WF.Cursor.Position = ribbon.PointToScreen(new(hover.Left + hover.Width / 2, hover.Top + hover.Height / 2)); await Task.Delay(40);
                    using var frame = ScreenFrame(ribbon.RectangleToScreen(ribbon.ClientRectangle)); var selected = Indicator(home); var passive = Indicator(insert);
                    Check(frame.GetPixel(selected.Left + selected.Width / 2, selected.Top + selected.Height / 2).ToArgb() == ribbon.Theme.Accent.ToArgb(), "Selected Office tab remains blue while another tab is hovered");
                    Check(frame.GetPixel(passive.Left + passive.Width / 2, passive.Top + passive.Height / 2).ToArgb() == ribbon.Theme.SecondaryText.ToArgb(), "Hovered Office tab indicator is themed grey"); frame.Save(Path.Combine(directory, mode + "-hover-grey.png"));
                }
            }
            log.Add($"PASS: pointer and keyboard tab motion in Classic/Simplified; style={ribbon.TabContentAnimation}; DPI={ribbon.DeviceDpi}; renderer={(neutral ? "neutral" : "Office2024")}; level={tabLevel}");
            async Task Switch(string name, RibbonTab tab, Func<Task> trigger)
            {
                var before = Indicator(ribbon.SelectedTab!); recording.Paints.Clear(); recording.Indicators.Clear(); await trigger();
                Check(ReferenceEquals(tab, ribbon.SelectedTab) && ribbon.IsAnimating, name + $" remains active after native selection (selected={ribbon.SelectedTab?.Id}, animating={ribbon.IsAnimating}, minimized={ribbon.IsMinimized})"); var after = Indicator(tab); List<Bitmap> frames = [];
                try
                {
                    foreach (int delay in tabLevel == RibbonAnimationLevel.Expressive ? new[] { 20, 45, 25 } : new[] { 12, 25, 25 }) { await Task.Delay(delay); frames.Add(ScreenFrame(ribbon.RectangleToScreen(ribbon.ClientRectangle))); }
                    await Finished(); Check(recording.Paints.Count >= 3, name + " draws multiple body frames");
                    if (!neutral) Check(recording.Indicators.Any(b => b.Left > Math.Min(before.Left, after.Left) && b.Left < Math.Max(before.Left, after.Left)), name + " indicator moves before completion");
                    if (!neutral && tabLevel == RibbonAnimationLevel.Expressive)
                    {
                        int direction = Math.Sign(after.Left - before.Left); int overshoot = recording.Indicators.Max(b => direction * (b.Left - after.Left));
                        Check(overshoot > 0, name + $" indicator springs past its destination by {overshoot} pixels");
                        using var final = ScreenFrame(ribbon.RectangleToScreen(ribbon.ClientRectangle));
                        Check(final.GetPixel(after.Left + after.Width / 2, after.Top + after.Height / 2).ToArgb() == ribbon.Theme.Accent.ToArgb(), name + " final pill is at the committed destination");
                        final.Save(Path.Combine(directory, name + "-final.png"));
                    }
                    for (int n = 0; n < frames.Count; n++) frames[n].Save(Path.Combine(directory, name + "-" + n + ".png"));
                }
                finally { foreach (var frame in frames) frame.Dispose(); }
            }
        }
        Rectangle Indicator(RibbonTab tab)
        {
            using var graphics = ribbon.CreateGraphics(); return renderer.GetTabIndicatorBounds(new(graphics, recording.TabBounds[tab.Id], ribbon.Theme, ribbon.Font, ribbon.DeviceDpi) { Density = ribbon.Density }, tab);
        }
        Bitmap ScreenFrame(Rectangle bounds)
        {
            var frame = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(frame); var source = GetDC(0); var destination = graphics.GetHdc();
            try { Check(BitBlt(destination, 0, 0, bounds.Width, bounds.Height, source, bounds.X, bounds.Y, 0x40CC0020), "Native screen frame capture"); }
            finally { graphics.ReleaseHdc(destination); ReleaseDC(0, source); }
            return frame;
        }
        async Task Menus()
        {
            int applied = 0; ribbon.CustomizationApplied += (_, _) => applied++;
            foreach (bool touch in new[] { true, false })
            {
                recording.MenuBounds.Clear(); ribbon.Refresh(); await Click(ribbon, recording.DensityButton);
                Check(Popup() is not null, "QAT density popup opens from native pointer input");
                await Transition(touch ? "menu-to-touch" : "menu-to-mouse", async () => await ClickMenu(touch ? "density.touch" : "density.mouse"));
                Check(ribbon.Density == (touch ? RibbonDensity.Touch : RibbonDensity.Compact), "Density choice commits");
            }
            foreach (var position in new[] { RibbonQuickAccessPosition.BelowRibbon, RibbonQuickAccessPosition.TabRow })
            {
                recording.MenuBounds.Clear(); ribbon.Refresh(); await Click(ribbon, recording.DensityButton, true);
                Check(Popup() is not null && recording.MenuBounds.ContainsKey("MoveQuickAccess"), "Native QAT context popup offers placement");
                await Transition(position == RibbonQuickAccessPosition.BelowRibbon ? "context-qat-below" : "context-qat-header", async () => await ClickMenu("MoveQuickAccess"));
                Check(ribbon.QuickAccessPosition == position, "Context placement commits");
            }
            Check(applied == 2 && ribbon.QuickAccessItems.Count == 3 && ReferenceEquals(copy.Parent, tools), "Placement retains customization notifications, QAT membership and source ownership");
            log.Add($"PASS: actual menu-triggered motion in both directions; DPI={ribbon.DeviceDpi}; renderer={(neutral ? "neutral" : "Office2024")}; Subtle timing");
            async Task Transition(string name, Func<Task> trigger)
            {
                var before = ribbon.RectangleToScreen(ribbon.ClientRectangle); recording.Paints.Clear(); recording.Indicators.Clear(); await trigger();
                Check(ribbon.IsAnimating, name + " remains active after native menu selection");
                var bounds = Rectangle.Union(before, ribbon.RectangleToScreen(ribbon.ClientRectangle)); List<Bitmap> frames = [];
                try
                {
                    foreach (int delay in new[] { 12, 35, 35 })
                    {
                        await Task.Delay(delay); var frame = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(frame); var source = GetDC(0); var destination = graphics.GetHdc();
                        try { Check(BitBlt(destination, 0, 0, bounds.Width, bounds.Height, source, bounds.X, bounds.Y, 0x40CC0020), "Visible menu-transition frame capture"); }
                        finally { graphics.ReleaseHdc(destination); ReleaseDC(0, source); }
                        frames.Add(frame);
                    }
                    await Finished(); Check(recording.Paints.Count >= 3, name + " paints multiple transition frames (" + recording.Paints.Count + ")");
                    if (!neutral) Check(recording.Indicators.Distinct().Count() > 2, name + " active underline follows geometry during playback");
                    for (int n = 0; n < frames.Count; n++) frames[n].Save(Path.Combine(directory, name + "-" + n + ".png"));
                }
                finally { foreach (var frame in frames) frame.Dispose(); }
            }
            async Task ClickMenu(string id)
            {
                var content = Popup()!.Items.OfType<WF.ToolStripControlHost>().Single().Control; await Click(content, recording.MenuBounds[id]);
            }
        }
        async Task Click(WF.Control control, Rectangle bounds, bool right = false)
        {
            Check(!bounds.IsEmpty, "Pointer target was painted"); WF.Cursor.Position = control.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2));
            mouse_event(right ? 8u : 2u, 0, 0, 0, 0); mouse_event(right ? 16u : 4u, 0, 0, 0, 0); await Task.Delay(20);
        }
        WF.ToolStripDropDown? Popup()
        {
            WF.ToolStripDropDown? result = null; EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (WF.Control.FromHandle(window) is WF.ToolStripDropDown { Visible: true } popup) result = popup; return true; }, 0); return result;
        }
        async Task Measure(string name, Action change)
        {
            recording.Paints.Clear(); long allocation = GC.GetAllocatedBytesForCurrentThread(); var watch = Stopwatch.StartNew(); change(); double preparation = watch.Elapsed.TotalMilliseconds;
            Check(ribbon.IsAnimating, name + " starts"); await Finished(); double total = watch.Elapsed.TotalMilliseconds;
            var intervals = recording.Paints.Zip(recording.Paints.Skip(1), (a, b) => Stopwatch.GetElapsedTime(a, b).TotalMilliseconds).Where(v => v > .5).Order().ToArray();
            double At(double percentile) => intervals.Length == 0 ? 0 : intervals[Math.Min(intervals.Length - 1, (int)Math.Ceiling(intervals.Length * percentile) - 1)];
            timing.Add(new { name, dpi = ribbon.DeviceDpi, renderer = neutral ? "neutral" : "Office2024", preparationMs = preparation, totalMs = total, paintIntervals = intervals, p50 = At(.5), p95 = At(.95), p99 = At(.99), max = intervals.LastOrDefault(), allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocation });
        }
    }
    private static IEnumerable<WF.Control> Descendants(WF.Control c) => c.Controls.Cast<WF.Control>().SelectMany(child => Descendants(child).Prepend(child));
    private sealed class Recording
    {
        internal RibbonControl? Ribbon; internal readonly List<long> Paints = []; internal Rectangle DensityButton; internal readonly Dictionary<string, Rectangle> MenuBounds = [];
        internal readonly Dictionary<string, Rectangle> TabBounds = []; internal readonly List<Rectangle> Indicators = [];
        internal readonly Dictionary<string, Rectangle> BodyItems = []; internal Rectangle MinimizeButton; internal readonly List<Rectangle> QatFrames = [], MessageFrames = [];
        internal void Paint() { if (Ribbon?.IsAnimating == true) Paints.Add(Stopwatch.GetTimestamp()); }
        internal void Item(RibbonPaintContext c, RibbonItem item, bool menu) { if (menu) MenuBounds[item.Id] = c.Bounds; else BodyItems[item.Id] = c.Bounds; }
        internal void Qat(RibbonPaintContext c, RibbonElement item, bool menu) { if (!menu && item is RibbonQuickAccessDropDownReference { Source.Id: "density.selector" }) DensityButton = c.Bounds; }
        internal void Indicator(RibbonPaintContext c) { if (Ribbon?.IsAnimating == true) Indicators.Add(c.Bounds); }
        internal void Toolbar(RibbonPaintContext c) { if (Ribbon?.IsAnimating == true) QatFrames.Add(c.Bounds); }
        internal void Message(RibbonPaintContext c) { if (Ribbon?.IsAnimating == true) MessageFrames.Add(c.Bounds); }
    }
    private sealed class OfficeRecorder(Recording r) : RibbonOffice2024Renderer
    {
        public override void DrawBody(RibbonPaintContext c) { r.Paint(); base.DrawBody(c); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Item(c, item, menu); base.DrawItem(c, item, size, menu); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu) { r.Qat(c, item, menu); base.DrawQuickAccessItem(c, item, menu); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.TabBounds[tab.Id] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawTabIndicator(RibbonPaintContext c, RibbonTab tab, bool selected) { if (selected) r.Indicator(c); base.DrawTabIndicator(c, tab, selected); }
        public override void DrawMinimizeButton(RibbonPaintContext c, bool minimized) { r.MinimizeButton = c.Bounds; base.DrawMinimizeButton(c, minimized); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition position) { r.Toolbar(c); base.DrawQuickAccessToolbar(c, position); }
        public override void DrawMessageBar(RibbonPaintContext c) { r.Message(c); base.DrawMessageBar(c); }
    }
    private sealed class NeutralRecorder(Recording r) : RibbonRenderer
    {
        public override void DrawBody(RibbonPaintContext c) { r.Paint(); base.DrawBody(c); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { r.Item(c, item, menu); base.DrawItem(c, item, size, menu); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu) { r.Qat(c, item, menu); base.DrawQuickAccessItem(c, item, menu); }
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected) { r.TabBounds[tab.Id] = c.Bounds; base.DrawTab(c, tab, selected); }
        public override void DrawMinimizeButton(RibbonPaintContext c, bool minimized) { r.MinimizeButton = c.Bounds; base.DrawMinimizeButton(c, minimized); }
        public override void DrawQuickAccessToolbar(RibbonPaintContext c, RibbonQuickAccessPosition position) { r.Toolbar(c); base.DrawQuickAccessToolbar(c, position); }
        public override void DrawMessageBar(RibbonPaintContext c) { r.Message(c); base.DrawMessageBar(c); }
    }
    private sealed class SampleIcon : IRibbonIcon { public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { using var pen = new Pen(c.Foreground, Math.Max(1, c.DeviceDpi / 96f)); b.Inflate(-b.Width / 5, -b.Height / 6); g.DrawRectangle(pen, b); g.DrawLine(pen, b.Left, b.Top + b.Height / 2, b.Right, b.Top + b.Height / 2); } }
}
