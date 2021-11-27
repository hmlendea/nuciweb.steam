using System;

namespace NuciWeb.Steam
{
    /// <summary>
    /// Key activation exception.
    /// </summary>
    [Serializable]
    public class KeyActivationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyActivationException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        public KeyActivationException(string message)
            : this(message, KeyActivationErrorCode.Unexpected)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyActivationException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="code">Error code.</param>
        public KeyActivationException(string message, KeyActivationErrorCode code)
            : base(message)
        {
            this.Code = code;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyActivationException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="innerException">Inner exception.</param>
        public KeyActivationException(string message, Exception innerException)
            : this(message, KeyActivationErrorCode.Unexpected, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyActivationException"/> class.
        /// </summary>
        /// <param name="message">Exception message.</param>
        /// <param name="code">Error code.</param>
        /// <param name="innerException">Inner exception.</param>
        public KeyActivationException(string message, KeyActivationErrorCode code, Exception innerException)
            : base(message, innerException)
        {
            this.Code = code;
        }

        public KeyActivationErrorCode Code { get; set; }
    }
}
