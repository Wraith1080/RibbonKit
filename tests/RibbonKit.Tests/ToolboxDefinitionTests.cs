using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed class ToolboxDefinitionTests
{
    [Fact]
    public void Manifest_exposes_the_reviewed_constructible_authoring_surface()
    {
        var document = XDocument.Load(Path.Combine(Root(), "src", "RibbonKit", "tools", "VisualStudioToolsManifest.xml"));
        var file = Assert.Single(document.Root!.Elements("File"));
        Assert.Equal("RibbonKit.dll", (string?)file.Attribute("Reference"));
        var items = Assert.Single(file.Elements("ToolboxItems"));
        Assert.Equal("RibbonKit", (string?)items.Attribute("VSCategory"));
        Assert.Equal("RibbonKit", (string?)items.Attribute("BlendCategory"));
        Assert.Equal("WPF", (string?)items.Attribute("UIFramework"));
        string[] names = ManifestTypes();
        Assert.Equal(31, names.Length);
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());

        string plan = File.ReadAllText(Path.Combine(Root(), "docs", "17-TOOLBOX-ITEM-DEFINITION-PLAN.md"));
        var approved = Regex.Matches(plan, @"^\| `(?<type>\w+)` \| (Keep|Add) \|", RegexOptions.Multiline)
            .Select(match => "RibbonKit.Controls." + match.Groups["type"].Value);
        Assert.Equal(approved.OrderBy(name => name), names.OrderBy(name => name));
        foreach (string name in names)
        {
            var type = typeof(Ribbon).Assembly.GetType(name, throwOnError: true)!;
            Assert.True(type.IsPublic && !type.IsAbstract && typeof(FrameworkElement).IsAssignableFrom(type), name);
            Assert.False(typeof(Window).IsAssignableFrom(type), name);
            Assert.NotNull(type.GetConstructor(Type.EmptyTypes));
        }
    }

    [Fact]
    public void Designer_visibility_matches_the_manifest_and_classifies_all_public_visual_types()
    {
        string metadata = File.ReadAllText(Path.Combine(Root(), "src", "RibbonKit.Design", "ToolboxMetadata.cs"));
        string[] included = ReadTypes("IncludedTypes");
        string[] excluded = ReadTypes("ExcludedTypes");
        Assert.Equal(ManifestTypes().OrderBy(name => name), included.OrderBy(name => name));
        Assert.Equal(included.Length, included.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(excluded.Length, excluded.Distinct(StringComparer.Ordinal).Count());
        Assert.Empty(included.Intersect(excluded, StringComparer.Ordinal));
        var publicVisuals = typeof(Ribbon).Assembly.GetExportedTypes()
            .Where(type => typeof(FrameworkElement).IsAssignableFrom(type)).Select(type => type.FullName!);
        Assert.Equal(publicVisuals.OrderBy(name => name), included.Concat(excluded).OrderBy(name => name));
        Assert.Contains("ToolboxMetadata.Register(builder)", File.ReadAllText(Path.Combine(Root(), "src", "RibbonKit.Design", "Metadata.cs")));

        string[] ReadTypes(string field)
        {
            var block = Regex.Match(metadata, field + @"\s*=\s*\{(?<body>.*?)\};", RegexOptions.Singleline);
            Assert.True(block.Success, field);
            return Regex.Matches(block.Groups["body"].Value, "\"(?<type>RibbonKit\\.[^\"]+)\"")
                .Select(match => match.Groups["type"].Value).ToArray();
        }
    }

    private static string[] ManifestTypes() => XDocument.Load(Path.Combine(Root(), "src", "RibbonKit", "tools", "VisualStudioToolsManifest.xml"))
        .Descendants("Item").Select(item => (string)item.Attribute("Type")!).ToArray();

    private static string Root()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("RibbonKit.sln was not found.");
    }
}
