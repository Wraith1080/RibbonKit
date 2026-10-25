using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using RibbonKit.Animation;
using RibbonKit.Controls;
using RibbonKit.Theming;
using RibbonKit.Writer.Appearance;
using RibbonKit.Writer.Editing;
using RibbonKit.Writer.Tests.Document;
using RibbonKit.Writer.View;
using Xunit;

namespace RibbonKit.Writer.Tests.Shell;

public sealed partial class MainWindowIntegrationTests
{
    [Theory]
    [InlineData(RibbonQuickAccessPosition.TitleBar, false)]
    [InlineData(RibbonQuickAccessPosition.TitleBar, true)]
    [InlineData(RibbonQuickAccessPosition.TabRow, false)]
    [InlineData(RibbonQuickAccessPosition.TabRow, true)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, false)]
    [InlineData(RibbonQuickAccessPosition.BelowRibbon, true)]
    public async Task WriterDensityMenuKeepsSharedMotionForMouseTouchRoundTrips(RibbonQuickAccessPosition placement, bool copied)
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var directory = new TemporaryDirectory();
            var store = new WriterSettingsStore(new WriterSettingsPaths(directory.File("appearance.json"), directory.File("ribbon.json")));
            store.SaveAppearance(new WriterAppearancePreferences { Theme = RibbonTheme.CrystalLight,
                ShowRuler = false, AnimationLevel = RibbonAnimationLevel.Subtle, RespectSystemReducedMotion = false });
            using var fixture = new WindowFixture(settings: store);
            var originalLevel = RibbonAnimation.GlobalLevel;
            bool originalReduce = RibbonAnimation.RespectSystemReduceMotion;
            try
            {
                fixture.Ribbon.QuickAccessPosition = placement;
                fixture.Ribbon.SelectedTab = (RibbonTab)fixture.Window.FindName("ViewTab");
                var selector = (RibbonDropDownButton)fixture.Window.FindName("InputModeSelector");
                if (copied)
                {
                    fixture.Ribbon.QuickAccessItems.Clear();
                    fixture.Ribbon.AddToQuickAccess(selector);
                }
                fixture.Show(); fixture.Window.Opacity = 1; await PumpAsync();
                var opener = copied ? fixture.Ribbon.QuickAccessItems.OfType<RibbonDropDownButton>()
                    .Single(item => ReferenceEquals(Ribbon.GetQuickAccessSource(item), selector)) : selector;
                foreach (var density in new[] { RibbonDensity.Touch, RibbonDensity.Compact })
                {
                    opener.IsDropDownOpen = true; await PumpAsync();
                    Assert.True(opener.IsDropDownOpen);
                    var events = new List<string>();
                    RibbonLayoutTransitionEventArgs? ending = null;
                    void Preparing(object? sender, RibbonLayoutTransitionEventArgs e) => events.Add($"Prepare {e.BoundaryMotion.IsAnimated}");
                    void Starting(object? sender, RibbonLayoutTransitionEventArgs e) => events.Add($"Start {e.BoundaryMotion.IsAnimated}");
                    void Ended(object? sender, RibbonLayoutTransitionEventArgs e) { ending = e; events.Add($"End {e.Outcome} {e.BoundaryMotion.Progress}"); }
                    fixture.Ribbon.LayoutTransitionPreparing += Preparing;
                    fixture.Ribbon.LayoutTransitionStarting += Starting;
                    fixture.Ribbon.LayoutTransitionEnded += Ended;
                    try
                    {
                        SelectWriterDensity(fixture, density);
                        await PumpAsync();
                        // Dispatcher/composition scheduling on a hosted runner can exceed 300 ms.
                        var completionWait = System.Diagnostics.Stopwatch.StartNew();
                        while (ending is null && completionWait.Elapsed < TimeSpan.FromSeconds(5))
                        {
                            await Task.Delay(20);
                            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        }
                        Assert.False(opener.IsDropDownOpen);
                        Assert.Equal(density, fixture.Ribbon.Density);
                        Assert.True(ending?.Outcome == RibbonLayoutTransitionOutcome.Completed,
                            $"{placement}/{copied}/{density}: {string.Join("; ", events)}");
                    }
                    finally
                    {
                        fixture.Ribbon.LayoutTransitionPreparing -= Preparing;
                        fixture.Ribbon.LayoutTransitionStarting -= Starting;
                        fixture.Ribbon.LayoutTransitionEnded -= Ended;
                    }
                }
            }
            finally { RibbonAnimation.GlobalLevel = originalLevel; RibbonAnimation.RespectSystemReduceMotion = originalReduce; }
        }, TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task WriterDocumentMotionFollowsSharedClockForModesDensityQatAndPreviewWithoutScalingText()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            var originalLevel = RibbonAnimation.GlobalLevel;
            bool originalReduce = RibbonAnimation.RespectSystemReduceMotion;
            try
            {
                fixture.Window.ApplyAppearance(new WriterAppearancePreferences { Theme = RibbonTheme.Office2024Flat,
                    AnimationLevel = RibbonAnimationLevel.Subtle, RespectSystemReducedMotion = false });
                fixture.Show();
                fixture.Window.Opacity = 1;
                await PumpAsync();
                var presenter = DocumentMotion(fixture);
                var host = (Panel)fixture.Window.FindName("DocumentPresentationHost");
                var surface = (WriterEditorSurface)fixture.Window.FindName("EditorSurface");
                ClickHostButton((RibbonButton)fixture.Window.FindName("ZoomInButton"));
                fixture.Editor.AppendText("Writer keeps this text at its natural size while paper and ruler travel together.");
                fixture.Editor.SelectAll();
                string selected = fixture.Editor.Selection.Text;
                var document = fixture.Editor.Document;
                var page = fixture.Shell.CurrentDocument.PageSettings;
                bool dirty = fixture.Shell.CurrentDocument.IsDirty;
                double zoom = surface.ZoomPercent;
                string folder = Path.Combine(AppContext.BaseDirectory, "writer-motion-diagnostics");
                Directory.CreateDirectory(folder);
                int index = 0;
                foreach (Action change in new Action[]
                {
                    () => fixture.Ribbon.LayoutMode = RibbonLayoutMode.Simplified,
                    () => fixture.Ribbon.LayoutMode = RibbonLayoutMode.Classic,
                    () => SelectWriterDensity(fixture, RibbonDensity.Touch),
                    () => SelectWriterDensity(fixture, RibbonDensity.Compact),
                    () => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow,
                    () => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon,
                    () => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow,
                    () => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar,
                    () => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon,
                    () => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar,
                })
                {
                    await Verify(change);
                }
                fixture.Window.ApplyWriterViewMode(WriterViewMode.PrintPreview, restoreEditorFocus: false);
                await PumpAsync();
                await Verify(() => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon);
                fixture.Window.ApplyWriterViewMode(WriterViewMode.Paper, restoreEditorFocus: false);

                // The existing Crystal overlap must not be painted above the moving QAT.
                fixture.Window.ApplyAppearance(new WriterAppearancePreferences { Theme = RibbonTheme.CrystalLight,
                    Density = RibbonDensity.Touch, ShowRuler = false,
                    AnimationLevel = RibbonAnimationLevel.Subtle, RespectSystemReducedMotion = false });
                await PumpAsync();
                await Verify(() => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow);
                await Verify(() => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar);
                await Verify(() => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon);
                await Verify(() => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TitleBar);
                await Verify(() => fixture.Ribbon.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon);

                async Task Verify(Action change)
                {
                    var before = host.TranslatePoint(new Point(), fixture.Window);
                    var finished = new TaskCompletionSource();
                    RibbonLayoutTransitionEventArgs? started = null;
                    void Start(object? sender, RibbonLayoutTransitionEventArgs e)
                    {
                        started = e;
                        SharedWriterClock(e).Controller!.Pause();
                    }
                    void End(object? sender, RibbonLayoutTransitionEventArgs e) => finished.TrySetResult();
                    fixture.Ribbon.LayoutTransitionStarting += Start;
                    fixture.Ribbon.LayoutTransitionEnded += End;
                    try
                    {
                        change();
                        await fixture.Window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        Assert.True(started is not null, $"Transition {index} did not start");
                        Assert.True(presenter.IsPresenting);
                        var final = host.TranslatePoint(new Point(), started.CoordinateRoot);
                        var first = presenter.PresentedOrigin!.Value;
                        Assert.InRange(Math.Abs(first.Y - fixture.Window.TranslatePoint(before, started.CoordinateRoot).Y), 0, 1);
                        var clock = SharedWriterClock(started);
                        foreach (double fraction in new[] { .15, .5, .85 })
                        {
                            clock.Controller!.SeekAlignedToLastTick(TimeSpan.FromMilliseconds(started.BoundaryMotion.Duration.TimeSpan.TotalMilliseconds * fraction), TimeSeekOrigin.BeginTime);
                            await fixture.Window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                            Assert.True(presenter.IsPresenting);
                            var shown = presenter.PresentedOrigin!.Value;
                            Assert.InRange(shown.Y, Math.Min(first.Y, final.Y) - 1, Math.Max(first.Y, final.Y) + 1);
                            Assert.Same(document, fixture.Editor.Document);
                            Assert.Same(page, fixture.Shell.CurrentDocument.PageSettings);
                            Assert.Equal(dirty, fixture.Shell.CurrentDocument.IsDirty);
                            Assert.Equal(selected, fixture.Editor.Selection.Text);
                            Assert.Equal(zoom, surface.ZoomPercent);
                            Assert.True(host.RenderTransform.Value.IsIdentity);
                            var image = new RenderTargetBitmap((int)fixture.Window.ActualWidth, (int)fixture.Window.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                            image.Render(fixture.Window);
                            var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(image));
                            using var file = File.Create(Path.Combine(folder, $"transition-{index:00}-{fraction}.png")); encoder.Save(file);
                        }
                        clock.Controller!.Resume();
                        await finished.Task.WaitAsync(TimeSpan.FromSeconds(3));
                        await fixture.Window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        Assert.False(presenter.IsPresenting);
                        Assert.Equal(1, host.Opacity);
                        index++;
                    }
                    finally
                    {
                        fixture.Ribbon.LayoutTransitionStarting -= Start;
                        fixture.Ribbon.LayoutTransitionEnded -= End;
                    }
                }
            }
            finally { RibbonAnimation.GlobalLevel = originalLevel; RibbonAnimation.RespectSystemReduceMotion = originalReduce; }
        }, TimeSpan.FromSeconds(90));
    }

    [Fact]
    public async Task WriterDocumentMotionCleansUpInterruptionsAndKeepsAuthoredProperties()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            var originalLevel = RibbonAnimation.GlobalLevel;
            bool originalReduce = RibbonAnimation.RespectSystemReduceMotion;
            try
            {
                fixture.Window.ApplyAppearance(new WriterAppearancePreferences { Theme = RibbonTheme.Office2024Flat,
                    AnimationLevel = RibbonAnimationLevel.Subtle, RespectSystemReducedMotion = false });
                fixture.Show(); await PumpAsync();
                var presenter = DocumentMotion(fixture);
                var host = (Panel)fixture.Window.FindName("DocumentPresentationHost");
                var transform = new TranslateTransform(2, 0);
                host.RenderTransform = transform;
                host.SetBinding(UIElement.OpacityProperty, new Binding { Source = .8d });
                var binding = BindingOperations.GetBinding(host, UIElement.OpacityProperty);
                var clip = new RectangleGeometry(new Rect(host.RenderSize));
                host.Clip = clip;
                host.ApplyAnimationClock(UIElement.OpacityProperty,
                    new DoubleAnimation(.8d, .8d, TimeSpan.FromDays(1)).CreateClock());
                var complete = new TaskCompletionSource();
                fixture.Ribbon.LayoutTransitionEnded += (_, e) =>
                {
                    if (e.TargetState.Density == RibbonDensity.Touch) complete.TrySetResult();
                };
                fixture.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
                Assert.True(presenter.IsPresenting);
                fixture.Ribbon.Density = RibbonDensity.Touch;
                Assert.True(presenter.IsPresenting);
                await complete.Task.WaitAsync(TimeSpan.FromSeconds(3));
                await fixture.Window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                Assert.False(presenter.IsPresenting);
                Assert.Equal(.8, host.Opacity);
                Assert.Same(transform, host.RenderTransform);
                Assert.Same(binding, BindingOperations.GetBinding(host, UIElement.OpacityProperty));
                Assert.True(host.HasAnimatedProperties);

                fixture.Ribbon.LayoutMode = RibbonLayoutMode.Classic;
                Assert.True(presenter.IsPresenting);
                fixture.Window.ApplyWriterViewMode(WriterViewMode.ContinuousEdit, restoreEditorFocus: false);
                Assert.False(presenter.IsPresenting);
                fixture.Ribbon.Density = RibbonDensity.Compact;
                RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None;
                await PumpAsync();
                Assert.False(presenter.IsPresenting);
                fixture.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
                Assert.False(presenter.IsPresenting);
                Assert.Equal(.8, host.Opacity);
                Assert.Same(clip, host.Clip);
            }
            finally { RibbonAnimation.GlobalLevel = originalLevel; RibbonAnimation.RespectSystemReduceMotion = originalReduce; }
        }, TimeSpan.FromSeconds(45));
    }

    [Fact]
    public async Task WriterDocumentMotionRebasesPlayingTransitionsAndSnapsUnsafeCompositionAndGeometryChanges()
    {
        await StaTestHelper.RunAsync(async () =>
        {
            using var fixture = new WindowFixture();
            var originalLevel = RibbonAnimation.GlobalLevel;
            bool originalReduce = RibbonAnimation.RespectSystemReduceMotion;
            try
            {
                fixture.Window.ApplyAppearance(new WriterAppearancePreferences { Theme = RibbonTheme.Office2024Flat,
                    AnimationLevel = RibbonAnimationLevel.Subtle, RespectSystemReducedMotion = false });
                fixture.Show(); await PumpAsync();
                var presenter = DocumentMotion(fixture);
                var host = (Panel)fixture.Window.FindName("DocumentPresentationHost");
                RibbonLayoutTransitionEventArgs? started = null;
                fixture.Ribbon.LayoutTransitionStarting += (_, e) => { started = e; SharedWriterClock(e).Controller!.Pause(); };
                fixture.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
                await fixture.Window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                Assert.NotNull(started);
                SharedWriterClock(started).Controller!.SeekAlignedToLastTick(
                    TimeSpan.FromMilliseconds(started.BoundaryMotion.Duration.TimeSpan.TotalMilliseconds / 2), TimeSeekOrigin.BeginTime);
                await fixture.Window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);
                Point origin = presenter.PresentedOrigin!.Value;
                fixture.Ribbon.Density = RibbonDensity.Touch;
                Assert.True(presenter.IsPresenting);
                Assert.Equal(origin, presenter.PresentedOrigin);
                await fixture.Window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                Assert.True(presenter.IsPresenting); // Ended(old) cannot release the successor.
                fixture.Window.Width += 30;
                fixture.Window.UpdateLayout();
                Assert.False(presenter.IsPresenting);
                await PumpAsync();

                var unsafeVisual = new Border { Width = 8, Height = 8,
                    Background = new VisualBrush(new System.Windows.Shapes.Rectangle { Fill = Brushes.Red }) };
                host.Children.Add(unsafeVisual); fixture.Window.UpdateLayout();
                fixture.Ribbon.Density = RibbonDensity.Compact;
                Assert.False(presenter.IsPresenting);
                host.Children.Remove(unsafeVisual);
                RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None; await PumpAsync();
                RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
                var composition = new TextComposition(System.Windows.Input.InputManager.Current, fixture.Editor, "test");
                fixture.Editor.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, composition)
                    { RoutedEvent = TextCompositionManager.PreviewTextInputStartEvent });
                fixture.Ribbon.LayoutMode = RibbonLayoutMode.Classic;
                Assert.False(presenter.IsPresenting);
                fixture.Editor.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, composition)
                    { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });
                RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None; await PumpAsync();
                Assert.Equal(1, host.Opacity);
                RibbonAnimation.GlobalLevel = RibbonAnimationLevel.Subtle;
                var scale = new ScaleTransform(1.1, 1.1);
                host.RenderTransform = scale;
                fixture.Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
                Assert.False(presenter.IsPresenting);
                Assert.Same(scale, host.RenderTransform);
                RibbonAnimation.GlobalLevel = RibbonAnimationLevel.None; await PumpAsync();
            }
            finally { RibbonAnimation.GlobalLevel = originalLevel; RibbonAnimation.RespectSystemReduceMotion = originalReduce; }
        }, TimeSpan.FromSeconds(45));
    }

    private static WriterDocumentTransition DocumentMotion(WindowFixture fixture) =>
        (WriterDocumentTransition)typeof(MainWindow).GetField("_documentTransition", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture.Window)!;

    private static ClockGroup SharedWriterClock(RibbonLayoutTransitionEventArgs args) =>
        (ClockGroup)((AnimationClock)typeof(RibbonLayoutTransitionMotion).GetField("_clock", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(args.BoundaryMotion)!).Parent!;
}
