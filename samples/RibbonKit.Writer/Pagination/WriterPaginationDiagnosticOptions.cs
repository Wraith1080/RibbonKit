namespace RibbonKit.Writer.Pagination;

internal static class WriterPaginationDiagnosticOptions
{
    private const string EnvironmentVariable = "RIBBONKIT_WRITER_PAGINATED_DIAGNOSTIC";

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

    internal static bool ShouldRunStressBurst =>
        Environment.GetCommandLineArgs().Any(argument =>
            string.Equals(argument, "--writer-pagination-stress-burst",
                StringComparison.OrdinalIgnoreCase));
}
