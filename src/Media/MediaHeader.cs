using System.Buffers.Binary;

namespace LunaPlayer.Media;

/// <summary>Reads how long a media file is out of its header, without decoding any of it.</summary>
///
/// <remarks>
/// Every container here states its own length somewhere near the front or the back of the file, so finding it
/// costs a few small reads rather than a decoder. That is the whole point: a folder of three hundred files is
/// answered in milliseconds instead of the seconds a demuxer per file costs.
///
/// The format is decided by what the bytes say, not by the file name, because an extension is a guess and the
/// magic is not. A file whose format states no length - MPEG program and transport streams, raw ADTS - is
/// reported as null so the caller can fall back to something that will actually decode it.
///
/// Layouts are taken from each format's own specification; see the remarks on each reader for which. Every
/// one of them is checked against ffprobe over a generated corpus covering all of them.
/// </remarks>
internal static class MediaHeader
{
    /// <summary>Enough to hold the largest fixed header any reader below inspects.</summary>
    private const int ProbeSize = 64 * 1024;

    /// <summary>The length in seconds, or null when the file does not state one in a form that can be
    /// trusted, or could not be read at all.</summary>
    internal static double? ReadDuration(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                bufferSize: 0, FileOptions.RandomAccess);
            if (stream.Length < 16)
                return null;
            Span<byte> head = stackalloc byte[16];
            if (!ReadAt(stream, 0, head))
                return null;
            var duration = Dispatch(stream, head);
            // A container may state a nonsense length - zero, or something a corrupt field produced. Treat
            // anything outside a day and a half as unstated rather than passing it on.
            return duration is > 0.001 and < 129600 ? duration : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or NotSupportedException or ArgumentException)
        {
            return null;
        }
    }

    private static double? Dispatch(FileStream stream, ReadOnlySpan<byte> head)
    {
        if (head[..4].SequenceEqual("fLaC"u8))
            return Flac.Read(stream);
        if (head[..4].SequenceEqual("OggS"u8))
            return Ogg.Read(stream);
        if (head[..3].SequenceEqual("FLV"u8))
            return Flv.Read(stream);
        if (head[..4].SequenceEqual(Matroska.Magic))
            return Matroska.Read(stream);
        if (head[..4].SequenceEqual(Asf.HeaderGuidPrefix))
            return Asf.Read(stream);
        if (head[..4].SequenceEqual("RIFF"u8) || head[..4].SequenceEqual("RF64"u8))
        {
            if (head[8..12].SequenceEqual("WAVE"u8)) return Riff.ReadWave(stream);
            if (head[8..12].SequenceEqual("AVI "u8)) return Riff.ReadAvi(stream);
            return null;
        }
        if (head[..4].SequenceEqual("FORM"u8)
            && (head[8..12].SequenceEqual("AIFF"u8) || head[8..12].SequenceEqual("AIFC"u8)))
            return Aiff.Read(stream);
        // An ISO base media file names its brand in the second box, which is almost always first but is
        // allowed to be preceded by others.
        if (head[4..8].SequenceEqual("ftyp"u8) || head[4..8].SequenceEqual("moov"u8)
            || head[4..8].SequenceEqual("mdat"u8) || head[4..8].SequenceEqual("free"u8)
            || head[4..8].SequenceEqual("skip"u8) || head[4..8].SequenceEqual("wide"u8))
            return IsoBaseMedia.Read(stream);
        // The formats that state no length worth having. They are named rather than left to fall through,
        // because each of them carries bytes that a scan for an MPEG audio frame will happily mistake for
        // one, and a confident wrong answer is worse than none.
        if (IsTransportStream(stream, head) || IsProgramStream(head) || IsAdts(head))
            return null;
        return Mpeg.Read(stream, head);
    }

    /// <summary>An MPEG transport stream: 188 byte packets each starting with a sync byte. The length is only
    /// discoverable by reading timestamps out of the stream itself, so it is left to a demuxer.</summary>
    private static bool IsTransportStream(FileStream stream, ReadOnlySpan<byte> head)
    {
        if (head[0] != 0x47)
            return false;
        // One sync byte is a coincidence; three at the right spacing is a transport stream. 192 byte packets
        // are the same thing with a timestamp in front, as used by M2TS.
        Span<byte> at = stackalloc byte[1];
        foreach (var size in (ReadOnlySpan<int>)[188, 192])
        {
            if (ReadAt(stream, size, at) && at[0] == 0x47 && ReadAt(stream, size * 2, at) && at[0] == 0x47)
                return true;
        }
        return false;
    }

    /// <summary>An MPEG program stream or elementary video stream, from its start code.</summary>
    private static bool IsProgramStream(ReadOnlySpan<byte> head)
        => head[0] == 0x00 && head[1] == 0x00 && head[2] == 0x01 && head[3] is 0xBA or 0xB3 or 0xBB or 0xE0;

    /// <summary>Raw AAC in ADTS frames. The sync is twelve set bits and then a layer of zero, which is what
    /// tells it apart from MPEG audio; no frame states how many follow it.</summary>
    private static bool IsAdts(ReadOnlySpan<byte> head)
        => head[0] == 0xFF && (head[1] & 0xF0) == 0xF0 && (head[1] & 0x06) == 0;

    // ---- shared reading helpers ---------------------------------------------------------------------

    private static bool ReadAt(Stream stream, long offset, Span<byte> buffer)
    {
        if (offset < 0 || offset + buffer.Length > stream.Length)
            return false;
        stream.Position = offset;
        return stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false) == buffer.Length;
    }

    /// <summary>Reads up to <paramref name="length"/> bytes, returning however many were there.</summary>
    private static Span<byte> ReadWindow(Stream stream, long offset, byte[] buffer, int length)
    {
        if (offset < 0 || offset >= stream.Length)
            return Span<byte>.Empty;
        stream.Position = offset;
        var wanted = (int)Math.Min(length, stream.Length - offset);
        var got = stream.ReadAtLeast(buffer.AsSpan(0, wanted), wanted, throwOnEndOfStream: false);
        return buffer.AsSpan(0, got);
    }

    private static uint BE32(ReadOnlySpan<byte> b, int at) => BinaryPrimitives.ReadUInt32BigEndian(b[at..]);
    private static ulong BE64(ReadOnlySpan<byte> b, int at) => BinaryPrimitives.ReadUInt64BigEndian(b[at..]);
    private static ushort BE16(ReadOnlySpan<byte> b, int at) => BinaryPrimitives.ReadUInt16BigEndian(b[at..]);
    private static uint LE32(ReadOnlySpan<byte> b, int at) => BinaryPrimitives.ReadUInt32LittleEndian(b[at..]);
    private static ulong LE64(ReadOnlySpan<byte> b, int at) => BinaryPrimitives.ReadUInt64LittleEndian(b[at..]);
    private static ushort LE16(ReadOnlySpan<byte> b, int at) => BinaryPrimitives.ReadUInt16LittleEndian(b[at..]);

    // ---- FLAC ---------------------------------------------------------------------------------------

    /// <summary>FLAC, from the format specification's STREAMINFO block.</summary>
    /// <remarks>
    /// After the <c>fLaC</c> marker comes a chain of metadata blocks, each with a four byte header: one bit
    /// saying whether it is the last, seven bits of type, then a 24 bit length. STREAMINFO is type 0 and is
    /// required to come first. Its last 18 bytes pack the sample rate (20 bits), channel count (3), bits per
    /// sample (5) and the total number of inter-channel samples (36) end to end, so they are read as one
    /// 64 bit word and shifted apart.
    /// </remarks>
    private static class Flac
    {
        internal static double? Read(FileStream stream)
        {
            Span<byte> block = stackalloc byte[4 + 34];
            if (!ReadAt(stream, 4, block))
                return null;
            if ((block[0] & 0x7F) != 0)
                return null;
            var packed = BE64(block, 4 + 10);
            var sampleRate = (uint)(packed >> 44);
            var totalSamples = packed & 0xF_FFFF_FFFF;
            return sampleRate > 0 && totalSamples > 0 ? (double)totalSamples / sampleRate : null;
        }
    }

    // ---- Ogg (Vorbis, Opus, FLAC in Ogg) ------------------------------------------------------------

    /// <summary>Ogg, from the RFC 3533 page header and the codec's own identification header.</summary>
    /// <remarks>
    /// A page begins <c>OggS</c>, a version byte, a flags byte, then the granule position as a little endian
    /// 64 bit value. The granule of the last page is the stream's length in samples, so the file is scanned
    /// backwards from the end for the last page rather than read through.
    ///
    /// What a granule counts depends on the codec, which the first page names. Opus always counts at 48 kHz
    /// and carries a pre-skip that has to come off the total (RFC 7845); Vorbis and FLAC count at their own
    /// sample rate, which their identification header states.
    /// </remarks>
    private static class Ogg
    {
        internal static double? Read(FileStream stream)
        {
            var buffer = new byte[ProbeSize];
            var first = ReadWindow(stream, 0, buffer, ProbeSize);
            if (first.Length < 32)
                return null;

            double rate;
            ulong preSkip = 0;
            var opus = first.IndexOf("OpusHead"u8);
            if (opus >= 0 && opus + 12 <= first.Length)
            {
                rate = 48000;
                preSkip = LE16(first, opus + 10);
            }
            else
            {
                var vorbis = first.IndexOf((ReadOnlySpan<byte>)[0x01, (byte)'v', (byte)'o', (byte)'r', (byte)'b', (byte)'i', (byte)'s']);
                if (vorbis >= 0 && vorbis + 16 <= first.Length)
                {
                    rate = LE32(first, vorbis + 12);
                }
                else
                {
                    var flac = first.IndexOf("FLAC"u8);
                    // The FLAC-in-Ogg mapping puts a whole STREAMINFO block nine bytes after the marker.
                    if (flac < 0 || flac + 9 + 18 > first.Length) return null;
                    rate = BE64(first, flac + 9 + 10) >> 44;
                }
            }
            if (rate <= 0)
                return null;

            var granule = LastGranule(stream, buffer);
            if (granule is not ulong samples || samples <= preSkip)
                return null;
            return (samples - preSkip) / rate;
        }

        /// <summary>The granule position of the last page, found by scanning back from the end of the file.
        /// </summary>
        private static ulong? LastGranule(FileStream stream, byte[] buffer)
        {
            // A page can be about 64 KB at most, so a window that size is certain to contain the start of
            // the final one. Two windows are walked back in case the file ends with padding.
            for (var step = 0; step < 2; step++)
            {
                var offset = Math.Max(0, stream.Length - (long)ProbeSize * (step + 1));
                var window = ReadWindow(stream, offset, buffer, ProbeSize);
                for (var i = window.Length - 27; i >= 0; i--)
                {
                    if (window[i] != 'O' || !window.Slice(i, 4).SequenceEqual("OggS"u8))
                        continue;
                    var granule = LE64(window, i + 6);
                    // -1 marks a page that completes no packet; keep looking for a real one.
                    if (granule != ulong.MaxValue)
                        return granule;
                }
                if (offset == 0)
                    break;
            }
            return null;
        }
    }

    // ---- ISO base media (MP4, M4A, M4V, MOV, 3GP) ---------------------------------------------------

    /// <summary>ISO base media files, from the movie header box defined in ISO/IEC 14496-12.</summary>
    /// <remarks>
    /// The file is a tree of boxes, each a 32 bit size then a four character type; a size of 1 means the real
    /// size follows as 64 bits, and 0 means the box runs to the end of the file. Only the top level is walked,
    /// looking for <c>moov</c> - which is allowed to sit after the media data, so the walk cannot stop early -
    /// and then its <c>mvhd</c> child, whose version decides whether the timescale and duration are 32 or 64
    /// bit. The duration is in timescale units.
    /// </remarks>
    private static class IsoBaseMedia
    {
        internal static double? Read(FileStream stream)
        {
            var moov = FindBox(stream, 0, stream.Length, "moov"u8);
            if (moov is not (var moovStart, var moovEnd))
                return null;
            var mvhd = FindBox(stream, moovStart, moovEnd, "mvhd"u8);
            if (mvhd is not (var mvhdStart, _))
                return null;

            Span<byte> header = stackalloc byte[32];
            if (!ReadAt(stream, mvhdStart, header))
                return null;
            var version = header[0];
            // version 0: creation and modification are 32 bit; version 1: 64 bit. The timescale and duration
            // follow them either way.
            var (timescale, duration) = version == 1
                ? ((double)BE32(header, 4 + 16), (double)BE64(header, 4 + 20))
                : (BE32(header, 4 + 8), BE32(header, 4 + 12));
            // 0xFFFFFFFF is the documented "unknown" duration for a version 0 header.
            if (version == 0 && (uint)duration == uint.MaxValue)
                return null;
            return timescale > 0 && duration > 0 ? duration / timescale : null;
        }

        /// <summary>Walks the boxes between two offsets, returning where the wanted box's payload starts and
        /// ends.</summary>
        private static (long Start, long End)? FindBox(FileStream stream, long from, long to, ReadOnlySpan<byte> type)
        {
            Span<byte> header = stackalloc byte[16];
            var position = from;
            while (position + 8 <= to)
            {
                if (!ReadAt(stream, position, header[..8]))
                    return null;
                long size = BE32(header, 0);
                var payload = position + 8;
                if (size == 1)
                {
                    if (!ReadAt(stream, position, header))
                        return null;
                    size = (long)BE64(header, 8);
                    payload = position + 16;
                }
                else if (size == 0)
                {
                    size = to - position;
                }
                if (size < payload - position || position + size > to)
                    return null;
                if (header[4..8].SequenceEqual(type))
                    return (payload, position + size);
                position += size;
            }
            return null;
        }
    }

    // ---- Matroska and WebM --------------------------------------------------------------------------

    /// <summary>Matroska and WebM, from the Info element defined in the Matroska specification.</summary>
    /// <remarks>
    /// EBML stores every element as a variable length ID, a variable length size, then the payload. The
    /// length of both is written in the leading byte: the number of leading zero bits before the first one
    /// bit says how many bytes follow. An ID keeps its marker bit, a size has it stripped.
    ///
    /// Duration (ID 0x4489) is a float in segment ticks and TimecodeScale (ID 0x2AD7B1) says how many
    /// nanoseconds a tick is, defaulting to a million. Both live in Info (0x1549A966) inside Segment
    /// (0x18538067). Only those three are descended into; everything else is skipped over.
    /// </remarks>
    private static class Matroska
    {
        /// <summary>The EBML magic every Matroska and WebM file opens with.</summary>
        internal static ReadOnlySpan<byte> Magic => [0x1A, 0x45, 0xDF, 0xA3];

        private const ulong SegmentId = 0x18538067;
        private const ulong InfoId = 0x1549A966;
        private const ulong DurationId = 0x4489;
        private const ulong TimecodeScaleId = 0x2AD7B1;

        internal static double? Read(FileStream stream)
        {
            // The EBML header comes first and is skipped whole; Segment follows it.
            var segment = Find(stream, 0, stream.Length, SegmentId);
            if (segment is not (var segmentStart, var segmentEnd))
                return null;
            var info = Find(stream, segmentStart, segmentEnd, InfoId);
            if (info is not (var infoStart, var infoEnd))
                return null;

            double? duration = null;
            double scale = 1_000_000;
            var position = infoStart;
            Span<byte> value = stackalloc byte[8];
            while (position < infoEnd)
            {
                if (!ReadElement(stream, position, infoEnd, out var id, out var payload, out var size))
                    break;
                if (id == DurationId && size is 4 or 8 && ReadAt(stream, payload, value[..(int)size]))
                {
                    duration = size == 4
                        ? BitConverter.Int32BitsToSingle((int)BE32(value, 0))
                        : BitConverter.Int64BitsToDouble((long)BE64(value, 0));
                }
                else if (id == TimecodeScaleId && size is > 0 and <= 8 && ReadAt(stream, payload, value[..(int)size]))
                {
                    ulong parsed = 0;
                    for (var i = 0; i < (int)size; i++) parsed = parsed << 8 | value[i];
                    if (parsed > 0) scale = parsed;
                }
                position = payload + (long)size;
            }
            return duration is > 0 ? duration * scale / 1_000_000_000 : null;
        }

        /// <summary>Walks the elements between two offsets, returning where the wanted one's payload starts
        /// and ends.</summary>
        private static (long Start, long End)? Find(FileStream stream, long from, long to, ulong wanted)
        {
            var position = from;
            while (position < to)
            {
                if (!ReadElement(stream, position, to, out var id, out var payload, out var size))
                    return null;
                if (id == wanted)
                    return (payload, Math.Min(to, payload + (long)size));
                position = payload + (long)size;
            }
            return null;
        }

        /// <summary>Reads one element's ID and size, and says where its payload starts.</summary>
        private static bool ReadElement(FileStream stream, long position, long limit,
            out ulong id, out long payload, out ulong size)
        {
            id = 0; payload = 0; size = 0;
            Span<byte> head = stackalloc byte[16];
            if (position + 2 > limit || !ReadAt(stream, position, head[..Math.Min(16, (int)(limit - position))]))
                return false;

            var idLength = MarkerLength(head[0]);
            if (idLength == 0 || position + idLength > limit)
                return false;
            // An ID is used with its marker bit still in place, which is how the published IDs are written.
            for (var i = 0; i < idLength; i++) id = id << 8 | head[i];

            var sizeLength = MarkerLength(head[idLength]);
            if (sizeLength == 0 || position + idLength + sizeLength > limit)
                return false;
            // A size drops the marker bit and keeps the rest.
            size = (ulong)(head[idLength] & (0xFF >> sizeLength));
            for (var i = 1; i < sizeLength; i++) size = size << 8 | head[idLength + i];

            payload = position + idLength + sizeLength;
            // All bits set means "unknown length", which only a Segment normally uses; treat it as running
            // to the end of what is being searched.
            var unknown = size == (1UL << (7 * sizeLength)) - 1;
            if (unknown) size = (ulong)(limit - payload);
            return payload + (long)size <= limit;
        }

        /// <summary>How many bytes a variable length integer occupies, from the position of the first set bit
        /// in its leading byte. Zero means the byte is not a valid marker.</summary>
        private static int MarkerLength(byte lead)
        {
            for (var length = 1; length <= 8; length++)
                if ((lead & (0x100 >> length)) != 0)
                    return length;
            return 0;
        }
    }

    // ---- ASF (WMA, WMV) -----------------------------------------------------------------------------

    /// <summary>ASF, from the File Properties Object in the Advanced Systems Format specification.</summary>
    /// <remarks>
    /// The file opens with the Header Object: a 16 byte GUID, a 64 bit size, a count of the objects inside
    /// it, then two reserved bytes - so its children start at offset 30. Each child is itself a GUID and a
    /// 64 bit size.
    ///
    /// In the File Properties Object, Play Duration sits at offset 64 in hundred nanosecond units and Preroll
    /// at offset 80 in milliseconds. The specification is explicit that the preroll has already been added
    /// into the play duration, so it has to be taken back off.
    /// </remarks>
    private static class Asf
    {
        /// <summary>The first four bytes of the Header Object GUID, 75B22630-668E-11CF-A6D9-00AA0062CE6C,
        /// which is stored with its first three groups little endian.</summary>
        internal static ReadOnlySpan<byte> HeaderGuidPrefix => [0x30, 0x26, 0xB2, 0x75];

        private static ReadOnlySpan<byte> FilePropertiesGuid =>
            [0xA1, 0xDC, 0xAB, 0x8C, 0x47, 0xA9, 0xCF, 0x11, 0x8E, 0xE4, 0x00, 0xC0, 0x0C, 0x20, 0x53, 0x65];

        internal static double? Read(FileStream stream)
        {
            Span<byte> header = stackalloc byte[30];
            if (!ReadAt(stream, 0, header))
                return null;
            var headerSize = (long)LE64(header, 16);
            if (headerSize <= 30 || headerSize > stream.Length)
                headerSize = stream.Length;

            var position = 30L;
            Span<byte> child = stackalloc byte[104];
            while (position + 24 <= headerSize)
            {
                if (!ReadAt(stream, position, child[..24]))
                    return null;
                var size = (long)LE64(child, 16);
                if (size < 24 || position + size > headerSize)
                    return null;
                if (child[..16].SequenceEqual(FilePropertiesGuid) && size >= 104
                    && ReadAt(stream, position, child))
                {
                    var hundredNanoseconds = LE64(child, 64);
                    var prerollMilliseconds = LE64(child, 80);
                    var seconds = hundredNanoseconds / 10_000_000.0 - prerollMilliseconds / 1000.0;
                    return seconds > 0 ? seconds : null;
                }
                position += size;
            }
            return null;
        }
    }

    // ---- RIFF (WAV, AVI) ----------------------------------------------------------------------------

    /// <summary>RIFF containers: WAVE from its format and data chunks, AVI from its main header.</summary>
    /// <remarks>
    /// A RIFF file is a four character type, a 32 bit size and a form type, then a series of chunks each with
    /// their own four character type and 32 bit size, padded to an even length.
    ///
    /// For WAVE the length is the size of the data chunk over the average bytes per second declared in the
    /// format chunk. A compressed WAVE states its sample count in a fact chunk instead, which is used in
    /// preference because the byte rate of a variable rate codec is only an average. RF64 carries a 64 bit
    /// data size in a ds64 chunk, for the files too large for a 32 bit one.
    ///
    /// For AVI the main header gives the number of microseconds each frame lasts and how many frames there
    /// are, which multiply out to the length.
    /// </remarks>
    private static class Riff
    {
        internal static double? ReadWave(FileStream stream)
        {
            uint byteRate = 0, samplesPerSecond = 0, factSamples = 0;
            long dataSize = -1, dataSize64 = -1;
            Span<byte> body = stackalloc byte[32];
            foreach (var (type, offset, size) in Chunks(stream, 12))
            {
                if (type.SequenceEqual("fmt "u8) && size >= 16 && ReadAt(stream, offset, body[..16]))
                {
                    samplesPerSecond = LE32(body, 4);
                    byteRate = LE32(body, 8);
                }
                else if (type.SequenceEqual("data"u8))
                {
                    dataSize = size;
                }
                else if (type.SequenceEqual("fact"u8) && size >= 4 && ReadAt(stream, offset, body[..4]))
                {
                    factSamples = LE32(body, 0);
                }
                else if (type.SequenceEqual("ds64"u8) && size >= 16 && ReadAt(stream, offset, body[..16]))
                {
                    // ds64 holds the real RIFF and data sizes when the 32 bit fields are placeholders.
                    dataSize64 = (long)LE64(body, 8);
                }
            }
            if (dataSize64 >= 0) dataSize = dataSize64;
            if (factSamples > 0 && samplesPerSecond > 0)
                return (double)factSamples / samplesPerSecond;
            return dataSize > 0 && byteRate > 0 ? (double)dataSize / byteRate : null;
        }

        internal static double? ReadAvi(FileStream stream)
        {
            // avih lives inside the hdrl list, so the list header is stepped over to reach it.
            Span<byte> form = stackalloc byte[4];
            Span<byte> header = stackalloc byte[32];
            foreach (var (type, offset, size) in Chunks(stream, 12))
            {
                if (!type.SequenceEqual("LIST"u8) || size < 4)
                    continue;
                if (!ReadAt(stream, offset, form) || !form.SequenceEqual("hdrl"u8))
                    continue;
                foreach (var (inner, innerOffset, innerSize) in Chunks(stream, offset + 4, offset + size))
                {
                    if (!inner.SequenceEqual("avih"u8) || innerSize < 32)
                        continue;
                    if (!ReadAt(stream, innerOffset, header))
                        return null;
                    double microsecondsPerFrame = LE32(header, 0);
                    double totalFrames = LE32(header, 16);
                    return microsecondsPerFrame > 0 && totalFrames > 0
                        ? microsecondsPerFrame * totalFrames / 1_000_000
                        : null;
                }
            }
            return null;
        }

        /// <summary>The chunks of a RIFF file, as type, payload offset and payload size.</summary>
        private static IEnumerable<(byte[] Type, long Offset, long Size)> Chunks(
            FileStream stream, long from, long to = -1)
        {
            var limit = to < 0 ? stream.Length : Math.Min(to, stream.Length);
            var position = from;
            var header = new byte[8];
            while (position + 8 <= limit)
            {
                stream.Position = position;
                if (stream.ReadAtLeast(header, 8, throwOnEndOfStream: false) != 8)
                    yield break;
                long size = LE32(header, 4);
                var payload = position + 8;
                if (size < 0 || payload + size > limit)
                    size = limit - payload;
                yield return (header[..4], payload, size);
                // Chunks are padded to an even boundary, and the pad byte is not counted in the size.
                position = payload + size + (size & 1);
            }
        }
    }

    // ---- AIFF and AIFF-C ----------------------------------------------------------------------------

    /// <summary>AIFF, from the Common chunk in the Audio Interchange File Format specification.</summary>
    /// <remarks>
    /// AIFF is IFF rather than RIFF, so its chunk sizes are big endian, but the shape is the same. The Common
    /// chunk gives the channel count, the number of sample frames, the sample size and then the sample rate as
    /// an 80 bit SANE extended float: a sign bit, a 15 bit exponent biased by 16383, and a 64 bit mantissa
    /// whose top bit is explicit rather than implied. The length is the frame count over that rate.
    ///
    /// AIFF-C compressed with a variable rate codec states no frame count that can be trusted, but the common
    /// case of AIFF-C is uncompressed or a fixed rate codec, where the count is exact.
    /// </remarks>
    private static class Aiff
    {
        internal static double? Read(FileStream stream)
        {
            var position = 12L;
            var header = new byte[8];
            Span<byte> common = stackalloc byte[18];
            while (position + 8 <= stream.Length)
            {
                stream.Position = position;
                if (stream.ReadAtLeast(header, 8, throwOnEndOfStream: false) != 8)
                    return null;
                long size = BE32(header, 4);
                var payload = position + 8;
                if (size < 0 || payload + size > stream.Length)
                    return null;
                if (header.AsSpan(0, 4).SequenceEqual("COMM"u8) && size >= 18 && ReadAt(stream, payload, common))
                {
                    var frames = BE32(common, 2);
                    var rate = Extended80(common[8..18]);
                    return frames > 0 && rate > 0 ? frames / rate : null;
                }
                position = payload + size + (size & 1);
            }
            return null;
        }

        /// <summary>An 80 bit SANE extended float as a double.</summary>
        private static double Extended80(ReadOnlySpan<byte> b)
        {
            var exponent = ((b[0] & 0x7F) << 8) | b[1];
            var mantissa = BE64(b, 2);
            if (exponent == 0 && mantissa == 0)
                return 0;
            // The bias is 16383 and the mantissa's 63 fractional bits are scaled off; the leading bit is
            // stored rather than implied, so no extra one is added back.
            var value = mantissa * Math.Pow(2, exponent - 16383 - 63);
            return (b[0] & 0x80) != 0 ? -value : value;
        }
    }

    // ---- FLV ----------------------------------------------------------------------------------------

    /// <summary>FLV, from the duration written into its onMetaData script tag.</summary>
    /// <remarks>
    /// The nine byte file header is followed by tags, each with a type, a 24 bit payload size, a timestamp and
    /// a stream id, and preceded by the size of the tag before it. A script tag (type 18) holds AMF0 data,
    /// which for the first tag is the string <c>onMetaData</c> followed by an array of properties. The
    /// properties are name-value pairs, so the scan looks for the name <c>duration</c> and reads the double
    /// that follows its one byte type marker.
    /// </remarks>
    private static class Flv
    {
        internal static double? Read(FileStream stream)
        {
            Span<byte> header = stackalloc byte[9];
            if (!ReadAt(stream, 0, header))
                return null;
            var dataOffset = BE32(header, 5);
            if (dataOffset < 9 || dataOffset > stream.Length)
                return null;

            Span<byte> tag = stackalloc byte[11];
            if (!ReadAt(stream, dataOffset + 4, tag))
                return null;
            // Only the first tag is looked at: onMetaData is required to be it when it is present at all.
            if ((tag[0] & 0x1F) != 18)
                return null;
            var size = (int)(tag[1] << 16 | tag[2] << 8 | tag[3]);
            if (size is <= 0 or > ProbeSize)
                return null;

            var buffer = new byte[size];
            var body = ReadWindow(stream, dataOffset + 4 + 11, buffer, size);
            // An AMF0 property name is a 16 bit length then the characters, with no type marker.
            var name = body.IndexOf("duration"u8);
            if (name < 2 || BE16(body, name - 2) != 8)
                return null;
            var value = name + 8;
            // Type 0 is a double, big endian.
            if (value + 9 > body.Length || body[value] != 0)
                return null;
            return BitConverter.Int64BitsToDouble((long)BE64(body, value + 1));
        }
    }

    // ---- MPEG audio (MP1, MP2, MP3) -----------------------------------------------------------------

    /// <summary>MPEG audio, from a Xing, Info or VBRI header, or failing that from the bit rate.</summary>
    /// <remarks>
    /// A frame begins with eleven set bits, then the MPEG version, the layer, a bit rate index and a sample
    /// rate index. A variable rate encoder writes the total frame count into a Xing or Info header placed in
    /// the first frame after the side information, or into a VBRI header at a fixed offset; either gives an
    /// exact length, as frames times samples per frame over the sample rate.
    ///
    /// Without one of those the only estimate available is the file size over the bit rate, which is exact for
    /// a constant rate file and approximate otherwise. That is the same fallback the reference decoders use -
    /// mpv logs "estimating duration from bitrate" and does exactly this - so it is no worse than decoding,
    /// and the tags at either end are discounted first so the estimate is over the audio alone.
    /// </remarks>
    private static class Mpeg
    {
        private static readonly int[,] BitRates =
        {
            // MPEG 1: layer I, II, III
            { 0, 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 0 },
            { 0, 32, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 384, 0 },
            { 0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0 },
            // MPEG 2 and 2.5: layer I, then II and III which share a table
            { 0, 32, 48, 56, 64, 80, 96, 112, 128, 144, 160, 176, 192, 224, 256, 0 },
            { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },
            { 0, 8, 16, 24, 32, 40, 48, 56, 64, 80, 96, 112, 128, 144, 160, 0 },
        };

        private static readonly int[,] SampleRates =
        {
            { 44100, 48000, 32000, 0 }, // MPEG 1
            { 22050, 24000, 16000, 0 }, // MPEG 2
            { 11025, 12000, 8000, 0 },  // MPEG 2.5
        };

        internal static double? Read(FileStream stream, ReadOnlySpan<byte> head)
        {
            var audioStart = 0L;
            if (head[..3].SequenceEqual("ID3"u8))
            {
                // An ID3v2 size is four bytes of seven bits each, not counting the ten byte header, plus a
                // ten byte footer when the flags say there is one.
                audioStart = 10
                    + ((head[6] & 0x7F) << 21 | (head[7] & 0x7F) << 14 | (head[8] & 0x7F) << 7 | (head[9] & 0x7F))
                    + ((head[5] & 0x10) != 0 ? 10 : 0);
            }
            var audioEnd = stream.Length;
            audioEnd -= TrailingTagSize(stream, audioEnd);

            var buffer = new byte[ProbeSize];
            var window = ReadWindow(stream, audioStart, buffer, ProbeSize);
            for (var i = 0; i + 4 <= window.Length; i++)
            {
                if (window[i] != 0xFF || (window[i + 1] & 0xE0) != 0xE0)
                    continue;
                if (!Frame(window[(i + 1)..], out var frame))
                    continue;
                // A sync pattern turns up inside tag art and inside other formats' payloads often enough that
                // one is meaningless. Insist on a run of frames that agree with each other about the stream
                // they belong to, each landing exactly where the one before said it would.
                if (!Run(window, i, frame))
                    continue;

                var exact = Exact(window, i, frame);
                if (exact is double seconds)
                    return seconds;
                var bytes = audioEnd - (audioStart + i);
                return bytes > 0 ? bytes * 8.0 / (frame.BitRate * 1000) : null;
            }
            return null;
        }

        /// <summary>How many frames in a row have to line up before a sync is believed.</summary>
        private const int RunLength = 4;

        /// <summary>Whether a run of consistent frames starts here.</summary>
        private static bool Run(ReadOnlySpan<byte> window, int start, FrameHeader first)
        {
            var position = start;
            var frame = first;
            for (var found = 1; found < RunLength; found++)
            {
                position += frame.Length;
                // A file that simply ends is not evidence against the frames already seen.
                if (position + 4 > window.Length)
                    return true;
                if (window[position] != 0xFF || (window[position + 1] & 0xE0) != 0xE0
                    || !Frame(window[(position + 1)..], out var next)
                    || next.SampleRate != first.SampleRate || next.Version != first.Version
                    || next.Layer != first.Layer)
                    return false;
                frame = next;
            }
            return true;
        }

        private readonly record struct FrameHeader(
            int Version, int Layer, int BitRate, int SampleRate, int SamplesPerFrame, int Length, int SideInfo);

        private static bool Frame(ReadOnlySpan<byte> b, out FrameHeader frame)
        {
            frame = default;
            if (b.Length < 3)
                return false;
            var versionBits = (b[0] >> 3) & 0x03; // 0 = 2.5, 2 = 2, 3 = 1
            var layerBits = (b[0] >> 1) & 0x03;   // 3 = I, 2 = II, 1 = III
            if (versionBits == 1 || layerBits == 0)
                return false;
            var rateIndex = (b[1] >> 4) & 0x0F;
            var freqIndex = (b[1] >> 2) & 0x03;
            if (rateIndex is 0 or 15 || freqIndex == 3)
                return false;

            var version = versionBits == 3 ? 0 : versionBits == 2 ? 1 : 2;
            var layer = 3 - layerBits;                       // 0 = I, 1 = II, 2 = III
            var bitRate = BitRates[version == 0 ? layer : 3 + layer, rateIndex];
            var sampleRate = SampleRates[version, freqIndex];
            if (bitRate == 0 || sampleRate == 0)
                return false;

            // Layer I carries 384 samples a frame; layer II always 1152; layer III 1152 for MPEG 1 and 576
            // for the half rate versions.
            var samples = layer == 0 ? 384 : layer == 1 ? 1152 : version == 0 ? 1152 : 576;
            var padding = (b[1] >> 1) & 0x01;
            var length = layer == 0
                ? (12 * bitRate * 1000 / sampleRate + padding) * 4
                : samples / 8 * bitRate * 1000 / sampleRate + padding;
            var channelMode = (b[2] >> 6) & 0x03;
            var sideInfo = version == 0 ? (channelMode == 3 ? 17 : 32) : (channelMode == 3 ? 9 : 17);
            frame = new FrameHeader(version, layer, bitRate, sampleRate, samples, length, sideInfo);
            return length > 0;
        }

        /// <summary>The length from a Xing, Info or VBRI header, when the file carries one.</summary>
        private static double? Exact(ReadOnlySpan<byte> window, int frameStart, FrameHeader frame)
        {
            var xing = frameStart + 4 + frame.SideInfo;
            if (xing + 12 <= window.Length
                && (window.Slice(xing, 4).SequenceEqual("Xing"u8) || window.Slice(xing, 4).SequenceEqual("Info"u8)))
            {
                // The first flag bit says whether the frame count is present; it is the first field after.
                if ((BE32(window, xing + 4) & 1) != 0)
                {
                    var frames = BE32(window, xing + 8);
                    if (frames > 0)
                        return (double)frames * frame.SamplesPerFrame / frame.SampleRate;
                }
            }
            // VBRI is written by the Fraunhofer encoder at a fixed 32 bytes past the end of the frame header.
            var vbri = frameStart + 36;
            if (vbri + 26 <= window.Length && window.Slice(vbri, 4).SequenceEqual("VBRI"u8))
            {
                var frames = BE32(window, vbri + 14);
                if (frames > 0)
                    return (double)frames * frame.SamplesPerFrame / frame.SampleRate;
            }
            return null;
        }

        /// <summary>How many bytes of tag sit at the end of the file, so a size based estimate can leave them
        /// out. ID3v1 is a fixed 128 bytes; APE states its own size in a footer.</summary>
        private static long TrailingTagSize(FileStream stream, long end)
        {
            long size = 0;
            Span<byte> tail = stackalloc byte[32];
            if (end >= 128 && ReadAt(stream, end - 128, tail[..3]) && tail[..3].SequenceEqual("TAG"u8))
            {
                size = 128;
                end -= 128;
            }
            if (end >= 32 && ReadAt(stream, end - 32, tail) && tail[..8].SequenceEqual("APETAGEX"u8))
                size += LE32(tail, 12) + 32;
            return size;
        }
    }
}
