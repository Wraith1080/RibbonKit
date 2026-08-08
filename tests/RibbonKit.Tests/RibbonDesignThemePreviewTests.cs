using System;
using System.ComponentModel;
using System.IO;
using RibbonKit.Controls;
using RibbonKit.Theming;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>Guards the Ribbon Editor's design-only theme-token scope.</summary>
public class RibbonDesignThemePreviewTests
{
    [Fact]
    public void Theme_preview_is_inert_at_runtime() => Sta.Run(() =>
    {
        var ribbon = new Ribbon
        {
            DesignPreviewTheme = (int)RibbonTheme.Office2007,
        };

        Assert.Empty(ribbon.Resources.MergedDictionaries);
    });

    [Fact]
    public void Theme_preview_scopes_and_replaces_token_dictionaries_only_in_design_mode() =>
        Sta.Run(() =>
        {
            var ribbon = new Ribbon();
            DesignerProperties.SetIsInDesignMode(ribbon, true);

            ribbon.DesignPreviewTheme = (int)RibbonTheme.Office2007;
            var office2007 = Assert.Single(ribbon.Resources.MergedDictionaries);
            Assert.EndsWith(
                "/RibbonKit;component/Themes/Tokens.Office2007.xaml",
                office2007.Source.OriginalString,
                StringComparison.Ordinal);
            Assert.Equal(
                new System.Windows.Thickness(2d, 2d, 2d, 0d),
                ribbon.TryFindResource("RibbonKit.Metrics.ApplicationButtonMargin"));

            ribbon.DesignPreviewTheme = (int)RibbonTheme.Office2019;
            var office2019 = Assert.Single(ribbon.Resources.MergedDictionaries);
            Assert.NotSame(office2007, office2019);
            Assert.EndsWith(
                "/RibbonKit;component/Themes/Tokens.Office2019.xaml",
                office2019.Source.OriginalString,
                StringComparison.Ordinal);
            Assert.Equal(
                new System.Windows.Thickness(8d, 4d, 2d, 0d),
                ribbon.TryFindResource("RibbonKit.Metrics.ApplicationButtonMargin"));

            ribbon.DesignPreviewTheme = -1;
            Assert.Empty(ribbon.Resources.MergedDictionaries);
        });

    [Fact]
    public void Editor_preview_tab_wires_the_theme_selector_to_the_value_provider()
    {
        string repository = RepositoryRoot();
        string editor = File.ReadAllText(Path.Combine(
            repository,
            "src",
            "RibbonKit.Design",
            "RibbonEditorWindow.cs"));
        string provider = File.ReadAllText(Path.Combine(
            repository,
            "src",
            "RibbonKit.Design",
            "TabPreview.cs"));

        Assert.Contains("BuildPreviewRow(\"Theme\", _themeCombo)", editor, StringComparison.Ordinal);
        Assert.Contains("TabPreviewCoordinator.SetTheme(_ribbon, theme)", editor, StringComparison.Ordinal);
        Assert.Contains(
            "Properties.Add(new TypeIdentifier(RibbonType), \"DesignPreviewTheme\")",
            provider,
            StringComparison.Ordinal);
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }
}
