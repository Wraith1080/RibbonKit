using System.IO.Enumeration;

namespace RibbonKit.VisualTests;

internal sealed class SnapshotSelection
{
    private readonly string[] _patterns;
    private readonly bool[] _matchedPatterns;
    private readonly List<string> _selectedScenes = new();

    public SnapshotSelection(string? filter)
    {
        _patterns = string.IsNullOrWhiteSpace(filter)
            ? Array.Empty<string>()
            : filter.Split(',').Select(pattern => pattern.Trim()).ToArray();
        if (_patterns.Any(string.IsNullOrEmpty))
            throw new ArgumentException("Visual scene selection contains an empty pattern.", nameof(filter));
        _matchedPatterns = new bool[_patterns.Length];
    }

    public IReadOnlyList<string> SelectedScenes => _selectedScenes;
    public bool IsFocused => _patterns.Length > 0;

    public bool Includes(string sceneName)
    {
        bool selected = _patterns.Length == 0;
        for (int index = 0; index < _patterns.Length; index++)
        {
            if (!FileSystemName.MatchesSimpleExpression(_patterns[index], sceneName, ignoreCase: true))
                continue;
            _matchedPatterns[index] = true;
            selected = true;
        }
        if (selected)
            _selectedScenes.Add(sceneName);
        return selected;
    }

    public void EnsureAllPatternsMatched()
    {
        string[] unmatched = _patterns.Where((_, index) => !_matchedPatterns[index]).ToArray();
        if (unmatched.Length > 0)
            throw new InvalidOperationException(
                "Visual scene selection matched no scenes for: " + string.Join(", ", unmatched) +
                ". Use snapshot names without the .png extension; * and ? are supported.");
        if (_selectedScenes.Count == 0)
            throw new InvalidOperationException("No visual scenes were selected.");
    }
}
