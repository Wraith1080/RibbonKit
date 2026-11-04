using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    // Opt-in public-API review. Authored Forms, controls and startup File mode stay intact.
    internal void EnableIllustratedGeometryDemo()
    {
        var tab = new RibbonTab { Text = "Illustrated menus" };
        var sizing = new RibbonGroup { Text = "Illustrated menu geometry" };
        var apply = new RibbonButtonItem { Text = "Roomier illustrated menu", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var reset = new RibbonButtonItem { Text = "Reset illustrated menu", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var menu = CreateIllustratedGeometryMenu("Try illustrated menu");
        apply.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { IllustratedMenuMinimumWidth = 540, IllustratedMenuMinimumRowHeight = 88, IllustratedMenuIconSize = 48,
                IllustratedMenuLeadingPadding = 18, IllustratedMenuTrailingPadding = 22, IllustratedMenuIconTextGap = 20, IllustratedMenuDescriptionGap = 8 };
            statusLabel.Text = "Illustrated geometry applied. Compare the direct, nested and QAT menu in Mouse/Touch.";
        };
        reset.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { IllustratedMenuMinimumWidth = null, IllustratedMenuMinimumRowHeight = null, IllustratedMenuIconSize = null,
                IllustratedMenuLeadingPadding = null, IllustratedMenuTrailingPadding = null, IllustratedMenuIconTextGap = null, IllustratedMenuDescriptionGap = null };
            statusLabel.Text = "Illustrated geometry reset; previous menu/editor settings retained.";
        };
        sizing.Items.AddRange(apply, reset, menu); tab.Groups.Add(sizing);
        var examples = new RibbonGroup { Text = "Reference examples" };
        var nested = new RibbonDropDownItem { Text = "Nested choices", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        nested.Items.Add(CreateIllustratedGeometryMenu("Open illustrated choices"));
        var qat = new RibbonToggleItem { Text = "Menu on QAT", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        RibbonQuickAccessDropDownReference? reference = null;
        qat.Click += (_, _) =>
        {
            if (qat.Checked) { reference = new() { Source = menu }; ribbon.QuickAccessItems.Add(reference); }
            else if (reference is not null) { ribbon.QuickAccessItems.Remove(reference); reference.Dispose(); reference = null; }
            statusLabel.Text = qat.Checked ? "The same illustrated menu source is available from QAT or QAT overflow." : "Temporary QAT menu removed; authored entries retained.";
        };
        examples.Items.AddRange(nested, qat); tab.Groups.Add(examples);
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
    }

    private RibbonDropDownItem CreateIllustratedGeometryMenu(string text)
    {
        var menu = new RibbonDropDownItem { Text = text, DropDownHeader = "Formatting choices", MenuPresentation = RibbonMenuPresentation.IllustratedChoices,
            SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var source = new RibbonToggleItem { Text = "Keep source formatting", Checked = true, MenuDescription = "Keep the original fonts and paragraph spacing.", Icon = RibbonStockIcons.Create("open") };
        var destination = new RibbonToggleItem { Text = "Match destination formatting", MenuDescription = "Use the destination document's fonts and spacing.", Icon = RibbonStockIcons.Create("open") };
        var plain = new RibbonButtonItem { Text = "Text only", MenuDescription = "Keep the text and discard document formatting.", Icon = RibbonStockIcons.Create("open") };
        source.Click += (_, _) => statusLabel.Text = "Keep source formatting = " + source.Checked;
        destination.Click += (_, _) => statusLabel.Text = "Match destination formatting = " + destination.Checked;
        plain.Click += (_, _) => statusLabel.Text = "Text only choice invoked.";
        menu.Items.AddRange(source, destination, new RibbonMenuSeparatorItem(), plain);
        return menu;
    }

    internal void VerifyIllustratedDemoContract()
    {
        ribbon.Theme = ribbon.Theme with { EditorMinimumHeight = 54, EditorVerticalPadding = 10, MenuLeadingPadding = 14 };
        var original = ribbon.Theme; var fileMode = ribbon.FileMenuMode; var qat = ribbon.QuickAccessItems.ToArray();
        EnableIllustratedGeometryDemo(); var groups = ribbon.SelectedTab!.Groups;
        var menu = (RibbonDropDownItem)groups[0].Items[2]; var sources = menu.Items.ToArray();
        groups[0].Items[0].PerformClick();
        if (ribbon.Theme.IllustratedMenuMinimumWidth != 540 || ribbon.Theme.IllustratedMenuMinimumRowHeight != 88 || ribbon.Theme.IllustratedMenuIconSize != 48 ||
            ribbon.Theme.IllustratedMenuLeadingPadding != 18 || ribbon.Theme.IllustratedMenuTrailingPadding != 22 || ribbon.Theme.IllustratedMenuIconTextGap != 20 || ribbon.Theme.IllustratedMenuDescriptionGap != 8)
            throw new InvalidOperationException("Illustrated demo did not apply all seven roles.");
        groups[1].Items[1].PerformClick();
        var reference = ribbon.QuickAccessItems.Last() as RibbonQuickAccessDropDownReference;
        if (ribbon.QuickAccessItems.Count != qat.Length + 1 || !ReferenceEquals(reference?.Source, menu)) throw new InvalidOperationException("QAT example changed source identity.");
        var choice = (RibbonToggleItem)sources[0]; choice.PerformClick();
        if (choice.Checked || menu.IsDropDownOpen) throw new InvalidOperationException("Illustrated choice state did not update without a popup.");
        groups[0].Items[1].PerformClick(); groups[1].Items[1].PerformClick();
        if (ribbon.Theme != original || ribbon.FileMenuMode != fileMode || !qat.SequenceEqual(ribbon.QuickAccessItems) || !sources.SequenceEqual(menu.Items) || choice.Checked)
            throw new InvalidOperationException("Illustrated reset changed prior geometry, values, File or QAT ownership.");
    }
}
