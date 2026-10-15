using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace RibbonKit.Controls;

// Presentation/placement policy for command copies inside either overflow menu.
// Original controls and strip copies do not acquire menu-row presentation.
internal static class RibbonOverflowMenu
{
    public static readonly DependencyProperty IsEntryProperty = DependencyProperty.RegisterAttached(
        "IsEntry", typeof(bool), typeof(RibbonOverflowMenu), new PropertyMetadata(false));
    public static readonly PropertyPath IsEntryPath = new("(0)", IsEntryProperty);
    internal static readonly DependencyProperty ParentSurfaceProperty = DependencyProperty.RegisterAttached(
        "ParentSurface", typeof(FrameworkElement), typeof(RibbonOverflowMenu), new PropertyMetadata(null));
    internal static bool IsEntry(DependencyObject element) => (bool)element.GetValue(IsEntryProperty);
    internal static Key OpenKey(FrameworkElement entry) => entry.FlowDirection == FlowDirection.RightToLeft ? Key.Left : Key.Right;
    internal static Key ReturnKey(FrameworkElement entry) => entry.FlowDirection == FlowDirection.RightToLeft ? Key.Right : Key.Left;

    internal static void ConfigurePopup(FrameworkElement entry, Popup popup, FrameworkElement? chrome = null)
    {
        popup.SetCurrentValue(Popup.PlacementTargetProperty, entry);
        popup.SetCurrentValue(Popup.CustomPopupPlacementCallbackProperty,
            new CustomPopupPlacementCallback((popupSize, _, offset) => Place(entry, popup, chrome, popupSize, offset)));
        popup.SetCurrentValue(Popup.PlacementProperty, PlacementMode.Custom);
    }

    private static CustomPopupPlacement[] Place(FrameworkElement entry, Popup popup, FrameworkElement? paintedSurface, Size popupSize, Point offset)
    {
        // WPF supplies callback sizes/positions in physical screen pixels. Using
        // PointToScreen also accounts for inherited RTL and the target's DPI.
        // The callback size excludes the child's outer margin. Use the measured
        // frame for edge checks, but join the visible chrome to the parent menu.
        // Transparent shadow allocations may overlap; painted surfaces may not.
        var inset = new Thickness();
        if (popup.Child is { } child && PresentationSource.FromVisual(child)?.RootVisual is FrameworkElement frame
            && frame.ActualWidth > 0 && frame.ActualHeight > 0)
        {
            Rect outer = ScreenBounds(frame);
            popupSize = outer.Size;
            FrameworkElement? surface = paintedSurface ?? child as FrameworkElement;
            if (surface is { ActualWidth: > 0, ActualHeight: > 0 })
            {
                Rect chrome = LayoutScreenBounds(surface);
                chrome.Intersect(outer);
                inset = new Thickness(chrome.Left - outer.Left, chrome.Top - outer.Top,
                    outer.Right - chrome.Right, outer.Bottom - chrome.Bottom);
            }
        }
        FrameworkElement? parentSurface = entry.GetValue(ParentSurfaceProperty) as FrameworkElement;
        bool hasParentSurface = parentSurface is { ActualWidth: > 0 } && parentSurface.IsAncestorOf(entry);
        Point TargetPoint(Point point) => hasParentSurface
            ? EntryLayoutScreenPoint(entry, parentSurface!, point)
            : entry.PointToScreen(point);
        Point origin = TargetPoint(offset);
        Point opposite = TargetPoint(new Point(entry.ActualWidth + offset.X, entry.ActualHeight + offset.Y));
        var target = new Rect(origin, opposite);
        Rect parent = hasParentSurface ? LayoutScreenBounds(parentSurface!) : target;
        var anchor = new Rect(parent.Left, target.Top, parent.Width, target.Height);
        Rect workArea = MonitorWorkArea(target);
        PlacementMode side = SelectSide(anchor, popupSize.Width, workArea, entry.FlowDirection, inset);
        double x = side switch
        {
            PlacementMode.Right => anchor.Right - inset.Left,
            PlacementMode.Left => anchor.Left - popupSize.Width + inset.Right,
            _ => entry.FlowDirection == FlowDirection.RightToLeft ? target.Right - popupSize.Width : target.Left,
        };
        double y = side == PlacementMode.Bottom ? target.Bottom : target.Top - inset.Top;
        if (!workArea.IsEmpty)
            y = Math.Max(workArea.Top, Math.Min(y, workArea.Bottom - popupSize.Height));
        // WPF adds its current target origin, including any still-running parent
        // flyout motion, to this result. Cancel that origin rather than moving the
        // desired final chrome position with the animation.
        Point currentOrigin = entry.PointToScreen(offset);
        return new[] { new CustomPopupPlacement(new Point(x - currentOrigin.X, y - currentOrigin.Y), PopupPrimaryAxis.None) };
    }

    internal static PlacementMode SelectSide(Rect target, double popupWidth, Rect workArea, FlowDirection direction, Thickness inset = default)
    {
        if (workArea.IsEmpty) return PlacementMode.Bottom;
        double right = target.Right - inset.Left, left = target.Left - popupWidth + inset.Right;
        bool rightFits = right + popupWidth <= workArea.Right && right >= workArea.Left;
        bool leftFits = left >= workArea.Left && left + popupWidth <= workArea.Right;
        if (direction == FlowDirection.RightToLeft)
            return leftFits ? PlacementMode.Left : rightFits ? PlacementMode.Right : PlacementMode.Bottom;
        return rightFits ? PlacementMode.Right : leftFits ? PlacementMode.Left : PlacementMode.Bottom;
    }

    private static Rect ScreenBounds(FrameworkElement element) => new(element.PointToScreen(new Point()),
        element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight)));

    // Flyout motion moves content through render transforms, never its arranged
    // slot. Position against the final slot, including after native group loading.
    private static Point EntryLayoutScreenPoint(FrameworkElement entry, FrameworkElement surface, Point point)
    {
        Visual node = entry;
        while (!ReferenceEquals(node, surface))
        {
            point += VisualTreeHelper.GetOffset(node);
            if (VisualTreeHelper.GetParent(node) is not Visual parent) return entry.PointToScreen(point);
            node = parent;
        }
        return LayoutScreenPoint(surface, point);
    }

    private static Point LayoutScreenPoint(FrameworkElement element, Point point)
    {
        if (VisualTreeHelper.GetParent(element) is Visual parent
            && PresentationSource.FromVisual(element)?.RootVisual is Visual root)
        {
            Vector offset = VisualTreeHelper.GetOffset(element);
            return root.PointToScreen(parent.TransformToAncestor(root).Transform(point + offset));
        }
        return element.PointToScreen(point);
    }

    private static Rect LayoutScreenBounds(FrameworkElement element) => new(LayoutScreenPoint(element, new Point()),
        LayoutScreenPoint(element, new Point(element.ActualWidth, element.ActualHeight)));

    private static Rect MonitorWorkArea(Rect target)
    {
        var bounds = new NativeRect { Left = (int)Math.Floor(target.Left), Top = (int)Math.Floor(target.Top),
            Right = (int)Math.Ceiling(target.Right), Bottom = (int)Math.Ceiling(target.Bottom) };
        IntPtr monitor = MonitorFromRect(ref bounds, 2); // MONITOR_DEFAULTTONEAREST
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) return Rect.Empty;
        return new Rect(info.Work.Left, info.Work.Top, info.Work.Right - info.Work.Left, info.Work.Bottom - info.Work.Top);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref NativeRect rect, int flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo { public int Size; public NativeRect Monitor, Work; public int Flags; }
}
