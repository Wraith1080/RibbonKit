namespace RibbonKitDesignerConsumer;
public sealed class GeneratedContentForm : System.Windows.Forms.Form
{
 private readonly RibbonKit.WinForms.RibbonControl ribbon;
 public GeneratedContentForm()
 {
 ribbon = new RibbonKit.WinForms.RibbonControl();
 ribbon.BeginInit();
RibbonKit.WinForms.RibbonBackstagePageItem page;
page = new RibbonKit.WinForms.RibbonBackstagePageItem();
// 
// page
// 
page.ContentType = typeof(RibbonKitDesignerConsumer.BackstageInfoControl);
page.Id = "rk:c754ba2340b2481bb848c3a1bef26021";
page.Text = "Info";
RibbonKit.WinForms.RibbonMenuSeparatorItem separator;
separator = new RibbonKit.WinForms.RibbonMenuSeparatorItem();
// 
// separator
// 
separator.Id = "rk:85b3366530a1433db4fa8b579f138f69";
RibbonKit.WinForms.RibbonMenuSeparatorItem footerSeparator;
footerSeparator = new RibbonKit.WinForms.RibbonMenuSeparatorItem();
// 
// footerSeparator
// 
footerSeparator.Id = "rk:b52b03f93d044849945fc6304f278c0e";
RibbonKit.WinForms.RibbonBackstagePageItem footerPage;
footerPage = new RibbonKit.WinForms.RibbonBackstagePageItem();
// 
// footerPage
// 
footerPage.ContentType = typeof(RibbonKitDesignerConsumer.BackstagePrintControl);
footerPage.Id = "rk:e637c113a8a74a6c9f4a07dce00cc6f8";
footerPage.Text = "Print";
// 
// 
// 
ribbon.Backstage.FooterItems.AddRange(new RibbonKit.WinForms.RibbonItem[] {
            footerSeparator,
            footerPage});
ribbon.Backstage.Id = "rk:afbfac252ef74c7985d0b6a997d30156";
ribbon.Backstage.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] {
            page,
            separator});
ribbon.Backstage.Text = "Backstage";

 Controls.Add(ribbon);
 ribbon.EndInit();
 }
}

public sealed class GeneratedContentRibbonForm : RibbonKit.WinForms.RibbonForm
{
 private readonly RibbonKit.WinForms.RibbonControl ribbon;
 public GeneratedContentRibbonForm()
 {
 ribbon = new RibbonKit.WinForms.RibbonControl();
 ribbon.BeginInit();
RibbonKit.WinForms.RibbonBackstagePageItem page;
page = new RibbonKit.WinForms.RibbonBackstagePageItem();
// 
// page
// 
page.ContentType = typeof(RibbonKitDesignerConsumer.BackstageInfoControl);
page.Id = "rk:e37091730b754de5bbeb495548957037";
page.Text = "Info";
RibbonKit.WinForms.RibbonMenuSeparatorItem separator;
separator = new RibbonKit.WinForms.RibbonMenuSeparatorItem();
// 
// separator
// 
separator.Id = "rk:2758bad9d19c405598614aa627f61941";
RibbonKit.WinForms.RibbonMenuSeparatorItem footerSeparator;
footerSeparator = new RibbonKit.WinForms.RibbonMenuSeparatorItem();
// 
// footerSeparator
// 
footerSeparator.Id = "rk:45f3f89179f94ab7a439a69e77ff4639";
RibbonKit.WinForms.RibbonBackstagePageItem footerPage;
footerPage = new RibbonKit.WinForms.RibbonBackstagePageItem();
// 
// footerPage
// 
footerPage.ContentType = typeof(RibbonKitDesignerConsumer.BackstagePrintControl);
footerPage.Id = "rk:11707d2df7044bf78cf569a2233adf41";
footerPage.Text = "Print";
// 
// 
// 
ribbon.Backstage.FooterItems.AddRange(new RibbonKit.WinForms.RibbonItem[] {
            footerSeparator,
            footerPage});
ribbon.Backstage.Id = "rk:804ff29a4a1441f883dc703d29c4c1f8";
ribbon.Backstage.Items.AddRange(new RibbonKit.WinForms.RibbonItem[] {
            page,
            separator});
ribbon.Backstage.Text = "Backstage";

 Controls.Add(ribbon);
 Ribbon = ribbon;
 ribbon.EndInit();
 }
}

