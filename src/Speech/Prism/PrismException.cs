using System;
using PrismSharp.Native;

namespace PrismSharp
{
    /// <summary>
    /// Thrown when a Prism operation fails, carrying the native error code.
    /// </summary>
    /// <remarks>
    /// The message is the description Prism itself supplies for the code, so it is suitable for
    /// logging and, in most cases, for showing to a user.
    /// </remarks>
    public sealed unsafe class PrismException : Exception
    {
        /// <summary>Creates an exception describing a native error code.</summary>
        /// <param name="error">The code Prism returned.</param>
        public PrismException(Error error)
            : base(Methods.String(Methods.prism_error_string(error)) ?? error.ToString())
        {
            Error = error;
        }

        /// <summary>The native error code that caused this exception.</summary>
        public Error Error { get; }
    }
}
