namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private void InitializeMerging()
    {
        var source = new RibbonMergeSource { Text = "Chart document", Order = 10 };
        components!.Add(source);
        var chart = new RibbonTab { Id = "merge.chart.tab", Text = "Chart Design", KeyTip = "CD", IsContextual = true, ContextualColor = Color.FromArgb(164, 57, 125) };
        var chartData = new RibbonGroup { Text = "Chart data" };
        var series = new RibbonTextBoxItem { Id = "merge.chart.series", Text = "Series", Value = "Revenue" };
        var chartType = new RibbonComboBoxItem { Text = "Type" }; chartType.Items.AddRange("Column", "Line", "Area"); chartType.SelectedIndex = 0;
        var update = new RibbonButtonItem { Text = "Update chart", KeyTip = "U", Icon = new SampleIcon(7) };
        update.Click += (_, _) => status.Text = $"Chart requested: {series.Value} ({chartType.Value})";
        chartData.Items.AddRange(series, chartType, update); chart.Groups.Add(chartData); source.Tabs.Add(chart);
        var contribution = new RibbonGroupContribution { TargetTabId = home.Id, Order = 10 };
        contribution.Group.Text = "Chart context"; var chartAction = new RibbonButtonItem { Text = "Chart data", Icon = new SampleIcon(1), KeyTip = "CM" };
        chartAction.Click += (_, _) => ribbon.SelectedTab = chart;
        contribution.Group.Items.Add(chartAction); source.Groups.Add(contribution);
        var merging = new RibbonTab { Id = "tab.merging", Text = "Merging", KeyTip = "M" };
        var actions = new RibbonGroup { Text = "Application context" };
        var active = new RibbonToggleItem { Text = "Chart context", KeyTip = "C", Icon = new SampleIcon(1), ScreenTipDescription = "The application activates a merge source. Keyboard focus does not drive activation." };
        active.CheckedChanged += (_, _) =>
        {
            if (active.Checked) ribbon.Merge(source); else ribbon.Unmerge(source);
            status.Text = source.IsMerged ? "Chart Design and Home's Chart context group are available." : "Chart context removed; its Quick Access commands are parked.";
        };
        var show = new RibbonButtonItem { Text = "Select Chart Design", KeyTip = "S", Icon = new SampleIcon(0) };
        show.Click += (_, _) => { active.Checked = true; ribbon.SelectedTab = chart; };
        var remove = new RibbonButtonItem { Text = "Remove chart context", KeyTip = "R", Icon = new SampleIcon(2) }; remove.Click += (_, _) => active.Checked = false;
        actions.Items.AddRange(active, show, remove); merging.Groups.Add(actions); ribbon.Tabs.Add(merging);
        ribbon.QuickAccessItems.AddRange(new RibbonQuickAccessReference { Source = update }, new RibbonQuickAccessComboBoxReference { Source = chartType });
    }
}
