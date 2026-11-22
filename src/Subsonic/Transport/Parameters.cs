using System.Globalization;

namespace LunaPlayer.Subsonic.Transport;

/// <summary>Preserves repeated keys and distinguishes omitted values from zero, false and empty text.</summary>
internal sealed class Parameters : List<KeyValuePair<string, string>>
{
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
    internal string Query => string.Join("&", this.Select(pair =>
        Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)));
}
