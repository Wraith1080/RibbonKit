using System.Windows;
using System.Windows.Markup;

namespace RibbonKit.Controls;

/// <summary>
/// One group a <see cref="RibbonMergeSource"/> injects into an existing <b>host</b> tab, the way
/// Office lets a tool context add a group to the host's Home tab rather than contributing a whole
/// tab of its own.
/// </summary>
/// <example>
/// <code language="xaml">
/// &lt;rk:RibbonMergeSource.Groups&gt;
///     &lt;rk:RibbonGroupContribution TargetTabId="tab.home" Order="5"&gt;
///         &lt;rk:RibbonGroup Header="Chart Data"&gt; ... &lt;/rk:RibbonGroup&gt;
///     &lt;/rk:RibbonGroupContribution&gt;
/// &lt;/rk:RibbonMergeSource.Groups&gt;
/// </code>
/// </example>
/// <remarks>
/// The target is named by the host tab's <c>Ribbon.CommandId</c> — the same stable identity ribbon
/// customization already persists with — so a contribution keeps working when the user reorders or
/// renames the tab. A contribution whose target tab isn't present is silently skipped: a merge
/// source shouldn't crash a host that happens not to have the tab it hoped for.
/// </remarks>
[ContentProperty(nameof(Group))]
public class RibbonGroupContribution : DependencyObject
{
    /// <summary>Identifies the <see cref="TargetTabId"/> dependency property.</summary>
    public static readonly DependencyProperty TargetTabIdProperty =
        DependencyProperty.Register(
            nameof(TargetTabId),
            typeof(string),
            typeof(RibbonGroupContribution),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Group"/> dependency property.</summary>
    public static readonly DependencyProperty GroupProperty =
        DependencyProperty.Register(
            nameof(Group),
            typeof(RibbonGroup),
            typeof(RibbonGroupContribution),
            new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <see cref="Order"/> dependency property.</summary>
    public static readonly DependencyProperty OrderProperty =
        DependencyProperty.Register(
            nameof(Order),
            typeof(int),
            typeof(RibbonGroupContribution),
            new FrameworkPropertyMetadata(0));

    /// <summary>
    /// The <c>Ribbon.CommandId</c> of the host tab this group joins. Matching is exact; a
    /// contribution with no match (or no id) is skipped.
    /// </summary>
    public string? TargetTabId
    {
        get => (string?)GetValue(TargetTabIdProperty);
        set => SetValue(TargetTabIdProperty, value);
    }

    /// <summary>The group to inject. Required — a contribution without one is skipped.</summary>
    public RibbonGroup? Group
    {
        get => (RibbonGroup?)GetValue(GroupProperty);
        set => SetValue(GroupProperty, value);
    }

    /// <summary>
    /// Where this group sits inside the target tab. The host tab's own groups behave as order 0
    /// and come first, so a contribution with <see cref="Order"/> ≥ 0 appends after them and a
    /// <b>negative</b> order lands before them. Contributions that tie are ordered by when their
    /// sources first merged, so the arrangement is stable across merge/unmerge cycles. Default 0.
    /// </summary>
    public int Order
    {
        get => (int)GetValue(OrderProperty);
        set => SetValue(OrderProperty, value);
    }
}
