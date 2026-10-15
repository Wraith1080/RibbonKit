using System.Collections;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Xunit;

namespace RibbonKit.Tests;

public sealed class DesignerToolboxInitializationTests
{
    [Fact]
    public void Tab_defaults_skip_generic_content_and_ribbon_placement_does_not_accept_geometry()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string project = Path.Combine(directory.FullName, "src", "RibbonKit.Design");

        // Read the existing restore graph rather than adding SDK dependencies to
        // runtime/tests or copying Visual Studio assemblies into runtime output.
        using var assets = JsonDocument.Parse(File.ReadAllText(Path.Combine(project, "obj", "project.assets.json")));
        var library = Assert.Single(assets.RootElement.GetProperty("libraries").EnumerateObject(),
            entry => entry.Name.StartsWith("Microsoft.VisualStudio.DesignTools.Extensibility/", StringComparison.Ordinal));
        string packagePath = library.Value.GetProperty("path").GetString()!;
        string sdk = assets.RootElement.GetProperty("packageFolders").EnumerateObject()
            .Select(folder => Path.Combine(folder.Name, packagePath, "lib", "net45"))
            .First(path => File.Exists(Path.Combine(path, "Microsoft.VisualStudio.DesignTools.Interaction.dll")));
        Assembly.LoadFrom(Path.Combine(sdk, "Microsoft.VisualStudio.DesignTools.Extensibility.dll"));
        var interaction = Assembly.LoadFrom(Path.Combine(sdk, "Microsoft.VisualStudio.DesignTools.Interaction.dll"));
        var defaultInitializer = interaction.GetType("Microsoft.VisualStudio.DesignTools.Extensibility.Model.DefaultInitializer", throwOnError: true)!;

        string configuration = typeof(DesignerToolboxInitializationTests).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()!.Configuration;
        var design = Assembly.LoadFrom(Path.Combine(project, "bin", configuration, "net472", "RibbonKit.DesignTools.dll"));
        var metadata = Activator.CreateInstance(design.GetType("RibbonKit.Design.Metadata", throwOnError: true)!, nonPublic: true)!;
        var table = metadata.GetType().GetProperty("AttributeTable")!.GetValue(metadata)!;
        var attributes = table.GetType().GetMethod("GetCustomAttributes", new[] { typeof(string) })!;
        var skip = defaultInitializer.GetProperty("SkipDesignerInitialization")!;

        var tabInitializer = Initializer("RibbonTab");
        Assert.Equal("RibbonTabDefaultInitializer", tabInitializer.GetType().Name);
        Assert.True((bool)skip.GetValue(tabInitializer)!);
        // The fix is specific to RibbonTab. Page Content and other control
        // defaults keep their existing designer behavior.
        foreach (string type in new[] { "Ribbon", "RibbonGroup", "BackstageTabItem", "RibbonButton" })
            Assert.False((bool)skip.GetValue(Initializer(type))!);

        // Placement adapters describe a parent's child layout. Register this on
        // Ribbon, where tabs are inserted, rather than on each tab's group area.
        var placementType = interaction.GetType("Microsoft.VisualStudio.DesignTools.Extensibility.Interaction.PlacementAdapter", throwOnError: true)!;
        var ribbonPlacement = Activator.CreateInstance(Assert.Single(Providers("Ribbon", placementType)))!;
        Assert.Equal("RibbonTabsPlacementAdapter", ribbonPlacement.GetType().Name);
        Assert.Empty(Providers("RibbonTab", placementType));
        var intentType = interaction.GetType("Microsoft.VisualStudio.DesignTools.Extensibility.Interaction.PlacementIntent", throwOnError: true)!;
        var positionsType = interaction.GetType("Microsoft.VisualStudio.DesignTools.Extensibility.Interaction.RelativePositions", throwOnError: true)!;
        var canSet = placementType.GetMethod("CanSetPosition")!;
        foreach (string intent in new[] { "Move", "Size" })
        foreach (string edge in new[] { "LeftSide", "RightSide", "TopSide", "BottomSide" })
        {
            object position = positionsType.GetProperty(edge)!.GetValue(null)!;
            Assert.False((bool)canSet.Invoke(ribbonPlacement, new[] { Enum.Parse(intentType, intent), position })!);
        }
        var tabSize = Assert.Single(Attributes("RibbonTab"),
            attribute => attribute.GetType().FullName == "Microsoft.VisualStudio.DesignTools.Extensibility.DefaultSizeAttribute");
        Assert.Equal("None", tabSize.GetType().GetProperty("Mode")!.GetValue(tabSize)!.ToString());

        object Initializer(string type)
        {
            return Activator.CreateInstance(Assert.Single(Providers(type, defaultInitializer)))!;
        }

        IEnumerable<Type> Providers(string type, Type providerBase) => Attributes(type)
                .Select(attribute => attribute.GetType().GetProperty("FeatureProviderType")?.GetValue(attribute) as Type)
                .OfType<Type>().Where(providerBase.IsAssignableFrom);

        IEnumerable<object> Attributes(string type) =>
            ((IEnumerable)attributes.Invoke(table, new object[] { "RibbonKit.Controls." + type })!).Cast<object>();
    }
}
