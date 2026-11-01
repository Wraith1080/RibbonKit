using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstagePageMotionTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly RibbonForm Host;
        internal readonly RibbonControl Ribbon = new() { FileMenuMode = RibbonFileMenuMode.Backstage };
        internal readonly ProbePage First = new(), Second = new();
        internal readonly TextBox Field = new() { Text = "Retain native text", Bounds = new(12, 12, 200, 28) };
        internal readonly RibbonBackstagePageItem A, B;
        internal RibbonBackstageSurface Surface => Ribbon.BackstageSurface!;
        internal Fixture(RibbonWindowBackdrop backdrop = RibbonWindowBackdrop.None)
        {
            Application.EnableVisualStyles();
            Host = new() { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 950, 650), ShowInTaskbar = false, Backdrop = backdrop };
            First.Controls.Add(Field); Second.Controls.Add(new TextBox { Text = "Print settings" });
            A = new() { Text = "Info", Content = First }; B = new() { Text = "Print", Content = Second };
            Ribbon.FileMenu.Visible = true; Ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
            Ribbon.Backstage.Items.AddRange(A, B); Host.Controls.Add(Ribbon); Host.Ribbon = Ribbon;
            Host.Show(); Application.DoEvents(); Ribbon.Motion.SystemPolicyOverride = true;
            Ribbon.IsBackstageOpen = true; _ = First.Handle; _ = Second.Handle;
        }
        public void Dispose() { Host.Dispose(); First.Dispose(); Second.Dispose(); }
    }
    private sealed class ProbePage : Panel
    {
        internal int Captures;
        protected override void WndProc(ref Message message)
        {
            if (message.Msg is 0x317 or 0x318 && Environment.StackTrace.Contains("Capture", StringComparison.Ordinal)
                && Environment.StackTrace.Contains("RibbonKit.WinForms.", StringComparison.Ordinal)) Captures++;
            base.WndProc(ref message);
        }
    }

    [Theory]
    [InlineData(RibbonTabContentAnimation.Fade, RibbonWindowBackdrop.None)]
    [InlineData(RibbonTabContentAnimation.SlideDown, RibbonWindowBackdrop.None)]
    [InlineData(RibbonTabContentAnimation.Fade, RibbonWindowBackdrop.Mica)]
    [InlineData(RibbonTabContentAnimation.SlideDown, RibbonWindowBackdrop.Mica)]
    [InlineData(RibbonTabContentAnimation.Fade, RibbonWindowBackdrop.Acrylic)]
    [InlineData(RibbonTabContentAnimation.SlideDown, RibbonWindowBackdrop.Acrylic)]
    public void Incoming_native_page_follows_shared_style_without_moving_navigation_or_capturing_content(RibbonTabContentAnimation style, RibbonWindowBackdrop material) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(material); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; f.Ribbon.TabContentAnimation = style;
        var parentA = f.First.Parent; var parentB = f.Second.Parent; var handleA = f.First.Handle; var handleB = f.Second.Handle; var fieldHandle = f.Field.Handle;
        var fieldBounds = f.Field.Bounds; var nav = f.Surface.NavigationBounds; var bounds = f.Surface.Bounds;
        f.Field.Select(2, 4); int changes = 0; f.Ribbon.Backstage.SelectedPageChanged += (_, _) => changes++;
        f.Ribbon.Backstage.SelectedPage = f.B;
        var motion = Assert.IsType<RibbonBackstagePageMotion>(f.Surface.PageMotion);
        Assert.True(f.Ribbon.IsAnimating); Assert.Equal(style, motion.Style); Assert.Equal(182, motion.Duration);
        Assert.Same(f.B, f.Ribbon.Backstage.SelectedPage); Assert.Equal(1, changes);
        int previous = int.MinValue;
        foreach (double progress in new[] { 0, .25, .5, .75, 1 })
        {
            motion.SetProgress(progress);
            if (style == RibbonTabContentAnimation.Fade)
            { Assert.Equal(Point.Empty, motion.Target.Location); if (progress is > 0 and < 1) Assert.InRange(motion.Opacity, 2, 254); }
            else
            { Assert.Equal(255, motion.Opacity); Assert.InRange(motion.Target.Top, previous, 0); previous = motion.Target.Top; if (progress == 0) Assert.True(previous < 0); }
            Assert.Equal(nav, f.Surface.NavigationBounds); Assert.Equal(bounds, f.Surface.Bounds); Assert.Equal(255, f.Surface.MotionAlpha);
            Assert.Same(parentA, f.First.Parent); Assert.Same(parentB, f.Second.Parent);
            Assert.Equal(handleA, f.First.Handle); Assert.Equal(handleB, f.Second.Handle); Assert.Equal(fieldHandle, f.Field.Handle);
            Assert.Equal(fieldBounds, f.Field.Bounds); Assert.Equal(2, f.Field.SelectionStart); Assert.Equal(4, f.Field.SelectionLength);
        }
        f.Ribbon.SettleMotion(); Assert.False(f.Ribbon.IsAnimating); Assert.Equal(Point.Empty, parentB!.Location);
        Assert.Equal(255, motion.Target.PageOpacity); Assert.Equal(0, f.First.Captures + f.Second.Captures);
        f.Ribbon.Backstage.SelectedPage = f.A; Assert.True(f.Ribbon.IsAnimating); f.Ribbon.SettleMotion();
        Assert.Equal("Retain native text", f.Field.Text); Assert.Single(f.Field.Parent!.Controls.Cast<Control>());
    });

    [Theory]
    [InlineData("none")] [InlineData("system")] [InlineData("batch")] [InlineData("validation")]
    public void Existing_policy_and_validation_keep_immediate_or_cancelled_selection(string condition) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        if (condition == "none") f.Ribbon.AnimationLevel = RibbonAnimationLevel.None;
        if (condition == "system") f.Ribbon.Motion.SystemPolicyOverride = false;
        if (condition == "batch") f.Ribbon.BeginUpdate();
        if (condition == "validation") f.Field.Validating += (_, e) => e.Cancel = true;
        f.Ribbon.Backstage.SelectedPage = f.B;
        Assert.False(f.Ribbon.IsAnimating); Assert.Same(condition == "validation" ? f.A : f.B, f.Ribbon.Backstage.SelectedPage);
        if (condition == "batch") f.Ribbon.EndUpdate();
    });

    [Theory]
    [InlineData("style")] [InlineData("level")] [InlineData("resize")] [InlineData("input")]
    [InlineData("repeat")] [InlineData("replace")] [InlineData("remove")] [InlineData("close")]
    public void Interruptions_restore_slot_opacity_bounds_and_native_ownership(string action) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        f.Ribbon.Backstage.SelectedPage = f.B; var motion = f.Surface.PageMotion!; var slot = motion.Target; var contentParent = f.Second.Parent;
        Assert.True(f.Ribbon.IsAnimating);
        using var replacement = new Panel();
        switch (action)
        {
            case "style": f.Ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown; break;
            case "level": f.Ribbon.AnimationLevel = RibbonAnimationLevel.None; break;
            case "resize": f.Host.Width -= 40; break;
            case "input": var input = Message.Create(f.Second.Handle, 0x100, (nint)Keys.A, 0); f.Ribbon.ObserveMotionInput(ref input); break;
            case "repeat": f.Ribbon.Backstage.SelectedPage = f.A; break;
            case "replace": f.B.Content = replacement; break;
            case "remove": f.Ribbon.Backstage.Items.Remove(f.B); break;
            case "close": f.Ribbon.IsBackstageOpen = false; break;
        }
        Assert.False(motion.IsRunning); Assert.Equal(255, motion.Opacity);
        if (!slot.IsDisposed) { Assert.Equal(Point.Empty, slot.Location); Assert.Equal(255, slot.PageOpacity); }
        Assert.False(f.Second.IsDisposed);
        if (action is not ("replace" or "remove")) Assert.Same(contentParent, f.Second.Parent);
        if (action == "close") { Assert.True(f.Ribbon.BackstageOpeningMotion!.IsClosing); f.Ribbon.SettleMotion(); Assert.False(f.Ribbon.IsBackstageOpen); }
        else Assert.False(f.Ribbon.IsAnimating);
    });

    [Fact]
    public void Disposing_an_active_slide_keeps_borrowed_content_alive_and_releases_native_motion() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; f.Ribbon.TabContentAnimation = RibbonTabContentAnimation.SlideDown;
        f.Ribbon.Backstage.SelectedPage = f.B; var motion = f.Surface.PageMotion!;
        Assert.True(motion.IsRunning); f.Ribbon.Dispose(); Assert.False(motion.IsRunning);
        Assert.Null(f.Second.Parent); Assert.False(f.Second.IsDisposed); Assert.False(f.Ribbon.IsAnimating);
    });
}
