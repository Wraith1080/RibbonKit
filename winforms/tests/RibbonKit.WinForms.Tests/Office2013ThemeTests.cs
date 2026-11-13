using System.CodeDom;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Showcase.Persistence;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2013ThemeTests
{
    [Fact]
    public void Accent_defaults_explicit_legacy_fill_assignments_and_independent_reset_keep_ownership() => FoundationTests.Sta(() =>
    {
        var theme = RibbonThemes.Office2013Light;
        Assert.Equal(0, (int)RibbonBuiltInTheme.Office2024Light); Assert.Equal(1, (int)RibbonBuiltInTheme.Office2019Light); Assert.Equal(2, (int)RibbonBuiltInTheme.Office2013Light);
        Assert.Equal(RibbonWindowCornerPreference.Square, theme.WindowCornerPreference);
        Assert.False(theme.ExtendTitleBarColorToHeader); Assert.Equal(Color.White, theme.Background);
        Assert.Null(theme.HoverOverride); Assert.Null(theme.PressedOverride); Assert.Null(theme.CheckedOverride);
        var changed = theme with { Accent = Color.DarkOrange };
        Assert.NotEqual(theme.Hover, changed.Hover); Assert.NotEqual(theme.Pressed, changed.Pressed); Assert.NotEqual(theme.Checked, changed.Checked);
        var glass = theme with { Surface = Color.Transparent };
        Assert.InRange(glass.Hover.Start.A, 25, 26); Assert.Equal(theme.Accent.R, glass.Hover.Start.R); Assert.Equal(theme.Accent.G, glass.Hover.Start.G);
        var gradient = new RibbonFill(Color.FromArgb(80, Color.Red), Color.Transparent);
        var authored = changed with { Hover = gradient, Pressed = new(Color.Transparent), Checked = new(Color.Purple) };
        var recolored = authored with { Accent = Color.ForestGreen };
        Assert.Equal(gradient, recolored.Hover); Assert.Equal(Color.Transparent, recolored.Pressed.Start); Assert.Equal(Color.Purple, recolored.Checked.Start);
        recolored = recolored with { HoverOverride = null };
        Assert.NotEqual(gradient, recolored.Hover); Assert.Equal(Color.Transparent, recolored.Pressed.Start); Assert.Equal(Color.Purple, recolored.Checked.Start);
        Assert.Equal(gradient, authored.Hover); Assert.Same(theme, RibbonThemes.Office2013Light);
        Assert.Equal(new RibbonFill(Color.FromArgb(230, 230, 230)), RibbonThemes.Office2024Light.Hover);
        Assert.Equal(new RibbonFill(Color.FromArgb(222, 220, 218)), RibbonThemes.Office2019Light.Hover);
        Assert.Equal(RibbonThemes.Office2019Light.Hover, (RibbonThemes.Office2019Light with { Accent = Color.Red }).Hover);
        Assert.NotEqual(RibbonThemes.Office2019Light.Checked, (RibbonThemes.Office2019Light with { Accent = Color.Red }).Checked);
        var zero = theme with { AccentStates = new() { HoverOpacity = 0, PressedOpacity = 0, CheckedOpacity = 0, CheckedHoverOpacity = 0 } };
        Assert.Equal(theme.Surface.ToArgb(), zero.Hover.Start.ToArgb()); Assert.Equal(theme.Surface.ToArgb(), zero.CommandStateFill(true, false, true).Start.ToArgb());
        using var ribbon = new RibbonControl { Theme = recolored };
        var property = TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2013Light; Assert.Same(theme, ribbon.Theme);
        property.ResetValue(ribbon); Assert.Same(recolored, ribbon.Theme);
    });

    [Theory] [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] [InlineData(-.01)] [InlineData(1.01)]
    public void Invalid_wash_and_file_strengths_are_rejected_before_theme_or_selection_mutation(double invalid) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2013Light };
        var before = ribbon.Theme; var application = ribbon.ApplicationTheme;
        foreach (var bad in new[]
        {
            before with { AccentStates = new() { HoverOpacity = invalid } }, before with { AccentStates = new() { PressedOpacity = invalid } },
            before with { AccentStates = new() { CheckedOpacity = invalid } }, before with { AccentStates = new() { CheckedHoverOpacity = invalid } },
            before with { FileButtonAppearance = new() { HoverDarkening = invalid } }, before with { FileButtonAppearance = new() { PressedDarkening = invalid } },
            before with { FileButtonAppearance = new() { OpenDarkening = invalid } }
        })
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = bad);
            Assert.Same(before, ribbon.Theme); Assert.Same(application, ribbon.ApplicationTheme); Assert.Equal(RibbonBuiltInTheme.Office2013Light, ribbon.BuiltInTheme);
        }
    });

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Public_command_toggle_split_qat_menu_and_file_paint_follow_accent_at_each_dpi(int dpi) => FoundationTests.Sta(() =>
    {
        foreach (var accent in new[] { Color.FromArgb(43, 87, 154), Color.DarkOrange, Color.ForestGreen })
        foreach (bool office in new[] { false, true })
        {
            using RibbonRenderer painter = office ? new RibbonOffice2024Renderer() : new RibbonRenderer();
            var theme = RibbonThemes.Office2013Light with { Accent = accent };
            using var image = new Bitmap(220, 110); using var graphics = Graphics.FromImage(image);
            using var toggle = new RibbonToggleItem { Checked = true };
            using var reference = new RibbonQuickAccessReference { Source = toggle };
            using var owner = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup();
            group.Items.Add(toggle); tab.Groups.Add(group); owner.Tabs.Add(tab); owner.QuickAccessItems.Add(reference);
            using var split = new RibbonSplitButtonItem { Checked = true };
            var context = new RibbonPaintContext(graphics, new(10, 10, 100, 60), theme, Control.DefaultFont, dpi);
            using var inkProbe = new InkProbe(); inkProbe.DrawTab(context, tab, true); Assert.Equal(accent, inkProbe.Ink);
            inkProbe.DrawTab(context with { Theme = theme with { HeaderSelectedForeground = Color.Purple } }, tab, true); Assert.Equal(Color.Purple, inkProbe.Ink);
            foreach (var (hot, pressed, check) in new[] { (true, false, false), (false, true, false), (false, false, true), (true, false, true) })
            {
                toggle.Checked = check;
                var c = context with { Hot = hot, Pressed = pressed };
                var expected = theme.CommandStateFill(hot, pressed, check).Start;
                foreach (bool menu in new[] { false, true })
                {
                    graphics.Clear(Color.White); painter.DrawItem(c, toggle, RibbonItemSize.Small, menu);
                    Assert.Equal(expected.ToArgb(), image.GetPixel(15, 15).ToArgb());
                    graphics.Clear(Color.White); painter.DrawQuickAccessItem(c, reference, menu);
                    Assert.Equal(expected.ToArgb(), image.GetPixel(15, 15).ToArgb());
                }
                split.Checked = check;
                graphics.Clear(Color.White); painter.DrawSplitItem(c, c with { Bounds = new(110, 10, 25, 60), Hot = false, Pressed = false }, split, RibbonItemSize.Small, false);
                Assert.Equal(expected.ToArgb(), image.GetPixel(15, 15).ToArgb());
            }
            using var file = new RibbonDropDownItem { Text = "" };
            foreach (var (hot, pressed, darkness) in new[] { (false, false, 0d), (true, false, .10), (true, true, .20) })
            {
                graphics.Clear(Color.Magenta); painter.DrawFileButton(context with { Hot = hot, Pressed = pressed }, file);
                Assert.Equal(RibbonFileButtonAppearanceSettings.Darken(accent, darkness).Start.ToArgb(), image.GetPixel(15, 15).ToArgb());
            }
            var transparent = theme with { FileButtonAppearance = new() { Background = new(Color.Transparent), HoverBackground = new(Color.Transparent) } };
            graphics.Clear(Color.Magenta); painter.DrawFileButton(context with { Theme = transparent, Hot = true }, file);
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(15, 15).ToArgb());
        }
    });

    [Theory] [InlineData(RibbonFileMenuMode.CommandMenu)] [InlineData(RibbonFileMenuMode.Backstage)]
    public void File_open_close_reopen_disabled_ink_and_focus_use_actual_renderer_hooks(RibbonFileMenuMode mode) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Bounds = new(-16000, -16000, 1000, 600), StartPosition = FormStartPosition.Manual };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2013Light, FileMenuMode = mode, AnimationLevel = RibbonAnimationLevel.None };
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" }); ribbon.FileMenu.Visible = true; ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Open" });
        using var field = new TextBox { Text = "Retained" }; ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = field });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        using var painter = new InkProbe(); using var image = new Bitmap(100, 50); using var g = Graphics.FromImage(image);
        var c = new RibbonPaintContext(g, new(0, 0, 100, 50), ribbon.Theme, Control.DefaultFont, 96);
        for (int i = 0; i < 2; i++)
        {
            ribbon.FileMenu.PerformClick(); Assert.True(ribbon.FileMenu.IsDropDownOpen);
            g.Clear(Color.Magenta); painter.DrawFileButton(c with { Focused = true }, ribbon.FileMenu);
            Assert.Equal(RibbonFileButtonAppearanceSettings.Darken(c.Theme.Accent, .20).Start.ToArgb(), image.GetPixel(20, 20).ToArgb());
            Assert.Equal(Color.White, painter.Ink); Assert.True(painter.FocusCalls > 0);
            g.Clear(Color.Magenta); painter.DrawFileButton(c with { FileMenuClosing = true }, ribbon.FileMenu);
            Assert.Equal(c.Theme.Accent.ToArgb(), image.GetPixel(20, 20).ToArgb());
            ribbon.FileMenu.CloseDropDown(); Assert.False(ribbon.FileMenu.IsDropDownOpen);
            g.Clear(Color.Magenta); painter.DrawFileButton(c, ribbon.FileMenu); Assert.Equal(c.Theme.Accent.ToArgb(), image.GetPixel(20, 20).ToArgb());
        }
        ribbon.FileMenu.Enabled = false; painter.DrawFileButton(c with { Hot = true, Pressed = true }, ribbon.FileMenu);
        Assert.Equal(c.Theme.FileButtonAppearance!.DisabledForeground, painter.Ink);
        var custom = c.Theme with { FileButtonAppearance = c.Theme.FileButtonAppearance with { Background = new(Color.Navy), Foreground = Color.Yellow, HoverForeground = Color.Lime } };
        ribbon.FileMenu.Enabled = true; painter.DrawFileButton(c with { Theme = custom }, ribbon.FileMenu); Assert.Equal(Color.Yellow, painter.Ink);
        painter.DrawFileButton(c with { Theme = custom, Hot = true }, ribbon.FileMenu); Assert.Equal(Color.Lime, painter.Ink);
        Assert.Equal("Retained", field.Text);
    });

    [Fact]
    public void Builtin_identity_roundtrips_designer_protocol_generated_enum_and_preferences() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host); Assert.Contains("Office2013Light", draft.ThemeChoices);
        draft.BuiltInTheme = "Office2013Light"; Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Same(RibbonThemes.Office2013Light, ribbon.Theme);
        var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession();
        var manager = (IDesignerSerializationManager)serialization; manager.SetName(ribbon, "ribbon");
        var serializer = (CodeDomSerializer)manager.GetSerializer(typeof(RibbonControl), typeof(CodeDomSerializer))!;
        var assignment = Assert.Single(serializer.SerializeMember(manager, ribbon, TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.BuiltInTheme)]!).OfType<CodeAssignStatement>());
        Assert.Equal("Office2013Light", Assert.IsType<CodeFieldReferenceExpression>(assignment.Right).FieldName); Assert.Empty(serialization.Errors.Cast<object>());
        var preferences = new ShowcasePreferences { BuiltInTheme = RibbonBuiltInTheme.Office2013Light };
        Assert.Equal(RibbonBuiltInTheme.Office2013Light, ShowcasePreferences.FromJson(preferences.ToJson()).BuiltInTheme);
    });

    private sealed class InkProbe : RibbonOffice2024Renderer
    {
        internal Color Ink; internal int FocusCalls;
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags) => Ink = color;
        public override void DrawFocus(RibbonPaintContext context) { FocusCalls++; }
    }
}
