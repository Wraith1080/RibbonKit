using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private void ThemeGallery_SelectionCommitted(object? sender, EventArgs e)
    {
        var selected = themeGallery.SelectedItem;
        if (ReferenceEquals(selected, office2019ThemeTile)) ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light;
        else if (ReferenceEquals(selected, office2024ThemeTile)) ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        else if (ReferenceEquals(selected, applicationThemeTile)) ribbon.BuiltInTheme = null;
        else return;
        SynchronizeThemeGallery();
        statusLabel.Text = "Theme: " + selected.Text;
    }

    internal void SynchronizeThemeGallery()
    {
        if (themeGallery is null) return; // InitializeComponent can raise selection events.
        themeGallery.SelectedItem = ribbon.BuiltInTheme switch
        {
            RibbonBuiltInTheme.Office2019Light => office2019ThemeTile,
            RibbonBuiltInTheme.Office2024Light => office2024ThemeTile,
            _ => applicationThemeTile
        };
    }

    private void Ribbon_ThemeGallerySelectedTabChanged(object? sender, EventArgs e)
    {
        if (ReferenceEquals(ribbon.SelectedTab, viewTab)) SynchronizeThemeGallery();
    }

    private void ThemeGallery_DropDownOpening(object? sender, System.ComponentModel.CancelEventArgs e) => SynchronizeThemeGallery();
}
