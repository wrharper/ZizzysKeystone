using NUnit.Framework;
using Shouldly;

namespace Keystone.Net.Tests
{
    /// <summary>
    /// Contains integration tests for the <see cref="Engine"/> class using the
    /// native Keystone assembler library.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These tests verify that the managed wrapper correctly loads the Keystone
    /// native library, assembles instructions for multiple architectures, and
    /// handles error conditions according to <see cref="Engine.ThrowOnError"/>.
    /// </para>
    ///
    /// <para>
    /// <b>How to run the tests:</b>
    /// </para>
    /// <list type="number">
    ///   <item><description>Ensure the Keystone native library (<c>keystone.dll</c>, <c>libkeystone.so</c>, or <c>libkeystone.dylib</c>) is available on your system path or next to the test binaries.</description></item>
    ///   <item><description>Build the solution in Debug or Release mode.</description></item>
    ///   <item><description>Run tests using your preferred method:</description></item>
    /// </list>
    ///
    /// <para>
    /// <b>Command Line:</b>
    /// <code>
    /// dotnet test
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <b>Visual Studio:</b> Test → Run All Tests  
    /// <b>Rider:</b> Right‑click the test project → Run Unit Tests  
    /// <b>VS Code:</b> Use the .NET Test Explorer extension  
    /// </para>
    ///
    /// <para>
    /// If the native library cannot be loaded, the test suite will fail during
    /// <see cref="InitializeKeystone"/> before any assembly tests run.
    /// </para>
    /// </remarks>
    [TestFixture]
    public class ExecutionTests
    {
        /// <summary>
        /// Ensures that the Keystone native library is available and supports
        /// at least the X86 architecture before running any tests.
        /// </summary>
        /// <remarks>
        /// This acts as a sanity check to confirm that the native library
        /// successfully loaded and is functional.
        /// </remarks>
        [OneTimeSetUp]
        public static void InitializeKeystone()
        {
            Engine.IsArchitectureSupported(Architecture.X86).ShouldBeTrue();
        }

        /// <summary>
        /// Verifies that valid x86 instructions assemble into the expected
        /// machine code bytes.
        /// </summary>
        /// <example>
        /// <code>
        /// using var engine = new Engine(Architecture.X86, Mode.X32)
        /// {
        ///     ThrowOnError = true
        /// };
        ///
        /// byte[] nop = engine.Assemble("nop", 0).Buffer;          // [0x90]
        /// byte[] add = engine.Assemble("add eax, eax", 0).Buffer; // [0x01, 0xC0]
        /// </code>
        /// </example>
        [Test]
        public void ShouldEmitValidX86Data()
        {
            using Engine engine = new(Architecture.X86, Mode.X32) { ThrowOnError = true };

            engine.Assemble("nop", 0).Buffer.ShouldBe([0x90]);
            engine.Assemble("add eax, eax", 0).Buffer.ShouldBe([0x01, 0xC0]);
        }

        /// <summary>
        /// Verifies that valid ARM instructions assemble into the expected
        /// machine code bytes.
        /// </summary>
        /// <example>
        /// <code>
        /// using var engine = new Engine(Architecture.ARM, Mode.ARM)
        /// {
        ///     ThrowOnError = true
        /// };
        ///
        /// byte[] mul = engine.Assemble("mul r1, r0, r0", 0).Buffer;
        /// // Expected: [0x90, 0x00, 0x01, 0xE0]
        /// </code>
        /// </example>
        [Test]
        public void ShouldEmitValidARMData()
        {
            using Engine engine = new(Architecture.ARM, Mode.ARM) { ThrowOnError = true };

            engine.Assemble("mul r1, r0, r0", 0)
                  .Buffer.ShouldBe([0x90, 0x00, 0x01, 0xE0]);
        }

        /// <summary>
        /// Ensures that <see cref="Engine.ThrowOnError"/> correctly controls
        /// whether assembly failures throw exceptions or return null.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>ThrowOnError = false</c>, invalid instructions return <c>null</c>.
        /// </para>
        /// <para>
        /// When <c>ThrowOnError = true</c>, invalid instructions throw
        /// <see cref="KeystoneException"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// using var engine = new Engine(Architecture.ARM, Mode.ARM)
        /// {
        ///     ThrowOnError = false
        /// };
        ///
        /// engine.Assemble("invalid", 0).ShouldBeNull();
        ///
        /// using var strict = new Engine(Architecture.ARM, Mode.ARM)
        /// {
        ///     ThrowOnError = true
        /// };
        ///
        /// Should.Throw&lt;KeystoneException&gt;(() => strict.Assemble("invalid", 0));
        /// </code>
        /// </example>
        [Test]
        public void ShouldThrowOnError()
        {
            using (Engine engine = new(Architecture.ARM, Mode.ARM) { ThrowOnError = false })
            {
                engine.Assemble("push eax, 0x42", 0).ShouldBeNull();
                engine.Assemble("doesntexist", 0).ShouldBeNull();
            }

            using (Engine engine = new(Architecture.ARM, Mode.ARM) { ThrowOnError = true })
            {
                Should.Throw<KeystoneException>(() => engine.Assemble("push eax, 0x42", 0));
                Should.Throw<KeystoneException>(() => engine.Assemble("doestexist", 0));
            }
        }

        /// <summary>
        /// Demonstrates symbol resolution using the <see cref="Engine.ResolveSymbol"/> event.
        /// This test is ignored unless Keystone was built with post‑2016 symbol support.
        /// </summary>
        /// <remarks>
        /// This test shows how to resolve labels such as <c>_j1</c> during assembly.
        /// </remarks>
        /// <example>
        /// <code>
        /// using var engine = new Engine(Architecture.X86, Mode.X32)
        /// {
        ///     ThrowOnError = true
        /// };
        ///
        /// engine.ResolveSymbol += (string name, ref ulong value) =>
        /// {
        ///     if (name == "_target")
        ///     {
        ///         value = 0x12345678;
        ///         return true;
        ///     }
        ///     return false;
        /// };
        ///
        /// EncodedData enc = engine.Assemble("jmp _target", 0);
        /// </code>
        /// </example>
        [Test, Ignore("Feature requires Keystone built after October 7th 2016.")]
        public void ShouldHaveValidExample()
        {
            using Engine keystone = new(Architecture.X86, Mode.X32) { ThrowOnError = true };
            ulong address = 0;

            keystone.ResolveSymbol += (s, ref w) =>
            {
                if (s == "_j1")
                {
                    w = 0x1234abcd;
                    return true;
                }

                return false;
            };

            EncodedData? enc = keystone.Assemble("xor eax, eax; jmp _j1", address);
            if (enc == null)
                return;

            enc.Buffer.ShouldBe([0x00]);
            enc.Address.ShouldBe(address);
            enc.StatementCount.ShouldBe(3);
        }
    }
}