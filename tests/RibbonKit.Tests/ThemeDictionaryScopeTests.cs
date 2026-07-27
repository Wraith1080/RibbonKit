using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace RibbonKit.Tests;

/// <summary>
/// Guards the Controls.*.xaml split of Themes/Office2024.xaml (04-DESIGN-NOTES.md §3.37).
///
/// Every rule checked here fails at RUNTIME in WPF - or, worse, silently - so it is checked at
/// build time instead. This is pure XML analysis: no WPF types are constructed, no Application is
/// started, so it runs headless on a CI agent.
///
/// Known limitation: keys are tracked per FILE, not per resource scope, so a key declared inside a
/// nested &lt;Style.Resources&gt; counts as visible to the whole file. That makes the check
/// permissive rather than noisy - it can miss an exotic scoping bug, but it will not fail a build
/// over a legal one.
/// </summary>
public class ThemeDictionaryScopeTests
{
    private const string AggregatorName = "Office2024.xaml";
    private const string PartSearchPattern = "Controls.*.xaml";

    private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";

    private static readonly Regex StaticRefPattern =
        new(@"\{StaticResource\s+([^}\s]+)\s*\}", RegexOptions.Compiled);

    private static readonly Regex KeyPattern =
        new(@"x:Key\s*=\s*""([^""]+)""", RegexOptions.Compiled);

    private static readonly Regex MergedSourcePattern =
        new(@"Source\s*=\s*""[^""]*?/Themes/([^""/]+\.xaml)""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex BasedOnPattern =
        new(@"BasedOn\s*=\s*""\{StaticResource\s+([^}\s]+)\s*\}""", RegexOptions.Compiled);

    // ---------------------------------------------------------------- rule 1

    [Fact]
    public void Every_StaticResource_reference_resolves_in_its_own_scope()
    {
        var parts = LoadParts();
        var problems = new List<string>();

        foreach (var part in parts.Values.OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            var visible = VisibleKeys(part, parts);

            foreach (Match match in StaticRefPattern.Matches(part.StrippedText))
            {
                var key = match.Groups[1].Value;
                var line = LineOf(part.StrippedText, match.Index);

                if (!visible.Contains(key))
                {
                    problems.Add(
                        $"{part.Name}({line}): '{key}' is not defined in this file nor in any " +
                        "dictionary it merges. A StaticResource does NOT resolve against sibling " +
                        "dictionaries merged by the same parent - merge the defining file here, " +
                        "inside this file's ResourceDictionary.MergedDictionaries.");
                }
                else if (part.Keys.TryGetValue(key, out var declaredAt) && declaredAt > line)
                {
                    problems.Add(
                        $"{part.Name}({line}): '{key}' is used before it is declared on line " +
                        $"{declaredAt}. StaticResource cannot forward-reference within a dictionary.");
                }
            }
        }

        Assert.True(problems.Count == 0, Report("Unresolvable StaticResource reference(s)", problems));
    }

    // ---------------------------------------------------------------- rule 2

    [Fact]
    public void BasedOn_chains_stay_inside_a_single_part()
    {
        var parts = LoadParts();
        var problems = new List<string>();

        foreach (var part in parts.Values.OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            foreach (Match match in BasedOnPattern.Matches(part.StrippedText))
            {
                var key = match.Groups[1].Value;

                if (!part.Keys.ContainsKey(key))
                {
                    problems.Add(
                        $"{part.Name}({LineOf(part.StrippedText, match.Index)}): BasedOn '{key}' " +
                        "crosses a file boundary. The XAML designer handles that badly - keep the " +
                        "base style and its derivatives in the same part.");
                }
            }
        }

        Assert.True(problems.Count == 0, Report("Cross-file BasedOn chain(s)", problems));
    }

    // ---------------------------------------------------------------- rule 3

    [Fact]
    public void Every_part_on_disk_is_merged_by_the_aggregator()
    {
        var themes = ThemesDirectory();
        var aggregator = StripComments(File.ReadAllText(Path.Combine(themes, AggregatorName)));

        var merged = MergedSourcePattern.Matches(aggregator)
            .Select(m => m.Groups[1].Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var onDisk = Directory.GetFiles(themes, PartSearchPattern)
            .Select(path => Path.GetFileName(path))
            .ToList();

        Assert.True(onDisk.Count > 0, $"No {PartSearchPattern} found in {themes}.");

        var orphans = onDisk.Where(f => !merged.Contains(f)).OrderBy(f => f, StringComparer.Ordinal);

        Assert.True(!orphans.Any(), Report(
            $"Part(s) not listed in {AggregatorName}",
            orphans.Select(f =>
                $"{f}: exists but is never merged, so its styles are silently absent at runtime. " +
                $"Add a <ResourceDictionary Source=\".../Themes/{f}\" /> line to {AggregatorName}.")));
    }

    // ---------------------------------------------------------------- rule 4

    [Fact]
    public void No_implicit_style_is_declared_by_two_parts()
    {
        var owners = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var part in LoadParts().Values)
        {
            var root = XDocument.Parse(part.RawText).Root;
            if (root is null) continue;

            foreach (var element in root.Elements())
            {
                if (element.Name.LocalName != "Style") continue;
                if (element.Attribute(Xaml + "Key") is not null) continue;

                var targetType = element.Attribute("TargetType")?.Value;
                if (string.IsNullOrWhiteSpace(targetType)) continue;

                if (!owners.TryGetValue(targetType, out var list))
                {
                    owners[targetType] = list = new List<string>();
                }

                list.Add(part.Name);
            }
        }

        var clashes = owners
            .Where(pair => pair.Value.Count > 1)
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair =>
                $"{pair.Key}: declared implicitly by {string.Join(" and ", pair.Value)}. " +
                "The last-merged one silently wins and the other is dead code.");

        Assert.True(!clashes.Any(), Report("Duplicate implicit style(s)", clashes));
    }

    // ---------------------------------------------------------------- helpers

    private static Dictionary<string, ThemePart> LoadParts()
    {
        var themes = ThemesDirectory();
        var parts = new Dictionary<string, ThemePart>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.GetFiles(themes, PartSearchPattern).OrderBy(p => p, StringComparer.Ordinal))
        {
            var raw = File.ReadAllText(path);
            var stripped = StripComments(raw);

            var keys = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Match match in KeyPattern.Matches(stripped))
            {
                var key = match.Groups[1].Value;
                var line = LineOf(stripped, match.Index);
                if (!keys.ContainsKey(key) || keys[key] > line) keys[key] = line;
            }

            var name = Path.GetFileName(path);
            parts[name] = new ThemePart(
                name,
                raw,
                stripped,
                keys,
                MergedSourcePattern.Matches(stripped).Select(m => m.Groups[1].Value).ToList());
        }

        return parts;
    }

    private static HashSet<string> VisibleKeys(ThemePart part, IReadOnlyDictionary<string, ThemePart> all) =>
        VisibleKeys(part, all, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private static HashSet<string> VisibleKeys(
        ThemePart part,
        IReadOnlyDictionary<string, ThemePart> all,
        HashSet<string> seen)
    {
        var visible = new HashSet<string>(part.Keys.Keys, StringComparer.Ordinal);

        if (!seen.Add(part.Name)) return visible;

        foreach (var dependency in part.Merges)
        {
            if (all.TryGetValue(dependency, out var merged))
            {
                visible.UnionWith(VisibleKeys(merged, all, seen));
            }
        }

        return visible;
    }

    private static string ThemesDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "RibbonKit.sln")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, $"Could not find RibbonKit.sln above {AppContext.BaseDirectory}.");

        var themes = Path.Combine(directory!.FullName, "src", "RibbonKit", "Themes");
        Assert.True(Directory.Exists(themes), $"Themes folder not found at {themes}.");

        return themes;
    }

    /// <summary>Blanks out XML comments while preserving line numbering.</summary>
    private static string StripComments(string xaml) =>
        Regex.Replace(
            xaml,
            "<!--.*?-->",
            match => new string('\n', match.Value.Count(c => c == '\n')),
            RegexOptions.Singleline);

    private static int LineOf(string text, int index) =>
        text.Take(index).Count(c => c == '\n') + 1;

    private static string Report(string headline, IEnumerable<string> problems)
    {
        var list = problems.ToList();

        return $"{headline} - see 04-DESIGN-NOTES.md §3.37:{Environment.NewLine}  " +
               string.Join(Environment.NewLine + "  ", list);
    }
}

internal sealed class ThemePart
{
    public ThemePart(
        string name,
        string rawText,
        string strippedText,
        Dictionary<string, int> keys,
        List<string> merges)
    {
        Name = name;
        RawText = rawText;
        StrippedText = strippedText;
        Keys = keys;
        Merges = merges;
    }

    /// <summary>File name, e.g. Controls.Shared.xaml.</summary>
    public string Name { get; }

    /// <summary>Original file text, used where comments must survive (XML parsing).</summary>
    public string RawText { get; }

    /// <summary>File text with comment bodies blanked out, line numbering preserved.</summary>
    public string StrippedText { get; }

    /// <summary>x:Key -> line number of its first declaration.</summary>
    public Dictionary<string, int> Keys { get; }

    /// <summary>File names this part merges via its own ResourceDictionary.MergedDictionaries.</summary>
    public List<string> Merges { get; }
}
