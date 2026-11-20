namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal static void CheckOffice2007Preferences(string output)
    {
        var settings = Path.Combine(output, "isolated-2007-preferences");
        using var writer = new MainForm(settings); _ = writer.Handle; writer.OnLoad(EventArgs.Empty);
        var route = writer.ribbon.FileMenuMode; var frame = writer.FrameAppearance; var backdrop = writer.Backdrop;
        writer._themeGallery.Items.Single(i => i.Id == "demo.theme.Office2007Blue").PerformClick();
        writer._accentGallery.Items.Single(i => i.Id == "demo.accent.purple").PerformClick(); writer._titleColorToggle.PerformClick();
        using var reader = new MainForm(settings);
        if (reader._sampleAppearance.SelectedTheme != RibbonBuiltInTheme.Office2007Blue || !reader._titleColorToggle.Checked ||
            reader.ribbon.Theme.TitleBarBackgroundFill != writer.ribbon.Theme.TitleBarBackgroundFill || reader.ribbon.Theme.Accent != writer.ribbon.Theme.Accent ||
            reader.ribbon.FileMenuMode != route || reader.FrameAppearance != frame || reader.Backdrop != backdrop)
            throw new InvalidOperationException("Office 2007 startup family/accent/caption/File/frame/material restore disagreed.");
        writer._titleColorToggle.PerformClick(); writer._accentGallery.Items.Single(i => i.Id == "demo.accent.default").PerformClick();
        using var reset = new MainForm(settings);
        if (reset.ribbon.BuiltInTheme != RibbonBuiltInTheme.Office2007Blue || reset._titleColorToggle.Checked || !ReferenceEquals(reset.ribbon.Theme, RibbonThemes.Office2007Blue))
            throw new InvalidOperationException("Office 2007 title/accent Reset startup restore disagreed.");
        File.AppendAllText(Path.Combine(output, "result.log"), "PASS: actual Showcase startup persistence for Office2007Blue, purple accent/caption, independent Reset and saved File/frame/material choices.\n");
    }

    internal static void CheckOffice2010Preferences(string output)
    {
        var settings = Path.Combine(output, "isolated-preferences");
        using var writer = new MainForm(settings); _ = writer.Handle; writer.OnLoad(EventArgs.Empty);
        writer._themeGallery.Items.Single(i => i.Id == "demo.theme.Office2010Blue").PerformClick();
        writer._accentGallery.Items.Single(i => i.Id == "demo.accent.purple").PerformClick();
        writer._titleColorToggle.PerformClick();
        using var reader = new MainForm(settings);
        if (reader._sampleAppearance.SelectedTheme != RibbonBuiltInTheme.Office2010Blue || !reader._titleColorToggle.Checked ||
            reader.ribbon.Theme.Accent != writer.ribbon.Theme.Accent || reader.ribbon.Theme.TitleBarBackgroundFill != writer.ribbon.Theme.TitleBarBackgroundFill ||
            !reader.TitleBarColor.IsEmpty || reader.ribbon.Theme.TitleBarForeground != Color.White)
            throw new InvalidOperationException("Office 2010 theme/accent/dimensional caption startup restore disagreed.");
        writer._titleColorToggle.PerformClick(); writer._accentGallery.Items.Single(i => i.Id == "demo.accent.default").PerformClick();
        using var reset = new MainForm(settings);
        if (reset.ribbon.BuiltInTheme != RibbonBuiltInTheme.Office2010Blue || reset._titleColorToggle.Checked || !ReferenceEquals(reset.ribbon.Theme, RibbonThemes.Office2010Blue))
            throw new InvalidOperationException("Office 2010 independent title/accent Reset startup restore disagreed.");
        File.AppendAllText(Path.Combine(output, "result.log"), "PASS: actual Showcase startup persistence for Office2010Blue, purple accent, gradient/white title, independent caption/accent Reset.\n");
    }

    internal static void CheckThemeSelectorHidden(string output)
    {
        Directory.CreateDirectory(output);
        string settings = Path.Combine(output, "isolated-preferences");
        if (Directory.Exists(settings)) throw new InvalidOperationException("Use a fresh diagnostic directory to preserve prior preferences.");
        using var writer = new MainForm(settings);
        _ = writer.Handle; writer.OnLoad(EventArgs.Empty);
        var menu = writer._themeGallery;
        foreach (var theme in new RibbonBuiltInTheme?[] { RibbonBuiltInTheme.Office2013Light, RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light, null })
        {
            string id = "demo.theme." + (theme?.ToString() ?? "application");
            menu.Items.Single(i => i.Id == id).PerformClick();
            using var reader = new MainForm(settings);
            if (writer.ribbon.BuiltInTheme != theme || reader.ribbon.BuiltInTheme != theme
                || menu.SelectedItem?.Id != id)
                throw new InvalidOperationException("Selector, selection state or startup theme restore disagreed: " + id);
            if (reader.nameInput.Value != writer.nameInput.Value || reader.fontInput.Value != writer.fontInput.Value)
                throw new InvalidOperationException("Theme selection changed application field values.");
        }
        foreach (var family in new[] { RibbonBuiltInTheme.Office2013Light, RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        {
            menu.Items.Single(i => i.Id == "demo.theme." + family).PerformClick();
            foreach (var choice in AccentChoices.Where(c => c.Color is not null))
            {
                writer._accentGallery.Items.Single(i => i.Id == "demo.accent." + choice.Key).PerformClick();
                writer._titleColorToggle.PerformClick();
                using var reader = new MainForm(settings);
                if (writer.ribbon.Theme.Accent != choice.Color || writer.TitleBarColor != choice.Color || writer.ribbon.Theme.TitleBarForeground != Color.White
                    || writer._sampleAppearance.SelectedTheme != family || reader._sampleAppearance.SelectedTheme != family
                    || reader.ribbon.Theme.Accent != choice.Color || reader.TitleBarColor != choice.Color || !reader._titleColorToggle.Checked
                    || reader.ribbon.Theme.TitleBarForeground != Color.White || writer._themeGallery.SelectedItem?.Id != "demo.theme." + family)
                    throw new InvalidOperationException("Accent/title state or persisted restore disagreed: " + choice.Key);
                writer._titleColorToggle.PerformClick();
                if (!writer.TitleBarColor.IsEmpty || !writer.ribbon.Theme.TitleBarForeground.IsEmpty || writer.ribbon.Theme.Accent != choice.Color)
                    throw new InvalidOperationException("Turning off title color did not restore its independent roles.");
            }
            writer._accentGallery.Items.Single(i => i.Id == "demo.accent.default").PerformClick();
            if (writer.ribbon.BuiltInTheme != family) throw new InvalidOperationException("Accent Reset did not restore built-in ownership.");
        }
        writer._accentGallery.Items.Single(i => i.Id == "demo.accent.purple").PerformClick(); writer._titleColorToggle.PerformClick();
        writer.ribbon.SelectedTab = writer.ribbon.Tabs.Single(t => t.Id == "tab.merging");
        writer.StartPosition = FormStartPosition.Manual; writer.Location = new(-16000, -16000); writer.ShowInTaskbar = false;
        writer.Show(); Application.DoEvents();
        foreach (var (family, name) in new[] { (RibbonBuiltInTheme.Office2013Light, "showcase-accent-title"), (RibbonBuiltInTheme.Office2019Light, "showcase-2019-accent-title"), (RibbonBuiltInTheme.Office2024Light, "showcase-2024-accent-title") })
        {
            menu.Items.Single(i => i.Id == "demo.theme." + family).PerformClick();
            writer._accentGallery.Items.Single(i => i.Id == "demo.accent.purple").PerformClick();
            using var capture = new Bitmap(writer.ribbon.Width, writer.ribbon.Height);
            writer.ribbon.DrawToBitmap(capture, new(Point.Empty, capture.Size)); capture.Save(Path.Combine(output, name + ".png"));
        }
        writer.Hide();
        if (writer.Visible) throw new InvalidOperationException("Hidden theme check showed a window.");
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: actual Showcase theme and accent galleries, all three themes/six accents, colored title with white ink, independent Reset/off, persisted theme/accent/title startup restore and native field values; offscreen direct-paint only, actual VS/input not run.");
    }
}
