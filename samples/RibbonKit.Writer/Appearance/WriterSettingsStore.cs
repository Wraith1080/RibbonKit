using System.Text;
using System.IO;

namespace RibbonKit.Writer.Appearance;

internal sealed record WriterSettingsPaths(string AppearanceFile, string RibbonLayoutFile)
{
    public static WriterSettingsPaths CreateDefault()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RibbonKit",
            "Writer");
        return new WriterSettingsPaths(
            Path.Combine(folder, "appearance.json"),
            Path.Combine(folder, "ribbon-layout.json"));
    }
}

internal sealed class WriterSettingsStore
{
    private readonly WriterSettingsPaths _paths;

    public WriterSettingsStore(WriterSettingsPaths paths) =>
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));

    public WriterAppearancePreferences LoadAppearance()
    {
        if (!TryRead(_paths.AppearanceFile, out string? json)
            || !WriterAppearancePreferencesSerializer.TryDeserialize(json, out var preferences))
        {
            return new WriterAppearancePreferences();
        }

        return preferences;
    }

    public string? LoadRibbonLayout() =>
        TryRead(_paths.RibbonLayoutFile, out string? json) ? json : null;

    public bool SaveAppearance(WriterAppearancePreferences preferences) =>
        TryWriteAtomic(
            _paths.AppearanceFile,
            WriterAppearancePreferencesSerializer.Serialize(preferences));

    public bool SaveRibbonLayout(string layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        return TryWriteAtomic(_paths.RibbonLayoutFile, layout);
    }

    private static bool TryRead(string path, out string? value)
    {
        value = null;
        try
        {
            if (!File.Exists(path))
                return false;

            value = File.ReadAllText(path, Encoding.UTF8);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool TryWriteAtomic(string path, string value)
    {
        string? directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
            return false;

        string temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(temporary, value, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (IOException)
        {
            TryDeleteTemporary(temporary);
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            TryDeleteTemporary(temporary);
            return false;
        }
    }

    private static void TryDeleteTemporary(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
