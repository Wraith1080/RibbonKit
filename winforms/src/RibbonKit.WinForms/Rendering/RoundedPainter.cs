using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RibbonKit.WinForms;

// One analytic rounded rectangle. Coverage is sampled in one quadrant and reflected,
// avoiding independent arc endpoints/rasterization for the four equal-radius corners.
// Pixels are cached at the actual device DPI; this is not a claim about GDI's helpers.
internal sealed class RoundedPainter : IDisposable
{
    private readonly Dictionary<Key, Bitmap> _cache = [];
    private readonly Queue<Key> _order = [];
    private readonly record struct Key(int Width, int Height, int Radius, int Stroke, RibbonFill Fill, Color Border);
    internal void Draw(Graphics graphics, Rectangle bounds, int radius, RibbonFill fill, Color border, int stroke = 1)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;
        radius = Math.Clamp(radius, 0, Math.Min(bounds.Width, bounds.Height) / 2);
        stroke = Math.Clamp(stroke, 0, Math.Min(bounds.Width, bounds.Height) / 2);
        var key = new Key(bounds.Width, bounds.Height, radius, stroke, fill, border);
        if (!_cache.TryGetValue(key, out var bitmap))
        {
            bitmap = Create(key);
            if (_cache.Count >= 96)
            {
                var old = _order.Dequeue();
                _cache[old].Dispose();
                _cache.Remove(old);
            }
            _cache.Add(key, bitmap);
            _order.Enqueue(key);
        }
        graphics.DrawImageUnscaled(bitmap, bounds.Location);
    }
    private static Bitmap Create(Key key)
    {
        var bitmap = new Bitmap(key.Width, key.Height, PixelFormat.Format32bppPArgb);
        var bytes = new byte[key.Width * key.Height * 4];
        for (int y = 0; y < key.Height; y++)
        {
            double t = key.Height == 1 ? 0 : (double)y / (key.Height - 1);
            var fill = Color.FromArgb(Lerp(key.Fill.Start.A, key.Fill.End.A, t), Lerp(key.Fill.Start.R, key.Fill.End.R, t), Lerp(key.Fill.Start.G, key.Fill.End.G, t), Lerp(key.Fill.Start.B, key.Fill.End.B, t));
            for (int x = 0; x < key.Width; x++)
            {
                double outer = Coverage(x, y, key.Width, key.Height, key.Radius);
                double inner = key.Stroke == 0 ? outer : Coverage(x - key.Stroke, y - key.Stroke, key.Width - 2 * key.Stroke, key.Height - 2 * key.Stroke, Math.Max(0, key.Radius - key.Stroke));
                double f = inner * fill.A / 255d, b = (outer - inner) * key.Border.A / 255d;
                int offset = (y * key.Width + x) * 4;
                bytes[offset] = (byte)Math.Clamp((int)Math.Round(fill.B * f + key.Border.B * b), 0, 255);
                bytes[offset + 1] = (byte)Math.Clamp((int)Math.Round(fill.G * f + key.Border.G * b), 0, 255);
                bytes[offset + 2] = (byte)Math.Clamp((int)Math.Round(fill.R * f + key.Border.R * b), 0, 255);
                bytes[offset + 3] = (byte)Math.Clamp((int)Math.Round(255 * (f + b)), 0, 255);
            }
        }
        var data = bitmap.LockBits(new Rectangle(0, 0, key.Width, key.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
        try
        {
            for (int y = 0; y < key.Height; y++)
                Marshal.Copy(bytes, y * key.Width * 4, data.Scan0 + y * data.Stride, key.Width * 4);
        }
        finally { bitmap.UnlockBits(data); }
        return bitmap;
    }
    private static int Lerp(int start, int end, double t) => (int)Math.Round(start + (end - start) * t);
    private static double Coverage(int x, int y, int width, int height, int radius)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
            return 0;
        x = Math.Min(x, width - 1 - x);
        y = Math.Min(y, height - 1 - y);
        if (x >= radius || y >= radius)
            return 1;
        int inside = 0;
        const int samples = 4;
        for (int sy = 0; sy < samples; sy++)
            for (int sx = 0; sx < samples; sx++)
            {
                double dx = radius - (x + (sx + .5) / samples), dy = radius - (y + (sy + .5) / samples);
                if (dx * dx + dy * dy <= radius * radius)
                    inside++;
            }
        return (double)inside / (samples * samples);
    }
    public void Dispose()
    {
        foreach (var bitmap in _cache.Values)
            bitmap.Dispose();
        _cache.Clear();
        _order.Clear();
    }
}
