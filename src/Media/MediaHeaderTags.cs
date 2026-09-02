using System.Text;

namespace LunaPlayer.Media;

/// <summary>What a file says about itself: the three fields the Windows media overlay shows.</summary>
/// <remarks>
/// Empty strings rather than nulls, because every consumer wants to display them and none of them wants to
/// tell "no artist" apart from "an artist of nothing".
/// </remarks>
internal readonly record struct MediaTags(string Title, string Artist, string Album)
{
    /// <summary>What a file that says nothing about itself reads as.</summary>
    internal static MediaTags None { get; } = new(string.Empty, string.Empty, string.Empty);
}

/// <summary>The tag half of the header reader: what a file calls itself, rather than how long it is.</summary>
///
/// <remarks>
/// Every container in <see cref="MediaHeader"/> carries its own tagging scheme, and they share almost nothing
/// with one another. What is here is one reader per scheme, dispatched on the container the magic bytes named,
/// so the same file is understood the same way whatever it happens to be called:
///
/// <list type="bullet">
/// <item>Vorbis comments, in FLAC metadata blocks and in the second packet of every Ogg mapping.</item>
/// <item>ID3v2.2, v2.3 and v2.4, APEv2 and ID3v1, for MPEG audio and for the RIFF and AIFF chunks that
/// embed them.</item>
/// <item>The iTunes-style <c>ilst</c> item list and the 3GPP user data boxes, for ISO base media.</item>
/// <item>Matroska tags, with the target that says whether a title names the track or the album.</item>
/// <item>The ASF Content Description and Extended Content Description objects.</item>
/// <item>RIFF <c>LIST INFO</c>, and the AIFF text chunks.</item>
/// </list>
///
/// This is deliberately not asked of mpv. mpv hands back whatever key the demuxer happened to produce, whose
/// spelling and case follow the container rather than any convention, so reading an artist out of it means
/// guessing at a name; and it costs a demuxer where a few small reads will do.
///
/// FLV is the one container here with no tags to read. Its <c>onMetaData</c> block is a fixed set of
/// properties about the stream - dimensions, frame rate, codec ids - with nothing in it that names the work.
/// </remarks>
internal static partial class MediaHeader
{
    /// <summary>The most of any single tag block that is read. Enough for any amount of text; short of the
    /// cover art that makes a tag large, which is not wanted and need not be paid for.</summary>
    private const int TagLimit = 1024 * 1024;

    /// <summary>The most of one field that is kept. A tag is free to hold a novel; a display is not.</summary>
    private const int FieldLimit = 512;

    /// <summary>Reads what a file says about itself, from its header alone.</summary>
    /// <remarks>
    /// Called when a file is opened rather than for every file in a scan, so unlike
    /// <see cref="Read(string, out double)"/> it can afford to read a tag block whole. The length that
    /// <see cref="Identify"/> works out along the way is discarded; sharing the one dispatch is worth more
    /// than saving a parse that happens once per file the user actually plays.
    /// </remarks>
    internal static MediaTags ReadTags(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,
                bufferSize: 0, FileOptions.RandomAccess);
            if (stream.Length < 16)
                return MediaTags.None;
            Span<byte> head = stackalloc byte[16];
            if (!ReadAt(stream, 0, head))
                return MediaTags.None;

            var into = new Collector();
            switch (Identify(stream, head).Format)
            {
                case MediaFormat.Flac: TagReaders.FromFlac(stream, into); break;
                case MediaFormat.Ogg: TagReaders.FromOgg(stream, into); break;
                case MediaFormat.Matroska: TagReaders.FromMatroska(stream, into); break;
                case MediaFormat.Asf: TagReaders.FromAsf(stream, into); break;
                case MediaFormat.IsoBaseMedia: TagReaders.FromIsoBaseMedia(stream, into); break;
                case MediaFormat.Wave or MediaFormat.Avi: TagReaders.FromRiff(stream, into); break;
                case MediaFormat.Aiff: TagReaders.FromAiff(stream, into); break;
                case MediaFormat.Mpeg: TagReaders.FromMpeg(stream, into); break;
            }
            return into.Result;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or NotSupportedException or ArgumentException or OutOfMemoryException)
        {
            return MediaTags.None;
        }
    }

    /// <summary>Gathers the fields as the readers find them.</summary>
    ///
    /// <remarks>
    /// The first value seen for a field is the one kept. A file can carry the same field twice - an ID3v2 tag
    /// at the front and an ID3v1 at the back, a Matroska tag at track level and another at album level - and
    /// the readers are ordered so that the better source is asked first.
    ///
    /// The album artist is collected separately rather than merged into the artist, because it is only a
    /// fallback: on a compilation the two differ, and the track artist is the one that names what is playing.
    /// </remarks>
    private sealed class Collector
    {
        private string? _title;
        private string? _artist;
        private string? _album;
        private string? _albumArtist;

        internal void Title(string? value) => Keep(ref _title, value);
        internal void Artist(string? value) => Keep(ref _artist, value);
        internal void Album(string? value) => Keep(ref _album, value);
        internal void AlbumArtist(string? value) => Keep(ref _albumArtist, value);

        /// <summary>Whether anything is still missing, so a reader can stop once it has all three.</summary>
        internal bool Wants => _title is null || _artist is null || _album is null;

        internal MediaTags Result =>
            new(_title ?? string.Empty, _artist ?? _albumArtist ?? string.Empty, _album ?? string.Empty);

        private static void Keep(ref string? field, string? value)
        {
            if (field is not null)
                return;
            var text = Clean(value);
            if (text.Length > 0)
                field = text;
        }

        /// <summary>Trims a value and drops the control characters tags collect - the trailing nulls of a
        /// fixed width field, the stray line breaks of a badly written editor.</summary>
        private static string Clean(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            Span<char> buffer = value.Length <= FieldLimit
                ? stackalloc char[value.Length]
                : new char[FieldLimit];
            var length = 0;
            foreach (var character in value)
            {
                if (length == buffer.Length)
                    break;
                if (char.IsControl(character) || character == '﻿')
                    continue;
                buffer[length++] = character;
            }
            return new string(buffer[..length]).Trim();
        }
    }

    // ---- shared text decoding -----------------------------------------------------------------------

    /// <summary>The first value of a field, for the formats that pack several into one by separating them
    /// with a null.</summary>
    private static string FirstValue(string text)
    {
        var end = text.IndexOf('\0');
        return end < 0 ? text : text[..end];
    }

    /// <summary>UTF-16 whose byte order is stated by a mark at the front, as ID3v2 and the 3GPP boxes write
    /// it. Little endian is assumed when there is no mark, which is what every writer produces in practice.
    /// </summary>
    private static string Utf16WithMark(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes[2..]);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes[2..]);
        return Encoding.Unicode.GetString(bytes);
    }

    /// <summary>Reads a run of bytes and decodes it, for the elements whose length the container states.
    /// </summary>
    private static string ReadText(FileStream stream, long offset, long size, Encoding encoding)
    {
        if (size <= 0 || size > FieldLimit * 4)
            return string.Empty;
        var buffer = new byte[(int)size];
        return FirstValue(encoding.GetString(ReadWindow(stream, offset, buffer, (int)size)));
    }

    /// <summary>A decoder that refuses anything that is not UTF-8, rather than papering over it with
    /// replacement characters.</summary>
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    /// <summary>Text from a chunk whose format states no encoding, which RIFF and AIFF both leave open.
    /// </summary>
    /// <remarks>
    /// Both were specified when text meant one byte a character, and writers have gone their own ways since:
    /// some write UTF-8, some write whatever single byte code page the machine was set to. Which it is can be
    /// told apart by trying, because UTF-8 is self-describing - a multi-byte sequence has a shape that single
    /// byte text does not produce by accident - so text that decodes cleanly as UTF-8 was UTF-8, and text
    /// that does not is read as Latin-1, which at least renders the ASCII correctly and leaves the rest
    /// legible rather than replacing it.
    /// </remarks>
    private static string ReadOpenText(FileStream stream, long offset, long size)
    {
        if (size <= 0 || size > FieldLimit * 4)
            return string.Empty;
        var bytes = ReadWindow(stream, offset, new byte[(int)size], (int)size);
        try
        {
            return FirstValue(StrictUtf8.GetString(bytes));
        }
        catch (DecoderFallbackException)
        {
            return FirstValue(Encoding.Latin1.GetString(bytes));
        }
    }

    private static class TagReaders
    {
        // ---- Vorbis comments ------------------------------------------------------------------------

        /// <summary>A Vorbis comment block, as used by FLAC and by every Ogg mapping.</summary>
        /// <remarks>
        /// From the Vorbis I specification, section 5: a 32 bit little endian vendor length and the vendor
        /// string, a 32 bit count, then that many entries, each a 32 bit length and that many bytes of UTF-8
        /// reading <c>NAME=value</c>. Field names are case insensitive and are compared as such; the values
        /// are UTF-8 by definition, so there is no encoding to guess at.
        /// </remarks>
        private static void VorbisComment(ReadOnlySpan<byte> body, Collector into)
        {
            if (body.Length < 8)
                return;
            long at = 4 + LE32(body, 0);
            if (at + 4 > body.Length)
                return;
            var count = LE32(body, (int)at);
            at += 4;
            for (var i = 0; i < count && at + 4 <= body.Length; i++)
            {
                var length = LE32(body, (int)at);
                at += 4;
                if (length > body.Length - at)
                    return;
                var entry = body.Slice((int)at, (int)length);
                at += length;
                var separator = entry.IndexOf((byte)'=');
                if (separator <= 0)
                    continue;
                var name = Encoding.ASCII.GetString(entry[..separator]);
                var value = Encoding.UTF8.GetString(entry[(separator + 1)..]);
                switch (name.ToUpperInvariant())
                {
                    case "TITLE": into.Title(value); break;
                    case "ARTIST": into.Artist(value); break;
                    case "ALBUM": into.Album(value); break;
                    case "ALBUMARTIST" or "ALBUM ARTIST": into.AlbumArtist(value); break;
                }
            }
        }

        // ---- FLAC -----------------------------------------------------------------------------------

        /// <summary>FLAC keeps its tags in a VORBIS_COMMENT metadata block, type 4 in the same chain of
        /// blocks the STREAMINFO the duration comes from opens.</summary>
        internal static void FromFlac(FileStream stream, Collector into)
        {
            var position = 4L;
            Span<byte> header = stackalloc byte[4];
            // A file states no block count, so the walk is bounded by the last-block bit and by a limit that
            // stops a corrupt chain from being followed forever.
            for (var block = 0; block < 128; block++)
            {
                if (!ReadAt(stream, position, header))
                    return;
                var last = (header[0] & 0x80) != 0;
                var type = header[0] & 0x7F;
                var size = header[1] << 16 | header[2] << 8 | header[3];
                var payload = position + 4;
                if (payload + size > stream.Length)
                    return;
                if (type == 4)
                {
                    if (size is > 0 and <= TagLimit)
                        VorbisComment(ReadWindow(stream, payload, new byte[size], size), into);
                    return;
                }
                if (last)
                    return;
                position = payload + size;
            }
        }

        // ---- Ogg ------------------------------------------------------------------------------------

        /// <summary>How much of an Ogg file is read looking for its comment packet. The identification and
        /// comment headers are required to come first, so this is generous.</summary>
        private const int OggWindow = 512 * 1024;

        /// <summary>Ogg keeps its tags in the second packet of the logical stream, in a form each mapping
        /// wraps differently but which is a Vorbis comment underneath in every case.</summary>
        /// <remarks>
        /// Vorbis I names the comment header packet type 3 and prefixes it <c>\x03vorbis</c>; RFC 7845 calls
        /// the Opus one <c>OpusTags</c>; the FLAC mapping sends native metadata blocks one to a packet;
        /// Speex sends the comment raw; Theora prefixes it <c>\x81theora</c>.
        /// </remarks>
        internal static void FromOgg(FileStream stream, Collector into)
        {
            var packets = OggPackets(ReadWindow(stream, 0, new byte[OggWindow], OggWindow), maximum: 8);
            if (packets.Count == 0)
                return;
            var first = (ReadOnlySpan<byte>)packets[0];

            if (first.StartsWith("OpusHead"u8))
            {
                foreach (var packet in packets)
                {
                    if (packet.AsSpan().StartsWith("OpusTags"u8))
                    {
                        VorbisComment(packet.AsSpan(8), into);
                        return;
                    }
                }
            }
            else if (first.StartsWith(VorbisIdentification))
            {
                foreach (var packet in packets)
                {
                    if (packet.AsSpan().StartsWith(VorbisComments))
                    {
                        VorbisComment(packet.AsSpan(7), into);
                        return;
                    }
                }
            }
            else if (first.StartsWith(FlacMapping))
            {
                // Every packet after the first is one native FLAC metadata block, header and all.
                for (var i = 1; i < packets.Count; i++)
                {
                    if (packets[i].Length > 4 && (packets[i][0] & 0x7F) == 4)
                    {
                        VorbisComment(packets[i].AsSpan(4), into);
                        return;
                    }
                }
            }
            else if (first.StartsWith("Speex   "u8))
            {
                if (packets.Count > 1)
                    VorbisComment(packets[1], into);
            }
            else if (first.StartsWith(TheoraIdentification))
            {
                foreach (var packet in packets)
                {
                    if (packet.AsSpan().StartsWith(TheoraComments))
                    {
                        VorbisComment(packet.AsSpan(7), into);
                        return;
                    }
                }
            }
        }

        private static ReadOnlySpan<byte> VorbisIdentification => [0x01, (byte)'v', (byte)'o', (byte)'r', (byte)'b', (byte)'i', (byte)'s'];
        private static ReadOnlySpan<byte> VorbisComments => [0x03, (byte)'v', (byte)'o', (byte)'r', (byte)'b', (byte)'i', (byte)'s'];
        private static ReadOnlySpan<byte> FlacMapping => [0x7F, (byte)'F', (byte)'L', (byte)'A', (byte)'C'];
        private static ReadOnlySpan<byte> TheoraIdentification => [0x80, (byte)'t', (byte)'h', (byte)'e', (byte)'o', (byte)'r', (byte)'a'];
        private static ReadOnlySpan<byte> TheoraComments => [0x81, (byte)'t', (byte)'h', (byte)'e', (byte)'o', (byte)'r', (byte)'a'];

        /// <summary>Reassembles the first few packets of the first logical stream in an Ogg file.</summary>
        /// <remarks>
        /// From RFC 3533: a page is <c>OggS</c>, a version and flags byte, the granule position, the stream
        /// serial number, the page number, a checksum, then a count of segments and that many lacing values.
        /// The payload is those segments end to end, and a packet runs until a segment shorter than 255
        /// bytes finishes it - which is why a packet has to be gathered rather than pointed at: it may be
        /// spread over several pages, and other streams may have pages in between.
        /// </remarks>
        private static List<byte[]> OggPackets(ReadOnlySpan<byte> data, int maximum)
        {
            var packets = new List<byte[]>();
            var pending = new List<byte>();
            uint serial = 0;
            var known = false;
            var at = 0;
            while (at + 27 <= data.Length && packets.Count < maximum)
            {
                if (!data.Slice(at, 4).SequenceEqual("OggS"u8))
                    break;
                var segments = data[at + 26];
                var lacing = at + 27;
                if (lacing + segments > data.Length)
                    break;
                var payload = lacing + segments;
                var total = 0;
                for (var i = 0; i < segments; i++)
                    total += data[lacing + i];
                if (payload + total > data.Length)
                    break;

                var pageSerial = LE32(data, at + 14);
                if (!known)
                {
                    serial = pageSerial;
                    known = true;
                }
                if (pageSerial == serial)
                {
                    var offset = payload;
                    for (var i = 0; i < segments; i++)
                    {
                        var length = data[lacing + i];
                        // A packet larger than any tag could sensibly be is not one worth gathering.
                        if (pending.Count + length > TagLimit)
                            return packets;
                        pending.AddRange(data.Slice(offset, length));
                        offset += length;
                        if (length == 255)
                            continue;
                        packets.Add([.. pending]);
                        pending.Clear();
                        if (packets.Count >= maximum)
                            break;
                    }
                }
                at = payload + total;
            }
            return packets;
        }

        // ---- ISO base media (MP4, M4A, M4V, MOV, 3GP) -----------------------------------------------

        /// <summary>ISO base media files tag themselves in the user data box, in either of two ways.</summary>
        /// <remarks>
        /// The one everything writes is the iTunes item list: <c>udta/meta/ilst</c>, holding a box per field
        /// whose type is the field name - <c>©nam</c>, <c>©ART</c>, <c>©alb</c>, <c>aART</c> - and whose
        /// contents are a <c>data</c> box stating a well-known type and then the text.
        ///
        /// The other two put their fields straight into user data with no item list around them. QuickTime
        /// writes the same <c>©</c> names as text entries of its own shape, which is what a <c>.mov</c>
        /// carries; 3GPP TS 26.244 defines <c>titl</c>, <c>perf</c> and <c>albm</c>, each a version and
        /// flags, a packed language code, then a null-terminated string.
        ///
        /// All three are read, the item list first, since a file carrying more than one is an iTunes file
        /// with the others left over from wherever it came from.
        /// </remarks>
        internal static void FromIsoBaseMedia(FileStream stream, Collector into)
        {
            if (IsoBaseMedia.FindBox(stream, 0, stream.Length, "moov"u8) is not (var moovStart, var moovEnd))
                return;
            if (IsoBaseMedia.FindBox(stream, moovStart, moovEnd, "udta"u8) is (var udtaStart, var udtaEnd))
            {
                FromItemList(stream, udtaStart, udtaEnd, into);
                FromQuickTimeUserData(stream, udtaStart, udtaEnd, into);
                From3Gpp(stream, udtaStart, udtaEnd, into);
            }
            // QuickTime is entitled to put the metadata box straight in the movie box instead.
            FromItemList(stream, moovStart, moovEnd, into);
        }

        private static void FromItemList(FileStream stream, long from, long to, Collector into)
        {
            if (IsoBaseMedia.FindBox(stream, from, to, "meta"u8) is not (var metaStart, var metaEnd))
                return;
            // The metadata box is a full box in ISO files - a version and three flag bytes before its
            // children - and a plain box in the QuickTime files Apple writes. Which it is shows in whether
            // the first child sits at the start or four bytes into it.
            Span<byte> probe = stackalloc byte[8];
            var children = ReadAt(stream, metaStart, probe) && LooksLikeBoxType(probe[4..8])
                ? metaStart
                : metaStart + 4;
            if (IsoBaseMedia.FindBox(stream, children, metaEnd, "ilst"u8) is not (var start, var end))
                return;

            Span<byte> header = stackalloc byte[8];
            var position = start;
            while (position + 8 <= end)
            {
                if (!ReadAt(stream, position, header))
                    return;
                long size = BE32(header, 0);
                if (size < 8 || position + size > end)
                    return;
                var value = ItemValue(stream, position + 8, position + size);
                switch (header[4..8])
                {
                    case [0xA9, (byte)'n', (byte)'a', (byte)'m']: into.Title(value); break;
                    case [0xA9, (byte)'A', (byte)'R', (byte)'T']: into.Artist(value); break;
                    case [0xA9, (byte)'a', (byte)'l', (byte)'b']: into.Album(value); break;
                    case [(byte)'a', (byte)'A', (byte)'R', (byte)'T']: into.AlbumArtist(value); break;
                }
                position += size;
            }
        }

        /// <summary>The text of one item list entry, out of the <c>data</c> box inside it.</summary>
        /// <remarks>
        /// A data box opens with a version byte and three flag bytes that together are the well-known type of
        /// what follows, then four bytes of locale. Type 1 is UTF-8 and type 2 UTF-16 big endian; anything
        /// else is a number or an image and is not text at all.
        /// </remarks>
        private static string? ItemValue(FileStream stream, long from, long to)
        {
            if (IsoBaseMedia.FindBox(stream, from, to, "data"u8) is not (var start, var end) || end - start <= 8)
                return null;
            Span<byte> header = stackalloc byte[8];
            if (!ReadAt(stream, start, header))
                return null;
            var wellKnown = BE32(header, 0) & 0x00FF_FFFF;
            return wellKnown switch
            {
                1 => ReadText(stream, start + 8, end - start - 8, Encoding.UTF8),
                2 => ReadText(stream, start + 8, end - start - 8, Encoding.BigEndianUnicode),
                _ => null,
            };
        }

        /// <summary>The QuickTime user data text boxes, from the QuickTime File Format specification.
        /// </summary>
        /// <remarks>
        /// These carry the same <c>©</c> names as an item list but none of its wrapping: the box holds one
        /// or more text entries directly, each a 16 bit length, a 16 bit language code, then that many bytes
        /// of text. Only the first entry is read - the rest are the same field in another language.
        ///
        /// The language code says how the text was written. Values from 0x400 up are packed ISO 639-2 codes
        /// and mean UTF-8; below that they are the old Macintosh language codes, whose text is in a Mac
        /// script that Latin-1 reads correctly for anything in the ASCII range and approximates above it.
        /// </remarks>
        private static void FromQuickTimeUserData(FileStream stream, long from, long to, Collector into)
        {
            Span<byte> header = stackalloc byte[8];
            Span<byte> entry = stackalloc byte[4];
            var position = from;
            while (position + 8 <= to)
            {
                if (!ReadAt(stream, position, header))
                    return;
                long size = BE32(header, 0);
                if (size < 8 || position + size > to)
                    return;
                string? text = null;
                if (size >= 12 && header[4] == 0xA9 && ReadAt(stream, position + 8, entry))
                {
                    var length = BE16(entry, 0);
                    var language = BE16(entry, 2);
                    if (length > 0 && position + 12 + length <= position + size)
                        text = ReadText(stream, position + 12, length,
                            language >= 0x400 ? Encoding.UTF8 : Encoding.Latin1);
                }
                if (text is not null)
                {
                    switch (header[4..8])
                    {
                        case [0xA9, (byte)'n', (byte)'a', (byte)'m']: into.Title(text); break;
                        case [0xA9, (byte)'A', (byte)'R', (byte)'T']: into.Artist(text); break;
                        case [0xA9, (byte)'a', (byte)'l', (byte)'b']: into.Album(text); break;
                    }
                }
                position += size;
            }
        }

        private static void From3Gpp(FileStream stream, long from, long to, Collector into)
        {
            Span<byte> header = stackalloc byte[8];
            var position = from;
            while (position + 8 <= to)
            {
                if (!ReadAt(stream, position, header))
                    return;
                long size = BE32(header, 0);
                if (size < 8 || position + size > to)
                    return;
                // A version and three flag bytes, then a packed language code, then the string itself.
                var text = size > 14 ? UserDataText(stream, position + 8 + 6, position + size) : null;
                if (text is not null)
                {
                    switch (header[4..8])
                    {
                        case [(byte)'t', (byte)'i', (byte)'t', (byte)'l']: into.Title(text); break;
                        case [(byte)'p', (byte)'e', (byte)'r', (byte)'f']: into.Artist(text); break;
                        case [(byte)'a', (byte)'u', (byte)'t', (byte)'h']: into.AlbumArtist(text); break;
                        case [(byte)'a', (byte)'l', (byte)'b', (byte)'m']: into.Album(text); break;
                    }
                }
                position += size;
            }
        }

        /// <summary>A 3GPP user data string, which is UTF-8 unless it opens with a byte order mark.</summary>
        private static string? UserDataText(FileStream stream, long from, long to)
        {
            var size = (int)Math.Min(to - from, FieldLimit * 4);
            if (size <= 0)
                return null;
            var text = ReadWindow(stream, from, new byte[size], size);
            if (text.Length >= 2 && (text[0] == 0xFE && text[1] == 0xFF || text[0] == 0xFF && text[1] == 0xFE))
                return FirstValue(Utf16WithMark(text));
            return FirstValue(Encoding.UTF8.GetString(text));
        }

        /// <summary>Whether four bytes could be a box type, which is defined as printable characters.
        /// </summary>
        private static bool LooksLikeBoxType(ReadOnlySpan<byte> type)
        {
            foreach (var character in type)
            {
                if (character is < 0x20 or > 0x7E)
                    return false;
            }
            return true;
        }

        // ---- Matroska and WebM ----------------------------------------------------------------------

        private const ulong TagsId = 0x1254C367;
        private const ulong TagId = 0x7373;
        private const ulong TargetsId = 0x63C0;
        private const ulong TargetTypeValueId = 0x68CA;
        private const ulong SimpleTagId = 0x67C8;
        private const ulong TagNameId = 0x45A3;
        private const ulong TagStringId = 0x4487;
        private const ulong SegmentTitleId = 0x7BA9;

        /// <summary>Matroska tags, from the Tags element of the Matroska specification.</summary>
        /// <remarks>
        /// A Tag holds a Targets element saying what it is about and any number of SimpleTags, each a name
        /// and a string. The target matters: Matroska has no separate album field, and says instead that a
        /// TITLE is the album name when the target is an album or higher and the track name when it is
        /// lower. TargetTypeValue 50 means album and 30 means track, and 50 is the default when the element
        /// is absent - which is why a file with no Targets at all reads as naming its album.
        /// </remarks>
        internal static void FromMatroska(FileStream stream, Collector into)
        {
            if (Matroska.Find(stream, 0, stream.Length, Matroska.SegmentId) is not (var segmentStart, var segmentEnd))
                return;
            if (Matroska.Find(stream, segmentStart, segmentEnd, TagsId) is (var tagsStart, var tagsEnd))
            {
                var position = tagsStart;
                while (position < tagsEnd)
                {
                    if (!Matroska.ReadElement(stream, position, tagsEnd, out var id, out var payload, out var size))
                        break;
                    if (id == TagId)
                        ReadTag(stream, payload, payload + (long)size, into);
                    position = payload + (long)size;
                }
            }
            // The segment title names the file as a whole. It is read last, so it only fills a gap the tags
            // left: a tagged music file has a better title than the one whoever muxed it typed in.
            if (Matroska.Find(stream, segmentStart, segmentEnd, Matroska.InfoId) is (var infoStart, var infoEnd)
                && Matroska.Find(stream, infoStart, infoEnd, SegmentTitleId) is (var titleStart, var titleEnd))
                into.Title(ReadText(stream, titleStart, titleEnd - titleStart, Encoding.UTF8));
        }

        private static void ReadTag(FileStream stream, long from, long to, Collector into)
        {
            // 50 is the specification's default, and means the tag is about the album.
            ulong target = 50;
            if (Matroska.Find(stream, from, to, TargetsId) is (var targetsStart, var targetsEnd)
                && Matroska.Find(stream, targetsStart, targetsEnd, TargetTypeValueId) is (var valueStart, var valueEnd)
                && valueEnd - valueStart is > 0 and <= 8)
            {
                Span<byte> value = stackalloc byte[8];
                var length = (int)(valueEnd - valueStart);
                if (ReadAt(stream, valueStart, value[..length]))
                {
                    ulong parsed = 0;
                    for (var i = 0; i < length; i++)
                        parsed = parsed << 8 | value[i];
                    target = parsed;
                }
            }

            var position = from;
            while (position < to)
            {
                if (!Matroska.ReadElement(stream, position, to, out var id, out var payload, out var size))
                    break;
                if (id == SimpleTagId)
                    ReadSimpleTag(stream, payload, payload + (long)size, target, into, depth: 0);
                position = payload + (long)size;
            }
        }

        private static void ReadSimpleTag(FileStream stream, long from, long to, ulong target, Collector into, int depth)
        {
            // SimpleTags nest, so a bound is needed; nothing real goes more than two or three deep.
            if (depth > 4)
                return;
            string? name = null;
            string? value = null;
            var position = from;
            while (position < to)
            {
                if (!Matroska.ReadElement(stream, position, to, out var id, out var payload, out var size))
                    break;
                var end = payload + (long)size;
                if (id == TagNameId)
                    name = ReadText(stream, payload, (long)size, Encoding.UTF8);
                else if (id == TagStringId)
                    value = ReadText(stream, payload, (long)size, Encoding.UTF8);
                else if (id == SimpleTagId)
                    ReadSimpleTag(stream, payload, end, target, into, depth + 1);
                position = end;
            }
            if (name is null || value is null)
                return;
            switch (name.ToUpperInvariant())
            {
                // A title is the album name when the tag is about an album or a larger thing, and the track
                // name when it is about the track. 40 is the boundary the specification draws.
                case "TITLE" when target >= 40: into.Album(value); break;
                case "TITLE": into.Title(value); break;
                case "ARTIST": into.Artist(value); break;
                case "ALBUM": into.Album(value); break;
                case "ALBUM ARTIST" or "ALBUMARTIST": into.AlbumArtist(value); break;
            }
        }

        // ---- ASF (WMA, WMV) -------------------------------------------------------------------------

        /// <summary>The Content Description Object GUID, 75B22633-668E-11CF-A6D9-00AA0062CE6C, whose first
        /// three groups are stored little endian.</summary>
        private static ReadOnlySpan<byte> ContentDescriptionGuid =>
            [0x33, 0x26, 0xB2, 0x75, 0x8E, 0x66, 0xCF, 0x11, 0xA6, 0xD9, 0x00, 0xAA, 0x00, 0x62, 0xCE, 0x6C];

        /// <summary>The Extended Content Description Object GUID, D2D0A440-E307-11D2-97F0-00A0C95EA850.
        /// </summary>
        private static ReadOnlySpan<byte> ExtendedContentDescriptionGuid =>
            [0x40, 0xA4, 0xD0, 0xD2, 0x07, 0xE3, 0xD2, 0x11, 0x97, 0xF0, 0x00, 0xA0, 0xC9, 0x5E, 0xA8, 0x50];

        /// <summary>ASF, from the two description objects of the Advanced Systems Format specification.
        /// </summary>
        /// <remarks>
        /// The Content Description Object holds the five fields ASF defines itself: title, author, copyright,
        /// description and rating, written as five 16 bit lengths followed by that many bytes of UTF-16
        /// little endian each, nulls included. Everything else a tagger writes - the album above all - goes
        /// in the Extended Content Description Object as named descriptors, the names being Microsoft's
        /// <c>WM/</c> attributes.
        /// </remarks>
        internal static void FromAsf(FileStream stream, Collector into)
        {
            Span<byte> header = stackalloc byte[30];
            if (!ReadAt(stream, 0, header))
                return;
            var headerSize = (long)LE64(header, 16);
            if (headerSize <= 30 || headerSize > stream.Length)
                headerSize = stream.Length;

            var position = 30L;
            Span<byte> child = stackalloc byte[24];
            while (position + 24 <= headerSize)
            {
                if (!ReadAt(stream, position, child))
                    return;
                var size = (long)LE64(child, 16);
                if (size < 24 || position + size > headerSize)
                    return;
                if (child[..16].SequenceEqual(ContentDescriptionGuid))
                    ContentDescription(stream, position + 24, position + size, into);
                else if (child[..16].SequenceEqual(ExtendedContentDescriptionGuid))
                    ExtendedContentDescription(stream, position + 24, position + size, into);
                position += size;
            }
        }

        private static void ContentDescription(FileStream stream, long from, long to, Collector into)
        {
            Span<byte> lengths = stackalloc byte[10];
            if (from + 10 > to || !ReadAt(stream, from, lengths))
                return;
            var at = from + 10;
            // Only the first two of the five fields name the work; the rest are the copyright, a free text
            // description and a rating.
            for (var field = 0; field < 5; field++)
            {
                var length = LE16(lengths, field * 2);
                if (at + length > to)
                    return;
                if (length > 0 && field < 2)
                {
                    var text = ReadText(stream, at, length, Encoding.Unicode);
                    if (field == 0) into.Title(text); else into.Artist(text);
                }
                at += length;
            }
        }

        private static void ExtendedContentDescription(FileStream stream, long from, long to, Collector into)
        {
            Span<byte> count = stackalloc byte[2];
            if (from + 2 > to || !ReadAt(stream, from, count))
                return;
            var descriptors = LE16(count, 0);
            var at = from + 2;
            Span<byte> field = stackalloc byte[2];
            for (var i = 0; i < descriptors && at + 2 <= to; i++)
            {
                if (!ReadAt(stream, at, field))
                    return;
                var nameLength = LE16(field, 0);
                at += 2;
                if (at + nameLength + 4 > to)
                    return;
                var name = ReadText(stream, at, nameLength, Encoding.Unicode);
                at += nameLength;
                if (!ReadAt(stream, at, field))
                    return;
                var type = LE16(field, 0);
                at += 2;
                if (!ReadAt(stream, at, field))
                    return;
                var valueLength = LE16(field, 0);
                at += 2;
                if (at + valueLength > to)
                    return;
                // Type 0 is a UTF-16 string; the others are numbers, booleans and raw bytes.
                if (type == 0 && valueLength > 0)
                {
                    var value = ReadText(stream, at, valueLength, Encoding.Unicode);
                    switch (name)
                    {
                        case "WM/AlbumTitle": into.Album(value); break;
                        case "WM/AlbumArtist": into.AlbumArtist(value); break;
                        case "Author" or "WM/Composer": into.Artist(value); break;
                        case "Title": into.Title(value); break;
                    }
                }
                at += valueLength;
            }
        }

        // ---- RIFF (WAV, AVI) ------------------------------------------------------------------------

        /// <summary>RIFF containers, from a LIST INFO chunk or an embedded ID3v2 tag.</summary>
        /// <remarks>
        /// The INFO list is the one tagging scheme RIFF defines: a LIST chunk whose form type is
        /// <c>INFO</c>, holding chunks named for what they hold - <c>INAM</c> the title, <c>IART</c> the
        /// artist, <c>IPRD</c> the product, which for music is the album. The text is null-terminated, in
        /// whichever encoding the writer felt like; see <see cref="ReadOpenText"/>.
        ///
        /// Taggers that want more than that write a whole ID3v2 tag into an <c>id3 </c> chunk instead, which
        /// is read here by the same parser MPEG audio uses. It is looked for first, being the richer of the
        /// two and the one a music tagger writes.
        ///
        /// An INFO list is normally at the top level, but an AVI file may nest it inside another list, so one
        /// level of lists is descended into.
        /// </remarks>
        internal static void FromRiff(FileStream stream, Collector into)
        {
            foreach (var (type, offset, size) in Riff.Chunks(stream, 12))
            {
                var name = type.AsSpan();
                if (name.SequenceEqual("id3 "u8) || name.SequenceEqual("ID3 "u8))
                    Id3v2(stream, offset, into);
            }
            Span<byte> form = stackalloc byte[4];
            foreach (var (type, offset, size) in Riff.Chunks(stream, 12))
            {
                if (!type.AsSpan().SequenceEqual("LIST"u8) || size < 4)
                    continue;
                if (!ReadAt(stream, offset, form))
                    continue;
                if (form.SequenceEqual("INFO"u8))
                {
                    InfoList(stream, offset + 4, offset + size, into);
                    continue;
                }
                foreach (var (nestedType, nestedOffset, nestedSize) in Riff.Chunks(stream, offset + 4, offset + size))
                {
                    if (!nestedType.AsSpan().SequenceEqual("LIST"u8) || nestedSize < 4)
                        continue;
                    if (ReadAt(stream, nestedOffset, form) && form.SequenceEqual("INFO"u8))
                        InfoList(stream, nestedOffset + 4, nestedOffset + nestedSize, into);
                }
            }
        }

        private static void InfoList(FileStream stream, long from, long to, Collector into)
        {
            foreach (var (type, offset, size) in Riff.Chunks(stream, from, to))
            {
                var value = ReadOpenText(stream, offset, size);
                switch (type.AsSpan())
                {
                    case [(byte)'I', (byte)'N', (byte)'A', (byte)'M']: into.Title(value); break;
                    case [(byte)'I', (byte)'A', (byte)'R', (byte)'T']: into.Artist(value); break;
                    case [(byte)'I', (byte)'P', (byte)'R', (byte)'D']: into.Album(value); break;
                }
            }
        }

        // ---- AIFF and AIFF-C ------------------------------------------------------------------------

        /// <summary>AIFF, from its text chunks and from an embedded ID3v2 tag.</summary>
        /// <remarks>
        /// The Audio Interchange File Format defines <c>NAME</c> and <c>AUTH</c> chunks holding plain text
        /// with no terminator - the chunk size is the length. It defines nothing for an album, so a tagger
        /// that wants one writes an ID3v2 tag into an <c>ID3 </c> chunk, which is read first for the same
        /// reason as in RIFF. Sizes here are big endian, AIFF being IFF rather than RIFF.
        /// </remarks>
        internal static void FromAiff(FileStream stream, Collector into)
        {
            var position = 12L;
            var header = new byte[8];
            while (position + 8 <= stream.Length)
            {
                stream.Position = position;
                if (stream.ReadAtLeast(header, 8, throwOnEndOfStream: false) != 8)
                    return;
                long size = BE32(header, 4);
                var payload = position + 8;
                if (size < 0 || payload + size > stream.Length)
                    return;
                switch (header.AsSpan(0, 4))
                {
                    case [(byte)'I', (byte)'D', (byte)'3', (byte)' ']:
                    case [(byte)'i', (byte)'d', (byte)'3', (byte)' ']:
                        Id3v2(stream, payload, into);
                        break;
                    case [(byte)'N', (byte)'A', (byte)'M', (byte)'E']:
                        into.Title(ReadOpenText(stream, payload, size));
                        break;
                    case [(byte)'A', (byte)'U', (byte)'T', (byte)'H']:
                        into.Artist(ReadOpenText(stream, payload, size));
                        break;
                }
                position = payload + size + (size & 1);
            }
        }

        // ---- MPEG audio -----------------------------------------------------------------------------

        /// <summary>MPEG audio, from whichever of the three tags it carries.</summary>
        /// <remarks>
        /// They are asked in order of how much they can say. ID3v2 at the front is what nearly every file
        /// has and the only one that is not length-limited; APEv2 before the end is what the lossless
        /// encoders write; ID3v1 in the last 128 bytes is the original, with thirty bytes a field and no
        /// encoding but Latin-1, and is worth reading only for what the others left out.
        /// </remarks>
        internal static void FromMpeg(FileStream stream, Collector into)
        {
            Span<byte> head = stackalloc byte[3];
            if (ReadAt(stream, 0, head) && head.SequenceEqual("ID3"u8))
                Id3v2(stream, 0, into);
            if (into.Wants)
                ApeTag(stream, into);
            if (into.Wants)
                Id3v1(stream, into);
        }

        /// <summary>ID3v1, the fixed 128 bytes at the end of the file.</summary>
        private static void Id3v1(FileStream stream, Collector into)
        {
            if (stream.Length < 128)
                return;
            var tag = ReadWindow(stream, stream.Length - 128, new byte[128], 128);
            if (tag.Length < 128 || !tag[..3].SequenceEqual("TAG"u8))
                return;
            into.Title(Encoding.Latin1.GetString(tag.Slice(3, 30)));
            into.Artist(Encoding.Latin1.GetString(tag.Slice(33, 30)));
            into.Album(Encoding.Latin1.GetString(tag.Slice(63, 30)));
        }

        /// <summary>APEv2, from the APE tags specification.</summary>
        /// <remarks>
        /// The tag sits at the end of the file, before an ID3v1 tag if there is one, and is found through its
        /// 32 byte footer: the marker <c>APETAGEX</c>, a version, the size of everything but a header, the
        /// number of items and a flags word. Each item is the size of its value, a flags word whose bits one
        /// and two give the value type, a null-terminated ASCII key, then the value - UTF-8 when the type
        /// says the item is text.
        /// </remarks>
        private static void ApeTag(FileStream stream, Collector into)
        {
            var end = stream.Length;
            Span<byte> footer = stackalloc byte[32];
            if (end >= 128 && ReadAt(stream, end - 128, footer[..3]) && footer[..3].SequenceEqual("TAG"u8))
                end -= 128;
            if (end < 32 || !ReadAt(stream, end - 32, footer) || !footer[..8].SequenceEqual("APETAGEX"u8))
                return;
            var size = (long)LE32(footer, 12);
            var items = LE32(footer, 16);
            if (size is <= 32 or > TagLimit || size > end)
                return;

            var length = (int)(size - 32);
            var body = ReadWindow(stream, end - size, new byte[length], length);
            var at = 0;
            for (var item = 0; item < items && at + 8 < body.Length; item++)
            {
                var valueSize = (long)LE32(body, at);
                var flags = LE32(body, at + 4);
                at += 8;
                var terminator = body[at..].IndexOf((byte)0);
                if (terminator < 0)
                    return;
                var key = Encoding.ASCII.GetString(body.Slice(at, terminator));
                at += terminator + 1;
                if (valueSize < 0 || at + valueSize > body.Length)
                    return;
                if ((flags >> 1 & 3) == 0 && valueSize > 0)
                {
                    var value = FirstValue(Encoding.UTF8.GetString(body.Slice(at, (int)valueSize)));
                    switch (key.ToUpperInvariant())
                    {
                        case "TITLE": into.Title(value); break;
                        case "ARTIST": into.Artist(value); break;
                        case "ALBUM": into.Album(value); break;
                        case "ALBUM ARTIST" or "ALBUMARTIST": into.AlbumArtist(value); break;
                    }
                }
                at += (int)valueSize;
            }
        }

        // ---- ID3v2 ----------------------------------------------------------------------------------

        /// <summary>ID3v2.2, v2.3 and v2.4, from the informal standard each of them publishes.</summary>
        /// <remarks>
        /// The ten byte header is <c>ID3</c>, a major and revision byte, a flags byte, then the size of
        /// everything after it as four bytes of seven bits each - "synchsafe", so that no run of bytes in the
        /// header can be mistaken for an audio frame sync.
        ///
        /// Frames follow. In v2.2 a frame is a three character identifier and a three byte size; in v2.3 and
        /// v2.4 a four character identifier, a four byte size and two flag bytes. The size is a plain integer
        /// in v2.3 and synchsafe in v2.4 - a difference several widely used taggers got wrong, so a v2.4 size
        /// that does not lead to another frame is retried as a plain one.
        ///
        /// A text frame is one encoding byte and then the text, and v2.4 allows several values separated by
        /// nulls, of which the first is taken.
        /// </remarks>
        private static void Id3v2(FileStream stream, long offset, Collector into)
        {
            Span<byte> header = stackalloc byte[10];
            if (!ReadAt(stream, offset, header) || !header[..3].SequenceEqual("ID3"u8))
                return;
            var major = header[3];
            if (major is < 2 or > 4)
                return;
            var flags = header[5];
            var stated = Synchsafe(header, 6);
            if (stated <= 0)
                return;

            var length = (int)Math.Min(stated, TagLimit);
            var body = ReadWindow(stream, offset + 10, new byte[length], length).ToArray().AsSpan();
            if (body.Length < 10)
                return;
            // Unsynchronisation is applied to the whole tag in v2.2 and v2.3: every 0xFF followed by a zero
            // byte has that zero inserted, and it has to come back out before anything can be measured.
            if (major < 4 && (flags & 0x80) != 0)
                body = Desynchronise(body);

            var at = 0;
            if ((flags & 0x40) != 0)
            {
                // The extended header states its own size: in v2.3 as a plain integer not counting itself,
                // in v2.4 as a synchsafe one that does.
                if (body.Length < 4)
                    return;
                at = major == 4 ? (int)Synchsafe(body, 0) : 4 + (int)BE32(body, 0);
                if (at is <= 0 or > int.MaxValue || at >= body.Length)
                    return;
            }

            var identifier = major == 2 ? 3 : 4;
            var frameHeader = major == 2 ? 6 : 10;
            while (at + frameHeader <= body.Length && into.Wants)
            {
                // A tag is padded with zeroes once the frames run out.
                if (body[at] == 0)
                    return;
                var name = Encoding.ASCII.GetString(body.Slice(at, identifier));
                var size = FrameSize(body, at, major, frameHeader);
                if (size <= 0 || at + frameHeader + size > body.Length)
                    return;

                var payload = body.Slice(at + frameHeader, (int)size);
                var usable = true;
                if (major == 3)
                {
                    // Compression and encryption, in the first two bits of the second flag byte.
                    usable = (body[at + 9] & 0xC0) == 0;
                }
                else if (major == 4)
                {
                    var frameFlags = body[at + 9];
                    usable = (frameFlags & 0x0C) == 0;
                    // A frame may be unsynchronised on its own in v2.4, and may state its decoded length in
                    // four bytes before the payload.
                    if (usable && (frameFlags & 0x01) != 0 && payload.Length > 4)
                        payload = payload[4..];
                    if (usable && (frameFlags & 0x02) != 0)
                        payload = Desynchronise(payload);
                }

                if (usable)
                {
                    var text = TextFrame(payload);
                    switch (name)
                    {
                        case "TIT2" or "TT2": into.Title(text); break;
                        case "TPE1" or "TP1": into.Artist(text); break;
                        case "TALB" or "TAL": into.Album(text); break;
                        case "TPE2" or "TP2": into.AlbumArtist(text); break;
                    }
                }
                at += frameHeader + (int)size;
            }
        }

        /// <summary>The size of one frame, allowing for the taggers that wrote a v2.4 size the v2.3 way.
        /// </summary>
        private static long FrameSize(ReadOnlySpan<byte> body, int at, int major, int frameHeader)
        {
            if (major == 2)
                return body[at + 3] << 16 | body[at + 4] << 8 | body[at + 5];
            if (major == 3)
                return BE32(body, at + 4);

            var synchsafe = Synchsafe(body, at + 4);
            var plain = (long)BE32(body, at + 4);
            // The two agree unless some byte of the size has its high bit set, which is where the mistake
            // shows. Whichever one lands on something that could be the next frame is the right reading.
            if (synchsafe == plain || Follows(body, at + frameHeader + synchsafe))
                return synchsafe;
            return Follows(body, at + frameHeader + plain) ? plain : synchsafe;
        }

        /// <summary>Whether an offset lands on the start of another frame, on the padding that ends the
        /// frames, or on the end of the tag - all of which mean the size that led there was right.</summary>
        private static bool Follows(ReadOnlySpan<byte> body, long at)
        {
            if (at == body.Length)
                return true;
            if (at < 0 || at + 4 > body.Length)
                return false;
            if (body[(int)at] == 0)
                return true;
            for (var i = 0; i < 4; i++)
            {
                var character = body[(int)at + i];
                if (character is not ((>= (byte)'A' and <= (byte)'Z') or (>= (byte)'0' and <= (byte)'9')))
                    return false;
            }
            return true;
        }

        /// <summary>The four bytes of seven bits ID3v2 states its sizes in.</summary>
        private static long Synchsafe(ReadOnlySpan<byte> bytes, int at) =>
            (long)(bytes[at] & 0x7F) << 21 | (uint)(bytes[at + 1] & 0x7F) << 14
            | (uint)(bytes[at + 2] & 0x7F) << 7 | (uint)(bytes[at + 3] & 0x7F);

        /// <summary>Undoes unsynchronisation: every 0xFF 0x00 pair becomes a single 0xFF.</summary>
        private static Span<byte> Desynchronise(Span<byte> body)
        {
            var length = 0;
            for (var i = 0; i < body.Length; i++)
            {
                body[length++] = body[i];
                if (body[i] == 0xFF && i + 1 < body.Length && body[i + 1] == 0x00)
                    i++;
            }
            return body[..length];
        }

        /// <summary>The text of a frame, decoded as its first byte says it was written.</summary>
        private static string TextFrame(ReadOnlySpan<byte> payload)
        {
            if (payload.Length < 2)
                return string.Empty;
            var text = payload[1..];
            return payload[0] switch
            {
                0 => FirstValue(Encoding.Latin1.GetString(text)),
                1 => FirstValue(Utf16WithMark(text)),
                // Big endian without a mark, and UTF-8, are both v2.4 additions.
                2 => FirstValue(Encoding.BigEndianUnicode.GetString(text)),
                3 => FirstValue(Encoding.UTF8.GetString(text)),
                _ => string.Empty,
            };
        }
    }
}
