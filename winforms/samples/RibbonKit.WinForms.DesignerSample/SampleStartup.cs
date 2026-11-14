using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class SampleStartup
{
    internal static void Apply(DesignerForm form, string[] args)
    {
        // Legacy review switches select a theme without creating tabs. Explicit
        // verification now lives in the dedicated test host.
        if (args.Contains("--office2019-light") || args.Contains("--office2019-theme-demo"))
        {
            form.TitleBarColor = Color.Empty;
            form.Ribbon!.BuiltInTheme = RibbonBuiltInTheme.Office2019Light;
        }
        form.SynchronizeThemeGallery();
    }

}
