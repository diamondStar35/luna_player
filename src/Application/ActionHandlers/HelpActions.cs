using System.Diagnostics;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.UI;

namespace LunaPlayer.Application.ActionHandlers;

/// <summary>Opens Luna Player's local and online help, and presents its application information.</summary>
internal sealed class HelpActions
{
    private readonly IMainView _view;

    internal HelpActions(ActionRouter router, IMainView view)
    {
        _view = view;
        router.Register(ActionId.UserGuide, OpenUserGuide);
        router.Register(ActionId.About, view.ShowAbout);
        router.Register(ActionId.ReleaseNotes, OpenReleaseNotes);
    }

    private void OpenUserGuide()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "docs");
        foreach (var language in DocumentationLanguages())
        {
            var path = Path.Combine(root, language, "user-guide.html");
            if (!File.Exists(path))
                continue;
            Open(
                path,
                // Translators: Shown when Windows cannot open the installed HTML user guide.
                Tr("The user guide could not be opened."),
                Tr("User guide"));
            return;
        }

        _view.ShowError(
            // Translators: Shown when no local user-guide HTML file was included with the application.
            Tr("The user guide is not installed."),
            Tr("User guide"));
    }

    private void OpenReleaseNotes()
    {
        var version = Uri.EscapeDataString(AppInfo.Version);
        Open(
            $"{AppInfo.RepositoryUrl}/releases/tag/v{version}",
            // Translators: Shown when Windows cannot open the release page in a web browser.
            Tr("The release notes could not be opened."),
            Tr("Release notes"));
    }

    private void Open(string target, string failureMessage, string caption)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
            if (process is null)
                // Translators: Detail shown when Windows accepts an item to open but starts no application.
                throw new InvalidOperationException(Tr("Windows did not start an application for this item."));
        }
        catch (Exception failure)
        {
            _view.ShowError($"{failureMessage}\n\n{failure.Message}", caption);
        }
    }

    private static IEnumerable<string> DocumentationLanguages()
    {
        var code = Localization.CurrentLanguageCode;
        var bcp47 = code.Replace('_', '-');
        if (!code.Equals(bcp47, StringComparison.OrdinalIgnoreCase))
            yield return code;
        yield return bcp47;

        var separator = bcp47.IndexOf('-');
        if (separator > 0)
            yield return bcp47[..separator];

        if (!bcp47.Equals("en", StringComparison.OrdinalIgnoreCase))
            yield return "en";
    }
}
