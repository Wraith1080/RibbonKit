namespace RibbonKit.WinForms.Layout;

internal sealed class RibbonBackstage2007Layout : RibbonBackstageLayoutProvider
{
    internal override RibbonBackstageLayoutDefaults Defaults { get; } = new(
        CompactRowHeight: 42, TouchRowHeight: 52, NavigationPadding: 4,
        CompactBackSize: 36, TouchBackSize: 44, BackInset: 0, BackGap: 0,
        IconTextGap: 12, IconInset: 12, IconSize: 24, AutoMinimumWidth: 220,
        FooterBottomPadding: null, PagePadding: 16, RowTextVerticalPadding: 16,
        SeparatorHeight: 9, MinimumPageWidth: 240, TrailingPadding: 8, ScrollbarWidth: 18,
        BackButtonVisible: false);

    internal override RibbonBackstageLayout Build(Size size, in RibbonBackstageLayoutInput input)
    {
        int seam = Math.Clamp(RibbonLayoutEngine.Scale(input.Metrics.TopSeamHeight, input.DeviceDpi), 0, size.Height);
        var geometry = RibbonBackstageWorkspaceGeometry.Measure(new(0, seam, size.Width, size.Height - seam), input.DeviceDpi, input.Metrics);
        var content = geometry.Interior;
        var measured = base.Build(content.Size, input with { CaptionHeight = 0, PageShadowInset = 0, Metrics = input.Metrics with { BackButtonVisible = false, TopSeamHeight = 0 } });
        Rectangle Move(Rectangle r) => new(r.Left + content.Left, r.Top + content.Top, r.Width, r.Height);
        var navigation = Move(measured.Navigation);
        int top = Math.Min(measured.Inset, navigation.Height);
        navigation = new(navigation.Left, navigation.Top + top, Math.Max(0, navigation.Width - geometry.Border), navigation.Height - top);
        var footer = measured.Footer.Height > 0 ? Move(measured.Footer) : Rectangle.Empty;
        if (!footer.IsEmpty) footer.Width = navigation.Width;
        int backWidth = Math.Min(geometry.Header.Width, RibbonLayoutEngine.Scale(input.Metrics.WorkspaceReturnWidth, input.DeviceDpi));
        return measured with
        {
            Back = input.Metrics.BackButtonVisible && geometry.Header.Height > 0 && backWidth > 0 ? new(geometry.Header.Location, new Size(backWidth, geometry.Header.Height)) : Rectangle.Empty,
            Header = geometry.Header, Workspace = geometry.Frame, NavigationCanvas = geometry.Interior,
            Navigation = navigation, Footer = footer, Page = Move(measured.Page)
        };
    }
}
