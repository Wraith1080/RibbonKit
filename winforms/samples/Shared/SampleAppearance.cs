using RibbonKit.WinForms;

namespace RibbonKit.WinForms.Samples;

// Host preference glue only. All rendering, theme validation and window behavior
// stay in RibbonKit's existing public APIs. No controls/models are owned here.
internal sealed class SampleAppearance(RibbonControl ribbon, RibbonForm form)
{
    private RibbonTheme _applicationTheme = ribbon.BuiltInTheme is null ? ribbon.Theme : RibbonThemes.Office2024Light;
    private RibbonTheme _appliedTheme = ribbon.Theme;
    private RibbonBuiltInTheme? _appliedBuiltIn = ribbon.BuiltInTheme;
    private Color _uncoloredTitle = form.TitleBarColor;
    internal RibbonTheme BaseTheme { get; private set; } = ribbon.Theme;
    internal RibbonBuiltInTheme? SelectedTheme { get; private set; } = ribbon.BuiltInTheme;
    internal Color? AccentOverride { get; private set; }
    internal bool ColorTitleBar { get; private set; }
    internal Color EffectiveAccent => AccentOverride ?? BaseTheme.Accent;

    internal void Synchronize()
    {
        if (!ColorTitleBar) _uncoloredTitle = form.TitleBarColor;
        if (ReferenceEquals(ribbon.Theme, _appliedTheme) && ribbon.BuiltInTheme == _appliedBuiltIn) return;
        var incoming = ribbon.Theme;
        if (ColorTitleBar && incoming.TitleBarForeground == Color.White) incoming = incoming with { TitleBarForeground = BaseTheme.TitleBarForeground };
        if (AccentOverride is { } accent && incoming.Accent == accent) incoming = incoming with { Accent = BaseTheme.Accent };
        else AccentOverride = null;
        BaseTheme = incoming; SelectedTheme = ribbon.BuiltInTheme;
        if (SelectedTheme is null) _applicationTheme = incoming;
        _appliedTheme = ribbon.Theme; _appliedBuiltIn = ribbon.BuiltInTheme;
    }

    internal void SelectTheme(RibbonBuiltInTheme? selected)
    {
        Synchronize();
        BaseTheme = selected switch
        {
            RibbonBuiltInTheme.Office2013Light => RibbonThemes.Office2013Light,
            RibbonBuiltInTheme.Office2019Light => RibbonThemes.Office2019Light,
            RibbonBuiltInTheme.Office2024Light => RibbonThemes.Office2024Light,
            null => _applicationTheme,
            _ => throw new ArgumentOutOfRangeException(nameof(selected))
        };
        SelectedTheme = selected; AccentOverride = null; Apply();
    }

    internal void SelectAccent(Color? accent)
    {
        if (accent is { A: not 255 }) throw new ArgumentException("Choose an opaque accent.", nameof(accent));
        Synchronize(); AccentOverride = accent; Apply();
    }

    internal void SetColorTitleBar(bool enabled)
    {
        Synchronize();
        if (ColorTitleBar == enabled) return;
        ColorTitleBar = enabled; Apply();
    }

    private void Apply()
    {
        ribbon.BeginUpdate();
        try
        {
            if (AccentOverride is null && !ColorTitleBar && SelectedTheme is { } selected)
            {
                ribbon.Theme = _applicationTheme;
                ribbon.BuiltInTheme = selected;
            }
            else
            {
                var next = AccentOverride is { } accent ? BaseTheme with { Accent = accent } : BaseTheme;
                if (ColorTitleBar) next = next with { TitleBarForeground = Color.White };
                ribbon.Theme = next;
            }
            form.TitleBarColor = ColorTitleBar ? EffectiveAccent : _uncoloredTitle;
        }
        finally { ribbon.EndUpdate(); }
        _appliedTheme = ribbon.Theme; _appliedBuiltIn = ribbon.BuiltInTheme;
    }
}
