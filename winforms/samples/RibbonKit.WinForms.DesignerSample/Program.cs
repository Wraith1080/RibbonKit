namespace RibbonKitDesignerConsumer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Diagnostics have their own executable; an old automation command must
        // fail instead of silently opening an unverified interactive sample.
        if (args.Any(arg => new[] { "--verify-", "--profile-", "--trace-", "--generate-", "--capture-" }.Any(prefix => arg.StartsWith(prefix, StringComparison.Ordinal))))
        {
            Console.Error.WriteLine("Designer Sample checks moved to winforms/eng/Test-DesignerSample.ps1.");
            Environment.ExitCode = 2;
            return;
        }
        ApplicationConfiguration.Initialize();
        using var form = new DesignerForm();
        SampleStartup.Apply(form, args);
        Application.Run(form);
    }
}