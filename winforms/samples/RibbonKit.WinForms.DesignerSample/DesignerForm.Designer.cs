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
        ribbonSeparator2 = new RibbonKit.WinForms.RibbonMenuSeparatorItem();
        selectDropdown = new RibbonKit.WinForms.RibbonDropDownItem();
        selectObjectButton = new RibbonKit.WinForms.RibbonButtonItem();
        ribbonSeparator1 = new RibbonKit.WinForms.RibbonMenuSeparatorItem();
        selectAllButton = new RibbonKit.WinForms.RibbonButtonItem();
        pasteSplitButton = new RibbonKit.WinForms.RibbonSplitButtonItem();
        pasteSpecialButton = new RibbonKit.WinForms.RibbonButtonItem();
        pasteNoFormattingButton = new RibbonKit.WinForms.RibbonButtonItem();
        ribbonGroup1 = new RibbonKit.WinForms.RibbonGroup();
        cmbFont = new RibbonKit.WinForms.RibbonComboBoxItem();
        cmbFontSize = new RibbonKit.WinForms.RibbonComboBoxItem();
        boldToggle = new RibbonKit.WinForms.RibbonToggleItem();
        btnItalic = new RibbonKit.WinForms.RibbonToggleItem();
        btnUnderline = new RibbonKit.WinForms.RibbonToggleItem();
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
        backstageCommand1 = new RibbonKit.WinForms.RibbonButtonItem();
        saveButton = new RibbonKit.WinForms.RibbonButtonItem();
        ribbonQatReference1 = new RibbonKit.WinForms.RibbonQuickAccessReference();
        ribbonQatReference2 = new RibbonKit.WinForms.RibbonQuickAccessReference();
        statusLabel = new Label();
        button1 = new Button();
        ((System.ComponentModel.ISupportInitialize)ribbon).BeginInit();
        SuspendLayout();
        // 
        // homeTab
        // 
        homeTab.Groups.AddRange(new RibbonKit.WinForms.RibbonGroup[] { commandsGroup, ribbonGroup1 });
        homeTab.Id = "home";
        homeTab.KeyTip = "H";
        homeTab.Text = "Home";
        // 
        // commandsGroup
        // 
        commandsGroup.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:window-apps", true);
        commandsGroup.Id = "commands";
        commandsGroup.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] { applyButton, ribbonSeparator2, selectDropdown, pasteSplitButton });
        commandsGroup.Text = "Commands";
        // 
        // applyButton
        // 
        applyButton.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:check", true);
        applyButton.Id = "apply";
        applyButton.Image = (Image)resources.GetObject("applyButton.Image");
        applyButton.KeyTip = "A";
        applyButton.Text = "Apply";
        applyButton.Click += ApplyButton_Click;
        // 
        // ribbonSeparator2
        // 
        ribbonSeparator2.Id = "rk:3799895c5ebd4e87b66f7d350997b984";
        ribbonSeparator2.Text = "New separator";
        // 
        // selectDropdown
        // 
        selectDropdown.DropDownHeader = "Object Selection";
        selectDropdown.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:copy-select", true);
        selectDropdown.Id = "rk:e0b3a46259ba4a868a80219eb1d82b07";
        selectDropdown.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] { selectObjectButton, ribbonSeparator1, selectAllButton });
        selectDropdown.MenuPresentation = RibbonKit.WinForms.RibbonMenuPresentation.IllustratedChoices;
        selectDropdown.Text = "Select";
        // 
        // selectObjectButton
        // 
        selectObjectButton.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:hand-point", true);
        selectObjectButton.Id = "rk:8069cea209974095b0550713403a61c4";
        selectObjectButton.MenuDescription = "Select one or more object from the document";
        selectObjectButton.SizePolicy = RibbonKit.WinForms.RibbonSizePolicy.Adaptive(RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Medium);
        selectObjectButton.Text = "Select Object";
        // 
        // ribbonSeparator1
        // 
        ribbonSeparator1.Id = "rk:cd1c74fde75547a698d758758bbd7543";
        ribbonSeparator1.Text = "New separator";
        // 
        // selectAllButton
        // 
        selectAllButton.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:grid", true);
        selectAllButton.Id = "rk:1d6b63dd99a54419bd43cc97c59e207c";
        selectAllButton.MenuDescription = "Select every object from the document";
        selectAllButton.SizePolicy = RibbonKit.WinForms.RibbonSizePolicy.Adaptive(RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Medium);
        selectAllButton.Text = "Select All";
        // 
        // pasteSplitButton
        // 
        pasteSplitButton.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:paste", true);
        pasteSplitButton.Id = "rk:0ccb08fcbf7f4657b1eeceeb3c9b5d28";
        pasteSplitButton.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] { pasteSpecialButton, pasteNoFormattingButton });
        pasteSplitButton.Text = "Paste";
        // 
        // pasteSpecialButton
        // 
        pasteSpecialButton.Id = "rk:dfaa94b368c44b3eb888b102ffc4b49e";
        pasteSpecialButton.SizePolicy = RibbonKit.WinForms.RibbonSizePolicy.Adaptive(RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Medium);
        pasteSpecialButton.Text = "Paste Special";
        // 
        // pasteNoFormattingButton
        // 
        pasteNoFormattingButton.Id = "rk:0025fbe12c944e9a85274a9dfbcefeaf";
        pasteNoFormattingButton.SizePolicy = RibbonKit.WinForms.RibbonSizePolicy.Adaptive(RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Medium);
        pasteNoFormattingButton.Text = "Paste without formatting";
        // 
        // ribbonGroup1
        // 
        ribbonGroup1.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:text-font-info", true);
        ribbonGroup1.Id = "rk:5ca74e4a0f0442a3ba05a1ab382e38b3";
        ribbonGroup1.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] { cmbFont, cmbFontSize, boldToggle, btnItalic, btnUnderline });
        ribbonGroup1.Layout = RibbonKit.WinForms.RibbonGroupLayout.TwoRows;
        ribbonGroup1.ReductionMode = RibbonKit.WinForms.RibbonGroupReductionMode.ResizeThenCollapse;
        ribbonGroup1.Text = "Formatting";
        // 
        // cmbFont
        // 
        cmbFont.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:text-font", true);
        cmbFont.Id = "rk:687b283484d44a5f88eba73dbef488f9";
        cmbFont.Items.AddRange(new string[] { "Aptos", "Arial", "Segoe UI", "Tahoma", "Times New Roman", "Verdana" });
        cmbFont.SelectedIndex = 0;
        cmbFont.ShowLabel = false;
        cmbFont.SizePolicy = RibbonKit.WinForms.RibbonSizePolicy.Adaptive(RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Small);
        cmbFont.Text = "Font :";
        cmbFont.Value = "Aptos";
        // 
        // cmbFontSize
        // 
        cmbFontSize.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:text-font-size", true);
        cmbFontSize.Id = "rk:8c0d075f57ff411f945b097c2f86a11d";
        cmbFontSize.InputWidth = 50;
        cmbFontSize.Items.AddRange(new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15" });
        cmbFontSize.SelectedIndex = 9;
        cmbFontSize.ShowLabel = false;
        cmbFontSize.SizePolicy = RibbonKit.WinForms.RibbonSizePolicy.Adaptive(RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Medium, RibbonKit.WinForms.RibbonItemSize.Small);
        cmbFontSize.Text = "Font Size :";
        cmbFontSize.Value = "10";
        // 
        // boldToggle
        // 
        boldToggle.Checked = true;
        boldToggle.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:bold", true);
        boldToggle.Id = "bold";
        boldToggle.Row = RibbonKit.WinForms.RibbonItemRow.Second;
        boldToggle.SizePolicy = RibbonKit.WinForms.RibbonSizePolicy.Adaptive(RibbonKit.WinForms.RibbonItemSize.Small, RibbonKit.WinForms.RibbonItemSize.Small, RibbonKit.WinForms.RibbonItemSize.Small);
        boldToggle.Text = "Bold";
        // 
        // btnItalic
        // 
        btnItalic.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:italic", true);
        btnItalic.Id = "rk:ade40c8b01b944429dfa677d9688b5a5";
        btnItalic.Row = RibbonKit.WinForms.RibbonItemRow.Second;
        btnItalic.SizePolicy = RibbonKit.WinForms.RibbonSizePolicy.Adaptive(RibbonKit.WinForms.RibbonItemSize.Small, RibbonKit.WinForms.RibbonItemSize.Small, RibbonKit.WinForms.RibbonItemSize.Small);
        btnItalic.Text = "Italic";
        // 
        // btnUnderline
        // 
        btnUnderline.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:underline", true);
        btnUnderline.Id = "rk:f5a461d4d0b74445a485b18542efb82f";
        btnUnderline.Row = RibbonKit.WinForms.RibbonItemRow.Second;
        btnUnderline.SizePolicy = RibbonKit.WinForms.RibbonSizePolicy.Adaptive(RibbonKit.WinForms.RibbonItemSize.Small, RibbonKit.WinForms.RibbonItemSize.Small, RibbonKit.WinForms.RibbonItemSize.Small);
        btnUnderline.Text = "Underline";
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
        fieldsGroup.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:text-field", true);
        fieldsGroup.Id = "fields";
        fieldsGroup.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] { nameField, fontField });
        fieldsGroup.Text = "Fields";
        // 
        // nameField
        // 
        nameField.DataBindings.Add(new Binding("Value", bindingTextBox, "Text", true, DataSourceUpdateMode.OnPropertyChanged));
        nameField.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:text-t", true);
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
        fontField.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:text-font", true);
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
        printPage.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:print", true);
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
        // ribbon.Backstage
        // 
        ribbon.Backstage.FooterItems.AddRange(new RibbonKit.WinForms.RibbonItem[] { backstageFooterSeparator, backstageOptionsCommand, backstageCommand1 });
        ribbon.Backstage.Id = "rk:03b381db26f845b490311e09277bd042";
        ribbon.Backstage.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] { infoPage, backstageSaveCommand, backstageMainSeparator, printPage });
        ribbon.Backstage.NavigationBackgroundOpacity = 0F;
        ribbon.BuiltInTheme = RibbonKit.WinForms.RibbonBuiltInTheme.Office2024Light;
        // 
        // ribbon.CommandCatalog
        // 
        ribbon.CommandCatalog.Commands.AddRange(new RibbonKit.WinForms.RibbonItem[] { saveButton });
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
        ribbon.QuickAccessItems.AddRange(new RibbonKit.WinForms.RibbonElement[] { ribbonQatReference1, ribbonQatReference2 });
        ribbon.QuickAccessPosition = RibbonKit.WinForms.RibbonQuickAccessPosition.BelowRibbon;
        ribbon.Size = new Size(1029, 287);
        ribbon.TabIndex = 0;
        ribbon.Tabs.AddRange(new RibbonKit.WinForms.RibbonTab[] { homeTab, editTab });
        // 
        // backstageCommand1
        // 
        backstageCommand1.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:arrow-exit", true);
        backstageCommand1.Id = "backstage.exit";
        backstageCommand1.Text = "Exit";
        // 
        // saveButton
        // 
        saveButton.Icon = new RibbonKit.WinForms.RibbonSvgIcon("stock:save", true);
        saveButton.Id = "qat.Save";
        saveButton.ScreenTipDescription = "Save the documents";
        saveButton.ScreenTipTitle = "Save";
        saveButton.Text = "Save";
        saveButton.Click += saveButton_Click;
        // 
        // ribbonQatReference1
        // 
        ribbonQatReference1.Id = "rk:772d6ad099024145bbcb696d8739585d";
        ribbonQatReference1.Source = applyButton;
        // 
        // ribbonQatReference2
        // 
        ribbonQatReference2.Id = "rk:7cf51470e7dc4796a830db3f28359224";
        ribbonQatReference2.Source = saveButton;
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
        // button1
        // 
        button1.ForeColor = Color.Black;
        button1.Location = new Point(538, 401);
        button1.Name = "button1";
        button1.Size = new Size(186, 80);
        button1.TabIndex = 2;
        button1.Text = "button1";
        button1.UseVisualStyleBackColor = true;
        // 
        // DesignerForm
        // 
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        Backdrop = RibbonKit.WinForms.RibbonWindowBackdrop.Mica;
        ClientSize = new Size(1029, 667);
        Controls.Add(button1);
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
    private RibbonKit.WinForms.RibbonButtonItem backstageCommand1;
    private Button button1;
    private RibbonKit.WinForms.RibbonButtonItem saveButton;
    private RibbonKit.WinForms.RibbonQuickAccessReference ribbonQatReference1;
    private RibbonKit.WinForms.RibbonQuickAccessReference ribbonQatReference2;
    private RibbonKit.WinForms.RibbonDropDownItem selectDropdown;
    private RibbonKit.WinForms.RibbonButtonItem selectObjectButton;
    private RibbonKit.WinForms.RibbonButtonItem selectAllButton;
    private RibbonKit.WinForms.RibbonSplitButtonItem pasteSplitButton;
    private RibbonKit.WinForms.RibbonButtonItem pasteSpecialButton;
    private RibbonKit.WinForms.RibbonButtonItem pasteNoFormattingButton;
    private RibbonKit.WinForms.RibbonMenuSeparatorItem ribbonSeparator1;
    private RibbonKit.WinForms.RibbonMenuSeparatorItem ribbonSeparator2;
    private RibbonKit.WinForms.RibbonGroup ribbonGroup1;
    private RibbonKit.WinForms.RibbonComboBoxItem cmbFont;
    private RibbonKit.WinForms.RibbonComboBoxItem cmbFontSize;
    private RibbonKit.WinForms.RibbonToggleItem btnItalic;
    private RibbonKit.WinForms.RibbonToggleItem btnUnderline;
}
