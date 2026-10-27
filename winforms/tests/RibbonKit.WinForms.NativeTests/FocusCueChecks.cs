using System.Diagnostics;
using System.IO;
using System.Windows.Automation;
using WF = System.Windows.Forms;

namespace RibbonKit.WinForms.NativeTests;

internal static partial class Program
{
    private static void FocusCues(AutomationElement window, AutomationElement root, Process process, string directory)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2);
        Directory.CreateDirectory(directory);
        var cursor = WF.Cursor.Position;
        try
        {
            SetForegroundWindow(process.MainWindowHandle);
            Wait(() => GetForegroundWindow() == process.MainWindowHandle);
            var copy = Find(root, "cmd.copy");
            Click(copy);
            Wait(() => window.Current.Name.Contains("Copy=1"));
            Check(copy.Current.HasKeyboardFocus, "Mouse click retains actual UIA keyboard focus");
            Cue(copy, false, "strip-mouse");
            WF.SendKeys.SendWait("{HOME}");
            Cue(copy, true, "strip-keyboard");
            var disabled = Find(root, "cmd.disabled").Current.BoundingRectangle;
            SetCursorPos((int)(disabled.Left + disabled.Width / 2), (int)(disabled.Top + disabled.Height / 2));
            Cue(copy, true, "strip-pointer-moved");
            Click(copy);
            Wait(() => window.Current.Name.Contains("Copy=2"));
            Cue(copy, false, "strip-mouse-again");
            copy.SetFocus();
            Cue(copy, true, "strip-assistive-focus");

            var document = Find(window, "document");
            Click(document);
            Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "document");
            WF.SendKeys.SendWait("+{TAB}");
            Wait(() => AutomationElement.FocusedElement.Current.AutomationId != "document");
            Cue(copy, true, "strip-tab-entry");
            Click(document, true);
            Wait(() => AutomationElement.FocusedElement.Current.AutomationId == "document");
            WF.SendKeys.SendWait("{F6}");
            Wait(() => copy.Current.HasKeyboardFocus);
            Cue(copy, true, "strip-f6-entry");

            var menu = Find(root, "cmd.menu");
            Click(menu);
            Wait(() => FindProcessElement(process.Id, "cmd.choice") is not null);
            var choice = FindProcessElement(process.Id, "cmd.choice")!;
            Cue(choice, false, "popup-mouse");
            WF.SendKeys.SendWait("{DOWN}");
            Cue(choice, true, "popup-keyboard");
            WF.SendKeys.SendWait("{ESC}");
            Wait(() => FindProcessElement(process.Id, "cmd.choice") is null);
            Cue(menu, true, "opener-keyboard-return");

            var transform = (TransformPattern)window.GetCurrentPattern(TransformPattern.Pattern);
            transform.Resize(260, 600);
            AutomationElement? group = null;
            Wait(() => (group = Find(root, "group.clipboard", false))?.Current.ControlType == ControlType.Button);
            Click(group!);
            Wait(() => FindProcessElement(process.Id, "cmd.copy") is not null);
            Cue(FindProcessElement(process.Id, "cmd.copy")!, false, "collapsed-mouse");
            WF.SendKeys.SendWait("{END}{RIGHT}");
            Wait(() => FindProcessElement(process.Id, "cmd.choice") is not null);
            Cue(FindProcessElement(process.Id, "cmd.choice")!, true, "nested-keyboard");
            WF.SendKeys.SendWait("{ESC}");
            Wait(() => FindProcessElement(process.Id, "cmd.choice") is null);
            Cue(FindProcessElement(process.Id, "cmd.menu")!, true, "parent-keyboard-return");
            WF.SendKeys.SendWait("{ESC}");
            Cue(group!, true, "group-keyboard-return");
            Console.WriteLine($"PASS: native focus painting follows mouse/keyboard input across strip, popup and nested returns; captures: {directory}");
        }
        finally { WF.Cursor.Position = cursor; }

        void Cue(AutomationElement element, bool expected, string name)
        {
            try
            {
                Wait(() =>
                {
                    using var bitmap = Capture(element);
                    return HasOutline(bitmap) == expected;
                });
            }
            catch
            {
                using var failed = Capture(element);
                failed.Save(Path.Combine(directory, name + "-failed.png"));
                Console.Error.WriteLine($"Focus cue failure: {name}, UIA focus={AutomationElement.FocusedElement.Current.AutomationId}, foreground={GetForegroundWindow()}, targetFocus={element.Current.HasKeyboardFocus}");
                throw;
            }
            using var capture = Capture(element);
            Check(HasOutline(capture) == expected, name + (expected ? " has keyboard outline" : " has no keyboard outline"));
            capture.Save(Path.Combine(directory, name + ".png"));
        }
        static Bitmap Capture(AutomationElement element)
        {
            var bounds = element.Current.BoundingRectangle;
            var bitmap = new Bitmap((int)bounds.Width, (int)bounds.Height);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.CopyFromScreen((int)bounds.Left, (int)bounds.Top, 0, 0, bitmap.Size);
            return bitmap;
        }
        static bool HasOutline(Bitmap bitmap)
        {
            // The default renderer insets focus by two device pixels. Inspect
            // vertical edges away from label/icon content and rounded corners.
            int accent = RibbonThemes.Office2024Light.Accent.ToArgb(), count = 0;
            for (int y = 8; y < bitmap.Height - 8; y++)
                if (bitmap.GetPixel(2, y).ToArgb() == accent || bitmap.GetPixel(bitmap.Width - 3, y).ToArgb() == accent)
                    count++;
            return count > 3;
        }
        static void Click(AutomationElement element, bool nearBottom = false)
        {
            var bounds = element.Current.BoundingRectangle;
            // Use a different document point after Shift+Tab, so rapid fixture
            // input does not become a native edit-control double-click.
            SetCursorPos((int)(nearBottom ? bounds.Left + 20 : bounds.Left + bounds.Width / 2),
                (int)(nearBottom ? bounds.Bottom - 20 : bounds.Top + bounds.Height / 2));
            MouseEvent(2, 0, 0, 0, 0);
            MouseEvent(4, 0, 0, 0, 0);
        }
    }
}
