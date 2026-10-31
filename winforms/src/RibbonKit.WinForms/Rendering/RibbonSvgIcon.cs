using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Text;
using RibbonKit.WinForms.Imaging;

namespace RibbonKit.WinForms;

/// <summary>Immutable, self-contained SVG artwork drawn at the requested device size. The assigning application retains ownership.</summary>
[Serializable, TypeConverter(typeof(RibbonSvgIconConverter))]
public sealed class RibbonSvgIcon : IRibbonIcon
{
    private readonly string _source;
    private readonly bool _useForeground;
    [NonSerialized] private SvgAsset? _asset;
    private SvgAsset Asset => _asset ??= new SvgAsset(_source, _useForeground);
    /// <summary>Creates an icon from inline SVG or a stock:key identifier. Foreground tinting is opt-in; original SVG colors are retained by default.</summary>
    public RibbonSvgIcon(string source, bool useForegroundColor = false)
    { _asset = new(source, useForegroundColor); _source = _asset.Source; _useForeground = useForegroundColor; }
    /// <summary>Gets the portable inline SVG or stock identifier; never a file-system path.</summary>
    public string Source => _source;
    /// <summary>Gets whether nonempty fills and strokes use the ribbon's effective foreground, including its disabled color.</summary>
    public bool UseForegroundColor => _useForeground;
    /// <summary>Gets the stock name or SVG title.</summary>
    public string Name => Asset.Name;
    /// <inheritdoc />
    public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
    {
        ArgumentNullException.ThrowIfNull(graphics); Asset.Draw(graphics, bounds, context.Foreground);
    }
}

/// <summary>Describes one bundled Fluent UI stock SVG.</summary>
public sealed class RibbonStockIcon
{
    internal RibbonStockIcon(string key, string name) { Key = key; Name = name; }
    /// <summary>Gets its stable, case-sensitive catalog key.</summary>
    public string Key { get; }
    /// <summary>Gets its display name.</summary>
    public string Name { get; }
}

/// <summary>Provides the curated, attributed Fluent UI regular icon catalog.</summary>
public static class RibbonStockIcons
{
    /// <summary>Gets the immutable catalog metadata.</summary>
    public static IReadOnlyList<RibbonStockIcon> All { get; } = Array.AsReadOnly(StockIcons.All.Select(icon => new RibbonStockIcon(icon.Key, icon.Name)).ToArray());
    /// <summary>Creates a stock icon using the ribbon foreground, including disabled state.</summary>
    public static RibbonSvgIcon Create(string key) { ArgumentNullException.ThrowIfNull(key); StockIcons.Find(key); return new("stock:" + key, true); }
}

/// <summary>Round-trips SVG sources through designer constructors and string-backed .resx resources.</summary>
public sealed class RibbonSvgIconConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(string) || destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string text && text.StartsWith("ribbon-svg-v1:", StringComparison.Ordinal))
        {
            var parts = text.Split(':', 3);
            if (parts.Length == 3 && parts[1] is "0" or "1") return new RibbonSvgIcon(Encoding.UTF8.GetString(Convert.FromBase64String(parts[2])), parts[1] == "1");
        }
        throw new FormatException("Invalid RibbonKit SVG resource.");
    }
    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is RibbonSvgIcon icon)
        {
            if (destinationType == typeof(string)) return "ribbon-svg-v1:" + (icon.UseForegroundColor ? "1:" : "0:") + Convert.ToBase64String(Encoding.UTF8.GetBytes(icon.Source));
            if (destinationType == typeof(InstanceDescriptor)) return new InstanceDescriptor(typeof(RibbonSvgIcon).GetConstructor(new[] { typeof(string), typeof(bool) }), new object[] { icon.Source, icon.UseForegroundColor }, true);
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
