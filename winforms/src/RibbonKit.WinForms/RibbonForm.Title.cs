namespace RibbonKit.WinForms;

public partial class RibbonForm
{
    private RibbonCaptionTitleMotion? _captionTitleMotion;
    private Rectangle? _captionTitleOverride, _normalCaptionTitle;
    private bool _captionTitleClosing;
    internal RibbonCaptionTitleMotion? CaptionTitleMotion => _captionTitleMotion;
    internal bool IsCaptionTitleAnimating => _captionTitleMotion?.IsRunning == true;
    internal Rectangle CaptionTitleDrawingBounds(RibbonControl ribbon) => _captionTitleMotion?.Current ?? _captionTitleOverride ?? CaptionTitleTarget(ribbon);
    private Rectangle CaptionTitleTarget(RibbonControl ribbon)
    {
        var box = CaptionTextBounds(ribbon);
        int width = Math.Min(box.Width, TextRenderer.MeasureText(Text, Font, Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width);
        int left = ribbon.IsBackstageOpen && ribbon.Theme.BackstageTitleCentered ? box.Left + (box.Width - width) / 2 : box.Left;
        return new(left, box.Top, width, box.Height);
    }
    internal void BeginBackstageTitle(RibbonControl ribbon, bool opening, bool allowMotion = true)
    {
        if (!ribbon.HasWindowCaption || !ribbon.Theme.BackstageTitleCentered) { SnapCaptionTitle(); return; }
        var from = CaptionTitleDrawingBounds(ribbon);
        if (opening && !ribbon.IsBackstageOpen) _normalCaptionTitle = CaptionTitleTarget(ribbon);
        _captionTitleClosing = !opening;
        // Called before changing visibility, so measure the requested target
        // explicitly. The final normal layout is retargeted after QAT returns.
        var box = opening && ribbon.Theme.BackstageTitleCentered ? CenteredCaptionTextBounds() : CaptionTextBounds(ribbon);
        int width = Math.Min(box.Width, TextRenderer.MeasureText(Text, Font, Size.Empty,
            TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width);
        var target = opening && ribbon.Theme.BackstageTitleCentered
            ? new Rectangle((ClientSize.Width - width) / 2, 0, width, CaptionHeight)
            : opening ? new Rectangle(Caption.ContentLeft, 0, width, CaptionHeight)
                : _normalCaptionTitle ?? new Rectangle(Caption.ContentLeft, 0, width, CaptionHeight);
        int left = Math.Clamp(target.Left, Math.Min(Caption.ContentLeft, Caption.Buttons.Left), Caption.Buttons.Left);
        target = new(left, 0, Math.Min(target.Width, Math.Max(0, Caption.Buttons.Left - left)), CaptionHeight);
        // Match Backstage's short arrival/departure slide, without fading ink.
        // Place most of the distance immediately, then ease the remaining travel.
        double travel = ribbon.Theme.BackstageTitleSlideDistanceRatio ?? .25;
        int Start(int a, int b) => (int)Math.Round(b + (a - b) * travel);
        from = new(Start(from.X, target.X), Start(from.Y, target.Y), Start(from.Width, target.Width), Start(from.Height, target.Height));
        StartCaptionTitle(ribbon, from, target, allowMotion);
    }
    internal void RetargetClosedTitle(RibbonControl ribbon, bool allowMotion = true)
    {
        if (!ribbon.Theme.BackstageTitleCentered) { SnapCaptionTitle(); return; }
        var from = CaptionTitleDrawingBounds(ribbon);
        _captionTitleClosing = false;
        StartCaptionTitle(ribbon, from, CaptionTitleTarget(ribbon), allowMotion);
    }
    private void StartCaptionTitle(RibbonControl ribbon, Rectangle from, Rectangle target, bool allowMotion)
    {
        _captionTitleMotion?.Dispose(); _captionTitleMotion = null;
        bool animate = allowMotion && from != target && ribbon.AnimationLevel != RibbonAnimationLevel.None && ribbon.Motion.SystemEnabled
            && ribbon.CanAnimatePopupOpening && Visible && WindowState != FormWindowState.Minimized && !IsDisposed && !Disposing;
        _captionTitleOverride = animate || _captionTitleClosing ? from : null;
        if (animate) _captionTitleMotion = new(this, ribbon, from, target);
        else if (_captionTitleClosing) _captionTitleOverride = target;
    }
    internal void CompleteCaptionTitleMotion(RibbonCaptionTitleMotion motion)
    {
        if (!ReferenceEquals(_captionTitleMotion, motion)) return;
        _captionTitleOverride = _captionTitleClosing ? motion.Current : null;
        _captionTitleMotion = null; motion.Dispose(); InvalidateCaption();
    }
    internal void SnapCaptionTitle()
    {
        _captionTitleMotion?.Dispose(); _captionTitleMotion = null;
        _captionTitleOverride = null; _captionTitleClosing = false;
        if (!IsDisposed && !Disposing && IsHandleCreated) InvalidateCaption();
    }
}
