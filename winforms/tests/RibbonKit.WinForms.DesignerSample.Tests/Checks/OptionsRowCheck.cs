using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class OptionsRowCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            var icon = new ProbeIcon(); using var renderer = new ProbeRenderer(); using var bitmap = new Bitmap(20, 20); using (var ink = Graphics.FromImage(bitmap)) ink.Clear(Color.DarkOrange);
            using var owner = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1200, 700), ShowInTaskbar = false, Backdrop = backdrop }; using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = renderer, AnimationLevel = RibbonAnimationLevel.None }; owner.Controls.Add(ribbon); owner.Ribbon = ribbon;
            var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "commands", Text = "Commands", Icon = icon }; var vector = new RibbonButtonItem { Id = "vector", Text = "Vector", Icon = icon, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; var raster = new RibbonButtonItem { Id = "raster", Text = "Raster", Image = bitmap, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline }; group.Items.AddRange(vector, raster); tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = vector }); ribbon.QuickAccessItems.Add(new RibbonQuickAccessGroupReference { Source = group }); ribbon.CommandCatalog.CaptureDefaults(); owner.Show(); Application.DoEvents(); var original = ribbon.Theme; string scope = family + "-" + backdrop; int states = 0, rows = 0;
            var appearance = new RibbonOptionsAppearanceSettings { RowTextPadding = 12, RowIconSize = 16, RowIconTextGap = 10, RowCornerRadius = 9, RowSelectedBackground = new(Color.LightBlue, Color.LightGreen), RowHeadingBold = false, FrameBorderWidth = 3, ButtonBorderWidth = 2, NavigationCornerRadius = 12 };
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.Theme = original with { OptionsAppearance = appearance }; using var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "RibbonKit.Rows.Check") { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; dialog.Show(owner); Application.DoEvents(); var draft = dialog.Editor.Session.Draft; var sources = ribbon.QuickAccessItems.ToArray();
                foreach (var page in dialog.Pages)
                {
                    dialog.SelectedPage = page; Application.DoEvents(); foreach (var list in Descendants(page).OfType<ListBox>())
                    {
                        list.SelectedIndex = Math.Min(1, list.Items.Count - 1); int selected = list.SelectedIndex, height = list.ItemHeight; var handle = list.Handle; var parent = list.Parent; var bounds = list.Bounds; var selectedRow = list.GetItemRectangle(selected); icon.Bounds.Clear(); renderer.Texts = 0;
                        using (var image = Print(list)) { Require(icon.Bounds.Count > 0 && icon.Bounds.All(b => b.Width == S(16, list.DeviceDpi) && b.Height <= height), "Actual native command icons bypassed row sizing."); Require(renderer.Texts == 0, "Native list text was rerouted through a renderer hook."); Require(image.GetPixel(1, selectedRow.Top + height / 2).ToArgb() != ribbon.Theme.Surface.ToArgb(), "Actual selected row lost authored fill."); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-{list.Name}.png")); }
                        ribbon.Theme = ribbon.Theme with { OptionsAppearance = appearance with { RowIconSize = 0, RowSelectedBackground = new(Color.Transparent) } }; icon.Bounds.Clear(); using (var hidden = Print(list)) { Require(icon.Bounds.Count == 0, "Hidden native icons were still drawn."); Require(hidden.GetPixel(1, selectedRow.Top + height / 2).ToArgb() == ribbon.Theme.Surface.ToArgb(), "Transparent row fill did not reveal Surface."); }
                        ribbon.Theme = ribbon.Theme with { OptionsAppearance = appearance }; Require(list.Handle == handle && ReferenceEquals(parent, list.Parent) && bounds == list.Bounds && list.ItemHeight == height && list.SelectedIndex == selected, "Appearance changed native row ownership, height or selection."); rows++;
                    }
                    using var pixels = Print(page); pixels.Save(Path.Combine(output, $"{scope}-{mode}-{density}-{page.Name}.png"));
                }
                dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "QuickAccessOptionsPage"); Application.DoEvents(); var search = Descendants(dialog.Editor).OfType<TextBox>().Single(c => c.Name == "CommandSearch"); search.Text = "Raster"; var add = Descendants(dialog.Editor).OfType<Button>().Single(c => c.Name == "AddCommand"); Require(add.Enabled, "Staged-add fixture did not find Raster."); add.PerformClick(); Require(dialog.Editor.Session.Draft.QuickAccess.Count == sources.Length + 1 && ReferenceEquals(draft, dialog.Editor.Session.Draft) && sources.SequenceEqual(ribbon.QuickAccessItems), "Row presentation applied or replaced the pending draft."); Descendants(dialog).OfType<Button>().Single(b => b.Name == "CancelCustomization").PerformClick(); Require(sources.SequenceEqual(ribbon.QuickAccessItems), "Cancel applied pending row edits."); states++;
            }
            ribbon.Theme = original with { OptionsAppearance = appearance with { RowTextPadding = null, RowIconSize = null, RowIconTextGap = null, RowCornerRadius = null, RowSelectedBackground = null, RowHeadingBold = null } }; Require(ribbon.Theme.OptionsAppearance is { FrameBorderWidth: 3, ButtonBorderWidth: 2, NavigationCornerRadius: 12 }, "Row Reset cleared accepted roles."); ribbon.Theme = ribbon.Theme with { OptionsAppearance = null }; Require(ribbon.Theme.OptionsAppearance is null && bitmap.Width == 20, "Group Reset or borrowed image lifetime failed.");
            evidence.Add($"PASS {scope}: effective owner={owner.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} mode/density states, {rows} native list paints; vector/group/raster ownership, authored icon sizes and zero hide, selection fill/Transparent, direct native text, retained HWND/parent/bounds/row height/selection/draft/sources, staged Add/Cancel and independent/group Reset."); owner.Close();
        }
evidence.Add("Synchronous offscreen public-package paint/state evidence at actual 120 DPI. Physical input/IME, actual VS, external UIA, mixed-monitor and visible pacing remain separate. Choose heading presets before opening; existing construction/font/DPI layout owns their fonts. Tree decoration and icon-picker tiles are separate."); File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static int S(int n, int dpi) => (int)Math.Round(n * dpi / 96d);
    private static Bitmap Print(Control c) { var image = new Bitmap(c.Width, c.Height); c.DrawToBitmap(image, new(Point.Empty, image.Size)); return image; }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class ProbeIcon : IRibbonIcon { internal readonly List<Rectangle> Bounds = []; public void Draw(Graphics g, Rectangle b, RibbonIconContext c) { Bounds.Add(b); using var ink = new SolidBrush(c.Foreground); g.FillRectangle(ink, b); } }
    private sealed class ProbeRenderer : RibbonOffice2024Renderer { internal int Texts; public override void DrawText(RibbonPaintContext c, string text, Color color, TextFormatFlags flags) { Texts++; base.DrawText(c, text, color, flags); } }
}
