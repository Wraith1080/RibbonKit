using System.Diagnostics;
using System.Text.Json;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Package-only public API probe, also linked into the permanent review sample.
internal static class Office2019ThemeCheck
{
    private static Exception? _threadFailure;
    internal static void Verify(string output)
    {
        _threadFailure = null;
        Application.ThreadException += ThreadFailed;
        try { VerifyCases(output); }
        finally { Application.ThreadException -= ThreadFailed; _threadFailure = null; }
    }
    private static void ThreadFailed(object sender, ThreadExceptionEventArgs args) => _threadFailure ??= args.Exception;
    private static void VerifyCases(string output)
    {
        Directory.CreateDirectory(output); var results = new List<object>(); int failures = 0;
        foreach (bool ribbonForm in new[] { false, true })
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        foreach (var backdrop in ribbonForm ? Enum.GetValues<RibbonWindowBackdrop>() : new[] { RibbonWindowBackdrop.None })
        {
            string scope = $"{(ribbonForm ? "RibbonForm" : "Form")}-{density}-{mode}-{backdrop}";
            if (Environment.GetEnvironmentVariable("RIBBONKIT_THEME_SCOPE") is { Length: > 0 } selectedScope && selectedScope != scope) continue;
            try { Check(output, scope, ribbonForm, density, mode, backdrop); results.Add(new { scope, passed = true }); }
            catch (Exception error)
            {
                failures++; results.Add(new { scope, passed = false, error = error.ToString() });
                File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
                File.WriteAllText(Path.Combine(output, "result.log"), $"FAIL: stopped at {scope}; {results.Count - failures} earlier scopes passed. See results.json.");
                throw;
            }
            File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
        }
        File.WriteAllText(Path.Combine(output, "result.log"), $"{(failures == 0 ? "PASS" : "FAIL")}: {results.Count - failures}/{results.Count} public-package native scopes. Ordinary/RibbonForm, both densities/layouts, requested/effective backdrops, light theme/color/Reset switching, retained editor/page handles, popup and Backstage frames. See results.json and captures.");
        if (failures > 0) throw new InvalidOperationException($"{failures} Office 2019 package scopes failed; see results.json.");
    }

    private static void Check(string output, string scope, bool integrated, RibbonDensity density, RibbonLayoutMode mode, RibbonWindowBackdrop backdrop)
    {
        using Form form = integrated ? new RibbonForm() : new Form();
        form.Text = "Office 2019 Light package check"; form.ClientSize = new(1100, 570);
        form.StartPosition = FormStartPosition.Manual; form.Location = new(20, 20);
        using var renderer = new ProbeRenderer(); using var ribbon = new RibbonControl { Renderer = renderer, Density = density, AnimationLevel = RibbonAnimationLevel.None };
        var application = RibbonThemes.Office2024Light with { Accent = Color.Purple, BodyContentHorizontalPadding = 9 }; ribbon.Theme = application;
        var text = new RibbonTextBoxItem { Text = "Name", Value = "Retained theme field", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var combo = new RibbonComboBoxItem { Text = "Font", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) }; combo.Items.AddRange("Aptos", "Segoe UI"); combo.SelectedIndex = 1;
        var menu = new RibbonDropDownItem { Text = "Popup", DropDownHeader = "Square popup", Icon = RibbonStockIcons.Create("open"), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var submenu = new RibbonDropDownItem { Text = "More" }; submenu.Items.Add(new RibbonButtonItem { Text = "Nested action" });
        menu.Items.AddRange(new RibbonCheckBoxItem { Text = "Checked option", Checked = true }, new RibbonTextBoxItem { Text = "Popup name", Value = "Borrowed value" }, submenu);
        var group = new RibbonGroup { Text = "Editing", CanResize = false }; group.Items.AddRange(text, combo, menu, new RibbonSplitButtonItem { Text = "Split action" });
        var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.Tabs.Add(new RibbonTab { Text = "View" });
        var gallery = new RibbonGalleryItem { Text = "Styles", IsInRibbon = true };
        gallery.Items.Add(new RibbonGalleryTile { Text = "Normal" }); gallery.Items.Add(new RibbonGalleryTile { Text = "Heading 1" });
        var styles = new RibbonGroup { Text = "Styles" }; styles.Items.Add(gallery); tab.Groups.Add(styles);
        ribbon.FileMenu.Visible = true; ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
        var reference = new RibbonQuickAccessDropDownReference { Source = menu }; ribbon.QuickAccessItems.Add(reference);
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        using var page = new TextBox { Text = "Retained native Backstage page", Multiline = true, Dock = DockStyle.Fill, BackColor = Color.LemonChiffon };
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        form.Controls.Add(ribbon);
        if (form is RibbonForm window) { window.Ribbon = ribbon; window.Backdrop = backdrop; }
        form.Show(); form.Activate(); Pump(120);
        var nativeText = Descendants(ribbon).OfType<TextBox>().Single(control => control.Text == text.Value);
        nativeText.Select(2, 5); var editorHandle = nativeText.Handle; var pageHandle = page.Handle;
        ribbon.LayoutMode = mode; Pump(40);
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; Pump(80);
        Require(ReferenceEquals(ribbon.Theme, RibbonThemes.Office2019Light), "Built-in selection failed.");
        Require(renderer.Body.Left == 0 && renderer.Body.Right == ribbon.ClientSize.Width, "Body is not full client width.");
        Require(renderer.GetBodyCornerRadius(ribbon.Theme, ribbon.DeviceDpi) == 0 && renderer.GetPopupCornerRadius(ribbon.Theme, ribbon.DeviceDpi) == 0, "Rounded theme frame remained.");
        Require(ribbon.Theme.PopupShadowEnabled && ribbon.Theme.PopupShadowColor.A > 0, "Popup shadow was lost.");
        Capture(form, Path.Combine(output, scope + "-neutral.png"));
        if (form is RibbonForm colored)
        {
            colored.TitleBarColor = ribbon.Theme.Accent; Pump(80);
            Require(renderer.HeaderBackground.ToArgb() == (backdrop == RibbonWindowBackdrop.None || colored.EffectiveBackdrop == RibbonWindowBackdrop.None ? colored.TitleBarColor.ToArgb() : Color.FromArgb((int)Math.Round(255 * colored.TitleBarTintOpacity), colored.TitleBarColor).ToArgb()), "Caption color did not reach the tab band.");
            Require(HeaderContrast(renderer.InactiveHeaderText, renderer.HeaderBackground, ribbon.Theme.Background) >= 4.5, "Inactive header text lost contrast.");
            Capture(form, Path.Combine(output, scope + "-colored.png"));
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; Pump(40);
            Capture(form, Path.Combine(output, scope + "-tabrow.png"));
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar; Pump(40);
            Require(ReferenceEquals(reference.Source, menu), "QAT source changed.");
        }
        menu.ShowDropDown(); Pump(100); Require(menu.IsDropDownOpen, "Popup did not open.");
        Capture(form, Path.Combine(output, scope + "-popup.png"));
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; Pump(40); Require(!menu.IsDropDownOpen, "Theme switch retained stale popup.");
        Require(nativeText.Handle == editorHandle && nativeText.Text == text.Value && text.Value == "Retained theme field" && combo.SelectedIndex == 1, "Editor state/handle changed.");
        ribbon.BuiltInTheme = null; Pump(40); Require(ReferenceEquals(application, ribbon.Theme), "Reset lost application theme.");
        ribbon.Theme = RibbonThemes.Office2019Light with { Accent = Color.Purple }; Pump(40);
        Require(ribbon.BuiltInTheme is null && ribbon.Theme.HeaderPresentation == RibbonHeaderPresentation.Filled, "Custom clone lost presentation.");
        if (form is RibbonForm reset) { reset.TitleBarColor = Color.Empty; Pump(40); Require(renderer.HeaderBackground.A == 0 || renderer.HeaderBackground == ribbon.Theme.Background, "Neutral reset left a stale colored band."); }
        ribbon.IsBackstageOpen = true; Pump(120); Require(ribbon.IsBackstageOpen && page.Visible, "Backstage failed to open.");
        Capture(form, Path.Combine(output, scope + "-backstage.png"));
        ribbon.IsBackstageOpen = false; Pump(80);
        Require(page.Handle == pageHandle && page.Text == "Retained native Backstage page" && page.BackColor == Color.LemonChiffon, "Application page changed.");
        File.WriteAllText(Path.Combine(output, scope + ".json"), JsonSerializer.Serialize(new { dpi = ribbon.DeviceDpi, backdropRequested = backdrop.ToString(), backdropEffective = (form as RibbonForm)?.EffectiveBackdrop.ToString(), body = renderer.Body, editorHandle = editorHandle.ToInt64(), pageHandle = pageHandle.ToInt64() }));
        form.Close();
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; }
    }
    private static void Pump(int milliseconds)
    {
        var watch = Stopwatch.StartNew();
        do
        {
            Application.DoEvents();
            if (_threadFailure is { } error) throw new InvalidOperationException("Native fixture UI exception; stopped without an error dialog.", error);
            Thread.Sleep(5);
        } while (watch.ElapsedMilliseconds < milliseconds);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static double HeaderContrast(Color text, Color tint, Color background)
    {
        double alpha = tint.A / 255d;
        static double Luminance(double red, double green, double blue)
        {
            static double Linear(double channel) { channel /= 255; return channel <= .04045 ? channel / 12.92 : Math.Pow((channel + .055) / 1.055, 2.4); }
            return .2126 * Linear(red) + .7152 * Linear(green) + .0722 * Linear(blue);
        }
        double a = Luminance(text.R, text.G, text.B), b = Luminance(tint.R * alpha + background.R * (1 - alpha), tint.G * alpha + background.G * (1 - alpha), tint.B * alpha + background.B * (1 - alpha));
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }
    private static void Capture(Form form, string path)
    {
        for (int attempt = 0; attempt < 3 && !FixtureHasForeground(); attempt++)
        { form.Activate(); form.BringToFront(); SetForegroundWindow(form.Handle); Pump(100); }
        form.Refresh(); Pump(50); var bounds = Rectangle.Intersect(form.Bounds, Screen.FromControl(form).Bounds);
        if (!FixtureHasForeground()) throw new InvalidOperationException("Capture stopped because the fixture lost foreground ownership.");
        using var image = new Bitmap(bounds.Width, bounds.Height); using (var g = Graphics.FromImage(image)) g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        image.Save(path);
    }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    private static bool FixtureHasForeground() { GetWindowThreadProcessId(GetForegroundWindow(), out uint process); return process == Environment.ProcessId; }
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    private sealed class ProbeRenderer : RibbonOffice2024Renderer
    {
        internal Rectangle Body; internal Color HeaderBackground, InactiveHeaderText;
        public override void DrawBody(RibbonPaintContext context) { Body = context.Bounds; base.DrawBody(context); }
        public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected)
        {
            HeaderBackground = context.Theme.Background;
            if (!selected) InactiveHeaderText = context.Theme.HeaderForeground.IsEmpty ? context.Theme.Foreground : context.Theme.HeaderForeground;
            base.DrawTab(context, tab, selected);
        }
    }
}
