using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Resources;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Design.Client;
using RibbonKit.WinForms.Design.Protocol;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class SvgIconTests
{
    internal const string TwoColors = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><rect width='12' height='24' fill='red'/><rect x='12' width='12' height='24' fill='blue'/></svg>";

    [Fact]
    public void Every_stock_asset_renders_at_native_icon_sizes_without_touching_graphics_state() => FoundationTests.Sta(() =>
    {
        Assert.Equal(512, RibbonStockIcons.All.Count);
        Assert.Equal(512, RibbonStockIcons.All.Select(icon => icon.Key).Distinct(StringComparer.Ordinal).Count());
        foreach (var stock in RibbonStockIcons.All)
        foreach (int side in new[] { 16, 20, 24, 32, 48, 64 })
        {
            var icon = RibbonStockIcons.Create(stock.Key); using var bitmap = new Bitmap(side + 8, side + 8); using var graphics = Graphics.FromImage(bitmap);
            graphics.TranslateTransform(1, 1); var transform = graphics.Transform.Elements; var clip = graphics.ClipBounds; var smoothing = graphics.SmoothingMode;
            icon.Draw(graphics, new Rectangle(2, 2, side, side), new(144, Color.MediumVioletRed, true));
            Assert.Equal(transform, graphics.Transform.Elements); Assert.Equal(clip, graphics.ClipBounds); Assert.Equal(smoothing, graphics.SmoothingMode);
            int drawn = 0;
            for (int y = 0; y < bitmap.Height; y++) for (int x = 0; x < bitmap.Width; x++)
                if (bitmap.GetPixel(x, y).A > 20) { drawn++; Assert.InRange(x, 3, side + 2); Assert.InRange(y, 3, side + 2); }
            Assert.True(drawn > 5, "Empty stock icon: " + stock.Key + " at " + side);
        }
    });

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void Original_colors_are_preserved_and_tint_is_an_explicit_choice(bool tint) => FoundationTests.Sta(() =>
    {
        var icon = new RibbonSvgIcon(TwoColors, tint); using var bitmap = new Bitmap(48, 48); using var graphics = Graphics.FromImage(bitmap);
        icon.Draw(graphics, new Rectangle(0, 0, 48, 48), new(192, Color.Green, false));
        Assert.Equal(tint ? Color.Green.ToArgb() : Color.Red.ToArgb(), bitmap.GetPixel(8, 24).ToArgb());
        Assert.Equal(tint ? Color.Green.ToArgb() : Color.Blue.ToArgb(), bitmap.GetPixel(40, 24).ToArgb());
    });

    [Fact]
    public void Repeated_draws_use_the_current_foreground_including_disabled_state() => FoundationTests.Sta(() =>
    {
        var icon = new RibbonSvgIcon(TwoColors, true); using var bitmap = new Bitmap(24, 24); using var graphics = Graphics.FromImage(bitmap);
        foreach (var color in new[] { Color.Red, Color.DarkCyan, SystemColors.GrayText, Color.Purple })
        {
            graphics.Clear(Color.Transparent); icon.Draw(graphics, new(0, 0, 24, 24), new(120, color, color != SystemColors.GrayText));
            Assert.Equal(color.ToArgb(), bitmap.GetPixel(6, 12).ToArgb()); Assert.Equal(color.ToArgb(), bitmap.GetPixel(18, 12).ToArgb());
        }
    });

    [Fact]
    public void Gradient_transform_and_non_square_viewbox_remain_vector_geometry() => FoundationTests.Sta(() =>
    {
        var icon = new RibbonSvgIcon("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 40 20'><defs><linearGradient id='g'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs><g transform='translate(5 0)'><path d='M0 0 H30 V20 H0 Z' fill='url(#g)'/></g></svg>");
        using var bitmap = new Bitmap(80, 80); using var graphics = Graphics.FromImage(bitmap); icon.Draw(graphics, new(0, 0, 80, 80), new(192, Color.Green, true));
        Assert.Equal(0, bitmap.GetPixel(40, 5).A); Assert.True(bitmap.GetPixel(25, 40).R > bitmap.GetPixel(25, 40).B);
        Assert.True(bitmap.GetPixel(55, 40).B > bitmap.GetPixel(55, 40).R);
    });

    [Theory]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><image href='https://example.invalid/a.png'/></svg>")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><use href='file:///test.svg#shape'/></svg>")]
    [InlineData("<!DOCTYPE svg [<!ENTITY test SYSTEM 'file:///test'>]><svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'>&test;</svg>")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><script>test()</script></svg>")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path d='M0 0' fill='url(https://example.invalid/p)'/></svg>")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 0 0'/>")]
    [InlineData("stock:unknown")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><g id='a'><use href='#a'/></g></svg>")]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><defs><linearGradient id='a' href='#b'/><linearGradient id='b' href='#a'/></defs></svg>")]
    public void Invalid_or_external_artwork_is_rejected(string source) => Assert.ThrowsAny<Exception>(() => new RibbonSvgIcon(source));

    [Fact]
    public void Constructors_strings_and_resx_roundtrip_immutable_svg_sources() => FoundationTests.Sta(() =>
    {
        var converter = TypeDescriptor.GetConverter(typeof(RibbonSvgIcon));
        foreach (var icon in new[] { RibbonStockIcons.Create("save"), new RibbonSvgIcon(TwoColors) })
        {
            var text = converter.ConvertToInvariantString(icon)!; var fromText = Assert.IsType<RibbonSvgIcon>(converter.ConvertFromInvariantString(text));
            Assert.Equal(icon.Source, fromText.Source); Assert.Equal(icon.UseForegroundColor, fromText.UseForegroundColor);
            var descriptor = Assert.IsType<InstanceDescriptor>(converter.ConvertTo(icon, typeof(InstanceDescriptor))); Assert.True(descriptor.IsComplete);
            Assert.Equal(icon.Source, Assert.IsType<RibbonSvgIcon>(descriptor.Invoke()).Source);
            using var output = new StringWriter(); using (var writer = new ResXResourceWriter(output)) { writer.AddResource("icon", icon); writer.Generate(); }
            Assert.DoesNotContain("binary", output.ToString(), StringComparison.OrdinalIgnoreCase);
            using var reader = new ResXResourceReader(new StringReader(output.ToString()));
            var entries = reader.GetEnumerator(); Assert.True(entries.MoveNext()); var resource = Assert.IsType<RibbonSvgIcon>(entries.Value);
            Assert.Equal(icon.Source, resource.Source); Assert.Equal(icon.UseForegroundColor, resource.UseForegroundColor);
        }
    });

    [Fact]
    public void Svg_metadata_serializes_svg_only_and_retains_borrowed_artwork() => FoundationTests.Sta(() =>
    {
        using var item = new RibbonButtonItem(); using var image = new Bitmap(16, 16); item.Image = image;
        var property = TypeDescriptor.GetProperties(item)["Icon"]!; var custom = new CustomIcon(); item.Icon = custom;
        Assert.False(property.ShouldSerializeValue(item)); item.Icon = RibbonStockIcons.Create("copy"); Assert.True(property.ShouldSerializeValue(item));
        item.Icon = null; Assert.False(property.ShouldSerializeValue(item)); Assert.Same(image, item.Image); Assert.False(custom.Disposed);
    });

    [Fact]
    public void Designer_icon_selection_is_detached_commits_once_and_preserves_unchanged_instances() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var button = (RibbonButtonItem)ribbon.Tabs[0].Groups[0].Items[0]; using var image = new Bitmap(16, 16); button.Image = image;
        var custom = new CustomIcon(); button.Icon = custom; var draft = RibbonDesignCommitter.Read(ribbon, host);
        using (var dialog = new RibbonEditorDialog(draft, _ => throw new InvalidOperationException("Do not accept")))
        {
            var tree = (TreeView)dialog.Controls.Find("StructureTree", true).Single(); _ = tree.Handle; tree.SelectedNode = tree.Nodes[0].Nodes[0].Nodes[0].Nodes[0];
            dialog.SetIcon(new() { Kind = "Svg", Source = "stock:save", UseForegroundColor = true });
            Assert.Same(custom, button.Icon); Assert.Equal(0, host.Transactions);
        }
        Assert.True(RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(1, host.Committed);
        var assigned = Assert.IsType<RibbonSvgIcon>(button.Icon); Assert.Equal("stock:save", assigned.Source); Assert.Same(image, button.Image); Assert.False(custom.Disposed);
        draft = RibbonDesignCommitter.Read(ribbon, host); draft.Tabs[0].Properties.Single(value => value.Name == "Text").Value = "Changed tab";
        RibbonDesignCommitter.Apply(ribbon, host, draft); Assert.Same(assigned, button.Icon);
        draft = RibbonDesignCommitter.Read(ribbon, host); draft.Nodes.Single(node => node.Kind == "Button").Icon = new();
        RibbonDesignCommitter.Apply(ribbon, host, draft); Assert.Null(button.Icon); Assert.Same(image, button.Image);
    });

    [Fact]
    public void Invalid_icon_draft_is_rejected_before_live_mutation() => FoundationTests.Sta(() =>
    {
        using var host = new DesignerAuthoringTests.Host(); using var ribbon = DesignerAuthoringTests.MakeRibbon(host);
        var draft = RibbonDesignCommitter.Read(ribbon, host); draft.Nodes.Single(node => node.Kind == "Button").Icon = new() { Kind = "Svg", Source = "invalid" };
        Assert.ThrowsAny<Exception>(() => RibbonDesignCommitter.Apply(ribbon, host, draft)); Assert.Equal(0, host.Transactions);
        Assert.Null(((RibbonButtonItem)ribbon.Tabs[0].Groups[0].Items[0]).Icon);
    });

    [Fact]
    public void Picker_filters_stock_and_embeds_imported_artwork_without_a_file_link() => FoundationTests.Sta(() =>
    {
        using var picker = new RibbonIconPickerDialog(Array.Empty<RibbonDesignIcon>(), new());
        var search = (TextBox)picker.Controls.Find("IconSearch", true).Single(); var list = (ListBox)picker.Controls.Find("IconList", true).Single();
        search.Text = "Save"; Assert.True(list.Items.Count > 1); picker.AcceptSelection(); Assert.Equal("stock:save", picker.Selection!.Source);
        var tint = (CheckBox)picker.Controls.Find("UseRibbonColor", true).Single(); tint.Checked = false;
        search.Text = "Sav"; picker.AcceptSelection(); Assert.False(picker.Selection!.UseForegroundColor);
        picker.AddImportedSvg(TwoColors, "My artwork"); picker.AcceptSelection();
        Assert.Contains("My artwork", picker.Selection!.Source); Assert.Contains("<svg", picker.Selection.Source); Assert.False(picker.Selection.UseForegroundColor);
    });

    [Fact]
    public void Expanded_catalog_keeps_original_identifiers_and_supports_specific_searches() => FoundationTests.Sta(() =>
    {
        string[] original = ["new", "open", "save", "print", "undo", "redo", "copy", "cut", "paste", "bold", "italic", "underline", "find", "picture", "link", "settings", "delete", "refresh", "help", "zoom-in", "zoom-out", "table", "check", "close"];
        Assert.Equal(original, RibbonStockIcons.All.Take(original.Length).Select(icon => icon.Key));
        foreach (var key in original) Assert.Equal("stock:" + key, RibbonStockIcons.Create(key).Source);
        using var picker = new RibbonIconPickerDialog(Array.Empty<RibbonDesignIcon>(), new());
        var search = (TextBox)picker.Controls.Find("IconSearch", true).Single(); var list = (ListBox)picker.Controls.Find("IconList", true).Single();
        Assert.Equal(512, list.Items.Count);
        foreach (string term in new[] { "Document", "Text", "Table", "Arrow", "Person", "Calendar", "Image", "Shield", "Code", "Cloud" })
        {
            search.Text = term; Assert.True(list.Items.Count > 0, "Missing search category: " + term);
            Assert.All(list.Items.Cast<object>(), item => Assert.Contains(term, item.ToString()!, StringComparison.OrdinalIgnoreCase));
        }
        search.Text = "no such icon 1723"; Assert.Empty(list.Items); Assert.False(((Button)picker.Controls.Find("AcceptIcon", true).Single()).Enabled);
        search.Clear(); Assert.Equal(512, list.Items.Count);
    });

    private sealed class CustomIcon : IRibbonIcon, IDisposable
    {
        internal bool Disposed; public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) { } public void Dispose() => Disposed = true;
    }
}
