namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private const string CustomizationApplicationId = "ribbonkit.winforms.showcase";
    private void InitializeCustomization()
    {
        ribbon.CustomizationApplicationId = CustomizationApplicationId;
        ribbon.CustomizationApplied += (_, _) => { status.Text = "Customization changes applied."; SaveCustomization(); };
        ribbon.OptionsDialogOpening += (_, e) =>
        {
            var page = new RibbonOptionsPage { Text = "Editor", Name = "ApplicationEditorOptions" };
            var wrap = new CheckBox { Text = "Wrap document text", Checked = document.WordWrap, AutoSize = true, Location = new(8, 12) };
            page.Controls.Add(wrap);
            page.Applied += (_, _) => { document.WordWrap = wrap.Checked; status.Text = "Application editor preferences applied."; SavePreferences(); };
            page.Cancelled += (_, _) => wrap.Checked = document.WordWrap;
            e.Dialog.Pages.Insert(0, page);
            if (e.Dialog is RibbonQuickAccessCustomizationForm shared)
            {
                shared.SimplifiedEditor.ImportRequested += (_, _) =>
                {
                    using var file = new OpenFileDialog { Filter = "Ribbon customization (*.json)|*.json", Title = "Import customization" };
                    if (file.ShowDialog(shared) != DialogResult.OK) return;
                    try { shared.SimplifiedEditor.Session.ImportJson(File.ReadAllText(file.FileName)); shared.SimplifiedEditor.RefreshDraft(); }
                    catch (Exception error) when (error is IOException or System.Text.Json.JsonException or InvalidOperationException or ArgumentException) { MessageBox.Show(shared, error.Message, "Import customization", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                };
                shared.SimplifiedEditor.ExportRequested += (_, _) =>
                {
                    try
                    {
                        string json = shared.SimplifiedEditor.Session.ExportJson();
                        using var file = new SaveFileDialog { Filter = "Ribbon customization (*.json)|*.json", DefaultExt = "json", FileName = "ribbon-customization.json", Title = "Export customization" };
                        if (file.ShowDialog(shared) == DialogResult.OK) File.WriteAllText(file.FileName, json);
                    }
                    catch (Exception error) when (error is IOException or InvalidOperationException or ArgumentException) { MessageBox.Show(shared, error.Message, "Export customization", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                };
                shared.RibbonEditor.ImportRequested += (_, _) =>
                {
                    using var file = new OpenFileDialog { Filter = "Ribbon customization (*.json)|*.json", Title = "Import customization" };
                    if (file.ShowDialog(shared) != DialogResult.OK) return;
                    try { shared.RibbonEditor.Session.ImportJson(File.ReadAllText(file.FileName)); shared.RibbonEditor.RefreshDraft(); }
                    catch (Exception error) when (error is IOException or System.Text.Json.JsonException or InvalidOperationException or ArgumentException) { MessageBox.Show(shared, error.Message, "Import customization", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                };
                shared.RibbonEditor.ExportRequested += (_, _) =>
                {
                    try
                    {
                        string json = shared.RibbonEditor.Session.ExportJson();
                        using var file = new SaveFileDialog { Filter = "Ribbon customization (*.json)|*.json", DefaultExt = "json", FileName = "ribbon-customization.json", Title = "Export customization" };
                        if (file.ShowDialog(shared) == DialogResult.OK) File.WriteAllText(file.FileName, json);
                    }
                    catch (Exception error) when (error is IOException or InvalidOperationException or ArgumentException) { MessageBox.Show(shared, error.Message, "Export customization", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                };
            }
        };
        var redo = new RibbonButtonItem { Id = "demo.qat.redo", Text = "Redo" };
        redo.Click += (_, _) => status.Text = "The application handled Redo.";
        ribbon.CommandCatalog.Commands.Add(redo);
        ribbon.CommandCatalog.CaptureDefaults();
        document.AppendText("\r\n\r\nRight-click the ribbon to open Customize Ribbon or Customize Quick Access Toolbar. Both pages share the Options dialog. Customize Ribbon can hide, rename and reorder ordinary tabs, add custom tabs/groups and populate custom groups from the command catalog. Built-in commands keep their original ownership and bindings. Each page applies independently; Cancel keeps the latest Apply. Import/Export use application-owned JSON file dialogs.");
    }
}
