using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

namespace RibbonKit.Design;

/// <summary>
/// Session cache of an icon <see cref="ResourceDictionary"/> from the active project. The catalog
/// first makes a conservative best-effort search for Icons.xaml beside/within the active XAML
/// project's directory, using the current Visual Studio automation object only to locate that
/// document. The browse button remains the fallback when discovery is unavailable or ambiguous.
/// Parsing happens in the extension's own WPF context, so <c>DrawingImage</c> values render as real
/// thumbnails.
/// </summary>
internal static class IconCatalog
{
    private static string? _lastDiscoveryContext;

    /// <summary>The loaded dictionary, or null until the user loads one this session.</summary>
    public static ResourceDictionary? Loaded { get; private set; }

    /// <summary>The path the current <see cref="Loaded"/> dictionary came from (for display).</summary>
    public static string? LoadedPath { get; private set; }

    /// <summary>Whether the current dictionary was found automatically rather than browsed to.</summary>
    public static bool AutomaticallyLoaded { get; private set; }

    /// <summary>Why automatic discovery did not load a catalog; null after a successful load.</summary>
    public static string? DiscoveryMessage { get; private set; }

    /// <summary>Loads a ResourceDictionary XAML file (e.g. Icons.xaml). Returns false with a message on failure.</summary>
    public static bool TryLoad(string path, out string? error)
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
                    AutomaticallyLoaded = false;
                    DiscoveryMessage = null;
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

    /// <summary>
    /// Tries to load a single unambiguous Icons.xaml from the active XAML project. Failure is silent
    /// and leaves the explicit browse workflow available.
    /// </summary>
    public static void TryAutoDiscover()
    {
        if (Loaded != null)
        {
            return;
        }

        VisualStudioPaths.TryGet(out string? activeDocumentPath, out string? solutionPath);
        string context = (activeDocumentPath ?? string.Empty) + "|" + (solutionPath ?? string.Empty);
        if (string.Equals(context, _lastDiscoveryContext, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _lastDiscoveryContext = context;

        try
        {
            string? path = IconCatalogDiscovery.FindSingle(activeDocumentPath, solutionPath, out string message);
            if (path == null)
            {
                DiscoveryMessage = message;
                return;
            }

            if (TryLoad(path, out string? error))
            {
                AutomaticallyLoaded = true;
                DiscoveryMessage = null;
                DesignLog.Write("IconCatalog: automatically loaded " + path);
            }
            else
            {
                DiscoveryMessage = "Automatic Icons.xaml load failed; use Load Icons.xaml…. " + error;
            }
        }
        catch (Exception ex)
        {
            DiscoveryMessage = "Icons.xaml wasn't found automatically; use Load Icons.xaml….";
            DesignLog.Error("IconCatalog.TryAutoDiscover", ex);
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
    public static ImageSource? Preview(string key)
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
        IconCatalog.TryAutoDiscover();
        RebuildTiles();
    }

    /// <summary>The chosen resource key, or null when the dialog was cancelled.</summary>
    public string? SelectedKey { get; private set; }

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
            if (IconCatalog.TryLoad(dialog.FileName, out string? error))
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

            _status.Text = keys.Count + " of " + IconCatalog.Keys().Count() + " icons — "
                + (IconCatalog.AutomaticallyLoaded ? "auto-loaded " : string.Empty)
                + IconCatalog.LoadedPath;
        }
        else
        {
            List<string> keys = _usedKeys.Where(Matches).ToList();
            foreach (string key in keys)
            {
                _tiles.Children.Add(MakeTile(key, null));
            }

            string guidance = _usedKeys.Count == 0
                ? "No icons in use yet. Click “Load Icons.xaml…” to browse all icons with previews."
                : "Icons used in this ribbon. Click “Load Icons.xaml…” to browse all with previews.";
            _status.Text = string.IsNullOrEmpty(IconCatalog.DiscoveryMessage)
                ? guidance
                : IconCatalog.DiscoveryMessage + " " + guidance;
        }
    }

    private Button MakeTile(string key, ImageSource? preview)
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

/// <summary>Locates a single Icons.xaml without depending on Visual Studio SDK interop assemblies.</summary>
internal static class IconCatalogDiscovery
{
    private static readonly HashSet<string> ExcludedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".vs", "artifacts", "bin", "node_modules", "obj", "packages", "TestResults",
    };

    public static string? FindSingle(string? activeDocumentPath, string? solutionPath, out string message)
    {
        string? solutionDirectory = DirectoryOfExistingFile(solutionPath);
        DirectoryInfo? documentDirectory = DirectoryInfoOfExistingFile(activeDocumentPath);
        DirectoryInfo? projectDirectory = null;

        for (DirectoryInfo? current = documentDirectory; current != null; current = current.Parent)
        {
            string besideDocument = Path.Combine(current.FullName, "Icons.xaml");
            if (File.Exists(besideDocument))
            {
                message = string.Empty;
                return besideDocument;
            }

            if (projectDirectory == null && ContainsProjectFile(current.FullName))
            {
                projectDirectory = current;
                break;
            }

            if (solutionDirectory != null && PathsEqual(current.FullName, solutionDirectory))
            {
                break;
            }
        }

        if (projectDirectory != null)
        {
            List<string> projectMatches = FindInTree(projectDirectory.FullName, 2);
            if (projectMatches.Count == 1)
            {
                message = string.Empty;
                return projectMatches[0];
            }

            if (projectMatches.Count > 1)
            {
                message = "Multiple Icons.xaml files were found in the active project; use Load Icons.xaml….";
                return null;
            }
        }

        if (solutionDirectory != null)
        {
            List<string> solutionMatches = FindInTree(solutionDirectory, 2);
            if (solutionMatches.Count == 1)
            {
                message = string.Empty;
                return solutionMatches[0];
            }

            if (solutionMatches.Count > 1)
            {
                message = "Multiple Icons.xaml files were found in the solution; use Load Icons.xaml….";
                return null;
            }
        }

        message = "Icons.xaml wasn't found automatically; use Load Icons.xaml….";
        return null;
    }

    private static string? DirectoryOfExistingFile(string? path)
    {
        return DirectoryInfoOfExistingFile(path)?.FullName;
    }

    private static DirectoryInfo? DirectoryInfoOfExistingFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        try
        {
            string? directory = Path.GetDirectoryName(path);
            return string.IsNullOrEmpty(directory) || !Directory.Exists(directory)
                ? null
                : new DirectoryInfo(directory);
        }
        catch
        {
            return null;
        }
    }

    private static bool ContainsProjectFile(string directory)
    {
        try
        {
            return Directory.EnumerateFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly).Any();
        }
        catch
        {
            return false;
        }
    }

    private static List<string> FindInTree(string root, int limit)
    {
        var matches = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count != 0 && matches.Count < limit)
        {
            string directory = pending.Pop();
            try
            {
                matches.AddRange(Directory.EnumerateFiles(directory, "Icons.xaml", SearchOption.TopDirectoryOnly)
                    .Take(limit - matches.Count));

                foreach (string child in Directory.EnumerateDirectories(directory))
                {
                    var info = new DirectoryInfo(child);
                    if (!ExcludedDirectories.Contains(info.Name)
                        && (info.Attributes & FileAttributes.ReparsePoint) == 0)
                    {
                        pending.Push(child);
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return matches;
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Reads the active document and solution paths from the DTE object registered for this exact
/// Visual Studio process. Reflection avoids adding an EnvDTE deployment dependency.
/// </summary>
internal static class VisualStudioPaths
{
    public static void TryGet(out string? activeDocumentPath, out string? solutionPath)
    {
        activeDocumentPath = null;
        solutionPath = null;

        try
        {
            object? dte = GetCurrentProcessDte();
            if (dte == null)
            {
                return;
            }

            object? document = ReadProperty(dte, "ActiveDocument");
            activeDocumentPath = ReadString(document, "FullName") ?? ReadString(document, "FileName");
            object? solution = ReadProperty(dte, "Solution");
            solutionPath = ReadString(solution, "FullName") ?? ReadString(solution, "FileName");
        }
        catch (Exception ex)
        {
            DesignLog.Error("VisualStudioPaths.TryGet", ex);
        }
    }

    private static object? GetCurrentProcessDte()
    {
        if (GetRunningObjectTable(0, out IRunningObjectTable table) != 0)
        {
            return null;
        }

        table.EnumRunning(out IEnumMoniker monikers);
        monikers.Reset();
        var current = new IMoniker[1];
        string processSuffix = ":" + Process.GetCurrentProcess().Id;

        while (monikers.Next(1, current, IntPtr.Zero) == 0)
        {
            try
            {
                if (CreateBindCtx(0, out IBindCtx bindContext) != 0)
                {
                    continue;
                }

                current[0].GetDisplayName(bindContext, null, out string displayName);
                if (displayName.StartsWith("!VisualStudio.DTE.", StringComparison.OrdinalIgnoreCase)
                    && displayName.EndsWith(processSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    table.GetObject(current[0], out object dte);
                    return dte;
                }
            }
            catch (COMException)
            {
                // Some unrelated ROT entries don't expose a display name to this process.
            }
        }

        return null;
    }

    private static object? ReadProperty(object? instance, string name)
    {
        return instance?.GetType().InvokeMember(
            name,
            BindingFlags.GetProperty,
            binder: null,
            target: instance,
            args: null);
    }

    private static string? ReadString(object? instance, string name)
    {
        return ReadProperty(instance, name) as string;
    }

    [DllImport("ole32.dll")]
    private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable table);

    [DllImport("ole32.dll")]
    private static extern int CreateBindCtx(int reserved, out IBindCtx bindContext);
}
