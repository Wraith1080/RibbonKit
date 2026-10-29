using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class PopupMotionTests
{
    private sealed class Fixture : IDisposable
    {
        internal readonly Form Host = new() { Size = new(1150, 750) };
        internal readonly RibbonControl Ribbon;
        internal readonly RibbonDropDownItem Menu = new() { Id = "menu", Text = "Paste", DropDownHeader = "Paste options" };
        internal readonly RibbonDropDownItem Nested = new() { Text = "More" };
        internal readonly RibbonComboBoxItem Combo = new() { Id = "font", Text = "Font", IsEditable = true, Value = "100" };
        internal readonly RibbonGalleryItem Gallery = new() { Text = "Styles", ColumnCount = 3 };
        internal Fixture(bool office)
        {
            Application.EnableVisualStyles(); Ribbon = new() { Renderer = office ? new RibbonOffice2024Renderer() : new RibbonRenderer(), BindingContext = new BindingContext() };
            var tab = new RibbonTab { Text = "Home" }; var group = new RibbonGroup { Text = "Commands" }; tab.Groups.Add(group); group.Items.AddRange(Menu, Gallery); Ribbon.Tabs.Add(tab);
            Menu.Items.Add(new RibbonButtonItem { Text = "Paste text only", Icon = new SolidIcon(), MenuDescription = "Insert plain clipboard text." }); Nested.Items.Add(new RibbonButtonItem { Text = "Nested command" }); Menu.Items.AddRange(Nested, Combo); Combo.Items.AddRange("100", "150");
            for (int i = 0; i < 6; i++) Gallery.Items.Add(new RibbonGalleryTile { Text = "Style " + i, Icon = new SolidIcon() });
            Host.Controls.Add(Ribbon); Host.Show(); Application.DoEvents(); Ribbon.Motion.SystemPolicyOverride = true;
        }
        internal RibbonPopupWindow Open(bool gallery = false)
        {
            RibbonDropDownItem item = gallery ? Gallery : Menu; item.ShowDropDown(); return (RibbonPopupWindow)Ribbon.Popups.Top!.Parent!;
        }
        public void Dispose() { var painter = Ribbon.Renderer; Host.Dispose(); painter?.Dispose(); }
    }
    private sealed class SolidIcon : IRibbonIcon { public void Draw(Graphics graphics, Rectangle bounds, RibbonIconContext context) => graphics.FillRectangle(Brushes.Magenta, bounds); }

    [Theory] [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public void Settling_menu_and_gallery_opening_keeps_frame_owner_and_native_editor_identity(bool office, bool gallery) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(office); f.Ribbon.AnimationLevel = gallery ? RibbonAnimationLevel.Expressive : RibbonAnimationLevel.Subtle;
        var window = f.Open(gallery); var surface = f.Ribbon.Popups.Top!; var bounds = window.Bounds; var motion = window.OpeningMotion;
        Assert.True(f.Ribbon.IsAnimating); Assert.NotNull(motion); Assert.True(motion!.IsRunning); Assert.True(RibbonDrawingHold.HasOpacity(surface.Handle));
        nint editor = gallery ? 0 : f.Ribbon.Editors.EditorFor(f.Combo)!.Handle; var owner = gallery ? f.Gallery.Items[0].Parent : f.Combo.Parent;
        window.SettleMotion(); Assert.False(f.Ribbon.IsAnimating); Assert.False(RibbonDrawingHold.HasOpacity(surface.Handle)); Assert.Equal(bounds, window.Bounds); Assert.True(window.Visible);
        if (!gallery) Assert.Equal(editor, f.Ribbon.Editors.EditorFor(f.Combo)!.Handle);
        Assert.Same(owner, gallery ? f.Gallery.Items[0].Parent : f.Combo.Parent); Assert.True(f.Ribbon.Popups.HasOpen);
        f.Ribbon.Popups.CloseAll(false); Assert.False(f.Ribbon.IsAnimating); Assert.True(window.IsDisposed);
    });

    [Theory] [InlineData(false)] [InlineData(true)]
    public void Inner_drawing_starts_blank_fades_at_natural_size_and_preserves_the_real_clip(bool gallery) => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; var window = f.Open(gallery); var motion = window.OpeningMotion!; var surface = f.Ribbon.Popups.Top!;
        Assert.True(motion.IsRunning); var frames = new List<Bitmap>();
        try
        {
            foreach (double progress in new[] { 0, .5, 1 })
            {
                var frame = new Bitmap(surface.Width, surface.Height, PixelFormat.Format32bppPArgb); frames.Add(frame); using var graphics = Graphics.FromImage(frame); using var alpha = new ImageAttributes(); motion.Draw(graphics, alpha, progress);
                foreach (var point in new[] { Point.Empty, new Point(frame.Width - 1, 0), new Point(0, frame.Height - 1), new Point(frame.Width - 1, frame.Height - 1) })
                    if (surface.Region is { } clip && !clip.IsVisible(point)) Assert.Equal(0, frame.GetPixel(point.X, point.Y).A);
            }
            var first = Pink(frames[0]); var middle = Pink(frames[1]); var last = Pink(frames[2]); Assert.True(first.IsEmpty); Assert.False(last.IsEmpty); Assert.Equal(last.Size, middle.Size); Assert.Equal(last.Left, middle.Left); Assert.True(middle.Top < last.Top);
            var color = frames[1].GetPixel(middle.Left + middle.Width / 2, middle.Top + Math.Min(2, middle.Height - 1)); Assert.InRange(color.G, 1, 254);
            string directory = Path.Combine("TestResults", "visual", "m6a-popup"); Directory.CreateDirectory(directory); for (int i = 0; i < frames.Count; i++) frames[i].Save(Path.Combine(directory, $"{gallery}-{i}.png"));
        }
        finally { foreach (var frame in frames) frame.Dispose(); }
    });

    [Fact]
    public void Policy_input_mutation_and_popup_lifetime_release_the_hold_without_delaying_the_input() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); var window = f.Open(); Assert.False(f.Ribbon.IsAnimating); f.Ribbon.Popups.CloseAll(false);
        f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.Motion.SystemPolicyOverride = false; window = f.Open(); Assert.False(f.Ribbon.IsAnimating); f.Ribbon.Popups.CloseAll(false); f.Ribbon.Motion.SystemPolicyOverride = true;
        window = f.Open(); var surface = f.Ribbon.Popups.Top!; Assert.True(f.Ribbon.IsAnimating); var input = Message.Create(surface.Handle, 0x100, (nint)Keys.Down, 0); f.Ribbon.ObserveMotionInput(ref input); Assert.False(f.Ribbon.IsAnimating); Assert.False(RibbonDrawingHold.HasOpacity(surface.Handle)); Assert.True(f.Ribbon.Popups.HasOpen); f.Ribbon.Popups.CloseAll(false);
        window = f.Open(); Assert.True(f.Ribbon.IsAnimating); f.Menu.Items[0].Text += " changed"; Assert.False(f.Ribbon.IsAnimating); Assert.True(window.IsDisposed);
        window = f.Open(); Assert.True(f.Ribbon.IsAnimating); f.Ribbon.AnimationLevel = RibbonAnimationLevel.None; Assert.False(f.Ribbon.IsAnimating); Assert.True(window.Visible); f.Ribbon.Popups.CloseAll(false); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        window = f.Open(); Assert.True(f.Ribbon.IsAnimating); window.Width += 5; Assert.False(f.Ribbon.IsAnimating); f.Ribbon.Popups.CloseAll(false);
    });

    [Fact]
    public void Opening_a_submenu_settles_its_parent_and_closing_or_disposal_is_immediate() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(false); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Expressive; var parent = f.Open(); Assert.True(f.Ribbon.IsAnimating);
        var surface = f.Ribbon.Popups.Top!; f.Ribbon.Activate(surface.Targets.Single(t => ReferenceEquals(t.Element, f.Nested)), surface); var child = (RibbonPopupWindow)f.Ribbon.Popups.Top!.Parent!;
        Assert.NotSame(parent, child); Assert.Null(parent.OpeningMotion); Assert.True(child.OpeningMotion!.IsRunning); Assert.True(f.Ribbon.IsAnimating); Assert.Equal(2, f.Ribbon.Popups.Surfaces.Count());
        f.Ribbon.Popups.CloseTop(true); Assert.False(f.Ribbon.IsAnimating); Assert.True(parent.Visible); Assert.True(child.IsDisposed);
        f.Ribbon.Popups.CloseAll(false); var window = f.Open(true); Assert.True(f.Ribbon.IsAnimating); f.Ribbon.Dispose(); Assert.True(window.IsDisposed); Assert.False(f.Ribbon.IsAnimating);
    });

    [Fact]
    public void Animation_policy_changes_inside_geometry_callbacks_still_settle_the_shared_drawing_hold() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle;
        f.Ribbon.DensityChanged += (_, _) => f.Ribbon.AnimationLevel = RibbonAnimationLevel.None;
        f.Ribbon.Density = RibbonDensity.Touch; Assert.Equal(RibbonDensity.Touch, f.Ribbon.Density); Assert.False(f.Ribbon.IsAnimating); Assert.False(RibbonDrawingHold.HasOpacity(f.Ribbon.Handle));
    });

    private static Rectangle Pink(Bitmap image)
    {
        int left = image.Width, top = image.Height, right = -1, bottom = -1; var data = image.LockBits(new(Point.Empty, image.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb); int[] row = new int[image.Width];
        try
        {
            for (int y = 0; y < image.Height; y++)
            {
                Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length);
                for (int x = 0; x < row.Length; x++) { var c = Color.FromArgb(row[x]); if (c.A > 0 && c.R == 255 && c.B == 255 && c.G < 240) { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); } }
            }
        }
        finally { image.UnlockBits(data); }
        return right < 0 ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
    }
    [Fact]
    public void Batch_changes_and_authored_form_transparency_keep_popups_immediate() => FoundationTests.Sta(() =>
    {
        using var f = new Fixture(true); f.Ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; f.Ribbon.BeginUpdate(); var window = f.Open(); Assert.False(f.Ribbon.IsAnimating); Assert.Null(window.OpeningMotion); f.Ribbon.EndUpdate(); Assert.True(window.IsDisposed);
        f.Host.Opacity = .9; window = f.Open(); Assert.False(f.Ribbon.IsAnimating); Assert.Null(window.OpeningMotion); Assert.Equal(.9, f.Host.Opacity); f.Ribbon.Popups.CloseAll(false);
    });
}
