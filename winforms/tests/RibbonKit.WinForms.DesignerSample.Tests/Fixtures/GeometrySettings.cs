using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class ChromeGeometrySettings
{
    internal static RibbonTheme Body(RibbonTheme theme) => theme with { GroupHorizontalPadding = 12, GroupColumnGap = 10, BodyContentHorizontalPadding = 12, GroupSeparatorVerticalInset = 20 };
    internal static RibbonTheme ResetBody(RibbonTheme theme) => theme with { GroupHorizontalPadding = null, GroupColumnGap = null, BodyContentHorizontalPadding = null, GroupSeparatorVerticalInset = null };
    internal static RibbonTheme Header(RibbonTheme theme) => theme with { TabHorizontalPadding = 22, TabMinimumWidth = 90, HeaderIndicatorPadding = 8 };
    internal static RibbonTheme ResetHeader(RibbonTheme theme) => theme with { TabHorizontalPadding = null, TabMinimumWidth = null, HeaderIndicatorPadding = null };
    internal static RibbonTheme Utilities(RibbonTheme theme) => theme with { QuickAccessHorizontalPadding = 8, QuickAccessItemGap = 6, MessageActionGap = 18, MessageTitleTextGap = 12 };
    internal static RibbonTheme ResetUtilities(RibbonTheme theme) => theme with { QuickAccessHorizontalPadding = null, QuickAccessItemGap = null, MessageActionGap = null, MessageTitleTextGap = null };
}


internal static class FrameGeometrySettings
{
    internal static RibbonTheme Corners(RibbonTheme t) => t with { BodyCornerRadius = 14, BackstagePageCornerRadius = 18, PopupCornerRadius = 12 };
    internal static RibbonTheme ResetCorners(RibbonTheme t, int popupRadius) => t with { BodyCornerRadius = null, BackstagePageCornerRadius = null, PopupCornerRadius = popupRadius };
    internal static RibbonTheme BodyShadow(RibbonTheme t) => t with { BodyShadowBlur = 12, BodyShadowVerticalOffset = 4 };
    internal static RibbonTheme ResetBodyShadow(RibbonTheme t) => t with { BodyShadowBlur = null, BodyShadowVerticalOffset = null };
    internal static RibbonTheme PopupShadow(RibbonTheme t) => t with { PopupShadowBlur = 12, PopupShadowVerticalOffset = 4 };
    internal static RibbonTheme ResetPopupShadow(RibbonTheme t) => t with { PopupShadowBlur = null, PopupShadowVerticalOffset = null };
}


internal static class GalleryBackstageGeometrySettings
{
    internal static RibbonTheme Gallery(RibbonTheme t) => t with { GalleryContentPadding = 10, GalleryUtilityWidth = 40, GalleryTilePadding = 10, GalleryHeadingHorizontalPadding = 16, GalleryHeadingVerticalPadding = 12 };
    internal static RibbonTheme ResetGallery(RibbonTheme t) => t with { GalleryContentPadding = null, GalleryUtilityWidth = null, GalleryTilePadding = null, GalleryHeadingHorizontalPadding = null, GalleryHeadingVerticalPadding = null };
    internal static RibbonTheme Navigation(RibbonTheme t) => t with { BackstageMinimumRowHeight = 62, BackstageNavigationPadding = 14, BackstageBackButtonSize = 52, BackstageBackButtonInset = 28, BackstageBackButtonBottomGap = 32, BackstageIconTextGap = 22 };
    internal static RibbonTheme ResetNavigation(RibbonTheme t) => t with { BackstageMinimumRowHeight = null, BackstageNavigationPadding = null, BackstageBackButtonSize = null, BackstageBackButtonInset = null, BackstageBackButtonBottomGap = null, BackstageIconTextGap = null };
}


internal static class TipCaptionGeometrySettings
{
    internal static RibbonTheme ScreenTips(RibbonTheme t) => t with { ScreenTipMaximumWidth = 420, ScreenTipContentPadding = 20, ScreenTipTitleGap = 12, ScreenTipPlacementGap = 16 };
    internal static RibbonTheme ResetScreenTips(RibbonTheme t) => t with { ScreenTipMaximumWidth = null, ScreenTipContentPadding = null, ScreenTipTitleGap = null, ScreenTipPlacementGap = null };
    internal static RibbonTheme KeyTips(RibbonTheme t) => t with { KeyTipHorizontalPadding = 6, KeyTipVerticalPadding = 4 };
    internal static RibbonTheme ResetKeyTips(RibbonTheme t) => t with { KeyTipHorizontalPadding = null, KeyTipVerticalPadding = null };
    internal static RibbonTheme Caption(RibbonTheme t) => t with { CaptionLeadingPadding = 22, CaptionIconTextGap = 18, CaptionQuickAccessTextGap = 18, CaptionTitleReservationWidth = 180 };
    internal static RibbonTheme ResetCaption(RibbonTheme t) => t with { CaptionLeadingPadding = null, CaptionIconTextGap = null, CaptionQuickAccessTextGap = null, CaptionTitleReservationWidth = null };
}
