namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private int _messageNumber;
    private void InitializeMessages()
    {
        var add = new RibbonButtonItem { Text = "Add message" };
        var reopen = new RibbonButtonItem { Text = "Reopen messages" };
        var clear = new RibbonButtonItem { Text = "Clear messages" };
        add.Click += (_, _) => AddSampleMessage();
        reopen.Click += (_, _) => { foreach (var message in ribbon.MessageBar.Items) message.IsOpen = true; };
        clear.Click += (_, _) => { ribbon.BeginUpdate(); try { foreach (var message in ribbon.MessageBar.Items.ToArray()) message.Dispose(); } finally { ribbon.EndUpdate(); } };
        var commands = new RibbonDropDownItem { Id = "demo.messages.menu", Text = "Messages", KeyTip = "M", DropDownHeader = "Messages" }; commands.Items.AddRange(add, reopen, clear);
        var group = new RibbonGroup { Id = "group.messages", Text = "Messages" }; group.Items.Add(commands);
        ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Add(group);
    }
    private void AddSampleMessage()
    {
        int number = ++_messageNumber;
        var message = new RibbonMessage { Title = number == 1 ? "PROTECTED VIEW" : "NOTIFICATION " + number,
            Text = number == 1 ? "Review this document before enabling editing." : "Messages stack independently and retain their actions and open state.", Icon = new CommandIcon("info", ribbon) };
        message.Action.Text = number == 1 ? "Enable editing" : "Review";
        message.Action.Icon = new CommandIcon(number == 1 ? "apply" : "preview-change", ribbon);
        message.Action.Click += (_, _) => { status.Text = "Message action " + number + " invoked by the application."; };
        ribbon.MessageBar.Items.Add(message);
    }
}
