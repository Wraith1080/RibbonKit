using System.IO;

namespace RibbonKit.Writer.Pagination;

internal static class WriterPaginationDiagnosticOptions
{
    private const string EnvironmentVariable = "RIBBONKIT_WRITER_PAGINATED_DIAGNOSTIC";
    private const string TelemetryEnvironmentVariable =
        "RIBBONKIT_WRITER_PAGINATION_TELEMETRY";
    private static readonly object TelemetrySync = new();

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
