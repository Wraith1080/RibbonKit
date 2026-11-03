using System.ComponentModel;
using System.ComponentModel.Design;

namespace RibbonKit.WinForms.Design;

// Smart-tag access to existing declarations. FileMenu remains the stable owned
// content property; no new runtime flag or extra File component is introduced.
internal sealed class RibbonFileDesignSettings(RibbonControl ribbon, Func<Type, object?> service)
{
    internal bool Visible
    {
        get => ribbon.FileMenu.Visible;
        set => Set(ribbon.FileMenu, nameof(ribbon.FileMenu.Visible), value, nameof(ribbon.FileMenu), value ? "Show File button" : "Hide File button");
    }
    internal string Opens
    {
        get => ribbon.FileMenuMode == RibbonFileMenuMode.Backstage ? "Backstage" : "Application Menu";
        set => Set(ribbon, nameof(ribbon.FileMenuMode), value switch
        {
            "Backstage" => RibbonFileMenuMode.Backstage,
            "Application Menu" or "Command menu" => RibbonFileMenuMode.CommandMenu,
            _ => throw new ArgumentException("Choose Application Menu or Backstage.", nameof(value))
        }, null, "Change File destination");
    }
    private void Set(object owner, string name, object value, string? rootName, string description)
    {
        var property = TypeDescriptor.GetProperties(owner)[name]!; var old = property.GetValue(owner);
        if (Equals(old, value)) return;
        var host = service(typeof(IDesignerHost)) as IDesignerHost ?? throw new InvalidOperationException("A designer session is required to edit File settings.");
        var changes = service(typeof(IComponentChangeService)) as IComponentChangeService ?? throw new InvalidOperationException("The designer change service is unavailable.");
        var root = rootName is null ? null : TypeDescriptor.GetProperties(ribbon)[rootName]!;
        using var transaction = host.CreateTransaction(description);
        try
        {
            if (root is not null) changes.OnComponentChanging(ribbon, root);
            changes.OnComponentChanging(owner, property); property.SetValue(owner, value);
            changes.OnComponentChanged(owner, property, old, value);
            if (root is not null) changes.OnComponentChanged(ribbon, root, null, null);
            transaction.Commit(); ribbon.Invalidate();
        }
        catch { transaction.Cancel(); throw; }
    }
}

internal sealed class RibbonFileModeConverter : StringConverter
{
    public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
    public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;
    public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context) => new(new[] { "Application Menu", "Backstage" });
}
