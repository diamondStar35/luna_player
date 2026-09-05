using System.Diagnostics;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using NAudio.Wave;

namespace LunaPlayer.Recording;

/// <summary>One entry in a device list.</summary>
/// <param name="Id">Null for the entry that stands for whichever device is default at the time.</param>
internal readonly record struct AudioDeviceChoice(string? Id, string Name);

/// <summary>What a format can actually be written as on this machine.</summary>
/// <param name="Rates">The offered sample rates this format's encoder accepts, in the listed order.</param>
/// <param name="Channels">For each entry of <paramref name="Rates"/>, the channel counts it accepts.</param>
internal readonly record struct FormatSupport(
    IReadOnlyList<int> Rates,
    IReadOnlyList<IReadOnlyList<int>> Channels);

/// <summary>One entry in the list of programs that can be captured.</summary>
internal readonly record struct ProcessChoice(int ProcessId, string Name)
{
    /// <summary>How the program is written in the list. The id is part of it because two windows of the
    /// same program are two different things to capture and are otherwise indistinguishable.</summary>
    internal string Label => $"{Name}; PID: {ProcessId}";
}

/// <summary>What there is to record from, and what the machine can encode.</summary>
///
/// <remarks>
/// Every method here talks to Windows and none of them belong on the thread that draws the windows:
/// enumerating endpoints opens each device, and asking Media Foundation for a bitrate list loads the
/// encoder to ask it. Callers run them on a worker and post the answer back.
/// </remarks>
internal sealed class AudioCatalog
{
    /// <summary>Whether this copy of Windows can capture one program's sound on its own.</summary>
    ///
    /// <remarks>
    /// The interface that does it arrived in Windows 10 version 2004, and the player runs as far back as
    /// 1809 - so this is a real question rather than a formality, and it is asked before the option is
    /// offered rather than after it has failed.
    /// </remarks>
    /// <remarks>Marked as a guard so the platform analyser trusts a test of it the way it would trust the
    /// version check written out in full at every call site.</remarks>
    [System.Runtime.Versioning.SupportedOSPlatformGuard("windows10.0.19041.0")]
    internal static bool SupportsProcessCapture { get; } =
        OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041);

    /// <summary>The rates offered for a wave file, lowest first.</summary>
    /// <remarks>
    /// Every rate any encoder on a normal Windows install declares, which is the widest set worth
    /// offering: a wave file has no encoder to refuse one, and shared-mode capture converts the device
    /// to whatever is asked for. All thirteen were opened successfully on a microphone and on a loopback
    /// device, in mono and stereo, before being listed here. The encoded formats do not use this list -
    /// they are asked what they take; see <see cref="Support"/>.
    /// </remarks>
    internal static IReadOnlyList<int> SampleRates { get; } =
        [8000, 11025, 12000, 16000, 22050, 24000, 32000, 44100, 48000, 88200, 96000, 176400, 192000];

    /// <summary>Microphones, line inputs, and anything else Windows treats as a capture device.</summary>
    internal IReadOnlyList<AudioDeviceChoice> InputDevices(string defaultLabel)
        => Devices(DataFlow.Capture, defaultLabel);

    /// <summary>Speakers and headphones - the devices whose output can be captured as it leaves.
    /// </summary>
    internal IReadOnlyList<AudioDeviceChoice> OutputDevices(string defaultLabel)
        => Devices(DataFlow.Render, defaultLabel);

    /// <summary>Every program that has an audio session, whether it is making a sound right now or not.
    /// </summary>
    ///
    /// <remarks>
    /// Across every active output device, not only the default one: a program playing to a second sound
    /// card has a session there and nowhere else. The system's own session is dropped - it has no process
    /// to capture - and a program with sessions on two devices is listed once.
    /// </remarks>
    internal IReadOnlyList<ProcessChoice> Processes()
    {
        var found = new Dictionary<int, ProcessChoice>();
        using var devices = new MMDeviceEnumerator();
        foreach (var device in devices.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            try
            {
                var sessions = device.AudioSessionManager.Sessions;
                for (var index = 0; index < sessions.Count; index++)
                {
                    var id = (int)sessions[index].GetProcessID;
                    if (id <= 0 || found.ContainsKey(id))
                        continue;
                    if (NameOf(id) is string name)
                        found[id] = new ProcessChoice(id, name);
                }
            }
            catch (Exception exception) when (exception is COMException or InvalidOperationException)
            {
                // One device that will not answer is not a reason to list nothing. A device can be taken
                // away between being enumerated and being asked.
            }
            finally
            {
                device.Dispose();
            }
        }
        return [.. found.Values.OrderBy(choice => choice.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(choice => choice.ProcessId)];
    }

    /// <summary>The rates and channel counts a format can actually be written at.</summary>
    ///
    /// <remarks>
    /// Asked of the encoder rather than assumed, because the answers are neither obvious nor the same:
    /// on this machine MP3 stops at 48 kHz, FLAC starts at 44.1, and AAC will do 96 kHz in stereo but not
    /// in mono. Offering a combination the encoder refuses is worse than not offering it - the recording
    /// starts, runs, and only fails when it is stopped and the encoder is finally asked to close a file
    /// it never opened.
    ///
    /// A wave file has no encoder and takes whatever it is given, so it is answered without asking.
    /// </remarks>
    internal FormatSupport Support(RecordingFormat format)
    {
        if (Subtype(format) is not Guid subtype)
            return new(SampleRates, [.. SampleRates.Select(_ => (IReadOnlyList<int>)BothChannelCounts)]);
        // The encoder's own rates rather than an intersection with the list above, so a codec that
        // offers something that list has never heard of is still offered it.
        var accepted = Accepted(subtype);
        var rates = new List<int>();
        var channels = new List<IReadOnlyList<int>>();
        foreach (var rate in accepted.Select(entry => entry.Rate).Distinct().Order())
        {
            var counts = BothChannelCounts.Where(count => accepted.Contains((rate, count))).ToArray();
            // Nothing in mono or stereo at this rate: the encoder does it only in surround, which the
            // interface does not offer, so the rate is no use here.
            if (counts.Length == 0)
                continue;
            rates.Add(rate);
            channels.Add(counts);
        }
        return new(rates, channels);
    }

    /// <summary>Whether a format can be written at this rate and channel count.</summary>
    internal bool Supports(RecordingFormat format, int sampleRate, int channels)
    {
        var support = Support(format);
        for (var index = 0; index < support.Rates.Count; index++)
        {
            if (support.Rates[index] == sampleRate)
                return support.Channels[index].Contains(channels);
        }
        return false;
    }

    /// <summary>Mono and stereo, which is all the interface offers.</summary>
    private static readonly int[] BothChannelCounts = [1, 2];

    private readonly Lock _sync = new();
    private readonly Dictionary<Guid, HashSet<(int Rate, int Channels)>> _accepted = [];

    /// <summary>Every rate and channel count an encoder declares, asked once and remembered.</summary>
    /// <remarks>Loading an encoder to interrogate it is not free, and the answer cannot change while the
    /// player is running.</remarks>
    private HashSet<(int Rate, int Channels)> Accepted(Guid subtype)
    {
        lock (_sync)
        {
            if (_accepted.TryGetValue(subtype, out var remembered))
                return remembered;
        }
        var found = new HashSet<(int, int)>();
        try
        {
            MediaFoundation.Start();
            foreach (var type in MediaFoundationEncoder.GetOutputMediaTypes(subtype))
            {
                try
                {
                    found.Add((type.SampleRate, type.ChannelCount));
                }
                catch (COMException)
                {
                    // A media type that does not carry both attributes tells us nothing; the rest still do.
                }
            }
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException
            or ArgumentException)
        {
            // The encoder is not installed at all. An empty set means the format offers no rates, which
            // is the truth and is what stops it being chosen.
        }
        lock (_sync)
            _accepted[subtype] = found;
        return found;
    }

    /// <summary>The bitrates this machine's encoder will accept for a format at a given rate and channel
    /// count, largest last. Empty for the formats that do not compress.</summary>
    ///
    /// <remarks>
    /// Asked of Windows rather than hard-coded, and asked again whenever the rate or the channel count
    /// changes, because the answer depends on both: mp3 offers 96k upwards in stereo at 44.1 kHz and a
    /// different set in mono.
    /// </remarks>
    internal IReadOnlyList<int> Bitrates(RecordingFormat format, int sampleRate, int channels)
    {
        if (Subtype(format) is not Guid subtype)
            return [];
        try
        {
            MediaFoundation.Start();
            return [.. MediaFoundationEncoder.GetEncodeBitrates(subtype, sampleRate, channels).Order()];
        }
        catch (Exception exception) when (exception is COMException or InvalidOperationException
            or ArgumentException)
        {
            // The encoder is not installed, or will not do this combination. An empty list leaves the
            // caller showing no choice rather than a wrong one.
            return [];
        }
    }

    /// <summary>The Media Foundation name for a format, or null for the ones written without an encoder.
    /// </summary>
    internal static Guid? Subtype(RecordingFormat format) => format switch
    {
        RecordingFormat.Mp3 => AudioSubtypes.MFAudioFormat_MP3,
        RecordingFormat.Aac => AudioSubtypes.MFAudioFormat_AAC,
        RecordingFormat.Flac => AudioSubtypes.MFAudioFormat_FLAC,
        _ => null,
    };

    /// <summary>Whether a format has a bitrate to choose at all. False for the lossless ones, where the
    /// figure would be meaningless.</summary>
    internal static bool HasBitrate(RecordingFormat format)
        => format is RecordingFormat.Mp3 or RecordingFormat.Aac;

    /// <summary>The extension a format is written with.</summary>
    internal static string Extension(RecordingFormat format) => format switch
    {
        RecordingFormat.Mp3 => "mp3",
        // The container rather than the codec: an AAC stream from Media Foundation is written into an
        // MPEG-4 file, which is what every player expects to be called m4a.
        RecordingFormat.Aac => "m4a",
        RecordingFormat.Flac => "flac",
        _ => "wav",
    };

    private static IReadOnlyList<AudioDeviceChoice> Devices(DataFlow flow, string defaultLabel)
    {
        // The default stands first and carries no id, so it keeps meaning "whichever is default now"
        // rather than freezing today's answer.
        var found = new List<AudioDeviceChoice> { new(null, defaultLabel) };
        using var devices = new MMDeviceEnumerator();
        foreach (var device in devices.EnumerateAudioEndPoints(flow, DeviceState.Active))
        {
            using (device)
                found.Add(new AudioDeviceChoice(device.ID, device.FriendlyName));
        }
        return found;
    }

    private static string? NameOf(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            // The program ended between the session being listed and being asked about. A session with
            // nothing behind it is nothing to record.
            return null;
        }
    }
}
