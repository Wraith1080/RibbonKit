using System;
using System.IO;

namespace RibbonKit.Design;

/// <summary>
/// Minimal file logger. The new XAML designer runs extensions isolated and SWALLOWS exceptions
/// thrown inside providers, so a failed edit just looks like "nothing happened". This writes what
/// actually occurred (and any exception) to a temp log so failures are diagnosable without a debugger.
/// Remove once the providers are confirmed working.
/// </summary>
internal static class DesignLog
{
    private static readonly string LogPath =
        Path.Combine(Path.GetTempPath(), "RibbonKit.DesignTools.log");

    /// <summary>Runs <paramref name="body"/>, logging start/success and any exception (full details).</summary>
    public static void Run(string label, Action body)
    {
        try
        {
            Write(label + ": start");
            body();
            Write(label + ": ok");
        }
        catch (Exception ex)
        {
            Write(label + ": FAILED -> " + ex);
        }
    }

    /// <summary>Appends a line to the log; never throws (logging must not break the designer).</summary>
    public static void Write(string message)
    {
        try
        {
            File.AppendAllText(LogPath, "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + message + Environment.NewLine);
        }
        catch
        {
            // Ignore — diagnostics are best-effort.
        }
    }
}
