namespace PrismSharp
{
    /// <summary>One voice offered by a backend.</summary>
    public readonly struct VoiceInfo
    {
        /// <summary>Creates a voice descriptor.</summary>
        /// <param name="index">Zero-based index within the backend's voice list.</param>
        /// <param name="name">Display name.</param>
        /// <param name="language">Language tag.</param>
        public VoiceInfo(int index, string name, string language)
        {
            Index = index;
            Name = name;
            Language = language;
        }

        /// <summary>
        /// Zero-based index within the backend's voice list, as
        /// <see cref="Backend.CurrentVoiceIndex"/> expects. Stable only until the voice list is
        /// refreshed.
        /// </summary>
        public int Index { get; }

        /// <summary>The voice's display name, such as <c>"Microsoft Hazel Desktop"</c>.</summary>
        public string Name { get; }

        /// <summary>
        /// The voice's language tag, such as <c>"en-GB"</c>. Empty when the backend does not report
        /// one.
        /// </summary>
        public string Language { get; }
    }
}
