using System.Runtime.InteropServices;
using System.IO;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace IndependentConsumer;

// Public-API validation only. No reflection, friend access, Showcase resources,
// accessibility provider or application-owned implementation of ribbon behavior.
internal static class SplitInteractionChecks
{
    [DllImport("user32.dll")] private static extern nint SendMessage(nint hwnd, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint hwnd);

    internal static int Run(string directory, bool flat = false, bool touchHorizontalOnly = false, bool groupPaddingOnly = false)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2);
        WF.Application.EnableVisualStyles();
        Directory.CreateDirectory(directory);
        var recording = new Recording();
        using RibbonRenderer renderer = flat ? new FlatRecorder(recording) : new Recorder(recording);
        using var icon = new Bitmap(32, 32);
        using (var graphics = Graphics.FromImage(icon))
        {
            graphics.Clear(Color.Transparent);
            using var pen = new Pen(Color.SteelBlue, 2);
            graphics.DrawRectangle(pen, 6, 3, 20, 26);
            graphics.DrawLine(pen, 9, 13, 23, 13);
        }
        using var form = new WF.Form { Text = "Public split-button checks", Width = 1200, Height = 500, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Renderer = renderer };
        var tab = new RibbonTab { Text = "Home", KeyTip = "H" };
        var group = new RibbonGroup { Text = "Split command actions", ReductionMode = RibbonGroupReductionMode.ResizeThenCollapse };
        var split = new RibbonSplitButtonItem { Text = "Paste", KeyTip = "V", DropDownKeyTip = "M", Layout = RibbonSplitButtonLayout.Vertical, Image = icon, CheckOnClick = true };
        var plain = new RibbonButtonItem { Text = "Paste text only", KeyTip = "T" };
        var nested = new RibbonSplitButtonItem { Text = "Special", KeyTip = "S", DropDownKeyTip = "O", Layout = RibbonSplitButtonLayout.Vertical };
        nested.Items.Add(new RibbonButtonItem { Text = "Nested choice" });
        split.Items.AddRange(plain, nested, new RibbonButtonItem { Text = "Unavailable", Enabled = false });
        group.Items.AddRange(split, new RibbonButtonItem { Text = "Neighbour", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) });
        RibbonItem[] captions = [new RibbonButtonItem(), new RibbonToggleItem { Checked = true }, new RibbonDropDownItem(),
            new RibbonSplitButtonItem { Checked = true }, new RibbonSplitButtonItem { Layout = RibbonSplitButtonLayout.Vertical, Checked = true }];
        foreach (var caption in captions) { caption.Text = "Keep source formatting"; caption.Image = icon; group.Items.Add(caption); }
        var reportedCaption = new RibbonSplitButtonItem { Text = "Track changes", Image = icon, Checked = true };
        group.Items.Add(reportedCaption);
        tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Controls.Add(new WF.TextBox { Dock = WF.DockStyle.Fill }); form.Controls.Add(ribbon);
        int primaryClicks = 0, menuOpens = 0, plainClicks = 0, nestedClicks = 0;
        split.Click += (_, _) => primaryClicks++;
        split.DropDownOpening += (_, _) => menuOpens++;
        plain.Click += (_, _) => plainClicks++;
        nested.Click += (_, _) => nestedClicks++;
        Exception? error = null;
        var results = new List<string>();
        form.Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield();
                form.Activate(); SetForegroundWindow(form.Handle);
                for (int attempt = 0; attempt < 20 && GetForegroundWindow() != form.Handle; attempt++) await Task.Delay(50);
                Check(GetForegroundWindow() == form.Handle, "Validation Form owns foreground focus before native input");
                ribbon.Refresh();
                if (groupPaddingOnly) { await CheckGroupPadding(); return; }
                if (touchHorizontalOnly) { await CheckTouchHorizontal(); return; }
                CheckRibbonSurface();
                int bodyHeight = recording.BodyBounds.Height;
                var originalTheme = ribbon.Theme;
                foreach (var caption in captions.Append(reportedCaption))
                {
                    var lines = recording.Captions[caption];
                    var bounds = caption is RibbonSplitButtonItem captionSplit
                        ? Rectangle.Union(recording.Parts[captionSplit].Primary.Bounds, recording.Parts[captionSplit].Arrow.Bounds) : recording.Items[caption];
                    Check(lines.Count == 2 && lines[1].Bounds.Top - lines[0].Bounds.Top < ribbon.Font.Height
                        && lines[1].Bounds.Bottom <= bounds.Bottom - (int)Math.Round(3 * ribbon.DeviceDpi / 96.0),
                        $"{caption.GetType().Name}{(caption is RibbonSplitButtonItem s ? " " + s.Layout : "")} fits two compact Large caption rows");
                    CheckCaption(caption);
                }
                await CaptureVisible(ribbon, "large-captions-all-types");
                foreach (var caption in captions) group.Items.Remove(caption);
                for (int index = 0; index < captions.Length; index++)
                {
                    var caption = captions[index];
                    caption.Text = "Keep the original document source formatting";
                    group.Items.Add(caption); ribbon.Refresh();
                    var bounds = caption is RibbonSplitButtonItem captionSplit
                        ? Rectangle.Union(recording.Parts[captionSplit].Primary.Bounds, recording.Parts[captionSplit].Arrow.Bounds) : recording.Items[caption];
                    Check(bounds.Width == (int)Math.Round(originalTheme.LargeButtonMaxWidth * ribbon.DeviceDpi / 96d)
                        && recording.BodyBounds.Height == bodyHeight && recording.Captions[caption][^1].Text.EndsWith('…'),
                        $"{caption.GetType().Name} respects the default width cap with a second-row ellipsis and fixed body height");
                    CheckCaption(caption);
                    await CaptureVisible(ribbon, $"large-long-caption-{index}");
                    int visibleLength = string.Concat(recording.Captions[caption].Select(line => line.Text)).Length;
                    ribbon.Theme = originalTheme with { LargeButtonMaxWidth = 160 }; ribbon.Refresh();
                    bounds = caption is RibbonSplitButtonItem widerSplit
                        ? Rectangle.Union(recording.Parts[widerSplit].Primary.Bounds, recording.Parts[widerSplit].Arrow.Bounds) : recording.Items[caption];
                    Check(bounds.Width == (int)Math.Round(160 * ribbon.DeviceDpi / 96d) && recording.BodyBounds.Height == bodyHeight
                        && string.Concat(recording.Captions[caption].Select(line => line.Text)).Length > visibleLength
                        && caption.Text == "Keep the original document source formatting",
                        "Changing the public width metric reflows more text without changing the model caption or body height");
                    CheckCaption(caption);
                    await CaptureVisible(ribbon, $"large-configured-caption-{index}");
                    ribbon.Theme = originalTheme;
                    caption.Text = "Paste"; ribbon.Refresh();
                    Check(recording.Captions[caption].Count == 1 && recording.BodyBounds.Height == bodyHeight, "Short caption reflows to one row without changing body height");
                    if (caption is RibbonSplitButtonItem { Layout: RibbonSplitButtonLayout.Vertical })
                        Check(recording.Arrows[caption].Top >= recording.Captions[caption][0].Bounds.Bottom, "Single-row vertical split keeps its arrow below the caption");
                    group.Items.Remove(caption); caption.Dispose();
                }
                form.Width = 900; ribbon.Refresh();
                Check(recording.Captions[reportedCaption].Count == 2 && recording.BodyBounds.Height == bodyHeight, "Reported horizontal caption fits at the unchanged body height");
                await CaptureVisible(ribbon, "large-caption-reported");
                group.Items.Remove(reportedCaption); reportedCaption.Dispose();
                var parts = Parts(ribbon);
                Check(recording.BodyCalls > 0 && recording.TextCalls > 0 && recording.ArrowCalls > 0 && recording.ImageCalls > 0,
                    "Public body/text/image/arrow hooks are used by the shared content pipeline");
                Check(parts.Primary.Bounds.Bottom == parts.Arrow.Bounds.Top, "Large vertical halves share one edge");
                Capture(ribbon, "large-normal");
                split.Layout = RibbonSplitButtonLayout.Horizontal;
                parts = Parts(ribbon);
                Check(parts.Primary.Bounds.Right == parts.Arrow.Bounds.Left, "Large horizontal arrangement is explicit");
                Capture(ribbon, "large-horizontal");
                CaptureChrome(ribbon, split, "large-horizontal");
                split.Layout = RibbonSplitButtonLayout.Vertical;
                parts = Parts(ribbon);
                CaptureChrome(ribbon, split, "large-vertical");
                Mouse(ribbon, 0x201, parts.Primary.Bounds); Mouse(ribbon, 0x202, parts.Arrow.Bounds);
                Check(primaryClicks == 0 && menuOpens == 0, "Cross-half release activates neither action");
                Click(ribbon, parts.Primary.Bounds);
                Check(primaryClicks == 1 && menuOpens == 0, "Primary mouse click executes exactly once");
                Check(split.Checked, "Primary mouse activation checks the split button");
                Check(!Parts(ribbon).Primary.Focused && !Parts(ribbon).Arrow.Focused, "Mouse focus has no keyboard outline");
                await Key("{HOME}");
                Check(Parts(ribbon).Primary.Focused, "Keyboard focus paints the primary half");
                await Key("{TAB}");
                Check(Parts(ribbon).Arrow.Focused && !Parts(ribbon).Primary.Focused, "Tab visits the menu half separately");
                Capture(ribbon, "arrow-keyboard");
                await Key("+{TAB}{ENTER}");
                Check(primaryClicks == 2 && menuOpens == 0, "Shift+Tab and Enter invoke the primary action once");
                Check(!split.Checked, "Primary keyboard activation unchecks the split button");
                await Key("{TAB} ");
                var popup = Popup();
                Check(primaryClicks == 2 && menuOpens == 1, "Space on the arrow opens without primary execution");
                SendMessage(ribbon.Handle, 0x2A3, 0, 0);
                Check(split.IsDropDownOpen && !split.Checked, "Open menu and primary checked state are independent");
                CheckFill(ribbon, Parts(ribbon).Arrow.Bounds, ribbon.Theme.Pressed.Start, "Arrow stays pressed after the pointer leaves its open menu");
                Capture(ribbon, "arrow-latched-open");
                Capture(popup, "menu-open");
                await Key("{ESC}");
                Check(!split.IsDropDownOpen, "Escape clears the latched open state");
                Check(Parts(ribbon).Arrow.Focused, "Escape restores the menu-half outline");
                await Key("{F10}hv");
                Check(primaryClicks == 3 && menuOpens == 1, "Primary KeyTip invokes exactly once");
                Check(split.Checked, "Primary KeyTip toggles the split button");
                await Key("{F10}hm");
                popup = Popup();
                Check(primaryClicks == 3 && menuOpens == 2, "Menu KeyTip opens only the menu");
                SendMessage(ribbon.Handle, 0x2A3, 0, 0);
                parts = Parts(ribbon);
                CheckFill(ribbon, parts.Primary.Bounds, ribbon.Theme.Checked.Start, "Checked primary remains painted while the arrow menu is open");
                Capture(ribbon, "checked-and-open");
                popup.Refresh(); Click(popup, recording.Items[plain]);
                Check(plainClicks == 1 && primaryClicks == 3, "Menu selection executes independently");
                foreach (var size in new[] { RibbonItemSize.Medium, RibbonItemSize.Small })
                {
                    split.SizePolicy = RibbonSizePolicy.Fixed(size);
                    parts = Parts(ribbon);
                    Check(parts.Primary.Bounds.Right == parts.Arrow.Bounds.Left, size + " uses horizontal halves");
                    Capture(ribbon, size.ToString().ToLowerInvariant());
                    CaptureChrome(ribbon, split, size.ToString().ToLowerInvariant());
                }
                split.Enabled = false;
                parts = Parts(ribbon);
                Click(ribbon, parts.Primary.Bounds); Click(ribbon, parts.Arrow.Bounds);
                split.PerformClick(); split.ShowDropDown();
                Check(primaryClicks == 3 && menuOpens == 2 && !ribbon.Capture, "Disabled split rejects both actions and capture");
                Check(split.Checked, "Disabled primary preserves its checked value");
                Capture(ribbon, "disabled");
                split.Enabled = true;
                split.SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small);
                form.Width = 130;
                ribbon.Refresh();
                Check(recording.Collapsed, "Narrow layout collapses the shared group");
                int groupTopInset = (int)Math.Round(6 * ribbon.DeviceDpi / 96.0);
                int groupBottomInset = (int)Math.Round(8 * ribbon.DeviceDpi / 96.0);
                Check(recording.GroupBounds.Top == recording.BodyBounds.Top + groupTopInset
                    && recording.GroupBounds.Bottom == recording.BodyBounds.Bottom - groupBottomInset,
                    "Collapsed opener painting stays inset from both body edges");
                Click(ribbon, recording.GroupBounds);
                var groupPopup = Popup();
                parts = Parts(groupPopup);
                Check(parts.Primary.Bounds.Bottom == parts.Arrow.Bounds.Top, "Collapsed flyout restores large vertical geometry");
                Capture(groupPopup, "collapsed-group");
                CaptureChrome(groupPopup, split, "collapsed-group");
                Click(groupPopup, parts.Arrow.Bounds);
                popup = Popup();
                Check(!ReferenceEquals(groupPopup, popup) && menuOpens == 3, "Split arrow opens a nested menu from the group");
                var nestedParts = Parts(popup, nested);
                Check(nestedParts.Primary.Bounds.Right == nestedParts.Arrow.Bounds.Left, "Menu-row split stays horizontal");
                CaptureChrome(popup, nested, "menu-row");
                Click(popup, nestedParts.Arrow.Bounds);
                Check(nestedClicks == 0, "Nested split arrow does not execute its primary command");
                Capture(Popup(), "nested-menu");
                await Key("{ESC}");
                Check(Parts(popup, nested).Arrow.Focused, "Nested Escape returns to the nested arrow");
                await Key("{ESC}");
                Check(Parts(groupPopup).Arrow.Focused, "Next Escape preserves the group and restores its split arrow");
                await Key("{ESC}");
                Check(ribbon.Focused, "Final Escape returns to the collapsed group opener");
                group.CanResize = false;
                ribbon.Refresh();
                Check(recording.ScrollCalls > 0, "Public scroll-button hook handles fixed-group overflow");
                await CheckBodyScrollButtons();
                results.Add($"PASS: public split interaction/painting, renderer={(flat ? "flat" : "Office2024")}, DPI={ribbon.DeviceDpi}; accessibility compatibility deferred");
            }
            catch (Exception e) { error = e; results.Add(e.ToString()); }
            finally { File.WriteAllLines(Path.Combine(directory, "result.log"), results); form.Close(); }
        };
        WF.Application.Run(form);
        foreach (string result in results) Console.WriteLine(result);
        return error is null ? 0 : 1;

        async Task CheckGroupPadding()
        {
            foreach (var item in group.Items.ToArray()) { group.Items.Remove(item); item.Dispose(); }
            group.Text = "Modal preview"; group.CanResize = false;
            var preview = new RibbonButtonItem { Text = "Print preview", Image = icon }; group.Items.Add(preview);
            var preceding = new RibbonGroup { Text = "Other", CanResize = false };
            preceding.Items.Add(new RibbonButtonItem { Image = icon, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small) }); tab.Groups.Insert(0, preceding);
            var editing = new RibbonGroup { Text = "Editing", ReductionMode = RibbonGroupReductionMode.Collapse };
            editing.Items.AddRange(Enumerable.Range(0, 8).Select(i => new RibbonButtonItem { Text = "Command " + i, Image = icon }).ToArray()); tab.Groups.Add(editing);
            form.Width = 540;
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            {
                ribbon.Density = density; ribbon.Refresh();
                var expanded = recording.GroupFrames[group]; var collapsed = recording.GroupFrames[editing];
                var content = recording.Items[preview];
                Check(!expanded.Collapsed && content.Left - expanded.Bounds.Left == expanded.Bounds.Right - 1 - content.Right,
                    density + " expanded group has equal padding inside its divider lane");
                Check(collapsed.Collapsed, density + " Editing presents its collapsed opener");
                using (var bitmap = new Bitmap(ribbon.Width, ribbon.Height))
                {
                    ribbon.DrawToBitmap(bitmap, ribbon.ClientRectangle);
                    int y = recording.BodyBounds.Top + (int)Math.Round(20 * ribbon.DeviceDpi / 96d), divider = -1;
                    for (int x = collapsed.Bounds.Right; x < bitmap.Width; x++)
                        if (bitmap.GetPixel(x, y).ToArgb() == ribbon.Theme.Border.ToArgb()) { divider = x; break; }
                    Check(divider >= 0 && collapsed.Bounds.Left - expanded.Bounds.Right == divider - collapsed.Bounds.Right,
                        density + " collapsed opener has equal clearance to the painted divider edges");
                }
                WF.Cursor.Position = ribbon.PointToScreen(new(content.Left + content.Width / 2, content.Top + content.Height / 2));
                await CaptureVisible(ribbon, density + "-expanded-group-hover");
                WF.Cursor.Position = ribbon.PointToScreen(new(collapsed.Bounds.Left + collapsed.Bounds.Width / 2, collapsed.Bounds.Top + collapsed.Bounds.Height / 2));
                await CaptureVisible(ribbon, density + "-collapsed-group-hover");
                Click(ribbon, collapsed.Bounds); await Task.Delay(60); var popup = Popup(); popup.Refresh();
                var contents = editing.Items.Select(i => recording.Items[i]).ToArray();
                Check(contents.Min(r => r.Left) == popup.ClientRectangle.Right - contents.Max(r => r.Right),
                    density + " opened group retains equal popup content padding");
                Capture(popup, density + "-opened-group"); await Key("{ESC}");
            }
            results.Add($"PASS: public group side-padding checks at actual {ribbon.DeviceDpi} DPI");
        }
        async Task CheckTouchHorizontal()
        {
            foreach (var item in group.Items.Where(i => i != split).ToArray()) { group.Items.Remove(item); item.Dispose(); }
            group.CanResize = false; split.Layout = RibbonSplitButtonLayout.Horizontal;
            int S(int value) => (int)Math.Round(value * ribbon.DeviceDpi / 96d);
            foreach (var size in new[] { RibbonItemSize.Large, RibbonItemSize.Medium, RibbonItemSize.Small })
            {
                split.SizePolicy = RibbonSizePolicy.Fixed(size); ribbon.Density = RibbonDensity.Compact;
                var compact = Parts(ribbon);
                ribbon.Density = RibbonDensity.Touch;
                var parts = Parts(ribbon);
                Check(parts.Arrow.Bounds.Width == S(44) && parts.Primary.Bounds.Width >= S(size == RibbonItemSize.Large ? 72 : 56)
                    && parts.Primary.Bounds.Width > parts.Arrow.Bounds.Width && parts.Primary.Bounds.Right == parts.Arrow.Bounds.Left,
                    $"{size} Touch horizontal split has a full 44-logical-pixel arrow and larger primary");
                CaptureChrome(ribbon, split, "touch-horizontal-" + size);
                Mouse(ribbon, 0x200, parts.Arrow.Bounds); await CaptureVisible(ribbon, "touch-horizontal-" + size);
                int clicks = primaryClicks, opens = menuOpens;
                Click(ribbon, parts.Primary.Bounds); Click(ribbon, parts.Arrow.Bounds);
                Check(primaryClicks == clicks + 1 && menuOpens == opens + 1 && split.IsDropDownOpen,
                    size + " primary and arrow mouse targets route separately");
                await Key("{ESC}{HOME}{ENTER}");
                await Key("{TAB} ");
                Check(primaryClicks == clicks + 2 && menuOpens == opens + 2 && split.IsDropDownOpen,
                    size + " primary Enter and arrow Space route separately");
                await Key("{ESC}");
                ribbon.Density = RibbonDensity.Compact;
                var restored = Parts(ribbon);
                Check(restored.Primary.Bounds == compact.Primary.Bounds && restored.Arrow.Bounds == compact.Arrow.Bounds,
                    size + " returns to its original Compact geometry");
            }
            results.Add($"PASS: public Touch horizontal split checks, renderer={(flat ? "neutral" : "Office2024")}, actual DPI={ribbon.DeviceDpi}");
        }
        (RibbonPaintContext Primary, RibbonPaintContext Arrow) Parts(WF.Control surface, RibbonSplitButtonItem? item = null)
        {
            surface.Refresh();
            return recording.Parts[item ?? split];
        }
        async Task Key(string keys)
        {
            nint foreground = GetForegroundWindow();
            if (foreground != form.Handle && WF.Control.FromHandle(foreground) is not WF.ToolStripDropDown)
                throw new InvalidOperationException("Validation Form lost foreground focus before native input");
            WF.SendKeys.SendWait(keys); await Task.Delay(60);
        }
        static WF.Control Popup()
        {
            if (WF.Control.FromHandle(GetForegroundWindow()) is WF.ToolStripDropDown drop)
                return drop.Items.OfType<WF.ToolStripControlHost>().Single().Control;
            throw new InvalidOperationException("Expected an active library popup.");
        }
        static void Mouse(WF.Control control, uint message, Rectangle bounds)
        {
            int x = bounds.Left + bounds.Width / 2, y = bounds.Top + bounds.Height / 2;
            SendMessage(control.Handle, message, message == 0x201 ? 1 : 0, (nint)((y << 16) | (x & 0xffff)));
        }
        static void Click(WF.Control control, Rectangle bounds) { Mouse(control, 0x201, bounds); Mouse(control, 0x202, bounds); }
        void Capture(WF.Control surface, string name)
        {
            surface.Refresh();
            using var bitmap = new Bitmap(surface.Width, surface.Height);
            surface.DrawToBitmap(bitmap, surface.ClientRectangle);
            bitmap.Save(Path.Combine(directory, name + ".png"));
        }
        async Task CaptureVisible(WF.Control surface, string name)
        {
            surface.Refresh(); surface.Update();
            await Task.Delay(160); // Allow the desktop compositor to present resize/repaint changes.
            if (GetForegroundWindow() != form.Handle) throw new InvalidOperationException("Caption capture lost foreground ownership");
            using var bitmap = new Bitmap(surface.Width, surface.Height);
            using (var graphics = Graphics.FromImage(bitmap)) graphics.CopyFromScreen(surface.PointToScreen(Point.Empty), Point.Empty, bitmap.Size);
            bitmap.Save(Path.Combine(directory, name + $"-{surface.DeviceDpi}dpi.png"));
        }
        void CaptureChrome(WF.Control surface, RibbonSplitButtonItem item, string name)
        {
            var parts = Parts(surface, item);
            var whole = Rectangle.Union(parts.Primary.Bounds, parts.Arrow.Bounds);
            foreach (bool arrow in new[] { false, true })
            {
                var bounds = arrow ? parts.Arrow.Bounds : parts.Primary.Bounds;
                Mouse(surface, 0x200, bounds);
                Save("hover");
                Mouse(surface, 0x201, bounds);
                Save("pressed");
                surface.Capture = false; // Cancel this paint probe without activating.
                void Save(string state)
                {
                    surface.Refresh();
                    using var bitmap = new Bitmap(surface.Width, surface.Height);
                    surface.DrawToBitmap(bitmap, surface.ClientRectangle);
                    using var crop = bitmap.Clone(whole, bitmap.PixelFormat);
                    crop.Save(Path.Combine(directory, $"chrome-{name}-{(arrow ? "arrow" : "primary")}-{state}-{ribbon.DeviceDpi}dpi.png"));
                }
            }
            SendMessage(surface.Handle, 0x2A3, 0, 0);
        }
        void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            results.Add("PASS: " + message);
        }
        void CheckRibbonSurface()
        {
            int S(int value) => (int)Math.Round(value * ribbon.DeviceDpi / 96d);
            int logicalFont = (int)Math.Ceiling(ribbon.Font.Height * 96d / ribbon.DeviceDpi);
            int rowHeight = Math.Max(26, logicalFont + 8), captionHeight = Math.Max(20, logicalFont + 6);
            int previousBodyHeight = Math.Max(104, 3 * rowHeight + captionHeight + 8);
            var large = Rectangle.Union(recording.Parts[split].Primary.Bounds, recording.Parts[split].Arrow.Bounds);
            Check(ribbon.Theme.BodyHeight == 110 && recording.BodyBounds.Top == S(Math.Max(32, logicalFont + 12)) + S(1)
                && recording.BodyBounds.Height == S(previousBodyHeight + 6)
                && large.Top == recording.BodyBounds.Top + S(6)
                && large.Height == S(previousBodyHeight - captionHeight - 8),
                "Shared body grows downward by six logical pixels, retaining tab anchoring and Large command height");
            using var bitmap = new Bitmap(ribbon.Width, ribbon.Height);
            ribbon.DrawToBitmap(bitmap, ribbon.ClientRectangle);
            Check(ribbon.Theme.Background == Color.White && bitmap.GetPixel(ribbon.Width - 1, 0).ToArgb() == Color.White.ToArgb()
                && bitmap.GetPixel(0, ribbon.Height - 1).ToArgb() == Color.White.ToArgb(), "Shared ribbon background and outer margins are white");
            var groupBounds = recording.GroupBounds;
            bool separatorMatches = true;
            for (int y = groupBounds.Top + 2; y < groupBounds.Bottom - 2; y++)
            {
                bool line = !flat && y >= groupBounds.Top + S(12) && y < groupBounds.Bottom - S(12);
                separatorMatches &= bitmap.GetPixel(groupBounds.Right - 1, y).ToArgb() == (line ? ribbon.Theme.Border : ribbon.Theme.Surface).ToArgb();
            }
            Check(separatorMatches, flat ? "Neutral renderer retains its separator-free surface" : "Office separator matches WPF-reference clearance at both ends");
            if (flat) return;
            var tabBounds = recording.TabBounds;
            var ink = Rectangle.Empty;
            for (int y = tabBounds.Bottom - S(6); y < tabBounds.Bottom; y++)
            for (int x = tabBounds.Left; x < tabBounds.Right; x++)
                // The body's neutral shadow extends beneath the tab strip.
                // Blue chroma isolates the indicator from that gray underlay.
                if (bitmap.GetPixel(x, y).B > bitmap.GetPixel(x, y).R)
                    ink = ink.IsEmpty ? new(x, y, 1, 1) : Rectangle.Union(ink, new(x, y, 1, 1));
            int labelWidth = WF.TextRenderer.MeasureText(recording.TabText, ribbon.Font, new(int.MaxValue, int.MaxValue),
                WF.TextFormatFlags.SingleLine | WF.TextFormatFlags.NoPrefix | WF.TextFormatFlags.NoPadding).Width;
            Check(ink.Width == labelWidth + 2 * S(4) && ink.Height == S(3)
                && ink.Left == tabBounds.Left + (tabBounds.Width - ink.Width) / 2,
                "Office active underline follows label width with four logical pixels per side and three-pixel logical thickness");
            Check(bitmap.GetPixel(ink.Left, ink.Top).ToArgb() != ribbon.Theme.Accent.ToArgb(), "Underline ends have antialiased rounded corners");
            bool symmetric = true;
            int Coverage(int x, int y) { var color = bitmap.GetPixel(x, y); return color.B - color.R; }
            for (int y = 0; y < ink.Height; y++) for (int x = 0; x < ink.Width; x++)
                symmetric &= Math.Abs(Coverage(ink.Left + x, ink.Top + y) - Coverage(ink.Right - 1 - x, ink.Top + y)) <= 1
                    && Math.Abs(Coverage(ink.Left + x, ink.Top + y) - Coverage(ink.Left + x, ink.Bottom - 1 - y)) <= 1;
            Check(symmetric, "Both underline caps use mirrored pixel coverage");
        }
        async Task CheckBodyScrollButtons()
        {
            int S(int value) => (int)Math.Round(value * ribbon.DeviceDpi / 96d);
            var body = recording.BodyBounds;
            Check(ribbon.Theme.BodyScrollButtonVerticalInset == 2 && recording.BodyScrolls.Count == 2
                && recording.BodyScrolls.All(pair => pair.Value.Top == body.Top + S(2) && pair.Value.Bottom == body.Bottom - S(2)
                    && pair.Value.Width == S(22) && pair.Value.Top - body.Top == (pair.Key ? pair.Value.Left - body.Left : body.Right - pair.Value.Right)),
                "Both body scroll buttons use uniform two-logical-pixel clearance at the body edges");
            var next = recording.BodyScrolls[false];
            int topGapY = body.Top + (next.Top - body.Top) / 2;
            int bottomGapY = body.Bottom - 1 - (body.Bottom - next.Bottom) / 2;
            SendMessage(ribbon.Handle, 0x2A3, 0, 0);
            CheckChrome(ribbon.Theme.PopupBorder, ribbon.Theme.Surface, "idle");
            await CaptureVisible(ribbon, "scroll-overflow");
            int originalLeft = recording.GroupBounds.Left;
            foreach (int y in new[] { topGapY, bottomGapY })
                Click(ribbon, new(next.Left, y, next.Width, 1));
            ribbon.Refresh();
            Check(recording.GroupBounds.Left == originalLeft, "Body margins above and below the scroller do not scroll content");
            Mouse(ribbon, 0x200, next);
            CheckChrome(ribbon.Theme.Accent, ribbon.Theme.Hover.Start, "hover");
            Capture(ribbon, "scroll-hover");
            Mouse(ribbon, 0x201, next);
            CheckChrome(ribbon.Theme.Accent, ribbon.Theme.Pressed.Start, "pressed");
            Capture(ribbon, "scroll-pressed");
            Mouse(ribbon, 0x202, next); ribbon.Refresh();
            Check(recording.GroupBounds.Left < originalLeft, "Right body scroller moves the fixed group into view");
            Click(ribbon, recording.BodyScrolls[true]); ribbon.Refresh();
            Check(recording.GroupBounds.Left == originalLeft, "Left body scroller returns the fixed group");
            var originalTheme = ribbon.Theme;
            foreach (int inset in new[] { 0, 6 })
            {
                ribbon.Theme = originalTheme with { BodyScrollButtonVerticalInset = inset }; ribbon.Refresh();
                Check(recording.BodyBounds == body && recording.BodyScrolls.Values.All(bounds =>
                    bounds.Top == body.Top + S(inset) && bounds.Bottom == body.Bottom - S(inset) && bounds.Width == next.Width),
                    $"Public theme inset {inset} changes both scrollers without changing the ribbon body or button width");
            }
            ribbon.Theme = originalTheme;

            void CheckChrome(Color border, Color fill, string state)
            {
                ribbon.Refresh();
                using var bitmap = new Bitmap(ribbon.Width, ribbon.Height);
                ribbon.DrawToBitmap(bitmap, ribbon.ClientRectangle);
                Check(bitmap.GetPixel(next.Left + next.Width / 2, next.Top + 1).ToArgb() == border.ToArgb()
                    && bitmap.GetPixel(next.Left + next.Width / 2, next.Top + S(12)).ToArgb() == fill.ToArgb(),
                    $"Body scroll {state} has a visible frame and matching fill");
                Check(bitmap.GetPixel(next.Left + next.Width / 2, topGapY).ToArgb() == ribbon.Theme.Surface.ToArgb()
                    && bitmap.GetPixel(next.Left + next.Width / 2, bottomGapY).ToArgb() == ribbon.Theme.Surface.ToArgb(),
                    $"Body scroll {state} keeps the top and bottom gaps clear");
            }
        }
        void CheckCaption(RibbonItem item)
        {
            var lines = recording.Captions[item];
            string Content(string value) => new(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
            string visible = Content(string.Concat(lines.Select(line => line.Text)));
            bool matches = visible.EndsWith('…') ? Content(item.Text).StartsWith(visible[..^1], StringComparison.Ordinal) : visible == Content(item.Text);
            Check(lines.Count <= 2 && matches
                && lines.All(line => WF.TextRenderer.MeasureText(line.Text, ribbon.Font, new(int.MaxValue, int.MaxValue),
                    WF.TextFormatFlags.SingleLine | WF.TextFormatFlags.NoPrefix | WF.TextFormatFlags.NoPadding).Width <= line.Bounds.Width),
                $"{item.GetType().Name} paints at most two measured rows, using an ellipsis for overflow");
            if (item is RibbonDropDownItem && item is not RibbonSplitButtonItem { Layout: RibbonSplitButtonLayout.Horizontal }
                && (lines.Count > 1 || item is not RibbonSplitButtonItem))
            {
                var arrow = recording.Arrows[item]; var last = lines[^1].Bounds;
                Check(arrow.Left == last.Right && arrow.Top == last.Top && arrow.Height == last.Height,
                    "Wrapped vertical dropdown arrow sits beside the second caption row");
            }
        }
        void CheckFill(WF.Control surface, Rectangle bounds, Color expected, string message)
        {
            using var bitmap = new Bitmap(surface.Width, surface.Height);
            surface.DrawToBitmap(bitmap, surface.ClientRectangle);
            int inset = (int)Math.Round(5 * surface.DeviceDpi / 96.0);
            var actual = bitmap.GetPixel(bounds.Left + inset, bounds.Top + inset);
            Check(actual.ToArgb() == expected.ToArgb(), $"{message} (actual={actual}, expected={expected})");
        }
    }
    private sealed class Recording
    {
        internal readonly Dictionary<RibbonSplitButtonItem, (RibbonPaintContext Primary, RibbonPaintContext Arrow)> Parts = [];
        internal readonly Dictionary<RibbonItem, Rectangle> Items = [];
        internal readonly Dictionary<RibbonItem, List<(string Text, Rectangle Bounds)>> Captions = [];
        internal readonly Dictionary<RibbonItem, Rectangle> Arrows = [];
        internal readonly Dictionary<bool, Rectangle> BodyScrolls = [];
        internal void Scroll(RibbonPaintContext context, bool backward)
        {
            ScrollCalls++;
            if (BodyBounds.Contains(context.Bounds)) BodyScrolls[backward] = context.Bounds;
        }
        internal RibbonItem? CaptionItem;
        internal void BeginCaption(RibbonItem item) { CaptionItem = item; Captions[item] = []; }
        internal void Text(RibbonPaintContext context, string text)
        { if (CaptionItem is { } item) Captions[item].Add((text, context.Bounds)); }
        internal void Arrow(RibbonPaintContext context)
        { if (CaptionItem is { } item) Arrows[item] = context.Bounds; }
        internal Rectangle GroupBounds, BodyBounds, TabBounds;
        internal readonly Dictionary<RibbonGroup, (Rectangle Bounds, bool Collapsed)> GroupFrames = [];
        internal string TabText = "";
        internal bool Collapsed;
        internal int BodyCalls, TextCalls, ArrowCalls, ImageCalls, ScrollCalls;
    }
    private sealed class Recorder(Recording recording) : RibbonOffice2024Renderer
    {
        public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected)
        { if (selected) { recording.TabBounds = context.Bounds; recording.TabText = tab.Text; } base.DrawTab(context, tab, selected); }
        public override void DrawBody(RibbonPaintContext context) { recording.BodyBounds = context.Bounds; recording.BodyCalls++; base.DrawBody(context); }
        public override void DrawScrollButton(RibbonPaintContext context, bool backward) { recording.Scroll(context, backward); base.DrawScrollButton(context, backward); }
        public override void DrawText(RibbonPaintContext context, string text, Color color, WF.TextFormatFlags flags) { recording.Text(context, text); recording.TextCalls++; base.DrawText(context, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext context, Color color) { recording.Arrow(context); recording.ArrowCalls++; base.DrawDropDownArrow(context, color); }
        protected override void DrawItemImage(RibbonPaintContext context, RibbonItem item, RibbonItemSize size) { recording.ImageCalls++; base.DrawItemImage(context, item, size); }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonSplitButtonItem item, RibbonItemSize size, bool menu)
        {
            recording.Parts[item] = (primary, dropDown);
            recording.BeginCaption(item);
            try { base.DrawSplitItem(primary, dropDown, item, size, menu); }
            finally { recording.CaptionItem = null; }
        }
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu)
        {
            recording.Items[item] = context.Bounds;
            recording.BeginCaption(item);
            try { base.DrawItem(context, item, size, menu); }
            finally { recording.CaptionItem = null; }
        }
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
        {
            recording.GroupBounds = context.Bounds; recording.Collapsed = collapsed;
            recording.GroupFrames[group] = (context.Bounds, collapsed);
            base.DrawGroup(context, group, collapsed);
        }
    }
    private sealed class FlatRecorder(Recording recording) : RibbonRenderer
    {
        public override void DrawTab(RibbonPaintContext context, RibbonTab tab, bool selected)
        { if (selected) { recording.TabBounds = context.Bounds; recording.TabText = tab.Text; } base.DrawTab(context, tab, selected); }
        public override void DrawBody(RibbonPaintContext context) { recording.BodyBounds = context.Bounds; recording.BodyCalls++; base.DrawBody(context); }
        public override void DrawScrollButton(RibbonPaintContext context, bool backward) { recording.Scroll(context, backward); base.DrawScrollButton(context, backward); }
        public override void DrawText(RibbonPaintContext context, string text, Color color, WF.TextFormatFlags flags) { recording.Text(context, text); recording.TextCalls++; base.DrawText(context, text, color, flags); }
        public override void DrawDropDownArrow(RibbonPaintContext context, Color color) { recording.Arrow(context); recording.ArrowCalls++; base.DrawDropDownArrow(context, color); }
        protected override void DrawItemImage(RibbonPaintContext context, RibbonItem item, RibbonItemSize size) { recording.ImageCalls++; base.DrawItemImage(context, item, size); }
        public override void DrawSplitItem(RibbonPaintContext primary, RibbonPaintContext dropDown, RibbonSplitButtonItem item, RibbonItemSize size, bool menu)
        {
            recording.Parts[item] = (primary, dropDown);
            recording.BeginCaption(item);
            try { base.DrawSplitItem(primary, dropDown, item, size, menu); }
            finally { recording.CaptionItem = null; }
        }
        public override void DrawItem(RibbonPaintContext context, RibbonItem item, RibbonItemSize size, bool menu)
        {
            recording.Items[item] = context.Bounds;
            recording.BeginCaption(item);
            try { base.DrawItem(context, item, size, menu); }
            finally { recording.CaptionItem = null; }
        }
        public override void DrawGroup(RibbonPaintContext context, RibbonGroup group, bool collapsed)
        {
            recording.GroupBounds = context.Bounds; recording.Collapsed = collapsed;
            base.DrawGroup(context, group, collapsed);
        }
    }
}
