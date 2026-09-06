using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using Xunit;

namespace RibbonKit.Writer.Tests.Editing;

[Collection("Writer UI")]
public sealed class WriterEditorContextMenuControllerTests
{
    [Fact]
    public void BaseMenuUsesStableTargetAndCommandTargetsTheEditor()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("context target");
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(run))
            };
            using var adapter = new WriterEditingAdapter(editor);
            WriterEditorContextMenuTarget? clearTarget = null;
            WriterEditorContextMenuTarget? fontTarget = null;
            WriterEditorContextMenuTarget? paragraphTarget = null;
            using var controller = new WriterEditorContextMenuController(editor)
            {
                ClearFormattingRequested = target => clearTarget = target,
                FontDialogRequested = target => fontTarget = target,
                ParagraphDialogRequested = target => paragraphTarget = target
            };
            var window = HostEditor(editor);
            try
            {
                window.Show();
                window.UpdateLayout();
                editor.Focus();
                editor.Selection.Select(run.ContentStart, run.ContentEnd);

                controller.Refresh();
                var target = controller.CurrentTarget;
                Assert.NotNull(target);
                Assert.True(target!.HasSelection);
                Assert.Same(controller.Menu, editor.ContextMenu);
                Assert.Same(editor, controller.Menu.PlacementTarget);

                var headers = controller.Menu.Items.OfType<MenuItem>()
                    .Select(static item => item.Header?.ToString())
                    .ToArray();
                Assert.Contains("Cut", headers);
                Assert.Contains("Copy", headers);
                Assert.Contains("Paste", headers);
                Assert.Contains("Bold", headers);
                Assert.Contains("Italic", headers);
                Assert.Contains("Underline", headers);
                Assert.Contains("Clear Formatting", headers);
                Assert.Contains("Font...", headers);
                Assert.Contains("Paragraph...", headers);
                Assert.DoesNotContain("Insert Row", headers);
                Assert.DoesNotContain("Delete Table", headers);
                Assert.DoesNotContain("Picture", headers);
                Assert.DoesNotContain("Resize", headers);

                foreach (var item in controller.Menu.Items.OfType<MenuItem>())
                    Assert.Same(editor, item.CommandTarget);

                editor.Selection.Select(run.ContentEnd, run.ContentEnd);
                var bold = FindItem(controller.Menu, "Bold");
                Assert.NotNull(bold.Command);
                bold.Command!.Execute(bold.CommandParameter);

                Assert.Equal(0, target.Start.CompareTo(editor.Selection.Start));
                Assert.Equal(0, target.End.CompareTo(editor.Selection.End));
                Assert.Equal(FontWeights.Bold,
                    new TextRange(target.Start, target.End).GetPropertyValue(TextElement.FontWeightProperty));

                FindItem(controller.Menu, "Clear Formatting")
                    .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                FindItem(controller.Menu, "Font...")
                    .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                FindItem(controller.Menu, "Paragraph...")
                    .RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Assert.Same(target, clearTarget);
                Assert.Same(target, fontTarget);
                Assert.Same(target, paragraphTarget);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void MissingRibbonKitMenuResourcesFallBackWithoutThrowing()
    {
        StaTestHelper.Run(() =>
        {
            var menu = new ContextMenu();
            var item = new MenuItem { Header = "Fallback" };
            menu.Items.Add(item);

            Assert.False(WriterEditorContextMenuController.TryApplyModernMenuStyles(
                menu, new ResourceDictionary()));
            Assert.Same(item, menu.Items[0]);
            Assert.Null(menu.Style);
        });
    }

    [Fact]
    public void CallbackItemsRestoreTheCapturedSelectionBeforeOpeningDialog()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("font callback");
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(run))
            };
            WriterEditorContextMenuTarget? callbackTarget = null;
            using var controller = new WriterEditorContextMenuController(editor)
            {
                FontDialogRequested = target => callbackTarget = target
            };
            var window = HostEditor(editor);
            try
            {
                window.Show();
                window.UpdateLayout();
                editor.Focus();
                editor.Selection.Select(run.ContentStart, run.ContentEnd);
                controller.Refresh();
                var target = controller.CurrentTarget;
                Assert.NotNull(target);

                editor.Selection.Select(run.ContentEnd, run.ContentEnd);
                var font = FindItem(controller.Menu, "Font...");
                font.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, font));

                Assert.Same(target, callbackTarget);
                Assert.Equal(0, target!.Start.CompareTo(editor.Selection.Start));
                Assert.Equal(0, target.End.CompareTo(editor.Selection.End));
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ExtensionSeamReceivesTheSameStableTargetAndCanAppendRows()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("extension target");
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(run))
            };
            WriterEditorContextMenuExtensionContext? extension = null;
            using var controller = new WriterEditorContextMenuController(editor);
            controller.ExtensionsRequested += (_, context) =>
            {
                extension = context;
                context.AddSeparator();
                context.AddItem(context.CreateCallbackItem("Later Object Action", _ => { }));
            };
            var window = HostEditor(editor);
            try
            {
                window.Show();
                window.UpdateLayout();
                editor.Selection.Select(run.ContentStart, run.ContentEnd);

                controller.Refresh();

                var current = controller.CurrentTarget;
                Assert.NotNull(current);
                Assert.NotNull(extension);
                Assert.Same(current, extension!.Target);
                Assert.Contains(controller.Menu.Items.OfType<MenuItem>(),
                    item => Equals(item.Header?.ToString(), "Later Object Action"));
                Assert.Same(editor, controller.Menu.Items.OfType<MenuItem>()
                    .Single(item => Equals(item.Header?.ToString(), "Later Object Action"))
                    .CommandTarget);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void PointerSelectionHelperPreservesInsideSelectionOnly()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("selection");
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(run))
            };
            var start = run.ContentStart;
            var end = run.ContentEnd;
            var inside = start.GetPositionAtOffset(2, LogicalDirection.Forward);
            var outside = end.GetPositionAtOffset(1, LogicalDirection.Forward);

            Assert.True(WriterEditorContextMenuController.IsPointerInsideSelection(inside, start, end));
            Assert.False(WriterEditorContextMenuController.IsPointerInsideSelection(outside, start, end));
            Assert.True(WriterEditorContextMenuController.IsPointerInsideSelection(start, start, end));
            Assert.False(WriterEditorContextMenuController.IsPointerInsideSelection(end, start, end));
        });
    }

    [Fact]
    public void DetachRestoresTheOriginalContextMenuAndIsIdempotent()
    {
        StaTestHelper.Run(() =>
        {
            var original = new ContextMenu();
            var editor = new RichTextBox
            {
                ContextMenu = original,
                Document = new FlowDocument(new Paragraph(new Run("menu")))
            };
            using var controller = new WriterEditorContextMenuController(editor);
            var refreshCount = 0;
            controller.ExtensionsRequested += (_, _) => refreshCount++;

            Assert.Same(controller.Menu, editor.ContextMenu);
            controller.Attach();
            controller.Attach();
            controller.Refresh();
            Assert.Equal(1, refreshCount);
            controller.Detach();
            Assert.Same(original, editor.ContextMenu);
            controller.Detach();
            controller.Attach();
            Assert.Same(controller.Menu, editor.ContextMenu);
        });
    }

    [Fact]
    public void CapturedTargetCanRestoreSelectionAfterTheEditorSelectionMoves()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("restore me");
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(run))
            };
            using var controller = new WriterEditorContextMenuController(editor);
            editor.Selection.Select(run.ContentStart, run.ContentEnd);
            var target = controller.CaptureCurrentTarget();
            editor.Selection.Select(run.ContentEnd, run.ContentEnd);

            Assert.True(controller.TryRestoreTarget(target));
            Assert.Equal(0, target.Start.CompareTo(editor.Selection.Start));
            Assert.Equal(0, target.End.CompareTo(editor.Selection.End));
        });
    }

    [Fact]
    public void StructuredHitTestCanPreserveASelectionOutsideItsRawTextBounds()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("one two three");
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(run))
            };
            using var controller = new WriterEditorContextMenuController(editor)
            {
                StructuredSelectionHitTest = (_, _, _) => true
            };
            var start = run.ContentStart;
            var end = start.GetPositionAtOffset(3, LogicalDirection.Forward)!;
            var rawOutside = run.ContentEnd;

            Assert.False(WriterEditorContextMenuController.IsPointerInsideSelection(
                rawOutside, start, end));
            Assert.True(controller.ShouldPreserveSelection(rawOutside, start, end));
        });
    }

    [Fact]
    public void CapturedTargetAndGuardedCallbackRejectDocumentReplacement()
    {
        StaTestHelper.Run(() =>
        {
            var run = new Run("stale target");
            var editor = new RichTextBox
            {
                Document = new FlowDocument(new Paragraph(run))
            };
            var invoked = false;
            MenuItem? guarded = null;
            using var controller = new WriterEditorContextMenuController(editor);
            controller.ExtensionsRequested += (_, context) =>
            {
                guarded = context.CreateCallbackItem("Guarded Object Action",
                    target => target.IsValidFor(editor), _ => invoked = true);
                context.AddItem(guarded);
            };
            editor.Selection.Select(run.ContentStart, run.ContentEnd);
            controller.Refresh();
            var target = Assert.IsType<WriterEditorContextMenuTarget>(controller.CurrentTarget);
            Assert.NotNull(guarded?.Command);

            editor.Document = new FlowDocument(new Paragraph(new Run("replacement")));

            Assert.False(target.IsValidFor(editor));
            Assert.False(target.TryRestore(editor));
            Assert.False(guarded!.Command!.CanExecute(guarded.CommandParameter));
            guarded.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, guarded));
            Assert.False(invoked);
        });
    }

    private static MenuItem FindItem(ContextMenu menu, string header) =>
        menu.Items.OfType<MenuItem>().Single(item => Equals(item.Header?.ToString(), header));

    private static Window HostEditor(RichTextBox editor) => new()
    {
        Content = editor,
        Width = 500,
        Height = 300,
        WindowStartupLocation = WindowStartupLocation.Manual,
        Left = -10000,
        Top = -10000,
        ShowInTaskbar = false,
        Opacity = 0.01
    };
}
