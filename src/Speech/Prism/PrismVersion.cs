using System;
using PrismSharp.Native;

namespace PrismSharp
{
    /// <summary>
    /// The version of the Prism library actually loaded into this process.
    /// </summary>
    /// <remarks>
    /// This reports the library binary, not the headers these bindings were written against. The
    /// distinction matters when Prism is a shared library, since the copy installed on a user's
    /// machine may differ from the one an application was built against.
    /// </remarks>
    public static unsafe class PrismVersion
    {
        /// <summary>Major component of the Prism release these bindings target.</summary>
        public const int RequiredMajor = 0;

        /// <summary>Minor component of the Prism release these bindings target.</summary>
        public const int RequiredMinor = 18;

        /// <summary>Patch component of the Prism release these bindings target.</summary>
        public const int RequiredPatch = 0;

        /// <summary>
        /// The encoded version of the loaded library: <c>(major &lt;&lt; 16) | (minor &lt;&lt; 8) | patch</c>,
        /// with bits 24-31 reserved and currently zero.
        /// </summary>
        /// <remarks>
        /// The encoding orders the same way Semantic Versioning does, so encoded values may be
        /// compared as ordinary unsigned integers. The value is fixed when Prism is built, so
        /// repeated reads return the same result.
        /// </remarks>
        public static uint Raw => Methods.prism_version();

        /// <summary>Major component of the loaded library's version.</summary>
        public static int Major => (int)((Raw >> 16) & 0xFF);

        /// <summary>Minor component of the loaded library's version.</summary>
        public static int Minor => (int)((Raw >> 8) & 0xFF);

        /// <summary>Patch component of the loaded library's version.</summary>
        public static int Patch => (int)(Raw & 0xFF);

        /// <summary>
        /// The loaded library's human-readable version string, falling back to the decoded numeric
        /// version if the library returns nothing.
        /// </summary>
        public static string Text => Methods.String(Methods.prism_version_string()) ?? $"{Major}.{Minor}.{Patch}";

        /// <summary>Encodes a major/minor/patch triple the way <see cref="Raw"/> reports it.</summary>
        /// <param name="major">Major component.</param>
        /// <param name="minor">Minor component.</param>
        /// <param name="patch">Patch component.</param>
        /// <returns>The encoded version.</returns>
        public static uint Encode(int major, int minor, int patch) =>
            (uint)(((major & 0xFF) << 16) | ((minor & 0xFF) << 8) | (patch & 0xFF));

        /// <summary>Whether the loaded library is at least the given version.</summary>
        /// <param name="major">Minimum major component.</param>
        /// <param name="minor">Minimum minor component.</param>
        /// <param name="patch">Minimum patch component.</param>
        /// <returns><see langword="true"/> when the loaded library is that version or newer.</returns>
        public static bool IsAtLeast(int major, int minor, int patch) => Raw >= Encode(major, minor, patch);

        /// <summary>
        /// Throws when the loaded library predates the release these bindings describe.
        /// </summary>
        /// <remarks>
        /// Prism is pre-1.0, and its manual states that compatibility between minor releases MUST NOT
        /// be assumed merely because the major version is still zero. An ABI drift here is otherwise
        /// silent and corrupts memory rather than failing cleanly, so <see cref="Context"/> performs
        /// this check before it does anything else.
        /// </remarks>
        /// <exception cref="PlatformNotSupportedException">The loaded library is too old.</exception>
        public static void EnsureSupported()
        {
            if (IsAtLeast(RequiredMajor, RequiredMinor, RequiredPatch))
                return;

            throw new PlatformNotSupportedException(
                $"Prism {Text} is older than the {RequiredMajor}.{RequiredMinor}.{RequiredPatch} " +
                "these bindings target; the PrismConfig layout and error codes differ.");
        }
    }
}
