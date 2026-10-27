using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class BackstageTests
{
    [Fact]
    public void Backstage_metadata_identity_collections_binding_and_borrowed_content_are_explicit() => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl { BindingContext = new BindingContext() };
        Assert.Same(ribbon.Backstage, ribbon.Backstage); Assert.Equal(RibbonFileMenuMode.CommandMenu, ribbon.FileMenuMode);
        Assert.False(ribbon.IsBackstageOpen);
        foreach (string name in new[] { nameof(ribbon.Backstage), nameof(RibbonBackstage.Items) })
        {
            object model = name == nameof(ribbon.Backstage) ? ribbon : ribbon.Backstage;
            Assert.Equal(DesignerSerializationVisibility.Content, TypeDescriptor.GetProperties(model)[name]!.SerializationVisibility);
        }
        using var panel = new Panel(); using var field = new TextBox(); panel.Controls.Add(field);
        using var other = new RibbonBackstagePageItem();
        Assert.Throws<ArgumentException>(() => other.Content = field);
        var page = new RibbonBackstagePageItem { Id = "info", Text = "Info", Content = panel };
        ribbon.Backstage.Items.Add(page);
        Assert.Same(ribbon.BindingContext, page.BindingContext);
        Assert.Throws<ArgumentException>(() => other.Content = panel);
        Assert.Throws<ArgumentException>(() => ribbon.Backstage.Items.Add(new RibbonDropDownItem()));
        Assert.Throws<ArgumentException>(() => ribbon.FileMenu.Items.Add(other));
        Assert.Throws<ArgumentException>(() => ribbon.Tabs.Add(new RibbonTab { Id = "info" }));
        using var source = new BindingSource { DataSource = new State() };
        ribbon.CreateControl();
        ribbon.DataBindings.Add(new Binding(nameof(ribbon.FileMenuMode), source, nameof(State.Mode), false, DataSourceUpdateMode.OnPropertyChanged));
        ribbon.FileMenuMode = RibbonFileMenuMode.Backstage; Assert.Equal(RibbonFileMenuMode.Backstage, ((State)source.Current!).Mode);
        ribbon.Dispose(); Assert.True(page.IsDisposed); Assert.False(panel.IsDisposed); Assert.Null(panel.Parent);
        other.Content = panel; other.Content = null;
    });

    [Fact]
    public void File_routing_cancellation_keyboard_commands_and_page_reuse_preserve_native_state() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 900, Height = 540 };
        var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage }; var document = new TextBox { Dock = DockStyle.Fill };
        form.Controls.AddRange([document, ribbon]); ribbon.FileMenu.Visible = true;
        var home = new RibbonTab { Text = "Home", KeyTip = "H" }; ribbon.Tabs.Add(home);
        using var info = new TextBox { Text = "Untitled" }; using var print = new NumericUpDown();
        var a = new RibbonBackstagePageItem { Text = "Info", KeyTip = "I", Content = info };
        var b = new RibbonBackstagePageItem { Text = "Print", KeyTip = "P", Content = print };
        var save = new RibbonButtonItem { Text = "Save", KeyTip = "S" }; int commands = 0, closed = 0, opened = 0;
        save.Click += (_, _) => { Assert.False(ribbon.IsBackstageOpen); commands++; };
        ribbon.Backstage.Items.AddRange(a, save, b);
        bool cancelFile = true, cancelStage = false;
        ribbon.FileMenu.DropDownOpening += (_, e) => e.Cancel = cancelFile;
        ribbon.Backstage.Opening += (_, e) => e.Cancel = cancelStage;
        ribbon.FileMenu.DropDownClosed += (_, _) => closed++;
        ribbon.Backstage.Opened += (_, _) => opened++;
        form.Show(); Application.DoEvents(); document.Focus();
        ribbon.FileMenu.ShowDropDown(); Assert.False(ribbon.IsBackstageOpen);
        cancelFile = false; cancelStage = true; ribbon.IsBackstageOpen = true; Assert.False(ribbon.IsBackstageOpen);
        cancelStage = false; ribbon.ToggleKeyTips(); ribbon.ProcessRibbonKey(Keys.F); Assert.True(ribbon.IsBackstageOpen);
        Assert.True(ribbon.FileMenu.IsDropDownOpen); Assert.False(ribbon.Popups.HasOpen); Assert.Same(home, ribbon.SelectedTab);
        Assert.Same(a, ribbon.Backstage.SelectedPage); var parent = info.Parent; nint handle = info.Handle;
        info.Text = "Retained"; info.Select(2, 3);
        ribbon.ProcessBackstageKey(Keys.P); Assert.Same(b, ribbon.Backstage.SelectedPage);
        ribbon.Backstage.SelectedPage = a; Assert.Same(parent, info.Parent); Assert.Equal(handle, info.Handle); Assert.Equal("Retained", info.Text); Assert.Equal(2, info.SelectionStart); Assert.Equal(3, info.SelectionLength);
        ribbon.IsBackstageOpen = false; Assert.True(document.Focused); Assert.Same(parent, info.Parent);
        ribbon.IsMinimized = true; ribbon.FileMenu.ShowDropDown(); Assert.True(ribbon.IsBackstageOpen); Assert.True(ribbon.IsMinimized);
        ribbon.BackstageSurface!.ToggleKeyTips(); ribbon.ProcessBackstageKey(Keys.S); Assert.Equal(1, commands); Assert.Equal(2, closed); Assert.Equal(2, opened);
        ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu; ribbon.FileMenu.Items.Add(new RibbonButtonItem { Text = "Old menu" });
        ribbon.FileMenu.ShowDropDown(); Assert.True(ribbon.Popups.HasOpen); Assert.False(ribbon.IsBackstageOpen);
        ribbon.FileMenu.CloseDropDown();
        ribbon.Dispose(); Assert.Null(info.Parent); Assert.False(info.IsDisposed); Assert.False(print.IsDisposed);
    });

    [Fact]
    public void Validation_blocks_navigation_commands_and_close_but_forced_teardown_releases_borrowed_pages() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 850, Height = 500 }; var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage };
        form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        using var content = new Panel(); var field = new TextBox { Text = "valid" }; content.Controls.Add(field);
        using var otherContent = new TextBox();
        var a = new RibbonBackstagePageItem { Text = "Info", Content = content }; var b = new RibbonBackstagePageItem { Text = "Print", Content = otherContent };
        var command = new RibbonButtonItem { Text = "Save" }; int calls = 0; command.Click += (_, _) => calls++;
        ribbon.Backstage.Items.AddRange(a, b, command);
        field.Validating += (_, e) => e.Cancel = field.Text.Length == 0;
        form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true; field.Focus(); field.Text = "";
        ribbon.Backstage.SelectedPage = b; Assert.Same(a, ribbon.Backstage.SelectedPage);
        ribbon.ActivateBackstage(command); Assert.True(ribbon.IsBackstageOpen); Assert.Equal(0, calls);
        ribbon.ProcessBackstageKey(Keys.Escape); Assert.True(ribbon.IsBackstageOpen);
        ribbon.FileMenuMode = RibbonFileMenuMode.CommandMenu; Assert.Equal(RibbonFileMenuMode.Backstage, ribbon.FileMenuMode);
        field.Text = "valid"; bool cancel = true; ribbon.Backstage.Closing += (_, e) => { ribbon.IsBackstageOpen = false; e.Cancel = cancel; };
        ribbon.IsBackstageOpen = false; Assert.True(ribbon.IsBackstageOpen);
        cancel = false; ribbon.IsBackstageOpen = false; Assert.False(ribbon.IsBackstageOpen);
        ribbon.IsBackstageOpen = true; field.Focus(); field.Text = "";
        ribbon.FileMenu.Enabled = false; Assert.False(ribbon.IsBackstageOpen);
        ribbon.FileMenu.Enabled = true; ribbon.IsBackstageOpen = true; Assert.True(ribbon.IsBackstageOpen);
        b.Visible = false; a.Enabled = false; Assert.Null(ribbon.Backstage.SelectedPage);
        a.Enabled = true; Assert.Same(a, ribbon.Backstage.SelectedPage);
        ribbon.Backstage.Items.Remove(a); Assert.Null(content.Parent); Assert.False(content.IsDisposed);
        b.Visible = true; Assert.Same(b, ribbon.Backstage.SelectedPage);
        form.Dispose(); Assert.Null(otherContent.Parent); Assert.False(otherContent.IsDisposed);
    });

    [Fact]
    public void Programmatic_page_changes_retain_focus_inside_the_surface_and_never_focus_a_closed_surface() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 850, Height = 500 }; var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage };
        var document = new TextBox { Dock = DockStyle.Fill }; form.Controls.Add(document); form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        using var first = new TextBox(); using var second = new TextBox();
        var a = new RibbonBackstagePageItem { Text = "First", Content = first }; var b = new RibbonBackstagePageItem { Text = "Second", Content = second };
        ribbon.Backstage.Items.AddRange(a, b);
        form.Show(); Application.DoEvents(); document.Focus(); ribbon.IsBackstageOpen = true;
        first.Focus(); Assert.True(first.Focused); ribbon.Backstage.SelectedPage = b;
        Assert.True(ribbon.BackstageSurface!.ContainsFocus); Assert.False(document.Focused);
        second.Focus(); ribbon.Backstage.Items.Remove(b); Assert.Same(a, ribbon.Backstage.SelectedPage);
        Assert.True(ribbon.BackstageSurface.ContainsFocus); Assert.Null(second.Parent);
        ribbon.IsBackstageOpen = false; Assert.True(document.Focused);
        ribbon.Backstage.Items.Add(b); ribbon.Backstage.SelectedPage = b; Assert.True(document.Focused);
    });

    [Fact]
    public void Page_availability_fallback_content_replacement_and_ribbon_visibility_do_not_leave_stale_parents() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 850, Height = 500 }; var ribbon = new RibbonControl { FileMenuMode = RibbonFileMenuMode.Backstage };
        form.Controls.Add(ribbon); ribbon.FileMenu.Visible = true;
        using var first = new Panel(); using var second = new TextBox(); using var third = new Panel(); using var replacement = new Panel();
        var a = new RibbonBackstagePageItem { Text = "First", Content = first };
        var b = new RibbonBackstagePageItem { Text = "Second", Content = second };
        var c = new RibbonBackstagePageItem { Text = "Third", Content = third };
        ribbon.Backstage.Items.AddRange(a, b, c);
        form.Show(); Application.DoEvents(); ribbon.IsBackstageOpen = true;
        ribbon.Backstage.SelectedPage = b; ribbon.Backstage.Items.Remove(b);
        Assert.Same(c, ribbon.Backstage.SelectedPage); Assert.Null(second.Parent); Assert.False(second.IsDisposed);
        c.Enabled = false; Assert.Same(a, ribbon.Backstage.SelectedPage);
        a.Content = replacement; Assert.Null(first.Parent); Assert.NotNull(replacement.Parent); Assert.False(first.IsDisposed);
        ribbon.Visible = false; Assert.False(ribbon.IsBackstageOpen);
        ribbon.Visible = true; ribbon.IsBackstageOpen = true; Assert.True(ribbon.IsBackstageOpen);
        replacement.Dispose(); Assert.Null(a.Content); Assert.True(ribbon.IsBackstageOpen);
        ribbon.Backstage.Dispose(); Assert.False(ribbon.IsBackstageOpen); Assert.Null(third.Parent); Assert.False(third.IsDisposed);
    });

    [Fact]
    public void Backstage_geometry_and_rendering_share_scaled_bounds_and_theme_state() => FoundationTests.Sta(() =>
    {
        using var font = new Font("Segoe UI", 10); using var office = new RibbonOffice2024Renderer(); using var flat = new RibbonRenderer();
        using var page = new RibbonBackstagePageItem { Text = "Info" };
        foreach (int dpi in new[] { 96, 120, 144, 192 })
        foreach (int width in new[] { 160, 400, 1000 })
        {
            var layout = RibbonBackstageLayout.Build(new(width, 500), font, dpi);
            Assert.Equal(layout.Navigation.Right, layout.Page.Left); Assert.Equal(width, layout.Page.Right);
            Assert.True(layout.Back.Right <= layout.Navigation.Right); Assert.True(layout.Page.Width >= 0);
            foreach (var renderer in new[] { flat, office })
            {
                using var bitmap = new Bitmap(400, 180); using var g = Graphics.FromImage(bitmap); var theme = RibbonThemes.Office2024Light;
                renderer.DrawBackstageBackground(new(g, new(0, 0, 400, 180), theme, font, dpi), 200);
                Assert.Equal(theme.BackstageNavigationBackground.ToArgb(), bitmap.GetPixel(10, 170).ToArgb());
                Assert.Equal(theme.Surface.ToArgb(), bitmap.GetPixel(390, 170).ToArgb());
                renderer.DrawBackstageEntry(new(g, new(8, 10, 180, 80), theme, font, dpi), page, true);
                int x = 8 + RibbonLayoutEngine.Scale(3, dpi); Assert.Equal(theme.Accent.ToArgb(), bitmap.GetPixel(x, 45).ToArgb());
            }
        }
    });
    private sealed class State { public RibbonFileMenuMode Mode { get; set; } }
}
