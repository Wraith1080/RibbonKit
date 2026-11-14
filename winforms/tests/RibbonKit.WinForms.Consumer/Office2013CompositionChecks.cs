using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace IndependentConsumer;

// Public APIs only. The package runner builds its own model; both samples also
// run Verify against their actual authored model without adding diagnostic tabs.
internal static class Office2013CompositionChecks
{
    internal static int Run(string directory, bool nativeOnly, bool paintOnly)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        Directory.CreateDirectory(directory); var log = new List<string>();
        try
        {
            foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
            foreach (var density in Enum.GetValues<RibbonDensity>())
            {
                using var form = new RibbonForm();
                using var ribbon = new RibbonControl { LayoutMode = mode, Density = density };
                var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "editing", Text = "Editing" };
                var choice = new RibbonComboBoxItem { Id = "font", Text = "Font", IsEditable = true, Value = "Retained choice", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
                choice.Items.AddRange("Retained choice", "Another choice");
                var menu = new RibbonDropDownItem { Id = "menu", Text = "Commands", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
                menu.Items.Add(new RibbonToggleItem { Id = "menu.checked", Text = "Checked command", Checked = true });
                menu.Items.Add(new RibbonTextBoxItem { Id = "menu.value", Text = "Popup value", Value = "Retained popup editor" });
                var gallery = new RibbonGalleryItem { Id = "styles", Text = "Styles", IsInRibbon = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
                gallery.Items.AddRange(new RibbonGalleryTile { Id = "style.one", Text = "Normal", Icon = RibbonStockIcons.Create("save") }, new RibbonGalleryTile { Id = "style.two", Text = "Heading", Icon = RibbonStockIcons.Create("copy") });
                gallery.SelectedItem = (RibbonGalleryTile)gallery.Items[0];
                group.Items.AddRange(choice, menu, gallery); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
                ribbon.QuickAccessItems.Add(new RibbonButtonItem { Id = "save", Text = "Save", Icon = RibbonStockIcons.Create("save") });
                ribbon.FileMenu.Visible = true;
                ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Id = "info", Text = "Info", Content = new TextBox { Multiline = true, Text = "Retained native page" } });
                ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Id = "print", Text = "Print", Content = new TextBox { Text = "Second page" } });
                form.Controls.Add(ribbon); form.Ribbon = ribbon; ribbon.CommandCatalog.CaptureDefaults();
                string scope = $"{mode}-{density}"; Verify(form, ribbon, Path.Combine(directory, scope), scope);
                log.Add($"PASS: {scope}, actual {form.DeviceDpi} DPI; native ribbon/popup editors, menu/gallery reopen, live Backstage accent/page handoff, Options validation/Apply/Cancel and pending QAT draft, retained source/native identities.");
            }
            log.Add("LIMIT: offscreen native windows and direct-print captures; no physical input/IME, actual VS, desktop material pixels, corner silhouette or mixed-monitor acceptance.");
            return 0;
        }
        catch (Exception error) { log.Add("FAIL: " + error); return 1; }
        finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); }
    }

    internal static void Verify(RibbonForm form, RibbonControl ribbon, string directory, string scope)
    {
        Directory.CreateDirectory(directory);
        form.StartPosition = FormStartPosition.Manual; form.Bounds = new(-16000, -16000, 1400, 850); form.ShowInTaskbar = false;
        ribbon.AnimationLevel = RibbonAnimationLevel.None; ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2013Light;
        form.TitleBarColor = Color.Empty; form.Show(); Application.DoEvents();
        var tabs = ribbon.Tabs.ToArray(); var groups = tabs.SelectMany(t => t.Groups).ToArray();
        var commands = groups.SelectMany(g => g.Items).ToArray(); var qat = ribbon.QuickAccessItems.ToArray(); var pages = ribbon.Backstage.Items.ToArray();
        var choice = commands.OfType<RibbonComboBoxItem>().First(c => c.Enabled && c.Visible && c.Items.Count > 1 && (ribbon.LayoutMode == RibbonLayoutMode.Classic || c.SimplifiedPlacement == RibbonSimplifiedPlacement.Inline));
        SelectOwner(choice);
        var editor = Children(ribbon).OfType<ComboBox>().Single(c => c.Name == choice.Id);
        nint editorHandle = editor.Handle; var editorParent = editor.Parent; string value = choice.Value; int selected = choice.SelectedIndex;
        int validations = 0; choice.ValueValidating += CountValidation;
        var gallery = commands.OfType<RibbonGalleryItem>().First(g => g.Enabled && g.Visible && g.Items.Count > 1);
        var galleryItems = gallery.Items.ToArray(); var gallerySelection = gallery.SelectedItem; var artwork = galleryItems.Select(i => i.Icon).ToArray();
        var menus = commands.OfType<RibbonDropDownItem>().Where(d => d is not RibbonGalleryItem && d.Enabled && d.Visible).ToArray();
        var menu = menus.FirstOrDefault(d => d.Items.Any(i => i is RibbonTextBoxItem or RibbonComboBoxItem) || d.Content is not null || d.ContentType is not null) ?? menus.First(d => d.Items.Count > 0);
        var page = pages.OfType<RibbonBackstagePageItem>().First(p => p.Content is not null);
        var secondPage = pages.OfType<RibbonBackstagePageItem>().First(p => !ReferenceEquals(p, page) && p.Content is not null);
        ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; ribbon.Backstage.SelectedPage = page; ribbon.IsBackstageOpen = true; Application.DoEvents();
        var nativePage = page.Content!; var pageEditor = Children(nativePage).Prepend(nativePage).OfType<TextBoxBase>().First();
        string pageText = pageEditor.Text; pageEditor.Select(Math.Min(2, pageText.Length), Math.Min(3, Math.Max(0, pageText.Length - 2)));
        nint pageHandle = pageEditor.Handle; var pageParent = pageEditor.Parent; int start = pageEditor.SelectionStart, length = pageEditor.SelectionLength;
        ribbon.Theme = ribbon.Theme with { Accent = Color.FromArgb(128, 55, 123), TitleBarForeground = Color.White };
        form.TitleBarColor = ribbon.Theme.Accent; Application.DoEvents();
        Require(ribbon.IsBackstageOpen && ReferenceEquals(ribbon.Backstage.SelectedPage, page), "Live accent change retains the open page");
        Capture(Children(form).Single(c => c.GetType().Name == "RibbonBackstageSurface" && c.Visible), "backstage-accent");
        Capture(nativePage, "backstage-native-page");
        secondPage.PerformClick(); page.PerformClick();
        Require(pageEditor.Handle == pageHandle && ReferenceEquals(pageEditor.Parent, pageParent) && pageEditor.Text == pageText && pageEditor.SelectionStart == start && pageEditor.SelectionLength == length, "Backstage page/native text and selection survive page/accent handoff");
        ribbon.IsBackstageOpen = false; Application.DoEvents();
        Require(choice.Value == value && choice.SelectedIndex == selected && editor.Handle == editorHandle && ReferenceEquals(editor.Parent, editorParent), "Ribbon editor survives Backstage and accent handoff");
        Require(validations == 0, "Pure presentation/page changes do not validate the ribbon editor");

        // Inspect the synchronously opened surface. This offscreen fixture does
        // not take desktop foreground ownership; pumping external deactivation
        // here would correctly dismiss the popup before it can be inspected.
        SelectOwner(menu); menu.ShowDropDown();
        Require(menu.IsDropDownOpen, "Authored command menu opens");
        var menuPopup = CapturePopup("menu");
        var popupEditors = Children(menuPopup).Where(c => c is TextBoxBase or ComboBox).ToArray();
        Require(popupEditors.Length > 0, "Authored popup has real native editors");
        var popupValues = popupEditors.Select(c => c.Text).ToArray(); var popupHandles = popupEditors.Select(c => c.Handle).ToArray();
        menuPopup.Refresh();
        Require(popupEditors.Select(c => c.Handle).SequenceEqual(popupHandles) && popupEditors.Select(c => c.Text).SequenceEqual(popupValues), "Popup paint retains native editor handles and values");
        menu.CloseDropDown();
        SelectOwner(gallery); gallery.PerformClick();
        Require(gallery.IsDropDownOpen, "Authored gallery opens"); CapturePopup("gallery"); gallery.CloseDropDown();
        ribbon.Theme = ribbon.Theme with { Accent = Color.ForestGreen, TitleBarForeground = Color.Empty }; form.TitleBarColor = Color.Empty;
        gallery.PerformClick(); Require(gallery.IsDropDownOpen, "Gallery reopens after accent and caption Reset"); gallery.CloseDropDown();
        Require(galleryItems.SequenceEqual(gallery.Items) && ReferenceEquals(gallerySelection, gallery.SelectedItem) && artwork.SequenceEqual(gallery.Items.Select(i => i.Icon)), "Gallery reopen retains source, selection and borrowed artwork");
        SelectOwner(choice); Capture(ribbon, "ribbon-neutral");

        using (var options = new RibbonQuickAccessCustomizationForm(ribbon, "RibbonKit.Office2013Composition"))
        {
            var draft = new RibbonOptionsPage { Name = "CompositionDraft", Text = "Consumer draft" };
            var text = new TextBox { Name = "DraftValue", Bounds = new(12, 12, 240, 28), Text = "Checkpoint" }; draft.Controls.Add(text);
            string checkpoint = text.Text; string? restored = null; bool reject = true; int applied = 0, cancelled = 0;
            draft.Applying += (_, e) => e.Cancel = reject;
            draft.Applied += (_, _) => { checkpoint = text.Text; applied++; };
            draft.Cancelled += (_, _) => { text.Text = checkpoint; restored = text.Text; cancelled++; };
            options.Pages.Add(draft); options.SelectedPage = draft;
            options.StartPosition = FormStartPosition.Manual; options.Location = new(-16000, -16000); options.Show(form); options.Location = new(-16000, -16000); Application.DoEvents();
            nint draftHandle = text.Handle; var draftParent = text.Parent;
            Require(ButtonFill(Child<Button>(options, "AcceptCustomization")) == ribbon.Theme.Accent.ToArgb(), "Real Options OK uses the effective accent");
            Require(ButtonFill(Child<Button>(options, "CancelCustomization")) == Color.White.ToArgb(), "Real Options Cancel retains its neutral idle fill");
            var qatDraft = options.Editor.Session.Draft;
            if (qatDraft.QuickAccess.Count > 0) qatDraft.QuickAccess.RemoveAt(0);
            int qatDraftCount = qatDraft.QuickAccess.Count; options.Editor.RefreshDraft();
            Require(qat.SequenceEqual(ribbon.QuickAccessItems), "Pending QAT draft does not mutate live commands");
            text.Text = "Pending draft"; Require(!draft.ApplyChanges() && applied == 0 && text.Text == "Pending draft", "Options validation preserves rejected draft");
            reject = false; Child<Button>(options, "ApplyCustomization").PerformClick();
            Require(applied == 1 && checkpoint == "Pending draft", "Real Apply commits the selected native page once");
            foreach (var target in options.Pages.Where(p => !ReferenceEquals(p, draft)).ToArray())
            {
                options.SelectedPage = target; Application.DoEvents(); Capture(options, target.Name);
                Require(text.Handle == draftHandle && ReferenceEquals(text.Parent, draftParent) && text.Text == checkpoint, "Options navigation retains native draft controls");
                Require(ReferenceEquals(qatDraft, options.Editor.Session.Draft) && qatDraft.QuickAccess.Count == qatDraftCount && qat.SequenceEqual(ribbon.QuickAccessItems), "Options navigation retains its pending QAT draft independently of live commands");
            }
            options.SelectedPage = draft; text.Text = "Unapplied edit";
            Child<Button>(options, "CancelCustomization").PerformClick();
            Require(cancelled == 1 && restored == checkpoint && applied == 1, $"Real Cancel restores only the post-Apply draft checkpoint (cancel={cancelled}, applied={applied}, restored={restored})");
        }
        Require(tabs.SequenceEqual(ribbon.Tabs) && groups.SequenceEqual(ribbon.Tabs.SelectMany(t => t.Groups)) && commands.SequenceEqual(ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items)), "Composed review preserves authored tabs/groups/commands");
        Require(qat.SequenceEqual(ribbon.QuickAccessItems) && pages.SequenceEqual(ribbon.Backstage.Items), "Options Cancel and surface handoffs retain QAT/pages");
        Require(editor.Handle == editorHandle && choice.Value == value && choice.SelectedIndex == selected, "Final native editor identity/value retained");
        choice.ValueValidating -= CountValidation;
        File.WriteAllText(Path.Combine(directory, "result.log"), $"PASS: {scope}; actual {form.DeviceDpi} DPI, composed native/editor/menu/gallery/Backstage/Options checks.\nLIMIT: offscreen/direct-print; physical/VS/material/mixed-monitor review remains separate.");

        void CountValidation(object? sender, System.ComponentModel.CancelEventArgs e) => validations++;
        void SelectOwner(RibbonItem item) { ribbon.SelectedTab = tabs.Single(t => t.Groups.Any(g => g.Items.Contains(item))); Application.DoEvents(); }
        void Capture(Control control, string name) { control.Refresh(); using var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, new(Point.Empty, image.Size)); image.Save(Path.Combine(directory, name + ".png")); }
        ToolStripDropDown CapturePopup(string name)
        {
            ToolStripDropDown? popup = null;
            EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (Control.FromHandle(window) is ToolStripDropDown { Visible: true } found) popup = found; return true; }, 0);
            Require(popup is not null, "Native popup surface exists"); Capture(popup!, name); return popup!;
        }
    }
    private static int ButtonFill(Button button) { using var image = new Bitmap(button.Width, button.Height); button.DrawToBitmap(image, new(Point.Empty, image.Size)); return image.GetPixel(image.Width / 2, image.Height - 5).ToArgb(); }
    private static T Child<T>(Control root, string name) where T : Control => Children(root).OfType<T>().Single(c => c.Name == name);
    private static IEnumerable<Control> Children(Control root) => root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Children(c)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private delegate bool WindowVisitor(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, WindowVisitor visitor, nint state);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
}
