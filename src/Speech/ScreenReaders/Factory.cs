namespace PrismSharp.Speech.ScreenReaders
{
    /// <summary>Creates screen reader instances.</summary>
    public static class Factory
    {
        /// <summary>
        /// Creates the Prism-backed <see cref="IScreenReader"/>.
        /// </summary>
        /// <returns>
        /// A reader that has not yet opened anything - call <see cref="IScreenReader.Initialize"/>,
        /// and confine it to one thread, most simply by handing it to a
        /// <see cref="ScreenReaderWorker"/>.
        /// </returns>
        public static IScreenReader Create() => new Reader();
    }
}
