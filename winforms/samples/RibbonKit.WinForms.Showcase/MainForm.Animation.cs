namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    private Action? _syncAnimationChoices;
    private void InitializeAnimation()
    {
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        var group = new RibbonGroup { Id = "group.ribbonAnimation", Text = "Ribbon animation" };
        var levels = new Dictionary<RibbonAnimationLevel, RibbonRadioButtonItem>();
        var styles = new Dictionary<RibbonTabContentAnimation, RibbonRadioButtonItem>();
        void Sync()
        {
            foreach (var (level, item) in levels) item.Checked = ribbon.AnimationLevel == level;
            foreach (var (style, item) in styles) item.Checked = ribbon.TabContentAnimation == style;
        }
        foreach (var level in Enum.GetValues<RibbonAnimationLevel>())
        {
            var item = new RibbonRadioButtonItem { Id = "demo.animation." + level, Text = level.ToString(), KeyTip = level.ToString()[..1], GroupName = "showcase.animation.level", SizePolicy = RibbonSizePolicy.Fixed(RibbonItemSize.Medium), SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            item.Click += (_, _) => { ribbon.AnimationLevel = level; Sync(); SavePreferences(); }; levels.Add(level, item); group.Items.Add(item);
        }
        var content = new RibbonDropDownItem { Id = "demo.animation.tabContent", Text = "Tab content", KeyTip = "T", DropDownHeader = "Tab content animation", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline, ScreenTipDescription = "Choose Fade or Slide down for tab content and newly shown or merged headers. Groups share this choice: Slide moves left on collapse and right on restore." };
        foreach (var style in Enum.GetValues<RibbonTabContentAnimation>())
        {
            var item = new RibbonRadioButtonItem { Id = "demo.animation.tabContent." + style, Text = style == RibbonTabContentAnimation.Fade ? "Fade" : "Slide down", GroupName = "showcase.animation.tabContent" };
            item.Click += (_, _) => { ribbon.TabContentAnimation = style; Sync(); SavePreferences(); }; styles.Add(style, item); content.Items.Add(item);
        }
        group.Items.Add(content); var view = ribbon.Tabs.Single(t => t.Id == "tab.merging"); view.Groups.Add(group); _syncAnimationChoices = Sync; Sync();
        ribbon.SelectedTabChanged += (_, _) => { if (ReferenceEquals(ribbon.SelectedTab, view)) Sync(); };
        document.AppendText("\r\n\r\nView > Ribbon animation selects None, Subtle or Expressive for ribbon motion, including minimizing/restoring with the chevron, Ctrl+F1 or a tab double-click, and opening command menus or galleries. Popup frames stay anchored while their contents fade and slide into place; input settles the effect before normal interaction. Backstage navigation slides in while live pages fade on opening and selection. Back or Escape fades Backstage out after validation. Tab content chooses Fade or Slide down for content and newly shown or merged headers. None and the system animation preference always keep transitions immediate. View > Messages collects Add, Reopen and Clear in one menu; those actions and Close animate the message rows and their joins to the ribbon/QAT.");
    }
}
