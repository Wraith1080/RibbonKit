using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class DropDownContentCheck
{
    internal static void Verify(string output, bool animate = false)
    {
        Directory.CreateDirectory(output); using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        using var form = new DesignerForm { Location = new(100, 80), StartPosition = FormStartPosition.Manual };
        var ribbon = form.Controls.OfType<RibbonControl>().Single(); ribbon.AnimationLevel = animate ? RibbonAnimationLevel.Subtle : RibbonAnimationLevel.None;
        var (drop, split) = form.PopupContentExample;
        var first = drop.Content as PopupOptionsControl; var second = split.Content as PopupOptionsControl;
        Require(first is not null && second is not null && !ReferenceEquals(first, second), "Both ContentType bodies created during InitializeComponent");
        ribbon.SelectedTab = ribbon.Tabs.First(); Exception? failure = null;
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Delay(120); form.Activate();
                foreach (var item in new RibbonDropDownItem[] { drop, split })
                {
                    var body = (PopupOptionsControl)item.Content!;
                    item.ShowDropDown(); await Task.Delay(100);
                    Require(item.IsDropDownOpen && body.Visible && body.Parent is not null, "Source opens retained body");
                    int padding = (int)Math.Round(4 * form.DeviceDpi / 96d);
                    Require(body.Parent!.ClientSize == body.Size + new Size(2 * padding, 2 * padding), "Popup fits native UserControl size plus padding");
                    log.WriteLine($"Size: dpi={form.DeviceDpi}; body={body.Size}; popup surface={body.Parent.ClientSize}");
                    body.ValueEditor.Text = "Retained document"; body.ValueEditor.Select(2, 4); var handle = body.ValueEditor.Handle;
                    await Capture(item == drop ? "dropdown" : "split-dropdown");
                    PostKey(body.ValueEditor, Keys.Escape); await Task.Delay(100);
                    Require(!item.IsDropDownOpen && body.Parent is null && !body.IsDisposed, "Escape detaches rather than disposes");
                    item.ShowDropDown(); await Task.Delay(100);
                    Require(body.ValueEditor.Handle == handle && body.ValueEditor.Text == "Retained document" && body.ValueEditor.SelectionStart == 2 && body.ValueEditor.SelectionLength == 4, "Reopening retains native handle/text/selection");
                    body.ValueEditor.Focus(); PostKey(body.ValueEditor, Keys.Tab); await Task.Delay(75);
                    Require(body.ModeEditor.Focused, "Queued Tab reaches native ComboBox");
                    body.ModeEditor.DroppedDown = true; await Task.Delay(75); PostKey(body.ModeEditor, Keys.Escape); await Task.Delay(100);
                    Require(!body.ModeEditor.DroppedDown && item.IsDropDownOpen, "First Escape dismisses the native ComboBox list only");
                    body.ValueEditor.Text = ""; body.ApplyButton.PerformClick(); Require(item.IsDropDownOpen, "Application validation can reject Apply");
                    body.ValueEditor.Text = "Accepted"; body.ApplyButton.PerformClick(); await Task.Delay(75); Require(!item.IsDropDownOpen, "Application Apply closes through public source API");
                    var reference = new RibbonQuickAccessDropDownReference { Source = item, Text = "Custom popup", KeyTip = "9", DropDownKeyTip = "8" }; ribbon.QuickAccessItems.Add(reference);
                    var sourceTab = ribbon.SelectedTab; ribbon.SelectedTab = ribbon.Tabs.First(tab => tab != sourceTab);
                    PostKey(ribbon, Keys.Escape); PostKey(ribbon, Keys.F10); await Task.Delay(70); PostKey(ribbon, item is RibbonSplitButtonItem ? Keys.D8 : Keys.D9); await Task.Delay(120);
                    Require(reference.IsDropDownOpen && body.Parent is not null && body.ValueEditor.Handle == handle, "Public QAT keyboard route reuses retained native body from inactive source tab");
                    await Capture(item == drop ? "qat-dropdown" : "qat-split");
                    item.CloseDropDown(); ribbon.QuickAccessItems.Remove(reference); reference.Dispose(); ribbon.SelectedTab = sourceTab;
                }
                int clicks = 0; split.Click += (_, _) => clicks++; split.PerformClick(); Require(clicks == 1 && !split.IsDropDownOpen, "Split primary command remains independent");
                log.WriteLine($"PASS: independent package-only consumer; animation={ribbon.AnimationLevel}; ordinary designer-editable UserControl; eager initialization; size fit; retained native handles/text/selection; queued Tab/Escape; native ComboBox first Escape; application validation/Apply; source and inactive-tab QAT dropdown/split routing. Actual VS and physical mixed-DPI review remain separate.");
            }
            catch (Exception error) { failure = error; log.WriteLine("FAIL: " + error); }
            finally { form.Close(); }
        };
        Application.Run(form); if (failure is not null) throw failure;

        async Task Capture(string name)
        {
            await Task.Delay(60);
            var bounds = Rectangle.Union(form.Bounds, ((PopupOptionsControl)(name.Contains("split") ? split.Content! : drop.Content!)).TopLevelControl!.Bounds);
            using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(output, name + ".png"));
        }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void PostKey(Control control, Keys key) { _ = PostMessage(control.Handle, 0x100, (nint)key, 0); _ = PostMessage(control.Handle, 0x101, (nint)key, 0); }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
}
