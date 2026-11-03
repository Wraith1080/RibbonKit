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
            new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true }, new RibbonTextBoxItem { Text = "Name", Value = "Retained native editor" });
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
        group.Items.AddRange(wider, reset, menu);
        var vertical = new RibbonGroup { Text = "Vertical menu spacing" }; tab.Groups.Add(vertical);
        var roomier = new RibbonButtonItem { Text = "More vertical spacing" };
        var resetVertical = new RibbonButtonItem { Text = "Reset vertical spacing" };
        var described = new RibbonDropDownItem { Text = "Try described menu", Icon = RibbonStockIcons.Create("open") };
        described.Items.AddRange(new RibbonButtonItem { Text = "Open document", Icon = RibbonStockIcons.Create("open"), MenuDescription = "Keep the original formatting and document layout." },
            new RibbonMenuSeparatorItem(), new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true, MenuDescription = "Preserve fonts and paragraph spacing." },
            new RibbonTextBoxItem { Text = "Name", Value = "Retained native editor" });
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
        ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
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
