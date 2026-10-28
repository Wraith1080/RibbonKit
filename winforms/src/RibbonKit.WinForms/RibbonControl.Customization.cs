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
    /// <summary>Occurs before the built-in QAT options dialog opens. Applications may insert their own native options pages.</summary>
    public event EventHandler<RibbonOptionsDialogEventArgs>? OptionsDialogOpening;
    internal void OnCustomizationApplied() => CustomizationApplied?.Invoke(this, EventArgs.Empty);

    /// <summary>Opens the shared native QAT editor. Returns None when focused ribbon input blocks opening. Owner defaults to the host form.</summary>
    public DialogResult ShowQuickAccessCustomization(IWin32Window? owner = null)
    {
        VerifyAccess();
        if (!Editors.CommitFocused()) return DialogResult.None;
        Popups.CloseAll(false); ScreenTips.Cancel(); ExitKeyTips(false);
        using var form = new RibbonQuickAccessCustomizationForm(this, CustomizationApplicationId);
        OptionsDialogOpening?.Invoke(this, new(form));
        return form.ShowDialog(owner ?? FindForm());
    }

    internal RibbonDropDownItem CreateCustomizationMenu(RibbonElement? element)
    {
        var menu = new RibbonDropDownItem { Text = "Quick Access Toolbar" };
        var catalog = CommandCatalog.GetAvailable(RibbonCustomizationTarget.QuickAccess);
        var source = element is null ? null : RibbonQuickAccess.Source(element) ?? element;
        var entry = catalog.FirstOrDefault(e => ReferenceEquals(e.Source, source));
        string? presentation = null;
        if (element is not null && QuickAccessItems.Contains(element) && CommandCatalog.Managed(element, CommandCatalog.Sources()))
            presentation = CommandCatalog.GetQuickAccessEntryId(element);
        void Add(string name, string text, Action<RibbonCustomizationSession> change, bool enabled = true)
        {
            var item = new RibbonButtonItem { Id = name, Text = text, Enabled = enabled };
            if (name is "AddToQuickAccess" or "RemoveFromQuickAccess" && source is RibbonItem command)
            { item.Icon = command.Icon; item.Image = command.Image; }
            item.Click += (_, _) =>
            {
                using var session = CommandCatalog.BeginCustomization(CustomizationApplicationId);
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
            using var session = CommandCatalog.BeginCustomization(CustomizationApplicationId);
            Add("AddToQuickAccess", "Add to Quick Access Toolbar", s => s.Draft.QuickAccess.Add(new() { CommandId = entry.CommandId }), !session.Draft.QuickAccess.Any(e => e.CommandId == entry.CommandId));
        }
        if (menu.Items.Count > 0) menu.Items.Add(new RibbonMenuSeparatorItem());
        Add("MoveQuickAccess", QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon ? "Show Quick Access Toolbar above the ribbon" : "Show Quick Access Toolbar below the ribbon", s => s.Draft.QuickAccessPosition = s.Draft.QuickAccessPosition == RibbonQuickAccessPosition.BelowRibbon ? RibbonQuickAccessPosition.TabRow : RibbonQuickAccessPosition.BelowRibbon);
        Add("ToggleQuickAccess", QuickAccessVisible ? "Hide Quick Access Toolbar" : "Show Quick Access Toolbar", s => s.Draft.QuickAccessVisible = !s.Draft.QuickAccessVisible);
        menu.Items.Add(new RibbonMenuSeparatorItem());
        var customize = new RibbonButtonItem { Id = "CustomizeQuickAccess", Text = "Customize Quick Access Toolbar…" };
        customize.Click += (_, _) => ShowQuickAccessCustomization(); menu.Items.Add(customize);
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
        var menu = CreateCustomizationMenu(element); _customizationMenu = menu; menu.Parent = this;
        Popups.CloseAll(false); ScreenTips.Cancel(); ExitKeyTips(false);
        // The root outlives any dismissed flyout; never attach this menu to a
        // borrowed popup control which is disposed by CloseAll.
        Popups.Toggle(menu, this, Rectangle.Empty, screen); return HasCustomizationMenu;
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
