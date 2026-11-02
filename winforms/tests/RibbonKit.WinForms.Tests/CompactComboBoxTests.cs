using System.ComponentModel;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class CompactComboBoxTests
{
    [Fact]
    public void A_hidden_combo_label_does_not_reserve_the_neighboring_fields_label_width() => FoundationTests.Sta(() =>
    {
        using var group = new RibbonGroup(); using var ribbon = new RibbonControl();
        var combo = new RibbonComboBoxItem { Text = "Font family", ShowLabel = false, InputWidth = 220 };
        var page = new RibbonTextBoxItem { Text = "Page", InputWidth = 48 }; group.Items.AddRange(combo, page);
        var measured = ribbon.Engine.Measure(group, 0, ribbon.Font, 96, ribbon.ClassicLayoutTheme);
        Assert.Equal(226, measured.Items[0].Bounds.Width); Assert.Equal(0, measured.Items[0].LabelWidth); Assert.True(measured.Items[1].LabelWidth > 0);
    });
    [Theory]
    [InlineData(96)] [InlineData(120)] [InlineData(144)] [InlineData(192)]
    public void Label_visibility_and_small_size_change_geometry_without_losing_the_command_name(int dpi) => FoundationTests.Sta(() =>
    {
        using var ribbon = new RibbonControl(); var tab = new RibbonTab(); var group = new RibbonGroup(); tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var combo = new RibbonComboBoxItem { Text = "Font family", InputWidth = 220, SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Medium, RibbonItemSize.Small, RibbonItemSize.Small) }; group.Items.Add(combo);
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (var layout in Enum.GetValues<RibbonGroupLayout>())
        {
            ribbon.Density = density; group.Layout = layout; combo.ShowLabel = true; ribbon.Engine.Clear();
            var labelled = ribbon.Engine.Measure(group, 0, ribbon.Font, dpi, ribbon.ClassicLayoutTheme).Items.Single();
            Assert.Equal(RibbonItemSize.Medium, labelled.Size); Assert.True(labelled.LabelWidth > 0);
            combo.ShowLabel = false; ribbon.Engine.Clear();
            var hidden = ribbon.Engine.Measure(group, 0, ribbon.Font, dpi, ribbon.ClassicLayoutTheme).Items.Single();
            var compact = ribbon.Engine.Measure(group, 1, ribbon.Font, dpi, ribbon.ClassicLayoutTheme).Items.Single();
            Assert.Equal(0, hidden.LabelWidth); Assert.True(hidden.Bounds.Width < labelled.Bounds.Width);
            Assert.Equal(RibbonItemSize.Small, compact.Size); Assert.Equal(0, compact.LabelWidth); Assert.True(compact.Bounds.Width < hidden.Bounds.Width);
            Assert.Equal(RibbonLayoutEngine.Scale(RibbonDensityMetrics.For(density).SmallWidth + 12, dpi), compact.Bounds.Width);
            Assert.Equal("Font family", combo.Text);
        }
    });

    [Fact]
    public void Actual_group_reduction_uses_a_chooser_then_restores_the_same_native_editor_and_validation() => FoundationTests.Sta(() =>
    {
        using var form = new Form { ClientSize = new(900, 350) }; var ribbon = new RibbonControl { BindingContext = new BindingContext() }; form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup { ReductionMode = RibbonGroupReductionMode.Resize }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var combo = new RibbonComboBoxItem { Text = "Font", ShowLabel = false, InputWidth = 300, IsEditable = true,
            SizePolicy = RibbonSizePolicy.Adaptive(RibbonItemSize.Medium, RibbonItemSize.Small, RibbonItemSize.Small) };
        combo.Items.AddRange("Alpha", "Beta", "Blocked"); combo.SelectedIndex = 0; group.Items.Add(combo);
        using var binding = new BindingSource { DataSource = new State() }; var state = (State)binding.DataSource;
        combo.DataBindings.Add(new Binding(nameof(combo.Value), binding, nameof(state.Value), false, DataSourceUpdateMode.OnValidation));
        int committed = 0; combo.ValueCommitted += (_, _) => committed++; combo.ValueValidating += (_, args) => args.Cancel = combo.Value == "Blocked";
        form.Show(); Application.DoEvents(); var editor = ribbon.Editors.EditorFor(combo)!; var handle = editor.Handle;
        Assert.Equal(RibbonItemSize.Medium, Target().Size);
        form.ClientSize = new(180, 350); Application.DoEvents(); Assert.Equal(RibbonItemSize.Small, Target().Size); Assert.False(editor.Visible);
        ribbon.Activate(Target()); Assert.True(combo.IsDropDownOpen); Assert.True(form.ContainsFocus);
        ribbon.Editors.ProcessListKey(Keys.Down); Assert.Equal("Beta", combo.Value); Assert.Equal("Alpha", state.Value);
        ribbon.Editors.ProcessListKey(Keys.Escape); Assert.Equal("Alpha", combo.Value); Assert.False(combo.IsDropDownOpen); Assert.Same(combo, ribbon.FocusedElement);
        combo.ShowDropDown(); ribbon.Editors.ProcessListKey(Keys.Down); ribbon.Editors.ProcessListKey(Keys.Enter);
        Assert.Equal("Beta", state.Value); Assert.Equal(1, committed); Assert.False(combo.IsDropDownOpen);
        combo.ShowDropDown(); ribbon.Editors.ProcessListKey(Keys.End); ribbon.Editors.ProcessListKey(Keys.Enter);
        Assert.True(combo.IsDropDownOpen); Assert.Equal("Beta", state.Value); Assert.Equal(1, committed);
        ribbon.Editors.ProcessListKey(Keys.Escape); Assert.Equal("Beta", combo.Value);
        combo.ShowDropDown(); ribbon.Editors.ProcessListKey(Keys.Home);
        form.ClientSize = new(900, 350); Application.DoEvents();
        Assert.False(combo.IsDropDownOpen); Assert.Equal("Beta", combo.Value); Assert.Equal(handle, editor.Handle); Assert.True(editor.Visible); Assert.Equal("Beta", editor.Text);
        Assert.Equal("Font", combo.Text); Assert.False(combo.ShowLabel);
        // A compact chooser preserves unmatched editable text until a choice is accepted.
        combo.Value = "Custom font"; form.ClientSize = new(180, 350); Application.DoEvents(); combo.ShowDropDown(); combo.CloseDropDown();
        form.ClientSize = new(900, 350); Application.DoEvents(); Assert.Equal("Custom font", editor.Text); Assert.Equal(handle, editor.Handle);
        RibbonTarget Target() => ribbon.Snapshot.Targets.Single(t => t.Element == combo);
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Compact_chooser_in_group_flyout_or_simplified_closes_on_mutation_and_never_duplicates_the_native_field(bool simplified) => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 950 }; var ribbon = new RibbonControl(); form.Controls.Add(ribbon);
        var tab = new RibbonTab(); var group = new RibbonGroup { CanResize = false }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var combo = new RibbonComboBoxItem { Text = "Zoom", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Small),
            SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedSize = RibbonItemSize.Small };
        combo.Items.AddRange("100%", "150%", "200%"); combo.SelectedIndex = 0; group.Items.Add(combo);
        if (simplified) ribbon.LayoutMode = RibbonLayoutMode.Simplified;
        form.Show(); Application.DoEvents();
        using var popup = simplified ? null : new RibbonPopupSurface(ribbon, group, new Size(1000, 700));
        if (popup != null) { form.Controls.Add(popup); popup.FocusElement(combo); }
        Control opener = popup ?? (Control)ribbon; var targets = popup?.Targets ?? ribbon.Snapshot.Targets;
        var target = targets.Single(t => t.Element == combo); Assert.Equal(RibbonItemSize.Small, target.Size);
        ribbon.Editors.OpenCompact(combo, opener, target.VisibleBounds); Assert.True(combo.IsDropDownOpen);
        Assert.True(ribbon.Editors.ProcessListCharacter('2')); Assert.Equal("200%", combo.Value);
        ribbon.Editors.ProcessListKey(Keys.Escape); Assert.Equal("100%", combo.Value);
        ribbon.Editors.OpenCompact(combo, opener, target.VisibleBounds); combo.Items.Insert(0, "75%"); Assert.False(combo.IsDropDownOpen); Assert.Equal("100%", combo.Value);
        ribbon.Editors.OpenCompact(combo, opener, target.VisibleBounds); combo.Enabled = false; Assert.False(combo.IsDropDownOpen);
        Assert.False(ribbon.Editors.EditorFor(combo)!.Visible);
        using var pixels = new Bitmap(opener.Width, opener.Height); opener.DrawToBitmap(pixels, opener.ClientRectangle);
    });

    [Fact]
    public void Custom_group_copies_keep_hidden_labels_named_and_share_commits_with_the_source() => FoundationTests.Sta(() =>
    {
        using var form = new Form { Width = 950 }; var ribbon = new RibbonControl { BindingContext = new BindingContext() }; form.Controls.Add(ribbon);
        var tab = new RibbonTab { Id = "home" }; var group = new RibbonGroup { Id = "font" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
        var combo = new RibbonComboBoxItem { Id = "family", Text = "Font family", ShowLabel = false }; combo.Items.AddRange("Arial", "Calibri"); combo.SelectedIndex = 0; group.Items.Add(combo);
        ribbon.CommandCatalog.CaptureDefaults();
        using var session = ribbon.CommandCatalog.BeginCustomization("combo.rows", RibbonCustomizationTarget.Ribbon);
        session.Draft.Ribbon!.Tabs.Add(new RibbonCustomizationTab { Label = "Mine", Groups = [new RibbonCustomizationGroup { Label = "Formatting", Commands = [new RibbonCustomizationCommand { CommandId = combo.Id, Size = RibbonItemSize.Small }] }] });
        Assert.True(session.Apply()); var proxy = Assert.IsType<RibbonComboBoxItem>(ribbon.Tabs.Last().Groups[0].Items.Single());
        Assert.Equal("Font family", proxy.Text); Assert.False(proxy.ShowLabel); Assert.Same(combo, ribbon.CommandCatalog.GetSource(proxy));
        form.Show(); ribbon.SelectedTab = ribbon.Tabs.Last(); Application.DoEvents();
        proxy.ShowDropDown(); ribbon.Editors.ProcessListKey(Keys.Down); ribbon.Editors.ProcessListKey(Keys.Enter); Assert.Equal("Calibri", combo.Value); Assert.Equal("Calibri", proxy.Value);
        combo.ShowLabel = true; Assert.True(proxy.ShowLabel); Assert.Equal("Font family", proxy.Text);
    });
    private sealed class State { public string Value { get; set; } = "Alpha"; }
}
