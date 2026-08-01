using System.Text.Json;
using System.Windows;
using RibbonKit.Controls;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>
/// Guards the Phase 7 tab-merging and modal-tab invariants recorded in
/// docs/06-MERGE-AND-MODAL-PLAN.md §7.
/// </summary>
public class RibbonMergeModalTests
{
    [Fact]
    public void First_merge_sequence_keeps_order_stable_across_later_permutations() => Sta.Run(() =>
    {
        var ribbon = HostRibbon(out RibbonTab home, out RibbonTab insert);
        RibbonMergeSource sourceA = Source("A", order: 0);
        RibbonMergeSource sourceB = Source("B", order: 0);
        RibbonMergeSource sourceC = Source("C", order: -1);
        RibbonMergeSource[] sources = { sourceA, sourceB, sourceC };

        // Establish the tie-break sequence once. It belongs to the ribbon/source relationship,
        // not to whichever order the sources happen to return in later.
        foreach (RibbonMergeSource source in sources)
        {
            Assert.True(ribbon.Merge(source));
        }

        foreach (RibbonMergeSource source in sources)
        {
            Assert.True(ribbon.Unmerge(source));
        }

        RibbonMergeSource[][] permutations =
        {
            new[] { sourceA, sourceB, sourceC },
            new[] { sourceA, sourceC, sourceB },
            new[] { sourceB, sourceA, sourceC },
            new[] { sourceB, sourceC, sourceA },
            new[] { sourceC, sourceA, sourceB },
            new[] { sourceC, sourceB, sourceA },
        };

        foreach (RibbonMergeSource[] permutation in permutations)
        {
            foreach (RibbonMergeSource source in permutation)
            {
                Assert.True(ribbon.Merge(source));
            }

            AssertReferences(
                ribbon.Tabs,
                sourceC.Tabs[0],
                home,
                insert,
                sourceA.Tabs[0],
                sourceB.Tabs[0]);

            foreach (RibbonMergeSource source in permutation)
            {
                Assert.True(ribbon.Unmerge(source));
            }

            AssertReferences(ribbon.Tabs, home, insert);
        }
    });

    [Fact]
    public void Merge_unmerge_cycles_restore_the_exact_host_collections() => Sta.Run(() =>
    {
        var ribbon = HostRibbon(out RibbonTab home, out RibbonTab insert);
        var hostA = new RibbonGroup { Header = "Clipboard" };
        var hostB = new RibbonGroup { Header = "Font" };
        home.Groups.Add(hostA);
        home.Groups.Add(hostB);

        RibbonMergeSource source = Source("Child A", order: 0, "Child B");
        var contributed = new RibbonGroup { Header = "Document" };
        source.Groups.Add(new RibbonGroupContribution
        {
            TargetTabId = "HOME",
            Group = contributed,
        });

        for (int cycle = 0; cycle < 8; cycle++)
        {
            Assert.True(ribbon.Merge(source));
            Assert.True(source.IsMerged);
            Assert.True(Ribbon.GetIsMerged(source.Tabs[0]));
            Assert.True(Ribbon.GetIsMerged(contributed));

            Assert.True(ribbon.Unmerge(source));

            Assert.False(source.IsMerged);
            Assert.False(Ribbon.GetIsMerged(source.Tabs[0]));
            Assert.False(Ribbon.GetIsMerged(contributed));
            AssertReferences(ribbon.Tabs, home, insert);
            AssertReferences(home.Groups, hostA, hostB);
        }
    });

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Two_group_sources_restore_the_host_when_unmerged_either_way(bool removeAFirst) => Sta.Run(() =>
    {
        var ribbon = HostRibbon(out RibbonTab home, out _);
        var hostA = new RibbonGroup { Header = "Clipboard" };
        var hostB = new RibbonGroup { Header = "Font" };
        home.Groups.Add(hostA);
        home.Groups.Add(hostB);

        RibbonMergeSource sourceA = GroupSource("A", "HOME");
        RibbonMergeSource sourceB = GroupSource("B", "HOME");
        RibbonGroup groupA = sourceA.Groups[0].Group!;
        RibbonGroup groupB = sourceB.Groups[0].Group!;

        ribbon.Merge(sourceA);
        ribbon.Merge(sourceB);
        AssertReferences(home.Groups, hostA, hostB, groupA, groupB);

        RibbonMergeSource first = removeAFirst ? sourceA : sourceB;
        RibbonMergeSource second = removeAFirst ? sourceB : sourceA;
        RibbonGroup survivor = removeAFirst ? groupB : groupA;

        Assert.True(ribbon.Unmerge(first));
        AssertReferences(home.Groups, hostA, hostB, survivor);

        Assert.True(ribbon.Unmerge(second));
        AssertReferences(home.Groups, hostA, hostB);
    });

    [Fact]
    public void Modal_exit_restores_authored_visibility_and_selection() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        RibbonTab home = Tab("Home", "HOME");
        RibbonTab hidden = Tab("Developer", "DEV");
        RibbonTab preview = Tab("Print Preview", "PREVIEW", isModal: true);
        hidden.Visibility = Visibility.Collapsed;
        preview.Visibility = Visibility.Collapsed;
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(hidden);
        ribbon.Tabs.Add(preview);
        ribbon.SelectedTab = home;

        Assert.True(ribbon.EnterModal(preview));

        Assert.True(ribbon.IsModal);
        Assert.Same(preview, ribbon.ModalTab);
        Assert.Same(preview, ribbon.SelectedTab);
        Assert.Equal(Visibility.Collapsed, home.Visibility);
        Assert.Equal(Visibility.Collapsed, hidden.Visibility);
        Assert.Equal(Visibility.Visible, preview.Visibility);
        Assert.Equal(Visibility.Visible, ribbon.GetAuthoredVisibility(home));
        Assert.Equal(Visibility.Collapsed, ribbon.GetAuthoredVisibility(hidden));
        Assert.Equal(Visibility.Collapsed, ribbon.GetAuthoredVisibility(preview));

        Assert.True(ribbon.ExitModal());

        Assert.False(ribbon.IsModal);
        Assert.Null(ribbon.ModalTab);
        Assert.Same(home, ribbon.SelectedTab);
        Assert.Equal(Visibility.Visible, home.Visibility);
        Assert.Equal(Visibility.Collapsed, hidden.Visibility);
        Assert.Equal(Visibility.Collapsed, preview.Visibility);
    });

    [Fact]
    public void Modal_enter_cancellation_leaves_the_ribbon_untouched() => Sta.Run(() =>
    {
        var ribbon = HostRibbon(out RibbonTab home, out RibbonTab preview);
        preview.IsModal = true;
        ribbon.SelectedTab = home;
        ribbon.ModalEntering += (_, args) => args.Cancel = true;

        Assert.False(ribbon.EnterModal(preview));

        Assert.False(ribbon.IsModal);
        Assert.Same(home, ribbon.SelectedTab);
        Assert.Equal(Visibility.Visible, home.Visibility);
        Assert.Equal(Visibility.Visible, preview.Visibility);
    });

    [Fact]
    public void Modal_exit_cancellation_preserves_the_modal_scope() => Sta.Run(() =>
    {
        var ribbon = HostRibbon(out RibbonTab home, out RibbonTab preview);
        preview.IsModal = true;
        ribbon.SelectedTab = home;
        ribbon.EnterModal(preview);
        RibbonModalReason? reason = null;
        ribbon.ModalExiting += (_, args) =>
        {
            reason = args.Reason;
            args.Cancel = true;
        };

        Assert.False(ribbon.ExitModal());

        Assert.Equal(RibbonModalReason.Application, reason);
        Assert.True(ribbon.IsModal);
        Assert.Same(preview, ribbon.ModalTab);
        Assert.Same(preview, ribbon.SelectedTab);
        Assert.Equal(Visibility.Collapsed, home.Visibility);
    });

    [Fact]
    public void A_tab_merged_during_modal_is_hidden_then_restored() => Sta.Run(() =>
    {
        var ribbon = HostRibbon(out RibbonTab home, out RibbonTab preview);
        preview.IsModal = true;
        ribbon.SelectedTab = home;
        ribbon.EnterModal(preview);
        RibbonMergeSource source = Source("Child", order: 0);
        RibbonTab child = source.Tabs[0];

        Assert.True(ribbon.Merge(source));

        Assert.Equal(Visibility.Collapsed, child.Visibility);
        Assert.Equal(Visibility.Visible, ribbon.GetAuthoredVisibility(child));

        Assert.True(ribbon.ExitModal());

        Assert.Equal(Visibility.Visible, child.Visibility);
        Assert.Same(home, ribbon.SelectedTab);
    });

    [Fact]
    public void Unmerging_the_modal_tab_forces_exit_even_when_exit_is_cancelled() => Sta.Run(() =>
    {
        var ribbon = HostRibbon(out RibbonTab home, out _);
        RibbonMergeSource source = Source("Child Preview", order: 0);
        RibbonTab preview = source.Tabs[0];
        preview.IsModal = true;
        ribbon.Merge(source);
        ribbon.SelectedTab = home;
        ribbon.EnterModal(preview);
        RibbonModalReason? exitedReason = null;
        ribbon.ModalExiting += (_, args) => args.Cancel = true;
        ribbon.ModalExited += (_, args) => exitedReason = args.Reason;

        Assert.True(ribbon.Unmerge(source));

        Assert.False(ribbon.IsModal);
        Assert.Null(ribbon.ModalTab);
        Assert.False(source.IsMerged);
        Assert.Same(home, ribbon.SelectedTab);
        Assert.Equal(Visibility.Visible, home.Visibility);
        Assert.Equal(RibbonModalReason.TabRemoved, exitedReason);
    });

    [Fact]
    public void Serialization_while_modal_round_trips_authored_visibility() => Sta.Run(() =>
    {
        var sourceRibbon = new Ribbon();
        RibbonTab home = Tab("Home", "HOME");
        RibbonTab hidden = Tab("Developer", "DEV");
        RibbonTab preview = Tab("Print Preview", "PREVIEW", isModal: true);
        hidden.Visibility = Visibility.Collapsed;
        preview.Visibility = Visibility.Collapsed;
        sourceRibbon.Tabs.Add(home);
        sourceRibbon.Tabs.Add(hidden);
        sourceRibbon.Tabs.Add(preview);
        sourceRibbon.SelectedTab = home;
        sourceRibbon.EnterModal(preview);

        string json = RibbonCustomizationSerializer.Serialize(sourceRibbon);

        var restored = new Ribbon();
        RibbonTab restoredHome = Tab("Changed Home", "HOME");
        RibbonTab restoredHidden = Tab("Changed Developer", "DEV");
        RibbonTab restoredPreview = Tab("Changed Preview", "PREVIEW", isModal: true);
        restored.Tabs.Add(restoredHome);
        restored.Tabs.Add(restoredHidden);
        restored.Tabs.Add(restoredPreview);

        RibbonCustomizationSerializer.Apply(restored, json);

        Assert.Equal(Visibility.Visible, restoredHome.Visibility);
        Assert.Equal(Visibility.Collapsed, restoredHidden.Visibility);
        Assert.Equal(Visibility.Collapsed, restoredPreview.Visibility);
    });

    [Fact]
    public void Serialization_excludes_merged_tabs_groups_and_qat_proxies() => Sta.Run(() =>
    {
        var ribbon = new Ribbon();
        RibbonTab home = Tab("Home", "HOME");
        var hostGroup = new RibbonGroup { Header = "Clipboard" };
        Ribbon.SetCommandId(hostGroup, "HOST_GROUP");
        home.Groups.Add(hostGroup);
        ribbon.Tabs.Add(home);

        RibbonMergeSource source = Source("Child", order: 0);
        RibbonTab childTab = source.Tabs[0];
        Ribbon.SetCommandId(childTab, "CHILD_TAB");
        var childGroup = new RibbonGroup { Header = "Child Commands" };
        Ribbon.SetCommandId(childGroup, "CHILD_GROUP");
        var childCommand = new RibbonButton { Header = "Child Command" };
        Ribbon.SetCommandId(childCommand, "CHILD_COMMAND");
        childGroup.Items.Add(childCommand);
        childTab.Groups.Add(childGroup);

        var contributed = new RibbonGroup { Header = "Contributed" };
        Ribbon.SetCommandId(contributed, "CONTRIBUTED_GROUP");
        source.Groups.Add(new RibbonGroupContribution
        {
            TargetTabId = "HOME",
            Group = contributed,
        });

        ribbon.Merge(source);
        Assert.True(ribbon.AddToQuickAccess(childCommand));

        string json = RibbonCustomizationSerializer.Serialize(ribbon);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        JsonElement[] tabs = root.GetProperty("Tabs").EnumerateArray().ToArray();
        JsonElement[] groups = tabs[0].GetProperty("Groups").EnumerateArray().ToArray();

        Assert.Single(tabs);
        Assert.Equal("HOME", tabs[0].GetProperty("Id").GetString());
        Assert.Single(groups);
        Assert.Equal("HOST_GROUP", groups[0].GetProperty("Id").GetString());
        Assert.Equal(0, root.GetProperty("QuickAccess").GetArrayLength());
        Assert.DoesNotContain("CHILD_", json, StringComparison.Ordinal);
        Assert.DoesNotContain("CONTRIBUTED_GROUP", json, StringComparison.Ordinal);
    });

    [Fact]
    public void Applying_customization_temporarily_unmerges_then_restores_sources() => Sta.Run(() =>
    {
        var ribbon = HostRibbon(out RibbonTab home, out RibbonTab insert);
        RibbonMergeSource source = Source("Child", order: 0);
        RibbonTab child = source.Tabs[0];
        ribbon.Merge(source);
        string baseline = RibbonCustomizationSerializer.Serialize(ribbon);

        home.Header = "Changed";
        ribbon.Tabs.Move(1, 0);
        RibbonCustomizationSerializer.Apply(ribbon, baseline);

        Assert.True(source.IsMerged);
        Assert.True(ribbon.IsMerged(source));
        Assert.Equal("Home", home.Header);
        AssertReferences(ribbon.Tabs, home, insert, child);

        Assert.True(ribbon.Unmerge(source));
        AssertReferences(ribbon.Tabs, home, insert);
    });

    [Fact]
    public void Declarative_activation_retargets_and_unmerges_a_source() => Sta.Run(() =>
    {
        var ribbonA = new Ribbon();
        var ribbonB = new Ribbon();
        RibbonMergeSource source = Source("Child", order: 0);
        RibbonTab child = source.Tabs[0];
        source.Target = ribbonA;

        source.IsActive = true;
        Assert.True(ribbonA.IsMerged(source));
        Assert.Contains(child, ribbonA.Tabs);

        source.Target = ribbonB;
        Assert.False(ribbonA.IsMerged(source));
        Assert.DoesNotContain(child, ribbonA.Tabs);
        Assert.True(ribbonB.IsMerged(source));
        Assert.Contains(child, ribbonB.Tabs);

        source.IsActive = false;
        Assert.False(source.IsMerged);
        Assert.DoesNotContain(child, ribbonB.Tabs);
    });

    private static Ribbon HostRibbon(out RibbonTab home, out RibbonTab insert)
    {
        var ribbon = new Ribbon();
        home = Tab("Home", "HOME");
        insert = Tab("Insert", "INSERT");
        ribbon.Tabs.Add(home);
        ribbon.Tabs.Add(insert);
        return ribbon;
    }

    private static RibbonTab Tab(string header, string id, bool isModal = false)
    {
        var tab = new RibbonTab
        {
            Header = header,
            IsModal = isModal,
        };
        Ribbon.SetCommandId(tab, id);
        return tab;
    }

    private static RibbonMergeSource Source(string firstHeader, int order, string? secondHeader = null)
    {
        var source = new RibbonMergeSource { Order = order };
        source.Tabs.Add(new RibbonTab { Header = firstHeader });
        if (secondHeader is not null)
        {
            source.Tabs.Add(new RibbonTab { Header = secondHeader });
        }

        return source;
    }

    private static RibbonMergeSource GroupSource(string header, string targetTabId)
    {
        var source = new RibbonMergeSource();
        source.Groups.Add(new RibbonGroupContribution
        {
            TargetTabId = targetTabId,
            Group = new RibbonGroup { Header = header },
        });
        return source;
    }

    private static void AssertReferences<T>(IEnumerable<T> actual, params T[] expected)
        where T : class
    {
        T[] values = actual.ToArray();
        Assert.Equal(expected.Length, values.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Same(expected[i], values[i]);
        }
    }
}
