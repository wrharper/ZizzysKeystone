using Keystone.Net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Keystone.Net
{
    /// <summary>
    /// Represents a managed wrapper around a Keystone assembler engine instance.
    /// This class provides assembly services, symbol resolution, and option configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="Engine"/> class encapsulates a native Keystone engine handle and exposes
    /// a safe, .NET‑friendly API for assembling machine code. It supports:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Assembling instructions into byte arrays</description></item>
    ///   <item><description>Writing assembled bytes directly into streams or buffers</description></item>
    ///   <item><description>Symbol resolution via the <see cref="ResolveSymbol"/> event</description></item>
    ///   <item><description>Configurable error behavior via <see cref="ThrowOnError"/></description></item>
    /// </list>
    ///
    /// <para>
    /// When <see cref="ThrowOnError"/> is <c>true</c>, assembly failures throw
    /// <see cref="KeystoneException"/>. When <c>false</c>, failures return empty results.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage
    /// using var engine = new Engine(Architecture.X86, Mode.X32)
    /// {
    ///     ThrowOnError = true
    /// };
    ///
    /// EncodedData data = engine.Assemble("nop", 0x00400000);
    /// byte[] bytes = data.Buffer;   // [0x90]
    /// </code>
    /// </example>
    public sealed class Engine : IDisposable
    {
        private IntPtr engine = IntPtr.Zero;
        private bool addedResolveSymbol;

        private readonly ResolverInternal internalImpl;
        private readonly List<Resolver> resolvers = [];

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool ResolverInternal(IntPtr symbol, ref ulong value);

        /// <summary>
        /// When true, assembly failures throw <see cref="KeystoneException"/>.
        /// When false, failures return empty results instead of throwing.
        /// </summary>
        public bool ThrowOnError { get; set; }

        /// <summary>
        /// Delegate used for resolving symbolic references during assembly.
        /// </summary>
        /// <param name="symbol">The symbol name requested by Keystone.</param>
        /// <param name="value">The resolved address to return.</param>
        /// <returns><c>true</c> if the symbol was resolved; otherwise <c>false</c>.</returns>
        public delegate bool Resolver(string symbol, ref ulong value);

        /// <summary>
        /// Event invoked when Keystone requests resolution of a symbolic operand.
        /// Multiple resolvers may be attached; the first to return true wins.
        /// </summary>
        /// <example>
        /// <code>
        /// using var engine = new Engine(Architecture.X86, Mode.X32);
        ///
        /// engine.ResolveSymbol += (string symbol, ref ulong value) =>
        /// {
        ///     if (symbol == "myLabel")
        ///     {
        ///         value = 0x00401000;
        ///         return true;
        ///     }
        ///     return false;
        /// };
        ///
        /// byte[] bytes = engine.Assemble("jmp myLabel", 0x00400000, out _, out _);
        /// </code>
        /// </example>
        public event Resolver ResolveSymbol
        {
            add
            {
                if (!addedResolveSymbol)
                {
                    KeystoneError err = NativeInterop.SetOption(
                        engine,
                        (int)OptionType.SYM_RESOLVER,
                        Marshal.GetFunctionPointerForDelegate(internalImpl));

                    if (err == KeystoneError.KS_ERR_OK)
                        addedResolveSymbol = true;
                    else
                        throw new KeystoneException("Could not add symbol resolver", err);
                }

                resolvers.Add(value);
            }

            remove
            {
                resolvers.Remove(value);

                if (addedResolveSymbol && resolvers.Count == 0)
                {
                    KeystoneError err = NativeInterop.SetOption(engine, (int)OptionType.SYM_RESOLVER, IntPtr.Zero);

                    if (err == KeystoneError.KS_ERR_OK)
                        addedResolveSymbol = false;
                    else
                        throw new KeystoneException("Could not remove symbol resolver", err);
                }
            }
        }

        /// <summary>
        /// Internal callback invoked by Keystone when a symbol must be resolved.
        /// </summary>
        private bool ResolveSymbolInternal(IntPtr symbolPtr, ref ulong value)
        {
            string? symbol = Marshal.PtrToStringAnsi(symbolPtr);
            if (symbol is null)
                return false;

            foreach (Resolver item in resolvers)
            {
                if (item(symbol, ref value))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a new Keystone engine instance for the specified architecture and mode.
        /// </summary>
        /// <param name="architecture">The target architecture (e.g., X86, ARM).</param>
        /// <param name="mode">The architecture mode (e.g., 32‑bit, 64‑bit).</param>
        /// <exception cref="KeystoneException">Thrown if Keystone fails to initialize.</exception>
        /// <example>
        /// <code>
        /// using var engine = new Engine(Architecture.X86, Mode.X64)
        /// {
        ///     ThrowOnError = true
        /// };
        /// </code>
        /// </example>
        public Engine(Architecture architecture, Mode mode)
        {
            internalImpl = ResolveSymbolInternal;

            var result = NativeInterop.Open(architecture, (int)mode, ref engine);

            if (result != KeystoneError.KS_ERR_OK)
                throw new KeystoneException("Error while initializing keystone", result);
        }

        /// <summary>
        /// Sets a Keystone engine option.
        /// </summary>
        /// <param name="type">The option type.</param>
        /// <param name="value">The option value.</param>
        /// <returns><c>true</c> if the option was set successfully; otherwise <c>false</c>.</returns>
        /// <exception cref="KeystoneException">Thrown if <see cref="ThrowOnError"/> is true and the option fails.</exception>
        /// <example>
        /// <code>
        /// engine.SetOption(OptionType.SYNTAX, (uint)OptionValue.SYNTAX_INTEL);
        /// </code>
        /// </example>
        public bool SetOption(OptionType type, uint value)
        {
            var result = NativeInterop.SetOption(engine, (int)type, (IntPtr)value);

            if (result != KeystoneError.KS_ERR_OK)
            {
                if (ThrowOnError)
                    throw new KeystoneException("Error while setting option", result);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Assembles the given instruction text at the specified base address.
        /// Returns a non-null byte array. On failure (and ThrowOnError=false), returns an empty array.
        /// </summary>
        /// <param name="toEncode">The assembly text to encode.</param>
        /// <param name="address">The base address for relative calculations.</param>
        /// <param name="size">The number of bytes generated.</param>
        /// <param name="statementCount">The number of statements assembled.</param>
        /// <returns>A non-null byte array containing the assembled machine code.</returns>
        /// <exception cref="KeystoneException">Thrown if assembly fails and <see cref="ThrowOnError"/> is true.</exception>
        /// <example>
        /// <code>
        /// using var engine = new Engine(Architecture.X86, Mode.X32);
        ///
        /// byte[] bytes = engine.Assemble("add eax, ebx", 0x00400000, out int size, out int count);
        ///
        /// // bytes = [0x01, 0xD8]
        /// // size = 2
        /// // count = 1
        /// </code>
        /// </example>
        public byte[] Assemble(string toEncode, ulong address, out int size, out int statementCount)
        {
            ArgumentNullException.ThrowIfNull(toEncode);

            int result = NativeInterop.Assemble(
                engine,
                toEncode,
                address,
                out IntPtr encoding,
                out uint size_,
                out uint statementCount_);

            if (result != 0)
            {
                if (ThrowOnError)
                    throw new KeystoneException("Error while assembling instructions", GetLastKeystoneError());

                size = 0;
                statementCount = 0;
                return [];
            }

            size = (int)size_;
            statementCount = (int)statementCount_;

            byte[] buffer = new byte[size];
            Marshal.Copy(encoding, buffer, 0, size);
            NativeInterop.Free(encoding);

            return buffer;
        }

        /// <summary>
        /// Assembles the given instruction text and returns an <see cref="EncodedData"/> wrapper.
        /// Never returns null. On failure (and ThrowOnError=false), returns an empty EncodedData.
        /// </summary>
        /// <param name="toEncode">The assembly text to encode.</param>
        /// <param name="address">The base address for relative calculations.</param>
        /// <returns>An <see cref="EncodedData"/> instance containing the assembled bytes.</returns>
        /// <example>
        /// <code>
        /// using var engine = new Engine(Architecture.X86, Mode.X32)
        /// {
        ///     ThrowOnError = true
        /// };
        ///
        /// EncodedData data = engine.Assemble("nop", 0x00400000);
        /// byte[] bytes = data.Buffer;   // [0x90]
        /// </code>
        /// </example>
        public EncodedData Assemble(string toEncode, ulong address)
        {
            byte[] buffer = Assemble(toEncode, address, out _, out int statementCount);
            return new EncodedData(buffer, statementCount, address);
        }

        /// <summary>
        /// Assembles instructions directly into an existing buffer.
        /// Returns the number of bytes written.
        /// </summary>
        /// <param name="toEncode">The assembly text to encode.</param>
        /// <param name="address">The base address for relative calculations.</param>
        /// <param name="buffer">The destination buffer.</param>
        /// <param name="index">The starting index in the buffer.</param>
        /// <param name="statementCount">The number of statements assembled.</param>
        /// <returns>The number of bytes written to the buffer.</returns>
        /// <exception cref="KeystoneException">Thrown if assembly fails and <see cref="ThrowOnError"/> is true.</exception>
        /// <example>
        /// <code>
        /// byte[] buffer = new byte[16];
        ///
        /// using var engine = new Engine(Architecture.X86, Mode.X32);
        ///
        /// int written = engine.Assemble("inc eax", 0, buffer, 0, out int statements);
        ///
        /// // buffer[0] = 0x40
        /// // written = 1
        /// // statements = 1
        /// </code>
        /// </example>
        public int Assemble(string toEncode, ulong address, byte[] buffer, int index, out int statementCount)
        {
            ArgumentNullException.ThrowIfNull(toEncode);
            ArgumentNullException.ThrowIfNull(buffer);
            if (index < 0 || index >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(buffer));

            int result = NativeInterop.Assemble(
                engine,
                toEncode,
                address,
                out IntPtr encoding,
                out uint size_,
                out uint statementCount_);

            int size = (int)size_;
            statementCount = (int)statementCount_;

            if (result != 0)
            {
                if (ThrowOnError)
                    throw new KeystoneException("Error while assembling instructions", GetLastKeystoneError());

                return 0;
            }

            Marshal.Copy(encoding, buffer, index, size);
            NativeInterop.Free(encoding);

            return size;
        }

        /// <summary>
        /// Assembles instructions into an existing buffer, ignoring statement count.
        /// </summary>
        public int Assemble(string toEncode, ulong address, byte[] buffer, int index)
        {
            return Assemble(toEncode, address, buffer, index, out _);
        }

        /// <summary>
        /// Assembles instructions and writes the result to a stream.
        /// Returns true on success, false on failure (when ThrowOnError=false).
        /// </summary>
        /// <param name="toEncode">The assembly text to encode.</param>
        /// <param name="address">The base address for relative calculations.</param>
        /// <param name="stream">The destination stream.</param>
        /// <param name="size">The number of bytes written.</param>
        /// <param name="statementCount">The number of statements assembled.</param>
        /// <returns><c>true</c> if assembly succeeded; otherwise <c>false</c>.</returns>
        /// <example>
        /// <code>
        /// using var ms = new MemoryStream();
        /// using var engine = new Engine(Architecture.X86, Mode.X32);
        ///
        /// bool ok = engine.Assemble("push ebp", 0, ms, out int size, out int statements);
        ///
        /// // ms contains: 0x55
        /// // size = 1
        /// // statements = 1
        /// </code>
        /// </example>
        public bool Assemble(string toEncode, ulong address, Stream stream, out int size, out int statementCount)
        {
            ArgumentNullException.ThrowIfNull(stream);

            byte[] enc = Assemble(toEncode, address, out size, out statementCount);

            if (enc.Length == 0 && size == 0 && statementCount == 0 && !ThrowOnError)
                return false;

            stream.Write(enc, 0, size);
            return true;
        }

        /// <summary>
        /// Assembles instructions and writes the result to a stream.
        /// </summary>
        public bool Assemble(string toEncode, ulong address, Stream stream, out int size)
        {
            return Assemble(toEncode, address, stream, out size, out _);
        }

        /// <summary>
        /// Assembles instructions and writes the result to a stream.
        /// </summary>
        public bool Assemble(string toEncode, ulong address, Stream stream)
        {
            return Assemble(toEncode, address, stream, out _, out _);
        }

        /// <summary>
        /// Retrieves the last Keystone error for this engine instance.
        /// </summary>
        public KeystoneError GetLastKeystoneError()
        {
            return NativeInterop.GetLastKeystoneError(engine);
        }

        /// <summary>
        /// Converts a Keystone error code into a human-readable string.
        /// Never returns null.
        /// </summary>
        public static string ErrorToString(KeystoneError code)
        {
            IntPtr error = NativeInterop.ErrorToString(code);
            return error != IntPtr.Zero
                ? Marshal.PtrToStringAnsi(error) ?? string.Empty
                : string.Empty;
        }

        /// <summary>
        /// Returns true if the specified architecture is supported by this Keystone build.
        /// </summary>
        public static bool IsArchitectureSupported(Architecture architecture)
        {
            return NativeInterop.IsArchitectureSupported(architecture);
        }

        /// <summary>
        /// Retrieves the Keystone engine version.
        /// </summary>
        public static uint GetKeystoneVersion(ref uint major, ref uint minor)
        {
            return NativeInterop.Version(ref major, ref minor);
        }

        /// <summary>
        /// Releases the underlying Keystone engine instance.
        /// </summary>
        /// <example>
        /// <code>
        /// using (var engine = new Engine(Architecture.X86, Mode.X32))
        /// {
        ///     // use engine
        /// }
        /// // engine is automatically closed here
        /// </code>
        /// </example>
        public void Dispose()
        {
            IntPtr currentEngine = Interlocked.Exchange(ref engine, IntPtr.Zero);

            if (currentEngine != IntPtr.Zero)
                NativeInterop.Close(currentEngine);
        }
    }
}