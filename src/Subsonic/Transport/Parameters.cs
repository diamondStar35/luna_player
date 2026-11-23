using System.Globalization;

namespace LunaPlayer.Subsonic.Transport;

/// <summary>Preserves repeated keys and distinguishes omitted values from zero, false and empty text.</summary>
internal sealed class Parameters : List<KeyValuePair<string, string>>
{
    private readonly HashSet<string> _escaped = new(StringComparer.Ordinal);
    internal Parameters With(string key, object? value)
    {
        if (value is not null)
            Add(new(key, value is bool flag ? flag ? "true" : "false"
                : Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""));
        return this;
    }
    internal Parameters Many<T>(string key, IEnumerable<T>? values)
    {
        if (values is not null) foreach (var value in values) With(key, value);
        return this;
    }
    internal Parameters Escaped(string key, string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or '~') continue;
            if (character == '%' && index + 2 < value.Length && Uri.IsHexDigit(value[index + 1]) && Uri.IsHexDigit(value[index + 2]))
            { index += 2; continue; }
            throw new ArgumentException("Expected an escaped query value.", nameof(value));
        }
        _escaped.Add(key); return With(key, value);
    }
    internal void Include(Parameters parameters)
    { AddRange(parameters); _escaped.UnionWith(parameters._escaped); }
    internal string Query => string.Join("&", this.Select(pair =>
        Uri.EscapeDataString(pair.Key) + "=" + (_escaped.Contains(pair.Key) ? pair.Value : Uri.EscapeDataString(pair.Value))));
}
