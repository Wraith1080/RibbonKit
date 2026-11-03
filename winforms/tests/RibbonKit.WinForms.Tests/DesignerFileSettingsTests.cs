using System.ComponentModel;
using System.ComponentModel.Design;
using RibbonKit.WinForms.Design;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class DesignerFileSettingsTests
{
    [Fact]
    public void Reading_defaults_and_repeating_current_values_does_not_enable_file_or_create_undo_entries() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var settings = fixture.Settings;
        Assert.False(settings.Visible); Assert.Equal("Application Menu", settings.Opens);
        settings.Visible = false; settings.Opens = "Command menu";
        Assert.Equal(0, fixture.Host.Transactions); Assert.Empty(fixture.Changes.Notifications);
        Assert.DoesNotContain(fixture.Ribbon.Snapshot.Targets, target => target.Kind == TargetKind.FileMenu);
    });

    [Fact]
    public void Show_hide_changes_measured_file_presentation_in_one_transaction_and_retains_the_owned_menu() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); var file = fixture.Ribbon.FileMenu;
        var command = new RibbonButtonItem { Text = "Existing command" }; file.Items.Add(command);
        fixture.Settings.Visible = true;
        Assert.Same(file, fixture.Ribbon.FileMenu); Assert.Same(command, Assert.Single(file.Items));
        Assert.Contains(fixture.Ribbon.Snapshot.Targets, target => ReferenceEquals(target.Element, file) && !target.VisibleBounds.IsEmpty);
        Assert.Equal(1, fixture.Host.Transactions); Assert.Equal(1, fixture.Host.Committed); Assert.Equal("Show File button", fixture.Host.TransactionDescription);
        Assert.Contains(fixture.Changes.Notifications, entry => ReferenceEquals(entry.Owner, fixture.Ribbon) && entry.Name == nameof(fixture.Ribbon.FileMenu));
        Assert.Contains(fixture.Changes.Notifications, entry => ReferenceEquals(entry.Owner, file) && entry.Name == nameof(file.Visible));
        Assert.Equal(DesignerSerializationVisibility.Content, TypeDescriptor.GetProperties(fixture.Ribbon)[nameof(fixture.Ribbon.FileMenu)]!.SerializationVisibility);
        Assert.Equal(RibbonFileMenuMode.CommandMenu, fixture.Ribbon.FileMenuMode);
        fixture.Settings.Visible = true; Assert.Equal(1, fixture.Host.Transactions);
        fixture.Settings.Visible = false; Assert.Equal(2, fixture.Host.Committed);
        Assert.DoesNotContain(fixture.Ribbon.Snapshot.Targets, target => target.Kind == TargetKind.FileMenu);
        Assert.False(file.IsDisposed); Assert.Same(command, file.Items[0]);
    });

    [Fact]
    public void File_destination_is_explicit_and_updates_the_designer_gesture_without_opening_runtime_backstage() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); fixture.Settings.Visible = true;
        var fileBounds = fixture.Ribbon.Snapshot.Targets.Single(target => target.Kind == TargetKind.FileMenu).VisibleBounds;
        var point = new Point(fileBounds.Left + fileBounds.Width / 2, fileBounds.Top + fileBounds.Height / 2);
        var preview = new RibbonBackstageDesignPreview(fixture.Ribbon, null, fixture.Ribbon);
        Assert.False(preview.CanOpenFromFile(point)); fixture.Settings.Opens = "Backstage";
        Assert.True(preview.CanOpenFromFile(point)); Assert.True(fixture.Settings.Visible);
        Assert.False(fixture.Ribbon.IsBackstageOpen); Assert.Equal(2, fixture.Host.Committed);
        fixture.Settings.Opens = "Backstage"; Assert.Equal(2, fixture.Host.Transactions);
        fixture.Settings.Opens = "Command menu"; Assert.False(preview.CanOpenFromFile(point)); Assert.Equal(3, fixture.Host.Committed);
        Assert.True(fixture.Settings.Visible);
    });

    [Fact]
    public void Invalid_destination_and_checkout_refusal_leave_file_unchanged() => FoundationTests.Sta(() =>
    {
        using var fixture = new Fixture(); Assert.Throws<ArgumentException>(() => fixture.Settings.Opens = "Unknown");
        Assert.Equal(0, fixture.Host.Transactions); Assert.Empty(fixture.Changes.Notifications);
        fixture.Changes.Refuse = true; Assert.Throws<CheckoutException>(() => fixture.Settings.Visible = true);
        Assert.False(fixture.Ribbon.FileMenu.Visible); Assert.Equal(1, fixture.Host.Cancelled); Assert.Equal(0, fixture.Host.Committed);
        Assert.Equal(RibbonFileMenuMode.CommandMenu, fixture.Ribbon.FileMenuMode);
    });

    private sealed class Fixture : IDisposable
    {
        internal readonly DesignerAuthoringTests.Host Host = new();
        internal readonly RibbonControl Ribbon = new() { Width = 900 };
        internal readonly Changes Changes = new();
        internal readonly RibbonFileDesignSettings Settings;
        internal Fixture()
        {
            Host.Container.Add(Ribbon, "ribbon"); Ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
            Settings = new(Ribbon, type => type == typeof(IComponentChangeService) ? Changes : Host.GetService(type));
        }
        public void Dispose() => Host.Dispose();
    }
    private sealed class Changes : IComponentChangeService
    {
        internal bool Refuse;
        internal List<(object Owner, string? Name)> Notifications = [];
        public void OnComponentChanging(object component, MemberDescriptor? member) { if (Refuse) throw CheckoutException.Canceled; }
        public void OnComponentChanged(object component, MemberDescriptor? member, object? oldValue, object? newValue) => Notifications.Add((component, member?.Name));
        public event ComponentEventHandler? ComponentAdded { add { } remove { } } public event ComponentEventHandler? ComponentAdding { add { } remove { } }
        public event ComponentChangedEventHandler? ComponentChanged { add { } remove { } } public event ComponentChangingEventHandler? ComponentChanging { add { } remove { } }
        public event ComponentEventHandler? ComponentRemoved { add { } remove { } } public event ComponentEventHandler? ComponentRemoving { add { } remove { } }
        public event ComponentRenameEventHandler? ComponentRename { add { } remove { } }
    }
}
