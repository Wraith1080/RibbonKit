using System.IO;
using System.Xml.Linq;
using RibbonKit.Controls;
using RibbonKit.Writer;
using Xunit;

namespace RibbonKit.Writer.Tests;

public sealed class WriterScaffoldTests
{
    [Fact]
    public void MainWindowDerivesFromRibbonWindow()
    {
        Assert.True(typeof(RibbonWindow).IsAssignableFrom(typeof(MainWindow)));
    }

    [Fact]
    public void ApplicationManifestEnablesThemedNativeDialogsAndKeepsPerMonitorV2()
    {
        var root = FindSolutionRoot();
        var manifestPath = Path.Combine(root.FullName, "samples", "RibbonKit.Writer", "app.manifest");
        var projectPath = Path.Combine(root.FullName, "samples", "RibbonKit.Writer", "RibbonKit.Writer.csproj");
        var manifest = XDocument.Load(manifestPath);
        XNamespace assembly = "urn:schemas-microsoft-com:asm.v1";
        XNamespace settings = "http://schemas.microsoft.com/SMI/2016/WindowsSettings";

        var commonControls = Assert.Single(manifest.Descendants(assembly + "assemblyIdentity"),
            element => (string?)element.Attribute("name") == "Microsoft.Windows.Common-Controls");
        Assert.Equal("win32", (string?)commonControls.Attribute("type"));
        Assert.Equal("6.0.0.0", (string?)commonControls.Attribute("version"));
        Assert.Equal("*", (string?)commonControls.Attribute("processorArchitecture"));
        Assert.Equal("6595b64144ccf1df", (string?)commonControls.Attribute("publicKeyToken"));
        Assert.Equal("*", (string?)commonControls.Attribute("language"));
        Assert.Equal("PerMonitorV2", Assert.Single(manifest.Descendants(settings + "dpiAwareness")).Value);

        var project = XDocument.Load(projectPath);
        Assert.Equal("app.manifest", Assert.Single(project.Descendants("ApplicationManifest")).Value);
    }

    private static DirectoryInfo FindSolutionRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
            directory = directory.Parent;
        return Assert.IsType<DirectoryInfo>(directory);
    }
}
