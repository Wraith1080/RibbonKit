using RibbonKit.WinForms.Showcase.Persistence;
using Xunit;

namespace RibbonKit.WinForms.Tests;

public sealed class ShowcasePersistenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "RibbonKit-Persistence-" + Guid.NewGuid().ToString("N"));
    private string SettingsPath => Path.Combine(_directory, "preferences.json");

    [Fact]
    public void Atomic_replacement_ignores_interrupted_temporary_files_and_leaves_no_new_temporary_file()
    {
        var store = new ShowcaseSettingsFile(SettingsPath);
        Assert.Null(store.Load(_ => throw new Exception("Missing file should not restore.")));
        Assert.Null(store.Save(new ShowcasePreferences { WordWrap = false }.ToJson()));
        File.WriteAllText(SettingsPath + ".interrupted.tmp", "partial");
        var restart = new ShowcaseSettingsFile(SettingsPath);
        ShowcasePreferences? loaded = null;
        Assert.Null(restart.Load(json => loaded = ShowcasePreferences.FromJson(json)));
        Assert.False(loaded!.WordWrap);
        Assert.Null(restart.Save(new ShowcasePreferences { Density = RibbonDensity.Touch }.ToJson()));
        Assert.Equal(RibbonDensity.Touch, ShowcasePreferences.FromJson(File.ReadAllText(SettingsPath)).Density);
        Assert.Single(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"format\":\"foreign\",\"version\":1}")]
    [InlineData("{\"format\":\"RibbonKit.WinForms.Showcase.Preferences\",\"version\":2}")]
    [InlineData("{\"format\":\"RibbonKit.WinForms.Showcase.Preferences\",\"version\":1,\"density\":\"Future\"}")]
    [InlineData("{\"format\":\"RibbonKit.WinForms.Showcase.Preferences\",\"version\":1,\"density\":1}")]
    [InlineData("{\"format\":\"RibbonKit.WinForms.Showcase.Preferences\",\"version\":1,\"window\":{\"x\":0,\"y\":0,\"width\":0,\"height\":100,\"dpi\":96,\"maximized\":false}}")]
    public void Rejected_input_uses_defaults_and_is_never_overwritten(string json)
    {
        Directory.CreateDirectory(_directory); File.WriteAllText(SettingsPath, json);
        var store = new ShowcaseSettingsFile(SettingsPath);
        Assert.NotNull(store.Load(text => ShowcasePreferences.FromJson(text)));
        Assert.NotNull(store.Save(new ShowcasePreferences().ToJson()));
        Assert.Equal(json, File.ReadAllText(SettingsPath));
        var other = new ShowcaseSettingsFile(Path.Combine(_directory, "other.json"));
        Assert.Null(other.Save("valid independent file"));
    }

    [Fact]
    public void Oversized_and_unwritable_files_do_not_crash_or_replace_existing_data()
    {
        Directory.CreateDirectory(_directory); File.WriteAllText(SettingsPath, new string(' ', ShowcaseSettingsFile.MaximumBytes + 1));
        var store = new ShowcaseSettingsFile(SettingsPath);
        Assert.NotNull(store.Load(_ => throw new Exception("Oversized file reached restore.")));
        Assert.NotNull(store.Save("replacement"));
        Assert.Equal(ShowcaseSettingsFile.MaximumBytes + 1, new FileInfo(SettingsPath).Length);
        Directory.CreateDirectory(Path.Combine(_directory, "blocked.json"));
        var blocked = new ShowcaseSettingsFile(Path.Combine(_directory, "blocked.json"));
        Assert.NotNull(blocked.Save("new"));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void Window_restore_scales_size_and_clamps_removed_monitor_coordinates_to_working_area()
    {
        var work = new Rectangle(-1920, 0, 1920, 1040);
        var removed = new ShowcaseWindowPlacement(6000, -500, 1000, 700, 96, true);
        var fit = removed.Fit(work, new(240, 300), 144);
        Assert.Equal(new Size(1500, 1040), fit.Size);
        Assert.True(work.Contains(fit));
        var tiny = new ShowcaseWindowPlacement(-1900, 30, 20, 20, 144, false).Fit(work, new(240, 300), 96);
        Assert.Equal(new Size(240, 300), tiny.Size);
    }

    public void Dispose()
    {
        string target = Path.GetFullPath(_directory), temporaryRoot = Path.GetFullPath(Path.GetTempPath());
        if (!target.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase)
            || !Path.GetFileName(target).StartsWith("RibbonKit-Persistence-", StringComparison.Ordinal))
            throw new InvalidOperationException("Refusing to remove a directory outside this test's temporary root.");
        if (Directory.Exists(target)) Directory.Delete(target, true);
    }
}
