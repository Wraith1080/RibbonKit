using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace RibbonKitDesignerConsumer;

internal static class SampleIsolationCheck
{
    internal static void Verify(string output, string sampleAssembly)
    {
        Directory.CreateDirectory(output);
        using var stream = File.OpenRead(sampleAssembly); using var image = new PEReader(stream);
        var metadata = image.GetMetadataReader();
        var types = metadata.TypeDefinitions.Select(h => metadata.GetString(metadata.GetTypeDefinition(h).Name)).ToArray();
        var methods = metadata.MethodDefinitions.Select(h => metadata.GetString(metadata.GetMethodDefinition(h).Name)).ToArray();
        var references = metadata.AssemblyReferences.Select(h => metadata.GetString(metadata.GetAssemblyReference(h).Name)).ToArray();
        var resources = metadata.ManifestResources.Select(h => metadata.GetString(metadata.GetManifestResource(h).Name)).ToArray();
        string[] required = ["DesignerForm", "BackstageInfoControl", "BackstagePrintControl", "PopupOptionsControl", "GalleryStylesControl", "GalleryStylesStripControl", "GalleryStyleTileControl"];
        Require(required.All(types.Contains), "Normal sample retains all authored Form/UserControl types");
        Require(!types.Any(n => n.EndsWith("Check", StringComparison.Ordinal) || n.EndsWith("Checks", StringComparison.Ordinal) || n.EndsWith("Demo", StringComparison.Ordinal) || n.EndsWith("Profile", StringComparison.Ordinal) || n.EndsWith("Trace", StringComparison.Ordinal) || n.EndsWith("GeometrySettings", StringComparison.Ordinal) || n.StartsWith("GeneratedContent", StringComparison.Ordinal)), "Normal sample contains no verification, retired demo or test-fixture types");
        Require(!methods.Any(n => n.StartsWith("Verify", StringComparison.Ordinal) || n.StartsWith("EnableOffice2019ThemeDemo", StringComparison.Ordinal)), "Normal sample contains no verification methods");
        Require(!references.Any(n => n.Contains("Tests", StringComparison.Ordinal) || n.Contains("Consumer", StringComparison.Ordinal)), "Normal sample does not reference its test host");
        Require(resources.Contains("RibbonKitDesignerConsumer.DesignerForm.resources") && resources.Contains("RibbonKitDesignerConsumer.GalleryStylesControl.resources"), "Authored designer resources retain their manifest names");
        Require(typeof(DesignerForm).GetMethod("VerifyThemeGallery", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) is not null, "Verification partials are available in the separate host");
        File.WriteAllText(Path.Combine(output, "isolation.json"), JsonSerializer.Serialize(new { Assembly = Path.GetFullPath(sampleAssembly), Bytes = stream.Length, TypeCount = types.Length, Resources = resources, TestMethodsInSample = methods.Count(n => n.StartsWith("Verify", StringComparison.Ordinal)) }));
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: compiled normal sample retains authored controls/resources and contains no test methods, checks, profiles, retired demo/fixture types or test-host reference; verification partials live in the separate host.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
