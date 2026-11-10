using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;

namespace RibbonKit.WinForms;

internal sealed partial class RibbonPopupSurface
{
    private Control? _customContent;
    private Size _contentMaximum;
    private DockStyle _savedContentDock;
    private AnchorStyles _savedContentAnchor;
    private Point _savedContentLocation;
    private bool _savedContentVisible;
    internal bool HasCustomContent => _customContent is not null;

    private Size InitializeContent(Control content, Size workArea)
    {
        if (content.IsDisposed || content.Parent is not null) throw new InvalidOperationException("Popup content must remain live and unparented while closed.");
        _contentMaximum = new(Math.Max(1, workArea.Width - RibbonLayoutEngine.Scale(24, _ribbon.DeviceDpi)), Math.Max(1, workArea.Height - RibbonLayoutEngine.Scale(32, _ribbon.DeviceDpi)));
        _savedContentDock = content.Dock; _savedContentAnchor = content.Anchor;
        _savedContentLocation = content.Location; _savedContentVisible = content.Visible;
        _customContent = content;
        AutoScroll = false;
        try
        {
            // Create/autoscale the body without forcing this surface's HWND.
            // The surface must first acquire its native parent from the popup
            // host, otherwise Windows rejects its opening drawing hold.
            if (content.BindingContext is { } context) content.BindingContext = context;
            content.Dock = DockStyle.None; content.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            Controls.Add(content); content.Location = new(_contentPadding, _contentPadding); content.Visible = true;
            content.CreateControl();
            content.SizeChanged += ContentSizeChanged;
            return ContentSurfaceSize();
        }
        catch { DetachContent(); throw; }
    }
    private Size ContentSurfaceSize() => _customContent is { } body
        ? new(Math.Clamp(body.Width + 2 * _contentPadding, 1, _contentMaximum.Width), Math.Clamp(body.Height + 2 * _contentPadding, 1, _contentMaximum.Height)) : Size;
    private void ContentSizeChanged(object? sender, EventArgs e) { if (!IsDisposed && HasCustomContent) Size = ContentSurfaceSize(); }
    internal bool OwnsContentInput(Control? origin) => _customContent is { } body && origin is not null && (ReferenceEquals(body, origin) || body.Contains(origin));
    internal bool HasContentComboList => _customContent is { } body && Descendants(body).OfType<ComboBox>().Any(combo => combo.DroppedDown);
    internal bool ContainsContentListPoint(Point point)
    {
        if (_customContent is not { } body) return false;
        foreach (var combo in Descendants(body).OfType<ComboBox>().Where(combo => combo.IsHandleCreated && combo.DroppedDown))
        {
            var info = new ComboInfo { Size = Marshal.SizeOf<ComboInfo>() };
            if (GetComboBoxInfo(combo.Handle, ref info) && GetWindowRect(info.List, out var bounds)
                && Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom).Contains(point)) return true;
        }
        return false;
    }
    internal bool ProcessContentKey(Keys keys)
    {
        if (ProcessApplicationContentKey(keys)) return true;
        if (_customContent is not { } body || keys is not (Keys.Tab or (Keys.Tab | Keys.Shift))) return false;
        var focused = Descendants(body).FirstOrDefault(control => control.Focused);
        body.SelectNextControl(focused, keys == Keys.Tab, true, true, true);
        return true;
    }
    private void FocusContent()
    {
        if (_customContent is not { } body) return;
        if (!body.SelectNextControl(null, true, true, true, false)) body.Focus();
    }
    internal void DetachContent()
    {
        if (_customContent is not { } body) return;
        _customContent = null;
        body.SizeChanged -= ContentSizeChanged;
        if (IsApplicationMenu) DetachApplicationContent(body);
        if (ReferenceEquals(body.Parent, this)) Controls.Remove(body);
        if (!body.IsDisposed)
        {
            body.Dock = _savedContentDock; body.Anchor = _savedContentAnchor;
            body.Location = _savedContentLocation; body.Visible = _savedContentVisible;
        }
    }
    protected override AccessibleObject CreateAccessibilityInstance() => IsApplicationMenu ? new ApplicationMenuAccessibleObject(this) : HasCustomContent ? new ContentAccessibleObject(this) : base.CreateAccessibilityInstance();
    private sealed class ContentAccessibleObject(RibbonPopupSurface owner) : ControlAccessibleObject(owner)
    {
        public override AccessibleRole Role => AccessibleRole.Pane;
        public override string? Name { get => owner.AccessibleName ?? owner.MenuSource?.Text; set => owner.AccessibleName = value; }
        public override int GetChildCount() => owner._customContent is null ? 0 : 1;
        public override AccessibleObject? GetChild(int index) => index == 0 ? owner._customContent?.AccessibilityObject : null;
    }
    private static IEnumerable<Control> Descendants(Control parent)
    {
        yield return parent;
        foreach (Control child in parent.Controls)
            foreach (var descendant in Descendants(child)) yield return descendant;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct ComboInfo { internal int Size; internal NativeRect ItemBounds, ButtonBounds; internal int State; internal nint Combo, Edit, List; }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetComboBoxInfo(nint window, ref ComboInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetWindowRect(nint window, out NativeRect bounds);
}
