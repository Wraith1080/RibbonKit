using System.Drawing.Drawing2D;
using System.Globalization;
using System.Xml.Linq;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private readonly List<string> _iconAudit = [];
    internal void CaptureCommandIcons(string directory)
    {
        Directory.CreateDirectory(directory); File.WriteAllLines(Path.Combine(directory, "coverage.txt"), _iconAudit.Order());
        const string prefix = "RibbonKit.WinForms.Showcase.Assets.CommandIcons.";
        var names = typeof(MainForm).Assembly.GetManifestResourceNames().Where(n => n.StartsWith(prefix, StringComparison.Ordinal) && n.EndsWith(".svg", StringComparison.Ordinal)).Select(n => n[prefix.Length..^4]).Order().ToArray();
        using var sheet = new Bitmap(1000, ((names.Length + 9) / 10) * 92); using var graphics = Graphics.FromImage(sheet); graphics.Clear(ribbon.Theme.Surface);
        using var font = new Font("Segoe UI", 8); using var format = new StringFormat { Alignment = StringAlignment.Center };
        for (int i = 0; i < names.Length; i++)
        {
            int x = i % 10 * 100, y = i / 10 * 92; new CommandIcon(names[i], ribbon).Draw(graphics, new(x + 26, y + 6, 48, 48), new(DeviceDpi, ribbon.Theme.Foreground, true));
            graphics.DrawString(names[i], font, Brushes.Black, new RectangleF(x + 1, y + 58, 98, 32), format);
        }
        sheet.Save(Path.Combine(directory, "icon-sheet.png"));
        foreach (var tab in ribbon.Tabs.Where(t => !t.IsContextual && !t.IsModal)) { ribbon.SelectedTab = tab; Capture(tab.Id); }
        ribbon.SelectedTab = home; Width = 440; Capture("collapsed-groups");
        void Capture(string name) { PerformLayout(); ribbon.Refresh(); using var image = new Bitmap(ClientSize.Width, ClientSize.Height); DrawToBitmap(image, ClientRectangle); image.Save(Path.Combine(directory, name + ".png")); }
    }
    private void InitializeCommandIcons()
    {
        var seen = new HashSet<RibbonItem>();
        void Item(RibbonItem item)
        {
            if (!seen.Add(item) || item is RibbonMenuSeparatorItem or RibbonGalleryTile) return;
            if (item.Icon is not DensityModeIcon)
            {
                string asset = IconName(item); item.Icon = new CommandIcon(asset, ribbon); _iconAudit.Add(asset + "\t" + item.Text);
            }
            else _iconAudit.Add("density-mode\t" + item.Text);
            if (item is RibbonDropDownItem menu) foreach (var child in menu.Items) Item(child);
        }
        void Group(RibbonGroup group)
        {
            string asset = group.Text switch
            {
                "Clipboard" => "group-clipboard", "Font" => "group-font", "Editing" => "group-editing",
                "Illustrations" => "group-illustrations", "Links" => "group-links", "Native editors" => "group-fields",
                "Input states" => "group-input-states", "Editing actions" => "group-input-actions", "Show" => "group-show",
                "Document view" => "group-view", "More options" => "group-more", "Picture" => "group-picture",
                "Galleries" => "group-galleries", "Application context" => "group-context", "Modal preview" => "group-preview",
                "Input mode" => "group-input-mode", "Messages" => "group-messages", "QAT drafts" => "group-customization",
                "Chart data" => "group-chart-data", "Chart context" => "group-chart-context", "Preview settings" => "group-preview-settings",
                _ => throw new InvalidOperationException("Add an explicit group icon for " + group.Text)
            };
            group.Icon = new CommandIcon(asset, ribbon); _iconAudit.Add(asset + "\t[group] " + group.Text);
            foreach (var item in group.Items) Item(item);
        }
        foreach (var tab in ribbon.Tabs) foreach (var group in tab.Groups) Group(group);
        foreach (var source in components!.Components.OfType<RibbonMergeSource>())
        {
            foreach (var tab in source.Tabs) foreach (var group in tab.Groups) Group(group);
            foreach (var contribution in source.Groups) Group(contribution.Group);
        }
        foreach (var item in ribbon.FileMenu.Items.Concat(ribbon.Backstage.Items).Concat(ribbon.CommandCatalog.Commands)) Item(item);
    }
    private static string IconName(RibbonItem item)
    {
        if (item is RibbonTextBoxItem) return item.Text switch { "Name" => "name", "Read-only" => "read-only", "Unavailable" => "disabled-input", "Popup value" => "popup-value", "Copies" => "copies", "Series" => "series", _ => throw new InvalidOperationException("Missing field icon: " + item.Text) };
        if (item is RibbonComboBoxItem) return item.Text switch { "Font" => "font", "Zoom" => "zoom", "Unavailable" => "disabled-choice", "Choice" => "choice", "Type" => "chart-type", _ => throw new InvalidOperationException("Missing choice icon: " + item.Text) };
        return item.Text switch
        {
            "Copy" => "copy", "Paste" => "paste", "Cut" => "cut", "Bold" => "bold", "Italic" => "italic",
            "Find matching text throughout this document" => "find", "Shapes" => "shapes", "Picture" => "picture", "Hyperlink" => "link",
            "Track changes" => "external-edit", "Tracking options" => "tracking-options", "Ruler" or "Ruler (shared binding)" => "ruler",
            "Gridlines" => "grid", "Print layout" => "print-layout", "Web layout" => "web-layout", "Draft" => "draft",
            "Unavailable" => "disabled", "Unavailable option" => "disabled-option", "Select" => "select", "Select all" => "select-all",
            "Clear selection" => "clear-selection", "Rectangle" => "rectangle", "Ellipse" => "ellipse", "Crop" => "crop", "Close picture tools" => "close-picture",
            "New" => "new", "Open" => "open", "Save" => "save", "Recent" => "recent", "Example document" => "document", "Info" => "info", "Print" or "Print request" => "print",
            "Styles" => "styles", "Text color" => "color", "Reset name" => "reset-name", "Edit in popup" => "input-options",
            "Options menu" => "options-menu", "Page color" => "page-color", "White" => "white-page", "Sepia" => "sepia-page",
            "Paste text only" => "paste-text", "Paste special" => "paste-special", "Chart context" => "chart", "Select Chart Design" => "chart-design",
            "Remove chart context" => "chart-remove", "Update chart" => "chart-update", "Chart data" => "chart-data", "Print preview" => "preview",
            "Add message" => "message-add", "Reopen messages" => "message-reopen", "Clear messages" => "message-clear",
            "Move Quick Access Toolbar" => "qat-move", "Customize QAT" => "qat-customize", "Customize Ribbon" => "ribbon-customize",
            "Apply" => "apply", "Cancel change" => "cancel", "Reset preview" => "reset", "Save preview" => "save-preview",
            "Restore preview" => "restore-preview", "Preview change" => "preview-change", "Redo" => "redo",
            _ => throw new InvalidOperationException("Add an explicit command icon for " + item.Text)
        };
    }
    // Application-owned artwork, consumed solely through the public IRibbonIcon
    // contract. The original SVG files are also reusable outside WinForms.
    private sealed class CommandIcon : IRibbonIcon
    {
        private static readonly Dictionary<string, XElement[]> Assets = [];
        private readonly XElement[] _shapes;
        private readonly RibbonControl _ribbon;
        internal CommandIcon(string name, RibbonControl ribbon)
        {
            _ribbon = ribbon;
            if (!Assets.TryGetValue(name, out var shapes))
            {
                using var resource = typeof(MainForm).Assembly.GetManifestResourceStream("RibbonKit.WinForms.Showcase.Assets.CommandIcons." + name + ".svg")
                    ?? throw new InvalidOperationException("Missing SVG asset: " + name);
                shapes = XDocument.Load(resource).Root!.Elements().Where(e => e.Name.LocalName != "title").ToArray(); Assets.Add(name, shapes);
            }
            _shapes = shapes;
        }
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
        {
            var saved = graphics.Save();
            try
            {
                float side = Math.Min(bounds.Width, bounds.Height); graphics.TranslateTransform(bounds.Left + (bounds.Width - side) / 2, bounds.Top + (bounds.Height - side) / 2); graphics.ScaleTransform(side / 24, side / 24);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                foreach (var shape in _shapes)
                {
                    float N(string name, float fallback = 0) => float.TryParse((string?)shape.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : fallback;
                    var color = !context.Enabled ? context.Foreground : (string?)shape.Attribute("data-accent") == "true" ? _ribbon.Theme.Accent : _ribbon.Theme.SecondaryText;
                    using var pen = new Pen(color, 1.65f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round }; using var brush = new SolidBrush(color);
                    using var path = new GraphicsPath();
                    switch (shape.Name.LocalName)
                    {
                        case "polyline": case "polygon":
                            var points = shape.Attribute("points")!.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => { var xy = p.Split(','); return new PointF(float.Parse(xy[0], CultureInfo.InvariantCulture), float.Parse(xy[1], CultureInfo.InvariantCulture)); }).ToArray();
                            path.AddLines(points); if (shape.Name.LocalName == "polygon") path.CloseFigure(); break;
                        case "line": path.AddLine(N("x1"), N("y1"), N("x2"), N("y2")); break;
                        case "rect":
                            float x = N("x"), y = N("y"), w = N("width"), h = N("height"), diameter = Math.Min(Math.Min(w, h), N("rx") * 2);
                            if (diameter <= 0) path.AddRectangle(new RectangleF(x, y, w, h));
                            else { path.AddArc(x, y, diameter, diameter, 180, 90); path.AddArc(x + w - diameter, y, diameter, diameter, 270, 90); path.AddArc(x + w - diameter, y + h - diameter, diameter, diameter, 0, 90); path.AddArc(x, y + h - diameter, diameter, diameter, 90, 90); path.CloseFigure(); } break;
                        case "circle": path.AddEllipse(N("cx") - N("r"), N("cy") - N("r"), N("r") * 2, N("r") * 2); break;
                        case "ellipse": path.AddEllipse(N("cx") - N("rx"), N("cy") - N("ry"), N("rx") * 2, N("ry") * 2); break;
                        case "text":
                            var style = (string?)shape.Attribute("font-weight") == "700" ? FontStyle.Bold : (string?)shape.Attribute("font-style") == "italic" ? FontStyle.Italic : FontStyle.Regular;
                            using (var font = new Font("Segoe UI", N("font-size", 12), style, GraphicsUnit.Pixel))
                            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center }) graphics.DrawString(shape.Value, font, brush, new RectangleF(1, 1, 22, 22), format);
                            continue;
                        default: throw new InvalidOperationException("Unsupported owned icon primitive.");
                    }
                    if (shape.Attribute("fill") is not null) graphics.FillPath(brush, path);
                    if ((string?)shape.Attribute("stroke") != "none") graphics.DrawPath(pen, path);
                }
            }
            finally { graphics.Restore(saved); }
        }
    }
}
