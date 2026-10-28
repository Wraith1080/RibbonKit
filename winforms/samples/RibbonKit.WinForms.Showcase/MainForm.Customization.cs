namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private RibbonCustomizationSession? _qatDraft;
    private string? _savedQatJson;
    private const string CustomizationApplicationId = "ribbonkit.winforms.showcase";
    private void InitializeCustomization()
    {
        ribbon.CustomizationApplicationId = CustomizationApplicationId;
        ribbon.CustomizationApplied += (_, _) => status.Text = "Quick Access Toolbar changes applied. The application can save its profile here.";
        ribbon.OptionsDialogOpening += (_, e) =>
        {
            var page = new RibbonOptionsPage { Text = "Editor", Name = "ApplicationEditorOptions" };
            var wrap = new CheckBox { Text = "Wrap document text", Checked = document.WordWrap, AutoSize = true, Location = new(8, 12) };
            page.Controls.Add(wrap);
            page.Applied += (_, _) => { document.WordWrap = wrap.Checked; status.Text = "Application editor preferences applied."; };
            page.Cancelled += (_, _) => wrap.Checked = document.WordWrap;
            e.Dialog.Pages.Insert(0, page);
        };
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
        Add("demo.qat.editor", "Customize QAT", () => ribbon.ShowQuickAccessCustomization(this));
        Add("demo.qat.apply", "Apply", () => status.Text = Draft().Apply() ? "Toolbar changes applied." : "Complete the highlighted input before applying.");
        Add("demo.qat.cancel", "Cancel change", () => { Draft().Cancel(); status.Text = "Unapplied toolbar edits discarded; the latest Apply remains."; });
        Add("demo.qat.reset", "Reset preview", () => { Draft().Reset(); status.Text = "Original toolbar is staged. Apply to restore it."; });
        Add("demo.qat.save", "Save preview", () => { _savedQatJson = Draft().ExportJson(); status.Text = "Toolbar preview saved in application memory."; });
        Add("demo.qat.restore", "Restore preview", () => { if (_savedQatJson is null) { status.Text = "Save a preview first."; return; } Draft().ImportJson(_savedQatJson); status.Text = "Saved toolbar is staged. Apply to restore it."; });
        ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Add(group);
        ribbon.CommandCatalog.CaptureDefaults();
        Disposed += (_, _) => _qatDraft?.Dispose();
        document.AppendText("\r\n\r\nRight-click ribbon commands or use Merging > QAT drafts > Customize QAT to open the shared toolbar editor. Search, Add/Remove and Move up/down stage a draft; Apply commits and Cancel keeps the latest Apply. Reset restores default commands; Reload reads the current toolbar. The preset demo replaces Italic with Redo and moves below. Save/Restore use the same public engine and application-owned memory.");
    }
    private RibbonCustomizationSession Draft() => _qatDraft ??= ribbon.CommandCatalog.BeginCustomization(CustomizationApplicationId);
    private void PreviewQatChange()
    {
        _qatDraft?.Dispose(); _qatDraft = ribbon.CommandCatalog.BeginCustomization(CustomizationApplicationId);
        _qatDraft.Draft.QuickAccess.RemoveAll(e => e.CommandId == italic.Id || e.CommandId == "demo.qat.redo");
        _qatDraft.Draft.QuickAccess.Add(new() { CommandId = "demo.qat.redo" });
        _qatDraft.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        status.Text = "Preview staged: Italic removed, Redo added, toolbar below. Apply or Cancel change.";
    }
    internal async Task CaptureOptions(string directory, bool touch = false)
    {
        Directory.CreateDirectory(directory); Exception? failure = null;
        ribbon.Density = touch ? RibbonDensity.Touch : RibbonDensity.Compact;
        using (var snapshot = ribbon.CommandCatalog.BeginCustomization(CustomizationApplicationId))
            File.WriteAllText(Path.Combine(directory, "default-qat.json"), snapshot.ExportJson());
        using var timer = new System.Windows.Forms.Timer { Interval = 150 };
        timer.Tick += async (_, _) =>
        {
            if (Application.OpenForms.OfType<RibbonQuickAccessCustomizationForm>().FirstOrDefault() is not { } dialog) return;
            timer.Stop();
            try
            {
                await Capture("qat");
                var fullBounds = dialog.Bounds;
                dialog.Width = dialog.MinimumSize.Width; await Capture("qat-narrow"); dialog.Bounds = fullBounds;
                dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "ApplicationEditorOptions"); await Capture("application-editor");
                async Task Capture(string name)
                {
                    await Task.Delay(100); dialog.Refresh();
                    if (!dialog.ContainsFocus || GetForegroundWindow() != dialog.Handle) throw new InvalidOperationException("Options dialog lost foreground before capture.");
                    var bounds = dialog.Bounds;
                    using var bitmap = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(bitmap);
                    graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); bitmap.Save(Path.Combine(directory, name + ".png"));
                }
            }
            catch (Exception e) { failure = e; }
            finally { dialog.Close(); }
        };
        Activate(); timer.Start(); ribbon.ShowQuickAccessCustomization(this);
        if (failure is not null) throw new InvalidOperationException("Options capture failed.", failure);
        await Task.Delay(300); // Let the native dialog close animation finish before capturing its owner.
        Activate(); Refresh(); Update();
        if (GetForegroundWindow() != Handle) throw new InvalidOperationException("Showcase lost foreground before ribbon capture.");
        using var ribbonBitmap = new Bitmap(Width, Height); using var ribbonGraphics = Graphics.FromImage(ribbonBitmap);
        ribbonGraphics.CopyFromScreen(Location, Point.Empty, Size); ribbonBitmap.Save(Path.Combine(directory, "ribbon.png"));
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
