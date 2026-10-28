using System.Drawing.Drawing2D;
using System.Globalization;
using System.Xml.Linq;

namespace RibbonKit.WinForms;

// These two owned SVG assets contain only polylines. Read their shared geometry
// once, then rasterize directly at the control's device size and current palette.
// This is deliberately not a general-purpose SVG loader or a runtime dependency.
internal static class RibbonOptionsTransferIcons
{
    private sealed record Stroke(PointF[] Points, bool Accent);
    private static readonly Stroke[] Import = Load("Import"), Export = Load("Export");
    private static Stroke[] Load(string name)
    {
        using var resource = typeof(RibbonControl).Assembly.GetManifestResourceStream("RibbonKit.WinForms.Assets.Icons." + name + ".svg")!;
        XNamespace svg = "http://www.w3.org/2000/svg";
        return XDocument.Load(resource).Descendants(svg + "polyline").Select(line => new Stroke(
            line.Attribute("points")!.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(point =>
            {
                var pair = point.Split(','); return new PointF(float.Parse(pair[0], CultureInfo.InvariantCulture), float.Parse(pair[1], CultureInfo.InvariantCulture));
            }).ToArray(), line.Ancestors().Any(parent => (string?)parent.Attribute("data-role") == "accent"))).ToArray();
    }
    internal static Bitmap Image(RibbonControl ribbon, int side, bool import)
    {
        var image = new Bitmap(side, side);
        using var graphics = Graphics.FromImage(image); graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.ScaleTransform(side / 24f, side / 24f);
        foreach (var stroke in import ? Import : Export)
        {
            using var pen = new Pen(stroke.Accent ? ribbon.Theme.Accent : ribbon.Theme.SecondaryText, 1.75f)
            { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
            graphics.DrawLines(pen, stroke.Points);
        }
        return image;
    }
}
