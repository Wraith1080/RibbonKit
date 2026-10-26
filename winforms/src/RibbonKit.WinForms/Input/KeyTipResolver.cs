namespace RibbonKit.WinForms;

internal static class KeyTipResolver
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    internal static string[] Resolve(IEnumerable<(string Text, string Keys)> candidates)
    {
        var input = candidates.ToArray();
        var result = new string[input.Length];
        var used = new List<string>();
        bool largeLevel = input.Length > Alphabet.Length;
        bool Available(string key) => !used.Any(other => key.StartsWith(other, StringComparison.Ordinal) || other.StartsWith(key, StringComparison.Ordinal));
        for (int i = 0; i < input.Length; i++)
        {
            string key = input[i].Keys.Trim().ToUpperInvariant();
            if (key.Length > 0 && key.All(Alphabet.Contains) && (!largeLevel || !key.StartsWith('9')) && Available(key))
            {
                result[i] = key;
                used.Add(key);
            }
        }
        for (int i = 0; i < input.Length; i++)
        {
            if (result[i] is not null)
                continue;
            string? key = input[i].Text.ToUpperInvariant().Where(Alphabet.Contains).Select(c => c.ToString()).Concat(Alphabet.Select(c => c.ToString())).FirstOrDefault(k => (!largeLevel || k != "9") && Available(k));
            if (key is null)
            {
                int suffixLength = Math.Max(1, (int)Math.Ceiling(Math.Log(Math.Max(2, input.Length), Alphabet.Length)));
                key = Codes(suffixLength).Select(code => "9" + code).First(Available);
            }
            result[i] = key;
            used.Add(key);
        }
        return result;
    }
    private static IEnumerable<string> Codes(int length)
    {
        if (length == 0)
        {
            yield return "";
            yield break;
        }
        foreach (var prefix in Codes(length - 1))
            foreach (var c in Alphabet)
                yield return prefix + c;
    }
}
