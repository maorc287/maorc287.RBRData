// Description: A class to read values from the GaugerPlugin.dll in Richard Burns Rally.
// This class provides methods to read various data types from the memory of the GaugerPlugin process.
// It is currently not in use.

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace maorc287.RBRDataExtPlugin
{
    public static class GaugerReader
    {
        private const string ProcessName = "RichardBurnsRally_SSE";
        private const string DllName = "GaugerPlugin.dll";

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            VirtualMemoryRead = 0x0010,
            VirtualMemoryOperation = 0x0008
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private static Process GetProcess() =>
            Process.GetProcessesByName(ProcessName).FirstOrDefault();

        private static IntPtr GetDllBaseAddress()
        {
            var process = GetProcess();
            if (process == null) return IntPtr.Zero;

            var module = process.Modules.Cast<ProcessModule>()
                .FirstOrDefault(m => m.ModuleName.Equals(DllName, StringComparison.OrdinalIgnoreCase));

            return module?.BaseAddress ?? IntPtr.Zero;
        }

        internal static T ReadValue<T>(uint offset) where T : struct
        {
            var process = GetProcess();
            if (process == null) return default;

            IntPtr dllBase = GetDllBaseAddress();
            if (dllBase == IntPtr.Zero) return default;

            IntPtr hProcess = OpenProcess(ProcessAccessFlags.VirtualMemoryRead | ProcessAccessFlags.VirtualMemoryOperation, false, process.Id);
            if (hProcess == IntPtr.Zero) return default;

            IntPtr finalAddress = IntPtr.Add(dllBase, (int)offset);
            int size = Marshal.SizeOf(typeof(T));
            byte[] buffer = new byte[size];

            bool success = ReadProcessMemory(hProcess, finalAddress, buffer, (uint)size, out _);
            CloseHandle(hProcess);

            if (!success) return default;

            if (typeof(T) == typeof(float)) return (T)(object)BitConverter.ToSingle(buffer, 0);
            if (typeof(T) == typeof(int)) return (T)(object)BitConverter.ToInt32(buffer, 0);
            if (typeof(T) == typeof(short)) return (T)(object)BitConverter.ToInt16(buffer, 0);
            if (typeof(T) == typeof(byte)) return (T)(object)buffer[0];
            if (typeof(T) == typeof(bool)) return (T)(object)(buffer[0] != 0);

            throw new NotSupportedException($"Type {typeof(T)} is not supported.");
        }

        // Optional helpers
        internal static float ReadFloat(uint offset) => ReadValue<float>(offset);
        internal static int ReadInt(uint offset) => ReadValue<int>(offset);
        internal static byte ReadByte(uint offset) => ReadValue<byte>(offset);
        internal static bool ReadBool(uint offset) => ReadValue<bool>(offset);
    }
}
