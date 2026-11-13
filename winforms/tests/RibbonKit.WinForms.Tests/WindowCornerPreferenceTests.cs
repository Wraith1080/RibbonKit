using System.ComponentModel;
using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class WindowCornerPreferenceTests
{
    [Fact]
    public void Defaults_override_reset_and_invalid_assignment_preserve_application_theme_atomically() => FoundationTests.Sta(() =>
    {
        using var form = new Probe(); using var ribbon = new RibbonControl();
        Assert.Null(form.WindowCornerPreference);
        Assert.Equal(RibbonWindowCornerPreference.SystemDefault, form.EffectiveWindowCornerPreference);
        Assert.Equal(RibbonWindowCornerPreference.SystemDefault, RibbonThemes.Office2024Light.WindowCornerPreference);
        Assert.Equal(RibbonWindowCornerPreference.Square, RibbonThemes.Office2019Light.WindowCornerPreference);
        form.Controls.Add(ribbon); form.Ribbon = ribbon;
        var application = RibbonThemes.Office2024Light with { WindowCornerPreference = RibbonWindowCornerPreference.SmallRounded };
        ribbon.Theme = application;
        Assert.Equal(RibbonWindowCornerPreference.SmallRounded, form.EffectiveWindowCornerPreference);
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light;
        Assert.Equal(RibbonWindowCornerPreference.Square, form.EffectiveWindowCornerPreference);
        int changed = 0; form.WindowCornerPreferenceChanged += (_, _) => changed++;
        var property = TypeDescriptor.GetProperties(form)[nameof(form.WindowCornerPreference)]!;
        Assert.False(property.ShouldSerializeValue(form));
        property.SetValue(form, RibbonWindowCornerPreference.Rounded);
        Assert.True(property.ShouldSerializeValue(form));
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        Assert.Equal(RibbonWindowCornerPreference.Rounded, form.EffectiveWindowCornerPreference);
        Assert.Throws<ArgumentOutOfRangeException>(() => form.WindowCornerPreference = (RibbonWindowCornerPreference)4);
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.Theme = application with { WindowCornerPreference = (RibbonWindowCornerPreference)(-1) });
        Assert.Throws<ArgumentOutOfRangeException>(() => ribbon.BuiltInTheme = (RibbonBuiltInTheme)99);
        Assert.Equal(RibbonBuiltInTheme.Office2024Light, ribbon.BuiltInTheme);
        Assert.Same(RibbonThemes.Office2024Light, ribbon.Theme); Assert.Same(application, ribbon.ApplicationTheme);
        Assert.Equal(RibbonWindowCornerPreference.Rounded, form.WindowCornerPreference); Assert.Equal(1, changed);
        property.ResetValue(form);
        Assert.Null(form.WindowCornerPreference); Assert.False(property.ShouldSerializeValue(form));
        Assert.Equal(RibbonWindowCornerPreference.SystemDefault, form.EffectiveWindowCornerPreference);
        ribbon.BuiltInTheme = null;
        Assert.Same(application, ribbon.Theme); Assert.Equal(RibbonWindowCornerPreference.SmallRounded, form.EffectiveWindowCornerPreference);
        Assert.Equal(2, changed); Assert.False(form.IsHandleCreated);
        var effective = TypeDescriptor.GetProperties(form)[nameof(form.EffectiveWindowCornerPreference)]!;
        Assert.False(effective.IsBrowsable);
        Assert.Equal(DesignerSerializationVisibility.Hidden, ((DesignerSerializationVisibilityAttribute)effective.Attributes[typeof(DesignerSerializationVisibilityAttribute)]!).Visibility);
    });

    [Theory]
    [InlineData(false, false, true)] [InlineData(true, true, true)] [InlineData(true, false, false)]
    public void Unsupported_designer_and_child_policy_retains_every_request(bool supported, bool designer, bool topLevel) => FoundationTests.Sta(() =>
    {
        using var form = new RibbonForm();
        foreach (var preference in Enum.GetValues<RibbonWindowCornerPreference>())
        {
            form.WindowCornerPreference = preference;
            Assert.False(RibbonForm.CanApplyWindowCornerPreference(supported, designer, topLevel));
            Assert.Equal(preference, form.WindowCornerPreference); Assert.Equal(preference, form.EffectiveWindowCornerPreference);
            Assert.False(form.IsHandleCreated);
        }
        Assert.True(RibbonForm.CanApplyWindowCornerPreference(true, false, true));
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Designer_and_child_handles_leave_native_attribute_unchanged(bool child) => FoundationTests.Sta(() =>
    {
        if (!Supported) return;
        using var parent = new Form(); using var form = new Probe();
        if (child) { form.TopLevel = false; parent.Controls.Add(form); }
        else form.Site = new DesignSite(form);
        nint handle = form.Handle;
        // A foreign sentinel detects an accidental application, including a
        // SystemDefault reset on creation/recreation of inert handles.
        int marker = (int)RibbonWindowCornerPreference.SmallRounded;
        DwmSetWindowAttribute(handle, 33, ref marker, sizeof(int));
        int beforeResult = DwmGetWindowAttribute(handle, 33, out int before, sizeof(int));
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light };
        form.Controls.Add(ribbon); form.Ribbon = ribbon;
        form.WindowCornerPreference = RibbonWindowCornerPreference.Rounded;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light;
        form.WindowCornerPreference = null;
        Assert.Equal(beforeResult, DwmGetWindowAttribute(handle, 33, out int after, sizeof(int)));
        if (beforeResult >= 0) Assert.Equal(before, after);
        Assert.Equal(handle, form.Handle);
    });

    [Fact]
    public void Live_theme_association_switch_reset_and_reassociation_update_native_request() => FoundationTests.Sta(() =>
    {
        if (!Supported) return;
        using var form = new Probe(); using var ribbon = new RibbonControl();
        _ = form.Handle; Check(RibbonWindowCornerPreference.SystemDefault);
        form.Controls.Add(ribbon); form.Ribbon = ribbon;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; Check(RibbonWindowCornerPreference.Square);
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2024Light; Check(RibbonWindowCornerPreference.SystemDefault);
        var application = ribbon.Theme with { WindowCornerPreference = RibbonWindowCornerPreference.SmallRounded };
        ribbon.Theme = application; Check(RibbonWindowCornerPreference.SmallRounded);
        form.WindowCornerPreference = RibbonWindowCornerPreference.Rounded;
        ribbon.BuiltInTheme = RibbonBuiltInTheme.Office2019Light; Check(RibbonWindowCornerPreference.Rounded);
        form.WindowCornerPreference = null; Check(RibbonWindowCornerPreference.Square);
        ribbon.BuiltInTheme = null; Check(RibbonWindowCornerPreference.SmallRounded);
        form.Ribbon = null; Check(RibbonWindowCornerPreference.SystemDefault);
        form.Ribbon = ribbon; Check(RibbonWindowCornerPreference.SmallRounded);
        ribbon.Dock = DockStyle.None; Check(RibbonWindowCornerPreference.SystemDefault);
        ribbon.Dock = DockStyle.Top; form.Ribbon = ribbon; ribbon.Dispose(); Check(RibbonWindowCornerPreference.SystemDefault);
        void Check(RibbonWindowCornerPreference value)
        { Assert.Equal(value, form.EffectiveWindowCornerPreference); Assert.Equal((int)value, Read(form.Handle)); }
    });

    [Theory]
    [InlineData(RibbonWindowCornerPreference.SystemDefault)] [InlineData(RibbonWindowCornerPreference.Square)]
    [InlineData(RibbonWindowCornerPreference.Rounded)] [InlineData(RibbonWindowCornerPreference.SmallRounded)]
    public void Creation_recreation_native_resize_and_system_caption_targets_keep_the_preference(RibbonWindowCornerPreference value) => FoundationTests.Sta(() =>
    {
        if (!Supported) return;
        using var form = new Probe { WindowCornerPreference = value, Bounds = new(-16000, -16000, 850, 500), StartPosition = FormStartPosition.Manual };
        Assert.False(form.IsHandleCreated); Assert.Equal((int)value, Read(form.Handle));
        form.Show(); Application.DoEvents(); AssertFrame();
        Assert.Equal(12, form.CaptionHitTest(new(200, 1))); // Native top resize.
        foreach (var target in form.Caption.Targets) Assert.Equal(target.Hit, form.CaptionHitTest(new(target.Bounds.Left + target.Bounds.Width / 2, target.Bounds.Height / 2)));
        form.WindowState = FormWindowState.Maximized; Application.DoEvents();
        Assert.Equal(2, form.CaptionHitTest(new(200, 1))); Assert.Equal((int)value, Read(form.Handle));
        form.WindowState = FormWindowState.Normal; form.Size = new(960, 600); Application.DoEvents(); AssertFrame();
        form.Rebuild(); AssertFrame();
        void AssertFrame()
        {
            Assert.Null(form.Region); Assert.Equal(FormBorderStyle.Sizable, form.FormBorderStyle);
            Assert.Equal(0x00CF0000, form.NativeStyle & 0x00CF0000); // caption/thickframe/system menu/min/max
            Assert.Equal((int)value, Read(form.Handle)); Assert.Equal(value, form.WindowCornerPreference);
        }
    });

    [Theory]
    [InlineData(RibbonWindowBackdrop.None, false)] [InlineData(RibbonWindowBackdrop.Mica, false)]
    [InlineData(RibbonWindowBackdrop.Acrylic, true)]
    public void Corners_coexist_with_material_and_survive_cancelled_close_and_native_destruction(RibbonWindowBackdrop backdrop, bool dispose) => FoundationTests.Sta(() =>
    {
        if (!Supported) return;
        using var form = new Probe { WindowCornerPreference = RibbonWindowCornerPreference.Square, Backdrop = backdrop,
            Bounds = new(-16000, -16000, 850, 500), StartPosition = FormStartPosition.Manual };
        form.Show(); Application.DoEvents();
        var applied = form.EffectiveBackdrop;
        form.WindowCornerPreference = RibbonWindowCornerPreference.SmallRounded;
        Assert.Equal(applied, form.EffectiveBackdrop);
        form.Rebuild(); Assert.Equal(applied, form.EffectiveBackdrop); Assert.Equal(3, Read(form.Handle));
        bool cancel = true; form.FormClosing += (_, e) => e.Cancel = cancel;
        form.Close(); Assert.False(form.IsDisposed); Assert.Equal(3, Read(form.Handle));
        cancel = false; form.Observe = true;
        if (dispose) form.Dispose(); else form.Close();
        Assert.Equal(3, Assert.Single(form.DestroyedCorners)); Assert.True(form.IsDisposed);
        Assert.Equal(RibbonWindowCornerPreference.SmallRounded, form.WindowCornerPreference);
    });

    [Fact]
    public void Changing_only_corners_does_not_settle_backstage_or_title_clocks_or_replace_native_pages() => FoundationTests.Sta(() =>
    {
        using var form = new Probe { Text = "Corner changes retain title motion", Bounds = new(-16000, -16000, 950, 600), StartPosition = FormStartPosition.Manual };
        using var ribbon = new RibbonControl { BuiltInTheme = RibbonBuiltInTheme.Office2019Light, FileMenuMode = RibbonFileMenuMode.Backstage, AnimationLevel = RibbonAnimationLevel.Expressive };
        ribbon.Tabs.Add(new RibbonTab { Text = "Home" });
        ribbon.FileMenu.Visible = true;
        using var page = new TextBox { Text = "Retained native page" };
        ribbon.Backstage.Items.Add(new RibbonBackstagePageItem { Text = "Info", Content = page });
        form.Controls.Add(ribbon); form.Ribbon = ribbon; form.Show(); Application.DoEvents();
        ribbon.Motion.SystemPolicyOverride = true;
        ribbon.IsBackstageOpen = true; ribbon.SettleMotion(); var handle = page.Handle; var parent = page.Parent;
        page.Select(2, 5);
        ribbon.IsBackstageOpen = false;
        var paneMotion = ribbon.BackstageOpeningMotion; var titleMotion = form.CaptionTitleMotion;
        Assert.NotNull(paneMotion); Assert.NotNull(titleMotion);
        form.WindowCornerPreference = RibbonWindowCornerPreference.Rounded;
        Assert.Same(paneMotion, ribbon.BackstageOpeningMotion); Assert.Same(titleMotion, form.CaptionTitleMotion);
        Assert.True(paneMotion.IsRunning); Assert.True(form.IsCaptionTitleAnimating);
        Assert.Equal(handle, page.Handle); Assert.Same(parent, page.Parent); Assert.Equal("Retained native page", page.Text);
        Assert.Equal(2, page.SelectionStart); Assert.Equal(5, page.SelectionLength);
    });

    private static bool Supported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000);
    private static int Read(nint window)
    { Assert.True(DwmGetWindowAttribute(window, 33, out int value, sizeof(int)) >= 0); return value; }
    private sealed class Probe : RibbonForm
    {
        internal int NativeStyle => CreateParams.Style;
        internal bool Observe;
        internal List<int> DestroyedCorners { get; } = [];
        internal void Rebuild() => RecreateHandle();
        protected override void WndProc(ref Message message)
        {
            if (Observe && message.Msg == 2) DestroyedCorners.Add(Read(message.HWnd));
            base.WndProc(ref message);
        }
    }
    private sealed class DesignSite(IComponent component) : ISite
    {
        public IComponent Component => component; public IContainer? Container => null;
        public bool DesignMode => true; public string? Name { get; set; }
        public object? GetService(Type serviceType) => null;
    }
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);
}
