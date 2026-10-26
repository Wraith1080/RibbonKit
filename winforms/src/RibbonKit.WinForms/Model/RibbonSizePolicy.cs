using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;

namespace RibbonKit.WinForms;

/// <summary>An immutable mapping from group reduction state to command size.</summary>
[TypeConverter(typeof(RibbonSizePolicyConverter))]
public readonly record struct RibbonSizePolicy
{
    /// <summary>Gets the command size for the expanded group state.</summary>
    public RibbonItemSize Large
    {
        get;
    }
    /// <summary>Gets the command size for the intermediate group state.</summary>
    public RibbonItemSize Medium
    {
        get;
    }
    /// <summary>Gets the command size for the smallest group state.</summary>
    public RibbonItemSize Small
    {
        get;
    }
    private RibbonSizePolicy(RibbonItemSize large, RibbonItemSize medium, RibbonItemSize small)
    {
        if (!Enum.IsDefined(large) || !Enum.IsDefined(medium) || !Enum.IsDefined(small))
            throw new ArgumentOutOfRangeException(nameof(large));
        (Large, Medium, Small) = (large, medium, small);
    }
    /// <summary>Creates a policy that retains the same command size in every state.</summary>
    public static RibbonSizePolicy Fixed(RibbonItemSize size) => new(size, size, size);
    /// <summary>Creates a policy with explicit sizes for each group state.</summary>
    public static RibbonSizePolicy Adaptive(RibbonItemSize largeState, RibbonItemSize mediumState, RibbonItemSize smallState) => new(largeState, mediumState, smallState);
    internal RibbonItemSize At(int state) => state == 0 ? Large : state == 1 ? Medium : Small;
    /// <inheritdoc />
    public override string ToString() => $"{Large}, {Medium}, {Small}";
}

/// <summary>Serializes immutable size policies for the WinForms designer.</summary>
public sealed class RibbonSizePolicyConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    /// <inheritdoc />
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) => destinationType == typeof(InstanceDescriptor) || destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s)
        {
            var values = s.Split(',').Select(x => Enum.Parse<RibbonItemSize>(x.Trim(), true)).ToArray();
            return values.Length == 1 ? RibbonSizePolicy.Fixed(values[0]) : values.Length == 3 ? RibbonSizePolicy.Adaptive(values[0], values[1], values[2]) : throw new FormatException("Specify one size or three comma-separated sizes.");
        }
        return base.ConvertFrom(context, culture, value);
    }
    /// <inheritdoc />
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is RibbonSizePolicy policy)
        {
            if (destinationType == typeof(string))
                return policy.ToString();
            if (destinationType == typeof(InstanceDescriptor))
                return new InstanceDescriptor(typeof(RibbonSizePolicy).GetMethod(nameof(RibbonSizePolicy.Adaptive)), new object[] { policy.Large, policy.Medium, policy.Small });
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
