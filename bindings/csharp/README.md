# Keystone.Net
Modern .NET 10 bindings for the Keystone Assembler Engine.

Keystone.Net provides a clean, safe, and fully documented managed wrapper around the native Keystone library.  
It is designed for reverse‑engineering tools, assemblers, debuggers, emulators, and dynamic code generation.

---

# Features

- Multi‑architecture assembly (X86, X64, ARM, ARM64, MIPS, PPC, SPARC, SystemZ, Hexagon)
- Modern .NET 10 interop using LibraryImport
- Fully nullable‑aware API
- Symbol resolution via events
- Stream and buffer‑based assembly
- Rich error reporting (KeystoneException)
- Comprehensive XML documentation and examples
- Clean, idiomatic .NET 10 coding style

---

# Quick Start

`csharp
using Keystone.Net;

using var engine = new Engine(Architecture.X86, Mode.X32)
{
    ThrowOnError = true
};

ulong address = 0;

// Optional: resolve symbolic labels
engine.ResolveSymbol += (string symbol, ref ulong value) =>
{
    if (symbol == "_j1")
    {
        value = 0x1234abcd;
        return true;
    }
    return false;
};

// Assemble multiple instructions
EncodedData enc = engine.Assemble("xor eax, eax; jmp _j1", address);

// Inspect results
byte[] bytes = enc.Buffer;        // Machine code
ulong addr  = enc.Address;        // Base address
int count   = enc.StatementCount; // Number of statements
`

---

# Example Output

`csharp
enc.Buffer.ShouldBe(new byte[] { 0x00 });
enc.Address.ShouldBe(address);
enc.StatementCount.ShouldBe(3);
`

---

# Symbol Resolution

Keystone.Net supports dynamic symbol resolution via the ResolveSymbol event:

`csharp
engine.ResolveSymbol += (string name, ref ulong value) =>
{
    if (name == "target")
    {
        value = 0x401000;
        return true;
    }
    return false;
};

var data = engine.Assemble("jmp target", 0x400000);
`

---

# Error Handling

Keystone.Net supports two modes:

Strict Mode (default)
Throws KeystoneException on any assembly error:

`csharp
engine.ThrowOnError = true;
engine.Assemble("invalid instruction", 0); // throws
`

Lenient Mode
Returns empty results instead of throwing:

`csharp
engine.ThrowOnError = false;
byte[] bytes = engine.Assemble("invalid", 0, out , out ); // empty array
`

---

# Running Tests

This project includes a full NUnit test suite.

To run tests:

`bash
dotnet test
`
