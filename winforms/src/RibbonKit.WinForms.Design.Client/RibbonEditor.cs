using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Threading;
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
    { new TypeRoutingDefinition(TypeRoutingKinds.Editor, RibbonDesignEndpoint.EditorName, typeof(RibbonEditor)),
      new TypeRoutingDefinition(TypeRoutingKinds.Editor, RibbonBackstageEndpoint.EditorName, typeof(RibbonBackstageEditor)) };
}

internal sealed class RibbonBackstageEditor : UITypeEditor
{
    public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext? context) => UITypeEditorEditStyle.Modal;
    public override object? EditValue(ITypeDescriptorContext? context, IServiceProvider provider, object? value)
    {
        if (context?.Instance is null || provider is null) return value;
        var client = provider.GetRequiredService<IDesignToolsClient>(); var session = provider.GetRequiredService<DesignerSession>();
        var sender = client.Protocol.GetEndpoint<RibbonBackstageEndpoint>().GetSender(client);
        var response = sender.SendRequest(new RibbonBackstageRequest(session.Id, context.Instance));
        if (response.Error.Length != 0) { MessageBox.Show(response.Error, "Edit Backstage", MessageBoxButtons.OK, MessageBoxIcon.Error); return value; }
        using var dialog = new RibbonBackstageDialog(RibbonBackstageSnapshot.Deserialize(response.Snapshot), draft =>
        {
            var result = sender.SendRequest(new RibbonBackstageRequest(session.Id, context.Instance, draft.Serialize())); return result.Error.Length == 0 ? null : result.Error;
        }, draft =>
        {
            var result = sender.SendRequest(new RibbonBackstageRequest(session.Id, context.Instance, draft.Serialize(), true));
            if (result.Error.Length != 0) throw new InvalidOperationException(result.Error);
            return RibbonBackstageSnapshot.FromJson<RibbonBackstagePreview>(result.Snapshot);
        });
        provider.GetRequiredService<IWindowsFormsEditorService>().ShowDialog(dialog); return value;
    }
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
        string? acceptedSelection = null;
        var ribbon = context.Instance;
        using var dialog = new RibbonEditorDialog(RibbonDesignSnapshot.Deserialize(response.Snapshot), draft =>
        {
            var result = sender.SendRequest(new RibbonDesignRequest(session.Id, context.Instance, draft.Serialize()));
            if (result.Error.Length == 0) acceptedSelection = draft.Serialize();
            return result.Error.Length == 0 ? null : result.Error;
        }, draft =>
        {
            var result = sender.SendRequest(new RibbonDesignRequest(session.Id, context.Instance, draft.Serialize(), true));
            if (result.Error.Length != 0) throw new InvalidOperationException(result.Error);
            return RibbonBackstageSnapshot.FromJson<RibbonThemePreview>(result.Snapshot);
        });
        if (provider.GetRequiredService<IWindowsFormsEditorService>().ShowDialog(dialog) == DialogResult.OK && acceptedSelection is { } selection)
        {
            void SelectAccepted()
            {
                try
                {
                    var result = sender.SendRequest(new RibbonDesignRequest(session.Id, ribbon, selection, selectOnly: true));
                    if (result.Error.Length != 0) MessageBox.Show(result.Error, "Select ribbon component", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch (Exception error) { MessageBox.Show(error.Message, "Select ribbon component", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            // Selecting during the commit happens inside the modal editor. VS
            // can restore its original property owner as EditValue unwinds.
            // Publish the selection after returning to the designer's UI loop.
            if (SynchronizationContext.Current is { } ui) ui.Post(_ => SelectAccepted(), null);
            else SelectAccepted();
        }
        return value;
    }
}
