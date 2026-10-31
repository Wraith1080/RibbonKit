using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm : Form
{
    public DesignerForm() => InitializeComponent();

    private void ApplyButton_Click(object? sender, EventArgs e) => statusLabel.Text = "Applied: " + nameField.Value;

    internal void VerifyRoundtrip()
    {
        Require(ribbon.Tabs.Contains(homeTab) && ribbon.Tabs.Contains(editTab) && ribbon.Tabs.IndexOf(homeTab) < ribbon.Tabs.IndexOf(editTab), "Original tab identity/relative order");
        Require(homeTab.Groups.Contains(commandsGroup) && editTab.Groups.Contains(fieldsGroup), "Group ownership");
        Require(commandsGroup.Items.Contains(applyButton) && commandsGroup.Items.Contains(boldToggle), "Command identity");
        Require(fieldsGroup.Items.Contains(nameField) && fieldsGroup.Items.Contains(fontField), "Native field identity");
        var nodes = Descendants(ribbon.Tabs).ToArray();
        Require(nodes.All(node => !string.IsNullOrWhiteSpace(node.Id)) && nodes.Select(node => node.Id).Distinct().Count() == nodes.Length, "Stable unique IDs");
        Require(applyButton.Image is { Width: 16, Height: 16 }, "Image loaded from resx");
        nameField.Value = "Model change";
        Require(bindingTextBox.Text == "Model change", "Model-to-control binding");
        bindingTextBox.Text = "Control change";
        Require(nameField.Value == "Control change", "Control-to-model binding");
        applyButton.PerformClick(); Require(statusLabel.Text == "Applied: Control change", "Click event hookup");
        Require(fontField.Items.SequenceEqual(new[] { "Segoe UI", "Arial", "Consolas" }) && fontField.SelectedIndex == 0, "Choice declarations");
        Require(!typeof(RibbonControl).Assembly.GetReferencedAssemblies().Any(assembly => assembly.Name?.Contains("DesignTools") == true), "No designer SDK runtime dependency");
    }

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
            foreach (var child in Descendants(children)) yield return child;
        }
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
