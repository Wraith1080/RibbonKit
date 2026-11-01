using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class AnimationSelectionCheck
{
    internal static void Generate(string path)
    {
        using var source = new StringWriter(); source.WriteLine("namespace RibbonKitDesignerConsumer;");
        foreach (var level in Enum.GetValues<RibbonAnimationLevel>())
        {
            using var ribbon = new RibbonControl { AnimationLevel = level };
            var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
            var manager = (IDesignerSerializationManager)serialization; manager.SetName(ribbon, "ribbon");
            var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
            var statements = serializer.SerializeMember(manager, ribbon, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.AnimationLevel)]!);
            if (serialization.Errors.Count != 0) throw new InvalidOperationException(string.Join("\n", serialization.Errors.Cast<object>()));
            if (statements.OfType<CodeAssignStatement>().Count() != (level == RibbonAnimationLevel.None ? 0 : 1)) throw new InvalidOperationException("Animation default/explicit persistence failed.");
            using var provider = CodeDomProvider.CreateProvider("CSharp"); using var code = new StringWriter();
            foreach (CodeStatement statement in statements) provider.GenerateCodeFromStatement(statement, code, new());
            source.WriteLine("public sealed class GeneratedAnimation" + level + " : System.Windows.Forms.Form\n{\n" +
                "  private readonly RibbonKit.WinForms.RibbonControl ribbon;\n  public GeneratedAnimation" + level + "()\n  {\n" +
                "    ribbon = new RibbonKit.WinForms.RibbonControl();\n" + code + "    Controls.Add(ribbon);\n  }\n}\n");
        }
        File.WriteAllText(path, source.ToString());
    }
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output);
        foreach (var level in Enum.GetValues<RibbonAnimationLevel>())
        {
            var type = typeof(AnimationSelectionCheck).Assembly.GetType("RibbonKitDesignerConsumer.GeneratedAnimation" + level)
                ?? throw new InvalidOperationException("Generate animation forms and rebuild first.");
            using var form = (Form)Activator.CreateInstance(type)!; var ribbon = form.Controls.OfType<RibbonControl>().Single();
            if (ribbon.AnimationLevel != level) throw new InvalidOperationException("Compiled initializer lost animation choice: " + level);
            var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.AnimationLevel)]!;
            property.ResetValue(ribbon);
            if (ribbon.AnimationLevel != RibbonAnimationLevel.None || property.ShouldSerializeValue(ribbon)) throw new InvalidOperationException("Reset lost the None default.");
        }
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: independent package-only public API; actual animation CodeDOM generated and normally compiled; fresh-process reopen retains None/Subtle/Expressive; default omission and Reset to None pass. No motion-engine or native presentation claim.");
    }
}
