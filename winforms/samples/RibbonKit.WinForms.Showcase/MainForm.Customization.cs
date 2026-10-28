namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private RibbonCustomizationSession? _qatDraft;
    private string? _savedQatJson;
    private const string CustomizationApplicationId = "ribbonkit.winforms.showcase";
    private void InitializeCustomization()
    {
        var redo = new RibbonButtonItem { Id = "demo.qat.redo", Text = "Redo", Icon = new SampleIcon(0) };
        redo.Click += (_, _) => status.Text = "The application handled Redo.";
        ribbon.CommandCatalog.Commands.Add(redo);
        var group = new RibbonGroup { Id = "group.qat.drafts", Text = "QAT drafts" };
        void Add(string id, string text, Action action)
        {
            var button = new RibbonButtonItem { Id = id, Text = text, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
            button.Click += (_, _) =>
            {
                try { action(); }
                catch (InvalidOperationException e) { status.Text = e.Message; }
            };
            group.Items.Add(button);
        }
        Add("demo.qat.preview", "Preview change", PreviewQatChange);
        Add("demo.qat.apply", "Apply", () => status.Text = Draft().Apply() ? "Toolbar changes applied." : "Complete the highlighted input before applying.");
        Add("demo.qat.cancel", "Cancel change", () => { Draft().Cancel(); status.Text = "Unapplied toolbar edits discarded; the latest Apply remains."; });
        Add("demo.qat.reset", "Reset preview", () => { Draft().Reset(); status.Text = "Original toolbar is staged. Apply to restore it."; });
        Add("demo.qat.save", "Save preview", () => { _savedQatJson = Draft().ExportJson(); status.Text = "Toolbar preview saved in application memory."; });
        Add("demo.qat.restore", "Restore preview", () => { if (_savedQatJson is null) { status.Text = "Save a preview first."; return; } Draft().ImportJson(_savedQatJson); status.Text = "Saved toolbar is staged. Apply to restore it."; });
        ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Add(group);
        ribbon.CommandCatalog.CaptureDefaults();
        Disposed += (_, _) => _qatDraft?.Dispose();
        document.AppendText("\r\n\r\nMerging > QAT drafts demonstrates the customization foundation. Preview change stages removing Copy, adding Redo and moving the toolbar below. Apply commits; Cancel change keeps the latest applied toolbar. Reset/Restore only stage a change. Save uses application-owned memory. The standard customization menu/editor is a later slice.");
    }
    private RibbonCustomizationSession Draft() => _qatDraft ??= ribbon.CommandCatalog.BeginCustomization(CustomizationApplicationId);
    private void PreviewQatChange()
    {
        _qatDraft?.Dispose(); _qatDraft = ribbon.CommandCatalog.BeginCustomization(CustomizationApplicationId);
        _qatDraft.Draft.QuickAccess.RemoveAll(e => e.CommandId == copy.Id || e.CommandId == "demo.qat.redo");
        _qatDraft.Draft.QuickAccess.Add(new() { CommandId = "demo.qat.redo" });
        _qatDraft.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        status.Text = "Preview staged: Copy removed, Redo added, toolbar below. Apply or Cancel change.";
    }
    internal async Task CaptureCustomization(string directory)
    {
        Directory.CreateDirectory(directory); Width = 1300; Activate(); ribbon.SelectedTab = home;
        await Capture("before"); PreviewQatChange(); await Capture("draft-unapplied");
        if (!Draft().Apply()) throw new InvalidOperationException("Showcase customization input validation blocked capture.");
        _savedQatJson = Draft().ExportJson(); status.Text = "Toolbar changes applied."; await Capture("applied");
        Draft().Draft.QuickAccess.Clear(); Draft().Cancel(); status.Text = "Unapplied edits discarded; the latest Apply remains."; await Capture("cancel-after-apply");
        Draft().Reset(); if (!Draft().Apply()) throw new InvalidOperationException("Showcase default reset was blocked."); status.Text = "Original toolbar restored."; await Capture("reset");
        Draft().ImportJson(_savedQatJson); if (!Draft().Apply()) throw new InvalidOperationException("Showcase import Apply was blocked."); status.Text = "Saved toolbar restored."; await Capture("restored");
        async Task Capture(string name)
        {
            await Task.Delay(100); Refresh(); Update();
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost foreground before customization capture.");
            using var bitmap = new Bitmap(Width, Height); using var g = Graphics.FromImage(bitmap); g.CopyFromScreen(Location, Point.Empty, Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
}
