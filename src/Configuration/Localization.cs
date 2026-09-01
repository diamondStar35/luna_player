using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using WxSharp;

namespace LunaPlayer.Configuration;

/// <summary>The application's language: the message catalogues and the locale they follow.</summary>
///
/// <remarks>
/// Translations are GNU gettext <c>.mo</c> catalogues under <c>locale/&lt;language&gt;/LC_MESSAGES</c>, next
/// to the executable, which is the layout the tooling in <c>scripts/</c> produces. A language with no
/// catalogue simply shows the English strings the source is written in, so a missing or partial translation
/// is never an error.
///
/// <see cref="Initialize"/> has to run before anything builds a string, which in practice means before the
/// first window and before the action tables are touched.
/// </remarks>
internal static partial class Localization
{
    /// <summary>The catalogue name. A <c>.mo</c> file has to be called this to be found.</summary>
    internal const string Domain = "LunaPlayer";

    /// <summary>The value <see cref="GeneralSettings.Language"/> holds to mean "follow Windows".</summary>
    internal const string SystemLanguage = "system";

    private const int NumericCategory = 4;
    private static Locale? _locale;

    /// <summary>Where the catalogues live.</summary>
    internal static string Directory => Path.Combine(AppContext.BaseDirectory, "locale");

    /// <summary>Sets the language to a code such as <c>ar</c> or <c>pt-BR</c>, or to whatever Windows is set
    /// to when it is <see cref="SystemLanguage"/>, unrecognised, or empty. Loading a catalogue that is not
    /// there is not a failure: the strings stay as they are written in the source.</summary>
    internal static void Initialize(string? language)
    {
        var selected = Normalize(language);
        // A language wx does not know cannot be asked for by number, so those fall back to the system one
        // rather than leaving the player with no locale at all.
        var requested = selected.Length == 0 ? null : Locale.FindLanguage(selected);
        _locale = new Locale(requested?.Language ?? Language.Default, LocaleInitFlags.LoadDefault);
        Locale.AddCatalogLookupPathPrefix(Directory);
        _locale.AddCatalog(Domain);
        // Creating the locale set the C runtime's, which decides what strtod accepts. libmpv requires the
        // numeric locale to be "C" and every option the player sends it is written that way, so a language
        // whose decimal separator is a comma would otherwise turn "0.5" into 0. Only this one category is
        // put back; dates, money and sorting stay in the user's language.
        _ = SetLocale(NumericCategory, "C");
    }

    /// <summary>The languages the player ships a catalogue for, as canonical codes. Empty until
    /// <see cref="Initialize"/> has run.</summary>
    internal static IReadOnlyList<string> AvailableLanguages()
    {
        var translations = Translations.Current;
        if (translations is null) return [];
        // wx reports the languages it found catalogues for while looking the domain up, which is more
        // reliable than listing the directory: it applies the same name matching that loading will.
        var languages = translations.GetAvailableTranslations(Domain);
        return [.. languages.Where(language => !string.IsNullOrWhiteSpace(language))];
    }

    /// <summary>A language code as a name to show the user, in English and then in the language itself -
    /// "Arabic (العربية)" rather than "ar". Both names are given because either one alone fails somebody: a
    /// speaker of the language may not read English, and a user who picked the wrong one by mistake cannot
    /// read their way back out of a list written entirely in scripts they do not know. Falls back to the
    /// code itself when wx does not recognise it.</summary>
    internal static string LanguageName(string? code)
    {
        var text = (code ?? string.Empty).Trim();
        if (text.Length == 0) return string.Empty;
        var info = Locale.FindLanguage(text) ?? Locale.FindLanguage(text.Replace('-', '_'))
            ?? Locale.FindLanguage(text.Replace('_', '-'));
        var description = info?.Description?.Trim() ?? string.Empty;
        var native = info?.DescriptionNative?.Trim() ?? string.Empty;
        if (description.Length == 0) return native.Length == 0 ? text : native;
        // wx leaves the native name empty for some languages and equal to the English one for English
        // itself, and neither is worth showing twice.
        return native.Length == 0 || native.Equals(description, StringComparison.CurrentCultureIgnoreCase)
            ? description
            : $"{description} ({native})";
    }

    private static string Normalize(string? language)
    {
        var text = (language ?? string.Empty).Trim();
        return text.Length == 0 || text.Equals(SystemLanguage, StringComparison.OrdinalIgnoreCase)
            || text.Equals("default", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : text;
    }

    [LibraryImport("ucrtbase.dll", EntryPoint = "setlocale", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint SetLocale(int category, string locale);
}

/// <summary>The translation lookup. Its members are available unqualified everywhere through the global
/// using in <c>GlobalUsings.cs</c>, so a string the user can see reads <c>Tr("Open File")</c>.</summary>
internal static class Text
{
    /// <summary>The translation of <paramref name="text"/>, or <paramref name="text"/> itself when no
    /// catalogue has it. Safe to wrap a string in before any translation of it exists.</summary>
    internal static string Tr(string text) => Translations.Get(text, Localization.Domain);

    /// <summary>The translation of a string that has a plural, choosing the form for <paramref name="count"/>
    /// by the rule the catalogue declares. This cannot be replaced by testing <c>count == 1</c> at the call
    /// site: languages disagree about how many plural forms they have and where the boundaries fall.</summary>
    internal static string TrPlural(string singular, string plural, int count)
        => Translations.Get(singular, plural, (uint)Math.Max(0, count), Localization.Domain);

    /// <summary>The translation of a message with values in it, named rather than numbered:
    /// <c>TrFormat("Delete {name}?", name)</c>. The values are given in the order the names first appear in
    /// <paramref name="text"/>, and a translation may put them in any order it needs - the name is what binds
    /// a placeholder to a value, not its position. A value may carry a .NET format specification after a
    /// colon, as in <c>{seconds:0.0}</c>. Doubled braces stand for a literal brace.</summary>
    internal static string TrFormat(string text, params object?[] values)
        => Substitute(Translations.Get(text, Localization.Domain), text, values);

    /// <summary><see cref="TrPlural"/> and <see cref="TrFormat"/> together, for a counted message that also
    /// shows the count: <c>TrPluralFormat("{count} file marked", "{count} files marked", count, count)</c>.
    /// </summary>
    internal static string TrPluralFormat(string singular, string plural, int count, params object?[] values)
        => Substitute(TrPlural(singular, plural, count), singular, values);

    /// <summary>Fills placeholders in <paramref name="translated"/>, taking the meaning of each name from
    /// <paramref name="template"/> - the string as the source wrote it, whose placeholder order defines which
    /// value is which. A translation naming something the source does not is used as written in the source
    /// instead, so a mistake in a catalogue costs the translation of one message rather than garbling it.
    /// </summary>
    private static string Substitute(string translated, string template, object?[] values)
    {
        if (values.Length == 0) return translated;
        var names = Names(template);
        if (TryBuild(translated, names, values, out var result)) return result;
        return TryBuild(template, names, values, out var original) ? original : template;
    }

    /// <summary>The placeholder names in the order they first appear, which is the order the call site passes
    /// its values in.</summary>
    private static List<string> Names(string template)
    {
        var names = new List<string>(2);
        for (var index = 0; index < template.Length; index++)
        {
            if (template[index] != '{') continue;
            if (index + 1 < template.Length && template[index + 1] == '{') { index++; continue; }
            var close = template.IndexOf('}', index + 1);
            if (close < 0) break;
            var name = Name(template[(index + 1)..close]);
            if (!names.Contains(name, StringComparer.Ordinal)) names.Add(name);
            index = close;
        }
        return names;
    }

    private static bool TryBuild(string text, List<string> names, object?[] values, out string result)
    {
        var builder = new StringBuilder(text.Length + 16);
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            var doubled = index + 1 < text.Length && text[index + 1] == character;
            if (character is '{' or '}' && doubled) { builder.Append(character); index++; continue; }
            if (character != '{') { builder.Append(character); continue; }
            var close = text.IndexOf('}', index + 1);
            if (close < 0) { result = string.Empty; return false; }
            var body = text[(index + 1)..close];
            var position = names.IndexOf(Name(body));
            if (position < 0 || position >= values.Length) { result = string.Empty; return false; }
            builder.Append(Render(values[position], Specification(body)));
            index = close;
        }
        result = builder.ToString();
        return true;
    }

    private static string Name(string body)
    {
        var separator = body.IndexOf(':');
        return separator < 0 ? body : body[..separator];
    }

    private static string? Specification(string body)
    {
        var separator = body.IndexOf(':');
        return separator < 0 ? null : body[(separator + 1)..];
    }

    private static string Render(object? value, string? specification) => value switch
    {
        null => string.Empty,
        // The user's language decides how a number reads, but a value the code needs to stay machine-readable
        // passes its own specification and an invariant culture with it.
        IFormattable formattable => formattable.ToString(specification, CultureInfo.CurrentCulture),
        _ => value.ToString() ?? string.Empty,
    };
}
