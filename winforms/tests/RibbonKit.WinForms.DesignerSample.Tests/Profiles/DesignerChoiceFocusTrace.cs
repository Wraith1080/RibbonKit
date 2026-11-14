using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RibbonKitDesignerConsumer;

// Opt-in native activation evidence for manual mouse/keyboard review. This
// observes only this consumer; it never changes focus or drives desktop input.
internal sealed class DesignerChoiceFocusTrace : NativeWindow, IDisposable
{
    private readonly Form _form;
    private readonly StreamWriter _log;
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly HashSet<ComboBox> _editors = [];
    private (nint Active, nint Foreground, nint Focus) _last;

    internal DesignerChoiceFocusTrace(Form form, string path)
    {
        _form = form;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        _log = new(path) { AutoFlush = true };
        AssignHandle(form.Handle);
        form.Shown += Shown;
        Application.Idle += Idle;
        Write("start");
    }
    private void Shown(object? sender, EventArgs e)
    {
        Write("shown backdrop=" + ((RibbonKit.WinForms.RibbonForm)_form).EffectiveBackdrop);
    }
    private static IEnumerable<Control> Children(Control parent) => parent.Controls.Cast<Control>()
        .SelectMany(c => new[] { c }.Concat(Children(c)));
    private void Idle(object? sender, EventArgs e)
    {
        // Editors may be created lazily when their tab is first selected.
        foreach (var editor in Children(_form).OfType<ComboBox>().Where(c => _editors.Add(c)))
        {
            editor.GotFocus += EditorFocus; editor.LostFocus += EditorFocus;
            editor.SelectedIndexChanged += ChoiceChanged;
        }
        var current = (GetActiveWindow(), GetForegroundWindow(), GetFocus());
        if (current != _last) { _last = current; Write("idle"); }
    }
    private void EditorFocus(object? sender, EventArgs e)
    {
        var editor = (ComboBox)sender!;
        Write($"editor {editor.Name} focused={editor.ContainsFocus} handle={editor.Handle:X}");
    }
    private void ChoiceChanged(object? sender, EventArgs e) => Write("choice " + ((ComboBox)sender!).Text);
    protected override void WndProc(ref Message m)
    {
        if (m.Msg is 0x6 or 0x86 or 0x1C)
            Write($"message={m.Msg:X} w={m.WParam:X} l={m.LParam:X}");
        base.WndProc(ref m);
    }
    private void Write(string text) => _log.WriteLine($"{_clock.ElapsedMilliseconds,6} {text} owner={_form.Handle:X} active={GetActiveWindow():X} foreground={GetForegroundWindow():X} focus={GetFocus():X}");
    public void Dispose()
    {
        Application.Idle -= Idle;
        _form.Shown -= Shown;
        foreach (var editor in _editors)
        {
            editor.GotFocus -= EditorFocus; editor.LostFocus -= EditorFocus;
            editor.SelectedIndexChanged -= ChoiceChanged;
        }
        if (Handle != 0) ReleaseHandle();
        _log.Dispose();
    }
    [DllImport("user32.dll")] private static extern nint GetActiveWindow();
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetFocus();
}
