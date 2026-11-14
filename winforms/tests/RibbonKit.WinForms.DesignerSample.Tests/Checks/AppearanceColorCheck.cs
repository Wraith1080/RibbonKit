using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// An independent package consumer of the public appearance APIs. The authored
// DesignerForm and its UserControls remain untouched. Read composed desktop
// pixels with the requested material actually active, rather than a solid fallback.
internal static class AppearanceColorCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
        foreach (bool office in new[] { true, false })
        foreach (var backdrop in Enum.GetValues<RibbonWindowBackdrop>())
        {
            string name = $"{(office ? "Office" : "Neutral")}-{backdrop}";
            using var form = new RibbonForm { Text = "Appearance colors - " + name, StartPosition = FormStartPosition.Manual,
                Bounds = new(160, 100, 1000, 550), Backdrop = backdrop, TitleBarColor = Color.Navy, TitleBarTintOpacity = .75f };
            using RibbonRenderer painter = office ? new OfficeRecorder() : new NeutralRecorder();
            using var ribbon = new RibbonControl { Renderer = painter, QuickAccessPosition = RibbonQuickAccessPosition.TitleBar,
                AnimationLevel = RibbonAnimationLevel.None, Theme = new RibbonTheme
                {
                    BodyBackground = new(Color.AliceBlue, Color.LightSteelBlue), BodyBorder = Color.SteelBlue,
                    PopupBackground = Color.Honeydew, EditorBackground = Color.LemonChiffon, EditorForeground = Color.Maroon,
                    EditorBorder = Color.OrangeRed, MenuHeadingBackground = new(Color.MidnightBlue), MenuHeadingForeground = Color.White
                } };
            var choice = new RibbonComboBoxItem { Text = "Choice", Id = "appearance-choice" };
            choice.Items.Add("First choice"); choice.Items.Add("Second choice"); choice.Items.Add("Third choice"); choice.SelectedIndex = 1;
            var menu = new RibbonDropDownItem { Text = "Tools", DropDownHeader = "Menu heading" };
            menu.Items.Add(new RibbonButtonItem { Text = "First command" }); menu.Items.Add(new RibbonButtonItem { Text = "Second command" });
            var command = new RibbonButtonItem { Text = "Save", Icon = new CaptionIcon() };
            var group = new RibbonGroup { Text = "Colors" }; group.Items.Add(command); group.Items.Add(choice); group.Items.Add(menu);
            var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command }); form.Controls.Add(ribbon); form.Ribbon = ribbon;
            Exception? failure = null;
            form.Shown += async (_, _) =>
            {
                try
                {
                    form.Activate(); await Task.Delay(200);
                    log.WriteLine($"{name}: Requested={backdrop}; Effective={form.EffectiveBackdrop}; DPI={ribbon.DeviceDpi}");
                    Require(form.EffectiveBackdrop == backdrop, name + " requested material is active");
                    var native = Descendants(ribbon).OfType<ComboBox>().Single(c => c.Name == choice.Id);
                    nint nativeHandle = native.Handle, formHandle = form.Handle;
                    Require(native.BackColor == Color.LemonChiffon && native.ForeColor == Color.Maroon, name + " native editor roles");
                    Require(ribbon.EffectiveQuickAccessPosition == RibbonQuickAccessPosition.TitleBar, name + " TitleBar QAT");
                    using var colored = Capture(form.RectangleToScreen(new(450, 2, 100, 5)));
                    using (var frame = Capture(form.Bounds)) frame.Save(Path.Combine(output, name + "-colored.png"));
                    Require(((ICaptionRecorder)painter).Foreground == Color.White, name + " QAT contrast");
                    form.TitleBarColor = Color.Empty; await Task.Delay(100);
                    using var reset = Capture(form.RectangleToScreen(new(450, 2, 100, 5)));
                    Require(Distance(colored.GetPixel(30, 2), Color.Navy) < Distance(reset.GetPixel(30, 2), Color.Navy), name + " caption tint changed composed pixels");
                    form.TitleBarColor = Color.Navy; await Task.Delay(100);
                    foreach (var density in Enum.GetValues<RibbonDensity>())
                    {
                        ribbon.Density = density; await Task.Delay(100);
                        Require(native.Handle == nativeHandle && form.Handle == formHandle && native.SelectedIndex == 1, name + " retained editor and window");
                        choice.ShowDropDown(); await Task.Delay(100);
                        var list = Windows().SelectMany(Descendants).OfType<ListBox>().Single(c => c.Name == choice.Id + ".choices");
                        Require(list.BackColor == Color.Honeydew && list.ForeColor == Color.Maroon, name + " choice popup roles");
                        using (var shot = Capture(list.RectangleToScreen(list.ClientRectangle)))
                        {
                            Require(Count(shot, Color.Honeydew) > 100, name + " native list background");
                            shot.Save(Path.Combine(output, name + "-" + density + "-choices.png"));
                        }
                        choice.CloseDropDown(); await Task.Delay(50);
                    }
                    menu.ShowDropDown(); await Task.Delay(100);
                    var popup = Windows().OfType<ToolStripDropDown>().Single(c => c.Visible);
                    using (var shot = Capture(popup.Bounds))
                    {
                        Require(Count(shot, Color.Honeydew) > 100, name + " command popup background");
                        Require(Count(shot, Color.MidnightBlue) > 100, name + " independent menu heading");
                        shot.Save(Path.Combine(output, name + "-menu.png"));
                    }
                    menu.CloseDropDown();
                    log.WriteLine(name + ": PASS caption tint/reset, QAT contrast, retained editor, both densities, choice and command popup roles");
                }
                catch (Exception error) { failure = error; }
                finally { form.Close(); }
            };
            Application.Run(form);
            if (failure is not null) throw new InvalidOperationException(name, failure);
        }
    }
    private interface ICaptionRecorder { Color Foreground { get; } }
    private sealed class OfficeRecorder : RibbonOffice2024Renderer, ICaptionRecorder
    {
        public Color Foreground { get; private set; }
        public override void DrawQuickAccessToolbar(RibbonPaintContext context, RibbonQuickAccessPosition position)
        { Foreground = context.Theme.Foreground; base.DrawQuickAccessToolbar(context, position); }
    }
    private sealed class NeutralRecorder : RibbonRenderer, ICaptionRecorder
    {
        public Color Foreground { get; private set; }
        public override void DrawQuickAccessToolbar(RibbonPaintContext context, RibbonQuickAccessPosition position)
        { Foreground = context.Theme.Foreground; base.DrawQuickAccessToolbar(context, position); }
    }
    private sealed class CaptionIcon : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        { using var pen = new Pen(context.Foreground, 2); graphics.DrawRectangle(pen, Rectangle.Inflate(bounds, -3, -3)); }
    }
    private static Bitmap Capture(Rectangle bounds)
    {
        var image = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(image);
        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); return image;
    }
    private static int Distance(Color a, Color b) => Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
    private static int Count(Bitmap image, Color color)
    {
        int count = 0; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
            if (image.GetPixel(x, y).ToArgb() == color.ToArgb()) count++;
        return count;
    }
    private static IEnumerable<Control> Descendants(Control owner) => owner.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static IEnumerable<Control> Windows()
    {
        List<Control> windows = [];
        EnumThreadWindows(GetCurrentThreadId(), (handle, _) => { if (Control.FromHandle(handle) is { } control) windows.Add(control); return true; }, 0);
        return windows;
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private delegate bool EnumWindow(nint handle, nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, EnumWindow callback, nint parameter);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
}
