using System.Diagnostics;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class WindowMotionRegressionTests
{
    [Theory]
    [InlineData(RibbonDensity.Compact, RibbonLayoutMode.Classic)]
    [InlineData(RibbonDensity.Touch, RibbonLayoutMode.Classic)]
    [InlineData(RibbonDensity.Compact, RibbonLayoutMode.Simplified)]
    [InlineData(RibbonDensity.Touch, RibbonLayoutMode.Simplified)]
    public void RibbonForm_width_layout_preserves_group_playback_and_native_editors(RibbonDensity density, RibbonLayoutMode mode) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 1200, Height = 700 }; var ribbon = new RibbonControl { Density = density, LayoutMode = mode, QuickAccessPosition = RibbonQuickAccessPosition.TitleBar };
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Fields" };
        var field = new RibbonComboBoxItem { Text = "Font", IsEditable = true, Value = "Retained", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        group.Items.AddRange(field, new RibbonButtonItem { Text = "Copy", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }); tab.Groups.Add(group);
        var other = new RibbonGroup { Text = "Other", CanResize = false }; other.Items.Add(new RibbonButtonItem { Text = "Another command" }); tab.Groups.Add(other); ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        nint handle = ribbon.Editors.EditorFor(field)!.Handle; ribbon.Motion.SystemPolicyOverride = true; ribbon.GroupMotion.ActiveOwnerOverride = true; ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
        form.Width = 250; Assert.True(ribbon.GroupMotion.IsRunning, ribbon.GroupMotion.LastSkipReason); int starts = ribbon.GroupMotion.StartedCount;
        form.Width -= 2; Assert.True(ribbon.GroupMotion.IsRunning); Assert.Equal(starts, ribbon.GroupMotion.StartedCount); Finish(ribbon);
        form.Width = 1200; Assert.True(ribbon.GroupMotion.IsRunning); Finish(ribbon);
        Assert.Equal(handle, ribbon.Editors.EditorFor(field)!.Handle); Assert.Equal("Retained", field.Value);
    });

    [Fact]
    public void Native_drawing_plane_is_clipped_to_RibbonForm_with_exact_screen_coordinates() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 1000, Height = 600, Location = new(50, 60), StartPosition = FormStartPosition.Manual };
        var ribbon = new RibbonControl(); ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        using var scene = new RibbonMotionScene(ribbon); using var window = new RibbonMotionWindow(form, clipToOwner: true); window.Render(ribbon, scene, scene, 0);
        Assert.Equal(form.Handle, GetParent(window.Handle)); Assert.True((GetStyle(window.Handle) & 0x40000000) != 0); Assert.False((GetStyle(window.Handle) & 0x80000000L) != 0);
        Assert.True(GetWindowRect(window.Handle, out var rectangle)); Assert.Equal(scene.Bounds, Rectangle.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom));
    });

    [Theory] [InlineData(true)] [InlineData(false)]
    public void Every_native_presentation_uses_the_same_committed_screen_rectangle(bool child) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 1000, Height = 600, Location = new(240, 180), StartPosition = FormStartPosition.Manual };
        var ribbon = new RibbonControl(); ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        using var scene = new RibbonMotionScene(ribbon); using var window = new RibbonMotionWindow(form, clipToOwner: child); using var probe = new PositionProbe(window.Handle);
        for (int i = 0; i < 3; i++) window.Render(ribbon, scene, scene, i / 4d);
        Assert.NotEmpty(probe.Positions);
        Assert.All(probe.Positions, position => Assert.Equal(scene.Bounds, position));
    });
    private sealed class PositionProbe : NativeWindow, IDisposable
    {
        internal readonly List<Rectangle> Positions = [];
        internal PositionProbe(nint window) => AssignHandle(window);
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x47 && GetWindowRect(Handle, out var bounds) && bounds.Right > bounds.Left && bounds.Bottom > bounds.Top)
                Positions.Add(Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom));
        }
        public void Dispose() => ReleaseHandle();
    }

    [Fact]
    public void Backstage_TextBox_fades_keep_one_opaque_surface_until_hidden_and_release_it_on_close() => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 1000, Height = 650 }; var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage };
        using var info = new Panel(); using var name = new TextBox { Text = "Document", Location = new(28, 143) }; using var notes = new TextBox { Multiline = true, Text = "Native notes", Location = new(28, 218), Size = new(440, 115) };
        info.Controls.AddRange([name, notes]); using var print = new Panel(); print.Controls.Add(new ComboBox());
        var a = new RibbonBackstagePageItem { Text = "Info", Content = info }; var b = new RibbonBackstagePageItem { Text = "Print", Content = print };
        ribbon.Backstage.Items.AddRange(a, b); ribbon.FileMenu.Visible = true; form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        ribbon.Motion.SystemPolicyOverride = true; ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.IsBackstageOpen = true;
        var surface = ribbon.BackstageSurface!; using var probe = new LayerProbe(surface.Handle); nint nameHandle = name.Handle, notesHandle = notes.Handle;
        Finish(ribbon); AssertOpaque();
        foreach (var page in new[] { b, a, b, a }) { ribbon.Backstage.SelectedPage = page; Assert.True(ribbon.IsAnimating); Finish(ribbon); AssertOpaque(); }
        Assert.Equal(0, probe.VisibleLayerRemovals); Assert.Equal(nameHandle, name.Handle); Assert.Equal(notesHandle, notes.Handle); Assert.Equal("Document", name.Text); Assert.Equal("Native notes", notes.Text);
        ribbon.IsBackstageOpen = false; Finish(ribbon); Assert.False(RibbonDrawingHold.HasOpacity(surface.Handle)); Assert.False(surface.Visible);
        Assert.Equal(0, probe.VisibleLayerRemovals);
        void AssertOpaque() { Assert.True(GetLayeredWindowAttributes(surface.Handle, out _, out byte alpha, out _)); Assert.Equal(255, alpha); Assert.False(RibbonDrawingHold.HasOpacity(info.Handle)); Assert.False(RibbonDrawingHold.HasOpacity(name.Handle)); }
    });

    [Fact]
    public void Application_owned_Backstage_opacity_is_never_claimed_or_cleared() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900 }; var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage }; ribbon.FileMenu.Visible = true;
        using var page = new TextBox { Text = "Application" }; ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page }); form.Controls.Add(ribbon); form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true;
        var surface = ribbon.BackstageSurface!; using var appOpacity = RibbonDrawingHold.Create(surface.Handle, 128);
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.Motion.SystemPolicyOverride = true;
        Assert.Null(RibbonBackstageMotion.PrepareFade(ribbon, surface, form, false)); ribbon.IsBackstageOpen = false;
        Assert.True(GetLayeredWindowAttributes(surface.Handle, out _, out byte alpha, out _)); Assert.Equal(128, alpha);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Caption_offset_keeps_tab_header_ink_in_the_measured_band_during_motion(bool minimize) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Width = 900, Height = 550 }; using var renderer = new HeaderInk(); var ribbon = new RibbonControl { Renderer = renderer };
        ribbon.Tabs.AddRange(new RibbonTab { Text = "Home" }, new RibbonTab { Text = "Insert" }); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        using var from = new RibbonMotionScene(ribbon); if (minimize) ribbon.IsMinimized = true; else ribbon.SelectedTab = ribbon.Tabs[1]; using var to = new RibbonMotionScene(ribbon);
        using var frame = new Bitmap(ribbon.Width, from.Image.Height); using var graphics = Graphics.FromImage(frame); using var alpha = new System.Drawing.Imaging.ImageAttributes();
        RibbonMotionScene.Render(graphics, from.Bounds, ribbon, from, to, .4, alpha);
        foreach (var target in ribbon.Snapshot.Targets.Where(t => t.Kind == TargetKind.Tab))
            Assert.Equal(Color.Blue.ToArgb(), frame.GetPixel(target.Bounds.Left + target.Bounds.Width / 2, target.Bounds.Top + target.Bounds.Height / 2).ToArgb());
    });
    private sealed class HeaderInk : RibbonRenderer
    { public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected) => context.Graphics.FillRectangle(Brushes.Blue, context.Bounds); }

    private static void Finish(RibbonControl ribbon) { var watch = Stopwatch.StartNew(); while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1500) { Application.DoEvents(); Thread.Sleep(1); } Assert.False(ribbon.IsAnimating); }
    private sealed class LayerProbe : NativeWindow, IDisposable
    {
        internal int VisibleLayerRemovals;
        internal LayerProbe(nint window) => AssignHandle(window);
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x7D && m.WParam == -20)
            {
                var style = Marshal.PtrToStructure<StyleChange>(m.LParam);
                if ((style.Old & 0x80000) != 0 && (style.New & 0x80000) == 0 && IsWindowVisible(Handle)) VisibleLayerRemovals++;
            }
            base.WndProc(ref m);
        }
        public void Dispose() => ReleaseHandle();
    }
    [StructLayout(LayoutKind.Sequential)] private struct StyleChange { internal uint Old, New; }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; }
    private static long GetStyle(nint window) => (IntPtr.Size == 8 ? GetWindowLongPtr(window, -16) : GetWindowLong(window, -16)).ToInt64();
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern nint GetWindowLong(nint window, int index);
    [DllImport("user32.dll")] private static extern nint GetParent(nint window);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out NativeRect rectangle);
    [DllImport("user32.dll")] private static extern bool GetLayeredWindowAttributes(nint window, out uint color, out byte alpha, out uint flags);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
}
