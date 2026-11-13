using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class BackstagePagePreferenceCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = $"{family}-{backdrop}";
            using var form = new RibbonForm { Text = "Independent Backstage page preference", StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1000, 620), Backdrop = backdrop, ShowInTaskbar = false };
            using var ribbon = new RibbonControl { BuiltInTheme = family, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None };
            using var field = new TextBox { Text = "Retained page-width draft" };
            var page = new RibbonBackstagePageItem { Text = new string('W', 120), Content = field };
            ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.Backstage.Items.Add(page);
            ribbon.Backstage.MinimumNavigationWidth = 80; ribbon.Backstage.MaximumNavigationWidth = 1000; ribbon.Backstage.NavigationWidth = 1000;
            ribbon.Theme = ribbon.Theme with { BackstagePageBorderWidth = 0, BackstagePagePadding = 0, CaptionButtonAppearance = new() { GlyphSize = 18 } };
            form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true;
            var surface = form.Controls.Cast<Control>().Single(c => c.AccessibleName == "Backstage");
            Control area = field; while (area.Parent is { } parent && !ReferenceEquals(parent, surface)) area = parent;
            Require(ReferenceEquals(area.Parent, surface), "Fixture did not find the public native page ancestor.");
            var handle = field.Handle; var fieldParent = field.Parent; field.Select(2, 5); int validations = 0;
            field.Validating += (_, args) => { validations++; args.Cancel = true; };
            int S(int n) => (int)Math.Round(n * ribbon.DeviceDpi / 96d);
            int profiles = 0, layouts = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density;
                foreach (int? preference in new int?[] { null, 160, 360, 600, 1024, 0, null })
                foreach (int? width in new int?[] { 1000, null })
                {
                    ribbon.Backstage.NavigationWidth = width;
                    ribbon.Theme = ribbon.Theme with { BackstagePreferredMinimumPageWidth = preference };
                    int available = Math.Max(surface.Width / 2, surface.Width - S(preference ?? 240));
                    Require(area.Left == available && area.Width == surface.Width - available, "Public page preference did not reach live page geometry.");
                    Require(ribbon.Backstage.NavigationWidth == width && ribbon.Backstage.MinimumNavigationWidth == 80 && ribbon.Backstage.MaximumNavigationWidth == 1000, "Page preference rewrote authored navigation constraints.");
                    Require(ribbon.IsBackstageOpen && ReferenceEquals(ribbon.Backstage.SelectedPage, page) && field.Handle == handle && ReferenceEquals(field.Parent, fieldParent) && field.Text == "Retained page-width draft" && field.SelectionStart == 2 && field.SelectionLength == 5 && validations == 0, "Preference changed native state or triggered validation."); layouts++;
                }
                ribbon.Theme = ribbon.Theme with { BackstagePreferredMinimumPageWidth = 600 };
                using (var image = new Bitmap(surface.Width, surface.Height)) { surface.DrawToBitmap(image, surface.ClientRectangle); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-page-space.png")); }
                form.ClientSize = new(200, 140);
                Require(area.Left == surface.Width / 2 && area.Width == surface.Width - surface.Width / 2, "Narrow host did not keep the existing half-host rule.");
                form.ClientSize = new(1000, 600);
                Require(area.Left == Math.Max(surface.Width / 2, surface.Width - S(600)), "Resizing did not recompute page preference."); profiles++;
            }
            var theme = ribbon.Theme; var selected = ribbon.BuiltInTheme;
            foreach (int invalid in new[] { -1, 1025, int.MaxValue })
            {
                bool rejected = false; try { ribbon.Theme = theme with { BackstagePreferredMinimumPageWidth = invalid }; } catch (ArgumentOutOfRangeException) { rejected = true; }
                Require(rejected && ReferenceEquals(theme, ribbon.Theme) && ribbon.BuiltInTheme == selected, "Invalid preference mutated Theme/BuiltInTheme.");
            }
            ribbon.Theme = theme with { BackstagePreferredMinimumPageWidth = null };
            Require(ribbon.Theme.CaptionButtonAppearance?.GlyphSize == 18 && ribbon.Theme.BackstagePagePadding == 0 && validations == 0, "Preference Reset cleared other roles or validated native content.");
            field.CausesValidation = false; var effective = form.EffectiveBackdrop; int dpi = ribbon.DeviceDpi; form.Close();
            evidence.Add($"PASS {scope}: effective={effective}, dpi={dpi}; {profiles} Classic/Simplified x Compact/Touch profiles, {layouts} live Auto/fixed preference layouts; 0/default/160/360/600/1024, short/narrow/resize, stable HWND/parent/value/selection/page, no validation, authored constraints, atomic validation and independent Reset.");
        }
        using (var demo = new DesignerForm()) demo.VerifyBackstagePagePreferenceDemo(output);
        evidence.Add("PASS Theme effects 11: four retained caption-button groups plus Backstage page space; tab 10 retains five and earlier counts remain. Presets, independent Reset, isolated fixed/Auto previews, source settings and idempotence pass.");
        evidence.Add("Offscreen public-package native/direct-print evidence. Focus retention has separate focused STA coverage; physical input/IME, actual VS, visible pacing, desktop material pixels and mixed-monitor acceptance remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
