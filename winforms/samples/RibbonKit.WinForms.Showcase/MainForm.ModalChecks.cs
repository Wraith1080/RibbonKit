namespace RibbonKit.WinForms.Showcase;

public partial class MainForm
{
    // Runs without Application.Run/Show/Activate or input injection. It checks
    // the actual consumer hierarchy/layout, not a copy of the sample's controls.
    internal void CheckHiddenModalLayout(string directory)
    {
        Directory.CreateDirectory(directory); List<string> log = [];
        if (Visible) throw new InvalidOperationException("The layout check requires a hidden form.");
        _ = Handle; _ = ribbon.Handle;
        var launch = ribbon.Tabs.Single(t => t.Id == "tab.merging").Groups.Single(g => g.Id == "group.modalPreview").Items.Single();
        foreach (var material in Enum.GetValues<RibbonWindowBackdrop>())
        foreach (var density in Enum.GetValues<RibbonDensity>())
        foreach (bool minimized in new[] { false, true })
        {
            Backdrop = material; ribbon.Density = density; ribbon.IsMinimized = minimized; PerformLayout();
            launch.PerformClick();
            if (!ribbon.IsModal) throw new InvalidOperationException("The actual Print Preview command did not enter modal mode.");
            PerformLayout();
            if (_printPreview is not { } preview || !ReferenceEquals(preview.Parent, document.Parent))
                throw new InvalidOperationException("Preview escaped the document's opaque content host.");
            // Invisible native children may be parked at unrelated screen
            // coordinates. Compare the actual managed layout in one form space.
            var area = InForm(preview);
            var header = InForm(ribbon);
            var statusArea = InForm(statusStrip);
            if (area.Width <= 0 || area.Height <= 0 || Rectangle.Intersect(area, header).Height > 0 || Rectangle.Intersect(area, statusArea).Height > 0)
                throw new InvalidOperationException($"Preview overlaps ribbon/status geometry: preview={area}; ribbon={header}; status={statusArea}.");
            if (Visible) throw new InvalidOperationException("The hidden layout check showed its form.");
            ribbon.ExitModal(); PerformLayout();
            if (ribbon.IsModal || ribbon.IsMinimized != minimized) throw new InvalidOperationException("Print Preview did not exit with its prior minimized state.");
            log.Add($"PASS: {material}/{density}/minimized={minimized}: actual preview command/exit; body-only bounds={area}; hidden={ !Visible }");
        }
        File.WriteAllLines(Path.Combine(directory, "result.log"), log);

        Rectangle InForm(Control control)
        {
            Point location = Point.Empty; Control? current = control;
            for (; current is not null && !ReferenceEquals(current, this); current = current.Parent) location.Offset(current.Location);
            if (current is null) throw new InvalidOperationException("Preview control left its application form.");
            return new(location, control.ClientSize);
        }
    }
}
