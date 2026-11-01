using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstagePageRepaintTests
{
    [Theory]
    [InlineData(RibbonAnimationLevel.None, RibbonTabContentAnimation.Fade, RibbonWindowBackdrop.None)]
    [InlineData(RibbonAnimationLevel.Expressive, RibbonTabContentAnimation.Fade, RibbonWindowBackdrop.None)]
    [InlineData(RibbonAnimationLevel.Expressive, RibbonTabContentAnimation.SlideDown, RibbonWindowBackdrop.None)]
    [InlineData(RibbonAnimationLevel.None, RibbonTabContentAnimation.Fade, RibbonWindowBackdrop.Mica)]
    [InlineData(RibbonAnimationLevel.Expressive, RibbonTabContentAnimation.Fade, RibbonWindowBackdrop.Mica)]
    [InlineData(RibbonAnimationLevel.Expressive, RibbonTabContentAnimation.SlideDown, RibbonWindowBackdrop.Mica)]
    [InlineData(RibbonAnimationLevel.None, RibbonTabContentAnimation.Fade, RibbonWindowBackdrop.Acrylic)]
    [InlineData(RibbonAnimationLevel.Expressive, RibbonTabContentAnimation.Fade, RibbonWindowBackdrop.Acrylic)]
    [InlineData(RibbonAnimationLevel.Expressive, RibbonTabContentAnimation.SlideDown, RibbonWindowBackdrop.Acrylic)]
    public void Switching_realized_pages_does_not_force_visible_editor_erases_or_recreate_native_fields(RibbonAnimationLevel level, RibbonTabContentAnimation style, RibbonWindowBackdrop material) => FoundationTests.Sta(() =>
    {
        Application.EnableVisualStyles();
        using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 950, 650), ShowInTaskbar = false, Backdrop = material };
        using var text = new EraseTextBox { Text = "Keep editable text", Bounds = new(12, 24, 250, 28) };
        using var rich = new EraseRichTextBox { Text = "Keep rich text and selection", Bounds = new(12, 52, 350, 160) };
        using var body = new Panel(); body.Controls.AddRange([text, rich]);
        using var other = new Panel(); other.Controls.Add(new TextBox { Text = "Other page" });
        var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage };
        ribbon.FileMenu.Visible = true; ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        var first = new RibbonBackstagePageItem { Text = "Info", Content = body };
        var second = new RibbonBackstagePageItem { Text = "Print", Content = other };
        ribbon.Backstage.Items.AddRange(first, second); host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
        ribbon.Motion.SystemPolicyOverride = true; ribbon.IsBackstageOpen = true;
        ribbon.Backstage.SelectedPage = second; ribbon.Backstage.SelectedPage = first; Application.DoEvents();
        nint textHandle = text.Handle, richHandle = rich.Handle; var parent = body.Parent!;
        text.Select(2, 4); rich.Select(3, 5); string? rtf = rich.Rtf;
        text.Counts.Reset(); rich.Counts.Reset(); ribbon.AnimationLevel = level; ribbon.TabContentAnimation = style;
        for (int repeat = 0; repeat < 3; repeat++)
        {
            ribbon.Backstage.SelectedPage = second; ribbon.SettleMotion(); Application.DoEvents();
            ribbon.Backstage.SelectedPage = first;
            if (ribbon.BackstageSurface!.PageMotion is { IsRunning: true } motion)
            {
                int textPaints = text.Counts.Paints, richPaints = rich.Counts.Paints;
                motion.SetProgress(.25); motion.SetProgress(.75); motion.SetProgress(1);
                Assert.Equal(textPaints, text.Counts.Paints); Assert.Equal(richPaints, rich.Counts.Paints);
            }
            ribbon.SettleMotion(); Application.DoEvents();
        }
        if (Environment.GetEnvironmentVariable("RIBBONKIT_REPAINT_DIAGNOSTICS") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, level + "-" + style + "-" + material + ".log"),
                $"Requested backdrop={material}, effective={host.EffectiveBackdrop}, slotStyle=0x{GetWindowLong(parent.Handle, -20):X}, areaStyle=0x{GetWindowLong(ribbon.BackstageSurface!.PageArea.Handle, -20):X}\nTextBox visible erases={text.Counts.VisibleErases}, forced={text.Counts.ForcedErases}, paints={text.Counts.Paints}\n" +
                $"RichTextBox visible erases={rich.Counts.VisibleErases}, forced={rich.Counts.ForcedErases}, paints={rich.Counts.Paints}\n");
        }
        Assert.Equal(textHandle, text.Handle); Assert.Equal(richHandle, rich.Handle); Assert.Same(parent, body.Parent);
        Assert.Equal("Keep editable text", text.Text); Assert.Equal(rtf, rich.Rtf);
        Assert.Equal(2, text.SelectionStart); Assert.Equal(4, text.SelectionLength); Assert.Equal(3, rich.SelectionStart); Assert.Equal(5, rich.SelectionLength);
        Assert.Equal(0, text.Counts.ForcedErases); Assert.Equal(0, rich.Counts.ForcedErases);
        // Material already composes the library page area; Solid composes its
        // native slot. Neither buffers the entire Form or changes authored controls.
        var compositor = ribbon.HasWindowBackdrop ? ribbon.BackstageSurface!.PageArea : parent;
        Assert.NotEqual(0, GetWindowLong(compositor.Handle, -20) & 0x02000000);
        Assert.Equal(0, GetWindowLong(host.Handle, -20) & 0x02000000);
        Assert.Equal(0, GetWindowLong(body.Handle, -20) & 0x02000000);
    });

    private sealed class PaintCounts
    {
        internal int VisibleErases, ForcedErases, Paints;
        internal void Reset() => VisibleErases = ForcedErases = Paints = 0;
        internal void Observe(Control control, Message message)
        {
            if (message.Msg == 0xF && control.Visible) Paints++;
            if (message.Msg != 0x14 || !control.Visible) return;
            VisibleErases++;
            if (Environment.StackTrace.Contains("RibbonDrawingHold.Repaint", StringComparison.Ordinal)) ForcedErases++;
        }
    }
    private sealed class EraseTextBox : TextBox
    {
        internal readonly PaintCounts Counts = new();
        protected override void WndProc(ref Message message) { Counts.Observe(this, message); base.WndProc(ref message); }
    }
    private sealed class EraseRichTextBox : RichTextBox
    {
        internal readonly PaintCounts Counts = new();
        protected override void WndProc(ref Message message) { Counts.Observe(this, message); base.WndProc(ref message); }
    }
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint window, int index);
}
