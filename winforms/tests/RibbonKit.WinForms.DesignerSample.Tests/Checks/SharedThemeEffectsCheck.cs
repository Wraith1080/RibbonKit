using System.Diagnostics;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class SharedThemeEffectsCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop;
            using var painter = new Probe();
            using var form = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1100, 650), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, FileMenuMode = RibbonFileMenuMode.CommandMenu };
            ribbon.FileMenu.Visible = true; ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Open" });
            var home = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Retained input" };
            var field = new RibbonTextBoxItem { Text = "Name", Value = "Shared effect input" }; group.Items.Add(field); home.Groups.Add(group); ribbon.Tabs.Add(home);
            for (int index = 0; index < 7; index++) { var tab = new RibbonTab { Text = "Document layout " + index }; tab.Groups.Add(new RibbonGroup { Text = "Commands" }); ribbon.Tabs.Add(tab); }
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
            var editor = Descendants(ribbon).OfType<TextBox>().Single(control => control.Text == field.Value); nint handle = editor.Handle; var parent = editor.Parent; editor.Select(2, 5);
            var original = ribbon.Theme; var header = new RibbonHeaderAppearanceSettings { FilledBorderColor = Color.Magenta, FilledBorderWidth = 2,
                ScrollerCornerRadius = 0, ScrollerBorderWidth = 2, ContextualUnselectedOpacity = .9,
                FileHighlightInset = 5, FileHighlightBackground = new(Color.Navy), FileHighlightForeground = Color.Yellow };
            ribbon.Theme = original with { HeaderPresentation = RibbonHeaderPresentation.Filled, HeaderFilledCornerRadius = 0, HeaderAppearance = header,
                HeaderScrollButtonWidth = 32, HeaderScrollButtonBackground = new(Color.LightBlue), HeaderScrollButtonBorder = Color.DarkBlue,
                Chevrons = new() { Length = 16, Depth = 9, StrokeWidth = 2 }, FocusInset = 4, FocusStrokeWidth = 2, TabContentSlideDistance = 24, HeaderIndicatorSpringAmount = .7 };
            painter.Check = true; Print(ribbon, Path.Combine(output, scope + "-wide.png"));
            Require(painter.SelectedPaints > 0 && painter.Failure is null, "Native Filled header check failed: " + painter.Failure);
            form.ClientSize = new(380, 650); Application.DoEvents(); Print(ribbon, Path.Combine(output, scope + "-narrow.png"));
            Require(painter.ScrollerPaints > 0 && painter.Failure is null, "Native scroller check failed: " + painter.Failure);
            form.ClientSize = new(1100, 650); Application.DoEvents();
            ribbon.FileMenu.ShowDropDown(); Require(ribbon.FileMenu.IsDropDownOpen, "CommandMenu did not open.");
            Print(ribbon, Path.Combine(output, scope + "-file.png")); Require(painter.FilePaints > 0 && painter.Failure is null, "Native File highlight check failed: " + painter.Failure); ribbon.FileMenu.CloseDropDown();
            painter.Check = false;
            Require(editor.Handle == handle && ReferenceEquals(editor.Parent, parent) && editor.Text == field.Value && editor.SelectionStart == 2 && editor.SelectionLength == 5,
                "Theme effects changed native input identity, value or selection.");
            ribbon.Theme = ribbon.Theme with { HeaderAppearance = header with { FilledBorderWidth = null } }; Require(ribbon.Theme.HeaderAppearance!.FileHighlightInset == 5, "Member reset cleared another effect.");
            ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown; ribbon.SelectedTab = ribbon.Tabs[1];
            Require(ribbon.IsAnimating, "The existing native motion path did not start."); var watch = Stopwatch.StartNew(); int samples = 0;
            while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1600) { Application.DoEvents(); Thread.Sleep(8); samples++; }
            Require(!ribbon.IsAnimating && samples > 1, "Native shared motion did not finish.");
            ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.SelectedTab = home; ribbon.Theme = original;
            Require(ribbon.Theme.HeaderAppearance is null && ribbon.Theme.Chevrons is null && editor.Handle == handle, "Reset did not retain native input and default groups.");
            evidence.Add($"PASS {scope}: effective={form.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; native selected/scroller/File callbacks, authored border/ink, narrow header, shared motion ({samples} timer samples), member/group reset and retained native editor.");
            form.Close();
        }
        evidence.Add("Offscreen package checks; timer samples are not compositor frames. Actual VS, visible frame pacing, physical input and mixed-monitor review remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Print(Control control, string path) { using var frame = new Bitmap(control.Width, control.Height); control.DrawToBitmap(frame, control.ClientRectangle); frame.Save(path); }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check;
        internal int SelectedPaints, ScrollerPaints, FilePaints;
        internal Exception? Failure;
        private Color _ink;
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) { _ink = color; base.DrawText(context, text, color, flags); }
        public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected)
        {
            if (Check && selected) Safely(() =>
            {
                using var frame = new Bitmap(context.Bounds.Width, context.Bounds.Height); using var graphics = Graphics.FromImage(frame);
                base.DrawTab(context with { Graphics = graphics, Bounds = new(Point.Empty, frame.Size), Focused = false }, tab, true);
                Require(frame.GetPixel(0, frame.Height / 2).ToArgb() == Color.Magenta.ToArgb(), "Selected native tab ignored Filled border."); SelectedPaints++;
            });
            base.DrawTab(context, tab, selected);
        }
        public override void DrawScrollButton(RibbonPaintContext context, bool backward)
        {
            if (Check) Safely(() =>
            {
                using var frame = new Bitmap(context.Bounds.Width, context.Bounds.Height); using var graphics = Graphics.FromImage(frame);
                base.DrawScrollButton(context with { Graphics = graphics, Bounds = new(Point.Empty, frame.Size) }, backward);
                Require(frame.GetPixel(0, frame.Height / 2).ToArgb() == Color.DarkBlue.ToArgb(), "Native scroller ignored its outline."); ScrollerPaints++;
            });
            base.DrawScrollButton(context, backward);
        }
        public override void DrawFileButton(RibbonPaintContext context, RibbonDropDownItem menu)
        {
            base.DrawFileButton(context, menu);
            if (Check && menu.IsDropDownOpen) Safely(() => { Require(_ink == Color.Yellow, "Native File highlight ignored authored ink."); FilePaints++; });
        }
        private void Safely(Action action) { try { action(); } catch (Exception error) { Failure ??= error; } }
    }
}
