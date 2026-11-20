using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class Office2010OptionsTests
{
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Ordinary_buttons_share_the_menu_footer_palette_and_primary_states_stay_accented(int dpi) => FoundationTests.Sta(() =>
    {
        var theme = RibbonThemes.Office2010Blue; var a = theme.OptionsAppearance!; var m = new RibbonOptionsShellMetrics(theme, dpi);
        Assert.Equal(theme.ApplicationMenuAppearance!.FooterBackground, m.ButtonFill(false, true, false, false));
        Assert.Equal(theme.Hover, m.ButtonFill(false, true, true, false)); Assert.Equal(theme.Pressed, m.ButtonFill(false, true, true, true));
        Assert.Equal(theme.ApplicationMenuAppearance.FooterStateChrome, a.StateChrome);
        using var painter = new RoundedPainter(); using var button = new Button { Text = "", Size = new(180, 48) }; using var image = new Bitmap(200, 64); using var g = Graphics.FromImage(image);
        var bounds = new Rectangle(8, 8, 180, 48); var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
        foreach (bool primary in new[] { false, true })
        foreach (int state in new[] { 0, 1, 2 })
        {
            g.Clear(Color.White); RibbonOptionsButtonRenderer.Draw(painter, c, button, primary, state == 1, state == 2, false, false, 3, Color.White);
            var border = image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top);
            var expected = primary ? theme.Accent : state == 0 ? theme.PopupBorder : state == 1 ? a.StateChrome!.HoverBorderColor : a.StateChrome!.PressedBorderColor;
            Assert.Equal(expected.ToArgb(), border.ToArgb());
            Assert.NotEqual(image.GetPixel(bounds.Right - 10, bounds.Top + 7), image.GetPixel(bounds.Right - 10, bounds.Bottom - 7));
        }
        button.Enabled = false; RibbonOptionsButtonRenderer.Draw(painter, c, button, false, true, true, false, false, 3, Color.White);
        Assert.Equal(a.ButtonDisabledBorderColor.ToArgb(), image.GetPixel(bounds.Left + bounds.Width / 2, bounds.Top).ToArgb());
    });

    [Theory] [InlineData(30,90,156)] [InlineData(180,40,64)] [InlineData(33,115,70)] [InlineData(128,57,123)] [InlineData(0,112,112)] [InlineData(168,75,0)]
    public void Primary_gradient_tracks_accent_hue_and_darkenings_and_explicit_fills_win(int r, int g, int b)
    {
        var accent = Color.FromArgb(r, g, b); var theme = RibbonThemes.Office2010Blue with { Accent = accent }; var a = theme.OptionsAppearance!; var m = new RibbonOptionsShellMetrics(theme, 96);
        var idle = m.ButtonFill(true, true, false, false); var hover = m.ButtonFill(true, true, true, false); var down = m.ButtonFill(true, true, true, true);
        foreach (double t in new[] { 0d, .25, .5, .75, 1 })
        {
            var pixel = RibbonFillColors.At(idle, t); double error = Math.Abs(pixel.GetHue() - accent.GetHue()); Assert.InRange(Math.Min(error, 360 - error), 0, 2);
            Assert.True(RibbonFillColors.At(hover, t).GetBrightness() < pixel.GetBrightness()); Assert.True(RibbonFillColors.At(down, t).GetBrightness() < RibbonFillColors.At(hover, t).GetBrightness());
        }
        Assert.Equal(Color.White.ToArgb(), m.ButtonForeground(true, true, true, true).ToArgb());
        var zero = new RibbonOptionsShellMetrics(theme with { OptionsAppearance = a with { AccentButtonGradientStrength = 0 } }, 96).ButtonFill(true, true, false, false); Assert.Equal(new RibbonFill(accent), zero);
        var explicitFill = new RibbonFill(Color.FromArgb(128, Color.Red), Color.FromArgb(64, Color.Blue)) { Direction = RibbonGradientDirection.Horizontal };
        m = new(theme with { OptionsAppearance = a with { AccentButtonBackground = explicitFill, AccentButtonHoverDarkening = 0, AccentButtonPressedBackground = new(Color.Magenta) } }, 96);
        Assert.Equal(explicitFill, m.ButtonFill(true, true, false, false)); Assert.Equal(explicitFill, m.ButtonFill(true, true, true, false)); Assert.Equal(new RibbonFill(Color.Magenta), m.ButtonFill(true, true, true, true));
    }

    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void List_and_tree_hover_gold_and_selected_accent_borders_keep_targets_and_explicit_hide(int dpi) => FoundationTests.Sta(() =>
    {
        var theme = RibbonThemes.Office2010Blue with { Accent = Color.Firebrick }; var a = theme.OptionsAppearance!; using var painter = new RoundedPainter(); using var owner = new RibbonOffice2024Renderer(); using var check = new RibbonCheckBoxItem();
        using var image = new Bitmap(260, 65); using var g = Graphics.FromImage(image); var bounds = new Rectangle(6, 6, 245, 50); var c = new RibbonPaintContext(g, bounds, theme, Control.DefaultFont, dpi);
        foreach (bool tree in new[] { false, true })
        foreach (bool selected in new[] { false, true })
        {
            g.Clear(Color.Magenta); Draw(c, selected, true);
            Assert.Equal((selected ? theme.Accent : a.RowStateChrome!.HoverBorderColor).ToArgb(), image.GetPixel(bounds.Left + 40, bounds.Top).ToArgb());
            Assert.Equal(Color.Magenta.ToArgb(), image.GetPixel(bounds.Left - 1, bounds.Top).ToArgb());
            var noOutline = a with { RowBorderWidth = 0, TreeBorderWidth = 0, RowStateChrome = null, TreeStateChrome = null };
            Draw(c with { Theme = theme with { OptionsAppearance = noOutline } }, selected, true); Assert.NotEqual((selected ? theme.Accent : a.RowStateChrome!.HoverBorderColor).ToArgb(), image.GetPixel(bounds.Left + 40, bounds.Top).ToArgb());
            void Draw(RibbonPaintContext context, bool marked, bool hot)
            {
                if (!tree) RibbonOptionsRowRenderer.Draw(painter, context with { Hot = hot }, null, "", marked);
                else RibbonOptionsTreeRenderer.Draw(owner, painter, context with { StateHot = hot }, default, "", marked, false, false, null, true, false, check, null);
            }
        }
    });

    [Fact]
    public void Hot_tracking_never_selects_and_recomputes_after_native_scroll_and_clear() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue }; using var form = new Form { Bounds = new(-16000,-16000,300,200), ShowInTaskbar = false };
        using var list = new RibbonOptionsListBox { Bounds = new(0,0,200,100), ItemHeight = 24, DrawMode = DrawMode.OwnerDrawFixed, IntegralHeight = false }; list.BindTheme(ribbon); for (int i=0;i<30;i++) list.Items.Add("Row " + i); form.Controls.Add(list); form.Show(); list.SelectedIndex = 0;
        SendMessage(list.Handle,0x200,0,(36<<16)|20);
        Assert.Equal(1,list.HotIndex); Assert.Equal(0,list.SelectedIndex); var handle = list.Handle;
        list.TopIndex = 4; Assert.Equal(5,list.HotIndex); Assert.Equal(0,list.SelectedIndex); Assert.Equal(handle,list.Handle);
        list.Items.Clear(); Assert.Equal(-1,list.HotIndex); form.Close();
    });

    [Fact]
    public void Invalid_new_roles_are_atomic_and_independent_reset_keeps_other_roles() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2010Blue }; var original = ribbon.Theme; var a = original.OptionsAppearance!;
        foreach (var invalid in new[] { a with { RowBorderWidth = -1 }, a with { TreeBorderWidth = 33 }, a with { NavigationBorderWidth = 33 }, a with { AccentButtonGradientStrength = double.NaN }, a with { BackgroundColor = Color.Transparent }, a with { ListBackgroundColor = Color.FromArgb(1,Color.Red) }, a with { ButtonIdleStateChrome = new() { InnerBorderWidth = 33 } } })
        { Assert.ThrowsAny<ArgumentException>(() => ribbon.Theme = original with { OptionsAppearance = invalid }); Assert.Same(original,ribbon.Theme); Assert.Equal(RibbonBuiltInTheme.Office2010Blue,ribbon.BuiltInTheme); }
        ribbon.Theme = original with { OptionsAppearance = a with { RowHoverBackground = null, RowStateChrome = null } }; Assert.NotNull(ribbon.Theme.OptionsAppearance!.ButtonBackground); Assert.Null(ribbon.Theme.OptionsAppearance.RowHoverBackground);
        ribbon.Theme = original with { OptionsAppearance = null }; Assert.Null(ribbon.Theme.OptionsAppearance); Assert.NotNull(ribbon.Theme.ApplicationMenuAppearance);
    });
    [Fact]
    public void Empty_row_and_tree_state_groups_preserve_legacy_alpha_compositing() => FoundationTests.Sta(() =>
    {
        using var painter=new RoundedPainter(); using var renderer=new RibbonOffice2024Renderer(); using var check=new RibbonCheckBoxItem();
        var theme=RibbonThemes.Office2019Light with { Checked=new(Color.FromArgb(96,Color.Blue)) };
        foreach(bool tree in new[] { false,true })
        {
            using var before=Draw(theme); using var after=Draw(theme with { OptionsAppearance=new() { RowStateChrome=new(),TreeStateChrome=new() } });
            for(int y=0;y<before.Height;y++) for(int x=0;x<before.Width;x++) Assert.Equal(before.GetPixel(x,y),after.GetPixel(x,y));
            Bitmap Draw(RibbonTheme value)
            {
                var image=new Bitmap(180,40); using var g=Graphics.FromImage(image); g.Clear(Color.Magenta); var c=new RibbonPaintContext(g,new(Point.Empty,image.Size),value,Control.DefaultFont,96);
                if(tree) RibbonOptionsTreeRenderer.Draw(renderer,painter,c,default,"",true,false,false,null,true,false,check,null); else RibbonOptionsRowRenderer.Draw(painter,c,null,"",true);
                return image;
            }
        }
    });
    [Theory] [InlineData(DockStyle.Top)] [InlineData(DockStyle.Fill)]
    public void Rename_field_frame_is_opt_in_and_preserves_the_native_editor(DockStyle dock) => FoundationTests.Sta(() =>
    {
        using var ribbon=new RibbonControl { BuiltInTheme=RibbonBuiltInTheme.Office2019Light }; using var native=new TextBox { Dock=dock,Text="Draft name" };
        Assert.Same(native,RibbonOptionsChrome.FrameTextField(native,ribbon,32));
        ribbon.BuiltInTheme=RibbonBuiltInTheme.Office2010Blue; using var frame=RibbonOptionsChrome.FrameTextField(native,ribbon,32); frame.Size=new(260,40); frame.CreateControl(); frame.PerformLayout();
        Assert.IsType<RibbonOptionsFrame>(frame); Assert.Same(frame,native.Parent); Assert.Equal("Draft name",native.Text); Assert.Equal(BorderStyle.None,native.BorderStyle); Assert.Equal(dock,frame.Dock);
        Assert.True(frame.ClientRectangle.Contains(native.Bounds));
    });
    [Theory] [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Default_outlines_are_one_device_pixel_and_explicit_wider_outlines_still_win(int dpi) => FoundationTests.Sta(() =>
    {
        var theme=RibbonThemes.Office2010Blue with { Accent=Color.Firebrick }; using var painter=new RoundedPainter(); using var owner=new RibbonOffice2024Renderer(); using var check=new RibbonCheckBoxItem(); using var button=new Button { Text="" };
        using var image=new Bitmap(320,100); using var g=Graphics.FromImage(image); var bounds=new Rectangle(10,10,300,80); var c=new RibbonPaintContext(g,bounds,theme,Control.DefaultFont,dpi);
        void Hairline(Rectangle paint,Color ink)
        { int x=paint.Left+paint.Width*3/4; Assert.Equal(ink.ToArgb(),image.GetPixel(x,paint.Top).ToArgb()); Assert.NotEqual(ink.ToArgb(),image.GetPixel(x,paint.Top+1).ToArgb()); }
        foreach(bool hot in new[] { false,true })
        {
            g.Clear(Color.White); RibbonOptionsRowRenderer.Draw(painter,c with { Hot=hot },null,"",!hot); Hairline(bounds,hot ? theme.OptionsAppearance!.RowStateChrome!.HoverBorderColor : theme.Accent);
            g.Clear(Color.White); RibbonOptionsTreeRenderer.Draw(owner,painter,c with { StateHot=hot },default,"",!hot,false,false,null,true,false,check,null); Hairline(bounds,hot ? theme.OptionsAppearance!.TreeStateChrome!.HoverBorderColor : theme.Accent);
        }
        g.Clear(Color.White); RibbonOptionsRenderer.DrawNavigation(owner,painter,c,"",true,false); Hairline(new RibbonOptionsAppearanceMetrics(theme,dpi).SelectionBounds(bounds),theme.Accent);
        foreach(bool primary in new[] { false,true })
        foreach(int state in new[] { 0,1,2 })
        { g.Clear(Color.White); RibbonOptionsButtonRenderer.Draw(painter,c,button,primary,state==1,state==2,false,false,3,Color.White); Hairline(bounds,primary ? theme.Accent : state==0 ? theme.PopupBorder : state==1 ? theme.OptionsAppearance!.StateChrome!.HoverBorderColor : theme.OptionsAppearance!.StateChrome!.PressedBorderColor); }
        g.Clear(Color.White); RibbonOptionsFieldRenderer.DrawFrame(painter,c,0); Hairline(bounds,theme.PopupBorder);
        g.Clear(Color.White); RibbonOptionsIconPickerRenderer.Draw(painter,c,null,true,0); Hairline(new RibbonOptionsIconPickerMetrics(theme,dpi,RibbonDensity.Compact).SelectionBounds(bounds),theme.Border);
        var wide=theme with { OptionsAppearance=theme.OptionsAppearance! with { RowBorderWidth=3,RowBorderColor=Color.Magenta,RowStateChrome=null } };
        RibbonOptionsRowRenderer.Draw(painter,c with { Theme=wide },null,"",true); int width=(int)Math.Ceiling(3*dpi/96d); for(int y=0;y<width;y++) Assert.Equal(Color.Magenta.ToArgb(),image.GetPixel(bounds.Left+200,bounds.Top+y).ToArgb());
    });
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window,int message,nint wParam,nint lParam);
}
