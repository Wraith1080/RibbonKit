using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace RibbonKit.Design;

/// <summary>
/// Session cache of an icon <see cref="ResourceDictionary"/> the user has pointed the picker at
/// (their project's Icons.xaml). The design extension can't auto-discover that file — resources
/// live in the isolated design-surface process and there's no reliable document-path service — so
/// the user browses to it once and it's remembered for the rest of the session. Parsing happens in
/// the extension's own WPF context, so the <c>DrawingImage</c> values render as real thumbnails.
/// </summary>
internal static class IconCatalog
{
    /// <summary>The loaded dictionary, or null until the user loads one this session.</summary>
    public static ResourceDictionary Loaded { get; private set; }

    /// <summary>The path the current <see cref="Loaded"/> dictionary came from (for display).</summary>
    public static string LoadedPath { get; private set; }

    /// <summary>Loads a ResourceDictionary XAML file (e.g. Icons.xaml). Returns false with a message on failure.</summary>
    public static bool TryLoad(string path, out string error)
    {
        error = null;
        try
        {
            using (System.IO.FileStream stream = System.IO.File.OpenRead(path))
            {
                if (XamlReader.Load(stream) is ResourceDictionary dictionary)
                {
                    Loaded = dictionary;
                    LoadedPath = path;
                    return true;
                }
            }

            error = "That file isn't a ResourceDictionary.";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            DesignLog.Error("IconCatalog.TryLoad " + path, ex);
            return false;
        }
    }

    /// <summary>The string resource keys in the loaded dictionary (empty when none loaded).</summary>
    public static IEnumerable<string> Keys()
    {
        if (Loaded is null)
        {
            yield break;
        }

        foreach (object key in Loaded.Keys)
        {
            if (key is string text)
            {
                yield return text;
            }
        }
    }

    /// <summary>The <see cref="ImageSource"/> for a key, or null if it isn't an image.</summary>
    public static ImageSource Preview(string key)
    {
        if (Loaded is null)
        {
            return null;
        }

        try
        {
            return Loaded[key] as ImageSource;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// A modal icon chooser. Shows a grid of clickable tiles: from a loaded Icons.xaml (with rendered
/// previews) when available, otherwise the icon keys already used elsewhere in this ribbon (text
/// tiles — always available with no file). A filter box narrows the list, and "Load Icons.xaml…"
/// browses to the full catalog. <see cref="SelectedKey"/> holds the chosen key (null if cancelled).
/// </summary>
internal sealed class IconPickerDialog : Window
{
    private readonly List<string> _usedKeys;
    private readonly string _currentKey;
    private readonly WrapPanel _tiles = new WrapPanel();
    private readonly TextBox _filter = new TextBox { MinWidth = 160, VerticalContentAlignment = VerticalAlignment.Center };
    private readonly TextBlock _status = new TextBlock { Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };

    public IconPickerDialog(IEnumerable<string> usedKeys, string currentKey)
    {
        _usedKeys = (usedKeys ?? Enumerable.Empty<string>())
            .Where(k => !string.IsNullOrEmpty(k))
            .Distinct()
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _currentKey = currentKey;

        Title = "Choose Icon";
        Width = 540;
        Height = 540;
        MinWidth = 380;
        MinHeight = 320;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;

        Content = BuildLayout();
        RebuildTiles();
    }

    /// <summary>The chosen resource key, or null when the dialog was cancelled.</summary>
    public string SelectedKey { get; private set; }

    private UIElement BuildLayout()
    {
        var grid = new Grid { Margin = new Thickness(10) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                 // toolbar
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // tiles
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                 // footer

        // Toolbar: filter + load button.
        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        toolbar.Children.Add(new TextBlock { Text = "Filter: ", VerticalAlignment = VerticalAlignment.Center });
        _filter.TextChanged += (_, _) => RebuildTiles();
        toolbar.Children.Add(_filter);
        var load = new Button
        {
            Content = "Load Icons.xaml…",
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(10, 3, 10, 3),
        };
        load.Click += (_, _) => OnLoad();
        toolbar.Children.Add(load);
        Grid.SetRow(toolbar, 0);
        grid.Children.Add(toolbar);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _tiles,
            BorderThickness = new Thickness(1),
            BorderBrush = SystemColors.ActiveBorderBrush,
            Padding = new Thickness(4),
        };
        Grid.SetRow(scroll, 1);
        grid.Children.Add(scroll);

        var footer = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(_status, 0);
        footer.Children.Add(_status);
        var cancel = new Button { Content = "Cancel", MinWidth = 84, Padding = new Thickness(10, 4, 10, 4), IsCancel = true };
        Grid.SetColumn(cancel, 1);
        footer.Children.Add(cancel);
        Grid.SetRow(footer, 2);
        grid.Children.Add(footer);

        return grid;
    }

    private void OnLoad()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Load an icon ResourceDictionary (e.g. Icons.xaml)",
            Filter = "XAML resource dictionary (*.xaml)|*.xaml|All files (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) == true)
        {
            if (IconCatalog.TryLoad(dialog.FileName, out string error))
            {
                _filter.Clear();
                RebuildTiles();
            }
            else
            {
                MessageBox.Show(this, "Couldn't load that file:\n\n" + error, "Choose Icon",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void RebuildTiles()
    {
        _tiles.Children.Clear();
        string filter = _filter.Text?.Trim() ?? string.Empty;

        bool Matches(string key) =>
            filter.Length == 0 || key.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;

        if (IconCatalog.Loaded != null)
        {
            List<string> keys = IconCatalog.Keys().Where(Matches).OrderBy(k => k, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (string key in keys)
            {
                _tiles.Children.Add(MakeTile(key, IconCatalog.Preview(key)));
            }

            _status.Text = keys.Count + " of " + IconCatalog.Keys().Count() + " icons — " + IconCatalog.LoadedPath;
        }
        else
        {
            List<string> keys = _usedKeys.Where(Matches).ToList();
            foreach (string key in keys)
            {
                _tiles.Children.Add(MakeTile(key, null));
            }

            _status.Text = _usedKeys.Count == 0
                ? "No icons in use yet. Click “Load Icons.xaml…” to browse all icons with previews."
                : "Icons used in this ribbon. Click “Load Icons.xaml…” to browse all with previews.";
        }
    }

    private Button MakeTile(string key, ImageSource preview)
    {
        var content = new StackPanel { Width = 96, Margin = new Thickness(4) };

        if (preview != null)
        {
            content.Children.Add(new Image
            {
                Source = preview,
                Width = 32,
                Height = 32,
                Margin = new Thickness(0, 2, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Center,
            });
        }

        content.Children.Add(new TextBlock
        {
            Text = key,
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center,
        });

        var tile = new Button
        {
            Content = content,
            Margin = new Thickness(2),
            Padding = new Thickness(2),
            ToolTip = key,
        };

        if (string.Equals(key, _currentKey, StringComparison.Ordinal))
        {
            tile.BorderBrush = SystemColors.HighlightBrush;
            tile.BorderThickness = new Thickness(2);
        }

        tile.Click += (_, _) =>
        {
            SelectedKey = key;
            DialogResult = true; // closes the modal
        };
        return tile;
    }
}
