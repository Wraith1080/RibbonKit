using System.Drawing.Drawing2D;

namespace RibbonKit.WinForms;

// Nine slices keep the cached blur independent of the ribbon's changing width.
internal sealed class ShadowPainter : IDisposable
{
    private readonly Dictionary<(int Radius, int Blur, Color Color), Bitmap> _cache = [];
    internal void Draw(Graphics graphics, Rectangle bounds, int radius, int blur, Color color)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || color.A == 0 || blur <= 0) return;
        radius = Math.Clamp(radius, 0, (Math.Min(bounds.Width, bounds.Height) - 1) / 2);
        var key = (radius, blur, color);
        if (!_cache.TryGetValue(key, out var mask))
        {
            if (_cache.Count >= 16) Dispose();
            _cache[key] = mask = Create(radius, blur, color);
        }
        int edge = radius + blur;
        var outer = Rectangle.Inflate(bounds, blur, blur);
        int[] xs = [outer.Left, outer.Left + edge, outer.Right - edge, outer.Right];
        int[] ys = [outer.Top, outer.Top + edge, outer.Bottom - edge, outer.Bottom];
        int[] ss = [0, edge, edge + 1, mask.Width];
        var state = graphics.Save();
        try
        {
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    graphics.DrawImage(mask, Rectangle.FromLTRB(xs[x], ys[y], xs[x + 1], ys[y + 1]),
                        ss[x], ss[y], ss[x + 1] - ss[x], ss[y + 1] - ss[y], GraphicsUnit.Pixel);
        }
        finally { graphics.Restore(state); }
    }
    private static Bitmap Create(int radius, int blur, Color color)
    {
        int edge = radius + blur, size = 2 * edge + 1;
        var bitmap = new Bitmap(size, size);
        double sigma = Math.Max(0.5, blur / 2.5), tail = Math.Exp(-0.5 * blur * blur / (sigma * sigma));
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                double qx = Math.Abs(x - edge) - 0.5, qy = Math.Abs(y - edge) - 0.5;
                double distance = Math.Max(0, Math.Sqrt(Math.Pow(Math.Max(qx, 0), 2) + Math.Pow(Math.Max(qy, 0), 2)) + Math.Min(Math.Max(qx, qy), 0) - radius);
                double coverage = Math.Clamp((Math.Exp(-0.5 * distance * distance / (sigma * sigma)) - tail) / (1 - tail), 0, 1);
                bitmap.SetPixel(x, y, Color.FromArgb((int)Math.Round(color.A * coverage), color));
            }
        return bitmap;
    }
    public void Dispose()
    {
        foreach (var bitmap in _cache.Values) bitmap.Dispose();
        _cache.Clear();
    }
}
