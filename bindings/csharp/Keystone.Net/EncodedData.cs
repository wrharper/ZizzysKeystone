namespace Keystone.Net
{
    /// <summary>
    /// Represents the result of a Keystone assembly operation.
    /// This class encapsulates the encoded machine code, the number of
    /// statements assembled, and the base address used during assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Instances of <see cref="EncodedData"/> are returned by
    /// <see cref="Engine.Assemble(string, ulong)"/>. The class provides a
    /// convenient, strongly‑typed container for the assembled bytes and
    /// associated metadata.
    /// </para>
    ///
    /// <para>
    /// The <see cref="Buffer"/> property contains the raw machine code emitted
    /// by Keystone. The <see cref="StatementCount"/> property indicates how many
    /// assembly statements were processed, which is useful for multi‑instruction
    /// assembly strings.
    /// </para>
    ///
    /// <para>
    /// <see cref="Address"/> stores the base address used during assembly,
    /// which is important for relative instructions such as jumps and calls.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var engine = new Engine(Architecture.X86, Mode.X32)
    /// {
    ///     ThrowOnError = true
    /// };
    ///
    /// // Assemble a single instruction
    /// EncodedData data = engine.Assemble("nop", 0x00400000);
    ///
    /// byte[] bytes = data.Buffer;       // [0x90]
    /// ulong addr = data.Address;        // 0x00400000
    /// int count = data.StatementCount;  // 1
    /// </code>
    /// </example>
    public sealed class EncodedData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncodedData"/> class.
        /// </summary>
        /// <param name="buffer">The assembled machine code bytes.</param>
        /// <param name="statementCount">The number of statements assembled.</param>
        /// <param name="address">The base address used during assembly.</param>
        /// <remarks>
        /// This constructor is internal because <see cref="EncodedData"/> objects
        /// are created exclusively by the <see cref="Engine"/> class.
        /// </remarks>
        internal EncodedData(byte[] buffer, int statementCount, ulong address)
        {
            Buffer = buffer;
            Address = address;
            StatementCount = statementCount;
        }

        /// <summary>
        /// Gets the base address of the first instruction in the assembled output.
        /// </summary>
        /// <remarks>
        /// This value corresponds to the <c>address</c> parameter passed to
        /// <see cref="Engine.Assemble(string, ulong)"/>.
        /// </remarks>
        public ulong Address { get; }

        /// <summary>
        /// Gets the raw machine code bytes produced by the assembly operation.
        /// </summary>
        /// <remarks>
        /// This array is never <c>null</c>. If assembly fails and
        /// <see cref="Engine.ThrowOnError"/> is <c>false</c>, this array may be empty.
        /// </remarks>
        public byte[] Buffer { get; }

        /// <summary>
        /// Gets the number of assembly statements processed during the operation.
        /// </summary>
        /// <remarks>
        /// This value is useful when assembling multiple instructions in a single
        /// call, such as:
        /// <code>
        /// "mov eax, 1; inc eax; ret"
        /// </code>
        /// </remarks>
        public int StatementCount { get; }
    }
}