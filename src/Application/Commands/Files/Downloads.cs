using LunaPlayer.Accessibility;
using LunaPlayer.Actions;
using LunaPlayer.Downloads;
using LunaPlayer.Playback;
using LunaPlayer.UI;

namespace LunaPlayer.Application.Commands.Files;

internal sealed class Downloads
{
    internal Downloads(ActionRouter router, Player player, IMainView view, ISpeechOutput speech,
        LunaPlayer.Downloads.DownloadQueue queue, YouTube.YouTube youtube)
    {
        // Keep the command IDs stable so existing customized download shortcuts remain valid.
        router.Register(ActionId.VideoDownload, () => Execute(false, false));
        router.Register(ActionId.VideoDirectDownload, () => Execute(true, false));
        router.Register(ActionId.AddToDownloadQueue, () => Execute(true, true));

        void Execute(bool direct, bool queued)
        {
            switch (Source.Resolve(player.CurrentPath, player.CurrentSource, player.InRadioSession))
            {
                case SourceKind.YouTube:
                    youtube.DownloadCurrent(direct, queued); break;
                case SourceKind.MusicServer:
                    view.Dialogs.ActOnServerSong(player.CurrentPath!, queued ? UI.Subsonic.SongAction.QueueDownload
                        : direct ? UI.Subsonic.SongAction.DirectDownload : UI.Subsonic.SongAction.Download); break;
                case SourceKind.HttpFile:
                    var uri = new Uri(player.CurrentPath!); var name = Source.FileName(uri, player.CurrentTitle ?? "");
                    var destination = direct ? Path.Combine(Configuration.Paths.DefaultDownloadsDirectory, name) : view.Dialogs.ChooseDownloadDestination(name);
                    if (destination is null) return;
                    var id = queue.Add(HttpJob.Create(uri, destination), startImmediately: !queued);
                    if (!queued) view.Dialogs.ShowDownloadProgress(queue, id);
                    else
                        // Translators: Short confirmation after adding the current online file to the download queue.
                        speech.SpeakText(Tr("Added to download queue."));
                    break;
            }
        }
    }
}
