using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using RibbonKit.WinForms;

namespace IndependentConsumer;

internal static class Office2010OptionsChecks
{
    internal static int Run(string directory, bool current)
    {
        Directory.CreateDirectory(directory);
        try
        {
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles();
            using var form = new RibbonForm(); using var ribbon = new RibbonControl(); form.Controls.Add(ribbon); form.Ribbon = ribbon;
            var tab = new RibbonTab { Id = "home", Text = "Home" }; var group = new RibbonGroup { Id = "commands", Text = "Commands" }; tab.Groups.Add(group); ribbon.Tabs.Add(tab);
            for (int i = 0; i < 35; i++) group.Items.Add(new RibbonButtonItem { Id = "command" + i, Text = "Command " + i, Icon = RibbonStockIcons.Create("save") });
            ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = (RibbonButtonItem)group.Items[0] }); ribbon.CommandCatalog.CaptureDefaults();
            VerifySample(form, ribbon, directory, "independent", current); return 0;
        }
        catch (Exception error) { File.WriteAllText(Path.Combine(directory, "failure.log"), error.ToString()); return 1; }
    }
    internal static void VerifySample(RibbonForm host, RibbonControl ribbon, string directory, string name, bool current = true)
    {
        Directory.CreateDirectory(directory); ribbon.AnimationLevel = RibbonAnimationLevel.None;
        var tile = ribbon.Tabs.SelectMany(t => t.Groups).SelectMany(g => g.Items).OfType<RibbonGalleryItem>().SelectMany(g => g.Items).OfType<RibbonGalleryTile>().FirstOrDefault(t => t.Text == "Office 2010 Blue");
        if (tile is null) ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2010Blue; else tile.PerformClick();
        host.StartPosition = FormStartPosition.Manual; host.Bounds = new(-16000,-16000,1100,760); host.ShowInTaskbar = false; host.Show(); Application.DoEvents();
        var sources = ribbon.Tabs.ToArray(); var qat = ribbon.QuickAccessItems.ToArray(); var result = new Dictionary<string, object>();
        var legacy = new Dictionary<string,string>();
        foreach (var (family, theme) in new[] { ("2013",RibbonThemes.Office2013Light), ("2019",RibbonThemes.Office2019Light), ("2024",RibbonThemes.Office2024Light) })
        {
            ribbon.Theme = theme; using var options = Open();
            using var memory = new MemoryStream(); using var shot = Paint(Find<Button>(options,"CancelCustomization")); shot.Save(memory,System.Drawing.Imaging.ImageFormat.Png); legacy[family] = Convert.ToHexString(SHA256.HashData(memory.ToArray()));
            Find<Button>(options,"CancelCustomization").PerformClick();
        }
        foreach (var (accentName, accent) in new[] { ("Blue",Color.FromArgb(30,90,156)), ("Red",Color.FromArgb(180,40,64)) })
        {
            ribbon.Theme = RibbonThemes.Office2010Blue with { Accent = accent }; using var options = Open();
            var navigation = Find<ListBox>(options,"OptionsNavigation"); var navHandle = navigation.Handle;
            options.SelectedPage = options.Pages.Single(p => p.Name == "CustomizeRibbonOptionsPage"); Application.DoEvents();
            var available = Find<ListBox>(options,"RibbonAvailableCommands"); var tree = Find<TreeView>(options,"RibbonHierarchy");
            Require(available.Items.Count > 1 && tree.Nodes.Count > 0,"Real command list/tree"); available.SelectedIndex = 0;
            var listHandle = available.Handle; var listParent = available.Parent; var selected = available.SelectedItem; var draft = options.RibbonEditor.Session.Draft;
            int index = Math.Min(1,available.Items.Count-1); var row = available.GetItemRectangle(index); Move(available,new(available.ClientSize.Width-12,row.Top+row.Height/2));
            using var list = Paint(available); list.Save(Path.Combine(directory,accentName+"-list-hover.png"));
            int selectedBorder = CountRow(list,available.GetItemRectangle(0),accent), hoverBorder = CountRow(list,row,Color.FromArgb(233,194,94));
            var selectedBounds=available.GetItemRectangle(0); int selectedOutline=Outline(list,new(selectedBounds.Left+selectedBounds.Width*3/4,selectedBounds.Top),accent);
            Require(ReferenceEquals(selected,available.SelectedItem) && available.SelectedIndex == 0,"Hover never changes selection");
            var hoverNode=tree.Nodes[0].Nodes.Count>0 ? tree.Nodes[0].Nodes[0] : tree.Nodes[0]; hoverNode.EnsureVisible();
            Move(tree,new(tree.ClientSize.Width-12,hoverNode.Bounds.Top+tree.ItemHeight/2));
            using var treeShot=Paint(tree); treeShot.Save(Path.Combine(directory,accentName+"-tree-hover.png"));
            int treeHover=CountRow(treeShot,new(0,hoverNode.Bounds.Top,tree.ClientSize.Width,tree.ItemHeight),Color.FromArgb(233,194,94));
            Capture(options,accentName+"-classic");
            var cancel = Find<Button>(options,"CancelCustomization"); var ok = Find<Button>(options,"AcceptCustomization");
            var buttonSamples = new Dictionary<string,int[]>(); var outlineWidths=new Dictionary<string,int> { ["selected-list"]=selectedOutline };
            foreach (var (role,button) in new[] { ("ordinary",cancel), ("primary",ok) })
            foreach (int state in new[] { 0,1,2 })
            {
                SendMessage(button.Handle,0x2A3,0,0); // WM_MOUSELEAVE
                if (state > 0) Move(button,new(button.Width/2,button.Height/2));
                if (state == 2) SendMessage(button.Handle,0x201,1,PointValue(new(button.Width/2,button.Height/2)));
                using var image = Paint(button); image.Save(Path.Combine(directory,$"{accentName}-{role}-{state}.png"));
                int x = image.Width-12; buttonSamples[role+state] = Enumerable.Range(6,image.Height-12).Select(y=>image.GetPixel(x,y).ToArgb()).ToArray();
                var ink=role=="primary" ? accent : state==0 ? ribbon.Theme.PopupBorder : state==1 ? Color.FromArgb(233,194,94) : Color.FromArgb(203,154,52);
                outlineWidths[role+state]=Outline(image,new(image.Width*3/4,0),ink);
                if (state == 2) SendMessage(button.Handle,0x202,0,PointValue(new(-4,-4))); // release outside: no action
                button.Capture = false; SendMessage(button.Handle,0x2A3,0,0);
            }
            if (current)
            {
                Require(selectedBorder > 0 && hoverBorder > 0 && treeHover>0,"Thin accent selected/gold hovered list/tree outlines");
                Require(outlineWidths.Values.All(width=>width==1),"Actual native list/button outlines are one device pixel");
                Require(buttonSamples["ordinary0"].Distinct().Count()>1,"Ordinary resting gradient");
                double Light(string key) => buttonSamples[key].Select(Color.FromArgb).Average(c=>c.GetBrightness());
                Require(Light("primary0")>Light("primary1") && Light("primary1")>Light("primary2"),"Primary darkens on hover and press");
            }
            available.TopIndex = Math.Min(4,available.Items.Count-1); Application.DoEvents();
            Require(available.Handle==listHandle && ReferenceEquals(available.Parent,listParent) && ReferenceEquals(selected,available.SelectedItem),"Native scrolling retains handles/parent/selection");
            ribbon.LayoutMode = RibbonLayoutMode.Simplified; options.SelectedPage = options.Pages.Single(p=>p.Name=="CustomizeSimplifiedOptionsPage"); Application.DoEvents(); Capture(options,accentName+"-simplified");
            Require(ReferenceEquals(draft,options.RibbonEditor.Session.Draft) && navigation.Handle==navHandle,"Page/mode changes retain draft and navigation HWND");
            options.SelectedPage=options.Pages.Single(p=>p.Name=="QuickAccessOptionsPage"); Application.DoEvents(); Capture(options,accentName+"-qat");
            if(options.Editor.Session.Draft.QuickAccess.Count>0) options.Editor.Session.Draft.QuickAccess.RemoveAt(0);
            options.Editor.RefreshDraft(); Require(qat.SequenceEqual(ribbon.QuickAccessItems),"Pending QAT draft stays isolated");
            var page = new RibbonOptionsPage { Name="VerificationDraft",Text="Native validation" }; var text = new TextBox { Text="Checkpoint",Bounds=new(12,12,220,28) }; page.Controls.Add(text); options.Pages.Add(page); options.SelectedPage=page; Application.DoEvents();
            bool reject=true; int applied=0, cancelled=0; string checkpoint=text.Text; string? restored=null; page.Applying+=(_,e)=>e.Cancel=reject; page.Applied+=(_,_)=>{ checkpoint=text.Text; applied++; }; page.Cancelled+=(_,_)=>{ text.Text=checkpoint; restored=text.Text; cancelled++; };
            var textHandle=text.Handle; var textParent=text.Parent; text.Select(2,4); text.Text="Pending"; Require(!page.ApplyChanges() && applied==0,"Rejected validation retains draft");
            reject=false; Find<Button>(options,"ApplyCustomization").PerformClick();
            options.SelectedPage=options.Pages.First(p=>!ReferenceEquals(p,page)); options.SelectedPage=page; Application.DoEvents();
            Require(text.Handle==textHandle && ReferenceEquals(text.Parent,textParent) && text.Text==checkpoint,"Native page ownership/checkpoint across navigation");
            text.Text="Unapplied"; Find<Button>(options,"CancelCustomization").PerformClick(); Require(applied==1 && cancelled==1 && restored==checkpoint,$"Apply checkpoint/Cancel: applied={applied}, cancelled={cancelled}, restored={restored}, checkpoint={checkpoint}"); ribbon.LayoutMode=RibbonLayoutMode.Classic;
            result[accentName]=new { SelectedBorderPixels=selectedBorder,HoverBorderPixels=hoverBorder,TreeHoverBorderPixels=treeHover,OutlineWidths=outlineWidths,ButtonSamples=buttonSamples };
        }
        ribbon.Theme=RibbonThemes.Office2010Blue;
        using (var secondary=Open())
        {
            secondary.SelectedPage=secondary.Pages.Single(p=>p.Name=="CustomizeRibbonOptionsPage"); Application.DoEvents();
            bool captured=false, capturedRename=false; Exception? error=null; using var timer=new System.Windows.Forms.Timer { Interval=25 };
            timer.Tick+=(_,_)=>
            {
                if (Application.OpenForms.Cast<Form>().FirstOrDefault(f=>f.Name=="RibbonGroupDialog" || f.Text=="Edit ribbon item") is not { } dialog) return;
                timer.Stop();
                try
                {
                    bool groupDialog=dialog.Name=="RibbonGroupDialog";
                    if(groupDialog) { var icons=Find<ListView>(dialog,"RibbonGroupIcons"); Require(icons.Items.Count>0,"Native icon picker"); icons.Items[0].Selected=true; }
                    var field=Find<TextBox>(dialog,groupDialog ? "RibbonGroupName" : "RibbonItemName");
                    if(current) Require(field.Parent?.GetType().Name=="RibbonOptionsFrame","Themed secondary name frame");
                    Capture(dialog,groupDialog ? "group-dialog" : "rename-dialog"); field.Text=" "; ((Button)dialog.AcceptButton!).PerformClick(); Require(dialog.Visible,"Blank name validation"); captured=true; capturedRename=!groupDialog;
                }
                catch(Exception failure) { error=failure; }
                finally { if (dialog.CancelButton is Button button) button.PerformClick(); else dialog.Close(); }
            };
            Find<Button>(secondary,"NewRibbonTab").PerformClick(); timer.Start(); Find<Button>(secondary,"NewRibbonGroup").PerformClick(); timer.Stop(); if(error is not null) throw error; Require(captured,"Group secondary dialog reached");
            var hierarchy=Find<TreeView>(secondary,"RibbonHierarchy"); hierarchy.SelectedNode=hierarchy.Nodes.Cast<TreeNode>().Single(n=>n.Text.StartsWith("New Tab",StringComparison.OrdinalIgnoreCase));
            captured=false; timer.Start(); Find<Button>(secondary,"EditRibbonItem").PerformClick(); timer.Stop(); if(error is not null) throw error; Require(captured && capturedRename,"Rename secondary dialog reached");
            secondary.RibbonEditor.Session.Reset(); secondary.RibbonEditor.RefreshDraft(); Require(sources.SequenceEqual(ribbon.Tabs),"Reset only changes the draft until Apply"); Find<Button>(secondary,"CancelCustomization").PerformClick();
        }
        using(var acceptance=new RibbonOptionsDialog(ribbon) { StartPosition=FormStartPosition.Manual,Location=new(-16000,-16000),ShowInTaskbar=false })
        {
            var page=new RibbonOptionsPage { Text="OK validation" }; bool reject=true; int applied=0; page.Applying+=(_,e)=>e.Cancel=reject; page.Applied+=(_,_)=>applied++; acceptance.Pages.Add(page); acceptance.Show(host); acceptance.Location=new(-16000,-16000);
            Find<Button>(acceptance,"AcceptCustomization").PerformClick(); Require(acceptance.Visible && applied==0,"OK retains rejected draft"); reject=false; Find<Button>(acceptance,"AcceptCustomization").PerformClick(); Require(acceptance.DialogResult==DialogResult.OK && applied==1,"OK commits once and closes");
        }
#if RKWF_F6
        if(current)
        {
            ribbon.Theme=RibbonThemes.Office2010Blue with { OptionsAppearance=RibbonThemes.Office2010Blue.OptionsAppearance! with { ButtonBackground=new(Color.Lime),ButtonBorderColor=Color.Magenta,AccentButtonBackground=new(Color.Purple),AccentButtonHoverDarkening=0,RowHoverBackground=null } };
            using var options=Open(); using var sample=Paint(Find<Button>(options,"CancelCustomization")); Require(sample.GetPixel(sample.Width-12,sample.Height/2).ToArgb()==Color.Lime.ToArgb(),"Public ordinary fill override"); Capture(options,"explicit-overrides"); Find<Button>(options,"CancelCustomization").PerformClick();
        }
#endif
        Require(sources.SequenceEqual(ribbon.Tabs) && qat.SequenceEqual(ribbon.QuickAccessItems),"Cancel retains live ribbon and QAT sources");
        File.WriteAllText(Path.Combine(directory,"measurements.json"),JsonSerializer.Serialize(new { Name=name,Dpi=ribbon.DeviceDpi,LegacyHashes=legacy,States=result },new JsonSerializerOptions { WriteIndented=true }));
        File.WriteAllText(Path.Combine(directory,"result.log"),$"PASS: {name}; actual {ribbon.DeviceDpi} DPI; Blue/Red Options, Classic/Simplified/QAT, native hover/scroll/selection/handles, independent drafts, validation/Apply/Cancel, secondary group/icon picker and state captures. LIMIT: native-print evidence; actual VS, physical input/IME, mixed monitors and final visual review remain separate. Scrollbar skin stays Windows-owned.");
        RibbonQuickAccessCustomizationForm Open() { var options=new RibbonQuickAccessCustomizationForm(ribbon,"RibbonKit.Office2010OptionsReview") { StartPosition=FormStartPosition.Manual,Location=new(-16000,-16000),ShowInTaskbar=false }; options.Show(host); options.Location=new(-16000,-16000); Application.DoEvents(); return options; }
        void Capture(Control control,string file) { using var image=Paint(control); image.Save(Path.Combine(directory,file+".png")); }
    }
    private static T Find<T>(Control root,string name) where T:Control => root.Controls.Find(name,true).OfType<T>().Single();
    private static Bitmap Paint(Control control) { var image=new Bitmap(control.Width,control.Height); control.DrawToBitmap(image,new(Point.Empty,image.Size)); return image; }
    // Capture the synchronous native-message state before pumping real cursor
    // leave notifications for these deliberately offscreen controls.
    private static void Move(Control control,Point point) { SendMessage(control.Handle,0x200,0,PointValue(point)); control.Invalidate(); control.Update(); }
    private static nint PointValue(Point p) => (nint)((p.Y<<16)|(p.X&65535));
    private static int CountRow(Bitmap image,Rectangle row,Color ink) { row.Intersect(new(Point.Empty,image.Size)); int count=0; for(int y=row.Top;y<row.Bottom;y++) for(int x=row.Left;x<row.Right;x++) if(image.GetPixel(x,y).ToArgb()==ink.ToArgb()) count++; return count; }
    private static int Outline(Bitmap image,Point start,Color ink) { int count=0; for(int y=start.Y;y<image.Height && image.GetPixel(start.X,y).ToArgb()==ink.ToArgb();y++) count++; return count; }
    private static void Require(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
    [DllImport("user32.dll")] private static extern nint SendMessage(nint window,int message,nint wParam,nint lParam);
}
