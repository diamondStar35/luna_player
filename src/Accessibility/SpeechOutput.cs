using LunaPlayer.Configuration;
using PrismSharp;
using PrismSharp.Speech;
using PrismSharp.Speech.ScreenReaders;

namespace LunaPlayer.Accessibility;

internal sealed class SpeechOutput : ISpeechOutput
{
    private readonly PlayerSettings _settings;
    private ScreenReaderWorker? _worker;

    internal SpeechOutput(PlayerSettings settings)
    {
        _settings = settings;
        try
        {
            _worker = new ScreenReaderWorker(Factory.Create());
            _worker.Invoke(static reader => reader.Initialize());
        }
        catch (InvalidOperationException)
        {
            _worker?.Dispose();
            _worker = null;
        }
        catch (PrismException)
        {
            _worker?.Dispose();
            _worker = null;
        }
        catch (DllNotFoundException)
        {
            _worker?.Dispose();
            _worker = null;
        }
    }

    public void Speak(string beginnerText, string? advancedText = null, bool interrupt = true)
    {
        var text = _settings.General.Verbosity == SpeechVerbosity.Advanced
            ? advancedText ?? beginnerText
            : beginnerText;
        SpeakText(text, interrupt);
    }

    public void SpeakText(string text, bool interrupt = true)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        _worker?.Post(reader => reader.Speak(text, interrupt));
    }

    public void Dispose()
    {
        _worker?.Dispose();
        _worker = null;
    }
}
