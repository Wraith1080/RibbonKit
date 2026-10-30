using System.Text.Json;

namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    // Isolated, real-process acceptance path. Program only enables storage for
    // this diagnostic when an explicit settings directory is supplied.
    internal async Task RunPersistenceProbe(string phase, string resultPath)
    {
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        if (phase == "write")
        {
            using (var edit = ribbon.CommandCatalog.BeginCustomization(CustomizationApplicationId))
            {
                edit.Reset(); // Materialize the authored structure before staging its first override.
                edit.Draft.Ribbon!.Tabs.Single(t => t.Id == home.Id).Label = "Home saved";
                edit.Draft.Ribbon.Tabs.Add(new() { Id = "user.tab:restart", Label = "My tools", Groups = [new()
                {
                    Id = "user.group:restart", Label = "Saved group", IconCommandId = copy.Id,
                    Commands = [new() { Id = "user.command:restart", CommandId = copy.Id, Label = "Copy saved", Size = RibbonItemSize.Small }]
                }] });
                edit.Draft.Simplified = new() { Tabs = [new() { TabId = home.Id, Commands = [new() { CommandId = copy.Id, Size = RibbonItemSize.Small }] }] };
                Require(edit.Apply(), "Structural/Simplified Apply failed.");
            }
            Exception? dialogFailure = null;
            EventHandler<RibbonOptionsDialogEventArgs> opening = (_, e) => e.Dialog.Shown += (_, _) => e.Dialog.BeginInvoke(() =>
            {
                try
                {
                    var dialog = (RibbonQuickAccessCustomizationForm)e.Dialog;
                    var apply = (Button)dialog.Controls.Find("ApplyCustomization", true).Single();
                    dialog.Editor.Session.Draft.QuickAccess.Add(new() { CommandId = "demo.qat.redo" });
                    dialog.Editor.Session.Draft.QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon;
                    apply.PerformClick();
                    Require(File.ReadAllText(_customizationFile!.Path).Contains("demo.qat.redo", StringComparison.Ordinal), "Apply did not immediately save the QAT.");
                    string checkpoint = File.ReadAllText(_customizationFile.Path);
                    dialog.Editor.Session.Draft.QuickAccess.Clear();
                    dialog.SelectedPage = dialog.Pages.Single(p => p.Name == "ApplicationEditorOptions");
                    var wrap = dialog.SelectedPage.Controls.OfType<CheckBox>().Single();
                    wrap.Checked = false; apply.PerformClick(); wrap.Checked = true;
                    ((Button)dialog.CancelButton!).PerformClick();
                    Require(File.ReadAllText(_customizationFile.Path) == checkpoint, "Cancel overwrote the last applied profile.");
                }
                catch (Exception error) { dialogFailure = error; e.Dialog.Close(); }
            });
            ribbon.OptionsDialogOpening += opening;
            try { ribbon.ShowQuickAccessCustomization(this); }
            finally { ribbon.OptionsDialogOpening -= opening; }
            if (dialogFailure is not null) throw new InvalidOperationException("Options Apply/Cancel failed.", dialogFailure);
            Require(!document.WordWrap, "Editor Cancel discarded an earlier Apply.");
            ribbon.LayoutMode = RibbonLayoutMode.Simplified; ribbon.Density = RibbonDensity.Touch; ribbon.IsMinimized = true;
            var animation = ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Single(g => g.Id == "group.ribbonAnimation");
            animation.Items.Single(i => i.Id == "demo.animation.Expressive").PerformClick();
            ((RibbonDropDownItem)animation.Items.Single(i => i.Id == "demo.animation.tabContent")).Items.Single(i => i.Id.EndsWith(".SlideDown", StringComparison.Ordinal)).PerformClick();
            Bounds = new(70, 80, 1100, 740); WindowState = FormWindowState.Maximized;
            // Direct placement/visibility changes also persist outside a dialog.
            ribbon.QuickAccessPosition = RibbonQuickAccessPosition.TabRow; ribbon.QuickAccessVisible = false;
            await Task.Yield();
            SavePreferences();
            Require(_persistenceWarning is null, _persistenceWarning ?? "Persistence failed.");
        }
        else if (phase == "read")
        {
            Require(ribbon.LayoutMode == RibbonLayoutMode.Simplified && ribbon.Density == RibbonDensity.Touch && ribbon.IsMinimized, "Ribbon appearance did not survive restart.");
            Require(ribbon.AnimationLevel == RibbonAnimationLevel.Expressive && ribbon.TabContentAnimation == RibbonTabContentAnimation.SlideDown, "Animation choices did not survive restart.");
            Require(!document.WordWrap && WindowState == FormWindowState.Maximized, "Editor/window preferences did not survive restart.");
            Require(ribbon.QuickAccessPosition == RibbonQuickAccessPosition.TabRow && !ribbon.QuickAccessVisible, "Direct QAT preferences did not survive restart.");
            using var live = ribbon.CommandCatalog.BeginCustomization(CustomizationApplicationId);
            Require(live.Draft.QuickAccess.Any(q => q.CommandId == "demo.qat.redo") && live.Draft.QuickAccess.Count == 4, "QAT Apply/Cancel checkpoint was lost.");
            Require(live.Draft.Ribbon!.Tabs.Single(t => t.Id == home.Id).Label == "Home saved", "Authored tab rename was lost.");
            var group = live.Draft.Ribbon.Tabs.Single(t => t.Id == "user.tab:restart").Groups.Single();
            Require(group.Commands.Single().Id == "user.command:restart" && group.IconCommandId == copy.Id, "Custom group identities or icon were lost.");
            Require(live.Draft.Simplified!.Tabs.Single(t => t.TabId == home.Id).Commands.Single().Size == RibbonItemSize.Small, "Simplified selection was lost.");
            Require(_normalWindowBounds.Width == _startupPlacement!.Fit(Screen.FromRectangle(_startupPlacement.Bounds).WorkingArea, MinimumSize, DeviceDpi).Width, "Normal window placement was lost.");
        }
        else if (phase == "defaults")
        {
            Require(ribbon.LayoutMode == RibbonLayoutMode.Classic && ribbon.Density == RibbonDensity.Compact && !ribbon.IsMinimized, "Safe ribbon defaults were not used.");
            Require(ribbon.AnimationLevel == RibbonAnimationLevel.Subtle && ribbon.TabContentAnimation == RibbonTabContentAnimation.Fade && document.WordWrap, "Safe application defaults were not used.");
            Require(ribbon.QuickAccessItems.Count == 3 && ribbon.QuickAccessPosition == RibbonQuickAccessPosition.TitleBar && ribbon.QuickAccessVisible, "Safe customization defaults were not used.");
        }
        else throw new ArgumentException("Unknown persistence probe phase: " + phase);
        File.WriteAllText(resultPath, JsonSerializer.Serialize(new { Result = "PASS", Phase = phase, Preferences = CapturePreferences(), Warning = _persistenceWarning }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
