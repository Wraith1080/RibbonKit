namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal static void CheckThemeSelectorHidden(string output)
    {
        Directory.CreateDirectory(output);
        string settings = Path.Combine(output, "isolated-preferences");
        if (Directory.Exists(settings)) throw new InvalidOperationException("Use a fresh diagnostic directory to preserve prior preferences.");
        using var writer = new MainForm(settings);
        _ = writer.Handle; writer.OnLoad(EventArgs.Empty);
        var menu = (RibbonDropDownItem)writer.ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Single(g => g.Id == "group.theme").Items.Single();
        foreach (var theme in new RibbonBuiltInTheme?[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light, null })
        {
            string id = "demo.theme." + (theme?.ToString() ?? "application");
            menu.Items.Single(i => i.Id == id).PerformClick();
            using var reader = new MainForm(settings);
            if (writer.ribbon.BuiltInTheme != theme || reader.ribbon.BuiltInTheme != theme
                || menu.Items.OfType<RibbonRadioButtonItem>().Single(i => i.Checked).Id != id)
                throw new InvalidOperationException("Selector, selection state or startup theme restore disagreed: " + id);
            if (reader.nameInput.Value != writer.nameInput.Value || reader.fontInput.Value != writer.fontInput.Value)
                throw new InvalidOperationException("Theme selection changed application field values.");
        }
        if (writer.Visible) throw new InvalidOperationException("Hidden theme check showed a window.");
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: actual Showcase selector actions for Office 2019/2024/Application theme, radio state, automatic preference save and startup restore; native field values retained; no visible windows/input.");
    }
}
