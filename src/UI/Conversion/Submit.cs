using LunaPlayer.Media;
using WxSharp;

namespace LunaPlayer.UI.Conversion;
internal sealed partial class MainDialog
{

    // Builds what the window was filled in with and closes it, handing the work to whoever opened it. Nothing
    // is converted here: gathering the request and running it are kept apart, so the window is free the moment
    // Convert is pressed and the conversion runs behind its own progress window, the way a download does.
    private void Convert()
    {
        var extension = Formats[_format.SelectedIndex].Extension;
        var preserve = _preserveLocations.Checked;
        var folder = _destination.Value.Trim();
        // A destination is needed only when the files are not written beside their sources. It is created now
        // rather than during the conversion so a folder that cannot be made is reported over this window,
        // where the user can still put it right, rather than failing every file once the window has gone.
        if (!preserve)
        {
            if (folder.Length == 0)
            {
                Warn(
                    // Translators: Shown when Convert is pressed with no destination folder chosen and files
                    // are not being written beside their sources.
                    Tr("Choose a folder to write the converted files to, or turn on \"Preserve original file locations\"."));
                return;
            }
            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception failure) when (failure is IOException or UnauthorizedAccessException
                or ArgumentException or NotSupportedException or System.Security.SecurityException)
            {
                // Translators: Shown when the chosen destination folder could not be created.
                var message = Tr("The destination folder could not be created.");
                // Translators: Adds {reason}, the system's explanation, below a destination-folder error.
                Warn(TrFormat("{message}\n{reason}", message, failure.Message));
                return;
            }
        }

        _request = RequestBuilder.Create(_files, extension, preserve, folder, BuildSettings(extension),
            _threads.Value, _deleteOriginals.Checked, Formats[_format.SelectedIndex].Label);
        _dialog.EndModal(StandardId.Ok);
    }

    private void Warn(string message) => Wx.MessageBox(
        message,
        // Translators: Title of the messages the media converter shows.
        Tr("Media converter"),
        MessageBoxStyle.Ok | MessageBoxStyle.IconWarning,
        _dialog);

}
