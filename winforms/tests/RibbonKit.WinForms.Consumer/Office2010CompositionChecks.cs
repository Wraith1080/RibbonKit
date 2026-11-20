using System.Diagnostics;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace IndependentConsumer;

internal static class Office2010CompositionChecks
{
    internal static int Run(string directory)
    {
        Directory.CreateDirectory(directory);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        using var form = new RibbonForm();
        using var ribbon = new RibbonControl { QuickAccessVisible = true };
        var home = new RibbonTab { Id = "home", Text = "Home" };
        var group = new RibbonGroup { Id = "commands", Text = "Commands" };
        var menu = new RibbonDropDownItem { Id = "edit", Text = "Edit", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        menu.Items.Add(new RibbonToggleItem { Id = "check", Text = "Checked command", Checked = true });
        var gallery = new RibbonGalleryItem { Id = "gallery", Text = "Styles", IsInRibbon = true, SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        var tile = new RibbonGalleryTile { Id = "normal", Text = "Normal", Icon = RibbonStockIcons.Create("save") };
        gallery.Items.AddRange(tile, new RibbonGalleryTile { Id = "heading", Text = "Heading", Icon = RibbonStockIcons.Create("copy") });
        gallery.SelectedItem = tile;
        var entry = new RibbonTextBoxItem { Id = "document-name", Text = "Name", Value = "Independent editor", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        group.Items.AddRange(entry, menu, gallery);
        var overflow = new RibbonGroup { Id = "extras", Text = "Extras" };
        for (int i = 0; i < 8; i++)
            overflow.Items.Add(new RibbonButtonItem { Id = $"command-{i}", Text = $"Extra command {i}", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline });
        home.Groups.AddRange(group, overflow);
        var view = new RibbonTab { Id = "view", Text = "View" };
        var viewGroup = new RibbonGroup { Id = "view-group", Text = "View" };
        viewGroup.Items.Add(new RibbonButtonItem { Id = "zoom", Text = "Zoom" });
        view.Groups.Add(viewGroup);
        ribbon.Tabs.AddRange(home, view);
        ribbon.QuickAccessItems.Add(new RibbonButtonItem { Id = "save", Text = "Save" });

        ribbon.FileMenu.Visible = true;
        var split = new RibbonSplitButtonItem { Id = "save-as", Text = "Save As", DropDownHeader = "Save a copy" };
        split.Items.Add(new RibbonButtonItem { Id = "save-copy", Text = "Save a copy" });
        ribbon.FileMenu.Items.Add(split);
        ribbon.ApplicationMenu.DefaultPane.DropDownHeader = "Recent Documents";
        ribbon.ApplicationMenu.DefaultPane.Items.Add(new RibbonButtonItem { Id = "report", Text = "Quarterly report.docx" });
        ribbon.ApplicationMenu.FooterItems.Add(new RibbonButtonItem { Id = "options", Text = "Options" });
        ribbon.Backstage.Items.AddRange(
            new RibbonBackstagePageItem { Id = "info", Text = "Info", Content = new RichTextBox { Dock = DockStyle.Fill, Text = "Independent native page" } },
            new RibbonBackstagePageItem { Id = "print", Text = "Print", Content = new TextBox { Text = "Second page" } });
        form.Controls.Add(ribbon);
        form.Ribbon = ribbon;
        ribbon.CommandCatalog.CaptureDefaults();
        return Verify(form, ribbon, directory, "independent", openOptionsFromFooter: true);
    }

    internal static int Verify(RibbonForm form, RibbonControl ribbon, string directory, string scope, bool openOptionsFromFooter = false)
    {
        Directory.CreateDirectory(directory);
        var log = new List<string>();
        try
        {
            var tabs = ribbon.Tabs.Where(t => t.Visible && t.Enabled).ToArray();
            var groups = ribbon.Tabs.SelectMany(t => t.Groups).ToArray();
            var items = groups.SelectMany(g => g.Items).ToArray();
            var qat = ribbon.QuickAccessItems.ToArray();
            var fileItems = ribbon.FileMenu.Items.ToArray();
            var pages = ribbon.Backstage.Items.ToArray();
            // The theme and accent galleries intentionally change selection
            // when the application applies a theme. Exercise a document gallery.
            var gallery = items.OfType<RibbonGalleryItem>().First(g => g.Visible && g.Enabled && g.Items.Count > 1
                && !g.Items.OfType<RibbonGalleryTile>().Any(tile => tile.Text is "Application theme" or "Default"));
            var menu = items.OfType<RibbonDropDownItem>().First(m => m is not RibbonGalleryItem && m.Visible && m.Enabled && m.Items.Count > 0);
            var galleryItems = gallery.Items.ToArray();
            var artwork = galleryItems.Select(i => i.Icon).ToArray();
            var styleSelection = gallery.SelectedItem;
            var split = fileItems.OfType<RibbonSplitButtonItem>().First(i => i.Visible && i.Enabled);
            var footer = ribbon.ApplicationMenu.FooterItems.OfType<RibbonButtonItem>().Single(i => i.Visible && i.Text == "Options");

            ribbon.AnimationLevel = RibbonAnimationLevel.None;
            ribbon.LayoutMode = RibbonLayoutMode.Classic;
            ribbon.Density = RibbonDensity.Compact;
            ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2010Blue;
            form.StartPosition = FormStartPosition.Manual;
            form.ShowInTaskbar = false;
            form.Bounds = new Rectangle(-16000, -16000, 1280, 760);
            form.Show();
            Application.DoEvents();
            var window = form.Handle;
            var ribbonWindow = ribbon.Handle;
            var wide = form.ClientSize;
            Require(form.EffectiveWindowCornerPreference == RibbonWindowCornerPreference.Rounded, "Office 2010 requests rounded corners");
            log.Add($"{scope}: actual {form.DeviceDpi} DPI, HWND {window}, client {wide}, gallery {gallery.Text}");

            // Exercise real transitions on one HWND, including narrow overflow.
            ribbon.AnimationLevel = RibbonAnimationLevel.Expressive;
            int animated = 0;
            foreach (var style in Enum.GetValues<RibbonTabContentAnimation>())
            {
                ribbon.TabContentAnimation = style;
                ribbon.SelectedTab = tabs[0];
                Settle($"select first tab for {style}");
                Transition($"tab switch {style}", () => ribbon.SelectedTab = tabs[1]);
                Require(ReferenceEquals(tabs[1], ribbon.SelectedTab), $"{style} commits selected tab");
                Transition($"tab return {style}", () => ribbon.SelectedTab = tabs[0]);
            }
            foreach (var layout in Enum.GetValues<RibbonLayoutMode>())
            foreach (var density in Enum.GetValues<RibbonDensity>())
            {
                Transition($"layout {layout}", () => ribbon.LayoutMode = layout);
                Transition($"density {density}", () => ribbon.Density = density);
                form.ClientSize = new Size(Math.Max(form.MinimumSize.Width, Math.Min(wide.Width - 1, 440)), wide.Height);
                Settle("narrow resize");
                Require(form.ClientSize.Width < wide.Width && ribbon.Width <= form.ClientSize.Width, "Ribbon fits narrow native window");
                Capture(ribbon, $"narrow-{layout}-{density}");
                form.ClientSize = wide;
                Settle("wide resize");
                Require(ribbon.LayoutMode == layout && ribbon.Density == density && form.Handle == window && ribbon.Handle == ribbonWindow,
                    "Resize retains the settings and HWNDs");
            }
            Transition("return to Classic", () => ribbon.LayoutMode = RibbonLayoutMode.Classic);
            Transition("return to Compact", () => ribbon.Density = RibbonDensity.Compact);

            // Open the actual built-in menu and gallery, then reopen the gallery
            // after the File and Options round trip below.
            SelectOwner(menu);
            Capture(ribbon, "wide-classic");
            menu.ShowDropDown();
            Require(menu.IsDropDownOpen, "Command popup opens after resize");
            Capture(Popup(), "command-popup");
            menu.CloseDropDown();
            Settle("command popup close");
            SelectOwner(gallery);
            gallery.ShowDropDown();
            Require(gallery.IsDropDownOpen, "Gallery popup opens after command popup");
            Capture(Popup(), "gallery-popup");
            gallery.CloseDropDown();
            Settle("gallery popup close");

            var red = Color.FromArgb(180, 40, 64);
            var blue = ribbon.Theme;
            var redCaption = blue.IdentityAccent!.CreateCaptionFill(red);
            ribbon.Theme = blue with { Accent = red, TitleBarForeground = Color.White, TitleBarBackgroundFill = redCaption };
            ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
            var backstagePages = pages.OfType<RibbonBackstagePageItem>().Where(p => p.Visible && p.Enabled).ToArray();
            var first = backstagePages.First(p => p.Content is { } content && Descendants(content).Prepend(content).OfType<TextBoxBase>().Any());
            var second = backstagePages.First(p => !ReferenceEquals(p, first));
            ribbon.Backstage.SelectedPage = first;
            Transition("Backstage opens with colored caption", ribbon.FileMenu.ShowDropDown);
            Require(ribbon.IsBackstageOpen && ReferenceEquals(first, ribbon.Backstage.SelectedPage), "Backstage page open");
            var firstPage = first.Content!;
            var editor = Descendants(firstPage).Prepend(firstPage).OfType<TextBoxBase>().First();
            editor.Select(0, Math.Min(4, editor.TextLength));
            var pageState = (editor.Handle, editor.Parent, editor.Text, editor.SelectionStart, editor.SelectionLength);
            Transition("Backstage page change", () => ribbon.Backstage.SelectedPage = second);
            Transition("Backstage page return", () => ribbon.Backstage.SelectedPage = first);
            Require(PageRetained(), "Backstage page return preserves native editor and selection");
            Control pane = firstPage;
            while (pane.Parent is { } parent && !ReferenceEquals(parent, form)) pane = parent;
            Require(ReferenceEquals(pane.Parent, form) && pane.Visible
                && form.Controls.GetChildIndex(pane) < form.Controls.GetChildIndex(ribbon), "Live Backstage pane covers the ribbon body");
            Capture(form, "backstage-open", pane);
            Transition("Backstage close", ribbon.FileMenu.CloseDropDown);
            Require(!ribbon.IsBackstageOpen && !ribbon.FileMenu.IsDropDownOpen && !pane.Visible, "Backstage closes before File route changes");
            Capture(ribbon, "caption-after-backstage-close");

            ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu;
            ribbon.FileMenu.ShowDropDown();
            Require(ribbon.FileMenu.IsDropDownOpen && !ribbon.IsBackstageOpen, "Application Menu opens after Backstage");
            Capture(Popup(), "file-default");
            split.ShowDropDown();
            Require(split.IsDropDownOpen, "Split File pane opens");
            var fileSurface = Popup();
            Capture(fileSurface, "file-split");
            var footerAction = AccessibleChildren(fileSurface.AccessibilityObject).Single(row => row.Name == footer.Text);
            int footerClicks = 0;
            DialogResult result = DialogResult.None;
            void OpenFromFooter(object? sender, EventArgs e)
            {
                footerClicks++;
                if (openOptionsFromFooter) result = OpenOptions();
            }
            footer.Click += OpenFromFooter;
            try { footerAction.DoDefaultAction(); }
            finally { footer.Click -= OpenFromFooter; }
            Require(footerClicks == 1 && !ribbon.FileMenu.IsDropDownOpen, "Native footer action closes File and fires once");
            if (!openOptionsFromFooter) result = OpenOptions();
            Require(result == DialogResult.Cancel, "Modal Options returns Cancel");
            Require(ribbon.Theme.TitleBarBackgroundFill == redCaption && ribbon.Theme.TitleBarForeground == Color.White,
                "Closing Backstage, File and Options retains the colored caption setting");

            ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
            Transition("Backstage reopens after Options", ribbon.FileMenu.ShowDropDown);
            Require(ribbon.IsBackstageOpen && ReferenceEquals(first, ribbon.Backstage.SelectedPage) && PageRetained(), "Backstage editor survives File and Options");
            Transition("Backstage closes after Options", ribbon.FileMenu.CloseDropDown);
            ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2010Blue;
            Require(ribbon.Theme.Accent == blue.Accent && ribbon.Theme.TitleBarBackgroundFill == blue.TitleBarBackgroundFill,
                "Built-in theme restores its accent and neutral caption");
            SelectOwner(gallery);
            gallery.ShowDropDown();
            Require(gallery.IsDropDownOpen, "Gallery reopens after File and Options");
            gallery.CloseDropDown();
            Settle("final gallery close");
            Require(gallery.Items.SequenceEqual(galleryItems) && gallery.Items.Select(i => i.Icon).SequenceEqual(artwork)
                && ReferenceEquals(styleSelection, gallery.SelectedItem), "Gallery source, artwork and selection survive the round trip");
            Require(ribbon.Tabs.SelectMany(t => t.Groups).SequenceEqual(groups) && groups.SelectMany(g => g.Items).SequenceEqual(items)
                && ribbon.QuickAccessItems.SequenceEqual(qat) && ribbon.FileMenu.Items.SequenceEqual(fileItems) && ribbon.Backstage.Items.SequenceEqual(pages),
                "Source models survive layout, theme, File and Options");
            Require(form.Handle == window && ribbon.Handle == ribbonWindow && !ribbon.IsAnimating, "Window identities and final settled state");
            log.Add($"PASS: {scope}; four layout/density combinations, File/Backstage/Options/gallery round trip; {animated} transitions observed running.");
            return 0;

            bool PageRetained() => pageState == (editor.Handle, editor.Parent, editor.Text, editor.SelectionStart, editor.SelectionLength);
            void SelectOwner(RibbonItem item)
            {
                ribbon.SelectedTab = tabs.Single(t => t.Groups.Any(g => g.Items.Contains(item)));
                Settle($"select owner of {item.Text}");
            }
            DialogResult OpenOptions()
            {
                Exception? failure = null;
                bool checkedDialog = false;
                void OnOpening(object? sender, RibbonOptionsDialogEventArgs e)
                {
                    Require(!ribbon.FileMenu.IsDropDownOpen && !ribbon.IsBackstageOpen, "Options starts after previous surfaces dismiss");
                    var options = (RibbonQuickAccessCustomizationForm)e.Dialog;
                    options.StartPosition = FormStartPosition.Manual;
                    options.Location = form.Location;
                    options.Shown += (_, _) =>
                    {
                        try
                        {
                            Require(ReferenceEquals(options.Owner, form) && options.SelectedPage?.Text == "Customize Ribbon", "Real modal Options owns the expected page and host");
                            var pending = options.Editor.Session.Draft;
                            if (pending.QuickAccess.Count > 0) pending.QuickAccess.RemoveAt(0);
                            options.Editor.RefreshDraft();
                            Capture(options, "options");
                            Require(ribbon.QuickAccessItems.SequenceEqual(qat), "Pending Options draft does not mutate live QAT");
                            checkedDialog = true;
                        }
                        catch (Exception error) { failure = error; }
                        finally { ((Button)options.CancelButton!).PerformClick(); }
                    };
                }
                ribbon.OptionsDialogOpening += OnOpening;
                try
                {
                    var closed = ribbon.ShowRibbonCustomization(form);
                    if (failure is not null) throw new InvalidOperationException("Options composition check failed.", failure);
                    Require(checkedDialog, "Modal customization really opened");
                    return closed;
                }
                finally { ribbon.OptionsDialogOpening -= OnOpening; }
            }
            void Transition(string operation, Action action)
            {
                action();
                if (ribbon.IsAnimating) { animated++; log.Add($"Motion ran: {operation}"); }
                Settle(operation);
            }
            void Settle(string operation)
            {
                var timer = Stopwatch.StartNew();
                do
                {
                    Application.DoEvents();
                    if (!ribbon.IsAnimating) return;
                    Thread.Sleep(12);
                } while (timer.Elapsed < TimeSpan.FromSeconds(3));
                throw new InvalidOperationException($"Motion did not settle after {operation}");
            }
            void Capture(Control control, string name, Control? front = null)
            {
                using var image = new Bitmap(control.Width, control.Height);
                control.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                if (front is not null)
                {
                    // The native print order can differ from live HWND z-order.
                    using var paneImage = new Bitmap(front.Width, front.Height);
                    front.DrawToBitmap(paneImage, new Rectangle(Point.Empty, paneImage.Size));
                    using var graphics = Graphics.FromImage(image);
                    var origin = front.PointToScreen(Point.Empty);
                    graphics.DrawImageUnscaled(paneImage, origin.X - control.Left, origin.Y - control.Top);
                }
                image.Save(Path.Combine(directory, name + ".png"));
            }
        }
        catch (Exception error) { log.Add("FAIL: " + error); return 1; }
        finally { File.WriteAllLines(Path.Combine(directory, "result.log"), log); }
    }

    private static Control Popup()
    {
        Control? surface = null;
        EnumThreadWindows(GetCurrentThreadId(), (handle, _) =>
        {
            if (Control.FromHandle(handle) is ToolStripDropDown { Visible: true } popup)
                surface = popup.Items.OfType<ToolStripControlHost>().Select(i => i.Control).FirstOrDefault() ?? surface;
            return true;
        }, 0);
        return surface ?? throw new InvalidOperationException("Expected a live native popup");
    }

    private static IEnumerable<AccessibleObject> AccessibleChildren(AccessibleObject root) =>
        Enumerable.Range(0, root.GetChildCount()).Select(root.GetChild).OfType<AccessibleObject>();

    private static IEnumerable<Control> Descendants(Control root) =>
        root.Controls.Cast<Control>().SelectMany(child => Descendants(child).Prepend(child));

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }

    private delegate bool WindowVisitor(nint window, nint state);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread, WindowVisitor visitor, nint state);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
}
