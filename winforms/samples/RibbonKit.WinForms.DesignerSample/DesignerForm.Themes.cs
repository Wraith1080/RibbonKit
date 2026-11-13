using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    private RibbonKit.WinForms.Samples.SampleAppearance? _sampleAppearance;
    private IEnumerable<(RibbonGalleryTile Tile, Color Color)> AccentChoices => new[]
    {
        (blueAccentTile, Color.FromArgb(43, 87, 154)), (tealAccentTile, Color.FromArgb(0, 112, 112)),
        (greenAccentTile, Color.FromArgb(33, 115, 70)), (purpleAccentTile, Color.FromArgb(128, 57, 123)),
        (redAccentTile, Color.FromArgb(180, 40, 64)), (orangeAccentTile, Color.FromArgb(168, 75, 0))
    };
    private void ThemeGallery_SelectionCommitted(object? sender, EventArgs e)
    {
        var selected = themeGallery.SelectedItem;
        if (_sampleAppearance is null) return;
        if (ReferenceEquals(selected, office2013ThemeTile)) _sampleAppearance.SelectTheme(RibbonBuiltInTheme.Office2013Light);
        else if (ReferenceEquals(selected, office2019ThemeTile)) _sampleAppearance.SelectTheme(RibbonBuiltInTheme.Office2019Light);
        else if (ReferenceEquals(selected, office2024ThemeTile)) _sampleAppearance.SelectTheme(RibbonBuiltInTheme.Office2024Light);
        else if (ReferenceEquals(selected, applicationThemeTile)) _sampleAppearance.SelectTheme(null);
        else return;
        SynchronizeThemeGallery();
        statusLabel.Text = "Theme: " + selected.Text;
    }

    internal void SynchronizeThemeGallery()
    {
        if (themeGallery is null) return; // InitializeComponent can raise selection events.
        _sampleAppearance?.Synchronize();
        themeGallery.SelectedItem = (_sampleAppearance?.SelectedTheme ?? ribbon.BuiltInTheme) switch
        {
            RibbonBuiltInTheme.Office2013Light => office2013ThemeTile,
            RibbonBuiltInTheme.Office2019Light => office2019ThemeTile,
            RibbonBuiltInTheme.Office2024Light => office2024ThemeTile,
            _ => applicationThemeTile
        };
        if (_sampleAppearance is { } appearance)
        {
            accentGallery.SelectedItem = appearance.AccentOverride is { } accent ? AccentChoices.FirstOrDefault(c => c.Color.ToArgb() == accent.ToArgb()).Tile : defaultAccentTile;
            colorTitleBarToggle.Checked = appearance.ColorTitleBar;
        }
    }

    private void Ribbon_ThemeGallerySelectedTabChanged(object? sender, EventArgs e)
    {
        if (ReferenceEquals(ribbon.SelectedTab, viewTab)) SynchronizeThemeGallery();
    }

    private void ThemeGallery_DropDownOpening(object? sender, System.ComponentModel.CancelEventArgs e) => SynchronizeThemeGallery();
    private void AccentGallery_SelectionCommitted(object? sender, EventArgs e)
    {
        if (_sampleAppearance is null) return;
        var selected = accentGallery.SelectedItem;
        _sampleAppearance.SelectAccent(ReferenceEquals(selected, defaultAccentTile) ? null : AccentChoices.Single(c => ReferenceEquals(c.Tile, selected)).Color);
        SynchronizeThemeGallery(); statusLabel.Text = "Accent: " + selected?.Text;
    }
    private void ColorTitleBarToggle_Click(object? sender, EventArgs e)
    {
        _sampleAppearance?.SetColorTitleBar(colorTitleBarToggle.Checked);
        SynchronizeThemeGallery();
    }
}
