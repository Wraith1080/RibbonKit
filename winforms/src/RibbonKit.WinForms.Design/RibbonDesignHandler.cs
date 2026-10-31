using Microsoft.DotNet.DesignTools.Protocol.Endpoints;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design;

[ExportRequestHandler(RibbonDesignEndpoint.EndpointName)]
internal sealed class RibbonDesignHandler : RequestHandler<RibbonDesignRequest, RibbonDesignResponse>
{
    public override RibbonDesignResponse HandleRequest(RibbonDesignRequest request)
    {
        try
        {
            if (request.Ribbon is not RibbonControl ribbon) return new(error: "Select a RibbonControl before opening Edit Ribbon.");
            var host = GetDesignerHost(request.SessionId) ?? throw new InvalidOperationException("A Visual Studio designer session is required.");
            if (request.Draft.Length == 0) return new(RibbonDesignCommitter.Read(ribbon, host).Serialize());
            var draft = RibbonDesignSnapshot.Deserialize(request.Draft);
            RibbonDesignCommitter.Apply(ribbon, host, draft);
            if (host.GetDesigner(ribbon) is RibbonControlDesigner designer)
                designer.Selection.SelectDraft(draft);
            return new();
        }
        catch (Exception error) { return new(error: error.Message); }
    }
}
