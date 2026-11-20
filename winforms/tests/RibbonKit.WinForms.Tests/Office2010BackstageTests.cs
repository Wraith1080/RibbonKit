using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2010BackstageTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Navigation_icons_follow_ink_even_when_artwork_supplies_its_own_color(bool vector) => FoundationTests.Sta(() =>
    {
        const int dpi = 120;
        int Scale(int value) => RibbonLayoutEngine.Scale(value, dpi);
        var theme = RibbonThemes.Office2010Blue;
        var metrics = RibbonPresentationResolver.Backstage(theme).ResolveMetrics(theme, RibbonDensity.Compact);
        using var painter = new RibbonOffice2024Renderer();
        using var row = new Bitmap(240, 52);
        using var graphics = Graphics.FromImage(row);
        using var artwork = new Bitmap(16, 16);
        using (var imageGraphics = Graphics.FromImage(artwork))
            imageGraphics.FillRectangle(Brushes.Crimson, 4, 4, 8, 8);
        using var item = new RibbonBackstagePageItem();
        var context = new RibbonPaintContext(graphics, new(Point.Empty, row.Size), theme, Control.DefaultFont, dpi);
        int iconLeft = Scale(metrics.IconInset), iconSize = Scale(metrics.IconSize);
        int iconTop = (row.Height - iconSize) / 2;

        painter.DrawBackstageEntry(context, item, true);
        var background = row.GetPixel(iconLeft + 1, iconTop + 1);
        if (vector) item.Icon = new FixedColorIcon();
        else item.Image = artwork;
        painter.DrawBackstageEntry(context, item, true);
        Assert.Equal(Color.White.ToArgb(), row.GetPixel(iconLeft + iconSize / 2, row.Height / 2).ToArgb());
        Assert.Equal(background, row.GetPixel(iconLeft + 1, iconTop + 1));

        painter.DrawBackstageEntry(context, item, false);
        Assert.Equal(theme.BackstageNavigationInk.ToArgb(), row.GetPixel(iconLeft + iconSize / 2, row.Height / 2).ToArgb());
        painter.DrawBackstageEntry(context with { Theme = theme with { BackstageMonochromeIcons = false } }, item, true);
        Assert.Equal(Color.Crimson.ToArgb(), row.GetPixel(iconLeft + iconSize / 2, row.Height / 2).ToArgb());
    });

    private sealed class FixedColorIcon : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context)
            => graphics.FillRectangle(Brushes.Crimson, Rectangle.Inflate(bounds, -bounds.Width / 4, -bounds.Height / 4));
    }

    [Fact]
    public void Registration_is_a_sibling_with_explicit_below_tab_caption_and_default_roles() => FoundationTests.Sta(() =>
    {
        Assert.Equal(0, (int)RibbonBackstageStyle.Modern); Assert.Equal(1, (int)RibbonBackstageStyle.Classic); Assert.Equal(2, (int)RibbonBackstageStyle.Office2010);
        var theme = RibbonThemes.Office2010Blue; var p = RibbonPresentationResolver.Backstage(theme);
        Assert.Equal(typeof(RibbonBackstageRenderer), p.Renderer.GetType().BaseType); Assert.Equal(typeof(RibbonBackstageLayoutProvider), p.Layout.GetType().BaseType);
        Assert.Equal(RibbonBackstagePlacement.BelowTabs, p.Placement); Assert.False(p.Caption.Resolve(theme).TintNeutralSeparateCaption);
        Assert.False(p.Caption.Resolve(theme with { BackstageExtendsIntoTitleBar = true, BackstageTitleCentered = true }).ExtendsIntoTitleBar);
        var metrics = p.ResolveMetrics(theme, RibbonDensity.Compact); Assert.False(metrics.BackButtonVisible); Assert.Equal(3, metrics.TopSeamHeight); Assert.Equal(6, metrics.PageDividerShadowWidth);
        Assert.Equal(Color.White, theme.BackstageSelectedForeground); Assert.True(theme.BackstageFullWidthNavigation);
        var custom = RibbonThemes.Office2024Light with { BackstageStyle = RibbonBackstageStyle.Office2010 };
        Assert.Equal(RibbonBackstagePlacement.BelowTabs, RibbonPresentationResolver.Backstage(custom).Placement);
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Seam_back_visibility_and_left_shadow_are_independently_adjustable_in_layout_and_paint(int dpi) => FoundationTests.Sta(() =>
    {
        int S(int n) => RibbonLayoutEngine.Scale(n, dpi);
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue };
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Home" });
        var theme = ribbon.Theme;
        var layout = RibbonBackstageLayout.Build(new(S(700), S(400)), Control.DefaultFont, dpi, ribbon.Backstage);
        Assert.True(layout.Back.IsEmpty); Assert.Equal(S(3), layout.Navigation.Top); Assert.Equal(S(3), layout.Page.Top);
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            using var image = new Bitmap(S(700), S(400)); using var g = Graphics.FromImage(image);
            var c = new RibbonPaintContext(g, new(Point.Empty, image.Size), theme, Control.DefaultFont, dpi);
            renderer.DrawBackstageBackground(c, layout.Page.Left);
            Assert.Equal(RibbonFileButtonRenderer.OpenFill(theme).End.ToArgb(), image.GetPixel(image.Width - 1, 0).ToArgb());
            Assert.Equal(Color.White.ToArgb(), image.GetPixel(layout.Page.Left + S(10), S(20)).ToArgb());
            int y = S(150); Assert.True(image.GetPixel(layout.Page.Left - 1, y).R < image.GetPixel(layout.Page.Left - S(10), y).R);
            var noShadow = theme with { BackstageAppearance = new() { PageDividerShadowOpacity = 0 } };
            renderer.DrawBackstageBackground(c with { Theme = noShadow }, layout.Page.Left);
            Assert.Equal(image.GetPixel(layout.Page.Left - S(10), y), image.GetPixel(layout.Page.Left - 1, y));
            var seam = theme with { BackstageAppearance = new() { TopSeamHeight = 5, TopSeamBackground = new(Color.Lime), PageDividerShadowColor = Color.Transparent } };
            renderer.DrawBackstageBackground(c with { Theme = seam }, layout.Page.Left); Assert.Equal(Color.Lime.ToArgb(), image.GetPixel(image.Width - 1, S(5) - 1).ToArgb());
            var accent = theme with { Accent = Color.Purple };
            renderer.DrawBackstageBackground(c with { Theme = accent }, layout.Page.Left); Assert.Equal(RibbonFileButtonRenderer.OpenFill(accent).End.ToArgb(), image.GetPixel(50, 0).ToArgb());
            var row = new Rectangle(0, S(3), layout.Page.Left, S(42)); using var item = new RibbonButtonItem { Text = "" };
            renderer.DrawBackstageEntry(c with { Theme = accent with { BackstageAppearance = new() { NavigationBorderColor = Color.Transparent } }, Bounds = row }, item, true);
            Assert.NotEqual(theme.BackstageSelectedFill, accent.BackstageSelectedFill);
            Assert.Equal(accent.BackstageSelectedFill.End.ToArgb(), image.GetPixel(row.Width / 2, row.Bottom - 1).ToArgb());
        }
        ribbon.Theme = theme with { BackstageAppearance = new() { BackButtonVisible = true, TopSeamHeight = 0 } };
        var shown = RibbonBackstageLayout.Build(new(S(700), S(400)), Control.DefaultFont, dpi, ribbon.Backstage);
        Assert.False(shown.Back.IsEmpty); Assert.True(shown.Navigation.Top > shown.Back.Bottom); Assert.Equal(0, shown.Page.Top);
        ribbon.Theme = ribbon.Theme with { BackstageAppearance = new() { TopSeamHeight = 0 } };
        Assert.Equal(0, RibbonBackstageLayout.Build(new(S(700), S(400)), Control.DefaultFont, dpi, ribbon.Backstage).Navigation.Top);
        foreach (var size in new[] { Size.Empty, new Size(1, 1), new Size(7, 3) })
        {
            var tiny = RibbonBackstageLayout.Build(size, Control.DefaultFont, dpi, ribbon.Backstage);
            Assert.True(tiny.Page.Width >= 0 && tiny.Page.Height >= 0 && tiny.Navigation.Height >= 0);
        }
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Native_host_uses_measured_headers_through_font_density_qat_and_nested_reflow(bool ordinary) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(ordinary); f.Open(); var selected = f.Ribbon.SelectedTab;
        var handle = f.Editor.Handle; var parent = f.Editor.Parent; var contentParent = f.Page.Content!.Parent;
        foreach (var mode in Enum.GetValues<RibbonLayoutMode>())
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var qat in Enum.GetValues<RibbonQuickAccessPosition>())
        {
            f.Ribbon.LayoutMode = mode; f.Ribbon.Density = density; f.Ribbon.QuickAccessPosition = qat;
            f.Form.ClientSize = new(1050, 720); f.Container.Location = new(12, 17);
            AssertAnchor();
            using var large = new Font("Segoe UI", 18); var previous = f.Ribbon.Font; f.Ribbon.Font = large; AssertAnchor(); f.Ribbon.Font = previous;
            Assert.Same(selected, f.Ribbon.SelectedTab); Assert.Equal(handle, f.Editor.Handle); Assert.Same(parent, f.Editor.Parent); Assert.Same(contentParent, f.Page.Content!.Parent);
            Assert.Equal("Native content retained", f.Editor.Text); Assert.True(f.Ribbon.ShowQuickAccess);
            Assert.False(f.Ribbon.HasBackstageCaption); Assert.False(f.Ribbon.BackstageSurface!.Controls.Cast<Control>().Single(c => c.AccessibleName == "Back").Visible);
        }
        void AssertAnchor()
        {
            Application.DoEvents(); int bottom = f.Ribbon.Snapshot.Targets.Where(t => t.Kind is TargetKind.Tab or TargetKind.FileMenu).Max(t => t.Bounds.Bottom);
            Assert.Equal(f.Form.PointToClient(f.Ribbon.PointToScreen(new(0, bottom))).Y, f.Ribbon.BackstageSurface!.Top);
            Assert.Equal(f.Form.ClientSize.Width, f.Ribbon.BackstageSurface.Width); Assert.Equal(f.Form.ClientSize.Height - f.Ribbon.BackstageSurface.Top, f.Ribbon.BackstageSurface.Height);
        }
    });

    [Fact]
    public void Header_actions_close_select_and_respect_page_validation_without_changing_models() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Open(); var home = f.Ribbon.SelectedTab; var handle = f.Editor.Handle; var parent = f.Editor.Parent;
        f.BlockValidation = true;
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, f.Other)));
        Assert.True(f.Ribbon.IsBackstageOpen); Assert.Same(home, f.Ribbon.SelectedTab);
        f.BlockValidation = false;
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, f.Other)));
        Assert.False(f.Ribbon.IsBackstageOpen); Assert.Same(f.Other, f.Ribbon.SelectedTab);
        f.Open(); f.Ribbon.SelectedTab = f.Other; Assert.False(f.Ribbon.IsBackstageOpen); // Same selected tab also closes.
        f.Open(); f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.QuickAccess)); Assert.False(f.Ribbon.IsBackstageOpen); Assert.Equal(1, f.Invocations);
        f.Open(); f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => t.Kind == TargetKind.FileMenu)); Assert.False(f.Ribbon.IsBackstageOpen);
        Assert.Equal(handle, f.Editor.Handle); Assert.Same(parent, f.Editor.Parent); Assert.Equal("Native content retained", f.Editor.Text);
    });

    [Fact]
    public void Rendered_selection_is_suppressed_and_restored_while_native_pages_and_title_stay_owned() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var renderer = new SelectionProbe(); f.Ribbon.Renderer = renderer;
        var logical = f.Ribbon.SelectedTab;
        f.Open(); using var image = new Bitmap(f.Ribbon.Width, f.Ribbon.Height); f.Ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
        Assert.Same(logical, f.Ribbon.SelectedTab); Assert.False(renderer.Selected); Assert.True(renderer.Connection.IsEmpty);
        f.Ribbon.IsBackstageOpen = false; f.Ribbon.DrawToBitmap(image, new(Point.Empty, image.Size)); Assert.True(renderer.Selected); Assert.False(renderer.Connection.IsEmpty);
    });

    [Fact]
    public void Motion_close_cancel_reversal_and_resize_keep_controls_without_new_handles() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; f.Ribbon.Motion.SystemPolicyOverride = true;
        f.Open(); var handle = f.Editor.Handle; var parent = f.Editor.Parent;
        var motion = f.Ribbon.BackstageOpeningMotion; Assert.NotNull(motion); motion!.SetProgress(.45);
        Assert.True(f.Ribbon.BackstageSurface!.Left < 0, "Host layout must not reset whole-pane travel on a motion frame.");
        Assert.True(f.Ribbon.BackstageSurface!.Top > f.Ribbon.WindowCaptionHeight); f.Ribbon.IsBackstageOpen = false;
        Assert.True(f.Ribbon.IsBackstageOpen); Assert.True(f.Ribbon.BackstageOpeningMotion!.IsClosing);
        f.Ribbon.IsBackstageOpen = true; Assert.True(f.Ribbon.IsBackstageOpen); Assert.Equal(handle, f.Editor.Handle); Assert.Same(parent, f.Editor.Parent);
        f.Form.ClientSize = new(880, 640); Application.DoEvents();
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.None; f.Ribbon.IsBackstageOpen = false; Assert.False(f.Ribbon.IsBackstageOpen);
        Assert.Equal(handle, f.Editor.Handle); Assert.Same(parent, f.Editor.Parent);
    });

    [Fact]
    public void Contextual_and_merged_headers_close_normally_and_restore_their_own_selected_connection() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var source = new RibbonMergeSource();
        var tools = new RibbonTab { Text = "Tools", IsContextual = true, ContextualColor = Color.Purple };
        source.Tabs.Add(tools); f.Ribbon.Merge(source); f.Ribbon.SelectedTab = tools;
        var parent = tools.Parent; f.Open();
        using var probe = new SelectionProbe { Name = "Tools" }; f.Ribbon.Renderer = probe;
        using var image = new Bitmap(f.Ribbon.Width, f.Ribbon.Height); f.Ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
        Assert.False(probe.Selected); Assert.True(probe.Connection.IsEmpty); Assert.Same(tools, f.Ribbon.SelectedTab);
        f.Ribbon.Activate(f.Ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, tools)));
        f.Ribbon.DrawToBitmap(image, new(Point.Empty, image.Size));
        Assert.False(f.Ribbon.IsBackstageOpen); Assert.True(probe.Selected); Assert.False(probe.Connection.IsEmpty); Assert.Same(parent, tools.Parent);
        f.Open(); tools.Visible = false; Assert.NotSame(tools, f.Ribbon.SelectedTab);
        Assert.True(f.Ribbon.IsBackstageOpen); f.Ribbon.Unmerge(source); Assert.True(f.Ribbon.IsBackstageOpen);
    });

    [Fact]
    public void Header_navigation_cycles_with_native_page_focus_and_keeps_rtf_selection() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); f.Editor.Rtf = @"{\rtf1\ansi Native \b content\b0 retained}"; f.Editor.Select(2, 7);
        f.Open(); var rtf = f.Editor.Rtf; var handle = f.Editor.Handle; var parent = f.Editor.Parent;
        Assert.True(f.Editor.Focus());
        Assert.True(f.Ribbon.ProcessBackstageKey(Keys.F6)); Assert.True(f.Ribbon.Focused); Assert.Same(f.Ribbon.FileMenu, f.Ribbon.FocusedElement);
        Assert.True(f.Ribbon.ProcessRibbonKey(Keys.F6)); Assert.False(f.Ribbon.ContainsFocus); Assert.True(f.Ribbon.BackstageSurface!.ContainsFocus);
        Assert.True(f.Ribbon.ProcessBackstageKey(Keys.F6)); Assert.True(f.Editor.Focused);
        f.BlockValidation = true; f.Ribbon.ProcessBackstageKey(Keys.F6); Assert.True(f.Editor.Focused);
        f.BlockValidation = false; f.Ribbon.ProcessBackstageKey(Keys.F6); Assert.True(f.Ribbon.Focused);
        f.Ribbon.FocusElement(f.Other); f.Ribbon.ProcessRibbonKey(Keys.Enter);
        Assert.False(f.Ribbon.IsBackstageOpen); Assert.Same(f.Other, f.Ribbon.SelectedTab);
        Assert.Equal(rtf, f.Editor.Rtf); Assert.Equal(2, f.Editor.SelectionStart); Assert.Equal(7, f.Editor.SelectionLength); Assert.Equal(handle, f.Editor.Handle); Assert.Same(parent, f.Editor.Parent);
    });

    [Theory] [InlineData(RibbonTabContentAnimation.Fade)] [InlineData(RibbonTabContentAnimation.SlideDown)]
    public void Native_page_reversal_keeps_borrowed_rtf_selection_and_measured_pane(RibbonTabContentAnimation style) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); using var print = new TextBox { Text = "Print settings", Dock = DockStyle.Fill };
        var page = new RibbonBackstagePageItem { Text = "Print", Content = print }; f.Ribbon.Backstage.Items.Add(page);
        f.Editor.Rtf = @"{\rtf1\ansi Native \b content\b0 retained}"; f.Editor.Select(2, 7); f.Open();
        var rtf = f.Editor.Rtf; var handle = f.Editor.Handle; var parent = f.Editor.Parent; var pane = f.Ribbon.BackstageSurface!.Bounds;
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; f.Ribbon.Motion.SystemPolicyOverride = true; f.Ribbon.TabContentAnimation = style;
        f.Ribbon.Backstage.SelectedPage = page; var motion = Assert.IsType<RibbonBackstagePageMotion>(f.Ribbon.BackstageSurface.PageMotion); motion.SetProgress(.45);
        Assert.Equal(pane, f.Ribbon.BackstageSurface.Bounds);
        f.Ribbon.Backstage.SelectedPage = f.Page; f.Ribbon.SettleMotion();
        Assert.Equal(pane, f.Ribbon.BackstageSurface.Bounds); Assert.Equal(rtf, f.Editor.Rtf); Assert.Equal(2, f.Editor.SelectionStart); Assert.Equal(7, f.Editor.SelectionLength);
        Assert.Equal(handle, f.Editor.Handle); Assert.Same(parent, f.Editor.Parent); Assert.Same(f.Editor, f.Page.Content); Assert.Same(print, page.Content);
    });

    [Fact]
    public void Appearance_and_identity_ranges_fail_before_effective_theme_mutation() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(); var before = f.Ribbon.Theme;
        foreach (var a in new[] { new RibbonBackstageAppearanceSettings { TopSeamHeight = -1 }, new() { TopSeamHeight = 33 }, new() { PageDividerShadowWidth = 65 }, new() { PageDividerShadowOpacity = double.NaN } })
        { Assert.Throws<ArgumentOutOfRangeException>(() => f.Ribbon.Theme = before with { BackstageAppearance = a }); Assert.Same(before, f.Ribbon.Theme); }
        Assert.Throws<ArgumentOutOfRangeException>(() => f.Ribbon.Theme = before with { IdentityAccent = before.IdentityAccent! with { NavigationStrength = double.PositiveInfinity } });
        Assert.Same(before, f.Ribbon.Theme);
    });

    private sealed class SelectionProbe : RibbonOffice2024Renderer
    {
        internal string Name = "Home";
        internal bool Selected; internal Rectangle Connection;
        public override void DrawTab(RibbonPaintContext c, RibbonTab tab, bool selected)
        { if (tab.Text == Name) { Selected = selected; Connection = c.HeaderBodyConnectionBounds; } base.DrawTab(c, tab, selected); }
    }
    private sealed class Fixture : IDisposable
    {
        internal readonly Form Form; internal readonly Panel Container = new() { Location = new(8, 11), Size = new(940, 640) };
        internal readonly RibbonControl Ribbon = new() { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.None, QuickAccessVisible = true, QuickAccessPosition = RibbonQuickAccessPosition.TabRow };
        internal readonly RichTextBox Editor = new() { Text = "Native content retained", Dock = DockStyle.Fill };
        internal readonly RibbonBackstagePageItem Page; internal readonly RibbonTab Other = new() { Text = "Edit" }; internal bool BlockValidation; internal int Invocations;
        internal Fixture(bool ordinary = false)
        {
            Form = ordinary ? new Form() : new RibbonForm(); Form.StartPosition = FormStartPosition.Manual; Form.Bounds = new(-16000, -16000, 1000, 720); Form.ShowInTaskbar = false;
            if (ordinary) { Form.Controls.Add(Container); Container.Controls.Add(Ribbon); }
            else { Form.Controls.Add(Ribbon); ((RibbonForm)Form).Ribbon = Ribbon; }
            var home = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" }; var command = new RibbonButtonItem { Text = "Save" }; command.Click += (_, _) => Invocations++;
            group.Items.Add(command); home.Groups.Add(group); Ribbon.Tabs.AddRange(home, Other); Ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command });
            Page = new() { Text = "Home", Content = Editor }; Ribbon.Backstage.Items.Add(Page); Ribbon.Backstage.FooterItems.Add(new RibbonButtonItem { Text = "Options" }); Ribbon.FileMenu.Visible = true;
            Editor.Validating += (_, e) => e.Cancel = BlockValidation;
            Form.Show(); Application.DoEvents();
        }
        internal void Open() { Ribbon.IsBackstageOpen = true; Application.DoEvents(); Assert.True(Ribbon.IsBackstageOpen); }
        public void Dispose() { BlockValidation = false; Ribbon.AnimationLevel = RibbonAnimationLevel.None; Form.Dispose(); Ribbon.Dispose(); Container.Dispose(); }
    }
}
