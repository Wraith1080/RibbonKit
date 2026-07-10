using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RibbonKit.Interop;

namespace RibbonKit.Controls;

/// <summary>One choice in the edit dialog's icon picker; <see cref="Icon"/> is
/// <see langword="null"/> for the "no icon" entry.</summary>
public sealed class RibbonIconChoice
{
    internal RibbonIconChoice(ImageSource? icon) => Icon = icon;

    /// <summary>The icon, or <see langword="null"/> for "no icon".</summary>
    public ImageSource? Icon { get; }

    /// <summary>Whether this is the "no icon" entry (shown as text).</summary>
    public bool IsNone => Icon is null;
}

/// <summary>
/// The small modal opened by <see cref="RibbonCustomizePage"/>'s Edit… button (Office's
/// equivalent is its "Rename" dialog, which also hides a symbol picker). Sections appear
/// per target:
/// <list type="bullet">
/// <item>name — everything editable (built-in tabs/groups included, like Office);</item>
/// <item>icon (picked from the ribbon's own icons) + layout — custom groups;</item>
/// <item>button size — custom-group commands, constrained by the group's layout
/// (a Large-layout group locks sizes to Large).</item>
/// </list>
/// Standard result flow: OK → <c>DialogResult = true</c> and the properties carry the
/// edited values; Cancel/Close → <c>false</c>.
/// </summary>
[TemplatePart(Name = NameBoxPartName, Type = typeof(TextBox))]
[TemplatePart(Name = IconListPartName, Type = typeof(ListBox))]
[TemplatePart(Name = LayoutBoxPartName, Type = typeof(Selector))]
[TemplatePart(Name = SizeBoxPartName, Type = typeof(Selector))]
[TemplatePart(Name = IconSectionPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = LayoutSectionPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = SizeSectionPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = OkButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = CancelButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = CloseButtonPartName, Type = typeof(ButtonBase))]
public class RibbonCustomizeEditDialog : Window
{
    private const string NameBoxPartName = "PART_NameBox";
    private const string IconListPartName = "PART_IconList";
    private const string LayoutBoxPartName = "PART_LayoutBox";
    private const string SizeBoxPartName = "PART_SizeBox";
    private const string IconSectionPartName = "PART_IconSection";
    private const string LayoutSectionPartName = "PART_LayoutSection";
    private const string SizeSectionPartName = "PART_SizeSection";
    private const string OkButtonPartName = "PART_OkButton";
    private const string CancelButtonPartName = "PART_CancelButton";
    private const string CloseButtonPartName = "PART_CloseButton";

    private TextBox? _nameBox;
    private ListBox? _iconList;
    private Selector? _layoutBox;
    private Selector? _sizeBox;
    private ButtonBase? _okButton;
    private ButtonBase? _cancelButton;
    private ButtonBase? _closeButton;

    static RibbonCustomizeEditDialog()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(RibbonCustomizeEditDialog),
            new FrameworkPropertyMetadata(typeof(RibbonCustomizeEditDialog)));
    }

    /// <summary>Initializes the dialog with small-modal defaults (custom close-only chrome).</summary>
    public RibbonCustomizeEditDialog()
    {
        // Fixed width; the height is derived from which sections show (OnSourceInitialized).
        // NOT SizeToContent: it collapses the width to near-zero under WindowStyle=None +
        // WindowChrome (WPF measures the custom-chrome window wrong).
        Width = 460;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
    }

    /// <summary>The target's display name; edited in place. Whitespace-only edits are ignored.</summary>
    public string? ItemName { get; set; }

    /// <summary>Show the icon picker (custom groups).</summary>
    public bool CanEditIcon { get; set; }

    /// <summary>Show the layout choice (custom groups).</summary>
    public bool CanEditLayout { get; set; }

    /// <summary>Show the button-size choice (custom-group commands).</summary>
    public bool CanEditSize { get; set; }

    /// <summary>When the owning group's layout is Large: show the size as a locked "Large".</summary>
    public bool SizeLocked { get; set; }

    /// <summary>The icons offered by the picker (typically
    /// <c>RibbonCommandCatalog.CollectIcons</c>'s harvest); "no icon" is added automatically.</summary>
    public IReadOnlyList<ImageSource> IconChoices { get; set; } = Array.Empty<ImageSource>();

    /// <summary>In: the current icon. Out: the picked icon (<see langword="null"/> = none).</summary>
    public ImageSource? SelectedIcon { get; set; }

    /// <summary>In: the group's current layout. Out: the picked layout.</summary>
    public RibbonGroupLayout SelectedLayout { get; set; } = RibbonGroupLayout.Stacked;

    /// <summary>In: the command's current size. Out: the picked size.</summary>
    public RibbonControlSize SelectedSize { get; set; } = RibbonControlSize.Medium;

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e)
    {
        // Size to the sections the caller enabled (they're set before ShowDialog). A base
        // height covers the title bar, name field, and button bar; each optional section adds
        // its own. Done here (not SizeToContent) to dodge the chrome-measurement bug.
        Height = 172
            + (CanEditIcon ? 192 : 0)
            + (CanEditLayout ? 62 : 0)
            + (CanEditSize ? 62 : 0);

        base.OnSourceInitialized(e);
        MicaHelper.SetRoundedCorners(this, borderColor: 0x00E0E0E0);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        if (_okButton is not null)
        {
            _okButton.Click -= OnOkClick;
        }

        if (_cancelButton is not null)
        {
            _cancelButton.Click -= OnCancelClick;
        }

        if (_closeButton is not null)
        {
            _closeButton.Click -= OnCancelClick;
        }

        base.OnApplyTemplate();

        _nameBox = GetTemplateChild(NameBoxPartName) as TextBox;
        _iconList = GetTemplateChild(IconListPartName) as ListBox;
        _layoutBox = GetTemplateChild(LayoutBoxPartName) as Selector;
        _sizeBox = GetTemplateChild(SizeBoxPartName) as Selector;
        _okButton = GetTemplateChild(OkButtonPartName) as ButtonBase;
        _cancelButton = GetTemplateChild(CancelButtonPartName) as ButtonBase;
        _closeButton = GetTemplateChild(CloseButtonPartName) as ButtonBase;

        if (_okButton is not null)
        {
            _okButton.Click += OnOkClick;
        }

        if (_cancelButton is not null)
        {
            _cancelButton.Click += OnCancelClick;
        }

        if (_closeButton is not null)
        {
            _closeButton.Click += OnCancelClick;
        }

        PopulateFromProperties();
    }

    private void PopulateFromProperties()
    {
        if (_nameBox is not null)
        {
            _nameBox.Text = ItemName ?? string.Empty;
            _nameBox.SelectAll();
            _nameBox.Focus();
        }

        SetSectionVisible(IconSectionPartName, CanEditIcon);
        SetSectionVisible(LayoutSectionPartName, CanEditLayout);
        SetSectionVisible(SizeSectionPartName, CanEditSize);

        if (_iconList is not null && CanEditIcon)
        {
            var choices = new List<RibbonIconChoice> { new(null) };
            choices.AddRange(IconChoices.Select(icon => new RibbonIconChoice(icon)));
            _iconList.ItemsSource = choices;
            _iconList.SelectedItem =
                choices.FirstOrDefault(c => ReferenceEquals(c.Icon, SelectedIcon)) ?? choices[0];
        }

        if (_layoutBox is not null && CanEditLayout)
        {
            _layoutBox.ItemsSource = new[] { RibbonGroupLayout.Stacked, RibbonGroupLayout.Large };
            _layoutBox.SelectedItem =
                SelectedLayout == RibbonGroupLayout.Default ? RibbonGroupLayout.Stacked : SelectedLayout;
        }

        if (_sizeBox is not null && CanEditSize)
        {
            if (SizeLocked)
            {
                // The group's Large layout dictates the size — show it, but read-only.
                _sizeBox.ItemsSource = new[] { RibbonControlSize.Large };
                _sizeBox.SelectedIndex = 0;
                _sizeBox.IsEnabled = false;
            }
            else
            {
                _sizeBox.ItemsSource = new[] { RibbonControlSize.Medium, RibbonControlSize.Small };
                _sizeBox.SelectedItem =
                    SelectedSize == RibbonControlSize.Large ? RibbonControlSize.Medium : SelectedSize;
            }
        }
    }

    private void SetSectionVisible(string partName, bool visible)
    {
        if (GetTemplateChild(partName) is FrameworkElement section)
        {
            section.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        string? name = _nameBox?.Text?.Trim();
        if (!string.IsNullOrEmpty(name))
        {
            ItemName = name;
        }

        if (CanEditIcon && _iconList?.SelectedItem is RibbonIconChoice choice)
        {
            SelectedIcon = choice.Icon;
        }

        if (CanEditLayout && _layoutBox?.SelectedItem is RibbonGroupLayout layout)
        {
            SelectedLayout = layout;
        }

        if (CanEditSize && !SizeLocked && _sizeBox?.SelectedItem is RibbonControlSize size)
        {
            SelectedSize = size;
        }

        CloseWithResult(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => CloseWithResult(false);

    private void CloseWithResult(bool result)
    {
        try
        {
            DialogResult = result;
        }
        catch (InvalidOperationException)
        {
            Close(); // Shown modeless — just close.
        }
    }
}
