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
        var group = new RibbonGroup { Id = "group.messages", Text = "Messages" }; group.Items.AddRange(add, reopen, clear);
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
    internal async Task CaptureMessages(string directory)
    {
        Directory.CreateDirectory(directory); Width = 1100; Activate(); ribbon.SelectedTab = home;
        AddSampleMessage(); AddSampleMessage();
        foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
        foreach (var position in new[] { RibbonQuickAccessPosition.TabRow, RibbonQuickAccessPosition.BelowRibbon })
        foreach (bool minimized in new[] { false, true })
        {
            ribbon.Density = density; ribbon.QuickAccessPosition = position; ribbon.IsMinimized = minimized;
            await Capture($"{density}-{position}-{(minimized ? "minimized" : "expanded")}");
        }
        ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; Width = 440;
        await Capture("Touch-not-below-minimized-narrow");
        async Task Capture(string name)
        {
            await Task.Delay(100); Refresh(); Update();
            if (!ContainsFocus) throw new InvalidOperationException("Showcase lost focus before message capture.");
            using var bitmap = new Bitmap(Width, Height); using var g = Graphics.FromImage(bitmap);
            g.CopyFromScreen(Location, Point.Empty, Size); bitmap.Save(Path.Combine(directory, name + ".png"));
        }
    }
}
