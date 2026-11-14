using System.CodeDom;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Runtime.InteropServices;
using RibbonKit.WinForms;

namespace RibbonKitDesignerConsumer;

public partial class DesignerForm
{
    [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")] private static extern bool SetInputModeForeground(nint window);
    [DllImport("user32.dll", EntryPoint = "mouse_event")] private static extern void InputModeMouseEvent(uint flags, uint x, uint y, uint data, nuint extra);

    internal void VerifyInputMode(string directory)
    {
        Directory.CreateDirectory(directory); var log = new List<string>();
        Require(homeTab.Groups.Contains(inputModeGroup) && inputModeGroup.Items.Single() == inputModeSelector, "Designer-authored Home/Input mode ownership");
        Require(inputModeSelector.Items.SequenceEqual(new RibbonItem[] { mouseModeOption, touchModeOption }), "Designer-authored Mouse/Touch radio order");
        Require(ribbon.QuickAccessItems[2] == inputModeQatReference && ReferenceEquals(inputModeQatReference.Source, inputModeSelector), "Third QAT entry references the one designer-authored selector");
        Require(inputModeSelector.MenuPresentation == RibbonMenuPresentation.IllustratedChoices && inputModeSelector.DropDownHeader == "Optimize spacing between commands", "Illustrated choices and noninteractive header");
        Require(mouseModeOption.MenuDescription.Contains("Optimized for use with mouse") && touchModeOption.MenuDescription.Contains("Optimized for use with touch"), "Showcase descriptions retained");
        Require(mouseModeOption.Icon is RibbonSvgIcon { Name: "Mouse spacing", UseForegroundColor: false } && touchModeOption.Icon is RibbonSvgIcon { Name: "Touch spacing", UseForegroundColor: false }, "Resource-backed colored vector artwork");
        Require(!GetType().Assembly.GetReferencedAssemblies().Any(a => a.Name?.Contains("Showcase") == true), "Public-package sample has no Showcase dependency");
        VerifySerialization(); log.Add("PASS: designer declarations, SVG resources/converters, menu metadata and QAT source serialization");

        var record = new InputModePaint(); using var painter = new InputModeRenderer(record, inputModeSelector, inputModeQatReference);
        ribbon.Renderer = painter; ribbon.AnimationLevel = RibbonAnimationLevel.None;
        var work = Screen.FromControl(this).WorkingArea; Size = new(Math.Min(1400, work.Width - 80), Math.Min(850, work.Height - 80));
        StartPosition = FormStartPosition.CenterScreen; ribbon.SelectedTab = homeTab;
        int densityEvents = 0, mouseClicks = 0, touchClicks = 0;
        ribbon.DensityChanged += (_, _) => densityEvents++; mouseModeOption.Click += (_, _) => mouseClicks++; touchModeOption.Click += (_, _) => touchClicks++;
        Exception? failure = null; var pointer = Cursor.Position;
        Shown += async (_, _) =>
        {
            try
            {
                await Task.Yield(); Activate(); SetInputModeForeground(Handle); await Task.Delay(250); Guard(); ribbon.Refresh();
                CheckState(RibbonDensity.Compact); var font = ribbon.Font;
                await Click(ribbon, record.Qat); var popup = Surface(); await Capture("mouse");
                await Click(popup, record.Header); Require(inputModeQatReference.IsDropDownOpen && densityEvents == 0, "Heading is noninteractive");
                await Click(popup, record.Rows[touchModeOption]); CheckState(RibbonDensity.Touch);
                Require(!inputModeQatReference.IsDropDownOpen && densityEvents == 1 && touchClicks == 1 && ReferenceEquals(font, ribbon.Font), "QAT Touch invokes once and keeps the authored font");
                ribbon.Refresh(); await Click(ribbon, record.Qat); await Capture("touch"); await Key("{HOME}{ENTER}"); CheckState(RibbonDensity.Compact);
                Require(densityEvents == 2 && mouseClicks == 1, "QAT keyboard returns to Mouse once");
                ribbon.Density = RibbonDensity.Touch; CheckState(RibbonDensity.Touch); ribbon.Refresh();
                await Click(ribbon, record.Selector); await Key("{HOME}{ENTER}"); CheckState(RibbonDensity.Compact);
                Require(densityEvents == 4, "Home selector and external Density assignment stay synchronized");
                int before = densityEvents; ribbon.Refresh(); await Click(ribbon, record.Qat); await Key("{HOME}{ENTER}");
                Require(densityEvents == before && mouseModeOption.Checked && !touchModeOption.Checked, "Choosing the current mode is a density no-op");

                ribbon.SelectedTab = editTab; await Task.Delay(80);
                var editor = ChildControls(ribbon).OfType<TextBox>().Single(c => c.Text == nameField.Value); var handle = editor.Handle;
                editor.Focus(); editor.Text = "Retained density review"; editor.Select(2, 4);
                ribbon.Refresh(); await Click(ribbon, record.Qat); await Click(Surface(), record.Rows[touchModeOption]); CheckState(RibbonDensity.Touch);
                Require(editor.Handle == handle && editor.Text == "Retained density review", "QAT is available from Edit and retains the native editor HWND/value");
                ribbon.Refresh(); await Click(ribbon, record.Qat); await Key("{HOME}{ENTER}"); CheckState(RibbonDensity.Compact);
                Require(editor.Handle == handle && editor.Text == "Retained density review", "Returning to Mouse keeps the same native editor");
                bool reject = true; nameField.ValueValidating += (_, e) => e.Cancel = reject; editor.Focus(); editor.Text = "Pending validation";
                touchModeOption.PerformClick(); CheckState(RibbonDensity.Compact);
                Require(mouseModeOption.Checked && !touchModeOption.Checked, "Validation veto restores the radio selection to the effective density");
                reject = false; touchModeOption.PerformClick(); CheckState(RibbonDensity.Touch); mouseModeOption.PerformClick(); CheckState(RibbonDensity.Compact);
                Require(editor.Handle == handle && editor.Text == "Pending validation", "Accepted edit survives subsequent density changes");
                log.Add($"PASS: actual {ribbon.DeviceDpi} DPI; native QAT/Home pointer and keyboard, mode synchronization, validation veto, retained editor; geometry review remains a separate gate");
            }
            catch (Exception error) { failure = error; log.Add("FAIL: " + error); }
            finally { inputModeSelector.CloseDropDown(); Close(); }
        };
        try { Application.Run(this); } finally { Cursor.Position = pointer; }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log); if (failure is not null) throw new InvalidOperationException("Input mode review fixture failed.", failure);

        void CheckState(RibbonDensity density)
        {
            bool mouse = density == RibbonDensity.Compact;
            Require(ribbon.Density == density && mouseModeOption.Checked == mouse && touchModeOption.Checked != mouse && inputModeSelector.Text == (mouse ? "Mouse" : "Touch")
                && ReferenceEquals(inputModeSelector.Icon, mouse ? mouseModeOption.Icon : touchModeOption.Icon), "Effective mode, selected radio, caption and icon agree");
        }
        void Guard() { Require(GetForegroundWindow() == Handle || Control.FromHandle(GetForegroundWindow()) is ToolStripDropDown, "Input mode fixture lost foreground; input stopped"); }
        async Task Key(string keys) { Guard(); SendKeys.SendWait(keys); await Task.Delay(100); Guard(); }
        async Task Click(Control control, Rectangle bounds)
        {
            Guard(); bounds.Intersect(control.ClientRectangle); Require(bounds.Width > 0 && bounds.Height > 0, "Input target is visible");
            Cursor.Position = control.PointToScreen(new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)); await Task.Delay(40); Guard();
            InputModeMouseEvent(2, 0, 0, 0, 0); InputModeMouseEvent(4, 0, 0, 0, 0); await Task.Delay(130); Guard();
        }
        Control Surface() => Control.FromHandle(GetForegroundWindow()) is ToolStripDropDown window ? window.Items.OfType<ToolStripControlHost>().Single().Control : throw new InvalidOperationException("Expected native density popup");
        async Task Capture(string name)
        {
            await Task.Delay(70); Guard(); var popup = Surface(); var bounds = Rectangle.Union(Bounds, popup.TopLevelControl!.Bounds);
            using var pixels = new Bitmap(bounds.Width, bounds.Height); using var graphics = Graphics.FromImage(pixels);
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size); pixels.Save(Path.Combine(directory, name + ".png"));
        }
        void VerifySerialization()
        {
            var serialization = new DesignerSerializationManager(); using var session = serialization.CreateSession(); var manager = (IDesignerSerializationManager)serialization;
            foreach (var (component, name) in new (IComponent, string)[] { (inputModeSelector, "inputModeSelector"), (mouseModeOption, "mouseModeOption"), (touchModeOption, "touchModeOption"), (inputModeQatReference, "inputModeQatReference") }) manager.SetName(component, name);
            using var provider = CodeDomProvider.CreateProvider("CSharp"); using var code = new StringWriter();
            foreach (var (component, property) in new (IComponent, string)[] { (inputModeSelector, "DropDownHeader"), (inputModeSelector, "MenuPresentation"), (mouseModeOption, "MenuDescription"), (touchModeOption, "MenuDescription"), (inputModeQatReference, "Source"),
                (mouseModeOption, "Icon"), (touchModeOption, "Icon"), (inputModeSelector, "Icon") })
            {
                var serializer = (CodeDomSerializer)manager.GetSerializer(component.GetType(), typeof(CodeDomSerializer))!;
                var statements = serializer.SerializeMember(manager, component, TypeDescriptor.GetProperties(component)[property]!);
                Require(statements.OfType<CodeAssignStatement>().Any(), "Designer serialization emits " + property);
                foreach (CodeStatement statement in statements) provider.GenerateCodeFromStatement(statement, code, new CodeGeneratorOptions());
            }
            Require(serialization.Errors.Count == 0, "Designer serialization has no errors"); File.WriteAllText(Path.Combine(directory, "designer-members.cs.txt"), code.ToString());
            var resources = new ComponentResourceManager(typeof(DesignerForm));
            foreach (var (key, expected) in new[] { ("mouseModeOption.Icon", mouseModeOption.Icon), ("touchModeOption.Icon", touchModeOption.Icon), ("inputModeSelector.Icon", inputModeSelector.Icon) })
            {
                Require(resources.GetObject(key) is RibbonSvgIcon icon && expected is RibbonSvgIcon current && icon.Source == current.Source && icon.UseForegroundColor == current.UseForegroundColor,
                    "Compiled typed icon resource: " + key);
            }
            foreach (var icon in new[] { (RibbonSvgIcon)mouseModeOption.Icon!, (RibbonSvgIcon)touchModeOption.Icon! })
            {
                var converter = TypeDescriptor.GetConverter(icon); var restored = (RibbonSvgIcon)converter.ConvertFromInvariantString(converter.ConvertToInvariantString(icon)!)!;
                Require(restored.Source == icon.Source && restored.UseForegroundColor == icon.UseForegroundColor, "SVG resource converter round-trip");
            }
        }
    }

    private sealed class InputModePaint
    {
        internal Rectangle Qat, Selector, Header;
        internal readonly Dictionary<RibbonItem, Rectangle> Rows = [];
    }
    private sealed class InputModeRenderer(InputModePaint record, RibbonDropDownItem selector, RibbonQuickAccessDropDownReference reference) : RibbonOffice2024Renderer
    {
        public override void DrawQuickAccessItem(RibbonPaintContext c, RibbonElement item, bool menu) { if (!menu && ReferenceEquals(item, reference)) record.Qat = c.Bounds; base.DrawQuickAccessItem(c, item, menu); }
        public override void DrawItem(RibbonPaintContext c, RibbonItem item, RibbonItemSize size, bool menu) { if (menu) record.Rows[item] = c.Bounds; else if (ReferenceEquals(item, selector)) record.Selector = c.Bounds; base.DrawItem(c, item, size, menu); }
        public override void DrawMenuHeader(RibbonPaintContext c, string text) { record.Header = c.Bounds; base.DrawMenuHeader(c, text); }
    }
}
