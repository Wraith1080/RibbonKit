using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

public sealed class TextMotionCompletionTests
{
    [Theory]
    [InlineData("ClassicTab")]
    [InlineData("SimplifiedTab")]
    [InlineData("OpenSlide")]
    [InlineData("OpenFade")]
    [InlineData("Fade")]
    [InlineData("Theme")]
    [InlineData("Title")]
    [InlineData("KeyTip")]
    [InlineData("Density")]
    [InlineData("Scroll")]
    public void Visible_text_is_repainted_at_rest_by_shared_motion(string kind) => Sta.Run(() =>
    {
        var oldLevel = RibbonAnimation.GlobalLevel;
        bool oldReduce = RibbonAnimation.RespectSystemReduceMotion;
        var text = new PaintedText { Width = 200, Height = 30 };
        var surface = new Border { Child = text, Background = Brushes.White };
        var scroll = new ScrollViewer { Height = 80 };
        if (kind == "Scroll") scroll.Content = new Border { Child = surface, Height = 1000 };
        var window = new Window { Content = kind == "Scroll" ? scroll : surface,
            Width = 350, Height = 180, Left = -10000, Top = -10000,
            ShowActivated = false, ShowInTaskbar = false };
        IDisposable? transition = null;
        try
        {
            RibbonAnimation.RespectSystemReduceMotion = false;
            RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
            TextOptions.SetTextRenderingMode(text, TextRenderingMode.Grayscale);
            TextOptions.SetTextHintingMode(text, TextHintingMode.Fixed);
            window.Show(); window.UpdateLayout(); PumpUntil(() => text.Paints > 0);
            Size size = text.RenderSize;
            int before = text.RestPaints;
            text.AtRest = () => surface.Opacity == 1 && surface.RenderTransform.Value.IsIdentity
                && (kind != "Scroll" || Math.Abs(scroll.VerticalOffset - 20) < 0.01);
            switch (kind)
            {
                case "ClassicTab": RibbonMotion.PlaySlideIn(surface, RibbonAnimationAction.TabSwitch); break;
                case "SimplifiedTab": transition = RibbonMotion.PlaySimplifiedTabSlide(surface); break;
                case "OpenSlide": RibbonMotion.PlayOpen(surface, RibbonAnimationAction.Backstage); break;
                case "OpenFade": RibbonMotion.PlayOpen(surface, RibbonAnimationAction.MessageBar, RibbonSlideFrom.None); break;
                case "Fade": RibbonMotion.PlayFadeIn(surface, RibbonAnimationAction.ThemeSwitch); break;
                case "Theme": RibbonMotion.PlayThemeCrossfade(surface, RibbonAnimationAction.ThemeSwitch); break;
                case "Title": RibbonMotion.AnimateTranslateX(surface, RibbonAnimationAction.Backstage, -8, 0); break;
                case "KeyTip": RibbonMotion.PlayKeyTipPop(surface, RibbonAnimationAction.KeyTip); break;
                case "Density": transition = new RibbonDensityTransition(surface, -8); break;
                case "Scroll": RibbonMotion.AnimateScrollToVerticalOffset(scroll, 20, RibbonAnimationAction.RibbonScroll); break;
            }
            PumpUntil(() => text.RestPaints > before);
            Assert.Equal(size, text.RenderSize);
            Assert.Equal(TextRenderingMode.Grayscale, TextOptions.GetTextRenderingMode(text));
            Assert.Equal(TextHintingMode.Fixed, TextOptions.GetTextHintingMode(text));
        }
        finally
        {
            transition?.Dispose(); window.Close();
            RibbonAnimation.GlobalLevel = oldLevel; RibbonAnimation.RespectSystemReduceMotion = oldReduce;
        }
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Qat_exemption_keeps_the_simplified_command_row_eligible(bool simplified) => Sta.Run(() =>
    {
        var text = new PaintedText { Width = 180, Height = 30 };
        var toolbar = new RibbonQuickAccessToolBar { IsSimplified = simplified };
        toolbar.Items.Add(text);
        var window = new Window { Content = toolbar, Width = 350, Height = 180,
            Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        try
        {
            window.Show(); window.UpdateLayout(); PumpUntil(() => text.Paints > 0);
            int before = text.Paints;
            RibbonMotion.RefreshSettledText(toolbar);
            window.UpdateLayout(); Sta.Drain();
            Assert.Equal(simplified, text.Paints > before);
        }
        finally { window.Close(); }
    });

    private sealed class PaintedText : FrameworkElement
    {
        internal int Paints, RestPaints;
        internal Func<bool> AtRest = () => true;
        protected override void OnRender(DrawingContext drawingContext)
        {
            Paints++;
            if (AtRest()) RestPaints++;
            drawingContext.DrawText(new FormattedText("Text rendering at rest", CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, new Typeface("Segoe UI"), 14, Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip), new Point());
        }
    }

    private static void PumpUntil(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew(); var frame = new DispatcherFrame();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(5) };
        timer.Tick += (_, _) => { if (condition() || watch.ElapsedMilliseconds > 2000) frame.Continue = false; };
        timer.Start();
        try { Dispatcher.PushFrame(frame); }
        finally { timer.Stop(); }
        Assert.True(condition());
    }
}
