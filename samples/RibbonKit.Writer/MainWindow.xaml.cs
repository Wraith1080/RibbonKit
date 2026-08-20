using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Writer.Editing;
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
    /// <summary>Gets the app-owned bridge between the ribbon and the native editor.</summary>
    public WriterEditingRibbonController EditingController => _editingController ??
        throw new InvalidOperationException("The Writer editing controller has not been initialized.");

    private readonly bool _ownsShell;
    private WriterEditingRibbonController? _editingController;
    private WriterFindReplaceDialog? _findReplaceDialog;
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
        _editingController = new WriterEditingRibbonController(DocumentEditor);
        _editingController.StateChanged += OnEditingStateChanged;
        _editingController.StatisticsChanged += OnEditingStatisticsChanged;
        _editingController.ZoomChanged += OnEditingZoomChanged;
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
        WireEditingRibbon();
        _editingController!.BindWindowCommand(this, WriterRibbonCommands.Find,
            (_, e) =>
            {
                ShowFindReplace(showReplace: false);
                e.Handled = true;
            },
            (_, e) => e.CanExecute = DocumentEditor.IsEnabled);
        _editingController.BindWindowCommand(this, WriterRibbonCommands.Replace,
            (_, e) =>
            {
                ShowFindReplace(showReplace: true);
                e.Handled = true;
            },
            (_, e) => e.CanExecute = DocumentEditor.IsEnabled && !DocumentEditor.IsReadOnly);
        foreach (var binding in InputBindings.OfType<KeyBinding>())
        {
            binding.Command = binding.Key switch
            {
                Key.N => Shell.NewCommand,
                Key.O => Shell.OpenCommand,
                Key.S when binding.Modifiers.HasFlag(ModifierKeys.Shift) => Shell.SaveAsCommand,
                Key.S => Shell.SaveCommand,
                Key.Z => ApplicationCommands.Undo,
                Key.Y => ApplicationCommands.Redo,
                Key.X => ApplicationCommands.Cut,
                Key.C => ApplicationCommands.Copy,
                Key.V => ApplicationCommands.Paste,
                Key.B => EditingCommands.ToggleBold,
                Key.I => EditingCommands.ToggleItalic,
                Key.U => EditingCommands.ToggleUnderline,
                Key.F => WriterRibbonCommands.Find,
                Key.H => WriterRibbonCommands.Replace,
                _ => binding.Command
            };
        }
    }

    private void WireEditingRibbon()
    {
        var editing = EditingController;
        editing.BindCommand(QatUndo, ApplicationCommands.Undo);
        editing.BindCommand(QatRedo, ApplicationCommands.Redo);
        editing.BindCommand(PasteButton, ApplicationCommands.Paste);
        editing.BindCommand(CutButton, ApplicationCommands.Cut);
        editing.BindCommand(CopyButton, ApplicationCommands.Copy);
        editing.BindFontFamily(FontFamilyCombo);
        editing.BindFontSize(FontSizeCombo);
        editing.BindToggle(BoldButton, EditingCommands.ToggleBold,
            state => WriterEditingRibbonController.Checked(state.Bold, true));
        editing.BindToggle(ItalicButton, EditingCommands.ToggleItalic,
            state => WriterEditingRibbonController.Checked(state.Italic, true));
        editing.BindToggle(UnderlineButton, EditingCommands.ToggleUnderline,
            state => WriterEditingRibbonController.Checked(state.Underline, true));
        editing.BindAvailability(TextColorButton, state => state.CanFormat);
        editing.BindCommand(TextColorAutomatic, WriterEditingCommands.ApplyForeground, null);
        editing.BindCommand(TextColorBlack, WriterEditingCommands.ApplyForeground, Colors.Black);
        editing.BindCommand(TextColorBlue, WriterEditingCommands.ApplyForeground, Colors.Blue);
        editing.BindCommand(TextColorRed, WriterEditingCommands.ApplyForeground, Colors.Red);
        editing.BindAvailability(HighlightColorButton, state => state.CanFormat);
        editing.BindCommand(HighlightNone, WriterEditingCommands.ApplyHighlight, null);
        editing.BindCommand(HighlightYellow, WriterEditingCommands.ApplyHighlight, Colors.Yellow);
        editing.BindCommand(HighlightGreen, WriterEditingCommands.ApplyHighlight, Colors.LightGreen);
        editing.BindToggle(AlignLeftButton, WriterEditingCommands.SetAlignment,
            state => WriterEditingRibbonController.Checked(state.Alignment, TextAlignment.Left), TextAlignment.Left);
        editing.BindToggle(AlignCenterButton, WriterEditingCommands.SetAlignment,
            state => WriterEditingRibbonController.Checked(state.Alignment, TextAlignment.Center), TextAlignment.Center);
        editing.BindToggle(AlignRightButton, WriterEditingCommands.SetAlignment,
            state => WriterEditingRibbonController.Checked(state.Alignment, TextAlignment.Right), TextAlignment.Right);
        editing.BindToggle(AlignJustifyButton, WriterEditingCommands.SetAlignment,
            state => WriterEditingRibbonController.Checked(state.Alignment, TextAlignment.Justify), TextAlignment.Justify);
        editing.BindToggle(BulletsButton, WriterEditingCommands.ToggleBullets,
            state => state.ListKind.IsUniform ? state.ListKind.Value == WriterListKind.Bulleted : null);
        editing.BindToggle(NumberingButton, WriterEditingCommands.ToggleNumbering,
            state => state.ListKind.IsUniform ? state.ListKind.Value == WriterListKind.Numbered : null);
        editing.BindCommand(IncreaseIndentButton, WriterEditingCommands.IncreaseIndentation);
        editing.BindCommand(DecreaseIndentButton, WriterEditingCommands.DecreaseIndentation);
        editing.BindAvailability(ParagraphSpacingButton, state => state.CanFormat);
        editing.BindParagraphSpacingPreset(ParagraphSpacingCompact, 0, 0);
        editing.BindParagraphSpacingPreset(ParagraphSpacingNormal, 0, 6);
        editing.BindParagraphSpacingPreset(ParagraphSpacingOpen, 6, 12);
        editing.BindAction(FindButton, () => ShowFindReplace(showReplace: false));
        editing.BindAction(ReplaceButton, () => ShowFindReplace(showReplace: true),
            state => state.IsEnabled && !state.IsReadOnly);
        editing.BindCommand(SelectAllButton, ApplicationCommands.SelectAll);
        editing.BindSpellCheck(SpellCheckButton);
        editing.BindZoom(ZoomInButton, ZoomOutButton, ZoomResetButton);
        UpdateEditingStatusSurface();
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
    private void ReplaceEditorDocument()
    {
        _replacingDocument = true;
        try
        {
            if (_editingController is not null)
                _editingController.ReplaceDocument(Shell.CurrentDocument.Content);
            else
                DocumentEditor.Document = Shell.CurrentDocument.Content;
        }
        finally
        {
            _replacingDocument = false;
        }
        UpdateEditingStatusSurface();
    }
    private void OnExitRequested(object? sender, EventArgs e) { _allowClose = true; Close(); }
    private void OnClosed(object? sender, EventArgs e)
    {
        _closing = false;
        Shell.PropertyChanged -= OnShellPropertyChanged;
        Shell.ExitRequested -= OnExitRequested;
        if (_findReplaceDialog is not null)
        {
            _findReplaceDialog.Close();
            _findReplaceDialog = null;
        }
        if (_editingController is not null)
        {
            _editingController.StateChanged -= OnEditingStateChanged;
            _editingController.StatisticsChanged -= OnEditingStatisticsChanged;
            _editingController.ZoomChanged -= OnEditingZoomChanged;
            _editingController.Dispose();
            _editingController = null;
        }
        DocumentEditor.TextChanged -= OnEditorTextChanged;
        if (_ownsShell)
            Shell.Dispose();
    }

    private void OnEditingStateChanged(object? sender, EventArgs e)
    {
        UpdateEditingStatusSurface();
        _findReplaceDialog?.SetCanReplace(EditingController.FindReplace.CanMutate);
    }

    private void OnEditingStatisticsChanged(object? sender, EventArgs e) => UpdateEditingStatusSurface();

    private void OnEditingZoomChanged(object? sender, EventArgs e) => UpdateEditingStatusSurface();

    private void UpdateEditingStatusSurface()
    {
        if (_editingController is null)
            return;
        var snapshot = _editingController.Statistics.Statistics;
        StatisticsText.Text = $"{snapshot.Words:N0} words, {snapshot.Characters:N0} characters";
        ZoomText.Text = $"{_editingController.Zoom.Value:0}%";
        SpellCheckStatusText.Text = !_editingController.SpellCheck.IsSupported
            ? "Spelling unavailable"
            : _editingController.SpellCheck.IsEnabled ? "Spelling on" : "Spelling off";
    }

    private void ShowFindReplace(bool showReplace)
    {
        if (_findReplaceDialog is not null)
        {
            if (showReplace)
                _findReplaceDialog.ShowReplaceControls();
            _findReplaceDialog.SetCanReplace(EditingController.FindReplace.CanMutate);
            _findReplaceDialog.Activate();
            return;
        }

        _findReplaceDialog = new WriterFindReplaceDialog(EditingController.FindReplace, showReplace)
        {
            Owner = this
        };
        _findReplaceDialog.SetCanReplace(EditingController.FindReplace.CanMutate);
        _findReplaceDialog.Closed += OnFindReplaceClosed;
        _findReplaceDialog.Show();
    }

    private void OnFindReplaceClosed(object? sender, EventArgs e)
    {
        if (_findReplaceDialog is not null)
            _findReplaceDialog.Closed -= OnFindReplaceClosed;
        _findReplaceDialog = null;
        DocumentEditor.Focus();
        EditingController.RefreshState();
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
