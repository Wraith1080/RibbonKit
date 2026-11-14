using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    internal void VerifyThemeGallery(string directory)
    {
        Directory.CreateDirectory(directory);
        var tabs = ribbon.Tabs.ToArray(); var groups = tabs.SelectMany(t => t.Groups).ToArray();
        var commands = groups.SelectMany(g => g.Items).ToArray(); var qat = ribbon.QuickAccessItems.ToArray();
        var pages = ribbon.Backstage.Items.ToArray(); var fileMode = ribbon.FileMenuMode;
        Require(tabs.Select(t => t.Text).SequenceEqual(new[] { "Home", "Edit", "Gallery", "View" }), "Authored startup tabs");
        Require(viewTab.Groups.SequenceEqual(new[] { themesGroup, accentGroup }) && ReferenceEquals(themesGroup.Items.Single(), themeGallery), "Authored View theme and accent groups");
        Require(themeGallery.Items.SequenceEqual(new[] { applicationThemeTile, office2013ThemeTile, office2019ThemeTile, office2024ThemeTile }), "Only available themes plus Application Reset");
        Require(ReferenceEquals(themeGallery.SelectedItem, office2024ThemeTile) && ribbon.BuiltInTheme == RibbonBuiltInTheme.Office2024Light, "Startup theme selection");
        Require(TitleBarColor.IsEmpty, "Authored sample follows the theme's caption/header colors");
        var nodes = Descendants(ribbon.Tabs).ToArray();
        Require(nodes.All(n => !string.IsNullOrWhiteSpace(n.Id)) && nodes.Select(n => n.Id).Distinct().Count() == nodes.Length, "Unique authored IDs");
        StartPosition = FormStartPosition.Manual; Location = new(-16000, -16000); ShowInTaskbar = false;
        Show(); Application.DoEvents();
        var notes = ((BackstageInfoControl)infoPage.Content!).NotesEditor;
        notes.Text = "Theme selection retains native content"; notes.Select(3, 6);
        var notesHandle = notes.Handle; var pageParent = notes.Parent;
        var name = nameField.Value; var choice = fontField.SelectedIndex;
        var stylesSelection = documentStylesGallery.SelectedItem;
        var application = ribbon.Theme with { Accent = Color.DarkViolet, WindowCornerPreference = RibbonWindowCornerPreference.SmallRounded };
        ribbon.Theme = application;
        // Entering View follows programmatic theme changes without committing a
        // different theme. Hover/preview likewise leaves application policy alone.
        ribbon.SelectedTab = viewTab;
        Require(ReferenceEquals(themeGallery.SelectedItem, applicationThemeTile) && ReferenceEquals(application, ribbon.Theme), "View entry synchronizes without applying a theme");
        var log = new List<string>();
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        {
            ribbon.LayoutMode = mode;
            foreach (var (tile, selected, theme, corner) in new[]
            {
                (office2013ThemeTile, (RibbonBuiltInTheme?)RibbonBuiltInTheme.Office2013Light, RibbonThemes.Office2013Light, RibbonWindowCornerPreference.Square),
                (office2019ThemeTile, (RibbonBuiltInTheme?)RibbonBuiltInTheme.Office2019Light, RibbonThemes.Office2019Light, RibbonWindowCornerPreference.Square),
                (office2024ThemeTile, (RibbonBuiltInTheme?)RibbonBuiltInTheme.Office2024Light, RibbonThemes.Office2024Light, RibbonWindowCornerPreference.SystemDefault),
                (applicationThemeTile, (RibbonBuiltInTheme?)null, application, RibbonWindowCornerPreference.SmallRounded)
            })
            {
                tile.PerformClick();
                Require(ribbon.BuiltInTheme == selected && ReferenceEquals(theme, ribbon.Theme) && ReferenceEquals(tile, themeGallery.SelectedItem), "Gallery commit applies exactly its public theme choice");
                Require(EffectiveWindowCornerPreference == corner, "Theme corner policy follows the gallery choice");
                Require(ReferenceEquals(ribbon.SelectedTab, viewTab), "View remains selected after theme commit");
                Require(statusLabel.Text == "Theme: " + tile.Text, "Selection status");
                AssertRetained();
                using var image = new Bitmap(ribbon.Width, ribbon.Height);
                ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
                if (selected is { } builtIn)
                {
                    var band = builtIn == RibbonBuiltInTheme.Office2019Light ? Color.FromArgb(230, 230, 230) : Color.White;
                    int headerY = (int)Math.Round(48 * DeviceDpi / 96d);
                    Require(image.GetPixel(image.Width / 2, 2).ToArgb() == band.ToArgb(), "Default caption uses the selected theme background");
                    Require(image.GetPixel(image.Width / 2, headerY).ToArgb() == band.ToArgb(), "Default tab band uses the selected theme background");
                    if (mode == RibbonLayoutMode.Classic)
                        Require(image.GetPixel(image.Width - 30, (int)Math.Round(100 * DeviceDpi / 96d)).ToArgb() == Color.White.ToArgb(), "Ribbon body stays white below the neutral header");
                }
                image.Save(Path.Combine(directory, $"{mode}-{tile.Id}.png"));
                log.Add($"PASS: {mode}/{tile.Text}; selected tile, theme/application Reset, corner policy and authored/native state retained");
            }
        }
        WindowCornerPreference = RibbonWindowCornerPreference.Rounded;
        office2019ThemeTile.PerformClick();
        Require(EffectiveWindowCornerPreference == RibbonWindowCornerPreference.Rounded, "Explicit Form corner override survives a theme choice");
        WindowCornerPreference = null;
        Require(EffectiveWindowCornerPreference == RibbonWindowCornerPreference.Square, "Form Reset follows selected theme");
        // Exercise the original inline source's expand route. Simplified uses a
        // projected launcher; its source commit/state checks are covered above.
        ribbon.LayoutMode = RibbonLayoutMode.Classic;
        ribbon.SelectedTab = viewTab;
        bool opening = false, closed = false;
        themeGallery.DropDownOpening += (_, _) => opening = true;
        themeGallery.DropDownClosed += (_, _) => closed = true;
        themeGallery.PerformClick();
        Require(themeGallery.IsDropDownOpen, $"Built-in gallery expands (opening={opening}, closed={closed})");
        applicationThemeTile.PerformClick(); themeGallery.CloseDropDown();
        Require(ReferenceEquals(application, ribbon.Theme), "Expanded gallery source restores authored application theme");
        AssertRetained();
        log.Add($"PASS: built-in expanded gallery; explicit Form override/Reset; actual DPI={DeviceDpi}; offscreen direct paint only, actual VS and physical input not run");
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        foreach (var themeTile in new[] { office2013ThemeTile, office2019ThemeTile, office2024ThemeTile })
        {
            ribbon.LayoutMode = mode; themeTile.PerformClick();
            var selectedTheme = _sampleAppearance!.SelectedTheme; var baseAccent = ribbon.Theme.Accent;
            foreach (var (tile, color) in AccentChoices)
            {
                tile.PerformClick();
                Require(ribbon.Theme.Accent == color && ReferenceEquals(accentGallery.SelectedItem, tile) && ReferenceEquals(themeGallery.SelectedItem, themeTile), "Accent choice retains selected theme family and gallery selection");
                colorTitleBarToggle.PerformClick();
                Require(colorTitleBarToggle.Checked && TitleBarColor == color && ribbon.Theme.TitleBarForeground == Color.White, "Colored title follows accent with white ink");
                AssertRetained();
                colorTitleBarToggle.PerformClick();
                Require(!colorTitleBarToggle.Checked && TitleBarColor.IsEmpty && ribbon.Theme.TitleBarForeground.IsEmpty && ribbon.Theme.Accent == color, "Title toggle restores previous roles without resetting the accent");
            }
            defaultAccentTile.PerformClick();
            Require(ribbon.Theme.Accent == baseAccent && ribbon.BuiltInTheme == selectedTheme && ReferenceEquals(accentGallery.SelectedItem, defaultAccentTile), "Independent default accent restores theme ownership");
            log.Add($"PASS: {mode}/{themeTile.Text}; all six accent actions, selected family retained, title toggle/white ink, independent default/off and native/model retention");
        }
        ribbon.LayoutMode = RibbonLayoutMode.Classic;
        foreach (var (tile, captureName) in new[] { (office2013ThemeTile, "designer-accent-title"), (office2019ThemeTile, "designer-2019-accent-title"), (office2024ThemeTile, "designer-2024-accent-title") })
        {
            tile.PerformClick(); purpleAccentTile.PerformClick();
            if (!colorTitleBarToggle.Checked) colorTitleBarToggle.PerformClick();
            using var capture = new Bitmap(ribbon.Width, ribbon.Height);
            ribbon.DrawToBitmap(capture, new(Point.Empty, capture.Size)); capture.Save(Path.Combine(directory, captureName + ".png"));
        }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log);

        void AssertRetained()
        {
            Require(tabs.SequenceEqual(ribbon.Tabs) && groups.SequenceEqual(ribbon.Tabs.SelectMany(t => t.Groups)) && commands.SequenceEqual(ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items)), "Authored tab/group/command identities and ordering");
            Require(qat.SequenceEqual(ribbon.QuickAccessItems) && pages.SequenceEqual(ribbon.Backstage.Items) && ribbon.FileMenuMode == fileMode, "QAT/pages/File routing");
            Require(notes.Handle == notesHandle && ReferenceEquals(notes.Parent, pageParent) && notes.Text == "Theme selection retains native content" && notes.SelectionStart == 3 && notes.SelectionLength == 6, "Native page handle/parent/text/selection");
            Require(nameField.Value == name && fontField.SelectedIndex == choice && ReferenceEquals(documentStylesGallery.SelectedItem, stylesSelection), "Native fields and existing gallery selection");
        }
    }
}
