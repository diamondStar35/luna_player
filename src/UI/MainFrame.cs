using WxSharp;

namespace LunaPlayer.UI;

internal static class MainFrame
{
    internal static Frame Create()
    {
        var frame = new Frame(title: "Luna Player", size: new Size(420, 160));
        BuildUi(frame);
        return frame;
    }

    private static void BuildUi(Frame frame)
    {
        var previousButton = new CustomButton(frame, "Previous");
        var rewindButton = new CustomButton(frame, "Rewind");
        var playButton = new CustomButton(frame, "Play");
        var forwardButton = new CustomButton(frame, "Forward");
        var nextButton = new CustomButton(frame, "Next");

        var buttonSizer = new BoxSizer(Orientation.Horizontal);
        buttonSizer.Insert(0, previousButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(rewindButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(playButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(forwardButton, flags: SizerFlags.All, border: 5);
        buttonSizer.Add(nextButton, flags: SizerFlags.All, border: 5);

        var sizer = new BoxSizer(Orientation.Vertical);
        sizer.Add(buttonSizer, flags: SizerFlags.AlignCenterHorizontal | SizerFlags.All, border: 15);
        frame.SetSizer(sizer);
        frame.Fit();
    }
}
