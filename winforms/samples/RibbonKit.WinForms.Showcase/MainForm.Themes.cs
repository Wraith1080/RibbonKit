using RibbonKit.WinForms.Samples;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private Action? _syncThemeChoices;
    private SampleAppearance _sampleAppearance = null!;
    private RibbonGalleryItem _themeGallery = null!, _accentGallery = null!;
    private RibbonToggleItem _titleColorToggle = null!;
    private static readonly (string Key, string Name, Color? Color)[] AccentChoices =
    [
        ("default", "Default", null), ("blue", "Blue", Color.FromArgb(43, 87, 154)),
        ("teal", "Teal", Color.FromArgb(0, 112, 112)), ("green", "Green", Color.FromArgb(33, 115, 70)),
        ("purple", "Purple", Color.FromArgb(128, 57, 123)), ("red", "Red", Color.FromArgb(180, 40, 64)),
        ("orange", "Orange", Color.FromArgb(168, 75, 0))
    ];

    private void InitializeThemes()
    {
        _sampleAppearance = new(ribbon, this);
        var themeGroup = new RibbonGroup { Id = "group.theme", Text = "Themes", CanResize = false };
        _themeGallery = new RibbonGalleryItem { Id = "demo.theme", Text = "Themes", KeyTip = "H", IsInRibbon = true,
            InRibbonColumnCount = 2, ColumnCount = 4, MaxVisibleRows = 1, TileWidth = 120, TileHeight = 78,
            SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, Icon = RibbonStockIcons.Create("desktop-edit") };
        var choices = new List<(RibbonBuiltInTheme? Theme, RibbonGalleryTile Tile)>();
        AddTheme(null, "Application theme", "application");
        AddTheme(RibbonBuiltInTheme.Office2013Light, "Office 2013 Light", "Office2013Light");
        AddTheme(RibbonBuiltInTheme.Office2019Light, "Office 2019 Light", "Office2019Light");
        AddTheme(RibbonBuiltInTheme.Office2024Light, "Office 2024 Light", "Office2024Light");
        themeGroup.Items.Add(_themeGallery);
        var colorGroup = new RibbonGroup { Id = "group.accent", Text = "Colors", CanResize = false, Icon = RibbonStockIcons.Create("desktop-edit") };
        _accentGallery = new RibbonGalleryItem { Id = "demo.accent", Text = "Accent colors", KeyTip = "A", IsInRibbon = true,
            InRibbonColumnCount = 2, ColumnCount = 3, MaxVisibleRows = 3, TileWidth = 60, TileHeight = 78,
            SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, Icon = RibbonStockIcons.Create("desktop-edit") };
        foreach (var choice in AccentChoices)
        {
            var color = choice.Color ?? Color.FromArgb(43, 87, 154);
            _accentGallery.Items.Add(new RibbonGalleryTile { Id = "demo.accent." + choice.Key, Text = choice.Name,
                Icon = new RibbonSvgIcon($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 48 28'><rect x='2' y='2' width='44' height='24' rx='2' fill='#{color.R:X2}{color.G:X2}{color.B:X2}'/></svg>", false) });
        }
        _titleColorToggle = new RibbonToggleItem { Id = "demo.colorTitleBar", Text = "Color title bar", KeyTip = "C", Icon = RibbonStockIcons.Create("desktop-edit"),
            SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, ScreenTipDescription = "Use the accent color for the title bar with white title text." };
        colorGroup.Items.AddRange(_accentGallery, _titleColorToggle);
        var view = ribbon.Tabs.Single(t => t.Id == "tab.merging"); view.Groups.Insert(0, themeGroup); view.Groups.Insert(1, colorGroup);
        void Sync()
        {
            _sampleAppearance.Synchronize();
            _themeGallery.SelectedItem = choices.Single(c => c.Theme == _sampleAppearance.SelectedTheme).Tile;
            var accent = _sampleAppearance.AccentOverride;
            var key = AccentChoices.FirstOrDefault(c => c.Color?.ToArgb() == accent?.ToArgb()).Key;
            _accentGallery.SelectedItem = _accentGallery.Items.OfType<RibbonGalleryTile>().FirstOrDefault(t => t.Id == "demo.accent." + key);
            _titleColorToggle.Checked = _sampleAppearance.ColorTitleBar;
            BackColor = ribbon.Theme.Background;
        }
        _themeGallery.SelectionCommitted += (_, _) =>
        {
            _sampleAppearance.SelectTheme(choices.Single(c => ReferenceEquals(c.Tile, _themeGallery.SelectedItem)).Theme);
            Sync(); SavePreferences(); status.Text = "Theme: " + _themeGallery.SelectedItem?.Text;
        };
        _accentGallery.SelectionCommitted += (_, _) =>
        {
            _sampleAppearance.SelectAccent(AccentChoices.Single(c => _accentGallery.SelectedItem?.Id == "demo.accent." + c.Key).Color);
            Sync(); SavePreferences(); status.Text = "Accent: " + _accentGallery.SelectedItem?.Text;
        };
        _titleColorToggle.Click += (_, _) => { _sampleAppearance.SetColorTitleBar(_titleColorToggle.Checked); Sync(); SavePreferences(); };
        _themeGallery.DropDownOpening += (_, _) => Sync(); _accentGallery.DropDownOpening += (_, _) => Sync();
        ribbon.SelectedTabChanged += (_, _) => { if (ReferenceEquals(ribbon.SelectedTab, view)) Sync(); };
        _syncThemeChoices = Sync; Sync();
        void AddTheme(RibbonBuiltInTheme? theme, string text, string id)
        {
            string band = theme == RibbonBuiltInTheme.Office2019Light ? "#e6e6e6" : "#ffffff";
            string selection = theme == RibbonBuiltInTheme.Office2024Light ? "<path d='M36 28h25' stroke='#2b579a' stroke-width='3'/>" : "<rect x='30' y='17' width='36' height='15' fill='#ffffff'/>";
            string file = theme == RibbonBuiltInTheme.Office2013Light ? "<rect x='2' y='17' width='23' height='15' fill='#2b579a'/>" : "";
            var tile = new RibbonGalleryTile { Id = "demo.theme." + id, Text = text,
                Icon = new RibbonSvgIcon($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 160 64'><rect x='1' y='1' width='158' height='62' fill='#ffffff' stroke='#919191'/><rect x='2' y='2' width='156' height='30' fill='{band}'/>{selection}{file}<path d='M8 10h36m92 0h15M10 24h12m16 0h18m19 0h17' stroke='#605e5c' stroke-width='2'/><path d='M11 39h17v14H11zM60 39h17v14H60zM110 39h17v14h-17z' fill='#dae5f2'/></svg>", false) };
            choices.Add((theme, tile)); _themeGallery.Items.Add(tile);
        }
    }
}
