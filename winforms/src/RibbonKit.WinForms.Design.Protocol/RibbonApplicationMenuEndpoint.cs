using System.Composition;
using Microsoft.DotNet.DesignTools.Protocol;
using Microsoft.DotNet.DesignTools.Protocol.DataPipe;
using Microsoft.DotNet.DesignTools.Protocol.Endpoints;

namespace RibbonKit.WinForms.Design.Protocol;

[Shared, ExportEndpoint]
public sealed class RibbonApplicationMenuEndpoint : Endpoint<RibbonApplicationMenuRequest, RibbonApplicationMenuResponse>
{
    public const string EndpointName = "RibbonKit.EditApplicationMenu";
    public const string EditorName = "RibbonKitEditApplicationMenu";
    public override string Name => EndpointName;
    protected override RibbonApplicationMenuRequest CreateRequest(IDataPipeReader reader) => new(reader);
    protected override RibbonApplicationMenuResponse CreateResponse(IDataPipeReader reader) => new(reader);
}

// The SDK registers both message types globally, in addition to endpoint names.
// A second endpoint must not reuse the Edit Ribbon request/response types.
public sealed class RibbonApplicationMenuRequest : Request
{
    public SessionId SessionId { get; private set; }
    public object? Ribbon { get; private set; }
    public string Draft { get; private set; } = "";
    public bool Preview { get; private set; }
    public bool SelectOnly { get; private set; }
    public RibbonApplicationMenuRequest(SessionId session, object ribbon, string draft = "", bool preview = false, bool selectOnly = false)
    { SessionId = session; Ribbon = ribbon; Draft = draft; Preview = preview; SelectOnly = selectOnly; }
    public RibbonApplicationMenuRequest(IDataPipeReader reader) : base(reader) { }
    protected override void ReadProperties(IDataPipeReader reader)
    { SessionId = reader.ReadSessionId(nameof(SessionId)); Ribbon = reader.ReadObject(nameof(Ribbon)); Draft = reader.ReadString(nameof(Draft)) ?? ""; Preview = reader.ReadBoolean(nameof(Preview)); SelectOnly = reader.ReadBoolean(nameof(SelectOnly)); }
    protected override void WriteProperties(IDataPipeWriter writer)
    { writer.Write(nameof(SessionId), SessionId); writer.WriteObject(nameof(Ribbon), Ribbon); writer.Write(nameof(Draft), Draft); writer.Write(nameof(Preview), Preview); writer.Write(nameof(SelectOnly), SelectOnly); }
}
public sealed class RibbonApplicationMenuResponse : Response
{
    public string Snapshot { get; private set; } = "";
    public string Error { get; private set; } = "";
    public RibbonApplicationMenuResponse(string snapshot = "", string error = "") { Snapshot = snapshot; Error = error; }
    public RibbonApplicationMenuResponse(IDataPipeReader reader) : base(reader) { }
    protected override void ReadProperties(IDataPipeReader reader)
    { Snapshot = reader.ReadString(nameof(Snapshot)) ?? ""; Error = reader.ReadString(nameof(Error)) ?? ""; }
    protected override void WriteProperties(IDataPipeWriter writer)
    { writer.Write(nameof(Snapshot), Snapshot); writer.Write(nameof(Error), Error); }
}
