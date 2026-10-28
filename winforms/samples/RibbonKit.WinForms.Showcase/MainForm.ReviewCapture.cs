namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    internal async Task CaptureReview(string directory)
    {
        Directory.CreateDirectory(directory); Width = 1280; Height = 780; ribbon.LayoutMode = RibbonLayoutMode.Classic; ribbon.SelectedTab = home;
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; await Task.Delay(80); Save(this, density + "-showcase");
            using var options = new RibbonQuickAccessCustomizationForm(ribbon, ribbon.CustomizationApplicationId); options.SelectedPage = options.Pages.Single(p => p.Text == "Customize Ribbon"); options.Show(this); await Task.Delay(80);
            using var timer = new System.Windows.Forms.Timer { Interval = 80 }; Exception? failure = null;
            timer.Tick += (_, _) =>
            {
                if (Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Name == "RibbonGroupDialog") is not { } group) return; timer.Stop();
                try
                {
                    ((TextBox)group.Controls.Find("RibbonGroupName", true).Single()).Text = "My commands";
                    var icons = (ListView)group.Controls.Find("RibbonGroupIcons", true).Single(); if (icons.Items[copy.Id] is { } item) item.Selected = true;
                    group.Refresh(); Save(group, density + "-group-palette");
                }
                catch (Exception error) { failure = error; }
                finally { group.DialogResult = DialogResult.Cancel; group.Close(); }
            };
            timer.Start(); ((Button)options.RibbonEditor.Controls.Find("NewRibbonGroup", true).Single()).PerformClick(); timer.Stop(); options.Close(); if (failure is not null) throw failure;
        }
        void Save(Control control, string name) { control.Refresh(); using var bitmap = new Bitmap(control.Width, control.Height); control.DrawToBitmap(bitmap, new(Point.Empty, control.Size)); bitmap.Save(Path.Combine(directory, name + ".png")); }
    }
}
