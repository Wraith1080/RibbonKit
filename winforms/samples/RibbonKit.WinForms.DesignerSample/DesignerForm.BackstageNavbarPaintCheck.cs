using System.Diagnostics;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    internal void VerifyBackstageNavbarPaint(string output, RibbonBuiltInTheme theme = RibbonBuiltInTheme.Office2019Light)
    {
        Require(theme is RibbonBuiltInTheme.Office2019Light or RibbonBuiltInTheme.Office2024Light, "This check covers the two implemented light-theme families.");
        Directory.CreateDirectory(output);
        if (theme == RibbonBuiltInTheme.Office2019Light) EnableOffice2019ThemeDemo();
        else ribbon.BuiltInTheme = theme;
        StartPosition = FormStartPosition.Manual; Location = new(30, 30); Backdrop = RibbonWindowBackdrop.None;
        ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
        Exception? failure = null;
        Shown += async (_, _) =>
        {
            try
            {
                Activate(); BringToFront(); SetForegroundWindow(Handle); await Task.Delay(100);
                ribbon.IsBackstageOpen = true;
                Require(ribbon.IsAnimating, "The fixture did not open with the sample's normal Backstage animation.");
                var opening = Stopwatch.StartNew();
                while (ribbon.IsAnimating && opening.ElapsedMilliseconds < 800) await Task.Delay(16);
                Require(!ribbon.IsAnimating, "Opening did not finish within its normal duration.");
                await Task.Delay(100);
                Activate(); BringToFront(); SetForegroundWindow(Handle); await Task.Delay(80);
                var info = (BackstageInfoControl)infoPage.Content!;
                var print = printPage.Content!;
                var area = info.RectangleToScreen(info.ClientRectangle);
                area.Intersect(RectangleToScreen(ClientRectangle));
                Require(area.Width > 100 && area.Height > 100, "The native page viewport was not realized.");
                var notesHandle = info.NotesEditor.Handle; var infoParent = info.Parent; var printParent = print.Parent;
                info.NotesEditor.Select(2, 3); string notes = info.NotesEditor.Text;
                ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown;
                var captureFrames = new List<NavbarPaintFrame>();
                for (int index = 0; index < 6; index++)
                {
                    string target = index % 2 == 0 ? "Print" : "Info";
                    var button = Descendants(this).OfType<Button>().Single(control => control.Text == target);
                    var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    int phase = index;
                    // The UI can block inside a synchronous native handoff. A
                    // separate capture thread must observe those compositor frames.
                    var capture = Task.Run(() => CaptureNavbarFrames(output, phase, target, area, ready));
                    await ready.Task;
                    button.Focus(); button.PerformClick();
                    var frames = await capture; captureFrames.AddRange(frames);
                    Require(ReferenceEquals(ribbon.Backstage.SelectedPage, target == "Print" ? printPage : infoPage), "The navbar click did not select its page.");
                    Require(!ribbon.IsAnimating, "The page transition did not settle within the capture window.");
                    Require(info.NotesEditor.Handle == notesHandle && ReferenceEquals(info.Parent, infoParent) && ReferenceEquals(print.Parent, printParent)
                        && info.NotesEditor.Text == notes && info.NotesEditor.SelectionStart == 2 && info.NotesEditor.SelectionLength == 3,
                        "A page switch changed authored native state.");
                }
                File.WriteAllText(Path.Combine(output, "frames.json"), JsonSerializer.Serialize(captureFrames, new JsonSerializerOptions { WriteIndented = true }));
                int blank = captureFrames.Count(frame => frame.Ink < 16);
                int blackCorner = captureFrames.Count(frame => frame.CornerDarkPixels > 0);
                var result = new { theme = ribbon.BuiltInTheme.ToString(), dpi = ribbon.DeviceDpi, switches = 6, frames = captureFrames.Count, blankFrames = blank, blackCornerFrames = blackCorner, nativeStateRetained = true };
                File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
                Require(blank == 0, $"Observed {blank} completely blank page frames during opaque navbar page changes.");
                Require(blackCorner == 0, $"Observed {blackCorner} frames with black pixels in the blank top-left page margin.");
                File.WriteAllText(Path.Combine(output, "result.log"), "PASS: six visible Info/Print navbar clicks through the public package; opaque SlideDown keeps page content visible, its top-left corner clean and native page parent/handle/text/selection retained.");
            }
            catch (Exception error) { failure = error; File.WriteAllText(Path.Combine(output, "result.log"), "FAIL: " + error); }
            finally { Close(); }
        };
        Application.Run(this);
        if (failure is not null) throw new InvalidOperationException("Backstage navbar paint check failed.", failure);
    }

    private sealed record NavbarPaintFrame(int Phase, string Target, int Index, long ElapsedMs, int Ink, int CornerDarkPixels);
    private static List<NavbarPaintFrame> CaptureNavbarFrames(string output, int phase, string target, Rectangle area, TaskCompletionSource ready)
    {
        var frames = new List<NavbarPaintFrame>(); var watch = Stopwatch.StartNew();
        try
        {
            do
            {
                GetWindowThreadProcessId(GetForegroundWindow(), out uint foreground);
                Require(foreground == Environment.ProcessId, "Page capture stopped because the fixture lost foreground ownership.");
                DwmFlush();
                using var image = new Bitmap(area.Width, area.Height, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(image)) graphics.CopyFromScreen(area.Location, Point.Empty, area.Size);
                var pixels = PagePixels(image);
                int index = frames.Count;
                frames.Add(new(phase, target, index, watch.ElapsedMilliseconds, pixels.Ink, pixels.CornerDarkPixels));
                if (index < 2 || pixels.Ink < 16 || pixels.CornerDarkPixels > 0) image.Save(Path.Combine(output, $"switch-{phase}-{target}-{index:D2}.png"));
                if (index == 0) ready.TrySetResult();
                Thread.Sleep(4);
            } while (watch.ElapsedMilliseconds < 400);
            return frames;
        }
        catch (Exception error) { ready.TrySetException(error); throw; }
    }
    private static (int Ink, int CornerDarkPixels) PagePixels(Bitmap image)
    {
        var data = image.LockBits(new(Point.Empty, image.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var pixels = new byte[data.Stride * data.Height]; Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
            int ink = 0;
            for (int y = 8; y < image.Height - 8; y += 2)
                for (int x = 8; x < image.Width - 8; x += 2)
                {
                    int offset = y * data.Stride + 4 * x;
                    if (Math.Min(pixels[offset], Math.Min(pixels[offset + 1], pixels[offset + 2])) < 180) ink++;
                }
            // The authored sample has a blank margin here. A native black strip
            // is therefore distinct from its headings and editable content.
            int cornerDark = 0;
            for (int y = 0; y < Math.Min(32, image.Height); y++)
                for (int x = 0; x < Math.Min(24, image.Width); x++)
                {
                    int offset = y * data.Stride + 4 * x;
                    if (Math.Max(pixels[offset], Math.Max(pixels[offset + 1], pixels[offset + 2])) < 64) cornerDark++;
                }
            return (ink, cornerDark);
        }
        finally { image.UnlockBits(data); }
    }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
}
