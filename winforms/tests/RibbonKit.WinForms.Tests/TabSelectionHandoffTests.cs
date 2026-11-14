using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class TabSelectionHandoffTests
{
    [Theory]
    [InlineData(RibbonTabContentAnimation.Fade, RibbonAnimationLevel.Subtle)]
    [InlineData(RibbonTabContentAnimation.SlideDown, RibbonAnimationLevel.Expressive)]
    public void Solid_tab_switches_keep_live_window_styles_stable_through_completion_and_interruption(RibbonTabContentAnimation style, RibbonAnimationLevel level) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 1050, 650), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var document = new Panel { Dock = DockStyle.Fill, BackColor = Color.DarkSlateBlue }; form.Controls.Add(document);
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2013Light, TabContentAnimation = style };
        var field = new RibbonComboBoxItem { Text = "Name", IsEditable = true, Value = "Retained value", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        foreach (string name in new[] { "Home", "Edit", "View" })
        {
            var tab = new RibbonTab { Text = name }; var group = new RibbonGroup { Text = name };
            if (name == "Home") group.Items.Add(field);
            group.Items.Add(new RibbonButtonItem { Text = name, Icon = RibbonStockIcons.Create("save") }); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        }
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        var editor = ribbon.Editors.EditorFor(field)!; nint editorHandle = editor.Handle, ribbonHandle = ribbon.Handle, formHandle = form.Handle;
        var parent = editor.Parent; long original = GetStyle(ribbon.Handle), hostStyle = GetStyle(form.Handle);
        using var rootProbe = new Probe(ribbon.Handle); using var hostProbe = new Probe(form.Handle);
        ribbon.Motion.SystemPolicyOverride = true; ribbon.AnimationLevel = level;
        bool committed = false; var coverage = new List<bool>();
        ribbon.SelectedTabChanged += (_, _) => committed = true;
        ribbon.Invalidated += (_, _) =>
        {
            if (!committed || !ribbon.Motion.Changing && !ribbon.Motion.IsRunning) return;
            nint cover = FindWindowEx(form.Handle, 0, null, "Ribbon motion");
            using var drawing = ribbon.Motion.CaptureFrame();
            coverage.Add(cover != 0 && IsWindowVisible(cover) && drawing is not null && drawing.GetPixel(drawing.Width / 2, drawing.Height / 2).A == 255);
        };
        int starts = 0, completions = 0, interruptions = 0;
        for (int i = 0; i < 18; i++)
        {
            var next = ribbon.Tabs[(i + 1) % ribbon.Tabs.Count]; if (ReferenceEquals(next, ribbon.SelectedTab)) continue;
            committed = false; ribbon.SelectedTab = next;
            Assert.True(ribbon.IsAnimating, ribbon.Motion.LastSkipReason); starts++;
            using (var cover = ribbon.Motion.CaptureFrame())
            {
                Assert.NotNull(cover); Assert.Equal(255, cover.GetPixel(cover.Width / 2, cover.Height / 2).A);
            }
            if (i % 3 == 0)
            {
                var watch = Stopwatch.StartNew(); while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1500) { Application.DoEvents(); Thread.Sleep(1); }
                Assert.False(ribbon.IsAnimating); completions++;
            }
            else
            {
                if (i % 2 == 0) ribbon.SelectedTab = ribbon.Tabs[(i + 2) % ribbon.Tabs.Count];
                else { var click = Message.Create(ribbon.Handle, 0x201, 1, 0); ribbon.ObserveMotionInput(ref click); }
                Assert.False(ribbon.IsAnimating); interruptions++;
            }
            Assert.Equal(original, GetStyle(ribbon.Handle)); Assert.Equal(hostStyle, GetStyle(form.Handle));
            Assert.Equal(editorHandle, editor.Handle); Assert.Same(parent, editor.Parent); Assert.Equal("Retained value", field.Value);
            Assert.Equal(ribbonHandle, ribbon.Handle); Assert.Equal(formHandle, form.Handle);
        }
        if (Environment.GetEnvironmentVariable("RIBBONKIT_TAB_HANDOFF_TRACE") is { Length: > 0 } output)
        {
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, $"{style}-{level}.json"), JsonSerializer.Serialize(new { starts, completions, interruptions, RibbonLayerAdds = rootProbe.LayerAdds, RibbonLayerRemovals = rootProbe.LayerRemovals, RibbonErases = rootProbe.Erases, HostLayerAdds = hostProbe.LayerAdds, HostLayerRemovals = hostProbe.LayerRemovals, HostErases = hostProbe.Erases, HostNonClientPaints = hostProbe.NonClientPaints }));
        }
        Assert.True(starts >= 12);
        Assert.NotEmpty(coverage); Assert.All(coverage, covered => Assert.True(covered, "Committed live repaint must finish under an opaque visible motion drawing."));
        Assert.Equal(0, rootProbe.LayerAdds); Assert.Equal(0, rootProbe.LayerRemovals);
        Assert.Equal(0, hostProbe.LayerAdds); Assert.Equal(0, hostProbe.LayerRemovals);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Selection_callbacks_that_change_geometry_or_throw_leave_no_hold_or_drawing(bool throws) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 1000, 650), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2013Light };
        ribbon.Tabs.AddRange(new RibbonTab { Text = "Home" }, new RibbonTab { Text = "View" }); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        long before = GetStyle(ribbon.Handle); using var probe = new Probe(ribbon.Handle);
        ribbon.Motion.SystemPolicyOverride = true; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
        ribbon.SelectedTabChanged += (_, _) => { if (throws) throw new InvalidOperationException("Application callback"); ribbon.Density = RibbonDensity.Touch; };
        if (throws) Assert.Throws<InvalidOperationException>(() => ribbon.SelectedTab = ribbon.Tabs[1]); else ribbon.SelectedTab = ribbon.Tabs[1];
        Assert.False(ribbon.IsAnimating); Assert.Null(ribbon.Motion.CaptureFrame()); Assert.Equal(before, GetStyle(ribbon.Handle));
        Assert.Equal(0, probe.LayerAdds); Assert.Equal(0, probe.LayerRemovals);
    });

    private sealed class Probe : NativeWindow, IDisposable
    {
        internal int LayerAdds, LayerRemovals, Erases, NonClientPaints;
        internal Probe(nint handle) => AssignHandle(handle);
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x7D && m.WParam == -20)
            {
                var styles = Marshal.PtrToStructure<Styles>(m.LParam);
                if ((styles.Old & 0x80000) == 0 && (styles.New & 0x80000) != 0) LayerAdds++;
                if ((styles.Old & 0x80000) != 0 && (styles.New & 0x80000) == 0) LayerRemovals++;
            }
            if (m.Msg == 0x14) Erases++; if (m.Msg == 0x85) NonClientPaints++;
            base.WndProc(ref m);
        }
        public void Dispose() => ReleaseHandle();
    }
    [StructLayout(LayoutKind.Sequential)] private struct Styles { internal uint Old, New; }
    private static long GetStyle(nint window) => GetWindowLongPtr(window, -20).ToInt64();
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint FindWindowEx(nint parent, nint after, string? className, string? title);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
}
