using LunaPlayer.Application;
using LunaPlayer.Actions;
using LunaPlayer.Configuration;
using LunaPlayer.Configuration.Models;
using LunaPlayer.Media;
using LunaPlayer.UI.Equalizer;
using WxSharp;
using Downloads = LunaPlayer.YouTube.Download;

namespace LunaPlayer.UI.Services.Dialogs;

internal sealed class PlaybackDialogs(DialogContext context)
{
    private readonly DialogContext _context = context;

    public string? ChooseSubtitleFile()
    {
        using var dialog = new FileDialog(_context.Parent,
            // Translators: Title of the window that picks a subtitle file to load, and its file-type filter.
            message: Tr("Load subtitle"),
            // Translators: File-type filter for supported subtitle files, followed by all files.
            wildcard: Tr("Subtitle files (*.srt;*.ass;*.ssa;*.vtt;*.sub)|*.srt;*.ass;*.ssa;*.vtt;*.sub|All files (*.*)|*.*"),
            style: FileDialogStyle.Open | FileDialogStyle.FileMustExist);
        return dialog.ShowModal() == StandardId.Ok ? dialog.Path : null;
    }

    public string? PromptSubtitleUrl()
    {
        // Translators: Prompt asking for a subtitle file's web address.
        var prompt = Tr("Enter the web address of a subtitle file:");
        // Translators: Title of the box asking for a subtitle file's web address.
        var title = Tr("Load subtitle from URL");
        var url = _context.Dialogs.PromptText(prompt, title);
        return string.IsNullOrWhiteSpace(url) ? null : url.Trim();
    }
    public double? ChooseTime(double duration, double elapsed) { using var dialog = new LunaPlayer.UI.Playback.TimeDialog(_context.Parent, duration, elapsed); return dialog.Show(); }
    public LunaPlayer.UI.Playback.SleepTimerDialogResult? ChooseSleepTimer(LunaPlayer.UI.Playback.SleepTimerRequest initial, bool armed, string? statusText) { using var dialog = new LunaPlayer.UI.Playback.SleepTimerDialog(_context.Parent, initial, armed, statusText); return dialog.Show(); }
}
