using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Printing;
using RibbonKit.Controls;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Models;
using RibbonKit.Writer.Page;
using RibbonKit.Writer.Preview;
using RibbonKit.Writer.Printing;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.Services.Persistence;
using RibbonKit.Writer.Services.RecentFiles;
using RibbonKit.Writer.Shell;
using RibbonKit.Writer.View;

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

    /// <summary>Gets the active Writer document presentation.</summary>
    public WriterViewMode CurrentViewMode { get; private set; } = WriterViewMode.Paper;

    internal bool IsPreviewRebuildEnabled => _previewController?.IsRebuildEnabled == true;

    private readonly bool _ownsShell;
    private WriterEditingRibbonController? _editingController;
    private WriterPreviewController? _previewController;
    private readonly WriterPrintService _printService = new();
    private WriterFindReplaceDialog? _findReplaceDialog;
    private DependencyPropertyDescriptor? _backstageOpenDescriptor;
    private WriterViewMode _viewModeBeforePrintPreview = WriterViewMode.Paper;
    private bool _closing;
    private bool _allowClose;
    private bool _replacingDocument;
    private bool _restoreEditorFocusAfterBackstageClose;
    private bool _rulerVisible = true;
    private bool _marginGuidesVisible = true;

    public MainWindow()
    {
        var dialogs = new WriterDialogService();
        var session = new WriterDocumentSession(new WriterDocumentPersistence(),
            new WriterUnsavedChangesDecider(dialogs), new WriterSaveDestinationProvider(dialogs),
            transitionDecider: new WriterFormatTransitionDecider(dialogs));
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
        _backstageOpenDescriptor = DependencyPropertyDescriptor.FromProperty(
            Ribbon.IsBackstageOpenProperty, typeof(Ribbon));
        _backstageOpenDescriptor.AddValueChanged(MainRibbon, OnBackstageOpenChanged);
        if (shellDialogs is not null) shellDialogs.Owner = this;
        DataContext = Shell;
        RecentList.ItemsSource = Shell.RecentEntries;
        EditorSurface.Attach(DocumentEditor, EditorViewport, PaperCanvas);
        _editingController = new WriterEditingRibbonController(DocumentEditor);
        MarginGuide.Attach(DocumentEditor, EditorViewport, PaperCanvas);
        HorizontalRuler.Attach(DocumentEditor, EditorViewport, PaperCanvas, _editingController.Editing);
        HorizontalRuler.PageSettings = Shell.CurrentDocument.PageSettings;
        MarginGuide.PageSettings = Shell.CurrentDocument.PageSettings;
        HorizontalRuler.PageSettingsPreviewChanged += OnRulerPageSettingsPreviewChanged;
        HorizontalRuler.PageSettingsCommitted += OnRulerPageSettingsCommitted;
        HorizontalRuler.PageSettingsDragCancelled += OnRulerPageSettingsDragCancelled;
        HorizontalRuler.ParagraphIndentDragCompleted += OnParagraphIndentDragCompleted;
        _editingController.StateChanged += OnEditingStateChanged;
        _editingController.StatisticsChanged += OnEditingStatisticsChanged;
        _editingController.ZoomChanged += OnEditingZoomChanged;
        WireRibbonCommands();
        Shell.PropertyChanged += OnShellPropertyChanged;
        Shell.ExitRequested += OnExitRequested;
        ReplaceEditorDocument();
        _previewController = new WriterPreviewController(DocumentEditor, Shell.CurrentDocument);
        _previewController.SnapshotChanged += OnPreviewSnapshotChanged;
        _previewController.SetRebuildEnabled(false);
        PreviewView.StateChanged += OnPreviewViewStateChanged;
        ApplyWriterViewMode(CurrentViewMode, restoreEditorFocus: false);
        DocumentEditor.TextChanged += OnEditorTextChanged;
        ContentRendered += OnInitialContentRendered;
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
                if (item.IsButton)
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

            // Backstage content is realized in a detached adorner when the File surface opens.
            // Wire the card commands at the same boundary so keyboard/UIA invocation does not
            // depend on a DataContext bridge surviving that reparenting.
            foreach (var card in FindLogicalDescendants<Button>(backstage)
                         .Where(button => AutomationProperties.GetAutomationId(button)
                             .StartsWith("New", StringComparison.Ordinal)))
            {
                card.Command = Shell.NewCommand;
                card.CommandParameter = card.Tag;
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
        editing.BindAction(ZoomOutButton, () => AdjustCurrentZoom(-10));
        editing.BindAction(ZoomResetButton, ResetCurrentZoom);
        editing.BindAction(ZoomInButton, () => AdjustCurrentZoom(10));
        editing.BindAction(PreviewZoomOutButton, () => AdjustCurrentZoom(-10));
        editing.BindAction(PreviewZoomResetButton, ResetCurrentZoom);
        editing.BindAction(PreviewZoomInButton, () => AdjustCurrentZoom(10));
        UpdateEditingStatusSurface();
        ApplyProfileCapabilityProjection();
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
        if (e.PropertyName == nameof(WriterShellViewModel.CurrentDocument))
            ReplaceEditorDocument();
        else if (e.PropertyName == nameof(WriterShellViewModel.IsBusy) && !Shell.IsBusy)
            CompleteBackstageCloseFocus();
    }
    private void ReplaceEditorDocument()
    {
        HorizontalRuler.CancelActiveDrags();
        MarginGuide.ClearPreview();
        if (_observedDocument is not null)
            _observedDocument.PropertyChanged -= OnCurrentDocumentPropertyChanged;
        _observedDocument = Shell.CurrentDocument;
        _observedDocument.PropertyChanged += OnCurrentDocumentPropertyChanged;
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
        EditorSurface.SetDocument(Shell.CurrentDocument.Content);
        EditorSurface.PageSettings = Shell.CurrentDocument.PageSettings;
        EditorSurface.ZoomPercent = _editingController?.Zoom.Value ?? 100d;
        HorizontalRuler.PageSettings = Shell.CurrentDocument.PageSettings;
        HorizontalRuler.ZoomPercent = _editingController?.Zoom.Value ?? 100d;
        MarginGuide.PageSettings = Shell.CurrentDocument.PageSettings;
        MarginGuide.ZoomPercent = _editingController?.Zoom.Value ?? 100d;
        DocumentEditor.Background = Shell.CurrentDocument.Content.Background ?? Brushes.White;
        PreviewView.SetSnapshot(null);
        _previewController?.SetDocument(Shell.CurrentDocument);
        if (_previewController is not null)
            MarkPreviewPending();
        UpdatePageSummary();
        UpdatePreviewState();
        UpdateEditingStatusSurface();
        ApplyProfileCapabilityProjection();
    }

    private WriterDocument? _observedDocument;

    private void OnCurrentDocumentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WriterDocument.PageSettings) or nameof(WriterDocument.Format))
        {
            if (e.PropertyName == nameof(WriterDocument.PageSettings))
            {
                EditorSurface.PageSettings = Shell.CurrentDocument.PageSettings;
                HorizontalRuler.PageSettings = Shell.CurrentDocument.PageSettings;
                MarginGuide.PageSettings = Shell.CurrentDocument.PageSettings;
                MarkPreviewPending();
                UpdatePageSummary();
            }
            ApplyProfileCapabilityProjection();
        }
    }
    private void OnExitRequested(object? sender, EventArgs e) { _allowClose = true; Close(); }
    private void OnClosed(object? sender, EventArgs e)
    {
        _closing = false;
        _restoreEditorFocusAfterBackstageClose = false;
        ContentRendered -= OnInitialContentRendered;
        Shell.PropertyChanged -= OnShellPropertyChanged;
        Shell.ExitRequested -= OnExitRequested;
        if (_backstageOpenDescriptor is not null)
        {
            _backstageOpenDescriptor.RemoveValueChanged(MainRibbon, OnBackstageOpenChanged);
            _backstageOpenDescriptor = null;
        }
        if (_observedDocument is not null)
            _observedDocument.PropertyChanged -= OnCurrentDocumentPropertyChanged;
        if (_findReplaceDialog is not null)
        {
            _findReplaceDialog.Close();
            _findReplaceDialog = null;
        }
        HorizontalRuler.PageSettingsPreviewChanged -= OnRulerPageSettingsPreviewChanged;
        HorizontalRuler.PageSettingsCommitted -= OnRulerPageSettingsCommitted;
        HorizontalRuler.PageSettingsDragCancelled -= OnRulerPageSettingsDragCancelled;
        HorizontalRuler.ParagraphIndentDragCompleted -= OnParagraphIndentDragCompleted;
        HorizontalRuler.Dispose();
        MarginGuide.Dispose();
        PreviewView.SetSnapshot(null);
        PreviewView.StateChanged -= OnPreviewViewStateChanged;
        if (_previewController is not null)
        {
            _previewController.SnapshotChanged -= OnPreviewSnapshotChanged;
            _previewController.Dispose();
            _previewController = null;
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

    private void OnInitialContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= OnInitialContentRendered;
        QueueEditorFocus();
    }

    private void QueueEditorFocus()
    {
        _ = Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
        {
            if (!IsVisible || _closing || CurrentViewMode == WriterViewMode.PrintPreview
                || MainRibbon.IsBackstageOpen || MainRibbon.IsModal)
                return;

            FocusManager.SetFocusedElement(this, DocumentEditor);
            DocumentEditor.Focus();
            Keyboard.Focus(DocumentEditor);
            EditingController.RefreshState();
        }));
    }

    private void OnEditingStateChanged(object? sender, EventArgs e)
    {
        UpdateEditingStatusSurface();
        _findReplaceDialog?.SetCanReplace(EditingController.FindReplace.CanMutate);
    }

    private void OnEditingStatisticsChanged(object? sender, EventArgs e) => UpdateEditingStatusSurface();

    private void OnEditingZoomChanged(object? sender, EventArgs e)
    {
        EditorSurface.ZoomPercent = EditingController.Zoom.Value;
        HorizontalRuler.ZoomPercent = EditingController.Zoom.Value;
        MarginGuide.ZoomPercent = EditingController.Zoom.Value;
        UpdateEditingStatusSurface();
    }

    private void UpdateEditingStatusSurface()
    {
        if (_editingController is null)
            return;
        var snapshot = _editingController.Statistics.Statistics;
        StatisticsText.Text = $"{snapshot.Words:N0} words, {snapshot.Characters:N0} characters";
        var zoom = CurrentViewMode == WriterViewMode.PrintPreview
            ? PreviewView.Zoom
            : _editingController.Zoom.Value;
        ZoomText.Text = $"{zoom:0}%";
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
        MarkPreviewPending();
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

    private void OnBackstageAction(object sender, RoutedEventArgs e)
    {
        MainRibbon.IsBackstageOpen = false;
    }

    private void OnNewProfileCardClick(object sender, RoutedEventArgs e)
    {
        // The command executes through ButtonBase; this handler only closes the page. The
        // IsBackstageOpen observer owns the common focus return for cards and every other File
        // surface action.
        MainRibbon.IsBackstageOpen = false;
    }

    private void OnRecentItemClick(object sender, RoutedEventArgs e)
    {
        MainRibbon.IsBackstageOpen = false;
    }

    private void OnPreviewSnapshotChanged(object? sender, EventArgs e)
    {
        if (_previewController is null)
            return;
        // This replacement is intentionally synchronous. The controller disposes its previous
        // package immediately after this event returns.
        PreviewView.SetSnapshot(_previewController.Snapshot);
        PreviewPendingOverlay.Visibility = Visibility.Collapsed;
        UpdatePageSummary();
        UpdatePreviewState();
    }

    private void OnPreviewViewStateChanged(object? sender, EventArgs e)
    {
        UpdatePreviewState();
        UpdateEditingStatusSurface();
    }

    private void OnContinuousViewClick(object sender, RoutedEventArgs e) =>
        ApplyWriterViewMode(WriterViewMode.ContinuousEdit, restoreEditorFocus: true);

    private void OnPaperViewClick(object sender, RoutedEventArgs e) =>
        ApplyWriterViewMode(WriterViewMode.Paper, restoreEditorFocus: true);

    private void OnRulerToggleClick(object sender, RoutedEventArgs e)
    {
        _rulerVisible = RulerToggleButton.IsChecked == true;
        ApplyRulerVisibility();
        RestoreEditorFocusAfterViewCommand();
    }

    private void OnMarginGuidesToggleClick(object sender, RoutedEventArgs e)
    {
        _marginGuidesVisible = MarginGuidesToggleButton.IsChecked == true;
        ApplyRulerVisibility();
        RestoreEditorFocusAfterViewCommand();
    }

    private void ApplyRulerVisibility()
    {
        var paper = CurrentViewMode == WriterViewMode.Paper;
        HorizontalRuler.IsPaperView = paper;
        HorizontalRuler.IsRulerVisible = _rulerVisible;
        MarginGuide.IsPaperView = paper;
        MarginGuide.IsGuideVisible = _marginGuidesVisible;
        MarginGuide.Visibility = paper && _marginGuidesVisible
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void OnRulerPageSettingsPreviewChanged(object? sender,
        WriterRulerPageSettingsEventArgs e) => MarginGuide.PreviewPageSettings = e.Settings;

    private void OnRulerPageSettingsCommitted(object? sender,
        WriterRulerPageSettingsEventArgs e)
    {
        MarginGuide.ClearPreview();
        if (!SupportsProfileCommand(WriterDocumentCommandCapabilities.PageSettings) ||
            !TryApplyPageSettings(e.Settings))
        {
            // A profile transition or a host validation failure must leave the ruler at the last
            // committed model value; the drag itself never dirties the document incrementally.
            HorizontalRuler.PageSettings = Shell.CurrentDocument.PageSettings;
            MarginGuide.PageSettings = Shell.CurrentDocument.PageSettings;
        }
        RestoreEditorFocusAfterViewCommand();
    }

    private void OnRulerPageSettingsDragCancelled(object? sender, EventArgs e)
    {
        MarginGuide.ClearPreview();
        RestoreEditorFocusAfterViewCommand();
    }

    private void OnParagraphIndentDragCompleted(object? sender, EventArgs e) =>
        RestoreEditorFocusAfterViewCommand();

    private void OnPrintPreviewViewClick(object sender, RoutedEventArgs e) => EnterPrintPreview();

    private void OnBackstagePreviewClick(object sender, RoutedEventArgs e)
    {
        if (!SupportsProfileCommand(WriterDocumentCommandCapabilities.Preview))
            return;
        MainRibbon.IsBackstageOpen = false;
        EnterPrintPreview();
    }

    private void EnterPrintPreview()
    {
        if (!SupportsProfileCommand(WriterDocumentCommandCapabilities.Preview))
            return;
        if (CurrentViewMode != WriterViewMode.PrintPreview)
            _viewModeBeforePrintPreview = CurrentViewMode;
        MainRibbon.EnterModal(PrintPreviewTab);
    }

    private void OnRibbonModalEntered(object sender, RibbonModalEventArgs e)
    {
        if (ReferenceEquals(e.Tab, PrintPreviewTab))
            ApplyWriterViewMode(WriterViewMode.PrintPreview, restoreEditorFocus: false);
    }

    private void OnRibbonModalExited(object sender, RibbonModalEventArgs e)
    {
        if (ReferenceEquals(e.Tab, PrintPreviewTab))
            ApplyWriterViewMode(_viewModeBeforePrintPreview, restoreEditorFocus: true);
    }

    private void OnBackstageSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdatePreviewDemand();

    private void OnBackstageOpenChanged(object? sender, EventArgs e)
    {
        UpdatePreviewDemand();
        if (MainRibbon.IsBackstageOpen || _closing || !IsVisible || MainRibbon.IsModal)
            return;

        // IsBackstageOpen changes before the adorner has finished closing. Defer once so a
        // command launched from the page can enter IsBusy, then complete on the common shell
        // idle edge instead of asking every File action to restore focus independently.
        _restoreEditorFocusAfterBackstageClose = true;
        _ = Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle,
            new Action(CompleteBackstageCloseFocus));
    }

    private void CompleteBackstageCloseFocus()
    {
        if (!_restoreEditorFocusAfterBackstageClose || Shell.IsBusy)
            return;

        _restoreEditorFocusAfterBackstageClose = false;
        QueueEditorFocus();
    }

    private void UpdatePreviewDemand()
    {
        if (_previewController is null)
            return;
        var needsPreview = CurrentViewMode == WriterViewMode.PrintPreview ||
            MainRibbon.IsBackstageOpen && PrintBackstageTab.IsSelected;
        _previewController.SetRebuildEnabled(needsPreview);
        if (needsPreview && !_previewController.TryGetCurrentSnapshot(out _))
            MarkPreviewPending();
        else
            UpdatePreviewState();
    }

    internal void ApplyWriterViewMode(WriterViewMode mode, bool restoreEditorFocus = true)
    {
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown Writer view mode.");

        CurrentViewMode = mode;
        if (mode == WriterViewMode.PrintPreview)
        {
            _previewController?.SetRebuildEnabled(true);
            EditorSurface.Visibility = Visibility.Collapsed;
            PreviewView.Visibility = Visibility.Visible;
            ApplyRulerVisibility();
            HorizontalRuler.CancelPageSettingsPreview();
            MarginGuide.ClearPreview();
            if (_previewController is null || !_previewController.TryGetCurrentSnapshot(out var snapshot))
            {
                PreviewView.SetSnapshot(null);
                PreviewPendingOverlay.Visibility = Visibility.Visible;
            }
            else
            {
                PreviewView.SetSnapshot(snapshot);
                PreviewPendingOverlay.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            PreviewView.Visibility = Visibility.Collapsed;
            PreviewPendingOverlay.Visibility = Visibility.Collapsed;
            EditorSurface.ViewMode = mode == WriterViewMode.ContinuousEdit
                ? WriterEditorViewMode.Continuous
                : WriterEditorViewMode.Paper;
            EditorSurface.Visibility = Visibility.Visible;
            var paper = mode == WriterViewMode.Paper;
            ApplyRulerVisibility();
            if (!paper)
            {
                HorizontalRuler.CancelPageSettingsPreview();
                MarginGuide.ClearPreview();
            }
            if (restoreEditorFocus)
                QueueEditorFocus();
        }

        UpdatePreviewDemand();
        UpdateViewButtons();
        UpdatePreviewState();
        UpdateEditingStatusSurface();
    }

    private void OnOnePageClick(object sender, RoutedEventArgs e)
    {
        PreviewView.ViewMode = WriterPreviewViewMode.OnePage;
    }

    private void OnTwoPagesClick(object sender, RoutedEventArgs e)
    {
        PreviewView.ViewMode = WriterPreviewViewMode.TwoPages;
    }

    private void OnPageWidthClick(object sender, RoutedEventArgs e)
    {
        PreviewView.ViewMode = WriterPreviewViewMode.PageWidth;
    }

    private void OnPreviousPageClick(object sender, RoutedEventArgs e) => PreviewView.GoToPreviousPage();

    private void OnNextPageClick(object sender, RoutedEventArgs e) => PreviewView.GoToNextPage();

    private void UpdateViewButtons()
    {
        ContinuousViewButton.IsChecked = CurrentViewMode == WriterViewMode.ContinuousEdit;
        PaperViewButton.IsChecked = CurrentViewMode == WriterViewMode.Paper;
        PrintPreviewViewButton.IsChecked = CurrentViewMode == WriterViewMode.PrintPreview;
        OnePageButton.IsChecked = PreviewView.ViewMode == WriterPreviewViewMode.OnePage;
        TwoPagesButton.IsChecked = PreviewView.ViewMode == WriterPreviewViewMode.TwoPages;
        PageWidthButton.IsChecked = PreviewView.ViewMode == WriterPreviewViewMode.PageWidth;
        RulerToggleButton.IsChecked = _rulerVisible;
        MarginGuidesToggleButton.IsChecked = _marginGuidesVisible;
    }

    private void UpdatePreviewState()
    {
        var previewActive = CurrentViewMode == WriterViewMode.PrintPreview;
        var profile = Shell.CurrentDocument.Format.GetProfile();
        var supportsPreview = profile.Supports(WriterDocumentCommandCapabilities.Preview);
        var supportsPrinting = profile.Supports(WriterDocumentCommandCapabilities.Printing);
        OnePageButton.IsEnabled = supportsPreview && previewActive;
        TwoPagesButton.IsEnabled = supportsPreview && previewActive;
        PageWidthButton.IsEnabled = supportsPreview && previewActive;
        PreviousPageButton.IsEnabled = supportsPreview && previewActive && PreviewView.CanGoToPreviousPage;
        NextPageButton.IsEnabled = supportsPreview && previewActive && PreviewView.CanGoToNextPage;
        BackstagePrintButton.IsEnabled = supportsPrinting && _previewController is not null &&
            _previewController.TryGetCurrentSnapshot(out _);
        BackstagePreviewButton.IsEnabled = supportsPreview;
        PreviewPageText.Text = previewActive && PreviewView.PageCount > 0
            ? $"· Page {PreviewView.CurrentPageNumber:N0} of {PreviewView.PageCount:N0}"
            : string.Empty;
        UpdateViewButtons();
    }

    /// <summary>Gets the active profile projected onto the Writer shell.</summary>
    internal WriterDocumentProfile CurrentProfile =>
        Shell.CurrentDocument.Format.GetProfile();

    /// <summary>Gets whether the active profile exposes a command capability.</summary>
    internal bool SupportsProfileCommand(WriterDocumentCommandCapabilities capability) =>
        CurrentProfile.Supports(capability);

    private void ApplyProfileCapabilityProjection()
    {
        if (!IsInitialized)
            return;

        var profile = CurrentProfile;
        var formatting = profile.Supports(WriterDocumentCommandCapabilities.CharacterFormatting
            | WriterDocumentCommandCapabilities.ParagraphFormatting);
        var pageSettings = profile.Supports(WriterDocumentCommandCapabilities.PageSettings);
        var preview = profile.Supports(WriterDocumentCommandCapabilities.Preview);
        var printing = profile.Supports(WriterDocumentCommandCapabilities.Printing);

        // Keep unsupported work visibly unavailable at the group boundary. Leaf controls are
        // also projected so UIA clients do not see an enabled child inside a disabled group.
        FontGroup.IsEnabled = formatting;
        ParagraphGroup.IsEnabled = formatting;
        PageSetupGroup.IsEnabled = pageSettings;
        MarginsGroup.IsEnabled = pageSettings;
        PageBackgroundGroup.IsEnabled = pageSettings;
        PageTab.IsEnabled = pageSettings;
        ViewTab.IsEnabled = preview;
        DocumentViewsGroup.IsEnabled = preview;
        ViewZoomGroup.IsEnabled = preview;
        PrintPreviewTab.IsEnabled = preview;
        PreviewLayoutGroup.IsEnabled = preview;
        PreviewNavigationGroup.IsEnabled = preview;
        PreviewZoomGroup.IsEnabled = preview;
        ViewDisplayGroup.IsEnabled = preview;
        HorizontalRuler.CanEditMargins = pageSettings;
        HorizontalRuler.CanEditParagraphs = formatting;

        foreach (var control in new UIElement[]
        {
            FontFamilyCombo, FontSizeCombo, BoldButton, ItalicButton, UnderlineButton,
            TextColorButton, HighlightColorButton, AlignLeftButton, AlignCenterButton,
            AlignRightButton, AlignJustifyButton, BulletsButton, NumberingButton,
            IncreaseIndentButton, DecreaseIndentButton, ParagraphSpacingButton,
            PaperSizeButton, OrientationButton, MarginsButton, PageColorButton,
            OnePageButton, TwoPagesButton, PageWidthButton, PreviousPageButton,
            NextPageButton, PreviewZoomOutButton, PreviewZoomResetButton, PreviewZoomInButton,
            RulerToggleButton, MarginGuidesToggleButton,
            BackstagePrintButton, BackstagePreviewButton
        })
        {
            if (control is null)
                continue;
            var formattingControl = ReferenceEquals(control, FontFamilyCombo)
                || ReferenceEquals(control, FontSizeCombo)
                || ReferenceEquals(control, BoldButton)
                || ReferenceEquals(control, ItalicButton)
                || ReferenceEquals(control, UnderlineButton)
                || ReferenceEquals(control, TextColorButton)
                || ReferenceEquals(control, HighlightColorButton)
                || ReferenceEquals(control, AlignLeftButton)
                || ReferenceEquals(control, AlignCenterButton)
                || ReferenceEquals(control, AlignRightButton)
                || ReferenceEquals(control, AlignJustifyButton)
                || ReferenceEquals(control, BulletsButton)
                || ReferenceEquals(control, NumberingButton)
                || ReferenceEquals(control, IncreaseIndentButton)
                || ReferenceEquals(control, DecreaseIndentButton)
                || ReferenceEquals(control, ParagraphSpacingButton);
            var pageControl = ReferenceEquals(control, PaperSizeButton)
                || ReferenceEquals(control, OrientationButton)
                || ReferenceEquals(control, MarginsButton)
                || ReferenceEquals(control, PageColorButton);
            var previewControl = ReferenceEquals(control, OnePageButton)
                || ReferenceEquals(control, TwoPagesButton)
                || ReferenceEquals(control, PageWidthButton)
                || ReferenceEquals(control, PreviousPageButton)
                || ReferenceEquals(control, NextPageButton)
                || ReferenceEquals(control, PreviewZoomOutButton)
                || ReferenceEquals(control, PreviewZoomResetButton)
                || ReferenceEquals(control, PreviewZoomInButton);
            previewControl = previewControl
                || ReferenceEquals(control, RulerToggleButton)
                || ReferenceEquals(control, MarginGuidesToggleButton);
            var allowed = formattingControl ? formatting
                : pageControl ? pageSettings
                : previewControl ? preview
                : ReferenceEquals(control, BackstagePrintButton) ? printing
                : ReferenceEquals(control, BackstagePreviewButton) ? preview
                : true;
            control.IsEnabled = allowed;
        }

        UpdatePreviewState();
    }

    private void MarkPreviewPending()
    {
        BackstagePrintButton.IsEnabled = false;
        if (CurrentViewMode == WriterViewMode.PrintPreview)
        {
            PreviewView.SetSnapshot(null);
            PreviewPendingOverlay.Visibility = Visibility.Visible;
            UpdatePreviewState();
        }
    }

    private void OnPaperSizeClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag } ||
            !Enum.TryParse<DocumentPaperSize>(tag, out var paperSize))
            return;
        TryApplyPageSettings(() => Shell.CurrentDocument.PageSettings.WithPreset(paperSize));
    }

    private void OnOrientationClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag } ||
            !Enum.TryParse<DocumentPageOrientation>(tag, out var orientation))
            return;
        TryApplyPageSettings(() => Shell.CurrentDocument.PageSettings.WithOrientation(orientation));
    }

    private void OnMarginPresetClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag } ||
            !Enum.TryParse<WriterMarginPreset>(tag, out var preset))
            return;
        TryApplyPageSettings(() => Shell.CurrentDocument.PageSettings.WithMargins(
            WriterPageUi.CreateMargins(preset)));
    }

    private void OnCustomMarginsClick(object sender, RoutedEventArgs e)
    {
        var dialog = new WriterCustomMarginsDialog(Shell.CurrentDocument.PageSettings)
        {
            Owner = this,
            FlowDirection = FlowDirection
        };
        if (dialog.ShowDialog() == true && dialog.ResultSettings is not null)
            TryApplyPageSettings(dialog.ResultSettings);
        else
            DocumentEditor.Focus();
    }

    internal bool TryApplyPageSettings(DocumentPageSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!SupportsProfileCommand(WriterDocumentCommandCapabilities.PageSettings))
            return false;
        try
        {
            return Shell.CurrentDocument.SetPageSettings(settings);
        }
        catch (ArgumentException exception)
        {
            MessageBox.Show(this, exception.Message, "Page Setup", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }
    }

    private void TryApplyPageSettings(Func<DocumentPageSettings> createSettings)
    {
        try
        {
            TryApplyPageSettings(createSettings());
        }
        catch (ArgumentException exception)
        {
            MessageBox.Show(this, exception.Message, "Page Setup", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OnPageColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag })
            return;
        var color = (Color)ColorConverter.ConvertFromString(tag)!;
        ApplyPageColor(color);
    }

    internal bool ApplyPageColor(Color color)
    {
        if (!SupportsProfileCommand(WriterDocumentCommandCapabilities.PageSettings))
            return false;
        var current = Shell.CurrentDocument.Content.Background as SolidColorBrush;
        if (current?.Color == color)
            return false;
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        Shell.CurrentDocument.Content.Background = brush;
        DocumentEditor.Background = brush;
        Shell.CurrentDocument.MarkDirty();
        _previewController?.Refresh();
        MarkPreviewPending();
        UpdatePageSummary();
        return true;
    }

    private void UpdatePageSummary()
    {
        var pageCount = _previewController is not null &&
            _previewController.TryGetCurrentSnapshot(out var snapshot)
                ? snapshot.Paginator.PageCount
                : 0;
        BackstagePageSummaryText.Text = WriterPageUi.FormatSummary(
            Shell.CurrentDocument.PageSettings, pageCount, Shell.CurrentDocument.Content.Background);
    }

    private void OnStatusZoomOutClick(object sender, RoutedEventArgs e) => AdjustCurrentZoom(-10);

    private void OnStatusZoomInClick(object sender, RoutedEventArgs e) => AdjustCurrentZoom(10);

    private void AdjustCurrentZoom(double change)
    {
        if (CurrentViewMode == WriterViewMode.PrintPreview)
        {
            PreviewView.ViewMode = WriterPreviewViewMode.OnePage;
            PreviewView.Zoom = Math.Clamp(PreviewView.Zoom + change,
                PreviewView.MinZoom, PreviewView.MaxZoom);
            return;
        }

        EditingController.Zoom.TrySet(EditingController.Zoom.Value + change);
        RestoreEditorFocusAfterViewCommand();
    }

    private void ResetCurrentZoom()
    {
        if (CurrentViewMode == WriterViewMode.PrintPreview)
        {
            PreviewView.ViewMode = WriterPreviewViewMode.OnePage;
            PreviewView.Zoom = 100;
            return;
        }

        EditingController.Zoom.Reset();
        RestoreEditorFocusAfterViewCommand();
    }

    private void RestoreEditorFocusAfterViewCommand() => QueueEditorFocus();

    private void OnPrintClick(object sender, RoutedEventArgs e)
    {
        if (!SupportsProfileCommand(WriterDocumentCommandCapabilities.Printing))
            return;
        if (_previewController is null || !_previewController.TryGetCurrentSnapshot(out _))
        {
            MessageBox.Show(this, "The preview is still updating. Wait for the current pages before printing.",
                "Print", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MainRibbon.IsBackstageOpen = false;
        var settings = Shell.CurrentDocument.PageSettings;
        var queues = new List<PrintQueue>();
        try
        {
            using var server = new LocalPrintServer();
            queues.AddRange(server.GetPrintQueues().Cast<PrintQueue>()
                .OrderBy(queue => queue.FullName, StringComparer.CurrentCultureIgnoreCase));
            if (queues.Count == 0)
            {
                MessageBox.Show(this, "Windows did not report an installed printer.",
                    "Print", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string? defaultPrinterName = null;
            try
            {
                using var defaultQueue = LocalPrintServer.GetDefaultPrintQueue();
                defaultPrinterName = defaultQueue?.FullName;
            }
            catch (PrintSystemException)
            {
                // A default is optional; the first available queue remains selectable.
            }

            if (_previewController is null || !_previewController.TryGetCurrentSnapshot(out var openingSnapshot))
                return;
            var setup = new WriterPrintSetupDialog(openingSnapshot,
                queues.Select(queue => new WriterPrinterChoice(queue)).ToArray(), defaultPrinterName)
            {
                Owner = this,
                FlowDirection = FlowDirection
            };
            if (setup.ShowDialog() != true || setup.SelectedPrinter?.Queue is not PrintQueue selectedQueue)
                return;

            if (_previewController is null || !_previewController.TryGetCurrentSnapshot(out var snapshot))
            {
                MessageBox.Show(this, "The document changed while print setup was open. Update preview and print again.",
                    "Print", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = CreateConfiguredPrintDialog(selectedQueue, settings);
            var device = new WriterPrintDialogDevice(dialog);
            var analysis = _printService.Analyze(snapshot, device.Capabilities);
            if (analysis.HasConflicts && MessageBox.Show(this, FormatPrintConflicts(analysis),
                    "Printer limits", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            _printService.Print(snapshot, device, new WriterPrintOptions
            {
                ConflictBehavior = WriterPrintConflictBehavior.ReportOnly,
                DocumentName = Shell.CurrentDocument.Path is null
                    ? "RibbonKit Writer - Untitled"
                    : $"RibbonKit Writer - {System.IO.Path.GetFileName(Shell.CurrentDocument.Path)}"
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or PrintSystemException)
        {
            MessageBox.Show(this, exception.Message, "Print", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            foreach (var queue in queues)
                queue.Dispose();
        }
    }

    private static PrintDialog CreateConfiguredPrintDialog(PrintQueue queue,
        DocumentPageSettings settings) => new()
    {
        PrintQueue = queue,
        PrintTicket = new PrintTicket
        {
            PageMediaSize = new PageMediaSize(settings.PortraitWidthDip, settings.PortraitHeightDip),
            PageOrientation = settings.Orientation == DocumentPageOrientation.Landscape
                ? PageOrientation.Landscape
                : PageOrientation.Portrait
        }
    };

    internal WriterPrintResult? TryPrintCurrentSnapshot(IWriterPrintDevice device,
        WriterPrintConflictBehavior conflictBehavior = WriterPrintConflictBehavior.ReportOnly)
    {
        ArgumentNullException.ThrowIfNull(device);
        if (_previewController is null || !_previewController.TryGetCurrentSnapshot(out var snapshot))
            return null;
        return _printService.Print(snapshot, device, new WriterPrintOptions
        {
            ConflictBehavior = conflictBehavior,
            DocumentName = "RibbonKit Writer integration test"
        });
    }

    private static string FormatPrintConflicts(WriterPrintAnalysis analysis)
    {
        var details = new List<string>();
        if (!analysis.AreCapabilitiesAvailable)
            details.Add("The printer did not report a complete printable area.");
        if (analysis.PageSizeMismatch is not null)
            details.Add("The selected printer paper differs from the preview page size.");
        details.AddRange(analysis.Conflicts.Select(conflict =>
            $"The {conflict.EdgeName.ToLowerInvariant()} margin may clip by {conflict.ClippingDip:0.#} DIP."));
        details.Add("Writer will keep the logical page and margins unchanged. Print anyway?");
        return string.Join(Environment.NewLine, details);
    }
}
