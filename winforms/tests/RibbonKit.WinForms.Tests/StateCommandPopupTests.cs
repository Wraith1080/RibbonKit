using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class StateCommandPopupTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (string presentation in new[] { "collapsed", "simplified-group", "simplified-overflow", "qat-group" })
        foreach (string kind in new[] { "toggle", "split", "checkbox", "radio" }) yield return [presentation, kind];
    }

    [Theory] [MemberData(nameof(Cases))]
    public void State_actions_retain_the_same_container_and_raise_state_before_click(string presentation, string kind) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(presentation, kind); var surface = f.Open(); var entry = f.Entry(surface);
        var events = new List<string>();
        switch (f.State)
        {
            case RibbonToggleItem item: item.CheckedChanged += (_, _) => events.Add("state"); break;
            case RibbonSplitButtonItem item: item.CheckedChanged += (_, _) => events.Add("state"); break;
            case RibbonCheckBoxItem item: item.CheckedChanged += (_, _) => events.Add("state"); break;
            case RibbonRadioButtonItem item: item.CheckedChanged += (_, _) => events.Add("state"); break;
        }
        if (f.State is RibbonButtonItem button) button.Click += (_, _) => events.Add("click");
        else ((RibbonSplitButtonItem)f.State).Click += (_, _) => events.Add("click");
        var target = surface.Targets.Single(t => ReferenceEquals(t.Element, entry) && !t.DropDownPart);
        // Exercise the native popup's pointer route, including focus and split halves.
        var point = target.VisibleBounds.Location; point.Offset(Math.Max(1, target.VisibleBounds.Width / 2), Math.Max(1, target.VisibleBounds.Height / 2));
        SendMessage(surface.Handle, 0x201, 1, (nint)(point.Y << 16 | point.X)); SendMessage(surface.Handle, 0x202, 0, (nint)(point.Y << 16 | point.X));
        Assert.True(f.Checked); Assert.False(f.Peer.Checked); Assert.Equal(new[] { "state", "click" }, events);
        Assert.Same(surface, f.Ribbon.Popups.Top); Assert.False(surface.IsDisposed); Assert.Same(entry, surface.FocusedElement);
        // The keyboard route must keep the same session too, rather than reopening it.
        surface.FocusElement(entry); var key = Message.Create(surface.Handle, 0x100, (nint)Keys.Space, 0);
        Assert.True(surface.PreProcessMessage(ref key));
        Assert.Same(surface, f.Ribbon.Popups.Top); Assert.False(surface.IsDisposed);
        Assert.Equal(kind == "radio", f.Checked); Assert.Equal(2, events.Count(e => e == "click"));
    });

    [Theory] [InlineData("collapsed")] [InlineData("simplified-group")] [InlineData("simplified-overflow")] [InlineData("qat-group")]
    public void Ordinary_actions_and_non_toggle_split_primaries_still_dismiss(string presentation) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(presentation, "split"); f.Split.CheckOnClick = false;
        var surface = f.Open(); f.Activate(surface, f.Entry(surface)); Assert.False(f.Ribbon.Popups.HasOpen); Assert.False(f.Split.Checked);
        surface = f.Open(); f.Activate(surface, f.Entry(surface, f.Command)); Assert.False(f.Ribbon.Popups.HasOpen);
    });

    [Theory] [InlineData("collapsed")] [InlineData("simplified-group")] [InlineData("simplified-overflow")] [InlineData("qat-group")]
    public void Split_menu_commands_and_explicit_callback_closure_are_preserved(string presentation) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(presentation, "split"); var surface = f.Open(); var entry = f.Entry(surface);
        f.Ribbon.Activate(surface.Targets.Single(t => ReferenceEquals(t.Element, entry) && t.DropDownPart), surface);
        var menu = f.Ribbon.Popups.Top!; Assert.NotSame(surface, menu); Assert.False(f.Split.Checked);
        f.Activate(menu, f.Split.Items[0]); Assert.False(f.Ribbon.Popups.HasOpen); Assert.False(f.Split.Checked);
        surface = f.Open(); f.Split.Click += (_, _) => f.Ribbon.Popups.CloseAll(false);
        f.Activate(surface, f.Entry(surface)); Assert.True(f.Split.Checked); Assert.False(f.Ribbon.Popups.HasOpen);
    });

    private sealed class Fixture : IDisposable
    {
        internal readonly Form Form = new() { Size = new(450, 700) };
        internal readonly RibbonControl Ribbon = new() { AnimationLevel = RibbonAnimationLevel.None };
        internal readonly RibbonGroup Group = new() { Text = "Formatting", ReductionMode = RibbonGroupReductionMode.Collapse };
        internal readonly RibbonButtonItem Command = new() { Text = "Run command", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedPriority = -1000 };
        internal readonly RibbonRadioButtonItem Peer = new() { Text = "Other choice", Checked = true };
        internal readonly RibbonItem State;
        private readonly string _presentation;
        private readonly RibbonQuickAccessGroupReference _qat;
        internal RibbonSplitButtonItem Split => (RibbonSplitButtonItem)State;
        internal bool Checked => State switch { RibbonToggleItem item => item.Checked, RibbonSplitButtonItem item => item.Checked,
            RibbonCheckBoxItem item => item.Checked, RibbonRadioButtonItem item => item.Checked, _ => false };
        internal Fixture(string presentation, string kind)
        {
            _presentation = presentation;
            State = kind switch { "toggle" => new RibbonToggleItem(), "split" => new RibbonSplitButtonItem { CheckOnClick = true },
                "checkbox" => new RibbonCheckBoxItem(), _ => new RibbonRadioButtonItem() };
            State.Text = "Change state"; State.SimplifiedPlacement = RibbonSimplifiedPlacement.Inline; State.SimplifiedPriority = -1000;
            if (State is RibbonSplitButtonItem split) split.Items.Add(new RibbonButtonItem { Text = "Menu command" });
            if (State is not RibbonRadioButtonItem) Peer.Checked = false;
            Group.Items.AddRange(State, Peer, Command);
            for (int i = 0; i < 12; i++) Group.Items.Add(new RibbonButtonItem { Text = "Priority command " + i,
                SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, SimplifiedPriority = 100, SimplifiedSize = RibbonItemSize.Medium });
            var tab = new RibbonTab { Text = "Home" }; tab.Groups.Add(Group); Ribbon.Tabs.Add(tab); Form.Controls.Add(Ribbon);
            _qat = new() { Source = Group };
            if (presentation == "qat-group") Ribbon.QuickAccessItems.Add(_qat);
            if (presentation.StartsWith("simplified", StringComparison.Ordinal)) Ribbon.LayoutMode = RibbonLayoutMode.Simplified;
            if (presentation == "simplified-group") foreach (var item in Group.Items) item.SimplifiedPlacement = RibbonSimplifiedPlacement.Overflow;
            Form.Show(); Form.Activate(); Application.DoEvents();
        }
        internal RibbonPopupSurface Open()
        {
            Ribbon.Popups.CloseAll(false);
            if (_presentation == "qat-group") Ribbon.Activate(Ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, _qat)));
            else if (_presentation.StartsWith("simplified", StringComparison.Ordinal))
            {
                Ribbon.Activate(Ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, Ribbon.Simplified.More)));
                if (_presentation == "simplified-group")
                {
                    var more = Ribbon.Popups.Top!;
                    Ribbon.Activate(more.Targets.Single(t => t.Element is RibbonQuickAccessGroupReference reference && ReferenceEquals(reference.Source, Group)), more);
                }
            }
            else
            {
                Assert.True(Ribbon.Snapshot.Groups.Single().Collapsed);
                Ribbon.Activate(Ribbon.Snapshot.Targets.Single(t => ReferenceEquals(t.Element, Group)));
            }
            return Ribbon.Popups.Top!;
        }
        internal RibbonElement Entry(RibbonPopupSurface surface, RibbonItem? item = null)
        {
            item ??= State;
            return surface.Targets.Select(t => t.Element).First(e => ReferenceEquals(e, item) || e is RibbonQuickAccessReference reference && ReferenceEquals(reference.Source, item)
                || e is RibbonQuickAccessDropDownReference drop && ReferenceEquals(drop.Source, item))!;
        }
        internal void Activate(RibbonPopupSurface surface, RibbonElement entry) => Ribbon.Activate(surface.Targets.Single(t => ReferenceEquals(t.Element, entry) && !t.DropDownPart), surface);
        public void Dispose() { Form.Dispose(); _qat.Dispose(); }
    }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window, uint message, nint wParam, nint lParam);
}
