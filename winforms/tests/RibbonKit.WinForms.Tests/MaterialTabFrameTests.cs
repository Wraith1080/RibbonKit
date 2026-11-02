using System.Drawing.Imaging;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MaterialTabFrameTests
{
    [Theory]
    [InlineData(RibbonWindowBackdrop.Mica, RibbonTabContentAnimation.Fade)]
    [InlineData(RibbonWindowBackdrop.Mica, RibbonTabContentAnimation.SlideDown)]
    [InlineData(RibbonWindowBackdrop.Acrylic, RibbonTabContentAnimation.Fade)]
    [InlineData(RibbonWindowBackdrop.Acrylic, RibbonTabContentAnimation.SlideDown)]
    public void Stationary_card_shadow_stays_on_the_live_surface_and_is_absent_from_all_tab_animation_frames(RibbonWindowBackdrop material, RibbonTabContentAnimation style) => FoundationTests.Sta(() =>
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) || SystemInformation.HighContrast) return;
        using var form = new RibbonForm { Backdrop = material, StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1000, 650) };
        var ribbon = new RibbonControl { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon, TabContentAnimation = style };
        var home = new RibbonTab { Text = "Home" }; var edit = new RibbonTab { Text = "Edit" };
        var commands = new RibbonGroup { Text = "Commands" }; commands.Items.Add(new RibbonButtonItem { Text = "Apply" }); home.Groups.Add(commands);
        var fields = new RibbonGroup { Text = "Fields" }; var combo = new RibbonComboBoxItem { Text = "Font" }; combo.Items.AddRange("Arial", "Calibri"); combo.SelectedIndex = 0; fields.Items.Add(combo); edit.Groups.Add(fields);
        ribbon.Tabs.AddRange(home, edit); ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save" }); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        form.Show(); Application.DoEvents(); Assert.Equal(material, form.EffectiveBackdrop);
        ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; ribbon.Motion.SystemPolicyOverride = true;
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; ribbon.SettleMotion(); ribbon.SelectedTab = home; ribbon.SettleMotion();
            using var baseline = LiveFrame(); var frame = ribbon.Snapshot.BodyFrameBounds; int blur = RibbonLayoutEngine.Scale(6, ribbon.DeviceDpi);
            var region = Rectangle.Intersect(ribbon.ClientRectangle, Rectangle.Inflate(frame, blur, blur));
            var shadow = new List<Point>();
            for (int y = region.Top; y < region.Bottom; y++)
            for (int x = region.Left; x < region.Right; x++)
            {
                if (ribbon.Snapshot.Targets.Any(target => target.Kind == TargetKind.Tab && Rectangle.Inflate(target.Bounds, 0, 2).Contains(x, y))) continue;
                var pixel = baseline.GetPixel(x, y);
                if (!frame.Contains(x, y) && pixel.A is > 0 and < 255 && pixel.R == 0 && pixel.G == 0 && pixel.B == 0) shadow.Add(new(x, y));
            }
            Assert.True(shadow.Count > 20);
            using var departure = new RibbonMotionScene(ribbon);
            ribbon.SelectedTab = edit; Assert.True(ribbon.IsAnimating);
            var editor = ribbon.Editors.EditorFor(combo)!; var handle = editor.Handle;
            using var live = LiveFrame();
            var different = shadow.Where(point => baseline.GetPixel(point.X, point.Y) != live.GetPixel(point.X, point.Y)).ToArray();
            Assert.True(different.Length == 0, $"Masking changed {different.Length} shadow pixels; keepFrame={ribbon.KeepsBackdropFrame}, first={different.FirstOrDefault()}, expected={baseline.GetPixel(different.FirstOrDefault().X, different.FirstOrDefault().Y)}, actual={live.GetPixel(different.FirstOrDefault().X, different.FirstOrDefault().Y)}.");
            using var arrival = new RibbonMotionScene(ribbon); var canvas = Rectangle.Union(departure.Bounds, arrival.Bounds);
            using var alpha = new ImageAttributes();
            foreach (double progress in new[] { 0d, .25, .5, .75, 1 })
            {
                using var image = new Bitmap(canvas.Width, canvas.Height, PixelFormat.Format32bppPArgb); using var graphics = Graphics.FromImage(image);
                RibbonMotionScene.Render(graphics, canvas, ribbon, departure, arrival, progress, alpha);
                Assert.True(shadow.All(point => image.GetPixel(point.X, point.Y).A == 0), $"Tab frame {progress} duplicates the live shadow.");
                var body = ribbon.Snapshot.BodyBounds; Assert.Equal(255, image.GetPixel(body.Right - 30, body.Top + 20).A);
            }
            ribbon.SettleMotion(); using var settled = LiveFrame();
            Assert.True(shadow.All(point => baseline.GetPixel(point.X, point.Y) == settled.GetPixel(point.X, point.Y)), "Settling changed the live card's shadow.");
            Assert.Equal(handle, editor.Handle); Assert.Equal("Arial", combo.Value);
        }
        Bitmap LiveFrame()
        {
            var image = new Bitmap(ribbon.Width, ribbon.Height, PixelFormat.Format32bppPArgb);
            RibbonMotionScene.PrintClient(ribbon, image); ribbon.CopyBackdropSnapshot(image); return image;
        }
    });
}
