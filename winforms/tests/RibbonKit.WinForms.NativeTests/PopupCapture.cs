using System.IO;
using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using WF = System.Windows.Forms;

namespace RibbonKit.WinForms.NativeTests;

internal static class PopupCapture
{
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);

    internal static int Run(string directory)
    {
        WF.Application.SetHighDpiMode(WF.HighDpiMode.PerMonitorV2);
        WF.Application.EnableVisualStyles();
        Directory.CreateDirectory(directory);
        using var form = new WF.Form { Text = "Native popup layout capture", Width = 130, Height = 500, StartPosition = WF.FormStartPosition.CenterScreen };
        var ribbon = new RibbonControl();
        var tab = new RibbonTab { Text = "Home" };
        var group = new RibbonGroup { Text = "Font" };
        var bold = new RibbonToggleItem { Text = "Bold", Checked = true, SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium) };
        var italic = new RibbonToggleItem { Text = "Italic", SizePolicy = bold.SizePolicy };
        var disabled = new RibbonButtonItem { Text = "Unavailable", Enabled = false, SizePolicy = bold.SizePolicy };
        int disabledClicks = 0;
        disabled.Click += (_, _) => disabledClicks++;
        bold.Icon = new LetterIcon("B");
        italic.Icon = new LetterIcon("I");
        group.Items.AddRange(bold, italic, disabled);
        tab.Groups.Add(group);
        ribbon.Tabs.Add(tab);
        form.Controls.Add(ribbon);
        Exception? error = null;
        form.Shown += (_, _) => form.BeginInvoke(() =>
        {
            try
            {
                ribbon.Refresh();
                ribbon.Activate(ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, group)));
                var popup = ribbon.Popups.Top ?? throw new InvalidOperationException("Group popup did not open.");
                int padding = RibbonLayoutEngine.Scale(4, ribbon.DeviceDpi);
                if (popup.VerticalScroll.Visible || popup.HorizontalScroll.Visible || popup.Targets.Any(t => t.Bounds.Right != popup.ClientSize.Width - padding))
                    throw new InvalidOperationException("Native popup retained an unused gutter or unmatched row bounds.");
                Save("checked");
                var row = popup.Targets.Single(t => ReferenceEquals(t.Element, italic)).Bounds;
                int x = popup.ClientSize.Width - padding - 5, y = row.Top + row.Height / 2;
                SendMessage(popup.Handle, 0x200, 0, (nint)((y << 16) | (x & 0xffff)));
                Save("hover");
                row = popup.Targets.Single(t => ReferenceEquals(t.Element, disabled)).Bounds;
                x = popup.ClientSize.Width - padding - 5;
                y = row.Top + row.Height / 2;
                nint position = (nint)((y << 16) | (x & 0xffff));
                SendMessage(popup.Handle, 0x200, 0, position);
                Save("disabled-hover");
                SendMessage(popup.Handle, 0x201, 1, position);
                Save("disabled-pressed");
                if (popup.Capture || ReferenceEquals(popup.FocusedElement, disabled))
                    throw new InvalidOperationException("Disabled item captured the mouse or logical focus.");
                SendMessage(popup.Handle, 0x202, 0, position);
                if (disabledClicks != 0)
                    throw new InvalidOperationException("Disabled item executed.");
                Console.WriteLine("PASS: disabled row remains inert during native hover/press/release; captures saved.");
                Console.WriteLine($"PASS: native group popup {popup.Width}x{popup.Height}, DPI={ribbon.DeviceDpi}, row right inset={padding}; checked/hover captures: {directory}");
                void Save(string state)
                {
                    popup.Refresh();
                    using var bitmap = new Bitmap(popup.Width, popup.Height);
                    popup.DrawToBitmap(bitmap, popup.ClientRectangle);
                    bitmap.Save(Path.Combine(directory, $"popup-{state}-{ribbon.DeviceDpi}dpi.png"));
                }
            }
            catch (Exception e) { error = e; }
            finally { form.Close(); }
        });
        WF.Application.Run(form);
        if (error is null)
            return 0;
        Console.Error.WriteLine(error);
        return 1;
    }

    private sealed class LetterIcon(string letter) : IRibbonIcon
    {
        public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) =>
            WF.TextRenderer.DrawText(graphics, letter, SystemFonts.DefaultFont, bounds, context.Foreground,
                WF.TextFormatFlags.HorizontalCenter | WF.TextFormatFlags.VerticalCenter | WF.TextFormatFlags.NoPrefix);
    }
}
