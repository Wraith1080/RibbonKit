using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Public API/package-only check of the authored font and size fields. Read the
// existing native surface before requesting a reference repaint of the same value.
internal static class ComboSelectionPaintCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        int observations = 0;
        foreach (var backdrop in Enum.GetValues<RibbonWindowBackdrop>())
        {
            using var form = new DesignerForm { Backdrop = backdrop, StartPosition = FormStartPosition.Manual, Location = new(120, 100) };
            var ribbon = form.Ribbon!; ribbon.AnimationLevel = RibbonAnimationLevel.None;
            var items = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonComboBoxItem>()
                .Where(i => i.Text is "Font :" or "Font Size :").ToArray();
            Check(items.Length == 2, "Authored font and size fields found");
            form.Show(); form.Activate(); Application.DoEvents();
            foreach (var density in Enum.GetValues<RibbonDensity>())
            {
                ribbon.Density = density; Application.DoEvents();
                foreach (var item in items)
                {
                    var native = Descendants(ribbon).OfType<ComboBox>().Single(c => c.Name == item.Id);
                    Check(native.Visible && native.DropDownStyle == ComboBoxStyle.DropDownList, "Visible native selection field");
                    nint handle = native.Handle;
                    item.SelectedIndex = 0; Application.DoEvents(); Observe("initial", 0);
                    item.ShowDropDown(); Key(Keys.End); Observe("preview", item.Items.Count - 1);
                    Key(Keys.Escape); Observe("cancel", 0);
                    item.ShowDropDown(); Key(Keys.End); Key(Keys.Enter); Observe("commit", item.Items.Count - 1);
                    item.SelectedIndex = 0; Application.DoEvents(); Observe("model", 0);
                    item.ShowDropDown(); Application.DoEvents();
                    var list = FindList(item.Id + ".choices");
                    var row = list.GetItemRectangle(1);
                    nint point = (nint)((row.Top + row.Height / 2) << 16 | (row.Left + 8));
                    SendMessage(list.Handle, 0x201, 1, point); SendMessage(list.Handle, 0x202, 0, point);
                    Application.DoEvents(); Check(!item.IsDropDownOpen, "Mouse choice accepted"); Observe("mouse", 1);

                    void Observe(string stage, int index)
                    {
                        Check(item.SelectedIndex == index && native.SelectedIndex == index && native.Text == item.Items[index], "Model/native value: " + stage);
                        Check(native.Handle == handle, "Native handle retained");
                        using var actual = ReadSurface(native);
                        // Only now force a repaint: the previously captured pixels must
                        // already match what the renderer produces for the current value.
                        native.Refresh(); Application.DoEvents();
                        using var expected = ReadSurface(native);
                        int differences = 0;
                        for (int y = 0; y < actual.Height; y++) for (int x = 0; x < actual.Width; x++)
                            if (actual.GetPixel(x, y).ToArgb() != expected.GetPixel(x, y).ToArgb()) differences++;
                        string name = $"{backdrop}-{density}-{(item == items[0] ? "font" : "size")}-{stage}";
                        actual.Save(Path.Combine(output, name + ".png"));
                        log.WriteLine($"{name}: value={item.Value}; DPI={ribbon.DeviceDpi}; stalePixels={differences}; handleRetained=True");
                        if (differences > 0) expected.Save(Path.Combine(output, name + "-expected.png"));
                        Check(differences == 0, "Native field was stale before reference repaint: " + name);
                        observations++;
                    }
                    void Key(Keys key)
                    {
                        var message = Message.Create(native.Handle, 0x100, (nint)(int)key, 0);
                        Check(Application.FilterMessage(ref message), "Choice key handled: " + key);
                        Application.DoEvents();
                    }
                }
            }
            form.Close();
        }
        log.WriteLine($"PASS: {observations} existing-pixel observations; authored font/size fields, Solid/Mica/Acrylic, Compact/Touch, preview/Escape/Enter/programmatic/native mouse selection. No Showcase resources or internal APIs.");
    }

    private static Bitmap ReadSurface(ComboBox native)
    {
        using var measure = native.CreateGraphics();
        var clip = Rectangle.Round(native.Region!.GetBounds(measure));
        var image = new Bitmap(clip.Width, clip.Height);
        nint source = GetDC(native.Handle);
        using var graphics = Graphics.FromImage(image);
        nint destination = graphics.GetHdc();
        try { GdiFlush(); Check(BitBlt(destination, 0, 0, clip.Width, clip.Height, source, clip.Left, clip.Top, 0x00cc0020), "Read existing native surface"); }
        finally { graphics.ReleaseHdc(destination); ReleaseDC(native.Handle, source); }
        return image;
    }
    private static ListBox FindList(string name)
    {
        ListBox? found = null;
        EnumThreadWindows(GetCurrentThreadId(), (handle, _) =>
        {
            if (Control.FromHandle(handle) is { } window)
                found ??= Descendants(window).OfType<ListBox>().SingleOrDefault(c => c.Name == name);
            return true;
        }, 0);
        return found ?? throw new InvalidOperationException("Choice list not found: " + name);
    }
    private static IEnumerable<Control> Descendants(Control owner) => owner.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private delegate bool EnumWindow(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern bool BitBlt(nint destination, int x, int y, int width, int height, nint source, int sourceX, int sourceY, uint operation);
    [DllImport("gdi32.dll")] private static extern bool GdiFlush();
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
}
