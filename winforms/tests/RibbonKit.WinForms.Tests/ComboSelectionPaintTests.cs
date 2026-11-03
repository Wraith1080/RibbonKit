using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ComboSelectionPaintTests
{
    [Theory]
    [InlineData(RibbonDensity.Compact, RibbonWindowBackdrop.None)] [InlineData(RibbonDensity.Touch, RibbonWindowBackdrop.None)]
    [InlineData(RibbonDensity.Compact, RibbonWindowBackdrop.Mica)] [InlineData(RibbonDensity.Touch, RibbonWindowBackdrop.Mica)]
    [InlineData(RibbonDensity.Compact, RibbonWindowBackdrop.Acrylic)] [InlineData(RibbonDensity.Touch, RibbonWindowBackdrop.Acrylic)]
    public void Choice_selection_draws_current_native_value_without_a_second_paint(RibbonDensity density, RibbonWindowBackdrop backdrop) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm { Size = new(1000, 650), Backdrop = backdrop };
        using var renderer = new SelectionMarkerRenderer();
        var ribbon = new RibbonControl { Renderer = renderer, Density = density, AnimationLevel = RibbonAnimationLevel.None };
        var item = new RibbonComboBoxItem { Id = "choice", Text = "Font" };
        item.Items.AddRange("Alpha", "Beta"); item.SelectedIndex = 0;
        var group = new RibbonGroup(); group.Items.Add(item);
        var tab = new RibbonTab(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon); form.Ribbon = ribbon;
        form.Show(); Application.DoEvents();
        var editor = ribbon.Editors.EditorFor(item)!;
        AssertMarker("Alpha");
        item.ShowDropDown(); Application.DoEvents();
        int selectionInvalidations = 0; editor.Invalidated += (_, _) => selectionInvalidations++;
        Assert.True(ribbon.Editors.ProcessListKey(Keys.End));
        AssertMarker("Beta"); // The first native draw must already use the new choice.
        Assert.Equal(0, selectionInvalidations);
        Application.DoEvents();
        Assert.Equal("Beta", item.Value); Assert.Equal("Beta", editor.Text);
        AssertMarker("Beta");
        Assert.True(ribbon.Editors.ProcessListKey(Keys.Escape)); Application.DoEvents();
        Assert.Equal("Alpha", item.Value); AssertMarker("Alpha");
        item.ShowDropDown();
        Assert.True(ribbon.Editors.ProcessListKey(Keys.End));
        Assert.True(ribbon.Editors.ProcessListKey(Keys.Enter)); Application.DoEvents();
        Assert.Equal("Beta", item.Value); Assert.False(item.IsDropDownOpen); AssertMarker("Beta");
        item.SelectedIndex = 0; Application.DoEvents(); AssertMarker("Alpha");
        int invalidations = 0; editor.Invalidated += (_, _) => invalidations++;
        for (int i = 0; i < 5; i++) ribbon.SyncEditors();
        Assert.Equal(0, invalidations);
        form.Close();

        void AssertMarker(string value)
        {
            // Read existing native pixels; DrawToBitmap/Refresh would hide a missing repaint.
            var text = RibbonEditorLayout.ComboText(editor.ClientRectangle, ribbon.DeviceDpi, ribbon.Density);
            nint dc = GetDC(editor.Handle);
            try
            {
                uint pixel = GetPixel(dc, text.Left + 2, text.Top + text.Height / 2);
                Assert.Equal(SelectionMarkerRenderer.ColorFor(value), pixel);
            }
            finally { ReleaseDC(editor.Handle, dc); }
        }
    });

    private sealed class SelectionMarkerRenderer : RibbonRenderer
    {
        internal static uint ColorFor(string text) => text == "Alpha" ? 0x0000ffu : 0xff0000u;
        public override void DrawText(RibbonPaintContext context, string text, Color color, TextFormatFlags flags)
        {
            base.DrawText(context, text, color, flags);
            if (text is not ("Alpha" or "Beta")) return;
            using var brush = new SolidBrush(text == "Alpha" ? Color.Red : Color.Blue);
            context.Graphics.FillRectangle(brush, context.Bounds.Left + 1, context.Bounds.Top + context.Bounds.Height / 2 - 2, 4, 4);
        }
    }

    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern uint GetPixel(nint dc, int x, int y);
}
