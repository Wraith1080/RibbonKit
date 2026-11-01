using System.ComponentModel;
using System.ComponentModel.Design;
using System.Windows.Forms.Design;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class RibbonFormDesignerTests
{
    [Theory]
    [InlineData(FormBorderStyle.Sizable)] [InlineData(FormBorderStyle.FixedSingle)] [InlineData(FormBorderStyle.Fixed3D)]
    public void Designer_suppresses_native_frame_without_changing_authored_border_or_runtime_styles(FormBorderStyle border) => FoundationTests.Sta(() =>
    {
        using var form = new FrameProbe { FormBorderStyle = border }; int runtimeStyle = form.NativeStyle, runtimeExtended = form.NativeExtendedStyle;
        form.Site = new DesignSite(form);
        Assert.Equal(0, form.NativeStyle & 0x00C40000); // caption, border/dialog frame and sizing frame
        Assert.Equal(0, form.NativeExtendedStyle & 0x00000301); // window/client/dialog edges
        Assert.Equal(border, form.FormBorderStyle); Assert.True(form.CustomCaption);
        form.Site = null; Assert.Equal(runtimeStyle, form.NativeStyle); Assert.Equal(runtimeExtended, form.NativeExtendedStyle);
    });

    [Fact]
    public void Framework_form_designer_keeps_resize_rules_and_client_size_without_native_bevel() => FoundationTests.Sta(() =>
    {
        using var surface = new DesignSurface(typeof(RibbonForm));
        var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var form = (RibbonForm)host.RootComponent; var designer = Assert.IsAssignableFrom<ControlDesigner>(host.GetDesigner(form));
        _ = ((Control)surface.View!).Handle; _ = form.Handle;
        Assert.Equal(form.Size, form.ClientSize);
        Assert.True(designer.SelectionRules.HasFlag(SelectionRules.RightSizeable));
        Assert.True(designer.SelectionRules.HasFlag(SelectionRules.BottomSizeable));
        var size = TypeDescriptor.GetProperties(form)[nameof(form.ClientSize)]!; size.SetValue(form, new Size(721, 513));
        Assert.Equal(new Size(721, 513), form.ClientSize); Assert.Equal(form.ClientSize, form.Size);
        Assert.Equal(FormBorderStyle.Sizable, form.FormBorderStyle);
    });

    private sealed class FrameProbe : RibbonForm
    {
        internal int NativeStyle => CreateParams.Style;
        internal int NativeExtendedStyle => CreateParams.ExStyle;
    }
    private sealed class DesignSite(IComponent component) : ISite
    {
        public IComponent Component => component; public IContainer? Container => null;
        public bool DesignMode => true; public string? Name { get; set; } = "form";
        public object? GetService(Type serviceType) => null;
    }
}
