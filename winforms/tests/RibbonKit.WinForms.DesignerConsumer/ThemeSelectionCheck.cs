using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Independent packed-runtime check. Uses public RibbonKit/framework APIs only;
// generated C# is compiled normally, then reopened in a fresh process.
internal static class ThemeSelectionCheck
{
    internal static void Generate(string path)
    {
        using var source = new StringWriter(); source.WriteLine("namespace RibbonKitDesignerConsumer;");
        foreach (bool ribbonForm in new[] { false, true })
        foreach (bool selected in new[] { false, true })
        {
            using var ribbon = new RibbonControl { Theme = RibbonThemes.Office2024Light with { Background = Color.Beige } };
            if (selected) ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
            var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
            var manager = (IDesignerSerializationManager)serialization; manager.SetName(ribbon, "ribbon");
            var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
            var statements = serializer.SerializeMember(manager, ribbon, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!);
            if (serialization.Errors.Count != 0) throw new InvalidOperationException(string.Join("\n", serialization.Errors.Cast<object>()));
            if (statements.Cast<CodeStatement>().OfType<CodeAssignStatement>().Count() != (selected ? 1 : 0)) throw new InvalidOperationException("Theme selection persistence omitted an override or replaced application defaults.");
            using var provider = CodeDomProvider.CreateProvider("CSharp"); using var code = new StringWriter();
            foreach (CodeStatement statement in statements) provider.GenerateCodeFromStatement(statement, code, new());
            string name = Name(ribbonForm, selected), parent = ribbonForm ? "RibbonKit.WinForms.RibbonForm" : "System.Windows.Forms.Form";
            source.WriteLine("public sealed class " + name + " : " + parent + "\n{\n" +
                "  private readonly RibbonKit.WinForms.RibbonControl ribbon;\n  public " + name + "()\n  {\n" +
                "    ribbon = new RibbonKit.WinForms.RibbonControl();\n" +
                "    ribbon.Theme = RibbonKit.WinForms.RibbonThemes.Office2024Light with { Background = System.Drawing.Color.Beige };\n" + code +
                "    ClientSize = new System.Drawing.Size(900, 500); Controls.Add(ribbon);\n" + (ribbonForm ? "    Ribbon = ribbon;\n" : "") + "  }\n}\n");
        }
        File.WriteAllText(path, source.ToString());
    }
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        foreach (bool ribbonForm in new[] { false, true })
        foreach (bool selected in new[] { false, true })
        {
            var type = typeof(ThemeSelectionCheck).Assembly.GetType("RibbonKitDesignerConsumer." + Name(ribbonForm, selected))
                ?? throw new InvalidOperationException("Generate theme forms in App and rebuild first.");
            using var form = (Form)Activator.CreateInstance(type)!; var ribbon = form.Controls.OfType<RibbonControl>().Single();
            if (ribbon.BuiltInTheme != (selected ? RibbonBuiltInTheme.Office2024Light : null) || ribbon.Theme.Background != (selected ? Color.White : Color.Beige))
                throw new InvalidOperationException("Reopened initializer lost the built-in/application choice.");
            using var body = new TextBox { BackColor = Color.Coral, Text = "Application content" };
            ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = body });
            if (body.BackColor != Color.Coral || body.Parent is not null) throw new InvalidOperationException("Native content styling/ownership changed.");
            using var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
            image.Save(Path.Combine(output, Name(ribbonForm, selected) + ".png"));
            var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!;
            property.ResetValue(ribbon);
            if (ribbon.BuiltInTheme is not null || ribbon.Theme.Background != Color.Beige || property.ShouldSerializeValue(ribbon)) throw new InvalidOperationException("Reset lost the retained application theme.");
            ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
            ribbon.Theme = ribbon.Theme with { Accent = Color.DarkMagenta };
            if (ribbon.BuiltInTheme is not null || ribbon.Theme.Accent != Color.DarkMagenta) throw new InvalidOperationException("Direct Theme assignment did not clear the override.");
        }
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: public-only packed runtime; actual CodeDOM generation, normal compilation and fresh-process reopen for Form/RibbonForm with explicit/default choices; custom application fallback, Reset, direct Theme precedence and borrowed native content styling/ownership retained.");
    }
    private static string Name(bool ribbonForm, bool selected) => "GeneratedTheme" + (ribbonForm ? "RibbonForm" : "Form") + (selected ? "Selected" : "Application");
}
