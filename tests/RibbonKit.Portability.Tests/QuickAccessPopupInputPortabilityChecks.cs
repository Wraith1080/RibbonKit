using System.Runtime.InteropServices;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Portability.Tests;

// Opt-in native pointer checks in a consumer with no Showcase resources/helpers.
// Requires an unlocked desktop and idle user input; not run by the full aggregate.
internal static class QuickAccessPopupInputPortabilityChecks
{
    internal static void Verify(Application application)
    {
        GetCursorPos(out var cursor); var foreground = GetForegroundWindow();
        var motion = RibbonAnimation.GlobalLevel; RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        try
        {
            foreach (var theme in new[] { RibbonTheme.Office2007, RibbonTheme.Office2024, RibbonTheme.CrystalLight })
            foreach (var position in new[] { RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon })
            foreach (string kind in new[] { "group", "combo", "gallery", "dropdown" })
            {
                string? selectedCase = Environment.GetEnvironmentVariable("RIBBONKIT_QAT_INPUT_CASE");
                if (!string.IsNullOrEmpty(selectedCase) && selectedCase != $"{theme}/{position}/{kind}") continue;
                VerifyCase(application, theme, position, kind);
            }
        }
        finally
        {
            mouse_event(4 | 16, 0, 0, 0, UIntPtr.Zero);
            SetCursorPos(cursor.X, cursor.Y); SetForegroundWindow(foreground);
            RibbonAnimation.GlobalLevel = motion;
        }
    }

    private static void VerifyCase(Application application, RibbonTheme theme, RibbonQuickAccessPosition position, string kind)
    {
        ThemeManager.Apply(application, theme); ThemeManager.SetDarkMode(application, false);
        int invoked = 0, insideInvoked = 0;
        var target = new RibbonButton { Header = "Run", Command = new ActionCommand(() => invoked++) };
        var inside = new RibbonButton { Header = "Inside", Command = new ActionCommand(() => insideInvoked++) };
        var group = new RibbonGroup { Header = "Tools", Items = { inside } };
        var firstEditor = new RibbonComboBox { IsEditable = true, InputWidth = 130, Items = { "Calibri", "Georgia" }, SelectedIndex = 0 };
        var secondPicker = new RibbonComboBox { InputWidth = 70, Items = { "11", "9" }, SelectedIndex = 0 };
        var toggle = new RibbonToggleButton { Header = "Bold", Size = RibbonControlSize.Small,
            Icon = new DrawingImage(new GeometryDrawing(Brushes.Black, null, Geometry.Parse("M2,1 L8,1 C14,1 14,8 9,8 C15,8 15,16 8,16 L2,16 Z M5,3 L5,7 L8,7 C11,7 11,3 8,3 Z M5,10 L5,14 L8,14 C12,14 12,10 8,10 Z"))) };
        if (kind == "group")
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { firstEditor, secondPicker } };
            group.Items.Insert(0, new StackPanel { Children = { row, toggle } });
        }
        FrameworkElement source = kind switch
        {
            "group" => group,
            "combo" => new RibbonComboBox { Header = "Choice", InputWidth = 120, Items = { "One", "Two" } },
            "gallery" => new InRibbonGallery { Header = "Gallery", Width = 180, Items = { new RibbonGalleryItem { Content = "Tile" } } },
            _ => new RibbonDropDownButton { Header = "Menu", Items = { new RibbonMenuItem { Header = "Action" } } },
        };
        if (!ReferenceEquals(source, group)) group.Items.Add(source);
        var home = new RibbonTab { Header = "Home", Groups = {
            new RibbonGroup { Header = "Space", Items = { new Border { Width = 280, Height = 35 } } },
            group, new RibbonGroup { Header = "Commands", Items = { target } } } };
        var other = new RibbonTab { Header = "Other", Groups = { new RibbonGroup { Header = "Other tools" } } };
        var ribbon = new Ribbon { Tabs = { home, other }, QuickAccessPosition = position,
            Density = theme == RibbonTheme.Office2024 ? RibbonDensity.Touch : RibbonDensity.Compact,
            FlowDirection = theme == RibbonTheme.Office2024 ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            ApplicationMenu = new RibbonApplicationMenu { Items = { new RibbonApplicationMenuItem { Header = "Save" } } } };
        ribbon.AddToQuickAccess(source);
        var window = new Window { Content = ribbon, Width = 1050, Height = 530, Left = 60, Top = 80, ShowInTaskbar = false };
        try
        {
            window.Show(); window.Activate(); Pump(window);
            var copy = (RibbonDropDownButton)ribbon.QuickAccessItems[0];
            var tabs = Part<RibbonTabControl>(ribbon, "TabControlHost");
            var file = Part<ButtonBase>(tabs, "PART_ApplicationButton");
            if (kind == "group")
            {
                int firstVisits = 0; KeyboardFocusChangedEventHandler onFocus = (_, _) => firstVisits++;
                firstEditor.GotKeyboardFocus += onFocus;
                Click(Center(Part<FrameworkElement>(copy, "PART_Toggle")), window);
                Assert.True(copy.IsDropDownOpen); Assert.Equal(0, firstVisits);
                var preview = Part<Decorator>(group, "PART_NormalHost").Child as FrameworkElement;
                Assert.NotNull(preview); var before = Pixels(preview, $"{theme}-{position}-before");
                Click(Center(secondPicker), window); Assert.True(secondPicker.IsDropDownOpen);
                Click(Center((FrameworkElement)secondPicker.ItemContainerGenerator.ContainerFromIndex(1)), window);
                Assert.Equal(1, secondPicker.SelectedIndex); Assert.True(copy.IsDropDownOpen); Assert.Equal(0, firstVisits);
                var selected = Pixels(preview, $"{theme}-{position}-selected");
                Assert.False(before.SequenceEqual(selected), $"{theme}/{position}: source preview must follow the second picker's value.");
                firstEditor.GotKeyboardFocus -= onFocus;
                copy.IsDropDownOpen = false; Pump(window);
            }
            Open(); Move(Center(target)); Pump(window);
            Assert.Equal(0, Part<FrameworkElement>(target, "HoverWash").Opacity);
            // Shared File/orb and tab chrome also retain their nonhover paint.
            var header = Part<Border>(other, "HeaderChrome");
            var beforeHeader = header.Background;
            Move(Center(header)); Pump(window); Assert.Same(beforeHeader, header.Background);
            Click(Center(target), window); Assert.Equal(1, invoked); Assert.False(copy.IsDropDownOpen);
            // The original group preview resolves its command after restoration.
            if (kind == "group")
            {
                var point = Center(inside); Open(); Click(point, window);
                Assert.Equal(1, insideInvoked); Assert.False(copy.IsDropDownOpen);
                var captionPoint = Center(Part<FrameworkElement>(group, "GroupCaption"));
                Open(); RightClick(captionPoint, window); Assert.False(copy.IsDropDownOpen);
                var menu = PresentationSource.CurrentSources.Cast<PresentationSource>()
                    .SelectMany(p => Descendants(p.RootVisual)).OfType<ContextMenu>()
                    .Single(m => m.IsOpen && ReferenceEquals(m.PlacementTarget, group));
                Assert.NotNull(menu.Style); Assert.Equal(ribbon.FlowDirection, menu.FlowDirection);
                Assert.False(((MenuItem)menu.Items[0]).IsEnabled); // Already in QAT.
                menu.IsOpen = false; Pump(window);
            }
            Open(); Click(Center(file), window);
            Assert.True(ribbon.IsBackstageOpen, $"{theme}/{kind}: File/orb must open on the first click.");
            Assert.False(copy.IsDropDownOpen); ribbon.IsBackstageOpen = false; Pump(window);
            Open(); Click(Center(Part<FrameworkElement>(other, "HeaderChrome")), window);
            Assert.Same(other, ribbon.SelectedTab); Assert.False(copy.IsDropDownOpen);
            void Open() { copy.IsDropDownOpen = true; Pump(window); Assert.True(copy.IsDropDownOpen); }
        }
        finally { window.Close(); Pump(window); }
    }

    private static Point Center(FrameworkElement element) => element.PointToScreen(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
    private static void Move(Point point) => Assert.True(SetCursorPos((int)Math.Round(point.X), (int)Math.Round(point.Y)));
    private static void Click(Point point, Window window)
    {
        Move(point); Pump(window); mouse_event(2, 0, 0, 0, UIntPtr.Zero); Pump(window);
        mouse_event(4, 0, 0, 0, UIntPtr.Zero); Pump(window);
    }
    private static void RightClick(Point point, Window window)
    {
        Move(point); Pump(window); mouse_event(8, 0, 0, 0, UIntPtr.Zero); Pump(window);
        mouse_event(16, 0, 0, 0, UIntPtr.Zero); Pump(window);
    }
    private static void Pump(Window window)
    {
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(35) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame); window.UpdateLayout();
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    }
    private static T Part<T>(Control owner, string name) where T : DependencyObject => Assert.IsAssignableFrom<T>(owner.Template.FindName(name, owner));
    private static byte[] Pixels(FrameworkElement element, string name)
    {
        var dpi = VisualTreeHelper.GetDpi(element);
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(element.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(element); byte[] pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        var directory = Environment.GetEnvironmentVariable("RIBBONKIT_GROUP_QAT_DIAGNOSTICS");
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory); var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var stream = File.Create(Path.Combine(directory, name + ".png")); encoder.Save(stream);
            var window = Window.GetWindow(element);
            var full = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth * dpi.DpiScaleX), (int)Math.Ceiling(window.ActualHeight * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            full.Render(window); var fullEncoder = new PngBitmapEncoder(); fullEncoder.Frames.Add(BitmapFrame.Create(full));
            using var fullStream = File.Create(Path.Combine(directory, name + "-window.png")); fullEncoder.Save(fullStream);
        }
        return pixels;
    }
    private static IEnumerable<DependencyObject> Descendants(DependencyObject? root)
    {
        if (root is not Visual) yield break;
        yield return root;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Descendants(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
    private sealed class ActionCommand(Action action) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => action();
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }
    [StructLayout(LayoutKind.Sequential)] private struct Cursor { internal int X; internal int Y; }
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Cursor point);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
}
