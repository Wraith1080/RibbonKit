using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Interop;
using RibbonKit.Theming;

namespace RibbonKit.Showcase;

/// <summary>
/// Main window of the showcase app, hosting the RibbonKit ribbon and demonstrating
/// gallery live preview: hovering a style previews it on the sample text, clicking
/// commits it, and leaving the gallery reverts to the committed style.
/// </summary>
public partial class MainWindow : RibbonWindow
{
    // Where this app persists the user's ribbon customizations between runs. A real app
    // would use its own product folder; LocalApplicationData keeps it per-user, per-machine.
    private static readonly string CustomizationFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RibbonKitShowcase",
        "ribbon-customization.json");

    private string _committedStyle = "Normal";

    // The factory ribbon captured at startup (before saved edits are applied) — the layout
    // the customize page's Reset button restores.
    private string? _baselineLayout;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnWindowLoaded; // One-shot: capture the baseline and restore once.

        // Capture the pristine ribbon FIRST — this is the Reset target, so it must be taken
        // before any saved customization is layered on.
        _baselineLayout = RibbonCustomizationSerializer.Serialize(MainRibbon);

        // Then restore the user's saved customizations, if the file exists. Apply tolerates a
        // missing/foreign/corrupt string, so a bad file just leaves the factory ribbon.
        try
        {
            if (File.Exists(CustomizationFile))
            {
                RibbonCustomizationSerializer.Apply(MainRibbon, File.ReadAllText(CustomizationFile));
            }
        }
        catch (IOException)
        {
            // Unreadable file — start from the factory ribbon.
        }
        catch (UnauthorizedAccessException)
        {
        }

        // Persist QAT edits made OUTSIDE the options dialog too: right-click "Add/Remove from
        // Quick Access Toolbar" mutates QuickAccessItems, and the placement menu changes
        // QuickAccessPosition — neither goes through the dialog's Apply. Subscribe AFTER the
        // restore above so replaying the saved state doesn't itself trigger a save.
        MainRibbon.QuickAccessItems.CollectionChanged += (_, _) => SaveCustomization();
        DependencyPropertyDescriptor
            .FromProperty(Ribbon.QuickAccessPositionProperty, typeof(Ribbon))
            ?.AddValueChanged(MainRibbon, (_, _) => SaveCustomization());
    }

    // Persists the ribbon's current customization; called when the options dialog applies.
    private void SaveCustomization()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CustomizationFile)!);
            File.WriteAllText(CustomizationFile, RibbonCustomizationSerializer.Serialize(MainRibbon));
        }
        catch (IOException)
        {
            // Best-effort persistence — a locked/unwritable file just isn't saved this time.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    // Demo: disable a plain button, a split button, and a whole group together so their disabled
    // (greyed, non-interactive) states are visible. Disabling a RibbonGroup cascades IsEnabled to
    // every control inside it — the whole Font group greys out from one flag.
    private void OnToggleDisableSamples(object sender, RoutedEventArgs e)
    {
        bool disabled = (sender as RibbonToggleButton)?.IsChecked == true;
        bool enabled = !disabled;

        if (DemoButtonTarget is not null)
        {
            DemoButtonTarget.IsEnabled = enabled;
        }

        if (DemoSplitTarget is not null)
        {
            DemoSplitTarget.IsEnabled = enabled;
        }

        if (DemoGroupTarget is not null)
        {
            DemoGroupTarget.IsEnabled = enabled;
        }
    }

    private void OnApplyOffice2024(object sender, RoutedEventArgs e) =>
        ThemeManager.Apply(Application.Current, RibbonTheme.Office2024);

    private void OnApplyOffice2019(object sender, RoutedEventArgs e) =>
        ThemeManager.Apply(Application.Current, RibbonTheme.Office2019);

    private void OnApplyOffice2013(object sender, RoutedEventArgs e) =>
        ThemeManager.Apply(Application.Current, RibbonTheme.Office2013);

    private void OnApplyOffice2010(object sender, RoutedEventArgs e) =>
        ThemeManager.Apply(Application.Current, RibbonTheme.Office2010);

    private void OnToggleAccentTitleBar(object sender, RoutedEventArgs e) =>
        ThemeManager.SetAccentedTitleBar(Application.Current, (sender as RibbonToggleButton)?.IsChecked == true);

    private void OnAccentGalleryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AccentGallery is null || GetStyleTag(AccentGallery.SelectedItem) is not { } tag)
        {
            return;
        }

        if (tag == "reset")
        {
            ThemeManager.ClearAccent(Application.Current);
        }
        else if (ColorConverter.ConvertFromString(tag) is Color color)
        {
            ThemeManager.SetAccent(Application.Current, color);
        }
    }

    private void OnAnimationOff(object sender, RoutedEventArgs e) =>
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;

    private void OnAnimationSubtle(object sender, RoutedEventArgs e) =>
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;

    private void OnAnimationExpressive(object sender, RoutedEventArgs e) =>
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Expressive;

    private void OnToggleRespectSystemMotion(object sender, RoutedEventArgs e) =>
        RibbonAnimation.RespectSystemReduceMotion = (sender as RibbonToggleButton)?.IsChecked == true;

    // Pick one of the three backstage designs (Classic 2013 / Modern 2024 / Classic2010 glass)
    // from the button's Tag. Open the File menu to see the change.
    private void OnSelectBackstageDesign(object sender, RoutedEventArgs e)
    {
        if (ShowcaseBackstage is not null
            && (sender as RibbonButton)?.Tag is string tag
            && Enum.TryParse(tag, out RibbonBackstageDesign design))
        {
            ShowcaseBackstage.Design = design;
        }
    }

    // Frosted-acrylic backstage: turn it semi-transparent and let the Ribbon strongly blur the
    // content behind it. Takes effect the next time the File menu opens.
    private void OnToggleBackstageTranslucent(object sender, RoutedEventArgs e)
    {
        if (ShowcaseBackstage is not null)
        {
            ShowcaseBackstage.Translucent = (sender as RibbonToggleButton)?.IsChecked == true;
        }
    }

    private Brush? _opaqueWindowBackground;

    private void OnToggleMica(object sender, RoutedEventArgs e)
    {
        var toggle = sender as RibbonToggleButton;

        if (toggle?.IsChecked == true)
        {
            // Mica needs Windows 11 22H2+. If the DWM rejects it, undo the toggle and stop.
            if (!MicaHelper.TrySetBackdrop(this, RibbonBackdrop.Mica))
            {
                toggle.IsChecked = false;
                return;
            }

            // The backdrop only composites where the DWM glass reaches: without extending
            // the glass across the client area, a transparent window renders BLACK.
            MicaHelper.ExtendGlassFrame(this, full: true);

            // Strip WS_SYSMENU so the DWM's native min/max/close buttons don't show through a
            // transparent title bar and overlap our custom caption buttons.
            MicaHelper.ShowNativeCaptionButtons(this, false);

            _opaqueWindowBackground ??= Background;
            Background = Brushes.Transparent;
            MainContentArea.Background = Brushes.Transparent;

            // Let the title bar go transparent so Mica shows through it — but only for the 2024
            // look with a non-colored title bar (ThemeManager enforces that rule and re-derives
            // it across theme/accent changes, so switching themes no longer reverts it).
            ThemeManager.SetTitleBarBackdrop(Application.Current, true);

            // Backstage stays opaque (Translucent left false): it fully covers the content
            // behind it, so Mica shows in the title bar / ribbon chrome but does not bleed
            // through the backstage page.
        }
        else
        {
            MicaHelper.TrySetBackdrop(this, RibbonBackdrop.None);

            // Do NOT collapse the glass frame here. The RibbonWindow template keeps
            // GlassFrameThickness="-1" as its resting state, and that extended glass is what
            // gives the window its Windows 11 border and rounded corners (WindowChrome strips
            // the native non-client frame, so the DWM glass is all that renders them).
            // Collapsing it to 0 destroys the border/corners — and there's no black-background
            // risk to avoid, because the visible content is opaque white again below.
            MicaHelper.ShowNativeCaptionButtons(this, true);
            ThemeManager.SetTitleBarBackdrop(Application.Current, false);
            Background = _opaqueWindowBackground ?? Brushes.White;
            MainContentArea.Background = Brushes.White;
        }
    }

    private enum OptionsPageKind
    {
        Editor,
        CustomizeRibbon,
        QuickAccess,
    }

    private void OnOpenOptions(object sender, RoutedEventArgs e) =>
        OpenOptionsDialog(OptionsPageKind.Editor);

    // Backstage footer BUTTON items: they run an action instead of switching to a page.
    private void OnBackstageOptions(object sender, RoutedEventArgs e)
    {
        MainRibbon.IsBackstageOpen = false; // leave the backstage, like Word
        OpenOptionsDialog(OptionsPageKind.Editor);
    }

    private void OnBackstageExit(object sender, RoutedEventArgs e) => Close();

    // Raised by the ribbon's right-click "Customize Quick Access Toolbar…" items.
    private void OnCustomizeQuickAccess(object sender, EventArgs e) =>
        OpenOptionsDialog(OptionsPageKind.QuickAccess);

    // Raised by the ribbon's right-click "Customize the Ribbon…" item.
    private void OnCustomizeRibbon(object sender, EventArgs e) =>
        OpenOptionsDialog(OptionsPageKind.CustomizeRibbon);

    /// <summary>
    /// The merged options dialog: an app-provided page ("Editor") next to RibbonKit's
    /// built-in Customize Ribbon and Quick Access Toolbar pages — one dialog, like Word.
    /// </summary>
    private void OpenOptionsDialog(OptionsPageKind select)
    {
        var editorPage = new RibbonOptionsPage { Header = "Editor", Content = BuildEditorOptionsContent() };
        var customizePage = new RibbonOptionsPage
        {
            Header = "Customize Ribbon",
            Content = new RibbonCustomizePage { Ribbon = MainRibbon, ResetLayout = _baselineLayout },
        };
        var qatPage = new RibbonOptionsPage
        {
            Header = "Quick Access Toolbar",
            Content = new RibbonQuickAccessPage { Ribbon = MainRibbon },
        };

        var dialog = new RibbonOptionsDialog { Title = "Options", Owner = this };
        dialog.Pages.Add(editorPage);
        dialog.Pages.Add(customizePage);
        dialog.Pages.Add(qatPage);
        dialog.SelectedPage = select switch
        {
            OptionsPageKind.CustomizeRibbon => customizePage,
            OptionsPageKind.QuickAccess => qatPage,
            _ => editorPage,
        };

        // The dialog raises Applied on OK — the app's cue to persist its settings (the
        // customization pages edit the ribbon live). ShowDialog's bool result carries the
        // same decision.
        dialog.Applied += (_, _) =>
        {
            SaveCustomization();
            StatusReady.Content = "Options applied";
        };
        dialog.ShowDialog();
    }

    /// <summary>Stand-in for an application options page — any UserControl works here.</summary>
    private static object BuildEditorOptionsContent()
    {
        var panel = new StackPanel { MaxWidth = 460, HorizontalAlignment = HorizontalAlignment.Left };
        panel.Children.Add(new TextBlock
        {
            Text = "An app-provided page living in the same dialog as RibbonKit's customization pages — add any UserControl as a RibbonOptionsPage's content.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61)),
            Margin = new Thickness(0, 0, 0, 14),
        });
        panel.Children.Add(new CheckBox { Content = "Check spelling as you type", IsChecked = true, Margin = new Thickness(0, 0, 0, 6) });
        panel.Children.Add(new CheckBox { Content = "Autosave every 10 minutes", IsChecked = true, Margin = new Thickness(0, 0, 0, 6) });
        panel.Children.Add(new CheckBox { Content = "Show formatting marks" });
        return panel;
    }

    // ---- Font dialog (Font group ↘ launcher) -------------------------------------------

    // Raised by the Font group's ↘ dialog launcher (RibbonGroup.DialogLauncherClick). Opens a
    // Word-style Font dialog seeded from the editor's current selection and, on OK, applies the
    // chosen family / style / size back to that selection.
    private void OnFontDialogLauncher(object sender, RoutedEventArgs e)
    {
        TextSelection sel = DocumentEditor.Selection;

        // Seed from the selection's current run properties. A selection that spans mixed values
        // returns UnsetValue for that property, so fall back to the editor's own value.
        FontFamily family = sel.GetPropertyValue(TextElement.FontFamilyProperty) as FontFamily ?? DocumentEditor.FontFamily;
        double sizePx = sel.GetPropertyValue(TextElement.FontSizeProperty) is double sz ? sz : DocumentEditor.FontSize;
        FontWeight weight = sel.GetPropertyValue(TextElement.FontWeightProperty) is FontWeight fw ? fw : FontWeights.Normal;
        FontStyle style = sel.GetPropertyValue(TextElement.FontStyleProperty) is FontStyle fs ? fs : FontStyles.Normal;

        // --- Pickers ---------------------------------------------------------------------
        var families = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();
        var familyBox = new ComboBox
        {
            IsEditable = true,
            IsTextSearchEnabled = true,
            DisplayMemberPath = "Source",
            ItemsSource = families,
            MaxDropDownHeight = 240,
        };
        familyBox.SelectedItem = families.FirstOrDefault(f => f.Source == family.Source);
        if (familyBox.SelectedItem is null)
        {
            familyBox.Text = family.Source;
        }

        var styleBox = new ComboBox { ItemsSource = new[] { "Regular", "Italic", "Bold", "Bold Italic" } };
        styleBox.SelectedItem = DescribeFontStyle(weight, style);

        var sizeBox = new ComboBox
        {
            IsEditable = true,
            ItemsSource = new[] { "8", "9", "10", "11", "12", "14", "16", "18", "20", "24", "28", "36", "48", "72" },
            Text = Math.Round(sizePx * 72.0 / 96.0).ToString(CultureInfo.InvariantCulture),
        };

        var preview = new TextBlock
        {
            Text = "AaBbYyZz  —  The quick brown fox 0123",
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        void UpdatePreview()
        {
            FontFamily? f = familyBox.SelectedItem as FontFamily ?? SafeFontFamily(familyBox.Text);
            if (f is not null)
            {
                preview.FontFamily = f;
            }

            (FontWeight w, FontStyle st) = ParseFontStyle(styleBox.SelectedItem as string);
            preview.FontWeight = w;
            preview.FontStyle = st;

            if (double.TryParse(sizeBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double pt) && pt > 0)
            {
                preview.FontSize = pt * 96.0 / 72.0;
            }
        }

        familyBox.SelectionChanged += (_, _) => UpdatePreview();
        familyBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => UpdatePreview()));
        styleBox.SelectionChanged += (_, _) => UpdatePreview();
        sizeBox.SelectionChanged += (_, _) => UpdatePreview();
        sizeBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler((_, _) => UpdatePreview()));
        UpdatePreview();

        // --- Layout ----------------------------------------------------------------------
        static StackPanel Labeled(string label, UIElement control)
        {
            var sp = new StackPanel { Margin = new Thickness(0, 0, 10, 0) };
            sp.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 4) });
            sp.Children.Add(control);
            return sp;
        }

        var pickers = new Grid();
        pickers.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pickers.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pickers.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(72) });
        var familyCell = Labeled("Font:", familyBox);
        var styleCell = Labeled("Font style:", styleBox);
        var sizeCell = Labeled("Size:", sizeBox);
        sizeCell.Margin = new Thickness(0);
        Grid.SetColumn(styleCell, 1);
        Grid.SetColumn(sizeCell, 2);
        pickers.Children.Add(familyCell);
        pickers.Children.Add(styleCell);
        pickers.Children.Add(sizeCell);

        var previewBox = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0xD1, 0xD1)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(3),
            Background = new SolidColorBrush(Color.FromRgb(0xFA, 0xFA, 0xFA)),
            Margin = new Thickness(0, 16, 0, 0),
            MinHeight = 96,
            Child = preview,
        };

        var okButton = new Button { Content = "OK", Width = 84, Height = 26, IsDefault = true, Margin = new Thickness(0, 0, 8, 0) };
        var cancelButton = new Button { Content = "Cancel", Width = 84, Height = 26, IsCancel = true };
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0),
        };
        buttons.Children.Add(okButton);
        buttons.Children.Add(cancelButton);

        var root = new Grid { Margin = new Thickness(16) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Grid.SetRow(previewBox, 1);
        Grid.SetRow(buttons, 2);
        root.Children.Add(pickers);
        root.Children.Add(previewBox);
        root.Children.Add(buttons);

        var win = new Window
        {
            Title = "Font",
            Owner = this,
            Width = 460,
            Height = 340,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Background = Brushes.White,
            Content = root,
        };
        okButton.Click += (_, _) => win.DialogResult = true;
        cancelButton.Click += (_, _) => win.DialogResult = false;

        if (win.ShowDialog() != true)
        {
            return;
        }

        // --- Apply to the selection ------------------------------------------------------
        FontFamily chosen = familyBox.SelectedItem as FontFamily ?? SafeFontFamily(familyBox.Text) ?? family;
        (FontWeight cw, FontStyle cs) = ParseFontStyle(styleBox.SelectedItem as string);
        double chosenPt = double.TryParse(sizeBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double p) && p > 0
            ? p
            : sizePx * 72.0 / 96.0;

        sel.ApplyPropertyValue(TextElement.FontFamilyProperty, chosen);
        sel.ApplyPropertyValue(TextElement.FontWeightProperty, cw);
        sel.ApplyPropertyValue(TextElement.FontStyleProperty, cs);
        sel.ApplyPropertyValue(TextElement.FontSizeProperty, chosenPt * 96.0 / 72.0);

        DocumentEditor.Focus();
        StatusReady.Content = $"Font: {chosen.Source}, {DescribeFontStyle(cw, cs)}, {chosenPt:0.#} pt";
    }

    // "Bold Italic" / "Bold" / "Italic" / "Regular" from a weight + style pair.
    private static string DescribeFontStyle(FontWeight weight, FontStyle style)
    {
        bool bold = weight.ToOpenTypeWeight() >= FontWeights.Bold.ToOpenTypeWeight();
        bool italic = style != FontStyles.Normal;
        return (bold, italic) switch
        {
            (true, true) => "Bold Italic",
            (true, false) => "Bold",
            (false, true) => "Italic",
            _ => "Regular",
        };
    }

    private static (FontWeight Weight, FontStyle Style) ParseFontStyle(string? name) => name switch
    {
        "Bold Italic" => (FontWeights.Bold, FontStyles.Italic),
        "Bold" => (FontWeights.Bold, FontStyles.Normal),
        "Italic" => (FontWeights.Normal, FontStyles.Italic),
        _ => (FontWeights.Normal, FontStyles.Normal),
    };

    // A typed family name may be blank or invalid; return null rather than throwing.
    private static FontFamily? SafeFontFamily(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        try
        {
            return new FontFamily(name);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void OnPictureSelected(object sender, RoutedEventArgs e)
    {
        if (PictureFormatTab is not null)
        {
            PictureFormatTab.Visibility = Visibility.Visible;
        }
    }

    private void OnPictureDeselected(object sender, RoutedEventArgs e)
    {
        if (PictureFormatTab is not null)
        {
            PictureFormatTab.Visibility = Visibility.Collapsed;
        }
    }

    private void OnStylePreview(object sender, RibbonGalleryPreviewEventArgs e)
    {
        if (GetStyleTag(e.PreviewedItem) is { } tag)
        {
            ApplyStyleToSample(tag);
        }
    }

    private void OnStylePreviewCancelled(object sender, RoutedEventArgs e)
    {
        ApplyStyleToSample(_committedStyle);
    }

    private void OnStyleCommitted(object sender, SelectionChangedEventArgs e)
    {
        // SelectionChanged fires during InitializeComponent (SelectedIndex="0" in
        // XAML) — before later-declared elements exist. Guard against that.
        if (StylesGallery is null)
        {
            return;
        }

        if (GetStyleTag(StylesGallery.SelectedItem) is { } tag)
        {
            _committedStyle = tag;
            ApplyStyleToSample(tag);
        }
    }

    private static string? GetStyleTag(object? item) =>
        (item as FrameworkElement)?.Tag as string;

    private void ApplyStyleToSample(string style)
    {
        // Events can fire during XAML parse, before this element is constructed.
        if (StylePreviewText is null)
        {
            return;
        }

        // Reset to the Normal baseline, then apply the style's deltas.
        StylePreviewText.FontSize = 14;
        StylePreviewText.FontWeight = FontWeights.Normal;
        StylePreviewText.FontStyle = FontStyles.Normal;
        StylePreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));

        switch (style)
        {
            case "Heading 1":
                StylePreviewText.FontSize = 20;
                StylePreviewText.FontWeight = FontWeights.SemiBold;
                StylePreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x0F, 0x6C, 0xBD));
                break;
            case "Heading 2":
                StylePreviewText.FontSize = 17;
                StylePreviewText.FontWeight = FontWeights.SemiBold;
                StylePreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x2B, 0x7F, 0xC7));
                break;
            case "Title":
                StylePreviewText.FontSize = 26;
                StylePreviewText.FontWeight = FontWeights.Light;
                break;
            case "Subtitle":
                StylePreviewText.FontSize = 15;
                StylePreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61));
                break;
            case "Quote":
                StylePreviewText.FontStyle = FontStyles.Italic;
                StylePreviewText.Foreground = new SolidColorBrush(Color.FromRgb(0x61, 0x61, 0x61));
                break;
            case "Strong":
                StylePreviewText.FontWeight = FontWeights.Bold;
                break;
            case "Emphasis":
                StylePreviewText.FontStyle = FontStyles.Italic;
                break;
        }
    }
}
