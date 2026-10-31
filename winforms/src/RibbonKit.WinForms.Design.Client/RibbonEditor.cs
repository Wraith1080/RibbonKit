using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using Microsoft.DotNet.DesignTools.Client;
using Microsoft.DotNet.DesignTools.Client.TypeRouting;
using RibbonKit.WinForms.Design.Protocol;

namespace RibbonKit.WinForms.Design.Client;

[ExportTypeRoutingDefinitionProvider]
internal sealed class RibbonTypeRouting : TypeRoutingDefinitionProvider
{
    public override IEnumerable<TypeRoutingDefinition> GetDefinitions() => new[]
    { new TypeRoutingDefinition(TypeRoutingKinds.Editor, RibbonDesignEndpoint.EditorName, typeof(RibbonEditor)) };
}

internal sealed class RibbonEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) => UITypeEditorEditStyle.Modal;
    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
    {
        if (context?.Instance == null || provider == null) return value;
        var client = provider.GetRequiredService<IDesignToolsClient>();
        var session = provider.GetRequiredService<DesignerSession>();
        var sender = client.Protocol.GetEndpoint<RibbonDesignEndpoint>().GetSender(client);
        var response = sender.SendRequest(new RibbonDesignRequest(session.Id, context.Instance));
        if (response.Error.Length != 0) { MessageBox.Show(response.Error, "Edit Ribbon", MessageBoxButtons.OK, MessageBoxIcon.Error); return value; }
        using var dialog = new RibbonEditorDialog(RibbonDesignSnapshot.Deserialize(response.Snapshot), draft =>
        {
            var result = sender.SendRequest(new RibbonDesignRequest(session.Id, context.Instance, draft.Serialize()));
            return result.Error.Length == 0 ? null : result.Error;
        });
        provider.GetRequiredService<IWindowsFormsEditorService>().ShowDialog(dialog);
        return value;
    }
}
