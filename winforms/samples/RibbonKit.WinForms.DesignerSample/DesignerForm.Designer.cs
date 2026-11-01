namespace RibbonKitDesignerConsumer;

partial class DesignerForm
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DesignerForm));
        homeTab = new RibbonKit.WinForms.RibbonTab();
        commandsGroup = new RibbonKit.WinForms.RibbonGroup();
        applyButton = new RibbonKit.WinForms.RibbonButtonItem();
        boldToggle = new RibbonKit.WinForms.RibbonToggleItem();
        editTab = new RibbonKit.WinForms.RibbonTab();
        fieldsGroup = new RibbonKit.WinForms.RibbonGroup();
        nameField = new RibbonKit.WinForms.RibbonTextBoxItem();
        bindingTextBox = new TextBox();
        fontField = new RibbonKit.WinForms.RibbonComboBoxItem();
        infoPage = new RibbonKit.WinForms.RibbonBackstagePageItem();
        printPage = new RibbonKit.WinForms.RibbonBackstagePageItem();
        backstageSaveCommand = new RibbonKit.WinForms.RibbonButtonItem();
        backstageMainSeparator = new RibbonKit.WinForms.RibbonMenuSeparatorItem();
        backstageFooterSeparator = new RibbonKit.WinForms.RibbonMenuSeparatorItem();
        backstageOptionsCommand = new RibbonKit.WinForms.RibbonButtonItem();
        ribbon = new RibbonKit.WinForms.RibbonControl();
        statusLabel = new Label();
        ((System.ComponentModel.ISupportInitialize)ribbon).BeginInit();
        SuspendLayout();
        // 
        // homeTab
        // 
        homeTab.Groups.AddRange(new RibbonKit.WinForms.RibbonGroup[] { commandsGroup });
        homeTab.Id = "home";
        homeTab.KeyTip = "H";
        homeTab.Text = "Home";
        // 
        // commandsGroup
        // 
        commandsGroup.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:copy", true);
        commandsGroup.Id = "commands";
        commandsGroup.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] { applyButton, boldToggle });
        commandsGroup.Text = "Commands";
        // 
        // applyButton
        // 
        applyButton.Id = "apply";
        applyButton.Image = (Image)resources.GetObject("applyButton.Image");
        applyButton.KeyTip = "A";
        applyButton.Text = "Apply";
        applyButton.Click += ApplyButton_Click;
        // 
        // boldToggle
        // 
        boldToggle.Checked = true;
        boldToggle.Id = "bold";
        boldToggle.Text = "Bold";
        // 
        // editTab
        // 
        editTab.Groups.AddRange(new RibbonKit.WinForms.RibbonGroup[] { fieldsGroup });
        editTab.Id = "edit";
        editTab.KeyTip = "E";
        editTab.Text = "Edit";
        // 
        // fieldsGroup
        // 
        fieldsGroup.Id = "fields";
        fieldsGroup.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] { nameField, fontField });
        fieldsGroup.Text = "Fields";
        // 
        // nameField
        // 
        nameField.DataBindings.Add(new Binding("Value", bindingTextBox, "Text", true, DataSourceUpdateMode.OnPropertyChanged));
        nameField.Id = "name";
        nameField.Text = "Name";
        nameField.Value = "Initial value";
        // 
        // bindingTextBox
        // 
        bindingTextBox.Location = new Point(27, 320);
        bindingTextBox.Margin = new Padding(3, 4, 3, 4);
        bindingTextBox.Name = "bindingTextBox";
        bindingTextBox.Size = new Size(342, 27);
        bindingTextBox.TabIndex = 1;
        bindingTextBox.Text = "Initial value";
        // 
        // fontField
        // 
        fontField.Id = "font";
        fontField.Items.AddRange(new string[] { "Segoe UI", "Arial", "Consolas" });
        fontField.SelectedIndex = 0;
        fontField.Text = "Font";
        fontField.Value = "Segoe UI";
        // 
        // infoPage
        // 
        infoPage.ContentType = typeof(BackstageInfoControl);
        infoPage.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:info", true);
        infoPage.Id = "backstage.info";
        infoPage.KeyTip = "I";
        infoPage.Text = "Info";
        // 
        // printPage
        // 
        printPage.ContentType = typeof(BackstagePrintControl);
        printPage.Id = "backstage.print";
        printPage.KeyTip = "P";
        printPage.Text = "Print";
        // 
        // backstageSaveCommand
        // 
        backstageSaveCommand.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:save", true);
        backstageSaveCommand.Id = "backstage.save";
        backstageSaveCommand.Text = "Save";
        backstageSaveCommand.Click += BackstageSaveCommand_Click;
        // 
        // backstageMainSeparator
        // 
        backstageMainSeparator.Id = "backstage.main-separator";
        // 
        // backstageFooterSeparator
        // 
        backstageFooterSeparator.Id = "backstage.footer-separator";
        // 
        // backstageOptionsCommand
        // 
        backstageOptionsCommand.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:settings", true);
        backstageOptionsCommand.Id = "backstage.options";
        backstageOptionsCommand.KeyTip = "O";
        backstageOptionsCommand.Text = "Options";
        backstageOptionsCommand.Click += BackstageOptionsCommand_Click;
        // 
        // ribbon
        // 
        ribbon.AccessibleName = "Ribbon";
        ribbon.AnimationLevel = RibbonKit.WinForms.RibbonAnimationLevel.Expressive;
        // 
        // 
        // 
        ribbon.Backstage.FooterItems.AddRange(new RibbonKit.WinForms.RibbonItem[] { backstageFooterSeparator, backstageOptionsCommand });
        ribbon.Backstage.Id = "rk:03b381db26f845b490311e09277bd042";
        ribbon.Backstage.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] { infoPage, backstageSaveCommand, backstageMainSeparator, printPage });
        ribbon.BuiltInTheme = RibbonKit.WinForms.RibbonBuiltInTheme.Office2024Light;
        // 
        // 
        // 
        ribbon.CommandCatalog.Id = "rk:4a4d409b0c2f4b02b1929bee7dc3279e";
        ribbon.Dock = DockStyle.Top;
        // 
        // 
        // 
        ribbon.FileMenu.Id = "rk:0d5299d1f5ec4fe98abb382d675c498f";
        ribbon.FileMenu.Visible = true;
        ribbon.FileMenuMode = RibbonKit.WinForms.RibbonFileMenuMode.Backstage;
        ribbon.Location = new Point(0, 0);
        ribbon.Margin = new Padding(3, 4, 3, 4);
        // 
        // 
        // 
        ribbon.MessageBar.Id = "rk:8433585172b34728b57d006d459f4081";
        ribbon.Name = "ribbon";
        ribbon.Size = new Size(1029, 247);
        ribbon.TabIndex = 0;
        ribbon.Tabs.AddRange(new RibbonKit.WinForms.RibbonTab[] { homeTab, editTab });
        // 
        // statusLabel
        // 
        statusLabel.AutoSize = true;
        statusLabel.Location = new Point(27, 387);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(136, 20);
        statusLabel.TabIndex = 0;
        statusLabel.Text = "Designer consumer";
        // 
        // DesignerForm
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1029, 667);
        Controls.Add(statusLabel);
        Controls.Add(bindingTextBox);
        Controls.Add(ribbon);
        Margin = new Padding(3, 4, 3, 4);
        Name = "DesignerForm";
        Ribbon = ribbon;
        Text = "RibbonKit independent designer consumer";
        ((System.ComponentModel.ISupportInitialize)ribbon).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    private RibbonKit.WinForms.RibbonControl ribbon = null!;
    private RibbonKit.WinForms.RibbonTab homeTab = null!;
    private RibbonKit.WinForms.RibbonTab editTab = null!;
    private RibbonKit.WinForms.RibbonGroup commandsGroup = null!;
    private RibbonKit.WinForms.RibbonGroup fieldsGroup = null!;
    private RibbonKit.WinForms.RibbonButtonItem applyButton = null!;
    private RibbonKit.WinForms.RibbonToggleItem boldToggle = null!;
    private RibbonKit.WinForms.RibbonTextBoxItem nameField = null!;
    private RibbonKit.WinForms.RibbonComboBoxItem fontField = null!;
    private RibbonKit.WinForms.RibbonBackstagePageItem infoPage = null!;
    private RibbonKit.WinForms.RibbonBackstagePageItem printPage = null!;
    private RibbonKit.WinForms.RibbonButtonItem backstageSaveCommand = null!;
    private RibbonKit.WinForms.RibbonMenuSeparatorItem backstageMainSeparator = null!;
    private RibbonKit.WinForms.RibbonMenuSeparatorItem backstageFooterSeparator = null!;
    private RibbonKit.WinForms.RibbonButtonItem backstageOptionsCommand = null!;
    private TextBox bindingTextBox = null!;
    private Label statusLabel = null!;
}
