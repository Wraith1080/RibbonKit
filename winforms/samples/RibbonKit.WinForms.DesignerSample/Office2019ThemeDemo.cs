using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    internal void EnableOffice2019ThemeDemo()
    {
        var tab = new RibbonTab { Text = "Office 2019 Light" };
        var themes = new RibbonGroup { Text = "Light themes" };
        Add(themes, "Office 2019 Light", () => ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light);
        Add(themes, "Office 2024 Light", () => ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light);
        Add(themes, "Application theme", () => ribbon.BuiltInTheme = null);
        Add(themes, "Custom 2019 accent", () => ribbon.Theme = RibbonThemes.Office2019Light with { Accent = Color.Purple });
        var caption = new RibbonGroup { Text = "Header and layout" };
        Add(caption, "Blue title bar", () => TitleBarColor = Color.FromArgb(43, 87, 154));
        Add(caption, "Neutral title bar", () => TitleBarColor = Color.Empty);
        Add(caption, "Mouse / Touch", () => ribbon.Density = ribbon.Density == RibbonDensity.Compact ? RibbonDensity.Touch : RibbonDensity.Compact);
        Add(caption, "Classic / Simplified", () => ribbon.LayoutMode = ribbon.LayoutMode == RibbonLayoutMode.Classic ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic);
        var surfaces = new RibbonGroup { Text = "Square surfaces" };
        var menu = new RibbonDropDownItem { Text = "Try popup", Icon = RibbonStockIcons.Create("open"), DropDownHeader = "Bordered and shadowed popup", ScreenTipTitle = "Square popup", ScreenTipDescription = "The popup keeps its border and shadow while the body stays flat.", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var nested = new RibbonDropDownItem { Text = "Nested menu" }; nested.Items.Add(new RibbonButtonItem { Text = "Nested action" });
        menu.Items.AddRange(new RibbonCheckBoxItem { Text = "Keep formatting", Checked = true },
            new RibbonTextBoxItem { Text = "Name", Value = "Retained popup field", FillMenuWidth = true }, nested);
        surfaces.Items.Add(menu);
        Add(surfaces, "Open Backstage", () => ribbon.IsBackstageOpen = true);
        Add(surfaces, "Show message", () => ribbon.MessageBar.Items.Add(new RibbonMessage { Title = "Office 2019", Text = "Square joins and a flat ribbon body." }));
        var settings = new RibbonGroup { Text = "Shared theme settings" };
        Add(settings, "Larger inner padding", () => ribbon.Theme = ribbon.Theme with { BodyContentHorizontalPadding = 12 });
        Add(settings, "Default inner padding", () => ribbon.Theme = ribbon.Theme with { BodyContentHorizontalPadding = null });
        Add(settings, "Wider popup shadow", () => ribbon.Theme = ribbon.Theme with { PopupShadowBlur = 12, PopupShadowVerticalOffset = 4 });
        Add(settings, "Default popup shadow", () => ribbon.Theme = ribbon.Theme with { PopupShadowBlur = null, PopupShadowVerticalOffset = null });
        tab.Groups.AddRange(themes, caption, surfaces, settings); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light;
        statusLabel.Text = "Office 2019 Light: compare neutral/blue headers, flat body, inner padding and square shadowed popups.";

        void Add(RibbonGroup group, string text, Action action)
        {
            var button = new RibbonButtonItem { Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            button.Click += (_, _) => action(); group.Items.Add(button);
        }
    }
}
