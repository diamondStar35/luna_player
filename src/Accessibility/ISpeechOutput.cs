namespace LunaPlayer.Accessibility;

internal interface ISpeechOutput : IDisposable
{
    void Speak(string beginnerText, string? advancedText = null, bool interrupt = true);
    void SpeakText(string text, bool interrupt = true);
}
