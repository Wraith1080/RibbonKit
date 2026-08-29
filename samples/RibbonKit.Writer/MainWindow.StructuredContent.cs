using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Controls;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Services.Documents;
using RibbonKit.Writer.View;

namespace RibbonKit.Writer;

public partial class MainWindow
{
    private const int QuickTableGridRowCount = 3;
    private readonly WriterImageService _writerImageService = new();
    private readonly WriterHyperlinkService _writerHyperlinkService = new();
    private readonly WriterDateTimeService _writerDateTimeService = new();
    private WriterTableInteractionController? _tableInteractionController;
    private WriterTableResizeController? _tableResizeController;
    private WriterStructuredContextResolver? _structuredContextResolver;
    private WriterPictureInteractionController? _pictureInteractionController;
    private Button? _customTableSizeButton;
    private bool _updatingTableGridSelection;

    /// <summary>Gets the app-owned bridge between table services and the live Writer surface.</summary>
    internal WriterTableInteractionController TableInteractionController =>
        _tableInteractionController ??
        throw new InvalidOperationException("The Writer table interaction controller has not been initialized.");

    /// <summary>Gets the app-owned explicit picture-selection and resize controller.</summary>
    internal WriterPictureInteractionController PictureInteractionController =>
        _pictureInteractionController ??
        throw new InvalidOperationException("The Writer picture interaction controller has not been initialized.");

    private void InitializeStructuredContent()
    {
        _tableInteractionController = new WriterTableInteractionController(
            DocumentEditor,
            () => CanEditTables);
        _tableInteractionController.StateChanged += OnTableInteractionStateChanged;
        _tableResizeController = new WriterTableResizeController(DocumentEditor,
            _tableInteractionController, CompleteStructuredContentMutation);
        EditingController.Editing.UndoExtension = _writerImageService;
        EditingController.Editing.UndoCompleted += OnEditingUndoCompleted;
        EditingController.Editing.RedoCompleted += OnEditingRedoCompleted;
        DocumentEditor.PreviewKeyDown += OnEditorPictureRemovalPreviewKeyDown;
        _structuredContextResolver = new WriterStructuredContextResolver(
            DocumentEditor, _tableInteractionController.Tables);
        _pictureInteractionController = new WriterPictureInteractionController(
            DocumentEditor, _writerImageService);
        _pictureInteractionController.StateChanged += OnPictureInteractionStateChanged;
        PopulateTableGridPicker();
        ApplyTableGridPopupSurface();
        ApplyStructuredContentCapabilityProjection();
    }

    private void ApplyTableGridPopupSurface()
    {
        // The shared InRibbonGallery popup lives in a separate HWND. Its template-level dynamic
        // background can remain unresolved there, exposing ribbon content behind the grid. Keep
        // this app-owned workaround until RKWF-013 is resolved in an approved RibbonKit packet.
        TableGridPicker.ApplyTemplate();
        if (TableGridPicker.Template.FindName("PART_PopupHost", TableGridPicker) is Border popupHost)
        {
            popupHost.Background = SystemParameters.HighContrast
                ? SystemColors.WindowBrush
                : TryFindResource("RibbonKit.Brushes.Ribbon.ContentBackground") as Brush
                    ?? SystemColors.WindowBrush;
        }
    }

    private void DisposeStructuredContent()
    {
        foreach (var item in TableGridPicker.Items.OfType<RibbonGalleryItem>())
        {
            item.PreviewMouseLeftButtonUp -= OnTableGridItemMouseUp;
            if (item.Content is Button invokeButton)
                invokeButton.Click -= OnTableGridInvokeClick;
        }
        if (_customTableSizeButton is not null)
        {
            _customTableSizeButton.Click -= OnCustomTableSizeClick;
            _customTableSizeButton = null;
        }

        if (_tableInteractionController is null)
            return;
        EditingController.Editing.UndoCompleted -= OnEditingUndoCompleted;
        EditingController.Editing.RedoCompleted -= OnEditingRedoCompleted;
        EditingController.Editing.UndoExtension = null;
        DocumentEditor.PreviewKeyDown -= OnEditorPictureRemovalPreviewKeyDown;
        if (_pictureInteractionController is not null)
        {
            _pictureInteractionController.StateChanged -= OnPictureInteractionStateChanged;
            _pictureInteractionController.Dispose();
            _pictureInteractionController = null;
        }
        _tableResizeController?.Dispose();
        _tableResizeController = null;
        _tableInteractionController.StateChanged -= OnTableInteractionStateChanged;
        _tableInteractionController.Dispose();
        _tableInteractionController = null;
        _structuredContextResolver = null;
    }

    private void AttachStructuredContextMenu(WriterEditorContextMenuController controller) =>
        controller.ExtensionsRequested += OnEditorContextMenuExtensionsRequested;

    private void DetachStructuredContextMenu()
    {
        if (_editorContextMenuController is not null)
        {
            _editorContextMenuController.ExtensionsRequested -=
                OnEditorContextMenuExtensionsRequested;
        }
    }

    private void OnEditorContextMenuExtensionsRequested(
        object? sender,
        WriterEditorContextMenuExtensionContext context)
    {
        if (_structuredContextResolver is null)
            return;
        var snapshot = _structuredContextResolver.Capture(context.Target);
        switch (snapshot.Kind)
        {
            case WriterStructuredContextKind.Table:
                AddTableContextMenu(context, snapshot);
                break;
            case WriterStructuredContextKind.Picture:
                AddPictureContextMenu(context, snapshot);
                break;
            case WriterStructuredContextKind.Hyperlink:
                AddHyperlinkContextMenu(context, snapshot);
                break;
        }
    }

    private void AddTableContextMenu(
        WriterEditorContextMenuExtensionContext context,
        WriterStructuredContextSnapshot snapshot)
    {
        var tableMenu = CreateContextSubmenu("Table", "WriterContextTable");
        var insertMenu = CreateContextSubmenu("Insert", "WriterContextTableInsert");
        insertMenu.Items.Add(CreateContextAction(context, snapshot, "Row Above",
            "WriterContextTableRowAbove", CanUseTableContext, current =>
                ExecuteTableCellContext(current, (tables, cell) =>
                    tables.InsertRows(cell, placement: WriterTableInsertPlacement.Before))));
        insertMenu.Items.Add(CreateContextAction(context, snapshot, "Row Below",
            "WriterContextTableRowBelow", CanUseTableContext, current =>
                ExecuteTableCellContext(current, (tables, cell) =>
                    tables.InsertRows(cell, placement: WriterTableInsertPlacement.After))));
        insertMenu.Items.Add(CreateContextAction(context, snapshot, "Column Left",
            "WriterContextTableColumnLeft", CanUseTableContext, current =>
                ExecuteTableCellContext(current, (tables, cell) =>
                    tables.InsertColumns(cell, placement: WriterTableInsertPlacement.Before))));
        insertMenu.Items.Add(CreateContextAction(context, snapshot, "Column Right",
            "WriterContextTableColumnRight", CanUseTableContext, current =>
                ExecuteTableCellContext(current, (tables, cell) =>
                    tables.InsertColumns(cell, placement: WriterTableInsertPlacement.After))));
        tableMenu.Items.Add(insertMenu);

        var deleteMenu = CreateContextSubmenu("Delete", "WriterContextTableDelete");
        deleteMenu.Items.Add(CreateContextAction(context, snapshot, "Row",
            "WriterContextTableDeleteRow", CanDeleteTableRowContext, current =>
                ExecuteTableCellContext(current, (tables, cell) => tables.DeleteRows(cell))));
        deleteMenu.Items.Add(CreateContextAction(context, snapshot, "Column",
            "WriterContextTableDeleteColumn", CanDeleteTableColumnContext, current =>
                ExecuteTableCellContext(current, (tables, cell) => tables.DeleteColumns(cell))));
        deleteMenu.Items.Add(CreateContextAction(context, snapshot, "Table",
            "WriterContextTableDeleteTable", CanUseTableContext, ExecuteDeleteTableContext));
        tableMenu.Items.Add(deleteMenu);
        tableMenu.Items.Add(new Separator());
        tableMenu.Items.Add(CreateContextAction(context, snapshot, "Merge Cells",
            "WriterContextTableMerge", CanMergeTableContext, current =>
                ExecuteTableContext(current, tables => tables.TryMergeSelection(out _))));
        tableMenu.Items.Add(CreateContextAction(context, snapshot, "Split Cell",
            "WriterContextTableSplit", CanSplitTableContext, current =>
                ExecuteTableContext(current, tables => tables.TrySplitCurrentCell())));

        var sizeMenu = CreateContextSubmenu("Cell Size", "WriterContextTableSize");
        foreach (var (header, height) in new[]
                 {
                     ("Compact Row", 24d), ("Standard Row", 32d), ("Tall Row", 48d)
                 })
        {
            sizeMenu.Items.Add(CreateContextAction(context, snapshot, header,
                $"WriterContextTable{header.Replace(" ", string.Empty)}", CanUseTableContext,
                current => ExecuteTableCellContext(current,
                    (tables, cell) => tables.SetRowHeight(cell, height))));
        }
        foreach (var (header, width) in new[]
                 {
                     ("Narrow Column", 80d), ("Standard Column", 120d), ("Wide Column", 160d)
                 })
        {
            sizeMenu.Items.Add(CreateContextAction(context, snapshot, header,
                $"WriterContextTable{header.Replace(" ", string.Empty)}", CanUseTableContext,
                current => ExecuteTableCellContext(current,
                    (tables, cell) => tables.SetCellWidth(cell,
                        new GridLength(width, GridUnitType.Pixel)))));
        }
        tableMenu.Items.Add(sizeMenu);

        var bordersMenu = CreateContextSubmenu("Borders", "WriterContextTableBorders");
        bordersMenu.Items.Add(CreateContextAction(context, snapshot, "All Borders",
            "WriterContextTableBordersAll", CanUseTableContext, current =>
                ExecuteTableContext(current, tables => tables.SetAllTableBorders(
                    current.Table!, GetTableBorderBrush(), new Thickness(1), new Thickness(0.5)))));
        bordersMenu.Items.Add(CreateContextAction(context, snapshot, "No Borders",
            "WriterContextTableBordersNone", CanUseTableContext, current =>
                ExecuteTableContext(current, tables => tables.SetAllTableBorders(
                    current.Table!, null, new Thickness(0), new Thickness(0)))));
        tableMenu.Items.Add(bordersMenu);

        var backgroundMenu = CreateContextSubmenu("Background", "WriterContextTableBackground");
        backgroundMenu.Items.Add(CreateContextAction(context, snapshot, "No Color",
            "WriterContextTableBackgroundNone", CanUseTableContext, current =>
                ExecuteTableContext(current,
                    tables => tables.SetTableBackground(current.Table!, null))));
        backgroundMenu.Items.Add(CreateContextAction(context, snapshot, "Soft Accent",
            "WriterContextTableBackgroundAccent", CanUseTableContext, current =>
                ExecuteTableContext(current, tables => tables.SetTableBackground(
                    current.Table!, GetTableBackgroundBrush()))));
        tableMenu.Items.Add(backgroundMenu);

        context.AddSeparator();
        context.AddItem(tableMenu);
    }

    private void AddPictureContextMenu(
        WriterEditorContextMenuExtensionContext context,
        WriterStructuredContextSnapshot snapshot)
    {
        var pictureMenu = CreateContextSubmenu("Picture", "WriterContextPicture");
        pictureMenu.Items.Add(CreateContextAction(context, snapshot, "Fit to Page Width",
            "WriterContextPictureFitWidth", CanUsePictureContext, ExecuteFitPictureContext));
        pictureMenu.Items.Add(CreateContextAction(context, snapshot, "Original Size",
            "WriterContextPictureOriginalSize", CanUsePictureContext, ExecuteOriginalPictureContext));
        pictureMenu.Items.Add(new Separator());
        pictureMenu.Items.Add(CreateContextAction(context, snapshot, "Remove Picture",
            "WriterContextPictureRemove", CanUsePictureContext, ExecuteRemovePictureContext));
        context.AddSeparator();
        context.AddItem(pictureMenu);
    }

    private void AddHyperlinkContextMenu(
        WriterEditorContextMenuExtensionContext context,
        WriterStructuredContextSnapshot snapshot)
    {
        var hyperlinkMenu = CreateContextSubmenu("Hyperlink", "WriterContextHyperlink");
        hyperlinkMenu.Items.Add(CreateContextAction(context, snapshot, "Edit Hyperlink...",
            "WriterContextHyperlinkEdit", CanUseHyperlinkContext, _ =>
            {
                ShowHyperlinkDialog();
                QueueStructuredContentEditorFocus();
            }));
        hyperlinkMenu.Items.Add(CreateContextAction(context, snapshot, "Remove Hyperlink",
            "WriterContextHyperlinkRemove", CanUseHyperlinkContext, ExecuteRemoveHyperlinkContext));
        context.AddSeparator();
        context.AddItem(hyperlinkMenu);
    }

    private MenuItem CreateContextAction(
        WriterEditorContextMenuExtensionContext context,
        WriterStructuredContextSnapshot snapshot,
        string header,
        string automationId,
        Func<WriterStructuredContextSnapshot, bool> canExecute,
        Action<WriterStructuredContextSnapshot> execute)
    {
        var item = context.CreateCallbackItem(header,
            _ => canExecute(snapshot), _ => execute(snapshot));
        AutomationProperties.SetAutomationId(item, automationId);
        AutomationProperties.SetName(item, header.TrimEnd('.'));
        return item;
    }

    private static MenuItem CreateContextSubmenu(string header, string automationId)
    {
        var item = new MenuItem { Header = header };
        AutomationProperties.SetAutomationId(item, automationId);
        AutomationProperties.SetName(item, header);
        return item;
    }

    private bool CanUseTableContext(WriterStructuredContextSnapshot snapshot) =>
        !_closing && !Shell.IsBusy && CanEditTables
        && _structuredContextResolver?.IsCurrent(snapshot) == true;

    private bool CanDeleteTableRowContext(WriterStructuredContextSnapshot snapshot) =>
        CanUseTableContext(snapshot)
        && _structuredContextResolver!.TryGetTableCell(snapshot, out var cell)
        && cell.RowGroup.Rows.Count > 1;

    private bool CanDeleteTableColumnContext(WriterStructuredContextSnapshot snapshot) =>
        CanUseTableContext(snapshot)
        && _structuredContextResolver!.TryGetTableCell(snapshot, out var cell)
        && CanDeleteCurrentTableColumn(cell);

    private bool CanMergeTableContext(WriterStructuredContextSnapshot snapshot) =>
        CanUseTableContext(snapshot)
        && _structuredContextResolver!.TryGetTableRange(snapshot, out var range)
        && (range.RowCount > 1 || range.ColumnCount > 1);

    private bool CanSplitTableContext(WriterStructuredContextSnapshot snapshot) =>
        CanUseTableContext(snapshot)
        && _structuredContextResolver!.TryGetTableCell(snapshot, out var cell)
        && (cell.RowSpan > 1 || cell.ColumnSpan > 1);

    private bool CanUsePictureContext(WriterStructuredContextSnapshot snapshot) =>
        !_closing && !Shell.IsBusy && DocumentEditor.IsEnabled && !DocumentEditor.IsReadOnly
        && CurrentProfile.Preserves(WriterDocumentContentCapabilities.Images)
        && _structuredContextResolver?.IsCurrent(snapshot) == true;

    private bool CanUseHyperlinkContext(WriterStructuredContextSnapshot snapshot) =>
        !_closing && !Shell.IsBusy && DocumentEditor.IsEnabled && !DocumentEditor.IsReadOnly
        && CurrentProfile.Preserves(WriterDocumentContentCapabilities.Hyperlinks)
        && _structuredContextResolver?.IsCurrent(snapshot) == true;

    private void ExecuteTableCellContext(
        WriterStructuredContextSnapshot snapshot,
        Func<WriterTableService, WriterTableCellReference, bool> mutation)
    {
        if (!CanUseTableContext(snapshot))
            return;
        _ = MutateTable(tables =>
            _structuredContextResolver?.TryGetTableCell(snapshot, out var cell) == true
            && mutation(tables, cell));
    }

    private void ExecuteTableContext(
        WriterStructuredContextSnapshot snapshot,
        Func<WriterTableService, bool> mutation)
    {
        if (!CanUseTableContext(snapshot))
            return;
        _ = MutateTable(mutation);
    }

    private void ExecuteDeleteTableContext(WriterStructuredContextSnapshot snapshot)
    {
        if (!CanUseTableContext(snapshot) || snapshot.Table is null)
            return;
        _ = MutateTable(tables => tables.DeleteTable(snapshot.Table));
    }

    private void ExecuteRemovePictureContext(WriterStructuredContextSnapshot snapshot)
    {
        if (!CanUsePictureContext(snapshot) || snapshot.Picture is null)
            return;
        _pictureInteractionController?.ClearSelection();
        if (_writerImageService.TryRemoveImage(DocumentEditor, snapshot.Picture))
            CompleteStructuredContentMutation();
    }

    private void ExecuteFitPictureContext(WriterStructuredContextSnapshot snapshot)
    {
        if (!CanUsePictureContext(snapshot) || snapshot.Picture is null
            || !WriterInlineInsertion.TryGetImage(snapshot.Picture, out var image)
            || _pictureInteractionController?.SelectPicture(snapshot.Picture) != true)
            return;
        var size = GetPictureDisplayedSize(image);
        var maximum = _pictureInteractionController.MaximumSize;
        var scale = Math.Min(maximum.Width / size.Width, maximum.Height / size.Height);
        if (_pictureInteractionController.TrySetSize(size.Width * scale, size.Height * scale))
            CompleteStructuredContentMutation();
    }

    private void ExecuteOriginalPictureContext(WriterStructuredContextSnapshot snapshot)
    {
        if (!CanUsePictureContext(snapshot) || snapshot.Picture is null
            || !WriterInlineInsertion.TryGetImage(snapshot.Picture, out var image)
            || image.Source is null
            || _pictureInteractionController?.SelectPicture(snapshot.Picture) != true)
            return;
        var maximum = _pictureInteractionController.MaximumSize;
        var scale = Math.Min(1d, Math.Min(maximum.Width / image.Source.Width,
            maximum.Height / image.Source.Height));
        if (_pictureInteractionController.TrySetSize(image.Source.Width * scale,
                image.Source.Height * scale))
            CompleteStructuredContentMutation();
    }

    private void ExecuteRemoveHyperlinkContext(WriterStructuredContextSnapshot snapshot)
    {
        if (!CanUseHyperlinkContext(snapshot) || snapshot.Hyperlink is null)
            return;
        if (_writerHyperlinkService.TryRemove(DocumentEditor, snapshot.Hyperlink))
            CompleteStructuredContentMutation();
    }

    private void OnEditingUndoCompleted(object? sender, EventArgs e)
    {
        if (_writerImageService.TryRestoreAfterUndo(DocumentEditor))
            MarkPreviewPending();
        _pictureInteractionController?.Refresh();
    }

    private void OnEditingRedoCompleted(object? sender, EventArgs e)
    {
        _writerImageService.NotifyRedo(DocumentEditor);
        _pictureInteractionController?.Refresh();
    }

    private void OnEditorPictureRemovalPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled || e.KeyboardDevice.Modifiers != ModifierKeys.None
            || e.Key is not (Key.Delete or Key.Back))
            return;

        var picture = WriterInlineInsertion.FindImageForKeyboardRemoval(
            DocumentEditor, backward: e.Key == Key.Back);
        if (picture is null || !_writerImageService.TryRemoveImage(DocumentEditor, picture))
            return;

        e.Handled = true;
        _pictureInteractionController?.ClearSelection();
        CompleteStructuredContentMutation();
    }

    private void PopulateTableGridPicker()
    {
        TableGridPicker.Items.Clear();
        for (var rows = 1; rows <= QuickTableGridRowCount; rows++)
        {
            for (var columns = 1; columns <= WriterTableService.MaximumStructuralCount; columns++)
            {
                var choice = new WriterTableGridChoice(rows, columns);
                var invokeButton = new Button
                {
                    Tag = choice,
                    Content = new WriterTableGridCellPreview { IsHitTestVisible = false },
                    Width = 20,
                    Height = 20,
                    Padding = new Thickness(0),
                    BorderThickness = new Thickness(0),
                    Background = Brushes.Transparent,
                    Focusable = false,
                    IsTabStop = false,
                    IsHitTestVisible = false
                };
                invokeButton.Click += OnTableGridInvokeClick;
                AutomationProperties.SetAutomationId(
                    invokeButton,
                    $"InsertTable{rows}x{columns}Invoke");
                AutomationProperties.SetName(invokeButton, $"Insert {rows} by {columns} table");
                AutomationProperties.SetHelpText(
                    invokeButton,
                    $"Invoke to insert a live table with {rows} rows and {columns} columns.");

                var item = new RibbonGalleryItem
                {
                    Tag = choice,
                    Content = invokeButton,
                    ToolTip = new RibbonScreenTip
                    {
                        Title = $"{rows} × {columns} table",
                        Description = "Insert this live table. RibbonKit Writer files preserve its supported structure and formatting."
                    }
                };
                item.PreviewMouseLeftButtonUp += OnTableGridItemMouseUp;
                Ribbon.SetCommandId(item, $"Writer.Insert.Table.{rows}x{columns}");
                AutomationProperties.SetAutomationId(item, $"InsertTable{rows}x{columns}");
                AutomationProperties.SetName(item, $"{rows} by {columns} table");
                AutomationProperties.SetHelpText(item,
                    $"Insert a live table with {rows} rows and {columns} columns. RibbonKit Writer files preserve its supported structure and formatting.");
                TableGridPicker.Items.Add(item);
            }
        }

        var customButton = new Button
        {
            Content = "Custom Table…",
            Width = 236,
            Height = 30,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(9, 0, 9, 0)
        };
        customButton.Click += OnCustomTableSizeClick;
        Ribbon.SetCommandId(customButton, "Writer.Insert.Table.Custom");
        AutomationProperties.SetAutomationId(customButton, "InsertCustomTableSize");
        AutomationProperties.SetName(customButton, "Custom Table");
        AutomationProperties.SetHelpText(customButton,
            "Enter a table size from 1 through 8 rows and columns.");

        var footer = new StackPanel { Width = 236 };
        footer.Children.Add(new Separator { Margin = new Thickness(0, 0, 0, 5) });
        footer.Children.Add(customButton);
        var footerItem = new RibbonGalleryItem
        {
            Content = footer,
            Width = 256,
            Focusable = false,
            IsTabStop = false,
            ToolTip = new RibbonScreenTip
            {
                Title = "Custom Table",
                Description = "Enter rows and columns manually for another supported table size."
            }
        };
        AutomationProperties.SetAutomationId(footerItem, "InsertCustomTableFooter");
        AutomationProperties.SetName(footerItem, "Custom table size");
        TableGridPicker.Items.Add(footerItem);
        _customTableSizeButton = customButton;
    }

    private void OnTableGridSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingTableGridSelection)
            return;
        if (TryGetTableGridChoice(TableGridPicker.SelectedItem, out var choice))
            UpdateTableGridHighlight(choice);
        else
            ClearTableGridHighlight();
    }

    private void OnTableGridItemPreview(object sender, RibbonGalleryPreviewEventArgs e)
    {
        if (TryGetTableGridChoice(e.PreviewedItem, out var choice))
            UpdateTableGridHighlight(choice);
        else
            ClearTableGridHighlight();
    }

    private void OnTableGridItemPreviewCancelled(object sender, RoutedEventArgs e)
    {
        if (TryGetTableGridChoice(TableGridPicker.SelectedItem, out var choice))
            UpdateTableGridHighlight(choice);
        else
            ClearTableGridHighlight();
    }

    private void OnTableGridItemMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (TryGetTableGridChoice(sender, out var choice))
            QueueTableGridChoiceCommit(choice);
    }

    private void OnTableGridInvokeClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WriterTableGridChoice choice })
            QueueTableGridChoiceCommit(choice);
    }

    private void OnCustomTableSizeClick(object sender, RoutedEventArgs e)
    {
        TableGridPicker.IsDropDownOpen = false;
        ClearTableGridHighlight();
        if (!CanEditTables)
            return;
        var dialog = new WriterTableSizeDialog
        {
            Owner = this,
            FlowDirection = FlowDirection
        };
        if (dialog.ShowDialog() == true && !TryInsertTable(dialog.Rows, dialog.Columns))
        {
            MessageBox.Show(this,
                "Place an empty caret at the beginning or end of a paragraph before inserting a table.",
                "Insert Table", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        QueueStructuredContentEditorFocus();
    }

    private void QueueTableGridChoiceCommit(WriterTableGridChoice choice)
    {
        // InRibbonGallery must finish its input route and release mouse capture before the shared
        // strip/popup presenter is re-homed. The same deferred path also gives UIA Invoke a stable
        // asynchronous command boundary.
        _ = Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() => CommitTableGridChoice(choice)));
    }

    private void OnTableGridPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, _customTableSizeButton) ||
            _customTableSizeButton?.IsKeyboardFocusWithin == true)
            return;
        if (e.Key is Key.Enter or Key.Return or Key.Space)
        {
            var selected = TableGridPicker.SelectedItem ?? TableGridPicker.Items[0];
            if (TryGetTableGridChoice(selected, out var choice))
            {
                QueueTableGridChoiceCommit(choice);
                e.Handled = true;
            }
            return;
        }

        if (e.Key is not (Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End))
            return;

        var current = TryGetTableGridChoice(TableGridPicker.SelectedItem, out var selectedChoice)
            ? selectedChoice
            : new WriterTableGridChoice(1, 1);
        var rows = current.Rows;
        var columns = current.Columns;
        // Keep the gallery template LTR so its shared strip/popup presenter remains
        // visible in an RTL ribbon. The physical arrow semantics still follow the
        // document window's direction.
        var rightToLeft = FlowDirection == FlowDirection.RightToLeft;
        switch (e.Key)
        {
            case Key.Left:
                columns += rightToLeft ? 1 : -1;
                break;
            case Key.Right:
                columns += rightToLeft ? -1 : 1;
                break;
            case Key.Up:
                rows--;
                break;
            case Key.Down:
                rows++;
                break;
            case Key.Home:
                rows = columns = 1;
                break;
            case Key.End:
                rows = QuickTableGridRowCount;
                columns = WriterTableService.MaximumStructuralCount;
                break;
        }

        rows = Math.Clamp(rows, 1, QuickTableGridRowCount);
        columns = Math.Clamp(columns, 1, WriterTableService.MaximumStructuralCount);
        SelectTableGridChoice(new WriterTableGridChoice(rows, columns));
        e.Handled = true;
    }

    private void SelectTableGridChoice(WriterTableGridChoice choice)
    {
        var item = TableGridPicker.Items.OfType<RibbonGalleryItem>()
            .First(candidate => candidate.Tag is WriterTableGridChoice itemChoice
                && itemChoice == choice);
        TableGridPicker.SelectedItem = item;
        TableGridPicker.ScrollIntoView(item);
        UpdateTableGridHighlight(choice);
    }

    private void CommitTableGridChoice(WriterTableGridChoice choice)
    {
        TableGridPicker.IsDropDownOpen = false;
        _updatingTableGridSelection = true;
        try
        {
            TableGridPicker.SelectedItem = null;
        }
        finally
        {
            _updatingTableGridSelection = false;
        }
        ClearTableGridHighlight();
        QueueStructuredContentAction(
            () => TryInsertTable(choice.Rows, choice.Columns),
            "Place an empty caret at the beginning or end of a paragraph before inserting a table.",
            "Insert Table");
    }

    private static bool TryGetTableGridChoice(object? value, out WriterTableGridChoice choice)
    {
        if (value is RibbonGalleryItem { Tag: WriterTableGridChoice itemChoice })
        {
            choice = itemChoice;
            return true;
        }
        choice = default;
        return false;
    }

    private void UpdateTableGridHighlight(WriterTableGridChoice choice)
    {
        foreach (var item in TableGridPicker.Items.OfType<RibbonGalleryItem>())
        {
            if (item.Tag is not WriterTableGridChoice itemChoice ||
                item.Content is not Button { Content: WriterTableGridCellPreview preview })
                continue;
            preview.IsHighlighted = itemChoice.Rows <= choice.Rows
                && itemChoice.Columns <= choice.Columns;
        }
        AutomationProperties.SetHelpText(TableGridPicker,
            $"Selected {choice.Rows} by {choice.Columns} table. Press Enter to insert. RibbonKit Writer files preserve supported tables.");
    }

    private void ClearTableGridHighlight()
    {
        foreach (var preview in TableGridPicker.Items.OfType<RibbonGalleryItem>()
                     .Select(item => item.Content).OfType<Button>()
                     .Select(button => button.Content).OfType<WriterTableGridCellPreview>())
            preview.IsHighlighted = false;
        AutomationProperties.SetHelpText(TableGridPicker,
            "Use arrow keys for a quick table up to 3 by 8, or choose Custom Table for another supported size. RibbonKit Writer files preserve supported tables.");
    }

    private void OnInsertPictureClick(object sender, RoutedEventArgs e)
    {
        if (!CurrentProfile.Preserves(WriterDocumentContentCapabilities.Images))
            return;
        var dialog = new WriterPictureInsertDialog
        {
            Owner = this,
            FlowDirection = FlowDirection
        };
        if (dialog.ShowDialog() == true && dialog.SelectedPath is { } path
            && !TryInsertPicture(path))
        {
            MessageBox.Show(this, "Writer could not validate or insert that picture.",
                "Insert Picture", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        QueueStructuredContentEditorFocus();
    }

    private void OnInsertHyperlinkClick(object sender, RoutedEventArgs e)
    {
        ShowHyperlinkDialog();
        QueueStructuredContentEditorFocus();
    }

    private void ShowHyperlinkDialog()
    {
        if (!CurrentProfile.Preserves(WriterDocumentContentCapabilities.Hyperlinks))
            return;
        var existing = WriterInlineInsertion.FindHyperlink(DocumentEditor);
        var currentText = existing is null
            ? null
            : new TextRange(existing.ContentStart, existing.ContentEnd).Text.TrimEnd('\r', '\n');
        var dialog = new WriterHyperlinkDialog(existing?.NavigateUri?.OriginalString, currentText)
        {
            Owner = this,
            FlowDirection = FlowDirection
        };
        if (dialog.ShowDialog() == true && dialog.Address is { } address
            && !TryInsertOrEditHyperlink(address, dialog.DisplayText))
        {
            MessageBox.Show(this, "Writer could not insert or update that hyperlink at the current selection.",
                "Hyperlink", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void OnInsertDateTimeClick(object sender, RoutedEventArgs e)
    {
        if (!SupportsProfileCommand(WriterDocumentCommandCapabilities.TextEditing))
            return;
        var dialog = new WriterDateTimeDialog
        {
            Owner = this,
            FlowDirection = FlowDirection
        };
        if (dialog.ShowDialog() == true && dialog.ResultValue is { } value
            && dialog.ResultFormat is { } format
            && !TryInsertDateTime(value, format))
        {
            MessageBox.Show(this, "Writer could not insert the date and time at the current selection.",
                "Insert Date and Time", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        QueueStructuredContentEditorFocus();
    }

    internal bool TryInsertPicture(string path)
    {
        if (!CurrentProfile.Preserves(WriterDocumentContentCapabilities.Images))
            return false;
        var changed = _writerImageService.TryInsertImage(DocumentEditor, path);
        if (changed)
            CompleteStructuredContentMutation();
        return changed;
    }

    internal bool TryInsertOrEditHyperlink(string address, string? displayText = null)
    {
        if (!CurrentProfile.Preserves(WriterDocumentContentCapabilities.Hyperlinks))
            return false;
        var changed = WriterInlineInsertion.FindHyperlink(DocumentEditor) is null
            ? _writerHyperlinkService.TryCreate(DocumentEditor, address, displayText)
            : _writerHyperlinkService.TryEdit(DocumentEditor, address, displayText);
        if (changed)
            CompleteStructuredContentMutation();
        return changed;
    }

    internal bool TryInsertDateTime(DateTimeOffset value, string format)
    {
        if (!SupportsProfileCommand(WriterDocumentCommandCapabilities.TextEditing))
            return false;
        var changed = _writerDateTimeService.TryInsert(DocumentEditor, value, format,
            CultureInfo.CurrentCulture);
        if (changed)
            CompleteStructuredContentMutation();
        return changed;
    }

    internal bool TryInsertTable(int rows, int columns)
    {
        if (!CanEditTables || rows is < 1 or > WriterTableService.MaximumStructuralCount
            || columns is < 1 or > WriterTableService.MaximumStructuralCount)
            return false;
        var changed = TableInteractionController.Tables.InsertTable(
            rows, columns, GetTableBorderBrush()) is not null;
        if (changed)
            CompleteStructuredContentMutation();
        return changed;
    }

    private void OnInsertTableRowAboveClick(object sender, RoutedEventArgs e) =>
        QueueStructuredContentAction(() => MutateCurrentCell((tables, cell) =>
            tables.InsertRows(cell, placement: WriterTableInsertPlacement.Before)));

    private void OnInsertTableRowBelowClick(object sender, RoutedEventArgs e) =>
        QueueStructuredContentAction(() => MutateCurrentCell((tables, cell) =>
            tables.InsertRows(cell, placement: WriterTableInsertPlacement.After)));

    private void OnInsertTableColumnLeftClick(object sender, RoutedEventArgs e) =>
        QueueStructuredContentAction(() => MutateCurrentCell((tables, cell) =>
            tables.InsertColumns(cell, placement: WriterTableInsertPlacement.Before)));

    private void OnInsertTableColumnRightClick(object sender, RoutedEventArgs e) =>
        QueueStructuredContentAction(() => MutateCurrentCell((tables, cell) =>
            tables.InsertColumns(cell, placement: WriterTableInsertPlacement.After)));

    private void OnDeleteTableRowClick(object sender, RoutedEventArgs e) =>
        QueueStructuredContentAction(() => MutateCurrentCell((tables, cell) => tables.DeleteRows(cell)));

    private void OnDeleteTableColumnClick(object sender, RoutedEventArgs e) =>
        QueueStructuredContentAction(() => MutateCurrentCell((tables, cell) => tables.DeleteColumns(cell)));

    private void OnMergeTableCellsClick(object sender, RoutedEventArgs e) =>
        QueueStructuredContentAction(() => MutateTable(tables => tables.TryMergeSelection(out _)));

    private void OnSplitTableCellClick(object sender, RoutedEventArgs e) =>
        QueueStructuredContentAction(() => MutateTable(tables => tables.TrySplitCurrentCell()));

    private void OnInsertTableLiteralTabClick(object sender, RoutedEventArgs e) =>
        QueueStructuredContentAction(() => MutateTable(_ => TableInteractionController.TryInsertLiteralTab()));

    private void OnTableRowHeightClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag }
            || !double.TryParse(tag, NumberStyles.Number, CultureInfo.InvariantCulture, out var height))
            return;
        QueueStructuredContentAction(() => MutateCurrentCell((tables, cell) =>
            tables.SetRowHeight(cell, height)));
    }

    private void OnTableColumnWidthClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag }
            || !double.TryParse(tag, NumberStyles.Number, CultureInfo.InvariantCulture, out var width))
            return;
        QueueStructuredContentAction(() => MutateCurrentCell((tables, cell) =>
            tables.SetCellWidth(cell, new GridLength(width, GridUnitType.Pixel))));
    }

    private void OnDistributeTableRowsClick(object sender, RoutedEventArgs e) =>
        QueueStructuredContentAction(() => MutateTable(tables =>
        {
            if (!tables.TryGetSelectionRange(out var range)
                || !tables.TryGetCell(DocumentEditor.Selection.Start, out var first))
                return false;
            return tables.DistributeRows(first, range.RowCount, range.RowCount * 32d);
        }));

    private void OnDistributeTableColumnsClick(object sender, RoutedEventArgs e) =>
        QueueStructuredContentAction(() => MutateTable(tables =>
        {
            if (!tables.TryGetSelectionRange(out var range))
                return false;
            return tables.DistributeColumns(range.Table, range.StartColumn, range.ColumnCount,
                range.ColumnCount * 120d);
        }));

    private void OnTableAlignmentClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag }
            || !Enum.TryParse<TextAlignment>(tag, out var alignment))
            return;
        QueueStructuredContentAction(() => MutateCurrentCell((tables, cell) =>
            tables.SetCellAlignment(cell, alignment)));
    }

    private void OnTableHorizontalAlignmentClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag }
            || !Enum.TryParse<WriterTableHorizontalAlignment>(tag, out var alignment))
            return;
        QueueStructuredContentAction(() => MutateCurrentTable((tables, table) =>
        {
            if (!WriterTableLayoutResolver.TryCreate(DocumentEditor,
                    TableInteractionController.GetOrderedCells(table),
                    TableInteractionController.CurrentCell?.GroupIndex ?? 0, out var layout))
                return false;
            var pageWidth = DocumentEditor.Document.PageWidth;
            var padding = DocumentEditor.Document.PagePadding;
            var availableWidth = double.IsFinite(pageWidth)
                ? pageWidth - padding.Left - padding.Right
                : DocumentEditor.ActualWidth - DocumentEditor.Padding.Left - DocumentEditor.Padding.Right;
            return tables.SetTableHorizontalAlignment(table, alignment,
                layout.Bounds.Width / layout.ProjectionScaleX, availableWidth);
        }));
    }

    private void OnTableBordersClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag })
            return;
        QueueStructuredContentAction(() => MutateCurrentTable((tables, table) =>
            tag == "None"
                ? tables.SetAllTableBorders(table, null, new Thickness(0), new Thickness(0))
                : tables.SetAllTableBorders(table, GetTableBorderBrush(), new Thickness(1),
                    new Thickness(0.5))));
    }

    private void OnTableBackgroundClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string tag })
            return;
        QueueStructuredContentAction(() => MutateCurrentTable((tables, table) =>
            tables.SetTableBackground(table, tag == "None" ? null : GetTableBackgroundBrush())));
    }

    private bool MutateCurrentCell(
        Func<WriterTableService, WriterTableCellReference, bool> mutation) =>
        MutateTable(tables => tables.TryGetCellAtCaret(out var cell) && mutation(tables, cell));

    private bool MutateCurrentTable(Func<WriterTableService, Table, bool> mutation) =>
        MutateTable(tables => tables.TryGetCellAtCaret(out var cell)
            && mutation(tables, cell.Table));

    private bool MutateTable(Func<WriterTableService, bool> mutation)
    {
        if (!CanEditTables || _tableInteractionController is null)
            return false;
        using (_tableInteractionController.DeferRefresh())
        {
            var changed = mutation(_tableInteractionController.Tables);
            if (changed)
                CompleteStructuredContentMutation();
            return changed;
        }
    }

    private Brush GetTableBorderBrush() => SystemParameters.HighContrast
        ? SystemColors.WindowTextBrush
        : TryFindResource("RibbonKit.Brushes.Text.Secondary") as Brush
            ?? SystemColors.ControlDarkBrush;

    private Brush GetTableBackgroundBrush() => SystemParameters.HighContrast
        ? SystemColors.HighlightBrush
        : TryFindResource("RibbonKit.Brushes.Control.CheckedBackground") as Brush
            ?? SystemColors.ControlLightBrush;

    private bool CanEditTables => _tableInteractionController is not null
        && SupportsProfileCommand(WriterDocumentCommandCapabilities.TableEditing)
        && _tableInteractionController.Tables.CanMutate;

    private void CompleteStructuredContentMutation()
    {
        Shell.MarkEditorDirty();
        MarkPreviewPending();
        EditingController.RefreshState();
        _tableInteractionController?.Refresh();
        QueueStructuredContentEditorFocus();
    }

    private void QueueStructuredContentAction(Func<bool> action, string? failureMessage = null,
        string failureTitle = "Table Tools")
    {
        var document = Shell.CurrentDocument;
        var editorDocument = DocumentEditor.Document;
        _ = Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            if (_closing || !IsVisible || Shell.IsBusy
                || !ReferenceEquals(Shell.CurrentDocument, document)
                || !ReferenceEquals(DocumentEditor.Document, editorDocument))
                return;
            if (action())
                return;
            if (failureMessage is not null)
            {
                MessageBox.Show(this, failureMessage, failureTitle,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            QueueStructuredContentEditorFocus();
        }));
    }

    private void QueueStructuredContentEditorFocus()
    {
        _ = Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            if (!IsVisible || _closing || CurrentViewMode == WriterViewMode.PrintPreview
                || Shell.IsBusy || MainRibbon.IsBackstageOpen || MainRibbon.IsModal)
                return;
            FocusManager.SetFocusedElement(this, DocumentEditor);
            DocumentEditor.Focus();
            Keyboard.Focus(DocumentEditor);
            EditingController.RefreshState();
            _tableInteractionController?.Refresh();
        }));
    }

    private void OnTableInteractionStateChanged(object? sender, EventArgs e) =>
        RefreshStructuredContentState();

    private void OnPictureInteractionStateChanged(object? sender, EventArgs e) =>
        RefreshStructuredContentState();

    private void RefreshStructuredContentState()
    {
        if (!IsInitialized || _tableInteractionController is null)
            return;

        var inTable = CurrentViewMode != WriterViewMode.PrintPreview
            && _tableInteractionController.IsInTable;
        var canEdit = inTable && CanEditTables;
        _tableResizeController?.SetEnabled(canEdit);
        TableToolsTab.Visibility = inTable ? Visibility.Visible : Visibility.Collapsed;
        TableToolsTab.IsEnabled = canEdit;
        TableRowsColumnsGroup.IsEnabled = canEdit;
        TableMergeGroup.IsEnabled = canEdit;
        TableSizeGroup.IsEnabled = canEdit;
        TableAlignmentGroup.IsEnabled = canEdit;
        TableDesignGroup.IsEnabled = canEdit;

        var hasPicture = CurrentViewMode != WriterViewMode.PrintPreview
            && _pictureInteractionController?.HasSelection == true;
        var canEditPicture = hasPicture && DocumentEditor.IsEnabled && !DocumentEditor.IsReadOnly
            && CurrentProfile.Preserves(WriterDocumentContentCapabilities.Images);
        if (!hasPicture && ReferenceEquals(MainRibbon.SelectedTab, PictureToolsTab))
            MainRibbon.SelectedTab = HomeTab;
        PictureToolsTab.Visibility = hasPicture ? Visibility.Visible : Visibility.Collapsed;
        PictureToolsTab.IsEnabled = canEditPicture;
        PictureSizeGroup.IsEnabled = canEditPicture;
        PictureActionsGroup.IsEnabled = canEditPicture;
        ApplyPictureSizeButton.IsEnabled = canEditPicture;
        RemoveSelectedPictureButton.IsEnabled = canEditPicture;
        UpdatePictureSizeFields();

        InsertTableRowAboveButton.IsEnabled = canEdit;
        InsertTableRowBelowButton.IsEnabled = canEdit;
        InsertTableColumnLeftButton.IsEnabled = canEdit;
        InsertTableColumnRightButton.IsEnabled = canEdit;
        var currentCell = _tableInteractionController.CurrentCell;
        DeleteTableRowButton.IsEnabled = canEdit
            && currentCell is { } rowCell
            && rowCell.RowGroup.Rows.Count > 1;
        DeleteTableColumnButton.IsEnabled = canEdit
            && CanDeleteCurrentTableColumn(currentCell);
        MergeTableCellsButton.IsEnabled = canEdit && _tableInteractionController.CanMerge;
        SplitTableCellButton.IsEnabled = canEdit && _tableInteractionController.CanSplit;
        InsertTableLiteralTabButton.IsEnabled = canEdit && IsSelectionInsideOneTableCell();
        TableRowHeightButton.IsEnabled = canEdit;
        TableColumnWidthButton.IsEnabled = canEdit;
        TableAlignmentButton.IsEnabled = canEdit;
        TableHorizontalAlignmentButton.IsEnabled = canEdit;
        TableBordersButton.IsEnabled = canEdit;
        TableBackgroundButton.IsEnabled = canEdit;

        var hasRange = _tableInteractionController.TryGetSelectionRange(out var range);
        DistributeTableRowsButton.IsEnabled = canEdit && hasRange && range.RowCount > 1;
        DistributeTableColumnsButton.IsEnabled = canEdit && hasRange && range.ColumnCount > 1;
    }

    private bool CanDeleteCurrentTableColumn(WriterTableCellReference? currentCell)
    {
        if (_tableInteractionController is null || currentCell is not { } current)
            return false;
        var cells = _tableInteractionController.GetOrderedCells(current.Table);
        var groupWidths = cells
            .GroupBy(cell => cell.GroupIndex)
            .ToDictionary(group => group.Key, group => group.Max(cell => cell.LastColumn) + 1);
        if (groupWidths.Count != current.Table.RowGroups.Count
            || !groupWidths.TryGetValue(current.GroupIndex, out var currentWidth))
            return false;

        var tableWidth = groupWidths.Values.Max();
        return currentWidth > 1
            && currentWidth == tableWidth
            && groupWidths.Values.All(width => width > 1 || current.Column >= width);
    }

    private bool IsSelectionInsideOneTableCell()
    {
        if (_tableInteractionController is null
            || !_tableInteractionController.Tables.TryGetCell(DocumentEditor.Selection.Start, out var first)
            || !_tableInteractionController.Tables.TryGetCell(DocumentEditor.Selection.End, out var last))
            return false;
        return ReferenceEquals(first.Cell, last.Cell);
    }

    private void ApplyStructuredContentCapabilityProjection()
    {
        if (!IsInitialized || _tableInteractionController is null)
            return;
        var writable = DocumentEditor.IsEnabled && !DocumentEditor.IsReadOnly;
        var canText = writable && SupportsProfileCommand(WriterDocumentCommandCapabilities.TextEditing);
        var canImage = writable
            && CurrentProfile.Preserves(WriterDocumentContentCapabilities.Images);
        var canHyperlink = writable
            && CurrentProfile.Preserves(WriterDocumentContentCapabilities.Hyperlinks);
        var canTable = writable
            && SupportsProfileCommand(WriterDocumentCommandCapabilities.TableEditing);

        InsertTab.IsEnabled = canText;
        InsertTextGroup.IsEnabled = canText;
        InsertDateTimeButton.IsEnabled = canText;
        InsertIllustrationsGroup.IsEnabled = canImage;
        InsertPictureButton.IsEnabled = canImage;
        InsertLinksGroup.IsEnabled = canHyperlink;
        InsertHyperlinkButton.IsEnabled = canHyperlink;
        InsertTablesGroup.IsEnabled = canTable;
        TableGridPicker.IsEnabled = canTable;
        RefreshStructuredContentState();
    }

    private void OnApplyPictureSizeClick(object sender, RoutedEventArgs e)
    {
        if (_pictureInteractionController is null
            || !double.TryParse(PictureWidthBox.Text, NumberStyles.Float,
                CultureInfo.CurrentCulture, out var width)
            || !double.TryParse(PictureHeightBox.Text, NumberStyles.Float,
                CultureInfo.CurrentCulture, out var height)
            || !_pictureInteractionController.TrySetSize(width, height))
        {
            UpdatePictureSizeFields();
            return;
        }
        CompleteStructuredContentMutation();
    }

    private void OnPictureSizeBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;
        OnApplyPictureSizeClick(sender, e);
        e.Handled = true;
    }

    private void OnRemoveSelectedPictureClick(object sender, RoutedEventArgs e)
    {
        if (_pictureInteractionController?.TryRemoveSelectedPicture() == true)
            CompleteStructuredContentMutation();
    }

    private void UpdatePictureSizeFields()
    {
        if (_pictureInteractionController?.SelectedImage is not { } image)
            return;
        var size = GetPictureDisplayedSize(image);
        PictureWidthBox.Text = size.Width.ToString("0.##", CultureInfo.CurrentCulture);
        PictureHeightBox.Text = size.Height.ToString("0.##", CultureInfo.CurrentCulture);
    }

    private static Size GetPictureDisplayedSize(Image image)
    {
        var width = double.IsFinite(image.Width) ? image.Width
            : image.ActualWidth > 0 ? image.ActualWidth : image.Source?.Width ?? 0;
        var height = double.IsFinite(image.Height) ? image.Height
            : image.ActualHeight > 0 ? image.ActualHeight : image.Source?.Height ?? 0;
        return new Size(Math.Max(WriterPictureResizeGeometry.MinimumDimension, width),
            Math.Max(WriterPictureResizeGeometry.MinimumDimension, height));
    }
}

internal readonly record struct WriterTableGridChoice(int Rows, int Columns);
