using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm : RibbonForm
{
    public DesignerForm()
    {
        InitializeComponent();
        ConfigurePopup(selectDropdown, "Select options");
        ConfigurePopup(pasteSplitButton, "Paste options");
        ConfigurePopup(applicationOpen, "Open options");
        pasteSplitButton.Click += (_, _) => statusLabel.Text = "Paste primary action";
    }

    internal (RibbonDropDownItem DropDown, RibbonSplitButtonItem Split) PopupContentExample => (selectDropdown, pasteSplitButton);
    private void ConfigurePopup(RibbonDropDownItem item, string caption)
    {
        if (item.Content is not PopupOptionsControl body) return;
        body.Heading.Text = caption;
        body.Applied += (_, _) => { statusLabel.Text = caption + ": " + body.ValueEditor.Text + " / " + body.ModeEditor.Text; item.CloseDropDown(); };
        body.Dismissed += (_, _) => item.CloseDropDown();
    }

    internal (RibbonTab Tab, RibbonGalleryItem Gallery) GalleryPresenterExample => (galleryTab, documentStylesGallery);
    private void documentStylesGallery_ItemPreview(object? sender, RibbonGalleryPreviewEventArgs e) => statusLabel.Text = "Preview: " + e.Item.Text;
    private void documentStylesGallery_ItemPreviewCancelled(object? sender, EventArgs e) => statusLabel.Text = "Selected: " + documentStylesGallery.SelectedItem?.Text;
    private void documentStylesGallery_SelectionCommitted(object? sender, EventArgs e) => statusLabel.Text = "Selected: " + documentStylesGallery.SelectedItem?.Text;

    private void ApplicationCommand_Click(object? sender, EventArgs e) => statusLabel.Text = "Application Menu: " + (sender as RibbonItem)?.Text;

    private void ApplyButton_Click(object? sender, EventArgs e) => statusLabel.Text = "Applied: " + nameField.Value;
    private void BackstageSaveCommand_Click(object? sender, EventArgs e) => statusLabel.Text = "Backstage Save: " + ((BackstageInfoControl?)infoPage.Content)?.NotesEditor.Text;
    private void BackstageOptionsCommand_Click(object? sender, EventArgs e) => statusLabel.Text = "Backstage Options";

    internal void VerifyRoundtrip()
    {
        Require(ribbon.Tabs.Contains(homeTab) && ribbon.Tabs.Contains(editTab) && ribbon.Tabs.IndexOf(homeTab) < ribbon.Tabs.IndexOf(editTab), "Original tab identity/relative order");
        Require(homeTab.Groups.Contains(commandsGroup) && editTab.Groups.Contains(fieldsGroup), "Group ownership");
        Require(commandsGroup.Items.Contains(applyButton) && commandsGroup.Items.Contains(boldToggle), "Command identity");
        Require(fieldsGroup.Items.Contains(nameField) && fieldsGroup.Items.Contains(fontField), "Native field identity");
        var nodes = Descendants(ribbon.Tabs).ToArray();
        Require(nodes.All(node => !string.IsNullOrWhiteSpace(node.Id)) && nodes.Select(node => node.Id).Distinct().Count() == nodes.Length, "Stable unique IDs");
        Require(applyButton.Image is { Width: 16, Height: 16 }, "Image loaded from resx");
        Require(boldToggle.Icon is RibbonSvgIcon { Source: "stock:bold", UseForegroundColor: true }, "SVG icon loaded from string-backed resx");
        Require(commandsGroup.Icon is RibbonSvgIcon { Source: "stock:copy", UseForegroundColor: true }, "Generated SVG constructor");
        Require(RibbonStockIcons.All.Count >= 512, "Expanded stock catalog metadata");
        var addedStock = RibbonStockIcons.Create("table-add");
        using (var addedPixels = new Bitmap(32, 32))
        using (var addedPaint = Graphics.FromImage(addedPixels))
        {
            addedStock.Draw(addedPaint, new Rectangle(0, 0, 32, 32), new RibbonIconContext(192, Color.DarkCyan, true));
            bool any = false;
            for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                any |= addedPixels.GetPixel(x, y).A != 0;
            Require(any, "Expanded stock asset renders outside Showcase");
        }
        foreach (int side in new[] { 16, 32, 64 })
        {
            using var pixels = new Bitmap(side, side);
            using var paint = Graphics.FromImage(pixels);
            boldToggle.Icon!.Draw(paint, new Rectangle(0, 0, side, side), new RibbonIconContext(192, Color.DarkCyan, true));
            bool drawn = false;
            for (int y = 0; y < side; y++)
            for (int x = 0; x < side; x++)
            {
                var pixel = pixels.GetPixel(x, y);
                drawn |= pixel.A != 0;
                if (pixel.A > 200)
                    Require(Math.Abs(pixel.R - Color.DarkCyan.R) <= 1 && Math.Abs(pixel.G - Color.DarkCyan.G) <= 1 && Math.Abs(pixel.B - Color.DarkCyan.B) <= 1, "SVG foreground color follows the public draw context: " + pixel);
            }
            Require(drawn, "Public-only SVG drawing at " + side);
        }
        nameField.Value = "Model change";
        Require(bindingTextBox.Text == "Model change", "Model-to-control binding");
        bindingTextBox.Text = "Control change";
        Require(nameField.Value == "Control change", "Control-to-model binding");
        applyButton.PerformClick();
        Require(statusLabel.Text == "Applied: Control change", "Click event hookup");
        Require(fontField.Items.SequenceEqual(new[] { "Segoe UI", "Arial", "Consolas" }) && fontField.SelectedIndex == 0, "Choice declarations");
        VerifyRuntimeEditors();
        Require(ribbon.Backstage.Items.Contains(infoPage) && ribbon.Backstage.Items.Contains(printPage) && ribbon.Backstage.Items.Contains(backstageSaveCommand), "Backstage generated components retained");
        VerifyPageContent();
        var infoContent = (BackstageInfoControl)infoPage.Content!;
        Require(ReferenceEquals(infoPage.Content, infoContent), "Designer-selected UserControl retained through the public ContentType API");
        infoContent.NotesEditor.Text = "Native notes";
        var notesHandle = infoContent.NotesEditor.Handle;
        infoContent.NotesEditor.Select(2, 4);
        backstageSaveCommand.PerformClick();
        Require(statusLabel.Text == "Backstage Save: Native notes", "Backstage command event retained");
        Require(infoContent.NotesEditor.Handle == notesHandle && infoContent.NotesEditor.SelectionStart == 2 && infoContent.NotesEditor.SelectionLength == 4, "Native page editor ownership and selection retained");
        using (var borrowed = new UserControl())
        {
            var declaration = new RibbonBackstagePageItem { Content = borrowed };
            declaration.Dispose();
            Require(!borrowed.IsDisposed && borrowed.Parent is null, "Page disposal releases application-owned content without disposing it");
        }
        Require(!typeof(RibbonControl).Assembly.GetReferencedAssemblies().Any(assembly => assembly.Name?.Contains("DesignTools") == true), "No designer SDK runtime dependency");
    }

    private void VerifyRuntimeEditors()
    {
        var startup = ribbon.SelectedTab;
        var animation = ribbon.AnimationLevel;
        bool minimized = ribbon.IsMinimized;
        try
        {
            ribbon.AnimationLevel = RibbonAnimationLevel.None;
            ribbon.IsMinimized = false;
            ribbon.SelectedTab = editTab;
            _ = ribbon.Handle;
            var input = ChildControls(ribbon).OfType<TextBox>().Single(control => control.Text == nameField.Value);
            var choice = ChildControls(ribbon).OfType<ComboBox>().Single();
            var inputHandle = input.Handle;
            var choiceHandle = choice.Handle;
            using var pixels = new Bitmap(ribbon.Width, ribbon.Height);
            ribbon.DrawToBitmap(pixels, new(Point.Empty, pixels.Size));
            ribbon.SelectedTab = homeTab;
            ribbon.SelectedTab = editTab;
            Require(ChildControls(ribbon).Contains(input) && ChildControls(ribbon).Contains(choice), "Runtime keeps native editors after tab switches");
            Require(input.Handle == inputHandle && choice.Handle == choiceHandle, "Native editor handles retained; designer field preview stays design-only");
            Require(input.Text == nameField.Value && choice.SelectedIndex == fontField.SelectedIndex, "Runtime native field values retained");
        }
        finally { ribbon.SelectedTab = startup; ribbon.IsMinimized = minimized; ribbon.AnimationLevel = animation; }
    }

    private static IEnumerable<Control> ChildControls(Control parent) => parent.Controls.Cast<Control>().SelectMany(child => new[] { child }.Concat(ChildControls(child)));

    private static IEnumerable<RibbonElement> Descendants(IEnumerable<RibbonElement> elements)
    {
        foreach (var element in elements)
        {
            yield return element;
            var children = element switch
            {
                RibbonTab tab => (IEnumerable<RibbonElement>)tab.Groups,
                RibbonGroup group => group.Items,
                RibbonDropDownItem dropDown => dropDown.Items,
                _ => Array.Empty<RibbonElement>()
            };
            foreach (var child in Descendants(children))
                yield return child;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private void saveButton_Click(object sender, EventArgs e)
    {
        MessageBox.Show("Save QAT command clicked");
    }
}
