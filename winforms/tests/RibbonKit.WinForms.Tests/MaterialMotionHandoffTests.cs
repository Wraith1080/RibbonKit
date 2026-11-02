using System.Runtime.InteropServices;
using RibbonKit.WinForms.Layout;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class MaterialMotionHandoffTests
{
    [Theory]
    [InlineData(RibbonWindowBackdrop.Mica, RibbonTabContentAnimation.Fade, false)]
    [InlineData(RibbonWindowBackdrop.Mica, RibbonTabContentAnimation.SlideDown, false)]
    [InlineData(RibbonWindowBackdrop.Acrylic, RibbonTabContentAnimation.Fade, false)]
    [InlineData(RibbonWindowBackdrop.Acrylic, RibbonTabContentAnimation.SlideDown, false)]
    [InlineData(RibbonWindowBackdrop.Mica, RibbonTabContentAnimation.Fade, true)]
    [InlineData(RibbonWindowBackdrop.Mica, RibbonTabContentAnimation.SlideDown, true)]
    [InlineData(RibbonWindowBackdrop.Acrylic, RibbonTabContentAnimation.Fade, true)]
    [InlineData(RibbonWindowBackdrop.Acrylic, RibbonTabContentAnimation.SlideDown, true)]
    public void Clearing_or_restoring_the_live_body_always_has_an_opaque_visible_motion_cover(RibbonWindowBackdrop material, RibbonTabContentAnimation style, bool addedCombo) => FoundationTests.Sta(() =>
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621) || SystemInformation.HighContrast) return;
        using var form = new RibbonForm { Backdrop = material, StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1000, 600) };
        var ribbon = new RibbonControl { QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon, TabContentAnimation = style };
        var home = new RibbonTab { Text = "Home" }; var edit = new RibbonTab { Text = "Edit" };
        var commands = new RibbonGroup { Text = "Commands" }; commands.Items.Add(new RibbonButtonItem { Text = "Apply" }); home.Groups.Add(commands);
        var fields = new RibbonGroup { Text = "Fields" }; var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained value" }; fields.Items.Add(field); edit.Groups.Add(fields);
        if (addedCombo)
        {
            var font = new RibbonComboBoxItem { Text = "Font", ShowLabel = false }; font.Items.AddRange("Arial", "Calibri"); font.SelectedIndex = 0; commands.Items.Add(font);
        }
        ribbon.Tabs.AddRange(home, edit); ribbon.QuickAccessItems.Add(new RibbonButtonItem { Text = "Save" }); form.Controls.Add(ribbon); form.Ribbon = ribbon;
        form.Show(); Application.DoEvents(); Assert.Equal(material, form.EffectiveBackdrop);
        ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; ribbon.Motion.SystemPolicyOverride = true;
        var observations = new List<(bool Masked, bool Covered)>(); bool sawMask = false;
        ribbon.Invalidated += (_, _) =>
        {
            if (!ribbon.Motion.Changing && !ribbon.Motion.IsRunning) return;
            bool masked = ribbon.BackdropBodyMasked;
            if (!masked && !sawMask) return;
            sawMask |= masked; bool covered = false;
            try
            {
                using var image = ribbon.Motion.CaptureFrame();
                var body = ribbon.Snapshot.BodyBounds;
                nint cover = FindWindowEx(form.Handle, 0, null, "Ribbon motion");
                covered = image is not null && cover != 0 && IsWindowVisible(cover)
                    && body.Width > 0 && body.Height > 0 && image.GetPixel(body.Right - 30, body.Top + 15).A > 240;
            }
            catch (NullReferenceException) { /* A mask published before the first frame has no drawable bitmap. */ }
            observations.Add((masked, covered));
        };
        foreach (var density in Enum.GetValues<RibbonDensity>())
        {
            ribbon.Density = density; ribbon.SettleMotion(); observations.Clear(); sawMask = false;
            ribbon.SelectedTab = edit; Assert.True(ribbon.Motion.IsRunning); ribbon.SettleMotion();
            ribbon.SelectedTab = home; Assert.True(ribbon.Motion.IsRunning); ribbon.SettleMotion();
            Assert.Contains(observations, sample => sample.Masked); Assert.Contains(observations, sample => !sample.Masked);
            Assert.All(observations, sample => Assert.True(sample.Covered, $"The live body was {(sample.Masked ? "cleared" : "restored")} without a visible opaque animation cover."));
            Assert.False(ribbon.BackdropBodyMasked); Assert.Equal("Retained value", field.Value);
        }
    });
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint FindWindowEx(nint parent, nint after, string? className, string? title);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
}
