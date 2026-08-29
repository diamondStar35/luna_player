namespace PrismSharp
{
    /// <summary>
    /// Severity of a log message, and - as a threshold - the least severe message to deliver.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>PrismLogLevel</c> from prism.h. Ordered least to most severe, with
    /// <see cref="None"/> above every real severity. A message is delivered when its level is at
    /// least the current threshold, so a threshold of <see cref="Warn"/> delivers warnings and errors
    /// and discards the rest. The numeric values are part of the ABI.
    /// </remarks>
    public enum LogLevel
    {
        /// <summary>Fine-grained tracing of internal operations; the most verbose level.</summary>
        Trace = 0,

        /// <summary>Diagnostic information useful during development.</summary>
        Debug = 1,

        /// <summary>Informational messages describing normal operation.</summary>
        Info = 2,

        /// <summary>Conditions that are not errors but may indicate a problem.</summary>
        Warn = 3,

        /// <summary>Error conditions.</summary>
        Error = 4,

        /// <summary>
        /// Not a message severity. As a threshold it suppresses everything, since no message is this
        /// severe.
        /// </summary>
        None = 5
    }
}
