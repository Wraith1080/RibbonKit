using RibbonKit.Writer.Models;

namespace RibbonKit.Writer.Services.Documents;

/// <summary>Features that may be lost when saving a document in another profile.</summary>
[Flags]
public enum WriterDocumentDataLoss
{
    /// <summary>No known loss.</summary>
    None = 0,

    /// <summary>Character or paragraph formatting may be lost.</summary>
    Formatting = 1 << 0,

    /// <summary>Embedded images may be lost.</summary>
    Images = 1 << 1,

    /// <summary>Embedded hyperlinks may be lost.</summary>
    Hyperlinks = 1 << 2,

    /// <summary>Tables may be lost.</summary>
    Tables = 1 << 3,

    /// <summary>Writer page settings may be lost.</summary>
    PageSettings = 1 << 4
}

/// <summary>How the source and target profile capability sets relate.</summary>
public enum WriterDocumentFormatTransitionKind
{
    /// <summary>The source and target profile are the same.</summary>
    Same = 0,

    /// <summary>The target profile is a strict superset of the source profile.</summary>
    Upgrade = 1,

    /// <summary>The target profile is a strict subset of the source profile.</summary>
    Downgrade = 2,

    /// <summary>The profiles have incomparable capability sets.</summary>
    CrossGrade = 3
}

/// <summary>One centralized analysis of a document format transition.</summary>
public sealed record WriterDocumentFormatTransition
{
    internal WriterDocumentFormatTransition(
        WriterDocumentProfile sourceProfile,
        WriterDocumentProfile targetProfile,
        WriterDocumentFormatTransitionKind kind,
        WriterDocumentDataLoss losses,
        IReadOnlyList<string> lossDescriptions,
        string? warningMessage)
    {
        SourceProfile = sourceProfile;
        TargetProfile = targetProfile;
        Kind = kind;
        Losses = losses;
        LossDescriptions = lossDescriptions;
        WarningMessage = warningMessage;
    }

    /// <summary>Gets the current profile.</summary>
    public WriterDocumentProfile SourceProfile { get; }

    /// <summary>Gets the requested destination profile.</summary>
    public WriterDocumentProfile TargetProfile { get; }

    /// <summary>Gets the current format identity.</summary>
    public WriterDocumentFormat SourceFormat => SourceProfile.Format;

    /// <summary>Gets the destination format identity.</summary>
    public WriterDocumentFormat TargetFormat => TargetProfile.Format;

    /// <summary>Gets the ordered transition kind.</summary>
    public WriterDocumentFormatTransitionKind Kind { get; }

    /// <summary>Whether the target is the same profile.</summary>
    public bool IsSameFormat => Kind == WriterDocumentFormatTransitionKind.Same;

    /// <summary>Whether the target is a strict capability superset.</summary>
    public bool IsUpgrade => Kind == WriterDocumentFormatTransitionKind.Upgrade;

    /// <summary>Whether the target is a strict capability subset.</summary>
    public bool IsDowngrade => Kind == WriterDocumentFormatTransitionKind.Downgrade;

    /// <summary>Whether the source and target capability sets are incomparable.</summary>
    public bool IsCrossGrade => Kind == WriterDocumentFormatTransitionKind.CrossGrade;

    /// <summary>Gets the features that the target persistence format cannot preserve.</summary>
    public WriterDocumentDataLoss Losses { get; }

    /// <summary>Alias for <see cref="Losses"/>.</summary>
    public WriterDocumentDataLoss DataLoss => Losses;

    /// <summary>Gets the stable human-readable names of the possible losses.</summary>
    public IReadOnlyList<string> LossDescriptions { get; }

    /// <summary>Whether an explicit user decision is required before persistence.</summary>
    public bool RequiresConfirmation => Losses != WriterDocumentDataLoss.None;

    /// <summary>Whether the declared profile capabilities identify no known loss.</summary>
    public bool IsLossless => !RequiresConfirmation;

    /// <summary>
    /// Gets the warning to present before a lower-fidelity save, or <see langword="null"/> when no warning is needed.
    /// </summary>
    public string? WarningMessage { get; }
}

/// <summary>Central policy for comparing the fidelity of Writer profiles.</summary>
public sealed class WriterDocumentFormatTransitionPolicy
{
    private static readonly string[] NoLossDescriptions = Array.Empty<string>();

    /// <summary>The process-wide policy for the canonical Writer profiles.</summary>
    public static WriterDocumentFormatTransitionPolicy Default { get; } = new();

    /// <summary>Analyzes a transition from one format identity to another.</summary>
    public WriterDocumentFormatTransition Evaluate(
        WriterDocumentFormat sourceFormat,
        WriterDocumentFormat targetFormat)
    {
        var source = WriterDocumentProfiles.ForFormat(sourceFormat);
        var target = WriterDocumentProfiles.ForFormat(targetFormat);
        var kind = CompareCapabilities(source, target);
        var losses = GetLosses(source, target);
        var descriptions = GetLossDescriptions(losses);
        var warning = descriptions.Count == 0
            ? null
            : $"Saving as {target.DisplayName} will not preserve {JoinDescriptions(descriptions)}.";
        return new WriterDocumentFormatTransition(source, target, kind, losses, descriptions, warning);
    }

    /// <summary>
    /// Analyzes a transition from a live document's profile identity to another format identity.
    /// This is profile-level analysis; it does not inspect the document tree for undeclared content.
    /// </summary>
    public WriterDocumentFormatTransition Evaluate(WriterDocument document, WriterDocumentFormat targetFormat)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Evaluate(document.Format, targetFormat);
    }

    /// <summary>Alias for <see cref="Evaluate(WriterDocumentFormat, WriterDocumentFormat)"/>.</summary>
    public WriterDocumentFormatTransition GetTransition(
        WriterDocumentFormat sourceFormat,
        WriterDocumentFormat targetFormat) => Evaluate(sourceFormat, targetFormat);

    /// <summary>Analyzes a transition using a descriptive method name for command projections.</summary>
    public WriterDocumentFormatTransition Analyze(
        WriterDocumentFormat sourceFormat,
        WriterDocumentFormat targetFormat) => Evaluate(sourceFormat, targetFormat);

    private static WriterDocumentFormatTransitionKind CompareCapabilities(
        WriterDocumentProfile source,
        WriterDocumentProfile target)
    {
        var sourceSet = GetCapabilitySet(source);
        var targetSet = GetCapabilitySet(target);
        if (source.Format == target.Format)
            return WriterDocumentFormatTransitionKind.Same;
        if (sourceSet == targetSet)
            return WriterDocumentFormatTransitionKind.CrossGrade;
        if ((targetSet & sourceSet) == sourceSet)
            return WriterDocumentFormatTransitionKind.Upgrade;
        if ((sourceSet & targetSet) == targetSet)
            return WriterDocumentFormatTransitionKind.Downgrade;
        return WriterDocumentFormatTransitionKind.CrossGrade;
    }

    private static ulong GetCapabilitySet(WriterDocumentProfile profile) =>
        (ulong)profile.Capabilities.Content
        | ((ulong)profile.Capabilities.PageMetadata << 16);

    private static WriterDocumentDataLoss GetLosses(
        WriterDocumentProfile source,
        WriterDocumentProfile target)
    {
        var losses = WriterDocumentDataLoss.None;
        if (source.Capabilities.Persistence.PreservesFormatting
            && !target.Capabilities.Persistence.PreservesFormatting)
            losses |= WriterDocumentDataLoss.Formatting;
        if (source.Capabilities.Persistence.PreservesImages
            && !target.Capabilities.Persistence.PreservesImages)
            losses |= WriterDocumentDataLoss.Images;
        if (source.Capabilities.Persistence.PreservesHyperlinks
            && !target.Capabilities.Persistence.PreservesHyperlinks)
            losses |= WriterDocumentDataLoss.Hyperlinks;
        if (source.Capabilities.Persistence.PreservesTables
            && !target.Capabilities.Persistence.PreservesTables)
            losses |= WriterDocumentDataLoss.Tables;
        if (source.Capabilities.Persistence.PreservesPageSettings
            && !target.Capabilities.Persistence.PreservesPageSettings)
            losses |= WriterDocumentDataLoss.PageSettings;
        return losses;
    }

    private static IReadOnlyList<string> GetLossDescriptions(WriterDocumentDataLoss losses)
    {
        if (losses == WriterDocumentDataLoss.None)
            return NoLossDescriptions;

        var descriptions = new List<string>(4);
        if ((losses & WriterDocumentDataLoss.Formatting) != 0)
            descriptions.Add("formatting");
        if ((losses & WriterDocumentDataLoss.Images) != 0)
            descriptions.Add("images");
        if ((losses & WriterDocumentDataLoss.Hyperlinks) != 0)
            descriptions.Add("hyperlinks");
        if ((losses & WriterDocumentDataLoss.Tables) != 0)
            descriptions.Add("tables");
        if ((losses & WriterDocumentDataLoss.PageSettings) != 0)
            descriptions.Add("page settings");
        return descriptions;
    }

    private static string JoinDescriptions(IReadOnlyList<string> descriptions) => descriptions.Count switch
    {
        0 => string.Empty,
        1 => descriptions[0],
        2 => $"{descriptions[0]} and {descriptions[1]}",
        _ => string.Join(", ", descriptions.Take(descriptions.Count - 1))
             + $", and {descriptions[^1]}"
    };
}

/// <summary>The user's response to a fidelity warning.</summary>
public enum WriterFormatTransitionDecision
{
    /// <summary>Do not persist the conversion.</summary>
    Cancel = 0,

    /// <summary>Allow the conversion to continue.</summary>
    Continue = 1
}

/// <summary>Obtains an explicit decision before a potentially lossy format transition.</summary>
public interface IWriterFormatTransitionDecider
{
    /// <summary>Chooses whether the supplied transition may be persisted.</summary>
    Task<WriterFormatTransitionDecision> DecideAsync(
        WriterDocument document,
        WriterDocumentFormatTransition transition,
        CancellationToken cancellationToken);
}
