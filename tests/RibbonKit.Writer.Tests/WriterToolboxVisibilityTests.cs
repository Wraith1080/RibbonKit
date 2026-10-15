using System.ComponentModel;
using System.Reflection;
using System.Windows;
using RibbonKit.Writer.Editing;
using Xunit;

namespace RibbonKit.Writer.Tests;

public sealed class WriterToolboxVisibilityTests
{
    [Fact]
    public void Sample_visual_types_do_not_advertise_themselves_as_reusable_toolbox_controls()
    {
        var types = typeof(WriterEditorSurface).Assembly.GetExportedTypes()
            .Where(type => typeof(FrameworkElement).IsAssignableFrom(type)).ToArray();
        Assert.NotEmpty(types);
        foreach (var type in types)
        {
            Assert.Equal(ToolboxItemAttribute.None, type.GetCustomAttribute<ToolboxItemAttribute>());
            Assert.Equal(DesignTimeVisibleAttribute.No, type.GetCustomAttribute<DesignTimeVisibleAttribute>());
        }
    }
}
