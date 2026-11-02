using System.CodeDom.Compiler;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

// Names stay separate from command IDs and draft keys. Apply renames last so
// source/owner maps keep their original keys throughout the structural commit.
internal sealed class RibbonComponentNames
{
    private readonly IDesignerHost _host;
    private readonly List<(IComponent Component, string Name)> _renames = [];
    internal RibbonComponentNames(IDesignerHost host, IEnumerable<(IComponent? Component, string Name)> names)
    {
        _host = host;
        var service = host.GetService(typeof(INameCreationService)) as INameCreationService;
        var language = host.GetService(typeof(CodeDomProvider)) as CodeDomProvider;
        using var fallback = language is null ? CodeDomProvider.CreateProvider("CSharp") : null;
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (component, name) in names)
        {
            if (string.IsNullOrWhiteSpace(name) || !used.Add(name)) throw new ArgumentException("Each component needs a nonempty, unique (Name).");
            var occupied = host.Container.Components[name];
            if (occupied is not null && !ReferenceEquals(occupied, component))
                throw new ArgumentException("The component name '" + name + "' is already in use.");
            if (component is not null && component.Site?.Name == name) continue;
            // VS's name service can check availability as well as syntax, but
            // it has no component argument with which to exclude the owner.
            // Unchanged names need no revalidation. A case-only rename already
            // owns its container slot, so validate its identifier separately.
            bool owned = component is not null && ReferenceEquals(occupied, component);
            if (service is not null && !owned) service.ValidateName(name);
            else if (!(language ?? fallback!).IsValidIdentifier(name)) throw new ArgumentException("Choose a valid programmatic component name: " + name);
            if (component is not null) _renames.Add((component, name));
        }
    }
    internal void Apply(IComponentChangeService changes)
    {
        foreach (var (component, name) in _renames)
        {
            if (component.Site is null) _host.Container.Add(component, name);
            else component.Site.Name = name; // The designer host publishes ComponentRename for code generation and Undo.
            if (component is Control control && control.Name != name)
            {
                var property = TypeDescriptor.GetProperties(control)[nameof(Control.Name)]!;
                string old = control.Name;
                changes.OnComponentChanging(control, property); control.Name = name;
                changes.OnComponentChanged(control, property, old, name);
            }
            TypeDescriptor.Refresh(component);
        }
    }
    internal static string ContentState(RibbonDesignSnapshot snapshot)
    {
        var copy = RibbonDesignSnapshot.Deserialize(snapshot.Serialize());
        copy.ComponentName = "";
        foreach (var node in copy.AllNodes) node.ComponentName = "";
        return copy.Serialize();
    }
    internal static bool SameEntries(IEnumerable<RibbonDesignNode> before, IEnumerable<RibbonDesignNode> after) =>
        ContentState(new() { Tabs = before.ToList() }) == ContentState(new() { Tabs = after.ToList() });
}
