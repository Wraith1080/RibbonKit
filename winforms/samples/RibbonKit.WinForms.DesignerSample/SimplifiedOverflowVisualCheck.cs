using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class SimplifiedOverflowVisualCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var probe = new OverflowProbe();
        using var form = new DesignerForm { StartPosition = FormStartPosition.Manual, Location = new(180, 120) };
        var ribbon = form.Controls.OfType<RibbonControl>().Single(); ribbon.Renderer = probe;
        ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.LayoutMode = RibbonLayoutMode.Simplified; ribbon.SelectedTab = form.GalleryPresenterExample.Tab;
        Exception? failure = null;
        form.Shown += async (_, _) =>
        {
            try
            {
                form.Activate(); await Task.Delay(500); Require(GetForegroundWindow() == form.Handle, "Fixture foreground unavailable; capture stopped");
                await Capture("closed-before"); Require(!probe.Pressed, "Closed More Commands opener is not pressed");
                PostKey(ribbon, Keys.F10); await Task.Delay(50); PostKey(ribbon, Keys.G); await Task.Delay(50); PostKey(ribbon, Keys.D0); await Task.Delay(150);
                bool popupOpen = false;
                EnumThreadWindows(GetCurrentThreadId(), (window, _) =>
                {
                    if (GetAncestor(window, 3) == form.Handle && Control.FromHandle(window)?.AccessibleName?.StartsWith("More Commands", StringComparison.Ordinal) == true) popupOpen = true;
                    return true;
                }, 0);
                Require(popupOpen, "More Commands popup is open through native KeyTips");
                await Capture("open"); Require(probe.Pressed, "More Commands opener retains the pressed visual after key release");
                PostKey(ribbon, Keys.Escape); await Task.Delay(100);
                await Capture("closed-after"); Require(!probe.Pressed, "Escape clears the opener's pressed visual");
                log.WriteLine($"PASS: package-only public renderer hook and native KeyTips; Simplified More Commands closed/open/closed pressed state; actual DPI={form.DeviceDpi}; opener bounds={probe.Bounds}; native PNGs captured. Physical input and mixed-monitor review remain separate.");
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { form.Close(); }
        };
        Application.Run(form); if (failure is not null) throw failure;

        async Task Capture(string name)
        {
            ribbon.Refresh(); await Task.Delay(50);
            Require(GetForegroundWindow() == form.Handle || GetAncestor(GetForegroundWindow(), 3) == form.Handle, "Foreground lost; capture stopped");
            var area = form.RectangleToScreen(form.ClientRectangle);
            using var image = new Bitmap(area.Width, area.Height); using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
            image.Save(Path.Combine(output, name + ".png"));
        }
    }
    private sealed class OverflowProbe : RibbonOffice2024Renderer
    {
        internal bool Pressed;
        internal Rectangle Bounds;
        public override void DrawQuickAccessOverflow(RibbonPaintContext context)
        { Pressed = context.Pressed; Bounds = context.Bounds; base.DrawQuickAccessOverflow(context); }
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void PostKey(Control control, Keys key)
    { _ = PostMessage(control.Handle, 0x100, (nint)key, 0); _ = PostMessage(control.Handle, 0x101, (nint)key, 0); }
    private delegate bool WindowCallback(nint window, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, WindowCallback callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
