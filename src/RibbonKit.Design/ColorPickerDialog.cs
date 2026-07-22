using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RibbonKit.Design;

/// <summary>
/// A small self-contained color chooser: a palette of standard/Office swatches plus a hex box for
/// anything else, with a live preview. Returns the chosen color as a string (a <c>#RRGGBB</c> hex or a
/// named color) in <see cref="SelectedColor"/> (null when cancelled). Self-contained WPF — no WinForms
/// dependency, so the design assembly stays a plain WPF library.
/// </summary>
internal sealed class ColorPickerDialog : Window
{
    private static readonly string[] Palette =
    {
        // Standard bright
        "#C00000", "#FF0000", "#FFC000", "#FFFF00", "#92D050", "#00B050", "#00B0F0", "#0070C0", "#002060", "#7030A0",
        // Grayscale
        "#FFFFFF", "#F2F2F2", "#D9D9D9", "#BFBFBF", "#808080", "#595959", "#404040", "#262626", "#0D0D0D", "#000000",
        // Office 2024-ish accents
        "#0F6CBD", "#107C41", "#C43E1C", "#5A6270", "#616161", "#8764B8", "#CA5010", "#498205", "#E3008C", "#00B7C3",
    };

    private readonly TextBox _hex;
    private readonly Border _preview;

    public ColorPickerDialog(string current)
    {
        Title = "Pick Color";
        Width = 340;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;

        _hex = new TextBox { Text = current ?? string.Empty, MinWidth = 120, VerticalContentAlignment = VerticalAlignment.Center };
        _preview = new Border { Width = 26, Height = 26, BorderThickness = new Thickness(1), BorderBrush = SystemColors.ActiveBorderBrush };

        Content = BuildLayout();
        UpdatePreview();
    }

    /// <summary>The chosen color string, or null when cancelled.</summary>
    public string SelectedColor { get; private set; }

    private UIElement BuildLayout()
    {
        var root = new StackPanel { Margin = new Thickness(12) };

        var swatches = new WrapPanel { MaxWidth = 316 };
        foreach (string hex in Palette)
        {
            swatches.Children.Add(MakeSwatch(hex));
        }

        root.Children.Add(swatches);

        var hexRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        hexRow.Children.Add(new TextBlock { Text = "Hex / name: ", VerticalAlignment = VerticalAlignment.Center });
        _hex.TextChanged += (_, _) => UpdatePreview();
        hexRow.Children.Add(_hex);
        hexRow.Children.Add(new Border { Child = _preview, Margin = new Thickness(8, 0, 0, 0) });
        root.Children.Add(hexRow);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
        var ok = new Button { Content = "OK", MinWidth = 76, Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(10, 4, 10, 4), IsDefault = true };
        ok.Click += (_, _) =>
        {
            string value = _hex.Text?.Trim();
            if (!string.IsNullOrEmpty(value))
            {
                SelectedColor = value;
                DialogResult = true;
            }
        };
        var cancel = new Button { Content = "Cancel", MinWidth = 76, Padding = new Thickness(10, 4, 10, 4), IsCancel = true };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        root.Children.Add(buttons);

        return root;
    }

    private Button MakeSwatch(string hex)
    {
        var button = new Button
        {
            Width = 26,
            Height = 26,
            Margin = new Thickness(2),
            Background = ParseBrush(hex),
            BorderBrush = SystemColors.ActiveBorderBrush,
            BorderThickness = new Thickness(1),
            ToolTip = hex,
        };
        button.Click += (_, _) =>
        {
            SelectedColor = hex;
            DialogResult = true;
        };
        return button;
    }

    private void UpdatePreview() => _preview.Background = ParseBrush(_hex.Text);

    /// <summary>Parses a color string ("#RRGGBB" or a named color) to a brush, or transparent when invalid.</summary>
    internal static Brush ParseBrush(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Brushes.Transparent;
        }

        try
        {
            return (Brush)new BrushConverter().ConvertFromString(text.Trim());
        }
        catch
        {
            return Brushes.Transparent;
        }
    }
}
