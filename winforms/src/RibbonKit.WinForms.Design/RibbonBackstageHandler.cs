using System.ComponentModel.Design;
using Microsoft.DotNet.DesignTools.Protocol.Endpoints;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

[ExportRequestHandler(RibbonBackstageEndpoint.EndpointName)]
internal sealed class RibbonBackstageHandler : RequestHandler<RibbonBackstageRequest, RibbonBackstageResponse>
{
    public override RibbonBackstageResponse HandleRequest(RibbonBackstageRequest request)
    {
        try
        {
            var ribbon = request.Ribbon as RibbonControl ?? (request.Ribbon as RibbonBackstage)?.Ribbon;
            if (ribbon is null) return new(error: "Select a RibbonControl before opening Edit Backstage.");
            var host = GetDesignerHost(request.SessionId) ?? throw new InvalidOperationException("A Visual Studio designer session is required.");
            if (request.Draft.Length == 0) return new(RibbonBackstageCommitter.Read(ribbon, host).Serialize());
            var draft = RibbonBackstageSnapshot.Deserialize(request.Draft);
            if (request.Preview) return new(RibbonBackstageSnapshot.Json(RibbonBackstagePreviewRenderer.Render(ribbon, host, draft)));
            RibbonBackstageCommitter.Apply(ribbon, host, draft);
            string? name = draft.Entries.FirstOrDefault(node => node.Key == draft.SelectionKey)?.ComponentName;
            var selected = ribbon.Backstage.NavigationItems.FirstOrDefault(item => name is not null && item.Site?.Name == name);
            (host.GetService(typeof(ISelectionService)) as ISelectionService)?.SetSelectedComponents(new object[] { (object?)selected ?? ribbon }, SelectionTypes.Replace);
            return new();
        }
        catch (Exception error) { return new(error: error.Message); }
    }
}
