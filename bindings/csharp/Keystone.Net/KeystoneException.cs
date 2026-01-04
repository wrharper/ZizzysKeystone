using System.Diagnostics;

namespace Keystone.Net
{
    /// <summary>
    /// Represents an error encountered while assembling one or more instructions
    /// using the Keystone engine.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exception is thrown when the Keystone engine reports an error and
    /// <see cref="Engine.ThrowOnError"/> is set to <c>true</c>. When
    /// <see cref="Engine.ThrowOnError"/> is <c>false</c>, assembly methods return
    /// empty results instead of throwing.
    /// </para>
    ///
    /// <para>
    /// The <see cref="Error"/> property exposes the underlying
    /// <see cref="KeystoneError"/> code returned by the native Keystone library.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// try
    /// {
    ///     using var engine = new Engine(Architecture.X86, Mode.X32)
    ///     {
    ///         ThrowOnError = true
    ///     };
    ///
    ///     // Invalid instruction triggers a KeystoneException
    ///     engine.Assemble("invalid_instruction", 0x00400000);
    /// }
    /// catch (KeystoneException ex)
    /// {
    ///     Console.WriteLine($"Assembly failed: {ex.Error}");
    ///     Console.WriteLine(ex.ToString());
    /// }
    /// </code>
    /// </example>
    public sealed class KeystoneException : Exception
    {
        /// <summary>
        /// Gets the Keystone error code associated with this exception.
        /// </summary>
        /// <remarks>
        /// This value corresponds directly to the <see cref="KeystoneError"/>
        /// enumeration returned by the native Keystone engine.
        /// </remarks>
        public KeystoneError Error { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeystoneException"/> class.
        /// </summary>
        /// <param name="message">A human-readable description of the error.</param>
        /// <param name="error">The underlying Keystone error code.</param>
        /// <remarks>
        /// This constructor is internal because exceptions are created only by
        /// the <see cref="Engine"/> class when the native Keystone engine reports
        /// an error condition.
        /// </remarks>
        internal KeystoneException(string message, KeystoneError error)
            : base(message + ".")
        {
            Debug.Assert(error != KeystoneError.KS_ERR_OK);
            Error = error;
        }

        /// <summary>
        /// Returns a detailed string representation of this exception,
        /// including the human-readable Keystone error description.
        /// </summary>
        /// <returns>A formatted string describing the error.</returns>
        /// <example>
        /// <code>
        /// catch (KeystoneException ex)
        /// {
        ///     Console.WriteLine(ex.ToString());
        ///     // Output example:
        ///     // "Error while assembling instructions: invalid mnemonic."
        /// }
        /// </code>
        /// </example>
        public override string ToString()
        {
            return $"{Message}: {Engine.ErrorToString(Error)}.";
        }
    }
}