using System.IO;

namespace RibbonKit.Writer.Pagination;

internal static class WriterPaginationDiagnosticOptions
{
    private const string EnvironmentVariable = "RIBBONKIT_WRITER_PAGINATED_DIAGNOSTIC";
    private const string TelemetryEnvironmentVariable =
        "RIBBONKIT_WRITER_PAGINATION_TELEMETRY";
    private static readonly object TelemetrySync = new();

    internal static (int Pages, long Bytes) CacheBudget =>
        ParseCacheBudget(Environment.GetCommandLineArgs());

    internal static (int Pages, long Bytes) ParseCacheBudget(IEnumerable<string> arguments)
    {
        var pages = WriterDedicatedPaginationEngine.DefaultPageCacheLimit;
        var bytes = WriterDedicatedPaginationEngine.DefaultCacheByteLimit;
        foreach (var argument in arguments)
        {
            const string pagesPrefix = "--writer-pagination-cache-pages=";
            const string mbPrefix = "--writer-pagination-cache-mb=";
            if (argument.StartsWith(pagesPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(argument[pagesPrefix.Length..], out pages) || pages < 1 || pages > 8)
                    throw new ArgumentException("Diagnostic cache pages must be between 1 and 8.");
            }
            if (argument.StartsWith(mbPrefix, StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(argument[mbPrefix.Length..], out var mb) || mb < 1 || mb > 64)
                    throw new ArgumentException("Diagnostic cache MB must be between 1 and 64.");
                bytes = mb * 1024L * 1024;
            }
        }
        return (pages, bytes);
    }

    internal static string? ProbeDocumentPath => Environment.GetCommandLineArgs()
        .FirstOrDefault(argument => argument.StartsWith("--writer-pagination-document=",
            StringComparison.OrdinalIgnoreCase))?["--writer-pagination-document=".Length..];

    internal static bool ShouldSeedLongParagraphDocument => Environment.GetCommandLineArgs()
        .Contains("--writer-pagination-long-paragraph-seed", StringComparer.OrdinalIgnoreCase);

    internal static bool IsEnabled =>
        Environment.GetCommandLineArgs().Any(argument =>
            string.Equals(argument, "--writer-paginated-diagnostic",
                StringComparison.OrdinalIgnoreCase)) ||
        string.Equals(Environment.GetEnvironmentVariable(EnvironmentVariable), "1",
            StringComparison.Ordinal);

    internal static bool ShouldSeedDocument =>
        Environment.GetCommandLineArgs().Any(argument =>
            string.Equals(argument, "--writer-pagination-seed",
                StringComparison.OrdinalIgnoreCase));

    internal static bool ShouldSeedStructuralTableDocument =>
        Environment.GetCommandLineArgs().Any(argument =>
            string.Equals(argument, "--writer-pagination-structural-seed",
                StringComparison.OrdinalIgnoreCase));

    internal static bool ShouldSeedStressDocument =>
        Environment.GetCommandLineArgs().Any(argument =>
            string.Equals(argument, "--writer-pagination-stress-seed",
                StringComparison.OrdinalIgnoreCase));

    internal static bool ShouldSeedMixedStressDocument =>
        Environment.GetCommandLineArgs().Any(argument =>
            string.Equals(argument, "--writer-pagination-mixed-stress-seed",
                StringComparison.OrdinalIgnoreCase));

    internal static bool ShouldRunStressBurst =>
        Environment.GetCommandLineArgs().Any(argument =>
            string.Equals(argument, "--writer-pagination-stress-burst",
                StringComparison.OrdinalIgnoreCase));

    internal static bool ShouldRunScrollProbe =>
        Environment.GetCommandLineArgs().Any(argument =>
            string.Equals(argument, "--writer-pagination-scroll-probe",
                StringComparison.OrdinalIgnoreCase));

    internal static bool ShouldExitAfterProbe =>
        Environment.GetCommandLineArgs().Any(argument =>
            string.Equals(argument, "--writer-pagination-exit-after-probe",
                StringComparison.OrdinalIgnoreCase));

    internal static int StressBlockCount
    {
        get
        {
            const string prefix = "--writer-pagination-stress-blocks=";
            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !int.TryParse(argument[prefix.Length..], out var count))
                    continue;
                return Math.Clamp(count, 1, 2000);
            }
            return 120;
        }
    }

    internal static int ScrollProbeCycles
    {
        get
        {
            const string prefix = "--writer-pagination-scroll-cycles=";
            foreach (var argument in Environment.GetCommandLineArgs())
            {
                if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !int.TryParse(argument[prefix.Length..], out var count))
                    continue;
                return Math.Clamp(count, 1, 10);
            }
            return 1;
        }
    }

    internal static void WriteTelemetry(string status)
    {
        var path = Environment.GetEnvironmentVariable(TelemetryEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(path))
            return;
        try
        {
            lock (TelemetrySync)
            {
                File.AppendAllText(path,
                    $"{DateTimeOffset.UtcNow:O}\t{Environment.ProcessId}\t{status}" +
                    Environment.NewLine);
            }
        }
        catch (Exception exception) when (exception is IOException or
            UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            // This optional observer must never alter the diagnostic path it measures.
        }
    }
}
