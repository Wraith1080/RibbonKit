namespace RibbonKitDesignerConsumer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 2 && args[0] == "--verify-editor-startup")
        {
            try { EditorStartupCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] is "--verify-tab-motion-handoff" or "--verify-tab-motion-handoff-solid" or "--verify-tab-shadow" or "--verify-tab-shadow-solid")
        {
            try { TabMotionHandoffCheck.Verify(args[1], args[0].EndsWith("-solid", StringComparison.Ordinal), args[0].StartsWith("--verify-tab-shadow", StringComparison.Ordinal)); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-backdrop-close")
        {
            try { BackdropCloseCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-combo-layout")
        {
            try { ComboLayoutCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-group-layout")
        {
            try { GroupItemLayoutCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-animation")
        {
            try { DesignerAnimationCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-backstage-navigation")
        {
            Directory.CreateDirectory(args[1]);
            try { using var example = new DesignerForm(); example.VerifyBackstageNavigation(); File.WriteAllText(Path.Combine(args[1], "result.log"), "PASS: independent package-only public API; bottom commands, decorative separators, keyboard reachability, inline scrolling fallback, native Notes handles/text/selection, events and model identity retained."); }
            catch (Exception error) { File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-designer-presentation")
        {
            try { DesignerPresentationCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] is "--generate-content-form" or "--verify-content-form")
        {
            try
            {
                if (args[0] == "--generate-content-form") ContentTypeSerializationCheck.Generate(args[1]);
                else ContentTypeSerializationCheck.Verify(args[1]);
            }
            catch (Exception error) { File.WriteAllText(args[1] + ".error.log", error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] is "--generate-file-form" or "--verify-file-form")
        {
            try
            {
                if (args[0] == "--generate-file-form") FileMenuSerializationCheck.Generate(args[1]);
                else FileMenuSerializationCheck.Verify(args[1]);
            }
            catch (Exception error) { File.WriteAllText(args[1] + ".error.log", error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        using var form = new DesignerForm();
        using var focusTrace = args.Length == 2 && args[0] == "--trace-choice-focus"
            ? new DesignerChoiceFocusTrace(form, args[1]) : null;
        if (args.Length == 2 && args[0] == "--verify-page-content")
        {
            Directory.CreateDirectory(args[1]);
            try
            {
                form.VerifyPageContent(); form.VerifyPageContentDisposal();
                File.WriteAllText(Path.Combine(args[1], "result.log"), "PASS: package-only Info/Print instances exist after InitializeComponent and in Form.Load before File clicks; Load-assigned values and native handles/text/selection retained across page switches and reopen; owned controls disposed with Form.");
            }
            catch (Exception error) { File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-roundtrip")
        {
            Directory.CreateDirectory(args[1]);
            try { form.VerifyRoundtrip(); File.WriteAllText(Path.Combine(args[1], "result.log"), "PASS: package-only Form; owned components, IDs, order, image resource, event and two-way binding retained."); }
            catch (Exception error) { File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        Application.Run(form);
    }
}
