namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task CaptureBackstage(string directory)
    {
        Directory.CreateDirectory(directory);
        Activate();
        await Task.Delay(100);
        ribbon.IsBackstageOpen = true;
        if (!ribbon.IsBackstageOpen) throw new InvalidOperationException("Backstage did not open for capture.");
        foreach (var page in ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>())
        {
            ribbon.Backstage.SelectedPage = page;
            Refresh(); Update();
            await Task.Delay(80);
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost foreground during capture.");
            using var bitmap = new Bitmap(Width, Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen(Location, Point.Empty, bitmap.Size);
            bitmap.Save(Path.Combine(directory, page.Text.ToLowerInvariant() + ".png"));
        }
    }
    private void InitializeBackstage()
    {
        ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
        // Backstage Layout properties support designer-authored widths; null keeps Auto.
        ribbon.FileMenu.ScreenTipDescription = "Open document information, print settings and commands.";
        var info = Page("Document information", "These native fields and their data belong to this application.");
        var nameLabel = new Label { Text = "Document name", AutoSize = true, Location = new(28, 115) };
        var name = new TextBox { Location = new(28, 143), Width = 280, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        name.DataBindings.Add(new Binding(nameof(TextBox.Text), formattingSource, nameof(FormattingState.Name), true, DataSourceUpdateMode.OnPropertyChanged));
        var notesLabel = new Label { Text = "Notes", AutoSize = true, Location = new(28, 190) };
        var notes = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, Location = new(28, 218), Size = new(440, 115), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Text = "Switch to Print and back: your notes stay here." };
        info.Controls.AddRange([nameLabel, name, notesLabel, notes]);
        var print = Page("Print", "Choose settings for this demonstration document.");
        print.Controls.Add(new Label { Text = "Copies", AutoSize = true, Location = new(28, 115) });
        var copies = new NumericUpDown { Minimum = 1, Maximum = 99, Value = 1, Location = new(28, 143), Width = 110 };
        print.Controls.Add(new Label { Text = "Paper size", AutoSize = true, Location = new(28, 190) });
        var paper = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new(28, 218), Width = 240 };
        paper.Items.AddRange(["A4", "Letter", "Legal"]); paper.SelectedIndex = 0;
        var preview = new Button { Text = "Preview settings", Location = new(28, 275), AutoSize = true };
        var summary = new Label { Text = "No print job has been submitted.", AutoSize = true, Location = new(28, 320) };
        preview.Click += (_, _) => summary.Text = $"Preview: {copies.Value} copies on {paper.Text}.";
        print.Controls.AddRange([copies, paper, preview, summary]);
        var newCommand = new RibbonButtonItem { Id = "backstage.new", Text = "New", KeyTip = "N", Icon = new SampleIcon(0) };
        var saveCommand = new RibbonButtonItem { Id = "backstage.save", Text = "Save", KeyTip = "S", Icon = new SampleIcon(7) };
        // Full-color image and vector icon examples share the library's left icon slot.
        var infoImage = SystemIcons.Information.ToBitmap();
        newCommand.Click += ActionClick; saveCommand.Click += ActionClick;
        ribbon.Backstage.Items.AddRange(
            new RibbonBackstagePageItem { Id = "backstage.info", Text = "Info", KeyTip = "I", Content = info, Image = infoImage },
            newCommand, saveCommand,
            new RibbonBackstagePageItem { Id = "backstage.print", Text = "Print", KeyTip = "P", Content = print, Icon = new SampleIcon(1) });
        Disposed += (_, _) => { info.Dispose(); print.Dispose(); infoImage.Dispose(); };
        static Panel Page(string title, string description)
        {
            var panel = new Panel { AutoScroll = true, BackColor = Color.White, Size = new(540, 400) };
            var headingFont = new Font("Segoe UI", 22);
            panel.Disposed += (_, _) => headingFont.Dispose();
            panel.Controls.Add(new Label { Text = title, AutoSize = true, Font = headingFont, Location = new(24, 24) });
            panel.Controls.Add(new Label { Text = description, AutoSize = true, Location = new(28, 78) });
            return panel;
        }
    }
}
