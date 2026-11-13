using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Length == 2 && args[0] == "--verify-options-fields")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { OptionsFieldCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), (callbackFailure is null ? error : new AggregateException(error, callbackFailure)).ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-options-shell")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { OptionsShellCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), (callbackFailure is null ? error : new AggregateException(error, callbackFailure)).ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-options-appearance")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { OptionsAppearanceCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), (callbackFailure is null ? error : new AggregateException(error, callbackFailure)).ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-tip-appearance")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { TipAppearanceCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), (callbackFailure is null ? error : new AggregateException(error, callbackFailure)).ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-gallery-content")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { GalleryContentCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), (callbackFailure is null ? error : new AggregateException(error, callbackFailure)).ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-gallery-appearance")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { GalleryAppearanceCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), (callbackFailure is null ? error : new AggregateException(error, callbackFailure)).ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-application-content")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { ApplicationMenuContentCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), (callbackFailure is null ? error : new AggregateException(error, callbackFailure)).ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-application-surface")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { ApplicationMenuSurfaceCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), (callbackFailure is null ? error : new AggregateException(error, callbackFailure)).ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-application-chrome")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { ApplicationMenuChromeCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), (callbackFailure is null ? error : new AggregateException(error, callbackFailure)).ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-menu-appearance")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { MenuAppearanceCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-popup-appearance")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { PopupAppearanceCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] is "--verify-editor-appearance" or "--verify-editor-appearance-demo")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try
            {
                if (args[0] == "--verify-editor-appearance-demo")
                {
                    Directory.CreateDirectory(args[1]); using var demo = new DesignerForm(); demo.VerifyEditorAppearanceDemo(args[1]);
                    File.WriteAllText(Path.Combine(args[1], "result.log"), "PASS: Theme effects 4 editor buttons/reset/idempotence; earlier tabs retain seven/five/four groups.");
                }
                else EditorAppearanceCheck.Verify(args[1]);
                if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure);
            }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-command-content")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { CommandContentCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-command-appearance")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { CommandAppearanceCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-body-presentation")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { BodyPresentationCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-shared-theme-effects")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try { SharedThemeEffectsCheck.Verify(args[1]); if (callbackFailure is not null) throw new InvalidOperationException("A native callback failed.", callbackFailure); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-backstage-appearance")
        {
            Exception? callbackFailure = null;
            ThreadExceptionEventHandler failed = (_, error) => callbackFailure ??= error.Exception;
            Application.ThreadException += failed;
            try
            {
                BackstageAppearanceCheck.Verify(args[1]);
                if (callbackFailure is not null) throw new InvalidOperationException("A native paint callback failed.", callbackFailure);
            }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            finally { Application.ThreadException -= failed; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-backstage-effects")
        {
            try { BackstageEffectsCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-header-presentation")
        {
            try { HeaderPresentationCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-large-command-geometry")
        {
            try { LargeCommandGeometryCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-chrome-geometry")
        {
            try { ChromeGeometryCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-paired-geometry")
        {
            try { PairedGeometryCheck.Verify(args[1]); LargeCommandGeometryCheck.Verify(Path.Combine(args[1], "large")); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-command-split-geometry")
        {
            try { CommandSplitGeometryCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-illustrated-geometry")
        {
            try { IllustratedGeometryCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-editor-geometry")
        {
            try { EditorGeometryCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-menu-container-geometry")
        {
            try { MenuContainerGeometryCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-application-menu")
        {
            Directory.CreateDirectory(args[1]);
            try { using var example = new DesignerForm(); example.VerifyApplicationMenu(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "PASS: permanent package-only Designer Sample; generated File/default/footer models, ContentType initialization, source-specific controls, retained HWND/text, split pane and footer event/dismissal."); }
            catch (Exception error) { File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-gallery-scrollbar-paint")
        {
            try { GalleryScrollbarPaintCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-appearance-colors")
        {
            try { AppearanceColorCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 3 && args[0] == "--verify-popup-designer-package")
        {
            try { PopupDesignerPackageCheck.Verify(args[1], args[2]); }
            catch (Exception error) { Directory.CreateDirectory(args[2]); File.AppendAllText(Path.Combine(args[2], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-combo-backdrop-paint")
        {
            try { ComboBackdropPaintCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-combo-selection-paint")
        {
            try { ComboSelectionPaintCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] is "--profile-inline-gallery" or "--profile-inline-gallery-no-motion" or "--profile-inline-gallery-builtin")
        {
            try { InlineGalleryOpeningProfile.Run(args[1], args[0].EndsWith("-no-motion", StringComparison.Ordinal), args[0].EndsWith("-builtin", StringComparison.Ordinal)); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-gallery-hosts")
        {
            try { GalleryHostPresentationCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] is "--verify-dropdown-content" or "--verify-dropdown-content-animated" or "--verify-dropdown-content-expressive")
        {
            try { DropDownContentCheck.Verify(args[1], args[0] != "--verify-dropdown-content", args[0].EndsWith("-expressive", StringComparison.Ordinal) ? RibbonKit.WinForms.RibbonAnimationLevel.Expressive : RibbonKit.WinForms.RibbonAnimationLevel.Subtle); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-simplified-overflow-visual")
        {
            try { SimplifiedOverflowVisualCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-qat-gallery-presenter")
        {
            try { QuickAccessGalleryPresenterCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-inline-gallery-presenter")
        {
            try { InlineGalleryPresenterCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length is 2 or 3 && args[0] is "--profile-gallery-opening" or "--profile-gallery-opening-animated" or "--trace-gallery-opening")
        {
            try { GalleryOpeningProfile.Run(args[1], args[0] == "--trace-gallery-opening", args[0] == "--profile-gallery-opening-animated", args.Length == 3 ? int.Parse(args[2]) : null); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length is 2 or 3 && args[0] == "--verify-gallery-presenter")
        {
            try { GalleryPresenterDemo.Verify(args[1], args.Length == 3 ? int.Parse(args[2]) : 18); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-caption-buttons")
        {
            try { CaptionButtonCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
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
        if (args.Length == 2 && args[0] == "--verify-tab-header-clipping")
        {
            try { TabHeaderClippingCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] is "--verify-frame-geometry" or "--capture-frame-surfaces")
        {
            try { if (args[0] == "--capture-frame-surfaces") FrameGeometryCheck.CaptureNativeSurfaces(args[1]); else FrameGeometryCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] is "--verify-tip-caption-geometry" or "--capture-caption-geometry")
        {
            try { TipCaptionGeometryCheck.Verify(args[1], args[0] == "--capture-caption-geometry"); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-gallery-review-correction")
        {
            try { GalleryReviewCorrectionCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-gallery-backstage-geometry")
        {
            try { GalleryBackstageGeometryCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-office2019-theme")
        {
            try { Office2019ThemeCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-backstage-sync")
        {
            try { BackstageSyncCheck.Verify(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        using var form = new DesignerForm();
        if (args.Length is 2 or 3 && args[0] == "--verify-backstage-navbar-paint")
        {
            try { form.VerifyBackstageNavbarPaint(args[1], args.Length == 3 ? Enum.Parse<RibbonBuiltInTheme>(args[2]) : RibbonBuiltInTheme.Office2019Light); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "failure.log"), error.ToString()); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-office2019-review-demo")
        {
            try { form.VerifyOffice2019ReviewDemo(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-input-mode")
        {
            try { form.VerifyInputMode(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.AppendAllText(Path.Combine(args[1], "result.log"), "\nFAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
        if (args.Length == 2 && args[0] == "--verify-menu-geometry-demo")
        {
            try { form.VerifyMenuGeometryDemo(args[1]); }
            catch (Exception error) { Directory.CreateDirectory(args[1]); File.WriteAllText(Path.Combine(args[1], "result.log"), "FAIL: " + error); Environment.ExitCode = 1; }
            return;
        }
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
        if (args.Contains("--menu-geometry-demo")) { form.EnableMenuGeometryDemo(); form.EnableIllustratedGeometryDemo(); form.EnableCommandSplitGeometryDemo(); form.EnablePairedGeometryDemo(); form.EnableChromeGeometryDemo(); form.EnableGalleryBackstageGeometryDemo(); form.EnableTipCaptionGeometryDemo(); form.EnableFrameGeometryDemo(); }
        if (args.Contains("--office2019-theme-demo")) form.EnableOffice2019ThemeDemo();
        Application.Run(form);
    }
}
