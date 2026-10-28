using System.ComponentModel;

namespace RibbonKit.WinForms;

public partial class RibbonControl
{
    private RibbonDropDownItem? _customizationMenu;
    private string _customizationApplicationId = "RibbonKit.WinForms.Application";
    private bool _quickAccessCustomizationEnabled = true;
    internal bool HasCustomizationMenu => _customizationMenu is { } menu && Popups.IsMenuExpanded(menu);
    internal RibbonDropDownItem? CustomizationMenu => _customizationMenu;
    internal void CloseCustomizationMenu() { if (_customizationMenu is { } menu) Popups.CloseMenu(menu, false); }
    private void ReleaseCustomizationMenu()
    {
        if (_customizationMenu is not { } menu) return;
        CloseCustomizationMenu(); _customizationMenu = null; menu.Parent = null; menu.Dispose();
    }
    /// <summary>Gets the stable owned command catalog and application command declarations used by native customization.</summary>
    [Category("Data"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public RibbonCommandCatalog CommandCatalog { get; }

    /// <summary>Gets or sets whether built-in QAT context menus are offered. An application ContextMenuStrip always takes precedence.</summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool QuickAccessCustomizationEnabled
    {
        get => _quickAccessCustomizationEnabled;
        set { VerifyAccess(); _quickAccessCustomizationEnabled = value; if (!value) CloseCustomizationMenu(); }
    }

    /// <summary>Gets or sets the stable application identity used by built-in customization sessions. Applications persisting preferences should assign their own identity.</summary>
    [Category("Data"), DefaultValue("RibbonKit.WinForms.Application")]
    public string CustomizationApplicationId
    {
        get => _customizationApplicationId;
        set { VerifyAccess(); ArgumentException.ThrowIfNullOrWhiteSpace(value); _customizationApplicationId = value; }
    }

    /// <summary>Occurs after any customization session successfully applies, including context commands. The application owns persistence.</summary>
    public event EventHandler? CustomizationApplied;
    /// <summary>Occurs before either built-in customization entry opens the options dialog. Applications may insert their own native options pages.</summary>
    public event EventHandler<RibbonOptionsDialogEventArgs>? OptionsDialogOpening;
    internal void OnCustomizationApplied() => CustomizationApplied?.Invoke(this, EventArgs.Empty);

    /// <summary>Opens the shared native QAT editor. Returns None when focused ribbon input blocks opening. Owner defaults to the host form.</summary>
    public DialogResult ShowQuickAccessCustomization(IWin32Window? owner = null)
        => ShowCustomization(false, owner);

    /// <summary>Opens the shared options dialog on Customize Ribbon. Returns None when focused ribbon input blocks opening. Owner defaults to the host form.</summary>
    public DialogResult ShowRibbonCustomization(IWin32Window? owner = null)
        => ShowCustomization(true, owner);

    private DialogResult ShowCustomization(bool ribbonPage, IWin32Window? owner)
    {
        VerifyAccess();
        if (!Editors.CommitFocused()) return DialogResult.None;
        Popups.CloseAll(false); ScreenTips.Cancel(); ExitKeyTips(false);
        using var form = new RibbonQuickAccessCustomizationForm(this, CustomizationApplicationId);
        if (ribbonPage) form.SelectedPage = form.ActiveRibbonPage;
        OptionsDialogOpening?.Invoke(this, new(form));
        return form.ShowDialog(owner ?? FindForm());
    }

    private bool IsQuickAccessContextElement(RibbonElement? element)
    {
        for (var node = element; node is not null; node = node.Parent as RibbonElement)
            if (ReferenceEquals(node, QuickAccessOverflow) || QuickAccessItems.Contains(node)) return true;
        return false;
    }
    internal RibbonDropDownItem CreateCustomizationMenu(RibbonElement? element, bool quickAccessContext = false)
    {
        quickAccessContext |= IsQuickAccessContextElement(element);
        var menu = new RibbonDropDownItem { Text = "Quick Access Toolbar" };
        var catalog = CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess);
        var source = element is null ? null : CommandCatalog.GetSource(element);
        var entry = catalog.FirstOrDefault(e => ReferenceEquals(e.Source, source));
        string? presentation = null;
        if (element is not null && QuickAccessItems.Contains(element) && CommandCatalog.Managed(element, CommandCatalog.Sources()))
            presentation = CommandCatalog.GetQuickAccessEntryId(element);
        void Add(string name, string text, Action<RibbonCustomizationSession> change, bool enabled = true)
        {
            var item = new RibbonButtonItem { Id = name, Text = text, Enabled = enabled };
            item.Click += (_, _) =>
            {
                using var session = CommandCatalog.BeginCustomization(CustomizationApplicationId, RibbonCustomizationTarget.QuickAccess);
                try
                {
                    change(session);
                    if (!session.Apply()) MessageBox.Show(FindForm(), "Complete the highlighted ribbon input before changing the toolbar.", "Quick Access Toolbar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (InvalidOperationException e) when (session.ApplyVersion == 0) { MessageBox.Show(FindForm(), e.Message, "Quick Access Toolbar", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            };
            menu.Items.Add(item);
        }
        if (presentation is not null)
            Add("RemoveFromQuickAccess", "Remove from Quick Access Toolbar", s => s.Draft.QuickAccess.RemoveAll(e => e.Id == presentation));
        else if (entry is not null)
        {
            using var session = CommandCatalog.BeginCustomization(CustomizationApplicationId, RibbonCustomizationTarget.QuickAccess);
            Add("AddToQuickAccess", "Add to Quick Access Toolbar", s => s.Draft.QuickAccess.Add(new() { CommandId = entry.CommandId }), !session.Draft.QuickAccess.Any(e => e.CommandId == entry.CommandId));
        }
        if (menu.Items.Count > 0) menu.Items.Add(new RibbonMenuSeparatorItem());
        Add("MoveQuickAccess", QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon ? "Show Quick Access Toolbar above the ribbon" : "Show Quick Access Toolbar below the ribbon", s => s.Draft.QuickAccessPosition = s.Draft.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon ? RibbonQuickAccessPosition.TabRow : RibbonQuickAccessPosition.BelowRibbon);
        Add("ToggleQuickAccess", QuickAccessVisible ? "Hide Quick Access Toolbar" : "Show Quick Access Toolbar", s => s.Draft.QuickAccessVisible = !s.Draft.QuickAccessVisible);
        if (LayoutMode == RibbonLayoutMode.Simplified && source is RibbonItem command && SelectedTab is { IsContextual: false, IsModal: false } selected
            && _simplified.Commands(selected, false).Any(c => c.Id == command.Id) && GetMergeSource(command) is null)
        {
            bool pinned = _simplified.Selected(selected).Any(c => c.Command.Id == command.Id);
            var pin = new RibbonButtonItem { Id = "ToggleSimplifiedPin", Text = pinned ? "Remove from Simplified Ribbon" : "Pin to Simplified Ribbon" };
            pin.Click += (_, _) =>
            {
                using var session = CommandCatalog.BeginCustomization(CustomizationApplicationId, RibbonCustomizationTarget.Simplified);
                var tab = session.Draft.Simplified!.Tabs.FirstOrDefault(t => t.TabId == selected.Id);
                if (tab is null)
                {
                    tab = new() { TabId = selected.Id, Commands = _simplified.Defaults(selected) };
                    session.Draft.Simplified.Tabs.Add(tab);
                }
                if (pinned) tab.Commands.RemoveAll(c => c.CommandId == command.Id); else tab.Commands.Add(new() { CommandId = command.Id, Size = command.SimplifiedSize });
                try { if (!session.Apply()) MessageBox.Show(FindForm(), "Complete the highlighted ribbon input before changing Simplified choices.", "Customize Ribbon", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                catch (InvalidOperationException error) when (session.ApplyVersion == 0) { MessageBox.Show(FindForm(), error.Message, "Customize Ribbon", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            };
            menu.Items.Add(new RibbonMenuSeparatorItem()); menu.Items.Add(pin);
        }
        if (!quickAccessContext)
        {
            var mode = new RibbonButtonItem { Id = "ToggleSimplifiedRibbon", Text = "Simplified Ribbon", Icon = LayoutMode == RibbonLayoutMode.Simplified ? RibbonMenuCheckmark.Instance : null };
            mode.Click += (_, _) => LayoutMode = LayoutMode == RibbonLayoutMode.Classic ? RibbonLayoutMode.Simplified : RibbonLayoutMode.Classic;
            menu.Items.Add(new RibbonMenuSeparatorItem()); menu.Items.Add(mode);
        }
        menu.Items.Add(new RibbonMenuSeparatorItem());
        var customize = new RibbonButtonItem { Id = "CustomizeQuickAccess", Text = "Customize Quick Access Toolbar…" };
        customize.Click += (_, _) => ShowQuickAccessCustomization(); menu.Items.Add(customize);
        var customizeRibbon = new RibbonButtonItem { Id = "CustomizeRibbon", Text = "Customize Ribbon…" };
        customizeRibbon.Click += (_, _) => ShowRibbonCustomization(); menu.Items.Add(customizeRibbon);
        return menu;
    }

    internal bool ShowCustomizationMenu(Control surface, Point screen, RibbonElement? element)
    {
        if (ContextMenuStrip is { } applicationMenu)
        {
            if (ReferenceEquals(surface, this)) return false;
            Popups.CloseAll(false); applicationMenu.Show(this, PointToClient(screen)); return true;
        }
        if (!QuickAccessCustomizationEnabled || IsBackstageOpen || !Enabled) return false;
        ReleaseCustomizationMenu();
        bool quickAccessContext = (ReferenceEquals(surface, this) && Snapshot.QuickAccessBounds.Contains(PointToClient(screen))) || Popups.IsQuickAccessContext(surface);
        var menu = CreateCustomizationMenu(element, quickAccessContext); _customizationMenu = menu; menu.Parent = this;
        ScreenTips.Cancel(); ExitKeyTips(false);
        if (surface is RibbonPopupSurface popup && Popups.Surfaces.Contains(popup))
            Popups.Toggle(menu, popup, popup.RectangleToClient(new Rectangle(screen, Size.Empty)), screen);
        else { Popups.CloseAll(false); Popups.Toggle(menu, this, Rectangle.Empty, screen); }
        return HasCustomizationMenu;
    }
    internal bool ProcessCustomizationKey(Keys keys)
    {
        if (Editors.ContainsFocus) return false;
        if (keys != (Keys.Shift | Keys.F10) && keys != Keys.Apps) return false;
        var element = FocusedElement;
        var bounds = Snapshot.Targets.FirstOrDefault(t => ReferenceEquals(t.Element, element))?.VisibleBounds ?? ClientRectangle;
        return ShowCustomizationMenu(this, PointToScreen(new(bounds.Left, bounds.Bottom)), element);
    }
    private bool HandleCustomizationContext(ref Message m)
    {
        if (m.Msg == 0x7B && QuickAccessCustomizationEnabled && ContextMenuStrip is null && !IsInDesignMode)
        {
            bool keyboard = m.LParam == new IntPtr(-1);
            var screen = keyboard ? PointToScreen(Point.Empty) : new Point(unchecked((short)(long)m.LParam), unchecked((short)((long)m.LParam >> 16)));
            var local = PointToClient(screen);
            var element = keyboard ? FocusedElement : Snapshot.Targets.LastOrDefault(t => t.VisibleBounds.Contains(local))?.Element
                ?? Snapshot.Groups.FirstOrDefault(g => g.Frame.Contains(local))?.Group;
            if (ShowCustomizationMenu(this, screen, element)) return true;
        }
        return false;
    }
}
