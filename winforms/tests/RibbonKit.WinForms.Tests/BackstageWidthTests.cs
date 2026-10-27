using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageWidthTests
{
    [Fact]
    public void Width_metadata_defaults_validation_events_and_two_way_binding_preserve_auto() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BindingContext = new BindingContext() }; var model = ribbon.Backstage;
        Assert.Null(model.NavigationWidth);
        Assert.Equal(160, model.MinimumNavigationWidth); Assert.Equal(320, model.MaximumNavigationWidth);
        var properties = TypeDescriptor.GetProperties(model);
        foreach (string name in new[] { nameof(model.NavigationWidth), nameof(model.MinimumNavigationWidth), nameof(model.MaximumNavigationWidth) })
        {
            Assert.Equal(DesignerSerializationVisibility.Visible, properties[name]!.SerializationVisibility);
            Assert.True(((BindableAttribute)properties[name]!.Attributes[typeof(BindableAttribute)]!).Bindable);
        }
        Assert.Null(properties[nameof(model.NavigationWidth)]!.Converter.ConvertFromInvariantString(""));
        Assert.Null(properties[nameof(model.NavigationWidth)]!.Converter.ConvertFromInvariantString("Auto"));
        Assert.Equal("Auto", properties[nameof(model.NavigationWidth)]!.Converter.ConvertToInvariantString(null));
        Assert.Equal(232, properties[nameof(model.NavigationWidth)]!.Converter.ConvertFromInvariantString("232"));
        Assert.True(properties[nameof(model.NavigationWidth)]!.Converter.GetStandardValuesSupported());
        Assert.True(TypeDescriptor.GetProperties(ribbon)[nameof(ribbon.Backstage)]!.Converter.GetPropertiesSupported());
        Assert.Throws<ArgumentOutOfRangeException>(() => model.NavigationWidth = 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => model.NavigationWidth = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => model.MinimumNavigationWidth = 79);
        Assert.Throws<ArgumentOutOfRangeException>(() => model.MaximumNavigationWidth = 79);
        using var source = new BindingSource { DataSource = new State { Width = 230 } };
        model.DataBindings.Add(new Binding(nameof(model.NavigationWidth), source, nameof(State.Width), true, DataSourceUpdateMode.OnPropertyChanged) { DataSourceNullValue = null });
        Assert.Equal(230, model.NavigationWidth); int changes = 0; model.NavigationWidthChanged += (_, _) => changes++;
        model.NavigationWidth = 260; model.NavigationWidth = 260;
        Assert.Equal(260, ((State)source.Current!).Width); Assert.Equal(1, changes);
        ((State)source.Current!).Width = 280; Assert.Equal(280, model.NavigationWidth);
        properties[nameof(model.NavigationWidth)]!.ResetValue(model); Assert.Null(model.NavigationWidth); Assert.Null(((State)source.Current!).Width);
        Assert.Equal(3, changes);
        model.MaximumNavigationWidth = 120; model.MinimumNavigationWidth = 100; // Designer order is valid.
    });

    [Fact]
    public void Auto_and_explicit_width_share_dpi_scaled_constraints_and_preserve_authored_values() => FoundationTests.Sta(() =>
    {
        using var model = new RibbonBackstage(); var page = new RibbonBackstagePageItem { Text = "Info" }; model.Items.Add(page);
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        {
            int S(int value) => RibbonLayoutEngine.Scale(value, dpi);
            using var font = new Font("Segoe UI", 9 * dpi / 96f);
            var size = new Size(S(1000), S(500));
            model.MinimumNavigationWidth = 160; model.MaximumNavigationWidth = 320; model.NavigationWidth = null;
            Assert.Equal(S(200), RibbonBackstageLayout.Build(size, font, dpi, model).Page.Left);
            page.Text = "Document information and publication settings";
            var measured = RibbonBackstageLayout.Build(size, font, dpi, model);
            Assert.InRange(measured.Page.Left, S(201), S(320));
            page.Visible = false; Assert.Equal(S(200), RibbonBackstageLayout.Build(size, font, dpi, model).Page.Left);
            page.Visible = true; page.Text = "Info";
            model.NavigationWidth = 220;
            var authored = RibbonBackstageLayout.Build(size, font, dpi, model);
            Assert.Equal(S(220), authored.Page.Left);
            Assert.Equal(authored.Navigation.Right, authored.Page.Left);
            Assert.Equal(size.Width, authored.Page.Right); Assert.True(authored.Back.Right <= authored.Navigation.Right);
            model.NavigationWidth = 1; Assert.Equal(S(160), RibbonBackstageLayout.Build(size, font, dpi, model).Page.Left);
            model.NavigationWidth = int.MaxValue; Assert.Equal(S(320), RibbonBackstageLayout.Build(size, font, dpi, model).Page.Left);
            var narrow = RibbonBackstageLayout.Build(new(S(160), S(400)), font, dpi, model);
            Assert.Equal(S(80), narrow.Page.Left); Assert.Equal(S(80), narrow.Page.Width); Assert.Equal(int.MaxValue, model.NavigationWidth);
            Assert.Equal(S(320), RibbonBackstageLayout.Build(size, font, dpi, model).Page.Left);
            model.MinimumNavigationWidth = 260; model.MaximumNavigationWidth = 200;
            Assert.Equal(S(260), RibbonBackstageLayout.Build(size, font, dpi, model).Page.Left);
        }
    });

    [Fact]
    public void Authored_width_changes_retain_page_handles_selection_focus_and_pending_validation() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 1000, Height = 600 }; var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage };
        form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        using var content = new TextBox { Text = "Draft value" };
        var page = new RibbonBackstagePageItem { Text = "Info", Content = content }; ribbon.Backstage.Items.Add(page);
        form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true; content.Focus(); content.Select(2, 4);
        int validations = 0; content.Validating += (_, e) => { validations++; e.Cancel = true; };
        nint handle = content.Handle; var parent = content.Parent;
        ribbon.Backstage.NavigationWidth = 240;
        Assert.Same(page, ribbon.Backstage.SelectedPage); Assert.Same(parent, content.Parent); Assert.Equal(handle, content.Handle);
        Assert.Equal(2, content.SelectionStart); Assert.Equal(4, content.SelectionLength); Assert.Equal("Draft value", content.Text); Assert.Equal(0, validations);
        Assert.True(content.Focused);
        ribbon.Backstage.NavigationWidth = 320;
        ribbon.Backstage.NavigationWidth = null;
        Assert.True(ribbon.IsBackstageOpen); Assert.Equal(0, validations);
        Assert.True(content.Focused);
        Assert.Same(parent, content.Parent); Assert.Equal(handle, content.Handle);
    });
    private sealed class State : INotifyPropertyChanged
    {
        private int? _width;
        public int? Width { get => _width; set { if (_width == value) return; _width = value; PropertyChanged?.Invoke(this, new(nameof(Width))); } }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
