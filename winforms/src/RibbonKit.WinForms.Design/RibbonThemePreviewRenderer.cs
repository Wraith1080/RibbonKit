using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Imaging;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

// Draw detached declarations only. No application Controls, event handlers,
// bindings or commands are copied into this inert design-mode ribbon.
internal static class RibbonThemePreviewRenderer
{
    internal static RibbonThemePreview Render(RibbonControl ribbon, IDesignerHost host, RibbonDesignSnapshot draft)
    {
        var before = RibbonDesignCommitter.Read(ribbon, host);
        if (draft.Revision != before.Revision) throw new ArgumentException("The ribbon changed while the editor was open. Cancel and reopen Edit Ribbon.");
        var theme = RibbonDesignCommitter.ParseTheme(draft.BuiltInTheme);
        _ = RibbonDesignCommitter.ParseAnimation(draft.AnimationLevel);
        _ = RibbonDesignCommitter.ParseContentAnimation(draft.TabContentAnimation);
        draft.ReservedIds = before.ReservedIds; draft.ReservedNames = before.ReservedNames; draft.Validate();
        using var probe = new PreviewRibbon();
        probe.Site = new PreviewSite(probe);
        probe.Theme = ribbon.ApplicationTheme; probe.BuiltInTheme = theme;
        probe.Renderer = ribbon.Renderer; probe.Font = ribbon.Font;
        probe.Density = ribbon.Density; probe.LayoutMode = ribbon.LayoutMode;
        probe.AnimationLevel = RibbonAnimationLevel.None;
        probe.Width = Math.Clamp(ribbon.Width, 640, 1600);
        probe.FileMenu.Text = ribbon.FileMenu.Text; probe.FileMenu.KeyTip = ribbon.FileMenu.KeyTip;
        probe.FileMenu.Visible = ribbon.FileMenu.Visible; probe.FileMenu.Enabled = ribbon.FileMenu.Enabled;
        probe.FileMenuMode = ribbon.FileMenuMode;
        var originals = before.Nodes.ToDictionary(node => node.Key, StringComparer.Ordinal);
        var components = ribbon.Tabs.SelectMany(tab => tab.Tree()).ToDictionary(RibbonDesignCommitter.Key, StringComparer.Ordinal);
        foreach (var node in draft.Tabs)
        {
            var tab = (RibbonTab)RibbonDesignCommitter.BuildProbe(node, originals, before.Templates);
            probe.Tabs.Add(tab);
            CopyArtwork(node, tab);
        }
        void CopyArtwork(RibbonDesignNode node, RibbonElement element)
        {
            var icon = node.Icon.Kind == "Svg" ? new RibbonSvgIcon(node.Icon.Source, node.Icon.UseForegroundColor)
                : node.Icon.Kind == "Custom" && components.TryGetValue(node.Key, out var source)
                    ? source is RibbonItem item ? item.Icon : (source as RibbonGroup)?.Icon : null;
            if (element is RibbonGroup group) group.Icon = icon;
            if (element is RibbonItem command)
            {
                command.Icon = icon;
                if (components.GetValueOrDefault(node.Key) is RibbonItem original)
                { command.Image = original.Image; command.LargeImage = original.LargeImage; }
            }
            var children = element switch
            {
                RibbonTab tab => tab.Groups.Cast<RibbonElement>(), RibbonGroup owner => owner.Items,
                RibbonGalleryItem gallery => gallery.Items, RibbonDropDownItem menu => menu.Items,
                _ => Enumerable.Empty<RibbonElement>()
            };
            foreach (var pair in node.Children.Zip(children)) CopyArtwork(pair.First, pair.Second);
        }
        if (ribbon.SelectedTab is { } selected && draft.Tabs.FindIndex(node => node.Key == RibbonDesignCommitter.Key(selected)) is var index && index >= 0)
            probe.SetDesignPreviewTab(probe.Tabs[index]);
        using var pixels = new Bitmap(probe.Width, probe.Height);
        using (var graphics = Graphics.FromImage(pixels)) probe.PaintPreview(graphics);
        using var stream = new MemoryStream(); pixels.Save(stream, ImageFormat.Png);
        var backstage = RibbonBackstageCommitter.Read(ribbon, host);
        if ((ribbon.Backstage.SelectedPage ?? ribbon.Backstage.NavigationItems.OfType<RibbonBackstagePageItem>().FirstOrDefault(page => page.Visible && page.EffectiveEnabled)) is { } page)
            backstage.SelectionKey = RibbonDesignCommitter.Key(page);
        backstage.PreviewWidth = probe.Width; backstage.PreviewHeight = 420; backstage.PreviewDpi = probe.DeviceDpi;
        return new() { RibbonPng = Convert.ToBase64String(stream.ToArray()), BackstagePng = RibbonBackstagePreviewRenderer.Render(ribbon, host, backstage, probe.Theme).Png };
    }
    private sealed class PreviewRibbon : RibbonControl
    {
        internal void PaintPreview(Graphics graphics) => OnPaint(new PaintEventArgs(graphics, ClientRectangle));
    }
    private sealed class PreviewSite(IComponent component) : ISite
    {
        public IComponent Component => component;
        public IContainer? Container => null;
        public bool DesignMode => true;
        public string? Name { get; set; }
        public object? GetService(Type serviceType) => null;
    }
}
