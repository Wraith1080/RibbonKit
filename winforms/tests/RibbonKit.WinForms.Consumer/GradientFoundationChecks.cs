using System.ComponentModel;
using RibbonKit.WinForms;

namespace IndependentConsumer;

// The default/legacy branch deliberately uses only APIs in the pre-F2 package.
internal static class GradientFoundationChecks
{
    internal static int Run(string directory)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); var log = new List<string>();
        try
        {
            foreach (var (name, basis) in new[] { ("2013", RibbonThemes.Office2013Light), ("2019", RibbonThemes.Office2019Light), ("2024", RibbonThemes.Office2024Light) })
            foreach (bool office in new[] { false, true })
            foreach (int dpi in new[] { 96, 144 })
            foreach (bool legacy in new[] { false, true })
            {
                using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
                var ramp = new RibbonFill(Color.LightYellow, Color.LightBlue);
                var theme = legacy ? basis with { BodyBackground = ramp, Hover = ramp, Checked = ramp, CheckedHover = ramp, HeaderSelectedBackground = ramp,
                    FileButtonAppearance = new() { Background = ramp, HoverBackground = ramp } } : basis;
                using var image = new Bitmap(640, 270); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
                for (int i = 0; i < 12; i++)
                {
                    var bounds = new Rectangle(i % 4 * 160 + 4, i / 4 * 90 + 4, 150, 80);
                    var c = new RibbonPaintContext(g, bounds, theme, SystemFonts.MessageBoxFont!, dpi, Hot: i is 4 or 8, Pressed: i == 5);
                    var saved = g.Save(); g.SetClip(bounds);
                    switch (i)
                    {
                        case 0: painter.DrawSurface(c, false); break;
                        case 1: painter.DrawBody(c); break;
                        case 2: painter.DrawPopupFrame(c); break;
                        case 3: painter.DrawTab(c, new RibbonTab { Text = "Tab" }, true); break;
                        case 4: case 5: painter.DrawItem(c, new RibbonButtonItem { Text = "Command" }, RibbonItemSize.Large, false); break;
                        case 6: painter.DrawItem(c, new RibbonToggleItem { Text = "Checked", Checked = true }, RibbonItemSize.Large, false); break;
                        case 7: painter.DrawGalleryTile(c, new RibbonGalleryTile { Text = "Tile" }, true); break;
                        case 8: painter.DrawFileButton(c, new RibbonDropDownItem { Text = "File" }); break;
                        case 9: var choice = new RibbonComboBoxItem(); choice.Items.Add("Choice"); painter.DrawComboBoxItem(c, choice, 0, true); break;
                        case 10: painter.DrawBackstageBackground(c, 50); break;
                        case 11: painter.DrawQuickAccessToolbar(c, RibbonQuickAccessPosition.BelowRibbon); break;
                    }
                    g.Restore(saved);
                }
                image.Save(Path.Combine(directory, $"{name}-{office}-{dpi}-{(legacy ? "legacy" : "default")}.png"));
                log.Add($"PASS: {name}/{office}/{dpi}/{legacy}: twelve public paint hooks.");
            }
#if F2
            Feature(directory); log.Add("PASS: immutable public gradients, state chrome, five surface roles, native editor/caption/application Reset and atomic invalid assignment.");
#endif
            log.Add("LIMIT: direct-print/offscreen native checks; no desktop material pixels, actual VS, physical input/IME, native corner or mixed-monitor acceptance.");
            return 0;
        }
        catch (Exception error) { log.Add("FAIL: " + error); return 1; }
        finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); }
    }
#if F2
    private static void Feature(string directory)
    {
        var input = new[] { new RibbonGradientStop(.5, Color.Lime) }; var stops = new RibbonGradientStops(input); input[0] = new(.2, Color.Black);
        var ramp = new RibbonFill(Color.Red, Color.Red) { IntermediateStops = stops };
        Require(stops[0].Color == Color.Lime && ramp == ramp with { IntermediateStops = new(new[] { new RibbonGradientStop(.5, Color.Lime) }) }, "Copied stops and structural equality");
        var chrome = new RibbonStateChromeSettings { HoverBorderColor = Color.Navy, CheckedBorderColor = Color.Purple, InnerBorderColor = Color.White, InnerBorderWidth = 1 };
        var authored = RibbonThemes.Office2013Light with { BackgroundFill = ramp, BodyBackground = ramp, PopupBackgroundFill = ramp, TitleBarBackgroundFill = ramp,
            QuickAccessBackgroundFill = ramp, BackstageNavigationBackgroundFill = ramp, Hover = ramp, CommandAppearance = new() { StateChrome = chrome } };
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(65, 65); using var g = Graphics.FromImage(image); var c = new RibbonPaintContext(g, new(0, 0, 65, 65), authored, SystemFonts.MessageBoxFont!, 96, Hot: true);
            painter.DrawSurface(c, false); Require(image.GetPixel(32, 32).ToArgb() == Color.Lime.ToArgb(), "Equal endpoints retain their middle color");
            painter.DrawPopupFrame(c); Require(image.GetPixel(32, 32).ToArgb() == Color.Lime.ToArgb(), "Popup fill retains its middle stop");
            painter.DrawQuickAccessToolbar(c with { Theme = authored with { BodyBackground = new(Color.White) } }, RibbonQuickAccessPosition.BelowRibbon); Require(image.GetPixel(32, 32).ToArgb() == Color.Lime.ToArgb(), "QAT fill is independent of the body");
            painter.DrawBackstageBackground(c, 65); Require(image.GetPixel(32, 32).ToArgb() == Color.Lime.ToArgb(), "Navigation rail retains its middle stop");
            painter.DrawItem(c, new RibbonButtonItem(), RibbonItemSize.Large, false); Require(image.GetPixel(1, 32).ToArgb() == Color.Navy.ToArgb(), "Actual command state hook consumes authored outer ink");
            image.Save(Path.Combine(directory, $"feature-{office}.png"));
        }
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 1100, 680), StartPosition = FormStartPosition.Manual, ShowInTaskbar = false };
        using var ribbon = new RibbonControl { Theme = authored, AnimationLevel = RibbonAnimationLevel.None };
        var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Editors" }; var field = new RibbonComboBoxItem { Id = "field", IsEditable = true, Value = "Retained native field" }; field.Items.Add("Retained native field");
        group.Items.Add(field); tab.Groups.Add(group); ribbon.Tabs.Add(tab); form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        var editor = Children(ribbon).OfType<ComboBox>().Single(c => c.Name == field.Id); var handle = editor.Handle; var parent = editor.Parent;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!.ResetValue(ribbon);
        Require(ReferenceEquals(authored, ribbon.Theme) && editor.Handle == handle && ReferenceEquals(editor.Parent, parent) && field.Value == "Retained native field", "Application Reset retains custom gradients and native editor ownership");
        form.TitleBarColor = Color.Purple; using var solid = Print(ribbon); solid.Save(Path.Combine(directory, "native-solid-caption.png"));
        form.TitleBarColor = Color.Empty; using var restored = Print(ribbon); restored.Save(Path.Combine(directory, "native-gradient-caption.png"));
        int captionBottom = 0, sampleX = solid.Width / 2;
        while (captionBottom < solid.Height && solid.GetPixel(sampleX, captionBottom).ToArgb() == Color.Purple.ToArgb()) captionBottom++;
        Require(captionBottom > 10 && captionBottom < solid.Height, "Native caption override has measured coverage");
        for (int y = captionBottom; y < solid.Height; y++) Require(solid.GetPixel(sampleX, y).ToArgb() == restored.GetPixel(sampleX, y).ToArgb(), "Caption override/Reset leaves the ribbon/header gradient intact");
        Require(solid.GetPixel(sampleX, captionBottom / 2).ToArgb() != restored.GetPixel(sampleX, captionBottom / 2).ToArgb(), "Caption Reset restores the authored gradient");
        var bad = ramp with { IntermediateStops = new(new[] { new RibbonGradientStop(.5, Color.Transparent) }) };
        try { ribbon.Theme = authored with { PopupBackgroundFill = bad }; throw new InvalidOperationException("Invalid opaque role accepted"); }
        catch (ArgumentException) { Require(ReferenceEquals(authored, ribbon.Theme), "Invalid surface assignment is atomic"); }
        Require((authored with { Background = Color.White }).BackgroundFill is null && (authored with { TitleBarBackground = Color.Black }).TitleBarBackgroundFill is null, "Existing Color setters own their roles");
    }
    private static Bitmap Print(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, new(Point.Empty, image.Size)); return image; }
    private static IEnumerable<Control> Children(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Children(c)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
#endif
}
