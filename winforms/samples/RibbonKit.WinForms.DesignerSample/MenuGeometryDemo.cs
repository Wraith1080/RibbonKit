using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    // Opt-in public-API example; never changes the authored startup presentation.
    internal void EnableMenuGeometryDemo()
    {
        var tab = new RibbonTab { Text = "Menu geometry" };
        var group = new RibbonGroup { Text = "Ordinary menu spacing" }; tab.Groups.Add(group);
        var wider = new RibbonButtonItem { Text = "Wider menu spacing" };
        var reset = new RibbonButtonItem { Text = "Reset menu spacing" };
        var menu = new RibbonDropDownItem { Text = "Try this menu", Icon = RibbonStockIcons.Create("open") };
        menu.Items.AddRange(new RibbonButtonItem { Text = "Open document", Icon = RibbonStockIcons.Create("open"), MenuDescription = "Horizontal spacing and row minima are shared with QAT overflow." },
            new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true }, new RibbonTextBoxItem { Text = "Name", Value = "Retained native editor", FillMenuWidth = true });
        var labelledCombo = new RibbonComboBoxItem { Text = "Font family", InputWidth = 120, FillMenuWidth = true };
        labelledCombo.Items.AddRange("Aptos", "Segoe UI", "Arial"); labelledCombo.SelectedIndex = 0;
        var unlabelledCombo = new RibbonComboBoxItem { Text = "Unlabelled font chooser", ShowLabel = false, InputWidth = 120, FillMenuWidth = true };
        unlabelledCombo.Items.AddRange("Aptos", "Segoe UI", "Arial"); unlabelledCombo.SelectedIndex = 0;
        menu.Items.AddRange(labelledCombo, unlabelledCombo);
        wider.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { MenuMinimumRowHeight = 64, MenuIconSize = 36, MenuLeadingPadding = 14, MenuTrailingPadding = 18, MenuIconTextGap = 16 };
            statusLabel.Text = "Menu overrides applied. Open Try this menu.";
        };
        reset.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { MenuMinimumRowHeight = null, MenuIconSize = null, MenuLeadingPadding = null, MenuTrailingPadding = null, MenuIconTextGap = null };
            statusLabel.Text = "Menu spacing reset to the current density defaults.";
        };
        var fillFields = new RibbonToggleItem { Text = "Fill menu fields", Checked = true, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        fillFields.Click += (_, _) =>
        {
            foreach (var item in tab.Groups.SelectMany(g => g.Items.OfType<RibbonDropDownItem>()).SelectMany(m => m.Items))
            {
                if (item is RibbonTextBoxItem text) text.FillMenuWidth = fillFields.Checked;
                else if (item is RibbonComboBoxItem combo) combo.FillMenuWidth = fillFields.Checked;
            }
            statusLabel.Text = fillFields.Checked ? "Menu fields fill the remaining row. Combine with Roomier menu containers to compare widths."
                : "Menu fields use fixed InputWidth sizing; label alignment and Touch scaling are retained.";
        };
        group.Items.AddRange(wider, reset, menu, fillFields);
        var vertical = new RibbonGroup { Text = "Vertical menu spacing" }; tab.Groups.Add(vertical);
        var roomier = new RibbonButtonItem { Text = "More vertical spacing" };
        var resetVertical = new RibbonButtonItem { Text = "Reset vertical spacing" };
        var described = new RibbonDropDownItem { Text = "Try described menu", Icon = RibbonStockIcons.Create("open") };
        described.Items.AddRange(new RibbonButtonItem { Text = "Open document", Icon = RibbonStockIcons.Create("open"), MenuDescription = "Keep the original formatting and document layout." },
            new RibbonMenuSeparatorItem(), new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true, MenuDescription = "Preserve fonts and paragraph spacing." },
            new RibbonTextBoxItem { Text = "Name", Value = "Retained native editor", FillMenuWidth = true });
        roomier.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { MenuIconVerticalPadding = 12, MenuDescriptionVerticalPadding = 14, MenuDescriptionGap = 10, MenuSeparatorHeight = 20 };
            statusLabel.Text = "Vertical overrides applied. Open Try described menu.";
        };
        resetVertical.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { MenuIconVerticalPadding = null, MenuDescriptionVerticalPadding = null, MenuDescriptionGap = null, MenuSeparatorHeight = null };
            statusLabel.Text = "Vertical spacing reset; horizontal settings retained.";
        };
        vertical.Items.AddRange(roomier, resetVertical, described);
        var containers = new RibbonGroup { Text = "Menu size, heading and overflow" }; tab.Groups.Add(containers);
        var applyContainers = new RibbonButtonItem { Text = "Roomier menu containers", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var resetContainers = new RibbonButtonItem { Text = "Reset menu containers", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var headed = new RibbonDropDownItem { Text = "Try headed menu", DropDownHeader = "Menu size and heading example", Icon = RibbonStockIcons.Create("open"), SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        headed.Items.AddRange(new RibbonButtonItem { Text = "Open document", MenuDescription = "Menu width and heading clearances share the resolved geometry." },
            new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true }, new RibbonTextBoxItem { Text = "Name", Value = "Retained native editor", FillMenuWidth = true });
        applyContainers.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { MenuMinimumWidth = 380, MenuDescriptionMinimumWidth = 520, MenuHeaderHorizontalPadding = 24,
                MenuHeaderVerticalPadding = 16, MenuHeaderBottomGap = 12, MenuOverflowScrollButtonHeight = 44 };
            statusLabel.Text = "Container overrides applied. Open Try headed menu; enable Overflow examples to review QAT scroll bands.";
        };
        resetContainers.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { MenuMinimumWidth = null, MenuDescriptionMinimumWidth = null, MenuHeaderHorizontalPadding = null,
                MenuHeaderVerticalPadding = null, MenuHeaderBottomGap = null, MenuOverflowScrollButtonHeight = null };
            statusLabel.Text = "Container geometry reset; horizontal and vertical row settings retained.";
        };
        var overflowExamples = new RibbonToggleItem { Text = "Overflow examples", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        RibbonDropDownItem? arrowSubmenu = null; RibbonSplitButtonItem? arrowSplit = null;
        RibbonElement[] examples = [];
        overflowExamples.Click += (_, _) =>
        {
            if (overflowExamples.Checked)
            {
                examples = [.. Enumerable.Range(1, 24).Select(i => new RibbonButtonItem { Text = "Overflow example " + i }),
                    new RibbonQuickAccessDropDownReference { Source = arrowSubmenu }, new RibbonQuickAccessDropDownReference { Source = arrowSplit },
                    new RibbonQuickAccessComboBoxReference { Source = labelledCombo }, new RibbonQuickAccessGroupReference { Source = group }];
                ribbon.QuickAccessItems.AddRange(examples);
                statusLabel.Text = "Open Quick Access overflow; its last four examples show menu/split/ComboBox/group indicators. Toggle Overflow examples again to remove only these examples.";
            }
            else
            {
                foreach (var example in examples) { ribbon.QuickAccessItems.Remove(example); example.Dispose(); }
                examples = []; statusLabel.Text = "Temporary overflow examples removed; authored Quick Access entries retained.";
            }
        };
        containers.Items.AddRange(applyContainers, resetContainers, headed, overflowExamples);
        var arrowGroup = new RibbonGroup { Text = "Menu arrows and separators" }; tab.Groups.Add(arrowGroup);
        var widerArrows = new RibbonButtonItem { Text = "Wider arrow areas", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var resetArrows = new RibbonButtonItem { Text = "Reset arrow areas", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var arrowMenu = new RibbonDropDownItem { Text = "Try arrow menu", DropDownHeader = "Submenus and split actions", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        arrowSubmenu = new RibbonDropDownItem { Text = "Export", MenuDescription = "Choose a format from the nested menu.", Icon = RibbonStockIcons.Create("open") };
        arrowSubmenu.Items.AddRange(new RibbonButtonItem { Text = "PDF" }, new RibbonButtonItem { Text = "Word document" });
        arrowSplit = new RibbonSplitButtonItem { Text = "Paste", CheckOnClick = true, MenuDescription = "Primary toggles; the arrow opens more choices." };
        arrowSplit.Items.AddRange(new RibbonButtonItem { Text = "Text only" }, new RibbonCheckBoxItem { Text = "Keep source formatting", Checked = true });
        arrowSplit.Click += (_, _) => statusLabel.Text = "Paste primary: checked=" + arrowSplit.Checked;
        arrowMenu.Items.AddRange(arrowSubmenu, new RibbonMenuSeparatorItem(), arrowSplit);
        widerArrows.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { MenuSubmenuArrowWidth = 52, MenuSplitArrowWidth = 64, MenuChooserArrowWidth = 44, MenuSeparatorHorizontalPadding = 24 };
            statusLabel.Text = "Arrow reservations applied; indicators share the split column. Open Try arrow menu or the final Overflow examples references.";
        };
        resetArrows.Click += (_, _) =>
        {
            ribbon.Theme = ribbon.Theme with { MenuSubmenuArrowWidth = null, MenuSplitArrowWidth = null, MenuChooserArrowWidth = null, MenuSeparatorHorizontalPadding = null };
            statusLabel.Text = "Arrow and separator clearance reset; previous geometry and field-fill settings retained.";
        };
        arrowGroup.Items.AddRange(widerArrows, resetArrows, arrowMenu);
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
    }

    internal void VerifyContainerDemoContract()
    {
        var original = ribbon.Theme; var fileMode = ribbon.FileMenuMode; var qat = ribbon.QuickAccessItems.ToArray();
        EnableMenuGeometryDemo(); var items = ribbon.SelectedTab!.Groups[2].Items;
        var chooserExamples = ((RibbonDropDownItem)ribbon.SelectedTab.Groups[0].Items[2]).Items.OfType<RibbonComboBoxItem>().ToArray();
        if (chooserExamples.Length != 2 || !chooserExamples[0].ShowLabel || chooserExamples[1].ShowLabel || chooserExamples.Any(i => !i.FillMenuWidth || i.InputWidth != 120 || i.Value != "Aptos"))
            throw new InvalidOperationException("Menu editor examples must retain labelled/hidden-label modes and authored width/value.");
        var editorExamples = ribbon.SelectedTab.Groups.SelectMany(g => g.Items.OfType<RibbonDropDownItem>()).SelectMany(m => m.Items)
            .Where(i => i is RibbonTextBoxItem or RibbonComboBoxItem).ToArray();
        var fillToggle = ribbon.SelectedTab.Groups[0].Items[3]; fillToggle.PerformClick();
        if (editorExamples.Any(i => i is RibbonTextBoxItem { FillMenuWidth: true } or RibbonComboBoxItem { FillMenuWidth: true })) throw new InvalidOperationException("Fixed menu width comparison failed.");
        fillToggle.PerformClick();
        if (editorExamples.Any(i => i is RibbonTextBoxItem { FillMenuWidth: false } or RibbonComboBoxItem { FillMenuWidth: false })) throw new InvalidOperationException("Menu fill comparison failed.");
        items[0].PerformClick();
        if (ribbon.Theme.MenuMinimumWidth != 380 || ribbon.Theme.MenuDescriptionMinimumWidth != 520 || ribbon.Theme.MenuHeaderHorizontalPadding != 24 ||
            ribbon.Theme.MenuHeaderVerticalPadding != 16 || ribbon.Theme.MenuHeaderBottomGap != 12 || ribbon.Theme.MenuOverflowScrollButtonHeight != 44)
            throw new InvalidOperationException("Container demo apply did not set all six roles.");
        items[3].PerformClick();
        if (ribbon.QuickAccessItems.Count != qat.Length + 28) throw new InvalidOperationException("Temporary QAT examples were not added.");
        items[3].PerformClick();
        if (!qat.SequenceEqual(ribbon.QuickAccessItems)) throw new InvalidOperationException("Authored QAT identities/order changed.");
        items[1].PerformClick();
        if (ribbon.Theme != original || ribbon.FileMenuMode != fileMode) throw new InvalidOperationException("Container reset changed unrelated theme/startup settings.");
        var arrows = ribbon.SelectedTab.Groups[3].Items; arrows[0].PerformClick();
        if (ribbon.Theme.MenuSubmenuArrowWidth != 52 || ribbon.Theme.MenuSplitArrowWidth != 64 || ribbon.Theme.MenuChooserArrowWidth != 44 || ribbon.Theme.MenuSeparatorHorizontalPadding != 24)
            throw new InvalidOperationException("Arrow demo did not apply all four roles.");
        arrows[1].PerformClick();
        if (ribbon.Theme != original || chooserExamples.Any(i => !i.FillMenuWidth)) throw new InvalidOperationException("Arrow reset changed prior theme/fill settings.");
    }

    internal void VerifyMenuGeometryDemo(string directory)
    {
        Directory.CreateDirectory(directory); var fileMode = ribbon.FileMenuMode; var originalTheme = ribbon.Theme;
        ribbon.AnimationLevel = RibbonAnimationLevel.None;
        EnableMenuGeometryDemo(); Show(); Activate(); Pump();
        var items = ribbon.SelectedTab!.Groups[0].Items;
        var menu = (RibbonDropDownItem)items[2]; menu.ShowDropDown(); Pump();
        Require(menu.IsDropDownOpen, $"The first demo menu is reachable (width={Width}, tab={ribbon.SelectedTab.Text}, minimized={ribbon.IsMinimized})");
        var first = Surface(); var size = first.Size; Capture("default");
        items[0].PerformClick(); Require(!menu.IsDropDownOpen && ribbon.Theme.MenuIconSize == 36, "Public demo action assigns geometry and closes stale popup");
        menu.ShowDropDown(); Pump(); Require(Surface().Height > size.Height, "Demo override changes measured extent"); Capture("override");
        items[1].PerformClick(); Require(ribbon.Theme == originalTheme, "Demo Reset restores every menu override");
        menu.ShowDropDown(); Pump(); Require(Surface().Size == size, "Demo reset restores default popup bounds"); Capture("reset");
        menu.CloseDropDown(); var vertical = ribbon.SelectedTab.Groups[1].Items; var described = (RibbonDropDownItem)vertical[2];
        described.ShowDropDown(); Pump(); var verticalDefault = Surface().Size; Capture("vertical-default");
        vertical[0].PerformClick(); described.ShowDropDown(); Pump();
        Require(Surface().Height > verticalDefault.Height, "Vertical demo changes measured description and separator extent"); Capture("vertical-override");
        vertical[1].PerformClick(); Require(ribbon.Theme == originalTheme, "Vertical reset restores its four roles");
        described.ShowDropDown(); Pump(); Require(Surface().Size == verticalDefault, "Vertical reset restores exact default extent"); Capture("vertical-reset");
        Require(ribbon.FileMenuMode == fileMode, "Authored startup File presentation retained"); described.CloseDropDown(); Close();
        File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: permanent public-package Designer Sample; horizontal/vertical opt-in demo actions, measured overrides, exact reset, unchanged authored File presentation.");
        static void Pump() { var elapsed = System.Diagnostics.Stopwatch.StartNew(); while (elapsed.ElapsedMilliseconds < 220) { Application.DoEvents(); Thread.Sleep(10); } }
        Control Surface() => Control.FromHandle(GetForegroundWindow()) is ToolStripDropDown window ? window.Items.OfType<ToolStripControlHost>().Single().Control : throw new InvalidOperationException($"Sample lost foreground; capture stopped. menuOpen={menu.IsDropDownOpen}; active={Control.FromHandle(GetForegroundWindow())?.GetType().Name ?? "external window"}");
        void Capture(string name)
        {
            var bounds = Rectangle.Union(Bounds, Surface().TopLevelControl!.Bounds);
            using var pixels = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(pixels);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); pixels.Save(Path.Combine(directory, name + ".png"));
        }
    }
}
