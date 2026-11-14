using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Package-only code-generation check. Generate, rebuild the app normally, then
// execute the compiled initializer in a separate process. No designer internals.
internal static class FileMenuSerializationCheck
{
    internal static void Generate(string path)
    {
        using var source = new StringWriter(); source.WriteLine("namespace RibbonKitDesignerConsumer;");
        foreach (bool useRibbonForm in new[] { false, true })
        {
            using Form host = useRibbonForm ? new RibbonForm() : new Form(); using var ribbon = new RibbonControl();
            host.Controls.Add(ribbon); if (host is RibbonForm window) window.Ribbon = ribbon;
            ribbon.FileMenu.Visible = true; ribbon.FileMenuMode = RibbonFileMenuMode.Backstage;
            var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
            var manager = (IDesignerSerializationManager)serialization; manager.SetName(ribbon, "ribbon");
            var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
            // Exercise the owned content member, including runtime-property
            // filtering. Serializing File.Visible directly missed that path.
            var statements = serializer.SerializeMember(manager, ribbon, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.FileMenu)]!);
            statements.AddRange(serializer.SerializeMember(manager, ribbon, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.FileMenuMode)]!));
            if (!statements.Cast<CodeStatement>().OfType<CodeAssignStatement>().Any(statement => statement.Left is CodePropertyReferenceExpression { PropertyName: "Visible" } && statement.Right is CodePrimitiveExpression { Value: true }))
                throw new InvalidOperationException("Nested File content serialization omitted Visible=true for " + host.GetType().Name);
            if (serialization.Errors.Count != 0) throw new InvalidOperationException(string.Join("\n", serialization.Errors.Cast<object>()));
            using var provider = CodeDomProvider.CreateProvider("CSharp"); using var code = new StringWriter();
            foreach (CodeStatement statement in statements) provider.GenerateCodeFromStatement(statement, code, new CodeGeneratorOptions());
            string name = useRibbonForm ? "GeneratedRibbonFileForm" : "GeneratedFileForm";
            string baseType = useRibbonForm ? "RibbonKit.WinForms.RibbonForm" : "System.Windows.Forms.Form";
            source.WriteLine("public sealed class " + name + " : " + baseType + "\n{\n" +
                "  private readonly RibbonKit.WinForms.RibbonControl ribbon;\n" +
                "  public " + name + "()\n  {\n    ribbon = new RibbonKit.WinForms.RibbonControl();\n" + code +
                "    ClientSize = new System.Drawing.Size(900, 500);\n    Controls.Add(ribbon);\n" +
                (useRibbonForm ? "    Ribbon = ribbon;\n" : "") + "  }\n}\n");
        }
        File.WriteAllText(path, source.ToString());
        File.WriteAllText(path + ".log", "PASS: nested File content CodeDOM emits Visible=true for both Form and RibbonForm; generated C# ready for a normal build.");
    }
    internal static void Verify(string output)
    {
        foreach (string name in new[] { "GeneratedFileForm", "GeneratedRibbonFileForm" })
        {
            var generatedType = typeof(FileMenuSerializationCheck).Assembly.GetType("RibbonKitDesignerConsumer." + name)
                ?? throw new InvalidOperationException("Generate GeneratedFileForm.cs in App, then rebuild before verification.");
            using var form = (Form)Activator.CreateInstance(generatedType)!;
            var ribbon = form.Controls.OfType<RibbonControl>().Single();
            if (!ribbon.FileMenu.Visible || ribbon.FileMenu.Text != "File" || ribbon.FileMenu.KeyTip != "F" || ribbon.FileMenuMode != RibbonFileMenuMode.Backstage)
                throw new InvalidOperationException("Compiled initializer lost File settings: " + name);
            if (form is RibbonForm host && !ReferenceEquals(host.Ribbon, ribbon)) throw new InvalidOperationException("RibbonForm association lost.");
            using var renderer = new FileRenderer(); ribbon.Renderer = renderer;
            using var pixels = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(pixels, new(Point.Empty, pixels.Size));
            if (renderer.Paints == 0 || renderer.Caption != "File") throw new InvalidOperationException("Runtime did not paint File: " + name);
        }
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: nested File content generated/compiled for both Form and RibbonForm; visibility, caption, F KeyTip, Backstage mode and association retained; runtime renderer paints File in both hosts.");
    }
    private sealed class FileRenderer : RibbonOffice2024Renderer
    {
        internal int Paints; internal string Caption = "";
        public override void DrawFileButton(RibbonPaintContext context, RibbonDropDownItem item)
        { Paints++; Caption = item.Text; base.DrawFileButton(context, item); }
    }
}
