using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using RibbonKit.WinForms;
using WF = System.Windows.Forms;

namespace RibbonKit.WinForms.NativeTests;

internal static partial class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--state-appearance-checks") return IndependentConsumer.StateAppearanceChecks.Run(args[1]);
        if (args.Length == 2 && args[0] == "--state-appearance-native-checks") return IndependentConsumer.StateAppearanceChecks.Run(args[1], nativeOnly: true);
        if (args.Length == 2 && args[0] == "--state-appearance-office-checks") return IndependentConsumer.StateAppearanceChecks.Run(args[1], officeOnly: true, rasterOnly: true);
        if (args.Length == 2 && args[0] == "--backstage-richtext-checks") return IndependentConsumer.BackstageRichTextChecks.Run(args[1]);
        if (args.Length == 2 && args[0] is "--backstage-motion-tail-checks" or "--backstage-motion-tail-checks-flat")
            return IndependentConsumer.BackstageInteractionChecks.Run(args[1], args[0].EndsWith("-flat", StringComparison.Ordinal), motionChecks: true, motionTailOnly: true);
        if (args.Length == 2 && args[0] == "--backstage-caption-checks") return IndependentConsumer.BackdropAppearanceChecks.Run(args[1], captionOnly: true);
        if (args.Length == 2 && args[0] == "--backstage-page-frame-checks") return IndependentConsumer.BackdropAppearanceChecks.Run(args[1], shadowOnly: true);
        if (args.Length == 2 && args[0] == "--backstage-page-shadow-checks") return IndependentConsumer.BackdropAppearanceChecks.Run(args[1], shadowOnly: true);
        if (args.Length == 2 && args[0] == "--backstage-close-checks") return IndependentConsumer.BackdropAppearanceChecks.Run(args[1], closingOnly: true);
        if (args.Length == 2 && args[0] == "--backdrop-appearance-checks") return IndependentConsumer.BackdropAppearanceChecks.Run(args[1]);
        if (args.Length == 2 && args[0] == "--keytip-appearance-checks") return IndependentConsumer.KeyTipAppearanceChecks.Run(args[1]);
        if (args.Length == 2 && args[0] == "--backdrop-startup-checks") return IndependentConsumer.WindowBackdropChecks.RunStartup(args[1]);
        if (args.Length == 2 && args[0] == "--gallery-backdrop-checks") return IndependentConsumer.GalleryBackdropChecks.Run(args[1]);
        if (args.Length == 2 && args[0] is "--backdrop-transparency-checks" or "--backdrop-transparency-checks-flat") return IndependentConsumer.WindowBackdropChecks.Run(args[1], flat: args[0].EndsWith("-flat", StringComparison.Ordinal), transparency: true);
        if (args.Length == 2 && args[0] == "--backdrop-simplified-checks") return IndependentConsumer.WindowBackdropChecks.Run(args[1], simplified: true);
        if (args.Length == 2 && args[0] is "--backdrop-checks" or "--backdrop-checks-flat") return IndependentConsumer.WindowBackdropChecks.Run(args[1], args[0].EndsWith("-flat", StringComparison.Ordinal));
        if (args.Length == 3 && args[0] is "--window-motion-regression" or "--window-motion-regression-flat")
            return IndependentConsumer.WindowMotionRegressionChecks.Run(args[2], args[1], args[0].EndsWith("-flat", StringComparison.Ordinal));
        if (args.Length == 2 && args[0] is "--window-projection-checks" or "--window-projection-checks-flat")
            return IndependentConsumer.WindowIntegrationChecks.Run(args[1], args[0] == "--window-projection-checks-flat", projectionsOnly: true);
        if (args.Length == 2 && args[0] is "--window-snap-checks" or "--window-snap-checks-flat")
            return IndependentConsumer.WindowIntegrationChecks.Run(args[1], args[0] == "--window-snap-checks-flat", frameOnly: true, snapOnly: true);
        if (args.Length == 2 && args[0] is "--window-tail-checks" or "--window-tail-checks-flat")
            return IndependentConsumer.WindowIntegrationChecks.Run(args[1], args[0] == "--window-tail-checks-flat", frameOnly: true, tailOnly: true);
        if (args.Length == 2 && args[0] is "--window-frame-checks" or "--window-frame-checks-flat")
            return IndependentConsumer.WindowIntegrationChecks.Run(args[1], args[0] == "--window-frame-checks-flat", frameOnly: true);
        if (args.Length == 2 && args[0] is "--window-checks" or "--window-checks-flat")
            return IndependentConsumer.WindowIntegrationChecks.Run(args[1], args[0] == "--window-checks-flat");
        if (args.Length == 2 && args[0] is "--group-fade-checks" or "--group-fade-checks-flat")
            return IndependentConsumer.GroupMotionInteractionChecks.Run(args[1], args[0] == "--group-fade-checks-flat", true);
        if (args.Length == 2 && args[0] is "--group-motion-checks" or "--group-motion-checks-flat")
            return IndependentConsumer.GroupMotionInteractionChecks.Run(args[1], args[0] == "--group-motion-checks-flat");
        if (args.Length == 2 && args[0] is "--backstage-motion-checks" or "--backstage-motion-checks-flat")
            return IndependentConsumer.BackstageInteractionChecks.Run(args[1], args[0] == "--backstage-motion-checks-flat", motionChecks: true);
        if (args.Length == 2 && args[0] is "--popup-motion-input-checks" or "--popup-motion-input-checks-flat")
            return IndependentConsumer.PopupMotionInteractionChecks.Run(args[1], args[0] == "--popup-motion-input-checks-flat", true);
        if (args.Length == 2 && args[0] is "--popup-motion-checks" or "--popup-motion-checks-flat")
            return IndependentConsumer.PopupMotionInteractionChecks.Run(args[1], args[0] == "--popup-motion-checks-flat");
        if (args.Length == 2 && args[0] is "--minimize-motion-checks" or "--minimize-motion-checks-flat")
            return IndependentConsumer.GeometryMotionInteractionChecks.Run(args[1], args[0] == "--minimize-motion-checks-flat", minimizeOnly: true);
        if (args.Length == 2 && args[0] == "--expressive-pill-checks")
            return IndependentConsumer.GeometryMotionInteractionChecks.Run(args[1], false, tabsOnly: true, tabLevel: RibbonAnimationLevel.Expressive);
        if (args.Length == 2 && args[0] is "--header-arrival-checks" or "--header-arrival-checks-flat")
            return IndependentConsumer.GeometryMotionInteractionChecks.Run(args[1], args[0] == "--header-arrival-checks-flat", headersOnly: true);
        if (args.Length == 2 && args[0] is "--tab-slide-motion-checks" or "--tab-slide-motion-checks-flat")
            return IndependentConsumer.GeometryMotionInteractionChecks.Run(args[1], args[0] == "--tab-slide-motion-checks-flat", false, true, RibbonTabContentAnimation.SlideDown);
        if (args.Length == 2 && args[0] is "--tab-motion-checks" or "--tab-motion-checks-flat")
            return IndependentConsumer.GeometryMotionInteractionChecks.Run(args[1], args[0] == "--tab-motion-checks-flat", false, true);
        if (args.Length == 2 && args[0] is "--geometry-menu-motion-checks" or "--geometry-menu-motion-checks-flat")
            return IndependentConsumer.GeometryMotionInteractionChecks.Run(args[1], args[0] == "--geometry-menu-motion-checks-flat", true);
        if (args.Length == 2 && args[0] is "--geometry-motion-checks" or "--geometry-motion-checks-flat")
            return IndependentConsumer.GeometryMotionInteractionChecks.Run(args[1], args[0] == "--geometry-motion-checks-flat");
        if (args.Length == 2 && args[0] is "--simplified-context-checks" or "--simplified-context-checks-flat")
            return IndependentConsumer.SimplifiedContextInteractionChecks.Run(args[1], args[0] == "--simplified-context-checks-flat");
        if (args.Length == 2 && args[0] is "--review-checks" or "--review-checks-flat")
            return IndependentConsumer.ReviewInteractionChecks.Run(args[1], args[0] == "--review-checks-flat");
        if (args.Length == 2 && args[0] is "--options-popup-checks" or "--options-popup-checks-flat")
            return IndependentConsumer.OptionsPopupInteractionChecks.Run(args[1], args[0] == "--options-popup-checks-flat");
        if (args.Length == 2 && args[0] is "--simplified-checks" or "--simplified-checks-flat")
            return IndependentConsumer.SimplifiedInteractionChecks.Run(args[1], args[0] == "--simplified-checks-flat");
        if (args.Length == 2 && args[0] is "--ribbon-customization-checks" or "--ribbon-customization-checks-flat")
            return IndependentConsumer.RibbonCustomizationInteractionChecks.Run(args[1], args[0] == "--ribbon-customization-checks-flat");
        if (args.Length == 2 && args[0] is "--customization-editor-checks" or "--customization-editor-checks-flat")
            return IndependentConsumer.CustomizationEditorInteractionChecks.Run(args[1], args[0] == "--customization-editor-checks-flat");
        if (args.Length == 2 && args[0] is "--popup-refinement-checks" or "--popup-refinement-checks-flat")
            return IndependentConsumer.PopupRefinementChecks.Run(args[1], args[0] == "--popup-refinement-checks-flat");
        if (args.Length == 2 && args[0] is "--gallery-reduction-checks" or "--gallery-reduction-checks-flat")
            return IndependentConsumer.InRibbonGalleryInteractionChecks.Run(args[1], args[0] == "--gallery-reduction-checks-flat", reductionOnly: true);
        if (args.Length == 2 && args[0] is "--density-checks" or "--density-checks-flat")
            return IndependentConsumer.DensityInteractionChecks.Run(args[1], args[0] == "--density-checks-flat");
        if (args.Length == 2 && args[0] is "--modal-checks" or "--modal-checks-flat")
            return IndependentConsumer.ModalInteractionChecks.Run(args[1], args[0] == "--modal-checks-flat");
        if (args.Length == 2 && args[0] is "--merge-checks" or "--merge-checks-flat")
            return IndependentConsumer.MergeInteractionChecks.Run(args[1], args[0] == "--merge-checks-flat");
        if (args.Length == 2 && args[0] is "--gallery-strip-checks" or "--gallery-strip-checks-flat")
            return IndependentConsumer.InRibbonGalleryInteractionChecks.Run(args[1], args[0] == "--gallery-strip-checks-flat", stripOnly: true);
        if (args.Length == 2 && args[0] is "--qat-gallery-checks" or "--qat-gallery-checks-flat")
            return IndependentConsumer.InRibbonGalleryInteractionChecks.Run(args[1], args[0] == "--qat-gallery-checks-flat", true);
        if (args.Length == 2 && args[0] is "--in-ribbon-gallery-checks" or "--in-ribbon-gallery-checks-flat")
            return IndependentConsumer.InRibbonGalleryInteractionChecks.Run(args[1], args[0] == "--in-ribbon-gallery-checks-flat");
        if (args.Length == 2 && args[0] is "--gallery-checks" or "--gallery-checks-flat")
            return IndependentConsumer.GalleryInteractionChecks.Run(args[1], args[0] == "--gallery-checks-flat");
        if (args.Length == 2 && args[0] is "--backstage-width-checks" or "--backstage-width-checks-flat")
            return IndependentConsumer.BackstageInteractionChecks.Run(args[1], args[0] == "--backstage-width-checks-flat", true);
        if (args.Length == 2 && args[0] is "--backstage-checks" or "--backstage-checks-flat")
            return IndependentConsumer.BackstageInteractionChecks.Run(args[1], args[0] == "--backstage-checks-flat");
        if (args.Length == 2 && args[0] is "--file-checks" or "--file-checks-flat")
            return IndependentConsumer.FileMenuInteractionChecks.Run(args[1], args[0] == "--file-checks-flat");
        if (args.Length == 2 && args[0] is "--contextual-checks" or "--contextual-checks-flat")
            return IndependentConsumer.ContextualTabInteractionChecks.Run(args[1], args[0] == "--contextual-checks-flat");
        if (args.Length == 2 && args[0] is "--qat-option-checks" or "--qat-option-checks-flat")
            return IndependentConsumer.QuickAccessOptionInteractionChecks.Run(args[1], args[0] == "--qat-option-checks-flat");
        if (args.Length == 2 && args[0] is "--qat-group-checks" or "--qat-group-checks-flat")
            return IndependentConsumer.QuickAccessGroupInteractionChecks.Run(args[1], args[0] == "--qat-group-checks-flat");
        if (args.Length == 2 && args[0] is "--qat-menu-checks" or "--qat-menu-checks-flat")
            return IndependentConsumer.QuickAccessMenuInteractionChecks.Run(args[1], args[0] == "--qat-menu-checks-flat");
        if (args.Length == 2 && args[0] is "--qat-combo-checks" or "--qat-combo-checks-flat")
            return IndependentConsumer.QuickAccessComboInteractionChecks.Run(args[1], args[0] == "--qat-combo-checks-flat");
        if (args.Length == 2 && args[0] is "--qat-checks" or "--qat-checks-flat")
            return IndependentConsumer.QuickAccessInteractionChecks.Run(args[1], args[0] == "--qat-checks-flat");
        if (args.Length == 2 && args[0] is "--minimize-checks" or "--minimize-checks-flat")
            return IndependentConsumer.MinimizeInteractionChecks.Run(args[1], args[0] == "--minimize-checks-flat");
        if (args.Length == 2 && args[0] is "--menu-checks" or "--menu-checks-flat")
            return IndependentConsumer.MenuInteractionChecks.Run(args[1], args[0] == "--menu-checks-flat");
        if (args.Length == 2 && args[0] is "--screentip-checks" or "--screentip-checks-flat")
            return IndependentConsumer.ScreenTipInteractionChecks.Run(args[1], args[0] == "--screentip-checks-flat");
        if (args.Length == 2 && args[0] is "--option-checks" or "--option-checks-flat")
            return IndependentConsumer.OptionInteractionChecks.Run(args[1], args[0] == "--option-checks-flat");
        if (args.Length == 2 && args[0] is "--combo-checks" or "--combo-checks-flat")
            return IndependentConsumer.ComboBoxInteractionChecks.Run(args[1], args[0] == "--combo-checks-flat");
        if (args.Length == 2 && args[0] == "--editor-checks")
            return IndependentConsumer.TextBoxInteractionChecks.Run(args[1]);
        if (args.Length == 2 && args[0] is "--split-checks" or "--split-checks-flat")
            return IndependentConsumer.SplitInteractionChecks.Run(args[1], args[0] == "--split-checks-flat");
        if (args.Length == 2 && args[0] == "--group-padding-checks")
            return IndependentConsumer.SplitInteractionChecks.Run(args[1], groupPaddingOnly: true);
        if (args.Length == 2 && args[0] is "--touch-split-checks" or "--touch-split-checks-flat")
            return IndependentConsumer.SplitInteractionChecks.Run(args[1], args[0] == "--touch-split-checks-flat", true);
        if (args.Length == 2 && args[0] is "--message-checks" or "--message-checks-flat")
            return IndependentConsumer.MessageInteractionChecks.Run(args[1], args[0] == "--message-checks-flat");
        if (args.Length == 2 && args[0] is "--message-motion" or "--message-motion-flat")
            return IndependentConsumer.MessageInteractionChecks.Run(args[1], args[0] == "--message-motion-flat", true);
        if (args.Length == 2 && args[0] is "--pre-m6b-review" or "--pre-m6b-review-flat")
            return IndependentConsumer.PreM6BReviewChecks.Run(args[1], args[0] == "--pre-m6b-review-flat");
        if (args.Length == 2 && args[0] is "--pre-m6b-popup-close" or "--pre-m6b-popup-close-flat")
            return IndependentConsumer.PreM6BReviewChecks.Run(args[1], args[0] == "--pre-m6b-popup-close-flat", true);
        if (args.Length == 2 && args[0] is "--customization-alignment" or "--customization-alignment-flat")
            return IndependentConsumer.PreM6BReviewChecks.Run(args[1], args[0] == "--customization-alignment-flat", optionsOnly: true);
        if (args.Length == 2 && args[0] is "--review-followup" or "--review-followup-flat")
            return IndependentConsumer.ReviewFollowupChecks.Run(args[1], args[0] == "--review-followup-flat");
        if (args.Length == 2 && args[0] is "--review-followup-groups" or "--review-followup-groups-flat")
            return IndependentConsumer.ReviewFollowupChecks.Run(args[1], args[0] == "--review-followup-groups-flat", true);
        if (args.Length == 2 && args[0] is "--review-followup-groups-touch" or "--review-followup-groups-touch-flat")
            return IndependentConsumer.ReviewFollowupChecks.Run(args[1], args[0].EndsWith("-flat"), true, RibbonDensity.Touch);
        if (args.Length == 2 && args[0] is "--customization-checks" or "--customization-checks-flat")
            return IndependentConsumer.CustomizationInteractionChecks.Run(args[1], args[0] == "--customization-checks-flat");
        if (args.Contains("--server"))
        {
            Server();
            return 0;
        }
        if (args.Length == 2 && args[0] == "--benchmark")
            return Benchmark.Run(args[1]);
        if (args.Length == 2 && args[0] == "--benchmark-popups")
            return Benchmark.Run(args[1], false);
        if (args.Length == 2 && args[0] == "--benchmark-first-popups")
            return Benchmark.Run(args[1], false, 0);
        if (args.Length == 2 && args[0] == "--capture-popups")
            return PopupCapture.Run(args[1]);
        int appArgument = Array.IndexOf(args, "--app");
        string app = appArgument >= 0 && args.Length > appArgument + 1 ? args[appArgument + 1] : Environment.ProcessPath!;
        using var process = Process.Start(new ProcessStartInfo(app, "--server") { UseShellExecute = false })!;
        try
        {
            AutomationElement? window = null;
            Wait(() => (window = AutomationElement.RootElement.FindFirst(TreeScope.Children, new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id))) is not null);
            var root = window!.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, "native.ribbon"));
            Check(root is not null, "Native ribbon root is discoverable");
            int focusArgument = Array.IndexOf(args, "--focus-cues");
            if (focusArgument >= 0)
            {
                FocusCues(window, root!, process, args[focusArgument + 1]);
                return 0;
            }
            if (args.Contains("--popup-dismissal"))
            {
                PopupDismissal(window, root!, process);
                return 0;
            }
            if (args.Contains("--keyboard"))
            {
                Keyboard(window, root!, process);
                return 0;
            }
            var copy = Find(root!, "cmd.copy");
            ((InvokePattern)copy.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
            Wait(() => window.Current.Name.Contains("Copy=1"));
            var bold = Find(root!, "cmd.bold");
            var toggle = (TogglePattern)bold.GetCurrentPattern(TogglePattern.Pattern);
            Check(toggle.Current.ToggleState == ToggleState.Off, "Initial toggle state");
            using var stateChanged = new ManualResetEventSlim();
            AutomationPropertyChangedEventHandler changed = (_, e) => { if (e.Property == TogglePattern.ToggleStateProperty) stateChanged.Set(); };
            Automation.AddAutomationPropertyChangedEventHandler(bold, TreeScope.Element, changed, TogglePattern.ToggleStateProperty);
            toggle.Toggle();
            Wait(() => window.Current.Name.Contains("Bold=True"));
            Check(toggle.Current.ToggleState == ToggleState.On, "Toggle action updates state");
            Check(stateChanged.Wait(8000), "Toggle property change reaches an external UIA subscriber");
            Automation.RemoveAutomationPropertyChangedEventHandler(bold, changed);
            var disabled = Find(root!, "cmd.disabled");
            Check(!disabled.Current.IsEnabled, "Disabled state");
            bool rejected = false;
            try
            {
                ((InvokePattern)disabled.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
            }
            catch (ElementNotEnabledException) { rejected = true; }
            Check(rejected, "Disabled Invoke is rejected");
            var tab = Find(root!, "tab.insert");
            ((SelectionItemPattern)tab.GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            Wait(() => Find(root!, "cmd.link", false) is not null);
            Check(Find(root!, "cmd.copy", false) is null, "Inactive commands are not in the active tree");
            ((SelectionItemPattern)Find(root!, "tab.home").GetCurrentPattern(SelectionItemPattern.Pattern)).Select();
            var menu = Find(root!, "cmd.menu");
            ((ExpandCollapsePattern)menu.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            AutomationElement? choice = null;
            Wait(() => (choice = AutomationElement.RootElement.FindFirst(TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.ProcessIdProperty, process.Id), new PropertyCondition(AutomationElement.AutomationIdProperty, "cmd.choice")))) is not null);
            ((InvokePattern)choice!.GetCurrentPattern(InvokePattern.Pattern)).Invoke();
            Wait(() => window.Current.Name.Contains("Choice=1"));
            Check(((ExpandCollapsePattern)menu.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Current.ExpandCollapseState == ExpandCollapseState.Collapsed, "Invoking a menu action closes its popup");
            SetForegroundWindow(process.MainWindowHandle);
            Find(root!, "cmd.copy").SetFocus();
            WF.SendKeys.SendWait("{ENTER}");
            Wait(() => window.Current.Name.Contains("Copy=2"));
            Console.WriteLine("PASS: Enter activates exactly once");
            WF.SendKeys.SendWait("{F10}hc");
            Wait(() => window.Current.Name.Contains("Copy=3"));
            Console.WriteLine("PASS: F10 and tab/command KeyTips activate exactly once");
            WF.SendKeys.SendWait("{F6}");
            Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "document");
            WF.SendKeys.SendWait("{F6}");
            Wait(() => AutomationElement.FocusedElement.Current.AutomationId != "document");
            Console.WriteLine("PASS: F6 moves focus between content and ribbon");
            copy = Find(root!, "cmd.copy");
            copy.SetFocus();
            WF.SendKeys.SendWait(" ");
            Wait(() => window.Current.Name.Contains("Copy=4"));
            var rectangle = copy.Current.BoundingRectangle;
            Check(rectangle.Width > 0 && rectangle.Height > 0, "Action bounds are on screen");
            var oldCursor = WF.Cursor.Position;
            try
            {
                SetCursorPos((int)(rectangle.Left + rectangle.Width / 2), (int)(rectangle.Top + rectangle.Height / 2));
                MouseEvent(2, 0, 0, 0, 0);
                MouseEvent(4, 0, 0, 0, 0);
            }
            finally { WF.Cursor.Position = oldCursor; }
            Wait(() => window.Current.Name.Contains("Copy=5"));
            Console.WriteLine("PASS: Space and native mouse press each activate exactly once");
            var transform = (TransformPattern)window.GetCurrentPattern(TransformPattern.Pattern);
            transform.Move(window.Current.BoundingRectangle.Left, 50);
            transform.Resize(260, 600);
            AutomationElement? collapsed = null;
            Wait(() => (collapsed = Find(root!, "group.clipboard", false))?.Current.ControlType == ControlType.Button);
            ((ExpandCollapsePattern)collapsed!.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            var popupBold = FindProcessElement(process.Id, "cmd.bold")!;
            var popupToggle = (TogglePattern)popupBold.GetCurrentPattern(TogglePattern.Pattern);
            Check(popupToggle.Current.ToggleState == ToggleState.On, "Toggle state survives tab changes, resizing and collapse");
            popupToggle.Toggle();
            Wait(() => window.Current.Name.Contains("Bold=False"));
            ((ExpandCollapsePattern)collapsed.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            AutomationElement? innerMenu = null;
            Wait(() => (innerMenu = FindProcessElement(process.Id, "cmd.menu")) is not null);
            ((ExpandCollapsePattern)innerMenu!.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            Wait(() => FindProcessElement(process.Id, "cmd.choice") is not null);
            WF.SendKeys.SendWait("{ESC}");
            Wait(() => FindProcessElement(process.Id, "cmd.choice") is null);
            Check(((ExpandCollapsePattern)collapsed.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Current.ExpandCollapseState == ExpandCollapseState.Expanded, "Nested Escape leaves the group popup open");
            WF.SendKeys.SendWait("{ESC}");
            Wait(() => ((ExpandCollapsePattern)collapsed.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Current.ExpandCollapseState == ExpandCollapseState.Collapsed);
            Check(collapsed.Current.HasKeyboardFocus, "Keyboard dismissal returns focus to the collapsed opener");
            ((ExpandCollapsePattern)collapsed.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Expand();
            var document = Find(window, "document");
            rectangle = document.Current.BoundingRectangle;
            oldCursor = WF.Cursor.Position;
            try
            {
                SetCursorPos((int)rectangle.Left + 20, (int)rectangle.Bottom - 20);
                MouseEvent(2, 0, 0, 0, 0);
                MouseEvent(4, 0, 0, 0, 0);
                Wait(() => ((ExpandCollapsePattern)collapsed.GetCurrentPattern(ExpandCollapsePattern.Pattern)).Current.ExpandCollapseState == ExpandCollapseState.Collapsed);
                Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "document");
            }
            finally { WF.Cursor.Position = oldCursor; }
            Console.WriteLine("PASS: Outside press dismisses popup and retains clicked content focus");
            Console.WriteLine("PASS: external-process UIA discovery, Invoke, Toggle, disabled, selection, popup ExpandCollapse and command execution.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { if (!process.HasExited) { process.CloseMainWindow(); if (!process.WaitForExit(3000)) process.Kill(); } }
    }
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll", EntryPoint = "mouse_event")] private static extern void MouseEvent(uint flags, uint x, uint y, uint data, nuint extra);
    [DllImport("user32.dll", EntryPoint = "keybd_event")] private static extern void KeyEvent(byte key, byte scan, uint flags, nuint extra);
    private static void Keyboard(AutomationElement window, AutomationElement root, Process process)
    {
        SetForegroundWindow(process.MainWindowHandle);
        var document = Find(window, "document");
        document.SetFocus();
        KeyEvent(0x12, 0, 0, 0);
        KeyEvent(0x12, 0, 2, 0);
        WF.SendKeys.SendWait("hc");
        Wait(() => window.Current.Name.Contains("Copy=1"));
        Console.WriteLine("PASS: Alt press/release opens typeable tab and command KeyTips");
        Find(root, "tab.home").SetFocus();
        WF.SendKeys.SendWait("{RIGHT}");
        Wait(() => Find(root, "cmd.link", false) is not null);
        WF.SendKeys.SendWait("{LEFT}{DOWN}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.copy");
        WF.SendKeys.SendWait("{TAB}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.bold");
        WF.SendKeys.SendWait("{TAB}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.menu");
        WF.SendKeys.SendWait("+{TAB}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.bold");
        WF.SendKeys.SendWait("{HOME}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.copy");
        WF.SendKeys.SendWait("{END}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "cmd.menu");
        Console.WriteLine("PASS: Arrows, Home/End and Tab/Shift+Tab navigate and skip disabled commands");
        document.SetFocus();
        WF.SendKeys.SendWait("{F10}{F10}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "document");
        KeyEvent(0x12, 0, 0, 0);
        KeyEvent(0x12, 0, 2, 0);
        WF.SendKeys.SendWait("{ESC}");
        Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "document");
        Console.WriteLine("PASS: F10 toggle and Escape restore prior application content focus");
    }
    private static AutomationElement? FindProcessElement(int process, string id) => AutomationElement.RootElement.FindFirst(TreeScope.Descendants, new AndCondition(new PropertyCondition(AutomationElement.ProcessIdProperty, process), new PropertyCondition(AutomationElement.AutomationIdProperty, id)));
    private static AutomationElement Find(AutomationElement root, string id, bool required = true)
    {
        var element = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.AutomationIdProperty, id));
        if (required && element is null)
            throw new InvalidOperationException("Missing UIA element: " + id);
        return element!;
    }
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
        Console.WriteLine("PASS: " + message);
    }
    private static void Wait(Func<bool> condition)
    {
        var timer = Stopwatch.StartNew();
        while (timer.Elapsed < TimeSpan.FromSeconds(8))
        {
            if (condition())
                return;
            Thread.Sleep(50);
        }
        throw new TimeoutException("Native UIA state did not arrive.");
    }
    private static void Server()
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2);
        WF.Application.EnableVisualStyles();
        var form = new WF.Form { Text = "Native UIA fixture", Width = 1000, Height = 350, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl { Name = "native.ribbon" };
        int copies = 0, choices = 0;
        bool checkedState = false;
        void UpdateTitle() => form.Text = $"Copy={copies}; Bold={checkedState}; Choice={choices}";
        var home = new RibbonTab { Id = "tab.home", Text = "Home", KeyTip = "H" };
        var group = new RibbonGroup { Id = "group.clipboard", Text = "Clipboard" };
        var copy = new RibbonButtonItem { Id = "cmd.copy", Text = "Copy" };
        copy.Click += (_, _) => { copies++; UpdateTitle(); };
        var bold = new RibbonToggleItem { Id = "cmd.bold", Text = "Bold" };
        bold.CheckedChanged += (_, _) => { checkedState = bold.Checked; UpdateTitle(); };
        var disabled = new RibbonButtonItem { Id = "cmd.disabled", Text = "Disabled", Enabled = false };
        var menu = new RibbonDropDownItem { Id = "cmd.menu", Text = "Menu" };
        var choice = new RibbonButtonItem { Id = "cmd.choice", Text = "Choice" };
        choice.Click += (_, _) => { choices++; UpdateTitle(); };
        menu.Items.Add(choice);
        group.Items.AddRange(copy, bold, disabled, menu);
        home.Groups.Add(group);
        var insert = new RibbonTab { Id = "tab.insert", Text = "Insert", KeyTip = "N" };
        var links = new RibbonGroup { Id = "group.links", Text = "Links" };
        links.Items.Add(new RibbonButtonItem { Id = "cmd.link", Text = "Link" });
        insert.Groups.Add(links);
        ribbon.Tabs.AddRange(home, insert);
        form.Controls.Add(new WF.TextBox { Name = "document", Dock = WF.DockStyle.Fill, Multiline = true, Text = "Document focus target" });
        form.Controls.Add(ribbon);
        WF.Form? otherWindow = null;
        var openOther = new WF.Button { Name = "other.window", Text = "Open another window", Dock = WF.DockStyle.Bottom, Height = 28, TabStop = false };
        openOther.Click += (_, _) =>
        {
            if (otherWindow is null || otherWindow.IsDisposed)
                otherWindow = new WF.Form { Name = "native.other", Text = "Another window in the ribbon process", Width = 400, Height = 250 };
            otherWindow.Show();
            otherWindow.Activate();
        };
        form.Controls.Add(openOther);
        form.FormClosed += (_, _) => otherWindow?.Dispose();
        UpdateTitle();
        WF.Application.Run(form);
    }
}
