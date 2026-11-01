using System.Composition;
using Microsoft.DotNet.DesignTools.Protocol;
using Microsoft.DotNet.DesignTools.Protocol.DataPipe;
using Microsoft.DotNet.DesignTools.Protocol.Endpoints;

namespace RibbonKit.WinForms.Design.Protocol;

[Shared, ExportEndpoint]
public sealed class RibbonBackstageEndpoint : Endpoint<RibbonBackstageRequest, RibbonBackstageResponse>
{
    public const string EndpointName = "RibbonKit.EditBackstage";
    public const string EditorName = "RibbonKitEditBackstage";
    public override string Name => EndpointName;
    protected override RibbonBackstageRequest CreateRequest(IDataPipeReader reader) => new(reader);
    protected override RibbonBackstageResponse CreateResponse(IDataPipeReader reader) => new(reader);
}

// The SDK registers both message types globally, in addition to endpoint names.
// A second endpoint must not reuse the Edit Ribbon request/response types.
public sealed class RibbonBackstageRequest : Request
{
    public SessionId SessionId { get; private set; }
    public object? Ribbon { get; private set; }
    public string Draft { get; private set; } = "";
    public bool Preview { get; private set; }
    public RibbonBackstageRequest(SessionId session, object ribbon, string draft = "", bool preview = false)
    { SessionId = session; Ribbon = ribbon; Draft = draft; Preview = preview; }
    public RibbonBackstageRequest(IDataPipeReader reader) : base(reader) { }
    protected override void ReadProperties(IDataPipeReader reader)
    { SessionId = reader.ReadSessionId(nameof(SessionId)); Ribbon = reader.ReadObject(nameof(Ribbon)); Draft = reader.ReadString(nameof(Draft)) ?? ""; Preview = reader.ReadBoolean(nameof(Preview)); }
    protected override void WriteProperties(IDataPipeWriter writer)
    { writer.Write(nameof(SessionId), SessionId); writer.WriteObject(nameof(Ribbon), Ribbon); writer.Write(nameof(Draft), Draft); writer.Write(nameof(Preview), Preview); }
}
public sealed class RibbonBackstageResponse : Response
{
    public string Snapshot { get; private set; } = "";
    public string Error { get; private set; } = "";
    public RibbonBackstageResponse(string snapshot = "", string error = "") { Snapshot = snapshot; Error = error; }
    public RibbonBackstageResponse(IDataPipeReader reader) : base(reader) { }
    protected override void ReadProperties(IDataPipeReader reader)
    { Snapshot = reader.ReadString(nameof(Snapshot)) ?? ""; Error = reader.ReadString(nameof(Error)) ?? ""; }
    protected override void WriteProperties(IDataPipeWriter writer)
    { writer.Write(nameof(Snapshot), Snapshot); writer.Write(nameof(Error), Error); }
}
