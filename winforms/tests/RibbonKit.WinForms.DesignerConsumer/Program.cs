namespace RibbonKitDesignerConsumer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        using var form = new DesignerForm();
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
