using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Public-package native fixtures. Position the owned window beneath the current
// stationary pointer to exercise passive hover without moving the user's cursor.
internal static class TipCaptionGeometryCheck
{
    internal static void Verify(string output, bool captionOnly = false)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (bool office in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (bool roomier in new[] { false, true })
        {
            var record = new Record(); using RibbonRenderer painter = office ? new Office(record) : new Neutral(record);
            using var form = new RibbonForm { ClientSize = new(1000, 650), StartPosition = FormStartPosition.Manual, TopMost = !captionOnly, Text = "Tip and caption geometry - public consumer" };
            var ribbon = new RibbonControl { Renderer = painter, Density = density, AnimationLevel = RibbonAnimationLevel.None, QuickAccessPosition = RibbonQuickAccessPosition.TitleBar, QuickAccessMaxWidth = 900 };
            form.Controls.Add(ribbon); form.Ribbon = ribbon;
            var tab = new RibbonTab { Text = "Home", KeyTip = "TG" }; var group = new RibbonGroup { Text = "Hover and keyboard", CanResize = false }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            var command = new RibbonButtonItem { Text = "Hover here", KeyTip = "AB", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Large),
                ScreenTipTitle = "Find matching text throughout the current document without changing the selection",
                ScreenTipDescription = "The passive window uses the selected maximum width, content padding, title gap and placement clearance. Its native owner and editor selection remain intact." };
            var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained package draft" }; group.Items.AddRange(command, field);
            for (int i = 0; i < 14; i++) ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command });
            var original = ribbon.Theme; ribbon.Theme = roomier ? TipCaptionGeometrySettings.Caption(TipCaptionGeometrySettings.KeyTips(TipCaptionGeometrySettings.ScreenTips(original))) : original;
            using (var measure = new Bitmap(ribbon.Width, ribbon.Height)) ribbon.DrawToBitmap(measure, new(Point.Empty, measure.Size));
            var stationary = Cursor.Position; var center = new Point(record.Command.Left + record.Command.Width / 2, record.Command.Top + record.Command.Height / 2);
            var screen = ribbon.PointToScreen(center); form.Location = captionOnly ? new(80, 80) : new(form.Left + stationary.X - screen.X, form.Top + stationary.Y - screen.Y);
            Exception? failure = null;
            form.Shown += async (_, _) =>
            {
                try
                {
                    if (captionOnly)
                    {
                        using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
                        image.Save(Path.Combine(output, $"caption-current-{office}-{density}-{roomier}.png"));
                        evidence.Add($"CAPTURE renderer={office} density={density} roomier={roomier} nativeDpi={ribbon.DeviceDpi}: automatic caption/QAT geometry, no hover/input.");
                        return;
                    }
                    form.Activate(); SetForegroundWindow(form.Handle); RequireOwner();
                    var editor = Descendants(ribbon).OfType<TextBox>().Single(t => t.Text == field.Value); editor.Focus(); editor.Select(2, 4);
                    nint editorHandle = editor.Handle, formHandle = form.Handle; int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
                    record.Quick.Clear(); using (var current = new Bitmap(ribbon.Width, ribbon.Height)) ribbon.DrawToBitmap(current, new(Point.Empty, current.Size));
                    File.WriteAllText(Path.Combine(output, "hover-fixture.txt"), $"pointer={stationary}; client={ribbon.PointToClient(stationary)}; command={record.Command}; ribbon={ribbon.Handle}; underPointer={WindowFromPoint(stationary)}; control={Control.FromChildHandle(WindowFromPoint(stationary))?.GetType().Name}");
                    var point = ribbon.PointToClient(stationary); PostMessage(ribbon.Handle, 0x200, 0, (nint)(point.Y << 16 | (ushort)point.X));
                    for (int i = 0; i < 20 && record.TipBounds.IsEmpty; i++) { await Task.Delay(75); RequireOwner(); if (Cursor.Position != stationary) throw new InvalidOperationException("Pointer moved; stopped passive hover fixture."); }
                    Require(!record.TipBounds.IsEmpty, "The real passive ScreenTip did not appear.");
                    Require(record.Title.Left == S(roomier ? 20 : 12), "Packaged ScreenTip padding");
                    Require(record.Description.Top - record.Title.Bottom == S(roomier ? 12 : 6), "Packaged title gap");
                    Require(record.TipBounds.Width <= S(roomier ? 420 : 320), "Packaged tip maximum width");
                    Require(editor.Focused && editor.Handle == editorHandle && editor.SelectionStart == 2 && editor.SelectionLength == 4, "Passive tip retained native editor selection/focus");
                    var tip = Application.OpenForms.Cast<Form>().Single(f => !ReferenceEquals(f, form) && ReferenceEquals(f.Owner, form) && f.FormBorderStyle == FormBorderStyle.None);
                    using (var image = new Bitmap(tip.Width, tip.Height)) { tip.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, $"tip-{office}-{density}-{roomier}.png")); }
                    using (var image = new Bitmap(ribbon.Width, ribbon.Height)) { ribbon.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, $"caption-current-{office}-{density}-{roomier}.png")); }
                    var firstQat = record.Quick.OrderBy(b => b.Left).First();
                    Require(firstQat.Left == (roomier ? S(22) + S(16) + S(18) : S(10) + S(16) + S(10)) + S(2), "Caption leading/icon gap drives actual QAT targets");
                    foreach (var b in record.Quick) Require(NativeHit(form, Center(b)) == 1, "QAT remains client hit space");
                    Require(NativeHit(form, new(form.ClientSize.Width - S(23), S(density == RibbonDensity.Touch ? 24 : 18))) == 20, "Close target unchanged");
                    ribbon.Theme = original; using (var image = new Bitmap(ribbon.Width, ribbon.Height)) { ribbon.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, $"caption-reset-{office}-{density}-{roomier}.png")); }
                    Require(editor.Handle == editorHandle && form.Handle == formHandle && field.Value == "Retained package draft", "Caption/reset retained native handles and value");
                    ribbon.Theme = roomier ? TipCaptionGeometrySettings.KeyTips(original) : original;
                    Key(Keys.F10); Key(Keys.T); Key(Keys.G);
                    using (var image = new Bitmap(ribbon.Width, ribbon.Height)) { ribbon.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(output, $"badges-{office}-{density}-{roomier}.png")); }
                    var text = TextRenderer.MeasureText("AB", ribbon.Font); Require(record.Badge.Width == text.Width + (roomier ? 2 * S(6) : 0), "Automatic keyboard badge horizontal padding");
                    Require(record.Badge.Height == text.Height + (roomier ? 2 * S(4) : 0), "Automatic keyboard badge vertical padding");
                    evidence.Add($"PASS renderer={office} density={density} roomier={roomier} nativeDpi={ribbon.DeviceDpi}: actual passive tip measurement, editor focus/selection, caption QAT/client/close targets, retained HWND/value/reset and native keyboard badges.");
                    void Key(Keys key) { var message = Message.Create(ribbon.Handle, 0x100, (nint)key, 0); Application.FilterMessage(ref message); }
                    void RequireOwner() { if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("Foreground lost; stopped passive fixture."); }
                }
                catch (Exception error) { failure = error; }
                finally { form.Close(); }
            };
            Application.Run(form);
            File.WriteAllLines(Path.Combine(output, "result.log"), evidence); if (failure is not null) throw failure;
        }
        if (!captionOnly)
        {
            using var demo = new DesignerForm(); demo.VerifyTipCaptionDemoContract();
            evidence.Add("PASS permanent demo independent ScreenTip/KeyTip/caption resets, earlier gallery/Backstage/command geometry and authored owners retained.");
        }
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static Point Center(Rectangle b) => new(b.Left + b.Width / 2, b.Top + b.Height / 2);
    private static int NativeHit(Form form, Point point) { var p = form.PointToScreen(point); return (int)SendMessage(form.Handle, 0x84, 0, (nint)((uint)(ushort)p.X | (uint)(ushort)p.Y << 16)); }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private sealed class Record { internal Rectangle Command, TipBounds, Title, Description, Badge; internal readonly List<Rectangle> Quick = []; }
    private sealed class Office(Record r) : RibbonOffice2024Renderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize s, bool menu) { if (i.KeyTip == "AB" && !menu) r.Command = c.Bounds; base.DrawItem(c, i, s, menu); }
        public override void DrawScreenTip(RibbonPaintContext c, string t, string d, Rectangle title, Rectangle description) { r.TipBounds = c.Bounds; r.Title = title; r.Description = description; base.DrawScreenTip(c, t, d, title, description); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement e, bool menu) { if (!menu) r.Quick.Add(c.Bounds); base.DrawQuickAccessItem(c, e, menu); }
        public override void DrawKeyTip(RibbonPaintContext c, string key) { if (key == "AB") r.Badge = c.Bounds; base.DrawKeyTip(c, key); }
    }
    private sealed class Neutral(Record r) : RibbonRenderer
    {
        public override void DrawItem(RibbonPaintContext c, RibbonItem i, RibbonItemSize s, bool menu) { if (i.KeyTip == "AB" && !menu) r.Command = c.Bounds; base.DrawItem(c, i, s, menu); }
        public override void DrawScreenTip(RibbonPaintContext c, string t, string d, Rectangle title, Rectangle description) { r.TipBounds = c.Bounds; r.Title = title; r.Description = description; base.DrawScreenTip(c, t, d, title, description); }
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement e, bool menu) { if (!menu) r.Quick.Add(c.Bounds); base.DrawQuickAccessItem(c, e, menu); }
        public override void DrawKeyTip(RibbonPaintContext c, string key) { if (key == "AB") r.Badge = c.Bounds; base.DrawKeyTip(c, key); }
    }
    [DllImport("user32.dll")] private static extern bool PostMessage(nint h, int m, nint w, nint l);
    [DllImport("user32.dll")] private static extern nint SendMessage(nint h, int m, nint w, nint l);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern nint WindowFromPoint(Point point);
}
