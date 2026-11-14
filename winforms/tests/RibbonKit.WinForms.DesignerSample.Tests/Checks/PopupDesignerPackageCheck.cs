using System.Collections;
using System.ComponentModel.Design;
using System.Reflection;
using System.Runtime.Loader;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Load the actual packaged designer server's SDK-free rendering endpoint.
// Reflection here probes internal designer packaging; it is not a runtime public
// API example or a substitute for Visual Studio's out-of-process designer.
internal static class PopupDesignerPackageCheck
{
    internal static void Verify(string package, string output)
    {
        Directory.CreateDirectory(output);
        using var surface = new DesignSurface(typeof(Form));
        var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var ribbon = (RibbonControl)host.CreateComponent(typeof(RibbonControl), "ribbon");
        ((Form)host.RootComponent).Controls.Add(ribbon);
        var tab = (RibbonTab)host.CreateComponent(typeof(RibbonTab), "home"); tab.Text = "Home"; ribbon.Tabs.Add(tab);
        var group = (RibbonGroup)host.CreateComponent(typeof(RibbonGroup), "commands"); tab.Groups.Add(group);
        var menu = (RibbonDropDownItem)host.CreateComponent(typeof(RibbonDropDownItem), "actions"); menu.Text = "Actions"; menu.DropDownHeader = "Document actions"; group.Items.Add(menu);
        var command = (RibbonButtonItem)host.CreateComponent(typeof(RibbonButtonItem), "apply"); command.Text = "Apply";
        var text = (RibbonTextBoxItem)host.CreateComponent(typeof(RibbonTextBoxItem), "name"); text.Text = "Name"; text.Value = "Retained text";
        var combo = (RibbonComboBoxItem)host.CreateComponent(typeof(RibbonComboBoxItem), "font"); combo.Text = "Font"; combo.Items.AddRange("Aptos", "Arial"); combo.SelectedIndex = 1;
        var nested = (RibbonSplitButtonItem)host.CreateComponent(typeof(RibbonSplitButtonItem), "more"); nested.Text = "More";
        var child = (RibbonButtonItem)host.CreateComponent(typeof(RibbonButtonItem), "nestedCommand"); child.Text = "Nested command"; nested.Items.Add(child);
        var content = (RibbonSplitButtonItem)host.CreateComponent(typeof(RibbonSplitButtonItem), "custom"); content.Text = "Custom popup";
        content.ContentType = typeof(PopupOptionsControl); group.Items.Add(content);
        menu.Items.AddRange(command, text, combo, nested);
        int clicks = 0, validations = 0;
        command.Click += (_, _) => clicks++; child.Click += (_, _) => clicks++;
        text.ValueValidating += (_, _) => validations++; combo.ValueValidating += (_, _) => validations++;
        Check(content.Content is null, "Design-mode content has not been constructed");
        string server = Directory.GetFiles(Path.GetFullPath(package), "RibbonKit.WinForms.Design.dll", SearchOption.AllDirectories).Single();
        string directory = Path.GetDirectoryName(server)!;
        Assembly? Resolve(AssemblyLoadContext context, AssemblyName name)
        {
            string path = Path.Combine(directory, name.Name + ".dll");
            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        }
        AssemblyLoadContext.Default.Resolving += Resolve;
        try
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(server);
            Check(assembly.Location == server, "Actual packaged designer assembly loaded");
            var read = assembly.GetType("RibbonKit.WinForms.Design.RibbonDesignCommitter", true)!.GetMethod("Read", BindingFlags.Static | BindingFlags.NonPublic)!;
            var render = assembly.GetType("RibbonKit.WinForms.Design.RibbonThemePreviewRenderer", true)!.GetMethod("Render", BindingFlags.Static | BindingFlags.NonPublic)!;
            int previews = 0;
            using var log = new StreamWriter(Path.Combine(output, "result.log")) { AutoFlush = true };
            foreach (var density in Enum.GetValues<RibbonDensity>())
            {
                ribbon.Density = density;
                foreach (var owner in new RibbonDropDownItem[] { menu, nested, content })
                {
                    var draft = read.Invoke(null, [ribbon, host])!;
                    draft.GetType().GetProperty("SelectionKey")!.SetValue(draft, "site:" + owner.Site!.Name);
                    var result = render.Invoke(null, [ribbon, host, draft])!;
                    var popup = result.GetType().GetProperty("Popup")!.GetValue(result)!;
                    var popupType = popup.GetType();
                    string png = (string)popupType.GetProperty("Png")!.GetValue(popup)!;
                    var hits = ((IEnumerable)popupType.GetProperty("Hits")!.GetValue(popup)!).Cast<object>().ToArray();
                    Check(hits.Length > 0, "Selectable popup regions returned");
                    Check(!owner.IsDropDownOpen && content.Content is null, "No live popup or application body created");
                    Check(clicks == 0 && validations == 0 && text.Value == "Retained text" && combo.Value == "Arial", "Live command/editor state preserved");
                    File.WriteAllBytes(Path.Combine(output, density + "-" + owner.Site.Name + ".png"), Convert.FromBase64String(png));
                    log.WriteLine($"PASS: {density} / {owner.Site.Name}; hits={hits.Length}; runtime popup closed; application content absent.");
                    previews++;
                }
            }
            Check(Directory.GetFiles(package, "RibbonKit.WinForms.Design.Client.dll", SearchOption.AllDirectories).Length > 0, "Designer client is packaged");
            log.WriteLine($"PASS: {previews} actual packaged designer previews; ordinary menu, nested split and custom-content placeholder in Compact/Touch; net472 client is packaged. Actual VS interaction remains separate.");
        }
        finally { AssemblyLoadContext.Default.Resolving -= Resolve; }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
