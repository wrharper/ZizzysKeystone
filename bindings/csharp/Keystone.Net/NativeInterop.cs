using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Keystone.Net
{
    /// <summary>
    ///   Imported symbols for interop with keystone.dll.
    /// </summary>
    internal static partial class NativeInterop
    {
        // This shouldn't be needed, even on Windows
        // /// <summary>
        // /// Taken from: http://stackoverflow.com/questions/10852634/using-a-32bit-or-64bit-dll-in-c-sharp-dllimport
        // /// </summary>
        // static NativeInterop()
        // {
        //     var myPath = new Uri(typeof(NativeInterop).Assembly.CodeBase).LocalPath;
        //     var myFolder = Path.GetDirectoryName(myPath);

        //     var is64 = IntPtr.Size == 8;
        //     var subfolder = is64 ? "\\win64\\" : "\\win32\\";

        //     string dllPosition = myFolder + subfolder + "keystone.dll";

        //     // If this file exist, load it. 
        //     // Otherwise let the marshaller load the appropriate file.
        //     if (File.Exists(dllPosition))
        //         LoadLibrary(dllPosition);
        // }

        // [DllImport("kernel32.dll")]
        // private static extern IntPtr LoadLibrary(string dllToLoad);

        // ---------------------------------------------------------------------
        // ks_version
        // ---------------------------------------------------------------------
        [LibraryImport("keystone", EntryPoint = "ks_version")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial uint Version(ref uint major, ref uint minor);

        // ---------------------------------------------------------------------
        // ks_open
        // ---------------------------------------------------------------------
        [LibraryImport("keystone", EntryPoint = "ks_open")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial KeystoneError Open(
            Architecture arch,
            int mode,
            ref IntPtr ks);

        // ---------------------------------------------------------------------
        // ks_close
        // ---------------------------------------------------------------------
        [LibraryImport("keystone", EntryPoint = "ks_close")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial KeystoneError Close(IntPtr ks);

        // ---------------------------------------------------------------------
        // ks_free
        // ---------------------------------------------------------------------
        [LibraryImport("keystone", EntryPoint = "ks_free")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial void Free(IntPtr buffer);

        // ---------------------------------------------------------------------
        // ks_strerror
        // ---------------------------------------------------------------------
        [LibraryImport("keystone", EntryPoint = "ks_strerror")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial IntPtr ErrorToString(KeystoneError code);

        // ---------------------------------------------------------------------
        // ks_errno
        // ---------------------------------------------------------------------
        [LibraryImport("keystone", EntryPoint = "ks_errno")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial KeystoneError GetLastKeystoneError(IntPtr ks);

        // ---------------------------------------------------------------------
        // ks_arch_supported
        // ---------------------------------------------------------------------
        [LibraryImport("keystone", EntryPoint = "ks_arch_supported")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        [return: MarshalAs(UnmanagedType.I1)] // bool from C
        internal static partial bool IsArchitectureSupported(Architecture arch);

        // ---------------------------------------------------------------------
        // ks_option
        // ---------------------------------------------------------------------
        [LibraryImport("keystone", EntryPoint = "ks_option")]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial KeystoneError SetOption(
            IntPtr ks,
            int type,
            IntPtr value);

        // ---------------------------------------------------------------------
        // ks_asm
        // ---------------------------------------------------------------------
        [LibraryImport("keystone", EntryPoint = "ks_asm", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
        internal static partial int Assemble(
            IntPtr ks,
            string toEncode,
            ulong baseAddress,
            out IntPtr encoding,
            out uint size,
            out uint statements);
    }
}
