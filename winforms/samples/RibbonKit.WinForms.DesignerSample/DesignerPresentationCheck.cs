using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows.Forms.Design;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class DesignerPresentationCheck
{
    internal static void Verify(string output)
    {
        using (var surface = new DesignSurface(typeof(RibbonForm)))
        {
            var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
            var form = (RibbonForm)host.RootComponent; var designer = (ControlDesigner)host.GetDesigner(form)!;
            _ = ((Control)surface.View!).Handle; _ = form.Handle;
            Require((GetWindowLong(form.Handle, -16) & 0x00C40000) == 0, "Native caption/sizing frame remains in the designer");
            Require((GetWindowLong(form.Handle, -20) & 0x00000301) == 0, "Native bevel edge remains in the designer");
            Require(form.Size == form.ClientSize && form.FormBorderStyle == FormBorderStyle.Sizable, "Authored frame/client-size contract lost");
            Require(designer.SelectionRules.HasFlag(SelectionRules.RightSizeable) && designer.SelectionRules.HasFlag(SelectionRules.BottomSizeable), "Designer resizing unavailable");
            var editor = (TextBox)host.CreateComponent(typeof(TextBox), "notes"); form.Controls.Add(editor);
            editor.Text = "Keep native content"; editor.Select(2, 4); var handle = editor.Handle;
            TypeDescriptor.GetProperties(form)[nameof(form.ClientSize)]!.SetValue(form, new Size(721, 513));
            Require(form.Size == new Size(721, 513) && form.ClientSize == form.Size, "Designer client sizing added native edge clearance");
            Require(editor.Handle == handle && editor.Text == "Keep native content" && editor.SelectionStart == 2 && editor.SelectionLength == 4, "Designer resize recreated or reset the editor");
        }
        using (var runtime = new RibbonForm())
        {
            Require((GetWindowLong(runtime.Handle, -16) & 0x00C00000) == 0x00C00000, "Runtime native frame changed");
            Require((GetWindowLong(runtime.Handle, -16) & 0x00040000) != 0, "Runtime resizing frame changed");
        }
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: package-only framework designer host suppresses native bevel/caption edges; retains FormBorderStyle, resize rules, exact client sizing and native editor HWND/text/selection; runtime caption/sizing styles remain present. Actual VS visual review is separate.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint window, int index);
}
