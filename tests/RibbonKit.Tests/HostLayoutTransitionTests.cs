using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

public sealed class HostLayoutTransitionTests
{
    [Theory]
    [InlineData(RibbonTheme.Office2007, false)]
    [InlineData(RibbonTheme.Office2010, false)]
    [InlineData(RibbonTheme.Office2013, false)]
    [InlineData(RibbonTheme.Office2007, true)]
    [InlineData(RibbonTheme.Office2010, true)]
    [InlineData(RibbonTheme.Office2013, true)]
    public void Connected_tab_cut_remains_in_opaque_chrome_during_mode_and_density_motion(RibbonTheme theme, bool rtl) => Run((window, ribbon) =>
    {
        ThemeManager.Apply(Application.Current, theme);
        ribbon.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        ribbon.Resources["RibbonKit.Brushes.Tab.ConnectNotch"] = Brushes.Magenta;
        Settle(window);
        var tabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
        var body = (FrameworkElement)tabs.Template.FindName("ContentPresentationHost", tabs);
        var notch = (Border)tabs.Template.FindName("PART_ConnectNotch", tabs);
        RibbonLayoutTransitionEventArgs? started = null;
        ribbon.LayoutTransitionStarting += (_, e) => started = e;
        foreach (Action change in new Action[]
        {
            () => ribbon.LayoutMode = RibbonLayoutMode.Simplified,
            () => ribbon.LayoutMode = RibbonLayoutMode.Classic,
            () => ribbon.Density = RibbonDensity.Touch,
            () => ribbon.Density = RibbonDensity.Compact,
        })
        {
            started = null; change(); Drain(); Assert.NotNull(started);
            var scene = Assert.Single(AdornerLayer.GetAdornerLayer(body)!.GetAdorners(body)!.OfType<RibbonLayoutTransition>());
            var clock = SharedClock(started); clock.Controller!.Pause();
            foreach (double progress in new[] { .15, .5, .85 })
            {
                clock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(started.BoundaryMotion.Duration.TimeSpan.TotalMilliseconds * progress), TimeSeekOrigin.BeginTime);
                Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.Render);
                window.UpdateLayout();
                var image = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                // Render the body presentation itself, without the tab header's
                // matching ConnectFoot hiding a missing cut. Leave room above
                // the final origin for the density transition's moving top edge.
                var presentation = new ContainerVisual { Transform = new TranslateTransform(0, 100) };
                presentation.Children.Add(RibbonVectorSnapshot.Clone(scene));
                image.Render(presentation);
                int x = (int)notch.TranslatePoint(new Point(notch.ActualWidth / 2, 0), body).X;
                byte[] column = new byte[(int)window.ActualHeight * 4];
                image.CopyPixels(new Int32Rect(x, 0, 1, (int)window.ActualHeight), column, 4, 0);
                // The moving one-pixel cut can straddle device pixels. Require its
                // hue rather than full pixel coverage at a fractional Y coordinate.
                Assert.True(Enumerable.Range(0, (int)window.ActualHeight).Any(y =>
                    column[y * 4] - column[y * 4 + 1] > 40 && column[y * 4 + 2] - column[y * 4 + 1] > 40),
                    $"{theme}, rtl={rtl}, progress={progress}: the body presentation covered the connected-tab cut.");
            }
            clock.Controller.Resume(); Settle(window);
            Assert.True(notch.Width > 0); Assert.Equal(1, body.Opacity);
        }
    });

    [Theory]
    [InlineData(RibbonTheme.Office2007, false)]
    [InlineData(RibbonTheme.Office2007, true)]
    [InlineData(RibbonTheme.Office2010, false)]
    [InlineData(RibbonTheme.Office2010, true)]
    [InlineData(RibbonTheme.Office2013, false)]
    [InlineData(RibbonTheme.Office2013, true)]
    [InlineData(RibbonTheme.Office2019, false)]
    [InlineData(RibbonTheme.Office2019, true)]
    [InlineData(RibbonTheme.Office2024, false)]
    [InlineData(RibbonTheme.Office2024, true)]
    [InlineData(RibbonTheme.Office2024Flat, false)]
    [InlineData(RibbonTheme.Office2024Flat, true)]
    [InlineData(RibbonTheme.CrystalLight, false)]
    [InlineData(RibbonTheme.CrystalLight, true)]
    public void Colored_caption_changes_do_not_start_a_ribbon_theme_fade(RibbonTheme theme, bool dark) => Run((window, ribbon) =>
    {
        ThemeManager.Apply(Application.Current, theme);
        ThemeManager.SetDarkMode(Application.Current, dark);
        Settle(window);
        var tabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
        // A completed theme fade retains its WPF clock. Isolate the caption toggle.
        tabs.BeginAnimation(UIElement.OpacityProperty, null);
        Assert.False(tabs.HasAnimatedProperties);
        foreach (bool enabled in new[] { true, false, true, false })
        {
            ThemeManager.SetAccentedTitleBar(Application.Current, enabled);
            Assert.False(tabs.HasAnimatedProperties);
            Assert.Equal(1, tabs.Opacity);
            Assert.Equal(enabled, ThemeManager.IsAccentedTitleBar);
        }
    });

    [Fact]
    public void Backdrop_updates_do_not_start_a_ribbon_theme_fade() => Run((window, ribbon) =>
    {
        var tabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
        var ink = ribbon.Tabs[0];
        Color before = PaintPixel(window, ink.TranslatePoint(new Point(10, 10), window));
        foreach (bool enabled in new[] { true, false, true, false })
        {
            ThemeManager.SetTitleBarBackdrop(Application.Current, enabled);
            Assert.False(tabs.HasAnimatedProperties);
            Assert.Equal(1, tabs.Opacity);
            Assert.Equal(before, PaintPixel(window, ink.TranslatePoint(new Point(10, 10), window)));
        }
        // Real palette changes retain the configured ThemeSwitch motion.
        ThemeManager.SetAccent(Application.Current, Colors.Purple);
        Assert.True(tabs.HasAnimatedProperties);
    });

    [Theory]
    [InlineData(RibbonQuickAccessPosition.TitleBar, RibbonQuickAccessPosition.BelowRibbon)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, RibbonQuickAccessPosition.TitleBar)]
    [InlineData(RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, RibbonQuickAccessPosition.TabRow)]
    public void Qat_relocation_plays_without_host_handlers_and_keeps_single_ownership(
        RibbonQuickAccessPosition from, RibbonQuickAccessPosition to) => Run((window, ribbon) =>
    {
        ribbon.QuickAccessPosition = from; Settle(window);
        FrameworkElement command = (FrameworkElement)ribbon.QuickAccessItems[0];
        ribbon.QuickAccessPosition = to;
        Assert.Single(QatScenes(window));
        Drain(); window.UpdateLayout();
        Assert.Single(QatScenes(window));
        Assert.Same(command, ribbon.QuickAccessItems[0]);
        Assert.NotNull(VisualTreeHelper.GetParent(command));
        Assert.Equal(to, ribbon.QuickAccessPosition);
        PumpUntil(() => QatScenes(window).Length == 0);
        Assert.Equal(1, command.Opacity);
    });

    [Fact]
    public void Shared_start_and_end_cover_effective_mode_density_coercion_and_placement() => Run((window, ribbon) =>
    {
        var order = new List<string>();
        var prepared = new List<RibbonLayoutTransitionEventArgs>();
        var started = new List<RibbonLayoutTransitionEventArgs>();
        var ended = new List<RibbonLayoutTransitionEventArgs>();
        ribbon.LayoutTransitionPreparing += (_, e) => { order.Add("P"); prepared.Add(e); Assert.Null(e.FinalGeometry); };
        ribbon.LayoutTransitionStarting += (_, e) =>
        {
            order.Add("S"); started.Add(e);
            Assert.NotNull(e.FinalGeometry); Assert.Equal(0, e.BoundaryMotion.Progress);
            Assert.NotEqual(e.PreviousGeometry.OccupiedBounds, e.FinalGeometry.OccupiedBounds);
        };
        ribbon.LayoutTransitionEnded += (_, e) => { order.Add("E"); ended.Add(e); };
        foreach (Action change in new Action[]
        {
            () => ribbon.LayoutMode = RibbonLayoutMode.Simplified,
            () => ribbon.Density = RibbonDensity.Touch,
            () => ribbon.IsSimplifiedRibbonEnabled = false,
            () => ribbon.IsSimplifiedRibbonEnabled = true,
            () => ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon,
        })
        {
            change(); Settle(window);
        }
        Assert.Equal(string.Concat(Enumerable.Repeat("PSE", 5)), string.Concat(order));
        Assert.Equal(prepared.Select(e => e.TransitionId), ended.Select(e => e.TransitionId));
        Assert.All(ended, e => Assert.Equal(RibbonLayoutTransitionOutcome.Completed, e.Outcome));
        Assert.Contains(prepared, e => e.Reason.HasFlag(RibbonLayoutTransitionReason.Capability));
        Assert.Equal(150, started[^1].QuickAccessMotion!.Duration.TimeSpan.TotalMilliseconds);
        Assert.Equal(1, ended[^1].BoundaryMotion.Progress);
    });

    [Fact]
    public void Reentrant_preparation_and_start_supersede_once_and_stale_completion_cannot_end_successor() => Run((window, ribbon) =>
    {
        var prepared = new List<long>(); var ended = new List<long>(); int starts = 0;
        ribbon.LayoutTransitionPreparing += (_, e) =>
        { prepared.Add(e.TransitionId); if (prepared.Count == 1) ribbon.Density = RibbonDensity.Touch; };
        ribbon.LayoutTransitionStarting += (_, _) =>
        { if (++starts == 1) ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; };
        ribbon.LayoutTransitionEnded += (_, e) => ended.Add(e.TransitionId);
        ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        Settle(window);
        Assert.Equal(3, prepared.Count);
        Assert.Equal(prepared.OrderBy(x => x), ended.OrderBy(x => x));
        Assert.Equal(ended.Count, ended.Distinct().Count());
        Assert.Equal(RibbonDensity.Touch, ribbon.Density);
        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, ribbon.QuickAccessPosition);
    });

    [Fact]
    public void Disabled_motion_and_unload_end_prepared_work_without_a_start_or_leaked_holds() => Run((window, ribbon) =>
    {
        var events = new List<RibbonLayoutTransitionEventArgs>(); int starts = 0;
        ribbon.LayoutTransitionStarting += (_, _) => starts++;
        ribbon.LayoutTransitionEnded += (_, e) => events.Add(e);
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
        ribbon.Density = RibbonDensity.Touch; Settle(window);
        Assert.Single(events); Assert.Equal(RibbonLayoutTransitionOutcome.Skipped, events[0].Outcome);
        Assert.Equal(0, starts);
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        ((DockPanel)window.Content).Children.Remove(ribbon); Drain();
        Assert.Equal(2, events.Count);
        Assert.Equal(RibbonLayoutTransitionOutcome.Interrupted, events[1].Outcome);
        Assert.Equal(0, starts); Assert.Empty(QatScenes(window));
    });

    [Fact]
    public void Composed_hiding_preserves_an_existing_opacity_clock_and_binding() => Sta.Run(() =>
    {
        var surface = new Border();
        surface.SetBinding(UIElement.OpacityProperty, new Binding(".") { Source = 0.8d });
        var existing = new DoubleAnimation(0.6, 0.6, TimeSpan.FromDays(1)).CreateClock();
        surface.ApplyAnimationClock(UIElement.OpacityProperty, existing);
        existing.Controller!.Begin(); existing.Controller.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
        var hide = RibbonLayoutDrawing.HideImmediately(surface);
        Assert.Equal(0, surface.Opacity);
        RibbonLayoutDrawing.ReleaseHide(hide);
        Assert.Equal(0.6, surface.Opacity);
        Assert.True(BindingOperations.IsDataBound(surface, UIElement.OpacityProperty));
        existing.Controller.Remove();
    });

    [Fact]
    public void End_callback_may_request_a_successor_and_input_settles_the_latest_identity() => Run((window, ribbon) =>
    {
        int preparations = 0, endings = 0;
        ribbon.LayoutTransitionPreparing += (_, _) => preparations++;
        ribbon.LayoutTransitionEnded += (_, _) =>
        { if (++endings == 1) ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; };
        ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        Drain();
        RibbonAnimation.SetActionLevel(RibbonAnimationAction.LayoutModeChange, RibbonAnimationLevel.None);
        Drain();
        Assert.Equal(2, preparations);
        Assert.Single(QatScenes(window));
        window.RaiseEvent(new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice,
            PresentationSource.FromVisual(window), 0, System.Windows.Input.Key.Escape) { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent });
        Assert.Equal(2, endings); Assert.Empty(QatScenes(window));
        RibbonAnimation.ClearActionLevel(RibbonAnimationAction.LayoutModeChange);
    });

    [Fact]
    public void Multiple_changes_in_End_keep_the_first_previous_state_and_combined_reason() => Run((window, ribbon) =>
    {
        var starts = new List<RibbonLayoutTransitionEventArgs>();
        int endings = 0;
        ribbon.LayoutTransitionStarting += (_, e) => starts.Add(e);
        ribbon.LayoutTransitionEnded += (_, _) =>
        {
            if (++endings != 1) return;
            ribbon.Density = RibbonDensity.Touch;
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        };
        ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        Drain();
        // Two sequential transitions can outlast the ordinary single-transition
        // settle interval, especially during the full suite. Wait for the actual
        // terminal notifications rather than a fixed wall-clock delay.
        PumpUntil(() => endings == 2);
        Assert.Equal(2, starts.Count);
        var combined = starts[1];
        Assert.Equal(RibbonDensity.Compact, combined.PreviousState.Density);
        Assert.Equal(RibbonDensity.Touch, combined.TargetState.Density);
        Assert.NotEqual(RibbonQuickAccessPosition.BelowRibbon, combined.PreviousState.QuickAccessPosition);
        Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, combined.TargetState.QuickAccessPosition);
        Assert.True(combined.Reason.HasFlag(RibbonLayoutTransitionReason.Density));
        Assert.True(combined.Reason.HasFlag(RibbonLayoutTransitionReason.QuickAccessPlacement));
        Assert.Equal(RibbonAnimationAction.DensityChange, combined.BoundaryMotion.Action);
        Assert.Equal(RibbonAnimationAction.QuickAccessMove, combined.QuickAccessMotion!.Action);
        Assert.Equal(2, endings);
    });

    [Fact]
    public void Throwing_preparation_still_ends_and_restores_presentations() => Run((window, ribbon) =>
    {
        int endings = 0;
        ribbon.LayoutTransitionPreparing += (_, _) => throw new InvalidOperationException("Host failure");
        ribbon.LayoutTransitionEnded += (_, e) => { endings++; Assert.False(e.WasStarted); };
        Assert.Throws<InvalidOperationException>(() => ribbon.Density = RibbonDensity.Touch);
        Assert.Equal(1, endings); Assert.Empty(QatScenes(window));
    });

    [Fact]
    public void Showcase_content_animator_holds_reflows_and_releases_without_changing_editor_state() => Run((window, ribbon) =>
    {
        var editor = new RichTextBox { Document = new FlowDocument(new Paragraph(new Run("Host-owned document"))) };
        var surface = new Border { Background = Brushes.White, Child = editor };
        var dock = (DockPanel)window.Content; dock.Children.RemoveAt(1); dock.Children.Add(surface); window.UpdateLayout();
        var transform = new TranslateTransform(2, 0);
        surface.SetBinding(UIElement.RenderTransformProperty, new Binding(".") { Source = transform });
        surface.SetBinding(UIElement.OpacityProperty, new Binding(".") { Source = 0.9d });
        var clip = new RectangleGeometry(new Rect(0, 0, 900, 500));
        surface.SetBinding(UIElement.ClipProperty, new Binding(".") { Source = clip });
        using var animator = new RibbonKit.Showcase.ShowcaseContentTransition(ribbon, surface);
        ribbon.Density = RibbonDensity.Touch;
        Assert.Equal(0, surface.Opacity);
        Dispatcher.CurrentDispatcher.Invoke(() => window.UpdateLayout(), DispatcherPriority.Render);
        Assert.Equal(0, surface.Opacity);
        ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        Drain();
        Assert.Equal(0, surface.Opacity);
        PumpUntil(() => QatScenes(window).Length == 0);
        Assert.Equal(0.9d, surface.Opacity); Assert.Same(transform, surface.RenderTransform); Assert.Same(clip, surface.Clip);
        Assert.Same(editor, surface.Child); Assert.Contains("Host-owned document", new TextRange(editor.Document.ContentStart, editor.Document.ContentEnd).Text);
    });

    [Theory]
    [InlineData(RibbonTheme.Office2024)]
    [InlineData(RibbonTheme.CrystalLight)]
    public void Main_window_document_uses_shared_progress_for_mode_density_and_both_Qat_routes(RibbonTheme theme) => Sta.Run(() =>
    {
        ThemeManager.Apply(Sta.UseApplication(showcaseResources: true), theme);
        var level = RibbonAnimation.GlobalLevel; bool reduce = RibbonAnimation.RespectSystemReduceMotion;
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; RibbonAnimation.RespectSystemReduceMotion = false;
        var window = new RibbonKit.Showcase.MainWindow { Width = 1080, Height = 680, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        window.Loaded -= (RoutedEventHandler)typeof(RibbonKit.Showcase.MainWindow).GetMethod("OnWindowLoaded", flags)!
            .CreateDelegate(typeof(RoutedEventHandler), window);
        typeof(RibbonKit.Showcase.MainWindow).GetField("_restoringAppearance", flags)!.SetValue(window, true);
        window.MainRibbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow;
        try
        {
            window.Show(); Settle(window);
            string text = new TextRange(window.DocumentEditor.Document.ContentStart, window.DocumentEditor.Document.ContentEnd).Text;
            RibbonLayoutTransitionEventArgs? starting = null;
            window.MainRibbon.LayoutTransitionStarting += (_, e) => starting = e;
            foreach (var (name, change) in new (string, Action)[]
            {
                ("simplified", () => window.MainRibbon.LayoutMode = RibbonLayoutMode.Simplified),
                ("touch", () => window.MainRibbon.Density = RibbonDensity.Touch),
                ("tab-to-below", () => window.MainRibbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon),
                ("below-to-title", () => window.MainRibbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar),
                ("title-to-below", () => window.MainRibbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon),
                ("below-to-tab", () => window.MainRibbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow),
            })
            {
                starting = null; change();
                Assert.Equal(0, window.DocumentSurface.Opacity);
                SaveFrame(window, $"{theme}-{name}-prepared");
                Drain(); Assert.NotNull(starting);
                Assert.Equal(0, window.DocumentSurface.Opacity);
                var clock = (AnimationClock)typeof(RibbonLayoutTransitionMotion).GetField("_clock", flags)!.GetValue(starting.BoundaryMotion)!;
                ClockGroup groupClock = (ClockGroup)clock.Parent!;
                groupClock.Controller!.Pause();
                groupClock.Controller.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
                SaveFrame(window, $"{theme}-{name}-start");
                groupClock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(starting.BoundaryMotion.Duration.TimeSpan.TotalMilliseconds / 2), TimeSeekOrigin.BeginTime);
                Assert.InRange(starting.BoundaryMotion.Progress, 0.1, 0.99);
                SaveFrame(window, $"{theme}-{name}-middle");
                groupClock.Controller.Resume();
                PumpUntil(() => QatScenes(window).Length == 0);
                Assert.Equal(1, window.DocumentSurface.Opacity);
                Assert.Equal(text, new TextRange(window.DocumentEditor.Document.ContentStart, window.DocumentEditor.Document.ContentEnd).Text);
                SaveFrame(window, $"{theme}-{name}-final");
            }
        }
        finally { window.Close(); Drain(); RibbonAnimation.GlobalLevel = level; RibbonAnimation.RespectSystemReduceMotion = reduce; Sta.ResetApplication(); }
    });

    private static void SaveFrame(Window window, string name)
    {
        string? folder = Environment.GetEnvironmentVariable("RIBBONKIT_LAYOUT_MOTION_DIAGNOSTICS");
        if (string.IsNullOrEmpty(folder)) return;
        Directory.CreateDirectory(folder); window.UpdateLayout();
        var image = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); image.Render(window);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(Path.Combine(folder, name + ".png")); png.Save(stream);
    }

    [Theory]
    [InlineData(RibbonQuickAccessPosition.TitleBar, RibbonQuickAccessPosition.BelowRibbon)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, RibbonQuickAccessPosition.TitleBar)]
    [InlineData(RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, RibbonQuickAccessPosition.TabRow)]
    public void Qat_pixels_travel_continuously_at_natural_scale(RibbonQuickAccessPosition from, RibbonQuickAccessPosition to) => Run((window, ribbon) =>
    {
        ribbon.QuickAccessItems.Clear();
        var qat = new RibbonButton { Header = "QAT only", Size = RibbonControlSize.Small,
            Icon = new DrawingImage(new GeometryDrawing(Brushes.Magenta, null, new RectangleGeometry(new Rect(0, 0, 16, 16)))) };
        ribbon.QuickAccessItems.Add(qat); ribbon.QuickAccessPosition = from; Settle(window);
        Rect before = MagentaBounds(window);
        RibbonLayoutTransitionEventArgs? starting = null;
        ribbon.LayoutTransitionStarting += (_, e) => starting = e;
        ribbon.QuickAccessPosition = to; Drain(); Assert.NotNull(starting);
        var clock = (AnimationClock)typeof(RibbonLayoutTransitionMotion).GetField("_clock", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(starting.QuickAccessMotion)!;
        var groupClock = (ClockGroup)clock.Parent!;
        groupClock.Controller!.Pause();
        groupClock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(75), TimeSeekOrigin.BeginTime);
        double p = starting.QuickAccessMotion!.Progress;
        Rect middle = MagentaBounds(window);
        groupClock.Controller.Resume(); PumpUntil(() => QatScenes(window).Length == 0);
        Rect final = MagentaBounds(window);
        Assert.True(Math.Abs(final.Top - before.Top) > 50);
        Assert.InRange(middle.Top, Math.Min(before.Top, final.Top) + 1, Math.Max(before.Top, final.Top) - 1);
        Assert.InRange(Math.Abs(middle.Top - (before.Top + (final.Top - before.Top) * p)), 0, 5);
        Assert.InRange(middle.Height, Math.Min(before.Height, final.Height) - 2, Math.Max(before.Height, final.Height) + 4);
        Assert.InRange(middle.Width, Math.Min(before.Width, final.Width) - 2, Math.Max(before.Width, final.Width) + 4);
    });

    private static Rect MagentaBounds(Window window)
    {
        window.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
        byte[] pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        int minX = bitmap.PixelWidth, minY = bitmap.PixelHeight, maxX = -1, maxY = -1;
        for (int y = 0; y < bitmap.PixelHeight; y++) for (int x = 0; x < bitmap.PixelWidth; x++)
        {
            int offset = (y * bitmap.PixelWidth + x) * 4;
            if (pixels[offset] > 180 && pixels[offset + 2] > 180 && pixels[offset + 1] < 150)
            { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
        }
        Assert.True(maxX >= minX && maxY >= minY, "The unique QAT command must remain painted.");
        return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    [Fact]
    public void Startup_qat_selection_does_not_close_a_subsequently_opened_File_surface() => Run((window, _) =>
    {
        var menu = new RibbonApplicationMenu(); menu.Items.Add(new RibbonMenuItem { Header = "Open" });
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon, ApplicationMenu = menu };
        var dock = (DockPanel)window.Content; dock.Children.RemoveAt(0); dock.Children.Insert(0, ribbon); DockPanel.SetDock(ribbon, Dock.Top);
        window.UpdateLayout(); ribbon.IsBackstageOpen = true;
        Drain(); window.UpdateLayout();
        Assert.True(ribbon.IsBackstageOpen);
        ribbon.IsBackstageOpen = false;
    });

    [Fact]
    public void Unsafe_media_presentation_skips_without_hiding_or_waiting_for_a_clock() => Run((window, ribbon) =>
    {
        ribbon.QuickAccessItems.Clear(); ribbon.QuickAccessItems.Add(new MediaElement { Width = 24, Height = 24 });
        Settle(window);
        int starts = 0; RibbonLayoutTransitionEventArgs? ended = null;
        ribbon.LayoutTransitionStarting += (_, _) => starts++;
        ribbon.LayoutTransitionEnded += (_, e) => ended = e;
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
        Assert.NotNull(ended); Assert.Equal(RibbonLayoutTransitionOutcome.Skipped, ended.Outcome);
        Assert.False(ended.BoundaryMotion.IsAnimated); Assert.Equal(0, starts); Assert.Empty(QatScenes(window));
        Drain(); Assert.Equal(RibbonQuickAccessPosition.BelowRibbon, ribbon.QuickAccessPosition);
    });

    [Fact]
    public void Overlapping_pending_holds_keep_the_old_body_visible_even_when_preparing_forces_layout() => Run((window, ribbon) =>
    {
        var tabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
        var body = (Border)tabs.Template.FindName("ContentHost", tabs);
        Rect bounds = body.TransformToVisual(window).TransformBounds(new Rect(body.RenderSize));
        int before = BluePixels(); Assert.True(before > 100);
        ribbon.LayoutTransitionPreparing += (_, _) => window.UpdateLayout();
        ribbon.Density = RibbonDensity.Touch;
        ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        Dispatcher.CurrentDispatcher.Invoke(() => window.UpdateLayout(), DispatcherPriority.Render);
        // Vector and live-image edge antialiasing can differ by a pixel. A blank
        // cover or the smaller destination icon cannot satisfy this area bound.
        Assert.InRange(BluePixels(), (int)(before * 0.85), (int)(before * 1.15));
        Drain(); PumpUntil(() => QatScenes(window).Length == 0);

        int BluePixels()
        {
            var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
            byte[] pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
            int count = 0;
            for (int y = (int)bounds.Top; y < (int)bounds.Bottom; y++) for (int x = (int)bounds.Left; x < (int)bounds.Right; x++)
            {
                int offset = (y * bitmap.PixelWidth + x) * 4;
                if (pixels[offset] is >= 170 and <= 190 && pixels[offset + 1] is >= 120 and <= 140 && pixels[offset + 2] is >= 60 and <= 80) count++;
            }
            return count;
        }
    });

    [Fact]
    public void A_changed_Qat_width_interrupts_instead_of_finishing_at_stale_geometry() => Run((window, ribbon) =>
    {
        ribbon.QuickAccessItems.Clear();
        for (int i = 0; i < 8; i++) ribbon.QuickAccessItems.Add(new RibbonButton { Header = "Command " + i });
        ribbon.QuickAccessMaxWidth = 200; Settle(window);
        RibbonLayoutTransitionEventArgs? ended = null;
        ribbon.LayoutTransitionEnded += (_, e) => ended = e;
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar; Drain();
        Assert.Single(QatScenes(window));
        ribbon.QuickAccessMaxWidth = 45; window.UpdateLayout();
        Assert.NotNull(ended); Assert.Equal(RibbonLayoutTransitionOutcome.Interrupted, ended.Outcome);
        Assert.Empty(QatScenes(window)); Assert.Equal(8, ribbon.QuickAccessItems.Count);
    });

    [Fact]
    public void Live_Qat_relocation_preserves_the_open_File_surface() => Run((window, ribbon) =>
    {
        var menu = new RibbonApplicationMenu(); menu.Items.Add(new RibbonMenuItem { Header = "Open" });
        ribbon.ApplicationMenu = menu; ribbon.IsBackstageOpen = true; Drain();
        Assert.True(ribbon.IsApplicationMenuOpen);
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; Drain();
        Assert.True(ribbon.IsApplicationMenuOpen);
        ribbon.IsBackstageOpen = false;
    });

    [Fact]
    public void Deferred_Qat_popup_cleanup_ignores_a_reopened_popup() => Run((window, ribbon) =>
    {
        var dropdown = new RibbonDropDownButton { Header = "Options" }; dropdown.Items.Add(new RibbonMenuItem { Header = "Save" });
        ribbon.QuickAccessItems.Clear(); ribbon.QuickAccessItems.Add(dropdown); Settle(window);
        dropdown.IsDropDownOpen = true; Drain();
        Action stale = PopupDismissHelper.CaptureDismissalWithin(dropdown, window);
        dropdown.IsDropDownOpen = false; Drain(); dropdown.IsDropDownOpen = true; Drain();
        stale(); Assert.True(dropdown.IsDropDownOpen);
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; Drain();
        Assert.False(dropdown.IsDropDownOpen);
    });

    [Fact]
    public void Qat_drawing_retains_ancestor_opacity_in_the_shared_window_plane() => Run((window, ribbon) =>
    {
        ribbon.QuickAccessItems.Clear();
        ribbon.QuickAccessItems.Add(new RibbonButton { Header = "QAT only", Size = RibbonControlSize.Small,
            Icon = new DrawingImage(new GeometryDrawing(Brushes.Magenta, null, new RectangleGeometry(new Rect(0, 0, 16, 16)))) });
        ribbon.Opacity = 0.65; Settle(window);
        double before = GreenChannel(); RibbonLayoutTransitionEventArgs? starting = null;
        ribbon.LayoutTransitionStarting += (_, e) => starting = e;
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon; Drain(); Assert.NotNull(starting);
        var clock = (AnimationClock)typeof(RibbonLayoutTransitionMotion).GetField("_clock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(starting.QuickAccessMotion)!;
        var groupClock = (ClockGroup)clock.Parent!; groupClock.Controller!.Pause();
        groupClock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(75), TimeSeekOrigin.BeginTime);
        Assert.InRange(GreenChannel(), before - 5, before + 40);
        groupClock.Controller.Resume(); PumpUntil(() => QatScenes(window).Length == 0);
        Assert.Equal(0.65, ribbon.Opacity);

        double GreenChannel()
        {
            var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32); bitmap.Render(window);
            byte[] pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4]; bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
            var samples = new List<byte>();
            for (int i = 0; i < pixels.Length; i += 4)
                if (pixels[i] > 180 && pixels[i + 2] > 180 && pixels[i + 1] < 150) samples.Add(pixels[i + 1]);
            Assert.NotEmpty(samples); return samples.Average(value => (double)value);
        }
    });

    [Fact]
    public void Body_holds_playback_and_reversal_keep_glyphs_outside_shadow_effects() => Run((window, ribbon) =>
    {
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.CrystalLight); Settle(window);
        ribbon.Density = RibbonDensity.Touch;
        InspectPresentation(); // Synchronous previous-presentation hold.
        Drain(); InspectPresentation(); // Playback uses the same separation.
        ribbon.Density = RibbonDensity.Compact;
        InspectPresentation(); // A reversal captures chrome independently of text.
        Drain(); InspectPresentation();
        PumpUntil(() => QatScenes(window).Length == 0);

        void InspectPresentation()
        {
            var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var root = window.RibbonPresentationSurface!.Value;
            var presentations = (root.Layer.GetAdorners(root.Root) ?? []).Cast<Visual>().ToList();
            var tabs = (RibbonTabControl)ribbon.Template.FindName("TabControlHost", ribbon);
            var body = (FrameworkElement)tabs.Template.FindName("ContentPresentationHost", tabs);
            presentations.AddRange(AdornerLayer.GetAdornerLayer(body)?.GetAdorners(body) ?? []);
            Assert.NotEmpty(presentations);
            var visuals = presentations.SelectMany(Visuals).ToArray();
            Assert.Contains(visuals, visual => HasGlyphs(VisualTreeHelper.GetDrawing(visual)));
            var shadows = visuals.Where(visual => (visual as UIElement)?.Effect is DropShadowEffect
                || (visual as ContainerVisual)?.Effect is DropShadowEffect).ToArray();
            Assert.NotEmpty(shadows);
            foreach (var shadow in shadows)
                Assert.DoesNotContain(Visuals(shadow), visual => HasGlyphs(VisualTreeHelper.GetDrawing(visual)));
        }
        static bool HasGlyphs(Drawing? drawing) => drawing is GlyphRunDrawing
            || drawing is DrawingGroup group && group.Children.Any(HasGlyphs);
        static IEnumerable<Visual> Visuals(Visual parent)
        {
            yield return parent;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
                if (VisualTreeHelper.GetChild(parent, i) is Visual child)
                    foreach (var visual in Visuals(child)) yield return visual;
        }
    });

    [Theory]
    [InlineData(RibbonQuickAccessPosition.TitleBar, false, 1d)]
    [InlineData(RibbonQuickAccessPosition.TitleBar, true, 1d)]
    [InlineData(RibbonQuickAccessPosition.TitleBar, true, 0.7d)]
    [InlineData(RibbonQuickAccessPosition.TabRow, false, 1d)]
    public void Caption_background_keeps_its_authored_paint_during_layout_motion(RibbonQuickAccessPosition placement, bool density, double opacity) => Run((window, ribbon) =>
    {
        window.Background = Brushes.White;
        ribbon.Background = Brushes.Navy;
        ribbon.QuickAccessPosition = placement; Settle(window);
        PumpUntil(() => QatScenes(window).Length == 0);
        window.Resources["RibbonKit.Brushes.TitleBar.Background"] = Brushes.Black;
        ((Border)window.Template.FindName("TitleBarBackgroundLayer", window))
            .SetBinding(UIElement.OpacityProperty, new Binding(".") { Source = opacity });
        Drain();
        Color before = PaintPixel(window, new Point(window.ActualWidth * 0.65, 12));
        double oldHeight = window.RibbonPresentationCaption!.ActualHeight;
        Color headerPaint = PaintPixel(window, new Point(window.ActualWidth * 0.65, oldHeight + 2));
        RibbonLayoutTransitionEventArgs? started = null;
        ribbon.LayoutTransitionStarting += (_, e) => started = e;
        if (density) ribbon.Density = RibbonDensity.Touch;
        else ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        Drain(); Assert.NotNull(started);
        var clock = SharedClock(started);
        clock.Controller!.Pause();
        foreach (double progress in new[] { 0d, 0.25d, 0.5d, 0.75d })
        {
            clock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(started.BoundaryMotion.Duration.TimeSpan.TotalMilliseconds * progress), TimeSeekOrigin.BeginTime);
            double height = oldHeight + (window.RibbonPresentationCaption!.ActualHeight - oldHeight) * started.BoundaryMotion.Progress;
            foreach (double y in new[] { 12d, height - 3 })
            {
                Color pixel = PaintPixel(window, new Point(window.ActualWidth * 0.65, y));
                Assert.InRange((int)pixel.R, before.R - 2, before.R + 2);
                Assert.InRange((int)pixel.G, before.G - 2, before.G + 2);
                Assert.InRange((int)pixel.B, before.B - 2, before.B + 2);
            }
            Assert.Equal(headerPaint, PaintPixel(window, new Point(window.ActualWidth * 0.65, height + 2)));
        }
        clock.Controller.Resume();
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Accent_caption_keeps_its_color_through_mode_and_density_reversals(bool dark) => Run((window, ribbon) =>
    {
        ThemeManager.SetDarkMode(Application.Current, dark);
        ThemeManager.SetAccent(Application.Current, Color.FromRgb(0x87, 0x64, 0xB8));
        ThemeManager.SetAccentedTitleBar(Application.Current, true);
        window.Background = Brushes.White;
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
        Settle(window); PumpUntil(() => QatScenes(window).Length == 0);
        Color before = PaintPixel(window, new Point(window.ActualWidth * 0.65, 12));
        Assert.NotEqual(Colors.White, before);
        RibbonLayoutTransitionEventArgs? started = null;
        ribbon.LayoutTransitionStarting += (_, e) => started = e;
        foreach (Action change in new Action[] {
            () => ribbon.LayoutMode = RibbonLayoutMode.Simplified,
            () => ribbon.Density = RibbonDensity.Touch,
            () => ribbon.Density = RibbonDensity.Compact,
            () => ribbon.LayoutMode = RibbonLayoutMode.Classic })
        {
            started = null; change(); Drain(); Assert.NotNull(started);
            var clock = SharedClock(started); clock.Controller!.Pause();
            foreach (double time in new[] { 0d, 0.15d, 0.5d, 0.9d })
            {
                clock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(started.BoundaryMotion.Duration.TimeSpan.TotalMilliseconds * time), TimeSeekOrigin.BeginTime);
                Assert.Equal(before, PaintPixel(window, new Point(window.ActualWidth * 0.65, 12)));
            }
            clock.Controller.Resume(); PumpUntil(() => QatScenes(window).Length == 0);
        }
    });

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public void Stationary_caption_header_and_Qat_ink_does_not_dim_during_mode_changes(bool dark, bool accent, bool reverse) => Run((window, ribbon) =>
    {
        ThemeManager.SetDarkMode(Application.Current, dark);
        ThemeManager.SetAccentedTitleBar(Application.Current, accent);
        if (accent) ThemeManager.SetAccent(Application.Current, Color.FromRgb(0x87, 0x64, 0xB8));
        window.Title = "Stationary caption";
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
        Settle(window);
        var title = (TextBlock)window.Template.FindName("PART_Title", window);
        Rect titleBounds = title.TransformToVisual(window).TransformBounds(new Rect(title.RenderSize));
        var header = (FrameworkElement)ribbon.Tabs[0].Template.FindName("HeaderText", ribbon.Tabs[0]);
        Rect tabBounds = header.TransformToVisual(window).TransformBounds(new Rect(header.RenderSize));
        var qat = (FrameworkElement)ribbon.QuickAccessItems[0];
        Rect qatBounds = qat.TransformToVisual(window).TransformBounds(new Rect(qat.RenderSize));
        RibbonLayoutTransitionEventArgs? started = null;
        ribbon.LayoutTransitionStarting += (_, e) => started = e;
        foreach (var mode in new[] { RibbonLayoutMode.Simplified, RibbonLayoutMode.Classic })
        {
            started = null; ribbon.LayoutMode = mode; Drain(); Assert.NotNull(started);
            var clock = SharedClock(started); clock.Controller!.Pause();
            clock.Controller.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
            byte[] titleAtStart = Pixels(titleBounds), tabAtStart = Pixels(tabBounds);
            byte[] qatAtStart = Pixels(qatBounds);
            foreach (double time in new[] { .15, .3, .5 })
            {
                clock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(started.BoundaryMotion.Duration.TimeSpan.TotalMilliseconds * time), TimeSeekOrigin.BeginTime);
                Assert.Equal(titleAtStart, Pixels(titleBounds));
                Assert.Equal(tabAtStart, Pixels(tabBounds));
                Assert.Equal(qatAtStart, Pixels(qatBounds));
            }
            // The checks above run before completion, so completion-only text
            // refresh cannot cause or conceal their loss of foreground alpha.
            if (reverse && mode == RibbonLayoutMode.Simplified) continue;
            clock.Controller.Resume(); Settle(window);
        }
        byte[] Pixels(Rect bounds)
        {
            window.UpdateLayout();
            var bitmap = new RenderTargetBitmap((int)window.ActualWidth, (int)window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(window);
            var rect = new Int32Rect((int)Math.Ceiling(bounds.X) + 1, (int)Math.Ceiling(bounds.Y) + 1,
                (int)Math.Floor(bounds.Width) - 2, (int)Math.Floor(bounds.Height) - 2);
            byte[] result = new byte[rect.Width * rect.Height * 4]; bitmap.CopyPixels(rect, result, rect.Width * 4, 0);
            return result;
        }
    });

    [Theory]
    [InlineData(1d)]
    [InlineData(0.7d)]
    public void Host_content_does_not_expose_the_backing_color_during_layout_motion(double opacity) => Run((window, ribbon) =>
    {
        window.Background = Brushes.White;
        var surface = new Border { Background = Brushes.Black, Opacity = opacity,
            BorderBrush = Brushes.Red, BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(8) };
        var dock = (DockPanel)window.Content; dock.Children.RemoveAt(1); dock.Children.Add(surface); window.UpdateLayout();
        PaintPixel(window, new Point(window.ActualWidth / 2, window.ActualHeight - 80));
        using var animator = new RibbonKit.Showcase.ShowcaseContentTransition(ribbon, surface);
        RibbonLayoutTransitionEventArgs? started = null;
        ribbon.LayoutTransitionStarting += (_, e) => started = e;
        ribbon.Density = RibbonDensity.Touch; Drain(); Assert.NotNull(started);
        var clock = SharedClock(started);
        clock.Controller!.Pause();
        foreach (double progress in new[] { 0d, 0.25d, 0.5d, 0.75d })
        {
            clock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(started.BoundaryMotion.Duration.TimeSpan.TotalMilliseconds * progress), TimeSeekOrigin.BeginTime);
            int expected = (int)Math.Round(255 * (1 - opacity));
            for (int offset = 4; offset <= 60; offset += 2)
            {
                Color pixel = PaintPixel(window, new Point(window.ActualWidth / 2, window.ActualHeight - offset));
                Assert.InRange((int)pixel.R, expected - 2, expected + 2);
                Assert.InRange((int)pixel.G, expected - 2, expected + 2);
            }
        }
        clock.Controller.Resume();
    });

    [Fact]
    public void Moving_ribbon_backdrop_stays_below_command_presentations_and_is_released() => Run((window, ribbon) =>
    {
        ribbon.Background = Brushes.Navy;
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar;
        ribbon.Tabs[0].Groups[0].Items.Add(new RibbonButton { Header = "Unique body ink", Size = RibbonControlSize.Large,
            Icon = new DrawingImage(new GeometryDrawing(Brushes.Magenta, null, new RectangleGeometry(new Rect(0, 0, 24, 24)))) });
        Settle(window);
        Assert.True(MagentaPixels(window) > 100);
        RibbonLayoutTransitionEventArgs? started = null;
        ribbon.LayoutTransitionStarting += (_, e) => started = e;
        ribbon.Density = RibbonDensity.Touch; Drain(); Assert.NotNull(started);
        var clock = SharedClock(started); clock.Controller!.Pause();
        clock.Controller.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(started.BoundaryMotion.Duration.TimeSpan.TotalMilliseconds / 2), TimeSeekOrigin.BeginTime);
        Assert.True(MagentaPixels(window) > 100, "The moving backdrop must not cover the body's command drawings.");
        var background = Assert.IsType<Border>(ribbon.Template.FindName("RibbonBackgroundLayer", ribbon));
        var layer = AdornerLayer.GetAdornerLayer(background)!;
        Assert.Equal(1, background.Opacity);
        clock.Controller.Resume(); PumpUntil(() => QatScenes(window).Length == 0);
        Assert.Null(layer.GetAdorners(background));
        Assert.Equal(1, background.Opacity);

        static int MagentaPixels(Window window)
        {
            window.UpdateLayout();
            int width = (int)window.ActualWidth, height = (int)window.ActualHeight;
            var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32); image.Render(window);
            byte[] pixels = new byte[width * height * 4]; image.CopyPixels(pixels, width * 4, 0);
            int count = 0;
            for (int i = 0; i < pixels.Length; i += 4)
                if (pixels[i] > 240 && pixels[i + 1] < 20 && pixels[i + 2] > 240) count++;
            return count;
        }
    });

    [Fact]
    public void Caption_without_a_separate_background_layer_uses_immediate_fallback() => Run((window, ribbon) =>
    {
        var decorator = new FrameworkElementFactory(typeof(AdornerDecorator), "WindowAdornerHost");
        var root = new FrameworkElementFactory(typeof(DockPanel), "WindowLayoutHost");
        var caption = new FrameworkElementFactory(typeof(Border), "TitleBarBand");
        caption.SetValue(DockPanel.DockProperty, Dock.Top);
        caption.SetValue(FrameworkElement.HeightProperty, 34d);
        caption.SetValue(Border.BackgroundProperty, Brushes.Navy);
        var titleContent = new FrameworkElementFactory(typeof(ContentPresenter), "TitleBarContentHost");
        titleContent.SetBinding(ContentPresenter.ContentProperty, new Binding(nameof(RibbonWindow.TitleBarContent)) { RelativeSource = RelativeSource.TemplatedParent });
        caption.AppendChild(titleContent); root.AppendChild(caption);
        var content = new FrameworkElementFactory(typeof(ContentPresenter));
        content.SetBinding(ContentPresenter.ContentProperty, new Binding(nameof(Window.Content)) { RelativeSource = RelativeSource.TemplatedParent });
        root.AppendChild(content); decorator.AppendChild(root);
        window.Template = new ControlTemplate(typeof(RibbonWindow)) { VisualTree = decorator };
        window.ApplyTemplate(); Settle(window);
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar; Settle(window);
        int starts = 0; var endings = new List<RibbonLayoutTransitionEventArgs>();
        ribbon.LayoutTransitionStarting += (_, _) => starts++;
        ribbon.LayoutTransitionEnded += (_, e) => endings.Add(e);
        ribbon.Density = RibbonDensity.Touch; Drain();
        Assert.Equal(0, starts);
        Assert.Equal(RibbonLayoutTransitionOutcome.Skipped, Assert.Single(endings).Outcome);
        Assert.Empty(QatScenes(window));
    });

    private static ClockGroup SharedClock(RibbonLayoutTransitionEventArgs args) => (ClockGroup)((AnimationClock)typeof(RibbonLayoutTransitionMotion)
        .GetField("_clock", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(args.BoundaryMotion)!).Parent!;

    private static Color PaintPixel(Window window, Point point)
    {
        window.UpdateLayout();
        var image = new RenderTargetBitmap((int)Math.Ceiling(window.ActualWidth), (int)Math.Ceiling(window.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        image.Render(window);
        byte[] pixel = new byte[4];
        image.CopyPixels(new Int32Rect((int)point.X, (int)point.Y, 1, 1), pixel, 4, 0);
        return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
    }

    private static Adorner[] QatScenes(RibbonWindow window)
    {
        var root = window.RibbonPresentationSurface!.Value;
        return root.Layer.GetAdorners(root.Root)?.Where(a => a.GetType().Name == "RibbonQuickAccessTransition").ToArray() ?? [];
    }
    private static void Run(Action<RibbonWindow, Ribbon> body) => Sta.Run(() =>
    {
        var oldLevel = RibbonAnimation.GlobalLevel; bool reduce = RibbonAnimation.RespectSystemReduceMotion;
        ThemeManager.Apply(Sta.UseApplication(), RibbonTheme.Office2024);
        RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle; RibbonAnimation.RespectSystemReduceMotion = false;
        var ribbon = new Ribbon { QuickAccessPosition = RibbonQuickAccessPosition.TabRow };
        var group = new RibbonGroup { Header = "Commands" };
        var command = new RibbonButton { Header = "Sample", Icon = new DrawingImage(new GeometryDrawing(Brushes.SteelBlue, null, new RectangleGeometry(new Rect(0, 0, 16, 16)))) };
        Ribbon.SetSimplifiedPlacement(command, RibbonSimplifiedPlacement.Inline); group.Items.Add(command);
        var tab = new RibbonTab { Header = "Home" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab); ribbon.SelectedTab = tab;
        ribbon.AddToQuickAccess(command);
        var dock = new DockPanel(); DockPanel.SetDock(ribbon, Dock.Top); dock.Children.Add(ribbon); dock.Children.Add(new TextBox { Text = "Independent content" });
        var window = new RibbonWindow { Width = 900, Height = 550, Left = -10000, Top = -10000, ShowInTaskbar = false, ShowActivated = false, Content = dock };
        try { window.Show(); Settle(window); body(window, ribbon); }
        finally { window.Close(); Drain(); RibbonAnimation.GlobalLevel = oldLevel; RibbonAnimation.RespectSystemReduceMotion = reduce; Sta.ResetApplication(); }
    });
    private static void Drain() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static void Settle(Window window)
    {
        Drain(); window.UpdateLayout();
        var watch = Stopwatch.StartNew();
        PumpUntil(() => watch.ElapsedMilliseconds >= 240
            && (window is not RibbonWindow ribbonWindow || QatScenes(ribbonWindow).Length == 0));
        bool rendered = false;
        EventHandler onRender = (_, _) => rendered = true;
        CompositionTarget.Rendering += onRender;
        try { PumpUntil(() => rendered); }
        finally { CompositionTarget.Rendering -= onRender; }
        Drain(); window.UpdateLayout();
    }
    private static void PumpUntil(Func<bool> condition)
    {
        var frame = new DispatcherFrame(); var watch = Stopwatch.StartNew();
        var timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(5) };
        timer.Tick += (_, _) => { if (condition() || watch.ElapsedMilliseconds > 2500) frame.Continue = false; };
        timer.Start(); try { Dispatcher.PushFrame(frame); } finally { timer.Stop(); }
        Assert.True(condition());
    }
}
