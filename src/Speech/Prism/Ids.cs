namespace PrismSharp
{
    /// <summary>
    /// The stable identifiers of Prism's built-in backends.
    /// </summary>
    /// <remarks>
    /// Mirrors the <c>PRISM_BACKEND_*</c> constants from prism.h (v0.18.2). These values are fixed
    /// and safe to persist, so an application can remember a user's chosen backend across runs. A
    /// backend appearing here is not necessarily present in a given build: use
    /// <see cref="Context.Exists"/> to check. Custom and plugin backends get generated identifiers
    /// that are not listed here.
    /// </remarks>
    public static class Ids
    {
        /// <summary>No backend. Returned by <see cref="Context.GetBackendId"/> when a name matches nothing.</summary>
        public const ulong Invalid = 0;
        /// <summary>Microsoft Speech API, the long-standing Windows text-to-speech interface. Windows only.</summary>
        public const ulong Sapi = 0x1D6DF72422CEEE66UL;
        /// <summary>AVSpeechSynthesizer, the Apple platforms’ built-in speech engine. Needs a main run loop.</summary>
        public const ulong AvSpeech = 0x28E3429577805C24UL;
        /// <summary>VoiceOver, the Apple platforms’ screen reader. Needs a window and an active run loop.</summary>
        public const ulong VoiceOver = 0xCB4897961A754BCBUL;
        /// <summary>Speech Dispatcher, the common speech layer on Linux and the BSDs.</summary>
        public const ulong SpeechDispatcher = 0xE3D6F895D949EBFEUL;
        /// <summary>NVDA, the free open-source Windows screen reader. Windows only.</summary>
        public const ulong Nvda = 0x89CC19C5C4AC1A56UL;
        /// <summary>JAWS, the commercial Windows screen reader. Windows only.</summary>
        public const ulong Jaws = 0xAC3D60E9BD84B53EUL;
        /// <summary>Windows OneCore voices, the modern Windows text-to-speech engine. Windows only.</summary>
        public const ulong OneCore = 0x6797D32F0D994CB4UL;
        /// <summary>Orca, the GNOME screen reader. Linux and the BSDs.</summary>
        public const ulong Orca = 0x10AA1FC05A17F96CUL;
        /// <summary>Android screen readers reached through the accessibility manager. Android only.</summary>
        public const ulong AndroidScreenReader = 0xD199C175AEEC494BUL;
        /// <summary>Android text-to-speech services. Android only.</summary>
        public const ulong AndroidTts = 0xBC175831BFE4E5CCUL;
        /// <summary>The Web SpeechSynthesis API. Web builds only.</summary>
        public const ulong WebSpeech = 0x3572538D44D44A8FUL;
        /// <summary>UI Automation notification events, which any listening Windows screen reader may announce. Windows only.</summary>
        public const ulong Uia = 0x6238F019DB678F8EUL;
        /// <summary>Zhengdu Screen Reader. Windows only.</summary>
        public const ulong Zdsr = 0x3D93C56C9E7F2A2EUL;
        /// <summary>ZoomText magnifier and reader. Windows only.</summary>
        public const ulong ZoomText = 0xAE439D62DC7B1479UL;
        /// <summary>BoyPCReader. Windows only.</summary>
        public const ulong BoyPcReader = 0x285ABA1C16F3300FUL;
        /// <summary>PCTalker, a Japanese Windows screen reader. Windows only.</summary>
        public const ulong PcTalker = 0x344B951962E3B835UL;
        /// <summary>Sense Reader. Windows only.</summary>
        public const ulong SenseReader = 0xED4760890B55C2F2UL;
        /// <summary>System Access. Windows only, and present only when explicitly enabled at build time.</summary>
        public const ulong SystemAccess = 0x8380F2A37B2C3EB6UL;
        /// <summary>Window-Eyes. Windows only, and present only when explicitly enabled at build time.</summary>
        public const ulong WindowEyes = 0x9120D89908785C13UL;
        /// <summary>Spiel, a modern speech framework for Linux and the BSDs.</summary>
        public const ulong Spiel = 0x478B44F14AD3D89CUL;
    }
}
