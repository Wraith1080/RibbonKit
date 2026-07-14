using System;
using System.Diagnostics;
using System.IO;

namespace RibbonKit.Design;

/// <summary>
/// Tiny append-only diagnostic log for the design-time tooling. The new XAML designer runs
/// extensions inside VS with no console and usually no attached debugger, so the reliable way
/// to see what happened is a file. Every entry also goes to <see cref="Debug"/>/DebugView.
/// </summary>
/// <remarks>
/// Log file: <c>%LOCALAPPDATA%\RibbonKit\DesignTools.log</c> (falls back to <c>%TEMP%</c>).
/// Logging never throws — a failure to write is swallowed. Development aid; gate or remove
/// before shipping.
/// </remarks>
internal static class DesignLog
{
    private static readonly object Gate = new object();
    private static string _path;

    /// <summary>Set to false to silence logging without removing the call sites.</summary>
    public static bool Enabled { get; set; } = true;

    /// <summary>The resolved log-file path (created lazily).</summary>
    public static string Path
    {
        get
        {
            if (_path != null)
            {
                return _path;
            }

            try
            {
                string dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RibbonKit");
                Directory.CreateDirectory(dir);
                _path = System.IO.Path.Combine(dir, "DesignTools.log");
            }
            catch
            {
                _path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RibbonKit.DesignTools.log");
            }

            return _path;
        }
    }

    /// <summary>Appends a timestamped line.</summary>
    public static void Write(string message)
    {
        if (!Enabled)
        {
            return;
        }

        Debug.WriteLine("[RibbonKit.Design] " + message);

        try
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "  " + message + Environment.NewLine;
            lock (Gate)
            {
                File.AppendAllText(Path, line);
            }
        }
        catch
        {
            // Logging must never break the tooling.
        }
    }

    /// <summary>Logs an exception with a short context tag and its full details.</summary>
    public static void Error(string context, Exception ex) =>
        Write("ERROR " + context + " :: " + (ex?.ToString() ?? "(null)"));
}
