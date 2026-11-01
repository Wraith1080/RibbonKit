using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Reflection;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

// Inspect the actual packaged server in a separate process. Reflection is only
// a test entry point for internal designer types; it is not a consumer API.
internal static class BackstageDesignerPreviewCheck
{
    internal static void Verify(string serverPath, string output)
    {
        var server = Assembly.LoadFrom(serverPath);
        using var surface = new DesignSurface(typeof(RibbonForm)); var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost))!;
        var root = (RibbonForm)host.RootComponent; root.ClientSize = new(900, 550); _ = ((Control)surface.View!).Handle; _ = root.Handle;
        using var ribbon = new RibbonControl(); ribbon.Site = new PreviewSite(ribbon); root.Controls.Add(ribbon);
        using var body = new Body(); var editorHandle = body.Editor.Handle; body.Editor.Select(2, 4);
        var info = new RibbonBackstagePageItem { Text = "Info", Content = body };
        var print = new RibbonBackstagePageItem { Text = "Print" }; var save = new RibbonButtonItem { Text = "Save" }; int clicks = 0; save.Click += (_, _) => clicks++;
        ribbon.Backstage.Items.AddRange(info, save); ribbon.Backstage.FooterItems.AddRange(new RibbonMenuSeparatorItem(), print);
        var selection = new PreviewSelection(); selection.SetSelectedComponents(new[] { ribbon });
        var previewType = server.GetType("RibbonKit.WinForms.Design.RibbonBackstageDesignPreview", true)!;
        var preview = Activator.CreateInstance(previewType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { ribbon, selection, root }, null)!;
        var layerType = server.GetType("RibbonKit.WinForms.Design.RibbonBackstagePreviewLayer", true)!;
        using var layer = (Control)Activator.CreateInstance(layerType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { root }, null)!;
        object? Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);
        T Property<T>(object target, string name) => (T)target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.GetValue(target)!;
        Func<Bitmap> frame = () => (Bitmap)Call(preview, "CreateFrame", (float)ribbon.DeviceDpi, (float)ribbon.DeviceDpi)!;
        var controls = root.Controls.Cast<Control>().ToArray(); int components = host.Container.Components.Count;
        Call(preview, "Open"); var bounds = Property<Rectangle>(preview, "Bounds");
        Require((bool)Call(layer, "Present", bounds, frame)!, "Presenting the packaged preview failed"); var handle = layer.Handle;
        Require(GetParent(handle) == root.Handle && layer.Parent is null && layer.Site is null, "Temporary preview entered the authored ownership tree");
        using var before = frame();
        var rows = ((IEnumerable)Call(preview, "Rows")!).Cast<object>().ToArray(); var printRow = rows.Single(row => ReferenceEquals(Property<RibbonItem>(row, "Item"), print));
        var rowBounds = Property<Rectangle>(printRow, "VisibleBounds"); Call(preview, "Click", new Point(rowBounds.Left + 2, rowBounds.Top + 2));
        Require(ReferenceEquals(selection.PrimarySelection, print) && ReferenceEquals(Property<RibbonBackstagePageItem>(preview, "Page"), print), "Footer page selection did not reach Properties");
        Require((bool)Call(layer, "Present", bounds, frame)! && layer.Handle == handle, "Page selection recreated the preview window");
        using var after = frame();
        for (int y = 0; y < after.Height; y++) for (int x = 450; x < after.Width; x++) Require(before.GetPixel(x, y) == after.GetPixel(x, y), "Shared page body changed on selection");
        root.Invalidate(true); root.Update();
        using var retained = new Bitmap(root.ClientSize.Width, root.ClientSize.Height);
        using (var graphics = Graphics.FromImage(retained)) Require((bool)Call(layer, "PaintRetained", graphics, Point.Empty)!, "Retained adorner pixels were lost after underlay refresh");
        Require(retained.GetPixel(600, bounds.Top + 200) == after.GetPixel(600, 200), "Retained frame origin or pixels changed");
        Require(controls.SequenceEqual(root.Controls.Cast<Control>()) && components == host.Container.Components.Count, "Designer component/Controls collections changed");
        Require(body.Parent is null && body.Paints == 0 && body.Editor.Handle == editorHandle && body.Editor.Text == "Native notes"
            && body.Editor.SelectionStart == 2 && body.Editor.SelectionLength == 4, "Native page content was painted, parented or reset");
        Require(clicks == 0 && !ribbon.IsBackstageOpen && ReferenceEquals(ribbon.Backstage.SelectedPage, info), "Designer preview activated runtime behavior");
        Call(preview, "Close", true); Call(layer, "HidePreview"); Require(!Property<bool>(layer, "IsPresented"), "Closing retained the temporary preview");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "result.log"), "PASS: actual packaged designer server loaded in a separate consumer process; one retained native preview window, shared body across main/footer selection, Properties selection, underlay refresh, caption origin, unchanged authored ownership and native page HWND/text/selection, inert runtime and close cleanup. Internal types invoked by reflection in a public .NET DesignSurface host; actual VS OOP routing and visual flicker acceptance remain separate.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Body : UserControl
    {
        internal TextBox Editor { get; } = new() { Text = "Native notes" }; internal int Paints;
        internal Body() => Controls.Add(Editor);
        protected override void OnPaint(PaintEventArgs e) { Paints++; throw new InvalidOperationException("Do not capture application content."); }
    }
    private sealed class PreviewSite(IComponent component) : ISite
    {
        public IComponent Component => component; public IContainer? Container => null; public bool DesignMode => true; public string? Name { get; set; }
        public object? GetService(Type serviceType) => null;
    }
    private sealed class PreviewSelection : ISelectionService
    {
        private object[] _selected = []; public object? PrimarySelection => _selected.FirstOrDefault(); public int SelectionCount => _selected.Length;
        public event EventHandler? SelectionChanged; public event EventHandler? SelectionChanging;
        public bool GetComponentSelected(object component) => _selected.Contains(component); public ICollection GetSelectedComponents() => _selected;
        public void SetSelectedComponents(ICollection? components) => SetSelectedComponents(components, SelectionTypes.Replace);
        public void SetSelectedComponents(ICollection? components, SelectionTypes selectionType)
        { SelectionChanging?.Invoke(this, EventArgs.Empty); _selected = components?.Cast<object>().ToArray() ?? []; SelectionChanged?.Invoke(this, EventArgs.Empty); }
    }
    [DllImport("user32.dll")] private static extern nint GetParent(nint window);
}
