using System.Diagnostics;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

internal static class BodyPresentationCheck
{
    internal static void Verify(string output)
    {
        Directory.CreateDirectory(output); var evidence = new List<string>();
        foreach (var family in new[] { RibbonBuiltInTheme.Office2019Light, RibbonBuiltInTheme.Office2024Light })
        foreach (var backdrop in new[] { RibbonWindowBackdrop.None, RibbonWindowBackdrop.Mica })
        {
            string scope = family + "-" + backdrop;
            using var painter = new Probe();
            using var host = new RibbonForm { StartPosition = FormStartPosition.Manual, Bounds = new(-16000, -16000, 1100, 750), ShowInTaskbar = false, Backdrop = backdrop };
            using var ribbon = new RibbonControl { BuiltInTheme = family, Renderer = painter, AnimationLevel = RibbonAnimationLevel.None, QuickAccessPosition = RibbonQuickAccessPosition.BelowRibbon };
            var field = new RibbonTextBoxItem { Text = "Name", Value = "Retained C2 input", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var command = new RibbonButtonItem { Text = "Save", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline };
            var group = new RibbonGroup { Text = "Editing", CanResize = false }; group.Items.AddRange(field, command);
            var other = new RibbonGroup { Text = "Layout", CanResize = false }; other.Items.Add(new RibbonButtonItem { Text = "Apply", SimplifiedPlacement = RibbonSimplifiedPlacement.Inline });
            var tab = new RibbonTab { Text = "Home" }; tab.Groups.AddRange(group, other); ribbon.Tabs.Add(tab);
            ribbon.QuickAccessItems.Add(new RibbonQuickAccessReference { Source = command });
            var message = new RibbonMessage { Title = "Frame preview", Text = "Retained message" }; ribbon.MessageBar.Items.Add(message);
            host.Controls.Add(ribbon); host.Ribbon = ribbon; host.Show(); Application.DoEvents();
            var editor = Descendants(ribbon).OfType<TextBox>().Single(control => control.Text == field.Value); nint handle = editor.Handle; var parent = editor.Parent; editor.Select(2, 5);
            var original = ribbon.Theme; var sources = ribbon.QuickAccessItems.ToArray();
            var settings = new RibbonBodyAppearanceSettings { BorderWidth = 3, MessageBorderWidth = 3, GroupSeparatorWidth = 3, GroupSeparatorColor = Color.Magenta,
                QuickAccessSeparatorWidth = 3, QuickAccessSeparatorInset = 7, QuickAccessSeparatorColor = Color.DarkOrange };
            ribbon.Theme = original with { BodyAppearance = settings, BodyBorder = Color.DarkRed, BodyShadowColor = Color.Transparent };
            painter.Check = true;
            int states = 0;
            foreach (var mode in new[] { RibbonLayoutMode.Classic, RibbonLayoutMode.Simplified })
            foreach (var density in new[] { RibbonDensity.Compact, RibbonDensity.Touch })
            foreach (bool minimized in new[] { false, true })
            {
                ribbon.LayoutMode = mode; ribbon.Density = density; ribbon.IsMinimized = minimized; Application.DoEvents();
                painter.LastGroup = painter.LastQuickAccess = painter.LastMessages = Rectangle.Empty;
                using var image = Print(ribbon); image.Save(Path.Combine(output, $"{scope}-{mode}-{density}-{minimized}.png"));
                Require(painter.Failure is null, "Native body painting failed: " + painter.Failure);
                Require(painter.BodyPaints > 0 && painter.MessagePaints > 0 && !painter.LastMessages.IsEmpty, "Native body/message callbacks were missing.");
                if (!minimized) Require(!painter.LastGroup.IsEmpty, "Native group-divider callback was missing.");
                if (!minimized) Require(painter.LastQuickAccess.Bottom == painter.LastMessages.Top, "Joined QAT/message edge was not shared.");
                ribbon.Theme = ribbon.Theme with { BodyAppearance = settings with { JoinBelowRibbonQuickAccess = false, JoinMessageBarToBody = false } };
                using (var separate = Print(ribbon)) { }
                Require(!painter.MessageConnected && painter.LastQuickAccess.Left == 0 && painter.LastMessages.Left == 0, "Authored separation did not reach native geometry.");
                ribbon.Theme = ribbon.Theme with { BodyAppearance = settings }; states++;
                Require(editor.Handle == handle && ReferenceEquals(editor.Parent, parent) && editor.Text == field.Value && editor.SelectionStart == 2 && editor.SelectionLength == 5,
                    "Native input identity, text or selection changed.");
            }
            painter.Check = false; ribbon.IsMinimized = false; ribbon.LayoutMode = RibbonLayoutMode.Classic; ribbon.Density = RibbonDensity.Compact;
            ribbon.Theme = ribbon.Theme with { BodyAppearance = settings with { BorderWidth = null } }; Require(ribbon.Theme.BodyAppearance!.GroupSeparatorWidth == 3, "Member reset cleared another frame setting.");
            ribbon.AnimationLevel = RibbonAnimationLevel.Subtle; message.IsOpen = false;
            Require(ribbon.IsAnimating, "The existing message frame motion did not start."); var watch = Stopwatch.StartNew(); int samples = 0;
            while (ribbon.IsAnimating && watch.ElapsedMilliseconds < 1600) { Application.DoEvents(); Thread.Sleep(8); samples++; }
            Require(!ribbon.IsAnimating && samples > 1, "Message frame motion did not settle.");
            ribbon.AnimationLevel = RibbonAnimationLevel.None; message.IsOpen = true; ribbon.Theme = original;
            Require(ribbon.Theme.BodyAppearance is null && editor.Handle == handle && sources.SequenceEqual(ribbon.QuickAccessItems), "Reset replaced native input or toolbar declarations.");
            evidence.Add($"PASS {scope}: effective={host.EffectiveBackdrop}, dpi={ribbon.DeviceDpi}; {states} native mode/density/minimized states with joined/separate geometry, border/divider callbacks, member/group reset, message motion ({samples} timer samples), retained editor and QAT sources."); host.Close();
        }
        using (var demo = new DesignerForm()) demo.VerifyBodyPresentationDemo();
        evidence.Add("PASS demo: body controls appended once to Theme effects 2; original seven Office 2019 groups retained; preview, public settings and reset checked.");
        evidence.Add("Offscreen public-package checks. Timer samples are not compositor frames; actual VS, visible flicker, physical input and mixed-monitor acceptance remain separate.");
        File.WriteAllLines(Path.Combine(output, "result.log"), evidence);
    }
    private static Bitmap Print(RibbonControl ribbon) { var image = new Bitmap(ribbon.Width, ribbon.Height); ribbon.DrawToBitmap(image, ribbon.ClientRectangle); return image; }
    private static IEnumerable<Control> Descendants(Control root) => root.Controls.Cast<Control>().SelectMany(control => new[] { control }.Concat(Descendants(control)));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private sealed class Probe : RibbonOffice2024Renderer
    {
        internal bool Check, MessageConnected;
        internal int BodyPaints, MessagePaints;
        internal Rectangle LastGroup, LastQuickAccess, LastMessages;
        internal Exception? Failure;
        public override void DrawBody(RibbonPaintContext context)
        {
            if (Check) Safely(() =>
            {
                using var image = new Bitmap(context.Bounds.Width, context.Bounds.Height); using var graphics = Graphics.FromImage(image);
                base.DrawBody(context with { Graphics = graphics, Bounds = new(Point.Empty, image.Size) });
                Require(image.GetPixel(image.Width / 2, image.Height - 1).ToArgb() == Color.DarkRed.ToArgb(), "Authored native body border was not consumed."); BodyPaints++;
            });
            base.DrawBody(context);
        }
        public override void DrawGroupSeparator(RibbonPaintContext context)
        {
            LastGroup = context.Bounds;
            if (Check) Safely(() => Require(context.Bounds.Width == (int)Math.Round(3 * context.DeviceDpi / 96d), "Authored divider width did not reach the public hook."));
            base.DrawGroupSeparator(context);
        }
        public override void DrawQuickAccessToolbar(RibbonPaintContext context, RibbonQuickAccessPosition position)
        { if (position == RibbonQuickAccessPosition.BelowRibbon) LastQuickAccess = context.Bounds; base.DrawQuickAccessToolbar(context, position); }
        public override void DrawMessageBar(RibbonPaintContext context)
        { LastMessages = context.Bounds; MessageConnected = context.IsTopEdgeConnected; MessagePaints++; base.DrawMessageBar(context); }
        private void Safely(Action action) { try { action(); } catch (Exception error) { Failure ??= error; } }
    }
}
