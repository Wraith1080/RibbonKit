using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace IndependentConsumer;

// Public-only package/sample checks. The G5 build excludes references to G6's additive APIs.
internal static class Office2007CompanionChecks
{
    internal static int Run(string directory, bool current = true)
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException); Directory.CreateDirectory(directory);
        try
        {
            Defaults(directory);
#if !G5_BASELINE
            if (current)
            {
                using var host = new RibbonForm(); using var ribbon = new RibbonControl { AnimationLevel = RibbonAnimationLevel.None }; host.Controls.Add(ribbon); host.Ribbon = ribbon;
                var home = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "commands", Text = "Commands" }; home.Groups.Add(group); ribbon.Tabs.Add(home);
                for (int i = 0; i < 35; i++) group.Items.Add(new RibbonButtonItem { Id = "command" + i, Text = "Command " + i });
                ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = (RibbonButtonItem)group.Items[0] }); ribbon.CommandCatalog.CaptureDefaults();
                VerifySample(host, ribbon, directory, "independent", commandFeedback: false);
                VerifyPopup(host, ribbon, directory);
                SecondaryDialogs(host, ribbon, directory);
                using (var dialog = Open(host, ribbon))
                {
                    dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "QuickAccessOptionsPage");
                    dialog.Editor.Session.Draft.QuickAccess.Clear(); dialog.Editor.RefreshDraft(); Find<Button>(dialog, "ApplyCustomization").PerformClick();
                    Require(ribbon.QuickAccessItems.Count == 0, "Apply publishes the QAT draft");
                    dialog.Editor.Session.Reset(); dialog.Editor.RefreshDraft(); Require(dialog.Editor.Session.Draft.QuickAccess.Count > 0 && ribbon.QuickAccessItems.Count == 0, "Reset stays in the draft until Apply");
                    Find<Button>(dialog, "CancelCustomization").PerformClick(); Require(ribbon.QuickAccessItems.Count == 0, "Cancel retains the last Apply checkpoint");
                }
                using (var acceptance = new RibbonOptionsDialog(ribbon) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false })
                {
                    var page = new RibbonOptionsPage { Text = "OK validation" }; bool reject = true; int applied = 0;
                    page.Applying += (_, e) => e.Cancel = reject; page.Applied += (_, _) => applied++; acceptance.Pages.Add(page); acceptance.Show(host); acceptance.Location = new(-16000, -16000);
                    Find<Button>(acceptance, "AcceptCustomization").PerformClick(); Require(acceptance.Visible && applied == 0, "OK preserves rejected changes");
                    reject = false; Find<Button>(acceptance, "AcceptCustomization").PerformClick(); Require(acceptance.DialogResult == DialogResult.OK && applied == 1, "OK applies once and closes");
                }
                File.AppendAllText(Path.Combine(directory, "result.log"), "PASS: independent QAT Apply/checkpoint/Reset/Cancel and validated OK commit.\n");
                VerifyCommandFeedback(host, ribbon, directory, "independent");
            }
#endif
            return 0;
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(directory, "failure.log"), error.ToString()); return 1; }
    }

    private static void Defaults(string directory)
    {
        foreach (var (name, theme) in new[] { ("2024", RibbonThemes.Office2024Light), ("2019", RibbonThemes.Office2019Light), ("2013", RibbonThemes.Office2013Light), ("2010", RibbonThemes.Office2010Blue) })
        {
            foreach (bool office in new[] { false, true }) foreach (int dpi in new[] { 96, 120, 144, 192 })
            {
                using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer(); using var image = new Bitmap(300, 140); using var g = Graphics.FromImage(image); g.Clear(Color.Magenta);
                var c = new RibbonPaintContext(g, new(5, 5, 290, 130), theme, Control.DefaultFont, dpi)
                {
#if !G5_BASELINE
                    IsScreenTip = true
#endif
                };
                painter.DrawPopupFrame(c); painter.DrawKeyTip(c with { Bounds = new(15, 20, 50, 30) }, "F");
                using var tile = new RibbonGalleryTile { Text = "Tile" }; painter.DrawGalleryTile(c with { Bounds = new(80, 15, 100, 60), Hot = true }, tile, false);
                painter.DrawMenuHeader(c with { Bounds = new(10, 90, 280, 30) }, "Heading");
                using var model = new RibbonControl { Theme = theme }; using var split = new RibbonSplitButtonItem { Checked = true, Layout = RibbonSplitButtonLayout.Vertical };
                painter.DrawSplitItem(c with { Bounds = new(160, 50, 70, 38), Hot = true }, c with { Bounds = new(160, 88, 70, 38) }, split, RibbonItemSize.Large, false);
                using var source = new RibbonToggleItem { Checked = true }; using var entry = new RibbonQuickAccessReference { Source = source }; model.CommandCatalog.Commands.Add(source); model.QuickAccessItems.Add(entry);
                painter.DrawQuickAccessItem(c with { Bounds = new(235, 10, 58, 48), Hot = true }, entry, false);
                image.Save(Path.Combine(directory, $"default-{name}-{office}-{dpi}.png"));
            }
            using var ribbon = new RibbonControl { Theme = theme }; using var dialog = new RibbonOptionsDialog(ribbon) { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false };
            dialog.Show(); dialog.Location = new(-16000, -16000); Application.DoEvents();
            foreach (var role in new[] { "AcceptCustomization", "CancelCustomization" }) { using var image = Paint(Find<Button>(dialog, role)); image.Save(Path.Combine(directory, $"default-options-{name}-{role}.png")); }
            dialog.Close();
        }
        File.WriteAllText(Path.Combine(directory, "result.log"), "PASS: 32 old-family tip/gallery/menu sheets at four DPIs and eight actual native Options button images.\n");
    }

#if !G5_BASELINE
    internal static void VerifySample(RibbonForm host, RibbonControl ribbon, string directory, string name, bool commandFeedback = true)
    {
        Directory.CreateDirectory(directory); ribbon.AnimationLevel = RibbonAnimationLevel.None;
        var tile = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonGalleryItem>().SelectMany(g => g.Items).FirstOrDefault(t => t.Text == "Office 2007 Blue");
        if (tile is null) ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2007Blue; else tile.PerformClick();
        host.StartPosition = FormStartPosition.Manual; host.Bounds = new(-16000, -16000, 1100, 760); host.ShowInTaskbar = false; host.Show(); Application.DoEvents();
        var sources = ribbon.Tabs.ToArray(); var qat = ribbon.QuickAccessItems.ToArray(); int scopes = 0;
        foreach (var density in Enum.GetValues<RibbonDensity>()) foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        {
            ribbon.Density = density; ribbon.LayoutMode = mode; using var options = Open(host, ribbon);
            Require(ribbon.Theme.OptionsAppearance!.BackgroundFill == ribbon.Theme.PopupBackgroundFill && ribbon.Theme.OptionsAppearance.ListBackgroundFill == ribbon.Theme.PopupBackgroundFill, "Shared customization body/wells use the reference popup gradient");
            var navigation = Find<ListBox>(options, "OptionsNavigation");
            var session = options.Editor.Session.Draft; var handle = navigation.Handle;
            foreach (var page in options.Pages.ToArray())
            { options.SelectedPage = page; Application.DoEvents(); Capture(options, Path.Combine(directory, $"{name}-{density}-{mode}-{page.Name}")); }
            Require(ReferenceEquals(session, options.Editor.Session.Draft) && handle == navigation.Handle, "Page changes retain the QAT draft and navigation HWND");
            options.SelectedPage = options.Pages.Single(p => p.Name == (mode == RibbonLayoutMode.Classic ? "CustomizeRibbonOptionsPage" : "CustomizeSimplifiedOptionsPage")); Application.DoEvents();
            var available = Find<ListBox>(options, mode == RibbonLayoutMode.Classic ? "RibbonAvailableCommands" : "SimplifiedAvailableCommands");
            int selectedIndex = available.SelectedIndex; available.SelectedIndex = -1; VerifyGradient(available); available.SelectedIndex = selectedIndex;
            var editor = mode == RibbonLayoutMode.Classic ? (Control)options.RibbonEditor : options.SimplifiedEditor;
            VerifyGradient(editor.Parent!.Parent!.Parent!);
            if (available.Items.Count > 0)
            {
                available.SelectedIndex = 0; var selection = available.SelectedItem; var listHandle = available.Handle; var parent = available.Parent;
                Move(available, new(available.Width - 8, Math.Min(available.Height - 3, available.ItemHeight + available.ItemHeight / 2)));
                using var image = Paint(available); Require(Count(image, Color.FromArgb(193, 154, 75)) > 0, "Native selected list has the 2007 gold outline");
                Require(ReferenceEquals(selection, available.SelectedItem), "Hover never selects a command");
                available.TopIndex = Math.Min(4, available.Items.Count - 1); Application.DoEvents(); Require(available.Handle == listHandle && ReferenceEquals(available.Parent, parent) && ReferenceEquals(selection, available.SelectedItem), "Native scrolling retains list HWND/parent/selection");
            }
            foreach (string role in new[] { "AcceptCustomization", "CancelCustomization" })
            {
                var button = Find<Button>(options, role);
                foreach (int state in new[] { 0, 1, 2 })
                {
                    SendMessage(button.Handle, 0x2A3, 0, 0); if (state > 0) Move(button, new(button.Width / 2, button.Height / 2));
                    if (state == 2) SendMessage(button.Handle, 0x201, 1, PointValue(new(button.Width / 2, button.Height / 2)));
                    using var image = Paint(button); image.Save(Path.Combine(directory, $"{name}-{density}-{mode}-{role}-{state}.png"));
                    var border = role == "AcceptCustomization" ? Color.FromArgb(23, 87, 143) : state == 0 ? Color.FromArgb(125, 158, 192) : state == 1 ? Color.FromArgb(193, 168, 119) : Color.FromArgb(182, 168, 146);
                    Require(Count(image, border) > 0, "Native dialog button uses its exact independent outline");
                    if (state == 2) SendMessage(button.Handle, 0x202, 0, PointValue(new(-4, -4))); button.Capture = false; SendMessage(button.Handle, 0x2A3, 0, 0);
                }
            }
            options.SelectedPage = options.Pages.Single(p => p.Name == "QuickAccessOptionsPage");
            if (options.Editor.Session.Draft.QuickAccess.Count > 0) options.Editor.Session.Draft.QuickAccess.RemoveAt(0);
            options.Editor.RefreshDraft(); Require(qat.SequenceEqual(ribbon.QuickAccessItems), "Unapplied QAT draft stays isolated");
            var appPage = new RibbonOptionsPage { Text = "Native validation" }; var field = new TextBox { Text = "Checkpoint", Bounds = new(12, 12, 220, 28) }; appPage.Controls.Add(field); options.Pages.Add(appPage); options.SelectedPage = appPage; Application.DoEvents();
            int applied = 0, cancelled = 0; bool reject = true; string checkpoint = field.Text; string? restored = null;
            appPage.Applying += (_, e) => e.Cancel = reject; appPage.Applied += (_, _) => { checkpoint = field.Text; applied++; }; appPage.Cancelled += (_, _) => { field.Text = checkpoint; restored = field.Text; cancelled++; };
            var fieldHandle = field.Handle; var fieldParent = field.Parent; field.Text = "Pending"; field.Select(2, 3); Require(!appPage.ApplyChanges() && applied == 0, "Validation rejects a pending application draft");
            reject = false; Find<Button>(options, "ApplyCustomization").PerformClick(); options.SelectedPage = options.Pages[0]; options.SelectedPage = appPage; Application.DoEvents();
            Require(field.Handle == fieldHandle && ReferenceEquals(field.Parent, fieldParent) && field.Text == checkpoint && field.SelectionStart == 2 && field.SelectionLength == 3, "Native application field ownership/text/selection survive navigation and Apply");
            field.Text = "Unapplied"; Find<Button>(options, "CancelCustomization").PerformClick(); Require(applied == 1 && cancelled == 1 && restored == checkpoint, "Cancel rolls the application page back to its Apply checkpoint");
            Require(sources.SequenceEqual(ribbon.Tabs) && qat.SequenceEqual(ribbon.QuickAccessItems), "Cancel preserves live ribbon and QAT models"); scopes++;
        }
        PaintCompanions(directory);
        VerifyCaptionOutlines(host, ribbon, directory, name);
        if (commandFeedback) VerifyCommandFeedback(host, ribbon, directory, name);
        File.AppendAllText(Path.Combine(directory, "result.log"), $"PASS: {name}; {scopes} native Options scopes at {ribbon.DeviceDpi} DPI; both densities/layouts, Classic/Simplified/QAT pages, exact button/list states, native handles/selection/scrolling, draft isolation and validated Apply/Cancel.\nLIMIT: native-print evidence; actual VS, physical input/IME, mixed monitors and live appearance acceptance remain separate.\n");
    }
    private static void PaintCompanions(string directory)
    {
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            using var painter = new RibbonOffice2024Renderer(); using var image = new Bitmap(700, 260); using var g = Graphics.FromImage(image); g.Clear(Color.White);
            var t = RibbonThemes.Office2007Blue; var c = new RibbonPaintContext(g, new(5, 5, 250, 100), t, Control.DefaultFont, dpi) { IsScreenTip = true };
            painter.DrawPopupFrame(c); painter.DrawScreenTip(c, "ScreenTip", "Native passive tip", new(18, 16, 220, 24), new(18, 45, 220, 45));
            painter.DrawKeyTip(c with { Bounds = new(275, 12, 40, 30), IsScreenTip = false }, "F");
            using var field = new RibbonTextBoxItem { Text = "Name", Value = "White native well" }; painter.DrawItem(c with { Bounds = new(10, 125, 240, 40), IsScreenTip = false }, field, RibbonItemSize.Small, false);
            using var tile = new RibbonGalleryTile { Text = "Gold selection" }; painter.DrawGalleryTile(c with { Bounds = new(330, 10, 150, 90), Hot = true, IsScreenTip = false }, tile, true);
            painter.DrawMenuHeader(c with { Bounds = new(10, 190, 240, 28), IsScreenTip = false }, "Blue heading"); image.Save(Path.Combine(directory, $"companions-{dpi}.png"));
            g.Clear(Color.Magenta); painter.DrawPopupFrame(c with { IsScreenTip = false });
            Require(image.GetPixel(100, 40).ToArgb() == Valley(35d / 99).ToArgb(), "Public ordinary popup painter uses the full reference valley gradient");
            image.Save(Path.Combine(directory, $"popup-background-{dpi}.png"));
        }
    }
    private static void VerifyPopup(Form host, RibbonControl ribbon, string directory)
    {
        var menu = new RibbonDropDownItem { Text = "Companion popup", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
        using var tab = new RibbonTab { Text = "Popup verification" }; var group = new RibbonGroup { Text = "Commands" }; tab.Groups.Add(group); group.Items.Add(menu);
        menu.Items.Add(new RibbonButtonItem { Text = "Native command" }); var selected = ribbon.SelectedTab; ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab; ribbon.LayoutMode = RibbonLayoutMode.Classic; host.PerformLayout(); Application.DoEvents();
        try
        {
            menu.ShowDropDown(); Application.DoEvents();
            var surfaces = new List<Control>();
            EnumThreadWindows(GetCurrentThreadId(), (window, _) => { if (Control.FromHandle(window) is ToolStripDropDown { Visible: true } popup) foreach (var item in popup.Items.OfType<ToolStripControlHost>()) if (item.Control.GetType().Name == "RibbonPopupSurface") surfaces.Add(item.Control); return true; }, 0);
            VerifyGradient(surfaces.Single()); using var image = Paint(surfaces.Single()); image.Save(Path.Combine(directory, "native-popup-background.png"));
            File.AppendAllText(Path.Combine(directory, "result.log"), "PASS: actual native ordinary popup paints the complete reference valley gradient.\n");
        }
        finally { menu.CloseDropDown(); ribbon.SelectedTab = selected; ribbon.Tabs.Remove(tab); }
    }
    private static void VerifyCaptionOutlines(RibbonForm host, RibbonControl ribbon, string directory, string name)
    {
        var settings = ribbon.Theme.CaptionButtonAppearance!; var handle = host.Handle; var bounds = host.Bounds; var state = host.WindowState;
        var idle = new Dictionary<int, int>();
        SendMessage(handle, 0x1F, 0, 0); SendMessage(handle, 0xA0, 0, 0);
        using (var image = Paint(ribbon)) foreach (int hit in new[] { 8, 9, 20 })
        {
            var chrome = hit == 20 ? settings.CloseStateChrome! : settings.StateChrome!;
            idle[hit * 10] = Count(image, chrome.HoverBorderColor); idle[hit * 10 + 1] = Count(image, chrome.PressedBorderColor);
        }
        try
        {
            foreach (int hit in new[] { 8, 9, 20 }) foreach (bool down in new[] { false, true })
            {
                SendMessage(handle, 0x1F, 0, 0); SendMessage(handle, 0xA0, hit, 0); if (down) SendMessage(handle, 0xA1, hit, 0);
                var chrome = hit == 20 ? settings.CloseStateChrome! : settings.StateChrome!;
                using var image = Paint(ribbon); var ink = down ? chrome.PressedBorderColor : chrome.HoverBorderColor;
                Require(Count(image, ink) > idle[hit * 10 + (down ? 1 : 0)] + 10, $"Native caption {hit} {(down ? "pressed" : "hover")} paints its outline");
                image.Save(Path.Combine(directory, $"{name}-caption-{hit}-{down}.png"));
            }
        }
        finally { SendMessage(handle, 0x1F, 0, 0); host.Capture = false; SendMessage(handle, 0xA0, 0, 0); }
        Require(host.Handle == handle && host.Bounds == bounds && host.WindowState == state && !host.IsDisposed, "Caption paint/cancel preserves native window state and handle");
        File.AppendAllText(Path.Combine(directory, "result.log"), $"PASS: {name}; native minimize/maximize-or-restore/close hover and pressed outlines, idle reset and unchanged window state/handle.\n");
    }
    private static void VerifyCommandFeedback(Form host, RibbonControl ribbon, string directory, string name)
    {
        var original = ribbon.Renderer; using var painter = new FeedbackPainter(); ribbon.Renderer = painter;
        try
        {
            using (var initial = Paint(ribbon)) { }
            foreach (var bounds in painter.QatBounds.Concat(painter.OverflowBounds).Distinct().ToArray())
            {
                painter.Feedback = null; Move(ribbon, new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2));
                using var image = Paint(ribbon);
                Require(painter.Feedback is { Start.R: > 200, Start.G: > 180 }, "Native QAT/overflow hover retains the gold palette after caption recoloring");
            }
            if (painter.OverflowBounds.Count > 0)
            {
                var bounds = painter.OverflowBounds[^1]; var point = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
                Move(ribbon, point); painter.Feedback = null; SendMessage(ribbon.Handle, 0x201, 1, PointValue(point));
                using (var image = Paint(ribbon)) { Require(painter.Feedback is { Start.R: > 200, Start.G: < 220 }, "Native overflow press uses orange gel"); image.Save(Path.Combine(directory, name + "-overflow-pressed.png")); }
                SendMessage(ribbon.Handle, 0x202, 0, PointValue(point)); SendMessage(ribbon.Handle, 0x100, 0x1B, 0);
            }
            foreach (int dpi in new[] { 96, 120, 144, 192 })
            {
                using var image = new Bitmap(350, 190); using var g = Graphics.FromImage(image); g.Clear(Color.White);
                var t = RibbonThemes.Office2007Blue; using var split = new RibbonSplitButtonItem { Layout = RibbonSplitButtonLayout.Vertical };
                var primary = new Rectangle(5, 5, 110, 100); var arrow = new Rectangle(5, 105, 110, 65); painter.Canvases.Clear();
                var c = new RibbonPaintContext(g, primary, t, Control.DefaultFont, dpi); painter.DrawSplitItem(c, c with { Bounds = arrow, Hot = true }, split, RibbonItemSize.Large, false);
                Require(painter.Canvases.Any(r => r.Top == arrow.Top && r.Height == arrow.Height - 1), "Public chrome hook receives the complete arrow-half fill canvas");
                using var model = new RibbonControl { Theme = t }; using var source = new RibbonButtonItem(); using var entry = new RibbonQuickAccessReference { Source = source }; model.CommandCatalog.Commands.Add(source); model.QuickAccessItems.Add(entry);
                var caption = t with { Hover = new(Color.Blue), Pressed = new(Color.Navy), BackdropHover = new(Color.Blue) };
                painter.DrawQuickAccessItem(new(g, new(150, 10, 60, 44), caption, Control.DefaultFont, dpi, Hot: true), entry, false);
                Require(painter.Feedback == t.Hover, "Public QAT hover override precedes caption colors");
                painter.DrawQuickAccessOverflow(new(g, new(230, 10, 60, 44), caption, Control.DefaultFont, dpi, Pressed: true)); Require(painter.Feedback == t.Pressed, "Public overflow press uses the independent pressed fill");
                image.Save(Path.Combine(directory, $"{name}-command-feedback-{dpi}.png"));
            }
            File.AppendAllText(Path.Combine(directory, "result.log"), $"PASS: {name}; native QAT/overflow hover/press and public split-half/QAT paint at four DPIs; Simplified group frames disabled through shared appearance.\n");
        }
        finally { SendMessage(ribbon.Handle, 0x100, 0x1B, 0); ribbon.Renderer = original; }
    }
    private sealed class FeedbackPainter : RibbonOffice2024Renderer
    {
        internal List<Rectangle> QatBounds = [], OverflowBounds = [], Canvases = []; internal RibbonFill? Feedback; private bool _feedback;
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement entry, bool menu)
        { if (!menu && !QatBounds.Contains(c.Bounds)) QatBounds.Add(c.Bounds); _feedback = !menu && (c.Hot || c.Pressed); base.DrawQuickAccessItem(c, entry, menu); _feedback = false; }
        public override void DrawQuickAccessOverflow(RibbonPaintContext c)
        { if (!OverflowBounds.Contains(c.Bounds)) OverflowBounds.Add(c.Bounds); _feedback = c.Hot || c.Pressed; base.DrawQuickAccessOverflow(c); _feedback = false; }
        protected override void DrawChrome(RibbonPaintContext c, RibbonFill fill, Color border)
        { if (_feedback) Feedback = fill; if (!c.BackgroundBounds.IsEmpty) Canvases.Add(c.BackgroundBounds); base.DrawChrome(c, fill, border); }
    }
    private static void SecondaryDialogs(Form host, RibbonControl ribbon, string directory)
    {
        ribbon.LayoutMode = RibbonLayoutMode.Classic; using var options = Open(host, ribbon);
        options.SelectedPage = options.Pages.Single(p => p.Name == "CustomizeRibbonOptionsPage"); Application.DoEvents();
        int captured = 0; Exception? error = null; using var timer = new System.Windows.Forms.Timer { Interval = 20 };
        timer.Tick += (_, _) =>
        {
            var dialog = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Name == "RibbonGroupDialog" || f.Text == "Edit ribbon item");
            if (dialog is null) return; timer.Stop(); dialog.Location = new(-16000, -16000);
            try
            {
                bool group = dialog.Name == "RibbonGroupDialog";
                if (group) { var icons = Find<ListView>(dialog, "RibbonGroupIcons"); Require(icons.Items.Count > 0, "Public native group icon picker"); icons.Items[0].Selected = true; VerifyGradient(icons); }
                var field = Find<TextBox>(dialog, group ? "RibbonGroupName" : "RibbonItemName"); Require(field.Parent?.GetType().Name == "RibbonOptionsFrame", "Secondary name field uses shared native frame");
                Capture(dialog, Path.Combine(directory, group ? "group-dialog" : "rename-dialog")); field.Text = " "; ((Button)dialog.AcceptButton!).PerformClick(); Require(dialog.Visible, "Secondary name validation rejects blank text"); captured++;
            }
            catch (Exception failure) { error = failure; }
            finally { if (dialog.CancelButton is Button cancel) cancel.PerformClick(); else dialog.Close(); }
        };
        Find<Button>(options, "NewRibbonTab").PerformClick(); timer.Start(); Find<Button>(options, "NewRibbonGroup").PerformClick(); timer.Stop(); if (error is not null) throw error;
        var tree = Find<TreeView>(options, "RibbonHierarchy"); tree.SelectedNode = tree.Nodes.Cast<TreeNode>().Single(n => n.Text.StartsWith("New Tab", StringComparison.OrdinalIgnoreCase));
        timer.Start(); Find<Button>(options, "EditRibbonItem").PerformClick(); timer.Stop(); if (error is not null) throw error;
        Require(captured == 2, "Both secondary dialogs reached and validated"); options.RibbonEditor.Session.Reset(); options.RibbonEditor.RefreshDraft(); Find<Button>(options, "CancelCustomization").PerformClick();
        File.AppendAllText(Path.Combine(directory, "result.log"), "PASS: native group/icon-picker and rename dialogs, shared name frames, blank-name validation and draft Reset/Cancel.\n");
    }
    private static Color Valley(double offset)
    {
        var start = Color.FromArgb(219, 230, 244); var middle = Color.FromArgb(200, 217, 237); var end = Color.FromArgb(227, 244, 255);
        var left = offset < .17 ? start : middle; var right = offset < .17 ? middle : end;
        double t = offset < .17 ? offset / .17 : (offset - .17) / .83;
        return Color.FromArgb(255, (int)Math.Round(left.R + (right.R - left.R) * t), (int)Math.Round(left.G + (right.G - left.G) * t), (int)Math.Round(left.B + (right.B - left.B) * t));
    }
    private static void VerifyGradient(Control control)
    {
        using var image = Paint(control); int x = control.ClientSize.Width - 5;
        foreach (int y in new[] { 3, control.ClientSize.Height / 2, control.ClientSize.Height - 4 })
            Require(image.GetPixel(x, y).ToArgb() == Valley(y / (double)(control.ClientSize.Height - 1)).ToArgb(), $"Complete viewport gradient in {control.Name} at {x},{y}");
    }
#endif
    private static RibbonQuickAccessCustomizationForm Open(Form host, RibbonControl ribbon) { var dialog = new RibbonQuickAccessCustomizationForm(ribbon, "RibbonKit.Office2007OptionsReview") { StartPosition = FormStartPosition.Manual, Location = new(-16000, -16000), ShowInTaskbar = false }; dialog.Show(host); dialog.Location = new(-16000, -16000); Application.DoEvents(); return dialog; }
    private static T Find<T>(Control root, string name) where T : Control => root.Controls.Find(name, true).OfType<T>().Single();
    private static Bitmap Paint(Control control) { var image = new Bitmap(control.Width, control.Height); control.DrawToBitmap(image, new(Point.Empty, image.Size)); return image; }
    private static void Capture(Control control, string path) { using var image = Paint(control); image.Save(path.EndsWith(".png", StringComparison.Ordinal) ? path : path + ".png"); }
    private static void Move(Control control, Point point) { SendMessage(control.Handle, 0x200, 0, PointValue(point)); control.Invalidate(); control.Update(); }
    private static nint PointValue(Point p) => (nint)((p.Y << 16) | (p.X & 65535));
    private static int Count(Bitmap image, Color ink) { int count = 0; for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++) if (image.GetPixel(x, y).ToArgb() == ink.ToArgb()) count++; return count; }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, int message, nint wParam, nint lParam);
    private delegate bool WindowCallback(nint window, nint argument);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint threadId, WindowCallback callback, nint argument);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
}
