using System.ComponentModel.Design;
using Microsoft.DotNet.DesignTools.Protocol.Endpoints;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

[ExportRequestHandler(RibbonApplicationMenuEndpoint.EndpointName)]
internal sealed class RibbonApplicationMenuHandler : RequestHandler<RibbonApplicationMenuRequest, RibbonApplicationMenuResponse>
{
    public override RibbonApplicationMenuResponse HandleRequest(RibbonApplicationMenuRequest request)
    {
        try
        {
            var ribbon = request.Ribbon as RibbonControl ?? (request.Ribbon as RibbonElement)?.Ribbon;
            if (ribbon is null) return new(error: "Select a ribbon to edit its Application Menu.");
            var host = GetDesignerHost(request.SessionId) ?? throw new InvalidOperationException("A designer session is required.");
            if (request.Draft.Length == 0) return new(RibbonDesignCommitter.ReadApplicationMenu(ribbon, host).Serialize());
            var draft = RibbonBackstageSnapshot.Deserialize(request.Draft);
            if (request.Preview) return new(RibbonBackstageSnapshot.Json(RibbonDesignCommitter.PreviewApplicationMenu(ribbon, host, draft)));
            if (!request.SelectOnly) RibbonDesignCommitter.ApplyApplicationMenu(ribbon, host, draft);
            var node = RibbonDesignSnapshot.Flatten(draft.Entries.SelectMany(s => s.Children)).FirstOrDefault(n => n.Key == draft.SelectionKey);
            var selected = draft.SelectionKey == "default" ? ribbon.ApplicationMenu.DefaultPane : draft.SelectionKey == "file" ? ribbon.FileMenu : draft.SelectionKey == "footer" ? ribbon.ApplicationMenu : ribbon.AllElements.FirstOrDefault(e => node is not null && e.Site?.Name == node.ComponentName);
            (host.GetService(typeof(ISelectionService)) as ISelectionService)?.SetSelectedComponents(new object[] { (object?)selected ?? ribbon }, SelectionTypes.Replace);
            return new();
        }
        catch (Exception error) { return new(error: error.Message); }
    }
}
