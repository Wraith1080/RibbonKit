using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    internal void EnableCommandSplitGeometryDemo()
    {
        var tab = new RibbonTab { Text = "Command splits" }; var settings = new RibbonGroup { Text = "Split action sizing" };
        var apply = new RibbonButtonItem { Text = "Roomier command splits", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var reset = new RibbonButtonItem { Text = "Reset command splits", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var simplified = new RibbonToggleItem { Text = "Simplified layout", Checked = ribbon.LayoutMode == RibbonLayoutMode.Simplified, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium),
            SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedPriority = int.MaxValue };
        apply.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { CommandSplitArrowWidth = 48, CommandSplitPrimaryMinimumWidth = 104, CommandLargeSplitPrimaryMinimumWidth = 144 };
            statusLabel.Text = "Split regions widened. Compare primary/arrow actions in Mouse/Touch and Classic/Simplified.";
        };
        reset.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { CommandSplitArrowWidth = null, CommandSplitPrimaryMinimumWidth = null, CommandLargeSplitPrimaryMinimumWidth = null };
            statusLabel.Text = "Command split sizing reset; earlier menu/editor geometry and current layout mode retained.";
        };
        simplified.Click += (_, _) => ribbon.LayoutMode = simplified.Checked ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
        ribbon.LayoutModeChanged += (_, _) => simplified.Checked = ribbon.LayoutMode == RibbonLayoutMode.Simplified;
        settings.Items.AddRange(apply, reset, simplified); tab.Groups.Add(settings);
        var commands = new RibbonGroup { Text = "Horizontal command splits", CanResize = false };
        foreach (var size in new[] { RibbonItemSize.Small, RibbonItemSize.Medium, RibbonItemSize.Large })
            commands.Items.Add(CreateCommandSplitExample(size + " paste", size, RibbonSplitButtonLayout.Horizontal));
        tab.Groups.Add(commands);
        var comparison = new RibbonGroup { Text = "Vertical / QAT comparison" };
        comparison.Items.Add(CreateCommandSplitExample("Vertical paste", RibbonItemSize.Large, RibbonSplitButtonLayout.Vertical));
        var qat = new RibbonToggleItem { Text = "Split on QAT", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        RibbonQuickAccessDropDownReference? reference = null;
        qat.Click += (_, _) =>
        {
            if (qat.Checked) { reference = new() { Source = (RibbonSplitButtonItem)commands.Items[1] }; ribbon.QuickAccessItems.Add(reference); }
            else if (reference is not null) { ribbon.QuickAccessItems.Remove(reference); reference.Dispose(); reference = null; }
            statusLabel.Text = "QAT keeps its own split geometry; Large vertical is the unchanged comparison in Classic.";
        };
        comparison.Items.Add(qat); tab.Groups.Add(comparison);
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
    }

    private RibbonSplitButtonItem CreateCommandSplitExample(string text, RibbonItemSize size, RibbonSplitButtonLayout layout)
    {
        var split = new RibbonSplitButtonItem { Text = text, Icon = RibbonStockIcons.Create("open"), Layout = layout, CheckOnClick = true,
            SizePolicy = RibbonSizePolicy.Fixed(size), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline,
            SimplifiedSize = size == RibbonItemSize.Small ? RibbonItemSize.Small : RibbonItemSize.Medium };
        split.Click += (_, _) => statusLabel.Text = split.Text + " primary: checked=" + split.Checked;
        split.Items.AddRange(new RibbonButtonItem { Text = "Text only" }, new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true });
        return split;
    }

    internal void VerifyCommandSplitDemoContract()
    {
        ribbon.Theme = ribbon.Theme with { EditorMinimumHeight = 54, IllustratedMenuIconSize = 48 };
        var original = ribbon.Theme; var mode = ribbon.LayoutMode; var file = ribbon.FileMenuMode; var qat = ribbon.QuickAccessItems.ToArray();
        EnableCommandSplitGeometryDemo(); var groups = ribbon.SelectedTab!.Groups; var sources = groups[1].Items.ToArray();
        groups[0].Items[0].PerformClick();
        if (ribbon.Theme.CommandSplitArrowWidth != 48 || ribbon.Theme.CommandSplitPrimaryMinimumWidth != 104 || ribbon.Theme.CommandLargeSplitPrimaryMinimumWidth != 144)
            throw new InvalidOperationException("Command split demo did not set all three roles.");
        var split = (RibbonSplitButtonItem)sources[1]; split.PerformClick();
        if (!split.Checked || split.IsDropDownOpen) throw new InvalidOperationException("Primary action did not update the original split state.");
        groups[2].Items[1].PerformClick();
        if (ribbon.QuickAccessItems.Count != qat.Length + 1 || !ReferenceEquals(((RibbonQuickAccessDropDownReference)ribbon.QuickAccessItems.Last()).Source, split))
            throw new InvalidOperationException("Command split QAT source changed.");
        groups[2].Items[1].PerformClick(); groups[0].Items[2].PerformClick(); groups[0].Items[2].PerformClick(); groups[0].Items[1].PerformClick();
        if (ribbon.Theme != original || ribbon.LayoutMode != mode || ribbon.FileMenuMode != file || !qat.SequenceEqual(ribbon.QuickAccessItems) ||
            !sources.SequenceEqual(groups[1].Items) || !split.Checked)
            throw new InvalidOperationException("Command split reset changed prior geometry, mode, File, state or QAT ownership.");
    }
}
