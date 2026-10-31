using System;
using System.Composition;
using Microsoft.DotNet.DesignTools.Protocol;
using Microsoft.DotNet.DesignTools.Protocol.DataPipe;
using Microsoft.DotNet.DesignTools.Protocol.Endpoints;

namespace RibbonKit.WinForms.Design.Protocol;

[Shared, ExportEndpoint]
public sealed class RibbonDesignEndpoint : Endpoint<RibbonDesignRequest, RibbonDesignResponse>
{
    public const string EndpointName = "RibbonKit.EditRibbon";
    public const string EditorName = "RibbonKitEditRibbon";
    public override string Name => EndpointName;
    protected override RibbonDesignRequest CreateRequest(IDataPipeReader reader) => new(reader);
    protected override RibbonDesignResponse CreateResponse(IDataPipeReader reader) => new(reader);
}
public sealed class RibbonDesignRequest : Request
{
    public SessionId SessionId { get; private set; }
    public object? Ribbon { get; private set; }
    public string Draft { get; private set; } = "";
    public RibbonDesignRequest(SessionId session, object ribbon, string draft = "") { SessionId = session; Ribbon = ribbon; Draft = draft; }
    public RibbonDesignRequest(IDataPipeReader reader) : base(reader) { }
    protected override void ReadProperties(IDataPipeReader reader)
    { SessionId = reader.ReadSessionId(nameof(SessionId)); Ribbon = reader.ReadObject(nameof(Ribbon)); Draft = reader.ReadString(nameof(Draft)) ?? ""; }
    protected override void WriteProperties(IDataPipeWriter writer)
    { writer.Write(nameof(SessionId), SessionId); writer.WriteObject(nameof(Ribbon), Ribbon); writer.Write(nameof(Draft), Draft); }
}
public sealed class RibbonDesignResponse : Response
{
    public string Snapshot { get; private set; } = "";
    public string Error { get; private set; } = "";
    public RibbonDesignResponse(string snapshot = "", string error = "") { Snapshot = snapshot; Error = error; }
    public RibbonDesignResponse(IDataPipeReader reader) : base(reader) { }
    protected override void ReadProperties(IDataPipeReader reader) { Snapshot = reader.ReadString(nameof(Snapshot)) ?? ""; Error = reader.ReadString(nameof(Error)) ?? ""; }
    protected override void WriteProperties(IDataPipeWriter writer) { writer.Write(nameof(Snapshot), Snapshot); writer.Write(nameof(Error), Error); }
}
