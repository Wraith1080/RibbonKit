using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class ContentTypeSerializationCheck
{
    internal static void Generate(string path)
    {
        using var source = new StringWriter(); source.WriteLine("namespace RibbonKitDesignerConsumer;");
        foreach (bool integrated in new[] { false, true })
        {
            using Form form = integrated ? new RibbonForm() : new Form(); using var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
            if (form is RibbonForm host) host.Ribbon = ribbon;
            var page = new RibbonBackstagePageItem { Text = "Info", ContentType = typeof(BackstageInfoControl) };
            var footerPage = new RibbonBackstagePageItem { Text = "Print", ContentType = typeof(BackstagePrintControl) };
            var separator = new RibbonMenuSeparatorItem(); var footerSeparator = new RibbonMenuSeparatorItem();
            ribbon.Backstage.Items.AddRange(page, separator); ribbon.Backstage.FooterItems.AddRange(footerSeparator, footerPage);
            var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
            var manager = (IDesignerSerializationManager)serialization; manager.SetName(ribbon, "ribbon");
            manager.SetName(page, "page");
            manager.SetName(footerPage, "footerPage"); manager.SetName(separator, "separator"); manager.SetName(footerSeparator, "footerSeparator");
            var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
            // A Form document serializes the sited page component as well as the
            // owned Backstage collection that references it. Exercise both.
            var statements = new CodeStatementCollection();
            foreach (var item in ribbon.Backstage.Items.Concat(ribbon.Backstage.FooterItems))
            {
                var itemSerializer = (CodeDomSerializer)manager.GetSerializer(item.GetType(), typeof(CodeDomSerializer))!;
                statements.AddRange((CodeStatementCollection)itemSerializer.Serialize(manager, item)!);
            }
            statements.AddRange(serializer.SerializeMember(manager, ribbon, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.Backstage)]!));
            if (serialization.Errors.Count != 0) throw new InvalidOperationException(string.Join("\n", serialization.Errors.Cast<object>()));
            using var provider = CodeDomProvider.CreateProvider("CSharp"); using var code = new StringWriter();
            foreach (CodeStatement statement in statements) provider.GenerateCodeFromStatement(statement, code, new CodeGeneratorOptions());
            if (!code.ToString().Contains("typeof(RibbonKitDesignerConsumer.BackstageInfoControl)")) throw new InvalidOperationException("Backstage component serialization omitted the UserControl type. Generated:\n" + code);
            if (!code.ToString().Contains("Backstage.FooterItems") || !code.ToString().Contains("typeof(RibbonKitDesignerConsumer.BackstagePrintControl)")) throw new InvalidOperationException("Footer collection or type declaration omitted. Generated:\n" + code);
            string name = integrated ? "GeneratedContentRibbonForm" : "GeneratedContentForm";
            source.WriteLine("public sealed class " + name + " : " + (integrated ? "RibbonKit.WinForms.RibbonForm" : "System.Windows.Forms.Form")
                + "\n{\n private readonly RibbonKit.WinForms.RibbonControl ribbon;\n public " + name + "()\n {\n ribbon = new RibbonKit.WinForms.RibbonControl();\n ribbon.BeginInit();\n" + code
                + "\n Controls.Add(ribbon);\n" + (integrated ? " Ribbon = ribbon;\n" : "") + " ribbon.EndInit();\n }\n}\n");
            if (ribbon.Backstage.Items.Concat(ribbon.Backstage.FooterItems).OfType<RibbonBackstagePageItem>().Any(page => page.Content is not null)) throw new InvalidOperationException("Serialization constructed a page body.");
        }
        File.WriteAllText(path, source.ToString());
    }
    internal static void Verify(string output)
    {
        foreach (string name in new[] { "GeneratedContentForm", "GeneratedContentRibbonForm" })
        {
            var type = typeof(ContentTypeSerializationCheck).Assembly.GetType("RibbonKitDesignerConsumer." + name) ?? throw new InvalidOperationException("Generate the content forms and rebuild first.");
            using var form = (Form)Activator.CreateInstance(type)!; var ribbon = form.Controls.OfType<RibbonControl>().Single();
            var page = ribbon.Backstage.Items.OfType<RibbonBackstagePageItem>().Single();
            var footerPage = ribbon.Backstage.FooterItems.OfType<RibbonBackstagePageItem>().Single();
            if (page.ContentType != typeof(BackstageInfoControl) || page.Content is not BackstageInfoControl || ribbon.IsBackstageOpen)
                throw new InvalidOperationException("Compiled owned-Backstage initializer must retain ContentType and create its instance at EndInit without opening Backstage.");
            if (footerPage.ContentType != typeof(BackstagePrintControl) || footerPage.Content is not BackstagePrintControl
                || ribbon.Backstage.Items.Last() is not RibbonMenuSeparatorItem || ribbon.Backstage.FooterItems.First() is not RibbonMenuSeparatorItem)
                throw new InvalidOperationException("Compiled initializer must retain main/footer collection order, separators, and eagerly initialized footer page content.");
            if (form is RibbonForm host && !ReferenceEquals(host.Ribbon, ribbon)) throw new InvalidOperationException("RibbonForm association lost.");
        }
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "result.log"), "PASS: owned-Backstage CodeDOM emits Items/FooterItems, main and bottom separators and typeof(UserControl); compiled Form and RibbonForm retain order, associations, and eagerly initialize both main/footer page types at EndInit without opening Backstage.");
    }
}
