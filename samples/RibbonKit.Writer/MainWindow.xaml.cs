using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.Services.Persistence;
using RibbonKit.Writer.Services.RecentFiles;
using RibbonKit.Writer.Shell;

namespace RibbonKit.Writer;

/// <summary>
/// The Writer editing surface hosted by the ribbon shell.
/// </summary>
public partial class MainWindow : RibbonWindow
{
    public WriterShellViewModel Shell { get; }
    private readonly bool _ownsShell;
    private bool _closing;
    private bool _allowClose;
    private bool _replacingDocument;

    public MainWindow()
    {
        var dialogs = new WriterDialogService();
        var session = new WriterDocumentSession(new WriterDocumentPersistence(), new WriterUnsavedChangesDecider(dialogs), new WriterSaveDestinationProvider(dialogs));
        Shell = new WriterShellViewModel(session, new RecentFileService(), dialogs);
        _ownsShell = true;
        InitializeShell(dialogs);
    }

    /// <summary>
    /// Creates a window around an injected shell. The caller retains ownership of the shell and
    /// remains responsible for disposing it after this window closes.
    /// </summary>
    public MainWindow(WriterShellViewModel shell)
    {
        Shell = shell ?? throw new ArgumentNullException(nameof(shell));
        _ownsShell = false;
        InitializeShell(shellDialogs: null);
    }

    private void InitializeShell(WriterDialogService? shellDialogs)
    {
        InitializeComponent();
        if (shellDialogs is not null) shellDialogs.Owner = this;
        DataContext = Shell;
        RecentList.ItemsSource = Shell.RecentEntries;
        WireRibbonCommands();
        Shell.PropertyChanged += OnShellPropertyChanged;
        Shell.ExitRequested += OnExitRequested;
        ReplaceEditorDocument();
        DocumentEditor.TextChanged += OnEditorTextChanged;
        Closing += OnClosing;
        Closed += OnClosed;
    }

    private void WireRibbonCommands()
    {
        if (FindName("RecentList") is ItemsControl wiredRecentList)
            wiredRecentList.ItemsSource = Shell.RecentEntries;
        if (MainRibbon.Backstage is Backstage backstage)
        {
            backstage.DataContext = Shell;
            foreach (var item in backstage.Items.OfType<BackstageTabItem>())
            {
                item.Command = CommandFor(AutomationProperties.GetAutomationId(item)) ?? item.Command;
            }
            // Backstage is moved into an adorner when opened, so its detached content does not
            // inherit the window DataContext. Wire the recent list at the same boundary as the
            // File commands so its realized item templates receive the real shell entries.
            foreach (var recentList in FindLogicalDescendants<ItemsControl>(backstage)
                         .Where(control => AutomationProperties.GetAutomationId(control) == "RecentList"))
            {
                recentList.ItemsSource = Shell.RecentEntries;
            }
        }
        foreach (var item in MainRibbon.QuickAccessItems.OfType<FrameworkElement>())
        {
            if (item is ButtonBase button)
                button.Command = CommandFor(AutomationProperties.GetAutomationId(item)) ?? button.Command;
        }
        foreach (var group in MainRibbon.Tabs.SelectMany(tab => tab.Groups))
        {
            foreach (var item in group.Items.OfType<FrameworkElement>())
            {
                if (item is ButtonBase button)
                    button.Command = CommandFor(AutomationProperties.GetAutomationId(item)) ?? button.Command;
            }
        }
        foreach (var binding in InputBindings.OfType<KeyBinding>())
        {
            binding.Command = binding.Key switch
            {
                Key.N => Shell.NewCommand,
                Key.O => Shell.OpenCommand,
                Key.S when binding.Modifiers.HasFlag(ModifierKeys.Shift) => Shell.SaveAsCommand,
                Key.S => Shell.SaveCommand,
                _ => binding.Command
            };
        }
    }

    private ICommand? CommandFor(string? automationId) => automationId switch
    {
        "FileNew" or "HomeNew" => Shell.NewCommand,
        "FileOpen" or "HomeOpen" => Shell.OpenCommand,
        "FileSave" or "HomeSave" or "QatSave" => Shell.SaveCommand,
        "FileSaveAs" or "HomeSaveAs" => Shell.SaveAsCommand,
        "FileExit" => Shell.ExitCommand,
        _ => null
    };

    private static IEnumerable<T> FindLogicalDescendants<T>(DependencyObject root) where T : DependencyObject
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            if (child is T match)
                yield return match;
            foreach (var descendant in FindLogicalDescendants<T>(child))
                yield return descendant;
        }
    }

    private void OnShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WriterShellViewModel.CurrentDocument)) ReplaceEditorDocument();
    }
    private void ReplaceEditorDocument() { _replacingDocument = true; try { DocumentEditor.Document = Shell.CurrentDocument.Content; } finally { _replacingDocument = false; } }
    private void OnExitRequested(object? sender, EventArgs e) { _allowClose = true; Close(); }
    private void OnClosed(object? sender, EventArgs e)
    {
        _closing = false;
        Shell.PropertyChanged -= OnShellPropertyChanged;
        Shell.ExitRequested -= OnExitRequested;
        if (_ownsShell)
            Shell.Dispose();
    }

    private void OnEditorTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_replacingDocument || !ReferenceEquals(DocumentEditor.Document, Shell.CurrentDocument.Content)) return;
        Shell.MarkEditorDirty();
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        if (_closing) { e.Cancel = true; return; }
        e.Cancel = true; _closing = true;
        var approved = false;
        try
        {
            approved = await Shell.RequestCloseAsync();
            if (approved)
            {
                _allowClose = true;
                _ = Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(Close));
            }
        }
        finally
        {
            // Keep the guard asserted through the approved second Closing pass. On cancel or
            // failure, the original close was canceled and the window must remain usable.
            if (!approved)
            {
                _allowClose = false;
                _closing = false;
            }
        }
    }

    private void OnBackstageAction(object sender, RoutedEventArgs e) => MainRibbon.IsBackstageOpen = false;
    private void OnRecentItemClick(object sender, RoutedEventArgs e) => MainRibbon.IsBackstageOpen = false;
}
